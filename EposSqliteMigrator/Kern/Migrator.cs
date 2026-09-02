using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;

namespace EposSqliteMigrator.Kern;

/// <summary>
/// Der Migrator: liest einen EPOS-Plan-Bestand (Access/ACE, ausschliesslich SELECT)
/// und schreibt eine SQLite-Datei nach dem eingebetteten Zielschema aus sql\schema.
/// Die .accdb wird nie veraendert - sie ist das Rollback. Bei jedem Fehler wird die
/// Zieldatei geloescht.
/// </summary>
public sealed class Migrator
{
    private const string Res001 = "EposSqliteMigrator.Kern.Schema.001_grundschema.sql";
    private const string Res002 = "EposSqliteMigrator.Kern.Schema.002_views.sql";
    private const string Res003 = "EposSqliteMigrator.Kern.Schema.003_indizes_fk.sql";
    private const string ResInv = "EposSqliteMigrator.Kern.Schema.inventar.json";

    private const int SollSchemaVersion = 61;

    /// <summary>Zusaetzliche Spalten fuer den Case-Drift-Messlauf (D5) neben allen
    /// TEXT-Spalten aus UNIQUE-Indizes.</summary>
    private static readonly (string Tabelle, string Spalte)[] CaseDriftZusatz =
    {
        ("Tab_Projekt", "Projektname"),
        ("Tab_Energieanlagen", "Bezeichner"),
        ("Tab_Energieanlagen", "WQ_Puffer"),
        ("Tab_Pufferspeicher", "Bezeichner"),
        ("energy_carrier", "code"),
        ("emissionsart", "kuerzel"),
    };

    private enum Wandlung { Bool, Datum, Ganzzahl, Real, Text }

    private readonly record struct SpaltenPlan(string Name, Wandlung Art, string DaoTypName);

    private readonly Action<string> _melden;

    /// <summary>Nur eine selbst angelegte Zieldatei darf im Fehlerfall geloescht werden -
    /// eine bereits vorhandene fremde Datei niemals.</summary>
    private bool _zielSelbstAngelegt;

    public Migrator(Action<string>? fortschritt = null)
        => _melden = fortschritt ?? (_ => { });

    public static string Version =>
        typeof(Migrator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Migrator).Assembly.GetName().Version?.ToString()
        ?? "unbekannt";

    // ---------------------------------------------------------------- Ablauf

    public MigrationsErgebnis Ausfuehren(MigrationsOptionen opt)
    {
        var uhr = Stopwatch.StartNew();
        var start = DateTime.Now;
        var erg = new MigrationsErgebnis
        {
            Quelle = Path.GetFullPath(opt.Quelle),
            Ziel = Path.GetFullPath(opt.Ziel),
            Start = start,
            OrphanPolicy = opt.OrphanPolicy,
            Werkzeugversion = Version,
        };
        erg.BerichtPfad = opt.BerichtPfad(start);
        _zielSelbstAngelegt = false;

        try
        {
            Durchfuehren(opt, erg);
        }
        catch (MigrationsAbbruch a)
        {
            erg.Code = a.Code;
            erg.Fehlermeldung = a.Message;
            if (_zielSelbstAngelegt) ZielLoeschen(erg.Ziel);
        }
        catch (Exception ex)
        {
            erg.Code = ExitCode.Fehler;
            erg.Fehlermeldung = ex.Message;
            if (_zielSelbstAngelegt) ZielLoeschen(erg.Ziel);
        }

        erg.Dauer = uhr.Elapsed;
        if (File.Exists(erg.Ziel)) erg.ZielBytes = new FileInfo(erg.Ziel).Length;

        try
        {
            Bericht.Schreiben(erg);
        }
        catch (Exception ex)
        {
            erg.Warnungen.Add($"Bericht konnte nicht geschrieben werden: {ex.Message}");
        }

        return erg;
    }

