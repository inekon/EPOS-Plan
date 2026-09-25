using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G4b, Welle B — der Bauteilvorschlag am Gebäudeimport</b>, ohne Datenbank: das Zielfeld
    /// Innenflächenfaktor (dieselbe Messung wie der Innenweg des Vorschlags, leer = Vorgabe 2,5) und
    /// seine Abbildung auf den Gebäudeeditor.
    /// </summary>
    public sealed class GebaeudeImportBauteileTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int KLASSE_E = 4;

        private static void Nah(double erwartet, double? ist, double toleranz = 1e-9) => BauteilvorschlagProbe.Nah(erwartet, ist, toleranz);

        // =================================================================================
        //  Zielfeld Innenflächenfaktor
        // =================================================================================

        [Fact]
        public void Das_Zielfeld_Innenflaechenfaktor_traegt_die_gemessene_Innenflaeche_je_Nutzflaeche()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml");
            GebaeudeImportSatz s = a.Zuordnen(0, null);
            GebaeudeFeldzeile f = s.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);

            // Innenwand und zwei Geschossdecken, je beide Seiten: 145 m² ÷ 120 m² Nutzfläche.
            Nah(145.0 / 120.0, f.Wert);
            Assert.Equal(Importherkunft.GbXml, f.Herkunft);
            Assert.True(f.Uebernehmen);
            Assert.True(f.Eingebbar);
            Assert.Null(f.Markierung);
            Assert.Equal("–", f.Einheit);
            Assert.Equal(GebaeudeZielfelder.GRUPPE_KENNGROESSEN, f.Gruppe);
            Assert.Equal(GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR, f.VorgabeWert);
            Assert.Equal("Innenflächenfaktor", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.INNENFLAECHENFAKTOR));
            Assert.Equal("Innenfläche beider Seiten 145 m² (3 Trennflächen) ÷ Nutzfläche 120 m²",
                         GebaeudeZuordnungsModell.BelegText(f.Beleg));
            Assert.Equal("Vorgabe des Stundenmodells 2,5, gilt bei leerer Zeile", GebaeudeZuordnungsModell.BelegText(f.VorgabeBeleg));

            // Dieselbe Messung wie der Innenweg des Vorschlags.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            Nah(v.InnenflaecheDateiM2 / v.Zone.Nutzflaeche.Value, f.Wert);
        }

        /// <summary>Auf dem Weg „Innenflächenfaktor" des Vorschlags trägt das Zielfeld genau seinen Faktor.</summary>
        [Theory]
        [InlineData("ifc4_haus.ifc", null)]
        [InlineData("gbxml_innenflaechen_teilweise.xml", null)]
        [InlineData("gbxml_haus_si.xml", null)]
        [InlineData("gbxml_ohne_konstruktionen.xml", 'E')]
        [InlineData("ifc4_vorhangfassade.ifc", 'E')]
        public void Zielfeld_und_Innenweg_des_Vorschlags_stimmen_ueberein(string probe, char? klasse)
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(probe);
            GebaeudeFeldzeile f = a.Zuordnen(0, klasse).Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, klasse);
            switch (v.Innenweg)
            {
                case Innenweg.Innenflaechenfaktor:
                    Nah(v.Innenflaechenfaktor.Value, f.Wert);
                    break;
                case Innenweg.Bauteile:
                    Nah(v.FlaecheInnen / v.Zone.Nutzflaeche.Value, f.Wert);
                    break;
                default:
                    Assert.Null(f.Wert);
                    Assert.False(f.Uebernehmen);
                    break;
            }
        }

        [Fact]
        public void Ohne_Innenflaechen_bleibt_das_Zielfeld_leer_und_es_gilt_die_Vorgabe()
        {
            GebaeudeImportSatz s = GebaeudeAggregation.Bilden(BauteilvorschlagProbe.Synthetisch(), 0, null, null, new GbxmlImportProfil());
            GebaeudeFeldzeile f = s.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
            Assert.Null(f.Wert);
            Assert.Equal(Importherkunft.Leer, f.Herkunft);
            Assert.False(f.Uebernehmen);
            Assert.Equal(2.5, f.VorgabeWert);
            Assert.Equal("keine Innenflächen in der Datei — leer, es gilt die Vorgabe 2,5", GebaeudeZuordnungsModell.BelegText(f.Beleg));
        }

        [Fact]
        public void Eine_unplausible_Innenflaeche_macht_das_Zielfeld_gelb()
        {
            var ueb = new Dictionary<string, bool> { ["raum-bad"] = false };
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_innenflaechen_teilweise.xml");
            GebaeudeFeldzeile f = a.Zuordnen(0, null, ueb).Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
            Nah(0.4, f.Wert);
            Assert.Equal(PruefStufe.Warnung, f.Markierung);
            Assert.True(f.Uebernehmen);
            Assert.Equal("Innenfläche beider Seiten 18 m² (1 Trennflächen) ÷ Nutzfläche 45 m² — außerhalb 1 … 5, bitte prüfen",
                         GebaeudeZuordnungsModell.BelegText(f.Beleg));
        }

        [Fact]
        public async Task Der_Innenflaechenfaktor_geht_mit_Haken_in_den_Editor_ohne_Haken_bleibt_die_Vorgabe()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportErgebnis e) =
                await GebaeudeImportEditorabschlussTests.Zuordnen("gbxml_haus_si.xml", KLASSE_E);
            GebaeudeFeldzeileDaten zeile = e.Zeile(GebaeudeZielfelder.INNENFLAECHENFAKTOR);
            Assert.True(zeile.Haken);
            Assert.Equal("GBXML", zeile.HerkunftSchluessel);

            GebaeudeKatalogDaten d = h.Vorbelegung(e).Daten;
            Nah(145.0 / 120.0, d.Innenflaechenfaktor);
            // … und über den Speicherweg des Editors in den Katalogsatz (von dort in die Projektkopie).
            Nah(145.0 / 120.0, GebaeudeKatalogHuelle.NachModell(d, new GebaeudeModel()).Innenflaechenfaktor);

            // Eine Handänderung gilt; ohne Haken bleibt das Feld leer (Vorgabe 2,5).
            List<GebaeudeFeldzeileDaten> hand = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.INNENFLAECHENFAKTOR
                ? z with { Wert = 3.0, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell } : z).ToList();
            Assert.Equal(3.0, h.Vorbelegung(e with { Zeilen = hand }).Daten.Innenflaechenfaktor);
            List<GebaeudeFeldzeileDaten> ohne = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.INNENFLAECHENFAKTOR
                ? z with { Haken = false } : z).ToList();
            Assert.Null(h.Vorbelegung(e with { Zeilen = ohne }).Daten.Innenflaechenfaktor);
        }
    }
}
