using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑12 Teil b (09.09.2026): „Preisindizierung der
    /// Ersatzbeschaffung (p_I) im PARAMETERSATZ."
    ///
    /// <para><b>Teil a</b> hat den Rechenweg gelegt — <c>KapitalwertRechner.Rechne</c>
    /// nimmt seit dem einen Satz p_I entgegen und indiziert damit die Ersatzbeschaffungen
    /// (<see cref="KapitalwertRechnerPreisindexTests"/>) — und die vier Spalten des
    /// Migrationsschritts 72 angelegt (<see cref="Migration72Tests"/>). Bis dahin stand
    /// p_I auf 0, und keine Zahl änderte sich.</para>
    ///
    /// <para><b>Diese Fälle halten Teil b fest:</b> die Nullsemantik „NULL heißt wie p_B,
    /// nicht 0 %", die Vorgabe je Szenario (Erwartet-p_I ∓ 1 %-Punkt — dieselbe Regel wie
    /// bei p_E und p_B, angewandt auf den Erwartungswert der eigenen Größe), den Vorrang
    /// eines gepflegten Szenariowerts, den Lade-/Speicherweg der vier Spalten und die
    /// Ende-zu-Ende-Wirkung über <see cref="WirtschaftlichkeitCtrl.Berechne"/>.</para>
    ///
    /// <para><b>Die Regressionszusage</b> steht am Schluss: Mit p_B = 0 und ungepflegtem
    /// p_I rechnet der Erwartungsfall <b>bitgleich</b> zum Stand vor dieser Etappe.</para>
    ///
    /// <para><b>Der Träger der Datenbankfälle</b> ist wie in
    /// <see cref="SzenarioParameterTests"/> das Projekt 1040.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PreisInvestitionTests
    {
        private const int PROJEKT = 1040;

        private const int ID_HAUPT = 101600487;   // BETRAG, IsMainComponent
        private static readonly int[] ID_UEBRIGE =
            { 101600491, 101600492, 101600493, 101600494, 101600495, 101600496, 101600497 };

        /// <summary>Der Betrag der einen Investitionszeile [€].</summary>
        private const double HAUPT = 10000.0;

        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        // =====================================================================
        // 1 Nullsemantik am Parametersatz
        // =====================================================================

        /// <summary>
        /// FALL 1 und 2: Ein ungepflegtes p_I rechnet „wie p_B" — nicht mit 0 %. Eine 0 als
        /// Vorbelegung hieße „Investitionsgüter werden nie teurer", und diese Aussage hat
        /// niemand getroffen. Ein gepflegter Satz gilt dagegen genau so, wie er dasteht,
        /// und zieht bei einer geänderten Betriebsangabe NICHT mehr mit.
        /// </summary>
        [Fact]
        public void Ohne_eigenen_Satz_gilt_die_Preissteigerung_Betrieb()
        {
            var p = new WirtschaftlichkeitParameter { PreissteigerungBetrieb = 1.5 };

            Assert.Null(p.PreissteigerungInvestition);
            Assert.Equal(1.5, p.PreisInvestWirksam, 9);

            p.PreissteigerungBetrieb = 2.75;              // Projektangabe geaendert
            Assert.Equal(2.75, p.PreisInvestWirksam, 9);  // das leere Feld zieht mit

            p.PreissteigerungInvestition = 3.0;           // jetzt gepflegt
            Assert.Equal(3.0, p.PreisInvestWirksam, 9);
            p.PreissteigerungBetrieb = 9.0;
            Assert.Equal(3.0, p.PreisInvestWirksam, 9);   // und bleibt stehen
        }

        // =====================================================================
        // 2 Die Vorgabe je Szenario
        // =====================================================================

        /// <summary>
        /// FALL 3: Die Szenariovorgabe spannt sich um das WIRKSAME Erwartet-p_I — ∓ 1
        /// Prozentpunkt, dieselbe Regel wie bei p_E und p_B.
        ///
        /// <para>Ist p_I nicht gepflegt (Regelfall), ist der Bezugswert p_B, und die
        /// Vorgabe trifft damit genau das wirksame p_B desselben Szenarios. Ist p_I
        /// gepflegt, spannt sich die Bandbreite um DIESEN Wert und nicht mehr um p_B —
        /// sonst zöge eine Betriebskostenannahme still die Ersatzbeschaffung mit.</para>
        /// </summary>
        [Fact]
        public void Die_Szenariovorgabe_spannt_sich_um_das_wirksame_Erwartet_p_I()
        {
            var p = new WirtschaftlichkeitParameter { PreissteigerungBetrieb = 2.0 };
            SzenarioSatz best = p.SatzFuer(WirtschaftlichkeitSzenario.BEST);
            SzenarioSatz worst = p.SatzFuer(WirtschaftlichkeitSzenario.WORST);

            // ohne gepflegtes Erwartet-p_I: Bezug ist p_B = 2,0
            Assert.Equal(1.0, best.PreisInvestWirksam(p.PreisInvestWirksam), 9);
            Assert.Equal(3.0, worst.PreisInvestWirksam(p.PreisInvestWirksam), 9);

            // und das ist genau das wirksame p_B des Szenarios (Regelfall).
            Assert.Equal(best.PreisBetriebWirksam(p.PreissteigerungBetrieb),
                         best.PreisInvestWirksam(p.PreisInvestWirksam), 9);
            Assert.Equal(worst.PreisBetriebWirksam(p.PreissteigerungBetrieb),
                         worst.PreisInvestWirksam(p.PreisInvestWirksam), 9);

            // mit gepflegtem Erwartet-p_I: Bezug ist 5,0 - nicht mehr p_B.
            p.PreissteigerungInvestition = 5.0;
            Assert.Equal(4.0, best.PreisInvestWirksam(p.PreisInvestWirksam), 9);
            Assert.Equal(6.0, worst.PreisInvestWirksam(p.PreisInvestWirksam), 9);
        }

        /// <summary>
        /// FALL 4: Ein gepflegter Szenariowert hat Vorrang vor jeder Vorgabe — und er
        /// bleibt auch dann stehen, wenn sich die Projektangabe darunter ändert.
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Szenariowert_schlaegt_die_Vorgabe()
        {
            var p = new WirtschaftlichkeitParameter { PreissteigerungBetrieb = 2.0 };
            SzenarioSatz worst = p.SatzFuer(WirtschaftlichkeitSzenario.WORST);

            worst.PreissteigerungInvestition = 7.5;
            Assert.Equal(7.5, worst.PreisInvestWirksam(p.PreisInvestWirksam), 9);

            p.PreissteigerungBetrieb = 4.0;
            Assert.Equal(7.5, worst.PreisInvestWirksam(p.PreisInvestWirksam), 9);
        }

        /// <summary>
        /// FALL 5: <see cref="SzenarioSatz.NurVorgaben"/> kennt die siebte Größe. Ohne
        /// diesen Nachweis meldete der Dialog „Vorgaben", obwohl jemand p_I gepflegt hat.
        /// </summary>
        [Fact]
        public void NurVorgaben_beruecksichtigt_das_neue_Feld()
        {
            SzenarioSatz s = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
            Assert.True(s.NurVorgaben);

            s.PreissteigerungInvestition = 1.0;
            Assert.False(s.NurVorgaben);

            s.PreissteigerungInvestition = null;
            Assert.True(s.NurVorgaben);

            // Die flache Kopie traegt das Feld mit - der Dialog arbeitet auf einer.
            s.PreissteigerungInvestition = 2.25;
            Assert.Equal(2.25, s.Kopie().PreissteigerungInvestition);
        }

        /// <summary>
        /// FALL 6: p_I steht in beiden Nachweiszeilen — der des Szenarios (wirksamer Wert)
        /// und der des Projekts (wirksamer Wert samt Herkunft). Eine Bandbreite, deren
        /// Annahmen nicht vollständig dastehen, ist keine offengelegte Annahme.
        /// </summary>
        [Fact]
        public void Die_Nachweiszeilen_nennen_p_I()
        {
            var p = new WirtschaftlichkeitParameter
            {
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 2.0,
                PreissteigerungBetrieb = 1.0
            };

            string satzzeile = p.SatzFuer(WirtschaftlichkeitSzenario.WORST).Nachweis(p, DE);
            Assert.Contains("p_B = 2,0 %/a", satzzeile);
            Assert.Contains("p_I = 2,0 %/a", satzzeile);

            Assert.Contains("Investition/Ersatz 1,0 %/a (wie Betrieb)", p.Nachweis(DE));

            p.PreissteigerungInvestition = 4.0;
            Assert.Contains("Investition/Ersatz 4,0 %/a (gepflegt)", p.Nachweis(DE));
            Assert.Contains("p_I = 5,0 %/a",
                            p.SatzFuer(WirtschaftlichkeitSzenario.WORST).Nachweis(p, DE));
        }

        // =====================================================================
        // 3 Ablage (Migrationsschritt 72)
        // =====================================================================

        /// <summary>
        /// FALL 7: Die vier Spalten überleben Speichern und Laden — die Zahl bleibt die
        /// Zahl, der Fließtext bleibt der Text, und <c>null</c> bleibt <c>null</c>. Das
        /// Letzte ist die eigentliche Zusage: Eine geschriebene 0 wäre bei p_I eine
        /// andere Aussage als ein leeres Feld.
        /// </summary>
        [Fact]
        public void Die_vier_Spalten_ueberleben_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            Assert.Null(p.PreissteigerungInvestition);      // frisch migriert: NULL
            Assert.True(string.IsNullOrEmpty(p.NichtMonetaer));

            p.PreissteigerungInvestition = 2.5;
            p.NichtMonetaer = "Versorgungssicherheit, Arbeitsschutz, Komfort";
            p.SatzWorst.PreissteigerungInvestition = 4.25;   // Best bleibt leer
            Assert.True(ctrl.SpeichereParameter(p));

            WirtschaftlichkeitParameter zurueck =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Equal(2.5, zurueck.PreissteigerungInvestition);
            Assert.Equal("Versorgungssicherheit, Arbeitsschutz, Komfort", zurueck.NichtMonetaer);
            Assert.Equal(4.25, zurueck.SatzWorst.PreissteigerungInvestition);
            Assert.Null(zurueck.SatzBest.PreissteigerungInvestition);   // leer bleibt leer

            // Und der Weg zurueck ins Leere: NULL heisst wieder "wie p_B".
            zurueck.PreissteigerungInvestition = null;
            zurueck.NichtMonetaer = "";
            zurueck.SatzWorst.PreissteigerungInvestition = null;
            Assert.True(new WirtschaftlichkeitCtrl().SpeichereParameter(zurueck));

            WirtschaftlichkeitParameter leer =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Null(leer.PreissteigerungInvestition);
            Assert.True(string.IsNullOrEmpty(leer.NichtMonetaer));
            Assert.Null(leer.SatzWorst.PreissteigerungInvestition);
        }

        /// <summary>
        /// FALL 8: <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> legt die vier Spalten
        /// des Schritts 72 selbst an. Das ist die TOLERANTE VORSORGE unmittelbar vor dem
        /// Zugriff: Eine nie migrierte Datenbank soll sich wie eine frisch migrierte
        /// verhalten und nicht an einer fehlenden Spalte scheitern.
        ///
        /// <para>Geprüft wird das, indem die Spalten hier ABSICHTLICH entfernt werden.
        /// Kann die Datenbank das nicht (ältere SQLite ohne <c>DROP COLUMN</c>), endet der
        /// Fall still — nachweisen lässt sich die Vorsorge dann nicht.</para>
        /// </summary>
        [Fact]
        public void StelleTabellenSicher_legt_die_Spalten_des_Schritts_72_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string tab = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT;
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                DataRepository.ExecuteNonQuery(
                    "ALTER TABLE \"" + tab + "\" DROP COLUMN \"" + s.Name + "\"");

            bool entfernt = true;
            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                if (DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) entfernt = false;
            if (!entfernt) return;   // DROP COLUMN nicht moeglich - nichts zu zeigen

            new WirtschaftlichkeitCtrl().StelleTabellenSicher();

            foreach (SchemaSpalte s in SchemaKatalog.Schritt72_ValeriErgaenzung)
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name),
                            "StelleTabellenSicher hat die Spalte nicht angelegt: " +
                            s.Tabelle + "." + s.Name);
        }

        // =====================================================================
        // 4 Ende zu Ende über Berechne
        // =====================================================================

        /// <summary>
        /// FALL 9: Der Satz erreicht den Rechenkern. Mit einer Position, deren
        /// Nutzungsdauer unter dem Betrachtungszeitraum liegt (n = 8 a, T = 20 a), wird
        /// zweimal ersetzt — und ein p_I &gt; 0 macht diese zwei Ersatzbeschaffungen
        /// teurer. Der Erwartet-Kapitalwert SINKT deshalb gegenüber einem ausdrücklich
        /// gepflegten p_I = 0.
        ///
        /// <para><b>Das ist die gewollte Wirkung des Entscheids</b> — der bisherige
        /// Ausweis war der zu günstige (Vereinfachung W1, VALERI-Lücke G4).</para>
        /// </summary>
        [Fact]
        public void Mit_Ersatzbeschaffung_senkt_p_I_den_Erwartet_Kapitalwert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen(8.0);

            double mitIndex = Kapitalwert(Parametersatz(2.0, null));   // p_I = p_B = 2 %
            double ohneIndex = Kapitalwert(Parametersatz(2.0, 0.0));   // p_I ausdruecklich 0

            Assert.True(mitIndex < ohneIndex,
                        "Kapitalwert mit p_I = 2 % (" + mitIndex.ToString("N2", DE) +
                        ") liegt nicht unter dem mit p_I = 0 % (" +
                        ohneIndex.ToString("N2", DE) + ").");
        }

        /// <summary>
        /// FALL 9b: OHNE Ersatzbeschaffung ändert p_I nichts. Steht keine Nutzungsdauer
        /// (n &lt; 1 heißt im Rechenkern „wie T"), gibt es weder Ersatz noch einen
        /// Restwert — und damit nichts, was ein Preisindex anfassen könnte. Die zwei
        /// Kapitalwerte sind BITgleich, nicht nur gerundet gleich.
        /// </summary>
        [Fact]
        public void Ohne_Ersatzbeschaffung_aendert_p_I_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen(0.0);

            Assert.Equal(Kapitalwert(Parametersatz(2.0, 0.0)),
                         Kapitalwert(Parametersatz(2.0, null)));
        }

        /// <summary>
        /// FALL 10 — DIE REGRESSIONSZUSAGE: Mit p_B = 0 und ungepflegtem p_I ist der
        /// wirksame Satz 0, und der Rechenkern bildet den Indexfaktor gar nicht erst
        /// (Teil a). Der Erwartungsfall rechnet damit BITgleich zum Stand vor dieser
        /// Etappe — nachgewiesen an einem Projekt, das ausdrücklich zweimal ersetzt.
        /// </summary>
        [Fact]
        public void Ohne_Preissteigerung_Betrieb_bleibt_der_Erwartungsfall_bitgleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen(8.0);

            Assert.Equal(Kapitalwert(Parametersatz(0.0, 0.0)),
                         Kapitalwert(Parametersatz(0.0, null)));
        }

        // =====================================================================
        // Hilfsmittel
        // =====================================================================

        /// <summary>Der Parametersatz der Ende-zu-Ende-Fälle: T = 20 a, i = 3 %, dazu
        /// p_B und p_I nach Wunsch. Die Szenariosätze bleiben auf Vorgabe.</summary>
        private static WirtschaftlichkeitParameter Parametersatz(double preisB, double? preisI)
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = PROJEKT,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = preisB,
                PreissteigerungInvestition = preisI
            };
        }

        /// <summary>
        /// Der Kapitalwert des Szenarios ERWARTET über den ECHTEN Weg
        /// <see cref="WirtschaftlichkeitCtrl.Berechne"/> — also durch <c>BaueEingabe</c>,
        /// <c>RechneProjekt</c> und <c>RechneBild</c> hindurch bis in den
        /// <c>KapitalwertRechner</c>. Genau diese Kette musste Teil b um p_I erweitern.
        /// </summary>
        private static double Kapitalwert(WirtschaftlichkeitParameter p)
        {
            var daten = new BerichtsDaten { IdStamm = PROJEKT };
            daten.Varianten.Add(new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Probe W5-B-12",
                Ergebnis = new ErgebnisModel(),
                Energiekosten = 3000.0
            });

            List<WirtschaftlichkeitErgebnis> alle = new WirtschaftlichkeitCtrl().Berechne(daten, p);
            WirtschaftlichkeitErgebnis e = alle.Find(
                x => x.IdProjekt == PROJEKT &&
                     x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);

            Assert.NotNull(e);
            Assert.Null(e.Fehlgrund);
            Assert.True(e.Kapitalwert.HasValue, "Kein Kapitalwert gerechnet.");
            return e.Kapitalwert.Value;
        }

        /// <summary>
        /// EINE Investitionszeile über <see cref="HAUPT"/> € mit der gewünschten
        /// Nutzungsdauer, alle übrigen Zeilen der Kategorie 1 auf 0 — und keine
        /// gepflegten Szenariowerte. So hängt der Unterschied zwischen zwei Läufen an
        /// genau einer Größe.
        /// </summary>
        private static void BeispielAnlegen(double nutzungsdauer)
        {
            Setze(ID_HAUPT, HAUPT, nutzungsdauer);
            foreach (int id in ID_UEBRIGE) Setze(id, 0.0, 0.0);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET BestCase = 0, WorstCase = 0, " +
                "BestCase_Nutzungsdauer = 0, WorstCase_Nutzungsdauer = 0 " +
                "WHERE ProjektID = " + PROJEKT + " AND KategorieID = 1");
        }

        private static void Setze(int id, double wert, double nutzungsdauer)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ?, Bemessung = ?, " +
                "Einheitpreis = NULL, Menge = NULL, Nutzungsdauer = ?, StartJahr = NULL, " +
                "Kostenart = ? WHERE ID = " + id,
                new DbParam("@w", wert),
                new DbParam("@b", DbWerte.BEMESSUNG_BETRAG),
                new DbParam("@n", nutzungsdauer),
                new DbParam("@k", DbWerte.KOSTENART_KAPITALGEBUNDEN));
        }
    }
}
