using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // GENAU EINE AKTIVE SPEICHERVARIANTE JE PROJEKT - Beifang des Migrationsschritts 87
    // (Nebenbefund aus Auftrag #321, Anwenderentscheid US-E-1 (a) vom 17.09.2026).
    //
    // WOZU. "Genau eine Variante ist aktiv" ist die Regel, die
    // StromspeicherVarianteCtrl.SetzeAktiv schreibt: erst ALLE Varianten des PROJEKTS
    // zuruecksetzen, dann die gewaehlte setzen - zwei zielgenaue UPDATEs, damit der
    // Zustand auch dann eindeutig bleibt, wenn das zweite scheitert. Die Datenbank selbst
    // liess bis hierher zwei aktive Zeilen zu, und die Messlatte
    // Referenzlaeufe/Kenndaten_Test.sqlite fuehrt genau diesen Fall: Projekt 1026 hat an
    // der Anlage 11280 die Varianten 10 und 13, beide mit Aktiv = 1.
    //
    // JE PROJEKT, NICHT JE ANLAGE. Beide Leser fragen PROJEKTWEIT:
    // ReadAktiveVariante joint ueber Tab_Energieanlagen auf ID_Projekt und nimmt
    // "ORDER BY v.ID LIMIT 1"; SetzeAktiv raeumt projektweit ab. Die Regel "je Anlage
    // eine" waere schwaecher als das, was beide tun - und sie liesse genau den Zustand
    // stehen, ueber den die Lesekette dann wieder stillschweigend hinwegginge. Fuer den
    // gemeldeten Fall ist das Ergebnis dasselbe: Die beiden Zeilen haengen an DERSELBEN
    // Anlage, es bleibt Variante 10.
    //
    // ERGEBNISNEUTRAL. Behalten wird je Projekt die aktive Zeile mit der KLEINSTEN ID -
    // genau die, die ReadAktiveVariante schon bisher geliefert hat. Was der Schritt
    // abschaltet, hat noch nie gerechnet. Projekt 1026 ist ausserdem kein
    // Referenzprojekt; der Referenzlauf bleibt byte-gleich.
    //
    // KEIN INDEX. Ein eindeutiger Teilindex ueber (ID_Energieanlage) WHERE Aktiv = 1
    // waere die naheliegende Ergaenzung - und er wuerde SetzeAktiv brechen, sobald ein
    // Aufrufer die neue Zeile setzt, bevor die alte faellt. Die Reihenfolge der zwei
    // UPDATEs ist heute richtig; ein Index, der eine Reihenfolge erzwingt, waere eine
    // zweite Wahrheit ueber dieselbe Regel und gehoert nicht in einen Schritt, der
    // aufraeumen soll. Die Entdoppelung ist deshalb wiederholbar statt abgesichert.
    //
    // WARUM HIER. Dieselbe Begruendung wie bei GesetzesparameterEindeutig (87) und
    // ProjektEnergietraegerEindeutig (76): drei Leser - Schemaschritt, Werkzeug
    // Werkzeuge/Testdatenbankschema und Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Die Entdoppelung der aktiven Speichervarianten (Beifang des Schemaschritts 87) -
    /// EINE Quelle fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class SpeicherVarianteAktivEindeutig
    {
        /// <summary>Die Tabelle der Speichervarianten.</summary>
        public const string TABELLE = "Tab_StromspeicherVariante";

        /// <summary>Die Anlagentabelle - sie traegt den Projektbezug.</summary>
        public const string TABELLE_ANLAGEN = "Tab_Energieanlagen";

        /// <summary>Die Merkspalte „diese Variante rechnet".</summary>
        public const string SPALTE_AKTIV = "Aktiv";

        /// <summary>Der Anlagenverweis der Variante.</summary>
        public const string SPALTE_ANLAGE = "ID_Energieanlage";

        /// <summary>
        /// Die aktiven Zeilen, die bleiben: je Projekt die mit der kleinsten ID - genau
        /// die, die <c>ReadAktiveVariante</c> liefert.
        /// </summary>
        private const string BEHALTEN =
            "(SELECT MIN(\"v\".\"ID\") FROM \"" + TABELLE + "\" AS \"v\" " +
            "INNER JOIN \"" + TABELLE_ANLAGEN + "\" AS \"a\" ON \"a\".\"ID\" = \"v\".\"" +
            SPALTE_ANLAGE + "\" WHERE \"v\".\"" + SPALTE_AKTIV + "\" = 1 " +
            "GROUP BY \"a\".\"ID_Projekt\")";

        // =================================================================
        //  Die Anweisung
        // =================================================================

        /// <summary>
        /// Schaltet je Projekt alle aktiven Varianten ausser der mit der kleinsten ID ab.
        ///
        /// <para><b>Waisen bleiben unberuehrt.</b> Eine Variante ohne Anlagenzeile
        /// gehoert zu keinem Projekt; sie faellt nicht unter die Regel und wird deshalb
        /// auch nicht abgeschaltet (der Fremdschluessel mit <c>ON DELETE CASCADE</c>
        /// laesst sie ohnehin nicht entstehen).</para>
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet nichts mehr.</para>
        /// </summary>
        public const string SQL_ENTDOPPELN =
            "UPDATE \"" + TABELLE + "\" SET \"" + SPALTE_AKTIV + "\" = 0 " +
            "WHERE \"" + SPALTE_AKTIV + "\" = 1 AND \"" + SPALTE_ANLAGE +
            "\" IN (SELECT \"ID\" FROM \"" + TABELLE_ANLAGEN + "\") " +
            "AND \"ID\" NOT IN " + BEHALTEN;

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele aktive Zeilen zu viel stehen da? 0 = jedes Projekt fuehrt hoechstens
        /// eine aktive Variante. Gezaehlt wird die Summe der ueberzaehligen Zeilen je
        /// Projekt (<c>n - 1</c>) - genau so viele schaltet
        /// <see cref="SQL_ENTDOPPELN"/> ab.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COALESCE(SUM(\"n\" - 1), 0) FROM (" +
                   "SELECT COUNT(*) AS \"n\" FROM \"" + TABELLE + "\" AS \"v\" " +
                   "INNER JOIN \"" + TABELLE_ANLAGEN + "\" AS \"a\" ON \"a\".\"ID\" = \"v\".\"" +
                   SPALTE_ANLAGE + "\" WHERE \"v\".\"" + SPALTE_AKTIV + "\" = 1 " +
                   "GROUP BY \"a\".\"ID_Projekt\" HAVING COUNT(*) > 1) AS \"d\"";
        }

        /// <summary>
        /// Eine PROTOKOLLZEILE je Variante, die <see cref="SQL_ENTDOPPELN"/> abschalten
        /// wird - mit Projekt, Anlage und Varianten-ID. Abzufragen <b>vor</b> dem
        /// Schreiben; danach liefert dieselbe Anweisung nichts mehr.
        ///
        /// <para>Die Zeilen kommen als EIN Text zurueck (<c>group_concat</c> mit
        /// Zeilenumbruch), weil der SQLite-Zweig der Migration nur Skalare lesen kann;
        /// zerlegt wird er mit <c>PvKoeffizientenReparatur.Zerlege</c> - dasselbe Muster
        /// wie in Schritt 69.</para>
        /// </summary>
        public static string Protokollabfrage()
        {
            return "SELECT group_concat(\"z\", char(10)) FROM (" +
                   "SELECT '" + TABELLE + ": Projekt ' || \"a\".\"ID_Projekt\" || " +
                   "', Anlage ' || \"v\".\"" + SPALTE_ANLAGE + "\" || " +
                   "' - zweite aktive Variante abgeschaltet (ID ' || \"v\".\"ID\" || ').' AS \"z\" " +
                   "FROM \"" + TABELLE + "\" AS \"v\" INNER JOIN \"" + TABELLE_ANLAGEN +
                   "\" AS \"a\" ON \"a\".\"ID\" = \"v\".\"" + SPALTE_ANLAGE + "\" " +
                   "WHERE \"v\".\"" + SPALTE_AKTIV + "\" = 1 AND \"v\".\"ID\" NOT IN " +
                   BEHALTEN + " ORDER BY \"a\".\"ID_Projekt\", \"v\".\"ID\" LIMIT " +
                   PROTOKOLL_HOECHSTZAHL.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ")";
        }

        /// <summary>
        /// Wie viele Protokollzeilen hoechstens - dieselbe Schranke wie in Schritt 69 und
        /// bei <see cref="GesetzesparameterEindeutig.PROTOKOLL_HOECHSTZAHL"/>.
        /// </summary>
        public const int PROTOKOLL_HOECHSTZAHL = 200;

        /// <summary>
        /// Die eine Anweisung mit ihrem Namen - dieselbe Form wie bei den Nachbarn, damit
        /// Migration, Werkzeug und Nachweis sie gleich abarbeiten.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Entdoppelung aktive " + TABELLE, SQL_ENTDOPPELN);
            }
        }

        /// <summary>
        /// Stehen noch mehrfach aktive Varianten im Bestand? Eine nicht lesbare Zaehlung
        /// gilt als „nichts zu tun".
        /// </summary>
        public static bool EntdoppelungNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }
    }
}
