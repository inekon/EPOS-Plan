using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zeileneditor einer Vorlagenposition (Etappe KD2, Konzept Kostendialoge
    /// Rev. 1.2, § 5.2 — Stift-Symbol): Bezeichnung, Kostenart nach VDI 2067,
    /// Erlös-Kennzeichen und Empfehlungsbereich. Nutzungsdauer und Satz stehen im
    /// Raster (FK4) und werden hier bewusst nicht gedoppelt.
    ///
    /// <para><b>Der Aufrufer schreibt</b> (Hausmuster <c>SetControls</c> vor
    /// <c>ShowDialog</c>): Das Formular füllt nur die übergebene Position.</para>
    /// </summary>
    public partial class Form_VorlagenPosition : Form
    {
        private static readonly string[] KOSTENARTEN =
        {
            DbWerte.KOSTENART_KAPITALGEBUNDEN,
            DbWerte.KOSTENART_BEDARFSGEBUNDEN,
            DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
            DbWerte.KOSTENART_SONSTIGE,
            DbWerte.KOSTENART_ZUSCHUSS,
        };

        private KostenVorlagenPosition _pos;

        public Form_VorlagenPosition()
        {
            InitializeComponent();

            foreach (string art in KOSTENARTEN)
                cmbKostenart.Items.Add(KostenartAnzeige(art));
        }

        /// <summary>Position vor <c>ShowDialog</c> übergeben.</summary>
        public void SetControls(KostenVorlagenPosition pos)
        {
            _pos = pos;
            txtBezeichnung.Text = pos.Bezeichnung;
            chkErloes.Checked = pos.IstErloes;
            txtEmpfVon.Text = pos.EmpfehlungVon.HasValue
                ? pos.EmpfehlungVon.Value.ToString("0.##") : "";
            txtEmpfBis.Text = pos.EmpfehlungBis.HasValue
                ? pos.EmpfehlungBis.Value.ToString("0.##") : "";

            int index = Array.IndexOf(KOSTENARTEN, pos.Kostenart ?? "");
            cmbKostenart.SelectedIndex = index >= 0 ? index : 3;   // Rückfall: sonstige
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            double von, bis;
            if (!Program.ZahlPruefen(txtEmpfVon, lblEmpfehlung.Text, out von, true)) return;
            if (!Program.ZahlPruefen(txtEmpfBis, lblEmpfehlung.Text, out bis, true)) return;

            string name = txtBezeichnung.Text.Trim();
            if (name.Length > 0) _pos.Bezeichnung = name;
            _pos.Kostenart = KOSTENARTEN[Math.Max(0, cmbKostenart.SelectedIndex)];
            _pos.IstErloes = chkErloes.Checked;
            _pos.EmpfehlungVon = string.IsNullOrWhiteSpace(txtEmpfVon.Text) ? (double?)null : von;
            _pos.EmpfehlungBis = string.IsNullOrWhiteSpace(txtEmpfBis.Text) ? (double?)null : bis;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Zahl_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private static string KostenartAnzeige(string persistenz)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString("KOSTENART_" + persistenz); }
            catch { }
            if (!string.IsNullOrEmpty(t)) return t;
            switch (persistenz)
            {
                case "KAPITALGEBUNDEN": return "kapitalgebunden";
                case "BEDARFSGEBUNDEN": return "bedarfsgebunden";
                case "BETRIEBSGEBUNDEN": return "betriebsgebunden";
                case "ZUSCHUSS": return "Zuschuss";
                default: return "sonstige";
            }
        }
    }
}
