using System;
using System.Collections.Generic;
using SkiaSharp;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die EINGABEPRÜFUNG der Speicherparameter (W11b‑B‑28, je Regel einzeln seit
    /// W11b‑B‑29) und die LEGENDENHÖHE des Verlaufsbildes — die zwei Regeln des
    /// Pakets, die ohne Oberfläche prüfbar sind.
    ///
    /// <para><b>Warum die Prüfung im Kern steht.</b> Der Schreibweg selbst liegt in
    /// <c>SimulationErgebnisHuelle.SpeicherfeldSchreiben</c>, und
    /// <c>WindowsFormsApplication1</c> hat kein Testprojekt. Die Regeln sind aber
    /// Fachregeln: ein SoC-Band braucht zwei verschiedene Kanten, eine Gerätegröße ist
    /// positiv, ein Zins ist nicht negativ. Als reine Funktionen sind sie hier prüfbar,
    /// und die Hülle ruft sie, bevor eine Zeile in die Datenbank geht.</para>
    ///
    /// <para><b>Je Regel eine Funktion</b> (W11b‑B‑29). Seit der Block jedes Feld sofort
    /// schreibt, gibt es keinen Satz mehr, den man als Ganzes prüfen könnte: Es kommt
    /// EIN Feld an, und geprüft wird die Regel, die zu ihm gehört. Die Gesamtprüfung
    /// <c>Pruefen(…)</c> ist damit ohne Verwender und entfallen.</para>
    ///
    /// <para><b>Die Legendenhöhe</b> ist die prüfbare Fassung der Bildkorrektur aus
    /// demselben Paket: Bei vier Reihen bricht die Legende in eine zweite Zeile um, und
    /// die lag bis dahin auf dem linken Achsentitel. Ein Testfall kann keine Bildpunkte
    /// lesen — wohl aber die Umbruchregel und, am fertigen PNG, dass es überhaupt
    /// entsteht und wiederholbar ist.</para>
    /// </summary>
    public class SpeicherParameterPruefungTests
    {
        // =================================================================
        //  Das SoC-Band — SpeicherParameterPruefung.SoCBand(min, max)
        // =================================================================

        /// <summary>Der Regelfall geht durch — <c>null</c> heißt „in Ordnung".</summary>
        [Fact]
        public void Ein_gueltiges_SoC_Band_wird_nicht_beanstandet()
        {
            Assert.Null(SpeicherParameterPruefung.SoCBand(10, 90));
        }

        /// <summary>Die Kanten 0 und 100 sind erlaubt — sie sind das Band, nicht sein Rand.</summary>
        [Fact]
        public void Die_Grenzfaelle_null_und_hundert_sind_erlaubt()
        {
            Assert.Null(SpeicherParameterPruefung.SoCBand(0, 100));
        }

        /// <summary>
        /// Ein Band ohne Hub ist keines: <c>min = max</c> ergäbe einen Speicher, dessen
        /// nutzbare Kapazität null ist — die Engine teilte später durch die Bandbreite.
        /// </summary>
        [Fact]
        public void Min_gleich_Max_ist_kein_Band()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(50, 50));
        }

        [Fact]
        public void Min_ueber_Max_wird_abgewiesen()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(90, 10));
        }

        [Theory]
        [InlineData(-1.0, 90.0)]
        [InlineData(10.0, 100.1)]
        [InlineData(-5.0, 105.0)]
        public void Werte_ausserhalb_von_null_bis_hundert_werden_abgewiesen(double min, double max)
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(min, max));
        }

        /// <summary>NaN und Unendlich sind keine Eingaben, sondern Rechenunfälle.</summary>
        [Fact]
        public void NaN_im_SoC_Band_wird_abgewiesen()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(double.NaN, 90));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(10, double.PositiveInfinity));
        }

        /// <summary>
        /// <b>Die eine Kante gegen die andere</b> (W11b‑B‑29). Wer das Minimum eingibt,
        /// gibt es gegen das GESPEICHERTE Maximum ein — deshalb nimmt die Regel beide
        /// Kanten und nicht nur die geänderte. 95 % neben einem Maximum von 90 % ist
        /// kein Band; unter 90 % ist es eines.
        /// </summary>
        [Fact]
        public void Eine_Kante_wird_gegen_die_gespeicherte_andere_geprueft()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_SOC_BAND,
                         SpeicherParameterPruefung.SoCBand(95, 90));
            Assert.Null(SpeicherParameterPruefung.SoCBand(85, 90));
        }

        // =================================================================
        //  Die Gerätegröße — SpeicherParameterPruefung.Geraet(kwh, kw)
        // =================================================================

        [Fact]
        public void Eine_positive_Geraetegroesse_wird_nicht_beanstandet()
        {
            Assert.Null(SpeicherParameterPruefung.Geraet(129, 100));
        }

        /// <summary>
        /// Null und negativ sind keine Gerätegröße — je Feld einzeln geprüft, weil je
        /// Feld einzeln eingegeben wird.
        /// </summary>
        [Theory]
        [InlineData(0.0, 100.0)]
        [InlineData(129.0, 0.0)]
        [InlineData(-1.0, 100.0)]
        [InlineData(129.0, -0.5)]
        public void Die_Geraetegroesse_muss_positiv_sein(double kwh, double kw)
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_GERAET_POSITIV,
                         SpeicherParameterPruefung.Geraet(kwh, kw));
        }

        [Fact]
        public void NaN_in_der_Geraetegroesse_wird_abgewiesen()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_GERAET_POSITIV,
                         SpeicherParameterPruefung.Geraet(double.NaN, 100));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_GERAET_POSITIV,
                         SpeicherParameterPruefung.Geraet(129, double.PositiveInfinity));
        }

        // =================================================================
        //  Die vier Wirtschaftswerte — SpeicherParameterPruefung.NichtNegativ(wert)
        // =================================================================

        /// <summary>Null ist erlaubt — ein Projekt ohne Leistungspreis ist ein gültiger Fall.</summary>
        [Fact]
        public void Null_ist_ein_erlaubter_Wirtschaftswert()
        {
            Assert.Null(SpeicherParameterPruefung.NichtNegativ(0));
            Assert.Null(SpeicherParameterPruefung.NichtNegativ(3.5));
        }

        [Theory]
        [InlineData(-1.0)]
        [InlineData(-0.1)]
        [InlineData(-20.0)]
        public void Negative_Wirtschaftswerte_werden_abgewiesen(double wert)
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_NEGATIV,
                         SpeicherParameterPruefung.NichtNegativ(wert));
        }

        [Fact]
        public void NaN_als_Wirtschaftswert_wird_abgewiesen()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_NEGATIV,
                         SpeicherParameterPruefung.NichtNegativ(double.NaN));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_PARAM_MSG_NEGATIV,
                         SpeicherParameterPruefung.NichtNegativ(double.NegativeInfinity));
        }

        /// <summary>
        /// Die drei Regeln melden DREI verschiedene Gründe — die Statuszeile des Blocks
        /// nennt damit das Feld, an dem es hängt, und nicht „irgendetwas stimmt nicht".
        /// </summary>
        [Fact]
        public void Jede_Regel_hat_ihre_eigene_Meldung()
        {
            Assert.NotEqual(SpeicherParameterPruefung.SoCBand(90, 10),
                            SpeicherParameterPruefung.Geraet(0, 0));
            Assert.NotEqual(SpeicherParameterPruefung.Geraet(0, 0),
                            SpeicherParameterPruefung.NichtNegativ(-1));
            Assert.NotEqual(SpeicherParameterPruefung.SoCBand(90, 10),
                            SpeicherParameterPruefung.NichtNegativ(-1));
        }

        // =================================================================
        //  Die Legendenhöhe des Verlaufsbildes (Bildkorrektur W11b-B-28)
        // =================================================================

        /// <summary>
        /// Passen alle Einträge nebeneinander, bleibt es EINE Zeile — das Zeichenrechteck
        /// steht dann wie bisher.
        /// </summary>
        [Fact]
        public void Eine_Legende_die_hineinpasst_bleibt_einzeilig()
        {
            Assert.Equal(ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(4, 200f, 100f, 900f));
        }

        /// <summary>
        /// <b>Der Befund.</b> Vier Einträge (Bezug ohne, Bezug mit, Speicherleistung,
        /// Ladezustand) in einem Band, das nur drei trägt: Die Legende geht auf ZWEI
        /// Zeilen, und um genau diese eine Zeilenhöhe muss das Zeichenrechteck nach
        /// unten rücken. Bis W11b‑B‑28 tat es das nicht, und die zweite Zeile lag auf
        /// dem linken Achsentitel.
        /// </summary>
        [Fact]
        public void Vier_zu_breite_Eintraege_brauchen_zwei_Zeilen()
        {
            Assert.Equal(2 * ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(4, 300f, 100f, 1000f));
        }

        /// <summary>Und bei noch weniger Platz entsprechend mehr.</summary>
        [Fact]
        public void Je_schmaler_das_Band_desto_mehr_Zeilen()
        {
            Assert.Equal(4 * ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(4, 300f, 100f, 320f));
        }

        /// <summary>
        /// Ohne Umbruchkante gibt es keinen Umbruch — die Legenden der übrigen Bilder
        /// rufen <c>Legende</c> ohne sie und bleiben deshalb einzeilig.
        /// </summary>
        [Fact]
        public void Ohne_Umbruchkante_bleibt_es_eine_Zeile()
        {
            Assert.Equal(ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(new float[] { 400f, 400f, 400f, 400f },
                                                     100f, 0f));
            Assert.Equal(ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(Array.Empty<float>(), 100f, 900f));
        }

        /// <summary>
        /// EIN Eintrag, der für sich schon zu breit ist, bricht NICHT um: Nach dem
        /// Umbruch stünde er an derselben Kante und wäre immer noch zu breit — die
        /// Bedingung <c>x &gt; startX</c> verhindert die Endlosreihe leerer Zeilen.
        /// </summary>
        [Fact]
        public void Ein_einzelner_zu_breiter_Eintrag_bricht_nicht_um()
        {
            Assert.Equal(ChartRenderer.LEGENDE_ZEILE,
                         ChartRenderer.LegendenHoehe(1, 5000f, 100f, 900f));
        }

        /// <summary>
        /// Und am fertigen Bild: Das PNG entsteht, hat das feste Maß und ist
        /// wiederholbar — auch mit vier Reihen, zweiter Achse und einem langen Titel für
        /// die rechte Achse („Ladezustand [kWh]"), also genau in dem Fall, in dem beide
        /// Fehler auftraten.
        /// </summary>
        [Fact]
        public void Das_Betriebsbild_mit_vier_Legendeneintraegen_entsteht_und_ist_wiederholbar()
        {
            List<ChartRenderer.Reihe> reihen = DreiLeistungsreihen();   // + Ladezustand = vier Legendeneintraege
            ChartRenderer.Reihe soc = new ChartRenderer.Reihe(
                "Ladezustand", Reihe(200, 100), SKColors.SteelBlue);

            byte[] a = ChartRenderer.Speicherbetrieb("Lastgang und Speicherbetrieb", reihen,
                                                     "Leistung [kW]", soc, "Ladezustand [kWh]");
            byte[] b = ChartRenderer.Speicherbetrieb("Lastgang und Speicherbetrieb", DreiLeistungsreihen(),
                                                     "Leistung [kW]",
                                                     new ChartRenderer.Reihe("Ladezustand",
                                                                             Reihe(200, 100),
                                                                             SKColors.SteelBlue),
                                                     "Ladezustand [kWh]");

            Assert.NotNull(a);
            Assert.True(a.Length > 0);
            Assert.Equal(a, b);

            using (SKBitmap bild = SKBitmap.Decode(a))
            {
                Assert.Equal(1240, bild.Width);
                Assert.Equal(560, bild.Height);
            }
        }

        /// <summary>
        /// Der Titel der zweiten Achse steht rechtsbündig und ändert damit das Bild —
        /// die Gegenprobe zum festen Einzug, der ihn am rechten Rand abschnitt.
        /// </summary>
        [Fact]
        public void Ein_langer_Titel_der_zweiten_Achse_veraendert_das_Bild()
        {
            ChartRenderer.Reihe Soc() => new ChartRenderer.Reihe("Ladezustand", Reihe(200, 100),
                                                                 SKColors.SteelBlue);

            Assert.NotEqual(
                ChartRenderer.Speicherbetrieb("T", DreiLeistungsreihen(), "kW", Soc(), "kWh"),
                ChartRenderer.Speicherbetrieb("T", DreiLeistungsreihen(), "kW", Soc(), "Ladezustand [kWh]"));
        }

        private const int STUNDEN = 8760;

        private static double[] Reihe(double grund, double hub, int versatz = 0)
        {
            var w = new double[STUNDEN];
            for (int i = 0; i < STUNDEN; i++)
                w[i] = grund + hub * Math.Sin(2 * Math.PI * (i + versatz) / 24.0);
            return w;
        }

        /// <summary>Die drei LEISTUNGEN der linken Achse; der Ladezustand kommt rechts dazu.</summary>
        private static List<ChartRenderer.Reihe> DreiLeistungsreihen() => new List<ChartRenderer.Reihe>
        {
            new ChartRenderer.Reihe("Netzbezug ohne Speicher", Reihe(40, 20), SKColors.Firebrick),
            new ChartRenderer.Reihe("Netzbezug mit Speicher", Reihe(35, 15, 3), SKColors.SeaGreen),
            new ChartRenderer.Reihe("Speicherleistung", Reihe(0, 25, 6), SKColors.DarkOrange)
        };
    }
}
