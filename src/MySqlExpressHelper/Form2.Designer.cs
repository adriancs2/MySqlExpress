namespace MySqlExpressHelper
{
    partial class Form2
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblConnStr = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.btRefreshTables = new System.Windows.Forms.Button();
            this.lblSql = new System.Windows.Forms.Label();
            this.txtSQL = new System.Windows.Forms.TextBox();
            this.btGenerateFromSql = new System.Windows.Forms.Button();
            this.pnlSettings = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btConvertToProps = new System.Windows.Forms.Button();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFieldType = new System.Windows.Forms.Label();
            this.cbFieldType = new System.Windows.Forms.ComboBox();
            this.chkAutoCopy = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblOutputType = new System.Windows.Forms.Label();
            this.rbClass = new System.Windows.Forms.RadioButton();
            this.rbDictionary = new System.Windows.Forms.RadioButton();
            this.rbCreateTableSql = new System.Windows.Forms.RadioButton();
            this.rbUpdateCol = new System.Windows.Forms.RadioButton();
            this.rbParamDictionary = new System.Windows.Forms.RadioButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.pnlOutput = new System.Windows.Forms.Panel();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.lblMethod = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.pnlTop.SuspendLayout();
            this.pnlSettings.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlOutput.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.lblConnStr);
            this.pnlTop.Controls.Add(this.txtConnStr);
            this.pnlTop.Controls.Add(this.btRefreshTables);
            this.pnlTop.Controls.Add(this.lblSql);
            this.pnlTop.Controls.Add(this.txtSQL);
            this.pnlTop.Controls.Add(this.btGenerateFromSql);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(5, 5);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1105, 78);
            this.pnlTop.TabIndex = 0;
            // 
            // lblConnStr
            // 
            this.lblConnStr.AutoSize = true;
            this.lblConnStr.Location = new System.Drawing.Point(5, 9);
            this.lblConnStr.Name = "lblConnStr";
            this.lblConnStr.Size = new System.Drawing.Size(144, 17);
            this.lblConnStr.TabIndex = 0;
            this.lblConnStr.Text = "MySQL Connection:";
            // 
            // txtConnStr
            // 
            this.txtConnStr.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnStr.Location = new System.Drawing.Point(155, 8);
            this.txtConnStr.Name = "txtConnStr";
            this.txtConnStr.Size = new System.Drawing.Size(777, 23);
            this.txtConnStr.TabIndex = 0;
            this.txtConnStr.TextChanged += new System.EventHandler(this.txtConnStr_TextChanged);
            this.txtConnStr.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtConnStr_KeyDown);
            // 
            // btRefreshTables
            // 
            this.btRefreshTables.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btRefreshTables.Location = new System.Drawing.Point(938, 6);
            this.btRefreshTables.Name = "btRefreshTables";
            this.btRefreshTables.Size = new System.Drawing.Size(162, 26);
            this.btRefreshTables.TabIndex = 1;
            this.btRefreshTables.Text = "Refresh Tables";
            this.btRefreshTables.UseVisualStyleBackColor = true;
            this.btRefreshTables.Click += new System.EventHandler(this.btRefreshTables_Click);
            // 
            // lblSql
            // 
            this.lblSql.AutoSize = true;
            this.lblSql.Location = new System.Drawing.Point(5, 43);
            this.lblSql.Name = "lblSql";
            this.lblSql.Size = new System.Drawing.Size(120, 17);
            this.lblSql.TabIndex = 2;
            this.lblSql.Text = "Custom SELECT:";
            // 
            // txtSQL
            // 
            this.txtSQL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSQL.Location = new System.Drawing.Point(155, 42);
            this.txtSQL.Name = "txtSQL";
            this.txtSQL.Size = new System.Drawing.Size(777, 23);
            this.txtSQL.TabIndex = 2;
            this.txtSQL.TextChanged += new System.EventHandler(this.txtSQL_TextChanged);
            this.txtSQL.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSQL_KeyDown);
            // 
            // btGenerateFromSql
            // 
            this.btGenerateFromSql.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btGenerateFromSql.Location = new System.Drawing.Point(938, 40);
            this.btGenerateFromSql.Name = "btGenerateFromSql";
            this.btGenerateFromSql.Size = new System.Drawing.Size(162, 26);
            this.btGenerateFromSql.TabIndex = 3;
            this.btGenerateFromSql.Text = "Generate from SQL";
            this.btGenerateFromSql.UseVisualStyleBackColor = true;
            this.btGenerateFromSql.Click += new System.EventHandler(this.btGenerateFromSql_Click);
            // 
            // pnlSettings
            // 
            this.pnlSettings.Controls.Add(this.tableLayoutPanel1);
            this.pnlSettings.Controls.Add(this.flowLayoutPanel1);
            this.pnlSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSettings.Location = new System.Drawing.Point(5, 83);
            this.pnlSettings.Name = "pnlSettings";
            this.pnlSettings.Size = new System.Drawing.Size(1105, 71);
            this.pnlSettings.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 336F));
            this.tableLayoutPanel1.Controls.Add(this.btConvertToProps, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel3, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 29);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1105, 38);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // btConvertToProps
            // 
            this.btConvertToProps.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btConvertToProps.Location = new System.Drawing.Point(782, 5);
            this.btConvertToProps.Name = "btConvertToProps";
            this.btConvertToProps.Size = new System.Drawing.Size(310, 28);
            this.btConvertToProps.TabIndex = 7;
            this.btConvertToProps.Text = "Convert Private Fields to Public Properties";
            this.btConvertToProps.UseVisualStyleBackColor = true;
            this.btConvertToProps.Click += new System.EventHandler(this.btConvertToProps_Click);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.Controls.Add(this.lblFieldType);
            this.flowLayoutPanel3.Controls.Add(this.cbFieldType);
            this.flowLayoutPanel3.Controls.Add(this.chkAutoCopy);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(763, 32);
            this.flowLayoutPanel3.TabIndex = 8;
            // 
            // lblFieldType
            // 
            this.lblFieldType.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblFieldType.Location = new System.Drawing.Point(3, 7);
            this.lblFieldType.Name = "lblFieldType";
            this.lblFieldType.Size = new System.Drawing.Size(100, 17);
            this.lblFieldType.TabIndex = 5;
            this.lblFieldType.Text = "Field Type:";
            // 
            // cbFieldType
            // 
            this.cbFieldType.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cbFieldType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFieldType.FormattingEnabled = true;
            this.cbFieldType.Items.AddRange(new object[] {
            "private fields + public properties",
            "public properties only",
            "public fields only"});
            this.cbFieldType.Location = new System.Drawing.Point(109, 3);
            this.cbFieldType.Name = "cbFieldType";
            this.cbFieldType.Size = new System.Drawing.Size(325, 25);
            this.cbFieldType.TabIndex = 5;
            this.cbFieldType.SelectedIndexChanged += new System.EventHandler(this.cbFieldType_SelectedIndexChanged);
            // 
            // chkAutoCopy
            // 
            this.chkAutoCopy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkAutoCopy.AutoSize = true;
            this.chkAutoCopy.Location = new System.Drawing.Point(440, 5);
            this.chkAutoCopy.Name = "chkAutoCopy";
            this.chkAutoCopy.Size = new System.Drawing.Size(219, 21);
            this.chkAutoCopy.TabIndex = 6;
            this.chkAutoCopy.Text = "Auto-copy generated text";
            this.chkAutoCopy.UseVisualStyleBackColor = true;
            this.chkAutoCopy.CheckedChanged += new System.EventHandler(this.chkAutoCopy_CheckedChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.lblOutputType);
            this.flowLayoutPanel1.Controls.Add(this.rbClass);
            this.flowLayoutPanel1.Controls.Add(this.rbDictionary);
            this.flowLayoutPanel1.Controls.Add(this.rbCreateTableSql);
            this.flowLayoutPanel1.Controls.Add(this.rbUpdateCol);
            this.flowLayoutPanel1.Controls.Add(this.rbParamDictionary);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1105, 29);
            this.flowLayoutPanel1.TabIndex = 8;
            // 
            // lblOutputType
            // 
            this.lblOutputType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutputType.Location = new System.Drawing.Point(3, 5);
            this.lblOutputType.Name = "lblOutputType";
            this.lblOutputType.Size = new System.Drawing.Size(100, 17);
            this.lblOutputType.TabIndex = 0;
            this.lblOutputType.Text = "Output Type:";
            // 
            // rbClass
            // 
            this.rbClass.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbClass.AutoSize = true;
            this.rbClass.Checked = true;
            this.rbClass.Location = new System.Drawing.Point(109, 3);
            this.rbClass.Name = "rbClass";
            this.rbClass.Size = new System.Drawing.Size(66, 21);
            this.rbClass.TabIndex = 0;
            this.rbClass.TabStop = true;
            this.rbClass.Text = "Class";
            this.rbClass.UseVisualStyleBackColor = true;
            this.rbClass.CheckedChanged += new System.EventHandler(this.OutputType_CheckedChanged);
            // 
            // rbDictionary
            // 
            this.rbDictionary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbDictionary.AutoSize = true;
            this.rbDictionary.Location = new System.Drawing.Point(181, 3);
            this.rbDictionary.Name = "rbDictionary";
            this.rbDictionary.Size = new System.Drawing.Size(106, 21);
            this.rbDictionary.TabIndex = 1;
            this.rbDictionary.Text = "Dictionary";
            this.rbDictionary.UseVisualStyleBackColor = true;
            this.rbDictionary.CheckedChanged += new System.EventHandler(this.OutputType_CheckedChanged);
            // 
            // rbCreateTableSql
            // 
            this.rbCreateTableSql.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbCreateTableSql.AutoSize = true;
            this.rbCreateTableSql.Location = new System.Drawing.Point(293, 3);
            this.rbCreateTableSql.Name = "rbCreateTableSql";
            this.rbCreateTableSql.Size = new System.Drawing.Size(154, 21);
            this.rbCreateTableSql.TabIndex = 2;
            this.rbCreateTableSql.Text = "Create Table SQL";
            this.rbCreateTableSql.UseVisualStyleBackColor = true;
            this.rbCreateTableSql.CheckedChanged += new System.EventHandler(this.OutputType_CheckedChanged);
            // 
            // rbUpdateCol
            // 
            this.rbUpdateCol.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbUpdateCol.AutoSize = true;
            this.rbUpdateCol.Location = new System.Drawing.Point(453, 3);
            this.rbUpdateCol.Name = "rbUpdateCol";
            this.rbUpdateCol.Size = new System.Drawing.Size(170, 21);
            this.rbUpdateCol.TabIndex = 3;
            this.rbUpdateCol.Text = "Update Column List";
            this.rbUpdateCol.UseVisualStyleBackColor = true;
            this.rbUpdateCol.CheckedChanged += new System.EventHandler(this.OutputType_CheckedChanged);
            // 
            // rbParamDictionary
            // 
            this.rbParamDictionary.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbParamDictionary.AutoSize = true;
            this.rbParamDictionary.Location = new System.Drawing.Point(629, 3);
            this.rbParamDictionary.Name = "rbParamDictionary";
            this.rbParamDictionary.Size = new System.Drawing.Size(194, 21);
            this.rbParamDictionary.TabIndex = 4;
            this.rbParamDictionary.Text = "Parameters Dictionary";
            this.rbParamDictionary.UseVisualStyleBackColor = true;
            this.rbParamDictionary.CheckedChanged += new System.EventHandler(this.OutputType_CheckedChanged);
            // 
            // splitMain
            // 
            this.splitMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(5, 154);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.listBox1);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.pnlOutput);
            this.splitMain.Size = new System.Drawing.Size(1105, 500);
            this.splitMain.SplitterDistance = 251;
            this.splitMain.TabIndex = 2;
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 17;
            this.listBox1.Location = new System.Drawing.Point(0, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(249, 498);
            this.listBox1.TabIndex = 0;
            this.listBox1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseClick);
            this.listBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listBox1_MouseDoubleClick);
            // 
            // pnlOutput
            // 
            this.pnlOutput.Controls.Add(this.richTextBox1);
            this.pnlOutput.Controls.Add(this.lblMethod);
            this.pnlOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOutput.Location = new System.Drawing.Point(0, 0);
            this.pnlOutput.Name = "pnlOutput";
            this.pnlOutput.Size = new System.Drawing.Size(848, 498);
            this.pnlOutput.TabIndex = 0;
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(227)))));
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(0, 24);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(848, 474);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            this.richTextBox1.WordWrap = false;
            // 
            // lblMethod
            // 
            this.lblMethod.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(220)))));
            this.lblMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMethod.Font = new System.Drawing.Font("Cascadia Code", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMethod.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblMethod.Location = new System.Drawing.Point(0, 0);
            this.lblMethod.Name = "lblMethod";
            this.lblMethod.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.lblMethod.Size = new System.Drawing.Size(848, 24);
            this.lblMethod.TabIndex = 1;
            this.lblMethod.Text = "Click a table name or use Custom SQL to generate.";
            this.lblMethod.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(5, 654);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1105, 22);
            this.statusStrip1.TabIndex = 3;
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
            // Form2
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1115, 681);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.pnlSettings);
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Cascadia Code SemiBold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form2";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MySqlExpress Helper";
            this.Load += new System.EventHandler(this.Form2_Load);
            this.ResizeEnd += new System.EventHandler(this.Form2_ResizeEnd);
            this.SizeChanged += new System.EventHandler(this.Form2_SizeChanged);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlSettings.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlOutput.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblConnStr;
        private System.Windows.Forms.TextBox txtConnStr;
        private System.Windows.Forms.Button btRefreshTables;
        private System.Windows.Forms.Label lblSql;
        private System.Windows.Forms.TextBox txtSQL;
        private System.Windows.Forms.Button btGenerateFromSql;

        private System.Windows.Forms.Panel pnlSettings;
        private System.Windows.Forms.Label lblOutputType;
        private System.Windows.Forms.RadioButton rbClass;
        private System.Windows.Forms.RadioButton rbDictionary;
        private System.Windows.Forms.RadioButton rbCreateTableSql;
        private System.Windows.Forms.RadioButton rbUpdateCol;
        private System.Windows.Forms.RadioButton rbParamDictionary;
        private System.Windows.Forms.Label lblFieldType;
        private System.Windows.Forms.ComboBox cbFieldType;
        private System.Windows.Forms.CheckBox chkAutoCopy;
        private System.Windows.Forms.Button btConvertToProps;

        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Panel pnlOutput;
        private System.Windows.Forms.Label lblMethod;
        private System.Windows.Forms.RichTextBox richTextBox1;

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
    }
}