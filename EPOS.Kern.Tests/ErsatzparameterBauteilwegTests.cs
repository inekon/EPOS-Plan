using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Werkzeug der Proben des Bauteilwegs: ein Bauteilsatz OHNE Schichten, der die fünf
    /// U/A-Gruppen eines Gebäudes wiedergibt (Mehrzonenkonzept 3.6) — Außenwand und Sonstiges je
    /// in vier Viertel Nord/Ost/Süd/West, das Dach waagerecht, die Grundfläche als Bodenplatte an
    /// ihrer Randbedingung, die Fenster je Richtung (die Restdifferenz zur gesamten Fensterfläche
    /// auf Süd), Σψ·L zu gleichen Teilen auf die Wandviertel.
    /// </summary>
    internal static class BauteilwegProbe
    {
        internal static List<BauteilEingang> AusGebaeude(GebaeudeModellEingang e)
        {
            var liste = new List<BauteilEingang>();
            double[] azimute = { 0.0, 90.0, 180.0, 270.0 };
            string[] namen = { "N", "O", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                if (e.A_Aussenwand_M2 > 0.0)
                    liste.Add(new BauteilEingang("Außenwand " + namen[i], Bauteilart.Aussenwand, e.A_Aussenwand_M2 / 4.0,
                        Bauteilrand.Aussenluft, e.U_Aussenwand, azimutGrad: azimute[i], psiL_WK: e.SummePsiL_WK / 4.0));
            }
            for (int i = 0; i < 4; i++)
            {
                if (e.A_Sonstige_M2 > 0.0)
                    liste.Add(new BauteilEingang("Sonstiges " + namen[i], Bauteilart.Sonstiges, e.A_Sonstige_M2 / 4.0,
                        Bauteilrand.Aussenluft, e.U_Sonstige, azimutGrad: azimute[i]));
            }
            if (e.A_Dach_M2 > 0.0)
                liste.Add(new BauteilEingang("Dach", Bauteilart.Dach, e.A_Dach_M2, Bauteilrand.Aussenluft, e.U_Dach));
            if (e.A_Grund_M2 > 0.0)
            {
                Bauteilrand rand = e.GrundRandbedingung == DbWerte.GRUND_KELLER ? Bauteilrand.Unbeheizt
                                 : e.GrundRandbedingung == DbWerte.GRUND_AUSSENLUFT ? Bauteilrand.Aussenluft
                                 : Bauteilrand.Erdreich;
                liste.Add(new BauteilEingang("Grund", Bauteilart.Bodenplatte, e.A_Grund_M2, rand, e.U_Grund));
            }
            if (e.A_Fenster_M2 > 0.0)
            {
                double[] flaechen = { e.A_FensterNord_M2, e.A_FensterOst_M2, e.A_FensterSued_M2, e.A_FensterWest_M2 };
                flaechen[2] += e.A_Fenster_M2 - flaechen.Sum();
                for (int i = 0; i < 4; i++)
                {
                    if (flaechen[i] > 0.0)
                        liste.Add(new BauteilEingang("Fenster " + namen[i], Bauteilart.Fenster, flaechen[i], Bauteilrand.Aussenluft,
                            e.U_Fenster, azimutGrad: azimute[i], gWert: e.GWert, rahmenanteil: e.Rahmenanteil,
                            verschattungsfaktor: e.Verschattungsfaktor));
                }
            }
            return liste;
        }

        /// <summary>
        /// Hält jedes double-Feld zweier Parametersätze gegeneinander (relativ, bei null absolut);
        /// gibt die größte relative Abweichung zurück und den Namen ihres Felds.
        /// </summary>
        internal static (double Abweichung, string Feld) Vergleich(ErsatzparameterRC a, ErsatzparameterRC b)
        {
            double groesste = 0.0;
            string feld = null;
            foreach (PropertyInfo p in typeof(ErsatzparameterRC).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (p.PropertyType != typeof(double)) continue;
                double x = (double)p.GetValue(a), y = (double)p.GetValue(b);
                double d;
                if (x.Equals(y)) d = 0.0;
                else if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) d = double.PositiveInfinity;
                else d = Math.Abs(x - y) / Math.Max(Math.Abs(x), Math.Abs(y));
                if (feld == null || d > groesste)
                {
                    groesste = d;
                    feld = p.Name;
                }
            }
            return (groesste, feld);
        }
    }

    /// <summary>
    /// <b>Stufe G3 — der Bauteilweg im Parametersatz</b>
    /// (<see cref="ErsatzparameterRC.AusBauteilweg(GebaeudeModellEingang, IReadOnlyList{BauteilEingang})"/>;
    /// Mehrzonenkonzept 2.2, 3.1–3.6): Grenzfall Bauteilweg = Klassenweg, Gruppenbildung, Normweg
    /// der Außenwände mit vollem U·A in Gl. (27), die gemischten Gruppen, der Vorrang des
    /// eingetragenen U-Werts mit Hinweis, die benannten Fehler und der Klimaweg für beliebige
    /// Flächen. Alle Aufbauten sind erfunden.
    /// </summary>
    public class ErsatzparameterBauteilwegTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public ErsatzparameterBauteilwegTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        private static readonly Schicht Beton = new Schicht(0.20, 2.0, 2400.0, 1000.0);
        private static readonly Schicht Daemmung = new Schicht(0.12, 0.035, 30.0, 1400.0);
        private static readonly Schicht Putz = new Schicht(0.015, 0.7, 1400.0, 1000.0);
        private static readonly Schicht Mauerwerk = new Schicht(0.115, 0.5, 1200.0, 1000.0);

        private static GebaeudeModellFehler Grund(Action a) => Assert.Throws<GebaeudeModellException>(a).Grund;

        private static BauteilwegGebaeude Gebaeude(double lueftung = 50.0)
            => new BauteilwegGebaeude("Probe", 100.0, 5000.0, 0.3, 2.5, lueftung);

        private static BauteilEingang Wand(string name, double flaeche, double azimut = 180.0, double u = double.NaN)
            => new BauteilEingang(name, Bauteilart.Aussenwand, flaeche, Bauteilrand.Aussenluft, u,
                                  new[] { Putz, Beton, Daemmung }, azimutGrad: azimut);

        private static BauteilEingang Fenster(double flaeche, double u = 1.3)
            => new BauteilEingang("Fenster", Bauteilart.Fenster, flaeche, Bauteilrand.Aussenluft, u, azimutGrad: 180.0, gWert: 0.6);

        private static BauteilEingang Innenwand(string name, double flaeche)
            => new BauteilEingang(name, Bauteilart.Innenwand, flaeche, Bauteilrand.Innen, schichten: new[] { Putz, Mauerwerk, Putz });

        // =====================================================================
        //  Grenzfall: Bauteilweg ohne Schichten = Klassenweg
        // =====================================================================

        [Fact]
        public void Ohne_Schichten_gleicht_der_Bauteilweg_dem_Klassenweg_in_jedem_Feld()
        {
            GebaeudeModellEingang e = GebaeudeModellEingang.Daten(Vdi6007Probe.Gebaeude());
            ErsatzparameterRC klasse = ErsatzparameterRC.AusKlassenweg(e);
            List<BauteilEingang> satz = BauteilwegProbe.AusGebaeude(e);
            ErsatzparameterRC bauteil = ErsatzparameterRC.AusBauteilweg(e, satz);

            (double abweichung, string feld) = BauteilwegProbe.Vergleich(klasse, bauteil);
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} Bauteile, größte relative Abweichung {1:E2} ({2})",
                                             satz.Count, abweichung, feld));
            Assert.True(abweichung <= 1e-12, "größte relative Abweichung " + abweichung.ToString("E2", CultureInfo.InvariantCulture) + " in " + feld);
            Assert.Equal(klasse.Gruppenfall, bauteil.Gruppenfall);
            Assert.Equal(klasse.R_1_Untergrenze28c, bauteil.R_1_Untergrenze28c);
            Assert.Equal(Gruppenweg.Klassenweg, bauteil.WegAussen);
            Assert.Equal(Gruppenweg.Klassenweg, bauteil.WegInnen);
            Assert.Equal(satz.Count, bauteil.Bauteilherleitung.Count);
            Assert.Empty(klasse.Bauteilherleitung);

            // Auch mit Keller unter der Grundfläche und ohne Wärmebrücken.
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER;
            g.Abmessung_Anschluß_Fenster_Wand = 0.0;
            g.Abmessung_Anschluß_Wand_Dach = 0.0;
            g.Abmessung_Anschluß_Außenwand_Kellerdecke = 0.0;
            e = GebaeudeModellEingang.Daten(g);
            (abweichung, feld) = BauteilwegProbe.Vergleich(ErsatzparameterRC.AusKlassenweg(e), ErsatzparameterRC.AusBauteilweg(e, BauteilwegProbe.AusGebaeude(e)));
            Assert.True(abweichung <= 1e-12, "Keller: größte relative Abweichung " + abweichung.ToString("E2", CultureInfo.InvariantCulture) + " in " + feld);
        }

        // =====================================================================
        //  Bauteilweg mit Schichten
        // =====================================================================

        [Fact]
        public void Mit_Schichten_reduziert_der_Bauteilweg_je_Gruppe_und_die_Wand_geht_mit_vollem_UA_in_Gl_27()
        {
            var satz = new List<BauteilEingang>
            {
                Wand("Wand S", 30.0, 180.0), Wand("Wand N", 30.0, 0.0), Fenster(12.0),
                new BauteilEingang("Dach", Bauteilart.Dach, 100.0, Bauteilrand.Aussenluft,
                                   schichten: new[] { Beton, new Schicht(0.2, 0.04, 30.0, 1400.0) }),
                Innenwand("Innenwand", 120.0),
                new BauteilEingang("Decke", Bauteilart.Decke, 100.0, Bauteilrand.Innen, schichten: new[] { Beton }),
            };
            ErsatzparameterRC p = ErsatzparameterRC.AusBauteilweg(Gebaeude(), satz);

            Assert.Equal(Gruppenweg.Bauteilweg, p.WegAussen);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegInnen);
            Assert.Equal(160.0, p.A_AW_opak_M2, 12);
            Assert.Equal(12.0, p.A_Fenster_M2, 12);
            Assert.Equal(220.0, p.A_IW_M2, 12);

            // Die Kapazitäten sind die der Parallelschaltung: außen mit C₁,korr, innen mit C₁.
            var aw = new List<(double, double)>();
            foreach (BauteilEingang b in satz.Where(b => b.Gruppe == Bauteilgruppe.Aussen))
            {
                Bauteilkennwerte k = Bauteilreduktion.BezugsperiodeWaehlen(b.Schichten, b.Flaeche_M2,
                    Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad)).Kennwerte;
                aw.Add((k.R1_KW, k.C1korr_Jk));
            }
            (double r1Aw, double cAw) = Bauteilreduktion.Parallel(aw);
            Assert.Equal(r1Aw, p.R_1_AW_KW);
            Assert.Equal(cAw, p.C_AW_Jk);
            var iw = satz.Where(b => b.Gruppe == Bauteilgruppe.Innen)
                         .Select(b => Bauteilreduktion.BezugsperiodeWaehlen(b.Schichten, b.Flaeche_M2,
                             Bauteilreduktion.RichtungAusNeigung(b.NeigungWirksamGrad)).Kennwerte)
                         .Select(k => (k.R1_KW, k.C1_Jk)).ToList();
            (double r1Iw, double cIw) = Bauteilreduktion.Parallel(iw);
            Assert.Equal(r1Iw, p.R_1_IW_KW);
            Assert.Equal(cIw, p.C_IW_Jk);

            // U aus Schichten mit den Bemessungswerten (Wand 0,13/0,04, Dach aufwärts 0,10/0,04).
            double uWand = Bauteilreduktion.UWertAusSchichten(new[] { Putz, Beton, Daemmung }, 90.0, Bauteilrand.Aussenluft).U_WM2K;
            double uDach = Bauteilreduktion.UWertAusSchichten(satz[3].Schichten, 0.0, Bauteilrand.Aussenluft).U_WM2K;
            double uaOpak = uWand * 30.0 + uWand * 30.0 + uDach * 100.0;
            Assert.Equal(uaOpak, p.SummeUA_opak_WK, 12);

            // Gl. (27): der Gesamtwiderstand der Gruppe ist 1/Σ(U·A) aller Wände und Fenster.
            Assert.Equal(1.0 / (uaOpak + 1.3 * 12.0), p.R_ges_AWGruppe_KW, 15);
            Assert.Equal(AussenbauteilgruppeFall.Regelfall, p.Gruppenfall);
            // Der Wandzweig allein trägt sein volles U·A (Normweg, ohne R_si-Abzug).
            double zweig = p.R_1_AW_KW + p.R_Rest_AW_KW + p.R_alphaInnen_KW * p.A_AW_gesamt_M2 / p.A_AW_opak_M2;
            Assert.Equal(1.0 / uaOpak, zweig, 15);

            // Übergänge: α_kon,i der Vorgabe je Fläche, R_ext = Lüftung (keine Wärmebrücken).
            Assert.Equal(1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * 172.0), p.R_conv_AW_KW, 15);
            Assert.Equal(1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * 220.0), p.R_conv_IW_KW, 15);
            Assert.Equal(1.0 / (GebaeudeFestwerte.ALPHA_STR_INNEN * 172.0), p.R_rad_KW, 15);
            Assert.Equal(1.0 / 50.0, p.R_ext_KW, 15);

            // Die Herleitung weist je Bauteil Periode und U-Wert aus.
            Assert.Equal(6, p.Bauteilherleitung.Count);
            BauteilHerleitung h = p.Bauteilherleitung.Single(x => x.Bezeichnung == "Wand S");
            Assert.Equal(GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D, h.Bezugsperiode_d);
            Assert.Equal(uWand, h.UGerechnet_WM2K, 15);
            Assert.False(h.UAbweichungHinweis);
            Assert.Null(h.Hinweis);
        }

        [Fact]
        public void Ein_eingetragener_U_Wert_gilt_vorrangig_und_weist_ueber_10_Prozent_einen_Hinweis_aus()
        {
            double uGerechnet = Bauteilreduktion.UWertAusSchichten(new[] { Putz, Beton, Daemmung }, 90.0, Bauteilrand.Aussenluft).U_WM2K;
            var satz = new List<BauteilEingang> { Wand("Wand", 50.0, u: 1.2 * uGerechnet), Innenwand("Innenwand", 80.0) };
            ErsatzparameterRC p = ErsatzparameterRC.AusBauteilweg(Gebaeude(), satz);

            Assert.Equal(1.2 * uGerechnet * 50.0, p.SummeUA_opak_WK, 12);
            BauteilHerleitung h = p.Bauteilherleitung[0];
            Assert.True(h.UAbweichungHinweis);
            Assert.Contains("Wand", h.Hinweis);

            // Knapp unter 10 %: kein Hinweis.
            satz[0] = Wand("Wand", 50.0, u: 1.09 * uGerechnet);
            Assert.False(ErsatzparameterRC.AusBauteilweg(Gebaeude(), satz).Bauteilherleitung[0].UAbweichungHinweis);
        }

        /// <summary>
        /// Gemischte Außengruppe: ein masseloses opakes Bauteil (Tür ohne Schichten) geht wie ein
        /// Fenster ein — R₁ = R/6 mit R aus Gl. (26), reell parallel nach den Wänden, ohne
        /// Kapazität, mit vollem U·A in Gl. (27). Gemischte Innengruppe: ein masseloses
        /// Innenbauteil trägt nur Fläche.
        /// </summary>
        [Fact]
        public void Gemischte_Gruppen_folgen_den_benannten_EPOS_Regeln()
        {
            var tuer = new BauteilEingang("Tür", Bauteilart.Tuer, 2.0, Bauteilrand.Aussenluft, 1.8, azimutGrad: 90.0);
            var leichtwand = new BauteilEingang("Leichtwand", Bauteilart.Innenwand, 30.0, Bauteilrand.Innen);
            var mitTuer = new List<BauteilEingang> { Wand("Wand", 40.0), tuer, Innenwand("Innenwand", 80.0), leichtwand };
            var ohneTuer = new List<BauteilEingang> { Wand("Wand", 40.0), Innenwand("Innenwand", 80.0) };

            ErsatzparameterRC p = ErsatzparameterRC.AusBauteilweg(Gebaeude(), mitTuer);
            ErsatzparameterRC q = ErsatzparameterRC.AusBauteilweg(Gebaeude(), ohneTuer);

            double rTuer = (1.0 / 1.8 - GebaeudeFestwerte.R_SI - 1.0 / GebaeudeFestwerte.ALPHA_AUSSEN) / 2.0;
            Assert.Equal(1.0 / (1.0 / q.R_1_AW_KW + 6.0 / rTuer), p.R_1_AW_KW, 15);
            Assert.Equal(q.C_AW_Jk, p.C_AW_Jk);                 // die Tür trägt keine Kapazität
            Assert.Equal(q.SummeUA_opak_WK + 1.8 * 2.0, p.SummeUA_opak_WK, 12);
            Assert.Equal(1.0 / p.SummeUA_opak_WK, p.R_ges_AWGruppe_KW, 15);
            Assert.True(p.Bauteilherleitung.Single(h => h.Bezeichnung == "Tür").Masselos);

            // Die Leichtwand: Fläche ja, R₁ und C nein.
            Assert.Equal(110.0, p.A_IW_M2, 12);
            Assert.Equal(q.R_1_IW_KW, p.R_1_IW_KW);
            Assert.Equal(q.C_IW_Jk, p.C_IW_Jk);
            Assert.Equal(1.0 / (GebaeudeFestwerte.ALPHA_KON_INNEN * 110.0), p.R_conv_IW_KW, 15);

            // Ein Außenbauteil nur aus ruhender Luft ist masselos und rechnet mit R aus den Schichten.
            var luftwand = new BauteilEingang("Luftwand", Bauteilart.Sonstiges, 5.0, Bauteilrand.Aussenluft,
                                              schichten: new[] { Schicht.RuhendeLuft(0.1) }, azimutGrad: 270.0);
            ErsatzparameterRC l = ErsatzparameterRC.AusBauteilweg(Gebaeude(), new List<BauteilEingang> { Wand("Wand", 40.0), luftwand, Innenwand("Innenwand", 80.0) });
            Assert.Equal(1.0 / (1.0 / q.R_1_AW_KW + 6.0 / (0.18 / 5.0)), l.R_1_AW_KW, 15);
        }

        [Fact]
        public void Innenbauteile_fehlen_ganz_dann_gilt_der_Klassenweg_der_Innengruppe()
        {
            BauteilwegGebaeude g = Gebaeude();
            ErsatzparameterRC p = ErsatzparameterRC.AusBauteilweg(g, new List<BauteilEingang> { Wand("Wand", 40.0) });
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegAussen);
            Assert.Equal(Gruppenweg.Klassenweg, p.WegInnen);
            Assert.Equal(g.Innenflaechenfaktor * g.Nutzflaeche_M2, p.A_IW_M2);
            Assert.Equal(1.0 / (GebaeudeFestwerte.H_MS * p.A_IW_M2), p.R_1_IW_KW);
            Assert.Equal((1.0 - g.MasseanteilAussen) * (g.Bauweise_WhK * GebaeudeFestwerte.SEKUNDEN_JE_STUNDE), p.C_IW_Jk);

            // Ohne Bauweise braucht der Klassenweg der Innengruppe eine — benannt.
            BauteilwegGebaeude ohne = g with { Bauweise_WhK = double.NaN };
            Assert.Equal(GebaeudeModellFehler.BauweiseUnplausibel,
                Grund(() => ErsatzparameterRC.AusBauteilweg(ohne, new List<BauteilEingang> { Wand("Wand", 40.0) })));
            // Tragen beide Gruppen Schichten, braucht der Bauteilweg keine Bauweise.
            ErsatzparameterRC.AusBauteilweg(ohne, new List<BauteilEingang> { Wand("Wand", 40.0), Innenwand("Innenwand", 80.0) });
        }

        // =====================================================================
        //  Benannte Fehler
        // =====================================================================

        [Fact]
        public void Fehlerfaelle_des_Bauteilwegs_sind_benannt()
        {
            List<BauteilEingang> Satz(BauteilEingang b) => new List<BauteilEingang> { Wand("Wand", 40.0), b };
            ErsatzparameterRC Rechne(BauteilEingang b) => ErsatzparameterRC.AusBauteilweg(Gebaeude(), Satz(b));

            // Wand ohne Azimut an Außenluft; das Dach (0°) und die Bodenplatte (180°) dürfen ohne.
            Assert.Equal(GebaeudeModellFehler.AzimutFehlt,
                Grund(() => Rechne(new BauteilEingang("Wand ohne Azimut", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft, 0.5))));
            Assert.Equal(GebaeudeModellFehler.AzimutFehlt,
                Grund(() => Rechne(new BauteilEingang("Schräges Dach", Bauteilart.Dach, 10.0, Bauteilrand.Aussenluft, 0.5, neigungGrad: 30.0))));
            Rechne(new BauteilEingang("Dach", Bauteilart.Dach, 10.0, Bauteilrand.Aussenluft, 0.5));
            Rechne(new BauteilEingang("Boden", Bauteilart.Bodenplatte, 10.0, Bauteilrand.Erdreich, 0.5));

            // Nachbarzone: nur im Mehrzonenweg (Zonenschleife, G6b); der Einzonenweg lehnt sie benannt ab.
            Assert.Equal(GebaeudeModellFehler.RandbedingungNichtAbgebildet,
                Grund(() => Rechne(new BauteilEingang("Trennwand", Bauteilart.Innenwand, 10.0, Bauteilrand.Zone, 0.5))));

            // λ ≤ 0 in einer Schicht.
            Assert.Equal(GebaeudeModellFehler.SchichtUngueltig,
                Grund(() => Rechne(new BauteilEingang("Wand λ 0", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft,
                                                      schichten: new[] { new Schicht(0.2, 0.0, 2000.0, 1000.0) }, azimutGrad: 90.0))));

            // Weder U-Wert noch Schichten; Fenster mit Schichten oder im Inneren; g außerhalb.
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("Ohne U", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft, azimutGrad: 90.0))));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("Fenster mit Aufbau", Bauteilart.Fenster, 2.0, Bauteilrand.Aussenluft, 1.3,
                                                      new[] { Putz }, azimutGrad: 90.0, gWert: 0.6))));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("Innenfenster", Bauteilart.Fenster, 2.0, Bauteilrand.Innen, 1.3, gWert: 0.6))));
            Assert.Equal(GebaeudeModellFehler.GWertUnplausibel,
                Grund(() => Rechne(new BauteilEingang("Fenster ohne g", Bauteilart.Fenster, 2.0, Bauteilrand.Aussenluft, 1.3, azimutGrad: 90.0))));
            Assert.Equal(GebaeudeModellFehler.UWertUnplausibel,
                Grund(() => Rechne(new BauteilEingang("Fenster U 9", Bauteilart.Fenster, 2.0, Bauteilrand.Aussenluft, 9.0, azimutGrad: 90.0, gWert: 0.6))));
            Assert.Equal(GebaeudeModellFehler.FensterzweigUngueltig,
                Grund(() => Rechne(new BauteilEingang("Fenster U 5,9", Bauteilart.Fenster, 2.0, Bauteilrand.Aussenluft, 5.9, azimutGrad: 90.0, gWert: 0.6))));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("Fläche 0", Bauteilart.Aussenwand, 0.0, Bauteilrand.Aussenluft, 0.5, azimutGrad: 90.0))));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("Neigung 200", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft, 0.5, neigungGrad: 200.0, azimutGrad: 90.0))));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => Rechne(new BauteilEingang("α 0", Bauteilart.Aussenwand, 10.0, Bauteilrand.Aussenluft, 0.5, azimutGrad: 90.0, alphaKonInnen_WM2K: 0.0))));

            // Ohne opakes Außenbauteil kein Wandzweig.
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => ErsatzparameterRC.AusBauteilweg(Gebaeude(), new List<BauteilEingang> { Innenwand("Innenwand", 80.0) })));
            Assert.Equal(GebaeudeModellFehler.BauteilUngueltig,
                Grund(() => ErsatzparameterRC.AusBauteilweg(Gebaeude(), new List<BauteilEingang>())));

            // Die Meldung nennt Gebäude und Bauteil.
            var ex = Assert.Throws<GebaeudeModellException>(() => Rechne(new BauteilEingang("Trennwand", Bauteilart.Innenwand, 10.0, Bauteilrand.Zone, 0.5)));
            Assert.Contains("Probe", ex.Message);
            Assert.Contains("Trennwand", ex.Message);
        }

        // =====================================================================
        //  Klimaweg für beliebige Flächen
        // =====================================================================

        [Fact]
        public void Die_Einstrahlung_auf_beliebige_Flaechen_ist_fuer_die_vier_Fassaden_bitgleich()
        {
            SolardatenModel[] k = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);
            foreach (Zeitbezug bezug in new[] { Zeitbezug.Stundenanfang, Zeitbezug.Stundenmitte })
            {
                Fassadenstrahlung f = GebaeudeKlimaweg.Fassaden(k, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, bezug);
                var paare = new (double AzimutNord, double[] Reihe)[] { (180.0, f.Sued), (90.0, f.Ost), (270.0, f.West), (0.0, f.Nord) };
                foreach ((double azimutNord, double[] reihe) in paare)
                {
                    double[] e = GebaeudeKlimaweg.Einstrahlung(k, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                        GebaeudeKlimaweg.AzimutAusDatenbank(azimutNord), 90.0, bezug);
                    Assert.Equal(reihe, e);   // bitgleich
                }
            }

            // Eine geneigte Fläche liegt nach Süden zwischen Fassade und Dach; nach Norden darunter.
            double[] sued45 = GebaeudeKlimaweg.Einstrahlung(k, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, 0.0, 45.0, Zeitbezug.Stundenanfang);
            double[] nord45 = GebaeudeKlimaweg.Einstrahlung(k, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, 180.0, 45.0, Zeitbezug.Stundenanfang);
            Assert.True(sued45.Sum() > nord45.Sum());
            Assert.All(sued45, w => Assert.True(w >= 0.0));
        }

        [Fact]
        public void Azimut_und_Sichtfaktor_folgen_den_benannten_Konventionen()
        {
            Assert.Equal(180.0, GebaeudeKlimaweg.AzimutAusDatenbank(0.0));      // Nord
            Assert.Equal(-90.0, GebaeudeKlimaweg.AzimutAusDatenbank(90.0));     // Ost
            Assert.Equal(0.0, GebaeudeKlimaweg.AzimutAusDatenbank(180.0));      // Süd
            Assert.Equal(90.0, GebaeudeKlimaweg.AzimutAusDatenbank(270.0));     // West
            Assert.Equal(180.0, GebaeudeKlimaweg.AzimutAusDatenbank(360.0));
            Assert.Equal(-45.0, GebaeudeKlimaweg.AzimutAusDatenbank(135.0));    // Südost

            Assert.Equal(GebaeudeFestwerte.SICHTFAKTOR_WAND, GebaeudeKlimaweg.SichtfaktorHimmel(90.0));
            Assert.Equal(GebaeudeFestwerte.SICHTFAKTOR_DACH, GebaeudeKlimaweg.SichtfaktorHimmel(0.0));
            Assert.Equal(0.0, GebaeudeKlimaweg.SichtfaktorHimmel(180.0), 15);
            Assert.Equal(0.75, GebaeudeKlimaweg.SichtfaktorHimmel(60.0), 15);
        }
    }

    /// <summary>
    /// Grenzfall Bauteilweg = Klassenweg an den Gebäuden der Referenzprojekte in der
    /// Testdatenbank: jedes Gebäude, das der Klassenweg rechnet, gibt als Bauteilsatz ohne
    /// Schichten denselben Parametersatz (relativ ≤ 1e-12).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErsatzparameterBauteilwegDatenbankTests : IClassFixture<TestDatenbank>
    {
        private static readonly int[] Projekte =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047 };

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _ausgabe;

        public ErsatzparameterBauteilwegDatenbankTests(TestDatenbank db, ITestOutputHelper ausgabe)
        {
            _db = db;
            _ausgabe = ausgabe;
        }

        [Fact]
        public void Die_Gebaeude_der_Referenzprojekte_rechnen_ohne_Schichten_im_Bauteilweg_wie_im_Klassenweg()
        {
            if (!_db.Vorhanden) return;
            int gerechnet = 0, abgelehnt = 0;
            double groesste = 0.0;
            foreach (int projekt in Projekte)
            {
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(projekt);
                for (int i = 0; i < ctrl.rows; i++)
                {
                    ProjektGebaeudeModel g = ctrl.items[i];
                    GebaeudeModellEingang e;
                    ErsatzparameterRC klasse;
                    try
                    {
                        e = GebaeudeModellEingang.Daten(g);
                        klasse = ErsatzparameterRC.AusKlassenweg(e);
                    }
                    catch (GebaeudeModellException)
                    {
                        abgelehnt++;   // der Klassenweg lehnt das Gebäude benannt ab; nichts zu vergleichen
                        continue;
                    }
                    ErsatzparameterRC bauteil = ErsatzparameterRC.AusBauteilweg(e, BauteilwegProbe.AusGebaeude(e));
                    (double abweichung, string feld) = BauteilwegProbe.Vergleich(klasse, bauteil);
                    Assert.True(abweichung <= 1e-12, "Projekt " + projekt + ", Gebäude " + g.ID_Gebaeude + ": " +
                        abweichung.ToString("E2", CultureInfo.InvariantCulture) + " in " + feld);
                    groesste = Math.Max(groesste, abweichung);
                    gerechnet++;
                }
            }
            _ausgabe.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0} Gebäude verglichen, {1} vom Klassenweg abgelehnt, größte relative Abweichung {2:E2}", gerechnet, abgelehnt, groesste));
            Assert.True(gerechnet > 0);
        }
    }
}
