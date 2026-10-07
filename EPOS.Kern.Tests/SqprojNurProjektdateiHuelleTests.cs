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
    /// <b>Der Weg „nur Projektdatei“ an der Hülle des Gebäudeimports</b> (Datenaustauschkonzept 16.1): drei Profile (gbXML,
    /// IFC, Projektdatei) mit der Grenze je Plattform, die Quellenwahl mit vier Einträgen, und der Durchlauf mit dem
    /// synthetischen Sporthaus — Lesen ohne IFC, Kopf mit Formatschlüssel, Nordrichtung als Annahme, Zuordnen mit Herkunft
    /// „Projektdatei“, Prüfen und Speichern des Vorschlags.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojNurProjektdateiHuelleTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        private string Sporthaus()
        {
            string pfad = SqprojImportTests.Sporthaus().Schreiben(SqprojProbenErzeuger.TempPfad("nur_projektdatei"));
            _dateien.Add(pfad);
            return pfad;
        }

        private static GebaeudeImportStand Zuordnen(IReadOnlyDictionary<string, object> gaben, IReadOnlyDictionary<string, bool> haken,
                                                    params GebaeudePlanschritt[] schritte)
            => ((Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"])(
                new GebaeudeZuordnungsanfrage(0, null, haken, null, null, null, null, false,
                                              schritte.Length > 0 ? schritte : null, schritte.Length > 0 ? haken : null));

        [Fact]
        public void Die_Huelle_fuehrt_drei_Profile_mit_der_Grenze_der_Plattform()
        {
            var windows = new GebaeudeImportHuelle(ios: false);
            Assert.Equal(new[] { GebaeudeQuelle.FORMAT_GBXML, GebaeudeQuelle.FORMAT_IFC, GebaeudeQuelle.FORMAT_SQPROJ },
                         windows.AlleProfile().Select(p => p.Format));
            Assert.Equal(SqprojProfil.MAX_BYTES, windows.AlleProfile().Single(p => p is SqprojImportProfil).MaxBytes);

            var ios = new GebaeudeImportHuelle(ios: true);
            GebaeudeImportProfil sqIos = ios.AlleProfile().Single(p => p is SqprojImportProfil);
            Assert.Equal(SqprojProfil.MAX_BYTES_IOS, sqIos.MaxBytes);
            Assert.True(SqprojProfil.MAX_BYTES_IOS < SqprojProfil.MAX_BYTES);
            Assert.False(GebaeudeImportAblauf.GroesseZulaessig(SqprojProfil.MAX_BYTES_IOS + 1, sqIos));

            GebaeudeImportProfilDaten daten = windows.ProfilDaten();
            Assert.Equal("gbXML, IFC, Projektdatei", daten.Formatname);
            Assert.Contains("Projektdatei 250 MB", daten.Groessengrenze);
            Assert.Contains("*.sqproj", daten.Dateifilter);
            Assert.Contains("Projektdatei 100 MB", ios.ProfilDaten().Groessengrenze);
        }

        [Fact]
        public void Die_Quellenwahl_nennt_vier_Eintraege_mit_ihrem_Filter()
        {
            IReadOnlyList<GebaeudeImportQuellwahl> q = new GebaeudeImportHuelle(ios: false).ProfilDaten().Quellen!;
            Assert.Equal(new[] { GebaeudeImportHuelle.QUELLE_IFC, GebaeudeImportHuelle.QUELLE_GBXML, GebaeudeImportHuelle.QUELLE_IFC_PROJEKTDATEI,
                                 GebaeudeImportHuelle.QUELLE_PROJEKTDATEI }, q.Select(x => x.Schluessel));
            Assert.Equal(new[] { GebaeudeImportWeg.Datei, GebaeudeImportWeg.Datei, GebaeudeImportWeg.MitProjektdatei,
                                 GebaeudeImportWeg.NurProjektdatei }, q.Select(x => x.Weg));
            Assert.Equal(new[] { "IFC", "gbXML", "IFC + Projektdatei", "Nur Projektdatei (.sqproj)" }, q.Select(x => x.Text));
            Assert.Equal(IfcImportProfil.DATEIFILTER, q[0].Dateifilter);
            Assert.Equal(GbxmlImportProfil.DATEIFILTER, q[1].Dateifilter);
            Assert.Equal(IfcImportProfil.DATEIFILTER, q[2].Dateifilter);
            Assert.EndsWith("|*.sqproj", q[3].Dateifilter);
            // Die Formatschlüssel der Wahl sind die Persistenzwerte des Formats.
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, GebaeudeImportHuelle.QUELLE_PROJEKTDATEI);
            Assert.Equal(GebaeudeQuelle.FORMAT_IFC, GebaeudeImportHuelle.QUELLE_IFC);
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, GebaeudeImportHuelle.QUELLE_GBXML);

            // Mit festem Profil gibt es keine Wahl.
            Assert.Empty(new GebaeudeImportHuelle(new IfcImportProfil(), ios: false).ProfilDaten().Quellen ?? Array.Empty<GebaeudeImportQuellwahl>());

            using (new Kulturvorrichtung("en-US"))
                {
                IReadOnlyList<GebaeudeImportQuellwahl> en = new GebaeudeImportHuelle(ios: false).ProfilDaten().Quellen!;
                Assert.Equal("IFC + project file", en[2].Text);
                Assert.Equal("Project file only (.sqproj)", en[3].Text);
            }
        }

        [Fact]
        public async Task Durchlauf_Lesen_Zuordnen_Speichern_allein_aus_der_Projektdatei()
        {
            var h = new GebaeudeImportHuelle(ios: false);
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(Sporthaus(), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            Assert.Equal(GebaeudeImportWeg.NurProjektdatei, gelesen.Kopf!.Weg);
            Assert.Equal("Projektdatei", gelesen.Kopf.Format);
            Assert.Contains(gelesen.Meldungen, m => m.Kennung == "IMP_SQPROJ_PROT_KEIN_NORDEN");

            // Die Nordrichtung: ohne Nordwinkel die Annahme — die Abfrage greift wie bei IFC.
            GebaeudeNordrichtungDaten nord = ((Func<GebaeudeNordrichtungDaten>)gaben["NordrichtungDaten"])();
            Assert.Equal("ANNAHME", nord.Herkunft);

            // Zuordnen: Herkunft „Projektdatei“, kein Dazuladen, die Projektdatei des Laufs im Stand.
            GebaeudeImportStand stand = Zuordnen(gaben, new Dictionary<string, bool>());
            Assert.False(stand.ProjektdateiMoeglich);
            Assert.True(stand.Projektdatei is { Gelesen: true });
            Assert.Contains(stand.Zeilen, z => z.HerkunftText == "Projektdatei");
            Assert.DoesNotContain(stand.Zeilen, z => z.HerkunftText == "IFC" || z.HerkunftText == "gbXML");
            IReadOnlyDictionary<string, bool> haken = stand.Raeume.ToDictionary(r => r.Kennung, _ => true);

            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION) };
            GebaeudeImportStand mitPlan = Zuordnen(gaben, haken, schritte);
            Assert.NotNull(mitPlan.Zonierung?.Plan);
            Assert.All(mitPlan.Zonierung!.Plan!.Zonen, z => Assert.Equal(GebaeudeZonierungSchluessel.HERKUNFT_SIMULATION, z.Herkunft));

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            var ergebnis = new GebaeudeImportErgebnis(0, null, "Sporthaus", haken, mitPlan.Zeilen.ToList(), AlsZone: true, Zonenregel: null,
                                                      Planschritte: schritte, Plangrundhaken: haken);
            IReadOnlyList<GebaeudeImportMeldung> befund = pruefen(ergebnis);
            Assert.DoesNotContain(befund, m => m.Stufe == WarnStufe.Fehler);
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, h.Quelle.Format);
            GebaeudeBauteilvorschlag v = h.Herkunft!.Vorschlag!;
            Assert.NotEmpty(v.Zonen);
            Assert.All(v.Zonen, z => Assert.Equal(DbWerte.HERKUNFT_SQPROJ, z.Herkunft));

            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                vorgang.Commit();
            }
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).ToList();
            Assert.Equal(v.Zonen.Count, zonen.Count);
            Assert.Contains(zonen, z => z.Bezeichner == "Sport");
        }
    }
}
