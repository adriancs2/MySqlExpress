using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySqlConnector;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MySqlExpress_Helper
{
    public partial class Form1 : Form
    {
        Timer timer1;
        Timer timer2;
        settings appSettings;

        string fileSettings;

        enum outputType
        {
            Generate_Class_Object,
            Generate_Dictionary_Entries,
            Generate_Create_Table_SQL,
            Create_Update_Column_List
        }

        outputType EnumOutputType
        {
            get
            {
                return (outputType)cbOutputType.SelectedIndex;
            }
        }

        public Form1()
        {
            timer1 = new Timer();
            timer1.Interval = 2000;

            timer2 = new Timer();
            timer2.Interval = 800;

            InitializeComponent();

            fileSettings = Path.Combine(Application.StartupPath, "settings");

            if (File.Exists(fileSettings))
            {
                try
                {
                    byte[] baFileSettings = File.ReadAllBytes(fileSettings);

                    using (var memStream = new MemoryStream())
                    {
                        var binForm = new BinaryFormatter();
                        memStream.Write(baFileSettings, 0, baFileSettings.Length);
                        memStream.Seek(0, SeekOrigin.Begin);
                        var obj = binForm.Deserialize(memStream);
                        appSettings = (settings)obj;
                    }
                }
                catch
                {
                    appSettings = null;
                }
            }

            if (appSettings == null)
            {
                appSettings = new settings()
                {
                    ConnStr = "server=127.0.0.1;user=root;pwd=1234;database=test;convertzerodatetime=true;treattinyasboolean=true;",
                    CustomSql = "select a.id 'player_id', a.name 'player_name', c.id 'team_id', c.name 'team_name' from player a, player_team b, team c where a.id=b.player_id and b.team_id=c.id;",
                    FieldType = 0
                };
            }
            else
            {
                this.Location = appSettings.Location;
                this.Size = appSettings.FormSize;
                if (this.Size.Width == 0 || this.Size.Height == 0)
                {
                    this.Size = new Size(1280, 700);
                }
            }

            cbFieldType.SelectedIndex = appSettings.FieldType;
            txtConnStr.Text = appSettings.ConnStr;
            txtSQL.Text = appSettings.CustomSql;

            try
            {
                btLoadTableList_Click(null, null);
            }
            catch
            {
                listBox1.Items.Clear();
            }

            timer1.Tick += Timer1_Tick;
            timer2.Tick += Timer2_Tick;
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            btLoadTableList_Click(null, null);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();

            appSettings.ConnStr = txtConnStr.Text;
            appSettings.CustomSql = txtSQL.Text;
            appSettings.Location = this.Location;
            appSettings.FormSize = this.Size;
            appSettings.FieldType = cbFieldType.SelectedIndex;

            byte[] ba = null;

            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, appSettings);
                ba = ms.ToArray();
            }

            File.WriteAllBytes(fileSettings, ba);
        }

        private void txtConnStr_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();

            timer2.Stop();
            timer2.Start();
        }

        private void txtSQL_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void cbCreateDateStr_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void txtDateFormat_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void cbFieldType_SelectedIndexChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void btLoadTableList_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        cmd.CommandText = "show tables;";
                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        da.Fill(dt);

                        conn.Close();
                    }
                }

                richTextBox1.Clear();
            }
            catch (Exception ex)
            {
                richTextBox1.Text = "Possible Connection String Error.\r\n\r\nError message:\r\n\r\n" + ex.Message;
                return;
            }
            finally
            {
                listBox1.Items.Clear();
            }

            foreach (DataRow dr in dt.Rows)
            {
                listBox1.Items.Add(dr[0] + "");
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string table = listBox1.SelectedItem.ToString();

            switch (EnumOutputType)
            {
                case outputType.Generate_Class_Object:
                case outputType.Generate_Dictionary_Entries:
                    DataTable dt = new DataTable();
                    using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                    {
                        MySqlCommand cmd = new MySqlCommand();
                        cmd.Connection = conn;
                        conn.Open();

                        cmd.CommandText = $"show columns from `{table}`;";
                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        da.Fill(dt);

                        conn.Close();
                    }
                    if (EnumOutputType == outputType.Generate_Class_Object)
                        GenerateClassObject(dt);
                    else if (EnumOutputType == outputType.Generate_Dictionary_Entries)
                        GenerateDictionary(dt);
                    break;
                case outputType.Create_Update_Column_List:
                    CreateUpdateColList(table);
                    break;
                case outputType.Generate_Create_Table_SQL:
                    GenerateCreateTableSQL(table);
                    break;
            }
        }

        void GenerateClassObject(DataTable dt)
        {
            Dictionary<string, string> dicDate = new Dictionary<string, string>();

            List<string> lst = new List<string>();
            List<string> lst2 = new List<string>();

            foreach (DataRow dr in dt.Rows)
            {
                bool isDate = false;
                string key = dr["Key"] + "";
                string extra = dr["Extra"] + "";
                string field = dr[0] + "";
                string data = dr[1] + "";
                string datatype = "";
                string datavalue = "";

                if (data.Contains("datetime"))
                {
                    isDate = true;
                    datatype = "DateTime";
                    datavalue = "DateTime.MinValue";
                }
                else if (data.Contains("varchar") || data.Contains("text"))
                {
                    datatype = "string";
                    datavalue = "\"\"";
                }
                else if (data.Contains("tinyint"))
                {
                    datatype = "bool";
                    datavalue = "false";
                }
                else if (data.Contains("bigint"))
                {
                    datatype = "long";
                    datavalue = "0L";
                }
                else if (data.Contains("int"))
                {
                    datatype = "int";
                    datavalue = "0";
                }
                else if (data.Contains("decimal"))
                {
                    datatype = "decimal";
                    datavalue = "0m";
                }
                else
                {
                    MessageBox.Show($"DataType unhandled. Column: {data}, DataType: {data}");
                    return;
                }

                if (cbFieldType.SelectedIndex == 2)
                {
                    lst.Add(string.Format("{0} {1} = {2};", datatype, field, datavalue));

                    string field2 = GetUpperCaseColName(field);

                    if (isDate)
                        dicDate[field2] = field;

                    lst2.Add($"public {datatype} {field2} {{ get {{ return {field}; }} set {{ {field} = value; }} }}");
                }
                else if (cbFieldType.SelectedIndex == 1)
                {
                    lst.Add($"public {datatype} {field} = {datavalue};");
                }
                else
                {
                    lst.Add($"public {datatype} {field} {{ get; set; }}");
                }
            }

            lst.Add("");

            foreach (string s in lst2)
            {
                lst.Add(s);
            }

            richTextBox1.Lines = lst.ToArray();

            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

        void GenerateDictionary(DataTable dt)
        {
            List<string> lst = new List<string>();
            lst.Add("Dictionary<string, object> dic = new Dictionary<string, object>();");
            lst.Add("");

            foreach (DataRow dr in dt.Rows)
            {
                string field = dr[0] + "";
                lst.Add(string.Format("            dic[\"{0}\"] = ", field));
            }

            richTextBox1.Lines = lst.ToArray();

            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

        void GenerateCreateTableSQL(string tablename)
        {
            DataTable dt = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;

                    cmd.CommandText = $"show create table `{tablename}`;";
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    da.Fill(dt);

                    conn.Close();
                }
            }

            richTextBox1.Text = dt.Rows[0][1] + "";
        }

        void CreateUpdateColList(string table)
        {
            List<string> lst = new List<string>();
            DataTable dt = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;

                    cmd.CommandText = $"show columns from `{table}`;";
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    da.Fill(dt);

                    conn.Close();
                }
            }

            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Key"] + "" == "")
                {
                    lst.Add(dr[0] + "");
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.Append("List<string> lstUpdateCol = new List<string>();");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var l in lst)
            {
                sb.AppendLine($"lstUpdateCol.Add(\"{l}\");");
            }

            string s = sb.ToString().Substring(0, sb.Length - 2);

            richTextBox1.Text = s;
            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

        string GetUpperCaseColName(string colname)
        {
            bool toUpperCase = true;

            StringBuilder sb = new StringBuilder();
            foreach (char c in colname)
            {
                if (c == '_')
                {
                    toUpperCase = true;
                    continue;
                }
                if (toUpperCase)
                {
                    sb.Append(Char.ToUpper(c));
                    toUpperCase = false;
                    continue;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }

        private void btGenerateCustomSqlObject_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    conn.Open();

                    cmd.CommandText = txtSQL.Text;
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    da.Fill(dt);

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                richTextBox1.Text = "Error:\r\n\r\n" + ex.Message;
                return;
            }

            List<string> lst = new List<string>();

            if (cbFieldType.SelectedIndex == 1)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    var targetType = dc.DataType;

                    if (targetType == typeof(String))
                    {
                        lst.Add($@"public string {dc.ColumnName} = """";");
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        lst.Add($@"public DateTime {dc.ColumnName} = DateTime.MinValue;");
                    }
                    else if (targetType == typeof(bool))
                    {
                        lst.Add($@"public bool {dc.ColumnName} = false;");
                    }
                    else if (targetType == typeof(short) ||
                        targetType == typeof(ushort))
                    {
                        lst.Add($"public short {dc.ColumnName} = 0;");
                    }
                    else if (targetType == typeof(int) ||
                        targetType == typeof(uint))

                    {
                        lst.Add($"public int {dc.ColumnName} = 0;");
                    }
                    else if (targetType == typeof(long) ||
                        targetType == typeof(ulong))
                    {
                        lst.Add($"public long {dc.ColumnName} = 0L;");
                    }
                    else if (targetType == typeof(decimal))
                    {
                        lst.Add($"public decimal {dc.ColumnName} = 0m;");
                    }
                    else
                    {
                        richTextBox1.Text = $"Unhandled data type: {targetType}, column name: {dc.ColumnName}";
                        return;
                    }
                }
            }
            else if (cbFieldType.SelectedIndex == 2)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    var targetType = dc.DataType;

                    if (targetType == typeof(String))
                    {
                        lst.Add($@"string {dc.ColumnName} = """";");
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        lst.Add($@"DateTime {dc.ColumnName} = DateTime.MinValue;");
                    }
                    else if (targetType == typeof(bool))
                    {
                        lst.Add($@"bool {dc.ColumnName} = false;");
                    }
                    else if (targetType == typeof(short) ||
                        targetType == typeof(ushort))
                    {
                        lst.Add($"short {dc.ColumnName} = 0;");
                    }
                    else if (targetType == typeof(int) ||
                        targetType == typeof(uint))

                    {
                        lst.Add($"int {dc.ColumnName} = 0;");
                    }
                    else if (targetType == typeof(long) ||
                        targetType == typeof(ulong))
                    {
                        lst.Add($"long {dc.ColumnName} = 0L;");
                    }
                    else if (targetType == typeof(decimal))
                    {
                        lst.Add($"decimal {dc.ColumnName} = 0m;");
                    }
                    else
                    {
                        richTextBox1.Text = $"Unhandled data type: {targetType}, column name: {dc.ColumnName}";
                        return;
                    }
                }

                lst.Add("");

                foreach (DataColumn dc in dt.Columns)
                {
                    string field2 = GetUpperCaseColName(dc.ColumnName);

                    var targetType = dc.DataType;

                    if (targetType == typeof(String))
                    {
                        lst.Add($@"public string {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        lst.Add($@"public DateTime {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(bool))
                    {
                        lst.Add($@"public bool {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(short) || targetType == typeof(ushort))
                    {
                        lst.Add($"public short {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(int) || targetType == typeof(uint))
                    {
                        lst.Add($"public int {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(long) || targetType == typeof(ulong))
                    {
                        lst.Add($"public long {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                    else if (targetType == typeof(decimal))
                    {
                        lst.Add($"public decimal {field2} {{ get {{ return {dc.ColumnName}; }} set {{ {dc.ColumnName} = value; }} }}");
                    }
                }
            }
            else
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    var targetType = dc.DataType;

                    if (targetType == typeof(String))
                    {
                        lst.Add($@"public string {dc.ColumnName} {{ get; set; }}");
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        lst.Add($@"public DateTime {dc.ColumnName} {{ get; set; }}");
                    }
                    else if (targetType == typeof(bool))
                    {
                        lst.Add($@"public bool {dc.ColumnName}  {{ get; set; }}");
                    }
                    else if (targetType == typeof(short) ||
                        targetType == typeof(ushort))
                    {
                        lst.Add($"public short {dc.ColumnName} {{ get; set; }}");
                    }
                    else if (targetType == typeof(int) ||
                        targetType == typeof(uint))

                    {
                        lst.Add($"public int {dc.ColumnName}  {{ get; set; }}");
                    }
                    else if (targetType == typeof(long) ||
                        targetType == typeof(ulong))
                    {
                        lst.Add($"public long {dc.ColumnName}  {{ get; set; }}"); ;
                    }
                    else if (targetType == typeof(decimal))
                    {
                        lst.Add($"public decimal {dc.ColumnName}  {{ get; set; }}");
                    }
                    else
                    {
                        richTextBox1.Text = $"Unhandled data type: {targetType}, column name: {dc.ColumnName}";
                        return;
                    }
                }
            }

            

            richTextBox1.Lines = lst.ToArray();

            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

    private void toolStripStatusLabel1_Click(object sender, EventArgs e)
    {
        ProcessStartInfo sInfo = new ProcessStartInfo("https://github.com/adriancs2/MySqlExpress");
        Process.Start(sInfo);
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        cbOutputType.SelectedIndex = 0;
    }

}
}