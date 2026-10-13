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
            // K6b-N: ohne Lastgang- und Profilzuordnung keine Herkunftszeilen am Kältekreis.
            SchemaModell.Knoten kreis = Assert.Single(SchemaModell.Aufbauen(PROJEKT, null).Knotenliste,
                                                      x => x.Schluessel == SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.DoesNotContain(kreis.Zeilen, x => x.StartsWith(Davon(WindowsFormsApplication1.MyResource.Resource.KONF_KS_DAVON_LASTGANG), StringComparison.Ordinal)
                                                  || x.StartsWith(Davon(WindowsFormsApplication1.MyResource.Resource.KONF_KS_DAVON_PROFIL), StringComparison.Ordinal));
        }

        private static string Davon(string format) => format.Substring(0, format.IndexOf('{'));

        /// <summary>
        /// K6b-N: die Zeilen des Knotens „Dezentral (Split)“ ohne Datenbank — der Kurzname gekürzt wie lange Knotentitel auf
        /// die Abnehmerspalte, die Sammelzeile „Strom: Projektträger“ nur, wenn alle Zeilen den Träger des Projekts nehmen,
        /// sonst „Strom: n Träger“ mit der Zahl der verschiedenen Träger (der Projektträger zählt mit).
        /// </summary>
        [Fact]
        public void K6bN_Der_Split_Knoten_kuerzt_auf_die_Abnehmerspalte_und_sammelt_den_Strom()
        {
            string zeile = WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_ZEILE;
            int breite = SchemaLayout.SPALTEN_BREITE[(int)SchemaModell.Knotenart.Abnehmer] - 2 * SchemaLayout.KNOTEN_RAND;

            List<string> projekt = SchemaModell.KaelteDezentralZeilen(
                new[] { ("Kühlraum", "3,0", (int?)null), ("Büro", "3,5", (int?)5) }, 5);
            Assert.Equal(new[]
            {
                string.Format(CultureInfo.CurrentCulture, zeile, "Kühlraum", "3,0"),
                string.Format(CultureInfo.CurrentCulture, zeile, "Büro", "3,5"),
                WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_STROM_PROJEKT
            }, projekt);

            List<string> drei = SchemaModell.KaelteDezentralZeilen(new[]
            {
                ("Raumkühlung Bürotrakt Nord", "3,0", (int?)null), ("A", "3,0", (int?)7), ("B", "4,0", (int?)8), ("C", "4,0", (int?)7)
            }, 5);
            Assert.Equal(5, drei.Count);
            Assert.EndsWith("…" + string.Format(CultureInfo.CurrentCulture, zeile, "", "3,0"), drei[0]);
            Assert.All(drei, x => Assert.True(SchemaLayout.TextBreite(x) <= breite, x));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                                       WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_STROM_TRAEGER, 3), drei[4]);

            Assert.Empty(SchemaModell.KaelteDezentralZeilen(Array.Empty<(string, string, int?)>(), 5));
        }

        /// <summary>
        /// K6b-N (Plan K6, Abschnitt 4): am Kältekreis die Herkunft des zentralen Kältebedarfs — „davon Lastgang“ (Zahl der
        /// Lastgangzeilen im Kanal Kühlung) und „davon Profil“ (Zahl der Profile); Split-Zeilen zählen nicht.
        /// </summary>
        [Fact]
        public void K6bN_Der_Kaeltekreis_nennt_davon_Lastgang_und_davon_Profil()
        {
            if (!_db.Vorhanden) return;
            int stamm = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", KUEHLRAUM)), CultureInfo.InvariantCulture);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 12 },
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 3.5 },
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = stamm, Bezeichner = KUEHLRAUM, Summe = 100, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35,
                },
            }));
            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG, Enumerable.Repeat(0.5, 8760).ToList()));
            List<Z_ProjWaermebedarfModel> zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT);
            zeilen.Add(new Z_ProjWaermebedarfModel { m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG });
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(PROJEKT));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(PROJEKT, zeilen));

            SchemaModell schema = SchemaModell.Aufbauen(PROJEKT, null);
            SchemaModell.Knoten kreis = Assert.Single(schema.Knotenliste, x => x.Schluessel == SchemaModell.ABNEHMER_KAELTEKREIS);
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.KONF_KS_DAVON_LASTGANG, 1), kreis.Zeilen);
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.KONF_KS_DAVON_PROFIL, 2),
                            kreis.Zeilen);
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.KONF_KS_DAVON_LASTGANG, 1), kreis.Hinweis);
        }

        [Fact]
        public void Split_Zeilen_stehen_in_Kaelteseite_Strombilanz_Bild_Schema_und_Bericht()
        {
            if (!_db.Vorhanden) return;
            // Die Testdatenbank steht auf Schritt 223: die Tabelle ist da, ohne dass der Test sie anlegt.
            Assert.True(KaeltebedarfSplitAbrechnungSchema.TabelleVorhanden(), "Die Testdatenbank steht auf Schritt 223.");
            int kuehl = Kuehltraeger(Kaeltestromabrechnung.Projekttraeger(PROJEKT));
            ZeilenAnlegen(kuehl);

            SimulationRunner lauf = Rechnen();
            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.True(k.MitDezentral);
            // Rundweg über ErgebnisCtrl mit der echten Tabelle: je Split-Zeile eine gespeicherte Zeile am Lauf, gelesen in
            // derselben Folge (gerundet gespeichert wie jede Ergebnisspalte).
            Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + KaeltebedarfSplitAbrechnungSchema.TAB_ERGEBNIS + " d JOIN Tab_Ergebnis e ON e.ID = d.ID_Ergebnis " +
                "WHERE e.ID_Projekt = ?", new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture));
            ErgebnisModel erg = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Equal(2, erg.KaelteDezentral.Count);
            for (int i = 0; i < 2; i++)
            {
                ErgebnisKaelteDezentralModel gespeichert = erg.KaelteDezentral[i];
                SimulationKaeltebedarf.DezentraleZeile gerechnet = k.DezentraleZeilen[i];
                Assert.Equal(gerechnet.Profil ? KaeltebedarfSplitAbrechnungSchema.QUELLE_PROFIL : KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG,
                             gespeichert.Quelle);
                Assert.Equal(gerechnet.IdZuordnung, gespeichert.ID_Zuordnung);
                Assert.Equal(gerechnet.Bezeichner, gespeichert.Bezeichner);
                Assert.Equal(Math.Round(gerechnet.KaelteKwh / 1000.0, 2), gespeichert.Kaelte_MWh, 9);
                Assert.Equal(Math.Round(gerechnet.StromKwh / 1000.0, 2), gespeichert.Strom_MWh, 9);
                Assert.Equal(kuehl, gespeichert.Kuehl_CarrierId);
                Assert.Equal(gerechnet.EigenerZaehler, gespeichert.Kuehl_EigenerZaehler);
            }
            Assert.Equal(KaeltebedarfSplitAbrechnungSchema.QUELLE_LASTGANG, erg.KaelteDezentral[0].Quelle);
            Assert.True(erg.KaelteDezentral[1].Kuehl_EigenerZaehler == true);

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
            // K6b-N: je Split-Zeile eine kurze Zeile auf die Abnehmerspalte, dann die Sammelzeile zum Strom (beide Zeilen auf
            // demselben abweichenden Träger); der Tooltip trägt den vollen Text mit EER-Weg, Träger und Zähler.
            string E(double w) => w.ToString("0.0#", CultureInfo.CurrentCulture);
            string zeile = WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_ZEILE;
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIM_SCHEMA_ABNEHMER_KAELTE_DEZENTRAL, knoten.Titel);
            Assert.Equal(3, knoten.Zeilen.Count);
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, zeile, KUEHLRAUM, E(3.0)), knoten.Zeilen.Take(2));
            string gekuerzt = Assert.Single(knoten.Zeilen.Take(2), x => x.StartsWith("K6b_K", StringComparison.Ordinal));
            Assert.EndsWith("…" + string.Format(CultureInfo.CurrentCulture, zeile, "", E(3.0) + "–" + E(4.0)), gekuerzt);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture,
                                       WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_STROM_TRAEGER, 1), knoten.Zeilen[2]);
            int breite = SchemaLayout.SPALTEN_BREITE[(int)SchemaModell.Knotenart.Abnehmer] - 2 * SchemaLayout.KNOTEN_RAND;
            Assert.All(knoten.Zeilen, x => Assert.True(SchemaLayout.TextBreite(x) <= breite, x));
            Assert.Contains(string.Format(CultureInfo.CurrentCulture, zeile, LASTGANG,
                                          string.Format(CultureInfo.CurrentCulture,
                                                        WindowsFormsApplication1.MyResource.Resource.SIMKONF_KAELTE_DEZ_EER_LINEAR,
                                                        E(3.0), "35", E(4.0), "25")), knoten.Hinweis);
            string traeger = WindowsFormsApplication1.MyResource.Resource.KONF_KS_DEZENTRAL_TRAEGER;
            Assert.Equal(2, knoten.Hinweis.Split(traeger.Substring(0, traeger.IndexOf('{'))).Length - 1);

            // Projektbeschreibung: Profile und Split-Zeilen mit EER-Weg und Träger.
            ProjektDetails d = ProjektDetails.Lade(PROJEKT);
            Assert.Equal(1, d.KaelteProfilZeilen);
            Assert.Equal(12.0, d.KaelteProfilSummeMwh, 9);
            Assert.Equal(2, d.KaelteDezentral.Count);
            Assert.Contains(d.KaelteDezentral, x => x.Bezeichner == KUEHLRAUM && x.Eer.Length > 0 && x.Traeger.Length > 0);
        }
    }
}
