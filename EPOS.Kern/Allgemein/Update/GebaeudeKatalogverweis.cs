using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DAS PROJEKTGEBAEUDE KENNT SEINEN KATALOGSATZ - Migrationsschritt 121
    // (Welle #468, Anwenderentscheid "Schemaschritt vornehmen" nach #465; Konzept
    // Administrationsdialoge 7.1 (a)).
    //
    // WOZU. Tab_Gebaeude (die Projektkopie) fuehrte ID_Projekt und ID_ProjektGebaeude,
    // aber keinen Verweis auf den Katalogsatz in Tab_Gebaeude_STAMM. Die Loeschsperre der
    // Gebaeudeverwaltung (GebaeudeStammCtrl.Projektverwendung) fand ein benutztes Gebaeude
    // deshalb allein ueber den NAMEN der Kopie - und verlor es, sobald jemand den
    // Katalogsatz umbenannte: Die Kopie hiess weiter wie vorher, der Katalogsatz nicht
    // mehr, und "Loeschen" war frei. Die Hausregel sagt dazu: "Neue Beziehungen ueber IDs,
    // nicht ueber Textfelder" (CLAUDE.md, Datenhaltung).
    //
    // DIE SPALTE IST NULLBAR, UND ZWAR DAUERHAFT. NULL heisst "diese Kopie haengt an
    // keinem Katalogsatz": ein Katalogsatz, den jemand geloescht hat, ein Projekt aus einem
    // Paket, dessen Gebaeude am Ziel nicht im Katalog steht, und jede Kopie, deren Name
    // beim Nachtragen keinen Katalogsatz traf. Der Rueckfall auf den Namen bleibt deshalb
    // in der Loeschsperre stehen, wo die Spalte NULL ist.
    //
    // ON DELETE SET NULL, NICHT RESTRICT. Die Projektkopie traegt alle Werte selbst, und
    // die Simulation liest nur sie (Kopiersemantik, KatalogRegistry). Ein geloeschter
    // Katalogsatz nimmt der Kopie also nichts - nur die Klammer ist weg, und genau das
    // sagt NULL. RESTRICT machte aus der WEICHEN Sperre der Verwaltung (sie nennt die
    // Projekte und laesst die freien Saetze loeschen) einen harten Datenbankfehler auf
    // JEDEM Loeschweg - Dublettenbereinigung, "Gebaeude in DB loeschen" des Projektdialogs,
    // Auslieferungsvorlage - und liesse sich in SQLite nur ueber einen Tabellenneubau
    // wieder zuruecknehmen. Die Sperre gehoert dem Kern (GebaeudeStammCtrl.Loeschen), die
    // Datenbank sichert nur, dass kein Verweis ins Leere zeigt. Vorbild ist der
    // Katalogverweis der Waermepumpe (Schritt 80, WaermepumpeKatalogverweis).
    //
    // DER NACHTRAG RAET NICHT. Gefuellt wird nur, wo der Gebaeudename GENAU EINEN
    // Katalogsatz trifft. Tab_Gebaeude_STAMM fuehrt einen eindeutigen Index ueber den
    // Bezeichner, mehr als ein Treffer ist dort also nicht zu erwarten - die Bedingung
    // "= 1" steht trotzdem, weil sie dasselbe sagt wie beim Waermepumpenverweis und eine
    // Datenbank ohne den Index (Altbestand) nicht falsch zuordnet.
    //
    // ERGEBNISNEUTRAL. Kein Rechenweg liest Tab_Gebaeude.ID_Gebaeude_Stamm; die Simulation
    // liest die Werte der Kopie. Der Referenzlauf bleibt byte-gleich.
    //
    // WARUM HIER UND NICHT IN DER MIGRATION: dieselbe Begruendung wie bei Schritt 80 -
    // drei Leser (Migration der Schale, Werkzeuge/Testdatenbankschema, Nachzieh-Liste der
    // Tests), eine Quelle.
    // ====================================================================================

    /// <summary>
    /// Der Katalogverweis des Projektgebäudes (Schemaschritt 121) — EINE Quelle für
    /// Migration, Werkzeug, Übernahme, Projekttransfer und Nachweis.
    /// </summary>
    public static class GebaeudeKatalogverweis
    {
        /// <summary>Die Projektkopie.</summary>
        public const string TABELLE = "Tab_Gebaeude";

        /// <summary>Der Katalog.</summary>
        public const string TABELLE_STAMM = "Tab_Gebaeude_STAMM";

        /// <summary>Die neue Verweisspalte.</summary>
        public const string SPALTE = "ID_Gebaeude_Stamm";

        /// <summary>Der Suchweg der Löschsperre: die Projektkopien EINES Katalogsatzes.</summary>
        public const string INDEX = "Tab_Gebaeude_ID_Gebaeude_Stamm";

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        /// <summary>
        /// Alles hinter dem Spaltennamen eines <c>ADD COLUMN</c> — dieselbe Bauart wie
        /// <see cref="WaermepumpeKatalogverweis.TYP_SPALTE"/>.
        ///
        /// <para><b>Mit <c>REFERENCES</c>, ohne <c>DEFAULT</c>.</b> SQLite lässt ein
        /// nachträgliches <c>ADD COLUMN</c> mit Fremdschlüssel genau dann zu, wenn der
        /// Vorgabewert NULL ist — und NULL ist hier ohnehin die Aussage „an keinem
        /// Katalogsatz". <c>INTEGER</c> ist in der STRICT-Tabelle zulässig.</para>
        ///
        /// <para><b><c>ON DELETE SET NULL</c></b> — die Begründung steht im Kopf dieser
        /// Datei.</para>
        /// </summary>
        public const string TYP_SPALTE =
            "INTEGER REFERENCES \"" + TABELLE_STAMM + "\" (\"ID\") ON DELETE SET NULL";

        /// <summary>Die Spalte — <c>ALTER TABLE ... ADD COLUMN</c>.</summary>
        public const string SQL_SPALTE =
            "ALTER TABLE \"" + TABELLE + "\" ADD COLUMN \"" + SPALTE + "\" " + TYP_SPALTE;

        /// <summary>Der Index über den Katalogverweis; <c>IF NOT EXISTS</c> macht ihn wiederholbar.</summary>
        public const string SQL_INDEX =
            "CREATE INDEX IF NOT EXISTS \"" + INDEX + "\" ON \"" + TABELLE + "\" (\"" + SPALTE + "\")";

        // DIE UNTERABFRAGE "wie viele Katalogsaetze heissen so?" STEHT MEHRFACH WOERTLICH
        // DA - mit Absicht, aus demselben Grund wie in WaermepumpeKatalogverweis: Als
        // eigener Satzteil bestuende sie keine Pruefung des SqlDialektPruefers.

        /// <summary>
        /// Trägt den Verweis nach, wo der Gebäudename EINDEUTIG trifft.
        ///
        /// <para><b>Wiederholbar</b> über <c>ID_Gebaeude_Stamm IS NULL</c>: Ein zweiter
        /// Lauf fasst nur an, was noch keinen Verweis hat.</para>
        /// </summary>
        public static string SqlNachtrag()
        {
            return "UPDATE \"" + TABELLE + "\" " +
                   "SET \"" + SPALTE + "\" = (SELECT s.\"ID\" FROM \"" + TABELLE_STAMM + "\" s " +
                   "WHERE s.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") = 1";
        }

        /// <summary>
        /// Derselbe Nachtrag für die Gebäude EINES Projekts (Parameter: <c>ID_Projekt</c>) —
        /// der Weg des Projekttransfers: Ein Katalogverweis reist nicht über Paketgrenzen
        /// (die Id eines fremden Katalogs sagt am Ziel nichts), das importierte Projekt
        /// findet seinen Katalogsatz am Ziel nach derselben Regel wie der Schritt.
        /// </summary>
        public static string SqlNachtragProjekt()
        {
            return "UPDATE \"" + TABELLE + "\" " +
                   "SET \"" + SPALTE + "\" = (SELECT s.\"ID\" FROM \"" + TABELLE_STAMM + "\" s " +
                   "WHERE s.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") " +
                   "WHERE \"ID_Projekt\" = ? AND \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") = 1";
        }

        // =================================================================
        //  Die Auskunft
        // =================================================================

        /// <summary>Wie viele Projektkopien bekommt <see cref="SqlNachtrag"/> noch? 0 = nichts zu tun.</summary>
        public static string Zaehlung()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") = 1";
        }

        /// <summary>
        /// Wie viele Projektkopien bleiben OHNE Verweis — kein oder ein mehrdeutiger
        /// Katalogsatz? Die Zahl gehört in den Bericht: Genau diese Kopien sperren weiter
        /// über den Namen.
        /// </summary>
        public static string ZaehlungOhneVerweis()
        {
            return "SELECT COUNT(*) FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE + "\" IS NULL " +
                   "AND (SELECT COUNT(*) FROM \"" + TABELLE_STAMM + "\" s2 " +
                   "WHERE s2.\"Bezeichner\" = \"" + TABELLE + "\".\"Gebaeudename\") <> 1";
        }

        /// <summary>
        /// Die Projektkopien ohne Verweis, je Zeile „Id | Projekt | Name" — das Protokoll
        /// des Schritts nennt sie, damit ein Name ohne Katalogsatz auffindbar bleibt.
        /// </summary>
        public static string ProtokollOhneVerweis()
        {
            return "SELECT \"ID\", \"ID_Projekt\", \"Gebaeudename\" FROM \"" + TABELLE + "\" " +
                   "WHERE \"" + SPALTE + "\" IS NULL ORDER BY \"ID\"";
        }

        /// <summary>Gibt es die Spalte schon?</summary>
        public static bool SpalteVorhanden()
        {
            return DataRepository.SpalteVorhanden(TABELLE, SPALTE);
        }

        /// <summary>
        /// Ist noch etwas nachzutragen? Ohne die Spalte ist die Antwort „ja" — dann fehlt
        /// alles. Eine nicht lesbare Zählung gilt wie überall als „nichts zu tun".
        /// </summary>
        public static bool NachtragNoetig()
        {
            if (!SpalteVorhanden()) return true;
            return Zahl(Zaehlung()) > 0;
        }

        // =================================================================
        //  Der ganze Schritt - fuer Werkzeug und Nachzieh-Liste der Tests
        // =================================================================

        /// <summary>Was ein Lauf von <see cref="Ausfuehren"/> getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Spalte ist in diesem Lauf entstanden.</summary>
            public bool SpalteAngelegt { get; internal set; }

            /// <summary>Projektkopien, die in diesem Lauf ihren Verweis bekamen.</summary>
            public long Nachgetragen { get; internal set; }

            /// <summary>Projektkopien, die danach ohne Verweis stehen (kein oder mehrdeutiger Katalogsatz).</summary>
            public long OhneVerweis { get; internal set; }

            /// <summary>Katalogsätze, deren „Sonstige Fläche" ohne U-Wert auf 0 gesetzt wurde.</summary>
            public long Repariert { get; internal set; }

            /// <summary>Die Namen der reparierten Katalogsätze, in Id-Reihenfolge.</summary>
            public IReadOnlyList<string> RepariertNamen { get; internal set; } = Array.Empty<string>();

            /// <summary>Eine Zeile für Protokoll und Konsole.</summary>
            public string Text()
            {
                return "Spalte " + (SpalteAngelegt ? "angelegt" : "vorhanden") +
                       ", nachgetragen " + Nachgetragen.ToString(CultureInfo.InvariantCulture) +
                       ", ohne Verweis " + OhneVerweis.ToString(CultureInfo.InvariantCulture) +
                       ", Sonstige Flaeche ohne U-Wert repariert " + Repariert.ToString(CultureInfo.InvariantCulture) +
                       (RepariertNamen.Count > 0 ? " (" + string.Join(", ", RepariertNamen) + ")" : "");
            }
        }

        /// <summary>
        /// Der ganze Schritt 121 in fester Reihenfolge — Spalte, Index, Nachtrag, dann die
        /// Reparatur der Katalogsätze (<see cref="GebaeudeSonstigeFlaeche"/>). Jeder
        /// Handgriff für sich wiederholbar; ein zweiter Lauf tut nichts mehr.
        ///
        /// <para>Die Migration der Schale ruft die Anweisungen einzeln (sie protokolliert
        /// jeden Handgriff); dieser Weg dient dem Werkzeug <c>Testdatenbankschema</c> und
        /// der Nachzieh-Liste der Tests. Die Texte sind dieselben.</para>
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var bericht = new Bericht();
            if (!SpalteVorhanden())
            {
                DataRepository.ExecuteNonQuery(SQL_SPALTE);
                bericht.SpalteAngelegt = true;
            }
            DataRepository.ExecuteNonQuery(SQL_INDEX);

            long offen = Zahl(Zaehlung());
            if (offen > 0) DataRepository.ExecuteNonQuery(SqlNachtrag());
            bericht.Nachgetragen = offen - Zahl(Zaehlung());
            bericht.OhneVerweis = Zahl(ZaehlungOhneVerweis());

            bericht.RepariertNamen = GebaeudeSonstigeFlaeche.Betroffene();
            long schaden = Zahl(GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG);
            if (schaden > 0) DataRepository.ExecuteNonQuery(GebaeudeSonstigeFlaeche.SQL_REPARATUR);
            bericht.Repariert = schaden - Zahl(GebaeudeSonstigeFlaeche.SQL_ZAEHLUNG);
            return bericht;
        }

        private static long Zahl(string sql)
        {
            object wert = DataRepository.ExecuteScalar(sql);
            if (wert == null || wert == DBNull.Value) return 0;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }

    // ====================================================================================
    // DIE "SONSTIGE FLAECHE" OHNE U-WERT - Reparatur im Schritt 121 (Welle #468)
    //
    // SCHADENSBILD. Katalogsaetze, die eine Sonstige Flaeche > 0 fuehren, aber als U-Wert
    // "Sonstiges" 0 (oder leer). Ein U-Wert 0 ist physikalisch kein Bauteil; der Editor
    // (GebaeudeArbeitsstand.Pruefen) und das Stundenmodell (ErsatzparameterRC.UWert) lehnen
    // ihn fuer ein Bauteil mit Flaeche beide ab - dieselbe Grenze 0,1 ... 6,0 W/(m2K)
    // (GebaeudeFestwerte, Konzept Gebaeudesimulation 4.8). Die Grenze ist nicht zu eng:
    // Kein ausgefuehrtes Bauteil liegt unter 0,1 W/(m2K). Falsch sind die Daten.
    //
    // DIE REPARATUR SETZT DIE FLAECHE AUF 0, NICHT DEN U-WERT HERAUF. Mit U = 0 hat die
    // Flaeche nie Waerme gefuehrt: Der Tagesbilanz-Weg rechnet Ks * As (SpezWaermeverlusteC)
    // und damit 0, das Stundenmodell rechnete gar nicht. Flaeche 0 haelt H_T exakt, wie er
    // war; ein erfundener U-Wert aenderte ihn. Beruehrt ist allein der KATALOG - eine
    // Projektkopie ist Rechengrundlage ihres Projekts und bleibt, wie sie ist.
    //
    // NACH SCHADENSBILD, NICHT NACH ID-LISTE, und ohne Ruecksicht auf ReadOnly: Jeder
    // Katalogsatz mit diesem Bild wird gleich behandelt, auch ein eigener Satz des Anwenders
    // und ein Auslieferungssatz. Kulturfrei - reines SQL mit Zahlliteralen.
    //
    // EINFRIERREGEL. In der Testdatenbank tragen vier Katalogsaetze das Bild
    // (AltenH-95-EnEV2016, Pflegeheim-122-EnEV2016, SpH-Umkl-287-EnEV2016, SpH-Umkl-NE);
    // keines ist einem Projekt zugeordnet, der Referenzlauf bleibt byte-gleich.
    // ====================================================================================

    /// <summary>
    /// Die Reparatur der „Sonstigen Fläche" ohne U-Wert im Gebäudekatalog (Schritt 121) —
    /// EINE Quelle für Migration, Werkzeug und Nachweis.
    /// </summary>
    public static class GebaeudeSonstigeFlaeche
    {
        /// <summary>Wie viele Katalogsätze tragen das Schadensbild? 0 = nichts zu tun.</summary>
        public const string SQL_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"Tab_Gebaeude_STAMM\" " +
            "WHERE \"Sonstige_Flaechen\" > 0 AND COALESCE(\"k_Wert_Sonstiges\", 0) = 0";

        /// <summary>Die Namen der betroffenen Katalogsätze — für das Protokoll.</summary>
        public const string SQL_BETROFFENE =
            "SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" " +
            "WHERE \"Sonstige_Flaechen\" > 0 AND COALESCE(\"k_Wert_Sonstiges\", 0) = 0 ORDER BY \"ID\"";

        /// <summary>
        /// Die Reparatur: Die Fläche ohne U-Wert wird 0. <c>H_T</c> bleibt, wie er war —
        /// <c>U · A</c> war vorher 0 und ist es danach.
        /// </summary>
        public const string SQL_REPARATUR =
            "UPDATE \"Tab_Gebaeude_STAMM\" SET \"Sonstige_Flaechen\" = 0 " +
            "WHERE \"Sonstige_Flaechen\" > 0 AND COALESCE(\"k_Wert_Sonstiges\", 0) = 0";

        /// <summary>Die Namen der Katalogsätze, die die Reparatur treffen wird.</summary>
        public static IReadOnlyList<string> Betroffene()
        {
            var namen = new List<string>();
            DataTable dt = DataRepository.GetDataTable(SQL_BETROFFENE);
            if (dt == null) return namen;
            foreach (DataRow r in dt.Rows)
                if (r[0] != DBNull.Value) namen.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture));
            return namen;
        }
    }
}
