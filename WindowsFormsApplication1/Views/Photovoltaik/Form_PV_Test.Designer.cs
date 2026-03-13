namespace WindowsFormsApplication1
{
    partial class Main_PV_Test
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._splitContainer = new System.Windows.Forms.SplitContainer();
            this._dgvModules = new System.Windows.Forms.DataGridView();
            this._detailsPanel = new System.Windows.Forms.Panel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_Uebersicht = new System.Windows.Forms.TabPage();
            this.tabPage_Elektrisch = new System.Windows.Forms.TabPage();
            this.tabPage_Thermisch = new System.Windows.Forms.TabPage();
            this._bottomPanel = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSelect = new System.Windows.Forms.Button();
            this._cecloadPanel = new System.Windows.Forms.Panel();
            this._btnCEC = new System.Windows.Forms.Button();
            this._filterPanel = new System.Windows.Forms.Panel();
            this._lblSearch = new System.Windows.Forms.Label();
            this._txtSearch = new System.Windows.Forms.TextBox();
            this._btnFilter = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._headerPanel = new WindowsFormsApplication1.HeaderGradientPanel();
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).BeginInit();
            this._splitContainer.Panel1.SuspendLayout();
            this._splitContainer.Panel2.SuspendLayout();
            this._splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvModules)).BeginInit();
            this._detailsPanel.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this._bottomPanel.SuspendLayout();
            this._cecloadPanel.SuspendLayout();
            this._filterPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _splitContainer
            // 
            this._splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._splitContainer.Location = new System.Drawing.Point(0, 154);
            this._splitContainer.Name = "_splitContainer";
            // 
            // _splitContainer.Panel1
            // 
            this._splitContainer.Panel1.Controls.Add(this._dgvModules);
            // 
            // _splitContainer.Panel2
            // 
            this._splitContainer.Panel2.Controls.Add(this._detailsPanel);
            this._splitContainer.Size = new System.Drawing.Size(1100, 508);
            this._splitContainer.SplitterDistance = 366;
            this._splitContainer.SplitterWidth = 6;
            this._splitContainer.TabIndex = 0;
            // 
            // _dgvModules
            // 
            this._dgvModules.AllowUserToAddRows = false;
            this._dgvModules.BackgroundColor = System.Drawing.Color.White;
            this._dgvModules.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._dgvModules.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dgvModules.Location = new System.Drawing.Point(0, 0);
            this._dgvModules.Name = "_dgvModules";
            this._dgvModules.ReadOnly = true;
            this._dgvModules.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgvModules.Size = new System.Drawing.Size(366, 508);
            this._dgvModules.TabIndex = 0;
            // 
            // _detailsPanel
            // 
            this._detailsPanel.AutoScroll = true;
            this._detailsPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this._detailsPanel.Controls.Add(this.tabControl1);
            this._detailsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailsPanel.Location = new System.Drawing.Point(0, 0);
            this._detailsPanel.Margin = new System.Windows.Forms.Padding(0);
            this._detailsPanel.Name = "_detailsPanel";
            this._detailsPanel.Size = new System.Drawing.Size(728, 508);
            this._detailsPanel.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_Uebersicht);
            this.tabControl1.Controls.Add(this.tabPage_Elektrisch);
            this.tabControl1.Controls.Add(this.tabPage_Thermisch);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.Padding = new System.Drawing.Point(0, 0);
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(728, 508);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage_Uebersicht
            // 
            this.tabPage_Uebersicht.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabPage_Uebersicht.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Uebersicht.Name = "tabPage_Uebersicht";
            this.tabPage_Uebersicht.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Uebersicht.Size = new System.Drawing.Size(720, 482);
            this.tabPage_Uebersicht.TabIndex = 0;
            this.tabPage_Uebersicht.Text = "Üebersicht";
            this.tabPage_Uebersicht.UseVisualStyleBackColor = true;
            // 
            // tabPage_Elektrisch
            // 
            this.tabPage_Elektrisch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.tabPage_Elektrisch.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Elektrisch.Name = "tabPage_Elektrisch";
            this.tabPage_Elektrisch.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Elektrisch.Size = new System.Drawing.Size(720, 482);
            this.tabPage_Elektrisch.TabIndex = 1;
            this.tabPage_Elektrisch.Text = "Elektrisch";
            this.tabPage_Elektrisch.UseVisualStyleBackColor = true;
            // 
            // tabPage_Thermisch
            // 
            this.tabPage_Thermisch.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.tabPage_Thermisch.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Thermisch.Name = "tabPage_Thermisch";
            this.tabPage_Thermisch.Size = new System.Drawing.Size(720, 482);
            this.tabPage_Thermisch.TabIndex = 2;
            this.tabPage_Thermisch.Text = "Thermisch";
            this.tabPage_Thermisch.UseVisualStyleBackColor = true;
            // 
            // _bottomPanel
            // 
            this._bottomPanel.BackColor = System.Drawing.Color.White;
            this._bottomPanel.Controls.Add(this.btnCancel);
            this._bottomPanel.Controls.Add(this.btnSelect);
            this._bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._bottomPanel.Location = new System.Drawing.Point(0, 662);
            this._bottomPanel.Name = "_bottomPanel";
            this._bottomPanel.Size = new System.Drawing.Size(1100, 38);
            this._bottomPanel.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.Black;
            this.btnCancel.Location = new System.Drawing.Point(765, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(121, 27);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "❌";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSelect
            // 
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelect.BackColor = System.Drawing.Color.MediumSeaGreen;
            this.btnSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelect.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSelect.ForeColor = System.Drawing.Color.White;
            this.btnSelect.Location = new System.Drawing.Point(892, 5);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(189, 29);
            this.btnSelect.TabIndex = 2;
            this.btnSelect.Text = "✔ Auswahl übernehmen";
            this.btnSelect.UseVisualStyleBackColor = false;
            // 
            // _cecloadPanel
            // 
            this._cecloadPanel.Controls.Add(this._btnCEC);
            this._cecloadPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._cecloadPanel.Location = new System.Drawing.Point(0, 60);
            this._cecloadPanel.Name = "_cecloadPanel";
            this._cecloadPanel.Size = new System.Drawing.Size(1100, 44);
            this._cecloadPanel.TabIndex = 1;
            // 
            // _btnCEC
            // 
            this._btnCEC.BackColor = System.Drawing.SystemColors.Highlight;
            this._btnCEC.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCEC.ForeColor = System.Drawing.Color.White;
            this._btnCEC.Location = new System.Drawing.Point(14, 8);
            this._btnCEC.Name = "_btnCEC";
            this._btnCEC.Size = new System.Drawing.Size(100, 28);
            this._btnCEC.TabIndex = 3;
            this._btnCEC.Text = "🌐 CEC laden";
            this._btnCEC.UseVisualStyleBackColor = false;
            this._btnCEC.Click += new System.EventHandler(this._btnCEC_Click);
            // 
            // _filterPanel
            // 
            this._filterPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._filterPanel.Controls.Add(this._lblSearch);
            this._filterPanel.Controls.Add(this._txtSearch);
            this._filterPanel.Controls.Add(this._btnFilter);
            this._filterPanel.Controls.Add(this._btnReset);
            this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._filterPanel.ForeColor = System.Drawing.Color.White;
            this._filterPanel.Location = new System.Drawing.Point(0, 104);
            this._filterPanel.Name = "_filterPanel";
            this._filterPanel.Size = new System.Drawing.Size(1100, 50);
            this._filterPanel.TabIndex = 1;
            // 
            // _lblSearch
            // 
            this._lblSearch.Location = new System.Drawing.Point(10, 17);
            this._lblSearch.Name = "_lblSearch";
            this._lblSearch.Size = new System.Drawing.Size(85, 18);
            this._lblSearch.TabIndex = 0;
            this._lblSearch.Text = "🔍 Modulname:";
            // 
            // _txtSearch
            // 
            this._txtSearch.Location = new System.Drawing.Point(101, 15);
            this._txtSearch.Name = "_txtSearch";
            this._txtSearch.Size = new System.Drawing.Size(250, 20);
            this._txtSearch.TabIndex = 1;
            // 
            // _btnFilter
            // 
            this._btnFilter.BackColor = System.Drawing.SystemColors.Highlight;
            this._btnFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnFilter.Location = new System.Drawing.Point(357, 11);
            this._btnFilter.Name = "_btnFilter";
            this._btnFilter.Size = new System.Drawing.Size(75, 26);
            this._btnFilter.TabIndex = 2;
            this._btnFilter.Text = "🔍 Suchen";
            this._btnFilter.UseVisualStyleBackColor = false;
            this._btnFilter.Click += new System.EventHandler(this._btnFilter_Click);
            // 
            // _btnReset
            // 
            this._btnReset.BackColor = System.Drawing.Color.Firebrick;
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.Location = new System.Drawing.Point(449, 11);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(106, 26);
            this._btnReset.TabIndex = 3;
            this._btnReset.Text = "✖ Zurücksetzen";
            this._btnReset.UseVisualStyleBackColor = false;
            this._btnReset.Click += new System.EventHandler(this._btnReset_Click);
            // 
            // _headerPanel
            // 
            this._headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._headerPanel.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this._headerPanel.ForeColor = System.Drawing.Color.White;
            this._headerPanel.Location = new System.Drawing.Point(0, 0);
            this._headerPanel.Name = "_headerPanel";
            this._headerPanel.Size = new System.Drawing.Size(1100, 60);
            this._headerPanel.TabIndex = 2;
            this._headerPanel.Text = "PV-Modul Import - CEC";
            // 
            // Main_PV_Test
            // 
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Controls.Add(this._splitContainer);
            this.Controls.Add(this._filterPanel);
            this.Controls.Add(this._cecloadPanel);
            this.Controls.Add(this._headerPanel);
            this.Controls.Add(this._bottomPanel);
            this.Name = "Main_PV_Test";
            this.Text = "PV-Import";
            this._splitContainer.Panel1.ResumeLayout(false);
            this._splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._splitContainer)).EndInit();
            this._splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._dgvModules)).EndInit();
            this._detailsPanel.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this._bottomPanel.ResumeLayout(false);
            this._cecloadPanel.ResumeLayout(false);
            this._filterPanel.ResumeLayout(false);
            this._filterPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.SplitContainer _splitContainer;
        private System.Windows.Forms.DataGridView _dgvModules;
        private System.Windows.Forms.Panel _detailsPanel;
        private System.Windows.Forms.Panel _filterPanel;
        private System.Windows.Forms.Panel _cecloadPanel;
        private System.Windows.Forms.TextBox _txtSearch;
        private System.Windows.Forms.Button _btnFilter;
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Button _btnCEC;
        private System.Windows.Forms.Label _lblSearch;
        private HeaderGradientPanel _headerPanel;
        private System.Windows.Forms.Panel _bottomPanel;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage_Uebersicht;
        private System.Windows.Forms.TabPage tabPage_Elektrisch;
        private System.Windows.Forms.TabPage tabPage_Thermisch;
    }
}