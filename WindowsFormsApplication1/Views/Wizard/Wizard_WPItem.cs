using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
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
        /// false, solange <see cref="OnLoad"/> noch nicht gelaufen ist. Solange darf
        /// KEIN Control ausgeblendet werden - Begruendung siehe <see cref="OnLoad"/>.
        /// </summary>
        private bool m_bGeladen = false;

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

            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_ALTERNATIV);
            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_PARALLEL);
            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_TEILPARALLEL);

            // Die Betriebsart- und Abschalttemperatur-Controls werden hier bewusst
            // NICHT ausgeblendet - das erledigt OnLoad (siehe dort). Sie muessen
            // waehrend der Handle-Erzeugung sichtbar sein, sonst nehmen sie den
            // AutoScroll-Versatz der BaseForm nicht mit.

            // Pufferspeicher-Bereich (Volumen, Kapazität, Anteil Solaranlage, rende MIX)
            // entfernt - der Pufferspeicher wird jetzt über die Zuordnung in der
            // Simulation-Konfiguration gepflegt. Gespeicherte Werte bleiben erhalten.
            groupBox1.Visible = false;

            // Ä19: Die Kostenzeile ersetzt die Modulkosten in JEDEM Zustand des
            // Dialogs — auch vor SetControls (leerer Neuanlage-Fall).
            KostenAnzeigeEinrichten();
        }

        public Wizard_WPItem(string wpname)
        {
            WPName = wpname;
            item = new WErzeugerModel();
            InitializeComponent();
            FillWPList();
            FillVorlaufCombo(WPName);

            comboBox_Ruecklauf.Items.AddRange(RUECKLAUF_VORSCHLAEGE);

            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_ALTERNATIV);
            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_PARALLEL);
            comboBox_Betriebsart.Items.Add(DbWerte.WP_BETRIEBSART_TEILPARALLEL);

            // Pufferspeicher-Bereich entfernt (siehe Kommentar im anderen Konstruktor)
            groupBox1.Visible = false;

            // Ä19: Die Kostenzeile ersetzt die Modulkosten in JEDEM Zustand des
            // Dialogs — auch vor SetControls (leerer Neuanlage-Fall).
            KostenAnzeigeEinrichten();
        }

        /// <summary>
        /// Setzt die Sichtbarkeit der Betriebsart-/Abschalttemperatur-Controls erstmalig -
        /// und zwar erst NACH dem Laden.
        ///
        /// Hintergrund: <see cref="BaseForm"/> staucht in ihrem OnLoad das 791 px hohe
        /// Formular auf die Bildschirm-Arbeitsflaeche; der Rest wandert in den
        /// AutoScroll-Bereich (gemessen: AutoScrollPosition -1/-46). Den Versatz gibt
        /// WinForms aber nur an Controls weiter, die zu diesem Zeitpunkt bereits ein
        /// Fensterhandle besitzen - und ein Handle bekommt beim Aufbau des Formulars
        /// nur, wer SICHTBAR ist. Wer vorher ausgeblendet wurde, blieb auf seiner
        /// Entwurfsposition stehen und lag nach dem Einblenden hinter dem gruenen Panel
        /// label21 (Z-Index 11 vor 17-21) - die Betriebsart-Auswahl war unerreichbar.
        ///
        /// Deshalb starten alle betroffenen Controls sichtbar (kein Visible=False mehr
        /// im Konstruktor und in der .resx), und die beiden Sichtbarkeits-Handler
        /// halten sich ueber <c>m_bGeladen</c> zurueck, bis hier der richtige Zustand
        /// gesetzt wird. Das Formular ist dabei noch nicht auf dem Bildschirm - ein
        /// Aufblitzen gibt es also nicht, weshalb OnLoad und nicht OnShown die
        /// richtige Stelle ist.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            m_bGeladen = true;

            // Deckt beide Faelle ab: bivalent an/aus sowie - ueber die
            // Betriebsart-Abfrage - die Bivalenztemperatur-Controls.
            checkBox_Bivalent_CheckedChanged(this, EventArgs.Empty);
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
                // Kein Ausblenden mehr an dieser Stelle: SetControls wird VOR ShowDialog
                // aufgerufen, ein hier unsichtbares Control bekaeme kein Handle und damit
                // nicht den AutoScroll-Versatz (siehe OnLoad). Die Sichtbarkeit setzt
                // OnLoad anhand von checkBox_Bivalent.Checked und comboBox_Betriebsart.Text;
                // die von den Zuweisungen oben ausgeloesten Handler laufen bis dahin leer.
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
                KostenAnzeigeEinrichten();   // Ä19: Kosten statt Modulkosten
  

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

            // Folgepaket zu ab5bf32: Zahlprüfung beim OK-Knopf statt im TextChanged -
            // sprechende Meldung, Fokus aufs Feld, Dialog bleibt offen. Bisher riss
            // Int32.Parse/double.Parse weiter unten bei leerem oder ungültigem Feld ab.
            int nSperrzeitVon, nSperrzeitBis, nNutzungszeit, nHeizstab;
            if (!Program.GanzzahlPruefen(textBox_von, "Sperrzeit von", out nSperrzeitVon, leerErlaubt: false)) return;
            if (!Program.GanzzahlPruefen(textBox_bis, "Sperrzeit bis", out nSperrzeitBis, leerErlaubt: false)) return;
            if (!Program.GanzzahlPruefen(textBox_Nutzungszeit, "Nutzungsdauer", out nNutzungszeit, leerErlaubt: false)) return;
            if (!Program.GanzzahlPruefen(textBox_PHeizstab, "Leistung Heizstab", out nHeizstab, leerErlaubt: false)) return;

            // Die Bivalenztemperatur ist je nach Betriebsart ausgeblendet - ein leeres
            // Feld ist deshalb erlaubt und lässt den bisherigen Wert stehen.
            double dAbschalt;
            if (!Program.ZahlPruefen(textBox_Abschalttemp, "Bivalenztemperatur", out dAbschalt, leerErlaubt: true)) return;
            bool bAbschaltGesetzt = textBox_Abschalttemp.Text.Trim().Length != 0;

            item.Bezeichner = listBox_WP.Text;
         //   item.ID_Type = WizardItemClass.WP_TYP;
            item.Betriebsart = comboBox_Betriebsart.Text;
            item.Sperrung = checkBox_Sperrzeit.Checked;
            item.Sperrzeit_bis = nSperrzeitBis;
            item.Sperrzeit_von = nSperrzeitVon;
            item.Ruecklauf = nRuecklauf;
            item.Vorlauf = nVorlauf;
            item.Bivalenter_Betrieb = checkBox_Bivalent.Checked;
            if (bAbschaltGesetzt) item.Abschaltpunkt = dAbschalt;
            item.Nutzungszeit = 0;
            item.ID_WP = m_nID_WP;
            item.ID_SP = 0;
            item.ID_PV = 0;
            item.ID_Solar = 0;
            item.Heizstab = checkBox_Heizstab.Checked;
            item.Heizung = nHeizstab;
            // Pufferspeicher-Felder sind ausgeblendet - vorhandene Werte unverändert
            // übernehmen (die Felder werden in SetControls aus dem Datensatz gefüllt),
            // bei leeren Feldern den bisherigen Wert des Datensatzes behalten.
            double dVolumen;
            if (double.TryParse(textBox_Volumen.Text, out dVolumen)) item.Volumen = dVolumen;
            item.rendeMix = checkBox_rendeMIX.Checked;
            int nAnteil;
            if (Int32.TryParse(textBox_Anteil.Text, out nAnteil)) item.Solaranteil = nAnteil;
            item.Nutzungszeit = nNutzungszeit;
            
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
        // ================================================================= Ä19

        /// <summary>Ä19 (Nutzerauftrag 26.08.2026): Geräte-„Modulkosten“ sind
        /// Kostendialog-Sache — die Zeile weicht den KOMPONENTENSUMMEN (Invest/Betrieb
        /// der Wärmepumpe aus der Kostenverwaltung) und dem Einstieg „Kosten
        /// bearbeiten…“ (Projektmodus, Komponente Wärmepumpe). Das Feld bleibt im
        /// Designer (resx-Layout unangetastet) und wird nur verborgen; sein Wert
        /// läuft im Speicherweg unverändert mit.</summary>
        private Button btnKosten;
        private Label lblKostenSummen;

        private void KostenAnzeigeEinrichten()
        {
            if (btnKosten != null) { KostenSummenAnzeigen(); return; }

            Control eltern = textBox_Modulkosten.Parent;
            if (eltern == null) return;
            int links = label32 != null ? 29 : textBox_Modulkosten.Left - 112;
            int oben = textBox_Modulkosten.Top;

            // ENTFERNEN statt Verbergen: Der Offscreen-Weg (DrawToBitmap) dieser
            // Alt-Dialoge zeichnet per Visible=false versteckte Controls weiter
            // (Befund Ä19). Das Textfeld wird nur ausgehängt — sein Wert läuft im
            // Speicherweg unverändert mit. label32/label33 sind Beschriftung und
            // €-Kästchen der Zeile (Namen aus dem Layout-Dump).
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

            btnKosten = new Button
            {
                Text = TWpi("WPI_BTN_KOSTEN", "Kosten bearbeiten…"),
                Location = new Point(links, oben - 2),
                Size = new Size(150, 25),
                UseVisualStyleBackColor = true
            };
            btnKosten.Click += new EventHandler(btnKosten_Click);
            eltern.Controls.Add(btnKosten);
            btnKosten.BringToFront();

            lblKostenSummen = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(26, 50, 97),
                Location = new Point(links + 158, oben + 2)
            };
            eltern.Controls.Add(lblKostenSummen);
            lblKostenSummen.BringToFront();

            KostenSummenAnzeigen();
        }

        /// <summary>Ä20: Invest-/Betriebssumme DIESER Anlage (Tab_ProjektWerte.ID_Anlage
        /// = Anlagenzeile item.ID); vor Migrationsschritt 45 bzw. ohne Anlage 0.</summary>
        private void KostenSummenAnzeigen()
        {
            if (lblKostenSummen == null) return;
            if (btnKosten != null)
                btnKosten.Enabled = item != null && item.ID_Projekt > 0;
            try
            {
                double invest = AnlagenSumme(Form_Kosten.KATEGORIE_INVESTITION);
                double betrieb = AnlagenSumme(Form_Kosten.KATEGORIE_BETRIEB);
                lblKostenSummen.Text = string.Format(
                    TWpi("WPI_KOSTEN_SUMMEN", "Invest {0:N0} € · Betrieb {1:N0} €/a"),
                    invest, betrieb);
            }
            catch { lblKostenSummen.Text = ""; }
        }

        private double AnlagenSumme(int kategorie)
        {
            if (item == null || item.ID <= 0 || item.ID_Projekt <= 0) return 0;
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return 0;
            object o = DataRepository.ExecuteScalar(
                "SELECT SUM(EingegebenerWert) FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KategorieID = ? AND ID_Anlage = ?",
                new OleDbParameter("@p", item.ID_Projekt),
                new OleDbParameter("@k", kategorie),
                new OleDbParameter("@a", item.ID));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToDouble(o);
        }

        private void btnKosten_Click(object sender, EventArgs e)
        {
            if (item == null || item.ID_Projekt <= 0) return;
            string projektname = "";
            try
            {
                var pc = new ProjektCtrl();
                pc.ReadSingle(item.ID_Projekt);
                if (pc.rows > 0) projektname = pc.m_szProjektname;
            }
            catch { }
            using (var dlg = new Form_KostenKomponente())
            {
                // Ä20: direkt die Kosten DIESER Anlage (item.ID = Anlagenzeile).
                dlg.SetProjekt(item.ID_Projekt, projektname, DbWerte.ERZEUGER_WAERMEPUMPE,
                               false, item.ID);
                dlg.ShowDialog(this);
            }
            KostenSummenAnzeigen();
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string TWpi(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        private void btn_WP_Click(object sender, EventArgs e)
        {
            Form_WP frm = new Form_WP(listBox_WP.Text);
            frm.ShowDialog();

            // WP spezifische Daten ggf. aktualisieren im Dialog
            WPCtrl wpctrl = new WPCtrl();
            wpctrl.ReadAll("ID=" + item.ID_WP);

            // Befund 26.08.2026: Wurde die Waermepumpe im Dialog geloescht oder
            // ist ID_WP nicht (mehr) vergeben, kommt eine LEERE Liste zurueck -
            // der ungepruefte items[0]-Zugriff warf eine
            // ArgumentOutOfRangeException. Dann bleibt die Anzeige unveraendert.
            if (wpctrl.items == null || wpctrl.items.Count == 0) return;

            var wp = wpctrl.items[0];
            textBox_Beschreibung.Text = wp.Beschreibung;
            textBox_Baujahr.Text = wp.Baujahr.ToString();
            textBox_Leistungsstufen.Text = wp.Regelung;
            textBox_Waermepumpentyp.Text = wp.Typ;
            textBox_Hersteller.Text = wp.Firma;
            textBox_Modulkosten.Text = wp.Modulkosten.ToString();
            textBox_Nennleistung.Text = wp.Nennleistung.ToString();
        }
        private void checkBox_Bivalent_CheckedChanged(object sender, EventArgs e)
        {
            // Vor OnLoad nichts ausblenden - sonst fehlt dem Control das Handle und
            // damit der AutoScroll-Versatz (siehe OnLoad). SetControls() laeuft vor
            // ShowDialog() und loest diesen Handler bereits aus; den richtigen
            // Zustand setzt danach OnLoad.
            if (!m_bGeladen) return;

            comboBox_Betriebsart.Visible = checkBox_Bivalent.Checked;
            label_Betriebsart.Visible = checkBox_Bivalent.Checked;
            BivalenztemperaturSichtbarkeitSetzen();
        }
        private void comboBox_Betriebsart_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Wie oben: vor OnLoad nicht ausblenden (SetControls setzt den Text und
            // loest diesen Handler damit schon vor ShowDialog aus).
            if (!m_bGeladen) return;

            BivalenztemperaturSichtbarkeitSetzen();
        }
        /// <summary>
        /// Blendet das Bivalenztemperatur-Feld samt Beschriftung genau dann ein, wenn
        /// der Wert rechenwirksam ist: bivalenter Betrieb UND eine Betriebsart, die
        /// <c>Tab_Energieanlagen.Abschaltpunkt</c> auswertet - Teilparallelbetrieb
        /// (seit jeher) und Alternativbetrieb (seit K-3, siehe
        /// SimulationWaermepumpe.AlternativAus: unterhalb dieser Aussentemperatur ist
        /// die WP aus). Im Parallelbetrieb bleibt der Wert wirkungslos und das Feld
        /// verborgen.
        /// </summary>
        private void BivalenztemperaturSichtbarkeitSetzen()
        {
            bool sichtbar = checkBox_Bivalent.Checked
                && (comboBox_Betriebsart.Text == DbWerte.WP_BETRIEBSART_TEILPARALLEL
                    || comboBox_Betriebsart.Text == DbWerte.WP_BETRIEBSART_ALTERNATIV);
            textBox_Abschalttemp.Visible = sichtbar;
            label_AbschalttemperaturEinheit.Visible = sichtbar;
            label_Abschalttemperatur.Visible = sichtbar;
        }
        // Die folgenden TextChanged-Handler färben nur noch (Begründung siehe
        // Program.ZahlFaerben); gemeldet wird erst in btn_Beenden_Click. Die alte
        // Fassung meldete jede Zwischeneingabe modal und nahm sie mit tb.Undo()
        // zurück - das konnte zwischen Fehleingabe und Leerstand pendeln.
        // Ganzzahl/Zahl richtet sich nach dem Speicherweg des Feldes.
        private void textBox_Nutzungszeit_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender); // wird als Int32 gespeichert
        }
        private void textBox_Volumen_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }
        private void textBox_von_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_bis_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }
        private void textBox_Anteil_TextChanged(object sender, EventArgs e)
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
    }
}
