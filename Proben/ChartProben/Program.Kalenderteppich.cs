using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE BILDER DER KALENDERKARTE</b> (Entwurf KP2, Welle K4, Festlegung 8; Befund B12): das
    /// Teppichbild eines Kalenders (<c>ChartRenderer.KalenderteppichModell</c>) und die Woche einer
    /// Größe mit Lücke für „aus" (<c>ChartRenderer.KalenderwocheModell</c>).
    ///
    /// <para><b>Synthetische Kalender</b> (keine Datenbank, kein Zufall, Bezugsjahr 2025): Heizen
    /// „Büro" mit Standardwoche 20/16 °C, Neujahr wie Sonntag, Herbstferien 16 °C und der Heizperiode
    /// 01.10.–30.04. (außerhalb „aus"); Lüftung werktags 6–22 Uhr 1,0/2,0 1/h, nachts und am
    /// Wochenende „aus". Die Texte sind die deutsche Vorgabe (<c>KalenderteppichTexte</c>).</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht
    /// sehen: dass „aus" als eigene Fläche im Bild steht (dieselbe Woche mit 0 statt „aus" zeichnet
    /// anders), dass die Ferien wirken, dass die Woche die Linie bricht — und ohne PNG, dass jedes
    /// Teppichbild höchstens 2 000 Elemente trägt, „aus" die Rolle <c>RASTER_LOCH</c> mit Schraffur
    /// bekommt und die Fläche der Woche in je einen Teilpfad je Tag zerfällt, ohne „NaN".</para>
    /// </summary>
    internal static partial class Program
    {
        private const int TEPPICH_JAHR = 2025;

        /// <summary>Tag 1 … 365 des Gemeinjahres zu einem Datum.</summary>
        private static int Jahrestag(int monat, int tag) => Feiertage.Gemeinjahrestag(monat, tag);

        /// <summary>Heizen „Büro" als Kalender — mit oder ohne Herbstferien.</summary>
        private static Konditionierungskalender BueroHeizkalender(bool mitFerien)
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt < 5 && s >= 7 && s < 18 ? 20.0 : 16.0;
            var perioden = new List<Kalenderregel>
            {
                Kalenderregel.Feiertag(100, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AlsWochentag(7)),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_SAISON, DbWerte.KOND_ART_BETRIEBSPAUSE,
                                       Standardfahrplan.BEZEICHNER_SAISON, Jahrestag(5, 1), Jahrestag(9, 30),
                                       Kalenderangabe.Abgeschaltet),
            };
            if (mitFerien)
                perioden.Add(Kalenderregel.Zeitraum(200, DbWerte.KOND_ART_FERIEN, "Herbstferien",
                                                    Jahrestag(10, 20), Jahrestag(10, 31), Kalenderangabe.AusWert(16.0)));
            return new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(w), null, perioden);
        }

        /// <summary>Die Lüftungswoche: werktags 6–8 und 18–22 Uhr 1,0 1/h, 8–18 Uhr 2,0 1/h; sonst „aus" (NaN) bzw. <paramref name="stattAus"/>.</summary>
        private static double[] Lueftungswoche(double stattAus)
        {
            var w = new double[168];
            for (int wt = 0; wt < 7; wt++)
                for (int s = 0; s < 24; s++)
                    w[wt * 24 + s] = wt >= 5 || s < 6 || s >= 22 ? stattAus : s >= 8 && s < 18 ? 2.0 : 1.0;
            return w;
        }

        private static Kalenderteppich Heizteppich(bool mitFerien)
            => Kalenderteppich.Bilden(BueroHeizkalender(mitFerien), TEPPICH_JAHR);

        private static Kalenderteppich Lueftungsteppich(double stattAus)
            => Kalenderteppich.Bilden(new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                          Kalenderangabe.AusWoche(Lueftungswoche(stattAus)), null, null), TEPPICH_JAHR);

        /// <summary>Eine Woche, die stündlich zwischen „aus" und 84 Werten wechselt — ungebündelt rund 13 000 Elemente.</summary>
        private static Kalenderteppich Grenzteppich()
        {
            var w = new double[168];
            for (int i = 0; i < 168; i++) w[i] = i % 2 == 0 ? double.NaN : i * 0.05;
            return Kalenderteppich.Bilden(new Konditionierungskalender(Konditionierungsgroesse.Lueftung,
                                              Kalenderangabe.AusWoche(w), null, null), TEPPICH_JAHR);
        }

        private static void KalenderteppichProben(string ziel)
        {
            // Maßproben: die drei Bilder, je 1244 x 464, mit den Farben ihrer Rollen.
            Pruefe(ziel, "kalenderteppich_heizen_buero", 1244, 464,
                   new[] { Rollenfarbe(Farbrolle.HEIZWAERME), Rollenfarbe(Farbrolle.RASTER_LOCH) },
                   () => ChartRenderer.KalenderteppichBild(Heizteppich(true)));
            Pruefe(ziel, "kalenderteppich_lueftung_aus", 1244, 464,
                   new[] { Rollenfarbe(Farbrolle.SERIE_6), Rollenfarbe(Farbrolle.RASTER_LOCH) },
                   () => ChartRenderer.KalenderteppichBild(Lueftungsteppich(double.NaN)));
            Pruefe(ziel, "kalenderwoche_aus", 1244, 464,
                   new[] { ChartRenderer.C_PROFILLINIE, PROFILFLAECHE_AUF_WEISS },
                   () => SkiaMaler.Png(ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung,
                                                                         Lueftungswoche(double.NaN))));

            // Gegenproben: „aus" ist eine eigene Fläche, die Ferien wirken, die Woche bricht die Linie.
            Unterschiedlich("kalenderteppich_aus_wirkt",
                () => ChartRenderer.KalenderteppichBild(Lueftungsteppich(double.NaN)),
                () => ChartRenderer.KalenderteppichBild(Lueftungsteppich(0.0)));
            Unterschiedlich("kalenderteppich_ferien_wirkt",
                () => ChartRenderer.KalenderteppichBild(Heizteppich(true)),
                () => ChartRenderer.KalenderteppichBild(Heizteppich(false)));
            Unterschiedlich("kalenderwoche_luecke_wirkt",
                () => SkiaMaler.Png(ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung, Lueftungswoche(double.NaN))),
                () => SkiaMaler.Png(ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.0))));

            // SVG-Gegenproben der Pixelbilder (Gruppe (c)): data-wert an jedem Feld, Titel, Legende.
            SvgPixelbildprobe("kalenderteppich_heizen_buero", () => ChartRenderer.KalenderteppichModell(Heizteppich(true)));
            SvgPixelbildprobe("kalenderteppich_lueftung_aus", () => ChartRenderer.KalenderteppichModell(Lueftungsteppich(double.NaN)));

            // Die Grenze und die Rolle „aus": jedes Teppichbild höchstens 2 000 Elemente - auch das
            // Grenzbild, das dafür benannt gröber zeichnet -, jedes „aus"-Feld RASTER_LOCH mit genau
            // einer Schraffur in RAHMEN, der Wert am Feld wörtlich.
            SvgProbe("svg_kalenderteppich_grenze_und_aus", e =>
            {
                var modelle = new (string Name, Zeichenmodell Modell)[]
                {
                    ("heizen", ChartRenderer.KalenderteppichModell(Heizteppich(true))),
                    ("lueftung", ChartRenderer.KalenderteppichModell(Lueftungsteppich(double.NaN))),
                    ("grenze", ChartRenderer.KalenderteppichModell(Grenzteppich())),
                };
                e.Masse = modelle[0].Modell.Breite + "x" + modelle[0].Modell.Hoehe;
                var zahlen = new List<string>();
                foreach ((string name, Zeichenmodell m) in modelle)
                {
                    int n = ChartRenderer.Elementzahl(m);
                    int knoten = SvgSchreiber.Baum(m).Alle().Count();
                    zahlen.Add(name + " " + n.ToString(CultureInfo.InvariantCulture));
                    if (n > ChartRenderer.KALENDERTEPPICH_ELEMENTE_MAX)
                        e.Maengel.Add(name + ": " + n + " Elemente über der Grenze");
                    if (knoten > ChartRenderer.KALENDERTEPPICH_ELEMENTE_MAX + 10)
                        e.Maengel.Add(name + ": " + knoten + " SVG-Knoten");

                    List<Zeichenbefehl> aus = m.Befehle.Where(b => b.Marke == "reihe:aus").ToList();
                    int flaechen = aus.OfType<Rechteck>().Count(r => r.Fuellung?.Ton.Rolle == Farbrolle.RASTER_LOCH);
                    int schraffuren = aus.OfType<Pfad>().Count(p => p.Rand?.Ton.Rolle == Farbrolle.RAHMEN);
                    if (flaechen == 0) e.Maengel.Add(name + ": keine „aus“-Fläche mit RASTER_LOCH");
                    if (flaechen != schraffuren || flaechen != aus.Count / 2)
                        e.Maengel.Add(name + ": " + flaechen + " „aus“-Flächen, " + schraffuren + " Schraffuren");
                    if (!m.Befehle.Any(b => b.Marke == "legende:aus")) e.Maengel.Add(name + ": „aus“ fehlt in der Legende");
                }
                e.Knoten = string.Join(" / ", zahlen);

                bool vereinfacht(Zeichenmodell m, string anfang)
                    => m.Befehle.OfType<Text>().Any(t => t.Inhalt.StartsWith(anfang, StringComparison.Ordinal));
                if (!vereinfacht(modelle[2].Modell, "Vereinfacht auf 3 Farbstufen und Blöcke zu 14 Tagen"))
                    e.Maengel.Add("das Grenzbild nennt seine Vergröberung nicht");
                if (vereinfacht(modelle[0].Modell, "Vereinfacht"))
                    e.Maengel.Add("das Büro-Bild ist vergröbert");

                var werte = modelle[0].Modell.Befehle.Select(b => b.Wert).Where(w => w != null).ToList();
                foreach (string soll in new[]
                         {
                             "Mo–Fr 06.–10.01., 7–18 Uhr: 20 °C · Standardwoche",
                             "01.05.–30.09., 0–24 Uhr: aus · außerhalb der Saison",
                             "20.–31.10., 0–24 Uhr: 16 °C · Herbstferien (Ferien)",
                         })
                    if (!werte.Contains(soll)) e.Maengel.Add("Wert fehlt: " + soll);
                if (!modelle[0].Modell.Befehle.OfType<Text>().Any(t => t.Marke == "titel" && t.Inhalt.Contains("Bezugsjahr 2025")))
                    e.Maengel.Add("das Bezugsjahr steht nicht im Titel");
            });

            // Die Woche mit Lücke: die Fläche zerfällt in einen Teilpfad je Werktag (roh wie
            // gebündelt), kein Pfad trägt „NaN", im PNG-Modell je Stück eine Fläche und eine Linie.
            SvgProbe("svg_kalenderwoche_luecke", e =>
            {
                Zeichenmodell m = ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung, Lueftungswoche(double.NaN));
                e.Masse = m.Breite + "x" + m.Hoehe;
                Datenreihe r = m.Reihen.Single();
                if (r.Einheit != "1/h") e.Maengel.Add("Einheit der Reihe: " + (r.Einheit ?? "keine"));
                foreach (bool roh in new[] { true, false })
                {
                    string d = SvgSchreiber.Reihenpfad(r, m.Flaeche, roh);
                    if (d.Contains("NaN", StringComparison.Ordinal)) e.Maengel.Add("NaN im Pfad (roh " + roh + ")");
                    int teile = d.Split('M').Length - 1, zu = d.Split('Z').Length - 1;
                    if (teile != 5 || zu != 5) e.Maengel.Add("Teilpfade (roh " + roh + "): " + teile + " M, " + zu + " Z statt 5");
                    if (roh) e.Groesse = d.Length.ToString(CultureInfo.InvariantCulture);
                }
                List<Pfad> pixel = m.Befehle.OfType<Pfad>().Where(p => p.Marke == "reihe:" + r.Name).ToList();
                e.Knoten = pixel.Count.ToString(CultureInfo.InvariantCulture);
                if (pixel.Count != 10) e.Maengel.Add("Pixelpfade der Reihe: " + pixel.Count + " statt 10");
                if (pixel.SelectMany(p => p.Punkte).Any(p => float.IsNaN(p.X) || float.IsNaN(p.Y)))
                    e.Maengel.Add("NaN-Bildpunkt");

                // Ohne Lücke bleibt es EIN Zug - der Bestand.
                Zeichenmodell voll = ChartRenderer.KalenderwocheModell(Konditionierungsgroesse.Lueftung, Lueftungswoche(0.0));
                string dv = SvgSchreiber.Reihenpfad(voll.Reihen.Single(), voll.Flaeche, true);
                if (dv.Split('M').Length - 1 != 1) e.Maengel.Add("die Woche ohne Lücke zerfällt");
            });
        }
    }
}
