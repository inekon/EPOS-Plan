using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6b, Welle W1 — die Regeln zwischen den Zonen, die Vorgabenkaskade und Σ H_T</b>,
    /// ohne Datenbank: <see cref="Zonenkopplungsregeln"/> (Nachbar, Randbedingung und Nachbar nur
    /// gemeinsam, Trennflächenbilanz, Luftströme als Paare, mindestens eine beheizte Zone, ψ·L der
    /// wärmeren Zone, geschlossene Hülle als Hinweis), ihr Ruf aus
    /// <see cref="GebaeudeZonenCtrl.Pruefen(IList{ZoneModel})"/>, die Freigabe von <c>ZONE</c> im
    /// Bauteildialog (<see cref="GebaeudeZonenCtrl.BauteilPruefen"/>), die Kaskade
    /// <see cref="Zonenvorgaben"/> und die Regel, dass eine Grenze <c>ZONE</c> nicht in das H_T des
    /// Gebäudes zählt.
    /// </summary>
    public class GebaeudeZonenkopplungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static string F(string muster, params object[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);

        // =================================================================================
        //  Vorrichtung: zwei Zonen, ein Quader von 10 × 10 × 3 m je Zone, übereinander
        // =================================================================================

        private static BauteilModel Wand(int id, double azimut, double flaeche = 30.0)
            => new BauteilModel { ID = id, Bezeichner = "Wand " + azimut.ToString(CultureInfo.InvariantCulture),
                                  Bauteilart = DbWerte.BAUTEILART_AUSSENWAND, Flaeche = flaeche, U_Wert = 0.3, Azimut = azimut };

        private static BauteilModel Flaeche(int id, string art, double flaeche = 100.0, string rand = null)
            => new BauteilModel { ID = id, Bezeichner = art, Bauteilart = art, Flaeche = flaeche, U_Wert = 0.25, Randbedingung = rand };

        /// <summary>
        /// Das Erdgeschoss (−1): vier Wände, Bodenplatte am Erdreich und die Decke zum Obergeschoss als
        /// Trennfläche; das Obergeschoss (−2): vier Wände und das Dach. Beide geschlossen.
        /// </summary>
        private static List<ZoneModel> Zwei()
        {
            var eg = new ZoneModel
            {
                ID = -1, Bezeichner = "EG", Nutzflaeche = 100,
                Bauteile =
                {
                    Wand(-1, 0), Wand(-2, 90), Wand(-3, 180), Wand(-4, 270),
                    Flaeche(-5, DbWerte.BAUTEILART_BODENPLATTE, rand: DbWerte.RANDBEDINGUNG_ERDREICH),
                    new BauteilModel { ID = -6, Bezeichner = "Decke EG/OG", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 100,
                                       U_Wert = 1.2, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = -2 },
                }
            };
            var og = new ZoneModel
            {
                ID = -2, Bezeichner = "OG", Nutzflaeche = 100,
                Bauteile = { Wand(-7, 0), Wand(-8, 90), Wand(-9, 180), Wand(-10, 270), Flaeche(-11, DbWerte.BAUTEILART_DACH) }
            };
            return new List<ZoneModel> { eg, og };
        }

        private static BauteilModel Trennflaeche(List<ZoneModel> z) => z[0].Bauteile.Single(b => b.ID == -6);

        // =================================================================================
        //  Zonenkopplungsregeln.Pruefen
        // =================================================================================

        [Fact]
        public void Zwei_gekoppelte_Zonen_sind_gueltig_und_geschlossen()
        {
            List<ZoneModel> z = Zwei();
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
            Assert.Null(Zonenkopplungsregeln.Pruefen(z, new List<ZonenluftstromModel>()));
            Assert.Empty(Zonenkopplungsregeln.Hinweise(z));
            Assert.Null(GebaeudeZonenCtrl.Pruefen(z));

            // Die Decke ist von oben gesehen der Boden des OG: gespiegelt gezählt.
            Zonenkopplungsregeln.Huellbilanz og = Zonenkopplungsregeln.Bilanz(z, 1);
            Assert.Equal(100.0, og.Oben, 9);
            Assert.Equal(100.0, og.Unten, 9);
            Assert.True(og.AzimuteVollstaendig);
            Assert.True(og.AbweichungWaagerecht < 1e-9);
        }

        [Fact]
        public void Randbedingung_und_Nachbar_stehen_nur_gemeinsam()
        {
            List<ZoneModel> z = Zwei();
            Trennflaeche(z).ID_Nachbarzone = null;
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_FEHLT, "Decke EG/OG", "EG"), Zonenkopplungsregeln.Pruefen(z));

            z = Zwei();
            Trennflaeche(z).Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT;
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_OHNE_RAND, "Decke EG/OG", "EG"), Zonenkopplungsregeln.Pruefen(z));

            // Die Zuordnung IW/AW nur an einer Trennfläche und nur mit einem bekannten Wert.
            z = Zwei();
            Trennflaeche(z).Trennflaeche_Zuordnung = DbWerte.TRENNFLAECHE_AW;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
            Trennflaeche(z).Trennflaeche_Zuordnung = "XX";
            Assert.Equal(F(R.ZONE_MSG_TRENNFLAECHE_ZUORDNUNG, "Decke EG/OG", "EG", "XX"), Zonenkopplungsregeln.Pruefen(z));
            z = Zwei();
            z[1].Bauteile[0].Trennflaeche_Zuordnung = DbWerte.TRENNFLAECHE_IW;
            Assert.Equal(F(R.ZONE_MSG_TRENNFLAECHE_ZUORDNUNG, "Wand 0", "OG", "IW"), Zonenkopplungsregeln.Pruefen(z));
        }

        [Fact]
        public void Der_Nachbar_liegt_im_selben_Gebaeude_und_ist_nicht_die_eigene_Zone()
        {
            List<ZoneModel> z = Zwei();
            Trennflaeche(z).ID_Nachbarzone = 4711;
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_FREMD, "Decke EG/OG", "EG", 4711), Zonenkopplungsregeln.Pruefen(z));

            Trennflaeche(z).ID_Nachbarzone = -1;
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_EIGEN, "Decke EG/OG", "EG"), Zonenkopplungsregeln.Pruefen(z));

            // Eine vorläufige Id, die zwei Zonen tragen, ist als Nachbar mehrdeutig.
            z = Zwei();
            z.Add(new ZoneModel { ID = -2, Bezeichner = "DG", Nutzflaeche = 50 });
            Assert.Equal(F(R.ZONE_MSG_NACHBAR_MEHRDEUTIG, "Decke EG/OG", "EG", -2), Zonenkopplungsregeln.Pruefen(z));

            // Gespeicherte Zonen heißen über ihre positive Id.
            z = Zwei();
            z[0].ID = 11;
            z[1].ID = 12;
            Trennflaeche(z).ID_Nachbarzone = 12;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
        }

        [Fact]
        public void Je_Zonenpaar_fuehrt_nur_eine_Seite_die_Trennflaechen()
        {
            List<ZoneModel> z = Zwei();
            z[1].Bauteile.Add(new BauteilModel
            {
                ID = -12, Bezeichner = "Boden OG", Bauteilart = DbWerte.BAUTEILART_DECKE, Flaeche = 100, U_Wert = 1.2,
                Neigung = 180, Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = -1
            });
            Assert.Equal(F(R.ZONE_MSG_TRENNFLAECHE_BEIDSEITIG, "EG", "OG"), Zonenkopplungsregeln.Pruefen(z));

            // Zwei Trennflächen derselben Seite sind erlaubt (etwa Decke und Treppenwand).
            z = Zwei();
            z[0].Bauteile.Add(new BauteilModel
            {
                ID = -13, Bezeichner = "Treppenwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 8, U_Wert = 1.5,
                Randbedingung = DbWerte.RANDBEDINGUNG_ZONE, ID_Nachbarzone = -2
            });
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
        }

        [Fact]
        public void Mindestens_eine_Zone_ist_beheizt()
        {
            List<ZoneModel> z = Zwei();
            z[1].IstBeheizt = false;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
            z[0].IstBeheizt = false;
            Assert.Equal(R.ZONE_MSG_KEINE_BEHEIZT, Zonenkopplungsregeln.Pruefen(z));
            Assert.Equal(R.ZONE_MSG_KEINE_BEHEIZT, GebaeudeZonenCtrl.Pruefen(z));

            // Eine einzelne unbeheizte Zone ebenso; keine Zone ist kein Fall der Regel.
            Assert.Equal(R.ZONE_MSG_KEINE_BEHEIZT,
                         Zonenkopplungsregeln.Pruefen(new List<ZoneModel> { new ZoneModel { ID = -1, Bezeichner = "Lager", IstBeheizt = false } }));
            Assert.Null(Zonenkopplungsregeln.Pruefen(new List<ZoneModel>()));
        }

        [Fact]
        public void Die_Waermebruecke_der_Zonengrenze_gehoert_der_waermeren_Zone()
        {
            // Das EG führt die Decke mit ψ·L; ist das OG wärmer, gehört ψ·L dorthin.
            List<ZoneModel> z = Zwei();
            Trennflaeche(z).Psi_L = 2.0;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
            z[1].Raumsolltemperatur_Tag = 22.0;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));                     // EG ohne eigenen Wert, ohne Gebäudewert: kein Vergleich
            Assert.Equal(F(R.ZONE_MSG_PSI_KALTE_SEITE, "Decke EG/OG", "EG", "OG"), Zonenkopplungsregeln.Pruefen(z, null, 20.0));
            z[0].Raumsolltemperatur_Tag = 22.0;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z, null, 20.0));         // gleich warm

            // Beheizt ist wärmer als unbeheizt.
            z = Zwei();
            Trennflaeche(z).Psi_L = 2.0;
            z[0].IstBeheizt = false;
            Assert.Equal(F(R.ZONE_MSG_PSI_KALTE_SEITE, "Decke EG/OG", "EG", "OG"), Zonenkopplungsregeln.Pruefen(z));
            z[0].IstBeheizt = true;
            z[1].IstBeheizt = false;
            Assert.Null(Zonenkopplungsregeln.Pruefen(z));
        }

        [Fact]
        public void Luftstroeme_stehen_nur_als_Paare()
        {
            List<ZoneModel> z = Zwei();
            ZonenluftstromModel L(int a, int b, double v) => new ZonenluftstromModel { ID_ZoneA = a, ID_ZoneB = b, Volumenstrom = v };

            Assert.Null(Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, 50) }));
            Assert.Null(Zonenkopplungsregeln.Pruefen(z, new[] { L(-2, -1, 50) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_DOPPELT, "OG", "EG"), Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, 50), L(-2, -1, 20) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_EIGEN, "EG"), Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -1, 50) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_ZONE, 99), Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, 99, 50) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_WERT, "EG", "OG", "0"), Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, 0) }));
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_WERT, "EG", "OG", (-5.0).ToString("0.###", CultureInfo.CurrentCulture)),
                         Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, -5) }));
            Assert.NotNull(Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, double.NaN) }));
            Assert.NotNull(Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, double.PositiveInfinity) }));

            z.Add(new ZoneModel { ID = -2, Bezeichner = "DG", Nutzflaeche = 50 });
            Trennflaeche(z).Randbedingung = null;
            Trennflaeche(z).ID_Nachbarzone = null;
            Assert.Equal(F(R.ZONE_MSG_LUFTSTROM_MEHRDEUTIG, -2), Zonenkopplungsregeln.Pruefen(z, new[] { L(-1, -2, 50) }));
        }

        // =================================================================================
        //  Hinweise: die geschlossene Hülle
        // =================================================================================

        [Fact]
        public void Eine_offene_Huelle_wird_ab_zwei_Zonen_benannt()
        {
            // Ohne Dach: das OG weist nach unten 100 m² (die gespiegelte Decke), nach oben nichts.
            List<ZoneModel> z = Zwei();
            z[1].Bauteile.RemoveAll(b => b.Bauteilart == DbWerte.BAUTEILART_DACH);
            IReadOnlyList<string> h = Zonenkopplungsregeln.Hinweise(z);
            Assert.Equal(new[] { F(R.ZONE_HINWEIS_HUELLE_SENKRECHT, "OG", "0", "100", "100") }, h);

            // Eine fehlende Wand: der waagerechte Flächenvektor bleibt stehen (30 gegen 45 m²).
            z = Zwei();
            z[0].Bauteile.RemoveAll(b => b.ID == -3);
            Assert.Equal(new[] { F(R.ZONE_HINWEIS_HUELLE_WAAGERECHT, "EG", "67") }, Zonenkopplungsregeln.Hinweise(z));

            // Unter 10 % schweigt der Hinweis; ohne Azimut entfällt die waagerechte Probe.
            z = Zwei();
            z[0].Bauteile.Single(b => b.ID == -1).Flaeche = 32.0;
            Assert.Empty(Zonenkopplungsregeln.Hinweise(z));
            z[0].Bauteile.RemoveAll(b => b.ID == -3);
            z[0].Bauteile.Single(b => b.ID == -1).Azimut = null;
            z[0].Bauteile.Single(b => b.ID == -1).Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT;
            Assert.False(Zonenkopplungsregeln.Bilanz(z, 0).AzimuteVollstaendig);
            Assert.Empty(Zonenkopplungsregeln.Hinweise(z));

            // Eine einzelne Zone bekommt keinen Hüllenhinweis (übernommene Katalogflächen).
            z = Zwei();
            z[1].Bauteile.RemoveAll(b => b.Bauteilart == DbWerte.BAUTEILART_DACH);
            Assert.Empty(Zonenkopplungsregeln.Hinweise(new List<ZoneModel> { z[1] }));
        }

        [Fact]
        public void Innenbauteile_zaehlen_nicht_zur_Huelle()
        {
            List<ZoneModel> z = Zwei();
            z[0].Bauteile.Add(Flaeche(-20, DbWerte.BAUTEILART_DECKE, 80.0));           // innerhalb der Zone
            z[0].Bauteile.Add(new BauteilModel { ID = -21, Bezeichner = "Innenwand", Bauteilart = DbWerte.BAUTEILART_INNENWAND, Flaeche = 40 });
            Assert.Empty(Zonenkopplungsregeln.Hinweise(z));
        }

        // =================================================================================
        //  Der Bauteildialog gibt ZONE frei
        // =================================================================================

        private static Bauteilangabe Wandangabe(Func<Bauteilangabe, Bauteilangabe> aendern = null)
        {
            var b = new Bauteilangabe("Trennwand", DbWerte.BAUTEILART_INNENWAND, 12.0, 1.5, false,
                                      null, null, null, null, null, DbWerte.RANDBEDINGUNG_ZONE, null, ID_Nachbarzone: -2);
            return aendern == null ? b : aendern(b);
        }

        [Fact]
        public void Der_Bauteildialog_gibt_die_Nachbarzone_frei()
        {
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wandangabe()));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { TrennflaecheZuordnung = DbWerte.TRENNFLAECHE_IW })));
            Assert.Equal(F(R.BAUTEIL_MSG_RAND_ZONE, "Trennwand"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { ID_Nachbarzone = null })));
            Assert.Equal(F(R.BAUTEIL_MSG_NACHBAR_OHNE_RAND, "Trennwand"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT })));
            Assert.Equal(F(R.BAUTEIL_MSG_TRENNFLAECHE_ZUORDNUNG, "Trennwand", "IW"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { Randbedingung = DbWerte.RANDBEDINGUNG_UNBEHEIZT,
                                                                                     ID_Nachbarzone = null,
                                                                                     TrennflaecheZuordnung = DbWerte.TRENNFLAECHE_IW })));
            Assert.Equal(F(R.BAUTEIL_MSG_TRENNFLAECHE_ZUORDNUNG, "Trennwand", "XX"),
                         GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { TrennflaecheZuordnung = "XX" })));

            // Festlegung 4: ZONE gilt wie UNBEHEIZT auch für Fenster und Tür; ohne U-Wert benannt.
            var fenster = new Bauteilangabe("Innenfenster", DbWerte.BAUTEILART_FENSTER, 2.0, 2.8, false,
                                            0.6, 0.3, 1.0, null, null, DbWerte.RANDBEDINGUNG_ZONE, null, ID_Nachbarzone: -2);
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(fenster));
            Assert.Equal(F(R.BAUTEIL_MSG_UWERT_FEHLT, "Innenfenster"), GebaeudeZonenCtrl.BauteilPruefen(fenster with { UWert = null }));
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(new Bauteilangabe("Tür", DbWerte.BAUTEILART_TUER, 2.0, 1.8, false,
                null, null, null, null, null, DbWerte.RANDBEDINGUNG_ZONE, null, ID_Nachbarzone: -2)));
            // Eine Trennfläche braucht keinen Azimut.
            Assert.Null(GebaeudeZonenCtrl.BauteilPruefen(Wandangabe(b => b with { Bauteilart = DbWerte.BAUTEILART_AUSSENWAND })));
        }

        // =================================================================================
        //  Σ H_T: eine Grenze ZONE zählt nicht in die Hülle des Gebäudes
        // =================================================================================

        [Fact]
        public void Eine_Trennflaeche_zaehlt_nicht_in_das_HT_des_Gebaeudes()
        {
            var ohne = new[] { new Zonenbauteil(DbWerte.BAUTEILART_AUSSENWAND, null!, 30, 0.3, null) };
            var mit = ohne.Append(new Zonenbauteil(DbWerte.BAUTEILART_INNENWAND, DbWerte.RANDBEDINGUNG_ZONE, 12, 1.5, 0.5)).ToArray();
            Zonenkennwerte a = Zonenkennwerte.Bilden(40, null, null, ohne, 100, 2.5, 0.5);
            Zonenkennwerte b = Zonenkennwerte.Bilden(40, null, null, mit, 100, 2.5, 0.5);
            Assert.Equal(a.HT + 0.5, b.HT, 12);                                          // ψ·L zählt, U·A der Trennfläche nicht
            Assert.Equal(Gebaeudehuellbilanz.Zonenzeilen(ohne.Select(x => (x.Bauteilart, x.Randbedingung, x.Flaeche, x.UWert))),
                         Gebaeudehuellbilanz.Zonenzeilen(mit.Select(x => (x.Bauteilart, x.Randbedingung, x.Flaeche, x.UWert))));

            // Ein Fenster zur Nachbarzone ebenso; UNBEHEIZT zählt weiter (Kellerregel).
            var fenster = ohne.Append(new Zonenbauteil(DbWerte.BAUTEILART_FENSTER, DbWerte.RANDBEDINGUNG_ZONE, 2, 2.8, null)).ToArray();
            Assert.Equal(a.HT, Zonenkennwerte.Bilden(40, null, null, fenster, 100, 2.5, 0.5).HT, 12);
            var keller = ohne.Append(new Zonenbauteil(DbWerte.BAUTEILART_DECKE, DbWerte.RANDBEDINGUNG_UNBEHEIZT, 40, 0.5, null)).ToArray();
            Assert.Equal(a.HT + 20.0, Zonenkennwerte.Bilden(40, null, null, keller, 100, 2.5, 0.5).HT, 12);
        }

        // =================================================================================
        //  Die Vorgabenkaskade
        // =================================================================================

        private static Gebaeudevorgaben Gebaeude(double? heizgrenze = 12.0)
            => Gebaeudevorgaben.Aus(new GebaeudeModel
            {
                Nutzflaeche = 201.0, Raumhoehe = 2.75, Raumsolltemperatur_Tag = 20.0, Raumsolltemperatur_Nachtabsenkung = 18.0,
                Raumsolltemperatur_Wochenende = 19.0, Raumsolltemperatur_Ferien = 16.0, Maximaleraumtemperatur = 24.0,
                Heizung_Strahlungsanteil = 0.3, Heizleistung_Max = heizgrenze, Luftwechselrate = 0.7,
                Luftwechsel_Infiltration = 0.2, Luftwechsel_Nutzer = null, Interne_Waermegewinne = 462.0, Bewohner = 5.0,
                Kuehlung_Aktiv = true, Kuehl_Sollwert = 26.0, Kuehl_Sollwert_Nacht = null, Kuehlleistung_Max = 8.0,
            });

        [Fact]
        public void Eine_Zone_ohne_Uebersteuerung_erbt_jedes_Bit_des_Gebaeudes()
        {
            Gebaeudevorgaben g = Gebaeude();
            foreach (int n in new[] { 1, 2 })
            {
                Zonenvorgaben v = Zonenvorgaben.Bilden(new ZoneModel { ID = -1, Bezeichner = "Z" }, g, n);
                Assert.Equal(1.0, v.Flaechenanteil);
                Assert.Equal(new Vorgabewert(201.0, Vorgabeherkunft.Gebaeude), v.Nutzflaeche);
                Assert.Equal(new Vorgabewert(2.75, Vorgabeherkunft.Gebaeude), v.Raumhoehe);
                Assert.Equal(new Vorgabewert(201.0 * 2.75, Vorgabeherkunft.Abgeleitet), v.Volumen);
                Assert.Equal(new Vorgabewert(20.0, Vorgabeherkunft.Gebaeude), v.SollTag);
                Assert.Equal(new Vorgabewert(18.0, Vorgabeherkunft.Gebaeude), v.SollNacht);
                Assert.Equal(new Vorgabewert(19.0, Vorgabeherkunft.Gebaeude), v.SollWochenende);
                Assert.Equal(new Vorgabewert(16.0, Vorgabeherkunft.Gebaeude), v.SollFerien);
                Assert.Equal(new Vorgabewert(24.0, Vorgabeherkunft.Gebaeude), v.Maximaleraumtemperatur);
                Assert.Equal(new Vorgabewert(0.3, Vorgabeherkunft.Gebaeude), v.HeizungStrahlungsanteil);
                Assert.Equal(new Vorgabewert(12.0, Vorgabeherkunft.Gebaeude), v.HeizleistungMaxKw);
                Assert.Equal(new Vorgabewert(0.2, Vorgabeherkunft.Gebaeude), v.LuftwechselInfiltration);
                Assert.Equal(new Vorgabewert(null, Vorgabeherkunft.Leer), v.LuftwechselNutzer);
                Assert.Equal(0.7, v.Luftwechselrate);
                Assert.Equal(new Vorgabewert(462.0, Vorgabeherkunft.Gebaeude), v.InterneWaermegewinne);
                Assert.Equal(new Vorgabewert(5.0, Vorgabeherkunft.Gebaeude), v.Bewohner);
                Assert.Equal(new Vorgabewert(8.0, Vorgabeherkunft.Gebaeude), v.KuehlleistungMaxKw);
                Assert.True(v.IstBeheizt);
                Assert.True(v.SollTag.IstVorgabe);
            }
        }

        [Fact]
        public void Der_Zonenwert_geht_vor()
        {
            var zone = new ZoneModel
            {
                ID = 3, Bezeichner = "Büro", Nutzflaeche = 201.0, Raumhoehe = 3.0, Volumen = 650.0, IstBeheizt = false,
                Raumsolltemperatur_Tag = 21.0, Raumsolltemperatur_Nachtabsenkung = 17.0, Raumsolltemperatur_Wochenende = 15.0,
                Raumsolltemperatur_Ferien = 12.0, Maximaleraumtemperatur = 26.0, Heizung_Strahlungsanteil = 0.5,
                Heizleistung_Max = 3.0, Luftwechsel_Infiltration = 0.1, Luftwechsel_Nutzer = 0.4,
                Interne_Waermegewinne = 100.0, Bewohner = 2.0,
            };
            Zonenvorgaben v = Zonenvorgaben.Bilden(zone, Gebaeude(), 3);
            Assert.Equal(new Vorgabewert(3.0, Vorgabeherkunft.Zone), v.Raumhoehe);
            Assert.Equal(new Vorgabewert(650.0, Vorgabeherkunft.Zone), v.Volumen);
            Assert.False(v.IstBeheizt);
            Assert.Equal(new Vorgabewert(21.0, Vorgabeherkunft.Zone), v.SollTag);
            Assert.Equal(new Vorgabewert(17.0, Vorgabeherkunft.Zone), v.SollNacht);
            Assert.Equal(new Vorgabewert(15.0, Vorgabeherkunft.Zone), v.SollWochenende);
            Assert.Equal(new Vorgabewert(12.0, Vorgabeherkunft.Zone), v.SollFerien);
            Assert.Equal(new Vorgabewert(26.0, Vorgabeherkunft.Zone), v.Maximaleraumtemperatur);
            Assert.Equal(new Vorgabewert(0.5, Vorgabeherkunft.Zone), v.HeizungStrahlungsanteil);
            Assert.Equal(new Vorgabewert(3.0, Vorgabeherkunft.Zone), v.HeizleistungMaxKw);
            Assert.Equal(new Vorgabewert(0.1, Vorgabeherkunft.Zone), v.LuftwechselInfiltration);
            Assert.Equal(new Vorgabewert(0.4, Vorgabeherkunft.Zone), v.LuftwechselNutzer);
            Assert.Equal(new Vorgabewert(100.0, Vorgabeherkunft.Zone), v.InterneWaermegewinne);
            Assert.Equal(new Vorgabewert(2.0, Vorgabeherkunft.Zone), v.Bewohner);
            Assert.False(v.SollTag.IstVorgabe);
        }

        [Fact]
        public void Der_Flaechenschluessel_teilt_Gewinne_und_ab_zwei_Zonen_die_Grenzen()
        {
            Gebaeudevorgaben g = Gebaeude();
            var zone = new ZoneModel { ID = -1, Bezeichner = "Hälfte", Nutzflaeche = 80.4 };
            double anteil = 80.4 / 201.0;

            Zonenvorgaben eine = Zonenvorgaben.Bilden(zone, g, 1);
            Assert.Equal(anteil, eine.Flaechenanteil);
            // Dieselbe Schreibweise wie der Lauf (GebaeudeModellEingang.Flaechenschluessel): Wert × (A_Zone / A_Gebäude).
            Assert.Equal(new Vorgabewert(462.0 * anteil, Vorgabeherkunft.GebaeudeAnteilig), eine.InterneWaermegewinne);
            Assert.Equal(new Vorgabewert(5.0 * anteil, Vorgabeherkunft.GebaeudeAnteilig), eine.Bewohner);
            // Festlegung 5: bei einer Zone bleibt die Grenze des Gebäudes (G3-Stand) ...
            Assert.Equal(new Vorgabewert(12.0, Vorgabeherkunft.Gebaeude), eine.HeizleistungMaxKw);
            Assert.Equal(new Vorgabewert(8.0, Vorgabeherkunft.Gebaeude), eine.KuehlleistungMaxKw);
            // ... ab zwei Zonen wird sie nach Fläche geteilt.
            Zonenvorgaben zwei = Zonenvorgaben.Bilden(zone, g, 2);
            Assert.Equal(new Vorgabewert(12.0 * anteil, Vorgabeherkunft.GebaeudeAnteilig), zwei.HeizleistungMaxKw);
            Assert.Equal(new Vorgabewert(8.0 * anteil, Vorgabeherkunft.GebaeudeAnteilig), zwei.KuehlleistungMaxKw);
            Assert.Equal(new Vorgabewert(80.4 * 2.75, Vorgabeherkunft.Abgeleitet), zwei.Volumen);

            // Ohne Grenze am Gebäude keine Grenze; ohne Nutzfläche des Gebäudes kein Schlüssel.
            Assert.Equal(new Vorgabewert(null, Vorgabeherkunft.Leer), Zonenvorgaben.Bilden(zone, Gebaeude(null), 2).HeizleistungMaxKw);
            Zonenvorgaben ohne = Zonenvorgaben.Bilden(zone, Gebaeude() with { Nutzflaeche = 0.0 }, 2);
            Assert.True(double.IsNaN(ohne.Flaechenanteil));
            Assert.True(double.IsNaN(ohne.InterneWaermegewinne.Wert!.Value));
        }

        [Fact]
        public void Die_Kuehlwerte_kommen_vom_Gebaeude()
        {
            // A4 (a): Tab_Zone.Kuehl_* bleiben ungelesen.
            var zone = new ZoneModel
            {
                ID = -1, Bezeichner = "Z", Kuehlung_Aktiv = false, Kuehl_Sollwert = 22.0, Kuehl_Sollwert_Nacht = 25.0, Kuehlleistung_Max = 1.0,
            };
            Zonenvorgaben v = Zonenvorgaben.Bilden(zone, Gebaeude(), 2);
            Assert.True(v.KuehlungAktiv);
            Assert.Equal(new Vorgabewert(26.0, Vorgabeherkunft.Gebaeude), v.KuehlSollwert);
            Assert.Equal(new Vorgabewert(null, Vorgabeherkunft.Leer), v.KuehlSollwertNacht);
            Assert.Equal(new Vorgabewert(8.0, Vorgabeherkunft.Gebaeude), v.KuehlleistungMaxKw);
        }

        [Fact]
        public void Katalogsatz_und_Projektgebaeude_liefern_dieselben_Vorgaben()
        {
            ProjektGebaeudeModel p = Vdi6007Probe.Gebaeude();
            p.Heizleistung_Max = 9.0;
            p.Luftwechsel_Nutzer = 0.3;
            var m = new GebaeudeModel
            {
                Nutzflaeche = p.Nutzflaeche, Raumhoehe = p.Raumhoehe, Raumsolltemperatur_Tag = p.Raumsolltemperatur_Tag,
                Raumsolltemperatur_Nachtabsenkung = p.Raumsolltemperatur_Nachtabsenkung,
                Raumsolltemperatur_Wochenende = p.Raumsolltemperatur_Wochenende, Raumsolltemperatur_Ferien = p.Raumsolltemperatur_Ferien,
                Maximaleraumtemperatur = p.Maximaleraumtemperatur, Heizung_Strahlungsanteil = p.Heizung_Strahlungsanteil,
                Heizleistung_Max = p.Heizleistung_Max, Luftwechselrate = p.Luftwechselrate,
                Luftwechsel_Infiltration = p.Luftwechsel_Infiltration, Luftwechsel_Nutzer = p.Luftwechsel_Nutzer,
                Interne_Waermegewinne = p.Interne_Waermegewinne, Bewohner = p.Bewohner, Kuehlung_Aktiv = p.Kuehlung_Aktiv,
                Kuehl_Sollwert = p.Kuehl_Sollwert, Kuehl_Sollwert_Nacht = p.Kuehl_Sollwert_Nacht, Kuehlleistung_Max = p.Kuehlleistung_Max,
            };
            Assert.Equal(Gebaeudevorgaben.Aus(m), Gebaeudevorgaben.Aus(p));
        }
    }
}
