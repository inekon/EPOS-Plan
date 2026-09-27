using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

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
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportBauteileTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

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

        // =================================================================================
        //  Die Hülle: der Abschnitt „Bauteile (echte Hülle)" und die ausstehende Herkunft
        // =================================================================================

        private static IReadOnlyDictionary<string, bool> Keine => new Dictionary<string, bool>();

        /// <summary>Liest eine Probe über den Parametersatz der Hülle und ordnet sie zu — wie der Zuordnungsdialog.</summary>
        internal static async Task<(GebaeudeImportHuelle Huelle, IReadOnlyDictionary<string, object> Gaben, GebaeudeImportStand Stand)> Zuordnen(
            string probe, int? klasse, IReadOnlyDictionary<string, bool> haken = null)
        {
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, System.Threading.CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, System.Threading.CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            return (h, gaben, zuordnen(new GebaeudeZuordnungsanfrage(0, klasse, haken ?? Keine, Zonenregel: Einzonenregel.Fuer(probe))));
        }

        [Fact]
        public async Task Die_Huelle_reicht_den_Vorschlag_mit_Zeilen_Innenweg_und_Meldungen()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportStand stand) = await Zuordnen("gbxml_haus_si.xml", null);
            GebaeudeBauteileDaten b = stand.Bauteile;
            Assert.NotNull(b);
            Assert.True(b.Moeglich);
            Assert.Equal("", b.Ablehnung);
            Assert.Equal(h.Vorschlag.Zeilen.Count, b.Zeilen.Count);
            Assert.Equal(34, b.Zeilen.Count);
            Assert.Equal("Zone „Haus 1“ · Nutzfläche 120 m² · 34 Bauteile · 6 Aufbauten", b.Kopftext);
            Assert.Equal("Innere Masse: Innenbauteile aus der Datei, 6 Zeilen mit 145 m² Innenfläche beider Seiten.", b.Innenweg);
            Assert.Equal(h.Vorschlag.Meldungen.Count, b.Meldungen.Count);

            // Eine Außenwand mit Aufbau: U „aus Schichten", Azimut, Neigung, Außenluft, Herkunft der Datei.
            GebaeudeBauteilzeile quelle = h.Vorschlag.Zeilen.First(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND && z.Bauteil.ID_Aufbau.HasValue);
            GebaeudeBauteilzeileDaten wand = b.Zeilen.Single(z => z.Kennung == quelle.Kennung);
            Assert.Equal("Außenwand", wand.Art);
            Assert.Equal("aus Schichten", wand.UWert);
            Assert.EndsWith(" m²", wand.Flaeche);
            Assert.EndsWith("°", wand.Azimut);
            Assert.Equal("90°", wand.Neigung);
            Assert.Equal("Außenluft", wand.Randbedingung);
            Assert.Equal("gbXML-Datei", wand.HerkunftText);
            Assert.Equal("GBXML", wand.HerkunftSchluessel);
            // Eine Innenseite: innerhalb der Zone, ohne Azimut.
            GebaeudeBauteilzeileDaten innen = b.Zeilen.First(z => z.Randbedingung == "innerhalb der Zone");
            Assert.Equal("—", innen.Azimut);
        }

        [Fact]
        public async Task Die_Raumauswahl_bildet_den_Vorschlag_neu()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportStand alle) = await Zuordnen("gbxml_haus_si.xml", null);
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];

            // Die Küche als unbeheizt: Die Innenwand zu ihr wird Hülle gegen einen unbeheizten Raum.
            GebaeudeImportStand ohneKueche = zuordnen(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool> { ["raum-kueche"] = false },
                                                                                    Zonenregel: Einzonenregel.Fuer("gbxml_haus_si.xml")));
            Assert.True(ohneKueche.Bauteile.Moeglich);
            Assert.NotEqual(alle.Bauteile.Kopftext, ohneKueche.Bauteile.Kopftext);
            Assert.Contains(ohneKueche.Bauteile.Zeilen, z => z.Kennung == "iw-eg" && z.Randbedingung == "unbeheizter Raum");
            Assert.DoesNotContain(alle.Bauteile.Zeilen, z => z.Kennung == "iw-eg" && z.Randbedingung == "unbeheizter Raum");
            Assert.Equal(new[] { "raum-wohnen", "raum-schlafen" }, h.Vorschlag.Raeume.Select(r => r.Quellkennung));
        }

        [Fact]
        public async Task Ein_abgelehnter_Vorschlag_nennt_seinen_Grund_und_die_Pruefung_lehnt_den_Schalter_benannt_ab()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportStand stand) =
                await Zuordnen("gbxml_ohne_konstruktionen.xml", null);
            GebaeudeBauteileDaten b = stand.Bauteile;
            Assert.False(b.Moeglich);
            Assert.Contains("keine Baualtersklasse liefert eine Vorgabe", b.Ablehnung);
            Assert.Contains(b.Meldungen, m => m.Stufe == EPOS.UI.Bausteine.WarnStufe.Fehler && m.Kennung == GebaeudeBauteilvorschlag.UWERT_FEHLT);

            // Das Ergebnis mit Schalter „ein" trotz Ablehnung: benannt abgelehnt, nicht still als Summenweg.
            var e = new GebaeudeImportErgebnis(0, null, "Probe", Keine, stand.Zeilen.ToList(), AlsZone: true);
            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            GebaeudeImportMeldung m = Assert.Single(pruefen(e), x => x.Kennung == GebaeudeImportHuelle.ALS_ZONE_NICHT);
            Assert.StartsWith("Nicht möglich: ", m.Text);
            Assert.False(h.AlsZone);
            Assert.Null(h.Herkunft?.Vorschlag);

            // Mit Klasse E lässt er sich bilden.
            (_, _, GebaeudeImportStand mitKlasse) = await Zuordnen("gbxml_ohne_konstruktionen.xml", KLASSE_E);
            Assert.True(mitKlasse.Bauteile.Moeglich);
        }

        [Fact]
        public async Task Die_ausstehende_Herkunft_traegt_den_Vorschlag_nur_mit_Schalter()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportStand stand) = await Zuordnen("gbxml_haus_si.xml", KLASSE_E);
            var ohne = new GebaeudeImportErgebnis(0, KLASSE_E, "Probe", Keine, stand.Zeilen.ToList(),
                                                  Zonenregel: Einzonenregel.Fuer("gbxml_haus_si.xml"));
            h.SatzAusErgebnis(ohne);
            Assert.False(h.AlsZone);
            Assert.Null(h.Herkunft.Vorschlag);
            Assert.Single(h.Herkunft.Paarungen);

            h.SatzAusErgebnis(ohne with { AlsZone = true });
            Assert.True(h.AlsZone);
            GebaeudeBauteilvorschlag v = h.Herkunft.Vorschlag;
            Assert.NotNull(v);
            Assert.False(v.Abgelehnt);
            Assert.Equal('E', v.Baualtersklasse);
            Assert.Single(h.Herkunft.Paarungen);          // die des Gebäudes; die des Vorschlags entstehen beim Schreiben
        }

        /// <summary>
        /// Was der Abschnitt bei den Proben zeigt — Schalter (übernehmbar oder mit Grund gesperrt), Zahl
        /// der Zeilen und die Zeile zur inneren Masse; zur Auskunft auch in der Testausgabe.
        /// </summary>
        [Theory]
        [InlineData("gbxml_haus_si.xml", null, true, 34, "Innere Masse: Innenbauteile aus der Datei, 6 Zeilen")]
        [InlineData("ifc4_haus.ifc", null, true, 17, "Innere Masse: Innenflächenfaktor 1,551 aus der Datei")]
        [InlineData("gbxml_ohne_konstruktionen.xml", null, false, 8, "Innere Masse: Innenflächenfaktor Vorgabe 2,5")]
        [InlineData("gbxml_ohne_konstruktionen.xml", KLASSE_E, true, 8, "Innere Masse: Innenflächenfaktor Vorgabe 2,5")]
        [InlineData("ifc4_vorhangfassade.ifc", KLASSE_E, true, 7, "Innere Masse: Innenflächenfaktor Vorgabe 2,5")]
        public async Task Der_Abschnitt_zeigt_bei_den_Proben_Schalter_Zeilen_und_Innenweg(string probe, int? klasse, bool moeglich,
                                                                                         int zeilen, string innenweg)
        {
            (_, _, GebaeudeImportStand stand) = await Zuordnen(probe, klasse);
            GebaeudeBauteileDaten b = stand.Bauteile;
            _aus.WriteLine(probe + " (Klasse " + (klasse.HasValue ? ((char)('A' + klasse.Value)).ToString() : "keine") + "): Schalter "
                           + (b.Moeglich ? "ein" : "aus, gesperrt — " + b.Ablehnung) + "; " + b.Zeilen.Count + " Zeilen; " + b.Kopftext
                           + "; " + b.Innenweg + " Meldungen: " + string.Join(" | ", b.Meldungen.Select(m => m.Kennung)));
            Assert.Equal(moeglich, b.Moeglich);
            if (zeilen >= 0) Assert.Equal(zeilen, b.Zeilen.Count);
            Assert.StartsWith(innenweg, b.Innenweg);
        }

        [Fact]
        public async Task Die_Texte_des_Vorschlags_stehen_auch_englisch()
        {
            using (new Kulturvorrichtung("en-US"))
            {
                (_, _, GebaeudeImportStand stand) = await Zuordnen("ifc4_haus.ifc", null);
                Assert.StartsWith("Zone \"", stand.Bauteile.Kopftext);
                Assert.Contains("usable area 130 m²", stand.Bauteile.Kopftext);
                Assert.Equal("Internal mass: internal area factor 1.551 from the file, the mass follows the construction class.",
                             stand.Bauteile.Innenweg);
            }
        }
    }
}
