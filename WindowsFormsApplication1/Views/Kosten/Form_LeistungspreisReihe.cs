using System;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Editor der saisonalen Leistungspreis-Sätze eines Energieträgers (Etappe KD4,
    /// Konzept Kostendialoge § 7.1, Entscheidung FK6a): zwölf Monatswerte
    /// [€/(kW·Monat)] als Leistungspreis-Reihe nach dem Preisreihen-Muster
    /// (<c>Tab_Preisreihe</c>, Auflösung Monat, Einheit EUR/kW/Monat).
    ///
    /// <para><b>Ebenen.</b> Im Projektkontext entsteht eine PROJEKTREIHE (sie gilt
    /// vor der Stammreihe), im Katalogkontext (Projekt 0) die STAMMREIHE. Je
    /// (Träger, Ebene, Jahr) führt der Editor genau eine Reihe: „Übernehmen"
    /// ersetzt die Reihe gleichen Jahres, andere Jahre bleiben als Historie —
    /// der Rechner zieht das jüngste Jahr
    /// (<see cref="PreisreiheCtrl.ReadTraegerReihe"/>).</para>
    ///
    /// <para><b>Rechenwirkung.</b> Eine gepflegte Reihe gilt VOR dem konstanten
    /// Satz: <c>KostenEmissionRechner</c> summiert die zwölf Monatssätze und
    /// multipliziert mit der vorgehaltenen Anschlussleistung.</para>
    /// </summary>
    public partial class Form_LeistungspreisReihe : Form
    {
        private readonly PreisreiheCtrl _ctrl = new PreisreiheCtrl();
        private int _projektId;
        private int _idTraeger;
        private string _traegerName = "";

        /// <summary>Die geladene Reihe der EIGENEN Ebene (null = keine).</summary>
        private PreisreiheModel _eigene;

        public Form_LeistungspreisReihe()
        {
            InitializeComponent();
            // H7: Infoknopf in das Kopfband (pnlKopf, Dock Top, 40 hoch) - der Knopf
            // sitzt darin senkrecht mittig.
            InfoKnopf.Anbringen(this, ziel: pnlKopf);

            Text = T("KDLG_LPR_TITEL", "Saisonale Leistungspreis-Sätze");
            lblKopfTitel.Text = Text;
            lblEinheit.Text = "€/(kW·Monat)";
            lblJahr.Text = T("KDLG_LPR_JAHR", "Jahr:");
            btnLoeschen.Text = T("KDLG_LPR_LOESCHEN", "Reihe löschen");
            btnUebernehmen.Text = T("KDLG_LPR_UEBERNEHMEN", "Übernehmen");
            btnAbbrechen.Text = T("KDLG_LPR_ABBRECHEN", "Abbrechen");
            lblHinweis.Text = T("KDLG_LPR_HINWEIS_VORRANG",
                "Eine gepflegte Reihe gilt vor dem konstanten Satz.");

            // Monatsbeschriftungen aus der Kultur — keine zwölf Resource-Schlüssel.
            string[] monate = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
            Label[] lbl = MonatsLabels();
            for (int i = 0; i < 12; i++) lbl[i].Text = monate[i] + ":";

            numJahr.Value = DateTime.Now.Year;
        }

        /// <summary>Kontext setzen und vorhandene Werte laden — vor <c>ShowDialog</c>.</summary>
        public void SetControls(int projektId, int idTraeger, string traegerName)
        {
            _projektId = projektId > 0 ? projektId : 0;
            _idTraeger = idTraeger;
            _traegerName = traegerName ?? "";

            lblTraeger.Text = _traegerName + "  —  " + (_projektId > 0
                ? T("KDLG_LPR_EBENE_PROJEKT", "Projektreihe")
                : T("KDLG_LPR_EBENE_STAMM", "Stammreihe (Katalog)"));

            PreisreiheModel geltend = _ctrl.ReadTraegerReihe(_projektId, _idTraeger);
            bool eigeneEbene = geltend != null &&
                ((_projektId > 0 && geltend.ID_Projekt > 0) ||
                 (_projektId <= 0 && geltend.IstStamm));

            if (geltend != null)
            {
                // Auch eine fremde Ebene (Stammreihe im Projektkontext) wird als
                // Ausgangspunkt vorgelegt — gespeichert wird immer die eigene Ebene.
                double[] werte = _ctrl.ReadWerte(geltend.ID);
                NumericUpDown[] num = MonatsFelder();
                for (int i = 0; i < 12 && i < werte.Length; i++)
                    num[i].Value = (decimal)Math.Min(100000, Math.Max(0, werte[i]));
                numJahr.Value = Math.Min(2100, Math.Max(2000, geltend.Jahr));

                if (eigeneEbene) _eigene = geltend;
                else lblHinweis.Text = string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_LPR_HINWEIS_STAMM",
                      "Vorbelegt aus der Stammreihe ({0}); gespeichert wird eine Projektreihe, die vorgeht."),
                    geltend.Jahr);
            }

            btnLoeschen.Enabled = _eigene != null;
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            double[] werte = new double[12];
            double summe = 0;
            NumericUpDown[] num = MonatsFelder();
            for (int i = 0; i < 12; i++) { werte[i] = (double)num[i].Value; summe += werte[i]; }

            if (summe <= 0)
            {
                MessageBox.Show(T("KDLG_LPR_ALLES_NULL",
                        "Alle zwölf Sätze sind 0 — zum Entfernen der Reihe bitte „Reihe löschen“ verwenden."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int jahr = (int)numJahr.Value;

            // Reihe gleichen Jahres der eigenen Ebene ersetzen (andere Jahre = Historie).
            if (_eigene != null && _eigene.Jahr == jahr) _ctrl.Delete(_eigene.ID);

            PreisreiheModel kopf = new PreisreiheModel
            {
                ID_Projekt = _projektId,
                ID_Energietraeger = _idTraeger,
                Bezeichner = "Leistungspreis " + _traegerName,
                Jahr = jahr,
                Aufloesung = DbWerte.PREISREIHE_AUFLOESUNG_MONAT,
                Einheit = DbWerte.PREISREIHE_EINHEIT_EUR_KW_MONAT
            };

            if (_ctrl.Insert(kopf, werte) > 0)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            // Fehler hat PreisreiheCtrl bereits gemeldet; der Dialog bleibt offen.
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            if (_eigene == null) return;
            if (_ctrl.Delete(_eigene.ID))
            {
                _eigene = null;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private NumericUpDown[] MonatsFelder()
        {
            return new[] { numM1, numM2, numM3, numM4, numM5, numM6,
                           numM7, numM8, numM9, numM10, numM11, numM12 };
        }

        private Label[] MonatsLabels()
        {
            return new[] { lblM1, lblM2, lblM3, lblM4, lblM5, lblM6,
                           lblM7, lblM8, lblM9, lblM10, lblM11, lblM12 };
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
