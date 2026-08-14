using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Config : Form
    {
        public KonfigurationModel Konfiguration = new KonfigurationModel();
        public int m_ID_Projekt;
        private ComboBox comboBox;
        private int index = -1;
        private List<string> listErzeuger = new List<string>();
        private List<string> listPufferSp = new List<string>();

        // Pufferspeicher-Dropdowns der Rubrik "Pufferspeicher" (Felder 5 und 6),
        // werden in InitPufferspeicherRubrik programmatisch angelegt
        private ComboBox comboBox_Puffer1;
        private ComboBox comboBox_Puffer2;
        private CheckBox checkBox_Puffer1;
        private CheckBox checkBox_Puffer2;
        private bool _pufferUiUpdate = false; // verhindert Event-Rückkopplung

        // Vollständiger Datenbestand der Pufferspeicher-Zuordnungen
        // (ErzeugerAnzeige, Pufferspeicher, Vorlauf, Rücklauf). listView1 zeigt
        // davon nur die per Checkbox ausgewählten Pufferspeicher an - gespeichert
        // wird immer der komplette Bestand.
        private List<string[]> _zuordnungen = new List<string[]>();

        // Live-Übersicht der ausgewählten Wärmeerzeuger (rechts oben),
        // wird in InitErzeugerUebersicht programmatisch angelegt
        private GroupBox groupBox_Uebersicht;
        private ListView listView_Uebersicht;

        // Inline-Editor für die Wärmequelle in der Übersicht
        private ComboBox _wqCombo;
        private AnlagenInfo _wqInfo;
        private bool _wqUpdating = false;

        // Außentemperatur der Klimaregion (8760 Stundenwerte) für die Vorschau des
        // Erdreichdialogs. Wird beim ersten Öffnen einmal geladen und gecacht
        // (Konzept 4.5) - nicht bei jeder Parameteränderung.
        private float[] _aussentempCache = null;
        private bool _aussentempGeladen = false;

        /// <summary>Eine im Projekt angelegte Anlage (Zeile der Übersicht).</summary>
        private class AnlagenInfo
        {
            public int ID;              // Tab_Energieanlagen.ID
            public string Bezeichner = "";
            public int Prioritaet;      // Einsatzreihenfolge (0 = nicht gesetzt)
            public string WpTyp = "";   // Luft-Wasser / Sole-Wasser / Wasser-Wasser
            public string WQ_Typ = "";  // Wärmequelle (WaermequelleClass.TYP_*)
            public double WQ_Temp;
            public string WS_Typ = "";  // Wärmesenke (WaermequelleClass.SENKE_*)
            public string BM_Typ = "";  // Betriebsmodus (WaermequelleClass.MODUS_*)
        }

        // Mouseover-Hinweise in der Übersicht
        private ToolTip _uebersichtTip = new ToolTip();
        private ListViewItem _tipItem = null;
        private int _tipSpalte = -1;

        // Mouseover-Hinweise in der Pufferspeicher-Zuordnung
        private ToolTip _zuordnungTip = new ToolTip();
        private ListViewItem _tipItemZuordnung = null;
        private int _tipSpalteZuordnung = -1;
        private Timer statusTimer = new Timer();

        public class LanguageItem
        {
            public string DisplayName { get; set; } // Das, was der User sieht (übersetzt)
            public string DbValue { get; set; }    // Das, was in die DB kommt (z.B. "STATUS_OPEN")
        }

        private readonly List<LanguageItem> _waermeerzeugerItems;

        public Form_Simulation_Config()
        {
            InitializeComponent();

            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.View = View.Details;
            listView1.Columns.Add("Wärmeerzeuger", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Pufferspeicher", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView1.Columns.Add("", -2, HorizontalAlignment.Left);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Handle double-click for editing
            listView1.MouseDoubleClick += ListView_MouseDoubleClick;

            // Mouseover-Hinweise für die Zuordnungstabelle
            _zuordnungTip.AutoPopDelay = 15000;
            _zuordnungTip.InitialDelay = 400;
            _zuordnungTip.ReshowDelay = 100;
            listView1.MouseMove += listView1_MouseMove;
            listView1.MouseLeave += (s, e) => { _tipItemZuordnung = null; _tipSpalteZuordnung = -1; _zuordnungTip.Hide(listView1); };

            // Initialize ComboBox (hidden by default) für die Bearbeitung der "Pufferspeicher"-Spalte  
            comboBox = new ComboBox
            {
                Visible = false,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _waermeerzeugerItems = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
            };

            var items_PV = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_PHOTOVOLTAIK, DbValue = "Photovoltaik" },
            };

            var items_SP = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_STROMSPEICHER, DbValue = "Stromspeicher" },
            };

            // Das Array mit deinen 4 ComboBoxen (Namen anpassen)
            ComboBox[] myComboBoxes = { comboBox1, comboBox2, comboBox3, comboBox4 };

            // Über das Array iterieren
            foreach (var cb in myComboBoxes)
            {
                // WICHTIG: Erst Member setzen, dann DataSource
                cb.DisplayMember = "DisplayName";
                cb.ValueMember = "DbValue";

                // Eine Kopie der Liste oder ToList() nutzen, falls die Boxen 
                // unabhängig voneinander selektieren sollen
                cb.DataSource = _waermeerzeugerItems.ToList();

                // Auswahl auf leer setzen
                cb.SelectedIndex = -1;
            }

            // Erst JETZT das Event abonnieren
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            comboBox4.SelectedIndexChanged += comboBox4_SelectedIndexChanged;

            comboBox5.DisplayMember = "DisplayName";
            comboBox5.ValueMember = "DbValue";
            comboBox5.DataSource = items_PV.ToList();
            comboBox5.SelectedIndex = -1;
            comboBox6.DisplayMember = "DisplayName";
            comboBox6.ValueMember = "DbValue";
            comboBox6.DataSource = items_SP.ToList();
            comboBox6.SelectedIndex = -1;

            comboBox5.SelectedIndexChanged += comboBox5_SelectedIndexChanged;
            comboBox6.SelectedIndexChanged += comboBox6_SelectedIndexChanged;

            // Aufruf im Konstruktor:
            SetGroupBoxFontBold(groupBox_Tools);

            // Timer konfigurieren
            statusTimer.Interval = 3000; // 3 Sekunden Sichtbarkeit
            statusTimer.Tick += (s, e) => {
                lblStatus.Visible = false;
                statusTimer.Stop();
            };

            // Dialog-Umbau: Rubrik "Pufferspeicher" statt Einblenden-Checkbox
            InitPufferspeicherRubrik();

            // Live-Übersicht der ausgewählten Erzeuger rechts oben
            InitErzeugerUebersicht();

            // Bereich für den KI-Hilfe-Assistenten melden (nur Bedien-Kontext,
            // keine Projekt- oder Kundendaten)
            this.Activated += (s, e) =>
                HilfeKontext.SetzeBereich("Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)");
        }

        /// <summary>
        /// Legt rechts oben (über der Pufferspeicher-Zuordnung) eine Übersicht an,
        /// die alle ausgewählten Wärmeerzeuger in Prioritätsreihenfolge mit ihrer
        /// Pufferspeicher-Zuordnung zeigt. Sie aktualisiert sich automatisch bei
        /// jeder Änderung der Auswahl und der Zuordnungstabelle.
        /// </summary>
        private void InitErzeugerUebersicht()
        {
            groupBox_Uebersicht = new GroupBox();
            groupBox_Uebersicht.Name = "groupBox_Uebersicht";
            groupBox_Uebersicht.Text = "Übersicht ausgewählte Erzeuger";
            groupBox_Uebersicht.Location = new Point(groupBox_PufferSp.Left, 109);
            groupBox_Uebersicht.Size = new Size(groupBox_PufferSp.Width,
                groupBox_PufferSp.Top - 109 - 10);
            this.Controls.Add(groupBox_Uebersicht);
            groupBox_Uebersicht.BringToFront();

            listView_Uebersicht = new ListView();
            listView_Uebersicht.Name = "listView_Uebersicht";
            listView_Uebersicht.View = View.Details;
            listView_Uebersicht.FullRowSelect = true;
            listView_Uebersicht.GridLines = true;
            listView_Uebersicht.MultiSelect = false;
            listView_Uebersicht.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView_Uebersicht.Font = listView1.Font;
            listView_Uebersicht.Location = new Point(7, 20);
            listView_Uebersicht.Size = new Size(groupBox_Uebersicht.Width - 14,
                groupBox_Uebersicht.Height - 27);
            listView_Uebersicht.Anchor = AnchorStyles.Top | AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Bottom;
            listView_Uebersicht.Columns.Add("Prio", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Wärmeerzeuger", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Anlage(n) im Projekt", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("WP-Prio (*)", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Wärmequelle (*)", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Wärmesenke (*)", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Betriebsmodus (*)", -2, HorizontalAlignment.Left);
            listView_Uebersicht.Columns.Add("Pufferspeicher", -2, HorizontalAlignment.Left);
            listView_Uebersicht.MouseDoubleClick += listView_Uebersicht_MouseDoubleClick;

            // Mouseover-Hinweise zu den bearbeitbaren Spalten
            _uebersichtTip.AutoPopDelay = 15000;
            _uebersichtTip.InitialDelay = 400;
            _uebersichtTip.ReshowDelay = 100;
            listView_Uebersicht.MouseMove += listView_Uebersicht_MouseMove;
            listView_Uebersicht.MouseLeave += (s, e) => { _tipItem = null; _tipSpalte = -1; _uebersichtTip.Hide(listView_Uebersicht); };

            groupBox_Uebersicht.Controls.Add(listView_Uebersicht);

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Baut die Erzeuger-Übersicht neu auf: ausgewählte Wärmeerzeuger in
        /// Prioritätsreihenfolge, je Erzeuger die zugeordneten Pufferspeicher
        /// aus der Zuordnungstabelle ("-" = keine Zuordnung).
        /// </summary>
        private void AktualisiereErzeugerUebersicht()
        {
            if (listView_Uebersicht == null) return;

            listView_Uebersicht.Items.Clear();

            int prio = 1;
            foreach (string dbWert in listErzeuger)
            {
                if (dbWert == "Gesamtsystem") continue; // eigener Eintrag weiter unten

                string anzeige = _waermeerzeugerItems.FirstOrDefault(x => x.DbValue == dbWert)?.DisplayName ?? dbWert;
                string puffer = ZugeordnetePufferSp(anzeige);
                List<AnlagenInfo> anlagen = AnlagenImProjekt(dbWert);
                bool istWP = dbWert == "Wärmepumpe";

                if (anlagen.Count == 0)
                {
                    listView_Uebersicht.Items.Add(new ListViewItem(new[]
                        { prio.ToString(), anzeige, "-", "", "", "", "", puffer }));
                }
                else
                {
                    // Jede im Projekt angelegte Anlage bekommt eine eigene Zeile
                    // (z. B. beide Wärmepumpen); Prio/Erzeuger/Puffer nur in der
                    // ersten Zeile, damit die Gruppierung erkennbar bleibt.
                    for (int a = 0; a < anlagen.Count; a++)
                    {
                        ListViewItem zeile = new ListViewItem(new[]
                        {
                            a == 0 ? prio.ToString() : "",
                            a == 0 ? anzeige : "",
                            anlagen[a].Bezeichner,
                            istWP ? (anlagen[a].Prioritaet > 0 ? anlagen[a].Prioritaet.ToString() : "-") : "",
                            istWP ? WaermequelleAnzeige(anlagen[a]) : "",
                            istWP ? WaermesenkeAnzeige(anlagen[a]) : "",
                            istWP ? BetriebsmodusAnzeige(anlagen[a]) : "",
                            a == 0 ? puffer : ""
                        });
                        if (istWP) zeile.Tag = anlagen[a]; // für Bearbeitung per Doppelklick
                        listView_Uebersicht.Items.Add(zeile);
                    }
                }
                prio++;
            }

            // Zuordnungen zum Gesamtsystem ebenfalls anzeigen, falls vorhanden
            string gesamt = MyResource.Resource.KONFIG_GESAMTSYSTEM;
            string gesamtSp = ZugeordnetePufferSp(gesamt);
            if (gesamtSp != "-")
                listView_Uebersicht.Items.Add(new ListViewItem(new[] { "", gesamt, "", "", "", "", "", gesamtSp }));

            listView_Uebersicht.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Uebersicht.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        /// <summary>
        /// Liefert alle im Projekt angelegten Anlagen des Erzeuger-Typs aus
        /// Tab_Energieanlagen (inkl. Priorität, WP-Typ und Wärmequelle),
        /// sortiert nach Einsatz-Priorität.
        /// </summary>
        private List<AnlagenInfo> AnlagenImProjekt(string dbWert)
        {
            List<AnlagenInfo> anlagen = new List<AnlagenInfo>();

            int typ = 0;
            switch (dbWert)
            {
                case "Wärmepumpe": typ = WizardItemClass.WP_TYP; break;
                case "Heizkessel": typ = WizardItemClass.KESSEL_TYP; break;
                case "BHKW": typ = WizardItemClass.BHKW_TYP; break;
                case "Solarthermie": typ = WizardItemClass.SOLAR_TYP; break;
            }
            if (typ == 0 || m_ID_Projekt == 0) return anlagen;

            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT a.ID, a.Bezeichner, a.Prioritaet, a.WQ_Typ, a.WQ_Temp, a.WS_Typ, a.BM_Typ, w.Typ AS WPTyp " +
                "FROM Tab_Energieanlagen AS a LEFT JOIN Tab_WP AS w ON a.ID_WP = w.ID " +
                "WHERE a.ID_Projekt=" + m_ID_Projekt + " AND a.ID_Type=" + typ +
                " ORDER BY a.Prioritaet, a.ID");
            if (dt == null) return anlagen;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                AnlagenInfo info = new AnlagenInfo();
                if (r["ID"] != DBNull.Value) info.ID = Convert.ToInt32(r["ID"]);
                if (r["Bezeichner"] != DBNull.Value) info.Bezeichner = r["Bezeichner"].ToString();
                if (r["Prioritaet"] != DBNull.Value) info.Prioritaet = Convert.ToInt32(r["Prioritaet"]);
                if (r["WPTyp"] != DBNull.Value) info.WpTyp = r["WPTyp"].ToString();
                if (r["WQ_Typ"] != DBNull.Value) info.WQ_Typ = r["WQ_Typ"].ToString();
                if (r["WQ_Temp"] != DBNull.Value) info.WQ_Temp = Convert.ToDouble(r["WQ_Temp"]);
                if (r["WS_Typ"] != DBNull.Value) info.WS_Typ = r["WS_Typ"].ToString();
                if (r["BM_Typ"] != DBNull.Value) info.BM_Typ = r["BM_Typ"].ToString();
                if (!string.IsNullOrEmpty(info.Bezeichner)) anlagen.Add(info);
            }

            return anlagen;
        }

        /// <summary>Kompakte Anzeige der Wärmequelle einer Wärmepumpe.</summary>
        private string WaermequelleAnzeige(AnlagenInfo a)
        {
            // Luft-Wasser-WP: Quelle ist immer die Außenluft (Klimadaten)
            if (string.IsNullOrEmpty(a.WpTyp) || a.WpTyp == "Luft-Wasser") return "Außenluft";

            switch (a.WQ_Typ)
            {
                case WaermequelleClass.TYP_KONSTANT: return "Konstant (" + a.WQ_Temp.ToString("0.#") + " °C)";
                case WaermequelleClass.TYP_PUFFER:
                    {
                        string name = WaermequelleClass.WertLesen(a.ID, "WQ_Puffer") as string;
                        return string.IsNullOrEmpty(name) ? "Pufferspeicher" : "Puffer: " + name;
                    }
                case WaermequelleClass.TYP_PROFIL: return "Quellprofil";
                case WaermequelleClass.TYP_CSV: return "CSV-Profil";
                case WaermequelleClass.TYP_ERDREICH: return ErdreichAnzeige(a.ID);
                default: return "Außenluft";
            }
        }

        /// <summary>
        /// Kompakte Anzeige der Wärmequelle Erdreich, z. B.
        /// "Erdreich Kollektor 1,5 m" oder "Erdsonde 2×90 m".
        /// </summary>
        private string ErdreichAnzeige(int idAnlage)
        {
            string quellsystem = WaermequelleClass.WertLesen(idAnlage, "WQ_Quellsystem") as string;
            object oTiefe = WaermequelleClass.WertLesen(idAnlage, "WQ_Tiefe");
            double tiefe = oTiefe != null ? Convert.ToDouble(oTiefe) : 0;

            if (string.Equals(quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                              StringComparison.OrdinalIgnoreCase))
            {
                object oAnzahl = WaermequelleClass.WertLesen(idAnlage, "WQ_Anzahl");
                int anzahl = oAnzahl != null ? Convert.ToInt32(oAnzahl) : 0;
                if (anzahl < 1) anzahl = 1;
                return "Erdsonde " + anzahl + "×" + tiefe.ToString("0.#") + " m";
            }

            if (tiefe <= 0) tiefe = ErdreichTemperatur.TIEFE_DEFAULT;
            return "Erdreich Kollektor " + tiefe.ToString("0.#") + " m";
        }

        /// <summary>
        /// Liefert die Außentemperatur der Projekt-Klimaregion (8760 Stundenwerte).
        /// Der Vektor wird einmal je Formularsitzung geladen und gecacht; er ist
        /// derselbe, den die Simulation über SimulationWaermebedarf.Stundentemperatur
        /// verwendet (Tab_Solar.Temperatur der Klimaregion). Liefert null, wenn dem
        /// Projekt keine Klimaregion zugeordnet ist oder keine 8760 Werte vorliegen.
        /// </summary>
        private float[] AussentemperaturLaden()
        {
            if (_aussentempGeladen) return _aussentempCache;
            _aussentempGeladen = true;

            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return null;
                int idRegion = Convert.ToInt32(oRegion);
                if (idRegion <= 0) return null;

                System.Data.DataTable dt = DataRepository.GetDataTable(
                    "SELECT Temperatur FROM Tab_Solar WHERE ID_Klimaregion = " + idRegion + " ORDER BY ID");
                if (dt == null || dt.Rows.Count < 8760) return null;

                float[] temp = new float[8760];
                for (int i = 0; i < 8760; i++)
                {
                    object v = dt.Rows[i]["Temperatur"];
                    temp[i] = (v == DBNull.Value) ? 0f : Convert.ToSingle(v);
                }
                _aussentempCache = temp;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Außentemperatur konnte nicht geladen werden: " + ex.Message);
            }

            return _aussentempCache;
        }

        /// <summary>DIN-4710-Klimazone der Projekt-Klimaregion; 0 = nicht zugeordnet.</summary>
        private int KlimazoneDesProjekts()
        {
            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return 0;
                return KlimaregionCtrl.GetKlimazone(Convert.ToInt32(oRegion));
            }
            catch { return 0; }
        }

        /// <summary>Speichert die DIN-4710-Klimazone an der Projekt-Klimaregion.</summary>
        private void KlimazoneSpeichern(int zone)
        {
            try
            {
                object oRegion = DataRepository.ExecuteScalar(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = " + m_ID_Projekt);
                if (oRegion == null || oRegion == DBNull.Value) return;
                KlimaregionCtrl.SetKlimazone(Convert.ToInt32(oRegion), zone);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimazone konnte nicht gespeichert werden: " + ex.Message);
            }
        }

        /// <summary>Kompakte Anzeige der Wärmesenke einer Wärmepumpe.</summary>
        private string WaermesenkeAnzeige(AnlagenInfo a)
        {
            switch (a.WS_Typ)
            {
                case WaermequelleClass.SENKE_WARMWASSER: return "nur Warmwasser";
                case WaermequelleClass.SENKE_HEIZUNG: return "nur Heizwärme";
                default: return "Warmwasser + Heizwärme";
            }
        }

        /// <summary>
        /// Mouseover-Hinweise in der Tabelle "Pufferspeicher Zuordnung":
        /// erklärt die per Doppelklick bearbeitbaren Spalten.
        /// </summary>
        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            if (hit.Item == null)
            {
                if (_tipItemZuordnung != null)
                {
                    _tipItemZuordnung = null; _tipSpalteZuordnung = -1;
                    _zuordnungTip.Hide(listView1);
                }
                return;
            }

            int spalte = hit.SubItem != null ? hit.Item.SubItems.IndexOf(hit.SubItem) : -1;
            if (_tipItemZuordnung == hit.Item && _tipSpalteZuordnung == spalte) return;
            _tipItemZuordnung = hit.Item;
            _tipSpalteZuordnung = spalte;

            string text;
            switch (spalte)
            {
                case 0:
                    text = "Wärmeerzeuger, dem dieser Pufferspeicher zugeordnet ist.\n" +
                           "Zuordnungen werden über 'Hinzufügen...' angelegt und über\n" +
                           "'Löschen' entfernt.";
                    break;

                case 1:
                    text = "Pufferspeicher (Doppelklick zum Ändern)\n" +
                           "Auswahl aus den Stammdaten. Volumen und Bereitschaftsverluste\n" +
                           "stammen aus dem Speicher-Datensatz und bestimmen zusammen mit\n" +
                           "Vor- und Rücklauf die nutzbare Kapazität.";
                    break;

                case 2:
                    text = "Vorlauftemperatur [°C] (Doppelklick zum Ändern)\n" +
                           "Obere Temperatur des Speichers. Die nutzbare Kapazität ergibt\n" +
                           "sich aus: Volumen × 1,16 Wh/(l·K) × (Vorlauf − Rücklauf).";
                    break;

                case 3:
                    text = "Rücklauftemperatur [°C] (Doppelklick zum Ändern)\n" +
                           "Untere Temperatur des Speichers. Je größer die Spreizung zum\n" +
                           "Vorlauf, desto mehr Energie kann der Speicher aufnehmen.";
                    break;

                case 4:
                    text = "Doppelklick öffnet die Pufferspeicher-Stammdaten (nur Ansicht).";
                    break;

                default:
                    text = "Pufferspeicher-Zuordnung: Doppelklick auf Pufferspeicher,\n" +
                           "Vorlauf oder Rücklauf zum Bearbeiten.";
                    break;
            }

            _zuordnungTip.Show(text, listView1, e.X + 16, e.Y + 18, 15000);
        }

        /// <summary>Kompakte Anzeige des Betriebsmodus einer Wärmepumpe.</summary>
        private string BetriebsmodusAnzeige(AnlagenInfo a)
        {
            switch (a.BM_Typ)
            {
                case WaermequelleClass.MODUS_LEISTUNG: return "leistungsoptimiert";
                case WaermequelleClass.MODUS_PV: return "PV-optimiert";
                default: return "laufzeitoptimiert";
            }
        }

        /// <summary>
        /// Auswahl des Betriebsmodus (Leistungssteuerung) einer Wärmepumpe.
        /// </summary>
        private void BetriebsmodusBearbeiten(AnlagenInfo info)
        {
            Form frm = new Form();
            frm.Text = "Betriebsmodus - " + info.Bezeichner;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(520, 300);

            Label kopf = new Label
            {
                Text = "Leistungssteuerung der Wärmepumpe:",
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 14)
            };

            RadioButton rbLaufzeit = new RadioButton
            {
                Text = "Laufzeitoptimiert - maximale Leistung",
                AutoSize = true,
                Location = new Point(24, 48)
            };
            Label lLaufzeit = new Label
            {
                Text = "Die Wärmepumpe fährt volle Leistung; die über den Bedarf hinaus\n" +
                       "erzeugte Wärme lädt den Pufferspeicher. Lange Laufzeiten, wenig Takten.",
                AutoSize = false,
                Size = new Size(460, 34),
                Location = new Point(46, 70)
            };

            RadioButton rbLeistung = new RadioButton
            {
                Text = "Leistungsoptimiert - nur den Bedarf decken",
                AutoSize = true,
                Location = new Point(24, 112)
            };
            Label lLeistung = new Label
            {
                Text = "Die Wärmepumpe moduliert exakt auf den Wärmebedarf und erzeugt\n" +
                       "keinen Überschuss. Der Speicher wird nicht gezielt beladen.",
                AutoSize = false,
                Size = new Size(460, 34),
                Location = new Point(46, 134)
            };

            RadioButton rbPV = new RadioButton
            {
                Text = "PV-optimiert - Überschuss nur mit PV-Strom",
                AutoSize = true,
                Location = new Point(24, 176)
            };
            Label lPV = new Label
            {
                Text = "Bei verfügbarem PV-Strom fährt die Wärmepumpe erhöhte Leistung\n" +
                       "(begrenzt auf den PV-Überschuss) und lädt den Speicher; sonst\n" +
                       "arbeitet sie leistungsoptimiert.",
                AutoSize = false,
                Size = new Size(460, 48),
                Location = new Point(46, 198)
            };

            switch (info.BM_Typ)
            {
                case WaermequelleClass.MODUS_LEISTUNG: rbLeistung.Checked = true; break;
                case WaermequelleClass.MODUS_PV: rbPV.Checked = true; break;
                default: rbLaufzeit.Checked = true; break;
            }

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(332, 258), Width = 85 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(423, 258), Width = 85 };

            frm.Controls.Add(kopf);
            frm.Controls.Add(rbLaufzeit); frm.Controls.Add(lLaufzeit);
            frm.Controls.Add(rbLeistung); frm.Controls.Add(lLeistung);
            frm.Controls.Add(rbPV); frm.Controls.Add(lPV);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            if (frm.ShowDialog(this) != DialogResult.OK) return;

            string modus = WaermequelleClass.MODUS_LAUFZEIT;
            if (rbLeistung.Checked) modus = WaermequelleClass.MODUS_LEISTUNG;
            else if (rbPV.Checked) modus = WaermequelleClass.MODUS_PV;

            WaermequelleClass.WertSchreiben(info.ID, "BM_Typ", modus);

            if (modus == WaermequelleClass.MODUS_PV && (comboBox5.SelectedIndex < 0 || !checkBox5.Checked))
            {
                MessageBox.Show("Hinweis: Für den PV-optimierten Betrieb muss im Bereich " +
                    "'Stromerzeuger' die Photovoltaik ausgewählt sein.\n" +
                    "Ohne PV-Anlage verhält sich die Wärmepumpe leistungsoptimiert.",
                    "Betriebsmodus PV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Mouseover-Hinweise: erklärt die per Doppelklick bearbeitbaren Spalten
        /// der Übersicht (WP-Priorität, Wärmequelle, Wärmesenke, Betriebsmodus).
        /// </summary>
        private void listView_Uebersicht_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView_Uebersicht.HitTest(e.Location);
            if (hit.Item == null || !(hit.Item.Tag is AnlagenInfo info))
            {
                if (_tipItem != null) { _tipItem = null; _tipSpalte = -1; _uebersichtTip.Hide(listView_Uebersicht); }
                return;
            }

            int spalte = hit.SubItem != null ? hit.Item.SubItems.IndexOf(hit.SubItem) : -1;

            // Nur bei Wechsel neu anzeigen (sonst flackert der Hinweis)
            if (_tipItem == hit.Item && _tipSpalte == spalte) return;
            _tipItem = hit.Item;
            _tipSpalte = spalte;

            string text;
            switch (spalte)
            {
                case 3:
                    text = "WP-Priorität (Doppelklick zum Ändern)\n" +
                           "Einsatz-Reihenfolge der Wärmepumpen: 1 = wird zuerst eingesetzt,\n" +
                           "die nächste deckt jeweils den verbleibenden Bedarf der Stunde.";
                    break;

                case 4:
                    text = "Wärmequelle (Doppelklick zum Ändern)\n" +
                           "Luft-Wasser: immer Außenluft aus den Klimadaten.\n" +
                           "Sole-/Wasser-Wasser: Konstante Temperatur, Pufferspeicher,\n" +
                           "Quellprofil (Monats- und Wochenwerte) oder CSV-Datei.";
                    break;

                case 5:
                    text = "Wärmesenke (Doppelklick zum Ändern)\n" +
                           "Legt fest, welchen Bedarf diese Wärmepumpe deckt:\n" +
                           "• nur Warmwasser - deckt ausschließlich den Warmwasserbedarf\n" +
                           "• nur Heizwärme - deckt ausschließlich den Heizwärmebedarf\n" +
                           "• beides - deckt beide Anteile (Warmwasser zuerst)";
                    break;

                case 6:
                    text = "Betriebsmodus (Doppelklick zum Ändern)\n" +
                           "• laufzeitoptimiert - volle Leistung, Überschuss lädt den Speicher\n" +
                           "• leistungsoptimiert - moduliert exakt auf den Wärmebedarf\n" +
                           "• PV-optimiert - erhöhte Leistung nur bei verfügbarem PV-Strom,\n" +
                           "  sonst leistungsoptimiert";
                    break;

                case 7:
                    text = "Pufferspeicher (Doppelklick öffnet die Speicherregelung)\n" +
                           "Ein- und Abschaltschwelle in % der nutzbaren Kapazität:\n" +
                           "Unterhalb der Einschaltschwelle läuft die Wärmepumpe an und\n" +
                           "lädt bis zur Abschaltschwelle - dazwischen bleibt sie aus und\n" +
                           "der Bedarf wird aus dem Speicher gedeckt.";
                    break;

                default:
                    text = "Anlage: " + info.Bezeichner + "\n" +
                           "Doppelklick auf die Spalten WP-Prio, Wärmequelle oder\n" +
                           "Wärmesenke zum Bearbeiten.";
                    break;
            }

            _uebersichtTip.Show(text, listView_Uebersicht, e.X + 16, e.Y + 18, 15000);
        }

        /// <summary>
        /// Doppelklick in der Übersicht: WP-Priorität (Spalte 3), Wärmequelle
        /// (Spalte 4) und Wärmesenke (Spalte 5) der Wärmepumpen-Zeilen bearbeiten.
        /// </summary>
        private void listView_Uebersicht_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView_Uebersicht.HitTest(e.Location);
            if (hit.Item == null) return;
            if (!(hit.Item.Tag is AnlagenInfo info)) return; // nur Wärmepumpen-Zeilen

            // Angeklickte Spalte ermitteln; ohne eindeutigen Treffer wird die
            // Wärmequellen-Bearbeitung geöffnet (Doppelklick irgendwo in der Zeile).
            int spalte = 4;
            if (hit.SubItem != null)
            {
                int idx = hit.Item.SubItems.IndexOf(hit.SubItem);
                if (idx >= 0) spalte = idx;
            }

            if (spalte == 3) // WP-Priorität
            {
                string eingabe = EingabeDialog("Wärmepumpen-Priorität",
                    "Einsatz-Reihenfolge der Wärmepumpe\n'" + info.Bezeichner + "'\n(1 = wird zuerst eingesetzt):",
                    info.Prioritaet > 0 ? info.Prioritaet.ToString() : "1");
                int prioNeu;
                if (eingabe != null && Int32.TryParse(eingabe, out prioNeu) && prioNeu > 0)
                {
                    WaermequelleClass.WertSchreiben(info.ID, "Prioritaet", prioNeu);
                    AktualisiereErzeugerUebersicht();
                }
            }
            else if (spalte == 5) // Wärmesenke
            {
                WaermesenkeBearbeiten(info);
            }
            else if (spalte == 6) // Betriebsmodus
            {
                BetriebsmodusBearbeiten(info);
            }
            else if (spalte == 7) // Pufferspeicher -> Speicherregelung (Schwellen)
            {
                SpeicherregelungBearbeiten();
            }
            else // Wärmequelle (alle übrigen Spalten der WP-Zeile)
            {
                if (string.IsNullOrEmpty(info.WpTyp) || info.WpTyp == "Luft-Wasser")
                {
                    MessageBox.Show("Für Luft-Wasser-Wärmepumpen ist die Wärmequelle immer die Außenluft\n" +
                        "(Außentemperatur der gewählten Klimaregion).\n\n" +
                        "WP-Typ: " + (string.IsNullOrEmpty(info.WpTyp) ? "(nicht gepflegt)" : info.WpTyp),
                        "Wärmequelle", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Rectangle zelle = hit.SubItem != null ? hit.SubItem.Bounds : hit.Item.Bounds;
                WaermequelleAuswahlAnzeigen(info, zelle);
            }
        }

        /// <summary>
        /// Einstellung der Speicherregelung (Hysterese) für den Pufferspeicher
        /// der Wärmepumpe: Ein- und Abschaltschwelle in Prozent der nutzbaren
        /// Kapazität. Gespeichert je Zuordnung in Z_ProjektPufferSp.
        /// </summary>
        private void SpeicherregelungBearbeiten()
        {
            // Zuordnung der Wärmepumpe suchen (höchste Priorität)
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt=" + m_ID_Projekt + " AND Erzeuger='Wärmepumpe'");
            if (ctrlpsp.rows == 0)
            {
                MessageBox.Show("Der Wärmepumpe ist kein Pufferspeicher zugeordnet.\n" +
                    "Die Zuordnung erfolgt in der Tabelle 'Pufferspeicher Zuordnung'.",
                    "Speicherregelung", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idZuordnung = ctrlpsp.items[0].ID;
            string speicherName = ctrlpsp.items[0].PufferSp;

            // Gespeicherte Schwellen lesen (Vorgabe 10 % / 95 %)
            double vorgabeEin = 10, vorgabeAus = 95;
            object sEin = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Ein", idZuordnung);
            object sAus = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Aus", idZuordnung);
            if (sEin != null && Convert.ToDouble(sEin) > 0) vorgabeEin = Convert.ToDouble(sEin);
            if (sAus != null && Convert.ToDouble(sAus) > 0) vorgabeAus = Convert.ToDouble(sAus);

            Form frm = new Form();
            frm.Text = "Speicherregelung - " + speicherName;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(430, 250);

            Label kopf = new Label
            {
                Text = "Ein- und Abschaltschwelle des Pufferspeichers",
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 14)
            };

            Label l1 = new Label { Text = "Einschaltschwelle [% der Kapazität]:", AutoSize = true, Location = new Point(24, 52) };
            TextBox tbEin = new TextBox { Location = new Point(280, 49), Width = 70, Text = vorgabeEin.ToString("0.#") };

            Label l2 = new Label { Text = "Abschaltschwelle [% der Kapazität]:", AutoSize = true, Location = new Point(24, 88) };
            TextBox tbAus = new TextBox { Location = new Point(280, 85), Width = 70, Text = vorgabeAus.ToString("0.#") };

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(14, 124),
                Size = new Size(400, 80),
                Text = "Unterschreitet der Speicherfüllstand die Einschaltschwelle, läuft die " +
                       "Wärmepumpe an und lädt bis zur Abschaltschwelle durch. Dazwischen bleibt " +
                       "sie aus und der Bedarf wird aus dem Speicher gedeckt.\n\n" +
                       "Die Abschaltschwelle sollte unter 100 % liegen, da die Bereitschaftsverluste " +
                       "den Füllstand laufend absenken."
            };

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(242, 210), Width = 85 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(333, 210), Width = 85 };

            frm.Controls.Add(kopf);
            frm.Controls.Add(l1);
            frm.Controls.Add(tbEin);
            frm.Controls.Add(l2);
            frm.Controls.Add(tbAus);
            frm.Controls.Add(hinweis);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            while (frm.ShowDialog(this) == DialogResult.OK)
            {
                float ein, aus;
                if (!WaermequelleClass.ZahlParsen(tbEin.Text, out ein) ||
                    !WaermequelleClass.ZahlParsen(tbAus.Text, out aus))
                {
                    MessageBox.Show("Bitte gültige Zahlenwerte eintragen!", "Speicherregelung",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                if (ein < 0 || ein > 100 || aus <= 0 || aus > 100 || ein >= aus)
                {
                    MessageBox.Show("Die Werte müssen zwischen 0 und 100 % liegen und\n" +
                        "die Einschaltschwelle muss kleiner als die Abschaltschwelle sein!",
                        "Speicherregelung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                DataRepository.ExecuteSQL(
                    "UPDATE Z_ProjektPufferSp SET Schwelle_Ein=?, Schwelle_Aus=? WHERE ID=" + idZuordnung,
                    new System.Data.OleDb.OleDbParameter("@ein", (double)ein),
                    new System.Data.OleDb.OleDbParameter("@aus", (double)aus));

                ShowStatus("✔ Speicherregelung gespeichert (" + ein.ToString("0.#") + " % / " +
                           aus.ToString("0.#") + " %)", Color.ForestGreen);
                return;
            }
        }

        /// <summary>
        /// Auswahl der Wärmesenke: Warmwasser und/oder Heizwärme.
        /// Ist nur Warmwasser angehakt, deckt dieser Erzeuger ausschließlich den
        /// Warmwasserbedarf (analog nur Heizwärme).
        /// </summary>
        private void WaermesenkeBearbeiten(AnlagenInfo info)
        {
            Form frm = new Form();
            frm.Text = "Wärmesenke - " + info.Bezeichner;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(400, 210);

            Label kopf = new Label
            {
                Text = "Welchen Bedarf soll diese Wärmepumpe decken?",
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 14)
            };

            CheckBox cbWW = new CheckBox
            {
                Text = "Warmwasserbedarf",
                AutoSize = true,
                Location = new Point(24, 50)
            };
            CheckBox cbHeiz = new CheckBox
            {
                Text = "Wärmebedarf (Heizwärme)",
                AutoSize = true,
                Location = new Point(24, 80)
            };

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(14, 112),
                Size = new Size(370, 48),
                Text = "Ist nur ein Bedarf angehakt, deckt der Erzeuger ausschließlich diesen Anteil.\n" +
                       "Sind beide angehakt, wird zuerst der Warmwasserbedarf gedeckt."
            };

            // Vorbelegung aus dem gespeicherten Wert
            if (info.WS_Typ == WaermequelleClass.SENKE_WARMWASSER) { cbWW.Checked = true; }
            else if (info.WS_Typ == WaermequelleClass.SENKE_HEIZUNG) { cbHeiz.Checked = true; }
            else { cbWW.Checked = true; cbHeiz.Checked = true; }

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(212, 170), Width = 85 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(303, 170), Width = 85 };

            frm.Controls.Add(kopf);
            frm.Controls.Add(cbWW);
            frm.Controls.Add(cbHeiz);
            frm.Controls.Add(hinweis);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            if (frm.ShowDialog(this) != DialogResult.OK) return;

            if (!cbWW.Checked && !cbHeiz.Checked)
            {
                MessageBox.Show("Es muss mindestens ein Bedarf ausgewählt sein!", "Wärmesenke",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string senke = WaermequelleClass.SENKE_BEIDES;
            if (cbWW.Checked && !cbHeiz.Checked) senke = WaermequelleClass.SENKE_WARMWASSER;
            else if (!cbWW.Checked && cbHeiz.Checked) senke = WaermequelleClass.SENKE_HEIZUNG;

            WaermequelleClass.WertSchreiben(info.ID, "WS_Typ", senke);
            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Zeigt das Wärmequellen-Dropdown (Sole-/Wasser-Wasser-WP) direkt in der
        /// Übersicht an - analog zur Zellbearbeitung in der Zuordnungstabelle.
        /// </summary>
        private void WaermequelleAuswahlAnzeigen(AnlagenInfo info, Rectangle zellBounds)
        {
            if (_wqCombo == null)
            {
                _wqCombo = new ComboBox { Visible = false, DropDownStyle = ComboBoxStyle.DropDownList };
                _wqCombo.SelectedIndexChanged += WqCombo_SelectedIndexChanged;
                _wqCombo.LostFocus += (s, ev) => _wqCombo.Visible = false;
            }
            if (!this.Controls.Contains(_wqCombo)) this.Controls.Add(_wqCombo);

            _wqInfo = info;

            _wqUpdating = true;
            _wqCombo.Items.Clear();
            _wqCombo.Items.AddRange(WaermequelleClass.TypAnzeige);
            int aktuell = Array.IndexOf(WaermequelleClass.TypWerte,
                string.IsNullOrEmpty(info.WQ_Typ) ? WaermequelleClass.TYP_AUSSENLUFT : info.WQ_Typ);
            _wqCombo.SelectedIndex = aktuell >= 0 ? aktuell : 0;
            _wqUpdating = false;

            Point screenPoint = listView_Uebersicht.PointToScreen(zellBounds.Location);
            Point formPoint = this.PointToClient(screenPoint);
            _wqCombo.Bounds = new Rectangle(formPoint, new Size(Math.Max(zellBounds.Width, 190), zellBounds.Height));
            _wqCombo.Visible = true;
            _wqCombo.BringToFront();
            _wqCombo.Focus();
            _wqCombo.DroppedDown = true;
        }

        private void WqCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_wqUpdating || _wqInfo == null || _wqCombo.SelectedIndex < 0) return;

            string typNeu = WaermequelleClass.TypWerte[_wqCombo.SelectedIndex];
            AnlagenInfo info = _wqInfo;
            _wqCombo.Visible = false;

            switch (typNeu)
            {
                case WaermequelleClass.TYP_AUSSENLUFT:
                    WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                    break;

                case WaermequelleClass.TYP_KONSTANT:
                    {
                        string eingabe = EingabeDialog("Konstante Quelltemperatur",
                            "Quelltemperatur der Wärmepumpe\n'" + info.Bezeichner + "' [°C]:",
                            info.WQ_Temp != 0 ? info.WQ_Temp.ToString("0.#") : "10");
                        float temp;
                        if (eingabe == null || !WaermequelleClass.ZahlParsen(eingabe, out temp)) return;
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Temp", (double)temp);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_PUFFER:
                    {
                        // Auswahl des Pufferspeichers, der als Wärmequelle dient
                        Form_QuellePufferspeicher frmQuelle = new Form_QuellePufferspeicher();
                        frmQuelle.WPName = info.Bezeichner;
                        frmQuelle.Pufferspeicher = WaermequelleClass.WertLesen(info.ID, "WQ_Puffer") as string;

                        object oTemp = WaermequelleClass.WertLesen(info.ID, "WQ_Temp");
                        if (oTemp != null) frmQuelle.Quelltemperatur = Convert.ToDouble(oTemp);
                        object oSpreiz = WaermequelleClass.WertLesen(info.ID, "WQ_Spreizung");
                        if (oSpreiz != null && Convert.ToDouble(oSpreiz) > 0) frmQuelle.Spreizung = Convert.ToDouble(oSpreiz);
                        object oReg = WaermequelleClass.WertLesen(info.ID, "WQ_Regeneration");
                        if (oReg != null) frmQuelle.Regeneration = Convert.ToDouble(oReg);
                        object oUnb = WaermequelleClass.WertLesen(info.ID, "WQ_Unbegrenzt");
                        if (oUnb != null) frmQuelle.Unbegrenzt = Convert.ToBoolean(oUnb);

                        frmQuelle.SetControls();
                        if (frmQuelle.ShowDialog(this) != DialogResult.OK) return;

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Puffer", frmQuelle.Pufferspeicher);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Temp", frmQuelle.Quelltemperatur);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", frmQuelle.Spreizung);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Regeneration", frmQuelle.Regeneration);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Unbegrenzt", frmQuelle.Unbegrenzt);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_PROFIL:
                    {
                        // Quellprofil über Monats- und Wochenwerte
                        // (analog "Brauchwassertypen Stundenverteilung")
                        Form_Quellprofil frmProfil = new Form_Quellprofil();
                        frmProfil.WPName = info.Bezeichner;
                        frmProfil.Monatswerte = WaermequelleClass.WertLesen(info.ID, "WQ_Monatswerte") as string;
                        frmProfil.Wochenwerte = WaermequelleClass.WertLesen(info.ID, "WQ_Wochenwerte") as string;
                        frmProfil.SetControls();

                        if (frmProfil.ShowDialog(this) != DialogResult.OK) return;

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Monatswerte", frmProfil.Monatswerte);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Wochenwerte", frmProfil.Wochenwerte);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_CSV:
                    {
                        if (MessageBox.Show(WaermequelleClass.CSV_FORMAT_HINWEIS + "\n\nJetzt Datei auswählen?",
                            "Quelltemperatur aus CSV-Datei", MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Information) != DialogResult.OK) return;

                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Title = "Quelltemperatur-Profil auswählen";
                        dlg.Filter = "CSV Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*";
                        if (dlg.ShowDialog() != DialogResult.OK) return;

                        if (WaermequelleClass.ProfilAusCsv(dlg.FileName) == null)
                        {
                            MessageBox.Show("Die Datei konnte nicht gelesen werden oder enthält keine 8760 Stundenwerte!\n\n" +
                                WaermequelleClass.CSV_FORMAT_HINWEIS, "CSV-Datei ungültig",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_CSV", dlg.FileName);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }

                case WaermequelleClass.TYP_ERDREICH:
                    {
                        // Erdreich nach VDI 4640 (Konzept 4.5): Kollektor oder Sonde.
                        Form_QuelleErdreich frmErde = new Form_QuelleErdreich();
                        frmErde.WPName = info.Bezeichner;

                        string quellsystem = WaermequelleClass.WertLesen(info.ID, "WQ_Quellsystem") as string;
                        if (!string.IsNullOrEmpty(quellsystem)) frmErde.Quellsystem = quellsystem;

                        object oTiefe = WaermequelleClass.WertLesen(info.ID, "WQ_Tiefe");
                        if (oTiefe != null && Convert.ToDouble(oTiefe) > 0) frmErde.Tiefe = Convert.ToDouble(oTiefe);
                        object oFlaeche = WaermequelleClass.WertLesen(info.ID, "WQ_Flaeche");
                        if (oFlaeche != null) frmErde.Flaeche = Convert.ToDouble(oFlaeche);
                        object oAnzahl = WaermequelleClass.WertLesen(info.ID, "WQ_Anzahl");
                        if (oAnzahl != null && Convert.ToInt32(oAnzahl) > 0) frmErde.Anzahl = Convert.ToInt32(oAnzahl);
                        string bodentyp = WaermequelleClass.WertLesen(info.ID, "WQ_Bodentyp") as string;
                        if (!string.IsNullOrEmpty(bodentyp)) frmErde.Bodentyp = bodentyp;
                        // Nutzbare Spreizung (Konzept 13.1) - dieselbe Spalte wie beim
                        // Pufferspeicher-Quellendialog, jetzt auch hier pflegbar.
                        object oSpreizErde = WaermequelleClass.WertLesen(info.ID, "WQ_Spreizung");
                        if (oSpreizErde != null && Convert.ToDouble(oSpreizErde) > 0)
                            frmErde.Spreizung = Convert.ToDouble(oSpreizErde);

                        // Klimazone aus der Region vorbelegen (0 = nicht zugeordnet),
                        // Außentemperaturvektor einmalig laden und gecacht übergeben.
                        int zoneVorher = KlimazoneDesProjekts();
                        frmErde.Klimazone = zoneVorher;
                        frmErde.Aussentemperatur = AussentemperaturLaden();

                        // Ergebnisanbindung der Auslegungsprüfung (Paket 7): Liegt für
                        // diese Anlage ein Simulationslauf der Sitzung vor, bekommt der
                        // Dialog die echten Werte statt "(noch kein Simulationslauf)".
                        ErdreichAuswertung.AnlageErgebnis erdErg =
                            ErdreichAuswertung.FuerAnlage(m_ID_Projekt, info.ID);
                        if (erdErg != null)
                        {
                            frmErde.ErgebnisseVorhanden = erdErg.MaxEntzugBelastbar;
                            frmErde.MaxEntzugW = erdErg.MaxEntzugW;
                            frmErde.JahresentzugKWh = erdErg.JahresentzugKWh;
                            frmErde.VolllastStunden = erdErg.VolllastStunden;
                            if (erdErg.Unwirksam)
                                // Luft-Wasser: die Konfiguration wird gar nicht gerechnet.
                                // Das muss im Dialog stehen, sonst pflegt der Anwender
                                // Bodentyp und Sondenlänge ins Leere (Konzept 4.5).
                                frmErde.HinweisErgebnis = "Diese Konfiguration bleibt wirkungslos:\r\n\r\n" + erdErg.Grenze;
                            else if (!erdErg.MaxEntzugBelastbar)
                                frmErde.HinweisErgebnis = "Auslegungsprüfung nicht möglich:\r\n\r\n" + erdErg.Grenze;
                            else
                            {
                                if (erdErg.MaxEntzugGeschaetzt)
                                    frmErde.HinweisVorbehalt = erdErg.Grenze;
                                if (erdErg.InklSpeicherladung)
                                    frmErde.HinweisVorbehalt = (frmErde.HinweisVorbehalt.Length > 0
                                        ? frmErde.HinweisVorbehalt + " "
                                        : "") +
                                        "Entzugsarbeit und Spitze enthalten die Wärme, mit der die " +
                                        "Wärmepumpe den Pufferspeicher lädt.";
                                if (erdErg.FrostWarnung)
                                    frmErde.HinweisFrost = erdErg.Frosttext();
                            }
                        }

                        frmErde.SetControls();
                        if (frmErde.ShowDialog(this) != DialogResult.OK) return;

                        // Die Klimazone ist eine Eigenschaft der Region, nicht der Anlage
                        // (Konzept 13.1) - eine Änderung im Dialog geht deshalb an die Region.
                        if (frmErde.Klimazone != zoneVorher) KlimazoneSpeichern(frmErde.Klimazone);

                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Quellsystem", frmErde.Quellsystem);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Tiefe", frmErde.Tiefe);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Flaeche", frmErde.Flaeche);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Anzahl", frmErde.Anzahl);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Bodentyp", frmErde.Bodentyp);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Spreizung", frmErde.Spreizung);
                        WaermequelleClass.WertSchreiben(info.ID, "WQ_Typ", typNeu);
                        break;
                    }
            }

            AktualisiereErzeugerUebersicht();
        }

        /// <summary>
        /// Kleiner modaler Eingabedialog (Titel, Beschriftung, Vorgabewert).
        /// Liefert den eingegebenen Text oder null bei Abbruch.
        /// </summary>
        private string EingabeDialog(string titel, string beschriftung, string vorgabe)
        {
            Form frm = new Form();
            frm.Text = titel;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(340, 140);

            Label lbl = new Label { Text = beschriftung, AutoSize = true, Location = new Point(12, 12) };
            TextBox txt = new TextBox { Location = new Point(12, 75), Width = 316, Text = vorgabe ?? "" };
            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(172, 105), Width = 75 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(253, 105), Width = 75 };

            frm.Controls.Add(lbl);
            frm.Controls.Add(txt);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            return frm.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }

        /// <summary>
        /// Liefert die dem Erzeuger zugeordneten Pufferspeicher aus der
        /// Zuordnungstabelle (kommagetrennt) oder "-" ohne Zuordnung.
        /// </summary>
        private string ZugeordnetePufferSp(string erzeugerAnzeigeName)
        {
            // Aus dem kompletten Datenbestand lesen, nicht aus der (ggf. per
            // Pufferspeicher-Checkbox gefilterten) Tabellen-Anzeige.
            List<string> speicher = new List<string>();
            foreach (string[] z in _zuordnungen)
            {
                if (z[0] == erzeugerAnzeigeName && !string.IsNullOrEmpty(z[1]) && !speicher.Contains(z[1]))
                    speicher.Add(z[1]);
            }
            return speicher.Count > 0 ? string.Join(", ", speicher) : "-";
        }

        /// <summary>
        /// Baut den Dialog um (programmatisch, kein Designer/.resx nötig):
        /// - Die Checkbox "Pufferspeicher Zuordnung einblenden" entfällt.
        /// - Links unter "Wärmeerzeuger:" gibt es die neue Rubrik "Pufferspeicher:"
        ///   mit zwei Dropdown-Feldern (analog zu den vier Wärmeerzeuger-Feldern);
        ///   "Stromerzeuger:" und "Energiespeicher:" rücken dafür nach unten.
        /// - Die Gruppe "Pufferspeicher Zuordnung" erscheint - wie früher über die
        ///   Checkbox - erst, sobald in einem der Dropdowns ein Pufferspeicher
        ///   ausgewählt ist.
        /// </summary>
        private void InitPufferspeicherRubrik()
        {
            const int VERSCHIEBUNG = 105; // Platzbedarf der neuen Rubrik (Label + 2 Dropdowns)

            // Checkbox entfernen; Sichtbarkeit steuern künftig die Dropdowns
            checkBox_PufferSp.Visible = false;
            checkBox_PufferSp.Checked = true; // hält evtl. abfragende Logik konsistent
            groupBox_PufferSp.Visible = false;

            // Formular unten erweitern und die unteren Bedienelemente nachziehen
            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + VERSCHIEBUNG);
            btn_Speichern.Location = new Point(btn_Speichern.Left, this.ClientSize.Height - 42);
            btn_OK.Location = new Point(btn_OK.Left, this.ClientSize.Height - 42);
            lblStatus.Location = new Point(lblStatus.Left, this.ClientSize.Height - 37);

            // Linke Gruppe vergrößern und die Rubriken unterhalb der Wärmeerzeuger verschieben
            groupBox_Tools.Height += VERSCHIEBUNG;
            label2.Top += VERSCHIEBUNG;      // "Stromerzeuger:"
            comboBox5.Top += VERSCHIEBUNG;
            checkBox5.Top += VERSCHIEBUNG;
            label3.Top += VERSCHIEBUNG;      // "Energiespeicher:"
            comboBox6.Top += VERSCHIEBUNG;
            checkBox6.Top += VERSCHIEBUNG;

            // Neue Rubrik "Pufferspeicher:" unter den Wärmeerzeuger-Auswahlfeldern
            Label lblPufferSp = new Label();
            lblPufferSp.Name = "label_PufferSpRubrik";
            lblPufferSp.Text = "Pufferspeicher:";
            lblPufferSp.AutoSize = true;
            lblPufferSp.Font = label2.Font; // gleiche Optik wie "Stromerzeuger:"
            lblPufferSp.Location = new Point(label2.Left, comboBox4.Bottom + 14);
            groupBox_Tools.Controls.Add(lblPufferSp);
            lblPufferSp.BringToFront();

            // Zwei Pufferspeicher-Dropdowns (Felder 5 und 6, analog comboBox1-4).
            // Befüllt werden sie in SetControls aus den Stammdaten; künftig stehen
            // hier mehrere Pufferspeicher-Typen zur Auswahl.
            comboBox_Puffer1 = new ComboBox();
            comboBox_Puffer1.Name = "comboBox_Puffer1";
            comboBox_Puffer1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_Puffer1.Size = comboBox4.Size;
            comboBox_Puffer1.Font = comboBox4.Font;
            comboBox_Puffer1.Location = new Point(comboBox4.Left, lblPufferSp.Bottom + 4);
            comboBox_Puffer1.SelectedIndexChanged += comboBox_Puffer_SelectedIndexChanged;
            groupBox_Tools.Controls.Add(comboBox_Puffer1);
            comboBox_Puffer1.BringToFront();

            comboBox_Puffer2 = new ComboBox();
            comboBox_Puffer2.Name = "comboBox_Puffer2";
            comboBox_Puffer2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_Puffer2.Size = comboBox4.Size;
            comboBox_Puffer2.Font = comboBox4.Font;
            comboBox_Puffer2.Location = new Point(comboBox4.Left, comboBox_Puffer1.Bottom + 3);
            comboBox_Puffer2.SelectedIndexChanged += comboBox_Puffer_SelectedIndexChanged;
            groupBox_Tools.Controls.Add(comboBox_Puffer2);
            comboBox_Puffer2.BringToFront();

            // Checkboxen rechts neben den Dropdowns (analog checkBox1-4):
            // angehakt => Zuordnung dieses Pufferspeichers wird eingeblendet
            checkBox_Puffer1 = new CheckBox();
            checkBox_Puffer1.Name = "checkBox_Puffer1";
            checkBox_Puffer1.AutoSize = false;
            checkBox_Puffer1.Size = checkBox4.Size;
            checkBox_Puffer1.Location = new Point(checkBox4.Left, comboBox_Puffer1.Top + 6);
            checkBox_Puffer1.CheckedChanged += checkBox_Puffer_CheckedChanged;
            groupBox_Tools.Controls.Add(checkBox_Puffer1);
            checkBox_Puffer1.BringToFront();

            checkBox_Puffer2 = new CheckBox();
            checkBox_Puffer2.Name = "checkBox_Puffer2";
            checkBox_Puffer2.AutoSize = false;
            checkBox_Puffer2.Size = checkBox4.Size;
            checkBox_Puffer2.Location = new Point(checkBox4.Left, comboBox_Puffer2.Top + 6);
            checkBox_Puffer2.CheckedChanged += checkBox_Puffer_CheckedChanged;
            groupBox_Tools.Controls.Add(checkBox_Puffer2);
            checkBox_Puffer2.BringToFront();

            // Zuordnungs-Gruppe auf Höhe der neuen Rubrik ausrichten
            groupBox_PufferSp.Location = new Point(groupBox_PufferSp.Left,
                groupBox_Tools.Top + lblPufferSp.Top - 8);
        }

        /// <summary>
        /// Füllt die beiden Pufferspeicher-Dropdowns aus den Stammdaten
        /// (erster Eintrag leer = kein Pufferspeicher ausgewählt).
        /// </summary>
        private void FuellePufferSpAuswahl()
        {
            ComboBox[] boxen = { comboBox_Puffer1, comboBox_Puffer2 };
            foreach (ComboBox cb in boxen)
            {
                if (cb == null) continue;
                cb.Items.Clear();
                cb.Items.Add(""); // Abwahl möglich
                cb.Items.AddRange(listPufferSp.ToArray());
            }
        }

        /// <summary>
        /// Blendet die Gruppe "Pufferspeicher Zuordnung" ein, sobald mindestens
        /// eine der Pufferspeicher-Checkboxen angehakt ist.
        /// </summary>
        private void AktualisierePufferSpSichtbarkeit()
        {
            bool auswahl =
                (checkBox_Puffer1 != null && checkBox_Puffer1.Checked) ||
                (checkBox_Puffer2 != null && checkBox_Puffer2.Checked);
            groupBox_PufferSp.Visible = auswahl;
        }

        /// <summary>
        /// Liefert die aktuell aktiven (Checkbox angehakt + Dropdown belegt)
        /// Pufferspeicher - sie bestimmen den Filter der Zuordnungsanzeige.
        /// </summary>
        private List<string> AktivePufferSp()
        {
            List<string> aktive = new List<string>();
            if (checkBox_Puffer1 != null && checkBox_Puffer1.Checked &&
                comboBox_Puffer1.SelectedIndex > 0 && !aktive.Contains(comboBox_Puffer1.Text))
                aktive.Add(comboBox_Puffer1.Text);
            if (checkBox_Puffer2 != null && checkBox_Puffer2.Checked &&
                comboBox_Puffer2.SelectedIndex > 0 && !aktive.Contains(comboBox_Puffer2.Text))
                aktive.Add(comboBox_Puffer2.Text);
            return aktive;
        }

        /// <summary>
        /// Baut die Zuordnungstabelle aus dem Datenbestand neu auf - angezeigt
        /// werden nur die Zuordnungen der aktiven Pufferspeicher (separate Ansicht
        /// je Pufferspeicher). Über Tag bleibt jede Zeile mit ihrem Eintrag im
        /// Datenbestand verknüpft; gespeichert wird immer der komplette Bestand.
        /// </summary>
        private void RefreshZuordnungAnzeige()
        {
            AktualisierePufferSpSichtbarkeit();
            if (listView1 == null) return;

            List<string> filter = AktivePufferSp();

            listView1.Items.Clear();
            for (int i = 0; i < _zuordnungen.Count; i++)
            {
                string[] z = _zuordnungen[i];
                if (filter.Count > 0 && !filter.Contains(z[1])) continue;

                ListViewItem lvitem = new ListViewItem(new[] { z[0], z[1], z[2], z[3], "📂" });
                lvitem.Tag = i; // Index im Datenbestand
                listView1.Items.Add(lvitem);
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            AktualisiereErzeugerUebersicht();
        }

        private void comboBox_Puffer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_pufferUiUpdate) return;

            // Auswahl im Dropdown hakt die zugehörige Checkbox automatisch an
            // (analog comboBox1-4), Abwahl entfernt den Haken.
            _pufferUiUpdate = true;
            if (sender == comboBox_Puffer1 && checkBox_Puffer1 != null)
                checkBox_Puffer1.Checked = comboBox_Puffer1.SelectedIndex > 0;
            if (sender == comboBox_Puffer2 && checkBox_Puffer2 != null)
                checkBox_Puffer2.Checked = comboBox_Puffer2.SelectedIndex > 0;
            _pufferUiUpdate = false;

            RefreshZuordnungAnzeige();
        }

        private void checkBox_Puffer_CheckedChanged(object sender, EventArgs e)
        {
            if (_pufferUiUpdate) return;

            // Haken entfernt => zugehöriges Dropdown leeren (analog checkBox1-4)
            _pufferUiUpdate = true;
            if (sender == checkBox_Puffer1 && !checkBox_Puffer1.Checked &&
                comboBox_Puffer1 != null && comboBox_Puffer1.Items.Count > 0)
                comboBox_Puffer1.SelectedIndex = 0;
            if (sender == checkBox_Puffer2 && !checkBox_Puffer2.Checked &&
                comboBox_Puffer2 != null && comboBox_Puffer2.Items.Count > 0)
                comboBox_Puffer2.SelectedIndex = 0;
            _pufferUiUpdate = false;

            RefreshZuordnungAnzeige();
        }

        /// <summary>
        /// Wählt den übergebenen Pufferspeicher im Dropdown aus; steht er nicht in
        /// der Stammdaten-Liste (z. B. projektspezifischer Altbestand), wird er
        /// ergänzt, damit die vorhandene Zuordnung sichtbar bleibt.
        /// </summary>
        private void PufferSpVorbelegen(ComboBox cb, string name)
        {
            if (cb == null || string.IsNullOrEmpty(name)) return;
            if (!cb.Items.Contains(name)) cb.Items.Add(name);
            cb.SelectedItem = name;
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            item.SubItems[1].Text = comboBox.SelectedItem.ToString();
            comboBox.Visible = false;

            // Änderung in den Datenbestand übernehmen
            if (item.Tag is int idx && idx >= 0 && idx < _zuordnungen.Count)
                _zuordnungen[idx][1] = item.SubItems[1].Text;

            AktualisiereErzeugerUebersicht();
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            index = subItemIndex;

            // Die Bounds des SubItems relativ zum ListView holen
            Rectangle subItemBounds = hit.SubItem.Bounds;

            // Umrechnung der Position: Absolut zur Form, egal in welcher GroupBox das ListView liegt
            Point screenPoint = listView1.PointToScreen(subItemBounds.Location);
            Point formPoint = this.PointToClient(screenPoint);
            Rectangle displayBounds = new Rectangle(formPoint, subItemBounds.Size);

            if (subItemIndex == 1) // Spalte "Pufferspeicher"
            {
                // WICHTIG: Alte Events entfernen, um Mehrfach-Aufrufe zu verhindern
                comboBox.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;

                // Sicherstellen, dass die ComboBox auf der Form liegt
                if (!this.Controls.Contains(comboBox)) this.Controls.Add(comboBox);

                comboBox.Bounds = displayBounds;
                comboBox.Text = item.SubItems[subItemIndex].Text;
                comboBox.Visible = true;
                comboBox.BringToFront();
                comboBox.Focus();
                comboBox.DroppedDown = true; // Öffnet die Liste sofort beim Doppelklick
            }
            else if (subItemIndex == 2 || subItemIndex == 3) // "Vorlauf" oder "Rücklauf"
            {
                string alterWert = item.SubItems[subItemIndex].Text;
                TextBox textBox = new TextBox { Bounds = displayBounds, Text = alterWert };

                // Verhindert die Eingabe von Buchstaben
                textBox.KeyPress += (s, ev) => {
                    if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                    {
                        ev.Handled = true;
                    }
                };

                // Event beim Verlassen der TextBox
                textBox.LostFocus += (s, ev) =>
                {
                    string neuerText = textBox.Text;

                    // B4-2: Die Eingabe läuft jetzt über dieselbe Prüfung wie überall
                    // sonst (ProjektPuffer.TemperaturenPruefen, siehe
                    // Form_KonfigPufferspeicher und Wizard_WPItem). Geprüft wird das
                    // PAAR und nicht die einzelne Zelle - erst Vorlauf UND Rücklauf
                    // ergeben eine Spreizung. Die Gegenzelle steht schon im ListView.
                    int gegenSpalte = (subItemIndex == 2) ? 3 : 2;
                    string gegenText = item.SubItems[gegenSpalte].Text;
                    string vorlaufText = (subItemIndex == 2) ? neuerText : gegenText;
                    string ruecklaufText = (subItemIndex == 2) ? gegenText : neuerText;

                    string fehler;
                    if (!TemperaturPaarPruefen(vorlaufText, ruecklaufText, out fehler))
                    {
                        MessageBox.Show(fehler, "Temperatur prüfen",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        neuerText = alterWert;   // Zelle auf den letzten gültigen Stand zurück
                    }

                    item.SubItems[subItemIndex].Text = neuerText;

                    // Änderung in den Datenbestand übernehmen (Spalte 2=Vorlauf, 3=Rücklauf)
                    if (item.Tag is int idxZ && idxZ >= 0 && idxZ < _zuordnungen.Count)
                        _zuordnungen[idxZ][subItemIndex] = neuerText;

                    textBox.Dispose();
                };

                // Enter-Taste zum Bestätigen unterstützen
                textBox.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter)
                    {
                        listView1.Focus(); // Löst LostFocus aus
                        ev.SuppressKeyPress = true;
                    }
                };

                this.Controls.Add(textBox);
                textBox.BringToFront();
                textBox.Focus();
            }
            else if (subItemIndex == 4) // Spalte mit dem "📂" Symbol
            {
                Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
                frm.m_bReadOnly = true;
                frm.ShowDialog();
            }
        }

        /// <summary>
        /// Prüft das Temperaturpaar einer Zuordnungszeile beim Verlassen einer Zelle
        /// (B4-2). Grundlage ist <see cref="ProjektPuffer.TemperaturenPruefen"/> — eine
        /// Stelle für alle Temperatureingaben, ohne Untergrenze: 35/28 ist gültig.
        ///
        /// Zwei Zustände gelten ausdrücklich als in Ordnung, obwohl
        /// <c>TemperaturenPruefen</c> sie ablehnen würde:
        ///
        ///   - **beide Zellen leer oder 0** — das ist die RÜCKNAHME einer Vorgabe
        ///     (B4-3). Beim Speichern werden Vorlauf/Ruecklauf am Puffer dann auf NULL
        ///     gesetzt, und die Engine fällt geordnet zurück.
        ///   - **genau eine Zelle gefüllt** — der unvermeidliche Zwischenstand während
        ///     der Eingabe. Wer die erste von zwei Zellen füllt, darf dabei nicht mit
        ///     einer Meldung unterbrochen werden. Ein halbes Paar wird ohnehin nirgends
        ///     an den Puffer geschrieben.
        ///
        /// Abgefangen wird damit genau das, was schaden würde: ein VOLLSTÄNDIGES, aber
        /// unbrauchbares Paar (vertauscht, Spreizung 0, über 110 °C).
        /// </summary>
        private static bool TemperaturPaarPruefen(string vorlaufText, string ruecklaufText, out string fehler)
        {
            fehler = null;

            bool vorlaufLeer = IstLeerwert(vorlaufText);
            bool ruecklaufLeer = IstLeerwert(ruecklaufText);
            if (vorlaufLeer || ruecklaufLeer) return true;

            int vorlauf, ruecklauf;
            return ProjektPuffer.TemperaturenPruefen(vorlaufText, ruecklaufText,
                                                     out vorlauf, out ruecklauf, out fehler);
        }

        /// <summary>
        /// "Nicht gepflegt": leere Zelle oder die 0. Beides bedeutet in diesen Spalten
        /// dasselbe — der Zelleditor lässt kein Minus zu, und 0 °C Rücklauf ist auch in
        /// der Datenbank der Wert für "nichts eingetragen".
        /// </summary>
        private static bool IstLeerwert(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return true;

            int zahl;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out zahl) && zahl == 0;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != -1)
            {
                checkBox1.Checked = true;
                // listBox1.Items.Add(comboBox1.Text);
                AddErzeuger();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex != -1)
            {
                checkBox2.Checked = true;
                //  listBox1.Items.Add(comboBox2.Text);
                AddErzeuger();
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex != -1)
            {
                checkBox3.Checked = true;
                // listBox1.Items.Add(comboBox3.Text);
                AddErzeuger();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex != -1)
            {
                checkBox4.Checked = true;
                // listBox1.Items.Add(comboBox4.Text);
                AddErzeuger();
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex != -1) { checkBox5.Checked = true; }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex != -1) { checkBox6.Checked = true; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) { comboBox1.Text = ""; }
            AddErzeuger();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox2.Checked) { comboBox2.Text = ""; }
            AddErzeuger();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox3.Checked) { comboBox3.Text = ""; }
            AddErzeuger();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox4.Checked) { comboBox4.Text = ""; }
            AddErzeuger();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox5.Checked) { comboBox5.Text = ""; comboBox5.SelectedIndex = -1; }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox6.Checked) { comboBox6.Text = ""; comboBox6.SelectedIndex = -1; }
        }

        /// <summary>
        /// Schaltet die Eingaben ab, wenn die Schema-Migration nicht durchkam
        /// (ADR-001, Aufgabe 6). Bewusst nur die Kindsteuerelemente und nicht das
        /// Formular selbst - sonst ließe sich das Fenster nicht mehr schließen.
        /// </summary>
        private void SimulationsbereichSperren()
        {
            foreach (Control c in this.Controls) c.Enabled = false;
        }

        public void SetControls(int ID_Projekt)
        {
            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001, Aufgabe 6):
            // auf halb migriertem Schema zu konfigurieren, führt zu stillen Datenfehlern.
            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                MessageBox.Show(sperrgrund, "Simulation nicht verfügbar",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SimulationsbereichSperren();
                return;
            }

            var items = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_GESAMTSYSTEM, DbValue = "Gesamtsystem" },
            };

            m_ID_Projekt = ID_Projekt;

            // Neue Spalten (Prioritaet, Wärmequelle) bei Bedarf anlegen
            WaermequelleClass.SchemaSicherstellen();

            comboBox1.SelectedValue = Konfiguration.m_Tool_1;
            comboBox2.SelectedValue = Konfiguration.m_Tool_2;
            comboBox3.SelectedValue = Konfiguration.m_Tool_3;
            comboBox4.SelectedValue = Konfiguration.m_Tool_4;
            comboBox5.SelectedValue = Konfiguration.m_Tool_5;
            comboBox6.SelectedValue = Konfiguration.m_Tool_6;
            
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt= " + m_ID_Projekt);
            _zuordnungen.Clear();
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                var match = items.FirstOrDefault(x => x.DbValue == ctrlpsp.items[i].Erzeuger);
                _zuordnungen.Add(new[] {
                    match != null ? match.DisplayName : ctrlpsp.items[i].Erzeuger,
                    ctrlpsp.items[i].PufferSp,
                    ctrlpsp.items[i].Vorlauf.ToString(),
                    ctrlpsp.items[i].Ruecklauf.ToString() });
            }

            // Auswahl aus den STAMM-Daten füllen (eindeutige Bezeichner) - die
            // Projekt-Tabelle enthält Kopien aller Projekte und erzeugte Duplikate
            // in der Dropdown-Liste. Beim Speichern wird der gewählte Speicher bei
            // Bedarf automatisch aus den Stammdaten ins Projekt kopiert.
            RecordSet rsPsp = new RecordSet();
            rsPsp.Open("SELECT DISTINCT Bezeichner FROM " + PufferSpStammCtrl.TABLE + " ORDER BY Bezeichner");
            while (rsPsp.Next()) listPufferSp.Add(rsPsp.Read("Bezeichner").ToString());
            rsPsp.Close();
            comboBox.Items.AddRange(listPufferSp.ToArray());

            // Pufferspeicher-Dropdowns der Rubrik füllen und aus der vorhandenen
            // Zuordnung vorbelegen - dadurch erscheint die Zuordnungs-Gruppe
            // automatisch, wenn das Projekt bereits Zuordnungen hat.
            FuellePufferSpAuswahl();
            List<string> vorhandenePuffer = new List<string>();
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                string name = ctrlpsp.items[i].PufferSp;
                if (!string.IsNullOrEmpty(name) && !vorhandenePuffer.Contains(name))
                    vorhandenePuffer.Add(name);
            }
            if (vorhandenePuffer.Count > 0) PufferSpVorbelegen(comboBox_Puffer1, vorhandenePuffer[0]);
            if (vorhandenePuffer.Count > 1) PufferSpVorbelegen(comboBox_Puffer2, vorhandenePuffer[1]);

            // Zuordnungstabelle und Übersicht mit den geladenen Daten aufbauen
            RefreshZuordnungAnzeige();
        }

        // Hilfsmethode, um den DB-Wert sicher zu extrahieren
        private string GetDbValue(ComboBox cb)
        {
            // Variante A: Über SelectedValue (setzt voraus, dass ValueMember="DbValue" korrekt ist)
            // return cb.SelectedValue?.ToString() ?? "";

            // Variante B: Über das Objekt selbst (sicherste Methode)
            if (cb.SelectedItem is LanguageItem item)
            {
                return item.DbValue;
            }
            return ""; // Falls nichts ausgewählt ist
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();

            Konfiguration.m_Tool_1 = checkBox1.Checked ? GetDbValue(comboBox1) : "";
            Konfiguration.m_Tool_2 = checkBox2.Checked ? GetDbValue(comboBox2) : "";
            Konfiguration.m_Tool_3 = checkBox3.Checked ? GetDbValue(comboBox3) : "";
            Konfiguration.m_Tool_4 = checkBox4.Checked ? GetDbValue(comboBox4) : "";
            Konfiguration.m_Tool_5 = checkBox5.Checked ? GetDbValue(comboBox5) : "";
            Konfiguration.m_Tool_6 = checkBox6.Checked ? GetDbValue(comboBox6) : "";

            ctrl.model = Konfiguration;
            if (!ctrl.Delete(m_ID_Projekt)) return;
            if (ctrl.Insert(m_ID_Projekt)) ShowStatus("✔ Konfiguration erfolgreich gespeichert", Color.ForestGreen);

            int prioritaet = 1;

            ctrlpsp.ID_Projekt = m_ID_Projekt;

            // B0-1: Die Schwellen der Speicherregelung hängen an der Zuordnungszeile und
            // überleben den Delete/Insert-Zyklus nicht (stiller Rückfall auf 10/95 %).
            // Vor dem Löschen sichern; Schlüssel: Erzeuger (DB-Wert) + Pufferspeicher.
            var alteSchwellen = new Dictionary<string, double?[]>();
            ctrlpsp.ReadAll("ID_Projekt=" + m_ID_Projekt);
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                var alt = ctrlpsp.items[i];
                alteSchwellen[alt.Erzeuger + "|" + alt.PufferSp] =
                    new double?[] { alt.Schwelle_Ein, alt.Schwelle_Aus };
            }

            if (!ctrlpsp.Delete()) return;

            // B0-11: Mapping-Liste einmal statt in jedem Schleifendurchlauf aufbauen.
            var items = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_BHKW, DbValue = "BHKW" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_HEIZKESSEL, DbValue = "Heizkessel" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_SOLARTHERMIE, DbValue = "Solarthermie" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_GESAMTSYSTEM, DbValue = "Gesamtsystem" },
                new LanguageItem { DisplayName = MyResource.Resource.KONFIG_WAERMEPUMPE, DbValue = "Wärmepumpe" },
            };

            // WICHTIG: Gespeichert wird der komplette Datenbestand - nicht nur die
            // aktuell (per Pufferspeicher-Checkbox gefiltert) angezeigten Zeilen!
            int fehlgeschlagen = 0;

            // B4-1: An den PUFFER schreibt nur die eine Zeile, die die Engine auch
            // auswertet. SimulationControl.Do_Simulation überspringt jede Zuordnung mit
            // einem anderen Erzeuger (continue) und bricht nach dem ersten
            // Wärmepumpen-Treffer ab (break) - die Reihenfolge ist ORDER BY Prioritaet.
            // Genau diese Zeile wird hier bestimmt: die Priorität vergibt die Schleife
            // unten fortlaufend in Listenreihenfolge (prioritaet++), also gewinnt die
            // ERSTE Wärmepumpen-Zeile der Liste. Sie bekommt zugleich die kleinste ID,
            // womit auch der Gleichstandsfall der Migration (R1: ORDER BY Prioritaet, ID)
            // dieselbe Zeile wählt.
            //
            // Alles andere - BHKW-, Kessel-, Solarthermie- und Gesamtsystem-Zeilen sowie
            // jede weitere WP-Zeile - schreibt NICHT an den Puffer. Vorher tat es das:
            // die zuletzt gespeicherte Zeile überschrieb die Betriebstemperaturen des
            // Speichers, obwohl die Engine sie nie gelesen hat. Das hätte die
            // R2-Entscheidung der Migration ausgehebelt (wirkungslose Altzuordnungen
            // bleiben wirkungslos) und über den Vorrang der führenden Ablage sogar
            // ergebniswirksam werden können.
            bool pufferZeileGeschrieben = false;

            for (int i = 0; i < _zuordnungen.Count; i++)
            {
                string[] z = _zuordnungen[i];
                ctrlpsp.PufferSp = z[1];

                // B0-11: erst über den Anzeigenamen matchen, nur bei Misserfolg über den
                // DB-Wert — defensiv gegen Alt-/Fremdwerte in _zuordnungen, damit nie ein
                // lokalisierter Anzeigename als Erzeuger in der Datenbank landet.
                var match = items.FirstOrDefault(x => x.DisplayName == z[0])
                         ?? items.FirstOrDefault(x => x.DbValue == z[0]);
                ctrlpsp.Erzeuger = match?.DbValue ?? z[0];

                // B0-1: gesicherte Schwellen der Zuordnung wieder mitgeben
                double?[] schwellen;
                if (alteSchwellen.TryGetValue(ctrlpsp.Erzeuger + "|" + ctrlpsp.PufferSp, out schwellen))
                {
                    ctrlpsp.Schwelle_Ein = schwellen[0];
                    ctrlpsp.Schwelle_Aus = schwellen[1];
                }
                else
                {
                    ctrlpsp.Schwelle_Ein = null;
                    ctrlpsp.Schwelle_Aus = null;
                }

                // Konzept 4.6: TryParse statt Int32.Parse. Ein leeres oder unlesbares
                // Feld warf hier bisher eine unbehandelte FormatException — und zwar
                // NACH dem Delete, also mitten im Datenverlust. Unlesbares wird zu 0;
                // die Engine fällt dann auf ihre Vorgabespreizung zurück.
                int vorlauf, ruecklauf;
                if (!Int32.TryParse(z[2], out vorlauf)) vorlauf = 0;
                if (!Int32.TryParse(z[3], out ruecklauf)) ruecklauf = 0;

                ctrlpsp.Vorlauf = vorlauf;
                ctrlpsp.Ruecklauf = ruecklauf;
                ctrlpsp.Prioritaet = prioritaet++;

                bool istWaermepumpe = string.Equals(ctrlpsp.Erzeuger,
                                                    ProjektPuffer.ERZEUGER_WAERMEPUMPE,
                                                    StringComparison.Ordinal);

                // B0-1: Rückgabewert auswerten — nach dem Delete ist ein stiller
                // Insert-Fehlschlag ein Datenverlust und muss sichtbar werden.
                if (!ctrlpsp.Insert()) fehlgeschlagen++;
                else if (istWaermepumpe && !pufferZeileGeschrieben)
                {
                    pufferZeileGeschrieben = true;

                    // Etappe 4: Die Puffer-Zeile ist die FÜHRENDE Ablage der
                    // Betriebstemperaturen (Konzept 5.1) — die Zuordnung wird nur noch
                    // mitgeschrieben, damit Alt-Datenbanken lesbar bleiben. Insert()
                    // hat ID_Pufferspeicher gerade frisch aufgelöst (und die
                    // Projektkopie bei Bedarf angelegt), der Wert zeigt also sicher auf
                    // Tab_Pufferspeicher.
                    if (ProjektPuffer.IstTemperaturpaar(vorlauf, ruecklauf))
                    {
                        // Niedrige Paare wie 35/28 laufen unverändert durch — hier wird
                        // nichts geklemmt.
                        PufferSpCtrl.SetTemperaturen(ctrlpsp.ID_Pufferspeicher, vorlauf, ruecklauf);
                    }
                    else if (vorlauf <= 0 && ruecklauf <= 0)
                    {
                        // B4-3, Rücknahme: Der Anwender hat beide Zellen geleert. Am
                        // Speicher darf dann kein alter Wert stehen bleiben — er wäre
                        // die führende Ablage und verdeckte die Zuordnung dauerhaft.
                        // Mit NULL fällt die Engine geordnet zurück (Zuordnung, sonst
                        // Vorgabe 10 K).
                        PufferSpCtrl.TemperaturenLoeschen(ctrlpsp.ID_Pufferspeicher);
                    }
                    // Halb gefülltes oder vertauschtes Paar: unverändert lassen. Die
                    // Eingabe wird bereits im Zelleditor über
                    // ProjektPuffer.TemperaturenPruefen abgefangen; kommt hier doch so
                    // etwas an (Altbestand in _zuordnungen), ist Nichtstun die sichere
                    // Wahl — es entsteht weder eine Scheinvorgabe noch ein Datenverlust.
                }
            }

            if (fehlgeschlagen > 0)
                ShowStatus("⚠ " + fehlgeschlagen + " Pufferspeicher-Zuordnung(en) konnten nicht gespeichert werden",
                    Color.Firebrick);
        }

        private void AddErzeuger()
        {
            listErzeuger.Clear(); // Liste leeren, wir bauen sie neu auf

            // Wir erstellen ein Array von Paaren: Checkbox + zugehörige ComboBox
            // Das ist viel sauberer als 4 separate Abfragen.
            var controlPairs = new[]
            {
                new { CheckBox = checkBox1, ComboBox = comboBox1 },
                new { CheckBox = checkBox2, ComboBox = comboBox2 },
                new { CheckBox = checkBox3, ComboBox = comboBox3 },
                new { CheckBox = checkBox4, ComboBox = comboBox4 }
            };

            foreach (var pair in controlPairs)
            {
                // SCHRITT 1: Prüfen, ob die Checkbox überhaupt aktiv ist!
                if (pair.CheckBox.Checked)
                {
                    // SCHRITT 2: Wenn ja, prüfen wir die ComboBox
                    if (pair.ComboBox.SelectedItem is LanguageItem selectedItem)
                    {
                        string valueToSave = selectedItem.DbValue;

                        if (!string.IsNullOrEmpty(valueToSave) && !listErzeuger.Contains(valueToSave))
                        {
                            listErzeuger.Add(valueToSave);
                        }
                    }
                }
                // WENN die Checkbox nicht aktiv ist (Checked == false), 
                // wird ihr ComboBox-Inhalt einfach ignoriert und nicht zur Liste hinzugefügt.
                // Ein explizites "Löschen" ist nicht nötig, da wir listErzeuger.Clear() oben machen.
            }

            // "Gesamtsystem" immer hinzufügen (außer es ist schon drin)
            if (!listErzeuger.Contains("Gesamtsystem"))
            {
                listErzeuger.Add("Gesamtsystem");
            }

            // Übersicht rechts an die geänderte Auswahl anpassen
            AktualisiereErzeugerUebersicht();
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            Form_KonfigPufferspeicher frm = new Form_KonfigPufferspeicher();

            // Transformation in eine Liste von Strings (nur die Anzeigenamen)
            List<string> displayListe = listErzeuger
                .Select(dbVal => _waermeerzeugerItems.FirstOrDefault(refItem => refItem.DbValue == dbVal)?.DisplayName)
                .Where(name => name != null) // Falls ein DB-Wert nicht in der Referenzliste war
                .ToList();
            displayListe.Add(MyResource.Resource.KONFIG_GESAMTSYSTEM);

            frm.listErzeuger = displayListe;

            // Nur die in der Rubrik aktivierten Pufferspeicher anbieten;
            // ohne aktive Auswahl steht die komplette Stammdaten-Liste bereit.
            List<string> aktivePuffer = AktivePufferSp();
            frm.listPufferSp = aktivePuffer.Count > 0 ? aktivePuffer : listPufferSp;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls();

            DialogResult result = frm.ShowDialog();
            if (result == DialogResult.OK)
            {
                _zuordnungen.Add(new[] { frm.model.Erzeuger, frm.model.PufferSp,
                    frm.model.Vorlauf.ToString(), frm.model.Ruecklauf.ToString() });
                RefreshZuordnungAnzeige();
            }
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            // Über Tag den Eintrag im Datenbestand entfernen (Anzeige kann gefiltert sein)
            if (listView1.SelectedItems[0].Tag is int idx && idx >= 0 && idx < _zuordnungen.Count)
                _zuordnungen.RemoveAt(idx);

            RefreshZuordnungAnzeige();
        }

        private void checkBox_PufferSp_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_PufferSp.Checked)
            {
                groupBox_PufferSp.Visible = true;
            }
            else
            {
                groupBox_PufferSp.Visible = false;
            }
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Hintergrund Hellblau
            using (SolidBrush pb = new SolidBrush(Color.LightBlue))
            {
                e.Graphics.FillRectangle(pb, e.Bounds);
            }

            // Text zeichnen
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

            // Rahmen (Optional)
            e.Graphics.DrawRectangle(Pens.LightGray, e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zeile
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zellen (inkl. Symbole)
        }

        private void SetGroupBoxFontBold(GroupBox gb)
        {
            // Titel der GroupBox fett machen
            gb.Font = new Font(gb.Font, FontStyle.Bold);

            // Alle Kinder in der GroupBox wieder auf normal setzen
            foreach (Control c in gb.Controls)
            {
                c.Font = new Font(c.Font, FontStyle.Regular);
            }
            gb.Invalidate(); 
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Visible = true;

            statusTimer.Stop(); // Falls er noch lief, zurücksetzen
            statusTimer.Start();
        }
    }
}
