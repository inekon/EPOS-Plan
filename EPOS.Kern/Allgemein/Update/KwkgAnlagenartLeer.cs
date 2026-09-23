using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE LEERE ANLAGENART WIRD NULL - Migrationsschritt 102
    //
    // WOZU. Tab_Energieanlagen.KWKG_Anlagenart traegt die Anlagenart nach § 8 KWKG
    // (DbWerte.KWKG_ANLAGENART_NEU / _MODERNISIERT / _NACHGERUESTET). Sieben
    // Anlagenzeilen der Testdatenbank fuehrten dort eine LEERE ZEICHENKETTE - weder
    // "nicht gepflegt" (NULL) noch eine Wahl (Analysepapier 2026-09-19, Befund D6;
    // Konzept Wirtschaftlichkeit § 6.3 Nr. 30).
    //
    // WAS DER SCHRITT TUT. Er setzt die leere Zeichenkette auf NULL - der Entscheid
    // R-NR Nr. 30 vom 22.09.2026 im Wortlaut: "ein DML-Schritt setzt die leere
    // Zeichenkette auf NULL; NULL heisst 'nicht gepflegt'". Geraten wird nichts: Ein
    // geratener Wert setzte Kontingent und Satzstaffel, die niemand eingegeben hat.
    //
    // NUR DIE LEERE ZEICHENKETTE. Getroffen wird genau '' - eine gepflegte Anlagenart
    // bleibt, NULL bleibt NULL, und jede andere Spalte der Zeile bleibt, auch
    // KWKG_Eigenstromfall, das in denselben sieben Zeilen '' traegt: Der Entscheid nennt
    // allein die Anlagenart.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg unterscheidet '' von NULL: Kontingent- und
    // Satzableitung fragen string.IsNullOrEmpty (KwkgKontingentRechner.Ableiten,
    // KwkgSatzRechner.Vorschlag), und der Dialog zeigt beide gleich. In der
    // Testdatenbank ist zudem keine der sieben Anlagen ein BHKW - es sind Waermepumpen,
    // ein Kessel und Pufferspeicher der Projekte 1032 und 1043.
    //
    // WIEDERHOLBAR. Ein zweiter Lauf findet keine leere Zeichenkette mehr (Offen() = 0)
    // und fasst nichts an.
    //
    // EINE QUELLE fuer drei Leser: den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests (TestDatenbank
    // zieht jede Arbeitskopie damit nach).
    // ====================================================================================

    /// <summary>
    /// Schemaschritt 102 — die leere Zeichenkette in
    /// <c>Tab_Energieanlagen.KWKG_Anlagenart</c> wird NULL („nicht gepflegt",
    /// Konzept Wirtschaftlichkeit § 6.3 Nr. 30). EINE Quelle für Migration, Werkzeug und
    /// Nachweis.
    /// </summary>
    public static class KwkgAnlagenartLeer
    {
        /// <summary>Die Tabelle der Anlagenzeilen.</summary>
        public const string TABELLE = SchemaKatalog.TAB_ENERGIEANLAGEN;

        /// <summary>Die Spalte, deren leere Zeichenkette NULL wird.</summary>
        public const string SPALTE = SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART;

        /// <summary>Der getroffene Wert — genau die leere Zeichenkette, gebunden statt
        /// in den Text geschrieben (BETRIEB_SQLITE § 6).</summary>
        public const string LEER = "";

        /// <summary>Die Anweisung des Schrittes.</summary>
        public const string SQL_SETZEN =
            "UPDATE [" + TABELLE + "] SET [" + SPALTE + "] = NULL WHERE [" + SPALTE + "] = ?";

        /// <summary>Die Zählung — dieselbe Bedingung wie die Anweisung.</summary>
        public const string SQL_ZAEHLEN =
            "SELECT COUNT(*) FROM [" + TABELLE + "] WHERE [" + SPALTE + "] = ?";

        /// <summary>Die betroffenen Zeilen mit Id, Projekt und Bezeichner — für das
        /// Protokoll des Schrittes, damit es sagt, WELCHE Anlagen er angefasst hat.</summary>
        public const string SQL_BETROFFENE =
            "SELECT [ID], [ID_Projekt], [Bezeichner] FROM [" + TABELLE + "] WHERE [" +
            SPALTE + "] = ? ORDER BY [ID]";

        /// <summary>Der eine Wert aller drei Anweisungen.</summary>
        public static DbParam[] Parameter()
        {
            return new[] { new DbParam("@leer", LEER) };
        }

        /// <summary>
        /// Die Anweisungen — eine, und nur solange sie etwas trifft. Auf einer bereits
        /// nachgezogenen Datenbank bleibt die Folge leer; der zweite Lauf fasst nichts an.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, Anweisung>> Anweisungen
        {
            get
            {
                if (!Vorhanden()) yield break;
                if (Offen() <= 0) yield break;
                yield return new KeyValuePair<string, Anweisung>(
                    "Leere Anlagenart in " + TABELLE + " auf NULL",
                    new Anweisung(SQL_SETZEN, Parameter()));
            }
        }

        /// <summary>
        /// Führt den Schritt aus und liefert die Zahl der Zeilen, die er auf NULL gesetzt
        /// hat (0 = nichts zu tun). Für Werkzeug und Nachweis; die Migration ruft die
        /// <see cref="Anweisungen"/> selbst, um einen Fehler benannt melden zu können.
        /// </summary>
        public static int Ausfuehren()
        {
            int vorher = Offen();
            foreach (KeyValuePair<string, Anweisung> a in Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
            return vorher - Offen();
        }

        /// <summary>Anweisungstext samt Parametern — Text und Werte gehören zusammen.</summary>
        public sealed class Anweisung
        {
            /// <summary>Der SQL-Text mit seinem <c>?</c>-Platzhalter.</summary>
            public string Sql { get; }

            /// <summary>Die Werte in Bindungsreihenfolge.</summary>
            public DbParam[] Parameter { get; }

            /// <summary>Text und Werte gehören zusammen und werden zusammen übergeben.</summary>
            public Anweisung(string sql, DbParam[] parameter)
            {
                Sql = sql;
                Parameter = parameter;
            }
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Steht die Spalte? Ohne sie tut der Schritt nichts — dieselbe
        /// tolerante Haltung wie bei jedem DML-Schritt.</summary>
        public static bool Vorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>Wie viele Anlagenzeilen tragen noch die leere Zeichenkette? Genau so
        /// viele setzt der Schritt auf NULL. 0 = nichts zu tun, er ist gelaufen.</summary>
        public static int Offen()
        {
            if (!Vorhanden()) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(SQL_ZAEHLEN, Parameter());
                if (o == null || o == DBNull.Value) return 0;
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Die Zeilen, die der Schritt anfassen wird, als Protokollzeilen
        /// „Id 12310, Projekt 1032: T 800-2" — vor dem Schreiben gelesen, danach findet
        /// die Abfrage nichts mehr.
        /// </summary>
        public static List<string> Betroffene()
        {
            var zeilen = new List<string>();
            if (!Vorhanden()) return zeilen;
            DataTable t = DataRepository.GetDataTable(SQL_BETROFFENE, Parameter());
            if (t == null) return zeilen;
            foreach (DataRow r in t.Rows)
                zeilen.Add("Id " + Convert.ToString(r["ID"], CultureInfo.InvariantCulture) +
                           ", Projekt " + Convert.ToString(r["ID_Projekt"], CultureInfo.InvariantCulture) +
                           ": " + Convert.ToString(r["Bezeichner"], CultureInfo.InvariantCulture));
            return zeilen;
        }
    }
}
