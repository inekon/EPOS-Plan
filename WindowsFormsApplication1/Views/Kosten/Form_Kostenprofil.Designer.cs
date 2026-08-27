namespace WindowsFormsApplication1
{
    partial class Form_Kostenprofil
    {
        /// <summary>Erforderliche Designervariable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Verwendete Ressourcen bereinigen.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.tbBezeichner = new System.Windows.Forms.TextBox();
            this.tabs = new System.Windows.Forms.TabControl();
            this.tpMonat = new System.Windows.Forms.TabPage();
            this.lblKopfMonat = new System.Windows.Forms.Label();
            this.btnAlleMonate = new System.Windows.Forms.Button();
            this.tpWoche = new System.Windows.Forms.TabPage();
            this.lblKopfWoche = new System.Windows.Forms.Label();
            this.lblWochentag = new System.Windows.Forms.Label();
            this.lbTag = new System.Windows.Forms.ListBox();
            this.btnTagKopieren = new System.Windows.Forms.Button();
            this.btnTagEinfuegen = new System.Windows.Forms.Button();
            this.btnAlleTage = new System.Windows.Forms.Button();
            this.btnTagUebernehmen = new System.Windows.Forms.Button();
            this.lblHinweisAbweichung = new System.Windows.Forms.Label();
            this.tpGrafik = new System.Windows.Forms.TabPage();
            this.chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbruch = new System.Windows.Forms.Button();
            this.tabs.SuspendLayout();
            this.tpMonat.SuspendLayout();
            this.tpWoche.SuspendLayout();
            this.tpGrafik.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).BeginInit();
            this.SuspendLayout();
            //
            // lblInfo
            //
            this.lblInfo.Location = new System.Drawing.Point(12, 10);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(676, 34);
            this.lblInfo.TabIndex = 0;
            this.lblInfo.Text = "Preisniveau je Monat und Tagesgang je Woche [ct/kWh].";
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 50);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(70, 15);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "Bezeichner:";
            //
            // tbBezeichner
            //
            this.tbBezeichner.Location = new System.Drawing.Point(120, 47);
            this.tbBezeichner.Name = "tbBezeichner";
            this.tbBezeichner.Size = new System.Drawing.Size(400, 23);
            this.tbBezeichner.TabIndex = 2;
            //
            // tabs
            //
            this.tabs.Controls.Add(this.tpMonat);
            this.tabs.Controls.Add(this.tpWoche);
            this.tabs.Controls.Add(this.tpGrafik);
            this.tabs.Location = new System.Drawing.Point(12, 80);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(676, 450);
            this.tabs.TabIndex = 3;
            this.tabs.SelectedIndexChanged += new System.EventHandler(this.tabs_SelectedIndexChanged);
            //
            // tpMonat
            //
            this.tpMonat.Controls.Add(this.lblKopfMonat);
            this.tpMonat.Controls.Add(this.btnAlleMonate);
            this.tpMonat.Name = "tpMonat";
            this.tpMonat.Padding = new System.Windows.Forms.Padding(3);
            this.tpMonat.Text = "Monatswerte";
            this.tpMonat.UseVisualStyleBackColor = true;
            //
            // lblKopfMonat
            //
            this.lblKopfMonat.AutoSize = true;
            this.lblKopfMonat.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblKopfMonat.Location = new System.Drawing.Point(20, 18);
            this.lblKopfMonat.Name = "lblKopfMonat";
            this.lblKopfMonat.Size = new System.Drawing.Size(180, 15);
            this.lblKopfMonat.TabIndex = 0;
            this.lblKopfMonat.Text = "Preisniveau je Monat [ct/kWh]";
            //
            // btnAlleMonate
            //
            this.btnAlleMonate.Location = new System.Drawing.Point(30, 340);
            this.btnAlleMonate.Name = "btnAlleMonate";
            this.btnAlleMonate.Size = new System.Drawing.Size(250, 25);
            this.btnAlleMonate.TabIndex = 1;
            this.btnAlleMonate.Text = "Januar-Wert für alle Monate übernehmen";
            this.btnAlleMonate.UseVisualStyleBackColor = true;
            this.btnAlleMonate.Click += new System.EventHandler(this.btnAlleMonate_Click);
            //
            // tpWoche
            //
            this.tpWoche.Controls.Add(this.lblKopfWoche);
            this.tpWoche.Controls.Add(this.lblWochentag);
            this.tpWoche.Controls.Add(this.lbTag);
            this.tpWoche.Controls.Add(this.btnTagKopieren);
            this.tpWoche.Controls.Add(this.btnTagEinfuegen);
            this.tpWoche.Controls.Add(this.btnAlleTage);
            this.tpWoche.Controls.Add(this.btnTagUebernehmen);
            this.tpWoche.Controls.Add(this.lblHinweisAbweichung);
            this.tpWoche.Name = "tpWoche";
            this.tpWoche.Padding = new System.Windows.Forms.Padding(3);
            this.tpWoche.Text = "Wochenwerte";
            this.tpWoche.UseVisualStyleBackColor = true;
            //
            // lblKopfWoche
            //
            this.lblKopfWoche.AutoSize = true;
            this.lblKopfWoche.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblKopfWoche.Location = new System.Drawing.Point(20, 15);
            this.lblKopfWoche.Name = "lblKopfWoche";
            this.lblKopfWoche.Size = new System.Drawing.Size(220, 15);
            this.lblKopfWoche.TabIndex = 0;
            this.lblKopfWoche.Text = "Abweichung je Wochentag und Stunde";
            //
            // lblWochentag
            //
            this.lblWochentag.AutoSize = true;
            this.lblWochentag.Location = new System.Drawing.Point(490, 25);
            this.lblWochentag.Name = "lblWochentag";
            this.lblWochentag.Size = new System.Drawing.Size(70, 15);
            this.lblWochentag.TabIndex = 1;
            this.lblWochentag.Text = "Wochentag:";
            //
            // lbTag
            //
            this.lbTag.ItemHeight = 15;
            this.lbTag.Location = new System.Drawing.Point(490, 48);
            this.lbTag.Name = "lbTag";
            this.lbTag.Size = new System.Drawing.Size(150, 130);
            this.lbTag.TabIndex = 2;
            this.lbTag.SelectedIndexChanged += new System.EventHandler(this.lbTag_SelectedIndexChanged);
            //
            // btnTagKopieren
            //
            this.btnTagKopieren.Location = new System.Drawing.Point(490, 190);
            this.btnTagKopieren.Name = "btnTagKopieren";
            this.btnTagKopieren.Size = new System.Drawing.Size(150, 25);
            this.btnTagKopieren.TabIndex = 3;
            this.btnTagKopieren.Text = "Tag kopieren";
            this.btnTagKopieren.UseVisualStyleBackColor = true;
            this.btnTagKopieren.Click += new System.EventHandler(this.btnTagKopieren_Click);
            //
            // btnTagEinfuegen
            //
            this.btnTagEinfuegen.Location = new System.Drawing.Point(490, 222);
            this.btnTagEinfuegen.Name = "btnTagEinfuegen";
            this.btnTagEinfuegen.Size = new System.Drawing.Size(150, 25);
            this.btnTagEinfuegen.TabIndex = 4;
            this.btnTagEinfuegen.Text = "Tag einfügen";
            this.btnTagEinfuegen.UseVisualStyleBackColor = true;
            this.btnTagEinfuegen.Click += new System.EventHandler(this.btnTagEinfuegen_Click);
            //
            // btnAlleTage
            //
            this.btnAlleTage.Location = new System.Drawing.Point(490, 254);
            this.btnAlleTage.Name = "btnAlleTage";
            this.btnAlleTage.Size = new System.Drawing.Size(150, 25);
            this.btnAlleTage.TabIndex = 5;
            this.btnAlleTage.Text = "Für alle Tage";
            this.btnAlleTage.UseVisualStyleBackColor = true;
            this.btnAlleTage.Click += new System.EventHandler(this.btnAlleTage_Click);
            //
            // btnTagUebernehmen
            //
            this.btnTagUebernehmen.Location = new System.Drawing.Point(20, 340);
            this.btnTagUebernehmen.Name = "btnTagUebernehmen";
            this.btnTagUebernehmen.Size = new System.Drawing.Size(430, 25);
            this.btnTagUebernehmen.TabIndex = 6;
            this.btnTagUebernehmen.Text = "Stundenwerte für diesen Tag übernehmen";
            this.btnTagUebernehmen.UseVisualStyleBackColor = true;
            this.btnTagUebernehmen.Click += new System.EventHandler(this.btnTagUebernehmen_Click);
            //
            // lblHinweisAbweichung
            //
            this.lblHinweisAbweichung.Location = new System.Drawing.Point(20, 378);
            this.lblHinweisAbweichung.Name = "lblHinweisAbweichung";
            this.lblHinweisAbweichung.Size = new System.Drawing.Size(430, 34);
            this.lblHinweisAbweichung.TabIndex = 7;
            this.lblHinweisAbweichung.Text = "Die Abweichung wird zum Monatswert addiert.";
            //
            // tpGrafik
            //
            this.tpGrafik.Controls.Add(this.chart);
            this.tpGrafik.Name = "tpGrafik";
            this.tpGrafik.Padding = new System.Windows.Forms.Padding(3);
            this.tpGrafik.Text = "Grafik";
            this.tpGrafik.UseVisualStyleBackColor = true;
            //
            // chart
            //
            this.chart.Location = new System.Drawing.Point(10, 10);
            this.chart.Name = "chart";
            this.chart.Size = new System.Drawing.Size(648, 390);
            this.chart.TabIndex = 0;
            //
            // btnOk
            //
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(510, 540);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(85, 25);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnAbbruch
            //
            this.btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbruch.Location = new System.Drawing.Point(603, 540);
            this.btnAbbruch.Name = "btnAbbruch";
            this.btnAbbruch.Size = new System.Drawing.Size(85, 25);
            this.btnAbbruch.TabIndex = 5;
            this.btnAbbruch.Text = "Abbrechen";
            this.btnAbbruch.UseVisualStyleBackColor = true;
            //
            // Form_Kostenprofil
            //
            this.AcceptButton = this.btnOk;
            this.CancelButton = this.btnAbbruch;
            this.ClientSize = new System.Drawing.Size(700, 580);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.tbBezeichner);
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnAbbruch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Kostenprofil";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Kostenprofil";
            this.tabs.ResumeLayout(false);
            this.tpMonat.ResumeLayout(false);
            this.tpMonat.PerformLayout();
            this.tpWoche.ResumeLayout(false);
            this.tpWoche.PerformLayout();
            this.tpGrafik.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.chart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox tbBezeichner;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tpMonat;
        private System.Windows.Forms.Label lblKopfMonat;
        private System.Windows.Forms.Button btnAlleMonate;
        private System.Windows.Forms.TabPage tpWoche;
        private System.Windows.Forms.Label lblKopfWoche;
        private System.Windows.Forms.Label lblWochentag;
        private System.Windows.Forms.ListBox lbTag;
        private System.Windows.Forms.Button btnTagKopieren;
        private System.Windows.Forms.Button btnTagEinfuegen;
        private System.Windows.Forms.Button btnAlleTage;
        private System.Windows.Forms.Button btnTagUebernehmen;
        private System.Windows.Forms.Label lblHinweisAbweichung;
        private System.Windows.Forms.TabPage tpGrafik;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbruch;
    }
}
