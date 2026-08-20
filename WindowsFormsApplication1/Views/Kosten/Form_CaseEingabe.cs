using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_CaseEingabe : Form
    {
        private KostenPosition _daten;

        /// <summary>
        /// ETAPPE K5 (Konzept § 7.4, L7): Schalter „diese Position ist ein Zuschuss".
        /// <c>null</c>, wo die Kostenart nicht angeboten wird.
        /// </summary>
        private CheckBox _chkZuschuss;

        public Form_CaseEingabe()
        {
            InitializeComponent();
        }

        public Form_CaseEingabe(KostenPosition daten)
        {
            InitializeComponent();
            _daten = daten;

            // Werte beim Laden anzeigen
            numBestCase.Value = _daten.BestCase;
            numWorstCase.Value = _daten.WorstCase;
            numBestCase_Nutzungsdauer.Value = _daten.BestCase_Nutzungsdauer;
            numWorstCase_Nutzungsdauer.Value = _daten.WorstCase_Nutzungsdauer;

            ZuschussSchalterAnlegen();
        }

        /// <summary>
        /// Hängt den Zuschuss-Schalter unter die vier Szenariofelder.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum hier.</b> Für die Kostenart einer INVESTITIONSposition gab es bis K5
        /// überhaupt keine Oberfläche — Migrationsschritt 19b belegt alle Zeilen der
        /// Kategorie 1 mit <c>KAPITALGEBUNDEN</c> vor, und geändert hat sie danach
        /// niemand mehr (die Betriebskosten pflegen ihre Kostenart über
        /// <c>Form_Betriebskosten</c>). Dieser Dialog ist die einzige Stelle, die es je
        /// Position bereits gibt: Er hängt am „+/−"-Knopf JEDER Zeile und schreibt schon
        /// heute in dasselbe <see cref="KostenPosition"/>-Objekt, das
        /// <c>Form_Kosten.UpdateSingleRowInDatabase</c> danach speichert. Eine eigene
        /// Maske für ein einziges Kästchen wäre ein Dialog zu viel.
        /// </para>
        /// <para>
        /// <b>Programmatisch, nicht im Designer.</b> Dieselbe Hausregel wie in Etappe K4:
        /// Die generierte Datei bleibt unberührt, damit ein späterer Designer-Lauf die
        /// Ergänzung nicht wieder herauswirft. Das Fenster wächst um die Zeile mit.
        /// </para>
        /// <para>
        /// <b>Nur bei Investitions-NEBENpositionen.</b> Ein Zuschuss mindert die
        /// Anfangsauszahlung — bei einer Betriebs- oder Energieposition hätte die
        /// Kostenart keine Rechenwirkung und wäre ein Versprechen, das der Rechenweg
        /// nicht einlöst (laufende Erlöse haben mit <c>IstErloes</c> ihren eigenen Weg).
        /// Die HAUPTposition scheidet ebenfalls aus: Sie ist der Anlagenpreis selbst, und
        /// sie zum Zuschuss zu erklären hiesse, die Komponente aus der Investition zu
        /// nehmen und gleichzeitig als Förderung zu buchen.
        /// </para>
        /// </remarks>
        private void ZuschussSchalterAnlegen()
        {
            if (_daten == null || _daten.IsMainComponent) return;

            // Erkennungsmerkmal des Investitionsreiters ist die Kostenart: Seit Schritt
            // 19b tragen Kategorie-1-Zeilen KAPITALGEBUNDEN (oder bereits ZUSCHUSS).
            // Eine LEERE Kostenart zählt mit — sonst bliebe der Schalter in einer nie
            // migrierten Datenbank für immer verborgen.
            bool investition = _daten.IstZuschuss ||
                               string.IsNullOrEmpty(_daten.Kostenart) ||
                               string.Equals(_daten.Kostenart, DbWerte.KOSTENART_KAPITALGEBUNDEN,
                                             StringComparison.OrdinalIgnoreCase);
            if (!investition) return;

            int y = btn_OK.Top;
            int links = numBestCase.Left;

            _chkZuschuss = new CheckBox
            {
                Name = "chkZuschuss",
                Text = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS,
                AutoSize = true,
                Location = new Point(links, y),
                Checked = _daten.IstZuschuss,
                ForeColor = Color.FromArgb(0x1B, 0x5E, 0x20)
            };

            var hinweis = new Label
            {
                Name = "lblZuschussHinweis",
                Text = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS_HINT,
                AutoSize = false,
                Size = new Size(Math.Max(120, ClientSize.Width - links - 12), 34),
                Location = new Point(links, y + _chkZuschuss.PreferredSize.Height + 2),
                ForeColor = Color.DimGray
            };

            Controls.Add(_chkZuschuss);
            Controls.Add(hinweis);

            // Die Knöpfe rücken unter den neuen Block, das Fenster wächst mit.
            int zuwachs = hinweis.Bottom + 10 - y;
            btn_OK.Top += zuwachs;
            btn_Abbrechen.Top += zuwachs;
            Height += zuwachs;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            _daten.BestCase = numBestCase.Value;
            _daten.WorstCase = numWorstCase.Value;
            _daten.BestCase_Nutzungsdauer = numBestCase_Nutzungsdauer.Value;
            _daten.WorstCase_Nutzungsdauer = numWorstCase_Nutzungsdauer.Value;
            if (_chkZuschuss != null) _daten.IstZuschuss = _chkZuschuss.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
