using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle D1 — die Datenseite von Grundriss und Umhängen</b> (<see cref="GebaeudeImportHuelle"/>,
    /// <see cref="GebaeudeImportAnsicht"/>), ohne Projekt und ohne Datenbank: Der Stand trägt den Grundriss je
    /// Geschoss aus dem Zonengeometrie-Modell (Räume mit Zonenschlüssel, Herkunft als Wert, Hinweise), die
    /// Zonenliste nennt je Zone ihren Schlüssel, und eine Zuordnung von Hand reist in der Anfrage bis zum
    /// Vorschlag und in die Herkunft — gespeichert wird weiter nur über den Weg der Gebäudeliste.
    /// </summary>
    public sealed class GebaeudeImportAnsichtHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private static async Task<(GebaeudeImportHuelle Huelle, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> Zuordnen)> Gelesen(string probe)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return (h, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"]);
        }

        private static GebaeudeAnsichtRaum Raum(GebaeudeAnsichtDaten ansicht, string name)
            => Assert.Single(ansicht.Geschosse.SelectMany(g => g.Raeume), r => r.Name == name);

        [Fact]
        public async Task Der_Stand_traegt_den_Grundriss_je_Geschoss_mit_Zonenschluesseln_und_Herkunft()
        {
            (GebaeudeImportHuelle h, var zuordnen) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, null, Keine));

            GebaeudeAnsichtDaten ansicht = Assert.IsType<GebaeudeAnsichtDaten>(stand.Ansicht);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, ansicht.Geschosse.Select(g => g.Name));
            Assert.Equal(new[] { 1, 2, 3 }, ansicht.Geschosse.Select(g => g.Raeume.Count));
            Assert.True(ansicht.Schematisch);
            Assert.True(ansicht.Umhaengbar);
            Assert.Equal(new[] { true, false, false }, ansicht.Geschosse.Select(g => g.Schematisch));

            // Die Zonen der Ansicht sind die der Zonenliste — Schlüssel und Name gleich.
            GebaeudeZonierungDaten zon = stand.Zonierung!;
            Assert.Equal(zon.Zonen.Select(z => z.Schluessel), ansicht.Zonen.Select(z => z.Schluessel));
            Assert.Equal(zon.Zonen.Select(z => z.Name), ansicht.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { 0, 1, 2 }, ansicht.Zonen.Select(z => z.Stelle));
            Assert.All(zon.Zonen, z => Assert.StartsWith("Z4|", z.Schluessel, StringComparison.Ordinal));
            Assert.All(zon.Zonen, z => Assert.False(z.VonHand));

            // Räume: Zone als Schlüssel, Herkunft als Wert, Polygone in Metern, Fläche als Anzeigetext.
            GebaeudeAnsichtRaum lager = Raum(ansicht, "Lager");
            Assert.True(lager.Schematisch);
            Assert.False(lager.Beheizt);
            Assert.Equal(ansicht.Zonen[0].Schluessel, lager.Zone);
            Assert.Equal("80 m²", lager.Flaeche);
            GebaeudeAnsichtRaum bad = Raum(ansicht, "Bad");
            Assert.False(bad.Schematisch);
            Assert.Equal(ansicht.Zonen[2].Schluessel, bad.Zone);
            Assert.Equal(new[] { new GebaeudeAnsichtPunkt(6, 0), new GebaeudeAnsichtPunkt(10, 0), new GebaeudeAnsichtPunkt(10, 7.5), new GebaeudeAnsichtPunkt(6, 7.5) },
                         Assert.Single(bad.Polygone));

            // Die Hinweise der Geometrie in der Anzeigesprache.
            Assert.Contains(ansicht.Hinweise, t => t.StartsWith("1 Räume ohne Umriss aus Raumgrenzen", StringComparison.Ordinal) && t.EndsWith("Lager", StringComparison.Ordinal));
            Assert.NotNull(h.Geometrie);
        }

        [Fact]
        public async Task Ein_Klick_haengt_um_und_der_Stand_bildet_Zonen_Bilanz_Vorschlag_und_Grundriss_neu()
        {
            (GebaeudeImportHuelle h, var zuordnen) = await Gelesen("ifc4_zonen.ifc");
            var anfrage = new GebaeudeZuordnungsanfrage(0, 4, Keine);
            GebaeudeImportStand vorher = zuordnen(anfrage);
            GebaeudeAnsichtRaum kueche = Raum(vorher.Ansicht!, "Küche");

            // Der Klick: Raumkennung und Zonenschlüssel, nie ein Name — hier „als eigene Zone abtrennen".
            anfrage = anfrage.MitUmhaengung(kueche.Kennung, null);
            GebaeudeImportStand stand = zuordnen(anfrage);

            GebaeudeZonierungDaten zon = stand.Zonierung!;
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Küche" }, zon.Zonen.Select(z => z.Name));
            Assert.Equal(new[] { false, true, false, true }, zon.Zonen.Select(z => z.VonHand));
            Assert.Equal("4", zon.Bilanz.Zonen);
            Assert.Equal(4, h.Vorschlag.Zonen.Count);
            Assert.Equal(4, h.Zonierung.Zonen.Count);

            // Der Grundriss zeigt die Küche in der neuen Zone; die Polygone bleiben, wo sie waren.
            GebaeudeAnsichtRaum neu = Raum(stand.Ansicht!, "Küche");
            Assert.Equal(zon.Zonen[3].Schluessel, neu.Zone);
            Assert.Equal(kueche.Polygone, neu.Polygone);
            Assert.True(stand.Ansicht!.Zonen[3].VonHand);

            // Eine zweite Zuordnung: die Küche zurück ins Erdgeschoss über dessen Schlüssel — die Handzone entfällt.
            GebaeudeImportStand zurueck = zuordnen(anfrage.MitUmhaengung(kueche.Kennung, zon.Zonen[1].Schluessel));
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, zurueck.Zonierung!.Zonen.Select(z => z.Name));
            Assert.Equal(2, anfrage.MitUmhaengung(kueche.Kennung, zon.Zonen[1].Schluessel).Umhaengungen!.Count);
        }

        [Fact]
        public async Task Die_Zuordnung_von_Hand_reist_bis_zum_Ergebnis_und_in_die_Herkunft()
        {
            (GebaeudeImportHuelle h, var zuordnen) = await Gelesen("ifc4_zonen.ifc");
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, 4, Keine));
            string kueche = Raum(stand.Ansicht!, "Küche").Kennung;
            var umhaengungen = new[] { new GebaeudeRaumumhaengung(kueche, null) };

            var ergebnis = new GebaeudeImportErgebnis(0, 4, "Zonenhaus", Keine, stand.Zeilen.ToList(), AlsZone: true,
                                                       Zonenregel: "Z4", Umhaengungen: umhaengungen);
            h.SatzAusErgebnis(ergebnis);
            Assert.True(h.AlsZone);
            GebaeudeImportHerkunft herkunft = h.Herkunft;
            Assert.NotNull(herkunft);
            GebaeudeBauteilvorschlag v = herkunft.Vorschlag;
            Assert.True(v.Mehrzonig);
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss", "Küche" }, v.Zonen.Select(z => z.Bezeichner));
            Assert.Equal(-4, Assert.Single(v.Raeume, p => p.Quellkennung == kueche).ZielId);
            // Die Quelle merkt weiter die Regel; die Zuordnung von Hand steht in den Raumpaarungen.
            Assert.Equal("Z4", herkunft.Quelle.Zonenregel);

            // Ohne Zuordnung im Ergebnis bleibt es bei drei Zonen: Die Hülle merkt sich nichts zwischen den Anfragen.
            h.SatzAusErgebnis(ergebnis with { Umhaengungen = null });
            Assert.Equal(3, h.Herkunft.Vorschlag.Zonen.Count);
        }

        [Fact]
        public async Task Unter_einer_Zone_je_Gebaeude_zeigt_der_Grundriss_alle_Raeume_und_haengt_nicht_um()
        {
            (GebaeudeImportHuelle h, var zuordnen) = await Gelesen("ifc4_zonen.ifc");
            var anfrage = new GebaeudeZuordnungsanfrage(0, null, Keine, Zonenregel: IfcImportProfil.ZONENREGEL_Z5);
            GebaeudeImportStand stand = zuordnen(anfrage);

            GebaeudeAnsichtDaten ansicht = stand.Ansicht!;
            Assert.False(ansicht.Umhaengbar);
            Assert.Single(ansicht.Zonen);
            Assert.Null(Raum(ansicht, "Lager").Zone);
            Assert.Equal(ansicht.Zonen[0].Schluessel, Raum(ansicht, "Wohnen").Zone);
            Assert.Equal(6, ansicht.Geschosse.Sum(g => g.Raeume.Count));

            // Eine Zuordnung von Hand ändert hier nichts und steht benannt im Kern.
            zuordnen(anfrage.MitUmhaengung(Raum(ansicht, "Küche").Kennung, null));
            Assert.Single(h.Zonierung.Zonen);
            Assert.Contains(h.Zonierung.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_" + GebaeudeZonierung.UMHAENGEN_EINZONIG);
        }

        [Fact]
        public async Task Die_Zellen_stehen_schematisch_und_die_Texte_gibt_es_englisch()
        {
            (GebaeudeImportHuelle _, var zuordnen) = await Gelesen("gbxml_zonen_viele.xml");
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, null, Keine, Zonenregel: GebaeudeImportProfil.ZONENREGEL_X2));
            GebaeudeAnsichtDaten ansicht = stand.Ansicht!;
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss" }, ansicht.Geschosse.Select(g => g.Name));
            Assert.All(ansicht.Geschosse.SelectMany(g => g.Raeume), r =>
            {
                Assert.True(r.Schematisch);
                Assert.Equal("10 m²", r.Flaeche);
                Assert.Equal(4, Assert.Single(r.Polygone).Count);
            });
            Assert.Equal(2, ansicht.Hinweise.Count);

            using (new Kulturvorrichtung("en-US"))
            {
                GebaeudeImportStand en = zuordnen(new GebaeudeZuordnungsanfrage(0, null, Keine, Zonenregel: GebaeudeImportProfil.ZONENREGEL_X2));
                Assert.Contains(en.Ansicht!.Hinweise, t => t.StartsWith("60 rooms without an outline", StringComparison.Ordinal));
                Assert.Contains(en.Ansicht.Hinweise, t => t.StartsWith("60 rooms without wall areas", StringComparison.Ordinal));
            }
        }
    }
}
