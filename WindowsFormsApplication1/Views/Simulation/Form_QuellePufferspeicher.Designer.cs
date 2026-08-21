namespace WindowsFormsApplication1
{
    partial class Form_QuellePufferspeicher
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
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
            this._lblKopf = new System.Windows.Forms.Label();
            this._lbSpeicher = new System.Windows.Forms.ListBox();
            this._lblDaten = new System.Windows.Forms.Label();
            this._lblLeer = new System.Windows.Forms.Label();
            this._btnPufferAnlegen = new System.Windows.Forms.Button();
            this._gbParameter = new System.Windows.Forms.GroupBox();
            this._lblQuelltemperatur = new System.Windows.Forms.Label();
            this._tbTemperatur = new System.Windows.Forms.TextBox();
            this._lblSpreizung = new System.Windows.Forms.Label();
            this._tbSpreizung = new System.Windows.Forms.TextBox();
            this._lblRegeneration = new System.Windows.Forms.Label();
            this._tbRegeneration = new System.Windows.Forms.TextBox();
            this._lblKapazitaet = new System.Windows.Forms.Label();
            this._cbUnbegrenzt = new System.Windows.Forms.CheckBox();
            this._lblHinweisArt = new System.Windows.Forms.Label();
            this._lblKaskade = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnAbbruch = new System.Windows.Forms.Button();
            this._gbParameter.SuspendLayout();
            this.SuspendLayout();
            //
            // _lblKopf
            //
            this._lblKopf.AutoSize = true;
            this._lblKopf.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblKopf.Location = new System.Drawing.Point(14, 12);
            this._lblKopf.Name = "_lblKopf";
            this._lblKopf.Text = "Pufferspeicher als Wärmequelle auswählen:";
            //
            // _lbSpeicher
            //
            this._lbSpeicher.Location = new System.Drawing.Point(14, 38);
            this._lbSpeicher.Name = "_lbSpeicher";
            this._lbSpeicher.Size = new System.Drawing.Size(300, 200);
            this._lbSpeicher.SelectedIndexChanged += new System.EventHandler(this.lbSpeicher_SelectedIndexChanged);
            //
            // _lblDaten
            //
            this._lblDaten.AutoSize = false;
            this._lblDaten.Location = new System.Drawing.Point(330, 38);
            this._lblDaten.Name = "_lblDaten";
            this._lblDaten.Size = new System.Drawing.Size(275, 84);
            //
            // _lblLeer
            //
            this._lblLeer.AutoSize = false;
            this._lblLeer.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblLeer.Location = new System.Drawing.Point(14, 240);
            this._lblLeer.Name = "_lblLeer";
            this._lblLeer.Size = new System.Drawing.Size(300, 48);
            //
            // _btnPufferAnlegen
            //
            this._btnPufferAnlegen.Location = new System.Drawing.Point(330, 244);
            this._btnPufferAnlegen.Name = "_btnPufferAnlegen";
            this._btnPufferAnlegen.Size = new System.Drawing.Size(275, 30);
            this._btnPufferAnlegen.Text = "Pufferspeicher anlegen…";
            this._btnPufferAnlegen.Click += new System.EventHandler(this.btnPufferAnlegen_Click);
            //
            // _lblQuelltemperatur
            //
            this._lblQuelltemperatur.AutoSize = true;
            this._lblQuelltemperatur.Location = new System.Drawing.Point(16, 30);
            this._lblQuelltemperatur.Name = "_lblQuelltemperatur";
            this._lblQuelltemperatur.Text = "Quelltemperatur [°C]:";
            //
            // _tbTemperatur
            //
            this._tbTemperatur.Location = new System.Drawing.Point(180, 27);
            this._tbTemperatur.Name = "_tbTemperatur";
            this._tbTemperatur.Width = 80;
            this._tbTemperatur.TextChanged += new System.EventHandler(this.tbTemperatur_TextChanged);
            //
            // _lblSpreizung
            //
            this._lblSpreizung.AutoSize = true;
            this._lblSpreizung.Location = new System.Drawing.Point(16, 62);
            this._lblSpreizung.Name = "_lblSpreizung";
            this._lblSpreizung.Text = "nutzbare Spreizung [K]:";
            //
            // _tbSpreizung
            //
            this._tbSpreizung.Location = new System.Drawing.Point(180, 59);
            this._tbSpreizung.Name = "_tbSpreizung";
            this._tbSpreizung.Width = 80;
            this._tbSpreizung.TextChanged += new System.EventHandler(this.tbSpreizung_TextChanged);
            //
            // _lblRegeneration
            //
            this._lblRegeneration.AutoSize = true;
            this._lblRegeneration.Location = new System.Drawing.Point(16, 94);
            this._lblRegeneration.Name = "_lblRegeneration";
            this._lblRegeneration.Text = "Regeneration [kW]:";
            //
            // _tbRegeneration
            //
            this._tbRegeneration.Location = new System.Drawing.Point(180, 91);
            this._tbRegeneration.Name = "_tbRegeneration";
            this._tbRegeneration.Width = 80;
            //
            // _lblKapazitaet
            //
            this._lblKapazitaet.AutoSize = false;
            this._lblKapazitaet.Location = new System.Drawing.Point(285, 28);
            this._lblKapazitaet.Name = "_lblKapazitaet";
            this._lblKapazitaet.Size = new System.Drawing.Size(290, 40);
            //
            // _cbUnbegrenzt
            //
            this._cbUnbegrenzt.AutoSize = true;
            this._cbUnbegrenzt.Location = new System.Drawing.Point(16, 122);
            this._cbUnbegrenzt.Name = "_cbUnbegrenzt";
            this._cbUnbegrenzt.Text = "Quelle unbegrenzt verfügbar (nur Temperatur maßgeblich)";
            //
            // _gbParameter
            //
            this._gbParameter.Controls.Add(this._lblQuelltemperatur);
            this._gbParameter.Controls.Add(this._tbTemperatur);
            this._gbParameter.Controls.Add(this._lblSpreizung);
            this._gbParameter.Controls.Add(this._tbSpreizung);
            this._gbParameter.Controls.Add(this._lblRegeneration);
            this._gbParameter.Controls.Add(this._tbRegeneration);
            this._gbParameter.Controls.Add(this._lblKapazitaet);
            this._gbParameter.Controls.Add(this._cbUnbegrenzt);
            this._gbParameter.Location = new System.Drawing.Point(14, 296);
            this._gbParameter.Name = "_gbParameter";
            this._gbParameter.Size = new System.Drawing.Size(590, 156);
            this._gbParameter.Text = "Parameter der Wärmequelle";
            //
            // _lblHinweisArt
            //
            this._lblHinweisArt.AutoSize = false;
            this._lblHinweisArt.Location = new System.Drawing.Point(330, 132);
            this._lblHinweisArt.Name = "_lblHinweisArt";
            this._lblHinweisArt.Size = new System.Drawing.Size(275, 105);
            this._lblHinweisArt.Text = "Die Wärmepumpe entzieht dem Speicher je Stunde die Verdampferwärme (Wärmeproduktio" +
    "n − Stromaufnahme).\r\n\r\nIst der Speicher leer, wird die Leistung der Wärmepumpe be" +
    "grenzt; die Regeneration lädt den Speicher laufend nach.";
            //
            // _lblKaskade
            //
            this._lblKaskade.AutoSize = false;
            this._lblKaskade.Location = new System.Drawing.Point(14, 296);
            this._lblKaskade.Name = "_lblKaskade";
            this._lblKaskade.Size = new System.Drawing.Size(590, 156);
            this._lblKaskade.Text = "Kaskade (Heizkessel): Der Kessel bezieht seine Eintrittstemperatur aus dem gewählt" +
    "en Pufferspeicher statt aus dem Systemrücklauf.\r\n\r\nAnteil = (Vorlauf des Puffers " +
    "− Rücklauf des Kessels) / (Vorlauf des Kessels − Rücklauf des Kessels)\r\n\r\nUm dies" +
    "en Anteil der Nutzwärme sinkt der Brennstoffbedarf; die Entnahme ist zugleich ein" +
    "e Entladung des Speichers. Liefert der Puffer weniger, springt Brennstoff für den" +
    " Fehlbetrag ein. Der Kessel rechnet nach dem Erzeuger, der den Puffer lädt.";
            this._lblKaskade.Visible = false;
            //
            // _btnOk
            //
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.Location = new System.Drawing.Point(378, 466);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(110, 30);
            this._btnOk.Text = "OK";
            this._btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // _btnAbbruch
            //
            this._btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnAbbruch.Location = new System.Drawing.Point(498, 466);
            this._btnAbbruch.Name = "_btnAbbruch";
            this._btnAbbruch.Size = new System.Drawing.Size(110, 30);
            this._btnAbbruch.Text = "Abbrechen";
            //
            // Form_QuellePufferspeicher
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnAbbruch;
            this.ClientSize = new System.Drawing.Size(620, 508);
            this.Controls.Add(this._lblKopf);
            this.Controls.Add(this._lbSpeicher);
            this.Controls.Add(this._lblDaten);
            this.Controls.Add(this._lblLeer);
            this.Controls.Add(this._btnPufferAnlegen);
            this.Controls.Add(this._gbParameter);
            this.Controls.Add(this._lblHinweisArt);
            this.Controls.Add(this._lblKaskade);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnAbbruch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_QuellePufferspeicher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Wärmequelle Pufferspeicher";
            this._gbParameter.ResumeLayout(false);
            this._gbParameter.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblKopf;
        private System.Windows.Forms.ListBox _lbSpeicher;
        private System.Windows.Forms.Label _lblDaten;
        private System.Windows.Forms.Label _lblLeer;
        private System.Windows.Forms.Button _btnPufferAnlegen;
        private System.Windows.Forms.GroupBox _gbParameter;
        private System.Windows.Forms.Label _lblQuelltemperatur;
        private System.Windows.Forms.TextBox _tbTemperatur;
        private System.Windows.Forms.Label _lblSpreizung;
        private System.Windows.Forms.TextBox _tbSpreizung;
        private System.Windows.Forms.Label _lblRegeneration;
        private System.Windows.Forms.TextBox _tbRegeneration;
        private System.Windows.Forms.Label _lblKapazitaet;
        private System.Windows.Forms.CheckBox _cbUnbegrenzt;
        private System.Windows.Forms.Label _lblHinweisArt;
        private System.Windows.Forms.Label _lblKaskade;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnAbbruch;
    }
}