    private void Durchfuehren(MigrationsOptionen opt, MigrationsErgebnis erg)
    {
        // --- Schritt 1: Waechter -------------------------------------------------
        if (!File.Exists(erg.Quelle))
            throw new MigrationsAbbruch(ExitCode.Fehler, $"Quelldatei nicht gefunden: {erg.Quelle}");

        var sperrdatei = Path.ChangeExtension(erg.Quelle, ".laccdb");
        if (File.Exists(sperrdatei))
            throw new MigrationsAbbruch(ExitCode.SitzungOffen,
                "Die Quelldatenbank ist geoeffnet: neben ihr liegt die Sperrdatei " +
                $"'{Path.GetFileName(sperrdatei)}'. Bitte EPOS-Plan und Access schliessen " +
                "(auch auf anderen Rechnern) und den Lauf wiederholen.");

        if (File.Exists(erg.Ziel))
            throw new MigrationsAbbruch(ExitCode.Fehler,
                $"Zieldatei existiert bereits und wird niemals ueberschrieben: {erg.Ziel}");

        var zielOrdner = Path.GetDirectoryName(erg.Ziel);
        if (!string.IsNullOrEmpty(zielOrdner)) Directory.CreateDirectory(zielOrdner);

        var qi = new FileInfo(erg.Quelle);
        erg.QuelleBytes = qi.Length;
        erg.QuelleGeaendert = qi.LastWriteTime;

        var inventar = Inventar.AusText(Ressource(ResInv));
        var whitelist = inventar.TabellenNamenSortiert();
        _melden($"Inventar: {whitelist.Count} Tabellen (Whitelist aus dem Zielschema).");

        // --- Schritt 2: Quelle oeffnen (nur lesend, nur SELECT) ------------------
        using var quelle = QuelleOeffnen(erg);

        // --- Schritt 3: Versionspruefung ----------------------------------------
        erg.SchemaVersion = SchemaVersionLesen(quelle);
        if (erg.SchemaVersion != SollSchemaVersion)
            throw new MigrationsAbbruch(ExitCode.Fehler,
                $"Schemastand der Quelle ist {erg.SchemaVersion}, erwartet {SollSchemaVersion}. " +
                "Bitte zuerst die letzte Access-Fassung von EPOS-Plan starten " +
                $"(hebt den Bestand auf Stand {SollSchemaVersion}).");
        _melden($"Schemastand der Quelle: {erg.SchemaVersion} - in Ordnung.");

        // Quelltabellen erfassen: Whitelist-Abgleich und "nicht migriert"-Liste
        var (quelltabellen, quellabfragen) = QuellobjekteLesen(quelle, erg);
        var whitelistSatz = new HashSet<string>(whitelist, StringComparer.OrdinalIgnoreCase);
        foreach (var t in quelltabellen.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            if (!whitelistSatz.Contains(t)) erg.NichtMigriert.Add(t);

        var fehlend = whitelist.Where(t => !quelltabellen.Contains(t)).ToList();
        if (fehlend.Count > 0)
            throw new MigrationsAbbruch(ExitCode.Fehler,
                "In der Quelle fehlen Tabellen des Zielschemas: " + string.Join(", ", fehlend));

        // --- Schritt 4: Ziel anlegen --------------------------------------------
        // ForeignKeys=false ist Pflicht: Microsoft.Data.Sqlite schaltet die
        // Fremdschluesselpruefung beim Oeffnen von sich aus EIN (anders als SQLite selbst).
        // Ohne diese Angabe scheitert der Ladelauf an der Reihenfolge der Tabellen.
        using var ziel = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = erg.Ziel,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            ForeignKeys = false,
        }.ToString());
        _zielSelbstAngelegt = true;   // ab hier gehoert die Zieldatei diesem Lauf
        ziel.Open();

        Sql(ziel, "PRAGMA foreign_keys = OFF;");
        Sql(ziel, "PRAGMA journal_mode = MEMORY;");
        Sql(ziel, "PRAGMA synchronous = OFF;");

        _melden("Zielschema einspielen: 001_grundschema.sql");
        Sql(ziel, Ressource(Res001));
        _melden("Zielschema einspielen: 002_views.sql");
        Sql(ziel, Ressource(Res002));
        _melden("Zielschema einspielen: 003_indizes_fk.sql");
        Sql(ziel, Ressource(Res003));

