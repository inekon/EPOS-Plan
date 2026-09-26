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
    /// <b>Die Kern-Ergänzungen der Stufe G4c, Welle 2</b> — was der Zuordnungsdialog vom Kern
    /// braucht: die Raumliste mit dem Haken „beheizt" samt Übersteuerung (Umsetzungskonzept 3.5
    /// Nr. 3), die Bauart aus den raumseitigen Schichten (Umsetzungskonzept 3.4, Zeile Bauweise),
    /// der Hinweis am g-Wert, die Handänderung (Herkunft „manuell") und die Prüfung am OK samt
    /// Namen, dazu die Texte der Oberfläche.
    ///
    /// <para><b>Von Hand nachgerechnet</b> sind die Speichermassen: je Aufbau die raumseitigen
    /// Schichten bis 10 cm, C″ = Σ ρ·c·d, flächengewichtet über die opaken Hüllbauteile.</para>
    /// </summary>
    public sealed class GebaeudeImportWelle2Tests : IDisposable
    {
        private const string G = "IMP_GEB_PROT_";
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Prüfstand
        // ==================================================================

        private static string Z(double w) => w.ToString("R", CultureInfo.InvariantCulture);

        private static string Masse(double breite, double hoehe)
            => "<Width>" + Z(breite) + "</Width><Height>" + Z(hoehe) + "</Height>";

        private static string Dach(string id, double breite, double hoehe, string kon = null)
            => "<Surface id=\"" + id + "\" surfaceType=\"Roof\"" + (kon == null ? "" : " constructionIdRef=\"" + kon + "\"") + ">"
               + "<AdjacentSpaceId spaceIdRef=\"raum-1\"/><RectangularGeometry><Azimuth>0</Azimuth><Tilt>0</Tilt>"
               + Masse(breite, hoehe) + "</RectangularGeometry></Surface>";

        /// <summary>Eine Konstruktion mit Schichten in DATEIreihenfolge — die erste Schicht ist außen (gbXML-Hausannahme).</summary>
        private static string Geschichtet(string id, params (string Id, double Dicke, double Lambda, double Rho, double Cp)[] schichten)
            => "<Construction id=\"" + id + "\"><LayerId layerIdRef=\"" + id + "-l\"/></Construction>"
               + "<Layer id=\"" + id + "-l\">" + string.Concat(schichten.Select(s => "<MaterialId materialIdRef=\"" + s.Id + "\"/>")) + "</Layer>"
               + string.Concat(schichten.Select(s => "<Material id=\"" + s.Id + "\"><Thickness>" + Z(s.Dicke) + "</Thickness>"
                   + "<Conductivity unit=\"WPerMeterK\">" + Z(s.Lambda) + "</Conductivity><Density unit=\"KgPerCubicM\">" + Z(s.Rho)
                   + "</Density><SpecificHeat unit=\"JPerKgK\">" + Z(s.Cp) + "</SpecificHeat></Material>"));

        private static GebaeudeImportSatz Satz(string flaechen, string katalog)
        {
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(flaechen, katalog: katalog));
            Assert.True(a.Gebaeude.Count == 1, string.Join(" | ", a.Meldungen));
            return a.Zuordnen(0, 'E');
        }

        private static void Nah(double erwartet, double? ist, double toleranz = 1e-9)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= toleranz, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        // ==================================================================
        //  Die Raumliste und ihr Haken
        // ==================================================================

        [Fact]
        public void Die_Raumliste_nennt_Flaeche_beheizt_und_Grund()
        {
            GebaeudeImportAblauf a = GbxmlImportTests.LesenDatei("gbxml_haus_si.xml");
            IReadOnlyList<GebaeudeRaumzeile> r = a.Raeume(0);

            Assert.Equal(new[] { "raum-wohnen", "raum-kueche", "raum-schlafen", "raum-keller" }, r.Select(x => x.Kennung));
            Assert.Equal(new[] { true, true, true, false }, r.Select(x => x.Beheizt));
            Assert.All(r, x => Assert.Equal(BeheiztQuelle.Attribut, x.Quelle));
            Assert.All(r, x => Assert.False(x.Uebersteuert));
            Nah(40.0, r[0].FlaecheM2);
            Assert.Equal("Wohnen", r[0].Anzeigename);
            Assert.Equal("Zustandsangabe der Datei: Unconditioned", GebaeudeZuordnungsModell.RaumGrundText(r[3]));
        }

        [Fact]
        public void Die_Namensregel_und_die_Annahme_stehen_als_Grund_da()
        {
            string raeume = "<Space id=\"r-1\"><Name>Wohnen</Name><Area>50</Area><Volume>125</Volume></Space>"
                          + "<Space id=\"r-2\"><Name>Kellerraum</Name><Area>30</Area><Volume>60</Volume></Space>";
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0, Masse(10, 2.5), nachbarn: new[] { "r-1" }), raum: raeume));
            IReadOnlyList<GebaeudeRaumzeile> r = a.Raeume(0);

            Assert.Equal(BeheiztQuelle.Annahme, r[0].Quelle);
            Assert.Equal("keine Angabe — als beheizt angenommen", GebaeudeZuordnungsModell.RaumGrundText(r[0]));
            Assert.Equal(BeheiztQuelle.Name, r[1].Quelle);
            Assert.Equal("Keller", r[1].Namenstreffer);
            Assert.Equal("Name enthält „Keller“", GebaeudeZuordnungsModell.RaumGrundText(r[1]));
        }

        [Fact]
        public void Ein_beheizter_Keller_zieht_Kellerdecke_nach_innen_und_Kellerwaende_zur_Grundflaeche()
        {
            GebaeudeImportAblauf a = GbxmlImportTests.LesenDatei("gbxml_haus_si.xml");
            var haken = new Dictionary<string, bool>(StringComparer.Ordinal) { ["raum-keller"] = true };

            GebaeudeImportSatz s = a.Zuordnen(0, 'E', haken);

            Nah(180.0, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);   // 120 + 60 m²
            // Die Kellerdecke ist jetzt innere Masse; Bodenplatte 60 m² und Kellerwände 85 m² gegen Erdreich.
            Nah(145.0, s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
            Assert.Equal(DbWerte.GRUND_ERDREICH, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "RAUM_ALS_BEHEIZT");
            Assert.Equal(new[] { "raum-keller", "Keller" }, m.Werte);
            Assert.Equal(new KeyValuePair<string, bool>("raum-keller", true), Assert.Single(s.Uebersteuerungen));
            Assert.Contains(s.Quellzuordnungen, q => q.Quelltyp == "Space" && q.Quellkennung == "raum-keller");

            GebaeudeRaumzeile keller = a.Raeume(0, haken).Single(x => x.Kennung == "raum-keller");
            Assert.True(keller.Beheizt);
            Assert.False(keller.BeheiztLautDatei);
            Assert.True(keller.Uebersteuert);
            Assert.Equal("vom Anwender umgestellt", GebaeudeZuordnungsModell.RaumGrundText(keller));

            // Das Abbild bleibt, wie der Leser es gebaut hat — ohne Haken gilt wieder die Datei.
            Assert.False(a.Abbild.Gebaeude[0].Raeume.Single(x => x.Kennung == "raum-keller").Beheizt);
            Nah(120.0, a.Zuordnen(0, 'E').Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
        }

        [Fact]
        public void Ein_unbeheizt_gesetzter_Raum_faellt_aus_der_Nutzflaeche()
        {
            GebaeudeImportAblauf a = GbxmlImportTests.LesenDatei("gbxml_haus_si.xml");
            var haken = new Dictionary<string, bool>(StringComparer.Ordinal) { ["raum-kueche"] = false, ["raum-wohnen"] = true };

            GebaeudeImportSatz s = a.Zuordnen(0, 'E', haken);

            Nah(100.0, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "RAUM_ALS_UNBEHEIZT");
            Assert.Equal(new[] { "raum-kueche", "Küche" }, m.Werte);
            // Ein Haken, der der Datei gleicht, ist keine Übersteuerung.
            Assert.DoesNotContain(s.Meldungen, x => x.Schluessel == G + "RAUM_ALS_BEHEIZT");
            Assert.Equal(new[] { "raum-kueche" }, s.Uebersteuerungen.Keys);
        }

        // ==================================================================
        //  Bauart aus den raumseitigen Schichten
        // ==================================================================

        [Fact]
        public void Die_wirksame_Kapazitaet_zaehlt_von_innen_bis_zehn_Zentimeter()
        {
            // Dateireihenfolge außen → innen: A 0,20 m (ρ 2000, c 1000), B 0,05 m (ρ 1000, c 1000).
            var aufbau = new AbbildAufbau { Kennung = "k", Status = Aufbaustatus.Vollstaendig };
            aufbau.Schichten.Add(new AbbildSchicht { DickeM = 0.20, LambdaWmK = 1.0, RhoKgM3 = 2000, CpJkgK = 1000 });
            aufbau.Schichten.Add(new AbbildSchicht { DickeM = 0.05, LambdaWmK = 0.5, RhoKgM3 = 1000, CpJkgK = 1000 });

            // innen B ganz (0,05·1000·1000 = 50 000), dann A zu 0,05 m (0,05·2000·1000 = 100 000)
            Nah(150000.0, SchichtwerteNaht.WirksameKapazitaetAusSchichten(aufbau), 1e-6);
            Nah(30000.0, SchichtwerteNaht.WirksameKapazitaetAusSchichten(aufbau, 0.03), 1e-6);
            // Der ganze Aufbau (ohne Tiefengrenze) bleibt die Sache von KapazitaetAusSchichten.
            Nah(450000.0, SchichtwerteNaht.KapazitaetAusSchichten(aufbau), 1e-6);

            aufbau.Status = Aufbaustatus.Masselos;
            Assert.Null(SchichtwerteNaht.WirksameKapazitaetAusSchichten(aufbau));

            var ausserhalb = new AbbildAufbau { Kennung = "x", Status = Aufbaustatus.Vollstaendig };
            ausserhalb.Schichten.Add(new AbbildSchicht { DickeM = 0.01, LambdaWmK = 1000, RhoKgM3 = 2000, CpJkgK = 1000 });
            Assert.Null(SchichtwerteNaht.WirksameKapazitaetAusSchichten(ausserhalb));
        }

        [Fact]
        public void Die_Bauart_kommt_aus_den_Schichten_von_Hand_nachgerechnet()
        {
            // Wand 25 m², außen → innen: Dämmung 0,12 / KS 0,175 (ρ 1800, c 1000) / Putz 0,015 (ρ 1200, c 1000)
            //   innen: Putz 0,015·1200·1000 = 18 000 + KS 0,085·1800·1000 = 153 000 → 171 000 J/(m²K)
            // Dach 50 m², außen → innen: Dämmung 0,20 (ρ 20, c 1400) / Gipskarton 0,0125 (ρ 900, c 1000)
            //   innen: Gips 0,0125·900·1000 = 11 250 + Dämmung 0,0875·20·1400 = 2 450 → 13 700 J/(m²K)
            // flächengewichtet: (25·171 000 + 50·13 700) / 75 = 66 133,3 J/(m²K) = 18,37 Wh/(m²K) < 30 → leicht
            string flaechen = Klein.Wand("aw-1", 0, Masse(10, 2.5), kon: "k-w") + Dach("dach-1", 10, 5, "k-d");
            string katalog = Geschichtet("k-w", ("m-dae", 0.12, 0.04, 30, 1500), ("m-ks", 0.175, 0.99, 1800, 1000), ("m-putz", 0.015, 0.7, 1200, 1000))
                           + Geschichtet("k-d", ("m-dd", 0.20, 0.035, 20, 1400), ("m-gk", 0.0125, 0.25, 900, 1000));
            GebaeudeImportSatz s = Satz(flaechen, katalog);

            double jeM2 = (25.0 * 171000.0 + 50.0 * 13700.0) / 75.0 / 3600.0;
            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_LEICHT, bauart.Textwert);
            Assert.Equal(Importherkunft.GbXml, bauart.Herkunft);
            Assert.Equal("GIMP_BELEG_BAUART_SCHICHTEN", bauart.Beleg.Schluessel);
            Assert.Equal(new[] { Z(Math.Round(jeM2, 2)), "2", "75" }, bauart.Beleg.Werte);
            Assert.Equal("18.37", bauart.Beleg.Werte[0]);
            Assert.Null(bauart.Markierung);
            Assert.True(bauart.Uebernehmen);

            // Die Bauweise bleibt leer — der Editor bildet sie aus der Bauart; die Zahl steht im Beleg.
            GebaeudeFeldzeile bauweise = s.Zeile(GebaeudeZielfelder.BAUWEISE);
            Assert.Null(bauweise.Wert);
            Assert.False(bauweise.Uebernehmen);
            Assert.Equal("GIMP_BELEG_BAUWEISE_SCHICHTEN", bauweise.Beleg.Schluessel);
            Assert.Equal(Z(Math.Round(jeM2 * 50.0)), bauweise.Beleg.Werte[0]);   // Nutzfläche 50 m²
            // Belegwerte stehen invariant (Hausregel PruefMeldung); der Text zeigt sie in der
            // Anzeigekultur (G4 Welle 4) — de-DE mit Komma.
            Assert.Equal("aus den Schichten 919 Wh/K (18,37 Wh/(m²K)) — der Gebäudeeditor bildet die Bauweise aus der Bauart",
                         GebaeudeZuordnungsModell.BelegText(bauweise.Beleg));
            Assert.DoesNotContain(s.Meldungen, m => m.Schluessel == G + "BAUWEISE_AUSSERHALB");
        }

        [Fact]
        public void Das_Probenhaus_rastet_auf_schwer_ein()
        {
            // Außenwand 145 m² (Putz 0,01 → 14 000, Innendämmung 0,08 → 8 000, Mauerwerk 0,01 → 16 000 = 38 000),
            // Dach 60 m² (Putz 14 000 + Beton 0,09 → 216 000 = 230 000), Kellerdecke 60 m² (Estrich 0,06 → 120 000
            // + Beton 0,04 → 96 000 = 216 000): (145·38 000 + 60·230 000 + 60·216 000) / 265 / 3 600 = 33,83 Wh/(m²K).
            GebaeudeImportSatz s = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, bauart.Textwert);
            Assert.Equal(new[] { "33.83", "13", "265" }, bauart.Beleg.Werte);
            Assert.Equal(new[] { "4059", "33.83" }, s.Zeile(GebaeudeZielfelder.BAUWEISE).Beleg.Werte);
        }

        [Fact]
        public void Ein_Huellbauteil_ohne_Aufbau_laesst_die_Bauart_bei_der_Vorgabe()
        {
            string flaechen = Klein.Wand("aw-1", 0, Masse(10, 2.5), kon: "k-w") + Dach("dach-1", 10, 5);
            string katalog = Geschichtet("k-w", ("m-ks", 0.175, 0.99, 1800, 1000), ("m-putz", 0.015, 0.7, 1200, 1000));
            GebaeudeImportSatz s = Satz(flaechen, katalog);

            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, bauart.Textwert);
            Assert.Equal(Importherkunft.Vorgabe, bauart.Herkunft);
            Assert.Equal("GIMP_BELEG_BAUART_VORGABE", bauart.Beleg.Schluessel);
            Assert.Equal(new[] { "1", "2" }, bauart.Beleg.Werte);
            Assert.Equal("GIMP_BELEG_BAUWEISE_AUS_BAUART", s.Zeile(GebaeudeZielfelder.BAUWEISE).Beleg.Schluessel);
        }

        [Fact]
        public void Eine_Speichermasse_ausserhalb_des_Bandes_warnt()
        {
            // Nur Dämmung: 0,10·30·1500 = 4 500 J/(m²K) = 1,25 Wh/(m²K) < 5 → leicht, gelb, Warnung.
            string flaechen = Klein.Wand("aw-1", 0, Masse(10, 2.5), kon: "k-d") + Dach("dach-1", 10, 5, "k-d");
            string katalog = Geschichtet("k-d", ("m-dae", 0.20, 0.04, 30, 1500));
            GebaeudeImportSatz s = Satz(flaechen, katalog);

            GebaeudeFeldzeile bauart = s.Zeile(GebaeudeZielfelder.BAUART);
            Assert.Equal(GebaeudeZielfelder.BAUART_LEICHT, bauart.Textwert);
            Assert.Equal(PruefStufe.Warnung, bauart.Markierung);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "BAUWEISE_AUSSERHALB");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal(new[] { "1.25", "5", "200" }, m.Werte);
        }

        // ==================================================================
        //  g-Wert-Hinweis
        // ==================================================================

        [Fact]
        public void Der_g_Wert_aus_gbXML_nennt_Glasanteil_und_Rahmenminderung()
        {
            GebaeudeImportSatz s = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeFeldzeile g = s.Zeile(GebaeudeZielfelder.G_WERT);
            Assert.Equal(Importherkunft.GbXml, g.Herkunft);
            Assert.Equal("GIMP_BELEG_G_GBXML", g.Beleg.Schluessel);
            string text = GebaeudeZuordnungsModell.BelegText(g.Beleg);
            Assert.Contains("ganze Fenster", text);
            Assert.Contains("Rahmenanteil", text);
            // Der U-Wert der Fenster behält den gewöhnlichen Beleg.
            Assert.Equal("GIMP_BELEG_U_GEWICHTET", s.Zeile(GebaeudeZielfelder.U_FENSTER).Beleg.Schluessel);
        }

        // ==================================================================
        //  Handänderung, Haken und die Prüfung am OK
        // ==================================================================

        [Fact]
        public void Eingebbar_und_Haken_folgen_dem_Zielfeld()
        {
            foreach (string f in new[] { GebaeudeZielfelder.BAUART, GebaeudeZielfelder.BAUALTERSKLASSE, GebaeudeZielfelder.GRUND_RANDBEDINGUNG,
                                         GebaeudeZielfelder.VOLUMEN, GebaeudeZielfelder.BAUWEISE, GebaeudeZielfelder.FENSTER_GESAMT })
                Assert.False(GebaeudeZielfelder.Finde(f).Eingebbar, f);
            foreach (string f in new[] { GebaeudeZielfelder.NUTZFLAECHE, GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.LAENGE_WAND_DACH,
                                         GebaeudeZielfelder.LUFTWECHSEL_NUTZER, GebaeudeZielfelder.INNERE_GEWINNE })
                Assert.True(GebaeudeZielfelder.Finde(f).Eingebbar, f);
            Assert.False(GebaeudeZielfelder.Finde(GebaeudeZielfelder.FENSTER_GESAMT).HakenSetzbar);
            Assert.True(GebaeudeZielfelder.Finde(GebaeudeZielfelder.BAUART).HakenSetzbar);

            GebaeudeImportSatz s = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            GebaeudeFeldzeile gesamt = s.Zeile(GebaeudeZielfelder.FENSTER_GESAMT);
            Assert.True(gesamt.HatWert);
            Assert.False(gesamt.Uebernehmen);        // abgeleitet — nie übernommen
            Assert.True(s.HakenSetzen(GebaeudeZielfelder.FENSTER_GESAMT, true));
            Assert.False(gesamt.Uebernehmen);

            Assert.Equal(GebaeudeZielfelder.BAUART_LEICHT, GebaeudeZielfelder.BauartSchluessel(Gebaeudebauweise.LEICHT));
            Assert.Equal(GebaeudeZielfelder.BAUART_SEHR_SCHWER, GebaeudeZielfelder.BauartSchluessel(Gebaeudebauweise.SEHR_SCHWER));
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, GebaeudeZielfelder.BauartSchluessel(-1));
        }

        [Fact]
        public void Die_Handaenderung_macht_die_Herkunft_manuell_und_loescht_die_Markierung()
        {
            // Fenster größer als die Wand: Außenwand 0 m², Zeile rot — die Prüfung sperrt.
            GebaeudeImportSatz s = GbxmlImportTests.Satz("gbxml_nettoflaeche_negativ.xml");
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Assert.Equal(PruefStufe.Fehler, wand.Markierung);
            Assert.Contains(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel == G + "ZEILE_FEHLER");

            Assert.True(s.ManuellSetzen(GebaeudeZielfelder.FLAECHE_AUSSENWAND, 12.5));
            Assert.Equal(12.5, wand.Wert);
            Assert.Equal(Importherkunft.Manuell, wand.Herkunft);
            Assert.Null(wand.Markierung);
            Assert.Null(wand.Beleg);
            Assert.True(wand.Uebernehmen);
            Assert.DoesNotContain(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel == G + "ZEILE_FEHLER");

            // Ein leerer Eintrag lässt den Haken stehen; die Prüfung sagt, ob das geht.
            GebaeudeFeldzeile flaeche = s.Zeile(GebaeudeZielfelder.NUTZFLAECHE);
            flaeche.ManuellSetzen(null);
            Assert.True(flaeche.Uebernehmen);
            Assert.Contains(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel == G + "PFLICHT_FEHLT" && m.Werte[0] == GebaeudeZielfelder.NUTZFLAECHE);

            // Aufzählungsfelder und abgeleitete Felder nimmt die Handänderung nicht an.
            Assert.False(s.ManuellSetzen(GebaeudeZielfelder.BAUART, 1.0));
            Assert.False(s.ManuellSetzen("GIBT_ES_NICHT", 1.0));
            Assert.Throws<InvalidOperationException>(() => s.Zeile(GebaeudeZielfelder.BAUWEISE).ManuellSetzen(5000));
        }

        [Fact]
        public void Die_Pruefung_am_OK_verlangt_einen_Namen()
        {
            GebaeudeImportSatz s = GbxmlImportTests.Satz("gbxml_haus_si.xml");
            Assert.DoesNotContain(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel == G + "NAME_FEHLT");
            Assert.DoesNotContain(GebaeudeImportAblauf.Pruefen(s, "Haus 1"), m => m.Schluessel == G + "NAME_FEHLT");

            IReadOnlyList<PruefMeldung> ohne = GebaeudeZuordnungsModell.Pruefe(s, "  ");
            PruefMeldung m = ohne.Single(x => x.Schluessel == G + "NAME_FEHLT");
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.True(GebaeudeImportAblauf.Blockiert(ohne));
            Assert.Equal("Das neue Gebäude braucht einen Namen.", GebaeudeZuordnungsModell.MeldungText(m));
        }

        // ==================================================================
        //  Texte und Plattformgrenze
        // ==================================================================

        [Fact]
        public void Die_Texte_der_Oberflaeche_kommen_aus_dem_Modell()
        {
            Assert.Equal("gbXML", GebaeudeZuordnungsModell.FormatText(new GbxmlImportProfil()));
            Assert.Equal("X4 – eine Zone je Gebäude", GebaeudeZuordnungsModell.ZonenregelText(GebaeudeImportProfil.ZONENREGEL_X4));
            Assert.Equal("X9", GebaeudeZuordnungsModell.ZonenregelText("X9"));
            Assert.Equal("20,1 KB", GebaeudeZuordnungsModell.GroesseText(20555));
            Assert.Equal("25 MB", GebaeudeZuordnungsModell.GroesseText(GbxmlImportProfil.MAX_BYTES_WINDOWS));
            Assert.Equal("Die Gebäudedatei haus.xml wird gelesen …",
                         GebaeudeZuordnungsModell.FortschrittText(new ImportFortschritt(null, G + "LESEN", "haus.xml")));
            Assert.Equal("Flächen werden gelesen (100 von 400) …",
                         GebaeudeZuordnungsModell.FortschrittText(new ImportFortschritt(0.25, G + "FLAECHEN", "100", "400")));

            // E51: Der freie Wert reist wie jede Vorgabe als VORGABE; unterschieden wird über den Beleg.
            foreach (Importherkunft h in Enum.GetValues(typeof(Importherkunft)))
                Assert.Equal(h == Importherkunft.VorgabeFrei ? Importherkunft.Vorgabe : h,
                             GebaeudeZuordnungsModell.HerkunftAusSchluessel(GebaeudeZuordnungsModell.HerkunftSchluessel(h)));
            Assert.Equal("LEER", GebaeudeZuordnungsModell.HerkunftSchluessel(Importherkunft.Leer));
            Assert.Equal(ImportherkunftWerte.MANUELL, GebaeudeZuordnungsModell.HerkunftSchluessel(Importherkunft.Manuell));
        }

        [Fact]
        public void Die_Grenze_haengt_an_der_Plattform()
        {
            var profil = new GbxmlImportProfil();
            Assert.Equal(25L * 1024 * 1024, profil.GrenzeFuerPlattform(false));
            // E42 (25.09.2026): nach der Messung im iOS-Lauf gilt auf iOS dieselbe Grenze wie unter Windows.
            Assert.Equal(25L * 1024 * 1024, profil.GrenzeFuerPlattform(true));
            Assert.Equal(20L * 1024 * 1024, new IfcImportProfil().GrenzeFuerPlattform(true));
        }
    }
}
