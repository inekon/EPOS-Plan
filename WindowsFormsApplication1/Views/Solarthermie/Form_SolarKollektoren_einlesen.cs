using System;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_SolarKollektoren_einlesen: Form
    {
        private Solarkollektorenlmport ctrl = new Solarkollektorenlmport();

        // Zuordnung: Position in der (gefilterten) ListBox -> Index in ctrl._list
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        // Sperre gegen Rueckkopplung: waehrend FuelleListe() die Markierung
        // wiederherstellt, feuert SelectedIndexChanged und wuerde die Detailfelder
        // auf einen Zwischenstand setzen.
        private bool _listeWirdGefuellt = false;

        public Form_SolarKollektoren_einlesen()
        {
            InitializeComponent();
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_Kollektoren.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Solarthermie");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.vdi)|*.vdi";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;

                ctrl.Import(filename);
                FuelleListe();
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_listeWirdGefuellt) return;   // Neuaufbau der Liste, kein Anwenderklick

            int sel = Liste_Kollektoren.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            ZeigeDetails(_anzeigeIndex[sel]);
        }

        // Uebertraegt einen VDI-Eintrag in die Detailfelder.
        private void ZeigeDetails(int i)
        {
            if (i < 0 || i >= ctrl._list.Count) return;

            textBox_Name.Text = ctrl._list[i].m_szName;
            textBox_Firma.Text = ctrl._list[i].m_szFirma;
            textBox_Bauart.Text = ctrl._list[i].m_szBauart;
            textBox_Leistung.Text = ctrl._list[i].m_Leistung.ToString();
            textBox_Aperturflaeche.Text = ctrl._list[i].m_Aperturfläche.ToString();
            textBox_a1.Text = ctrl._list[i].m_a1.ToString();
            textBox_a2.Text = ctrl._list[i].m_a2.ToString();
            textBox_h0.Text = ctrl._list[i].m_h0.ToString();
            textBox_Kdir.Text = ctrl._list[i].m_kdir.ToString();
            textBox_Kdiff.Text = ctrl._list[i].m_kdiff.ToString();
        }

        private void Kollektorfilter_ValueChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        // Live-Filter ueber Bezeichner und Firma (Anwenderanforderung 17.08.2026).
        private void Suchfilter_TextChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        private void FuelleListe()
        {
            double aMin = (double)num_AperturVon.Value;
            double aMax = (double)num_AperturBis.Value;
            string suche = txt_Filter.Text;

            // Markierung (echte Indizes) sichern und nach dem Neuaufbau fuer alle
            // weiterhin sichtbaren Eintraege wiederherstellen: so geht eine bereits
            // getroffene Auswahl beim Tippen im Filter nicht stillschweigend
            // verloren, und unsichtbare Eintraege bleiben unmarkiert.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();

            _listeWirdGefuellt = true;
            Liste_Kollektoren.BeginUpdate();
            Liste_Kollektoren.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                if (!VdiAuswahlFilter.Passt(suche, ctrl._list[i].m_szName, ctrl._list[i].m_szFirma)) continue;
                double apertur = ctrl._list[i].m_Aperturfläche;
                double leistung = ctrl._list[i].m_Leistung;
                if (apertur < aMin || apertur > aMax) continue;
                Liste_Kollektoren.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            for (int zeile = 0; zeile < _anzeigeIndex.Count; zeile++)
            {
                if (markiert.Contains(_anzeigeIndex[zeile])) Liste_Kollektoren.SetSelected(zeile, true);
            }
            Liste_Kollektoren.EndUpdate();
            _listeWirdGefuellt = false;

            // Detailfelder auf die verbleibende Markierung nachziehen, damit sie
            // nach dem Umfiltern nicht mehr auf einen ausgefilterten Eintrag
            // zeigen (bei der Uebernahme sind sie die Quelle bzw. die Anzeige).
            if (Liste_Kollektoren.SelectedIndex >= 0 && Liste_Kollektoren.SelectedIndex < _anzeigeIndex.Count)
                ZeigeDetails(_anzeigeIndex[Liste_Kollektoren.SelectedIndex]);
        }

        // Markierte Zeilen -> Indizes in ctrl._list.
        private System.Collections.Generic.List<int> MarkierteQuellIndizes()
        {
            return VdiAuswahlFilter.QuellIndizes(Liste_Kollektoren.SelectedIndices, _anzeigeIndex);
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            // Mehrfachselektion: je markierter Eintrag laeuft genau der bestehende
            // Einzel-Weg (UebernehmeEintrag) - nur in einer Schleife, damit es
            // keinen zweiten Schreibpfad in die STAMM-Tabelle gibt.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();
            if (markiert.Count == 0)
            {
                MessageBox.Show("Bitte einen Solarkollektor selektieren!");
                return;
            }

            string fehlertext;

            if (markiert.Count == 1)
            {
                // Einzelfall: Meldungen und Dialogverhalten bleiben wie im Bestand.
                VdiUebernahmeErgebnis einzel = UebernehmeEintrag(markiert[0], out fehlertext);
                if (einzel == VdiUebernahmeErgebnis.Duplikat)
                {
                    MessageBox.Show("Daten bereits eingelesen!");
                    return;
                }
                if (fehlertext != null)
                {
                    MessageBox.Show("Ein Fehler ist aufgetreten: " + fehlertext);
                    this.DialogResult = DialogResult.Cancel;
                    return;
                }
                if (einzel == VdiUebernahmeErgebnis.Gespeichert)
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                }
                Close();
                return;
            }

            int nGespeichert = 0;
            int nDuplikat = 0;
            int nFehler = 0;
            Cursor alt = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (int i in markiert)
                {
                    // Detailfelder je Eintrag mitfuehren, damit die Anzeige zu dem
                    // passt, was gerade geschrieben wird.
                    ZeigeDetails(i);

                    // Ein fehlerhafter Eintrag darf den Gesamtvorgang nicht abbrechen.
                    VdiUebernahmeErgebnis ergebnis = UebernehmeEintrag(i, out fehlertext);
                    if (ergebnis == VdiUebernahmeErgebnis.Gespeichert) nGespeichert++;
                    else if (ergebnis == VdiUebernahmeErgebnis.Duplikat) nDuplikat++;
                    else nFehler++;
                }
            }
            finally
            {
                Cursor = alt;
            }

            MessageBox.Show(VdiAuswahlFilter.LadeMeldung(nGespeichert, markiert.Count, nDuplikat, nFehler));

            // Wie im Bestand wird der Dialog nach erfolgreicher Uebernahme beendet;
            // ohne einen einzigen Treffer bleibt er offen, damit der Anwender
            // Filter und Auswahl korrigieren kann.
            if (nGespeichert > 0)
            {
                this.DialogResult = DialogResult.OK;
                Close();
            }
        }

        // Uebernahme genau eines VDI-Eintrags in Tab_Solarkollektoren_STAMM.
        // Unveraenderter Bestandsweg, nur mit Index als Parameter und Ergebnis als
        // Rueckgabewert statt MessageBox (die Meldung entscheidet der Aufrufer).
        private VdiUebernahmeErgebnis UebernehmeEintrag(int index, out string fehlertext)
        {
            fehlertext = null;

            // Duplikatpruefung direkt am Listeneintrag statt am Anzeigefeld -
            // beim Mehrfachladen ist textBox_Name nur eine Momentaufnahme.
            string checkSql = "SELECT COUNT(*) FROM [Tab_Solarkollektoren_STAMM] WHERE Bezeichner = ?";
            OleDbParameter checkParam = new OleDbParameter("?", ctrl._list[index].m_szName);
            object checkResult = DataRepository.ExecuteScalar(checkSql, checkParam);

            if (checkResult != null && Convert.ToInt32(checkResult) > 0) return VdiUebernahmeErgebnis.Duplikat;

            try
            {
                SolarkollektorenStammCtrl sctrl = new SolarkollektorenStammCtrl();
                if (sctrl.InsertFrom(InitDatensatzUpdate(index))) return VdiUebernahmeErgebnis.Gespeichert;
                return VdiUebernahmeErgebnis.Fehler;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Solarkollektors: " + ex.Message);
                fehlertext = ex.Message;
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        SolarkollektorenModel InitDatensatzUpdate(int index)
        {
            SolarkollektorenModel model = new SolarkollektorenModel();
            
            model.m_szKollektorname = ctrl._list[index].m_szName;
            model.m_szFirma = ctrl._list[index].m_szFirma;
            model.m_szBeschreibung = ctrl._list[index].m_szBeschreibung;
            model.m_szKollektortyp = ctrl._list[index].m_szBauart;
            model.m_h0 = ctrl._list[index].m_h0;
            model.m_k1 = ctrl._list[index].m_a1;
            model.m_k2 = ctrl._list[index].m_a2;
            model.m_Kdir = ctrl._list[index].m_kdir;
            model.m_Kdfu = ctrl._list[index].m_kdiff;
            model.m_Modulfläche = ctrl._list[index].m_Modulfläche;
            model.m_Aperturfläche = ctrl._list[index].m_Aperturfläche;

            return model;
        }

    }
}