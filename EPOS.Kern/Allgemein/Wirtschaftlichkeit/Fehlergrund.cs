using System;
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

            string meldung = (e.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            string text = e.GetType().Name + (meldung.Length > 0 ? ": " + meldung : "");
            return text.Length > HOECHSTLAENGE ? text.Substring(0, HOECHSTLAENGE - 1) + "…" : text;
        }
    }
}
