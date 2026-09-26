using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using static EPOS.Kern.Tests.BauteilwegLaufProbe;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kern-Eingang der Mehrzonen-Rechnung (Stufe G6b, Welle W3)</b> — Zonenwerte auch bei
    /// einer Zone (A5 (a)), Trennflächen mit Gegenseite und Gruppe, θ_eq mit Nachbargliedern und der
    /// Zusicherung Σ B_v = 1, Luftaustausch über R_ext und die Zulufttemperatur, der Zonenlauf mit
    /// seinem Orakel gegen <see cref="Vdi6007Rechenweg.Laufen"/> und Probe 9 (Grenzfälle der
    /// Bauteilzuordnung, Mehrzonenkonzept 8.1). Ohne Datenbank.
    /// </summary>
    public class ZonenEingangTests
    {
        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static GebaeudeKlima KlimaDes(SolardatenModel[] zeilen = null)
            => new GebaeudeKlima(zeilen ?? Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        private static GebaeudeModellEingang Einzonen(ProjektGebaeudeModel g)
            => GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        // =====================================================================
        //  Das Orakel: Zonenlauf einer Einzelzone = Laufen
        // =====================================================================

        public static IEnumerable<object[]> Faelle() => GebaeudeEinzonennetzTests.FallDaten();

        /// <summary>
        /// Der Zonenlauf mit genau einer Zone ist bitgleich zu <see cref="Vdi6007Rechenweg.Laufen"/> —
        /// über denselben Eingang und über den Zonenbauer mit N = 1 erzwungen, für jeden Fall des
        /// Einzonennetzes (W0).
        /// </summary>
        [Theory]
        [MemberData(nameof(Faelle))]
        public void Der_Zonenlauf_einer_Einzelzone_ist_bitgleich_zu_Laufen(string fall)
        {
            ProjektGebaeudeModel g = GebaeudeEinzonennetzTests.Aufbau(fall, out SolardatenModel[] klima, out bool kuehl, out string stufe);
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                                                                  Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehl, stufe);
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(e, 0, 1);

            GebaeudeModellErgebnis ist = Zonenlauf.Laufen(ZonenEingang.Einzeln(e), 0, 1);
            Bitgleich(fall + " (Einzeln)", e, soll, e, ist);

            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, KlimaDes(klima), kuehl, stufe);
            ZonenEingang z = Assert.Single(zonen);
            Assert.False(z.Gekoppelt);
            Assert.Equal(1, z.Eingang.Zonenzahl);
            GebaeudeModellErgebnis ist2 = Zonenlauf.Laufen(z, 0, 1);
            Bitgleich(fall + " (Zonenbauer)", e, soll, z.Eingang, ist2);
        }

        /// <summary>Auch der Klassenweg (keine Zone) läuft durch den Zonenlauf bitgleich.</summary>
        [Fact]
        public void Der_Zonenlauf_des_Klassenwegs_ist_bitgleich_zu_Laufen()
        {
            GebaeudeModellEingang e = Einzonen(Vdi6007Probe.Gebaeude());
            Assert.Equal(0, e.Zonenzahl);
            Bitgleich("Klassenweg", e, Vdi6007Rechenweg.Laufen(e, 0, 1), e, Zonenlauf.Laufen(ZonenEingang.Einzeln(e), 0, 1));
        }

        private static void Bitgleich(string wer, GebaeudeModellEingang e1, GebaeudeModellErgebnis r1,
                                      GebaeudeModellEingang e2, GebaeudeModellErgebnis r2)
        {
            List<(string Name, double[] Werte)> a = GebaeudeEinzonennetzTests.Reihen(e1, r1);
            List<(string Name, double[] Werte)> b = GebaeudeEinzonennetzTests.Reihen(e2, r2);
            Assert.Equal(a.Select(x => x.Name), b.Select(x => x.Name));
            for (int i = 0; i < a.Count; i++)
                Assert.True(GebaeudeEinzonennetzTests.Bilden(a[i].Werte).Sha256 == GebaeudeEinzonennetzTests.Bilden(b[i].Werte).Sha256,
                            wer + ": Reihe " + a[i].Name + " weicht ab.");
        }

        // =====================================================================
        //  A5 (a): Zonenwerte auch bei einer Zone
        // =====================================================================

        [Fact]
        public void Die_Werte_der_Zone_wirken_auch_bei_genau_einer_Zone()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            var eingaben = new Zoneneingaben(SollTag: 22.0, SollNacht: 19.0, Maximaleraumtemperatur: 26.0,
                HeizungStrahlungsanteil: 0.3, HeizleistungMaxKw: 9.0, LuftwechselInfiltration: 0.3, LuftwechselNutzer: 0.2,
                InterneWaermegewinne: 600.0, Raumhoehe: 3.0);
            g.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile, double.NaN, eingaben) };
            GebaeudeModellEingang e = Einzonen(g);

            Assert.Equal(22.0, e.SollTag);
            Assert.Equal(22.0, e.ThetaSoll.Max());
            Assert.Equal(19.0, e.ThetaSoll.Min());
            Assert.Equal(26.0, e.ThetaMaxWert);
            Assert.Equal(0.3, e.HeizungStrahlungsanteil);
            Assert.Equal(9000.0, e.HeizleistungMaxW);
            Assert.Equal(Gebaeudemodellvorgaben.WirksamerLuftwechsel(g.Luftwechselrate, 0.3, 0.2), e.Luftwechselrate_h);
            Assert.Equal(600.0, e.InnereGewinne_W);
            Assert.Equal(3.0, e.Raumhoehe_M);
            Assert.True(double.IsNaN(e.Luftvolumen_M3));
            Assert.Equal(e.Luftwechselrate_h * e.Nutzflaeche_M2 * 3.0 * GebaeudeFestwerte.C_RHO_LUFT, e.Lueftungsleitwert_WK);
            Assert.Equal(1.0 / (e.Lueftungsleitwert_WK + 20.0), e.Parameter.R_ext_KW, 1e-12);
            Assert.Equal(Vorgabeherkunft.Zone, e.Vorgaben.SollTag.Herkunft);
            Assert.Equal(Vorgabeherkunft.Gebaeude, e.Vorgaben.SollWochenende.Herkunft);
        }

        /// <summary>Eine Zone, deren Spalten alle leer sind, rechnet bitgleich wie eine ohne Eingaben.</summary>
        [Fact]
        public void Eine_Zone_ohne_eigene_Werte_rechnet_bitgleich()
        {
            ProjektGebaeudeModel g1 = Vdi6007Probe.Gebaeude();
            ProjektGebaeudeModel g2 = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g1);
            g1.Zonen = new[] { basis };
            g2.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile, double.NaN, new Zoneneingaben()) };
            GebaeudeModellEingang e1 = Einzonen(g1), e2 = Einzonen(g2);
            Bitgleich("leere Eingaben", e1, Vdi6007Rechenweg.Laufen(e1, 0, 1), e2, Vdi6007Rechenweg.Laufen(e2, 0, 1));
        }

        [Fact]
        public void Eine_einzige_unbeheizte_Zone_wird_benannt_abgelehnt()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            g.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile, double.NaN,
                                                    new Zoneneingaben(IstBeheizt: false)) };
            var ex = Assert.Throws<GebaeudeModellException>(() => Einzonen(g));
            Assert.Equal(GebaeudeModellFehler.KeineBeheizteZone, ex.Grund);
        }

        /// <summary>Festlegung 5: Die Leistungsgrenze des Gebäudes wird erst ab zwei Zonen nach Fläche verteilt.</summary>
        [Fact]
        public void Leistungsgrenzen_werden_erst_ab_zwei_Zonen_anteilig()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizleistung_Max = 8.0;
            GebaeudeZonensatz basis = Geschichtet(g);
            var halb = new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile, 100.5);
            g.Zonen = new[] { halb };
            Assert.Equal(8000.0, Einzonen(g).HeizleistungMaxW);

            var zweite = new GebaeudeZonensatz(8, "Zweite", basis.Bauteile, 100.5, null, 2);
            g.Zonen = new[] { halb, zweite };
            IReadOnlyList<ZonenEingang> z = ZonenEingang.Bauen(g, KlimaDes());
            Assert.Equal(2, z.Count);
            Assert.All(z, x => Assert.Equal(1000.0 * (8.0 * (100.5 / 201.0)), x.Eingang.HeizleistungMaxW));
            Assert.All(z, x => Assert.Equal(2, x.Eingang.Zonenzahl));
        }

        // =====================================================================
        //  Trennflächen
        // =====================================================================

        [Fact]
        public void Die_Gegenseite_einer_Trennflaeche_ist_gespiegelt()
        {
            var b = new BauteilEingang("Decke", Bauteilart.Decke, 40.0, Bauteilrand.Zone, schichten: new[] { Estrich, Daemmung, Beton },
                                       neigungGrad: double.NaN, azimutGrad: 90.0, psiL_WK: 3.0, alphaKonInnen_WM2K: 1.7,
                                       alphaKonAussen_WM2K: 2.5, idNachbarzone: 5, zuordnung: Trennflaechenzuordnung.Aussen);
            BauteilEingang g = b.Gespiegelt(3);
            Assert.Equal(new[] { Beton, Daemmung, Estrich }, g.Schichten);
            Assert.Equal(2.5, g.AlphaKonInnen_WM2K);
            Assert.Equal(1.7, g.AlphaKonAussen_WM2K);
            Assert.Equal(180.0, g.NeigungWirksamGrad);
            Assert.Equal(270.0, g.AzimutGrad);
            Assert.Equal(0.0, g.PsiL_WK);
            Assert.Equal(3, g.IdNachbarzone);
            Assert.Equal(Trennflaechenzuordnung.Aussen, g.Zuordnung);
            Assert.Equal(b.Flaeche_M2, g.Flaeche_M2);
            Assert.Throws<InvalidOperationException>(() => new BauteilEingang("Wand", Bauteilart.Aussenwand, 1.0, Bauteilrand.Aussenluft).Gespiegelt(1));
        }

        [Fact]
        public void Die_Gruppe_einer_Trennflaeche_folgt_ihrer_Zuordnung()
        {
            BauteilEingang T(Trennflaechenzuordnung z) => new BauteilEingang("T", Bauteilart.Innenwand, 10.0, Bauteilrand.Zone,
                schichten: new[] { Putz, Innenmauerwerk, Putz }, idNachbarzone: 2, zuordnung: z);
            Assert.Equal(Bauteilgruppe.Innen, T(Trennflaechenzuordnung.Regel).Gruppe);
            Assert.Equal(Bauteilgruppe.Innen, T(Trennflaechenzuordnung.Innen).Gruppe);
            Assert.Equal(Bauteilgruppe.Aussen, T(Trennflaechenzuordnung.Aussen).Gruppe);
            Assert.True(T(Trennflaechenzuordnung.Aussen).KoppeltAnNachbarzone);
            Assert.False(T(Trennflaechenzuordnung.Innen).KoppeltAnNachbarzone);

            // Die Ablehnung fällt nur im Mehrzonenweg; dort braucht die Trennfläche ihren Nachbarn.
            var ex = Assert.Throws<GebaeudeModellException>(() => T(Trennflaechenzuordnung.Aussen).Pruefen("P"));
            Assert.Equal(GebaeudeModellFehler.RandbedingungNichtAbgebildet, ex.Grund);
            T(Trennflaechenzuordnung.Aussen).Pruefen("P", mehrzonenweg: true);
            ex = Assert.Throws<GebaeudeModellException>(() =>
                new BauteilEingang("T", Bauteilart.Innenwand, 10.0, Bauteilrand.Zone, 1.0).Pruefen("P", mehrzonenweg: true));
            Assert.Equal(GebaeudeModellFehler.ZonenkopplungUngueltig, ex.Grund);
        }

        /// <summary>
        /// Ein Gebäude mit Wohnzone und unbeheiztem Keller: Die Kellerdecke führt die Wohnzone (am Ende
        /// ihrer Liste), der Keller rechnet sie gespiegelt am Ende seiner. Eine unbeheizte Zone koppelt
        /// immer über die Außengruppe.
        /// </summary>
        [Fact]
        public void Wohnzone_und_Keller_koppeln_ueber_die_Kellerdecke()
        {
            ProjektGebaeudeModel g = MitKeller(out _);
            IReadOnlyList<ZonenEingang> z = ZonenEingang.Bauen(g, KlimaDes());
            Assert.Equal(new[] { WOHNEN, KELLER }, z.Select(x => x.ZonenId));

            ZonenEingang wohnen = z[0], keller = z[1];
            BauteilEingang decke = wohnen.Eingang.Bauteile[wohnen.Eingang.Bauteile.Count - 1];
            Assert.Equal(Bauteilrand.Zone, decke.Rand);
            Assert.Equal(Trennflaechenzuordnung.Aussen, decke.Zuordnung);
            BauteilEingang gegen = keller.Eingang.Bauteile[keller.Eingang.Bauteile.Count - 1];
            Assert.Equal(WOHNEN, gegen.IdNachbarzone);
            Assert.Equal(0.0, gegen.NeigungWirksamGrad);
            Assert.Equal(decke.Schichten.Reverse(), gegen.Schichten);

            Assert.True(wohnen.Gekoppelt);
            Assert.Equal(new[] { 1 }, wohnen.NachbarIndex);
            Assert.Equal(new[] { 0 }, keller.NachbarIndex);
            Assert.True(double.IsNaN(wohnen.Eingang.ThetaEq[100]));
            Assert.False(keller.IstBeheizt);
            Assert.True(keller.Eingang.ThetaSoll.All(double.IsNaN));
            Assert.All(keller.Eingang.ThetaMax, t => Assert.True(double.IsPositiveInfinity(t)));

            // θ_eq = (Zähler + U·A·θ_Keller) / Σ U·A, Σ B_v = 1.
            Nachbarglied n = Assert.Single(wohnen.Eingang.Nachbarglieder);
            Assert.Equal(KELLER, n.ZonenId);
            double[] luft = { 21.0, 9.0 };
            double erwartet = (wohnen.Eingang.ThetaEqZaehler[100] + n.UA_WK * 9.0) / wohnen.Eingang.UaSummeGewichtung_WK;
            Assert.Equal(Bits(erwartet), Bits(wohnen.ThetaEq(100, luft)));
            Assert.True(Math.Abs(wohnen.Eingang.SummeGewichte - 1.0) <= 1e-12);
            Assert.True(Math.Abs(keller.Eingang.SummeGewichte - 1.0) <= 1e-12);
        }

        /// <summary>
        /// <b>Der Kopplungspfad verfälscht die Einzonenrechnung nicht</b> (der Gedanke von Probe 1,
        /// Lauf 1): Hält man die Kellerluft auf der Kellertemperatur fest, rechnet die Wohnzone Bit für
        /// Bit wie das Einzonengebäude mit unbeheiztem Rand — mit derselben Fläche am Ende der
        /// Bauteilliste und dem Übergang A7 (b).
        /// </summary>
        [Fact]
        public void Festgehaltene_Kellerluft_ist_der_unbeheizte_Rand()
        {
            const double keller = 8.0;
            ProjektGebaeudeModel g = MitKeller(out List<BauteilEingang> wohnteile);

            ProjektGebaeudeModel einzonen = Vdi6007Probe.Gebaeude();
            einzonen.Kellertemperatur = keller;
            einzonen.Zonen = new[] { new GebaeudeZonensatz(WOHNEN, "Wohnen", wohnteile
                .Select(b => b.Rand == Bauteilrand.Zone
                    ? new BauteilEingang(b.Bezeichnung, b.Art, b.Flaeche_M2, Bauteilrand.Unbeheizt, schichten: b.Schichten)
                    : b).ToList()) };
            GebaeudeModellEingang e1 = Einzonen(einzonen);
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(e1, 0, 1);

            // Bitgleich nur mit dem Übergang des unbeheizten Raums (A7 (b)); mit A7 (a) ändern sich R_Rest und U.
            Nachbaruebergang regel = GebaeudeFestwerte.NACHBARUEBERGANG;
            if (regel != Nachbaruebergang.WieUnbeheizt) return;
            ZonenEingang wohnen = ZonenEingang.Bauen(g, KlimaDes())[0];
            Assert.True(ParameterGleich(e1.Parameter, wohnen.Eingang.Parameter), "Die Ersatzparameter weichen ab.");
            var lauf = new Zonenlauf(wohnen);
            double[] luft = { double.NaN, keller };
            int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
            lauf.Beginnen(wohnen.Eingang.ThetaSoll[start]);
            for (int h = start; h < 8760; h++)
            {
                bool s = lauf.Sommerlueftung();
                Stundenrand r = wohnen.Rand(h, s, luft);
                Assert.Equal(Bits(e1.ThetaEq[h]), Bits(r.ThetaEq));
                lauf.VorlaufUebernehmen(h, lauf.Modell.Schritt(in r));
            }
            for (int h = 0; h < 8760; h++)
            {
                bool s = lauf.Sommerlueftung();
                Stundenrand r = wohnen.Rand(h, s, luft);
                lauf.Uebernehmen(h, s, lauf.Modell.Schritt(in r));
            }
            GebaeudeModellErgebnis ist = lauf.Ergebnis(0, 1);
            foreach ((string name, double[] a, double[] b) in new[]
            {
                ("HeizlastW", soll.HeizlastW, ist.HeizlastW), ("Raumtemperatur", soll.Raumtemperatur, ist.Raumtemperatur),
                ("OperativeTemperatur", soll.OperativeTemperatur, ist.OperativeTemperatur),
            })
                Assert.True(GebaeudeEinzonennetzTests.Bilden(a).Sha256 == GebaeudeEinzonennetzTests.Bilden(b).Sha256, name + " weicht ab.");
        }

        private static bool ParameterGleich(ErsatzparameterRC a, ErsatzparameterRC b)
            => Bits(a.C_AW_Jk) == Bits(b.C_AW_Jk) && Bits(a.C_IW_Jk) == Bits(b.C_IW_Jk)
               && Bits(a.R_1_AWGruppe_KW) == Bits(b.R_1_AWGruppe_KW) && Bits(a.R_Rest_AWGruppe_KW) == Bits(b.R_Rest_AWGruppe_KW)
               && Bits(a.R_1_IW_KW) == Bits(b.R_1_IW_KW) && Bits(a.R_conv_AW_KW) == Bits(b.R_conv_AW_KW)
               && Bits(a.R_conv_IW_KW) == Bits(b.R_conv_IW_KW) && Bits(a.R_rad_KW) == Bits(b.R_rad_KW)
               && Bits(a.R_ext_KW) == Bits(b.R_ext_KW) && Bits(a.A_AW_gesamt_M2) == Bits(b.A_AW_gesamt_M2)
               && Bits(a.A_IW_M2) == Bits(b.A_IW_M2);

        /// <summary>Ohne Übersteuerung entscheidet die 4-K-Regel (hier von außen gesetzt); ohne Entscheid rechnet die Trennfläche adiabat.</summary>
        [Fact]
        public void Die_4K_Regel_setzt_die_Gruppe_zwischen_beheizten_Zonen()
        {
            ProjektGebaeudeModel g = ZweiBeheizte(Trennflaechenzuordnung.Regel);
            IReadOnlyList<ZonenEingang> ohne = ZonenEingang.Bauen(g, KlimaDes());
            Assert.False(ohne[0].Gekoppelt);
            Assert.Empty(ohne[0].Eingang.Nachbarglieder);

            IReadOnlyList<ZonenEingang> mit = ZonenEingang.Bauen(g, KlimaDes(), vierK: (a, b) => Trennflaechenzuordnung.Aussen);
            Assert.True(mit[0].Gekoppelt);
            Assert.True(mit[1].Gekoppelt);
            Assert.Equal(mit[0].Eingang.Nachbarglieder[0].UA_WK, mit[1].Eingang.Nachbarglieder[0].UA_WK, 1e-9);

            // Die ausdrückliche Zuordnung geht der Regel vor (M3 (b)).
            IReadOnlyList<ZonenEingang> iw = ZonenEingang.Bauen(ZweiBeheizte(Trennflaechenzuordnung.Innen), KlimaDes(),
                                                                vierK: (a, b) => Trennflaechenzuordnung.Aussen);
            Assert.False(iw[0].Gekoppelt);
        }

        [Fact]
        public void Eine_widerspruechliche_Kopplung_wird_benannt_abgelehnt()
        {
            ProjektGebaeudeModel g = ZweiBeheizte(Trennflaechenzuordnung.Aussen);
            GebaeudeZonensatz a = g.Zonen[0];
            BauteilEingang fremd = new BauteilEingang("T", Bauteilart.Innenwand, 5.0, Bauteilrand.Zone, 1.0, idNachbarzone: 99);
            g.Zonen = new[] { new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile.Append(fremd).ToList(), a.Nutzflaeche_M2), g.Zonen[1] };
            Assert.Equal(GebaeudeModellFehler.ZonenkopplungUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => ZonenEingang.Bauen(g, KlimaDes())).Grund);

            g = ZweiBeheizte(Trennflaechenzuordnung.Aussen);
            g.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 42, 100.0) };
            Assert.Equal(GebaeudeModellFehler.ZonenkopplungUngueltig,
                         Assert.Throws<GebaeudeModellException>(() => ZonenEingang.Bauen(g, KlimaDes())).Grund);

            g = ZweiBeheizte(Trennflaechenzuordnung.Aussen);
            g.Zonen = g.Zonen.Select(z => new GebaeudeZonensatz(z.ZonenId, z.Bezeichnung, z.Bauteile, z.Nutzflaeche_M2,
                                                                 new Zoneneingaben(Nutzflaeche: 100.5, IstBeheizt: false), z.Rang)).ToList();
            Assert.Equal(GebaeudeModellFehler.KeineBeheizteZone,
                         Assert.Throws<GebaeudeModellException>(() => ZonenEingang.Bauen(g, KlimaDes())).Grund);
        }

        // =====================================================================
        //  Luftaustausch
        // =====================================================================

        /// <summary>
        /// Der Luftaustausch wirkt ohne Eingriff in den Löser: R_ext = 1/(H_ve + Σψ·L + Σ G), und die
        /// Zulufttemperatur mischt Außenluft und Nachbarluft so, dass gExt·θ_Lue den Strömen gleicht.
        /// </summary>
        [Fact]
        public void Der_Luftaustausch_wirkt_ueber_R_ext_und_die_Zulufttemperatur()
        {
            ProjektGebaeudeModel ohne = ZweiBeheizte(Trennflaechenzuordnung.Innen);
            ProjektGebaeudeModel mit = ZweiBeheizte(Trennflaechenzuordnung.Innen);
            mit.Sommerlueftung = true;
            ohne.Sommerlueftung = true;
            mit.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 2, 100.0) };
            ZonenEingang a0 = ZonenEingang.Bauen(ohne, KlimaDes())[0];
            ZonenEingang a = ZonenEingang.Bauen(mit, KlimaDes())[0];

            double g = GebaeudeFestwerte.C_RHO_LUFT * 100.0;
            Assert.Equal(g, a.Eingang.LuftaustauschLeitwert_WK);
            Assert.Equal(1.0 / (1.0 / a0.Eingang.Parameter.R_ext_KW + g), a.Eingang.Parameter.R_ext_KW, 1e-15);
            Assert.True(a.Gekoppelt);
            Assert.Equal(new[] { 1 }, a.LuftIndex);

            int h = 4000;
            double aussen = a.Eingang.ThetaOut[h];
            double gVe = 1.0 / a0.Eingang.Parameter.R_ext_KW;
            foreach (bool sommer in new[] { false, true })
            {
                double z = sommer ? a.Eingang.SommerlueftungZusatzleitwertWK : 0.0;
                double lue = a.ThetaLue(h, sommer, new[] { 20.0, 23.0 });
                double gExt = 1.0 / a.Eingang.Parameter.R_ext_KW + z;
                Assert.Equal((gVe + z) * aussen + g * 23.0, gExt * lue, 1e-8);
                Assert.Equal(aussen, a.ThetaLue(h, sommer, new[] { 20.0, aussen }), 1e-11);
                Assert.Equal(aussen, a0.ThetaLue(h, sommer, new[] { 20.0, 23.0 }));
            }
            Assert.True(a.Eingang.SommerlueftungZusatzleitwertWK > 0.0);
            Stundenrand r = a.Rand(h, false, new[] { 20.0, 23.0 });
            Assert.Equal(a.ThetaLue(h, false, new[] { 20.0, 23.0 }), r.ThetaOut);
        }

        // =====================================================================
        //  Probe 9 — Grenzfälle der Bauteilzuordnung (Mehrzonenkonzept 8.1; VDI 6007-1, 6.8)
        // =====================================================================

        /// <summary>
        /// Eine Zone ohne Außenbauteile — alle Grenzflächen adiabat: Die Widerstände der nicht
        /// vorhandenen Außengruppe tragen 10¹² statt 0, A_AW = 0; die Zone koppelt allein über
        /// Lüftung und Wärmebrücken nach außen und rechnet ein Jahr endlich.
        /// </summary>
        [Fact]
        public void Probe_9_Zone_ohne_Aussenbauteile()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            var wohnen = new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile, 181.0);
            var innen = new GebaeudeZonensatz(9, "Innenraum", new List<BauteilEingang>
            {
                new BauteilEingang("Innenwände", Bauteilart.Innenwand, 40.0, Bauteilrand.Innen, schichten: new[] { Putz, Innenmauerwerk, Putz }),
                new BauteilEingang("Trennwand", Bauteilart.Innenwand, 30.0, Bauteilrand.Zone, schichten: new[] { Putz, Innenmauerwerk, Putz },
                                   idNachbarzone: wohnen.ZonenId, zuordnung: Trennflaechenzuordnung.Innen),
            }, 20.0, null, 2);
            g.Zonen = new[] { wohnen, innen };
            ZonenEingang z = ZonenEingang.Bauen(g, KlimaDes())[1];
            ErsatzparameterRC p = z.Eingang.Parameter;

            Assert.True(p.OhneAussenbauteile);
            Assert.Equal(ErsatzparameterRC.R_OHNE_AW_KW, p.R_1_AW_KW);
            Assert.Equal(ErsatzparameterRC.R_OHNE_AW_KW, p.R_Rest_AW_KW);
            Assert.Equal(ErsatzparameterRC.R_OHNE_AW_KW, p.R_conv_AW_KW);
            Assert.Equal(ErsatzparameterRC.R_OHNE_AW_KW, p.R_rad_KW);
            Assert.Equal(0.0, p.A_AW_gesamt_M2);
            Assert.Equal(70.0, p.A_IW_M2);
            Assert.False(z.Gekoppelt);
            Assert.True(double.IsNaN(z.Eingang.SummeGewichte));
            Assert.All(z.Eingang.PhiRadAW, w => Assert.Equal(0.0, w));

            // Stationär: die Last ist allein die des masselosen Zweigs, H_ext·(θ_i − θ_e).
            var m = new Zonenmodell2K(p, "Innenraum");
            double last = m.StationaereHeizlastW(20.0, 0.0, 0.0, 0.0);
            Assert.Equal(20.0 / p.R_ext_KW, last, 1e-6);

            // Ein Jahr rechnet endlich.
            GebaeudeModellErgebnis r = Zonenlauf.Laufen(z, 0, 1);
            Assert.All(r.HeizlastW, w => Assert.True(double.IsFinite(w) && w >= 0.0));
            Assert.True(r.HeizlastW.Sum() > 0.0);
        }

        /// <summary>Ohne Innenbauteile wird nicht durch deren Fläche geteilt: A_IW = f_IW·A_f (Klassenweg der Gruppe).</summary>
        [Fact]
        public void Probe_9_Zone_ohne_Innenbauteile()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            g.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung,
                                                    basis.Bauteile.Where(b => b.Rand != Bauteilrand.Innen).ToList()) };
            GebaeudeModellEingang e = Einzonen(g);
            Assert.Equal(GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR * g.Nutzflaeche, e.Parameter.A_IW_M2);
            Assert.Equal(Gruppenweg.Klassenweg, e.Parameter.WegInnen);
            Assert.True(double.IsFinite(e.Parameter.R_conv_IW_KW) && double.IsFinite(e.Parameter.R_rad_KW));
            Assert.True(Vdi6007Rechenweg.Laufen(e, 0, 1).HeizlastW.All(double.IsFinite));
        }

        /// <summary>Der Umschaltpunkt Gl. (29) → (31): Ist A_IW kleiner als A_AW, bezieht sich R_rad auf A_IW.</summary>
        [Fact]
        public void Probe_9_Umschaltpunkt_der_Bezugsflaeche()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            foreach (double aInnen in new[] { 150.0, 2000.0 })
            {
                g.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile
                    .Select(b => b.Rand == Bauteilrand.Innen
                        ? new BauteilEingang(b.Bezeichnung, b.Art, aInnen, b.Rand, schichten: b.Schichten) : b).ToList()) };
                ErsatzparameterRC p = Einzonen(g).Parameter;
                double bezug = Math.Min(p.A_AW_gesamt_M2, p.A_IW_M2);
                Assert.Equal(aInnen < p.A_AW_gesamt_M2 ? aInnen : p.A_AW_gesamt_M2, bezug);
                Assert.Equal(1.0 / (GebaeudeFestwerte.ALPHA_STR_INNEN * bezug), p.R_rad_KW);
                Assert.Equal(aInnen < 1000.0, p.A_IW_M2 < p.A_AW_gesamt_M2);
            }
        }

        /// <summary>Σ B_v = 1 je Zone — mit und ohne Nachbarglied, als stehende Zusicherung des Eingangsbauers.</summary>
        [Fact]
        public void Probe_9_Die_Gewichte_summieren_sich_je_Zone_zu_eins()
        {
            foreach (string fall in GebaeudeEinzonennetzTests.Faelle)
            {
                GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(fall);
                Assert.True(Math.Abs(e.SummeGewichte - 1.0) <= GebaeudeFestwerte.GEWICHTE_SUMME_TOLERANZ, fall);
            }
            foreach (ZonenEingang z in ZonenEingang.Bauen(MitKeller(out _), KlimaDes()))
                Assert.True(Math.Abs(z.Eingang.SummeGewichte - 1.0) <= GebaeudeFestwerte.GEWICHTE_SUMME_TOLERANZ, z.Bezeichnung);
        }

        // =====================================================================
        //  Probe 12 — Bezugsperiode je Seite einer Trennfläche (Mehrzonenkonzept 3.2, 8.1)
        // =====================================================================

        /// <summary>
        /// Derselbe Aufbau mit raumseitiger Vorsatzschale und Luftschicht schaltet auf 2 Tage, ohne sie
        /// nicht; aufgeklebte Innendämmung schaltet nicht. An einer Trennfläche gilt das je Seite: Die
        /// Zone mit der Vorsatzschale rechnet 2 Tage, die Gegenseite — gespiegelt, die Vorsatzschale
        /// liegt dort außen — 7 Tage.
        /// </summary>
        [Fact]
        public void Probe_12_Bezugsperiode_je_Seite_der_Trennflaeche()
        {
            Schicht[] decke = { new Schicht(0.18, 2.3, 2400.0, 1000.0), new Schicht(0.03, 0.04, 100.0, 1000.0), new Schicht(0.05, 1.4, 2000.0, 1000.0) };
            Schicht[] vorsatz = { new Schicht(0.001, 50.0, 7850.0, 460.0), new Schicht(0.01, 0.04, 60.0, 840.0), Schicht.RuhendeLuft(0.20) };
            Schicht[] mit = vorsatz.Concat(decke).ToArray();
            Schicht[] geklebt = new[] { new Schicht(0.06, 0.035, 30.0, 1400.0) }.Concat(new[] { Mauerwerk, Putz }).ToArray();

            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D,
                         Bauteilreduktion.BezugsperiodeWaehlen(mit, 20.0, Waermestromrichtung.Aufwaerts).Periode_d);
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D,
                         Bauteilreduktion.BezugsperiodeWaehlen(decke, 20.0, Waermestromrichtung.Aufwaerts).Periode_d);
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D,
                         Bauteilreduktion.BezugsperiodeWaehlen(geklebt, 20.0, Waermestromrichtung.Horizontal).Periode_d);

            // Die Trennfläche: Zone 1 führt sie mit der Vorsatzschale raumseitig.
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            var t = new BauteilEingang("Zwischendecke", Bauteilart.Decke, 20.0, Bauteilrand.Zone, schichten: mit,
                                       idNachbarzone: 2, zuordnung: Trennflaechenzuordnung.Aussen);
            var eins = new GebaeudeZonensatz(1, "Unten", basis.Bauteile.Append(t).ToList(), 100.5, null, 1);
            var zwei = new GebaeudeZonensatz(2, "Oben", basis.Bauteile, 100.5, null, 2);
            g.Zonen = new[] { eins, zwei };
            IReadOnlyList<ZonenEingang> z = ZonenEingang.Bauen(g, KlimaDes());
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_ABGEDECKT_D,
                         z[0].Eingang.Parameter.Bauteilherleitung.Single(h => h.Bezeichnung == "Zwischendecke").Bezugsperiode_d);
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D,
                         z[1].Eingang.Parameter.Bauteilherleitung.Single(h => h.Bezeichnung == "Zwischendecke").Bezugsperiode_d);
        }

        // =====================================================================
        //  Gebäude der Proben
        // =====================================================================

        internal const int WOHNEN = 7;
        internal const int KELLER = 8;

        /// <summary>
        /// Das Probegebäude mit Wohnzone (die geschichtete Zone, die Bodenplatte als Kellerdecke am Ende
        /// ihrer Liste, Gruppe nach der Regel) und unbeheiztem Keller (Wände und Boden am Erdreich).
        /// </summary>
        internal static ProjektGebaeudeModel MitKeller(out List<BauteilEingang> wohnteile)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            BauteilEingang boden = basis.Bauteile.Single(b => b.Art == Bauteilart.Bodenplatte);
            wohnteile = basis.Bauteile.Where(b => b != boden).ToList();
            wohnteile.Add(new BauteilEingang("Kellerdecke", Bauteilart.Bodenplatte, boden.Flaeche_M2, Bauteilrand.Zone,
                                             schichten: boden.Schichten, idNachbarzone: KELLER));
            var wohnen = new GebaeudeZonensatz(WOHNEN, "Wohnen", wohnteile, g.Nutzflaeche, null, 1);
            var keller = new GebaeudeZonensatz(KELLER, "Keller", new List<BauteilEingang>
            {
                new BauteilEingang("Kellerwände", Bauteilart.Aussenwand, 100.0, Bauteilrand.Erdreich, schichten: new[] { Beton }),
                new BauteilEingang("Kellerboden", Bauteilart.Bodenplatte, boden.Flaeche_M2, Bauteilrand.Erdreich, schichten: new[] { Estrich, Beton }),
            }, 88.0, new Zoneneingaben(Nutzflaeche: 88.0, IstBeheizt: false), 2);
            g.Zonen = new[] { keller, wohnen };   // Eingabereihenfolge verkehrt: gerechnet wird nach Rang
            return g;
        }

        /// <summary>Zwei beheizte Hälften des Probegebäudes (Zone 1 und 2), getrennt durch eine Trennwand der Zuordnung <paramref name="zuordnung"/>.</summary>
        internal static ProjektGebaeudeModel ZweiBeheizte(Trennflaechenzuordnung zuordnung)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            var trenn = new BauteilEingang("Trennwand", Bauteilart.Innenwand, 30.0, Bauteilrand.Zone,
                                           schichten: new[] { Putz, Innenmauerwerk, Putz }, idNachbarzone: 2, zuordnung: zuordnung);
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(1, "West", basis.Bauteile.Append(trenn).ToList(), 100.5, null, 1),
                new GebaeudeZonensatz(2, "Ost", basis.Bauteile, 100.5, null, 2),
            };
            return g;
        }
    }
}
