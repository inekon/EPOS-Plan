using System;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die EINWILLIGUNGSSTUFE „Dialogdaten" (Auftrag #200, Anwenderentscheid
    /// <b>KI‑D‑Q2</b> vom 11.09.2026).
    ///
    /// <para><b>Was hier bewiesen wird.</b> Die neue Stufe ist von der allgemeinen
    /// Einwilligung UNABHÄNGIG (wer dem Rechtshinweis zustimmt, hat der Übertragung von
    /// Feldwerten nicht zugestimmt), sie trägt Fassung und Datum, sie lässt sich
    /// zurücknehmen, der Abschalter überstimmt sie — und ohne eingehängten Haken gibt es
    /// keinen Weg zu ihr. Der letzte Punkt ist die eigentliche Zusage: Ein Lauf ohne
    /// Oberfläche kann keine Feldwerte übertragen.</para>
    ///
    /// <para>Die Klasse tauscht <c>Dienste.Einstellungen</c> und steht deshalb in der
    /// EINEN seriellen Sammlung (Befund iU5‑O‑1, Wächter
    /// <c>DiensteSammlungTests</c>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KiDialogdatenEinwilligungTests : IDisposable
    {
        private readonly IEinstellungen _ablageVorher = Dienste.Einstellungen;
        private readonly Func<Task<bool>> _nachfragenVorher = KiEinwilligung.Nachfragen;
        private readonly Func<Task<bool>> _dialogdatenVorher = KiEinwilligung.NachfragenDialogdaten;

        public KiDialogdatenEinwilligungTests()
        {
            // Eine frische Ablage je Fall: Die Einwilligung liegt unter Windows in der
            // Registry, im Pruefstand im Arbeitsspeicher (FluechtigeEinstellungen).
            Dienste.Einstellungen = new FluechtigeEinstellungen();
            KiEinwilligung.Nachfragen = null;
            KiEinwilligung.NachfragenDialogdaten = null;
        }

        public void Dispose()
        {
            KiEinwilligung.NachfragenDialogdaten = _dialogdatenVorher;
            KiEinwilligung.Nachfragen = _nachfragenVorher;
            Dienste.Einstellungen = _ablageVorher;
        }

        // =====================================================================
        //  Zustand
        // =====================================================================

        [Fact]
        public void Ohne_Zutun_liegt_keine_Dialogdaten_Einwilligung_vor()
        {
            Assert.Equal(0, KiEinwilligung.DialogdatenFassung);
            Assert.Equal("", KiEinwilligung.DialogdatenBestaetigtAm);
            Assert.False(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public void Erteilen_merkt_Fassung_und_Zeitpunkt()
        {
            KiEinwilligung.DialogdatenErteilen();

            Assert.Equal(KiEinwilligung.FASSUNG_DIALOGDATEN, KiEinwilligung.DialogdatenFassung);
            Assert.True(KiEinwilligung.DialogdatenErteilt);

            // Der Zeitpunkt ist Anzeige und kein Rechenwert - geprueft wird, DASS er
            // dasteht und wie er gebaut ist (yyyy-MM-dd HH:mm, invariant).
            string am = KiEinwilligung.DialogdatenBestaetigtAm;
            Assert.Equal(16, am.Length);
            Assert.Equal('-', am[4]);
            Assert.Equal(':', am[13]);
        }

        [Fact]
        public void Zuruecknehmen_loescht_Merker_und_Zeitpunkt()
        {
            KiEinwilligung.DialogdatenErteilen();
            KiEinwilligung.DialogdatenZuruecknehmen();

            Assert.Equal(0, KiEinwilligung.DialogdatenFassung);
            Assert.Equal("", KiEinwilligung.DialogdatenBestaetigtAm);
            Assert.False(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public void Die_allgemeine_Einwilligung_deckt_die_Dialogdaten_NICHT_ab()
        {
            // Das ist der Kern von KI-D-Q2: Wer dem Rechtshinweis zustimmt, hat der
            // Uebertragung seiner Feldwerte nicht zugestimmt - und umgekehrt.
            KiEinwilligung.Erteilen();

            Assert.True(KiEinwilligung.Erteilt);
            Assert.False(KiEinwilligung.DialogdatenErteilt);

            KiEinwilligung.Zuruecknehmen();
            KiEinwilligung.DialogdatenErteilen();

            Assert.False(KiEinwilligung.Erteilt);
            Assert.True(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public void Eine_aeltere_Fassung_deckt_die_aktuelle_nicht_ab()
        {
            // Aendert sich der Erklaertext inhaltlich, wird FASSUNG_DIALOGDATEN erhoeht;
            // die alte Zustimmung gilt dann nicht mehr.
            Dienste.Einstellungen.Schreib("KiDialogdatenBestaetigt",
                                          (KiEinwilligung.FASSUNG_DIALOGDATEN - 1)
                                              .ToString(System.Globalization.CultureInfo.InvariantCulture));

            Assert.False(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public void Der_Abschalter_ueberstimmt_die_erteilte_Einwilligung()
        {
            KiEinwilligung.DialogdatenErteilen();
            KiEinwilligung.Abgeschaltet = true;

            try
            {
                Assert.False(KiEinwilligung.DialogdatenErteilt);
                Assert.False(KiEinwilligung.DialogdatenMoeglich);
            }
            finally { KiEinwilligung.Abgeschaltet = false; }

            Assert.True(KiEinwilligung.DialogdatenErteilt);
        }

        // =====================================================================
        //  Der Riegel
        // =====================================================================

        [Fact]
        public async Task Ohne_eingehaengten_Haken_gibt_es_keinen_Weg_zur_Einwilligung()
        {
            // DIE ZUSAGE: Ein Lauf ohne Oberflaeche kann keine Feldwerte uebertragen.
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);

            Assert.False(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.False(KiEinwilligung.DialogdatenErteilt);
            Assert.False(KiEinwilligung.DialogdatenMoeglich);
        }

        [Fact]
        public async Task Ohne_die_ALLGEMEINE_Einwilligung_wird_gar_nicht_erst_gefragt()
        {
            // Ohne sie geht ohnehin keine Anfrage hinaus; eine Zustimmung zu den
            // Feldwerten allein waere wertlos - und eine zweite Rueckfrage laestig.
            bool gefragt = false;
            KiEinwilligung.Nachfragen = () => Task.FromResult(false);
            KiEinwilligung.NachfragenDialogdaten = () => { gefragt = true; return Task.FromResult(true); };

            Assert.False(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.False(gefragt);
            Assert.False(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public async Task Eine_Ablehnung_erteilt_nichts()
        {
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);
            KiEinwilligung.NachfragenDialogdaten = () => Task.FromResult(false);

            Assert.False(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.False(KiEinwilligung.DialogdatenErteilt);

            // Moeglich ist sie trotzdem: Der Anwender kann es sich anders ueberlegen.
            Assert.True(KiEinwilligung.DialogdatenMoeglich);
        }

        [Fact]
        public async Task Ein_werfender_Haken_gilt_als_Ablehnung()
        {
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);
            KiEinwilligung.NachfragenDialogdaten = () => throw new InvalidOperationException("weg");

            Assert.False(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.False(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public async Task Gefragt_wird_EINMAL_danach_traegt_der_Merker()
        {
            int fragen = 0;
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);
            KiEinwilligung.NachfragenDialogdaten = () => { fragen++; return Task.FromResult(true); };

            Assert.True(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.True(await KiEinwilligung.SicherstellenDialogdatenAsync());
            Assert.True(await KiEinwilligung.SicherstellenDialogdatenAsync());

            Assert.Equal(1, fragen);
            Assert.True(KiEinwilligung.DialogdatenErteilt);
        }

        [Fact]
        public async Task Nach_dem_Zuruecknehmen_wird_wieder_gefragt()
        {
            int fragen = 0;
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);
            KiEinwilligung.NachfragenDialogdaten = () => { fragen++; return Task.FromResult(true); };

            Assert.True(await KiEinwilligung.SicherstellenDialogdatenAsync());
            KiEinwilligung.DialogdatenZuruecknehmen();
            Assert.True(await KiEinwilligung.SicherstellenDialogdatenAsync());

            Assert.Equal(2, fragen);
        }

        [Fact]
        public async Task Der_Abschalter_laesst_gar_nicht_erst_fragen()
        {
            bool gefragt = false;
            KiEinwilligung.Nachfragen = () => Task.FromResult(true);
            KiEinwilligung.NachfragenDialogdaten = () => { gefragt = true; return Task.FromResult(true); };
            KiEinwilligung.Abgeschaltet = true;

            try
            {
                Assert.False(await KiEinwilligung.SicherstellenDialogdatenAsync());
                Assert.False(gefragt);
            }
            finally { KiEinwilligung.Abgeschaltet = false; }
        }
    }
}
