using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die vier Kennzahlen der Kennzahlenzeile über den Grafikblättern einer Kältezeile (Konzept Kältebedarfsprofile,
    /// Abschnitt 5): Jahressumme [MWh/a], Spitze [kW], Vollbenutzungsstunden [h/a] und Anteil der Grundlast an der
    /// Jahresmenge [%].
    /// </summary>
    /// <param name="JahressummeMwh">Summe der Stundenwerte [kWh] / 1000.</param>
    /// <param name="SpitzeKw">Größter Stundenwert [kWh je Stunde = kW].</param>
    /// <param name="VollbenutzungsstundenH">Jahresmenge durch Spitze [h/a]; 0 ohne Spitze.</param>
    /// <param name="GrundlastKw">Kleinster Stundenwert [kW] — die Grundlast.</param>
    /// <param name="GrundlastAnteilProzent">Grundlast mal Stundenzahl durch Jahresmenge [%]; 0 ohne Jahresmenge.</param>
    public sealed record Kaelteprofilkennzahlen(double JahressummeMwh, double SpitzeKw, double VollbenutzungsstundenH,
                                                double GrundlastKw, double GrundlastAnteilProzent);

    /// <summary>
    /// Was die Grafik einer Kältezeile zeigt: die Kennzahlenzeile und die vier Zeichenmodelle der Blätter Jahresgang,
    /// Monate, Woche und Dauerlinie. Die Modelle sind die des Berichtswegs (<see cref="ChartRenderer"/>); die
    /// Oberfläche zeigt sie nach der Oberflächenregel (<c>DiagrammSvg</c>).
    /// </summary>
    public sealed record Kaelteprofilbilder(Kaelteprofilkennzahlen Kennzahlen, Zeichenmodell Jahresgang,
                                            Zeichenmodell Monate, Zeichenmodell Woche, Zeichenmodell Dauerlinie);

    /// <summary>
    /// <b>Die Grafik der Kältebedarfsprofile</b> (Konzept Kältebedarfsprofile, Abschnitt 5; Welle KBP-3b) — ohne
    /// Datenbank und ohne Oberfläche: Kennzahlen, Monatssummen, typische Woche nach Tagesarten, Dauerlinie und die vier
    /// Zeichenmodelle. Es entsteht KEIN neues Bild im Renderer: Jahresgang ist
    /// <see cref="ChartRenderer.JahresverlaufModell(string, double[], string, Farbrolle, Achsenfenster)"/> (Zeitachse
    /// des Jahresrasters, also Tagesband und Wahl „Datum | Stunde im Jahr“ von selbst), Monate sind
    /// <see cref="ChartRenderer.MonatsSaeulenModell(string, double[], Farbrolle, string, IReadOnlyList{string})"/> bzw.
    /// bei mehreren Zeilen <see cref="ChartRenderer.MonatsStapelModell"/> (Kältebilder sind Stapel), die Woche ist
    /// <see cref="ChartRenderer.StundenprofileModell"/> und die Dauerlinie <see cref="ChartRenderer.SummenlinieModell"/>
    /// (x = Rang, keine Zeitachse, kein Band). Bericht und ChartProben bleiben unberührt.
    /// </summary>
    public static class Kaelteprofilgrafik
    {
        /// <summary>Tagesart „Werktag“ (Montag bis Freitag außerhalb von Ferien und Feiertagen).</summary>
        public const byte WERKTAG = 0;

        /// <summary>Tagesart „Samstag“.</summary>
        public const byte SAMSTAG = 1;

        /// <summary>Tagesart „Sonntag bzw. Feiertag“.</summary>
        public const byte SONNTAG = 2;

        /// <summary>Tagesart „Ferien“ (Betriebsferien des Kalenders; gehen Feiertagen und Wochentagen vor).</summary>
        public const byte FERIEN = 3;

        /// <summary>Die Zahl der Tagesarten.</summary>
        public const int TAGESARTEN = 4;

        private const int TAGE = 365;
        private const int STUNDEN = 8760;

        /// <summary>Die Farbe der Kälte: die Bedarfsrolle wie die Kältevorschau des Ergebnisdialogs, keine neue Rolle.</summary>
        public static readonly Farbrolle ROLLE = Farbrolle.BEDARF;

        /// <summary>Die Farben der übrigen Zeilen im Monatsstapel, in Zeilenfolge nach der ersten.</summary>
        private static readonly Farbrolle[] ROLLEN_STAPEL =
        {
            Farbrolle.SERIE_1, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4,
            Farbrolle.SERIE_5, Farbrolle.SERIE_6, Farbrolle.SERIE_7, Farbrolle.SERIE_8
        };

        // =================================================================================
        // Rechnung
        // =================================================================================

        /// <summary>
        /// Die vier Kennzahlen einer Stundenreihe [kWh je Stunde]. Nicht endliche Werte zählen nicht; eine leere Reihe
        /// gibt lauter Nullen.
        /// </summary>
        public static Kaelteprofilkennzahlen Kennzahlen(double[] stundeKwh)
        {
            double summe = 0, spitze = 0, minimum = double.PositiveInfinity;
            int n = 0;
            foreach (double w in stundeKwh ?? Array.Empty<double>())
            {
                if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                summe += w;
                if (w > spitze) spitze = w;
                if (w < minimum) minimum = w;
                n++;
            }
            if (n == 0) return new Kaelteprofilkennzahlen(0, 0, 0, 0, 0);
            double grundlast = Math.Max(0.0, minimum);
            double vbh = spitze > 0 ? summe / spitze : 0.0;
            double anteil = summe > 0 ? grundlast * n / summe * 100.0 : 0.0;
            return new Kaelteprofilkennzahlen(summe / 1000.0, spitze, vbh, grundlast, anteil);
        }

        /// <summary>Die zwölf Monatssummen [MWh] einer Stundenreihe [kWh] im Gemeinjahr.</summary>
        public static double[] Monatssummen(double[] stundeKwh)
        {
            var monat = new double[12];
            if (stundeKwh == null) return monat;
            int stunde = 0;
            for (int m = 0; m < 12; m++)
            {
                int ende = stunde + Feiertage.TageJeMonat[m] * 24;
                double s = 0;
                for (; stunde < ende && stunde < stundeKwh.Length; stunde++)
                {
                    double w = stundeKwh[stunde];
                    if (!double.IsNaN(w) && !double.IsInfinity(w)) s += w;
                }
                monat[m] = s / 1000.0;
            }
            return monat;
        }

        /// <summary>
        /// Die Tagesart jedes der 365 Tage: Ferien vor Feiertag vor Wochentag. <paramref name="wochentagJan1"/> ist der
        /// Wochentag des 1. Januar im Wochentagsraster des Laufs (0 = Montag … 6 = Sonntag);
        /// <paramref name="kalenderarten"/> die Tagesarten des Betriebskalenders
        /// (<see cref="Betriebskalender.Tagesarten"/>), <c>null</c> = kein Kalender — dann zählen nur die Wochentage.
        /// </summary>
        public static byte[] Tagesklassen(int wochentagJan1, byte[] kalenderarten)
        {
            var klassen = new byte[TAGE];
            int w0 = ((wochentagJan1 % 7) + 7) % 7;
            for (int d = 0; d < TAGE; d++)
            {
                byte k = kalenderarten != null && d < kalenderarten.Length ? kalenderarten[d] : Betriebskalender.TAG_NORMAL;
                if (k == Betriebskalender.TAG_FERIEN) { klassen[d] = FERIEN; continue; }
                if (k == Betriebskalender.TAG_FEIERTAG) { klassen[d] = SONNTAG; continue; }
                int wt = (w0 + d) % 7;
                klassen[d] = wt == 6 ? SONNTAG : wt == 5 ? SAMSTAG : WERKTAG;
            }
            return klassen;
        }

        /// <summary>
        /// Die mittleren Tagesgänge [kW] je Tagesart (Index <see cref="WERKTAG"/> … <see cref="FERIEN"/>), je 24 Werte;
        /// eine Tagesart ohne Tag ist <c>null</c>.
        /// </summary>
        public static double[][] Tagesgaenge(double[] stundeKwh, byte[] tagesklassen)
        {
            var summen = new double[TAGESARTEN][];
            var zahl = new int[TAGESARTEN];
            if (stundeKwh == null || tagesklassen == null) return new double[TAGESARTEN][];
            for (int d = 0; d < TAGE && d < tagesklassen.Length && (d + 1) * 24 <= stundeKwh.Length; d++)
            {
                int k = tagesklassen[d];
                if (k >= TAGESARTEN) continue;
                summen[k] ??= new double[24];
                for (int h = 0; h < 24; h++)
                {
                    double w = stundeKwh[d * 24 + h];
                    if (!double.IsNaN(w) && !double.IsInfinity(w)) summen[k][h] += w;
                }
                zahl[k]++;
            }
            for (int k = 0; k < TAGESARTEN; k++)
                if (summen[k] != null)
                    for (int h = 0; h < 24; h++) summen[k][h] /= zahl[k];
            return summen;
        }

        /// <summary>
        /// Die typische Woche [kW] aus den Tagesgängen, 168 Stunden ab Montag 0 Uhr: Montag bis Freitag der Werktag,
        /// Samstag der Samstag, Sonntag der Sonntag bzw. Feiertag. Fehlt eine Tagesart, steht dort 0.
        /// </summary>
        public static double[] TypischeWoche(double[][] tagesgaenge)
        {
            var woche = new double[168];
            for (int t = 0; t < 7; t++)
            {
                byte art = t == 6 ? SONNTAG : t == 5 ? SAMSTAG : WERKTAG;
                double[] tag = tagesgaenge != null && art < tagesgaenge.Length ? tagesgaenge[art] : null;
                if (tag == null) continue;
                Array.Copy(tag, 0, woche, t * 24, 24);
            }
            return woche;
        }

        /// <summary>Die Woche aus sieben gleichen Ferientagen [kW]; <c>null</c>, wenn das Jahr keinen Ferientag hat.</summary>
        public static double[] Ferienwoche(double[][] tagesgaenge)
        {
            double[] tag = tagesgaenge != null && tagesgaenge.Length > FERIEN ? tagesgaenge[FERIEN] : null;
            if (tag == null) return null;
            var woche = new double[168];
            for (int t = 0; t < 7; t++) Array.Copy(tag, 0, woche, t * 24, 24);
            return woche;
        }

        /// <summary>Die Dauerlinie: die endlichen Stundenwerte absteigend.</summary>
        public static double[] Dauerlinie(double[] stundeKwh)
            => (stundeKwh ?? Array.Empty<double>())
               .Where(w => !double.IsNaN(w) && !double.IsInfinity(w))
               .OrderByDescending(w => w)
               .ToArray();

        // =================================================================================
        // Zeichenmodelle
        // =================================================================================

        /// <summary>
        /// Die Kennzahlen und die vier Bilder einer Kältezeile. <paramref name="stundeKwh"/> ist die Reihe der markierten
        /// Zeile; <paramref name="zeilen"/> alle Zeilen des Projekts für das Monatsblatt (mehr als eine: Stapel je
        /// Zeile, sonst ein Balken der markierten Zeile).
        /// </summary>
        public static Kaelteprofilbilder Bilder(double[] stundeKwh, IReadOnlyList<(string Name, double[] StundeKwh)> zeilen,
                                                int wochentagJan1, byte[] kalenderarten)
        {
            double[] reihe = stundeKwh ?? new double[STUNDEN];
            double[][] gaenge = Tagesgaenge(reihe, Tagesklassen(wochentagJan1, kalenderarten));
            return new Kaelteprofilbilder(Kennzahlen(reihe), JahresgangModell(reihe), MonateModell(reihe, zeilen),
                                          WocheModell(gaenge), DauerlinieModell(reihe));
        }

        /// <summary>Das Blatt Jahresgang: die 8 760 Stundenwerte auf der Zeitachse des Jahresrasters.</summary>
        public static Zeichenmodell JahresgangModell(double[] stundeKwh)
            => ChartRenderer.JahresverlaufModell(MyResource.Resource.KBP_TITEL_JAHRESGANG, stundeKwh ?? new double[STUNDEN],
                                                 MyResource.Resource.KBP_ACHSE_LEISTUNG, ROLLE);

        /// <summary>Das Blatt Monate: ein Balken je Monat, bei mehreren Zeilen gestapelt je Zeile [MWh].</summary>
        public static Zeichenmodell MonateModell(double[] stundeKwh, IReadOnlyList<(string Name, double[] StundeKwh)> zeilen)
        {
            string titel = MyResource.Resource.KBP_TITEL_MONATE;
            if (zeilen != null && zeilen.Count > 1)
            {
                var reihen = new List<ChartRenderer.Reihe>(zeilen.Count);
                for (int i = 0; i < zeilen.Count; i++)
                {
                    Farbrolle rolle = i == 0 ? ROLLE : ROLLEN_STAPEL[(i - 1) % ROLLEN_STAPEL.Length];
                    reihen.Add(new ChartRenderer.Reihe(zeilen[i].Name ?? "", Monatssummen(zeilen[i].StundeKwh), rolle,
                                                       ChartRenderer.Stapelart.Saeule));
                }
                return ChartRenderer.MonatsStapelModell(titel, Energieeinheit.MWh.Text, reihen);
            }
            string[] namen = (MyResource.Resource.CHART_ACHSE_MONATSKUERZEL ?? "").Split('|');
            return ChartRenderer.MonatsSaeulenModell(titel, Monatssummen(stundeKwh), ROLLE, Energieeinheit.MWh.Text,
                                                     namen.Length == 12 ? namen : null);
        }

        /// <summary>
        /// Das Blatt Woche: die typische Woche (Werktag, Samstag, Sonntag bzw. Feiertag) als Fläche, dazu — wenn der
        /// Kalender Ferien trägt — der Ferientag als Linie über alle sieben Tage.
        /// </summary>
        public static Zeichenmodell WocheModell(double[][] tagesgaenge)
        {
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(MyResource.Resource.KBP_REIHE_WOCHE, TypischeWoche(tagesgaenge), ROLLE)
            };
            double[] ferien = Ferienwoche(tagesgaenge);
            if (ferien != null)
                reihen.Add(new ChartRenderer.Reihe(MyResource.Resource.KBP_REIHE_FERIEN, ferien, Farbrolle.SERIE_2,
                                                   ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt));
            return ChartRenderer.StundenprofileModell(MyResource.Resource.KBP_TITEL_WOCHE, reihen, 24,
                                                      MyResource.Resource.KBP_ACHSE_WOCHENSTUNDE,
                                                      MyResource.Resource.KBP_ACHSE_LEISTUNG);
        }

        /// <summary>Das Blatt Dauerlinie: die Stundenwerte absteigend über dem Rang 1 … n, als Fläche, ohne Zeitachse.</summary>
        public static Zeichenmodell DauerlinieModell(double[] stundeKwh)
        {
            double[] sortiert = Dauerlinie(stundeKwh);
            var x = new double[sortiert.Length];
            for (int i = 0; i < x.Length; i++) x[i] = i + 1;
            var reihen = new List<ChartRenderer.Reihe>();
            if (sortiert.Length > 0)
                reihen.Add(new ChartRenderer.Reihe(MyResource.Resource.KBP_REIHE_KAELTE, sortiert, ROLLE,
                                                   ChartRenderer.Stapelart.Flaeche));
            return ChartRenderer.SummenlinieModell(MyResource.Resource.KBP_TITEL_DAUERLINIE, reihen, x, 1000.0, 1.0,
                                                   MyResource.Resource.KBP_ACHSE_RANG, MyResource.Resource.KBP_ACHSE_LEISTUNG);
        }
    }
}
