using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE PROJEKTKOPIE EINER WAERMEPUMPE KENNT IHREN KATALOGSATZ
    // - Migrationsschritt 80 (Auftrag #299, Anwenderentscheid vom 16.09.2026).
    //
    // WOZU. Tab_WP (die Projektkopie) fuehrte ID_Projekt, aber keinen Verweis auf den
    // Katalogsatz in Tab_WP_STAMM. Die einzige Klammer zwischen beiden war der
    // BEZEICHNER - ein Textfeld, das in keiner der zwei Tabellen eindeutig ist:
    //
    //   WPCtrl.CopyFromStamm            "gibt es die Kopie schon?" per Name
    //   WPCtrl.GetProjektId             Projektkopie per Name
    //   WPCtrl.KennlinienAusKatalog     Katalogsatz per Name (GetIdByName)
    //   WPStammCtrl.UebernahmeVorschau  Katalogsatz per Name
    //   WPStammCtrl.UebernehmenAusProjekt  Katalogsatz per Name; bei einer Dublette
    //                                   lehnt sie ab, bei einer UMBENENNUNG legt sie
    //                                   einen zweiten Katalogsatz an.
    //
    // Wer einen Katalogsatz umbenennt, zerreisst damit die Klammer zu jeder Projektkopie,
    // die ihn benutzt - und merkt es erst daran, dass "In Stamm uebernehmen" einen NEUEN
    // Satz anlegt statt den vorhandenen zu pflegen. Die Hausregel sagt dazu: "Neue
    // Beziehungen ueber IDs, nicht ueber Textfelder" (CLAUDE.md, Datenhaltung).
    //
    // DIE SPALTE IST NULLBAR, UND ZWAR DAUERHAFT. NULL heisst "diese Kopie haengt an
    // keinem Katalogsatz" - das ist ein gueltiger Zustand: ein von Hand angelegtes Geraet,
    // ein Katalogsatz, den jemand geloescht hat, und jede Kopie, deren Name beim
    // Nachtragen MEHRDEUTIG war. Der Rueckfall auf den Namen bleibt deshalb ueberall
    // stehen, wo die Spalte NULL ist.
    //
    // DER NACHTRAG RAET NICHT. Gefuellt wird nur, wo der Bezeichner GENAU EINEN
    // Katalogsatz trifft. Zwei Treffer (Dublette) und kein Treffer ergeben NULL - eine
    // erfundene Zuordnung waere schlimmer als keine, weil die Uebernahme danach den
    // falschen Katalogsatz ueberschriebe.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest Tab_WP.ID_Stamm; die Simulation holt aus
    // Tab_WP die Kennwerte (Nennleistung, Heizung, Typ) und aus Tab_Kenndaten die
    // Stuetzstellen. Der Referenzlauf bleibt byte-gleich.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION: dieselbe Begruendung wie bei
    // HeizstabJeWaermepumpe (79) und den Schritten davor - drei Leser, eine Quelle.
    // ====================================================================================

    /// <summary>
    /// Der Katalogverweis der Waermepumpen-Projektkopie (Schemaschritt 80) - EINE Quelle
    /// fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class WaermepumpeKatalogverweis
    {
        /// <summary>Die Projektkopie.</summary>
        public const string TABELLE = "Tab_WP";

        /// <summary>Der Katalog.</summary>
        public const string TABELLE_STAMM = "Tab_WP_STAMM";

        /// <summary>Die neue Verweisspalte.</summary>
        public const string SPALTE = "ID_Stamm";

        /// <summary>Der Suchweg jedes Lesers: die Projektkopien EINES Katalogsatzes.</summary>
        public const string INDEX = "Tab_WP_ID_Stamm";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Alles hinter dem Spaltennamen eines <c>ADD COLUMN</c> - dieselbe Bauart wie
        /// <c>NutzungsdauerSchema.TYP_VERWEIS</c>.
        ///
        /// <para><b>Mit <c>REFERENCES</c>, ohne <c>DEFAULT</c>.</b> SQLite laesst ein
        /// nachtraegliches <c>ADD COLUMN</c> mit Fremdschluessel genau dann zu, wenn der
        /// Vorgabewert NULL ist - und NULL ist hier ohnehin die Aussage "an keinem
        /// Katalogsatz".</para>
        ///
        /// <para><b><c>ON DELETE SET NULL</c>, nicht CASCADE.</b> Ein geloeschter
        /// Katalogsatz nimmt die Projektkopie NICHT mit: Sie ist das Geraet DIESES
        /// Projekts und rechnet weiter; nur ihre Klammer zum Katalog ist weg, und genau
        /// das sagt NULL. Der Bestand kennt beide Schreibweisen - die Geraeteverweise der
        /// Anlagenzeile kaskadieren, die Pufferverweise stehen ohne Aktion; hier ist die
        /// benannte Aktion die richtige, weil "keiner mehr" ein gueltiger Zustand der
        /// Spalte ist.</para>
        /// </summary>
        public const string TYP_SPALTE =
            "INTEGER REFERENCES \"" + TABELLE_STAMM + "\" (\"ID\") ON DELETE SET NULL";

        /// <summary>Die Spalte - <c>ALTER TABLE ... ADD COLUMN</c>.</summary>
        public const string SQL_SPALTE =
            "ALTER TABLE \"" + TABELLE + "\" ADD COLUMN \"" + SPALTE + "\" " + TYP_SPALTE;

        /// <summary>Der Index ueber den Katalogverweis; <c>IF NOT EXISTS</c> macht ihn wiederholbar.</summary>
        public const string SQL_INDEX =
            "CREATE INDEX IF NOT EXISTS \"" + INDEX + "\" ON \"" + TABELLE + "\" (\"" + SPALTE + "\")";

        // =================================================================
        //  Die Anweisungen
        //
        // DIE UNTERABFRAGE "wie viele Katalogsaetze heissen so?" STEHT DREIMAL
        // WOERTLICH DA - mit Absicht, und nicht als herausgezogene Konstante oder
        // Hilfsmethode. Sie ist ein SATZTEIL, kein Satz: Fuer sich genommen hat sie
        // keinen Bezug auf "Tab_WP" und besteht deshalb weder eine Syntax- noch eine
        // Objektpruefung. Werkzeuge/SqlDialektPruefer zieht aber jeden Text, den er
        // findet, und haelt ihn mit EXPLAIN gegen die Testdatenbank. Ein Satzteil als
        // eigene Vereinbarung waere dort eine dauerhafte Fundstelle - eine, die nichts
        // ueber den Bestand aussagt, sondern nur ueber die Zerlegung. Drei vollstaendige
        // Anweisungen sind das kleinere Uebel als eine unpruefbare vierte.
        // =================================================================

        /// <summary>
        /// Traegt den Verweis nach, wo der Bezeichner EINDEUTIG trifft.
        ///
        /// <para><b>Der Nachtrag raet nicht.</b> Zwei Treffer (Dublette) und kein Treffer
        /// ergeben NULL - eine erfundene Zuordnung waere schlimmer als keine, weil die
        /// Uebernahme danach den falschen Katalogsatz ueberschriebe.</para>
        ///
        /// <para><b>Wiederholbar</b> ueber <c>ID_Stamm IS NULL</c>: Ein zweiter Lauf fasst
        /// nur an, was noch keinen Verweis hat - eine von Hand geaenderte Zuordnung bleibt
        /// damit stehen.</para>
        /// </summary>
        public static string SqlNachtrag()
        {
            return "UPDATE \"" + TABELLE + "\" " +
                   "SET \"" + SPALTE + "\" = (SELECT s.\"ID\" FROM \"" + TABELLE_STAMM + "\" s " +
                   "WHERE s.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") = 1";
        }

        /// <summary>
        /// Derselbe Nachtrag fuer die Waermepumpen EINES Projekts (Parameter:
        /// <c>ID_Projekt</c>) - der Weg des Projekttransfers: Der Katalogverweis reist nicht
        /// ueber Paketgrenzen (die Id eines fremden Katalogs zeigt am Ziel auf nichts oder
        /// auf ein anderes Geraet); das importierte Projekt findet seinen Katalogsatz am Ziel
        /// nach derselben Regel wie der Schritt - genau ein Satz dieses Bezeichners, sonst
        /// bleibt der Verweis NULL.
        /// </summary>
        public static string SqlNachtragProjekt()
        {
            return "UPDATE \"" + TABELLE + "\" " +
                   "SET \"" + SPALTE + "\" = (SELECT s.\"ID\" FROM \"" + TABELLE_STAMM + "\" s " +
                   "WHERE s.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") " +
                   "WHERE \"ID_Projekt\" = ? AND \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") = 1";
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Projektkopien bekommt <see cref="SqlNachtrag"/> noch? 0 = nichts zu
        /// tun.
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") = 1";
        }

        /// <summary>
        /// Wie viele Projektkopien bleiben OHNE Verweis - kein Katalogsatz oder ein
        /// mehrdeutiger Name? Die Zahl gehoert in den Bericht: Genau diese Kopien gehen
        /// weiter ueber den Namen, und genau dort kann eine Umbenennung noch treffen.
        /// </summary>
        public static string ZaehlungOhneVerweis()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Bezeichner\") <> 1";
        }

        /// <summary>Gibt es die Spalte schon?</summary>
        public static bool SpalteVorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>
        /// Ist noch etwas nachzutragen? Ohne die Spalte ist die Antwort "ja" - dann fehlt
        /// alles. Eine nicht lesbare Zaehlung gilt wie ueberall als "nichts zu tun".
        /// </summary>
        public static bool NachtragNoetig()
        {
            if (!SpalteVorhanden()) return true;

            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }
    }
}
