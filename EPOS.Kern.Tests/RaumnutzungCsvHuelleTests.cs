using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle des CSV-Imports und -Exports</b> (Stufe NP4a; Konzept Nutzungsprofile 6.1, 6.4) gegen die Testdatenbank:
    /// das Bündel hängt am Weg des Blatts, die Vorschau trägt Stand und Zählung je Profil, die Dateiwahl läuft über
    /// <see cref="Dienste.Datei"/> (Öffnen, Speichern, auf iOS das Teilen), eine abgebrochene Wahl schreibt nichts.
    /// </summary>
    [Collection("Testdatenbank")]   // die Fälle tauschen Dienste.Datei — prozessweiter Zustand
    public sealed class RaumnutzungCsvHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly IDateiDienst _dateiVorher = Dienste.Datei;
        private readonly string _ordner = Path.Combine(Path.GetTempPath(), "epos-np4a-" + Guid.NewGuid().ToString("N"));

        public RaumnutzungCsvHuelleTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            Dienste.Datei = _dateiVorher;
            try { Directory.Delete(_ordner, true); } catch (IOException) { }
            _db.Dispose();
        }

        private sealed class Dateiprobe : IDateiDienst
        {
            internal string Oeffnen = "";
            internal string Speichern = "";
            internal string Vorschlag = "";
            internal readonly List<string> Geteilt = new();

            public string DateiOeffnen(string titel, string filter, string startOrdner) => Oeffnen;

            public string DateiSpeichern(string titel, string filter, string vorschlag)
            {
                Vorschlag = vorschlag ?? "";
                return Speichern;
            }

            public string OrdnerWaehlen(string titel, string startOrdner) => "";

            public bool MitSystemOeffnen(string pfad)
            {
                Geteilt.Add(pfad);
                return true;
            }
        }

        [Fact]
        public void Das_Buendel_haengt_am_Weg_des_Blatts_mit_allen_Delegaten_und_Texten()
        {
            RaumnutzungCsvWeg w = RaumnutzungHuelle.Weg().Csv;
            Assert.NotNull(w);
            Assert.NotNull(w.DateiWaehlen);
            Assert.NotNull(w.Pruefen);
            Assert.NotNull(w.Uebernehmen);
            Assert.NotNull(w.Exportieren);
            RaumnutzungCsvTexte t = RaumnutzungCsvHuelle.Texte();
            Assert.All(typeof(RaumnutzungCsvTexte).GetProperties().Where(p => p.PropertyType == typeof(string)),
                       p => Assert.False(string.IsNullOrEmpty((string)p.GetValue(t)), p.Name));
        }

        [Fact]
        public async Task Wahl_Vorschau_Uebernehmen_und_erneute_Vorschau_zeigen_den_Stand()
        {
            Dienste.Datei = new Dateiprobe { Oeffnen = RaumnutzungCsvTests.Beispielpfad() };
            RaumnutzungCsvWeg w = RaumnutzungCsvHuelle.Weg(new RaumnutzungCtrl(), ios: false);

            RaumnutzungCsvDatei datei = await w.DateiWaehlen();
            Assert.Equal("nutzungsprofile_beispiel.csv", datei.Name);
            RaumnutzungCsvVorschau v = w.Pruefen(datei, null);
            Assert.Null(v.Abbruch);
            Assert.Equal(3, v.Uebernehmbar);
            Assert.All(v.Profile, p => Assert.Equal(RaumnutzungCsvStand.Neu, p.Stand));
            Assert.All(v.Profile, p => Assert.True(p.Uebernommen > 0));
            Assert.Empty(v.Hinweise);

            RaumnutzungErgebnis e = w.Uebernehmen(datei, null, "Aus der Hülle", false);
            Assert.True(e.Ok, e.Meldung);
            RaumnutzungCsvVorschau nochmal = w.Pruefen(datei, e.Id);
            Assert.Equal(3, nochmal.Vorhanden);
            Assert.All(nochmal.Profile, p => Assert.Equal(RaumnutzungCsvStand.Vorhanden, p.Stand));
        }

        [Fact]
        public async Task Eine_abgebrochene_Wahl_liefert_nichts_und_ein_Lesefehler_kommt_mit_Text()
        {
            Dienste.Datei = new Dateiprobe();
            RaumnutzungCsvWeg w = RaumnutzungCsvHuelle.Weg(new RaumnutzungCtrl(), ios: false);
            Assert.Null(await w.DateiWaehlen());

            Dienste.Datei = new Dateiprobe { Oeffnen = Path.Combine(_ordner, "fehlt.csv") };
            await Assert.ThrowsAsync<InvalidOperationException>(() => w.DateiWaehlen());
        }

        [Fact]
        public void Eine_kaputte_Datei_kommt_als_Abbruch_in_die_Vorschau()
        {
            RaumnutzungCsvWeg w = RaumnutzungCsvHuelle.Weg(new RaumnutzungCtrl(), ios: false);
            RaumnutzungCsvVorschau v = w.Pruefen(new RaumnutzungCsvDatei("x.csv", System.Text.Encoding.UTF8.GetBytes("A,B\n1,2\n")), null);
            Assert.NotNull(v.Abbruch);
            Assert.Empty(v.Profile);
            Assert.False(w.Uebernehmen(new RaumnutzungCsvDatei("x.csv", Array.Empty<byte>()), null, "Leer", false).Ok);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Der_Export_schreibt_die_Datei_und_teilt_sie_auf_iOS(bool ios)
        {
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungCtrl.Kategorie muster = ctrl.Kategorien().First(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER);
            string ziel = Path.Combine(_ordner, "muster.csv");
            var probe = new Dateiprobe { Speichern = ziel };
            Dienste.Datei = probe;

            RaumnutzungErgebnis e = await RaumnutzungCsvHuelle.Exportieren(ctrl, muster.Id, ios);
            Assert.True(e.Ok, e.Meldung);
            Assert.Contains(ziel, e.Meldung);
            Assert.Equal(RaumnutzungCsvHuelle.Dateivorschlag(muster.Bezeichner), probe.Vorschlag);
            byte[] bytes = File.ReadAllBytes(ziel);
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
            Assert.Equal(ctrl.Profile(muster.Id).Count, RaumnutzungCsv.Lesen(bytes).Zeilen.Count);
            Assert.Equal(ios ? 1 : 0, probe.Geteilt.Count);
        }

        [Fact]
        public async Task Ein_abgebrochener_Export_schreibt_nichts_und_eine_fehlende_Kategorie_wird_benannt()
        {
            var probe = new Dateiprobe();
            Dienste.Datei = probe;
            var ctrl = new RaumnutzungCtrl();
            RaumnutzungErgebnis ab = await RaumnutzungCsvHuelle.Exportieren(ctrl, ctrl.Kategorien().First().Id, false);
            Assert.True(ab.Ok);
            Assert.Equal("", ab.Meldung);
            Assert.Empty(Directory.GetFiles(_ordner));

            RaumnutzungErgebnis fehlt = await RaumnutzungCsvHuelle.Exportieren(ctrl, 987654, false);
            Assert.False(fehlt.Ok);
            Assert.False(string.IsNullOrEmpty(fehlt.Meldung));
        }

        [Fact]
        public void Der_Dateivorschlag_traegt_keine_verbotenen_Zeichen()
        {
            Assert.Equal("A_B_C.csv", RaumnutzungCsvHuelle.Dateivorschlag("A/B:C"));
            Assert.Equal("Nutzungsprofile.csv", RaumnutzungCsvHuelle.Dateivorschlag("  "));
        }
    }
}
