using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// K6b auf einer Kopie der Testdatenbank (Projekt 1017 mit Kühlbetrieb): eine Profilzeile mit abweichendem Kühlträger und
    /// eigenem Zähler und eine Lastgangzeile anteilig, beide dezentral (Split). Der Lauf trägt sie in Kälteseite, Strombilanz,
    /// Kältegang, Zeitreihensatz, Anlagenschema und Projektbeschreibung; ohne Split-Zeile steht nichts davon da.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaelteDezentralErgebnisDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string KUEHLRAUM = "Kühlraum";
        private const string LASTGANG = "K6b_Kuehlung";
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Kuehltraeger(int projekttraeger) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT id FROM energy_carrier WHERE pricing_model = 'ELECTRICITY' AND id <> ? ORDER BY id LIMIT 1",
            new DbParam("?", projekttraeger)), CultureInfo.InvariantCulture);

        private static void ZeilenAnlegen(int kuehl)
        {
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", KUEHLRAUM)), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 12, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35, KuehlIdCarrier = kuehl, KuehlEigenerZaehler = true,
                },
            }));
            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG,
                Enumerable.Range(0, 8760).Select(h => 0.2 + 0.3 * ((h / 24) % 2)).ToList()));
            List<Z_ProjWaermebedarfModel> zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT);
            zeilen.Add(new Z_ProjWaermebedarfModel
            {
                m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
                KuehlIdCarrier = kuehl, KuehlEigenerZaehler = false,
            });
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(PROJEKT));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(PROJEKT, zeilen));
        }

        private static SimulationRunner Rechnen()
        {
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            return lauf;
        }

        [Fact]
        public void Ohne_Split_Zeile_entsteht_keine_neue_Stelle()
        {
            if (!_db.Vorhanden) return;
            Assert.DoesNotContain(Z_ProjektKaeltebedarfCtrl.ZeilenDesProjekts(PROJEKT), z => z.Split);
            SchemaModell schema = SchemaModell.Aufbauen(PROJEKT, new List<string>());
            Assert.DoesNotContain(schema.Knotenliste, k => k.Schluessel == SchemaModell.ABNEHMER_KAELTE_DEZENTRAL);
            ProjektDetails d = ProjektDetails.Lade(PROJEKT);
            Assert.Empty(d.KaelteDezentral);
        }

        [Fact]
        public void Split_Zeilen_stehen_in_Kaelteseite_Strombilanz_Bild_Schema_und_Bericht()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(1, KaeltebedarfSplitAbrechnungSchema.Ausfuehren(null));
            int kuehl = Kuehltraeger(Kaeltestromabrechnung.Projekttraeger(PROJEKT));
            ZeilenAnlegen(kuehl);

            SimulationRunner lauf = Rechnen();
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.True(k.MitDezentral);

            // Kälteseite: Herkunft, Split-Zeilen und die zwei Deckungsgrade (Frage 1).
            SimulationErgebnisCtrl.KaelteErgebnis e = SimulationErgebnisCtrl.Kaelte(lauf.simulation_Waermebedarf);
            Assert.True(e.MitDezentral);
            Assert.Equal(2, e.Dezentral.Count);
            Assert.False(e.Dezentral[0].Profil);
            Assert.True(e.Dezentral[1].Profil && e.Dezentral[1].EigenerZaehler);
            Assert.Equal(k.Kaeltebedarf_Dezentral_Gesamt, e.DezentralMwh, 9);
            Assert.True(e.ProfilMwh > 0 && e.ExternMwh > 0);
            Assert.Equal(KaelteDeckungsgrad.Hauptzahl(e.KaeltedeckungMwh, e.DezentralMwh, e.KaeltebedarfMwh), e.DeckungsgradGesamtProzent);
            Assert.Equal(KaelteDeckungsgrad.DavonKaeltefolge(e.KaeltedeckungMwh, e.DezentralMwh, e.KaeltebedarfMwh),
                         e.DeckungsgradKaeltefolgeProzent);
            Assert.Equal(6, SimulationErgebnisCtrl.KaelteCsvSpalten(e).Count);

            // Strombilanz: „Kühlung dezentral“ ist die ganze Split-Reihe, vom Kältestrom geht nur ihr Anteil in der Stufe ab.
            SimulationErgebnisCtrl.UebersichtKennzahlen u =
                SimulationErgebnisCtrl.Uebersicht(lauf.sim, lauf.simulation_Waermebedarf, lauf.simulation_Strombedarf);
            Assert.Equal(k.Strom_Dezentral_Gesamt, u.KaeltestromDezentralMwh, 6);
            Assert.True(u.KaeltestromDezentralStufeMwh > 0);
            Assert.True(u.KaeltestromDezentralEigenerZaehlerMwh > 0);
            Assert.Equal(u.KaeltestromStufeMwh - u.KaeltestromDezentralStufeMwh, u.KaeltestromErzeugerStufeMwh, 12);

            // Kältegang und Zeitreihensatz: die Split-Fläche, dieselbe Reihe.
            KaelteProduktionBild.Reihen r = KaelteProduktionBild.AusLauf(k);
            Assert.Equal(k.Kaeltebedarf_Dezentral.Sum(), r.Dezentral.Sum(), 6);
            ZeitreihenSatz z = ZeitreihenExtraktor.AusLauf(lauf);
            Assert.Equal(r.Dezentral.Sum(), z.Hole(ZeitreihenSatz.KAELTE_DEZENTRAL).Sum(), 6);

            // Anlagenschema (Frage 2): ein Abnehmer ohne Kante, Träger und Zähler als Zeilen.
            SchemaModell schema = SchemaModell.Aufbauen(PROJEKT, new List<string>());
            SchemaModell.Knoten knoten = Assert.Single(schema.Knotenliste, x => x.Schluessel == SchemaModell.ABNEHMER_KAELTE_DEZENTRAL);
            Assert.Equal(SchemaModell.Bahn.Kaelte, knoten.Bahn);
            Assert.DoesNotContain(schema.Kantenliste, x => x.Von == knoten.Schluessel || x.Nach == knoten.Schluessel);
            Assert.Contains(knoten.Zeilen, x => x.Contains(KUEHLRAUM));
            Assert.Equal(4, knoten.Zeilen.Count);

            // Projektbeschreibung: Profile und Split-Zeilen mit EER-Weg und Träger.
            ProjektDetails d = ProjektDetails.Lade(PROJEKT);
            Assert.Equal(1, d.KaelteProfilZeilen);
            Assert.Equal(12.0, d.KaelteProfilSummeMwh, 9);
            Assert.Equal(2, d.KaelteDezentral.Count);
            Assert.Contains(d.KaelteDezentral, x => x.Bezeichner == KUEHLRAUM && x.Eer.Length > 0 && x.Traeger.Length > 0);
        }
    }
}
