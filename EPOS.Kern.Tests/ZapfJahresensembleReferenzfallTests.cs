using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der unabhängige Referenzfall des Jahresensembles</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.3 Satz 3, 4.2, 4.4; Kapitel 7, Zeile Z3; Muster der Referenzfälle Z1,
    /// Z2 und des Auslegungsensembles).
    ///
    /// <para>Das Python-Skript <c>Proben/Zapfprofil/jahresensemble_referenzfall_bauen.py</c> rechnet
    /// aus <c>jahresensemble_referenzfall_eingabe.json</c> — eine Zone mit Index 1, drei Einheiten,
    /// drei erfundene Kategorien, vier Realisierungen — nach den Formeln des Papiers, ohne den
    /// C#-Code: Kalender mit Feiertag und Ferien, Entkopplung der Urlaube (ein Fenster wird über den
    /// Jahreswechsel verschoben und geteilt), Tagesmengen je Einheit mit Monats- und
    /// Kaltwasserfaktor, Ereignisse mit der Spreizung des Monats, Minuten über Mitternacht und über
    /// den Jahreswechsel (Dezember → Januar), Stundenreihen, die Konsistenzprobe aus den R Jahren und
    /// die <b>Bilanz als Realisierung zum Seed mal dem Faktor der Energieprobe</b>. Dieser Test
    /// rechnet dieselbe Eingabe mit <see cref="Jahresensemble"/> und verlangt <b>Abweichung 0 auf
    /// 1e-9</b>: |C# − Referenz| ≤ ½ · 1e-9 + 1e-12 · |Referenz| — das Skript schreibt zwölf
    /// Nachkommastellen und rechnet jede Summe in derselben Folge, die Schranke deckt allein die
    /// Rundung der Ausgabe (Jahreswerte um 5000 kWh tragen dort rund 1e-12 relativ). Alle Werte sind
    /// erfunden; keine Normzahl.</para>
    /// </summary>
    public sealed class ZapfJahresensembleReferenzfallTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Provenienz Fiktiv = new Provenienz("Referenzfall (fiktiv)", null, "REF-Z3", Herkunftsart.Fiktiv);

        [Fact]
        public void Das_Jahresensemble_stimmt_auf_neun_Stellen()
        {
            Dictionary<string, double> r = Referenz();
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ZapfZufallTests.Probenordner(),
                                                                                       "jahresensemble_referenzfall_eingabe.json")));
            JsonElement e = doc.RootElement;
            JsonElement zone = e.GetProperty("zone");
            long seed = e.GetProperty("seed").GetInt64();
            int realisierungen = e.GetProperty("realisierungen").GetInt32();
            int jan1 = e.GetProperty("wochentag_jan1").GetInt32();
            var feiertage = new HashSet<int>(e.GetProperty("feiertage").EnumerateArray().Select(x => x.GetInt32()));
            bool[] we = Enumerable.Range(1, Zapfkalender.TAGE)
                .Select(d => Zapfkalender.Wochentag(jan1, d) >= Zapfkalender.SAMSTAG || feiertage.Contains(d)).ToArray();
            Ferienfenster[] ferien = e.GetProperty("ferien").EnumerateArray()
                .Select(f => new Ferienfenster(f[0].GetInt32(), f[1].GetInt32())).ToArray();

            string[] namen = { "werktag", "samstag", "sonntag", "ruhetag" };
            var gaenge = new double[4, 24];
            var leer = new bool[4];
            for (int t = 0; t < 4; t++)
            {
                double[] g = Zahlen(e.GetProperty("gaenge").GetProperty(namen[t]));
                for (int h = 0; h < 24; h++) gaenge[t, h] = g[h];
                leer[t] = g.Sum() == 0.0;
            }
            JsonElement ff = e.GetProperty("ferienfaktor");
            var s = new Zeitstruktur(Zahlen(e.GetProperty("monate")), Zahlen(e.GetProperty("woche")),
                                     ff.ValueKind == JsonValueKind.Null ? null : ff.GetDouble(), gaenge, leer);
            Zapfkategorie[] kategorien = e.GetProperty("kategorien").EnumerateArray().Select(k =>
                new Zapfkategorie(k.GetProperty("id_art").GetInt32(), k.GetProperty("name").GetString(),
                                  k.GetProperty("mu").GetDouble(), k.GetProperty("sigma").GetDouble(),
                                  k.GetProperty("dauer").GetInt32(), k.GetProperty("anteil").GetDouble(), Fiktiv)
                {
                    KappungLJeMin = k.GetProperty("kappung").ValueKind == JsonValueKind.Null ? null : k.GetProperty("kappung").GetDouble()
                }).ToArray();
            string name = zone.GetProperty("zone").GetString();
            double jahresKwh = zone.GetProperty("jahresmenge_kwh").GetDouble();
            double[] kaltwasser = Zahlen(e.GetProperty("kaltwasserfaktor"));
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(jan1, we, ferien);
            int versatz = e.GetProperty("urlaubsversatz_tage").GetInt32();
            var z = new Jahreszone
            {
                Index = zone.GetProperty("index").GetInt32(), Zone = name, Einheiten = zone.GetProperty("einheiten").GetInt32(),
                Kategorien = Zapfkategoriensatz.Aus(kategorien, zone.GetProperty("id_art").GetInt32(), name),
                JahresmengeKwh = jahresKwh, Struktur = s, Kalender = kalender, Ferien = ferien,
                Kaltwasserfaktor = kaltwasser, SpreizungJeMonatK = Zahlen(e.GetProperty("spreizung_k")),
                WochentagJan1 = jan1, We = we, Urlaubsentkopplung = true, UrlaubsversatzTage = versatz
            };

            // Parallel gerechnet — Blöcke und Fäden ändern kein Bit (seriell zur Gegenprobe).
            Jahresensemble ens = Jahresensemble.Ziehen(z, seed, realisierungen);
            var det = new Bilanzreihe(Formvektor.Stundenreihe(
                Formvektor.Tagesmengen(jahresKwh, s, kalender, jan1, kaltwasser, name), s, kalender));
            Jahreskonsistenz k = ens.Pruefen(det);
            Bilanzreihe bilanz = ens.Bilanz(k);

            int v = 0;
            for (int i = 0; i < realisierungen; i++)
            {
                v += Gleich(r, "r" + i + "_jahr_kwh", ens.JahresenergienKwh[i]);
                // Der Versatz jeder Einheit ist die erste Ziehung ihres Stroms (Realisierung, Zone, Einheit).
                for (int u = 0; u < z.Einheiten; u++)
                {
                    var strom = new ZapfZufall(ZapfZufall.Kindseed(ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(seed, i), z.Index), u));
                    v += Gleich(r, "r" + i + "_einheit" + u + "_versatz", strom.Ganzzahl(2 * versatz + 1) - versatz);
                }
            }
            v += Gleich(r, "mittel_jahr_kwh", ens.Mittel.JahressummeKwh);
            for (int m = 0; m < Zapfkalender.MONATE; m++)
                v += Gleich(r, "mittel_monat_" + (m + 1).ToString("00", CultureInfo.InvariantCulture) + "_kwh", ens.Mittel.MonatssummenKwh[m]);

            // Die Konsistenzprobe aus den R Jahren.
            v += Gleich(r, "deterministisch_kwh", k.DeterministischKwh);
            v += Gleich(r, "mittel_kwh", k.MittelKwh);
            v += Gleich(r, "standardabweichung_kwh", k.StandardabweichungKwh);
            v += Gleich(r, "toleranz_kwh", k.ToleranzKwh);
            v += Gleich(r, "erfuellt", k.Erfuellt ? 1.0 : 0.0);
            v += Gleich(r, "jahr_zum_seed_kwh", k.JahrZumSeedKwh);
            v += Gleich(r, "faktor", k.Faktor);
            v += Gleich(r, "tagesgangabweichung", k.TagesgangAbweichung);
            Assert.Equal(realisierungen, k.Realisierungen);

            // Die Bilanz: Realisierung zum Seed mal Faktor — jede Stunde.
            v += Gleich(r, "bilanz_jahr_kwh", bilanz.JahressummeKwh);
            for (int m = 0; m < Zapfkalender.MONATE; m++)
                v += Gleich(r, "bilanz_monat_" + (m + 1).ToString("00", CultureInfo.InvariantCulture) + "_kwh", bilanz.MonatssummenKwh[m]);
            for (int h = 0; h < Bilanzreihe.STUNDEN; h++)
                v += Gleich(r, "bilanz_stunde_" + h.ToString("0000", CultureInfo.InvariantCulture), bilanz.StundenKwh[h]);

            Assert.Equal(r.Count, v);
            Assert.True(v > 8800, v + " Größen verglichen.");
            // Die Bilanz ist nicht das Mittel der R Jahre (mit demselben Faktor auf die Jahresmenge gebracht).
            Assert.NotEqual(ens.Mittel.Mal(k.DeterministischKwh / ens.Mittel.JahressummeKwh).StundenKwh, bilanz.StundenKwh);
            Assert.NotEqual(1.0, k.Faktor);
            // Seriell dieselben Bits.
            Jahresensemble seriell = Jahresensemble.Ziehen(z, seed, realisierungen, parallel: false);
            Assert.Equal(ens.JahrZumSeed.StundenKwh.Select(BitConverter.DoubleToInt64Bits),
                         seriell.JahrZumSeed.StundenKwh.Select(BitConverter.DoubleToInt64Bits));
            Assert.Equal(ens.Mittel.StundenKwh.Select(BitConverter.DoubleToInt64Bits),
                         seriell.Mittel.StundenKwh.Select(BitConverter.DoubleToInt64Bits));
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static double[] Zahlen(JsonElement feld) => feld.EnumerateArray().Select(x => x.GetDouble()).ToArray();

        private static int Gleich(Dictionary<string, double> r, string name, double ist)
        {
            Assert.True(r.TryGetValue(name, out double soll), "Die Referenz nennt „" + name + "“ nicht.");
            double rand = 0.5e-9 + 1e-12 * Math.Abs(soll);
            Assert.True(Math.Abs(ist - soll) <= rand,
                name + ": C# " + ist.ToString("R", CultureInfo.InvariantCulture) + ", Referenz " + soll.ToString("R", CultureInfo.InvariantCulture));
            return 1;
        }

        private static Dictionary<string, double> Referenz()
        {
            var r = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (string z in File.ReadAllLines(Path.Combine(ZapfZufallTests.Probenordner(), "jahresensemble_referenzfall_ergebnis.csv")))
            {
                if (z.StartsWith("#", StringComparison.Ordinal) || z.StartsWith("groesse", StringComparison.Ordinal) || z.Length == 0)
                    continue;
                string[] t = z.Split(',');
                r.Add(t[0], double.Parse(t[1], NumberStyles.Float, CultureInfo.InvariantCulture));
            }
            return r;
        }
    }
}
