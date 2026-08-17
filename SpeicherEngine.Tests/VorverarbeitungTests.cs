using System;
using System.Globalization;
using Xunit;

namespace SpeicherEngine.Tests
{
    /// <summary>
    /// Tests der Vorverarbeitung je Intervall (Fachkonzept 6, Einleitung):
    /// BHKW-Ueberschussbildung, Quellen-Matrix und die Bilanzidentitaeten.
    /// </summary>
    public sealed class VorverarbeitungTests
    {
        private const double Dt = 0.25;
        private const double Toleranz = 1e-12;

        private static IntervallEnergien Rechne(
            double lastKw, double pvKw, double bhkwKw,
            bool pvZulaessig = true, bool bhkwZulaessig = true)
            => Vorverarbeitung.Berechne(lastKw, pvKw, bhkwKw, Dt, pvZulaessig, bhkwZulaessig);

        // ------------------------------------------------------- Grundkonstellationen

        /// <summary>
        /// Reiner PV-Ueberschuss ohne BHKW: Last 4 kW, PV 12 kW.
        /// E_last = 1, E_pv = 3, E_restlast = 1, E_pv_frei = 2, E_defizit = 0.
        /// </summary>
        [Fact]
        public void Nur_PV_Ueberschuss()
        {
            IntervallEnergien e = Rechne(4.0, 12.0, 0.0);

            Assert.Equal(1.0, e.ELastKwh, 12);
            Assert.Equal(3.0, e.EPvKwh, 12);
            Assert.Equal(0.0, e.EBhkwKwh, 12);
            Assert.Equal(1.0, e.ERestlastKwh, 12);
            Assert.Equal(2.0, e.EPvFreiKwh, 12);
            Assert.Equal(0.0, e.EBhkwFreiKwh, 12);
            Assert.Equal(0.0, e.EDefizitKwh, 12);
            Assert.Equal(2.0, e.EQuelleKwh, 12);
            Assert.Equal(1.0, e.EDirektKwh, 12);
        }

        /// <summary>
        /// Reines Defizit: Last 20 kW, PV 4 kW, kein BHKW.
        /// E_defizit = (20-4)*0,25 = 4 kWh, kein Ueberschuss.
        /// </summary>
        [Fact]
        public void Nur_Defizit()
        {
            IntervallEnergien e = Rechne(20.0, 4.0, 0.0);

            Assert.Equal(5.0, e.ELastKwh, 12);
            Assert.Equal(4.0, e.EDefizitKwh, 12);
            Assert.Equal(0.0, e.EQuelleKwh, 12);
            Assert.Equal(0.0, e.EUeberschussKwh, 12);
            Assert.Equal(1.0, e.EDirektKwh, 12);
        }

        /// <summary>
        /// <b>BHKW-Ueberschussbildung</b> (im Bestand nirgends vorhanden, Fachkonzept 3.3):
        /// Last 4 kW, BHKW 12 kW, keine PV.
        /// E_bhkw_frei = (12-4)*0,25 = 2 kWh; E_restlast = 0; E_pv_frei = 0.
        /// </summary>
        [Fact]
        public void BHKW_Ueberschuss_Wird_Gebildet()
        {
            IntervallEnergien e = Rechne(4.0, 0.0, 12.0);

            Assert.Equal(1.0, e.ELastKwh, 12);
            Assert.Equal(3.0, e.EBhkwKwh, 12);
            Assert.Equal(0.0, e.ERestlastKwh, 12);
            Assert.Equal(0.0, e.EPvFreiKwh, 12);
            Assert.Equal(2.0, e.EBhkwFreiKwh, 12);
            Assert.Equal(0.0, e.EDefizitKwh, 12);
            Assert.Equal(2.0, e.EQuelleKwh, 12);
        }

