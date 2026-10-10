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
    /// <b>Welle KB-A — die Kältefolge als eine Quelle</b> (Entwurf Kältebereich 3.4): Die Ordnungsregeln von
    /// <see cref="Kaeltefolge"/> sind stabil; der Leser <see cref="Kaeltefolge.Lesen"/> liefert die Kälteerzeuger und
    /// Kältespeicher in genau der Folge, in der der Lauf sie rechnet (Probe gegen <c>Kaeltekaskade.Erzeuger</c> und
    /// <c>Kaeltekaskade.Speicher</c> eines Laufs); die Hülle des Bereichs „Kälte“ baut daraus das DTO samt Schreibweg.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltefolgeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

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

        /// <summary>Die Probe: dieselbe Folge in Leser und Lauf — Erzeuger (im Kühlbetrieb) und Kältespeicher.</summary>
        private static void FolgeGleichDemLauf(int projekt, Kaeltekaskade k)
        {
            KaeltefolgeStand stand = Kaeltefolge.Lesen(projekt);
            Assert.Equal(k.Erzeuger.Select(e => e.AnlagenID),
                         stand.Erzeuger.Where(e => e.Kuehlbetrieb).Select(e => e.AnlagenId));
            Assert.Equal(k.Speicher.Select(s => s.ID_Pufferspeicher), stand.Kaeltespeicher.Select(p => p.ID));
        }

        [Fact]
        public void Die_Stufen_stehen_fest_und_die_Ordnung_ist_stabil()
        {
            Assert.Equal(new[] { KaeltefolgeStufe.FreieKuehlung, KaeltefolgeStufe.Kaeltespeicher,
                                 KaeltefolgeStufe.Waermepumpe, KaeltefolgeStufe.Kaeltemaschine }, Kaeltefolge.Stufen);

            var eintraege = new[]
            {
                (Name: "KM a", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 0),
                (Name: "WP 2", Art: KaeltefolgeStufe.Waermepumpe, Platz: 2),
                (Name: "KM b", Art: KaeltefolgeStufe.Kaeltemaschine, Platz: 1),
                (Name: "WP 0", Art: KaeltefolgeStufe.Waermepumpe, Platz: 0),
                (Name: "WP 0'", Art: KaeltefolgeStufe.Waermepumpe, Platz: 0)
            };
            Assert.Equal(new[] { "WP 0", "WP 0'", "WP 2", "KM a", "KM b" },
                         Kaeltefolge.ErzeugerOrdnen(eintraege, e => e.Art, e => e.Platz).Select(e => e.Name));

            // Kältespeicher: gepflegte Entladepriorität vorn, 0 hinten in Eingangsfolge.
            var speicher = new[] { (N: "A", P: 0), (N: "B", P: 2), (N: "C", P: 0), (N: "D", P: 1) };
            Assert.Equal(new[] { "D", "B", "A", "C" },
                         Kaeltefolge.KaeltespeicherOrdnen(speicher, s => s.P).Select(s => s.N));

            // Kältemaschinen nach Anlagen-ID.
            var km = new[] { new KaeltemaschineAnlageModel { AnlagenId = 9 }, null, new KaeltemaschineAnlageModel { AnlagenId = 3 } };
            Assert.Equal(new[] { 3, 9 }, Kaeltefolge.KaeltemaschinenOrdnen(km).Select(a => a.AnlagenId));

            // Ohne Projekt der leere Stand - ohne Datenbank.
            KaeltefolgeStand leer = Kaeltefolge.Lesen(0);
            Assert.Empty(leer.Erzeuger);
            Assert.Empty(leer.Kaeltespeicher);
            Assert.False(leer.Kuehlbetrieb);
        }

        [Fact]
        public void Der_Leser_liefert_die_Folge_des_Laufs_mit_Kaeltemaschine_und_Kaeltespeicher()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1055;   // Wärmepumpe ohne Kühlbetrieb, eine Kältemaschine, ein Kältespeicher
            Kaeltekaskade k = Rechnen(PROJEKT);
            Assert.NotEmpty(k.Erzeuger);
            Assert.NotEmpty(k.Speicher);
            FolgeGleichDemLauf(PROJEKT, k);

            KaeltefolgeStand stand = Kaeltefolge.Lesen(PROJEKT);
            Assert.True(stand.Kuehlbetrieb);
            KaelteerzeugerEintrag km = Assert.Single(stand.Erzeuger, e => e.Art == KaeltefolgeStufe.Kaeltemaschine);
            Assert.True(km.Kuehlbetrieb);
            Assert.True(km.IdGeraet > 0);
            Assert.Equal(1, km.AnlagenJeKopie);
            // Eine Wärmepumpe ohne Kühlbetrieb steht in der Liste (aufnehmbar), aber nicht im Lauf.
            Assert.All(stand.Erzeuger.Where(e => e.Art == KaeltefolgeStufe.Waermepumpe), e => Assert.False(e.Kuehlbetrieb));
            // Freie Kühlung: genau die Kältemaschinen mit Trocken- oder Nasskühler, vor allen.
            Assert.Equal(stand.Erzeuger.Where(e => e.FreieKuehlung && e.Art == KaeltefolgeStufe.Kaeltemaschine).Select(e => e.AnlagenId),
                         stand.FreieKuehlung.Where(f => f.VorAllenErzeugern).Select(f => f.AnlagenId));
        }

        [Fact]
        public void Waermepumpe_vor_den_Kaeltemaschinen_und_diese_nach_Anlagen_ID_wie_im_Lauf()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1017;   // Wärmepumpe im Kühlbetrieb
            int stamm = Stamm(LUFTGEKUEHLT);
            int erste = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Nord");
            int zweite = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, stamm, "KM Süd");
            Assert.True(erste > 0 && zweite > erste);

            Kaeltekaskade k = Rechnen(PROJEKT);
            FolgeGleichDemLauf(PROJEKT, k);
            KaeltefolgeStand stand = Kaeltefolge.Lesen(PROJEKT);
            Assert.Equal(new[] { KaeltefolgeStufe.Waermepumpe, KaeltefolgeStufe.Kaeltemaschine, KaeltefolgeStufe.Kaeltemaschine },
                         stand.Erzeuger.Select(e => e.Art));
            Assert.Equal(new[] { erste, zweite }, stand.Erzeuger.Skip(1).Select(e => e.AnlagenId));
            Assert.Equal(new[] { "KM Nord", "KM Süd" }, stand.Erzeuger.Skip(1).Select(e => e.Bezeichner));
            KaelteerzeugerEintrag wp = stand.Erzeuger[0];
            Assert.True(wp.Kuehlbetrieb);
            Assert.True(wp.InWaermekaskade);
            Assert.Equal(10211, wp.AnlagenId);
        }

        [Fact]
        public void Die_Huelle_baut_den_Kaeltebereich_aus_dem_Leser_samt_Schreibweg_des_Kuehlbetriebs()
        {
            Assert.Empty(KaeltebereichBau.Daten(0).Erzeuger);
            Assert.True(KaeltebereichBau.Daten(0).Leer);
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1055;

            KaeltebereichDaten d = KaeltebereichBau.Daten(PROJEKT);
            KaeltefolgeStand stand = Kaeltefolge.Lesen(PROJEKT);
            Assert.True(d.Kuehlbetrieb);
            Assert.False(d.Leer);
            Assert.Equal(new[] { KaelteStufe.FreieKuehlung, KaelteStufe.Kaeltespeicher, KaelteStufe.Waermepumpe, KaelteStufe.Kaeltemaschine },
                         d.Folge);
            Assert.Equal(stand.Erzeuger.Select(e => e.AnlagenId), d.Erzeuger.Select(z => z.IdAnlage));
            Assert.Equal(Enumerable.Range(1, d.Erzeuger.Count), d.Erzeuger.Select(z => z.Nummer));
            Assert.Equal(d.Erzeuger.Select(z => z.Nummer.ToString(CultureInfo.CurrentCulture)), d.Erzeuger.Select(z => z.Kachel.Rang));
            KaelteerzeugerZeile km = Assert.Single(d.Erzeuger, z => z.Art == KaelteStufe.Kaeltemaschine);
            Assert.False(km.Kachel.Umschaltbar);
            Assert.Equal(EPOS.UI.Bausteine.Kachelzustand.Aufgenommen, km.Kachel.Zustand);
            Assert.Contains(km.Kachel.Chips, c => c.Text == WindowsFormsApplication1.MyResource.Resource.KONF_KS_KAELTEMASCHINE);
            Assert.Equal(new[] { 1071273 }, d.Kaeltespeicher.Select(s => s.IdPuffer));

            // Die Seite der Hülle: Kältebereich im Ladestand, Schreibweg des Kühlbetriebs einer Wärmepumpe.
            var dienste = (SimulationKonfigDienste)SimulationKonfigHuelle.Erzeugen(PROJEKT).Gaben()["Dienste"];
            SimulationKonfigDaten geladen = dienste.Laden(PROJEKT);
            Assert.Equal(d.Erzeuger.Select(z => z.IdAnlage), geladen.Kaeltebereich.Erzeuger.Select(z => z.IdAnlage));
            Assert.Equal(new[] { 1071273 }, geladen.Kaeltebereich.Kaeltespeicher.Select(s => s.IdPuffer));
            Assert.NotNull(dienste.KuehlbetriebWpSchreiben);

            KaelteerzeugerZeile wp = d.Erzeuger.FirstOrDefault(z => z.Art == KaelteStufe.Waermepumpe && z.Sperrgrund == null);
            if (wp == null) return;
            Assert.False(wp.Kuehlbetrieb);
            Assert.True(wp.Kachel.Umschaltbar);
            string grund = dienste.KuehlbetriebWpSchreiben(wp.IdGeraet, true);
            Assert.Null(grund);
            KaelteerzeugerZeile nachher = KaeltebereichBau.Daten(PROJEKT).Erzeuger.Single(z => z.IdAnlage == wp.IdAnlage);
            Assert.True(nachher.Kuehlbetrieb);
            Assert.Equal(EPOS.UI.Bausteine.Kachelzustand.Aufgenommen, nachher.Kachel.Zustand);
        }
    }
}
