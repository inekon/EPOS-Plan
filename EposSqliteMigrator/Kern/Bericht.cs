using System.Globalization;
using System.Text;

namespace EposSqliteMigrator.Kern;

/// <summary>Schreibt den Migrationsbericht als Markdown (UTF-8).</summary>
public static class Bericht
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static void Schreiben(MigrationsErgebnis e)
    {
        var b = new StringBuilder();

        b.AppendLine("# Migrationsbericht EposSqliteMigrator");
        b.AppendLine();
        b.AppendLine($"Erzeugt: {e.Start.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
        b.AppendLine();

        // ------------------------------------------------------------- Kopf
        b.AppendLine("## Kopf");
        b.AppendLine();
        b.AppendLine("| Feld | Wert |");
        b.AppendLine("|---|---|");
        b.AppendLine($"| Quelle | `{e.Quelle}` |");
        b.AppendLine($"| Quellgroesse | {Mb(e.QuelleBytes)} ({e.QuelleBytes.ToString("N0", De)} Bytes) |");
        b.AppendLine($"| Quelle geaendert | {e.QuelleGeaendert:yyyy-MM-dd HH:mm:ss} |");
        b.AppendLine($"| Schemastand | {e.SchemaVersion} |");
        b.AppendLine($"| Oeffnungsart | {e.OeffnungsVermerk} |");
        b.AppendLine($"| Ziel | `{e.Ziel}` |");
        b.AppendLine($"| Zielgroesse | {(e.ZielBytes > 0 ? Mb(e.ZielBytes) : "(Datei nicht vorhanden - Lauf abgebrochen)")} |");
        b.AppendLine($"| Dauer | {e.Dauer.TotalSeconds.ToString("F2", De)} s |");
        b.AppendLine($"| Werkzeugversion | {e.Werkzeugversion} |");
        b.AppendLine($"| orphanPolicy | {e.OrphanPolicy} |");
        b.AppendLine($"| Tabellen migriert | {e.Tabellen.Count} |");
        b.AppendLine($"| Zeilen gesamt (Ziel) | {e.ZeilenGesamt.ToString("N0", De)} |");
        b.AppendLine($"| Exit-Code | {e.Code} ({CodeText(e.Code)}) |");
        b.AppendLine();

        if (e.Fehlermeldung is not null)
        {
            b.AppendLine("> **Abbruch:** " + e.Fehlermeldung);
            b.AppendLine();
        }

        b.AppendLine("Die Quelldatenbank wurde ausschliesslich lesend angesprochen (nur SELECT). ");
        b.AppendLine("Bei jedem Fehler wird die Zieldatei geloescht - die `.accdb` ist das Rollback.");
        b.AppendLine();

        // ------------------------------------------------- Tabellenuebersicht
        b.AppendLine("## Tabellenuebersicht");
        b.AppendLine();
        if (e.Tabellen.Count == 0)
        {
            b.AppendLine("_Keine Tabelle kopiert._");
        }
        else
        {
            b.AppendLine("Pruefsumme: reihenfolgeunabhaengige 128-Bit-Summe der SHA-256-Zeilenhashes " +
                         "ueber kanonisierte Spaltenwerte (NULL = Paragraphenzeichen+0, Boolean = 1/0, " +
                         "Ganzzahl invariant, REAL im Format \"R\", Datum `yyyy-MM-dd HH:mm:ss`).");
            b.AppendLine();
            b.AppendLine("| Tabelle | Zeilen Quelle | Zeilen gelesen | Zeilen Ziel | Pruefsumme Quelle | Pruefsumme Ziel | gleich | s |");
            b.AppendLine("|---|---:|---:|---:|---|---|:--:|---:|");
            foreach (var t in e.Tabellen)
            {
                b.AppendLine($"| {t.Name} | {Z(t.QuelleCount)} | {Z(t.QuelleGelesen)} | {Z(t.ZielCount)} | " +
                             $"`{Kurz(t.QuellePruefsumme)}` | `{Kurz(t.ZielPruefsumme)}` | " +
                             $"{(t.Ok ? "ja" : "**NEIN**")} | {t.Sekunden.ToString("F2", De)} |");
            }
            b.AppendLine();

            var schlecht = e.Tabellen.Where(t => !t.Ok).ToList();
            if (schlecht.Count == 0)
            {
                b.AppendLine($"**Datenbeweis bestanden:** alle {e.Tabellen.Count} Tabellen mit gleicher " +
                             "Zeilenzahl und gleicher Inhaltspruefsumme.");
            }
            else
            {
                b.AppendLine("### Differenzliste (Datenbeweis fehlgeschlagen)");
                b.AppendLine();
                b.AppendLine("| Tabelle | Befund |");
                b.AppendLine("|---|---|");
                foreach (var t in schlecht)
                {
                    var grund = !t.ZeilenGleich
                        ? $"Zeilenzahl Quelle {Z(t.QuelleCount)} / gelesen {Z(t.QuelleGelesen)} / Ziel {Z(t.ZielCount)}"
                        : $"Pruefsumme Quelle `{t.QuellePruefsumme}` != Ziel `{t.ZielPruefsumme}`";
                    b.AppendLine($"| {t.Name} | {grund} |");
                }
                b.AppendLine();
            }
        }
        b.AppendLine();

        // ------------------------------------------- Nicht migrierte Tabellen
        b.AppendLine("## Nicht migrierte Quelltabellen");
        b.AppendLine();
        b.AppendLine("Tabellen und Verknuepfungen der Quelle ausserhalb der Whitelist des Zielschemas. " +
                     "Sie werden hier namentlich genannt und nie stumm uebergangen.");
        b.AppendLine();
        if (e.NichtMigriert.Count == 0)
        {
            b.AppendLine("_Keine - die Quelle enthaelt genau die Tabellen des Zielschemas._");
        }
        else
        {
            b.AppendLine($"Anzahl: {e.NichtMigriert.Count}");
            b.AppendLine();
            foreach (var t in e.NichtMigriert) b.AppendLine($"- `{t}`");
        }
        b.AppendLine();

        if (e.QuellobjektArten.Count > 0)
        {
            b.AppendLine("### Weitere Objekte der Quelle (kein Migrationsgegenstand)");
            b.AppendLine();
            b.AppendLine("Vollzaehligkeitsnachweis zum Schema-Rowset: nur `TABLE` und `LINK` sind " +
                         "Migrationsgegenstand. `ACCESS TABLE`/`SYSTEM TABLE` sind Access-Interna (`MSys*`), " +
                         "`VIEW` sind gespeicherte Abfragen - sie stehen als Views im Zielschema (002).");
            b.AppendLine();
            b.AppendLine("| TABLE_TYPE | Anzahl |");
            b.AppendLine("|---|---:|");
            foreach (var kv in e.QuellobjektArten.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                b.AppendLine($"| {kv.Key} | {kv.Value} |");
            b.AppendLine();

            if (e.AbfragenOhneView.Count > 0)
            {
                b.AppendLine($"Gespeicherte Abfragen ohne View im Ziel ({e.AbfragenOhneView.Count}) - " +
                             "Ergebnis der Schemapflege in S2, kein Datenverlust:");
                b.AppendLine();
                foreach (var a in e.AbfragenOhneView) b.AppendLine($"- `{a}`");
                b.AppendLine();
            }
        }

        // ---------------------------------------------------- Autowert-Staende
        b.AppendLine("## seq je Tabelle (Autowert-Staende)");
        b.AppendLine();
        b.AppendLine("Nur Auffaelligkeiten. Explizite ID-Inserts pflegen `sqlite_sequence` selbst; " +
                     "geprueft wurde `seq >= MAX(id)` je AUTOINCREMENT-Tabelle.");
        b.AppendLine();
        if (e.SeqBefunde.Count == 0)
        {
            b.AppendLine("_Keine Auffaelligkeiten._");
        }
        else
        {
            b.AppendLine("| Tabelle | Spalte | MAX(id) | seq vorher | seq nachher | Vermerk |");
            b.AppendLine("|---|---|---:|---:|---:|---|");
            foreach (var s in e.SeqBefunde)
                b.AppendLine($"| {s.Tabelle} | {s.Spalte} | {s.MaxId} | " +
                             $"{(s.SeqVorher < 0 ? "(fehlt)" : s.SeqVorher.ToString(CultureInfo.InvariantCulture))} | " +
                             $"{s.SeqNachher} | {s.Vermerk} |");
        }
        b.AppendLine();

        // ------------------------------------------------------- Integritaet
        b.AppendLine("## Fremdschluessel und Integritaet");
        b.AppendLine();
        b.AppendLine($"- `PRAGMA integrity_check`: **{e.IntegrityCheck}**");
        b.AppendLine($"- `PRAGMA foreign_key_check`: **{(e.FkVerletzungen.Count == 0 ? "keine Verletzung" : e.FkVerletzungen.Count + " Verletzung(en)")}**");
        b.AppendLine($"- orphanPolicy: **{e.OrphanPolicy}**");
        b.AppendLine();
        if (e.FkVerletzungen.Count > 0)
        {
            b.AppendLine(e.OrphanPolicy == OrphanPolicy.Abbruch
                ? "Bei `Abbruch` wurde die Zieldatei geloescht; der Lauf endet mit Exit 3."
                : "Bei `AlsProtokollAussetzen` bleibt die Zieldatei erhalten. **Der Constraint bleibt " +
                  "bestehen** - die Verletzung wird ausgehalten und hier namentlich protokolliert.");
            b.AppendLine();
            b.AppendLine("| Tabelle | rowid | Elterntabelle | fkid |");
            b.AppendLine("|---|---:|---|---:|");
            foreach (var v in e.FkVerletzungen)
                b.AppendLine($"| {v.Tabelle} | {v.RowId} | {v.Elterntabelle} | {v.FkId} |");
            b.AppendLine();
        }

        // -------------------------------------------------------- Case-Drift
        b.AppendLine("## Case-Drift-Messlauf (D5)");
        b.AppendLine();
        b.AppendLine("Je Textschluessel: `GROUP BY lower(spalte) HAVING COUNT(DISTINCT spalte) > 1` im Ziel. " +
                     "Geprueft wurden alle TEXT-Spalten aus UNIQUE-Indizes des Inventars sowie " +
                     "`Tab_Projekt.Projektname`, `Tab_Energieanlagen.Bezeichner`, `Tab_Energieanlagen.WQ_Puffer`, " +
                     "`Tab_Pufferspeicher.Bezeichner`, `energy_carrier.code`, `emissionsart.kuerzel`. " +
                     "Leerer Befund = BINARY-Vergleich beweisbar folgenlos fuer diesen Bestand.");
        b.AppendLine();
        b.AppendLine($"Geprueft: {e.CaseDriftGeprueft.Count} Spalten - Befunde: **{e.CaseDrifts.Count}**");
        b.AppendLine();
        if (e.CaseDrifts.Count > 0)
        {
            b.AppendLine("| Tabelle | Spalte | Kleinform | Werte |");
            b.AppendLine("|---|---|---|---|");
            foreach (var d in e.CaseDrifts)
                b.AppendLine($"| {d.Tabelle} | {d.Spalte} | `{d.Kleinform}` | {string.Join(" / ", d.Werte.Select(w => "`" + w + "`"))} |");
            b.AppendLine();
        }
        if (e.CaseDriftGeprueft.Count > 0)
        {
            b.AppendLine("<details><summary>Gepruefte Spalten</summary>");
            b.AppendLine();
            foreach (var s in e.CaseDriftGeprueft) b.AppendLine($"- `{s}`");
            b.AppendLine();
            b.AppendLine("</details>");
            b.AppendLine();
        }

        // ---------------------------------------------------------- Warnungen
        if (e.Warnungen.Count > 0)
        {
            b.AppendLine("## Warnungen");
            b.AppendLine();
            foreach (var w in e.Warnungen) b.AppendLine($"- {w}");
            b.AppendLine();
        }

        b.AppendLine("---");
        b.AppendLine();
        b.AppendLine($"Exit-Code: **{e.Code}** ({CodeText(e.Code)})");

        var ordner = Path.GetDirectoryName(e.BerichtPfad);
        if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
        File.WriteAllText(e.BerichtPfad, b.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Z(long v) => v < 0 ? "-" : v.ToString("N0", De);

    private static string Kurz(string h) => string.IsNullOrEmpty(h) ? "-" : h;

    private static string Mb(long bytes) =>
        (bytes / 1024.0 / 1024.0).ToString("N1", De) + " MB";

    private static string CodeText(int code) => code switch
    {
        ExitCode.Erfolg => "Erfolg",
        ExitCode.Fehler => "Fehler",
        ExitCode.SitzungOffen => "Quelle geoeffnet (.laccdb)",
        ExitCode.Waisen => "Fremdschluesselverletzungen, orphanPolicy=Abbruch",
        ExitCode.BeweisFehlgeschlagen => "Datenbeweis fehlgeschlagen",
        _ => "unbekannt",
    };
}
