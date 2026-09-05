using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kennzahlen hinter der Ganglinien-Grafik</b> (iU9-W12, Anwenderwunsch
    /// <b>W12‑E‑2</b> der Windows-Abnahme vom 05.09.2026: „Stelle die importierte
    /// Stromganglinie als Grafik dar").
    ///
    /// <para><b>Warum es diese Faelle gibt.</b> Der Dialog „Stromganglinien" zeigt seit
    /// diesem Wunsch drei Zahlen — Jahresarbeit, Spitze, Vollbenutzungsstunden — und
    /// eine Kurve. Der Referenzlauf sieht davon nichts: Er rechnet einen bestehenden
    /// Projektstand nach und oeffnet keinen Dialog. Ohne diese Faelle waeren die
    /// Zahlen allein am Windows-Geraet nachweisbar. Sie sind hier EINGEFROREN: Wer
    /// den Leseweg oder die Verdichtung anfasst, sieht es an dieser Stelle.</para>
    ///
    /// <para><b>Die zwei Raster in einem Bild.</b> Die Testdatenbank fuehrt beide
    /// Faelle nebeneinander: <c>Lastgang_Strom_NestleLB-…</c> mit 8 760
    /// STUNDENwerten und <c>test</c> mit 35 040 VIERTELSTUNDENwerten. Der zweite ist
    /// der eigentliche Nachweis — er belegt, dass die Verdichtung greift und dass
    /// die Jahresarbeit dabei erhalten bleibt (Σ ÷ 4 der Viertelstunden = Σ der
    /// Stundenmittel), waehrend die SPITZE naturgemaess kleiner wird.</para>
    ///
    /// <para><b>Ohne Datenbank schweigen die Faelle</b> (<see cref="TestDatenbank"/>);
    /// gelesen wird nur, geschrieben nichts.</para>
    ///
    /// <para><b>Seit iU9-W9-E-3 ist der Controller verallgemeinert</b>: Der Rechenweg
    /// heisst <see cref="GanglinienAuswertungCtrl"/> und bekommt seine Tabellen als
    /// <see cref="GanglinienQuelle"/> herein — <c>Strom</c> hier, <c>Waermebedarf</c>
    /// in <see cref="WaermebedarfKatalogTests"/>. Die Zahlen dieser Faelle sind davon
    /// UNBERUEHRT geblieben; genau das ist ihr Zweck.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromganglinieAuswertungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StromganglinieAuswertungTests(TestDatenbank db) { _db = db; }

        /// <summary>Der Katalogsatz mit 8 760 STUNDENwerten (Zeitinterval 1).</summary>
        private const string STUNDENREIHE = "Lastgang_Strom_NestleLB-05-2010-05-2011";

        /// <summary>Der Katalogsatz mit 35 040 VIERTELSTUNDENwerten (Zeitinterval 4).</summary>
        private const string VIERTELSTUNDENREIHE = "test";

        // ==================================================================
        //  1 - Die Stundenreihe: die drei Zahlen, eingefroren
        // ==================================================================

        /// <summary>
        /// <c>Lastgang_Strom_NestleLB-…</c> liegt bereits im Stundenraster; die
        /// Verdichtung fasst sie nicht an. Jahresarbeit = Σ der 8 760
        /// Stundenleistungen ÷ 1 000, Spitze = ihr Hoechstwert,
        /// Vollbenutzungsstunden = Jahresarbeit [kWh] ÷ Spitze.
        /// </summary>
        [Fact]
        public void Eine_Stundenreihe_traegt_die_eingefrorenen_Kennzahlen()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, STUNDENREIHE);

            Assert.True(a.Erfolgreich);
            Assert.Equal(STUNDENREIHE, a.Bezeichner);
            Assert.Equal(GanglinienAuswertungCtrl.STUNDEN_JAHR, a.Stundenwerte.Length);

            Assert.Equal(4790.086, a.JahresarbeitMwh, 3);
            Assert.Equal(2070.0, a.SpitzeKw, 3);

            Assert.NotNull(a.VollbenutzungsstundenH);
            Assert.Equal(2314.0512, a.VollbenutzungsstundenH.Value, 3);
        }

        // ==================================================================
        //  2 - Die Viertelstundenreihe: verdichtet, Arbeit erhalten
        // ==================================================================

        /// <summary>
        /// <c>test</c> liegt mit 35 040 Werten vor und wird ueber
        /// <c>SimulationControl.Viertelstunden_zu_Stundenwerte_Mittelwert</c> auf
        /// 8 760 Stundenmittel gebracht — dieselbe Methode, die der Lauf benutzt.
        /// </summary>
        [Fact]
        public void Eine_Viertelstundenreihe_wird_auf_Stunden_verdichtet()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a =
                GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, VIERTELSTUNDENREIHE);

            Assert.True(a.Erfolgreich);
            Assert.Equal(GanglinienAuswertungCtrl.STUNDEN_JAHR, a.Stundenwerte.Length);

            Assert.Equal(4788.929, a.JahresarbeitMwh, 3);
            Assert.Equal(1310.75, a.SpitzeKw, 3);

            Assert.NotNull(a.VollbenutzungsstundenH);
            Assert.Equal(3653.5792, a.VollbenutzungsstundenH.Value, 3);
        }

        /// <summary>
        /// <b>Die Verdichtung erhaelt die Jahresarbeit</b>, nicht die Spitze: Das
        /// Stundenmittel von vier Viertelstunden ist dieselbe Energie, aber eine
        /// niedrigere Leistung als die hoechste der vier. Genau deshalb steht in der
        /// Grafik die Spitze der GEZEIGTEN Reihe — sie ist die 100 %-Linie des
        /// Bildes — und nicht die Viertelstundenspitze des Laufs (hier 1 335 kW).
        /// </summary>
        [Fact]
        public void Die_Verdichtung_erhaelt_die_Arbeit_und_glaettet_die_Spitze()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a =
                GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, VIERTELSTUNDENREIHE);
            Assert.True(a.Erfolgreich);

            // Die Summe der Viertelstundenwerte, wie der Lauf sie liest: Σ ÷ 4 000 = MWh.
            System.Data.DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM Tab_StromganglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                new DbParam("@g", DbParamTyp.Integer)
                {
                    Wert = new StromganglinieStammCtrl().GetStammId(VIERTELSTUNDENREIHE)
                });

            Assert.Equal(GanglinienAuswertungCtrl.VIERTELSTUNDEN_JAHR, dt.Rows.Count);

            double summe = 0;
            double hoechster = 0;
            foreach (System.Data.DataRow r in dt.Rows)
            {
                float w = System.Convert.ToSingle(r[0]);
                summe += w;
                if (w > hoechster) hoechster = w;
            }

            // Dieselbe Energie - der Weg ueber die Stundenmittel aendert sie nicht.
            Assert.Equal(summe / 4000.0, a.JahresarbeitMwh, 3);

            // Aber eine kleinere Leistung.
            Assert.True(a.SpitzeKw < hoechster);
        }

        // ==================================================================
        //  3 - Projektfassung und Rueckfall
        // ==================================================================

        /// <summary>
        /// Eine im Dialog eben erst zugeordnete Zeile hat noch KEINE Projektkopie
        /// (<c>GanglinieId</c> = 0). Gezeigt wird dann der Katalogsatz, aus dem die
        /// Kopie entstehen wird — dieselben Werte, dieselben Zahlen.
        /// </summary>
        [Fact]
        public void Ohne_Projektkopie_gilt_der_Katalogsatz()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung ausProjekt =
                GanglinienAuswertungCtrl.AusProjekt(GanglinienQuelle.Strom, 0, STUNDENREIHE);
            GanglinienAuswertung ausKatalog =
                GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, STUNDENREIHE);

            Assert.True(ausProjekt.Erfolgreich);
            Assert.Equal(ausKatalog.JahresarbeitMwh, ausProjekt.JahresarbeitMwh, 6);
            Assert.Equal(ausKatalog.SpitzeKw, ausProjekt.SpitzeKw, 6);
        }

        /// <summary>
        /// Die PROJEKTKOPIE traegt dieselbe Zeitreihe wie ihr Katalogsatz
        /// (<c>CopyGanglinieToProjekt</c> kopiert sie in Stamm-Reihenfolge). Gelesen
        /// wird sie ueber <c>Tab_Stromganglinie.ID</c>, nicht ueber den Bezeichner —
        /// im Projekt 1030 haengt <c>Lastgang_Strom_NestleLB-…</c> unter der Id
        /// 1008032.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_wird_ueber_ihre_Id_gelesen()
        {
            if (!_db.Vorhanden) return;

            const int ID_PROJEKTKOPIE = 1008032;

            GanglinienAuswertung kopie =
                GanglinienAuswertungCtrl.AusProjekt(GanglinienQuelle.Strom, ID_PROJEKTKOPIE, STUNDENREIHE);

            Assert.True(kopie.Erfolgreich);
            Assert.Equal(GanglinienAuswertungCtrl.STUNDEN_JAHR, kopie.Stundenwerte.Length);
            Assert.Equal(4790.086, kopie.JahresarbeitMwh, 3);
            Assert.Equal(2070.0, kopie.SpitzeKw, 3);
        }

        // ==================================================================
        //  4 - Was es nicht gibt
        // ==================================================================

        /// <summary>
        /// Ein unbekannter Bezeichner ergibt kein Ergebnis und keine Ausnahme — der
        /// Dialog laesst die Grafik dann weg.
        /// </summary>
        [Fact]
        public void Eine_unbekannte_Ganglinie_liefert_kein_Ergebnis()
        {
            if (!_db.Vorhanden) return;

            GanglinienAuswertung a =
                GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, "gibt es nicht");

            Assert.False(a.Erfolgreich);
            Assert.Empty(a.Stundenwerte);
            Assert.Null(a.VollbenutzungsstundenH);
        }

        /// <summary>Ein leerer Name fragt die Datenbank gar nicht erst.</summary>
        [Fact]
        public void Ein_leerer_Name_fragt_die_Datenbank_nicht()
        {
            GanglinienAuswertung a = GanglinienAuswertungCtrl.AusKatalog(GanglinienQuelle.Strom, "");

            Assert.False(a.Erfolgreich);
            Assert.Equal("", a.Bezeichner);
        }
    }
}
