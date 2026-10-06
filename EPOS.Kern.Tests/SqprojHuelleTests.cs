using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projektdatei an der Hülle des Gebäudeimports</b> (Datenaustauschkonzept 16.4, Stufe SQ-2, Probe 37 Datenseite):
    /// das Zonenhaus als HottCAD-Export mit der Projektdatei derselben Räume — Bilanz der Projektdatei im Stand, der Schritt
    /// <see cref="GebaeudePlanschrittArt.PROJEKTDATEI"/> am Zonenplan (Zonen mit Herkunft, Nutzung, Profil, Heizsollwert Tag),
    /// Ablehnung bei fehlender Tabelle und ohne HottCAD, „Projektdatei entfernen“, und Speichern über die Herkunft der Hülle:
    /// die Kalender tragen den Beleg der Projektdatei in <c>Bemerkung</c>. Dazu der iOS-Dateifilter <c>.sqproj</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class SqprojHuelleTests : IDisposable
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

        private string Merken(string pfad)
        {
            _dateien.Add(pfad);
            return pfad;
        }

        private async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben)> Gelesen(bool hottcad = true)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            string ifc = hottcad ? Merken(SqprojProbenErzeuger.HottcadZonenhaus(Wurzel())) : GbxmlImportTests.Probe("ifc4_zonen.ifc");
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(ifc, null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return (h, gaben);
        }

        private static GebaeudeImportStand Zuordnen(IReadOnlyDictionary<string, object> gaben, IReadOnlyDictionary<string, bool> haken,
                                                    params GebaeudePlanschritt[] schritte)
            => ((Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"])(
                new GebaeudeZuordnungsanfrage(0, null, haken, null, null, "Z4", null, false,
                                              schritte.Length > 0 ? schritte : null, schritte.Length > 0 ? haken : null));

        /// <summary>Alle Räume beheizt — die Zonen der Projektdatei nehmen auch den Keller auf.</summary>
        private static IReadOnlyDictionary<string, bool> AlleBeheizt(IReadOnlyDictionary<string, object> gaben)
            => Zuordnen(gaben, new Dictionary<string, bool>()).Raeume.ToDictionary(r => r.Kennung, _ => true);

        private async Task<GebaeudeProjektdateiDaten> Dazuladen(IReadOnlyDictionary<string, object> gaben, SqprojProbenErzeuger probe)
        {
            string pfad = Merken(probe.Schreiben(SqprojProbenErzeuger.TempPfad("huelle")));
            var lesen = (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)gaben["ProjektdateiLesen"];
            return await lesen(pfad, 0, CancellationToken.None);
        }

        [Fact]
        public async Task Der_Knopf_gilt_nur_fuer_den_HottCAD_Export()
        {
            (_, IReadOnlyDictionary<string, object> fremd) = await Gelesen(hottcad: false);
            Assert.False(Zuordnen(fremd, new Dictionary<string, bool>()).ProjektdateiMoeglich);
            GebaeudeProjektdateiDaten ab = await Dazuladen(fremd, SqprojProbenErzeuger.Zonenhaus());
            Assert.False(ab.Gelesen);
            Assert.Equal(SqprojProtokoll.KEIN_HOTTCAD, ab.Ablehnung!.Kennung);

            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            GebaeudeImportStand stand = Zuordnen(gaben, new Dictionary<string, bool>());
            Assert.True(stand.ProjektdateiMoeglich);
            Assert.Null(stand.Projektdatei);
        }

        [Fact]
        public async Task Bilanz_Schritt_und_Entfernen_an_der_Huelle()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            IReadOnlyDictionary<string, bool> haken = AlleBeheizt(gaben);
            GebaeudeProjektdateiDaten sq = await Dazuladen(gaben, SqprojProbenErzeuger.Zonenhaus().Raum("R7", "Galerie", "FO", null, 5.0));
            Assert.True(sq.Gelesen, sq.Ablehnung?.Text);
            Assert.Equal(6, sq.Abgeglichen);
            Assert.Equal(1, sq.NichtAbgeglichen);
            Assert.Equal("Obergeschoss/Galerie", Assert.Single(sq.RaeumeOhneTreffer));
            Assert.Equal(6, sq.Zonen);
            Assert.Null(sq.Uebernommen);
            Assert.Equal(WarnStufe.Warnung, sq.Schwerste!.Stufe);
            Assert.Equal(SqprojProtokoll.RAUM_OHNE_TREFFER, sq.Schwerste.Kennung);

            GebaeudeImportStand stand = Zuordnen(gaben, haken, new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION));
            GebaeudeZonenplanDaten plan = stand.Zonierung!.Plan!;
            Assert.False(plan.LetzterAbgelehnt);
            Assert.Equal(SqprojProtokoll.BILANZ, plan.Schrittmeldung!.Kennung);
            Assert.Equal(new[] { "Keller", "Simulation EG", "Simulation OG" }, plan.Zonen.Select(z => z.Name));
            Assert.All(plan.Zonen, z => Assert.True(z.AusProjektdatei));
            Assert.Equal(new[] { null, DbWerte.KOND_NUTZUNG_WOHNEN, DbWerte.KOND_NUTZUNG_BUERO }, plan.Zonen.Select(z => z.Nutzung));
            Assert.Equal("Nutzungsprofil 71 nach DIN V 18599", plan.Zonen[1].Profiltext);
            Assert.Equal("20 °C", plan.Zonen[1].Sollwert);
            Assert.Equal("21 °C", plan.Zonen[2].Sollwert);
            Assert.Empty(plan.NichtZugeordnet);
            Assert.Equal(3, stand.Projektdatei!.Uebernommen);
            GebaeudeProjektdateiGroesse heizen = stand.Projektdatei.Groessen.Single(g => g.Schluessel == DbWerte.KOND_GROESSE_HEIZSOLL);
            Assert.Equal((1, 1), (heizen.Ganglinie, heizen.Nutzungsprofil));

            ((Action)gaben["ProjektdateiEntfernen"])();
            GebaeudeImportStand ohne = Zuordnen(gaben, haken);
            Assert.Null(ohne.Projektdatei);
            Assert.All(ohne.Zonierung!.Plan!.Zonen, z => Assert.False(z.AusProjektdatei));
            GebaeudeImportStand abgelehnt = Zuordnen(gaben, haken, new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION));
            Assert.True(abgelehnt.Zonierung!.Plan!.LetzterAbgelehnt);
            Assert.Equal(SqprojProtokoll.NICHT_GELESEN, abgelehnt.Zonierung.Plan.Schrittmeldung!.Kennung);
            Assert.Null(h.Satz?.Projektdatei);
        }

        [Fact]
        public async Task Wahl_der_Zonierung_an_der_Huelle_Vorgabe_DIN_Wechsel_ohne_neues_Lesen()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            IReadOnlyDictionary<string, bool> haken = AlleBeheizt(gaben);
            GebaeudeProjektdateiDaten sq = await Dazuladen(gaben, SqprojProbenErzeuger.Zonenhaus());
            Assert.True(sq.Gelesen, sq.Ablehnung?.Text);
            Assert.True(sq.HatDinZonen);
            Assert.True(sq.HatSimulationszonen);
            Assert.True(sq.BeideZonierungen);
            Assert.Equal(GebaeudeZonierungSchluessel.DIN, sq.Gewaehlt);
            Assert.Equal(GebaeudeZonierungSchluessel.DIN, sq.Zonierung);
            Assert.Equal("DIN-V-18599-Zonen (3 Zonen)", sq.ZonierungText);

            // Vorgabe: die DIN-V-18599-Zonen, je Zone die Herkunft „aus Projektdatei (DIN-Zonen)“.
            GebaeudeImportStand din = Zuordnen(gaben, haken, new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI));
            GebaeudeZonenplanDaten plan = din.Zonierung!.Plan!;
            Assert.Equal(new[] { "Nutzung EG", "Nutzung Keller", "Nutzung OG" }, plan.Zonen.Select(z => z.Name));
            Assert.All(plan.Zonen, z => Assert.Equal(GebaeudeZonierungSchluessel.HERKUNFT_DIN, z.Herkunft));
            Assert.All(plan.Zonen, z => Assert.Equal("aus Projektdatei (DIN-Zonen)", z.HerkunftText));
            Assert.Contains(din.Projektdatei!.Meldungen, m => m.Kennung == SqprojProtokoll.ZONIERUNG
                                                             && m.Text == "Zonierung: DIN-V-18599-Zonen (3 Zonen); Simulationszonen vorhanden (3).");

            // Wechsel: derselbe Schritt mit der anderen Zonierung — gerechnet aus dem gelesenen Abbild, ohne neues Lesen.
            GebaeudeImportStand sim = Zuordnen(gaben, haken,
                new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION));
            Assert.Equal(new[] { "Keller", "Simulation EG", "Simulation OG" }, sim.Zonierung!.Plan!.Zonen.Select(z => z.Name));
            Assert.All(sim.Zonierung.Plan.Zonen, z => Assert.Equal(GebaeudeZonierungSchluessel.HERKUNFT_SIMULATION, z.Herkunft));
            Assert.Equal("aus Projektdatei (Simulationszonen)", sim.Zonierung.Plan.Zonen[0].HerkunftText);
            Assert.Equal(GebaeudeZonierungSchluessel.SIMULATION, sim.Projektdatei!.Zonierung);
            Assert.Equal(GebaeudeZonierungSchluessel.SIMULATION, sim.Projektdatei.Gewaehlt);
            Assert.Equal("Simulationszonen (3 Zonen)", sim.Projektdatei.ZonierungText);
            Assert.Equal(sq.Dateiname, sim.Projektdatei.Dateiname);
        }

        [Fact]
        public async Task Nur_eine_Zonierung_ohne_Wahl_und_Grenze_je_Plattform()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            SqprojProbenErzeuger nurDin = new SqprojProbenErzeuger()
                .Geschoss("FK", "Kellergeschoss").Geschoss("FE", "Erdgeschoss").Geschoss("FO", "Obergeschoss")
                .Raum("R1", "Lager", "FK", null, 20.0).Raum("R2", "Wohnen", "FE", null, 30.0)
                .Zone("Z1", "Haus", 5, null, "R1", "R2");
            GebaeudeProjektdateiDaten sq = await Dazuladen(gaben, nurDin);
            Assert.True(sq.Gelesen, sq.Ablehnung?.Text);
            Assert.True(sq.HatDinZonen);
            Assert.False(sq.HatSimulationszonen);
            Assert.False(sq.BeideZonierungen);
            GebaeudeImportStand stand = Zuordnen(gaben, AlleBeheizt(gaben),
                new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION));
            Assert.Equal(GebaeudeZonierungSchluessel.SIMULATION, stand.Projektdatei!.Gewaehlt);
            Assert.Equal(GebaeudeZonierungSchluessel.DIN, stand.Projektdatei.Zonierung);   // ohne Wahl die vorhandene
            Assert.Contains(stand.Projektdatei.Meldungen, m => m.Kennung == SqprojProtokoll.ZONIERUNG_EINE);

            Assert.Equal(SqprojProfil.MAX_BYTES, new GebaeudeImportHuelle(ios: false).ProjektdateiGrenze);
            Assert.Equal(SqprojProfil.MAX_BYTES_IOS, new GebaeudeImportHuelle(ios: true).ProjektdateiGrenze);
            Assert.Equal(SqprojProfil.MAX_BYTES_IOS, new GebaeudeImportHuelle(new IfcImportProfil(), ios: true).ProjektdateiGrenze);
        }

        [Fact]
        public async Task Eine_fehlende_Tabelle_ist_die_benannte_Ablehnung()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            GebaeudeProjektdateiDaten sq = await Dazuladen(gaben, SqprojProbenErzeuger.Zonenhaus().Ohne("BmZone"));
            Assert.False(sq.Gelesen);
            Assert.Equal(SqprojProtokoll.TABELLE_FEHLT, sq.Ablehnung!.Kennung);
            Assert.Equal(WarnStufe.Fehler, sq.Schwerste!.Stufe);
            Assert.Contains("BmZone", sq.Ablehnung.Text);
            Assert.Equal(sq.Ablehnung, Zuordnen(gaben, new Dictionary<string, bool>()).Projektdatei!.Ablehnung);
        }

        [Fact]
        public async Task Speichern_ueber_die_Huelle_schreibt_Kalender_mit_Beleg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            IReadOnlyDictionary<string, bool> haken = AlleBeheizt(gaben);
            Assert.True((await Dazuladen(gaben, SqprojProbenErzeuger.Zonenhaus())).Gelesen);
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION) };
            GebaeudeImportStand stand = Zuordnen(gaben, haken, schritte);

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            var ergebnis = new GebaeudeImportErgebnis(0, null, "Zonenhaus", haken, stand.Zeilen.ToList(), AlsZone: true, Zonenregel: "Z4",
                                                      Planschritte: schritte, Plangrundhaken: haken);
            IReadOnlyList<GebaeudeImportMeldung> befund = pruefen(ergebnis);
            Assert.DoesNotContain(befund, m => m.Stufe == WarnStufe.Fehler);
            GebaeudeBauteilvorschlag v = h.Herkunft!.Vorschlag!;
            Assert.Equal(3, v.Zonen.Count);
            Assert.All(v.Zonenkonditionierungen, k => Assert.NotNull(k));

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
            ZoneModel eg = zonen.Single(z => z.Bezeichner == "Simulation EG");
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Groesse\", \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" = ? ORDER BY \"Groesse\"",
                new DbParam("@z", eg.ID));
            Dictionary<string, string> bemerkung = t.Rows.Cast<DataRow>().ToDictionary(
                r => Convert.ToString(r[0], CultureInfo.InvariantCulture),
                r => r[1] is DBNull ? "" : Convert.ToString(r[1], CultureInfo.InvariantCulture));
            Assert.StartsWith("aus Projektdatei:", bemerkung[DbWerte.KOND_GROESSE_HEIZSOLL]);
            Assert.Contains("PdProfileTimeCurve.Temperature", bemerkung[DbWerte.KOND_GROESSE_HEIZSOLL]);
            Assert.Contains("PdProfileTimeCurve.Ratio", bemerkung[DbWerte.KOND_GROESSE_PERSONEN]);
            Assert.Equal(DbWerte.KOND_NUTZUNG_BUERO, ZonenplanCtrl.Nutzung(zonen.Single(z => z.Bezeichner == "Simulation OG").ID));
        }

        /// <summary>
        /// Einzonenweg mit Projektdatei (SQ-3): Mehrere belegte Zonen und eine Gebäudegruppe — das Gebäude nimmt die
        /// Konditionierung der Gebäudegruppe als Gebäudekalender (Eigentümer Gebäude ohne Zone), mit dem Beleg der Projektdatei;
        /// im Mehrzonenweg trägt die Herkunft keine. Ohne Gebäudegruppe und mit mehreren Zonen gibt es keine.
        /// </summary>
        [Fact]
        public async Task Einzonenweg_nimmt_die_Gebaeudegruppe_als_Gebaeudekalender()
        {
            using var db = new TestDatenbank();
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            IReadOnlyDictionary<string, bool> haken = AlleBeheizt(gaben);
            SqprojProbenErzeuger probe = SqprojProbenErzeuger.Zonenhaus();
            probe.Gebaeudegruppe = "G1";
            Assert.True((await Dazuladen(gaben, probe)).Gelesen);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            GebaeudeImportStand stand = Zuordnen(gaben, haken);
            pruefen(new GebaeudeImportErgebnis(0, null, "Zonenhaus", haken, stand.Zeilen.ToList()));
            Zonenkonditionierung k = h.Herkunft!.Gebaeudekonditionierung;
            Assert.NotNull(k);
            Assert.Equal(Konditionierungsherkunft.Ganglinie, k.Groesse(Konditionierungsgroesse.Heizsoll).Herkunft);
            Assert.Equal(RaumnutzungSaat.WOHNEN, k.Nutzung);   // NP-F14: der Profilname der Zuordnung DIN_NUMMER

            // Im Mehrzonenweg mit den Zonen der Projektdatei: keine Gebäudekonditionierung.
            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI, Zonierung: GebaeudeZonierungSchluessel.SIMULATION) };
            GebaeudeImportStand mehr = Zuordnen(gaben, haken, schritte);
            pruefen(new GebaeudeImportErgebnis(0, null, "Zonenhaus", haken, mehr.Zeilen.ToList(), AlsZone: true, Zonenregel: "Z4",
                                               Planschritte: schritte, Plangrundhaken: haken));
            Assert.Null(h.Herkunft!.Gebaeudekonditionierung);

            if (!db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(PROJEKT);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.Null(ZonenplanCtrl.ProjektdateiUebernehmen(g.ID_Gebaeude, null, k));
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Groesse\", \"Bemerkung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL ORDER BY \"Groesse\"",
                new DbParam("@g", g.ID_Gebaeude));
            Dictionary<string, string> bemerkung = t.Rows.Cast<DataRow>().ToDictionary(
                r => Convert.ToString(r[0], CultureInfo.InvariantCulture),
                r => r[1] is DBNull ? "" : Convert.ToString(r[1], CultureInfo.InvariantCulture));
            Assert.StartsWith("aus Projektdatei:", bemerkung[DbWerte.KOND_GROESSE_HEIZSOLL]);
            Assert.Contains("PdProfileTimeCurve.Temperature", bemerkung[DbWerte.KOND_GROESSE_HEIZSOLL]);
        }

        [Fact]
        public void Der_iOS_Dateifilter_bietet_die_Projektdatei_an()
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.iOS", "Dienste", "Dateifilter.cs"));
            string code = Regex.Replace(text, @"//[^\r\n]*", "");
            Assert.Matches(@"\[""\.sqproj""\]\s*=\s*ALLES", code);
            Assert.EndsWith("|*.sqproj", WindowsFormsApplication1.MyResource.Resource.GIMP_DLG_SQ_FILTER);
        }

        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
