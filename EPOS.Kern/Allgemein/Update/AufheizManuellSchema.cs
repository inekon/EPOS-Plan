using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KP-S4 - AUFSCHLAG, MANUELLE AUFHEIZZEIT UND ART IM ERGEBNIS (Entscheid E59 samt den
    // Folgeentscheiden vom 03.10.2026, E60; Entwurf KP3 Abschnitt 4 und Festlegungen 35-43,
    // Teilkonzept Konditionierungsprofile 5.3/5.4).
    //
    // WOZU. Die Aufheizoptimierung bekommt einen Aufschlag des Projekts in Stunden und Prozent
    // (es gilt das Maximum, Festlegung 35) und je Gebaeude eine manuelle Aufheizzeit (Festlegung
    // 37); das Ergebnis traegt die wirksame Art, die Auslegungsheizlast und den Aufheizzuschlag
    // (Festlegungen 39, 41). Ein Schritt (Festlegung 43):
    //
    //   Tab_Einstellungen    Aufheiz_Aufschlag_H        INTEGER, 0 bis 24      NULL = 0 h
    //                        Aufheiz_Aufschlag_Prozent  REAL,    0 bis 100     NULL = 0 %
    //   Tab_Gebaeude         Aufheizzeit_Manuell_H      INTEGER, 1 bis 47      NULL = Art des Projekts
    //   Tab_ErgebnisGebaeude Aufheiz_Art                TEXT IN (TAEGLICH, FEST, MANUELL)
    //                        Auslegungsheizlast_Kw      REAL > 0
    //                        Aufheizzuschlag_Kw         REAL >= 0
    //   Tab_ErgebnisZone     Aufheiz_Art                wie am Gebaeude
    //                        Aufheiz_Zustand            CHECK um GEKOPPELT erweitert (Befund D2)
    //
    // Alle Spalten per ADD COLUMN, nullbar, mit Pruefklausel; Tab_Gebaeude_STAMM bekommt keine
    // (Festlegung 38), Tab_ErgebnisGebaeude wird NICHT neu gebaut (Entscheid Schemaweg A1:
    // Aufheiz_Bemessung behaelt die Variante, die Art steht in der eigenen Spalte).
    //
    // DER KLEINE NEUBAU. SQLite aendert ein CHECK nicht an Ort und Stelle: Allein der Nachtrag
    // GEKOPPELT an Tab_ErgebnisZone.Aufheiz_Zustand baut diese eine Tabelle neu - nach dem Rezept
    // von Schritt 96 (Muster KonditionierungVorlagenSchema): Der Zieltext entsteht aus dem
    // GELTENDEN sqlite_master.sql, in dem die EINE alte Wertliste gegen die neue tauscht (sonst
    // Zeichen fuer Zeichen der Bestand, STRICT, beide Fremdschluessel); die Zeilen ziehen namentlich
    // mit ihren IDs um, beide Indizes entstehen Wort fuer Wort neu; foreign_key_check bleibt leer.
    // Keine Tabelle, Sicht oder Trigger verweist auf Tab_ErgebnisZone. Erkannt wird der Stand am
    // CHECK-Text - steht die neue Wertliste, laeuft kein Neubau.
    //
    // DER ACHTE SICHTNEUBAU. Der Lauf liest das Gebaeude ueber Abfrage_Projektgebaeude; die
    // manuelle Aufheizzeit steht dort HINTER dem Energiestandard (103 Spalten,
    // GebaeudeSchema.SQL_VIEW_AUFHEIZ_MANUELL). Weil aeltere Durchgaenge die Sicht in ihrer Form neu
    // bauen, laeuft dieser Schritt in Migration, Werkzeug und Testkopie ZULETZT und baut die Sicht,
    // sobald sie nicht seine Form hat.
    //
    // KEIN DML AN BESTANDSDATEN. Jede Bestandszeile steht danach auf NULL: kein Aufschlag, keine
    // manuelle Zeit, kein Ergebnis dieser Art - jedes Projekt rechnet wie vorher. Der Referenzlauf
    // liest weder Tab_ErgebnisGebaeude noch Tab_ErgebnisZone; er bleibt byte-gleich. Alles in EINEM
    // Vorgang mit abgeschalteten Fremdschluesseln (DataRepository.VorgangOhneFremdschluessel): Scheitert
    // ein Teil, bleibt die Datei, wie sie war, und der Schritt ist wiederholbar.
    //
    // DREI LESER: der Schemaschritt in WindowsFormsApplication1/Allgemein/Update/
    // SchemaMigration.cs, das Werkzeug Werkzeuge/Testdatenbankschema und die Testvorrichtung samt
    // Nachweis in EPOS.Kern.Tests; dazu KonfigurationCtrl (Aufschlag), ProjektGebaeudeCtrl (manuelle
    // Aufheizzeit) und ErgebnisCtrl (Ergebnisspalten nach Vorhandensein).
    // ====================================================================================

    /// <summary>
    /// <b>KP-S4</b> — Aufschlag und manuelle Aufheizzeit der Aufheizoptimierung, die wirksame Art,
    /// Auslegungsheizlast und Aufheizzuschlag im Ergebnis, <c>GEKOPPELT</c> an der Zone und der achte
    /// Neubau der Sicht <c>Abfrage_Projektgebaeude</c> (Entwurf KP3 Abschnitt 4) — EINE Quelle für
    /// Migration, Werkzeug, Testkopie, Controller und Nachweis (ADR-001 Option C). Anlass und Bauform
    /// stehen im Kopf der Datei.
    /// </summary>
    public static class AufheizManuellSchema
    {
        /// <summary>
        /// <b>Die Nummer des Schemaschritts</b> — die EINE Stelle, an der sie steht; vergeben unmittelbar
        /// vor dem Schemacommit gegen <c>origin</c> (Festlegung 43, ADR-001 A11): der Schritt hinter der
        /// zuletzt auf <c>origin</c> liegenden Klasse.
        /// </summary>
        public const int SCHRITT = KatalogfassungStufe2Schema.SCHRITT + 1;

        /// <summary>Die Projekteinstellungen.</summary>
        public const string TAB_EINSTELLUNGEN = SchemaKatalog.TAB_EINSTELLUNGEN;

        /// <summary>Die Projektgebäude (nicht der Katalog, Festlegung 38).</summary>
        public const string TAB_GEBAEUDE = GebaeudeSchema.TAB_GEBAEUDE;

        /// <summary>Die Gebäudetabelle des Ergebnisses.</summary>
        public const string TAB_ERGEBNIS_GEBAEUDE = AufheizErgebnisSchema.TAB_GEBAEUDE;

        /// <summary>Die Zonentabelle des Ergebnisses — die einzige, die neu gebaut wird.</summary>
        public const string TAB_ERGEBNIS_ZONE = AufheizErgebnisSchema.TAB_ZONE;

        /// <summary><c>Aufheiz_Aufschlag_H</c> [h]: Aufschlag auf jede ermittelte Rampe, 0 bis 24; NULL = 0.</summary>
        public const string SPALTE_AUFSCHLAG_H = "Aufheiz_Aufschlag_H";

        /// <summary><c>Aufheiz_Aufschlag_Prozent</c> [%]: Aufschlag als Anteil von n, 0 bis 100; NULL = 0.</summary>
        public const string SPALTE_AUFSCHLAG_PROZENT = "Aufheiz_Aufschlag_Prozent";

        /// <summary><c>Aufheizzeit_Manuell_H</c> [h] an <c>Tab_Gebaeude</c>: 1 bis 47; NULL = Art des Projekts.</summary>
        public const string SPALTE_MANUELL = GebaeudeSchema.SPALTE_AUFHEIZZEIT_MANUELL;

        /// <summary><c>Aufheiz_Art</c> an beiden Ergebnistabellen: die wirksame Art (<see cref="DbWerte.AUFHEIZ_ERGEBNIS_ARTEN"/>).</summary>
        public const string SPALTE_ART = "Aufheiz_Art";

        /// <summary><c>Auslegungsheizlast_Kw</c> [kW] (nur Gebäude): Φ_HL, skaliert wie P_auf (Festlegung 41).</summary>
        public const string SPALTE_AUSLEGUNGSHEIZLAST = "Auslegungsheizlast_Kw";

        /// <summary><c>Aufheizzuschlag_Kw</c> [kW] (nur Gebäude): Φ_RH = max(0, P_auf − Φ_stat), skaliert wie P_auf (Festlegung 41).</summary>
        public const string SPALTE_AUFHEIZZUSCHLAG = "Aufheizzuschlag_Kw";

        /// <summary>Die Zustandsspalte der Zone, deren Prüfklausel der Neubau erweitert.</summary>
        public const string SPALTE_ZUSTAND = AufheizErgebnisSchema.SPALTE_ZUSTAND;

        /// <summary>Der größte Aufschlag in Stunden.</summary>
        public const int AUFSCHLAG_H_MAX = 24;

        /// <summary>Der größte Aufschlag in Prozent.</summary>
        public const double AUFSCHLAG_PROZENT_MAX = 100;

        /// <summary>Die kleinste manuelle Aufheizzeit [h].</summary>
        public const int MANUELL_MIN_H = 1;

        /// <summary>Die größte manuelle Aufheizzeit [h] — t = n − 1 mit dem Deckel n ≤ 48.</summary>
        public const int MANUELL_MAX_H = 47;

        /// <summary>Der Zusatz, unter dem die Zonentabelle für die Dauer ihres Neubaus ausweicht.</summary>
        public const string HILFSZUSATZ = "_alt";

        /// <summary>Die Zahl der Spalten, die der Schritt an <c>Tab_Einstellungen</c> anlegt (Nachweis in den Tests).</summary>
        public const int EINSTELLUNGSSPALTEN = 2;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisGebaeude</c> nach diesem Schritt (40 + 3; B24).</summary>
        public const int SPALTENZAHL_ERGEBNIS_GEBAEUDE = AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_GEBAEUDE + 3;

        /// <summary>Spaltenzahl von <c>Tab_ErgebnisZone</c> nach diesem Schritt (29 + 1; B24).</summary>
        public const int SPALTENZAHL_ERGEBNIS_ZONE = AufheizErgebnisSchema.SPALTENZAHL_ERGEBNIS_ZONE + 1;

        /// <summary>Die Wertliste des Zonenzustands VOR dem Schritt (Schritt 161, ohne GEKOPPELT).</summary>
        public static readonly IReadOnlyList<string> ZUSTAENDE_ZONE_ALT = new[]
        {
            DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT,
        };

        /// <summary>Die Wertliste des Zonenzustands NACH dem Schritt — mit GEKOPPELT (Befund D2, Festlegung 25).</summary>
        public static readonly IReadOnlyList<string> ZUSTAENDE_ZONE = new[]
        {
            DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR, DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT,
            DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT,
        };

        /// <summary>Die Prüfklausel des Zonenzustands, wie Schritt 161 sie anlegt (die Stelle des Neubaus).</summary>
        public static readonly string CHECK_ZUSTAND_ALT =
            "CHECK (\"" + SPALTE_ZUSTAND + "\" IN (" + AufheizvorgabeSchema.Liste(ZUSTAENDE_ZONE_ALT) + "))";

        /// <summary>Die Prüfklausel des Zonenzustands nach dem Neubau.</summary>
        public static readonly string CHECK_ZUSTAND =
            "CHECK (\"" + SPALTE_ZUSTAND + "\" IN (" + AufheizvorgabeSchema.Liste(ZUSTAENDE_ZONE) + "))";

        /// <summary>
        /// Die sieben angelegten Spalten in Anlegereihenfolge: (Tabelle, Spalte, Typ samt Prüfklausel).
        /// Jede nullbar, ohne Vorgabe — leer heißt „die Vorgabe" bzw. „kein Ergebnis dieser Art".
        /// </summary>
        public static readonly IReadOnlyList<(string Tabelle, string Spalte, string Typ)> SPALTEN = new[]
        {
            (TAB_EINSTELLUNGEN, SPALTE_AUFSCHLAG_H,
                "INTEGER " + Pruefung(SPALTE_AUFSCHLAG_H, "BETWEEN 0 AND " + Ganz(AUFSCHLAG_H_MAX))),
            (TAB_EINSTELLUNGEN, SPALTE_AUFSCHLAG_PROZENT,
                "REAL " + Pruefung(SPALTE_AUFSCHLAG_PROZENT, "BETWEEN 0 AND " + AufheizvorgabeSchema.Zahl(AUFSCHLAG_PROZENT_MAX))),
            (TAB_GEBAEUDE, SPALTE_MANUELL,
                "INTEGER " + Pruefung(SPALTE_MANUELL, "BETWEEN " + Ganz(MANUELL_MIN_H) + " AND " + Ganz(MANUELL_MAX_H))),
            (TAB_ERGEBNIS_GEBAEUDE, SPALTE_ART,
                "TEXT " + Pruefung(SPALTE_ART, "IN (" + AufheizvorgabeSchema.Liste(DbWerte.AUFHEIZ_ERGEBNIS_ARTEN) + ")")),
            (TAB_ERGEBNIS_GEBAEUDE, SPALTE_AUSLEGUNGSHEIZLAST, "REAL " + Pruefung(SPALTE_AUSLEGUNGSHEIZLAST, "> 0")),
            (TAB_ERGEBNIS_GEBAEUDE, SPALTE_AUFHEIZZUSCHLAG, "REAL " + Pruefung(SPALTE_AUFHEIZZUSCHLAG, ">= 0")),
            (TAB_ERGEBNIS_ZONE, SPALTE_ART,
                "TEXT " + Pruefung(SPALTE_ART, "IN (" + AufheizvorgabeSchema.Liste(DbWerte.AUFHEIZ_ERGEBNIS_ARTEN) + ")")),
        };

        /// <summary>Die Tabellen, die der Schritt voraussetzt.</summary>
        public static IReadOnlyList<string> Voraussetzungen()
            => new[] { TAB_EINSTELLUNGEN, TAB_GEBAEUDE, TAB_ERGEBNIS_GEBAEUDE, TAB_ERGEBNIS_ZONE };

        /// <summary>Die Anweisung, die eine Spalte des Schritts anlegt.</summary>
        public static string Anlegen((string Tabelle, string Spalte, string Typ) s)
            => "ALTER TABLE \"" + s.Tabelle + "\" ADD COLUMN \"" + s.Spalte + "\" " + s.Typ;

        // =================================================================
        //  Auskunft
        // =================================================================

        /// <summary>
        /// Steht der Schritt? Alle sieben Spalten, die Zonentabelle kennt <c>GEKOPPELT</c>, und die Sicht
        /// beginnt mit den 103 Spalten des achten Durchgangs.
        /// </summary>
        public static bool Vollstaendig()
        {
            foreach ((string tabelle, string spalte, string _) in SPALTEN)
                if (!DataRepository.SpalteVorhanden(tabelle, spalte)) return false;
            return ZoneKenntGekoppelt() && SichtSteht();
        }

        /// <summary>Kennt die Zustandsklausel von <c>Tab_ErgebnisZone</c> den Wert <c>GEKOPPELT</c>?</summary>
        public static bool ZoneKenntGekoppelt()
        {
            string text = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("?", TAB_ERGEBNIS_ZONE)), CultureInfo.InvariantCulture) ?? "";
            return text.Contains(CHECK_ZUSTAND, StringComparison.Ordinal);
        }

        /// <summary>Beginnt die Spaltenfolge der Sicht mit <see cref="GebaeudeSchema.SICHT_AUFHEIZ_MANUELL"/>?</summary>
        public static bool SichtSteht()
        {
            List<string> ist = GebaeudeSchema.SichtSpalten();
            string[] soll = GebaeudeSchema.SICHT_AUFHEIZ_MANUELL;
            return ist.Count >= soll.Length && ist.Take(soll.Length).SequenceEqual(soll, StringComparer.Ordinal);
        }

        // =================================================================
        //  Der Zieltext des Neubaus
        // =================================================================

        /// <summary>
        /// Der Zieltext der Zonentabelle: ihr GELTENDER <c>sqlite_master.sql</c>, in dem die EINE Klausel
        /// <see cref="CHECK_ZUSTAND_ALT"/> gegen <see cref="CHECK_ZUSTAND"/> tauscht — sonst Zeichen für Zeichen
        /// der Bestand (STRICT, Fremdschlüssel, übrige Prüfklauseln).
        ///
        /// <para>Steht die alte Klausel nicht genau einmal da, ist die Tabelle keine STRICT-Tabelle oder hat
        /// sie eine andere Bauform, bricht der Aufruf BENANNT ab, statt eine falsche Tabelle anzulegen.</para>
        /// </summary>
        public static string Zieltext(string bestand)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException("Zu " + TAB_ERGEBNIS_ZONE + " gibt es keinen CREATE-Text in sqlite_master.");
            string text = bestand.TrimEnd();
            if (!text.EndsWith(KonditionierungVorlagenSchema.ENDE, StringComparison.Ordinal))
                throw new InvalidOperationException("Die Tabelle " + TAB_ERGEBNIS_ZONE + " endet nicht auf \"" +
                                                    KonditionierungVorlagenSchema.ENDE + "\" - Schemaschritt " + Nr +
                                                    " baut sie deshalb NICHT um.");
            string kopf = "CREATE TABLE \"" + TAB_ERGEBNIS_ZONE + "\" (";
            if (!text.StartsWith(kopf, StringComparison.Ordinal))
                throw new InvalidOperationException("Der CREATE-Text von " + TAB_ERGEBNIS_ZONE +
                                                    " hat eine andere Bauform als erwartet (erwartet wurde der Beginn " +
                                                    kopf + ").");
            int erste = text.IndexOf(CHECK_ZUSTAND_ALT, StringComparison.Ordinal);
            int zweite = erste < 0 ? -1 : text.IndexOf(CHECK_ZUSTAND_ALT, erste + CHECK_ZUSTAND_ALT.Length, StringComparison.Ordinal);
            if (erste < 0 || zweite >= 0)
                throw new InvalidOperationException("Im CREATE-Text von " + TAB_ERGEBNIS_ZONE + " steht die Klausel " +
                                                    CHECK_ZUSTAND_ALT + " " + (erste < 0 ? "nicht" : "mehr als einmal") +
                                                    " - Schemaschritt " + Nr + " baut die Tabelle deshalb NICHT um.");
            return text.Substring(0, erste) + CHECK_ZUSTAND + text.Substring(erste + CHECK_ZUSTAND_ALT.Length);
        }

        // =================================================================
        //  Ausführung
        // =================================================================

        /// <summary>
        /// Führt den Schritt in EINEM Vorgang mit abgeschalteten Fremdschlüsseln aus — für die Migration der
        /// Schale, <c>Werkzeuge/Testdatenbankschema</c> und <c>EPOS.Kern.Tests</c>. <b>Wiederholbar:</b> Eine
        /// stehende Spalte wird übergangen, ein stehender Zonenzustand nicht neu gebaut, die Sicht nur neu
        /// gebaut, wenn sie nicht die Form des achten Durchgangs hat; steht alles, öffnet er keinen Vorgang.
        /// <b>Kein DML an Bestandsdaten.</b>
        ///
        /// <para><b>Benannter Abbruch</b> (<see cref="InvalidOperationException"/>, nichts geändert): eine
        /// fehlende vorausgesetzte Tabelle, eine Zustandsklausel, die nicht genau einmal dasteht, ein
        /// Neubau, der Zeilen verliert, ein nicht leerer <c>foreign_key_check</c>.</para>
        /// </summary>
        /// <param name="bericht">Nimmt je Handgriff eine Zeile auf; darf <c>null</c> sein.</param>
        /// <returns>Die Zahl der angelegten Spalten (0 bis 7) — wie die übrigen Sichtdurchgänge; Neubau und Sicht
        /// nennt der Bericht.</returns>
        public static int Ausfuehren(IList<string> bericht)
        {
            // Die Auskunft VOR dem Vorgang - SpalteVorhanden arbeitet auf einer eigenen Verbindung
            // und saehe die offene Transaktion nicht.
            foreach (string t in Voraussetzungen())
                if (!DataRepository.TabelleVorhanden(t))
                    throw new InvalidOperationException("Schemaschritt " + Nr + ": Die Tabelle " + t +
                                                        " fehlt; ein frueherer Schritt ist nicht gelaufen.");
            var offen = SPALTEN.Where(s => !DataRepository.SpalteVorhanden(s.Tabelle, s.Spalte)).ToList();
            bool neubau = !ZoneKenntGekoppelt();
            bool sicht = !SichtSteht();
            if (offen.Count == 0 && !neubau && !sicht)
            {
                bericht?.Add("steht bereits - Aufschlag, manuelle Aufheizzeit, Ergebnisspalten, GEKOPPELT an der " +
                             "Zone und die Sicht; nichts zu tun");
                return 0;
            }

            int angelegt = 0;
            var zeilen = new List<string>();
            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    // ---- 1. Der kleine Neubau der Zonentabelle - VOR ihrer neuen Spalte, damit der
                    //      Zieltext genau den Bestand von Schritt 161 traegt.
                    if (neubau) zeilen.Add(Neubau(v));

                    // ---- 2. Die sieben Spalten.
                    if (sicht) v.Ausfuehren(GebaeudeSchema.SQL_VIEW_DROP);
                    foreach ((string Tabelle, string Spalte, string Typ) s in offen)
                    {
                        v.Ausfuehren(Anlegen(s));
                        zeilen.Add(s.Tabelle + "." + s.Spalte + " angelegt (leer)");
                        angelegt++;
                    }

                    // ---- 3. Der achte Sichtneubau.
                    if (sicht)
                    {
                        v.Ausfuehren(GebaeudeSchema.SQL_VIEW_AUFHEIZ_MANUELL);
                        zeilen.Add("Sicht " + GebaeudeSchema.VIEW + " neu gebaut (" +
                                   Ganz(GebaeudeSchema.SICHT_AUFHEIZ_MANUELL.Length) + " Spalten)");
                    }

                    // ---- Der Zeuge NACHHER, noch INNERHALB der Transaktion.
                    foreach (string t in new[] { TAB_ERGEBNIS_ZONE, TAB_ERGEBNIS_GEBAEUDE })
                    {
                        long verletzt = Zahl(v.Skalar("SELECT COUNT(*) FROM pragma_foreign_key_check(?)", new DbParam("@t", t)));
                        if (verletzt > 0)
                            throw new InvalidOperationException("Nach dem Umbau meldet foreign_key_check fuer " + t + " " +
                                                                verletzt.ToString(CultureInfo.InvariantCulture) +
                                                                " verletzte Zeile(n) - Schemaschritt " + Nr +
                                                                " nimmt alles zurueck.");
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN (Muster Schritt 96).
                    try { if (v.Offen) v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* nach Commit oder Rollback abgeschlossen */ }
                }
            }

            if (bericht != null)
                foreach (string z in zeilen) bericht.Add(z);
            bericht?.Add("foreign_key_check leer; KEIN DML an Bestandsdaten, der Referenzlauf bleibt byte-gleich");
            return angelegt;
        }

        /// <summary>
        /// Das Neubau-Rezept aus Schritt 96 für <c>Tab_ErgebnisZone</c>: Die alte Tabelle weicht unter
        /// <c>legacy_alter_table = ON</c> auf den Hilfsnamen aus, die neue entsteht unter dem echten Namen
        /// mit erweiterter Zustandsklausel, die Zeilen ziehen namentlich mit ihren IDs um, die alte fällt
        /// (Fremdschlüssel aus — keine Kaskade), die Indizes des Bestands stehen Wort für Wort wieder.
        /// </summary>
        /// <returns>Die Berichtszeile.</returns>
        private static string Neubau(DbVorgang v)
        {
            string tabelle = TAB_ERGEBNIS_ZONE;
            string alt = tabelle + HILFSZUSATZ;
            string bestand = Convert.ToString(v.Skalar("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                                                       new DbParam("@t", tabelle)), CultureInfo.InvariantCulture);
            string ziel = Zieltext(bestand);

            // Spalten NAMENTLICH - "SELECT *" haengt an der Reihenfolge zweier Schemastaende.
            var namen = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("@t", tabelle));
            foreach (DataRow zeile in info.Rows)
                namen.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", namen);

            var indizes = new List<string>();
            DataTable idx = v.Lese("SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = ? AND sql IS NOT NULL",
                                   new DbParam("@t", tabelle));
            foreach (DataRow zeile in idx.Rows)
                indizes.Add(Convert.ToString(zeile["sql"], CultureInfo.InvariantCulture));

            object standWert = v.Skalar("SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
            long zeilenVorher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));

            v.Ausfuehren("DROP TABLE IF EXISTS \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = ON");
            v.Ausfuehren("ALTER TABLE \"" + tabelle + "\" RENAME TO \"" + alt + "\"");
            v.Ausfuehren("PRAGMA legacy_alter_table = OFF");

            v.Ausfuehren(ziel);
            v.Ausfuehren("INSERT INTO \"" + tabelle + "\" (" + spaltenliste + ") SELECT " + spaltenliste + " FROM \"" + alt + "\"");
            v.Ausfuehren("DROP TABLE \"" + alt + "\"");

            // Der Zaehler (ohne AUTOINCREMENT gibt es keinen; mit ihm reist er mit).
            v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", alt));
            if (standWert != null && standWert != DBNull.Value)
            {
                v.Ausfuehren("DELETE FROM sqlite_sequence WHERE name = ?", new DbParam("@t", tabelle));
                v.Ausfuehren("INSERT INTO sqlite_sequence (name, seq) VALUES (?, ?)",
                             new DbParam("@t", tabelle),
                             new DbParam("@s", Convert.ToInt64(standWert, CultureInfo.InvariantCulture)));
            }

            foreach (string anweisung in indizes)
                v.Ausfuehren(ProjektFremdschluessel.MitIfNotExists(anweisung));

            long zeilenNachher = Zahl(v.Skalar("SELECT COUNT(*) FROM \"" + tabelle + "\""));
            if (zeilenNachher != zeilenVorher)
                throw new InvalidOperationException("Der Neubau von " + tabelle + " traegt " +
                                                    zeilenNachher.ToString(CultureInfo.InvariantCulture) + " statt " +
                                                    zeilenVorher.ToString(CultureInfo.InvariantCulture) +
                                                    " Zeilen - Schemaschritt " + Nr + " nimmt ihn zurueck.");

            return tabelle + ": " + SPALTE_ZUSTAND + " kennt GEKOPPELT (Neubau); Zeilen " +
                   zeilenVorher.ToString(CultureInfo.InvariantCulture) + " -> " +
                   zeilenNachher.ToString(CultureInfo.InvariantCulture) + ", IDs erhalten, " +
                   indizes.Count.ToString(CultureInfo.InvariantCulture) + " Index(e) des Bestands wieder angelegt";
        }

        private static string Nr => SCHRITT.ToString(CultureInfo.InvariantCulture);

        private static string Pruefung(string spalte, string bedingung)
            => "CHECK (\"" + spalte + "\" IS NULL OR \"" + spalte + "\" " + bedingung + ")";

        private static string Ganz(int wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static long Zahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt64(wert, CultureInfo.InvariantCulture);
    }
}
