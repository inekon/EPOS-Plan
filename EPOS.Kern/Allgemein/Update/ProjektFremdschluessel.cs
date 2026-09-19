using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE PROJEKTTABELLEN BEKOMMEN IHREN FREMDSCHLUESSEL AUF Tab_Projekt
    // - Migrationsschritt 96 (Anwenderentscheid vom 19.09.2026).
    //
    // WOZU. Ein Projekt ist der Anker von rund fuenfzig Tabellen. Zwanzig davon tragen
    // ihren Fremdschluessel auf Tab_Projekt seit der Access-Uebernahme, ACHTUNDZWANZIG
    // nicht - dieselbe Beziehung, dieselbe Spalte (ID_Projekt bzw. ProjektID), nur ohne
    // Zusage. Die Folge steht messbar in der Testdatenbank: 1.668 Zeilen zeigen dort auf
    // Projekte, die es nicht mehr gibt. Sie sind beim Loeschen eines Projekts liegen
    // geblieben, weil kein Fremdschluessel sie mitgenommen hat und der Loeschweg des
    // Controllers sie nicht kannte.
    //
    // WAS DER SCHRITT HERSTELLT. Je Tabelle und Projektspalte
    //
    //     FOREIGN KEY ("<Spalte>") REFERENCES "Tab_Projekt" ("ID")
    //                              ON DELETE CASCADE ON UPDATE CASCADE
    //
    // Ab hier nimmt ein geloeschtes Projekt seine Zeilen mit, und eine neue Waise kann
    // gar nicht mehr entstehen. Tab_Applikation (ID_Projekt ist dort das zuletzt
    // geoeffnete Projekt, 0 = keines) und Tab_Kenndaten_Kuehlung_STAMM (Katalogtabelle,
    // ID_Projekt = 0) bleiben BENANNT aussen vor.
    //
    // DER ZIELTEXT WIRD NICHT ABGESCHRIEBEN. Achtundzwanzig CREATE-Texte in einer
    // Quelldatei waeren achtundzwanzig zweite Wahrheiten, die beim naechsten ADD COLUMN
    // veralten. Der Zieltext entsteht deshalb aus dem GELTENDEN sqlite_master.sql der
    // Tabelle: die FK-Klauseln vor das schliessende ") STRICT" gesetzt, sonst Zeichen fuer
    // Zeichen der Bestand. Wer eine Spalte ergaenzt, ergaenzt hier nichts.
    //
    // DAS REZEPT - UND WARUM ES ANDERS IST ALS IN DEN SCHRITTEN 74 UND 81. Jene bauen
    // KINDtabellen um; ein DROP TABLE kann dort keine fremde Zeile mitreissen. Dieser
    // Schritt baut ELTERNtabellen um: Tab_WP traegt drei Kindtabellen mit ON DELETE
    // CASCADE, Tab_Waermebedarf und Tab_Stromganglinie je ihre Ganglinienpunkte. Bei
    // eingeschalteten Fremdschluesseln fuehrt DROP TABLE ein implizites DELETE FROM aus,
    // und das LOEST DIE KASKADE AUS - die Kindzeilen waeren weg. Gemessen: defer_foreign_keys
    // hilft dagegen nicht, es verschiebt die PRUEFUNG, nicht die AKTION. Deshalb laeuft
    // der Umbau ueber DataRepository.VorgangOhneFremdschluessel(), also mit
    // "PRAGMA foreign_keys = OFF" VOR der Transaktion - Schritt 1 des Rezepts im
    // SQLite-Handbuch ("Making Other Kinds Of Table Schema Changes"). Das ist der Helfer,
    // auf den der Kopfkommentar des SQLite-Werkzeugkastens in SchemaMigration.cs
    // vorausverweist ("ein Helfer dafuer entsteht ERST, wenn der erste Schritt ihn
    // wirklich braucht").
    //
    // UMBENENNEN STATT DROPPEN. Die alte Tabelle wird nicht gedroppt und die neue
    // umbenannt, sondern umgekehrt: Die alte weicht auf den Hilfsnamen aus, die neue
    // entsteht sofort unter dem ECHTEN Namen. Der Grund ist der Namensraum der Indizes -
    // sie wandern beim Umbenennen mit und geben ihre Namen erst frei, wenn die
    // Hilfstabelle faellt. Und es ist ehrlicher: Die Zieltabelle traegt nie einen anderen
    // Namen als ihren.
    //
    // WAISEN WERDEN GEZAEHLT, GEHEILT ODER GELOESCHT - NIE STILL UEBERGANGEN. Ohne
    // Bereinigung scheiterte der neue Fremdschluessel am foreign_key_check. Zwei Wege,
    // und die Reihenfolge ist fachlich:
    //
    //   HEILEN, wo die Zeile an einem GUELTIGEN Elternsatz haengt. Tab_Kenndaten fuehrt
    //   1.446 Kennlinienzeilen mit ID_Projekt = 0 oder 1, die alle an einer echten
    //   Waermepumpe eines echten Projekts haengen (ID_WP, mit eigenem Fremdschluessel).
    //   Dort ist ID_Projekt eine ungepflegte Redundanz, kein Waisenbefund - 165 dieser
    //   Zeilen gehoeren zum Referenzprojekt 1045. Sie zu loeschen waere Datenverlust und
    //   wuerde die Referenzbasis brechen. Die Spalte wird deshalb aus dem Elternsatz
    //   nachgezogen. Dasselbe gilt fuer Tab_Stromverbrauchertyp, Tab_Brauchwassertyp und
    //   Tab_Prozesstyp ueber ihre jeweilige Elterntabelle.
    //
    //   LOESCHEN, was danach uebrig bleibt - Zeilen ohne Projekt UND ohne gueltigen
    //   Elternsatz. In der Testdatenbank sind das 202 Zeilen aus elf Tabellen.
    //
    // DIE NACHRAEUMUNG. Weil die Fremdschluessel fuer den Umbau ausgeschaltet sind,
    // kaskadiert SQLite das Waisenloeschen NICHT selbst. Der Schritt tut es deshalb
    // selbst, rekursiv ueber pragma_foreign_key_list: Was nach dem Loeschen auf eine
    // verschwundene Elternzeile zeigt, faellt mit (SET NULL / SET DEFAULT werden
    // stattdessen auf NULL gesetzt). Das Ergebnis ist genau das, was die Kaskade bei
    // eingeschalteten Fremdschluesseln getan haette - nur nachweisbar und im Bericht.
    //
    // EIGENE TRANSAKTION JE TABELLE. Ein Fehler an Tabelle 17 laesst die ersten sechzehn
    // stehen; der Schritt bleibt wiederholbar, weil jede fertige Tabelle beim naechsten
    // Lauf uebersprungen wird (UmbauNoetig fragt pragma_foreign_key_list). Die
    // Anwenderdatenbank vom 19.09.2026 traegt 24 der 28 Beziehungen bereits - sie bekommt
    // nur die vier fehlenden.
    //
    // ERGEBNISNEUTRAL FUER JEDES LEBENDE PROJEKT. Der Schritt kopiert Zeilen, er rechnet
    // nicht: Werte, Typen, IDs, sqlite_sequence-Staende, Spaltenreihenfolge, Indizes und
    // Sichten bleiben. Entfernt wird ausschliesslich, was zu KEINEM Projekt gehoert - was
    // also kein Rechenweg je gelesen hat. Der Referenzlauf bleibt byte-gleich.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION. Dieselbe Begruendung wie bei
    // SpeicherAuslegungStrict (74) und ProjektWerteLoeschschutz (81): Die Anweisungen
    // brauchen DREI Leser - den Schemaschritt in
    // WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs, das Werkzeug
    // Werkzeuge/Testdatenbankschema und den Nachweis in EPOS.Kern.Tests.
    // ====================================================================================

    /// <summary>
    /// Der Fremdschlüssel der Projekttabellen auf <c>Tab_Projekt</c> (Schemaschritt 96) —
    /// EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class ProjektFremdschluessel
    {
        /// <summary>Die Tabelle, auf die alle Beziehungen zeigen.</summary>
        public const string ZIEL = "Tab_Projekt";

        /// <summary>Die Spalte, auf die alle Beziehungen zeigen.</summary>
        public const string ZIELSPALTE = "ID";

        /// <summary>Die Löschregel, die der Schritt herstellt.</summary>
        public const string LOESCHREGEL = "CASCADE";

        /// <summary>Die Änderungsregel, die der Schritt herstellt.</summary>
        public const string AENDERUNGSREGEL = "CASCADE";

        /// <summary>
        /// Das Ende, das jede STRICT-Tabelle trägt und vor das die Klauseln treten.
        /// Trägt eine Tabelle es nicht, wird sie NICHT umgebaut (siehe <see cref="Zieltext"/>).
        /// </summary>
        public const string ENDE = ") STRICT";

        /// <summary>Der Zusatz, unter dem die alte Tabelle für die Dauer des Umbaus ausweicht.</summary>
        public const string HILFSZUSATZ = "_alt";

        /// <summary>Wie viele Runden die Nachräumung höchstens läuft (Schutz gegen Zyklen).</summary>
        public const int NACHRAEUMRUNDEN = 8;

        // =================================================================
        //  Der Katalog
        // =================================================================

        /// <summary>
        /// Eine Tabelle des Katalogs: ihr Name, ihre Projektspalte(n) und — wo es eine
        /// gibt — die Elternbeziehung, aus der sich eine ungepflegte Projektspalte
        /// HEILEN lässt, statt die Zeile zu verlieren.
        /// </summary>
        public sealed class Eintrag
        {
            /// <summary>Der Tabellenname.</summary>
            public string Tabelle { get; }

            /// <summary>
            /// Die Projektspalte(n) in Schemareihenfolge. Genau eine — außer
            /// <c>Tab_Variante</c>, die mit <c>ID_Projekt</c> (das Projekt selbst) und
            /// <c>ID_ProjektRef</c> (der Stamm, von dem die Variante abzweigt) zwei führt.
            /// </summary>
            public string[] Spalten { get; }

            /// <summary>Die Elterntabelle der Heilung, oder <c>null</c>.</summary>
            public string ElternTabelle { get; }

            /// <summary>Die Spalte DIESER Tabelle, die auf die Elterntabelle zeigt.</summary>
            public string ElternVerweis { get; }

            /// <summary>Die Schlüsselspalte der Elterntabelle.</summary>
            public string ElternSchluessel { get; }

            /// <summary>Die Projektspalte der Elterntabelle.</summary>
            public string ElternProjektspalte { get; }

            /// <summary>Hat dieser Eintrag eine Heilung?</summary>
            public bool Heilbar
            {
                get { return !string.IsNullOrEmpty(ElternTabelle); }
            }

            /// <summary>Ein Eintrag ohne Heilung.</summary>
            public Eintrag(string tabelle, params string[] spalten)
                : this(tabelle, spalten, null, null, null, null)
            {
            }

            /// <summary>Der vollständige Eintrag.</summary>
            public Eintrag(string tabelle, string[] spalten, string elternTabelle,
                           string elternVerweis, string elternSchluessel,
                           string elternProjektspalte)
            {
                Tabelle = tabelle;
                Spalten = spalten;
                ElternTabelle = elternTabelle;
                ElternVerweis = elternVerweis;
                ElternSchluessel = elternSchluessel;
                ElternProjektspalte = elternProjektspalte;
            }
        }

        /// <summary>
        /// DIE ACHTUNDZWANZIG TABELLEN in AUSFUEHRUNGSREIHENFOLGE.
        ///
        /// <para>Alphabetisch mit EINER bewussten Ausnahme: <c>Tab_Prozesswaerme</c> steht
        /// vor <c>Tab_Prozesstyp</c>, weil der Typ aus der Prozesswärme geheilt wird und
        /// die Elterntabelle dafür schon bereinigt sein soll. Bei
        /// <c>Tab_Brauchwasser</c>/<c>-typ</c> und
        /// <c>Tab_Stromverbraucher</c>/<c>-typ</c> ergibt das Alphabet dieselbe Ordnung
        /// von selbst.</para>
        ///
        /// <para>NICHT im Katalog und warum: <c>Tab_Applikation</c> (die Spalte merkt sich
        /// das zuletzt geöffnete Projekt, 0 = keines — ein Fremdschlüssel verböte diese 0)
        /// und <c>Tab_Kenndaten_Kuehlung_STAMM</c> (Katalogtabelle der Auslieferung,
        /// ID_Projekt durchgängig 0). Die Wache in den Tests hält den Katalog gegen die
        /// Datenbank: Jede andere Tabelle mit einer Projektspalte muss hier stehen.</para>
        /// </summary>
        public static readonly Eintrag[] Katalog =
        {
            new Eintrag("Berichtskonfiguration", "ProjektID"),
            new Eintrag("Tab_BHKW", "ID_Projekt"),
            new Eintrag("Tab_Brauchwasser", "ID_Projekt"),
            new Eintrag("Tab_Brauchwassertyp", new[] { "ID_Projekt" },
                        "Tab_Brauchwasser", "ID_Brauchwasser", "ID", "ID_Projekt"),
            new Eintrag("Tab_Ergebnis", "ID_Projekt"),
            new Eintrag("Tab_ErgebnisStromMatrix", "ID_Projekt"),
            new Eintrag("Tab_ErgebnisWirtSensitivitaet", "ID_Projekt"),
            new Eintrag("Tab_ErgebnisWirtschaftlichkeit", "ID_Projekt"),
            new Eintrag("Tab_Gebaeude", "ID_Projekt"),
            new Eintrag("Tab_Heizkessel", "ID_Projekt"),
            new Eintrag("Tab_Kenndaten", new[] { "ID_Projekt" },
                        "Tab_WP", "ID_WP", "ID", "ID_Projekt"),
            new Eintrag("Tab_Klimadaten", "ID_Projekt"),
            new Eintrag("Tab_PV", "ID_Projekt"),
            new Eintrag("Tab_ProjektPhotovoltaik", "ID_Projekt"),
            new Eintrag("Tab_Prozesswaerme", "ID_Projekt"),
            new Eintrag("Tab_Prozesstyp", new[] { "ID_Projekt" },
                        "Tab_Prozesswaerme", "ID_Prozesswaerme", "ID", "ID_Projekt"),
            new Eintrag("Tab_Quellprofil", "ID_Projekt"),
            new Eintrag("Tab_Solar", "ID_Projekt"),
            new Eintrag("Tab_Solarganglinie", "ID_Projekt"),
            new Eintrag("Tab_Solarkollektoren", "ID_Projekt"),
            new Eintrag("Tab_Stromganglinie", "ID_Projekt"),
            new Eintrag("Tab_Stromspeicher", "ID_Projekt"),
            new Eintrag("Tab_Stromverbraucher", "ID_Projekt"),
            new Eintrag("Tab_Stromverbrauchertyp", new[] { "ID_Projekt" },
                        "Tab_Stromverbraucher", "ID_Stromverbraucher", "ID", "ID_Projekt"),
            new Eintrag("Tab_Variante", "ID_Projekt", "ID_ProjektRef"),
            new Eintrag("Tab_WP", "ID_Projekt"),
            new Eintrag("Tab_Waermebedarf", "ID_Projekt"),
            new Eintrag("Tab_Wechselrichter", "ID_Projekt")
        };

        /// <summary>
        /// Die Tabellen mit einer Projektspalte, die BENANNT keinen Fremdschlüssel
        /// bekommen. Die Wache in den Tests liest diese Liste — eine dritte Ausnahme muss
        /// hier eingetragen und damit begründet werden.
        /// </summary>
        public static readonly string[] Ausgenommen =
        {
            "Tab_Applikation",
            "Tab_Kenndaten_Kuehlung_STAMM"
        };

        /// <summary>Die Spaltennamen, die im Haus eine Projektbeziehung tragen.</summary>
        public static readonly string[] Projektspalten =
        {
            "ID_Projekt", "ID_ProjektRef", "ProjektID"
        };

        /// <summary>Der Eintrag zu einem Tabellennamen, oder <c>null</c>.</summary>
        public static Eintrag Finde(string tabelle)
        {
            foreach (Eintrag e in Katalog)
                if (string.Equals(e.Tabelle, tabelle, StringComparison.OrdinalIgnoreCase))
                    return e;
            return null;
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>
        /// Steht der Fremdschlüssel dieser Spalte auf <see cref="ZIEL"/> schon? Gefragt
        /// wird <c>pragma_foreign_key_list</c> — die BEZIEHUNG ist die Frage, nicht ihre
        /// Schreibweise im CREATE-Text.
        /// </summary>
        public static string ZaehlungSpalte(string tabelle, string spalte)
        {
            return "SELECT COUNT(*) FROM pragma_foreign_key_list(?) " +
                   "WHERE \"table\" = ? AND \"from\" = ?";
        }

        /// <summary>Die Parameter zu <see cref="ZaehlungSpalte"/>.</summary>
        public static DbParam[] ParameterSpalte(string tabelle, string spalte)
        {
            return new[]
            {
                new DbParam("p1", tabelle),
                new DbParam("p2", ZIEL),
                new DbParam("p3", spalte)
            };
        }

        /// <summary>
        /// Ist der Umbau dieser Tabelle nötig? true, sobald mindestens eine ihrer
        /// Projektspalten noch ohne Fremdschlüssel auf <see cref="ZIEL"/> steht.
        ///
        /// <para>Eine Tabelle, die es auf dieser Datei nicht gibt, gilt als „nichts zu
        /// tun" — dieselbe Regel wie bei <see cref="SpeicherAuslegungStrict.UmbauNoetig"/>.</para>
        /// </summary>
        public static bool UmbauNoetig(string tabelle)
        {
            Eintrag e = Finde(tabelle);
            if (e == null) return false;
            if (!DataRepository.TabelleVorhanden(tabelle)) return false;

            foreach (string spalte in e.Spalten)
            {
                if (!DataRepository.SpalteVorhanden(tabelle, spalte)) continue;
                if (!FremdschluesselSteht(tabelle, spalte)) return true;
            }
            return false;
        }

        /// <summary>Trägt diese Spalte ihren Fremdschlüssel auf <see cref="ZIEL"/>?</summary>
        public static bool FremdschluesselSteht(string tabelle, string spalte)
        {
            object wert = DataRepository.ExecuteScalar(ZaehlungSpalte(tabelle, spalte),
                                                       ParameterSpalte(tabelle, spalte));
            if (wert == null || wert == DBNull.Value) return false;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>Wie viele Tabellen des Katalogs sind noch umzubauen?</summary>
        public static int Offen()
        {
            int offen = 0;
            foreach (Eintrag e in Katalog)
                if (UmbauNoetig(e.Tabelle)) offen++;
            return offen;
        }

        /// <summary>
        /// Der Zähltext der Waisen einer Spalte: Zeilen, deren Projektspalte gesetzt ist
        /// und trotzdem in keiner Zeile von <see cref="ZIEL"/> vorkommt.
        ///
        /// <para><c>IS NOT NULL</c> ist keine Zier: SQLite lässt einen NULL-Fremdschlüssel
        /// immer durch, eine NULL-Zeile ist also keine Waise.</para>
        /// </summary>
        public static string SqlWaisen(string tabelle, string spalte)
        {
            return "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte +
                   "\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"" + ZIEL +
                   "\" z WHERE z.\"" + ZIELSPALTE + "\" = \"" + tabelle + "\".\"" + spalte + "\")";
        }

        /// <summary>
        /// Wie viele Waisen trägt diese Tabelle — über alle ihre Projektspalten gezählt?
        /// −1, wenn es die Tabelle nicht gibt.
        /// </summary>
        public static long Waisen(string tabelle)
        {
            Eintrag e = Finde(tabelle);
            if (e == null || !DataRepository.TabelleVorhanden(tabelle)) return -1;

            long summe = 0;
            foreach (string spalte in e.Spalten)
            {
                if (!DataRepository.SpalteVorhanden(tabelle, spalte)) continue;
                object wert = DataRepository.ExecuteScalar(SqlWaisen(tabelle, spalte));
                if (wert != null && wert != DBNull.Value)
                    summe += Convert.ToInt64(wert, CultureInfo.InvariantCulture);
            }
            return summe;
        }

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Der Zieltext der Tabelle: ihr GELTENDER <c>sqlite_master.sql</c>, ergänzt um
        /// die Fremdschlüsselklauseln vor dem schließenden <see cref="ENDE"/>.
        ///
        /// <para>Greift die Stelle nicht — weil die Tabelle kein <c>STRICT</c> trägt oder
        /// anders gebaut ist als erwartet —, bricht der Aufruf ab, statt eine falsche
        /// Tabelle anzulegen. Das ist zugleich die STRICT-Wache des Schritts.</para>
        ///
        /// <para>Spalten, die schon einen Fremdschlüssel auf <see cref="ZIEL"/> tragen,
        /// bekommen KEINEN zweiten.</para>
        /// </summary>
        public static string Zieltext(string tabelle, string bestand, IList<string> offeneSpalten)
        {
            if (string.IsNullOrEmpty(bestand))
                throw new InvalidOperationException(
                    "Zu " + tabelle + " gibt es keinen CREATE-Text in sqlite_master.");

            string text = bestand.TrimEnd();
            if (!text.EndsWith(ENDE, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Die Tabelle " + tabelle + " endet nicht auf \"" + ENDE + "\" - sie ist " +
                    "keine STRICT-Tabelle oder anders gebaut als erwartet; " +
                    "Schemaschritt 96 baut sie deshalb NICHT um.");

            string kopf = "CREATE TABLE \"" + tabelle + "\" (";
            if (!text.StartsWith(kopf, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Der CREATE-Text von " + tabelle + " hat eine andere Bauform als erwartet " +
                    "(erwartet wurde der Beginn " + kopf + ").");

            var klauseln = new List<string>();
            foreach (string spalte in offeneSpalten)
                klauseln.Add(", FOREIGN KEY (\"" + spalte + "\") REFERENCES \"" + ZIEL +
                             "\" (\"" + ZIELSPALTE + "\") ON DELETE " + LOESCHREGEL +
                             " ON UPDATE " + AENDERUNGSREGEL);

            return text.Substring(0, text.Length - ENDE.Length) +
                   string.Join("", klauseln.ToArray()) + ENDE;
        }

        /// <summary>Der Hilfsname, unter den die alte Tabelle für die Dauer des Umbaus ausweicht.</summary>
        public static string Hilfsname(string tabelle)
        {
            return tabelle + HILFSZUSATZ;
        }

        // =================================================================
        //  Der Umbau
        // =================================================================

        /// <summary>
        /// Baut EINE Tabelle des Katalogs um und schreibt jede Zahl in den Bericht.
        /// Fehler werden DURCHGEREICHT — der Aufrufer entscheidet, ob daraus eine
        /// Berichtszeile, eine Konsolenzeile oder ein roter Test wird.
        /// </summary>
        /// <returns>true, wenn umgebaut wurde; false, wenn nichts zu tun war.</returns>
        public static bool Umbauen(string tabelle, IList<string> bericht)
        {
            Eintrag e = Finde(tabelle);
            if (e == null)
                throw new InvalidOperationException(
                    "Die Tabelle " + tabelle + " steht nicht im Katalog von Schemaschritt 96.");

            if (!DataRepository.TabelleVorhanden(tabelle))
            {
                Notiere(bericht, tabelle + ": nicht vorhanden - uebergangen.");
                return false;
            }

            var offeneSpalten = new List<string>();
            var alleSpalten = new List<string>();
            foreach (string spalte in e.Spalten)
            {
                if (!DataRepository.SpalteVorhanden(tabelle, spalte)) continue;
                alleSpalten.Add(spalte);
                if (!FremdschluesselSteht(tabelle, spalte)) offeneSpalten.Add(spalte);
            }

            if (alleSpalten.Count == 0)
            {
                Notiere(bericht, tabelle + ": keine Projektspalte vorhanden - uebergangen.");
                return false;
            }

            if (offeneSpalten.Count == 0)
            {
                Notiere(bericht, tabelle + ": uebersprungen - der Fremdschluessel steht bereits.");
                return false;
            }

            long geheilt = 0;
            long geloescht = 0;
            long nachgeraeumt = 0;
            var neueIndizes = new List<string>();
            long zeilenVorher;
            long zeilenNachher;

            using (DbVorgang v = DataRepository.VorgangOhneFremdschluessel())
            {
                try
                {
                    zeilenVorher = Zahl(v, "SELECT COUNT(*) FROM \"" + tabelle + "\"");

                    // ---- 1. Heilen, wo ein gueltiger Elternsatz die Antwort kennt.
                    if (e.Heilbar && DataRepository.TabelleVorhanden(e.ElternTabelle))
                        geheilt = Heilen(v, e);

                    // ---- 2. Was danach keinem Projekt gehoert, faellt.
                    foreach (string spalte in alleSpalten)
                        geloescht += v.Ausfuehren(
                            "DELETE FROM \"" + tabelle + "\" WHERE \"" + spalte +
                            "\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"" + ZIEL +
                            "\" z WHERE z.\"" + ZIELSPALTE + "\" = \"" + tabelle + "\".\"" + spalte + "\")");

                    // ---- 3. Die Kaskade von Hand - die Fremdschluessel sind ja aus.
                    if (geloescht > 0) nachgeraeumt = Nachraeumen(v, tabelle);

                    // ---- 4. Das Tabellenneubau-Rezept.
                    Neubau(v, tabelle, offeneSpalten, neueIndizes);

                    zeilenNachher = Zahl(v, "SELECT COUNT(*) FROM \"" + tabelle + "\"");
                    v.Commit();
                }
                finally
                {
                    // DER LEGACY-MODUS DARF DIE VERBINDUNG NICHT UEBERLEBEN - die Klammer
                    // fuer einen Abbruch zwischen den beiden PRAGMAs (Muster aus Schritt 81).
                    try { v.Ausfuehren("PRAGMA legacy_alter_table = OFF"); }
                    catch (Exception) { /* die Verbindung ist dann ohnehin am Ende */ }
                }
            }

            Notiere(bericht,
                    tabelle + ": Fremdschluessel auf " + ZIEL + " fuer " +
                    string.Join(", ", offeneSpalten.ToArray()) + " gesetzt; Waisen geheilt " +
                    Text(geheilt) + ", geloescht " + Text(geloescht) +
                    ", abhaengige Zeilen mitgeloescht " + Text(nachgeraeumt) +
                    ", Zeilen " + Text(zeilenVorher) + " -> " + Text(zeilenNachher) +
                    (neueIndizes.Count > 0
                        ? ", Index neu: " + string.Join(", ", neueIndizes.ToArray())
                        : "") + ".");
            return true;
        }

        /// <summary>
        /// Baut ALLE Tabellen des Katalogs um, jede in ihrer eigenen Transaktion, in
        /// Katalogreihenfolge.
        /// </summary>
        /// <returns>Die Zahl der tatsächlich umgebauten Tabellen.</returns>
        public static int Alle(IList<string> bericht)
        {
            int umgebaut = 0;
            foreach (Eintrag e in Katalog)
                if (Umbauen(e.Tabelle, bericht)) umgebaut++;
            return umgebaut;
        }

        // =================================================================
        //  Die Handgriffe
        // =================================================================

        /// <summary>
        /// Zieht die Projektspalte aus dem Elternsatz nach, wo die Zeile an einem
        /// GUELTIGEN Elternsatz hängt. Nur die erste Projektspalte wird geheilt — die
        /// zweite (<c>Tab_Variante.ID_ProjektRef</c>) hat keine Elternbeziehung, aus der
        /// sich ein Stammprojekt ableiten ließe.
        /// </summary>
        private static long Heilen(DbVorgang v, Eintrag e)
        {
            string t = e.Tabelle;
            string sp = e.Spalten[0];
            string et = e.ElternTabelle;
            string ev = e.ElternVerweis;
            string es = e.ElternSchluessel;
            string ep = e.ElternProjektspalte;

            if (!DataRepository.SpalteVorhanden(et, ep)) return 0;

            return v.Ausfuehren(
                "UPDATE \"" + t + "\" SET \"" + sp + "\" = " +
                "(SELECT e.\"" + ep + "\" FROM \"" + et + "\" e WHERE e.\"" + es + "\" = \"" + t + "\".\"" + ev + "\") " +
                "WHERE \"" + sp + "\" IS NOT NULL " +
                "AND NOT EXISTS (SELECT 1 FROM \"" + ZIEL + "\" z WHERE z.\"" + ZIELSPALTE + "\" = \"" + t + "\".\"" + sp + "\") " +
                "AND EXISTS (SELECT 1 FROM \"" + et + "\" e2 WHERE e2.\"" + es + "\" = \"" + t + "\".\"" + ev + "\" " +
                "AND EXISTS (SELECT 1 FROM \"" + ZIEL + "\" z2 WHERE z2.\"" + ZIELSPALTE + "\" = e2.\"" + ep + "\"))");
        }

        /// <summary>
        /// Die Kaskade von Hand: Was nach dem Löschen auf eine verschwundene Elternzeile
        /// zeigt, fällt mit — rekursiv, bis nichts mehr nachrückt.
        ///
        /// <para>Die Kindtabellen werden GEMESSEN (<c>pragma_foreign_key_list</c>) und
        /// nicht aufgezählt: Eine später hinzukommende Kindtabelle ist damit von selbst
        /// versorgt. <c>SET NULL</c> und <c>SET DEFAULT</c> werden geachtet — dort wird
        /// die Spalte auf NULL gesetzt statt die Zeile gelöscht.</para>
        /// </summary>
        private static long Nachraeumen(DbVorgang v, string tabelle)
        {
            long summe = 0;
            var runde = new List<string> { tabelle };

            for (int i = 0; i < NACHRAEUMRUNDEN && runde.Count > 0; i++)
            {
                var naechste = new List<string>();
                foreach (string eltern in runde)
                {
                    DataTable kinder = v.Lese(
                        "SELECT m.name AS kind, f.\"from\" AS von, f.\"to\" AS nach, f.on_delete AS regel " +
                        "FROM sqlite_master m JOIN pragma_foreign_key_list(m.name) f " +
                        "WHERE m.type = 'table' AND f.\"table\" = ?",
                        new DbParam("p1", eltern));

                    foreach (DataRow zeile in kinder.Rows)
                    {
                        string kind = Convert.ToString(zeile["kind"], CultureInfo.InvariantCulture);
                        string von = Convert.ToString(zeile["von"], CultureInfo.InvariantCulture);
                        string nach = Convert.ToString(zeile["nach"], CultureInfo.InvariantCulture);
                        string regel = Convert.ToString(zeile["regel"], CultureInfo.InvariantCulture);
                        if (string.IsNullOrEmpty(nach)) nach = ZIELSPALTE;

                        string wo = "\"" + von + "\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"" +
                                    eltern + "\" e WHERE e.\"" + nach + "\" = \"" + kind + "\".\"" + von + "\")";

                        int betroffen = string.Equals(regel, "SET NULL", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(regel, "SET DEFAULT", StringComparison.OrdinalIgnoreCase)
                            ? v.Ausfuehren("UPDATE \"" + kind + "\" SET \"" + von + "\" = NULL WHERE " + wo)
                            : v.Ausfuehren("DELETE FROM \"" + kind + "\" WHERE " + wo);

                        if (betroffen > 0)
                        {
                            summe += betroffen;
                            if (!naechste.Contains(kind) &&
                                !string.Equals(kind, eltern, StringComparison.OrdinalIgnoreCase))
                                naechste.Add(kind);
                        }
                    }
                }
                runde = naechste;
            }
            return summe;
        }

        /// <summary>
        /// Das Tabellenneubau-Rezept: Die alte Tabelle weicht auf den Hilfsnamen aus, die
        /// neue entsteht unter dem echten Namen, die Zeilen ziehen namentlich um, der
        /// AUTOINCREMENT-Stand reist mit, die alte fällt, die Indizes stehen wieder.
        /// </summary>
        private static void Neubau(DbVorgang v, string tabelle, IList<string> offeneSpalten,
                                   IList<string> neueIndizes)
        {
            string alt = Hilfsname(tabelle);

            string bestand = Text(v.Skalar(
                "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                new DbParam("p1", tabelle)));
            string ziel = Zieltext(tabelle, bestand, offeneSpalten);

            // Spalten NAMENTLICH - "SELECT *" haengt an der Reihenfolge zweier Schemastaende.
            var spalten = new List<string>();
            DataTable info = v.Lese("SELECT name FROM pragma_table_info(?)", new DbParam("p1", tabelle));
            foreach (DataRow zeile in info.Rows)
                spalten.Add("\"" + Convert.ToString(zeile["name"], CultureInfo.InvariantCulture) + "\"");
            string spaltenliste = string.Join(", ", spalten.ToArray());

            // Die Indizes der Tabelle, Wort fuer Wort der Bestand. Sie wandern beim
            // Umbenennen mit und fallen mit der Hilfstabelle; danach entstehen sie neu.
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
                v.Ausfuehren(MitIfNotExists(anweisung));

            // Der Loeschweg der Kaskade sucht die Kindzeilen ueber die Projektspalte -
            // ohne Index waere das ein Tabellendurchlauf je geloeschtem Projekt.
            foreach (string spalte in offeneSpalten)
            {
                if (IndexDeckt(v, tabelle, spalte)) continue;
                string name = tabelle + "_" + spalte;
                v.Ausfuehren("CREATE INDEX IF NOT EXISTS \"" + name + "\" ON \"" + tabelle +
                             "\" (\"" + spalte + "\")");
                neueIndizes.Add(name);
            }
        }

        /// <summary>
        /// Gibt es einen Index, dessen ERSTE Spalte diese ist? Nur ein solcher hilft dem
        /// Löschweg der Kaskade.
        /// </summary>
        private static bool IndexDeckt(DbVorgang v, string tabelle, string spalte)
        {
            DataTable namen = v.Lese(
                "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = ?",
                new DbParam("p1", tabelle));

            foreach (DataRow zeile in namen.Rows)
            {
                string name = Convert.ToString(zeile["name"], CultureInfo.InvariantCulture);
                DataTable erste = v.Lese("SELECT name FROM pragma_index_info(?) WHERE seqno = 0",
                                         new DbParam("p1", name));
                if (erste.Rows.Count == 0) continue;
                if (string.Equals(Convert.ToString(erste.Rows[0]["name"], CultureInfo.InvariantCulture),
                                  spalte, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Setzt <c>IF NOT EXISTS</c> in einen CREATE-INDEX-Text, der es noch nicht
        /// trägt — dieselbe Wiederholbarkeit wie bei den festen Indexlisten der
        /// Schritte 74 und 81.
        /// </summary>
        public static string MitIfNotExists(string anweisung)
        {
            if (anweisung == null) return null;
            if (anweisung.IndexOf("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase) >= 0)
                return anweisung;

            const string unique = "CREATE UNIQUE INDEX ";
            const string schlicht = "CREATE INDEX ";

            if (anweisung.StartsWith(unique, StringComparison.OrdinalIgnoreCase))
                return unique + "IF NOT EXISTS " + anweisung.Substring(unique.Length);
            if (anweisung.StartsWith(schlicht, StringComparison.OrdinalIgnoreCase))
                return schlicht + "IF NOT EXISTS " + anweisung.Substring(schlicht.Length);

            return anweisung;
        }

        // =================================================================
        //  Kleinkram
        // =================================================================

        private static long Zahl(DbVorgang v, string sql)
        {
            object wert = v.Skalar(sql);
            if (wert == null || wert == DBNull.Value) return -1;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }

        private static string Text(long zahl)
        {
            return zahl < 0 ? "unbekannt" : zahl.ToString(CultureInfo.InvariantCulture);
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
