using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Gebäude-Stepper</b> (Entwurf AK3, Architektur 2.2, Welle W1) — die Proben der Welle:
    /// <list type="number">
    /// <item><b>Stepper bitgleich zu <c>Laufen</c></b>: Der Jahreslauf über den Stepper
    /// (<see cref="Vdi6007Rechenweg.Laufen"/>) gegen die Jahresschleife ohne Stepper
    /// (<see cref="JahresschleifeOhneStepper"/>, der Text des Jahreslaufs vor W1 aus den Bausteinen
    /// des <see cref="Zonenlauf"/>) — ideal, AK1 (Heiz- und Kälteseite), Fahrplan (AK2), mit
    /// Aufheizplan; Mehrzonen ideal, AK1z und mit Fahrplan gegen <see cref="Zonenschleife.Jahr"/>.</item>
    /// <item><b>Probeschritt + Rücksetzen + Wiederholen bitgleich zu einem Schritt</b>: je Stunde ein
    /// Probeschritt mit angepasstem Rand (Schranke 0), ein zweiter Probeschritt ohne Rücksetzen dazwischen,
    /// dann der Schritt des Bestands — dasselbe Jahr, Bit für Bit, samt Zählern der Zonenschleife.</item>
    /// <item><b>Determinismus</b>: zwei Läufe mit Probeschritten liefern dieselben Bits.</item>
    /// </list>
    /// Die eingefrorenen Reihen des Einzonennetzes (<c>GebaeudeEinzonennetzTests</c>) halten den Weg
    /// zusätzlich gegen die Abdrücke von vor W1.
    /// </summary>
    public class GebaeudeStepperTests
    {
        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        public static IEnumerable<object[]> Faelle() => GebaeudeEinzonennetzTests.FallDaten();

        // =====================================================================
        //  Das Orakel: die Jahresschleife ohne Stepper
        // =====================================================================

        /// <summary>
        /// Der Jahreslauf einer Einzelzone, wie er vor dem Stepper stand: Vorlauf von
        /// <see cref="Vdi6007Rechenweg.VORLAUF_H"/> Stunden, dann das Jahr — dieselben Bausteine, ohne
        /// Sicherung, Probeschritt und Festschreiben.
        /// </summary>
        private static GebaeudeModellErgebnis JahresschleifeOhneStepper(ZonenEingang zone, int index = 0, int idGebaeude = 1)
        {
            var lauf = new Zonenlauf(zone);
            ReadOnlySpan<double> keine = ReadOnlySpan<double>.Empty;
            int start = 8760 - Vdi6007Rechenweg.VORLAUF_H;
            lauf.Beginnen(Vdi6007Rechenweg.VorlaufStartwertC(zone.Eingang, start));
            for (int h = start; h < 8760; h++)
            {
                bool sommer = lauf.Sommerlueftung(h);
                bool nacht = lauf.Nachtauskuehlung(h);
                Stundenrand r = zone.Rand(h, sommer, keine, nacht);
                Stundenergebnis v = lauf.Modell.Schritt(in r);
                lauf.VorlaufUebernehmen(h, in v);
            }
            for (int h = 0; h < 8760; h++)
            {
                bool sommer = lauf.Sommerlueftung(h);
                bool nacht = lauf.Nachtauskuehlung(h);
                Stundenrand r = zone.Rand(h, sommer, keine, nacht);
                Stundenergebnis s = lauf.Modell.Schritt(in r);
                lauf.Uebernehmen(h, sommer, nacht, in s);
            }
            return lauf.Ergebnis(index, idGebaeude);
        }

        /// <summary>Die Anpassung des Probeschritts: Schranke 0 (Sperrzeit) — wirkt mit wirksamer Kopplung.</summary>
        private static Stundenrand Gesperrt(int zone, int h, in Stundenrand r)
            => r.MitVerfuegbarkeit(0.0, Verfuegbarkeitsgrund.Sperrzeit, double.NaN);

        /// <summary>
        /// Das Jahr über den Stepper mit Probeschritten: je Stunde ein Probeschritt mit Schranke 0, ein
        /// zweiter mit Schranke 0 ohne Rücksetzen dazwischen (der Stepper beginnt wieder am Stundenanfang),
        /// ein ausdrückliches Rücksetzen und dann der Schritt des Bestands, der festgeschrieben wird.
        /// </summary>
        private static GebaeudeModellErgebnis[] MitProben(GebaeudeStepper s, out int abweichend)
        {
            abweichend = 0;
            s.Beginnen();
            for (int h = 0; h < 8760; h++)
            {
                double probe = s.Schritt(h, Gesperrt).Sum(e => e.HeizleistungW);
                double nochmal = s.Schritt(h, Gesperrt).Sum(e => e.HeizleistungW);
                Assert.Equal(Bits(probe), Bits(nochmal));
                s.Zuruecksetzen();
                double bestand = s.Schritt(h).Sum(e => e.HeizleistungW);
                if (Bits(probe) != Bits(bestand)) abweichend++;
                s.Festschreiben(h);
            }
            return s.Abschluss(0, 1);
        }

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static void Bitgleich(string wer, GebaeudeModellEingang e1, GebaeudeModellErgebnis r1,
                                      GebaeudeModellEingang e2, GebaeudeModellErgebnis r2)
        {
            List<(string Name, double[] Werte)> a = GebaeudeEinzonennetzTests.Reihen(e1, r1);
            List<(string Name, double[] Werte)> b = GebaeudeEinzonennetzTests.Reihen(e2, r2);
            Assert.Equal(a.Select(x => x.Name), b.Select(x => x.Name));
            for (int i = 0; i < a.Count; i++)
                Assert.True(GebaeudeEinzonennetzTests.Bilden(a[i].Werte).Sha256 == GebaeudeEinzonennetzTests.Bilden(b[i].Werte).Sha256,
                            wer + ": Reihe " + a[i].Name + " weicht ab.");
            Assert.True(GebaeudeEinzonennetzTests.Bilden(r1.HeizleistungMaxAnteil).Sha256 == GebaeudeEinzonennetzTests.Bilden(r2.HeizleistungMaxAnteil).Sha256,
                        wer + ": Reihe HeizleistungMaxAnteil weicht ab.");
            Assert.Equal(Bits(r1.HeizleistungMaxStundenH), Bits(r2.HeizleistungMaxStundenH));
            Assert.Equal(Bits(r1.VerbrauchAltKwh), Bits(r2.VerbrauchAltKwh));
            Assert.Equal(r1.StundenMitUmschaltung, r2.StundenMitUmschaltung);
            Assert.Equal(r1.FahrplanBegrenzt, r2.FahrplanBegrenzt);
            Assert.Equal(r1.Aufheizung == null, r2.Aufheizung == null);
            if (r1.Aufheizung != null)
            {
                Assert.Equal(r1.Aufheizung with { Rampenmaske = null, Nachweisbandtage = null },
                             r2.Aufheizung with { Rampenmaske = null, Nachweisbandtage = null });
                Assert.Equal(r1.Aufheizung.Rampenmaske, r2.Aufheizung.Rampenmaske);
                Assert.Equal(r1.Aufheizung.Nachweisbandtage, r2.Aufheizung.Nachweisbandtage);
            }
        }

        // =====================================================================
        //  Einzone
        // =====================================================================

        [Theory]
        [MemberData(nameof(Faelle))]
        public void Der_Jahreslauf_ueber_den_Stepper_ist_bitgleich_zur_Jahresschleife(string fall)
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(fall);
            GebaeudeModellErgebnis soll = JahresschleifeOhneStepper(ZonenEingang.Einzeln(e));
            Bitgleich(fall + " (Laufen)", e, soll, e, Vdi6007Rechenweg.Laufen(e, 0, 1));
            Bitgleich(fall + " (Probeschritte)", e, soll, e,
                      Assert.Single(MitProben(GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e)), out _)));
        }

        /// <summary>Das gekoppelte Probegebäude (Radiator, Heizkurve, ohne Nachtabsenkung) mit einer Sperrreihe (AK2).</summary>
        private static GebaeudeModellEingang Gekoppelt(Anlagenverfuegbarkeit[] verfuegbarkeit)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Nachtabsenkung = g.Raumsolltemperatur_Tag;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE,
                Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            e.Verfuegbarkeit = verfuegbarkeit;
            return e;
        }

        private static Anlagenverfuegbarkeit[] Sperre()
        {
            var sperre = new Anlagenverfuegbarkeit[8760];
            for (int h = 0; h < 8760; h++)
            {
                bool gesperrt = h < 59 * 24 && h % 24 >= 8 && h % 24 < 16;
                sperre[h] = gesperrt
                    ? new Anlagenverfuegbarkeit(0.0, double.NaN, Verfuegbarkeitsgrund.Sperrzeit)
                    : new Anlagenverfuegbarkeit(double.NaN, double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung);
            }
            return sperre;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AK1_und_Fahrplan_rechnen_ueber_den_Stepper_bitgleich(bool fahrplan)
        {
            GebaeudeModellEingang e = Gekoppelt(fahrplan ? Sperre() : null);
            Assert.True(e.KopplungWirksam);
            Assert.Equal(fahrplan, e.FahrplanWirksam);
            GebaeudeModellErgebnis soll = JahresschleifeOhneStepper(ZonenEingang.Einzeln(e));
            Bitgleich("Laufen", e, soll, e, Vdi6007Rechenweg.Laufen(e, 0, 1));
            GebaeudeModellErgebnis ist = Assert.Single(MitProben(GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e)), out int abweichend));
            Bitgleich("Probeschritte", e, soll, e, ist);
            // Die Probe hat wirklich anders gerechnet: Schranke 0 heizt nicht, wo der Bestand heizt.
            Assert.True(abweichend > 1000, "Probeschritte mit anderer Heizleistung: " + abweichend);
            if (fahrplan) Assert.Contains(true, ist.FahrplanBegrenzt);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Mit_Aufheizplan_rechnet_der_Stepper_bitgleich(bool grenze)
        {
            ProjektGebaeudeModel g = Bueroprobe.Gebaeude(grenze ? Bueroprobe.GrenzeKw(1.10) : (double?)null);
            SolardatenModel[] reihe = grenze ? Bueroprobe.MitKaelteeinbruch() : null;
            Bueroprobe.Lauf lauf = Bueroprobe.Rechnen(g, Bueroprobe.An(), reihe);
            Assert.NotNull(lauf.Ergebnis.Aufheizung);
            GebaeudeModellErgebnis soll = JahresschleifeOhneStepper(lauf.Zone, 0, g.ID_Gebaeude);
            Bitgleich(grenze ? "Grenze" : "Ziel", lauf.Eingang, soll, lauf.Eingang, lauf.Ergebnis);
        }

        [Fact]
        public void Der_Stepper_ist_deterministisch()
        {
            GebaeudeModellEingang e = Gekoppelt(Sperre());
            GebaeudeModellErgebnis a = MitProben(GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e)), out int na)[0];
            GebaeudeModellErgebnis b = MitProben(GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e)), out int nb)[0];
            Assert.Equal(na, nb);
            Bitgleich("zweiter Lauf", e, a, e, b);
        }

        [Fact]
        public void Der_Stepper_haelt_die_Reihenfolge()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            GebaeudeStepper s = GebaeudeStepper.Einzone(ZonenEingang.Einzeln(e));
            Assert.Throws<InvalidOperationException>(() => s.Schritt(0));
            s.Beginnen();
            Assert.Throws<InvalidOperationException>(() => s.Beginnen());
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Schritt(1));
            Assert.Throws<InvalidOperationException>(() => s.Festschreiben(0));
            s.Schritt(0);
            s.Zuruecksetzen();
            Assert.Throws<InvalidOperationException>(() => s.Festschreiben(0));
            s.Schritt(0);
            s.Festschreiben(0);
            Assert.Equal(1, s.NaechsteStunde);
            Assert.Throws<InvalidOperationException>(() => s.Abschluss(0, 1));
            s.Jahr();
            Assert.Equal(8760, s.NaechsteStunde);
            Assert.Single(s.Abschluss(0, 1));
        }

        // =====================================================================
        //  Mehrzonen
        // =====================================================================

        private static ProjektGebaeudeModel Dreizonen(bool gekoppelt)
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            if (!gekoppelt) return g;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = false;
            g.Auslegung_Vorlauf = 55.0;
            g.Auslegung_Ruecklauf = 45.0;
            g.Heizung_Strahlungsanteil = 0.3;
            return g;
        }

        private static IReadOnlyList<ZonenEingang> Zonen(bool gekoppelt, bool fahrplan)
        {
            ProjektGebaeudeModel g = Dreizonen(gekoppelt);
            IReadOnlyList<ZonenEingang> zonen = ZonenEingang.Bauen(g, ZonenschleifeTests.KlimaDes(), false,
                                                                   gekoppelt ? DbWerte.ANLAGENKOPPLUNG_AK1 : null);
            if (fahrplan)
                foreach (ZonenEingang z in zonen) z.Eingang.Verfuegbarkeit = Sperre();
            return zonen;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Mehrzonen_mit_Probeschritten_sind_bitgleich_zur_Zonenschleife(bool gekoppelt, bool fahrplan)
        {
            IReadOnlyList<ZonenEingang> z1 = Zonen(gekoppelt, fahrplan);
            var soll = new Zonenschleife(z1, "Probe");
            soll.Vorlauf();
            soll.Jahr();
            GebaeudeModellErgebnis[] a = soll.Zonenergebnisse(0, 1);

            IReadOnlyList<ZonenEingang> z2 = Zonen(gekoppelt, fahrplan);
            var ist = new Zonenschleife(z2, "Probe");
            GebaeudeStepper stepper = GebaeudeStepper.Mehrzonen(ist);
            Assert.Equal(z2.Count, stepper.Zonenzahl);
            GebaeudeModellErgebnis[] b = MitProben(stepper, out int abweichend);

            Assert.Equal(a.Length, b.Length);
            for (int z = 0; z < a.Length; z++)
                Bitgleich("Zone " + z, z1[z].Eingang, a[z], z2[z].Eingang, b[z]);
            Assert.Equal(soll.IterierteStunden, ist.IterierteStunden);
            Assert.Equal(soll.DurchlaeufeSumme, ist.DurchlaeufeSumme);
            Assert.Equal(soll.DurchlaeufeMax, ist.DurchlaeufeMax);
            Assert.Equal(soll.Musterwechsel, ist.Musterwechsel);
            Assert.Equal(soll.MusterNichtHaltbar, ist.MusterNichtHaltbar);
            Assert.Equal(soll.MusterwechselJeZone, ist.MusterwechselJeZone);
            Assert.Equal(soll.DurchlaeufeMaxJeZone, ist.DurchlaeufeMaxJeZone);
            Assert.Equal(soll.StundenMitHeizen, ist.StundenMitHeizen);
            Assert.Equal(soll.StundenMitUmschaltung, ist.StundenMitUmschaltung);
            Assert.True(soll.IterierteStunden > 0);
            if (gekoppelt) Assert.True(abweichend > 1000, "Probeschritte mit anderer Heizleistung: " + abweichend);
        }

        [Fact]
        public void Die_Mehrzonenrechnung_laeuft_ueber_den_Stepper_bitgleich()
        {
            ProjektGebaeudeModel g = Dreizonen(true);
            Mehrzonenergebnis m = Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), false, DbWerte.ANLAGENKOPPLUNG_AK1, 0, g.ID_Gebaeude);

            IReadOnlyList<ZonenEingang> zonen = Zonen(true, false);
            var soll = new Zonenschleife(zonen, "Probe");
            soll.Vorlauf();
            soll.Jahr();
            GebaeudeModellErgebnis[] a = soll.Zonenergebnisse(0, g.ID_Gebaeude);
            Assert.Equal(a.Length, m.Zonen.Count);
            for (int z = 0; z < a.Length; z++)
                Assert.Equal(ZonenschleifeTests.Abdruck(a[z].HeizlastW), ZonenschleifeTests.Abdruck(m.Zonen[z].HeizlastW));
            Assert.Equal(soll.IterierteStunden, m.Schleife.IterierteStunden);
        }
    }
}
