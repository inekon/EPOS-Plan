using System;
using System.Linq;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zonenplan mit Profil statt Kennung</b> (Konzept Nutzungsprofile NP-F23, Stufe NP2): <c>EPOS_Zone.Nutzung</c>
    /// einer IFC-Datei von EPOS-Plan als Profilname → dieses Profil, als alte Kennung → EPOS-Muster gleicher Nutzung, als
    /// unbekannter Text → der Text bleibt mit dem Befund „nicht im Katalog“; der exportierte Profilname führt beim erneuten
    /// Import zum selben Profil (IFC-Rundreise). Die Klappliste der Hülle trägt die Profile des Katalogs.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenplanProfilTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        /// <summary>Das Schichtenhaus mit <c>EPOS_Zone.Nutzung</c> = <paramref name="nutzung"/> an jedem Raum, exportiert und wieder gelesen.</summary>
        private static Zonenplan Rundreise(string nutzung)
        {
            GebaeudeExportPlan export = ExportSatzProbe.Plan(ExportSatzProbe.Satz(ExportSatzProbe.Schichtenhaus()), IfcExportProbe.Profil());
            foreach (AbbildRaum r in export.Abbild.Gebaeude[0].Raeume)
                r.Konditionierung = new AbbildKonditionierung { Nutzung = nutzung, HeizsollTagC = 21.0 };
            byte[] datei = IfcExportProbe.Schreiben(export.Abbild);
            GebaeudeAbbild zurueck = IfcExportProbe.Lesen(datei);
            Assert.All(zurueck.Gebaeude[0].Raeume, r => Assert.Equal(nutzung, r.Konditionierung?.Nutzung));
            Zonenplan plan = Zonenplan.Vorschlag(zurueck, 0);
            Assert.NotEmpty(plan.Zonen);
            return plan;
        }

        private static long MusterId(string name)
        {
            var ctrl = new RaumnutzungCtrl();
            long katalog = ctrl.Kategorien().Single(k => k.Art == RaumnutzungSchema.ART_EPOS_MUSTER).Id;
            return ctrl.Profile(katalog).Single(p => p.Bezeichner == name).Id;
        }

        [Fact]
        public void Profilname_der_Datei_fuehrt_zum_selben_Profil()
        {
            if (!_db.Vorhanden) return;
            Zonenplan plan = Rundreise(RaumnutzungSaat.SPORT);
            Assert.All(plan.Zonen, z => Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.SPORT), RaumnutzungSaat.SPORT), z.Profil));
        }

        [Fact]
        public void Alte_Kennung_der_Datei_fuehrt_zum_EPOS_Muster_gleicher_Nutzung()
        {
            if (!_db.Vorhanden) return;
            Zonenplan plan = Rundreise(DbWerte.KOND_NUTZUNG_SCHULE);
            Assert.All(plan.Zonen, z => Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.SCHULE), RaumnutzungSaat.SCHULE), z.Profil));
        }

        [Fact]
        public void Unbekannter_Text_bleibt_mit_Befund_nicht_im_Katalog_und_bleibt_waehlbar()
        {
            if (!_db.Vorhanden) return;
            Zonenplan plan = Rundreise("Großraum Nord");
            Planzone z = plan.Zonen[0];
            Assert.Equal(new Planprofil(null, "Großraum Nord", NichtImKatalog: true), z.Profil);

            // Die Klappliste trägt die Profile des Katalogs und den Text der Zone; die Zone zeigt ihren Schlüssel.
            GebaeudeZonenplanDaten daten = GebaeudeImportZonen.PlanDaten(plan, null, Array.Empty<GebaeudeZonenzeileDaten>(), (null, false, 0));
            Assert.Contains(daten.Nutzungen, n => n.Schluessel == "#" + MusterId(RaumnutzungSaat.TECHNIK) && n.Text == RaumnutzungSaat.TECHNIK);
            Assert.Contains(daten.Nutzungen, n => n.Schluessel == "Großraum Nord" && n.Text == "Großraum Nord");
            Assert.Equal("Großraum Nord", daten.Zonen[0].Nutzung);

            // Wählbar bleibt der Text nur an seiner Zone; ein anderer unbekannter Text wird benannt abgelehnt.
            Assert.True(plan.NutzungSetzen(z.Schluessel, "#" + MusterId(RaumnutzungSaat.LAGER)).Ok);
            Assert.Equal(RaumnutzungSaat.LAGER, z.Nutzung);
            Planschritt ab = plan.NutzungSetzen(z.Schluessel, "Großraum Süd");
            Assert.False(ab.Ok);
            Assert.EndsWith(Zonenplan.PLAN_NUTZUNG_UNGUELTIG, ab.Meldung.Schluessel);
            Assert.True(plan.NutzungSetzen(z.Schluessel, DbWerte.KOND_NUTZUNG_BUERO).Ok);
            Assert.Equal(new Planprofil(MusterId(RaumnutzungSaat.BUERO), RaumnutzungSaat.BUERO), z.Profil);
        }
    }
}
