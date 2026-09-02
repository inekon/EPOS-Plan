using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_WP : BaseForm
    {
        private WPModel item;
        public bool CloseWithOK = false;
        private WPStammCtrl ctrl = null;
        private bool neu = false;

        /// <summary>
        /// false, solange <see cref="OnLoad"/> noch nicht gelaufen ist. Solange darf
        /// KEIN Control ausgeblendet werden - Begründung siehe <see cref="OnLoad"/>.
        /// </summary>
        private bool m_bGeladen = false;

        /// <summary>Ä19: Geräte-Modulkosten werden nicht mehr hier gepflegt — die
        /// Kosten laufen über die Kostenverwaltung (Komponente Wärmepumpe). Die Zeile
        /// wird verborgen; das Feld bleibt befüllt, damit der bestehende Speicherweg
        /// (Pflichtprüfung + Update) den Altwert unverändert mitschreibt.</summary>
        private void ModulkostenVerbergen()
        {
            Control eltern = textBox_Modulkosten.Parent;
            if (eltern == null) return;
            // ENTFERNEN statt Verbergen: Der Offscreen-Weg (DrawToBitmap) dieser
            // Alt-Dialoge zeichnet per Visible=false versteckte Controls weiter
            // (Befund Ä19). Das Textfeld wird nur ausgehängt — sein Text bleibt
            // für Pflichtprüfung und Update-Speicherweg lesbar.
            eltern.Controls.Remove(textBox_Modulkosten);
            foreach (string name in new[] { "label32", "label33" })
            {
                Control[] c = this.Controls.Find(name, true);
                if (c.Length > 0 &&
                    ((c[0].Text ?? "").StartsWith("Modulkosten") || c[0].Text == "€"))
                {
                    c[0].Parent.Controls.Remove(c[0]);
                    c[0].Dispose();
                }
            }
        }

        public Form_WP()
        {
            InitializeComponent();
            ModulkostenVerbergen();   // Ä19

            // Dezenter Einstieg in den Assistenten, oben rechts im Client-Bereich
            // (Fachkonzept 11.8). Programmatisch, damit Designer und .resx
            // unberuehrt bleiben.
            KiAufrufKnopf.Anbringen(this);

            listBox_WP.DrawMode = DrawMode.OwnerDrawFixed;
            listBox_WP.DrawItem += listBox_WP_DrawItem;
            item = new WPModel();
            ctrl = new WPStammCtrl();
            ctrl.ReadAll();
            FillWPList();
            InitChart("WÄRME");
            FusszeileNormen();
        }

        public Form_WP(string wpname)
        {
            InitializeComponent();
            ModulkostenVerbergen();   // Ä19

            // Dezenter Einstieg in den Assistenten, oben rechts im Client-Bereich
            // (Fachkonzept 11.8). Programmatisch, damit Designer und .resx
            // unberuehrt bleiben.
            KiAufrufKnopf.Anbringen(this);

            listBox_WP.DrawMode = DrawMode.OwnerDrawFixed;
            listBox_WP.DrawItem += listBox_WP_DrawItem;
            item = new WPModel();
            ctrl = new WPStammCtrl();
            ctrl.ReadAll("Bezeichner='" + wpname + "'");
            FillWPList();
            textBox_Name.Enabled = false;
            btn_Neu.Enabled=false;
            btn_Loeschen.Enabled = false;
            InitChart("WÄRME");
            FusszeileNormen();
        }

        /// <summary>
        /// D2 (28.08.2026): Fußzeile auf die Norm. Die Zeile trägt VIER Knöpfe —
        /// „Speichern" (410/600), „Neu" (533/600), „Löschen" (634/600) und den
        /// Abschlussknopf <c>btn_Beenden</c> (748/600), beschriftet mit „OK". Sie standen
        /// in drei Größen (117x30 bzw. 95x30, zur Laufzeit auf 136x35 bzw. 111x35
        /// hochskaliert), unverankert und mit 39 px Abstand zum rechten Rand.
        ///
        /// Die Norm nimmt die ganze Reihe: Abschluss ganz rechts, davor die
        /// Satzverwaltung in ihrer bisherigen Reihenfolge von links nach rechts. Nur den
        /// Abschlussknopf zu normen ginge nicht — „Speichern" käme dann auf „Neu" und
        /// „Löschen" zu liegen.
        ///
        /// Beide Konstruktoren rufen die Methode, damit Pflege- und Ansichtsbetrieb
        /// dieselbe Zeile zeigen.
        ///
        /// <para><b>D3 (28.08.2026) — die Knopfrolle.</b> <c>btn_Beenden</c> trug die
        /// Aufschrift „OK" (deutsch wie englisch), sein <c>DialogResult</c> ist
        /// <c>None</c>, und sein Behandler <see cref="butt_Beenden_Click"/> setzt lediglich
        /// <c>CloseWithOK = true</c> und ruft <c>Close()</c>. Dieses Feld liest NIEMAND:
        /// Die beiden Aufrufer (<c>MenueCtrl</c> und <c>Wizard_WPItem</c>) werten es nicht
        /// aus, die Treffer in <c>Form_WPAuswahl</c> gehören zum gleichnamigen Feld von
        /// <c>Wizard_WPItem</c>. Der Knopf SCHLIESST also nur — gespeichert wird
        /// ausschließlich über „Speichern". Eine Aufschrift „OK" sagt dem Anwender das
        /// Gegenteil (Eingaben werden übernommen). Text und Rolle stimmen jetzt überein:
        /// „Beenden" / „Finish", dieselbe Beschriftung wie beim baugleichen Abschlussknopf
        /// von <c>Form_Simulation_Config</c> und <c>Form_Simulation_Detail</c>, und
        /// dieselbe wie der Name des Knopfes.</para>
        ///
        /// <para>Am VERHALTEN ändert sich nichts: kein <c>DialogResult</c>, kein
        /// Behandler, kein Speicherweg. Der Text kommt aus dem zentralen Katalog
        /// (<c>MyResource.Resource.WP_BTN_BEENDEN</c>, de + en) und nicht aus der
        /// <c>.resx</c> des Formulars — die bleibt wie alle Designer-Dateien unangetastet.
        /// Er wird VOR dem Einhängen gesetzt, damit die Norm die Mindestbreite am neuen
        /// Text misst.</para>
        /// </summary>
        private void FusszeileNormen()
        {
            if (btn_Beenden != null) btn_Beenden.Text = MyResource.Resource.WP_BTN_BEENDEN;
            FusszeilenNorm.Einhaengen(this, btn_Beenden, btn_Loeschen, btn_Neu, btn_Speichern);
        }

        public void FillWPList()
        {
            listBox_WP.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_WP.Items.Add(ctrl.items[i].WPName);
            }
            listBox_WP.SetSelected(0,true);
        }

        /// <summary>
        /// Holt das Ausblenden der Betriebsart-Radiobuttons nach, das der
        /// Konstruktor-Durchlauf bewusst übersprungen hat - und zwar erst NACH dem
        /// Laden.
        ///
        /// Hintergrund (Muster aus Wizard_WPItem, Commit d49075e): Beide Konstruktoren
        /// rufen FillWPList() auf; das SetSelected(0, true) darin löst
        /// listBox_WP_SelectedIndexChanged bereits VOR ShowDialog aus. Hat die erste
        /// Wärmepumpe keine Kühl-Kenndaten, würden radioButton_Kuehlung/-_Waerme dort
        /// ausgeblendet, bekämen beim Formularaufbau kein Fensterhandle und verpassten
        /// den AutoScroll-Versatz der BaseForm (sie staucht das Formular auf die
        /// Bildschirm-Arbeitsfläche und scrollt den Inhalt). Beim späteren Einblenden -
        /// Auswahl einer Wärmepumpe MIT Kühl-Kenndaten im gescrollten Zustand - stünden
        /// die Radiobuttons auf der ungescrollten Entwurfsposition (Default-Anker
        /// Top|Left, kein Anker-Layout, das die Position korrigieren würde). Deshalb
        /// bleiben sie bis hierher sichtbar (Handle!) und werden erst jetzt - noch vor
        /// dem ersten Zeichnen, also ohne Aufblitzen - passend zur vorselektierten
        /// Wärmepumpe ausgeblendet.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            m_bGeladen = true;

            if (!HatKuehlKenndaten())
            {
                radioButton_Kuehlung.Visible = false;
                radioButton_Waerme.Visible = false;
            }
        }

        /// <summary>
        /// Liefert true, wenn zur aktuell gewählten Wärmepumpe Kühl-Kenndaten
        /// vorliegen (dieselbe Abfrage wie in listBox_WP_SelectedIndexChanged).
        /// </summary>
        private bool HatKuehlKenndaten()
        {
            RecordSet rs = new RecordSet();
            rs.Open("SELECT * FROM Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID);
            bool gefunden = rs.Next();
            rs.Close();
            return gefunden;
        }

        // Schreibgeschützte (ReadOnly) Wärmepumpen in der Liste grau darstellen.
        private void listBox_WP_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            bool ro = (ctrl != null && e.Index < ctrl.rows && ctrl.items[e.Index] != null && ctrl.items[e.Index].m_bReadOnly);
            Color foreColor = ro ? Color.Gray : e.ForeColor;
            using (SolidBrush brush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(listBox_WP.Items[e.Index].ToString(), e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        public void SetControls()
        {
            comboBox_Waermepumpentyp.Items.Clear();
            comboBox_Leistungsstufen.Items.Clear();
            comboBox_Aufstellung.Items.Clear();
            comboBox_Baujahr.Items.Clear();

            textBox_Beschreibung.Text = item.Beschreibung;
            comboBox_Baujahr.Text = item.Baujahr.ToString();
            comboBox_Leistungsstufen.Text = item.Regelung;
            comboBox_Waermepumpentyp.Text = item.Typ;
            textBox_Hersteller.Text = item.Firma;
            textBox_Modulkosten.Text = item.Modulkosten.ToString();
            textBox_Nennleistung.Text = item.Nennleistung.ToString();
            textBox_Heizstab.Text = item.Heizung.ToString();
            comboBox_Aufstellung.Text = item.Aufstellung;
            textBox_Kuehlung.Text = item.Kuehlleistung.ToString(); 

            comboBox_Waermepumpentyp.Items.Add("Sole-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Wasser-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Luft-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Luft-Luft-Klimagerät");
            comboBox_Leistungsstufen.Items.Add("einstufig");
            comboBox_Leistungsstufen.Items.Add("zweistufig");
            comboBox_Leistungsstufen.Items.Add("stetig");
            comboBox_Aufstellung.Items.Add("Außenaufstellung");
            comboBox_Aufstellung.Items.Add("Innen- oder Außenaufstellung");
            comboBox_Aufstellung.Items.Add("Innenaufstellung");
            comboBox_Aufstellung.Items.Add("ohne Typenbeschreibung");
            comboBox_Baujahr.Items.Add("2025");
            comboBox_Baujahr.Items.Add("2024");
            comboBox_Baujahr.Items.Add("2023");
            comboBox_Baujahr.Items.Add("2024");
            comboBox_Baujahr.Items.Add("2021");
            comboBox_Baujahr.Items.Add("2020");
            comboBox_Baujahr.Items.Add("2019");
            comboBox_Baujahr.Items.Add("2018");
            comboBox_Baujahr.Items.Add("2017");
            comboBox_Baujahr.Items.Add("2016");
            
            
        }

        private void InitChart(string mode)
        {
            // Erstellen eines Datasets und Füllen mit Daten
            string sql_Waerme = "select * from Tab_Kenndaten_STAMM where ID_WP = " + item.ID + " order by Temperatur ASC";
            string sql_Kuehlung;
            string sql = "";

            if (mode == "WÄRME")
            {
                sql = sql_Waerme;
            }
            else
            {
                RecordSet rs = new RecordSet();
                rs.Open("SELECT MAX(Last) as maxwert FROM Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID);
                if (rs.Next())
                {
                    if (rs.Read("maxwert") != DBNull.Value)
                    {
                        int last = (int)rs.Read("maxwert");
                        sql_Kuehlung = "select * from Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID + " and Last=" + last + " order by Temperatur ASC";
                        
                    }
                    else
                        sql_Kuehlung = "select * from Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID + " order by Temperatur ASC";

                }
                else
                    sql_Kuehlung = "select * from Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID + " order by Temperatur ASC";
                rs.Close();
                sql = sql_Kuehlung;
            }

            // Das Repository liefert direkt die fertige DataTable
            DataTable dataTable = DataRepository.GetDataTable(sql);

            chart1.ChartAreas[0].AxisX.Title = "Temperatur";
            chart1.ChartAreas[0].AxisY.Title = "COP";
            chart1.Series.Clear();

            chart2.ChartAreas[0].AxisX.Title = "Temperatur";
            chart2.ChartAreas[0].AxisY.Title = "Leistung";
            chart2.Series.Clear();

            KenndatenCtrl ctrl = new KenndatenCtrl();
            if(mode == "WÄRME")
                ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten_STAMM GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + item.ID);
            else
                ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten_Kuehlung_STAMM GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + item.ID);

            for (int i = 0; i < ctrl.rows; i++)
            {
                chart1.Series.Add(ctrl.items[i].m_nVorlauf.ToString());
                chart1.Series[i].Name = ctrl.items[i].m_nVorlauf.ToString() + "°C";
                chart1.Series[i].BorderWidth = 3;
                chart1.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ
                chart2.Series.Add(ctrl.items[i].m_nVorlauf.ToString());
                chart2.Series[i].Name = ctrl.items[i].m_nVorlauf.ToString() + "°C";
                chart2.Series[i].BorderWidth = 3;
                chart2.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ

                chart1.Series[i].XValueMember = "Temperatur";
                chart2.Series[i].XValueMember = "Temperatur";
                chart1.Series[i].YValueMembers = "COP";
                chart2.Series[i].YValueMembers = "Leistung";

                chart1.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart1.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart1.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart1.Series[i].Points.DataBind(dataTable.Select("Vorlauf=" + ctrl.items[i].m_nVorlauf.ToString()), "Temperatur", "COP", "");
                chart1.Series[i].MarkerSize = 5;
                chart1.Series[i].MarkerStyle = MarkerStyle.Circle;
                chart1.Series[i].MarkerColor = chart2.Series[i].Color;

                chart2.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart2.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart2.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart2.Series[i].MarkerSize = 5;
                chart2.Series[i].MarkerStyle = MarkerStyle.Cross;
                chart2.Series[i].MarkerColor = chart2.Series[i].Color;

                if (mode== "WÄRME")  
                    chart2.Series[i].Points.DataBind(dataTable.Select("Vorlauf=" + ctrl.items[i].m_nVorlauf.ToString()), "Temperatur", "Ptherm", "");
                else
                    chart2.Series[i].Points.DataBind(dataTable.Select("Vorlauf=" + ctrl.items[i].m_nVorlauf.ToString()), "Temperatur", "Pkuehl", "");
                
            }
        }

        private void butt_Beenden_Click(object sender, EventArgs e)
        {
            CloseWithOK = true;
            Close();
        }

        private void listBox_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBox_WP.SelectedIndex;
            if (index != -1)
            {
                item = ctrl.items[index];
                textBox_Name.Text = item.WPName;
                SetControls();
                InitChart("WÄRME");

                RecordSet rs = new RecordSet();
                rs.Open("SELECT * FROM Tab_Kenndaten_Kuehlung_STAMM where ID_WP = " + item.ID);
                if (!rs.Next())
                {
                    // Vor OnLoad nichts ausblenden - sonst fehlt den Radiobuttons das
                    // Handle und damit der AutoScroll-Versatz der BaseForm (siehe
                    // OnLoad). Den richtigen Startzustand stellt OnLoad her.
                    if (m_bGeladen)
                    {
                        radioButton_Kuehlung.Visible = false;
                        radioButton_Waerme.Visible = false;
                    }
                }
                else
                {
                    radioButton_Kuehlung.Visible = true;
                    radioButton_Waerme.Visible = true;
                    radioButton_Waerme.PerformClick();
                }   

                rs.Close(); 
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (!neu && item != null && item.m_bReadOnly)
            {
                MessageBox.Show("Diese Wärmepumpe ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ctrl.ReadSingle("select * from Tab_WP_STAMM where Bezeichner='" + item.WPName + "'");

            // Folgepaket zu ab5bf32: Modulkosten erst hier prüfen (im TextChanged wird
            // nur noch gefärbt) - sprechende Meldung, Fokus aufs Feld, Dialog bleibt
            // offen. Die Schaltflächen setzen kein DialogResult, ein return genügt.
            int nModulkosten;
            if (!Program.GanzzahlPruefen(textBox_Modulkosten, "Modulkosten", out nModulkosten, leerErlaubt: false)) return;

            ctrl.Modulkosten = nModulkosten;
            // Die übrigen Zahlenfelder wie bisher still übernehmen: ungültiger Text
            // lässt den gerade gelesenen Datensatzwert stehen, statt wie Int32.Parse
            // mit einer FormatException abzubrechen.
            int nWert;
            if (Program.GanzzahlParsen(textBox_Nennleistung.Text, out nWert)) ctrl.Nennleistung = nWert;
            ctrl.Beschreibung = textBox_Beschreibung.Text;
            if (Program.GanzzahlParsen(comboBox_Baujahr.Text, out nWert)) ctrl.Baujahr = nWert;
            ctrl.Regelung = comboBox_Leistungsstufen.Text;
            ctrl.Typ = comboBox_Waermepumpentyp.Text;
            ctrl.Firma = textBox_Hersteller.Text;
            if (Program.GanzzahlParsen(textBox_Heizstab.Text, out nWert)) ctrl.Heizung = nWert;
            ctrl.Aufstellung = comboBox_Aufstellung.Text;
            ctrl.WPName = textBox_Name.Text;
            
            bool result;
            if (!neu)
            {
                result = ctrl.Update();
            }
            else
            {
                result = ctrl.Insert();
            }
             
            if (result)
            {
                ctrl.ReadAll();
                FillWPList();
                listBox_WP.SelectedIndex = listBox_WP.FindString(ctrl.WPName);
                MessageBox.Show("Gespeichert");
            }
            else
            {
                listBox_WP.SelectedIndex = 0; 
                MessageBox.Show("Speicherung nicht möglich, Fehler aufgetreten!");
            }
            neu = false;
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (item != null && item.m_bReadOnly)
            {
                MessageBox.Show("Diese Wärmepumpe ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Wollen Sie wirklich die Wärmepumpe löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                RecordSet rs = new RecordSet();
                rs.Open("SELECT Tab_Projekt.ID, Tab_Projekt.Projektname FROM Tab_Projekt INNER JOIN " +
                    "Tab_Energieanlagen ON Tab_Projekt.ID = " +
                    "Tab_Energieanlagen.ID_Projekt where Tab_Energieanlagen.Bezeichner='" + item.WPName + "'");
                if (rs.Next())
                {
                    MessageBox.Show("Löschen nicht möglich!\nDiese Wärmepumpe ist dem Projekt " + rs.Read("Projektname") + " zugeordnet!", "Hinweis");
                    return;
                }

                ctrl.ReadSingle("select * from Tab_WP_STAMM where Bezeichner = '" + listBox_WP.Text + "'");
                ctrl.Delete();
                ctrl.ReadAll();
                FillWPList();
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            WPModel itemneu = new WPModel();
            neu = true;
            listBox_WP.ClearSelected();
            textBox_Beschreibung.Text = itemneu.Beschreibung;
            comboBox_Baujahr.Text = itemneu.Baujahr.ToString();
            comboBox_Leistungsstufen.Text = itemneu.Regelung;
            comboBox_Waermepumpentyp.Text = itemneu.Typ;
            textBox_Hersteller.Text = itemneu.Firma;
            textBox_Modulkosten.Text = itemneu.Modulkosten.ToString();
            textBox_Nennleistung.Text = itemneu.Nennleistung.ToString();
            textBox_Heizstab.Text = itemneu.Heizung.ToString();
            comboBox_Aufstellung.Text = itemneu.Aufstellung;
            textBox_Name.Text = "";
        }

        private void btn_Kenndaten_Click(object sender, EventArgs e)
        {
            bool roKenn = (item != null && item.m_bReadOnly);
            if (roKenn)
                MessageBox.Show("Diese Wärmepumpe ist schreibgeschützt (ReadOnly). Die Kennliniendaten können nur angesehen, nicht geändert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 1. SQL-Abfrage mit Parameter definieren (Schutz vor SQL-Injection)
            string sql = "SELECT ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm FROM Tab_Kenndaten_STAMM WHERE ID_WP = ?";

            DataSet ds = new DataSet();

            // 2. ARBEITSPAKET S4b: Die eigene Verbindung samt OleDbDataAdapter ist der
            //    Zugriffsschicht gewichen. GetDataTable liefert die Zeilen im Zustand
            //    "Unchanged" - genau wie Adapter.Fill; der Dialog erzeugt daraus
            //    Added/Modified/Deleted, und die Auswertung unten bleibt unveraendert.
            {
                {
                    ds.Tables.Add(DataRepository.GetDataTable(sql,
                        new OleDbParameter("?", OleDbType.Integer) { Value = item.ID }));

                    // Das Formular aufrufen (es bekommt das DataSet per ref wie im Original)
                    Kenndaten frm = new Kenndaten(ref ds);
                    frm.m_ID_WP = item.ID;

                    DialogResult ret = frm.ShowDialog();

                    if (ret == DialogResult.OK && !roKenn)
                    {
                        // Änderungen explizit und typisiert zurückschreiben – ein
                        // CommandBuilder kommt hier bewusst nicht zum Einsatz (er warf
                        // bei Access/ACE einen Prepare-Fehler, und die Zugriffsschicht
                        // fuehrt ohnehin keinen mit).
                        DataTable dtK = ds.Tables[0];

                        int nextId = 1;
                        object mxId = DataRepository.ExecuteScalar("SELECT Max(ID) FROM Tab_Kenndaten_STAMM");
                        if (mxId != null && mxId != DBNull.Value) nextId = Convert.ToInt32(mxId) + 1;

                        foreach (DataRow rK in dtK.Rows)
                        {
                            if (rK.RowState == DataRowState.Deleted)
                            {
                                DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_STAMM WHERE ID = ?",
                                    new OleDbParameter("@id", OleDbType.Integer) { Value = Convert.ToInt32(rK["ID", DataRowVersion.Original]) });
                            }
                            else if (rK.RowState == DataRowState.Added)
                            {
                                DataRepository.ExecuteSQL(
                                    "INSERT INTO Tab_Kenndaten_STAMM (ID, ID_WP, Vorlauf, Temperatur, COP, Ptherm) VALUES (?, ?, ?, ?, ?, ?)",
                                    new OleDbParameter("@id",  OleDbType.Integer) { Value = nextId++ },
                                    new OleDbParameter("@wp",  OleDbType.Integer) { Value = item.ID },
                                    new OleDbParameter("@vl",  OleDbType.Integer) { Value = KInt(rK["Vorlauf"]) },
                                    new OleDbParameter("@t",   OleDbType.Integer) { Value = KInt(rK["Temperatur"]) },
                                    new OleDbParameter("@cop", OleDbType.Double)  { Value = KDbl(rK["COP"]) },
                                    new OleDbParameter("@pt",  OleDbType.Double)  { Value = KDbl(rK["Ptherm"]) });
                            }
                            else if (rK.RowState == DataRowState.Modified)
                            {
                                DataRepository.ExecuteSQL(
                                    "UPDATE Tab_Kenndaten_STAMM SET ID_WP = ?, Vorlauf = ?, Temperatur = ?, COP = ?, Ptherm = ? WHERE ID = ?",
                                    new OleDbParameter("@wp",  OleDbType.Integer) { Value = item.ID },
                                    new OleDbParameter("@vl",  OleDbType.Integer) { Value = KInt(rK["Vorlauf"]) },
                                    new OleDbParameter("@t",   OleDbType.Integer) { Value = KInt(rK["Temperatur"]) },
                                    new OleDbParameter("@cop", OleDbType.Double)  { Value = KDbl(rK["COP"]) },
                                    new OleDbParameter("@pt",  OleDbType.Double)  { Value = KDbl(rK["Ptherm"]) },
                                    new OleDbParameter("@id",  OleDbType.Integer) { Value = Convert.ToInt32(rK["ID"]) });
                            }
                        }

                        ds.AcceptChanges();

                        // Chart aktualisieren
                        InitChart("WÄRME");
                    }
                }
            }
        }

        private void radioButton_Waerme_CheckedChanged(object sender, EventArgs e)
        {
            InitChart("WÄRME");
        }

        private void radioButton_Kuehlung_CheckedChanged(object sender, EventArgs e)
        {
            InitChart("KÜHLUNG");
        }

        /// <summary>
        /// Färbt nur noch (Begründung siehe Program.GanzzahlFaerben); gemeldet wird
        /// erst beim Speichern. Ganzzahl, weil Modulkosten als Int32 abgelegt werden.
        /// </summary>
        private void textBox_Modulkosten_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void btn_Katalog_Click(object sender, EventArgs e)
        {
            Form_WpFilterAuswahl frmauswahl = new Form_WpFilterAuswahl();
            DialogResult result = frmauswahl.ShowDialog();
            if (result != DialogResult.OK) return;
            listBox_WP.Text = frmauswahl.SelectedWP.Bezeichnung;
        }

        // Hilfsfunktionen für die typisierte Kennlinien-Speicherung
        private static int KInt(object v) => (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        private static double KDbl(object v) => (v == null || v == DBNull.Value) ? 0 : Convert.ToDouble(v);
    }
}
