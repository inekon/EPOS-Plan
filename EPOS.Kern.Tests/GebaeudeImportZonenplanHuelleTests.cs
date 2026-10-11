using System;
using System.Collections.Generic;
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
    /// <b>Der Zonenplan an der Hülle</b> (Zonenbaum, Mehrzonenkonzept 6.4) mit dem Zonenhaus (Z4): ohne Schritt der Plan aus der
    /// Regel neben der unveränderten Zonierung der Regel; Schritte in der Anfrage legt die Hülle auf den Regelvorschlag und
    /// zoniert aus dem Plan; die Ablehnung des letzten Schritts steht im Stand; ein Plan mit nicht zugeordneten Räumen besteht
    /// die Prüfung nicht; ein Raumhaken nach dem ersten Schritt lässt die Schlüssel der Zonen stehen.
    /// </summary>
    public sealed class GebaeudeImportZonenplanHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private static async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben)> Gelesen()
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe("ifc4_zonen.ifc"), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return (h, gaben);
        }

        private static GebaeudeImportStand Zuordnen(IReadOnlyDictionary<string, object> gaben, IReadOnlyDictionary<string, bool> haken = null,
                                                    IReadOnlyDictionary<string, bool> grundhaken = null, params GebaeudePlanschritt[] schritte)
            => ((Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"])(
                new GebaeudeZuordnungsanfrage(0, 4, haken ?? Keine, null, null, "Z4", null, false,
                                              schritte.Length > 0 ? schritte : null, grundhaken));

        private static string Raum(GebaeudeZonenplanDaten plan, string name)
            => plan.Zonen.SelectMany(z => z.Raumliste).Concat(plan.NichtZugeordnet).Single(r => r.Name == name).Kennung;

        [Fact]
        public async Task Ohne_Schritt_steht_der_Plan_aus_der_Regel_neben_der_Zonierung_der_Regel()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            GebaeudeImportStand stand = Zuordnen(gaben);
            GebaeudeZonierungDaten zon = stand.Zonierung!;
            GebaeudeZonenplanDaten plan = zon.Plan!;

            Assert.Equal(new[] { "Z:1", "Z:2", "Z:3" }, plan.Zonen.Select(z => z.Schluessel));
            Assert.Equal(zon.Zonen.Select(z => z.Name), plan.Zonen.Select(z => z.Name));
            Assert.Equal(zon.Zonen.Select(z => z.Schluessel), plan.Zonen.Select(z => z.Ansichtsschluessel));
            Assert.All(plan.Zonen, z => Assert.Null(z.Nutzung));
            Assert.Empty(plan.NichtZugeordnet);
            Assert.False(plan.Einzonig);
            Assert.Equal("Zone 4", plan.NeuerName);
            Assert.Equal(new[] { "WOHNEN", "BUERO", "SCHULE" }, plan.Nutzungen.Select(n => n.Schluessel));
            Assert.Equal(3, plan.Geschosse.Count);
            Assert.Null(plan.Schrittmeldung);
            Assert.True(stand.Ansicht!.Umhaengbar);
        }

        [Fact]
        public async Task Schritte_legen_Zone_und_Zuordnung_auf_und_der_Plan_reist_zur_Herkunft()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            string kueche = Raum(Zuordnen(gaben).Zonierung!.Plan!, "Küche");
            var schritte = new[]
            {
                new GebaeudePlanschritt(GebaeudePlanschrittArt.ANLEGEN, Name: "Küche", Nutzung: "WOHNEN"),
                new GebaeudePlanschritt(GebaeudePlanschrittArt.ZUORDNEN, new[] { kueche }, "Z:4"),
            };
            GebaeudeImportStand stand = Zuordnen(gaben, null, Keine, schritte);
            GebaeudeZonenplanDaten plan = stand.Zonierung!.Plan!;
            Assert.False(plan.LetzterAbgelehnt);
            Assert.Equal(4, stand.Zonierung.Zonen.Count);
            Assert.Equal("Z:4", plan.Zonen[3].Ansichtsschluessel);
            Assert.Equal("WOHNEN", plan.Zonen[3].Nutzung);
            Assert.Equal(kueche, Assert.Single(plan.Zonen[3].Raumliste).Kennung);
            Assert.Equal("Z:4", stand.Ansicht!.Raum(kueche)!.Zone);

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            var ergebnis = new GebaeudeImportErgebnis(0, 4, "Zonenhaus", Keine, stand.Zeilen.ToList(), AlsZone: true, Zonenregel: "Z4",
                                                      Planschritte: schritte, Plangrundhaken: Keine);
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == WarnStufe.Fehler);
            Assert.Equal(4, h.Herkunft!.Vorschlag!.Zonen.Count);
            Assert.NotNull(h.Herkunft.Vorschlag.Zonierung.Plan);
            Assert.Equal("Z4", h.Herkunft.Quelle.Zonenregel);
        }

        [Fact]
        public async Task Der_abgelehnte_letzte_Schritt_steht_im_Stand_und_ein_frueherer_zaehlt_als_verworfen()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            GebaeudeZonenplanDaten vorher = Zuordnen(gaben).Zonierung!.Plan!;
            string lager = Raum(vorher, "Lager");
            var falsch = new GebaeudePlanschritt(GebaeudePlanschrittArt.ZUORDNEN, new[] { lager }, "Z:2");

            GebaeudeZonenplanDaten plan = Zuordnen(gaben, null, Keine, falsch).Zonierung!.Plan!;
            Assert.True(plan.LetzterAbgelehnt);
            Assert.Equal("IMP_IFC_PROT_PLAN_BEHEIZUNG", plan.Schrittmeldung!.Kennung);
            Assert.Equal(WarnStufe.Fehler, plan.Schrittmeldung.Stufe);
            Assert.Contains(plan.Zonen[0].Raumliste, r => r.Kennung == lager);

            plan = Zuordnen(gaben, null, Keine, falsch, new GebaeudePlanschritt(GebaeudePlanschrittArt.NUTZUNG, Zone: "Z:2", Nutzung: "BUERO"))
                .Zonierung!.Plan!;
            Assert.False(plan.LetzterAbgelehnt);
            Assert.Equal(1, plan.Verworfen);
            Assert.Equal("BUERO", plan.Zonen[1].Nutzung);
        }

        [Fact]
        public async Task Nicht_zugeordnete_Raeume_bestehen_die_Pruefung_nicht_bis_der_Rest_nach_Regel_zugeordnet_ist()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            var aufheben = new GebaeudePlanschritt(GebaeudePlanschrittArt.AUFHEBEN);
            GebaeudeImportStand stand = Zuordnen(gaben, null, Keine, aufheben);
            GebaeudeZonenplanDaten plan = stand.Zonierung!.Plan!;
            Assert.Empty(plan.Zonen);
            Assert.NotEmpty(plan.NichtZugeordnet);

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            var ergebnis = new GebaeudeImportErgebnis(0, 4, "Zonenhaus", Keine, stand.Zeilen.ToList(), Zonenregel: "Z4",
                                                      Planschritte: new[] { aufheben }, Plangrundhaken: Keine);
            Assert.Contains(pruefen(ergebnis), m => m.Stufe == WarnStufe.Fehler && m.Kennung == "IMP_IFC_PROT_" + Zonenplan.ZUORDNUNG_UNVOLLSTAENDIG);

            var rest = new GebaeudePlanschritt(GebaeudePlanschrittArt.REST, Regel: "Z4");
            Assert.Empty(Zuordnen(gaben, null, Keine, aufheben, rest).Zonierung!.Plan!.NichtZugeordnet);
            Assert.DoesNotContain(pruefen(ergebnis with { Planschritte = new[] { aufheben, rest } }),
                                  m => m.Kennung == "IMP_IFC_PROT_" + Zonenplan.ZUORDNUNG_UNVOLLSTAENDIG);
        }

        [Fact]
        public async Task Ein_Raumhaken_nach_dem_ersten_Schritt_laesst_die_Schluessel_stehen_und_oeffnet_die_Zuordnung()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen();
            string lager = Raum(Zuordnen(gaben).Zonierung!.Plan!, "Lager");
            var warm = new Dictionary<string, bool> { [lager] = true };
            var schritte = new[]
            {
                new GebaeudePlanschritt(GebaeudePlanschrittArt.ANLEGEN, Name: "Neu"),
                new GebaeudePlanschritt(GebaeudePlanschrittArt.HAKEN, new[] { lager }, Beheizt: true),
                new GebaeudePlanschritt(GebaeudePlanschrittArt.ZUORDNEN, new[] { lager }, "Z:2"),
            };
            GebaeudeZonenplanDaten plan = Zuordnen(gaben, warm, Keine, schritte).Zonierung!.Plan!;
            Assert.False(plan.LetzterAbgelehnt);
            Assert.Equal(0, plan.Verworfen);
            Assert.Equal(new[] { "Z:1", "Z:2", "Z:3", "Z:4" }, plan.Zonen.Select(z => z.Schluessel));
            Assert.Contains(plan.Zonen[1].Raumliste, r => r.Kennung == lager && r.Beheizt);
            Assert.Null(plan.Zonen[0].Beheizt);   // das Kellergeschoss steht leer
        }
    }
}
