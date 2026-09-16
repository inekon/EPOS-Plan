using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DER KOSTENFAKTOR REISST KEINE PROJEKTPOSITION MEHR MIT
    // - Migrationsschritt 81 (Auftrag #302, Befund des Anwenders vom 16.09.2026).
    //
    // WOZU. Tab_ProjektWerte ist die EINE Tabelle der Projektkosten - Investition wie
    // Betrieb, jedes Gewerk, jedes Projekt. Ihr Fremdschluessel auf den Katalog
    //
    //     FOREIGN KEY ("StammID") REFERENCES "Tab_Kostenfaktor" ("StammID")
    //                             ON UPDATE CASCADE ON DELETE CASCADE
    //
    // trug ON DELETE CASCADE, und PRAGMA foreign_keys = ON steht je Verbindung
    // (SqliteDatenzugriff). EINEN Katalogeintrag zu loeschen - "Administration
    // Kostenfaktoren", Knopf "Loeschen" - riss damit JEDE Projektposition derselben
    // StammID mit, quer durch alle Projekte und Gewerke. Nachgerechnet auf der
    // Testdatenbank: "Planung / Baunebenkosten" (StammID 114) nahm 6 Positionen aus
    // 5 Projekten und 3 Gewerken mit, "Waermepumpe (Aggregat)" (108) 4 Positionen aus
    // 4 Projekten - darunter drei Waermepumpen-Investitionen von je 13.000,00 EUR, die
    // danach auf 0,00 EUR standen. Die Rueckfrage des Dialogs nannte nichts davon.
    //
    // DIE KASKADE WAR NIE GEWOLLT. Der Beleg steht in derselben Datenbank:
    // Tab_KostenVorlagePosition.StammID - dieselbe Beziehung zum selben Katalog - traegt
    // ueberhaupt keinen Fremdschluessel. Die Kaskade ist ein Erbstueck der
    // Access-Uebernahme, keine fachliche Aussage. Fachlich gilt das Gegenteil: Ein
    // Katalogeintrag, auf den Projektpositionen zeigen, DARF nicht verschwinden.
    //
    // ZWEITE SCHICHT, NICHT DIE EINZIGE. Die erste Schicht ist KostenfaktorCtrl.Loeschen:
    // Es zaehlt vor dem DELETE und lehnt benannt ab. Dieser Schritt ist die zweite - er
    // gilt auch fuer jeden Weg, der an diesem Controller vorbeigeht (Werkzeug, Skript,
    // ein spaeterer zweiter Loeschweg). Deshalb RESTRICT und nicht bloss "der Controller
    // passt schon auf".
    //
    // WARUM RESTRICT UND NICHT NO ACTION. Beide verhindern das Loeschen; RESTRICT urteilt
    // SOFORT, NO ACTION erst am Ende der Anweisung (und bei deferred erst am Commit).
    // Sofort ist hier das Richtige: Der Fehler soll an der Anweisung haengen, die ihn
    // ausloest, nicht an einem Commit weit dahinter. ON UPDATE CASCADE bleibt unangetastet
    // - eine umnummerierte StammID soll ihre Positionen weiterhin mitnehmen.
    //
    // WAS DER SCHRITT TUT - das Tabellenneubau-Rezept des SQLite-Handbuchs, wie schon in
    // SpeicherAuslegungStrict (Schritt 74). SQLite kennt kein "ALTER TABLE ... DROP
    // CONSTRAINT" und keine Aenderung einer Fremdschluesselregel; die Regel steht im
    // CREATE-Text, also muss die Tabelle neu gebaut werden:
    //
    //   1. einen Rest aus einem abgebrochenen Lauf abraeumen (DROP TABLE IF EXISTS),
    //   2. die Zieltabelle unter dem Hilfsnamen anlegen - derselbe CREATE-Text, STRICT,
    //      AUTOINCREMENT, dieselben 24 Spalten samt CHECK und dieselben FUENF
    //      Fremdschluessel; genau EINER davon aendert seine Loeschregel,
    //   3. INSERT INTO ... SELECT mit NAMENTLICH genannten Spalten (kein "SELECT *"),
    //   4. den AUTOINCREMENT-Zaehler mitnehmen (siehe "Der Zaehler" weiter unten),
    //   5. die alte Tabelle loeschen - ihre fuenf Indizes fallen mit ihr,
    //   6. den Hilfsnamen auf den echten Namen umbenennen,
    //   7. die fuenf Indizes neu anlegen.
    //
    // ALLES IN EINER TRANSAKTION, aus demselben Grund wie in Schritt 74: Zwischen 5 und 6
    // gibt es einen Augenblick, in dem die Tabelle unter ihrem echten Namen nicht
    // existiert. Fremdschluessel werden ueber "PRAGMA defer_foreign_keys = ON" EINMAL am
    // Commit beurteilt statt Anweisung fuer Anweisung; "PRAGMA foreign_keys = OFF" waere
    // innerhalb einer Transaktion ein No-op.
    //
    // DIE SICHT ABFRAGE_KOSTENFAKTOREN - der Unterschied zu Schritt 74. Seit SQLite 3.25
    // prueft ALTER TABLE ... RENAME jede Sicht und jeden Trigger der Datenbank neu. Genau
    // eine Sicht liest diese Tabelle (Abfrage_Kostenfaktoren, ein Verbund ueber
    // Tab_ProjektWerte, Tab_Kostenfaktor und Tab_KostenKomponente) - und im Augenblick der
    // Umbenennung ist die alte Tabelle bereits weg. Die Umbenennung endet dann mit
    // "error in view Abfrage_Kostenfaktoren: no such table: main.Tab_ProjektWerte".
    // Fuer genau diesen Fall gibt es "PRAGMA legacy_alter_table": Die Umbenennung schreibt
    // dann keine fremden Schemaobjekte um. Das ist hier nicht nur ein Ausweg, sondern das
    // Richtige - die Sicht nennt die Tabelle beim Namen, den sie nach der Umbenennung
    // wieder traegt. Die Klammer wird sofort danach wieder geschlossen.
    //
    // DER ZAEHLER. "ID" ist INTEGER PRIMARY KEY AUTOINCREMENT; der Stand steht in
    // sqlite_sequence und darf nicht zurueckfallen. Ein DROP TABLE nimmt die Zeile mit,
    // und der INSERT ... SELECT setzte den Zaehler nur auf die groesste kopierte ID -
    // haette der Anwender seither Positionen geloescht, kaeme eine bereits vergebene ID
    // ein zweites Mal heraus. Der Stand reist deshalb ausdruecklich mit (zwei Anweisungen
    // VOR dem DROP, eine Umbenennung danach). Die Umbenennung der Zeile ist die Folge des
    // legacy-Modus: Sie tut nichts, wenn die Umbenennung sie schon erledigt hat.
    //
    // WIEDERHOLBAR. Zaehlung() fragt pragma_foreign_key_list, ob der Fremdschluessel auf
    // Tab_Kostenfaktor noch CASCADE traegt. Traegt er es nicht mehr - oder gibt es die
    // Tabelle gar nicht -, tut der Schritt nichts. Ein zweiter Lauf aendert keine Zeile.
    //
    // ERGEBNISNEUTRAL. Der Schritt kopiert Zeilen, er rechnet nicht. Kein Wert, kein Typ,
    // keine Reihenfolge, keine ID aendert sich; die Referenzbasis bleibt, wie sie ist.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // SpeicherAuslegungStrict (74), NutzungsdauerSchema (75) und HeizstabJeWaermepumpe
    // (79): Die Anweisungen brauchen DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs (Access-Zweig, deshalb
    // nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema und den Nachweis in
    // EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden sie abschreiben.
    // ====================================================================================

    /// <summary>
    /// Der Loeschschutz der Projektkosten (Schemaschritt 81) — EINE Quelle fuer
    /// Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class ProjektWerteLoeschschutz
    {
        /// <summary>Die Tabelle, die der Schritt umbaut.</summary>
        public const string TABELLE = "Tab_ProjektWerte";

        /// <summary>Die Katalogtabelle, deren Loeschregel sich aendert.</summary>
        public const string KATALOG = "Tab_Kostenfaktor";

        /// <summary>
        /// Der Hilfsname, unter dem die Zieltabelle entsteht, bevor sie den echten Namen
        /// uebernimmt. Er lebt nur innerhalb des Umbaus; ausserhalb einer laufenden
        /// Transaktion gibt es ihn in keiner Datenbank.
        /// </summary>
        public const string HILFSTABELLE = TABELLE + "_neu";

        /// <summary>Die Loeschregel, die der Schritt herstellt.</summary>
        public const string LOESCHREGEL = "RESTRICT";

        /// <summary>Die Loeschregel, die der Schritt ablegt.</summary>
        public const string ALTE_LOESCHREGEL = "CASCADE";

        // =================================================================
        //  Die Tabelle
        //
        // DER CREATE-TEXT STEHT HIER UND NUR HIER. Anders als
        // Tab_SpeicherAuslegung (Schritt 74) hat diese Tabelle keinen Controller
        // mit einem SQL_TABELLE - sie stammt aus der Access-Uebernahme und ist
        // seither nur ueber ADD COLUMN gewachsen. Mit diesem Schritt bekommt sie
        // eine beschriebene Gestalt; wer hier eine Spalte ergaenzt, ergaenzt sie
        // in SPALTEN mit (der Nachweis haelt beide gegeneinander).
        //
        // "IF NOT EXISTS" ist kein Schmuck: Werkzeuge/SqlDialektPruefer haelt
        // jeden SQL-Text des Bestands mit EXPLAIN gegen die Testdatenbank, und die
        // fuehrt diese Tabelle. Ohne IF NOT EXISTS endete die Probe mit "table
        // Tab_ProjektWerte already exists" - dieselbe Schreibweise wie
        // SpeicherAuslegungCtrl.SQL_TABELLE.
        // =================================================================

        /// <summary>
        /// Die Zieltabelle — Wort fuer Wort der Bestand, mit EINER Aenderung: Der
        /// Fremdschluessel auf <see cref="KATALOG"/> traegt
        /// <c>ON DELETE <see cref="LOESCHREGEL"/></c> statt <c>ON DELETE CASCADE</c>.
        /// </summary>
        public const string SQL_TABELLE =
            "CREATE TABLE IF NOT EXISTS \"" + TABELLE + "\" (" +
            "\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"ProjektID\" INTEGER DEFAULT 0, " +
            "\"StammID\" INTEGER DEFAULT 0, " +
            "\"KomponentenID\" INTEGER DEFAULT 0, " +
            "\"KategorieID\" INTEGER DEFAULT 0, " +
            "\"EingegebenerWert\" REAL DEFAULT 0, " +
            "\"Worstcase\" REAL DEFAULT 0, " +
            "\"Bestcase\" REAL DEFAULT 0, " +
            "\"Nutzungsdauer\" REAL DEFAULT 0, " +
            "\"Worstcase_Nutzungsdauer\" REAL DEFAULT 0, " +
            "\"Bestcase_Nutzungsdauer\" REAL DEFAULT 0, " +
            "\"Einheit\" TEXT, " +
            "\"Gruppe\" TEXT, " +
            "\"Kostenart\" TEXT CHECK (length(\"Kostenart\") <= 20), " +
            "\"Bemessung\" TEXT CHECK (length(\"Bemessung\") <= 30), " +
            "\"IstErloes\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstErloes\" IN (0,1)), " +
            "\"Menge\" REAL, " +
            "\"Einheitpreis\" REAL, " +
            "\"VorlageID\" INTEGER, " +
            "\"StartJahr\" INTEGER, " +
            "\"ID_Anlage\" INTEGER, " +
            "\"ID_AnlageGeraet\" INTEGER, " +
            "\"IstPflicht\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstPflicht\" IN (0,1)), " +
            "\"NutzungsdauerID\" INTEGER REFERENCES \"Tab_Nutzungsdauer\" (\"ID\"), " +
            "FOREIGN KEY (\"StammID\") REFERENCES \"" + KATALOG + "\" (\"StammID\") " +
            "ON UPDATE CASCADE ON DELETE " + LOESCHREGEL + ", " +
            "FOREIGN KEY (\"Gruppe\") REFERENCES \"Tab_KostenGruppenKatalog\" (\"GruppenName\"), " +
            "FOREIGN KEY (\"KomponentenID\") REFERENCES \"Tab_KostenKomponente\" (\"ID\") ON UPDATE CASCADE, " +
            "FOREIGN KEY (\"ProjektID\") REFERENCES \"Tab_Projekt\" (\"ID\") ON UPDATE CASCADE ON DELETE CASCADE" +
            ") STRICT";

        /// <summary>
        /// Die 24 Spalten in Schemareihenfolge — genau die des CREATE-Textes. Sie werden
        /// beim Kopieren NAMENTLICH genannt: <c>SELECT *</c> haengt an der Reihenfolge
        /// zweier Schemastaende. Der Nachweis haelt diese Liste gegen den CREATE-Text.
        /// </summary>
        public static readonly string[] SPALTEN =
        {
            "ID", "ProjektID", "StammID", "KomponentenID", "KategorieID",
            "EingegebenerWert", "Worstcase", "Bestcase", "Nutzungsdauer",
            "Worstcase_Nutzungsdauer", "Bestcase_Nutzungsdauer", "Einheit", "Gruppe",
            "Kostenart", "Bemessung", "IstErloes", "Menge", "Einheitpreis", "VorlageID",
            "StartJahr", "ID_Anlage", "ID_AnlageGeraet", "IstPflicht", "NutzungsdauerID"
        };

        /// <summary>
        /// Die fuenf Indizes der Tabelle, Wort fuer Wort der Bestand. Sie fallen mit der
        /// alten Tabelle und entstehen danach neu; <c>IF NOT EXISTS</c> macht auch sie
        /// wiederholbar.
        /// </summary>
        public static readonly string[] SQL_INDIZES =
        {
            "CREATE INDEX IF NOT EXISTS \"Tab_ProjektWerte_KategorieID\" ON \"" + TABELLE + "\" (\"KategorieID\")",
            "CREATE INDEX IF NOT EXISTS \"KomponentenID\" ON \"" + TABELLE + "\" (\"KomponentenID\")",
            "CREATE INDEX IF NOT EXISTS \"ProjektID\" ON \"" + TABELLE + "\" (\"ProjektID\")",
            "CREATE INDEX IF NOT EXISTS \"Tab_KostenfaktorTab_ProjektWerte\" ON \"" + TABELLE + "\" (\"StammID\")",
            "CREATE INDEX IF NOT EXISTS \"Tab_KostenGruppenKatalogTab_ProjektWerte\" ON \"" + TABELLE + "\" (\"Gruppe\")"
        };

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Traegt der Fremdschluessel auf <see cref="KATALOG"/> noch
        /// <c>ON DELETE CASCADE</c>? 1 = umzubauen, 0 = nichts zu tun (schon umgebaut,
        /// oder die Tabelle gibt es gar nicht).
        ///
        /// <para>Gefragt wird <c>pragma_foreign_key_list</c> und nicht der CREATE-Text:
        /// Die REGEL ist die Frage, nicht ihre Schreibweise. Ein <c>LIKE '%CASCADE%'</c>
        /// fiele ausserdem auf die drei anderen Fremdschluessel herein, die ihr
        /// <c>ON UPDATE CASCADE</c> behalten.</para>
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM pragma_foreign_key_list('" + TABELLE + "') " +
                   "WHERE \"table\" = '" + KATALOG + "' AND on_delete = '" + ALTE_LOESCHREGEL + "'";
        }

        /// <summary>
        /// Die GEGENPROBE: Steht die neue Regel? 1 = ja. Sie gehoert in den
        /// Migrationsbericht — „nichts mehr mit CASCADE" allein belegt noch nicht, dass
        /// der Fremdschluessel ueberhaupt noch da ist.
        /// </summary>
        public static string ZaehlungNeueRegel()
        {
            return "SELECT COUNT(*) FROM pragma_foreign_key_list('" + TABELLE + "') " +
                   "WHERE \"table\" = '" + KATALOG + "' AND on_delete = '" + LOESCHREGEL + "'";
        }

        /// <summary>Wie viele Zeilen traegt die Tabelle? Vor und nach dem Umbau dieselbe Zahl.</summary>
        public static string ZaehlungZeilen()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\"";
        }

        /// <summary>
        /// Ist der Umbau noetig? Eine nicht lesbare Zaehlung gilt als „nichts zu tun" —
        /// dieselbe Regel wie bei <see cref="SpeicherAuslegungStrict.UmbauNoetig"/>.
        /// </summary>
        public static bool UmbauNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }

        // =================================================================
        //  Die Anweisungen des Umbaus
        // =================================================================

        /// <summary>
        /// Der <c>CREATE</c>-Text der Zieltabelle unter dem Hilfsnamen — abgeleitet aus
        /// <see cref="SQL_TABELLE"/> und nicht abgeschrieben.
        ///
        /// <para>Damit gibt es weiterhin genau EINE Beschreibung dieser Tabelle. Ersetzt
        /// wird ausschliesslich der Tabellenname zwischen Leerzeichen und oeffnender
        /// Klammer; greift die Ersetzung nicht, bricht der Aufruf ab, statt
        /// stillschweigend die falsche Tabelle anzulegen.</para>
        /// </summary>
        public static string Neuanlage()
        {
            string alt = " \"" + TABELLE + "\" (";
            string neu = " \"" + HILFSTABELLE + "\" (";
            if (SQL_TABELLE.IndexOf(alt, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Der CREATE-Text von " + TABELLE + " hat eine andere Bauform als erwartet; " +
                    "der Hilfsname liess sich nicht einsetzen.");
            return SQL_TABELLE.Replace(alt, neu);
        }

        /// <summary>
        /// <c>DROP TABLE IF EXISTS</c> fuer eine der beiden Tabellen des Umbaus.
        /// <c>IF EXISTS</c> macht sowohl das Abraeumen eines Restes als auch den
        /// Zweitlauf gefahrlos.
        /// </summary>
        public static string Loeschung(string tabelle)
        {
            return "DROP TABLE IF EXISTS \"" + tabelle + "\"";
        }

        /// <summary>
        /// Der <c>INSERT … SELECT</c> mit namentlich genannten Spalten — in beide
        /// Richtungen dieselbe Liste, damit jede Spalte auf sich selbst faellt.
        /// </summary>
        public static string Uebernahme(string ziel, string quelle)
        {
            string spalten = Spaltenliste();
            return "INSERT INTO \"" + ziel + "\" (" + spalten + ") SELECT " + spalten +
                   " FROM \"" + quelle + "\"";
        }

        /// <summary>Der letzte Handgriff am Schema: der Hilfsname wird zum echten Namen.</summary>
        public static string Umbenennung(string von, string nach)
        {
            return "ALTER TABLE \"" + von + "\" RENAME TO \"" + nach + "\"";
        }

        /// <summary>Die Spaltenliste als Text, in Schemareihenfolge und in Anfuehrungszeichen.</summary>
        public static string Spaltenliste()
        {
            var teile = new List<string>(SPALTEN.Length);
            foreach (string s in SPALTEN) teile.Add("\"" + s + "\"");
            return string.Join(", ", teile.ToArray());
        }

        /// <summary>
        /// Raeumt einen etwaigen Zaehlerstand des Hilfsnamens ab — <c>sqlite_sequence</c>
        /// fuehrt keinen eindeutigen Index ueber <c>name</c>, ein blosses <c>INSERT</c>
        /// legte dort sonst eine zweite Zeile desselben Namens an.
        /// </summary>
        public static string ZaehlerAbraeumen()
        {
            return "DELETE FROM sqlite_sequence WHERE name = '" + HILFSTABELLE + "'";
        }

        /// <summary>
        /// Nimmt den AUTOINCREMENT-Stand der alten Tabelle an den Hilfsnamen mit. Ohne
        /// ihn fiele der Zaehler auf die groesste kopierte <c>ID</c> zurueck, und eine
        /// einmal vergebene Id kaeme ein zweites Mal heraus.
        /// </summary>
        public static string ZaehlerUebernehmen()
        {
            return "INSERT INTO sqlite_sequence (name, seq) SELECT '" + HILFSTABELLE + "', seq " +
                   "FROM sqlite_sequence WHERE name = '" + TABELLE + "'";
        }

        /// <summary>
        /// Gibt dem Zaehlerstand den echten Namen. Im legacy-Modus benennt
        /// <c>ALTER TABLE … RENAME</c> die Zeile in <c>sqlite_sequence</c> nicht mit;
        /// hat sie es doch getan, trifft diese Anweisung nichts mehr.
        /// </summary>
        public static string ZaehlerUmbenennen()
        {
            return "UPDATE sqlite_sequence SET name = '" + TABELLE + "' WHERE name = '" + HILFSTABELLE + "'";
        }

        /// <summary>
        /// Die Anweisungen des Umbaus in AUSFUEHRUNGSREIHENFOLGE, je mit der Bezeichnung,
        /// unter der sie im Migrationsbericht erscheinen. EINE Liste fuer Migration,
        /// Werkzeug und Nachweis.
        ///
        /// <para>Die zwei <c>PRAGMA legacy_alter_table</c> gehoeren in DIESE Liste und
        /// nicht in <see cref="Umbauen"/>: Sie klammern genau eine Anweisung, und wer die
        /// Liste liest, soll sehen, welche.</para>
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Rest der Hilfstabelle " + HILFSTABELLE, Loeschung(HILFSTABELLE));
                yield return new KeyValuePair<string, string>(
                    "Hilfstabelle " + HILFSTABELLE + " (ON DELETE " + LOESCHREGEL + ")", Neuanlage());
                yield return new KeyValuePair<string, string>(
                    "Uebernahme der Zeilen", Uebernahme(HILFSTABELLE, TABELLE));
                yield return new KeyValuePair<string, string>(
                    "Zaehlerstand abraeumen", ZaehlerAbraeumen());
                yield return new KeyValuePair<string, string>(
                    "Zaehlerstand uebernehmen", ZaehlerUebernehmen());
                yield return new KeyValuePair<string, string>(
                    "alte Tabelle " + TABELLE, Loeschung(TABELLE));
                yield return new KeyValuePair<string, string>(
                    "Sichten nicht umschreiben (legacy an)", "PRAGMA legacy_alter_table = ON");
                yield return new KeyValuePair<string, string>(
                    "Umbenennung auf " + TABELLE, Umbenennung(HILFSTABELLE, TABELLE));
                yield return new KeyValuePair<string, string>(
                    "Sichten wieder pruefen (legacy aus)", "PRAGMA legacy_alter_table = OFF");
                yield return new KeyValuePair<string, string>(
                    "Zaehlerstand umbenennen", ZaehlerUmbenennen());

                foreach (string index in SQL_INDIZES)
                    yield return new KeyValuePair<string, string>("Index neu", index);
            }
        }

        // =================================================================
        //  Der Umbau
        // =================================================================

        /// <summary>
        /// Fuehrt den Umbau aus, wenn er noetig ist, und meldet zurueck, ob er gelaufen
        /// ist. Alle Anweisungen stehen in EINER Transaktion (siehe Klassenkopf).
        ///
        /// <para>Fehler werden DURCHGEREICHT, nicht gemeldet — wie bei jedem
        /// <c>DbVorgang</c>. Der Aufrufer entscheidet, ob daraus eine Berichtszeile, eine
        /// Konsolenzeile oder ein roter Test wird.</para>
        /// </summary>
        /// <returns>true, wenn umgebaut wurde; false, wenn nichts zu tun war.</returns>
        public static bool Umbauen()
        {
            if (!UmbauNoetig()) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("PRAGMA defer_foreign_keys = ON");
                    foreach (KeyValuePair<string, string> a in Anweisungen)
                        v.Ausfuehren(a.Value);
                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN. Sie geht
                    // nach dem Dispose in den Pool zurueck; ein spaeteres
                    // ALTER TABLE ... RENAME auf derselben Verbindung schriebe dann
                    // Sichten und Trigger nicht mehr um. Laeuft der Umbau durch, hat ihn
                    // die Anweisungsliste selbst schon abgeschaltet - diese Zeile ist die
                    // Klammer fuer den ABBRUCH zwischen den beiden PRAGMAs.
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }
            return true;
        }
    }
}
