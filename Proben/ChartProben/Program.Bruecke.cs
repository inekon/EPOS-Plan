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
    /// <b>ETAPPE E8a — DAS BRÜCKENBILD</b> „Von der Investition zur Kapitalwertdifferenz"
    /// (Mockup-Anhang U41, Anwenderentscheid E5b‑4 vom 22.09.2026: Bau mit E8, dann
    /// Bildprobe wie bei den anderen Diagrammen).
    ///
    /// <para><b>Was hier gezeichnet wird.</b> Die Kapitalwertdifferenz als Wasserfall
    /// (<c>ChartRenderer.KapitalwertBruecke</c>): je Bestandteil eine Säule vom Stand vor bis
    /// zum Stand nach dem Schritt — rot, wenn er die Differenz mindert, grün, wenn er sie
    /// mehrt —, zuletzt die Ergebnissäule von null bis zur Summe in der Hausfarbe. Die erste
    /// Probe trägt die Zahlen des Mockups (Beide Anlagen gegen das Stammprojekt, ΔKW
    /// 1.842.695 €), die zweite eine Brücke, die unter der Referenz endet.</para>
    ///
    /// <para><b>Die Texte sind die deutsche Vorgabe</b> (<c>BrueckenTexte</c>) — kein
    /// Probebild hängt an der Oberflächensprache des Rechners.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung
    /// nicht sehen: dass ein Schritt sein Bild ändert, dass die Reihenfolge der Schritte zählt
    /// — und im SVG, dass die Säulen eine TREPPE bilden: Jede beginnt auf der Höhe, auf der
    /// die vorige endet, und die Ergebnissäule reicht von der Nulllinie bis zum letzten
    /// Stand.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Die Proben des Brückenbilds — eine Zeile in <c>Program.cs</c> ruft sie.
        /// </summary>
        private static void BrueckenProben(string ziel)
        {
            ChartRenderer.BrueckenTexte texte = Brueckentexte();

            // ---- Maßproben -------------------------------------------------------

            // Die Zahlen des Mockups: vier Schritte rot und grün, die Ergebnissäule in der
            // Hausfarbe über der Nulllinie.
            Pruefe(ziel, "kapitalwert_bruecke", ChartRenderer.BRUECKE_BREITE, ChartRenderer.BRUECKE_HOEHE,
                   new[] { ChartRenderer.C_RASTER_GUT, ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_STAMM },
                   () => ChartRenderer.KapitalwertBruecke(Mockupschritte(), texte));

            // Eine Brücke, die unter der Referenz endet: Die Ergebnissäule hängt unter der
            // Nulllinie, das Bildmaß bleibt.
            Pruefe(ziel, "kapitalwert_bruecke_unter_referenz", ChartRenderer.BRUECKE_BREITE, ChartRenderer.BRUECKE_HOEHE,
                   new[] { ChartRenderer.C_RASTER_GUT, ChartRenderer.C_RASTER_SCHLECHT, ChartRenderer.C_STAMM },
                   () => ChartRenderer.KapitalwertBruecke(Unterschritte(), texte));

            // Kein zeichenbarer Schritt (nicht endlich): der Leerhinweis an der Stelle des Bildes.
            Pruefe(ziel, "kapitalwert_bruecke_leer", ChartRenderer.BRUECKE_BREITE, 200,
                   new SKColor[0],
                   () => ChartRenderer.KapitalwertBruecke(new List<ChartRenderer.Brueckenschritt>
                   {
                       new ChartRenderer.Brueckenschritt { Name = "ohne Wert", Wert = double.NaN }
                   }, texte));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens der SCHRITT: dieselbe Brücke, nur die Energiekosten sparen weniger.
            Unterschiedlich("kapitalwert_bruecke_schritt_wirkt",
                () => ChartRenderer.KapitalwertBruecke(Mockupschritte(), texte),
                () =>
                {
                    List<ChartRenderer.Brueckenschritt> s = Mockupschritte();
                    s[2].Wert -= 500000.0;
                    return ChartRenderer.KapitalwertBruecke(s, texte);
                });

            // Zweitens die REIHENFOLGE: dieselben Schritte, dieselbe Summe, umgekehrt gestapelt.
            // Ein Renderer, der die Säulen nach Betrag sortierte oder alle von null aus
            // zeichnete, bestünde jede Maß- und Farbprüfung.
            Unterschiedlich("kapitalwert_bruecke_reihenfolge_wirkt",
                () => ChartRenderer.KapitalwertBruecke(Mockupschritte(), texte),
                () =>
                {
                    List<ChartRenderer.Brueckenschritt> s = Mockupschritte();
                    s.Reverse();
                    return ChartRenderer.KapitalwertBruecke(s, texte);
                });

            // ---- SVG: dasselbe Modell auf dem Bildschirmweg ----------------------

            SvgPixelbildprobe("kapitalwert_bruecke",
                () => ChartRenderer.KapitalwertBrueckeModell(Mockupschritte(), texte));
            BrueckenTreppenprobe("svg_kapitalwert_bruecke_treppe", Mockupschritte(), texte);
            BrueckenTreppenprobe("svg_kapitalwert_bruecke_treppe_unter_referenz", Unterschritte(), texte);

            // Sichtprüfung (--svg-alle): beide Modelle als Dateien neben den Skia-PNG.
            if (_svgordner != null)
                SvgOrdnerSchreiben(new List<KeyValuePair<string, Func<Zeichenmodell>>>
                {
                    new KeyValuePair<string, Func<Zeichenmodell>>("kapitalwert_bruecke",
                        () => ChartRenderer.KapitalwertBrueckeModell(Mockupschritte(), texte)),
                    new KeyValuePair<string, Func<Zeichenmodell>>("kapitalwert_bruecke_unter_referenz",
                        () => ChartRenderer.KapitalwertBrueckeModell(Unterschritte(), texte))
                });
        }

        /// <summary>
        /// <b>Die Säulen bilden eine Treppe</b> — im SVG nachgemessen: Jede Säule beginnt auf der
        /// Höhe, auf der die vorige endet (eine mindernde Säule hängt von ihrer Oberkante herab,
        /// eine mehrende steht auf ihrer Unterkante), die erste auf der Nulllinie, und die
        /// Ergebnissäule reicht von der Nulllinie bis zum letzten Stand. Jede Säule nennt ihren
        /// Betrag mit Vorzeichen am Element.
        /// </summary>
        private static void BrueckenTreppenprobe(string name, List<ChartRenderer.Brueckenschritt> schritte,
                                                 ChartRenderer.BrueckenTexte texte)
        {
            SvgProbe(name, e =>
            {
                Zeichenmodell m = ChartRenderer.KapitalwertBrueckeModell(schritte, texte);
                List<SvgKnoten> alle = SvgSchreiber.Baum(m).Alle().ToList();
                e.Masse = m.Breite + "x" + m.Hoehe;
                e.Knoten = alle.Count.ToString(CultureInfo.InvariantCulture);

                SvgKnoten null0 = alle.FirstOrDefault(
                    k => k.Name == "line" && Attributwert(k, "data-marke") == "nulllinie");
                if (null0 == null) { e.Maengel.Add("keine Nulllinie im Baum"); return; }
                double y0 = Zahl(Attributwert(null0, "y1"));

                // Die Höhe VOR und NACH einer Säule: Eine mindernde beginnt oben, eine mehrende unten.
                double stand = y0;
                double summe = 0.0;
                foreach (ChartRenderer.Brueckenschritt s in schritte)
                {
                    SvgKnoten r = alle.FirstOrDefault(
                        k => k.Name == "rect" && Attributwert(k, "data-marke") == "reihe:" + s.Name);
                    if (r == null) { e.Maengel.Add("keine Säule für " + s.Name); return; }
                    double oben = Zahl(Attributwert(r, "y")), unten = oben + Zahl(Attributwert(r, "height"));
                    double vorher = s.Wert < 0.0 ? oben : unten, nachher = s.Wert < 0.0 ? unten : oben;
                    if (Math.Abs(vorher - stand) > 0.5)
                        e.Maengel.Add("die Säule " + s.Name + " beginnt nicht auf der Höhe der vorigen");
                    stand = nachher;
                    summe += s.Wert;

                    string wert = Attributwert(r, "data-wert") ?? "";
                    string betrag = s.Wert.ToString("+#,##0;−#,##0;0", CultureInfo.GetCultureInfo("de-DE"));
                    if (!wert.Contains(betrag, StringComparison.Ordinal))
                        e.Maengel.Add("die Säule " + s.Name + " nennt ihren Betrag nicht: " + wert);
                }

                SvgKnoten ergebnis = alle.FirstOrDefault(
                    k => k.Name == "rect" && Attributwert(k, "data-marke") == "reihe:" + texte.Ergebnis);
                if (ergebnis == null) { e.Maengel.Add("keine Ergebnissäule"); return; }
                double eo = Zahl(Attributwert(ergebnis, "y")), eu = eo + Zahl(Attributwert(ergebnis, "height"));
                double nullseite = summe < 0.0 ? eo : eu, standseite = summe < 0.0 ? eu : eo;
                if (Math.Abs(nullseite - y0) > 0.5)
                    e.Maengel.Add("die Ergebnissäule steht nicht auf der Nulllinie");
                if (Math.Abs(standseite - stand) > 0.5)
                    e.Maengel.Add("die Ergebnissäule endet nicht auf dem letzten Stand");
            });
        }

        /// <summary>Die Texte der Proben: die deutsche Vorgabe samt Unterzeile und Fuß des Mockups.</summary>
        private static ChartRenderer.BrueckenTexte Brueckentexte() => new ChartRenderer.BrueckenTexte
        {
            Unterzeile = "Beide Anlagen gegenüber dem Stammprojekt · Barwerte · Szenario Erwartet",
            Fuss = "Barwerte gegenüber dem Stammprojekt, Szenario Erwartet — i = 3,0 %, T = 20 a"
        };

        /// <summary>Die sechs Schritte des Mockups (Beide Anlagen gegen das Stammprojekt, €).</summary>
        private static List<ChartRenderer.Brueckenschritt> Mockupschritte() => new List<ChartRenderer.Brueckenschritt>
        {
            new ChartRenderer.Brueckenschritt { Name = "Investition I₀", Wert = -426922.0 },
            new ChartRenderer.Brueckenschritt { Name = "Betriebskosten", Wert = -935724.0 },
            new ChartRenderer.Brueckenschritt { Name = "Energiekosten", Wert = 2606605.0 },
            new ChartRenderer.Brueckenschritt { Name = "Erlöse", Wert = 649686.0 },
            new ChartRenderer.Brueckenschritt { Name = "Ersatzbeschaffungen", Wert = -148982.0 },
            new ChartRenderer.Brueckenschritt { Name = "Restwert am Ende", Wert = 98032.0 }
        };

        /// <summary>Sechs Schritte, die unter der Referenz enden (Summe −410.000 €).</summary>
        private static List<ChartRenderer.Brueckenschritt> Unterschritte() => new List<ChartRenderer.Brueckenschritt>
        {
            new ChartRenderer.Brueckenschritt { Name = "Investition I₀", Wert = -900000.0 },
            new ChartRenderer.Brueckenschritt { Name = "Betriebskosten", Wert = -120000.0 },
            new ChartRenderer.Brueckenschritt { Name = "Energiekosten", Wert = 600000.0 },
            new ChartRenderer.Brueckenschritt { Name = "Erlöse", Wert = 50000.0 },
            new ChartRenderer.Brueckenschritt { Name = "Ersatzbeschaffungen", Wert = -80000.0 },
            new ChartRenderer.Brueckenschritt { Name = "Restwert am Ende", Wert = 40000.0 }
        };
    }
}
