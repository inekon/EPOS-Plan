using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE WIEDERHOLPERIODE JE KOSTENPOSITION - Schemaschritt SCHRITT (Etappe E16,
    // Konzept Wirtschaftlichkeit § 2.11.2 V-G3, DIN EN 17463 6.3.1 "alle n Jahre").
    //
    // WOZU. Die Norm kennt vier Zeitpunkte eines Zahlungsstroms: Periode 0, jaehrlich,
    // alle n Jahre, einmalig im Jahr k. Die Kostenwelt fuehrte bis hierher drei davon -
    // Jahr 0 (Investition), jaehrlich (Betriebskosten) und einmalig in k (Startjahr,
    // Ersatzkette). "Alle n Jahre" fehlte: Eine Dichtheitspruefung alle 2 Jahre stand als
    // halber Jahresbetrag in der Rechnung - richtig in der Summe ueber lange Zeitraeume,
    // falsch im Zeitpunkt und damit im Barwert.
    //
    // DIE SPALTE. Wiederholperiode_a (INTEGER, nullbar) an Tab_ProjektWerte und an
    // Tab_KostenVorlagePosition - dieselbe Doppelpflicht wie Schritt 111: die
    // Vorlagenposition traegt die Periode, die Uebernahme reicht sie in die Projektzeile.
    // NULL, 0 und 1 heissen "jaehrlich" - der Weg vor dem Schritt; erst n >= 2 zahlt die
    // Position nur in den Jahren start, start + n, start + 2n ... <= T
    // (KapitalwertRechner.ZahltImJahr).
    //
    // WARUM EINE EIGENE DATEI. Drei Leser - der Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und die Testvorrichtung EPOS.Kern.Tests/TestDatenbank -
    // und EINE Nummer (SCHRITT). Muss der Schritt beim Zusammenfuehren umnummeriert
    // werden, aendert sich allein diese Konstante.
    //
    // ERGEBNISNEUTRAL. Kein DML: Alle Zeilen stehen nach dem Schritt auf NULL, und NULL
    // rechnet Zeichen fuer Zeichen den Weg vor dem Schritt. Der Referenzlauf bleibt
    // byte-gleich. Jeder Leser fragt die Spalte vor dem Lesen (SpaltenVorhanden), eine nie
    // migrierte Datenbank rechnet deshalb ebenfalls jaehrlich.
    // ====================================================================================

    /// <summary>
    /// <b>Die Spalte <c>Wiederholperiode_a</c></b> an <c>Tab_ProjektWerte</c> und
    /// <c>Tab_KostenVorlagePosition</c> — Schemaschritt <see cref="SCHRITT"/>. Anlass und
    /// Regeln stehen im Kopf der Datei; Lese- und Schreibweg der Kostenwelt stehen bei
    /// <c>Wiederholperiode</c> (Controller).
    /// </summary>
    public static class WiederholperiodeSchema
    {
        /// <summary>Die Nummer des Schemaschritts (E16) — die EINZIGE Stelle der Zahl für
        /// Migration, Werkzeug, Testvorrichtung und Zielstand. 128 trägt der Heizkreis je
        /// Gebäude (Anlagenkopplung AK1 Welle 3), der zuerst veröffentlicht wurde.</summary>
        public const int SCHRITT = 129;

        /// <summary>Der Spaltenname — an beiden Tabellen derselbe. Einheit Jahre; NULL, 0
        /// und 1 = jährlich.</summary>
        public const string SPALTE = "Wiederholperiode_a";

        /// <summary>
        /// Die zwei Spalten des Schritts — Typangabe „LONG", übersetzt von
        /// <c>StilleDb.SqliteSpaltenTyp</c> in <c>INTEGER</c> ohne <c>NOT NULL</c> und ohne
        /// Vorgabe: NULL trägt „jährlich wie bisher". Beide Tabellen sind STRICT; ein
        /// <c>ADD COLUMN</c> mit INTEGER-Typ ist dort zulässig.
        ///
        /// <para>Die Spalten stehen BEWUSST NICHT in <see cref="SchemaKatalog.Alle"/> — Leser
        /// ist allein die Kostenwelt, und jeder Leser prüft die Spalte vor dem Lesen.</para>
        /// </summary>
        public static readonly SchemaSpalte[] Spalten =
        {
            new SchemaSpalte(SchemaKatalog.TAB_PROJEKTWERTE,          SPALTE, "LONG"),
            new SchemaSpalte(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, SPALTE, "LONG"),
        };

        // =====================================================================
        //  Spaltenstand
        // =====================================================================

        private static readonly object _sperre = new object();
        private static string _pfad;
        private static readonly Dictionary<string, bool> _da =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Trägt <paramref name="tabelle"/> die Spalte? Gemerkt je Datenbankpfad — eine
        /// Testkopie, ein Referenzlauf und die Datenbank des Anwenders bekommen je ihre
        /// eigene Antwort (Muster <c>ErsatzRestwertKennzeichen.SpaltenVorhanden</c>).
        /// </summary>
        public static bool SpalteVorhanden(string tabelle)
        {
            if (string.IsNullOrEmpty(tabelle)) return false;
            string pfad = Pfad();
            lock (_sperre)
            {
                if (!string.Equals(pfad, _pfad, StringComparison.OrdinalIgnoreCase))
                {
                    _da.Clear();
                    _pfad = pfad;
                }
                bool da;
                if (_da.TryGetValue(tabelle, out da)) return da;
                da = false;
                try { da = DataRepository.SpalteVorhanden(tabelle, SPALTE); }
                catch { da = false; }
                _da[tabelle] = da;
                return da;
            }
        }

        /// <summary>Stehen beide Spalten? Die Nachprobe von Migration und Werkzeug.</summary>
        public static bool Vollstaendig()
        {
            SpaltenStandVergessen();
            foreach (SchemaSpalte s in Spalten)
                if (!SpalteVorhanden(s.Tabelle)) return false;
            return true;
        }

        /// <summary>Vergisst den gemerkten Spaltenstand — gerufen vom Schemaschritt,
        /// nachdem er die Spalten angelegt hat, damit derselbe Prozess sie sofort liest.</summary>
        public static void SpaltenStandVergessen()
        {
            lock (_sperre)
            {
                _da.Clear();
                _pfad = null;
            }
        }

        private static string Pfad()
        {
            try { return DataRepository.GetDBPath() ?? ""; }
            catch { return ""; }
        }
    }
}
