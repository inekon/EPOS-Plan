using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KM‑K1 — Wärmepumpen mit Kühlfunktion in der Katalogauswahl der Kältemaschine</b> (KA‑E‑26, Anwenderentscheide
    /// 12.10.2026): die gemeinsame Katalogliste, der Bezugspunkt 7 °C mit Rückfall, Übernehmen samt Kühlbetrieb und Rang,
    /// „schon im Projekt“, Ausschalten mit geschlossener Folge, Zurücknehmen und die benannte Ablehnung — gegen eine Kopie
    /// der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehlerzeugerKatalogCtrlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;       // Wärmepumpe 10211 im Kühlbetrieb, keine Kältefolge gepflegt
        private const int STAMM_WP = 40;        // Luft-Wasser, Kühlkennlinie bei 7 und 18 °C, Quelle 35 °C
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        private static KuehlkennlinienZeile Z(int id, int vorlauf, int temp, double eer, double pk)
            => new KuehlkennlinienZeile(id, vorlauf, temp, eer, pk, null);

        private static object Feld(string sql, params object[] werte)
            => DataRepository.ExecuteScalar(sql, werte.Select(w => new DbParam("?", w)).ToArray());

        private static int? Rang(int anlage)
        {
            object v = Feld("SELECT Kaelte_Rang FROM Tab_Energieanlagen WHERE ID = ?", anlage);
            return v == null || v == DBNull.Value ? null : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Bezugspunkt_liegt_bei_7_Grad_Kaltwasser_und_faellt_auf_die_kleinste_Stuetzstelle_zurueck()
        {
            var gedeckt = new List<KuehlkennlinienZeile>
            {
                Z(1, 7, 25, 4.0, 40), Z(2, 7, 35, 3.0, 30), Z(3, 18, 25, 5.0, 50), Z(4, 18, 35, 4.0, 45)
            };
            KuehlerzeugerKatalogCtrl.Bezugswert luft = KuehlerzeugerKatalogCtrl.Bezugspunkt(gedeckt, DbWerte.WP_BAUART_LUFT_WASSER);
            Assert.False(luft.Rueckfall);
            Assert.Equal(7, luft.KaltwasserC);
            Assert.Equal(35, luft.QuelleC);
            Assert.Equal(30, luft.KuehlleistungKw, 6);
            Assert.Equal(3.0, luft.Eer, 6);

            KuehlerzeugerKatalogCtrl.Bezugswert sole = KuehlerzeugerKatalogCtrl.Bezugspunkt(gedeckt, DbWerte.WP_BAUART_SOLE_WASSER);
            Assert.Equal(30, sole.QuelleC);
            Assert.Equal(35, sole.KuehlleistungKw, 6);           // zwischen 25 und 35 °C linear

            var ohne7 = new List<KuehlkennlinienZeile> { Z(1, 10, 35, 3.5, 38), Z(2, 15, 35, 4.5, 44) };
            KuehlerzeugerKatalogCtrl.Bezugswert rueck = KuehlerzeugerKatalogCtrl.Bezugspunkt(ohne7, DbWerte.WP_BAUART_LUFT_WASSER);
            Assert.True(rueck.Rueckfall);
            Assert.Equal(10, rueck.KaltwasserC);
            Assert.Equal(38, rueck.KuehlleistungKw, 6);
            Assert.Null(KuehlerzeugerKatalogCtrl.Bezugspunkt(new List<KuehlkennlinienZeile>(), "Luft-Wasser"));
        }

        [Fact]
        public void Die_Katalogliste_fuehrt_beide_Arten_mit_Spalte_Art()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<Katalogfilterzeile> zeilen = KuehlerzeugerKatalogCtrl.Katalogzeilen();
            int maschinen = KaeltemaschineStammCtrl.Katalogfilterzeilen().Count;
            var wp = zeilen.Where(z => KuehlerzeugerKatalogCtrl.IstWaermepumpe(z.Schluessel, out _)).ToList();
            Assert.Equal(maschinen, zeilen.Count - wp.Count);
            Assert.Equal(7, wp.Count);                                        // jede Katalog-WP mit Kühlkennlinie, sonst keine
            Assert.All(wp, z => Assert.Equal(KuehlerzeugerKatalogCtrl.ArtText(true), z.Text(KuehlerzeugerKatalogCtrl.SpArt)));
            Katalogfilterzeile z40 = wp.Single(z => z.Schluessel == KuehlerzeugerKatalogCtrl.WpSchluessel(STAMM_WP));
            Assert.Equal(KuehlerzeugerKatalogCtrl.BezugspunktStamm(STAMM_WP).KuehlleistungKw,
                         z40.Zahl(Katalogfilterprofil.SpNennkaelteleistung) ?? double.NaN, 1);

            Katalogfilterprofil profil = KuehlerzeugerKatalogCtrl.Profil();
            Assert.Equal(KuehlerzeugerKatalogCtrl.SpArt, profil.Spalten[0].Schluessel);
            Assert.NotNull(profil.Spalte(Katalogfilterprofil.SpQuelle));
            Assert.NotNull(profil.Spalte(Katalogfilterprofil.SpVerwendet));
            Assert.Equal(KuehlerzeugerKatalogCtrl.PROFIL, profil.Schluessel);
        }

        /// <summary>
        /// KM‑K2: Die Übernahme aus dem Kältedialog lässt die WÄRMEKASKADE unberührt — genau wie der Anlegeweg des
        /// Wärmepumpendialogs, der ebenfalls keinen Platz vergibt (den Platz pflegt allein die
        /// Simulationskonfiguration). Ohne Platz kühlt die Wärmepumpe nicht; das steht als Hinweis am Ergebnis und
        /// wird nicht still übergangen.
        /// </summary>
        [Fact]
        public void Uebernehmen_laesst_die_Waermekaskade_wie_der_Waermepumpendialog_und_nennt_den_fehlenden_Platz()
        {
            if (!_db.Vorhanden) return;
            List<string> vorher = KonfigurationCtrl.KaskadeLesen(PROJEKT);
            bool platz = vorher.Contains(DbWerte.ERZEUGER_WAERMEPUMPE);

            KuehlerzeugerKatalogCtrl.Uebernahme u = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(PROJEKT, STAMM_WP);
            Assert.True(u.Ok);
            Assert.Equal(vorher, KonfigurationCtrl.KaskadeLesen(PROJEKT));
            Assert.Equal(platz ? null : WindowsFormsApplication1.MyResource.Resource.KMA_MSG_WP_OHNE_KASKADE, u.Hinweis);
            Assert.Equal(u.Hinweis, KuehlerzeugerKatalogCtrl.WaermekaskadeHinweis(PROJEKT));
            Assert.Null(KuehlerzeugerKatalogCtrl.WaermekaskadeHinweis(0));

            KuehlerzeugerKatalogCtrl.UebernahmeZuruecknehmen(PROJEKT, u);
        }

        /// <summary>
        /// KM‑K2: Die Verwendungsmarke „im Projekt verwendet" zählt JEDE Projektkopie der Wärmepumpe — nicht erst die
        /// mit eingeschaltetem Kühlbetrieb (dasselbe Maß nimmt die Projektliste des Wärmepumpendialogs).
        /// </summary>
        [Fact]
        public void Die_Verwendungsmarke_zaehlt_jede_Projektkopie_der_Waermepumpe()
        {
            if (!_db.Vorhanden) return;
            IReadOnlyList<string> namen = KuehlerzeugerKatalogCtrl.ProjektkopienNamen(PROJEKT);
            Assert.NotEmpty(namen);
            foreach (KuehlerzeugerKatalogCtrl.ProjektWaermepumpe w in KuehlerzeugerKatalogCtrl.ProjektWaermepumpen(PROJEKT))
                Assert.Contains(w.Bezeichner, namen);

            // Eine eben übernommene Kopie steht sofort da; ihr Kühlbetrieb spielt keine Rolle.
            KuehlerzeugerKatalogCtrl.Uebernahme u = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(PROJEKT, STAMM_WP);
            Assert.Contains(u.Bezeichner, KuehlerzeugerKatalogCtrl.ProjektkopienNamen(PROJEKT));
            KuehlerzeugerKatalogCtrl.KuehlbetriebSetzen(PROJEKT, u.IdWp, false);
            Assert.Contains(u.Bezeichner, KuehlerzeugerKatalogCtrl.ProjektkopienNamen(PROJEKT));
            KuehlerzeugerKatalogCtrl.UebernahmeZuruecknehmen(PROJEKT, u);

            Assert.Empty(KuehlerzeugerKatalogCtrl.ProjektkopienNamen(0));
        }

        [Fact]
        public void Uebernehmen_legt_Kopie_Anlage_Kuehlbetrieb_und_Rang_an_und_laesst_sich_zuruecknehmen()
        {
            if (!_db.Vorhanden) return;
            KuehlerzeugerKatalogCtrl.Uebernahme u = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(PROJEKT, STAMM_WP);
            Assert.True(u.Ok, u.Meldung);
            Assert.True(u.KopieNeu);
            Assert.False(u.SchonImProjekt);
            int anlage = Assert.Single(u.NeueAnlagen);
            Assert.Equal(1L, Convert.ToInt64(Feld("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = ?", u.IdWp)));
            Assert.Equal(1, Rang(anlage));                                     // hinter den Erzeugern mit Rang (keine)
            Assert.Contains(KuehlerzeugerKatalogCtrl.ProjektWaermepumpen(PROJEKT), w => w.IdWp == u.IdWp && w.Rang == 1);

            // Schon im Projekt: keine zweite Kopie, keine zweite Anlage - nur der Kühlbetrieb.
            KuehlerzeugerKatalogCtrl.Uebernahme zwei = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(PROJEKT, STAMM_WP);
            Assert.True(zwei.SchonImProjekt);
            Assert.Equal(u.IdWp, zwei.IdWp);
            Assert.Empty(zwei.NeueAnlagen);
            Assert.Single(KuehlerzeugerKatalogCtrl.AnlagenDerKopie(PROJEKT, u.IdWp));

            // Ausschalten: Kühlbetrieb aus, Rang weg - Gerät und Anlage bleiben.
            Assert.Null(KuehlerzeugerKatalogCtrl.KuehlbetriebSetzen(PROJEKT, u.IdWp, false));
            Assert.Equal(0L, Convert.ToInt64(Feld("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = ?", u.IdWp)));
            Assert.Null(Rang(anlage));
            Assert.Single(KuehlerzeugerKatalogCtrl.AnlagenDerKopie(PROJEKT, u.IdWp));

            // Zurücknehmen: Anlage und neue Kopie gehen.
            KuehlerzeugerKatalogCtrl.UebernahmeZuruecknehmen(PROJEKT, u);
            Assert.Empty(KuehlerzeugerKatalogCtrl.AnlagenDerKopie(PROJEKT, u.IdWp));
            Assert.Equal(0L, Convert.ToInt64(Feld("SELECT COUNT(*) FROM Tab_WP WHERE ID = ?", u.IdWp)));
        }

        [Fact]
        public void Ohne_Kuehlkennlinie_im_Projekt_lehnt_der_Kern_den_Kuehlbetrieb_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            KuehlerzeugerKatalogCtrl.Uebernahme u = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(PROJEKT, STAMM_WP);
            Assert.True(u.Ok, u.Meldung);
            Assert.Null(KuehlerzeugerKatalogCtrl.KuehlbetriebSetzen(PROJEKT, u.IdWp, false));
            DataRepository.ExecuteSQL("DELETE FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = ?", new DbParam("?", u.IdWp));
            string grund = KuehlerzeugerKatalogCtrl.KuehlbetriebSetzen(PROJEKT, u.IdWp, true);
            Assert.Equal(WPCtrl.KuehlbetriebSperrgrund(u.IdWp, PROJEKT), grund);
            Assert.False(string.IsNullOrEmpty(grund));
            Assert.Equal(0L, Convert.ToInt64(Feld("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = ?", u.IdWp)));
            Assert.All(u.NeueAnlagen, a => Assert.Null(Rang(a)));
        }

        [Fact]
        public void Rang_anhaengen_und_entfernen_schliesst_die_Folge()
        {
            if (!_db.Vorhanden) return;
            int stamm = Convert.ToInt32(Feld("SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", LUFTGEKUEHLT),
                                        CultureInfo.InvariantCulture);
            int nord = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Nord");
            int sued = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Süd");
            Assert.Null(KaeltefolgeCtrl.RangAnhaengen(PROJEKT, new[] { nord }));
            Assert.Null(KaeltefolgeCtrl.RangAnhaengen(PROJEKT, new[] { sued, nord }));
            Assert.Equal(1, Rang(nord));
            Assert.Equal(2, Rang(sued));
            IReadOnlyDictionary<int, int> gemerkt = Kaeltefolge.RaengeLesen(PROJEKT);

            Assert.Null(KaeltefolgeCtrl.RangEntfernen(PROJEKT, new[] { nord }));
            Assert.Null(Rang(nord));
            Assert.Equal(1, Rang(sued));                                       // die Folge schließt lückenlos

            Assert.Null(KaeltefolgeCtrl.RaengeWiederherstellen(PROJEKT, gemerkt));
            Assert.Equal(1, Rang(nord));
            Assert.Equal(2, Rang(sued));
            Assert.Equal(new[] { nord, sued }, KaeltefolgeCtrl.FolgeLesen(PROJEKT).Take(2));
        }
    }
}
