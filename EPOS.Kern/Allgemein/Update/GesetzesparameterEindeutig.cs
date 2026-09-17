using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // EINE ZEILE JE SCHLUESSEL, KLASSE UND STICHJAHR - Migrationsschritt 87
    // (Anwenderentscheid US-E-1 (a) vom 17.09.2026, Ausgangslage aus Auftrag #319).
    //
    // WOZU. Die Katalogsaat (GesetzKatalog.StelleKatalogSicher) laeuft generationsweise
    // bei JEDEM Programmstart: Sie liest den Marker KATALOG_GENERATION, saet alles mit
    // hoeherer Generation ein und hebt den Marker erst DANACH. Brach die Schleife
    // mittendrin ab - bis #319 tat das die Generation 7 am CHECK auf die Laenge von
    // Quelle -, blieb der Marker stehen, und die Zeilen VOR der Fehlstelle wurden beim
    // naechsten Start noch einmal angelegt. Jeder Start eine weitere Zeile, jede mit
    // neuer ID. Produktive Datenbanken, die zwischen dem 17.09.2026 und #319 gestartet
    // wurden, koennen STROMST_REDUZIERT_SATZ und UMLAGE_KWKG deshalb mehrfach fuehren.
    //
    // #319 HAT DIE URSACHE BEHOBEN, NICHT DIE FOLGE. Der abgebrochene Lauf schreibt
    // seither eine benannte Warnung (GesetzKatalog.SaatWarnungen) statt eines leeren
    // catch; die bereits entstandenen Dubletten raeumt erst dieser Schritt weg - und der
    // Index danach sorgt dafuer, dass kein Startlauf sie neu erzeugen kann.
    //
    // WARUM DAS NICHT NUR ORDNUNGSLIEBE IST. GesetzKatalog.Sicherstellen liest die
    // Tabelle mit ORDER BY Schluessel, JahrVon und baut daraus je Schluessel eine nach
    // Stichjahr sortierte REIHE; die Auskunft "welcher Satz gilt im Jahr J" nimmt daraus
    // die letzte Zeile mit JahrVon <= J. Stehen zwei Zeilen desselben Stichjahrs
    // nebeneinander, entscheidet die Speicherreihenfolge, welcher Satz gilt - und der
    // Anwender, der in der Pflegemaske die eine Zeile bearbeitet, bearbeitet vielleicht
    // die, die niemand liest.
    //
    // DIE SPALTENKOMBINATION IST DIE DES HAUSES, NICHT GERATEN. Die Pflegemaske prueft
    // seit jeher genau dieses Tripel (GesetzKatalog.Existiert: Klasse, Schluessel,
    // JahrVon) und weist eine zweite Zeile mit GESETZ_MSG_DOPPELT ab. Was die
    // Anwendungslogik dort haelt, haelt ab hier die Datenbank - dieselbe Bewegung wie in
    // Schritt 76 (ProjektEnergietraegerEindeutig). Der Bestand der Messlatte
    // Referenzlaeufe/Kenndaten_Test.sqlite (226 Zeilen) fuehrt zu keinem Tripel mehr als
    // eine Zeile: Sie hat nie ein Programm gestartet, also auch nie abgebrochen gesaet.
    //
    // DIE ID IST FREI. Tab_Gesetzesparameter traegt keinen Fremdschluessel, und keine
    // andere Tabelle verweist auf sie; die ID lebt nur waehrend einer Pflegesitzung
    // (GesetzZeile.Id -> Aendern/Loeschen). Kein gespeicherter Verweis haengt daran,
    // also darf jede ueberzaehlige Zeile fallen. Behalten wird je Tripel die KLEINSTE ID
    // - die zuerst gesaete und damit die, die jede Lesekette schon bisher genommen hat
    // (ORDER BY Schluessel, JahrVon ist innerhalb eines Tripels stabil ueber die
    // Einfuegereihenfolge). Der Schritt ist damit ERGEBNISNEUTRAL.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // ProjektEnergietraegerEindeutig (76): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden
    // sie abschreiben - drei Wahrheiten ueber denselben Index.
    //
    // NUR VOLLSTAENDIG BESETZTE TRIPEL. Entdoppelt wird ausschliesslich, was der Index
    // auch bindet: Zeilen mit Schluessel, Klasse UND JahrVon. SQLite haelt NULL in einem
    // eindeutigen Index nie gegen NULL; eine Zeile mit einer leeren Schluesselspalte
    // waere also weder Dublette noch vom Index betroffen, und sie zu loeschen hiesse,
    // mehr wegzuraeumen, als die Regel verlangt. Im Bestand gibt es keine solche Zeile.
    // ====================================================================================

    /// <summary>
    /// Der eindeutige Index ueber <c>Tab_Gesetzesparameter</c> (Schemaschritt 87) samt
    /// der Entdoppelung, die ihm vorausgeht - EINE Quelle fuer Migration, Werkzeug und
    /// Nachweis.
    /// </summary>
    public static class GesetzesparameterEindeutig
    {
        /// <summary>Die Tabelle, ueber der der Index steht.</summary>
        public const string TABELLE = "Tab_Gesetzesparameter";

        /// <summary>Erste Schluesselspalte: der Parametername.</summary>
        public const string SPALTE_SCHLUESSEL = "Schluessel";

        /// <summary>Zweite Schluesselspalte: die Klasse (KWKG, STROMSTEUER, ...).</summary>
        public const string SPALTE_KLASSE = "Klasse";

        /// <summary>Dritte Schluesselspalte: das Stichjahr, ab dem der Satz gilt.</summary>
        public const string SPALTE_JAHR = "JahrVon";

        /// <summary>Name des eindeutigen Index - Stil der Nachbarn (Schritt 76).</summary>
        public const string INDEX = "idx_Gesetzesparameter_Eindeutig";

        /// <summary>
        /// Die Bedingung, die genau die Zeilen trifft, die der Index bindet: alle drei
        /// Schluesselspalten besetzt (Klassenkopf, Abschnitt „NUR VOLLSTAENDIG BESETZTE
        /// TRIPEL").
        /// </summary>
        private const string VOLLSTAENDIG =
            "\"" + SPALTE_SCHLUESSEL + "\" IS NOT NULL AND \"" + SPALTE_KLASSE +
            "\" IS NOT NULL AND \"" + SPALTE_JAHR + "\" IS NOT NULL";

        /// <summary>Die Zeilen, die behalten werden - je Tripel die kleinste ID.</summary>
        private const string BEHALTEN =
            "SELECT MIN(\"ID\") FROM \"" + TABELLE + "\" WHERE " + VOLLSTAENDIG +
            " GROUP BY \"" + SPALTE_SCHLUESSEL + "\", \"" + SPALTE_KLASSE +
            "\", \"" + SPALTE_JAHR + "\"";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Entfernt je Tripel (Schluessel, Klasse, Stichjahr) alles ausser der Zeile mit
        /// der kleinsten <c>ID</c> - der zuerst gesaeten (Klassenkopf).
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet nichts mehr zu loeschen.</para>
        /// </summary>
        public const string SQL_ENTDOPPELN =
            "DELETE FROM \"" + TABELLE + "\" WHERE " + VOLLSTAENDIG +
            " AND \"ID\" NOT IN (" + BEHALTEN + ")";

        /// <summary>
        /// Der eindeutige Index ueber die drei Schluesselspalten. <c>IF NOT EXISTS</c>
        /// macht den Schritt wiederholbar; die Anlage scheitert, solange noch eine
        /// Dublette steht - deshalb laeuft <see cref="SQL_ENTDOPPELN"/> vorher.
        /// </summary>
        public const string SQL_INDEX =
            "CREATE UNIQUE INDEX IF NOT EXISTS \"" + INDEX + "\" ON \"" + TABELLE + "\" " +
            "(\"" + SPALTE_SCHLUESSEL + "\", \"" + SPALTE_KLASSE + "\", \"" +
            SPALTE_JAHR + "\")";

        /// <summary>
        /// Beide Anweisungen in Ausfuehrungsreihenfolge - so, wie Migration und Werkzeug
        /// sie abarbeiten: erst entdoppeln, dann den Index anlegen. Die Reihenfolge ist
        /// NICHT beliebig.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Entdoppelung " + TABELLE, SQL_ENTDOPPELN);
                yield return new KeyValuePair<string, string>(
                    "Index " + INDEX, SQL_INDEX);
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Zeilen zu viel stehen da? 0 = der Index laesst sich anlegen.
        ///
        /// <para>Gezaehlt wird die Summe der UEBERZAEHLIGEN Zeilen je Tripel (also
        /// <c>n - 1</c>), nicht die Zahl der Tripel: Genau so viele Zeilen entfernt
        /// <see cref="SQL_ENTDOPPELN"/>.</para>
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COALESCE(SUM(\"n\" - 1), 0) FROM (" +
                   "SELECT COUNT(*) AS \"n\" FROM \"" + TABELLE + "\" WHERE " + VOLLSTAENDIG +
                   " GROUP BY \"" + SPALTE_SCHLUESSEL + "\", \"" + SPALTE_KLASSE +
                   "\", \"" + SPALTE_JAHR + "\" HAVING COUNT(*) > 1) AS \"d\"";
        }

        /// <summary>
        /// Eine PROTOKOLLZEILE je Zeile, die <see cref="SQL_ENTDOPPELN"/> entfernen wird -
        /// mit Schluessel, Klasse, Stichjahr und der entfernten ID. Abzufragen <b>vor</b>
        /// der Entdoppelung; danach liefert dieselbe Anweisung nichts mehr.
        ///
        /// <para><b>Wozu.</b> Eine geloeschte Katalogzeile ist nicht wiederzubeschaffen,
        /// und vom Schemalauf sieht der Anwender nur das Protokoll. Der Schritt schreibt
        /// deshalb JEDE entfernte Zeile hinein - nicht nur ihre Zahl.</para>
        ///
        /// <para>Die Zeilen kommen als EIN Text zurueck (<c>group_concat</c> mit
        /// Zeilenumbruch), weil der SQLite-Zweig der Migration nur Skalare lesen kann -
        /// dasselbe Muster wie <c>PvKoeffizientenReparatur.ProtokollAbfrage</c> (69);
        /// zerlegt wird er mit <c>PvKoeffizientenReparatur.Zerlege</c>. Ein Schluessel
        /// enthaelt keinen Zeilenumbruch, die Spalte ist einzeilig.</para>
        /// </summary>
        public static string Protokollabfrage()
        {
            return "SELECT group_concat(\"z\", char(10)) FROM (" +
                   "SELECT '" + TABELLE + ": Schluessel \"' || \"" + SPALTE_SCHLUESSEL +
                   "\" || '\", Klasse \"' || \"" + SPALTE_KLASSE + "\" || '\", ab ' || \"" +
                   SPALTE_JAHR + "\" || ' - Dublette entfernt (ID ' || \"ID\" || ').' AS \"z\" " +
                   "FROM \"" + TABELLE + "\" WHERE " + VOLLSTAENDIG +
                   " AND \"ID\" NOT IN (" + BEHALTEN + ") ORDER BY \"" +
                   SPALTE_SCHLUESSEL + "\", \"" + SPALTE_KLASSE + "\", \"" +
                   SPALTE_JAHR + "\", \"ID\" LIMIT " +
                   PROTOKOLL_HOECHSTZAHL.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ")";
        }

        /// <summary>
        /// Wie viele Protokollzeilen hoechstens - dieselbe Schranke wie in Schritt 69
        /// (<c>PvKoeffizientenReparatur.PROTOKOLL_HOECHSTZAHL</c>): Ein Protokoll, das
        /// niemand mehr liest, hilft niemandem.
        /// </summary>
        public const int PROTOKOLL_HOECHSTZAHL = 200;

        /// <summary>
        /// Steht der eindeutige Index? 1 = ja. Gefragt wird <c>sqlite_master</c> nach dem
        /// Namen - dieselbe Frage, die Migrationsbericht, Werkzeug und Nachweis stellen.
        /// </summary>
        public static string ZaehlungIndex()
        {
            return "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = '" +
                   INDEX + "'";
        }

        /// <summary>
        /// Stehen noch Dubletten im Bestand? Die Auskunft geht ueber
        /// <see cref="Zaehlung"/>; eine nicht lesbare Zaehlung gilt als „nichts zu tun".
        /// </summary>
        public static bool EntdoppelungNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }
    }
}
