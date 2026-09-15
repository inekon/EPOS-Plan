using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE PV-VORLAGENZEILE "BATTERIESPEICHER" BEKOMMT EINE BEZUGSGROESSE, DIE ES GIBT
    // - Migrationsschritt 78 (Auftrag #287, Anwenderentscheid vom 15.09.2026).
    //
    // WOZU. Die ausgelieferte Investitionsvorlage der Photovoltaik (Vorlage 5,
    // Tab_KostenKomponente.ID 3) fuehrt die Position "Batteriespeicher" mit der Bemessung
    // "je kWh Kapazitaet". Eine Kapazitaet fuehrt die PV-Anlage nicht: Ihre einzige
    // Baugroesse ist die installierte Leistung in kWp (Modulanzahl x Modulleistung,
    // PhotovoltaikCtrl.KwpSumme). TechnikPlanwertCtrl liefert fuer EUR_PRO_KWH_KAPAZITAET
    // an diesem Gewerk deshalb keine Bezugsgroesse, und ein dort gepflegter Satz laeuft
    // ueber den Anwenderentscheid I-2 auf den erfassten Betrag hinaus - bei einer reinen
    // Satzzeile also auf 0, und zwar ohne Warnung. Dieselbe Lage wie am Pufferspeicher
    // (Schritt 77), nur am anderen Gewerk.
    //
    // WARUM DER FESTE BETRAG UND NICHT "JE KWP". Die Frage ist gemessen worden, nicht
    // geraten: Die Photovoltaik fuehrt nach WirtschaftlichkeitCtrl.BasisGrund im
    // Investitionsraster genau zwei Arten mit einer ECHTEN Baugroesse - "je kWp Leistung"
    // und "je kW elektrisch", und beide meinen DIESELBE Zahl, die installierte
    // Modulleistung. Ein Batteriespeicher-Satz je kWp bemaesse also den PREIS EINES
    // GERAETS AN DER GROESSE EINES ANDEREN; die Kapazitaet, an der ein Speicher wirklich
    // haengt, steht nirgends darin. Genau diese Erfindung soll der Entscheid abstellen,
    // nicht durch eine zweite ersetzen.
    //
    // Die Vorlage selbst sagt, wie ihre Geraetepositionen ohne eigene Baugroesse bemessen
    // werden: "Wechselrichter", "Montagesystem / Unterkonstruktion" und "Bauliche Anlagen
    // (Geruest etc.)" stehen alle auf dem FESTEN BETRAG - nur die Modulzeile traegt den
    // kWp-Satz. Der Batteriespeicher reiht sich dort ein. Er ist damit die einzige
    // Bemessung, die an diesem Gewerk sicher einen Betrag ergibt: Beim festen Betrag IST
    // der erfasste Wert der Betrag (BemessungKatalog.Info.Absolut), es gibt keine Menge,
    // die fehlen koennte.
    //
    // Wer den Speicher nach seiner KAPAZITAET bemessen will, hat dafuer das Gewerk
    // STROMSPEICHER (Tab_KostenKomponente.ID 5): Dessen Investitionsvorlage traegt die
    // Position "Speicher" mit "je kWh Kapazitaet", und Tab_Stromspeicher.Energie ist die
    // Bezugsgroesse dazu. Die kapazitaetsbemessene Zeile geht also nicht verloren - sie
    // steht an dem Gewerk, das die Kapazitaet fuehrt.
    //
    // ERGEBNISNEUTRAL, UND ZWAR NACHWEISBAR. Umgestellt wird NUR eine Zeile, deren Satz
    // NULL ist - eine Vorlagenzeile ohne Satz gibt allein die ART vor, keine Zahl. Eine
    // Zeile mit gepflegtem Satz bleibt unangetastet: Ihre Zahl waere ein EUR/kWh-Satz,
    // und ihn stillschweigend als festen EUR-Betrag weiterzufuehren waere eine Umdeutung
    // gespeicherter Anwenderdaten - hier sogar eine besonders grobe, weil aus einem Satz
    // je Einheit ein Gesamtbetrag wuerde. Der Fall IST moeglich: Der Schreibschutz der
    // Auslieferungsvorlagen ist seit Ae8 (26.08.2026) aufgehoben. Die Auskunft
    // ZaehlungGepflegt nennt die Zahl solcher Zeilen; der Migrationsbericht schreibt sie
    // mit.
    //
    // WAS DER SCHRITT NICHT ANFASST. Tab_ProjektWerte - die Kostenzeilen der Projekte -
    // bleibt vollstaendig unberuehrt: Dort steht die Bemessung eines Projekts samt
    // gepflegtem Wert, und eine Projektzeile ist kein Auslieferungsdatum. Ebenso bleiben
    // die Vorlagen der uebrigen neun Komponenten stehen, insbesondere die Position
    // "Speicher" des Stromspeichers - dort IST die Kapazitaet die Baugroesse.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // WechselrichterSchema (65), SpeicherAuslegungStrict (74), NutzungsdauerSchema (75),
    // ProjektEnergietraegerEindeutig (76) und PufferspeicherBemessungVolumen (77): Die
    // Anweisung braucht DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    //
    // DIE SAAT WANDERT MIT. SchemaKatalog.Schritt39_Vorlagen - die Quelle der zwanzig
    // Auslieferungsvorlagen einer frisch gesaeten Datenbank - traegt fuer die Position
    // "Batteriespeicher" der Komponente Photovoltaik dieselbe Art wie dieser Schritt.
    // Saat und Nachzug duerfen nicht auseinanderlaufen; der Nachweis haelt beide
    // gegeneinander.
    // ====================================================================================

    /// <summary>
    /// Die Umstellung der ausgelieferten PV-Position "Batteriespeicher" auf den festen
    /// Betrag (Schemaschritt 78) - EINE Quelle fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class PvVorlageBatteriespeicher
    {
        /// <summary><c>Tab_KostenKomponente.ID</c> der Photovoltaik - dieselbe feste
        /// Nummer wie in <c>TechnikPlanwertCtrl.KomponentenName</c> und
        /// <c>EndenergieAufloeser.KOMPONENTE_PHOTOVOLTAIK</c>.</summary>
        public const int KOMPONENTE_PHOTOVOLTAIK = 3;

        /// <summary>Die Art, die an diesem Gewerk keine Bezugsgroesse hat.</summary>
        public const string BEMESSUNG_ALT = DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET;

        /// <summary>Die Art, die an diesem Gewerk sicher einen Betrag ergibt: der FESTE
        /// BETRAG - der erfasste Wert IST der Betrag, eine Menge braucht es nicht.</summary>
        public const string BEMESSUNG_NEU = DbWerte.BEMESSUNG_BETRAG;

        /// <summary>Die Bezeichnung der betroffenen Auslieferungsposition - dieselbe
        /// Schreibweise wie in <c>SchemaKatalog.Schritt39_Vorlagen</c>.</summary>
        public const string POSITION = "Batteriespeicher";

        // =================================================================
        //  Die Anweisung
        // =================================================================

        /// <summary>Die Vorlagen dieses Gewerks - Unterabfrage beider Anweisungen.</summary>
        private const string VORLAGEN_DES_GEWERKS =
            "SELECT \"ID\" FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGE + "\" " +
            "WHERE \"" + SchemaKatalog.SPALTE_KV_KOMPONENTENID + "\" = 3";

        /// <summary>
        /// Setzt an den Vorlagenpositionen der Photovoltaik den festen Betrag - aber NUR,
        /// wo kein Satz gepflegt ist.
        ///
        /// <para><b>Am GEWERK, nicht am Namen.</b> Die Bedingung fragt nach der Komponente
        /// und der alten Art, nicht nach der Bezeichnung "Batteriespeicher": Wer der
        /// PV-Vorlage eine zweite kapazitaetsbemessene Zeile hinzugefuegt hat, steht vor
        /// genau demselben Betrag 0. Die Bezeichnung waere hier eine zweite, schwaechere
        /// Wahrheit - das Gewerk ist der Grund.</para>
        ///
        /// <para><b><c>Satz IS NULL</c> ist die eigentliche Zusage.</b> Eine Zeile ohne
        /// Satz gibt nur die Art vor; ihre Umstellung aendert keinen gespeicherten Betrag
        /// und kein Rechenergebnis. Eine Zeile MIT Satz traegt eine Zahl je kWh; sie
        /// bliebe stehen und waere nach der Umstellung ein fester Betrag - das waere eine
        /// stille Umdeutung und ist deshalb ausgeschlossen.</para>
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
                    "Fester Betrag fuer die PV-Position " + POSITION, SQL_UMSTELLEN);
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Vorlagenzeilen der Photovoltaik tragen noch die alte Art OHNE Satz?
        /// Genau so viele stellt <see cref="SQL_UMSTELLEN"/> um. 0 = nichts zu tun.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM \"" + SchemaKatalog.TAB_KOSTENVORLAGEPOSITION + "\" " +
                   "WHERE \"" + SchemaKatalog.SPALTE_KVP_BEMESSUNG + "\" = '" + BEMESSUNG_ALT + "' " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_SATZ + "\" IS NULL " +
                   "AND \"" + SchemaKatalog.SPALTE_KVP_VORLAGEID + "\" IN (" + VORLAGEN_DES_GEWERKS + ")";
        }

        /// <summary>
        /// Wie viele Vorlagenzeilen der Photovoltaik tragen die alte Art MIT gepflegtem
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
        /// dieselbe Regel wie bei <c>PufferspeicherBemessungVolumen.UmstellungNoetig</c>.
        /// </summary>
        public static bool UmstellungNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }
    }
}
