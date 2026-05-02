using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySqlConnector;

namespace MySqlExpressHelper
{
    public partial class Form2 : Form
    {
        // ------------------------------------------------------------
        //  Output-type enum (mirrors the web handler's "kind" switch)
        // ------------------------------------------------------------
        enum OutputKind
        {
            ClassFields,
            DictionaryEntries,
            CreateTableSql,
            UpdateColumnList,
            ParameterDictionary
        }

        // ------------------------------------------------------------
        //  Fields
        // ------------------------------------------------------------
        Timer saveTimer;
        HelperSettings appSettings;
        string fileSettings;
        bool loading = true;                    // suppresses save during initial load
        bool suppressAutoCopy = false;          // guards RTB.TextChanged from firing on manual edits

        OutputKind CurrentOutputKind
        {
            get
            {
                if (rbDictionary.Checked) return OutputKind.DictionaryEntries;
                if (rbCreateTableSql.Checked) return OutputKind.CreateTableSql;
                if (rbUpdateCol.Checked) return OutputKind.UpdateColumnList;
                if (rbParamDictionary.Checked) return OutputKind.ParameterDictionary;
                return OutputKind.ClassFields;
            }
        }

        MySqlExpress.FieldsOutputType CurrentFieldsOutputType
        {
            get
            {
                switch (cbFieldType.SelectedIndex)
                {
                    case 1: return MySqlExpress.FieldsOutputType.PublicProperties;
                    case 2: return MySqlExpress.FieldsOutputType.PublicFields;
                    default: return MySqlExpress.FieldsOutputType.PrivateFieldsPublicProperties;
                }
            }
        }

        // ------------------------------------------------------------
        //  Ctor + settings load
        // ------------------------------------------------------------
        public Form2()
        {
            InitializeComponent();

            this.Text = $"MySqlExpress Helper v{MySqlExpress.Version}";

            fileSettings = Path.Combine(Application.StartupPath, "settings.ini");

            // Load settings
            if (File.Exists(fileSettings))
            {
                try
                {
                    appSettings = HelperSettings.FromIni(File.ReadAllText(fileSettings));
                }
                catch
                {
                    appSettings = new HelperSettings();
                }
            }
            else
            {
                appSettings = new HelperSettings();
            }

            // Apply size / location (sanity-check to avoid off-screen or collapsed windows)
            if (appSettings.FormWidth >= 600 && appSettings.FormHeight >= 300)
            {
                this.Size = new Size(appSettings.FormWidth, appSettings.FormHeight);
            }

            if (appSettings.LocationX >= 0 && appSettings.LocationY >= 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(appSettings.LocationX, appSettings.LocationY);
            }

            // Apply values to controls
            txtConnStr.Text = appSettings.ConnStr;
            txtSQL.Text = appSettings.CustomSql;
            cbFieldType.SelectedIndex = Math.Max(0, Math.Min(2, appSettings.FieldType));
            chkAutoCopy.Checked = appSettings.AutoCopy;
            txtPrefixModelFilename.Text = appSettings.ClassModelPrefix;

            // Output RTB text changes are only for auto-copy; we'll call Copy manually
            // from ShowOutput(), not from TextChanged. That way we never steal focus
            // or copy on manual edits (e.g. after Convert).

            loading = false;

            // Try an initial table-list load; silent failure is fine
            try { LoadTableList(); } catch { }

            saveTimer = new Timer();
            saveTimer.Interval = 5000;
            saveTimer.Tick += SaveTimer_Tick;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            txtConnStr.Focus();
            txtConnStr.Select(0, 0);
        }

        // ------------------------------------------------------------
        //  Debounced save
        // ------------------------------------------------------------
        void KickSaveTimer()
        {
            if (loading) return;
            if (saveTimer != null)
            {
                saveTimer.Stop();
                saveTimer.Start();
            }
        }

