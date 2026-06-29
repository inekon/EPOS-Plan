namespace WindowsFormsApplication1
{
    partial class Form_WpFilterAuswahl
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            dgv = new System.Windows.Forms.DataGridView();
            filterPanel = new System.Windows.Forms.FlowLayoutPanel();
            pnlHersteller = new System.Windows.Forms.Panel();
            cbHersteller = new System.Windows.Forms.ComboBox();
            lblHersteller = new System.Windows.Forms.Label();
            pnlAuslegung = new System.Windows.Forms.Panel();
            cbAuslegung = new System.Windows.Forms.ComboBox();
            lblAuslegung = new System.Windows.Forms.Label();
            pnlPrinzip = new System.Windows.Forms.Panel();
            cbPrinzip = new System.Windows.Forms.ComboBox();
            lblPrinzip = new System.Windows.Forms.Label();
            pnlRegelung = new System.Windows.Forms.Panel();
            cbRegelung = new System.Windows.Forms.ComboBox();
            lblRegelung = new System.Windows.Forms.Label();
            pnlBauart = new System.Windows.Forms.Panel();
            cbBauart = new System.Windows.Forms.ComboBox();
            lblBauart = new System.Windows.Forms.Label();
            pnlAufstellung = new System.Windows.Forms.Panel();
            cbAufstellung = new System.Windows.Forms.ComboBox();
            lblAufstellung = new System.Windows.Forms.Label();
            pnlZuheizun = new System.Windows.Forms.Panel();
            cbZuheizung = new System.Windows.Forms.ComboBox();
            lblZuheizung = new System.Windows.Forms.Label();
            pnlTempMin = new System.Windows.Forms.Panel();
            numTempMin = new System.Windows.Forms.NumericUpDown();
            lblTempMin = new System.Windows.Forms.Label();
            pnlTempMax = new System.Windows.Forms.Panel();
            numTempMax = new System.Windows.Forms.NumericUpDown();
            lblTempMax = new System.Windows.Forms.Label();
            pnlLeistungMin = new System.Windows.Forms.Panel();
            numLeistungMin = new System.Windows.Forms.NumericUpDown();
            lblLeistungMin = new System.Windows.Forms.Label();
            pnlLeistungMax = new System.Windows.Forms.Panel();
            numLeistungMax = new System.Windows.Forms.NumericUpDown();
            lblLeistungMax = new System.Windows.Forms.Label();
            filterBezeichnungPanel = new System.Windows.Forms.TableLayoutPanel();
            label1 = new System.Windows.Forms.Label();
            txtSucheBezeichnung = new System.Windows.Forms.TextBox();
            pnlFilterbtn = new System.Windows.Forms.Panel();
            btn_Reset = new System.Windows.Forms.Button();
            btnFilter = new System.Windows.Forms.Button();
            bottomPanel = new System.Windows.Forms.Panel();
            btnCancel = new System.Windows.Forms.Button();
            btnSelect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            filterPanel.SuspendLayout();
            pnlHersteller.SuspendLayout();
            pnlAuslegung.SuspendLayout();
            pnlPrinzip.SuspendLayout();
            pnlRegelung.SuspendLayout();
            pnlBauart.SuspendLayout();
            pnlAufstellung.SuspendLayout();
            pnlZuheizun.SuspendLayout();
            pnlTempMin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTempMin).BeginInit();
            pnlTempMax.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTempMax).BeginInit();
            pnlLeistungMin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numLeistungMin).BeginInit();
            pnlLeistungMax.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numLeistungMax).BeginInit();
            filterBezeichnungPanel.SuspendLayout();
            pnlFilterbtn.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 160);
            dgv.Name = "dgv";
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.Size = new System.Drawing.Size(1181, 576);
            dgv.TabIndex = 0;
            // 
            // filterPanel
            // 
            filterPanel.AutoScroll = true;
            filterPanel.BackColor = System.Drawing.Color.FromArgb(220, 225, 230);
            filterPanel.Controls.Add(pnlHersteller);
            filterPanel.Controls.Add(pnlAuslegung);
            filterPanel.Controls.Add(pnlPrinzip);
            filterPanel.Controls.Add(pnlRegelung);
            filterPanel.Controls.Add(pnlBauart);
            filterPanel.Controls.Add(pnlAufstellung);
            filterPanel.Controls.Add(pnlZuheizun);
            filterPanel.Controls.Add(pnlTempMin);
            filterPanel.Controls.Add(pnlTempMax);
            filterPanel.Controls.Add(pnlLeistungMin);
            filterPanel.Controls.Add(pnlLeistungMax);
            filterPanel.Controls.Add(filterBezeichnungPanel);
            filterPanel.Controls.Add(pnlFilterbtn);
            filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            filterPanel.Location = new System.Drawing.Point(0, 0);
            filterPanel.Name = "filterPanel";
            filterPanel.Padding = new System.Windows.Forms.Padding(15);
            filterPanel.Size = new System.Drawing.Size(1181, 160);
            filterPanel.TabIndex = 1;
            // 
            // pnlHersteller
            // 
            pnlHersteller.Controls.Add(cbHersteller);
            pnlHersteller.Controls.Add(lblHersteller);
            pnlHersteller.Location = new System.Drawing.Point(20, 20);
            pnlHersteller.Margin = new System.Windows.Forms.Padding(5);
            pnlHersteller.Name = "pnlHersteller";
            pnlHersteller.Size = new System.Drawing.Size(160, 55);
            pnlHersteller.TabIndex = 0;
            // 
            // cbHersteller
            // 
            cbHersteller.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbHersteller.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbHersteller.Location = new System.Drawing.Point(0, 32);
            cbHersteller.Name = "cbHersteller";
            cbHersteller.Size = new System.Drawing.Size(160, 23);
            cbHersteller.TabIndex = 0;
            // 
            // lblHersteller
            // 
            lblHersteller.Dock = System.Windows.Forms.DockStyle.Top;
            lblHersteller.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblHersteller.Location = new System.Drawing.Point(0, 0);
            lblHersteller.Name = "lblHersteller";
            lblHersteller.Size = new System.Drawing.Size(160, 23);
            lblHersteller.TabIndex = 1;
            lblHersteller.Text = "Hersteller";
            // 
            // pnlAuslegung
            // 
            pnlAuslegung.Controls.Add(cbAuslegung);
            pnlAuslegung.Controls.Add(lblAuslegung);
            pnlAuslegung.Location = new System.Drawing.Point(190, 20);
            pnlAuslegung.Margin = new System.Windows.Forms.Padding(5);
            pnlAuslegung.Name = "pnlAuslegung";
            pnlAuslegung.Size = new System.Drawing.Size(160, 55);
            pnlAuslegung.TabIndex = 1;
            // 
            // cbAuslegung
            // 
            cbAuslegung.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbAuslegung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbAuslegung.Location = new System.Drawing.Point(0, 32);
            cbAuslegung.Name = "cbAuslegung";
            cbAuslegung.Size = new System.Drawing.Size(160, 23);
            cbAuslegung.TabIndex = 0;
            // 
            // lblAuslegung
            // 
            lblAuslegung.Dock = System.Windows.Forms.DockStyle.Top;
            lblAuslegung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblAuslegung.Location = new System.Drawing.Point(0, 0);
            lblAuslegung.Name = "lblAuslegung";
            lblAuslegung.Size = new System.Drawing.Size(160, 23);
            lblAuslegung.TabIndex = 1;
            lblAuslegung.Text = "Auslegung";
            // 
            // pnlPrinzip
            // 
            pnlPrinzip.Controls.Add(cbPrinzip);
            pnlPrinzip.Controls.Add(lblPrinzip);
            pnlPrinzip.Location = new System.Drawing.Point(360, 20);
            pnlPrinzip.Margin = new System.Windows.Forms.Padding(5);
            pnlPrinzip.Name = "pnlPrinzip";
            pnlPrinzip.Size = new System.Drawing.Size(160, 55);
            pnlPrinzip.TabIndex = 2;
            // 
            // cbPrinzip
            // 
            cbPrinzip.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbPrinzip.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbPrinzip.Location = new System.Drawing.Point(0, 32);
            cbPrinzip.Name = "cbPrinzip";
            cbPrinzip.Size = new System.Drawing.Size(160, 23);
            cbPrinzip.TabIndex = 0;
            // 
            // lblPrinzip
            // 
            lblPrinzip.Dock = System.Windows.Forms.DockStyle.Top;
            lblPrinzip.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblPrinzip.Location = new System.Drawing.Point(0, 0);
            lblPrinzip.Name = "lblPrinzip";
            lblPrinzip.Size = new System.Drawing.Size(160, 23);
            lblPrinzip.TabIndex = 1;
            lblPrinzip.Text = "Funktionsprinzip";
            // 
            // pnlRegelung
            // 
            pnlRegelung.Controls.Add(cbRegelung);
            pnlRegelung.Controls.Add(lblRegelung);
            pnlRegelung.Location = new System.Drawing.Point(530, 20);
            pnlRegelung.Margin = new System.Windows.Forms.Padding(5);
            pnlRegelung.Name = "pnlRegelung";
            pnlRegelung.Size = new System.Drawing.Size(160, 55);
            pnlRegelung.TabIndex = 3;
            // 
            // cbRegelung
            // 
            cbRegelung.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbRegelung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbRegelung.Location = new System.Drawing.Point(0, 32);
            cbRegelung.Name = "cbRegelung";
            cbRegelung.Size = new System.Drawing.Size(160, 23);
            cbRegelung.TabIndex = 0;
            // 
            // lblRegelung
            // 
            lblRegelung.Dock = System.Windows.Forms.DockStyle.Top;
            lblRegelung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblRegelung.Location = new System.Drawing.Point(0, 0);
            lblRegelung.Name = "lblRegelung";
            lblRegelung.Size = new System.Drawing.Size(160, 23);
            lblRegelung.TabIndex = 1;
            lblRegelung.Text = "Regelung";
            // 
            // pnlBauart
            // 
            pnlBauart.Controls.Add(cbBauart);
            pnlBauart.Controls.Add(lblBauart);
            pnlBauart.Location = new System.Drawing.Point(700, 20);
            pnlBauart.Margin = new System.Windows.Forms.Padding(5);
            pnlBauart.Name = "pnlBauart";
            pnlBauart.Size = new System.Drawing.Size(160, 55);
            pnlBauart.TabIndex = 4;
            // 
            // cbBauart
            // 
            cbBauart.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbBauart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbBauart.Location = new System.Drawing.Point(0, 32);
            cbBauart.Name = "cbBauart";
            cbBauart.Size = new System.Drawing.Size(160, 23);
            cbBauart.TabIndex = 0;
            // 
            // lblBauart
            // 
            lblBauart.Dock = System.Windows.Forms.DockStyle.Top;
            lblBauart.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblBauart.Location = new System.Drawing.Point(0, 0);
            lblBauart.Name = "lblBauart";
            lblBauart.Size = new System.Drawing.Size(160, 23);
            lblBauart.TabIndex = 1;
            lblBauart.Text = "Bauart";
            // 
            // pnlAufstellung
            // 
            pnlAufstellung.Controls.Add(cbAufstellung);
            pnlAufstellung.Controls.Add(lblAufstellung);
            pnlAufstellung.Location = new System.Drawing.Point(870, 20);
            pnlAufstellung.Margin = new System.Windows.Forms.Padding(5);
            pnlAufstellung.Name = "pnlAufstellung";
            pnlAufstellung.Size = new System.Drawing.Size(160, 55);
            pnlAufstellung.TabIndex = 5;
            // 
            // cbAufstellung
            // 
            cbAufstellung.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbAufstellung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbAufstellung.Location = new System.Drawing.Point(0, 32);
            cbAufstellung.Name = "cbAufstellung";
            cbAufstellung.Size = new System.Drawing.Size(160, 23);
            cbAufstellung.TabIndex = 0;
            // 
            // lblAufstellung
            // 
            lblAufstellung.Dock = System.Windows.Forms.DockStyle.Top;
            lblAufstellung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblAufstellung.Location = new System.Drawing.Point(0, 0);
            lblAufstellung.Name = "lblAufstellung";
            lblAufstellung.Size = new System.Drawing.Size(160, 23);
            lblAufstellung.TabIndex = 1;
            lblAufstellung.Text = "Aufstellung";
            // 
            // pnlZuheizun
            // 
            pnlZuheizun.Controls.Add(cbZuheizung);
            pnlZuheizun.Controls.Add(lblZuheizung);
            pnlZuheizun.Location = new System.Drawing.Point(1040, 20);
            pnlZuheizun.Margin = new System.Windows.Forms.Padding(5);
            pnlZuheizun.Name = "pnlZuheizun";
            pnlZuheizun.Size = new System.Drawing.Size(120, 55);
            pnlZuheizun.TabIndex = 6;
            // 
            // cbZuheizung
            // 
            cbZuheizung.Dock = System.Windows.Forms.DockStyle.Bottom;
            cbZuheizung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbZuheizung.Location = new System.Drawing.Point(0, 32);
            cbZuheizung.Name = "cbZuheizung";
            cbZuheizung.Size = new System.Drawing.Size(120, 23);
            cbZuheizung.TabIndex = 0;
            // 
            // lblZuheizung
            // 
            lblZuheizung.Dock = System.Windows.Forms.DockStyle.Top;
            lblZuheizung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblZuheizung.Location = new System.Drawing.Point(0, 0);
            lblZuheizung.Name = "lblZuheizung";
            lblZuheizung.Size = new System.Drawing.Size(120, 23);
            lblZuheizung.TabIndex = 1;
            lblZuheizung.Text = "Zuheizung";
            // 
            // pnlTempMin
            // 
            pnlTempMin.Controls.Add(numTempMin);
            pnlTempMin.Controls.Add(lblTempMin);
            pnlTempMin.Location = new System.Drawing.Point(20, 85);
            pnlTempMin.Margin = new System.Windows.Forms.Padding(5);
            pnlTempMin.Name = "pnlTempMin";
            pnlTempMin.Size = new System.Drawing.Size(100, 55);
            pnlTempMin.TabIndex = 7;
            // 
            // numTempMin
            // 
            numTempMin.DecimalPlaces = 1;
            numTempMin.Dock = System.Windows.Forms.DockStyle.Bottom;
            numTempMin.Location = new System.Drawing.Point(0, 32);
            numTempMin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numTempMin.Name = "numTempMin";
            numTempMin.Size = new System.Drawing.Size(100, 23);
            numTempMin.TabIndex = 0;
            // 
            // lblTempMin
            // 
            lblTempMin.Dock = System.Windows.Forms.DockStyle.Top;
            lblTempMin.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblTempMin.Location = new System.Drawing.Point(0, 0);
            lblTempMin.Name = "lblTempMin";
            lblTempMin.Size = new System.Drawing.Size(100, 23);
            lblTempMin.TabIndex = 1;
            lblTempMin.Text = "VLT Min [°C]";
            // 
            // pnlTempMax
            // 
            pnlTempMax.Controls.Add(numTempMax);
            pnlTempMax.Controls.Add(lblTempMax);
            pnlTempMax.Location = new System.Drawing.Point(130, 85);
            pnlTempMax.Margin = new System.Windows.Forms.Padding(5);
            pnlTempMax.Name = "pnlTempMax";
            pnlTempMax.Size = new System.Drawing.Size(100, 55);
            pnlTempMax.TabIndex = 8;
            // 
            // numTempMax
            // 
            numTempMax.DecimalPlaces = 1;
            numTempMax.Dock = System.Windows.Forms.DockStyle.Bottom;
            numTempMax.Location = new System.Drawing.Point(0, 32);
            numTempMax.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numTempMax.Name = "numTempMax";
            numTempMax.Size = new System.Drawing.Size(100, 23);
            numTempMax.TabIndex = 0;
            // 
            // lblTempMax
            // 
            lblTempMax.Dock = System.Windows.Forms.DockStyle.Top;
            lblTempMax.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblTempMax.Location = new System.Drawing.Point(0, 0);
            lblTempMax.Name = "lblTempMax";
            lblTempMax.Size = new System.Drawing.Size(100, 23);
            lblTempMax.TabIndex = 1;
            lblTempMax.Text = "VLT Max [°C]";
            // 
            // pnlLeistungMin
            // 
            pnlLeistungMin.Controls.Add(numLeistungMin);
            pnlLeistungMin.Controls.Add(lblLeistungMin);
            pnlLeistungMin.Location = new System.Drawing.Point(240, 85);
            pnlLeistungMin.Margin = new System.Windows.Forms.Padding(5);
            pnlLeistungMin.Name = "pnlLeistungMin";
            pnlLeistungMin.Size = new System.Drawing.Size(100, 55);
            pnlLeistungMin.TabIndex = 9;
            // 
            // numLeistungMin
            // 
            numLeistungMin.DecimalPlaces = 1;
            numLeistungMin.Dock = System.Windows.Forms.DockStyle.Bottom;
            numLeistungMin.Location = new System.Drawing.Point(0, 32);
            numLeistungMin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numLeistungMin.Name = "numLeistungMin";
            numLeistungMin.Size = new System.Drawing.Size(100, 23);
            numLeistungMin.TabIndex = 0;
            // 
            // lblLeistungMin
            // 
            lblLeistungMin.Dock = System.Windows.Forms.DockStyle.Top;
            lblLeistungMin.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblLeistungMin.Location = new System.Drawing.Point(0, 0);
            lblLeistungMin.Name = "lblLeistungMin";
            lblLeistungMin.Size = new System.Drawing.Size(100, 23);
            lblLeistungMin.TabIndex = 1;
            lblLeistungMin.Text = "Leist. Min [kW]";
            // 
            // pnlLeistungMax
            // 
            pnlLeistungMax.Controls.Add(numLeistungMax);
            pnlLeistungMax.Controls.Add(lblLeistungMax);
            pnlLeistungMax.Location = new System.Drawing.Point(350, 85);
            pnlLeistungMax.Margin = new System.Windows.Forms.Padding(5);
            pnlLeistungMax.Name = "pnlLeistungMax";
            pnlLeistungMax.Size = new System.Drawing.Size(100, 55);
            pnlLeistungMax.TabIndex = 10;
            // 
            // numLeistungMax
            // 
            numLeistungMax.DecimalPlaces = 1;
            numLeistungMax.Dock = System.Windows.Forms.DockStyle.Bottom;
            numLeistungMax.Location = new System.Drawing.Point(0, 32);
            numLeistungMax.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            numLeistungMax.Name = "numLeistungMax";
            numLeistungMax.Size = new System.Drawing.Size(100, 23);
            numLeistungMax.TabIndex = 0;
            // 
            // lblLeistungMax
            // 
            lblLeistungMax.Dock = System.Windows.Forms.DockStyle.Top;
            lblLeistungMax.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            lblLeistungMax.Location = new System.Drawing.Point(0, 0);
            lblLeistungMax.Name = "lblLeistungMax";
            lblLeistungMax.Size = new System.Drawing.Size(100, 23);
            lblLeistungMax.TabIndex = 1;
            lblLeistungMax.Text = "Leist. Max [kW]";
            // 
            // filterBezeichnungPanel
            // 
            filterBezeichnungPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            filterBezeichnungPanel.Controls.Add(label1);
            filterBezeichnungPanel.Controls.Add(txtSucheBezeichnung);
            filterBezeichnungPanel.Location = new System.Drawing.Point(460, 85);
            filterBezeichnungPanel.Margin = new System.Windows.Forms.Padding(5);
            filterBezeichnungPanel.Name = "filterBezeichnungPanel";
            filterBezeichnungPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            filterBezeichnungPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            filterBezeichnungPanel.Size = new System.Drawing.Size(200, 57);
            filterBezeichnungPanel.TabIndex = 12;
            // 
            // label1
            // 
            label1.Dock = System.Windows.Forms.DockStyle.Top;
            label1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            label1.Location = new System.Drawing.Point(3, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(194, 20);
            label1.TabIndex = 14;
            label1.Text = "Modell filtern (z.B. CS*7*)";
            // 
            // txtSucheBezeichnung
            // 
            txtSucheBezeichnung.Dock = System.Windows.Forms.DockStyle.Bottom;
            txtSucheBezeichnung.Location = new System.Drawing.Point(3, 31);
            txtSucheBezeichnung.Name = "txtSucheBezeichnung";
            txtSucheBezeichnung.Size = new System.Drawing.Size(194, 23);
            txtSucheBezeichnung.TabIndex = 0;
            // 
            // pnlFilterbtn
            // 
            pnlFilterbtn.Controls.Add(btn_Reset);
            pnlFilterbtn.Controls.Add(btnFilter);
            pnlFilterbtn.Location = new System.Drawing.Point(668, 83);
            pnlFilterbtn.Name = "pnlFilterbtn";
            pnlFilterbtn.Size = new System.Drawing.Size(138, 57);
            pnlFilterbtn.TabIndex = 13;
            // 
            // btn_Reset
            // 
            btn_Reset.BackColor = System.Drawing.Color.Gray;
            btn_Reset.Dock = System.Windows.Forms.DockStyle.Top;
            btn_Reset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn_Reset.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btn_Reset.ForeColor = System.Drawing.Color.White;
            btn_Reset.Location = new System.Drawing.Point(0, 31);
            btn_Reset.Margin = new System.Windows.Forms.Padding(15, 12, 0, 0);
            btn_Reset.Name = "btn_Reset";
            btn_Reset.Size = new System.Drawing.Size(138, 28);
            btn_Reset.TabIndex = 12;
            btn_Reset.Text = "Filter Reset";
            btn_Reset.UseVisualStyleBackColor = false;
            btn_Reset.Click += btn_Reset_Click;
            // 
            // btnFilter
            // 
            btnFilter.BackColor = System.Drawing.Color.SteelBlue;
            btnFilter.Dock = System.Windows.Forms.DockStyle.Top;
            btnFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            btnFilter.ForeColor = System.Drawing.Color.White;
            btnFilter.Location = new System.Drawing.Point(0, 0);
            btnFilter.Margin = new System.Windows.Forms.Padding(15, 12, 0, 0);
            btnFilter.Name = "btnFilter";
            btnFilter.Size = new System.Drawing.Size(138, 31);
            btnFilter.TabIndex = 11;
            btnFilter.Text = "Daten filtern";
            btnFilter.UseVisualStyleBackColor = false;
            // 
            // bottomPanel
            // 
            bottomPanel.BackColor = System.Drawing.Color.White;
            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnSelect);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 736);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new System.Drawing.Size(1181, 64);
            bottomPanel.TabIndex = 2;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.BackColor = System.Drawing.SystemColors.Control;
            btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            btnCancel.ForeColor = System.Drawing.Color.Black;
            btnCancel.Location = new System.Drawing.Point(819, 15);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(121, 36);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "❌";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSelect
            // 
            btnSelect.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnSelect.BackColor = System.Drawing.Color.MediumSeaGreen;
            btnSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnSelect.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            btnSelect.ForeColor = System.Drawing.Color.White;
            btnSelect.Location = new System.Drawing.Point(961, 15);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new System.Drawing.Size(189, 36);
            btnSelect.TabIndex = 0;
            btnSelect.Text = "✔ Auswahl übernehmen";
            btnSelect.UseVisualStyleBackColor = false;
            // 
            // Form_WpFilterAuswahl
            // 
            ClientSize = new System.Drawing.Size(1181, 800);
            Controls.Add(dgv);
            Controls.Add(filterPanel);
            Controls.Add(bottomPanel);
            Name = "Form_WpFilterAuswahl";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Wärmepumpen-Katalog";
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            filterPanel.ResumeLayout(false);
            pnlHersteller.ResumeLayout(false);
            pnlAuslegung.ResumeLayout(false);
            pnlPrinzip.ResumeLayout(false);
            pnlRegelung.ResumeLayout(false);
            pnlBauart.ResumeLayout(false);
            pnlAufstellung.ResumeLayout(false);
            pnlZuheizun.ResumeLayout(false);
            pnlTempMin.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numTempMin).EndInit();
            pnlTempMax.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numTempMax).EndInit();
            pnlLeistungMin.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numLeistungMin).EndInit();
            pnlLeistungMax.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numLeistungMax).EndInit();
            filterBezeichnungPanel.ResumeLayout(false);
            filterBezeichnungPanel.PerformLayout();
            pnlFilterbtn.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.FlowLayoutPanel filterPanel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Button btnFilter;

        private System.Windows.Forms.Panel pnlHersteller;
        private System.Windows.Forms.Label lblHersteller;
        private System.Windows.Forms.ComboBox cbHersteller;

        private System.Windows.Forms.Panel pnlAuslegung;
        private System.Windows.Forms.Label lblAuslegung;
        private System.Windows.Forms.ComboBox cbAuslegung;

        private System.Windows.Forms.Panel pnlPrinzip;
        private System.Windows.Forms.Label lblPrinzip;
        private System.Windows.Forms.ComboBox cbPrinzip;

        private System.Windows.Forms.Panel pnlRegelung;
        private System.Windows.Forms.Label lblRegelung;
        private System.Windows.Forms.ComboBox cbRegelung;

        private System.Windows.Forms.Panel pnlBauart;
        private System.Windows.Forms.Label lblBauart;
        private System.Windows.Forms.ComboBox cbBauart;

        private System.Windows.Forms.Panel pnlAufstellung;
        private System.Windows.Forms.Label lblAufstellung;
        private System.Windows.Forms.ComboBox cbAufstellung;

        private System.Windows.Forms.Panel pnlZuheizun;
        private System.Windows.Forms.Label lblZuheizung;
        private System.Windows.Forms.ComboBox cbZuheizung;

        private System.Windows.Forms.Panel pnlTempMin;
        private System.Windows.Forms.Label lblTempMin;
        private System.Windows.Forms.NumericUpDown numTempMin;

        private System.Windows.Forms.Panel pnlTempMax;
        private System.Windows.Forms.Label lblTempMax;
        private System.Windows.Forms.NumericUpDown numTempMax;

        private System.Windows.Forms.Panel pnlLeistungMin;
        private System.Windows.Forms.Label lblLeistungMin;
        private System.Windows.Forms.NumericUpDown numLeistungMin;

        private System.Windows.Forms.Panel pnlLeistungMax;
        private System.Windows.Forms.Label lblLeistungMax;
        private System.Windows.Forms.NumericUpDown numLeistungMax;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel pnlFilterbtn;
        private System.Windows.Forms.Button btn_Reset;
        private System.Windows.Forms.TextBox txtSucheBezeichnung;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel filterBezeichnungPanel;
    }
}