using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1064 „Referenzprojekt Freie Kühlung"</b> (KU3-6, Entscheid E75) — die Kopie des
    /// Kühlreferenzprojekts 1017, deren Sole-Wasser-Wärmepumpe an der Erdsonde (5 × 120 m, Kühlvorlauf 18 °C) frei über
    /// ihre Wärmequelle kühlt: Schalter <c>Kuehl_Frei</c>, Grädigkeit 4 K, Leistungsgrenze 4 kW. Gesät von
    /// <c>Referenzlaeufe/Skripte/referenzprojekt_1064_freie_kuehlung.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> der Anlagenzeile steht wie im Skript; jede übrige Fachzelle der Anlagenzeile, die
    /// Wärmepumpe mit Kühlkennlinie, das Gebäude und die Kaskade stehen wie in 1017.</item>
    /// <item><b>1064 ist das einzige Projekt mit freier Kühlung</b>; 1017 rechnet weiter ohne (Bestand der Basis).</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> kühlt frei — Jahreswerte gegen die Basis R51 —, hält die Leistungsgrenze
    /// in jeder Stunde, erreicht sie in einem Teil der Stunden (dort deckt der Verdichter den Rest), schreibt die Zähler
    /// in Ergebnis und Kennzahlen und braucht für dieselbe Kälte weniger Strom als 1017.</item>
    /// </list>
    /// 1064 gehört zur Einfrierregel „gesäte Daten der freien Kühlung eines Referenzprojekts" und als Kopie von 1017 zu
    /// „gesäte Kältedaten" und „gesäte Erdreichquellen" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class FreieKuehlungReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public FreieKuehlungReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1064, VORLAGE = 1017;
        private const string NAME = "Referenzprojekt Freie Kühlung";
        private const double GRAEDIGKEIT_K = 4.0, LEISTUNG_KW = 4.0;

        // Die Jahreswerte der Basis R51 (Referenzlaeufe/2026-10-10_R51_FreieKuehlung/Projekt_1064/aggregate.csv).
        private const int R51_FREI_STUNDEN = 125;
        private const double R51_FREI_MWH = 0.277361939;
        private const double R51_KAELTE_MWH = 4.05051694;
        private const double R51_STROM_MWH = 0.739744926;

        private static object Skalar(string sql, params object[] w)
            => DataRepository.ExecuteScalar(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static long Zahl(string sql, params object[] w) => Convert.ToInt64(Skalar(sql, w), CultureInfo.InvariantCulture);

        private static DataTable Tabelle(string sql, params object[] w)
            => DataRepository.GetDataTable(sql, w.Select((x, i) => new DbParam("@p" + i, x)).ToArray());

        private static DataRow Zeile(string sql, params object[] w)
        {
            DataTable t = Tabelle(sql, w);
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static string T(object o) => o == DBNull.Value ? "<leer>" : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Die Anlagenzeile der Wärmepumpe im Kühlbetrieb eines Projekts.</summary>
        private static DataRow AnlageWp(int projekt)
            => Zeile("SELECT a.* FROM Tab_Energieanlagen a JOIN Tab_WP w ON w.ID = a.ID_WP WHERE a.ID_Projekt = ? AND w.Kuehlbetrieb = 1", projekt);

        // =====================================================================
        //  Die Saat
        // =====================================================================

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_im_Skript()
        {
            if (!_db.Vorhanden) return;
            DataRow p = Zeile("SELECT Projektname, Beschreibung, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", PROJEKT);
            Assert.Equal(NAME, T(p["Projektname"]));
            Assert.StartsWith("Referenzprojekt Freie Kühlung: Kopie von Projekt 1017", T(p["Beschreibung"]), StringComparison.Ordinal);
            Assert.Equal(DBNull.Value, p["Kosten_Geaendert"]);

            DataRow a = AnlageWp(PROJEKT);
            Assert.Equal(1L, Convert.ToInt64(a["Kuehl_Frei"], CultureInfo.InvariantCulture));
            Assert.Equal(GRAEDIGKEIT_K, Convert.ToDouble(a["Kuehl_Frei_Graedigkeit_K"], CultureInfo.InvariantCulture), 12);
            Assert.Equal(LEISTUNG_KW, Convert.ToDouble(a["Kuehl_Frei_Leistung_kW"], CultureInfo.InvariantCulture), 12);
            // Die Quelle, die die freie Kühlung trägt (KU3-6, F2): Erdreich, Sonde.
            Assert.Equal(DbWerte.WQ_TYP_ERDREICH, T(a["WQ_Typ"]));
            Assert.Equal("Sonde", T(a["WQ_Quellsystem"]));
            Assert.True(SimulationControl.FreieKuehlungSoleMoeglich(
                T(Skalar("SELECT Typ FROM Tab_WP WHERE ID = ?", a["ID_WP"])), T(a["WQ_Typ"])));
        }

        [Fact]
        public void Alle_uebrigen_Zellen_stehen_wie_in_1017()
        {
            if (!_db.Vorhanden) return;
            string[] gesaet = { "Kuehl_Frei", "Kuehl_Frei_Graedigkeit_K", "Kuehl_Frei_Leistung_kW" };
            DataRow a = AnlageWp(PROJEKT), aV = AnlageWp(VORLAGE);
            // Fachzellen: ohne die Kennungen und die Verweise auf kopierte Zeilen (alle Spalten mit „ID").
            foreach (DataColumn c in a.Table.Columns)
            {
                string s = c.ColumnName;
                if (s.IndexOf("ID", StringComparison.Ordinal) >= 0 || gesaet.Contains(s, StringComparer.OrdinalIgnoreCase)) continue;
                Assert.True(T(aV[s]) == T(a[s]), "Tab_Energieanlagen." + s + ": " + T(a[s]) + " statt " + T(aV[s]));
            }
            // 1017 rechnet ohne freie Kühlung - die Zellen der Vorlage stehen auf dem Bestand.
            Assert.Equal(0L, Convert.ToInt64(aV["Kuehl_Frei"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, aV["Kuehl_Frei_Graedigkeit_K"]);
            Assert.Equal(DBNull.Value, aV["Kuehl_Frei_Leistung_kW"]);

            // Wärmepumpe (Bauart, Kühlbetrieb, Kühlvorlauf) und Kühlkennlinie wie in 1017.
            string wp = "SELECT w.Typ || '|' || w.Kuehlbetrieb || '|' || w.Kuehl_Vorlauf FROM Tab_WP w WHERE w.ID = ?";
            Assert.Equal(T(Skalar(wp, aV["ID_WP"])), T(Skalar(wp, a["ID_WP"])));
            Assert.Equal("Sole-Wasser|1|18", T(Skalar(wp, a["ID_WP"])).Replace(".0", ""));
            string kennlinie = "SELECT Vorlauf || '|' || Temperatur || '|' || COP || '|' || Pkuehl FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ? ORDER BY ID";
            List<string> Punkte(object id) => Tabelle(kennlinie, id).Rows.Cast<DataRow>().Select(r => T(r[0])).ToList();
            Assert.Equal(10, Punkte(a["ID_WP"]).Count);
            Assert.Equal(Punkte(aV["ID_WP"]), Punkte(a["ID_WP"]));

            // Gebäude mit Kühlung, Kaskade und Kühlbetrieb wie in 1017.
            string geb = "SELECT Gebaeudename || '|' || Kuehlung_Aktiv || '|' || Kuehl_Sollwert || '|' || Kuehlleistung_Max FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            Assert.Equal(T(Skalar(geb, VORLAGE)), T(Skalar(geb, PROJEKT)));
            string kaskade = "SELECT Tool_1 || '|' || Tool_2 || '|' || Tool_3 || '|' || Tool_4 || '|' || Tool_5 || '|' || Tool_6 || '|' || Kuehlbetrieb " +
                             "|| '|' || IFNULL(Anlagenkopplung, '') FROM Tab_Einstellungen WHERE ID_Projekt = ?";
            Assert.Equal(T(Skalar(kaskade, VORLAGE)), T(Skalar(kaskade, PROJEKT)));
            string anlagen = "SELECT Bezeichner || '|' || ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY Bezeichner, ID_Type";
            List<string> Liste(int projekt) => Tabelle(anlagen, projekt).Rows.Cast<DataRow>().Select(r => T(r[0])).ToList();
            Assert.Equal(Liste(VORLAGE), Liste(PROJEKT));
        }

        [Fact]
        public void Das_Projekt_ist_das_einzige_mit_freier_Kuehlung()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_Frei = 1"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_Frei = 1 AND ID_Projekt = ?", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE (Kuehl_Frei_Graedigkeit_K IS NOT NULL OR Kuehl_Frei_Leistung_kW IS NOT NULL) " +
                                  "AND ID_Projekt <> ?", PROJEKT));
            // 1064 ist das jüngste Projekt der Testdatenbank.
            Assert.Equal((long)PROJEKT, Zahl("SELECT MAX(ID) FROM Tab_Projekt"));
        }

        // =====================================================================
        //  Der Lauf
        // =====================================================================

        [Fact]
        public void Ein_Lauf_kuehlt_frei_bis_zur_Leistungsgrenze_mit_weniger_Strom_als_1017()
        {
            if (!_db.Vorhanden) return;
            (Kaelteerzeuger e, Kaeltekaskade kaskade, int kopf) Lauf(int projekt)
            {
                SimulationProtokoll.NeuStarten();
                var lauf = new SimulationRunner();
                int k = lauf.SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(k > 0, "Lauf " + projekt + " gescheitert: " + fehler);
                Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe Kälte", StringComparison.Ordinal));
                Kaeltekaskade kaskade = lauf.simulation_Kaeltebedarf.Kaskade;
                Assert.NotNull(kaskade);
                Assert.True(kaskade.RestGesamtKwh <= 1e-3 * kaskade.BedarfGesamtKwh, "Unterdeckung in " + projekt);
                return (Assert.Single(kaskade.Erzeuger), kaskade, k);
            }

            var (e, kaskade, kopf) = Lauf(PROJEKT);
            Assert.Null(e.Maschine);
            Assert.True(e.FreieKuehlungSole, "freie Kühlung nicht wirksam");
            Assert.Equal(GRAEDIGKEIT_K, e.FreieKuehlungGraedigkeitK, 12);
            Assert.NotNull(e.FreieKuehlungLeistungKw);
            Assert.Equal(LEISTUNG_KW, e.FreieKuehlungLeistungKw.Value, 12);

            // Jahreswerte gegen die Basis R51 - mit Toleranz, nie über die Stellenzahl.
            Assert.InRange(e.StundenFreieKuehlung, R51_FREI_STUNDEN - 2, R51_FREI_STUNDEN + 2);
            Assert.InRange(e.KaelteFreiKwh / 1000.0, R51_FREI_MWH * (1 - 1e-4) - 1e-6, R51_FREI_MWH * (1 + 1e-4) + 1e-6);
            Assert.InRange(e.KaelteGesamtKwh / 1000.0, R51_KAELTE_MWH * (1 - 1e-4), R51_KAELTE_MWH * (1 + 1e-4));
            Assert.InRange(e.StromGesamtKwh / 1000.0, R51_STROM_MWH * (1 - 1e-4), R51_STROM_MWH * (1 + 1e-4));
            Assert.Equal(e.StundenFreieKuehlung, kaskade.StundenFreieKuehlungWp);

            // Die Leistungsgrenze hält in jeder Stunde und wird in einem Teil der Stunden erreicht;
            // der Verdichter deckt den Rest des Jahres und der Grenzstunden.
            double[] frei = kaskade.FreieKuehlungWp_stuendlich;
            Assert.All(frei, v => Assert.True(v <= LEISTUNG_KW + 1e-9, "freie Kühlung " + v + " kW über der Grenze"));
            int anGrenze = frei.Count(v => v >= LEISTUNG_KW - 1e-9);
            Assert.True(anGrenze > 0, "die Leistungsgrenze greift nie");
            Assert.True(anGrenze < e.StundenFreieKuehlung, "die Grenze greift in jeder freien Stunde");
            Assert.Equal(e.KaelteFreiKwh, frei.Sum(), 6);
            Assert.True(e.KaelteGesamtKwh > e.KaelteFreiKwh + 1000.0, "der Verdichter deckt keinen Rest");

            // Ergebnis und Kennzahlen.
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.NotNull(erg.Waermepumpe.FreieKuehlung_MWh);
            Assert.InRange(erg.Waermepumpe.FreieKuehlung_MWh.Value, e.KaelteFreiKwh / 1000.0 - 0.006, e.KaelteFreiKwh / 1000.0 + 0.006);
            Assert.Equal(e.StundenFreieKuehlung, erg.Waermepumpe.FreieKuehlung_Stunden);
            double? kennzahl = KennzahlenKatalog.Alle().Single(k => k.Schluessel == KennzahlenKatalog.SCHLUESSEL_WP_FREI)
                                                .Wert(new VariantenDaten { Ergebnis = erg });
            Assert.Equal(erg.Waermepumpe.FreieKuehlung_MWh, kennzahl);

            // 1017: dieselbe Kälte ohne freie Kühlung, mehr Strom.
            var (eV, _, _) = Lauf(VORLAGE);
            Assert.False(eV.FreieKuehlungSole);
            Assert.Equal(0, eV.StundenFreieKuehlung);
            Assert.Equal(eV.KaelteGesamtKwh, e.KaelteGesamtKwh, 3);
            Assert.True(e.StromGesamtKwh < eV.StromGesamtKwh, "freie Kühlung spart keinen Strom");
            ErgebnisModel ergV = new ErgebnisCtrl().Load(VORLAGE);
            Assert.Null(ergV.Waermepumpe.FreieKuehlung_MWh);

            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1064: frei {0} h ({1} h an der Grenze {2:F1} kW), {3:F6} MWh von {4:F6} MWh Kälte; Kältestrom {5:F6} MWh; " +
                "1017: Kältestrom {6:F6} MWh",
                e.StundenFreieKuehlung, anGrenze, LEISTUNG_KW, e.KaelteFreiKwh / 1000.0, e.KaelteGesamtKwh / 1000.0,
                e.StromGesamtKwh / 1000.0, eV.StromGesamtKwh / 1000.0));
        }
    }
}
