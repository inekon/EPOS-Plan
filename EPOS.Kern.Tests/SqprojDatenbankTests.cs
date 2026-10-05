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
    /// <b>Die Projektdatei gegen die Arbeitskopie der Testdatenbank</b> (Datenaustauschkonzept 16.3, SQ-1): das Zonenhaus
    /// (<c>ifc4_zonen.ifc</c>, als HottCAD-Export gekennzeichnet) mit einer Projektdatei derselben Räume → Zonenplan →
    /// Bauteilvorschlag → Speichern. Danach trägt die Simulationszone EG die Kalender der Ganglinie mit Beleg in
    /// <c>Bemerkung</c>, die Lüftung aus dem Nutzungsprofil als Bestandswert ohne Kalender, die übrigen Größen die Kopien
    /// der Vorlage ihrer Nutzung; die Zone OG trägt den Heizsollwert des Profils in der Matrix.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1045;

        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _pfad = SqprojProbenErzeuger.TempPfad("db");

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            try { File.Delete(_pfad); } catch (IOException) { }
        }

        private static SqprojProbenErzeuger Zonenhaus()
            => new SqprojProbenErzeuger()
                .Geschoss("FK", "Kellergeschoss").Geschoss("FE", "Erdgeschoss").Geschoss("FO", "Obergeschoss")
                .Raum("R1", "Lager", "FK", null, 20.0).Raum("R2", "Wohnen", "FE", null, 30.0).Raum("R3", "Küche", "FE", null, 10.0)
                .Raum("R4", "Schlafen", "FO", null, 20.0).Raum("R5", "Bad", "FO", null, 8.0).Raum("R6", "Abstellraum", "FO", null, 4.0)
                .Zone("Z1", "Simulation EG", 6, "G1", "R2", "R3")
                .Zone("Z2", "Simulation OG", 6, "G2", "R4", "R5", "R6")
                .Zone("Z3", "Keller", 6, null, "R1")
                .Zone("Z4", "Nutzung EG", 5, null, "R2", "R3")
                .Zone("Z5", "Nutzung OG", 5, null, "R4", "R5", "R6")
                .Zone("Z6", "Nutzung Keller", 5, null, "R1")
                .Nutzung("U4", "Profil Wohnen", "Z4", 71, SqprojProbenErzeuger.NULLZEIT, SqprojProbenErzeuger.NULLZEIT, null, 20.0, null, 0.5,
                         null, null, null, null, null, null)
                .Nutzung("U5", "Profil Buero", "Z5", 1, SqprojProbenErzeuger.Uhr(7), SqprojProbenErzeuger.Uhr(18), null, 21.0, 4.0, null,
                         null, null, null, null, null, null)
                .Nutzung("U6", "Profil Lager", "Z6", 20, SqprojProbenErzeuger.NULLZEIT, SqprojProbenErzeuger.NULLZEIT, null, null, null, null,
                         null, null, null, null, null, null)
                .Gruppe("G1", "Gruppe EG", 6, 71)
                .Zeitprofil("H1", "Heizen EG", 6, "G1", h => h >= 6 && h < 22 ? 20.0 : 17.0)
                .Zeitprofil("P1", "Personen EG", 8, "G1", _ => 1.0, personen: 3.0, wattJePerson: 80.0)
                .Gruppe("G2", "Gruppe OG", 4, 1);

        private static List<(string Groesse, string Nutzung, string Bemerkung, string Woche)> Kalender(int idZone)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Groesse\", \"Nutzung\", \"Bemerkung\", \"Woche\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" = ? ORDER BY \"Groesse\"",
                new DbParam("@z", idZone));
            static string T(object o) => o is DBNull ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
            return t.Rows.Cast<DataRow>().Select(r => (T(r[0]), T(r[1]), T(r[2]), T(r[3]))).ToList();
        }

        private static List<string> Vorlagengroessen(string nutzung)
        {
            var v = new KonditionierungsvorlageCtrl();
            return Konditionierungsgroessen.Alle.Where(g => v.Liste(g).Any(x => x.Ausgeliefert && x.Nutzung == nutzung))
                                                .Select(Konditionierungsgroessen.Kennwort).ToList();
        }

        [Fact]
        public void Kalender_der_Projektdatei_mit_Beleg_und_Vorlage_wo_nichts_geliefert_wird()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc");
            ablauf.Abbild.Gebaeude[0].Art = "TModelBuilding";
            SqprojStand stand = ablauf.ProjektdateiLesen(Zonenhaus().Schreiben(_pfad), 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.ToString());
            Assert.Equal(6, stand.Abgeglichen);

            Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0, IfcImportProfil.ZONENREGEL_Z4);
            foreach (AbbildRaum r in plan.Gebaeude.Raeume) plan.BeheizungSetzen(r.Kennung, true);
            SqprojZonenergebnis ergebnis = ablauf.ProjektdateiUebernehmen(plan);
            Assert.Equal(3, ergebnis.Uebernommen);
            Assert.Empty(plan.NichtZugeordnet);
            Assert.Contains(ergebnis.Meldungen, m => m.Schluessel == SqprojProtokoll.BILANZ && m.Werte[0] == "6" && m.Werte[3] == "3");

            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, plan.Haken, null, plan.Zonieren());
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.ToString())));
            Assert.Equal(new[] { null, DbWerte.KOND_NUTZUNG_WOHNEN, DbWerte.KOND_NUTZUNG_BUERO }, v.Zonennutzungen);
            Assert.All(v.Zonenkonditionierungen, k => Assert.NotNull(k));

            using (DbVorgang vorgang = DataRepository.Vorgang())
            {
                GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                Assert.True(e.Ok, e.Meldung);
                vorgang.Commit();
            }

            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).ToList();
            Assert.Equal(new[] { "Keller", "Simulation EG", "Simulation OG" }, zonen.Select(z => z.Bezeichner));

            // Keller: Profil 20 ohne Entsprechung, kein Wert — kein Kalender.
            Assert.Empty(Kalender(zonen[0].ID));

            // EG: Heizen und Personen aus der Ganglinie mit Beleg, Lüftung als Bestandswert, Rest aus der Vorlage Wohnen.
            var eg = Kalender(zonen[1].ID).ToDictionary(k => k.Groesse);
            Assert.Contains("PdProfileTimeCurve.Temperature", eg[DbWerte.KOND_GROESSE_HEIZSOLL].Bemerkung);
            Assert.Equal(168, eg[DbWerte.KOND_GROESSE_HEIZSOLL].Woche.Split(';').Length);
            Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, eg[DbWerte.KOND_GROESSE_HEIZSOLL].Nutzung);
            Assert.Contains("PdProfileTimeCurve.Ratio", eg[DbWerte.KOND_GROESSE_PERSONEN].Bemerkung);
            Assert.False(eg.ContainsKey(DbWerte.KOND_GROESSE_LUEFTUNG));
            foreach (string gr in Vorlagengroessen(DbWerte.KOND_NUTZUNG_WOHNEN)
                         .Where(x => x == DbWerte.KOND_GROESSE_KUEHLSOLL || x == DbWerte.KOND_GROESSE_GERAETE))
            {
                Assert.True(eg.ContainsKey(gr), gr);
                Assert.DoesNotContain("PdProfile", eg[gr].Bemerkung ?? "");
                Assert.Equal(DbWerte.KOND_NUTZUNG_WOHNEN, eg[gr].Nutzung);
            }
            Konditionierungsarbeitsstand ks = new KonditionierungCtrl().ArbeitsstandLesen(g.ID_Gebaeude, null, out string m);
            Assert.Null(m);
            Assert.Equal(0.5, Konditionierungsarbeit.Bestandswert(ks.Zone(zonen[1].ID).Stand.Bestand, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG));

            // OG: Heizsollwert aus dem Nutzungsprofil in der Matrix — der Kalender der Vorlage ist gewichen.
            var og = Kalender(zonen[2].ID).ToDictionary(k => k.Groesse);
            Assert.False(og.ContainsKey(DbWerte.KOND_GROESSE_HEIZSOLL));
            Matrixeingang b = ks.Zone(zonen[2].ID).Stand.Bestand;
            Assert.Equal(21.0, Konditionierungsarbeit.Bestandswert(b, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG));
            Assert.Equal(17.0, Konditionierungsarbeit.Bestandswert(b, Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_NACHT));
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, ZonenplanCtrl.Nutzung(zonen[2].ID));
        }
    }
}
