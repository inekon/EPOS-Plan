using System;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_einlesen : Form
    {
        private PufferSpImport ctrl = new PufferSpImport();

        // Zuordnung: Position in der (gefilterten) ListBox -> Index in ctrl._list
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        // Sperre gegen Rueckkopplung: waehrend FuelleListe() die Markierung
        // wiederherstellt, feuert SelectedIndexChanged und wuerde die Detailfelder
        // auf einen Zwischenstand setzen.
        private bool _listeWirdGefuellt = false;

        public Form_PufferSp_einlesen ()
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

            Liste_PufferSp.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Pufferspeicher");

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

            int sel = Liste_PufferSp.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            ZeigeDetails(_anzeigeIndex[sel]);
        }

        // Uebertraegt einen VDI-Eintrag in die Detailfelder. Die Felder sind auch
        // beim Mehrfachladen der Traeger fuer die Uebernahme (InitDatensatzUpdate
        // liest sie aus) - damit bleibt es bei genau einem Schreibweg.
        private void ZeigeDetails(int i)
        {
            if (i < 0 || i >= ctrl._list.Count) return;

            textBox_Name.Text = ctrl._list[i].m_szName;
            textBox_Firma.Text = ctrl._list[i].m_szFirma;
            textBox_Volumen.Text = ctrl._list[i].m_szVolumen;
            textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
            textBox_Typ.Text = ctrl._list[i].m_szTyp;
        }

        private void Volumenfilter_ValueChanged(object sender, EventArgs e)
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
            double min = (double)num_VolumenVon.Value;
            double max = (double)num_VolumenBis.Value;
            string suche = txt_Filter.Text;

            // Markierung (echte Indizes) sichern und nach dem Neuaufbau fuer alle
            // weiterhin sichtbaren Eintraege wiederherstellen: so geht eine bereits
            // getroffene Auswahl beim Tippen im Filter nicht stillschweigend
            // verloren, und unsichtbare Eintraege bleiben unmarkiert.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();

            _listeWirdGefuellt = true;
            Liste_PufferSp.BeginUpdate();
            Liste_PufferSp.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                double volumen = Program.convertTxt2Double(ctrl._list[i].m_szVolumen);
                if (volumen < min || volumen > max) continue;
                if (!VdiAuswahlFilter.Passt(suche, ctrl._list[i].m_szName, ctrl._list[i].m_szFirma)) continue;
                Liste_PufferSp.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            for (int zeile = 0; zeile < _anzeigeIndex.Count; zeile++)
            {
                if (markiert.Contains(_anzeigeIndex[zeile])) Liste_PufferSp.SetSelected(zeile, true);
            }
            Liste_PufferSp.EndUpdate();
            _listeWirdGefuellt = false;

            // Detailfelder auf die verbleibende Markierung nachziehen, damit sie
            // nach dem Umfiltern nicht mehr auf einen ausgefilterten Eintrag
            // zeigen (bei der Uebernahme sind sie die Quelle bzw. die Anzeige).
            if (Liste_PufferSp.SelectedIndex >= 0 && Liste_PufferSp.SelectedIndex < _anzeigeIndex.Count)
                ZeigeDetails(_anzeigeIndex[Liste_PufferSp.SelectedIndex]);
        }

        // Markierte Zeilen -> Indizes in ctrl._list.
        private System.Collections.Generic.List<int> MarkierteQuellIndizes()
        {
            return VdiAuswahlFilter.QuellIndizes(Liste_PufferSp.SelectedIndices, _anzeigeIndex);
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            // Mehrfachselektion: je markierter Eintrag laeuft genau der bestehende
            // Einzel-Weg (UebernehmeEintrag) - nur in einer Schleife, damit es
            // keinen zweiten Schreibpfad in die STAMM-Tabelle gibt.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();
            if (markiert.Count == 0)
            {
                MessageBox.Show(MyResource.Resource.PSP_MELDUNG_PUFFER_SELEKTIEREN);
                return;
            }

            string fehlertext;

            if (markiert.Count == 1)
            {
                // Einzelfall: Meldungen und Dialogverhalten bleiben wie im Bestand;
                // die Detailfelder werden nicht neu besetzt, damit eine Korrektur
                // von Hand erhalten bleibt.
                VdiUebernahmeErgebnis einzel = UebernehmeEintrag(out fehlertext);
                if (einzel == VdiUebernahmeErgebnis.Duplikat)
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATEN_BEREITS_EINGELESEN);
                    return;
                }
                if (fehlertext != null)
                {
                    MessageBox.Show(string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, fehlertext));
                    this.DialogResult = DialogResult.Cancel;
                    return;
                }
                if (einzel == VdiUebernahmeErgebnis.Gespeichert)
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT);
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER);
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
                    // Detailfelder je Eintrag besetzen - sie sind der Traeger fuer
                    // InitDatensatzUpdate() und damit fuer die Uebernahme.
                    ZeigeDetails(i);

                    // Ein fehlerhafter Eintrag darf den Gesamtvorgang nicht abbrechen.
                    VdiUebernahmeErgebnis ergebnis = UebernehmeEintrag(out fehlertext);
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

        // Uebernahme genau eines Eintrags in Tab_PufferSp_STAMM. Unveraenderter
        // Bestandsweg (Quelle sind die Detailfelder), nur mit Ergebnis als
        // Rueckgabewert statt MessageBox - die Meldung entscheidet der Aufrufer.
        // Der lokale Stamm-Controller heisst pspctrl, weil er sonst das Feld ctrl
        // (PufferSpImport) verdecken wuerde.
        private VdiUebernahmeErgebnis UebernehmeEintrag(out string fehlertext)
        {
            fehlertext = null;

            try
            {
                PufferSpStammCtrl pspctrl = new PufferSpStammCtrl();
                if (pspctrl.Exists(textBox_Name.Text)) return VdiUebernahmeErgebnis.Duplikat;

                if (pspctrl.InsertFrom(InitDatensatzUpdate())) return VdiUebernahmeErgebnis.Gespeichert;
                return VdiUebernahmeErgebnis.Fehler;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Pufferspeichers: " + ex.Message);
                fehlertext = ex.Message;
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        PufferSpModel InitDatensatzUpdate()
        {
            PufferSpModel model = new PufferSpModel();
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Speichertyp = textBox_Typ.Text;   
            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            model.Gesamtvolumen = Program.convertTxt2Int(textBox_Volumen.Text);

            return model;
        }

    }
}