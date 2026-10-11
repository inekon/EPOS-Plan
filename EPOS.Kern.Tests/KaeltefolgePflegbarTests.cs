using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle KB-D — die pflegbare Kältefolge</b> (Entscheid E117 F1, Schemaschritt <see cref="KaelteRangSchema.SCHRITT"/>):
    /// Ohne Rang gilt die Vorgabefolge (Wärmepumpen, dann Kältemaschinen); ein gepflegter Rang ordnet die Erzeuger in Leser,
    /// Lauf und Schema gleich um; der Schreibweg <see cref="KaeltefolgeCtrl"/> prüft, schreibt eindeutig und setzt zurück;
    /// Duplizieren nimmt den Rang mit, Löschen einer Anlage lässt die übrige Folge stehen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltefolgePflegbarTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;      // Wärmepumpe 10211 im Kühlbetrieb
        private const int WP = 10211;
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        private static int Stamm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        private static Kaeltekaskade Rechnen(int projekt)
        {
            var lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(projekt, out string fehler) > 0, fehler);
            return lauf.simulation_Kaeltebedarf.Kaskade;
        }

        /// <summary>Zwei Kältemaschinen neben der Wärmepumpe von 1017: Vorgabefolge WP, Nord, Süd.</summary>
        private static (int Nord, int Sued) ZweiKaeltemaschinen()
        {
            int stamm = Stamm(LUFTGEKUEHLT);
            int nord = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Nord");
            int sued = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Süd");
            Assert.True(nord > 0 && sued > nord);
            return (nord, sued);
        }

        [Fact]
        public void Ohne_Rang_gilt_die_Vorgabefolge_und_der_Rang_ordnet_stabil()
        {
            var eintraege = new[]
            {
                (Name: "KM a", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 0, Rang: (int?)null),
                (Name: "WP 1", Art: KaeltefolgeStufe.Waermepumpe, Platz: 1, Rang: (int?)null),
                (Name: "KM b", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 1, Rang: (int?)null),
                (Name: "WP 0", Art: KaeltefolgeStufe.Waermepumpe, Platz: 0, Rang: (int?)null),
            };
            Assert.Equal(new[] { "WP 0", "WP 1", "KM a", "KM b" },
                         Kaeltefolge.ErzeugerOrdnen(eintraege, e => e.Art, e => e.Platz, e => e.Rang).Select(e => e.Name));
            Assert.Equal(Kaeltefolge.ErzeugerOrdnen(eintraege, e => e.Art, e => e.Platz).Select(e => e.Name),
                         Kaeltefolge.ErzeugerOrdnen(eintraege, e => e.Art, e => e.Platz, e => e.Rang).Select(e => e.Name));

            // Gepflegt: die mit Rang vorn, aufsteigend; ohne Rang dahinter in der Vorgabefolge.
            var gepflegt = new[]
            {
                (Name: "KM a", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 0, Rang: (int?)2),
                (Name: "WP 1", Art: KaeltefolgeStufe.Waermepumpe, Platz: 1, Rang: (int?)null),
                (Name: "KM b", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 1, Rang: (int?)1),
                (Name: "WP 0", Art: KaeltefolgeStufe.Waermepumpe, Platz: 0, Rang: (int?)null),
            };
            Assert.Equal(new[] { "KM b", "KM a", "WP 0", "WP 1" },
                         Kaeltefolge.ErzeugerOrdnen(gepflegt, e => e.Art, e => e.Platz, e => e.Rang).Select(e => e.Name));
        }

        [Fact]
        public void Die_Referenzprojekte_tragen_keinen_Rang()
        {
            if (!_db.Vorhanden) return;
            foreach (int p in new[] { 1017, 1047, 1055, 1058, 1059, 1061, 1062, 1063, 1064 })
            {
                Assert.Empty(Kaeltefolge.RaengeLesen(p));
                Assert.False(Kaeltefolge.Lesen(p).Gepflegt);
            }
        }

        [Fact]
        public void Die_gepflegte_Folge_wirkt_im_Lauf_auf_die_Deckung_und_die_Vorgabe_stellt_sie_zurueck()
        {
            if (!_db.Vorhanden) return;
            (int nord, int sued) = ZweiKaeltemaschinen();

            Kaeltekaskade vorher = Rechnen(PROJEKT);
            Assert.Equal(new[] { WP, nord, sued }, vorher.Erzeuger.Select(e => e.AnlagenID));
            double wpVorher = vorher.Erzeuger.Single(e => e.AnlagenID == WP).KaelteGesamtKwh;
            double suedVorher = vorher.Erzeuger.Single(e => e.AnlagenID == sued).KaelteGesamtKwh;
            Assert.True(wpVorher > 0);

            // Süd zuerst, die Wärmepumpe zuletzt.
            Assert.Null(KaeltefolgeCtrl.FolgeSetzen(PROJEKT, new[] { sued, nord, WP }));
            Assert.Equal(new Dictionary<int, int> { [sued] = 1, [nord] = 2, [WP] = 3 }, Kaeltefolge.RaengeLesen(PROJEKT));
            KaeltefolgeStand stand = Kaeltefolge.Lesen(PROJEKT);
            Assert.True(stand.Gepflegt);
            Assert.Equal(new[] { sued, nord, WP }, stand.Erzeuger.Select(e => e.AnlagenId));
            Assert.Equal(new int?[] { 1, 2, 3 }, stand.Erzeuger.Select(e => e.KaelteRang));
            Assert.Equal(Kaeltefolge.Stufen, stand.Stufen);   // die Stufen bleiben fest

            Kaeltekaskade nachher = Rechnen(PROJEKT);
            Assert.Equal(new[] { sued, nord, WP }, nachher.Erzeuger.Select(e => e.AnlagenID));
            double wpNachher = nachher.Erzeuger.Single(e => e.AnlagenID == WP).KaelteGesamtKwh;
            double suedNachher = nachher.Erzeuger.Single(e => e.AnlagenID == sued).KaelteGesamtKwh;
            Assert.True(suedNachher > suedVorher, "Süd deckt vorn mehr: " + suedNachher + " gegen " + suedVorher);
            Assert.True(wpNachher < wpVorher, "Die Wärmepumpe deckt hinten weniger: " + wpNachher + " gegen " + wpVorher);

            // Das Schema zeichnet die Kältebahn in derselben Folge.
            SchemaModell schema = SchemaModell.Aufbauen(PROJEKT, null);
            Assert.Equal(stand.Erzeuger.Where(e => e.Kuehlbetrieb).Select(e => e.Bezeichner), schema.KaelteKette);

            // Vorgabefolge: kein Rang mehr, der Lauf rechnet wie vorher.
            Assert.Null(KaeltefolgeCtrl.VorgabeSetzen(PROJEKT));
            Assert.Empty(Kaeltefolge.RaengeLesen(PROJEKT));
            Assert.False(Kaeltefolge.Lesen(PROJEKT).Gepflegt);
            Kaeltekaskade zurueck = Rechnen(PROJEKT);
            Assert.Equal(new[] { WP, nord, sued }, zurueck.Erzeuger.Select(e => e.AnlagenID));
            Assert.Equal(wpVorher, zurueck.Erzeuger.Single(e => e.AnlagenID == WP).KaelteGesamtKwh);
        }

        [Fact]
        public void Der_Schreibweg_prueft_die_Folge()
        {
            if (!_db.Vorhanden) return;
            (int nord, int sued) = ZweiKaeltemaschinen();

            Assert.NotNull(KaeltefolgeCtrl.FolgeSetzen(PROJEKT, new[] { nord, nord, WP }));          // doppelt
            Assert.NotNull(KaeltefolgeCtrl.FolgeSetzen(PROJEKT, new[] { nord, WP }));                // unvollständig
            int fremd = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type NOT IN (?, ?)",
                new DbParam("?", PROJEKT), new DbParam("?", WizardItemClass.WP_TYP), new DbParam("?", WizardItemClass.KM_TYP)),
                CultureInfo.InvariantCulture);
            Assert.NotNull(KaeltefolgeCtrl.FolgeSetzen(PROJEKT, new[] { nord, sued, WP, fremd }));  // kein Kälteerzeuger
            Assert.NotNull(KaeltefolgeCtrl.FolgeSetzen(0, new[] { nord }));                          // kein Projekt
            Assert.Empty(Kaeltefolge.RaengeLesen(PROJEKT));                                          // nichts geschrieben

            // Verschieben: am Rand benannt abgelehnt, sonst die ganze Folge eindeutig ab 1.
            Assert.NotNull(KaeltefolgeCtrl.Verschieben(PROJEKT, WP, KaeltefolgeCtrl.NACH_VORN));
            Assert.NotNull(KaeltefolgeCtrl.Verschieben(PROJEKT, sued, KaeltefolgeCtrl.NACH_HINTEN));
            Assert.NotNull(KaeltefolgeCtrl.Verschieben(PROJEKT, fremd, KaeltefolgeCtrl.NACH_VORN));
            Assert.Null(KaeltefolgeCtrl.Verschieben(PROJEKT, nord, KaeltefolgeCtrl.NACH_VORN));
            Assert.Equal(new[] { nord, WP, sued }, KaeltefolgeCtrl.FolgeLesen(PROJEKT));
            Assert.Equal(new[] { 1, 2, 3 }, Kaeltefolge.RaengeLesen(PROJEKT).Values.OrderBy(r => r));
        }

        [Fact]
        public void Duplizieren_nimmt_den_Rang_mit_und_Loeschen_laesst_die_Folge_stehen()
        {
            if (!_db.Vorhanden) return;
            (int nord, int sued) = ZweiKaeltemaschinen();
            Assert.Null(KaeltefolgeCtrl.FolgeSetzen(PROJEKT, new[] { sued, WP, nord }));

            var dup = new ProjektDuplizierenCtrl();
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("?", PROJEKT)), CultureInfo.InvariantCulture);
            int kopie = dup.Duplizieren(name, name + " Kältefolge");
            Assert.True(kopie > 0);
            KaeltefolgeStand k = Kaeltefolge.Lesen(kopie);
            Assert.True(k.Gepflegt);
            Assert.Equal(new[] { "KM Süd", Kaeltefolge.Lesen(PROJEKT).Erzeuger[1].Bezeichner, "KM Nord" },
                         k.Erzeuger.Select(e => e.Bezeichner));
            Assert.Equal(new int?[] { 1, 2, 3 }, k.Erzeuger.Select(e => e.KaelteRang));

            // Eine Anlage weg: ihr Rang geht mit der Zeile, die übrige Folge bleibt.
            KaeltemaschineAnlageCtrl.Loeschen(sued);
            Assert.Equal(new[] { WP, nord }, KaeltefolgeCtrl.FolgeLesen(PROJEKT));
            Assert.True(Kaeltefolge.Lesen(PROJEKT).Gepflegt);
            Assert.Null(KaeltefolgeCtrl.Verschieben(PROJEKT, nord, KaeltefolgeCtrl.NACH_VORN));
            Assert.Equal(new Dictionary<int, int> { [nord] = 1, [WP] = 2 }, Kaeltefolge.RaengeLesen(PROJEKT));
        }

        [Fact]
        public void Die_Huelle_fuehrt_Pfeile_Vorgabefolge_und_Hinweis()
        {
            Assert.False(KaeltebereichBau.Daten(0).FolgeGepflegt);
            if (!_db.Vorhanden) return;
            (int nord, int sued) = ZweiKaeltemaschinen();

            KaeltebereichDaten d = KaeltebereichBau.Daten(PROJEKT);
            Assert.False(d.FolgeGepflegt);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KAELTEFOLGE_HINWEIS_VORGABE, d.FolgeHinweis);
            Assert.Equal(new[] { WP, nord, sued }, d.Erzeuger.Select(z => z.IdAnlage));
            Assert.Equal(new[] { false, true, true }, d.Erzeuger.Select(z => z.NachVornMoeglich));
            Assert.Equal(new[] { true, true, false }, d.Erzeuger.Select(z => z.NachHintenMoeglich));
            Assert.All(d.Erzeuger, z => Assert.Null(z.KaelteRang));

            var dienste = (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(PROJEKT).Gaben()["Dienste"];
            Assert.NotNull(dienste.KaelteVerschieben);
            Assert.NotNull(dienste.KaelteVorgabefolge);
            Assert.Null(dienste.KaelteVerschieben(sued, -1));
            Assert.NotNull(dienste.KaelteVerschieben(WP, -1));            // am Rand
            KaeltebereichDaten nachher = dienste.Laden(PROJEKT).Kaeltebereich;
            Assert.True(nachher.FolgeGepflegt);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KAELTEFOLGE_HINWEIS_GEPFLEGT, nachher.FolgeHinweis);
            Assert.Equal(new[] { WP, sued, nord }, nachher.Erzeuger.Select(z => z.IdAnlage));
            Assert.Equal(new int?[] { 1, 2, 3 }, nachher.Erzeuger.Select(z => z.KaelteRang));
            Assert.Equal(nachher.Erzeuger.Select(z => z.Nummer.ToString(CultureInfo.CurrentCulture)),
                         nachher.Erzeuger.Select(z => z.Kachel.Rang));

            // Die KI-Sicht liest die Folge und setzt nur auf die Vorgabe zurück.
            SimulationKonfigDaten stand = dienste.Laden(PROJEKT);
            var sicht = new SimulationKiSicht(() => stand, () => null, () => null, () => null, () => "", () => "",
                                              konfigwege: () => dienste);
            Assert.True(sicht.KaeltefolgeGepflegt);
            Assert.Contains("KM Süd", sicht.Kaeltefolge, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() => sicht.KaeltefolgeGepflegt = true);
            sicht.KaeltefolgeGepflegt = false;
            Assert.False(sicht.KaeltefolgeGepflegt);
            Assert.False(dienste.Laden(PROJEKT).Kaeltebereich.FolgeGepflegt);
            Assert.Equal(new[] { WP, nord, sued }, dienste.Laden(PROJEKT).Kaeltebereich.Erzeuger.Select(z => z.IdAnlage));
        }
    }
}
