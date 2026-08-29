using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Waermebedarf : Form
    {
        public List<Z_ProjWaermebedarfModel> list_wbmodel = new List<Z_ProjWaermebedarfModel>();
        private Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        private ToolsClass tool = new ToolsClass();
        string filename = "";
        string filebasename = "";

        // ------------------------------------------------------------------
        // Kanalzuordnung (Migrationsschritt 48, Konzept
        // Brauchwasser/Heizung/Pufferspeicher 4.2, Entscheidung F18)
        // ------------------------------------------------------------------
        //
        // Die beiden Steuerelemente stehen BEWUSST hier statt im Designer: Der
        // Dialog wird auch als Wizardseite ohne Rahmen eingebettet, seine
        // Designer- und .resx-Dateien sind vom Werkzeug erzeugt und werden nicht
        // von Hand gepflegt.
        //
        // Geometrie: listBox_Auswahl steht auf (27, 95) und ist 341 x 174 gross,
        // endet also bei y = 269; die Fusstasten liegen auf y = 399. Der Streifen
        // dazwischen ist frei - btn_Bearbeiten und btn_Loeschen sitzen auf x = 653,
        // also rechts neben der linken Spalte.
        private Label _lblKanal;
        private ComboBox _cbKanal;

        /// <summary>Sperrt <see cref="_cbKanal"/>-Ereignisse, waehrend die Anzeige gesetzt wird.</summary>
        private bool _kanalStumm;

        /// <summary>
        /// Eintrag des Kanal-Dropdowns nach dem Muster von
        /// <c>Form_PufferSp_Projekt.VerwendungItem</c> (Drei-Schichten-Regel):
        /// <see cref="DbWert"/> geht in die Datenbank und in jeden Vergleich,
        /// <see cref="ToString"/> liefert den uebersetzten Anzeigetext. Der
        /// Anzeigetext ist NIE Steuerwert.
        /// </summary>
        private class KanalItem
        {
            public string DbWert = "";
            public string Anzeige = "";
            public override string ToString() { return Anzeige; }
        }

        public Form_Waermebedarf()
        {
            InitializeComponent();
            KanalControlsAufbauen();

            WaermebedarfStammCtrl ctrl = new WaermebedarfStammCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Extern.Items.Add(ctrl.items[i].m_szBezeichner);
            }

            // D2 (28.08.2026): Fusszeile auf die Norm - bisher Abbrechen links von OK,
            // Groesse 98x33 und ohne Anker.
            FusszeilenNorm.Einhaengen(this, btn_OK, btn_Abbrechen);
        }

        /// <summary>Legt Beschriftung und Kanal-Dropdown an und haengt sie ein.</summary>
        private void KanalControlsAufbauen()
        {
            _lblKanal = new Label();
            _lblKanal.Name = "_lblKanal";
            _lblKanal.AutoSize = false;
            _lblKanal.Location = new Point(23, 285);
            _lblKanal.Size = new Size(146, 21);
            _lblKanal.TextAlign = ContentAlignment.MiddleLeft;
            _lblKanal.Text = MyResource.Resource.KANAL_LABEL;

            _cbKanal = new ComboBox();
            _cbKanal.Name = "_cbKanal";
            _cbKanal.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbKanal.Location = new Point(175, 282);
            _cbKanal.Size = new Size(193, 25);
            _cbKanal.Items.AddRange(new object[]
            {
                new KanalItem
                {
                    DbWert = DbWerte.KANAL_HEIZUNG,
                    Anzeige = MyResource.Resource.KANAL_HEIZUNG_ANZEIGE
                },
                new KanalItem
                {
                    DbWert = DbWerte.KANAL_BRAUCHWASSER,
                    Anzeige = MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE
                },
                new KanalItem
                {
                    DbWert = DbWerte.KANAL_PROZESS,
                    Anzeige = MyResource.Resource.KANAL_PROZESS_ANZEIGE
                }
            });
            _cbKanal.SelectedIndexChanged += new EventHandler(_cbKanal_SelectedIndexChanged);

            Controls.Add(_lblKanal);
            Controls.Add(_cbKanal);

            // Der Kanal gilt je Zuordnung; das Dropdown wirkt deshalb immer auf die
            // in listBox_Auswahl markierte Zeile.
            listBox_Auswahl.SelectedIndexChanged +=
                new EventHandler(listBox_Auswahl_SelectedIndexChanged);

            KanalAnzeigeAktualisieren();
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            KanalAnzeigeAktualisieren();
        }

        private void _cbKanal_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_kanalStumm) return;

            int n = listBox_Auswahl.SelectedIndex;
            if (n < 0 || n >= list_wbmodel.Count) return;

            KanalItem it = _cbKanal.SelectedItem as KanalItem;
            if (it == null) return;

            list_wbmodel[n].Kanal = it.DbWert;
        }

        /// <summary>
        /// Zeigt den Kanal der markierten Zuordnung. Ohne Markierung ist das
        /// Dropdown gesperrt; ein unbekannter oder leerer Wert faellt auf Heizung
        /// zurueck (Vorbelegung nach F18).
        /// </summary>
        private void KanalAnzeigeAktualisieren()
        {
            if (_cbKanal == null) return;

            int n = listBox_Auswahl.SelectedIndex;
            bool gewaehlt = n >= 0 && n < list_wbmodel.Count;

            _kanalStumm = true;
            try
            {
                _cbKanal.Enabled = gewaehlt;

                string dbWert = gewaehlt
                    ? Z_ProjektGebGanglinieCtrl.KanalOderHeizung(list_wbmodel[n].Kanal)
                    : DbWerte.KANAL_HEIZUNG;

                foreach (object o in _cbKanal.Items)
                {
                    KanalItem it = o as KanalItem;
                    if (it != null &&
                        string.Equals(it.DbWert, dbWert, StringComparison.OrdinalIgnoreCase))
                    {
                        _cbKanal.SelectedItem = o;
                        return;
                    }
                }
                if (_cbKanal.Items.Count > 0) _cbKanal.SelectedIndex = 0;
            }
            finally { _kanalStumm = false; }
        }

        public void SetControls(string projekt, bool bWizard=false)
        {
            if (bWizard)
            {
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
            }

            m_szProjekt = projekt;
       
            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_wbmodel.Count; n++)
            {
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                item.m_szBezeichner = list_wbmodel[n].m_szBezeichner;
                listBox_Auswahl.Items.Add(item.m_szBezeichner);
                m_ID_Projekt = list_wbmodel[n].m_ID_Projekt;
            }

            // Migrationsschritt 48 (F18): Die Kanaele aus der Datenbank nachtragen.
            // Notwendig, weil die aufrufenden Stellen (Kontextmenue der Startseite,
            // Karte der Startseite, FormMain, Wizard) die Liste ueber ausgeschriebene
            // SELECT-Listen bzw. ListView-Spalten ohne den Kanal aufbauen - und der
            // Speicherweg der Zuordnung LOESCHEN + NEU ANLEGEN ist. Ohne das
            // Nachladen schriebe der Dialog jede Ganglinie auf Heizung zurueck.
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(m_ID_Projekt, list_wbmodel);

            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
            KanalAnzeigeAktualisieren();
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_Extern.Text == "") return;

            // Je Zuordnung ein EIGENES Modell. Vorher wanderte das Feld "model"
            // selbst in die Liste - bei zwei Ganglinien standen also zwei Verweise
            // auf DASSELBE Objekt darin, und die zweite ueberschrieb die erste.
            // Mit der Kanalzuordnung (Schritt 48, F18) faellt das sofort auf: Der
            // Kanal gilt je Zeile und braucht ein eigenes Modell je Zeile.
            Z_ProjWaermebedarfModel neu = new Z_ProjWaermebedarfModel();
            neu.m_szBezeichner = listBox_Extern.Text;
            neu.m_ID_Projekt = m_ID_Projekt;

            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Waermebedarf_STAMM where Bezeichner='" + listBox_Extern.Text + "'");
            if (!rs.EOF())
            {
                neu.m_ID_Ganglinie = (int)rs.Read("ID");
                neu.m_ID_Projekt = m_ID_Projekt;
            }
            rs.Close();

            list_wbmodel.Add(neu);
            listBox_Auswahl.Items.Add(listBox_Extern.Text);

            // Die neue Zeile markieren, damit das Kanal-Dropdown sichtbar auf sie
            // wirkt (neue Ganglinien starten auf Heizung).
            listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;
            KanalAnzeigeAktualisieren();

            if (listBox_Extern.Items.Count > 0) listBox_Extern.SelectedIndex = listBox_Extern.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.m_szBezeichner = listBox_Auswahl.Text;
            for (int i = 0; i < list_wbmodel.Count; i++)
            {
                if (list_wbmodel[i].m_szBezeichner == listBox_Auswahl.Text)
                {
                    list_wbmodel.RemoveAt(i);
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                    break;
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;
            KanalAnzeigeAktualisieren();
        }
        
        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_AdminWaermeeinlesen frm = new Form_AdminWaermeeinlesen();
            frm.SetControls();
            frm.ShowDialog();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;
            Close();
        }

        private void Einlesen()
        {
            // Datei schon eingelesen?
            if (listBox_Extern.FindString(Path.GetFileNameWithoutExtension(filebasename)) != ListBox.NoMatches)
            {

                Form_Hinweis frm = new Form_Hinweis("Hinweis", "Datei ist bereits eingelesen!");
                frm.Location = this.PointToScreen(btn_Bearbeiten.Location);
                frm.ShowDialog();
                return;
            }

            // Datei in Liste einlesen 
            if (!tool.OpenText(filename)) return;

            this.Cursor = Cursors.WaitCursor;

            // Import in die STAMM-Tabellen (Kopf + Daten)
            WaermebedarfStammCtrl ctrl_stamm = new WaermebedarfStammCtrl();
            ctrl_stamm.ImportGanglinie(Path.GetFileNameWithoutExtension(filebasename), tool.textList);

            this.Cursor = Cursors.Default;
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            WaermebedarfStammCtrl ctrl_ganglinie = new WaermebedarfStammCtrl();
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("Select * from Z_ProjektWaermebedarf where Bezeichner ='" + listBox_Extern.Text + "'");
            if (ctrl.rows > 0)
            {
                MessageBox.Show("Es existiert eine Projektzuordnung, Löschen nicht möglich!");
                return;
            }

            // Delete prueft selbst auf ReadOnly und meldet ggf.
            if (!ctrl_ganglinie.Delete(listBox_Extern.Text)) return;

            listBox_Extern.Items.Clear();
            listBox_Extern.SelectedItems.Clear();
            ctrl_ganglinie.ReadAll();
            for (int i = 0; i < ctrl_ganglinie.rows; i++)
            {
                listBox_Extern.Items.Add(ctrl_ganglinie.items[i].m_szBezeichner);
            }

        }
    }
}
