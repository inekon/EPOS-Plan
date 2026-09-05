using System;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die 40 Kennzahlzeilen des Stromspeicher-Ergebnisses (iU9-W11a.3).
    ///
    /// <para>Geprueft werden Zahl und Reihenfolge der Zeilen, die drei Gruppen, das
    /// Verhalten mit und ohne Vergleichslauf, der Sonderfall „Eigenverbrauchsquote ohne
    /// Erzeugung" und die beiden Warnstaffelungen.</para>
    ///
    /// <para>Die Beschriftungen kommen aus <c>MyResource.Resource</c> und folgen der
    /// Oberflaechensprache. Wo ein Text geprueft wird, ist die Sprache gepinnt (Regel
    /// seit iU9-W8).</para>
    /// </summary>
    public class SpeicherKennzahlenBlockTests
    {
        /// <summary>Ein Ergebnismodell mit unterscheidbaren Werten.</summary>
        private static ErgebnisStromspeicherModel Modell(double faktor = 1.0)
        {
            return new ErgebnisStromspeicherModel
            {
                Ladung_PV = 100 * faktor,
                Ladung_BHKW = 200 * faktor,
                Ladung_Netz = 300 * faktor,
                Ladung_Gesamt = 600 * faktor,
                Entladung_Gesamt = 550 * faktor,
                Verluste_Gesamt = 50 * faktor,
                Netzbezug_Ohne = 1000 * faktor,
                Netzbezug_Mit = 800 * faktor,
                Einspeisung_Ohne = 900 * faktor,
                Einspeisung_Mit = 400 * faktor,
                Eigenverbrauchsquote = 55.5 * faktor,
                Autarkiegrad = 44.4 * faktor,
                Vollzyklen = 120 * faktor,
                SoC_Min = 1 * faktor,
                SoC_Mittel = 5 * faktor,
                SoC_Max = 9 * faktor,
                Zeitanteil_Untergrenze = 10 * faktor,
                Zeitanteil_Obergrenze = 20 * faktor,
                Zyklen_Hochrechnung = 2400 * faktor,
                Ertrag_Bezugsersparnis = 500 * faktor,
                Ertrag_Verguetung_Entgangen = 60 * faktor,
                Ertrag_Netzerloes = 30 * faktor,
                Kosten_Ladung = 20 * faktor,
                Ertrag_Leistungspreis = 10 * faktor,
                Verschleisskosten = 40 * faktor,
                Investition = 9000 * faktor,
                Annuitaet = 700 * faktor,
                Jahresueberschuss = 420 * faktor,
                Ertrag_Jahr1 = 430 * faktor,
                Ertrag_Aequivalent = 440 * faktor,
                Kapitalwert = 1234 * faktor
            };
        }

        /// <summary>Ein Engine-Ergebnis mit Erzeugung — der Regelfall.</summary>
        private static SpeicherErgebnis Ergebnis(double erzeugungPv = 5000.0)
        {
            var kennzahlen = new SpeicherKennzahlen
            {
                LastKwh = 12000.0,
                ErzeugungPvKwh = erzeugungPv,
                ErzeugungBhkwKwh = 0.0,
                DirektverbrauchKwh = 3000.0
            };

            var wirtschaft = new SpeicherEngine.WirtschaftlichkeitErgebnis
            {
                StatischeAmortisation = Amortisation.Jahreswert(7.25),
                DynamischeAmortisation = Amortisation.UeberNutzungsdauer
            };

            return new SpeicherErgebnis(
                new double[4], new double[4], 0.0, 0.0, 0.0,
                SpeicherModus.Energetisch, wirtschaft, kennzahlen);
        }

        /// <summary>
        /// <b>39, nicht 40.</b> Die Vermessung nennt „40 Zeilen (18 Energie, 8 Speicher,
        /// 14 Wirtschaft)"; nachgezaehlt am Vorlaeufer sind es 17 Energiezeilen — die
        /// Eigenverbrauchsquote steht in einer if/else-Verzweigung und ist dort
        /// offenbar doppelt gezaehlt worden. Der Block hier bildet den Vorlaeufer
        /// zeilengenau ab (iU9-W11a.3, Berichtigung zur Vermessung § R10).
        /// </summary>
        [Fact]
        public void Zeilen_ohne_Vergleich_und_ohne_Preissteuerung_sind_neununddreissig()
        {
            var zeilen = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(), null);

            Assert.Equal(39, zeilen.Count);
            Assert.Equal(17, zeilen.Count(z => z.Gruppe == SpeicherKennzahlenBlock.GRUPPE_ENERGIE));
            Assert.Equal(8, zeilen.Count(z => z.Gruppe == SpeicherKennzahlenBlock.GRUPPE_SPEICHER));
            Assert.Equal(14, zeilen.Count(z => z.Gruppe == SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT));
        }

        /// <summary>Ohne Vergleichslauf bleibt die Vergleichsspalte in jeder Zeile leer.</summary>
        [Fact]
        public void Zeilen_ohne_Vergleich_lassen_die_Vergleichsspalte_leer()
        {
            var zeilen = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(), null);
            Assert.All(zeilen, z => Assert.Equal("", z.Vergleich));
        }

        /// <summary>
        /// Mit Vergleichslauf tragen die vergleichbaren Zeilen einen zweiten Wert.
        /// Investition und Annuitaet bekommen bewusst KEINEN: Sie haengen an den
        /// Parametern, nicht an der Betriebsstrategie.
        /// </summary>
        [Fact]
        public void Zeilen_mit_Vergleich_fuellen_die_zweite_Spalte()
        {
            var zeilen = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(), null,
                                                        Modell(2.0), Ergebnis());

            Assert.True(zeilen.Count(z => z.Vergleich.Length > 0) > 20);

            // Die Zaehlung darf sich durch den Vergleich nicht aendern.
            Assert.Equal(39, zeilen.Count);
        }

        /// <summary>
        /// Abnahmebefund 2: Ohne Erzeugung ist die Eigenverbrauchsquote unbestimmt (0/0)
        /// und wird als Gedankenstrich gezeigt, nicht als 0 %.
        /// </summary>
        [Fact]
        public void Eigenverbrauchsquote_ohne_Erzeugung_ist_unbestimmt()
        {
            var zeilen = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(0.0), null);

            var zeile = zeilen.Single(z => z.Einheit == "%" &&
                                           z.Wert == SpeicherKennzahlenBlock.UNBESTIMMT);
            Assert.Equal(SpeicherKennzahlenBlock.GRUPPE_ENERGIE, zeile.Gruppe);
            Assert.Equal(KennzahlStufe.Unbestimmt, zeile.Stufe);
        }

        [Fact]
        public void Zeilen_ohne_Modell_bleiben_leer()
        {
            Assert.Empty(SpeicherKennzahlenBlock.Zeilen(null, Ergebnis(), null));
            Assert.Empty(SpeicherKennzahlenBlock.Zeilen(Modell(), null, null));
        }

        /// <summary>
        /// Die Zyklenstaffelung (Fachkonzept 5.4/7.1): gruen bis 90 % des Budgets, gelb
        /// darueber, rot bei Ueberschreitung, unbestimmt ohne gepflegte N_zyk.
        /// </summary>
        [Theory]
        [InlineData(0.0, 5000.0, KennzahlStufe.Unbestimmt)]
        [InlineData(10000.0, 5000.0, KennzahlStufe.Ok)]
        [InlineData(10000.0, 9500.0, KennzahlStufe.Knapp)]
        [InlineData(10000.0, 10001.0, KennzahlStufe.Ueberschritten)]
        public void Zyklenstufe_folgt_der_Neunzigprozentregel(double budget, double hochrechnung,
                                                              KennzahlStufe erwartet)
        {
            var k = new ErgebnisStromspeicherModel { Zyklen_Hochrechnung = hochrechnung };
            var kontext = new StromspeicherLaufKontext { ZyklenZugesichert = budget };

            Assert.Equal(erwartet, SpeicherKennzahlenBlock.Zyklenstufe(k, kontext));
        }

        [Fact]
        public void Zyklenstufe_ohne_Kontext_ist_unbestimmt()
        {
            Assert.Equal(KennzahlStufe.Unbestimmt,
                         SpeicherKennzahlenBlock.Zyklenstufe(new ErgebnisStromspeicherModel(), null));
        }

        [Theory]
        [InlineData(0.0, 50.0, KennzahlStufe.Unbestimmt)]
        [InlineData(1000.0, 50.0, KennzahlStufe.Ok)]
        [InlineData(1000.0, 95.0, KennzahlStufe.Knapp)]
        [InlineData(1000.0, 101.0, KennzahlStufe.Ueberschritten)]
        public void Budgetstufe_folgt_derselben_Staffelung(double budget, double auslastung,
                                                           KennzahlStufe erwartet)
        {
            // BudgetauslastungProzent ist abgeleitet (100 * Entladeenergie / Budget) —
            // gesetzt wird deshalb die Entladeenergie, die zu der Auslastung fuehrt.
            var a = new ArbitrageKennzahlen
            {
                ZyklenbudgetDcKwhProA = budget,
                EntladeenergieDcGesamtKwh = budget * auslastung / 100.0
            };

            Assert.Equal(erwartet, SpeicherKennzahlenBlock.Budgetstufe(a));
        }

        [Fact]
        public void VerkaufKwh_ohne_Preissteuerung_ist_null()
        {
            Assert.Equal(0.0, SpeicherKennzahlenBlock.VerkaufKwh(null));
            Assert.Equal(0.0, SpeicherKennzahlenBlock.VerkaufKwh(new StromspeicherLaufKontext()));
        }

        /// <summary>
        /// Die Amortisation trennt Zustand und Zahl: Die beiden Sonderfaelle liefern den
        /// Klartext des Katalogs, sonst die Jahre.
        /// </summary>
        [Fact]
        public void AmortisationText_nennt_die_beiden_Sonderfaelle()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_NICHT_AMORTISIERBAR,
                         SpeicherKennzahlenBlock.AmortisationText(Amortisation.NichtAmortisierbar));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_UEBER_NUTZUNGSDAUER,
                         SpeicherKennzahlenBlock.AmortisationText(Amortisation.UeberNutzungsdauer));

            string jahre = SpeicherKennzahlenBlock.AmortisationText(Amortisation.Jahreswert(7.25));
            Assert.Contains("7", jahre);
        }

        /// <summary>
        /// Die Kurvennamen und Kennzahltexte kommen aus <c>MyResource.Resource</c> und
        /// folgen der Oberflaechensprache des Fadens. Stellt de-DE ein und beim Verlassen
        /// die vorherige Sprache wieder her (Muster seit iU9-W8).
        /// </summary>
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
