using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>DIE BESCHRIFTUNG EINER JAHRESZEITACHSE MIT DATUM</b> — die eine Regel für jede
    /// Stundenachse eines Zeitreihenbildes: im Bild des Renderers (Vollansicht und
    /// Ausschnitt), in der nachgezeichneten Achsenteilung der Oberfläche und in der
    /// Zeigerzeile unter dem Bild.
    ///
    /// <para><b>Gemeinjahr ohne Jahreszahl</b> (Konvention des Rechenkerns, wie
    /// <see cref="Feiertage.Gemeinjahrestag"/> und der <c>Gemeinjahrkalender</c>): 365 Tage,
    /// kein Schaltjahr. Das Datum folgt allein aus dem Jahrestag; die Jahresstunde
    /// <c>h</c> beginnt am Tag <c>⌊h / 24⌋</c> (ab 0 = 1. Januar) um <c>h mod 24</c> Uhr —
    /// dieselbe Zählung wie <see cref="Pfadregel.Zeitpunkt"/>, der Index 0 ist der
    /// 1. Januar, 00:00.</para>
    ///
    /// <para><b>Sprache aus dem Ressourcenkatalog, nicht aus der Kultur:</b> Monatskürzel
    /// (<c>CHART_ACHSE_MONATSKUERZEL</c>) und Muster (<c>CHART_ACHSE_DATUM_MUSTER</c>,
    /// de „7. Sep.“, en „Sep 7“) stehen im Katalog. Die Kürzel der Kultur unterscheiden
    /// sich je Plattform (ICU „Sept.“, NLS „Sep“) — die Messlatte der ChartProben hinge
    /// sonst an der Plattformbibliothek.</para>
    /// </summary>
    public static class Zeitachse
    {
        /// <summary>Stunden des Gemeinjahres.</summary>
        public const int STUNDEN_JAHR = 8760;

        /// <summary>
        /// Wie viele Werte eine Stunde trägt, wenn eine Reihe der Länge
        /// <paramref name="gesamt"/> ein ganzes Jahresraster ist: 1 (8 760 Stunden),
        /// 4 (35 040 Viertelstunden), sonst 0 — dann trägt die Achse kein Datum.
        /// </summary>
        public static int WerteJeStunde(int gesamt)
        {
            int jeTag = Pfadregel.WerteJeTag(gesamt);
            return jeTag == 0 ? 0 : jeTag / 24;
        }

        /// <summary>
        /// Die Werte je Stunde einer Zeichenfläche, deren x-Achse die Stützstelle einer
        /// Jahresreihe zählt (<see cref="Achsenart.Stunden"/>, Fenster ab 0); sonst 0.
        /// </summary>
        public static int WerteJeStunde(Zeichenflaeche flaeche)
        {
            if (flaeche == null || flaeche.X != Achsenart.Stunden || flaeche.Daten == null) return 0;
            Datenfenster d = flaeche.Daten;
            if (Math.Abs(d.XVon) > 1e-9) return 0;
            return WerteJeStunde((int)Math.Round(d.XBis - d.XVon) + 1);
        }

        /// <summary>
        /// Das Datum der Jahresstunde <paramref name="stunde"/> im Gemeinjahr, ohne Jahr:
        /// „7. Sep.“ bzw. „Sep 7“. Stunden außerhalb des Jahres werden an den Rand
        /// geklemmt (1. Januar bzw. 31. Dezember).
        /// </summary>
        public static string Datum(double stunde)
        {
            int tag = (int)Math.Floor(Klemmen(stunde) / 24.0);   // 0 … 364
            int monat = 0;
            while (monat < 11 && tag >= Feiertage.TageJeMonat[monat])
            {
                tag -= Feiertage.TageJeMonat[monat];
                monat++;
            }
            string[] kuerzel = (MyResource.Resource.CHART_ACHSE_MONATSKUERZEL ?? "").Split('|');
            string name = kuerzel.Length == 12 ? kuerzel[monat] : (monat + 1).ToString(CultureInfo.InvariantCulture);
            string muster = MyResource.Resource.CHART_ACHSE_DATUM_MUSTER;
            if (string.IsNullOrEmpty(muster)) muster = "{0}. {1}";
            return string.Format(CultureInfo.InvariantCulture, muster,
                                 (tag + 1).ToString(CultureInfo.InvariantCulture), name);
        }

        /// <summary>Die Uhrzeit der Jahresstunde, „23:00“ bzw. „06:15“ (24-Stunden-Zählung in beiden Sprachen).</summary>
        public static string Uhrzeit(double stunde)
        {
            double h = Klemmen(stunde);
            int minuten = (int)Math.Round((h - Math.Floor(h / 24.0) * 24.0) * 60.0);
            if (minuten >= 24 * 60) minuten = 24 * 60 - 1;
            return (minuten / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                   (minuten % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>Die zweite Zeile einer Achsenmarke:</b> das Datum, und wenn der sichtbare
        /// Bereich kürzer als ein Tag ist (<paramref name="spanneStunden"/> &lt; 24), dazu die
        /// Uhrzeit — dann tragen alle Marken dasselbe Datum, und erst die Uhrzeit trennt sie.
        /// </summary>
        public static string Markentext(double stunde, double spanneStunden)
            => spanneStunden < 24.0 ? Datum(stunde) + " " + Uhrzeit(stunde) : Datum(stunde);

        /// <summary>
        /// <b>Die Stelle in der Zeigerzeile:</b> „6.000 h · 8. Sep. 00:00“ — Jahresstunde,
        /// Datum und Uhrzeit. <paramref name="kultur"/> formatiert die Stundenzahl.
        /// </summary>
        public static string Zeigertext(double stunde, string einheit, CultureInfo kultur)
        {
            string zahl = Math.Round(stunde, 2).ToString(Math.Abs(stunde - Math.Round(stunde)) < 1e-9 ? "N0" : "N2",
                                                           kultur ?? CultureInfo.CurrentCulture);
            string kopf = string.IsNullOrEmpty(einheit) ? zahl : zahl + " " + einheit;
            return kopf + " · " + Datum(stunde) + " " + Uhrzeit(stunde);
        }

        private static double Klemmen(double stunde)
        {
            if (double.IsNaN(stunde) || stunde < 0) return 0.0;
            return Math.Min(stunde, STUNDEN_JAHR - 1e-6);
        }

        // =================================================================
        // DIE WAHL "MONAT | TAG | STUNDE" (Anwenderrueckmeldung 13.10.2026)
        //
        // Die Oberflaeche teilt die Zeitachse eines Jahresbildes in der Einheit,
        // die der Anwender am Bild waehlt - in der Vollansicht wie im Ausschnitt.
        // Die Regel steht hier einmal: Marken in GANZEN Einheiten, die Schrittweite
        // so, dass zwei Beschriftungen mindestens MARKE_MIN_PX Bildpunkte
        // auseinanderliegen (keine Ueberlappung), jede Marke mit erster und zweiter
        // Zeile - Monatsname; Tag des Jahres und Datum; Jahresstunde und Datum.
        // =================================================================

        /// <summary>
        /// Eine Marke der Zeitachse: ihre Stelle in Jahresstunden, die Beschriftung und die
        /// zweite Zeile darunter (leer = keine).
        /// </summary>
        public sealed record Achsenmarke(double Stunde, string Text, string Zweitzeile);

        /// <summary>Der kleinste Abstand zweier Marken in Bildpunkten — darunter überlappten sich die Beschriftungen.</summary>
        public const double MARKE_MIN_PX = 72.0;

        private static readonly int[] TAGESSCHRITTE = { 1, 2, 3, 5, 7, 10, 14, 15, 20, 30, 50, 60, 90, 120, 180 };
        private static readonly int[] MONATSSCHRITTE = { 1, 2, 3, 4, 6 };

        /// <summary>
        /// <b>Die Teilung der Zeitachse in einer Einheit</b> für den Ausschnitt
        /// <paramref name="vonStunde"/> … <paramref name="bisStunde"/> (Jahresstunden,
        /// einschließlich) über <paramref name="breitePx"/> Bildpunkte: bei
        /// <see cref="Zeitachseneinheit.Monat"/> die Monatsgrenzen mit Monatskürzel, bei
        /// <see cref="Zeitachseneinheit.Tag"/> die Tagesgrenzen mit Tag des Jahres und Datum,
        /// bei <see cref="Zeitachseneinheit.Stunde"/> die runden Jahresstunden mit Datum
        /// (<c>ChartRenderer.Jahresstundenteilung</c>). Die Schrittweite ist die kleinste der
        /// Einheit, bei der zwei Marken mindestens <see cref="MARKE_MIN_PX"/> auseinanderliegen.
        /// </summary>
        public static IReadOnlyList<Achsenmarke> Teilung(Zeitachseneinheit einheit, double vonStunde,
                                                         double bisStunde, double breitePx)
        {
            var marken = new List<Achsenmarke>();
            if (double.IsNaN(vonStunde) || double.IsNaN(bisStunde)) return marken;
            double spanne = bisStunde - vonStunde;
            if (spanne < 1e-9) return marken;
            if (breitePx <= 0) breitePx = 1000;
            double pxJeStunde = breitePx / spanne;

            switch (einheit)
            {
                case Zeitachseneinheit.Monat:
                    {
                        int schritt = MONATSSCHRITTE[MONATSSCHRITTE.Length - 1];
                        foreach (int s in MONATSSCHRITTE)
                            if (s * 28 * 24 * pxJeStunde >= MARKE_MIN_PX) { schritt = s; break; }
                        string[] kuerzel = (MyResource.Resource.CHART_ACHSE_MONATSKUERZEL ?? "").Split('|');
                        int tag = 0;
                        for (int m = 0; m < 12; m++)
                        {
                            double stunde = tag * 24.0;
                            tag += Feiertage.TageJeMonat[m];
                            if (m % schritt != 0) continue;
                            if (stunde < vonStunde - 1e-9 || stunde > bisStunde + 1e-9) continue;
                            string name = kuerzel.Length == 12 ? kuerzel[m] : (m + 1).ToString(CultureInfo.InvariantCulture);
                            marken.Add(new Achsenmarke(stunde, name, ""));
                        }
                        return marken;
                    }

                case Zeitachseneinheit.Tag:
                    {
                        int schritt = TAGESSCHRITTE[TAGESSCHRITTE.Length - 1];
                        foreach (int s in TAGESSCHRITTE)
                            if (s * 24 * pxJeStunde >= MARKE_MIN_PX) { schritt = s; break; }
                        int erster = (int)Math.Ceiling(vonStunde / 24.0 - 1e-9);
                        int letzter = (int)Math.Floor(bisStunde / 24.0 + 1e-9);
                        for (int t = erster; t <= letzter && t < 365; t++)
                        {
                            if (t < 0 || t % schritt != 0) continue;
                            marken.Add(new Achsenmarke(t * 24.0, (t + 1).ToString("N0", CultureInfo.CurrentCulture),
                                                       Datum(t * 24.0)));
                        }
                        return marken;
                    }

                default:
                    foreach ((int stunde, string text) in
                             ChartRenderer.Jahresstundenteilung((int)Math.Ceiling(vonStunde), (int)Math.Floor(bisStunde)))
                        marken.Add(new Achsenmarke(stunde, text, Markentext(stunde, spanne)));
                    return marken;
            }
        }

        /// <summary>
        /// Der Titel der Zeitachse in der gewählten Einheit: „Monat“, „Tag des Jahres · Datum“,
        /// „Jahresstunden [h] · Datum“ (Ressourcenkatalog).
        /// </summary>
        public static string Achsentitel(Zeitachseneinheit einheit)
        {
            switch (einheit)
            {
                case Zeitachseneinheit.Monat: return MyResource.Resource.CHART_ACHSE_MONAT;
                case Zeitachseneinheit.Tag: return MyResource.Resource.CHART_ACHSE_TAG_DATUM;
                default: return MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN_DATUM;
            }
        }
    }
}
