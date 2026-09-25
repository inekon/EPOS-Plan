using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>ANLAGENKOPPLUNG AK1 WELLE 3 — DAS BILD „VORLAUF UND RÜCKLAUF"</b> (Konzept Anlagenkopplung
    /// 9.4, 12.1): der Jahresverlauf des gefahrenen Vorlaufs und des Rücklaufs eines gekoppelt
    /// gerechneten Gebäudes, dazu gestrichelt der Auslegungspunkt.
    ///
    /// <para><b>Stunden ohne Heizbetrieb sind LÜCKEN</b> (<c>ChartRenderer.Reihe.Luecken</c>): Die
    /// Probenreihen heizen bis Stunde 2 999 und ab Stunde 6 500; dazwischen tragen sie NaN, und die
    /// Linie bricht dort ab — im PNG wie im SVG. Ohne den Schalter machte ein einziger NaN die Reihe
    /// unbrauchbar; jedes Bild des Bestands bleibt davon unberührt.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht
    /// sehen: dass der Auslegungspunkt im Bild steht, dass die Lücke das Bild verändert (dieselben
    /// Reihen mit gefülltem Sommer zeichnen anders) — und im SVG, dass jede Reihe mit Lücke in zwei
    /// Teilpfade zerfällt, kein Pfad ein „NaN" trägt und die Linie des Auslegungspunkts ein Zug
    /// bleibt.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Heizstunden der Probe: bis Stunde 2 999 und ab Stunde 6 500.</summary>
        private static bool Heizstunde(int h) => h < 3000 || h >= 6500;

        /// <summary>Ein Vorlauf, der der Außentemperatur folgt (Jahresgang), mit Lücken im Sommer.</summary>
        private static double[] Vorlaufprobe(bool mitLuecke)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
            {
                double gang = 38.0 + 9.0 * Math.Cos(2.0 * Math.PI * i / STUNDEN) + 1.5 * Math.Sin(2.0 * Math.PI * i / 24.0);
                w[i] = Heizstunde(i) || !mitLuecke ? gang : double.NaN;
            }
            return w;
        }

        /// <summary>Der Rücklauf dazu: 7 K unter dem Vorlauf, dieselben Lücken.</summary>
        private static double[] Ruecklaufprobe(bool mitLuecke)
        {
            double[] v = Vorlaufprobe(mitLuecke);
            var r = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++) r[i] = double.IsNaN(v[i]) ? double.NaN : v[i] - 7.0;
            return r;
        }

        /// <summary>Die Kühlstunden der Probe (E37): Stunde 3 500 bis 6 499, der Sommer.</summary>
        private static bool Kuehlstunde(int h) => h >= 3500 && h < 6500;

        /// <summary>Der feste Kaltwasser-Vorlauf der Kälteseite, 16 °C, mit Lücken außerhalb der Kühlstunden.</summary>
        private static double[] Kuehlvorlaufprobe(bool mitLuecke)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++) w[i] = Kuehlstunde(i) || !mitLuecke ? 16.0 : double.NaN;
            return w;
        }

        /// <summary>Der Rücklauf zur gelieferten Kühlleistung: über dem Vorlauf, im Tagesgang, dieselben Lücken.</summary>
        private static double[] Kuehlruecklaufprobe(bool mitLuecke)
        {
            double[] v = Kuehlvorlaufprobe(mitLuecke);
            var r = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
                r[i] = double.IsNaN(v[i]) ? double.NaN : v[i] + 1.5 + 1.2 * Math.Sin(2.0 * Math.PI * i / 24.0);
            return r;
        }

        private static byte[] Kuehlvorlaufbild(bool mitLuecke, double? auslegungVorlauf, double? auslegungRuecklauf)
            => ChartRenderer.VorlaufRuecklauf("Kühlvorlauf und Kühlrücklauf", Kuehlvorlaufprobe(mitLuecke), Kuehlruecklaufprobe(mitLuecke),
                                              auslegungVorlauf, auslegungRuecklauf,
                                              new ChartRenderer.VorlaufRuecklaufnamen
                                              {
                                                  Vorlauf = "Kühlvorlauf", Ruecklauf = "Kühlrücklauf",
                                                  AuslegungVorlauf = "Auslegung Kühlvorlauf", AuslegungRuecklauf = "Auslegung Kühlrücklauf"
                                              });

        private static byte[] Vorlaufbild(bool mitLuecke, double? auslegungVorlauf, double? auslegungRuecklauf)
            => ChartRenderer.VorlaufRuecklauf("Vorlauf und Rücklauf", Vorlaufprobe(mitLuecke), Ruecklaufprobe(mitLuecke),
                                              auslegungVorlauf, auslegungRuecklauf,
                                              new ChartRenderer.VorlaufRuecklaufnamen());

        private static void AnlagenkopplungProben(string ziel)
        {
            // Maßprobe: 1240 x 560, die Farben von Vorlauf und Rücklauf, Determinismus.
            Pruefe(ziel, "vorlauf_ruecklauf_gebaeude", 1240, 560,
                   new[] { Rollenfarbe(Farbrolle.SERIE_1), Rollenfarbe(Farbrolle.SERIE_2) },
                   () => Vorlaufbild(true, 55.0, 45.0));

            // Gegenprobe 1: Der Auslegungspunkt muss im Bild stehen.
            Unterschiedlich("vorlauf_auslegung_wirkt",
                () => Vorlaufbild(true, 55.0, 45.0),
                () => Vorlaufbild(true, null, null));

            // Gegenprobe 2: Die Lücke muss im Bild stehen - derselbe Gang mit gefülltem Sommer
            // zeichnet anders.
            Unterschiedlich("vorlauf_luecke_wirkt",
                () => Vorlaufbild(true, 55.0, 45.0),
                () => Vorlaufbild(false, 55.0, 45.0));

            // SVG: Jede Reihe mit Lücke zerfällt in zwei Teilpfade, kein Pfad trägt "NaN", die
            // Linien des Auslegungspunkts bleiben EIN Zug - roh wie gebündelt.
            SvgProbe("svg_vorlauf_luecken", e =>
            {
                Zeichenmodell m = ChartRenderer.VorlaufRuecklaufModell("Vorlauf und Rücklauf",
                    Vorlaufprobe(true), Ruecklaufprobe(true), 55.0, 45.0,
                    new ChartRenderer.VorlaufRuecklaufnamen());
                e.Masse = m.Breite + "x" + m.Hoehe;

                List<SvgKnoten> pfade = SvgSchreiber.Baum(m).Alle()
                    .Where(k => k.Name == "path" && Attributwert(k, "class") == SvgSchreiber.KLASSE_REIHE)
                    .ToList();
                e.Knoten = pfade.Count.ToString(CultureInfo.InvariantCulture);
                if (pfade.Count != 4) { e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt 4"); return; }

                int[] erwartet = { 2, 2, 1, 1 };
                for (int i = 0; i < pfade.Count; i++)
                {
                    string d = Attributwert(pfade[i], "d") ?? "";
                    if (d.Contains("NaN", StringComparison.Ordinal)) e.Maengel.Add("NaN im Pfad: " + m.Reihen[i].Name);
                    int teile = d.Split('M').Length - 1;
                    if (teile != erwartet[i])
                        e.Maengel.Add("Teilpfade " + m.Reihen[i].Name + ": " + teile + " statt " + erwartet[i]);
                }

                // Gebündelt (der Zoom rechnet roh nach, das Vollbild bündelt ab vier Reihen): dieselbe
                // Zerlegung, und der erste Teilpfad endet vor der Lücke.
                Datenreihe vorlauf = m.Reihen[0];
                string gebuendelt = SvgSchreiber.Reihenpfad(vorlauf, m.Flaeche, false);
                if (gebuendelt.Split('M').Length - 1 != 2) e.Maengel.Add("gebündelt nicht in zwei Teilpfaden");
                if (gebuendelt.Contains("NaN", StringComparison.Ordinal)) e.Maengel.Add("NaN im gebündelten Pfad");
                string ausschnitt = SvgSchreiber.Reihenpfad(vorlauf, m.Flaeche, 4000, 6000, true);
                if (ausschnitt.Length != 0) e.Maengel.Add("der Ausschnitt ganz in der Lücke zeichnet: " + ausschnitt);
                e.Groesse = gebuendelt.Length.ToString(CultureInfo.InvariantCulture);
            });

            // E37 - DIE KÄLTESEITE: dasselbe Bild mit den Reihen der Kühlübergabe (fester
            // Kaltwasser-Vorlauf, Rücklauf darüber, Lücken außerhalb der Kühlstunden). Ohne neuen
            // Parameter - jedes Bild des Bestands bleibt byte-gleich.
            Pruefe(ziel, "kuehlvorlauf_ruecklauf_gebaeude", 1240, 560,
                   new[] { Rollenfarbe(Farbrolle.SERIE_1), Rollenfarbe(Farbrolle.SERIE_2) },
                   () => Kuehlvorlaufbild(true, 16.0, 19.0));

            // Gegenproben der Kälteseite: Auslegungspunkt und Lücke stehen im Bild.
            Unterschiedlich("kuehlvorlauf_auslegung_wirkt",
                () => Kuehlvorlaufbild(true, 16.0, 19.0),
                () => Kuehlvorlaufbild(true, null, null));
            Unterschiedlich("kuehlvorlauf_luecke_wirkt",
                () => Kuehlvorlaufbild(true, 16.0, 19.0),
                () => Kuehlvorlaufbild(false, 16.0, 19.0));

            // SVG der Kälteseite: Vorlauf und Rücklauf mit EINER Lücke (Winter davor und danach)
            // zerfallen in einen Teilpfad je Kühlperiode - hier genau einen -, kein Pfad trägt "NaN".
            SvgProbe("svg_kuehlvorlauf_luecken", e =>
            {
                Zeichenmodell m = ChartRenderer.VorlaufRuecklaufModell("Kühlvorlauf und Kühlrücklauf",
                    Kuehlvorlaufprobe(true), Kuehlruecklaufprobe(true), 16.0, 19.0,
                    new ChartRenderer.VorlaufRuecklaufnamen());
                e.Masse = m.Breite + "x" + m.Hoehe;
                List<SvgKnoten> pfade = SvgSchreiber.Baum(m).Alle()
                    .Where(k => k.Name == "path" && Attributwert(k, "class") == SvgSchreiber.KLASSE_REIHE)
                    .ToList();
                e.Knoten = pfade.Count.ToString(CultureInfo.InvariantCulture);
                if (pfade.Count != 4) { e.Maengel.Add("Reihenpfade: " + pfade.Count + " statt 4"); return; }
                for (int i = 0; i < pfade.Count; i++)
                {
                    string d = Attributwert(pfade[i], "d") ?? "";
                    if (d.Contains("NaN", StringComparison.Ordinal)) e.Maengel.Add("NaN im Pfad: " + m.Reihen[i].Name);
                    int teile = d.Split('M').Length - 1;
                    if (teile != 1) e.Maengel.Add("Teilpfade " + m.Reihen[i].Name + ": " + teile + " statt 1");
                }
                e.Groesse = (Attributwert(pfade[0], "d") ?? "").Length.ToString(CultureInfo.InvariantCulture);
            });

            // SVG: Eine Reihe ohne Lücke schreibt sich wörtlich wie vorher - hier gegen den Pfad der
            // Raumtemperatur gehalten, der denselben Verlaufsweg nimmt und keine Lücke kennt.
            SvgProbe("svg_ohne_luecke_unveraendert", e =>
            {
                double[] gang = Vorlaufprobe(false);
                Zeichenmodell m = ChartRenderer.RaumtemperaturModell("Raumtemperatur", gang, null, null, null,
                                                                     new ChartRenderer.Raumtemperaturnamen());
                e.Masse = m.Breite + "x" + m.Hoehe;
                string d = SvgSchreiber.Reihenpfad(m.Reihen[0], m.Flaeche, true);
                int teile = d.Split('M').Length - 1;
                e.Knoten = teile.ToString(CultureInfo.InvariantCulture);
                if (teile != 1) e.Maengel.Add("ein Zug ohne Lücke zerfällt in " + teile + " Teilpfade");
                int punkte = d.Split(' ').Count(s => s.Contains(',', StringComparison.Ordinal));
                if (punkte != STUNDEN) e.Maengel.Add("roh nicht jede Stunde: " + punkte + " Punkte");
            });
        }
    }
}
