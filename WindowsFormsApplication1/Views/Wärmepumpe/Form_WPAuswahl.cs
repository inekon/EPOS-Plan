using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_WPAuswahl : Form
    {
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        private WizardParent wizardparent = null;
        private bool m_bWizard = false;

        public Form_WPAuswahl()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
 
            listView_WP.View = View.Details;
            listView_WP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
        }

        public void SetControls(string projekt, bool bWizard = false)
        {
            if (bWizard)
            {
                m_bWizard = true;
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_werzmodel = wizardparent.list_werzmodel;
            }

            listView_WP.Items.Clear();
   
            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                WErzeugerModel item = new WErzeugerModel();
                ListViewItem lvitem = new ListViewItem();

                if (list_werzmodel[n].ID_Type == WizardItemClass.WP_TYP)
                {
                    item.Bezeichner = list_werzmodel[n].Bezeichner;
                    item.Abschaltpunkt = (double)list_werzmodel[n].Abschaltpunkt;
                    item.Betriebsart = (string)list_werzmodel[n].Betriebsart;
                    item.Bivalenter_Betrieb = list_werzmodel[n].Bivalenter_Betrieb;
                    item.Nutzungszeit = list_werzmodel[n].Nutzungszeit;
                    item.Ruecklauf = list_werzmodel[n].Ruecklauf;
                    item.Sperrung = list_werzmodel[n].Sperrung;
                    item.Sperrzeit_bis = list_werzmodel[n].Sperrzeit_bis;
                    item.Sperrzeit_von = list_werzmodel[n].Sperrzeit_von;
                    item.Vorlauf = list_werzmodel[n].Vorlauf;
                    item.Heizstab = list_werzmodel[n].Heizstab;
                    item.Volumen = list_werzmodel[n].Volumen;
                    item.rendeMix = list_werzmodel[n].rendeMix;
                    item.Solaranteil = list_werzmodel[n].Solaranteil;
                    item.ID_WP = list_werzmodel[n].ID_WP;

                    // Ä22: zweistufig (Projekt vor Stamm) statt items[0]-Zugriff —
                    // ein frischer Eintrag mit Stamm-Id ließ den Aufbau sonst
                    // abstürzen; ohne Treffer bleibt die Zeile mit Grunddaten stehen.
                    WaermepumpeGeraeteCtrl.GeraetedatenFuellen(item, item.ID_WP);

                    lvitem.Text = item.Bezeichner;
                    lvitem.SubItems.Add(item.Nennleistung.ToString());
                    lvitem.SubItems.Add(item.Vorlauf.ToString());
                    lvitem.SubItems.Add(item.Ruecklauf.ToString());
                    lvitem.SubItems.Add(item.Betriebsart);

                    listView_WP.Items.Add(lvitem);
                }
            }
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        // iU9-W7.0e: Die zweistufige Geraetesuche (Ä22, Projektgeraet vor
        // Stammkatalog) steht als WaermepumpeGeraeteCtrl.GeraetedatenFuellen im Kern -
        // sie hat mit W7.4 drei weitere Aufrufer bekommen, und keiner von ihnen ist
        // mehr eine WinForms-Maske.

        private Form getWizardPage()
        {
            // P4: typisierte Erkennung ueber WizardParent.Aktiver. Die frueheren elf
            // Kopien suchten den Rahmen als Zeichenkette "WizardParent" in
            // Application.OpenForms; der Rahmen meldet sich jetzt selbst an.
            return WizardParent.Aktiver as Form;
        }

        private void listView_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                textBox_WP.Text = lvitem.Text;
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            string wpname;

            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;

            if (indexes.Count > 0)
            {
                int n = indexes[0];
                wpname = listView_WP.Items[n].SubItems[0].Text;
                listView_WP.Items[indexes[0]].Remove();
                list_werzmodel.RemoveAt(n);
                if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;
            }
        }

        // iU9-W7.4: btn_Ansicht_Click ist mit Wizard_WPItem entfallen. Der Handler war
        // VERWAIST - der Designer kennt kein btn_Ansicht, weder als Steuerelement noch
        // als Ereignisbindung (Befund W7-O-3). Der Ansichtsweg der Maske lief ueber den
        // Doppelklick (listView_WP_MouseDoubleClick).

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                int n = indexes[0];

                int idwp = 0;
                int index = 0;

                for (index = 0; index < list_werzmodel.Count; index++)
                {
                    if (list_werzmodel[index].Bezeichner == textBox_WP.Text && list_werzmodel[index].ID_Type == 1)
                    {
                        idwp = list_werzmodel[index].ID_WP;
                        break;
                    }
                }

                // Absicherung: Ohne Treffer läuft die Schleife bis index == Count durch -
                // der Zugriff list_werzmodel[index] würde dann mit
                // ArgumentOutOfRangeException abstürzen.
                if (idwp <= 0 || index >= list_werzmodel.Count)
                {
                    MessageBox.Show("Die ausgewählte Wärmepumpe '" + textBox_WP.Text +
                        "' wurde in der Projektliste nicht gefunden!", "Wärmepumpe",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ä22: zweistufig (Projektgerät vor Stammkatalog) — „Ändern..“
                // funktioniert damit auch auf einem frisch angelegten Eintrag,
                // dessen ID_WP bis zum Speichern die Stamm-Id ist (Befund
                // „Datensatz (ID 67) nicht gefunden“).
                if (!WaermepumpeGeraeteCtrl.GeraetedatenFuellen(list_werzmodel[index], idwp))
                {
                    MessageBox.Show("Die Wärmepumpe (ID " + idwp +
                        ") wurde weder bei den Projektgeräten noch im Stammkatalog gefunden!",
                        "Wärmepumpe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // iU9-W7.4: Die Detailansicht ist die Razor-Komponente
                // WaermepumpeAnlageDialog; Wizard_WPItem ist im selben Schritt
                // GELOESCHT (Regel M1). Die Huelle bearbeitet die Zeile an Ort und
                // Stelle - bei Abbruch bleibt sie unveraendert.
                if (!WaermepumpeAnlageHuelle.Oeffnen(this, list_werzmodel[index], ProjektId())) return;

                if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

                ListViewItem lvitem;
                lvitem = listView_WP.Items[n];
                lvitem.Text = list_werzmodel[index].Bezeichner;
                // Ä23: Die Spalte heißt „Leistung [kW]“ und zeigt die NENNLEISTUNG —
                // maxPTherm ist am Listenobjekt nie gefüllt und schrieb hier eine 0
                // über den korrekten Aufbauwert.
                lvitem.SubItems[1].Text = list_werzmodel[index].Nennleistung.ToString();
                lvitem.SubItems[2].Text = list_werzmodel[index].Vorlauf.ToString();
                lvitem.SubItems[3].Text = list_werzmodel[index].Ruecklauf.ToString();
                lvitem.SubItems[4].Text = list_werzmodel[index].Betriebsart;
            }
        }

        /// <summary>
        /// Das geöffnete Projekt — Rückfall beim Nachziehen der Anlagenzeile
        /// (iU9-W7.4). Der Vorläufer holte es sich in <c>Wizard_WPItem</c> selbst
        /// aus <c>Program.startfrm</c>; jetzt reicht der Aufrufer es herein.
        /// </summary>
        private static int ProjektId()
        {
            return Program.startfrm != null ? Program.startfrm.m_ID_Projekt : 0;
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_WpFilterAuswahl frmauswahl = new Form_WpFilterAuswahl();
            DialogResult result =frmauswahl.ShowDialog();
            if (result != DialogResult.OK) return;

            // iU9-W7.4: Die Detailansicht ist die Razor-Komponente
            // WaermepumpeAnlageDialog. Der frische Eintrag traegt zunaechst nur den
            // Namen und die STAMM-Id; die Huelle fuellt ihn beim OK.
            WErzeugerModel neu = new WErzeugerModel();
            neu.Bezeichner = frmauswahl.SelectedWP.Bezeichnung;
            neu.ID_WP = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", neu.Bezeichner);

            if (!WaermepumpeAnlageHuelle.Oeffnen(this, neu, ProjektId())) return;

            // Ä23: Der frische Eintrag bekommt seine Stammdaten (Nennleistung,
            // Regelung, …) ins Listenobjekt — zweistufig, denn ID_WP ist hier
            // noch die Stamm-Id (Ä22).
            WaermepumpeGeraeteCtrl.GeraetedatenFuellen(neu, neu.ID_WP);
            neu.ID_Type = 1;

            list_werzmodel.Add(neu);
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            ListViewItem lvitem = new ListViewItem();

            lvitem.Text = neu.Bezeichner;
            lvitem.SubItems.Add(neu.Nennleistung.ToString());
            lvitem.SubItems.Add(neu.Vorlauf.ToString());
            lvitem.SubItems.Add(neu.Ruecklauf.ToString());
            lvitem.SubItems.Add(neu.Betriebsart);
            lvitem = listView_WP.Items.Add(lvitem);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_WP_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                WErzeugerCtrl ctrl = new WErzeugerCtrl();

                ctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "'");
                if (ctrl.rows == 0)
                {
                    MessageBox.Show("Die Wärmepumpe '" + lvitem.Text +
                        "' wurde in der Datenbank nicht gefunden!", "Wärmepumpe",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // Ä22/Ä23: zweistufig (Projektgerät vor Stammkatalog) statt
                // Einstufigkeit über Tab_WP allein — frisch angelegte Einträge
                // tragen bis zum Verwaltungs-OK die Stamm-Id.
                if (!WaermepumpeGeraeteCtrl.GeraetedatenFuellen(ctrl.items[0], ctrl.items[0].ID_WP))
                {
                    MessageBox.Show("Die Wärmepumpe (ID " + ctrl.items[0].ID_WP +
                        ") wurde weder bei den Projektgeräten noch im Stammkatalog gefunden!",
                        "Wärmepumpe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // iU9-W7.4: dieselbe Razor-Komponente wie in den uebrigen drei Wegen.
                WaermepumpeAnlageHuelle.Oeffnen(this, ctrl.items[0], ProjektId());
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
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
