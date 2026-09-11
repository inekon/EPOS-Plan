using System;
using System.Globalization;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Riegel vor dem externen Dienst: Abschalter der Installation und die
    /// versionierte Einwilligung des Anwenders in den Rechtshinweis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum eine eigene Klasse ohne Oberfläche.</b> Die Zusage „ohne Einwilligung
    /// geht keine Anfrage hinaus" muss OHNE Fenster prüfbar sein - der Aktionsharnisch
    /// läuft ohne Netz, ohne Schlüssel und ohne Dialoge. Hier steht deshalb nur der
    /// Zustand und die Entscheidung; den Hinweistext zeigt
    /// <c>KiHinweisHuelle</c> (seit iU9-W15b.3; davor <c>Form_KiHinweis</c>),
    /// die sich beim Programmstart über
    /// <see cref="Nachfragen"/> einhängt. Ist kein Haken eingehängt (Harnisch, Tests,
    /// Konsolenlauf), kann keine Einwilligung entstehen - und damit auch keine
    /// Übertragung.
    /// </para>
    /// <para>
    /// <b>Warum die Einwilligung eine Fassungsnummer trägt.</b> Abgelegt wird nicht
    /// „ja", sondern die Nummer der Hinweisfassung, der zugestimmt wurde. Ändert sich
    /// der Hinweistext inhaltlich, wird <see cref="FASSUNG"/> erhöht; die alte Zustimmung
    /// deckt die neue Fassung dann nicht mehr ab und es wird erneut gefragt.
    /// </para>
    /// <para>
    /// <b>Ablage.</b> Derselbe Registry-Zweig wie Sprache, Lizenzzustimmung und
    /// KI-Einstellungen: <c>HKCU\Software\wp-plan</c>. Der Abschalter wird zusätzlich
    /// unter <c>HKLM\Software\wp-plan</c> GELESEN - dort kann die Verwaltung einer
    /// Kundeninstallation ihn setzen, ohne dass ein Anwender ihn wieder lösen kann.
    /// Geschrieben wird immer nur nach HKCU.
    /// </para>
    /// </remarks>
    public static class KiEinwilligung
    {
        /// <summary>
        /// Fassung des Rechtshinweises. <b>Bei jeder inhaltlichen Änderung des
        /// Hinweistextes (Ressourcen KI_HINWEIS_*) um eins erhöhen</b> - dann wird
        /// erneut gefragt.
        /// </summary>
        // Fassung 2 (22.08.2026): Der Hinweis sprach von "ausschließlich lesenden
        // Aktionen"; tatsaechlich fuehrt der Assistent nach ausdruecklicher Bestaetigung
        // auch datenveraendernde Aktionen aus. Weil die alte Fassung damit die heutige
        // Verarbeitung nicht mehr deckt, wird die Einwilligung erneut eingeholt.
        public const int FASSUNG = 2;

        private const string REG_SCHLUESSEL = @"Software\wp-plan";

        /// <summary>Bestätigte Fassung des Hinweises (Zahl als Text), leer/fehlend = keine.</summary>
        private const string REG_BESTAETIGT = "KiHinweisBestaetigt";

        /// <summary>Zeitpunkt der Bestätigung, nur zur Anzeige.</summary>
        private const string REG_BESTAETIGT_AM = "KiHinweisBestaetigtAm";

        /// <summary>Abschalter: "1" = alle KI-Funktionen aus.</summary>
        private const string REG_ABSCHALTER = "KiDeaktiviert";

        // ------------------------------------------------------------------
        // Zweite Stufe: DIALOGDATEN (Auftrag #200, Anwenderentscheid KI-D-Q2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Fassung der Einwilligung in die Übertragung von <b>Feldwerten offener
        /// Masken</b>. <b>Bei jeder inhaltlichen Änderung des Erklärtextes (Ressourcen
        /// <c>KI_DIALOGDATEN_*</c>) um eins erhöhen</b> — dann wird erneut gefragt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum eine eigene Stufe und nicht die vorhandene.</b> Die allgemeine
        /// Einwilligung (<see cref="FASSUNG"/>) deckt ab, dass FRAGE und HILFEABSCHNITTE
        /// an den Anbieter gehen — Bedienbegriffe, keine Projektdaten (Konzept „Der
        /// Hilfe-Assistent im Dialog", 3.2). Ein Feldblock ist etwas anderes: Er trägt
        /// die Zahlen, die der Anwender gerade eingibt. Wer dem einen zustimmt, hat dem
        /// anderen nicht zugestimmt — deshalb ein eigener Merker, eine eigene Fassung
        /// und ein eigener Rückweg (Anwenderentscheid <b>KI‑D‑Q2</b>, 11.09.2026).
        /// </para>
        /// <para>
        /// <b>Der Abschalter überstimmt auch sie.</b> Ist die KI abgeschaltet, geht
        /// nichts hinaus — eine erteilte Dialogdaten-Einwilligung ändert daran nichts.
        /// </para>
        /// </remarks>
        public const int FASSUNG_DIALOGDATEN = 1;

        /// <summary>Bestätigte Fassung der Dialogdaten-Einwilligung (Zahl als Text).</summary>
        private const string REG_DIALOGDATEN = "KiDialogdatenBestaetigt";

        /// <summary>Zeitpunkt der Dialogdaten-Einwilligung, nur zur Anzeige.</summary>
        private const string REG_DIALOGDATEN_AM = "KiDialogdatenBestaetigtAm";

        // ------------------------------------------------------------------
        // Einhängepunkt der Oberfläche
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt den vollständigen Hinweis und liefert <c>true</c>, wenn der Anwender
        /// zugestimmt hat. Wird beim Programmstart von der Hülle gesetzt
        /// (Windows: <c>KiHinweisHuelle.Einholen</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bleibt der Haken leer, gibt es keinen Weg zu einer Einwilligung. Das ist
        /// Absicht: ein Lauf ohne Oberfläche darf nichts an den Anbieter senden.
        /// Der Haken darf nicht werfen; tut er es doch, gilt das als Ablehnung.
        /// </para>
        /// <para>
        /// <b>Seit iU9-W15b.0b asynchron</b> (Befund W15b-B12). Der Vorläufer war ein
        /// <c>Func&lt;bool&gt;</c>, weil ein modales WinForms-Fenster synchron antwortet.
        /// Eine Razor-Überlagerung kann das nicht: Sie zeichnet sich, wartet auf einen
        /// Klick und meldet ihn über einen <c>EventCallback</c> - das ist ein
        /// <c>Task</c>. Ein blockierendes Warten darauf verklemmt den Renderer.
        /// </para>
        /// </remarks>
        public static Func<Task<bool>> Nachfragen { get; set; }

        // ------------------------------------------------------------------
        // Abschalter
        // ------------------------------------------------------------------

        /// <summary>
        /// Der Abschalter steht maschinenweit (HKLM) und lässt sich aus der Anwendung
        /// heraus nicht lösen - vorgesehen für Installationen, in denen der externe
        /// Dienst nicht zulässig ist.
        /// </summary>
        /// <remarks>
        /// Gelesen werden BEIDE Registry-Sichten, ein Treffer genügt. Grund: Die x86-Fassung
        /// der Anwendung landete über die WOW6432Node-Umleitung tatsächlich in
        /// <c>HKLM\SOFTWARE\WOW6432Node\wp-plan</c>, die x64-Fassung liest dagegen
        /// <c>HKLM\SOFTWARE\wp-plan</c>. Ohne beide Sichten würden Alt-Einträge aus der
        /// x86-Zeit nach der Umstellung stillschweigend wirkungslos - und der Schalter
        /// wirkt so in beiden Bitnessen gleich
        /// (Konzept_Umstellung_64Bit_EPOS-Plan.md, P1.1).
        /// </remarks>
        public static bool AbschalterMaschine
        {
            get { return Ist(Dienste.Einstellungen.LiesMaschine(REG_ABSCHALTER)); }
        }

        /// <summary>
        /// Alle KI-Funktionen sind abgeschaltet. Gesetzt wird benutzerbezogen (HKCU);
        /// ein maschinenweiter Abschalter (HKLM) überstimmt jede Einstellung.
        /// </summary>
        public static bool Abgeschaltet
        {
            get { return AbschalterMaschine || Ist(Lesen(REG_ABSCHALTER)); }
            set { Schreiben(REG_ABSCHALTER, value ? "1" : "0"); }
        }

        // ------------------------------------------------------------------
        // Einwilligung
        // ------------------------------------------------------------------

        /// <summary>Bestätigte Hinweisfassung; 0 = noch keine Einwilligung.</summary>
        public static int BestaetigteFassung
        {
            get
            {
                int n;
                string wert = Lesen(REG_BESTAETIGT);
                return int.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 0;
            }
        }

        /// <summary>Zeitpunkt der Bestätigung als Text; leer, wenn keine vorliegt.</summary>
        public static string BestaetigtAm
        {
            get { return Lesen(REG_BESTAETIGT_AM) ?? ""; }
        }

        /// <summary>
        /// Es liegt eine gültige Einwilligung für die AKTUELLE Hinweisfassung vor und
        /// die KI ist nicht abgeschaltet.
        /// </summary>
        public static bool Erteilt
        {
            get { return !Abgeschaltet && BestaetigteFassung >= FASSUNG; }
        }

        /// <summary>Merkt die Einwilligung für die aktuelle Fassung samt Zeitpunkt.</summary>
        public static void Erteilen()
        {
            Schreiben(REG_BESTAETIGT, FASSUNG.ToString(CultureInfo.InvariantCulture));
            Schreiben(REG_BESTAETIGT_AM, DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        /// <summary>Nimmt die Einwilligung zurück; beim nächsten Mal wird wieder gefragt.</summary>
        public static void Zuruecknehmen()
        {
            Loeschen(REG_BESTAETIGT);
            Loeschen(REG_BESTAETIGT_AM);
        }

        /// <summary>
        /// Der eine Riegel vor jeder Übertragung: liefert <c>true</c>, wenn gesendet
        /// werden darf. Fehlt die Einwilligung, wird sie über <see cref="Nachfragen"/>
        /// eingeholt - ist kein Haken eingehängt oder lehnt der Anwender ab, bleibt es
        /// bei <c>false</c> und es geht nichts hinaus.
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge ist Teil der Zusage: Abschalter zuerst (er überstimmt eine
        /// vorhandene Einwilligung), dann die vorhandene Fassung, erst danach die Frage.
        /// Wer eine Fassung 1 bestätigt hat, wird bei FASSUNG 2 erneut gefragt.
        /// </remarks>
        public static async Task<bool> SicherstellenAsync()
        {
            if (Abgeschaltet) return false;
            if (BestaetigteFassung >= FASSUNG) return true;

            Func<Task<bool>> frage = Nachfragen;
            if (frage == null) return false;

            bool ja;
            try
            {
                Task<bool> lauf = frage();
                ja = lauf != null && await lauf.ConfigureAwait(true);
            }
            catch { return false; }

            if (!ja) return false;

            Erteilen();
            return true;
        }

        /// <summary>
        /// Die synchrone Fassade: <c>true</c> nur, wenn die Einwilligung SCHON vorliegt.
        /// Sie fragt nicht nach.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sie bleibt für die Stellen, die keine Fortsetzung haben - eine Sichtbarkeit,
        /// ein Menütext, eine Vorbelegung. Fehlt die Einwilligung, liefert sie
        /// <c>false</c>, statt zu fragen: Ein blockierendes Warten auf
        /// <see cref="Nachfragen"/> würde in einer WebView den Renderer verklemmen, der
        /// die Überlagerung erst noch zeichnen müsste (R-W15b-5).
        /// </para>
        /// <para>
        /// <b>Wer fragen will, ruft <see cref="SicherstellenAsync"/>.</b> Das sind die
        /// drei Stellen des Bestands: der Einwilligungsriegel in
        /// <c>KiChatService</c>, der Aktionsschalter des Chats und - über den Haken -
        /// die Hülle beim Programmstart.
        /// </para>
        /// </remarks>
        public static bool Sicherstellen()
        {
            return !Abgeschaltet && BestaetigteFassung >= FASSUNG;
        }

        // ------------------------------------------------------------------
        // Einwilligungsstufe „Dialogdaten" (Auftrag #200, KI-D-Q2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt die Erklärung, was mit den Feldwerten geschieht, und liefert
        /// <c>true</c>, wenn der Anwender zustimmt. Die Plattformhülle hängt sie beim
        /// Programmstart ein.
        /// </summary>
        /// <remarks>
        /// Bleibt der Haken leer, gibt es keinen Weg zu dieser Einwilligung — und damit
        /// keinen Weg, Feldwerte zu übertragen. Das ist dieselbe Zusage wie bei
        /// <see cref="Nachfragen"/>: Ein Lauf ohne Oberfläche darf nichts an den Anbieter
        /// senden. Der Haken darf nicht werfen; tut er es doch, gilt das als Ablehnung.
        /// </remarks>
        public static Func<Task<bool>> NachfragenDialogdaten { get; set; }

        /// <summary>Bestätigte Fassung der Dialogdaten-Einwilligung; 0 = keine.</summary>
        public static int DialogdatenFassung
        {
            get
            {
                int n;
                string wert = Lesen(REG_DIALOGDATEN);
                return int.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 0;
            }
        }

        /// <summary>Zeitpunkt der Dialogdaten-Einwilligung als Text; leer, wenn keine vorliegt.</summary>
        public static string DialogdatenBestaetigtAm
        {
            get { return Lesen(REG_DIALOGDATEN_AM) ?? ""; }
        }

        /// <summary>
        /// Es liegt eine gültige Einwilligung in die Übertragung von Feldwerten vor und
        /// die KI ist nicht abgeschaltet.
        /// </summary>
        public static bool DialogdatenErteilt
        {
            get { return !Abgeschaltet && DialogdatenFassung >= FASSUNG_DIALOGDATEN; }
        }

        /// <summary>
        /// Lässt sich die Einwilligung hier überhaupt einholen? <c>false</c> heißt: Der
        /// Schalter „Feldwerte mitsenden" bleibt aus und gesperrt, und die Oberfläche
        /// nennt den Grund.
        /// </summary>
        /// <remarks>
        /// Drei Fälle machen sie unmöglich: die abgeschaltete KI, eine fehlende
        /// allgemeine Einwilligung ohne Nachfrageweg und ein Lauf ohne Oberfläche
        /// (Prüfstand, Konsole) — dort ist kein Haken eingehängt.
        /// </remarks>
        public static bool DialogdatenMoeglich
        {
            get
            {
                if (Abgeschaltet) return false;
                if (DialogdatenFassung >= FASSUNG_DIALOGDATEN) return true;
                return NachfragenDialogdaten != null;
            }
        }

        /// <summary>Merkt die Dialogdaten-Einwilligung für die aktuelle Fassung samt Zeitpunkt.</summary>
        public static void DialogdatenErteilen()
        {
            Schreiben(REG_DIALOGDATEN, FASSUNG_DIALOGDATEN.ToString(CultureInfo.InvariantCulture));
            Schreiben(REG_DIALOGDATEN_AM,
                      DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        /// <summary>Nimmt die Dialogdaten-Einwilligung zurück; beim nächsten Mal wird wieder gefragt.</summary>
        public static void DialogdatenZuruecknehmen()
        {
            Loeschen(REG_DIALOGDATEN);
            Loeschen(REG_DIALOGDATEN_AM);
        }

        /// <summary>
        /// Der Riegel vor jeder Übertragung von Feldwerten: <c>true</c>, wenn gesendet
        /// werden darf. Gefragt wird <b>einmal</b> — beim ersten Einschalten des
        /// Schalters; danach entscheidet der abgelegte Merker.
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge ist dieselbe wie bei <see cref="SicherstellenAsync"/>:
        /// Abschalter zuerst, dann die abgelegte Fassung, erst danach die Frage. Die
        /// ALLGEMEINE Einwilligung wird vorher sichergestellt — ohne sie geht ohnehin
        /// keine Anfrage hinaus, und eine Zustimmung zu den Feldwerten allein wäre
        /// wertlos.
        /// </remarks>
        public static async Task<bool> SicherstellenDialogdatenAsync()
        {
            if (Abgeschaltet) return false;
            if (!await SicherstellenAsync().ConfigureAwait(true)) return false;
            if (DialogdatenFassung >= FASSUNG_DIALOGDATEN) return true;

            Func<Task<bool>> frage = NachfragenDialogdaten;
            if (frage == null) return false;

            bool ja;
            try
            {
                Task<bool> lauf = frage();
                ja = lauf != null && await lauf.ConfigureAwait(true);
            }
            catch { return false; }

            if (!ja) return false;

            DialogdatenErteilen();
            return true;
        }

        // ------------------------------------------------------------------
        // Registry (still, ohne Fehlerdialoge - wie in KiChatService)
        // ------------------------------------------------------------------

        private static bool Ist(string wert)
        {
            return string.Equals(wert, "1", StringComparison.Ordinal);
        }

        // Die drei Helfer bedienen unter Windows unveraendert HKCU\Software\wp-plan;
        // der Weg dorthin liegt seit iU5 in Dienste.Einstellungen. Die zwei
        // Registry-Sichten des maschinenweiten Abschalters (WOW6432Node-Umleitung der
        // x86-Zeit) sind dorthin mitgewandert - siehe RegistryEinstellungen.LiesMaschine.

        private static string Lesen(string wert)
        {
            return Dienste.Einstellungen.Lies(wert, null);
        }

        private static void Schreiben(string wert, string inhalt)
        {
            Dienste.Einstellungen.Schreib(wert, inhalt ?? "");
        }

        private static void Loeschen(string wert)
        {
            Dienste.Einstellungen.Loesche(wert);
        }
    }
}
