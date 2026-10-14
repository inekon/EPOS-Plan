using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wärmeübergabe je Zone im Mehrzonenweg</b> (E63, Welle AK1z, Teil B): Schritt H je Zone am
    /// gemeinsamen Vorlauf des Gebäudes. Synthetische Gebäude ohne Datenbank (<see cref="AufheizMehrzonenTests.Dreizonen"/>),
    /// dazu ein Lauf auf einer Arbeitskopie der Testdatenbank mit dem gekoppelten Zonenprojekt 1052:
    /// <list type="bullet">
    /// <item>Probe B je Zone (Grenzfall 3.7): unbegrenzte Übergabe und Xp = 0 an jeder Zone — bitgleich zum
    /// idealen Lauf desselben Gebäudes.</item>
    /// <item>Eine zu kleine Übergabe hält nur ihre Zone unter dem Sollwert.</item>
    /// <item>Rücklauf des Gebäudes massenstromgewichtet: Σ W_H,z·θ_R,z / Σ W_H,z = θ_V − ΣΦ_z/ΣW_H,z.</item>
    /// <item>IDEAL an einer Zone: ohne Heizkreis, das Gebäude allein aus den übrigen.</item>
    /// <item>Gauß-Seidel mit Übergabefällen: deterministisch, ohne Ausnahme, Musterwechsel gezählt; das feste
    /// Muster eines Übergabefalls rechnet die Stunde bitgleich nach.</item>
    /// </list>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenuebergabeRechenwegTests
    {
        private readonly ITestOutputHelper _aus;

        public ZonenuebergabeRechenwegTests(ITestOutputHelper aus) { _aus = aus; }

        private const int W1 = AufheizMehrzonenTests.WOHNUNG_1, W2 = AufheizMehrzonenTests.WOHNUNG_2, KELLER = AufheizMehrzonenTests.KELLER;

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static string F(double x, string format = "F3") => x.ToString(format, CultureInfo.InvariantCulture);

        /// <summary>Das gekoppelte Dreizonengebäude: Radiator am Gebäude, fester Vorlauf 55 °C, Strahlungsanteil fest.</summary>
        private static ProjektGebaeudeModel Gebaeude(Zoneneingaben eins, Zoneneingaben zwei, Zoneneingaben keller = null,
                                                     double stromM3h = 40.0)
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen(stromM3h: stromM3h, eins: eins, zwei: zwei);
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = false;
            g.Auslegung_Vorlauf = 55.0;
            g.Auslegung_Ruecklauf = 45.0;
            g.Heizung_Strahlungsanteil = 0.3;
            if (keller != null)
            {
                GebaeudeZonensatz k = g.Zonen[2];
                g.Zonen = new[] { g.Zonen[0], g.Zonen[1],
                                  new GebaeudeZonensatz(k.ZonenId, k.Bezeichnung, k.Bauteile.ToList(), k.Nutzflaeche_M2, keller, k.Rang) };
            }
            return g;
        }

        private static Zoneneingaben Zone(double flaeche, double sollTag, double sollNacht, double? nennKw, double? xp,
                                          string art = null)
            => new Zoneneingaben(Nutzflaeche: flaeche, SollTag: sollTag, SollNacht: sollNacht, UebergabeArt: art,
                                 UebergabeLeistungNennKw: nennKw, ReglerProportionalbandK: xp);

        private static double Flaeche => 0.5 * Vdi6007Probe.Gebaeude().Nutzflaeche;

        private static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel g, string stufe, GebaeudeKlima klima = null)
            => Zonenrechnung.Rechnen(g, klima ?? ZonenschleifeTests.KlimaDes(), false, stufe, 0, g.ID_Gebaeude);

        private static int Stelle(Mehrzonenergebnis m, int id) => m.Eingaenge.Single(z => z.ZonenId == id).Index;

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            Assert.Equal(a.Length, b.Length);
            for (int h = 0; h < a.Length; h++)
                if (Bits(a[h]) != Bits(b[h]))
                    Assert.Fail(was + ", Stunde " + h + ": " + a[h].ToString("R", CultureInfo.InvariantCulture) + " ≠ " +
                                b[h].ToString("R", CultureInfo.InvariantCulture));
        }

        // =====================================================================
        //  Probe B je Zone
        // =====================================================================

        /// <summary>
        /// <b>Probe B je Zone</b> (Bauvorschrift Konzept 3.7): drei beheizte Zonen, jede gekoppelt mit unbegrenzter
        /// Übergabe und Xp = 0 — Heizlast, Raumluft und operative Temperatur jeder Zone und des Gebäudes sind Stunde
        /// für Stunde bitgleich zum idealen Lauf desselben Gebäudes; der Rücklauf jeder Zone ist der Vorlauf (W_H = ∞).
        /// </summary>
        [Fact]
        public void Probe_B_je_Zone_ist_bitgleich_zum_idealen_Lauf()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 16.0, double.PositiveInfinity, 0.0),
                                              Zone(f, 20.0, 17.0, double.PositiveInfinity, 0.0),
                                              new Zoneneingaben(Nutzflaeche: 88.0, SollTag: 15.0, SollNacht: 12.0,
                                                                UebergabeLeistungNennKw: double.PositiveInfinity,
                                                                ReglerProportionalbandK: 0.0));
            Mehrzonenergebnis ideal = Rechnen(g, null);
            Mehrzonenergebnis gekoppelt = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1);

            Assert.All(ideal.Eingaenge, z => Assert.False(z.Eingang.KopplungWirksam));
            Assert.All(gekoppelt.Eingaenge, z => Assert.True(z.Eingang.KopplungWirksam));
            Assert.Equal(3, gekoppelt.Zonen.Count);
            for (int i = 0; i < 3; i++)
            {
                string was = gekoppelt.Eingaenge[i].Bezeichnung;
                Bitgleich(ideal.Zonen[i].HeizlastW, gekoppelt.Zonen[i].HeizlastW, was + ", Heizlast");
                Bitgleich(ideal.Zonen[i].Raumtemperatur, gekoppelt.Zonen[i].Raumtemperatur, was + ", Raumluft");
                Bitgleich(ideal.Zonen[i].OperativeTemperatur, gekoppelt.Zonen[i].OperativeTemperatur, was + ", operativ");
                HeizkreisErgebnis hk = gekoppelt.Zonen[i].Heizkreis;
                Assert.NotNull(hk);
                Assert.Null(ideal.Zonen[i].Heizkreis);
                Assert.Equal(0.0, hk.UebergabeBegrenztStundenH);
                for (int h = 0; h < 8760; h++)
                    if (!double.IsNaN(hk.VorlaufC[h])) Assert.Equal(Bits(hk.VorlaufC[h]), Bits(hk.RuecklaufC[h]));
            }
            Bitgleich(ideal.Gebaeude.HeizlastW, gekoppelt.Gebaeude.HeizlastW, "Gebäude, Heizlast");
            Bitgleich(ideal.Gebaeude.Raumtemperatur, gekoppelt.Gebaeude.Raumtemperatur, "Gebäude, Raumluft");
            Assert.Equal(Bits(ideal.Gebaeude.SpitzeKw), Bits(gekoppelt.Gebaeude.SpitzeKw));
            Assert.Equal(Bits(ideal.Gebaeude.JahresheizwaermeMwh), Bits(gekoppelt.Gebaeude.JahresheizwaermeMwh));
            Assert.Null(ideal.Gebaeude.Heizkreis);
            Assert.NotNull(gekoppelt.Gebaeude.Heizkreis);
            _aus.WriteLine("Probe B je Zone: Jahresheizwärme {0} MWh, Spitze {1} kW, Musterwechsel {2}",
                           F(gekoppelt.Gebaeude.JahresheizwaermeMwh), F(gekoppelt.Gebaeude.SpitzeKw),
                           gekoppelt.Schleife.Musterwechsel + gekoppelt.Schleife.MusterNichtHaltbar);
        }

        // =====================================================================
        //  Begrenzte Zone, Rücklauf, IDEAL
        // =====================================================================

        /// <summary>
        /// Eine Zone mit zu kleiner Nennleistung bleibt unter ihrem Sollwert, die Nachbarzone (unbegrenzt, Xp = 0)
        /// hält ihn; begrenzte Stunden trägt nur die kleine Zone, das Gebäude hat das Maximum.
        /// </summary>
        [Fact]
        public void Eine_zu_kleine_Uebergabe_haelt_nur_ihre_Zone_unter_dem_Sollwert()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 21.0, 0.3, 0.0), Zone(f, 20.0, 20.0, double.PositiveInfinity, 0.0));
            Mehrzonenergebnis m = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1, AufheizMehrzonenTests.Klima(-5.0));
            GebaeudeModellErgebnis eins = m.Zonen[Stelle(m, W1)], zwei = m.Zonen[Stelle(m, W2)];
            double unterEins = 0.0, unterZwei = 0.0;
            for (int h = 0; h < 8760; h++)
            {
                unterEins = Math.Max(unterEins, eins.Heizsollwert[h] - eins.Raumtemperatur[h]);
                unterZwei = Math.Max(unterZwei, zwei.Heizsollwert[h] - zwei.Raumtemperatur[h]);
            }
            Assert.True(unterEins > 0.5, "Wohnung 1 hält den Sollwert: " + F(unterEins));
            Assert.True(unterZwei < 1e-6, "Wohnung 2 unterschreitet: " + F(unterZwei, "E3"));
            Assert.True(eins.Heizkreis.UebergabeBegrenztStundenH > 0.0);
            Assert.Equal(0.0, zwei.Heizkreis.UebergabeBegrenztStundenH);
            Assert.Null(m.Zonen[Stelle(m, KELLER)].Heizkreis);
            Assert.False(m.Eingaenge[Stelle(m, KELLER)].Eingang.KopplungWirksam);
            HeizkreisErgebnis geb = m.Gebaeude.Heizkreis;
            Assert.NotNull(geb);
            Assert.Equal(eins.Heizkreis.UebergabeBegrenztStundenH, geb.UebergabeBegrenztStundenH, 9);
            Assert.Equal(Vorgabeherkunft.Zone, Vorgabeherkunft.Zone);
            Assert.Equal(300.0, m.Eingaenge[Stelle(m, W1)].Eingang.Uebergabe.PhiNW, 9);
            _aus.WriteLine("Unterschreitung Wohnung 1 {0} K, begrenzt {1} h; Wohnung 2 {2} K", F(unterEins),
                           F(eins.Heizkreis.UebergabeBegrenztStundenH, "F1"), F(unterZwei, "E2"));
        }

        /// <summary>
        /// <b>Rücklaufidentität</b>: Der Rücklauf des Gebäudes ist je Stunde Σ W_H,z·θ_R,z / Σ W_H,z und
        /// gleich θ_V − ΣΦ_z/ΣW_H,z (Toleranz 1e-9); der Vorlauf ist der gemeinsame der Zonen.
        /// </summary>
        [Fact]
        public void Der_Ruecklauf_des_Gebaeudes_ist_massenstromgewichtet()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 16.0, 0.6, 1.0), Zone(f, 20.0, 17.0, 4.0, 2.0));
            Mehrzonenergebnis m = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1);
            var gekoppelt = Enumerable.Range(0, m.Zonen.Count).Where(i => m.Eingaenge[i].Eingang.KopplungWirksam).ToList();
            Assert.Equal(2, gekoppelt.Count);
            HeizkreisErgebnis geb = m.Gebaeude.Heizkreis;
            int stunden = 0;
            for (int h = 0; h < 8760; h++)
            {
                double sW = 0.0, sWR = 0.0, sPhi = 0.0, v = double.NaN;
                foreach (int i in gekoppelt)
                {
                    HeizkreisErgebnis z = m.Zonen[i].Heizkreis;
                    if (double.IsNaN(z.VorlaufC[h])) continue;
                    double w = m.Eingaenge[i].Eingang.Uebergabe.WHWK;
                    v = z.VorlaufC[h];
                    sW += w;
                    sWR += w * z.RuecklaufC[h];
                    sPhi += m.Zonen[i].HeizlastW[h];
                }
                if (double.IsNaN(v))
                {
                    Assert.True(double.IsNaN(geb.RuecklaufC[h]));
                    continue;
                }
                stunden++;
                Assert.Equal(v, geb.VorlaufC[h]);
                Assert.Equal(sWR / sW, geb.RuecklaufC[h], 9);
                Assert.Equal(v - sPhi / sW, geb.RuecklaufC[h], 9);
            }
            Assert.True(stunden > 0);
            Assert.True(geb.RuecklaufMittelC < geb.VorlaufMittelC);
            _aus.WriteLine("Gebäude: Vorlauf {0} °C, Rücklauf {1} °C über {2} Heizstunden", F(geb.VorlaufMittelC),
                           F(geb.RuecklaufMittelC), geb.Heizstunden);
        }

        /// <summary>
        /// IDEAL an einer Zone eines gekoppelten Gebäudes: Die Zone rechnet den Bestandsweg ohne Heizkreis (Ergebniszeile
        /// NULL, kein Exportschlüssel), der Rücklauf des Gebäudes kommt allein aus der übrigen gekoppelten Zone.
        /// </summary>
        [Fact]
        public void Ideal_an_einer_Zone_rechnet_ohne_Heizkreis()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 16.0, 0.8, 1.0), Zone(f, 20.0, 17.0, null, null, DbWerte.UEBERGABE_IDEAL));
            Mehrzonenergebnis m = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1);
            int i1 = Stelle(m, W1), i2 = Stelle(m, W2);
            Assert.True(m.Eingaenge[i1].Eingang.KopplungWirksam);
            Assert.False(m.Eingaenge[i2].Eingang.KopplungWirksam);
            Assert.Null(m.Zonen[i2].Heizkreis);
            HeizkreisErgebnis eins = m.Zonen[i1].Heizkreis, geb = m.Gebaeude.Heizkreis;
            for (int h = 0; h < 8760; h++)
            {
                if (double.IsNaN(eins.VorlaufC[h])) Assert.True(double.IsNaN(geb.RuecklaufC[h]));
                else Assert.Equal(eins.RuecklaufC[h], geb.RuecklaufC[h], 1e-9);
            }

            ErgebnisGebaeudeModel zeile = GebaeudeKennzahlen.Bilden(1, g.ID_Gebaeude, "Probe", DbWerte.GEBAEUDE_MODELL_VDI6007,
                                                                    m.Gebaeude.HeizlastW.Select(w => w / 1000.0).ToArray(), m.Gebaeude);
            ErgebnisZoneModel z1 = zeile.Zonen.Single(z => z.ID_Zone == W1), z2 = zeile.Zonen.Single(z => z.ID_Zone == W2);
            ErgebnisZoneModel zk = zeile.Zonen.Single(z => z.ID_Zone == KELLER);
            Assert.NotNull(z1.VorlaufMittelC);
            Assert.NotNull(z1.RuecklaufMittelC);
            Assert.NotNull(z1.UebergabeBegrenztH);
            Assert.Null(z2.VorlaufMittelC);
            Assert.Null(z2.RuecklaufMittelC);
            Assert.Null(z2.UebergabeBegrenztH);
            Assert.Null(zk.VorlaufMittelC);
            Assert.NotNull(zeile.VorlaufMittelC);

            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(m.Gebaeude);
            List<string> schluessel = satz.Skalare.Select(p => p.Key).ToList();
            string q1 = "Zone[" + i1 + "].", q2 = "Zone[" + i2 + "].";
            Assert.Contains(schluessel, k => k.EndsWith(q1 + "VorlaufMittelC", StringComparison.Ordinal));
            Assert.Contains(schluessel, k => k.EndsWith(q1 + "UebergabeBegrenztH", StringComparison.Ordinal));
            Assert.DoesNotContain(schluessel, k => k.Contains(q2 + "VorlaufMittelC", StringComparison.Ordinal));
            Assert.DoesNotContain(schluessel, k => k.Contains(q2 + "UebergabeBegrenztH", StringComparison.Ordinal));
        }

        /// <summary>Ein ungekoppeltes Mehrzonengebäude trägt weder Heizkreise noch die neuen Exportschlüssel.</summary>
        [Fact]
        public void Ungekoppelt_entsteht_kein_Heizkreis_und_kein_Schluessel()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 16.0, 0.8, 1.0), Zone(f, 20.0, 17.0, 2.0, 1.0));
            Mehrzonenergebnis m = Rechnen(g, null);
            Assert.Null(m.Gebaeude.Heizkreis);
            Assert.All(m.Zonen, z => Assert.Null(z.Heizkreis));
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(m.Gebaeude);
            Assert.DoesNotContain(satz.Skalare, p => p.Key.EndsWith("VorlaufMittelC", StringComparison.Ordinal)
                                                   || p.Key.EndsWith("UebergabeBegrenztH", StringComparison.Ordinal));
        }

        // =====================================================================
        //  Gauß-Seidel mit Übergabefällen
        // =====================================================================

        /// <summary>
        /// Eine Teilgruppe mit Trennwand und Luftstrom, beide Zonen mit begrenzter Übergabe und Xp &gt; 0: zwei
        /// Läufe bitgleich, keine Ausnahme, die Musterwechsel je Zone summieren sich zu denen der Schleife.
        /// </summary>
        [Fact]
        public void Gauss_Seidel_mit_Uebergabefaellen_ist_deterministisch()
        {
            double f = Flaeche;
            ProjektGebaeudeModel g = Gebaeude(Zone(f, 21.0, 16.0, 0.7, 1.5), Zone(f, 20.0, 17.0, 1.2, 1.0), stromM3h: 120.0);
            Mehrzonenergebnis a = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1);
            Mehrzonenergebnis b = Rechnen(g, DbWerte.ANLAGENKOPPLUNG_AK1);
            Assert.Contains(a.Schleife.Gruppen, gr => gr.Count >= 2);
            for (int i = 0; i < a.Zonen.Count; i++)
            {
                string was = a.Eingaenge[i].Bezeichnung;
                Bitgleich(a.Zonen[i].HeizlastW, b.Zonen[i].HeizlastW, was + ", Heizlast");
                Bitgleich(a.Zonen[i].Raumtemperatur, b.Zonen[i].Raumtemperatur, was + ", Raumluft");
                if (a.Zonen[i].Heizkreis != null)
                {
                    Bitgleich(a.Zonen[i].Heizkreis.RuecklaufC, b.Zonen[i].Heizkreis.RuecklaufC, was + ", Rücklauf");
                    Bitgleich(a.Zonen[i].Heizkreis.UebergabeBegrenztAnteil, b.Zonen[i].Heizkreis.UebergabeBegrenztAnteil, was + ", begrenzt");
                }
            }
            Bitgleich(a.Gebaeude.Heizkreis.RuecklaufC, b.Gebaeude.Heizkreis.RuecklaufC, "Gebäude, Rücklauf");
            Assert.Equal(a.Schleife.Musterwechsel, b.Schleife.Musterwechsel);
            Assert.Equal(a.Schleife.MusterNichtHaltbar, b.Schleife.MusterNichtHaltbar);
            List<GebaeudeZonenergebnis> zonen = a.Gebaeude.Zonen.ToList();
            Assert.Equal(a.Schleife.Musterwechsel + a.Schleife.MusterNichtHaltbar, zonen.Sum(z => z.MusterwechselH));
            Assert.True(a.Zonen.Any(z => z.Heizkreis != null && z.Heizkreis.UebergabeBegrenztStundenH > 0.0));
            _aus.WriteLine("Musterwechsel gehalten {0}, nicht haltbar {1}, Durchläufe max {2}", a.Schleife.Musterwechsel,
                           a.Schleife.MusterNichtHaltbar, a.Schleife.DurchlaeufeMax);
        }

        /// <summary>
        /// Das feste Muster eines Übergabefalls (Regel in <see cref="Zonenmodell2K.SchrittMitMuster"/>): Mit dem
        /// Muster, das <see cref="Zonenmodell2K.Schritt"/> für denselben Rand und Zustand gefunden hat, ist jede Stunde
        /// bitgleich — Leistung, Raumluft, Vorlauf, Rücklauf, Grund und Anteile; die Folge enthält beide Übergabefälle.
        /// </summary>
        [Fact]
        public void Das_feste_Muster_eines_Uebergabefalls_rechnet_bitgleich_nach()
        {
            var k = new Uebergabekennwerte(2500.0, 1.3, 55.0, 45.0, 20.0);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(18.0);
            b.Zuruecksetzen(18.0);
            int gesaettigt = 0, regel = 0;
            for (int h = 0; h < 2000; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 2.0 - 10.0 * Math.Cos(2.0 * Math.PI * h / 1500.0) + 4.0 * tag;
                double soll = (h % 24) >= 6 && (h % 24) <= 21 ? 21.0 : 16.0;
                double vorlauf = aussen < 15.0 ? 55.0 - 0.8 * aussen : double.NaN;
                var r = new Stundenrand(aussen, aussen, soll, double.PositiveInfinity, 0.0, 0.0, Math.Max(0.0, tag) * 400.0,
                                        h % 7 == 0 ? 1800.0 : double.NaN, double.NaN, 0.3, 0.0, 0.0,
                                        uebergabe: k, vorlaufC: vorlauf, reglerbandK: 1.0 + (h % 3));
                Stundenergebnis ea = a.Schritt(in r);
                Stundenmuster muster = a.LetztesMuster;
                Stundenergebnis eb = b.SchrittMitMuster(in r, muster);
                foreach (Betriebsfall fall in muster.Folge)
                {
                    if (fall == Betriebsfall.UebergabeGesaettigt) gesaettigt++;
                    if (fall == Betriebsfall.UebergabeRegelbereich) regel++;
                }
                Assert.Equal(Bits(ea.HeizleistungW), Bits(eb.HeizleistungW));
                Assert.Equal(Bits(ea.ThetaAirMittel), Bits(eb.ThetaAirMittel));
                Assert.Equal(Bits(ea.ThetaOpMittel), Bits(eb.ThetaOpMittel));
                Assert.Equal(Bits(ea.VorlaufC), Bits(eb.VorlaufC));
                Assert.Equal(Bits(ea.RuecklaufC), Bits(eb.RuecklaufC));
                Assert.Equal(ea.Begrenzungsgrund, eb.Begrenzungsgrund);
                Assert.Equal(Bits(ea.UebergabeBegrenztAnteil), Bits(eb.UebergabeBegrenztAnteil));
                Assert.Equal(Bits(ea.HeizleistungMaxAnteil), Bits(eb.HeizleistungMaxAnteil));
                Assert.Equal(Bits(ea.HeizgrenzeAnteil), Bits(eb.HeizgrenzeAnteil));
                Assert.Equal(Bits(a.ThetaMAw), Bits(b.ThetaMAw));
                Assert.Equal(Bits(a.ThetaMIw), Bits(b.ThetaMIw));
            }
            Assert.True(gesaettigt > 0, "kein gesättigter Abschnitt");
            Assert.True(regel > 0, "kein Abschnitt im Regelbereich");
            _aus.WriteLine("Abschnitte gesättigt {0}, Regelbereich {1}", gesaettigt, regel);
        }

        // =====================================================================
        //  Datenbankfall: 1052 gekoppelt
        // =====================================================================

        /// <summary>
        /// Arbeitskopie der Testdatenbank, 1052 gekoppelt (AK1, Heizkreis, Radiator am Gebäude, die Gastronomie mit
        /// Konvektor 70/50 °C und Xp 2 K): <c>Tab_ErgebnisZone</c> trägt Vorlauf, Rücklauf und begrenzte Stunden für
        /// beide beheizten Zonen, der Keller NULL; <c>Tab_ErgebnisGebaeude</c> trägt Vorlauf und Rücklauf; zwei Läufe
        /// sind bitgleich.
        /// </summary>
        [Fact]
        public void Das_gekoppelte_Zonenprojekt_schreibt_den_Kreis_je_Zone()
        {
            using var db = new TestDatenbank();
            using var kultur = new Kulturvorrichtung();
            if (!db.Vorhanden) return;
            const int projekt = 1052;
            long gebaeude = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Z_ProjektGebaeude WHERE ID_Projekt = ?", new DbParam("@p", projekt)), CultureInfo.InvariantCulture);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Anlagenkopplung = ? WHERE ID_Projekt = ?",
                new DbParam("@s", DbWerte.ANLAGENKOPPLUNG_AK1), new DbParam("@p", projekt)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Heizkurve_Aktiv = 1 WHERE ID = ?",
                new DbParam("@art", DbWerte.UEBERGABE_RADIATOR), new DbParam("@id", gebaeude)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Zone SET Uebergabe_Art = ?, Auslegung_Vorlauf = 70, Auslegung_Ruecklauf = 50, " +
                "Regler_Proportionalband = 2 WHERE ID_Gebaeude = ? AND Bezeichner = ?",
                new DbParam("@art", DbWerte.UEBERGABE_KONVEKTOR), new DbParam("@g", gebaeude),
                new DbParam("@z", "Gastronomie und Verwaltung")));

            ErgebnisGebaeudeModel Lauf()
            {
                int kopf = new SimulationRunner().SimuliereUndSpeichere(projekt, out string fehler);
                Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
                return Assert.Single(new ErgebnisCtrl().Load(projekt).Gebaeude);
            }
            ErgebnisGebaeudeModel eins = Lauf(), zwei = Lauf();

            Assert.Equal(3, eins.Zonen.Count);
            foreach (ErgebnisZoneModel z in eins.Zonen)
            {
                if (z.IstBeheizt)
                {
                    Assert.NotNull(z.VorlaufMittelC);
                    Assert.NotNull(z.RuecklaufMittelC);
                    Assert.NotNull(z.UebergabeBegrenztH);
                    Assert.True(z.RuecklaufMittelC < z.VorlaufMittelC, z.Bezeichner);
                }
                else
                {
                    Assert.Null(z.VorlaufMittelC);
                    Assert.Null(z.RuecklaufMittelC);
                    Assert.Null(z.UebergabeBegrenztH);
                }
            }
            Assert.NotNull(eins.VorlaufMittelC);
            Assert.NotNull(eins.RuecklaufMittelC);
            Assert.Equal(DbWerte.UEBERGABE_RADIATOR, eins.UebergabeArt);
            for (int i = 0; i < 3; i++)
            {
                ErgebnisZoneModel a = eins.Zonen[i], b = zwei.Zonen[i];
                Assert.Equal(a.HeizwaermeMwh, b.HeizwaermeMwh);
                Assert.Equal(a.VorlaufMittelC, b.VorlaufMittelC);
                Assert.Equal(a.RuecklaufMittelC, b.RuecklaufMittelC);
                Assert.Equal(a.UebergabeBegrenztH, b.UebergabeBegrenztH);
            }
            Assert.Equal(eins.VorlaufMittelC, zwei.VorlaufMittelC);
            Assert.Equal(eins.RuecklaufMittelC, zwei.RuecklaufMittelC);
            _aus.WriteLine("1052 gekoppelt: Gebäude Vorlauf {0} °C, Rücklauf {1} °C; {2}", F(eins.VorlaufMittelC ?? double.NaN),
                           F(eins.RuecklaufMittelC ?? double.NaN), string.Join("; ", eins.Zonen.Select(z =>
                               z.Bezeichner + ": " + (z.VorlaufMittelC.HasValue
                                   ? F(z.VorlaufMittelC.Value) + "/" + F(z.RuecklaufMittelC.Value) + " °C, begrenzt " + F(z.UebergabeBegrenztH.Value, "F1") + " h"
                                   : "NULL"))));
        }
    }
}
