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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.filterPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlHersteller = new System.Windows.Forms.Panel();
            this.cbHersteller = new System.Windows.Forms.ComboBox();
            this.lblHersteller = new System.Windows.Forms.Label();
            this.pnlAuslegung = new System.Windows.Forms.Panel();
            this.cbAuslegung = new System.Windows.Forms.ComboBox();
            this.lblAuslegung = new System.Windows.Forms.Label();
            this.pnlPrinzip = new System.Windows.Forms.Panel();
            this.cbPrinzip = new System.Windows.Forms.ComboBox();
            this.lblPrinzip = new System.Windows.Forms.Label();
            this.pnlRegelung = new System.Windows.Forms.Panel();
            this.cbRegelung = new System.Windows.Forms.ComboBox();
            this.lblRegelung = new System.Windows.Forms.Label();
            this.pnlBauart = new System.Windows.Forms.Panel();
            this.cbBauart = new System.Windows.Forms.ComboBox();
            this.lblBauart = new System.Windows.Forms.Label();
            this.pnlAufstellung = new System.Windows.Forms.Panel();
            this.cbAufstellung = new System.Windows.Forms.ComboBox();
            this.lblAufstellung = new System.Windows.Forms.Label();
            this.pnlZuheizun = new System.Windows.Forms.Panel();
            this.cbZuheizung = new System.Windows.Forms.ComboBox();
            this.lblZuheizung = new System.Windows.Forms.Label();
            this.pnlTempMin = new System.Windows.Forms.Panel();
            this.numTempMin = new System.Windows.Forms.NumericUpDown();
            this.lblTempMin = new System.Windows.Forms.Label();
            this.pnlTempMax = new System.Windows.Forms.Panel();
            this.numTempMax = new System.Windows.Forms.NumericUpDown();
            this.lblTempMax = new System.Windows.Forms.Label();
            this.pnlLeistungMin = new System.Windows.Forms.Panel();
            this.numLeistungMin = new System.Windows.Forms.NumericUpDown();
            this.lblLeistungMin = new System.Windows.Forms.Label();
            this.pnlLeistungMax = new System.Windows.Forms.Panel();
            this.numLeistungMax = new System.Windows.Forms.NumericUpDown();
            this.lblLeistungMax = new System.Windows.Forms.Label();
            this.filterBezeichnungPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtSucheBezeichnung = new System.Windows.Forms.TextBox();
            this.pnlFilterbtn = new System.Windows.Forms.Panel();
            this.btn_Reset = new System.Windows.Forms.Button();
            this.btnFilter = new System.Windows.Forms.Button();
            this.bottomPanel = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSelect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.filterPanel.SuspendLayout();
            this.pnlHersteller.SuspendLayout();
            this.pnlAuslegung.SuspendLayout();
            this.pnlPrinzip.SuspendLayout();
            this.pnlRegelung.SuspendLayout();
            this.pnlBauart.SuspendLayout();
            this.pnlAufstellung.SuspendLayout();
            this.pnlZuheizun.SuspendLayout();
            this.pnlTempMin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTempMin)).BeginInit();
            this.pnlTempMax.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTempMax)).BeginInit();
            this.pnlLeistungMin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungMin)).BeginInit();
            this.pnlLeistungMax.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungMax)).BeginInit();
            this.filterBezeichnungPanel.SuspendLayout();
            this.pnlFilterbtn.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 160);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size = new System.Drawing.Size(1181, 576);
            this.dgv.TabIndex = 0;
            // 
            // filterPanel
            // 
            this.filterPanel.AutoScroll = true;
            this.filterPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(225)))), ((int)(((byte)(230)))));
            this.filterPanel.Controls.Add(this.pnlHersteller);
            this.filterPanel.Controls.Add(this.pnlAuslegung);
            this.filterPanel.Controls.Add(this.pnlPrinzip);
            this.filterPanel.Controls.Add(this.pnlRegelung);
            this.filterPanel.Controls.Add(this.pnlBauart);
            this.filterPanel.Controls.Add(this.pnlAufstellung);
            this.filterPanel.Controls.Add(this.pnlZuheizun);
            this.filterPanel.Controls.Add(this.pnlTempMin);
            this.filterPanel.Controls.Add(this.pnlTempMax);
            this.filterPanel.Controls.Add(this.pnlLeistungMin);
            this.filterPanel.Controls.Add(this.pnlLeistungMax);
            this.filterPanel.Controls.Add(this.filterBezeichnungPanel);
            this.filterPanel.Controls.Add(this.pnlFilterbtn);
            this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.filterPanel.Location = new System.Drawing.Point(0, 0);
            this.filterPanel.Name = "filterPanel";
            this.filterPanel.Padding = new System.Windows.Forms.Padding(15);
            this.filterPanel.Size = new System.Drawing.Size(1181, 160);
            this.filterPanel.TabIndex = 1;
            // 
            // pnlHersteller
            // 
            this.pnlHersteller.Controls.Add(this.cbHersteller);
            this.pnlHersteller.Controls.Add(this.lblHersteller);
            this.pnlHersteller.Location = new System.Drawing.Point(20, 20);
            this.pnlHersteller.Margin = new System.Windows.Forms.Padding(5);
            this.pnlHersteller.Name = "pnlHersteller";
            this.pnlHersteller.Size = new System.Drawing.Size(160, 55);
            this.pnlHersteller.TabIndex = 0;
            // 
            // cbHersteller
            // 
            this.cbHersteller.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbHersteller.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbHersteller.Location = new System.Drawing.Point(0, 34);
            this.cbHersteller.Name = "cbHersteller";
            this.cbHersteller.Size = new System.Drawing.Size(160, 21);
            this.cbHersteller.TabIndex = 0;
            // 
            // lblHersteller
            // 
            this.lblHersteller.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHersteller.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblHersteller.Location = new System.Drawing.Point(0, 0);
            this.lblHersteller.Name = "lblHersteller";
            this.lblHersteller.Size = new System.Drawing.Size(160, 23);
            this.lblHersteller.TabIndex = 1;
            this.lblHersteller.Text = "Hersteller";
            // 
            // pnlAuslegung
            // 
            this.pnlAuslegung.Controls.Add(this.cbAuslegung);
            this.pnlAuslegung.Controls.Add(this.lblAuslegung);
            this.pnlAuslegung.Location = new System.Drawing.Point(190, 20);
            this.pnlAuslegung.Margin = new System.Windows.Forms.Padding(5);
            this.pnlAuslegung.Name = "pnlAuslegung";
            this.pnlAuslegung.Size = new System.Drawing.Size(160, 55);
            this.pnlAuslegung.TabIndex = 1;
            // 
            // cbAuslegung
            // 
            this.cbAuslegung.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbAuslegung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAuslegung.Location = new System.Drawing.Point(0, 34);
            this.cbAuslegung.Name = "cbAuslegung";
            this.cbAuslegung.Size = new System.Drawing.Size(160, 21);
            this.cbAuslegung.TabIndex = 0;
            // 
            // lblAuslegung
            // 
            this.lblAuslegung.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAuslegung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblAuslegung.Location = new System.Drawing.Point(0, 0);
            this.lblAuslegung.Name = "lblAuslegung";
            this.lblAuslegung.Size = new System.Drawing.Size(160, 23);
            this.lblAuslegung.TabIndex = 1;
            this.lblAuslegung.Text = "Auslegung";
            // 
            // pnlPrinzip
            // 
            this.pnlPrinzip.Controls.Add(this.cbPrinzip);
            this.pnlPrinzip.Controls.Add(this.lblPrinzip);
            this.pnlPrinzip.Location = new System.Drawing.Point(360, 20);
            this.pnlPrinzip.Margin = new System.Windows.Forms.Padding(5);
            this.pnlPrinzip.Name = "pnlPrinzip";
            this.pnlPrinzip.Size = new System.Drawing.Size(160, 55);
            this.pnlPrinzip.TabIndex = 2;
            // 
            // cbPrinzip
            // 
            this.cbPrinzip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbPrinzip.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPrinzip.Location = new System.Drawing.Point(0, 34);
            this.cbPrinzip.Name = "cbPrinzip";
            this.cbPrinzip.Size = new System.Drawing.Size(160, 21);
            this.cbPrinzip.TabIndex = 0;
            // 
            // lblPrinzip
            // 
            this.lblPrinzip.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblPrinzip.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblPrinzip.Location = new System.Drawing.Point(0, 0);
            this.lblPrinzip.Name = "lblPrinzip";
            this.lblPrinzip.Size = new System.Drawing.Size(160, 23);
            this.lblPrinzip.TabIndex = 1;
            this.lblPrinzip.Text = "Funktionsprinzip";
            // 
            // pnlRegelung
            // 
            this.pnlRegelung.Controls.Add(this.cbRegelung);
            this.pnlRegelung.Controls.Add(this.lblRegelung);
            this.pnlRegelung.Location = new System.Drawing.Point(530, 20);
            this.pnlRegelung.Margin = new System.Windows.Forms.Padding(5);
            this.pnlRegelung.Name = "pnlRegelung";
            this.pnlRegelung.Size = new System.Drawing.Size(160, 55);
            this.pnlRegelung.TabIndex = 3;
            // 
            // cbRegelung
            // 
            this.cbRegelung.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbRegelung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRegelung.Location = new System.Drawing.Point(0, 34);
            this.cbRegelung.Name = "cbRegelung";
            this.cbRegelung.Size = new System.Drawing.Size(160, 21);
            this.cbRegelung.TabIndex = 0;
            // 
            // lblRegelung
            // 
            this.lblRegelung.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRegelung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblRegelung.Location = new System.Drawing.Point(0, 0);
            this.lblRegelung.Name = "lblRegelung";
            this.lblRegelung.Size = new System.Drawing.Size(160, 23);
            this.lblRegelung.TabIndex = 1;
            this.lblRegelung.Text = "Regelung";
            // 
            // pnlBauart
            // 
            this.pnlBauart.Controls.Add(this.cbBauart);
            this.pnlBauart.Controls.Add(this.lblBauart);
            this.pnlBauart.Location = new System.Drawing.Point(700, 20);
            this.pnlBauart.Margin = new System.Windows.Forms.Padding(5);
            this.pnlBauart.Name = "pnlBauart";
            this.pnlBauart.Size = new System.Drawing.Size(160, 55);
            this.pnlBauart.TabIndex = 4;
            // 
            // cbBauart
            // 
            this.cbBauart.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbBauart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBauart.Location = new System.Drawing.Point(0, 34);
            this.cbBauart.Name = "cbBauart";
            this.cbBauart.Size = new System.Drawing.Size(160, 21);
            this.cbBauart.TabIndex = 0;
            // 
            // lblBauart
            // 
            this.lblBauart.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblBauart.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblBauart.Location = new System.Drawing.Point(0, 0);
            this.lblBauart.Name = "lblBauart";
            this.lblBauart.Size = new System.Drawing.Size(160, 23);
            this.lblBauart.TabIndex = 1;
            this.lblBauart.Text = "Bauart";
            // 
            // pnlAufstellung
            // 
            this.pnlAufstellung.Controls.Add(this.cbAufstellung);
            this.pnlAufstellung.Controls.Add(this.lblAufstellung);
            this.pnlAufstellung.Location = new System.Drawing.Point(870, 20);
            this.pnlAufstellung.Margin = new System.Windows.Forms.Padding(5);
            this.pnlAufstellung.Name = "pnlAufstellung";
            this.pnlAufstellung.Size = new System.Drawing.Size(160, 55);
            this.pnlAufstellung.TabIndex = 5;
            // 
            // cbAufstellung
            // 
            this.cbAufstellung.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbAufstellung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAufstellung.Location = new System.Drawing.Point(0, 34);
            this.cbAufstellung.Name = "cbAufstellung";
            this.cbAufstellung.Size = new System.Drawing.Size(160, 21);
            this.cbAufstellung.TabIndex = 0;
            // 
            // lblAufstellung
            // 
            this.lblAufstellung.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAufstellung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblAufstellung.Location = new System.Drawing.Point(0, 0);
            this.lblAufstellung.Name = "lblAufstellung";
            this.lblAufstellung.Size = new System.Drawing.Size(160, 23);
            this.lblAufstellung.TabIndex = 1;
            this.lblAufstellung.Text = "Aufstellung";
            // 
            // pnlZuheizun
            // 
            this.pnlZuheizun.Controls.Add(this.cbZuheizung);
            this.pnlZuheizun.Controls.Add(this.lblZuheizung);
            this.pnlZuheizun.Location = new System.Drawing.Point(1040, 20);
            this.pnlZuheizun.Margin = new System.Windows.Forms.Padding(5);
            this.pnlZuheizun.Name = "pnlZuheizun";
            this.pnlZuheizun.Size = new System.Drawing.Size(120, 55);
            this.pnlZuheizun.TabIndex = 6;
            // 
            // cbZuheizung
            // 
            this.cbZuheizung.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbZuheizung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbZuheizung.Location = new System.Drawing.Point(0, 34);
            this.cbZuheizung.Name = "cbZuheizung";
            this.cbZuheizung.Size = new System.Drawing.Size(120, 21);
            this.cbZuheizung.TabIndex = 0;
            // 
            // lblZuheizung
            // 
            this.lblZuheizung.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblZuheizung.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblZuheizung.Location = new System.Drawing.Point(0, 0);
            this.lblZuheizung.Name = "lblZuheizung";
            this.lblZuheizung.Size = new System.Drawing.Size(120, 23);
            this.lblZuheizung.TabIndex = 1;
            this.lblZuheizung.Text = "Zuheizung";
            // 
            // pnlTempMin
            // 
            this.pnlTempMin.Controls.Add(this.numTempMin);
            this.pnlTempMin.Controls.Add(this.lblTempMin);
            this.pnlTempMin.Location = new System.Drawing.Point(20, 85);
            this.pnlTempMin.Margin = new System.Windows.Forms.Padding(5);
            this.pnlTempMin.Name = "pnlTempMin";
            this.pnlTempMin.Size = new System.Drawing.Size(100, 55);
            this.pnlTempMin.TabIndex = 7;
            // 
            // numTempMin
            // 
            this.numTempMin.DecimalPlaces = 1;
            this.numTempMin.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.numTempMin.Location = new System.Drawing.Point(0, 35);
            this.numTempMin.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numTempMin.Name = "numTempMin";
            this.numTempMin.Size = new System.Drawing.Size(100, 20);
            this.numTempMin.TabIndex = 0;
            // 
            // lblTempMin
            // 
            this.lblTempMin.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTempMin.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblTempMin.Location = new System.Drawing.Point(0, 0);
            this.lblTempMin.Name = "lblTempMin";
            this.lblTempMin.Size = new System.Drawing.Size(100, 23);
            this.lblTempMin.TabIndex = 1;
            this.lblTempMin.Text = "VLT Min [°C]";
            // 
            // pnlTempMax
            // 
            this.pnlTempMax.Controls.Add(this.numTempMax);
            this.pnlTempMax.Controls.Add(this.lblTempMax);
            this.pnlTempMax.Location = new System.Drawing.Point(130, 85);
            this.pnlTempMax.Margin = new System.Windows.Forms.Padding(5);
            this.pnlTempMax.Name = "pnlTempMax";
            this.pnlTempMax.Size = new System.Drawing.Size(100, 55);
            this.pnlTempMax.TabIndex = 8;
            // 
            // numTempMax
            // 
            this.numTempMax.DecimalPlaces = 1;
            this.numTempMax.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.numTempMax.Location = new System.Drawing.Point(0, 35);
            this.numTempMax.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numTempMax.Name = "numTempMax";
            this.numTempMax.Size = new System.Drawing.Size(100, 20);
            this.numTempMax.TabIndex = 0;
            // 
            // lblTempMax
            // 
            this.lblTempMax.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTempMax.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblTempMax.Location = new System.Drawing.Point(0, 0);
            this.lblTempMax.Name = "lblTempMax";
            this.lblTempMax.Size = new System.Drawing.Size(100, 23);
            this.lblTempMax.TabIndex = 1;
            this.lblTempMax.Text = "VLT Max [°C]";
            // 
            // pnlLeistungMin
            // 
            this.pnlLeistungMin.Controls.Add(this.numLeistungMin);
            this.pnlLeistungMin.Controls.Add(this.lblLeistungMin);
            this.pnlLeistungMin.Location = new System.Drawing.Point(240, 85);
            this.pnlLeistungMin.Margin = new System.Windows.Forms.Padding(5);
            this.pnlLeistungMin.Name = "pnlLeistungMin";
            this.pnlLeistungMin.Size = new System.Drawing.Size(100, 55);
            this.pnlLeistungMin.TabIndex = 9;
            // 
            // numLeistungMin
            // 
            this.numLeistungMin.DecimalPlaces = 1;
            this.numLeistungMin.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.numLeistungMin.Location = new System.Drawing.Point(0, 35);
            this.numLeistungMin.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numLeistungMin.Name = "numLeistungMin";
            this.numLeistungMin.Size = new System.Drawing.Size(100, 20);
            this.numLeistungMin.TabIndex = 0;
            // 
            // lblLeistungMin
            // 
            this.lblLeistungMin.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLeistungMin.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLeistungMin.Location = new System.Drawing.Point(0, 0);
            this.lblLeistungMin.Name = "lblLeistungMin";
            this.lblLeistungMin.Size = new System.Drawing.Size(100, 23);
            this.lblLeistungMin.TabIndex = 1;
            this.lblLeistungMin.Text = "Leist. Min [kW]";
            // 
            // pnlLeistungMax
            // 
            this.pnlLeistungMax.Controls.Add(this.numLeistungMax);
            this.pnlLeistungMax.Controls.Add(this.lblLeistungMax);
            this.pnlLeistungMax.Location = new System.Drawing.Point(350, 85);
            this.pnlLeistungMax.Margin = new System.Windows.Forms.Padding(5);
            this.pnlLeistungMax.Name = "pnlLeistungMax";
            this.pnlLeistungMax.Size = new System.Drawing.Size(100, 55);
            this.pnlLeistungMax.TabIndex = 10;
            // 
            // numLeistungMax
            // 
            this.numLeistungMax.DecimalPlaces = 1;
            this.numLeistungMax.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.numLeistungMax.Location = new System.Drawing.Point(0, 35);
            this.numLeistungMax.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numLeistungMax.Name = "numLeistungMax";
            this.numLeistungMax.Size = new System.Drawing.Size(100, 20);
            this.numLeistungMax.TabIndex = 0;
            // 
            // lblLeistungMax
            // 
            this.lblLeistungMax.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLeistungMax.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLeistungMax.Location = new System.Drawing.Point(0, 0);
            this.lblLeistungMax.Name = "lblLeistungMax";
            this.lblLeistungMax.Size = new System.Drawing.Size(100, 23);
            this.lblLeistungMax.TabIndex = 1;
            this.lblLeistungMax.Text = "Leist. Max [kW]";
            // 
            // filterBezeichnungPanel
            // 
            this.filterBezeichnungPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.filterBezeichnungPanel.Controls.Add(this.label1);
            this.filterBezeichnungPanel.Controls.Add(this.txtSucheBezeichnung);
            this.filterBezeichnungPanel.Location = new System.Drawing.Point(460, 85);
            this.filterBezeichnungPanel.Margin = new System.Windows.Forms.Padding(5);
            this.filterBezeichnungPanel.Name = "filterBezeichnungPanel";
            this.filterBezeichnungPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.filterBezeichnungPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.filterBezeichnungPanel.Size = new System.Drawing.Size(200, 57);
            this.filterBezeichnungPanel.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(194, 20);
            this.label1.TabIndex = 14;
            this.label1.Text = "Modell filtern (z.B. CS*7*)";
            // 
            // txtSucheBezeichnung
            // 
            this.txtSucheBezeichnung.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtSucheBezeichnung.Location = new System.Drawing.Point(3, 34);
            this.txtSucheBezeichnung.Name = "txtSucheBezeichnung";
            this.txtSucheBezeichnung.Size = new System.Drawing.Size(194, 20);
            this.txtSucheBezeichnung.TabIndex = 0;
            // 
            // pnlFilterbtn
            // 
            this.pnlFilterbtn.Controls.Add(this.btn_Reset);
            this.pnlFilterbtn.Controls.Add(this.btnFilter);
            this.pnlFilterbtn.Location = new System.Drawing.Point(668, 83);
            this.pnlFilterbtn.Name = "pnlFilterbtn";
            this.pnlFilterbtn.Size = new System.Drawing.Size(138, 57);
            this.pnlFilterbtn.TabIndex = 13;
            // 
            // btn_Reset
            // 
            this.btn_Reset.BackColor = System.Drawing.Color.Gray;
            this.btn_Reset.Dock = System.Windows.Forms.DockStyle.Top;
            this.btn_Reset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Reset.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Reset.ForeColor = System.Drawing.Color.White;
            this.btn_Reset.Location = new System.Drawing.Point(0, 31);
            this.btn_Reset.Margin = new System.Windows.Forms.Padding(15, 12, 0, 0);
            this.btn_Reset.Name = "btn_Reset";
            this.btn_Reset.Size = new System.Drawing.Size(138, 28);
            this.btn_Reset.TabIndex = 12;
            this.btn_Reset.Text = "Filter Reset";
            this.btn_Reset.UseVisualStyleBackColor = false;
            this.btn_Reset.Click += new System.EventHandler(this.btn_Reset_Click);
            // 
            // btnFilter
            // 
            this.btnFilter.BackColor = System.Drawing.Color.SteelBlue;
            this.btnFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnFilter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFilter.ForeColor = System.Drawing.Color.White;
            this.btnFilter.Location = new System.Drawing.Point(0, 0);
            this.btnFilter.Margin = new System.Windows.Forms.Padding(15, 12, 0, 0);
            this.btnFilter.Name = "btnFilter";
            this.btnFilter.Size = new System.Drawing.Size(138, 31);
            this.btnFilter.TabIndex = 11;
            this.btnFilter.Text = "Daten filtern";
            this.btnFilter.UseVisualStyleBackColor = false;
            // 
            // bottomPanel
            // 
            this.bottomPanel.BackColor = System.Drawing.Color.White;
            this.bottomPanel.Controls.Add(this.btnCancel);
            this.bottomPanel.Controls.Add(this.btnSelect);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bottomPanel.Location = new System.Drawing.Point(0, 736);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(1181, 64);
            this.bottomPanel.TabIndex = 2;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.SystemColors.Control;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCancel.ForeColor = System.Drawing.Color.Black;
            this.btnCancel.Location = new System.Drawing.Point(819, 15);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(121, 36);
            this.btnCancel.TabIndex = 1;
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
            this.btnSelect.Location = new System.Drawing.Point(961, 15);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(189, 36);
            this.btnSelect.TabIndex = 0;
            this.btnSelect.Text = "✔ Auswahl übernehmen";
            this.btnSelect.UseVisualStyleBackColor = false;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // Form_WpFilterAuswahl
            // 
            this.ClientSize = new System.Drawing.Size(1181, 800);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.filterPanel);
            this.Controls.Add(this.bottomPanel);
            this.Name = "Form_WpFilterAuswahl";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wärmepumpen-Katalog";
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.filterPanel.ResumeLayout(false);
            this.pnlHersteller.ResumeLayout(false);
            this.pnlAuslegung.ResumeLayout(false);
            this.pnlPrinzip.ResumeLayout(false);
            this.pnlRegelung.ResumeLayout(false);
            this.pnlBauart.ResumeLayout(false);
            this.pnlAufstellung.ResumeLayout(false);
            this.pnlZuheizun.ResumeLayout(false);
            this.pnlTempMin.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numTempMin)).EndInit();
            this.pnlTempMax.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numTempMax)).EndInit();
            this.pnlLeistungMin.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungMin)).EndInit();
            this.pnlLeistungMax.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numLeistungMax)).EndInit();
            this.filterBezeichnungPanel.ResumeLayout(false);
            this.filterBezeichnungPanel.PerformLayout();
            this.pnlFilterbtn.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

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