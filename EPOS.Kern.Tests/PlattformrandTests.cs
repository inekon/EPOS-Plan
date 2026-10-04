using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids zum Plattformbefund PB‑1</b> vom 29.09.2026
    /// („Rand an Phase G und Quellpuffer, neue Basis R25“). Die Projekte 1008, 1023 und
    /// 1042 rechneten auf Windows und Linux verschieden, weil die C-Bibliotheken beider
    /// Plattformen <c>Math.Sin</c>/<c>Exp</c> im letzten Bit verschieden runden und zwei
    /// Entscheidungen des Rechenkerns an diesem Bit hingen:
    ///
    /// <list type="number">
    /// <item>die Abschaltprüfung der Phase G in <c>Kaskadenschleife</c> — sie verglich den
    /// Füllstand ohne Zahlenrand mit <c>Q_max · SchwelleAus</c>, auf den die Nachentladung
    /// ihn gerade gesteuert hatte; jetzt nimmt sie
    /// <see cref="SimulationPufferspeicher.AbschaltschwelleErreicht"/>, dieselbe Prüfung
    /// wie die Hysterese;</item>
    /// <item>die Begrenzung einer Wärmepumpe durch ihren Quellspeicher — ein Rest von
    /// 10⁻¹⁶ kWh skalierte das Modul auf ebenso wenig, und die Laufzeitzählung wertete
    /// das als volle Betriebsstunde; jetzt gilt ein Rest unter
    /// <see cref="Rechenrand.ABSOLUT"/> als leer
    /// (<see cref="SimulationWaermepumpe.QuellInhalt"/>).</item>
    /// <item>der Kessellauf (Nachzug zum Entscheid) — ein Kessel, der nur den Rest einer
    /// Vorstufe von 10⁻¹⁶ kWh deckte, zählte eine Laufstunde und einen Start; jetzt läuft er
    /// erst ab <see cref="Rechenrand.ABSOLUT"/> (<see cref="SimulationSPK.KesselLaeuft"/>).</item>
    /// </list>
    ///
    /// <para>Die Bauform der Randproben ist die von <see cref="RechenrandTests"/>: ein
    /// Stand GENAU auf der Grenze und einer um ein ulp daneben liefern dieselbe
    /// Entscheidung, und eine GEGENPROBE hält fest, dass der blanke Vergleich auf
    /// denselben Zahlen verschieden ausfällt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PlattformrandTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public PlattformrandTests(TestDatenbank db) { _db = db; }

        // =====================================================================
        //  1 — Die Abschaltschwelle, auch in Phase G
        // =====================================================================

        /// <summary>
        /// Ein Füllstand GENAU auf <c>Q_max · SchwelleAus</c> und einer um ein ulp darunter
        /// haben die Abschaltschwelle beide erreicht.
        /// </summary>
        [Fact]
        public void Die_Abschaltschwelle_gilt_auf_der_Grenze_und_ein_ulp_darunter_als_erreicht()
        {
            SimulationPufferspeicher sp = Speicher(13.92);
            double grenze = sp.Q_max * sp.SchwelleAus;

            sp.SOC = grenze;
            Assert.True(sp.AbschaltschwelleErreicht());

            double einUlpDarunter = Math.BitDecrement(grenze);
            sp.SOC = einUlpDarunter;
            Assert.True(sp.AbschaltschwelleErreicht());

            // GEGENPROBE: die alte Bauart der Phase G.
            Assert.False(einUlpDarunter >= grenze);

            // Und eine sichtbare Menge darunter ist NICHT erreicht - der Rand ist kein
            // Spielraum, er deckt nur die Rundung.
            sp.SOC = grenze - 1e-6;
            Assert.False(sp.AbschaltschwelleErreicht());
        }

        /// <summary>
        /// <b>Der Fall aus Projekt 1023, Stunde 2500.</b> Die Nachentladung nimmt aus dem
        /// vollen Speicher (13,92 kWh) die Menge bis zur Abschaltmarke 13,224 kWh. Auf
        /// Windows kam sie als 0,695999999999998 an, auf Linux als 0,6960000000000015 — der
        /// Füllstand landete einmal ein ulp über, einmal ein ulp unter der Marke. Mit dem
        /// Rand beenden beide den Ladebetrieb.
        /// </summary>
        [Fact]
        public void Nach_der_Nachentladung_auf_die_Marke_endet_der_Ladebetrieb_auf_beiden_Plattformen()
        {
            foreach (double entnahme in new[] { 0.695999999999998, 0.6960000000000015 })
            {
                SimulationPufferspeicher sp = Speicher(13.92);
                sp.SOC = sp.Q_max;
                Assert.Equal(entnahme, sp.Entladen(entnahme, 0));

                Assert.True(Math.Abs(sp.SOC - sp.Q_max * sp.SchwelleAus) < 1e-12,
                            "Der Füllstand liegt nicht auf der Marke: " + sp.SOC.ToString("R"));
                Assert.True(sp.AbschaltschwelleErreicht(),
                            "Entnahme " + entnahme.ToString("R") + ": Der Ladebetrieb endet nicht.");
            }

            // GEGENPROBE: Der blanke Vergleich entschied die beiden Fälle verschieden.
            Assert.True(13.92 - 0.695999999999998 >= 13.92 * 0.95);
            Assert.False(13.92 - 0.6960000000000015 >= 13.92 * 0.95);
        }

        /// <summary>
        /// Die Hysterese nimmt dieselbe Prüfung — sie ist bitgleich zur Bauart vor dem
        /// Entscheid, weil sie den Rand schon trug.
        /// </summary>
        [Fact]
        public void Die_Hysterese_nimmt_dieselbe_Pruefung()
        {
            SimulationPufferspeicher sp = Speicher(13.92);
            sp.LaedtGerade = true;
            sp.SOC = Math.BitDecrement(sp.Q_max * sp.SchwelleAus);

            Assert.True(sp.AbschaltschwelleErreicht());
            Assert.True(sp.HystereseFortschreiben());
            Assert.False(sp.LaedtGerade);
        }

        /// <summary>
        /// <b>Wache:</b> Kein Vergleich im Rechenkern hält den Füllstand blank gegen
        /// <c>Q_max · SchwelleAus</c> — jede Prüfung dieser Marke geht über
        /// <see cref="SimulationPufferspeicher.AbschaltschwelleErreicht"/>. Kommentarzeilen
        /// zählen nicht.
        /// </summary>
        [Fact]
        public void Kein_blanker_Vergleich_mit_der_Abschaltschwelle_im_Rechenkern()
        {
            var muster = new Regex(@"(>=|<=|>|<)\s*[\w.]*Q_max\s*\*\s*[\w.]*SchwelleAus\b|[\w.]*Q_max\s*\*\s*[\w.]*SchwelleAus\b\s*(>=|<=|>|<)");
            var funde = new List<string>();
            string kern = Path.Combine(Wurzel(), "EPOS.Kern");

            foreach (string datei in Directory.EnumerateFiles(kern, "*.cs", SearchOption.AllDirectories))
            {
                if (datei.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                    || datei.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)) continue;

                string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    if (IstKommentar(zeilen[i])) continue;
                    if (muster.IsMatch(zeilen[i]))
                        funde.Add(Path.GetRelativePath(kern, datei) + ":" + (i + 1) + "  " + zeilen[i].Trim());
                }
            }

            Assert.True(funde.Count == 0,
                        "Blanker Vergleich mit Q_max · SchwelleAus - AbschaltschwelleErreicht() nehmen:\n"
                        + string.Join("\n", funde));
        }

        // =====================================================================
        //  2 — Der Quellspeicher der Wärmepumpe
        // =====================================================================

        /// <summary>
        /// Ein Rest unter <see cref="Rechenrand.ABSOLUT"/> gilt als leer; alles ab dem Rand
        /// bleibt, wie es ist.
        /// </summary>
        [Fact]
        public void Ein_Rest_unter_dem_Rand_gilt_als_leerer_Quellspeicher()
        {
            SimulationPufferspeicher q = Speicher(100.0);

            q.SOC = 4.423132784291063E-16;           // der Rest aus Projekt 1042, Stunde 1746
            Assert.Equal(0.0, SimulationWaermepumpe.QuellInhalt(q));

            q.SOC = Math.BitDecrement(Rechenrand.ABSOLUT);
            Assert.Equal(0.0, SimulationWaermepumpe.QuellInhalt(q));

            q.SOC = Rechenrand.ABSOLUT;
            Assert.Equal(Rechenrand.ABSOLUT, SimulationWaermepumpe.QuellInhalt(q));

            q.SOC = 2.395559054621648;
            Assert.Equal(2.395559054621648, SimulationWaermepumpe.QuellInhalt(q));

            q.SOC = 0;
            Assert.Equal(0.0, SimulationWaermepumpe.QuellInhalt(q));
        }

        /// <summary>
        /// <b>Wache:</b> In <c>SimulationWaermepumpe</c> liest nur
        /// <see cref="SimulationWaermepumpe.QuellInhalt"/> den Füllstand eines
        /// Quellspeichers — sonst begrenzte eine zweite Stelle das Modul wieder mit dem Rest.
        /// </summary>
        [Fact]
        public void Nur_QuellInhalt_liest_den_Fuellstand_des_Quellspeichers()
        {
            string datei = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Simulation", "SimulationWaermepumpe.cs");
            string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
            var funde = new List<string>();
            for (int i = 0; i < zeilen.Length; i++)
            {
                if (IstKommentar(zeilen[i])) continue;
                if (zeilen[i].Contains("quelle.SOC") && !zeilen[i].Contains("return quelle.SOC < Rechenrand.ABSOLUT"))
                    funde.Add((i + 1) + "  " + zeilen[i].Trim());
            }
            Assert.True(funde.Count == 0, "quelle.SOC ausserhalb von QuellInhalt:\n" + string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Wache am Lauf:</b> Projekt 1042 führt eine Wärmepumpe (Modul 1) an einem
        /// Quellspeicher. Vor dem Entscheid zählte sie auf Windows 2 073,4 Betriebsstunden,
        /// davon 498 mit einer Ladung unter 10⁻⁹ kWh; ohne diese Scheinstunden sind es
        /// 1 575,4 — die Zahl der Basen R25 und R26 (`aggregate.csv`, `WaermepumpeModul[1]`). Das
        /// andere Modul hängt an keiner Quelle und bleibt bei 5 995,29.
        ///
        /// <para>Der Referenzlauf der CI rechnet 1042 nicht mit; diese Probe hält die Zahl
        /// deshalb auch dort.</para>
        /// </summary>
        [Fact]
        public void Projekt_1042_zaehlt_keine_Scheinstunden_am_Quellspeicher()
        {
            if (!_db.Vorhanden) return;

            using (new Kulturvorrichtung())
            {
                SimulationRunner l = new SimulationRunner();
                string fehler;
                Assert.True(l.Simuliere(1042, out fehler), "Lauf gescheitert: " + fehler);

                SimulationWaermepumpe wp = l.sim.simulation_wp;
                Assert.True(wp.wp_list.Count == 2, "Projekt 1042 führt nicht mehr zwei Module.");
                // Rechenweg RP2a (Erdreichwiderstand): vorher 5 995,29 und 1 575,4.
                Assert.Equal(5925.57, wp.Modul_WP_Laufzeit[0], 2);
                Assert.Equal(1587.23, wp.Modul_WP_Laufzeit[1], 2);
            }
        }

        // =====================================================================
        //  3 — Der Kessellauf
        // =====================================================================

        /// <summary>
        /// Die gemessenen Kesselabgaben aus Projekt 1024 (Rechenreste von 4,4 bis 6,7·10⁻¹⁶ kWh)
        /// sind kein Lauf; ein ulp unter dem Rand auch nicht; ab dem Rand und jede echte Abgabe
        /// sind einer. GEGENPROBE: Der blanke Vergleich <c>&gt; 0</c> hätte jeden Rest als Lauf
        /// gezählt.
        /// </summary>
        [Theory]
        [InlineData(4.440892098500626E-16)]
        [InlineData(6.106226635438361E-16)]
        [InlineData(6.661338147750939E-16)]
        [InlineData(double.Epsilon)]
        public void Ein_Rest_unter_dem_Rand_ist_kein_Kessellauf(double rest)
        {
            Assert.False(SimulationSPK.KesselLaeuft(rest));
            Assert.False(SimulationSPK.KesselLaeuft(Math.BitIncrement(rest)));
            Assert.False(SimulationSPK.KesselLaeuft(0.0));

            // GEGENPROBE
            Assert.True(rest > 0);
        }

        [Fact]
        public void Ab_dem_Rand_laeuft_der_Kessel()
        {
            Assert.False(SimulationSPK.KesselLaeuft(Math.BitDecrement(Rechenrand.ABSOLUT)));
            Assert.True(SimulationSPK.KesselLaeuft(Rechenrand.ABSOLUT));
            Assert.True(SimulationSPK.KesselLaeuft(1e-6));
            Assert.True(SimulationSPK.KesselLaeuft(1.24010381));
        }

        /// <summary>
        /// <b>Wache:</b> In <c>SimulationSPK</c> entscheidet nur
        /// <see cref="SimulationSPK.KesselLaeuft"/>, ob der Kessel läuft — ein blanker Vergleich
        /// der Abgabe mit 0 zählte den Rest wieder als Laufstunde.
        /// </summary>
        [Fact]
        public void Nur_KesselLaeuft_entscheidet_den_Kessellauf()
        {
            string datei = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Simulation", "SimulationSPK.cs");
            string[] zeilen = File.ReadAllText(datei).Replace("\r\n", "\n").Split('\n');
            var blank = new Regex(@"_kesselAbgabe\[\w+\]\s*(>|>=|!=)\s*0(\.0)?\b");
            var funde = new List<string>();
            bool gerufen = false;
            for (int i = 0; i < zeilen.Length; i++)
            {
                if (IstKommentar(zeilen[i])) continue;
                if (blank.IsMatch(zeilen[i])) funde.Add((i + 1) + "  " + zeilen[i].Trim());
                if (zeilen[i].Contains("KesselLaeuft(_kesselAbgabe[")) gerufen = true;
            }
            Assert.True(funde.Count == 0, "Blanker Vergleich der Kesselabgabe:\n" + string.Join("\n", funde));
            Assert.True(gerufen, "Stunde_Abschluss ruft KesselLaeuft nicht.");
        }

        /// <summary>
        /// <b>Wache am Lauf:</b> Projekt 1024 führt einen Elektrokessel (0 kW Bereitschaft), der in
        /// 26 Stunden nur Rechenreste deckte. Ohne sie: 4 895 Laufstunden, 233 Starts, 2 608
        /// Bereitschaftsstunden — die Zahlen der Basis R26 (<c>Kessel[0].*</c> in
        /// <c>aggregate.csv</c>). Die CI rechnet 1024 nicht mit; diese Probe hält die Zahlen dort.
        /// </summary>
        [Fact]
        public void Projekt_1024_zaehlt_keine_Kesselstunden_aus_Rechenresten()
        {
            if (!_db.Vorhanden) return;

            using (new Kulturvorrichtung())
            {
                SimulationRunner l = new SimulationRunner();
                string fehler;
                Assert.True(l.Simuliere(1024, out fehler), "Lauf gescheitert: " + fehler);

                SimulationSPK spk = l.sim.simulation_spk;
                // Rechenweg RP2a (Erdreichwiderstand): vorher 4 895 / 233 / 2 608.
                Assert.Equal(4652, spk.Laufstunden_Spk[0]);
                Assert.Equal(233, spk.Starts_Spk[0]);
                Assert.Equal(2594, spk.Bereitschaftsstunden_Spk[0]);
            }
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static SimulationPufferspeicher Speicher(double qMax)
        {
            return new SimulationPufferspeicher
            {
                Q_max = qMax,
                SchwelleEin = 0.10,
                SchwelleAus = 0.95,
                SOC = 0
            };
        }

        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal)
                || s.StartsWith("*", StringComparison.Ordinal)
                || s.StartsWith("/*", StringComparison.Ordinal);
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
