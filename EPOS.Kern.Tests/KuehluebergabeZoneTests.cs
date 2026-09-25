using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kühlübergabe an der Zone über die Kopierwege von <c>Tab_Zone</c></b> (E37, Schritt 137;
    /// W4-6): Die drei Spalten (<c>Kuehl_Uebergabe_Art</c> mit <c>IDEAL</c>, Exponent, Nennleistung)
    /// reisen über das Aggregat je Gebäude (<see cref="GebaeudeZonenCtrl"/>), das Projektduplikat und
    /// den Projekttransfer — gesetzt mit ihrem Wert, leer als NULL. Die Spaltenliste von
    /// <see cref="ZonenSchema"/> bleibt unverändert; gelesen werden die Spalten erst ab G6.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KuehluebergabeZoneTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1007;
        private const string PROJEKTNAME = "Laurentiuskirche";
        private const int GEBAEUDE = 10614;

        /// <summary>Drei Zonen: Kühldecke mit Werten, ausdrücklich ideal, und ganz leer (Wert des Gebäudes).</summary>
        private static List<ZoneModel> DreiZonen()
        {
            ZoneModel decke = GebaeudeG3PruefregelTests.GueltigeZone();
            decke.Bezeichner = "Büro";
            decke.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            decke.Kuehl_Uebergabe_Exponent = 1.05;
            decke.Kuehl_Uebergabe_Leistung_Nenn = 3.5;

            ZoneModel ideal = GebaeudeG3PruefregelTests.GueltigeZone();
            ideal.Bezeichner = "Lager";
            ideal.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_IDEAL;

            ZoneModel leer = GebaeudeG3PruefregelTests.GueltigeZone();
            leer.Bezeichner = "Flur";
            return new List<ZoneModel> { decke, ideal, leer };
        }

        private static void PruefeDreiZonen(IList<ZoneModel> g)
        {
            Assert.Equal(new[] { "Büro", "Lager", "Flur" }, g.Select(z => z.Bezeichner));
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, g[0].Kuehl_Uebergabe_Art);
            Assert.Equal(1.05, g[0].Kuehl_Uebergabe_Exponent);
            Assert.Equal(3.5, g[0].Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_IDEAL, g[1].Kuehl_Uebergabe_Art);
            Assert.Null(g[1].Kuehl_Uebergabe_Exponent);
            Assert.Null(g[1].Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Null(g[2].Kuehl_Uebergabe_Art);
            Assert.Null(g[2].Kuehl_Uebergabe_Exponent);
            Assert.Null(g[2].Kuehl_Uebergabe_Leistung_Nenn);
        }

        /// <summary>
        /// Das Aggregat legt die drei Spalten an, liest sie verlustfrei zurück und ändert sie über die
        /// Id — ein geleertes Feld schreibt NULL, ein unverändertes bleibt stehen.
        /// </summary>
        [Fact]
        public void Das_Aggregat_traegt_die_Kuehluebergabe_der_Zone_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new GebaeudeZonenCtrl();
            Assert.True(KuehluebergabeSchema.ZoneVollstaendig());

            GebaeudeZonenCtrl.Ergebnis e = ctrl.SpeichernJeGebaeude(GEBAEUDE, DreiZonen());
            Assert.True(e.Ok, e.Meldung);
            List<ZoneModel> g = ctrl.LesenJeGebaeude(GEBAEUDE);
            PruefeDreiZonen(g);

            // Ändern über die Id: der Exponent wird geleert, die Nennleistung bleibt, die Art wechselt.
            g[0].Kuehl_Uebergabe_Exponent = null;
            g[1].Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;
            Assert.True(ctrl.SpeichernJeGebaeude(GEBAEUDE, g).Ok);
            List<ZoneModel> h = ctrl.LesenJeGebaeude(GEBAEUDE);
            Assert.Null(h[0].Kuehl_Uebergabe_Exponent);
            Assert.Equal(3.5, h[0].Kuehl_Uebergabe_Leistung_Nenn);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR, h[1].Kuehl_Uebergabe_Art);
            Assert.Null(h[2].Kuehl_Uebergabe_Art);

            // Der Leseweg des Rechenkerns liest dieselben Werte.
            List<ZoneModel> p = ctrl.LesenJeProjekt(PROJEKT)[GEBAEUDE];
            Assert.Equal(h.Select(z => z.Kuehl_Uebergabe_Art), p.Select(z => z.Kuehl_Uebergabe_Art));
        }

        /// <summary>Eine unbekannte Kühlübergabeart der Zone wird benannt abgelehnt, bevor etwas geschrieben ist.</summary>
        [Fact]
        public void Eine_unbekannte_Kuehluebergabeart_der_Zone_wird_benannt_abgelehnt()
        {
            ZoneModel z = GebaeudeG3PruefregelTests.GueltigeZone();
            z.Kuehl_Uebergabe_Art = "KLIMAGERAET";
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ZONE_MSG_KUEHLUEBERGABEART,
                                       "Wohnen", "KLIMAGERAET"),
                         GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
            z.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_IDEAL;
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { z }));
        }

        private static List<ZoneModel> Zonen(int idProjekt)
            => new GebaeudeZonenCtrl().LesenJeProjekt(idProjekt).Values.Single();

        /// <summary>Das Projektduplikat trägt die drei Spalten der Zone mit, NULL bleibt NULL.</summary>
        [Fact]
        public void Das_Projektduplikat_traegt_die_Kuehluebergabe_der_Zone()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, DreiZonen()).Ok);

            int neu = new ProjektDuplizierenCtrl().Duplizieren(PROJEKTNAME, PROJEKTNAME + " Zone KAK");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            PruefeDreiZonen(Zonen(neu));
        }

        /// <summary>Der Projekttransfer (Export und Import eines Pakets) trägt die drei Spalten der Zone.</summary>
        [Fact]
        public void Der_Projekttransfer_traegt_die_Kuehluebergabe_der_Zone()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new GebaeudeZonenCtrl().SpeichernJeGebaeude(GEBAEUDE, DreiZonen()).Ok);

            string ordner = Path.Combine(Path.GetTempPath(), "epos-kak-zone-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "z.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(PROJEKTNAME, paket));
                int neu = io.Importieren(paket, "Transfer Zone KAK", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeDreiZonen(Zonen(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }
    }
}