        // Nachweis statt Vertrauen: der Ladelauf laeuft nur mit ausgeschalteter Pruefung.
        Sql(ziel, "PRAGMA foreign_keys = OFF;");
        long fkStand = Skalar(ziel, "PRAGMA foreign_keys;");
        if (fkStand != 0)
            throw new MigrationsAbbruch(ExitCode.Fehler,
                "PRAGMA foreign_keys liess sich nicht ausschalten (Stand: " + fkStand +
                "). Der Ladelauf braucht sie aus; geprueft wird spaeter mit foreign_key_check.");
        _melden("Ladelauf-PRAGMAs gesetzt (foreign_keys=OFF, journal_mode=MEMORY, synchronous=OFF).");

        // Gespeicherte Access-Abfragen ohne Gegenstueck im Ziel benennen (S2-Entscheidung).
        var zielViews = new HashSet<string>(ZielViews(ziel), StringComparer.OrdinalIgnoreCase);
        foreach (var a in quellabfragen.Where(a => !zielViews.Contains(a))
                                       .OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
            erg.AbfragenOhneView.Add(a);

        // --- Schritt 5: Kopieren je Tabelle -------------------------------------
        int nr = 0;
        foreach (var tab in whitelist)
        {
            nr++;
            var te = TabelleKopieren(quelle, ziel, tab, inventar.Tabellen[tab], nr, whitelist.Count);
            erg.Tabellen.Add(te);
        }

        // --- Schritt 6: Autowert-Staende ----------------------------------------
        SequenzenPruefen(ziel, inventar, erg);

        // --- Schritt 7: Integritaet ---------------------------------------------
        FremdschluesselPruefen(ziel, erg);
        IntegritaetPruefen(ziel, erg);

        if (erg.FkVerletzungen.Count > 0 && opt.OrphanPolicy == OrphanPolicy.Abbruch)
            throw new MigrationsAbbruch(ExitCode.Waisen,
                $"foreign_key_check meldet {erg.FkVerletzungen.Count} Verletzung(en); " +
                "orphanPolicy=Abbruch - die Zieldatei wurde geloescht. " +
                "Die Liste steht im Bericht. Mit --orphanPolicy AlsProtokollAussetzen " +
                "bleibt die Datei erhalten (der Constraint bleibt bestehen, die Verletzung wird ausgehalten).");

        // --- Schritt 8: Datenbeweis ---------------------------------------------
        DatenbeweisZiel(ziel, inventar, erg);

        var abweichend = erg.Tabellen.Where(t => !t.Ok).ToList();
        if (abweichend.Count > 0)
            throw new MigrationsAbbruch(ExitCode.BeweisFehlgeschlagen,
                "Der Datenbeweis ist fehlgeschlagen fuer: " +
                string.Join(", ", abweichend.Select(t => t.Name)) +
                ". Die Zieldatei wurde geloescht; die Differenzliste steht im Bericht.");

        // --- Schritt 9: Case-Drift-Messlauf (D5) --------------------------------
        CaseDriftMessen(ziel, inventar, erg);

        // --- Schritt 10: Abschluss ----------------------------------------------
        using (var cmd = ziel.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode = WAL;";
            var modus = Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            if (!string.Equals(modus, "wal", StringComparison.OrdinalIgnoreCase))
                erg.Warnungen.Add($"journal_mode konnte nicht auf WAL gesetzt werden (Rueckgabe: {modus}).");
        }
        Sql(ziel, "PRAGMA synchronous = NORMAL;");
        Sql(ziel, "PRAGMA wal_checkpoint(TRUNCATE);");
        ziel.Close();

        erg.Code = ExitCode.Erfolg;
        _melden("Migration abgeschlossen.");
    }

    // ---------------------------------------------------------------- Quelle

