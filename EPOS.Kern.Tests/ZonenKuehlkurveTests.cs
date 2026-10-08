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
    /// <b>KZ2 — die Kühlkurve im Mehrzonenweg</b> (Entwurf KK 2.7, 2.8; Festlegungen 14–17; E106 Q-KK-4 (b), Q-KK-7 (a); E107)
    /// — ohne Datenbank, an zwei Hälften des Probegebäudes mit Gebläsekonvektor (Zone 1 heizt auf 20 °C und kühlt auf 24 °C,
    /// Zone 2 heizt auf 14 °C und kühlt auf 16 °C): der Bezug der Kurvenreihe (niedrigster Kühlsollwert der Zonen, die in der
    /// Stunde kühlen, auch für den Auslegungsweg), der Kreis mit gekühlten Zonen, Tage mit Heiz- und Kühlzone, „Schalter aus
    /// bitgleich“, die Konvergenz der frei gerechneten Kühlstunden und das Orakel <b>O3kz</b> (Führungsgröße größte
    /// Überschreitung, Fixpunkt, Konvergenz im Mehrzonenweg).
    /// </summary>
    public class ZonenKuehlkurveTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public ZonenKuehlkurveTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose() => _kultur.Dispose();

        private const double ANLAGE_C = 14.0, ERZEUGER_MIN_C = 6.0, GROSS_KW = 1.0e6;

        /// <summary>Toleranzen des Orakels O3kz wie O2kk: Absenkung und Raumluft am Fixpunkt.</summary>
        private const double TOL_ABSENKUNG_K = 0.1, TOL_THETA_K = 0.05;

        // =====================================================================
        //  Die Vorrichtung
        // =====================================================================

        /// <summary>Zwei Hälften, gekühlt mit Gebläsekonvektor (7/12 °C, ohne Vorlaufgrenze); die Kühlkurve nach Wunsch.</summary>
        private static ProjektGebaeudeModel Zwei(double? kK = null, bool kurve = true, string weg = null, double? aussen = null,
                                                 double? nennEinsKw = null, double kuehlEinsC = 24.0)
        {
            ProjektGebaeudeModel g = ZonenschleifeTests.Haelften(Trennflaechenzuordnung.Aussen);
            g.Kuehlung_Aktiv = true;
            g.Kuehl_Sollwert = 22.0;
            g.Kuehlleistung_Max = 10.0;
            g.Kuehluebergabe_Aktiv = true;
            g.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_GEBLAESEKONVEKTOR;
            g.Kuehlkurve_Aktiv = kurve;
            g.Kuehlkurve_Raumeinfluss = kK;
            g.Kuehlkurve_Auslegung_Weg = weg;
            g.Kuehlkurve_Auslegung_Aussen = aussen;
            GebaeudeZonensatz a = g.Zonen[0], b = g.Zonen[1];
            g.Zonen = new[]
            {
                new GebaeudeZonensatz(a.ZonenId, a.Bezeichnung, a.Bauteile, a.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: a.Nutzflaeche_M2, SollTag: 20.0, SollNacht: 20.0, KuehlSollwert: kuehlEinsC,
                                                       KuehlUebergabeLeistungNennKw: nennEinsKw), a.Rang),
                new GebaeudeZonensatz(b.ZonenId, b.Bezeichnung, b.Bauteile, b.Nutzflaeche_M2,
                                      new Zoneneingaben(Nutzflaeche: b.Nutzflaeche_M2, SollTag: 14.0, SollNacht: 14.0, KuehlSollwert: 16.0), b.Rang),
            };
            return g;
        }

        private static Mehrzonenergebnis Rechnen(ProjektGebaeudeModel g, string stufe, bool schalter)
        {
            using (KuehlkurveKernschalter.Schalten(schalter))
                return Zonenrechnung.Rechnen(g, ZonenschleifeTests.KlimaDes(), true, stufe, 0, g.ID_Gebaeude,
                                             kuehlVorlaufAnlageC: ANLAGE_C, kuehlErzeugerMinC: ERZEUGER_MIN_C);
        }

        private static void Bitgleich(double[] a, double[] b, string was)
        {
            for (int h = 0; h < 8760; h++)
                Assert.True(BitConverter.DoubleToInt64Bits(a[h]) == BitConverter.DoubleToInt64Bits(b[h]),
                            was + ", Stunde " + h + ": " + a[h].ToString("R", CultureInfo.InvariantCulture) + " ≠ " +
                            b[h].ToString("R", CultureInfo.InvariantCulture));
        }

        private static void ErgebnisBitgleich(Mehrzonenergebnis a, Mehrzonenergebnis b)
        {
            Bitgleich(a.Gebaeude.HeizlastW, b.Gebaeude.HeizlastW, "Heizlast");
            Bitgleich(a.Gebaeude.KuehlbedarfKwh, b.Gebaeude.KuehlbedarfKwh, "Kühlbedarf");
            for (int z = 0; z < a.Zonen.Count; z++)
                Bitgleich(a.Zonen[z].Raumtemperatur, b.Zonen[z].Raumtemperatur, "Raumluft Zone " + z);
        }

        private static double Min(double x, double y)
        {
            bool ex = !double.IsNaN(x) && !double.IsInfinity(x), ey = !double.IsNaN(y) && !double.IsInfinity(y);
            if (ex && ey) return Math.Min(x, y);
            return ex ? x : ey ? y : double.PositiveInfinity;
        }

        // =====================================================================
        //  Bezug und Reihe
        // =====================================================================

        /// <summary>
        /// <b>Bezug</b> (2.8, Festlegung 14; E107): EINE Kurve je Kühlkreis, dieselbe Reihe für jede Zone; Auslegungsweg
        /// „eingabe“ am niedrigsten Kühlsollwert (16 °C: 17,5 °C liegt über 16 + 1 K und trägt — am Sollwert 24 °C der anderen
        /// Zone fiele sie zurück), Weg „tagesmittel“ mit derselben Mindestspanne über 16 °C. Die Reihe je Stunde am niedrigsten
        /// Kühlsollwert der Zonen, die in der Stunde kühlen — eine Zone in Heiztagesart der Zonensperre (θ_max +∞) zählt nicht.
        /// </summary>
        [Fact]
        public void Kurvenreihe_am_niedrigsten_Kuehlsollwert_der_kuehlenden_Zonen()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(weg: DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, aussen: 17.5), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            GebaeudeModellEingang a = m.Eingaenge[0].Eingang, b = m.Eingaenge[1].Eingang;
            Gebaeudekuehlkreis k = a.Gebaeudekuehlkreis;
            Assert.NotNull(k?.Kuehlkurve);
            Assert.Same(k, b.Gebaeudekuehlkreis);
            Assert.True(a.KuehlkurveWirksam && b.KuehlkurveWirksam);
            Assert.Same(a.KuehlVorlaufC, b.KuehlVorlaufC);
            Assert.Same(k.VorlaufC, a.KuehlVorlaufC);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_EINGABE, k.Kuehlkurve.AuslegungWeg);
            Assert.False(k.Kuehlkurve.AuslegungRueckfall);
            Assert.Equal(17.5, k.Kuehlkurve.AuslegungAussenC);
            Assert.Equal(ERZEUGER_MIN_C, k.KurveErzeugerC);

            Mehrzonenergebnis v = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            double soll = Kuehlkurve.AuslegungAussentemperaturC(null, null, 16.0, a.ThetaOut, out string weg, out _);
            Assert.Equal(DbWerte.KUEHLKURVE_AUSLEGUNG_TAGESMITTEL, weg);
            Assert.Equal(soll, v.Eingaenge[0].Eingang.Gebaeudekuehlkreis.Kuehlkurve.AuslegungAussenC);

            // Zone 2 am Tag 200 auf „aus“ (wie Kalender oder Heiztagesart der Zonensperre), dann die Reihe neu wie der Kreis:
            // Bezug je Stunde, Zonen mit „aus“ ausgenommen.
            for (int h = 200 * 24; h < 201 * 24; h++) b.ThetaMax[h] = double.PositiveInfinity;
            GebaeudeModellEingang.ZonenKuehlkurveBilden(k, m.Eingaenge);
            int nurZwei = 0, nurEins = 0;
            for (int h = 0; h < 8760; h++)
            {
                double s = Min(a.ThetaMax[h], b.ThetaMax[h]);
                Assert.Equal(s, k.SollwertC[h]);
                Assert.Equal(k.Kuehlkurve.VorlaufC(s, a.ThetaOut[h], ERZEUGER_MIN_C, out bool grenze), k.VorlaufC[h]);
                Assert.Equal(grenze, k.VorlaufAnGrenze[h]);
                if (double.IsPositiveInfinity(a.ThetaMax[h]) && !double.IsPositiveInfinity(b.ThetaMax[h])) nurZwei++;
                if (double.IsPositiveInfinity(b.ThetaMax[h]) && !double.IsPositiveInfinity(a.ThetaMax[h])) nurEins++;
            }
            _aus.WriteLine("Stunden mit Kühlung allein in Zone 2: {0}, allein in Zone 1: {1}", nurZwei, nurEins);
            Assert.Equal(24, nurEins);
            Assert.Equal(24.0, k.SollwertC[200 * 24 + 12]);
        }

        // =====================================================================
        //  Schalter und Stufe
        // =====================================================================

        /// <summary>
        /// <b>Schalter aus bitgleich</b> (Festlegungen 1, 13): ohne Kernschalter rechnet ein Gebäude mit <c>Kuehlkurve_Aktiv</c> und
        /// k_K Bit für Bit wie eines ohne; unter AK3 (AK1) baut der Kühlkreis keine Kurve und rechnet wie ohne
        /// <c>Kuehlkurve_Aktiv</c>; auf AK3 ohne <c>Kuehlkurve_Aktiv</c> bleibt der feste Vorlauf von KZ1.
        /// </summary>
        [Fact]
        public void Schalter_aus_und_unter_AK3_bitgleich_ohne_Kurve()
        {
            ErgebnisBitgleich(Rechnen(Zwei(kK: 3.0), DbWerte.ANLAGENKOPPLUNG_AK3, false),
                              Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK3, false));

            Mehrzonenergebnis ak1 = Rechnen(Zwei(kK: 3.0), DbWerte.ANLAGENKOPPLUNG_AK1, true);
            Assert.NotNull(ak1.Eingaenge[0].Eingang.Gebaeudekuehlkreis);
            Assert.Null(ak1.Eingaenge[0].Eingang.Gebaeudekuehlkreis.Kuehlkurve);
            ErgebnisBitgleich(ak1, Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK1, true));

            Mehrzonenergebnis fest = Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            Gebaeudekuehlkreis k = fest.Eingaenge[0].Eingang.Gebaeudekuehlkreis;
            Assert.Null(k.Kuehlkurve);
            Assert.All(k.VorlaufC, x => Assert.Equal(k.VorlaufFestC, x));
            Assert.False(fest.Eingaenge[0].Eingang.KuehlkurveWirksam);
        }

        /// <summary>
        /// <b>Kreis mit gekühlten Zonen</b> (Auftrag KZ2, Punkt 3): Die Zonen tragen keine Heizkopplung; ohne Kernschalter
        /// bekommt das Gebäude den Kreis nicht (erste Zone nicht heizgekoppelt, Bestand), mit ihm wegen der gekoppelten
        /// Kälteseite. Der Raumeinfluss entsteht aus dem Stepper des Mehrzonengebäudes; ohne Kurve keiner.
        /// </summary>
        [Fact]
        public void Kreis_mit_gekuehlten_Zonen_auch_ohne_heizgekoppelte_erste_Zone()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(kK: 2.0), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            Assert.False(m.Eingaenge[0].Eingang.KopplungWirksam);
            Assert.True(m.Eingaenge.All(z => z.Eingang.KuehlKopplungWirksam));
            using (KuehlkurveKernschalter.Schalten(false)) Assert.False(Vdi6007Rechenweg.KreisMitZonen(m.Eingaenge));
            using (KuehlkurveKernschalter.Schalten(true)) Assert.True(Vdi6007Rechenweg.KreisMitZonen(m.Eingaenge));

            KuehlRaumeinfluss k2 = KuehlRaumeinfluss.AusSteppern(new[] { Stepper(m) });
            Assert.NotNull(k2);
            Assert.Equal(2.0, k2.Kk(0));
            Assert.Null(KuehlRaumeinfluss.AusSteppern(new[] { Stepper(Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK3, true)) }));
        }

        // =====================================================================
        //  Der Kreis
        // =====================================================================

        private static GebaeudeStepper Stepper(Mehrzonenergebnis m)
        {
            GebaeudeStepper s = GebaeudeStepper.Mehrzonen(new Zonenschleife(m.Eingaenge, "Probe"));
            s.Beginnen();
            return s;
        }

        /// <summary>Eine Wärmeseite ohne Grenze (zustandsfrei).</summary>
        private sealed class Waermegross : IErzeugerkapazitaet
        {
            public string Bezeichner => "Probewärme";
            public Erzeugerangebot Abfragen(int stunde, double vorlaufC)
                => new Erzeugerangebot(GROSS_KW, GROSS_KW, Verfuegbarkeitsgrund.KeineBegrenzung, true, double.NaN, false);
        }

        /// <summary>Ein Kälteerzeuger konstanter Kapazität, der den verlangten Vorlauf liefert.</summary>
        private sealed class Kaeltekonstante : IKaelteerzeugerkapazitaet
        {
            public string Bezeichner => "Probekälte";
            public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
                => new Erzeugerangebot(GROSS_KW, GROSS_KW, Verfuegbarkeitsgrund.KeineBegrenzung, true, kuehlVorlaufC, false);
        }

        private static Anlagenkopplung Kreis(GebaeudeStepper s, KuehlRaumeinfluss k2, double festC = ANLAGE_C)
            => new Anlagenkopplung(new[] { new Kopplungsgebaeude(0, 1, "Probe", s, 1.0) },
                                   new IErzeugerkapazitaet[] { new Waermegross() }, null)
            {
                Kaelteschranke = new Kaelteschranke(new IKaelteerzeugerkapazitaet[] { new Kaeltekonstante() }, null, festC),
                KuehlRaumeinfluss = k2,
            };

        /// <summary>
        /// <b>O3kz</b> (Entwurf KK 2.8, Festlegungen 8, 14): der Raumeinfluss der Kühlkurve im Kreis eines Mehrzonengebäudes
        /// gegen die Bisektion des Fixpunkts je Stunde. F(a) = a − k_K · max(0, max_z(θ_air,z(a) − θ_max,z)) über die gekühlten,
        /// gekoppelten Zonen, die in der Stunde kühlen (Zonen mit „aus“ ausgenommen), der Rand wie im Kreis (Vorlauf der Kurve
        /// minus a, unten am Erzeuger): θ_air,z fällt mit a, F steigt streng — der Fixpunkt ist eindeutig. Der Kreis trifft ihn
        /// auf das Abbruchmaß; ab k_K 5 wechselt die Führung zwischen den Zonen; Stunden mit Heiz- und Kühlzone rechnen beide Seiten.
        /// </summary>
        [Theory]
        [InlineData(2.0)]
        [InlineData(5.0)]
        public void O3kz_Fuehrungsgroesse_groesste_Ueberschreitung_trifft_den_Fixpunkt(double kK)
        {
            // Zone 1 auf 21 °C mit knapper Kühlübergabe (0,2 kW): an warmen Tagen überschreitet sie weiter als Zone 2 — die Führung wechselt.
            Mehrzonenergebnis m = Rechnen(Zwei(kK: kK, nennEinsKw: 0.2, kuehlEinsC: 21.0), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            GebaeudeStepper s = Stepper(m);
            KuehlRaumeinfluss k2 = KuehlRaumeinfluss.AusSteppern(new[] { s });
            Anlagenkopplung kreis = Kreis(s, k2);

            const int BISEKTION_MAX = 400;
            int bisektiert = 0, fuehrtEins = 0, fuehrtZwei = 0, durchlaeufeMax = 0, gemischt = 0, festgehaltenVor = 0;
            double dAMax = 0.0, dThetaMax = 0.0;
            var max = new double[2];
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde k = kreis.Stunde(h, double.NaN, default);
                bool festgehalten = k2.StundenFestgehalten > festgehaltenVor;
                festgehaltenVor = k2.StundenFestgehalten;
                IReadOnlyList<Stundenergebnis> ist = k.Loesung[0];
                if (ist.Any(z => z.HeizleistungW > 0.0) && ist.Any(z => z.KuehlleistungW > 0.0)) gemischt++;
                durchlaeufeMax = Math.Max(durchlaeufeMax, k.Durchlaeufe);

                if (bisektiert < BISEKTION_MAX && !festgehalten && h % 13 == 0)
                {
                    IReadOnlyList<Stundenergebnis> Schritt(double a)
                        => s.Schritt(h, (int z, int st, in Stundenrand r) =>
                        {
                            max[z] = r.MitKuehlung && r.MitKuehluebergabe ? r.ThetaMax : double.NaN;
                            Stundenrand x = r;
                            if (a > 0.0 && r.MitKuehluebergabe && !double.IsNaN(r.KuehlVorlaufC))
                            {
                                double ziel = Math.Max(r.KuehlVorlaufC - a, ERZEUGER_MIN_C);
                                if (ziel < r.KuehlVorlaufC) x = r.MitKuehlvorlauf(ziel, r.KuehlVorlaufGekappt);
                            }
                            return x.MitVerfuegbarkeit(GROSS_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN)
                                    .MitKaelteverfuegbarkeit(GROSS_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN);
                        });
                    double Ueber(IReadOnlyList<Stundenergebnis> e, out int fuehrend)
                    {
                        double ue = 0.0;
                        fuehrend = -1;
                        for (int z = 0; z < e.Count; z++)
                        {
                            if (double.IsNaN(max[z]) || double.IsInfinity(max[z])) continue;
                            double d = e[z].ThetaAirMittel - max[z];
                            if (d > ue) { ue = d; fuehrend = z; }
                        }
                        return ue;
                    }
                    double ue0 = Ueber(Schritt(0.0), out _);
                    if (ue0 > 0.0)
                    {
                        double F(double a) => a - kK * Ueber(Schritt(a), out _);
                        double lo = 0.0, hi = kK * ue0 + 1.0;
                        Assert.True(F(lo) < 0.0 && F(hi) > 0.0, "Stunde " + h + ": keine Klammer");
                        for (int i = 0; i < 50; i++)
                        {
                            double mitte = 0.5 * (lo + hi);
                            if (F(mitte) > 0.0) hi = mitte; else lo = mitte;
                        }
                        double soll = 0.5 * (lo + hi);
                        IReadOnlyList<Stundenergebnis> loes = Schritt(soll);
                        Ueber(loes, out int fuehrend);
                        if (fuehrend == 0) fuehrtEins++;
                        if (fuehrend == 1) fuehrtZwei++;
                        dAMax = Math.Max(dAMax, Math.Abs(k.KuehlAbsenkungK - soll));
                        for (int z = 0; z < loes.Count; z++)
                            dThetaMax = Math.Max(dThetaMax, Math.Abs(ist[z].ThetaAirMittel - loes[z].ThetaAirMittel));
                        bisektiert++;
                    }
                    else Assert.Equal(0.0, k.KuehlAbsenkungK);
                    // Den Schritt der Lösung wiederherstellen (Festschreiben übernimmt den letzten Schritt).
                    s.Schritt(h, (int z, int st, in Stundenrand r) =>
                    {
                        Stundenrand x = k2.Absenken(0, z, r);
                        return x.MitVerfuegbarkeit(GROSS_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN)
                                .MitKaelteverfuegbarkeit(GROSS_KW * 1000.0, Verfuegbarkeitsgrund.KeineBegrenzung, double.NaN);
                    });
                }
                kreis.Festschreiben(h);
            }
            _aus.WriteLine("O3kz k_K {0}: Überschreitungsstunden bisektiert {1} (Führung Zone 1: {2}, Zone 2: {3}), |Δa| max {4:0.0000} K, " +
                           "|Δθ| max {5:0.00000} K, Durchläufe max {6} (Mittel {7:0.000}), festgehalten {8}; Stunden mit Heiz- und Kühlzone {9}; " +
                           "Kühlstunden {10}, Vorlauf Mittel {11:0.00} / min {12:0.00} °C, abgesenkt {13} h, Absenkung {14:0.0} Kh",
                           kK, bisektiert, fuehrtEins, fuehrtZwei, dAMax, dThetaMax, durchlaeufeMax, kreis.DurchlaeufeMittel,
                           k2.StundenFestgehalten, gemischt, k2.Kuehlstunden, k2.KuehlVorlaufMittelC, k2.KuehlVorlaufMinC,
                           k2.StundenAbgesenkt, k2.AbsenkungSummeKh);
            Assert.True(bisektiert > 50, "zu wenige Überschreitungsstunden: " + bisektiert);
            Assert.True(fuehrtZwei > 0, "Zone 2 führt nie");
            // Ab k_K 5 senkt der Kreis die Überschreitung von Zone 2 so weit, dass an warmen Tagen Zone 1 führt.
            if (kK >= 5.0) Assert.True(fuehrtEins > 0, "die Führung wechselt nie zwischen den Zonen");
            Assert.True(gemischt > 0, "keine Stunde mit Heiz- und Kühlzone im Kreis");
            Assert.True(durchlaeufeMax > 1, "der Raumeinfluss wirkt nicht zurück");
            Assert.True(durchlaeufeMax <= Anlagenkopplung.HOECHSTZAHL);
            Assert.True(dAMax <= TOL_ABSENKUNG_K, "Absenkung " + dAMax);
            Assert.True(dThetaMax <= TOL_THETA_K, "Raumluft " + dThetaMax);
            Assert.True(k2.StundenAbgesenkt > 0 && k2.Kuehlstunden > 0);
            Assert.True(k2.KuehlVorlaufMinC >= ERZEUGER_MIN_C - 1e-12, "nie unter dem Erzeuger");
        }

        /// <summary>
        /// <b>Raumeinfluss 0 = ohne</b> im Mehrzonenweg: Ein Raumeinfluss mit k_K = 0 an einer Kälteschranke, die nicht vom
        /// Vorlauf abhängt und keinen festen Kühlvorlauf anbietet, rechnet Raumluft und Kühlleistung jeder Zone Bit für Bit wie
        /// der Kreis ohne ihn.
        /// </summary>
        [Fact]
        public void Raumeinfluss_null_im_Mehrzonenweg_bitgleich_wie_ohne()
        {
            Mehrzonenergebnis m = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            GebaeudeStepper sa = Stepper(m), sb = Stepper(m);
            KuehlRaumeinfluss k2 = KuehlRaumeinfluss.AusSteppern(new[] { sb });
            // Ohne festen Kühlvorlauf der Schranke: der feste Vorlauf kappte die Kurve im Kühlkreis (KZ1, Vorlaufangebot).
            Anlagenkopplung ohne = Kreis(sa, null, double.NaN), mit = Kreis(sb, k2, double.NaN);
            for (int h = 0; h < 8760; h++)
            {
                Kopplungsstunde a = ohne.Stunde(h, double.NaN, default);
                Kopplungsstunde b = mit.Stunde(h, double.NaN, default);
                for (int z = 0; z < a.Loesung[0].Count; z++)
                {
                    Assert.True(a.Loesung[0][z].ThetaAirMittel.Equals(b.Loesung[0][z].ThetaAirMittel), "Raumluft, Stunde " + h);
                    Assert.True(a.Loesung[0][z].KuehlleistungW.Equals(b.Loesung[0][z].KuehlleistungW), "Kühlleistung, Stunde " + h);
                }
                Assert.Equal(0.0, b.KuehlAbsenkungK);
                ohne.Festschreiben(h);
                mit.Festschreiben(h);
            }
            Assert.Equal(0, k2.StundenAbgesenkt);
            Assert.True(k2.Kuehlstunden > 100);
        }

        // =====================================================================
        //  Konvergenz der frei gerechneten Kühlstunden (Auftrag KZ2, Punkt 4)
        // =====================================================================

        /// <summary>
        /// Stunden mit Kühlübergabe je Zone rechnen in der Zonenschleife frei (KZ1, kein festes Muster). Gemessen: Durchläufe
        /// der Zonenschleife mit Kühlübergabe (fester Vorlauf und Kühlkurve) gegen die ideale Kälteseite; keine Stunde erreicht
        /// die Höchstzahl der Zonenschleife.
        /// </summary>
        [Fact]
        public void Frei_gerechnete_Kuehlstunden_konvergieren()
        {
            Mehrzonenergebnis ideal = Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK3, false);
            Mehrzonenergebnis fest = Rechnen(Zwei(kurve: false), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            Mehrzonenergebnis kurve = Rechnen(Zwei(), DbWerte.ANLAGENKOPPLUNG_AK3, true);
            foreach ((string name, Mehrzonenergebnis m) in new[] { ("ideal", ideal), ("fest", fest), ("Kurve", kurve) })
            {
                Zonenschleife z = m.Schleife;
                _aus.WriteLine("{0}: iterierte Stunden {1}, Durchläufe Mittel {2:0.000} / max {3}, Musterwechsel {4}, nicht haltbar {5}",
                               name, z.IterierteStunden, z.DurchlaeufeMittel, z.DurchlaeufeMax, z.Musterwechsel, z.MusterNichtHaltbar);
                Assert.True(z.DurchlaeufeMax < Zonenschleife.HOECHSTZAHL_DURCHLAEUFE, name + ": Höchstzahl erreicht");
            }
        }
    }
}
