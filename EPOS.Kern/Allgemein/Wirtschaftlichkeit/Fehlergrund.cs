using System;
using System.Collections.Generic;
using System.Reflection;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7c3 (Befund B‑6, Konzept § 4: „Fehler werden geschluckt, catch {} ⇒ still
    /// 0") — der <b>Grund</b> eines abgefangenen Fehlers als kurzer, einzeiliger Text für
    /// eine Herleitungs- oder Kohärenzzeile („Prüfung „X“ nicht ausführbar: &lt;Grund&gt;“,
    /// „Rechenstufe „X“ nicht ausführbar: &lt;Grund&gt;“).
    ///
    /// <para><b>Warum eine eigene Stelle.</b> Die Zeilen entstehen in vier Klassen
    /// (Kohärenzprüfung, Emissionsbilanz, Emissionsquelle, Wirtschaftlichkeit); jede soll
    /// den Grund gleich nennen — Fehlerart und Meldung, ohne Stapel, ohne Zeilenumbruch,
    /// gekürzt. Eine Zeile, die einen Stapel trüge, wäre im Bericht nicht lesbar.</para>
    /// </summary>
    public static class Fehlergrund
    {
        /// <summary>Höchstlänge des Grundes in Zeichen (danach „…").</summary>
        public const int HOECHSTLAENGE = 200;

        /// <summary>
        /// „Fehlerart: Meldung" — bei einem umhüllten Fehler (Aufruf per Reflexion) der
        /// innere; Zeilenumbrüche werden Leerzeichen, der Text wird auf
        /// <see cref="HOECHSTLAENGE"/> Zeichen gekürzt. <c>null</c> ergibt "".
        /// </summary>
        public static string Text(Exception ex)
        {
            if (ex == null) return "";
            Exception e = ex;
            while ((e is TargetInvocationException || e is AggregateException) && e.InnerException != null)
                e = e.InnerException;

            // Ein Zeilenumbruch wird EIN Leerzeichen — auch der Windows-Umbruch "\r\n".
            string meldung = (e.Message ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
            string text = e.GetType().Name + (meldung.Length > 0 ? ": " + meldung : "");
            return text.Length > HOECHSTLAENGE ? text.Substring(0, HOECHSTLAENGE - 1) + "…" : text;
        }

        /// <summary>
        /// ETAPPE E13 (E7c3‑Q6 a) — die Zeilen, mit denen die Oberfläche die drei Gründe des
        /// Kerns zeigt: <c>WirtschaftlichkeitCtrl.Ladefehler</c> („Gespeicherte Ergebnisse
        /// nicht vollständig gelesen: …"), <c>Speicherfehler</c> („Speichern gescheitert: …")
        /// und <c>Vorsorgewarnung</c> („Tabellenvorsorge unvollständig: …"). Statuszeile der
        /// Ergebnisseite und BHKW-Dialog lesen dieselben Zeilen.
        ///
        /// <para><b>Ein Grund erscheint einmal.</b> Derselbe Grund aus zwei Quellen (etwa
        /// eine fehlende Tabelle, an der Vorsorge und Laden scheitern) ergibt EINE Zeile, in
        /// der Reihenfolge Laden, Speichern, Vorsorge; leere Gründe ergeben keine. Der Text
        /// ist der des Kerns (<see cref="Text"/>: Art und Meldung, ohne Stapel).</para>
        /// </summary>
        public static List<string> Anzeigezeilen(IEnumerable<string> ladefehler, string speicherfehler,
                                                 string vorsorgewarnung)
        {
            var zeilen = new List<string>();
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            void Neu(string vorlage, string grund)
            {
                if (string.IsNullOrWhiteSpace(grund) || !gesehen.Add(grund.Trim())) return;
                zeilen.Add(string.Format(vorlage, grund.Trim()));
            }

            if (ladefehler != null)
                foreach (string g in ladefehler) Neu(MyResource.Resource.WIRT_STATUS_LADEFEHLER, g);
            Neu(MyResource.Resource.WIRT_STATUS_SPEICHERFEHLER, speicherfehler);
            Neu(MyResource.Resource.WIRT_STATUS_VORSORGE, vorsorgewarnung);
            return zeilen;
        }
    }
}
