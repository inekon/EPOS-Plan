using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE FREMDSCHLUESSELSPALTEN VERLIEREN IHRE VORGABE 0
    // - Migrationsschritt 100 (Anwenderentscheid vom 21.09.2026, Auftrag FK-1).
    //
    // WOZU. Einundvierzig Fremdschluesselspalten der Datenbank tragen DEFAULT 0, und
    // KEINE Elterntabelle hat eine Zeile 0. Die Vorgabe ist damit eine Falle: Jeder
    // Schreibweg, der eine solche Spalte WEGLAESST, bekommt still die 0 eingetragen -
    // und die 0 verletzt den Fremdschluessel. Die Meldung, die daraus entsteht, faellt
    // irgendwo weiter vorn an ("SQLite Error 19: FOREIGN KEY constraint failed") und
    // nennt weder Tabelle noch Spalte.
    //
    // Genau so kam die Gerätemeldung vom 21.09.2026 zustande: Der Typprofil-Insert in
    // StromverbraucherStammCtrl.CopyFromStamm liess ID_Projekt weg, bekam die 0, und die
    // Kopie scheiterte an dem Fremdschluessel, den Schritt 96 auf diese Spalte gesetzt
    // hat.
    //
    // WAS DER SCHRITT HERSTELLT. Je betroffener Spalte dieselbe Spaltendefinition OHNE
    // ihr " DEFAULT 0". NOT NULL bleibt, wo es steht - und darauf kommt es an: Wer die
    // Spalte danach weglaesst, bekommt sofort "NOT NULL constraint failed:
    // <Tabelle>.<Spalte>", also Tabelle und Spalte im Klartext, statt still eine 0. Wo
    // die Spalte nullbar ist, heisst weggelassen ab hier NULL - und NULL laesst SQLite
    // bei einem Fremdschluessel immer durch, denn "kein Elternsatz gemeint" ist eine
    // gueltige Aussage.
    //
    // DER SCHRITT AENDERT KEINEN WERT. Er kopiert Zeilen, er rechnet nicht: Werte,
    // Typen, Ids, sqlite_sequence-Staende, Spaltenreihenfolge, Indizes und Sichten
    // bleiben. Der Referenzlauf bleibt byte-gleich.
    //
    // DER KATALOG WIRD GEMESSEN, NICHT AUFGEZAEHLT. Einundvierzig Spaltennamen in einer
    // Quelldatei waeren einundvierzig zweite Wahrheiten, die beim naechsten neuen
    // Fremdschluessel veralten. Gefragt wird deshalb die Datei selbst
    // (pragma_foreign_key_list x pragma_table_info, siehe SqlBetroffene) - und dieselbe
    // Frage ist zugleich die Wache in den Tests und die Wiederholbarkeit des Schritts:
    // Was keine Vorgabe mehr traegt, steht nicht mehr in der Antwort.
    //
    // DER ZIELTEXT WIRD NICHT ABGESCHRIEBEN - dieselbe Regel wie in Schritt 96. Er
    // entsteht aus dem GELTENDEN sqlite_master.sql der Tabelle: die Spaltendefinition
    // gesucht, ihr " DEFAULT 0" herausgenommen, sonst Zeichen fuer Zeichen der Bestand.
    // Wer eine Spalte ergaenzt, ergaenzt hier nichts. Die Suche ist auf die EINE
    // Definition begrenzt (Abschnitte zerlegt den CREATE-Text an den Kommas der obersten
    // Ebene) - Tab_ProjektWerte fuehrt neben den drei Fremdschluesselspalten sieben
    // weitere Spalten mit DEFAULT 0, und die behalten ihre Vorgabe.
    //
    // DAS REZEPT IST DAS VON SCHRITT 96, aus demselben Grund: Der Schritt baut
    // ELTERNtabellen um (Tab_Stromverbraucher traegt Tab_Stromverbrauchertyp,
    // Tab_Gebaeude traegt Tab_DBTagV). Bei eingeschalteten Fremdschluesseln fuehrt
    // DROP TABLE ein implizites DELETE FROM aus, und das LOEST DIE KASKADE AUS. Deshalb
    // laeuft der Umbau ueber DataRepository.VorgangOhneFremdschluessel(), also mit
    // "PRAGMA foreign_keys = OFF" VOR der Transaktion. Die alte Tabelle weicht auf den
    // Hilfsnamen aus, die neue entsteht sofort unter dem ECHTEN Namen.
    //
    // ZWEI ZEUGEN JE TABELLE. VORHER: keine Zeile traegt den Wert 0 in einer betroffenen
    // Spalte - sonst bricht der Schritt BENANNT ab, statt eine Zeile zu verlieren oder
    // eine kaputte Beziehung zu zementieren. NACHHER: foreign_key_check der Tabelle ist
    // leer. Beide Proben laufen INNERHALB der Transaktion; schlaegt eine an, bleibt die
    // Tabelle, wie sie war.
    //
    // EIGENE TRANSAKTION JE TABELLE. Ein Fehler an Tabelle 17 laesst die ersten sechzehn
    // stehen; der Schritt bleibt wiederholbar, weil jede fertige Tabelle beim naechsten
    // Lauf uebersprungen wird (UmbauNoetig fragt die Datei).
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // ProjektFremdschluessel (96): Die Anweisungen brauchen DREI Leser - den Schemaschritt
    // in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Die Vorgabe <c>0</c> der Fremdschlüsselspalten (Schemaschritt 100) — EINE Quelle
    /// für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class FremdschluesselVorgabe
    {
        /// <summary>Die Vorgabe, die der Schritt entfernt — als Text, wie sie in
        /// <c>pragma_table_info.dflt_value</c> und im CREATE-Text steht.</summary>
        public const string VORGABE = "0";

        /// <summary>
        /// Das Ende, das jede STRICT-Tabelle trägt. Trägt eine Tabelle es nicht, wird
        /// sie NICHT umgebaut (siehe <see cref="Zieltext"/>) — dieselbe STRICT-Wache
        /// wie in Schemaschritt 96.
        /// </summary>
        public const string ENDE = ") STRICT";

        /// <summary>Der Zusatz, unter dem die alte Tabelle für die Dauer des Umbaus ausweicht.</summary>
        public const string HILFSZUSATZ = "_alt";

        /// <summary>Das Wort, hinter dem die Vorgabe im CREATE-Text steht.</summary>
        private const string WORT = "DEFAULT";

        // =================================================================
        //  Was betroffen ist - gemessen, nicht aufgezaehlt
        // =================================================================

        /// <summary>Eine betroffene Spalte: ihre Tabelle und ihr Name.</summary>
        public sealed class Spalte
        {
            /// <summary>Die Tabelle.</summary>
            public string Tabelle { get; }

            /// <summary>Der Spaltenname.</summary>
            public string Name { get; }

            /// <summary>Beides zusammen.</summary>
            public Spalte(string tabelle, string name)
            {
                Tabelle = tabelle;
                Name = name;
            }

            /// <summary><c>Tabelle.Spalte</c> — die Schreibweise des Berichts.</summary>
            public override string ToString()
            {
                return Tabelle + "." + Name;
            }
        }

        /// <summary>
        /// DIE EINE FRAGE AN DIE DATEI: Welche Fremdschlüsselspalte trägt noch die
        /// Vorgabe <see cref="VORGABE"/>?
        ///
        /// <para><c>pragma_foreign_key_list</c> nennt die Spalten mit einer Beziehung,
        /// <c>pragma_table_info</c> die Vorgabe dazu. <c>DISTINCT</c> ist keine Zier:
        /// Eine Spalte kann zwei gleichlautende Beziehungen tragen
        /// (<c>Z_Projekt_Stromverbraucher.ID_Stromverbraucher</c> führt sie doppelt) —
        /// umgebaut wird sie trotzdem nur einmal.</para>
        /// </summary>
        public static string SqlBetroffene()
        {
            return "SELECT DISTINCT m.name AS tabelle, f.\"from\" AS spalte " +
                   "FROM sqlite_master m " +
                   "JOIN pragma_foreign_key_list(m.name) f " +
                   "JOIN pragma_table_info(m.name) t ON t.name = f.\"from\" " +
                   "WHERE m.type = 'table' AND substr(m.name, 1, 7) <> 'sqlite_' " +
                   "AND t.dflt_value = ? " +
                   "ORDER BY m.name, f.\"from\"";
        }

        /// <summary>Alle betroffenen Spalten, in Tabellen- und Spaltenreihenfolge.</summary>
        public static List<Spalte> Betroffene()
        {
            var liste = new List<Spalte>();
            DataTable t = DataRepository.GetDataTable(SqlBetroffene(), new DbParam("p1", VORGABE));
            if (t == null) return liste;

            foreach (DataRow zeile in t.Rows)
                liste.Add(new Spalte(Convert.ToString(zeile["tabelle"], CultureInfo.InvariantCulture),
                                     Convert.ToString(zeile["spalte"], CultureInfo.InvariantCulture)));
            return liste;
        }

        /// <summary>Die betroffenen Tabellen, jede einmal, in Reihenfolge.</summary>
        public static List<string> Tabellen()
        {
            var namen = new List<string>();
            foreach (Spalte s in Betroffene())
                if (!namen.Contains(s.Tabelle)) namen.Add(s.Tabelle);
            return namen;
        }

        /// <summary>Die betroffenen Spalten EINER Tabelle.</summary>
        public static List<string> BetroffeneSpalten(string tabelle)
        {
            var namen = new List<string>();
            foreach (Spalte s in Betroffene())
                if (string.Equals(s.Tabelle, tabelle, StringComparison.OrdinalIgnoreCase))
                    namen.Add(s.Name);
            return namen;
        }

        /// <summary>Ist der Umbau dieser Tabelle nötig?</summary>
        public static bool UmbauNoetig(string tabelle)
        {
            if (!DataRepository.TabelleVorhanden(tabelle)) return false;
            return BetroffeneSpalten(tabelle).Count > 0;
        }

        /// <summary>Wie viele Tabellen sind noch umzubauen?</summary>
        public static int Offen()
        {
            return Tabellen().Count;
        }

        /// <summary>Wie viele Spalten tragen die Vorgabe noch?</summary>
        public static int OffeneSpalten()
        {
            return Betroffene().Count;
        }

        /// <summary>
        /// Der Zähltext der Zeilen, die in dieser Spalte WIRKLICH den Wert
        /// <see cref="VORGABE"/> tragen — der Zeuge VOR dem Umbau.
        /// </summary>
        public static string SqlZeilenMitVorgabe(string tabelle, string spalte)
        {
            return "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = " + VORGABE;
        }

        /// <summary>Wie viele Zeilen tragen in dieser Spalte den Wert <see cref="VORGABE"/>?</summary>
        public static long ZeilenMitVorgabe(string tabelle, string spalte)
        {
            object wert = DataRepository.ExecuteScalar(SqlZeilenMitVorgabe(tabelle, spalte));
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        // =================================================================
        //  Der Zieltext
        // =================================================================

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Umbaus ausweicht.</summary>
        public static string Hilfsname(string tabelle)
        {
            return tabelle + HILFSZUSATZ;
        }

        /// <summary>
        /// Der Zieltext der Tabelle: ihr GELTENDER <c>sqlite_master.sql</c>, aus dem die
        /// Vorgabe der genannten Spalten herausgenommen ist — sonst Zeichen für Zeichen
        /// der Bestand.
        ///
        /// <para>Greift eine Stelle nicht — weil die Tabelle kein <c>STRICT</c> trägt,
        /// anders gebaut ist als erwartet, die Spalte im Text nicht vorkommt oder dort
        /// eine ANDERE Vorgabe steht —, bricht der Aufruf ab, statt eine falsche Tabelle
        /// anzulegen.</para>
        /// </summary>
        public static string Zieltext(string tabelle, string bestand, IList<string> spalten)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException(
                    "Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");

            string text = bestand.TrimEnd();
            if (!text.EndsWith(ENDE, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Die Tabelle " + tabelle + " endet nicht auf \"" + ENDE + "\" - sie ist " +
                    "keine STRICT-Tabelle oder anders gebaut als erwartet; " +
                    "Schemaschritt 100 baut sie deshalb NICHT um.");

            string kopf = "CREATE TABLE \"" + tabelle + "\" (";
            if (!text.StartsWith(kopf, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Der CREATE-Text von " + tabelle + " hat eine andere Bauform als erwartet " +
                    "(erwartet wurde der Beginn " + kopf + ").");

            foreach (string spalte in spalten)
            {
                int anfang = -1;
                int ende = -1;
                foreach (Abschnitt a in Abschnitte(text, kopf.Length))
                {
                    if (!string.Equals(a.Name, spalte, StringComparison.OrdinalIgnoreCase)) continue;
                    anfang = a.Anfang;
                    ende = a.Ende;
                    break;
                }

                if (anfang < 0)
                    throw new InvalidOperationException(
                        "Im CREATE-Text von " + tabelle + " steht keine Definition der Spalte " +
                        spalte + ".");

                string neu = OhneVorgabe(text.Substring(anfang, ende - anfang), tabelle, spalte);
                text = text.Substring(0, anfang) + neu + text.Substring(ende);
            }

            return text;
        }

        /// <summary>Ein Abschnitt des CREATE-Textes: eine Spalte oder eine Tabellenbedingung.</summary>
        private sealed class Abschnitt
        {
            public int Anfang;
            public int Ende;
            public string Name;
        }

        /// <summary>
        /// Zerlegt den CREATE-Text ab <paramref name="start"/> an den Kommas der
        /// OBERSTEN Ebene. Zeichenketten (<c>'…'</c>), Bezeichner (<c>"…"</c>,
        /// <c>[…]</c>, <c>`…`</c>) und Klammern werden übersprungen — ohne das träfe
        /// schon <c>CHECK ("Kostenart" IN (0,1))</c> daneben.
        /// </summary>
        private static List<Abschnitt> Abschnitte(string text, int start)
        {
            var liste = new List<Abschnitt>();
            int tiefe = 1;
            int anfang = start;

            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '\'' || c == '"' || c == '`')
                {
                    i = Ueberspringe(text, i, c);
                    continue;
                }

                if (c == '[')
                {
                    i = Ueberspringe(text, i, ']');
                    continue;
                }

                if (c == '(') { tiefe++; continue; }

                if (c == ')')
                {
                    tiefe--;
                    if (tiefe == 0)
                    {
                        liste.Add(Baue(text, anfang, i));
                        return liste;
                    }
                    continue;
                }

                if (c == ',' && tiefe == 1)
                {
                    liste.Add(Baue(text, anfang, i));
                    anfang = i + 1;
                }
            }

            liste.Add(Baue(text, anfang, text.Length));
            return liste;
        }

        /// <summary>Der Index des SCHLIESSENDEN Zeichens; doppelte Anführung zählt als Inhalt.</summary>
        private static int Ueberspringe(string text, int anfang, char schluss)
        {
            for (int i = anfang + 1; i < text.Length; i++)
            {
                if (text[i] != schluss) continue;
                if (i + 1 < text.Length && text[i + 1] == schluss) { i++; continue; }
                return i;
            }
            return text.Length - 1;
        }

        /// <summary>Ein Abschnitt samt dem Bezeichner, mit dem er beginnt.</summary>
        private static Abschnitt Baue(string text, int anfang, int ende)
        {
            return new Abschnitt { Anfang = anfang, Ende = ende, Name = ErsterBezeichner(text, anfang, ende) };
        }

        /// <summary>
        /// Der Bezeichner, mit dem ein Abschnitt beginnt — der Spaltenname, oder bei
        /// einer Tabellenbedingung deren erstes Wort (<c>FOREIGN</c>, <c>PRIMARY</c>,
        /// <c>CHECK</c>, <c>UNIQUE</c>), das auf keinen Spaltennamen dieses Schemas
        /// passt.
        /// </summary>
        private static string ErsterBezeichner(string text, int anfang, int ende)
        {
            int i = anfang;
            while (i < ende && char.IsWhiteSpace(text[i])) i++;
            if (i >= ende) return "";

            char c = text[i];
            if (c == '"' || c == '`' || c == '[')
            {
                char schluss = c == '[' ? ']' : c;
                int zu = Ueberspringe(text, i, schluss);
                if (zu > ende) return "";
                return text.Substring(i + 1, zu - i - 1).Replace(new string(schluss, 2), schluss.ToString());
            }

            var wort = new StringBuilder();
            while (i < ende && !char.IsWhiteSpace(text[i]) && text[i] != '(' && text[i] != ',')
                wort.Append(text[i++]);
            return wort.ToString();
        }

        /// <summary>
        /// Dieselbe Spaltendefinition ohne ihr <c>DEFAULT &lt;Vorgabe&gt;</c>. Steht dort
        /// keine oder eine ANDERE Vorgabe, bricht der Aufruf ab — der Schritt fasst nur
        /// an, was er versteht.
        /// </summary>
        private static string OhneVorgabe(string definition, string tabelle, string spalte)
        {
            int wortAnfang = -1;
            int tiefe = 0;

            for (int i = 0; i < definition.Length; i++)
            {
                char c = definition[i];

                if (c == '\'' || c == '"' || c == '`') { i = Ueberspringe(definition, i, c); continue; }
                if (c == '[') { i = Ueberspringe(definition, i, ']'); continue; }
                if (c == '(') { tiefe++; continue; }
                if (c == ')') { tiefe--; continue; }
                if (tiefe != 0) continue;

                if (char.ToUpperInvariant(c) != WORT[0]) continue;
                if (i + WORT.Length > definition.Length) continue;
                if (string.Compare(definition, i, WORT, 0, WORT.Length,
                                   StringComparison.OrdinalIgnoreCase) != 0) continue;
                if (i > 0 && !char.IsWhiteSpace(definition[i - 1])) continue;

                int nach = i + WORT.Length;
                if (nach < definition.Length && !char.IsWhiteSpace(definition[nach])) continue;

                wortAnfang = i;
                break;
            }

            if (wortAnfang < 0)
                throw new InvalidOperationException(
                    "Die Spalte " + tabelle + "." + spalte + " traegt im CREATE-Text kein " +
                    WORT + " - Schemaschritt 100 fasst sie deshalb NICHT an.");

            int wertAnfang = wortAnfang + WORT.Length;
            while (wertAnfang < definition.Length && char.IsWhiteSpace(definition[wertAnfang])) wertAnfang++;

            int wertEnde = wertAnfang;
            if (wertEnde < definition.Length && definition[wertEnde] == '(')
            {
                int klammern = 0;
                while (wertEnde < definition.Length)
                {
                    if (definition[wertEnde] == '(') klammern++;
                    else if (definition[wertEnde] == ')') { klammern--; if (klammern == 0) { wertEnde++; break; } }
                    wertEnde++;
                }
            }
            else if (wertEnde < definition.Length &&
                     (definition[wertEnde] == '\'' || definition[wertEnde] == '"'))
            {
                wertEnde = Ueberspringe(definition, wertEnde, definition[wertEnde]) + 1;
            }
            else
            {
                while (wertEnde < definition.Length && !char.IsWhiteSpace(definition[wertEnde])) wertEnde++;
            }

            string wert = definition.Substring(wertAnfang, wertEnde - wertAnfang).Trim().Trim('\'', '"');
            if (!string.Equals(wert, VORGABE, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Die Spalte " + tabelle + "." + spalte + " traegt die Vorgabe \"" + wert +
                    "\" statt \"" + VORGABE + "\" - Schemaschritt 100 fasst sie deshalb NICHT an.");

            string vorne = definition.Substring(0, wortAnfang).TrimEnd();
            string hinten = definition.Substring(wertEnde).TrimStart();
            return hinten.Length == 0 ? vorne : vorne + " " + hinten;
        }

        // =================================================================
        //  Der Umbau
        // =================================================================

        /// <summary>
        /// Baut EINE Tabelle um und schreibt jede Zahl in den Bericht. Fehler werden
        /// DURCHGEREICHT — der Aufrufer entscheidet, ob daraus eine Berichtszeile, eine
        /// Konsolenzeile oder ein roter Test wird.
        /// </summary>
        /// <returns>true, wenn umgebaut wurde; false, wenn nichts zu tun war.</returns>
        public static bool Umbauen(string tabelle, IList<string> bericht)
        {
            if (!DataRepository.TabelleVorhanden(tabelle))
            {
                Notiere(bericht, tabelle + ": nicht vorhanden - uebergangen.");
                return false;
            }

            List<string> spalten = BetroffeneSpalten(tabelle);
            if (spalten.Count == 0)
            {
                Notiere(bericht, tabelle + ": uebersprungen - keine Fremdschluesselspalte " +
                                 "traegt noch die Vorgabe " + VORGABE + ".");
                return false;
            }

            long zeilenVorher;
            long zeilenNachher;
            int indizes = 0;

            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    zeilenVorher = Zahl(v, "SELECT COUNT(*) FROM \"" + tabelle + "\"");

                    // ---- DER ZEUGE VORHER. Der Schritt aendert keine Werte; eine Zeile,
                    //      die die Vorgabe wirklich traegt, zeigt auf einen Elternsatz,
                    //      den es nicht gibt. Die ist zu klaeren, nicht zu zementieren.
                    foreach (string spalte in spalten)
                    {
                        long mitVorgabe = Zahl(v, SqlZeilenMitVorgabe(tabelle, spalte));
                        if (mitVorgabe > 0)
                            throw new InvalidOperationException(
                                tabelle + "." + spalte + " fuehrt " +
                                mitVorgabe.ToString(CultureInfo.InvariantCulture) +
                                " Zeile(n) mit dem Wert " + VORGABE + ". Schemaschritt 100 " +
                                "aendert keine Werte und baut die Tabelle deshalb NICHT um - " +
                                "diese Zeilen gehoeren zu keinem Elternsatz und sind vorher " +
                                "zu klaeren.");
                    }

                    indizes = Neubau(v, tabelle, spalten);

                    // ---- DER ZEUGE NACHHER, noch INNERHALB der Transaktion: Die neu
                    //      gebaute Tabelle traegt keine verletzte Beziehung. Sonst bleibt
                    //      die Tabelle, wie sie war.
                    long verletzt = Zahl(v, "SELECT COUNT(*) FROM pragma_foreign_key_check(?)",
                                         new DbParam("p1", tabelle));
                    if (verletzt > 0)
                        throw new InvalidOperationException(
                            "Nach dem Umbau meldet foreign_key_check fuer " + tabelle + " " +
                            verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");

                    zeilenNachher = Zahl(v, "SELECT COUNT(*) FROM \"" + tabelle + "\"");
                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN - die Klammer
                    // fuer einen Abbruch zwischen den beiden PRAGMAs (Muster aus Schritt 96).
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }

            Notiere(bericht,
                    tabelle + ": Vorgabe " + VORGABE + " entfernt fuer " +
                    string.Join(", ", spalten.ToArray()) + "; Zeilen " +
                    zeilenVorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                    zeilenNachher.ToString(CultureInfo.InvariantCulture) + ", Index wieder " +
                    indizes.ToString(CultureInfo.InvariantCulture) + ".");
            return true;
        }

        /// <summary>
        /// Baut ALLE betroffenen Tabellen um, jede in ihrer eigenen Transaktion.
        /// </summary>
        /// <returns>Die Zahl der tatsächlich umgebauten Tabellen.</returns>
        public static int Alle(IList<string> bericht)
        {
            int umgebaut = 0;
            foreach (string tabelle in Tabellen())
                if (Umbauen(tabelle, bericht)) umgebaut++;
            return umgebaut;
        }

        /// <summary>
        /// Das Tabellenneubau-Rezept aus Schemaschritt 96, Wort für Wort: Die alte
        /// Tabelle weicht auf den Hilfsnamen aus, die neue entsteht unter dem ECHTEN
        /// Namen, die Zeilen ziehen namentlich um, der AUTOINCREMENT-Stand reist mit,
        /// die alte fällt, die Indizes stehen wieder.
        /// </summary>
        /// <returns>Die Zahl der wieder angelegten Indizes.</returns>
        private static int Neubau(DbVorgang v, string tabelle, IList<string> spalten)
        {
            string alt = Hilfsname(tabelle);

            string bestand = Text(v.Skalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("p1", tabelle)));
            string ziel = Zieltext(tabelle, bestand, spalten);

            // Spalten NAMENTLICH - "SELECT *" haengt an der Reihenfolge zweier Schemastaende.
            var namen = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("p1", tabelle));
            foreach (DataRow zeile in info.Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen.ToArray());

            var indizes = new List<string>();
            DataTable idx = v.Lese(
                "SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = ? AND sql IS NOT NULL",
                new DbParam("p1", tabelle));
            foreach (DataRow zeile in idx.Rows)
                indizes.Add(Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture));

            object standWert = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?",
                                        new DbParam("p1", tabelle));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");

            // Seit SQLite 3.25 prueft ALTER TABLE ... RENAME jede Sicht neu - und mehrere
            // Sichten nennen diese Tabellen beim Namen, den sie am Ende wieder tragen.
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " +
                         spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");

            // DER ZAEHLER. Ohne ihn fiele der AUTOINCREMENT-Stand auf die groesste
            // kopierte ID zurueck, und eine vergebene Id kaeme ein zweites Mal heraus.
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", alt));
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("p1", tabelle));
            if (standWert != null && standWert != DBNull.Value)
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                             new DbParam("p1", tabelle),
                             new DbParam("p2", Convert.ToInt64(standWert, CultureInfo.InvariantCulture)));

            foreach (string anweisung in indizes)
                v.Ausfuehren(ProjektFremdschluessel.MitIfNotExists(anweisung));

            return indizes.Count;
        }

        // =================================================================
        //  Kleinkram
        // =================================================================

        private static long Zahl(DbVorgang v, string sql, params DbParam[] parameter)
        {
            object wert = v.Skalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
        {
            return wert == null || wert == DBNull.Value
                ? null
                : Convert.ToString(wert, CultureInfo.InvariantCulture);
        }

        private static void Notiere(IList<string> bericht, string zeile)
        {
            if (bericht != null) bericht.Add(zeile);
        }
    }
}
