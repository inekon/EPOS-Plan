using System;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// ABNAHMEBEFUND 2 zum ersten App-Start — „Kennzahlen offensichtlich falsch:
    /// die Kopplung an PV und Strombedarf scheint nicht zu passen".
    ///
    /// <para>
    /// <b>Das beobachtete Bild.</b> Ergebnisseite eines realen Projekts (BYD B-Box,
    /// 10,2 kWh / 11,04 kW, SoC-Band 10–90 %, Start-SoC 100 % → auf 9,18 kWh geklemmt;
    /// Jahreslast 4,81 GWh, PV 5,2 kWp): Eigenverbrauchsquote 0,0 %, Autarkiegrad
    /// 0,0 %, Einspeisung ohne/mit Speicher 0/0, Netzbezugsdifferenz ~7 kWh/a,
    /// SoC 1,0 … 6,3 kWh, 100 % Zeitanteil an der Untergrenze, Speicherverluste 0.
    /// </para>
    /// <para>
    /// <b>Die Frage, die diese Klasse beantwortet.</b> Rechnet die Engine falsch, oder
    /// ist die EINGANGSREIHE das Problem? Beide Fälle stehen hier als Test nebeneinander
    /// und werden mit demselben Speicher und derselben Last gerechnet — der einzige
    /// Unterschied ist die PV-Reihe.
    /// </para>
    /// <list type="number">
    ///   <item><description><b>Mit PV</b> (auch mit sehr kleiner): Eigenverbrauchsquote
    ///     ≈ 100 %, Autarkiegrad &gt; 0. Die Engine ist in Ordnung — der beobachtete
    ///     0-%-Wert kann so nicht entstehen.</description></item>
    ///   <item><description><b>Ohne PV</b> (Nullvektor, weil die Photovoltaik im Lauf
    ///     nicht aufgenommen war): Eigenverbrauchsquote 0 % (0/0 per Definition),
    ///     Autarkiegrad praktisch 0, Einspeisung 0/0, Entladung = die Anfangsladung.
    ///     Das ist das Bild des Screenshots, Zahl für Zahl.</description></item>
    /// </list>
    /// <para>
    /// Der Fix liegt deshalb nicht in der Formel, sondern in der Sichtbarkeit: Der
    /// Controller meldet den fehlenden Erzeugungseingang ins Laufprotokoll, und die
    /// Ergebnisseite zeigt die Eigenverbrauchsquote ohne Bezugsgröße als „–" statt als
    /// „0,0 %" (siehe <c>StromspeicherSimCtrl.ErzeugungPruefen</c> und
    /// <c>Form_Simulation_Detail.SpErzeugungshinweisSetzen</c>).
    /// </para>
    /// </summary>
    public sealed class KleinePvGrosseLastTests
    {
        // Gerät und Band des Befundprojekts.
        private const double CNom = 10.2;      // kWh
        private const double PKw = 11.04;      // kW
        private const double SoCMin = 0.10 * CNom;
        private const double SoCMax = 0.90 * CNom;

        private const double LastKw = 550.0;   // konstante Last, wie im Befund
        private const double PvSpitzeKw = 0.26;  // 260-W-Skala

        private const int N = RasterAdapter.ViertelstundenJahr;   // 35.040

        private static SpeicherParameter Parameter(double? startSoC = null)
            => new SpeicherParameter
            {
                CNomKwh = CNom,
                PKw = PKw,
                SoCMinKwh = SoCMin,
                SoCMaxKwh = SoCMax,
                RoundTripWirkungsgrad = 0.90,
                StartSoCKwh = startSoC,
                Kapitalzins = 0.03,
                NutzungsdauerA = 20.0
            };

        /// <summary>
        /// PV-Reihe mit Tagesgang: eine Sinushalbwelle zwischen 6 und 18 Uhr, skaliert
        /// auf <see cref="PvSpitzeKw"/>. Bewusst deterministisch und ohne Wetterdaten —
        /// geprüft wird die Kopplung, nicht die Ertragsrechnung.
        /// </summary>
        private static double[] PvReiheMitTagesgang()
        {
            double[] pv = new double[N];
            for (int i = 0; i < N; i++)
            {
                double stunde = (i % 96) / 4.0;                 // 0 … 23,75
                if (stunde < 6.0 || stunde > 18.0) continue;
                pv[i] = PvSpitzeKw * Math.Sin(Math.PI * (stunde - 6.0) / 12.0);
            }
            return pv;
        }

        private static SpeicherEingang Eingang(double[] pv)
            => SpeicherEingang.MitFixpreis(SpeicherEingang.KonstanteReihe(LastKw, N), pv, 20.0);

        // ------------------------------------------------------------------ Mit PV

        /// <summary>
        /// <b>Kernaussage.</b> Last ≫ PV: Jede erzeugte Kilowattstunde wird im selben
        /// Intervall verbraucht, es gibt nie Überschuss. Die Eigenverbrauchsquote ist
        /// damit 100 % — NICHT 0 %.
        /// </summary>
        [Fact]
        public void Kleine_PV_an_grosser_Last_Eigenverbrauch_100_Prozent()
        {
            SpeicherErgebnis erg = new Dauernutzung().Berechne(Eingang(PvReiheMitTagesgang()), Parameter());
            SpeicherKennzahlen k = erg.Kennzahlen;

            Assert.True(k.ErzeugungPvKwh > 0.0, "Die PV-Reihe muss Ertrag liefern.");
            Assert.Equal(0.0, k.EinspeisungOhneSpeicherKwh, 9);
            Assert.Equal(0.0, k.EinspeisungMitSpeicherKwh, 9);
            Assert.Equal(1.0, k.EigenverbrauchsquoteMitSpeicher, 9);
            Assert.Equal(1.0, k.EigenverbrauchsquoteOhneSpeicher, 9);
        }

        /// <summary>
        /// Der Autarkiegrad ist der PV-Anteil an der Last — klein, aber deutlich über
        /// null. Genau diese Zahl stand im Befund auf 0,0 %.
        /// </summary>
        [Fact]
        public void Kleine_PV_an_grosser_Last_Autarkie_Entspricht_PV_Anteil()
        {
            SpeicherErgebnis erg = new Dauernutzung().Berechne(Eingang(PvReiheMitTagesgang()), Parameter());
            SpeicherKennzahlen k = erg.Kennzahlen;

            double pvAnteil = k.ErzeugungPvKwh / k.LastKwh;

            Assert.True(pvAnteil > 0.0);
            // Ohne Speicherwirkung ist der Autarkiegrad exakt der PV-Anteil: Die ganze
            // Erzeugung geht direkt in die Last (siehe Test darueber).
            Assert.Equal(pvAnteil, k.AutarkiegradOhneSpeicher, 9);
            // Mit Speicher kommt hoechstens die Entladung hinzu - nie weniger.
            Assert.True(k.AutarkiegradMitSpeicher >= k.AutarkiegradOhneSpeicher);
            Assert.True(k.AutarkiegradMitSpeicher > 0.0);
        }

        /// <summary>
        /// Die BILANZ schliesst auch in dieser Konstellation je Intervall:
        /// Last = Direkt + Netzbezug(mit) + Entladung und
        /// Erzeugung = Direkt + Ladung + Einspeisung(mit).
        /// </summary>
        [Fact]
        public void Kleine_PV_an_grosser_Last_Bilanz_Schliesst()
        {
            SpeicherErgebnis erg = new Dauernutzung().Berechne(Eingang(PvReiheMitTagesgang()), Parameter());
            SpeicherKennzahlen k = erg.Kennzahlen;

            Assert.Equal(k.LastKwh,
                         k.DirektverbrauchKwh + k.NetzbezugMitSpeicherKwh + erg.EntladeenergieKwh, 6);
            Assert.Equal(k.ErzeugungKwh,
                         k.DirektverbrauchKwh + erg.LadeenergieKwh + k.EinspeisungMitSpeicherKwh, 6);
        }

        // ----------------------------------------------------------------- Ohne PV

        /// <summary>
        /// <b>Das Screenshot-Muster.</b> Dieselbe Last, dieselbe Batterie, aber ein
        /// PV-NULLVEKTOR — so, wie ihn <c>StromspeicherSimCtrl.BauePvReihe</c> liefert,
        /// wenn die Photovoltaik im Lauf nicht aufgenommen ist. Erwartet wird nicht
        /// „richtig", sondern „genau das Bild des Befunds".
        /// </summary>
        [Fact]
        public void Ohne_PV_Reihe_Entsteht_Das_Befundbild()
        {
            // Start-SoC: Der Geraetedatensatz des Projekts fuehrt Ladezustand = 100 %.
            // GEKLEMMT wird im Controller (StromspeicherSimCtrl.LeseParameter, "in das
            // Band geklemmt"), nicht in der Engine - der Test setzt deshalb den bereits
            // geklemmten Wert SoC_max ein, so wie ihn die Kette uebergibt.
            SpeicherParameter p = Parameter(SoCMax);
            SpeicherErgebnis erg = new Dauernutzung().Berechne(Eingang(new double[N]), p);
            SpeicherKennzahlen k = erg.Kennzahlen;

            // (1) Keine Erzeugung -> die Quote ist 0/0 und wird als 0 gefuehrt.
            Assert.Equal(0.0, k.ErzeugungKwh, 12);
            Assert.Equal(0.0, k.EigenverbrauchsquoteMitSpeicher, 12);

            // (2) Einspeisung ohne/mit Speicher 0/0.
            Assert.Equal(0.0, k.EinspeisungOhneSpeicherKwh, 12);
            Assert.Equal(0.0, k.EinspeisungMitSpeicherKwh, 12);

            // (3) Autarkiegrad rundet auf eine Nachkommastelle zu 0,0 %.
            Assert.True(k.AutarkiegradMitSpeicher > 0.0);
            Assert.True(k.AutarkiegradMitSpeicher * 100.0 < 0.05);

            // (4) Ohne Quelle wird NIE geladen; entladen wird genau die Anfangsladung
            //     ueber dem Band: (SoC_start - SoC_min) * eta_dis.
            Assert.Equal(0.0, erg.LadeenergieKwh, 12);
            Assert.Equal((SoCMax - SoCMin) * p.EtaDis, erg.EntladeenergieKwh, 9);

            // (5) Die Netzbezugsdifferenz IST diese Entladung - rund 7,7 kWh/a.
            Assert.Equal(erg.EntladeenergieKwh,
                         k.NetzbezugOhneSpeicherKwh - k.NetzbezugMitSpeicherKwh, 9);
            Assert.InRange(erg.EntladeenergieKwh, 7.0, 8.0);

            // (6) SoC-Fenster 1,02 … 6,27 kWh; der Startwert 9,18 steht nie im Array,
            //     weil soc[0] erst NACH dem ersten Entladeintervall geschrieben wird.
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < erg.SoCKwh.Length; i++)
            {
                if (erg.SoCKwh[i] < min) min = erg.SoCKwh[i];
                if (erg.SoCKwh[i] > max) max = erg.SoCKwh[i];
            }
            Assert.Equal(SoCMin, min, 9);
            Assert.Equal(SoCMax - PKw * p.DtH / p.EtaDis, max, 9);
            Assert.InRange(max, 6.2, 6.4);
        }

        /// <summary>
        /// Gegenprobe zu (4): Die Reihe steht praktisch 100 % der Zeit an der
        /// Untergrenze — nur die beiden ersten Werte liegen darueber, waehrend die
        /// Anfangsladung abfliesst (im dritten Intervall ist SoC_min erreicht).
        /// </summary>
        [Fact]
        public void Ohne_PV_Reihe_Steht_Der_SoC_An_Der_Untergrenze()
        {
            SpeicherErgebnis erg = new Dauernutzung().Berechne(Eingang(new double[N]), Parameter(SoCMax));

            int unten = 0;
            for (int i = 0; i < erg.SoCKwh.Length; i++)
                if (erg.SoCKwh[i] <= SoCMin + 1e-9) unten++;

            Assert.Equal(N - 2, unten);
            Assert.True(unten * 100.0 / N > 99.99);
        }
    }
}
