using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // =====================================================================================
    // ARBEITSPAKET iU6-T4 (Umsetzungskonzept iOS § 1.4, Entscheidung iF10, Weg (b)).
    //
    // DIE NAHT, DIE DER KERN FUER iOS BRAUCHT. Bis hierher war der Datenzugriff eine
    // statische Klasse mit fest eingebauter SQLite-Umsetzung. Das traegt auf Windows und
    // auf Linux, aber es laesst sich weder austauschen noch pruefen, ohne die Datei
    // anzufassen. IDatenzugriff beschreibt, WAS der Kern von einer Datenhaltung braucht -
    // DataRepository bleibt die Fassade davor, SqliteDatenzugriff ist die eine Umsetzung.
    //
    // DER VERTRAG IST EIN ABBILD DES BESTANDS, KEIN ENTWURF AUF DER GRUENEN WIESE. Die
    // Vermessung vom 03.09.2026 hat die tatsaechlich genutzte Flaeche gezaehlt: sechs
    // Ausfuehrungs- und fuenf Schemamethoden, dazu die Startpruefung und der Pfad. Genau
    // das steht hier - nichts vorsorglich dazu, nichts weggelassen.
    //
    // WAS AUSDRUECKLICH NICHT HIER STEHT:
    //   - der Engine-Modus (FehlerMelden, EngineModus, StilleFehlerAbholen). Er ist eine
    //     MELDEENTSCHEIDUNG des Programms ("Dialog oder Protokoll"), nicht Sache eines
    //     Datenanbieters; er bleibt auf der Fassade, damit es bei EINER Entscheidung
    //     bleibt (Konzept 13.4, Paket 8).
    //   - die vier Bequemlichkeiten GetMaxID, DeleteWithDependencies, GetIdByName und
    //     GetValueById. Sie setzen nur SQL zusammen und rufen den Vertrag - jede Umsetzung
    //     wuerde sie identisch abschreiben.
    //   - PfadUeberschreibung und GetDBPath. Die Pfadaufloesung liest Einstellungen und
    //     kennt den Proben-/Referenzlauf-Haken; sie bekommt in iU5 ihr eigenes IPfade.
    //     Der Vertrag fragt nur nach dem ERGEBNIS (DatenbankPfad).
    //   - alles mit SQLite-Typen in der Signatur (OeffneVerbindung, ErzeugeKommando,
    //     LadeTabelle). Das ist Innenleben der Umsetzung; es steht dort und ist ueber die
    //     Fassade erreichbar, solange DbVorgang, RecordSet und StilleDb es brauchen.
    //
    // NUR EIN DIALEKT. Es wird keinen Parallelbetrieb "Access | SQLite" geben - das
    // SQLite-Konzept haelt in § 9 fest, dass der Providerbruch diese Weiche im selben
    // Build verhindert (Umsetzungskonzept § 1.5, Praezisierung zu iL2). IDatenzugriff
    // traegt deshalb genau EINEN Dialekt; die Schnittstelle ist die Naht fuer die
    // PLATTFORM, nicht fuer zwei Datenbanken.
    // =====================================================================================

    /// <summary>
    /// Der Datenzugriff, wie der Rechenkern ihn braucht - providerfrei und ohne
    /// Plattformbezug. Parameter sind ausnahmslos <see cref="DbParam"/>; gebunden wird
    /// nach POSITION, die Namen sind Lesehilfe.
    /// </summary>
    public interface IDatenzugriff
    {
        // ---------------------------------------------------------------- Ausfuehrung

        /// <summary>SELECT in den Arbeitsspeicher. Im Fehlerfall eine LEERE Tabelle.</summary>
        DataTable GetDataTable(string sql, params DbParam[] parameter);

        /// <summary>INSERT/UPDATE/DELETE. true bei Erfolg, false im Fehlerfall.</summary>
        bool ExecuteSQL(string sql, params DbParam[] parameter);

        /// <summary>Wie <see cref="ExecuteSQL"/>, liefert die Zahl der betroffenen
        /// Zeilen; -1 unterscheidet den Fehler von "0 Zeilen".</summary>
        int ExecuteNonQuery(string sql, params DbParam[] parameter);

        /// <summary>INSERT samt Rueckgabe der erzeugten ID; 0 im Fehlerfall. Signatur
        /// bewusst OHNE <c>params</c> - sieben Aufrufstellen verlassen sich darauf.</summary>
        int ExecuteInsertAndGetId(string insertSql, DbParam[] parameter);

        /// <summary>Einzelwert; <c>null</c> bei DBNull und im Fehlerfall.</summary>
        object ExecuteScalar(string sql, params DbParam[] parameter);

        /// <summary>Oeffnet einen Datenbankvorgang (Verbindung + Transaktion) fuer einen
        /// <c>using</c>-Block. Der EINZIGE Weg in eine Transaktion.</summary>
        DbVorgang Vorgang();

        // -------------------------------------------------------------- Schemaauskunft

        /// <summary>Gibt es eine Tabelle (oder Sicht) dieses Namens?</summary>
        bool TabelleVorhanden(string name);

        /// <summary>Gibt es diese Spalte in dieser Tabelle? (ohne Gross-/Kleinschreibung)</summary>
        bool SpalteVorhanden(string tabelle, string spalte);

        /// <summary>Spaltennamen in Schemareihenfolge; leer, wenn es die Tabelle nicht gibt.</summary>
        List<string> SpaltenVonTabelle(string tabelle);

        /// <summary>Indizes einer Tabelle, eine Zeile je Index-Spalte.</summary>
        DataTable IndexListe(string tabelle);

        /// <summary>Fremdschluessel einer Tabelle.</summary>
        DataTable FremdschluesselListe(string tabelle);

        // ------------------------------------------------------------------- Umgebung

        /// <summary>Ist die Datenbank vorhanden und lesbar? (Startpruefung, Konzept 2.8)</summary>
        bool DatenbankVorhanden();

        /// <summary>Der Pfad zur Datei, auf die dieser Zugriff arbeitet - fuer
        /// Diagnose und Protokoll.</summary>
        string DatenbankPfad { get; }
    }
}
