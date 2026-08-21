using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class MDIMainForm : Form
    {
        public MDIMainForm()
        {
            InitializeComponent();

            // Statt MDI: reguläre SDI-Hauptform.
            // Form_Start wird unten in MDIMainForm_Load als eingebettete Form
            // (TopLevel=false) ins Controls-Collection gehängt – wie ein UserControl.
            this.IsMdiContainer = false;

            // Beim Start vollflächig, aber später vom Nutzer skalierbar.
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Produktname dezent im Kopfbereich anzeigen
            InitMarke();

            // KI-Hilfe-Assistent einbinden (Menüeintrag und F1)
            InitKiHilfe();

            // Lizenzverwaltung einbinden (Administration → Lizenz)
            InitLizenzMenue();

            // Lastspitzenkappung einbinden (Strombedarf & Speicher → Peak-Shaving)
            InitPeakShavingMenue();

            // Katalog gesetzlicher Parameter einbinden (Administration → Gesetzliche Parameter)
            InitGesetzeMenue();

            // Katalog-Dublettensuche einbinden (Administration → Katalog-Dubletten prüfen)
            InitDublettenMenue();
        }

        /// <summary>
        /// Bindet die Pflegemaske „Gesetzliche Parameter" ein: Menüeintrag im Menü
        /// Administration direkt unterhalb von „Einstellungen"
        /// (Konzept_BHKW_Kosten_Erloese.md, Abschnitt 6, Etappe E1).
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben; der
        /// Anzeigetext kommt aus MyResource und ist damit zweisprachig.
        /// </summary>
        private void InitGesetzeMenue()
        {
            try
            {
                ToolStripMenuItem eintrag = new ToolStripMenuItem(
                    MyResource.Resource.GESETZ_MENUE);
                eintrag.Name = "MenuItem_Gesetzesparameter";
                eintrag.Click += (s, e) =>
                {
                    using (Form_Gesetzesparameter frm = new Form_Gesetzesparameter())
                        frm.ShowDialog(this);
                };

                // Direkt unterhalb von "Einstellungen" einordnen — dieselbe Stelle,
                // an der auch die Lizenzverwaltung hängt.
                int position = Administration.DropDownItems.IndexOf(MenuItem_Einstellungen);
                if (position >= 0)
                    Administration.DropDownItems.Insert(position + 1, eintrag);
                else
                    Administration.DropDownItems.Add(eintrag);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Menü der gesetzlichen Parameter konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Bindet die Admin-Dublettensuche ein: Menüeintrag im Menü Administration
        /// unterhalb von „Einstellungen" bzw. nach dem Gesetze-Eintrag
        /// (Konzept_Dublettenpruefung_Import_EPOS-Plan.md, Abschnitt 5, Paket D3).
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben; der
        /// Anzeigetext kommt aus MyResource und ist damit zweisprachig.
        /// </summary>
        private void InitDublettenMenue()
        {
            try
            {
                ToolStripMenuItem eintrag = new ToolStripMenuItem(
                    MyResource.Resource.ADM_DUBLETTEN_MENUE);
                eintrag.Name = "MenuItem_KatalogDubletten";
                eintrag.Click += (s, e) =>
                {
                    using (Form_KatalogDubletten frm = new Form_KatalogDubletten())
                        frm.ShowDialog(this);
                };

                // Unterhalb von "Einstellungen" einordnen; hängt dort bereits der
                // Gesetze-Eintrag (InitGesetzeMenue läuft davor), rückt dieser Eintrag
                // dahinter — die Admin-Werkzeuge bleiben beieinander.
                int position = Administration.DropDownItems.IndexOf(MenuItem_Einstellungen);
                if (position >= 0)
                {
                    ToolStripItem gesetze = Administration.DropDownItems["MenuItem_Gesetzesparameter"];
                    int posGesetze = gesetze != null ? Administration.DropDownItems.IndexOf(gesetze) : -1;
                    Administration.DropDownItems.Insert((posGesetze > position ? posGesetze : position) + 1, eintrag);
                }
                else
                {
                    Administration.DropDownItems.Add(eintrag);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Menü der Katalog-Dublettensuche konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Bindet die Lastspitzenkappung (Peak-Shaving) ein: eigener Menüeintrag
        /// direkt unterhalb von „Stromspeicher“ (Fachkonzept 6.4 – separate
        /// Funktionalität, offener Punkt 10, AP7).
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben; der
        /// Anzeigetext kommt aus MyResource und ist damit zweisprachig.
        /// </summary>
        private void InitPeakShavingMenue()
        {
            try
            {
                ToolStripMenuItem eintrag = new ToolStripMenuItem(
                    MyResource.Resource.PEAK_MENUE);
                eintrag.Name = "MenuItem_PeakShaving";
                eintrag.Click += (s, e) => new MenueCtrl().PeakShavingBearbeiten();

                // Direkt unterhalb von „Stromspeicher“ einordnen
                int position = MenuItem_StromBedarfundSp.DropDownItems.IndexOf(MenuItem_Stromspeicher);
                if (position >= 0)
                    MenuItem_StromBedarfundSp.DropDownItems.Insert(position + 1, eintrag);
                else
                    MenuItem_StromBedarfundSp.DropDownItems.Add(eintrag);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Peak-Shaving-Menü konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>Produktname für Titelleiste, Kopfzeile und Meldungen.</summary>
        public const string PRODUKTNAME = "EPOS-Plan";

        /// <summary>Gattungsbezeichnung, erscheint unter dem Produktnamen.</summary>
        public const string PRODUKT_GATTUNG = "Energieplanungs-Software";

        /// <summary>Auflösung des Namens EPOS: Energie, Planung, Optimierung, Simulation.</summary>
        public const string PRODUKT_CLAIM = "Energie · Planung · Optimierung · Simulation";

        /// <summary>
        /// Zeigt den Produktnamen zurückhaltend an: in der Titelleiste sowie in
        /// einer schmalen Kopfzeile unterhalb des Menüs. Die Kopfzeile enthält
        /// links den Namen mit der Auflösung EPOS = Energie, Optimierung,
        /// Planung, Simulation und rechts die Programmversion.
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben.
        /// </summary>
        private void InitMarke()
        {
            try
            {
                this.Text = PRODUKTNAME;

                Panel kopf = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 52,
                    BackColor = System.Drawing.Color.White
                };
                kopf.Paint += (s, e) =>
                {
                    // feine Trennlinie unten
                    using (System.Drawing.Pen stift = new System.Drawing.Pen(System.Drawing.Color.FromArgb(222, 227, 232)))
                        e.Graphics.DrawLine(stift, 0, kopf.Height - 1, kopf.Width, kopf.Height - 1);
                    // schmaler Farbakzent links, damit der Name Halt bekommt, ohne zu dominieren
                    using (System.Drawing.SolidBrush pinsel = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 90, 160)))
                        e.Graphics.FillRectangle(pinsel, 0, 10, 4, kopf.Height - 22);
                };

                Label name = new Label
                {
                    Text = PRODUKTNAME,
                    AutoSize = true,
                    Font = new System.Drawing.Font("Segoe UI Semibold", 14f, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.FromArgb(0, 90, 160),
                    Margin = new Padding(0, 0, 0, 0)
                };

                Label untertitel = new Label
                {
                    Text = PRODUKT_GATTUNG + "  ·  " + PRODUKT_CLAIM,
                    AutoSize = true,
                    Font = new System.Drawing.Font("Segoe UI", 8.25f),
                    ForeColor = System.Drawing.Color.FromArgb(112, 119, 126),
                    Margin = new Padding(1, 1, 0, 0)
                };

                FlowLayoutPanel links = new FlowLayoutPanel
                {
                    Dock = DockStyle.Left,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Padding = new Padding(16, 6, 0, 0),
                    BackColor = System.Drawing.Color.Transparent
                };
                links.Controls.Add(name);
                links.Controls.Add(untertitel);

                Label version = new Label
                {
                    Text = "Version " + VersionText(),
                    Dock = DockStyle.Right,
                    AutoSize = true,
                    Font = new System.Drawing.Font("Segoe UI", 8.25f),
                    ForeColor = System.Drawing.Color.FromArgb(150, 156, 162),
                    Padding = new Padding(0, 20, 18, 0),
                    BackColor = System.Drawing.Color.Transparent
                };

                ToolTip hinweis = new ToolTip();
                hinweis.SetToolTip(name, PRODUKTNAME + " - " + PRODUKT_GATTUNG + " (" + PRODUKT_CLAIM + ")");
                hinweis.SetToolTip(untertitel, PRODUKTNAME + " - " + PRODUKT_GATTUNG + " (" + PRODUKT_CLAIM + ")");

                kopf.Controls.Add(links);
                kopf.Controls.Add(version);

                this.Controls.Add(kopf);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Kopfzeile konnte nicht erstellt werden: " + ex.Message);
            }
        }

        /// <summary>Versionsnummer der Anwendung als Text (z. B. "1.0.0.0").</summary>
        private static string VersionText()
        {
            try
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }

        /// <summary>
        /// Bindet den KI-Hilfe-Assistenten ein: ein Menüeintrag unter "Hilfe"
        /// (bzw. ein eigener Menüpunkt, falls kein Hilfe-Menü vorhanden ist)
        /// sowie F1 als Tastenkürzel. Der Assistent bekommt automatisch den
        /// Bereich mitgeteilt, in dem der Benutzer gerade arbeitet.
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben.
        /// </summary>
        private void InitKiHilfe()
        {
            try
            {
                ToolStripMenuItem eintrag = new ToolStripMenuItem("Hilfe-Assistent (KI)...");
                eintrag.ShortcutKeys = Keys.F1;
                eintrag.ShowShortcutKeys = true;
                eintrag.Click += (s, e) => Form_KiChat.Oeffnen(this);

                MenuStrip strip = SucheMenuStrip(this);
                if (strip != null)
                {
                    // Vorhandenes Hilfe-Menü suchen, sonst ein neues anlegen
                    ToolStripMenuItem hilfeMenu = null;
                    foreach (ToolStripItem item in strip.Items)
                    {
                        ToolStripMenuItem mi = item as ToolStripMenuItem;
                        if (mi == null) continue;
                        string text = (mi.Text ?? "").Replace("&", "");
                        if (text.StartsWith("Hilfe", StringComparison.OrdinalIgnoreCase) ||
                            text.StartsWith("Help", StringComparison.OrdinalIgnoreCase))
                        {
                            hilfeMenu = mi;
                            break;
                        }
                    }

                    if (hilfeMenu == null)
                    {
                        hilfeMenu = new ToolStripMenuItem("Hilfe");
                        strip.Items.Add(hilfeMenu);
                    }

                    if (hilfeMenu.DropDownItems.Count > 0)
                        hilfeMenu.DropDownItems.Add(new ToolStripSeparator());
                    hilfeMenu.DropDownItems.Add(eintrag);

                    // Abschalter der Installation: ist er gesetzt, ist der Assistent
                    // gar nicht erst erreichbar. Ausgewertet wird beim Aufklappen und
                    // nicht einmalig beim Start - die Einstellung kann im laufenden
                    // Programm über die Administration umgelegt werden.
                    hilfeMenu.DropDownOpening += (s, e) =>
                        eintrag.Available = !KiEinwilligung.Abgeschaltet;
                }

                // F1 auch unabhängig vom Menü verfügbar machen
                this.KeyPreview = true;
                this.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.F1)
                    {
                        e.Handled = true;
                        Form_KiChat.Oeffnen(this);
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("KI-Hilfe konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Bindet die Lizenzverwaltung ein: Menüeintrag "Lizenz…" im Menü
        /// Administration direkt unterhalb von "Einstellungen" sowie eine
        /// stille Online-Nachprüfung des Lizenz-Tokens im Hintergrund.
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben.
        /// </summary>
        private void InitLizenzMenue()
        {
            try
            {
                ToolStripMenuItem eintrag = new ToolStripMenuItem(
                    Program.nLanguage == 1 ? "License…" : "Lizenz…");
                eintrag.Click += (s, e) =>
                {
                    using (Form_LizenzVerwaltung frm = new Form_LizenzVerwaltung())
                        frm.ShowDialog(this);
                };

                // Direkt unterhalb von "Einstellungen" einordnen
                int position = Administration.DropDownItems.IndexOf(MenuItem_Einstellungen);
                if (position >= 0)
                    Administration.DropDownItems.Insert(position + 1, eintrag);
                else
                    Administration.DropDownItems.Add(eintrag);

                // Stille Nachprüfung — Fehler bleiben bewusst folgenlos,
                // die Karenzzeit im LizenzManager fängt Offline-Phasen ab.
                _ = LizenzManager.NachpruefungImHintergrund();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lizenzmenü konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>Sucht die MenuStrip des Formulars (auch in Unterebenen).</summary>
        private MenuStrip SucheMenuStrip(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                MenuStrip ms = c as MenuStrip;
                if (ms != null) return ms;
                if (c.Controls.Count > 0)
                {
                    MenuStrip treffer = SucheMenuStrip(c);
                    if (treffer != null) return treffer;
                }
            }
            return this.MainMenuStrip;
        }

        private void MenuItem_Neu_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();

            // Befund 3: Der Menueweg hat die Startseite bisher gar nicht aktualisiert -
            // Reiter blieben gesperrt und m_ID_Projekt zeigte weiter auf das zuvor
            // geoeffnete Projekt, die Wizard-Kacheln schrieben also ins falsche Projekt.
            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
                Program.startfrm?.ProjektKontextUebernehmen(Program.wizardctrl.Projektname);
        }

        private async void MDIMainForm_Load(object sender, EventArgs e)
        {
            // Verhindert, dass der Designer in Visual Studio die API blockiert
            if (this.DesignMode) return;

            try
            {
                // Einmaliger Download der Slugs beim echten Programmstart
                label_OnlineDoku.Left = (this.ClientSize.Width - label_OnlineDoku.Width) / 2;
                label_OnlineDoku.Top = (this.ClientSize.Height - label_OnlineDoku.Height) / 2;
                label_OnlineDoku.Visible = true;
                Program.HelpCatalog.LoadAllAsync(); // Await entfernt, wordpress zugriff asynchron, Main läuft weiter,
                                                    // Doku wird im Hintergrund geladen, wenn Nutzer auf Doku klickt, wird geprüft ob schon geladen,
                                                    // wenn nein, dann warten bis geladen ist, wenn ja, dann sofort öffnen
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Laden der Doku: " + ex.Message);
            }
            label_OnlineDoku.Visible = false;

            // Form_Start als eingebettete Hauptansicht (kein MDI-Child mehr).
            // TopLevel=false erlaubt es, eine Form wie ein UserControl in Controls.Add zu hängen.
            // Dock=Fill sorgt für korrekte Skalierung beim Resize/DPI-Wechsel.
            Program.startfrm = new Form_Start
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
            };
            this.Controls.Add(Program.startfrm);
            Program.startfrm.BringToFront();
            Program.startfrm.Show();
        }

        private void MenuItem_zuletztGeöffnet_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektOeffnen(true);
        }

        private void MenuItem_ProjektLöschen_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektDelete();
        }

        private void MenuItem_Klimadaten_Click(object sender, EventArgs e)
        {
            Form_Klimadaten frm = new Form_Klimadaten();
            frm.ShowDialog();
        }

        private void MenuItem_ProjektBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();

            // Befund 3: wie bei "Neu" - ohne diese Zeile bleibt der Projektkontext der
            // Startseite auf dem vorher geoeffneten Projekt stehen.
            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
                Program.startfrm?.ProjektKontextUebernehmen(Program.wizardctrl.Projektname);
        }

        private void MenuItem_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektOeffnen();
        }

        private void MenuItem_WPBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.WP_Administration();
        }

        private void MenuItem_Stromspeicher_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.StromspeicherBearbeiten();
        }

        private void MenuItem_GebBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.GebaeudeBearbeiten();
        }

        private void MenuItem_GebTypen_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.GebaeudetypenBearbeiten();
        }

        private void MenuItem_WaermebedarfExtern_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.WaermebedarfExtern(); 
        }

        private void MenuItem_Prozesswaerme_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Prozesswaerme(); 
        }

        private void MenuItem_Stromverbraucher_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromverbraucher(); 
        }

        private void MenuItem_Stromganglinie_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromganglinie();
        }

        private void MenuItem_WP_VDI3805_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.WPImport();
        }

        private void MenuItem_BHKW_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.BHKW();
        }

        private void MenuItem_SolThermGanglinie_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Solarganglinie();
        }

        private void MenuItem_Version_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                PRODUKTNAME + Environment.NewLine +
                PRODUKT_CLAIM + Environment.NewLine + Environment.NewLine +
                "Version " + VersionText() + Environment.NewLine +
                "INEKON - Intelligente Energiekonzepte",
                "Über " + PRODUKTNAME, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MenuItem_Lizenz_Click(object sender, EventArgs e)
        {
            // Lizenzvereinbarung und AGB anzeigen (Grundlage: LIZENZ-INEKON.rtf
            // aus dem Projektstammverzeichnis)
            Form_Lizenz.Anzeigen(this);
        }

        private void Deutsch_Click(object sender, EventArgs e)
        {
            var culture_de = new CultureInfo("de-DE");

            // Erzwingen der deutschen Sprache
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            var nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 0) return;
            key.SetValue("Language", 0, RegistryValueKind.DWord);
            Application.Restart();
        }

        private void Englisch_Click(object sender, EventArgs e)
        {
            var culture_de = new CultureInfo("en-US");

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            var nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 1) return;
            key.SetValue("Language", 1, RegistryValueKind.DWord);
            Application.Restart();
        }

        private void MenuItem_SPKBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Kessel();
        }

        private void MeniItem_PufferSp_VDI3805_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSPImport();
        }

        private void MenuItem_PufferSpBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSp();
        }

        private void MenuItem_Brauchwasser_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Brauchwasser();
        }

        private void MenuItem_PV_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_AdminPV frm = new Form_AdminPV();
            frm.ShowDialog();
        }

        private void MenuItem_PV_Import_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PVImport();
        }

        private void MenuItem_ST_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Solarkollektoren();
        }

        private void MenuItem_ST_Import_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.SolarThermieImport();
        }
        
        private void MenuItem_Import_Heizkessel_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.SPKImport();
        }

        private void MenuItem_Kessel_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Kessel();
        }

        private void MenuItem_PufferSp_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSp();
        }

        private void MenuItem_WP_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.WP_Administration();
        }

        private void MenuItem_PV_Import_CEC_Click(object sender, EventArgs e)
        {
            Main_PV_Test frm = new Main_PV_Test();
            frm.ShowDialog();
        }

        private void MenuItem_PV_Import_PAN_Click(object sender, EventArgs e)
        {
            Main_PV_Test frm = new Main_PV_Test();
            frm.ShowDialog();
        }

        private void MenuItem_Kosten_Click(object sender, EventArgs e)
        {
            int id = Program.startfrm.m_ID_Projekt;
            if (id != 0)
            {
                using (var form = new Form_Kosten(id))
                {
                    form.ShowDialog(); // Öffnet das Fenster als modaler Dialog
                }
            }
            else MessageBox.Show("Projekt auswählen!");
        }

        private void kostenAdminToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_KostenAdmin frm = new Form_KostenAdmin();  
            frm.ShowDialog();
        }

        private void MenuItem_Einstellungen_Click(object sender, EventArgs e)
        {
            Form_AdminSettings frm = new Form_AdminSettings();
            frm.ShowDialog();
        }

        /// <summary>Online-Dokumentation von epos-plan.de.</summary>
        public const string DOKU_URL = "https://epos-plan.de/epos-plan/epos-plan-dokumetation/";

        private void MenuItem_Dokumentation_Click(object sender, EventArgs e)
        {
            try
            {
                // Standard ist die Online-Dokumentation; ist in den Einstellungen
                // eine abweichende (z. B. lokale) WordPress-Adresse hinterlegt,
                // hat diese Vorrang.
                string _targetUrl = Properties.Settings.Default.WordPressUrl;
                if (string.IsNullOrWhiteSpace(_targetUrl) ||
                    _targetUrl.Contains("localhost"))
                {
                    _targetUrl = DOKU_URL;
                }

                Process.Start(new ProcessStartInfo { FileName = _targetUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Öffnen des Links: " + ex.Message);
            }
        }
    }
}

