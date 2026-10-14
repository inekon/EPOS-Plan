using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
    /// MA-6 „Wiederimport bewahrt“ (Entwurf Geführter Modellaufbau 2.6, E137 F7): Hand-Profil, Hand-Aufbau und Hand-Zone
    /// überstehen den Wiederimport einer geänderten Fassung — je mit Gegenprobe, dass der Dateiwert aktualisiert wird —, eine
    /// entfallende Fläche wird gemeldet und nur mit Zustimmung entfernt, und ein Fehler rollt die Zusammenführung zurück.
    /// Synthetische Projektdatei mit runden Phantasiewerten.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class WiederimportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        /// <summary>Das Probehaus; die Fassung 2 ändert Halle (R1) und die Wände und lässt das Dach DA1 weg.</summary>
        private string Probehaus(string name, bool fassung2)
        {
            SqprojProbenErzeuger e = new SqprojProbenErzeuger { Baujahr = "1995-05-01 00:00:00" }
                .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
                .Raum("R1", "Halle", "F1", "{41111111-1111-1111-1111-111111111111}", fassung2 ? 60.0 : 50.0, null, 1, 3.0)
                .Raum("R2", "Umkleide", "F1", "{42222222-2222-2222-2222-222222222222}", 20.0, null, 1, 3.0)
                .Raum("R3", "Buero", "F2", "{43333333-3333-3333-3333-333333333333}", 30.0, null, 1, 3.0)
                .Raum("R4", "Lager", "F2", null, 10.0, null, 1, 3.0)
                .Zone("Z3", "Nutzung Halle", 5, null, "R1", "R2")
                .Zone("Z4", "Nutzung Buero", 5, null, "R3")
                .Flaeche("W1", 1, 3, fassung2 ? 40.0 : 30.0, fassung2 ? 44.0 : 34.0, 180.0, 90.0, cad: "C1", u: 0.5).Bezug("E1", "R1", "W1", 1)
                .Flaeche("W2", 1, 3, fassung2 ? 20.0 : 25.0, fassung2 ? 20.0 : 25.0, 0.0, 90.0, cad: "C2", u: 0.5).Bezug("E3", "R3", "W2", 1)
                .Flaeche("BP1", 11, 5, 50.0, 50.0, null, 0.0, cad: "C3", u: 0.8).Bezug("E4", "R1", "BP1", 8);
            if (!fassung2)
                e = e.Flaeche("DA1", 5, 3, 30.0, 30.0, null, 0.0, cad: "C5", u: 0.4).Bezug("E6", "R3", "DA1", 9);
            e = e.Flaeche("DA2", 5, 3, 10.0, 10.0, null, 0.0, cad: "C6", u: 0.4).Bezug("E7", "R4", "DA2", 9);
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad(name));
            _dateien.Add(pfad);
            return pfad;
        }

        private static ImportquelleModel QuelleVon(string pfad)
        {
            string hash;
            using (FileStream s = File.OpenRead(pfad)) hash = MehrzonenuebernahmeCtrl.Pruefsumme(s);
            return new ImportquelleModel { Format = GebaeudeImportProfil.FuerDatei(pfad).Format, Dateiname = Path.GetFileName(pfad), Hash = hash };
        }

        /// <summary>
        /// Öffnet das Blatt, liest, ordnet nach DIN zu, holt die Vorschau (erstes OK), setzt die Wahl und merkt vor (zweites OK).
        /// Liefert die Vorschau (<c>null</c> = ohne Bestand).
        /// </summary>
        private static async Task<GebaeudeWiederimportDaten> Vormerken(GebaeudeMehrzonenHuelle h, string pfad, ImportquelleModel q, bool entfernen = false)
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
            var vorschau = (Func<GebaeudeImportErgebnis, GebaeudeWiederimportDaten>)gaben["Wiederimport"];
            GebaeudeWiederimportDaten daten = vorschau(ergebnis);
            if (daten != null)
            {
                Assert.Equal("", daten.Meldung);
                daten.Entfernen = entfernen;
                daten.EntfernenSetzen?.Invoke(entfernen);
            }
            var uebernehmen = (Func<GebaeudeImportErgebnis, Task<string>>)gaben["Uebernehmen"];
            Assert.Null(await uebernehmen(ergebnis));
            return daten;
        }

        private static List<ZoneModel> Zonen(int idGebaeude) => new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude);

        private static string Kalender(int idZone)
            => string.Join(";", DataRepository.GetDataTable(
                "SELECT \"ID\", \"Groesse\", COALESCE(\"Nutzung\", '') FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ? ORDER BY \"ID\"",
                new DbParam("@z", idZone)).Rows.Cast<DataRow>().Select(r => string.Join(",", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture)))));

        /// <summary>Die Wand eines Raums: das einzige Bauteil mit Neigung 90° in der Zone.</summary>
        private static BauteilModel Wand(ZoneModel z) => z.Bauteile.Single(b => b.Neigung.HasValue && Math.Abs(b.Neigung.Value - 90.0) < 1e-6 && b.Quellkennung != null);

        private static int Anlegen(Func<DbVorgang, int> schreiben)
        {
            using DbVorgang v = DataRepository.Vorgang();
            int id = schreiben(v);
            v.Commit();
            return id;
        }

        [Fact]
        public async Task Hand_Profil_Hand_Aufbau_und_Hand_Zone_ueberstehen_den_Wiederimport()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            int idGebaeude = ctrl.items[0].ID_Gebaeude;
            Assert.Empty(Zonen(idGebaeude));

            // Erster Import (ohne Bestand: keine Vorschau, alles neu).
            string v1 = Probehaus("ma6_v1", false);
            var h = new GebaeudeMehrzonenHuelle(() => idGebaeude, 1045, ios: false);
            Assert.Null(await Vormerken(h, v1, QuelleVon(v1)));
            Assert.Null(h.Schreiben());
            List<ZoneModel> erst = Zonen(idGebaeude);
            Assert.Equal(3, erst.Count);
            ZoneModel halle = erst.OrderByDescending(z => z.Nutzflaeche ?? 0).First();
            ZoneModel buero = erst.Single(z => z.ID != halle.ID && z.Bauteile.Any(b => b.Neigung is double n && Math.Abs(n - 90.0) < 1e-6));
            BauteilModel w1 = Wand(halle), w2 = Wand(buero);
            int dachDa1 = buero.Bauteile.Single(b => b.Neigung is double n && n < 1e-6).ID;

            // Hand-Profil: ein anderes Profil des Katalogs und Sollwerte über das Blatt „Nutzung und Kalender“ (MA-2).
            var nutzung = new ZonenNutzungCtrl(idGebaeude);
            ZonenNutzungsprofil profil = ZonenNutzungCtrl.Profile().FirstOrDefault(p => p.Name != halle.Nutzungsprofil);
            if (profil != null) Assert.True(nutzung.ProfilZuweisen(new[] { halle.ID }, profil.Id).Ok);
            Assert.True(nutzung.SollwerteSetzen(new[] { halle.ID }, 21.5, 16.5).Ok);
            ZoneModel halleHand = Zonen(idGebaeude).Single(z => z.ID == halle.ID);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, halleHand.Herkunft);
            string kalenderHand = Kalender(halle.ID);

            // Hand-Aufbau: ein Katalogaufbau als Projektkopie an der Wand W1 (wie MA-4), mit eigenem U-Wert.
            int stamm = Convert.ToInt32(DataRepository.GetDataTable("SELECT MIN(\"ID\") FROM \"Tab_Bauteilaufbau_STAMM\"").Rows[0][0], CultureInfo.InvariantCulture);
            int idProjekt = Convert.ToInt32(DataRepository.GetDataTable("SELECT \"ID_Projekt\" FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                                                        new DbParam("@g", idGebaeude)).Rows[0][0], CultureInfo.InvariantCulture);
            int aufbauHand = new BauteilaufbauCtrl().CopyFromStamm(stamm, idProjekt);
            Assert.True(aufbauHand > 0);
            Anlegen(v => v.Ausfuehren("UPDATE \"Tab_Bauteil\" SET \"ID_Aufbau\" = ?, \"U_Wert\" = ? WHERE \"ID\" = ?",
                                      new DbParam("@a", aufbauHand), new DbParam("@u", 0.25), new DbParam("@b", w1.ID)));

            // Hand-Zone samt Bauteil (ohne Quellkennung) und ein ergänztes Bauteil (MA-3, Herkunft VORGABE) in der Halle.
            int handzone = Anlegen(v => v.EinfuegenUndId(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\", \"Nutzflaeche\", \"Raumhoehe\") VALUES (?, ?, ?, ?, ?, ?)",
                new[] { new DbParam("@g", idGebaeude), new DbParam("@r", 9), new DbParam("@b", "Handzone"), new DbParam("@h", 1),
                        new DbParam("@f", 10.0), new DbParam("@rh", 3.0) }));
            BauteilModel handwand = w2.Kopie();
            handwand.ID_Zone = handzone; handwand.Bezeichner = "Handwand"; handwand.Quellkennung = null; handwand.Herkunft = DbWerte.HERKUNFT_VORGABE;
            handwand.ID_Aufbau = null; handwand.ID_Nachbarzone = null;
            Anlegen(v => GebaeudeZonenCtrl.BauteilEinfuegen(v, handwand));
            BauteilModel ergaenzt = w1.Kopie();
            ergaenzt.Bezeichner = "Wand (ergänzt)"; ergaenzt.Quellkennung = null; ergaenzt.Herkunft = DbWerte.HERKUNFT_VORGABE;
            ergaenzt.ID_Aufbau = null; ergaenzt.Rang = 99; ergaenzt.ID_Nachbarzone = null;
            Anlegen(v => GebaeudeZonenCtrl.BauteilEinfuegen(v, ergaenzt));

            // Wiederimport der geänderten Fassung: Vorschau, dann Zusammenführung (entfallende behalten).
            string v2 = Probehaus("ma6_v2", true);
            GebaeudeWiederimportDaten vorschau = await Vormerken(h, v2, MehrzonenuebernahmeCtrl.Quelle(idGebaeude));
            Assert.NotNull(vorschau);
            Assert.True(vorschau.GeaenderteFassung);
            Assert.Equal(1, vorschau.Anzahl(Wiederimportstand.Bleibt, true));
            Assert.Equal(2, vorschau.Anzahl(Wiederimportstand.Bleibt, false));
            Assert.Equal(1, vorschau.Anzahl(Wiederimportstand.Entfaellt, false));
            Assert.Equal(0, vorschau.Anzahl(Wiederimportstand.Neu, true));
            Assert.Contains(vorschau.Zeilen, z => z.IstZone && z.Stand == Wiederimportstand.Geaendert
                                                 && z.Felder.Contains(MyResource.Resource.GEBWI_FELD_FLAECHE)
                                                 && z.Hand.Contains(MyResource.Resource.GEBWI_FELD_PROFIL));
            Assert.Contains(vorschau.Zeilen, z => !z.IstZone && z.Hand.Contains(MyResource.Resource.GEBWI_FELD_AUFBAU)
                                                 && z.Felder.Contains(MyResource.Resource.GEBWI_FELD_FLAECHE));
            Assert.Null(h.Schreiben());

            List<ZoneModel> zweit = Zonen(idGebaeude);
            // Hand-Profil: Id, Profil, Sollwerte, Herkunft und Kalender bleiben - Gegenprobe: die Fläche kommt aus der Datei.
            ZoneModel h2 = zweit.Single(z => z.ID == halle.ID);
            Assert.Equal(halleHand.Nutzungsprofil, h2.Nutzungsprofil);
            Assert.Equal(21.5, h2.Raumsolltemperatur_Tag);
            Assert.Equal(16.5, h2.Raumsolltemperatur_Nachtabsenkung);
            Assert.Equal(DbWerte.HERKUNFT_MANUELL, h2.Herkunft);
            Assert.Equal(kalenderHand, Kalender(halle.ID));
            Assert.Equal((halle.Nutzflaeche ?? 0) + 10.0, h2.Nutzflaeche ?? 0, 6);

            // Hand-Aufbau: Aufbau und U bleiben an derselben Id - Gegenprobe: ihre Fläche und die der Wand W2 kommen aus der Datei.
            BauteilModel w1neu = h2.Bauteile.Single(b => b.ID == w1.ID);
            Assert.Equal(aufbauHand, w1neu.ID_Aufbau);
            Assert.Equal(0.25, w1neu.U_Wert);
            Assert.NotEqual(w1.Flaeche, w1neu.Flaeche);
            BauteilModel w2neu = zweit.SelectMany(z => z.Bauteile).Single(b => b.ID == w2.ID);
            Assert.NotEqual(w2.Flaeche, w2neu.Flaeche);
            Assert.Equal(w2.ID_Zone, w2neu.ID_Zone);

            // Hand-Zone und ergänzte Bauteile bleiben; die entfallende Fläche DA1 bleibt (ohne Zustimmung).
            Assert.Contains(zweit, z => z.ID == handzone && z.Bezeichner == "Handzone" && z.Bauteile.Any(b => b.Bezeichner == "Handwand"));
            Assert.Contains(h2.Bauteile, b => b.Bezeichner == "Wand (ergänzt)");
            Assert.Contains(zweit.SelectMany(z => z.Bauteile), b => b.ID == dachDa1);
            Assert.Equal(new[] { buero.ID, halle.ID, handzone }.Concat(erst.Select(z => z.ID)).Distinct().OrderBy(i => i),
                         zweit.Select(z => z.ID).OrderBy(i => i));
            Assert.Single(DataRepository.GetDataTable("SELECT \"ID\" FROM \"Tab_Importquelle\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude)).Rows.Cast<DataRow>());

            // Rückweg: die Herkunft scheitert NACH der Zusammenführung - der Stand bleibt Satz für Satz.
            await Vormerken(h, v2, MehrzonenuebernahmeCtrl.Quelle(idGebaeude));
            GebaeudeImportHerkunft gut = h.Vorgemerkt!;
            GebaeudeQuelle g = gut.Quelle;
            var kaputt = gut with
            {
                Quelle = new GebaeudeQuelle(g.Format, g.Dateiname, "kein-hash", g.Groesse, g.Schemastand, g.Zeitpunkt,
                                            g.Programmfassung, g.Zonenregel, g.FehlendeEntitaeten)
            };
            string vorher = Abbild(idGebaeude);
            Mehrzonenersetzung e = MehrzonenuebernahmeCtrl.Zusammenfuehren(idGebaeude, kaputt, true);
            Assert.False(e.Ok);
            Assert.StartsWith("Das Mehrzonenmodell ist nicht übernommen", e.Meldung);
            Assert.Equal(vorher, Abbild(idGebaeude));

            // Mit Zustimmung fällt die entfallende Fläche; Hand-Sätze bleiben.
            Assert.True(MehrzonenuebernahmeCtrl.Zusammenfuehren(idGebaeude, gut, true).Ok);
            List<ZoneModel> dritt = Zonen(idGebaeude);
            Assert.DoesNotContain(dritt.SelectMany(z => z.Bauteile), b => b.ID == dachDa1);
            Assert.Contains(dritt, z => z.ID == handzone);
            Assert.Equal(aufbauHand, dritt.SelectMany(z => z.Bauteile).Single(b => b.ID == w1.ID).ID_Aufbau);
        }

        /// <summary>Alle Zonen und Bauteile des Gebäudes als Text (Id und Felder).</summary>
        private static string Abbild(int idGebaeude)
            => string.Join("|", Zonen(idGebaeude).OrderBy(z => z.ID).Select(z =>
                   z.ID + ":" + z.Bezeichner + ":" + z.Nutzflaeche + ":" + z.Nutzungsprofil + ":" + Kalender(z.ID) + "[" +
                   string.Join(",", z.Bauteile.OrderBy(b => b.ID).Select(b => b.ID + "/" + b.Flaeche + "/" + b.ID_Aufbau + "/" + b.U_Wert)) + "]"));
    }
}