        /// <summary>
        /// <b>Merit-Order-Konvention "BHKW deckt die Last vorrangig"</b> (Fachkonzept 2.2):
        /// Last 12 kW, BHKW 8 kW, PV 8 kW. E_restlast = (12-8)*0,25 = 1 kWh,
        /// davon deckt PV 1 kWh; ladefaehig bleibt E_pv_frei = 2-1 = 1 kWh.
        /// BHKW liefert keinen Ueberschuss, weil es die Last nicht uebersteigt.
        /// </summary>
        [Fact]
        public void BHKW_Deckt_Die_Last_Vorrangig_PV_Bleibt_Ladefaehig()
        {
            IntervallEnergien e = Rechne(12.0, 8.0, 8.0);

            Assert.Equal(3.0, e.ELastKwh, 12);
            Assert.Equal(2.0, e.EPvKwh, 12);
            Assert.Equal(2.0, e.EBhkwKwh, 12);
            Assert.Equal(1.0, e.ERestlastKwh, 12);
            Assert.Equal(1.0, e.EPvFreiKwh, 12);
            Assert.Equal(0.0, e.EBhkwFreiKwh, 12);
            Assert.Equal(0.0, e.EDefizitKwh, 12);
            Assert.Equal(1.0, e.EQuelleKwh, 12);
            Assert.Equal(3.0, e.EDirektKwh, 12);   // die Last wird vollstaendig direkt gedeckt
        }

        /// <summary>
        /// Beide Quellen im Ueberschuss: Last 4 kW, PV 8 kW, BHKW 8 kW.
        /// E_restlast = 0 -&gt; E_pv_frei = 2 kWh (volle PV), E_bhkw_frei = 1 kWh.
        /// </summary>
        [Fact]
        public void Beide_Quellen_Im_Ueberschuss()
        {
            IntervallEnergien e = Rechne(4.0, 8.0, 8.0);

            Assert.Equal(2.0, e.EPvFreiKwh, 12);
            Assert.Equal(1.0, e.EBhkwFreiKwh, 12);
            Assert.Equal(3.0, e.EQuelleKwh, 12);
            Assert.Equal(0.0, e.EDefizitKwh, 12);
            Assert.Equal(1.0, e.EDirektKwh, 12);
        }

        // ------------------------------------------------------------ Quellen-Matrix

        /// <summary>
        /// Die Quellen-Matrix (Fachkonzept 2.1) wirkt ausschliesslich auf E_quelle -
        /// die physikalischen Groessen bleiben unveraendert. Insbesondere deckt das
        /// BHKW die Last auch dann vorrangig, wenn es nicht laden darf.
        /// </summary>
        [Theory]
        [InlineData(true, true, 3.0)]     // Gruen "BHKW + PV" bzw. Grau
        [InlineData(true, false, 2.0)]    // Gruen "nur PV"
        [InlineData(false, true, 1.0)]    // Gruen "nur BHKW"
        [InlineData(false, false, 0.0)]   // Speicher gesperrt
        public void Quellenflags_Steuern_Nur_E_Quelle(bool pv, bool bhkw, double erwartetQuelle)
        {
            IntervallEnergien e = Rechne(4.0, 8.0, 8.0, pv, bhkw);

            Assert.Equal(erwartetQuelle, e.EQuelleKwh, 12);
            Assert.Equal(pv ? 2.0 : 0.0, e.EPvQuelleKwh, 12);
            Assert.Equal(bhkw ? 1.0 : 0.0, e.EBhkwQuelleKwh, 12);

            // Unveraendert, unabhaengig von den Flags:
            Assert.Equal(2.0, e.EPvFreiKwh, 12);
            Assert.Equal(1.0, e.EBhkwFreiKwh, 12);
            Assert.Equal(1.0, e.EDirektKwh, 12);
            Assert.Equal(0.0, e.EDefizitKwh, 12);
        }

        // -------------------------------------------------------- Bilanzidentitaeten

