using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DAS VOLUMEN IST DIE EINZIGE BEZUGSGROESSE DES PUFFERSPEICHERS - Migrationsschritt 77
    // (Auftrag #284, Anwenderentscheid vom 15.09.2026, wortgleich: "Pruefe Pufferspeicher
    // Daten mit Volumen/Groesse. EUR_PRO_KWH_KAPAZITAET spielt keine Rolle, nur das
    // Volumen als Bezugsgroesse").
    //
    // WOZU. Der Pufferspeicher fuehrt keine kWh-Kapazitaet: Ohne Temperaturpaar gibt es
    // keine belastbare Umrechnung seines Volumens, und die Definition gehoert zur
    // Speicherrechnung, nicht in eine Kostenformel. TechnikPlanwertCtrl liefert deshalb
    // fuer EUR_PRO_KWH_KAPAZITAET an diesem Gewerk keine Bezugsgroesse - ein satzbasierter
    // Betrag liefe ueber den Anwenderentscheid I-2 auf den erfassten Betrag hinaus, bei
    // einer Satzzeile also auf 0. Die ausgelieferte Investitionsvorlage der Komponente
    // trug die Art dennoch; sie ist die Quelle jedes neuen Projekts. Ab diesem Schritt
    // traegt sie EUR_PRO_KW_LEISTUNG - am Pufferspeicher die Bemessung je LITER
    // Gesamtvolumen ("je Liter", EUR/Ltr.), festgelegt am selben Tag.
    //
    // ERGEBNISNEUTRAL, UND ZWAR NACHWEISBAR. Umgestellt wird NUR eine Zeile, deren Satz
    // NULL ist - eine Vorlagenzeile ohne Satz gibt allein die ART vor, keine Zahl. Eine
    // Zeile mit gepflegtem Satz bleibt unangetastet: Ihre Zahl waere ein EUR/kWh-Satz,
    // und ihn stillschweigend als EUR/Ltr.-Satz weiterzufuehren waere eine Umdeutung
    // gespeicherter Anwenderdaten. Dieser Fall IST moeglich - der Schreibschutz der
    // Auslieferungsvorlagen ist seit Ae8 (26.08.2026) aufgehoben, KostenVorlagenCtrl.
    // IstNurLesen liefert false, und jede Vorlagenzeile laesst sich pflegen. Die
    // Auskunft ZaehlungGepflegt nennt die Zahl solcher Zeilen; der Migrationsbericht
    // schreibt sie mit.
    //
    // WAS DER SCHRITT NICHT ANFASST. Tab_ProjektWerte - die Kostenzeilen der Projekte -
    // bleibt vollstaendig unberuehrt: Dort steht die Bemessung eines Projekts samt
    // gepflegtem Wert, und eine Projektzeile ist kein Auslieferungsdatum. Ebenso bleiben
    // die Vorlagen der uebrigen neun Komponenten stehen, auch die PV-Position
    // "Batteriespeicher": Sie bemisst den Batteriespeicher einer PV-Anlage, nicht einen
    // Pufferspeicher, und der Entscheid gilt dem Pufferspeicher.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // WechselrichterSchema (65), AnlageStrangSchema (66), SpeicherAuslegungStrict (74),
    // NutzungsdauerSchema (75) und ProjektEnergietraegerEindeutig (76): Die Anweisung
    // braucht DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    //
    // DIE SAAT WANDERT MIT. SchemaKatalog.Schritt39_Vorlagen - die Quelle der zwanzig
    // Auslieferungsvorlagen einer frisch gesaeten Datenbank - traegt fuer die Position
    // "Speicher" der Komponente Pufferspeicher dieselbe Art wie dieser Schritt. Saat und
    // Nachzug duerfen nicht auseinanderlaufen; der Nachweis haelt beide gegeneinander.
    // ====================================================================================

    /// <summary>
    /// Die Umstellung der ausgelieferten Pufferspeicher-Vorlage auf die Volumenbemessung
    /// (Schemaschritt 77) - EINE Quelle fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class PufferspeicherBemessungVolumen
    {
        /// <summary><c>Tab_KostenKomponente.ID</c> des Pufferspeichers - dieselbe feste
        /// Nummer wie in <c>BetriebskostenCtrl.KOMPONENTE_PUFFERSPEICHER</c>.</summary>
        public const int KOMPONENTE_PUFFERSPEICHER = 6;

        /// <summary>Die Art, die an diesem Gewerk keine Bezugsgroesse hat.</summary>
        public const string BEMESSUNG_ALT = DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET;

        /// <summary>Die Art, die am Pufferspeicher das GESAMTVOLUMEN in Litern bemisst
        /// (Anzeige "je Liter", Einheit EUR/Ltr. - <c>BemessungKatalog.Anzeige</c>).</summary>
        public const string BEMESSUNG_NEU = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG;

        // =================================================================
        //  Die Anweisung
        // =================================================================

        /// <summary>Die Vorlagen dieses Gewerks - Unterabfrage beider Anweisungen.</summary>
        private const string VORLAGEN_DES_GEWERKS =
            "SELECT \"ID\" FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" " +
            "WHERE \"" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "\" = 6";

        /// <summary>
        /// Setzt an den Vorlagenpositionen des Pufferspeichers die Volumenbemessung -
        /// aber NUR, wo kein Satz gepflegt ist.
        ///
        /// <para><b><c>Satz IS NULL</c> ist die eigentliche Zusage.</b> Eine Zeile ohne
        /// Satz gibt nur die Art vor; ihre Umstellung aendert keinen gespeicherten Betrag
        /// und kein Rechenergebnis. Eine Zeile MIT Satz traegt eine Zahl je kWh; sie
        /// bliebe stehen und waere nach der Umstellung eine Zahl je Liter - das waere
        /// eine stille Umdeutung und ist deshalb ausgeschlossen.</para>
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet keine Zeile mehr mit der
        /// alten Art.</para>
        /// </summary>
        public const string SQL_UMSTELLEN =
            "UPDATE \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "\" " +
            "SET \"" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "\" = '" + BEMESSUNG_NEU + "' " +
            "WHERE \"" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "\" = '" + BEMESSUNG_ALT + "' " +
            "AND \"" + SchemaKatalog.SPALTE_KVP_SATZ + "\" IS NULL " +
            "AND \"" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "\" IN (" + VORLAGEN_DES_GEWERKS + ")";

        /// <summary>
        /// Die eine Anweisung des Schritts - als Paar Name/SQL, damit Migration und
        /// Werkzeug sie gleich abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Volumenbemessung der Pufferspeicher-Vorlage", SQL_UMSTELLEN);
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Vorlagenzeilen des Pufferspeichers tragen noch die alte Art OHNE
        /// Satz? Genau so viele stellt <see cref="SQL_UMSTELLEN"/> um. 0 = nichts zu tun.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "\" " +
                   "WHERE \"" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "\" = '" + BEMESSUNG_ALT + "' " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_SATZ + "\" IS NULL " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "\" IN (" + VORLAGEN_DES_GEWERKS + ")";
        }

        /// <summary>
        /// Wie viele Vorlagenzeilen des Pufferspeichers tragen die alte Art MIT gepflegtem
        /// Satz? Genau die laesst der Schritt in Ruhe - die Zahl gehoert deshalb in den
        /// Bericht, damit sie niemandem entgeht.
        /// </summary>
        public static string ZaehlungGepflegt()
        {
            return "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "\" " +
                   "WHERE \"" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "\" = '" + BEMESSUNG_ALT + "' " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_SATZ + "\" IS NOT NULL " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "\" IN (" + VORLAGEN_DES_GEWERKS + ")";
        }

        /// <summary>
        /// Steht noch etwas um? Eine nicht lesbare Zaehlung gilt als "nichts zu tun" -
        /// dieselbe Regel wie bei <c>ProjektEnergietraegerEindeutig.EntdoppelungNoetig</c>.
        /// </summary>
        public static bool UmstellungNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }
    }
}
