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

        // =================================================================
        // Anwenderwunsch 08.09.2026, W11b-B-14: der gegliederte Wirtschaftsblock
        // =================================================================

        private static string Text(string schluessel)
        {
            switch (schluessel)
            {
                case "UG_JAHR": return WindowsFormsApplication1.MyResource.Resource.SP_ERG_UG_REFERENZJAHR;
                case "UG_DAUER": return WindowsFormsApplication1.MyResource.Resource.SP_ERG_UG_NUTZUNGSDAUER;
                case "UG_NACH": return WindowsFormsApplication1.MyResource.Resource.SP_ERG_UG_NACHRICHTLICH;
                default: throw new ArgumentException(schluessel);
            }
        }

        /// <summary>Nur die Wirtschaftszeilen, in ihrer Reihenfolge.</summary>
        private static System.Collections.Generic.List<SpeicherKennzahlenBlock.Zeile> Wirtschaft(
            ErgebnisStromspeicherModel k = null)
        {
            return SpeicherKennzahlenBlock.Zeilen(k ?? Modell(), Ergebnis(), null)
                                          .Where(z => z.Gruppe == SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT)
                                          .ToList();
        }

        /// <summary>Die formatierte Zahl einer Zeile wieder als Zahl.</summary>
        private static double Wert(SpeicherKennzahlenBlock.Zeile z)
        {
            return double.Parse(z.Wert, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Die vierzehn Wirtschaftszeilen stehen in DREI Unterabschnitten, jeder in
        /// einem Stueck und in dieser Reihenfolge: Referenzjahr, Nutzungsdauer,
        /// Nachrichtliches. Vorher war es eine ungegliederte Reihe, in der die
        /// Verschleisskosten mitten zwischen den Summanden standen.
        /// </summary>
        [Fact]
        public void Der_Wirtschaftsblock_steht_in_drei_Unterabschnitten()
        {
            using var _ = new DeutscheAnzeige();

            var w = Wirtschaft();
            string jahr = Text("UG_JAHR"), dauer = Text("UG_DAUER"), nach = Text("UG_NACH");

            Assert.Equal(14, w.Count);
            Assert.Equal(new[]
            {
                jahr, jahr, jahr, jahr, jahr, jahr,
                dauer, dauer, dauer, dauer, dauer, dauer, dauer,
                nach
            }, w.Select(z => z.Untergruppe).ToArray());

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_ERTRAG_JAHR1, w[5].Bezeichnung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_JAHRESUEBERSCHUSS, w[9].Bezeichnung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_KAPITALWERT, w[12].Bezeichnung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_VERSCHLEISS, w[13].Bezeichnung);
        }

        /// <summary>
        /// Was Summe ist, was Ergebnis und was nur nachrichtlich dabeisteht, sagt der
        /// KERN — die Oberflaeche macht daraus nur Fettschrift und Linie.
        /// </summary>
        [Fact]
        public void Summe_Ergebnis_und_Nachrichtliches_sind_gekennzeichnet()
        {
            using var _ = new DeutscheAnzeige();

            var w = Wirtschaft();

            Assert.Equal(KennzahlArt.Summe, w[5].Art);           // E_a,1
            Assert.Equal(KennzahlArt.Summe, w[9].Art);           // dJ
            Assert.Equal(KennzahlArt.Ergebnis, w[12].Art);       // NPV
            Assert.Equal(KennzahlArt.Nachrichtlich, w[13].Art);  // K_ver
            Assert.Equal(10, w.Count(z => z.Art == KennzahlArt.Normal));
        }

        /// <summary>
        /// <b>Die Spalte addiert sich.</b> Die Engine bildet E_a,1 als Summe der
        /// Zeitschrittbewertung; deren Summanden sind Bezugsersparnis (+), entgangene
        /// Verguetung (−), Netzerloes (+) und Ladekosten (−) — <c>Arbitrage</c>
        /// :309-325. Genau so muessen die fuenf Posten des Referenzjahrs auf dem Schirm
        /// stehen, sonst ist die Summenzeile darunter eine Behauptung ohne Rechnung.
        /// </summary>
        [Fact]
        public void Die_Posten_des_Referenzjahrs_addieren_sich_zur_Summenzeile()
        {
            using var _ = new DeutscheAnzeige();

            var k = Modell();
            k.Ertrag_Bezugsersparnis = 500.0;
            k.Ertrag_Verguetung_Entgangen = 60.0;
            k.Ertrag_Netzerloes = 30.0;
            k.Kosten_Ladung = 20.0;
            k.Ertrag_Leistungspreis = 0.0;
            k.Ertrag_Jahr1 = 500.0 - 60.0 + 30.0 - 20.0;   // 450 - so rechnet die Engine

            var w = Wirtschaft(k);
            double summe = Wert(w[0]) + Wert(w[1]) + Wert(w[2]) + Wert(w[3]) + Wert(w[4]);

            Assert.Equal(450.0, summe, 2);
            Assert.Equal(450.0, Wert(w[5]), 2);
        }

        /// <summary>
        /// Abzuege stehen mit MINUSZEICHEN — die entgangene Verguetung tat es schon,
        /// die Netzladung und die Annuitaet nicht. Beide gehen negativ in die Summe ein
        /// (<c>Arbitrage</c> :324 bzw. <c>Wirtschaftlichkeit</c> :158).
        /// </summary>
        [Fact]
        public void Abzuege_und_Kosten_stehen_mit_Minuszeichen()
        {
            using var _ = new DeutscheAnzeige();

            var k = Modell();
            k.Ertrag_Verguetung_Entgangen = 60.0;
            k.Kosten_Ladung = 20.0;
            k.Annuitaet = 700.0;

            var w = Wirtschaft(k);

            Assert.Equal(-60.0, Wert(w[1]), 2);    // entgangene Einspeiseverguetung
            Assert.Equal(-20.0, Wert(w[3]), 2);    // Netzladung
            Assert.Equal(-700.0, Wert(w[8]), 2);   // Annuitaet A
        }

        /// <summary>
        /// <b>Ohne Investition ist die Amortisation nicht bestimmbar.</b> Die Engine
        /// liefert bei I = 0 statisch 0 und dynamisch ein negatives Null; auf dem Schirm
        /// stand „0,0 a“ und „−0,0 a“. Beides ist keine Aussage — jetzt steht dort der
        /// Gedankenstrich samt Begruendung im Werkzeugtipp.
        /// </summary>
        [Fact]
        public void Ohne_Investition_ist_die_Amortisation_unbestimmt()
        {
            using var _ = new DeutscheAnzeige();

            var k = Modell();
            k.Investition = 0.0;

            var w = Wirtschaft(k);

            Assert.Equal(SpeicherKennzahlenBlock.UNBESTIMMT, w[10].Wert);
            Assert.Equal(SpeicherKennzahlenBlock.UNBESTIMMT, w[11].Wert);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_AMORT_OHNE_INVEST, w[10].Hinweis);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_AMORT_OHNE_INVEST, w[11].Hinweis);

            // Mit Investition bleibt es bei der Jahreszahl und ohne Hinweis.
            var mit = Wirtschaft();
            Assert.Contains("7", mit[10].Wert);
            Assert.Equal("", mit[10].Hinweis);
        }

        /// <summary>
        /// Die Kuerzel des Blocks erklaeren sich im Werkzeugtipp — E_a,1, E_a,aeq, dJ,
        /// NPV, K_ver und die beiden Zyklenzahlen. Sie standen bis dahin unerklaert da.
        /// </summary>
        [Fact]
        public void Die_Kuerzel_tragen_ihren_Werkzeugtipp()
        {
            using var _ = new DeutscheAnzeige();

            var alle = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(), null);

            void Tipp(string bezeichnung, string erwartet)
            {
                var z = alle.Single(x => x.Bezeichnung == bezeichnung);
                Assert.False(string.IsNullOrEmpty(erwartet), "Leerer Werkzeugtipp im Katalog");
                Assert.Equal(erwartet, z.Hinweis);
            }

            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_ERTRAG_JAHR1,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_E_A1);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_ERTRAG_AEQUIVALENT,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_E_AEQ);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_JAHRESUEBERSCHUSS,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_DELTA_J);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_KAPITALWERT,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_NPV);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_VERSCHLEISS,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_K_VER);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_VOLLZYKLEN,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_N_ZYK_AEQ);
            Tipp(WindowsFormsApplication1.MyResource.Resource.SP_ERG_ZYKLEN_ZUGESICHERT,
                 WindowsFormsApplication1.MyResource.Resource.SP_ERG_TIP_N_ZYK);
        }

        /// <summary>
        /// Energie und Speicher bleiben UNGEGLIEDERT — die Vorgabewerte des Datensatzes
        /// halten jeden Aufrufer unveraendert, der von Untergruppe und Art nichts weiss.
        /// </summary>
        [Fact]
        public void Energie_und_Speicher_bleiben_ungegliedert()
        {
            var andere = SpeicherKennzahlenBlock.Zeilen(Modell(), Ergebnis(), null)
                                                .Where(z => z.Gruppe != SpeicherKennzahlenBlock.GRUPPE_WIRTSCHAFT)
                                                .ToList();

            Assert.Equal(25, andere.Count);
            Assert.All(andere, z => Assert.Equal("", z.Untergruppe));
            Assert.All(andere, z => Assert.Equal(KennzahlArt.Normal, z.Art));
        }

        /// <summary>
        /// Wie <see cref="DeutscheOberflaeche"/>, aber mit der ZAHLENkultur dazu: Die
        /// Werte des Blocks sind mit <c>CultureInfo.CurrentCulture</c> formatiert, und
        /// ein Test, der „-700,00“ erwartet, muss beide Kulturen festnageln.
        /// </summary>
        private sealed class DeutscheAnzeige : IDisposable
        {
            private readonly System.Globalization.CultureInfo _oberflaeche =
                System.Threading.Thread.CurrentThread.CurrentUICulture;
            private readonly System.Globalization.CultureInfo _zahlen =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            public DeutscheAnzeige()
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
