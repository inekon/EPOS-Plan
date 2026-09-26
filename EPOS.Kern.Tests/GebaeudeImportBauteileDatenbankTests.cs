using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G4b, Welle B — der ganze Weg des Bauteilvorschlags</b> gegen die Arbeitskopie der
    /// Testdatenbank: Importprobe → Zuordnung mit Schalter „Als Zone mit Bauteilen übernehmen" →
    /// vorbelegter Editor → neue Projektzeile → Speichern der Gebäudeliste. Danach trägt die
    /// Projektkopie eine Zone mit den Zeilen des Vorschlags, <c>Tab_Importzuordnung</c> Paarungen für
    /// Gebäude, Zone, Bauteile und Aufbauten, und <c>HeizwaermeEinesGebaeudes</c> rechnet den
    /// Bauteilweg. Gegenprobe ohne Schalter (keine Zone) und die Fehlerfälle (nichts geschrieben).
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportBauteileDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportBauteileDatenbankTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const int KLASSE_E = 4;

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        private static long Zeilen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilenzahl aller Tabellen, in die der Weg schreibt.</summary>
        private static long[] Bestand()
            => new[]
            {
                Zeilen("Z_ProjektGebaeude"), Zeilen("Tab_Gebaeude"), Zeilen(ZonenSchema.TAB_ZONE), Zeilen(ZonenSchema.TAB_BAUTEIL),
                Zeilen(BauteilaufbauSchema.TAB_AUFBAU), Zeilen(BauteilaufbauSchema.TAB_SCHICHT),
                Zeilen(ImportzuordnungSchema.TAB_QUELLE), Zeilen(ImportzuordnungSchema.TAB_ZUORDNUNG),
            };

        /// <summary>Die Projektkopie einer Zuordnung (<c>Tab_Gebaeude.ID_ProjektGebaeude</c>).</summary>
        private static int Kopie(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", idZ), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        /// <summary>
        /// Der Weg des Gebäudedialogs: Knopf „Importieren…" → Zuordnung (Klasse, Schalter) → OK →
        /// vorbelegter Editor → Speichern → neue Projektzeile; zurück die Fachliste und die neue Zeile.
        /// </summary>
        private static async Task<(List<Z_ProjGebModel> Modelle, Z_ProjGebModel Neu, GebaeudeImportStand Stand)> Importieren(
            string probe, int? klasse, string name, bool alsZone)
        {
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();

            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, klasse, Keine, Zonenregel: Einzonenregel.Fuer(probe)));
            Assert.NotNull(stand.Bauteile);

            var ergebnis = new GebaeudeImportErgebnis(0, klasse, name, Keine, stand.Zeilen.ToList(), alsZone,
                                                      Zonenregel: Einzonenregel.Fuer(probe));
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(ergebnis));

            // Der Editor im Modus Neu: prüfen, vor dem Speichern ableiten (Bauweise, obere Raumtemperatur), speichern.
            IReadOnlyDictionary<string, object> editor = weg.EditorGaben();
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden((GebaeudeKatalogDaten)editor["Daten"], neu: true);
            GebaeudePruefbefund befund = arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
            Assert.True(befund == null, befund?.Meldung);
            arbeit.Ableiten();
            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)editor["Speichern"];
            Assert.True(speichern(arbeit.Stand, true, arbeit.Stand.Name).Erfolg);
            GebaeudeProjektZeile zeile = weg.Aufnehmen();
            Assert.NotNull(zeile);
            ((List<GebaeudeProjektZeile>)gaben["Zeilen"]).Add(zeile);
            ((Action)gaben["Geaendert"])();
            return (modelle, modelle.Single(m => m.Gebaeudename == name), stand);
        }

        // =============================================================================
        //  Mit Schalter: Zone, Bauteile, Aufbauten, Herkunft — und der Bauteilweg rechnet
        // =============================================================================

        [Fact]
        public async Task Mit_Schalter_traegt_das_Gebaeude_eine_Zone_mit_Bauteilen_und_rechnet_den_Bauteilweg()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Importhaus Bauteile G4b";
            (List<Z_ProjGebModel> modelle, Z_ProjGebModel neu, GebaeudeImportStand stand) =
                await Importieren("gbxml_haus_si.xml", KLASSE_E, NAME, alsZone: true);
            GebaeudeBauteilvorschlag v = neu.Importherkunft.Vorschlag;
            Assert.NotNull(v);
            Assert.Equal(stand.Bauteile.Zeilen.Count, v.Zeilen.Count);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            int kopie = Kopie(neu.ID_Z);

            // Die Zone mit den Zeilen des Vorschlags.
            ZoneModel zone = Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(kopie));
            Assert.Equal(v.Zeilen.Count, zone.Bauteile.Count);
            Assert.Equal(v.Zeilen.Select(z => (z.Bauteil.Bezeichner, z.Bauteil.Bauteilart, z.Bauteil.Flaeche, z.Bauteil.Randbedingung)),
                         zone.Bauteile.Select(b => (b.Bezeichner, b.Bauteilart, b.Flaeche, b.Randbedingung)));
            Assert.Equal(120.0, zone.Nutzflaeche);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, zone.Herkunft);
            List<BauteilaufbauModel> aufbauten = new BauteilaufbauCtrl().LesenJeProjekt(PROJEKT)
                .Where(a => a.Quelle == "gbxml_haus_si.xml").ToList();
            Assert.Equal(v.Aufbauten.Count, aufbauten.Count);

            // Die Herkunft: eine Quelle, Paarungen für Gebäude, Räume → Zone, Flächen → Bauteile, Konstruktionen → Aufbauten.
            var ctrl = new GebaeudeImportCtrl();
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(kopie));
            List<ImportzuordnungModel> paare = ctrl.LesenZuordnungen(q.ID);
            ImportzuordnungModel geb = Assert.Single(paare, p => p.ID_Gebaeude.HasValue);
            Assert.Equal(kopie, geb.ID_Gebaeude);
            Assert.Equal("Building", geb.Quelltyp);
            Assert.Equal(3, paare.Count(p => p.ID_Zone == zone.ID && p.Quelltyp == "Space"));
            var bauteilIds = new HashSet<int>(zone.Bauteile.Select(b => b.ID));
            List<ImportzuordnungModel> bauteilPaare = paare.Where(p => p.ID_Bauteil.HasValue).ToList();
            Assert.Equal(v.Zeilen.Count(z => z.Quelltyp != null), bauteilPaare.Count);
            Assert.All(bauteilPaare, p => Assert.Contains(p.ID_Bauteil.Value, bauteilIds));
            var aufbauIds = new HashSet<int>(aufbauten.Select(a => a.ID));
            List<ImportzuordnungModel> aufbauPaare = paare.Where(p => p.ID_Aufbau.HasValue).ToList();
            Assert.Equal(v.Aufbauten.Count, aufbauPaare.Count);
            Assert.All(aufbauPaare, p => Assert.Contains(p.ID_Aufbau.Value, aufbauIds));
            Assert.Equal(1 + 3 + bauteilPaare.Count + aufbauPaare.Count, paare.Count);

            // Das Zielfeld Innenflächenfaktor steht an der Projektkopie (über Editor und Katalogsatz).
            object faktor = DataRepository.ExecuteScalar("SELECT Innenflaechenfaktor FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@g", kopie));
            BauteilvorschlagProbe.Nah(145.0 / 120.0, Convert.ToDouble(faktor, CultureInfo.InvariantCulture));

            // Der Lauf: HeizwaermeEinesGebaeudes rechnet die Zone über den Bauteilweg.
            var gebaeude = new ProjektGebaeudeCtrl();
            gebaeude.ReadAll(PROJEKT);
            int index = gebaeude.items.FindIndex(g => g.ID_Gebaeude == kopie);
            Assert.True(index >= 0);
            ProjektGebaeudeModel item = gebaeude.items[index];
            GebaeudeZonensatz satz = Assert.Single(item.Zonen);
            Assert.Null(satz.Lesefehler);
            Assert.Equal(v.Zeilen.Count, satz.Bauteile.Count);

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            var werte = new double[8760];
            SimulationProtokoll p0 = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(item, index, werte));
            Assert.True(p0.IstFehlerfrei, string.Join(" | ", p0.Hinweise));
            Assert.True(werte.Sum() > 0.0);
            KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
            GebaeudeModellEingang eingang = GebaeudeModellEingang.Bauen(item, k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad,
                                                                        GebaeudeKlimaweg.ZEITBEZUG_VORGABE, sim.KuehlbetriebProjekt, null);
            Assert.True(eingang.Bauteilweg);
            Assert.Equal(Gruppenweg.Bauteilweg, eingang.Parameter.WegInnen);
            Assert.Equal(145.0, eingang.Parameter.A_IW_M2, 9);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1} Bauteile, {2} Aufbauten, {3} Paarungen, Jahresheizwärme {4:F3} MWh",
                NAME, zone.Bauteile.Count, aufbauten.Count, paare.Count, werte.Sum() / 1000.0));
        }

        [Fact]
        public async Task Ohne_Schalter_bleibt_es_beim_Summenweg_ohne_Zone()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Importhaus Summen G4b";
            long zonenVorher = Zeilen(ZonenSchema.TAB_ZONE), aufbautenVorher = Zeilen(BauteilaufbauSchema.TAB_AUFBAU);
            (List<Z_ProjGebModel> modelle, Z_ProjGebModel neu, _) = await Importieren("gbxml_haus_si.xml", KLASSE_E, NAME, alsZone: false);
            Assert.Null(neu.Importherkunft.Vorschlag);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            int kopie = Kopie(neu.ID_Z);
            Assert.Empty(new GebaeudeZonenCtrl().LesenJeGebaeude(kopie));
            Assert.Equal(zonenVorher, Zeilen(ZonenSchema.TAB_ZONE));
            Assert.Equal(aufbautenVorher, Zeilen(BauteilaufbauSchema.TAB_AUFBAU));
            ImportquelleModel q = Assert.Single(new GebaeudeImportCtrl().LesenQuellen(kopie));
            ImportzuordnungModel geb = Assert.Single(new GebaeudeImportCtrl().LesenZuordnungen(q.ID));
            Assert.Equal(kopie, geb.ID_Gebaeude);

            // Die Summenfelder stehen wie bisher an der Kopie.
            object aw = DataRepository.ExecuteScalar("SELECT \"Flaeche_Außenwand\" FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@g", kopie));
            Assert.True(Convert.ToDouble(aw, CultureInfo.InvariantCulture) > 0.0);
        }

        // =============================================================================
        //  Fehlerfälle: nichts wird geschrieben
        // =============================================================================

        /// <summary>Eine neue Zeile der Liste aus dem Katalogsatz der ersten Projektzeile, mit der Herkunft.</summary>
        private static Z_ProjGebModel NeueZeile(List<Z_ProjGebModel> liste, GebaeudeImportHerkunft herkunft)
        {
            Z_ProjGebModel vorlage = liste[0];
            return new Z_ProjGebModel
            {
                ID_Z = 100000,
                ID_Projekt = PROJEKT,
                ID_Gebaeude_Stamm = vorlage.ID_Gebaeude_Stamm,
                Gebaeudename = vorlage.Gebaeudename,
                Wohnflaeche = 100,
                Einheit = "Wohnfläche [m²]",
                Jahresnutzungsgrad = 1,
                Importherkunft = herkunft
            };
        }

        /// <summary>
        /// Scheitert der Vorschlag mitten im Schreiben (ein Bauteil zeigt auf einen Aufbau, den der
        /// Vorschlag nicht führt), wird nichts geschrieben: weder Zuordnung noch Kopie, weder Zone noch
        /// Bauteile, Aufbauten oder Herkunft; die Meldung nennt den Grund.
        /// </summary>
        [Fact]
        public void Scheitert_der_Vorschlag_wird_nichts_geschrieben()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportSatz satz = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeBauteilvorschlag kaputt = BauteilvorschlagProbe.Vorschlag("gbxml_haus_si.xml");
            kaputt.Zone.Bauteile[0].ID_Aufbau = -999;
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, new GebaeudeImportHerkunft(satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz), kaputt));
            liste.Add(neu);
            long[] vorher = Bestand();

            var wizard = new WizardCtrl();
            (bool ok, string meldung) = wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(ok);
            Assert.StartsWith(WindowsFormsApplication1.MyResource.Resource.GEB_MSG_LISTE_NICHT_GESPEICHERT, meldung);
            Assert.Contains("Die Bauteile wurden nicht geschrieben", meldung);
            Assert.Contains("-999", wizard.Herkunftsfehler);
            Assert.Equal(100000, neu.ID_Z);
            Assert.Equal(vorher, Bestand());
        }

        /// <summary>
        /// Scheitert erst die Herkunft NACH dem Vorschlag (ein Hash außerhalb der Hausregel), rollt
        /// auch die schon geschriebene Zone zurück — der Vorschlag und die Herkunft gehen zusammen oder
        /// gar nicht.
        /// </summary>
        [Fact]
        public void Scheitert_die_Herkunft_nach_dem_Vorschlag_bleibt_keine_Zone()
        {
            if (!_db.Vorhanden) return;
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag("gbxml_haus_si.xml");
            Assert.False(v.Abgelehnt);
            GebaeudeQuelle kaputt = ImportzuordnungSchemaRegelTests.Quelle(hash: new string('A', 64));
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, new GebaeudeImportHerkunft(kaputt, Array.Empty<GebaeudeQuellzuordnung>(), v));
            liste.Add(neu);
            long[] vorher = Bestand();

            var wizard = new WizardCtrl();
            (bool ok, _) = wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.HERKUNFT_MSG_HASH, wizard.Herkunftsfehler);
            Assert.Equal(vorher, Bestand());
        }
    }
}