        /// <summary>
        /// Ueber synthetische Gruen-/Grau-Konstellationen (fester Seed): die beiden
        /// Bilanzidentitaeten der Vorverarbeitung schliessen in <b>jedem</b> Intervall,
        /// und Ueberschuss und Defizit schliessen einander aus.
        /// </summary>
        [Fact]
        public void Bilanzidentitaeten_Schliessen_In_Jedem_Intervall()
        {
            var zufall = new Random(20260816);

            for (int k = 0; k < 20000; k++)
            {
                double last = zufall.NextDouble() * 60.0;
                double pv = (k % 3 == 0) ? 0.0 : zufall.NextDouble() * 60.0;
                double bhkw = (k % 4 == 0) ? 0.0 : zufall.NextDouble() * 40.0;

                IntervallEnergien e = Rechne(last, pv, bhkw);

                // (1) Last = Direktdeckung + Residuallast
                Assert.True(Math.Abs(e.ELastKwh - (e.EDirektKwh + e.EDefizitKwh)) <= Toleranz,
                    Meldung("Lastbilanz", k, last, pv, bhkw));

                // (2) Erzeugung = Direktdeckung + freier Ueberschuss beider Quellen
                double erzeugung = e.EPvKwh + e.EBhkwKwh;
                Assert.True(Math.Abs(erzeugung - (e.EDirektKwh + e.EUeberschussKwh)) <= 1e-9,
                    Meldung("Erzeugungsbilanz", k, last, pv, bhkw));

                // (3) Ueberschuss und Defizit schliessen sich aus
                Assert.True(e.EUeberschussKwh <= Toleranz || e.EDefizitKwh <= Toleranz,
                    Meldung("Ueberschuss UND Defizit gleichzeitig", k, last, pv, bhkw));

                // (4) Keine Groesse wird negativ
                Assert.True(e.EPvFreiKwh >= 0.0 && e.EBhkwFreiKwh >= 0.0 &&
                            e.EDefizitKwh >= 0.0 && e.ERestlastKwh >= 0.0,
                    Meldung("negative Teilgroesse", k, last, pv, bhkw));
            }
        }

        private static string Meldung(string was, int k, double last, double pv, double bhkw)
            => was + " verletzt in Fall " + k +
               " (last = " + last.ToString("R", CultureInfo.InvariantCulture) +
               ", pv = " + pv.ToString("R", CultureInfo.InvariantCulture) +
               ", bhkw = " + bhkw.ToString("R", CultureInfo.InvariantCulture) + ")";

        // ------------------------------------------------------------------ Vertrag

        /// <summary>Die Ueberladung mit Parametersatz liest die Flags von dort.</summary>
        [Fact]
        public void Ueberladung_Mit_Parametern_Uebernimmt_Flags_Und_Dt()
        {
            SpeicherParameter p = new SpeicherParameter
            {
                DtH = Dt,
                PvZulaessig = false,
                BhkwUeberschussZulaessig = true
            };

            IntervallEnergien e = Vorverarbeitung.Berechne(4.0, 8.0, 8.0, p);

            Assert.Equal(1.0, e.EQuelleKwh, 12);
            Assert.Equal(0.0, e.EPvQuelleKwh, 12);
            Assert.Throws<ArgumentNullException>(() => Vorverarbeitung.Berechne(1.0, 1.0, 1.0, null!));
        }

        /// <summary>Ohne Erzeugung und ohne Last bleibt alles bei null.</summary>
        [Fact]
        public void Leeres_Intervall_Bleibt_Null()
        {
            IntervallEnergien e = Rechne(0.0, 0.0, 0.0);

            Assert.Equal(0.0, e.ELastKwh);
            Assert.Equal(0.0, e.EQuelleKwh);
            Assert.Equal(0.0, e.EDefizitKwh);
            Assert.Equal(0.0, e.EUeberschussKwh);
            Assert.Equal(0.0, e.EDirektKwh);
        }
    }
}
