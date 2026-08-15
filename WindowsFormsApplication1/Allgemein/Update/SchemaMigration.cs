using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Versionierte In-Code-Migration der Access-Datenbank nach ADR-001.
    ///
    /// Ablauf (einmalig beim Programmstart, aus <c>Program.Main</c> vor dem MDI-Fenster):
    ///   1. Bootstrap: <c>Tab_Applikation.SchemaVersion</c> anlegen und die Einzelzeile
    ///      der Statustabelle sicherstellen.
    ///   2. Alle registrierten Schritte mit Nummer &gt; gespeicherter Version in
    ///      Reihenfolge ausführen.
    ///   3. Den Marker NACH jedem nachgewiesen erfolgreichen Schritt anheben.
    ///   4. Beim ersten Fehlschlag anhalten - der Marker bleibt stehen, damit ein halb
    ///      migriertes Schema nie als fertig gilt.
    ///
    /// Fehler werden gesammelt und EINMAL gemeldet. <see cref="MigrationOk"/> und
    /// <see cref="Fehlerbericht"/> tragen das Ergebnis; der Simulationsbereich fragt sie
    /// über <see cref="SimulationGesperrt"/> ab.
    ///
    /// Bewusst NICHT über <see cref="DataRepository"/>: dessen Methoden zeigen bei
    /// Fehlern MessageBoxen und schlucken den Fehlertext, womit sich "Spalte existiert
    /// schon" nicht von "Datei schreibgeschützt" unterscheiden ließe. Der Verbindungs-
    /// string kommt trotzdem von dort, also läuft alles über
    /// <see cref="DataRepository.GetDBPath"/> - der offene Punkt O6 des Konzepts ist
    /// damit gegenstandslos.
    ///
    /// ETAPPE 1 deckt die Schritte 1-4 ab (Schema), ETAPPE 2 den Schritt 5 - die
    /// einmalige Projektdatenmigration nach Konzept 5.5. Schritt 6 kommt mit Paket 4
    /// (Etappe 4a) hinzu und legt das Feature-Flag der zweikanaligen Kaskade an,
    /// Schritt 7 mit Paket 8 und belegt die Einstellung Extrapolation_erlaubt vor
    /// (Konzept 13.4).
    /// </summary>
    public static class SchemaMigration
    {
        /// <summary>Schemastand, den ein vollständiger Lauf dieser Programmfassung erreicht.</summary>
        public const int ZIEL_VERSION = 7;

        /// <summary>
        /// Nummer der einmaligen Projektdatenmigration Quellen/Senken (Konzept 5.5).
        /// Sie ist seit ETAPPE 2 in <see cref="SCHRITTE"/> registriert und hebt den
        /// Marker auf 5. Eine bereits auf 4 stehende Datenbank läuft dadurch sauber in
        /// die Datenmigration hinein, ohne die Schemaschritte zu wiederholen.
        /// </summary>
        public const int SCHRITT_5_DATENMIGRATION = 5;

        /// <summary>
        /// Nummer des Feature-Flags der zweikanaligen Kaskade (Paket 4, Etappe 4a).
        /// Rein additives DDL aus dem Spaltenkatalog - eine Datenbank auf Stand 5 läuft
        /// allein in diesen Schritt hinein, ohne die Schemaschritte oder die
        /// Datenmigration zu wiederholen.
        /// </summary>
        public const int SCHRITT_6_FEATUREFLAG = 6;

        /// <summary>
        /// Nummer der Vorbelegung von <c>Extrapolation_erlaubt</c> (Paket 8,
        /// Konzept 13.4). Die SPALTE entsteht bereits in Schritt 2; dieser Schritt setzt
        /// ihren WERT einmalig auf WAHR und ist damit das zweite DML des Vorhabens.
        /// </summary>
        public const int SCHRITT_7_EXTRAPOLATION = 7;

        /// <summary>Best-effort-Protokoll neben der Datenbank.</summary>
        public const string PROTOKOLL_DATEI = "migration_protokoll.txt";

        /// <summary>
        /// false, sobald ein Lauf einen Schritt nicht abschließen konnte. Vor dem ersten
        /// Lauf true - Werkzeuge, die die Migration gar nicht anstoßen (Referenzlauf-Suite),
        /// sollen dadurch nicht blockiert werden.
        /// </summary>
        public static bool MigrationOk { get; private set; }

        /// <summary>Vollständiger Bericht des letzten Laufs; erste Zeile ist der DB-Pfad.</summary>
        public static string Fehlerbericht { get; private set; }

        /// <summary>true, sobald <see cref="Ausfuehren"/> mindestens einmal gelaufen ist.</summary>
        public static bool Ausgefuehrt { get; private set; }

        /// <summary>Schemastand vor bzw. nach dem letzten Lauf.</summary>
        public static int StandVorher { get; private set; }
        public static int StandNachher { get; private set; }

        /// <summary>Zählwerk der ID_PUFFER-Bereinigung aus Schritt 4.</summary>
        public static int IdPufferGemappt { get; private set; }
        public static int IdPufferGenullt { get; private set; }

        // --- Zählwerk der Datenmigration aus Schritt 5 (Konzept 5.5) ------------------

        /// <summary>R1: Projekt-Puffer, die Verwendung und Betriebsparameter erhalten haben.</summary>
        public static int DatenPufferVerwendung { get; private set; }
        /// <summary>R1/R6: Anlagen, deren Wärmesenke auf einen Puffer gesetzt wurde.</summary>
        public static int DatenAnlagenPuffersenke { get; private set; }
        /// <summary>R5: Anlagen, die den Vorgabewert WS_Ziel = 'Heizkreis' erhalten haben.</summary>
        public static int DatenAnlagenHeizkreis { get; private set; }
        /// <summary>R3: aufgelöste Quell-Pufferreferenzen (WQ_Puffer -&gt; WQ_ID_Puffer).</summary>
        public static int DatenQuellPuffer { get; private set; }
        /// <summary>R4: nachgetragene Anlagenzeilen (ID_Type = 12).</summary>
        public static int DatenAnlagenzeilenNeu { get; private set; }
        /// <summary>
        /// R4: BESTEHENDE Puffer-Anlagenzeilen, deren leeres <c>ID_PUFFER</c> auf die
        /// Projektkopie nachgetragen wurde. Sie sind der Grund, aus dem der harte
        /// <c>(int)</c>-Cast in <c>FormMain.SetPufferSpControl</c> nicht mehr auf NULL
        /// läuft.
        /// </summary>
        public static int DatenAnlagenzeilenRepariert { get; private set; }
        /// <summary>R6: angelegte Puffer "BHKW-Pendelspeicher".</summary>
        public static int DatenPendelspeicherNeu { get; private set; }
        /// <summary>
        /// R6 (Etappe 4): davon mit Betriebstemperaturen aus den Systemvorgaben
        /// vorbelegt. Die Differenz zu <see cref="DatenPendelspeicherNeu"/> sind die
        /// Projekte, in denen keine Wärmeerzeuger-Anlage ein Temperaturpaar trägt.
        /// </summary>
        public static int DatenPendelspeicherTemperaturen { get; private set; }
        /// <summary>Summe aller Protokollhinweise aus Schritt 5.</summary>
        public static int DatenHinweise { get; private set; }

        /// <summary>
        /// Schritt 7 (Paket 8): Einstellungssätze, die die Vorbelegung
        /// <c>Extrapolation_erlaubt = WAHR</c> erhalten haben.
        /// </summary>
        public static int DatenExtrapolationVorbelegt { get; private set; }

        static SchemaMigration()
        {
            MigrationOk = true;
            Fehlerbericht = "";
        }

        // =================================================================================
        // Schrittregister
        // =================================================================================

        private delegate bool SchrittAktion(Lauf l);

        private sealed class Schritt
        {
            public readonly int Nr;
            public readonly string Name;
            /// <summary>Verständlicher Klartext, wenn der Schritt scheitert.</summary>
            public readonly string Fehlertext;
            public readonly SchrittAktion Aktion;

            public Schritt(int nr, string name, string fehlertext, SchrittAktion aktion)
            {
                Nr = nr; Name = name; Fehlertext = fehlertext; Aktion = aktion;
            }
        }

        private static readonly Schritt[] SCHRITTE =
        {
            new Schritt(1, "Spalten in Tab_Energieanlagen (Konzept 5.3)",
                        "Die Spalten für Wärmequelle und Wärmesenke konnten nicht angelegt werden.",
                        Schritt_1_SpaltenAnlagen),

            new Schritt(2, "Spalten in Tab_Pufferspeicher, Tab_Klimaregion und Tab_Einstellungen (Konzept 5.1/12)",
                        "Die Betriebsparameter-Spalten der Pufferspeicher konnten nicht angelegt werden.",
                        Schritt_2_SpaltenPuffer),

            new Schritt(3, "Ergebnistabelle Tab_ErgebnisPufferspeicher (Konzept 6.6)",
                        "Die Ergebnistabelle für Pufferspeicher konnte nicht angelegt werden.",
                        Schritt_3_ErgebnisTabelle),

            new Schritt(4, "Beziehungen der Pufferspeicher (Konzept 5.3 / B0-6b)",
                        "Die Beziehungen zwischen Anlagen, Pufferspeichern und Projekt konnten nicht angelegt werden.",
                        Schritt_4_Beziehungen),

            // ETAPPE 2 - das einzige einmalige DML des Vorhabens (Konzept 5.5).
            new Schritt(SCHRITT_5_DATENMIGRATION,
                        "Datenmigration Quellen/Senken (Konzept 5.5)",
                        "Die Projektdaten konnten nicht auf das neue Senkenmodell umgestellt werden.",
                        Schritt_5_ProjektdatenQuellenSenken),

            // PAKET 4, ETAPPE 4a - Feature-Flag der zweikanaligen Kaskade (Kapitel 9).
            new Schritt(SCHRITT_6_FEATUREFLAG,
                        "Feature-Flag Kaskade_Zweikanalig in Tab_Einstellungen (Konzept Kapitel 9)",
                        "Die Projekteinstellung für die zweikanalige Kaskade konnte nicht angelegt werden.",
                        Schritt_6_FeatureFlag),

            // PAKET 8 - Vorbelegung der Einstellung Extrapolation_erlaubt (Konzept 13.4).
            new Schritt(SCHRITT_7_EXTRAPOLATION,
                        "Vorbelegung Extrapolation_erlaubt in Tab_Einstellungen (Konzept 13.4)",
                        "Die Projekteinstellung für die Kennlinien-Extrapolation konnte nicht vorbelegt werden.",
                        Schritt_7_ExtrapolationVorbelegung),
        };

        // =================================================================================
        // Einstiegspunkt
        // =================================================================================

        /// <summary>
        /// Führt alle noch ausstehenden Migrationsschritte aus.
        /// Rückgabe true, wenn die Datenbank danach auf <see cref="ZIEL_VERSION"/> steht.
        /// </summary>
        /// <param name="fehlerbericht">
        /// Immer gefüllt. Erste Zeile ist der tatsächlich verwendete Datenbankpfad,
        /// danach folgt je Schritt eine Statuszeile.
        /// </param>
        public static bool Ausfuehren(out string fehlerbericht)
        {
            Ausgefuehrt = true;
            IdPufferGemappt = 0;
            IdPufferGenullt = 0;
            DatenPufferVerwendung = 0;
            DatenAnlagenPuffersenke = 0;
            DatenAnlagenHeizkreis = 0;
            DatenQuellPuffer = 0;
            DatenAnlagenzeilenNeu = 0;
            DatenAnlagenzeilenRepariert = 0;
            DatenPendelspeicherNeu = 0;
            DatenPendelspeicherTemperaturen = 0;
            DatenHinweise = 0;
            DatenExtrapolationVorbelegt = 0;

            var l = new Lauf();
            string dbPfad;
            try { dbPfad = DataRepository.GetDBPath(); }
            catch (Exception ex) { dbPfad = "(Pfad nicht ermittelbar: " + ex.Message + ")"; }

            l.DbPfad = dbPfad;
            l.Kopf(dbPfad);
            l.Kopf("Zeitpunkt: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture));

            bool erfolg = false;
            try
            {
                erfolg = Durchfuehren(l, dbPfad);
            }
            catch (Exception ex)
            {
                l.Zeile("ABBRUCH: unerwarteter Fehler - " + ex.Message);
                erfolg = false;
            }

            MigrationOk = erfolg;
            Fehlerbericht = l.Text();
            fehlerbericht = Fehlerbericht;

            ProtokollSchreiben(dbPfad, Fehlerbericht);
            return erfolg;
        }

        private static bool Durchfuehren(Lauf l, string dbPfad)
        {
            // --- Datei überhaupt vorhanden? ------------------------------------------
            bool dateiDa;
            try { dateiDa = File.Exists(dbPfad); } catch { dateiDa = false; }
            if (!dateiDa)
            {
                l.Zeile("Die Datenbankdatei wurde nicht gefunden. Bitte den Datenbankpfad in den " +
                        "Einstellungen prüfen oder die Datei wiederherstellen.");
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }

            // --- Verbindung ------------------------------------------------------------
            try
            {
                using (var conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    l.Conn = conn;
                    return SchritteAbarbeiten(l);
                }
            }
            catch (Exception ex)
            {
                l.Zeile("Die Datenbank konnte nicht geöffnet werden: " + Kurzmeldung(ex));
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }
            finally { l.Conn = null; }
        }

        private static bool SchritteAbarbeiten(Lauf l)
        {
            // --- Bootstrap: Versionsmarker --------------------------------------------
            if (!Bootstrap(l))
            {
                l.Zeile("Der Schemamarker Tab_Applikation.SchemaVersion konnte nicht angelegt werden. " +
                        "Die Datenbank ist vermutlich schreibgeschützt oder von einem anderen " +
                        "Programm exklusiv geöffnet. Der dritte mögliche Grund: die Statustabelle " +
                        "Tab_Applikation ist leer und eines ihrer Pflichtfelder (ID, Projektname) " +
                        "ließ sich nicht belegen - dann nennt die Meldung der Datenbank das Feld.");
                if (l.LetzterFehler != null) l.Zeile("Meldung der Datenbank: " + l.LetzterFehler);
                StandVorher = 0;
                StandNachher = 0;
                return false;
            }

            l.Zeile("Bootstrap Schemamarker Tab_Applikation.SchemaVersion: OK");
            l.Detail();

            int version = ApplikationCtrl.GetSchemaVersion();
            StandVorher = version;
            StandNachher = version;
            l.Kopf("Schemastand vorher: " + version + "   (Zielstand " + ZIEL_VERSION + ")");
            l.Leerzeile();

            bool alleOk = true;

            foreach (Schritt s in SCHRITTE)
            {
                if (s.Nr <= version)
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": bereits erledigt");
                    continue;
                }

                l.LetzterFehler = null;
                bool ok;
                try { ok = s.Aktion(l); }
                catch (Exception ex)
                {
                    l.LetzterFehler = Kurzmeldung(ex);
                    ok = false;
                }

                if (!ok)
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": FEHLGESCHLAGEN");
                    l.Zeile("        " + s.Fehlertext);
                    if (l.LetzterFehler != null) l.Zeile("        Meldung der Datenbank: " + l.LetzterFehler);
                    l.Detail();
                    alleOk = false;
                    break; // beim ersten Fehler anhalten - kein halb migriertes Schema fortschreiben
                }

                // Marker erst NACH nachgewiesenem Erfolg anheben.
                if (!ApplikationCtrl.SetSchemaVersion(s.Nr))
                {
                    l.Zeile("Schritt " + s.Nr + "  " + s.Name +
                            ": ausgeführt, aber der Schemamarker konnte nicht fortgeschrieben werden.");
                    l.Detail();
                    alleOk = false;
                    break;
                }

                version = s.Nr;
                StandNachher = version;
                l.Zeile("Schritt " + s.Nr + "  " + s.Name + ": OK");
                l.Detail();
            }

            l.Leerzeile();
            l.Zeile("Schemastand nachher: " + StandNachher + "   (Zielstand " + ZIEL_VERSION + ")");
            if (IdPufferGemappt > 0 || IdPufferGenullt > 0)
                l.Zeile("ID_PUFFER-Bereinigung: " + IdPufferGemappt + " auf die Projektkopie umgesetzt, " +
                        IdPufferGenullt + " geleert.");

            if (DatenPufferVerwendung + DatenAnlagenPuffersenke + DatenAnlagenHeizkreis +
                DatenQuellPuffer + DatenAnlagenzeilenNeu + DatenPendelspeicherNeu > 0)
                l.Zeile("Datenmigration 5.5: " + DatenPufferVerwendung + " Puffer mit Verwendung, " +
                        DatenAnlagenPuffersenke + " Anlagen auf Puffer, " +
                        DatenAnlagenHeizkreis + " Anlagen auf Heizkreis, " +
                        DatenQuellPuffer + " Quell-Puffer aufgelöst, " +
                        DatenAnlagenzeilenNeu + " Anlagenzeilen nachgetragen, " +
                        DatenAnlagenzeilenRepariert + " Anlagenzeilen mit ID_PUFFER repariert, " +
                        DatenPendelspeicherNeu + " Pendelspeicher angelegt, " +
                        DatenHinweise + " Hinweise.");

            if (DatenExtrapolationVorbelegt > 0)
                l.Zeile("Vorbelegung 13.4: " + DatenExtrapolationVorbelegt +
                        " Einstellungssätze mit Extrapolation_erlaubt = WAHR.");

            return alleOk && StandNachher >= ZIEL_VERSION;
        }

        /// <summary>
        /// Legt den Versionsmarker an (ADR-001, Aufgabe 2) und stellt sicher, dass die
        /// Einzelzeilen-Statustabelle <c>Tab_Applikation</c> genau eine Zeile hat.
        /// </summary>
        private static bool Bootstrap(Lauf l)
        {
            DataTable schema = TabellenSchema(l, SchemaKatalog.TAB_APPLIKATION);
            if (schema == null) return false;

            SchemaSpalte marker = SchemaKatalog.SchemaVersionSpalte;
            if (!schema.Columns.Contains(marker.Name))
            {
                if (!Ddl(l, "ALTER TABLE [" + marker.Tabelle + "] ADD COLUMN [" + marker.Name + "] " +
                            marker.TypDefinition,
                        marker.Tabelle + "." + marker.Name))
                    return false;
            }

            object anzahl = Scalar(l, "SELECT COUNT(*) FROM [" + SchemaKatalog.TAB_APPLIKATION + "]");
            if (anzahl != null && Convert.ToInt32(anzahl, CultureInfo.InvariantCulture) == 0)
            {
                if (!StatuszeileAnlegen(l, marker)) return false;
            }

            // Leere Marker auf 0 ziehen, damit GetSchemaVersion nicht auf NULL läuft.
            NonQuery(l, "UPDATE [" + SchemaKatalog.TAB_APPLIKATION + "] SET [" + marker.Name +
                        "] = 0 WHERE [" + marker.Name + "] IS NULL");
            return true;
        }

        /// <summary>Pflichtfeld der Statustabelle, das ohne Wert kein INSERT zulässt.</summary>
        private const string SPALTE_PROJEKTNAME = "Projektname";

        /// <summary>
        /// Legt die fehlende Einzelzeile in <c>Tab_Applikation</c> an.
        ///
        /// Zwei Eigenheiten der Tabelle machen das nötig - an der Datenbank verifiziert
        /// (Arbeitskopie, 14.08.2026):
        ///
        ///   - <c>ID</c> ist KEIN AutoWert und nicht NULL-fähig. Ein INSERT ohne ID
        ///     scheitert also immer; der frühere Rückfallweg "einmal mit ID = 1, sonst
        ///     ganz ohne ID" konnte auf einer leeren Tabelle gar nicht gelingen.
        ///   - <c>Projektname</c> ist ein PFLICHTFELD ohne Spalten-Default. Ein INSERT
        ///     ohne diese Spalte endet mit "Sie müssen einen Wert in das Feld … eingeben"
        ///     - und damit scheiterte die gesamte Migration schon am Bootstrap, sobald
        ///     die Statustabelle einmal leer war.
        ///
        /// Die ID wird deshalb nach dem <c>GetMaxID + 1</c>-Muster selbst vergeben.
        /// <c>MAX(ID)</c> liefert auf der leeren Tabelle NULL; <see cref="Zahl"/> macht
        /// daraus 0 und damit die 1 - das ist der Nz-sichere Weg, ohne die
        /// Access-Funktion <c>Nz</c> zu brauchen (die kennt der OLE-DB-Provider
        /// außerhalb von Access nicht).
        ///
        /// Zwei Rückfallwege bleiben stehen, damit fremde Schemastände nicht hängen:
        /// ohne <c>Projektname</c> (falls die Spalte dort nicht existiert) und ganz ohne
        /// ID (falls sie doch ein AutoWert ist). Gemeldet wird am Ende die Meldung des
        /// ERSTEN Versuchs - sie benennt den eigentlichen Grund, während die
        /// Rückfallwege auf diesem Schema zwangsläufig an der fehlenden ID scheitern.
        /// </summary>
        private static bool StatuszeileAnlegen(Lauf l, SchemaSpalte marker)
        {
            string tab = SchemaKatalog.TAB_APPLIKATION;
            string id = (Zahl(Scalar(l, "SELECT MAX(ID) FROM [" + tab + "]")) + 1)
                        .ToString(CultureInfo.InvariantCulture);

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] (ID, [" + SPALTE_PROJEKTNAME + "], [" + marker.Name + "]) " +
                       "VALUES (" + id + ", '', 0)",
                    "Statuszeile in Tab_Applikation", true))
                return true;

            string ersterFehler = l.LetzterFehler;

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] (ID, [" + marker.Name + "]) VALUES (" + id + ", 0)",
                    "Statuszeile in Tab_Applikation", true))
                return true;

            l.LetzterFehler = null;
            if (Ddl(l, "INSERT INTO [" + tab + "] ([" + marker.Name + "]) VALUES (0)",
                    "Statuszeile in Tab_Applikation"))
                return true;

            if (!string.IsNullOrEmpty(l.LetzterFehler))
                l.Notiz("Statuszeile, letzter Rückfallweg (ohne ID): " + l.LetzterFehler);

            // Die aussagekräftige Meldung wieder einsetzen, statt sie zu verlieren.
            if (!string.IsNullOrEmpty(ersterFehler)) l.LetzterFehler = ersterFehler;
            return false;
        }

        // =================================================================================
        // Schritt 1 und 2 - additives DDL aus dem gemeinsamen Spaltenkatalog
        // =================================================================================

        private static bool Schritt_1_SpaltenAnlagen(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt1_Energieanlagen);
        }

        private static bool Schritt_2_SpaltenPuffer(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt2_Speicher);
        }

        /// <summary>
        /// Schritt 6 (Paket 4, Etappe 4a): die eine Spalte des Feature-Flags. Bewusst
        /// derselbe additive Weg wie Schritt 1 und 2 - eigener Schritt nur deshalb, weil
        /// eine bereits auf Stand 5 stehende Datenbank die Schritte 1-5 nicht wiederholen
        /// darf (Schritt 5 ist das einzige DML des Vorhabens).
        /// </summary>
        private static bool Schritt_6_FeatureFlag(Lauf l)
        {
            return SpaltenAnlegen(l, SchemaKatalog.Schritt6_FeatureFlag);
        }

        /// <summary>
        /// Schritt 7 (Paket 8, Konzept 13.4): Vorbelegung der Projekteinstellung
        /// <c>Extrapolation_erlaubt</c> auf WAHR.
        ///
        /// ZWEI TEILE, in dieser Reihenfolge:
        ///
        ///   1. <b>DDL, idempotent</b> — dieselbe Spaltenanlage wie in Schritt 2 aus dem
        ///      gemeinsamen Katalog. Auf jeder gepflegten Datenbank ein No-op
        ///      („bereits vorhanden"); sie steht hier nur, damit ein Zwischenstand nicht
        ///      am UPDATE scheitert.
        ///   2. <b>DML, einmalig</b> — <c>UPDATE … SET Extrapolation_erlaubt = TRUE</c>
        ///      über ALLE Zeilen.
        ///
        /// WARUM DAS UPDATE. <c>ALTER TABLE … ADD COLUMN … YESNO</c> belegt bestehende
        /// Zeilen in Access mit <c>False</c>; ein Ja/Nein-Feld kennt kein NULL. Ohne
        /// diesen Schritt stünde jedes Altprojekt auf „Extrapolation verboten" — und
        /// damit auf einem ANDEREN Verhalten als bisher: Bis Paket 8 fragte die Engine
        /// bei Unterschreitung der Kennlinien-Untergrenze nach, und in jedem
        /// dokumentierten Lauf (Referenzlauf-Suite, fünf von neun Projekten) lautete die
        /// Antwort „Ja". WAHR ist damit die einzige ergebnisneutrale Vorbelegung.
        ///
        /// EINMALIGKEIT. Der Schritt läuft genau einmal je Datenbank (Marker 6 → 7); ein
        /// später vom Anwender gesetztes „nein" wird dadurch nicht wieder überschrieben.
        /// Neu angelegte Einstellungssätze belegt <c>KonfigurationCtrl</c> selbst vor
        /// (dort <c>ExtrapolationVorbelegen</c>) — der Weg über die Migration steht
        /// ausschließlich für den Bestand.
        /// </summary>
        private static bool Schritt_7_ExtrapolationVorbelegung(Lauf l)
        {
            if (!SpaltenAnlegen(l, SchemaKatalog.Schritt7_Extrapolation)) return false;

            int betroffen = NonQuery(l,
                "UPDATE [" + SchemaKatalog.TAB_EINSTELLUNGEN + "] SET [" +
                SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] = TRUE");

            if (betroffen < 0)
            {
                l.Notiz("Vorbelegung Extrapolation_erlaubt: UPDATE fehlgeschlagen");
                return false;
            }

            DatenExtrapolationVorbelegt = betroffen;
            l.Notiz("Extrapolation_erlaubt: " + betroffen + " Einstellungssätze auf WAHR vorbelegt " +
                    "(entspricht der bisherigen Antwort auf die Extrapolationsrückfrage)");
            return true;
        }

        /// <summary>
        /// Legt die fehlenden Spalten einer Katalogauswahl an. Idempotent: was schon da
        /// ist, wird übersprungen; meldet "existiert bereits" als Erfolg.
        /// </summary>
        private static bool SpaltenAnlegen(Lauf l, IEnumerable<SchemaSpalte> spalten)
        {
            bool ok = true;

            foreach (var gruppe in spalten.GroupBy(s => s.Tabelle, StringComparer.OrdinalIgnoreCase))
            {
                DataTable schema = TabellenSchema(l, gruppe.Key);
                if (schema == null)
                {
                    l.Notiz(gruppe.Key + ": Tabelle nicht lesbar");
                    ok = false;
                    continue;
                }

                int neu = 0, vorhanden = 0;
                foreach (SchemaSpalte s in gruppe)
                {
                    if (schema.Columns.Contains(s.Name)) { vorhanden++; continue; }

                    if (Ddl(l, "ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " + s.TypDefinition,
                            s.Tabelle + "." + s.Name, true))
                        neu++;
                    else
                        ok = false;
                }

                l.Notiz(gruppe.Key + ": " + neu + " Spalten angelegt, " + vorhanden + " bereits vorhanden");
            }

            return ok;
        }

        // =================================================================================
        // Schritt 3 - Ergebnistabelle (Konzept 6.6)
        // =================================================================================

        private const string SQL_CREATE_ERGEBNISPUFFER =
            "CREATE TABLE Tab_ErgebnisPufferspeicher (ID LONG NOT NULL PRIMARY KEY, " +
            "ID_Ergebnis LONG, ID_Pufferspeicher LONG, Bezeichner TEXT(255), " +
            "Verwendung TEXT(50), Q_max DOUBLE, Ladung_gesamt DOUBLE, Entladung_gesamt DOUBLE, " +
            "Verluste_gesamt DOUBLE, SOC_Ende DOUBLE, SOC_Mittel DOUBLE, SOC_Max DOUBLE, " +
            "Vollzyklen DOUBLE)";

        private static bool Schritt_3_ErgebnisTabelle(Lauf l)
        {
            bool ok = Ddl(l, SQL_CREATE_ERGEBNISPUFFER, "Tabelle Tab_ErgebnisPufferspeicher");

            ok &= Ddl(l, "CREATE INDEX idx_ErgPuffer ON Tab_ErgebnisPufferspeicher (ID_Ergebnis)",
                      "Index idx_ErgPuffer");

            // Dieselbe Löschweitergabe wie bei allen Geschwistertabellen (13.7): das
            // DELETE FROM Tab_Ergebnis in ErgebnisCtrl.Save räumt den Vorgängerlauf damit
            // mit ab. Ohne diese Beziehung entstünden Waisenzeilen, die wegen der
            // MAX(ID)+1-Vergabe später auf fremde Läufe zeigen würden.
            ok &= Ddl(l, "ALTER TABLE Tab_ErgebnisPufferspeicher ADD CONSTRAINT FK_ErgPuffer " +
                         "FOREIGN KEY (ID_Ergebnis) REFERENCES Tab_Ergebnis (ID) ON DELETE CASCADE",
                      "Beziehung FK_ErgPuffer (mit Löschweitergabe)");

            return ok;
        }

        // =================================================================================
        // Schritt 4 - Beziehungen
        // =================================================================================

        /// <summary>
        /// Legt die vier fehlenden Beziehungen rund um <c>Tab_Pufferspeicher</c> an.
        ///
        /// BEWUSSTE ABWEICHUNG VOM KONZEPT-WORTLAUT (5.3):
        /// Die drei Anlagen-Beziehungen (WS_ID_Puffer, WS_ID_Puffer2, WQ_ID_Puffer) und
        /// die Nachrüstung von ID_PUFFER werden RESTRIKTIV angelegt, also OHNE
        /// Löschweitergabe. Konzept 5.3 nennt als Vorbild
        /// <c>Z_ProjektPufferSp.ID_Pufferspeicher</c> mit DEL-CASCADE - dort sind die
        /// Kinder aber reine Zuordnungszeilen, deren Verschwinden folgenlos ist. Hier
        /// stehen ERZEUGER-Anlagen auf der Kindseite: eine Löschweitergabe würde beim
        /// Entfernen eines Pufferspeichers stillschweigend die referenzierende Wärmepumpe
        /// (oder BHKW/Kessel) mitlöschen. Das ist Datenverlust ohne Rückfrage und wäre
        /// aus der Oberfläche nicht nachvollziehbar.
        ///
        /// Damit die restriktiven Beziehungen die bestehende Aufräumlogik nicht blockieren,
        /// setzen <c>PufferSpCtrl.ProjektWaisenEntfernen</c> und
        /// <c>PufferSpCtrl.DeleteFromProjekt</c> die referenzierenden Spalten der
        /// betroffenen Puffer-IDs vor dem DELETE auf NULL.
        ///
        /// Ausnahme ist B0-6b: <c>Tab_Projekt.ID -&gt; Tab_Pufferspeicher.ID_Projekt</c>
        /// bekommt sehr wohl eine Löschweitergabe - dort ist die Puffer-Projektkopie das
        /// Kind, und mit dem Projekt soll sie verschwinden.
        /// </summary>
        private static bool Schritt_4_Beziehungen(Lauf l)
        {
            bool ok = true;

            // --- 4a) 0-Werte in den neuen FK-Spalten sind keine gültigen IDs ----------
            foreach (string spalte in new[] { "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer" })
            {
                int n = NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL WHERE [" + spalte + "] = 0");
                if (n > 0) l.Notiz(spalte + ": " + n + " Nullwerte geleert");
            }

            // --- 4b) Altbestand in ID_PUFFER bereinigen ------------------------------
            if (!IdPufferBereinigen(l)) ok = false;

            // --- 4c) verwaiste Puffer-Projektkopien entfernen -------------------------
            // Steht VOR den vier ADD CONSTRAINT (Review-Nacharbeit). Zwei Gründe, und
            // beide sind zwingend:
            //   - Nach den Beziehungen wäre das DELETE der Waisen blockiert, sobald noch
            //     eine Anlage auf eine solche Zeile zeigt (restriktiv, kein CASCADE) -
            //     der Schritt scheiterte dann an genau dem Bestand, den er bereinigen
            //     soll.
            //   - Umgekehrt darf nach dem DELETE keine Anlage mehr auf eine entfernte
            //     Zeile zeigen, sonst kippt das ADD CONSTRAINT mit Jet-Fehler 3379.
            //     PufferWaisenEntfernen löst die Referenzen deshalb selbst.
            if (!PufferWaisenEntfernen(l)) ok = false;

            // --- 4d) die vier restriktiven Beziehungen auf Tab_Pufferspeicher.ID -----
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WS_Puffer", "WS_ID_Puffer"),
                      "Beziehung Tab_Energieanlagen.WS_ID_Puffer -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WS_Puffer2", "WS_ID_Puffer2"),
                      "Beziehung Tab_Energieanlagen.WS_ID_Puffer2 -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_WQ_Puffer", "WQ_ID_Puffer"),
                      "Beziehung Tab_Energieanlagen.WQ_ID_Puffer -> Tab_Pufferspeicher.ID (restriktiv)");
            ok &= Ddl(l, FkRestriktiv("FK_Energieanlagen_ID_Puffer", "ID_PUFFER"),
                      "Beziehung Tab_Energieanlagen.ID_PUFFER -> Tab_Pufferspeicher.ID (restriktiv)");

            // --- 4e) B0-6b: Projekt -> Pufferspeicher MIT Löschweitergabe ------------
            ok &= Ddl(l, "ALTER TABLE Tab_Pufferspeicher ADD CONSTRAINT FK_Pufferspeicher_Projekt " +
                         "FOREIGN KEY (ID_Projekt) REFERENCES Tab_Projekt (ID) ON DELETE CASCADE",
                      "Beziehung Tab_Projekt.ID -> Tab_Pufferspeicher.ID_Projekt (mit Löschweitergabe)");

            return ok;
        }

        private static string FkRestriktiv(string name, string spalte)
        {
            return "ALTER TABLE Tab_Energieanlagen ADD CONSTRAINT " + name +
                   " FOREIGN KEY ([" + spalte + "]) REFERENCES Tab_Pufferspeicher (ID)";
        }

        /// <summary>
        /// Bereinigt <c>Tab_Energieanlagen.ID_PUFFER</c>, bevor die Beziehung erzwungen wird.
        ///
        /// Regeln:
        ///   - 0 ist keine gültige ID -&gt; NULL.
        ///   - Wert zeigt auf eine Zeile in <c>Tab_Pufferspeicher</c> MIT demselben
        ///     <c>ID_Projekt</c> wie die Anlage -&gt; unverändert.
        ///   - sonst: identifiziert der Bezeichner der Anlage genau EINE Projektkopie des
        ///     Projekts, wird auf deren ID umgesetzt (das repariert die bekannten
        ///     STAMM-IDs aus Konzept 2.3, die <c>Form_PufferSp</c> schreibt).
        ///   - sonst: NULL.
        /// </summary>
        private static bool IdPufferBereinigen(Lauf l)
        {
            int n0 = NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = NULL WHERE ID_PUFFER = 0");
            if (n0 > 0) IdPufferGenullt += n0;

            DataTable offen = Abfrage(l,
                "SELECT ID, ID_Projekt, Bezeichner, ID_PUFFER FROM Tab_Energieanlagen " +
                "WHERE ID_PUFFER IS NOT NULL AND ID_PUFFER <> 0 " +
                "  AND ID_PUFFER NOT IN (SELECT ID FROM Tab_Pufferspeicher)");

            // Werte, die zwar auf eine existierende Puffer-Zeile zeigen, aber auf die
            // eines FREMDEN Projekts, sind ebenfalls falsch - sie kommen aus kopierten
            // Projekten. Sie verletzen die Beziehung zwar nicht, führen aber in Paket 2
            // zu fremden Speichern; deshalb hier mitbehandelt.
            DataTable fremd = Abfrage(l,
                "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_PUFFER FROM Tab_Energieanlagen AS a " +
                "INNER JOIN Tab_Pufferspeicher AS p ON a.ID_PUFFER = p.ID " +
                "WHERE a.ID_Projekt <> p.ID_Projekt OR p.ID_Projekt IS NULL");

            if (offen == null || fremd == null)
            {
                l.Notiz("ID_PUFFER: Altbestand nicht lesbar");
                return false;
            }

            var zuPruefen = new List<DataRow>();
            foreach (DataRow r in offen.Rows) zuPruefen.Add(r);
            foreach (DataRow r in fremd.Rows) zuPruefen.Add(r);

            foreach (DataRow r in zuPruefen)
            {
                int idAnlage = Zahl(r["ID"]);
                int idProjekt = Zahl(r["ID_Projekt"]);
                string bezeichner = r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString();

                int ziel = 0;
                if (idProjekt > 0 && bezeichner.Length > 0)
                {
                    DataTable treffer = Abfrage(l,
                        "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@bez", bezeichner));
                    if (treffer != null && treffer.Rows.Count == 1) ziel = Zahl(treffer.Rows[0][0]);
                }

                if (ziel > 0)
                {
                    if (NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = " +
                                    ziel.ToString(CultureInfo.InvariantCulture) +
                                    " WHERE ID = " + idAnlage.ToString(CultureInfo.InvariantCulture)) >= 0)
                        IdPufferGemappt++;
                }
                else
                {
                    if (NonQuery(l, "UPDATE Tab_Energieanlagen SET ID_PUFFER = NULL WHERE ID = " +
                                    idAnlage.ToString(CultureInfo.InvariantCulture)) >= 0)
                        IdPufferGenullt++;
                }
            }

            l.Notiz("ID_PUFFER: " + IdPufferGemappt + " auf die Projektkopie umgesetzt, " +
                    IdPufferGenullt + " geleert.");
            return true;
        }

        /// <summary>
        /// B0-6b, Vorarbeit: Zeilen in <c>Tab_Pufferspeicher</c>, deren <c>ID_Projekt</c>
        /// auf ein längst gelöschtes Projekt zeigt, verhindern das ADD CONSTRAINT.
        ///
        /// Läuft seit der Review-Nacharbeit VOR den vier restriktiven Beziehungen und
        /// löst zuvor die Anlagen-Referenzen auf genau diese Zeilen. Ohne das Lösen
        /// zeigten nach dem DELETE Anlagen ins Leere, und das anschließende
        /// ADD CONSTRAINT scheiterte mit Jet-Fehler 3379 ("Existing data violates
        /// referential integrity rules") - an einer Datenlage, die dieser Schritt
        /// gerade erst selbst erzeugt hätte.
        /// </summary>
        private static bool PufferWaisenEntfernen(Lauf l)
        {
            int leer = NonQuery(l, "UPDATE Tab_Pufferspeicher SET ID_Projekt = NULL WHERE ID_Projekt = 0");
            if (leer > 0) l.Notiz("Tab_Pufferspeicher: " + leer + " Zeilen mit ID_Projekt = 0 geleert");

            const string WAISEN_FILTER =
                "SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt IS NOT NULL " +
                "AND ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)";

            foreach (string spalte in new[] { "ID_PUFFER", "WS_ID_Puffer", "WS_ID_Puffer2", "WQ_ID_Puffer" })
            {
                int n = NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = NULL " +
                                    "WHERE [" + spalte + "] IN (" + WAISEN_FILTER + ")");
                if (n > 0) l.Notiz(spalte + ": " + n + " Verweise auf verwaiste Puffer-Zeilen geleert");
            }

            int weg = NonQuery(l,
                "DELETE FROM Tab_Pufferspeicher WHERE ID_Projekt IS NOT NULL " +
                "AND ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)");
            if (weg < 0) return false;
            l.Notiz("Tab_Pufferspeicher: " + weg + " verwaiste Projektkopien entfernt");
            return true;
        }

        // =================================================================================
        // Schritt 5 - einmalige Projektdatenmigration Quellen/Senken (Konzept 5.5)
        // =================================================================================

        // Werte des neuen Senkenmodells (Konzept 3.2/5.1/5.3). Alles, was auch die
        // Oberfläche braucht, steht seit Etappe 3 in ProjektPuffer - hier nur noch
        // die Aliase, damit der Migrationscode unverändert lesbar bleibt.
        private const string WS_ZIEL_HEIZKREIS = "Heizkreis";
        private const string WS_ZIEL_PUFFER_HEIZUNG = ProjektPuffer.WS_ZIEL_PUFFER_HEIZUNG;
        private const string VERWENDUNG_HEIZUNG = ProjektPuffer.VERWENDUNG_HEIZUNG;

        /// <summary>Literal der Alt-Zuordnung; SimulationControl vergleicht genau darauf.</summary>
        private const string ERZEUGER_WAERMEPUMPE = ProjektPuffer.ERZEUGER_WAERMEPUMPE;

        /// <summary>Bezeichner des aus Tab_Einstellungen.Pendelspeicher erzeugten Puffers.</summary>
        private const string BEZ_PENDELSPEICHER = ProjektPuffer.BEZ_PENDELSPEICHER;

        // ID_Type aus WizardItemClass - hier bewusst als lokale Konstanten, damit der
        // Migrationscode nicht von der UI-Schicht abhängt.
        private const int TYP_WP = 1;
        private const int TYP_SOLARTHERMIE = 2;
        private const int TYP_KESSEL = 10;
        private const int TYP_BHKW = ProjektPuffer.TYP_BHKW;
        private const int TYP_PUFFER = ProjektPuffer.TYP_PUFFER;

        /// <summary>
        /// Umrechnung des Alt-Parameters <c>Tab_Einstellungen.Pendelspeicher</c> (m³) in
        /// das Gesamtvolumen eines Puffers (Liter). Herleitung und Belege stehen bei
        /// <see cref="ProjektPuffer.M3_IN_LITER"/>.
        /// </summary>
        private const double PENDELSPEICHER_M3_IN_LITER = ProjektPuffer.M3_IN_LITER;

        /// <summary>
        /// Stellt die Projektdaten auf das Quellen-/Senkenmodell um - genau EINMAL je
        /// Datenbank, garantiert durch den Versionsmarker (Konzept 5.5). Es gibt bewusst
        /// keine Heuristik über den Datenbestand: eine solche würde bei jedem Start die
        /// Entscheidung des Anwenders (z. B. ein zurückgesetztes WS_Ziel = 'Heizkreis')
        /// wieder überschreiben.
        ///
        /// Die sechs Regeln der Migrationstabelle, je Projekt in dieser Reihenfolge:
        ///   R1  erste Zuordnung Z_ProjektPufferSp mit Erzeuger = 'Wärmepumpe' (nach
        ///       Prioritaet) -&gt; Betriebsparameter an den Puffer, Senke an ALLE
        ///       WP-Anlagen. Das entspricht exakt der heutigen break-Logik in
        ///       SimulationControl.
        ///   R2  Zuordnungen anderer Erzeuger -&gt; keine Übernahme (waren wirkungslos),
        ///       je Eintrag ein Protokollhinweis.
        ///   R3  WQ_Typ = 'Pufferspeicher' mit WQ_Puffer (Bezeichner) -&gt; WQ_ID_Puffer.
        ///   R6  BHKW-Pendelspeicher aus Tab_Einstellungen.Pendelspeicher als echten
        ///       Projekt-Puffer anlegen (vor R4, damit R4 die Anlagenzeile mitzieht).
        ///   R4  Projekt-Puffer ohne Anlagenzeile (ID_Type = 12) nachtragen.
        ///   R5  verhaltensneutrale Vorbelegung aller übrigen Felder.
        ///
        /// <c>Z_ProjektPufferSp</c> wird ausschließlich GELESEN (Konzept 5.4): weder
        /// geändert noch gelöscht. Zusammen damit, dass die Engine die neuen Spalten
        /// noch nicht liest, ist der Schritt ergebnisneutral.
        ///
        /// Der Schritt ist zusätzlich in sich idempotent (alle Einfügungen sind durch
        /// Existenzprüfungen gedeckt, alle Aktualisierungen schreiben denselben Wert) -
        /// ein Wiederholungslauf nach einem Abbruch mitten im Schritt richtet also
        /// keinen Schaden an.
        /// </summary>
        private static bool Schritt_5_ProjektdatenQuellenSenken(Lauf l)
        {
            DataTable projekte = Abfrage(l, "SELECT ID FROM Tab_Projekt ORDER BY ID");
            if (projekte == null)
            {
                l.Notiz("Tab_Projekt ist nicht lesbar - die Datenmigration wurde nicht ausgeführt.");
                return false;
            }

            bool ok = true;
            int migriert = 0;

            foreach (DataRow p in projekte.Rows)
            {
                int idProjekt = Zahl(p["ID"]);
                if (idProjekt <= 0) continue;

                if (ProjektMigrieren(l, idProjekt)) migriert++;
                else ok = false;
            }

            l.Notiz("Projekte bearbeitet: " + migriert + " von " + projekte.Rows.Count);
            l.Notiz("R1: " + DatenPufferVerwendung + " Puffer mit Verwendung/Betriebsparameter, " +
                    DatenAnlagenPuffersenke + " Anlagen mit WS_Ziel = '" + WS_ZIEL_PUFFER_HEIZUNG + "'");
            l.Notiz("R3: " + DatenQuellPuffer + " Quell-Pufferreferenzen aufgelöst");
            l.Notiz("R4: " + DatenAnlagenzeilenNeu + " Anlagenzeilen (ID_Type = " + TYP_PUFFER +
                    ") nachgetragen, " + DatenAnlagenzeilenRepariert + " vorhandene mit ID_PUFFER repariert");
            l.Notiz("R5: " + DatenAnlagenHeizkreis + " Anlagen mit WS_Ziel = '" + WS_ZIEL_HEIZKREIS + "'");
            l.Notiz("R6: " + DatenPendelspeicherNeu + " Puffer '" + BEZ_PENDELSPEICHER + "' angelegt, " +
                    DatenPendelspeicherTemperaturen + " davon mit Systemtemperaturen vorbelegt");
            l.Notiz("Hinweise insgesamt: " + DatenHinweise);
            return ok;
        }

        private static bool ProjektMigrieren(Lauf l, int idProjekt)
        {
            bool ok = true;

            if (!Regel1_WaermepumpenZuordnung(l, idProjekt)) ok = false;
            Regel2_UebrigeZuordnungen(l, idProjekt);
            if (!Regel3_QuellPuffer(l, idProjekt)) ok = false;
            if (!Regel6_BhkwPendelspeicher(l, idProjekt)) ok = false;
            if (!Regel4_AnlagenzeilenNachtragen(l, idProjekt)) ok = false;
            if (!Regel5_Vorbelegung(l, idProjekt)) ok = false;

            return ok;
        }

        // --- R1 ----------------------------------------------------------------------

        /// <summary>
        /// Übernimmt die heute allein wirksame Zuordnung: den ERSTEN Eintrag mit
        /// <c>Erzeuger = 'Wärmepumpe'</c> nach <c>Prioritaet</c>. SimulationControl
        /// liest über <c>Z_ProjektPufferSpCtrl.ReadAll</c> (ORDER BY Prioritaet) und
        /// bricht nach dem ersten WP-Treffer mit <c>break</c> ab.
        ///
        /// Der Sortierschlüssel ist hier <c>Prioritaet, ID</c> - das <c>, ID</c> ist die
        /// einzige Abweichung und macht die Migration bei gleicher Priorität
        /// reproduzierbar (in der Arbeitskopie tragen die Dubletten je Projekt
        /// ohnehin identische Werte).
        /// </summary>
        private static bool Regel1_WaermepumpenZuordnung(Lauf l, int idProjekt)
        {
            DataTable z = Abfrage(l,
                "SELECT ID, ID_Pufferspeicher, Pufferspeicher, Vorlauf, Ruecklauf, Prioritaet, " +
                "       Schwelle_Ein, Schwelle_Aus " +
                "FROM Z_ProjektPufferSp WHERE ID_Projekt = ? AND Erzeuger = ? ORDER BY Prioritaet, ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@erz", ERZEUGER_WAERMEPUMPE));

            if (z == null) return false;
            if (z.Rows.Count == 0) return true;

            DataRow erste = z.Rows[0];
            int idZuordnung = Zahl(erste["ID"]);
            int idPuffer = PufferAufloesen(l, idProjekt, Zahl(erste["ID_Pufferspeicher"]),
                                           Txt(erste["Pufferspeicher"]));

            bool ok = true;
            if (idPuffer > 0)
            {
                // Betriebsparameter wandern von der Zuordnung an den Speicher (Konzept 5.1).
                // NULL-tolerant: was in der Zuordnung leer ist, bleibt auch am Puffer leer -
                // die Engine-Vorgaben (10 % / 95 %) greifen dann später.
                object sAus = Wert(erste, "Schwelle_Aus");

                // Etappe 4 / Review-Nacharbeit: das Temperaturpaar wird nur übernommen,
                // wenn es als Betriebsvorgabe taugt (ProjektPuffer.IstTemperaturpaar:
                // beide gesetzt, Rücklauf > 0, Vorlauf > Rücklauf) - dasselbe Prinzip,
                // nach dem R6 die Systemvorgaben prüft. Ein vertauschtes Paar an den
                // Speicher zu schreiben wäre schlechter als gar nichts: es sähe gepflegt
                // aus, ergäbe über ΔT <= 0 aber doch nur den stillen Rückfall - und
                // verdeckte dabei die Zuordnung, die Stufe 2 der Rückfallkette ist.
                // Belegt an Projekt 1008: die Zuordnungen 10058/10072 tragen
                // Vorlauf 35 / Ruecklauf 45, also vertauscht.
                int? zVor = ZahlOderNull(Wert(erste, "Vorlauf"));
                int? zRue = ZahlOderNull(Wert(erste, "Ruecklauf"));
                bool paar = ProjektPuffer.IstTemperaturpaar(zVor, zRue);

                int n = NonQuery(l,
                    "UPDATE Tab_Pufferspeicher SET Verwendung = ?, Vorlauf = ?, Ruecklauf = ?, " +
                    "Schwelle_Ein = ?, Schwelle_Aus = ?, Schwelle_Aus_Nachrang = ? WHERE ID = ?",
                    new OleDbParameter("@verw", VERWENDUNG_HEIZUNG),
                    Par("@vor", OleDbType.Integer, paar ? (object)zVor.Value : DBNull.Value),
                    Par("@rue", OleDbType.Integer, paar ? (object)zRue.Value : DBNull.Value),
                    Par("@sEin", OleDbType.Double, Wert(erste, "Schwelle_Ein")),
                    Par("@sAus", OleDbType.Double, sAus),
                    // Ohne Reservezone: nachrangige Erzeuger schalten bei derselben
                    // Schwelle ab wie der vorrangige -> verhaltensneutral (Konzept 3.4).
                    Par("@sNach", OleDbType.Double, sAus),
                    new OleDbParameter("@id", idPuffer));

                if (n >= 0 && !paar)
                    Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                               " trägt kein brauchbares Temperaturpaar (Vorlauf " +
                               (zVor.HasValue ? zVor.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                               ", Rücklauf " +
                               (zRue.HasValue ? zRue.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                               ") - Vorlauf/Ruecklauf am Puffer " + idPuffer + " bleiben leer, " +
                               "die Engine fällt geordnet auf Zuordnung bzw. Vorgabe zurück.");

                if (n < 0) ok = false;
                else
                {
                    DatenPufferVerwendung++;

                    int nAnlagen = NonQuery(l,
                        "UPDATE Tab_Energieanlagen SET WS_Ziel = ?, WS_ID_Puffer = ? " +
                        "WHERE ID_Projekt = ? AND ID_Type = ?",
                        new OleDbParameter("@ziel", WS_ZIEL_PUFFER_HEIZUNG),
                        new OleDbParameter("@puf", idPuffer),
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@typ", TYP_WP));

                    if (nAnlagen < 0) ok = false;
                    else
                    {
                        DatenAnlagenPuffersenke += nAnlagen;
                        l.Notiz("Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                                " -> Puffer " + idPuffer + " (Verwendung '" + VERWENDUNG_HEIZUNG +
                                "'), " + nAnlagen + " Wärmepumpen-Anlage(n) auf '" +
                                WS_ZIEL_PUFFER_HEIZUNG + "'");
                        if (nAnlagen == 0)
                            Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                                       " nennt eine Wärmepumpe, im Projekt gibt es aber keine " +
                                       "WP-Anlage - der Puffer bleibt ohne Erzeuger.");
                    }
                }
            }
            else
            {
                Hinweis(l, "Projekt " + idProjekt + " R1: Zuordnung " + idZuordnung +
                           " verweist auf keinen Pufferspeicher des Projekts - keine Übernahme.");
            }

            // Alles nach dem ersten Treffer ist heute wirkungslos (break in SimulationControl).
            for (int i = 1; i < z.Rows.Count; i++)
                Hinweis(l, "Projekt " + idProjekt + " R1: weitere Wärmepumpen-Zuordnung " +
                           Zahl(z.Rows[i]["ID"]) + " (Puffer '" + Txt(z.Rows[i]["Pufferspeicher"]) +
                           "') war schon bisher wirkungslos und wurde nicht übernommen.");

            return ok;
        }

        /// <summary>
        /// Ermittelt den Projekt-Puffer einer Zuordnung. Vorrang hat die ID; sie muss aber
        /// zum selben Projekt gehören - ein Verweis auf den Speicher eines fremden
        /// Projekts wäre derselbe stille Datenfehler, den Schritt 4 für ID_PUFFER
        /// bereinigt hat. Rückfallweg ist der Bezeichner (wie in SimulationControl).
        /// </summary>
        private static int PufferAufloesen(Lauf l, int idProjekt, int idPuffer, string bezeichner)
        {
            if (idPuffer > 0)
            {
                object treffer = Scalar(l,
                    "SELECT ID FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?",
                    new OleDbParameter("@id", idPuffer),
                    new OleDbParameter("@proj", idProjekt));
                if (treffer != null) return Zahl(treffer);
            }

            if (!string.IsNullOrEmpty(bezeichner))
            {
                object ueberNamen = Scalar(l,
                    "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezeichner));
                if (ueberNamen != null) return Zahl(ueberNamen);
            }

            return 0;
        }

        // --- R2 ----------------------------------------------------------------------

        /// <summary>
        /// Zuordnungen mit einem anderen Erzeuger als der Wärmepumpe hat die Engine nie
        /// ausgewertet (Stufe 1 der Pufferintegration). Sie werden bewusst NICHT
        /// übernommen - sonst entstünde aus einer wirkungslosen Altzeile eine wirksame
        /// Senke und die Ergebnisse änderten sich. Jede Zeile wird protokolliert, damit
        /// der Anwender sie in Paket 2 bewusst neu setzen kann.
        /// </summary>
        private static void Regel2_UebrigeZuordnungen(Lauf l, int idProjekt)
        {
            DataTable z = Abfrage(l,
                "SELECT ID, Erzeuger, Pufferspeicher FROM Z_ProjektPufferSp " +
                "WHERE ID_Projekt = ? AND (Erzeuger IS NULL OR Erzeuger <> ?) ORDER BY Prioritaet, ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@erz", ERZEUGER_WAERMEPUMPE));

            if (z == null) return;

            foreach (DataRow r in z.Rows)
                Hinweis(l, "Projekt " + idProjekt + " R2: Zuordnung " + Zahl(r["ID"]) +
                           " (Erzeuger '" + Txt(r["Erzeuger"]) + "', Puffer '" +
                           Txt(r["Pufferspeicher"]) + "') war ohne Wirkung und wurde nicht " +
                           "übernommen - Wärmesenke bei Bedarf neu zuweisen.");
        }

        // --- R3 ----------------------------------------------------------------------

        /// <summary>
        /// Wandelt die Bezeichner-Referenz <c>WQ_Puffer</c> in den Fremdschlüssel
        /// <c>WQ_ID_Puffer</c>. Die Altspalte bleibt unverändert lesbar (Konzept 5.3).
        /// </summary>
        private static bool Regel3_QuellPuffer(Lauf l, int idProjekt)
        {
            DataTable q = Abfrage(l,
                "SELECT ID, Bezeichner, WQ_Puffer FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND WQ_Typ = ? AND WQ_Puffer IS NOT NULL ORDER BY ID",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@typ", WaermequelleClass.TYP_PUFFER));

            if (q == null) return false;

            bool ok = true;
            foreach (DataRow r in q.Rows)
            {
                int idAnlage = Zahl(r["ID"]);
                string bezPuffer = Txt(r["WQ_Puffer"]);
                if (bezPuffer.Length == 0) continue;

                object treffer = Scalar(l,
                    "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@bez", bezPuffer));

                int idPuffer = Zahl(treffer);
                if (idPuffer <= 0)
                {
                    // Feld bleibt NULL - die Anlage rechnet weiter über den Altweg.
                    Hinweis(l, "Projekt " + idProjekt + " R3: Anlage " + idAnlage + " (" +
                               Txt(r["Bezeichner"]) + ") bezieht Wärme aus dem Puffer '" +
                               bezPuffer + "', der im Projekt nicht existiert - " +
                               "Quell-Puffer im Projekt anlegen.");
                    continue;
                }

                if (NonQuery(l, "UPDATE Tab_Energieanlagen SET WQ_ID_Puffer = ? WHERE ID = ?",
                             new OleDbParameter("@puf", idPuffer),
                             new OleDbParameter("@id", idAnlage)) < 0)
                {
                    ok = false;
                    continue;
                }

                DatenQuellPuffer++;
                l.Notiz("Projekt " + idProjekt + " R3: Anlage " + idAnlage +
                        " -> WQ_ID_Puffer = " + idPuffer + " ('" + bezPuffer + "')");
            }

            return ok;
        }

        // --- R4 ----------------------------------------------------------------------

        /// <summary>
        /// Trägt für jeden Projekt-Puffer ohne Anlagenzeile eine solche nach
        /// (<c>ID_Type = 12</c>), damit er im Projektbaum erscheint.
        ///
        /// Die Zuordnung Anlagenzeile ↔ Puffer läuft im Bestand über den BEZEICHNER
        /// (<c>PufferSpCtrl.ProjektWaisenEntfernen</c>, <c>GetProjektId</c>), nicht über
        /// die ID. Deshalb wird je (Projekt, Bezeichner) genau EINE Zeile angelegt und
        /// mit der kleinsten Puffer-ID dieses Bezeichners verknüpft - andernfalls
        /// entstünden für die vielen gleichnamigen Kopien der Arbeitskopie Dutzende
        /// identischer Baumeinträge.
        ///
        /// Seit der Review-Nacharbeit repariert die Regel zusätzlich BESTEHENDE
        /// Puffer-Anlagenzeilen mit leerem <c>ID_PUFFER</c> (siehe unten). Nebenwirkung,
        /// die man kennen muss: <c>PufferSpCtrl.ProjektWaisenEntfernen</c> löscht
        /// Projektkopien, zu denen KEINE Anlagenzeile gleichen Bezeichners mehr
        /// existiert. Weil R4 für jeden Bezeichner eine Anlagenzeile sicherstellt, ist
        /// nach der Migration keine Projektkopie mehr "verwaist" - das Aufräumen läuft
        /// danach also ins Leere, bis der Anwender selbst eine Puffer-Anlage löscht.
        /// Das ist gewollt: die Migration darf keine Anwenderdaten entfernen.
        /// </summary>
        private static bool Regel4_AnlagenzeilenNachtragen(Lauf l, int idProjekt)
        {
            DataTable puffer = Abfrage(l,
                "SELECT Bezeichner, MIN(ID) AS ErsteID FROM Tab_Pufferspeicher " +
                "WHERE ID_Projekt = ? GROUP BY Bezeichner",
                new OleDbParameter("@proj", idProjekt));

            if (puffer == null) return false;

            bool ok = true;
            foreach (DataRow r in puffer.Rows)
            {
                string bez = Txt(r["Bezeichner"]);
                int idPuffer = Zahl(r["ErsteID"]);
                if (bez.Length == 0)
                {
                    Hinweis(l, "Projekt " + idProjekt + " R4: Pufferspeicher " + idPuffer +
                               " hat keinen Bezeichner - keine Anlagenzeile angelegt.");
                    continue;
                }

                object vorhanden = Scalar(l,
                    "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ?",
                    new OleDbParameter("@proj", idProjekt),
                    new OleDbParameter("@typ", TYP_PUFFER),
                    new OleDbParameter("@bez", bez));

                if (Zahl(vorhanden) > 0)
                {
                    // Review-Nacharbeit: Eine BESTEHENDE Puffer-Anlagenzeile ohne
                    // ID_PUFFER bekommt die Referenz nachgetragen - dieselbe Auswahl
                    // (kleinste ID des gleichnamigen Projekt-Puffers), mit der oben eine
                    // neue Zeile verknüpft würde.
                    //
                    // Warum das nötig ist: FormMain.SetPufferSpControl liest den Wert mit
                    // einem harten (int)-Cast (FormMain.cs:1116). Eine Zeile mit NULL
                    // reisst die Projektansicht dort mit einer InvalidCastException ab -
                    // und genau solche Zeilen entstehen, weil Schritt 4 ungültige
                    // ID_PUFFER-Werte auf NULL zieht. Die Migration räumt die Datenlage
                    // hier auf; der fehlende defensive Read in FormMain bleibt davon
                    // unberührt und ist der FormMain-Parallelsitzung gemeldet.
                    int n = NonQuery(l,
                        "UPDATE Tab_Energieanlagen SET ID_PUFFER = ? " +
                        "WHERE ID_Projekt = ? AND ID_Type = ? AND Bezeichner = ? " +
                        "  AND (ID_PUFFER IS NULL OR ID_PUFFER = 0)",
                        new OleDbParameter("@puf", idPuffer),
                        new OleDbParameter("@proj", idProjekt),
                        new OleDbParameter("@typ", TYP_PUFFER),
                        new OleDbParameter("@bez", bez));

                    if (n < 0) { ok = false; continue; }
                    if (n > 0)
                    {
                        DatenAnlagenzeilenRepariert += n;
                        l.Notiz("Projekt " + idProjekt + " R4: " + n + " vorhandene Anlagenzeile(n) für Puffer '" +
                                bez + "' auf ID_PUFFER = " + idPuffer + " gesetzt (war leer)");
                    }
                    continue;
                }

                if (!AnlagenzeileAnlegen(l, idProjekt, bez, idPuffer)) { ok = false; continue; }

                DatenAnlagenzeilenNeu++;
                l.Notiz("Projekt " + idProjekt + " R4: Anlagenzeile für Puffer '" + bez +
                        "' nachgetragen (ID_PUFFER = " + idPuffer + ")");
            }

            return ok;
        }

        /// <summary>
        /// Legt eine Puffer-Anlagenzeile an. Anweisung und Parameter stehen seit
        /// Etappe 3 in <see cref="ProjektPuffer"/> - die Oberfläche legt beim Anlegen
        /// eines Pendelspeichers dieselbe Zeile an und darf dabei nicht abweichen.
        /// Die Fallstricke (AutoWert, Komponenten-Fremdschlüssel auf NULL, Par() mit
        /// ausdrücklichem Typ) sind dort dokumentiert.
        /// </summary>
        private static bool AnlagenzeileAnlegen(Lauf l, int idProjekt, string bezeichner, int idPuffer)
        {
            return NonQuery(l, ProjektPuffer.SQL_ANLAGENZEILE_INSERT,
                            ProjektPuffer.AnlagenzeileParameter(idProjekt, bezeichner, idPuffer)) >= 0;
        }

        // --- R5 ----------------------------------------------------------------------

        /// <summary>
        /// Vorbelegung, die den Bestand ausdrücklich NICHT verändert (Konzept 3.4/5.5):
        ///
        ///   - Wärmeerzeuger ohne Senke bekommen <c>WS_Ziel = 'Heizkreis'</c>, also genau
        ///     das, was die Engine heute tut. <c>WS_Typ</c> (Bedarfsart) bleibt unberührt.
        ///   - <c>WS_Ladeprio*</c>, <c>WS_Ladeprio_PV</c> und <c>WS_Ladegrenze*</c> werden
        ///     auf 0 gesetzt. Das sind KEINE Fremdschlüssel - 0 heißt hier "nach Vorgabe"
        ///     bzw. "nicht gesetzt". Die ID-Spalten (WS_ID_Puffer, WS_ID_Puffer2,
        ///     WQ_ID_Puffer) bleiben dagegen NULL, wenn sie nicht gesetzt sind: eine 0
        ///     würde die erzwungenen Beziehungen aus Schritt 4 verletzen.
        ///   - Am Puffer: <c>Entladeprio = 0</c> (automatisch) und
        ///     <c>Schwelle_Aus_Nachrang = Schwelle_Aus</c> - letzteres nur dort, wo eine
        ///     Abschaltschwelle gepflegt ist; sonst bleiben beide NULL, damit später die
        ///     Engine-Vorgaben 10 % / 95 % greifen.
        /// </summary>
        private static bool Regel5_Vorbelegung(Lauf l, int idProjekt)
        {
            bool ok = true;

            int nHeizkreis = NonQuery(l,
                "UPDATE Tab_Energieanlagen SET WS_Ziel = ? WHERE ID_Projekt = ? " +
                "AND ID_Type IN (" + TYP_WP + "," + TYP_SOLARTHERMIE + "," + TYP_KESSEL + "," + TYP_BHKW + ") " +
                "AND (WS_Ziel IS NULL OR WS_Ziel = '')",
                new OleDbParameter("@ziel", WS_ZIEL_HEIZKREIS),
                new OleDbParameter("@proj", idProjekt));

            if (nHeizkreis < 0) ok = false; else DatenAnlagenHeizkreis += nHeizkreis;

            foreach (string spalte in new[]
                     { "WS_Ladeprio", "WS_Ladeprio2", "WS_Ladeprio_PV", "WS_Ladegrenze", "WS_Ladegrenze2" })
            {
                if (NonQuery(l, "UPDATE Tab_Energieanlagen SET [" + spalte + "] = 0 " +
                                "WHERE ID_Projekt = ? AND [" + spalte + "] IS NULL",
                             new OleDbParameter("@proj", idProjekt)) < 0) ok = false;
            }

            if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Entladeprio = 0 " +
                            "WHERE ID_Projekt = ? AND Entladeprio IS NULL",
                         new OleDbParameter("@proj", idProjekt)) < 0) ok = false;

            if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Schwelle_Aus_Nachrang = Schwelle_Aus " +
                            "WHERE ID_Projekt = ? AND Schwelle_Aus_Nachrang IS NULL " +
                            "AND Schwelle_Aus IS NOT NULL",
                         new OleDbParameter("@proj", idProjekt)) < 0) ok = false;

            return ok;
        }

        // --- R6 ----------------------------------------------------------------------

        /// <summary>
        /// Der BHKW-Pendelspeicher war bis Etappe 3 kein Objekt, sondern eine Zahl in
        /// <c>Tab_Einstellungen.Pendelspeicher</c>, die <c>SimulationBHKW</c> intern als
        /// Kapazität führte. Er wird hier zu einem echten Projekt-Puffer, damit ihn
        /// Paket 2 wie jeden anderen Speicher anzeigen und regeln kann.
        ///
        /// Einheit: der Alt-Parameter ist in m³, <c>Gesamtvolumen</c> in Litern -
        /// siehe <see cref="PENDELSPEICHER_M3_IN_LITER"/>.
        ///
        /// Der Alt-Parameter bleibt unverändert stehen (nicht genullt, nicht gelöscht).
        /// Seit Etappe 3 lesen ihn weder Engine noch Oberfläche; er ist damit eine tote,
        /// aber unschädliche Spalte - und die einzige Grundlage dieser Migration, die
        /// auf einer noch nicht migrierten Datenbank genau einmal greift.
        /// </summary>
        private static bool Regel6_BhkwPendelspeicher(Lauf l, int idProjekt)
        {
            object roh = Scalar(l, "SELECT TOP 1 Pendelspeicher FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                                new OleDbParameter("@proj", idProjekt));
            double volumenM3 = Kommazahl(roh);
            if (volumenM3 <= 0) return true;

            int anzahlBhkw = Zahl(Scalar(l,
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@typ", TYP_BHKW)));

            if (anzahlBhkw == 0)
            {
                Hinweis(l, "Projekt " + idProjekt + " R6: Pendelspeicher " + Anzeige(volumenM3) +
                           " m³ eingetragen, aber keine BHKW-Anlage im Projekt - kein Puffer angelegt.");
                return true;
            }

            int volumenLiter = (int)Math.Round(volumenM3 * PENDELSPEICHER_M3_IN_LITER,
                                               MidpointRounding.AwayFromZero);

            int idPuffer = Zahl(Scalar(l,
                "SELECT MIN(ID) FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@bez", BEZ_PENDELSPEICHER)));

            if (idPuffer > 0)
            {
                // Wiederverwenden statt doppelt anlegen. Das gepflegte Volumen des
                // vorhandenen Speichers bleibt stehen - es ist die jüngere Angabe.
                if (NonQuery(l, "UPDATE Tab_Pufferspeicher SET Verwendung = ? " +
                                "WHERE ID = ? AND (Verwendung IS NULL OR Verwendung = '')",
                             new OleDbParameter("@verw", VERWENDUNG_HEIZUNG),
                             new OleDbParameter("@id", idPuffer)) < 0) return false;

                l.Notiz("Projekt " + idProjekt + " R6: vorhandener Puffer '" + BEZ_PENDELSPEICHER +
                        "' (ID " + idPuffer + ") wiederverwendet.");
            }
            else
            {
                idPuffer = Zahl(Scalar(l, "SELECT MAX(ID) FROM Tab_Pufferspeicher")) + 1;

                // Etappe 4: Vorbelegung der Betriebstemperaturen aus den SYSTEMVORGABEN
                // des Projekts (kleinster Vorlauf / größter Rücklauf über die
                // Wärmeerzeuger). Gibt es dort nichts, bleiben beide Spalten NULL.
                int? sysVor = SystemTemperatur(l, idProjekt, ProjektPuffer.SQL_SYSTEM_VORLAUF);
                int? sysRue = SystemTemperatur(l, idProjekt, ProjektPuffer.SQL_SYSTEM_RUECKLAUF);

                // ID explizit nach dem GetMaxID-Muster aus PufferSpCtrl.CopyFromStamm -
                // Tab_Pufferspeicher.ID ist kein AutoWert. Anweisung und Parameter aus
                // ProjektPuffer, damit die Oberfläche denselben Puffer erzeugt.
                if (NonQuery(l, ProjektPuffer.SQL_PUFFER_INSERT,
                             ProjektPuffer.PufferParameter(idPuffer, idProjekt,
                                                           BEZ_PENDELSPEICHER, volumenLiter,
                                                           sysVor, sysRue)) < 0)
                    return false;

                DatenPendelspeicherNeu++;
                if (ProjektPuffer.IstTemperaturpaar(sysVor, sysRue))
                {
                    DatenPendelspeicherTemperaturen++;
                    l.Notiz("Projekt " + idProjekt + " R6: Systemvorgaben " + sysVor.Value + "/" +
                            sysRue.Value + " °C als Betriebstemperaturen vorbelegt.");
                }
                else
                {
                    l.Notiz("Projekt " + idProjekt + " R6: keine brauchbaren Systemvorgaben (Vorlauf " +
                            (sysVor.HasValue ? sysVor.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                            ", Rücklauf " +
                            (sysRue.HasValue ? sysRue.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                            ") - Vorlauf/Ruecklauf bleiben leer.");
                }

                l.Notiz("Projekt " + idProjekt + " R6: Puffer '" + BEZ_PENDELSPEICHER + "' angelegt (ID " +
                        idPuffer + ", " + Anzeige(volumenM3) + " m³ = " + volumenLiter + " l)");
            }

            int nBhkw = NonQuery(l, ProjektPuffer.SQL_BHKW_AUF_PUFFER,
                                 ProjektPuffer.BhkwAufPufferParameter(idProjekt, idPuffer));

            if (nBhkw < 0) return false;

            DatenAnlagenPuffersenke += nBhkw;
            l.Notiz("Projekt " + idProjekt + " R6: " + nBhkw + " BHKW-Anlage(n) auf '" +
                    WS_ZIEL_PUFFER_HEIZUNG + "' (Puffer " + idPuffer + ")");
            return true;
        }

        /// <summary>
        /// Systemvorgabe eines Projekts (kleinster Vorlauf bzw. größter Rücklauf über die
        /// Wärmeerzeuger-Anlagen), <c>null</c> wenn dort nichts gepflegt ist.
        ///
        /// Bewusst auf der stillen Migrationsverbindung statt über
        /// <c>PufferSpCtrl.SystemVorlauf</c>: die Migration darf keine zweite Verbindung
        /// auf eine Datei aufmachen, die sie gerade exklusiv umbaut. Gemeinsam ist mit
        /// dem Controller die Anweisung (<see cref="ProjektPuffer.SQL_SYSTEM_VORLAUF"/>),
        /// nicht der Weg zur Datenbank - dasselbe Muster wie bei den übrigen Bausteinen.
        /// </summary>
        private static int? SystemTemperatur(Lauf l, int idProjekt, string sql)
        {
            object v = Scalar(l, sql, ProjektPuffer.SystemTemperaturParameter(idProjekt));
            if (v == null || v == DBNull.Value) return null;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        // --- gemeinsame Kleinigkeiten -------------------------------------------------

        private static void Hinweis(Lauf l, string text)
        {
            DatenHinweise++;
            l.Notiz("HINWEIS  " + text);
        }

        /// <summary>
        /// Parameter mit ausdrücklichem Typ. Nötig überall dort, wo der Wert
        /// <see cref="DBNull"/> sein kann: aus DBNull allein kann der OLE-DB-Provider
        /// den Spaltentyp nicht ableiten.
        /// </summary>
        private static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            return new OleDbParameter(name, typ) { Value = wert ?? DBNull.Value };
        }

        // =================================================================================
        // Blockade des Simulationsbereichs
        // =================================================================================

        /// <summary>
        /// true, wenn die Migration gelaufen ist und NICHT durchkam. Der Simulationsbereich
        /// verweigert dann den Start, statt auf halb migriertem Schema zu rechnen.
        /// </summary>
        public static bool SimulationGesperrt(out string grund)
        {
            if (!Ausgefuehrt || MigrationOk)
            {
                grund = null;
                return false;
            }

            grund = "Die Datenbank ist nicht auf dem für die Simulation benötigten Stand." +
                    Environment.NewLine + Environment.NewLine +
                    FehlerKopf() + Environment.NewLine + Environment.NewLine +
                    "Der Simulationsbereich bleibt gesperrt, bis die Aktualisierung der " +
                    "Datenbank erfolgreich war.";
            return true;
        }

        /// <summary>
        /// Die ersten Zeilen des Berichts - genug für eine verständliche Meldung,
        /// ohne den Anwender mit dem vollständigen Protokoll zu erschlagen.
        /// </summary>
        public static string FehlerKopf()
        {
            if (string.IsNullOrEmpty(Fehlerbericht)) return "(kein Bericht vorhanden)";

            string[] zeilen = Fehlerbericht.Replace("\r\n", "\n").Split('\n');
            var kopf = new List<string>();
            foreach (string z in zeilen)
            {
                kopf.Add(z);
                if (kopf.Count >= 12) break;
            }
            return string.Join(Environment.NewLine, kopf).TrimEnd();
        }

        /// <summary>Vollständiger Pfad der Protokolldatei neben der Datenbank.</summary>
        public static string ProtokollPfad()
        {
            try
            {
                string ordner = Path.GetDirectoryName(DataRepository.GetDBPath());
                return string.IsNullOrEmpty(ordner) ? PROTOKOLL_DATEI : Path.Combine(ordner, PROTOKOLL_DATEI);
            }
            catch { return PROTOKOLL_DATEI; }
        }

        /// <summary>
        /// Best effort: schlägt das Schreiben fehl (schreibgeschützter Ordner - genau der
        /// Fall, in dem auch die Migration scheitert), darf das nichts blockieren.
        /// </summary>
        private static void ProtokollSchreiben(string dbPfad, string bericht)
        {
            try
            {
                string ordner = Path.GetDirectoryName(dbPfad);
                if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner)) return;
                File.WriteAllText(Path.Combine(ordner, PROTOKOLL_DATEI), bericht, new UTF8Encoding(true));
            }
            catch { /* bewusst still - das Protokoll ist eine Zugabe, keine Voraussetzung */ }
        }

        // =================================================================================
        // Ausführungs-Hilfsmittel (still, ohne Dialoge)
        // =================================================================================

        private sealed class Lauf
        {
            public OleDbConnection Conn;
            public string DbPfad;
            public string LetzterFehler;

            private readonly List<string> _kopf = new List<string>();
            private readonly List<string> _zeilen = new List<string>();
            private readonly List<string> _notizen = new List<string>();

            public void Kopf(string t) { _kopf.Add(t); }
            public void Zeile(string t) { _zeilen.Add(t); }
            public void Leerzeile() { _zeilen.Add(""); }
            public void Notiz(string t) { _notizen.Add(t); }

            /// <summary>Übernimmt die gesammelten Detailnotizen des laufenden Schritts.</summary>
            public void Detail()
            {
                foreach (string n in _notizen) _zeilen.Add("        - " + n);
                _notizen.Clear();
            }

            public string Text()
            {
                var sb = new StringBuilder();
                foreach (string z in _kopf) sb.AppendLine(z);
                foreach (string z in _zeilen) sb.AppendLine(z);
                return sb.ToString().TrimEnd();
            }
        }

        /// <summary>Spaltenliste einer Tabelle; null, wenn die Tabelle nicht lesbar ist.</summary>
        private static DataTable TabellenSchema(Lauf l, string tabelle)
        {
            try
            {
                var dt = new DataTable();
                using (var cmd = new OleDbCommand("SELECT TOP 1 * FROM [" + tabelle + "]", l.Conn))
                using (var adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.FillSchema(dt, SchemaType.Source);
                }
                return dt;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return null;
            }
        }

        /// <summary>
        /// Führt eine DDL-/DML-Anweisung aus. "existiert bereits" gilt als Erfolg -
        /// die Migration muss über bereits vorhandene Objekte idempotent hinweggehen.
        /// </summary>
        private static bool Ddl(Lauf l, string sql, string bezeichnung, bool stillBeiErfolg = false)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn)) cmd.ExecuteNonQuery();
                if (!stillBeiErfolg) l.Notiz(bezeichnung + ": angelegt");
                return true;
            }
            catch (OleDbException ex)
            {
                if (IstBereitsVorhanden(ex))
                {
                    if (!stillBeiErfolg) l.Notiz(bezeichnung + ": bereits vorhanden");
                    return true;
                }
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz(bezeichnung + ": FEHLER - " + Kurzmeldung(ex));
                return false;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz(bezeichnung + ": FEHLER - " + Kurzmeldung(ex));
                return false;
            }
        }

        private static int NonQuery(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz("SQL fehlgeschlagen (" + Kurzmeldung(ex) + "): " + Gekuerzt(sql));
                return -1;
            }
        }

        private static object Scalar(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    object v = cmd.ExecuteScalar();
                    return v == DBNull.Value ? null : v;
                }
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                return null;
            }
        }

        private static DataTable Abfrage(Lauf l, string sql, params OleDbParameter[] p)
        {
            try
            {
                var dt = new DataTable();
                using (var cmd = new OleDbCommand(sql, l.Conn))
                {
                    if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
                    using (var adapter = new OleDbDataAdapter(cmd)) adapter.Fill(dt);
                }
                return dt;
            }
            catch (Exception ex)
            {
                l.LetzterFehler = Kurzmeldung(ex);
                l.Notiz("Abfrage fehlgeschlagen (" + Kurzmeldung(ex) + "): " + Gekuerzt(sql));
                return null;
            }
        }

        /// <summary>
        /// Erkennt "Objekt existiert bereits" an der Jet-/ACE-Fehlernummer (SQLState) und
        /// ersatzweise am Meldungstext. Die Nummern sind sprachunabhängig:
        ///   3010 Tabelle existiert bereits
        ///   3283 Primärschlüssel existiert bereits
        ///   3375 Index existiert bereits
        ///   3378 Beziehung dieses Namens existiert bereits
        ///   3380 Feld existiert bereits
        /// </summary>
        private static bool IstBereitsVorhanden(OleDbException ex)
        {
            if (ex == null) return false;

            foreach (OleDbError e in ex.Errors)
            {
                switch (e.SQLState)
                {
                    case "3010":
                    case "3283":
                    case "3375":
                    case "3378":
                    case "3380":
                        return true;
                }
            }

            string m = (ex.Message ?? "").ToLowerInvariant();
            return m.Contains("already exists")
                || m.Contains("already has an index")
                || m.Contains("already a relationship")
                || m.Contains("existiert bereits")
                || m.Contains("bereits einen index")
                || m.Contains("bereits eine beziehung");
        }

        private static string Kurzmeldung(Exception ex)
        {
            if (ex == null) return "";
            string m = ex.Message ?? "";
            m = m.Replace("\r", " ").Replace("\n", " ").Trim();
            return m.Length > 300 ? m.Substring(0, 297) + "..." : m;
        }

        private static string Gekuerzt(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "";
            sql = sql.Replace("\r", " ").Replace("\n", " ");
            return sql.Length > 90 ? sql.Substring(0, 87) + "..." : sql;
        }

        private static int Zahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static double Kommazahl(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        /// <summary>
        /// Ganzzahl oder <c>null</c> - im Unterschied zu <see cref="Zahl"/> bleibt "nicht
        /// gepflegt" hier von der echten 0 unterscheidbar. Genau das braucht
        /// <see cref="ProjektPuffer.IstTemperaturpaar"/>.
        /// </summary>
        private static int? ZahlOderNull(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return null; }
        }

        private static string Txt(object o)
        {
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        /// <summary>
        /// Spaltenwert einer Zeile als Parameterwert - fehlende Spalte und NULL werden
        /// gleichermaßen zu <see cref="DBNull"/>. So bleibt die Übernahme NULL-tolerant,
        /// ohne dass aus einem leeren Altwert eine 0 wird.
        /// </summary>
        private static object Wert(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte)) return DBNull.Value;
            return r[spalte] == DBNull.Value ? DBNull.Value : r[spalte];
        }

        private static string Anzeige(double d)
        {
            return d.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
