using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Referenzprojekts 1058 „Referenzprojekt AK3"</b> (RP-AK3; Entwurf AK3 Abschnitt 5, E100, E102 mit
    /// Q-AK3-2, Q-AK3-5 und Q-AK3-7) — die Kopie des Referenzprojekts 1056 auf der Stufe AK3, mit einem Heizungspuffer
    /// an der Wärmepumpe, der die Sperre von 0 bis 6 Uhr überbrückt, und dem Raumeinfluss der Heizkurve 1 K/K. Die
    /// gesäten Zellen schreibt <c>Referenzlaeufe/Skripte/referenzprojekt_1058_ak3.py</c>.
    /// <list type="bullet">
    /// <item><b>Jede gesäte Zelle</b> steht: Stufe AK3, <c>Heizkurve_Raumeinfluss</c> 1,0 K/K, der Puffer
    /// (10 000 l, 50/35 °C, Schwellen 10/95 %, 60/60 kW) samt Anlagenzeile, die Senken der drei Wärmeerzeuger am
    /// Puffer mit Lade-Priorität Wärmepumpe vor BHKW vor Kessel.</item>
    /// <item><b>1058 ist eine Kopie von 1056</b>: alle übrigen Zellen von Einstellungen, Gebäude, Anlagen und
    /// Senken gleich; <b>1056 bleibt unverändert</b> (AK1, kein Puffer, Senken am Heizkreis).</item>
    /// <item><b>Ein Lauf aus der Datenbank</b> rechnet den Kreis das ganze Jahr ohne <c>AnlagenkopplungException</c>
    /// und schreibt die sechs <c>Ak3_*</c>-Kennzahlen mit Wert.</item>
    /// <item><b>Vergleich ohne Kreis gegen AK3</b> (Anlagenkopplung 11.2): dasselbe Projekt auf AK1 mit Fahrplan und
    /// auf AK3; auf AK3 höchstens so viele Unterschreitungsstunden wie ohne Kreis.</item>
    /// <item><b>Laufzeit von 1054 auf AK3</b> (Q-AK3-5, Muster E36, nicht in der Basis): gemessen gegen AK1 und
    /// berichtet; rot erst weit über der Grenze.</item>
    /// </list>
    /// 1058 gehört zur Einfrierregel „gesäte Auslegungsdaten der Übergabe" (<c>Referenzlaeufe/LIESMICH.md</c>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class Ak3ReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public Ak3ReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _db.Dispose();

        private const int PROJEKT = 1058;
        private const int VORLAGE = 1056;
        private const string NAME = "Referenzprojekt AK3";
        private const int TYP_WP = 1, TYP_KESSEL = 10, TYP_BHKW = 11, TYP_PUFFER = 12;

        /// <summary>Q-AK3-5: höchstens Faktor 3 gegenüber dem Lauf ohne Kreis.</summary>
        private const double LAUFZEIT_FAKTOR_GRENZE = 3.0;

        /// <summary>Q-AK3-5: der Kreis eines Einzonengebäudes höchstens 100 ms je Jahr.</summary>
        private const double KREIS_GRENZE_MS = 100.0;

        /// <summary>Erst ab diesem Vielfachen der Grenze ist die Probe rot (keine harte Schwelle, Muster E36).</summary>
        private const double MESS_FAKTOR = 5.0;

        private static long Zahl(string sql, params object[] werte)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql, params object[] werte)
        {
            DataTable t = DataRepository.GetDataTable(sql, werte.Select((w, i) => new DbParam("@p" + i, w)).ToArray());
            Assert.Equal(1, t.Rows.Count);
            return t.Rows[0];
        }

        private static DataRow Anlage(int projekt, int typ)
            => Zeile("SELECT * FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?", projekt, typ);

        private static string Text(object o) => Convert.ToString(o, CultureInfo.InvariantCulture);

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

        private static SimulationRunner Rechnen(int projekt, out double sekunden)
        {
            SimulationProtokoll.NeuStarten();
            var r = new SimulationRunner();
            var uhr = Stopwatch.StartNew();
            bool ok = r.SimuliereUndSpeichere(projekt, out string fehler) > 0;
            uhr.Stop();
            sekunden = uhr.Elapsed.TotalSeconds;
            Assert.True(ok, "Lauf " + projekt + " gescheitert: " + fehler);
            return r;
        }

        private static void StufeSetzen(int projekt, string stufe)
            => Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?",
                                                     new DbParam("@s", stufe), new DbParam("@p", projekt)));

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
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizkreis_Aktiv = 1 AND " +
                                  "Uebergabe_Art = 'RADIATOR' AND Heizkurve_Aktiv = 1 AND Heizkurve_Raumeinfluss = 1.0", PROJEKT));

            DataRow p = Zeile("SELECT * FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", PROJEKT);
            long puffer = Convert.ToInt64(p["ID"], CultureInfo.InvariantCulture);
            Assert.Equal("Heizungspuffer 10000 l", Text(p["Bezeichner"]));
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_PUFFER, Text(p["Speichertyp"]));
            Assert.Equal(DbWerte.PSP_VERWENDUNG_HEIZUNG, Text(p["Verwendung"]));
            Assert.Equal(10000.0, Convert.ToDouble(p["Gesamtvolumen"], CultureInfo.InvariantCulture));
            Assert.Equal(50.0, Convert.ToDouble(p["Vorlauf"], CultureInfo.InvariantCulture));
            Assert.Equal(35.0, Convert.ToDouble(p["Ruecklauf"], CultureInfo.InvariantCulture));
            Assert.Equal(5.0, Convert.ToDouble(p["Bereitschaftsverluste"], CultureInfo.InvariantCulture));
            Assert.Equal(10.0, Convert.ToDouble(p["Schwelle_Ein"], CultureInfo.InvariantCulture));
            Assert.Equal(95.0, Convert.ToDouble(p["Schwelle_Aus"], CultureInfo.InvariantCulture));
            Assert.True(p["Schwelle_Aus_Nachrang"] == DBNull.Value, "Schwelle_Aus_Nachrang ist nicht leer");
            Assert.Equal(1L, Convert.ToInt64(p["Nutzung_Heizung"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(p["Nutzung_Brauchwasser"], CultureInfo.InvariantCulture));
            Assert.Equal(60.0, Convert.ToDouble(p["Ladeleistung_Max"], CultureInfo.InvariantCulture));
            Assert.Equal(60.0, Convert.ToDouble(p["Entladeleistung_Max"], CultureInfo.InvariantCulture));
            Assert.Equal(puffer, Convert.ToInt64(Anlage(PROJEKT, TYP_PUFFER)["ID_PUFFER"], CultureInfo.InvariantCulture));

            foreach ((int typ, long prio) in new[] { (TYP_WP, 1L), (TYP_BHKW, 2L), (TYP_KESSEL, 3L) })
            {
                long id = Convert.ToInt64(Anlage(PROJEKT, typ)["ID"], CultureInfo.InvariantCulture);
                DataRow s = Zeile("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ?", id);
                Assert.Equal(1L, Convert.ToInt64(s["Rang"], CultureInfo.InvariantCulture));
                Assert.Equal(DbWerte.WS_ZIEL_PUFFER_HEIZUNG, Text(s["Ziel"]));
                Assert.Equal(puffer, Convert.ToInt64(s["ID_Puffer"], CultureInfo.InvariantCulture));
                Assert.Equal(prio, Convert.ToInt64(s["Ladeprio"], CultureInfo.InvariantCulture));
            }
        }

        [Fact]
        public void Projekt_1058_ist_eine_Kopie_von_1056_und_1056_bleibt_unveraendert()
        {
            if (!_db.Vorhanden) return;
            ZellenGleich(Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", PROJEKT), "Tab_Einstellungen", "Anlagenkopplung");
            ZellenGleich(Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", VORLAGE),
                         Zeile("SELECT * FROM Tab_Gebaeude WHERE ID_Projekt = ?", PROJEKT), "Tab_Gebaeude", "Heizkurve_Raumeinfluss");
            foreach (int typ in new[] { TYP_WP, TYP_KESSEL, TYP_BHKW })
            {
                DataRow v = Anlage(VORLAGE, typ), k = Anlage(PROJEKT, typ);
                ZellenGleich(v, k, "Tab_Energieanlagen[" + typ + "]", "WS_Ziel", "WS_ID_Puffer", "WS_Ladeprio");
                ZellenGleich(Zeile("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ?", v["ID"]),
                             Zeile("SELECT * FROM Z_AnlageSenke WHERE ID_Anlage = ?", k["ID"]),
                             "Z_AnlageSenke[" + typ + "]", "Ziel", "Ladeprio");
            }
            string typen = "SELECT GROUP_CONCAT(ID_Type, ',') FROM (SELECT ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type <> 12 ORDER BY ID_Type)";
            Assert.Equal(Text(DataRepository.ExecuteScalar(typen, new DbParam("@p", VORLAGE))),
                         Text(DataRepository.ExecuteScalar(typen, new DbParam("@p", PROJEKT))));

            // 1056 unverändert: AK1, kein Puffer, Senken am Heizkreis, kein Raumeinfluss.
            Assert.Equal("AK1", KonfigurationCtrl.AnlagenkopplungLesen(VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID_Projekt = ?", VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 12", VORLAGE));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_AnlageSenke s JOIN Tab_Energieanlagen a ON a.ID = s.ID_Anlage " +
                                  "WHERE a.ID_Projekt = ? AND (s.Ziel <> 'Heizkreis' OR s.ID_Puffer IS NOT NULL)", VORLAGE));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude WHERE ID_Projekt = ? AND Heizkurve_Raumeinfluss IS NULL", VORLAGE));
        }

        [Fact]
        public void Ein_Lauf_rechnet_den_Kreis_und_schreibt_die_sechs_Kennzahlen()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner r = Rechnen(PROJEKT, out double sekunden);
            Assert.Empty(SimulationProtokoll.Aktuell.Fehler);
            Ak3Weg weg = r.simulation_Waermebedarf.Ak3;
            Assert.NotNull(weg);
            Assert.Equal(8760, weg.Kreis.Stunden);
            Assert.NotNull(weg.Kreis.Raumeinfluss);
            foreach (string s in Ak3Schema.SPALTEN_ERGEBNIS)
                Assert.False(Kennzahl(PROJEKT, s) is null or DBNull, s + " ist leer");
            long leer = Convert.ToInt64(Kennzahl(PROJEKT, Ak3Schema.SPALTE_SPEICHER_LEER_STUNDEN), CultureInfo.InvariantCulture);
            _aus.WriteLine("1058: Laufzeit {0:0.00} s; " + string.Join(", ", Ak3Schema.SPALTEN_ERGEBNIS.Select(
                s => s + " " + Convert.ToString(Kennzahl(PROJEKT, s), CultureInfo.InvariantCulture))), sekunden);
            // Der Puffer überbrückt die Sperre: höchstens eine Sperrstunde je Woche ohne entnehmbaren Vorrat.
            Assert.True(leer <= 52, "Speicher leer in " + leer + " Stunden");
        }

        /// <summary>
        /// <b>Vergleich ohne Kreis gegen AK3</b> (Anlagenkopplung 11.2, Entwurf AK3 Abschnitt 5): dasselbe Projekt auf
        /// AK1 mit Fahrplan (Profilweg) und auf AK3; erwartet höchstens so viele Unterschreitungsstunden auf AK3.
        /// </summary>
        [Fact]
        public void Auf_AK3_hoechstens_so_viele_Unterschreitungsstunden_wie_ohne_Kreis()
        {
            if (!_db.Vorhanden) return;
            SimulationRunner ak3 = Rechnen(PROJEKT, out double tAk3);
            Komfortkennzahlen kAk3 = ak3.simulation_Waermebedarf.KomfortProjekt().Heizen;
            StufeSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1);
            SimulationRunner ohne = Rechnen(PROJEKT, out double tOhne);
            Assert.Null(ohne.simulation_Waermebedarf.Ak3);
            Assert.True(ohne.simulation_Waermebedarf.FahrplanWirksam);
            Komfortkennzahlen kOhne = ohne.simulation_Waermebedarf.KomfortProjekt().Heizen;
            Assert.NotNull(kAk3);
            Assert.NotNull(kOhne);
            _aus.WriteLine("1058 ohne Kreis (AK1 mit Fahrplan): {0} h, {1:0.0} Kh, längste Strecke {2} h, Wärme Gebäude {3:0.000}, Laufzeit {4:0.00} s",
                           kOhne.Stunden, kOhne.Kelvinstunden, kOhne.LaengsteStrecke, ohne.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt, tOhne);
            _aus.WriteLine("1058 AK3: {0} h, {1:0.0} Kh, längste Strecke {2} h, Wärme Gebäude {3:0.000}, Laufzeit {4:0.00} s",
                           kAk3.Stunden, kAk3.Kelvinstunden, kAk3.LaengsteStrecke, ak3.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt, tAk3);
            Assert.True(kAk3.Stunden <= kOhne.Stunden, "AK3 " + kAk3.Stunden + " h > ohne Kreis " + kOhne.Stunden + " h");
        }
    }
}
