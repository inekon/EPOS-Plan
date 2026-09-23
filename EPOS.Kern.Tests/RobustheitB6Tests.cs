using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c3 — <b>Befund B‑6 „Fehler werden geschluckt (catch {} ⇒ still 0)"</b>
    /// (Konzept § 4, § 7: „B‑6 ist Robustheit"). Ein Fehler in einer Teilprüfung oder
    /// Rechenstufe wird zur Zeile „… nicht ausführbar: &lt;Grund&gt;“, nie still.
    ///
    /// <para><b>Gruppe 1 — Kohärenzprüfung:</b> Die zehn Teilprüfungen laufen weiter je
    /// für sich gekapselt, aber über <c>KohaerenzPruefung.Teilpruefung</c>: Scheitert
    /// eine, steht an ihrer Stelle eine WARNUNG mit Prüfungsname und Grund. Ihre
    /// Lesehelfer tragen kein eigenes <c>catch</c> mehr — ein Lesefehler erreicht die
    /// Teilprüfung.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class RobustheitB6Tests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // =================================================================
        //  Der Grund
        // =================================================================

        [Fact]
        public void Der_Grund_nennt_Fehlerart_und_Meldung_einzeilig_und_gekuerzt()
        {
            Assert.Equal("", Fehlergrund.Text(null));
            Assert.Equal("InvalidOperationException: Probe", Fehlergrund.Text(new InvalidOperationException("Probe")));
            Assert.Equal("InvalidOperationException: Zeile 1 Zeile 2",
                         Fehlergrund.Text(new InvalidOperationException("Zeile 1\r\nZeile 2")));

            // Ein Aufruf per Reflexion nennt den inneren Fehler.
            var innen = new ArgumentException("innen");
            Assert.Equal("ArgumentException: innen",
                         Fehlergrund.Text(new TargetInvocationException(innen)));

            string lang = Fehlergrund.Text(new Exception(new string('x', 500)));
            Assert.Equal(Fehlergrund.HOECHSTLAENGE, lang.Length);
            Assert.EndsWith("…", lang);
        }

        // =================================================================
        //  Gruppe 1 — Kohärenzprüfung
        // =================================================================

        [Fact]
        public void Eine_gescheiterte_Teilpruefung_wird_zur_Warnzeile()
        {
            var liste = new List<KohaerenzHinweis>();
            KohaerenzPruefung.Teilpruefung(KohaerenzPruefung.TP_HILFSENERGIE,
                () => throw new InvalidOperationException("Probe B-6"), DE, liste);

            KohaerenzHinweis h = Assert.Single(liste);
            Assert.Equal(KohaerenzSchwere.WARNUNG, h.Schwere);
            Assert.Null(h.Betrag);
            Assert.Equal("Prüfung „Doppelpflege der Hilfsenergie“ nicht ausführbar: " +
                         "InvalidOperationException: Probe B-6", h.Text);
            Assert.Equal(string.Format(DE, Resource.KOH_PRUEFUNG_NICHT_AUSFUEHRBAR,
                                       Resource.KOH_TP_HILFSENERGIE, "InvalidOperationException: Probe B-6"),
                         h.Text);
        }

        [Fact]
        public void Eine_bestandene_Teilpruefung_schreibt_nur_ihre_eigenen_Zeilen()
        {
            var liste = new List<KohaerenzHinweis>();
            KohaerenzPruefung.Teilpruefung(KohaerenzPruefung.TP_STROM,
                () => liste.Add(new KohaerenzHinweis { Text = "eigene Zeile" }), DE, liste);

            Assert.Equal("eigene Zeile", Assert.Single(liste).Text);
        }

        /// <summary>Jede der zehn Teilprüfungen hat ihren Namen in beiden Sprachen.</summary>
        [Fact]
        public void Jede_Teilpruefung_traegt_einen_Namen()
        {
            string[] schluessel =
            {
                KohaerenzPruefung.TP_HILFSENERGIE, KohaerenzPruefung.TP_PV_HERKUNFT,
                KohaerenzPruefung.TP_CO2_DOPPEL, KohaerenzPruefung.TP_STROMMIX,
                KohaerenzPruefung.TP_KWKG, KohaerenzPruefung.TP_BRENNSTOFF,
                KohaerenzPruefung.TP_MISCHLAGE, KohaerenzPruefung.TP_STROM,
                KohaerenzPruefung.TP_ERLAUBNIS, KohaerenzPruefung.TP_DOPPELZAEHLUNG
            };
            Assert.Equal(10, schluessel.Distinct().Count());
            foreach (string s in schluessel)
            {
                Assert.False(string.IsNullOrEmpty(Resource.ResourceManager.GetString(s, DE)), s + " (de)");
                Assert.False(string.IsNullOrEmpty(Resource.ResourceManager.GetString(s, CultureInfo.GetCultureInfo("en-US"))),
                             s + " (en)");
            }
        }

        /// <summary>
        /// Am echten Weg: Fehlt der Kohärenzprüfung eine Tabelle (hier
        /// <c>Tab_Energieanlagen</c>, auf der eigenen Kopie umbenannt), meldet die
        /// Teilprüfung „Doppelpflege der Hilfsenergie" ihren Grund — bis E7c3 lieferte sie
        /// still eine leere Liste. Dieselbe Zeile zeigt der Kostendialog (U31).
        /// </summary>
        [Fact]
        public void Ein_Lesefehler_der_Pruefung_steht_in_der_Kohaerenzliste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Energieanlagen RENAME TO Tab_Energieanlagen_B6");

            List<KohaerenzHinweis> liste = KohaerenzPruefung.Pruefe(1030, null);

            KohaerenzHinweis h = Assert.Single(liste, x => x.Text.StartsWith(
                "Prüfung „Doppelpflege der Hilfsenergie“ nicht ausführbar: ", StringComparison.Ordinal));
            Assert.Equal(KohaerenzSchwere.WARNUNG, h.Schwere);
            Assert.Contains("Tab_Energieanlagen", h.Text);

            string dialog = KohaerenzPruefung.HilfsenergieDoppelpflege(1030, 14920);
            Assert.StartsWith("Prüfung „Doppelpflege der Hilfsenergie“ nicht ausführbar: ", dialog);
        }
    }
}
