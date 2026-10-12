using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gleichwertigkeitsprobe des Rückkühlwerks</b> (K-F1; Entwurf Split/VRF/Rückkühlwerk 5.6): Ein Referenzprojekt
    /// mit Kältemaschine rechnet auf einer Kopie der Testdatenbank einmal ohne und einmal mit einem Rückkühlwerk
    /// <c>TROCKEN</c>/<c>FEST</c>/leere Felder/<c>PARALLEL</c> an seiner Kältemaschinen-Anlage — die Ordner des Referenzlaufs
    /// (<c>Ergebnisexport.ProjektAusfuehren</c>) und die Ergebniszeilen der Kältemaschine (<c>Tab_ErgebnisKaeltemaschine</c>)
    /// sind byte-gleich (bis auf die Ergebnisspalten des Rückkühlwerks, die nur mit Rückkühlwerk belegt sind, K-F2). Gegenprobe:
    /// dieselbe Kopie mit +2 K Annäherung rechnet mehr Kältestrom. K-F2: eine Kopie mit Kühlturm, lastabhängiger Annäherung,
    /// Drehzahlventilator und Wasserpreis füllt die Ergebnisspalten. Ein Datenbanktest, nicht in der Basis.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class RueckkuehlwerkGleichwertigkeitTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public RueckkuehlwerkGleichwertigkeitTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Die Kältemaschinen-Anlage (Typ 13) des Projekts — genau eine.</summary>
        private static int Anlage(int projekt) =>
            Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("?", projekt), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE)), CultureInfo.InvariantCulture);

        private static string Ordner(string name)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-kf1b-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static int Export(int projekt, string ziel)
        {
            var log = new Protokoll();
            int n = Ergebnisexport.ProjektAusfuehren(projekt, ziel, log);
            Assert.True(n > 0, "Der Lauf von " + projekt + " scheiterte (" + log.Fehler + " Fehler).");
            return n;
        }

        /// <summary>Die Ergebnisspalten des Rückkühlwerks (K-F2): nur mit Rückkühlwerk belegt, darum nicht im Byte-Vergleich.</summary>
        private static readonly HashSet<string> RkSpalten =
            new HashSet<string>(RueckkuehlwerkSchema.SPALTEN_ERGEBNIS.Select(s => s.Spalte), StringComparer.OrdinalIgnoreCase);

        /// <summary>Die Ergebniszeilen der Kältemaschine des jüngsten Laufs, jede Zahl rundungsfrei als Text (ohne die Spalten des Rückkühlwerks).</summary>
        private static List<string> KaelteZeilen(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT k.* FROM Tab_ErgebnisKaeltemaschine k WHERE k.ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?) ORDER BY k.ID_Kaeltemaschine, k.ID",
                new DbParam("?", projekt));
            var zeilen = new List<string>();
            foreach (DataRow r in t.Rows)
                zeilen.Add(string.Join("|", t.Columns.Cast<DataColumn>()
                    .Where(c => c.ColumnName != "ID" && c.ColumnName != "ID_Ergebnis" && !RkSpalten.Contains(c.ColumnName))
                    .Select(c => c.ColumnName + "=" + (r[c] is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                                       : Convert.ToString(r[c], CultureInfo.InvariantCulture)))));
            return zeilen;
        }

        /// <summary>Eine Spalte der ersten Ergebniszeile der Kältemaschine des jüngsten Laufs; <c>null</c> = NULL.</summary>
        private static object KaelteWert(int projekt, string spalte)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT " + spalte + " FROM Tab_ErgebnisKaeltemaschine WHERE ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?) ORDER BY ID LIMIT 1", new DbParam("?", projekt));
            return o == null || o == DBNull.Value ? null : o;
        }

        private static double KaelteSumme(int projekt, string spalte) =>
            Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT SUM(" + spalte + ") FROM Tab_ErgebnisKaeltemaschine WHERE ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?)", new DbParam("?", projekt)), CultureInfo.InvariantCulture);

        /// <summary>Vergleicht zwei Ordner des Referenzlaufs Byte für Byte (ohne <c>protokoll.txt</c>); die Zahl der Bytes.</summary>
        private static long Bytegleich(string a, string b, out int dateien)
        {
            string[] da = Directory.GetFiles(a).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            string[] dbb = Directory.GetFiles(b).Select(Path.GetFileName).Where(f => f != "protokoll.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
            Assert.Equal(da, dbb);
            long bytes = 0;
            foreach (string f in da)
            {
                byte[] x = File.ReadAllBytes(Path.Combine(a, f)), y = File.ReadAllBytes(Path.Combine(b, f));
                Assert.True(x.AsSpan().SequenceEqual(y), f + " weicht mit Rückkühlwerk ab.");
                bytes += x.Length;
            }
            dateien = da.Length;
            return bytes;
        }

        [Theory]
        [InlineData(1055)]   // Kältemaschine mit Trocken-Rückkühler und Kältespeicher
        [InlineData(1063)]   // dieselbe mit Teillastkurve, Takten und Gütegrad-Extrapolation
        public void Ein_Rueckkuehlwerk_mit_leeren_Feldern_rechnet_bytegleich(int projekt)
        {
            if (!_db.Vorhanden) return;
            int anlage = Anlage(projekt);
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);

            string ohne = Ordner("ohne"), mit = Ordner("mit");
            try
            {
                Export(projekt, ohne);
                List<string> zeilenOhne = KaelteZeilen(projekt);
                foreach (string sp in RkSpalten) Assert.Null(KaelteWert(projekt, sp));
                double stromOhne = KaelteSumme(projekt, "Stromverbrauch_MWh");
                Assert.NotEmpty(zeilenOhne);

                RueckkuehlwerkStammCtrl.SpeicherErgebnis s = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
                {
                    Bezeichner = "RK Gleichwertigkeit", Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN,
                    Annaeherung_Weg = RueckkuehlwerkSchema.WEG_FEST, Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_PARALLEL
                });
                Assert.True(s.Ok, s.Meldung);
                Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s.Id));
                int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;

                Export(projekt, mit);
                long bytes = Bytegleich(ohne, mit, out int dateien);
                Assert.Equal(zeilenOhne, KaelteZeilen(projekt));
                // K-F2: mit Rückkühlwerk belegt - trocken ohne Wasser, Strom der Rückkühlung = Hilfsstrom der Rückkühlung,
                // die Teil-Freikühlung bleibt leer (K-F3).
                Assert.Equal(0.0, Convert.ToDouble(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_WASSER), CultureInfo.InvariantCulture));
                Assert.Equal(0L, Convert.ToInt64(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_STUNDEN_NASS), CultureInfo.InvariantCulture));
                Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_VENTILATORSTROM));
                Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_RUECKKUEHLTEMPERATUR_MITTEL));
                Assert.Null(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG));
                Assert.Null(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG_STUNDEN));
                _aus.WriteLine("{0}: mit Rückkühlwerk TROCKEN/FEST/leer/PARALLEL {1} Dateien, {2} Bytes byte-gleich, {3} Ergebniszeilen gleich",
                               projekt, dateien, bytes, zeilenOhne.Count);

                // Gegenprobe: +2 K Annäherung über dem Festwert - wärmerer Rückkühler, mehr Kältestrom.
                RueckkuehlwerkModel m = RueckkuehlwerkCtrl.Laden(kopie);
                m.Annaeherung_Nenn_K = KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K + 2.0;
                Assert.Null(RueckkuehlwerkCtrl.Speichern(m));
                var lauf = new SimulationRunner();
                Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf gescheitert: " + fehler);
                double stromWaermer = KaelteSumme(projekt, "Stromverbrauch_MWh");
                Assert.NotEqual(zeilenOhne, KaelteZeilen(projekt));
                Assert.True(stromWaermer > stromOhne,
                            "Kältestrom mit +2 K " + stromWaermer.ToString("R", CultureInfo.InvariantCulture) +
                            " MWh nicht über " + stromOhne.ToString("R", CultureInfo.InvariantCulture) + " MWh");
                Assert.DoesNotContain(lauf.Protokoll.Hinweise, h => h.Contains("RK Gleichwertigkeit", StringComparison.Ordinal));
                _aus.WriteLine("{0}: Kältestrom {1:F4} MWh, mit +2 K {2:F4} MWh", projekt, stromOhne, stromWaermer);
            }
            finally
            {
                Directory.Delete(ohne, true);
                Directory.Delete(mit, true);
            }
        }

        [Fact]
        public void Kuehlturm_lastabhaengig_mit_Drehzahlventilator_fuellt_die_Ergebnisspalten()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1055;
            int anlage = Anlage(projekt);
            var lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf gescheitert: " + fehler);
            double kaelteOhne = KaelteSumme(projekt, "Kaelteproduktion_MWh");

            RueckkuehlwerkStammCtrl.SpeicherErgebnis s = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK Kuehlturm", Bauart = RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN, Nennleistung_kW = 80,
                Annaeherung_Weg = RueckkuehlwerkSchema.WEG_LASTABHAENGIG, Ventilator_Nenn_kW = 1.5,
                Ventilator_Regelung = RueckkuehlwerkSchema.REGELUNG_DREHZAHL, Ventilator_Drehzahl_Min = 0.2,
                Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_REIHE
            });
            Assert.True(s.Ok, s.Meldung);
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s.Id));
            Assert.Null(KaeltemaschineAnlageCtrl.WasserpreisSetzen(anlage, 3.0));

            lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out fehler) > 0, "Lauf gescheitert: " + fehler);
            double ventilator = Convert.ToDouble(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_VENTILATORSTROM), CultureInfo.InvariantCulture);
            double wasser = Convert.ToDouble(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_WASSER), CultureInfo.InvariantCulture);
            long nass = Convert.ToInt64(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_STUNDEN_NASS), CultureInfo.InvariantCulture);
            double rk = Convert.ToDouble(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_RUECKKUEHLTEMPERATUR_MITTEL), CultureInfo.InvariantCulture);
            double hilfs = KaelteSumme(projekt, "Hilfsstrom_MWh");
            Assert.True(ventilator > 0 && ventilator <= hilfs + 1e-12, "Ventilator " + ventilator + " MWh, Hilfsstrom " + hilfs + " MWh");
            Assert.True(wasser > 0, "kein Wasser");
            Assert.True(nass > 0 && nass <= 8760);
            Assert.True(rk > -30 && rk < 50, "Rückkühltemperatur " + rk);
            // K-F3: die Reihe rechnet - die Spalten der Teil-Freikühlung sind belegt.
            Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG));
            Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG_STUNDEN));
            Assert.True(KaelteSumme(projekt, "Kaelteproduktion_MWh") > 0);

            // Die Hinweise je einmal: Ausweichweg ohne Feuchte, 42. BImSchV, Wasser mit Kosten, Reihe mit Spreizung (K-F3).
            Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("RK Kuehlturm", StringComparison.Ordinal) && h.Contains("BImSchV", StringComparison.Ordinal));
            string w = Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("RK Kuehlturm", StringComparison.Ordinal) && h.Contains("m³", StringComparison.Ordinal));
            Assert.Contains("€/a", w, StringComparison.Ordinal);
            Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("RK Kuehlturm", StringComparison.Ordinal) &&
                                                       h.Contains("Teil-Freikühlung", StringComparison.Ordinal));
            Assert.DoesNotContain(lauf.Protokoll.Hinweise, h => h.Contains("noch nicht gerechnet", StringComparison.Ordinal));
            Assert.Contains(lauf.Protokoll.Hinweise, h => h.Contains("Luftfeuchte", StringComparison.Ordinal));
            _aus.WriteLine("1055 mit Kühlturm: Kälte {0:F3} MWh (ohne {1:F3}), Ventilator {2:F4} MWh, Wasser {3:F1} m³, {4} Nassstunden, Rückkühlung im Mittel {5:F2} °C",
                           KaelteSumme(projekt, "Kaelteproduktion_MWh"), kaelteOhne, ventilator, wasser, nass, rk);
            _aus.WriteLine(w);
        }

        /// <summary>Kälteproduktion, freie Kühlung, Teil-Freikühlung und Strom der Kältemaschine des jüngsten Laufs.</summary>
        private static (double Kaelte, double Frei, double TeilFrei, long TeilStunden, double Strom) Kaeltebilanz(int projekt) =>
            (KaelteSumme(projekt, "Kaelteproduktion_MWh"), KaelteSumme(projekt, "FreieKuehlung_MWh"),
             Convert.ToDouble(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG) ?? 0.0, CultureInfo.InvariantCulture),
             Convert.ToInt64(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG_STUNDEN) ?? 0L, CultureInfo.InvariantCulture),
             KaelteSumme(projekt, "Stromverbrauch_MWh"));

        /// <summary>Setzt die Schaltung des Rückkühlwerks <paramref name="kopie"/>, rechnet das Projekt und liefert die Bilanz.</summary>
        private static (double Kaelte, double Frei, double TeilFrei, long TeilStunden, double Strom) LaufMit(
            int projekt, int kopie, string schaltung, out SimulationRunner lauf)
        {
            RueckkuehlwerkModel m = RueckkuehlwerkCtrl.Laden(kopie);
            m.Freikuehlung_Schaltung = schaltung;
            Assert.Null(RueckkuehlwerkCtrl.Speichern(m));
            lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, "Lauf gescheitert: " + fehler);
            return Kaeltebilanz(projekt);
        }

        /// <summary>
        /// K-F3 (Entwurf 5.5): 1055 mit einem trockenen Rückkühlwerk in Reihe (Annäherung 4 K) gegen dasselbe Rückkühlwerk
        /// parallel — die Spalten der Teil-Freikühlung sind belegt (1055 kühlt nur an Sommertagen bei Kaltwasser 6 °C: keine
        /// Stunde unter Rücklauf − 3 K, darum 0), Kälte und Strom gleich. Dann mit Kaltwasser 18 °C (Flächenkühlung), Annäherung 2 K und Rückkühlerleistung 1 kW: die
        /// Mischstunden zählen, die freie Kühlung in Reihe (ganze Stunden plus Vorkühlung) ist mindestens die parallele, der
        /// Kältestrom höchstens der parallele; die Kälteproduktion bleibt in der Größenordnung (gleicher Bedarf).
        /// </summary>
        [Fact]
        public void Trocken_in_Reihe_fuellt_die_Teil_Freikuehlung_plausibel()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1055;
            int anlage = Anlage(projekt);

            RueckkuehlwerkStammCtrl.SpeicherErgebnis s = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK Reihe", Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN, Annaeherung_Nenn_K = 4.0,
                Annaeherung_Weg = RueckkuehlwerkSchema.WEG_FEST, Freikuehlung_Schaltung = RueckkuehlwerkSchema.SCHALTUNG_PARALLEL
            });
            Assert.True(s.Ok, s.Meldung);
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s.Id));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;

            // 1. Die Daten von 1055: belegt, ohne Mischstunde.
            var parallel = LaufMit(projekt, kopie, RueckkuehlwerkSchema.SCHALTUNG_PARALLEL, out _);
            Assert.Null(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG));
            var reihe = LaufMit(projekt, kopie, RueckkuehlwerkSchema.SCHALTUNG_REIHE, out SimulationRunner lauf);
            Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG));
            Assert.NotNull(KaelteWert(projekt, RueckkuehlwerkSchema.SPALTE_TEILFREIKUEHLUNG_STUNDEN));
            Assert.Equal(0L, reihe.TeilStunden);
            Assert.Equal(parallel.Kaelte, reihe.Kaelte, 9);
            Assert.Equal(parallel.Strom, reihe.Strom, 9);
            Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("RK Reihe", StringComparison.Ordinal) &&
                                                       h.Contains("Teil-Freikühlung", StringComparison.Ordinal));
            _aus.WriteLine("1055 trocken 4 K, Kaltwasser 6 °C: Kälte {0:F3} MWh, Teil-Freikühlung {1:F3} MWh in {2} h, Strom {3:F4} / {4:F4} MWh",
                           reihe.Kaelte, reihe.TeilFrei, reihe.TeilStunden, reihe.Strom, parallel.Strom);

            // 2. Kaltwasser 18 °C an der Projektkopie der Kältemaschine (Kopie der Testdatenbank), Annäherung 2 K.
            int idKm = KaeltemaschineAnlageCtrl.Laden(anlage).IdKaeltemaschine.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine SET Kuehl_Vorlauf = ? WHERE ID = ?",
                                           new DbParam("?", 18.0), new DbParam("?", idKm));
            RueckkuehlwerkModel rm = RueckkuehlwerkCtrl.Laden(kopie);
            rm.Annaeherung_Nenn_K = 2.0;
            rm.Nennleistung_kW = 1.0;   // die Rückkühlerleistung begrenzt die Vorkühlung: Mischstunden
            Assert.Null(RueckkuehlwerkCtrl.Speichern(rm));
            parallel = LaufMit(projekt, kopie, RueckkuehlwerkSchema.SCHALTUNG_PARALLEL, out _);
            reihe = LaufMit(projekt, kopie, RueckkuehlwerkSchema.SCHALTUNG_REIHE, out _);
            Assert.True(reihe.TeilStunden > 0 && reihe.TeilStunden <= 8760, "Mischstunden " + reihe.TeilStunden);
            Assert.True(reihe.TeilFrei >= 0, "Teil-Freikühlung negativ");   // auf 0,01 MWh gerundet gespeichert
            Assert.True(reihe.TeilFrei <= reihe.Kaelte - reihe.Frei + 0.01, "Teil-Freikühlung über der Kälte der Mischstunden");
            Assert.True(reihe.Frei + reihe.TeilFrei >= parallel.Frei - 1e-9,
                        "freie Kühlung in Reihe " + (reihe.Frei + reihe.TeilFrei) + " unter parallel " + parallel.Frei);
            Assert.True(reihe.Strom <= parallel.Strom + 1e-9, "Kältestrom in Reihe " + reihe.Strom + " über parallel " + parallel.Strom);
            Assert.InRange(reihe.Kaelte, parallel.Kaelte * 0.9, parallel.Kaelte * 1.1);
            _aus.WriteLine("1055 trocken 2 K, Kaltwasser 18 °C: parallel Kälte {0:F3} MWh, frei {1:F3} MWh, Strom {2:F4} MWh; Reihe Kälte {3:F3} MWh, " +
                           "frei {4:F3} MWh, Teil-Freikühlung {5:F3} MWh in {6} h, Strom {7:F4} MWh",
                           parallel.Kaelte, parallel.Frei, parallel.Strom, reihe.Kaelte, reihe.Frei, reihe.TeilFrei, reihe.TeilStunden, reihe.Strom);
        }
    }
}
