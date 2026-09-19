using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE VERGUETUNG JE VARIANTE - Migrationsschritt 93 (Konzept Wirtschaftlichkeit 2.16)
    //
    // WOZU. Die PV-Verguetungsangaben (Vermarktungsform, anzulegender Wert, Einspeiseart,
    // Inbetriebnahme, Paragraf 51/51a, 60-%-Begrenzung) stehen in Tab_ProjektPhotovoltaik,
    // eine Zeile je Projekt. Der Rechenweg liest die Zeile des JEWEILIGEN Projekts - eine
    // Variante bekam die Angaben also nur, wenn sie eine eigene Zeile hatte, und die hatte
    // sie genau dann, wenn sie NACH der Pflege des Stamms angelegt wurde (der Kopierlauf
    // nahm die Zeile mit). Wer von aussen hinsah, konnte das nicht erkennen: Reiter,
    // Dialog und Bericht sagten "stammprojektbezogen". Der Anwender verlangt beides -
    // "Die Option der Uebernahme soll es geben, aber eigene Verguetung in den Varianten
    // muss moeglich sein" (18.09.2026).
    //
    // WAS DER SCHRITT TUT. Er legt die Spalte Uebernahme_Stamm an
    // (SchemaKatalog.Schritt93_VerguetungJeVariante, nullbares 0/1) und leitet die Wahl
    // aus dem Bestand ab. Danach beantwortet die Datenbank je Stand, was vorher die
    // Anlegereihenfolge entschied.
    //
    // DIE ABLEITUNG, UND WARUM SIE ERGEBNISNEUTRAL IST (Anwenderentscheid VV-Q4). Ein
    // Schemaschritt, der Zahlen aendert, ohne dass jemand gewaehlt hat, verletzte die
    // Regel "die Vorgabe ist die, die nichts aendert". Deshalb zwei DML:
    //
    //   1. Jede VORHANDENE Zeile bekommt Uebernahme_Stamm = 0 - "eigene Werte". Der
    //      Stamm fuehrt ohnehin immer eigene; eine Variante mit Zeile hat mit genau
    //      diesen Werten gerechnet und rechnet weiter damit.
    //   2. Eine Variante OHNE Zeile, deren Stamm eine AKTIVE Zeile fuehrt, bekommt eine
    //      eigene, INAKTIVE Zeile (Aktiv = 0, Uebernahme_Stamm = 0). Sie rechnete bisher
    //      den Flat-Pfad (RechnePvVerguetung steigt ohne aktive Zeile aus) und tut es
    //      weiter. Wuerde sie stattdessen auf "uebernehmen" gesetzt, uebernaehme sie ab
    //      dem naechsten Lauf die Erloesreihe des Stamms - eine Zahlenaenderung ohne
    //      Wahl. Die Spur dieser Ableitung ist der Kohaerenzhinweis
    //      (KohaerenzPruefung.PvVerguetungHerkunft): "fuehrt eine eigene, inaktive
    //      PV-Verguetung, waehrend der Stamm eine aktive fuehrt".
    //
    // Eine Variante ohne Zeile, deren Stamm auch keine aktive fuehrt, bleibt ohne Zeile -
    // und keine Zeile heisst "uebernehmen". Beide Seiten rechnen Flat, wie bisher.
    //
    // DIE TESTDATENBANK fuehrt keine einzige Zeile in Tab_ProjektPhotovoltaik. Beide DML
    // fassen dort nichts an; die dreizehn Referenzprojekte bleiben byte-gleich.
    //
    // KEIN TABELLENNEUBAU. Die Spalte kommt per ALTER TABLE ... ADD COLUMN; SQLite laesst
    // eine CHECK-Bedingung dabei zu (sie prueft bestehende Zeilen mit NULL, und NULL ist
    // nicht falsch). Die LOESCHWEITERGABE Tab_Projekt -> Tab_ProjektPhotovoltaik, die das
    // Konzept in derselben Etappe verlangt, ist KEIN Schemateil: SQLite kann einer
    // bestehenden Tabelle keinen Fremdschluessel anhaengen, und ein Tabellenneubau fuer
    // eine Beziehung, die ProjektCtrl.Delete ohnehin auf dem einen Weg bedient, durch den
    // jedes Projektloeschen laeuft, waere eine zweite Wahrheit. Sie steht deshalb dort -
    // ProjektCtrl.PvVerguetungAufloesen, gemeinsam mit dem Randfall "Loeschen des Stamms",
    // den ein Fremdschluessel gar nicht bedienen koennte (er muesste den uebernehmenden
    // Varianten vorher die Stammwerte geben).
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KwkgProjektaltspalten: Die Anweisungen brauchen drei Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Die Ableitung der Vergütungswahl aus dem Bestand — EINE Quelle für Migration,
    /// Werkzeug und Nachweis (Schemaschritt 93, Konzept § 2.16).
    /// </summary>
    public static class PvVerguetungJeVariante
    {
        /// <summary>Die PV-Vergütungszeilen der Projekte.</summary>
        public const string TABELLE = SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK;

        /// <summary>Die Wahl je Zeile: 1 = übernommen, 0 = eigene Werte; NULL an einer
        /// vorhandenen Zeile liest sich wie 0 (Bestand), keine Zeile wie 1.</summary>
        public const string SPALTE = SchemaKatalog.SPALTE_PPV_UEBERNAHME_STAMM;

        /// <summary>Die Variantenverknüpfung (<c>ID_Projekt</c> → <c>ID_ProjektRef</c> = Stamm).</summary>
        public const string TAB_VARIANTE = SchemaKatalog.TAB_VARIANTE;

        // =================================================================
        //  Die zwei Anweisungen
        // =================================================================

        /// <summary>
        /// DML 1: Jede vorhandene Zeile ist eine Zeile mit EIGENEN Werten. Nur Zeilen
        /// ohne Wahl werden angefasst — der Schritt ist damit wiederholbar und
        /// überschreibt keine später getroffene Wahl.
        /// </summary>
        public const string SQL_BESTAND_EIGENE =
            "UPDATE [" + TABELLE + "] SET [" + SPALTE + "] = 0 WHERE [" + SPALTE + "] IS NULL";

        /// <summary>
        /// DML 2: Varianten ohne eigene Zeile, deren Stamm eine AKTIVE Zeile führt,
        /// bekommen eine eigene, INAKTIVE Zeile — ergebnisneutral (VV‑Q4).
        ///
        /// <para><b>Die ID kommt von SQLite.</b> <c>ID</c> ist ein
        /// <c>INTEGER PRIMARY KEY</c>, also der Rowid-Alias: Ein INSERT ohne ID vergibt
        /// <c>MAX(rowid) + 1</c> — genau das MAX+1-Hausmuster, nur mengenweise statt
        /// zeilenweise. Die drei NOT-NULL-Spalten der Tabelle (<c>Aktiv</c>,
        /// <c>Par51a_Kompensieren</c>, <c>BezugAusPreisreihe</c>) tragen ihre
        /// DDL-Vorgabe 0; <c>Aktiv</c> steht trotzdem ausgeschrieben da, weil genau sie
        /// die Ergebnisneutralität trägt.</para>
        /// </summary>
        public const string SQL_INAKTIVE_SPUR =
            "INSERT INTO [" + TABELLE + "] (ID_Projekt, Aktiv, [" + SPALTE + "]) " +
            "SELECT v.ID_Projekt, 0, 0 FROM [" + TAB_VARIANTE + "] v " +
            "JOIN [" + TABELLE + "] s ON s.ID_Projekt = v.ID_ProjektRef " +
            "WHERE s.Aktiv = 1 AND NOT EXISTS (SELECT 1 FROM [" + TABELLE + "] p " +
            "WHERE p.ID_Projekt = v.ID_Projekt)";

        /// <summary>
        /// Die Anweisungen in fester Reihenfolge — Beschreibung und SQL. Migration und
        /// Werkzeug arbeiten sie gleich ab.
        ///
        /// <para><b>Die Reihenfolge ist Pflicht:</b> DML 2 legt Zeilen MIT Wahl an; liefe
        /// DML 1 danach, fände es sie schon gesetzt vor und ließe sie in Ruhe — das
        /// Ergebnis wäre dasselbe, die Zählung aber irreführend.</para>
        ///
        /// <para><b>Wiederholbar:</b> Ein zweiter Lauf findet keine Zeile ohne Wahl
        /// (<see cref="OhneWahl"/> = 0) und keine Variante ohne Zeile bei aktivem Stamm
        /// (<see cref="OhneZeileBeiAktivemStamm"/> = 0) und fasst nichts an.</para>
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "vorhandene Zeilen als eigene Werte kennzeichnen", SQL_BESTAND_EIGENE);
                yield return new KeyValuePair<string, string>(
                    "inaktive Spur fuer Varianten ohne Zeile bei aktivem Stamm", SQL_INAKTIVE_SPUR);
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Steht die Spalte schon?</summary>
        public static bool SpalteVorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>Zeilen ohne Wahl — so viele kennzeichnet DML 1. 0 = gelaufen.</summary>
        public static int OhneWahl()
        {
            if (!SpalteVorhanden()) return 0;
            return Anzahl("SELECT COUNT(*) FROM [" + TABELLE + "] WHERE [" + SPALTE + "] IS NULL");
        }

        /// <summary>
        /// Varianten ohne eigene Zeile, deren Stamm eine aktive führt — so viele Zeilen
        /// legt DML 2 an. 0 = gelaufen oder nichts zu tun.
        /// </summary>
        public static int OhneZeileBeiAktivemStamm()
        {
            return Anzahl(
                "SELECT COUNT(*) FROM [" + TAB_VARIANTE + "] v " +
                "JOIN [" + TABELLE + "] s ON s.ID_Projekt = v.ID_ProjektRef " +
                "WHERE s.Aktiv = 1 AND NOT EXISTS (SELECT 1 FROM [" + TABELLE + "] p " +
                "WHERE p.ID_Projekt = v.ID_Projekt)");
        }

        /// <summary>Eine Zählabfrage; 0, wenn sie nicht läuft (fehlende Tabelle im
        /// Altbestand) — dann fasst der Schritt auch nichts an.</summary>
        private static int Anzahl(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == System.DBNull.Value ? 0 : System.Convert.ToInt32(o);
            }
            catch { return 0; }
        }
    }
}
