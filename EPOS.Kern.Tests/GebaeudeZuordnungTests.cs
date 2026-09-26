using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Regeln der gemeinsamen Zuordnung (E2)</b> des Gebäudeimports — formatfrei, geprüft
    /// über kleine gbXML-Texte: flächengewichtetes U mit der 30-%-Regel, Fenstersektoren samt
    /// Grenzen, Fensterabzug (U14), Vorgaben der Baualtersklasse (U12, U15), Luftwechsel (D12),
    /// Hülle gegen Unbeheizt, Zonenvorschlag (Probe 9), Prüfung am OK-Weg und die Texte des
    /// Zuordnungsmodells.
    /// </summary>
    public sealed class GebaeudeZuordnungTests : IDisposable
    {
        private const string G = "IMP_GEB_PROT_";
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static string Masse(double breite, double hoehe)
            => "<Width>" + Z(breite) + "</Width><Height>" + Z(hoehe) + "</Height>";

        private static string Z(double w) => w.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

        private static string Konstruktion(string id, double u)
            => "<Construction id=\"" + id + "\"><U-value unit=\"WPerSquareMeterK\">" + Z(u) + "</U-value></Construction>";

        private static string Waagerecht(string id, string typ, double neigung, double breite, double hoehe, string kon = null,
                                         params (string Raum, string Sicht)[] nachbarn)
            => "<Surface id=\"" + id + "\" surfaceType=\"" + typ + "\"" + (kon == null ? "" : " constructionIdRef=\"" + kon + "\"") + ">"
               + string.Concat(nachbarn.Select(n => "<AdjacentSpaceId spaceIdRef=\"" + n.Raum + "\"" + (n.Sicht == null ? "" : " surfaceType=\"" + n.Sicht + "\"") + "/>"))
               + "<RectangularGeometry><Azimuth>0</Azimuth><Tilt>" + Z(neigung) + "</Tilt>" + Masse(breite, hoehe) + "</RectangularGeometry></Surface>";

        private static string Fenster(string id, double breite, double hoehe, string typ = "FixedWindow", double? u = null)
            => "<Opening id=\"" + id + "\" openingType=\"" + typ + "\"><RectangularGeometry>" + Masse(breite, hoehe) + "</RectangularGeometry>"
               + (u.HasValue ? "<U-value unit=\"WPerSquareMeterK\">" + Z(u.Value) + "</U-value>" : "") + "</Opening>";

        private static string Raum(string id, string zustand, double flaeche, double volumen, string zusatz = "", string name = null, string zone = null, string geschoss = null)
            => "<Space id=\"" + id + "\"" + (zustand == null ? "" : " conditionType=\"" + zustand + "\"")
               + (zone == null ? "" : " zoneIdRef=\"" + zone + "\"") + (geschoss == null ? "" : " buildingStoreyIdRef=\"" + geschoss + "\"") + ">"
               + "<Name>" + (name ?? id) + "</Name><Area>" + Z(flaeche) + "</Area><Volume>" + Z(volumen) + "</Volume>" + zusatz + "</Space>";

        private static GebaeudeImportSatz Satz(string flaechen, string katalog = "", char? klasse = 'F', string raeume = null)
        {
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(flaechen, raum: raeume, katalog: katalog));
            Assert.True(a.Gebaeude.Count == 1, string.Join(" | ", a.Meldungen));
            return a.Zuordnen(0, klasse);
        }

        private static void Nah(double erwartet, double? ist, double toleranz = 1e-9)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= toleranz, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        // ==================================================================
        //  U-Werte: flächengewichtet und die 30-%-Regel
        // ==================================================================

        [Fact]
        public void Der_U_Wert_einer_Gruppe_ist_flaechengewichtet()
        {
            // 10 m² × 0,5 und 30 m² × 1,5 → (5 + 45) / 40 = 1,25
            GebaeudeImportSatz s = Satz(
                Klein.Wand("aw-1", 0, Masse(4, 2.5), kon: "k-05") + Klein.Wand("aw-2", 180, Masse(12, 2.5), kon: "k-15"),
                Konstruktion("k-05", 0.5) + Konstruktion("k-15", 1.5));
            Nah(40.0, s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);
            Nah(1.25, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert);
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Assert.Equal("GIMP_BELEG_U_GEWICHTET", s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Beleg.Schluessel);
        }

        [Theory]
        [InlineData(28.0, 12.0, true)]    // 30 m² von 100 m² ohne U-Wert: genau 30 % — noch aus der Datei
        [InlineData(27.6, 12.4, false)]   // 31 % — die Gruppe gilt als „nicht aus der Datei"
        public void Fehlt_der_U_Wert_bei_mehr_als_30_Prozent_gilt_die_Vorgabe(double breiteMitU, double breiteOhneU, bool ausDatei)
        {
            GebaeudeImportSatz s = Satz(
                Klein.Wand("aw-mit", 0, Masse(breiteMitU, 2.5), kon: "k-1") + Klein.Wand("aw-ohne", 180, Masse(breiteOhneU, 2.5)),
                Konstruktion("k-1", 1.0), 'F');
            GebaeudeFeldzeile u = s.Zeile(GebaeudeZielfelder.U_AUSSENWAND);
            Nah(1.08, u.VorgabeWert);
            if (ausDatei)
            {
                Nah(1.0, u.Wert);
                Assert.Equal(Importherkunft.GbXml, u.Herkunft);
                Assert.DoesNotContain(s.Meldungen, m => m.Schluessel == G + "U_UNVOLLSTAENDIG");
            }
            else
            {
                Nah(1.08, u.Wert);
                Assert.Equal(Importherkunft.Vorgabe, u.Herkunft);
                PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "U_UNVOLLSTAENDIG");
                Assert.Equal(new[] { GebaeudeZielfelder.U_AUSSENWAND, "31" }, m.Werte);
            }
        }

        private static string Geschichtet(string id, params (string Id, double Dicke, double Lambda, double Rho, double Cp)[] schichten)
            => "<Construction id=\"" + id + "\"><LayerId layerIdRef=\"" + id + "-l\"/></Construction>"
               + "<Layer id=\"" + id + "-l\">" + string.Concat(schichten.Select(s => "<MaterialId materialIdRef=\"" + s.Id + "\"/>")) + "</Layer>"
               + string.Concat(schichten.Select(s => "<Material id=\"" + s.Id + "\"><Thickness>" + Z(s.Dicke) + "</Thickness>"
                   + "<Conductivity unit=\"WPerMeterK\">" + Z(s.Lambda) + "</Conductivity><Density unit=\"KgPerCubicM\">" + Z(s.Rho)
                   + "</Density><SpecificHeat unit=\"JPerKgK\">" + Z(s.Cp) + "</SpecificHeat></Material>"));

        [Fact]
        public void Ohne_eingetragenen_U_Wert_rechnet_die_Bauteilreduktion_aus_den_Schichten()
        {
            // außen Mauerwerk 0,24/0,8 = 0,30, innen Dämmung 0,10/0,04 = 2,50 → 1/(0,13 + 2,80 + 0,04)
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5), kon: "k-schicht"),
                Geschichtet("k-schicht", ("m-mw", 0.24, 0.8, 1600, 1000), ("m-dae", 0.10, 0.04, 30, 1500)));
            GebaeudeFeldzeile u = s.Zeile(GebaeudeZielfelder.U_AUSSENWAND);
            Nah(1.0 / 2.97, u.Wert, 1e-12);
            Assert.Equal(Importherkunft.GbXml, u.Herkunft);
            Assert.DoesNotContain(s.Meldungen, m => m.Schluessel == G + "SCHICHTEN_AUSSERHALB");
        }

        [Fact]
        public void Stoffwerte_ausserhalb_des_Bandes_werden_gemeldet_nicht_geworfen()
        {
            // λ = 1 000 W/(mK) liegt über dem Band (500) — die Reduktion würde werfen, der Import meldet.
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5), kon: "k-band") + Klein.Wand("aw-2", 180, Masse(10, 2.5), kon: "k-band"),
                Geschichtet("k-band", ("m-ausserhalb", 0.01, 1000, 2000, 1000)), 'F');
            GebaeudeFeldzeile u = s.Zeile(GebaeudeZielfelder.U_AUSSENWAND);
            Assert.Equal(Importherkunft.Vorgabe, u.Herkunft);
            Nah(1.08, u.Wert);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "SCHICHTEN_AUSSERHALB");   // einmal je Aufbau
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal("k-band", m.Werte[0]);
            Assert.False(string.IsNullOrWhiteSpace(m.Werte[1]));
        }

        // ==================================================================
        //  Fenster nach Himmelsrichtung
        // ==================================================================

        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(44.999, 0)]
        [InlineData(45.0, 1)]
        [InlineData(90.0, 1)]
        [InlineData(134.999, 1)]
        [InlineData(135.0, 2)]
        [InlineData(180.0, 2)]
        [InlineData(224.999, 2)]
        [InlineData(225.0, 3)]
        [InlineData(270.0, 3)]
        [InlineData(314.999, 3)]
        [InlineData(315.0, 0)]
        [InlineData(359.999, 0)]
        [InlineData(360.0, 0)]
        [InlineData(405.0, 1)]
        [InlineData(-45.0, 0)]
        [InlineData(-45.001, 3)]
        [InlineData(-135.0, 3)]   // −135° ≡ 225° — die Grenze gehört zu West
        public void Der_Sektor_ist_die_naechste_Mitte_die_Grenze_gehoert_zum_groesseren(double azimut, int sektor)
        {
            Assert.Equal(sektor, GebaeudeAggregation.Sektor(azimut));
        }

        [Fact]
        public void Fenster_ohne_Himmelsrichtung_verteilen_sich_gleichmaessig_mit_Warnung()
        {
            // Wirtswand ohne Azimut und ohne Polygon, dazu ein Oberlicht im Flachdach.
            string dach = "<Surface id=\"dach\" surfaceType=\"Roof\"><AdjacentSpaceId spaceIdRef=\"raum-1\"/><RectangularGeometry>"
                        + "<Azimuth>90</Azimuth><Tilt>0</Tilt>" + Masse(10, 5) + "</RectangularGeometry>"
                        + Fenster("oberlicht", 1, 2, "FixedSkylight") + "</Surface>";
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-ohne-azimut", null, Masse(10, 2.5), oeffnungen: Fenster("f-1", 1, 2)) + dach);
            foreach (string f in new[] { GebaeudeZielfelder.FENSTER_NORD, GebaeudeZielfelder.FENSTER_OST,
                                         GebaeudeZielfelder.FENSTER_SUED, GebaeudeZielfelder.FENSTER_WEST })
            {
                Nah(1.0, s.Zeile(f).Wert);
                Assert.Equal(PruefStufe.Warnung, s.Zeile(f).Markierung);
            }
            Nah(4.0, s.Zeile(GebaeudeZielfelder.FENSTER_GESAMT).Wert);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "FENSTER_OHNE_AZIMUT");
            Assert.Equal(new[] { "2", "4" }, m.Werte);
            Nah(48.0, s.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);   // 50 − 2 m² Oberlicht
        }

        // ==================================================================
        //  U14 — Fensterabzug
        // ==================================================================

        [Fact]
        public void U14_Wandflaeche_ist_brutto_minus_Fenster_minus_Aussentuer()
        {
            // 100 − 15 − 2 = 83
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 180, Masse(10, 10),
                oeffnungen: Fenster("f-1", 5, 3) + Fenster("t-1", 1, 2, "NonSlidingDoor", 2.0)));
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Nah(83.0, wand.Wert);
            Nah(100.0, wand.Bruttowert);
            Assert.Equal("GIMP_BELEG_FLAECHE_NETTO", wand.Beleg.Schluessel);
            Assert.Equal(new[] { "1", "100", "17" }, wand.Beleg.Werte);
            Nah(15.0, s.Zeile(GebaeudeZielfelder.FENSTER_SUED).Wert);
            Nah(2.0, s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);
            Nah(2.0, s.Zeile(GebaeudeZielfelder.U_SONSTIGE).Wert);
            Assert.Null(s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Markierung);
        }

        // ==================================================================
        //  U12/U15 — Vorgaben der Baualtersklasse
        // ==================================================================

        [Fact]
        public void U15_Psi_ist_Vorgabe_der_Klasse_die_Anschlusslaengen_bleiben_leer()
        {
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'E');   // E47: 1958 bis 1968
            Nah(0.11, s.Zeile(GebaeudeZielfelder.PSI_FENSTER_WAND).Wert);
            Nah(0.345, s.Zeile(GebaeudeZielfelder.PSI_WAND_DACH).Wert);
            Nah(0.63, s.Zeile(GebaeudeZielfelder.PSI_AUSSENWAND_KELLER).Wert);
            foreach (string f in new[] { GebaeudeZielfelder.PSI_FENSTER_WAND, GebaeudeZielfelder.PSI_WAND_DACH, GebaeudeZielfelder.PSI_AUSSENWAND_KELLER })
            {
                GebaeudeFeldzeile z = s.Zeile(f);
                Assert.Equal(Importherkunft.Vorgabe, z.Herkunft);
                Assert.Equal(z.Wert, z.VorgabeWert);
                Assert.Equal("GIMP_BELEG_VORGABE_KLASSE", z.VorgabeBeleg.Schluessel);
                Assert.Equal("E", z.VorgabeBeleg.Werte[0]);
                Assert.True(z.Uebernehmen);
            }
            foreach (string f in new[] { GebaeudeZielfelder.LAENGE_FENSTER_WAND, GebaeudeZielfelder.LAENGE_WAND_DACH, GebaeudeZielfelder.LAENGE_AUSSENWAND_KELLER })
            {
                GebaeudeFeldzeile z = s.Zeile(f);
                Assert.Null(z.Wert);
                Assert.Equal(Importherkunft.Leer, z.Herkunft);
                Assert.False(z.Uebernehmen);
                Assert.Equal("GIMP_BELEG_LAENGE_LEER", z.Beleg.Schluessel);
            }
        }

        [Fact]
        public void Ohne_Klasse_bleiben_die_Vorgaben_leer_ohne_Katalogsatz_gilt_der_freie_Wert()
        {
            GebaeudeImportSatz ohne = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: null);
            Assert.Contains(ohne.Meldungen, m => m.Schluessel == G + "KEINE_BAUALTERSKLASSE");
            Assert.Equal(Importherkunft.Leer, ohne.Zeile(GebaeudeZielfelder.PSI_WAND_DACH).Herkunft);
            Assert.Equal(Importherkunft.Leer, ohne.Zeile(GebaeudeZielfelder.U_DACH).Herkunft);
            Assert.Null(ohne.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE).Textwert);

            // Klasse M (klein geschrieben) hat Katalogsätze (E51): die Vorgaben kommen aus dem Katalog, keine Meldung.
            GebaeudeImportSatz t = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'm');
            Assert.Equal("M", t.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE).Textwert);
            Assert.DoesNotContain(t.Meldungen, m => m.Schluessel == G + "KLASSE_OHNE_VORGABE" || m.Schluessel == G + "KLASSE_VORGABE_FREI");
            Assert.Equal(GebaeudeVorgaben.Fuer('M').PsiWandDach, t.Zeile(GebaeudeZielfelder.PSI_WAND_DACH).Wert);
            Assert.Equal(GebaeudeVorgaben.BELEG_KLASSE, t.Zeile(GebaeudeZielfelder.U_AUSSENWAND).VorgabeBeleg.Schluessel);
            Assert.Equal(new[] { "M", "3" }, t.Zeile(GebaeudeZielfelder.U_AUSSENWAND).VorgabeBeleg.Werte);

            // Ohne Katalogsatz (Lesenaht) gilt der freie Wert der EIGENEN Klasse - als Info, mit Beleg;
            // ψ bleibt leer, und nie gilt die Vorgabe der Nachbarklasse L.
            GebaeudeImportSatz f;
            using (GebaeudeVorgaben.KatalogOhne("M"))
                f = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'M');
            PruefMeldung info = Assert.Single(f.Meldungen, m => m.Schluessel == G + "KLASSE_VORGABE_FREI");
            Assert.Equal(PruefStufe.Info, info.Stufe);
            Assert.Equal(new[] { "M", "2021–2025" }, info.Werte);
            Assert.DoesNotContain(f.Meldungen, m => m.Schluessel == G + "KLASSE_OHNE_VORGABE");
            GebaeudeFeldzeile uAw = f.Zeile(GebaeudeZielfelder.U_AUSSENWAND);
            Assert.Equal(0.16, uAw.VorgabeWert);
            Assert.NotEqual(GebaeudeVorgaben.Fuer('L').UAussenwand, uAw.VorgabeWert);
            Assert.Equal(GebaeudeVorgaben.BELEG_FREI, uAw.VorgabeBeleg.Schluessel);
            Assert.Equal(new[] { "M", "2021–2025" }, uAw.VorgabeBeleg.Werte);
            Assert.Null(f.Zeile(GebaeudeZielfelder.PSI_WAND_DACH).Wert);
            Assert.Equal(Importherkunft.Leer, f.Zeile(GebaeudeZielfelder.PSI_WAND_DACH).Herkunft);

            GebaeudeImportSatz falsch = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'Z');
            Assert.Null(falsch.Baualtersklasse);
            GebaeudeImportSatz alt = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'N');   // ein Buchstabe der alten Liste
            Assert.Null(alt.Baualtersklasse);
        }

        [Fact]
        public void Die_Vorgabentabelle_fuehrt_13_Klassen_alle_mit_Katalogsaetzen()
        {
            Assert.Equal(13, GebaeudeVorgaben.Alle.Count);
            Assert.Equal("ABCDEFGHIJKLM", new string(GebaeudeVorgaben.Alle.Select(v => v.Klasse).ToArray()));
            Assert.Equal(13, GebaeudeStammCtrl.BAUALTERSKLASSEN_DE.Length);
            // E51: auch A und M haben Katalogsätze (je drei), mit Wärmebrücken.
            foreach (char k in "AM")
            {
                Baualtersvorgabe v = GebaeudeVorgaben.Fuer(k);
                Assert.Equal(3, v.Katalogsaetze);
                Assert.False(v.Frei);
                Assert.NotNull(v.UAussenwand);
                Assert.NotNull(v.PsiWandDach);
            }
            Assert.Null(GebaeudeVorgaben.Fuer(null));
            Assert.Null(GebaeudeVorgaben.Fuer('N'));
            Assert.Equal(GebaeudeVorgaben.Fuer('a'), GebaeudeVorgaben.Fuer('A'));
            Assert.Null(GebaeudeVorgaben.Wert('A', GebaeudeZielfelder.NUTZFLAECHE));
        }

        // ==================================================================
        //  D12 — der Luftwechsel geht auf die Infiltration
        // ==================================================================

        [Fact]
        public void D12_der_Luftwechsel_geht_volumengewichtet_auf_die_Infiltration()
        {
            string raeume = Raum("raum-1", "Heated", 40, 100, "<AirChangesPerHour>0.4</AirChangesPerHour>")
                          + Raum("raum-2", "Heated", 120, 300, "<AirChangesPerHour>0.8</AirChangesPerHour>");
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), raeume: raeume);
            Nah(0.7, s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Wert);   // (0,4·100 + 0,8·300) / 400
            Assert.Equal("GIMP_BELEG_LUFTWECHSEL", s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Beleg.Schluessel);
            GebaeudeFeldzeile nutzer = s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_NUTZER);
            Assert.Null(nutzer.Wert);
            Assert.Equal(Importherkunft.Leer, nutzer.Herkunft);
            Assert.False(nutzer.Uebernehmen);

            // Die Luftwechselrate bleibt die ausgewiesene Vorgabe 0,3 + 0,4 — gerechnet wird mit der
            // gelesenen Infiltration und der Vorgabe der Nutzerlüftung, so sagt es der Beleg.
            LuftwechselrateIstVorgabe(s, "GIMP_BELEG_LUFTWECHSELRATE_VORGABE", 0.7 + GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER);

            // Nennt ein beheizter Raum keinen Luftwechsel, bleibt die Infiltration leer.
            string teilweise = Raum("raum-1", "Heated", 40, 100, "<AirChangesPerHour>0.4</AirChangesPerHour>") + Raum("raum-2", "Heated", 120, 300);
            GebaeudeImportSatz t = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), raeume: teilweise);
            Assert.Null(t.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Wert);
            Assert.Contains(t.Meldungen, m => m.Schluessel == G + "LUFTWECHSEL_UNVOLLSTAENDIG");
            // Infiltration und Nutzerlüftung leer: dann rechnet das Modell mit der Luftwechselrate selbst.
            LuftwechselrateIstVorgabe(t, "GIMP_BELEG_LUFTWECHSELRATE_VORGABE_WIRKT",
                GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION + GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER);
        }

        /// <summary>
        /// Die Luftwechselrate ist die Vorgabe Infiltration + Nutzerlüftung des Stundenmodells, und der
        /// Luftwechsel, mit dem gerechnet wird, ist der von <see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?)"/>.
        /// </summary>
        private static void LuftwechselrateIstVorgabe(GebaeudeImportSatz s, string beleg, double wirksam)
        {
            GebaeudeFeldzeile rate = s.Zeile(GebaeudeZielfelder.LUFTWECHSELRATE);
            double vorgabe = GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION + GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER;
            Assert.Equal(vorgabe, rate.Wert);
            Assert.Equal(vorgabe, rate.VorgabeWert);
            Assert.Equal(Importherkunft.Vorgabe, rate.Herkunft);
            Assert.True(rate.Uebernehmen);
            Assert.Equal(beleg, rate.Beleg.Schluessel);
            Nah(wirksam, Gebaeudemodellvorgaben.WirksamerLuftwechsel(rate.Wert,
                s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION).Wert, s.Zeile(GebaeudeZielfelder.LUFTWECHSEL_NUTZER).Wert));
            if (rate.Beleg.Werte.Length > 3)
                Nah(wirksam, double.Parse(rate.Beleg.Werte[3], System.Globalization.CultureInfo.InvariantCulture));
        }

        // ==================================================================
        //  Hülle gegen Unbeheizt, Erdreich und Außenluft
        // ==================================================================

        [Fact]
        public void Gegen_Unbeheizt_Boden_wird_Keller_Decke_wird_Dach_Wand_wird_Sonstige()
        {
            string raeume = Raum("raum-1", "Heated", 50, 125)
                          + Raum("raum-2", "Heated", 50, 125)
                          + Raum("keller", "Unconditioned", 50, 100)
                          + Raum("dachboden", "Vented", 50, 50)
                          + Raum("treppe", "Unconditioned", 10, 25);
            string flaechen =
                Waagerecht("boden", "InteriorFloor", 180, 10, 5, null, ("raum-1", "InteriorFloor"), ("keller", "Ceiling"))
              + Waagerecht("decke", "Ceiling", 0, 10, 5, null, ("raum-2", "Ceiling"), ("dachboden", "InteriorFloor"))
              + Klein.Wand("wand-treppe", 90, Masse(4, 2.5), "InteriorWall", null, "", "raum-1", "treppe")
              + Klein.Wand("wand-innen", 90, Masse(5, 2.5), "InteriorWall", null, "", "raum-1", "raum-2")
              + Waagerecht("geschossdecke", "Ceiling", 0, 10, 5, null, ("raum-1", "Ceiling"), ("raum-2", "InteriorFloor"));
            GebaeudeImportSatz s = Satz(flaechen, raeume: raeume);

            Nah(100.0, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            Nah(50.0, s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
            Assert.Equal(DbWerte.GRUND_KELLER, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
            Nah(50.0, s.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);
            Nah(10.0, s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);   // nur die Treppenhauswand
            Nah(0.0, s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "BODEN_GEGEN_UNBEHEIZT" && m.Werte[0] == "1");
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "DECKE_GEGEN_UNBEHEIZT" && m.Werte[1] == "50");
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "FLAECHE_GEGEN_UNBEHEIZT" && m.Werte[1] == "10");
            Assert.DoesNotContain(s.Quellzuordnungen, z => z.Quellkennung == "wand-innen" || z.Quellkennung == "geschossdecke");
        }

        [Fact]
        public void Ohne_Sicht_entscheidet_die_Neigung_vom_ersten_Nachbarn_aus()
        {
            string raeume = Raum("raum-1", "Heated", 50, 125) + Raum("keller", "Unconditioned", 50, 100)
                          + Raum("dachboden", "Unconditioned", 50, 50);
            // Die Normale zeigt vom ERSTEN Nachbarn weg: nach unten (180°) mit dem beheizten Raum
            // zuerst — sein Boden; nach oben (0°) mit dem beheizten Raum zuerst — seine Decke.
            string flaechen = Waagerecht("boden", "InteriorFloor", 180, 10, 5, null, ("raum-1", null), ("keller", null))
                            + Waagerecht("decke", "Ceiling", 0, 10, 5, null, ("raum-1", null), ("dachboden", null));
            GebaeudeImportSatz s = Satz(flaechen, raeume: raeume);
            Nah(50.0, s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
            Nah(50.0, s.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);

            // Dieselbe Decke aus Sicht des Dachbodens geschrieben (er zuerst, Normale nach unten).
            string umgekehrt = Waagerecht("decke", "InteriorFloor", 180, 10, 5, null, ("dachboden", null), ("raum-1", null));
            GebaeudeImportSatz u = Satz(umgekehrt, raeume: raeume);
            Nah(50.0, u.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert);
            Nah(0.0, u.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
        }

        [Fact]
        public void Gemischte_Randbedingung_nimmt_die_groessere_Flaeche_und_warnt()
        {
            string raeume = Raum("raum-1", "Heated", 80, 200) + Raum("keller", "Unconditioned", 50, 100);
            string flaechen = Waagerecht("bp", "SlabOnGrade", 180, 6, 5, null, ("raum-1", null))
                            + Waagerecht("kd", "InteriorFloor", 180, 10, 5, null, ("raum-1", "InteriorFloor"), ("keller", "Ceiling"));
            GebaeudeImportSatz s = Satz(flaechen, raeume: raeume);
            Nah(80.0, s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
            GebaeudeFeldzeile rand = s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG);
            Assert.Equal(DbWerte.GRUND_KELLER, rand.Textwert);
            Assert.Equal(PruefStufe.Warnung, rand.Markierung);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == G + "RANDBEDINGUNG_GEMISCHT");
            Assert.Equal(new[] { "30", "50", DbWerte.GRUND_KELLER }, m.Werte);
        }

        [Fact]
        public void Erdreichwaende_zaehlen_zur_Grundflaeche_Aussenluftboeden_zu_Sonstige()
        {
            string flaechen = Klein.Wand("kw", 0, Masse(10, 2), "UndergroundWall")
                            + Waagerecht("bp", "SlabOnGrade", 180, 10, 5, null, ("raum-1", null))
                            + Waagerecht("auskragung", "ExposedFloor", 180, 4, 5, null, ("raum-1", null))
                            + Klein.Wand("stuetze", 90, Masse(0.5, 2.5), "EmbeddedColumn");
            GebaeudeImportSatz s = Satz(flaechen);
            Nah(70.0, s.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert);
            Assert.Equal(DbWerte.GRUND_ERDREICH, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);
            Assert.Null(s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Markierung);
            Nah(21.25, s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);   // Außenluftboden 20 + Stütze 1,25
            Nah(0.0, s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);  // die Stütze zählt nie zur Außenwand
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "ERDREICH_ZUR_GRUNDFLAECHE" && m.Werte[1] == "20");
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "BODEN_AUSSENLUFT" && m.Werte[1] == "20");
        }

        [Theory]
        [InlineData(20.2, false)]   // 1 % — gleich genug, die größere zählt still
        [InlineData(21.0, true)]    // 5 % — gemeldet
        public void Eine_Trennflaeche_von_beiden_Seiten_zaehlt_einmal(double breiteZwei, bool gemeldet)
        {
            string raeume = Raum("raum-1", "Heated", 50, 125) + Raum("lager", "Unconditioned", 20, 50);
            string flaechen = Klein.Wand("tw-1", 90, Masse(20, 1), "InteriorWall", null, "", "raum-1", "lager")
                            + Klein.Wand("tw-2", 270, Masse(breiteZwei, 1), "InteriorWall", null, "", "lager", "raum-1");
            GebaeudeImportSatz s = Satz(flaechen, raeume: raeume);
            Nah(breiteZwei, s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);
            Assert.Equal(gemeldet, s.Meldungen.Any(m => m.Schluessel == "IMP_GBXML_PROT_TRENNFLAECHE_UNGLEICH"));
        }

        [Fact]
        public void Die_Namensregel_entscheidet_ohne_Zustandsangabe()
        {
            string raeume = Raum("r-1", null, 50, 125, name: "Wohnen") + Raum("r-2", null, 30, 60, name: "Kellerraum")
                          + Raum("r-3", "Heated", 20, 50, name: "Garage beheizt");
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0, Masse(10, 2.5), nachbarn: new[] { "r-1" }), raum: raeume));
            List<AbbildRaum> r = a.Abbild.Gebaeude[0].Raeume;
            Assert.True(r[0].Beheizt);
            Assert.Equal(BeheiztQuelle.Annahme, r[0].BeheiztQuelle);
            Assert.False(r[1].Beheizt);
            Assert.Equal(BeheiztQuelle.Name, r[1].BeheiztQuelle);
            Assert.True(r[2].Beheizt);   // das Attribut schlägt den Namen
            Assert.Equal(BeheiztQuelle.Attribut, r[2].BeheiztQuelle);

            GebaeudeImportSatz s = a.Zuordnen(0, 'F');
            Nah(70.0, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == "IMP_GBXML_PROT_UNBEHEIZT_NAME");
            Assert.Equal(new[] { "r-2", "Kellerraum", "Keller" }, m.Werte);
            Assert.Equal("Technik", Raumnamenregel.Treffer("Haustechnik"));
            Assert.True(Raumnamenregel.IstUnbeheizt("ATTIC"));
            Assert.False(Raumnamenregel.IstUnbeheizt("Bad"));
        }

        // ==================================================================
        //  Probe 9 — der Zonenvorschlag; gewählt bleibt X4
        // ==================================================================

        [Theory]
        [InlineData(12, 12, 2, "X2")]   // 1:1 ist keine Gliederung (D13) — zwei Geschosse: X2
        [InlineData(12, 12, 1, "X4")]   // ein Geschoss: X4
        [InlineData(26, 1, 1, "X1")]    // 26 Räume in einer Zone: X1
        public void Probe9_der_Zonenvorschlag_ist_nur_Auskunft(int raeume, int zonen, int geschosse, string vorschlag)
        {
            string r = string.Concat(Enumerable.Range(0, raeume).Select(i => Raum("raum-" + i, "Heated", 10, 25,
                zone: "zone-" + (i % zonen), geschoss: "gs-" + (i % geschosse))));
            string z = string.Concat(Enumerable.Range(0, zonen).Select(i => "<Zone id=\"zone-" + i + "\"><Name>Zone " + i + "</Name></Zone>"));
            GebaeudeImportAblauf a = Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0, Masse(10, 2.5), nachbarn: new[] { "raum-0" }), raum: r, katalog: z));
            GebaeudeImportSatz s = a.Zuordnen(0, 'F');
            Assert.Equal(vorschlag, s.Zonenvorschlag);
            Assert.Equal("X4", s.Zonenregel);
            Assert.Null(s.Ablehnung);
            Assert.Equal(vorschlag != "X4", s.Meldungen.Any(m => m.Schluessel == "IMP_GBXML_PROT_ZONENVORSCHLAG"));
            Nah(10.0 * raeume, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
        }

        // ==================================================================
        //  Prüfen am OK-Weg
        // ==================================================================

        [Fact]
        public void Pruefen_verlangt_Nutzflaeche_und_Raumhoehe_und_warnt_bei_U_und_g_ausserhalb()
        {
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)));
            Assert.Empty(GebaeudeImportAblauf.Pruefen(s));

            s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert = 7.0;
            s.Zeile(GebaeudeZielfelder.G_WERT).Wert = 1.2;
            s.Zeile(GebaeudeZielfelder.RAUMHOEHE).Uebernehmen = false;
            IReadOnlyList<PruefMeldung> p = GebaeudeImportAblauf.Pruefen(s);
            Assert.Contains(p, m => m.Stufe == PruefStufe.Warnung && m.Schluessel == G + "U_AUSSERHALB"
                                    && m.Werte.SequenceEqual(new[] { GebaeudeZielfelder.U_AUSSENWAND, "7", "0.1", "6" }));
            Assert.Contains(p, m => m.Stufe == PruefStufe.Warnung && m.Schluessel == G + "G_AUSSERHALB");
            Assert.Contains(p, m => m.Stufe == PruefStufe.Fehler && m.Schluessel == G + "PFLICHT_FEHLT" && m.Werte[0] == GebaeudeZielfelder.RAUMHOEHE);
            Assert.True(GebaeudeImportAblauf.Blockiert(p));
            Assert.Equal(p.Select(m => m.ToString()), GebaeudeZuordnungsModell.Pruefe(s).Select(m => m.ToString()));

            // Die Prüfgröße: Volumen gegen Nutzfläche × Raumhöhe.
            s.Zeile(GebaeudeZielfelder.RAUMHOEHE).Uebernehmen = true;
            s.Zeile(GebaeudeZielfelder.RAUMHOEHE).Wert = 3.5;
            Assert.Contains(GebaeudeImportAblauf.Pruefen(s), m => m.Schluessel == G + "VOLUMEN_ABWEICHUNG");
        }

        [Fact]
        public void Ohne_beheizten_Raum_bleibt_alles_leer_und_die_Pruefung_sperrt()
        {
            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)),
                                        raeume: Raum("raum-1", "Unconditioned", 50, 125));
            Assert.Contains(s.Meldungen, m => m.Schluessel == G + "KEINE_BEHEIZTEN_RAEUME");
            Assert.Null(s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert);
            Nah(0.0, s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert);
            Assert.True(GebaeudeImportAblauf.Blockiert(GebaeudeImportAblauf.Pruefen(s)));
        }

        // ==================================================================
        //  Zielfelder, Quelle und Texte
        // ==================================================================

        [Fact]
        public void Die_Zielfelder_sind_eindeutig_geordnet_und_haben_je_eine_Zeile()
        {
            IReadOnlyList<GebaeudeZielfeld> alle = GebaeudeZielfelder.Alle;
            Assert.Equal(alle.Count, alle.Select(f => f.Schluessel).Distinct().Count());
            Assert.Equal(Enumerable.Range(1, alle.Count), alle.Select(f => f.Reihenfolge));
            Assert.All(alle, f => Assert.Matches("^[A-Z_]+$", f.Schluessel));
            Assert.All(alle, f => Assert.Matches("^[A-Z_]+$", f.Gruppe));

            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)));
            Assert.Equal(alle.Select(f => f.Schluessel), s.Zeilen.Select(z => z.Zielfeld));
            Assert.Throws<ArgumentException>(() => new GebaeudeFeldzeile("GIBT_ES_NICHT"));
            Assert.Equal(Gebaeudebauweise.SCHWER, GebaeudeZielfelder.BauartIndex(GebaeudeZielfelder.BAUART_SCHWER));
            Assert.Equal(-1, GebaeudeZielfelder.BauartIndex("MITTEL"));
        }

        [Theory]
        [InlineData("C:\\Projekte\\Haus\\modell.xml", "modell.xml")]
        [InlineData("/var/mobile/Containers/Data/Application/1A2B/Documents/modell.gbxml", "modell.gbxml")]
        [InlineData("modell.xml", "modell.xml")]
        [InlineData(null, "")]
        public void Die_Quelle_fuehrt_nur_den_Dateinamen(string pfad, string name)
        {
            Assert.Equal(name, GebaeudeQuelle.NurName(pfad));
            Assert.Equal(name, new GebaeudeQuelle("GBXML", pfad, "", 0, null, "", null, "X4", 0).Dateiname);
        }

        [Fact]
        public void Die_Persistenzwerte_der_Herkunft_folgen_dem_Wertebereich_der_Spalte()
        {
            Assert.Equal("GBXML", ImportherkunftWerte.Wert(Importherkunft.GbXml));
            Assert.Equal("IFC", ImportherkunftWerte.Wert(Importherkunft.Ifc));
            Assert.Equal("KATALOG", ImportherkunftWerte.Wert(Importherkunft.Katalog));
            Assert.Equal("MANUELL", ImportherkunftWerte.Wert(Importherkunft.Manuell));
            Assert.Equal("VORGABE", ImportherkunftWerte.Wert(Importherkunft.Vorgabe));
            Assert.Equal("VORGABE", ImportherkunftWerte.Wert(Importherkunft.VorgabeFrei));   // E51: kein neuer Datenbankwert
            Assert.True(ImportherkunftWerte.IstVorgabe(Importherkunft.VorgabeFrei));
            Assert.False(ImportherkunftWerte.IstVorgabe(Importherkunft.Manuell));
            Assert.Null(ImportherkunftWerte.Wert(Importherkunft.Leer));
        }

        [Fact]
        public void Die_Texte_kommen_aus_den_Ressourcen()
        {
            Assert.Equal("Vorgabe", GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Vorgabe));
            Assert.Equal("Vorgabe (freier Wert)", GebaeudeZuordnungsModell.HerkunftText(Importherkunft.VorgabeFrei));
            Assert.Equal("gbXML-Datei", GebaeudeZuordnungsModell.HerkunftText(Importherkunft.GbXml));
            Assert.Equal("Nutzfläche", GebaeudeZuordnungsModell.FeldText(GebaeudeZielfelder.NUTZFLAECHE));
            Assert.Equal("Wärmebrücken", GebaeudeZuordnungsModell.GruppenText(GebaeudeZielfelder.GRUPPE_WAERMEBRUECKEN));
            Assert.Equal("gbXML-Version 0.37", GebaeudeZuordnungsModell.SchemaText(new GbxmlImportProfil(), "0.37"));

            GebaeudeImportSatz s = Satz(Klein.Wand("aw-1", 0, Masse(10, 2.5)), klasse: 'F');
            Assert.Equal("Schwere Bauart", GebaeudeZuordnungsModell.WertText(s.Zeile(GebaeudeZielfelder.BAUART)));
            Assert.Equal("F – 1969 bis 1978", GebaeudeZuordnungsModell.WertText(s.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE)));
            Assert.Equal("25", GebaeudeZuordnungsModell.WertText(s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND)));
            Assert.Equal("—", GebaeudeZuordnungsModell.WertText(s.Zeile(GebaeudeZielfelder.LAENGE_WAND_DACH)));
            Assert.Equal("Außenwand · Fläche Außenwand: 25 m² (gbXML-Datei)",
                         GebaeudeZuordnungsModell.ZeilenText(s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND)));
            Assert.Equal("1 Bauteile", GebaeudeZuordnungsModell.BelegText(s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Beleg));
            Assert.StartsWith("probe.xml · Gebäude „Haus“ · Baualtersklasse F – 1969 bis 1978 · ", GebaeudeZuordnungsModell.KopfText(s));

            // Ein Zielfeldschlüssel in einer Meldung erscheint mit seiner Beschriftung.
            string text = GebaeudeZuordnungsModell.MeldungText(new PruefMeldung(PruefStufe.Fehler, G + "PFLICHT_FEHLT", GebaeudeZielfelder.RAUMHOEHE));
            Assert.StartsWith("Raumhöhe fehlt", text);
            Assert.Contains("Keller (unbeheizt)", GebaeudeZuordnungsModell.MeldungText(
                new PruefMeldung(PruefStufe.Warnung, G + "RANDBEDINGUNG_GEMISCHT", "30", "50", DbWerte.GRUND_KELLER)));
        }

        [Fact]
        public void Jeder_Schluessel_steht_in_beiden_Sprachen()
        {
            Dictionary<string, string> de = Resx("Resource.resx");
            Dictionary<string, string> en = Resx("Resource.en-US.resx");
            var schluessel = new List<string>();
            schluessel.AddRange(GebaeudeZielfelder.Alle.Select(f => "GIMP_FELD_" + f.Schluessel));
            schluessel.AddRange(GebaeudeZielfelder.Alle.Select(f => "GIMP_GRP_" + f.Gruppe).Distinct());
            schluessel.AddRange(Enum.GetNames(typeof(Importherkunft)).Select(n => "GIMP_HERKUNFT_" + n.ToUpperInvariant()));
            // Die achtzehn Fehlerbilder aus Datenaustauschkonzept 3.8.
            schluessel.AddRange(new[]
            {
                "ZU_GROSS", "LESEFEHLER", "KEIN_CAMPUS", "VERSION_UNBEKANNT", "EINHEIT_UNBEKANNT", "KEINE_KONSTRUKTIONEN",
                "OHNE_AUFBAU", "AUFBAU_MASSELOS", "NACHBAR_UNBEKANNT", "OHNE_NACHBAR", "VERWEIS_LEER", "KENNUNG_GEKUERZT",
                "NORDDREHUNG", "GEOMETRIE_FEHLT", "TRENNFLAECHE_UNGLEICH", "NETTOFLAECHE_NEGATIV", "ZU_VIELE_ZONEN", "KEIN_NORDEN",
            }.Select(n => "IMP_GBXML_PROT_" + n));
            // Jeder Meldungs- und Belegschlüssel, den der Quelltext wörtlich nennt — samt den Meldungen
            // des Bauteilvorschlags (IMP_BAUTEIL_PROT_*, Konstanten in GebaeudeBauteilvorschlag).
            foreach (string datei in Quelltexte())
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), "\"((?:GIMP|IMP_GEB_PROT|IMP_GBXML_PROT|IMP_BAUTEIL_PROT)_[A-Z_]+)\""))
                    schluessel.Add(m.Groups[1].Value);
            Assert.True(schluessel.Distinct().Count(k => k.StartsWith("IMP_BAUTEIL_PROT_", StringComparison.Ordinal)) >= 29,
                        "Die Meldungskennungen des Bauteilvorschlags sind nicht gesammelt.");
            foreach (string datei in Quelltexte())
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), "(?:MELDUNG|GebaeudeImportAblauf\\.MELDUNG) \\+ \"([A-Z_]+)\""))
                    schluessel.Add("IMP_GEB_PROT_" + m.Groups[1].Value);
            foreach (string datei in Quelltexte())
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), "(?:Datei\\(PruefStufe\\.[A-Za-z]+, |P \\+ )\"([A-Z_]+)\""))
                    schluessel.Add("IMP_GBXML_PROT_" + m.Groups[1].Value);
            foreach (string datei in Quelltexte())
                foreach (Match m in Regex.Matches(File.ReadAllText(datei), "Zaehlen\\(zaehler, \"([A-Z_]+)\""))
                    schluessel.Add("IMP_GEB_PROT_" + m.Groups[1].Value);

            var funde = new List<string>();
            foreach (string k in schluessel.Where(k => !k.EndsWith("_", StringComparison.Ordinal)).Distinct())
            {
                if (!de.TryGetValue(k, out string d) || d.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.resx");
                if (!en.TryGetValue(k, out string e) || e.Trim().Length == 0) funde.Add(k + ": fehlt in Resource.en-US.resx");
            }
            Assert.True(schluessel.Count > 100, "Nur " + schluessel.Count + " Schlüssel gesammelt.");
            Assert.True(funde.Count == 0, string.Join("\n", funde));
        }

        private static IEnumerable<string> Quelltexte()
        {
            foreach (string ordner in new[] { "Gebaeude", "Gbxml" })
                foreach (string datei in Directory.GetFiles(Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Import", ordner), "*.cs"))
                    yield return datei;
        }

        private static Dictionary<string, string> Resx(string datei)
        {
            string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.Kern", "MyResource", datei));
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(text, @"<data name=""(?<k>[^""]+)""[^>]*>\s*<value>(?<v>.*?)</value>", RegexOptions.Singleline))
                d[m.Groups["k"].Value] = System.Net.WebUtility.HtmlDecode(m.Groups["v"].Value);
            return d;
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
