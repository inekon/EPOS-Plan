using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Baustoffzuordnung des Importdialogs über die Datenbank</b> (Namensabgleich, Welle 2) — der
    /// ganze Weg des Gebäudedialogs gegen die Arbeitskopie der Testdatenbank: Import des Probenhauses mit
    /// Materialnamen, eine Handzuordnung für „Fußbodenaufbau", OK, vorbelegter Editor, neue Projektzeile,
    /// Speichern der Gebäudeliste. Vorher ist nichts geschrieben; danach trägt
    /// <c>Tab_Baustoffzuordnung</c> die Zeile, die Zone die Aufbauten mit Herkunft <c>KATALOG</c>, und ein
    /// zweiter Import im selben Projekt trifft den Namen über die gemerkte Zuordnung (N7). Scheitert
    /// etwas beim Speichern, wird nichts geschrieben — auch keine Zuordnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportBaustoffzuordnungDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportBaustoffzuordnungDatenbankTests(ITestOutputHelper aus)
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
        private const int ZEMENTESTRICH = 5;
        private const string MATERIALHAUS = "ifc4_haus_materialnamen.ifc";

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        private static long Zeilen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        /// <summary>Die Zeilenzahl aller Tabellen, in die der Weg schreibt — samt den gemerkten Zuordnungen.</summary>
        private static long[] Bestand()
            => new[]
            {
                Zeilen("Z_ProjektGebaeude"), Zeilen("Tab_Gebaeude"), Zeilen(ZonenSchema.TAB_ZONE), Zeilen(ZonenSchema.TAB_BAUTEIL),
                Zeilen(BauteilaufbauSchema.TAB_AUFBAU), Zeilen(BauteilaufbauSchema.TAB_SCHICHT),
                Zeilen(ImportzuordnungSchema.TAB_QUELLE), Zeilen(ImportzuordnungSchema.TAB_ZUORDNUNG),
                Zeilen(BaustoffabgleichSchema.TAB_ZUORDNUNG), Zeilen(BaustoffSchema.TAB_PROJEKT),
            };

        /// <summary>Die gemerkten Zuordnungen des Projekts: Materialname → Baustoff.</summary>
        private static Dictionary<string, int> Gemerkt()
            => BaustoffabgleichCtrl.LesenJeProjekt(PROJEKT).ToDictionary(z => z.Materialname, z => z.IdBaustoff, StringComparer.Ordinal);

        private static int Kopie(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", idZ), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        /// <summary>Liest die Probe über einen neuen Importweg des Gebäudedialogs und ordnet sie mit Klasse E zu.</summary>
        private static Task<(IReadOnlyDictionary<string, object> Gaben, GebaeudeImportweg Weg, GebaeudeImportStand Stand)> Lesen(
            List<Z_ProjGebModel> modelle, IReadOnlyDictionary<string, int?> zuordnungen)
            => Lesen(GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false), zuordnungen);

        /// <summary>Wie oben, über den Parametersatz eines schon offenen Gebäudedialogs — ein weiterer Import derselben Liste.</summary>
        private static async Task<(IReadOnlyDictionary<string, object> Gaben, GebaeudeImportweg Weg, GebaeudeImportStand Stand)> Lesen(
            IReadOnlyDictionary<string, object> gaben, IReadOnlyDictionary<string, int?> zuordnungen)
        {
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(MATERIALHAUS), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, KLASSE_E, Keine, null, zuordnungen,
                                                                               Einzonenregel.Fuer(MATERIALHAUS)));
            Assert.NotNull(stand.Baustoffe);
            return (gaben, weg, stand);
        }

        /// <summary>
        /// Der Weg des Gebäudedialogs bis zur neuen Projektzeile: Zuordnung mit den Baustoffzuordnungen
        /// → OK → vorbelegter Editor → Speichern des Katalogsatzes → neue Zeile. Die Gebäudeliste ist
        /// danach noch NICHT gespeichert.
        /// </summary>
        private static async Task<(List<Z_ProjGebModel> Modelle, Z_ProjGebModel Neu, GebaeudeImportStand Stand)> Importieren(
            string name, IReadOnlyDictionary<string, int?> zuordnungen, bool alsZone = true)
        {
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            (_, GebaeudeImportStand stand) = await Aufnehmen(GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false), name, zuordnungen, alsZone);
            return (modelle, modelle.Single(m => m.Gebaeudename == name), stand);
        }

        /// <summary>Derselbe Weg über den Parametersatz eines offenen Gebäudedialogs; gibt die neue Anzeigezeile.</summary>
        private static async Task<(GebaeudeProjektZeile Zeile, GebaeudeImportStand Stand)> Aufnehmen(
            IReadOnlyDictionary<string, object> gaben, string name, IReadOnlyDictionary<string, int?> zuordnungen, bool alsZone = true)
        {
            (_, GebaeudeImportweg weg, GebaeudeImportStand stand) = await Lesen(gaben, zuordnungen);

            var ergebnis = new GebaeudeImportErgebnis(0, KLASSE_E, name, Keine, stand.Zeilen.ToList(), alsZone, zuordnungen,
                                                      Einzonenregel.Fuer(MATERIALHAUS));
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(ergebnis));

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
            return (zeile, stand);
        }

        private static Dictionary<string, int?> Fussboden(int? id) => new(StringComparer.Ordinal) { ["fussbodenaufbau"] = id };

        // =============================================================================
        //  Handzuordnung → Speichern → gemerkt; der zweite Import trifft über N7
        // =============================================================================

        [Fact]
        public async Task Die_Handzuordnung_wird_mit_der_Gebaeudeliste_gemerkt_und_ein_zweiter_Import_trifft_sie()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Importhaus Baustoffe";
            Assert.Empty(Gemerkt());

            (List<Z_ProjGebModel> modelle, Z_ProjGebModel neu, GebaeudeImportStand stand) = await Importieren(NAME, Fussboden(ZEMENTESTRICH));
            GebaeudeMaterialzeileDaten fb = stand.Baustoffe!.Zeilen.Single(z => z.Name == "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, (int?)ZEMENTESTRICH, true, false), (fb.AbgleichSchluessel, fb.IdBaustoff, fb.Vorgemerkt, fb.Gemerkt));
            Assert.Equal(ZEMENTESTRICH, neu.Importherkunft.Baustoffzuordnungen["fussbodenaufbau"]);
            GebaeudeBauteilvorschlag v = neu.Importherkunft.Vorschlag;
            Assert.Equal(7, v.Aufbauten.Count);

            // Vor dem Speichern der Liste ist nichts gemerkt (Hausmuster OK/Abbrechen).
            Assert.Empty(Gemerkt());

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            Assert.Equal(new Dictionary<string, int> { ["fussbodenaufbau"] = ZEMENTESTRICH }, Gemerkt());

            // Die Zone trägt die Aufbauten mit Herkunft KATALOG — auch die Kellerdecke.
            int kopie = Kopie(neu.ID_Z);
            ZoneModel zone = Assert.Single(new GebaeudeZonenCtrl().LesenJeGebaeude(kopie));
            List<BauteilaufbauModel> aufbauten = new BauteilaufbauCtrl().LesenJeProjekt(PROJEKT).Where(a => a.Quelle == MATERIALHAUS).ToList();
            Assert.Equal(7, aufbauten.Count);
            Assert.All(aufbauten, a => Assert.Equal(DbWerte.HERKUNFT_KATALOG, a.Herkunft));
            BauteilModel kellerdecke = zone.Bauteile.Single(b => b.Bezeichner == "Kellerdecke");
            Assert.True(kellerdecke.ID_Aufbau.HasValue);
            Assert.Contains(kellerdecke.ID_Aufbau.Value, aufbauten.Select(a => a.ID));

            // Der zweite Import im selben Projekt: „Fußbodenaufbau" trifft über die gemerkte Zuordnung.
            (_, _, GebaeudeImportStand zweiter) = await Lesen(Z_ProjGebCtrl.LiesProjekt(PROJEKT), null);
            GebaeudeMaterialzeileDaten n7 = zweiter.Baustoffe!.Zeilen.Single(z => z.Name == "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, (int?)ZEMENTESTRICH, true, false, false),
                         (n7.AbgleichSchluessel, n7.IdBaustoff, n7.Gemerkt, n7.Vorgemerkt, n7.OhneTreffer));
            Assert.Equal("17 von 20 zugeordnet, 0 ohne Treffer", zweiter.Baustoffe.Zusammenfassung);
            _aus.WriteLine(NAME + ": " + zone.Bauteile.Count + " Bauteile, " + aufbauten.Count + " Aufbauten; zweiter Import: " + zweiter.Baustoffe.Zusammenfassung);
        }

        [Fact]
        public async Task Ohne_Bauteilschalter_wird_die_Zuordnung_trotzdem_gemerkt()
        {
            if (!_db.Vorhanden) return;
            long zonenVorher = Zeilen(ZonenSchema.TAB_ZONE);
            (List<Z_ProjGebModel> modelle, Z_ProjGebModel neu, _) = await Importieren("Importhaus Baustoffe Summen", Fussboden(ZEMENTESTRICH), alsZone: false);
            Assert.Null(neu.Importherkunft.Vorschlag);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            Assert.Equal(zonenVorher, Zeilen(ZonenSchema.TAB_ZONE));
            Assert.Equal(new Dictionary<string, int> { ["fussbodenaufbau"] = ZEMENTESTRICH }, Gemerkt());
        }

        [Fact]
        public async Task Das_Entfernen_einer_gemerkten_Zuordnung_gilt_erst_mit_dem_Speichern()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Fußbodenaufbau", ZEMENTESTRICH).Ok);
            Assert.True(BaustoffabgleichCtrl.Merken(PROJEKT, "Gipsputz", 1).Ok);

            (List<Z_ProjGebModel> modelle, Z_ProjGebModel neu, GebaeudeImportStand stand) = await Importieren("Importhaus Baustoffe entfernt", Fussboden(null));
            GebaeudeMaterialzeileDaten fb = stand.Baustoffe!.Zeilen.Single(z => z.Name == "Fußbodenaufbau");
            Assert.True(fb.OhneTreffer);
            Assert.True(fb.Gemerkt);
            Assert.Equal(GebaeudeAbgleichSchluessel.EigeneZuordnung, stand.Baustoffe.Zeilen.Single(z => z.Name == "Gipsputz").AbgleichSchluessel);
            Assert.Equal(2, Gemerkt().Count);   // noch nichts vergessen

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            Assert.Equal(new Dictionary<string, int> { ["gipsputz"] = 1 }, Gemerkt());
            Assert.Equal(6, neu.Importherkunft.Vorschlag.Aufbauten.Count);
        }

        /// <summary>
        /// Eine importierte Zeile geht vor dem Speichern wieder aus der Liste: Ein weiterer Import desselben
        /// Dialogs sieht ihre Zuordnungen nicht mehr als vorgemerkt — die vorgemerkten Zuordnungen folgen den
        /// Zeilen, die noch in der Liste stehen —, und das Speichern der Liste merkt keine.
        /// </summary>
        [Fact]
        public async Task Die_Zuordnungen_einer_vor_dem_Speichern_entfernten_Importzeile_gelten_nicht_mehr()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            var zeilen = (List<GebaeudeProjektZeile>)gaben["Zeilen"];
            (GebaeudeProjektZeile importiert, _) = await Aufnehmen(gaben, "Importhaus Baustoffe wieder entfernt", Fussboden(ZEMENTESTRICH));

            // Solange die Zeile in der Liste steht, trifft ein weiterer Import „Fußbodenaufbau" über ihre Zuordnung.
            (_, _, GebaeudeImportStand mitZeile) = await Lesen(gaben, null);
            GebaeudeMaterialzeileDaten vor = mitZeile.Baustoffe!.Zeilen.Single(z => z.Name == "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, (int?)ZEMENTESTRICH, true, false),
                         (vor.AbgleichSchluessel, vor.IdBaustoff, vor.Gemerkt, vor.OhneTreffer));

            // „Aus dem Projekt entfernen" vor dem Speichern: derselbe Weg wie der Dialog.
            Assert.True(zeilen.Remove(importiert));
            ((Action)gaben["Geaendert"])();

            (_, _, GebaeudeImportStand ohneZeile) = await Lesen(gaben, null);
            GebaeudeMaterialzeileDaten nach = ohneZeile.Baustoffe!.Zeilen.Single(z => z.Name == "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.Ohne, (int?)null, false, true),
                         (nach.AbgleichSchluessel, nach.IdBaustoff, nach.Gemerkt, nach.OhneTreffer));
            Assert.Equal("16 von 20 zugeordnet, 1 ohne Treffer", ohneZeile.Baustoffe.Zusammenfassung);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            Assert.Empty(Gemerkt());
            Assert.DoesNotContain(modelle, m => m.Importherkunft != null);
        }

        // =============================================================================
        //  Fehlerfälle: nichts wird geschrieben — auch keine Zuordnung
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
        /// Scheitert die Herkunft (ein Hash außerhalb der Hausregel), nachdem die Zuordnungen geschrieben
        /// sind, rollt auch die Zuordnung zurück — mit Zone, Aufbauten, Kopie und Zeile.
        /// </summary>
        [Fact]
        public void Scheitert_die_Herkunft_wird_auch_keine_Zuordnung_geschrieben()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen(MATERIALHAUS);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, null,
                new Baustoffabgleich(BaustoffabgleichDaten.AusSaat(new[] { new BaustoffNamenzuordnung("fussbodenaufbau", ZEMENTESTRICH) })));
            Assert.False(v.Abgelehnt);
            GebaeudeQuelle kaputt = ImportzuordnungSchemaRegelTests.Quelle(hash: new string('A', 64));
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, new GebaeudeImportHerkunft(kaputt, Array.Empty<GebaeudeQuellzuordnung>(), v, Fussboden(ZEMENTESTRICH)));
            liste.Add(neu);
            long[] vorher = Bestand();

            var wizard = new WizardCtrl();
            (bool ok, _) = wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.HERKUNFT_MSG_HASH, wizard.Herkunftsfehler);
            Assert.Equal(vorher, Bestand());
            Assert.Empty(Gemerkt());
            Assert.Equal(100000, neu.ID_Z);
        }

        /// <summary>
        /// Scheitert die Zuordnung selbst (ein Katalogbaustoff, den es nicht gibt), wird nichts
        /// geschrieben — weder Zeile noch Kopie, Zone oder Herkunft; die Meldung nennt den Grund.
        /// </summary>
        [Fact]
        public void Ein_unbekannter_Baustoff_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            GebaeudeImportSatz satz = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag("gbxml_haus_si.xml");
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            Z_ProjGebModel neu = NeueZeile(liste, new GebaeudeImportHerkunft(satz.Quelle, GebaeudeImportCtrl.Einzonenpaarungen(satz), v,
                                                                             Fussboden(999999)));
            liste.Add(neu);
            long[] vorher = Bestand();

            var wizard = new WizardCtrl();
            (bool ok, string meldung) = wizard.Speichere_Projekt_Gebaeudeliste(PROJEKT, liste);

            Assert.False(ok);
            string erwartet = string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_STOFF, 999999);
            Assert.Equal(erwartet, wizard.Herkunftsfehler);
            Assert.Contains(erwartet, meldung, StringComparison.Ordinal);
            Assert.Equal(vorher, Bestand());
            Assert.Empty(Gemerkt());
        }
    }
}
