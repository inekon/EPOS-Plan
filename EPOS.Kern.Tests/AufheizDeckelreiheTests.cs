using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Deckelreihe je Stunde und die Zustandssicherung der Zone</b> (Entwurf Vorheizrampe Fassung 2, Welle V1):
    /// Ohne Reihe und mit einer Reihe „keine Grenze" rechnet jeder Weg bitgleich (Einzone, Mehrzonen, AK1, AK3-Kreis);
    /// eine Grenze in einzelnen Stunden kappt genau dort (Betriebsfall Heizgrenze); wirksam ist das Minimum aus Skalar
    /// <c>Heizleistung_Max</c>, Reihe und der Schranke der Verfügbarkeit (AK2); der Plan bringt die Reihe in den Stepper
    /// des AK3-Kreises; sichern → n Stunden → setzen → dieselben n Stunden ist bitgleich.
    /// </summary>
    public class AufheizDeckelreiheTests
    {
        private const int STUNDEN = 8760;

        /// <summary>Die Stunden mit eigener Grenze: im Winter, vor dem Vorlauf des Jahresendes.</summary>
        private static readonly int[] Gekappt = { 10 * 24 + 7, 12 * 24 + 8, 20 * 24 + 6, 33 * 24 + 7, 41 * 24 + 9 };

        private static long Bits(double x) => BitConverter.DoubleToInt64Bits(x);

        private static double[] Reihe(double wert) => Enumerable.Repeat(wert, STUNDEN).ToArray();

        public static IEnumerable<object[]> Faelle() => GebaeudeEinzonennetzTests.FallDaten();

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Die Jahresschleife über den Zonenlauf (Muster <c>GebaeudeStepperTests</c>), je Stunde Ergebnis und Fallfolge.</summary>
        private static (Stundenergebnis[] Stunden, bool[] Heizgrenze) Jahr(ZonenEingang zone)
        {
            var lauf = new Zonenlauf(zone);
            ReadOnlySpan<double> keine = ReadOnlySpan<double>.Empty;
            int start = STUNDEN - Vdi6007Rechenweg.VORLAUF_H;
            lauf.Beginnen(Vdi6007Rechenweg.VorlaufStartwertC(zone.Eingang, start));
            for (int h = start; h < STUNDEN; h++)
            {
                bool sommer = lauf.Sommerlueftung(h);
                bool nacht = lauf.Nachtauskuehlung(h);
                Stundenrand r = zone.Rand(h, sommer, keine, nacht);
                Stundenergebnis v = lauf.Modell.Schritt(in r);
                lauf.VorlaufUebernehmen(h, in v);
            }
            var stunden = new Stundenergebnis[STUNDEN];
            var grenze = new bool[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                bool sommer = lauf.Sommerlueftung(h);
                bool nacht = lauf.Nachtauskuehlung(h);
                Stundenrand r = zone.Rand(h, sommer, keine, nacht);
                stunden[h] = lauf.Modell.Schritt(in r);
                grenze[h] = lauf.Modell.LetzteFallfolge.Contains(Betriebsfall.Heizgrenze);
                lauf.Uebernehmen(h, sommer, nacht, in stunden[h]);
            }
            return (stunden, grenze);
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
            Assert.True(GebaeudeEinzonennetzTests.Bilden(r1.HeizleistungMaxAnteil).Sha256
                        == GebaeudeEinzonennetzTests.Bilden(r2.HeizleistungMaxAnteil).Sha256, wer + ": HeizleistungMaxAnteil weicht ab.");
            Assert.Equal(Bits(r1.HeizleistungMaxStundenH), Bits(r2.HeizleistungMaxStundenH));
            Assert.Equal(Bits(r1.VerbrauchAltKwh), Bits(r2.VerbrauchAltKwh));
        }

        private static GebaeudeModellEingang Gekoppelt(Anlagenverfuegbarkeit[] verfuegbarkeit = null)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            g.Heizkurve_Aktiv = true;
            GebaeudeModellEingang e = GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang),
                Vdi6007Probe.Wochenende(), Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false,
                DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            e.Verfuegbarkeit = verfuegbarkeit;
            return e;
        }

        /// <summary>Die Reihe mit Grenze <paramref name="anteil"/> × Heizleistung des Laufs ohne Reihe in den Stunden <see cref="Gekappt"/>.</summary>
        private static double[] Deckel(Stundenergebnis[] ohne, double anteil)
        {
            double[] reihe = Reihe(double.NaN);
            foreach (int h in Gekappt)
            {
                Assert.True(ohne[h].HeizleistungW > 100.0, "Stunde " + h + " heizt im Lauf ohne Reihe nicht.");
                reihe[h] = anteil * ohne[h].HeizleistungW;
            }
            return reihe;
        }

        // =====================================================================
        //  (a) Ohne Grenze bitgleich
        // =====================================================================

        [Theory]
        [MemberData(nameof(Faelle))]
        public void Einzone_mit_Reihe_ohne_Grenze_rechnet_bitgleich(string fall)
        {
            GebaeudeModellEingang ohne = GebaeudeEinzonennetzTests.Eingang(fall);
            GebaeudeModellEingang nan = GebaeudeEinzonennetzTests.Eingang(fall);
            GebaeudeModellEingang unendlich = GebaeudeEinzonennetzTests.Eingang(fall);
            nan.HeizleistungMaxReiheSetzen(Reihe(double.NaN));
            unendlich.HeizleistungMaxReiheSetzen(Reihe(double.PositiveInfinity));
            GebaeudeModellErgebnis soll = Vdi6007Rechenweg.Laufen(ohne, 0, 1);
            Bitgleich(fall + " (NaN)", ohne, soll, nan, Vdi6007Rechenweg.Laufen(nan, 0, 1));
            Bitgleich(fall + " (+∞)", ohne, soll, unendlich, Vdi6007Rechenweg.Laufen(unendlich, 0, 1));
        }

        [Fact]
        public void Mehrzonen_mit_Reihe_ohne_Grenze_rechnen_bitgleich()
        {
            ProjektGebaeudeModel g = AufheizMehrzonenTests.Dreizonen();
            IReadOnlyList<ZonenEingang> ohne = ZonenEingang.Bauen(g, AufheizMehrzonenTests.Klima());
            IReadOnlyList<ZonenEingang> mit = ZonenEingang.Bauen(g, AufheizMehrzonenTests.Klima());
            foreach (ZonenEingang z in mit) z.Eingang.HeizleistungMaxReiheSetzen(Reihe(double.NaN));
            GebaeudeModellErgebnis[] a = Mehrzonenjahr(ohne);
            GebaeudeModellErgebnis[] b = Mehrzonenjahr(mit);
            Assert.Equal(a.Length, b.Length);
            Assert.Contains(a, r => r.HeizlastW.Sum() > 0.0);
            for (int z = 0; z < a.Length; z++)
                Bitgleich("Zone " + z, ohne[z].Eingang, a[z], mit[z].Eingang, b[z]);
        }

        private static GebaeudeModellErgebnis[] Mehrzonenjahr(IReadOnlyList<ZonenEingang> zonen)
        {
            GebaeudeStepper s = GebaeudeStepper.Mehrzonen(new Zonenschleife(zonen, "Dreizonen"));
            s.Beginnen();
            s.Jahr();
            return s.Abschluss(0, 1);
        }

        // =====================================================================
        //  (b) Grenze in einzelnen Stunden
        // =====================================================================

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Grenze_in_einzelnen_Stunden_kappt_genau_dort(bool ak1)
        {
            GebaeudeModellEingang e0 = ak1 ? Gekoppelt() : GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            GebaeudeModellEingang e1 = ak1 ? Gekoppelt() : GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            Assert.Equal(ak1, e1.KopplungWirksam);
            Assert.True(double.IsNaN(e1.HeizleistungMaxW));
            (Stundenergebnis[] ohne, bool[] grenzeOhne) = Jahr(ZonenEingang.Einzeln(e0));
            double[] reihe = Deckel(ohne, 0.5);
            e1.HeizleistungMaxReiheSetzen(reihe);
            (Stundenergebnis[] mit, bool[] grenzeMit) = Jahr(ZonenEingang.Einzeln(e1));

            // Vor der ersten Grenze Zeichen für Zeichen derselbe Lauf.
            for (int h = 0; h < Gekappt[0]; h++)
            {
                Assert.Equal(Bits(ohne[h].HeizleistungW), Bits(mit[h].HeizleistungW));
                Assert.Equal(Bits(ohne[h].ThetaAirMittel), Bits(mit[h].ThetaAirMittel));
            }
            // In den Stunden mit Grenze: Leistung an der Grenze, Betriebsfall Heizgrenze, Kappungsanteil gezählt.
            foreach (int h in Gekappt)
            {
                Assert.True(mit[h].HeizleistungW <= reihe[h] * (1.0 + 1e-12) + 1e-9, "Stunde " + h + ": " + mit[h].HeizleistungW + " > " + reihe[h]);
                Assert.True(grenzeMit[h], "Stunde " + h + " ohne Betriebsfall Heizgrenze.");
                Assert.True(mit[h].HeizleistungMaxAnteil > 0.0, "Stunde " + h + " ohne Kappungsanteil.");
                Assert.True(mit[h].ThetaAirMittel < ohne[h].ThetaAirMittel, "Stunde " + h + ": Die Luft bleibt nicht unter dem Lauf ohne Grenze.");
            }
            // Außerhalb der Stunden greift keine Grenze — die Reihe trägt dort „keine Grenze".
            for (int h = 0; h < STUNDEN; h++)
            {
                if (Array.IndexOf(Gekappt, h) >= 0) continue;
                Assert.Equal(grenzeOhne[h], grenzeMit[h]);
                Assert.Equal(0.0, mit[h].HeizleistungMaxAnteil);
            }
        }

        // =====================================================================
        //  (c) Minimum mit Skalar und Verfügbarkeit
        // =====================================================================

        [Fact]
        public void Wirksam_ist_das_Minimum_aus_Skalar_und_Reihe()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.LEISTUNGSGRENZE);
            double skalar = e.HeizleistungMaxW;
            Assert.Equal(1000.0 * GebaeudeEinzonennetzTests.GRENZE_KW, skalar);
            double[] reihe = Reihe(double.NaN);
            reihe[100] = 0.5 * skalar;
            reihe[101] = 2.0 * skalar;
            reihe[102] = double.PositiveInfinity;
            reihe[103] = skalar;
            reihe[104] = 0.0;
            e.HeizleistungMaxReiheSetzen(reihe);

            Assert.Equal(Bits(skalar), Bits(e.Rand(99).HeizleistungMaxW));
            Assert.Equal(Bits(0.5 * skalar), Bits(e.Rand(100).HeizleistungMaxW));
            Assert.Equal(Bits(skalar), Bits(e.Rand(101).HeizleistungMaxW));
            Assert.Equal(Bits(skalar), Bits(e.Rand(102).HeizleistungMaxW));
            Assert.Equal(Bits(skalar), Bits(e.Rand(103).HeizleistungMaxW));
            Assert.Equal(0.0, e.Rand(104).HeizleistungMaxW);

            // Ohne Skalar gilt die Reihe allein; NaN bleibt unbegrenzt.
            GebaeudeModellEingang frei = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            frei.HeizleistungMaxReiheSetzen(reihe);
            Assert.True(double.IsNaN(frei.Rand(99).HeizleistungMaxW));
            Assert.Equal(Bits(2.0 * skalar), Bits(frei.Rand(101).HeizleistungMaxW));
            Assert.True(double.IsNaN(frei.Rand(102).HeizleistungMaxW));
        }

        [Fact]
        public void Mit_Verfuegbarkeit_gilt_die_kleinste_der_Grenzen()
        {
            const double DECKEL_W = 3000.0;
            var v = new Anlagenverfuegbarkeit[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) v[h] = new Anlagenverfuegbarkeit(double.NaN, double.NaN, Verfuegbarkeitsgrund.KeineBegrenzung);
            v[200] = new Anlagenverfuegbarkeit(1.0, double.NaN, Verfuegbarkeitsgrund.Leistungsgrenze);    // 1 kW unter dem Deckel
            v[201] = new Anlagenverfuegbarkeit(10.0, double.NaN, Verfuegbarkeitsgrund.Leistungsgrenze);   // 10 kW über dem Deckel
            v[202] = new Anlagenverfuegbarkeit(2.0, double.NaN, Verfuegbarkeitsgrund.Leistungsgrenze);    // ohne Deckel in der Stunde
            GebaeudeModellEingang e = Gekoppelt(v);
            Assert.True(e.FahrplanWirksam);
            double[] reihe = Reihe(double.NaN);
            reihe[200] = DECKEL_W;
            reihe[201] = DECKEL_W;
            e.HeizleistungMaxReiheSetzen(reihe);

            Stundenrand r200 = e.Rand(200);
            Assert.True(r200.VerfuegbarkeitIstGrenze);
            Assert.Equal(1000.0, r200.HeizleistungMaxW);
            Stundenrand r201 = e.Rand(201);
            Assert.False(r201.VerfuegbarkeitIstGrenze);
            Assert.Equal(DECKEL_W, r201.HeizleistungMaxW);
            Stundenrand r202 = e.Rand(202);
            Assert.True(r202.VerfuegbarkeitIstGrenze);
            Assert.Equal(2000.0, r202.HeizleistungMaxW);
            Assert.True(double.IsNaN(e.Rand(203).HeizleistungMaxW));
        }

        [Fact]
        public void Die_Reihe_wird_geprueft_und_kopiert()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            Assert.Throws<ArgumentException>(() => e.HeizleistungMaxReiheSetzen(new double[24]));
            double[] negativ = Reihe(double.NaN);
            negativ[5] = -1.0;
            Assert.Throws<ArgumentException>(() => e.HeizleistungMaxReiheSetzen(negativ));
            double[] minusUnendlich = Reihe(double.NaN);
            minusUnendlich[5] = double.NegativeInfinity;
            Assert.Throws<ArgumentException>(() => e.HeizleistungMaxReiheSetzen(minusUnendlich));
            Assert.Null(e.HeizleistungMaxReiheW);

            double[] reihe = Reihe(double.NaN);
            reihe[7] = 500.0;
            e.HeizleistungMaxReiheSetzen(reihe);
            reihe[7] = 1.0;
            Assert.Equal(500.0, e.Rand(7).HeizleistungMaxW);
            e.HeizleistungMaxReiheSetzen(null);
            Assert.Null(e.HeizleistungMaxReiheW);
            Assert.True(double.IsNaN(e.Rand(7).HeizleistungMaxW));
        }

        // =====================================================================
        //  (d) AK3: der Plan bringt die Reihe in den Stepper des Kreises
        // =====================================================================

        /// <summary>
        /// Der Kreis wie im AK3-Weg (<c>Vdi6007Rechenweg</c>, AK3-W3b): ein frischer Stepper aus DEMSELBEN Eingang, der Plan an
        /// der Kreiszone, ein Kessel, der nie begrenzt.
        /// </summary>
        private static GebaeudeModellErgebnis Kreisjahr(GebaeudeModellEingang quelle, Aufheizplan plan)
        {
            ZonenEingang kreiszone = ZonenEingang.Einzeln(quelle);
            if (plan != null) kreiszone.AufheizplanSetzen(plan);
            GebaeudeStepper s = GebaeudeStepper.Einzone(kreiszone);
            s.Beginnen();
            var kreis = new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Kreis", s, 1.0) },
                new IErzeugerkapazitaet[] { new FesteKapazitaet(new Fahrplanerzeuger { Bezeichner = "Kessel", NennleistungKw = 1000.0 }) },
                null);
            for (int h = 0; h < STUNDEN; h++)
            {
                kreis.Stunde(h, double.NaN, default);
                kreis.Festschreiben(h);
            }
            return Zonenrechnung.Abschluss(s, 0, 1);
        }

        [Fact]
        public void AK3_Kreis_sieht_die_Reihe_des_Plans()
        {
            GebaeudeModellEingang ohne = Gekoppelt();
            GebaeudeModellEingang leer = Gekoppelt();
            GebaeudeModellEingang mit = Gekoppelt();
            Aufheizplan plan = Aufheizoptimierung.Planen(Aufheizzone.Aus(ZonenEingang.Einzeln(mit)), AufheizMehrzonenTests.An());

            GebaeudeModellErgebnis soll = Kreisjahr(ohne, null);
            Assert.True(soll.HeizlastW.Sum() > 0.0);

            // Reihe „keine Grenze" über den Plan: bitgleich.
            Aufheizoptimierung.PlanSetzen(ZonenEingang.Einzeln(leer), plan with { Deckelreihe = Reihe(double.NaN) });
            Assert.NotNull(leer.HeizleistungMaxReiheW);
            Bitgleich("AK3 ohne Grenze", ohne, soll, leer, Kreisjahr(leer, plan with { Deckelreihe = Reihe(double.NaN) }));

            // Reihe mit Grenze über den Plan: der Kreis kappt in genau diesen Stunden.
            double[] reihe = Reihe(double.NaN);
            foreach (int h in Gekappt)
            {
                Assert.True(soll.HeizlastW[h] > 100.0, "Stunde " + h + " heizt im Kreis ohne Reihe nicht.");
                reihe[h] = 0.5 * soll.HeizlastW[h];
            }
            Aufheizplan gedeckelt = plan with { Deckelreihe = reihe };
            Aufheizoptimierung.PlanSetzen(ZonenEingang.Einzeln(mit), gedeckelt);
            GebaeudeModellErgebnis ist = Kreisjahr(mit, gedeckelt);
            for (int h = 0; h < Gekappt[0]; h++) Assert.Equal(Bits(soll.HeizlastW[h]), Bits(ist.HeizlastW[h]));
            foreach (int h in Gekappt)
            {
                Assert.True(ist.HeizlastW[h] <= reihe[h] * (1.0 + 1e-12) + 1e-9, "Stunde " + h + ": " + ist.HeizlastW[h] + " > " + reihe[h]);
                Assert.True(ist.HeizleistungMaxAnteil[h] > 0.0, "Stunde " + h + " ohne Kappungsanteil.");
            }
        }

        // =====================================================================
        //  (e) Sichern und setzen
        // =====================================================================

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Sichern_n_Schritte_Setzen_rechnet_dieselben_Schritte_bitgleich(bool ak1)
        {
            GebaeudeModellEingang e = ak1 ? Gekoppelt() : GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            var modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            Assert.Null(modell.ZustandSichern().Muster);
            modell.Zuruecksetzen(18.0);
            for (int h = 0; h < 30 * 24; h++) modell.Schritt(e.Rand(h));

            const int N = 48;
            int von = 30 * 24;
            Zonenzustand z = modell.ZustandSichern();
            Assert.NotNull(z.Muster);
            Betriebsfall[] folgeVorher = modell.LetzteFallfolge;
            var erster = new Stundenergebnis[N];
            for (int i = 0; i < N; i++) erster[i] = modell.Schritt(e.Rand(von + i));

            modell.ZustandSetzen(in z);
            Assert.Equal(Bits(z.ThetaMAw), Bits(modell.ThetaMAw));
            Assert.Equal(Bits(z.ThetaMIw), Bits(modell.ThetaMIw));
            Assert.Equal(folgeVorher, modell.LetzteFallfolge);
            for (int i = 0; i < N; i++)
            {
                Stundenergebnis s = modell.Schritt(e.Rand(von + i));
                Assert.Equal(Bits(erster[i].HeizleistungW), Bits(s.HeizleistungW));
                Assert.Equal(Bits(erster[i].ThetaAirMittel), Bits(s.ThetaAirMittel));
                Assert.Equal(Bits(erster[i].ThetaMAwEnde), Bits(s.ThetaMAwEnde));
                Assert.Equal(Bits(erster[i].ThetaMIwEnde), Bits(s.ThetaMIwEnde));
                Assert.Equal(Bits(erster[i].VorlaufC), Bits(s.VorlaufC));
            }

            // Ein Zustand in ein zweites Modell derselben Parameter: dieselben Stunden.
            var zweites = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            zweites.ZustandSetzen(in z);
            for (int i = 0; i < N; i++)
                Assert.Equal(Bits(erster[i].HeizleistungW), Bits(zweites.Schritt(e.Rand(von + i)).HeizleistungW));
        }

        [Fact]
        public void Ein_leerer_Zustand_wird_abgelehnt_und_aendert_nichts()
        {
            GebaeudeModellEingang e = GebaeudeEinzonennetzTests.Eingang(GebaeudeEinzonennetzTests.IDEAL);
            var modell = new Zonenmodell2K(e.Parameter, e.Bezeichnung);
            modell.Zuruecksetzen(19.0, 17.0);
            Assert.Throws<GebaeudeModellException>(() => modell.ZustandSetzen(new Zonenzustand(double.NaN, 17.0, null)));
            Assert.Equal(19.0, modell.ThetaMAw);
            Assert.Equal(17.0, modell.ThetaMIw);
        }
    }
}
