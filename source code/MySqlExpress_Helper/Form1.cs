using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using MySqlConnector;

namespace MySqlExpress_Helper
{
    public partial class Form1 : Form
    {
        Timer timer1;
        settings appSettings;

        string fileSettings;

        bool displayTooltip = true;

        enum outputType
        {
            Generate_Class_Object,
            Generate_Dictionary_Entries,
            Generate_Create_Table_SQL,
            Create_Update_Column_List,
            Parameters_Dictionary
        }

        outputType EnumOutputType
        {
            get
            {
                if (rbDictionary.Checked)
                    return outputType.Generate_Dictionary_Entries;
                else if (rbCreateTableSql.Checked)
                    return outputType.Generate_Create_Table_SQL;
                else if(rbUpdateCol.Checked)
                    return outputType.Create_Update_Column_List;
                else if (rbParamDictionary.Checked)
                    return outputType.Parameters_Dictionary;

                return outputType.Generate_Class_Object;
            }
        }

        MySqlExpress.FieldsOutputType EnumFieldsOutputType
        {
            get
            {
                switch (cbFieldType.SelectedIndex)
                {
                    case 1:
                        return MySqlExpress.FieldsOutputType.PublicFields;
                    case 2:
                        return MySqlExpress.FieldsOutputType.PublicProperties;
                    default:
                        return MySqlExpress.FieldsOutputType.PrivateFielsPublicProperties;
                }
            }
        }

        public Form1()
        {
            timer1 = new Timer();
            timer1.Interval = 2000;

            InitializeComponent();

            this.Text = $"MySqlExpress Helper v{MySqlExpress.Version}";

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
                    CustomSql = "select a.*, b.`year`, c.name 'teamname', c.code 'teamcode', c.id 'teamid' from player a inner join player_team b on a.id=b.player_id inner join team c on b.team_id=c.id;",
                    FieldType = 0
                };
            }
            else
            {
                this.Location = appSettings.Location;
                this.Size = appSettings.FormSize;
                if (this.Size.Width == 0 || this.Size.Height == 0)
                {
                    this.Size = new Size(950, 650);
                }
            }

            if (this.Size.Height < 200 || this.Size.Width < 200)
            {
                this.Size = new Size(950, 650);
            }

            cbFieldType.SelectedIndex = appSettings.FieldType;
            txtConnStr.Text = appSettings.ConnStr;
            txtSQL.Text = appSettings.CustomSql;

            try
            {
                LoadTableList();
            }
            catch
            {
                listBox1.Items.Clear();
            }

            timer1.Tick += Timer1_Tick;
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

        void LoadTableList()
        {
            List<string> lst = null;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        lst = m.GetTableList();

                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(ex);
                return;
            }

            richTextBox1.Clear();
            listBox1.Items.Clear();

            foreach (var tablename in lst)
            {
                listBox1.Items.Add(tablename);
            }
        }

        private void btLoadTableList_Click(object sender, EventArgs e)
        {
            LoadTableList();
        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBox1.SelectedIndex < 0)
                return;

            try
            {
                string table = listBox1.SelectedItem.ToString();

                switch (EnumOutputType)
                {
                    case outputType.Generate_Class_Object:

                        string data = "";

                        using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conn;
                                conn.Open();

                                MySqlExpress m = new MySqlExpress(cmd);

                                data = m.GenerateTableClassFields(table, EnumFieldsOutputType);

                                conn.Close();
                            }
                        }

                        richTextBox1.Text = data;
                        break;

                    case outputType.Generate_Dictionary_Entries:

                        string data2 = "";
                        using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conn;
                                conn.Open();

                                MySqlExpress m = new MySqlExpress(cmd);

                                data2 = m.GenerateTableDictionaryEntries(table);

                                conn.Close();
                            }
                        }

                        richTextBox1.Text = data2;
                        break;

                    case outputType.Create_Update_Column_List:

                        string data3 = null;

                        using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conn;
                                conn.Open();

                                MySqlExpress m = new MySqlExpress(cmd);

                                data3 = m.GenerateUpdateColumnList(table);

                                conn.Close();
                            }
                        }

                        richTextBox1.Text = data3;

                        break;
                    case outputType.Generate_Create_Table_SQL:

                        string data4 = "";

                        using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conn;
                                conn.Open();

                                MySqlExpress m = new MySqlExpress(cmd);

                                data4 = m.GetCreateTableSql(table);

                                conn.Close();
                            }
                        }

                        richTextBox1.Text = data4;
                        break;

                    case outputType.Parameters_Dictionary:

                        string data5 = "";

                        using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                        {
                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.Connection = conn;
                                conn.Open();

                                MySqlExpress m = new MySqlExpress(cmd);

                                data5 = m.GenerateParameterDictionaryTable(table);

                                conn.Close();
                            }
                        }

                        richTextBox1.Text = data5;
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            richTextBox1.Focus();
            richTextBox1.SelectAll();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.Focus();
            richTextBox1.SelectAll();
            richTextBox1.Copy();

            if (displayTooltip)
            {
                toolTip1.Show("Generated text copied to clipboard", this, 400, 240, 1000);
                displayTooltip = false;
            }
        }

        private void btGenerateCustomSqlObject_Click(object sender, EventArgs e)
        {
            string output = "";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(txtConnStr.Text))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = conn;
                        conn.Open();

                        MySqlExpress m = new MySqlExpress(cmd);

                        output = m.GenerateCustomClassField(txtSQL.Text, EnumFieldsOutputType);

                        conn.Close();
                    }
                }

                richTextBox1.Text = output;
            }
            catch (Exception ex)
            {
                WriteError(ex);
                return;
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("https://github.com/adriancs2/MySqlExpress");
            Process.Start(sInfo);
        }

        void WriteError(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            richTextBox1.Text = "Error:\r\n\r\n" + ex.ToString();
        }

        private void rbClass_CheckedChanged(object sender, EventArgs e)
        {
            listBox1_MouseClick(null, null);
        }

        private void rbDictionary_CheckedChanged(object sender, EventArgs e)
        {
            listBox1_MouseClick(null, null);
        }

        private void rbCreateTableSql_CheckedChanged(object sender, EventArgs e)
        {
            listBox1_MouseClick(null, null);
        }

        private void rbUpdateCol_CheckedChanged(object sender, EventArgs e)
        {
            listBox1_MouseClick(null, null);
        }

        private void rbParamDictionary_CheckedChanged(object sender, EventArgs e)
        {
            listBox1_MouseClick(null, null);
        }
    }
}