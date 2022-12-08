namespace MySqlExpress_Helper
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.txtSQL = new System.Windows.Forms.TextBox();
            this.rbGenClassObject = new System.Windows.Forms.RadioButton();
            this.rbGenDictionary = new System.Windows.Forms.RadioButton();
            this.rbCreateTableSql = new System.Windows.Forms.RadioButton();
            this.rbCreateUpdateColList = new System.Windows.Forms.RadioButton();
            this.cbCreateDateStr = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDateFormat = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btGenerateCustomSqlObject = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.btLoadTableList = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(76, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(200, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "MySQL Connection String:";
            // 
            // txtConnStr
            // 
            this.txtConnStr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnStr.Location = new System.Drawing.Point(288, 7);
            this.txtConnStr.Name = "txtConnStr";
            this.txtConnStr.Size = new System.Drawing.Size(963, 22);
            this.txtConnStr.TabIndex = 1;
            this.txtConnStr.TextChanged += new System.EventHandler(this.txtConnStr_TextChanged);
            // 
            // txtSQL
            // 
            this.txtSQL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSQL.Location = new System.Drawing.Point(288, 36);
            this.txtSQL.Name = "txtSQL";
            this.txtSQL.Size = new System.Drawing.Size(963, 22);
            this.txtSQL.TabIndex = 3;
            this.txtSQL.TextChanged += new System.EventHandler(this.txtSQL_TextChanged);
            // 
            // rbGenClassObject
            // 
            this.rbGenClassObject.AutoSize = true;
            this.rbGenClassObject.Checked = true;
            this.rbGenClassObject.Location = new System.Drawing.Point(12, 74);
            this.rbGenClassObject.Name = "rbGenClassObject";
            this.rbGenClassObject.Size = new System.Drawing.Size(194, 21);
            this.rbGenClassObject.TabIndex = 4;
            this.rbGenClassObject.TabStop = true;
            this.rbGenClassObject.Text = "Generate Class Object";
            this.rbGenClassObject.UseVisualStyleBackColor = true;
            // 
            // rbGenDictionary
            // 
            this.rbGenDictionary.AutoSize = true;
            this.rbGenDictionary.Location = new System.Drawing.Point(212, 74);
            this.rbGenDictionary.Name = "rbGenDictionary";
            this.rbGenDictionary.Size = new System.Drawing.Size(242, 21);
            this.rbGenDictionary.TabIndex = 5;
            this.rbGenDictionary.Text = "Generate Dictionary Entries";
            this.rbGenDictionary.UseVisualStyleBackColor = true;
            // 
            // rbCreateTableSql
            // 
            this.rbCreateTableSql.AutoSize = true;
            this.rbCreateTableSql.Location = new System.Drawing.Point(460, 74);
            this.rbCreateTableSql.Name = "rbCreateTableSql";
            this.rbCreateTableSql.Size = new System.Drawing.Size(226, 21);
            this.rbCreateTableSql.TabIndex = 7;
            this.rbCreateTableSql.Text = "Generate Create Table SQL";
            this.rbCreateTableSql.UseVisualStyleBackColor = true;
            // 
            // rbCreateUpdateColList
            // 
            this.rbCreateUpdateColList.AutoSize = true;
            this.rbCreateUpdateColList.Location = new System.Drawing.Point(692, 74);
            this.rbCreateUpdateColList.Name = "rbCreateUpdateColList";
            this.rbCreateUpdateColList.Size = new System.Drawing.Size(226, 21);
            this.rbCreateUpdateColList.TabIndex = 8;
            this.rbCreateUpdateColList.Text = "Create Update Column List";
            this.rbCreateUpdateColList.UseVisualStyleBackColor = true;
            // 
            // cbCreateDateStr
            // 
            this.cbCreateDateStr.AutoSize = true;
            this.cbCreateDateStr.Location = new System.Drawing.Point(12, 104);
            this.cbCreateDateStr.Name = "cbCreateDateStr";
            this.cbCreateDateStr.Size = new System.Drawing.Size(171, 21);
            this.cbCreateDateStr.TabIndex = 9;
            this.cbCreateDateStr.Text = "Create Date String";
            this.cbCreateDateStr.UseVisualStyleBackColor = true;
            this.cbCreateDateStr.CheckedChanged += new System.EventHandler(this.cbCreateDateStr_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(209, 105);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(160, 17);
            this.label3.TabIndex = 10;
            this.label3.Text = "Date String Format:";
            // 
            // txtDateFormat
            // 
            this.txtDateFormat.Location = new System.Drawing.Point(375, 102);
            this.txtDateFormat.Name = "txtDateFormat";
            this.txtDateFormat.Size = new System.Drawing.Size(128, 22);
            this.txtDateFormat.TabIndex = 11;
            this.txtDateFormat.TextChanged += new System.EventHandler(this.txtDateFormat_TextChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.btLoadTableList);
            this.panel1.Controls.Add(this.txtDateFormat);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.cbCreateDateStr);
            this.panel1.Controls.Add(this.rbCreateUpdateColList);
            this.panel1.Controls.Add(this.rbGenClassObject);
            this.panel1.Controls.Add(this.rbCreateTableSql);
            this.panel1.Controls.Add(this.rbGenDictionary);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(5, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1254, 225);
            this.panel1.TabIndex = 12;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(10, 125);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(655, 63);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Note:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(632, 34);
            this.label4.TabIndex = 15;
            this.label4.Text = "1: Double click table\'s name to generate table class object\r\n2: All MySQL column\'" +
    "s name must be lowercase for MySqlExpress to work properly";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.panel2);
            this.panel3.Controls.Add(this.txtConnStr);
            this.panel3.Controls.Add(this.txtSQL);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(1254, 68);
            this.panel3.TabIndex = 15;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.btGenerateCustomSqlObject);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(282, 68);
            this.panel2.TabIndex = 15;
            // 
            // btGenerateCustomSqlObject
            // 
            this.btGenerateCustomSqlObject.Location = new System.Drawing.Point(5, 33);
            this.btGenerateCustomSqlObject.Name = "btGenerateCustomSqlObject";
            this.btGenerateCustomSqlObject.Size = new System.Drawing.Size(271, 28);
            this.btGenerateCustomSqlObject.TabIndex = 17;
            this.btGenerateCustomSqlObject.Text = "Generate Customized SQL Object:";
            this.btGenerateCustomSqlObject.UseVisualStyleBackColor = true;
            this.btGenerateCustomSqlObject.Click += new System.EventHandler(this.btGenerateCustomSqlObject_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(305, 200);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 17);
            this.label5.TabIndex = 16;
            this.label5.Text = "Output:";
            // 
            // btLoadTableList
            // 
            this.btLoadTableList.Location = new System.Drawing.Point(5, 194);
            this.btLoadTableList.Name = "btLoadTableList";
            this.btLoadTableList.Size = new System.Drawing.Size(178, 28);
            this.btLoadTableList.TabIndex = 14;
            this.btLoadTableList.Text = "Refresh Table List";
            this.btLoadTableList.UseVisualStyleBackColor = true;
            this.btLoadTableList.Click += new System.EventHandler(this.btLoadTableList_Click);
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 17;
            this.listBox1.Location = new System.Drawing.Point(5, 230);
            this.listBox1.Margin = new System.Windows.Forms.Padding(0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(308, 404);
            this.listBox1.TabIndex = 13;
            this.listBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDoubleClick);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(227)))));
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(313, 230);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(946, 404);
            this.richTextBox1.TabIndex = 14;
            this.richTextBox1.Text = "";
            this.richTextBox1.WordWrap = false;
            this.richTextBox1.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(5, 634);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1254, 22);
            this.statusStrip1.TabIndex = 15;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("Cascadia Code", 9F, System.Drawing.FontStyle.Bold);
            this.toolStripStatusLabel1.IsLink = true;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(294, 17);
            this.toolStripStatusLabel1.Text = "https://github.com/adriancs2/MySqlExpress";
            this.toolStripStatusLabel1.Click += new System.EventHandler(this.toolStripStatusLabel1_Click);
            // 
            // Form1
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1264, 661);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Cascadia Mono", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form1";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MySqlExpress Helper v1.1";
            this.ResizeEnd += new System.EventHandler(this.Form1_ResizeEnd);
            this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtConnStr;
        private System.Windows.Forms.TextBox txtSQL;
        private System.Windows.Forms.RadioButton rbGenClassObject;
        private System.Windows.Forms.RadioButton rbGenDictionary;
        private System.Windows.Forms.RadioButton rbCreateTableSql;
        private System.Windows.Forms.RadioButton rbCreateUpdateColList;
        private System.Windows.Forms.CheckBox cbCreateDateStr;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDateFormat;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btLoadTableList;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btGenerateCustomSqlObject;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}

