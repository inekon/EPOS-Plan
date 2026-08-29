using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ProjektSpeichernUnter : Form
    {
        public string m_szProjekt;
        public string m_szNeuerProjektName;
        public int m_ID_Klimaregion;
        public int m_ID_Projekt;
        public string m_szKlimaregion;
        public string m_szKunde;
        public string m_szBearbeiter;
        public DateTime m_Datum;

        public Form_ProjektSpeichernUnter()
        {
            InitializeComponent();
            m_szProjekt = "";
            m_szKlimaregion = "";
            m_ID_Klimaregion = 0;
            m_ID_Projekt = 0;

            listView_Projekt.View = View.Details;
            listView_Projekt.Columns.Add(MyResource.Resource.Text_Name, -2, HorizontalAlignment.Left);
            listView_Projekt.Columns.Add(MyResource.Resource.Text_Beschreibung, -2, HorizontalAlignment.Left);
            listView_Projekt.Columns[0].Width = listView_Projekt.ClientRectangle.Width;
        }

        private void button_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        // P3: hiess bis zur Vereinheitlichung der Projektdialoge Form_ProjektOpen_Load -
        // ein Relikt aus der Zeit, als dieser Dialog als "Oeffnen" missbraucht wurde.
        // Oeffnen macht jetzt Form_ProjektAuswahl; dieser Dialog dupliziert nur noch.
        private void Form_ProjektSpeichernUnter_Load(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();

            for (int i = 0; i < ctrl.rows; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = ctrl.items[i].m_szProjektname;
                lvitem.SubItems.Add(ctrl.items[i].m_szBeschreibung);
                listView_Projekt.Items.Add(lvitem);
            }
            listView_Projekt.Select();
            if (listView_Projekt.Items.Count > 0) listView_Projekt.Items[0].Selected = true;
            listView_Projekt.Items[0].Selected = true;
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            ctrl = null;
        }

        private async void button_Open_Click(object sender, EventArgs e)
        {
            m_szNeuerProjektName = textBox_NeuerProjektName.Text;
            if (string.IsNullOrWhiteSpace(m_szNeuerProjektName))
            { MessageBox.Show("Bitte einen neuen Projektnamen eingeben.", "Hinweis", MessageBoxButtons.OK); return; }
            if (listView_Projekt.FindItemWithText(m_szNeuerProjektName) != null)
            { MessageBox.Show("Projektname bereits vorhanden!", "Hinweis", MessageBoxButtons.OK); return; }

            // Fortschritt an die UI melden (Progress marshalt automatisch auf den UI-Thread).
            var progress = new Progress<ProjektDuplizierenCtrl.Fortschritt>(f =>
            {
                if (f.Gesamt > 0)
                {
                    progressBar_Duplizieren.Maximum = f.Gesamt;
                    progressBar_Duplizieren.Value = Math.Min(f.Aktuell, f.Gesamt);
                }
                lbl_Fortschritt.Text = (f.Gesamt > 0 && f.Aktuell < f.Gesamt)
                    ? string.Format("Kopiere Tabelle {0}/{1}: {2}", f.Aktuell + 1, f.Gesamt, f.Tabelle)
                    : "Fertigstellen ...";
            });

            SetBusy(true);
            int neueId = -1;
            try
            {
                neueId = await Task.Run(() =>
                {
                    ProjektDuplizierenCtrl ctrl = new ProjektDuplizierenCtrl();
                    return ctrl.Duplizieren(m_szProjekt, m_szNeuerProjektName, progress);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern unter: " + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (neueId > 0)
            {
                // Auch bei sehr schnellem Kopieren den fertigen Balken kurz sichtbar lassen.
                progressBar_Duplizieren.Value = progressBar_Duplizieren.Maximum;
                lbl_Fortschritt.Text = "Fertig";
                await Task.Delay(FERTIG_ANZEIGE_MS);

                SetBusy(false);
                this.DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // Fehler: Duplizieren hat bereits gemeldet -> Dialog offen lassen.
                SetBusy(false);
            }
        }

        // Dauer, wie lange der fertige Fortschrittsbalken am Ende noch angezeigt wird (in ms).
        private const int FERTIG_ANZEIGE_MS = 1000;

        // Blendet die Fortschrittsanzeige ein/aus, vergroessert den Dialog nur waehrend der Operation
        // und sperrt die Bedienelemente, damit kein zweiter Lauf gestartet wird.
        private void SetBusy(bool busy)
        {
            if (busy && !panel_Fortschritt.Visible)
            {
                progressBar_Duplizieren.Value = 0;
                lbl_Fortschritt.Text = "";
                this.Height += panel_Fortschritt.Height;
                panel_Fortschritt.Visible = true;
            }
            else if (!busy && panel_Fortschritt.Visible)
            {
                panel_Fortschritt.Visible = false;
                this.Height -= panel_Fortschritt.Height;
            }

            button_Open.Enabled = !busy;
            button_Abbrechen.Enabled = !busy;
            listView_Projekt.Enabled = !busy;
            textBox_NeuerProjektName.Enabled = !busy;
            this.UseWaitCursor = busy;
        }

        private void listView_Projekt_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Projekt.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Projekt.Items[indexes[0]];
                m_szProjekt = lvitem.Text;
            }
        }

        private void listView_Projekt_DoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Projekt.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Projekt.Items[indexes[0]];
                m_szProjekt = lvitem.Text;
                button_Open.PerformClick();
            }

        }

    }
}
