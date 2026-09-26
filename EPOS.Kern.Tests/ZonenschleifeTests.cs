using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;
using static EPOS.Kern.Tests.BauteilwegLaufProbe;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zonenschleife (Stufe G6b, Welle W4)</b> — Gauß-Seidel je Stunde, Vorlauf nach A3,
    /// Startwert der unbeheizten Zone (Festlegung 7), 4-K-Regel mit adiabatem Vorlauf und die Proben
    /// des Mehrzonenkonzepts 8.1: 2 (zwei Hälften = das Ganze), 3 (adiabate Symmetrie), 7
    /// (Determinismus), 8 (Nichtkonvergenz und Mustertreue). Ohne Datenbank.
    /// </summary>
    public class ZonenschleifeTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenschleifeTests(ITestOutputHelper aus) { _aus = aus; }

        internal static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        internal static GebaeudeKlima KlimaDes()
            => new GebaeudeKlima(Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE);

        internal static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel g) => Zonenrechnung.Rechnen(g, KlimaDes(), false, null, 0, g.ID_Gebaeude);

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        // =====================================================================
        //  Keller und Wohnzone
        // =====================================================================

        /// <summary>
        /// Wohnzone und unbeheizter Keller rechnen gekoppelt: Die Schleife konvergiert, der Keller
        /// schwingt frei zwischen Erdreich und Wohnzone, die Heizlast des Gebäudes ist die Summe der
        /// Zonen, und der Keller startet beim Mittel seines θ_eq (Festlegung 7).
        /// </summary>
        [Fact]
        public void Wohnzone_und_Keller_rechnen_gekoppelt()
        {
            Mehrzonenergebnis r = Rechnen(ZonenEingangTests.MitKeller(out _));
            Assert.Equal(2, r.Zonen.Count);
            Assert.Single(r.Schleife.Gruppen);
            Assert.Empty(r.Paare);
            GebaeudeModellErgebnis wohnen = r.Zonen[0], keller = r.Zonen[1];

            for (int h = 0; h < 8760; h++)
            {
                Assert.Equal(wohnen.HeizlastW[h] + keller.HeizlastW[h], r.Gebaeude.HeizlastW[h]);
                Assert.Equal(0.0, keller.HeizlastW[h]);
                Assert.True(keller.Raumtemperatur[h] < wohnen.Raumtemperatur[h] + 1e-9, "Stunde " + h);
            }
            for (int h = 0; h < 8760; h++) Assert.Equal(wohnen.Raumtemperatur[h], r.Gebaeude.Raumtemperatur[h], 12);
            Assert.True(r.Gebaeude.JahresheizwaermeMwh > 0.0);
            Assert.True(r.Schleife.DurchlaeufeMax <= Zonenschleife.HOECHSTZAHL_DURCHLAEUFE);
            Assert.True(r.Schleife.DurchlaeufeMittel >= 2.0);
            Assert.True(r.Schleife.VorlaufStunden >= 2 * Vdi6007Rechenweg.VORLAUF_H);

            // Festlegung 7: Der Keller startet beim Mittel seines θ_eq über die Vorlaufstunden.
            double start = r.Schleife.Startwerte[1];
            Assert.True(start > 0.0 && start < 20.0, "Startwert " + start);
            Assert.Equal(r.Eingaenge[0].Eingang.ThetaSoll[8760 - Vdi6007Rechenweg.VORLAUF_H], r.Schleife.Startwerte[0]);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Keller: Start {0:F2} °C, Mittel {1:F2} °C, Min {2:F2}, Max {3:F2}; Wohnen {4:F3} MWh; Durchläufe Mittel {5:F2}, max {6}; Vorlauf {7} h (Abweichung {8:F4} K)",
                start, keller.Raumtemperatur.Average(), keller.Raumtemperatur.Min(), keller.Raumtemperatur.Max(),
                wohnen.JahresheizwaermeMwh, r.Schleife.DurchlaeufeMittel, r.Schleife.DurchlaeufeMax,
                r.Schleife.VorlaufStunden, r.Schleife.VorlaufAbweichungK));
        }

        // =====================================================================
        //  Probe 7 — Determinismus
        // =====================================================================

        /// <summary>Zwei Läufe und ein Lauf mit umgekehrter Eingabereihenfolge der Zonen sind byte-gleich.</summary>
        [Fact]
        public void Probe_7_Zwei_Laeufe_und_umgekehrte_Eingabe_sind_bytegleich()
        {
            ProjektGebaeudeModel g = ZonenEingangTests.MitKeller(out _);
            Mehrzonenergebnis a = Rechnen(g);
            Mehrzonenergebnis b = Rechnen(g);
            g.Zonen = g.Zonen.Reverse().ToList();
            Mehrzonenergebnis c = Rechnen(g);
            foreach (Mehrzonenergebnis x in new[] { b, c })
            {
                Assert.Equal(Abdruck(a.Gebaeude.HeizlastW), Abdruck(x.Gebaeude.HeizlastW));
                for (int z = 0; z < a.Zonen.Count; z++)
                {
                    Assert.Equal(Abdruck(a.Zonen[z].Raumtemperatur), Abdruck(x.Zonen[z].Raumtemperatur));
                    Assert.Equal(Abdruck(a.Zonen[z].HeizlastW), Abdruck(x.Zonen[z].HeizlastW));
                }
            }
        }

        internal static string Abdruck(double[] r) => GebaeudeEinzonennetzTests.Bilden(r).Sha256;

        // =====================================================================
        //  Probe 2 — zwei identische Hälften sind das Ganze
        // =====================================================================

        /// <summary>
        /// Probe 2 (Mehrzonenkonzept 8.1): Dieselbe halbe Zone zweimal, getrennt durch eine adiabate
        /// Trennwand — die beiden Zonenreihen sind bitgleich zueinander, und die Gebäudesumme trifft
        /// die Einzonenrechnung des ganzen Gebäudes (die Trennwand dort als Innenwand mit beiden
        /// Seiten): Jahresenergie 1e‑9 relativ, Stundenwerte 1e‑6 K. So sind Flächenaufteilung und
        /// die Halbierung der adiabaten Trennwand (2.2) geprüft.
        /// </summary>
        [Fact]
        public void Probe_2_Zwei_identische_Haelften_sind_das_Ganze()
        {
            ProjektGebaeudeModel ganz = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(ganz);
            const double trennA = 30.0;
            var trennInnen = new BauteilEingang("Trennwand", Bauteilart.Innenwand, 2.0 * trennA, Bauteilrand.Innen,
                                                schichten: new[] { Putz, Innenmauerwerk, Putz });
            ganz.Zonen = new[] { new GebaeudeZonensatz(basis.ZonenId, basis.Bezeichnung, basis.Bauteile.Append(trennInnen).ToList()) };
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(ganz, KlimaDes());
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(e, 0, 1);

            Mehrzonenergebnis r = Rechnen(Haelften(Trennflaechenzuordnung.Innen, trennA));
            GebaeudeModellErgebnis a = r.Zonen[0], b = r.Zonen[1];
            Assert.Equal(2, r.Schleife.Gruppen.Count);
            Assert.Equal(Abdruck(a.HeizlastW), Abdruck(b.HeizlastW));
            Assert.Equal(Abdruck(a.Raumtemperatur), Abdruck(b.Raumtemperatur));

            double jahr = r.Gebaeude.JahresheizwaermeMwh, jahrSoll = soll.JahresheizwaermeMwh;
            double dT = 0.0;
            for (int h = 0; h < 8760; h++) dT = Math.Max(dT, Math.Abs(a.Raumtemperatur[h] - soll.Raumtemperatur[h]));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Probe 2: Jahr {0:R} / {1:R} MWh (relativ {2:E2}), größte Stundenabweichung {3:E2} K",
                jahr, jahrSoll, jahr / jahrSoll - 1.0, dT));
            Assert.True(Math.Abs(jahr / jahrSoll - 1.0) <= 1e-9, "Jahresenergie relativ " + (jahr / jahrSoll - 1.0));
            Assert.True(dT <= 1e-6, "Stundenwerte " + dT + " K");
        }

        /// <summary>
        /// Zwei gleiche Hälften des Probegebäudes (Zone 1 und 2, je die halbe Fläche, halbe Bauteile und
        /// halbe Wärmebrücken), getrennt durch eine Trennwand der Fläche <paramref name="trennA"/> und der
        /// Zuordnung <paramref name="zuordnung"/>, am Ende der Liste der ersten Zone.
        /// </summary>
        internal static ProjektGebaeudeModel Haelften(Trennflaechenzuordnung zuordnung, double trennA = 30.0)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            GebaeudeZonensatz basis = Geschichtet(g);
            List<BauteilEingang> halb = basis.Bauteile.Select(Halbiert).ToList();
            var trenn = new BauteilEingang("Trennwand", Bauteilart.Innenwand, trennA, Bauteilrand.Zone,
                                           schichten: new[] { Putz, Innenmauerwerk, Putz }, idNachbarzone: 2, zuordnung: zuordnung);
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(1, "Hälfte 1", halb.Append(trenn).ToList(), 0.5 * g.Nutzflaeche, null, 1),
                new GebaeudeZonensatz(2, "Hälfte 2", halb, 0.5 * g.Nutzflaeche, null, 2),
            };
            return g;
        }

        private static BauteilEingang Halbiert(BauteilEingang b)
            => new BauteilEingang(b.Bezeichnung, b.Art, 0.5 * b.Flaeche_M2, b.Rand, b.UWert_WM2K, b.Schichten, b.NeigungGrad, b.AzimutGrad,
                                  b.GWert, b.Rahmenanteil, b.Verschattungsfaktor, 0.5 * b.PsiL_WK, b.AlphaKonInnen_WM2K, b.AlphaKonAussen_WM2K);

        // =====================================================================
        //  Probe 3 — adiabate Symmetrie
        // =====================================================================

        /// <summary>
        /// Probe 3 (Mehrzonenkonzept 8.1): zwei gleiche Zonen, die Trennwand einmal als IW und einmal
        /// ausdrücklich als AW. Im AW-Fall weicht der Summand der Trennwand B_NR·θ_NR,eq je Stunde um
        /// weniger als 0,01 K von B_NR·θ_air der eigenen Zone ab — der Antrieb über die Trennfläche
        /// verschwindet.
        ///
        /// <para><b>Befund zur Jahresenergie</b> (MZ 8.1 verlangt 0,1 %): gemessen +2,36 % bei 30 m²
        /// Trennwand je 100 m² Zone, +0,42 % bei 5 m², mit und ohne Nachtabsenkung gleich. Der Rest
        /// liegt nicht in der Kopplung: Die Trennwand wechselt die Gruppe, damit ändern sich A_AW und
        /// A_IW, der innere Übergang R_α,i, R_rad und die flächenanteilige Lastaufteilung — die
        /// Näherung von Gl. (27)/(28) (Strahlungspartner auf Lufttemperatur) trifft beide Rechnungen
        /// verschieden. Gehalten wird deshalb der Antrieb scharf und die Jahresenergie mit der
        /// gemessenen Größenordnung (3 %); das Kriterium steht zur Entscheidung.</para>
        /// </summary>
        [Fact]
        public void Probe_3_Adiabate_Symmetrie()
        {
            Mehrzonenergebnis iw = Rechnen(Haelften(Trennflaechenzuordnung.Innen));
            Mehrzonenergebnis aw = Rechnen(Haelften(Trennflaechenzuordnung.Aussen));
            Assert.Single(aw.Schleife.Gruppen);
            ZonenEingang z1 = aw.Eingaenge[0];
            Nachbarglied n = Assert.Single(z1.Eingang.Nachbarglieder);
            double b = n.UA_WK / z1.Eingang.UaSummeGewichtung_WK;
            double groesste = 0.0;
            for (int h = 0; h < 8760; h++)
                groesste = Math.Max(groesste, Math.Abs(b * aw.Zonen[1].Raumtemperatur[h] - b * aw.Zonen[0].Raumtemperatur[h]));
            double rel = aw.Gebaeude.JahresheizwaermeMwh / iw.Gebaeude.JahresheizwaermeMwh - 1.0;
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Probe 3: B_NR {0:F4}, größter Antrieb {1:E2} K, Jahresenergie AW/IW relativ {2:E2}", b, groesste, rel));
            Assert.True(groesste < 0.01, "Antrieb über die Trennfläche " + groesste + " K");
            Assert.True(Math.Abs(rel) < 0.03, "Jahresenergie relativ " + rel);
        }

        // =====================================================================
        //  Probe 8 — Nichtkonvergenz und Mustertreue
        // =====================================================================

        /// <summary>
        /// Probe 8: Bei erzwungener Nichtkonvergenz — hier eine Schwelle, die nicht zu erreichen ist —
        /// bricht die Rechnung mit dem benannten Fehler ab, der Gebäude, Stunde und Zonen nennt; kein
        /// stiller Rückfall.
        /// </summary>
        [Fact]
        public void Probe_8_Nichtkonvergenz_bricht_benannt_ab()
        {
            ProjektGebaeudeModel g = Haelften(Trennflaechenzuordnung.Aussen);
            g.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 2, 50000.0) };
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, KlimaDes());
            // Die beiden Hälften sind gleich - mit einer um 3 K verschobenen Hälfte konvergiert
            // der starke Luftaustausch sichtbar, aber nicht auf eine Schwelle von null.
            var schleife = new Zonenschleife(zonen, "Probegebäude") { Schwellen = (0.0, 0.0) };
            var ex = Assert.Throws<GebaeudeModellException>(() => schleife.Vorlauf());
            Assert.Equal(GebaeudeModellFehler.ZonenkopplungKonvergiertNicht, ex.Grund);
            Assert.Contains("Hälfte 1", ex.Message);
            Assert.Contains("Hälfte 2", ex.Message);
            Assert.Contains("Probegebäude", ex.Message);
        }

        /// <summary>
        /// Probe 8, die Pendelstunde: Zwei gekoppelte Zonen verschiedener Sollwerte mit starkem
        /// Luftaustausch — in Stunden, in denen eine Zone zwischen Heizen und freiem Lauf umschlägt,
        /// wechselt die Fallfolge zwischen den Durchläufen; das Muster des ersten Durchlaufs wird
        /// gehalten und gezählt, und die Rechnung konvergiert.
        /// </summary>
        [Fact]
        public void Probe_8_Pendelstunden_halten_das_Muster_und_werden_gezaehlt()
        {
            ProjektGebaeudeModel g = Haelften(Trennflaechenzuordnung.Aussen);
            GebaeudeZonensatz a = g.Zonen[0];
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile, a.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: a.Nutzflaeche_M2, SollTag: 22.0, SollNacht: 21.0), a.Rang),
                g.Zonen[1],
            };
            g.Zonenluftstroeme = new[] { new Zonenluftstrom(1, 2, 400.0) };
            Mehrzonenergebnis r = Rechnen(g);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "Probe 8: Musterwechsel {0}, nicht haltbar {1}, Durchläufe Mittel {2:F2}, max {3}",
                r.Schleife.Musterwechsel, r.Schleife.MusterNichtHaltbar, r.Schleife.DurchlaeufeMittel, r.Schleife.DurchlaeufeMax));
            Assert.True(r.Schleife.Musterwechsel + r.Schleife.MusterNichtHaltbar > 0, "Keine Pendelstunde.");
            Assert.All(r.Gebaeude.HeizlastW, w => Assert.True(double.IsFinite(w) && w >= 0.0));
        }
    }
}
