using System;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using W = EPOS.Kern.Tests.KuehlkurveReferenzprojektWacheTests;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1062 „Referenzprojekt KK Zonen"</b> (RP-KKZ; Entwurf KK Abschnitt 4, E106 Q-KK-6
    /// erweitert) — die Kopie von 1061 (RP-KK), das Gebäude über die Wege des Zonendialogs in zwei Zonen geteilt: „Süd und
    /// West“ mit der Trennwand, „Nord und Ost“ mit Gebläsekonvektor statt der Kühldecke des Gebäudes. Die gesäten Zellen
    /// schreibt <c>Referenzlaeufe/Skripte/referenzprojekt_1062_kkz.cs</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht: zwei beheizte Zonen zu gleicher Nutzfläche, Bauteile nach Himmelsrichtung,
    /// Dach und Bodenplatte je zur Hälfte, eine Trennwand (eine Seite), die Kühlübergabe der Zone Nord/Ost.</item>
    /// <item><b>1062 ist eine Kopie von 1061</b>; <b>1061 bleibt einzonig</b>.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> rechnet das Mehrzonengebäude im Kreis von AK3 mit Kühlkurve und Raumeinfluss
    /// und schreibt die drei Kennzahlen der Kühlkurve; beide Zonen kühlen.</item>
    /// </list>
    /// 1062 gehört zu den Einfrierregeln „gesäte Zonendaten“ und „gesäte Auslegungsdaten der Übergabe“.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenKuehlkurveReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public ZonenKuehlkurveReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1062;
        private const int VORLAGE = W.PROJEKT;
        private const string NAME = "Referenzprojekt KK Zonen";

        private static string Text(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);
        private static double Wert(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);

        private static DataTable Zonen(int projekt)
            => DataRepository.GetDataTable("SELECT z.* FROM Tab_Zone z JOIN Tab_Gebaeude g ON g.ID = z.ID_Gebaeude WHERE g.ID_Projekt = ? " +
                                           "ORDER BY z.Rang, z.ID", new DbParam("?", projekt));

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Text(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT))));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK3, KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, W.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));

            DataTable z = Zonen(PROJEKT);
            Assert.Equal(2, z.Rows.Count);
            DataRow sw = z.Rows[0], no = z.Rows[1];
            Assert.Equal("Süd und West", Text(sw["Bezeichner"]));
            Assert.Equal("Nord und Ost", Text(no["Bezeichner"]));
            Assert.Equal(Wert(sw["Nutzflaeche"]), Wert(no["Nutzflaeche"]));
            Assert.True(sw["Kuehl_Uebergabe_Art"] == DBNull.Value, "Die Zone Süd/West trägt eine eigene Kühlübergabe.");
            Assert.Equal(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, Text(no["Kuehl_Uebergabe_Art"]));

            DataTable b = DataRepository.GetDataTable("SELECT * FROM Tab_Bauteil WHERE ID_Zone IN (?, ?) ORDER BY ID",
                                                      new DbParam("?", sw["ID"]), new DbParam("?", no["ID"]));
            Assert.Equal(17, b.Rows.Count);
            foreach (DataRow r in b.Rows)
            {
                bool inSw = Text(r["ID_Zone"]) == Text(sw["ID"]);
                if (r["Azimut"] != DBNull.Value)
                {
                    double a = Wert(r["Azimut"]);
                    Assert.True(inSw ? a == 180.0 || a == 270.0 : a == 0.0 || a == 90.0, "Azimut " + a + " in der falschen Zone");
                }
            }
            // Die Trennwand: eine Seite (Süd/West) mit der Nachbarzone Nord/Ost.
            DataRow[] trenn = b.Rows.Cast<DataRow>().Where(r => r["ID_Nachbarzone"] != DBNull.Value).ToArray();
            DataRow t = Assert.Single(trenn);
            Assert.Equal(Text(sw["ID"]), Text(t["ID_Zone"]));
            Assert.Equal(Text(no["ID"]), Text(t["ID_Nachbarzone"]));
            Assert.Equal(30.0, Wert(t["Flaeche"]));
            Assert.Equal(DbWerte.RANDBEDINGUNG_ZONE, Text(t["Randbedingung"]));
            // Fenster: Süd/West trägt mehr Glas als Nord/Ost.
            double Glas(DataRow zone) => b.Rows.Cast<DataRow>().Where(r => Text(r["ID_Zone"]) == Text(zone["ID"])
                                                                          && Text(r["Bauteilart"]) == DbWerte.BAUTEILART_FENSTER)
                                          .Sum(r => Wert(r["Flaeche"]));
            Assert.True(Glas(sw) > Glas(no), "Glas Süd/West " + Glas(sw) + " ≤ Nord/Ost " + Glas(no));
        }

        [Fact]
        public void Projekt_1062_ist_eine_Kopie_von_1061_und_1061_bleibt_einzonig()
        {
            if (!_db.Vorhanden) return;
            W.ZellenGleich(W.Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE),
                           W.Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT), "Tab_Einstellungen");
            W.ZellenGleich(W.Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE),
                           W.Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT), "Tab_Gebaeude");
            W.ZellenGleich(W.Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", VORLAGE),
                           W.Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", PROJEKT), "Tab_WP");
            Assert.Equal(W.Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", VORLAGE),
                         W.Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", PROJEKT));
            Assert.Equal(0, Zonen(VORLAGE).Rows.Count);
            Assert.Equal(0L, W.Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID > ?", PROJEKT));
        }

        [Fact]
        public void Ein_Lauf_rechnet_beide_Zonen_im_Kreis_mit_Kuehlkurve()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            Assert.True(r.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, "Lauf " + PROJEKT + " gescheitert: " + fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);

            Ak3Weg weg = r.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.True(weg.Kreis.DurchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.Contains(weg.Gebaeude, e => e.Stepper.Zonenzahl == 2);
            KuehlRaumeinfluss k2 = weg.Kreis.KuehlRaumeinfluss;
            Assert.NotNull(k2);
            Assert.True(k2.Kuehlstunden > 100, "Kühlstunden " + k2.Kuehlstunden);

            foreach (string s in KuehlkurveSchema.SPALTEN_ERGEBNIS)
                Assert.False(W.Kennzahl(PROJEKT, s) is null or DBNull, s + " ist leer");
            double mittel = Wert(W.Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_VORLAUF_MITTEL));
            double absenkung = Wert(W.Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_ABSENKUNG_KH));
            long grenze = Convert.ToInt64(W.Kennzahl(PROJEKT, KuehlkurveSchema.SPALTE_VORLAUFGRENZE_STUNDEN), CultureInfo.InvariantCulture);
            _aus.WriteLine("1062: Vorlauf Mittel {0:0.00} °C, Absenkung {1:0.0} Kh, an der Grenze {2} h, Kühlstunden {3}, Durchläufe Mittel {4:0.000} / max {5}",
                           mittel, absenkung, grenze, k2.Kuehlstunden, weg.Kreis.DurchlaeufeMittel, weg.Kreis.DurchlaeufeMax);
            Assert.Equal(k2.KuehlVorlaufMittelC, mittel, 2);
            Assert.True(absenkung > 0.0, "Der Raumeinfluss senkt nicht.");
            Assert.True(grenze < k2.Kuehlstunden);
            Assert.True(mittel > 12.0, "Die Kurve gleitet nicht über den Anlagenvorlauf: " + mittel);

            // Beide Zonen kühlen (Kältebedarf je Zone im Ergebnis).
            DataTable zonen = DataRepository.GetDataTable(
                "SELECT z.* FROM Tab_ErgebnisZone z JOIN Tab_ErgebnisGebaeude g ON g.ID = z.ID_ErgebnisGebaeude " +
                "WHERE g.ID_Ergebnis = (SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?)", new DbParam("?", PROJEKT));
            Assert.Equal(2, zonen.Rows.Count);
            foreach (DataRow z in zonen.Rows)
                Assert.True(Wert(z["Kuehlenergie_Mwh"]) > 0.0, "Zone " + Text(z["ID_Zone"]) + " kühlt nicht.");
        }
    }
}
