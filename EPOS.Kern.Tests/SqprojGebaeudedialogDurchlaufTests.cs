using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// <b>Der Weg „nur Projektdatei“ im Gebäudedialog eines Projekts</b> gegen die Arbeitskopie der Testdatenbank, je
    /// Schritt mit Zeitgrenze: Importweg des Dialogs → Lesen der Projektdatei → Zuordnen mit dem Schritt PROJEKTDATEI →
    /// Prüfen → OK (Übernehmen) → vorbelegter Katalogeditor → Speichern des Katalogsatzes → neue Projektzeile → Speichern der
    /// Gebäudeliste. Kein Schritt darf hängen oder werfen.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojGebaeudedialogDurchlaufTests : IDisposable
    {
        private const int PROJEKT = 1007;
        private static readonly TimeSpan GRENZE = TimeSpan.FromSeconds(60);

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;
        private readonly List<string> _dateien = new List<string>();

        public SqprojGebaeudedialogDurchlaufTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        /// <summary>Führt einen Schritt mit Zeitgrenze aus und schreibt seine Dauer.</summary>
        private async Task<T> Schritt<T>(string name, Func<Task<T>> weg)
        {
            var uhr = Stopwatch.StartNew();
            Task<T> lauf = Task.Run(weg);
            Task fertig = await Task.WhenAny(lauf, Task.Delay(GRENZE));
            Assert.True(ReferenceEquals(fertig, lauf), "Der Schritt „" + name + "“ hängt (über " + GRENZE.TotalSeconds + " s).");
            T wert = await lauf;
            _aus.WriteLine(name + ": " + uhr.ElapsedMilliseconds + " ms");
            return wert;
        }

        private Task<T> Schritt<T>(string name, Func<T> weg) => Schritt(name, () => Task.FromResult(weg()));

        [Fact]
        public async Task Nur_Projektdatei_laeuft_im_Gebaeudedialog_bis_zur_gespeicherten_Gebaeudeliste()
        {
            if (!_db.Vorhanden) return;
            string pfad = SqprojImportTests.Sporthaus().Schreiben(SqprojProbenErzeuger.TempPfad("gebaeudedialog_durchlauf"));
            _dateien.Add(pfad);

            List<Z_ProjGebModel> modelle = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            IReadOnlyDictionary<string, object> gaben = GebaeudeHuelle.Gaben(PROJEKT, "", modelle, wizard: false);
            GebaeudeImportweg weg = await Schritt("Importweg", () => ((Func<GebaeudeImportweg>)gaben["ImportGaben"])());

            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)weg.Gaben["Lesen"];
            GebaeudeLesestand gelesen = await Schritt("Lesen", () => lesen(pfad, null, CancellationToken.None));
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            Assert.Equal(GebaeudeImportWeg.NurProjektdatei, gelesen.Kopf!.Weg);

            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)weg.Gaben["Zuordnen"];
            var keine = new Dictionary<string, bool>();
            GebaeudeImportStand stand = await Schritt("Zuordnen", () => zuordnen(new GebaeudeZuordnungsanfrage(0, null, keine, null, null, null)));
            IReadOnlyDictionary<string, bool> haken = stand.Raeume.ToDictionary(r => r.Kennung, _ => true);
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI) };
            GebaeudeImportStand mitPlan = await Schritt("Zuordnen mit Projektdatei", () =>
                zuordnen(new GebaeudeZuordnungsanfrage(0, null, haken, null, null, null, null, false, schritte, haken)));
            Assert.NotNull(mitPlan.Zonierung?.Plan);

            const string NAME = "Sporthaus Durchlauf";
            var ergebnis = new GebaeudeImportErgebnis(0, null, NAME, haken, mitPlan.Zeilen.ToList(), AlsZone: true, Zonenregel: null,
                                                      Planschritte: schritte, Plangrundhaken: haken);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)weg.Gaben["Pruefen"];
            IReadOnlyList<GebaeudeImportMeldung> befund = await Schritt("Prüfen", () => pruefen(ergebnis));
            Assert.DoesNotContain(befund, m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler);

            var uebernehmen = (Func<GebaeudeImportErgebnis, Task<string>>)weg.Gaben["Uebernehmen"];
            string grund = await Schritt("Übernehmen", () => uebernehmen(ergebnis));
            Assert.Null(grund);

            IReadOnlyDictionary<string, object> editor = await Schritt("Editorgaben", () => weg.EditorGaben());
            Assert.NotNull(editor);
            var arbeit = new GebaeudeArbeitsstand();
            GebaeudePruefbefund pb = await Schritt("Editor laden und prüfen", () =>
            {
                arbeit.Laden((GebaeudeKatalogDaten)editor["Daten"], neu: true);
                GebaeudePruefbefund b = arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
                arbeit.Ableiten();
                return b;
            });
            Assert.True(pb == null, pb?.Meldung);
            var speichern = (Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>)editor["Speichern"];
            GebaeudeKatalogErgebnis gespeichert = await Schritt("Katalogsatz speichern", () => speichern(arbeit.Stand, true, arbeit.Stand.Name));
            Assert.True(gespeichert.Erfolg, gespeichert.Meldung);

            GebaeudeProjektZeile zeile = await Schritt("Aufnehmen", () => weg.Aufnehmen());
            Assert.NotNull(zeile);
            ((List<GebaeudeProjektZeile>)gaben["Zeilen"]).Add(zeile);
            ((Action)gaben["Geaendert"])();

            (bool ok, string meldung) = await Schritt("Gebäudeliste speichern",
                () => new WizardCtrl().Speichere_Projekt_Gebaeudeliste(PROJEKT, modelle));
            Assert.True(ok, meldung);
        }
    }
}
