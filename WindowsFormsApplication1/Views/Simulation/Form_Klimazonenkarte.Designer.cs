namespace WindowsFormsApplication1
{
    partial class Form_Klimazonenkarte
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

        // Geometrie (Segoe UI 9 pt, 96 dpi, DpiUnaware):
        //
        // * ClientSize 700 x 760 - dieselbe Breite wie der aufrufende
        //   Erdreich-Dialog; die Hoehe bleibt unter dessen 748 + Fensterrahmen und
        //   damit im 1366x768-Rahmen, den FensterEinpassung notfalls klemmt.
        // * _karte (12/12, 676 x 694, alle vier Anker): Die SVG-Box der Karte ist
        //   1303,65 x 1349,50 (Verhaeltnis 0,966) - bei 694 px Hoehe zeichnet das
        //   Control 670 px breit und zentriert mit 3 px Rand je Seite. Das Fenster
        //   ist absichtlich GROESSENVERAENDERLICH: Mehr Fenster = mehr Karte, die
        //   Staedtenamen des PNG werden ab etwa 900 px Fensterhoehe gut lesbar.
        // * _lblGewaehlt (12/725, AutoSize, Anker unten links) traegt die
        //   Statuszeile; der Echttext unten ist der laengste Fall (Leerauswahl,
        //   gemessen 292 px - endet bei 304, weit vor _btnOk bei 458).
        // * Fussknoepfe nach Hausnorm 110 x 30 (Anker unten rechts): _btnOk 458/718,
        //   _btnAbbruch 578/718 - rechte Kante 688 = ClientSize.Width - 12,
        //   10 px Knopfabstand; Unterkante 748, also 12 px Luft.
        // * MinimumSize 560 x 620 (Aussenmass), damit Karte und Fusszeile nie
        //   kollidieren; FormBorderStyle Sizable, StartPosition CenterParent.

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this._karte = new WindowsFormsApplication1.KlimazonenKarte();
            this._lblGewaehlt = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnAbbruch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _karte
            //
            this._karte.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._karte.BackColor = System.Drawing.Color.White;
            this._karte.Location = new System.Drawing.Point(12, 12);
            this._karte.Name = "_karte";
            this._karte.Size = new System.Drawing.Size(676, 694);
            this._karte.TabIndex = 0;
            this._karte.ZoneGewaehlt += new System.EventHandler(this.karte_ZoneGewaehlt);
            this._karte.ZoneUebernommen += new System.EventHandler(this.karte_ZoneUebernommen);
            //
            // _lblGewaehlt
            //
            this._lblGewaehlt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._lblGewaehlt.AutoSize = true;
            this._lblGewaehlt.Location = new System.Drawing.Point(12, 725);
            this._lblGewaehlt.Name = "_lblGewaehlt";
            this._lblGewaehlt.Text = "Noch keine Zone gewählt — eine Zonenfläche auf der Karte anklicken.";
            //
            // _btnOk
            //
            this._btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.Location = new System.Drawing.Point(458, 718);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(110, 30);
            this._btnOk.TabIndex = 1;
            this._btnOk.Text = "OK";
            //
            // _btnAbbruch
            //
            this._btnAbbruch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnAbbruch.Location = new System.Drawing.Point(578, 718);
            this._btnAbbruch.Name = "_btnAbbruch";
            this._btnAbbruch.Size = new System.Drawing.Size(110, 30);
            this._btnAbbruch.TabIndex = 2;
            this._btnAbbruch.Text = "Abbrechen";
            //
            // Form_Klimazonenkarte
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnAbbruch;
            this.ClientSize = new System.Drawing.Size(700, 760);
            this.Controls.Add(this._karte);
            this.Controls.Add(this._lblGewaehlt);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnAbbruch);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(560, 620);
            this.Name = "Form_Klimazonenkarte";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Klimazonen nach DIN 4710";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private WindowsFormsApplication1.KlimazonenKarte _karte;
        private System.Windows.Forms.Label _lblGewaehlt;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnAbbruch;
    }
}
