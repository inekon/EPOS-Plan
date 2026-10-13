using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Nachzüge GD-N zu „Als Mehrzonenmodell übernehmen…“</b>: Der zuletzt gespeicherte Zonenplan eines Projektgebäudes wird
    /// als Vorbelegung des Zonenbaums vorgelegt (<see cref="ZonenplanCtrl.GespeichertJeGebaeude"/>, Hülle
    /// <see cref="GebaeudeImportHuelle.Planvorlage"/>), eine abweichende Prüfsumme benannt; beim Weg „Gebäudedatei +
    /// Projektdatei“ trägt die Projektdatei einen Prüfbefund (Vorhandensein, Lesbarkeit, Zuordnung, Prüfsumme).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenplanVorlageTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            _kultur.Dispose();
        }

        private string Probehaus()
        {
            SqprojProbenErzeuger e = new SqprojProbenErzeuger { Baujahr = "1995-05-01 00:00:00" }
                .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
                .Raum("R1", "Halle", "F1", "{41111111-1111-1111-1111-111111111111}", 50.0, null, 1, 3.0)
                .Raum("R2", "Umkleide", "F1", "{42222222-2222-2222-2222-222222222222}", 20.0, null, 1, 3.0)
                .Raum("R3", "Buero", "F2", "{43333333-3333-3333-3333-333333333333}", 30.0, null, 1, 3.0)
                .Raum("R4", "Lager", "F2", null, 10.0, null, 1, 3.0)
                .Zone("Z3", "Nutzung Halle", 5, null, "R1", "R2")
                .Zone("Z4", "Nutzung Buero", 5, null, "R3")
                .Flaeche("W1", 1, 3, 30.0, 34.0, 180.0, 90.0, cad: "C1", u: 0.5).Bezug("E1", "R1", "W1", 1)
                .Flaeche("W2", 1, 3, 25.0, 25.0, 0.0, 90.0, cad: "C2", u: 0.5).Bezug("E3", "R3", "W2", 1)
                .Flaeche("BP1", 11, 5, 50.0, 50.0, null, 0.0, cad: "C3", u: 0.8).Bezug("E4", "R1", "BP1", 8)
                .Flaeche("DA1", 5, 3, 30.0, 30.0, null, 0.0, cad: "C5", u: 0.4).Bezug("E6", "R3", "DA1", 9)
                .Flaeche("DA2", 5, 3, 10.0, 10.0, null, 0.0, cad: "C6", u: 0.4).Bezug("E7", "R4", "DA2", 9);
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("gdn_zonenplan"));
            _dateien.Add(pfad);
            return pfad;
        }

        private static ImportquelleModel QuelleVon(string pfad)
        {
            string hash;
            using (FileStream s = File.OpenRead(pfad)) hash = MehrzonenuebernahmeCtrl.Pruefsumme(s);
            return new ImportquelleModel
            {
                Format = GebaeudeImportProfil.FuerDatei(pfad).Format, Dateiname = Path.GetFileName(pfad), Hash = hash
            };
        }

        /// <summary>Liest die Datei im Blatt der Hülle und liefert die Gaben samt erstem Stand ohne Schritte.</summary>
        private static async Task<(IReadOnlyDictionary<string, object> Gaben, GebaeudeImportStand Stand)> Blatt(GebaeudeMehrzonenHuelle h,
                                                                                                                  string pfad, ImportquelleModel q)
        {
            GebaeudeMehrzonenstart s = await h.StartAsync(pfad, q);
            Assert.Null(s.Meldung);
            IReadOnlyDictionary<string, object> gaben = s.Gaben!;
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(pfad, null!, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, null, null,
                                                                               false, null, null));
            return (gaben, stand);
        }

        private static async Task ErsterImport(GebaeudeMehrzonenHuelle h, string pfad, ImportquelleModel q)
        {
            (IReadOnlyDictionary<string, object> gaben, GebaeudeImportStand ohne) = await Blatt(h, pfad, q);
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            IReadOnlyDictionary<string, bool> haken = ohne.Raeume.ToDictionary(r => r.Kennung, _ => true);
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.DIN) };
            GebaeudeImportStand plan = zuordnen(new GebaeudeZuordnungsanfrage(0, null, haken, null, null, null, null, false, schritte, haken));
            var ergebnis = new GebaeudeImportErgebnis(0, null, "Probe", haken, plan.Zeilen.ToList(), AlsZone: true,
                                                      Zonenregel: null, Planschritte: schritte, Plangrundhaken: haken);
            var uebernehmen = (Func<GebaeudeImportErgebnis, Task<string>>)gaben["Uebernehmen"];
            Assert.Null(await uebernehmen(ergebnis));
            Assert.Null(h.Schreiben());
        }

        [Fact]
        public async Task Der_gespeicherte_Zonenplan_wird_vorgelegt_und_eine_abweichende_Pruefsumme_benannt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string pfad = Probehaus();
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            int idGebaeude = ctrl.items[0].ID_Gebaeude;

            var h = new GebaeudeMehrzonenHuelle(() => idGebaeude, 1045, ios: false);
            await ErsterImport(h, pfad, QuelleVon(pfad));
            ImportquelleModel gespeichert = MehrzonenuebernahmeCtrl.Quelle(idGebaeude);
            Assert.NotNull(gespeichert);
            List<string> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).Select(z => z.Bezeichner).ToList();
            Assert.Equal(3, zonen.Count);

            // Zweiter Lauf: der Zonenbaum startet mit dem gespeicherten Plan, die Prüfsumme ist gleich.
            var zweiter = new GebaeudeMehrzonenHuelle(() => idGebaeude, 1045, ios: false);
            (_, GebaeudeImportStand stand) = await Blatt(zweiter, pfad, gespeichert);
            GebaeudeZonierungDaten zon = stand.Zonierung!;
            Assert.NotNull(zon.Plan);
            Assert.Equal(zonen, zon.Plan!.Zonen.Select(z => z.Name));
            Assert.Contains(gespeichert.Dateiname, zon.Planvorlage);
            Assert.Equal("", zon.PlanvorlageAbweichung);

            // Direkt: eine andere Prüfsumme legt den Plan über die Raumkennungen trotzdem vor und benennt die Abweichung.
            GebaeudeAbbild abbild = Lesen(pfad);
            Zonenplanvorlage gleich = ZonenplanCtrl.GespeichertJeGebaeude(idGebaeude, gespeichert.Hash, abbild, 0);
            Assert.NotNull(gleich);
            Assert.False(gleich.PruefsummeAbweichend);
            Assert.True(gleich.Raeume > 0);
            Zonenplanvorlage anders = ZonenplanCtrl.GespeichertJeGebaeude(idGebaeude, new string('0', 64), abbild, 0);
            Assert.NotNull(anders);
            Assert.True(anders.PruefsummeAbweichend);
            Assert.Equal(gleich.Plan.Zonen.Count, anders.Plan.Zonen.Count);
            Assert.Null(ZonenplanCtrl.GespeichertJeGebaeude(0, gespeichert.Hash, abbild, 0));
            Assert.Null(ZonenplanCtrl.GespeichertJeGebaeude(idGebaeude, gespeichert.Hash, null, 0));
        }

        private static GebaeudeAbbild Lesen(string pfad)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = lesen(pfad, null!, CancellationToken.None).GetAwaiter().GetResult();
            Assert.True(gelesen.Gelesen);
            return h.Ablauf.Abbild;
        }

        [Fact]
        public async Task Die_Projektdatei_traegt_im_Mehrzonenweg_einen_Pruefbefund()
        {
            string pfad = Probehaus();
            var h = new GebaeudeImportHuelle { ProjektdateiPruefen = true };
            GebaeudeProjektdateiDaten fehlt = await h.ProjektdateiLesenAsync(pfad + ".fehlt", 0, CancellationToken.None);
            Assert.Single(fehlt.Pruefbefund);
            Assert.Contains("nicht zu öffnen", fehlt.Pruefbefund[0]);

            GebaeudeProjektdateiDaten da = await h.ProjektdateiLesenAsync(pfad, 0, CancellationToken.None);
            string hash;
            using (FileStream s = File.OpenRead(pfad)) hash = MehrzonenuebernahmeCtrl.Pruefsumme(s);
            Assert.Contains(da.Pruefbefund, z => z.Contains("vorhanden"));
            Assert.Contains(da.Pruefbefund, z => z.Contains(hash.Substring(0, MehrzonenuebernahmeCtrl.HASH_KURZ)));
            Assert.Contains(da.Pruefbefund, z => z.StartsWith("Lesbar", StringComparison.Ordinal) || z.StartsWith("Nicht lesbar", StringComparison.Ordinal));

            // Der gewöhnliche Import prüft nicht.
            var ohne = new GebaeudeImportHuelle();
            Assert.Empty((await ohne.ProjektdateiLesenAsync(pfad, 0, CancellationToken.None)).Pruefbefund);
        }
    }
}
