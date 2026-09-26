using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ANWENDERBEFUND 19.09.2026 (#363) — „Die Hilfsenergiekosten werden nicht
    /// berechnet auch nach Simulation. Korrigiere und prüfe andere Felder mit
    /// Abhängigkeiten."</b>
    ///
    /// <para><b>Zwei Befunde stecken darin, und beide werden hier gepinnt.</b></para>
    ///
    /// <list type="number">
    ///   <item><description><b>Der Kessel verlor seine Bezugsgröße an einem
    ///     GESPEICHERTEN Lauf.</b> <c>Tab_ErgebnisHeizkesselModul.Verbrauch</c> und
    ///     <c>Waermeproduktion</c> füllt der Lauf erst seit Befund B-1; jeder Lauf von
    ///     davor trägt dort 0. Der Endenergie-Auflöser las die Spalten roh — und damit
    ///     standen „je kWh thermisch", „% der Endenergiekosten" und „% des
    ///     Endenergiebedarfs" an einem Kessel ohne Bezugsgröße, obwohl derselbe Lauf
    ///     Wärme und Nutzungsgrad führt. Jetzt gilt in beiden Fällen dieselbe
    ///     Ableitung, die Steuerseite und Bericht längst benutzen.</description></item>
    ///   <item><description><b>Der GRUND war irreführend.</b> Jede Zeile ohne
    ///     Laufgröße nannte „kein Simulationslauf" — auch die, deren Lauf samt Menge
    ///     dastand und der allein der Arbeitspreis des Energieträgers fehlte. Vier
    ///     Lagen sehen im Raster gleich aus und verlangen vier verschiedene
    ///     Handgriffe; sie heißen jetzt auch verschieden.</description></item>
    /// </list>
    ///
    /// <para><b>Die MATRIX ist der Kern dieser Klasse</b>
    /// (<see cref="Die_Matrix_der_Gewerke_und_Bemessungen_steht"/>): je Gewerk und je
    /// Bemessungsart eine Zeile — hat die Position eine Bezugsgröße, und wenn nicht,
    /// warum nicht. Sie prüft den ganzen Bestand auf einmal und zeigt bei Rot in einem
    /// Textdiff, welche Zelle gekippt ist.</para>
    ///
    /// <para><b>Gerechnet wird gegen die GESPEICHERTEN Läufe der Testdatenbank</b>,
    /// nicht gegen einen frischen — genau das ist die Lage des Befundes. Ein Fall
    /// rechnet zusätzlich frisch nach und zeigt, dass beide Wege dieselbe Zahl
    /// liefern. Eigene Arbeitskopie je Fall (<see cref="TestDatenbank"/>): mehrere
    /// Fälle SCHREIBEN (Lauf löschen, Menge auf 0 setzen, Anlage umhängen).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenBemessungsmatrixTests
    {
        /// <summary>BHKW-Kaskade mit Gaskessel und Pufferspeicher — das Projekt des
        /// Referenzlaufs. Sein Kessel steht IN der Kaskade (anders als bei 1007, #190).</summary>
        private const int PROJEKT_KASKADE = 1030;

        // Positionszeilen (Tab_ProjektWerte.ID) — Kategorie 2, je Gewerk eine.
        // E30 (#548, Datenpflege B3): Die Altzeilen 101600098/101600097 (Wartung als fester
        // Betrag ohne Vorlage) sind entfallen; ihre Betraege stehen in den Pflichtzeilen
        // derselben Komponente und Anlage - FrischeBasis fragt nur Komponente und Anlage.
        private const int Z_KASKADE_KESSEL = 101600585;    // Komponente 2, Anlage 11334
        private const int Z_KASKADE_PUFFER = 101600582;    // Komponente 6, Anlage 11331
        private const int Z_KASKADE_BHKW_1 = 101600588;    // Komponente 7, Anlage 14920
        private const int Z_KASKADE_BHKW_2 = 101600591;    // Komponente 7, Anlage 14921

        // Projekt 1026: Wärmepumpe, Gaskessel, PV, Solarthermie, Strom- und
        // Pufferspeicher — sechs Gewerke mit Betriebszeilen in EINEM Projekt. Sein
        // Energieträger „Erdgas E" führt KEINEN Arbeitspreis, einen Stromträger hat es
        // gar nicht, und sein gespeicherter Lauf führt nur Wärmepumpe und Kessel: die
        // Lage des Bildschirmfotos.
        private const int Z_SECHS_WP = 101600568;          // Komponente 1, Anlage 14917
        private const int Z_SECHS_KESSEL = 101600574;      // Komponente 2, Anlage 11275
        private const int Z_SECHS_PV = 101600580;          // Komponente 3, Anlage 11281
        private const int Z_SECHS_SOLAR = 101600571;       // Komponente 4, Anlage 11274
        private const int Z_SECHS_STROMSP = 101600577;     // Komponente 5, Anlage 11280
        private const int Z_SECHS_PUFFER = 101600565;      // Komponente 6, Anlage 11264

        /// <summary>Die Anlage des Pufferspeichers in 1030 — sie steht in KEINER
        /// Kessel-Modulzeile und dient deshalb als „Anlage nicht im Lauf".</summary>
        private const int ANLAGE_PUFFER_KASKADE = 11331;

        /// <summary>Nutzwärme des Kessels im gespeicherten Lauf von 1030
        /// [kWh/a] — <c>Waerme_Gas</c> 5.403,1 MWh.</summary>
        private const double KESSEL_WAERME_KWH = 5403100.0;

        /// <summary>Die vierzehn Bemessungsarten des Katalogs, die eine Bezugsgröße
        /// brauchen — der feste Betrag steht nicht dabei (er ist absolut).</summary>
        private static readonly string[] ARTEN =
        {
            DbWerte.BEMESSUNG_JAHRESBETRAG,
            DbWerte.BEMESSUNG_PROZENT_INVESTITION,
            DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN,
            DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
            DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
            DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH,
            DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
            DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
            DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG,
            DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH,
            DbWerte.BEMESSUNG_EUR_PRO_KWP,
            DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET,
            DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR,
            DbWerte.BEMESSUNG_EUR_PRO_H,
        };

        // =====================================================================
        // 1 — Der Befund selbst: der gespeicherte Lauf trägt die Bezugsgröße
        // =====================================================================

        /// <summary>
        /// DER BEFUND. Die Modulzeile des gespeicherten Laufs führt WEDER
        /// <c>Verbrauch</c> NOCH <c>Waermeproduktion</c> — so sieht jeder Lauf von vor
        /// Befund B-1 aus, und so sah der Lauf des Anwenders aus. Trotzdem müssen alle
        /// drei Laufgrößen des Kessels dastehen: Die Wärme steht auf ihren Kanälen, und
        /// der Brennstoff folgt aus ihr und dem Nutzungsgrad.
        ///
        /// <para>Der Fall stellt die Lage selbst her, statt sie in der Testdatenbank
        /// vorauszusetzen: Wird sie eines Tages mit einem frischen Lauf neu gesät,
        /// prüft er weiterhin dasselbe.</para>
        /// </summary>
        [Fact]
        public void Ein_gespeicherter_Lauf_ohne_die_zwei_Spalten_traegt_die_Bezugsgroesse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SpaltenLeeren(PROJEKT_KASKADE);
            Assert.Equal(0.0, ModulZahl(PROJEKT_KASKADE, "Verbrauch"));
            Assert.Equal(0.0, ModulZahl(PROJEKT_KASKADE, "Waermeproduktion"));

            Assert.Equal(KESSEL_WAERME_KWH,
                         Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH), 3);

            double kosten = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN);
            double bedarf = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF);
            Assert.True(kosten > 0, "Weg A steht ohne Bezugsgröße da.");
            Assert.True(bedarf > 0, "Weg B steht ohne Bezugsgröße da.");

            // Beide Wege bemessen sich an DERSELBEN Menge, nur anders bewertet: Weg A
            // mit dem Arbeitspreis des Brennstoffträgers, Weg B mit dem Strompreis
            // (§ 4.5). Zwei verschiedene Preise, also zwei verschiedene Zahlen.
            Assert.NotEqual(kosten, bedarf);
        }

        /// <summary>
        /// Die Gegenprobe: Ein FRISCHER Lauf füllt beide Spalten — und liefert
        /// dieselben Zahlen. Ohne diesen Fall bliebe offen, ob die Ableitung nur eine
        /// Notlösung ist oder wirklich die Größe des Laufs trifft.
        /// </summary>
        [Fact]
        public void Der_frische_Lauf_liefert_dieselben_Zahlen_wie_die_Ableitung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SpaltenLeeren(PROJEKT_KASKADE);
            double waermeAbgeleitet = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH);
            double kostenAbgeleitet = Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN);

            Rechne(PROJEKT_KASKADE);
            Assert.True(ModulZahl(PROJEKT_KASKADE, "Verbrauch") > 0,
                        "Der frische Lauf hat die Spalte nicht gefüllt.");

            Assert.Equal(waermeAbgeleitet,
                         Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH), 3);
            Assert.Equal(kostenAbgeleitet,
                         Basis(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN), 3);
        }

        // =====================================================================
        // 2 — Die vier Gründe: jede Lage nennt ihren eigenen
        // =====================================================================

        /// <summary>
        /// DIE LAGE DES BILDSCHIRMFOTOS. Lauf da, Menge da — und trotzdem keine
        /// Bezugsgröße, weil „Erdgas E" keinen Arbeitspreis führt und das Projekt gar
        /// keinen Stromträger hat. Der Grund muss den PREIS nennen und damit die
        /// Energieträgerverwaltung, nicht einen Lauf, den es längst gibt.
        ///
        /// <para>Die Mengenart daneben („je kWh thermisch") hat ihre Bezugsgröße — sie
        /// braucht keinen Preis. Das ist die Probe darauf, dass wirklich der Preis
        /// fehlt und nicht der Lauf.</para>
        /// </summary>
        [Fact]
        public void Ohne_Arbeitspreis_nennt_der_Grund_den_Preis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_SECHS_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));
            // ANWENDERENTSCHEID 19.09.2026: Weg B am BRENNSTOFFkessel vermisst den
            // Arbeitspreis des PROJEKT-Stromträgers und sagt das mit einem eigenen
            // Steuerwert — es ist ein anderer Eintrag der Energieträgerverwaltung als
            // der, den Weg A daneben meint.
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS,
                         Grund(Z_SECHS_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_PREIS,
                         Grund(Z_SECHS_WP, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));

            Assert.True(Basis(Z_SECHS_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH) > 0);
        }

        /// <summary>
        /// Zeigt die Zeile auf eine Anlage, die der Lauf nicht führt, ist ein zweiter
        /// Simulationslauf keine Abhilfe — die Anlage braucht einen PLATZ in der
        /// Simulationskonfiguration (#190). Der Fall hängt die Kesselzeile an den
        /// Pufferspeicher: Sein Bezeichner steht in keiner Kessel-Modulzeile.
        /// </summary>
        [Fact]
        public void Eine_Anlage_ausserhalb_des_Laufs_nennt_die_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = ? WHERE ID = ?",
                new DbParam("@a", ANLAGE_PUFFER_KASKADE),
                new DbParam("@id", Z_KASKADE_KESSEL));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_ANLAGE,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_ANLAGE,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));
        }

        /// <summary>
        /// Lauf da, Anlage da, Menge 0 — dann ist nichts zu suchen; der Grund sagt
        /// genau das. Ohne diese Unterscheidung stünde dort ein Handlungsauftrag
        /// („starte die Simulation"), der ins Leere liefe.
        /// </summary>
        [Fact]
        public void Eine_Menge_von_Null_im_Lauf_nennt_die_Menge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            SpaltenLeeren(PROJEKT_KASKADE);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ErgebnisHeizkesselModul SET Waerme_Gas = 0, Waerme_Oel = 0 " +
                "WHERE ID_ErgebnisHeizkessel IN (SELECT h.ID FROM Tab_ErgebnisHeizkessel AS h " +
                "INNER JOIN Tab_Ergebnis AS e ON h.ID_Ergebnis = e.ID WHERE e.ID_Projekt = ?)",
                new DbParam("@p", PROJEKT_KASKADE));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_MENGE,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_MENGE,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));
        }

        /// <summary>
        /// Und ohne jeden Lauf bleibt es beim alten Grund — der einzige Fall, in dem
        /// „kein Simulationslauf" auch wirklich stimmt.
        /// </summary>
        [Fact]
        public void Ohne_Lauf_bleibt_es_bei_kein_Simulationslauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL("DELETE FROM Tab_Ergebnis WHERE ID_Projekt = ?",
                                      new DbParam("@p", PROJEKT_KASKADE));

            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                         Grund(Z_KASKADE_KESSEL, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN));
        }

        /// <summary>
        /// Die LANDKARTE ohne Lauf bleibt unangetastet — sie füllt auch die
        /// Auswahlliste der Bemessungsarten (<c>KostenVorlagenCtrl.PasstZuGewerk</c>),
        /// und dort darf nicht plötzlich ein Preis- oder Mengengrund stehen.
        /// </summary>
        [Fact]
        public void Die_Landkarte_ohne_Projekt_antwortet_wie_zuvor()
        {
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_LAUF,
                         WirtschaftlichkeitCtrl.BasisGrundFuerZeile(
                             DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
                             BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL, 0));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK,
                         WirtschaftlichkeitCtrl.BasisGrund(
                             DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN,
                             EndenergieAufloeser.KOMPONENTE_PHOTOVOLTAIK));
        }

        // =====================================================================
        // 3 — Die Matrix: jedes Gewerk gegen jede Bemessungsart
        // =====================================================================

        /// <summary>
        /// DIE MATRIX. Je Gewerk und Bemessungsart eine Zeile: „ja" heißt, die
        /// Position trägt eine Bezugsgröße; sonst steht der Grund. Gerechnet wird
        /// gegen die GESPEICHERTEN Läufe der Testdatenbank — die Lage, in der der
        /// Anwender steht, wenn er einen Dialog öffnet.
        ///
        /// <para><b>Was die Matrix zeigt.</b> Der Gaskessel der Kaskade (1030) trägt
        /// jede seiner drei Laufgrößen; die sechs Gewerke von 1026 zerfallen in die
        /// Gründe Gewerk (die Art passt nicht), Investition und — neu benannt —
        /// Preis und Anlage.</para>
        ///
        /// <para>Verglichen wird EIN Text gegen EINEN Text: Bei Rot zeigt der Diff
        /// unmittelbar, welche Zelle gekippt ist.</para>
        /// </summary>
        [Fact]
        public void Die_Matrix_der_Gewerke_und_Bemessungen_steht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(Erwartet(), Gemessen());
        }

        private static string Erwartet()
        {
            return string.Join("\n", new[]
            {
                // 1030 — BHKW-Kaskade mit Gaskessel und Pufferspeicher;
                //        1026 — sechs Gewerke, Erdgas E ohne Arbeitspreis,
                //        kein Stromträger, und ein Lauf, der nur Wärmepumpe
                //        und Kessel führt.
                "1030 Kessel JAHRESBETRAG nein",
                "1030 Kessel PROZENT_INVESTITION ja",
                "1030 Kessel PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1030 Kessel PROZENT_ENDENERGIEKOSTEN ja",
                "1030 Kessel PROZENT_ENDENERGIEBEDARF ja",
                "1030 Kessel EUR_PRO_KWH_THERMISCH ja",
                "1030 Kessel EUR_PRO_KWH_ELEKTRISCH nein GEWERK",
                "1030 Kessel EUR_PRO_KW_LEISTUNG ja",
                "1030 Kessel EUR_PRO_KW_HEIZLEISTUNG ja",
                "1030 Kessel EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1030 Kessel EUR_PRO_KWP nein GEWERK",
                "1030 Kessel EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1030 Kessel EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1030 Kessel EUR_PRO_H nein GEWERK",
                "1030 Puffer JAHRESBETRAG nein",
                "1030 Puffer PROZENT_INVESTITION ja",
                "1030 Puffer PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1030 Puffer PROZENT_ENDENERGIEKOSTEN nein GEWERK",
                "1030 Puffer PROZENT_ENDENERGIEBEDARF nein GEWERK",
                "1030 Puffer EUR_PRO_KWH_THERMISCH nein GEWERK",
                "1030 Puffer EUR_PRO_KWH_ELEKTRISCH nein GEWERK",
                "1030 Puffer EUR_PRO_KW_LEISTUNG ja",
                "1030 Puffer EUR_PRO_KW_HEIZLEISTUNG nein GEWERK",
                "1030 Puffer EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1030 Puffer EUR_PRO_KWP nein GEWERK",
                "1030 Puffer EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1030 Puffer EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1030 Puffer EUR_PRO_H nein GEWERK",
                "1030 BHKW JAHRESBETRAG nein",
                "1030 BHKW PROZENT_INVESTITION ja",
                "1030 BHKW PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1030 BHKW PROZENT_ENDENERGIEKOSTEN ja",
                "1030 BHKW PROZENT_ENDENERGIEBEDARF ja",
                "1030 BHKW EUR_PRO_KWH_THERMISCH ja",
                "1030 BHKW EUR_PRO_KWH_ELEKTRISCH ja",
                "1030 BHKW EUR_PRO_KW_LEISTUNG ja",
                "1030 BHKW EUR_PRO_KW_HEIZLEISTUNG ja",
                "1030 BHKW EUR_PRO_KW_ELEKTRISCH ja",
                "1030 BHKW EUR_PRO_KWP nein GEWERK",
                "1030 BHKW EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1030 BHKW EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1030 BHKW EUR_PRO_H ja",
                "1026 WP JAHRESBETRAG nein",
                "1026 WP PROZENT_INVESTITION ja",
                "1026 WP PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 WP PROZENT_ENDENERGIEKOSTEN nein PREIS",
                // ANWENDERENTSCHEID 19.09.2026: Weg B bewertet mit Strom. Die
                // Wärmepumpe von 1026 trägt KEINEN eigenen Stromträger, es gilt also
                // der des Projekts — und den hat 1026 nicht. Der Grund nennt deshalb
                // genau diesen Eintrag, nicht den Träger der Anlage.
                "1026 WP PROZENT_ENDENERGIEBEDARF nein STROMPREIS",
                // E23 (25.09.2026): Beide kWh-Arten stehen an der Wärmepumpe nicht
                // mehr zur Auswahl (Landkarte GEWERK) — eine Bestandszeile RECHNET
                // aber weiter: Die Menge kommt aus dem Lauf, nicht aus der Landkarte.
                "1026 WP EUR_PRO_KWH_THERMISCH ja",
                "1026 WP EUR_PRO_KWH_ELEKTRISCH ja",
                "1026 WP EUR_PRO_KW_LEISTUNG ja",
                "1026 WP EUR_PRO_KW_HEIZLEISTUNG ja",
                "1026 WP EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1026 WP EUR_PRO_KWP nein GEWERK",
                "1026 WP EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1026 WP EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1026 WP EUR_PRO_H ja",
                "1026 Kessel JAHRESBETRAG nein",
                "1026 Kessel PROZENT_INVESTITION ja",
                "1026 Kessel PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 Kessel PROZENT_ENDENERGIEKOSTEN nein PREIS",
                // Derselbe Entscheid am BRENNSTOFFkessel: Sein Hilfsstrom hängt
                // ohnehin am Stromträger des Projekts, sein eigener Träger ist Gas.
                "1026 Kessel PROZENT_ENDENERGIEBEDARF nein STROMPREIS",
                "1026 Kessel EUR_PRO_KWH_THERMISCH ja",
                "1026 Kessel EUR_PRO_KWH_ELEKTRISCH nein GEWERK",
                "1026 Kessel EUR_PRO_KW_LEISTUNG ja",
                "1026 Kessel EUR_PRO_KW_HEIZLEISTUNG ja",
                "1026 Kessel EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1026 Kessel EUR_PRO_KWP nein GEWERK",
                "1026 Kessel EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1026 Kessel EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1026 Kessel EUR_PRO_H nein GEWERK",
                "1026 PV JAHRESBETRAG nein",
                "1026 PV PROZENT_INVESTITION ja",
                "1026 PV PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 PV PROZENT_ENDENERGIEKOSTEN nein GEWERK",
                "1026 PV PROZENT_ENDENERGIEBEDARF nein GEWERK",
                "1026 PV EUR_PRO_KWH_THERMISCH nein GEWERK",
                "1026 PV EUR_PRO_KWH_ELEKTRISCH nein ANLAGE",
                "1026 PV EUR_PRO_KW_LEISTUNG nein GEWERK",
                "1026 PV EUR_PRO_KW_HEIZLEISTUNG nein GEWERK",
                "1026 PV EUR_PRO_KW_ELEKTRISCH ja",
                "1026 PV EUR_PRO_KWP ja",
                "1026 PV EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1026 PV EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1026 PV EUR_PRO_H nein GEWERK",
                "1026 Solar JAHRESBETRAG nein",
                "1026 Solar PROZENT_INVESTITION ja",
                "1026 Solar PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 Solar PROZENT_ENDENERGIEKOSTEN nein GEWERK",
                "1026 Solar PROZENT_ENDENERGIEBEDARF nein GEWERK",
                "1026 Solar EUR_PRO_KWH_THERMISCH nein ANLAGE",
                "1026 Solar EUR_PRO_KWH_ELEKTRISCH nein GEWERK",
                "1026 Solar EUR_PRO_KW_LEISTUNG ja",
                "1026 Solar EUR_PRO_KW_HEIZLEISTUNG ja",
                "1026 Solar EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1026 Solar EUR_PRO_KWP nein GEWERK",
                "1026 Solar EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1026 Solar EUR_PRO_M2_KOLLEKTOR ja",
                "1026 Solar EUR_PRO_H nein GEWERK",
                "1026 Stromsp JAHRESBETRAG nein",
                "1026 Stromsp PROZENT_INVESTITION ja",
                "1026 Stromsp PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 Stromsp PROZENT_ENDENERGIEKOSTEN nein GEWERK",
                "1026 Stromsp PROZENT_ENDENERGIEBEDARF nein GEWERK",
                "1026 Stromsp EUR_PRO_KWH_THERMISCH nein GEWERK",
                "1026 Stromsp EUR_PRO_KWH_ELEKTRISCH nein ANLAGE",
                "1026 Stromsp EUR_PRO_KW_LEISTUNG ja",
                "1026 Stromsp EUR_PRO_KW_HEIZLEISTUNG nein GEWERK",
                "1026 Stromsp EUR_PRO_KW_ELEKTRISCH ja",
                "1026 Stromsp EUR_PRO_KWP nein GEWERK",
                "1026 Stromsp EUR_PRO_KWH_KAPAZITAET ja",
                "1026 Stromsp EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1026 Stromsp EUR_PRO_H nein GEWERK",
                "1026 Puffer JAHRESBETRAG nein",
                "1026 Puffer PROZENT_INVESTITION ja",
                "1026 Puffer PROZENT_ERZEUGERKOSTEN nein INVEST",
                "1026 Puffer PROZENT_ENDENERGIEKOSTEN nein GEWERK",
                "1026 Puffer PROZENT_ENDENERGIEBEDARF nein GEWERK",
                "1026 Puffer EUR_PRO_KWH_THERMISCH nein GEWERK",
                "1026 Puffer EUR_PRO_KWH_ELEKTRISCH nein GEWERK",
                "1026 Puffer EUR_PRO_KW_LEISTUNG ja",
                "1026 Puffer EUR_PRO_KW_HEIZLEISTUNG nein GEWERK",
                "1026 Puffer EUR_PRO_KW_ELEKTRISCH nein GEWERK",
                "1026 Puffer EUR_PRO_KWP nein GEWERK",
                "1026 Puffer EUR_PRO_KWH_KAPAZITAET nein GEWERK",
                "1026 Puffer EUR_PRO_M2_KOLLEKTOR nein GEWERK",
                "1026 Puffer EUR_PRO_H nein GEWERK",
            });
        }

        private static string Gemessen()
        {
            var zeilen = new List<string>();
            Sammle(zeilen, "1030 Kessel", Z_KASKADE_KESSEL);
            Sammle(zeilen, "1030 Puffer", Z_KASKADE_PUFFER);
            Sammle(zeilen, "1030 BHKW", Z_KASKADE_BHKW_1);
            Sammle(zeilen, "1026 WP", Z_SECHS_WP);
            Sammle(zeilen, "1026 Kessel", Z_SECHS_KESSEL);
            Sammle(zeilen, "1026 PV", Z_SECHS_PV);
            Sammle(zeilen, "1026 Solar", Z_SECHS_SOLAR);
            Sammle(zeilen, "1026 Stromsp", Z_SECHS_STROMSP);
            Sammle(zeilen, "1026 Puffer", Z_SECHS_PUFFER);
            return string.Join("\n", zeilen);
        }

        private static void Sammle(List<string> ziel, string kopf, int positionsId)
        {
            foreach (string bem in ARTEN)
            {
                string grund;
                double? basis = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bem, out grund);
                ziel.Add(kopf + " " + bem + " " + (basis.HasValue ? "ja" : "nein") +
                         (grund.Length > 0 ? " " + grund : ""));
            }
        }

        /// <summary>Die zweite Anlage des BHKW steht in der Matrix nicht, weil sie
        /// dieselbe Landkarte hat; dieser Fall belegt, dass sie ihre EIGENE Laufmenge
        /// bekommt und nicht die der ersten.</summary>
        [Fact]
        public void Jede_Anlage_traegt_ihre_eigene_Laufmenge()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double eins = Basis(Z_KASKADE_BHKW_1, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH);
            double zwei = Basis(Z_KASKADE_BHKW_2, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH);

            Assert.True(eins > 0 && zwei > 0);
            Assert.True(eins > zwei, "Die zweite Anlage bekommt die Menge der ersten.");
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>Die Bezugsgröße einer Position zu einer Bemessungsart; der Fall
        /// bricht ab, wenn es keine gibt (dann sagt der Grund, warum).</summary>
        private static double Basis(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.True(b.HasValue, "Keine Bezugsgröße zu " + bemessung + ": " + grund);
            return b.Value;
        }

        /// <summary>Der Grund, wenn es keine Bezugsgröße gibt; der Fall bricht ab,
        /// wenn doch eine dasteht.</summary>
        private static string Grund(int positionsId, string bemessung)
        {
            string grund;
            double? b = WirtschaftlichkeitCtrl.FrischeBasis(positionsId, bemessung, out grund);
            Assert.False(b.HasValue, "Unerwartete Bezugsgröße zu " + bemessung + ": " + b);
            return grund;
        }

        /// <summary>Stellt die Lage „gespeicherter Lauf von vor Befund B-1" her:
        /// Verbrauch und Waermeproduktion leer, Wärme und Nutzungsgrad wie gehabt.</summary>
        private static void SpaltenLeeren(int idProjekt)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ErgebnisHeizkesselModul SET Verbrauch = 0, Waermeproduktion = 0 " +
                "WHERE ID_ErgebnisHeizkessel IN (SELECT h.ID FROM Tab_ErgebnisHeizkessel AS h " +
                "INNER JOIN Tab_Ergebnis AS e ON h.ID_Ergebnis = e.ID WHERE e.ID_Projekt = ?)",
                new DbParam("@p", idProjekt));
        }

        /// <summary>Eine Zahl der Kessel-Modulzeile des jüngsten Laufs.</summary>
        private static double ModulZahl(int idProjekt, string spalte)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT MAX(m.[" + spalte + "]) FROM Tab_ErgebnisHeizkesselModul AS m " +
                "INNER JOIN Tab_ErgebnisHeizkessel AS h ON m.ID_ErgebnisHeizkessel = h.ID " +
                "INNER JOIN Tab_Ergebnis AS e ON h.ID_Ergebnis = e.ID WHERE e.ID_Projekt = ?",
                new DbParam("@p", idProjekt));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o);
        }

        /// <summary>Rechnet das Projekt und SPEICHERT den Lauf — erst dann liest ihn
        /// der Endenergie-Auflöser.</summary>
        private static void Rechne(int idProjekt)
        {
            var laeufer = new SimulationRunner();
            string fehler;
            Assert.True(laeufer.Simuliere(idProjekt, out fehler), "Lauf gescheitert: " + fehler);
            Assert.True(SimulationLaufCtrl.ErgebnisSpeichern(
                            idProjekt, laeufer.simulation_Waermebedarf,
                            laeufer.simulation_Strombedarf, laeufer.sim),
                        "Lauf nicht gespeichert.");
        }
    }
}