    private OleDbConnection QuelleOeffnen(MigrationsErgebnis erg)
    {
        var basis = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={erg.Quelle};";

        try
        {
            var c = new OleDbConnection(basis + "Mode=Read;");
            c.Open();
            erg.NurLesendGeoeffnet = true;
            erg.OeffnungsVermerk = "Mode=Read (nur lesend)";
            _melden("Quelle geoeffnet: Mode=Read.");
            return c;
        }
        catch (Exception ex)
        {
            erg.NurLesendGeoeffnet = false;
            erg.OeffnungsVermerk =
                $"Mode=Read nicht moeglich ({ex.Message.Replace("\r", " ").Replace("\n", " ").Trim()}); " +
                "normal geoeffnet. Es wurden ausschliesslich SELECT-Anweisungen abgesetzt.";
            erg.Warnungen.Add("Quelle konnte nicht mit Mode=Read geoeffnet werden - siehe Kopf des Berichts.");
            _melden("Quelle: Mode=Read nicht moeglich, oeffne normal (nur SELECTs).");
            var c = new OleDbConnection(basis);
            c.Open();
            return c;
        }
    }

    private static int SchemaVersionLesen(OleDbConnection quelle)
    {
        using var cmd = new OleDbCommand("SELECT MAX(SchemaVersion) FROM Tab_Applikation", quelle);
        var v = cmd.ExecuteScalar();
        if (v is null || v is DBNull)
            throw new MigrationsAbbruch(ExitCode.Fehler,
                "Tab_Applikation enthaelt keinen Schemastand. " +
                "Bitte zuerst die letzte Access-Fassung von EPOS-Plan starten " +
                $"(hebt den Bestand auf Stand {SollSchemaVersion}).");
        return Convert.ToInt32(v, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Liest das OleDb-Schema-Rowset 'Tables'. Migrationsgegenstand sind nur TABLE und LINK;
    /// die uebrigen Arten werden gezaehlt und im Bericht genannt, damit nichts stumm bleibt.
    /// </summary>
    private static (HashSet<string> Tabellen, List<string> Abfragen) QuellobjekteLesen(
        OleDbConnection quelle, MigrationsErgebnis erg)
    {
        var satz = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var abfragen = new List<string>();

        var rs = quelle.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        if (rs is null) return (satz, abfragen);

        foreach (DataRow r in rs.Rows)
        {
            var typ = Convert.ToString(r["TABLE_TYPE"], CultureInfo.InvariantCulture) ?? "(ohne Typ)";
            var name = Convert.ToString(r["TABLE_NAME"], CultureInfo.InvariantCulture);
            erg.QuellobjektArten[typ] = erg.QuellobjektArten.TryGetValue(typ, out var n) ? n + 1 : 1;
            if (string.IsNullOrEmpty(name)) continue;

            if (typ.Equals("TABLE", StringComparison.OrdinalIgnoreCase) ||
                typ.Equals("LINK", StringComparison.OrdinalIgnoreCase))
                satz.Add(name!);
            else if (typ.Equals("VIEW", StringComparison.OrdinalIgnoreCase))
                abfragen.Add(name!);
        }
        return (satz, abfragen);
    }

    private static List<string> ZielViews(SqliteConnection ziel)
    {
        var liste = new List<string>();
        using var cmd = ziel.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'view' ORDER BY name";
        using var r = cmd.ExecuteReader();
        while (r.Read()) liste.Add(r.GetString(0));
        return liste;
    }

    // ---------------------------------------------------------------- Kopieren

    private TabellenErgebnis TabelleKopieren(
        OleDbConnection quelle, SqliteConnection ziel,
        string tab, InventarTabelle def, int nr, int gesamt)
    {
        var uhr = Stopwatch.StartNew();
        var erg = new TabellenErgebnis { Name = tab };

        var spalten = def.Spalten.OrderBy(s => s.Ordinal).ToList();
        var plan = spalten.Select(s => new SpaltenPlan(s.Name, WandlungFuer(tab, s), s.DaoTypName)).ToArray();

        using (var zaehl = new OleDbCommand($"SELECT COUNT(*) FROM [{tab}]", quelle))
            erg.QuelleCount = Convert.ToInt64(zaehl.ExecuteScalar(), CultureInfo.InvariantCulture);

        var quellSql = "SELECT " + string.Join(",", spalten.Select(s => $"[{s.Name}]")) + $" FROM [{tab}]";
        var zielSql = $"INSERT INTO \"{Q(tab)}\" (" +
                      string.Join(",", spalten.Select(s => $"\"{Q(s.Name)}\"")) + ") VALUES (" +
                      string.Join(",", spalten.Select((_, i) => "@p" + i.ToString(CultureInfo.InvariantCulture))) + ")";

        var pruef = new PruefsummeAggregat();
        long zeilen = 0;

        using (var tx = ziel.BeginTransaction())
        using (var ein = ziel.CreateCommand())
        {
            ein.Transaction = tx;
            ein.CommandText = zielSql;
            var pars = new SqliteParameter[spalten.Count];
            for (int i = 0; i < spalten.Count; i++)
                pars[i] = ein.Parameters.Add(new SqliteParameter("@p" + i.ToString(CultureInfo.InvariantCulture), DBNull.Value));
            ein.Prepare();

            using var lese = new OleDbCommand(quellSql, quelle);
            using var reader = lese.ExecuteReader();
            var werte = new object[spalten.Count];

            while (reader.Read())
            {
                reader.GetValues(werte);
                pruef.ZeileBeginnen();

                for (int i = 0; i < plan.Length; i++)
                {
                    var (wert, kanon) = Wandeln(tab, plan[i], werte[i]);
                    pars[i].Value = wert;
                    pruef.Feld(kanon);
                }

                pruef.ZeileAbschliessen();
                ein.ExecuteNonQuery();
                zeilen++;

                if (zeilen % 50000 == 0)
                    _melden($"        ... {zeilen:N0} Zeilen");
            }

            tx.Commit();
        }

        erg.QuelleGelesen = zeilen;
        erg.QuellePruefsumme = pruef.Hex();
        erg.Sekunden = uhr.Elapsed.TotalSeconds;

        _melden($"[{nr,3}/{gesamt}] {tab,-40} {zeilen,9:N0} Zeilen  ({erg.Sekunden,6:F2} s)");

        if (erg.QuelleCount != zeilen)
            throw new MigrationsAbbruch(ExitCode.BeweisFehlgeschlagen,
                $"Tabelle {tab}: COUNT(*) meldet {erg.QuelleCount}, gelesen wurden {zeilen} Zeilen.");

        return erg;
    }

    private static Wandlung WandlungFuer(string tab, InventarSpalte s) => s.DaoTypName switch
    {
        "dbBoolean" => Wandlung.Bool,
        "dbDate" => Wandlung.Datum,
        "dbLong" => Wandlung.Ganzzahl,
        "dbDouble" => Wandlung.Real,
        "dbText" or "dbMemo" => Wandlung.Text,
        _ => throw new MigrationsAbbruch(ExitCode.Fehler,
            $"Unbekannter DAO-Typ '{s.DaoTypName}' in {tab}.{s.Name} - das Inventar deckt ihn nicht ab."),
    };

    /// <summary>Typgetriebene Wandlung eines Quellwerts: Parameterwert + kanonischer Text.</summary>
    private static (object Wert, string Kanon) Wandeln(string tab, SpaltenPlan p, object roh)
    {
        bool leer = roh is null || roh is DBNull;

        switch (p.Art)
        {
            case Wandlung.Bool:
                // D3-a: DBNull -> 0. Ausschliesslich bei Boolean.
                if (leer) return (0L, "0");
                if (roh is bool b) return (b ? 1L : 0L, b ? "1" : "0");
                throw Typfehler(tab, p, roh!);

            case Wandlung.Datum:
                if (leer) return (DBNull.Value, Kanon.NullMarke);
                if (roh is DateTime dt)
                {
                    var s = Kanon.Datum(dt);
                    return (s, s);
                }
                throw Typfehler(tab, p, roh!);

            case Wandlung.Ganzzahl:
                if (leer) return (DBNull.Value, Kanon.NullMarke);
                {
                    long v = roh switch
                    {
                        long l => l,
                        int i => i,
                        short sh => sh,
                        ushort us => us,
                        byte by => by,
                        sbyte sb => sb,
                        uint ui => ui,
                        _ => throw Typfehler(tab, p, roh!),
                    };
                    return (v, Kanon.Ganzzahl(v));
                }

            case Wandlung.Real:
                if (leer) return (DBNull.Value, Kanon.NullMarke);
                {
                    double d = roh switch
                    {
                        double dd => dd,
                        float f => f,
                        decimal m => (double)m,
                        long l => l,
                        int i => i,
                        short sh => sh,
                        byte by => by,
                        _ => throw Typfehler(tab, p, roh!),
                    };
                    return (d, Kanon.Real(d));
                }

            case Wandlung.Text:
                if (leer) return (DBNull.Value, Kanon.NullMarke);
                {
                    string s = roh switch
                    {
                        string st => st,
                        char c => c.ToString(),
                        _ => throw Typfehler(tab, p, roh!),
                    };
                    return (s, s);
                }

            default:
                throw new MigrationsAbbruch(ExitCode.Fehler, $"Unbehandelte Wandlung in {tab}.{p.Name}.");
        }
    }

    private static MigrationsAbbruch Typfehler(string tab, SpaltenPlan p, object roh) =>
        new(ExitCode.Fehler,
            $"Unerwarteter CLR-Typ in {tab}.{p.Name}: {roh.GetType().FullName} " +
            $"(Inventar: {p.DaoTypName}, erwartete Wandlung: {p.Art}). " +
            "Der Wert wird nicht stillschweigend umgedeutet - der Lauf bricht ab.");

    // ---------------------------------------------------------------- Autowerte

    private void SequenzenPruefen(SqliteConnection ziel, Inventar inventar, MigrationsErgebnis erg)
    {
        _melden("Autowert-Staende pruefen (sqlite_sequence).");

        foreach (var name in inventar.TabellenNamenSortiert())
        {
            var def = inventar.Tabellen[name];
            if (string.IsNullOrEmpty(def.Autowert)) continue;

            long max;
            bool leer;
            using (var cmd = ziel.CreateCommand())
            {
                cmd.CommandText = $"SELECT MAX(\"{Q(def.Autowert!)}\") FROM \"{Q(name)}\"";
                var v = cmd.ExecuteScalar();
                leer = v is null || v is DBNull;
                max = leer ? 0 : Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }

            long? seq;
            using (var cmd = ziel.CreateCommand())
            {
                cmd.CommandText = "SELECT seq FROM sqlite_sequence WHERE name = $n";
                cmd.Parameters.AddWithValue("$n", name);
                var v = cmd.ExecuteScalar();
                seq = (v is null || v is DBNull) ? null : Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }

            if (leer)
            {
                if (seq is not null && seq.Value != 0)
                    erg.SeqBefunde.Add(new SeqBefund(name, def.Autowert!, 0, seq.Value, seq.Value,
                        "Tabelle leer, sqlite_sequence traegt dennoch einen Stand - belassen."));
                continue;
            }

            if (seq is null)
            {
                using var cmd = ziel.CreateCommand();
                cmd.CommandText = "INSERT INTO sqlite_sequence (name, seq) VALUES ($n, $s)";
                cmd.Parameters.AddWithValue("$n", name);
                cmd.Parameters.AddWithValue("$s", max);
                cmd.ExecuteNonQuery();
                erg.SeqBefunde.Add(new SeqBefund(name, def.Autowert!, max, -1, max,
                    "Zeile in sqlite_sequence fehlte, mit MAX(id) nachgetragen."));
            }
            else if (seq.Value < max)
            {
                using var cmd = ziel.CreateCommand();
                cmd.CommandText = "UPDATE sqlite_sequence SET seq = $s WHERE name = $n";
                cmd.Parameters.AddWithValue("$n", name);
                cmd.Parameters.AddWithValue("$s", max);
                cmd.ExecuteNonQuery();
                erg.SeqBefunde.Add(new SeqBefund(name, def.Autowert!, max, seq.Value, max,
                    "seq lag unter MAX(id) und wurde angehoben."));
            }
        }
    }

    // ---------------------------------------------------------------- Integritaet

    private void FremdschluesselPruefen(SqliteConnection ziel, MigrationsErgebnis erg)
    {
        _melden("PRAGMA foreign_key_check ...");
        using var cmd = ziel.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_key_check;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            erg.FkVerletzungen.Add(new FkVerletzung(
                r.IsDBNull(0) ? "?" : r.GetString(0),
                r.IsDBNull(1) ? "(ohne rowid)" : Convert.ToString(r.GetValue(1), CultureInfo.InvariantCulture) ?? "?",
                r.IsDBNull(2) ? "?" : r.GetString(2),
                r.IsDBNull(3) ? "?" : Convert.ToString(r.GetValue(3), CultureInfo.InvariantCulture) ?? "?"));
        }
        _melden(erg.FkVerletzungen.Count == 0
            ? "  keine Fremdschluesselverletzungen."
            : $"  {erg.FkVerletzungen.Count} Fremdschluesselverletzung(en) gefunden.");
    }

    private void IntegritaetPruefen(SqliteConnection ziel, MigrationsErgebnis erg)
    {
        _melden("PRAGMA integrity_check ...");
        var zeilen = new List<string>();
        using (var cmd = ziel.CreateCommand())
        {
            cmd.CommandText = "PRAGMA integrity_check;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) zeilen.Add(r.IsDBNull(0) ? "?" : r.GetString(0));
        }
        erg.IntegrityCheck = zeilen.Count == 0 ? "(leer)" : string.Join(" | ", zeilen);

        if (zeilen.Count != 1 || !zeilen[0].Equals("ok", StringComparison.OrdinalIgnoreCase))
            throw new MigrationsAbbruch(ExitCode.Fehler, "integrity_check meldet: " + erg.IntegrityCheck);
        _melden("  integrity_check: ok.");
    }

    // ---------------------------------------------------------------- Datenbeweis

    private void DatenbeweisZiel(SqliteConnection ziel, Inventar inventar, MigrationsErgebnis erg)
    {
        _melden("Datenbeweis: Zielseite messen (Zeilenzahl und Inhaltspruefsumme).");

        foreach (var te in erg.Tabellen)
        {
            var def = inventar.Tabellen[te.Name];
            var spalten = def.Spalten.OrderBy(s => s.Ordinal).ToList();
            var arten = spalten.Select(s => WandlungFuer(te.Name, s)).ToArray();

            using (var cmd = ziel.CreateCommand())
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM \"{Q(te.Name)}\"";
                te.ZielCount = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            var pruef = new PruefsummeAggregat();
            using (var cmd = ziel.CreateCommand())
            {
                cmd.CommandText = "SELECT " + string.Join(",", spalten.Select(s => $"\"{Q(s.Name)}\"")) +
                                  $" FROM \"{Q(te.Name)}\"";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    pruef.ZeileBeginnen();
                    for (int i = 0; i < arten.Length; i++)
                    {
                        if (r.IsDBNull(i)) { pruef.Feld(Kanon.NullMarke); continue; }
                        pruef.Feld(arten[i] switch
                        {
                            Wandlung.Bool => r.GetInt64(i) != 0 ? "1" : "0",
                            Wandlung.Ganzzahl => Kanon.Ganzzahl(r.GetInt64(i)),
                            Wandlung.Real => Kanon.Real(r.GetDouble(i)),
                            _ => r.GetString(i),   // Text, Memo und Datum (ISO-Text unveraendert)
                        });
                    }
                    pruef.ZeileAbschliessen();
                }
            }
            te.ZielPruefsumme = pruef.Hex();
        }

        int ok = erg.Tabellen.Count(t => t.Ok);
        _melden($"  {ok}/{erg.Tabellen.Count} Tabellen mit gleicher Zeilenzahl und gleicher Pruefsumme.");
    }

