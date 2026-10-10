using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Auftrag KS: die KÄLTEBAHN des Anlagenschemas an den Referenzprojekten der Testdatenbank —
    /// Kältemaschine mit Rückkühler und Kältespeicher (1055, 1059 auf AK3), Wärmepumpe im
    /// Kühlbetrieb ohne Kältespeicher (1017) und ein Projekt ohne Kälte (1030), dazu die Anordnung
    /// (Kältebahn unter der Wärmebahn) und der Satz, den die Hülle an die Oberfläche gibt.
    /// Gelesen wird nur; verglichen wird gegen Ressourcenwerte derselben Kultur.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteschemaTests : IClassFixture<TestDatenbank>
    {
        private const int PROJEKT_AK3K = 1059;
        private const int KM_1059 = 23959;
        private const int KALTSPEICHER_1059 = 1071277;
        private const int HEIZPUFFER_1059 = 1071276;

        private const int PROJEKT_KM = 1055;
        private const int KM_1055 = 23776;
        private const int WP_1055 = 23726;
        private const int KALTSPEICHER_1055 = 1071273;

        private const int PROJEKT_WP_KUEHLT = 1017;
        private const int WP_1017 = 10211;

        private const int PROJEKT_OHNE_KAELTE = 1030;

        private readonly TestDatenbank _db;

        public KaelteschemaTests(TestDatenbank db) { _db = db; }

        private static string KE(int id) => SchemaModell.PRAEFIX_KAELTE_ERZEUGER + id;
        private static string KQ(int id) => SchemaModell.PRAEFIX_KAELTE_QUELLE + id;
        private static string KS(int id) => SchemaModell.PRAEFIX_KAELTE_SPEICHER + id;

        private static SchemaModell.Kante Kante(SchemaModell m, string von, string nach)
            => m.Kantenliste.SingleOrDefault(e => e.Von == von && e.Nach == nach);

        [Fact]
        public void Projekt_1059_fuehrt_Kaeltemaschine_Rueckkuehler_Kaeltespeicher_und_Kaeltekreis()
        {
            if (!_db.Vorhanden) return;
            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_AK3K, null);

            Assert.True(m.HatKaelte);

            SchemaModell.Knoten km = m.Finden(KE(KM_1059));
            Assert.NotNull(km);
            Assert.Equal(SchemaModell.Bahn.Kaelte, km.Bahn);
            Assert.Equal(SchemaModell.Knotenart.Erzeuger, km.Art);
            Assert.Contains(R.KONF_KS_KAELTEMASCHINE, km.Zeilen);
            Assert.Contains(string.Format(R.KONF_KS_ANZAHL_LEISTUNG, 1, 10.0), km.Zeilen);

            SchemaModell.Knoten rk = m.Finden(KQ(KM_1059));
            Assert.NotNull(rk);
            Assert.Equal(SchemaModell.Knotenart.Quelle, rk.Art);
            Assert.Equal(R.KM_RUECKKUEHLART_TROCKENKUEHLER, rk.Titel);

            SchemaModell.Knoten ks = m.Finden(KS(KALTSPEICHER_1059));
            Assert.NotNull(ks);
            Assert.Equal(SchemaModell.Knotenart.Speicher, ks.Art);
            Assert.Equal(SchemaModell.Bahn.Kaelte, ks.Bahn);
            Assert.Contains(R.KONF_KS_KAELTE, ks.Badges);
            Assert.Contains(string.Format(R.SIM_KARTE_TEMPERATURPAAR, 6, 12), ks.Zeilen);

            SchemaModell.Knoten kk = m.Finden(SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.NotNull(kk);
            Assert.Equal(SchemaModell.Bahn.Kaelte, kk.Bahn);
            Assert.False(kk.Warnung);
            Assert.Contains(kk.Zeilen, z => z.StartsWith(R.KONF_KS_KUEHLUEBERGABE.Split('{')[0]));

            // Kanten: Quellseite, Ladung mit Platz 1, Versorgung aus dem Speicher - keine Direktkante.
            Assert.Equal(SchemaModell.Kantenart.Quelle, Kante(m, KQ(KM_1059), KE(KM_1059)).Art);
            SchemaModell.Kante ladung = Kante(m, KE(KM_1059), KS(KALTSPEICHER_1059));
            Assert.Equal(SchemaModell.Kantenart.Ladung, ladung.Art);
            Assert.Equal(1, ladung.Prioritaet);
            Assert.Equal(SchemaModell.Kantenart.Versorgung,
                         Kante(m, KS(KALTSPEICHER_1059), SchemaModell.ABNEHMER_KAELTEKREIS).Art);
            Assert.Null(Kante(m, KE(KM_1059), SchemaModell.ABNEHMER_KAELTEKREIS));

            // Die Wärmebahn bleibt die Wärmebahn: Heizungspuffer oben, der Kältespeicher nicht.
            Assert.Equal(SchemaModell.Bahn.Waerme, m.Finden(SchemaModell.PRAEFIX_SPEICHER + HEIZPUFFER_1059).Bahn);
            Assert.Null(m.Finden(SchemaModell.PRAEFIX_SPEICHER + KALTSPEICHER_1059));

            Assert.Equal(new[] { "Kältemaschine 10 kW" }, m.KaelteKette);
            Assert.Empty(m.Pruefen());
        }

        [Fact]
        public void Projekt_1055_kuehlt_mit_der_Kaeltemaschine_und_die_Waermepumpe_heizt_nur()
        {
            if (!_db.Vorhanden) return;
            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_KM, null);

            Assert.True(m.HatKaelte);
            Assert.NotNull(m.Finden(KE(KM_1055)));
            Assert.Null(m.Finden(KE(WP_1055)));
            Assert.Equal(R.KM_RUECKKUEHLART_TROCKENKUEHLER, m.Finden(KQ(KM_1055)).Titel);
            Assert.NotNull(m.Finden(KS(KALTSPEICHER_1055)));
            Assert.Equal(1, Kante(m, KE(KM_1055), KS(KALTSPEICHER_1055)).Prioritaet);
            Assert.NotNull(Kante(m, KS(KALTSPEICHER_1055), SchemaModell.ABNEHMER_KAELTEKREIS));
            Assert.Equal(new[] { "Kältemaschine 20 kW" }, m.KaelteKette);

            // Die Wärmepumpe steht nur in der Wärmebahn.
            Assert.Equal(SchemaModell.Bahn.Waerme, m.Finden(SchemaModell.PRAEFIX_ERZEUGER + WP_1055).Bahn);
        }

        [Fact]
        public void Projekt_1017_kuehlt_mit_der_Waermepumpe_ohne_Kaeltespeicher()
        {
            if (!_db.Vorhanden) return;
            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_WP_KUEHLT, null);

            Assert.True(m.HatKaelte);
            SchemaModell.Knoten wp = m.Finden(KE(WP_1017));
            Assert.NotNull(wp);
            Assert.Contains(R.KONF_KS_WP_KUEHLBETRIEB, wp.Zeilen);
            Assert.Contains(string.Format(R.KONF_KS_KUEHLVORLAUF, 18.0), wp.Zeilen);

            // Die Quelle (Erdsonde) nimmt die Abwärme auf - derselbe Text wie in der Wärmebahn.
            Assert.Equal(m.Finden(SchemaModell.PRAEFIX_QUELLE + WP_1017).Titel, m.Finden(KQ(WP_1017)).Titel);
            Assert.Equal(SchemaModell.Kantenart.Quelle, Kante(m, KQ(WP_1017), KE(WP_1017)).Art);

            // Kein Kältespeicher: der Kälteerzeuger versorgt den Kältekreis direkt.
            Assert.DoesNotContain(m.Knotenliste, k => k.Schluessel.StartsWith(SchemaModell.PRAEFIX_KAELTE_SPEICHER));
            Assert.Equal(SchemaModell.Kantenart.Versorgung,
                         Kante(m, KE(WP_1017), SchemaModell.ABNEHMER_KAELTEKREIS).Art);

            // Und die Wärmebahn bedient den Kältekreis nicht mehr quer über die Bahnen.
            Assert.Null(Kante(m, SchemaModell.PRAEFIX_ERZEUGER + WP_1017, SchemaModell.ABNEHMER_KAELTEKREIS));
        }

        [Fact]
        public void Projekt_1030_ohne_Kaelte_hat_keine_Kaeltebahn()
        {
            if (!_db.Vorhanden) return;
            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_OHNE_KAELTE, null);

            Assert.False(m.HatKaelte);
            Assert.Empty(m.KaelteKette);
            Assert.Null(m.Finden(SchemaModell.ABNEHMER_KAELTEKREIS));

            SchemaLayout l = SchemaLayout.Anordnen(m, 0);
            Assert.Equal(-1, l.KaelteOben);
            Assert.Equal(-1, l.KaelteKetteOben);
            Assert.Equal(l.LegendeOben + 3 * SchemaLayout.LEGENDE_ZEILE + SchemaLayout.RAND, l.Gesamthoehe);
        }

        [Fact]
        public void Die_Kaeltebahn_steht_unter_der_Waermebahn()
        {
            if (!_db.Vorhanden) return;
            SchemaLayout l = SchemaLayout.Anordnen(SchemaModell.Aufbauen(PROJEKT_AK3K, null), 0);

            Assert.True(l.KaelteOben > 0);
            foreach (SchemaLayout.Knotenflaeche k in l.Knoten)
            {
                if (k.Knoten.Bahn == SchemaModell.Bahn.Waerme)
                    Assert.True(k.Flaeche.Unten < l.KaelteOben, k.Schluessel);
                else
                    Assert.True(k.Flaeche.Y >= l.KaelteOben + SchemaLayout.BAHN_KOPF, k.Schluessel);

                Assert.Equal(l.SpaltenX[(int)k.Knoten.Art], k.Flaeche.X);
            }

            // Jede Kante der Kältebahn bleibt in der Kältebahn.
            foreach (SchemaLayout.Kantenzug z in l.Kanten)
                if (z.Kante.Von.StartsWith("K") && z.Kante.Von != "")
                    Assert.All(z.Punkte, p => Assert.True(p.Y > l.KaelteOben, z.Kante.Von + "→" + z.Kante.Nach));

            Assert.True(l.KaelteKetteOben > l.BandOben);
            Assert.True(l.LegendeOben > l.KaelteKetteOben);
            Assert.Equal(l.LegendeOben + 4 * SchemaLayout.LEGENDE_ZEILE + SchemaLayout.RAND, l.Gesamthoehe);
        }

        [Fact]
        public void Die_Huelle_gibt_Kaeltebahn_Kaskadensatz_und_Legende_weiter()
        {
            if (!_db.Vorhanden) return;

            SchemaBild mit = Schema(PROJEKT_AK3K);
            Assert.True(mit.HatKaelte);
            Assert.Equal(R.KONF_KS_KAELTE, mit.KaelteTitel);
            Assert.Equal(4, mit.KaelteSpaltenkoepfe.Count);
            Assert.Equal(string.Format(R.KONF_KS_KETTE, "Kältemaschine 10 kW"), mit.KaelteKetteText);
            SchemaLegendeeintrag marke = Assert.Single(mit.Legende, e => e.Marke);
            Assert.Equal(R.KONF_KS_KAELTE, marke.MarkeText);
            Assert.True(mit.Knoten.Single(k => k.Schluessel == KS(KALTSPEICHER_1059)).Kaelte);
            Assert.False(mit.Knoten.Single(k => k.Schluessel == SchemaModell.PRAEFIX_SPEICHER + HEIZPUFFER_1059).Kaelte);

            SchemaBild ohne = Schema(PROJEKT_OHNE_KAELTE);
            Assert.False(ohne.HatKaelte);
            Assert.Equal("", ohne.KaelteKetteText);
            Assert.Empty(ohne.KaelteSpaltenkoepfe);
            Assert.Equal(5, ohne.Legende.Count);
            Assert.DoesNotContain(ohne.Knoten, k => k.Kaelte);
        }

        private static SchemaBild Schema(int idProjekt)
        {
            var dienste = (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(idProjekt).Gaben()["Dienste"];
            return dienste.SchemaLaden(idProjekt);
        }
    }
}
