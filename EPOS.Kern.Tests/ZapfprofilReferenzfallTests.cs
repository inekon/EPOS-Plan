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
    /// <b>Der unabhängige Referenzfall über 8760 Stunden</b> (Umsetzungskonzept
    /// Zapfprofilgenerator Kapitel 7, Zeile Z1; Methodikkonzept 3.6 P1).
    ///
    /// <para>Das Python-Skript <c>Proben/Zapfprofil/referenzfall_bauen.py</c> rechnet aus
    /// <c>referenzfall_eingabe.json</c> (zwei fiktive Zonen, fiktiver Katalog mit runden Werten,
    /// Feiertage, Ferienfenster, Messwert in m³, Zirkulation nach der Methode Flächenkennwert) die
    /// Stundenwerte von Zapfung und Zirkulation nach den Formeln des Papiers — ohne den C#-Code —
    /// und legt sie mit neun Nachkommastellen ab. Dieser Test rechnet dieselbe Eingabe mit
    /// <see cref="ZapfprofilRechner"/> und verlangt je Stunde <b>Abweichung 0 nach Rundung auf
    /// 1e-9 kWh</b> (|C# − Referenz| ≤ ½ · 1e-9) sowie gleiche Monats- und Jahressummen und
    /// Kennzahlen. Alle Werte sind erfunden; keine Normzahl.</para>
    /// </summary>
    public sealed class ZapfprofilReferenzfallTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        /// <summary>Halbe Rundungsstelle: „gleich nach Rundung auf 1e-9" (mit einem Hauch Zahlenrand).</summary>
        private const double RUNDUNG = 0.5e-9 + 1e-15;

        private static readonly Provenienz Fiktiv =
            new Provenienz("Referenzfall (fiktiv)", null, "REF-Z1", Herkunftsart.Fiktiv);

        [Fact]
        public void Der_Referenzfall_stimmt_je_Stunde_auf_neun_Stellen()
        {
            (Zapfprofileingang ein, List<Nutzungsart> katalog) = Eingang();
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(ein, katalog);
            Assert.True(e.Vollstaendig, string.Join("; ", e.Ablehnungen.Select(a => a.Klartext)));

            (double[] zapf, double[] zirk) = Stunden();
            int abweichend = 0;
            double groesste = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                double dz = Math.Abs(e.Zapfung.StundenKwh[h] - zapf[h]);
                double dc = Math.Abs(e.Zirkulation.StundenKwh[h] - zirk[h]);
                groesste = Math.Max(groesste, Math.Max(dz, dc));
                // Gleich nach Rundung: der auf neun Stellen gerundete C#-Wert ist der Wert der CSV
                // (bis auf die Darstellung der Dezimalzahl als double), und der ungerundete liegt
                // höchstens eine halbe Stelle daneben.
                bool gleichGerundet = Math.Abs(Math.Round(e.Zapfung.StundenKwh[h], 9) - zapf[h]) <= 1e-12
                                      && Math.Abs(Math.Round(e.Zirkulation.StundenKwh[h], 9) - zirk[h]) <= 1e-12;
                if (!gleichGerundet || dz > RUNDUNG || dc > RUNDUNG) abweichend++;
            }
            Assert.True(abweichend == 0,
                abweichend + " Stunden weichen nach Rundung auf 1e-9 kWh ab; größte Abweichung "
                + groesste.ToString("E3", CultureInfo.InvariantCulture) + " kWh.");
        }

        [Fact]
        public void Der_Referenzfall_stimmt_in_Monats_und_Jahressummen_und_Kennzahlen()
        {
            (Zapfprofileingang ein, List<Nutzungsart> katalog) = Eingang();
            ZapfprofilErgebnis e = ZapfprofilRechner.Rechnen(ein, katalog);
            Dictionary<string, double> k = Kennzahlen();

            Gleich(k["jahr_zapfung_kwh"], e.Zapfung.JahressummeKwh, "Jahr Zapfung");
            Gleich(k["jahr_zirkulation_kwh"], e.Zirkulation.JahressummeKwh, "Jahr Zirkulation");
            for (int m = 0; m < 12; m++)
            {
                string mm = (m + 1).ToString("00", CultureInfo.InvariantCulture);
                Gleich(k["monat_" + mm + "_zapfung_kwh"], e.Zapfung.MonatssummenKwh[m], "Monat " + mm + " Zapfung");
                Gleich(k["monat_" + mm + "_zirkulation_kwh"], e.Zirkulation.MonatssummenKwh[m], "Monat " + mm + " Zirkulation");
            }
            for (int z = 0; z < 2; z++)
            {
                Gleich(k["zone_" + (z + 1) + "_zapfung_kwh"], e.JeZone[z].Zapfung.JahressummeKwh, "Zone " + (z + 1) + " Zapfung");
                Gleich(k["zone_" + (z + 1) + "_zirkulation_kwh"], e.JeZone[z].Zirkulation.JahressummeKwh, "Zone " + (z + 1) + " Zirkulation");
            }
            Gleich(k["zone_2_kalibrierfaktor"], e.JeZone[1].Kalibrierfaktor.Value, "Kalibrierfaktor");
            Gleich(k["zirkulation_gewicht"], e.Zirkulationsansatz.Gewicht, "Gewicht α");
            Gleich(k["zirkulation_leistung_kw"], e.Zirkulationsansatz.LeistungKw, "Leistung");
            Gleich(k["zirkulation_jahresverlust_kwh"], e.Zirkulationsansatz.JahresverlustKwh, "Jahresverlust");
            Gleich(k["laufzeit_beginn_h"], e.Laufzeitfenster.ToList().FindIndex(x => x > 0), "Beginn der Laufzeit");
            Gleich(k["groesster_stundenwert_kw"], e.Kennzahlen.GroessterStundenwertKw, "größter Stundenwert");
            Gleich(k["volllaststunden_h"], e.Kennzahlen.VolllaststundenH, "Volllaststunden");
            Gleich(k["zirkulationsanteil"], e.Kennzahlen.Zirkulationsanteil, "Zirkulationsanteil");
            Gleich(k["zapfung_liter_je_tag"], e.Kennzahlen.ZapfungLiterJeTag.Value, "Liter je Tag");
            Gleich(k["stunden_ueber_schwelle"], e.Kennzahlen.StundenUeberSchwelle.Value, "Stunden über Schwelle");
        }

        // =================================================================================
        // Eingabe und Erwartung lesen
        // =================================================================================

        private static void Gleich(double referenz, double ist, string was)
        {
            // Referenz mit neun Stellen: gleich nach Rundung, plus relativer Rand für Summen im
            // Bereich von 1e4 kWh, deren letzte Bits die Summationsfolge bestimmt.
            double rand = RUNDUNG + 1e-13 * Math.Abs(referenz);
            Assert.True(Math.Abs(ist - referenz) <= rand,
                was + ": C# " + ist.ToString("R", CultureInfo.InvariantCulture) + ", Referenz "
                + referenz.ToString("R", CultureInfo.InvariantCulture));
        }

        private static string Ordner()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests", "Proben", "Zapfprofil");
                if (Directory.Exists(kandidat)) return kandidat;
            }
            Assert.Fail("Der Probenordner EPOS.Kern.Tests/Proben/Zapfprofil wurde nicht gefunden.");
            return null;
        }

        private static (double[] Zapf, double[] Zirk) Stunden()
        {
            string[] zeilen = File.ReadAllLines(Path.Combine(Ordner(), "referenzfall_stunden.csv"));
            var zapf = new double[8760];
            var zirk = new double[8760];
            int n = 0;
            foreach (string z in zeilen)
            {
                if (z.StartsWith("#", StringComparison.Ordinal) || z.StartsWith("stunde", StringComparison.Ordinal)) continue;
                string[] t = z.Split(',');
                int h = int.Parse(t[0], CultureInfo.InvariantCulture) - 1;
                zapf[h] = double.Parse(t[1], CultureInfo.InvariantCulture);
                zirk[h] = double.Parse(t[2], CultureInfo.InvariantCulture);
                n++;
            }
            Assert.Equal(8760, n);
            return (zapf, zirk);
        }

        private static Dictionary<string, double> Kennzahlen()
        {
            var k = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (string z in File.ReadAllLines(Path.Combine(Ordner(), "referenzfall_kennzahlen.csv")))
            {
                if (z.StartsWith("#", StringComparison.Ordinal) || z.StartsWith("groesse", StringComparison.Ordinal)) continue;
                string[] t = z.Split(',');
                k[t[0]] = double.Parse(t[1], CultureInfo.InvariantCulture);
            }
            return k;
        }

        private static double? ZahlOderNull(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

        private static double[] Gang(JsonElement stunden)
        {
            var g = new double[24];
            foreach (JsonProperty p in stunden.EnumerateObject())
                g[int.Parse(p.Name, CultureInfo.InvariantCulture)] = p.Value.GetDouble();
            return g;
        }

        private static (Zapfprofileingang, List<Nutzungsart>) Eingang()
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Ordner(), "referenzfall_eingabe.json")));
            JsonElement r = doc.RootElement;

            int jan1 = r.GetProperty("wochentag_jan1").GetInt32();
            var feiertage = new HashSet<int>(r.GetProperty("feiertage").EnumerateArray().Select(x => x.GetInt32()));
            var we = new bool[365];
            for (int d = 1; d <= 365; d++) we[d - 1] = (jan1 + d - 1) % 7 >= 5 || feiertage.Contains(d);

            Parametersatz ps = Parametersatz.Aus("REF-Z1", r.GetProperty("parameter").EnumerateObject()
                .Select(p => new ZapfParameterwert(p.Name, p.Value.GetDouble(), "", Fiktiv)).ToArray());

            var saetze = new Dictionary<int, Tagesgangsatz>();
            foreach (JsonElement s in r.GetProperty("tagesgangsaetze").EnumerateArray())
            {
                int id = s.GetProperty("id").GetInt32();
                double[][] g =
                {
                    Gang(s.GetProperty("werktag")), Gang(s.GetProperty("samstag")),
                    Gang(s.GetProperty("sonntag")), Gang(s.GetProperty("ruhetag"))
                };
                var anteile = new double[4, 24];
                for (int t = 0; t < 4; t++) for (int h = 0; h < 24; h++) anteile[t, h] = g[t][h];
                saetze[id] = new Tagesgangsatz(id, anteile, new[] { Fiktiv, Fiktiv, Fiktiv, Fiktiv })
                {
                    Bezeichner = "Referenzsatz " + id + " (fiktiv)", Katalogversion = "REF-Z1"
                };
            }

            var katalog = new List<Nutzungsart>();
            foreach (JsonElement n in r.GetProperty("nutzungsarten").EnumerateArray())
            {
                katalog.Add(new Nutzungsart(
                    n.GetProperty("id").GetInt32(), n.GetProperty("name").GetString(),
                    (ZapfBezugsart)n.GetProperty("bezugsart").GetInt32(),
                    n.GetProperty("bedarf_kwh_je_einheit_tag").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                    new Temperaturbezug(n.GetProperty("bezug_zapftemperatur_c").GetDouble(),
                                        n.GetProperty("bezug_kaltwasser_c").GetDouble()),
                    (ZapfBilanzgrenze)n.GetProperty("bilanzgrenze").GetInt32(),
                    (ZapfKalenderart)n.GetProperty("kalenderart").GetInt32(),
                    ZahlOderNull(n, "ferienfaktor"),
                    n.GetProperty("monate").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                    n.GetProperty("woche").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                    saetze[n.GetProperty("tagesgangsatz").GetInt32()],
                    new Katalogherkunft(Fiktiv, new Bedarfsbandbreite(new double?[3], new double?[3]), Fiktiv, Fiktiv,
                                        new[] { Fiktiv, Fiktiv, Fiktiv, Fiktiv })) { Katalogversion = "REF-Z1" });
            }

            var zonen = new List<ZonenStand>();
            int reihenfolge = 0;
            foreach (JsonElement z in r.GetProperty("zonen").EnumerateArray())
            {
                var beginn = new int?[4];
                var ende = new int?[4];
                int i = 0;
                foreach (JsonElement f in z.GetProperty("ferien").EnumerateArray())
                {
                    beginn[i] = f[0].ValueKind == JsonValueKind.Number ? f[0].GetInt32() : null;
                    ende[i] = f[1].ValueKind == JsonValueKind.Number ? f[1].GetInt32() : null;
                    i++;
                }
                double? m3 = ZahlOderNull(z, "messwert_m3");
                double? satz = ZahlOderNull(z, "tagesgangsatz");
                zonen.Add(new ZonenStand
                {
                    Id = z.GetProperty("id").GetInt32(),
                    Name = z.GetProperty("name").GetString(),
                    IdNutzungsart = z.GetProperty("nutzungsart").GetInt32(),
                    IdTagesgangsatz = satz.HasValue ? (int)satz.Value : null,
                    Bezugsmenge = z.GetProperty("bezugsmenge").GetDouble(),
                    Niveau = (ZapfNiveau)z.GetProperty("niveau").GetInt32(),
                    Zirkulation = z.GetProperty("zirkulation").GetBoolean(),
                    Ferienbeginn = beginn,
                    Ferienende = ende,
                    ZapftemperaturC = ZahlOderNull(z, "zapftemperatur_c"),
                    KaltwasserMittelC = ZahlOderNull(z, "kaltwasser_mittel_c"),
                    KaltwasserAmplitudeK = ZahlOderNull(z, "kaltwasser_amplitude_k"),
                    Jahresmesswert = m3,
                    JahresmesswertEinheit = m3.HasValue ? ZapfMesswerteinheit.KubikmeterJeJahr : null,
                    Reihenfolge = ++reihenfolge
                });
            }

            JsonElement p = r.GetProperty("projekt");
            var projekt = new ProjektStand
            {
                Id = 1, Weg = BrauchwasserWeg.Generator, ZirkAuto = true,
                ZirkMethode = (ZapfZirkulationsmethode)p.GetProperty("zirk_methode").GetInt32(),
                ZirkFlaecheM2 = ZahlOderNull(p, "zirk_flaeche_m2"),
                Speicherart = ZapfSpeicherart.Ladespeicher, Realisierungen = 10, Perzentil = 99, Seed = 1, LadeAuto = true
            };

            var ein = new Zapfprofileingang
            {
                Zonen = zonen, Projekt = projekt, WochentagJan1 = jan1, We = we, Parameter = ps,
                Tagesgangsaetze = saetze.Values.ToList(),
                AnzeigetemperaturC = r.GetProperty("anzeigetemperatur_c").GetDouble(),
                SchwelleKw = r.GetProperty("schwelle_kw").GetDouble()
            };
            return (ein, katalog);
        }
    }
}
