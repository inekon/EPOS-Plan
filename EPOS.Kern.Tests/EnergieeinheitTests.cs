using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Anzeigeeinheit der beiden Bedarfsansichten (Anwenderentscheid W8‑O‑5 /
    /// W9‑O‑3 vom 04.09.2026: MWh als Vorgabe, kWh wählbar, konsistent in den
    /// Ansichten).
    ///
    /// <para>Geprüft werden drei Dinge: dass die Umrechnung in beide Richtungen
    /// stimmt UND in der Identität BITGLEICH ist (sonst wäre die Anzeige bei der
    /// Vorgabe MWh nicht mehr zeichengleich zum Bestand), dass die Vorgabe MWh
    /// ist, und dass die Wahl den Rundweg über <see cref="Dienste.Einstellungen"/>
    /// übersteht.</para>
    ///
    /// <para>Die Klasse steht in derselben Sammlung wie <c>DiensteTests</c>:
    /// <c>Dienste.Einstellungen</c> ist prozessweiter Zustand, und xunit fährt
    /// Testklassen sonst nebeneinander.</para>
    /// </summary>
    [Collection("Dienste")]
    public class EnergieeinheitTests
    {
        /// <summary>
        /// Die Oberflächenkultur für die Fälle, die einen FORMATIERTEN Text
        /// vergleichen — der Windows- und der en_US-Lauf sähen sonst einen Punkt
        /// statt eines Kommas (Regel seit W8).
        /// </summary>
        private sealed class DeutscheOberflaeche : System.IDisposable
        {
            private readonly CultureInfo _vorher = Thread.CurrentThread.CurrentCulture;
            private readonly CultureInfo _vorherUi = Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                var de = new CultureInfo("de-DE");
                Thread.CurrentThread.CurrentCulture = de;
                Thread.CurrentThread.CurrentUICulture = de;
            }

            public void Dispose()
            {
                Thread.CurrentThread.CurrentCulture = _vorher;
                Thread.CurrentThread.CurrentUICulture = _vorherUi;
            }
        }

        // ================================================================= Vorgabe

        [Fact]
        public void Vorgabe_ist_MWh()
        {
            Assert.Same(Energieeinheit.MWh, Energieeinheit.Vorgabe);
            Assert.Equal("MWh", Energieeinheit.MWh.Text);
            Assert.Equal("kWh", Energieeinheit.KWh.Text);
        }

        [Fact]
        public void Alle_fuehrt_beide_Einheiten_mit_MWh_zuerst()
        {
            Assert.Equal(2, Energieeinheit.Alle.Count);
            Assert.Same(Energieeinheit.MWh, Energieeinheit.Alle[0]);
            Assert.Same(Energieeinheit.KWh, Energieeinheit.Alle[1]);
        }

        [Fact]
        public void Format_ist_je_Einheit_verschieden()
        {
            Assert.Equal("F2", Energieeinheit.MWh.Format);
            Assert.Equal("F0", Energieeinheit.KWh.Format);
        }

        // ============================================================= Umrechnung

        [Fact]
        public void MWh_zeigt_einen_MWh_Wert_BITGLEICH()
        {
            // Die Identitaet darf nicht ueber x * 1000 * 0,001 laufen - sonst waere
            // die Anzeige bei der Vorgabe MWh nicht mehr zeichengleich zum Bestand.
            double[] proben = { 0, 0.1, 4.0597, 30, 594.3, 12345.678 };
            foreach (double wert in proben)
            {
                Assert.Equal(wert, Energieeinheit.MWh.AusMWh(wert));
                Assert.Equal(wert, Energieeinheit.MWh.NachMWh(wert));
                Assert.Equal(wert, Energieeinheit.MWh.Aus(Energieeinheit.MWh, wert));
            }
        }

        [Fact]
        public void kWh_zeigt_einen_kWh_Wert_BITGLEICH()
        {
            double[] proben = { 0, 0.1, 4059.7, 30000, 594300 };
            foreach (double wert in proben)
            {
                Assert.Equal(wert, Energieeinheit.KWh.AusKWh(wert));
                Assert.Equal(wert, Energieeinheit.KWh.NachKWh(wert));
                Assert.Equal(wert, Energieeinheit.KWh.Aus(Energieeinheit.KWh, wert));
            }
        }

        [Fact]
        public void MWh_aus_kWh_teilt_durch_tausend()
        {
            Assert.Equal(0.5943, Energieeinheit.MWh.AusKWh(594.3), 10);
            Assert.Equal(0.5943, Energieeinheit.MWh.Aus(Energieeinheit.KWh, 594.3), 10);
        }

        [Fact]
        public void kWh_aus_MWh_nimmt_mal_tausend()
        {
            Assert.Equal(594.3, Energieeinheit.KWh.AusMWh(0.5943), 10);
            Assert.Equal(594.3, Energieeinheit.KWh.Aus(Energieeinheit.MWh, 0.5943), 10);
        }

        [Fact]
        public void Der_Rueckweg_einer_Eingabe_kehrt_die_Anzeige_um()
        {
            // Der Speicherweg der Bedarfsprofile liegt in MWh: Was in kWh eingegeben
            // wird, muss vor dem Schreiben durch 1000.
            Assert.Equal(12.5, Energieeinheit.KWh.NachMWh(12500), 10);
            Assert.Equal(12500, Energieeinheit.KWh.NachKWh(12500));
            Assert.Equal(12.5, Energieeinheit.MWh.NachMWh(12.5));
            Assert.Equal(12500, Energieeinheit.MWh.NachKWh(12.5), 10);
        }

        [Fact]
        public void Aus_ohne_Quelle_laesst_den_Wert_stehen()
        {
            Assert.Equal(7.5, Energieeinheit.KWh.Aus(null, 7.5));
        }

        // ============================================================ Formatierung

        [Fact]
        public void Formatiere_folgt_der_Einheit_und_der_Kultur()
        {
            using var _ = new DeutscheOberflaeche();
            Assert.Equal("0,59", Energieeinheit.MWh.Formatiere(0.5943));
            Assert.Equal("594", Energieeinheit.KWh.Formatiere(594.3));
        }

        // ================================================================= Schluessel

        [Theory]
        [InlineData("MWh")]
        [InlineData("mwh")]
        public void AusText_findet_MWh(string text)
        {
            Assert.Same(Energieeinheit.MWh, Energieeinheit.AusText(text));
        }

        [Theory]
        [InlineData("kWh")]
        [InlineData("KWH")]
        public void AusText_findet_kWh(string text)
        {
            Assert.Same(Energieeinheit.KWh, Energieeinheit.AusText(text));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("GWh")]
        public void AusText_faellt_auf_die_Vorgabe_zurueck(string text)
        {
            Assert.Same(Energieeinheit.MWh, Energieeinheit.AusText(text));
        }

        // ================================================== die gemerkte Wahl

        [Fact]
        public void Ohne_Eintrag_liest_die_Wahl_MWh()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                Dienste.Einstellungen = new FluechtigeEinstellungen();
                Assert.Same(Energieeinheit.MWh, BedarfEinheitWahl.Lies());
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }

        [Fact]
        public void Die_Wahl_uebersteht_den_Rundweg()
        {
            IEinstellungen vorher = Dienste.Einstellungen;
            try
            {
                var ablage = new FluechtigeEinstellungen();
                Dienste.Einstellungen = ablage;

                BedarfEinheitWahl.Schreib(Energieeinheit.KWh);
                Assert.Equal("kWh", ablage.Lies(BedarfEinheitWahl.SCHLUESSEL));
                Assert.Same(Energieeinheit.KWh, BedarfEinheitWahl.Lies());

                BedarfEinheitWahl.Schreib(Energieeinheit.MWh);
                Assert.Same(Energieeinheit.MWh, BedarfEinheitWahl.Lies());

                // null legt die Vorgabe ab, es bleibt kein Altstand stehen.
                BedarfEinheitWahl.Schreib(Energieeinheit.KWh);
                BedarfEinheitWahl.Schreib(null);
                Assert.Same(Energieeinheit.MWh, BedarfEinheitWahl.Lies());
            }
            finally
            {
                Dienste.Einstellungen = vorher;
            }
        }
    }
}
