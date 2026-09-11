using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // Tab_SpeicherAuslegung als STRICT-Tabelle (Migrationsschritt 74, Auftrag #178).
    //
    // WOZU. Jede Fachtabelle des SQLite-Zielschemas ist STRICT - das Grundschema, die
    // zwei Tabellen des Wechselrichterkatalogs (WechselrichterSchema, Schritt 65) und
    // die Strangzuordnung (AnlageStrangSchema, Schritt 66) allesamt. STRICT ist im Haus
    // kein Geschmack, sondern die Zusage, dass eine Spalte nur den Typ aufnimmt, den sie
    // deklariert: BETRIEB_SQLITE.md verlangt dafuer mindestens SQLite 3.37, und die
    // iOS-CI zaehlt die STRICT-Tabellen der Seed-Datenbank als STARTGATE (ios.yml).
    //
    // BEFUND (Nachweis des iOS-Laufs 41, 11.09.2026). Referenzlaeufe/Kenndaten_Test.sqlite
    // fuehrt 119 Tabellen, davon 117 STRICT. Die zwei ohne STRICT sind sqlite_sequence
    // (Systemtabelle, von SQLite selbst angelegt) und Tab_SpeicherAuslegung - die Tabelle
    // aus Schemaschritt 73 (Stromspeicher-Sync vom 11.09.2026). An genau EINER Stelle
    // war die Konvention damit verletzt.
    //
    // WARUM EIN EIGENER SCHRITT UND KEINE AENDERUNG AN SCHRITT 73. Schritt 73 ist
    // ausgeliefert: Die Testdatenbank steht auf 73, die Referenzbasis R7 ist darauf
    // eingefroren, und eine produktive Datenbank kann ihn bereits durchlaufen haben. Ein
    // nachtraeglich umgeschriebener Schritt erreicht sie nicht mehr - der Schemazaehler
    // steht dort schon auf 73. Der BESTAND braucht deshalb einen eigenen, hoeher
    // nummerierten Schritt.
    //
    // ZWEITE HAELFTE DESSELBEN BEFUNDS: der CREATE-Text. SpeicherAuslegungCtrl.SQL_TABELLE
    // ist die EINE Quelle, aus der sich Schritt 73, die stille Selbstanlage des
    // Controllers (SchemaSicherstellen) und das Werkzeug Testdatenbankschema bedienen. Er
    // traegt seit diesem Auftrag STRICT - sonst entstuende auf jeder NEUEN Datenbank
    // wieder eine Tabelle ohne STRICT, und Schritt 74 raeumte ihr ein Leben lang
    // hinterher. Der Text bleibt EINE Quelle; hier steht keine zweite Fassung davon,
    // sondern derselbe Text unter dem Hilfsnamen (siehe Neuanlage).
    //
    // WAS DER SCHRITT TUT - das Tabellenneubau-Rezept des SQLite-Handbuchs ("Making Other
    // Kinds Of Table Schema Changes"). SQLite kennt kein "ALTER TABLE ... STRICT": STRICT
    // steht hinter der schliessenden Klammer des CREATE und laesst sich nachtraeglich
    // nicht setzen. Also
    //
    //   1. einen Rest aus einem abgebrochenen Lauf abraeumen (DROP TABLE IF EXISTS),
    //   2. die Zieltabelle unter dem Hilfsnamen anlegen - derselbe CREATE-Text, STRICT,
    //      dieselben Spalten, NOT NULL, PRIMARY KEY und die zwei Fremdschluessel,
    //   3. INSERT INTO ... SELECT mit NAMENTLICH genannten Spalten (kein "SELECT *"),
    //   4. die alte Tabelle loeschen - der Index faellt mit ihr,
    //   5. den Hilfsnamen auf den echten Namen umbenennen,
    //   6. den eindeutigen Index neu anlegen (SpeicherAuslegungCtrl.SQL_INDEX).
    //
    // ALLES IN EINER TRANSAKTION, und das ist keine Zier: Zwischen Schritt 4 und 5 gibt
    // es einen Augenblick, in dem die Tabelle unter ihrem echten Namen NICHT existiert.
    // Braeche der Lauf dort ab, stuende die Nutzlast unter dem Hilfsnamen, und der
    // naechste Lauf raeumte sie in Schritt 1 weg. Die Klammer schliesst dieses Fenster.
    // Sie ist auch der Grund, warum der Umbau ueber DataRepository.Vorgang() geht und
    // nicht ueber die Einzelanweisungen des SQLite-Werkzeugkastens: Die Zugriffsschicht
    // oeffnet je Anweisung eine Verbindung aus dem Pool - eine ueber mehrere Aufrufe
    // gespannte Transaktion gibt es dort nicht, und ein "PRAGMA foreign_keys = OFF"
    // waere schon beim naechsten Aufruf wieder vergessen.
    //
    // FREMDSCHLUESSEL. Das Rezept des Handbuchs schaltet sie fuer den Umbau ab; das geht
    // INNERHALB einer Transaktion nicht (PRAGMA foreign_keys ist dort ein No-op). Der
    // Vorgang setzt deshalb "PRAGMA defer_foreign_keys = ON" - das Muster, mit dem auch
    // Werkzeuge/Auslieferungsvorlage seine Loeschkaskade fasst: Die Beziehungen werden
    // dann EINMAL am Commit beurteilt statt Anweisung fuer Anweisung. Fuer diesen Umbau
    // aendert das am Ergebnis nichts - die Kopie zielt auf dieselben Elternzeilen wie das
    // Original, und das implizite DELETE eines DROP TABLE trifft eine KINDtabelle, kann
    // also keine Beziehung verletzen -, aber es haelt den Zwischenstand aus dem Urteil
    // heraus, so wie das Rezept es meint.
    //
    // WIEDERHOLBAR. Zaehlung() fragt sqlite_master, ob es die Tabelle OHNE STRICT gibt.
    // Ist sie schon STRICT - oder gibt es sie gar nicht, weil eine frische Datenbank sie
    // erst beim ersten Zugriff bekommt -, tut der Schritt nichts. Ein zweiter Lauf
    // aendert keine Zeile und keine Datenbankseite.
    //
    // ERGEBNISNEUTRAL. Der Schritt kopiert Zeilen, er rechnet nicht. Kein Wert, kein Typ,
    // keine Reihenfolge aendert sich - auch die ID bleibt, weil sie namentlich mitkopiert
    // wird. Der Referenzlauf muss vor und nach dem Umbau byte-gleiche CSV liefern; das
    // ist die Abnahme (Referenzlaeufe/LIESMICH.md, Nachtrag zum Schemastand 74).
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // KlimaWaisenBereinigung (62), BhkwLeistungsgrenzeVorgabe (67) und
    // StromspeicherFirmaNachtrag (68): Die Anweisungen brauchen DREI Leser - den
    // Schemaschritt in WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs
    // (Access-Zweig, deshalb nicht im Kern), das Werkzeug Werkzeuge/Testdatenbankschema
    // und den Nachweis in EPOS.Kern.Tests. Stuenden sie dort, muessten die anderen beiden
    // sie abschreiben - drei Wahrheiten ueber denselben Umbau.
    // ====================================================================================
    public static class SpeicherAuslegungStrict
    {
        /// <summary>Die Tabelle, die der Schritt umbaut.</summary>
        public const string TABELLE = "Tab_SpeicherAuslegung";

        /// <summary>
        /// Der Hilfsname, unter dem die Zieltabelle entsteht, bevor sie den echten Namen
        /// uebernimmt. Er lebt nur innerhalb des Umbaus; ausserhalb einer laufenden
        /// Transaktion gibt es ihn in keiner Datenbank.
        /// </summary>
        public const string HILFSTABELLE = TABELLE + "_neu";

        /// <summary>
        /// Die Spalten in Schemareihenfolge — genau die des CREATE-Textes
        /// <see cref="SpeicherAuslegungCtrl.SQL_TABELLE"/>. Sie werden beim Kopieren
        /// NAMENTLICH genannt: <c>SELECT *</c> haengt an der Reihenfolge zweier
        /// Schemastaende, und die Nutzlast (<c>Daten</c>) darf auf keinen Fall in der
        /// falschen Spalte landen. Der Nachweis haelt diese Liste gegen den CREATE-Text.
        /// </summary>
        public static readonly string[] SPALTEN =
        {
            "ID", "ID_Projekt", "ID_Energieanlage", "Bezeichner", "Daten", "Stand"
        };

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Wie viele Tabellen dieses Namens gibt es OHNE <c>STRICT</c>? 1 = umzubauen,
        /// 0 = nichts zu tun (schon STRICT, oder noch gar nicht vorhanden).
        ///
        /// <para>Geprüft wird das WORT am Ende des <c>CREATE</c>-Textes und nicht ein
        /// <c>LIKE '%STRICT%'</c> — dieselbe Schreibweise, mit der
        /// <c>Werkzeuge/Auslieferungsvorlage</c> seine STRICT-Tabellen zählt. Ein
        /// <c>LIKE</c> fiele auf eine Spalte oder einen Prüfausdruck herein, in dem das
        /// Wort zufällig vorkommt.</para>
        /// </summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" +
                   TABELLE + "' AND upper(trim(substr(sql, length(sql) - 6))) <> 'STRICT'";
        }

        // =================================================================
        //  Die Anweisungen des Umbaus
        // =================================================================

        /// <summary>
        /// Der <c>CREATE</c>-Text der Zieltabelle unter dem Hilfsnamen — abgeleitet aus
        /// <see cref="SpeicherAuslegungCtrl.SQL_TABELLE"/> und nicht abgeschrieben.
        ///
        /// <para>Damit gibt es weiterhin genau EINE Beschreibung dieser Tabelle: Wer dort
        /// eine Spalte ergänzt, ergänzt sie hier mit. Ersetzt wird ausschließlich der
        /// Tabellenname zwischen Leerzeichen und öffnender Klammer; greift die Ersetzung
        /// nicht, bricht der Aufruf ab, statt stillschweigend die falsche Tabelle
        /// anzulegen.</para>
        /// </summary>
        public static string Neuanlage()
        {
            string quelle = SpeicherAuslegungCtrl.SQL_TABELLE;
            string alt = " " + TABELLE + " (";
            string neu = " " + HILFSTABELLE + " (";
            if (quelle.IndexOf(alt, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(
                    "Der CREATE-Text von " + TABELLE + " hat eine andere Bauform als erwartet; " +
                    "der Hilfsname liess sich nicht einsetzen.");
            return quelle.Replace(alt, neu);
        }

        /// <summary>
        /// <c>DROP TABLE IF EXISTS</c> für eine der beiden Tabellen des Umbaus.
        /// <c>IF EXISTS</c> macht sowohl das Abräumen eines Restes als auch den
        /// Zweitlauf gefahrlos.
        /// </summary>
        public static string Loeschung(string tabelle)
        {
            return "DROP TABLE IF EXISTS \"" + tabelle + "\"";
        }

        /// <summary>
        /// Der <c>INSERT … SELECT</c> mit namentlich genannten Spalten — in beide
        /// Richtungen dieselbe Liste, damit jede Spalte auf sich selbst fällt.
        /// </summary>
        public static string Uebernahme(string ziel, string quelle)
        {
            string spalten = Spaltenliste();
            return "INSERT INTO \"" + ziel + "\" (" + spalten + ") SELECT " + spalten +
                   " FROM \"" + quelle + "\"";
        }

        /// <summary>Der letzte Handgriff: der Hilfsname wird zum echten Namen.</summary>
        public static string Umbenennung(string von, string nach)
        {
            return "ALTER TABLE \"" + von + "\" RENAME TO \"" + nach + "\"";
        }

        /// <summary>Die Spaltenliste als Text, in Schemareihenfolge.</summary>
        public static string Spaltenliste()
        {
            return string.Join(", ", SPALTEN);
        }

        /// <summary>
        /// Die sechs Anweisungen des Umbaus in Ausführungsreihenfolge, je mit der
        /// Bezeichnung, unter der sie im Migrationsbericht erscheinen. EINE Liste für
        /// Migration, Werkzeug und Nachweis.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Anweisungen
        {
            get
            {
                yield return new KeyValuePair<string, string>(
                    "Rest der Hilfstabelle " + HILFSTABELLE, Loeschung(HILFSTABELLE));
                yield return new KeyValuePair<string, string>(
                    "Hilfstabelle " + HILFSTABELLE + " (STRICT)", Neuanlage());
                yield return new KeyValuePair<string, string>(
                    "Uebernahme der Zeilen", Uebernahme(HILFSTABELLE, TABELLE));
                yield return new KeyValuePair<string, string>(
                    "alte Tabelle " + TABELLE, Loeschung(TABELLE));
                yield return new KeyValuePair<string, string>(
                    "Umbenennung auf " + TABELLE, Umbenennung(HILFSTABELLE, TABELLE));
                yield return new KeyValuePair<string, string>(
                    "Index idx_SpeicherAuslegung", SpeicherAuslegungCtrl.SQL_INDEX);
            }
        }

        // =================================================================
        //  Der Umbau
        // =================================================================

        /// <summary>
        /// Führt den Umbau aus, wenn er nötig ist, und meldet zurück, ob er gelaufen ist.
        /// Alle Anweisungen stehen in EINER Transaktion (siehe Klassenkopf).
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
                v.Ausfuehren("PRAGMA defer_foreign_keys = ON");
                foreach (KeyValuePair<string, string> a in Anweisungen)
                    v.Ausfuehren(a.Value);
                v.Commit();
            }
            return true;
        }

        /// <summary>
        /// Gibt es die Tabelle und trägt sie noch kein <c>STRICT</c>? Die Auskunft geht
        /// über <see cref="Zaehlung"/> — dieselbe Frage, die auch das Werkzeug und der
        /// Migrationsbericht stellen.
        /// </summary>
        public static bool UmbauNoetig()
        {
            object wert = DataRepository.ExecuteScalar(Zaehlung());
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }
    }
}
