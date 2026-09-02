using Microsoft.Win32;
using System;
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
        public static MDIMainForm mdifrm = null;
        public static FormMain mainfrm = null;
        public static Form_Start startfrm = null;
        public static MenueCtrl menuectrl = null;
        public static WizardCtrl wizardctrl = null;
        public static string ApplicationPath_Common = "";
        public static string ApplicationPath_User = "";
        public static int nLanguage = 0; // 0=de, 1=en  

        /// <summary>
        /// Not-Rückfall für die Basis-URL der Wiki-Dokumentation, falls der
        /// Einstellwert <c>WordPressUrl</c> leer ist (A2). Derselbe Wert steht
        /// als Werksvorgabe in der <c>app.config</c>.
        /// </summary>
        public const string WIKI_STANDARD = "https://wiki.epos-plan.de";

        // Der globale Katalog, auf den alle Formulare zugreifen können
        public static WikiHelpCatalog HelpCatalog { get; private set; }

        /// <summary>
        /// Der anwendungsweite Infobutton-Extender (Konzept Hilfesystem, F5).
        /// EINE Instanz für das ganze Programm — bisher erzeugte jedes Formular
        /// eine eigene. <see cref="HilfeAutomatik"/> erfasst darüber jedes geöffnete
        /// Formular und jedes nachgeladene UserControl von selbst; kein Formular
        /// braucht dafür noch eigenen Programmtext.
        /// </summary>
        public static HelpExtender HelpExtender { get; private set; }

        private static Process _webServerProcess;

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        /// <summary>
        /// Der einzige Schalter, der die Feldsicherung des Assistenten abschaltet
        /// (Fachkonzept 11.5, Abnahme 20.08.2026).
        /// </summary>
        public const string SCHALTER_FELDSICHERUNG_AUS = "/ki-feldsicherung-aus";

        [STAThread]
        static void Main()
        {
            // Die Feldsicherung wird VOR allem anderen ausgewertet - vor der ersten
            // Meldung und vor dem ersten Fenster. Sonst könnte eine Maske aufgehen und
            // ihren Aufrufknopf anbringen, während der Zustand noch nicht feststeht.
            FeldsicherungSchalterAuswerten();

            // MELDEHAKEN VOR ALLEM ANDEREN (Umsetzungskonzept iU3, Schritt 2).
            //
            // Die Kerndateien - Zugriffsschicht, Wärmepumpen-Simulation, Stamm-Dialoge -
            // rufen ihre Meldungen seither über Meldung.*, damit sie ohne
            // System.Windows.Forms übersetzbar bleiben. Unter Windows soll sich exakt
            // nichts ändern; deshalb werden die Haken hier wortgleich auf MessageBox
            // bzw. Cursor gesetzt, und zwar VOR jedem Code, der eine Meldung absetzen
            // könnte. Stünde das weiter unten, ginge genau die erste Meldung eines
            // Startfehlers auf die Konsole statt in einen Dialog.
            Meldung.Zeigen = text => MessageBox.Show(text);
            Meldung.Hinweis = (text, titel) => MessageBox.Show(text, titel);
            Meldung.Warnung = (text, titel) =>
                MessageBox.Show(text, titel, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Meldung.Warten = an =>
                Cursor.Current = an ? Cursors.WaitCursor : Cursors.Default;

            // Aktiviert die moderne High-DPI-Unterstützung (Verfügbar ab .NET Framework 4.7)
            if (Environment.OSVersion.Version.Major >= 10)
            {
                Application.SetHighDpiMode(HighDpiMode.DpiUnaware); // Für .NET Core / .NET 5+
                                                                     // Für älteres .NET Framework 4.7+ nutzt man stattdessen oft:
                                                                     // Application.EnableVisualStyles();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"Software\\wp-plan");
            }

            nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 0)
            {
                var culture_de = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentUICulture = culture_de;
            }
            else
            {
                var culture_en = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentUICulture = culture_en;
            }

            // Startprüfung (vormals x64-Umstellung P1.3, jetzt DB-Migration SQLite 2.8):
            // Ohne lesbare Datenbankdatei ist jede DB-Operation unmöglich — sprechende
            // Meldung statt später einer nackten Ausnahme tief im Startpfad (erste
            // Fundstelle wäre SchemaMigration.Ausfuehren). NACH der Sprachwahl, damit die
            // Meldung in der eingestellten Sprache kommt, und VOR jedem Datenbankzugriff.
            // Einen registrierungspflichtigen Provider gibt es nach der Umstellung nicht
            // mehr; geprüft wird jetzt die Datei selbst.
            // Der Meldungstext steht bewusst noch als Literal hier: Die Ressourcenschlüssel
            // START_ACE_FEHLT_* beschreiben die Access-Engine und werden mit dem übrigen
            // Textbestand in Arbeitspaket S8 nachgezogen.
            //
            // ERSTSTART-ASSISTENT (S8, Implementierungskonzept Abschnitt 8): Auf einem
            // Bestandsrechner gibt es die SQLite-Datei beim allerersten Start dieser
            // Fassung noch gar nicht - DatenbankVorhanden() prüft aber genau sie. Der
            // Assistent muss deshalb INNERHALB dieser Prüfung greifen, und zwar bevor
            // ihre Fehlermeldung erscheint: Nur wenn die Datei fehlt, wird das Lagebild
            // des Ordners erhoben; liegt dort ein Access-Altbestand, wird er einmalig
            // umgestellt. Alles Weitere (Lizenz, Schemapflege, Oberfläche) läuft danach
            // unverändert - insbesondere SchemaMigration.Ausfuehren, das dann auf der
            // frischen SQLite-Datei aufsetzt.
            if (!DataRepository.DatenbankVorhanden())
            {
                if (!ErststartAnbieten()) return;
            }

            // Zustimmung zur Lizenzvereinbarung beim ersten Start (einmal je
            // Windows-Benutzer; Ablage HKCU\Software\wp-plan\LizenzZugestimmt mit
            // Programmversion und Datum, siehe Form_Lizenz.ZustimmungMerken).
            // NACH der ACE-Prüfung - eine nicht startfähige Installation braucht
            // keine Zustimmung - und VOR der Schema-Migration: Wer ablehnt, dessen
            // Datenbank wird nicht angefasst.
            if (!Form_Lizenz.ZustimmungSicherstellen()) return;

            // Textlieferant des KI-Kerns einhaengen - NACH der Sprachwahl, damit
            // KiKern seine Schluessel in der eingestellten Sprache beantwortet
            // bekommt (Fachkonzept 3.7; KiKern darf MyResource nicht kennen).
            KiTextlieferant.Einrichten();

            // Rechtshinweis des KI-Assistenten einhaengen: erst damit gibt es ueberhaupt
            // einen Weg zu einer Einwilligung. Ohne diesen Aufruf - Aktionsharnisch,
            // Tests, Konsolenlauf - wird keine Anfrage an den Anbieter gesendet.
            Form_KiHinweis.Einhaengen();

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

            ApplicationPath_Common = Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            ApplicationPath_Common = Path.Combine(ApplicationPath_Common, "WP-Plan");
            ApplicationPath_User = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ApplicationPath_User = Path.Combine(ApplicationPath_User, "WP-Plan");

            // Katalog-Objekt einmalig erstellen
            //
            // A2 (H1): Die Basis-URL kommt wieder aus den Einstellungen. Damit
            // steuert EIN Wert (Admin-Dialog, Feld "Online-Dokumentation")
            // sowohl den Hilfekatalog als auch den Menüpunkt Dokumentation.
            // Der Settings-Schlüssel heißt aus Kompatibilitätsgründen weiterhin
            // "WordPressUrl" — eine Umbenennung würde gespeicherte Anwenderwerte
            // in der user.config verwerfen (Entscheid 7.3 des Konzepts).
            // WIKI_STANDARD greift nur, wenn der Einstellwert leer ist.
            string dokuBasis = Properties.Settings.Default.WordPressUrl;
            if (string.IsNullOrWhiteSpace(dokuBasis)) dokuBasis = WIKI_STANDARD;

            HelpCatalog = new WikiHelpCatalog(dokuBasis);

            // F6 / Startwettlauf: Der Katalog wird SOFORT belegt — aus der lokalen
            // Sicherung, sonst aus dem mitgelieferten Startbestand. MDIMainForm_Load
            // stößt den Onlineabruf danach bewusst ohne await an; ohne diese
            // Vorbelegung sähe jedes Formular, das früher öffnet, einen leeren
            // Katalog. Rangfolge insgesamt: Online > AppData-Sicherung > Beilage.
            HelpCatalog.StartbestandLaden();

            // F5: EIN anwendungsweiter Extender, und eine Automatik, die jedes
            // geöffnete Formular und jedes nachgeladene UserControl selbst erfasst.
            // Ab hier ist help_mapping.txt die einzige Stelle, an der Hilfe
            // gepflegt wird.
            HelpExtender = HilfeAutomatik.Starten(HelpCatalog);

            // nur zum Testen, Testserver wird in dieser Funktion beim Starten des Programms automatisch aufgerufen,
            // kein separates CMD Fensetr mit Aufruf nötig
            //StartLocalWebServer();

            mdifrm = new MDIMainForm();
            Application.Run(mdifrm);

            Application.Exit();
        }

        /// <summary>
        /// Die Gabelung des Erststarts (Arbeitspaket S8): Es gibt keine lesbare
        /// SQLite-Datei — liegt daneben ein Access-Altbestand, wird er einmalig
        /// umgestellt; sonst bleibt es bei der bisherigen Meldung.
        /// </summary>
        /// <returns><c>true</c> = weiterstarten, <c>false</c> = Programm beenden.</returns>
        /// <remarks>
        /// <para>
        /// Der Settings-Fixup (N7) läuft hier mit <c>true</c>: Im Programmbetrieb soll der
        /// gespeicherte <c>DBName</c> nach der Umstellung auf <c>Kenndaten.sqlite</c>
        /// zeigen. Der Vorgriff in <see cref="DataRepository.GetDBPath"/> bleibt als Netz
        /// bestehen, falls der Fixup nicht durchkommt.
        /// </para>
        /// <para>
        /// Nach erfolgreicher Umstellung wird die Startprüfung WIEDERHOLT — erst ein
        /// zweites <c>DatenbankVorhanden()</c> beweist, dass die neue Datei auch wirklich
        /// zu öffnen ist. Nur dann geht es weiter.
        /// </para>
        /// </remarks>
        private static bool ErststartAnbieten()
        {
            string ordner = ErststartMigration.StandardOrdner();

            if (ErststartMigration.Pruefe(ordner) != ErststartLage.NurAccdbVorhanden)
            {
                // Unverändert der bisherige Fall: keine Datenbank, kein Altbestand.
                MessageBox.Show(
                    "Datenbankdatei nicht gefunden/lesbar: " + DataRepository.GetDBPath(),
                    "Datenbank nicht verfügbar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string berichtPfad;
            bool umgestellt = Form_Erststart.Zeigen(ordner, true, out berichtPfad);

            if (!umgestellt)
            {
                MessageBox.Show(
                    "Die Datenbank wurde nicht umgestellt — das Programm kann nicht starten." +
                    Environment.NewLine + Environment.NewLine +
                    ErststartMigration.LetzteMeldung +
                    (string.IsNullOrEmpty(berichtPfad)
                        ? ""
                        : Environment.NewLine + Environment.NewLine + "Bericht: " + berichtPfad),
                    "Datenbankumstellung",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!DataRepository.DatenbankVorhanden())
            {
                MessageBox.Show(
                    "Die Umstellung meldet Erfolg, die neue Datenbankdatei lässt sich aber nicht " +
                    "öffnen: " + DataRepository.GetDBPath() +
                    (string.IsNullOrEmpty(berichtPfad)
                        ? ""
                        : Environment.NewLine + Environment.NewLine + "Bericht: " + berichtPfad),
                    "Datenbank nicht verfügbar",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Wertet <see cref="SCHALTER_FELDSICHERUNG_AUS"/> aus und schaltet die
        /// Feldsicherung des Assistenten ab, wenn er in der Befehlszeile steht
        /// (Fachkonzept 11.5, Umsetzungskonzept Etappe 3b, Paket F4).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Diese Methode ist der EINZIGE Weg zur Abschaltung.</b> Ausdrücklich nicht
        /// vorgesehen sind ein Menüpunkt, ein Kontrollkästchen, eine Einstellung in
        /// <c>Properties.Settings</c> und ein Registry-Wert — alles vier trüge den Zustand
        /// über einen Neustart hinweg, und der Anwender könnte einer einmal gesehenen
        /// Bestätigungsfrage nicht mehr entnehmen, ob sie beim nächsten Mal wiederkommt.
        /// Der Schalter ist ein Startzustand für Entwicklung und Prüfläufe, kein
        /// Betriebsmodus (<see cref="KiKern.KiFeldsicherung"/>).
        /// </para>
        /// <para>
        /// <b>Warum <see cref="Environment.GetCommandLineArgs"/> und kein
        /// <c>Main(string[])</c>.</b> Der Einstiegspunkt dieser Anwendung nimmt seit jeher
        /// keine Argumente entgegen; ihn umzubauen, wäre eine Änderung an der
        /// Programmsignatur für eine Nebensache. Der Aufruf liefert dieselben Argumente,
        /// nur mit dem Programmpfad an Stelle 0 — deshalb beginnt die Schleife bei 1.
        /// </para>
        /// <para>
        /// <b>Nur diese eine Schreibweise, dafür ohne Rücksicht auf Groß- und
        /// Kleinschreibung.</b> Wer den Schalter setzt, tut das absichtlich; Abwandlungen
        /// wie <c>--ki-feldsicherung-aus</c> mit zu erlauben, würde den Abschaltkanal
        /// verbreitern, ohne irgendetwas leichter zu machen. Unbekannte Argumente bleiben
        /// unbeachtet — die Anwendung hat keine Befehlszeilenverarbeitung, und diese
        /// Methode soll auch keine werden.
        /// </para>
        /// <para>
        /// <b>Ein Fehlschlag darf den Start nicht kosten.</b> Bleibt die Auswertung
        /// stecken, ist die Feldsicherung AN — die sichere Richtung.
        /// </para>
        /// </remarks>
        private static void FeldsicherungSchalterAuswerten()
        {
            try
            {
                if (!FeldsicherungAusVerlangt(Environment.GetCommandLineArgs())) return;

                // Der Grund steht später im Chat und in jeder Protokollzeile einer
                // Formularaktion; er nennt deshalb den Schalter im Wortlaut.
                KiKern.KiFeldsicherung.Abschalten(
                    "Befehlszeilenschalter " + SCHALTER_FELDSICHERUNG_AUS);
            }
            catch (Exception)
            {
                // Im Zweifel bleibt die Sicherung an.
            }
        }

        /// <summary>
        /// Steht <see cref="SCHALTER_FELDSICHERUNG_AUS"/> in dieser Argumentliste?
        /// </summary>
        /// <param name="argumente">
        /// Die Argumente wie von <see cref="Environment.GetCommandLineArgs"/> - Stelle 0
        /// ist der Programmpfad und wird übersprungen.
        /// </param>
        /// <remarks>
        /// Vom Abschalten getrennt, damit sich die Erkennung prüfen lässt, ohne die
        /// Feldsicherung wirklich abzuschalten: <c>KiFeldsicherung.Abschalten</c> wirkt
        /// einmalig und unwiderruflich für den ganzen Prozess, ein Prüflauf könnte den
        /// Zustand also nicht wiederherstellen. Diese Methode dagegen ist eine reine
        /// Funktion über ihrer Eingabe und wird vom Aktionsharnisch mit erfundenen
        /// Argumentlisten befragt (<c>KiHarnisch\Katalogpruefung.cs</c>).
        /// </remarks>
        internal static bool FeldsicherungAusVerlangt(string[] argumente)
        {
            if (argumente == null) return false;

            for (int i = 1; i < argumente.Length; i++)
            {
                string argument = (argumente[i] ?? "").Trim();
                if (string.Equals(argument, SCHALTER_FELDSICHERUNG_AUS,
                                  StringComparison.OrdinalIgnoreCase))
                    return true;
            }

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
        /// Parst eine Zahl mit Dezimal-Komma ODER -Punkt. Gleiche Regel wie
        /// WaermequelleClass.ZahlParsen, nur in double-Genauigkeit - gedacht für
        /// Eingabefelder, deren Wert als double weiterverarbeitet wird.
        /// Kein Tausendertrennzeichen: "1.234,5" wird bewusst abgelehnt, statt
        /// wie double.Parse(CurrentCulture) still zu 12345 zu werden.
        /// </summary>
        public static bool ZahlParsen(string text, out double wert)
        {
            wert = 0.0;
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out wert);
        }

        /// <summary>
        /// Ganzzahl-Gegenstück zu <see cref="ZahlParsen"/>: invariant geparst.
        /// Komma und Punkt sind hier bewusst KEINE gültigen Zeichen - es geht um
        /// Stückzahlen, Tage, Nutzungsdauern und ganze Grad.
        /// </summary>
        public static bool GanzzahlParsen(string text, out int wert)
        {
            wert = 0;
            if (string.IsNullOrEmpty(text)) return false;
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out wert);
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
        /// Allgemein\Simulation\Befund_convertTxt2Double_Dezimaltrennzeichen.md.
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

        private static void StartLocalWebServer()
        {
            try
            {
                _webServerProcess = new Process();

                // Da 'dotnet' ein globaler Systembefehl ist, können wir ihn direkt beim Namen nennen
                _webServerProcess.StartInfo.FileName = "dotnet";

                // Hier sagen wir dotnet-serve, welchen Ordner es auf welchem Port öffnen soll:
                // "serve" = Tool aufrufen
                // "-d C:\WPFake" = Dieses Verzeichnis ausliefern
                // "-p 8080" = Port 8080 nutzen
                _webServerProcess.StartInfo.Arguments = @"serve -d C:\WPFake -p 8080";

                // WICHTIG: Macht das CMD-Fenster für den Benutzer unsichtbar
                _webServerProcess.StartInfo.CreateNoWindow = true;
                _webServerProcess.StartInfo.UseShellExecute = false;

                _webServerProcess.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Starten von dotnet-serve: " + ex.Message);
            }
        }

        private static void StopLocalWebServer()
        {
            if (_webServerProcess != null && !_webServerProcess.HasExited)
            {
                _webServerProcess.Kill(); // Schließt dotnet-serve im Hintergrund wieder
                _webServerProcess.Dispose();
            }
        }

     }

}