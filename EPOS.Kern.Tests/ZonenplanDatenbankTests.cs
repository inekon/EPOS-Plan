using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Zonenplan gegen die Arbeitskopie der Testdatenbank</b> (Mehrzonenkonzept 6.4): das Zonenhaus
    /// (<c>ifc4_zonen.ifc</c>) mit Plan nach Z4, eine Zone umbenannt, eine mit der Nutzung Büro → Bauteilvorschlag →
    /// Zonen, Bauteile und Herkunft in einem Vorgang. Danach trägt <c>Tab_Zone.Bezeichner</c> die Namen des Plans, die
    /// Büro-Zone je Größe die Kalenderkopie der ausgelieferten Büro-Vorlage (mit Nutzung), die übrigen Zonen keinen
    /// Kalender, und ein erneuter Import derselben Datei findet den Plan wieder.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenplanDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static ProjektGebaeudeModel Zeile(int idProjekt)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            return ctrl.items[0];
        }

        private static List<(string Groesse, string Nutzung)> Kalender(int idZone)
        {
            var t = DataRepository.GetDataTable(
                "SELECT \"Groesse\", \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" = ? ORDER BY \"Groesse\"",
                new DbParam("@z", idZone));
            return t.Rows.Cast<System.Data.DataRow>()
                    .Select(r => (Convert.ToString(r[0], CultureInfo.InvariantCulture), r[1] is DBNull ? null : Convert.ToString(r[1], CultureInfo.InvariantCulture)))
                    .ToList();
        }

        [Fact]
        public void Name_Nutzung_und_Raeume_des_Plans_werden_gespeichert_und_wiedergefunden()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            string eg = plan.Zonen.Single(z => z.Name == "Erdgeschoss").Schluessel;
            string og = plan.Zonen.Single(z => z.Name == "Obergeschoss").Schluessel;
            Assert.True(plan.NutzungSetzen(eg, DbWerte.KOND_NUTZUNG_BUERO).Ok);
            Assert.True(plan.ZoneUmbenennen(og, "Wohnung oben").Ok);

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, plan.Haken, null, plan.Zonieren());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel)));
            Assert.Equal(new[] { null, DbWerte.KOND_NUTZUNG_BUERO, null }, v.Zonennutzungen);

            GebaeudeZonenCtrl.Vorschlagsergebnis e;
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang);
                Assert.True(h.Ok, h.Meldung);
                vorgang.Commit();
            }

            // Tab_Zone.Bezeichner = Name des Plans.
            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).ToList();
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Wohnung oben" }, zonen.Select(z => z.Bezeichner));

            // Die Büro-Zone trägt je Größe die Kalenderkopie der ausgelieferten Büro-Vorlage, die übrigen keinen Kalender.
            var buero = new KonditionierungsvorlageCtrl();
            List<string> groessen = Konditionierungsgroessen.Alle
                .Where(gr => buero.Liste(gr).Any(x => x.Ausgeliefert && x.Nutzung == DbWerte.KOND_NUTZUNG_BUERO))
                .Select(Konditionierungsgroessen.Kennwort).OrderBy(x => x, StringComparer.Ordinal).ToList();
            Assert.NotEmpty(groessen);
            List<(string Groesse, string Nutzung)> kalender = Kalender(zonen[1].ID);
            Assert.Equal(groessen, kalender.Select(k => k.Groesse).OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(kalender, k => Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, k.Nutzung));
            Assert.Empty(Kalender(zonen[0].ID));
            Assert.Empty(Kalender(zonen[2].ID));
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, ZonenplanCtrl.Nutzung(zonen[1].ID));

            // Erneuter Import derselben Datei: der Plan kommt mit Namen, Nutzung und Räumen wieder.
            GebaeudeImportAblauf erneut = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            Zonenplan wieder = ZonenplanCtrl.Gespeichert(PROJEKT, erneut.Quelle.Hash, erneut.Abbild, 0);
            Assert.NotNull(wieder);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Wohnung oben" }, wieder.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { null, DbWerte.KOND_NUTZUNG_BUERO, null }, wieder.Zonen.Select(z => z.Nutzung));
            foreach (Planzone z in plan.Zonen)
                Assert.Equal(plan.RaeumeVon(z.Schluessel).Select(r => r.Kennung),
                             wieder.RaeumeVon(wieder.Zonen.Single(x => x.Name == z.Name).Schluessel).Select(r => r.Kennung));
            Assert.Empty(wieder.NichtZugeordnet);
            Assert.Null(ZonenplanCtrl.Gespeichert(PROJEKT, new string('0', 64), erneut.Abbild, 0));
        }

        [Fact]
        public void Ein_Plan_mit_offenen_Raeumen_wird_nicht_geschrieben()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel g = Zeile(PROJEKT);
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            plan.Zuordnen(new[] { plan.Gebaeude.Raeume.Single(r => r.Name == "Bad").Kennung }, null);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, plan.Haken, null, plan.Zonieren());
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.False(e.Ok);
            Assert.Empty(new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude));
        }
    }
}
