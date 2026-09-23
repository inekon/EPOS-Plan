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
    /// <b>Stufe G1 — der VDI-6007-Weg ohne Datenbank</b> (Umsetzungskonzept 1.9, Rechenschritte
    /// 10.4): Rechenweg mit synthetischem Gebäude (stationärer Grenzfall, Energiebilanz des
    /// Jahres, Determinismus, Vorlauf, Rechenzeit), Kennzahlen und Skalierung, Fehlerweg und
    /// Weiche. Gebäude und Klimareihe: <see cref="Vdi6007Probe"/>.
    /// </summary>
    public class GebaeudeVdi6007Tests
    {
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeVdi6007Tests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        // =====================================================================
        //  Schritte F und G — Rechenweg
        // =====================================================================

        /// <summary>
        /// Stationärer Grenzfall (Rechenschritte 10.4): konstante Außentemperatur, keine Sonne,
        /// keine inneren Lasten, fester Sollwert, Grundfläche an Außenluft, konvektive Heizung —
        /// Φ_h = [1/(R_Rest + R_1 + R_innen,eff) + H_ext]·(θ_soll − θ_out) mit den Widerständen der
        /// zusammengefassten Außenbauteilgruppe.
        /// </summary>
        [Fact]
        public void Der_stationaere_Grenzfall_trifft_den_wirksamen_Leitwert()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            g.Interne_Waermegewinne = 0.0;
            g.Raumsolltemperatur_Nachtabsenkung = 20.0;
            g.Heizung_Strahlungsanteil = 0.0;
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(h => -5.0, mitSonne: false));

            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
            ErsatzparameterRC p = e.Parameter;
            double rInnen = p.R_conv_AW_KW * (p.R_conv_IW_KW + p.R_rad_KW) / (p.R_conv_AW_KW + p.R_conv_IW_KW + p.R_rad_KW);
            double leitwert = 1.0 / (p.R_Rest_AWGruppe_KW + p.R_1_AWGruppe_KW + rInnen) + 1.0 / p.R_ext_KW;
            double erwartet = leitwert * 25.0;

            Nahe(erwartet, r.HeizlastW[4000], 1e-9);
            Nahe(erwartet, r.HeizlastW[8759], 1e-9);
            Assert.Equal(0.0, r.KuehlenergieMwh);
            _ausgabe.WriteLine("Stationär: wirksamer Leitwert {0:F2} W/K, Φ_h = {1:F1} W", leitwert, erwartet);
        }

        /// <summary>
        /// Energiebilanz des Jahres: Heizen − Kühlen + Lasten = Abfluss über R_Rest an θ_eq und
        /// über R_ext an θ_out + Speicheränderung der beiden Massen — aus den Blockmitteln exakt.
        /// </summary>
        [Fact]
        public void Die_Energiebilanz_des_Jahres_ist_geschlossen()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));
            ErsatzparameterRC p = e.Parameter;
            var m = new Zonenmodell2K(p, "Bilanz");
            m.Zuruecksetzen(20.0);
            for (int h = 8760 - Vdi6007Rechenweg.VORLAUF_H; h < 8760; h++) { Stundenrand r0 = e.Rand(h); m.Schritt(in r0); }

            double aw0 = m.ThetaMAw, iw0 = m.ThetaMIw;
            double zu = 0.0, ab = 0.0, kuehl = 0.0, heiz = 0.0;
            double gRest = 1.0 / p.R_Rest_AWGruppe_KW, gExt = 1.0 / p.R_ext_KW;
            for (int h = 0; h < 8760; h++)
            {
                Stundenrand r = e.Rand(h);
                Stundenergebnis s = m.Schritt(in r);
                heiz += s.HeizleistungW;
                kuehl += s.KuehlleistungW;
                zu += s.HeizleistungW - s.KuehlleistungW + r.PhiRadAW + r.PhiRadIW + r.PhiConv;
                ab += gRest * (s.ThetaMAwMittel - r.ThetaEq) + gExt * (s.ThetaAirMittel - r.ThetaOut);
            }
            double speicher = (p.C_AW_Jk * (m.ThetaMAw - aw0) + p.C_IW_Jk * (m.ThetaMIw - iw0)) / 3600.0;

            Assert.True(heiz > 0.0 && kuehl > 0.0, "Die Probe soll heizen und kühlen.");
            Assert.True(Math.Abs(zu - ab - speicher) <= 1e-7 * Math.Abs(zu) + 1e-3,
                "Bilanz offen: zu " + zu + " Wh, ab " + ab + " Wh, Speicher " + speicher + " Wh");
        }

        [Fact]
        public void Zwei_Laeufe_sind_byte_gleich_und_der_Vorlauf_schwingt_ein()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang e = Vdi6007Probe.Eingang(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang));

            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(e, 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(e, 0, 1);
            Assert.Equal(a.HeizlastW, b.HeizlastW);
            Assert.Equal(a.Raumtemperatur, b.Raumtemperatur);
            Assert.Equal(a.KuehlbedarfKwh, b.KuehlbedarfKwh);

            // Der Vorlauf von 30 Tagen: zwei Startwerte 10 K auseinander enden unter 0,1 K.
            double[] ende = new double[2];
            for (int k = 0; k < 2; k++)
            {
                var m = new Zonenmodell2K(e.Parameter, "Vorlauf");
                m.Zuruecksetzen(k == 0 ? 20.0 : 10.0);
                for (int h = 8760 - Vdi6007Rechenweg.VORLAUF_H; h < 8760; h++) { Stundenrand r = e.Rand(h); m.Schritt(in r); }
                ende[k] = m.ThetaMAw;
            }
            Assert.True(Math.Abs(ende[0] - ende[1]) < 0.1, "Vorlauf nicht eingeschwungen: " + ende[0] + " / " + ende[1]);

            // Rechenzeit je Gebäude und Jahr (Planungsgröße Konzept 5.13: 10 ms).
            var uhr = Stopwatch.StartNew();
            const int n = 5;
            for (int i = 0; i < n; i++) Vdi6007Rechenweg.Laufen(e, 0, 1);
            uhr.Stop();
            _ausgabe.WriteLine("Rechenzeit je Gebäude und Jahr (mit Vorlauf): {0:F2} ms", uhr.Elapsed.TotalMilliseconds / n);
        }

        [Fact]
        public void Die_Kennzahlen_folgen_8_2_und_die_Skalierung_multipliziert_nach()
        {
            GebaeudeModellErgebnis r = Vdi6007Rechenweg.Laufen(
                Vdi6007Probe.Eingang(Vdi6007Probe.Gebaeude(), Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang)), 3, 4711);

            Assert.Equal(r.HeizlastW.Sum() / 1e6, r.JahresheizwaermeMwh, 12);
            Assert.Equal(r.VerbrauchAltKwh / 1000.0, r.JahresheizwaermeMwh, 9);
            Assert.Equal(r.HeizlastW.Max() / 1000.0, r.SpitzeKw, 12);
            Assert.True(r.SpitzeTagesmittelKw <= r.SpitzeKw && r.Spitze95Kw <= r.SpitzeKw);
            Assert.True(r.StundenMitKuehlbedarf > 0);
            Assert.InRange(r.MittlereRaumtemperaturHeizzeit, 19.5, 24.0);

            GebaeudeModellErgebnis s = r.Skaliert(2.5);
            Assert.Equal(2.5 * r.JahresheizwaermeMwh, s.JahresheizwaermeMwh, 9);
            Assert.Equal(2.5 * r.SpitzeKw, s.SpitzeKw, 9);
            Assert.Equal(2.5 * r.KuehlenergieMwh, s.KuehlenergieMwh, 9);
            Assert.Equal(r.VerbrauchAltKwh, s.VerbrauchAltKwh);
            Assert.Equal(r.MittlereRaumtemperaturHeizzeit, s.MittlereRaumtemperaturHeizzeit);
            Assert.Equal(r.Ueberhitzungsstunden, s.Ueberhitzungsstunden);
            Assert.Equal(2.5, s.Skalierungsfaktor);
        }

        [Fact]
        public void Der_Rechenweg_legt_das_Ergebnis_ab_und_meldet_Fehler_ohne_Ausnahme()
        {
            var traeger = new GebaeudeErgebnistraeger();
            var weg = new Vdi6007Rechenweg(traeger);
            SolardatenModel[] k = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            var ziel = new double[8760];

            Assert.True(weg.Rechnen(Vdi6007Probe.Gebaeude(), 2, ziel, Vdi6007Probe.Kalender(k), out double verbrauchAlt));
            Assert.Equal(ziel.Sum() / 1000.0, verbrauchAlt, 6);
            Assert.NotNull(traeger.Ergebnis(2));
            Assert.Null(traeger.Ergebnis(0));

            ProjektGebaeudeModel falsch = Vdi6007Probe.Gebaeude();
            falsch.Bauweise = 50.0;
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.False(weg.Rechnen(falsch, 0, ziel, Vdi6007Probe.Kalender(k), out _));
            Assert.Contains(protokoll.Fehler, f => f.Contains(nameof(GebaeudeModellFehler.BauweiseUnplausibel), StringComparison.Ordinal));
        }

        // =====================================================================
        //  Die Weiche dieser Welle
        // =====================================================================

        [Fact]
        public void Die_Weiche_fuehrt_nur_TAGESBILANZ_auf_den_Tagesbilanz_Weg()
        {
            var sim = new SimulationWaermebedarf();
            Assert.IsType<Vdi6007Rechenweg>(sim.RechenwegWaehlen(new ProjektGebaeudeModel { Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007 }));
            Assert.IsType<Vdi6007Rechenweg>(sim.RechenwegWaehlen(new ProjektGebaeudeModel { Gebaeude_Modell = null }));
            Assert.Same(sim.Tagesbilanzweg, sim.RechenwegWaehlen(new ProjektGebaeudeModel { Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ }));

            // Die NULL-Regel ist eine benannte Stelle: VDI 6007 ist das Vorgabemodell (E1).
            Assert.Equal(DbWerte.GEBAEUDE_MODELL_VDI6007, SimulationWaermebedarf.MODELL_OHNE_ANGABE);
        }

        private static void Nahe(double erwartet, double ist, double relativ)
        {
            double abw = Math.Abs(ist - erwartet);
            Assert.True(abw <= relativ * Math.Abs(erwartet),
                string.Format(CultureInfo.InvariantCulture, "erwartet {0:G9}, ist {1:G9} (relativ {2:G3})",
                              erwartet, ist, abw / Math.Abs(erwartet)));
        }
    }

    /// <summary>
    /// <b>Stufe G1 — der VDI-Weg auf der Testdatenbank.</b> Die Gebäudezeilen der
    /// Referenzprojekte werden <b>im Speicher</b> auf <c>VDI6007</c> gestellt (die Datei bleibt
    /// unberührt) und über die Fassade <c>HeizwaermeEinesGebaeudes</c> gerechnet — denselben
    /// Rumpf, den der Lauf ruft. Berichtet werden die Jahreswerte gegen den Tagesbilanz-Weg
    /// (Konzept 5.5 nennt +7 bis +33 % für den Prototyp), die Rechenzeit, die Wochenendprobe
    /// und die Messung zu U6; geprüft wird nur grobe Plausibilität.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeVdi6007DatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _ausgabe;

        public GebaeudeVdi6007DatenbankTests(TestDatenbank db, ITestOutputHelper ausgabe)
        {
            _db = db;
            _ausgabe = ausgabe;
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf NeueRechnung(int idProjekt)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(Klimaregion(idProjekt));
            return sim;
        }

        private static ProjektGebaeudeModel Zeile(int idProjekt, int nummer, string modell)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[nummer];
            item.Gebaeude_Modell = modell;
            return item;
        }

        /// <summary>
        /// Das Abnahmefenster der Katalogkennzahl (Kriterium (4), Leitkonzept 10.4): Jahres-
        /// heizwärme je m² auf dem VDI-Weg gegen <c>spez_Waermeverbrauch</c> des Katalogs.
        /// </summary>
        private const double KATALOGFENSTER_UNTEN = 0.90, KATALOGFENSTER_OBEN = 1.15;

        /// <summary>
        /// Die dreizehn Referenzprojekte der Basis. 1009 stand hier als Fall des Datenfehlers
        /// „Bauweise 50 Wh/K auf 304 m²"; seit der Korrektur vom 23.09.2026
        /// (<c>Referenzlaeufe/Skripte/gebaeude_10612_233_bauweise.py</c>) rechnet es, und die
        /// benannte Ablehnung prüft <see cref="Der_Rechenweg_legt_das_Ergebnis_ab_und_meldet_Fehler_ohne_Ausnahme"/>.
        /// Kriterium (3) gilt den Referenzgebäuden: 1009 rechnet auf einer Verbrauchsangabe, der
        /// Tagesbilanz-Weg vergrößert dort die Fläche, nicht aber die absolute Bauweise.
        /// </summary>
        private static readonly int[] Projekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046 };

        /// <summary>
        /// Jahreswerte gegenüber dem Tagesbilanz-Weg; ein Gebäude mit unplausibler Bauweise
        /// wird benannt abgelehnt.
        /// </summary>
        [Fact]
        public void Die_Referenzgebaeude_rechnen_auf_dem_VDI_Weg_plausibel()
        {
            if (!_db.Vorhanden) return;

            _ausgabe.WriteLine("Projekt;Index;Gebäude;m²;Altweg kWh;VDI kWh;VDI/Alt;VDI kWh/m²a;Katalog kWh/m²a;Spitze Alt kW;Spitze VDI kW;Kühl kWh;r Tagessummen;ms");
            foreach (int projekt in Projekte)
            {
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(projekt);
                for (int i = 0; i < ctrl.rows; i++)
                {
                    SimulationWaermebedarf alt = NeueRechnung(projekt);
                    var wAlt = new double[8760];
                    Assert.True(alt.HeizwaermeEinesGebaeudes(Zeile(projekt, i, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ), i, wAlt));

                    SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
                    SimulationWaermebedarf vdi = NeueRechnung(projekt);
                    ProjektGebaeudeModel item = Zeile(projekt, i, DbWerte.GEBAEUDE_MODELL_VDI6007);
                    var wVdi = new double[8760];
                    var uhr = Stopwatch.StartNew();
                    bool ok = vdi.HeizwaermeEinesGebaeudes(item, i, wVdi);
                    uhr.Stop();

                    if (item.Bauweise / item.Nutzflaeche < 5.0)
                    {
                        Assert.False(ok);
                        Assert.Contains(protokoll.Fehler, f => f.Contains(nameof(GebaeudeModellFehler.BauweiseUnplausibel), StringComparison.Ordinal));
                        _ausgabe.WriteLine("{0};{1};{2};benannt abgelehnt: BauweiseUnplausibel", projekt, i, item.Gebaeudename);
                        continue;
                    }

                    Assert.True(ok, string.Join(" | ", protokoll.Fehler));
                    double sAlt = wAlt.Sum() / 1000.0, sVdi = wVdi.Sum() / 1000.0;
                    Assert.True(sVdi > 0.0);
                    Assert.InRange(sVdi / sAlt, 0.5, 2.5);

                    GebaeudeModellErgebnis r = vdi.GebaeudeErgebnisse.Ergebnis(i);
                    Assert.NotNull(r);
                    Assert.Equal(sVdi / 1000.0, r.JahresheizwaermeMwh, 6);

                    // Abnahmekriterium (3), Leitkonzept 10.4: Tagessummen-Korrelation zum
                    // Tagesbilanz-Weg r >= 0,98.
                    double rTag = Korrelation(Tagessummen(wAlt), Tagessummen(wVdi));
                    Assert.True(rTag >= 0.98, projekt + "/" + i + ": r = " + rTag.ToString(CultureInfo.InvariantCulture) +
                                ", VDI/Alt = " + (sVdi / sAlt).ToString(CultureInfo.InvariantCulture));

                    // Abnahmekriterium (4), Leitkonzept 10.4, mit dem Auslieferungsweg neu
                    // bestimmt (Schlusswelle G1 + G2): gemessen 95,8 % bis 109,4 % der
                    // Katalogkennzahl, das Fenster ist 90 bis 115 %. Ohne Katalogwert keine Probe.
                    if (item.spez_Waermeverbrauch > 0)
                        Assert.InRange(sVdi / item.Z_AuswahlWohnflaeche / item.spez_Waermeverbrauch,
                                       KATALOGFENSTER_UNTEN, KATALOGFENSTER_OBEN);

                    _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0};{1};{2};{3:F0};{4:F0};{5:F0};{6:F3};{7:F1};{8:F0};{9:F2};{10:F2};{11:F0};{12:F3};{13:F1}",
                        projekt, i, item.Gebaeudename, item.Z_AuswahlWohnflaeche, sAlt, sVdi, sVdi / sAlt,
                        sVdi / item.Z_AuswahlWohnflaeche, item.spez_Waermeverbrauch, wAlt.Max() / 1000.0, r.SpitzeKw,
                        r.KuehlenergieMwh * 1000.0, Korrelation(Tagessummen(wAlt), Tagessummen(wVdi)),
                        uhr.Elapsed.TotalMilliseconds));
                }
            }
        }

        /// <summary>
        /// Skalierung nach E8 über die Fassade: Flächenangabe multipliziert mit Fläche/Nutzfläche,
        /// Verbrauchsangabe trifft den Verbrauch, der Verbrauchsfall mit Heizbedarf null wird
        /// benannt abgelehnt.
        /// </summary>
        [Fact]
        public void Die_Fassade_skaliert_den_einen_Lauf_nach_E8()
        {
            if (!_db.Vorhanden) return;

            SimulationWaermebedarf sim = NeueRechnung(1045);
            ProjektGebaeudeModel basis = Zeile(1045, 0, DbWerte.GEBAEUDE_MODELL_VDI6007);
            basis.Z_AuswahlWohnflaeche = basis.Nutzflaeche;
            var w1 = new double[8760];
            Assert.True(sim.HeizwaermeEinesGebaeudes(basis, 0, w1));

            ProjektGebaeudeModel doppelt = Zeile(1045, 0, DbWerte.GEBAEUDE_MODELL_VDI6007);
            doppelt.Z_AuswahlWohnflaeche = 2.0 * doppelt.Nutzflaeche;
            var w2 = new double[8760];
            Assert.True(sim.HeizwaermeEinesGebaeudes(doppelt, 0, w2));
            for (int h = 0; h < 8760; h += 13) Assert.Equal(2.0 * w1[h], w2[h], 9);
            Assert.Equal(2.0 * doppelt.Nutzflaeche / doppelt.Flaeche_Nutzer, doppelt.Bewohner, 9);
            Assert.Equal(2.0, sim.GebaeudeErgebnisse.Ergebnis(0).Skalierungsfaktor, 12);

            ProjektGebaeudeModel verbrauch = Zeile(1045, 0, DbWerte.GEBAEUDE_MODELL_VDI6007);
            verbrauch.Einheit = "Verbrauch  [MWh/a]";
            verbrauch.Z_AuswahlWohnflaeche = 20.0;
            var w3 = new double[8760];
            Assert.True(sim.HeizwaermeEinesGebaeudes(verbrauch, 0, w3));
            Assert.Equal(20000.0, w3.Sum() / 1000.0, 6);
            Assert.Equal(20.0, sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh, 9);

            // Kein Heizbedarf im Kataloglauf ⇒ die Rückrechnung wird benannt abgelehnt.
            ProjektGebaeudeModel warm = Zeile(1045, 0, DbWerte.GEBAEUDE_MODELL_VDI6007);
            warm.Einheit = "Verbrauch  [MWh/a]";
            warm.Z_AuswahlWohnflaeche = 20.0;
            warm.Raumsolltemperatur_Tag = -30.0;
            warm.Raumsolltemperatur_Nachtabsenkung = -30.0;
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            Assert.False(sim.HeizwaermeEinesGebaeudes(warm, 0, new double[8760]));
            Assert.Contains(protokoll.Fehler, f => f.Contains(nameof(GebaeudeModellFehler.VerbrauchAltNull), StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Messung zu U6 (E27): beide Zeitbezüge an der einen Stelle, Wirkung auf Ost, West
        /// und die Jahresheizwärme. Berichtend; geprüft wird nur, dass beide rechnen.
        /// </summary>
        [Fact]
        public void U6_Stundenanfang_und_Stundenmitte_gemessen()
        {
            if (!_db.Vorhanden) return;

            foreach (int projekt in new[] { 1045, 1017, 1023 })
            {
                SimulationWaermebedarf sim = NeueRechnung(projekt);
                KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
                ProjektGebaeudeModel g = Zeile(projekt, 0, DbWerte.GEBAEUDE_MODELL_VDI6007);

                GebaeudeModellEingang a = GebaeudeModellEingang.Bauen(g, k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad, Zeitbezug.Stundenanfang);
                GebaeudeModellEingang m = GebaeudeModellEingang.Bauen(g, k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad, Zeitbezug.Stundenmitte);
                double qa = Vdi6007Rechenweg.Laufen(a, 0, g.ID_Gebaeude).VerbrauchAltKwh;
                double qm = Vdi6007Rechenweg.Laufen(m, 0, g.ID_Gebaeude).VerbrauchAltKwh;

                _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "U6 {0}: Ost {1:F1} → {2:F1} kWh/m²a ({3:+0.0;-0.0} %), West {4:F1} → {5:F1} kWh/m²a ({6:+0.0;-0.0} %), " +
                    "Süd {7:+0.0;-0.0} %, Fenstersolar {8:+0.0;-0.0} %, Heizwärme Katalogbau {9:F0} → {10:F0} kWh ({11:+0.00;-0.00} %); " +
                    "Wochenendprobe Referenzjahr {12}: {13} Tage verschieden",
                    projekt, a.Strahlung.Ost.Sum() / 1000, m.Strahlung.Ost.Sum() / 1000, Prozent(a.Strahlung.Ost, m.Strahlung.Ost),
                    a.Strahlung.West.Sum() / 1000, m.Strahlung.West.Sum() / 1000, Prozent(a.Strahlung.West, m.Strahlung.West),
                    Prozent(a.Strahlung.Sued, m.Strahlung.Sued), Prozent(a.PhiSolar, m.PhiSolar),
                    qa, qm, 100.0 * (qm / qa - 1.0), k.Referenzjahr, k.WochenendProbeAbweichungen));
                Assert.True(qa > 0.0 && qm > 0.0);
            }
        }

        private static double Prozent(double[] a, double[] m) => 100.0 * (m.Sum() / a.Sum() - 1.0);

        private static double[] Tagessummen(double[] w)
        {
            var t = new double[365];
            for (int h = 0; h < 8760; h++) t[h / 24] += w[h];
            return t;
        }

        private static double Korrelation(double[] x, double[] y)
        {
            double mx = x.Average(), my = y.Average(), sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < x.Length; i++)
            {
                sxy += (x[i] - mx) * (y[i] - my);
                sxx += (x[i] - mx) * (x[i] - mx);
                syy += (y[i] - my) * (y[i] - my);
            }
            return sxy / Math.Sqrt(sxx * syy);
        }
    }
}
