using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zugriff auf <c>Z_AnlageStrang</c> — die GEORDNETE STRANGLISTE je PV-Anlage
    /// (Stufe S2, <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 3.4; angelegt von
    /// <c>SchemaMigration</c> Schritt 66). Der Zwilling von
    /// <see cref="Z_AnlageSenkeCtrl"/>, Zeile für Zeile derselben Bauart.
    ///
    /// <para>
    /// <b>Zwei Lesewege, ein Grund.</b> <see cref="LesenJeAnlage"/> bedient den
    /// PV-Dialog (eine Anlage, ihre Stränge in Rangfolge). <see cref="LesenJeProjekt"/>
    /// bedient die Rettung im Speicherweg (<c>WizardCtrl.StraengeSichern</c>) und ab
    /// Stufe S3 den Rechenkern: Beide brauchen die Stränge ALLER Anlagen eines
    /// Projekts, und ein Aufruf je Anlage wäre bei fünf PV-Feldern fünf Rundreisen zur
    /// Datenbank für dieselbe Information. Der Projektbezug läuft über den Verbund zu
    /// <c>Tab_Energieanlagen</c> — <c>Z_AnlageStrang</c> führt bewusst KEIN
    /// <c>ID_Projekt</c>: Die Anlage weiss, zu welchem Projekt sie gehört, und eine
    /// zweite Wahrheit darüber könnte auseinanderlaufen.
    /// </para>
    ///
    /// <para>
    /// <b>Der Schreibweg ist Löschen + Neuanlegen je Anlage</b>
    /// (<see cref="SchreibenJeAnlage"/>) — und das ist hier nicht Bequemlichkeit,
    /// sondern die Form, die zur Sache passt: Der Dialog liefert eine LISTE in einer
    /// bestimmten Reihenfolge; welche Zeile darin die frühere Zeile 3 ist, ist keine
    /// sinnvolle Frage. Die Ränge werden beim Schreiben lückenlos ab 1 neu vergeben.
    /// </para>
    ///
    /// <para>
    /// <b>DIE FALLE N3.3 — sie ist hier NICHT gelöst, sondern im Speicherweg.</b>
    /// <c>Z_AnlageStrang.ID_Anlage</c> hängt mit <c>ON DELETE CASCADE</c> an
    /// <c>Tab_Energieanlagen</c>, und der Speicherweg jeder Anlage ist Löschen +
    /// Neuanlegen (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> gefolgt von
    /// <c>Add_WP_Waermeerzeuger</c>). Ohne Gegenmassnahme räumte JEDES Speichern —
    /// über Karte, Kontextmenü oder Assistent — die Strangzuordnung des Projekts ab.
    /// Die Gegenmassnahme steht dort, wo das Löschen steht: <c>StraengeSichern</c> vor
    /// dem DELETE, <c>StraengeWiederherstellen</c> nach dem Neuanlegen (Block ST1 des
    /// <c>WizardCtrl</c>), wörtlich nach dem Vorbild der Senkenrettung (Block S1).
    /// Dieser Controller kennt die Falle nicht — er schreibt, was man ihm gibt.
    /// </para>
    ///
    /// <para>
    /// <b>Über <see cref="DataRepository"/> mit <c>?</c>-Parametern</b>, nicht über
    /// <c>RecordSet</c> und nie mit verkettetem SQL — Vorgabe für neuen Code. Löschen
    /// und Einfügen laufen auf EINER Verbindung in EINER Transaktion
    /// (<c>DataRepository.Vorgang</c>): Scheitert ein Insert, wird zurückgerollt, sonst
    /// stünde die Anlage nach einem halben Speichervorgang mit einer halben
    /// Strangliste da.
    /// </para>
    ///
    /// <para>
    /// <b>ACE-FALLE: KEINE PARAMETER IN UNTERABFRAGEN.</b> Ein <c>?</c> in der
    /// UNTERABFRAGE trifft bei ACE still 0 Zeilen, ohne einen Fehler zu melden. Der
    /// Projekt-Leseweg benutzt deshalb einen JOIN, keine Unterabfrage — dieselbe Regel
    /// wie bei <see cref="Z_AnlageSenkeCtrl"/>.
    /// </para>
    /// </summary>
    public class AnlageStrangCtrl
    {
        /// <summary>Die Zuordnungstabelle.</summary>
        public const string TABLE = SchemaKatalog.Z_ANLAGESTRANG;

        // =====================================================================
        // Schemaprobe
        // =====================================================================

        /// <summary>
        /// Gemerktes Ergebnis der Tabellenprobe. <c>null</c> = noch nicht geprüft.
        ///
        /// <para>
        /// GECACHT, weil jeder Leser fragt: der PV-Dialog beim Öffnen, die Rettung bei
        /// jedem Speichern, ab S3 die Simulation je Lauf. Die Antwort kann sich
        /// innerhalb einer Sitzung nicht ändern — die Migration läuft beim
        /// Programmstart, lange vor dem ersten Aufruf hier.
        /// </para>
        /// </summary>
        private static bool? m_TabelleVorhanden;

        /// <summary>
        /// Gibt es die Strangliste in dieser Datenbank? <c>false</c> heisst
        /// „Migrationsschritt 66 ist hier noch nicht gelaufen" — dann führt keine
        /// Anlage Stränge, und alles rechnet wie bisher.
        ///
        /// <para>
        /// <b>Warum eine Rückfallebene und kein Eintrag in
        /// <c>SchemaKatalog.Alle</c>:</b> Dort stehen ausschliesslich additive SPALTEN
        /// an vorhandenen Tabellen; hier muss erst die TABELLE entstehen — dieselbe
        /// Grenze wie bei <c>Z_AnlageSenke</c>, und dieselbe Lösung.
        /// </para>
        ///
        /// <para>
        /// <c>COUNT(*)</c> statt einer Schemaabfrage: Eine leere Tabelle liefert 0,
        /// eine FEHLENDE Tabelle einen Fehler und damit <c>null</c> — der Unterschied,
        /// auf den es ankommt.
        /// </para>
        /// </summary>
        public static bool TabelleVorhanden()
        {
            if (m_TabelleVorhanden.HasValue) return m_TabelleVorhanden.Value;

            bool da = StilleDb.Scalar("SELECT COUNT(*) FROM [" + TABLE + "]") != null;
            m_TabelleVorhanden = da;

            if (!da)
                Console.WriteLine("AnlageStrangCtrl: Die Strangliste " + TABLE +
                                  " fehlt (Migrationsschritt 66 noch nicht gelaufen) - " +
                                  "keine Anlage fuehrt Straenge.");

            return da;
        }

        /// <summary>
        /// Verwirft die gemerkte Probe — nach einem Migrationslauf innerhalb derselben
        /// Sitzung (Referenzlauf-Suite, Admin-Werkzeuge). Im Normalbetrieb unnötig.
        /// </summary>
        public static void ProbeVerwerfen()
        {
            m_TabelleVorhanden = null;
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Alle Strangzeilen EINES PROJEKTS, sortiert nach (<c>ID_Anlage</c>,
        /// <c>Rang</c>); nie <c>null</c>. Der Projektbezug läuft über den JOIN auf
        /// <c>Tab_Energieanlagen</c> (siehe Klassenkopf).
        /// </summary>
        public List<AnlageStrangModel> LesenJeProjekt(int idProjekt)
        {
            var liste = new List<AnlageStrangModel>();
            if (idProjekt <= 0 || !TabelleVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT s.ID, s.ID_Anlage, s.Rang, s.Bezeichner, s.ID_Wechselrichter, " +
                "       s.Geraetenummer, s.Mppt, s.Module_Reihe, s.Straenge_Parallel, " +
                "       s.Neigung, s.Azimut, s.ID_PV " +
                "FROM [" + TABLE + "] s " +
                "INNER JOIN [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a ON a.ID = s.ID_Anlage " +
                "WHERE a.ID_Projekt = ? " +
                "ORDER BY s.ID_Anlage, s.Rang",
                new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });

            AusTabelle(dt, liste);
            return liste;
        }

        /// <summary>
        /// Die Stränge EINER Anlage in Rangfolge; nie <c>null</c>.
        ///
        /// <para>
        /// Eine LEERE Liste ist ein gültiges Ergebnis und bedeutet „diese Anlage hat
        /// keine Strangzeile" — also den Weg von heute. Diese Klasse erfindet dafür
        /// KEINE Zeile: Sonst wüsste der Aufrufer nicht mehr, ob er eine gespeicherte
        /// Zuordnung oder eine Notannahme in der Hand hält.
        /// </para>
        /// </summary>
        public List<AnlageStrangModel> LesenJeAnlage(int idAnlage)
        {
            var liste = new List<AnlageStrangModel>();
            if (idAnlage <= 0 || !TabelleVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Anlage, Rang, Bezeichner, ID_Wechselrichter, " +
                "       Geraetenummer, Mppt, Module_Reihe, Straenge_Parallel, " +
                "       Neigung, Azimut, ID_PV " +
                "FROM [" + TABLE + "] WHERE ID_Anlage = ? ORDER BY Rang",
                new DbParam("@anl", DbParamTyp.Integer) { Wert = idAnlage });

            AusTabelle(dt, liste);
            return liste;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Ersetzt die Strangliste EINER Anlage vollständig: erst alle vorhandenen
        /// Zeilen dieser Anlage löschen, dann <paramref name="zeilen"/> in der
        /// übergebenen Reihenfolge neu anlegen. Rückgabe <c>false</c>, wenn nichts
        /// geschrieben wurde — dann steht auch nichts halb Geschriebenes in der
        /// Datenbank.
        ///
        /// <para>
        /// <b>Die Ränge werden neu vergeben</b>, lückenlos ab 1 in Listenreihenfolge.
        /// Was der Aufrufer in <c>Rang</c> mitgibt, wird ignoriert: Nach dem
        /// Umsortieren im Dialog wäre jede andere Regel eine zweite Wahrheit über die
        /// Reihenfolge.
        /// </para>
        ///
        /// <para>
        /// <b>Eine Transaktion.</b> Löschen und Anlegen laufen auf EINER Verbindung;
        /// scheitert ein Insert, wird zurückgerollt. Ein Rollback-Fehler wird gemeldet,
        /// nicht geworfen: Der Aufrufer erfährt das Scheitern ohnehin am Rückgabewert.
        /// </para>
        ///
        /// <para>
        /// Eine LEERE Liste ist zulässig und löscht die Stränge der Anlage — der Weg,
        /// den der Dialog beim Entfernen der letzten Zeile und beim Umschalten auf
        /// „vereinfacht mit Aufräumen" braucht.
        /// </para>
        /// </summary>
        public bool SchreibenJeAnlage(int idAnlage, List<AnlageStrangModel> zeilen)
        {
            if (idAnlage <= 0 || !TabelleVorhanden()) return false;

            // Der Vorgang wird bewusst INNERHALB des try geoeffnet - ein Fehler beim
            // Verbindungsaufbau soll in denselben catch laufen (Muster
            // Z_AnlageSenkeCtrl.SchreibenJeAnlage, Arbeitspaket S4e).
            DbVorgang v = null;

            try
            {
                v = DataRepository.Vorgang();

                v.Ausfuehren("DELETE FROM [" + TABLE + "] WHERE ID_Anlage = ?",
                             new DbParam("@anl", DbParamTyp.Integer) { Wert = idAnlage });

                int rang = 0;
                if (zeilen != null)
                {
                    foreach (AnlageStrangModel z in zeilen)
                    {
                        if (z == null) continue;
                        rang++;

                        // Reihenfolge der Parameter = Reihenfolge der Spalten:
                        // OleDb bindet nach POSITION, nicht nach Namen.
                        v.Ausfuehren(
                            "INSERT INTO [" + TABLE + "] " +
                            "(ID_Anlage, Rang, Bezeichner, ID_Wechselrichter, Geraetenummer, " +
                            " Mppt, Module_Reihe, Straenge_Parallel, Neigung, Azimut, ID_PV) " +
                            "VALUES (?,?,?,?,?,?,?,?,?,?,?)",
                            Par("@anl", DbParamTyp.Integer, idAnlage),
                            Par("@rang", DbParamTyp.Integer, rang),
                            Par("@bez", DbParamTyp.VarWChar,
                                string.IsNullOrEmpty(z.Bezeichner) ? null : z.Bezeichner),
                            // 0 wird NIE geschrieben - die Beziehung auf
                            // Tab_Wechselrichter.ID ist erzwungen, "kein Geraet" ist NULL.
                            Par("@wr", DbParamTyp.Integer,
                                (z.ID_Wechselrichter ?? 0) > 0 ? (object)z.ID_Wechselrichter.Value : null),
                            Par("@ger", DbParamTyp.Integer, Wert(z.Geraetenummer)),
                            Par("@mppt", DbParamTyp.Integer, Wert(z.Mppt)),
                            Par("@reihe", DbParamTyp.Integer, Wert(z.Module_Reihe)),
                            Par("@par", DbParamTyp.Integer, Wert(z.Straenge_Parallel)),
                            // NULL heisst "der Anlagenwert"; 0 ist eine GUELTIGE
                            // Ausrichtung (Sueden) und muss geschrieben werden.
                            Par("@neig", DbParamTyp.Integer, Wert(z.Neigung)),
                            Par("@azi", DbParamTyp.Integer, Wert(z.Azimut)),
                            Par("@pv", DbParamTyp.Integer,
                                (z.ID_PV ?? 0) > 0 ? (object)z.ID_PV.Value : null));
                    }
                }

                v.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (v != null) { try { v.Rollback(); } catch { } }
                Console.WriteLine("Die Strangliste der Anlage " + idAnlage +
                                  " konnte nicht gespeichert werden: " + ex.Message);
                return false;
            }
            finally
            {
                if (v != null) { try { v.Dispose(); } catch { } }
            }
        }

        // =====================================================================
        // Innenleben
        // =====================================================================

        private static void AusTabelle(DataTable dt, List<AnlageStrangModel> ziel)
        {
            if (dt == null) return;

            foreach (DataRow r in dt.Rows)
            {
                var z = new AnlageStrangModel();
                z.ID = Zahl(dt, r, "ID") ?? 0;
                z.ID_Anlage = Zahl(dt, r, AnlageStrangSchema.SPALTE_ID_ANLAGE) ?? 0;
                z.Rang = Zahl(dt, r, AnlageStrangSchema.SPALTE_RANG) ?? 0;
                z.Bezeichner = Text(dt, r, AnlageStrangSchema.SPALTE_BEZEICHNER);
                z.ID_Wechselrichter = Zahl(dt, r, AnlageStrangSchema.SPALTE_ID_WECHSELRICHTER);
                z.Geraetenummer = Zahl(dt, r, AnlageStrangSchema.SPALTE_GERAETENUMMER);
                z.Mppt = Zahl(dt, r, AnlageStrangSchema.SPALTE_MPPT);
                z.Module_Reihe = Zahl(dt, r, AnlageStrangSchema.SPALTE_MODULE_REIHE);
                z.Straenge_Parallel = Zahl(dt, r, AnlageStrangSchema.SPALTE_STRAENGE_PARALLEL);

                // Ausdruecklich mit null: NULL heisst "der Anlagenwert", 0 heisst
                // "Sueden" bzw. "flach". Der Rundweg muss beide Zustaende unterscheiden.
                z.Neigung = Zahl(dt, r, AnlageStrangSchema.SPALTE_NEIGUNG);
                z.Azimut = Zahl(dt, r, AnlageStrangSchema.SPALTE_AZIMUT);
                z.ID_PV = Zahl(dt, r, AnlageStrangSchema.SPALTE_ID_PV);

                ziel.Add(z);
            }
        }

        private static DbParam Par(string name, DbParamTyp typ, object wert)
        {
            // AUSDRUECKLICHER Spaltentyp, auch bei NULL: Aus DBNull allein leitet der
            // Provider keinen Typ ab - dieselbe Regel wie in ProjektPuffer.Par.
            return new DbParam(name, typ) { Wert = wert ?? DBNull.Value };
        }

        /// <summary>
        /// <c>int?</c> als Parameterwert: <c>null</c> bleibt <c>null</c> und wird zu
        /// <see cref="DBNull"/>. Der ausdrückliche Weg macht sichtbar, dass hier NICHT
        /// auf 0 ausgewichen wird (Muster <c>AnlagenSql.Wert</c>).
        /// </summary>
        private static object Wert(int? v)
        {
            return v.HasValue ? (object)v.Value : null;
        }

        private static int? Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return (r[spalte].ToString() ?? "").Trim();
        }
    }
}
