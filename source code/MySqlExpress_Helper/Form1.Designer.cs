﻿namespace MySqlExpress_Helper
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.txtSQL = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rbParamDictionary = new System.Windows.Forms.RadioButton();
            this.rbUpdateCol = new System.Windows.Forms.RadioButton();
            this.rbCreateTableSql = new System.Windows.Forms.RadioButton();
            this.rbDictionary = new System.Windows.Forms.RadioButton();
            this.rbClass = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cbFieldType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btGenerateCustomSqlObject = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.btLoadTableList = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.btConvertPrivateFieldsToPublicProperties = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
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
            // panel1
            // 
            this.panel1.Controls.Add(this.btConvertPrivateFieldsToPublicProperties);
            this.panel1.Controls.Add(this.rbParamDictionary);
            this.panel1.Controls.Add(this.rbUpdateCol);
            this.panel1.Controls.Add(this.rbCreateTableSql);
            this.panel1.Controls.Add(this.rbDictionary);
            this.panel1.Controls.Add(this.rbClass);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cbFieldType);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.btLoadTableList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(5, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1254, 176);
            this.panel1.TabIndex = 12;
            // 
            // rbParamDictionary
            // 
            this.rbParamDictionary.AutoSize = true;
            this.rbParamDictionary.Location = new System.Drawing.Point(635, 75);
            this.rbParamDictionary.Name = "rbParamDictionary";
            this.rbParamDictionary.Size = new System.Drawing.Size(194, 21);
            this.rbParamDictionary.TabIndex = 25;
            this.rbParamDictionary.Text = "Parameters Dictionary";
            this.rbParamDictionary.UseVisualStyleBackColor = true;
            this.rbParamDictionary.CheckedChanged += new System.EventHandler(this.rbParamDictionary_CheckedChanged);
            // 
            // rbUpdateCol
            // 
            this.rbUpdateCol.AutoSize = true;
            this.rbUpdateCol.Location = new System.Drawing.Point(459, 75);
            this.rbUpdateCol.Name = "rbUpdateCol";
            this.rbUpdateCol.Size = new System.Drawing.Size(170, 21);
            this.rbUpdateCol.TabIndex = 24;
            this.rbUpdateCol.Text = "Update Column List";
            this.rbUpdateCol.UseVisualStyleBackColor = true;
            this.rbUpdateCol.CheckedChanged += new System.EventHandler(this.rbUpdateCol_CheckedChanged);
            // 
            // rbCreateTableSql
            // 
            this.rbCreateTableSql.AutoSize = true;
            this.rbCreateTableSql.Location = new System.Drawing.Point(299, 75);
            this.rbCreateTableSql.Name = "rbCreateTableSql";
            this.rbCreateTableSql.Size = new System.Drawing.Size(154, 21);
            this.rbCreateTableSql.TabIndex = 23;
            this.rbCreateTableSql.Text = "Create Table SQL";
            this.rbCreateTableSql.UseVisualStyleBackColor = true;
            this.rbCreateTableSql.CheckedChanged += new System.EventHandler(this.rbCreateTableSql_CheckedChanged);
            // 
            // rbDictionary
            // 
            this.rbDictionary.AutoSize = true;
            this.rbDictionary.Location = new System.Drawing.Point(187, 75);
            this.rbDictionary.Name = "rbDictionary";
            this.rbDictionary.Size = new System.Drawing.Size(106, 21);
            this.rbDictionary.TabIndex = 22;
            this.rbDictionary.Text = "Dictionary";
            this.rbDictionary.UseVisualStyleBackColor = true;
            this.rbDictionary.CheckedChanged += new System.EventHandler(this.rbDictionary_CheckedChanged);
            // 
            // rbClass
            // 
            this.rbClass.AutoSize = true;
            this.rbClass.Checked = true;
            this.rbClass.Location = new System.Drawing.Point(115, 75);
            this.rbClass.Name = "rbClass";
            this.rbClass.Size = new System.Drawing.Size(66, 21);
            this.rbClass.TabIndex = 21;
            this.rbClass.TabStop = true;
            this.rbClass.Text = "Class";
            this.rbClass.UseVisualStyleBackColor = true;
            this.rbClass.CheckedChanged += new System.EventHandler(this.rbClass_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 108);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 17);
            this.label6.TabIndex = 19;
            this.label6.Text = "Field Type:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(375, 145);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(256, 17);
            this.label4.TabIndex = 15;
            this.label4.Text = "Click table\'s name to generate.";
            // 
            // cbFieldType
            // 
            this.cbFieldType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFieldType.FormattingEnabled = true;
            this.cbFieldType.Items.AddRange(new object[] {
            "fields + properties",
            "public properties",
            "public fields"});
            this.cbFieldType.Location = new System.Drawing.Point(105, 105);
            this.cbFieldType.Name = "cbFieldType";
            this.cbFieldType.Size = new System.Drawing.Size(248, 25);
            this.cbFieldType.TabIndex = 20;
            this.cbFieldType.SelectedIndexChanged += new System.EventHandler(this.cbFieldType_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 17);
            this.label2.TabIndex = 17;
            this.label2.Text = "Output Type:";
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
            this.btGenerateCustomSqlObject.Text = "Generate Class From Custom SQL";
            this.btGenerateCustomSqlObject.UseVisualStyleBackColor = true;
            this.btGenerateCustomSqlObject.Click += new System.EventHandler(this.btGenerateCustomSqlObject_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(305, 145);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 17);
            this.label5.TabIndex = 16;
            this.label5.Text = "Output:";
            // 
            // btLoadTableList
            // 
            this.btLoadTableList.Location = new System.Drawing.Point(3, 139);
            this.btLoadTableList.Name = "btLoadTableList";
            this.btLoadTableList.Size = new System.Drawing.Size(141, 28);
            this.btLoadTableList.TabIndex = 14;
            this.btLoadTableList.Text = "Refresh Tables";
            this.btLoadTableList.UseVisualStyleBackColor = true;
            this.btLoadTableList.Click += new System.EventHandler(this.btLoadTableList_Click);
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 17;
            this.listBox1.Location = new System.Drawing.Point(5, 181);
            this.listBox1.Margin = new System.Windows.Forms.Padding(0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(308, 453);
            this.listBox1.TabIndex = 13;
            this.listBox1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseClick);
            this.listBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDoubleClick);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(227)))));
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(313, 181);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(946, 453);
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
            // toolTip1
            // 
            this.toolTip1.BackColor = System.Drawing.Color.LightGreen;
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ToolTipTitle = "Text Auto Copied";
            // 
            // btConvertPrivateFieldsToPublicProperties
            // 
            this.btConvertPrivateFieldsToPublicProperties.Location = new System.Drawing.Point(637, 139);
            this.btConvertPrivateFieldsToPublicProperties.Name = "btConvertPrivateFieldsToPublicProperties";
            this.btConvertPrivateFieldsToPublicProperties.Size = new System.Drawing.Size(372, 28);
            this.btConvertPrivateFieldsToPublicProperties.TabIndex = 26;
            this.btConvertPrivateFieldsToPublicProperties.Text = "Convert Private Fields to Public Properties";
            this.btConvertPrivateFieldsToPublicProperties.UseVisualStyleBackColor = true;
            this.btConvertPrivateFieldsToPublicProperties.Click += new System.EventHandler(this.btConvertPrivateFieldsToPublicProperties_Click);
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
            this.Text = "MySqlExpress Helper";
            this.ResizeEnd += new System.EventHandler(this.Form1_ResizeEnd);
            this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btLoadTableList;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btGenerateCustomSqlObject;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbFieldType;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton rbParamDictionary;
        private System.Windows.Forms.RadioButton rbUpdateCol;
        private System.Windows.Forms.RadioButton rbCreateTableSql;
        private System.Windows.Forms.RadioButton rbDictionary;
        private System.Windows.Forms.RadioButton rbClass;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btConvertPrivateFieldsToPublicProperties;
    }
}

