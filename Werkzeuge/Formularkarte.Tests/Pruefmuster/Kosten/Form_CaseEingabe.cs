// Prüfmuster für Formularkarte — Stand vor iU9-W1.3 (f6e9264^); die Maske wurde durch
// EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor ersetzt.
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
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        // ---------------------------------------------------------------------
        // ETAPPE KD6 (Konzept § 11): Umschalter absolut/%-Eingabe und das
        // Startjahr je Position. Programmatisch nach dem K5-Muster darunter.
        // ---------------------------------------------------------------------

        /// <summary>%-Eingabemodus: die Szenariofelder tragen Abweichungen [%] zum
        /// Erwartungswert; persistiert werden weiterhin BETRÄGE (keine neuen
        /// Spalten, KL9).</summary>
        private RadioButton _rbAbsolut, _rbProzent;

        /// <summary>Live-Anzeige der resultierenden Beträge im %-Modus.</summary>
        private Label _lblUmrechnung;

        /// <summary>Startjahr der Position (FK10); 0 = sofort (t0).</summary>
        private NumericUpDown _numStartJahr;

        public Form_CaseEingabe(KostenPosition daten)
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            _daten = daten;

            // Werte beim Laden anzeigen
            numBestCase.Value = _daten.BestCase;
            numWorstCase.Value = _daten.WorstCase;
            numBestCase_Nutzungsdauer.Value = _daten.BestCase_Nutzungsdauer;
            numWorstCase_Nutzungsdauer.Value = _daten.WorstCase_Nutzungsdauer;

            ProzentUmschalterAnlegen();
            StartjahrZeileAnlegen();
            ZuschussSchalterAnlegen();
        }

        /// <summary>
        /// ETAPPE KD6 (§ 11): „absolut [€] oder relativ [%] zum Erwartungswert" —
        /// der Umschalter sitzt über den Szenariofeldern. Im %-Modus tragen die
        /// Felder Abweichungen (Best +x %, Worst −x % ist NICHT erzwungen — das
        /// Vorzeichen bestimmt der Anwender), die Live-Zeile zeigt die
        /// resultierenden Beträge; gespeichert werden IMMER Beträge.
        /// Ohne Erwartungswert (Betrag 0) bleibt der Modus gesperrt.
        /// </summary>
        private void ProzentUmschalterAnlegen()
        {
            int links = numBestCase.Left;
            int y = Math.Min(numBestCase.Top, numWorstCase.Top) - 26;

            _rbAbsolut = new RadioButton
            {
                Name = "rbCaseAbsolut",
                Text = Text_("KOSTEN_CASE_ABSOLUT", "Eingabe absolut [€]"),
                AutoSize = true,
                Location = new Point(links, y),
                Checked = true
            };
            _rbProzent = new RadioButton
            {
                Name = "rbCaseProzent",
                Text = Text_("KOSTEN_CASE_PROZENT", "Eingabe in % vom Erwartungswert"),
                AutoSize = true,
                Location = new Point(links + _rbAbsolut.PreferredSize.Width + 16, y),
                Enabled = _daten != null && _daten.Betrag != 0
            };
            _rbProzent.CheckedChanged += ProzentModusGewechselt;

            _lblUmrechnung = new Label
            {
                Name = "lblCaseUmrechnung",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(links, y - 18),
                Visible = false
            };

            Controls.Add(_rbAbsolut);
            Controls.Add(_rbProzent);
            Controls.Add(_lblUmrechnung);

            numBestCase.ValueChanged += delegate { UmrechnungAnzeigen(); };
            numWorstCase.ValueChanged += delegate { UmrechnungAnzeigen(); };
        }

        private void ProzentModusGewechselt(object sender, EventArgs e)
        {
            if (_daten == null || _daten.Betrag == 0) return;
            decimal basis = Math.Abs(_daten.Betrag);

            if (_rbProzent.Checked)
            {
                // Beträge → %-Abweichung (0 bleibt 0 = „nicht gepflegt").
                numBestCase.Minimum = -1000; numBestCase.Maximum = 1000;
                numWorstCase.Minimum = -1000; numWorstCase.Maximum = 1000;
                numBestCase.Value = _daten.BestCase == 0 ? 0
                    : Math.Round((_daten.BestCase - _daten.Betrag) / basis * 100m, 1);
                numWorstCase.Value = _daten.WorstCase == 0 ? 0
                    : Math.Round((_daten.WorstCase - _daten.Betrag) / basis * 100m, 1);
            }
            else
            {
                decimal best = ProzentZuBetrag(numBestCase.Value);
                decimal worst = ProzentZuBetrag(numWorstCase.Value);
                numBestCase.Minimum = 0; numBestCase.Maximum = 100000000;
                numWorstCase.Minimum = 0; numWorstCase.Maximum = 100000000;
                numBestCase.Value = Math.Max(0, best);
                numWorstCase.Value = Math.Max(0, worst);
            }
            UmrechnungAnzeigen();
        }

        private decimal ProzentZuBetrag(decimal prozent)
        {
            if (_daten == null || prozent == 0) return 0;   // 0 = nicht gepflegt
            return Math.Round(_daten.Betrag * (1m + prozent / 100m), 2);
        }

        private void UmrechnungAnzeigen()
        {
            bool zeigen = _rbProzent != null && _rbProzent.Checked;
            _lblUmrechnung.Visible = zeigen;
            if (!zeigen) return;
            _lblUmrechnung.Text = string.Format(
                Text_("KOSTEN_CASE_UMRECHNUNG", "ergibt: Best {0:N2} € · Worst {1:N2} €"),
                ProzentZuBetrag(numBestCase.Value), ProzentZuBetrag(numWorstCase.Value));
        }

        /// <summary>
        /// ETAPPE KD6 (§ 11, FK10): Startjahr je Position — 0 = sofort (t0),
        /// Jahr X ≥ 2 = Investition erst im Jahr X, Betriebskosten ab X
        /// (Rechenwirkung im KapitalwertRechner; Energiekosten bleiben
        /// Gesamtrechnung, Hinweis der Wirtschaftlichkeit).
        /// </summary>
        private void StartjahrZeileAnlegen()
        {
            if (_daten == null) return;

            int y = btn_OK.Top;
            int links = numBestCase.Left;

            var lbl = new Label
            {
                Name = "lblStartJahr",
                Text = Text_("KOSTEN_CASE_STARTJAHR", "Startjahr (0 = sofort; Jahr X: Zahlung/Betrieb ab X):"),
                AutoSize = true,
                Location = new Point(12, y + 3)
            };
            _numStartJahr = new NumericUpDown
            {
                Name = "numStartJahr",
                Location = new Point(Math.Max(links, lbl.PreferredSize.Width + 20), y),
                Size = new Size(70, 23),
                Maximum = 50,
                Value = Math.Min(50, Math.Max(0, _daten.StartJahr))
            };

            Controls.Add(lbl);
            Controls.Add(_numStartJahr);

            int zuwachs = _numStartJahr.Bottom + 10 - y;
            btn_OK.Top += zuwachs;
            btn_Abbrechen.Top += zuwachs;
            Height += zuwachs;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
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
            // KD6 (§ 11): Im %-Modus werden die Abweichungen in BETRÄGE übersetzt —
            // persistiert wird immer der Betrag (keine neuen Spalten, KL9).
            if (_rbProzent != null && _rbProzent.Checked)
            {
                _daten.BestCase = Math.Max(0, ProzentZuBetrag(numBestCase.Value));
                _daten.WorstCase = Math.Max(0, ProzentZuBetrag(numWorstCase.Value));
            }
            else
            {
                _daten.BestCase = numBestCase.Value;
                _daten.WorstCase = numWorstCase.Value;
            }
            _daten.BestCase_Nutzungsdauer = numBestCase_Nutzungsdauer.Value;
            _daten.WorstCase_Nutzungsdauer = numWorstCase_Nutzungsdauer.Value;
            if (_chkZuschuss != null) _daten.IstZuschuss = _chkZuschuss.Checked;
            if (_numStartJahr != null)
                _daten.StartJahr = _numStartJahr.Value > 1 ? (int)_numStartJahr.Value : 0;
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
