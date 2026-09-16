using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Auftrag Kostenbereich, Punkt 3: Die Betriebskosten gehoeren der ANLAGE, nicht dem
    /// Gewerk.
    ///
    /// <para><b>Der Befund.</b> <c>BetriebskostenCtrl.Lies</c> und
    /// <c>…Speichere</c> arbeiteten ueber (Projekt, Kategorie, Komponente, StammID) —
    /// OHNE <c>Tab_ProjektWerte.ID_Anlage</c>. Fuehrt ein Projekt mehrere Anlagen
    /// desselben Gewerks, fand diese Suche stets <c>MIN(ID)</c>, also die Zeile der
    /// ERSTEN Anlage: Der Dialog zeigte deren Zahlen fuer jede weitere Anlage, und das
    /// Speichern schrieb sie dorthin zurueck. Fuer die INVESTITION ist derselbe Fehler
    /// seit Ä25 behoben (<c>KostenPositionCtrl.SetzeBetrag</c> mit Anlagenbezug).</para>
    ///
    /// <para><b>Die Messlatte.</b> Projekt 1030 „Referenz BHKW-Kaskade" fuehrt ZWEI
    /// BHKW-Anlagen (14920, 14921) und zu beiden je drei Betriebspositionen der
    /// Komponente BHKW — der Regelfall, an dem der Fehler sichtbar wird.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenAnlagenbezugTests
    {
        private const int PROJEKT = 1030;
        private const int ERSTE = 14920;
        private const int ZWEITE = 14921;

        /// <summary>Die Anlage, an der eine Projektposition haengt; 0 = keine.</summary>
        private static int AnlageDer(int positionsId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID_Anlage FROM Tab_ProjektWerte WHERE ID = ?",
                new DbParam("@id", positionsId));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Alle Betriebspositionen der Komponente BHKW einer Anlage: Id → Betrag.</summary>
        private static Dictionary<int, double> BetraegeDer(int idAnlage)
        {
            var werte = new Dictionary<int, double>();
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, EingegebenerWert FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KomponentenID = ? AND KategorieID = ? AND ID_Anlage = ?",
                new DbParam("@p", PROJEKT),
                new DbParam("@k", BetriebskostenCtrl.KOMPONENTE_BHKW),
                new DbParam("@g", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@a", idAnlage));
            if (dt == null) return werte;
            foreach (System.Data.DataRow r in dt.Rows)
                werte[Convert.ToInt32(r["ID"])] =
                    r["EingegebenerWert"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["EingegebenerWert"]);
            return werte;
        }

        private static List<int> ErfassteIds(List<BetriebskostenCtrl.Zeile> zeilen)
        {
            return zeilen.Where(z => z.Id > 0).Select(z => z.Id).OrderBy(i => i).ToList();
        }

        /// <summary>
        /// Die LESESEITE trennt die beiden Anlagen — jede bekommt ihre eigenen Zeilen.
        /// Die anlagenblinde Bestandssignatur liefert dagegen fuer beide dieselbe Menge:
        /// die der ERSTEN Anlage. Genau das ist der Befund.
        /// </summary>
        [Fact]
        public void Die_Leseseite_findet_die_Positionen_der_gemeinten_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> erste = ErfassteIds(BetriebskostenCtrl.Lies(PROJEKT, null, ERSTE));
            List<int> zweite = ErfassteIds(BetriebskostenCtrl.Lies(PROJEKT, null, ZWEITE));

            Assert.NotEmpty(erste);
            Assert.NotEmpty(zweite);
            Assert.Empty(erste.Intersect(zweite));

            foreach (int id in erste) Assert.Equal(ERSTE, AnlageDer(id));
            foreach (int id in zweite) Assert.Equal(ZWEITE, AnlageDer(id));

            // Die Gegenprobe: ohne Anlagenbezug kommt die Menge der ERSTEN Anlage.
            Assert.Equal(erste, ErfassteIds(BetriebskostenCtrl.Lies(PROJEKT, null)));
        }

        /// <summary>
        /// Die SCHREIBSEITE legt die fehlende Position bei der GEMEINTEN Anlage an — die
        /// Zeilen der ersten Anlage bleiben Wert fuer Wert, wie sie waren.
        ///
        /// <para>Vor der Berichtigung lief der Weg ueber die anlagenblinde Ueberladung
        /// von <c>KostenPositionCtrl.SetzeBetrag</c>: Sie fand die gleichnamige Zeile der
        /// ERSTEN Anlage und ueberschrieb deren Betrag — die Betriebskosten der ersten
        /// Anlage waren weg, und die zweite bekam gar keine eigene Zeile.</para>
        /// </summary>
        [Fact]
        public void Eine_neue_Betriebsposition_der_zweiten_Anlage_laesst_die_erste_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Ausgangslage: die ZWEITE Anlage fuehrt keine Betriebsposition mehr, die
            // erste ihre eigenen - der Zustand, in dem der Anwender sie neu erfasst.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = ? " +
                "AND KategorieID = ? AND ID_Anlage = ?",
                new DbParam("@p", PROJEKT),
                new DbParam("@k", BetriebskostenCtrl.KOMPONENTE_BHKW),
                new DbParam("@g", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@a", ZWEITE));

            Dictionary<int, double> vorher = BetraegeDer(ERSTE);
            Assert.NotEmpty(vorher);
            Assert.Empty(BetraegeDer(ZWEITE));

            List<BetriebskostenCtrl.Zeile> zeilen = BetriebskostenCtrl.Lies(PROJEKT, null, ZWEITE);
            Assert.All(zeilen, z => Assert.Equal(0, z.Id));      // nichts erfasst

            const double BETRAG = 1234.50;
            foreach (BetriebskostenCtrl.Zeile z in zeilen)
            {
                z.Bemessung = DbWerte.BEMESSUNG_BETRAG;
                z.Fest = BETRAG;
                z.Satz = null;
                z.Menge = null;
            }
            Assert.True(BetriebskostenCtrl.Speichere(PROJEKT, zeilen, ZWEITE) > 0);

            // Die zweite Anlage hat jetzt eigene Zeilen - mit dem erfassten Betrag.
            Dictionary<int, double> nachher = BetraegeDer(ZWEITE);
            Assert.NotEmpty(nachher);
            foreach (double w in nachher.Values) Assert.Equal(BETRAG, w, 6);

            // Und die erste Anlage steht unveraendert da: dieselben Zeilen, dieselben Werte.
            Dictionary<int, double> ersteNachher = BetraegeDer(ERSTE);
            Assert.Equal(vorher.Count, ersteNachher.Count);
            foreach (KeyValuePair<int, double> kv in vorher)
            {
                Assert.True(ersteNachher.ContainsKey(kv.Key),
                            "Zeile " + kv.Key + " der ersten Anlage ist verschwunden.");
                Assert.Equal(kv.Value, ersteNachher[kv.Key], 6);
            }
        }
    }
}
