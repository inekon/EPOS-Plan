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
    /// <b>Wache des Referenzprojekts 1059 „Referenzprojekt AK3-K"</b> (RP-AK3K; Entwurf AK3-K Abschnitt 6) — die Kopie
    /// des Referenzprojekts 1058 (Stufe AK3, Heizungspuffer, Raumeinfluss, Fahrplan), deren Wärmepumpe nur heizt und
    /// deren Kälte eine bewusst kleine Kältemaschine (10 kW, Trocken-Rückkühler, eigener Zähler) samt Kaltwasserspeicher
    /// aus 1055 deckt. Die gesäten Zellen schreibt <c>Referenzlaeufe/Skripte/referenzprojekt_1059_ak3k.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht: Wärmepumpe ohne Kühlbetrieb, Kältemaschine 10 kW mit Kennlinie (die
    /// Hälfte von 1055), Anlagenzeile mit Träger 58 und eigenem Zähler, Kaltwasserspeicher 2 m³ 6/12 °C.</item>
    /// <item><b>1059 ist eine Kopie von 1058</b>; <b>1058 und 1055 bleiben unverändert</b>.</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> schreibt die sieben Kennzahlen von AK3-K (Schemaschritt S1); die
    /// Kälteschranke greift (&gt; 0 h) mit der Kältemaschine als einzigem Kälteerzeuger, ohne Umschaltstunden.</item>
    /// <item><b>Der Kälte-Restbedarf ist erklärt</b>: Die Schranke kürzt die Kühlung des Gebäudes schon im Kreis auf das,
    /// was Maschine und Speicher liefern — der Rest bleibt 0, das Defizit steht als Überschreitungsstunden im Raum.</item>
    /// </list>
    /// 1059 gehört zu den Einfrierregeln „gesäte Kältemaschinendaten" und „gesäte Auslegungsdaten der Übergabe".
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3KReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public Ak3KReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1059;
        private const int VORLAGE = 1058;
        private const int KAELTEVORLAGE = 1055;
        private const string NAME = "Referenzprojekt AK3-K";
        private const int TYP_WP = 1, TYP_KESSEL = 10, TYP_BHKW = 11, TYP_PUFFER = 12, TYP_KAELTEMASCHINE = 13;
        private const double NENN = 10.0, NENN_VORLAGE = 20.0;

        private static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static DataTable Tabelle(string sql, params object[] werte)
            => DataRepository.GetDataTable(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        private static DataRow Zeile(string sql, params object[] werte)
        {
            DataTable t = Tabelle(sql, werte);
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static DataRow Anlage(int projekt, int typ)
            => Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", projekt, typ);

        private static DataRow Puffer(int projekt, string verwendung)
            => Zeile("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Verwendung = ?", projekt, verwendung);

        private static string Text(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);
        private static double Wert(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>Alle Zellen zweier Zeilen gleich, außer Schlüsseln (<c>ID</c>, <c>ID_*</c>) und den genannten.</summary>
        private static void ZellenGleich(DataRow vorlage, DataRow kopie, string wo, params string[] ausser)
        {
            var aus = new HashSet<string>(ausser, StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn c in vorlage.Table.Columns)
            {
                string n = c.ColumnName;
                if (n.Equals("ID", StringComparison.OrdinalIgnoreCase) || n.StartsWith("ID_", StringComparison.OrdinalIgnoreCase)
                    || aus.Contains(n)) continue;
                Assert.True(Text(vorlage[n]) == Text(kopie[n]), wo + "." + n + ": " + Text(vorlage[n]) + " ≠ " + Text(kopie[n]));
            }
        }

        private static object Kennzahl(int projekt, string spalte)
            => DataRepository.ExecuteScalar("SELECT e." + spalte + " FROM Tab_ErgebnisEnergiebedarf e JOIN Tab_Ergebnis k ON k.ID = e.ID_Ergebnis " +
                                            "WHERE k.ID_Projekt = ? ORDER BY e.ID DESC LIMIT 1", new DbParam("?", projekt));

        [Fact]
        public void Jede_gesaete_Zelle_steht_wie_geplant()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(NAME, Text(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT))));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK3, KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ? AND Kosten_Geaendert IS NULL", PROJEKT));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb = 0", PROJEKT));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb <> 0", PROJEKT));

            // Die Kältemaschine: Projektkopie von 1055, auf 10 kW skaliert.
            DataRow km = Zeile("SELECT * FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", PROJEKT);
            DataRow kmV = Zeile("SELECT * FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", KAELTEVORLAGE);
            Assert.Equal("Kältemaschine 10 kW mit Trockenkühler", Text(km["Bezeichner"]));
            Assert.Equal(NENN, Wert(km["Nennkaelteleistung_kW"]));
            Assert.Equal(0.3, Wert(km["Hilfsstrom_Rueckkuehlung_kW"]), 12);
            Assert.Equal("TROCKENKUEHLER", Text(km["Rueckkuehlart"]));
            Assert.True(km["Kuehl_Vorlauf"] == DBNull.Value, "Kuehl_Vorlauf ist nicht leer");
            Assert.Equal(0.05, Wert(km["Kuehl_Hilfsstromanteil"]), 12);
            ZellenGleich(kmV, km, "Tab_Kaeltemaschine", "Bezeichner", "Nennkaelteleistung_kW", "Hilfsstrom_Rueckkuehlung_kW");
            Assert.Equal(Text(kmV["ID_Stamm"]), Text(km["ID_Stamm"]));
            DataTable p = Tabelle("SELECT * FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ? ORDER BY ID", km["ID"]);
            DataTable pV = Tabelle("SELECT * FROM Tab_Kenndaten_Kaeltemaschine WHERE ID_Kaeltemaschine = ? ORDER BY ID", kmV["ID"]);
            Assert.Equal(6, p.Rows.Count);
            Assert.Equal(6, pV.Rows.Count);
            for (int i = 0; i < 6; i++)
            {
                ZellenGleich(pV.Rows[i], p.Rows[i], "Tab_Kenndaten_Kaeltemaschine[" + i + "]", "Kaelteleistung_kW");
                Assert.Equal(Wert(pV.Rows[i]["Kaelteleistung_kW"]) * NENN / NENN_VORLAGE, Wert(p.Rows[i]["Kaelteleistung_kW"]), 9);
                Assert.Equal(PROJEKT, Convert.ToInt32(p.Rows[i]["ID_Projekt"], CultureInfo.InvariantCulture));
            }
            DataRow a = Anlage(PROJEKT, TYP_KAELTEMASCHINE);
            Assert.Equal(Text(km["ID"]), Text(a["ID_Kaeltemaschine"]));
            Assert.Equal("Kältemaschine 10 kW", Text(a["Bezeichner"]));
            Assert.Equal(1L, Convert.ToInt64(a["Kaeltemaschine_Anzahl"], CultureInfo.InvariantCulture));
            Assert.Equal(58L, Convert.ToInt64(a["Kuehl_ID_Carrier"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(a["Kuehl_EigenerZaehler"], CultureInfo.InvariantCulture));
            ZellenGleich(Anlage(KAELTEVORLAGE, TYP_KAELTEMASCHINE), a, "Tab_Energieanlagen[13]", "Bezeichner");

            // Der Kaltwasserspeicher: Zeile für Zeile wie 1055, die Anlagenzeile zeigt auf ihn.
            DataRow kalt = Puffer(PROJEKT, DbWerte.PSP_VERWENDUNG_KAELTE);
            Assert.Equal(2000.0, Wert(kalt["Gesamtvolumen"]));
            Assert.Equal(6.0, Wert(kalt["Vorlauf"]));
            Assert.Equal(12.0, Wert(kalt["Ruecklauf"]));
            ZellenGleich(Puffer(KAELTEVORLAGE, DbWerte.PSP_VERWENDUNG_KAELTE), kalt, "Tab_Pufferspeicher[Kaelte]");
            DataTable anl = Tabelle("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_PUFFER = ?", PROJEKT, kalt["ID"]);
            Assert.Equal(1, anl.Rows.Count);
            Assert.Equal(TYP_PUFFER, Convert.ToInt32(anl.Rows[0]["ID_Type"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_AnlageSenke WHERE ID_Anlage = ?", anl.Rows[0]["ID"]));
        }

        [Fact]
        public void Projekt_1059_ist_eine_Kopie_von_1058_und_1058_und_1055_bleiben_unveraendert()
        {
            if (!_db.Vorhanden) return;
            ZellenGleich(Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT), "Tab_Einstellungen");
            ZellenGleich(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT), "Tab_Gebaeude");
            ZellenGleich(Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_WP WHERE ID_Projekt = ?", PROJEKT), "Tab_WP", "Kuehlbetrieb");
            foreach (int typ in new[] { TYP_WP, TYP_KESSEL, TYP_BHKW })
            {
                DataRow v = Anlage(VORLAGE, typ), k = Anlage(PROJEKT, typ);
                ZellenGleich(v, k, "Tab_Energieanlagen[" + typ + "]", "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer");
                ZellenGleich(Zeile("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ?", v["ID"]),
                             Zeile("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ?", k["ID"]), "Z_AnlageSenke[" + typ + "]");
            }
            ZellenGleich(Puffer(VORLAGE, DbWerte.PSP_VERWENDUNG_HEIZUNG), Puffer(PROJEKT, DbWerte.PSP_VERWENDUNG_HEIZUNG),
                         "Tab_Pufferspeicher[Heizung]");
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", VORLAGE) + 2,
                         Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", PROJEKT));

            // 1058 unverändert: die Wärmepumpe kühlt, keine Kältemaschine, nur der Heizungspuffer.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ? AND Kuehlbetrieb = 1", VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ?", VORLAGE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", VORLAGE));
            // 1055 unverändert: Kältemaschine 20 kW, ein Kaltwasserspeicher.
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltemaschine WHERE ID_Projekt = ? AND Nennkaelteleistung_kW = 20", KAELTEVORLAGE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Verwendung = 'Kaelte'", KAELTEVORLAGE));
            // Nach 1059 nur die Referenzprojekte der Übergabegrenze (1060), der Kühlkurve (1061 RP-KK, 1062 RP-KKZ) und der Kältemaschinen-Teillast (1063, KM3) und der freien Kühlung (1064, FK).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Projekt WHERE ID > ? AND ID NOT IN (1060, 1061, 1062, 1063, 1064)", PROJEKT));
        }

        [Fact]
        public void Ein_Lauf_schreibt_die_Kennzahlen_und_die_Kaelteschranke_greift()
        {
            if (!_db.Vorhanden) return;
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            Assert.True(r.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, "Lauf " + PROJEKT + " gescheitert: " + fehler);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);

            Ak3Weg weg = r.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Kaelteschranke schranke = weg.Kreis.Kaelteschranke;
            Assert.NotNull(schranke);
            // Die Kältemaschine ist der einzige Kälteerzeuger der Schranke - die Wärmepumpe heizt nur.
            Assert.Single(schranke.Erzeuger);
            Assert.IsType<KaeltemaschineKapazitaet>(schranke.Erzeuger[0]);

            foreach (string s in Ak3KSchema.SPALTEN_ERGEBNIS)
                Assert.False(Kennzahl(PROJEKT, s) is null or DBNull, s + " ist leer");
            _aus.WriteLine("1059: " + string.Join(", ", Ak3KSchema.SPALTEN_ERGEBNIS.Concat(new[]
            {
                AnlagenfahrplanSchema.SPALTE_UEBERSCHREITUNGSSTUNDEN,
            }).Select(s => s + " " + Text(Kennzahl(PROJEKT, s)))));

            long schrankeStunden = Convert.ToInt64(Kennzahl(PROJEKT, Ak3KSchema.SPALTE_KAELTESCHRANKE_STUNDEN), CultureInfo.InvariantCulture);
            Assert.Equal(weg.Kreis.StundenAnDerKaelteschranke, schrankeStunden);
            Assert.True(schrankeStunden > 0, "Die Kälteschranke greift nicht.");
            Assert.True(schrankeStunden < 1000, "Die Kälteschranke greift in " + schrankeStunden + " Stunden - die Maschine ist nicht nur knapp.");
            Assert.Equal(0L, Convert.ToInt64(Kennzahl(PROJEKT, Ak3KSchema.SPALTE_UMSCHALT_STUNDEN), CultureInfo.InvariantCulture));
            Assert.True(Convert.ToInt64(Kennzahl(PROJEKT, Ak3KSchema.SPALTE_ZONENSPERRE_TAGE), CultureInfo.InvariantCulture) > 0);

            // Der Kälte-Restbedarf ist erklärt: Die Schranke kürzt die Kühlung im Kreis, der Rest bleibt 0 und das
            // Defizit steht als Überschreitungsstunden im Raum.
            Assert.Equal(0L, Convert.ToInt64(Kennzahl(PROJEKT, Ak3KSchema.SPALTE_KAELTEREST_STUNDEN), CultureInfo.InvariantCulture));
            Assert.Equal(0.0, Wert(Kennzahl(PROJEKT, Ak3KSchema.SPALTE_KAELTEREST_MWH)));
            Assert.Equal(0.0, r.sim.Ak3KaelteRestKwh);
            Assert.True(Convert.ToInt64(Kennzahl(PROJEKT, AnlagenfahrplanSchema.SPALTE_UEBERSCHREITUNGSSTUNDEN), CultureInfo.InvariantCulture) > 0,
                        "Die knappe Kältemaschine zeigt keine Überschreitungsstunden.");
        }
    }
}
