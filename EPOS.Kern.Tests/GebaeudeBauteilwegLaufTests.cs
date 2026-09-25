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
    /// Werkzeug der Proben des Bauteilwegs im Lauf (Stufe G3): Vergleich zweier Ergebnisse
    /// Stunde für Stunde, Gegenstrahlung für eine Klimareihe, Schichten erfundener Aufbauten.
    /// </summary>
    internal static class BauteilwegLaufProbe
    {
        internal static readonly Schicht Putz = new Schicht(0.015, 0.7, 1400.0, 1000.0);
        internal static readonly Schicht Mauerwerk = new Schicht(0.24, 0.8, 1600.0, 1000.0);
        internal static readonly Schicht Innenmauerwerk = new Schicht(0.115, 0.5, 1200.0, 1000.0);
        internal static readonly Schicht Beton = new Schicht(0.18, 2.0, 2400.0, 1000.0);
        internal static readonly Schicht Daemmung = new Schicht(0.04, 0.04, 30.0, 1400.0);
        internal static readonly Schicht Estrich = new Schicht(0.05, 1.4, 2000.0, 1000.0);

        /// <summary>Das Gebäude mit genau einer Zone.</summary>
        internal static ProjektGebaeudeModel MitZone(ProjektGebaeudeModel g, GebaeudeZonensatz zone)
        {
            g.Zonen = new[] { zone };
            return g;
        }

        /// <summary>Die übernommene Zone des Gebäudes, um weitere Bauteile ergänzt.</summary>
        internal static GebaeudeZonensatz UebernahmePlus(ProjektGebaeudeModel g, params BauteilEingang[] mehr)
        {
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            return new GebaeudeZonensatz(z.ZonenId, z.Bezeichnung, z.Bauteile.Concat(mehr).ToList());
        }

        /// <summary>
        /// Eine Zone mit geschichteten Bauteilen in den Flächen des Gebäudes — die Aufbauten sind
        /// erfunden und so gewählt, dass die U-Werte in der Nähe der Gebäudewerte liegen.
        /// Sonstiges bleibt masselos mit U-Wert (gemischte Außengruppe), dazu Innenwände.
        /// </summary>
        internal static GebaeudeZonensatz Geschichtet(ProjektGebaeudeModel g)
        {
            var b = new List<BauteilEingang>();
            double[] azimute = { 0.0, 90.0, 180.0, 270.0 };
            foreach (double az in azimute)
                b.Add(new BauteilEingang("Wand " + az.ToString(CultureInfo.InvariantCulture), Bauteilart.Aussenwand,
                    g.Flaeche_Außenwand / 4.0, Bauteilrand.Aussenluft, schichten: new[] { Putz, Mauerwerk, Putz },
                    azimutGrad: az, psiL_WK: 5.0));
            if (g.Sonstige_Flaechen > 0.0)
                b.Add(new BauteilEingang("Sonstiges", Bauteilart.Sonstiges, g.Sonstige_Flaechen, Bauteilrand.Aussenluft,
                    g.k_Wert_Sonstiges, azimutGrad: 180.0));
            b.Add(new BauteilEingang("Dach", Bauteilart.Dach, g.Dachflaeche, Bauteilrand.Aussenluft,
                schichten: new[] { Beton, Daemmung }));
            b.Add(new BauteilEingang("Bodenplatte", Bauteilart.Bodenplatte, g.Grundflaeche, Bauteilrand.Erdreich,
                schichten: new[] { Estrich, Daemmung, Beton }));
            b.Add(new BauteilEingang("Innenwände", Bauteilart.Innenwand, 1.5 * g.Nutzflaeche, Bauteilrand.Innen,
                schichten: new[] { Putz, Innenmauerwerk, Putz }));
            b.Add(new BauteilEingang("Fenster Süd", Bauteilart.Fenster, g.Fensterflaeche_Sued, Bauteilrand.Aussenluft,
                g.k_Wert_Fenster, azimutGrad: 180.0));
            b.Add(new BauteilEingang("Fenster Nord", Bauteilart.Fenster, Math.Max(g.Fensterflaeche_Nord, 1.0), Bauteilrand.Aussenluft,
                g.k_Wert_Fenster, azimutGrad: 0.0));
            return new GebaeudeZonensatz(7, "Geschichtet", b);
        }

        /// <summary>Gegenstrahlung einer Stunde nach Swinbank [W/m²] — nur für die Probe.</summary>
        internal static double Gegenstrahlung(double thetaAussen) => 5.31e-13 * Math.Pow(273.15 + thetaAussen, 6);

        /// <summary>Eine Kopie der Klimareihe mit Gegenstrahlung in jeder Stunde.</summary>
        internal static SolardatenModel[] MitGegenstrahlung(IReadOnlyList<SolardatenModel> klima)
        {
            var z = new SolardatenModel[klima.Count];
            for (int h = 0; h < klima.Count; h++)
            {
                SolardatenModel q = klima[h];
                z[h] = new SolardatenModel
                {
                    TagUtc = q.TagUtc, StundeUtc = q.StundeUtc, Außen_Temp = q.Außen_Temp,
                    Globalstrahlung = q.Globalstrahlung, Direktstrahlung = q.Direktstrahlung,
                    Diffusstrahlung = q.Diffusstrahlung, Gegenstrahlung = Gegenstrahlung(q.Außen_Temp),
                };
            }
            return z;
        }

        internal static double GroessteAbweichung(double[] a, double[] b)
        {
            if (a == null && b == null) return 0.0;
            Assert.NotNull(a);
            Assert.NotNull(b);
            double d = 0.0;
            for (int h = 0; h < a.Length; h++) d = Math.Max(d, Math.Abs(a[h] - b[h]));
            return d;
        }

        /// <summary>
        /// Die Abnahme des Grenzfalls: Heizlast und Kühlleistung je Stunde ≤ 1e-6 W,
        /// Raumluft und operative Temperatur ≤ 1e-6 K, Jahresheizwärme relativ ≤ 1e-9, die
        /// Randreihen des Eingangs ≤ 1e-9; mit Kopplung dazu Auslegungsheizlast relativ ≤ 1e-9
        /// und Vorlauf/Rücklauf ≤ 1e-6 K.
        /// </summary>
        internal static void Grenzfall(GebaeudeModellEingang klasse, GebaeudeModellEingang bauteil, string fall, ITestOutputHelper aus)
        {
            Assert.False(klasse.Bauteilweg);
            Assert.True(bauteil.Bauteilweg);
            double dEq = GroessteAbweichung(klasse.ThetaEq, bauteil.ThetaEq);
            double dSol = GroessteAbweichung(klasse.PhiSolar, bauteil.PhiSolar);
            double dRad = Math.Max(GroessteAbweichung(klasse.PhiRadAW, bauteil.PhiRadAW), GroessteAbweichung(klasse.PhiRadIW, bauteil.PhiRadIW));

            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(klasse, 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(bauteil, 0, 1);
            double dW = GroessteAbweichung(a.HeizlastW, b.HeizlastW);
            double dK = Math.Max(GroessteAbweichung(a.Raumtemperatur, b.Raumtemperatur),
                                 GroessteAbweichung(a.OperativeTemperatur, b.OperativeTemperatur));
            double dKuehl = 1000.0 * GroessteAbweichung(a.KuehlbedarfKwh, b.KuehlbedarfKwh);
            double rel = a.VerbrauchAltKwh > 0.0 ? Math.Abs(b.VerbrauchAltKwh / a.VerbrauchAltKwh - 1.0) : Math.Abs(b.VerbrauchAltKwh);

            aus?.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: Q = {1:F1} kWh, rel {2:E1}; Heizlast {3:E1} W, Kühlung {4:E1} W, Temperatur {5:E1} K; θ_eq {6:E1} K, Φ_sol {7:E1} W, Φ_rad {8:E1} W",
                fall, a.VerbrauchAltKwh, rel, dW, dKuehl, dK, dEq, dSol, dRad));

            Assert.True(dEq <= 1e-9, fall + ": θ_eq weicht um " + dEq.ToString("E2", CultureInfo.InvariantCulture) + " K ab.");
            Assert.True(dSol <= 1e-9, fall + ": Φ_sol weicht um " + dSol.ToString("E2", CultureInfo.InvariantCulture) + " W ab.");
            Assert.True(dRad <= 1e-9, fall + ": Φ_rad weicht um " + dRad.ToString("E2", CultureInfo.InvariantCulture) + " W ab.");
            Assert.True(dW <= 1e-6, fall + ": Heizlast weicht um " + dW.ToString("E2", CultureInfo.InvariantCulture) + " W ab.");
            Assert.True(dKuehl <= 1e-6, fall + ": Kühlleistung weicht um " + dKuehl.ToString("E2", CultureInfo.InvariantCulture) + " W ab.");
            Assert.True(dK <= 1e-6, fall + ": Temperatur weicht um " + dK.ToString("E2", CultureInfo.InvariantCulture) + " K ab.");
            Assert.True(rel <= 1e-9, fall + ": Jahresheizwärme weicht relativ um " + rel.ToString("E2", CultureInfo.InvariantCulture) + " ab.");
            Assert.Equal(1.0, b.Skalierungsfaktor);

            Assert.Equal(klasse.KopplungWirksam, bauteil.KopplungWirksam);
            if (klasse.KopplungWirksam)
            {
                double relN = Math.Abs(bauteil.AuslegungsheizlastW / klasse.AuslegungsheizlastW - 1.0);
                Assert.True(relN <= 1e-9, fall + ": Auslegungsheizlast weicht relativ um " + relN.ToString("E2", CultureInfo.InvariantCulture) + " ab.");
                Assert.Equal(klasse.Vorlaufquelle, bauteil.Vorlaufquelle);
                double dV = Math.Max(NaNGleich(a.Heizkreis.VorlaufC, b.Heizkreis.VorlaufC),
                                     NaNGleich(a.Heizkreis.RuecklaufC, b.Heizkreis.RuecklaufC));
                Assert.True(dV <= 1e-6, fall + ": Vor-/Rücklauf weicht um " + dV.ToString("E2", CultureInfo.InvariantCulture) + " K ab.");
            }
        }

        /// <summary>Größte Abweichung zweier Reihen, in denen NaN an denselben Stunden stehen muss.</summary>
        private static double NaNGleich(double[] a, double[] b)
        {
            double d = 0.0;
            for (int h = 0; h < a.Length; h++)
            {
                Assert.Equal(double.IsNaN(a[h]), double.IsNaN(b[h]));
                if (!double.IsNaN(a[h])) d = Math.Max(d, Math.Abs(a[h] - b[h]));
            }
            return d;
        }
    }

    /// <summary>
    /// <b>Stufe G3, Welle W — der Bauteilweg im Lauf</b>, ohne Datenbank: die Übernahme
    /// „Gebäude als eine Zone" (<see cref="GebaeudeZonenuebernahme"/>), der Grenzfall Bauteilweg
    /// = Klassenweg im Jahreslauf an der synthetischen Klimareihe (mit Gegenstrahlung, Schalter,
    /// Keller, Kopplung), geneigte und waagerechte Fenster, geschichtete Bauteile und der
    /// benannte Fehler bei zwei Zonen. Die Proben an Gebäuden der Testdatenbank stehen in
    /// <see cref="GebaeudeBauteilwegDatenbankTests"/>.
    /// </summary>
    public class GebaeudeBauteilwegLaufTests
    {
        private readonly ITestOutputHelper _aus;

        public GebaeudeBauteilwegLaufTests(ITestOutputHelper aus) { _aus = aus; }

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        /// <summary>Gleich bis auf die Reihenfolge der Summen: relativ ≤ 1e-9.</summary>
        private static void Relativ(double erwartet, double ist)
            => Assert.True(Math.Abs(ist - erwartet) <= 1e-9 * Math.Abs(erwartet),
                           "erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.ToString("R", CultureInfo.InvariantCulture));

        private static GebaeudeModellEingang Eingang(ProjektGebaeudeModel g, IReadOnlyList<SolardatenModel> klima = null,
                                                     string stufe = null)
            => GebaeudeModellEingang.Bauen(g, klima ?? Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                                           GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, stufe);

        // =====================================================================
        //  Die Übernahme „Gebäude als eine Zone"
        // =====================================================================

        [Fact]
        public void Die_Uebernahme_zerlegt_die_Gruppen_des_Gebaeudes()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            IReadOnlyList<BauteilEingang> b = z.Bauteile;

            Assert.Equal(0, z.ZonenId);
            Assert.Equal(GebaeudeZonenuebernahme.ZONE_BEZEICHNUNG, z.Bezeichnung);
            Assert.All(b, x => Assert.False(x.HatSchichten));

            BauteilEingang[] waende = b.Where(x => x.Art == Bauteilart.Aussenwand).ToArray();
            Assert.Equal(4, waende.Length);
            Assert.Equal(new[] { 0.0, 90.0, 180.0, 270.0 }, waende.Select(x => x.AzimutGrad));
            Assert.All(waende, x => Assert.Equal(90.0, x.NeigungGrad));
            Assert.All(waende, x => Assert.Equal(g.Flaeche_Außenwand / 4.0, x.Flaeche_M2));
            Assert.All(waende, x => Assert.Equal(g.k_Wert_Außenwand, x.UWert_WM2K));

            BauteilEingang[] sonstige = b.Where(x => x.Art == Bauteilart.Sonstiges).ToArray();
            Assert.Equal(4, sonstige.Length);
            Assert.All(sonstige, x => Assert.Equal(0.0, x.PsiL_WK));

            BauteilEingang dach = Assert.Single(b, x => x.Art == Bauteilart.Dach);
            Assert.Equal(0.0, dach.NeigungGrad);
            Assert.True(double.IsNaN(dach.AzimutGrad));
            BauteilEingang boden = Assert.Single(b, x => x.Art == Bauteilart.Bodenplatte);
            Assert.Equal(180.0, boden.NeigungGrad);
            Assert.Equal(Bauteilrand.Erdreich, boden.Rand);

            // Fenster je Richtung, Ost/West aus dem Bestandsfeld halbiert; g, Rahmen, Verschattung offen.
            BauteilEingang[] fenster = b.Where(x => x.Art == Bauteilart.Fenster).ToArray();
            Assert.Equal(new[] { 14.4, 10.08, 10.5, 10.08 }, fenster.Select(x => x.Flaeche_M2));
            Assert.Equal(new[] { 0.0, 90.0, 180.0, 270.0 }, fenster.Select(x => x.AzimutGrad));
            Assert.All(fenster, x => Assert.True(double.IsNaN(x.GWert) && double.IsNaN(x.Rahmenanteil) && double.IsNaN(x.Verschattungsfaktor)));

            // Σψ·L zu gleichen Teilen auf die Wandviertel.
            double psiL = 0.15 * 160.0 + 0.40 * 202.02 + 0.70 * 2.5;
            Assert.Equal(psiL, b.Sum(x => x.PsiL_WK), 12);
            Assert.All(waende, x => Assert.Equal(psiL / 4.0, x.PsiL_WK, 14));

            // Randbedingung der Grundfläche, eigene Ost-/Westflächen, Σψ·L ohne Außenwand.
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER;
            Assert.Equal(Bauteilrand.Unbeheizt, GebaeudeZonenuebernahme.AlsEineZone(g).Bauteile.Single(x => x.Art == Bauteilart.Bodenplatte).Rand);
            g.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            Assert.Equal(Bauteilrand.Aussenluft, GebaeudeZonenuebernahme.AlsEineZone(g).Bauteile.Single(x => x.Art == Bauteilart.Bodenplatte).Rand);
            g.Fensterflaeche_Ost = 15.0;
            g.Fensterflaeche_West = 5.16;
            Assert.Equal(new[] { 14.4, 15.0, 10.5, 5.16 },
                         GebaeudeZonenuebernahme.AlsEineZone(g).Bauteile.Where(x => x.Art == Bauteilart.Fenster).Select(x => x.Flaeche_M2));
            g.Flaeche_Außenwand = 0.0;
            IReadOnlyList<BauteilEingang> ohneWand = GebaeudeZonenuebernahme.AlsEineZone(g).Bauteile;
            Assert.DoesNotContain(ohneWand, x => x.Art == Bauteilart.Aussenwand);
            Assert.All(ohneWand.Where(x => x.Art == Bauteilart.Sonstiges), x => Assert.Equal(psiL / 4.0, x.PsiL_WK, 14));

            // Eine Gruppe ohne Fläche ergibt kein Bauteil.
            g.Fensterflaeche_Nord = 0.0;
            Assert.DoesNotContain(GebaeudeZonenuebernahme.AlsEineZone(g).Bauteile, x => x.Bezeichnung == "Fenster Nord");
        }

        [Fact]
        public void Die_Uebernahme_gleicht_dem_Klassenweg_im_Parametersatz()
        {
            foreach (Action<ProjektGebaeudeModel> variante in new Action<ProjektGebaeudeModel>[]
            {
                _ => { },
                x => x.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER,
                x => x.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT,
                x => { x.Fensterflaeche_Ost = 0.0; x.Fensterflaeche_West = 0.0; x.gesamte_Fensterflaeche = 24.9; },
            })
            {
                ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
                variante(g);
                ErsatzparameterRC klasse = Eingang(g).Parameter;
                ErsatzparameterRC bauteil = Eingang(BauteilwegLaufProbe.MitZone(g, GebaeudeZonenuebernahme.AlsEineZone(g))).Parameter;
                (double abweichung, string feld) = BauteilwegProbe.Vergleich(klasse, bauteil);
                Assert.True(abweichung <= 1e-12, "größte relative Abweichung " + abweichung.ToString("E2", CultureInfo.InvariantCulture) + " in " + feld);
                Assert.Equal(Gruppenweg.Klassenweg, bauteil.WegAussen);
                Assert.Equal(Gruppenweg.Klassenweg, bauteil.WegInnen);
            }
        }

        // =====================================================================
        //  Grenzfall im Jahreslauf (synthetische Klimareihe)
        // =====================================================================

        [Fact]
        public void Ohne_Zone_rechnet_der_Klassenweg_bitgleich()
        {
            ProjektGebaeudeModel ohne = Vdi6007Probe.Gebaeude();
            ProjektGebaeudeModel leer = Vdi6007Probe.Gebaeude();
            leer.Zonen = Array.Empty<GebaeudeZonensatz>();
            GebaeudeModellEingang a = Eingang(ohne), b = Eingang(leer);
            Assert.False(b.Bauteilweg);
            Assert.Null(b.Zone);
            Assert.Null(b.Bauteile);
            Assert.Equal(a.Parameter, b.Parameter);
            GebaeudeModellErgebnis x = Vdi6007Rechenweg.Laufen(a, 0, 1), y = Vdi6007Rechenweg.Laufen(b, 0, 1);
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(a.ThetaEq[h].Equals(b.ThetaEq[h]));
                Assert.True(x.HeizlastW[h].Equals(y.HeizlastW[h]));
            }
        }

        public static IEnumerable<object[]> Varianten()
        {
            yield return new object[] { "Bestand", false, false };
            yield return new object[] { "Schalter", false, false };
            yield return new object[] { "Schalter+Gegenstrahlung", true, false };
            yield return new object[] { "Keller+Schalter+Gegenstrahlung", true, false };
            yield return new object[] { "Außenluft ohne Schalter", true, false };
            yield return new object[] { "Ost/West eigen", false, false };
            yield return new object[] { "ohne Ost/West", false, false };
            yield return new object[] { "Kopplung", false, true };
            yield return new object[] { "Kopplung+Keller+Schalter", true, true };
        }

        [Theory]
        [MemberData(nameof(Varianten))]
        public void Mit_uebernommener_Zone_rechnet_der_Bauteilweg_wie_der_Klassenweg(string fall, bool gegenstrahlung, bool kopplung)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            if (fall.Contains("Schalter", StringComparison.Ordinal) && !fall.Contains("ohne Schalter", StringComparison.Ordinal))
                g.Aussenbauteile_Strahlung = true;
            if (fall.Contains("Keller", StringComparison.Ordinal))
            {
                g.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER;
                g.Kellertemperatur = 8.0;
            }
            if (fall.Contains("Außenluft", StringComparison.Ordinal)) g.Grundflaeche_Randbedingung = DbWerte.GRUND_AUSSENLUFT;
            if (fall == "Ost/West eigen") { g.Fensterflaeche_Ost = 14.0; g.Fensterflaeche_West = 6.16; }
            if (fall == "ohne Ost/West") { g.Fensterflaeche_Ost = 0.0; g.Fensterflaeche_West = 0.0; g.gesamte_Fensterflaeche = 24.9; }
            if (kopplung)
            {
                g.Heizkreis_Aktiv = true;
                g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
                g.Heizkurve_Aktiv = true;
            }
            IReadOnlyList<SolardatenModel> klima = gegenstrahlung ? BauteilwegLaufProbe.MitGegenstrahlung(Klima) : Klima;
            string stufe = kopplung ? DbWerte.ANLAGENKOPPLUNG_AK1 : null;

            GebaeudeModellEingang klasse = Eingang(g, klima, stufe);
            GebaeudeModellEingang bauteil = Eingang(BauteilwegLaufProbe.MitZone(g, GebaeudeZonenuebernahme.AlsEineZone(g)), klima, stufe);
            BauteilwegLaufProbe.Grenzfall(klasse, bauteil, fall, _aus);
        }

        // =====================================================================
        //  Fenster je Bauteil: Neigung, Azimut, eigene Werte
        // =====================================================================

        [Fact]
        public void Ein_Dachfenster_bekommt_mehr_Sonne_als_ein_senkrechtes_Suedfenster()
        {
            double sued45 = GebaeudeKlimaweg.EinstrahlungBauteil(Klima, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.AzimutAusDatenbank(180.0), 45.0, GebaeudeKlimaweg.ZEITBEZUG_VORGABE).Sum();
            double sued90 = GebaeudeKlimaweg.EinstrahlungBauteil(Klima, Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE,
                GebaeudeKlimaweg.AzimutAusDatenbank(180.0), 90.0, GebaeudeKlimaweg.ZEITBEZUG_VORGABE).Sum();
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Süd 45°: {0:F0} kWh/m², Süd 90°: {1:F0} kWh/m²", sued45 / 1000, sued90 / 1000));
            Assert.True(sued45 > sued90);

            BauteilEingang Fenster(double neigung)
                => new BauteilEingang("Dachfenster", Bauteilart.Fenster, 4.0, Bauteilrand.Aussenluft, 1.3,
                                      neigungGrad: neigung, azimutGrad: 180.0);
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang basis = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), GebaeudeZonenuebernahme.AlsEineZone(g)));
            GebaeudeModellEingang geneigt = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), BauteilwegLaufProbe.UebernahmePlus(g, Fenster(45.0))));
            GebaeudeModellEingang senkrecht = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), BauteilwegLaufProbe.UebernahmePlus(g, Fenster(90.0))));

            // Der solare Gewinn des einen Fensters ist A · I · g · (1 − R) · F_S · F_W mit den Gebäudewerten.
            double faktor = 4.0 * 0.75 * (1.0 - GebaeudeFestwerte.VORGABE_RAHMENANTEIL) * GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR * GebaeudeFestwerte.F_W;
            double gewinn45 = geneigt.PhiSolar.Sum() - basis.PhiSolar.Sum();
            double gewinn90 = senkrecht.PhiSolar.Sum() - basis.PhiSolar.Sum();
            Relativ(faktor * sued45, gewinn45);
            Relativ(faktor * sued90, gewinn90);
            Assert.True(gewinn45 > gewinn90);

            // Die Rechnung nimmt es auf: andere Gewinne, andere Heizwärme.
            double q45 = Vdi6007Rechenweg.Laufen(geneigt, 0, 1).VerbrauchAltKwh;
            double q90 = Vdi6007Rechenweg.Laufen(senkrecht, 0, 1).VerbrauchAltKwh;
            Assert.NotEqual(q45, q90);

            // Das Fenster trägt die Werte des Gebäudes, wo es keine eigenen hat.
            BauteilEingang dachfenster = geneigt.Bauteile.Single(x => x.Bezeichnung == "Dachfenster");
            Assert.Equal(0.75, dachfenster.GWert);
            Assert.Equal(GebaeudeFestwerte.VORGABE_RAHMENANTEIL, dachfenster.Rahmenanteil);
            Assert.Equal(GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR, dachfenster.Verschattungsfaktor);
        }

        [Fact]
        public void Ein_waagerechtes_Fenster_ohne_Azimut_bekommt_die_Globalstrahlung()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            var oberlicht = new BauteilEingang("Oberlicht", Bauteilart.Fenster, 3.0, Bauteilrand.Aussenluft, 1.3,
                                               neigungGrad: 0.0, gWert: 0.5, rahmenanteil: 0.2, verschattungsfaktor: 1.0);
            GebaeudeModellEingang basis = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), GebaeudeZonenuebernahme.AlsEineZone(g)));
            GebaeudeModellEingang mit = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), BauteilwegLaufProbe.UebernahmePlus(g, oberlicht)));

            double global = Klima.Sum(z => Math.Max(z.Globalstrahlung, 0.0));
            double erwartet = 3.0 * 0.5 * 0.8 * 1.0 * GebaeudeFestwerte.F_W * global;
            Relativ(erwartet, mit.PhiSolar.Sum() - basis.PhiSolar.Sum());
            Assert.True(Vdi6007Rechenweg.Laufen(mit, 0, 1).VerbrauchAltKwh > 0.0);
        }

        [Fact]
        public void Eigene_Fensterwerte_gelten_vor_den_Gebaeudewerten()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            var eigen = z.Bauteile.Select(b => b.IstTransparent
                ? new BauteilEingang(b.Bezeichnung, b.Art, b.Flaeche_M2, b.Rand, b.UWert_WM2K, neigungGrad: b.NeigungGrad,
                                     azimutGrad: b.AzimutGrad, gWert: 0.375)
                : b).ToList();
            GebaeudeModellEingang gebaeudewert = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), z));
            GebaeudeModellEingang halb = Eingang(BauteilwegLaufProbe.MitZone(Vdi6007Probe.Gebaeude(), new GebaeudeZonensatz(0, "eigen", eigen)));
            Relativ(0.5 * gebaeudewert.PhiSolar.Sum(), halb.PhiSolar.Sum());
        }

        // =====================================================================
        //  Geschichtete Bauteile
        // =====================================================================

        [Fact]
        public void Eine_Zone_mit_geschichteten_Bauteilen_rechnet_plausibel()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeModellEingang klasse = Eingang(Vdi6007Probe.Gebaeude());
            GebaeudeModellEingang schichten = Eingang(BauteilwegLaufProbe.MitZone(g, BauteilwegLaufProbe.Geschichtet(g)));

            Assert.True(schichten.Bauteilweg);
            Assert.Equal(Gruppenweg.Bauteilweg, schichten.Parameter.WegAussen);
            Assert.Equal(Gruppenweg.Bauteilweg, schichten.Parameter.WegInnen);
            Assert.All(schichten.ThetaEq, t => Assert.True(!double.IsNaN(t) && !double.IsInfinity(t)));

            GebaeudeModellErgebnis a = Vdi6007Rechenweg.Laufen(klasse, 0, 1);
            GebaeudeModellErgebnis b = Vdi6007Rechenweg.Laufen(schichten, 0, 1);
            double verhaeltnis = b.VerbrauchAltKwh / a.VerbrauchAltKwh;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Klassenweg {0:F0} kWh, Bauteilweg mit Schichten {1:F0} kWh, Verhältnis {2:F3}; C_AW {3:E3} J/K, C_IW {4:E3} J/K, ΣUA_opak {5:F1} W/K",
                a.VerbrauchAltKwh, b.VerbrauchAltKwh, verhaeltnis, schichten.Parameter.C_AW_Jk, schichten.Parameter.C_IW_Jk,
                schichten.Parameter.SummeUA_opak_WK));
            Assert.True(b.VerbrauchAltKwh > 0.0 && !double.IsInfinity(b.VerbrauchAltKwh));
            Assert.InRange(verhaeltnis, 1.0 / 3.0, 3.0);
            Assert.Equal(1.0, b.Skalierungsfaktor);

            // Der eingetragene U-Wert des masselosen Sonstigen steht in der Gewichtung.
            int i = schichten.Bauteile.ToList().FindIndex(x => x.Bezeichnung == "Sonstiges");
            Assert.Equal(g.k_Wert_Sonstiges, schichten.Parameter.UWirksamJeBauteil_WM2K[i]);
        }

        // =====================================================================
        //  Mehrere Zonen
        // =====================================================================

        [Fact]
        public void Zwei_Zonen_sind_ein_benannter_Fehler()
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            g.Zonen = new[] { z, new GebaeudeZonensatz(2, "Anbau", z.Bauteile) };
            GebaeudeModellException ex = Assert.Throws<GebaeudeModellException>(() => Eingang(g));
            Assert.Equal(GebaeudeModellFehler.MehrereZonen, ex.Grund);
            Assert.Contains("4711", ex.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <b>Stufe G3, Welle W — der Bauteilweg im Lauf an Gebäuden der Testdatenbank</b>: der
    /// Grenzfall Bauteilweg = Klassenweg im Jahreslauf für mehrere Gebäude und Varianten
    /// (Schalter, Keller, Fenster Ost/West, Kopplung), die Fassade mit Skalierungsfaktor 1 und
    /// benanntem Verbrauch, Auskunft = Lauf über den Zonenanschluss und der Fehlerweg bei zwei
    /// Zonen im Protokoll.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeBauteilwegDatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public GebaeudeBauteilwegDatenbankTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
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

        private static ProjektGebaeudeModel Zeile(int idProjekt, int nummer = 0)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[nummer];
            item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
            return item;
        }

        private static GebaeudeModellEingang Eingang(SimulationWaermebedarf sim, ProjektGebaeudeModel g,
                                                     IReadOnlyList<SolardatenModel> klima = null, string stufe = null)
        {
            KlimakalenderGemeinsam k = sim.Kalender.Gemeinsam;
            return GebaeudeModellEingang.Bauen(g, klima ?? k.SolarOrtszeit, k.WochenendeOrtszeit, k.Laengengrad, k.Breitengrad,
                                               GebaeudeKlimaweg.ZEITBEZUG_VORGABE, sim.KuehlbetriebProjekt, stufe);
        }

        /// <summary>
        /// <b>Abnahme (a): der Grenzfall im Jahreslauf.</b> Fünf Gebäude der Testdatenbank (mit
        /// und ohne Sonstiges, mit und ohne Nordfenster, ein gekühltes), je in sieben Varianten:
        /// wie gelesen, mit Schalter „Strahlung auf Außenbauteile", mit Keller und Schalter, mit
        /// Gegenstrahlung, mit eigenen und ohne Fensterflächen Ost/West, gekoppelt (AK1). Der Lauf
        /// mit der übernommenen Zone liefert dieselben Reihen wie der Klassenweg.
        /// </summary>
        [Theory]
        [InlineData(1045)]
        [InlineData(1007)]
        [InlineData(1017)]
        [InlineData(1018)]
        [InlineData(1023)]
        public void Mit_uebernommener_Zone_rechnet_der_Lauf_wie_der_Klassenweg(int projekt)
        {
            if (!_db.Vorhanden) return;

            SimulationWaermebedarf sim = NeueRechnung(projekt);
            SolardatenModel[] mitEa = BauteilwegLaufProbe.MitGegenstrahlung(sim.Kalender.Gemeinsam.SolarOrtszeit);
            var varianten = new (string Fall, Action<ProjektGebaeudeModel> Aendern, bool Ea, bool Kopplung)[]
            {
                ("gelesen", _ => { }, false, false),
                ("Schalter", x => x.Aussenbauteile_Strahlung = true, false, false),
                ("Keller+Schalter", x => { x.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER; x.Aussenbauteile_Strahlung = true; }, false, false),
                ("Schalter+Gegenstrahlung", x => x.Aussenbauteile_Strahlung = true, true, false),
                ("Ost/West eigen", x => { x.Fensterflaeche_Ost = 0.7 * x.Fensterflaeche_OstWest; x.Fensterflaeche_West = 0.3 * x.Fensterflaeche_OstWest; }, false, false),
                ("ohne Ost/West", x => { x.Fensterflaeche_Ost = 0.0; x.Fensterflaeche_West = 0.0; x.gesamte_Fensterflaeche = x.Fensterflaeche_Sued + x.Fensterflaeche_Nord; }, false, false),
                ("Kopplung+Keller", x => { x.Heizkreis_Aktiv = true; x.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR; x.Heizkurve_Aktiv = true; x.Grundflaeche_Randbedingung = DbWerte.GRUND_KELLER; }, false, true),
                ("Kopplung", x => { x.Heizkreis_Aktiv = true; x.Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE; }, false, true),
            };
            foreach (var v in varianten)
            {
                ProjektGebaeudeModel g = Zeile(projekt);
                v.Aendern(g);
                IReadOnlyList<SolardatenModel> klima = v.Ea ? mitEa : null;
                string stufe = v.Kopplung ? DbWerte.ANLAGENKOPPLUNG_AK1 : null;
                GebaeudeModellEingang klasse = Eingang(sim, g, klima, stufe);
                GebaeudeModellEingang bauteil = Eingang(sim, BauteilwegLaufProbe.MitZone(g, GebaeudeZonenuebernahme.AlsEineZone(g)), klima, stufe);
                BauteilwegLaufProbe.Grenzfall(klasse, bauteil, projekt.ToString(CultureInfo.InvariantCulture) + " " + v.Fall, _aus);
            }
        }

        /// <summary>
        /// <b>Die Fassade mit Zone</b> (Abnahme (a) und (c)): Mit der übernommenen Zone und einer
        /// Flächenangabe gleich der Nutzfläche liefert die Fassade dieselbe Reihe wie ohne Zone —
        /// der Klassenweg skaliert dann mit 1. Mit einer Verbrauchsangabe entfällt im Bauteilweg
        /// die Rückrechnung: Skalierungsfaktor 1, die Reihe ist die des einen Laufs, Bewohner =
        /// Nutzfläche / Fläche je Nutzer, und die nicht angewandte Angabe steht einmal im
        /// Protokoll.
        /// </summary>
        [Fact]
        public void Die_Fassade_rechnet_ein_Gebaeude_mit_Zone_ohne_Skalierung()
        {
            if (!_db.Vorhanden) return;

            SimulationWaermebedarf sim = NeueRechnung(1045);

            ProjektGebaeudeModel ohne = Zeile(1045);
            ohne.Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE;
            ohne.Z_AuswahlWohnflaeche = ohne.Nutzflaeche;
            var wOhne = new double[8760];
            SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(ohne, 0, wOhne));

            ProjektGebaeudeModel mit = Zeile(1045);
            string id = "(" + mit.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")";
            mit.Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE;
            mit.Z_AuswahlWohnflaeche = mit.Nutzflaeche;
            BauteilwegLaufProbe.MitZone(mit, new GebaeudeZonensatz(0, "Probezone", GebaeudeZonenuebernahme.AlsEineZone(mit).Bauteile));
            var wMit = new double[8760];
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, wMit));
            Assert.True(BauteilwegLaufProbe.GroessteAbweichung(wOhne, wMit) <= 1e-6);
            Assert.Equal(1.0, sim.GebaeudeErgebnisse.Ergebnis(0).Skalierungsfaktor);
            // Die Flächenangabe gleich der Nutzfläche hätte nicht skaliert: kein Hinweis darauf,
            // wohl aber der Hinweis auf den Bauteilweg mit der Zone.
            Assert.DoesNotContain(p.Hinweise, x => x.Contains(GebaeudeVorbereitung.EINHEIT_FLAECHE, StringComparison.Ordinal));
            Assert.Contains(p.Hinweise, x => x.Contains(id, StringComparison.Ordinal) && x.Contains("Probezone", StringComparison.Ordinal));

            // Verbrauchsangabe: keine Rückrechnung, ein Lauf, Faktor 1.
            ProjektGebaeudeModel verbrauch = Zeile(1045);
            verbrauch.Einheit = "Verbrauch  [MWh/a]";
            verbrauch.Z_AuswahlWohnflaeche = 20.0;
            GebaeudeZonensatz geschichtet = BauteilwegLaufProbe.Geschichtet(verbrauch);
            BauteilwegLaufProbe.MitZone(verbrauch, geschichtet);
            var wVerbrauch = new double[8760];
            p = SimulationProtokoll.NeuStarten();
            Assert.True(sim.HeizwaermeEinesGebaeudes(verbrauch, 0, wVerbrauch));
            GebaeudeModellErgebnis e = sim.GebaeudeErgebnisse.Ergebnis(0);
            Assert.Equal(1.0, e.Skalierungsfaktor);
            Assert.Equal(verbrauch.Nutzflaeche, verbrauch.Z_AuswahlWohnflaeche);
            Assert.Equal(verbrauch.Nutzflaeche / verbrauch.Flaeche_Nutzer, verbrauch.Bewohner);
            Assert.Equal(verbrauch.Nutzflaeche, sim.Wohnflaeche);

            // Die Reihe ist die des einen, unskalierten Laufs.
            ProjektGebaeudeModel frei = Zeile(1045);
            BauteilwegLaufProbe.MitZone(frei, geschichtet);
            GebaeudeModellErgebnis lauf = Vdi6007Rechenweg.Laufen(Eingang(sim, frei), 0, frei.ID_Gebaeude);
            for (int h = 0; h < 8760; h++) Assert.True(lauf.HeizlastW[h].Equals(wVerbrauch[h]), "Stunde " + h);
            Assert.True(Math.Abs(e.JahresheizwaermeMwh - 20.0) > 1.0, "Der Verbrauch darf nicht zurückgerechnet sein.");

            // Die nicht angewandte Angabe steht genau einmal im Protokoll.
            Assert.Single(p.Hinweise, x => x.Contains(id, StringComparison.Ordinal) && x.Contains("Verbrauch  [MWh/a]", StringComparison.Ordinal));
            Assert.True(p.IstFehlerfrei);
        }

        /// <summary>
        /// <b>Abnahme (d): Auskunft = Lauf für ein Gebäude mit Zone.</b> Über den Zonenanschluss
        /// bekommt das eine Gebäude des Projekts 1007 eine Zone (übernommen, dazu ein Dachfenster
        /// Süd 45°). Lauf (<c>Waermebedarf_berechnen</c>) und Auskunft
        /// (<see cref="GebaeudeBedarfCtrl.Rechnen"/>) lesen es beide über
        /// <see cref="ProjektGebaeudeCtrl.ReadAll"/> und liefern bitgleich dieselbe Zahl — eine
        /// andere als ohne Zone.
        /// </summary>
        [Fact]
        public void Die_Auskunft_ist_der_Lauf_fuer_ein_Gebaeude_mit_Zone()
        {
            if (!_db.Vorhanden) return;

            const int projekt = 1007;
            int region = Klimaregion(projekt);
            List<Z_ProjGebModel> zuordnungen = Z_ProjGebCtrl.LiesProjekt(projekt);
            Assert.Single(zuordnungen);

            ProjektGebaeudeModel g = Zeile(projekt);
            GebaeudeZonensatz zone = BauteilwegLaufProbe.UebernahmePlus(g,
                new BauteilEingang("Dachfenster", Bauteilart.Fenster, 3.0, Bauteilrand.Aussenluft, 1.3, neigungGrad: 45.0, azimutGrad: 180.0));
            var zonen = new Dictionary<int, IReadOnlyList<GebaeudeZonensatz>> { [g.ID_Gebaeude] = new[] { zone } };

            double ohneZone = GebaeudeBedarfCtrl.Rechnen(projekt, region, zuordnungen[0].ID_Z).HeizwaermeMwh;
            var vorher = GebaeudeZonenanschluss.Leser;
            try
            {
                GebaeudeZonenanschluss.Leser = id => id == projekt ? zonen : null;

                ProjektGebaeudeCtrl ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(projekt);
                Assert.Same(zone, Assert.Single(ctrl.items[0].Zonen));

                GebaeudeBedarfErgebnis auskunft = GebaeudeBedarfCtrl.Rechnen(projekt, region, zuordnungen[0].ID_Z);
                Assert.True(auskunft.Erfolgreich);
                var lauf = new SimulationWaermebedarf();
                lauf.Waermebedarf_berechnen(projekt, region);

                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "1007: ohne Zone {0:F4} MWh, mit Zone Lauf {1:F4} MWh, Auskunft {2:F4} MWh",
                                             ohneZone, lauf.Waermebedarf_Gebaeude_Gesamt, auskunft.HeizwaermeMwh));
                Assert.Equal(lauf.Waermebedarf_Gebaeude_Gesamt, auskunft.HeizwaermeMwh);
                Assert.NotEqual(ohneZone, auskunft.HeizwaermeMwh);
                Assert.Equal(1.0, lauf.GebaeudeErgebnisse.Ergebnis(0).Skalierungsfaktor);
            }
            finally
            {
                GebaeudeZonenanschluss.Leser = vorher;
            }

            // Ohne Leser trägt kein Gebäude eine Zone.
            ProjektGebaeudeCtrl ohne = new ProjektGebaeudeCtrl();
            ohne.ReadAll(projekt);
            Assert.Null(ohne.items[0].Zonen);
        }

        /// <summary>
        /// <b>Abnahme (e): zwei Zonen im Lauf.</b> Der VDI-Weg lehnt ein Gebäude mit zwei Zonen
        /// benannt ab — Fehler im Protokoll mit dem Grund, kein Ergebnis, kein stilles Auswählen.
        /// Auf dem Tagesbilanz-Weg geht eine Zone nicht ein; das steht als Hinweis im Protokoll.
        /// </summary>
        [Fact]
        public void Zwei_Zonen_brechen_den_Lauf_benannt_ab()
        {
            if (!_db.Vorhanden) return;

            SimulationWaermebedarf sim = NeueRechnung(1045);
            ProjektGebaeudeModel g = Zeile(1045);
            GebaeudeZonensatz z = GebaeudeZonenuebernahme.AlsEineZone(g);
            g.Zonen = new[] { z, z };
            SimulationProtokoll p = SimulationProtokoll.NeuStarten();
            Assert.False(sim.HeizwaermeEinesGebaeudes(g, 0, new double[8760]));
            Assert.Contains(p.Fehler, f => f.Contains(nameof(GebaeudeModellFehler.MehrereZonen), StringComparison.Ordinal));

            string id = "(" + g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture) + ")";
            ProjektGebaeudeModel alt = Zeile(1045);
            alt.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
            p = SimulationProtokoll.NeuStarten();
            sim.HeizwaermeEinesGebaeudes(alt, 0, new double[8760]);
            Assert.DoesNotContain(p.Hinweise, x => x.Contains(id, StringComparison.Ordinal));

            alt = Zeile(1045);
            alt.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
            alt.Zonen = new[] { z };
            p = SimulationProtokoll.NeuStarten();
            sim.HeizwaermeEinesGebaeudes(alt, 0, new double[8760]);
            Assert.Single(p.Hinweise, x => x.Contains(id, StringComparison.Ordinal));
        }
    }
}
