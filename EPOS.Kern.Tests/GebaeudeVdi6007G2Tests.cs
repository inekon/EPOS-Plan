using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G2 — Lüftung, Sommerlüftung und langwelliger Austausch ohne Datenbank</b>
    /// (Rechenschritte A7, E5, 7.2; Konzept 4.4): die Regel des wirksamen Luftwechsels, die
    /// Sommerlüftungsregel mit Hysterese, der langwellige Term nach VDI 6007 Blatt 1
    /// Gl. (33)–(37) mit Blatt 3 Gl. (89) samt NULL-Regel, die geschlossene Energiebilanz mit
    /// Sommerlüftung und die Kernseite des Ergebnisexports.
    /// </summary>
    public class GebaeudeVdi6007G2Tests
    {
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeVdi6007G2Tests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        // =====================================================================
        //  Infiltration und Nutzerlüftung
        // =====================================================================

        [Fact]
        public void Der_wirksame_Luftwechsel_folgt_der_Rueckfallkette()
        {
            Assert.Equal(0.7, Gebaeudemodellvorgaben.WirksamerLuftwechsel(0.7, null, null, out Luftwechselherkunft h1), 12);
            Assert.Equal(Luftwechselherkunft.Luftwechselrate, h1);

            Assert.Equal(0.7, Gebaeudemodellvorgaben.WirksamerLuftwechsel(0.0, null, null, out Luftwechselherkunft h2), 12);
            Assert.Equal(Luftwechselherkunft.Vorgabe, h2);

            Assert.Equal(0.9, Gebaeudemodellvorgaben.WirksamerLuftwechsel(1.5, 0.5, null, out Luftwechselherkunft h3), 12);
            Assert.Equal(Luftwechselherkunft.InfiltrationUndNutzer, h3);

            Assert.Equal(0.3 + 1.2, Gebaeudemodellvorgaben.WirksamerLuftwechsel(0.7, null, 1.2), 12);
            Assert.Equal(0.2, Gebaeudemodellvorgaben.WirksamerLuftwechsel(0.7, 0.2, 0.0), 12);
        }

        [Fact]
        public void Infiltration_und_Nutzerlueftung_bestimmen_den_Lueftungszweig()
        {
            // 0,3 + 0,4 = 0,7 = Luftwechselrate der Probe: derselbe Lauf, bitgleich.
            ProjektGebaeudeModel ohne = Vdi6007Probe.Gebaeude();
            ProjektGebaeudeModel mit = Vdi6007Probe.Gebaeude();
            mit.Luftwechsel_Infiltration = 0.3;
            mit.Luftwechsel_Nutzer = 0.4;
            SolardatenModel[] klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(ohne, klima), 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(Vdi6007Probe.Eingang(mit, klima), 0, 1);
            Assert.Equal(a.HeizlastW, b.HeizlastW);

            // Stationärer Grenzfall mit 1,0 1/h: der Lüftungszweig trägt n·A_f·H·c·ρ + Σψ·L.
            ProjektGebaeudeModel g = Stationaer();
            g.Luftwechsel_Infiltration = 0.5;
            g.Luftwechsel_Nutzer = 0.5;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(h => -5.0, mitSonne: false));
            Assert.Equal(1.0, e.Luftwechselrate_h, 12);
            Assert.Equal(Luftwechselherkunft.InfiltrationUndNutzer, e.LuftwechselHerkunft);
            double hVe = 1.0 * g.Nutzflaeche * g.Raumhoehe * 0.34;
            Assert.Equal(1.0 / (hVe + e.SummePsiL_WK), e.Parameter.R_ext_KW, 12);

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            ErsatzparameterRC p = e.Parameter;
            double rInnen = p.R_conv_AW_KW * (p.R_conv_IW_KW + p.R_rad_KW) / (p.R_conv_AW_KW + p.R_conv_IW_KW + p.R_rad_KW);
            double leitwert = 1.0 / (p.R_Rest_AWGruppe_KW + p.R_1_AWGruppe_KW + rInnen) + 1.0 / p.R_ext_KW;
            Assert.Equal(leitwert * 25.0, r.HeizlastW[5000], 6);
        }

        // =====================================================================
        //  Sommerlüftung
        // =====================================================================

        [Fact]
        public void Die_Sommerlueftungsregel_schaltet_mit_Hysterese_nach_der_Vorstunde()
        {
            var r = new Sommerlueftungsregel();
            Assert.False(r.Stunde(double.NaN, 10.0));            // keine Vorstunde
            Assert.False(r.Stunde(23.0, 15.0));                  // nicht über 23 °C
            Assert.False(r.Stunde(25.0, 23.0));                  // Außenluft nur 2 K kühler, nicht mehr
            Assert.True(r.Stunde(25.0, 22.9));                   // ein
            Assert.True(r.Stunde(22.5, 20.0));                   // unter 23, aber nicht unter 22: bleibt
            Assert.True(r.Stunde(24.0, 22.5));                   // Abstand 1,5 K: bleibt (Hysterese)
            Assert.False(r.Stunde(24.0, 23.1));                  // Abstand unter 1 K: aus
            Assert.True(r.Stunde(24.0, 21.0));                   // wieder ein
            Assert.False(r.Stunde(21.9, 15.0));                  // unter 22 °C: aus
            r.Zuruecksetzen();
            Assert.False(r.Aktiv);
        }

        [Fact]
        public void Sommerlueftung_senkt_Kuehlbedarf_und_Ueberhitzung_und_haelt_die_Bilanz()
        {
            SolardatenModel[] klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            ProjektGebaeudeModel aus = Vdi6007Probe.Gebaeude();
            ProjektGebaeudeModel ein = Vdi6007Probe.Gebaeude();
            ein.Sommerlueftung = true;

            GebaeudeModellEingang eAus = Vdi6007Probe.Eingang(aus, klima);
            GebaeudeModellEingang eEin = Vdi6007Probe.Eingang(ein, klima);
            Assert.Equal(0.0, eAus.SommerlueftungZusatzleitwertWK);
            Assert.Equal((2.0 - 0.7) * 201.0 * 2.75 * 0.34, eEin.SommerlueftungZusatzleitwertWK, 9);

            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(eAus, 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(eEin, 0, 1);
            Assert.Equal(0, a.StundenMitSommerlueftung);
            Assert.True(b.StundenMitSommerlueftung > 0);
            Assert.True(b.KuehlenergieMwh < a.KuehlenergieMwh);
            Assert.True(b.Ueberhitzungsstunden <= a.Ueberhitzungsstunden);
            Assert.InRange(b.JahresheizwaermeMwh / a.JahresheizwaermeMwh, 0.98, 1.05);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Sommerlüftung: {0} h; Kühlenergie {1:F3} → {2:F3} MWh; Überhitzung {3} → {4} h; Heizwärme {5:F3} → {6:F3} MWh",
                b.StundenMitSommerlueftung, a.KuehlenergieMwh, b.KuehlenergieMwh, a.Ueberhitzungsstunden,
                b.Ueberhitzungsstunden, a.JahresheizwaermeMwh, b.JahresheizwaermeMwh));

            // Energiebilanz mit wechselndem Lüftungszustand (Zusatzleitwert im Abfluss).
            ErsatzparameterRC p = eEin.Parameter;
            var m = new Zonenmodell2K(p, "Bilanz G2");
            var regel = new Sommerlueftungsregel();
            m.Zuruecksetzen(20.0);
            double luftVor = double.NaN, aussenVor = double.NaN;
            for (int h = 8760 - Vdi6007Rechenweg.VORLAUF_H; h < 8760; h++)
            {
                Stundenrand r0 = eEin.Rand(h, regel.Stunde(luftVor, aussenVor));
                Stundenergebnis v = m.Schritt(in r0);
                luftVor = v.ThetaAirMittel; aussenVor = eEin.ThetaOut[h];
            }
            double aw0 = m.ThetaMAw, iw0 = m.ThetaMIw, zu = 0.0, ab = 0.0;
            double gRest = 1.0 / p.R_Rest_AWGruppe_KW, gExt = 1.0 / p.R_ext_KW;
            for (int h = 0; h < 8760; h++)
            {
                Stundenrand r = eEin.Rand(h, regel.Stunde(luftVor, aussenVor));
                Stundenergebnis s = m.Schritt(in r);
                zu += s.HeizleistungW - s.KuehlleistungW + r.PhiRadAW + r.PhiRadIW + r.PhiConv;
                ab += gRest * (s.ThetaMAwMittel - r.ThetaEq) + (gExt + r.ZusatzleitwertWK) * (s.ThetaAirMittel - r.ThetaOut);
                luftVor = s.ThetaAirMittel; aussenVor = eEin.ThetaOut[h];
            }
            double speicher = (p.C_AW_Jk * (m.ThetaMAw - aw0) + p.C_IW_Jk * (m.ThetaMIw - iw0)) / 3600.0;
            Assert.True(Math.Abs(zu - ab - speicher) <= 1e-7 * Math.Abs(zu) + 1e-3,
                "Bilanz offen: zu " + zu + " Wh, ab " + ab + " Wh, Speicher " + speicher + " Wh");
        }

        [Fact]
        public void Ohne_Schalter_rechnet_der_Weg_wie_in_G1()
        {
            // Der Rand ohne Sommerlüftung trägt keinen Zusatzleitwert.
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            for (int h = 0; h < 8760; h += 97) Assert.Equal(0.0, e.Rand(h).ZusatzleitwertWK);
            Assert.False(e.Sommerlueftung);
        }

        // =====================================================================
        //  Langwelliger Austausch (Blatt 1 Gl. (33)–(37), Blatt 3 Gl. (89))
        // =====================================================================

        [Fact]
        public void Ohne_Gegenstrahlung_gilt_die_NULL_Regel()
        {
            Assert.Equal(0.0, GebaeudeKlimaweg.DeltaThetaLangwellig(double.NaN, 5.0, 0.5));
            Assert.Equal(0.0, GebaeudeKlimaweg.DeltaThetaLangwellig(0.0, 5.0, 1.0));
            Assert.Equal(5.0, GebaeudeKlimaweg.AlphaStrAussen(double.NaN, 5.0));
            Assert.Equal(25.0, GebaeudeKlimaweg.AlphaAussen(double.NaN, 5.0));
            Assert.Equal(double.NaN, GebaeudeKlimaweg.Gegenstrahlung(new SolardatenModel()));
        }

        [Fact]
        public void Der_langwellige_Term_folgt_den_Gleichungen_33_bis_37()
        {
            const double theta = 10.0;
            double t4 = Math.Pow(273.15 + theta, 4);

            // (34)/(35) kehren E = 0,93·σ·T⁴ um.
            Assert.Equal(theta, GebaeudeKlimaweg.TemperaturAtmosphaere(0.93 * 5.67e-8 * t4), 9);
            Assert.Equal(theta, GebaeudeKlimaweg.TemperaturErde(-0.93 * 5.67e-8 * t4), 9);

            // (89): Ausstrahlung der Erde mit reflektierter Gegenstrahlung, DWD-Vorzeichen.
            double eA = 280.0;
            double eE = GebaeudeKlimaweg.AusstrahlungErde(eA, theta);
            Assert.Equal(-0.93 * 5.671e-8 * t4 + 0.07 * eA, eE, 9);

            // (37): der Strahlungsübergang liegt in der physikalischen Größenordnung 4…6 W/(m²K).
            double alpha = GebaeudeKlimaweg.AlphaStrAussen(eA, theta);
            Assert.InRange(alpha, 3.5, 6.0);

            // Klarer Himmel (Gegenstrahlung weit unter der Ausstrahlung): Δθ_lw < 0, und das Dach
            // (φ = 1) kühlt stärker aus als die Wand (φ = 0,5).
            double wand = GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 0.5);
            double dach = GebaeudeKlimaweg.DeltaThetaLangwellig(eA, theta, 1.0);
            Assert.True(wand < 0.0 && dach < wand, "Wand " + wand + " K, Dach " + dach + " K");

            // (33) ausgeschrieben.
            double thetaAtm = GebaeudeKlimaweg.TemperaturAtmosphaere(eA);
            double thetaErd = GebaeudeKlimaweg.TemperaturErde(eE);
            double erwartet = ((thetaErd - theta) * 0.5 + (thetaAtm - theta) * 0.5) * 0.9 * alpha / ((20.0 + alpha) * 0.93);
            Assert.Equal(erwartet, wand, 12);

            // (38): kurzwellig mit α_A aus (37).
            Assert.Equal(500.0 * 0.6 / (20.0 + alpha), GebaeudeKlimaweg.DeltaThetaKurzwellig(500.0, eA, theta), 12);
            Assert.Equal(500.0 * 0.6 / 25.0, GebaeudeKlimaweg.DeltaThetaKurzwellig(500.0, double.NaN, theta), 12);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "E_A {0} W/m², θ {1} °C: α_str {2:F2}, Δθ_lw Wand {3:F2} K, Dach {4:F2} K", eA, theta, alpha, wand, dach));
        }

        [Fact]
        public void Die_aequivalente_Aussentemperatur_nimmt_Schalter_und_Gegenstrahlung_auf()
        {
            SolardatenModel[] ohneEa = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            SolardatenModel[] mitEa = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            foreach (SolardatenModel z in mitEa) z.Gegenstrahlung = Gegenstrahlung(z.Außen_Temp);

            ProjektGebaeudeModel aus = Vdi6007Probe.Gebaeude();
            aus.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            ProjektGebaeudeModel ein = Vdi6007Probe.Gebaeude();
            ein.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            ein.Aussenbauteile_Strahlung = true;

            // Schalter aus, keine Gegenstrahlung: θ_eq = θ_out (Grund an Außenluft) — wie in G1.
            GebaeudeModellEingang g1 = Vdi6007Probe.Eingang(aus, ohneEa);
            for (int h = 0; h < 8760; h += 101) Assert.Equal(g1.ThetaOut[h], g1.ThetaEq[h], 9);

            // Schalter aus, mit Gegenstrahlung: nur der Fensterterm (39) senkt θ_eq.
            GebaeudeModellEingang fenster = Vdi6007Probe.Eingang(aus, mitEa);
            Assert.True(Enumerable.Range(0, 8760).All(h => fenster.ThetaEq[h] <= fenster.ThetaOut[h] + 1e-12));

            // Schalter ein, ohne Gegenstrahlung: nachts θ_eq = θ_out, mittags darüber (kurzwellig).
            GebaeudeModellEingang kw = Vdi6007Probe.Eingang(ein, ohneEa);
            Assert.Equal(kw.ThetaOut[2], kw.ThetaEq[2], 9);
            Assert.True(kw.ThetaEq[12] > kw.ThetaOut[12] + 0.5);

            // Schalter ein, mit Gegenstrahlung: nachts unter θ_out (langwellige Abstrahlung).
            GebaeudeModellEingang beide = Vdi6007Probe.Eingang(ein, mitEa);
            Assert.True(beide.ThetaEq[2] < beide.ThetaOut[2] - 0.2);

            double qAus = Vdi6007Rechenweg.Laufen(g1, 0, 1).JahresheizwaermeMwh;
            double qKw = Vdi6007Rechenweg.Laufen(kw, 0, 1).JahresheizwaermeMwh;
            double qBeide = Vdi6007Rechenweg.Laufen(beide, 0, 1).JahresheizwaermeMwh;
            Assert.True(qKw < qAus && qBeide > qKw);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Heizwärme Probe: aus {0:F3}, kurzwellig {1:F3} ({2:+0.0;-0.0} %), kurz- und langwellig {3:F3} ({4:+0.0;-0.0} %) MWh",
                qAus, qKw, 100.0 * (qKw / qAus - 1.0), qBeide, 100.0 * (qBeide / qAus - 1.0)));
        }

        /// <summary>
        /// Eine Gegenstrahlung für die Probe — Klarhimmel nach Swinbank (1963), 5,31·10⁻¹³·T⁶;
        /// eine Prüfgröße, kein Rechenweg des Produkts (das schätzt nicht, NULL-Regel E5).
        /// </summary>
        internal static double Gegenstrahlung(double thetaAussen) => 5.31e-13 * Math.Pow(273.15 + thetaAussen, 6);

        // =====================================================================
        //  Ergebnisexport (Kernseite)
        // =====================================================================

        [Fact]
        public void Der_Exportsatz_nennt_drei_Reihen_und_neun_Zahlenskalare()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 2, 4711);
            GebaeudeExportsatz s = GebaeudeErgebnisexport.Satz(r);

            Assert.Equal(new[] { "raumtemperatur_2.csv", "operative_temperatur_2.csv", "kuehlbedarf_2.csv" },
                         s.Reihen.Select(p => p.Key).ToArray());
            Assert.Same(r.Raumtemperatur, s.Reihen[0].Value);
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, s.Modell);
            Assert.Equal(new[]
            {
                "Geb[2].ID_Gebaeude", "Geb[2].JahresheizwaermeMwh", "Geb[2].SpitzeKw", "Geb[2].SpitzeTagesmittelKw",
                "Geb[2].Spitze95Kw", "Geb[2].KuehlenergieMwh", "Geb[2].StundenMitKuehlbedarf",
                "Geb[2].MittlereRaumtemperaturHeizzeit", "Geb[2].Ueberhitzungsstunden"
            }, s.Skalare.Select(p => p.Key).ToArray());
            Assert.Equal(4711.0, s.Skalare[0].Value);
            Assert.Equal(r.JahresheizwaermeMwh, s.Skalare[1].Value);

            // Ohne VDI-Gebäude: kein Satz (der Bestandsordner bleibt byte-gleich).
            Assert.Empty(GebaeudeErgebnisexport.Saetze(new SimulationWaermebedarf()));
        }

        [Fact]
        public void Das_Ergebnis_fuehrt_den_Heizsollwert_fuer_das_Sollwertband()
        {
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.Equal(e.ThetaSoll, r.Heizsollwert);
            Assert.NotSame(e.ThetaSoll, r.Heizsollwert);
            Assert.Same(r.Heizsollwert, r.Skaliert(2.0).Heizsollwert);
        }

        private static ProjektGebaeudeModel Stationaer()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            g.Interne_Waermegewinne = 0.0;
            g.Raumsolltemperatur_Nachtabsenkung = 20.0;
            g.Heizung_Strahlungsanteil = 0.0;
            return g;
        }
    }

    /// <summary>
    /// <b>Stufe G2 — die Wirkung auf die Referenzgebäude</b> (berichtend): jedes Gebäude der
    /// dreizehn Referenzprojekte im Speicher auf VDI 6007 umgestellt und je Schalter einmal
    /// gerechnet. Geprüft wird die Richtung, ausgegeben die relative Wirkung auf die
    /// Jahreswerte. Die Testdatenbank führt keine Gegenstrahlung; für den langwelligen Term
    /// setzt die Messung eine Klarhimmel-Gegenstrahlung ein (Prüfgröße, kein Produktweg).
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeVdi6007G2DatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeVdi6007G2DatenbankTests(TestDatenbank db, ITestOutputHelper ausgabe)
        {
            _db = db;
            _ausgabe = ausgabe;
        }

        private static readonly int[] Projekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046 };

        [Fact]
        public void Wirkung_von_Sommerlueftung_Infiltration_und_Strahlung_auf_die_Referenzgebaeude()
        {
            if (!_db.Vorhanden) return;

            var summen = new Dictionary<string, double[]>();   // Variante → {Heiz, Kühl, Überhitzung}
            string[] varianten = { "Basis", "Sommerlüftung", "Infiltration 0,3 + Nutzer 0,4", "Nutzer 0", "Strahlung kurzwellig", "Strahlung kurz- und langwellig" };
            foreach (string v in varianten) summen[v] = new double[3];
            int gebaeude = 0;

            foreach (int projekt in Projekte)
            {
                var projektCtrl = new ProjektCtrl();
                projektCtrl.ReadSingle(projekt);
                var sim = new SimulationWaermebedarf { m_ID_Projekt = projekt };
                sim.KlimakalenderLesen(projektCtrl.m_ID_Klimaregion);
                KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
                SolardatenModel[] mitEa = k.SolarOrtszeit.Select(z => Kopie(z)).ToArray();

                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(projekt);
                for (int i = 0; i < ctrl.rows; i++)
                {
                    ProjektGebaeudeModel g = ctrl.items[i];
                    if (g.Bauweise / g.Nutzflaeche < 5.0) continue;         // 1009-Fall: benannt abgelehnt
                    g.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
                    gebaeude++;

                    foreach (string v in varianten)
                    {
                        ProjektGebaeudeModel x = Zeile(projekt, i);
                        IReadOnlyList<SolardatenModel> klima = k.SolarOrtszeit;
                        switch (v)
                        {
                            case "Sommerlüftung": x.Sommerlueftung = true; break;
                            case "Infiltration 0,3 + Nutzer 0,4": x.Luftwechsel_Infiltration = 0.3; x.Luftwechsel_Nutzer = 0.4; break;
                            case "Nutzer 0": x.Luftwechsel_Infiltration = 0.3; x.Luftwechsel_Nutzer = 0.0; break;
                            case "Strahlung kurzwellig": x.Aussenbauteile_Strahlung = true; break;
                            case "Strahlung kurz- und langwellig": x.Aussenbauteile_Strahlung = true; klima = mitEa; break;
                        }
                        GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(x, klima, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad);
                        GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, i, x.ID_Gebaeude);
                        summen[v][0] += r.JahresheizwaermeMwh;
                        summen[v][1] += r.KuehlenergieMwh;
                        summen[v][2] += r.Ueberhitzungsstunden;
                    }
                }
            }

            double[] b = summen["Basis"];
            _ausgabe.WriteLine("Gebäude: " + gebaeude);
            foreach (string v in varianten)
            {
                double[] s = summen[v];
                _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}: Heizwärme {1:F1} MWh ({2:+0.00;-0.00} %), Kühlbedarf {3:F1} MWh ({4:+0.0;-0.0} %), Überhitzungsstunden {5:F0} ({6:+0.0;-0.0} %)",
                    v, s[0], 100.0 * (s[0] / b[0] - 1.0), s[1], 100.0 * (s[1] / b[1] - 1.0), s[2], 100.0 * (s[2] / b[2] - 1.0)));
            }

            // Die Richtung: Sommerlüftung senkt die Kühlung, die Vorgabe 0,3 + 0,4 = 0,7 ist die
            // Luftwechselrate der gesäten Gebäude, weniger Luftwechsel senkt die Heizwärme.
            Assert.True(summen["Sommerlüftung"][1] < b[1]);
            Assert.Equal(b[0], summen["Infiltration 0,3 + Nutzer 0,4"][0], 9);
            Assert.True(summen["Nutzer 0"][0] < b[0]);
            Assert.True(summen["Strahlung kurzwellig"][0] < b[0]);
            Assert.True(summen["Strahlung kurz- und langwellig"][0] > summen["Strahlung kurzwellig"][0]);
        }

        private static ProjektGebaeudeModel Zeile(int projekt, int i)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(projekt);
            ProjektGebaeudeModel g = ctrl.items[i];
            g.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            return g;
        }

        private static SolardatenModel Kopie(SolardatenModel z)
        {
            return new SolardatenModel
            {
                TagUtc = z.TagUtc,
                StundeUtc = z.StundeUtc,
                Außen_Temp = z.Außen_Temp,
                Globalstrahlung = z.Globalstrahlung,
                Direktstrahlung = z.Direktstrahlung,
                Diffusstrahlung = z.Diffusstrahlung,
                Sonnenwinkel = z.Sonnenwinkel,
                Gegenstrahlung = GebaeudeVdi6007G2Tests.Gegenstrahlung(z.Außen_Temp),
            };
        }
    }
}
