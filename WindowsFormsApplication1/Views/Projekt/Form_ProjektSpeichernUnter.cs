using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ProjektSpeichernUnter : Form
    {
        public string m_szProjekt;
        public string m_szNeuerProjektName;

        // ALTLAST. Die vier Felder m_ID_Klimaregion, m_ID_Projekt, m_szKlimaregion und
        // m_Datum stammen aus der Zeit, als dieser Dialog auch zum OEFFNEN benutzt wurde
        // (siehe Form_ProjektSpeichernUnter_Load). Ein Aufrufer liest sie heute nicht mehr:
        // MenueCtrl.ProjektSpeichernUnter wertet nur das DialogResult aus. Sie bleiben
        // deshalb BEWUSST auf ihrem Anfangswert aus dem Konstruktor - ein echter Wert darin
        // waere eine Behauptung, auf die sich niemand verlassen koennte. Beim Aufraeumen
        // der Projektdialoge ersatzlos weg.
        public int m_ID_Klimaregion;
        public int m_ID_Projekt;
        public string m_szKlimaregion;
        public DateTime m_Datum;

        // Ebenfalls Altlast, aber ab jetzt mit ehrlichem Inhalt: Nach erfolgreichem
        // Duplizieren stehen hier die Werte, die auf der KOPIE gelandet sind. Auch sie
        // liest zurzeit kein Aufrufer.
        public string m_szKunde;
        public string m_szBearbeiter;

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
                // Beschreibung, Kunde und Bearbeiter auf die KOPIE schreiben - vor dem
                // Schliessen, damit ein Fehler dabei noch gemeldet werden kann.
                VerwaltungsfelderAufKopieSchreiben();

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

        /// <summary>
        /// Schreibt Beschreibung, Kunde und Bearbeiter aus den drei Eingabefeldern auf die
        /// FERTIG DUPLIZIERTE Kopie.
        ///
        /// <para>
        /// Reihenfolge ist hier keine Geschmacksfrage: <see cref="ProjektCtrl.Update"/>
        /// schreibt mit EINEM UPDATE auch ID_Klimaregion und Erstelldatum
        /// (WHERE Projektname=?). Wuerde man einen frisch angelegten ProjektCtrl nur mit
        /// den drei Textfeldern fuellen, traegt die Kopie hinterher Klimaregion 0 und ein
        /// Erstelldatum von heute. Deshalb wird die Kopie zuerst GELESEN und danach werden
        /// nur die drei Textfelder plus Aenderungsdatum ueberschrieben - alles Uebrige
        /// laeuft unveraendert durch.
        /// </para>
        /// <para>
        /// Fehler werden gemeldet, aber NICHT zurueckgerollt: Die Kopie ist an dieser
        /// Stelle vollstaendig angelegt; sie wieder wegzuwerfen, weil ein Beschreibungstext
        /// nicht geschrieben werden konnte, waere der groessere Schaden. Der Anwender kann
        /// die drei Felder im Hauptformular jederzeit nachtragen.
        /// </para>
        /// </summary>
        private void VerwaltungsfelderAufKopieSchreiben()
        {
            try
            {
                ProjektCtrl ctrl = new ProjektCtrl();
                ctrl.ReadSingle(m_szNeuerProjektName);

                if (ctrl.rows == 0)
                {
                    MessageBox.Show("Die Kopie '" + m_szNeuerProjektName + "' wurde nicht gefunden. "
                        + "Beschreibung, Kunde und Bearbeiter wurden nicht uebernommen.",
                        "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ctrl.m_szBeschreibung = textBox_Beschreibung.Text;
                ctrl.m_szKunde = textBox_Kunde.Text;
                ctrl.m_szBearbeiter = textBox_Bearbeiter.Text;
                ctrl.m_Aenderungsdatum = DateTime.Now;

                // Ergebnisfelder des Dialogs mitfuehren (siehe Kommentar an der Deklaration).
                m_szKunde = ctrl.m_szKunde;
                m_szBearbeiter = ctrl.m_szBearbeiter;

                if (!ctrl.Update())
                {
                    // ExecuteSQL hat den Datenbankfehler bereits gemeldet - hier fehlt nur
                    // noch die Folge fuer den Anwender.
                    MessageBox.Show("Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden. "
                        + "Die Projektkopie selbst ist angelegt.",
                        "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden: "
                    + ex.Message + Environment.NewLine + "Die Projektkopie selbst ist angelegt.",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            textBox_Beschreibung.Enabled = !busy;
            textBox_Kunde.Enabled = !busy;
            textBox_Bearbeiter.Enabled = !busy;
            this.UseWaitCursor = busy;
        }

        private void listView_Projekt_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Projekt.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Projekt.Items[indexes[0]];
                m_szProjekt = lvitem.Text;
                QuellProjektFelderLaden();
            }
        }

        /// <summary>
        /// Belegt Beschreibung, Kunde und Bearbeiter mit den Werten des links gewaehlten
        /// QUELLPROJEKTS vor.
        ///
        /// <para>
        /// Ein Auswahlwechsel ueberschreibt dabei BEWUSST bereits eingetippte Aenderungen:
        /// Die drei Felder gehoeren sichtbar zum gewaehlten Projekt. Ein stehengebliebener
        /// Text des vorher markierten Projekts waere die groessere Ueberraschung - er
        /// landete unbemerkt auf der Kopie eines ganz anderen Projekts.
        /// </para>
        /// </summary>
        private void QuellProjektFelderLaden()
        {
            if (string.IsNullOrEmpty(m_szProjekt)) return;

            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadSingle(m_szProjekt);
            if (ctrl.rows == 0) return;

            textBox_Beschreibung.Text = ctrl.m_szBeschreibung ?? "";
            textBox_Kunde.Text = ctrl.m_szKunde ?? "";
            textBox_Bearbeiter.Text = ctrl.m_szBearbeiter ?? "";
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
