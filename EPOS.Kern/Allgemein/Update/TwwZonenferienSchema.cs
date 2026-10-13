using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ZONENFERIEN ALS LISTE - Schemaschritt des Zapfprofilgenerators (Konzept Zapfprofil-Benutzerfuehrung,
    // ZB5 mit der Entscheidung ZB-Q3 B: beliebig viele eigene Ferienzeitraeume je Zone, wie im Gebaeudedialog).
    //
    // WOZU. Eine Zone trug ihre eigenen Ferien in vier festen Spaltenpaaren Ferienbeginn_1..4/Ferienende_1..4 an
    // Tab_TwwZone. Ab diesem Schritt stehen sie in einer eigenen Tabelle, eine Zeile je Zeitraum:
    //
    //   Tab_TwwZonenferien (STRICT)
    //     ID           INTEGER PRIMARY KEY AUTOINCREMENT
    //     ID_Zone      INTEGER NOT NULL -> Tab_TwwZone (ID) ON DELETE CASCADE   die Beziehung ueber die ID (Hausregel)
    //     Reihenfolge  INTEGER NOT NULL >= 1                                    die Position in der Liste der Zone
    //     Name         TEXT NULL                                                wahlfrei, wie die Ferienliste des Gebaeudes
    //     Beginn       INTEGER NULL, 1 .. 365                                   NULL = vom 1. Januar an
    //     Ende         INTEGER NOT NULL, 1 .. 365                               ein Zeitraum ohne Ende gilt nicht
    //     UNIQUE (ID_Zone, Reihenfolge)
    //
    // BENENNUNG UND PRUEFKLAUSELN. Tabellenname nach dem Muster der Projekttabellen des Generators (Tab_Tww*, ohne
    // _STAMM: Projektdaten). Jahrestage des Gemeinjahrs 1 .. 365 (fester Raster des Kerns, kein Schaltjahr). Die alten
    // Spalten kannten 0 und 366 als "keine Angabe" - die Tabelle nicht: Was keine Angabe ist, steht nicht als Zeile da
    // (Ende) bzw. als NULL (Beginn). Ein Zeitraum ueber den Jahreswechsel beginnt im Dezember (Beginn > Ende), die
    // Deutung bleibt beim Kern (Zapfkalender.AusJahrestagen). Die Eindeutigkeit (ID_Zone, Reihenfolge) haelt die
    // Reihenfolge eindeutig; ihr Index beginnt mit ID_Zone und traegt damit auch die Suche der Kaskade - ein eigener
    // Index auf ID_Zone entfaellt.
    //
    // DIE UEBERTRAGUNG. Je Zone die Paare n = 1 .. 4 in dieser Reihenfolge: Ein Paar mit gueltigem Ende (1 .. 365)
    // wird eine Zeile, ein ungueltiger Beginn (NULL, 0, 366) wird NULL; ein Paar ohne gueltiges Ende faellt weg - es
    // hat im Kern nie ein Ferienfenster ergeben. Die Reihenfolge zaehlt die uebertragenen Zeilen fortlaufend ab 1. Eine
    // Zone, die schon Zeilen in der neuen Tabelle traegt, wird nicht ein zweites Mal uebertragen (wiederholbar).
    //
    // DER SPALTENABBAU nach dem Rezept des Hauses (TwwBezugsartSchema.Neubau, wie die Schritte 153, 155, 200, 221):
    // Fremdschluessel AUS vor der Transaktion, der Zieltext ist der GELTENDE sqlite_master-Text ohne die acht
    // Spaltendefinitionen (alles andere Zeichen fuer Zeichen), die alte Tabelle weicht unter legacy_alter_table auf
    // einen Hilfsnamen aus, die Zeilen ziehen namentlich um, IDs und AUTOINCREMENT-Stand bleiben, Indizes stehen wieder,
    // und foreign_key_check haelt Tab_TwwZone samt ihren Kindern (Tab_TwwWohnungstyp, Tab_TwwZonenferien) noch IN der
    // Transaktion. Uebertragung und Abbau laufen in EINEM Vorgang.
    //
    // DIE ALTEN NAMEN STEHEN NUR HIER (Muster StrompreisAltspalten): Sie reisen als Argument in die Bauweise, damit der
    // SqlDialektPruefer sie gegen die nachgezogene Testdatenbank nicht als "no such column" haelt. Das Grundschema
    // (TwwSchema.SQL_CREATE_ZONE, Schritt 103) bleibt der historische Stand; dieser Schritt nimmt die Spalten weg.
    //
    // ERGEBNISNEUTRAL. Der Zapfkalender rechnet ueber die Liste dieselben Fenster wie ueber die vier Paare; kein
    // Referenzprojekt traegt eigene Zonenferien (Projekt 1045: keine).
    //
    // VIER LESER: SchemaMigration (Schale), Werkzeuge/Testdatenbankschema, die Testvorrichtung in EPOS.Kern.Tests und
    // die Paketanhebung (Art Umformung: ein aelteres Paket bringt die Paare an der Zone, die Stufe bildet daraus die
    // Zeilen der neuen Tabelle, die Spalten fallen am Ziel mit der Schnittmenge des Imports).
    // ====================================================================================

    /// <summary>
    /// <b>Zonenferien als Liste</b> — Schemaschritt (Nummer <see cref="SCHRITT"/>): die Tabelle
    /// <see cref="TAB_ZONENFERIEN"/>, die Übertragung der vier Spaltenpaare von <c>Tab_TwwZone</c> und deren Abbau. EINE
    /// Quelle für Migration, Werkzeug, Testkopie, Paketanhebung und Nachweis (ADR-001 Option C). Anlass und Bauform
    /// stehen im Kopf der Datei.
    /// </summary>
    public static class TwwZonenferienSchema
    {
        // ================================================================================
        // VORLÄUFIG: vor dem Push an 223 `KaeltebedarfSplitAbrechnungSchema` hängen (224).
        // Angemeldet ist der Schritt als 224 (hängt an 223 `KaeltebedarfSplitAbrechnungSchema` der Sitzung
        // Anlagendialog, das an 222 `BauteilkatalogHaustechnikSchema` hängt); beide liegen noch nicht auf
        // origin. Bis dahin hängt er an 221.
        // ================================================================================

        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; Migration, Werkzeug, Zielstand,
        /// Paketanhebung und Tests verweisen hierher.
        /// </summary>
        public const int SCHRITT = BauteilkatalogSchema.SCHRITT + 1;   // VORLÄUFIG, siehe oben

        /// <summary>Die neue Tabelle der eigenen Ferienzeiträume einer Zone.</summary>
        public const string TAB_ZONENFERIEN = "Tab_TwwZonenferien";

        /// <summary>Der kleinste und der größte Jahrestag eines Zeitraums (Gemeinjahr).</summary>
        public const int TAG_MIN = 1;
        public const int TAG_MAX = 365;

        /// <summary>Die Zahl der Spaltenpaare, die der Schritt überträgt und abbaut.</summary>
        public const int PAARE = 4;

        /// <summary><c>CREATE TABLE IF NOT EXISTS Tab_TwwZonenferien</c> — 6 Spalten, STRICT.</summary>
        public const string SQL_CREATE =
            "CREATE TABLE IF NOT EXISTS \"Tab_TwwZonenferien\" (\n" +
            "    \"ID\" INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
            "    \"ID_Zone\" INTEGER NOT NULL REFERENCES \"Tab_TwwZone\" (\"ID\") ON DELETE CASCADE,\n" +
            "    \"Reihenfolge\" INTEGER NOT NULL CHECK (\"Reihenfolge\" >= 1),\n" +
            "    \"Name\" TEXT,\n" +
            "    \"Beginn\" INTEGER CHECK (\"Beginn\" BETWEEN 1 AND 365),\n" +
            "    \"Ende\" INTEGER NOT NULL CHECK (\"Ende\" BETWEEN 1 AND 365),\n" +
            "    UNIQUE (\"ID_Zone\", \"Reihenfolge\")\n" +
            ") STRICT";

        /// <summary>Der Zusatz des Hilfsnamens, unter den <c>Tab_TwwZone</c> für die Dauer des Neubaus ausweicht.</summary>
        private const string HILFSZUSATZ = "_vor_Zonenferien";

        /// <summary>
        /// Der Name einer abzubauenden Spalte: <c>Ferienbeginn_n</c> bzw. <c>Ferienende_n</c> (n = 1 … <see cref="PAARE"/>).
        /// Aus einer Schleife, nie aus einer Eingabe.
        /// </summary>
        public static string Altspalte(bool beginn, int n)
            => (beginn ? "Ferienbeginn_" : "Ferienende_") + n.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die acht abzubauenden Spalten in Schemareihenfolge.</summary>
        public static IReadOnlyList<string> Altspalten()
        {
            var l = new List<string>(2 * PAARE);
            for (int n = 1; n <= PAARE; n++)
            {
                l.Add(Altspalte(true, n));
                l.Add(Altspalte(false, n));
            }
            return l;
        }

        /// <summary>Die Tabellen, die ein früherer Schritt angelegt haben muss.</summary>
        public static IReadOnlyList<string> Voraussetzungen() => new[] { TwwSchema.TAB_TWW_ZONE };

        /// <summary>Der Hilfsname, unter den die Zonentabelle für die Dauer des Neubaus ausweicht.</summary>
        public static string Hilfsname(string tabelle) => tabelle + HILFSZUSATZ;

        /// <summary>
        /// <b>Ein Paar der alten Spalten als Zeitraum</b> (Beginn, Ende) der neuen Tabelle, oder <c>null</c>, wenn es keine
        /// Zeile wird: Das Ende muss 1 … 365 sein; ein Beginn außerhalb (<c>null</c>, 0, 366) wird <c>null</c>.
        /// </summary>
        public static (int? Beginn, int Ende)? Zeitraum(int? beginn, int? ende)
        {
            if (!ende.HasValue || ende.Value < TAG_MIN || ende.Value > TAG_MAX) return null;
            int? b = beginn.HasValue && beginn.Value >= TAG_MIN && beginn.Value <= TAG_MAX ? beginn : null;
            return (b, ende.Value);
        }

        /// <summary>
        /// <b>Der Zieltext der Zonentabelle</b>: der GELTENDE <c>sqlite_master</c>-Text ohne die Definitionen der
        /// Spalten aus <paramref name="weg"/>; alles andere bleibt Zeichen für Zeichen. <c>null</c>, wenn keine davon
        /// (mehr) dasteht. Nennt eine Tabellenbedingung eine der Spalten oder ist die Tabelle nicht STRICT, bricht der
        /// Aufruf ab, statt eine falsche Tabelle anzulegen.
        /// </summary>
        public static string Zieltext(string bestand, IEnumerable<string> weg)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + TwwSchema.TAB_TWW_ZONE + " gibt es keinen CREATE-Text in sqlite_master.");
            var namen = new HashSet<string>(weg ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            int auf = bestand.IndexOf('(');
            int zu = bestand.LastIndexOf(')');
            if (auf < 0 || zu <= auf)
                throw new InvalidOperationException(TwwSchema.TAB_TWW_ZONE + ": unerwarteter CREATE-Text.");
            if (!bestand.Substring(zu).TrimEnd().Equals(") STRICT", StringComparison.Ordinal))
                throw new InvalidOperationException(TwwSchema.TAB_TWW_ZONE + " ist keine STRICT-Tabelle - der Schritt " + Nr +
                                                    " baut sie nicht um.");

            List<string> teile = TeileAufObersterEbene(bestand.Substring(auf + 1, zu - auf - 1));
            var bleiben = new List<string>(teile.Count);
            int entfernt = 0;
            foreach (string teil in teile)
            {
                string name = Spaltenname(teil);
                if (name != null && namen.Contains(name))
                {
                    entfernt++;
                    continue;
                }
                if (name == null)
                    foreach (string n in namen)
                        if (teil.IndexOf("\"" + n + "\"", StringComparison.OrdinalIgnoreCase) >= 0)
                            throw new InvalidOperationException(TwwSchema.TAB_TWW_ZONE + ": eine Tabellenbedingung nennt " + n +
                                                                " - der Schritt " + Nr + " baut die Tabelle deshalb NICHT um.");
                bleiben.Add(teil);
            }
            if (entfernt == 0) return null;
            // Die letzte verbleibende Definition endet vor der schließenden Klammer: Hing an einer entfernten
            // Definition der Zeilenumbruch vor ")", geht er an die letzte über.
            string schluss = teile[teile.Count - 1];
            string rest = string.Join(",", bleiben);
            if (!ReferenceEquals(bleiben[bleiben.Count - 1], schluss))
            {
                string nachlauf = schluss.Substring(schluss.TrimEnd().Length);
                rest = rest.TrimEnd() + nachlauf;
            }
            return bestand.Substring(0, auf + 1) + rest + bestand.Substring(zu);
        }

        /// <summary>Teilt den Rumpf eines CREATE-Texts an den Kommas der obersten Ebene (außerhalb von Klammern und Anführung).</summary>
        private static List<string> TeileAufObersterEbene(string rumpf)
        {
            var teile = new List<string>();
            var sb = new StringBuilder();
            int tiefe = 0;
            char anfuehrung = '\0';
            foreach (char c in rumpf)
            {
                if (anfuehrung != '\0')
                {
                    if (c == anfuehrung) anfuehrung = '\0';
                }
                else if (c == '"' || c == '\'' || c == '`') anfuehrung = c;
                else if (c == '(') tiefe++;
                else if (c == ')') tiefe--;
                else if (c == ',' && tiefe == 0)
                {
                    teile.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }
                sb.Append(c);
            }
            teile.Add(sb.ToString());
            return teile;
        }

        /// <summary>Der Name einer Spaltendefinition (erstes Token in Anführung); <c>null</c> bei einer Tabellenbedingung.</summary>
        private static string Spaltenname(string teil)
        {
            string t = teil.TrimStart();
            if (t.Length < 2 || t[0] != '"') return null;
            int ende = t.IndexOf('"', 1);
            return ende > 1 ? t.Substring(1, ende - 1) : null;
        }

        /// <summary>Die alten Spalten, die <c>Tab_TwwZone</c> (noch) führt.</summary>
        private static List<string> VorhandeneAltspalten(Func<string, string, bool> spalteVorhanden)
            => Altspalten().Where(s => spalteVorhanden(TwwSchema.TAB_TWW_ZONE, s)).ToList();

        /// <summary>Wie viele Handgriffe sind offen? Die fehlende Tabelle und jede verbliebene alte Spalte zählen je einmal.</summary>
        public static int Offen()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZONE)) return 1;
            int offen = DataRepository.TabelleVorhanden(TAB_ZONENFERIEN) ? 0 : 1;
            return offen + VorhandeneAltspalten(DataRepository.SpalteVorhanden).Count;
        }

        /// <summary>Steht der Schritt? Zonentabelle und Ferientabelle da, keine alte Spalte mehr.</summary>
        public static bool Vollstaendig() => Offen() == 0;

        /// <summary>
        /// <b>Führt den Schritt aus</b> — in EINEM Vorgang mit abgeschalteten Fremdschlüsseln: Tabelle anlegen, Paare
        /// übertragen, Zonentabelle ohne die alten Spalten neu bauen. Scheitert etwas, rollt der Vorgang zurück und die
        /// Ausnahme geht an den Aufrufer (Migration, Werkzeug). <b>Wiederholbar</b>. Liefert die Zahl der Handgriffe
        /// (Tabelle angelegt, Zonentabelle neu gebaut); die Zahl der übertragenen Zeiträume steht im Bericht.
        /// </summary>
        public static int Ausfuehren(IList<string> bericht)
        {
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            int handgriffe = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    bool tabelleDa = Zahl(v, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                                          new DbParam("p1", TAB_ZONENFERIEN)) > 0;
                    if (!tabelleDa)
                    {
                        v.Ausfuehren(SQL_CREATE);
                        handgriffe++;
                        zeilen.Add(TAB_ZONENFERIEN + ": angelegt");
                    }
                    else zeilen.Add("steht bereits - " + TAB_ZONENFERIEN);

                    Umformzugriff z = Zugriff(v);
                    int uebertragen = Uebertragen(z, false);
                    zeilen.Add(uebertragen.ToString(CultureInfo.InvariantCulture) +
                               " Ferienzeitraum/-raeume aus Tab_TwwZone uebertragen");

                    List<string> alt = VorhandeneAltspalten(z.SpalteVorhanden);
                    if (alt.Count > 0)
                    {
                        string bestand = Text(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                       new DbParam("p1", TwwSchema.TAB_TWW_ZONE)));
                        string ziel = Zieltext(bestand, alt);
                        long vorher = Zahl(v, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_ZONE + "\"");
                        long summeVorher = Zahl(v, "SELECT COALESCE(SUM(\"ID\"), 0) FROM \"" + TwwSchema.TAB_TWW_ZONE + "\"");
                        int objekte = TwwBezugsartSchema.Neubau(v, TwwSchema.TAB_TWW_ZONE, ziel, Hilfsname(TwwSchema.TAB_TWW_ZONE),
                                                                 new HashSet<string>(alt, StringComparer.OrdinalIgnoreCase));
                        long nachher = Zahl(v, "SELECT COUNT(*) FROM \"" + TwwSchema.TAB_TWW_ZONE + "\"");
                        long summeNachher = Zahl(v, "SELECT COALESCE(SUM(\"ID\"), 0) FROM \"" + TwwSchema.TAB_TWW_ZONE + "\"");
                        if (vorher != nachher || summeVorher != summeNachher)
                            throw new InvalidOperationException(TwwSchema.TAB_TWW_ZONE + ": " +
                                vorher.ToString(CultureInfo.InvariantCulture) + " Zeile(n) vor dem Neubau, " +
                                nachher.ToString(CultureInfo.InvariantCulture) + " danach (oder die IDs haben sich verschoben).");
                        long verletzt = TwwBezugsartSchema.Verletzt(v, TwwSchema.TAB_TWW_ZONE);
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Neubau meldet foreign_key_check fuer " +
                                TwwSchema.TAB_TWW_ZONE + " oder eine Kindtabelle " +
                                verletzt.ToString(CultureInfo.InvariantCulture) + " verletzte Zeile(n).");
                        handgriffe++;
                        zeilen.Add(TwwSchema.TAB_TWW_ZONE + ": neu gebaut ohne " + string.Join(", ", alt) + "; Zeilen " +
                                   vorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                                   nachher.ToString(CultureInfo.InvariantCulture) + ", Index/Trigger wieder " +
                                   objekte.ToString(CultureInfo.InvariantCulture));
                    }
                    else zeilen.Add("steht bereits - " + TwwSchema.TAB_TWW_ZONE + " ohne Ferienspalten");
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster aus Schritt 96).
                    try { if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            if (bericht != null)
            {
                foreach (string zeile in zeilen) bericht.Add(zeile);
                bericht.Add("foreign_key_check leer; der Zapfkalender rechnet ueber die Liste dieselben Fenster, " +
                            "der Referenzlauf bleibt gleich");
            }
            return handgriffe;
        }

        /// <summary>
        /// <b>Die Übertragung</b> der Paare jeder Zone in die Ferientabelle — EINE Regel für den Schritt und die
        /// Paketanhebung (<see cref="Paketanhebung"/>, Arbeitsdatenbank eines Pakets). Liest <c>Tab_TwwZone</c> mit den
        /// alten Spalten, die sie führt, und legt je gültigem Paar eine Zeile an (<see cref="Zeitraum"/>); eine Zone, die
        /// schon Zeilen trägt, bleibt unberührt. <paramref name="mitId"/> vergibt die ID selbst (die Arbeitsdatenbank kennt
        /// keinen AUTOINCREMENT). Liefert die Zahl der angelegten Zeilen.
        /// </summary>
        internal static int Uebertragen(Umformzugriff z, bool mitId)
        {
            List<string> alt = VorhandeneAltspalten(z.SpalteVorhanden);
            if (alt.Count == 0) return 0;

            var belegt = new HashSet<long>();
            DataTable vorhanden = z.Lesen("SELECT DISTINCT \"ID_Zone\" FROM \"" + TAB_ZONENFERIEN + "\"");
            if (vorhanden != null)
                foreach (DataRow r in vorhanden.Rows)
                {
                    long? id = Ganz(r[0]);
                    if (id.HasValue) belegt.Add(id.Value);
                }

            DataTable zonen = z.Lesen("SELECT \"ID\", " + string.Join(", ", alt.Select(s => "\"" + s + "\"")) +
                                      " FROM \"" + TwwSchema.TAB_TWW_ZONE + "\" ORDER BY \"ID\"");
            if (zonen == null) return 0;
            long naechsteId = mitId ? z.GroessteId(TAB_ZONENFERIEN) + 1L : 0L;
            int angelegt = 0;
            foreach (DataRow r in zonen.Rows)
            {
                long? zone = Ganz(r["ID"]);
                if (!zone.HasValue || belegt.Contains(zone.Value)) continue;
                int reihenfolge = 0;
                for (int n = 1; n <= PAARE; n++)
                {
                    string sb = Altspalte(true, n), se = Altspalte(false, n);
                    int? beginn = zonen.Columns.Contains(sb) ? Tag(r[sb]) : null;
                    int? ende = zonen.Columns.Contains(se) ? Tag(r[se]) : null;
                    (int? Beginn, int Ende)? zr = Zeitraum(beginn, ende);
                    if (!zr.HasValue) continue;
                    reihenfolge++;
                    object b = zr.Value.Beginn.HasValue ? (object)zr.Value.Beginn.Value : DBNull.Value;
                    if (mitId)
                        z.Ausfuehren("INSERT INTO \"" + TAB_ZONENFERIEN + "\" (\"ID\", \"ID_Zone\", \"Reihenfolge\", \"Beginn\", \"Ende\") " +
                                     "VALUES (?, ?, ?, ?, ?)",
                                     new DbParam("@id", naechsteId++), new DbParam("@zone", zone.Value),
                                     new DbParam("@reihe", reihenfolge), new DbParam("@beginn", b),
                                     new DbParam("@ende", zr.Value.Ende));
                    else
                        z.Ausfuehren("INSERT INTO \"" + TAB_ZONENFERIEN + "\" (\"ID_Zone\", \"Reihenfolge\", \"Beginn\", \"Ende\") " +
                                     "VALUES (?, ?, ?, ?)",
                                     new DbParam("@zone", zone.Value), new DbParam("@reihe", reihenfolge),
                                     new DbParam("@beginn", b), new DbParam("@ende", zr.Value.Ende));
                    angelegt++;
                }
            }
            return angelegt;
        }

        /// <summary>Der Zugriff der Übertragung im Vorgang des Schritts.</summary>
        private static Umformzugriff Zugriff(DbVorgang v) => new Umformzugriff(
            v.Lese, v.Ausfuehren, v.Skalar,
            (t, s) => Zahl(v, "SELECT COUNT(*) FROM pragma_table_info(?) WHERE name = ?",
                           new DbParam("p1", t), new DbParam("p2", s)) > 0);

        /// <summary>Ein Jahrestag aus einer Zelle (ganz, gebrochen oder Text); leer oder nicht lesbar = <c>null</c>.</summary>
        private static int? Tag(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            double d;
            if (wert is string s)
            {
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return null;
            }
            else
            {
                try { d = Convert.ToDouble(wert, CultureInfo.InvariantCulture); }
                catch (Exception) { return null; }
            }
            if (double.IsNaN(d) || double.IsInfinity(d) || d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue) return null;
            return (int)d;
        }

        private static long? Ganz(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            try { return Convert.ToInt64(wert, CultureInfo.InvariantCulture); }
            catch (Exception) { return null; }
        }

        private static long Zahl(DbVorgang v, string sql, params DbParam[] parameter)
        {
            object wert = v.Skalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(object wert)
            => wert == null || wert == DBNull.Value ? null : Convert.ToString(wert, CultureInfo.InvariantCulture);

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);
    }
}
