using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Stromverbraucher : Form
    {
        private StromverbraucherModel model = new StromverbraucherModel();
        private StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
        public List<Z_ProjektStromverbraucherModel> list_sbmodel = new List<Z_ProjektStromverbraucherModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationStrombedarf simulation = new SimulationStrombedarf();
        private string m_szProjekt;
        private int m_ListIndex = -1;   
        private bool m_bWizard = false;
        // Hinweis "Projekt noch nicht gespeichert" nur einmal je Wizard-Sitzung zeigen.
        private bool m_bHinweisNeuesProjekt = false;


        public Form_Stromverbraucher()
        {
            InitializeComponent();
            listView_Strom_Auswahl.View = View.Details;
            listView_Strom_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Strom_Auswahl.Columns[0].Width = listView_Strom_Auswahl.ClientRectangle.Width;
        }

        public void SetControls(string szProjekt, bool bWizard=false)
        {
            Z_ProjektStromverbraucherCtrl ctrl = new Z_ProjektStromverbraucherCtrl();
            StromverbraucherStammCtrl ctrl_pw = new StromverbraucherStammCtrl();
            Z_ProjektStromverbraucherModel model = new Z_ProjektStromverbraucherModel();

            m_szProjekt = szProjekt;
            m_bWizard = bWizard;    

            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
            }

            listView_Strom_Auswahl.Items.Clear();
            for (int i = 0; i < list_sbmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_sbmodel[i].m_szVerbraucher;
                lvitem.SubItems.Add(list_sbmodel[i].m_ID_Z.ToString());
                listView_Strom_Auswahl.Items.Add(lvitem);
            }

            if (listView_Strom_Auswahl.Items.Count > 0)
            {
                textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
            }
            
            btn_ErgebnisseVerbrauch.Enabled = false;

            listBox_Strom_DB.Items.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_Strom_DB.Items.Add(ctrl_pw.items[i].m_szBezeichner);
            }
            listView_Strom_Auswahl.Select(); 
            if (listView_Strom_Auswahl.Items.Count > 0) listView_Strom_Auswahl.Items[0].Selected = true;
            
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox list = (ListBox)sender;
            string szName = list.Text;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString("F2");
            SetProzessInfo(szName);
        }

        private void SetProzessInfo(string szName)
        {
            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Stromname.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Stromtyp.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            ctrl.ReadSingle(szName);

            double summe = 0;
            if (ctrl.rows > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;  
        }

        private void listView_Prozess_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strom_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                m_ListIndex = indexes[0];
                ListViewItem lvitem = listView_Strom_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = list_sbmodel[m_ListIndex].m_Summe.ToString("F2");
                SetProzessInfo(lvitem.Text);
            }
        }

        private void btn__Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            
            if (listBox_Strom_DB.Text == "") return;

            string sql = "SELECT * from Tab_Stromverbraucher_STAMM where Bezeichner='" + listBox_Strom_DB.Text + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektStromverbraucherModel model = new Z_ProjektStromverbraucherModel();
                model.m_ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.m_ID_Stromverbraucher = (int)rs.Read("ID");
                model.m_ID_Projekt = m_ID_Projekt;
                model.m_szVerbraucher = listBox_Strom_DB.Text;
                model.m_Summe = Prozesssumme(model.m_szVerbraucher);

                list_sbmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = listBox_Strom_DB.Text;
                lvitem.SubItems.Add(model.m_ID_Z.ToString());
                listView_Strom_Auswahl.Items.Add(lvitem);
            }
            rs.Close();

            textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strom_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
       
                ListViewItem lvitem = listView_Strom_Auswahl.Items[indexes[0]];
    
                for (int i = 0; i < list_sbmodel.Count; i++)
                {
                    if (list_sbmodel[i].m_szVerbraucher == lvitem.Text && list_sbmodel[i].m_ID_Z.ToString()  == lvitem.SubItems[1].Text  )
                    {
                        list_sbmodel.RemoveAt(i);
                        listView_Strom_Auswahl.Items[indexes[0]].Remove();
                        textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
                        break;
                    }
                }
            }
            
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i = 0; i < listView_Strom_Auswahl.Items.Count; i++)
            {
                summe += list_sbmodel[i].m_Summe;
            }
            return summe;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            Close();
        }

        /// <summary>
        /// Legt den im Feld "neuer Wert" eingegebenen Jahresverbrauch vor dem Simulationslauf
        /// in Z_Projekt_Stromverbraucher.Summe ab. Die Simulation liest ihn NICHT aus dem
        /// Formular, sondern aus dieser Zeile -
        /// SimulationStrombedarf.Stromprofil_Strombedarf_berechnen skaliert damit das
        /// Katalogprofil. Daher der Schreibzugriff mitten in der Bedienung.
        ///
        /// Das setzt ein bereits gespeichertes Projekt voraus. Im Neuanlage-Zweig des Wizards
        /// ist m_ID_Projekt nur die in WizardParent.Next geratene ProjektCtrl.GetMaxID()+1;
        /// weder Tab_Projekt noch Z_Projekt_Stromverbraucher haben dazu eine Zeile. Das UPDATE
        /// traf dort 0 Zeilen und meldete trotzdem Erfolg - ein stiller No-op, der zugleich
        /// vortaeuschte, die Vorschau rechne mit dem eingegebenen Wert.
        ///
        /// Verloren geht dabei nichts: Gespeichert wird der Jahresverbrauch ohnehin aus
        /// list_sbmodel (Schaltflaeche "neuer Wert") ueber WizardParent.Speichern und
        /// WizardCtrl.Add_Projekt_Stromverbraucher.
        /// </summary>
        private void SummeFuerSimulationSichern()
        {
            if (!m_bWizard || textBox_Verbrauch.Text.Trim() == "") return;

            // ZahlParsen statt double.Parse: Seit die Pruefung nicht mehr im TextChanged
            // sitzt, kann im Feld auch ein ungueltiger Text stehen - double.Parse haette
            // hier eine FormatException geworfen. Ungueltig heisst jetzt schlicht: nicht
            // sichern, die Vorschau rechnet mit dem zuletzt gespeicherten Wert.
            double dSumme;
            if (!Program.ZahlParsen(textBox_Verbrauch.Text, out dSumme)) return;

            if (ProjektIstGespeichert())
            {
                Z_ProjektStromverbraucherCtrl ctrl = new Z_ProjektStromverbraucherCtrl();
                ctrl.UpdateSumme(dSumme, textBox_Stromname.Text, m_ID_Projekt);
                return;
            }

            // Neues Projekt: der Wert kann noch nicht in die Projektzeile. Das muss der
            // Anwender wissen, sonst haelt er die Vorschau faelschlich fuer skaliert.
            if (m_bHinweisNeuesProjekt) return;
            m_bHinweisNeuesProjekt = true;
            MessageBox.Show(
                "Das Projekt ist noch nicht gespeichert. Die Vorschau rechnet deshalb mit den " +
                "Katalogwerten; der eingegebene Jahresverbrauch wirkt sich erst nach dem " +
                "Speichern des Projekts auf die Simulation aus.",
                "Vorschau ohne Projektwerte", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// true, wenn m_ID_Projekt auf eine wirklich vorhandene Tab_Projekt-Zeile zeigt.
        /// Im Neuanlage-Zweig des Wizards ist m_ID_Projekt lediglich eine geratene MAX(ID)+1.
        /// </summary>
        private bool ProjektIstGespeichert()
        {
            if (m_ID_Projekt <= 0) return false;

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?",
                new DbParam("@id", m_ID_Projekt));

            return anzahl != null && Convert.ToInt32(anzahl) > 0;
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            float[] result = new float[8760];
            List<string> list;

            if (textBox_Stromname.Text == "")
            {
                MessageBox.Show("Bitte einen Entrag aus der Liste auswählen!");
                return;
            }

            SummeFuerSimulationSichern();
 
            simulation.m_ID_Projekt = m_ID_Projekt;
            
            list = listView_Strom_Auswahl.Items.Cast<ListViewItem>().Select(item => item.Text).ToList();
            result = simulation.Stromprofil_Strombedarf_berechnen(list);
            if (result == null) return;
 
            //simulation.Strombedarf_gesamt = simulation.com.I_vector_summe(result);
            simulation.Strombedarf_gesamt = result.Sum(); 

            Array.Copy(result, simulation.Strombedarf_viertelStundenwerte, result.Length);
            //simulation.com.I_monats_summe(simulation.Strombedarf_viertelStundenwerte, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.Strombedarf_viertelStundenwerte, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);
            simulation.Strombedarf_Max = simulation.Maximaler_Strombedarf(simulation.Strombedarf_viertelStundenwerte);
            simulation.Strombedarf_gesamt = simulation.Strombedarf_Gebaeude_gesamt;

            // iU9-W8.2: Blazor-Huelle statt Form_ErgStromverbraucher (Reiter 1 = monatlich).
            BedarfErgebnisHuelle.Zeigen(this, simulation, 1);
            btn_ErgebnisseVerbrauch.Enabled = true;
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            // iU9-W8.2: Blazor-Huelle statt Form_ErgStromverbraucher (Reiter 1 = monatlich).
            BedarfErgebnisHuelle.Zeigen(this, simulation, 1);
        }

        private void btn_Strom_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            frm.m_szStromname = textBox_Stromname.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szStromtyp = textBox_Stromtyp.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt); 
        }

        private void btn_Strom_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                frm.m_szStromname = szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.ShowDialog();
                SetControls(m_szProjekt);
            }
        }

        private void btn_Strom_loeschen_Click(object sender, EventArgs e)
        {
            // Sicherheitsprüfung, ob überhaupt ein Eintrag ausgewählt wurde
            if (string.IsNullOrEmpty(listBox_Strom_DB.Text))
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Eintrag aus!");
                return;
            }

            DialogResult dialogResult = MessageBox.Show(
                $"Soll {listBox_Strom_DB.Text} wirklich gelöscht werden ?",
                "Löschen",
                MessageBoxButtons.YesNo
            );

            if (dialogResult == DialogResult.No) return;

            // Delete prueft selbst auf ReadOnly und meldet ggf.
            StromverbraucherStammCtrl ctrl_del = new StromverbraucherStammCtrl();
            if (!ctrl_del.Delete(listBox_Strom_DB.Text)) return;
            listBox_Strom_DB.Items.Remove(listBox_Strom_DB.Text);
        }

        private void btn_StromtypDBedit_Click(object sender, EventArgs e)
        {
            Form_EingStromTyp frm = new Form_EingStromTyp();
            frm.SetControls();
            frm.ShowDialog(); 
        }

        private void btn_neuerWert_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strom_Auswahl.SelectedIndices;
            if (indexes.Count == 0 || textBox_Verbrauch.Text.Trim() == "")
            {
                MessageBox.Show("Bitte einen Eintrag aus der Liste auswählen und einen Wert eingeben!");
                return;
            }

            // Pruefung beim Aktionsknopf statt im TextChanged (Muster
            // ProjektPuffer.TemperaturenPruefen): TryParse, sprechende Meldung, Feld
            // markieren, Dialog bleibt offen. double.Parse() stand hier ungesichert und
            // haette nach dem Wegfall des Undo() eine FormatException geworfen.
            double dVerbrauch;
            if (!Program.ZahlParsen(textBox_Verbrauch.Text, out dVerbrauch) || dVerbrauch < 0)
            {
                MessageBox.Show("Eingaben überprüfen: \"" + textBox_Verbrauch.Text + "\"" + Environment.NewLine +
                                "Bitte den Jahresverbrauch als Zahl in kWh eingeben, z. B. 12,5.",
                                "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Verbrauch.Focus();
                textBox_Verbrauch.SelectAll();
                return;
            }

            list_sbmodel[indexes[0]].m_Summe = dVerbrauch;
            textBox_Jahres_Verbrauch.Text = dVerbrauch.ToString("F2");
            textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
            pictureBox1.Visible = true;
            pictureBox1.Refresh();
            Thread.Sleep(500);
            pictureBox1.Visible = false;
        }

        /// <summary>
        /// Stiller Hinweis statt fokushaltender Pruefung.
        ///
        /// Die alte Fassung meldete jede Zwischeneingabe modal und nahm sie mit
        /// tb.Undo() zurueck. Undo() loest TextChanged erneut aus und schaltet dabei
        /// zwischen Rueckgaengig und Wiederherstellen um: War das Feld vorher leer
        /// (also ebenfalls keine Zahl), pendelte der Text zwischen Fehleingabe und
        /// Leerstand, die Meldung kam nach jedem OK sofort zurueck und der Dialog war
        /// gefangen. Geprueft wird jetzt erst beim Uebernehmen (btn_neuerWert_Click);
        /// hier bleibt nur die Feldfarbe als Hinweis.
        /// </summary>
        private void textBox_Verbrauch_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            double wert;
            bool bOk = tb.Text.Trim().Length == 0 || Program.ZahlParsen(tb.Text, out wert);
            tb.BackColor = bOk ? SystemColors.Window : Color.FromArgb(255, 235, 235);
        }
    }
}