        void SaveTimer_Tick(object sender, EventArgs e)
        {
            saveTimer.Stop();

            appSettings.ConnStr = txtConnStr.Text;
            appSettings.CustomSql = txtSQL.Text;
            appSettings.FieldType = cbFieldType.SelectedIndex < 0 ? 0 : cbFieldType.SelectedIndex;
            appSettings.AutoCopy = chkAutoCopy.Checked;
            appSettings.LocationX = this.Location.X;
            appSettings.LocationY = this.Location.Y;
            appSettings.FormWidth = this.Size.Width;
            appSettings.FormHeight = this.Size.Height;
            appSettings.ClassModelPrefix = this.txtPrefixModelFilename.Text;

            try
            {
                File.WriteAllText(fileSettings, appSettings.ToIni());
            }
            catch
            {
                // Settings are not critical; swallow.
            }
        }

        private void txtConnStr_TextChanged(object sender, EventArgs e) { KickSaveTimer(); }
        private void txtSQL_TextChanged(object sender, EventArgs e) { KickSaveTimer(); }
        private void cbFieldType_SelectedIndexChanged(object sender, EventArgs e)
        {
            KickSaveTimer();
            RegenerateCurrent(); // keep output in sync with field-type change
        }
        private void chkAutoCopy_CheckedChanged(object sender, EventArgs e) { KickSaveTimer(); }
        private void Form2_ResizeEnd(object sender, EventArgs e) { KickSaveTimer(); }
        private void Form2_SizeChanged(object sender, EventArgs e) { KickSaveTimer(); }
        private void txtPrefixModelFilename_TextChanged(object sender, EventArgs e) { KickSaveTimer(); }

        // ------------------------------------------------------------
        //  Connection boilerplate -> single helper
        // ------------------------------------------------------------
        T RunOnExpress<T>(Func<MySqlExpress, T> action)
        {
            using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                conn.Open();
                return action(new MySqlExpress(cmd));
            }
        }

        // ------------------------------------------------------------
        //  Table list
        // ------------------------------------------------------------
        void LoadTableList()
        {
            List<string> lst;
            try
            {
                lst = RunOnExpress(m => m.GetTableList());
            }
            catch (Exception ex)
            {
                WriteError(ex);
                listBox1.Items.Clear();
                return;
            }

            listBox1.Items.Clear();
            richTextBox1.Clear();
            lblMethod.Text = lst.Count == 0
                ? "(no tables found – check connection or run Setup first)"
                : "Click a table name or use Custom SQL to generate.";

            foreach (string t in lst)
                listBox1.Items.Add(t);
        }

        private void btRefreshTables_Click(object sender, EventArgs e)
        {
            LoadTableList();
        }

        // ------------------------------------------------------------
        //  Output rendering (centralized so auto-copy is predictable)
        // ------------------------------------------------------------
        void ShowOutput(string code, string methodCall)
        {
            lblMethod.Text = string.IsNullOrEmpty(methodCall) ? "" : methodCall;

            suppressAutoCopy = true;
            richTextBox1.Text = code ?? "";
            suppressAutoCopy = false;

            if (chkAutoCopy.Checked && !string.IsNullOrEmpty(code))
            {
                try
                {
                    Clipboard.SetText(code);
                    toolTip1.Show("Generated text copied to clipboard",
                                  this, 500, 260, 1200);
                }
                catch
                {
                    // Clipboard access can fail (e.g. RDP); don't crash.
                }
            }
        }

