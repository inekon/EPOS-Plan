using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using MyResource = WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Als Mehrzonenmodell übernehmen…“</b> (Welle GD-4; Entwurf Gebäudedialog 2.4, E130 F7): die Prüfsumme der gespeicherten
    /// Importquelle (passend, abweichend, Datei fehlt, anderes Format, keine Quelle — nie ein stiller Rückfall) und das
    /// <b>Ersetzen in einem Vorgang</b> samt Rückweg auf den alten Stand bei einem Fehler. Synthetische Projektdatei aus dem
    /// <see cref="SqprojProbenErzeuger"/>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class MehrzonenuebernahmeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        /// <summary>Ein Haus mit zwei DIN-Zonen und einem Raum ohne Zone (platziert in „Rest OG“) — drei Zonen.</summary>
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
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("gd4_mehrzonen"));
            _dateien.Add(pfad);
            return pfad;
        }

        /// <summary>Die gespeicherte Quelle einer Datei, wie sie der Import schreibt (Format und SHA-256).</summary>
        private static ImportquelleModel QuelleVon(string pfad)
        {
            string hash;
            using (FileStream s = File.OpenRead(pfad)) hash = MehrzonenuebernahmeCtrl.Pruefsumme(s);
            return new ImportquelleModel
            {
                Format = GebaeudeImportProfil.FuerDatei(pfad).Format, Dateiname = Path.GetFileName(pfad), Hash = hash
            };
        }

        [Fact]
        public void Pruefsumme_passend_abweichend_fehlend_falsches_Format_und_ohne_Quelle_sind_benannt()
        {
            string pfad = Probehaus();
            ImportquelleModel q = QuelleVon(pfad);

            Assert.Equal(MehrzonenquellZustand.Passend, MehrzonenuebernahmeCtrl.QuellePruefen(pfad, q).Zustand);

            Mehrzonenquellpruefung keine = MehrzonenuebernahmeCtrl.QuellePruefen(pfad, null);
            Assert.Equal(MehrzonenquellZustand.KeineQuelle, keine.Zustand);
            Assert.Equal(MyResource.Resource.GEBMZ_MSG_KEINE_QUELLE, keine.Meldung);

            string fehlt = Path.Combine(Path.GetDirectoryName(pfad)!, "gibt-es-nicht-" + Guid.NewGuid().ToString("N") + ".sqproj");
            Mehrzonenquellpruefung f = MehrzonenuebernahmeCtrl.QuellePruefen(fehlt, q);
            Assert.Equal(MehrzonenquellZustand.DateiFehlt, f.Zustand);
            Assert.Contains("ist nicht lesbar", f.Meldung);

            string anders = Path.ChangeExtension(pfad, ".xml");
            File.Copy(pfad, anders, true);
            _dateien.Add(anders);
            Assert.Equal(MehrzonenquellZustand.FormatAbweichend, MehrzonenuebernahmeCtrl.QuellePruefen(anders, q).Zustand);

            // Ein Byte mehr: dieselbe Datei, aber eine andere Prüfsumme - die Meldung nennt den Weg „Import…“.
            string geaendert = Path.Combine(Path.GetDirectoryName(pfad)!, "gd4_geaendert_" + Guid.NewGuid().ToString("N") + ".sqproj");
            File.Copy(pfad, geaendert, true);
            _dateien.Add(geaendert);
            using (FileStream s = new FileStream(geaendert, FileMode.Append)) s.WriteByte(0);
            Mehrzonenquellpruefung ab = MehrzonenuebernahmeCtrl.QuellePruefen(geaendert, q);
            Assert.Equal(MehrzonenquellZustand.PruefsummeAbweichend, ab.Zustand);
            Assert.Contains("weicht von der gespeicherten Importquelle ab", ab.Meldung);
            Assert.False(string.IsNullOrEmpty(ab.HashDatei));
        }

        [Fact]
        public async Task Hulle_lehnt_abweichende_Pruefsumme_ab_ohne_Blatt()
        {
            string pfad = Probehaus();
            ImportquelleModel q = QuelleVon(pfad);
            q.Hash = new string('0', 64);
            var h = new GebaeudeMehrzonenHuelle(() => 0, 0, ios: false);
            GebaeudeMehrzonenstart s = await h.StartAsync(pfad, q);
            Assert.Null(s.Gaben);
            Assert.Contains("weicht von der gespeicherten Importquelle ab", s.Meldung);
            Assert.Null(h.Ausstehend());
        }

        /// <summary>Lesen, Zonen der Projektdatei (DIN), OK „als Zonen mit Bauteilen“ — die Hülle merkt das Modell vor.</summary>
        private static async Task Vormerken(GebaeudeMehrzonenHuelle h, string pfad, ImportquelleModel q)
        {
            GebaeudeMehrzonenstart s = await h.StartAsync(pfad, q);
            Assert.Null(s.Meldung);
            IReadOnlyDictionary<string, object> gaben = s.Gaben!;
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(pfad, null!, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand ohne = zuordnen(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, null, null, false, null, null));
            IReadOnlyDictionary<string, bool> haken = ohne.Raeume.ToDictionary(r => r.Kennung, _ => true);
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.DIN) };
            GebaeudeImportStand plan = zuordnen(new GebaeudeZuordnungsanfrage(0, null, haken, null, null, null, null, false, schritte, haken));
            var ergebnis = new GebaeudeImportErgebnis(0, null, "Probe", haken, plan.Zeilen.ToList(), AlsZone: true,
                                                      Zonenregel: null, Planschritte: schritte, Plangrundhaken: haken);
            var uebernehmen = (Func<GebaeudeImportErgebnis, Task<string>>)gaben["Uebernehmen"];
            Assert.Null(await uebernehmen(ergebnis));
        }

        private static (List<int> Zonen, long Bauteile, long Quellen) Stand(int idGebaeude)
        {
            List<int> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).Select(z => z.ID).OrderBy(i => i).ToList();
            long bauteile = Convert.ToInt64(DataRepository.GetDataTable(
                "SELECT COUNT(*) FROM \"Tab_Bauteil\" b INNER JOIN \"Tab_Zone\" z ON z.\"ID\" = b.\"ID_Zone\" WHERE z.\"ID_Gebaeude\" = ?",
                new DbParam("@g", idGebaeude)).Rows[0][0]);
            long quellen = Convert.ToInt64(DataRepository.GetDataTable(
                "SELECT COUNT(*) FROM \"Tab_Importquelle\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude)).Rows[0][0]);
            return (zonen, bauteile, quellen);
        }

        /// <summary>Alle Spalten der Projektkopie als Text — die Gebäudefelder außerhalb der Zonen bleiben.</summary>
        private static string Gebaeudefelder(int idGebaeude)
            => string.Join("|", DataRepository.GetDataTable("SELECT * FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                                            new DbParam("@g", idGebaeude)).Rows[0].ItemArray.Select(o => Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)));

        [Fact]
        public async Task Ersetzen_in_einem_Vorgang_und_Rueckweg_bei_Fehler()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            string pfad = Probehaus();
            ImportquelleModel q = QuelleVon(pfad);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            int idGebaeude = ctrl.items[0].ID_Gebaeude;
            string felder = Gebaeudefelder(idGebaeude);

            // Erster Lauf: das Modell wird vorgemerkt (nichts geschrieben), dann im OK-Weg in einem Vorgang geschrieben.
            var h = new GebaeudeMehrzonenHuelle(() => idGebaeude, 1045, ios: false);
            var vorher = Stand(idGebaeude);
            await Vormerken(h, pfad, q);
            Assert.NotNull(h.Ausstehend());
            var vorgemerkt = Stand(idGebaeude);
            Assert.Equal(vorher.Zonen, vorgemerkt.Zonen);
            Assert.Equal((vorher.Bauteile, vorher.Quellen), (vorgemerkt.Bauteile, vorgemerkt.Quellen));
            Assert.Null(h.Schreiben());
            Assert.Null(h.Ausstehend());
            var erst = Stand(idGebaeude);
            Assert.Equal(3, erst.Zonen.Count);
            Assert.True(erst.Bauteile > 0);
            Assert.Equal(1, erst.Quellen);
            Assert.NotNull(MehrzonenuebernahmeCtrl.Quelle(idGebaeude));

            // Zweiter Lauf ersetzt: wieder drei Zonen, neue Ids, eine Quelle; die Gebäudefelder bleiben.
            await Vormerken(h, pfad, MehrzonenuebernahmeCtrl.Quelle(idGebaeude));
            Assert.Null(h.Schreiben());
            var zweit = Stand(idGebaeude);
            Assert.Equal(3, zweit.Zonen.Count);
            Assert.Empty(zweit.Zonen.Intersect(erst.Zonen));
            Assert.Equal(erst.Bauteile, zweit.Bauteile);
            Assert.Equal(1, zweit.Quellen);
            Assert.Equal(felder, Gebaeudefelder(idGebaeude));

            // Rückweg: Die Herkunft scheitert NACH dem Löschen und dem Schreiben der Zonen (ungültige Prüfsumme der Quelle) -
            // der Vorgang rollt zurück, der Stand des zweiten Laufs bleibt Zone für Zone.
            await Vormerken(h, pfad, MehrzonenuebernahmeCtrl.Quelle(idGebaeude));
            GebaeudeImportHerkunft gut = h.Vorgemerkt!;
            GebaeudeQuelle g = gut.Quelle;
            var kaputt = gut with
            {
                Quelle = new GebaeudeQuelle(g.Format, g.Dateiname, "kein-hash", g.Groesse, g.Schemastand, g.Zeitpunkt,
                                            g.Programmfassung, g.Zonenregel, g.FehlendeEntitaeten)
            };
            Mehrzonenersetzung e = MehrzonenuebernahmeCtrl.Ersetzen(idGebaeude, kaputt);
            Assert.False(e.Ok);
            Assert.StartsWith("Das Mehrzonenmodell ist nicht übernommen", e.Meldung);
            Assert.Equal(zweit.Zonen, Stand(idGebaeude).Zonen);
            Assert.Equal(zweit.Bauteile, Stand(idGebaeude).Bauteile);
            Assert.Equal(1, Stand(idGebaeude).Quellen);

            // Ohne Bauteilvorschlag und an einem Katalogsatz: benannt abgelehnt, bevor etwas geschrieben ist.
            Assert.Equal(MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN,
                         MehrzonenuebernahmeCtrl.Ersetzen(idGebaeude, gut with { Vorschlag = null }).Meldung);
            Assert.False(MehrzonenuebernahmeCtrl.Ersetzen(-5, gut).Ok);
            Assert.Equal(zweit.Zonen, Stand(idGebaeude).Zonen);
        }
    }
}
