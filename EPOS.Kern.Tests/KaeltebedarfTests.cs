using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kälteseite, Stufe KU1</b> (zweite Welle; Kühlkonzept 3, 4.2, 4.4, 5.5, 6.4, 10.2):
    /// Kühlsollwert und Kühlleistungsgrenze im Löser, die Fassade
    /// <see cref="SimulationKaeltebedarf"/> und der Projektschalter.
    ///
    /// <para><b>Die Rechenproben aus 10.2</b> — „Bestandsweg-Gebäude liefert Kältebedarf 0 mit
    /// Hinweis", „Symmetrie der Kennzahlen", „Ein Lauf, zwei Reihen" — dazu Parität,
    /// Leistungsgrenze, Energiebilanz, Vorzeichen und Abschnittsregel, die Prüfregel des
    /// Kühlsollwerts, die Bedarfsprobe Kälte und „Projekt aus → nichts gerechnet". Die Fälle
    /// ohne Datenbank rechnen das Probegebäude der Rechenschritte; die Fälle mit Datenbank
    /// schalten die Kühlung an einer Arbeitskopie ein (Referenzprojekte bleiben in der
    /// Testdatenbank aus).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltebedarfTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public KaeltebedarfTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        // =============================================================================
        //  Teil 1 — Löser und Eingang ohne Datenbank (10.2)
        // =============================================================================

        private static readonly SolardatenModel[] KLIMA = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        private static GebaeudeModellErgebnis Rechnen(ProjektGebaeudeModel g, bool kuehlbetrieb, out GebaeudeModellEingang e)
        {
            e = GebaeudeModellEingang.Bauen(g, KLIMA, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                            GebaeudeKlimaweg.ZEITBEZUG_VORGABE, kuehlbetrieb);
            return Vdi6007Rechenweg.Laufen(e, 0, g.ID_Gebaeude);
        }

        private static GebaeudeModellErgebnis Rechnen(ProjektGebaeudeModel g, bool kuehlbetrieb)
            => Rechnen(g, kuehlbetrieb, out _);

        private static ProjektGebaeudeModel Gekuehlt(double? sollwert, double? grenzeKw = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = sollwert;
            g.Kuehlleistung_Max = grenzeKw;
            return g;
        }

        /// <summary>K10/7.2: Ohne Projektschalter rechnet ein Gebäude mit Kühleingaben wie ohne sie.</summary>
        [Fact]
        public void Ohne_Projektschalter_rechnet_das_Gebaeude_wie_ohne_Kuehleingaben()
        {
            GebaeudeModellErgebnis ohne = Rechnen(Vdi6007Probe.Gebaeude(), false);
            GebaeudeModellErgebnis mit = Rechnen(Gekuehlt(22.0, 0.5), false, out GebaeudeModellEingang e);

            Assert.False(e.KuehlungWirksam);
            Assert.Equal(24.0, e.KuehlSollwert);
            Assert.True(double.IsNaN(e.KuehlleistungMaxW));
            Assert.False(mit.KuehlungWirksam);
            Assert.Equal(ohne.HeizlastW, mit.HeizlastW);
            Assert.Equal(ohne.KuehlbedarfKwh, mit.KuehlbedarfKwh);
            Assert.Equal(ohne.Raumtemperatur, mit.Raumtemperatur);
        }

        /// <summary>
        /// Paritätsprobe: Mit Kühlsollwert = oberer Raumtemperatur und ohne Grenze regelt der Löser
        /// genau dort, wo er vorher gekappt hat — alle Reihen bitgleich, nur jetzt „wirksam".
        /// </summary>
        [Fact]
        public void Paritaetsprobe_Kuehlsollwert_gleich_oberer_Raumtemperatur_ist_bitgleich()
        {
            GebaeudeModellErgebnis ohne = Rechnen(Vdi6007Probe.Gebaeude(), true);
            GebaeudeModellErgebnis mit = Rechnen(Gekuehlt(24.0), true, out GebaeudeModellEingang e);

            Assert.True(e.KuehlungWirksam);
            Assert.True(mit.KuehlungWirksam);
            Assert.Equal(24.0, mit.KuehlSollwert);
            Assert.Equal(ohne.HeizlastW, mit.HeizlastW);
            Assert.Equal(ohne.KuehlbedarfKwh, mit.KuehlbedarfKwh);
            Assert.Equal(ohne.Raumtemperatur, mit.Raumtemperatur);
            Assert.True(mit.KuehlenergieMwh > 0.0);
        }

        /// <summary>10.2 „Kühlsollwert sehr hoch": kein Kühlbedarf, die Heizreihe wie ohne Kappung.</summary>
        [Fact]
        public void Kuehlsollwert_sehr_hoch_kuehlt_nie_und_die_Heizreihe_bleibt()
        {
            ProjektGebaeudeModel ohneKappung = Vdi6007Probe.Gebaeude();
            ohneKappung.Maximaleraumtemperatur = 60.0;
            GebaeudeModellErgebnis frei = Rechnen(ohneKappung, true);
            GebaeudeModellErgebnis hoch = Rechnen(Gekuehlt(60.0), true);

            Assert.All(hoch.KuehlbedarfKwh, w => Assert.Equal(0.0, w));
            Assert.Equal(frei.HeizlastW, hoch.HeizlastW);
            Assert.Equal(frei.Raumtemperatur, hoch.Raumtemperatur);
        }

        /// <summary>10.2 „Kühlsollwert = höchster Heizsollwert + 1 K, Winter": kein Kühlen bei Frost.</summary>
        [Fact]
        public void Kuehlsollwert_knapp_ueber_dem_Heizsollwert_kuehlt_im_Winter_nicht()
        {
            GebaeudeModellErgebnis r = Rechnen(Gekuehlt(21.0), true);
            for (int h = 0; h < 744; h++)
                Assert.Equal(0.0, r.KuehlbedarfKwh[h]);
            Assert.True(r.KuehlenergieMwh > 0.0, "Im Sommer muss gekühlt werden.");
        }

        /// <summary>
        /// F-K2, 10.2 „Kühlleistungsgrenze wirkt": Der Kühlbedarf ist exakt auf die Grenze gekappt,
        /// und in diesen Stunden steigt die Raumluft über den Kühlsollwert.
        /// </summary>
        [Fact]
        public void Die_Kuehlleistungsgrenze_kappt_den_Kuehlbedarf_und_die_Raumluft_steigt()
        {
            GebaeudeModellErgebnis frei = Rechnen(Gekuehlt(22.0), true);
            double spitzeFrei = frei.KuehlbedarfKwh.Max();
            double grenzeKw = Math.Round(0.4 * spitzeFrei, 3);
            Assert.True(grenzeKw > 0.0);

            GebaeudeModellErgebnis begrenzt = Rechnen(Gekuehlt(22.0, grenzeKw), true, out GebaeudeModellEingang e);
            Assert.Equal(1000.0 * grenzeKw, e.KuehlleistungMaxW);

            double spitze = begrenzt.KuehlbedarfKwh.Max();
            Assert.True(spitze <= grenzeKw * (1.0 + 1e-12) + 1e-12, "Spitze " + spitze + " über der Grenze " + grenzeKw);
            Assert.True(spitze >= grenzeKw * (1.0 - 1e-9), "Die Grenze wird nie erreicht.");

            int anDerGrenze = 0, darueber = 0;
            for (int h = 0; h < 8760; h++)
            {
                if (begrenzt.KuehlbedarfKwh[h] < grenzeKw * (1.0 - 1e-9)) continue;
                anDerGrenze++;
                if (begrenzt.Raumtemperatur[h] > 22.0 + 1e-6) darueber++;
            }
            Assert.True(anDerGrenze > 0);
            Assert.True(darueber > 0, "An der Grenze muss die Raumluft über den Kühlsollwert steigen.");
            Assert.True(begrenzt.KuehlenergieMwh < frei.KuehlenergieMwh);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Grenze {0} kW: {1} Stunden an der Grenze, {2} davon über 22 °C; Kühlenergie {3:F3} → {4:F3} MWh",
                grenzeKw, anDerGrenze, darueber, frei.KuehlenergieMwh, begrenzt.KuehlenergieMwh));
        }

        /// <summary>
        /// Energiebilanz mit Kühlung und Kühlgrenze: zugeführte Wärme minus Kälte plus Lasten ist
        /// Abfluss plus Speicheränderung — dieselbe Probe wie für die Sommerlüftung (G2).
        /// </summary>
        [Fact]
        public void Die_Energiebilanz_schliesst_mit_Kuehlung_und_Kuehlgrenze()
        {
            GebaeudeModellErgebnis frei = Rechnen(Gekuehlt(22.0), true);
            double grenzeKw = Math.Round(0.5 * frei.KuehlbedarfKwh.Max(), 3);
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(Gekuehlt(22.0, grenzeKw), KLIMA, Vdi6007Probe.Wochenende(),
                Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, true);

            ErsatzparameterRC p = e.Parameter;
            var m = new Zonenmodell2K(p, "Bilanz KU1");
            m.Zuruecksetzen(20.0);
            for (int h = 8760 - Vdi6007Rechenweg.VORLAUF_H; h < 8760; h++)
            {
                Stundenrand r0 = e.Rand(h);
                m.Schritt(in r0);
            }
            double aw0 = m.ThetaMAw, iw0 = m.ThetaMIw, zu = 0.0, ab = 0.0, kaelte = 0.0;
            double gRest = 1.0 / p.R_Rest_AWGruppe_KW, gExt = 1.0 / p.R_ext_KW;
            int grenzstunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                Stundenrand r = e.Rand(h);
                Assert.Equal(e.KuehlleistungMaxW, r.KuehlleistungMaxW);
                Stundenergebnis s = m.Schritt(in r);
                zu += s.HeizleistungW - s.KuehlleistungW + r.PhiRadAW + r.PhiRadIW + r.PhiConv;
                ab += gRest * (s.ThetaMAwMittel - r.ThetaEq) + gExt * (s.ThetaAirMittel - r.ThetaOut);
                kaelte += s.KuehlleistungW;
                if (s.KuehlleistungW >= e.KuehlleistungMaxW * (1.0 - 1e-9)) grenzstunden++;
            }
            double speicher = (p.C_AW_Jk * (m.ThetaMAw - aw0) + p.C_IW_Jk * (m.ThetaMIw - iw0)) / 3600.0;
            Assert.True(kaelte > 0.0 && grenzstunden > 0);
            Assert.True(Math.Abs(zu - ab - speicher) <= 1e-7 * Math.Abs(zu) + 1e-3,
                "Bilanz offen: zu " + zu + " Wh, ab " + ab + " Wh, Speicher " + speicher + " Wh");
        }

        /// <summary>
        /// F-K3, 10.2 „Vorzeichen": beide Reihen nie negativ; die Stunden mit beidem sind genau die
        /// Stunden, in denen beide Reihen tragen (Hinweis, kein Fehler). Die scharfe Abschnittsregel
        /// hält — sonst hätte der Löser benannt abgebrochen.
        /// </summary>
        [Fact]
        public void Beide_Reihen_nie_negativ_und_die_Stunden_mit_beidem_kommen_aus_den_Reihen()
        {
            foreach (double soll in new[] { 21.0, 22.0, 24.0 })
            {
                GebaeudeModellErgebnis r = Rechnen(Gekuehlt(soll), true);
                int beides = 0;
                for (int h = 0; h < 8760; h++)
                {
                    Assert.True(r.HeizlastW[h] >= 0.0);
                    Assert.True(r.KuehlbedarfKwh[h] >= 0.0);
                    if (r.HeizlastW[h] > 0.0 && r.KuehlbedarfKwh[h] > 0.0) beides++;
                }
                Assert.Equal(beides, r.StundenHeizenUndKuehlen);
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "Kühlsollwert {0} °C: {1} Stunden mit Heizen und Kühlen, {2} Stunden mit Umschaltung",
                    soll, r.StundenHeizenUndKuehlen, r.StundenMitUmschaltung));
            }
        }

        /// <summary>
        /// <b>Eine Regel, zwei Stellen</b> (Kühlkonzept 8.1, KU1 Welle 3): Der Gebäudedialog prüft
        /// den Kühlsollwert gegen <see cref="Gebaeudemodellvorgaben.HoechsterHeizsollwert"/>, der
        /// Löser gegen das Maximum des gerechneten Sollwertfahrplans. Für dieselben Eingaben sind
        /// beide Zahlen gleich — Tag, Nacht, ein unwirksamer und ein wirksamer Wochenendsollwert,
        /// Ferien mit und ohne Fahrplan —, und der Löser bricht genau dann ab, wenn der Dialog
        /// meldet.
        /// </summary>
        [Theory]
        [InlineData(20.0, 18.0, 0.0, 0.0, false)]
        [InlineData(20.0, 18.0, 22.0, 0.0, false)]
        [InlineData(19.0, 21.0, 4.0, 0.0, false)]
        [InlineData(20.0, 18.0, 0.0, 23.0, true)]
        [InlineData(20.0, 18.0, 0.0, 23.0, false)]
        public void Der_Dialog_prueft_den_Kuehlsollwert_mit_der_Regel_des_Loesers(
            double tag, double nacht, double wochenende, double ferien, bool mitFerien)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Tag = tag;
            g.Raumsolltemperatur_Nachtabsenkung = nacht;
            g.Raumsolltemperatur_Wochenende = wochenende;
            g.Raumsolltemperatur_Ferien = ferien;
            g.Maximaleraumtemperatur = 27.0;
            if (mitFerien)
            {
                g.Ferien = 1.0;
                g.Ferienbeginn_3 = 190.0;
                g.Ferienende_3 = 230.0;
            }

            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(
                g, KLIMA, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false);
            bool ferienAktiv = e.Ferientage.Any(t => t);
            double fahrplan = e.ThetaSoll.Max();
            double dialog = Gebaeudemodellvorgaben.HoechsterHeizsollwert(tag, nacht, wochenende, ferien, ferienAktiv);
            Assert.Equal(fahrplan, dialog);
            Assert.Equal(mitFerien, ferienAktiv);

            // Genau an der Grenze: gerade erlaubt, knapp darunter ein benannter Abbruch.
            double grenze = dialog + Gebaeudemodellvorgaben.KuehlsollwertAbstand;
            Rechnen(Mit(g, grenze), true);
            var ex = Assert.Throws<GebaeudeModellException>(() => Rechnen(Mit(g, grenze - 0.1), true));
            Assert.Equal(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert, ex.Grund);

            static ProjektGebaeudeModel Mit(ProjektGebaeudeModel quelle, double soll)
            {
                quelle.Kuehlung_Aktiv = true;
                quelle.Kuehl_Sollwert = soll;
                return quelle;
            }
        }

        /// <summary>3.2, 8.5: Die harte Prüfregel nennt beide Werte und bricht benannt ab.</summary>
        [Fact]
        public void Ein_Kuehlsollwert_unter_Heizsollwert_plus_1_K_bricht_benannt_ab()
        {
            var ex = Assert.Throws<GebaeudeModellException>(() => Rechnen(Gekuehlt(20.5), true));
            Assert.Equal(GebaeudeModellFehler.KuehlsollwertUnterHeizsollwert, ex.Grund);
            Assert.Contains("20.5", ex.Message);
            Assert.Contains("20 °C", ex.Message);

            // Genau 1 K darüber ist zulässig; ohne Projektschalter wird nicht geprüft.
            Rechnen(Gekuehlt(21.0), true);
            Rechnen(Gekuehlt(20.5), false);
        }

        /// <summary>F-K1: Ein leerer Kühlsollwert heißt „Kühlung aus" — ausdrücklich, nicht still.</summary>
        [Fact]
        public void Ein_leerer_Kuehlsollwert_heisst_Kuehlung_aus()
        {
            GebaeudeModellErgebnis ohne = Rechnen(Vdi6007Probe.Gebaeude(), true);
            GebaeudeModellErgebnis leer = Rechnen(Gekuehlt(null, 1.0), true, out GebaeudeModellEingang e);
            Assert.False(e.KuehlungWirksam);
            Assert.False(leer.KuehlungWirksam);
            Assert.Equal(ohne.HeizlastW, leer.HeizlastW);
        }

        [Fact]
        public void Eine_Kuehlleistungsgrenze_von_null_ist_ein_benannter_Fehler()
        {
            var ex = Assert.Throws<GebaeudeModellException>(() => Rechnen(Gekuehlt(25.0, 0.0), true));
            Assert.Equal(GebaeudeModellFehler.ParameterUngueltig, ex.Grund);
        }

        /// <summary>3.4: Mit wirksamer Kühlung schaltet die Sommerlüftung ab θ_kuehl − 3 K.</summary>
        [Fact]
        public void Die_Sommerlueftung_folgt_dem_Kuehlsollwert()
        {
            ProjektGebaeudeModel tief = Gekuehlt(26.0);
            ProjektGebaeudeModel hoch = Gekuehlt(30.0);
            tief.Sommerlueftung = true;
            hoch.Sommerlueftung = true;
            GebaeudeModellErgebnis a = Rechnen(tief, true);     // Schwelle 23 °C
            GebaeudeModellErgebnis b = Rechnen(hoch, true);     // Schwelle 27 °C
            Assert.True(a.StundenMitSommerlueftung > 0);
            Assert.True(b.StundenMitSommerlueftung < a.StundenMitSommerlueftung,
                "Schwelle 27 °C: " + b.StundenMitSommerlueftung + " h, Schwelle 23 °C: " + a.StundenMitSommerlueftung + " h");
        }

        // =============================================================================
        //  Teil 2 — Die Fassade ohne Datenbank (4.4, 5.5, F-K12, F-K18)
        // =============================================================================

        private static GebaeudeModellErgebnis GekuehltesErgebnis(double sollwert = 24.0)
            => Rechnen(Gekuehlt(sollwert), true);

        [Fact]
        public void Die_Fassade_verteilt_die_Kuehlreihe_und_meldet_die_Unterdeckung()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            GebaeudeModellErgebnis erg = GekuehltesErgebnis();

            kaelte.Beginnen(kanaele, true);
            kaelte.GebaeudeBuchen(0, Gekuehlt(24.0), erg);
            var mo = new int[12]; var moEnde = new int[12];
            WPPlan.Core.BhkwPlan.MonatsGrenzen(mo, moEnde);
            Assert.True(kaelte.Abschliessen(mo, moEnde));

            Assert.True(kaelte.Gerechnet);
            Assert.Equal(erg.KuehlbedarfKwh, kanaele.Kuehlung);
            Assert.Equal(erg.KuehlbedarfKwh, kaelte.Kaeltebedarf);
            Assert.Equal(erg.KuehlbedarfKwh.Max(), kaelte.Kaeltebedarf_Max);
            Assert.Equal(erg.StundenMitKuehlbedarf, kaelte.StundenMitKuehlbedarf);
            Assert.Equal(erg.KuehlenergieMwh, kaelte.Kaeltebedarf_Gesamt, 12);
            Assert.Equal(kaelte.Kaeltebedarf_Gesamt, kaelte.Kaelterestbedarf);   // KU1: gedeckt von niemandem
            Assert.Equal(kaelte.Kaeltebedarf_Gesamt, kaelte.Kaeltebedarf_Monat.Sum(), 9);
            Assert.Equal(0, kaelte.Bedarfsprobe_Verletzungen);
            Assert.Equal(1, kaelte.GekuehlteGebaeude);

            // Die eigene Dauerlinie: absteigend, normiert auf die Kältespitze.
            Assert.Equal(100.0, kaelte.Dauerlinie[0], 9);
            for (int h = 1; h < 8760; h++) Assert.True(kaelte.Dauerlinie[h] <= kaelte.Dauerlinie[h - 1]);
            Assert.Equal(kaelte.Kaeltebedarf_Gesamt * 1000.0 / kaelte.Kaeltebedarf_Max, kaelte.VollbenutzungsstundenKaelte, 9);

            // F-K12: Unterdeckung benannt; K5: die Grenze der Zahl einmal je Lauf.
            string warnung = Assert.Single(protokoll.Warnungen);
            Assert.StartsWith("Kältebedarf ohne Kälteerzeuger", warnung);
            Assert.Single(protokoll.Hinweise, h => h == SimulationKaeltebedarf.GrenzeFeuchte);
            Assert.Contains("Entfeuchtung", SimulationKaeltebedarf.GrenzeFeuchte);
        }

        /// <summary>#30, 4.4: Die Bedarfsprobe Kälte fällt laut — Stufe Fehler, Lauf fehlgeschlagen.</summary>
        [Fact]
        public void Die_Bedarfsprobe_Kaelte_faellt_laut_wenn_ein_Fremdbeitrag_im_Kuehlkanal_steht()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, true);
            kaelte.GebaeudeBuchen(0, Gekuehlt(24.0), GekuehltesErgebnis());

            kanaele.Kuehlung[4000] += 1.5;          // ein Beitrag, den niemand gebucht hat
            Assert.False(kaelte.Abschliessen(null, null));
            Assert.False(kaelte.Gerechnet);
            Assert.Equal(1, kaelte.Bedarfsprobe_Verletzungen);
            Assert.Equal(1.5, kaelte.Bedarfsprobe_MaxAbweichung, 9);
            Assert.False(string.IsNullOrEmpty(kaelte.Fehlertext));
            Assert.Equal(kaelte.Fehlertext, Assert.Single(protokoll.Fehler));
        }

        /// <summary>10.2 „Bestandsweg-Gebäude liefert 0 mit Hinweis" — Fassade allein, samt Gegenprobe.</summary>
        [Fact]
        public void Ein_Gebaeude_auf_dem_Bestandsweg_liefert_0_mit_genau_einem_Hinweis()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            ProjektGebaeudeModel g = Gekuehlt(22.0);
            g.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;

            kaelte.Beginnen(kanaele, true);
            kaelte.GebaeudeBuchen(0, g, null);
            kaelte.GebaeudeBuchen(0, g, null);      // derselbe Lauf: der Hinweis kommt einmal
            Assert.True(kaelte.Abschliessen(null, null));
            Assert.All(kanaele.Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Equal(0.0, kaelte.Kaeltebedarf_Gesamt);
            Assert.Contains(g.ID_Gebaeude, kaelte.GebaeudeBestandsweg);
            string hinweis = Assert.Single(protokoll.Hinweise);
            Assert.Contains("Tagesbilanz (Bestandsweg) liefert keine Kühllast", hinweis);
            Assert.Contains(g.Gebaeudename, hinweis);
            Assert.Empty(protokoll.Warnungen);       // ohne Kältebedarf keine Unterdeckung

            // Gegenprobe: dasselbe Gebäude auf VDI 6007 trägt Kältebedarf und keinen Hinweis.
            SimulationProtokoll zweiter = SimulationProtokoll.NeuStarten();
            kaelte.Beginnen(kanaele = new Kanalsatz(), true);
            g.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            kaelte.GebaeudeBuchen(0, g, Rechnen(g, true));
            Assert.True(kaelte.Abschliessen(null, null));
            Assert.True(kanaele.Kuehlung.Sum() > 0.0);
            Assert.Empty(kaelte.GebaeudeBestandsweg);
            Assert.DoesNotContain(zweiter.Hinweise, h => h.Contains("Bestandsweg"));
        }

        /// <summary>
        /// K6 (E31), 3.5, 6.4: Stunden mit gleichzeitigem Heizen und Kühlen werden nicht saldiert,
        /// sondern ausgewiesen — Projektwert ist das Maximum über die gekühlten Gebäude, mit dem
        /// führenden Gebäude als Herkunft, und der Lauf nennt ihn als Hinweis.
        /// </summary>
        [Fact]
        public void Stunden_mit_Heizen_und_Kuehlen_werden_als_Maximum_mit_Herkunft_ausgewiesen()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, true);

            ProjektGebaeudeModel a = Gekuehlt(24.0); a.ID_Gebaeude = 1; a.Gebaeudename = "Nordzone";
            ProjektGebaeudeModel b = Gekuehlt(24.0); b.ID_Gebaeude = 2; b.Gebaeudename = "Suedzone";
            kaelte.GebaeudeBuchen(0, a, Synthetisch(1, beides: 3));
            kaelte.GebaeudeBuchen(1, b, Synthetisch(2, beides: 7));
            Assert.True(kaelte.Abschliessen(null, null));

            Assert.Equal(7, kaelte.StundenHeizenUndKuehlen);
            Assert.Equal("Suedzone", kaelte.StundenHeizenUndKuehlenGebaeude);
            Assert.Equal(2, kaelte.GekuehlteGebaeude);
            string hinweis = Assert.Single(protokoll.Hinweise, h => h.StartsWith("Stunden mit gleichzeitigem Heizen und Kühlen", StringComparison.Ordinal));
            Assert.Contains("7 h", hinweis);
            Assert.Contains("Suedzone", hinweis);

            // Nicht saldiert: Der Kühlkanal ist die Summe der Kühlreihen, die Heizlast bleibt der Wärme.
            Assert.Equal(2.0 * 8760 * 0.5 / 1000.0, kaelte.Kaeltebedarf_Gesamt, 12);
        }

        /// <summary>Ein Ergebnis mit gewählter Zahl von Stunden mit Heizen und Kühlen (Kühlreihe 0,5 kWh je Stunde).</summary>
        private static GebaeudeModellErgebnis Synthetisch(int id, int beides)
        {
            double[] heiz = Enumerable.Repeat(1000.0, 8760).ToArray();
            double[] kuehl = Enumerable.Repeat(0.5, 8760).ToArray();
            double[] luft = Enumerable.Repeat(22.0, 8760).ToArray();
            return new GebaeudeModellErgebnis(0, id, DbWerte.GEBAEUDE_MODELL_VDI6007, heiz, luft, luft, kuehl, 24.0,
                                              8760.0, 1.0, beides, beides, kuehlSollwert: 24.0);
        }

        /// <summary>K10, 8.3: Projekt aus — nichts gebucht, nichts erhoben, der Grund steht im Protokoll.</summary>
        [Fact]
        public void Projekt_aus_bucht_nichts_und_nennt_den_Grund()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, false);
            kaelte.GebaeudeBuchen(0, Gekuehlt(24.0), GekuehltesErgebnis());
            kaelte.GanglinieBuchen(Enumerable.Repeat(2.0, 8760).ToArray());
            Assert.True(kaelte.Abschliessen(null, null));

            Assert.False(kaelte.Gerechnet);
            Assert.All(kanaele.Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Equal(0.0, kaelte.Kaeltebedarf_Gesamt);
            string hinweis = Assert.Single(protokoll.Hinweise);
            Assert.Contains("1 Gebäude", hinweis);
            Assert.Contains("1 Lastgänge", hinweis);
            Assert.Empty(protokoll.Warnungen);
        }

        // =============================================================================
        //  Teil 3 — Symmetrie der Kennzahlen (10.2, F-K19)
        // =============================================================================

        /// <summary>
        /// Die Probe läuft über die LISTE: Jedes Paar nennt Mitglieder, die es gibt; jede Zeile ohne
        /// Gegenstück trägt ihre Begründung; und jedes öffentliche Mitglied der Kältefassade steht
        /// in der Liste — ein neues Mitglied ohne Gegenstück oder Begründung fällt hier auf.
        /// </summary>
        [Fact]
        public void Symmetrie_der_Kennzahlen_ueber_die_Gegenueberstellung()
        {
            var liste = SimulationKaeltebedarf.GEGENSTUECKE;
            foreach (var z in liste)
            {
                if (z.Waerme == null || z.Kaelte == null)
                    Assert.False(string.IsNullOrWhiteSpace(z.Abweichung), "Ohne Gegenstück ohne Begründung: " + (z.Waerme ?? z.Kaelte));
                if (z.Stufe != "KU1") continue;
                if (z.Waerme != null && z.Kaelte != null)
                {
                    Assert.True(MitgliedDa(z.Waerme, typeof(SimulationWaermebedarf)), "Wärmeseite fehlt: " + z.Waerme);
                    Assert.True(MitgliedDa(z.Kaelte, typeof(SimulationKaeltebedarf)), "Kälteseite fehlt: " + z.Kaelte);
                }
                else if (z.Kaelte != null && !z.Kaelte.Contains(' '))
                    Assert.True(MitgliedDa(z.Kaelte, typeof(SimulationKaeltebedarf)), "Kälteseite fehlt: " + z.Kaelte);
            }

            var gelistet = new HashSet<string>(liste.Where(z => z.Kaelte != null).Select(z => z.Kaelte));
            const BindingFlags OEFFENTLICH = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            IEnumerable<string> mitglieder = typeof(SimulationKaeltebedarf).GetFields(OEFFENTLICH).Select(f => f.Name)
                .Concat(typeof(SimulationKaeltebedarf).GetProperties(OEFFENTLICH).Select(p => p.Name))
                .Where(n => n != nameof(SimulationKaeltebedarf.GEGENSTUECKE));
            foreach (string n in mitglieder)
                Assert.True(gelistet.Contains(n), "Mitglied der Kältefassade ohne Zeile in der Gegenüberstellung: " + n);

            // Die drei Ergebnisfelder aus E21 (7.4) - persistierte Gegenstücke.
            Assert.NotNull(typeof(ErgebnisEnergiebedarfModel).GetField("Kaeltebedarf_Gesamt"));
            Assert.NotNull(typeof(ErgebnisEnergiebedarfModel).GetField("Kaeltelast_Max"));
            Assert.NotNull(typeof(ErgebnisEnergiebedarfModel).GetField("Kaelterestbedarf"));
        }

        /// <summary>Ein Name der Liste: Feld/Eigenschaft des Typs, oder Kanalsatz.X / Ergebnis.X / Freitext mit Leerzeichen.</summary>
        private static bool MitgliedDa(string name, Type typ)
        {
            if (name.Contains(' ')) return true;                       // Kennzahl ohne Feld (Freitext)
            if (name.StartsWith("Kanalsatz.", StringComparison.Ordinal))
                return typeof(Kanalsatz).GetMethod(name.Substring("Kanalsatz.".Length)) != null;
            if (name.StartsWith("Ergebnis.", StringComparison.Ordinal))
                return typeof(ErgebnisEnergiebedarfModel).GetField(name.Substring("Ergebnis.".Length)) != null;
            const BindingFlags ALLE = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return typ.GetField(name, ALLE) != null || typ.GetProperty(name, ALLE) != null;
        }

        // =============================================================================
        //  Teil 4 — Mit der Testdatenbank (10.2, 10.3)
        // =============================================================================

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static void GebaeudeKuehlen(int idGebaeude, double sollwert, double? grenzeKw = null)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehlung_Aktiv = 1, Kuehl_Sollwert = ?, Kuehlleistung_Max = ? WHERE ID = ?",
                new DbParam("@s", sollwert),
                new DbParam("@g", grenzeKw.HasValue ? (object)grenzeKw.Value : DBNull.Value),
                new DbParam("@id", idGebaeude)));
        }

        private static SimulationWaermebedarf Bedarf(int idProjekt)
        {
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            Assert.True(string.IsNullOrEmpty(sim.Fehlertext), sim.Fehlertext);
            return sim;
        }

        /// <summary>10.2 „Bestandsweg-Gebäude liefert Kältebedarf 0 mit Hinweis" im Lauf, samt Gegenprobe.</summary>
        [Fact]
        public void Im_Lauf_liefert_das_Bestandsweg_Gebaeude_0_mit_Hinweis_und_auf_VDI_6007_Kaeltebedarf()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1040, GEBAEUDE = 10645;          // Tagesbilanz (A15)
            GebaeudeKuehlen(GEBAEUDE, 26.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, true));

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf alt = Bedarf(PROJEKT);
            Assert.True(alt.Kaelteseite.Gerechnet);
            Assert.All(alt.KanaeleDrei().Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Equal(new[] { GEBAEUDE }, alt.Kaelteseite.GebaeudeBestandsweg);
            Assert.Single(protokoll.Hinweise, h => h.Contains("Tagesbilanz (Bestandsweg) liefert keine Kühllast"));

            // Gegenprobe: dasselbe Gebäude auf VDI 6007.
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Gebaeude_Modell = NULL WHERE ID = ?",
                                                  new DbParam("@id", GEBAEUDE)));
            SimulationProtokoll zweiter = SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf vdi = Bedarf(PROJEKT);
            Assert.True(vdi.KanaeleDrei().Kuehlung.Sum() > 0.0);
            Assert.Empty(vdi.Kaelteseite.GebaeudeBestandsweg);
            Assert.DoesNotContain(zweiter.Hinweise, h => h.Contains("Bestandsweg"));
        }

        /// <summary>
        /// 10.2 „Ein Lauf, zwei Reihen" (E21, 3.7): Das Gebäudemodell läuft je Gebäude EINMAL; der
        /// Kühlkanal ist die Summe der Kühlreihen aus denselben Ergebnissen, und die Jahreskälte ist
        /// bitgleich mit dem Kanalbedarf (Probe „Kanalsumme der Kälteseite").
        /// </summary>
        [Fact]
        public void Ein_Lauf_zwei_Reihen_das_Gebaeudemodell_laeuft_je_Gebaeude_einmal()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1039;                                // drei Gebäude, alle VDI 6007
            foreach (int id in new[] { 10642, 10643, 10644 }) GebaeudeKuehlen(id, 25.0, 30.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, true));

            SimulationWaermebedarf sim = Bedarf(PROJEKT);
            Assert.Equal(3, sim.Vdi6007weg.Aufrufe);
            Assert.Equal(3, sim.GebaeudeErgebnisse.Anzahl);
            Assert.Equal(3, sim.Kaelteseite.GekuehlteGebaeude);

            double[] erwartet = new double[8760];
            foreach (GebaeudeModellErgebnis e in sim.GebaeudeErgebnisse.Alle)
            {
                Assert.True(e.KuehlungWirksam);
                for (int h = 0; h < 8760; h++) erwartet[h] = erwartet[h] + e.KuehlbedarfKwh[h];
            }
            Assert.Equal(erwartet, sim.KanaeleDrei().Kuehlung);
            Assert.Equal(SimulationRunner.BedarfJeKanal(sim)[Kanal.KUEHLUNG], sim.Kaelteseite.Kaeltebedarf_Gesamt);
            Assert.True(sim.Kaelteseite.Kaeltebedarf_Gesamt > 0.0);
            Assert.Equal(0, sim.Kaelteseite.Bedarfsprobe_Verletzungen);

            // Der Wärmesummenvektor enthält keine Kälte (F-K4).
            double[] summe = sim.KanaeleDrei().Summe();
            Assert.Equal(summe, sim.Waermebedarf);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "1039 gekühlt (25 °C, 30 kW je Gebäude): Kältebedarf {0:F3} MWh, Spitze {1:F2} kW, {2} h mit Kühlbedarf, " +
                "{3} h mit Heizen und Kühlen ({4}); Wärmebedarf {5:F3} MWh, Wärmespitze {6:F2} kW",
                sim.Kaelteseite.Kaeltebedarf_Gesamt, sim.Kaelteseite.Kaeltebedarf_Max, sim.Kaelteseite.StundenMitKuehlbedarf,
                sim.Kaelteseite.StundenHeizenUndKuehlen, sim.Kaelteseite.StundenHeizenUndKuehlenGebaeude,
                sim.Waermebedarf_Gesamt, sim.Waermebedarf_Max));
        }

        /// <summary>
        /// F-K4 im ganzen Lauf, und 7.4: Ein Projekt MIT Kühlung auf Parität (Kühlsollwert = obere
        /// Raumtemperatur, ohne Grenze) rechnet die Wärmeseite bitgleich wie ohne Kühlung — kein
        /// Wärmeerzeuger deckt Kälte, die Wärmespitze bleibt; die Kältespalten tragen Werte, die
        /// Deckung der Wärmeerzeuger 0, die Speicherzeile bleibt leer.
        /// </summary>
        [Fact]
        public void Mit_Kuehlung_bleibt_die_Waermeseite_bitgleich_und_die_Kaeltespalten_tragen_Werte()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;

            var aus = new SimulationRunner();
            int kopfAus = aus.SimuliereUndSpeichere(PROJEKT, out string fehlerAus);
            Assert.True(kopfAus > 0, fehlerAus);
            Assert.False(aus.simulation_Kaeltebedarf.Gerechnet);
            Dictionary<string, object> energieAus = Zeile("Tab_ErgebnisEnergiebedarf", kopfAus);
            Dictionary<string, object> wpAus = Zeile("Tab_ErgebnisWaermepumpe", kopfAus);

            GebaeudeKuehlen(GEBAEUDE, 24.0);
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, true));
            var an = new SimulationRunner();
            int kopfAn = an.SimuliereUndSpeichere(PROJEKT, out string fehlerAn);
            Assert.True(kopfAn > 0, fehlerAn);
            SimulationKaeltebedarf k = an.simulation_Kaeltebedarf;
            Assert.True(k.Gerechnet);
            Assert.True(k.Kaeltebedarf_Gesamt > 0.0);

            // Wärmeseite bitgleich.
            Assert.Equal(aus.simulation_Waermebedarf.Waermebedarf, an.simulation_Waermebedarf.Waermebedarf);
            Assert.Equal(aus.simulation_Waermebedarf.Waermebedarf_Max, an.simulation_Waermebedarf.Waermebedarf_Max);
            Assert.Equal(aus.sim.Rest_Waermebedarf_stuendlich, an.sim.Rest_Waermebedarf_stuendlich);
            Assert.Equal(aus.sim.RestwaermeMwh, an.sim.RestwaermeMwh);

            // Die Ergebniszeilen: alte Spalten gleich, die Kältespalten mit Wert.
            Dictionary<string, object> energieAn = Zeile("Tab_ErgebnisEnergiebedarf", kopfAn);
            foreach (var kv in energieAus)
            {
                if (kv.Key == "ID" || kv.Key == "ID_Ergebnis") continue;
                if (KuehlungSchema.Ergebnisspalten.Any(s => s.Name == kv.Key))
                {
                    Assert.Equal(DBNull.Value, kv.Value);                 // ohne Kühlung: nicht erhoben
                    Assert.NotEqual(DBNull.Value, energieAn[kv.Key]);     // mit Kühlung: erhoben
                }
                else Assert.Equal(kv.Value, energieAn[kv.Key]);
            }
            double kanal = Convert.ToDouble(energieAn[KuehlungSchema.SPALTE_BEDARF_KUEHLUNG], CultureInfo.InvariantCulture);
            Assert.Equal(kanal, Convert.ToDouble(energieAn[KuehlungSchema.SPALTE_KAELTEBEDARF_GESAMT], CultureInfo.InvariantCulture));
            Assert.Equal(kanal, Convert.ToDouble(energieAn[KuehlungSchema.SPALTE_KAELTERESTBEDARF], CultureInfo.InvariantCulture));
            Assert.Equal(Math.Round(k.Kaeltebedarf_Max, 2, MidpointRounding.AwayFromZero),
                         Convert.ToDouble(energieAn[KuehlungSchema.SPALTE_KAELTELAST_MAX], CultureInfo.InvariantCulture));

            Dictionary<string, object> wpAn = Zeile("Tab_ErgebnisWaermepumpe", kopfAn);
            Assert.Equal(DBNull.Value, wpAus[KuehlungSchema.SPALTE_DECKUNG_KUEHLUNG]);
            Assert.Equal(0.0, Convert.ToDouble(wpAn[KuehlungSchema.SPALTE_DECKUNG_KUEHLUNG], CultureInfo.InvariantCulture));
            foreach (var kv in wpAus)
                if (kv.Key != "ID" && kv.Key != "ID_Ergebnis" && kv.Key != KuehlungSchema.SPALTE_DECKUNG_KUEHLUNG)
                    Assert.Equal(kv.Value, wpAn[kv.Key]);

            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_ErgebnisPufferspeicher WHERE ID_Ergebnis = ? AND Entladung_Kuehlung IS NOT NULL", kopfAn));
            Assert.Contains(an.Protokoll.Warnungen, w => w.StartsWith("Kältebedarf ohne Kälteerzeuger", StringComparison.Ordinal));
        }

        /// <summary>K10: Projekt aus — Gebäude mit Kühleingaben rechnen wie ohne, die Kältespalten bleiben NULL.</summary>
        [Fact]
        public void Projekt_aus_rechnet_nichts_und_laesst_die_Kaeltespalten_leer()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;

            SimulationWaermebedarf vorher = Bedarf(PROJEKT);
            GebaeudeKuehlen(GEBAEUDE, 22.0, 1.0);                    // Projekt bleibt aus
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf nachher = Bedarf(PROJEKT);

            Assert.False(nachher.Kaelteseite.Gerechnet);
            Assert.All(nachher.KanaeleDrei().Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Equal(vorher.Waermebedarf, nachher.Waermebedarf);
            Assert.False(nachher.GebaeudeErgebnisse.Ergebnis(0).KuehlungWirksam);
            Assert.Single(protokoll.Hinweise, h => h.StartsWith("Kühlung: Das Projekt rechnet keine Kälte", StringComparison.Ordinal));

            var laeufer = new SimulationRunner();
            int kopf = laeufer.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);
            foreach (SchemaSpalte s in KuehlungSchema.Ergebnisspalten)
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE ID_Ergebnis = ? AND [" + s.Name + "] IS NOT NULL", kopf));
        }

        /// <summary>K3, F-K5: Ein Lastgang im Kanal Kühlung trägt Kälte — nie Wärme.</summary>
        [Fact]
        public void Ein_Lastgang_im_Kanal_Kuehlung_traegt_Kaelte_und_keine_Waerme()
        {
            if (!_db.Vorhanden) return;
            const int PROJEKT = 1030;                                // nur ein Wärmelastgang
            SimulationWaermebedarf heiz = Bedarf(PROJEKT);
            double lastgang = heiz.Waermebedarf_Extern.Sum();
            Assert.True(lastgang > 0.0);

            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_ProjektWaermebedarf SET Kanal = ? WHERE ID_Projekt = ?",
                                                  new DbParam("@k", DbWerte.KANAL_KUEHLUNG), new DbParam("@p", PROJEKT)));

            // Projekt aus: weder Wärme noch Kälte, und der Grund steht im Protokoll.
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            SimulationWaermebedarf aus = Bedarf(PROJEKT);
            Assert.Equal(0.0, aus.Waermebedarf_Extern_Gesamt);
            Assert.All(aus.KanaeleDrei().Heizung, w => Assert.Equal(0.0, w));
            Assert.All(aus.KanaeleDrei().Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Single(protokoll.Hinweise, h => h.Contains("1 Lastgänge"));

            // Projekt an: der Lastgang ist Kältebedarf.
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, true));
            SimulationWaermebedarf an = Bedarf(PROJEKT);
            Assert.Equal(heiz.Waermebedarf_Extern, an.Kaelteseite.Kaeltebedarf_Extern);
            Assert.Equal(heiz.Waermebedarf_Extern, an.KanaeleDrei().Kuehlung);
            Assert.Equal(lastgang / 1000, an.Kaelteseite.Kaeltebedarf_Extern_Gesamt, 9);
            Assert.All(an.KanaeleDrei().Heizung, w => Assert.Equal(0.0, w));
            Assert.Equal(0.0, an.Waermebedarf_Gesamt);
        }

        private static Dictionary<string, object> Zeile(string tabelle, int kopf)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] WHERE ID_Ergebnis = ?",
                                                       new DbParam("@e", kopf));
            Assert.Equal(1, dt.Rows.Count);
            var d = new Dictionary<string, object>();
            foreach (DataColumn c in dt.Columns) d[c.ColumnName] = dt.Rows[0][c];
            return d;
        }

        private static long Zahl(string sql, int kopf)
        {
            object o = DataRepository.GetDataTable(sql, new DbParam("@e", kopf)).Rows[0][0];
            return Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
