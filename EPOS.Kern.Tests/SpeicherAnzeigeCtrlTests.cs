using System;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="SpeicherAnzeigeCtrl"/> — die drei Anzeigeuebersetzungen, die bis
    /// iU9-W11a.5 dreifach im Oberflaechencode standen (Befund W11-B42).
    ///
    /// <para>Die Texte kommen aus <c>MyResource.Resource</c> und folgen der
    /// Oberflaechensprache; die Faelle pinnen sie deshalb auf de-DE (Regel seit
    /// iU9-W8). Ohne Datenbank.</para>
    /// </summary>
    public class SpeicherAnzeigeCtrlTests
    {
        [Fact]
        public void BetriebsartText_uebersetzt_die_beiden_Persistenzwerte()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRAUSTROM,
                         SpeicherAnzeigeCtrl.BetriebsartText(DbWerte.SP_BETRIEBSART_GRAUSTROM));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BETRIEBSART_ANZEIGE_GRUENSTROM,
                         SpeicherAnzeigeCtrl.BetriebsartText(DbWerte.SP_BETRIEBSART_GRUENSTROM));
        }

        /// <summary>
        /// Unbekannte Werte kommen unveraendert zurueck — besser der Persistenzwert als
        /// gar nichts; <c>null</c> wird zum Leerwert.
        /// </summary>
        [Fact]
        public void BetriebsartText_laesst_Unbekanntes_stehen()
        {
            Assert.Equal("Irgendwas", SpeicherAnzeigeCtrl.BetriebsartText("Irgendwas"));
            Assert.Equal("", SpeicherAnzeigeCtrl.BetriebsartText(null));
        }

        [Fact]
        public void BerechnungsartText_uebersetzt_Nacht_und_Dauernutzung()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG,
                         SpeicherAnzeigeCtrl.BerechnungsartText(DbWerte.SP_BERECHNUNG_NACHTNUTZUNG));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG,
                         SpeicherAnzeigeCtrl.BerechnungsartText(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG));
        }

        /// <summary>
        /// Die Preissteuerung kennt er seit dem Zusammenfuehren mit W10b (W11a-O-4).
        /// DREI der vier Fassungen des Bestands kannten sie NICHT und zeigten den
        /// Persistenzwert „Arbitrage"; die vierte (Simulationskonfiguration) kannte sie —
        /// und die hat gewonnen.
        /// </summary>
        [Fact]
        public void BerechnungsartText_kennt_die_Preissteuerung()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_BERECHNUNG_ANZEIGE_ARBITRAGE,
                         SpeicherAnzeigeCtrl.BerechnungsartText(DbWerte.SP_BERECHNUNG_ARBITRAGE));
            Assert.NotEqual(DbWerte.SP_BERECHNUNG_ARBITRAGE,
                            SpeicherAnzeigeCtrl.BerechnungsartText(DbWerte.SP_BERECHNUNG_ARBITRAGE));
        }

        /// <summary>
        /// Ein UNBEKANNTER Wert kommt unveraendert zurueck. Die vierte Fassung fiel dort
        /// auf „Dauernutzung" zurueck — eine Behauptung ueber Daten, die man nicht kennt.
        /// </summary>
        [Fact]
        public void BerechnungsartText_laesst_Unbekanntes_stehen()
        {
            Assert.Equal("Irgendwas", SpeicherAnzeigeCtrl.BerechnungsartText("Irgendwas"));
            Assert.Equal("", SpeicherAnzeigeCtrl.BerechnungsartText(null));
        }

        [Fact]
        public void AmortisationText_nennt_die_beiden_Sonderfaelle()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR,
                         SpeicherAnzeigeCtrl.AmortisationText(Amortisation.NichtAmortisierbar));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER,
                         SpeicherAnzeigeCtrl.AmortisationText(Amortisation.UeberNutzungsdauer));
        }

        /// <summary>
        /// Die beiden Ressourcenpaare des Bestands tragen denselben Wortlaut — geprueft,
        /// bevor eines von beiden gewaehlt wurde.
        /// </summary>
        [Fact]
        public void Die_beiden_Ressourcenpaare_sind_wortgleich()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_AMORT_NIE,
                         WindowsFormsApplication1.MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.OPT_AMORT_UEBER,
                         WindowsFormsApplication1.MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER);
        }

        [Fact]
        public void AmortisationText_zeigt_sonst_die_Jahre()
        {
            using var _ = new DeutscheOberflaeche();

            string t = SpeicherAnzeigeCtrl.AmortisationText(Amortisation.Jahreswert(7.25));
            Assert.Contains("7", t);
            Assert.DoesNotContain("amort", t.ToLowerInvariant());
        }

        /// <summary>
        /// <b>Kein „−0,0“</b> (Anwenderwunsch 08.09.2026, W11b‑B‑14). Die dynamische
        /// Amortisation rechnet <c>-ln(1 - I·i/E)/ln(1+i)</c>; bei <c>I = 0</c> ist der
        /// Zähler <c>-ln(1) = -0</c>, und ein negatives Null bleibt in IEEE 754 negativ.
        /// <c>"N1"</c> schrieb dafür ein Minuszeichen vor eine Null.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.0)]
        [InlineData(-0.04)]
        public void AmortisationText_schreibt_kein_negatives_Null(double jahre)
        {
            using var _ = new DeutscheZahlen();

            string t = SpeicherAnzeigeCtrl.AmortisationText(Amortisation.Jahreswert(jahre));

            Assert.DoesNotContain("-", t);
            Assert.DoesNotContain("\u2212", t);
            Assert.Equal("0,0", t);
        }

        /// <summary>
        /// Eine Amortisationszeit von „0,0 a“ behauptet, der Speicher habe sich sofort
        /// bezahlt gemacht. Ohne Investition (I = 0) ist sie gar nicht bestimmbar — die
        /// Überladung mit der Investition sagt das mit dem Gedankenstrich, den die Seite
        /// für jede andere unbestimmte Kennzahl führt.
        /// </summary>
        [Fact]
        public void AmortisationText_ohne_Investition_ist_unbestimmt()
        {
            using var _ = new DeutscheZahlen();

            Assert.Equal(SpeicherKennzahlenBlock.UNBESTIMMT,
                         SpeicherAnzeigeCtrl.AmortisationText(Amortisation.Jahreswert(0.0), 0.0));
            Assert.Equal(SpeicherKennzahlenBlock.UNBESTIMMT,
                         SpeicherAnzeigeCtrl.AmortisationText(Amortisation.Jahreswert(7.25), -1.0));

            // Mit Investition bleibt es bei der Zahl bzw. beim Klartext des Sonderfalls.
            Assert.Equal("12,5", SpeicherAnzeigeCtrl.AmortisationText(Amortisation.Jahreswert(12.5), 9000.0));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR,
                         SpeicherAnzeigeCtrl.AmortisationText(Amortisation.NichtAmortisierbar, 9000.0));
        }

        /// <summary>
        /// Wie <see cref="DeutscheOberflaeche"/>, aber mit der ZAHLENkultur dazu — die
        /// Amortisation wird mit <c>CultureInfo.CurrentCulture</c> formatiert.
        /// </summary>
        private sealed class DeutscheZahlen : IDisposable
        {
            private readonly System.Globalization.CultureInfo _oberflaeche =
                System.Threading.Thread.CurrentThread.CurrentUICulture;
            private readonly System.Globalization.CultureInfo _zahlen =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            public DeutscheZahlen()
            {
                var de = new System.Globalization.CultureInfo("de-DE");
                System.Threading.Thread.CurrentThread.CurrentUICulture = de;
                System.Threading.Thread.CurrentThread.CurrentCulture = de;
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _oberflaeche;
                System.Threading.Thread.CurrentThread.CurrentCulture = _zahlen;
            }
        }

        // ------------------------------------------------------------ CO2 (W11-B31)

        /// <summary>
        /// Die beiden RUECKFALLWERTE der Autarkie-Kachel.
        ///
        /// <para>Bis zum Anwenderentscheid <b>W11a-O-2</b> (07.09.2026) waren es die
        /// zwei Literale aus <c>DashboardForm.cs:355</c> — 0,42 und 0,20 —, und sie
        /// galten IMMER. Seither holt die Kachel ihre Faktoren aus dem
        /// Emissionskatalog des Projekts; die Konstanten greifen nur noch, wenn dem
        /// Projekt kein Traeger zugeordnet ist. Der NETZSTROMWERT ist dabei auf den
        /// BAFA-Faktor gezogen, mit dem der Rest des Hauses rechnet
        /// (<c>KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH</c> = 435 g/kWh) — genau
        /// das war der Inhalt des offenen Punktes: keine zweite Wahrheit.</para>
        /// </summary>
        [Fact]
        public void Co2Rueckfallwerte_kommen_aus_der_Emissionsquelle()
        {
            Assert.Equal(0.435, EmissionsVorgaben.CO2_NETZSTROM_KG_JE_KWH);
            Assert.Equal(0.20, EmissionsVorgaben.CO2_WAERME_KG_JE_KWH);

            // Dieselbe Zahl wie im Kennzahlenrechner - EINE Quelle (W11a-O-2).
            Assert.Equal(KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH / 1000.0,
                         EmissionsVorgaben.CO2_NETZSTROM_KG_JE_KWH, 9);
        }

        [Fact]
        public void Co2ErsparnisKg_rechnet_wie_die_Kachel()
        {
            Assert.Equal(1000.0 * 0.435 + 500.0 * 0.20,
                         EmissionsVorgaben.Co2ErsparnisKg(1000.0, 500.0), 9);
            Assert.Equal(0.0, EmissionsVorgaben.Co2ErsparnisKg(0.0, 0.0), 9);
        }

        /// <summary>
        /// OHNE Projekt (Id 0) ist die Fassung MIT Projekt zeichengleich zur Fassung
        /// ohne: Es gibt keinen Traeger, also gelten beide Rueckfallwerte (W11a-O-2).
        /// Der Fall braucht keine Datenbank — genau deshalb steht er hier.
        /// </summary>
        [Fact]
        public void Co2ErsparnisKg_ohne_Projekt_nimmt_die_Rueckfallwerte()
        {
            Assert.Equal(EmissionsVorgaben.Co2ErsparnisKg(1000.0, 500.0),
                         EmissionsVorgaben.Co2ErsparnisKg(0, 1000.0, 500.0), 9);
        }

        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _vorher =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo("de-DE");
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = _vorher;
            }
        }
    }
}
