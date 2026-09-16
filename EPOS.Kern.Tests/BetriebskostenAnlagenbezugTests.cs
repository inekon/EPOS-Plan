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
    /// <para><b>Der Befund.</b> Wer eine Betriebsposition ueber (Projekt, Kategorie,
    /// Komponente, StammID) sucht — OHNE <c>Tab_ProjektWerte.ID_Anlage</c> —, findet bei
    /// mehreren Anlagen desselben Gewerks stets die Zeile der ERSTEN Anlage: Die Maske
    /// zeigte deren Zahlen fuer jede weitere Anlage, und das Speichern schrieb sie
    /// dorthin zurueck. Fuer die INVESTITION ist derselbe Fehler seit Ä25 behoben
    /// (<c>KostenPositionCtrl.SetzeBetrag</c> mit Anlagenbezug).</para>
    ///
    /// <para><b>Geprueft wird der LEBENDE Weg.</b> Betriebspositionen liest und legt
    /// heute die Kostenseite an: <c>KostenProjektPositionenCtrl.Lies(…, idAnlage)</c> und
    /// <c>KostenProjektPositionenCtrl.Neu(…, idAnlage)</c>, darunter
    /// <c>KostenPositionCtrl.FindePosition</c> bzw. <c>SetzeBetrag</c> — beide mit
    /// Anlagenbezug. Die gleichnamigen Glieder des alten WinForms-Betriebskostendialogs
    /// (<c>BetriebskostenCtrl.Lies</c>/<c>…Speichere</c> samt ihren Zeilen- und
    /// Bezugsgroessentypen) sind mit dem Dialog gefallen; ein Test auf sie haette nur
    /// noch sich selbst geprueft.</para>
    ///
    /// <para><b>Die Messlatte.</b> Projekt 1030 „Referenz BHKW-Kaskade" fuehrt ZWEI
    /// BHKW-Anlagen (14920, 14921) und zu beiden Betriebspositionen der Komponente BHKW —
    /// der Regelfall, an dem der Fehler sichtbar wird.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenAnlagenbezugTests
    {
        private const int PROJEKT = 1030;
        private const int ERSTE = 14920;
        private const int ZWEITE = 14921;

        private const int KATEGORIE = DbWerte.KOSTEN_KATEGORIE_BETRIEB;
        private const int KOMPONENTE = BetriebskostenCtrl.KOMPONENTE_BHKW;

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
                new DbParam("@k", KOMPONENTE),
                new DbParam("@g", KATEGORIE),
                new DbParam("@a", idAnlage));
            if (dt == null) return werte;
            foreach (System.Data.DataRow r in dt.Rows)
                werte[Convert.ToInt32(r["ID"])] =
                    r["EingegebenerWert"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["EingegebenerWert"]);
            return werte;
        }

        /// <summary>Die Zeilen, die die Kostenseite fuer EINE Anlage liest.</summary>
        private static List<KostenProjektPositionenCtrl.Zeile> ZeilenDer(int idAnlage)
        {
            return KostenProjektPositionenCtrl.Lies(PROJEKT, KOMPONENTE, KATEGORIE, idAnlage);
        }

        private static List<int> IdsDer(int idAnlage)
        {
            return ZeilenDer(idAnlage).Select(z => z.Raster.Id).OrderBy(i => i).ToList();
        }

        /// <summary>
        /// Die LESESEITE trennt die beiden Anlagen — jede bekommt ihre eigenen Zeilen,
        /// und keine Zeile taucht bei beiden auf. Genau das ist der Befund: Ohne
        /// Anlagenbezug gaebe es nur EINE Menge fuer beide.
        /// </summary>
        [Fact]
        public void Die_Leseseite_findet_die_Positionen_der_gemeinten_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<int> erste = IdsDer(ERSTE);
            List<int> zweite = IdsDer(ZWEITE);

            Assert.NotEmpty(erste);
            Assert.NotEmpty(zweite);
            Assert.Empty(erste.Intersect(zweite));

            foreach (int id in erste) Assert.Equal(ERSTE, AnlageDer(id));
            foreach (int id in zweite) Assert.Equal(ZWEITE, AnlageDer(id));

            // Die Gegenprobe: OHNE Anlagenfilter (Bestandssignatur, -1) steht die Menge
            // BEIDER Anlagen da - der Rechen- und Smokeweg sieht unveraendert alles.
            List<int> alle = KostenProjektPositionenCtrl
                .Lies(PROJEKT, KOMPONENTE, KATEGORIE)
                .Select(z => z.Raster.Id).ToList();
            foreach (int id in erste) Assert.Contains(id, alle);
            foreach (int id in zweite) Assert.Contains(id, alle);
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
                new DbParam("@k", KOMPONENTE),
                new DbParam("@g", KATEGORIE),
                new DbParam("@a", ZWEITE));

            Dictionary<int, double> vorher = BetraegeDer(ERSTE);
            Assert.NotEmpty(vorher);
            Assert.Empty(BetraegeDer(ZWEITE));
            Assert.Empty(IdsDer(ZWEITE));

            // Anlegen auf dem Weg der Kostenseite - MIT Anlagenbezug.
            int id = KostenProjektPositionenCtrl.Neu(
                PROJEKT, KOMPONENTE, KATEGORIE,
                DbWerte.VDI_POS_WARTUNG_BHKW, DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                DbWerte.BEMESSUNG_BETRAG, ZWEITE);

            Assert.True(id > 0, "Die Position der zweiten Anlage wurde nicht angelegt.");
            Assert.Equal(ZWEITE, AnlageDer(id));
            Assert.Equal(new[] { id }, IdsDer(ZWEITE));

            // Und sie nimmt ihren eigenen Betrag an - ueber denselben Speicherweg.
            const double BETRAG = 1234.50;
            KostenProjektPositionenCtrl.Zeile neu = ZeilenDer(ZWEITE).Single();
            neu.Raster.Bemessung = DbWerte.BEMESSUNG_BETRAG;
            neu.Raster.Satz = BETRAG;
            Assert.True(KostenProjektPositionenCtrl.Speichern(neu));

            Dictionary<int, double> nachher = BetraegeDer(ZWEITE);
            Assert.Equal(BETRAG, Assert.Single(nachher).Value, 6);

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
