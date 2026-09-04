using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

            // Kostenvorlagen-Pflege einbinden (Administration → Kosten → Kostenvorlagen)
            InitKostenvorlagenMenue();
        }

        /// <summary>
        /// Bindet den Komponenten-Kostendialog (Stammvorlagen) ein: Menüeintrag im
        /// Untermenü Administration → Kosten (Ä7: die Alteinträge „Kosten“/„Kosten
        /// Admin“ sind entfernt; der Positionskatalog hängt als Knopf in der
        /// Kostenverwaltung), unterhalb der Bestandseinträge
        /// (Konzept Kostendialoge Rev. 1.2, § 3.1/Ä5 — Etappe KD2; der vollständige
        /// Menü-Umbau folgt mit KD4/KD6).
        ///
        /// Bewusst programmatisch, damit Designer und .resx unberührt bleiben; der
        /// Anzeigetext kommt aus MyResource und ist damit zweisprachig.
        /// </summary>
        private void InitKostenvorlagenMenue()
        {
            try
            {
                string text = null;
                try { text = MyResource.Resource.ResourceManager.GetString("KDLG_MENUE_VORLAGEN"); }
                catch { }
                if (string.IsNullOrEmpty(text)) text = "Kostenverwaltung …";

                ToolStripMenuItem eintrag = new ToolStripMenuItem(text);
                eintrag.Name = "MenuItem_Kostenvorlagen";
                eintrag.Click += (s, e) => KostenKomponenteHuelle.Oeffnen(this);

                MenuItem_KostenVerwaltung.DropDownItems.Add(eintrag);

                // KD4 (§ 3.1): Energieträgerverwaltung im Admin-Kontext (Katalog).
                string textEt = null;
                try { textEt = MyResource.Resource.ResourceManager.GetString("KDLG_MENUE_ENERGIETRAEGER"); }
                catch { }
                if (string.IsNullOrEmpty(textEt)) textEt = "Energieträgerverwaltung…";

                ToolStripMenuItem eintragEt = new ToolStripMenuItem(textEt);
                eintragEt.Name = "MenuItem_Energietraeger";
                eintragEt.Click += (s, e) => EnergietraegerHuelle.Oeffnen(this, 0);
                MenuItem_KostenVerwaltung.DropDownItems.Add(eintragEt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Menü der Kostenvorlagen konnte nicht eingebunden werden: " + ex.Message);
            }
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
                eintrag.Click += (s, e) => GesetzeskatalogHuelle.Oeffnen(this);
                eintrag.Image = Properties.Resources.gesetzliche_parameter_32;
                eintrag.ImageScaling = ToolStripItemImageScaling.None;

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
                eintrag.Click += (s, e) => KatalogDublettenHuelle.Oeffnen(this);

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
                ToolStripMenuItem eintrag = new ToolStripMenuItem(MenuetextAssistent());
                eintrag.ShortcutKeys = Keys.F1;
                eintrag.ShowShortcutKeys = true;
                eintrag.Click += (s, e) => KiChatHuelle.Oeffnen(this);

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

                    // Abschalter der Installation: Er blendet den Eintrag NICHT mehr aus,
                    // sondern benennt ihn um (Hilfe-Betrieb, Fachkonzept 11.9, Paket F5).
                    // Grund: Das Fenster arbeitet ohne den Dienst als reine Hilfesuche
                    // weiter - ein verschwundener Menüeintrag hätte den Anwendern also die
                    // Hilfe genommen, obwohl sie lokal vorliegt und nichts kostet.
                    //
                    // Ausgewertet wird weiterhin beim Aufklappen und nicht einmalig beim
                    // Start - die Einstellung kann im laufenden Programm über die
                    // Administration umgelegt werden (Form_AdminSettings).
                    hilfeMenu.DropDownOpening += (s, e) =>
                        eintrag.Text = MenuetextAssistent();
                }

                // F1 auch unabhängig vom Menü verfügbar machen
                this.KeyPreview = true;
                this.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.F1)
                    {
                        e.Handled = true;
                        KiChatHuelle.Oeffnen(this);
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("KI-Hilfe konnte nicht eingebunden werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Beschriftung des Assistenten-Menüeintrags: im Regelbetrieb mit KI-Zusatz,
        /// im Hilfe-Betrieb ohne ihn (Fachkonzept 11.9, Umsetzungspaket F5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum aus MyResource und nicht mehr fest im Code.</b> Der Eintrag bleibt in
        /// beiden Betriebsarten sichtbar, also müssen auch beide Beschriftungen übersetzt
        /// werden können. Sie stehen deshalb zweisprachig in <c>MyResource</c>
        /// (KI_MENUE_ASSISTENT und KI_MENUE_ASSISTENT_HILFE).
        /// </para>
        /// <para>
        /// <b>Warum eine gemeinsame Stelle für Anlegen und Aufklappen.</b> So trägt der
        /// Eintrag die richtige Beschriftung schon vor dem ersten Aufklappen - das zählt,
        /// wenn kein Menüband gefunden wurde und nur das Tastenkürzel F1 bleibt.
        /// </para>
        /// <para>
        /// <b>Keine Schutzwirkung.</b> Dass im Hilfe-Betrieb nichts an den Dienst
        /// hinausgeht, trägt <c>KiEinwilligung.Sicherstellen</c> und der Riegel; die
        /// Beschriftung ist eine reine Darstellungsfrage.
        /// </para>
        /// </remarks>
        private static string MenuetextAssistent()
        {
            return KiEinwilligung.Abgeschaltet
                       ? MyResource.Resource.KI_MENUE_ASSISTENT_HILFE
                       : MyResource.Resource.KI_MENUE_ASSISTENT;
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
                    Dienste.Sprache.IstEnglisch ? "License…" : "Lizenz…");
                // iU9-W15c.5: Die Lizenzverwaltung ist eine Razor-Komponente
                // (EPOS.UI/Dialoge/Lizenz/LizenzVerwaltungDialog.razor); hier steht
                // nur noch der Aufruf ihrer Windows-Huelle.
                eintrag.Click += (s, e) => LizenzVerwaltungHuelle.Oeffnen(this);
                eintrag.Image = Properties.Resources.lizenzen_32;
                eintrag.ImageScaling = ToolStripItemImageScaling.None;
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

        /// <summary>
        /// Die Datenseite der Startseite (iU9-W16b.3) — die Nachfolge von
        /// <c>Program.startfrm</c>.
        /// </summary>
        private StartseiteHuelle _startseite;

        /// <summary>Die WebView, die die Startseite trägt.</summary>
        private BlazorSeite<EPOS.UI.Seiten.Start.Startseite> _startbild;

        private void MenuItem_Neu_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();

            // Befund 3: Der Menueweg hat die Startseite bisher gar nicht aktualisiert -
            // Reiter blieben gesperrt und m_ID_Projekt zeigte weiter auf das zuvor
            // geoeffnete Projekt, die Wizard-Kacheln schrieben also ins falsche Projekt.
            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
            {
                Program.projektkontext?.Setzen(Program.wizardctrl.Projektname);
            }
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
                // Await bleibt bewusst weg: der WordPress-Zugriff läuft im
                // Hintergrund weiter, der Start blockiert nicht.
                //
                // Der frühere Startwettlauf ist damit entschärft, ohne den Start zu
                // bremsen (Konzept Hilfesystem, H3):
                //   1. Program.Main hat den Katalog bereits mit dem Startbestand
                //      belegt — kein Formular sieht mehr einen leeren Katalog.
                //   2. HilfeAutomatik zieht alle bereits geöffneten Formulare nach,
                //      sobald dieser Ladelauf durch ist (HelpCatalog.Loaded).
                // LoadAllAsync fängt jeden Fehler selbst ab und läuft nie in eine
                // unbeobachtete Ausnahme; die Zuweisung an _ macht das sichtbar.
                _ = Program.HelpCatalog.LoadAllAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Laden der Doku: " + ex.Message);
            }
            label_OnlineDoku.Visible = false;

            // iU9-W16b.3: Die Startseite ist eine RAZOR-SEITE. Bis hierher hing hier
            // eine eingebettete Form_Start (TopLevel = false, Dock = Fill, "wie ein
            // UserControl"); jetzt ist es eine BlazorSeite<Startseite> - und die IST
            // ein UserControl, der Kunstgriff entfaellt.
            _startseite = new StartseiteHuelle(() => this, Program.projektkontext);
            _startbild = new BlazorSeite<EPOS.UI.Seiten.Start.Startseite>(
                new Dictionary<string, object>(_startseite.Gaben()));
            this.Controls.Add(_startbild);
            _startbild.BringToFront();

            BaueVariantenMenue();
        }

        // ============================================================
        //  Menü „Projekte": Einträge „Als Variante speichern…" und
        //  „Varianten und Bericht…".
        //
        //  Programmatisch angehängt, damit MDIMainForm.Designer.cs und die
        //  Satelliten-.resx unberührt bleiben (CLAUDE.md: Designer-Dateien
        //  nicht von Hand editieren) — dasselbe Vorgehen wie bei
        //  Form_Start.BaueBerichteKostenSeite. Die Beschriftungen kommen
        //  deshalb aus MyResource und nicht aus der Formular-Ressource;
        //  ein Sprachwechsel startet das Programm ohnehin neu.
        // ============================================================

        private void BaueVariantenMenue()
        {
            if (this.DesignMode || Projekte == null) return;

            Projekte.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem alsVariante = new ToolStripMenuItem(MyResource.Resource.MENU_VARIANTE_SPEICHERN);
            alsVariante.Name = "MenuItem_AlsVariante";
            alsVariante.Click += new EventHandler(this.MenuItem_AlsVariante_Click);
            Projekte.DropDownItems.Add(alsVariante);

            ToolStripMenuItem variantenBericht = new ToolStripMenuItem(MyResource.Resource.MENU_VARIANTEN_BERICHT);
            variantenBericht.Name = "MenuItem_VariantenBericht";
            variantenBericht.Click += new EventHandler(this.MenuItem_VariantenBericht_Click);
            Projekte.DropDownItems.Add(variantenBericht);
        }

        private void MenuItem_AlsVariante_Click(object sender, EventArgs e)
        {
            // iU9-W2.1: Der Ablauf liegt in AlsVarianteHuelle; die Namensabfrage
            // stellt die Razor-Komponente NamensDialog (Form_AlsVariante geloescht).
            // iU9-W16b.3: Das offene Projekt kommt aus dem Kern statt aus der Maske.
            AlsVarianteHuelle.Zeige(this, Dienste.Projekt.Id, Dienste.Projekt.Name);
        }

        private void MenuItem_VariantenBericht_Click(object sender, EventArgs e)
        {
            StartseiteHuelle.Aktuelle?.ZeigeBerichteKosten(
                EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_UEBERSICHT);
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
            // iU9-W14c.7: Mit Besitzer - der Vorlaeufer rief ShowDialog() ohne "this"
            // und ohne using; das Fenster erschien nicht ueber dem Hauptfenster und
            // wurde nie entsorgt (Befund W14c-B34). Der MENUETEXT bleibt "Klimadaten",
            // und die Komponente heisst seit dem Entscheid E-3 (04.09.2026) wieder
            // genauso: KLIMAREGION sind die deutschen Regionen der Klimazonenkarte,
            // KLIMADATEN der weltweite TMY-Download aus PVGIS.
            KlimadatenHuelle.Oeffnen(this);
        }

        private void MenuItem_ProjektBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();

            // Befund 3: wie bei "Neu" - ohne diese Zeile bleibt der Projektkontext der
            // Startseite auf dem vorher geoeffneten Projekt stehen.
            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
                Program.projektkontext?.Setzen(Program.wizardctrl.Projektname);
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
            // Lizenzvereinbarung und rechtliche Hinweise anzeigen. Seit iU9-W15c.11
            // ist das die Razor-Komponente LizenzDialog; die Huelle sucht den
            // Vertragstext ueber LizenzTextCtrl (Datei, Zwischenspeicher oder die
            // Online-Fassung von epos-plan.de).
            LizenzHuelle.Anzeigen(this);
        }

        /// <summary>
        /// Stellt die Oberflaeche auf Deutsch um.
        ///
        /// <para>Wirksam wird das erst beim Neustart - die Textressourcen bereits
        /// geoeffneter Masken wechseln nicht mehr. Das ist unveraendert der Bestand;
        /// neu ist nur, dass der Registry-Wert ueber <c>Dienste.Sprache</c> geschrieben
        /// wird statt hier von Hand.</para>
        /// </summary>
        private void Deutsch_Click(object sender, EventArgs e)
        {
            if (!Dienste.Sprache.IstEnglisch) return;

            Dienste.Sprache.Setzen("de");
            Application.Restart();
        }

        /// <summary>Stellt die Oberflaeche auf Englisch um; siehe <see cref="Deutsch_Click"/>.</summary>
        private void Englisch_Click(object sender, EventArgs e)
        {
            if (Dienste.Sprache.IstEnglisch) return;

            Dienste.Sprache.Setzen("en");
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

        /// <summary>
        /// Der Modulkatalog der Photovoltaik.
        /// </summary>
        /// <remarks>
        /// iU9-W14a.0h (Befund W14-B36): Bis hierher stand hier
        /// <c>new Form_AdminPV(); frm.ShowDialog();</c> - der EINZIGE der elf
        /// Katalogmenuepunkte, der die Maske selbst anlegte statt ueber
        /// <see cref="MenueCtrl"/> zu gehen. Damit war <c>MenueCtrl.PV()</c> ohne
        /// Aufrufer, und mit ihm die ganze Kette <c>Masken.PvAdmin</c> ->
        /// <c>WinFormsNavigation</c> unerreichbar: drei tote Stellen. Der Weg ist
        /// jetzt derselbe wie bei den zehn anderen.
        /// </remarks>
        private void MenuItem_PV_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PV();
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

        // iU9-W13.0k: Beide Menuepunkte oeffnen dieselbe Razor-Komponente
        // (PvModulImportDialog), jetzt aber im richtigen Zustand: Das Argument
        // sagt, mit welcher Quelle sie aufmacht. Bis dahin brachte "PAN laden"
        // die Maske NICHT in den PAN-Modus (Befund W13-B51), und beide gingen an
        // der Navigation vorbei (B55).
        private void MenuItem_PV_Import_CEC_Click(object sender, EventArgs e)
        {
            Dienste.Navigation.OeffneMaske(Masken.PvImport, "CEC");
        }

        private void MenuItem_PV_Import_PAN_Click(object sender, EventArgs e)
        {
            Dienste.Navigation.OeffneMaske(Masken.PvImport, "PAN");
        }

        private void MenuItem_Einstellungen_Click(object sender, EventArgs e)
        {
            // iU9-W14c.6: Mit Besitzer - der Vorlaeufer rief ShowDialog() ohne "this"
            // und ohne using (Befund W14c-B34). Der Menuepunkt selbst bleibt: Er ist
            // der Anker, an dem InitGesetzeMenue, InitDublettenMenue und
            // InitLizenzMenue ihre Eintraege einhaengen (Befund W14c-B63).
            EinstellungenHuelle.Oeffnen(this);
        }

        /// <summary>
        /// Online-Dokumentation im Wiki (A4). Reiner Not-Fallback — führend ist
        /// der Einstellwert, den auch der Hilfekatalog verwendet (A2).
        /// </summary>
        public const string DOKU_URL = Program.WIKI_STANDARD;

        private void MenuItem_Dokumentation_Click(object sender, EventArgs e)
        {
            try
            {
                // Standard ist die Online-Dokumentation im Wiki; ist in den
                // Einstellungen eine abweichende (z. B. lokale) Adresse
                // hinterlegt, hat diese Vorrang.
                string _targetUrl = Properties.Settings.Default.WordPressUrl;
                if (string.IsNullOrWhiteSpace(_targetUrl) ||
                    _targetUrl.Contains("localhost"))
                {
                    _targetUrl = DOKU_URL;
                }

                // A6 / Entscheid 7.1a: englische Oberfläche über den
                // Übersetzungs-Proxy; deutsche Oberfläche und fremde Hosts
                // unverändert.
                _targetUrl = DokuUebersetzung.FuerAnzeige(_targetUrl);

                Process.Start(new ProcessStartInfo { FileName = _targetUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Öffnen des Links: " + ex.Message);
            }
        }

        /// <summary>
        /// Der Export-/Import-Dialog. Der Rumpf stand bis iU4-2 als
        /// <c>ProjektDuplizierenCtrl.ZeigeExportImportDialog</c> im Controller und war
        /// dessen einzige WinForms-Kante - bei genau diesem einen Aufrufer. Er steht
        /// deshalb jetzt hier, wo das Fenster ohnehin zu Hause ist.
        ///
        /// <para>iU9-W15a.5: Das Fenster ist die Razor-Komponente
        /// <c>ProjektTransferDialog</c>; die Huelle zeigt sie modal. Der Rueckgabewert
        /// sagt jetzt ehrlich, ob ein Import gelungen ist - der Vorlaeufer wertete das
        /// <c>DialogResult</c> gar nicht aus (Befund W15a-B37).</para>
        /// </summary>
        private void MenuItem_ExportImport_Click(object sender, EventArgs e)
        {
            ProjektTransferHuelle.Oeffnen(this);
        }
    }
}

