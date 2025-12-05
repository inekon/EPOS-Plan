namespace WindowsFormsApplication1
{
    partial class Form_Simulation_Kurz 
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.label47 = new System.Windows.Forms.Label();
            this.textBox_gesWaermebedarf = new System.Windows.Forms.TextBox();
            this.label62 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox_gesStrombedarf = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.btn_Simulation = new System.Windows.Forms.Button();
            this.label63 = new System.Windows.Forms.Label();
            this.label81 = new System.Windows.Forms.Label();
            this.textBox_Restwaermebedarf = new System.Windows.Forms.TextBox();
            this.label59 = new System.Windows.Forms.Label();
            this.textBoxBHKWWaermeproduktion = new System.Windows.Forms.TextBox();
            this.label64 = new System.Windows.Forms.Label();
            this.label33 = new System.Windows.Forms.Label();
            this.textBox_HeizstabStromverbrauch = new System.Windows.Forms.TextBox();
            this.label34 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.textBox_WPStromverbrauch = new System.Windows.Forms.TextBox();
            this.label32 = new System.Windows.Forms.Label();
            this.textBox_WPWaermeproduktion = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.textBox_SWWaermeproduktion = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.textBox_SPKWaermeproduktion = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label23 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.textBox_BHKWStromproduktion = new System.Windows.Forms.TextBox();
            this.label25 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.textBox_PVStromproduktion = new System.Windows.Forms.TextBox();
            this.label27 = new System.Windows.Forms.Label();
            this.label28 = new System.Windows.Forms.Label();
            this.textBox_Reststrombedarf = new System.Windows.Forms.TextBox();
            this.label29 = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Konfiguration = new System.Windows.Forms.Button();
            this.chart5 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_SPKStromverbrauch = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.chart5)).BeginInit();
            this.SuspendLayout();
            // 
            // label47
            // 
            this.label47.AutoSize = true;
            this.label47.BackColor = System.Drawing.SystemColors.Control;
            this.label47.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label47.ForeColor = System.Drawing.Color.Black;
            this.label47.Location = new System.Drawing.Point(47, 125);
            this.label47.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label47.Name = "label47";
            this.label47.Size = new System.Drawing.Size(220, 17);
            this.label47.TabIndex = 191;
            this.label47.Text = "Wärmebedarf des Nahwärmenetzes:";
            this.label47.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_gesWaermebedarf
            // 
            this.textBox_gesWaermebedarf.BackColor = System.Drawing.Color.White;
            this.textBox_gesWaermebedarf.Enabled = false;
            this.textBox_gesWaermebedarf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_gesWaermebedarf.ForeColor = System.Drawing.Color.Black;
            this.textBox_gesWaermebedarf.Location = new System.Drawing.Point(267, 122);
            this.textBox_gesWaermebedarf.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_gesWaermebedarf.Name = "textBox_gesWaermebedarf";
            this.textBox_gesWaermebedarf.Size = new System.Drawing.Size(97, 25);
            this.textBox_gesWaermebedarf.TabIndex = 192;
            // 
            // label62
            // 
            this.label62.AutoSize = true;
            this.label62.BackColor = System.Drawing.SystemColors.Control;
            this.label62.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label62.ForeColor = System.Drawing.Color.Black;
            this.label62.Location = new System.Drawing.Point(369, 126);
            this.label62.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(51, 17);
            this.label62.TabIndex = 193;
            this.label62.Text = "MWh/a";
            this.label62.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.SystemColors.Control;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Location = new System.Drawing.Point(180, 161);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(85, 17);
            this.label9.TabIndex = 203;
            this.label9.Text = "Strombedarf:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_gesStrombedarf
            // 
            this.textBox_gesStrombedarf.BackColor = System.Drawing.Color.White;
            this.textBox_gesStrombedarf.Enabled = false;
            this.textBox_gesStrombedarf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_gesStrombedarf.ForeColor = System.Drawing.Color.Black;
            this.textBox_gesStrombedarf.Location = new System.Drawing.Point(267, 157);
            this.textBox_gesStrombedarf.Margin = new System.Windows.Forms.Padding(5);
            this.textBox_gesStrombedarf.Name = "textBox_gesStrombedarf";
            this.textBox_gesStrombedarf.Size = new System.Drawing.Size(97, 25);
            this.textBox_gesStrombedarf.TabIndex = 204;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.SystemColors.Control;
            this.label10.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Black;
            this.label10.Location = new System.Drawing.Point(369, 161);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(51, 17);
            this.label10.TabIndex = 205;
            this.label10.Text = "MWh/a";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.SystemColors.Control;
            this.label13.Font = new System.Drawing.Font("Segoe UI", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.Color.Black;
            this.label13.Location = new System.Drawing.Point(27, 94);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(118, 21);
            this.label13.TabIndex = 208;
            this.label13.Text = "Energiebedarf";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btn_Simulation
            // 
            this.btn_Simulation.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.btn_Simulation.ForeColor = System.Drawing.Color.Black;
            this.btn_Simulation.Location = new System.Drawing.Point(21, 30);
            this.btn_Simulation.Name = "btn_Simulation";
            this.btn_Simulation.Size = new System.Drawing.Size(158, 33);
            this.btn_Simulation.TabIndex = 209;
            this.btn_Simulation.Text = "Simulation starten =>";
            this.btn_Simulation.UseVisualStyleBackColor = false;
            this.btn_Simulation.Click += new System.EventHandler(this.btn_Simulation_Click);
            // 
            // label63
            // 
            this.label63.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label63.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label63.ForeColor = System.Drawing.Color.Black;
            this.label63.Location = new System.Drawing.Point(437, 435);
            this.label63.Name = "label63";
            this.label63.Size = new System.Drawing.Size(114, 22);
            this.label63.TabIndex = 217;
            this.label63.Text = "Restwärmebedarf:";
            this.label63.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label81
            // 
            this.label81.AutoSize = true;
            this.label81.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label81.ForeColor = System.Drawing.Color.Black;
            this.label81.Location = new System.Drawing.Point(648, 437);
            this.label81.Name = "label81";
            this.label81.Size = new System.Drawing.Size(51, 17);
            this.label81.TabIndex = 216;
            this.label81.Text = "MWh/a";
            // 
            // textBox_Restwaermebedarf
            // 
            this.textBox_Restwaermebedarf.BackColor = System.Drawing.Color.White;
            this.textBox_Restwaermebedarf.Enabled = false;
            this.textBox_Restwaermebedarf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Restwaermebedarf.ForeColor = System.Drawing.Color.Black;
            this.textBox_Restwaermebedarf.Location = new System.Drawing.Point(558, 434);
            this.textBox_Restwaermebedarf.Name = "textBox_Restwaermebedarf";
            this.textBox_Restwaermebedarf.Size = new System.Drawing.Size(84, 25);
            this.textBox_Restwaermebedarf.TabIndex = 215;
            // 
            // label59
            // 
            this.label59.AutoSize = true;
            this.label59.BackColor = System.Drawing.SystemColors.Control;
            this.label59.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label59.ForeColor = System.Drawing.Color.Black;
            this.label59.Location = new System.Drawing.Point(28, 344);
            this.label59.Name = "label59";
            this.label59.Size = new System.Drawing.Size(221, 17);
            this.label59.TabIndex = 212;
            this.label59.Text = "Wärmeproduktion der Spitzenkessel:";
            this.label59.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBoxBHKWWaermeproduktion
            // 
            this.textBoxBHKWWaermeproduktion.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxBHKWWaermeproduktion.Enabled = false;
            this.textBoxBHKWWaermeproduktion.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxBHKWWaermeproduktion.ForeColor = System.Drawing.Color.Black;
            this.textBoxBHKWWaermeproduktion.Location = new System.Drawing.Point(256, 275);
            this.textBoxBHKWWaermeproduktion.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxBHKWWaermeproduktion.Name = "textBoxBHKWWaermeproduktion";
            this.textBoxBHKWWaermeproduktion.Size = new System.Drawing.Size(84, 25);
            this.textBoxBHKWWaermeproduktion.TabIndex = 213;
            // 
            // label64
            // 
            this.label64.AutoSize = true;
            this.label64.BackColor = System.Drawing.SystemColors.Control;
            this.label64.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label64.ForeColor = System.Drawing.Color.Black;
            this.label64.Location = new System.Drawing.Point(345, 277);
            this.label64.Name = "label64";
            this.label64.Size = new System.Drawing.Size(51, 17);
            this.label64.TabIndex = 214;
            this.label64.Text = "MWh/a";
            this.label64.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label33
            // 
            this.label33.AutoSize = true;
            this.label33.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label33.ForeColor = System.Drawing.Color.Black;
            this.label33.Location = new System.Drawing.Point(706, 274);
            this.label33.Name = "label33";
            this.label33.Size = new System.Drawing.Size(51, 17);
            this.label33.TabIndex = 226;
            this.label33.Text = "MWh/a";
            // 
            // textBox_HeizstabStromverbrauch
            // 
            this.textBox_HeizstabStromverbrauch.Enabled = false;
            this.textBox_HeizstabStromverbrauch.Font = new System.Drawing.Font("Segoe UI Semibold", 8F);
            this.textBox_HeizstabStromverbrauch.Location = new System.Drawing.Point(611, 272);
            this.textBox_HeizstabStromverbrauch.Name = "textBox_HeizstabStromverbrauch";
            this.textBox_HeizstabStromverbrauch.Size = new System.Drawing.Size(89, 22);
            this.textBox_HeizstabStromverbrauch.TabIndex = 225;
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label34.ForeColor = System.Drawing.Color.Black;
            this.label34.Location = new System.Drawing.Point(449, 275);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(158, 17);
            this.label34.TabIndex = 224;
            this.label34.Text = "Stromverbrauch Heizstab:";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.BackColor = System.Drawing.SystemColors.Control;
            this.label31.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label31.ForeColor = System.Drawing.Color.Black;
            this.label31.Location = new System.Drawing.Point(706, 245);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(51, 17);
            this.label31.TabIndex = 223;
            this.label31.Text = "MWh/a";
            // 
            // textBox_WPStromverbrauch
            // 
            this.textBox_WPStromverbrauch.Enabled = false;
            this.textBox_WPStromverbrauch.Font = new System.Drawing.Font("Segoe UI Semibold", 8F);
            this.textBox_WPStromverbrauch.Location = new System.Drawing.Point(611, 243);
            this.textBox_WPStromverbrauch.Name = "textBox_WPStromverbrauch";
            this.textBox_WPStromverbrauch.Size = new System.Drawing.Size(89, 22);
            this.textBox_WPStromverbrauch.TabIndex = 222;
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label32.ForeColor = System.Drawing.Color.Black;
            this.label32.Location = new System.Drawing.Point(449, 246);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(127, 17);
            this.label32.TabIndex = 221;
            this.label32.Text = "Stromverbrauch WP:";
            // 
            // textBox_WPWaermeproduktion
            // 
            this.textBox_WPWaermeproduktion.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_WPWaermeproduktion.Enabled = false;
            this.textBox_WPWaermeproduktion.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.textBox_WPWaermeproduktion.Location = new System.Drawing.Point(255, 246);
            this.textBox_WPWaermeproduktion.Name = "textBox_WPWaermeproduktion";
            this.textBox_WPWaermeproduktion.Size = new System.Drawing.Size(84, 22);
            this.textBox_WPWaermeproduktion.TabIndex = 219;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.BackColor = System.Drawing.SystemColors.Control;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.ForeColor = System.Drawing.Color.Black;
            this.label18.Location = new System.Drawing.Point(28, 277);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(156, 17);
            this.label18.TabIndex = 232;
            this.label18.Text = "Wärmeproduktion BHKW:";
            this.label18.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_SWWaermeproduktion
            // 
            this.textBox_SWWaermeproduktion.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_SWWaermeproduktion.Enabled = false;
            this.textBox_SWWaermeproduktion.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_SWWaermeproduktion.ForeColor = System.Drawing.Color.Black;
            this.textBox_SWWaermeproduktion.Location = new System.Drawing.Point(256, 309);
            this.textBox_SWWaermeproduktion.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_SWWaermeproduktion.Name = "textBox_SWWaermeproduktion";
            this.textBox_SWWaermeproduktion.Size = new System.Drawing.Size(84, 25);
            this.textBox_SWWaermeproduktion.TabIndex = 233;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.BackColor = System.Drawing.SystemColors.Control;
            this.label19.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.ForeColor = System.Drawing.Color.Black;
            this.label19.Location = new System.Drawing.Point(345, 311);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(51, 17);
            this.label19.TabIndex = 234;
            this.label19.Text = "MWh/a";
            this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.BackColor = System.Drawing.SystemColors.Control;
            this.label20.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.ForeColor = System.Drawing.Color.Black;
            this.label20.Location = new System.Drawing.Point(28, 245);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(139, 17);
            this.label20.TabIndex = 235;
            this.label20.Text = "Wärmeproduktion WP:";
            this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.BackColor = System.Drawing.SystemColors.Control;
            this.label21.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.ForeColor = System.Drawing.Color.Black;
            this.label21.Location = new System.Drawing.Point(28, 313);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(93, 17);
            this.label21.TabIndex = 236;
            this.label21.Text = "Solare Wärme:";
            this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_SPKWaermeproduktion
            // 
            this.textBox_SPKWaermeproduktion.BackColor = System.Drawing.SystemColors.Window;
            this.textBox_SPKWaermeproduktion.Enabled = false;
            this.textBox_SPKWaermeproduktion.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_SPKWaermeproduktion.ForeColor = System.Drawing.Color.Black;
            this.textBox_SPKWaermeproduktion.Location = new System.Drawing.Point(255, 342);
            this.textBox_SPKWaermeproduktion.Margin = new System.Windows.Forms.Padding(4);
            this.textBox_SPKWaermeproduktion.Name = "textBox_SPKWaermeproduktion";
            this.textBox_SPKWaermeproduktion.Size = new System.Drawing.Size(84, 25);
            this.textBox_SPKWaermeproduktion.TabIndex = 237;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.BackColor = System.Drawing.SystemColors.Control;
            this.label22.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label22.ForeColor = System.Drawing.Color.Black;
            this.label22.Location = new System.Drawing.Point(345, 344);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(51, 17);
            this.label22.TabIndex = 238;
            this.label22.Text = "MWh/a";
            this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.BackColor = System.Drawing.SystemColors.Control;
            this.label23.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label23.ForeColor = System.Drawing.Color.Black;
            this.label23.Location = new System.Drawing.Point(345, 246);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(51, 17);
            this.label23.TabIndex = 239;
            this.label23.Text = "MWh/a";
            this.label23.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label24.ForeColor = System.Drawing.Color.Black;
            this.label24.Location = new System.Drawing.Point(706, 302);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(51, 17);
            this.label24.TabIndex = 242;
            this.label24.Text = "MWh/a";
            // 
            // textBox_BHKWStromproduktion
            // 
            this.textBox_BHKWStromproduktion.Enabled = false;
            this.textBox_BHKWStromproduktion.Font = new System.Drawing.Font("Segoe UI Semibold", 8F);
            this.textBox_BHKWStromproduktion.Location = new System.Drawing.Point(611, 300);
            this.textBox_BHKWStromproduktion.Name = "textBox_BHKWStromproduktion";
            this.textBox_BHKWStromproduktion.Size = new System.Drawing.Size(89, 22);
            this.textBox_BHKWStromproduktion.TabIndex = 241;
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label25.ForeColor = System.Drawing.Color.Black;
            this.label25.Location = new System.Drawing.Point(449, 303);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(150, 17);
            this.label25.TabIndex = 240;
            this.label25.Text = "Stromproduktion BHKW:";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label26.ForeColor = System.Drawing.Color.Black;
            this.label26.Location = new System.Drawing.Point(706, 330);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(51, 17);
            this.label26.TabIndex = 245;
            this.label26.Text = "MWh/a";
            // 
            // textBox_PVStromproduktion
            // 
            this.textBox_PVStromproduktion.Enabled = false;
            this.textBox_PVStromproduktion.Font = new System.Drawing.Font("Segoe UI Semibold", 8F);
            this.textBox_PVStromproduktion.Location = new System.Drawing.Point(611, 328);
            this.textBox_PVStromproduktion.Name = "textBox_PVStromproduktion";
            this.textBox_PVStromproduktion.Size = new System.Drawing.Size(89, 22);
            this.textBox_PVStromproduktion.TabIndex = 244;
            // 
            // label27
            // 
            this.label27.AutoSize = true;
            this.label27.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label27.ForeColor = System.Drawing.Color.Black;
            this.label27.Location = new System.Drawing.Point(449, 331);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(129, 17);
            this.label27.TabIndex = 243;
            this.label27.Text = "Stromproduktion PV:";
            // 
            // label28
            // 
            this.label28.AutoSize = true;
            this.label28.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label28.ForeColor = System.Drawing.Color.Black;
            this.label28.Location = new System.Drawing.Point(648, 468);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(51, 17);
            this.label28.TabIndex = 247;
            this.label28.Text = "MWh/a";
            // 
            // textBox_Reststrombedarf
            // 
            this.textBox_Reststrombedarf.BackColor = System.Drawing.Color.White;
            this.textBox_Reststrombedarf.Enabled = false;
            this.textBox_Reststrombedarf.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_Reststrombedarf.ForeColor = System.Drawing.Color.Black;
            this.textBox_Reststrombedarf.Location = new System.Drawing.Point(558, 465);
            this.textBox_Reststrombedarf.Name = "textBox_Reststrombedarf";
            this.textBox_Reststrombedarf.Size = new System.Drawing.Size(84, 25);
            this.textBox_Reststrombedarf.TabIndex = 246;
            // 
            // label29
            // 
            this.label29.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label29.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label29.ForeColor = System.Drawing.Color.Black;
            this.label29.Location = new System.Drawing.Point(437, 466);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(115, 22);
            this.label29.TabIndex = 248;
            this.label29.Text = "Reststrombedarf:";
            this.label29.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(716, 564);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 30);
            this.btn_OK.TabIndex = 249;
            this.btn_OK.Text = "Beenden";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.Control;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(29, 194);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 21);
            this.label1.TabIndex = 250;
            this.label1.Text = "Ergebnisse";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btn_Konfiguration
            // 
            this.btn_Konfiguration.Location = new System.Drawing.Point(599, 30);
            this.btn_Konfiguration.Name = "btn_Konfiguration";
            this.btn_Konfiguration.Size = new System.Drawing.Size(158, 33);
            this.btn_Konfiguration.TabIndex = 251;
            this.btn_Konfiguration.Text = "Konfiguration ...";
            this.btn_Konfiguration.UseVisualStyleBackColor = true;
            this.btn_Konfiguration.Click += new System.EventHandler(this.btn_Konfiguration_Click);
            // 
            // chart5
            // 
            this.chart5.BorderlineColor = System.Drawing.Color.Black;
            this.chart5.BorderlineDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.DashDot;
            chartArea1.Name = "ChartArea1";
            this.chart5.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart5.Legends.Add(legend1);
            this.chart5.Location = new System.Drawing.Point(21, 380);
            this.chart5.Name = "chart5";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Pie;
            series1.IsVisibleInLegend = false;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart5.Series.Add(series1);
            this.chart5.Size = new System.Drawing.Size(319, 177);
            this.chart5.TabIndex = 252;
            this.chart5.Text = "chart5";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(705, 358);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 17);
            this.label2.TabIndex = 255;
            this.label2.Text = "MWh/a";
            // 
            // textBox_SPKStromverbrauch
            // 
            this.textBox_SPKStromverbrauch.Enabled = false;
            this.textBox_SPKStromverbrauch.Font = new System.Drawing.Font("Segoe UI Semibold", 8F);
            this.textBox_SPKStromverbrauch.Location = new System.Drawing.Point(610, 356);
            this.textBox_SPKStromverbrauch.Name = "textBox_SPKStromverbrauch";
            this.textBox_SPKStromverbrauch.Size = new System.Drawing.Size(89, 22);
            this.textBox_SPKStromverbrauch.TabIndex = 254;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Location = new System.Drawing.Point(448, 359);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(130, 17);
            this.label3.TabIndex = 253;
            this.label3.Text = "Stromverbrauch SPK:";
            // 
            // Form_Simulation_Kurz
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(809, 606);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox_SPKStromverbrauch);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chart5);
            this.Controls.Add(this.btn_Konfiguration);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.label28);
            this.Controls.Add(this.textBox_Reststrombedarf);
            this.Controls.Add(this.label26);
            this.Controls.Add(this.textBox_PVStromproduktion);
            this.Controls.Add(this.label27);
            this.Controls.Add(this.label24);
            this.Controls.Add(this.textBox_BHKWStromproduktion);
            this.Controls.Add(this.label25);
            this.Controls.Add(this.label23);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.textBox_SPKWaermeproduktion);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.textBox_SWWaermeproduktion);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.label33);
            this.Controls.Add(this.textBox_HeizstabStromverbrauch);
            this.Controls.Add(this.label34);
            this.Controls.Add(this.label31);
            this.Controls.Add(this.textBox_WPStromverbrauch);
            this.Controls.Add(this.label32);
            this.Controls.Add(this.textBox_WPWaermeproduktion);
            this.Controls.Add(this.label63);
            this.Controls.Add(this.label81);
            this.Controls.Add(this.textBox_Restwaermebedarf);
            this.Controls.Add(this.label59);
            this.Controls.Add(this.textBoxBHKWWaermeproduktion);
            this.Controls.Add(this.label64);
            this.Controls.Add(this.btn_Simulation);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.textBox_gesStrombedarf);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label47);
            this.Controls.Add(this.textBox_gesWaermebedarf);
            this.Controls.Add(this.label62);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form_Simulation_Kurz";
            this.Text = "Schnellsimulation";
            ((System.ComponentModel.ISupportInitialize)(this.chart5)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label47;
        private System.Windows.Forms.TextBox textBox_gesWaermebedarf;
        private System.Windows.Forms.Label label62;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox_gesStrombedarf;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button btn_Simulation;
        private System.Windows.Forms.Label label63;
        private System.Windows.Forms.Label label81;
        private System.Windows.Forms.TextBox textBox_Restwaermebedarf;
        private System.Windows.Forms.Label label59;
        private System.Windows.Forms.TextBox textBoxBHKWWaermeproduktion;
        private System.Windows.Forms.Label label64;
        private System.Windows.Forms.Label label33;
        private System.Windows.Forms.TextBox textBox_HeizstabStromverbrauch;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Label label31;
        private System.Windows.Forms.TextBox textBox_WPStromverbrauch;
        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.TextBox textBox_WPWaermeproduktion;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox textBox_SWWaermeproduktion;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox textBox_SPKWaermeproduktion;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.TextBox textBox_BHKWStromproduktion;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.TextBox textBox_PVStromproduktion;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.TextBox textBox_Reststrombedarf;
        private System.Windows.Forms.Label label29;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Konfiguration;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart5;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox_SPKStromverbrauch;
        private System.Windows.Forms.Label label3;
    }
}