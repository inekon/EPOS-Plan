using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE B7P — der <b>Nachweisumschlag</b> des Wirtschaftlichkeitsergebnisses
    /// (Anwenderentscheid B7-E-1 vom 18.09.2026, offene Punkte B7-2 und BK1-3).
    ///
    /// <para><b>Die Lage vorher.</b> Modulnachweis, Energiekosten je Anlage,
    /// Betriebskostenpositionen und Kohärenzzeilen entstanden im Lauf und starben mit
    /// ihm. Der gebuchte Stand trug die Summen, aber keine Herleitung: Der Reiter ließ
    /// die Unterzeilen weg, Word und Excel ihre Modultabelle, die Gruppe 5 des
    /// BHKW-Dialogs sagte „ohne Lauf".</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Vier Dinge: Ein gespeicherter Lauf trägt
    /// die Zeilen wieder; ein KAPUTTER Umschlag kostet nur die Zeilen und setzt genau
    /// EINEN Hinweis, während alle Ergebniszeilen stehen bleiben; ein Lauf OHNE
    /// Umschlag (vor B7P gespeichert, nie migrierte Datei) lädt unverändert; und ein zu
    /// großer Umschlag wirft nicht, sondern nennt seinen Grund.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisNachweisPersistenzTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>„Referenz BHKW-Kaskade" — zwei Gasmodule; der Lauf führt Modulzeilen,
        /// Energiekosten je Anlage und Betriebskostenpositionen.</summary>
        private const int PROJEKT = 1030;

        // =================================================================
        //  1 — Der Rundweg über die Datenbank
        // =================================================================

        /// <summary>
        /// DIE KERNAUSSAGE. Was der frische Lauf an Zeilen führt, trägt der geladene
        /// Stand wieder — Modulzeilen, Energiekosten je Anlage, Betriebskostenpositionen
        /// und Kohärenzzeilen, mit denselben Schlüsselgrößen.
        /// </summary>
        [Fact]
        public void Ein_gespeicherter_Lauf_traegt_seine_Nachweiszeilen_wieder()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis frisch = Rechne();
            WirtschaftlichkeitErgebnis geladen = Lade();

            // Ohne irgendeine Zeile misst der Prüffall nichts.
            Assert.True(frisch.KwkgModule.Count + frisch.EnergiekostenJeAnlage.Count +
                        frisch.Betriebskosten.Count + frisch.KohaerenzHinweise.Count > 0,
                        "Der frische Lauf von " + PROJEKT + " führt keine einzige " +
                        "Nachweiszeile — der Prüffall misst nichts.");

            Assert.Equal(frisch.KwkgModule.Count, geladen.KwkgModule.Count);
            Assert.Equal(frisch.EnergiekostenJeAnlage.Count, geladen.EnergiekostenJeAnlage.Count);
            Assert.Equal(frisch.Betriebskosten.Count, geladen.Betriebskosten.Count);
            Assert.Equal(frisch.KohaerenzHinweise.Count, geladen.KohaerenzHinweise.Count);

            for (int i = 0; i < frisch.KwkgModule.Count; i++)
            {
                Assert.Equal(frisch.KwkgModule[i].Bezeichner, geladen.KwkgModule[i].Bezeichner);
                Assert.Equal(frisch.KwkgModule[i].PelKW, geladen.KwkgModule[i].PelKW, 6);
                Assert.Equal(frisch.KwkgModule[i].Jahr1Eur, geladen.KwkgModule[i].Jahr1Eur, 6);
                Assert.Equal(frisch.KwkgModule[i].EigenMWh, geladen.KwkgModule[i].EigenMWh, 6);
                Assert.Equal(frisch.KwkgModule[i].HerleitungEigen, geladen.KwkgModule[i].HerleitungEigen);
            }
            for (int i = 0; i < frisch.EnergiekostenJeAnlage.Count; i++)
            {
                Assert.Equal(frisch.EnergiekostenJeAnlage[i].Anlage, geladen.EnergiekostenJeAnlage[i].Anlage);
                Assert.Equal(frisch.EnergiekostenJeAnlage[i].KostenEur,
                             geladen.EnergiekostenJeAnlage[i].KostenEur, 6);
            }
            for (int i = 0; i < frisch.Betriebskosten.Count; i++)
            {
                Assert.Equal(frisch.Betriebskosten[i].Id, geladen.Betriebskosten[i].Id);
                Assert.Equal(frisch.Betriebskosten[i].Bezeichnung, geladen.Betriebskosten[i].Bezeichnung);
                Assert.Equal(frisch.Betriebskosten[i].BetragJahr, geladen.Betriebskosten[i].BetragJahr, 6);
            }

            // Die Skalare des Umschlags reisen mit.
            Assert.Equal(frisch.VermiedenMengeMWh, geladen.VermiedenMengeMWh, 6);
            Assert.Equal(frisch.VermiedenEntlastung9bJahr, geladen.VermiedenEntlastung9bJahr, 6);
            Assert.Equal(frisch.ProduzierendesGewerbe, geladen.ProduzierendesGewerbe);
            Assert.Equal(frisch.BezugsspitzeKW, geladen.BezugsspitzeKW);
            Assert.Equal(frisch.KwkgPauschaleEur, geladen.KwkgPauschaleEur, 6);   // U17
        }

        /// <summary>Die Rubrik zeigt beim GELADENEN Stand dieselben Unterzeilen wie beim
        /// frischen Lauf — das ist der Zweck der ganzen Übung.</summary>
        [Fact]
        public void Die_Unterzeilen_der_Rubrik_stehen_auch_beim_gebuchten_Stand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis frisch = Rechne();
            if (frisch.KwkgModule.Count == 0) return;   // ohne Modulzeilen nichts zu messen

            WirtschaftlichkeitErgebnis geladen = Lade();
            var menge = new List<WirtschaftlichkeitErgebnis> { geladen };
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null), menge);

            Assert.Contains(zeilen, z => z.Schluessel == "ERL_A1_EINSPEISUNG");
            Assert.Contains(zeilen, z => z.Schluessel == "ERL_A2_EIGEN");
        }

        /// <summary>
        /// DER BERICHTSRÜCKFALL von Word und Excel. Beide bauen ihre Zahlen aus
        /// <c>daten.Wirtschaftlichkeit</c>, und wenn die Rechnung dieses Berichtslaufs
        /// scheiterte, aus <c>provider.LadeErgebnisse(ids)</c>
        /// (<c>BausteineWirtschaftlichkeit.SchreibeWord</c> und
        /// <c>ExcelBerichtGenerator.BlattWirtschaftlichkeit</c>). Ihre Modultafel hängt
        /// an EINER Bedingung, die beide wortgleich führen: ein Ergebnis des Szenarios
        /// ERWARTET mit mindestens einer Modulzeile. Bis B7P war sie auf dem
        /// Rückfallweg nie erfüllt — der gespeicherte Stand trug keine Modulzeilen.
        ///
        /// <para>Gemessen wird die BEDINGUNG, nicht das gerenderte Dokument: Ein
        /// vollständiger Word- oder Excel-Lauf braucht Grafiken, Vorlagen und alle
        /// Blätter und sagte über diese eine Naht nichts Zusätzliches.</para>
        /// </summary>
        [Fact]
        public void Der_Berichtsrueckfall_traegt_die_Modultabelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis frisch = Rechne();
            if (frisch.KwkgModule.Count == 0) return;   // ohne Modulzeilen nichts zu messen

            // Genau der Ausdruck, mit dem Word und Excel zurückfallen — ohne
            // daten.Wirtschaftlichkeit gibt es nur diesen einen Weg.
            List<WirtschaftlichkeitErgebnis> rueckfall =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { PROJEKT });

            List<WirtschaftlichkeitErgebnis> mitModulen = rueckfall.Where(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                     x.KwkgModule != null && x.KwkgModule.Count > 0).ToList();

            Assert.NotEmpty(mitModulen);
            Assert.Equal(frisch.KwkgModule.Count, mitModulen[0].KwkgModule.Count);
            Assert.All(mitModulen[0].KwkgModule,
                       m => Assert.False(string.IsNullOrEmpty(m.Bezeichner)));
        }

        // =================================================================
        //  2 — Ein kaputter Umschlag kostet nur die Nachweise
        // =================================================================

        /// <summary>
        /// DIE WICHTIGSTE ABSICHERUNG. Der Leseweg der Ergebnisse hat EINEN Fang um die
        /// ganze Projektschleife — eine Ausnahme aus dem Deserialisierer verlöre ALLE
        /// Ergebniszeilen ALLER Projekte. Deshalb steht der Umschlag in einem eigenen,
        /// engen <c>try</c>: Kaputt heißt „keine Nachweise und genau ein Hinweis", nicht
        /// „kein Ergebnis".
        /// </summary>
        [Fact]
        public void Ein_kaputter_Umschlag_kostet_nur_die_Nachweise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Rechne();
            int zeilenVorher = Zeilenzahl();
            Assert.True(zeilenVorher > 0, "Ohne gespeicherte Ergebniszeilen misst der Prüffall nichts.");

            DataRepository.ExecuteSQL(
                "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS +
                " SET [" + WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON + "] = ? WHERE ID_Projekt = ?",
                new DbParam("@j", "nw1:{\"Version\":1,\"KwkgModule\":[ kaputt"),
                new DbParam("@p", PROJEKT));

            List<WirtschaftlichkeitErgebnis> alle =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { PROJEKT });

            // ALLE Ergebniszeilen stehen — das ist der Punkt.
            Assert.Equal(zeilenVorher, alle.Count);

            foreach (WirtschaftlichkeitErgebnis e in alle)
            {
                Assert.Empty(e.KwkgModule);
                Assert.Empty(e.EnergiekostenJeAnlage);
                Assert.Empty(e.Betriebskosten);
                Assert.Single(e.KohaerenzHinweise);
                Assert.Equal(KohaerenzSchwere.HINWEIS, e.KohaerenzHinweise[0].Schwere);
                Assert.Contains("nicht lesbar", e.KohaerenzHinweise[0].Text);
            }
        }

        /// <summary>Ein Umschlag mit FREMDER Fassung wird genauso behandelt wie ein
        /// kaputter: nicht halb verstehen, sondern benannt liegen lassen.</summary>
        [Fact]
        public void Eine_fremde_Fassung_wird_nicht_halb_gelesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Rechne();
            DataRepository.ExecuteSQL(
                "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS +
                " SET [" + WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON + "] = ? WHERE ID_Projekt = ?",
                new DbParam("@j", "nw1:{\"Version\":99,\"KwkgModule\":[]}"),
                new DbParam("@p", PROJEKT));

            List<WirtschaftlichkeitErgebnis> alle =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { PROJEKT });

            Assert.NotEmpty(alle);
            Assert.All(alle, e => Assert.Single(e.KohaerenzHinweise));
        }

        // =================================================================
        //  3 — Ein Lauf ohne Umschlag lädt unverändert
        // =================================================================

        /// <summary>
        /// Ein vor B7P gespeicherter Lauf führt die Spalte nicht oder leer. Er lädt
        /// unverändert: Ergebniszeilen vollständig, keine Nachweise, <b>kein</b> Hinweis
        /// — das ist der alte Stand und keine Störung.
        ///
        /// <para>Die Datei OHNE die Spalte durchläuft denselben Weg: <c>LadeErgebnisse</c>
        /// ruft zuerst <c>StelleTabellenSicher</c>, die Spalte entsteht leer, und die
        /// Zeile liest NULL. Der Fall wird deshalb hier mitgeprüft, indem die Spalte
        /// tatsächlich entfernt wird.</para>
        /// </summary>
        [Fact]
        public void Ein_Lauf_ohne_Umschlag_laedt_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Rechne();
            int zeilenVorher = Zeilenzahl();

            // Die Spalte wirklich entfernen — so sieht eine nie migrierte Datei aus.
            try
            {
                DataRepository.ExecuteSQL(
                    "ALTER TABLE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS +
                    " DROP COLUMN [" + WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON + "]");
            }
            catch
            {
                DataRepository.ExecuteSQL(
                    "UPDATE " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS +
                    " SET [" + WirtschaftlichkeitCtrl.SPALTE_NACHWEIS_JSON + "] = NULL");
            }

            List<WirtschaftlichkeitErgebnis> alle =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { PROJEKT });

            Assert.Equal(zeilenVorher, alle.Count);
            Assert.All(alle, e =>
            {
                Assert.Empty(e.KwkgModule);
                Assert.Empty(e.KohaerenzHinweise);          // kein Hinweis, kein Lärm
            });
        }

        // =================================================================
        //  4 — Der Längenwächter wirft nicht
        // =================================================================

        /// <summary>
        /// Ein Umschlag über der Grenze liefert <c>null</c> und einen Grund — er wirft
        /// nicht. Der Schreibweg läuft in der Transaktion, die auch die Ergebniszeilen
        /// schreibt; ein Wurf hier verlöre den ganzen Lauf.
        /// </summary>
        [Fact]
        public void Ein_zu_grosser_Umschlag_liefert_null_und_einen_Grund()
        {
            var e = new WirtschaftlichkeitErgebnis { IdProjekt = PROJEKT };
            string fuellung = new string('x', 400);
            for (int i = 0; i < 12000; i++)
                e.Betriebskosten.Add(new KostenPositionNachweis
                {
                    Id = i,
                    Bezeichnung = fuellung,
                    Gruppe = fuellung,
                    BetragJahr = i
                });

            string grund;
            string json = ErgebnisNachweisUmschlag.Schreiben(e, out grund);

            Assert.Null(json);
            Assert.False(string.IsNullOrEmpty(grund));
            Assert.Contains("frisch", grund);
        }

        /// <summary>Der Rundweg des Umschlags ohne Datenbank: Was hineingeht, kommt
        /// heraus; ein fremdes Präfix, kaputtes JSON und eine fremde Fassung liefern
        /// <c>null</c> statt eines Wurfs.</summary>
        [Fact]
        public void Der_Umschlag_haelt_Praefix_Fassung_und_Schrott_auseinander()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                BezugsspitzeKW = 412.5,
                ProduzierendesGewerbe = true,
                VermiedenMengeMWh = 20,
                VermiedenEntlastung9bJahr = 400,
                KwkgPauschaleEur = 4320            // U17
            };
            e.KwkgModule.Add(new KwkgModulNachweis { Bezeichner = "Modul 1", PelKW = 50, Jahr1Eur = 1234 });

            string grund;
            string json = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);
            Assert.StartsWith(ErgebnisNachweisUmschlag.PRAEFIX, json);

            ErgebnisNachweisUmschlag zurueck = ErgebnisNachweisUmschlag.Lesen(json);
            Assert.NotNull(zurueck);
            Assert.Equal(ErgebnisNachweisUmschlag.FASSUNG, zurueck.Version);
            Assert.Single(zurueck.KwkgModule);
            Assert.Equal("Modul 1", zurueck.KwkgModule[0].Bezeichner);
            Assert.Equal(50.0, zurueck.KwkgModule[0].PelKW, 6);
            Assert.Equal(412.5, zurueck.BezugsspitzeKW.Value, 6);
            Assert.Equal(20.0, zurueck.VermiedenMengeMWh, 6);
            Assert.Equal(400.0, zurueck.VermiedenEntlastung9bJahr, 6);
            Assert.True(zurueck.ProduzierendesGewerbe);
            Assert.Equal(4320.0, zurueck.KwkgPauschaleEur, 6);

            Assert.Null(ErgebnisNachweisUmschlag.Lesen(null));
            Assert.Null(ErgebnisNachweisUmschlag.Lesen(""));
            Assert.Null(ErgebnisNachweisUmschlag.Lesen("gz1:irgendwas"));
            Assert.Null(ErgebnisNachweisUmschlag.Lesen("nw1:{kaputt"));
            Assert.Null(ErgebnisNachweisUmschlag.Lesen(
                "nw1:{\"Version\":" + (ErgebnisNachweisUmschlag.FASSUNG + 1) + "}"));
        }

        // =================================================================
        //  AUFTRAG U7 — die Umschlagfassung 4 trägt beide Steuerbeträge
        // =================================================================

        /// <summary>
        /// Der Umschlag trägt beide Paragrafenbeträge samt Mengen und Sätzen — die
        /// Ergebnisspalte führt weiterhin nur die Summe, und ohne den Umschlag
        /// könnte die Rubrik eines gebuchten Stands die zwei Zeilen nicht zeichnen.
        /// </summary>
        [Fact]
        public void Die_Fassung_4_traegt_beide_Steuerbetraege_samt_Mengen_und_Saetzen()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                EnergiesteuerJahr1 = 24088.43,
                EnergiesteuerAufgeteilt = true,
                Energiesteuer53Jahr1 = 21202.71,
                Energiesteuer54Jahr1 = 2885.72,
                Energiesteuer54SockelJahr1 = 250.0
            };
            e.EnergiesteuerNachweise.Add(new EnergiesteuerNachweis
            {
                Anlage = "BHKW",
                Paragraf = EnergiesteuerNachweis.PARAGRAF_53A,
                Menge = 4796.99,
                Einheit = DbWerte.GESETZ_EINHEIT_EUR_MWH,
                SatzEur = 4.42,
                BetragEur = 21202.71
            });

            string grund;
            string json = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);

            ErgebnisNachweisUmschlag u = ErgebnisNachweisUmschlag.Lesen(json);
            Assert.NotNull(u);
            Assert.Equal(4, ErgebnisNachweisUmschlag.FASSUNG);

            var zurueck = new WirtschaftlichkeitErgebnis();
            u.Uebernimm(zurueck);

            Assert.True(zurueck.EnergiesteuerAufgeteilt);
            Assert.Equal(21202.71, zurueck.Energiesteuer53Jahr1, 2);
            Assert.Equal(2885.72, zurueck.Energiesteuer54Jahr1, 2);
            Assert.Equal(250.0, zurueck.Energiesteuer54SockelJahr1, 6);
            Assert.Single(zurueck.EnergiesteuerNachweise);
            Assert.Equal(4796.99, zurueck.EnergiesteuerNachweise[0].Menge, 2);
            Assert.Equal(4.42, zurueck.EnergiesteuerNachweise[0].SatzEur, 6);
            Assert.Equal(DbWerte.GESETZ_EINHEIT_EUR_MWH, zurueck.EnergiesteuerNachweise[0].Einheit);
        }

        /// <summary>
        /// Ein Umschlag der Fassung 3 wird weiter gelesen — seine Felder sind eine
        /// echte Teilmenge. Was er NICHT kann, ist die Aufteilung: Sie gab es damals
        /// nicht, und <c>EnergiesteuerAufgeteilt</c> sagt das, statt zwei Nullen für
        /// eine Aussage auszugeben.
        /// </summary>
        [Fact]
        public void Eine_aeltere_Fassung_kennt_die_Aufteilung_nicht_und_sagt_es()
        {
            ErgebnisNachweisUmschlag alt = ErgebnisNachweisUmschlag.Lesen(
                "nw1:{\"Version\":3,\"KwkgPauschaleEur\":4320}");

            Assert.NotNull(alt);
            Assert.Equal(3, alt.Version);

            var e = new WirtschaftlichkeitErgebnis { EnergiesteuerJahr1 = 5119 };
            alt.Uebernimm(e);

            Assert.False(e.EnergiesteuerAufgeteilt);
            Assert.Equal(0.0, e.Energiesteuer53Jahr1, 6);
            Assert.Equal(0.0, e.Energiesteuer54Jahr1, 6);
            Assert.Empty(e.EnergiesteuerNachweise);
            Assert.Equal(4320.0, e.KwkgPauschaleEur, 6);     // Fassung 2 bleibt lesbar
        }

        // =================================================================
        //  Prüfstand
        // =================================================================

        private static int Zeilenzahl()
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + WirtschaftlichkeitCtrl.TAB_ERGEBNIS + " WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static WirtschaftlichkeitErgebnis Lade()
        {
            List<WirtschaftlichkeitErgebnis> alle =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { PROJEKT });
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>Rechnet <see cref="PROJEKT"/> als Stammprojekt — der Lauf schreibt
        /// seine Ergebnisse selbst in die Arbeitskopie.</summary>
        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);

            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall B7P",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);

            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);

            List<WirtschaftlichkeitErgebnis> alle = new WirtschaftlichkeitCtrl().Berechne(daten, p);
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }
    }
}
