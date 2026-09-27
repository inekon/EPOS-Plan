using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Die Regel „eine Zone je Gebäude" des Formats einer Probe (X4 bzw. Z5) — der Einzonenweg aus G4b.
    /// Die Fälle, die ihn prüfen, wählen sie ausdrücklich: Die Vorgabe der Datei ist je Geschoss (M7).
    /// </summary>
    internal static class Einzonenregel
    {
        internal static string Fuer(string probe)
            => probe.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? GebaeudeImportProfil.ZONENREGEL_X4 : IfcImportProfil.ZONENREGEL_Z5;
    }

    /// <summary>
    /// <b>Stufe G6c, Welle C — die Datenseite des Zuordnungsdialogs mit mehreren Zonen</b>
    /// (<see cref="GebaeudeImportHuelle"/>, <see cref="GebaeudeImportZonen"/>), ohne Projekt und ohne
    /// Datenbank: die wählbaren Regeln mit der Vorgabe je Geschoss (M7), die Bilanz, die Zonen samt Räumen,
    /// die Flächen je Zone mit ihren Befunden, die Obergrenze mit dem Vorschlag einer gröberen Regel (M12),
    /// der Einzonenweg unverändert (eine Regel, eine Zone), die Regel am Prüfweg und in der Herkunft, und
    /// die englischen Texte.
    /// </summary>
    public sealed class GebaeudeImportZonenHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private static async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben)> Gelesen(string probe)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return (h, gaben);
        }

        private static GebaeudeImportStand Zuordnen(IReadOnlyDictionary<string, object> gaben, string regel = null, int? klasse = null,
                                                    IReadOnlyDictionary<string, bool> haken = null)
            => ((Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"])(
                new GebaeudeZuordnungsanfrage(0, klasse, haken ?? Keine, null, null, regel));

        // ==================================================================
        //  Regel, Bilanz, Zonen
        // ==================================================================

        [Fact]
        public async Task Die_Vorgabe_je_Geschoss_bildet_drei_Zonen_mit_Bilanz_und_Raeumen()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeImportStand stand = Zuordnen(gaben);

            GebaeudeZonierungDaten zon = Assert.IsType<GebaeudeZonierungDaten>(stand.Zonierung);
            Assert.Equal(new[] { "Z1", "Z2", "Z4", "Z5" }, zon.Regeln.Select(r => r.Schluessel));
            Assert.Equal("Z4", zon.Regel);
            Assert.Equal("Z4 – eine Zone je Geschoss", zon.RegelText);
            Assert.False(zon.Einzonig);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, zon.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { false, true, true }, zon.Zonen.Select(z => z.Beheizt));
            Assert.All(zon.Zonen, z => Assert.Equal("Z4", z.Regel));
            Assert.Equal("80 m²", zon.Zonen[1].Flaeche);

            // Die Bilanz: drei Zonen, beheizt EG und OG, Außen- und Trennfläche aus den Zeilen des Vorschlags.
            Assert.Equal("3", zon.Bilanz.Zonen);
            Assert.Equal("159,5 m²", zon.Bilanz.BeheizteFlaeche);
            double trenn = h.Vorschlag.Zeilen.Where(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).Sum(z => z.Bauteil.Flaeche);
            Assert.True(trenn > 0.0);
            Assert.Equal(GebaeudeZuordnungsModell.ZahlText(Math.Round(trenn, 2)) + " m²", zon.Bilanz.Trennflaeche);
            Assert.NotEqual("—", zon.Bilanz.Aussenflaeche);

            // Aufgeklappt: die Räume mit Geschoss, Fläche und Beheizungsregel samt Beleg.
            GebaeudeZonenraumDaten lager = Assert.Single(zon.Zonen[0].Raumliste, r => r.Name == "Lager");
            Assert.Equal("B5", lager.Beheizungsregel);
            Assert.Equal("Kellergeschoss", lager.Geschoss);
            Assert.False(string.IsNullOrEmpty(lager.Beleg));
            Assert.False(lager.BeheiztLautDatei);

            // Der Vorschlag der Hülle ist der mehrerer Zonen; der Kopf der Bauteile nennt sie.
            Assert.True(h.Vorschlag.Mehrzonig);
            Assert.StartsWith("3 Zonen", stand.Bauteile!.Kopftext);
            Assert.True(stand.Bauteile.Moeglich, stand.Bauteile.Ablehnung);
        }

        [Fact]
        public async Task Die_Flaechen_je_Zone_tragen_Zone_Nachbarzone_Azimut_und_Befund()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeZonierungDaten zon = Zuordnen(gaben)!.Zonierung!;

            Assert.NotNull(zon.Flaechenprofil);
            Assert.Equal(12, zon.Flaechenprofil!.Spalten.Count);
            Assert.Equal(h.Vorschlag.Zeilen.Count, zon.Flaechen.Count);
            Assert.Equal(zon.Flaechen.Count, zon.Flaechen.Select(f => f.Zeile.Schluessel).Distinct().Count());

            Katalogfilterzeile decke = zon.Flaechen.Select(f => f.Zeile)
                .First(z => z.Bezeichner == "Geschossdecke" && z.Text(GebaeudeImportZonen.SP_RAND) == "Nachbarzone");
            Assert.Equal("Erdgeschoss", decke.Text(GebaeudeImportZonen.SP_ZONE));
            Assert.Equal("Obergeschoss", decke.Text(GebaeudeImportZonen.SP_NACHBARZONE));

            // Der Azimut steht mit einer Nachkommastelle.
            Katalogfilterzeile fassade = zon.Flaechen.Select(f => f.Zeile).First(z => z.Bezeichner == "Fassade Süd");
            Assert.Matches(@"^\d+,\d$", fassade.Text(GebaeudeImportZonen.SP_AZIMUT));

            // Unter Z4 hat jede innere Grenze ihr Gegenstück.
            Assert.DoesNotContain(zon.Flaechen, f => f.OhneGegenstueck);
        }

        [Fact]
        public async Task Nach_den_Zonen_der_Datei_nennen_die_Flaechen_ohne_Gegenstueck_ihren_Befund()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeZonierungDaten zon = Zuordnen(gaben, "Z1")!.Zonierung!;

            Assert.Equal("Z1", zon.Regel);
            Assert.Equal(4, zon.Zonen.Count);
            Assert.Contains("ohne Zone", Assert.Single(zon.Zonen, z => z.Name == "Obergeschoss").Hinweis);   // zugeschlagen
            List<GebaeudeFlaechenzeileDaten> ohne = zon.Flaechen.Where(f => f.OhneGegenstueck).ToList();
            Assert.NotEmpty(ohne);
            Assert.All(ohne, f =>
            {
                Assert.True(f.Fehler);
                Assert.Contains("ohne Gegenstück", f.Zeile.Text(GebaeudeImportZonen.SP_BEFUND));
                Assert.Equal("unbeheizter Raum", f.Zeile.Text(GebaeudeImportZonen.SP_RAND));
            });
            Assert.All(zon.Flaechen.Where(f => !f.Fehler), f => Assert.Equal(Katalogwert.Leer.Text, f.Zeile.Text(GebaeudeImportZonen.SP_BEFUND)));
        }

        [Fact]
        public async Task Ohne_Klasse_nennen_Flaechen_ohne_U_Wert_ihren_Befund()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen("gbxml_haus_si.xml");
            GebaeudeZonierungDaten zon = Zuordnen(gaben)!.Zonierung!;
            Assert.Equal("X1", zon.Regel);
            Assert.False(zon.Einzonig);
            foreach (GebaeudeFlaechenzeileDaten f in zon.Flaechen)
            {
                bool leer = f.Zeile.Text(GebaeudeImportZonen.SP_UWERT) == WindowsFormsApplication1.MyResource.Resource.GIMP_WERT_LEER;
                Assert.Equal(leer, f.OhneUWert);
                if (f.OhneUWert) Assert.Contains("ohne U-Wert", f.Zeile.Text(GebaeudeImportZonen.SP_BEFUND));
            }
        }

        [Fact]
        public async Task Eine_Regel_die_das_Gebaeude_nicht_traegt_heisst_die_Vorgabe()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            Assert.Equal("Z4", Zuordnen(gaben, "Z3")!.Zonierung!.Regel);
            Assert.Equal("Z4", Zuordnen(gaben, "X2")!.Zonierung!.Regel);
        }

        // ==================================================================
        //  Obergrenze (M12)
        // ==================================================================

        [Fact]
        public async Task Ueber_der_Obergrenze_warnt_die_Zonierung_und_schlaegt_die_Geschossregel_vor()
        {
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen("gbxml_zonen_viele.xml");
            GebaeudeImportStand stand = Zuordnen(gaben, "X3", klasse: 7);
            GebaeudeZonierungDaten zon = stand.Zonierung!;

            Assert.True(zon.ZuViele);
            Assert.Equal("X2", zon.Vorschlagsregel);
            Assert.Equal("X2 – eine Zone je Geschoss", zon.VorschlagsregelText);
            Assert.Equal(60, zon.Zonen.Count);
            Assert.Equal("60", zon.Bilanz.Zonen);
            GebaeudeImportMeldung m = Assert.IsType<GebaeudeImportMeldung>(zon.Schwerste);
            Assert.Equal(WarnStufe.Warnung, m.Stufe);
            Assert.EndsWith("ZU_VIELE_ZONEN_VORSCHLAG", m.Kennung);
            Assert.Contains("X2", m.Text);
            Assert.Empty(zon.Flaechen);                 // der Vorschlag lehnt so viele Zonen ab
            Assert.False(stand.Bauteile!.Moeglich);

            // Die gröbere Regel: zwei Zonen, übernehmbar.
            GebaeudeImportStand grob = Zuordnen(gaben, zon.Vorschlagsregel, klasse: 7);
            Assert.False(grob.Zonierung!.ZuViele);
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, grob.Zonierung.Zonen.Select(z => z.Name));
            Assert.True(grob.Bauteile!.Moeglich, grob.Bauteile.Ablehnung);
            Assert.Equal(120, grob.Zonierung.Flaechen.Count);
        }

        // ==================================================================
        //  Einzonenweg unverändert
        // ==================================================================

        [Fact]
        public async Task Mit_nur_einer_Regel_gibt_es_keine_Zonierung_und_der_Vorschlag_ist_der_aus_G4b()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_schichten.ifc");
            GebaeudeImportStand stand = Zuordnen(gaben);
            Assert.Null(stand.Zonierung);
            Assert.False(h.Vorschlag.Mehrzonig);
            Assert.Equal(GebaeudeBauteilvorschlag.Bilden(BauteilvorschlagProbe.Lesen("ifc4_schichten.ifc"), 0, null).Zeilen.Select(z => z.ToString()),
                         h.Vorschlag.Zeilen.Select(z => z.ToString()));
            Assert.StartsWith("Zone „", stand.Bauteile!.Kopftext);
        }

        [Fact]
        public async Task Die_Regel_mit_einer_Zone_ist_der_Einzonenweg_ohne_Zonen_und_Flaechenliste()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeImportStand stand = Zuordnen(gaben, "Z5");
            GebaeudeZonierungDaten zon = stand.Zonierung!;
            Assert.True(zon.Einzonig);
            Assert.Equal("Z5", zon.Regel);
            Assert.Null(zon.Flaechenprofil);
            Assert.Empty(zon.Flaechen);
            Assert.Equal("1", zon.Bilanz.Zonen);
            Assert.Equal("0 m²", zon.Bilanz.Trennflaeche);
            Assert.False(h.Vorschlag.Mehrzonig);
            Assert.Equal(GebaeudeBauteilvorschlag.Bilden(BauteilvorschlagProbe.Lesen("ifc4_zonen.ifc"), 0, null).Zeilen.Select(z => z.ToString()),
                         h.Vorschlag.Zeilen.Select(z => z.ToString()));
        }

        // ==================================================================
        //  Prüfweg und Herkunft
        // ==================================================================

        [Fact]
        public async Task Pruefen_und_Herkunft_tragen_die_gewaehlte_Regel()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeImportStand stand = Zuordnen(gaben, "Z4", klasse: 4);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];

            var ergebnis = new GebaeudeImportErgebnis(0, 4, "Zonenhaus", Keine, stand.Zeilen.ToList(), AlsZone: true, Zonenregel: "Z4");
            Assert.DoesNotContain(pruefen(ergebnis), m => m.Stufe == WarnStufe.Fehler && m.Kennung == GebaeudeImportHuelle.ALS_ZONE_NICHT);
            Assert.True(h.AlsZone);
            GebaeudeImportHerkunft herkunft = h.Herkunft!;
            Assert.Equal(3, herkunft.Vorschlag!.Zonen.Count);
            Assert.Equal("Z4", herkunft.Quelle.Zonenregel);
            Assert.Equal(h.Quelle.Hash, herkunft.Quelle.Hash);

            // Die Regel mit einer Zone: der Einzonenweg, die Quelle trägt die Regel des Profils.
            pruefen(ergebnis with { Zonenregel = "Z5" });
            Assert.Single(h.Herkunft!.Vorschlag!.Zonen);
            Assert.Equal(h.Quelle.Zonenregel, h.Herkunft.Quelle.Zonenregel);
        }

        // ==================================================================
        //  Englisch
        // ==================================================================

        [Fact]
        public async Task Regeln_Profil_und_Befunde_stehen_auch_englisch()
        {
            using var en = new Kulturvorrichtung("en-US");
            (_, IReadOnlyDictionary<string, object> gaben) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeZonierungDaten zon = Zuordnen(gaben, "Z1")!.Zonierung!;
            Assert.Equal("Z1 – one zone per zone of the file", zon.RegelText);
            Assert.Equal(new[] { "Zone", "Component", "Adjacent zone", "Finding" },
                         new[] { GebaeudeImportZonen.SP_ZONE, Katalogfilterprofil.SpBezeichner, GebaeudeImportZonen.SP_NACHBARZONE, GebaeudeImportZonen.SP_BEFUND }
                             .Select(s => zon.Flaechenprofil!.Spalte(s).Titel));
            Assert.Contains(zon.Flaechen, f => f.Zeile.Text(GebaeudeImportZonen.SP_BEFUND).Contains("no counterpart"));
        }
    }
}
