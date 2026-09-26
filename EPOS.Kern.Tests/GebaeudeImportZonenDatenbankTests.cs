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
    /// <b>Stufe G6c, Welle C — der Durchgang des Zuordnungsdialogs mit mehreren Zonen</b> gegen die
    /// Arbeitskopie der Testdatenbank: das Zonenhaus (<c>ifc4_zonen.ifc</c>) über den Importweg des
    /// Gebäudedialogs mit der Regel je Geschoss (Z4) und dem Schalter „Als Zonen mit Bauteilen
    /// übernehmen" → vorbelegter Editor → neue Projektzeile → Speichern der Gebäudeliste. Danach trägt die
    /// Projektkopie drei Zonen samt Trennflächen auf die endgültigen Nachbarzonen, die Quelle die Regel Z4,
    /// die Räume sind auf ihre Zone gepaart — alles in dem Vorgang, der Kopie, Herkunft und
    /// Baustoff-Zuordnungen schreibt —, und die Mehrzonenrechnung aus G6b rechnet das Gebäude.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeImportZonenDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private const int KLASSE_E = 4;
        private const string PROBE = "ifc4_zonen.ifc";

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportZonenDatenbankTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        private static long Zeilen(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        private static int Kopie(int idZ)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Gebaeude WHERE ID_ProjektGebaeude = ? AND ID_Projekt = ?",
                new DbParam("@z", idZ), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        [Fact]
        public async Task Das_Zonenhaus_je_Geschoss_wird_mit_drei_Zonen_und_Trennflaechen_gespeichert_und_gerechnet()
        {
            if (!_db.Vorhanden) return;
            const string NAME = "Zonenhaus G6c";
            long zonenVorher = Zeilen(ZonenSchema.TAB_ZONE);

            // Der Weg des Gebäudedialogs: Importieren… → Zuordnung (Klasse, Regel Z4, Schalter) → OK.
            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = ((Func<GebaeudeImportweg>)gaben["ImportGaben"])();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(PROBE), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, KLASSE_E, Keine, Zonenregel: "Z4"));
            Assert.Equal("Z4", stand.Zonierung!.Regel);
            Assert.False(stand.Zonierung.Einzonig);
            Assert.True(stand.Bauteile!.Moeglich, stand.Bauteile.Ablehnung);

            var ergebnis = new GebaeudeImportErgebnis(0, KLASSE_E, NAME, Keine, stand.Zeilen.ToList(), AlsZone: true, Zonenregel: "Z4");
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);
            Assert.Null(await ((Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"])(ergebnis));

            // Der Editor im Modus Neu: prüfen, ableiten, speichern; dann die neue Projektzeile.
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
            Z_ProjGebModel neu = modelle.Single(m => m.Gebaeudename == NAME);
            GebaeudeBauteilvorschlag v = neu.Importherkunft.Vorschlag;
            Assert.True(v.Mehrzonig);
            Assert.Equal("Z4", neu.Importherkunft.Quelle.Zonenregel);

            // Speichern der Gebäudeliste: Kopie, Zonen, Bauteile, Aufbauten und Herkunft in einem Vorgang.
            (bool ok, string meldung) = new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle);
            Assert.True(ok, meldung);
            int kopie = Kopie(neu.ID_Z);

            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(kopie).ToList();
            Assert.Equal(zonenVorher + 3, Zeilen(ZonenSchema.TAB_ZONE));
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, zonen.Select(z => z.Bezeichner));
            Assert.Equal(new[] { false, true, true }, zonen.Select(z => z.IstBeheizt));
            for (int i = 0; i < 3; i++) Assert.Equal(v.Zeilen.Count(z => z.Zone == i), zonen[i].Bauteile.Count);

            // Die Trennflächen zeigen auf die endgültigen Nachbarzonen.
            List<BauteilModel> trenn = zonen.SelectMany(z => z.Bauteile).Where(b => b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).ToList();
            Assert.NotEmpty(trenn);
            var ids = new HashSet<int>(zonen.Select(z => z.ID));
            Assert.All(trenn, b => Assert.Contains(b.ID_Nachbarzone!.Value, ids));
            BauteilModel decke = Assert.Single(zonen[1].Bauteile, b => b.Bezeichner == "Geschossdecke" && b.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE);
            Assert.Equal(zonen[2].ID, decke.ID_Nachbarzone);

            // Die Herkunft: die Quelle mit der Regel Z4, die Räume auf ihre Zone gepaart.
            var ctrl = new GebaeudeImportCtrl();
            ImportquelleModel q = Assert.Single(ctrl.LesenQuellen(kopie));
            Assert.Equal("Z4", q.Zonenregel);
            List<ImportzuordnungModel> paare = ctrl.LesenZuordnungen(q.ID);
            Assert.Equal(zonen.Select(z => z.ID).OrderBy(x => x),
                         paare.Where(p => p.ID_Zone.HasValue).Select(p => p.ID_Zone.Value).Distinct().OrderBy(x => x));

            // Der Lauf: die Mehrzonenrechnung (G6b).
            var gebaeude = new ProjektGebaeudeCtrl();
            gebaeude.ReadAll(PROJEKT);
            int index = gebaeude.items.FindIndex(g => g.ID_Gebaeude == kopie);
            Assert.True(index >= 0);
            ProjektGebaeudeModel item = gebaeude.items[index];
            item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            Assert.Equal(3, item.Zonen.Count);
            Assert.All(item.Zonen, z => Assert.Null(z.Lesefehler));
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
            var werte = new double[8760];
            SimulationProtokoll p0 = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(item, index, werte), string.Join(" | ", p0.Hinweise));
            Assert.True(p0.IstFehlerfrei, string.Join(" | ", p0.Hinweise));
            Assert.True(werte.Sum() > 0.0);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1} Zonen, {2} Bauteile, {3} Trennflächen, Summe der Stundenwerte {4:F0}",
                NAME, zonen.Count, zonen.Sum(z => z.Bauteile.Count), trenn.Count, werte.Sum()));
        }
    }
}
