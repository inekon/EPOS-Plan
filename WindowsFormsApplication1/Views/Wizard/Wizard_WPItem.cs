using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Wizard_WPItem : BaseForm
    {
        public WErzeugerModel item;
        public List<WErzeugerModel> m_werzitemlist = null;
        private string WPName;
        public bool CloseWithOK = false;
        private int m_nID_WP = 0;

        /// <summary>
        /// Vorschlagswerte der Rücklauf-Auswahl [°C].
        ///
        /// Etappe 4: Die frühere Liste begann bei 25 °C und sprang in 5-K-Schritten
        /// (25/30/35/40/45) — für ein Niedertemperatursystem wie 35/28 gab es damit
        /// keinen passenden Eintrag, obwohl die ComboBox frei beschreibbar ist. Der
        /// Anwender wurde also auf hohe Rückläufe gelenkt, und über
        /// <c>Tab_Energieanlagen.[Rücklauf]</c> wanderte das direkt in die
        /// Systemvorgabe des Projekts (PufferSpCtrl.SystemRuecklauf).
        ///
        /// Die Liste ist nach unten erweitert und im unteren Bereich feiner gestuft;
        /// sie bleibt eine reine VORSCHLAGSLISTE ohne Grenzwirkung.
        /// </summary>
        private static readonly string[] RUECKLAUF_VORSCHLAEGE =
            { "20", "22", "25", "28", "30", "32", "35", "40", "45" };

        public Wizard_WPItem()
        {
            item = new WErzeugerModel();
            InitializeComponent();
            FillWPList();

            comboBox_Ruecklauf.Items.AddRange(RUECKLAUF_VORSCHLAEGE);

            comboBox_Betriebsart.Items.Add("Alternativbetrieb");
            comboBox_Betriebsart.Items.Add("Parallelbetrieb");
            comboBox_Betriebsart.Items.Add("Teilparallelbetrieb");

            comboBox_Betriebsart.Visible = false;
            textBox_Abschalttemp.Visible = false;
            label_AbschalttemperaturEinheit.Visible = false;
            label_Abschalttemperatur.Visible = false;
            label_Betriebsart.Visible = false;

            // Pufferspeicher-Bereich (Volumen, Kapazität, Anteil Solaranlage, rende MIX)
            // entfernt - der Pufferspeicher wird jetzt über die Zuordnung in der
            // Simulation-Konfiguration gepflegt. Gespeicherte Werte bleiben erhalten.
            groupBox1.Visible = false;
        }

        public Wizard_WPItem(string wpname)
        {
            WPName = wpname;
            item = new WErzeugerModel();
            InitializeComponent();
            FillWPList();
            FillVorlaufCombo(WPName);

            comboBox_Ruecklauf.Items.AddRange(RUECKLAUF_VORSCHLAEGE);

            comboBox_Betriebsart.Items.Add("Alternativbetrieb");
            comboBox_Betriebsart.Items.Add("Parallelbetrieb");
            comboBox_Betriebsart.Items.Add("Teilparallelbetrieb");

            // Pufferspeicher-Bereich entfernt (siehe Kommentar im anderen Konstruktor)
            groupBox1.Visible = false;
        }

        public void SetWPCombox(string wpname) { listBox_WP.Text = wpname; }

        public void FillVorlaufCombo(string wpname)
        {
            WPStammCtrl wpctrl = new WPStammCtrl();
            wpctrl.ReadSingle("select * from Tab_WP_STAMM where Bezeichner='" + wpname + "'");

            KenndatenCtrl ctrl = new KenndatenCtrl();
            ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten_STAMM GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + wpctrl.ID);
            comboBox_Vorlauf.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Vorlauf.Items.Add(ctrl.items[i].m_nVorlauf);
            }
        }

        public void FillWPList()
        {
            WPStammCtrl ctrl = new WPStammCtrl();
            ctrl.ReadAll();
            listBox_WP.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_WP.Items.Add(ctrl.items[i].WPName);
            }
            listBox_WP.Text = WPName;
        }

        public void SetControls(int index)
        {
            if (index >= 0)
            {
                item = m_werzitemlist.ElementAt(index);

                listBox_WP.Text = item.Bezeichner;
                textBox_Abschalttemp.Text = item.Abschaltpunkt.ToString();
                comboBox_Betriebsart.Text = item.Betriebsart;
                checkBox_Bivalent.Checked = item.Bivalenter_Betrieb;
                if (!item.Bivalenter_Betrieb)
                {
                    comboBox_Betriebsart.Visible = false;
                    textBox_Abschalttemp.Visible = false;
                    label_AbschalttemperaturEinheit.Visible = false;
                    label_Abschalttemperatur.Visible = false;
                    label_Betriebsart.Visible = false;
                }
                comboBox_Ruecklauf.Text = item.Ruecklauf.ToString();
                checkBox_Sperrzeit.Checked = item.Sperrung;
                textBox_bis.Text = item.Sperrzeit_bis.ToString();
                textBox_von.Text = item.Sperrzeit_von.ToString();
                comboBox_Vorlauf.Text = item.Vorlauf.ToString();
                comboBox_Ruecklauf.Text = item.Ruecklauf.ToString();
                checkBox_Heizstab.Checked = item.Heizstab;
                textBox_Volumen.Text = item.Volumen.ToString();
                checkBox_rendeMIX.Checked = item.rendeMix;
                textBox_Anteil.Text = item.Solaranteil.ToString();
                textBox_Nutzungszeit.Text = item.Nutzungszeit.ToString();
                textBox_PHeizstab.Text = item.Heizung.ToString();
  

                // WP spezifische Daten im Dialog mit anzeigen
                textBox_Beschreibung.Text = item.Beschreibung;
                textBox_Baujahr.Text = item.Baujahr.ToString();
                textBox_Leistungsstufen.Text = item.Regelung;
                textBox_Waermepumpentyp.Text = item.Typ;
                textBox_Hersteller.Text = item.Firma;
                textBox_Modulkosten.Text = item.Modulkosten.ToString();
                textBox_Nennleistung.Text = item.Nennleistung.ToString();
            }
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            if(comboBox_Betriebsart.Text == "" && checkBox_Bivalent.Checked)
            {
                MessageBox.Show("Bitte Betriebsart auswählen!");
                return;
            }
            if(listBox_WP.Text == "")
            {
                MessageBox.Show("Bitte Wärmepumpe auswählen!");
                return;
            }   
            // Etappe 4: dieselbe Prüfung wie überall, wo Vor-/Rücklauf eingegeben wird
            // (ProjektPuffer.TemperaturenPruefen). Behebt drei Schwächen der bisherigen
            // Fassung:
            //   - Int32.Parse auf einer FREI beschreibbaren ComboBox riss das Formular
            //     bei jeder Nicht-Zahl mit einer FormatException ab,
            //   - "<" statt "<=" ließ Vorlauf == Rücklauf durch (Spreizung 0, die
            //     Engine fiel danach still auf ihre Vorgabe zurück),
            //   - keine Obergrenze.
            // Eine Untergrenze gibt es weiterhin nicht: 35/28 und tiefer sind gültig.
            int nVorlauf, nRuecklauf;
            string fehlerTemperatur;
            if (!ProjektPuffer.TemperaturenPruefen(comboBox_Vorlauf.Text, comboBox_Ruecklauf.Text,
                                                   out nVorlauf, out nRuecklauf, out fehlerTemperatur))
            {
                MessageBox.Show(fehlerTemperatur);
                return;
            }

            item.Bezeichner = listBox_WP.Text;
         //   item.ID_Type = WizardItemClass.WP_TYP;
            item.Betriebsart = comboBox_Betriebsart.Text;
            item.Sperrung = checkBox_Sperrzeit.Checked;
            item.Sperrzeit_bis = Int32.Parse(textBox_bis.Text);
            item.Sperrzeit_von = Int32.Parse(textBox_von.Text);
            item.Ruecklauf = nRuecklauf;
            item.Vorlauf = nVorlauf;
            item.Bivalenter_Betrieb = checkBox_Bivalent.Checked;
            item.Abschaltpunkt = double.Parse(textBox_Abschalttemp.Text);
            item.Nutzungszeit = 0;
            item.ID_WP = m_nID_WP;
            item.ID_SP = 0;
            item.ID_PV = 0;
            item.ID_Solar = 0;
            item.Heizstab = checkBox_Heizstab.Checked;
            item.Heizung = Int32.Parse(textBox_PHeizstab.Text);
            // Pufferspeicher-Felder sind ausgeblendet - vorhandene Werte unverändert
            // übernehmen (die Felder werden in SetControls aus dem Datensatz gefüllt),
            // bei leeren Feldern den bisherigen Wert des Datensatzes behalten.
            double dVolumen;
            if (double.TryParse(textBox_Volumen.Text, out dVolumen)) item.Volumen = dVolumen;
            item.rendeMix = checkBox_rendeMIX.Checked;
            int nAnteil;
            if (Int32.TryParse(textBox_Anteil.Text, out nAnteil)) item.Solaranteil = nAnteil;
            item.Nutzungszeit = Int32.Parse(textBox_Nutzungszeit.Text);
            
            CloseWithOK = true;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            CloseWithOK = false;
            Close();
        }
        private void listBox_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillVorlaufCombo(listBox_WP.Text);
            WPStammCtrl wpctrl = new WPStammCtrl();
            wpctrl.ReadSingle("select * from Tab_WP_STAMM where Bezeichner='" + listBox_WP.Text + "'");
            m_nID_WP = wpctrl.ID;
            item.ID_WP = wpctrl.ID;
            // WP spezifische Daten im Dialog mit anzeigen

            // WP spezifische Daten im Dialog mit anzeigen
            textBox_Beschreibung.Text = wpctrl.Beschreibung;
            textBox_Baujahr.Text = wpctrl.Baujahr.ToString();
            textBox_Leistungsstufen.Text = wpctrl.Regelung;
            textBox_Waermepumpentyp.Text = wpctrl.Typ;
            textBox_Hersteller.Text = wpctrl.Firma;
            textBox_Modulkosten.Text = wpctrl.Modulkosten.ToString();
            textBox_Nennleistung.Text = wpctrl.Nennleistung.ToString();
            textBox_PHeizstab.Text = wpctrl.Heizung.ToString();

            // Sicher, sauber und ohne SQL-Injection (Verwendung von ? als Parameter)
            string sql = "SELECT * FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? ORDER BY Temperatur ASC";
            OleDbParameter parameter = new OleDbParameter("?", wpctrl.ID);

            // Das DataRepository übernimmt das Erstellen, Öffnen und Befüllen automatisch
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            chart1.ChartAreas[0].AxisX.Title = "Temperatur";
            chart1.ChartAreas[0].AxisY.Title = "COP";
            chart1.Series.Clear();

            chart2.ChartAreas[0].AxisX.Title = "Temperatur";
            chart2.ChartAreas[0].AxisY.Title = "Leistung";
            chart2.Series.Clear();

            for (int i = 0; i < comboBox_Vorlauf.Items.Count; i++)
            {
                chart1.Series.Add(comboBox_Vorlauf.Items[i].ToString());
                chart1.Series[i].Name = comboBox_Vorlauf.Items[i].ToString() + "°C";
                chart1.Series[i].BorderWidth = 3;
                chart1.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ
                chart1.Series[i].XValueMember = "Temperatur";
                chart1.Series[i].YValueMembers = "COP";
                chart1.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart1.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart1.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart1.Series[i].Points.DataBind(dt.Select("Vorlauf=" + comboBox_Vorlauf.Items[i].ToString()), "Temperatur", "COP", "");

                chart2.Series.Add(comboBox_Vorlauf.Items[i].ToString());
                chart2.Series[i].Name = comboBox_Vorlauf.Items[i].ToString() + "°C";
                chart2.Series[i].BorderWidth = 3;
                chart2.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ
                chart2.Series[i].XValueMember = "Temperatur";
                chart2.Series[i].YValueMembers = "Leistung";
                chart2.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart2.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart2.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart2.Series[i].Points.DataBind(dt.Select("Vorlauf=" + comboBox_Vorlauf.Items[i].ToString()), "Temperatur", "Ptherm", "");

            }
            return;
        }
        private void btn_WP_Click(object sender, EventArgs e)
        {
            Form_WP frm = new Form_WP(listBox_WP.Text);
            frm.ShowDialog();

            // WP spezifische Daten ggf. aktualisieren im Dialog
            WPCtrl wpctrl = new WPCtrl();
            wpctrl.ReadAll("ID=" + item.ID_WP);

            textBox_Beschreibung.Text = wpctrl.items[0].Beschreibung;
            textBox_Baujahr.Text = wpctrl.items[0].Baujahr.ToString();
            textBox_Leistungsstufen.Text = wpctrl.items[0].Regelung;
            textBox_Waermepumpentyp.Text = wpctrl.items[0].Typ;
            textBox_Hersteller.Text = wpctrl.items[0].Firma;
            textBox_Modulkosten.Text = wpctrl.items[0].Modulkosten.ToString();
            textBox_Nennleistung.Text = wpctrl.items[0].Nennleistung.ToString();
        }
        private void checkBox_Bivalent_CheckedChanged(object sender, EventArgs e)
        {
             if(checkBox_Bivalent.Checked)
             {
                comboBox_Betriebsart.Visible = true;
                label_Betriebsart.Visible = true;
                if (comboBox_Betriebsart.Text == "Teilparallelbetrieb")
                {
                    textBox_Abschalttemp.Visible = true;
                    label_AbschalttemperaturEinheit.Visible = true;
                    label_Abschalttemperatur.Visible = true;
                    
                }
                else
                {
                    textBox_Abschalttemp.Visible = false;
                    label_AbschalttemperaturEinheit.Visible = false;
                    label_Abschalttemperatur.Visible = false;
                }
            }
            else
            {
                comboBox_Betriebsart.Visible = false;
                textBox_Abschalttemp.Visible = false;
                label_AbschalttemperaturEinheit.Visible = false;
                label_Abschalttemperatur.Visible = false;
                label_Betriebsart.Visible = false;
            }
        }
        private void comboBox_Betriebsart_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_Betriebsart.Text == "Teilparallelbetrieb" && checkBox_Bivalent.Checked == true)
            {
                textBox_Abschalttemp.Visible = true;
                label_AbschalttemperaturEinheit.Visible = true;
                label_Abschalttemperatur.Visible = true;
            }
            else
            {
                textBox_Abschalttemp.Visible = false;
                label_AbschalttemperaturEinheit.Visible = false;
                label_Abschalttemperatur.Visible = false;

            }
        }
        private void textBox_Nutzungszeit_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }
        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }
        private void textBox_von_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }

        private void textBox_bis_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }
        private void textBox_Anteil_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) tb.Undo();
        }
        private void btn_Katalog_Click(object sender, EventArgs e)
        {
            Form_WpFilterAuswahl frmauswahl = new Form_WpFilterAuswahl();
            DialogResult result = frmauswahl.ShowDialog();
            if (result != DialogResult.OK) return;
            listBox_WP.Text = frmauswahl.SelectedWP.Bezeichnung;
        }
    }
}
