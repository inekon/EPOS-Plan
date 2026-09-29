using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BEZUGSART ZIMMER - Schemaschritt des Zapfprofilgenerators (Auftrag A2, Entscheide E-A2-1,
    // E-A2-3 und E-A2-4; Umsetzungskonzept Zapfprofilgenerator, Nachtrag N34, Weg 3 der
    // Hotel-Durchsicht).
    //
    // WOZU. Der Katalogtyp "Hotel (aus Messung, je Zimmer)" fuehrt seine Kennwerte je Zimmer.
    // Bisher stand er unter der Bezugsart Betten, und ein Namenszusatz trug die Aussage. Er
    // bekommt die eigene Bezugsart Zimmer (8). Beide Tabellen mit einer Bezugsart tragen heute
    // CHECK ("Bezugsart" IN (1,2,3,4,5,6,7)) - SQLite kann einem CHECK keinen Wert nachtragen,
    // also werden sie neu gebaut.
    //
    // ZWEI TABELLEN. Tab_TwwNutzungsart_STAMM (Spalte aus T1, Schritt 103, NOT NULL) und
    // Tab_TwwBedarfstag_STAMM (Spalte aus T3, Schritt 124, nullbar). Weitere Tabellen fuehren keine
    // Bezugsart mit CHECK: Zone, Wohnungstyp und Projektzeile tragen eine Bezugsmenge, keine Art;
    // die Messreihen und die Konstruktorzeilen keine von beiden (geprueft am Schema).
    //
    // DAS REZEPT des Hauses fuer STRICT-Tabellen (Schritte 96 und 100): Fremdschluessel AUS vor der
    // Transaktion (DataRepository.VorgangOhneFremdschluessel), die alte Tabelle weicht unter
    // legacy_alter_table auf einen Hilfsnamen aus, die neue entsteht unter dem echten Namen aus dem
    // GELTENDEN sqlite_master-Text mit der neuen Pruefklausel, die Zeilen ziehen namentlich um, der
    // AUTOINCREMENT-Stand reist mit, Indizes und Trigger stehen wieder, und foreign_key_check haelt
    // die Tabelle und jede Kindtabelle, die auf sie zeigt, noch IN der Transaktion.
    //
    // GRUNDSCHEMA = SCHRITT. Das Grundschema (TwwSchema.SQL_CREATE_NUTZUNGSART, TwwSchema.SpaltenT3)
    // fuehrt dieselbe Wertemenge TwwSchema.BEZUGSART_WERTE. Der Neubau tauscht im geltenden Text
    // allein die Pruefklausel - eine neu angelegte Datenbank und eine nachgezogene tragen danach
    // Zeichen fuer Zeichen denselben CREATE-Text (Test TwwBezugsartSchemaTests).
    //
    // DATENNACHFUEHRUNG (E-A2-4 a). Im selben Vorgang fuehrt PaketteilNachfuehrung die gespeicherten
    // Zeilen des ausgelieferten Paketteils nach: "Hotel (aus Messung)" bzw. "Hotel (aus Messung, je
    // Zimmer)" mit Bezugsart Betten heisst danach "Hotel (aus Messung, je Zimmer)" mit Bezugsart
    // Zimmer - dieselbe ID. Bedarfstage des Paketteils mit Bezugsart Betten gibt es nicht (die
    // Ecodesign-Zapfprofile tragen Wohneinheiten); an Tab_TwwBedarfstag_STAMM aendert sich keine Zeile.
    //
    // ERGEBNISNEUTRAL. Zimmer rechnet wie Betten (Mengengeruest, Einheiten, Typtage, Auslegung);
    // kein Referenzprojekt benutzt die Hotelzeile (1045 rechnet mit "Wohnen gross (abgeleitet)").
    // ====================================================================================

    /// <summary>
    /// <b>Die Bezugsart Zimmer</b> — Schemaschritt (Nummer <see cref="SCHRITT"/>): Neubau von
    /// <c>Tab_TwwNutzungsart_STAMM</c> und <c>Tab_TwwBedarfstag_STAMM</c> mit der Prüfklausel
    /// <see cref="CHECK_NEU"/>, dazu die Nachführung der gespeicherten Paketzeilen
    /// (<see cref="PaketteilNachfuehrung"/>). EINE Quelle für Migrationsschritt,
    /// <c>Werkzeuge/Testdatenbankschema</c>, Testvorrichtung und Nachweis (ADR-001 Option C).
    /// Anlass und Bauform stehen im Kopf der Datei.
    /// </summary>
    public static class TwwBezugsartSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration,
        /// Werkzeug, Zielstand, Paketanhebung und Tests verweisen hierher. Wird er umnummeriert,
        /// ändert sich nur diese Zeile.
        /// </summary>
        public const int SCHRITT = KonditionierungVorlagenSchema.SCHRITT + 1;

        /// <summary>Die Tabellen mit einer Bezugsart samt Prüfklausel — die Nutzungsart zuerst.</summary>
        public static readonly IReadOnlyList<string> TABELLEN = new[]
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM
        };

        /// <summary>Die Prüfklausel vor dem Schritt: Bezugsart 1 bis 7.</summary>
        public const string CHECK_ALT = "CHECK (\"Bezugsart\" IN (1,2,3,4,5,6,7))";

        /// <summary>Die Prüfklausel nach dem Schritt — dieselbe Wertemenge wie Grundschema und Schreibweg.</summary>
        public const string CHECK_NEU = "CHECK (\"Bezugsart\" IN (" + TwwSchema.BEZUGSART_WERTE + "))";

        /// <summary>Der Zusatz des Hilfsnamens, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        private const string HILFSZUSATZ = "_vor_Bezugsart_Zimmer";

        /// <summary>
        /// <b>Der Zieltext einer Tabelle</b>: der GELTENDE <c>sqlite_master</c>-Text, in dem allein
        /// <see cref="CHECK_ALT"/> durch <see cref="CHECK_NEU"/> ersetzt ist. <c>null</c>, wenn nichts
        /// zu tun ist (die Tabelle trägt schon <see cref="CHECK_NEU"/> oder gar keine Bezugsart). Trägt
        /// sie die Spalte mit einer anderen Prüfklausel, bricht der Aufruf ab, statt eine falsche
        /// Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(string tabelle, string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");
            if (bestand.Contains(CHECK_NEU, StringComparison.Ordinal)) return null;
            int stelle = bestand.IndexOf(CHECK_ALT, StringComparison.Ordinal);
            if (stelle < 0)
            {
                if (bestand.IndexOf("\"Bezugsart\"", StringComparison.Ordinal) < 0) return null;
                throw new InvalidOperationException(
                    tabelle + " traegt die Spalte Bezugsart mit einer unbekannten Pruefklausel - der Schritt " +
                    SCHRITT.ToString(CultureInfo.InvariantCulture) + " baut die Tabelle deshalb NICHT um.");
            }
            if (bestand.IndexOf(CHECK_ALT, stelle + CHECK_ALT.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(tabelle + " traegt die Pruefklausel der Bezugsart zweimal.");
            if (!bestand.TrimEnd().EndsWith(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(tabelle + " ist keine STRICT-Tabelle - der Schritt baut sie nicht um.");
            return bestand.Substring(0, stelle) + CHECK_NEU + bestand.Substring(stelle + CHECK_ALT.Length);
        }

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + HILFSZUSATZ;

        /// <summary>Muss die Tabelle noch neu gebaut werden? <c>false</c> ohne die Tabelle.</summary>
        public static bool UmbauNoetig(string tabelle)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("p1", tabelle));
            string bestand = o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
            return bestand != null && Zieltext(tabelle, bestand) != null;
        }

        /// <summary>Wie viele der Tabellen müssen noch neu gebaut werden?</summary>
        public static int Offen()
        {
            int offen = 0;
            foreach (string t in TABELLEN)
                if (UmbauNoetig(t)) offen++;
            return offen;
        }

        /// <summary>Steht der Schritt? Keine Tabelle offen, keine Paketzeile in einem früheren Stand.</summary>
        public static bool Vollstaendig() => Offen() == 0 && PaketteilNachfuehrung.Offen() == 0;

        /// <summary>
        /// <b>Führt den Schritt aus</b> — alles in EINEM Vorgang mit abgeschalteten Fremdschlüsseln:
        /// erst der Neubau jeder offenen Tabelle, dann die Nachführung der Paketzeilen. Scheitert
        /// etwas, rollt der Vorgang zurück und die Ausnahme geht an den Aufrufer (Migration,
        /// Werkzeug). <b>Wiederholbar</b>: Eine fertige Tabelle wird übersprungen, eine nachgeführte
        /// Zeile trifft die Regel nicht mehr. Liefert die Zahl der neu gebauten Tabellen.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            int umgebaut = 0;
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    foreach (string t in TABELLEN)
                    {
                        string bestand = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                       new DbParam("p1", t)));
                        if (bestand == null)
                        {
                            Notiere(bericht, t + ": fehlt - nichts umzubauen");
                            continue;
                        }
                        string ziel = Zieltext(t, bestand);
                        if (ziel == null) continue;

                        long vorher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        int indizes = Neubau(v, t, ziel);
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM \"" + t + "\"");
                        if (vorher != nachher)
                            throw new InvalidOperationException(t + ": " + vorher.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) vor dem Neubau, " + nachher.ToString(CultureInfo.InvariantCulture) + " danach.");
                        long verletzt = Verletzt(v, t);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Neubau meldet foreign_key_check fuer " + t +
                                " oder eine Kindtabelle " + verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                        umgebaut++;
                        Notiere(bericht, t + ": neu gebaut mit " + CHECK_NEU + "; Zeilen " +
                                         vorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                                         nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                         indizes.ToString(CultureInfo.InvariantCulture));
                    }

                    int nachgefuehrt = PaketteilNachfuehrung.Nachfuehren(v, bericht);
                    Notiere(bericht, nachgefuehrt.ToString(CultureInfo.InvariantCulture) +
                                     " Paketzeile(n) in einem frueheren Stand nachgefuehrt");
                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster aus Schritt 96).
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            return umgebaut;
        }

        /// <summary>
        /// Das Neubaurezept des Hauses (Schritte 96 und 100): Die alte Tabelle weicht auf den
        /// Hilfsnamen aus, die neue entsteht unter dem ECHTEN Namen aus <paramref name="ziel"/>, die
        /// Zeilen ziehen namentlich um, der AUTOINCREMENT-Stand reist mit, die alte fällt, Indizes und
        /// Trigger stehen wieder. Liefert die Zahl der wieder angelegten Indizes und Trigger.
        /// </summary>
        internal static int Neubau(DbVorgang v, string tabelle, string ziel)
        {
            string alt = Hilfsname(tabelle);

            // Spalten NAMENTLICH - "SELECT *" haengt an der Reihenfolge zweier Schemastaende.
            var namen = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("p1", tabelle));
            foreach (DataRow zeile in info.Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen.ToArray());

            // Indizes und Trigger der Tabelle (die automatischen Indizes tragen keinen Text).
            var nachher = new List<string>();
            DataTable objekte = v.Lese(
                "SELECT type, sql FROM sqlite_master WHERE type IN ('index', 'trigger') AND tbl_name = ? AND sql IS NOT NULL " +
                "ORDER BY CASE WHEN type = 'index' THEN 0 ELSE 1 END, name",
                new DbParam("p1", tabelle));
            foreach (DataRow zeile in objekte.Rows)
            {
                string sql = Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture);
                nachher.Add(string.Equals(Convert.ToString(zeile["type"], CultureInfo.InvariantCulture), "index", StringComparison.Ordinal)
                    ? ProjektFremdschluessel.MitIfNotExists(sql) : sql);
            }

            object standWert = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("p1", tabelle));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");

            // Seit SQLite 3.25 prueft ALTER TABLE ... RENAME jede Sicht neu, und ohne den Legacy-Modus
            // schriebe SQLite die Verweise der Kindtabellen auf den Hilfsnamen um - sie sollen aber auf
            // den Namen zeigen, den die neue Tabelle gleich wieder traegt.
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " + spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");

            // DER ZAEHLER. Ohne ihn fiele der AUTOINCREMENT-Stand auf die groesste kopierte ID zurueck,
            // und die ID einer geloeschten Zeile kaeme ein zweites Mal heraus (Konzept 3.2).
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", alt));
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", tabelle));
            if (standWert != null && standWert != DBNull.Value)
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                             new DbParam("p1", tabelle),
                             new DbParam("p2", Convert.ToInt64(standWert, CultureInfo.InvariantCulture)));

            foreach (string anweisung in nachher)
                v.Ausfuehren(anweisung);
            return nachher.Count;
        }

        /// <summary>
        /// Die verletzten Beziehungen der Tabelle selbst und jeder Tabelle, die auf sie zeigt
        /// (<c>foreign_key_check</c> je Tabelle — ein Befund einer fremden Tabelle hält den Schritt
        /// nicht an). Auch <see cref="TwwFuellstandSchema"/> geht diesen Weg — EIN Rezept.
        /// </summary>
        internal static long Verletzt(DbVorgang v, string tabelle)
        {
            var pruefen = new List<string> { tabelle };
            DataTable kinder = v.Lese(
                "SELECT DISTINCT m.name AS name FROM sqlite_master AS m, pragma_foreign_key_list(m.name) AS f " +
                "WHERE m.type = 'table' AND f.\"table\" = ?", new DbParam("p1", tabelle));
            foreach (DataRow zeile in kinder.Rows)
            {
                string kind = Convert.ToString(zeile["name"], CultureInfo.InvariantCulture);
                if (!pruefen.Contains(kind)) pruefen.Add(kind);
            }
            long verletzt = 0;
            foreach (string t in pruefen)
                verletzt += Zahl(v, "SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("p1", t));
            return verletzt;
        }

        private static long Zahl(DbVorgang v, string sql, params DbParam[] parameter)
        {
            object wert = v.Skalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static void Notiere(IList<string> bericht, string zeile)
        {
            if (bericht != null) bericht.Add(zeile);
        }
    }
}