    // ---------------------------------------------------------------- Case-Drift

    private void CaseDriftMessen(SqliteConnection ziel, Inventar inventar, MigrationsErgebnis erg)
    {
        _melden("Case-Drift-Messlauf (D5) ...");

        var kandidaten = new List<(string Tabelle, string Spalte)>();
        var gesehen = new HashSet<string>(StringComparer.Ordinal);

        void Aufnehmen(string tab, string spalte)
        {
            if (!inventar.Tabellen.TryGetValue(tab, out var d)) return;
            var s = d.Spalten.FirstOrDefault(x => x.Name.Equals(spalte, StringComparison.OrdinalIgnoreCase));
            if (s is null || s.SqliteTyp != "TEXT") return;
            if (gesehen.Add(tab + "." + s.Name)) kandidaten.Add((tab, s.Name));
        }

        foreach (var tab in inventar.TabellenNamenSortiert())
        {
            var def = inventar.Tabellen[tab];
            var typ = def.Spalten.ToDictionary(s => s.Name, s => s.SqliteTyp, StringComparer.OrdinalIgnoreCase);
            foreach (var ix in def.Indizes.Where(i => i.Unique))
                foreach (var sp in ix.Spalten)
                    if (typ.TryGetValue(sp, out var t) && t == "TEXT") Aufnehmen(tab, sp);
        }
        foreach (var (tab, sp) in CaseDriftZusatz) Aufnehmen(tab, sp);

        foreach (var (tab, sp) in kandidaten)
        {
            erg.CaseDriftGeprueft.Add($"{tab}.{sp}");

            var treffer = new List<string>();
            using (var cmd = ziel.CreateCommand())
            {
                cmd.CommandText =
                    $"SELECT lower(\"{Q(sp)}\") AS k FROM \"{Q(tab)}\" WHERE \"{Q(sp)}\" IS NOT NULL " +
                    $"GROUP BY lower(\"{Q(sp)}\") HAVING COUNT(DISTINCT \"{Q(sp)}\") > 1";
                using var r = cmd.ExecuteReader();
                while (r.Read()) treffer.Add(r.IsDBNull(0) ? string.Empty : r.GetString(0));
            }

            foreach (var k in treffer)
            {
                var werte = new List<string>();
                using var cmd = ziel.CreateCommand();
                cmd.CommandText = $"SELECT DISTINCT \"{Q(sp)}\" FROM \"{Q(tab)}\" WHERE lower(\"{Q(sp)}\") = $k ORDER BY 1";
                cmd.Parameters.AddWithValue("$k", k);
                using var r = cmd.ExecuteReader();
                while (r.Read()) werte.Add(r.IsDBNull(0) ? "(NULL)" : r.GetString(0));
                erg.CaseDrifts.Add(new CaseDrift(tab, sp, k, werte));
            }
        }

        _melden(erg.CaseDrifts.Count == 0
            ? $"  {kandidaten.Count} Textschluessel geprueft, kein Case-Drift."
            : $"  {erg.CaseDrifts.Count} Case-Drift-Befund(e) in {kandidaten.Count} geprueften Textschluesseln.");
    }

