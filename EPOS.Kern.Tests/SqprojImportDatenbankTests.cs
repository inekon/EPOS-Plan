using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Durchstich des Imports allein aus der Projektdatei</b> gegen die Arbeitskopie der Testdatenbank: Lesen →
    /// Zonenplan mit den Zonen der Projektdatei → Vorschlag → Speichern samt Herkunft in EINEM Vorgang (Format und Herkunft
    /// <c>SQPROJ</c> an Importquelle, Zonen, Bauteilen, Aufbauten und Baustoffen; Nordrichtung als Annahme) → Neulesen der
    /// Datei → der gespeicherte Plan findet jeden Raum in seiner Zone wieder. Gegenprobe: Der Weg „IFC + Projektdatei“
    /// speichert weiter <c>IFC</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojImportDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        private static long MaxId(string tabelle)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT IFNULL(MAX(\"ID\"), 0) FROM \"" + tabelle + "\""), CultureInfo.InvariantCulture);

        /// <summary>Die Werte einer Spalte der Zeilen mit einer ID über <paramref name="ab"/>.</summary>
        private static List<string> Werte(string tabelle, string spalte, long ab, string bedingung = null)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"" + spalte + "\" FROM \"" + tabelle + "\" WHERE \"ID\" > ?" + (bedingung == null ? "" : " AND " + bedingung) + " ORDER BY \"ID\"",
                new DbParam("@id", ab));
            return t.Rows.Cast<DataRow>().Select(r => r[0] is DBNull ? null : Convert.ToString(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        private static ProjektGebaeudeModel Gebaeude()
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            return ctrl.items[0];
        }

        [Fact]
        public void Lesen_Zonieren_Vorschlagen_Speichern_Neulesen_mit_Herkunft_Projektdatei()
        {
            if (!_db.Vorhanden) return;
            string pfad = SqprojImportTests.Sporthaus().Schreiben(SqprojProbenErzeuger.TempPfad("durchstich"));
            _dateien.Add(pfad);
            ProjektGebaeudeModel g = Gebaeude();
            GebaeudeImportAblauf ablauf = SqprojImportTests.Lesen(pfad);

            // Zonenplan wie die Hülle: der Vorschlag nach der Vorgabe, dann die Zonen der Projektdatei übernommen.
            // Wie beim Weg „IFC + Projektdatei“: Eine Simulationszone mit unbeheiztem Raum übernimmt die Hülle erst, wenn der
            // Anwender die Beheizung entschieden hat — hier alle Räume beheizt.
            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0);
            foreach (AbbildRaum r in plan.Gebaeude.Raeume) plan.BeheizungSetzen(r.Kennung, true);
            SqprojZonenergebnis uebernahme = ablauf.ProjektdateiUebernehmen(plan);
            Assert.Equal(2, uebernahme.Uebernommen);
            Assert.Contains(uebernahme.Meldungen, m => m.Schluessel == SqprojProtokoll.BILANZ && m.Werte[0] == "4" && m.Werte[1] == "0" && m.Werte[2] == "0");

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, plan.Haken, null, plan.Zonieren());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));

            long zone0 = MaxId(ZonenSchema.TAB_ZONE), bauteil0 = MaxId(ZonenSchema.TAB_BAUTEIL);
            long aufbau0 = MaxId(SchemaKatalog.TAB_BAUTEILAUFBAU), stoff0 = MaxId(SchemaKatalog.TAB_BAUSTOFF);
            long quelle0 = MaxId(ImportzuordnungSchema.TAB_QUELLE);
            GebaeudeZonenCtrl.Vorschlagsergebnis e;
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang);
                Assert.True(h.Ok, h.Meldung);
                vorgang.Commit();
            }

            // Format und Herkunft SQPROJ — nirgends IFC.
            Assert.Equal(new[] { DbWerte.IMPORT_FORMAT_SQPROJ }, Werte(ImportzuordnungSchema.TAB_QUELLE, "Format", quelle0));
            Assert.Equal(new[] { NordwinkelherkunftWerte.ANNAHME },
                         Werte(ImportzuordnungSchema.TAB_QUELLE, NordrichtungSchema.SPALTE, quelle0));
            List<string> zonen = Werte(ZonenSchema.TAB_ZONE, "Herkunft", zone0);
            Assert.NotEmpty(zonen);
            Assert.All(zonen, x => Assert.Equal(DbWerte.HERKUNFT_SQPROJ, x));
            List<string> bauteile = Werte(ZonenSchema.TAB_BAUTEIL, "Herkunft", bauteil0);
            Assert.Contains(DbWerte.HERKUNFT_SQPROJ, bauteile);
            Assert.DoesNotContain(DbWerte.HERKUNFT_IFC, bauteile);
            List<string> aufbauten = Werte(SchemaKatalog.TAB_BAUTEILAUFBAU, "Herkunft", aufbau0);
            Assert.Contains(DbWerte.HERKUNFT_SQPROJ, aufbauten);
            Assert.DoesNotContain(DbWerte.HERKUNFT_IFC, aufbauten);
            List<string> stoffe = Werte(SchemaKatalog.TAB_BAUSTOFF, "Herkunft", stoff0, "\"Quelle\" = 'Projektdatei'");
            Assert.NotEmpty(stoffe);
            Assert.All(stoffe, x => Assert.Equal(DbWerte.HERKUNFT_SQPROJ, x));

            // Zurückgelesen: die Quelle, die Ausrichtung als Annahme, die Herkunft mit ihrem Text.
            var import = new GebaeudeImportCtrl();
            ImportquelleModel quelle = import.LesenQuellen(g.ID_Gebaeude).Single(q => q.ID > quelle0);
            Assert.Equal(DbWerte.IMPORT_FORMAT_SQPROJ, quelle.Format);
            Assert.Equal(Nordwinkelherkunft.Annahme, import.LesenAusrichtung(g.ID_Gebaeude).Herkunft);
            Assert.Equal("Projektdatei", BaustoffCtrl.HerkunftText(zonen[0]));

            // Neulesen: dieselbe Datei passt, der gespeicherte Plan findet jeden Raum in seiner Zone wieder.
            NeulesenErgebnis n;
            using (FileStream s = File.OpenRead(pfad))
                n = GebaeudeNeulesen.Lesen(s, pfad, quelle, import.Gebaeudekennung(quelle), false);
            Assert.Equal(NeulesenZustand.Passend, n.Zustand);
            Zonenplan gespeichert = ZonenplanCtrl.Gespeichert(PROJEKT, quelle.Hash, n.Abbild, n.Gebaeudeindex);
            Assert.NotNull(gespeichert);
            foreach (AbbildRaum r in n.Abbild.Gebaeude[n.Gebaeudeindex].Raeume)
                Assert.Equal(plan.Zone(plan.ZoneVon(r.Kennung))?.Name, gespeichert.Zone(gespeichert.ZoneVon(r.Kennung))?.Name);
        }

        [Fact]
        public void Gegenprobe_IFC_mit_Projektdatei_speichert_weiter_IFC()
        {
            if (!_db.Vorhanden) return;
            string pfad = SqprojProbenErzeuger.Zonenhaus().Schreiben(SqprojProbenErzeuger.TempPfad("gegenprobe"));
            _dateien.Add(pfad);
            ProjektGebaeudeModel g = Gebaeude();
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            ablauf.Abbild.Gebaeude[0].Art = "TModelBuilding";
            SqprojStand stand = ablauf.ProjektdateiLesen(pfad, 0);
            Assert.False(stand.Abgelehnt);
            Assert.False(ablauf.AlleinAusProjektdatei);
            Assert.Same(stand, ablauf.ProjektdateiFuer(0));

            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            foreach (AbbildRaum r in plan.Gebaeude.Raeume) plan.BeheizungSetzen(r.Kennung, true);
            ablauf.ProjektdateiUebernehmen(plan, SqprojZonierung.Simulation);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, plan.Haken, null, plan.Zonieren());
            Assert.False(v.Abgelehnt);

            long zone0 = MaxId(ZonenSchema.TAB_ZONE), quelle0 = MaxId(ImportzuordnungSchema.TAB_QUELLE);
            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                Assert.True(new GebaeudeImportCtrl().SchreibeHerkunft(g.ID_Gebaeude, ablauf.Quelle, e.Zuordnungen, vorgang).Ok);
                vorgang.Commit();
            }
            Assert.Equal(new[] { DbWerte.IMPORT_FORMAT_IFC }, Werte(ImportzuordnungSchema.TAB_QUELLE, "Format", quelle0));
            List<string> zonen = Werte(ZonenSchema.TAB_ZONE, "Herkunft", zone0);
            Assert.NotEmpty(zonen);
            Assert.All(zonen, x => Assert.Equal(DbWerte.HERKUNFT_IFC, x));
        }
    }
}
