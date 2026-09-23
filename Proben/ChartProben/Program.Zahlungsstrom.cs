using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>ETAPPE E8a — DAS ZAHLUNGSSTROMBILD</b> „Zahlungsstrom je Jahr" (Mockup-Anhang U42,
    /// Anwenderentscheid E8a‑Q1 vom 23.09.2026, Lesart a: gestapelte Jahresbalken des absoluten
    /// Zahlungsstroms EINER Version, Reihen wie die Spalten der Mehrjahrestafel, Ausgaben nach
    /// unten, Ersatzjahre markiert, keine Differenzdarstellung).
    ///
    /// <para><b>Was hier gezeichnet wird.</b> <c>ChartRenderer.Zahlungsstrom</c>: je Jahr 0…T die
    /// Positionen der Tafel, die Einnahmen von der Nulllinie nach oben, die Ausgaben nach unten,
    /// beide in der Reihenfolge der Tafel; die Ersatzjahre als Band hinter ihrem Balken mit einem
    /// Dreieck am oberen Rand. Die erste Probe trägt einen BHKW-Stand mit allen Spaltenarten
    /// (KWK-Zuschlag, der im Jahr 7 ausläuft, Steuergutschriften, PV-Vergütung, Pauschale im
    /// Jahr 0, drei Ersatzjahre), die zweite eine PV-Anlage über zehn Jahre ohne Ersatz.</para>
    ///
    /// <para><b>Die Texte sind die deutsche Vorgabe</b> (<c>ZahlungsstromTexte</c>) — kein
    /// Probebild hängt an der Oberflächensprache des Rechners. Die Beträge wachsen durch
    /// fortgesetzte Multiplikation, nicht über <c>Math.Pow</c>: So sind sie auf jedem Rechner
    /// bitgleich.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung
    /// nicht sehen: dass jedes Ersatzjahr seine Marke zeichnet und dass das Vorzeichen die Seite
    /// der Nulllinie entscheidet — und im SVG, dass die Schichten eines Jahres lückenlos von der
    /// Nulllinie aus gestapelt sind, die Einnahmen darüber, die Ausgaben darunter, in der
    /// Reihenfolge der Tafel, und dass genau die Ersatzjahre eine Marke tragen.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Ersatzjahre der BHKW-Probe.</summary>
        private static readonly int[] BHKW_ERSATZ = { 8, 15, 16 };

        /// <summary>
        /// Die Proben des Zahlungsstrombilds — eine Zeile in <c>Program.cs</c> ruft sie.
        /// </summary>
        private static void ZahlungsstromProben(string ziel)
        {
            ChartRenderer.ZahlungsstromTexte texte = Zahlungsstromtexte();

            // ---- Maßproben -------------------------------------------------------

            // Ein BHKW-Stand über zwanzig Jahre: die Investition in der Hausfarbe unter der
            // Nulllinie, die Energiekosten rot, der KWK-Zuschlag petrol, die Ersatzjahre mit Dreieck.
            Pruefe(ziel, "zahlungsstrom", ChartRenderer.ZAHLUNGSSTROM_BREITE, ChartRenderer.ZAHLUNGSSTROM_HOEHE,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_SERIEN[6], ChartRenderer.C_SERIEN[5],
                           ChartRenderer.C_RASTER_SCHLECHT },
                   () => ChartRenderer.Zahlungsstrom(Bhkwreihen(), BHKW_ERSATZ, texte));

            // Eine PV-Anlage über zehn Jahre ohne Ersatz: keine Marke, das Bildmaß bleibt.
            Pruefe(ziel, "zahlungsstrom_ohne_ersatz", ChartRenderer.ZAHLUNGSSTROM_BREITE, ChartRenderer.ZAHLUNGSSTROM_HOEHE,
                   new[] { ChartRenderer.C_STAMM, ChartRenderer.C_SERIEN[0], ChartRenderer.C_SERIEN[7] },
                   () => ChartRenderer.Zahlungsstrom(Pvreihen(), new int[0], PvTexte()));

            // Kein zeichenbarer Betrag (nur 0 und nicht endlich): der Leerhinweis an der Stelle des Bildes.
            Pruefe(ziel, "zahlungsstrom_leer", ChartRenderer.ZAHLUNGSSTROM_BREITE, 200,
                   new SKColor[0],
                   () => ChartRenderer.Zahlungsstrom(new List<ChartRenderer.Zahlungsstromreihe>
                   {
                       Zahlungsspalte("ENERGIE", "ohne Betrag", new[] { 0.0, double.NaN })
                   }, new[] { 1 }, texte));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens das ERSATZJAHR: dieselben Reihen, eines der drei Ersatzjahre fehlt in der
            // Liste. Ein Renderer, der nur den Schlüssel unter der Achse zeichnete, sähe gleich aus.
            Unterschiedlich("zahlungsstrom_ersatzjahr_wirkt",
                () => ChartRenderer.Zahlungsstrom(Bhkwreihen(), BHKW_ERSATZ, texte),
                () => ChartRenderer.Zahlungsstrom(Bhkwreihen(), new[] { 8, 15 }, texte));

            // Zweitens das VORZEICHEN: dieselben Beträge, die CO₂-Abgabe als Einnahme statt als
            // Ausgabe. Ein Renderer, der jeden Betrag ohne Vorzeichen nach oben stapelte, bestünde
            // jede Maß- und Farbprüfung und zeichnete beide gleich.
            Unterschiedlich("zahlungsstrom_vorzeichen_wirkt",
                () => ChartRenderer.Zahlungsstrom(Bhkwreihen(), BHKW_ERSATZ, texte),
                () =>
                {
                    List<ChartRenderer.Zahlungsstromreihe> r = Bhkwreihen();
                    ChartRenderer.Zahlungsstromreihe co2 = r.Single(x => x.Schluessel == "BEHG");
                    co2.JeJahr = co2.JeJahr.Select(w => -w).ToArray();
                    return ChartRenderer.Zahlungsstrom(r, BHKW_ERSATZ, texte);
                });

            // ---- SVG: dasselbe Modell auf dem Bildschirmweg ----------------------

            SvgPixelbildprobe("zahlungsstrom",
                () => ChartRenderer.ZahlungsstromModell(Bhkwreihen(), BHKW_ERSATZ, texte));
            ZahlungsstromStapelprobe("svg_zahlungsstrom_stapel", Bhkwreihen(), BHKW_ERSATZ, texte);
            ZahlungsstromStapelprobe("svg_zahlungsstrom_stapel_ohne_ersatz", Pvreihen(), new int[0], PvTexte());

            // Sichtprüfung (--svg-alle): beide Modelle als Dateien neben den Skia-PNG.
            if (_svgordner != null)
                SvgOrdnerSchreiben(new List<KeyValuePair<string, Func<Zeichenmodell>>>
                {
                    new KeyValuePair<string, Func<Zeichenmodell>>("zahlungsstrom",
                        () => ChartRenderer.ZahlungsstromModell(Bhkwreihen(), BHKW_ERSATZ, texte)),
                    new KeyValuePair<string, Func<Zeichenmodell>>("zahlungsstrom_ohne_ersatz",
                        () => ChartRenderer.ZahlungsstromModell(Pvreihen(), new int[0], PvTexte()))
                });
        }

        /// <summary>
        /// <b>Die Schichten bilden Stapel</b> — im SVG nachgemessen: Je Jahr steht die erste
        /// Einnahme auf der Nulllinie und jede weitere auf der vorigen, die erste Ausgabe hängt an
        /// der Nulllinie und jede weitere an der vorigen, beide in der Reihenfolge der Tafel. Jede
        /// Schicht nennt Jahr, Spalte und Betrag am Element; genau die Ersatzjahre tragen eine
        /// Marke.
        /// </summary>
        private static void ZahlungsstromStapelprobe(string name, List<ChartRenderer.Zahlungsstromreihe> reihen,
                                                     int[] ersatzjahre, ChartRenderer.ZahlungsstromTexte texte)
        {
            SvgProbe(name, e =>
            {
                Zeichenmodell m = ChartRenderer.ZahlungsstromModell(reihen, ersatzjahre, texte);
                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);
                CultureInfo de = CultureInfo.GetCultureInfo("de-DE");

                SvgKnoten null0 = alle.FirstOrDefault(
                    k => k.Name == "line" && Attributwert(k, "data-marke") == "nulllinie");
                if (null0 == null) { e.Maengel.Add("keine Nulllinie im Baum"); return; }
                double y0 = Zahl(Attributwert(null0, "y1"));

                int jahre = reihen.Max(r => r.JeJahr.Length);
                for (int t = 0; t < jahre; t++)
                {
                    double oben = y0, unten = y0;
                    foreach (ChartRenderer.Zahlungsstromreihe r in reihen)
                    {
                        double w = t < r.JeJahr.Length ? r.JeJahr[t] : 0.0;
                        if (w == 0.0 || double.IsNaN(w) || double.IsInfinity(w)) continue;
                        string wert = texte.Jahr + " " + t.ToString(de) + " · " + r.Name + ": " +
                                      w.ToString("#,##0;−#,##0;0", de) + " €";
                        SvgKnoten k = alle.FirstOrDefault(
                            x => x.Name == "rect" && Attributwert(x, "data-marke") == "reihe:" + r.Name &&
                                 Attributwert(x, "data-wert") == wert);
                        if (k == null) { e.Maengel.Add("keine Schicht „" + wert + "“"); return; }
                        double y = Zahl(Attributwert(k, "y")), h = Zahl(Attributwert(k, "height"));
                        if (w > 0.0)
                        {
                            if (Math.Abs(y + h - oben) > 0.5)
                                e.Maengel.Add("die Einnahme „" + wert + "“ steht nicht auf der vorigen Schicht");
                            oben = y;
                        }
                        else
                        {
                            if (Math.Abs(y - unten) > 0.5)
                                e.Maengel.Add("die Ausgabe „" + wert + "“ hängt nicht an der vorigen Schicht");
                            unten = y + h;
                        }
                    }
                }

                // Die Ersatzjahre: je Jahr eine Marke, sonst keine.
                List<string> marken = alle
                    .Where(k => Attributwert(k, "data-marke") == "marke" && Attributwert(k, "data-wert") != null)
                    .Select(k => Attributwert(k, "data-wert")).Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal).ToList();
                List<string> soll = ersatzjahre
                    .Select(t => texte.Jahr + " " + t.ToString(de) + ": " + texte.Ersatzjahr)
                    .OrderBy(x => x, StringComparer.Ordinal).ToList();
                if (!marken.SequenceEqual(soll, StringComparer.Ordinal))
                    e.Maengel.Add("die Ersatzjahr-Marken sind [" + string.Join(", ", marken) + "] statt [" +
                                  string.Join(", ", soll) + "]");
            });
        }

        /// <summary>Die Texte der BHKW-Probe: die deutsche Vorgabe samt Unterzeile.</summary>
        private static ChartRenderer.ZahlungsstromTexte Zahlungsstromtexte() => new ChartRenderer.ZahlungsstromTexte
        {
            Unterzeile = "BHKW + Photovoltaik · Szenario Erwartet · nominal je Jahr, Ausgaben nach unten, ohne Restwert"
        };

        /// <summary>Die Texte der PV-Probe.</summary>
        private static ChartRenderer.ZahlungsstromTexte PvTexte() => new ChartRenderer.ZahlungsstromTexte
        {
            Unterzeile = "Photovoltaik · Szenario Günstig · nominal je Jahr, Ausgaben nach unten, ohne Restwert"
        };

        /// <summary>Eine Spalte der Probe.</summary>
        private static ChartRenderer.Zahlungsstromreihe Zahlungsspalte(string schluessel, string name, double[] werte)
            => new ChartRenderer.Zahlungsstromreihe { Schluessel = schluessel, Name = name, JeJahr = werte };

        /// <summary>
        /// Ein Betrag ab Jahr 1, jedes Jahr um <paramref name="faktor"/> fortgeschrieben — durch
        /// Multiplikation, damit er auf jedem Rechner bitgleich ist.
        /// </summary>
        private static double[] Fortgeschrieben(int jahre, double jahr1, double faktor)
        {
            var werte = new double[jahre + 1];
            double w = jahr1;
            for (int t = 1; t <= jahre; t++) { werte[t] = w; w *= faktor; }
            return werte;
        }

        /// <summary>
        /// Der BHKW-Stand der Probe (T = 20, €): Investition 1,45 Mio. und drei Ersatzjahre,
        /// Betrieb +2 %/a, Energie +3 %/a, CO₂-Abgabe steigend; Einspeisung, KWK-Zuschlag bis
        /// Jahr 7 (dort halb), Energiesteuer-Gutschrift, Stromsteuer-Befreiung, PV-Vergütung,
        /// die Pauschale im Jahr 0 — dieselben Spalten und Schlüssel wie die Mehrjahrestafel.
        /// </summary>
        private static List<ChartRenderer.Zahlungsstromreihe> Bhkwreihen()
        {
            const int T = 20;
            var invest = new double[T + 1];
            invest[0] = -1450000.0;
            invest[8] = -95000.0;
            invest[15] = -260000.0;
            invest[16] = -95000.0;
            var co2 = new double[T + 1];
            var kwk = new double[T + 1];
            for (int t = 1; t <= T; t++)
            {
                co2[t] = -21000.0 - 1000.0 * (t - 1);
                kwk[t] = t <= 6 ? 96000.0 : t == 7 ? 48000.0 : 0.0;
            }
            var pauschale = new double[T + 1];
            pauschale[0] = 25000.0;
            return new List<ChartRenderer.Zahlungsstromreihe>
            {
                Zahlungsspalte(ChartRenderer.Zahlungsstromreihe.INVEST_ERSATZ, "Investition und Ersatz", invest),
                Zahlungsspalte("BETRIEB", "Betriebskosten", Fortgeschrieben(T, -38000.0, 1.02)),
                Zahlungsspalte("ENERGIE", "Energiekosten", Fortgeschrieben(T, -182000.0, 1.03)),
                Zahlungsspalte("BEHG", "CO₂-Abgabe", co2),
                Zahlungsspalte("EINSPEISUNG", "Einspeiseerlös", Fortgeschrieben(T, 64000.0, 1.0)),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.KWKG, "KWK-Zuschlag", kwk),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.ENERGIESTEUER, "Energiesteuer-Gutschrift",
                               Fortgeschrieben(T, 7500.0, 1.0)),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG, "Stromsteuer-Befreiung",
                               Fortgeschrieben(T, 11000.0, 1.0)),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.PV_VERGUETUNG, "PV-Vergütung (EEG)",
                               Fortgeschrieben(T, 9000.0, 1.0)),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, "KWKG-Pauschale (Jahr 0)", pauschale)
            };
        }

        /// <summary>Die PV-Anlage der Probe (T = 10, €): Investition, Betrieb, PV-Vergütung.</summary>
        private static List<ChartRenderer.Zahlungsstromreihe> Pvreihen()
        {
            const int T = 10;
            var invest = new double[T + 1];
            invest[0] = -180000.0;
            return new List<ChartRenderer.Zahlungsstromreihe>
            {
                Zahlungsspalte(ChartRenderer.Zahlungsstromreihe.INVEST_ERSATZ, "Investition und Ersatz", invest),
                Zahlungsspalte("BETRIEB", "Betriebskosten", Fortgeschrieben(T, -2500.0, 1.0)),
                Zahlungsspalte(KapitalwertRechner.ErloesReihe.PV_VERGUETUNG, "PV-Vergütung (EEG)",
                               Fortgeschrieben(T, 21000.0, 1.0))
            };
        }
    }
}
