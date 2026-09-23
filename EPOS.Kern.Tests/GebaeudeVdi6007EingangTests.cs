using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Ein Gebäude und eine Klimareihe für die Rechenproben des VDI-Wegs (Stufe G1). Das
    /// Gebäude trägt die Werte des Katalogbaus aus Rechenschritte Kapitel 9 (EPOS-Zahlenweg,
    /// keine Zahl einer Richtlinie); die Klimareihe ist synthetisch — ein Jahresgang der
    /// Temperatur mit Tagesgang und eine einfache Tagesglocke der Strahlung.
    /// </summary>
    internal static class Vdi6007Probe
    {
        internal const double LAENGE = 11.0;
        internal const double BREITE = 48.0;

        internal static ProjektGebaeudeModel Gebaeude()
        {
            return new ProjektGebaeudeModel
            {
                ID_Gebaeude = 4711,
                Gebaeudename = "Probegebäude",
                Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007,
                Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE,
                Z_AuswahlWohnflaeche = 201.0,
                Wohnflaeche_gesamt = 201.0,
                Nutzflaeche = 201.0,
                Flaeche_Nutzer = 40.0,
                Raumhoehe = 2.75,
                Bauweise = 10050.0,
                k_Wert_Außenwand = 1.84,
                k_Wert_Fenster = 2.8,
                k_Wert_Dachflaeche = 0.8,
                k_Wert_Grundflaeche = 0.8,
                k_Wert_Sonstiges = 3.5,
                Flaeche_Außenwand = 280.28,
                gesamte_Fensterflaeche = 45.06,
                Dachflaeche = 116.86,
                Grundflaeche = 88.0,
                Sonstige_Flaechen = 2.1,
                Fensterflaeche_Sued = 10.5,
                Fensterflaeche_OstWest = 20.16,
                Fensterflaeche_Nord = 14.4,
                Fensterdurchlassgrad = 0.75,
                Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = 0.15,
                Abmessung_Anschluß_Fenster_Wand = 160.0,
                Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = 0.40,
                Abmessung_Anschluß_Wand_Dach = 202.02,
                Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = 0.70,
                Abmessung_Anschluß_Außenwand_Kellerdecke = 2.5,
                Luftwechselrate = 0.7,
                Interne_Waermegewinne = 462.0,
                Raumsolltemperatur_Tag = 20.0,
                Raumsolltemperatur_Nachtabsenkung = 18.0,
                Raumsolltemperatur_Wochenende = 0.0,
                Raumsolltemperatur_Ferien = 0.0,
                Maximaleraumtemperatur = 24.0,
                Ferienbeginn_1 = 366.0,
            };
        }

        /// <summary>Eine Klimareihe, Ortszeit = UTC-Reihenfolge (die Probe braucht keine Umsortierung).</summary>
        internal static SolardatenModel[] Klima(Func<int, double> temperatur, bool mitSonne = true)
        {
            var z = new SolardatenModel[8760];
            for (int h = 0; h < 8760; h++)
            {
                int stunde = h % 24;
                double ghi = 0.0;
                if (mitSonne && stunde >= 6 && stunde <= 17)
                    ghi = 600.0 * Math.Sin(Math.PI * (stunde - 5) / 13.0);
                z[h] = new SolardatenModel
                {
                    TagUtc = h / 24 + 1,
                    StundeUtc = stunde,
                    Außen_Temp = temperatur(h),
                    Globalstrahlung = ghi,
                    Direktstrahlung = 0.8 * ghi,
                    Diffusstrahlung = 0.4 * ghi,
                };
            }
            return z;
        }

        /// <summary>Jahresgang 10 °C ± 12 K (Minimum Mitte Januar) mit Tagesgang ± 3 K.</summary>
        internal static double Jahresgang(int h)
            => 10.0 - 12.0 * Math.Cos(2.0 * Math.PI * (h - 360.0) / 8760.0) - 3.0 * Math.Cos(2.0 * Math.PI * (h % 24 - 3) / 24.0);

        internal static bool[] Wochenende() => KlimakalenderGemeinsam.WochenendmaskeBilden(2025);

        internal static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, SolardatenModel[] klima,
                                                      Zeitbezug bezug = GebaeudeKlimaweg.ZEITBEZUG_VORGABE)
            => GebaeudeModellEingang.Bauen(g, klima, Wochenende(), LAENGE, BREITE, bezug);

        /// <summary>
        /// Das Probegebäude mit wirksamer Kühlung (Stufe KU1): Haken, Kühlsollwert und, wenn
        /// gesetzt, Kühlleistungsgrenze — wirksam nur in einem Eingang mit Projektschalter
        /// (<see cref="EingangGekuehlt"/>). Mit dem Kühlsollwert gleich der oberen Raumtemperatur
        /// (24 °C) und ohne Grenze regelt der Löser genau dort, wo er vor Entscheid E32 jedes
        /// Gebäude an θ_max hielt — die Proben, die Heiz- UND Kühlanteil brauchen, rechnen so.
        /// </summary>
        internal static ProjektGebaeudeModel Gekuehlt(double sollwert = 24.0, double? grenzeKw = null)
        {
            ProjektGebaeudeModel g = Gebaeude();
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = sollwert;
            g.Kuehlleistung_Max = grenzeKw;
            return g;
        }

        /// <summary>Wie <see cref="Eingang"/>, in einem Projekt mit Kühlbetrieb (Projektschalter ein).</summary>
        internal static GebaeudeModellEingang EingangGekuehlt(ProjektGebaeudeModel g, SolardatenModel[] klima)
            => GebaeudeModellEingang.Bauen(g, klima, Wochenende(), LAENGE, BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE,
                                           kuehlbetrieb: true);

        internal static KlimakalenderGemeinsam Kalender(SolardatenModel[] klima)
        {
            var mo = new int[12];
            var gemeinsam = new KlimakalenderGemeinsam(new bool[365], new double[8760], mo, mo)
            {
                SolarOrtszeit = klima,
                Laengengrad = LAENGE,
                Breitengrad = BREITE,
                Referenzjahr = 2025,
                WochenendeOrtszeit = Wochenende(),
            };
            return gemeinsam;
        }
    }

    /// <summary>
    /// <b>Stufe G1 — Klassenweg, Klimaweg und Eingangsbauer ohne Datenbank</b>
    /// (Umsetzungskonzept 1.9, Rechenschritte 10.4): Klassenweg gegen den Zahlenweg aus
    /// Rechenschritte 9.1, benannte Plausibilitätsfehler, Erdreich und Zeitbezug des
    /// Klimawegs, Lastaufteilung, äquivalente Außentemperatur, Sollwertfahrplan und
    /// Wochenendmaske.
    /// </summary>
    public class GebaeudeVdi6007EingangTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeVdi6007EingangTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        private static GebaeudeModellFehler Grund(Action a)
        {
            var ex = Assert.Throws<GebaeudeModellException>(a);
            return ex.Grund;
        }

        // =====================================================================
        //  Schritt A — Klassenweg
        // =====================================================================

        /// <summary>
        /// Die Größen, die nicht von der Fensterkonvention abhängen, folgen dem Zahlenweg aus
        /// Rechenschritte 9.1 (dort sechsstellig gedruckt).
        /// </summary>
        [Fact]
        public void Der_Klassenweg_folgt_dem_Zahlenweg_der_Rechenschritte()
        {
            GebaeudeModellEingang e = GebaeudeModellEingang.Daten(Vdi6007Probe.Gebaeude());
            ErsatzparameterRC p = ErsatzparameterRC.AusKlassenweg(e);

            Nahe(1.0854e7, p.C_AW_Jk, 1e-4);
            Nahe(2.5326e7, p.C_IW_Jk, 1e-4);
            Nahe(2.25536e-4, p.R_1_AW_KW, 1e-5);
            Nahe(9.63358e-4, p.R_Rest_AW_KW, 1e-5);
            Nahe(2.18687e-4, p.R_1_IW_KW, 1e-5);
            Nahe(7.37055e-4, p.R_conv_IW_KW, 1e-5);
            Nahe(686.953, p.SummeUA_opak_WK, 1e-5);
            Nahe(487.24, p.A_AW_opak_M2, 1e-9);
            Nahe(502.5, p.A_IW_M2, 1e-9);

            // E14: Fenster in der Außenwandgruppe, R_ext nur Lüftung + Wärmebrücken.
            Nahe(1.0 / (131.5545 + 106.558), p.R_ext_KW, 1e-5);
            Nahe(126.168, p.UA_Fenster_WK, 1e-6);
            Nahe(532.30, p.A_AW_gesamt_M2, 1e-9);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);

            // Zweigsumme des Fensters = 1/(U·A)_w (A7a).
            double zweig = p.R_1_AF_KW + p.R_Rest_AF_KW + p.R_alphaInnen_KW * p.A_AW_gesamt_M2 / p.A_Fenster_M2;
            Nahe(1.0 / 126.168, zweig, 1e-9);
        }

        [Fact]
        public void Plausibilitaetsfehler_sind_benannt()
        {
            Assert.Equal(GebaeudeModellFehler.BauweiseUnplausibel, Grund(() => Klassenweg(g => g.Bauweise = 50.0)));
            Assert.Equal(GebaeudeModellFehler.UWertUnplausibel, Grund(() => Klassenweg(g => g.k_Wert_Außenwand = 8.0)));
            Assert.Equal(GebaeudeModellFehler.GWertUnplausibel, Grund(() => Klassenweg(g => g.Fensterdurchlassgrad = 0.0)));
            Assert.Equal(GebaeudeModellFehler.PflichtgroesseFehlt, Grund(() => Klassenweg(g => g.Nutzflaeche = 0.0)));
            // Stufe G2: ohne Luftwechselrate und ohne Infiltration/Nutzerluftung gilt die Vorgabe
            // (0,3 + 0,4 1/h) - kein Fehler mehr; eine gesetzte Infiltration <= 0 ist benannt.
            Klassenweg(g => g.Luftwechselrate = 0.0);
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Luftwechsel_Infiltration = 0.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Luftwechsel_Nutzer = -0.1)));
            Assert.Equal(GebaeudeModellFehler.PflichtgroesseFehlt, Grund(() => Klassenweg(g => g.Raumhoehe = 0.0)));
            Assert.Equal(GebaeudeModellFehler.FensterzweigUngueltig, Grund(() => Klassenweg(g => g.k_Wert_Fenster = 6.0)));

            Assert.Equal(GebaeudeModellFehler.FensterflaechenWidersprechen, Grund(() => Klassenweg(g => g.Fensterflaeche_Nord = 20.0)));
            Assert.Equal(GebaeudeModellFehler.PflichtgroesseFehlt, Grund(() => Klassenweg(g => g.Flaeche_Nutzer = 0.0)));
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig, Grund(() => Klassenweg(g => g.Maximaleraumtemperatur = 20.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Rahmenanteil = 1.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Verschattungsfaktor = 0.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Masseanteil_Aussen = 1.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Heizleistung_Max = 0.0)));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, Grund(() => Klassenweg(g => g.Grundflaeche_Randbedingung = "WASSER")));

            // Aktiver Ferienfahrplan mit einem Tag außerhalb 1 … 365: benannt; 0 und 366 heißen „aus".
            Assert.Equal(GebaeudeModellFehler.SollwertfahrplanUngueltig, Grund(() => Klassenweg(g =>
            {
                g.Ferien = 1; g.Raumsolltemperatur_Ferien = 12; g.Ferienbeginn_2 = 100; g.Ferienende_2 = 400;
            })));
            Klassenweg(g => { g.Ferien = 1; g.Raumsolltemperatur_Ferien = 12; g.Ferienbeginn_2 = 0; g.Ferienende_2 = 366; });

            // Keine Klimareihe ⇒ benannt, kein stiller Lauf.
            Assert.Equal(GebaeudeModellFehler.KlimadatenUnvollstaendig, Grund(() =>
                GebaeudeModellEingang.Bauen(Vdi6007Probe.Gebaeude(), new SolardatenModel[100], Vdi6007Probe.Wochenende(), 11, 48)));
            Assert.Equal(GebaeudeModellFehler.KlimadatenUnvollstaendig, Grund(() =>
                GebaeudeModellEingang.Bauen(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                                            Vdi6007Probe.Wochenende(), double.NaN, 48)));
        }

        private static void Klassenweg(Action<ProjektGebaeudeModel> aendern)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            aendern(g);
            ErsatzparameterRC.AusKlassenweg(GebaeudeModellEingang.Daten(g));
        }

        [Fact]
        public void Ost_und_West_fallen_ohne_Angabe_je_zur_Haelfte_auf_das_Bestandsfeld()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang e = GebaeudeModellEingang.Daten(g);
            Assert.Equal(10.08, e.A_FensterOst_M2, 12);
            Assert.Equal(10.08, e.A_FensterWest_M2, 12);

            g.Fensterflaeche_Ost = 15.16;
            g.Fensterflaeche_West = 5.0;
            e = GebaeudeModellEingang.Daten(g);
            Assert.Equal(15.16, e.A_FensterOst_M2, 12);
            Assert.Equal(5.0, e.A_FensterWest_M2, 12);
        }

        // =====================================================================
        //  Schritt E — Klimaweg und Eingangsbauer
        // =====================================================================

        [Fact]
        public void Die_Erdreichtemperatur_daempft_und_verschiebt_nach_E6()
        {
            // Ein reiner Jahresgang: Dämpfung exp(−z/d), Phasenverzug z/d mit d = sqrt(2a/ω).
            double[] luft = new double[8760];
            for (int h = 0; h < 8760; h++) luft[h] = 10.0 - 8.0 * Math.Cos(2.0 * Math.PI * (h - 400.0) / 8760.0);
            double[] erde = GebaeudeKlimaweg.Grundtemperatur(DbWerte.GRUND_ERDREICH, 10.0, luft, out bool ausKlima);

            Assert.True(ausKlima);
            double amplitude = (erde.Max() - erde.Min()) / 2.0;
            Nahe(0.6847 * 8.0, amplitude, 2e-3);
            int minLuft = Array.IndexOf(luft, luft.Min());
            int minErde = Array.IndexOf(erde, erde.Min());
            Assert.InRange(minErde - minLuft, 22 * 24 - 12, 22 * 24 + 12);

            Assert.Equal(12.5, GebaeudeKlimaweg.Grundtemperatur(DbWerte.GRUND_KELLER, 12.5, luft, out _)[100]);
            Assert.Equal(luft[100], GebaeudeKlimaweg.Grundtemperatur(DbWerte.GRUND_AUSSENLUFT, 10.0, luft, out _)[100]);
        }

        [Fact]
        public void Die_Fassadenstrahlung_folgt_der_Himmelsrichtung_und_dem_Zeitbezug()
        {
            SolardatenModel[] k = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            Fassadenstrahlung a = GebaeudeKlimaweg.Fassaden(k, 11, 48, Zeitbezug.Stundenanfang);
            Fassadenstrahlung m = GebaeudeKlimaweg.Fassaden(k, 11, 48, Zeitbezug.Stundenmitte);

            Assert.True(a.Sued.Sum() > a.Nord.Sum());
            for (int h = 0; h < 8760; h++)
                if (k[h].Globalstrahlung == 0.0)
                {
                    Assert.Equal(0.0, a.Ost[h]);
                    Assert.Equal(0.0, m.West[h]);
                }

            // Eine halbe Stunde später steht die Sonne weiter westlich: West gewinnt, Ost verliert.
            Assert.True(m.West.Sum() > a.West.Sum());
            Assert.True(m.Ost.Sum() < a.Ost.Sum());
            _ausgabe.WriteLine("Synthetisch: Ost {0:F0} → {1:F0}, West {2:F0} → {3:F0} Wh/m²a (Anfang → Mitte)",
                               a.Ost.Sum(), m.Ost.Sum(), a.West.Sum(), m.West.Sum());
        }

        [Fact]
        public void Der_Eingangsbauer_teilt_die_Lasten_vollstaendig_und_flaechenproportional_auf()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            ErsatzparameterRC p = e.Parameter;
            double aRaum = p.A_AW_gesamt_M2 + p.A_IW_M2;

            for (int h = 0; h < 8760; h += 7)
            {
                double summe = e.PhiRadAW[h] + e.PhiRadIW[h] + e.PhiConv[h];
                Assert.Equal(e.PhiSolar[h] + 462.0, summe, 9);
                Assert.Equal(p.A_AW_gesamt_M2 / p.A_IW_M2, e.PhiRadAW[h] / e.PhiRadIW[h], 9);
                Assert.Equal(0.09 * e.PhiSolar[h] + 231.0, e.PhiConv[h], 9);
                Assert.Equal((1 - 0.09) * e.PhiSolar[h] * p.A_IW_M2 / aRaum + 231.0 * p.A_IW_M2 / aRaum, e.PhiRadIW[h], 9);
            }
            Assert.True(e.PhiSolar.Max() > 0.0);
        }

        [Fact]
        public void Die_aequivalente_Aussentemperatur_ist_UA_gewichtet_mit_dem_Erdreich()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            SolardatenModel[] k = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, k);

            double uaG = 0.8 * 88.0;
            double uaAlle = 686.953 - 0.0 + 126.168;
            for (int h = 0; h < 8760; h += 97)
            {
                double erwartet = ((uaAlle - uaG) * e.ThetaOut[h] + uaG * e.ThetaGrund[h]) / uaAlle;
                Assert.Equal(erwartet, e.ThetaEq[h], 3);
            }

            g.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            e = Vdi6007Probe.Eingang(g, k);
            for (int h = 0; h < 8760; h += 97) Assert.Equal(e.ThetaOut[h], e.ThetaEq[h], 12);
        }

        [Fact]
        public void Der_Sollwertfahrplan_folgt_E8()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = 16.0;
            g.Ferien = 1; g.Raumsolltemperatur_Ferien = 12.0;
            g.Ferienbeginn_2 = 100; g.Ferienende_2 = 101;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));

            // 2025: 1. Januar Mittwoch → Tag 0 werktags, Tag 3/4 Wochenende.
            Assert.Equal(18.0, e.ThetaSoll[5]);    // Stunde des Tages 6
            Assert.Equal(20.0, e.ThetaSoll[6]);    // Stunde des Tages 7
            Assert.Equal(20.0, e.ThetaSoll[21]);   // Stunde des Tages 22
            Assert.Equal(18.0, e.ThetaSoll[22]);   // Stunde des Tages 23
            Assert.Equal(16.0, e.ThetaSoll[3 * 24 + 12]);
            Assert.Equal(12.0, e.ThetaSoll[99 * 24 + 12]);   // Ferien vor Wochenende
            Assert.Equal(12.0, e.ThetaSoll[100 * 24 + 2]);

            // E32: Ohne wirksame Kühlung hat der Löser keine obere Grenze - das Gebäude läuft
            // frei; die obere Raumtemperatur bleibt allein die Grenze der Überhitzungskennzahl.
            Assert.True(double.IsPositiveInfinity(e.ThetaMax[4000]));
            Assert.Equal(24.0, e.ThetaMaxWert);
        }

        [Fact]
        public void Die_Wochenendmaske_steht_auf_dem_Kalender_des_Referenzjahres()
        {
            bool[] m2025 = KlimakalenderGemeinsam.WochenendmaskeBilden(2025);   // 1.1. Mittwoch
            Assert.False(m2025[0]);
            Assert.True(m2025[3]);
            Assert.True(m2025[4]);
            Assert.Equal(104, m2025.Count(x => x));

            bool[] m2023 = KlimakalenderGemeinsam.WochenendmaskeBilden(2023);   // 1.1. Sonntag
            Assert.True(m2023[0]);
            Assert.True(KlimakalenderGemeinsam.Abweichungen(m2025, m2023) > 0);
            Assert.Equal(0, KlimakalenderGemeinsam.Abweichungen(m2025, KlimakalenderGemeinsam.WochenendmaskeBilden(2025)));
        }


        private static void Nahe(double erwartet, double ist, double relativ)
        {
            double abw = Math.Abs(ist - erwartet);
            Assert.True(abw <= relativ * Math.Abs(erwartet),
                string.Format(CultureInfo.InvariantCulture, "erwartet {0:G9}, ist {1:G9} (relativ {2:G3})",
                              erwartet, ist, abw / Math.Abs(erwartet)));
        }
    }

    /// <summary>Stufe G1 — der Namensleser der vierzehn übrigen Gebäudespalten (NULL-erhaltend).</summary>
    [Collection("Testdatenbank")]
    public class GebaeudeVdi6007EingangDatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public GebaeudeVdi6007EingangDatenbankTests(TestDatenbank db) { _db = db; }

        private static ProjektGebaeudeModel Zeile(int idProjekt, int nummer, string modell)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[nummer];
            item.Gebaeude_Modell = modell;
            return item;
        }

        [Fact]
        public void Der_Namensleser_haelt_die_neuen_Spalten_NULL()
        {
            if (!_db.Vorhanden) return;
            ProjektGebaeudeModel item = Zeile(1045, 0, null);
            Assert.Null(item.Fensterflaeche_Ost);
            Assert.Null(item.Fensterflaeche_West);
            Assert.Null(item.Rahmenanteil);
            Assert.Null(item.Verschattungsfaktor);
            Assert.Null(item.Grundflaeche_Randbedingung);
            Assert.Null(item.Kellertemperatur);
            Assert.Null(item.Masseanteil_Aussen);
            Assert.Null(item.Innenflaechenfaktor);
            Assert.Null(item.Heizung_Strahlungsanteil);
            Assert.Null(item.Heizleistung_Max);
            Assert.Null(item.Luftwechsel_Infiltration);
            Assert.Null(item.Luftwechsel_Nutzer);
            Assert.False(item.Aussenbauteile_Strahlung);
            Assert.False(item.Sommerlueftung);
        }

        /// <summary>
        /// Jedes Gebäude der Referenzprojekte, im Speicher auf <c>VDI6007</c> gestellt: plausible
    }
}
