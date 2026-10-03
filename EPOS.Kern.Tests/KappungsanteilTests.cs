using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Kappungsanteil auch im idealen Fall</b> (Entwurf KP3, Befund B1, Festlegung 20; Welle
    /// R1): <see cref="Zonenmodell2K.Schritt"/> und <see cref="Zonenmodell2K.SchrittMitMuster"/>
    /// schreiben den Zeitanteil, in dem <c>Heizleistung_Max</c> gekappt hat, auch ohne Kopplung in
    /// <see cref="Stundenergebnis.HeizleistungMaxAnteil"/> — ein neuer Ausgang, keine geänderte Zahl.
    /// Die Folge aus <c>AnlagenkopplungZonenmodellTests</c> (Probe B, Anlagenkopplung 3.7): Im
    /// Grenzfall (unbegrenzte Übergabe, Xp = 0) rechnet der gekoppelte Zweig bitgleich wie der ideale,
    /// also muss auch der Anteil bitgleich sein. Phantasiewerte, ohne Datenbank.
    /// </summary>
    public class KappungsanteilTests
    {
        private readonly ITestOutputHelper _aus;

        public KappungsanteilTests(ITestOutputHelper aus) { _aus = aus; }

        /// <summary>Dieselbe deterministische Folge wie in <c>AnlagenkopplungZonenmodellTests</c>: Heizen, Kühlen, Grenzen, Sommerlüftung.</summary>
        private static List<Stundenrand> Folge(int stunden, int saat)
        {
            var z = new Random(saat);
            var folge = new List<Stundenrand>(stunden);
            for (int h = 0; h < stunden; h++)
            {
                double tag = Math.Sin(2.0 * Math.PI * h / 24.0);
                double aussen = 5.0 - 12.0 * Math.Cos(2.0 * Math.PI * h / 2000.0) + 4.0 * tag + 2.0 * (z.NextDouble() - 0.5);
                double soll = (h % 24) >= 6 && (h % 24) <= 21 ? 20.0 : 16.0;
                double sonne = Math.Max(0.0, tag) * 1500.0 * z.NextDouble();
                double heizMax = z.NextDouble() < 0.3 ? 1500.0 + 3000.0 * z.NextDouble() : double.NaN;
                bool kuehlen = z.NextDouble() < 0.3;
                double zusatz = z.NextDouble() < 0.1 ? 150.0 : 0.0;
                folge.Add(new Stundenrand(aussen, aussen + 1.0, soll, kuehlen ? 25.0 : double.PositiveInfinity,
                                          0.4 * sonne, 0.6 * sonne, 200.0 + 300.0 * z.NextDouble(),
                                          heizMax, kuehlen ? 2000.0 : double.NaN, 0.3, 0.0, zusatz));
            }
            return folge;
        }

        private static Stundenrand MitKopplung(in Stundenrand r, Uebergabekennwerte k, double vorlauf, double xp)
            => new Stundenrand(r.ThetaOut, r.ThetaEq, r.ThetaSoll, r.ThetaMax, r.PhiRadAW, r.PhiRadIW, r.PhiConv,
                               r.HeizleistungMaxW, r.KuehlleistungMaxW, r.HeizungStrahlungsanteil,
                               r.KuehlungAnteilInnenflaeche, r.ZusatzleitwertWK,
                               uebergabe: k, vorlaufC: vorlauf, reglerbandK: xp);

        /// <summary>Alle Ausgänge einer idealen Stunde außer dem Kappungsanteil — bitgleich.</summary>
        private static void UebrigeBitgleich(Stundenergebnis a, Stundenergebnis b, int h)
        {
            string w = " (Stunde " + h.ToString(CultureInfo.InvariantCulture) + ")";
            Assert.True(a.HeizleistungW.Equals(b.HeizleistungW), "Heizleistung" + w);
            Assert.True(a.KuehlleistungW.Equals(b.KuehlleistungW), "Kühlleistung" + w);
            Assert.True(a.ThetaAirMittel.Equals(b.ThetaAirMittel), "Raumluft" + w);
            Assert.True(a.ThetaOpMittel.Equals(b.ThetaOpMittel), "operativ" + w);
            Assert.True(a.ThetaSAwMittel.Equals(b.ThetaSAwMittel) && a.ThetaSIwMittel.Equals(b.ThetaSIwMittel), "Oberflächen" + w);
            Assert.True(a.ThetaMAwMittel.Equals(b.ThetaMAwMittel) && a.ThetaMIwMittel.Equals(b.ThetaMIwMittel), "Massen im Mittel" + w);
            Assert.True(a.ThetaMAwEnde.Equals(b.ThetaMAwEnde) && a.ThetaMIwEnde.Equals(b.ThetaMIwEnde), "Zustand" + w);
            Assert.Equal(a.Abschnitte, b.Abschnitte);
        }

        /// <summary>
        /// Die übrigen Felder einer idealen Stunde stehen, wo der Kurzkonstruktor sie hinstellt: Vorlauf
        /// und Rücklauf NaN, kein Grund, alle anderen Anteile null — der Kappungsanteil ist der einzige
        /// neue Ausgang.
        /// </summary>
        private static void IdealeFelderUnveraendert(Stundenergebnis e, int h)
        {
            string w = " (Stunde " + h.ToString(CultureInfo.InvariantCulture) + ")";
            Assert.True(double.IsNaN(e.VorlaufC) && double.IsNaN(e.RuecklaufC), "Vorlauf/Rücklauf" + w);
            Assert.Equal(Begrenzungsgrund.KeineBegrenzung, e.Begrenzungsgrund);
            Assert.True(e.UebergabeBegrenztAnteil.Equals(0.0) && e.HeizgrenzeAnteil.Equals(0.0), "Anteile der Übergabe" + w);
            Assert.True(double.IsNaN(e.KuehlVorlaufC) && double.IsNaN(e.KuehlRuecklaufC), "Kältekreis" + w);
            Assert.Equal(Begrenzungsgrund.KeineBegrenzung, e.KuehlBegrenzungsgrund);
            Assert.True(e.KuehlUebergabeBegrenztAnteil.Equals(0.0) && e.VorlaufgrenzeAnteil.Equals(0.0)
                        && e.KuehlleistungMaxAnteil.Equals(0.0) && e.KeineKaelteAnteil.Equals(0.0), "Anteile der Kälteseite" + w);
        }

        /// <summary>Der Anteil aus der Fallfolge der Stunde: die Dauern der Abschnitte an der Heizgrenze, in Reihenfolge summiert.</summary>
        private static double AnteilAusMuster(Zonenmodell2K m)
        {
            Betriebsfall[] folge = m.LetzteFallfolge;
            double[] dauer = m.LetzteAbschnittsdauern;
            double summe = 0.0;
            for (int i = 0; i < folge.Length; i++)
                if (folge[i] == Betriebsfall.Heizgrenze) summe += dauer[i];
            return summe / Zonenmodell2K.STUNDE_S;
        }

        /// <summary>
        /// <b>Ideal = gekoppelt im Grenzfall</b> (Anlagenkopplung 3.7): unbegrenzte Übergabe, Xp = 0 —
        /// der ideale Zweig schreibt Stunde für Stunde denselben Kappungsanteil wie der gekoppelte, bitgleich,
        /// und alle übrigen Ausgänge bleiben bitgleich.
        /// </summary>
        [Fact]
        public void Ideal_und_gekoppelt_im_Grenzfall_tragen_denselben_Kappungsanteil()
        {
            var unbegrenzt = new Uebergabekennwerte(double.PositiveInfinity, 1.3, 55.0, 45.0, 20.0);
            List<Stundenrand> ideal = Folge(3000, 42);

            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(18.0);
            b.Zuruecksetzen(18.0);
            int gekappt = 0, teilweise = 0;
            for (int h = 0; h < ideal.Count; h++)
            {
                Stundenrand ri = ideal[h];
                Stundenrand rg = MitKopplung(ideal[h], unbegrenzt, 55.0, 0.0);
                Stundenergebnis ei = a.Schritt(in ri);
                Stundenergebnis eg = b.Schritt(in rg);
                UebrigeBitgleich(ei, eg, h);
                IdealeFelderUnveraendert(ei, h);
                Assert.True(ei.HeizleistungMaxAnteil.Equals(eg.HeizleistungMaxAnteil),
                    "Kappungsanteil, Stunde " + h.ToString(CultureInfo.InvariantCulture) + ": ideal " +
                    ei.HeizleistungMaxAnteil.ToString("R", CultureInfo.InvariantCulture) + ", gekoppelt " +
                    eg.HeizleistungMaxAnteil.ToString("R", CultureInfo.InvariantCulture));
                if (ei.HeizleistungMaxAnteil > 0.0) gekappt++;
                if (ei.HeizleistungMaxAnteil > 0.0 && ei.HeizleistungMaxAnteil < 1.0) teilweise++;
            }
            _aus.WriteLine("Stunden mit Kappung: " + gekappt.ToString(CultureInfo.InvariantCulture) +
                           ", davon teilweise: " + teilweise.ToString(CultureInfo.InvariantCulture));
            Assert.True(gekappt > 0, "die Folge muss Stunden an der Heizleistungsgrenze haben");
            Assert.True(teilweise > 0, "die Folge muss Stunden mit Umschaltung an der Grenze haben");
        }

        /// <summary>
        /// Der Anteil ist die Zeit der Abschnitte im Fall <see cref="Betriebsfall.Heizgrenze"/>: aus der
        /// Fallfolge nachgerechnet bitgleich, 0 ohne Grenze, 1 bei einer ganzen gekappten Stunde.
        /// </summary>
        [Fact]
        public void Der_Kappungsanteil_ist_die_Zeit_an_der_Heizgrenze()
        {
            List<Stundenrand> folge = Folge(3000, 7);
            var m = new Zonenmodell2K(Phantasiegebaeude.Standard());
            m.Zuruecksetzen(19.0);
            int mitGrenze = 0;
            for (int h = 0; h < folge.Count; h++)
            {
                Stundenrand r = folge[h];
                Stundenergebnis e = m.Schritt(in r);
                Assert.True(e.HeizleistungMaxAnteil.Equals(AnteilAusMuster(m)),
                    "Stunde " + h.ToString(CultureInfo.InvariantCulture));
                Assert.InRange(e.HeizleistungMaxAnteil, 0.0, 1.0);
                if (double.IsNaN(r.HeizleistungMaxW)) Assert.Equal(0.0, e.HeizleistungMaxAnteil);
                else mitGrenze++;
            }
            Assert.True(mitGrenze > 0);

            // Eine ganze Stunde an der Grenze: kalt, Grenze weit unter der Last.
            var k = new Zonenmodell2K(Phantasiegebaeude.Standard());
            k.Zuruecksetzen(15.0);
            Stundenrand kalt = Phantasiegebaeude.Rand(-10.0, 20.0, double.PositiveInfinity, heizMaxW: 100.0);
            Stundenergebnis g = k.Schritt(in kalt);
            Assert.Equal(new[] { Betriebsfall.Heizgrenze }, k.LetzteFallfolge);
            Assert.Equal(1.0, g.HeizleistungMaxAnteil);
            Assert.Equal(100.0, g.HeizleistungW, 9);
        }

        /// <summary>
        /// <b><see cref="Zonenmodell2K.SchrittMitMuster"/> = <see cref="Zonenmodell2K.Schritt"/></b>: Mit dem
        /// Muster, das <c>Schritt</c> gefunden hat, ist jede Stunde bitgleich — auch der Kappungsanteil.
        /// </summary>
        [Fact]
        public void SchrittMitMuster_traegt_denselben_Kappungsanteil_wie_Schritt()
        {
            List<Stundenrand> folge = Folge(3000, 11);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard(true));
            a.Zuruecksetzen(18.5);
            b.Zuruecksetzen(18.5);
            int gekappt = 0;
            for (int h = 0; h < folge.Count; h++)
            {
                Stundenrand r = folge[h];
                Stundenergebnis ea = a.Schritt(in r);
                Stundenergebnis eb = b.SchrittMitMuster(in r, a.LetztesMuster);
                UebrigeBitgleich(ea, eb, h);
                IdealeFelderUnveraendert(eb, h);
                Assert.True(ea.HeizleistungMaxAnteil.Equals(eb.HeizleistungMaxAnteil),
                    "Kappungsanteil, Stunde " + h.ToString(CultureInfo.InvariantCulture));
                if (eb.HeizleistungMaxAnteil > 0.0) gekappt++;
            }
            Assert.True(gekappt > 0, "die Folge muss Stunden an der Heizleistungsgrenze haben");
        }

        /// <summary>
        /// Eine Stunde nur mit Kühlübergabe heizt ideal: Sie trägt den Kappungsanteil aus dem eigenen
        /// Akkumulator — bitgleich zu derselben Stunde ganz ohne Kopplung, solange keine Kälte verlangt ist.
        /// </summary>
        [Fact]
        public void Eine_Stunde_nur_mit_Kuehluebergabe_traegt_den_Kappungsanteil_der_Heizseite()
        {
            Uebergabekennwerte gespiegelt = Kuehluebergabe.Gespiegelt(5000.0, 1.0, 16.0, 19.0, 26.0);
            var a = new Zonenmodell2K(Phantasiegebaeude.Standard());
            var b = new Zonenmodell2K(Phantasiegebaeude.Standard());
            a.Zuruecksetzen(17.0);
            b.Zuruecksetzen(17.0);
            int gekappt = 0;
            for (int h = 0; h < 48; h++)
            {
                double aussen = -8.0 + 3.0 * Math.Sin(2.0 * Math.PI * h / 24.0);
                double soll = (h % 24) >= 6 ? 20.0 : 16.0;
                var ri = new Stundenrand(aussen, aussen, soll, 26.0, 0.0, 0.0, 200.0, 2500.0, double.NaN, 0.3, 0.0, 0.0);
                var rk = new Stundenrand(aussen, aussen, soll, 26.0, 0.0, 0.0, 200.0, 2500.0, double.NaN, 0.3, 0.0, 0.0,
                                         kuehlUebergabeGespiegelt: gespiegelt, kuehlVorlaufC: 16.0, kuehlStrahlungsanteil: 0.0);
                Stundenergebnis ei = a.Schritt(in ri);
                Stundenergebnis ek = b.Schritt(in rk);
                UebrigeBitgleich(ei, ek, h);
                Assert.True(ei.HeizleistungMaxAnteil.Equals(ek.HeizleistungMaxAnteil), "Stunde " + h.ToString(CultureInfo.InvariantCulture));
                Assert.Equal(0.0, ek.UebergabeBegrenztAnteil);
                if (ek.HeizleistungMaxAnteil > 0.0) gekappt++;
            }
            Assert.True(gekappt > 0, "die Folge muss Stunden an der Heizleistungsgrenze haben");
        }
    }
}
