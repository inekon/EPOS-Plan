using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E30/1 (#544, Befunde B3 und B5 der Sichtprüfung 1030, Entscheide E30‑Q8 a,
    /// Q9 a, Q10 a) — die Datenpflege der Betriebskosten an den Projekten 1030 und 1026,
    /// festgehalten als Fakt. Gepflegt wird mit
    /// <c>Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs</c>.
    /// <list type="bullet">
    ///   <item><description><b>B3:</b> Die Wartung von 1030 steht nur noch in den
    ///   Pflichtzeilen — 18.000 €/a als Jahresbetrag an „Wartung BHKW" (Anlage 14920, für
    ///   die ganze Kaskade) und 2.000 €/a an „Vollwartung / Wartung Kessel" (11334). Die
    ///   Altzeilen ohne Vorlage (101600097, 101600098) sind entfallen; eine spätere
    ///   Satzpflege an der Pflichtzeile kann die Wartung nicht mehr doppelt buchen.</description></item>
    ///   <item><description><b>B5:</b> Die Hilfsenergie-Pflichtzeilen von 1030 und 1026 tragen
    ///   „% des Endenergiebedarfs" wie ihre Vorlagen (Schemaschritt 94); ohne Satz
    ///   ergebnisneutral.</description></item>
    /// </list>
    /// <para><b>Ergebnisneutral:</b> 1030 behält 20.000,00 €/a in allen drei Szenarien, die
    /// Kapitalwert-Anker bleiben (<see cref="WirtschaftlichkeitAnkerTests"/>,
    /// <c>PvAusweisStromMatrixTests</c>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class DatenpflegeBetriebskosten1030Tests
    {
        private static DataRow Zeile(long id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ProjektID, ID_Anlage, Bemessung, EingegebenerWert, Einheitpreis, IstPflicht " +
                "FROM Tab_ProjektWerte WHERE ID = ?",
                new DbParam("@id", DbParamTyp.Integer) { Wert = id });
            return dt.Rows.Count == 1 ? dt.Rows[0] : null;
        }

        [Theory]
        [InlineData(101600588, 14920, 18000.0)]   // Wartung BHKW (Pflicht) — die Kaskade
        [InlineData(101600585, 11334, 2000.0)]    // Vollwartung / Wartung Kessel (Pflicht)
        public void Die_Wartung_steht_als_Jahresbetrag_in_der_Pflichtzeile(long id, int anlage, double betrag)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRow r = Zeile(id);
            Assert.NotNull(r);
            Assert.Equal(1030, Convert.ToInt32(r["ProjektID"]));
            Assert.Equal(anlage, Convert.ToInt32(r["ID_Anlage"]));
            Assert.Equal(DbWerte.BEMESSUNG_JAHRESBETRAG, Convert.ToString(r["Bemessung"]));
            Assert.Equal(betrag, Convert.ToDouble(r["EingegebenerWert"]), 9);
            Assert.Equal(DBNull.Value, r["Einheitpreis"]);
            Assert.Equal(1, Convert.ToInt32(r["IstPflicht"]));
        }

        [Fact]
        public void Die_Altzeilen_der_Wartung_sind_entfallen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(Zeile(101600097));
            Assert.Null(Zeile(101600098));
            // Kein fester Betrag ohne Vorlage mehr in den Betriebskosten von 1030.
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = 1030 AND KategorieID = 2 " +
                "AND VorlageID IS NULL");
            Assert.Equal(0, Convert.ToInt32(n));
        }

        [Theory]
        [InlineData(101600587, 1030)]
        [InlineData(101600590, 1030)]
        [InlineData(101600593, 1030)]
        [InlineData(101600570, 1026)]
        [InlineData(101600576, 1026)]
        public void Die_Hilfsenergiezeile_bemisst_am_Endenergiebedarf(long id, int projekt)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRow r = Zeile(id);
            Assert.NotNull(r);
            Assert.Equal(projekt, Convert.ToInt32(r["ProjektID"]));
            Assert.Equal(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, Convert.ToString(r["Bemessung"]));
            Assert.Equal(DBNull.Value, r["Einheitpreis"]);
        }

        /// <summary>Keine Hilfsenergie-Kostenzeile von 1030 und 1026 rechnet mehr mit Weg A.
        /// 1019 (kein Referenzprojekt) und die Altart von 1018 bleiben bewusst stehen
        /// (E30‑Q10 a; 1018 ist Prüfzeile von <see cref="ProjektkostenArtenTests"/>).</summary>
        [Fact]
        public void In_1030_und_1026_rechnet_keine_Hilfsenergiezeile_mit_Weg_A()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS f " +
                "ON w.StammID = f.StammID WHERE w.ProjektID IN (1030, 1026) AND w.KategorieID = 2 " +
                "AND f.Bezeichnung LIKE 'Hilfsenergiekosten%' AND w.Bemessung = ?",
                new DbParam("@b", DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));
            Assert.Equal(0, Convert.ToInt32(n));
        }

        /// <summary>Ergebnisneutral: 20.000,00 €/a im Betriebs-Topf, 0 im Endenergie-Topf,
        /// in allen drei Szenarien; die Nachweisliste führt dieselbe Summe.</summary>
        [Theory]
        [InlineData(WirtschaftlichkeitSzenario.ERWARTET)]
        [InlineData(WirtschaftlichkeitSzenario.BEST)]
        [InlineData(WirtschaftlichkeitSzenario.WORST)]
        public void Die_Betriebskosten_von_1030_bleiben_20000_Euro(string szenario)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitCtrl.BetriebsTopfe t = WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(1030, szenario);
            Assert.Null(t.Fehler);
            Assert.Equal(20000.0, t.BetriebSofort, 9);
            Assert.Equal(0.0, t.EndenergieSofort, 9);

            List<KostenPositionNachweis> p = WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(1030, szenario);
            Assert.Equal(20000.0, p.Sum(x => x.BetragJahr), 9);
            Assert.Equal(new[] { 2000.0, 18000.0 },
                         p.Where(x => x.BetragJahr != 0).Select(x => x.BetragJahr).OrderBy(x => x).ToArray());
        }
    }
}
