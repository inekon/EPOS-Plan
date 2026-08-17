using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Intervallreihen <see cref="SpeicherErgebnis.LadungAcKwh"/> und
    /// <see cref="SpeicherErgebnis.EntladungAcKwh"/> (AP2b).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Reihen sind die Intervallaufloesung der bereits geprueften Summen
    /// <see cref="SpeicherErgebnis.LadeenergieKwh"/> und
    /// <see cref="SpeicherErgebnis.EntladeenergieKwh"/>. Sie tragen den Einbau der
    /// Engine in die Simulationskette des Hauptprojekts: Der Netzbezug eines
    /// Intervalls sinkt um die Entladung, die Ladung mindert die Einspeisung.
    /// Deshalb wird hier nachgerechnet, dass (a) die Summen exakt getroffen werden,
    /// (b) Laden und Entladen einander je Intervall ausschliessen und (c) die
    /// SoC-Fortschreibung genau aus diesen beiden Reihen folgt.
    /// </para>
    /// <para>
    /// Dieselben Mini-Eingaenge wie in <c>EnergetischerModusTests</c>: dt = 0,25 h,
    /// P = 10 kW, Band 0 .. 10 kWh, eta_RT = 0,81 (eta_ch = eta_dis = 0,9 exakt).
    /// </para>
    /// </remarks>
    public sealed class IntervallreihenTests
    {
        private static readonly double[] MiniLastKw = { 0.0, 0.0, 0.0, 20.0, 20.0, 0.0 };
        private static readonly double[] MiniPvKw = { 8.0, 8.0, 8.0, 0.0, 0.0, 0.0 };
        private const double MiniPreisCtKwh = 20.0;

        private static SpeicherParameter MiniParameter() => new SpeicherParameter
        {
            CNomKwh = 10.0,
            PKw = 10.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 10.0,
            RoundTripWirkungsgrad = 0.81,
            DtH = 0.25,
            VerguetungCtKwh = 5.0,
            CCapEurProKwh = 500.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0
        };

        private static SpeicherEingang MiniEingang()
            => SpeicherEingang.MitFixpreis(MiniLastKw, MiniPvKw, MiniPreisCtKwh);

        // ==================================================================
        // Mini-Beispiel, von Hand nachgerechnet
        // ==================================================================

        /// <summary>
        /// Dasselbe Mini-Beispiel wie <c>Energetisch_Mini_Rechnet_Von_Hand_Nach</c>,
        /// jetzt intervallweise:
        /// <code>
        /// k=0..2  Ladung 2,00 kWh (PV-Ueberschuss 8 kW * 0,25 h), Entladung 0
        /// k=3     Entladung 2,50 kWh (Leistungsgrenze P*dt), Ladung 0
        /// k=4     Entladung 2,36 kWh (SoC-Grenze), Ladung 0
        /// k=5     weder noch
        /// </code>
        /// </summary>
        [Fact]
        public void Energetisch_Intervallreihen_Rechnen_Von_Hand_Nach()
        {
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch)
                .Berechne(MiniEingang(), MiniParameter());

            double[] sollLadung = { 2.0, 2.0, 2.0, 0.0, 0.0, 0.0 };
            double[] sollEntladung = { 0.0, 0.0, 0.0, 2.5, 2.36, 0.0 };

            Assert.Equal(6, r.LadungAcKwh.Length);
            Assert.Equal(6, r.EntladungAcKwh.Length);

            for (int k = 0; k < 6; k++)
            {
                Assert.True(Math.Abs(r.LadungAcKwh[k] - sollLadung[k]) <= 1e-12,
                    "Ladung[" + k + "] = " + r.LadungAcKwh[k].ToString("R", CultureInfo.InvariantCulture) +
                    ", erwartet " + sollLadung[k].ToString("R", CultureInfo.InvariantCulture));
                Assert.True(Math.Abs(r.EntladungAcKwh[k] - sollEntladung[k]) <= 1e-12,
                    "Entladung[" + k + "] = " + r.EntladungAcKwh[k].ToString("R", CultureInfo.InvariantCulture) +
                    ", erwartet " + sollEntladung[k].ToString("R", CultureInfo.InvariantCulture));
            }
        }

        // ==================================================================
        // Summen- und Strukturtreue am Referenzjahr
        // ==================================================================

        /// <summary>
        /// Die sequenzielle Summe der Intervallreihen trifft die Jahressummen des
        /// Ergebnisses <b>bitgenau</b> - beide entstehen in derselben Schleife aus
        /// derselben Groesse.
        /// </summary>
        [Theory]
        [InlineData(SpeicherModus.Energetisch)]
        [InlineData(SpeicherModus.ExcelKompatibilitaet)]
        public void Summe_Der_Intervallreihen_Trifft_Die_Jahressummen(SpeicherModus modus)
        {
            SpeicherErgebnis r = new Dauernutzung(modus)
                .Berechne(Referenzdaten.V7Eingang(), Referenzdaten.V7Parameter());

            double summeLadung = 0.0;
            double summeEntladung = 0.0;
            for (int k = 0; k < r.Anzahl; k++)
            {
                summeLadung += r.LadungAcKwh[k];
                summeEntladung += r.EntladungAcKwh[k];
            }

            Assert.Equal(r.LadeenergieKwh, summeLadung);
            Assert.Equal(r.EntladeenergieKwh, summeEntladung);
        }

        /// <summary>
        /// Laden und Entladen schliessen einander je Intervall aus (Vorverarbeitung:
        /// Ueberschuss und Defizit koennen nicht gleichzeitig positiv sein), und keine
        /// der beiden Reihen wird negativ.
        /// </summary>
        [Theory]
        [InlineData(SpeicherModus.Energetisch)]
        [InlineData(SpeicherModus.ExcelKompatibilitaet)]
        public void Laden_Und_Entladen_Schliessen_Sich_Aus(SpeicherModus modus)
        {
            SpeicherErgebnis r = new Dauernutzung(modus)
                .Berechne(Referenzdaten.V7Eingang(), Referenzdaten.V7Parameter());

            for (int k = 0; k < r.Anzahl; k++)
            {
                Assert.True(r.LadungAcKwh[k] >= 0.0, "Ladung[" + k + "] ist negativ.");
                Assert.True(r.EntladungAcKwh[k] >= 0.0, "Entladung[" + k + "] ist negativ.");
                Assert.True(r.LadungAcKwh[k] == 0.0 || r.EntladungAcKwh[k] == 0.0,
                    "Intervall " + k + " laedt und entlaedt gleichzeitig.");
            }
        }

        /// <summary>
        /// Der SoC-Verlauf folgt in <b>jedem</b> Intervall genau aus den beiden Reihen:
        /// <c>SoC[k] - SoC[k-1] = Ladung[k]*eta_ch - Entladung[k]/eta_dis</c>.
        /// Damit ist ausgeschlossen, dass die Reihen eine andere Groesse mitschreiben
        /// als die, die den Speicher wirklich bewegt hat.
        /// </summary>
        [Fact]
        public void SoC_Fortschreibung_Folgt_Aus_Den_Intervallreihen()
        {
            SpeicherParameter p = Referenzdaten.V7Parameter() with { RoundTripWirkungsgrad = 0.81 };
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.Energetisch)
                .Berechne(Referenzdaten.V7Eingang(), p);

            double etaCh = p.EtaCh;
            double etaDis = p.EtaDis;
            double prev = p.StartSoCEffektivKwh;

            for (int k = 0; k < r.Anzahl; k++)
            {
                double erwartet = prev + r.LadungAcKwh[k] * etaCh - r.EntladungAcKwh[k] / etaDis;
                Assert.True(Math.Abs(r.SoCKwh[k] - erwartet) <= 1e-9,
                    "SoC[" + k + "] = " + r.SoCKwh[k].ToString("R", CultureInfo.InvariantCulture) +
                    ", erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture));
                prev = r.SoCKwh[k];
            }
        }

        /// <summary>
        /// Im Kompatibilitaetsmodus bleibt Index 0 unangetastet - die V7-Mappe
        /// simuliert ihn nicht. Die Intervallreihen muessen dieselbe Luecke zeigen wie
        /// SoC und Geldwert, sonst verschoebe der Einbau in die Kette die Bilanz um ein
        /// Intervall.
        /// </summary>
        [Fact]
        public void Kompatibilitaetsmodus_Laesst_Index_0_Leer()
        {
            SpeicherErgebnis r = new Dauernutzung(SpeicherModus.ExcelKompatibilitaet)
                .Berechne(Referenzdaten.V7Eingang(), Referenzdaten.V7Parameter());

            Assert.Equal(0.0, r.LadungAcKwh[0]);
            Assert.Equal(0.0, r.EntladungAcKwh[0]);
        }

        // ==================================================================
        // Vertrag des Ergebnistyps
        // ==================================================================

        /// <summary>
        /// Ein Ergebnis ohne Intervallreihen fuehrt Nullvektoren statt <c>null</c> -
        /// kein Aufrufer muss pruefen (Rueckwaertskompatibilitaet der Strategien, die
        /// die Reihen noch nicht fuellen).
        /// </summary>
        [Fact]
        public void Ohne_Angabe_Stehen_Nullvektoren_Statt_Null()
        {
            SpeicherErgebnis r = new SpeicherErgebnis(
                new double[3], new double[3], 0.0, 0.0, 0.0,
                SpeicherModus.Energetisch, Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang()));

            Assert.NotNull(r.LadungAcKwh);
            Assert.NotNull(r.EntladungAcKwh);
            Assert.Equal(3, r.LadungAcKwh.Length);
            Assert.Equal(3, r.EntladungAcKwh.Length);
            Assert.All(r.LadungAcKwh, w => Assert.Equal(0.0, w));
            Assert.All(r.EntladungAcKwh, w => Assert.Equal(0.0, w));
        }

        /// <summary>
        /// Eine Intervallreihe abweichender Laenge ist ein Programmierfehler und wird
        /// sofort gemeldet - ein stillschweigend verschobenes Raster waere im
        /// Jahreslauf nicht mehr auffindbar.
        /// </summary>
        [Fact]
        public void Falsche_Reihenlaenge_Wird_Abgewiesen()
        {
            WirtschaftlichkeitErgebnis w = Wirtschaftlichkeit.Berechne(new WirtschaftlichkeitEingang());

            Assert.Throws<ArgumentException>(() => new SpeicherErgebnis(
                new double[3], new double[3], 0.0, 0.0, 0.0,
                SpeicherModus.Energetisch, w, null, new double[2], null));

            Assert.Throws<ArgumentException>(() => new SpeicherErgebnis(
                new double[3], new double[3], 0.0, 0.0, 0.0,
                SpeicherModus.Energetisch, w, null, null, new double[4]));
        }
    }
}
