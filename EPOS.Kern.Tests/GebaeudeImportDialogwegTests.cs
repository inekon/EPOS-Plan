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
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Stufe G4, Welle 4 — der GANZE Weg des Gebäudeimports über die Hüllen gegen die Arbeitskopie
    /// der Testdatenbank: Knopf „Importieren…" (<c>GebaeudeHuelle.Gaben</c> → <c>ImportGaben</c>) →
    /// Zuordnung → OK → vorbelegter Editor im Modus Neu → Speichern → neue Projektzeile mit
    /// Herkunftsschlüssel → Speichern der Gebäudeliste; dazu die benannte Absage bei einem schon
    /// vergebenen Namen und der Hinweis „schon importiert".
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeImportDialogwegTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const int ANDERES_PROJEKT = 1039;

        /// <summary>Die Projektkopie einer Zuordnung (<c>Tab_Gebaeude.ID_ProjektGebaeude</c>).</summary>
        private static int Kopie(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", idZ), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        // =============================================================================
        //  Der ganze Weg über die Hüllen
        // =============================================================================

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        /// <summary>Liest das Probenhaus über den Parametersatz eines Importwegs und ordnet es mit Klasse E zu.</summary>
        private static async Task<GebaeudeImportErgebnis> Einlesen(GebaeudeImportweg weg, string name)
        {
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, 4, Keine));
            return new GebaeudeImportErgebnis(0, 4, name, Keine, stand.Zeilen.ToList());
        }

        private static Task<string> Uebernehmen(GebaeudeImportweg weg, GebaeudeImportErgebnis e)
            => ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(e);

        /// <summary>
        /// Knopf „Importieren…" → Zuordnung → OK → vorbelegter Editor im Modus Neu → Speichern →
        /// neue Projektzeile mit Herkunftsschlüssel → OK des Gebäudedialogs: Katalogsatz, Projektkopie
        /// und Herkunft stehen; ein zweiter Import derselben Datei nennt das Gebäude.
        /// </summary>
        [Fact]
        public async Task Der_Import_im_Gebaeudedialog_legt_Katalogsatz_Zeile_und_Herkunft_an()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Importhaus Probe G4";
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            int vorher = modelle.Count;
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();

            // Der Zuordnungsdialog trägt das Profil beider Formate und den Schreibweg des Wirts.
            var profil = (GebaeudeImportProfilDaten)weg.Gaben["Profil"];
            Assert.Equal(GebaeudeImportProfil.DATEIFILTER_ALLE, profil.Dateifilter);
            Assert.Equal(GebaeudeImportProfil.HILFE_ZUORDNUNG, profil.HilfeSchluessel);
            Assert.True(weg.Gaben.ContainsKey("Uebernehmen"));
            Assert.Null(weg.EditorGaben());                           // vor dem OK kein Editor

            Assert.Null(await Uebernehmen(weg, await Einlesen(weg, NAME)));

            IReadOnlyDictionary<string, object> editor = weg.EditorGaben();
            Assert.NotNull(editor);
            Assert.Equal(GebaeudeKatalogModus.Neu, editor["Modus"]);
            var daten = (GebaeudeKatalogDaten)editor["Daten"];
            Assert.Equal(NAME, daten.Name);
            Assert.Equal(120.0, daten.WohnflaecheGesamt);
            Assert.Equal("Vorbelegt aus dem Import: Datei gbxml_haus_si.xml, Format gbXML.", editor["Vorbelegung"]);
            Assert.Null(weg.Aufnehmen());                             // ohne Speichern im Editor keine Zeile

            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)editor["Speichern"];
            Assert.True(speichern(daten, true, daten.Name).Erfolg);
            Assert.NotNull(new GebaeudeStammCtrl().Lies(NAME));       // der Katalogsatz

            GebaeudeProjektZeile zeile = weg.Aufnehmen();
            Assert.NotNull(zeile);
            Assert.Equal(NAME, zeile.Name);
            Assert.True(zeile.IdKatalog > 0);
            Assert.StartsWith("import-", zeile.Herkunftsschluessel);
            Assert.Null(weg.Aufnehmen());                             // einmal

            // Der Dialog nimmt die Zeile auf und meldet die Änderung — die Fachliste trägt die Herkunft.
            ((List<GebaeudeProjektZeile>)gaben["Zeilen"]).Add(zeile);
            ((Action)gaben["Geaendert"])();
            Assert.Equal(vorher + 1, modelle.Count);
            Z_ProjGebModel neu = modelle.Single(m => m.Gebaeudename == NAME);
            Assert.NotNull(neu.Importherkunft);
            Assert.Equal("gbxml_haus_si.xml", neu.Importherkunft.Quelle.Dateiname);
            Assert.Single(neu.Importherkunft.Paarungen);

            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            int kopie = Kopie(neu.ID_Z);
            Assert.Equal(zeile.IdKatalog, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Gebaeude_Stamm FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@g", kopie)), CultureInfo.InvariantCulture));
            ImportquelleModel q = Assert.Single(new GebaeudeImportCtrl().LesenQuellen(kopie));
            Assert.Equal("gbxml_haus_si.xml", q.Dateiname);

            // Ein zweiter Import derselben Datei im Projekt: der leise Hinweis nennt das Gebäude.
            var h = new GebaeudeImportHuelle(PROJEKT);
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)h.Gaben()["Lesen"];
            GebaeudeLesestand zweiter = await lesen(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None);
            Assert.StartsWith("Diese Datei ist im Projekt schon importiert: Gebäude „" + NAME + "“, ", zweiter.SchonImportiert);
            Assert.Contains(GebaeudeImportHuelle.Zeitpunkttext(q.Zeitpunkt), zweiter.SchonImportiert);

            // Ein anderes Projekt kennt die Datei nicht.
            var anders = new GebaeudeImportHuelle(ANDERES_PROJEKT);
            var lesenAnders = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)anders.Gaben()["Lesen"];
            Assert.Equal("", (await lesenAnders(GbxmlImportTests.Probe("gbxml_haus_si.xml"), null, CancellationToken.None)).SchonImportiert);
        }

        /// <summary>Ein Name, den der Katalog schon führt, ist die benannte Absage am OK des Zuordnungsdialogs.</summary>
        [Fact]
        public async Task Ein_vergebener_Name_haelt_den_Zuordnungsdialog_offen()
        {
            if (!_db.Vorhanden) return;
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();
            string vergeben = GebaeudeStammCtrl.Katalognamen()[0];

            string grund = await Uebernehmen(weg, await Einlesen(weg, vergeben));

            Assert.Equal(R.GEBK_MSG_NAME_VERGEBEN, grund);
            Assert.Null(weg.EditorGaben());
            Assert.Null(weg.Aufnehmen());
        }
    }
}
