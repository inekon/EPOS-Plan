using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Parametersatz des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 2.1, 3.3, Kapitel 6 (a); Stufe Z0, Posten P5).
    ///
    /// <para>Alle Schlüssel und Werte sind ERFUNDEN („Probe.Eins" = 1, „Probe.Zwei" = 20):
    /// Die Fälle prüfen Laden, Unveränderlichkeit und die benannten Ablehnungen, nie eine
    /// Normzahl. Die Datenbankfälle arbeiten in einer eigenen leeren Datei
    /// (<see cref="TwwTestdatenbank"/>), nie in der Testdatenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ParametersatzTests
    {
        private static readonly Provenienz Fiktiv =
            new Provenienz(TwwTestdatenbank.QUELLE, null, "T1", Herkunftsart.Fiktiv);

        // =================================================================================
        // 1 — Die Klasse ohne Datenbank
        // =================================================================================

        [Fact]
        public void Der_Satz_liefert_jeden_Wert_seiner_Katalogversion()
        {
            Parametersatz ps = Parametersatz.Aus("T1", new[]
            {
                new ZapfParameterwert("Probe.Eins", 1.0, "K", Fiktiv),
                new ZapfParameterwert("Probe.Zwei", 20.0, null, Fiktiv)
            });

            Assert.Equal("T1", ps.Katalogversion);
            Assert.Equal(2, ps.Anzahl);
            Assert.Equal(1.0, ps.Wert("Probe.Eins"));
            Assert.Equal(20.0, ps.Wert("Probe.Zwei"));
            Assert.Equal("K", ps.Lies("Probe.Eins").Einheit);
            Assert.Equal(Herkunftsart.Fiktiv, ps.Lies("Probe.Zwei").Herkunft.Art);
            Assert.True(ps.Enthaelt("Probe.Eins"));
            Assert.False(ps.Enthaelt("probe.eins"));   // ordinal: Gross/Klein zaehlt
            Assert.False(ps.Enthaelt(null));
        }

        [Fact]
        public void Ein_fehlender_Schluessel_wird_benannt_abgelehnt_statt_ersetzt()
        {
            Parametersatz ps = Parametersatz.Aus("T1", new[] { new ZapfParameterwert("Probe.Eins", 1.0, null, Fiktiv) });

            ParametersatzException ex = Assert.Throws<ParametersatzException>(() => ps.Wert("Probe.Fehlt"));
            Assert.Equal(ParametersatzFehler.ParameterFehlt, ex.Fehler);
            Assert.Equal("Probe.Fehlt", ex.Schluessel);
            Assert.Equal("T1", ex.Katalogversion);
            Assert.Contains("Parameter fehlt", ex.Message);
            Assert.Contains("Probe.Fehlt", ex.Message);
        }

        [Fact]
        public void Der_Satz_ist_unveraenderlich_gegen_seine_Quelle()
        {
            var quelle = new List<ZapfParameterwert> { new ZapfParameterwert("Probe.Eins", 1.0, null, Fiktiv) };
            Parametersatz ps = Parametersatz.Aus("T1", quelle);

            quelle.Add(new ZapfParameterwert("Probe.Zwei", 2.0, null, Fiktiv));
            quelle[0] = new ZapfParameterwert("Probe.Eins", 99.0, null, Fiktiv);

            Assert.Equal(1, ps.Anzahl);
            Assert.Equal(1.0, ps.Wert("Probe.Eins"));
            Assert.False(ps.Werte is IDictionary<string, ZapfParameterwert> d && !d.IsReadOnly);
        }

        [Fact]
        public void Leere_Version_und_doppelter_Schluessel_werden_abgelehnt()
        {
            ParametersatzException leer = Assert.Throws<ParametersatzException>(
                () => Parametersatz.Aus("T1", new ZapfParameterwert[0]));
            Assert.Equal(ParametersatzFehler.KatalogversionFehlt, leer.Fehler);

            ParametersatzException ohne = Assert.Throws<ParametersatzException>(
                () => Parametersatz.Aus("", new[] { new ZapfParameterwert("Probe.Eins", 1.0, null, Fiktiv) }));
            Assert.Equal(ParametersatzFehler.KeineKatalogversion, ohne.Fehler);

            Assert.Throws<System.ArgumentException>(() => Parametersatz.Aus("T1", new[]
            {
                new ZapfParameterwert("Probe.Eins", 1.0, null, Fiktiv),
                new ZapfParameterwert("Probe.Eins", 2.0, null, Fiktiv)
            }));
        }

        // =================================================================================
        // 2 — Laden über den Controller (leere Datei mit den Tww-Tabellen)
        // =================================================================================

        [Fact]
        public void Der_Controller_laedt_die_zuletzt_eingespielte_Katalogversion()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 1.0, "T1", "K");
            TwwTestdatenbank.ParameterAnlegen("Probe.Zwei", 2.0, "T1");
            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 10.0, "T2", "K");

            Assert.Equal("T2", ZapfprofilCtrl.AktuelleKatalogversion());

            Parametersatz aktuell = ZapfprofilCtrl.Parameter();
            Assert.Equal("T2", aktuell.Katalogversion);
            Assert.Equal(1, aktuell.Anzahl);
            Assert.Equal(10.0, aktuell.Wert("Probe.Eins"));
            Assert.Equal("K", aktuell.Lies("Probe.Eins").Einheit);
            Assert.Equal(TwwTestdatenbank.QUELLE, aktuell.Lies("Probe.Eins").Herkunft.Quelle);
            Assert.Equal(Herkunftsart.Fiktiv, aktuell.Lies("Probe.Eins").Herkunft.Art);

            // Die aeltere Version bleibt ausdruecklich lesbar - samt ihrem zweiten Schluessel.
            Parametersatz alt = ZapfprofilCtrl.Parameter("T1");
            Assert.Equal(2, alt.Anzahl);
            Assert.Equal(1.0, alt.Wert("Probe.Eins"));
            Assert.Equal(2.0, alt.Wert("Probe.Zwei"));

            // Der Schluessel, den nur T1 kennt, fehlt in T2 benannt - kein Rueckgriff auf T1.
            ParametersatzException ex = Assert.Throws<ParametersatzException>(() => aktuell.Wert("Probe.Zwei"));
            Assert.Equal(ParametersatzFehler.ParameterFehlt, ex.Fehler);
        }

        [Fact]
        public void Eine_fehlende_Katalogversion_wird_benannt_abgelehnt()
        {
            using var db = new TwwTestdatenbank();
            TwwTestdatenbank.ParameterAnlegen("Probe.Eins", 1.0, "T1");

            ParametersatzException ex = Assert.Throws<ParametersatzException>(() => ZapfprofilCtrl.Parameter("T9"));
            Assert.Equal(ParametersatzFehler.KatalogversionFehlt, ex.Fehler);
            Assert.Equal("T9", ex.Katalogversion);
            Assert.Contains("T9", ex.Message);
        }

        [Fact]
        public void Ohne_Parameterzeile_oder_ohne_Tabelle_gibt_es_keine_Katalogversion()
        {
            using (var leer = new TwwTestdatenbank())
            {
                Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
                Assert.Equal(ParametersatzFehler.KeineKatalogversion,
                             Assert.Throws<ParametersatzException>(() => ZapfprofilCtrl.Parameter()).Fehler);
            }

            using (var ohneTabellen = new TwwTestdatenbank(mitTwwSchema: false))
            {
                Assert.Null(ZapfprofilCtrl.AktuelleKatalogversion());
                Assert.Equal(ParametersatzFehler.KeineKatalogversion,
                             Assert.Throws<ParametersatzException>(() => ZapfprofilCtrl.Parameter()).Fehler);
                Assert.Equal(ParametersatzFehler.KeineKatalogversion,
                             Assert.Throws<ParametersatzException>(() => ZapfprofilCtrl.Parameter("T1")).Fehler);
            }
        }
    }
}
