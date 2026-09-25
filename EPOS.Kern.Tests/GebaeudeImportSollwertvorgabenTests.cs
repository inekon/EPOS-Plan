using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorgaben des Gebäudeimports für innere Gewinne, Sollwerte und Nachtzeit</b> (Entscheid E43,
    /// Konzept-Nachtrag N1.48): innere Gewinne 5 W/m² × Nutzfläche, Tag 20 °C (wenn die Datei keinen
    /// trägt), Nacht 18 °C (höchstens der Tagsollwert), Nachtzeit 22 bis 6 Uhr — jede als ausgewiesene,
    /// änderbare Vorgabe mit Beleg, in der Herleitungszeile des vorbelegten Editors genannt und auf den
    /// Editor abgebildet; dazu der ganze Weg einer Probe ohne Sollwerte bis zur Annahme durch das
    /// Stundenmodell.
    /// </summary>
    public sealed class GebaeudeImportSollwertvorgabenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int KLASSE_E = 4;

        private static GebaeudeImportSatz KleinerSatz(string raum = null, char? klasse = 'E')
        {
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0, "<Width>10</Width><Height>2.5</Height>"), raum: raum));
            Assert.Single(a.Gebaeude);
            return a.Zuordnen(0, klasse);
        }

        private static GebaeudeImportSatz Probenhaus(Func<string, string> aendern = null)
        {
            string xml = File.ReadAllText(GbxmlImportTests.Probe("gbxml_haus_si.xml"));
            GebaeudeImportAblauf a = Klein.Lesen(aendern == null ? xml : aendern(xml));
            return a.Zuordnen(0, 'E');
        }

        // =================================================================================
        //  Innere Gewinne
        // =================================================================================

        [Fact]
        public void Die_inneren_Gewinne_sind_5_W_je_m2_Nutzflaeche_und_der_Vorschlag_bleibt_im_Beleg()
        {
            Assert.Equal(5.0, GebaeudeStammCtrl.INNERE_GEWINNE_JE_M2_VORGABE);
            GebaeudeImportSatz s = Probenhaus();

            GebaeudeFeldzeile gewinne = s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE);
            Assert.Equal(120.0, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            Assert.Equal(600.0, gewinne.Wert);
            Assert.Equal(600.0, gewinne.VorgabeWert);
            Assert.Equal(Importherkunft.Vorgabe, gewinne.Herkunft);
            Assert.True(gewinne.Uebernehmen);
            Assert.True(gewinne.Eingebbar);
            Assert.Equal("nicht in der Datei — Vorgabe 5 W/m² × 120 m² Nutzfläche = 600 W",
                         GebaeudeZuordnungsModell.BelegText(gewinne.VorgabeBeleg));
            string beleg = GebaeudeZuordnungsModell.BelegText(gewinne.Beleg);
            Assert.StartsWith("Vorgabe 5 W/m² × 120 m² Nutzfläche = 600 W; Vorschlag 600 W: Auslegungsleistung", beleg);
        }

        [Fact]
        public void Ohne_Nutzflaeche_bleiben_die_inneren_Gewinne_bei_0_W_mit_Beleg()
        {
            GebaeudeImportSatz s = KleinerSatz("<Space id=\"raum-1\" conditionType=\"Heated\"><Name>Wohnraum</Name><Volume>125</Volume></Space>");

            Assert.Null(s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            GebaeudeFeldzeile gewinne = s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE);
            Assert.Equal(0.0, gewinne.Wert);
            Assert.Equal(Importherkunft.Vorgabe, gewinne.Herkunft);
            Assert.Equal("GIMP_BELEG_GEWINNE_VORGABE", gewinne.Beleg.Schluessel);
            Assert.Equal("nicht in der Datei — 0 W wie bei einem neuen Gebäude", GebaeudeZuordnungsModell.BelegText(gewinne.Beleg));
        }

        // =================================================================================
        //  Sollwerte und Nachtzeit
        // =================================================================================

        [Fact]
        public void Ohne_Sollwert_in_der_Datei_gelten_20_und_18_Grad_und_22_bis_6_Uhr()
        {
            GebaeudeImportSatz s = KleinerSatz();

            (string feld, double wert, string einheit, string beleg)[] erwartet =
            {
                (GebaeudeZielfelder.SOLL_TAG, 20.0, "°C", "nicht in der Datei — Vorgabe 20 °C"),
                (GebaeudeZielfelder.SOLL_NACHT, 18.0, "°C", "nicht in der Datei — Vorgabe 18 °C"),
                (GebaeudeZielfelder.NACHT_BEGINN, 22.0, "h", "nicht in der Datei — Vorgabe 22 Uhr (Nacht von 22 bis 6 Uhr)"),
                (GebaeudeZielfelder.NACHT_ENDE, 6.0, "h", "nicht in der Datei — Vorgabe 6 Uhr (Nacht von 22 bis 6 Uhr)"),
            };
            foreach ((string feld, double wert, string einheit, string beleg) in erwartet)
            {
                GebaeudeFeldzeile z = s.Zeile(feld);
                Assert.Equal(wert, z.Wert);
                Assert.Equal(einheit, z.Einheit);
                Assert.Equal(GebaeudeZielfelder.GRUPPE_SOLLWERTE, z.Gruppe);
                Assert.Equal(Importherkunft.Vorgabe, z.Herkunft);
                Assert.True(z.Uebernehmen, feld);
                Assert.True(z.Eingebbar, feld);
                Assert.Equal(beleg, GebaeudeZuordnungsModell.BelegText(z.Beleg));
            }
            Assert.Equal("Heizsollwert in der Nacht", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.SOLL_NACHT));
            Assert.Equal("Nachtabsenkung von", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.NACHT_BEGINN));
            Assert.Equal("Nachtabsenkung bis", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.NACHT_ENDE));
        }

        [Fact]
        public void Der_Tagsollwert_der_Datei_gilt_und_die_Nacht_liegt_hoechstens_auf_ihm()
        {
            GebaeudeImportSatz s = Probenhaus();
            Assert.Equal(20.0, s.Zeile(GebaeudeZielfelder.SOLL_TAG).Wert);
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.SOLL_TAG).Herkunft);
            Assert.Equal(20.0, s.Zeile(GebaeudeZielfelder.SOLL_TAG).VorgabeWert);    // die Vorgabe steht daneben
            Assert.Equal(18.0, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);

            GebaeudeImportSatz kalt = Probenhaus(x => x.Replace("<DesignHeatT>20</DesignHeatT>", "<DesignHeatT>17</DesignHeatT>"));
            Assert.Equal(17.0, kalt.Zeile(GebaeudeZielfelder.SOLL_TAG).Wert);
            GebaeudeFeldzeile nacht = kalt.Zeile(GebaeudeZielfelder.SOLL_NACHT);
            Assert.Equal(17.0, nacht.Wert);
            Assert.Equal(Importherkunft.Vorgabe, nacht.Herkunft);
            Assert.Equal("nicht in der Datei — Vorgabe 18 °C, höchstens der Tagsollwert: 17 °C",
                         GebaeudeZuordnungsModell.BelegText(nacht.Beleg));
        }

        [Fact]
        public void Eine_Handaenderung_macht_die_Herkunft_manuell_und_die_Pruefung_haelt_Stunden_im_Tag()
        {
            GebaeudeImportSatz s = KleinerSatz();
            Assert.True(s.ManuellSetzen(GebaeudeZielfelder.NACHT_BEGINN, 23));
            Assert.True(s.ManuellSetzen(GebaeudeZielfelder.SOLL_NACHT, 16));
            Assert.Equal(Importherkunft.Manuell, s.Zeile(GebaeudeZielfelder.NACHT_BEGINN).Herkunft);
            Assert.Null(s.Zeile(GebaeudeZielfelder.NACHT_BEGINN).Beleg);
            Assert.Equal(16.0, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);
            Assert.DoesNotContain(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel.EndsWith("NACHTZEIT_UNGUELTIG", StringComparison.Ordinal));

            foreach (double falsch in new[] { 24.0, -1.0, 22.5 })
            {
                s.ManuellSetzen(GebaeudeZielfelder.NACHT_ENDE, falsch);
                PruefMeldung m = Assert.Single(GebaeudeImportAblauf.Pruefen(s),
                                               x => x.Schluessel == GebaeudeImportAblauf.MELDUNG + "NACHTZEIT_UNGUELTIG");
                Assert.Equal(PruefStufe.Fehler, m.Stufe);
                Assert.StartsWith("Nachtabsenkung bis ", GebaeudeZuordnungsModell.MeldungText(m));
            }
        }

        // =================================================================================
        //  Folgevorgaben: Gewinne folgen der Nutzfläche, die Nacht folgt dem Tag
        // =================================================================================

        [Fact]
        public void Eine_Handaenderung_der_Nutzflaeche_zieht_die_inneren_Gewinne_nach()
        {
            GebaeudeImportSatz s = Probenhaus();
            s.ManuellSetzen(GebaeudeZielfelder.NUTZFLAECHE, 200);
            s.FolgevorgabenNachziehen();

            // 5 W/m² × 200 m², weiter als Vorgabe; der Vorschlag der Datei bleibt im Beleg, mit den neuen Zahlen.
            GebaeudeFeldzeile gewinne = s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE);
            Assert.Equal(1000.0, gewinne.Wert);
            Assert.Equal(1000.0, gewinne.VorgabeWert);
            Assert.Equal(Importherkunft.Vorgabe, gewinne.Herkunft);
            Assert.True(gewinne.Uebernehmen);
            Assert.Equal("nicht in der Datei — Vorgabe 5 W/m² × 200 m² Nutzfläche = 1000 W",
                         GebaeudeZuordnungsModell.BelegText(gewinne.VorgabeBeleg));
            Assert.Equal("GIMP_BELEG_GEWINNE_JE_FLAECHE_VORSCHLAG", gewinne.Beleg.Schluessel);
            Assert.StartsWith("Vorgabe 5 W/m² × 200 m² Nutzfläche = 1000 W; Vorschlag 600 W: Auslegungsleistung",
                              GebaeudeZuordnungsModell.BelegText(gewinne.Beleg));

            // Ohne Nutzfläche gilt die Vorgabe eines neuen Gebäudes (0 W); der Vorschlag bleibt im Beleg.
            s.ManuellSetzen(GebaeudeZielfelder.NUTZFLAECHE, null);
            s.FolgevorgabenNachziehen();
            Assert.Equal(0.0, gewinne.Wert);
            Assert.Equal("GIMP_BELEG_GEWINNE_VORSCHLAG", gewinne.Beleg.Schluessel);
            Assert.Equal("GIMP_BELEG_GEWINNE_VORGABE", gewinne.VorgabeBeleg.Schluessel);

            // Ein Satz ohne Vorschlag in der Datei: der Beleg ist die Vorgabe selbst.
            GebaeudeImportSatz klein = KleinerSatz();
            klein.ManuellSetzen(GebaeudeZielfelder.NUTZFLAECHE, 40);
            klein.FolgevorgabenNachziehen();
            Assert.Equal(200.0, klein.Zeile(GebaeudeZielfelder.INNERE_GEWINNE).Wert);
            Assert.Equal("GIMP_BELEG_GEWINNE_JE_FLAECHE", klein.Zeile(GebaeudeZielfelder.INNERE_GEWINNE).Beleg.Schluessel);
        }

        [Fact]
        public void Von_Hand_gesetzte_Gewinne_bleiben_bei_einer_neuen_Nutzflaeche()
        {
            GebaeudeImportSatz s = Probenhaus();
            s.ManuellSetzen(GebaeudeZielfelder.INNERE_GEWINNE, 777);
            s.ManuellSetzen(GebaeudeZielfelder.NUTZFLAECHE, 200);
            s.FolgevorgabenNachziehen();
            Assert.Equal(777.0, s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE).Wert);
            Assert.Equal(Importherkunft.Manuell, s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE).Herkunft);
        }

        [Fact]
        public void Die_Nacht_folgt_einem_Handtag_unter_18_Grad()
        {
            GebaeudeImportSatz s = KleinerSatz();
            s.ManuellSetzen(GebaeudeZielfelder.SOLL_TAG, 17);
            s.FolgevorgabenNachziehen();
            GebaeudeFeldzeile nacht = s.Zeile(GebaeudeZielfelder.SOLL_NACHT);
            Assert.Equal(17.0, nacht.Wert);
            Assert.Equal(Importherkunft.Vorgabe, nacht.Herkunft);
            Assert.Equal("nicht in der Datei — Vorgabe 18 °C, höchstens der Tagsollwert: 17 °C", GebaeudeZuordnungsModell.BelegText(nacht.Beleg));

            // Ein Tag über 18 °C: wieder die Vorgabe 18 °C.
            s.ManuellSetzen(GebaeudeZielfelder.SOLL_TAG, 21);
            s.FolgevorgabenNachziehen();
            Assert.Equal(18.0, nacht.Wert);
            Assert.Equal("nicht in der Datei — Vorgabe 18 °C", GebaeudeZuordnungsModell.BelegText(nacht.Beleg));
        }

        [Fact]
        public void Eine_Handnacht_bleibt_bei_einem_neuen_Tag()
        {
            GebaeudeImportSatz s = KleinerSatz();
            s.ManuellSetzen(GebaeudeZielfelder.SOLL_NACHT, 16);
            s.ManuellSetzen(GebaeudeZielfelder.SOLL_TAG, 15);
            s.FolgevorgabenNachziehen();
            Assert.Equal(16.0, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Wert);
            Assert.Equal(Importherkunft.Manuell, s.Zeile(GebaeudeZielfelder.SOLL_NACHT).Herkunft);
        }

        /// <summary>Der Nachzug ohne Handänderung ändert nichts — Zuordnung und Nachzug benutzen dieselbe Regel.</summary>
        [Fact]
        public void Ein_Satz_ohne_Handaenderung_ist_nach_dem_Nachzug_unveraendert()
        {
            foreach (GebaeudeImportSatz s in new[]
                     {
                         Probenhaus(), KleinerSatz(),
                         KleinerSatz("<Space id=\"raum-1\" conditionType=\"Heated\"><Name>Wohnraum</Name><Volume>125</Volume></Space>"),
                         Probenhaus(x => x.Replace("<DesignHeatT>20</DesignHeatT>", "<DesignHeatT>17</DesignHeatT>")),
                     })
            {
                string vorher = Abdruck(s);
                s.FolgevorgabenNachziehen();
                Assert.Equal(vorher, Abdruck(s));
            }
        }

        private static string Abdruck(GebaeudeImportSatz s)
            => string.Join("\n", s.Zeilen.Select(z => string.Join("|", z.Zielfeld, z.Wert?.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                                                                   z.Textwert, z.Herkunft, z.Beleg?.ToString(), z.VorgabeWert?.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                                                                   z.VorgabeBeleg?.ToString(), z.Uebernehmen, z.Markierung)));

        [Fact]
        public async Task Die_Vorbelegung_mit_einer_Handflaeche_ergibt_5_W_je_m2_mal_Flaeche()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportErgebnis e) =
                await GebaeudeImportEditorabschlussTests.Zuordnen("gbxml_ohne_konstruktionen.xml", KLASSE_E);

            // Die Handfläche im Ergebnis — die Zeile der Gewinne steht noch auf der alten Vorgabe.
            List<GebaeudeFeldzeileDaten> zeilen = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.NUTZFLAECHE
                ? z with { Wert = 150, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell } : z).ToList();
            var hand = new GebaeudeImportErgebnis(e.Gebaeudeindex, e.Baualtersklasse, e.Gebaeudename, e.BeheiztUebersteuert, zeilen);
            GebaeudeVorbelegung v = h.Vorbelegung(hand);
            Assert.Equal(150.0, v.Daten.WohnflaecheGesamt);
            Assert.Equal(750.0, v.Daten.Waermegewinne);
            Assert.Contains("Interne Wärmegewinne 750 W", v.Herleitung);

            // Die Zuordnung mit Handwerten zeigt dasselbe im Dialog.
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand stand = zuordnen(new GebaeudeZuordnungsanfrage(0, KLASSE_E, new Dictionary<string, bool>(),
                new Dictionary<string, double?> { [GebaeudeZielfelder.NUTZFLAECHE] = 150, [GebaeudeZielfelder.SOLL_TAG] = 17 }));
            GebaeudeFeldzeileDaten gewinne = stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.INNERE_GEWINNE);
            Assert.Equal(750.0, gewinne.Wert);
            Assert.Equal(ImportherkunftWerte.VORGABE, gewinne.HerkunftSchluessel);
            Assert.Equal(GebaeudeHerkunftSchluessel.Manuell, stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.NUTZFLAECHE).HerkunftSchluessel);
            Assert.Equal(17.0, stand.Zeilen.Single(z => z.Zielfeld == GebaeudeZielfelder.SOLL_NACHT).Wert);
        }

        // =================================================================================
        //  Abbildung auf den Editor und Herleitungszeile
        // =================================================================================

        [Fact]
        public async Task Die_Vorgaben_gehen_in_den_Editor_und_die_Herleitungszeile()
        {
            (GebaeudeImportHuelle h, _, GebaeudeImportErgebnis e) =
                await GebaeudeImportEditorabschlussTests.Zuordnen("gbxml_ohne_konstruktionen.xml", KLASSE_E);

            GebaeudeVorbelegung v = h.Vorbelegung(e);
            Assert.Equal(20.0, v.Daten.SollTag);
            Assert.Equal(18.0, v.Daten.NachtAbsenkung);
            Assert.Equal(22, v.Daten.NachtBeginn);
            Assert.Equal(6, v.Daten.NachtEnde);
            Assert.Equal(GebaeudeStammCtrl.INNERE_GEWINNE_JE_M2_VORGABE * v.Daten.WohnflaecheGesamt.Value, v.Daten.Waermegewinne.Value, 9);
            Assert.Contains("Heizsollwert am Tag 20 °C; Heizsollwert in der Nacht 18 °C; Nachtabsenkung von 22 h; " +
                            "Nachtabsenkung bis 6 h", v.Herleitung);

            // Eine Handänderung im Dialog: Herkunft manuell, und der Editor bekommt den Wert.
            List<GebaeudeFeldzeileDaten> zeilen = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.NACHT_BEGINN
                ? z with { Wert = 21, HerkunftSchluessel = GebaeudeHerkunftSchluessel.Manuell } : z).ToList();
            var geaendert = new GebaeudeImportErgebnis(e.Gebaeudeindex, e.Baualtersklasse, e.Gebaeudename, e.BeheiztUebersteuert, zeilen);
            GebaeudeImportSatz satz = h.SatzAusErgebnis(geaendert);
            Assert.Equal(Importherkunft.Manuell, satz.Zeile(GebaeudeZielfelder.NACHT_BEGINN).Herkunft);
            Assert.Equal(21, h.Vorbelegung(geaendert).Daten.NachtBeginn);

            // Ohne Haken bleibt die Grundlage stehen (leer = Vorgabe).
            List<GebaeudeFeldzeileDaten> ohne = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.NACHT_BEGINN
                                                                  || z.Zielfeld == GebaeudeZielfelder.NACHT_ENDE
                ? z with { Haken = false } : z).ToList();
            GebaeudeKatalogDaten d = GebaeudeImportHuelle.NachKatalogdaten(new GebaeudeKatalogDaten(),
                new GebaeudeImportErgebnis(e.Gebaeudeindex, e.Baualtersklasse, e.Gebaeudename, e.BeheiztUebersteuert, ohne));
            Assert.Null(d.NachtBeginn);
            Assert.Null(d.NachtEnde);
        }

        /// <summary>Nur eine Grenze mit Haken hält schon der Zuordnungsdialog an — über die Prüfung des vorbelegten Editors.</summary>
        [Fact]
        public async Task Eine_halbe_Nachtzeit_haelt_schon_der_Zuordnungsdialog_an()
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportErgebnis e) =
                await GebaeudeImportEditorabschlussTests.Zuordnen("gbxml_ohne_konstruktionen.xml", KLASSE_E);
            List<GebaeudeFeldzeileDaten> zeilen = e.Zeilen.Select(z => z.Zielfeld == GebaeudeZielfelder.NACHT_ENDE
                ? z with { Haken = false } : z).ToList();
            var halb = new GebaeudeImportErgebnis(e.Gebaeudeindex, e.Baualtersklasse, e.Gebaeudename, e.BeheiztUebersteuert, zeilen);

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            GebaeudeImportMeldung m = Assert.Single(pruefen(halb), x => x.Stufe == WarnStufe.Fehler);
            Assert.Equal(GebaeudeImportHuelle.EDITOR_BEFUND, m.Kennung);
            Assert.Contains("beide eingeben oder beide leer lassen", m.Text);
        }

        // =================================================================================
        //  Der ganze Weg: eine Probe ohne Sollwerte bis zur Annahme durch das Stundenmodell
        // =================================================================================

        [Theory]
        [InlineData("gbxml_ohne_konstruktionen.xml", KLASSE_E)]
        [InlineData("gbxml_norddrehung.xml", KLASSE_E)]
        [InlineData("gbxml_rwert_schicht.xml", KLASSE_E)]
        public async Task Eine_Probe_ohne_Sollwerte_rechnet_nach_dem_Editor_im_Stundenmodell(string probe, int klasse)
        {
            (GebaeudeImportHuelle h, IReadOnlyDictionary<string, object> gaben, GebaeudeImportErgebnis e) =
                await GebaeudeImportEditorabschlussTests.Zuordnen(probe, klasse);

            // Die Probe trägt keinen Sollwert: alle vier Zeilen der Gruppe sind Vorgaben.
            foreach (string feld in new[] { GebaeudeZielfelder.SOLL_TAG, GebaeudeZielfelder.SOLL_NACHT,
                                            GebaeudeZielfelder.NACHT_BEGINN, GebaeudeZielfelder.NACHT_ENDE })
                Assert.Equal(ImportherkunftWerte.VORGABE, e.Zeile(feld).HerkunftSchluessel);

            var pruefen = (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)gaben["Pruefen"];
            Assert.DoesNotContain(pruefen(e), m => m.Stufe == WarnStufe.Fehler);

            // Der Editor im Modus Neu nimmt die Vorbelegung an und leitet vor dem Speichern ab
            // (Maximalraumtemperatur < 1 → 24 °C) …
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(h.Vorbelegung(e).Daten, neu: true);
            GebaeudePruefbefund befund = arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
            Assert.True(befund == null, probe + ": " + befund?.Meldung);
            arbeit.Ableiten();
            GebaeudeModel satz = GebaeudeKatalogHuelle.NachModell(arbeit.Stand, new GebaeudeModel());
            Assert.Equal(24.0, satz.Maximaleraumtemperatur);
            Assert.Equal(20.0, satz.Raumsolltemperatur_Tag);
            Assert.Equal(18.0, satz.Raumsolltemperatur_Nachtabsenkung);
            Assert.Equal(22, satz.Nachtabsenkung_Beginn);
            Assert.Equal(6, satz.Nachtabsenkung_Ende);

            // … und das Stundenmodell nimmt den Satz an: θ_max 24 °C über dem Tagsollwert, die Nachtzeit
            // ausdrücklich 22 bis 6 Uhr — derselbe Fahrplan wie die Vorgabe.
            ProjektGebaeudeModel g = UebergabeHerleitungsquelle.AusKatalogsatz(satz);
            g.ID_Gebaeude = 1;
            GebaeudeModellEingang eingang = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            Assert.False(eingang.Nachtzeit.IstVorgabe);
            Assert.Equal(22, eingang.Nachtzeit.Beginn);
            Assert.Equal(6, eingang.Nachtzeit.Ende);
            for (int stunde = 0; stunde < 8760; stunde++)
                Assert.Equal(Nachtzeit.Vorgabe.Nutzungszeit(stunde), eingang.Nutzungszeit(stunde));
            Assert.True(eingang.ThetaMaxWert > eingang.SollTag);
            Assert.Equal(GebaeudeStammCtrl.INNERE_GEWINNE_JE_M2_VORGABE * satz.Wohnflaeche_gesamt, eingang.InnereGewinne_W, 9);
        }
    }
}
