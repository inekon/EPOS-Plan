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
    /// <b>Der unabhängige Referenzfall des Auslegungsensembles</b> (Umsetzungskonzept
    /// Zapfprofilgenerator Kapitel 7, Zeile Z3; Muster der Referenzfälle Z1 und Z2).
    ///
    /// <para>Das Python-Skript <c>Proben/Zapfprofil/ensemble_referenzfall_bauen.py</c> rechnet aus
    /// <c>ensemble_referenzfall_eingabe.json</c> — zwei Zonen mit drei bzw. zwei Einheiten, fünf
    /// erfundene Kategorien, sechs Realisierungen — nach den Formeln des Papiers, ohne den C#-Code:
    /// Zufallsströme je Realisierung, Zone und Einheit, gestutztes Mittel, Poisson-Zahl, Zeitpunkt,
    /// gekappter Volumenstrom, Superposition, Minutenwerte, Spitzen, Perzentile, Spitze je Einheit,
    /// GLF_P, der Vergleich μ + z·σ/√N, das Volumen der Summenlinie beim festen Φ_N je Realisierung
    /// und GLF_V. Dieser Test rechnet dieselbe Eingabe mit <see cref="Zapfensemble"/> und verlangt
    /// <b>Abweichung 0 auf 1e-9</b>: |C# − Referenz| ≤ ½ · 1e-9 + 1e-12 · |Referenz|. Alle Werte sind
    /// erfunden; keine Normzahl.</para>
    /// </summary>
    public sealed class ZapfensembleReferenzfallTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private static readonly Provenienz Fiktiv = new Provenienz("Referenzfall (fiktiv)", null, "REF-Z3", Herkunftsart.Fiktiv);

        [Fact]
        public void Das_Ensemble_stimmt_auf_neun_Stellen()
        {
            Dictionary<string, double> r = Referenz();
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(ZapfZufallTests.Probenordner(),
                                                                                       "ensemble_referenzfall_eingabe.json")));
            JsonElement e = doc.RootElement;
            int perzentil = e.GetProperty("perzentil").GetInt32();
            int realisierungen = e.GetProperty("realisierungen").GetInt32();
            Zapfkategorie[] kategorien = e.GetProperty("kategorien").EnumerateArray().Select(k =>
                new Zapfkategorie(k.GetProperty("id_art").GetInt32(), k.GetProperty("name").GetString(),
                                  k.GetProperty("mu").GetDouble(), k.GetProperty("sigma").GetDouble(),
                                  k.GetProperty("dauer").GetInt32(), k.GetProperty("anteil").GetDouble(), Fiktiv)
                {
                    KappungLJeMin = k.GetProperty("kappung").ValueKind == JsonValueKind.Null ? null : k.GetProperty("kappung").GetDouble()
                }).ToArray();
            Ensemblezone[] zonen = e.GetProperty("zonen").EnumerateArray().Select(z =>
            {
                double[] gang = z.GetProperty("gang").EnumerateArray().Select(x => x.GetDouble()).ToArray();
                string name = z.GetProperty("zone").GetString();
                return new Ensemblezone(z.GetProperty("index").GetInt32(), name, z.GetProperty("einheiten").GetInt32(),
                    Zapfkategoriensatz.Aus(kategorien, z.GetProperty("id_art").GetInt32(), name),
                    z.GetProperty("tagesmenge_kwh").GetDouble(), z.GetProperty("spreizung_k").GetDouble(),
                    Tageszeitdichte.Aus(ZapfereignisgeneratorTests.Struktur(gang), ZapfTagtyp.Werktag));
            }).ToArray();

            // Parallel gerechnet — die feste Summationsfolge macht es gleichgültig.
            Bedarfstagensemble ens = Zapfensemble.Ziehen(zonen, e.GetProperty("seed").GetInt64(), realisierungen, perzentil);
            int v = 0;
            for (int i = 0; i < realisierungen; i++)
            {
                Bedarfstag t = ens.Tage[i];
                v += Gleich(r, "r" + i + "_tagessumme_kwh", t.TagessummeKwh);
                v += Gleich(r, "r" + i + "_minutenspitze_kw", t.GroessteMinutenleistungKw);
                v += Gleich(r, "r" + i + "_stundenspitze_kw", t.GroessteStundenleistungKw);
                for (int m = 0; m < Bedarfstag.MINUTEN; m++)
                    v += Gleich(r, "r" + i + "_minute_" + m.ToString("0000", CultureInfo.InvariantCulture), t.MinutenKwh[m]);
            }
            v += Perzentile(r, "minutenspitze_kw", ens.MinutenspitzeKw);
            v += Perzentile(r, "stundenspitze_kw", ens.StundenspitzeKw);
            for (int z = 0; z < zonen.Length; z++)
                v += Perzentile(r, "zone" + z + "_spitze_je_einheit_kw", ens.Zonen[z].SpitzeJeEinheitKw);
            v += Gleich(r, "glf_p", ens.GleichzeitigkeitLeistung.Value);
            v += Gleich(r, "wurzel_n_kw", ens.WurzelNSchaetzungKw(e.GetProperty("quantil").GetDouble()));

            JsonElement s = e.GetProperty("summenlinie");
            var p = new Summenlinienparameter
            {
                KaltwasserAuslegungC = s.GetProperty("kaltwasser_auslegung_c").GetDouble(),
                SpeicherC = s.GetProperty("speicher_c").GetDouble(),
                Ladungsfaktor = s.GetProperty("ladungsfaktor").GetDouble(),
                SensorhoeheAnteil = s.GetProperty("sensorhoehe").GetDouble(),
                Speicherart = (ZapfSpeicherart)s.GetProperty("speicherart").GetInt32(),
                VerzoegerungMin = s.GetProperty("verzoegerung_min").GetDouble(),
                SpeicherverlustKw = s.GetProperty("speicherverlust_kw").GetDouble(),
                Zirkulation = Zirkulationslast.Keine
            };
            Speicherensemble sp = Zapfensemble.Volumina(ens, p, s.GetProperty("leistung_kw").GetDouble());
            v += Perzentile(r, "volumen_l", sp.VolumenL);
            v += Gleich(r, "glf_v", sp.GleichzeitigkeitVolumen.Value);
            Assert.Equal(0, sp.OhneNachweis);

            Assert.Equal(r.Count(kv => !kv.Key.Contains("_volumen_") || kv.Key.StartsWith("volumen_l_", StringComparison.Ordinal)), v);
            Assert.True(v > 8600, v + " Größen verglichen.");
            // Der Fall deckt die Kappung (Kategorie C, E), leere Stunden und zwei Zonen mit eigenem Index.
            Assert.Equal(new[] { 0, 2 }, zonen.Select(z => z.Index).ToArray());
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static int Perzentile(Dictionary<string, double> r, string name, Perzentilwerte w)
            => Gleich(r, name + "_p50", w.P50) + Gleich(r, name + "_p90", w.P90) + Gleich(r, name + "_p95", w.P95)
               + Gleich(r, name + "_p99", w.P99) + Gleich(r, name + "_min", w.Minimum) + Gleich(r, name + "_max", w.Maximum);

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
            foreach (string z in File.ReadAllLines(Path.Combine(ZapfZufallTests.Probenordner(), "ensemble_referenzfall_ergebnis.csv")))
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
