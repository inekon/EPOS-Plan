using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EPOS.UI.Seiten.Berichte;
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
    /// <see cref="GesetzKatalog.LetzterFehler"/>, die Saat mit benannter Tabellenanlage.
    /// <b>Gruppe 5 — Wirtschaftlichkeit:</b> Jede Rechenstufe, deren Scheitern bisher still
    /// in einen Rückfall führte (Parameter, Tarif, Anlagen, Leistung, Träger, Heizöl,
    /// Betriebskosten, Katalog, Kohärenz, Satzherleitung), schreibt an jede Ergebniszeile
    /// „Rechenstufe „X“ nicht ausführbar: &lt;Grund&gt;“; Speichern und Laden nennen ihr
    /// Scheitern an den Zeilen bzw. in <see cref="WirtschaftlichkeitCtrl.Ladefehler"/>.</para>
    ///
    /// <para><b>Der strenge Leseweg.</b> <c>DataRepository</c> meldet einen Abfragefehler
    /// selbst (Dialog, im Engine-Modus still) und liefert eine leere Tabelle bzw.
    /// <c>null</c> — ein <c>catch</c> um den Aufruf griff deshalb nie. Die Lesestellen der
    /// Gruppen lesen über <c>StilleDb.TabelleStreng</c>/<c>ScalarStreng</c>, die den
    /// Fehler weiterreichen; erst damit erreicht ein Abfragefehler die benannte
    /// Behandlung. Die Fälle unten benennen auf ihrer eigenen Kopie eine Tabelle oder
    /// Spalte um.</para>
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

        /// <summary>Der strenge Leseweg reicht den Abfragefehler weiter; die stillen
        /// Fassungen bleiben, wie sie sind (<c>null</c>), und beide lesen dasselbe.</summary>
        [Fact]
        public void Der_strenge_Leseweg_reicht_den_Fehler_weiter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(StilleDb.Tabelle("SELECT * FROM Tab_Gibt_Es_Nicht_B6"));
            Assert.Null(StilleDb.Scalar("SELECT COUNT(*) FROM Tab_Gibt_Es_Nicht_B6"));

            Exception t = Assert.ThrowsAny<Exception>(
                () => StilleDb.TabelleStreng("SELECT * FROM Tab_Gibt_Es_Nicht_B6"));
            Assert.Contains("Tab_Gibt_Es_Nicht_B6", Fehlergrund.Text(t));
            Exception s = Assert.ThrowsAny<Exception>(
                () => StilleDb.ScalarStreng("SELECT COUNT(*) FROM Tab_Gibt_Es_Nicht_B6"));
            Assert.Contains("Tab_Gibt_Es_Nicht_B6", Fehlergrund.Text(s));

            const string SQL = "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?";
            Assert.Equal(Convert.ToInt64(StilleDb.Scalar(SQL, new DbParam("@p", 1030))),
                         Convert.ToInt64(StilleDb.ScalarStreng(SQL, new DbParam("@p", 1030))));
            Assert.Null(StilleDb.ScalarStreng("SELECT NULL"));
            Assert.Equal(StilleDb.Tabelle("SELECT ID FROM Tab_Projekt ORDER BY ID").Rows.Count,
                         StilleDb.TabelleStreng("SELECT ID FROM Tab_Projekt ORDER BY ID").Rows.Count);
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

        // =================================================================
        //  Gruppe 5 — Wirtschaftlichkeit: die Rechenstufen
        // =================================================================

        private const int PROJEKT = 1030;

        /// <summary>Die Kette der Ankertests, ohne Stundenreihen.</summary>
        private static List<WirtschaftlichkeitErgebnis> Rechne(WirtschaftlichkeitParameter p)
        {
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall B-6",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return new WirtschaftlichkeitCtrl().Berechne(daten, p);
        }

        /// <summary>Jede der drei Szenariozeilen trägt GENAU EINE Warnzeile, die mit
        /// <paramref name="anfang"/> beginnt und <paramref name="enthaelt"/> nennt.</summary>
        private static void JedeZeileNennt(List<WirtschaftlichkeitErgebnis> alle, string anfang,
                                           string enthaelt)
        {
            Assert.Equal(3, alle.Count(e => e.IdProjekt == PROJEKT));
            foreach (WirtschaftlichkeitErgebnis e in alle.Where(x => x.IdProjekt == PROJEKT))
            {
                KohaerenzHinweis h = Assert.Single(e.KohaerenzHinweise,
                    x => x.Text.StartsWith(anfang, StringComparison.Ordinal));
                Assert.Equal(KohaerenzSchwere.WARNUNG, h.Schwere);
                Assert.Contains(enthaelt, h.Text);
            }
        }

        private static WirtschaftlichkeitErgebnis Erwartet(List<WirtschaftlichkeitErgebnis> alle)
            => alle.Single(x => x.IdProjekt == PROJEKT && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);

        /// <summary>Gruppe 5: Ohne Fehler steht an keiner Ergebniszeile eine Stufenzeile.</summary>
        [Fact]
        public void Ohne_Fehler_steht_keine_Stufenzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Null(p.Lesefehler);
            foreach (WirtschaftlichkeitErgebnis e in Rechne(p))
                Assert.DoesNotContain(e.KohaerenzHinweise ?? new List<KohaerenzHinweis>(),
                                      h => h.Text.Contains("nicht ausführbar"));
        }

        /// <summary>
        /// Gruppe 5: Lässt sich der Parametersatz nicht lesen (hier ein Datum, das keines
        /// ist), rechnet der Lauf wie bisher mit den Vorgaben — aber jede Ergebniszeile
        /// nennt die Rechenstufe und den Grund, statt wie ein vollständiger Kapitalwert
        /// auszusehen.
        /// </summary>
        [Fact]
        public void Ein_unlesbarer_Parametersatz_steht_an_jeder_Ergebniszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET GeaendertAm = 'kaputt' WHERE ID_Projekt = " + PROJEKT);
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.NotNull(p.Lesefehler);
            Assert.StartsWith("FormatException: ", p.Lesefehler);

            JedeZeileNennt(Rechne(p),
                "Rechenstufe „Parameter des Projekts“ nicht ausführbar: FormatException: ", "kaputt");
        }

        /// <summary>Gruppe 5: Ist die Anlagentabelle nicht lesbar, fehlt die KWKG-Reihe
        /// wie bisher — und die Ergebniszeile nennt die Rechenstufe „Anlagen des
        /// Projekts" samt Grund (bis E7c3 las sich das wie „keine Anlage").</summary>
        [Fact]
        public void Eine_unlesbare_Anlagenliste_steht_an_der_Ergebniszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            DataRepository.ExecuteNonQuery("ALTER TABLE Tab_Energieanlagen RENAME TO Tab_Energieanlagen_B6");

            JedeZeileNennt(Rechne(p),
                "Rechenstufe „Anlagen des Projekts“ nicht ausführbar: ", "Tab_Energieanlagen");
        }

        /// <summary>Gruppe 5: Scheitert das Speichern, gelten die Zahlen des Laufs weiter —
        /// derselbe Kapitalwert —, aber jede Zeile sagt, dass sie NICHT gespeichert ist;
        /// beim nächsten Laden erschiene sonst still der alte Stand.</summary>
        [Fact]
        public void Ein_gescheitertes_Speichern_steht_an_jeder_Ergebniszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            double kapitalwert = Erwartet(Rechne(p)).Kapitalwert.Value;

            DataRepository.ExecuteNonQuery(
                "CREATE TRIGGER b6_stop BEFORE INSERT ON " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS +
                " BEGIN SELECT RAISE(ABORT, 'B6-Probe'); END");
            List<WirtschaftlichkeitErgebnis> alle = Rechne(p);

            JedeZeileNennt(alle, "Rechenstufe „Speichern der Ergebnisse“ nicht ausführbar: ", "B6-Probe");
            Assert.Equal(kapitalwert, Erwartet(alle).Kapitalwert.Value);
        }

        /// <summary>Gruppe 5: Lässt sich der gespeicherte Stand nicht laden, nennt
        /// <see cref="WirtschaftlichkeitCtrl.Ladefehler"/> den Grund, statt dass das Projekt
        /// wie „nie gerechnet" aussieht.</summary>
        [Fact]
        public void Ein_unlesbarer_Ergebnisstand_nennt_den_Ladefehler()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            Rechne(ctrl.LadeParameter(PROJEKT));
            Assert.Equal(3, ctrl.LadeErgebnisse(new List<int> { PROJEKT }).Count);
            Assert.Null(ctrl.Ladefehler);

            DataRepository.ExecuteNonQuery(
                "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS + " SET Zeitstempel = 'kaputt' " +
                "WHERE ID_Projekt = " + PROJEKT);
            List<WirtschaftlichkeitErgebnis> geladen = ctrl.LadeErgebnisse(new List<int> { PROJEKT });

            Assert.NotNull(ctrl.Ladefehler);
            Assert.StartsWith("FormatException: ", ctrl.Ladefehler);
            // Was vor dem Fehler gelesen war, trüge die Zeile „Laden der Ergebnisse";
            // hier scheitert schon das Einlesen der Tabelle.
            foreach (WirtschaftlichkeitErgebnis e in geladen)
                Assert.Contains(e.KohaerenzHinweise, h => h.Text.StartsWith(
                    "Rechenstufe „Laden der Ergebnisse“ nicht ausführbar: ", StringComparison.Ordinal));
        }

        // =================================================================
        //  ETAPPE E13 (E7c3‑Q6 a) — die drei Gründe in der Oberfläche
        // =================================================================

        /// <summary>
        /// Die Zeilen der Oberfläche: je Grund eine, in der Reihenfolge Laden, Speichern,
        /// Vorsorge, mit dem Text des Kerns; derselbe Grund aus zwei Quellen ergibt EINE
        /// Zeile, leere Gründe keine.
        /// </summary>
        [Fact]
        public void Die_Anzeigezeilen_nennen_jeden_Grund_einmal()
        {
            Assert.Empty(Fehlergrund.Anzeigezeilen(null, null, null));
            Assert.Empty(Fehlergrund.Anzeigezeilen(new[] { null, "", "  " }, "", null));

            List<string> zeilen = Fehlergrund.Anzeigezeilen(
                new[] { "SqliteException: no such table", null, "SqliteException: no such table" },
                "FormatException: kaputt",
                "SqliteException: no such table");
            Assert.Equal(new[]
            {
                "Gespeicherte Ergebnisse nicht vollständig gelesen: SqliteException: no such table",
                "Speichern gescheitert: FormatException: kaputt"
            }, zeilen);

            Assert.Equal(new[] { "Tabellenvorsorge unvollständig: IOException: gesperrt" },
                         Fehlergrund.Anzeigezeilen(null, null, "IOException: gesperrt"));

            // Kein Stapel: Die Zeile trägt, was Fehlergrund.Text liefert.
            string grund = Fehlergrund.Text(new InvalidOperationException("Zeile 1\r\n   bei X.Y()"));
            string zeile = Assert.Single(Fehlergrund.Anzeigezeilen(null, grund, null));
            Assert.DoesNotContain("\n", zeile);
            Assert.Equal(string.Format(Resource.WIRT_STATUS_SPEICHERFEHLER, grund), zeile);
        }

        /// <summary>
        /// Die Statuszeile der Ergebnisseite nennt den Ladefehler des Kerns — einmal, mit
        /// dem Text des Kerns —, statt dass die Zeilen still fehlen.
        /// </summary>
        [Fact]
        public void Die_Statuszeile_nennt_den_Ladefehler_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            Rechne(ctrl.LadeParameter(PROJEKT));
            string ohne = Stand(new WirtschaftlichkeitSeiteGaben(PROJEKT, "")).Statuszeile;
            Assert.DoesNotContain("nicht vollständig gelesen", ohne);

            DataRepository.ExecuteNonQuery(
                "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS + " SET Zeitstempel = 'kaputt' " +
                "WHERE ID_Projekt = " + PROJEKT);
            string mit = Stand(new WirtschaftlichkeitSeiteGaben(PROJEKT, "")).Statuszeile;

            const string anfang = "Gespeicherte Ergebnisse nicht vollständig gelesen: FormatException: ";
            Assert.Contains(anfang, mit);
            Assert.Equal(mit.IndexOf(anfang, StringComparison.Ordinal),
                         mit.LastIndexOf(anfang, StringComparison.Ordinal));
            Assert.DoesNotContain("   bei ", mit);    // kein Stapel
        }

        /// <summary>
        /// Scheitert der Schreibweg der Referenzwahl an der Datenbank, erscheint der Grund
        /// EINMAL: Die Zugriffsschicht meldet ihn (<c>DataRepository.FehlerMelden</c>), und
        /// die Statuszeile wiederholt ihn nicht. Vor E13 lief der Kern danach in ein INSERT,
        /// das am eindeutigen Index scheiterte und einen zweiten, falschen Grund meldete.
        /// </summary>
        [Fact]
        public void Ein_Datenbankfehler_der_Referenzwahl_erscheint_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);   // Vorsorge vor dem Auslöser
            DataRepository.ExecuteNonQuery(
                "CREATE TRIGGER e13_stop BEFORE UPDATE ON " + WirtschaftlichkeitCtrl.TAB_PARAMETER +
                " BEGIN SELECT RAISE(ABORT, 'E13-Probe'); END");

            var seite = new WirtschaftlichkeitSeiteGaben(PROJEKT, "");
            IReadOnlyDictionary<string, object> gaben = seite.Gaben();
            var referenz = (Func<int, WirtschaftlichkeitStand>)gaben["ReferenzGewaehlt"];

            IDialogDienst vorher = Dienste.Dialog;
            var mitschrift = new Mitschrift();
            string status;
            try
            {
                Dienste.Dialog = mitschrift;
                status = referenz(PROJEKT).Statuszeile;
            }
            finally { Dienste.Dialog = vorher; }

            Assert.Single(mitschrift.Zeilen, z => z.Contains("E13-Probe", StringComparison.Ordinal));
            Assert.DoesNotContain(mitschrift.Zeilen, z => z.Contains("UNIQUE", StringComparison.Ordinal));
            Assert.DoesNotContain("E13-Probe", status);
            Assert.DoesNotContain("Speichern gescheitert: ", status);
        }

        /// <summary>Ein Dialogdienst, der nichts zeigt, sondern mitschreibt.</summary>
        private sealed class Mitschrift : IDialogDienst
        {
            public readonly List<string> Zeilen = new List<string>();
            public void Meldung(string text, string titel = null) { Zeilen.Add("Meldung|" + text); }
            public void Warnung(string text, string titel = null) { Zeilen.Add("Warnung|" + text); }
            public void Fehler(string text, string titel = null) { Zeilen.Add("Fehler|" + text); }
            public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false)
            { Zeilen.Add("Frage|" + text); return true; }
            public JaNeinAbbruch Wahl(string text, string titel = null)
            { Zeilen.Add("Wahl|" + text); return JaNeinAbbruch.Ja; }
            public void Warten(bool an) { }
        }

        /// <summary>
        /// Der BHKW-Dialog bekommt die Gründe des Kerns: den Speicherfehler des
        /// Schreibwegs (einmal gelesen) und den Ladefehler nach dem Laden des gebuchten
        /// Stands.
        /// </summary>
        [Fact]
        public void Der_BHKW_Dialog_bekommt_Speicher_und_Ladefehler_des_Kerns()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            Rechne(ctrl.LadeParameter(PROJEKT));
            IReadOnlyDictionary<string, object> gaben = BhkwWirtschaftlichkeitHuelle.Gaben(PROJEKT, null, out _);
            Assert.Equal("", gaben["Vorsorgewarnung"]);

            var laden = (Func<IReadOnlyList<int>, IReadOnlyList<WirtschaftlichkeitErgebnis>>)gaben["ErgebnisseLaden"];
            var ladefehler = (Func<string>)gaben["Ladefehler"];
            laden(new[] { PROJEKT });
            Assert.Null(ladefehler());

            DataRepository.ExecuteNonQuery(
                "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS + " SET Zeitstempel = 'kaputt' " +
                "WHERE ID_Projekt = " + PROJEKT);
            laden(new[] { PROJEKT });
            Assert.StartsWith("FormatException: ", ladefehler());

            DataRepository.ExecuteNonQuery(
                "CREATE TRIGGER e13_stop BEFORE UPDATE ON " + WirtschaftlichkeitCtrl.TAB_PARAMETER +
                " BEGIN SELECT RAISE(ABORT, 'E13-Probe'); END");
            var speichern = (Func<WirtschaftlichkeitParameter, bool>)gaben["SpeichereVorgaben"];
            var speicherfehler = (Func<string>)gaben["Speicherfehler"];

            // Ein Datenbankfehler: Die Zugriffsschicht meldet ihn einmal, der Dialog
            // bekommt keinen zweiten Grund (sonst stünde er zweimal da).
            IDialogDienst vorher = Dienste.Dialog;
            var mitschrift = new Mitschrift();
            bool gespeichert;
            try
            {
                Dienste.Dialog = mitschrift;
                gespeichert = speichern((WirtschaftlichkeitParameter)gaben["Parameter"]);
            }
            finally { Dienste.Dialog = vorher; }
            Assert.False(gespeichert);
            Assert.Single(mitschrift.Zeilen, z => z.Contains("E13-Probe", StringComparison.Ordinal));
            Assert.Null(speicherfehler());

            // Der Träger des Grundes für Fehler außerhalb der Zugriffsschicht: einmal gelesen,
            // leere Gründe überschreiben keinen.
            var grund = new BhkwWirtschaftlichkeitHuelle.Speichergrund();
            grund.Setzen("InvalidOperationException: E13-Probe");
            grund.Setzen("  ");
            Assert.Equal("InvalidOperationException: E13-Probe", grund.Lesen());
            Assert.Null(grund.Lesen());
        }

        private static WirtschaftlichkeitStand Stand(WirtschaftlichkeitSeiteGaben seite)
            => ((Func<WirtschaftlichkeitStand>)seite.Gaben()["Laden"])();
    }
}