        // ------------------------------------------------------------
        //  Single regenerate path (listbox click, radio change)
        // ------------------------------------------------------------
        void RegenerateCurrent()
        {
            if (listBox1.SelectedIndex < 0) return;

            string table = listBox1.SelectedItem.ToString();
            OutputKind kind = CurrentOutputKind;
            MySqlExpress.FieldsOutputType styleEnum = CurrentFieldsOutputType;

            try
            {
                string code = "";
                string method = "";

                RunOnExpress<object>(m =>
                {
                    switch (kind)
                    {
                        case OutputKind.ClassFields:
                            code = m.GenerateTableClassFields(table, styleEnum);
                            method = $"m.GenerateTableClassFields(\"{table}\", FieldsOutputType.{styleEnum})";
                            break;

                        case OutputKind.DictionaryEntries:
                            code = m.GenerateTableDictionaryEntries(table);
                            method = $"m.GenerateTableDictionaryEntries(\"{table}\")";
                            break;

                        case OutputKind.UpdateColumnList:
                            code = m.GenerateUpdateColumnList(table);
                            method = $"m.GenerateUpdateColumnList(\"{table}\")";
                            break;

                        case OutputKind.CreateTableSql:
                            code = m.GetCreateTableSql(table);
                            method = $"m.GetCreateTableSql(\"{table}\")";
                            break;

                        case OutputKind.ParameterDictionary:
                            code = m.GenerateParameterDictionaryTable(table);
                            method = $"m.GenerateParameterDictionaryTable(\"{table}\")";
                            break;
                    }
                    return null;
                });

                ShowOutput(code, method);
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
            RegenerateCurrent();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Jump to output and select-all so the user can manually Ctrl+C
            // if auto-copy is off.
            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

        // Guard: CheckedChanged fires twice per user click (old rb unchecks, new rb checks).
        // Only regenerate on the "checked" side.
        private void OutputType_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;
            RegenerateCurrent();
        }

        // ------------------------------------------------------------
        //  Custom SQL -> class
        // ------------------------------------------------------------
        private void btGenerateFromSql_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSQL.Text))
            {
                WriteError(new Exception("Provide a SELECT statement in the Custom SELECT box."));
                return;
            }

            MySqlExpress.FieldsOutputType styleEnum = CurrentFieldsOutputType;

            try
            {
                string code = RunOnExpress(m =>
                    m.GenerateCustomClassField(txtSQL.Text, styleEnum));

                string method = $"m.GenerateCustomClassField(sql, FieldsOutputType.{styleEnum})";
                ShowOutput(code, method);
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }

        // ------------------------------------------------------------
        //  Convert private fields -> public properties
        //  (Preallocated-model workflow: user writes `int member_id = 0;`
        //   lines, this produces matching public properties that align
        //   with the MySqlExpress-generated style.)
        // ------------------------------------------------------------
        private void btConvertToProps_Click(object sender, EventArgs e)
        {
            string[] lines = richTextBox1.Lines;

            for (int i = 0; i < lines.Length; i++)
            {
                string a = lines[i].Replace(";", string.Empty).Trim();

                // Collapse runs of whitespace
                while (a.Contains("  "))
                    a = a.Replace("  ", " ");

                // Strip optional "private " prefix
                if (a.StartsWith("private "))
                    a = a.Substring("private ".Length);

                // Must be "<type> <name> = <default>"
                string[] sa = a.Split(new[] { " = " }, StringSplitOptions.None);
                if (sa.Length != 2) continue;

                string[] typeAndName = sa[0].Trim().Split(' ');
                if (typeAndName.Length < 2) continue;

                string type = typeAndName[0];
                string fieldName = typeAndName[1];

                // Convert snake_case -> PascalCase for property name
                string propName = SnakeToPascal(fieldName);

                lines[i] = $"public {type} {propName} {{ get {{ return {fieldName}; }} set {{ {fieldName} = value; }} }}";
            }

            // Bypass auto-copy when user is editing the output manually
            suppressAutoCopy = true;
            richTextBox1.Lines = lines;
            suppressAutoCopy = false;
        }

        static string SnakeToPascal(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            StringBuilder sb = new StringBuilder(s.Length);
            bool upperNext = true;
            foreach (char c in s)
            {
                if (c == '_') { upperNext = true; continue; }
                if (upperNext) { sb.Append(char.ToUpper(c)); upperNext = false; }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------
        //  Misc
        // ------------------------------------------------------------
        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo("https://github.com/adriancs2/MySqlExpress")
                {
                    UseShellExecute = true
                };
                Process.Start(sInfo);
            }
            catch { }
        }

        void WriteError(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;

            lblMethod.Text = "Error";
            suppressAutoCopy = true;
            richTextBox1.Text = "Error:\r\n\r\n" + ex.Message;
            suppressAutoCopy = false;
        }

        private void txtConnStr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                btRefreshTables_Click(null, null);
            }
        }

        private void txtSQL_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                btGenerateFromSql_Click(null, null);
            }
        }

        private void btGenerateModelFiles_Click(object sender, EventArgs e)
        {
            string prefixFilename = (txtPrefixModelFilename.Text ?? "").Trim();
            string folder = (txtModelFolder.Text ?? "").Trim();

            if (listBox1.Items.Count == 0)
            {
                WriteError(new Exception("No tables to generate. Click 'Refresh Tables' first."));
                return;
            }

            if (string.IsNullOrEmpty(folder))
            {
                WriteError(new Exception("Please select an output folder."));
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                WriteError(ex);
                return;
            }

            // Match the field pattern selected by the user via cbFieldType:
            //  - private fields + public properties  (PrivateFieldsPublicProperties)
            //  - public properties only              (PublicProperties)
            //  - public fields only                  (PublicFields)
            MySqlExpress.FieldsOutputType styleEnum = CurrentFieldsOutputType;

            int genWritten = 0;
            int genUnchanged = 0;
            int mainWritten = 0;
            int mainSkipped = 0;
            var report = new StringBuilder();

            try
            {
                RunOnExpress<object>(m =>
                {
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        string tableName = listBox1.Items[i].ToString();
                        string pascalName = SnakeToPascal(tableName);
                        string className = $"{prefixFilename}{pascalName}";

                        // Generate fields/properties body via the existing helper,
                        // honoring the cbFieldType selection.
                        string body = m.GenerateTableClassFields(tableName, styleEnum);

                        // Indent every body line by 8 spaces (inside namespace + class).
                        string indentedBody = IndentBlock(body, "        ");

                        // Build .gen.cs content
                        StringBuilder gen = new StringBuilder();
                        gen.AppendLine("// ----------------------------------------------------------------------");
                        gen.AppendLine("//  Auto-generated by MySqlExpress Helper.");
                        gen.AppendLine("//  Mirrors the MySQL table schema exactly.");
                        gen.AppendLine("//  This file is regenerated on demand. Do not edit by hand;");
                        gen.AppendLine("//  any changes will be overwritten on the next regeneration.");
                        gen.AppendLine($"//  Add custom code in the matching partial class file: {className}.cs");
                        gen.AppendLine("// ----------------------------------------------------------------------");
                        gen.AppendLine();
                        gen.AppendLine("namespace System");
                        gen.AppendLine("{");
                        gen.AppendLine($"    public partial class {className}");
                        gen.AppendLine("    {");
                        gen.AppendLine(indentedBody);
                        gen.AppendLine("    }");
                        gen.Append("}");

                        string genPath = Path.Combine(folder, $"{className}.gen.cs");
                        bool wrote = WriteIfChanged(genPath, gen.ToString());
                        if (wrote) genWritten++;
                        else genUnchanged++;

                        // Build main .cs content (empty partial – do NOT overwrite if
                        // the user has already added code there).
                        string mainPath = Path.Combine(folder, $"{className}.cs");
                        if (!File.Exists(mainPath))
                        {
                            StringBuilder main = new StringBuilder();
                            main.AppendLine("namespace System");
                            main.AppendLine("{");
                            main.AppendLine($"    public partial class {className}");
                            main.AppendLine("    {");
                            main.AppendLine("        ");
                            main.AppendLine("    }");
                            main.Append("}");

                            File.WriteAllText(mainPath, main.ToString());
                            mainWritten++;
                        }
                        else
                        {
                            mainSkipped++;
                        }

                        string status = wrote ? "(updated)" : "(unchanged)";
                        report.AppendLine($"{tableName,-40} -> {className,-40} {status}");
                    }
                    return null;
                });

                StringBuilder summary = new StringBuilder();
                summary.AppendLine($"Field style: {styleEnum}");
                summary.AppendLine($"Wrote     {genWritten} *.gen.cs file(s).");
                if (genUnchanged > 0)
                    summary.AppendLine($"Unchanged {genUnchanged} *.gen.cs file(s) (content identical, file untouched).");
                summary.AppendLine($"Created   {mainWritten} new main *.cs file(s).");
                if (mainSkipped > 0)
                    summary.AppendLine($"Skipped   {mainSkipped} existing main *.cs file(s) (preserved user code).");
                summary.AppendLine($"Folder: {folder}");
                summary.AppendLine();
                summary.Append(report.ToString());

                ShowOutput(summary.ToString(),
                    $"Generated model files in: {folder}");
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }

        // Prefix every line in `block` with `indent`. Blank lines are kept blank
        // (no trailing whitespace) for cleaner output.
        static string IndentBlock(string block, string indent)
        {
            if (string.IsNullOrEmpty(block)) return "";
            string[] lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.AppendLine();
                if (lines[i].Length == 0)
                    sb.Append("");
                else
                    sb.Append(indent + lines[i]);
            }
            return sb.ToString();
        }

        // Write `content` to `path` only if the file does not exist or its current
        // content differs (ordinal/byte-identical comparison). Returns true when a
        // write actually happened. Avoiding no-op writes keeps file mtime stable,
        // which is friendlier to IDEs, file watchers, and source control.
        static bool WriteIfChanged(string path, string content)
        {
            if (File.Exists(path))
            {
                try
                {
                    string existing = File.ReadAllText(path);
                    if (string.Equals(existing, content, StringComparison.Ordinal))
                        return false;
                }
                catch
                {
                    // If we can't read it, fall through and try to overwrite.
                }
            }

            File.WriteAllText(path, content);
            return true;
        }

        private void btSelectModelFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            if (f.ShowDialog() == DialogResult.OK)
            {
                txtModelFolder.Text = f.SelectedPath;
            }
        }
    }

    // ------------------------------------------------------------
    //  Settings (plain INI text, no dependency, no BinaryFormatter)
    // ------------------------------------------------------------
    class HelperSettings
    {
        public string ConnStr =
            "server=127.0.0.1;user=root;pwd=1234;database=test;" +
            "convertzerodatetime=true;treattinyasboolean=true;";

        public string CustomSql =
            "select a.*, b.`year`, c.name 'teamname', c.code 'teamcode', c.id 'teamid' " +
            "from player a inner join player_team b on a.id=b.player_id " +
            "inner join team c on b.team_id=c.id;";

        public int FieldType = 0;            // index into cbFieldType
        public bool AutoCopy = true;         // auto-copy generated text
        public int LocationX = -1;
        public int LocationY = -1;
        public int FormWidth = 1020;
        public int FormHeight = 720;
        public string ClassModelPrefix = "ob";

        public string ToIni()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# MySqlExpress Helper settings");
            sb.AppendLine("ConnStr=" + Escape(ConnStr));
            sb.AppendLine("CustomSql=" + Escape(CustomSql));
            sb.AppendLine("FieldType=" + FieldType);
            sb.AppendLine("AutoCopy=" + (AutoCopy ? "1" : "0"));
            sb.AppendLine("LocationX=" + LocationX);
            sb.AppendLine("LocationY=" + LocationY);
            sb.AppendLine("FormWidth=" + FormWidth);
            sb.AppendLine("FormHeight=" + FormHeight);
            sb.AppendLine("ClassModelPrefix=" + ClassModelPrefix);
            return sb.ToString();
        }

        public static HelperSettings FromIni(string text)
        {
            HelperSettings s = new HelperSettings();
            if (string.IsNullOrEmpty(text))
                return s;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#") || line.StartsWith(";")) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1); // don't trim value, user may want trailing space in conn str

                switch (key)
                {
                    case "ConnStr": s.ConnStr = Unescape(val); break;
                    case "CustomSql": s.CustomSql = Unescape(val); break;
                    case "FieldType": int.TryParse(val, out s.FieldType); break;
                    case "AutoCopy": s.AutoCopy = (val.Trim() == "1" || val.Trim().ToLower() == "true"); break;
                    case "LocationX": int.TryParse(val, out s.LocationX); break;
                    case "LocationY": int.TryParse(val, out s.LocationY); break;
                    case "FormWidth": int.TryParse(val, out s.FormWidth); break;
                    case "FormHeight": int.TryParse(val, out s.FormHeight); break;
                    case "ClassModelPrefix": s.ClassModelPrefix = val; break;
                }
            }

            return s;
        }

        // Line breaks in values would break our line-oriented parser,
        // so escape them. Everything else is passed through.
        static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    if (n == 'r') { sb.Append('\r'); i++; continue; }
                    if (n == 'n') { sb.Append('\n'); i++; continue; }
                    if (n == '\\') { sb.Append('\\'); i++; continue; }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}