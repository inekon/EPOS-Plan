using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE NULLZEILEN DER DREI ERFASSUNGSGRUPPEN FALLEN WEG
    // - Migrationsschritt 90, DML-Teil (Anwenderentscheid K-WZ-1 (a)).
    //
    // DER BEFUND. Die Kostenseite haengt Positionen ohne Anlagenbezug unten an die Liste.
    // Anlagenfaehige Gruppen erscheinen dort gelb und mit Papierkorb; die drei NICHT
    // anlagenfaehigen Erfassungsgruppen - Waermezentrale, Bauliche Anlagen,
    // Stromeinspeisung (KostenVorlagenCtrl.IstWaehlbar == false) - standen als gewoehnliche
    // Zeile unter der Ueberschrift "Anlagenkomponenten", ohne Kennzeichnung und ohne
    // Papierkorb. Der Anwender sah dort eine Zeile "Waermezentrale 0,00", die er weder
    // einordnen noch entfernen konnte.
    //
    // WOHER DIE ZEILE KOMMT. Es sind ALT-HAUPTKOMPONENTENZEILEN der frueheren Kostenmaske:
    // Tab_ProjektWerte-Zeilen, deren StammID auf eine Hauptkomponente zeigt
    // (Tab_Kostenfaktor.IsMainComponent = 1), Gruppe "Allgemein", Wert 0,00. Kein heutiger
    // Rechenweg legt sie an - KostenPositionCtrl schreibt nur bei eingegebenem Wert.
    //
    // WAS DER SCHRITT ENTFERNT, und was nicht. Entfernt wird ausschliesslich eine Zeile,
    // die ALLE drei Bedingungen erfuellt: Ihre Komponente ist eine der drei
    // Erfassungsgruppen, ihre StammID ist eine Hauptkomponente, und sie traegt in KEINEM
    // ihrer fuenf Wertfelder etwas (EingegebenerWert, Worstcase, Bestcase, Menge,
    // Einheitpreis). Dazu kommt eine vierte, staerkere Bedingung: Traegt IRGENDEINE
    // Position derselben Gruppe (Projekt, Kategorie, Komponente) einen Wert, bleibt die
    // Gruppe VOLLSTAENDIG stehen - samt ihrer Nullzeilen. Eine Gruppe, in der der Anwender
    // etwas erfasst hat, wird nicht angetastet; dort ist die Hauptkomponentenzeile die
    // Ueberschrift ihrer Positionen.
    //
    // ERGEBNISNEUTRAL. Jede entfernte Zeile traegt 0,00 in jedem Wertfeld; die Summe je
    // Projekt und Kategorie bleibt damit gleich, und die Wirtschaftlichkeit der betroffenen
    // Projekte aendert sich nicht. Die Referenzbasis fuehrt keine Kostengroesse - der
    // Referenzlauf bleibt byte-gleich.
    //
    // IDEMPOTENT. Der zweite Lauf findet keine Zeile mehr (Offen() = 0) und fasst nichts
    // an. Gezaehlt wird vor dem Loeschen, damit das Protokoll sagt, was geschehen ist.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Quelle-drei-Leser-Regel wie bei
    // KwkgProjektaltspalten und StrompreisAltspalten: Schemaschritt (Access-Zweig),
    // Werkzeuge/Testdatenbankschema und der Nachweis in EPOS.Kern.Tests fahren DIESELBEN
    // Anweisungen.
    // ====================================================================================

    /// <summary>
    /// Die Nullzeilen der drei Erfassungsgruppen (Schemaschritt 90, DML-Teil) - EINE
    /// Quelle fuer Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class KostenErfassungsgruppenAltzeilen
    {
        /// <summary>Die Projektwerte - dort stehen die Zeilen.</summary>
        public const string TABELLE = SchemaKatalog.TAB_PROJEKTWERTE;

        /// <summary>Der Komponentenkatalog - er loest den Namen der Erfassungsgruppe in
        /// ihre <c>ID</c> auf.</summary>
        public const string TABELLE_KOMPONENTE = SchemaKatalog.TAB_KOSTENKOMPONENTE;

        /// <summary>Die Kostenfaktoren - dort steht, ob eine <c>StammID</c> eine
        /// Hauptkomponente ist.</summary>
        public const string TABELLE_FAKTOR = "Tab_Kostenfaktor";

        /// <summary>Die Spalte, die eine Hauptkomponente kennzeichnet.</summary>
        public const string SPALTE_HAUPTKOMPONENTE = "IsMainComponent";

        /// <summary>
        /// Die drei nicht anlagenfaehigen Erfassungsgruppen, in der Reihenfolge des
        /// Katalogs. Dieselben Namen wie in
        /// <see cref="KostenVorlagenCtrl.WaehlbareKomponenten"/> NICHT enthalten sind -
        /// die Steuerwerte stehen einmal in <see cref="DbWerte"/>.
        /// </summary>
        public static IEnumerable<string> Erfassungsgruppen
        {
            get
            {
                yield return DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE;
                yield return DbWerte.KOSTEN_KOMPONENTE_BAULICHE_ANLAGEN;
                yield return DbWerte.KOSTEN_KOMPONENTE_STROMEINSPEISUNG;
            }
        }

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Die eine Anweisung des Schrittes - Beschreibung und SQL. Sie ist leer, wenn
        /// keine Zeile zutrifft (<see cref="Offen"/> = 0) oder eine der drei Tabellen
        /// fehlt; der zweite Lauf fasst dadurch nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                if (!Vorhanden()) yield break;
                if (Offen() <= 0) yield break;
                yield return new KeyValuePair<string, string>(
                    "Nullzeilen der drei Erfassungsgruppen entfernen", SqlLoeschen());
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Stehen die drei Tabellen samt der gelesenen Spalte? Ohne sie tut der
        /// Schritt nichts - dieselbe tolerante Haltung wie bei jedem DML-Schritt.</summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, "KomponentenID")
                && DataRepository.SpalteVorhanden(TABELLE_KOMPONENTE, SchemaKatalog.SPALTE_KK_KOMPONENTE)
                && DataRepository.SpalteVorhanden(TABELLE_FAKTOR, SPALTE_HAUPTKOMPONENTE);
        }

        /// <summary>
        /// Wie viele Zeilen trifft der Schritt noch? Genau so viele entfernt er.
        /// 0 = nichts zu tun, er ist gelaufen.
        /// </summary>
        public static int Offen()
        {
            try
            {
                object o = DataRepository.ExecuteScalar(SqlZaehlen());
                if (o == null || o == System.DBNull.Value) return 0;
                return System.Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>Wie viele Zeilen trifft der Schritt in EINEM Projekt? Fuer das
        /// Protokoll und den Nachweis.</summary>
        public static int OffenJeProjekt(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    SqlZaehlen() + " AND w.ProjektID = ?", new DbParam("@p", idProjekt));
                if (o == null || o == System.DBNull.Value) return 0;
                return System.Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        // =================================================================
        //  Der Baukasten
        // =================================================================

        /// <summary>Die fuenf Wertfelder einer Position; alle 0 oder NULL = ohne Wert.</summary>
        private static string OhneWert(string praefix)
        {
            return "IFNULL(" + praefix + "EingegebenerWert, 0) = 0 AND " +
                   "IFNULL(" + praefix + "Worstcase, 0) = 0 AND " +
                   "IFNULL(" + praefix + "Bestcase, 0) = 0 AND " +
                   "IFNULL(" + praefix + "Menge, 0) = 0 AND " +
                   "IFNULL(" + praefix + "Einheitpreis, 0) = 0";
        }

        /// <summary>Die drei Namen als SQL-Liste. Sie sind Steuerwerte aus
        /// <see cref="DbWerte"/>, keine Anwenderdaten - ein Apostroph kommt darin nicht
        /// vor; er wird trotzdem verdoppelt, damit die Bauweise unabhaengig von den
        /// Werten richtig bleibt.</summary>
        private static string Gruppenliste()
        {
            var sb = new System.Text.StringBuilder();
            foreach (string g in Erfassungsgruppen)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append('\'').Append(g.Replace("'", "''")).Append('\'');
            }
            return sb.ToString();
        }

        /// <summary>Die Bedingung, die eine zu entfernende Zeile beschreibt -
        /// buchstabengleich in Zaehlung und Loeschung.</summary>
        private static string Bedingung(string t)
        {
            return t + ".KomponentenID IN (SELECT k.ID FROM " + TABELLE_KOMPONENTE + " AS k " +
                   "WHERE k." + SchemaKatalog.SPALTE_KK_KOMPONENTE + " IN (" + Gruppenliste() + ")) " +
                   "AND " + t + ".StammID IN (SELECT f.StammID FROM " + TABELLE_FAKTOR + " AS f " +
                   "WHERE f." + SPALTE_HAUPTKOMPONENTE + " = 1) " +
                   "AND " + OhneWert(t + ".") +
                   // Die Gruppe darf NIRGENDS einen Wert tragen - sonst bleibt sie ganz.
                   " AND NOT EXISTS (SELECT 1 FROM " + TABELLE + " AS m " +
                   "WHERE m.ProjektID = " + t + ".ProjektID " +
                   "AND m.KategorieID = " + t + ".KategorieID " +
                   "AND m.KomponentenID = " + t + ".KomponentenID " +
                   "AND NOT (" + OhneWert("m.") + "))";
        }

        /// <summary>Die Zaehlung - dieselbe Bedingung wie die Loeschung.</summary>
        private static string SqlZaehlen()
        {
            return "SELECT COUNT(*) FROM " + TABELLE + " AS w WHERE " + Bedingung("w");
        }

        /// <summary>
        /// Die Loeschung. Sie geht ueber die <c>ID</c>-Liste der Zaehlung: SQLite laesst
        /// in einem DELETE keinen Tabellen-Alias zu, und die Unterabfrage auf DIESELBE
        /// Tabelle braucht einen.
        /// </summary>
        private static string SqlLoeschen()
        {
            return "DELETE FROM " + TABELLE + " WHERE ID IN (" +
                   "SELECT w.ID FROM " + TABELLE + " AS w WHERE " + Bedingung("w") + ")";
        }
    }
}
