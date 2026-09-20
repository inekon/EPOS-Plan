using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kern-Nachzüge des Oberflächen-Abschlusses</b> (Konzept Diagramme,
    /// Etappe E3, Gruppen (b) und (c) — Oberfläche).
    ///
    /// <para>Die Gruppen (b) und (c) haben jede Bildart des <c>ChartRenderer</c> auf
    /// ein Zeichenmodell gestellt. Was fehlte, waren fünf Dinge, die NICHT im Renderer
    /// stehen oder erst die Oberfläche braucht: die Modelle der zwei Oberflächenbilder
    /// (<c>PeakShavingBild</c>, <c>SpeicherBetriebsbild</c>), der Wert am Element der
    /// Pixelbilder der Gruppe (b), die Achsenseite einer Reihe (DG-E3-12), die
    /// Stützpunkte der Schnittkurve als eigene Punktreihen und — über den Auftrag
    /// hinaus — die Achsenart einer DAUERLINIE.</para>
    ///
    /// <para><b>Die Zusage, die über allem steht:</b> Kein Bild darf sich ändern. Jeder
    /// Fall, der ein Modell prüft, hält es deshalb gegen das PNG des Bestandsweges —
    /// byte für byte, neben der Messlatte der ChartProben.</para>
    ///
    /// <para><b>Die Kultur ist gepinnt</b> (<see cref="Kulturvorrichtung"/>): Die Werte
    /// am Element tragen deutsche Zahlen, und der Windows-Läufer steht auf en-US.</para>
    /// </summary>
    public sealed class ChartRendererNachzuegeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        // =====================================================================
        // Die Gaben
        // =====================================================================

        /// <summary>Drei Vorlaufstufen mit je vier Stützstellen.</summary>
        private static List<ChartRenderer.KennlinienReihe> Kennlinien()
        {
            var reihen = new List<ChartRenderer.KennlinienReihe>();
            foreach (int vorlauf in new[] { 35, 45, 55 })
            {
                var punkte = new List<(double Temperatur, double Wert)>();
                for (int t = -15; t <= 15; t += 10)
                    punkte.Add((t, Math.Round(2.0 + (t + 20) * 0.11 - (vorlauf - 35) * 0.035, 2)));
                reihen.Add(new ChartRenderer.KennlinienReihe(vorlauf, punkte));
            }
            return reihen;
        }

        private static int[] Jahre() => Enumerable.Range(1, 8).ToArray();

        private static ChartRenderer.Reihe Netto()
            => new ChartRenderer.Reihe("Netto-Cashflow",
                   new double[] { -12000, 4200, 4300, -900, 4500, 4600, 4700, 4800 },
                   ChartRenderer.C_PV);

        private static ChartRenderer.Reihe Kumuliert()
        {
            double[] netto = Netto().Werte;
            var summe = new double[netto.Length];
            double s = 0;
            for (int i = 0; i < netto.Length; i++) { s += netto[i]; summe[i] = s; }
            return new ChartRenderer.Reihe("Kumuliert", summe, ChartRenderer.C_STAMM);
        }

        private static int[] Stueckzahlen() => new[] { 1, 2, 3, 4, 5 };

        private static double[] Stueckwerte() => new double[] { -2000, 1500, 3100, 2200, -800 };

        /// <summary>Jeder Wert am Element eines Modells, in Zeichenreihenfolge.</summary>
        private static List<string> Werte(Zeichenmodell modell)
            => Alle(modell.Befehle).Where(b => b.Wert != null).Select(b => b.Wert).ToList();

        /// <summary>Jeder Befehl samt den Befehlen jeder Gruppe.</summary>
        private static IEnumerable<Zeichenbefehl> Alle(IEnumerable<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                yield return b;
                if (b is Gruppe g)
                    foreach (Zeichenbefehl k in Alle(g.Befehle)) yield return k;
            }
        }

        // =====================================================================
        // (1) Die zwei Oberflächenbilder — Modell und PNG sind dasselbe Bild
        // =====================================================================

        /// <summary>
        /// <c>SpeicherBetriebsbild.Modell</c> ist der Zwilling von <c>Zeichnen</c>: Das
        /// PNG des Bestandsweges entsteht aus genau diesem Modell.
        /// </summary>
        [Fact]
        public void Das_Speicherbetriebsbild_malt_sein_eigenes_Modell()
        {
            var eingang = Speichereingang();
            var ergebnis = Speicherergebnis(eingang);

            byte[] bild = SpeicherBetriebsbild.Zeichnen(
                "Betrieb", eingang, ergebnis, 0.25, null, false, null);
            Zeichenmodell modell = SpeicherBetriebsbild.Modell(
                "Betrieb", eingang, ergebnis, 0.25, null, false, null);

            Assert.NotNull(bild);
            Assert.NotNull(modell);
            Assert.Equal(bild, SkiaMaler.Png(modell));
        }

        /// <summary>Ohne Lauf gibt es weder Bild noch Modell — und keine Ausnahme.</summary>
        [Fact]
        public void Ohne_Lauf_gibt_es_kein_Speicherbetriebsmodell()
        {
            Assert.Null(SpeicherBetriebsbild.Modell("Betrieb", null, null, 0.25, null, false, null));
            Assert.Null(SpeicherBetriebsbild.Zeichnen("Betrieb", null, null, 0.25, null, false, null));
        }

        /// <summary>
        /// Der LADEZUSTAND steht auf der rechten Achse — und sagt es selbst (DG-E3-12).
        /// Ohne diese Angabe müsste die Oberfläche es aus der y-Spanne erraten.
        /// </summary>
        [Fact]
        public void Der_Ladezustand_steht_auf_der_rechten_Achse()
        {
            var eingang = Speichereingang();
            var ergebnis = Speicherergebnis(eingang);
            Zeichenmodell modell = SpeicherBetriebsbild.Modell(
                "Betrieb", eingang, ergebnis, 0.25, null, false, null);

            Datenreihe[] rechts = modell.Reihen
                .Where(r => r.Achsenseite == Achsenseite.Rechts).ToArray();

            Assert.Single(rechts);
            Assert.All(modell.Reihen.Where(r => r.Achsenseite == Achsenseite.Links),
                       r => Assert.NotEqual(rechts[0].Name, r.Name));
        }

        /// <summary>
        /// Dieselbe Zusage für den Erzeugerstapel: Die Reihen der zweiten Achse tragen
        /// <see cref="Achsenseite.Rechts"/>, alle übrigen <see cref="Achsenseite.Links"/>.
        /// </summary>
        [Fact]
        public void Die_zweite_Achse_des_Erzeugerstapels_traegt_ihre_Seite()
        {
            var linien = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Bedarf", Reihe(48, 12.0), ChartRenderer.C_BEDARF)
            };
            var zweite = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Speicherinhalt", Reihe(48, 600.0), ChartRenderer.C_WP)
            };

            Zeichenmodell modell = ChartRenderer.ErzeugerStapelModell(
                "Stapel", new List<ChartRenderer.Reihe>(), linien, null, "kW",
                ChartRenderer.Achse.Jahresstunden, false, zweite, "kWh");

            Assert.Equal(Achsenseite.Links,
                         modell.Reihen.Single(r => r.Name == "Bedarf").Achsenseite);
            Assert.Equal(Achsenseite.Rechts,
                         modell.Reihen.Single(r => r.Name == "Speicherinhalt").Achsenseite);
        }

        // =====================================================================
        // (2) Der Wert am Element der Pixelbilder der Gruppe (b)
        // =====================================================================

        /// <summary>
        /// Jede PUNKTMARKE einer Kennlinie nennt ihre Außentemperatur und ihren Wert —
        /// im Format der eigenen Achse und in der Kultur des Renderers. Der Linienzug
        /// darunter zeigt keine einzelne Zahl und nennt deshalb nur seinen Namen.
        /// </summary>
        [Fact]
        public void Jede_Punktmarke_einer_Kennlinie_nennt_ihren_Wert()
        {
            Zeichenmodell modell = ChartRenderer.KennlinienModell(
                "COP", "COP", "Außentemperatur [°C]", Kennlinien(),
                ChartRenderer.Kennlinienmarke.Kreis);

            List<string> werte = Werte(modell);

            Assert.Contains(werte, w => w.StartsWith("Außentemperatur -15 °C", StringComparison.Ordinal)
                                     || w.StartsWith("Außentemperatur −15 °C", StringComparison.Ordinal));
            Assert.Contains("35°C", string.Join("|", werte));
            // Der Zug selbst nennt seinen Namen, nicht eine Zahl.
            Assert.Contains("35°C", werte);
        }

        /// <summary>
        /// Jede SÄULE der Jahresprojektion nennt ihr Jahr und ihren Betrag, jedes
        /// Ersatzjahr-Band seinen Anlass.
        /// </summary>
        [Fact]
        public void Jede_Saeule_der_Jahresprojektion_nennt_ihr_Jahr_und_ihren_Betrag()
        {
            Zeichenmodell modell = ChartRenderer.JahresprojektionModell(
                "Jahresprojektion [€]", Jahre(), Netto(), Kumuliert(), new[] { 4 },
                null, "Zahlung [€]", "Jahr");

            List<string> werte = Werte(modell);

            Assert.Contains(werte, w => w.Contains("Jahr 1") && w.Contains("Netto-Cashflow")
                                     && w.Contains("€"));
            Assert.Contains(werte, w => w.Contains("Jahr 4") && w.Contains("Ersatzinvestition"));
            Assert.Contains("Kumuliert", werte);
        }

        /// <summary>
        /// Jede SÄULE der Stückzahlkurve nennt ihre Stückzahl und ihren Wert; eine
        /// gesperrte hängt ihre Sperre an.
        /// </summary>
        [Fact]
        public void Jede_Saeule_der_Stueckzahlkurve_nennt_ihren_Wert_samt_Sperre()
        {
            var unzulaessig = new[] { false, false, false, false, true };

            Zeichenmodell modell = ChartRenderer.StueckzahlkurveModell(
                "Kapitalwert über Stückzahl", "Stückzahl", "Kapitalwert [€]",
                Stueckzahlen(), Stueckwerte(), 2, unzulaessig);

            List<string> werte = Werte(modell);

            Assert.Contains(werte, w => w.StartsWith("Stückzahl 1:", StringComparison.Ordinal));
            Assert.Contains(werte, w => w.StartsWith("Stückzahl 5:", StringComparison.Ordinal)
                                     && w.EndsWith("(unzulässig)", StringComparison.Ordinal));
            // Die Bestmarke liegt ueber ihrer Saeule und braucht einen eigenen Wert.
            Assert.Contains(werte, w => w.Contains("bester Wert"));
        }

        /// <summary>
        /// <b>Der Wert ändert kein Bild.</b> Für alle drei Pixelbilder gilt: Das PNG des
        /// Bestandsweges ist byte-gleich mit dem aus dem Modell gemalten.
        /// </summary>
        [Fact]
        public void Der_Wert_am_Element_aendert_kein_Bild()
        {
            Assert.Equal(
                ChartRenderer.Kennlinien("COP", "COP", "Außentemperatur [°C]", Kennlinien(),
                                         ChartRenderer.Kennlinienmarke.Kreis),
                SkiaMaler.Png(ChartRenderer.KennlinienModell(
                    "COP", "COP", "Außentemperatur [°C]", Kennlinien(),
                    ChartRenderer.Kennlinienmarke.Kreis)));

            Assert.Equal(
                ChartRenderer.Jahresprojektion("Jahresprojektion [€]", Jahre(), Netto(),
                                               Kumuliert(), new[] { 4 }, null,
                                               "Zahlung [€]", "Jahr"),
                SkiaMaler.Png(ChartRenderer.JahresprojektionModell(
                    "Jahresprojektion [€]", Jahre(), Netto(), Kumuliert(), new[] { 4 },
                    null, "Zahlung [€]", "Jahr")));

            Assert.Equal(
                ChartRenderer.Stueckzahlkurve("Stückzahl", "Stückzahl", "Kapitalwert [€]",
                                              Stueckzahlen(), Stueckwerte(), 2, null),
                SkiaMaler.Png(ChartRenderer.StueckzahlkurveModell(
                    "Stückzahl", "Stückzahl", "Kapitalwert [€]",
                    Stueckzahlen(), Stueckwerte(), 2, null)));
        }

        /// <summary>
        /// Die Kennlinien mit KREUZ-Marken zeichnen zwei Striche je Punkt — beide in
        /// derselben Klammer und mit demselben Wert. Auch das ändert kein Bild.
        /// </summary>
        [Fact]
        public void Auch_die_Kreuzmarke_traegt_ihren_Wert_und_aendert_kein_Bild()
        {
            Zeichenmodell modell = ChartRenderer.KennlinienModell(
                "Leistung", "Leistung [kW]", "Außentemperatur [°C]", Kennlinien(),
                ChartRenderer.Kennlinienmarke.Kreuz);

            Assert.Contains(Werte(modell), w => w.Contains("kW"));
            Assert.Equal(
                ChartRenderer.Kennlinien("Leistung", "Leistung [kW]", "Außentemperatur [°C]",
                                         Kennlinien(), ChartRenderer.Kennlinienmarke.Kreuz),
                SkiaMaler.Png(modell));
        }

        // =====================================================================
        // (3) Die Stützpunkte der Schnittkurve
        // =====================================================================

        /// <summary>
        /// <b>Die Stützpunkte der Schnittkurve stehen als eigene Punktreihen</b> — sonst
        /// fehlten sie im SVG: Sie tragen die Marke ihrer Reihe und werden damit vom
        /// inneren <c>&lt;svg&gt;</c> verschluckt (DG-E2-2).
        ///
        /// <para>Zwei Reihen und nicht eine, weil das PNG zwei Punktarten zeichnet;
        /// beide tragen den Namen der Kurve, damit die Legendenwahl alles zusammen
        /// schaltet.</para>
        /// </summary>
        [Fact]
        public void Die_Stuetzpunkte_der_Schnittkurve_stehen_als_Punktreihen()
        {
            double[] kapazitaeten = { 100, 150, 160, 170, 200, 250 };
            double[] werte = { 1000, 2200, 2400, 2350, 2100, 1400 };
            bool[] fein = { false, false, true, true, false, false };

            Zeichenmodell modell = ChartRenderer.SchnittkurveModell(
                "Kapitalwert über Kapazität", "Kapazität [kWh]", "ΔJ [€/a]",
                kapazitaeten, werte, 160, 2400, fein);

            Datenreihe[] punkte = modell.Reihen
                .Where(r => r.Art == Reihenart.Punkte).ToArray();

            Assert.Equal(2, punkte.Length);
            Assert.All(punkte, r => Assert.Equal("Kapitalwert über Kapazität", r.Name));
            // Grobpunkte: vier; Feinpunkte: zwei. Jede Reihe traegt ihre x-Stellen.
            Assert.Equal(new[] { 2, 4 }, punkte.Select(r => r.Werte.Length).OrderBy(n => n).ToArray());
            Assert.All(punkte, r => Assert.Equal(r.Werte.Length, r.XWerte.Length));
        }

        /// <summary>
        /// <b>Ohne Feinraster entsteht keine zweite Punktreihe</b> — die Zahl der Reihen
        /// bleibt damit bei zwei, und die <c>Pfadregel</c> bündelt die Kurve nicht
        /// plötzlich anders.
        /// </summary>
        [Fact]
        public void Ohne_Feinpunkte_gibt_es_nur_eine_Punktreihe()
        {
            double[] kapazitaeten = { 100, 200, 300, 400 };
            double[] werte = { 1000, 2200, 2100, 1400 };

            Zeichenmodell modell = ChartRenderer.SchnittkurveModell(
                "Schnitt", "Kapazität [kWh]", "ΔJ [€/a]", kapazitaeten, werte, 200, 2200);

            Assert.Single(modell.Reihen, r => r.Art == Reihenart.Punkte);
            Assert.Equal(2, modell.Reihen.Count);
        }

        /// <summary>Auch die Punktreihen ändern kein Bild.</summary>
        [Fact]
        public void Die_Punktreihen_der_Schnittkurve_aendern_kein_Bild()
        {
            double[] kapazitaeten = { 100, 150, 160, 200 };
            double[] werte = { 1000, 2200, 2400, 2100 };
            bool[] fein = { false, true, true, false };

            Assert.Equal(
                ChartRenderer.Schnittkurve("Schnitt", "Kapazität [kWh]", "ΔJ [€/a]",
                                           kapazitaeten, werte, 160, 2400, fein),
                SkiaMaler.Png(ChartRenderer.SchnittkurveModell(
                    "Schnitt", "Kapazität [kWh]", "ΔJ [€/a]",
                    kapazitaeten, werte, 160, 2400, fein)));
        }

        // =====================================================================
        // (4) Die Achsenart einer Dauerlinie
        // =====================================================================

        /// <summary>
        /// <b>In der DAUERLINIE zählt x den Rang, nicht die Stunde</b> (DG-E3-11). Die
        /// Oberfläche liest die Achsenart aus dem Modell; eine Jahresstundenteilung mit
        /// Monatsnamen wäre über einer Rangachse falsch, und die Zeigerzeile schriebe
        /// „h" hinter eine Zahl, die keine Stunde ist.
        /// </summary>
        [Fact]
        public void Eine_Dauerlinie_zaehlt_einen_Index_statt_Stunden()
        {
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Bedarf", Reihe(96, 10.0), ChartRenderer.C_BEDARF)
            };

            Zeichenmodell gang = ChartRenderer.ErzeugerStapelModell(
                "Stapel", new List<ChartRenderer.Reihe>(), reihen, null, "kW",
                ChartRenderer.Achse.Jahresstunden, false);
            Zeichenmodell dauer = ChartRenderer.ErzeugerStapelModell(
                "Stapel", new List<ChartRenderer.Reihe>(), reihen, null, "kW",
                ChartRenderer.Achse.Jahresstunden, true);

            Assert.Equal(Achsenart.Stunden, gang.Flaeche.X);
            Assert.Equal(Achsenart.Index, dauer.Flaeche.X);
        }

        /// <summary>
        /// Dieselbe Regel im Verlaufsbild (Speicherbetrieb) und in der normierten
        /// Ganglinie — und auch sie ändert kein Bild.
        /// </summary>
        [Fact]
        public void Auch_Speicherbetrieb_und_Ganglinie_zaehlen_in_der_Dauerlinie_einen_Index()
        {
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Netzbezug", Reihe(96, 10.0), ChartRenderer.C_NETZ)
            };

            Assert.Equal(Achsenart.Index,
                ChartRenderer.SpeicherbetriebModell("Betrieb", reihen, "kW", null, null, true)
                             .Flaeche.X);
            Assert.Equal(Achsenart.Stunden,
                ChartRenderer.SpeicherbetriebModell("Betrieb", reihen, "kW", null, null, false)
                             .Flaeche.X);

            Assert.Equal(Achsenart.Index,
                ChartRenderer.GanglinieNormiertModell("Ganglinie", reihen, "%",
                                                      ChartRenderer.Achse.Jahresstunden, true)
                             .Flaeche.X);

            // Die Zeichenflaeche geht den Maler nichts an: Das Bild bleibt, wie es war.
            Assert.Equal(
                ChartRenderer.Speicherbetrieb("Betrieb", reihen, "kW", null, null, true),
                SkiaMaler.Png(ChartRenderer.SpeicherbetriebModell(
                    "Betrieb", reihen, "kW", null, null, true)));
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        /// <summary>Eine glatte Reihe ohne Zufall.</summary>
        private static double[] Reihe(int n, double hub)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++)
                w[i] = Math.Round(hub * (1.0 + Math.Sin(2 * Math.PI * i / n)), 3);
            return w;
        }

        /// <summary>
        /// Ein synthetischer Lauf über zwei Tage im Viertelstundenraster — dieselbe
        /// Bauart wie in <c>SpeicherBetriebsbildTests</c>: tagsüber mehr Last und eine
        /// Erzeugungsspitze, damit der Speicher überhaupt etwas zu tun hat.
        /// </summary>
        private static SpeicherEingang Speichereingang()
        {
            const int n = 192;
            var last = new double[n];
            var pv = new double[n];
            for (int i = 0; i < n; i++)
            {
                int viertel = i % 96;
                last[i] = viertel >= 8 * 4 && viertel < 20 * 4 ? 40.0 : 30.0;
                pv[i] = viertel >= 10 * 4 && viertel < 16 * 4 ? 60.0 : 0.0;
            }
            return SpeicherEingang.MitFixpreis(last, pv, 30.0);
        }

        private static SpeicherErgebnis Speicherergebnis(SpeicherEingang eingang)
            => new Dauernutzung(SpeicherModus.Energetisch).Berechne(eingang, new SpeicherParameter
            {
                CNomKwh = 100.0,
                PKw = 50.0,
                SoCMinKwh = 0.0,
                SoCMaxKwh = 100.0,
                RoundTripWirkungsgrad = 0.9,
                DtH = 0.25,
                CCapEurProKwh = 300.0,
                CPowEurProKw = 100.0,
                IFixEur = 0.0,
                Kapitalzins = 0.0,
                NutzungsdauerA = 20.0,
                DegradationProA = 0.0,
                CVerEurProKwhZyklus = 0.0
            });
    }
}