    // ---------------------------------------------------------------- Hilfen

    private static void Sql(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static long Skalar(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        var v = cmd.ExecuteScalar();
        return (v is null || v is DBNull) ? -1 : Convert.ToInt64(v, CultureInfo.InvariantCulture);
    }

    /// <summary>Bezeichner fuer doppelt gequotete SQLite-Namen absichern.</summary>
    private static string Q(string bezeichner) => bezeichner.Replace("\"", "\"\"");

    private static string Ressource(string name)
    {
        using var s = typeof(Migrator).Assembly.GetManifestResourceStream(name)
                      ?? throw new MigrationsAbbruch(ExitCode.Fehler,
                          $"Eingebettete Ressource fehlt: {name}. Der Build ist unvollstaendig.");
        using var r = new StreamReader(s, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
        return r.ReadToEnd();
    }

    private static void ZielLoeschen(string pfad)
    {
        SqliteConnection.ClearAllPools();
        foreach (var p in new[] { pfad, pfad + "-wal", pfad + "-shm", pfad + "-journal" })
        {
            for (int versuch = 0; versuch < 3; versuch++)
            {
                try
                {
                    if (File.Exists(p)) File.Delete(p);
                    break;
                }
                catch (IOException)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(120);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(120);
                }
            }
        }
    }
}
