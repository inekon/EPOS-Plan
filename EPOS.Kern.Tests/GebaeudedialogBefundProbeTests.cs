using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Probe zum Befund GD</b> (Entwurf Gebäudedialog und Mehrzonenübernahme, 11.10.2026): der Weg „nur Projektdatei“ mit
    /// DIN-V-18599-Zonen, die einen Raum nicht erfassen. Die Übernahme der DIN-Zonen hebt die Zonierung der Regel auf, den Raum
    /// ohne DIN-Zone platziert der Plan selbst (Welle GD-3, E130/F3) — OK und Bauteilvorschlag sind frei, die Zonen werden ohne
    /// Handschritt gespeichert; erst die Rücknahme im Zonenbaum lässt ihn offen und sperrt wie zuvor;
    /// der vorbelegte Satz trägt Baujahr und Verwendung aus der Datei, die Gebäudeart bleibt die Vorgabe eines neuen Satzes.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudedialogBefundProbeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        /// <summary>Das Sporthaus der Probe, die DIN-Zonen (Typ 5) ohne den Raum R4 und ohne Simulationszonen.</summary>
        private string SporthausMitRaumOhneDinZone()
        {
            SqprojProbenErzeuger e = new SqprojProbenErzeuger { Baujahr = "1970-05-01 00:00:00" }
                .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
                .Raum("R1", "Halle", "F1", "{31111111-1111-1111-1111-111111111111}", 50.0, null, 1, 3.0)
                .Raum("R2", "Umkleide", "F1", "{32222222-2222-2222-2222-222222222222}", 20.0, null, 1, 3.0)
                .Raum("R3", "Buero", "F2", "{33333333-3333-3333-3333-333333333333}", 30.0, null, 1, 3.0)
                .Raum("R4", "Lager", "F2", null, 10.0, null, 1, 3.0)
                .Zone("Z3", "Nutzung Halle", 5, null, "R1", "R2")
                .Zone("Z4", "Nutzung Buero", 5, null, "R3")
                .Nutzung("U1", "Profil Buero", "Z4", 1, SqprojProbenErzeuger.Uhr(7), SqprojProbenErzeuger.Uhr(18), null, 21.0,
                         null, null, null, null, null, null, null, null)
                .Flaeche("W1", 1, 3, 30.0, 34.0, 180.0, 90.0, cad: "C1", u: 0.5).Bezug("E1", "R1", "W1", 1)
                .Flaeche("W2", 1, 3, 25.0, 25.0, 0.0, 90.0, cad: "C2", u: 0.5).Bezug("E3", "R3", "W2", 1)
                .Flaeche("BP1", 11, 5, 50.0, 50.0, null, 0.0, cad: "C3", u: 0.8).Bezug("E4", "R1", "BP1", 8)
                .Flaeche("DA1", 5, 3, 30.0, 30.0, null, 0.0, cad: "C5", u: 0.4).Bezug("E6", "R3", "DA1", 9)
                .Flaeche("DA2", 5, 3, 10.0, 10.0, null, 0.0, cad: "C6", u: 0.4).Bezug("E7", "R4", "DA2", 9);
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad("gd_befund"));
            _dateien.Add(pfad);
            return pfad;
        }

        private static GebaeudeImportStand Zuordnen(IReadOnlyDictionary<string, object> gaben, IReadOnlyDictionary<string, bool> haken,
                                                    params GebaeudePlanschritt[] schritte)
            => ((Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"])(
                new GebaeudeZuordnungsanfrage(0, null, haken, null, null, null, null, false,
                                              schritte.Length > 0 ? schritte : null, schritte.Length > 0 ? haken : null));

        [Fact]
        public async Task Raum_ohne_DIN_Zone_wird_platziert_und_die_Zonen_werden_gespeichert()
        {
            var h = new GebaeudeImportHuelle(ios: false);
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(SporthausMitRaumOhneDinZone(), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));

            GebaeudeImportStand ohne = Zuordnen(gaben, new Dictionary<string, bool>());
            IReadOnlyDictionary<string, bool> haken = ohne.Raeume.ToDictionary(r => r.Kennung, _ => true);

            // Der Schritt „Zonen der Projektdatei“ (DIN) hebt die Zonierung der Regel auf; den Raum ohne DIN-Zone platziert der
            // Plan selbst (E130/F3): beheizt, ohne Nachbarraum in einer Zone → die Restzone seines Geschosses.
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.DIN) };
            GebaeudeImportStand mitPlan = Zuordnen(gaben, haken, schritte);
            GebaeudeZonenplanDaten plan = mitPlan.Zonierung!.Plan!;
            Assert.Empty(plan.NichtZugeordnet);
            Assert.Equal(1, plan.Platziert);
            Assert.NotEmpty(plan.Platzierungsmeldung);
            GebaeudePlanzoneDaten rest = Assert.Single(plan.Zonen, z => z.Raumliste.Any(r => r.Name == "Lager"));
            Assert.Equal("Rest OG", rest.Name);
            GebaeudePlanraumDaten lager = rest.Raumliste.Single(r => r.Name == "Lager");
            Assert.Equal("geschoss", lager.Platzierungsgrund);
            Assert.True(mitPlan.Bauteile!.Moeglich);

            // OK ist frei — mit und ohne „als Zonen mit Bauteilen“.
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            foreach (bool alsZone in new[] { false, true })
            {
                var ergebnis = new GebaeudeImportErgebnis(0, null, "Probe", haken, mitPlan.Zeilen.ToList(), AlsZone: alsZone,
                                                          Zonenregel: null, Planschritte: schritte, Plangrundhaken: haken);
                Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == WarnStufe.Fehler);
            }

            // Der Durchlauf „nur Projektdatei“ speichert die Zonen ohne Handschritt — drei Zonen samt Restzone.
            GebaeudeBauteilvorschlag vorschlag = h.Herkunft!.Vorschlag!;
            Assert.Equal(3, vorschlag.Zonen.Count);
            using (var db = new TestDatenbank())
            {
                if (db.Vorhanden)
                {
                    var ctrl = new ProjektGebaeudeCtrl();
                    ctrl.ReadAll(1045);
                    ProjektGebaeudeModel g = ctrl.items[0];
                    using (DbVorgang vorgang = DataRepository.Vorgang())
                    {
                        GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, vorschlag, vorgang);
                        Assert.True(e.Ok, e.Meldung);
                        vorgang.Commit();
                    }
                    Assert.Equal(3, new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).Count());
                }
            }

            // Die Rücknahme im Zonenbaum stellt den Raum zurück auf „ohne Zone“: dann sperrt er wie zuvor OK und Bauteilvorschlag.
            var zurueck = new[] { schritte[0], new GebaeudePlanschritt(GebaeudePlanschrittArt.RUECKNAHME, new[] { lager.Kennung }) };
            GebaeudeImportStand offen = Zuordnen(gaben, haken, zurueck);
            Assert.Equal(new[] { "Lager" }, offen.Zonierung!.Plan!.NichtZugeordnet.Select(r => r.Name));
            Assert.DoesNotContain(offen.Zonierung.Plan.Zonen, z => z.Name == "Rest OG");
            Assert.False(offen.Bauteile!.Moeglich);
            var gesperrt = new GebaeudeImportErgebnis(0, null, "Probe", haken, offen.Zeilen.ToList(), AlsZone: false,
                                                      Zonenregel: null, Planschritte: zurueck, Plangrundhaken: haken);
            Assert.Contains(pruefen(gesperrt), m => m.Stufe == WarnStufe.Fehler);

            // „Rest nach Regel zuordnen“ (X3, je Raum) löst auch diese Sperre.
            var mitRest = new[] { zurueck[0], zurueck[1], new GebaeudePlanschritt(GebaeudePlanschrittArt.REST, Regel: GebaeudeImportProfil.ZONENREGEL_X3) };
            Assert.Empty(Zuordnen(gaben, haken, mitRest).Zonierung!.Plan!.NichtZugeordnet);

            // Ohne Plan (Regelvorschlag ohne Schritt): Vorbelegung des Gebäudesatzes — Baujahr und Verwendung aus der Datei,
            // die Gebäudeart bleibt die Vorgabe eines neuen Satzes bzw. wird „Sonstige“ bei einem Nichtwohngebäude.
            var ohnePlan = new GebaeudeImportErgebnis(0, null, "Probe", haken, ohne.Zeilen.ToList(), AlsZone: false, Zonenregel: null);
            GebaeudeVorbelegung v = h.Vorbelegung(ohnePlan);
            Assert.Equal(1970, v.Daten.Baujahr);
            Assert.Equal(GebaeudeStammCtrl.FILTERWERT_SONSTIGE, v.Daten.Verwendung);
            Assert.Equal(GebaeudeImportHuelle.GEBAEUDEART_SONSTIGE, v.Daten.Gebaeudeart);
            // Die Gebäudewerte der Projektdatei (Art, Nutzung, Wärmeschutz) liest der Import nicht: Energiestandard bleibt leer.
            Assert.Null(v.Daten.Energiestandard);
        }
    }
}
