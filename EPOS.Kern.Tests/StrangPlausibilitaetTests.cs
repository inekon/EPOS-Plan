using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die acht Auslegungsprüfungen P1 bis P8</b> der Strangzuordnung
    /// (<c>Konzept_Wechselrichter_EPOS-Plan.md</c> 4.2, Stufe S2, Anwenderentscheid
    /// <b>W6‑E‑2</b> vom 06.09.2026).
    ///
    /// <para><b>Die Messlatte ist ANHANG A des Konzepts</b> — dieselben Zahlen, die im
    /// Mockup stehen: Modul Ablytek 6MN6A275 (275,19 W; U_oc 38,4 V; U_mpp 31,4 V;
    /// I_sc 9,34 A; beta_OC −0,118 V/K; alpha_SC +0,0047 A/K) an einem
    /// Muster 2500TL (2,50 kW; 1 MPPT; MPP 80…500 V; U_dc,max 600 V; I_dc,max 12,0 A),
    /// zehn Module in Reihe. Der Anhang nennt für diesen Fall SECHS grüne Prüfungen und
    /// dazu drei Gegenproben — 14 Module (gelb, P6), 15 Module (rot, P1) und zwei
    /// Stränge parallel (rot, P4). Alle vier stehen hier.</para>
    ///
    /// <para><b>Geprüft werden die ZAHLEN, nicht die Sätze.</b> Ein Prüfstand, der Text
    /// vergleicht, prüft die Sprache. Der Befund trägt die Größen selbst
    /// (<c>UocKalt</c>, <c>UmppHeiss</c>, <c>UmppKalt</c>, <c>Strom</c>,
    /// <c>DcAc</c>), und genau die sind gegen den Anhang nachzurechnen.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche</b> — die Klasse braucht keine
    /// Arbeitskopie und steht deshalb in keiner Sammlung.</para>
    /// </summary>
    public class StrangPlausibilitaetTests
    {
        // =================================================================================
        // 1 - Anhang A: der grüne Fall
        // =================================================================================

        /// <summary>
        /// <b>Der Fall des Mockups: zehn Module in Reihe — alles grün.</b> Die sechs
        /// Zeilen des Anhangs A, Zahl für Zahl.
        /// </summary>
        [Fact]
        public void Anhang_A_zehn_Module_in_Reihe_sind_gruen()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 10);

            StrangPlausibilitaet.Strangbefund s = Assert.Single(b.Straenge);
            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);

            // P1: 10 · [38,4 + (−0,118)·(−35)] = 10 · 42,53 = 425,3 V ≤ 600 V
            Assert.Equal(425.3, s.UocKalt.Value, 6);

            // P2: 10 · [31,4 + (−0,118)·45] = 10 · 26,09 = 260,9 V ≥ 80 V
            Assert.Equal(260.9, s.UmppHeiss.Value, 6);

            // P3: 10 · [31,4 + (−0,118)·(−35)] = 10 · 35,53 = 355,3 V ≤ 500 V
            Assert.Equal(355.3, s.UmppKalt.Value, 6);

            // P4: 1 · [9,34 + 0,0047·45] = 9,5515 A ≤ 12,0 A
            StrangPlausibilitaet.Mpptbefund m = Assert.Single(g.Mppts);
            Assert.Equal(9.5515, m.Strom.Value, 6);
            Assert.Equal(1, m.Straenge);

            // P6: 10 · 275,19 W = 2,7519 kWp / 2,50 kW = 1,10076
            Assert.Equal(2.7519, g.Kwp, 6);
            Assert.Equal(1.10076, g.DcAc.Value, 6);

            // P8: 10 · 1 = 10 = „Anzahl Module"
            Assert.Equal(10, b.Modulsumme);
            Assert.True(b.ModulsummeStimmt);

            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, s.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, g.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, b.Farbe);
        }

        // =================================================================================
        // 2 - Anhang A: die drei Gegenproben
        // =================================================================================

        /// <summary>
        /// <b>Gegenprobe 1 — 14 Module: GELB über P6.</b> `14 · 42,53 = 595,4 V` bleibt
        /// unter 600 V (P1 also grün), aber `14 · 275,19 = 3,853 kWp` gegen 2,50 kW
        /// ist DC/AC 1,54 und damit ausserhalb des Bandes 1,0…1,5.
        /// </summary>
        [Fact]
        public void Anhang_A_vierzehn_Module_sind_gelb_ueber_P6()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 14, parallel: 1, anzahlModuleAnlage: 14);

            StrangPlausibilitaet.Strangbefund s = Assert.Single(b.Straenge);
            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);

            Assert.Equal(595.42, s.UocKalt.Value, 6);           // < 600 -> P1 haelt
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, s.Farbe);

            Assert.Equal(3.85266, g.Kwp, 6);
            Assert.Equal(1.541064, g.DcAc.Value, 6);            // > 1,5 -> P6
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, g.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Farbe);
        }

        /// <summary>
        /// <b>Gegenprobe 2 — 15 Module: ROT über P1.</b> `15 · 42,53 = 637,95 V`
        /// überschreitet die maximale DC-Spannung von 600 V; das Gerät kann bei Frost
        /// und Sonne Schaden nehmen.
        /// </summary>
        [Fact]
        public void Anhang_A_fuenfzehn_Module_sind_rot_ueber_P1()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 15, parallel: 1, anzahlModuleAnlage: 15);

            StrangPlausibilitaet.Strangbefund s = Assert.Single(b.Straenge);

            Assert.Equal(637.95, s.UocKalt.Value, 6);
            Assert.Equal(StrangPlausibilitaet.Ampel.Rot, s.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Rot, b.Farbe);
        }

        /// <summary>
        /// <b>Gegenprobe 3 — zwei Stränge parallel: ROT über P4.</b>
        /// `2 · 9,5515 = 19,103 A` überschreitet den maximalen DC-Strom von 12,0 A
        /// <b>je MPPT</b>.
        /// </summary>
        [Fact]
        public void Anhang_A_zwei_Straenge_parallel_sind_rot_ueber_P4()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 2, anzahlModuleAnlage: 20);

            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);
            StrangPlausibilitaet.Mpptbefund m = Assert.Single(g.Mppts);

            Assert.Equal(2, m.Straenge);
            Assert.Equal(19.103, m.Strom.Value, 6);
            Assert.Equal(StrangPlausibilitaet.Ampel.Rot, g.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Rot, b.Farbe);
        }

        // =================================================================================
        // 3 - Grenzfälle je Prüfung
        // =================================================================================

        /// <summary>
        /// <b>P1 ist eine „kleiner oder gleich"-Prüfung.</b> Genau auf der Grenze bleibt
        /// die Ampel grün — die Auslegungsgrenze ist ein zulässiger Wert.
        /// </summary>
        [Fact]
        public void P1_genau_auf_der_Grenze_bleibt_gruen()
        {
            // 10 Module ergeben 425,3 V; die Grenze wird genau darauf gesetzt.
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 10,
                                                  geraet: Geraet(uDcMax: 425.3));

            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, b.Straenge[0].Farbe);
        }

        /// <summary>
        /// <b>P2 — der Strang regelt im Sommer ab: ROT.</b> Bei nur zwei Modulen liegt
        /// die MPP-Spannung im heissen Fall bei 52,18 V und damit unter dem Fenster.
        /// </summary>
        [Fact]
        public void P2_unter_dem_MPP_Fenster_ist_rot()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 2, parallel: 1, anzahlModuleAnlage: 2);

            Assert.Equal(52.18, b.Straenge[0].UmppHeiss.Value, 6);   // < 80 V
            Assert.Equal(StrangPlausibilitaet.Ampel.Rot, b.Straenge[0].Farbe);
        }

        /// <summary>
        /// <b>P3 — das Gerät regelt an der oberen Grenze: GELB, nicht rot.</b> Der
        /// Strang liefert dann weniger, aber nichts geht kaputt (Konzept 4.2).
        /// </summary>
        [Fact]
        public void P3_ueber_dem_MPP_Fenster_ist_gelb()
        {
            // 14 Module: U_mpp(−10 °C) = 14 · 35,53 = 497,4 V; das Fenster wird auf
            // 480 V gedeckelt, U_dc,max bleibt bei 600 V (P1 haelt).
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 14, parallel: 1, anzahlModuleAnlage: 14,
                                                  geraet: Geraet(uMppMax: 480.0));

            Assert.Equal(497.42, b.Straenge[0].UmppKalt.Value, 6);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Straenge[0].Farbe);
        }

        /// <summary>
        /// <b>P5 — mehr Stränge am MPPT als zulässig: GELB.</b> Zwei parallele Stränge
        /// an einem Gerät, das nur einen führt.
        /// </summary>
        [Fact]
        public void P5_zu_viele_Straenge_je_MPPT_sind_gelb()
        {
            // Der Strom bleibt unter der Grenze (I_dc,max grosszuegig), damit P4 nicht
            // dazwischenfunkt und allein P5 die Farbe setzt.
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 2, anzahlModuleAnlage: 20,
                                                  geraet: Geraet(iDcMax: 30.0, straengeJeMppt: 1,
                                                                 pAcNenn: 5.0));

            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);
            Assert.Equal(2, g.Mppts[0].Straenge);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, g.Farbe);
        }

        /// <summary>
        /// <b>P6 unterhalb des Bandes: GELB.</b> Eine zu klein ausgelegte
        /// Modulfläche an einem grossen Gerät ist ebenso ein Hinweis wie eine zu
        /// grosse — das Band gilt in BEIDE Richtungen (Konzept 4.2).
        /// </summary>
        [Fact]
        public void P6_unter_dem_Band_ist_ebenfalls_gelb()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 10,
                                                  geraet: Geraet(pAcNenn: 5.0));

            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);
            Assert.Equal(0.55038, g.DcAc.Value, 6);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, g.Farbe);
        }

        /// <summary>
        /// <b>P7 — über der DC-Eingangsgrenze: GELB.</b>
        /// </summary>
        [Fact]
        public void P7_ueber_der_DC_Grenze_ist_gelb()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 10,
                                                  geraet: Geraet(pDcMax: 2.6));

            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, Assert.Single(b.Geraete).Farbe);
        }

        /// <summary>
        /// <b>P8 — die Modulsumme weicht vom Anlagenwert ab: GELB</b>, und der Hinweis
        /// steht am ersten Strang, wo der Anwender ihn liest.
        /// </summary>
        [Fact]
        public void P8_eine_abweichende_Modulsumme_ist_gelb()
        {
            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 12);

            Assert.Equal(10, b.Modulsumme);
            Assert.False(b.ModulsummeStimmt);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Straenge[0].Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Farbe);
        }

        // =================================================================================
        // 4 - Fehlende Werte
        // =================================================================================

        /// <summary>
        /// <b>Ein fehlender Modulwert macht GELB, nicht rot und nicht grün</b>
        /// (Konzept 4.2, offener Punkt W6‑O‑2). Die Prüfung entfällt, der Befund führt
        /// dann keinen Wert, und der Satz sagt, welche Angabe fehlt.
        /// </summary>
        [Fact]
        public void Ein_fehlender_Modulwert_macht_gelb_und_nicht_pruefbar()
        {
            PhotovoltaikModel modul = Modul();
            modul.m_beta_OC = 0;                       // 0 heisst hier "nicht gepflegt"

            StrangPlausibilitaet.Befund b = Pruefe(reihe: 10, parallel: 1, anzahlModuleAnlage: 10,
                                                   modul: modul);

            Assert.Null(b.Straenge[0].UocKalt);
            Assert.Null(b.Straenge[0].UmppHeiss);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Straenge[0].Farbe);
            Assert.Contains("beta_OC", b.Straenge[0].Satz, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Fehlt die MPPT-Zahl</b> — die CEC-Liste führt sie nicht (W6‑O‑2) —, wird
        /// auf EINEM Tracker gerechnet, dem konservativen Fall, und der Satz sagt es.
        /// Zwei Stränge auf verschiedenen MPPT-Nummern landen dann auf demselben
        /// Tracker und summieren ihren Strom.
        /// </summary>
        [Fact]
        public void Ohne_MPPT_Zahl_rechnet_die_Pruefung_auf_einem_Tracker()
        {
            // Der Satz kommt aus den Ressourcen der laufenden UI-Kultur; der Windows-Laeufer
            // der CI steht auf en-US und lieferte "MPP trackers" statt "MPP-Tracker"
            // (CI 06.09.2026, dreimal rot). Muster LizenzTokenTests: Kultur pinnen und im
            // finally zuruecklegen.
            var kulturVorher = System.Globalization.CultureInfo.CurrentUICulture;
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");
            try
            {
                var geraet = Geraet(anzahlMppt: null);

                StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                    new StrangPlausibilitaet.Gaben
                    {
                        Straenge = new List<AnlageStrangModel>
                        {
                            new AnlageStrangModel { Rang = 1, ID_Wechselrichter = GERAET_ID,
                                                    Mppt = 1, Module_Reihe = 10 },
                            new AnlageStrangModel { Rang = 2, ID_Wechselrichter = GERAET_ID,
                                                    Mppt = 2, Module_Reihe = 10 }
                        },
                        Modul = Modul(),
                        Geraete = new Dictionary<int, WechselrichterModel> { { GERAET_ID, geraet } },
                        AnzahlModuleAnlage = 20
                    });

                StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);
                StrangPlausibilitaet.Mpptbefund m = Assert.Single(g.Mppts);   // EIN Tracker
                Assert.Equal(2, m.Straenge);
                Assert.Equal(19.103, m.Strom.Value, 6);                       // > 12,0 A -> P4 rot
                Assert.Equal(StrangPlausibilitaet.Ampel.Rot, g.Farbe);
                Assert.Contains("MPP-Tracker", g.Satz, StringComparison.Ordinal);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentUICulture = kulturVorher;
            }
        }

        /// <summary>
        /// <b>Ein Strang ohne Gerät</b> ist gelb, nicht rot: Er steht in der Tabelle,
        /// rechnet aber nicht mit, und die Ampel sagt genau das.
        /// </summary>
        [Fact]
        public void Ein_Strang_ohne_Geraet_ist_gelb()
        {
            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel>
                    {
                        new AnlageStrangModel { Rang = 1, Module_Reihe = 10 }
                    },
                    Modul = Modul(),
                    Geraete = new Dictionary<int, WechselrichterModel>(),
                    AnzahlModuleAnlage = 10
                });

            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, b.Farbe);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gelb, Assert.Single(b.Geraete).Farbe);
        }

        // =================================================================================
        // 5 - Ost/West: ein Gerät, zwei MPPT
        // =================================================================================

        /// <summary>
        /// <b>Der Fall, für den die Stufe gebaut wird:</b> zwei Stränge mit eigener
        /// Ausrichtung an EINEM Gerät und zwei Trackern. Jeder Tracker führt seinen
        /// eigenen Strom, das DC/AC-Verhältnis gilt für beide zusammen — eine
        /// Clipping-Grenze über beiden Dachhälften.
        /// </summary>
        [Fact]
        public void Ost_West_ist_ein_Geraet_mit_zwei_Trackern()
        {
            var geraet = Geraet(pAcNenn: 5.0, anzahlMppt: 2, uMppMax: 800.0, uDcMax: 800.0);

            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel>
                    {
                        new AnlageStrangModel { Rang = 1, Bezeichner = "Dach Ost",
                                                ID_Wechselrichter = GERAET_ID, Geraetenummer = 1,
                                                Mppt = 1, Module_Reihe = 11, Azimut = -90 },
                        new AnlageStrangModel { Rang = 2, Bezeichner = "Dach West",
                                                ID_Wechselrichter = GERAET_ID, Geraetenummer = 1,
                                                Mppt = 2, Module_Reihe = 11, Azimut = 90 }
                    },
                    Modul = Modul(),
                    Geraete = new Dictionary<int, WechselrichterModel> { { GERAET_ID, geraet } },
                    AnzahlModuleAnlage = 22
                });

            StrangPlausibilitaet.Geraetebefund g = Assert.Single(b.Geraete);
            Assert.Equal(2, g.Mppts.Count);
            Assert.All(g.Mppts, m => Assert.Equal(9.5515, m.Strom.Value, 6));

            // 22 · 275,19 W = 6,05418 kWp auf 5,00 kW = 1,21
            Assert.Equal(6.05418, g.Kwp, 6);
            Assert.Equal(1.210836, g.DcAc.Value, 6);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, b.Farbe);
            Assert.Equal(22, b.Modulsumme);
        }

        /// <summary>
        /// Zwei GERÄTE derselben Bauart sind zwei Befunde — die Gruppierung läuft über
        /// (Wechselrichter, Gerätenummer), und daran hängt das Clipping je Gerät
        /// (Konzept 3.4, Q6).
        /// </summary>
        [Fact]
        public void Zwei_Geraetenummern_sind_zwei_Befunde()
        {
            var geraet = Geraet();

            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel>
                    {
                        new AnlageStrangModel { Rang = 1, ID_Wechselrichter = GERAET_ID,
                                                Geraetenummer = 1, Module_Reihe = 10 },
                        new AnlageStrangModel { Rang = 2, ID_Wechselrichter = GERAET_ID,
                                                Geraetenummer = 2, Module_Reihe = 10 }
                    },
                    Modul = Modul(),
                    Geraete = new Dictionary<int, WechselrichterModel> { { GERAET_ID, geraet } },
                    AnzahlModuleAnlage = 20
                });

            Assert.Equal(2, b.Geraete.Count);
            Assert.Equal(new[] { 1, 2 }, b.Geraete.Select(g => g.Geraetenummer).ToArray());
            Assert.All(b.Geraete, g => Assert.Equal(1.10076, g.DcAc.Value, 6));
        }

        /// <summary>
        /// <b>Ohne Strangzeile ist der Befund leer und grün</b> — dann rechnet die
        /// Anlage wie bisher, und es gibt nichts zu melden.
        /// </summary>
        [Fact]
        public void Ohne_Strang_ist_der_Befund_leer_und_gruen()
        {
            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = new List<AnlageStrangModel>(),
                    Modul = Modul(),
                    AnzahlModuleAnlage = 10
                });

            Assert.Empty(b.Straenge);
            Assert.Empty(b.Geraete);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, b.Farbe);
            Assert.True(b.ModulsummeStimmt);
        }

        /// <summary>
        /// <c>Pruefe(null)</c> wirft nicht — der Dialog ruft den Prüfstand auch, bevor
        /// er etwas geladen hat.
        /// </summary>
        [Fact]
        public void Ohne_Gaben_wirft_die_Pruefung_nicht()
        {
            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(null);

            Assert.Empty(b.Straenge);
            Assert.Equal(StrangPlausibilitaet.Ampel.Gruen, b.Farbe);
        }

        // =================================================================================
        // Der Prüfaufbau — Anhang A des Konzepts
        // =================================================================================

        private const int GERAET_ID = 4711;

        /// <summary>
        /// Das Modul <b>Ablytek 6MN6A275</b> mit den Katalogwerten des Anhangs A.
        /// </summary>
        private static PhotovoltaikModel Modul()
        {
            return new PhotovoltaikModel
            {
                m_szName = "Ablytek 6MN6A275",
                m_Leistung = 275.19,
                m_U_Leerlauf = 38.4,
                m_U_Mpp = 31.4,
                m_I_Kurzschluss = 9.34,
                m_beta_OC = -0.118,
                m_alpha_SC = 0.0047
            };
        }

        /// <summary>
        /// Das Gerät <b>Muster 2500TL</b> des Anhangs A; jeder Wert ist einzeln
        /// austauschbar, damit ein Grenzfall genau EINE Prüfung anspricht.
        /// </summary>
        private static WechselrichterModel Geraet(double pAcNenn = 2.5,
                                                  double uMppMin = 80.0,
                                                  double uMppMax = 500.0,
                                                  double uDcMax = 600.0,
                                                  double iDcMax = 12.0,
                                                  int? anzahlMppt = 1,
                                                  int? straengeJeMppt = null,
                                                  double? pDcMax = null)
        {
            return new WechselrichterModel
            {
                m_ID = GERAET_ID,
                m_szName = "Muster 2500TL",
                m_P_AC_Nenn = pAcNenn,
                m_U_Mpp_Min = uMppMin,
                m_U_Mpp_Max = uMppMax,
                m_U_Dc_Max = uDcMax,
                m_I_Dc_Max = iDcMax,
                m_Anzahl_Mppt = anzahlMppt,
                m_Straenge_Je_Mppt = straengeJeMppt,
                m_P_DC_Max = pDcMax
            };
        }

        private static StrangPlausibilitaet.Befund Pruefe(int reihe, int parallel,
                                                          double anzahlModuleAnlage,
                                                          WechselrichterModel geraet = null,
                                                          PhotovoltaikModel modul = null)
        {
            WechselrichterModel g = geraet ?? Geraet();

            return StrangPlausibilitaet.Pruefe(new StrangPlausibilitaet.Gaben
            {
                Straenge = new List<AnlageStrangModel>
                {
                    new AnlageStrangModel
                    {
                        Rang = 1,
                        ID_Wechselrichter = GERAET_ID,
                        Module_Reihe = reihe,
                        Straenge_Parallel = parallel
                    }
                },
                Modul = modul ?? Modul(),
                Geraete = new Dictionary<int, WechselrichterModel> { { GERAET_ID, g } },
                AnzahlModuleAnlage = anzahlModuleAnlage
            });
        }
    }
}
