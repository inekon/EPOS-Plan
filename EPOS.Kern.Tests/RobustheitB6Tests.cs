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
    ///
    /// <para><b>Gruppe 2 — Emissionsbilanz:</b> Katalog der Kraftwerksparks, Laden des
    /// Ergebnisses, biogene Einstufung und Faktoren des Referenzkessels nennen ihr
    /// Scheitern im Hinweis der Bilanz („Emissionsbilanz — Stufe „X“ nicht ausführbar").
    /// <b>Gruppe 3 — Emissionsquelle:</b> Die benannten Rückfälle der Lesekette bleiben,
    /// ein Lesefehler steht in <see cref="Emissionsfaktoren.Lesefehler"/> und an der
    /// Herkunft. <b>Gruppe 4 — Gesetzeskatalog:</b> Rückfallebene mit Grund
    /// (<see cref="GesetzKatalog.Lesefehler"/>), die Pflegewege mit
    /// <see cref="GesetzKatalog.LetzterFehler"/>, die Saat mit benannter Tabellenanlage.</para>
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

        // =================================================================
        //  Gruppe 2 — Emissionsbilanz
        // =================================================================

        /// <summary>
        /// Gruppe 2: Lässt sich der Brennstoffstamm nicht lesen, nennt die Emissionsbilanz
        /// die zwei Stufen, die daran hängen — die biogene Einstufung der BHKW-Träger und
        /// die Faktoren des Referenzkessels —, statt still „kein Faktor" zu melden.
        /// </summary>
        [Fact]
        public void Die_Emissionsbilanz_nennt_gescheiterte_Stufen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(1030);
            p.IdKraftwerkspark = 1;
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Brennstoff_Stamm RENAME TO Tab_Brennstoff_Stamm_B6");

            EmissionsBilanz b = EmissionsBilanzRechner.Berechne(1030, p);

            Assert.NotNull(b);
            Assert.Contains("Emissionsbilanz — Stufe „Faktoren des Referenzkessels“ nicht ausführbar: ", b.Hinweis);
            Assert.Contains("Emissionsbilanz — Stufe „biogene Einstufung eines Energieträgers“ nicht ausführbar: ", b.Hinweis);
            Assert.Contains("Tab_Brennstoff_Stamm", b.Hinweis);
        }

        /// <summary>Gruppe 2: Ist der Katalog der Kraftwerksparks nicht lesbar, liefert
        /// die Bilanz eines Projekts mit gewähltem Park eine Zeile mit dem Grund statt
        /// still keiner Bilanz.</summary>
        [Fact]
        public void Ein_unlesbarer_Parkkatalog_ist_keine_fehlende_Wahl()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(1030);
            p.IdKraftwerkspark = 1;
            EmissionsBilanzRechner.LadeKatalog();   // legt den Katalog an, falls er fehlt
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Kraftwerkspark RENAME COLUMN Bezeichner TO Bezeichner_B6");

            EmissionsBilanz b = EmissionsBilanzRechner.Berechne(1030, p);

            Assert.NotNull(b);
            Assert.Null(b.CO2GekoppeltT);
            Assert.StartsWith("Emissionsbilanz — Stufe „Katalog der Kraftwerksparks“ nicht ausführbar: ", b.Hinweis);
            Assert.NotNull(EmissionsBilanzRechner.Katalogfehler);
        }

        // =================================================================
        //  Gruppe 3 — Emissionsquelle
        // =================================================================

        /// <summary>
        /// Gruppe 3: Der Rückfall der Lesekette bleibt, wie er war (Faktor 0, nicht
        /// gepflegt, Vorgabewert Wärme 200 g/kWh), aber ein Lesefehler steht jetzt in
        /// <see cref="Emissionsfaktoren.Lesefehler"/> und an der Herkunft — „nicht
        /// lesbar" statt „nicht gepflegt".
        /// </summary>
        [Fact]
        public void Die_Emissionsquelle_nennt_Lesefehler_an_der_Herkunft()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Brennstoff_Stamm RENAME TO Tab_Brennstoff_Stamm_B6");
            Emissionsfaktoren f = Emissionsquelle.Fuer(1030, 0, 3, DbWerte.EMISSION_MODUS_CO2);
            Assert.False(f.Co2Gepflegt);
            Assert.Equal(0.0, f.Co2GKwh);
            Assert.NotNull(f.Lesefehler);
            Assert.Contains("Brennstoffkatalog nicht lesbar: ", f.Herkunft);

            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Energieanlagen RENAME TO Tab_Energieanlagen_B6");
            Emissionsfaktoren w = Emissionsquelle.Waerme(1030, DbWerte.EMISSION_MODUS_CO2);
            Assert.Equal(Emissionsquelle.WAERME_RUECKFALL_G_JE_KWH, w.Co2GKwh);
            Assert.NotNull(w.Lesefehler);
            Assert.Contains("Energieträger des Wärmeerzeugers nicht lesbar: ", w.Herkunft);
        }

        /// <summary>Gruppe 3: Scheitern beide Abfragen des Stromträgers, bleibt es bei 0
        /// und dem Vorgabewert Strommix — mit Grund an der Herkunft.</summary>
        [Fact]
        public void Ein_unlesbarer_Stromtraeger_steht_an_der_Herkunft_des_Netzstroms()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("ALTER TABLE energy_project_settings RENAME TO energy_project_settings_B6");

            Assert.Equal(0, Emissionsquelle.StromTraeger(1030, out string grund));
            Assert.NotNull(grund);

            Emissionsfaktoren f = Emissionsquelle.Netzstrom(1030, DbWerte.EMISSION_MODUS_CO2);
            Assert.NotNull(f.Lesefehler);
            Assert.Contains("Stromträger des Projekts nicht lesbar: ", f.Herkunft);
        }

        // =================================================================
        //  Gruppe 4 — Gesetzeskatalog
        // =================================================================

        /// <summary>
        /// Gruppe 4: Lässt sich die Katalogtabelle nicht lesen, rechnet die Fassade wie
        /// bisher aus der Rückfallebene (<see cref="GesetzKatalog.Vorbelegung"/>) — aber
        /// <see cref="GesetzKatalog.Lesefehler"/> nennt den Grund, statt dass die Rückfall-
        /// ebene aussieht wie eine leere Tabelle.
        /// </summary>
        [Fact]
        public void Der_Katalog_nennt_den_Grund_seiner_Rueckfallebene()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var gelesen = new GesetzKatalog();
            Assert.NotNull(gelesen.Wert(DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE, 2027));
            Assert.False(gelesen.AusRueckfallebene);
            Assert.Null(gelesen.Lesefehler);

            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Gesetzesparameter RENAME COLUMN Quelle TO Quelle_B6");
            var kaputt = new GesetzKatalog();
            Assert.Equal(2030.0, kaputt.Wert(DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE, 2027));   // aus der Vorbelegung
            Assert.True(kaputt.AusRueckfallebene);
            Assert.NotNull(kaputt.Lesefehler);
            Assert.Contains("Quelle", kaputt.Lesefehler);
        }

        /// <summary>Gruppe 4: Die Schreibwege der Pflegemaske melden ihr Scheitern weiter
        /// über den Rückgabewert — und nennen den Grund in
        /// <see cref="GesetzKatalog.LetzterFehler"/>.</summary>
        [Fact]
        public void Die_Katalogpflege_nennt_den_Grund_eines_Fehlschlags()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.False(GesetzKatalog.Existiert(DbWerte.GESETZ_KLASSE_KWKG, "GIBT_ES_NICHT", 2026, 0));
            Assert.Null(GesetzKatalog.LetzterFehler);

            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Gesetzesparameter RENAME COLUMN JahrVon TO JahrVon_B6");

            Assert.False(GesetzKatalog.Existiert(DbWerte.GESETZ_KLASSE_KWKG, "GIBT_ES_NICHT", 2026, 0));
            Assert.NotNull(GesetzKatalog.LetzterFehler);
            Assert.Contains("JahrVon", GesetzKatalog.LetzterFehler);
        }
    }
}
