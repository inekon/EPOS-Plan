using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    static class Program
    {
        /// <summary>
        /// Das Anwendungsfenster, das <c>Application.Run</c> traegt.
        /// </summary>
        /// <remarks>
        /// Anwenderentscheid E-10 (04.09.2026): Klasse und Feld heissen seither
        /// <c>Hauptfensterrahmen</c> bzw. <c>rahmen</c> - vorher
        /// <c>MDIMainForm</c> bzw. <c>mdifrm</c>, obwohl es seit jeher kein MDI
        /// gibt (Befund W16-B10). Ausserhalb dieser Klasse liest das Feld
        /// niemand; die Umbenennung hat deshalb keinen zweiten Aufrufer.
        /// </remarks>
        public static Hauptfensterrahmen rahmen = null;

        // iU9-W16b.1 (Anwenderentscheid E-7, K6-a): Das Feld "mainfrm" ist ersatzlos
        // entfallen - das Detailformular FormMain ("Konfiguration Projekt") mit seinen
        // zwoelf Gewerkslisten und elf Kontextmenues ist geloescht. Was es zeigte, fuehrt
        // die Startseite als Kacheln.
        //
        // iU9-W16b.3: Ebenso "startfrm". Die Startseite ist eine Razor-Seite
        // (EPOS.UI/Seiten/Start/Startseite.razor) in einer BlazorSeite<>; wer sie
        // erreichen will, geht ueber StartseiteHuelle.Aktuelle, und wer das offene
        // Projekt braucht, ueber Dienste.Projekt (ProjektKontextCtrl, K2).
        public static ProjektKontextCtrl projektkontext = null;
        public static MenueCtrl menuectrl = null;
        public static WizardCtrl wizardctrl = null;
        /// <summary>
        /// <c>C:\ProgramData\WP-Plan</c> — seit iU5 nur noch eine Weiterleitung auf
        /// <c>Dienste.Pfade.Gemeinsam</c>. Die Masken lesen unveraendert weiter; wer neu
        /// schreibt, nimmt den Dienst.
        /// </summary>
        public static string ApplicationPath_Common
        {
            get { return Dienste.Pfade.Gemeinsam; }
        }

        /// <summary>
        /// <c>LocalApplicationData\WP-Plan</c> — Weiterleitung auf
        /// <c>Dienste.Pfade.BenutzerLokal</c>, siehe <see cref="ApplicationPath_Common"/>.
        /// </summary>
        public static string ApplicationPath_User
        {
            get { return Dienste.Pfade.BenutzerLokal; }
        }
        /// <summary>
        /// 0=de, 1=en — der Wert liegt seit iU4-1 in <see cref="Sprache.Nummer"/>,
        /// damit Kern-Code (Berichtstexte) ihn ohne <c>Program</c> lesen kann. Diese
        /// Weiterleitung bleibt, damit die vorhandenen Leser und die eine Setzstelle
        /// aus der Registry unverändert weiterlaufen.
        /// </summary>
        public static int nLanguage
        {
            get { return Sprache.Nummer; }
            set { Sprache.Nummer = value; }
        }

        /// <summary>
        /// Not-Rückfall für die Basis-URL der Wiki-Dokumentation, falls der
        /// Einstellwert <c>WordPressUrl</c> leer ist (A2). Seit iU5 nur noch eine
        /// Weiterleitung auf <see cref="WikiWissen.WIKI_STANDARD"/>, damit Kern-Code
        /// den Rückfall ohne <c>Program</c> erreicht.
        /// </summary>
        public const string WIKI_STANDARD = WikiWissen.WIKI_STANDARD;

        /// <summary>
        /// Der globale Hilfekatalog, auf den alle Formulare zugreifen — seit iU5 nur
        /// noch eine Weiterleitung auf <see cref="WikiHelpCatalog.Aktueller"/>, damit
        /// Kern-naher Programmtext ihn ohne <c>Program</c> erreicht.
        /// </summary>
        public static WikiHelpCatalog HelpCatalog
        {
            get { return WikiHelpCatalog.Aktueller; }
        }

        /// <summary>
        /// Der anwendungsweite Infobutton-Extender (Konzept Hilfesystem, F5).
        /// EINE Instanz für das ganze Programm — bisher erzeugte jedes Formular
        /// eine eigene. <see cref="HilfeAutomatik"/> erfasst darüber jedes geöffnete
        /// Formular und jedes nachgeladene UserControl von selbst; kein Formular
        /// braucht dafür noch eigenen Programmtext.
        /// </summary>
        public static HelpExtender HelpExtender { get; private set; }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        // HIER STAND DER SCHALTER "/ki-feldsicherung-aus" (Fachkonzept 11.5, Abnahme
        // 20.08.2026). Er war der einzige Weg, die Feldbestaetigung der Formularaktionen
        // aufzuheben - gedacht als Entwicklerkanal fuer Pruefläufe. Der Auftraggeber hat
        // ihn am 14.09.2026 abbestellt: "eine Bestaetigung was gesetzt wird sollte immer
        // erscheinen". Mit ihm sind KiFeldsicherung und die zwei Auswertungsmethoden
        // entfallen; der Aktionsharnisch prueft seither keine Argumentlisten mehr.

        [STAThread]
        static void Main()
        {
            EinstellungenUebernehmen();

            SpeicherFlottenProjektCtrl.PlanerFactory = () => new SpeicherPlanung.OrToolsFlottenPlaner();

            // DIE DIENSTE VOR ALLEM ANDEREN (Umsetzungskonzept iU5).
            //
            // Kern-Code - Zugriffsschicht, Controller, Modelle - spricht die Umgebung
            // ueber neun kleine Schnittstellen an (Dienste.Dialog, .Datei, .Pfade,
            // .Einstellungen, .Lizenzablage, .GeraeteId, .Sprache, .Navigation,
            // .Projekt). Ohne Oberflaeche gilt die Vorbelegung des Kerns; hier werden
            // die Windows-Fassungen eingelegt.
            //
            // WARUM AN DIESER STELLE. Vor jedem Programmtext, der eine Meldung absetzen
            // koennte - insbesondere vor DataRepository.DatenbankVorhanden() weiter
            // unten. Stuende die Belegung darunter, ginge genau die erste Meldung eines
            // Startfehlers auf die Konsole statt in einen Dialog.
            //
            // MELDUNG.* WIRD NICHT MEHR BELEGT. Die vier Melde-Haken zeigen seit iU5
            // selbst auf Dienste.Dialog (siehe Meldung.cs); eine Belegung hier waere
            // eine zweite Wahrheit. Ein Nebeneffekt ist beabsichtigt: Meldung.Hinweis
            // traegt damit wieder das Informationssymbol, das die Hinweisdialoge des
            // Kerns bis iU3-2 hatten.
            WindowsSprache sprache = new WindowsSprache();

            Dienste.Dialog = new WindowsDialogDienst();
            Dienste.Datei = new WindowsDateiDienst();
            Dienste.Pfade = new WindowsPfade();
            Dienste.Einstellungen = new SettingsEinstellungen();
            Dienste.Lizenzablage = new DpapiLizenzAblage();
            Dienste.GeraeteId = new WindowsGeraeteId();
            Dienste.Sprache = sprache;
            Dienste.Navigation = new WinFormsNavigation();

            // iU9-W16b.3 (K2): Der Projektkontext liegt im KERN. Bis hierher stand
            // hier FormStartProjektKontext - eine Fassade auf Program.startfrm, also
            // auf ein FELD der Startmaske (Befund W16-B6).
            projektkontext = new ProjektKontextCtrl();
            Dienste.Projekt = projektkontext;

            // Die Farben der Diagramme sind eine Anwendungseinstellung (Rubrik
            // "Diagramme"): Farbpalette.Aktuell wird EINMAL beim Start daraus gespeist,
            // danach nur noch beim Speichern der Einstellungen. Ohne Eintrag bleiben es
            // die Hausfarben. Erst NACH Dienste.Einstellungen, denn von dort liest sie.
            Zeichnung.Diagrammfarben.Uebernehmen();

            // DPI: PER MONITOR V2 (iU9-W16c.4, Anwenderentscheid E-6 / iF21).
            //
            // Bis hierher stand hier HighDpiMode.DpiUnaware, passend zum
            // app.manifest: Windows skalierte jedes Fenster als Bitmap, und bei
            // 125-200 % war die Oberflaeche sichtbar unscharf. Der Grund dafuer
            // waren die fest gerechneten Pixelkoordinaten der gewachsenen
            // WinForms-Masken - und die gibt es seit Welle 16 nicht mehr: Die
            // Oberflaeche ist eine Razor-Seite in einer WebView, es bleibt genau
            // eine Designer-Maske (Form_HelpPopup) und die DPI-freie Huelle.
            //
            // Fuehrend ist das MANIFEST (dpiAware true/pm + dpiAwareness
            // PerMonitorV2); dieser Aufruf haelt den verwalteten Zustand
            // deckungsgleich - stuende hier weiter DpiUnaware, waeren es zwei
            // Wahrheiten.
            if (Environment.OSVersion.Version.Major >= 10)
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Die zuletzt eingestellte Oberflaechensprache uebernehmen: Registry-Wert
            // Language lesen, Sprache.Nummer setzen, Anzeigekultur setzen. Der Weg
            // dorthin liegt seit iU5 in WindowsSprache; nLanguage bleibt die
            // Weiterleitung auf Sprache.Nummer, damit die Masken unveraendert lesen.
            sprache.AusRegistryUebernehmen();

            // WEBVIEW2-RIEGEL (iU9-W15c.6a, Entscheid E-8 Weg 2).
            //
            // Ab dieser Welle laufen ZWEI Startschritte über eine Blazor-Hülle: der
            // Erststart der Datenbank und die Zustimmung zur Lizenzvereinbarung. Beide
            // liefern "false", wenn ihr Fenster leer bleibt, und beide beenden dann das
            // Programm. Ohne WebView2-Laufzeit wäre EPOS-Plan damit nicht mehr nur
            // unbequem (leere Dialoge, iR12), sondern unstartbar — und der Anwender
            // sähe kein Wort dazu (Befund W15c-B10).
            //
            // Deshalb: EINE Prüfung, EINE Meldung mit der Bezugsquelle, dann Ende.
            // Keine WinForms-Rückfallmasken — zwei Fassungen derselben Maske sind
            // ausgeschlossen (Regel M1). NACH der Sprachwahl, damit die Meldung in der
            // eingestellten Sprache kommt; VOR dem ersten besitzerlosen Dialog.
            //
            // Die Meldung ist bewusst eine native MessageBox und kein Dienste.Dialog:
            // Die Windows-Fassung von Dienste.Dialog zeigt zwar ebenfalls eine
            // MessageBox, aber hier soll unmissverständlich sein, dass an dieser Stelle
            // keine Oberfläche mehr angenommen wird.
            if (!WebView2Vorhanden())
            {
                MessageBox.Show(
                    MyResource.Resource.START_WEBVIEW2_FEHLT,
                    MyResource.Resource.START_WEBVIEW2_FEHLT_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Startprüfung (vormals x64-Umstellung P1.3, jetzt DB-Migration SQLite 2.8):
            // Ohne lesbare Datenbankdatei ist jede DB-Operation unmöglich — sprechende
            // Meldung statt später einer nackten Ausnahme tief im Startpfad (erste
            // Fundstelle wäre SchemaMigration.Ausfuehren). NACH der Sprachwahl, damit die
            // Meldung in der eingestellten Sprache kommt, und VOR jedem Datenbankzugriff.
            // Einen registrierungspflichtigen Provider gibt es nach der Umstellung nicht
            // mehr; geprüft wird jetzt die Datei selbst.
            //
            // ERSTBEREITSTELLUNG (Anwenderentscheid #157-E-1, Weg W3 vom 09.09.2026):
            // Auf einem frischen Rechner gibt es die SQLite-Datei beim allerersten Start
            // noch gar nicht - DatenbankVorhanden() prüft aber genau sie. Bevor ihre
            // Fehlermeldung erscheint, wird deshalb die AUSGELIEFERTE VORLAGE
            // ({app}\Vorlage\Kenndaten.sqlite) in den Datenordner kopiert; danach läuft
            // alles Weitere (Lizenz, Schemapflege, Oberfläche) unverändert - insbesondere
            // SchemaMigration.Ausfuehren, das die frisch kopierte Vorlage auf den
            // benötigten Stand hebt, falls sie älter ist.
            //
            // Der Access-Weg (Übernahme-Assistent, ACE-Engine) ist mit W3 GEFALLEN: Access
            // wurde beim Kunden nie produktiv eingesetzt; die Übernahme eines Altbestands
            // war seither ein Hauswerkzeug (EposSqliteMigrator) und ist seit dem 24.09.2026
            // eingestellt.
            if (!DataRepository.DatenbankVorhanden())
            {
                if (!DatenbankBereitstellen()) return;
            }

            // Zustimmung zur Lizenzvereinbarung beim ersten Start (einmal je
            // Windows-Benutzer; Ablage ueber Dienste.Einstellungen und damit
            // unveraendert HKCU\Software\wp-plan\LizenzZugestimmt mit
            // Programmversion und Datum, siehe ZustimmungCtrl.Merken). Seit
            // iU9-W15c.11 zeigt die Huelle dafuer den Razor-Dialog - BESITZERLOS,
            // es gibt noch kein Fenster.
            // NACH der ACE-Prüfung - eine nicht startfähige Installation braucht
            // keine Zustimmung - und VOR der Schema-Migration: Wer ablehnt, dessen
            // Datenbank wird nicht angefasst.
            if (!LizenzHuelle.ZustimmungSicherstellen()) return;

            // Textlieferant des KI-Kerns einhaengen - NACH der Sprachwahl, damit
            // KiKern seine Schluessel in der eingestellten Sprache beantwortet
            // bekommt (Fachkonzept 3.7; KiKern darf MyResource nicht kennen).
            KiTextlieferant.Einrichten();

            // Ausfuehrungsschicht des KI-Assistenten einlegen. Sie LIEGT SEIT AUFTRAG
            // #201 IM KERN (EPOS.Kern\Allgemein\KI\KiAusfuehrung.cs) - damit haben iOS
            // und die Razor-Dialoge dasselbe Register; vorher stand dort die stille
            // Fassung KeineAusfuehrung mit einem leeren. Hier bleiben die zwei Haken, die
            // nur Windows beantworten kann: die Modalitaetsfrage (Form.ActiveForm.Modal)
            // und der Hilfetext eines Katalogfeldes (WikiHelpCatalog).
            KiAusfuehrungWindows.Einlegen();

            // Bedienkontext des Assistenten: Die ZUORDNUNG (Positivliste, Tabellen)
            // liegt seit iU9-W15b.0f im Kern, die ERMITTLUNG des aktiven Fensters
            // bleibt hier - Form.ActiveForm gibt es auf iOS nicht (Befund W15b-B19).
            // Ohne diesen Aufruf bleibt der Bereich "Unbekannter Bereich".
            HilfeKontext.Einhaengen();

            // Auftrag #208: Der eine Weg der plattformfreien Datenseite, der noch in
            // einer Windows-Huelle steckt - der Auslieferungskatalog der
            // Pufferspeicher (KatalogBrowserHuelle oeffnet je nach Weg ein eigenes
            // Fenster). Ohne diesen Haken zeigt die Pufferverwaltung den Knopf
            // "Katalog ansehen" nicht - genau der Stand auf iOS.
            Katalogwege.PufferKatalogGaben = () => PufferSpAdminHuelle.Gaben(true);

            // Berichtsvorlagen BV-E1 (Konzept 10.3): die Wege um eine Vorlagendatei, die nur
            // Windows kennt - "Im Ordner zeigen" (Explorer), "In Word oeffnen" und
            // "Schreibgeschuetzt oeffnen" (Word selbst) und die Wahl des Vorlagenordners. Ohne
            // diesen Haken bietet das Menue "…" der Berichtsseite "Teilen…", und der
            // Vorlagenordner ist fest - genau der Stand auf iOS.
            Berichtsvorlagenwege.Plattform = WindowsBerichtsvorlagenwege.Erzeugen();

            // Berichtsvorlagen BV-E7-6: die Muster im Unterordner "Mitgeliefert" des Vorlagenordners
            // (Standardvorlage, Kurzbericht, Baukasten, Excel-Standardmappe) aktuell halten - im
            // Hintergrund, nur bei geaendertem Inhalt, nur in diesem Unterordner. Was misslingt,
            // benennt der Befund; er geht ins Ablaufprotokoll und haelt den Start nicht auf.
            _ = BerichtsvorlagenCtrl.MusterImHintergrundBereitstellen(befund =>
            {
                if (!befund.Erfolg) Trace.TraceWarning("Berichtsvorlagen, Muster: " + befund.Meldung);
            });

            // Stufe G1 der Gebaeudesimulation (Umsetzungskonzept 2.8, E27/A10): Die
            // Gebaeudehuellen liegen in EPOS.UI.Daten; zwei ihrer Unterdialoge haben die
            // Datenhaelfte noch hier - die Brauchwasser-Profilliste und die
            // Gebaeudetypen-Verwaltung. Ohne diese Haken fehlen ihre Knoepfe (iOS).
            Gebaeudewege.BrauchwasserGaben = (projektId, zeilen, geaendert, zapfprofil) =>
                BedarfsProfileHuelle.Gaben(null, BedarfsArt.Brauchwasser, projektId,
                                           zeilen, geaendert, wizard: false, zapfprofil);
            Gebaeudewege.GebaeudetypGaben = () => GebaeudetypHuelle.Gaben();

            // ETAPPE E3, SCHRITTE 5 UND 6: Die Uebergangsnaht Wirtschaftlichkeitswege
            // ist WEG. Alle Huellen der Kosten- und Wirtschaftlichkeitsseite liegen
            // jetzt in EPOS.UI.Daten und rufen einander unmittelbar; ihre Dialoge
            // erscheinen als Ueberlagerung im selben Fenster. Windows steuert nur noch
            // zwei Fenster-Adapter bei (KostenKomponenteFenster fuer den Menuepunkt
            // "Kostenverwaltung", GesetzeskatalogFenster fuer "Gesetzeskatalog"), und
            // die ruft die Hauptfensterhuelle unmittelbar - kein Haken noetig.

            // Rechtshinweis des KI-Assistenten einhaengen: erst damit gibt es ueberhaupt
            // einen Weg zu einer Einwilligung. Ohne diesen Aufruf - Aktionsharnisch,
            // Tests, Konsolenlauf - wird keine Anfrage an den Anbieter gesendet.
            KiHinweisHuelle.Einhaengen();

            // -----------------------------------------------------------------------
            // Schema-Ausrollung (ADR-001): die versionierte Migration laeuft genau
            // einmal je Programmstart, VOR dem Oeffnen der MDI-Oberflaeche.
            //
            // Bei Fehlschlag gibt es GENAU EINE Meldung; das Programm startet trotzdem
            // (Kataloge, Projekte und Berichte bleiben nutzbar), aber der
            // Simulationsbereich verweigert den Start - siehe
            // SchemaMigration.SimulationGesperrt().
            // -----------------------------------------------------------------------
            string migrationsBericht;
            if (!SchemaMigration.Ausfuehren(out migrationsBericht))
            {
                MessageBox.Show(
                    "Die Datenbank konnte nicht auf den benötigten Stand gebracht werden." +
                    Environment.NewLine + Environment.NewLine +
                    SchemaMigration.FehlerKopf() +
                    Environment.NewLine + Environment.NewLine +
                    "Das Programm startet trotzdem, der Simulationsbereich bleibt jedoch gesperrt." +
                    Environment.NewLine +
                    "Ausführliches Protokoll: " + SchemaMigration.ProtokollPfad(),
                    "Datenbank-Aktualisierung",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            menuectrl = new MenueCtrl();
            wizardctrl = new WizardCtrl();

            // Anmeldung beim eigenen Halter (iU5): Programmtext ausserhalb der Masken
            // erreicht den Assistenten-Controller ueber WizardCtrl.Aktueller und nicht
            // mehr ueber Program.wizardctrl.
            WizardCtrl.Aktueller = wizardctrl;

            // Katalog-Objekt einmalig erstellen
            //
            // A2 (H1): Die Basis-URL kommt wieder aus den Einstellungen. Damit
            // steuert EIN Wert (Admin-Dialog, Feld "Online-Dokumentation")
            // sowohl den Hilfekatalog als auch den Menüpunkt Dokumentation.
            // Der Settings-Schlüssel heißt aus Kompatibilitätsgründen weiterhin
            // "WordPressUrl" — eine Umbenennung würde gespeicherte Anwenderwerte
            // in der user.config verwerfen (Entscheid 7.3 des Konzepts).
            // WIKI_STANDARD greift nur, wenn der Einstellwert leer ist.
            string dokuBasis = Dienste.Einstellungen.Lies(WikiWissen.EINSTELLUNG_BASIS);
            if (string.IsNullOrWhiteSpace(dokuBasis)) dokuBasis = WIKI_STANDARD;

            WikiHelpCatalog.Aktueller = new WikiHelpCatalog(dokuBasis);

            // F6 / Startwettlauf: Der Katalog wird SOFORT belegt — aus der lokalen
            // Sicherung, sonst aus dem mitgelieferten Startbestand.
            // Hauptfensterrahmen.BeimLaden
            // stößt den Onlineabruf danach bewusst ohne await an; ohne diese
            // Vorbelegung sähe jedes Formular, das früher öffnet, einen leeren
            // Katalog. Rangfolge insgesamt: Online > AppData-Sicherung > Beilage.
            HelpCatalog.StartbestandLaden();

            // F5: EIN anwendungsweiter Extender, und eine Automatik, die jedes
            // geöffnete Formular und jedes nachgeladene UserControl selbst erfasst.
            // Ab hier ist help_mapping.txt die einzige Stelle, an der Hilfe
            // gepflegt wird.
            HelpExtender = HilfeAutomatik.Starten(HelpCatalog);

            rahmen = new Hauptfensterrahmen();
            Application.Run(rahmen);

            Application.Exit();
        }

        /// <summary>
        /// DIE BENUTZEREINSTELLUNGEN DER VORHERIGEN PROGRAMMVERSION UEBERNEHMEN —
        /// genau EINMAL je Fassung, beim ersten Start (16.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum es diesen Aufruf braucht.</b> Die neun Einstellungen in
        /// <c>Properties.Settings</c> (Datenbankpfad und -name, Import-/Exportpfade,
        /// VDI-3805-Pfad, PVGIS- und Geokodierungs-URL, Wiki-Adresse) haben alle den
        /// Gueltigkeitsbereich <b>User</b>. .NET legt sie in einer
        /// <c>user.config</c> ab, deren Pfad die ASSEMBLYVERSION enthaelt — bei
        /// 1.2.0.1 ein anderer Ordner als bei 1.2.0.2. Ohne diesen Aufruf faende
        /// das Programm nach jedem Update eine leere Ablage vor und fiele auf die
        /// Vorgaben zurueck: Der Anwender haette seinen eingestellten
        /// Datenbankpfad verloren, und das ist kein kosmetischer Verlust.</para>
        ///
        /// <para><b>Warum ein eigener Schalter und keine Abfrage auf „leer".</b>
        /// <c>Upgrade()</c> ist nicht kostenlos (es sucht die Ablagen aelterer
        /// Fassungen ab) und darf eine SPAETERE Aenderung des Anwenders nicht wieder
        /// ueberschreiben. Der Schalter <c>EinstellungenUebernommen</c> steht selbst
        /// in derselben user.config: In einer frischen Ablage ist er
        /// <c>false</c> — also genau dann, wenn die Uebernahme ansteht —, und nach
        /// dem <c>Save()</c> ist er es nie wieder. Ein leerer Pfad taugte nicht als
        /// Kennzeichen: Er kann auch die bewusste Wahl des Anwenders sein.</para>
        ///
        /// <para><b>Vor allem anderen</b>, denn <c>DataRepository</c> liest
        /// <c>DBPath</c> und <c>DBName</c> beim ersten Datenbankzugriff. Ein
        /// Fehlschlag wird geschluckt: Der Verlust der Einstellungen ist aergerlich,
        /// ein Programm, das deshalb gar nicht startet, waere schlimmer.</para>
        /// </remarks>
        private static void EinstellungenUebernehmen()
        {
            try
            {
                if (Properties.Settings.Default.EinstellungenUebernommen) return;

                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.EinstellungenUebernommen = true;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Einstellungen konnten nicht uebernommen werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Ist die Microsoft-Edge-WebView2-Laufzeit auf diesem Rechner installiert?
        /// </summary>
        /// <remarks>
        /// <para>Gefragt wird die Laufzeit selbst, nicht die Registry:
        /// <c>CoreWebView2Environment.GetAvailableBrowserVersionString()</c> liefert
        /// die Fassung der Laufzeit, die eine <c>WebView2</c> in diesem Prozess
        /// tatsächlich benutzen würde — einschließlich einer mitgelieferten
        /// „Fixed Version". Das Setup prüft dieselbe Sache über zwei
        /// Registry-Schlüssel (<c>WebView2Vorhanden</c>,
        /// <c>Setup/EPOS-Plan.iss:444</c>); dort gibt es keinen Prozess, der fragen
        /// könnte.</para>
        /// <para>Fehlt die Laufzeit, wirft der Aufruf eine
        /// <c>WebView2RuntimeNotFoundException</c>; jeder andere Fehlschlag (etwa eine
        /// nicht ladbare <c>WebView2Loader.dll</c>) ist für den Anwender dieselbe Lage.
        /// Deshalb wird breit gefangen — das Programm soll hier melden, nicht
        /// abstürzen.</para>
        /// </remarks>
        private static bool WebView2Vorhanden()
        {
            try
            {
                string fassung = Microsoft.Web.WebView2.Core.CoreWebView2Environment
                                          .GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(fassung);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Die Erstbereitstellung der Datenbank (Anwenderentscheid <b>#157‑E‑1 / W3</b>,
        /// 09.09.2026): Es gibt keine lesbare SQLite-Datei — dann wird die ausgelieferte
        /// Vorlage in den Datenordner kopiert.
        /// </summary>
        /// <returns><c>true</c> = weiterstarten, <c>false</c> = Programm beenden.</returns>
        /// <remarks>
        /// <para>
        /// <b>Der Access-Weg ist mit W3 gefallen.</b> Bis dahin stand hier der
        /// Übernahme-Assistent aus Arbeitspaket S8: Lag im Datenordner eine
        /// <c>Kenndaten.accdb</c>, wurde sie einmalig nach SQLite umgestellt. Access wurde
        /// beim Kunden nie produktiv eingesetzt; die Übernahme eines Altbestands war
        /// seither ein <b>Hauswerkzeug</b> (<c>EposSqliteMigrator</c>) und ist seit dem
        /// 24.09.2026 eingestellt.
        /// </para>
        /// <para>
        /// <b>Der Kern entscheidet, die Hülle meldet.</b> Kopieren, Prüfen und Aufräumen
        /// stehen in <see cref="Erstbereitstellung"/> — plattformfrei, damit auf iOS
        /// derselbe Gedanke gilt (dort kopiert
        /// <c>EPOS.iOS/Datenbankbereitstellung</c> aus dem Anwendungspaket). Hier bleibt
        /// nur die Meldung.
        /// </para>
        /// <para>
        /// Nach dem Kopieren wird die Startprüfung WIEDERHOLT — erst ein zweites
        /// <c>DatenbankVorhanden()</c> beweist, dass die neue Datei auch wirklich zu öffnen
        /// ist. Nur dann geht es weiter; die Schemapflege
        /// (<c>SchemaMigration.Ausfuehren</c>) hebt eine ältere Vorlage unmittelbar danach
        /// auf den benötigten Stand.
        /// </para>
        /// </remarks>
        private static bool DatenbankBereitstellen()
        {
            string ziel = DataRepository.GetDBPath();
            string vorlage = Dienste.Pfade.Auslieferungsvorlage;

            Erstbereitstellungsergebnis ergebnis = Erstbereitstellung.Sicherstellen(ziel, vorlage);

            // Kopiert (oder wider Erwarten doch schon da): die Datei muss sich jetzt
            // oeffnen lassen, sonst war die Bereitstellung wertlos.
            if (ergebnis.Bereit && DataRepository.DatenbankVorhanden()) return true;

            // Ab hier startet das Programm nicht. Die Meldung nennt IMMER beide Orte -
            // wo die Datenbank erwartet wurde und wo die Vorlage gesucht wurde.
            string text = string.Format(MyResource.Resource.START_DB_FEHLT, ziel) +
                          Environment.NewLine + Environment.NewLine +
                          string.Format(MyResource.Resource.START_VORLAGE_FEHLT, vorlage);

            if (ergebnis.Lage != Erstbereitstellungslage.VorlageFehlt &&
                !string.IsNullOrEmpty(ergebnis.Meldung))
            {
                text += Environment.NewLine + Environment.NewLine + ergebnis.Meldung;
            }

            MessageBox.Show(text,
                            MyResource.Resource.START_DB_FEHLT_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        public static bool HasValue(this double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        // ------------------------------------------------------------------
        // Sperre gegen die Endlosschleife aus Prüfmeldung und Undo()
        //
        // Fast alle Aufrufer von checkInt nehmen eine ungültige
        // Eingabe nach 'false' mit TextBox.Undo() zurück. Undo() löst TextChanged
        // erneut aus, und weil die Win32-Edit-Box mit EM_UNDO zwischen Rückgängig
        // und Wiederherstellen umschaltet, pendelt der Text zwischen der
        // Fehleingabe und dem vorherigen Stand. War der vorherige Stand leer
        // (also ebenfalls keine Zahl), meldete die Prüfung endlos weiter: Die
        // Meldung kam nach jedem OK sofort zurück, der Dialog war gefangen
        // (Befund „Brauchwasser Verwaltung / Ändern des Jahresverbrauchs").
        //
        // Nach einer gezeigten Meldung wird deshalb für dasselbe Eingabefeld bis
        // zum Ende der laufenden Nachrichtenverarbeitung nicht erneut gemeldet;
        // der verschachtelte Aufruf liefert 'true' und lässt den vom Aufrufer
        // zurückgesetzten Text stehen. Andere Felder bleiben unberührt - eine
        // Reihenprüfung mehrerer Felder in einem Klick meldet weiterhin jedes
        // fehlerhafte Feld.
        // ------------------------------------------------------------------
        private static bool m_bPruefmeldungGesperrt = false;
        private static Control m_ctrlPruefmeldung = null;
        private static EventHandler m_hPruefmeldungFrei = null;

        private static bool PruefmeldungGesperrt(Control ctrl)
        {
            return m_bPruefmeldungGesperrt && ctrl != null && ReferenceEquals(ctrl, m_ctrlPruefmeldung);
        }

        private static void PruefmeldungSperren(Control ctrl)
        {
            m_bPruefmeldungGesperrt = true;
            m_ctrlPruefmeldung = ctrl;

            // Die Sperre gilt nur für den laufenden Ereignisdurchlauf: Das
            // Freigeben wird in die Nachrichtenschlange gestellt und damit erst
            // ausgeführt, wenn der komplette TextChanged-Stapel abgearbeitet ist.
            if (ctrl != null && ctrl.IsHandleCreated && !ctrl.InvokeRequired)
            {
                try { ctrl.BeginInvoke(new MethodInvoker(PruefmeldungFreigeben)); }
                catch (Exception) { /* Handle schon weg - Leerlauf gibt frei */ }
            }

            // Sicherheitsnetz: Wird der Dialog geschlossen, bevor die Nachricht
            // abgearbeitet ist, gibt der Leerlauf der Nachrichtenschleife frei.
            if (m_hPruefmeldungFrei == null)
            {
                m_hPruefmeldungFrei = delegate { PruefmeldungFreigeben(); };
                Application.Idle += m_hPruefmeldungFrei;
            }
        }

        private static void PruefmeldungFreigeben()
        {
            m_bPruefmeldungGesperrt = false;
            m_ctrlPruefmeldung = null;
        }

        public static bool checkInt(Control ctrl, string text)
        {
            int number;
            if (int.TryParse(text, out number)) return true;
            if (PruefmeldungGesperrt(ctrl)) return true;

            if (ctrl != null) ctrl.Focus();
            MessageBox.Show("Eingaben überprüfen: \"" + text + "\"" + Environment.NewLine +
                            "Bitte eine ganze Zahl eingeben.");
            PruefmeldungSperren(ctrl);
            return false;
        }

        /// <summary>
        /// Weiterleitung auf <see cref="ZahlText.Parsen"/> (dort steht der Rumpf seit
        /// iU4-1) — parst eine Zahl mit Dezimal-Komma ODER -Punkt, ohne
        /// Tausendertrennzeichen. Bleibt stehen, damit die Masken ihren gewohnten
        /// Aufruf behalten.
        /// </summary>
        public static bool ZahlParsen(string text, out double wert)
        {
            return ZahlText.Parsen(text, out wert);
        }

        /// <summary>
        /// Weiterleitung auf <see cref="ZahlText.GanzzahlParsen"/> — Ganzzahl-
        /// Gegenstück zu <see cref="ZahlParsen"/>, invariant geparst.
        /// </summary>
        public static bool GanzzahlParsen(string text, out int wert)
        {
            return ZahlText.GanzzahlParsen(text, out wert);
        }

        // ------------------------------------------------------------------
        // Folgepaket zu ab5bf32: Eingabeprüfung weg vom TextChanged.
        //
        // TextChanged färbt nur noch (ZahlFaerben/GanzzahlFaerben), gemeldet
        // wird erst beim OK-/Übernehmen-Knopf (ZahlPruefen/GanzzahlPruefen nach
        // dem Muster ProjektPuffer.TemperaturenPruefen: TryParse, sprechende
        // Meldung, Fokus+SelectAll, der Aufrufer lässt den Dialog offen).
        // Abbrechen bleibt dadurch immer frei.
        // ------------------------------------------------------------------

        /// <summary>Hinweisfarbe für Felder, deren Text gerade keine Zahl ist.</summary>
        private static readonly Color FarbeFehleingabe = Color.FromArgb(255, 235, 235);

        /// <summary>
        /// TextChanged-Begleiter: färbt das Feld, statt modal zu melden. Ein
        /// leeres Feld gilt als neutral - ob leer erlaubt ist, entscheidet die
        /// Knopf-Prüfung des jeweiligen Dialogs.
        /// </summary>
        public static void ZahlFaerben(object sender)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            double wert;
            bool bOk = tb.Text.Trim().Length == 0 || ZahlParsen(tb.Text, out wert);
            tb.BackColor = bOk ? SystemColors.Window : FarbeFehleingabe;
        }

        /// <summary>Wie <see cref="ZahlFaerben"/>, nur für Ganzzahlfelder.</summary>
        public static void GanzzahlFaerben(object sender)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            int wert;
            bool bOk = tb.Text.Trim().Length == 0 || GanzzahlParsen(tb.Text, out wert);
            tb.BackColor = bOk ? SystemColors.Window : FarbeFehleingabe;
        }

        /// <summary>
        /// Knopf-Prüfung für ein Dezimalzahlfeld: TryParse (Komma oder Punkt),
        /// bei Fehler sprechende Meldung + Fokus + SelectAll und 'false' - der
        /// Aufrufer kehrt dann zurück und lässt den Dialog offen.
        /// </summary>
        /// <param name="leerErlaubt">true: ein leeres Feld gilt als 0 (bisheriges
        /// Verhalten der Speicherwege mit convertTxt2Double bzw. '"" ? 0').</param>
        public static bool ZahlPruefen(TextBox feld, string bezeichnung, out double wert, bool leerErlaubt = false)
        {
            wert = 0.0;
            if (feld == null) return true;

            string text = feld.Text.Trim();
            if (text.Length == 0 && leerErlaubt) return true;
            if (text.Length != 0 && ZahlParsen(text, out wert)) return true;

            MessageBox.Show("Eingaben überprüfen: \"" + feld.Text + "\"" + Environment.NewLine +
                            "Bitte für \"" + bezeichnung + "\" eine Zahl eingeben " +
                            "(Dezimaltrennzeichen Komma oder Punkt).",
                            "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            feld.Focus();
            feld.SelectAll();
            return false;
        }

        /// <summary>Wie <see cref="ZahlPruefen"/>, nur für Ganzzahlfelder.</summary>
        public static bool GanzzahlPruefen(TextBox feld, string bezeichnung, out int wert, bool leerErlaubt = false)
        {
            wert = 0;
            if (feld == null) return true;

            string text = feld.Text.Trim();
            if (text.Length == 0 && leerErlaubt) return true;
            if (text.Length != 0 && GanzzahlParsen(text, out wert)) return true;

            MessageBox.Show("Eingaben überprüfen: \"" + feld.Text + "\"" + Environment.NewLine +
                            "Bitte für \"" + bezeichnung + "\" eine ganze Zahl eingeben.",
                            "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            feld.Focus();
            feld.SelectAll();
            return false;
        }

        /// <summary>
        /// Zahl aus Text nach derselben Regel wie <see cref="ZahlParsen"/>:
        /// Dezimal-Komma ODER -Punkt, kein Tausendertrennzeichen. Vertrag der
        /// Aufrufer bleibt erhalten: leer (oder null) ergibt 0, nicht parsbarer
        /// Text wirft FormatException - die Einlese-Dialoge fangen sie und zählen
        /// den Eintrag als Fehler. Aufruferkataster und Herleitung:
        /// Dokumentation\ueberholt\Protokolle\Simulation\Befund_convertTxt2Double_Dezimaltrennzeichen.md.
        /// </summary>
        public static double convertTxt2Double(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return 0;

            double number;
            if (ZahlParsen(txt, out number)) return number;
            throw new FormatException("Keine gültige Zahl: \"" + txt + "\"");
        }

        /// <summary>
        /// Ganzzahl aus Text; zusätzlich werden Dezimalschreibweisen ganzer
        /// Zahlen akzeptiert ("35.0", "35,0" ergibt 35 - VDI-Dateien liefern
        /// Ganzzahlfelder teils so). Vertrag bleibt: leer oder nicht
        /// (ganzzahlig) parsbar ergibt 0, kein Wurf.
        /// </summary>
        public static int convertTxt2Int(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return 0;

            int number;
            if (GanzzahlParsen(txt, out number)) return number;

            double d;
            if (ZahlParsen(txt, out d) && d >= int.MinValue && d <= int.MaxValue && d == Math.Floor(d))
                return (int)d;

            return 0;
        }

        public static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;

        }

        public static class UICharacters
        {

            // Benutzung: btnParse.Text = $"{UICharacters.Search} Vorschau";
            // --- Datei-Operationen ---
            public const string OpenFile = "📂"; // \U0001F4C2
            public const string Save = "💾"; // \U0001F4BE
            public const string Settings = "⚙";  // \u2699
            public const string Trash = "🗑";  // \U0001F5D1
            public const string Refresh = "🔄"; // \U0001F504
            public const string Export = "📤"; // \U0001F4E4

            // --- PV-Technik & Details ---
            public const string Energy = "⚡";  // \u26A1
            public const string Sun = "☀️";  // \u2600
            public const string Temp = "🌡️";  // \U0001F321
            public const string Chart = "📊";  // \U0001F4CA
            public const string Geometry = "📐";  // \U0001F4D0
            public const string Eco = "🌿";  // \U0001F33F
            public const string Bifacial = "💎";  // \U0001F48E (Oft für hochwertige/bifaziale Zellen genutzt)

            // --- Status & Navigation ---
            public const string Search = "🔍";  // \U0001F50D
            public const string Success = "✅";  // \u2705
            public const string Cancel = "❌";  // \u274C
            public const string Info = "ℹ";   // \u2139
            public const string Warning = "⚠️";  // \u26A0
            public const string Link = "🔗";  // \U0001F517
            public const string Web = "🌐";  // \U0001F310

            // --- Listen-Steuerung ---
            public const string MoveUp = "⬆";   // \u2B06
            public const string MoveDown = "⬇";   // \u2B07
            public const string Add = "➕";   // \u2795
            public const string Remove = "➖";   // \u2796
        }

     }

}
