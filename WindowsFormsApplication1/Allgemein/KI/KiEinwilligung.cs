using System;
using System.Globalization;
using Microsoft.Win32;

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
    /// <c>Form_KiHinweis</c>, das sich beim Programmstart über
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
        // Einhängepunkt der Oberfläche
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt den vollständigen Hinweis und liefert <c>true</c>, wenn der Anwender
        /// zugestimmt hat. Wird beim Programmstart von <c>Form_KiHinweis</c> gesetzt.
        /// </summary>
        /// <remarks>
        /// Bleibt der Haken leer, gibt es keinen Weg zu einer Einwilligung. Das ist
        /// Absicht: ein Lauf ohne Oberfläche darf nichts an den Anbieter senden.
        /// Der Haken darf nicht werfen; tut er es doch, gilt das als Ablehnung.
        /// </remarks>
        public static Func<bool> Nachfragen { get; set; }

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
            get
            {
                return Ist(LesenMaschine(RegistryView.Registry64, REG_ABSCHALTER))
                    || Ist(LesenMaschine(RegistryView.Registry32, REG_ABSCHALTER));
            }
        }

        /// <summary>
        /// Alle KI-Funktionen sind abgeschaltet. Gesetzt wird benutzerbezogen (HKCU);
        /// ein maschinenweiter Abschalter (HKLM) überstimmt jede Einstellung.
        /// </summary>
        public static bool Abgeschaltet
        {
            get { return AbschalterMaschine || Ist(Lesen(Registry.CurrentUser, REG_ABSCHALTER)); }
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
                string wert = Lesen(Registry.CurrentUser, REG_BESTAETIGT);
                return int.TryParse(wert, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 0;
            }
        }

        /// <summary>Zeitpunkt der Bestätigung als Text; leer, wenn keine vorliegt.</summary>
        public static string BestaetigtAm
        {
            get { return Lesen(Registry.CurrentUser, REG_BESTAETIGT_AM) ?? ""; }
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
        public static bool Sicherstellen()
        {
            if (Abgeschaltet) return false;
            if (BestaetigteFassung >= FASSUNG) return true;

            Func<bool> frage = Nachfragen;
            if (frage == null) return false;

            bool ja;
            try { ja = frage(); }
            catch { return false; }

            if (!ja) return false;

            Erteilen();
            return true;
        }

        // ------------------------------------------------------------------
        // Registry (still, ohne Fehlerdialoge - wie in KiChatService)
        // ------------------------------------------------------------------

        private static bool Ist(string wert)
        {
            return string.Equals(wert, "1", StringComparison.Ordinal);
        }

        private static string Lesen(RegistryKey wurzel, string wert)
        {
            try
            {
                using (RegistryKey key = wurzel.OpenSubKey(REG_SCHLUESSEL))
                    return key == null ? null : key.GetValue(wert) as string;
            }
            catch { return null; }
        }

        /// <summary>
        /// Liest aus HKLM in einer ausdrücklich gewählten Registry-Sicht. Auf
        /// 32-bit-Windows liefert <see cref="RegistryView.Registry64"/> die einzige
        /// vorhandene Sicht - ein Sonderfall ist deshalb nicht nötig, doppeltes Lesen
        /// derselben Sicht schadet nicht.
        /// </summary>
        private static string LesenMaschine(RegistryView sicht, string wert)
        {
            try
            {
                using (RegistryKey basis = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, sicht))
                using (RegistryKey key = basis.OpenSubKey(REG_SCHLUESSEL))
                    return key == null ? null : key.GetValue(wert) as string;
            }
            catch { return null; }
        }

        private static void Schreiben(string wert, string inhalt)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                    if (key != null) key.SetValue(wert, inhalt ?? "");
            }
            catch { }
        }

        private static void Loeschen(string wert)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL, true))
                    if (key != null && key.GetValue(wert) != null) key.DeleteValue(wert, false);
            }
            catch { }
        }
    }
}
