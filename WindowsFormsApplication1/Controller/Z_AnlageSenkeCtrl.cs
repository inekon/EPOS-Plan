using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zugriff auf <c>Z_AnlageSenke</c> — die GEORDNETE SENKENLISTE je
    /// Wärmeerzeuger-Anlage (Paket S1, Konzept Brauchwasser/Heizung/Pufferspeicher
    /// § 5.1; angelegt von <c>SchemaMigration</c> Schritt 50).
    ///
    /// <para>
    /// <b>Zwei Lesewege, ein Grund.</b> <see cref="LesenJeAnlage"/> bedient den
    /// Dialog (eine Anlage, ihre Senken in Rangfolge). <see cref="LesenJeProjekt"/>
    /// bedient die Engine: Sie baut ihre Senkenketten EINMAL je Lauf auf, und ein
    /// Aufruf je Anlage wäre bei fünf Erzeugern fünf Rundreisen zur Datenbank für
    /// dieselbe Information. Der Projektbezug läuft über den Verbund zu
    /// <c>Tab_Energieanlagen</c> — <c>Z_AnlageSenke</c> führt bewusst KEIN
    /// <c>ID_Projekt</c>: Die Anlage weiß, zu welchem Projekt sie gehört, und eine
    /// zweite Wahrheit darüber könnte auseinanderlaufen.
    /// </para>
    ///
    /// <para>
    /// <b>Der Schreibweg ist Löschen + Neuanlegen je Anlage</b>
    /// (<see cref="SchreibenJeAnlage"/>). Das ist hier nicht die Bequemlichkeit, die
    /// es im Erzeuger-Speicherweg ist, sondern die einzige Form, die zur Sache passt:
    /// Der Dialog liefert eine LISTE in einer bestimmten Reihenfolge; welche Zeile
    /// darin die frühere Zeile 3 ist, ist keine sinnvolle Frage. Die Ränge werden
    /// beim Schreiben lückenlos ab 1 neu vergeben — was der Dialog an Rängen mitgibt,
    /// zählt nur als Reihenfolge, nicht als Wert.
    /// </para>
    ///
    /// <para>
    /// <b>Über <see cref="DataRepository"/> mit <c>?</c>-Parametern</b>, nicht über
    /// <c>RecordSet</c> — Vorgabe für neuen Code. Ein Sonderfall bleibt: Das
    /// <c>INSERT</c> und das <c>DELETE</c> einer Anlage laufen auf EINER Verbindung
    /// in EINER Transaktion (<c>DataRepository.BeginTransaction</c>), damit ein
    /// gescheitertes Insert nicht eine Anlage ohne jede Senke hinterlässt — und Rang 1
    /// ist Pflicht.
    /// </para>
    ///
    /// <para>
    /// <b>ACE-FALLE: KEINE PARAMETER IN UNTERABFRAGEN.</b> Ein <c>?</c> in der
    /// UNTERABFRAGE trifft bei ACE still 0 Zeilen, ohne einen Fehler zu melden. Der
    /// Projekt-Leseweg benutzt deshalb einen JOIN, keine Unterabfrage.
    /// </para>
    /// </summary>
    public class Z_AnlageSenkeCtrl
    {
        public const string TABLE = "Z_AnlageSenke";

        // =====================================================================
        // Schemaprobe
        // =====================================================================

        /// <summary>
        /// Gemerktes Ergebnis der Tabellenprobe. <c>null</c> = noch nicht geprüft.
        ///
        /// <para>
        /// GECACHT, weil jeder Leser fragt: Der Senkendialog fragt beim Öffnen, die
        /// Engine je Anlage und Lauf, die Löschwege bei jedem Puffer. Die Antwort kann
        /// sich innerhalb einer Sitzung nicht ändern — die Migration läuft beim
        /// Programmstart, lange vor dem ersten Aufruf hier.
        /// </para>
        /// </summary>
        private static bool? m_TabelleVorhanden;

        /// <summary>
        /// Gibt es die Senkenliste in dieser Datenbank? <c>false</c> heißt „Schritt 50
        /// ist hier noch nicht gelaufen" — dann fällt jeder Leser auf die Altspalten
        /// <c>WS_Ziel</c>/<c>WS_Ziel2</c> zurück, also auf das Bestandsverhalten.
        ///
        /// <para>
        /// Der Name lautet aus einem Grund <c>SpalteVorhanden</c> und nicht
        /// <c>TabelleVorhanden</c>: Er hält sich an das Muster der übrigen
        /// Schemaproben dieses Vorhabens (<c>PufferSpCtrl</c>,
        /// <c>KostenPositionCtrl.StelleSpaltenSicher</c>), damit die Aufrufstellen
        /// gleich aussehen.
        /// </para>
        ///
        /// <para>
        /// <c>COUNT(*)</c> statt einer Schemaabfrage: Eine leere Tabelle liefert 0,
        /// eine FEHLENDE Tabelle einen Fehler und damit <c>null</c> — der Unterschied,
        /// auf den es ankommt.
        /// </para>
        /// </summary>
        public static bool SpalteVorhanden()
        {
            if (m_TabelleVorhanden.HasValue) return m_TabelleVorhanden.Value;

            bool da = StilleDb.Scalar("SELECT COUNT(*) FROM [" + TABLE + "]") != null;
            m_TabelleVorhanden = da;

            if (!da)
                Console.WriteLine("Z_AnlageSenkeCtrl: Die Senkenliste " + TABLE +
                                  " fehlt (Migrationsschritt 50 noch nicht gelaufen) - " +
                                  "es gelten die Altspalten WS_Ziel/WS_Ziel2.");

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
        /// Alle Senkenzeilen EINES PROJEKTS, sortiert nach (<c>ID_Anlage</c>,
        /// <c>Rang</c>); nie <c>null</c>. Der Projektbezug läuft über den JOIN auf
        /// <c>Tab_Energieanlagen</c> (siehe Klassenkopf).
        /// </summary>
        public List<Z_AnlageSenkeModel> LesenJeProjekt(int idProjekt)
        {
            List<Z_AnlageSenkeModel> liste = new List<Z_AnlageSenkeModel>();
            if (idProjekt <= 0 || !SpalteVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT s.ID, s.ID_Anlage, s.Rang, s.Ziel, s.Bedarfsart, s.ID_Puffer, " +
                "       s.Ladeprio, s.Ladeprio_PV, s.Ladegrenze, s.Anschlusshoehe " +
                "FROM [" + TABLE + "] s " +
                "INNER JOIN [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] a ON a.ID = s.ID_Anlage " +
                "WHERE a.ID_Projekt = ? " +
                "ORDER BY s.ID_Anlage, s.Rang",
                new OleDbParameter("@proj", OleDbType.Integer) { Value = idProjekt });

            AusTabelle(dt, liste);
            return liste;
        }

        /// <summary>
        /// Die Senken EINER Anlage in Rangfolge; nie <c>null</c>.
        ///
        /// <para>
        /// Eine LEERE Liste ist ein gültiges Ergebnis und bedeutet „diese Anlage hat
        /// keine Senkenzeile". Der Aufrufer behandelt das wie im Bestand: Es gilt
        /// <c>Heizkreis</c>/<c>Beides</c> mit Protokollwarnung (§ 5.1). Diese Klasse
        /// erfindet dafür KEINE Zeile — sonst wüsste der Aufrufer nicht mehr, ob er
        /// eine gespeicherte Konfiguration oder eine Notannahme in der Hand hält.
        /// </para>
        /// </summary>
        public List<Z_AnlageSenkeModel> LesenJeAnlage(int idAnlage)
        {
            List<Z_AnlageSenkeModel> liste = new List<Z_AnlageSenkeModel>();
            if (idAnlage <= 0 || !SpalteVorhanden()) return liste;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, " +
                "       Ladeprio, Ladeprio_PV, Ladegrenze, Anschlusshoehe " +
                "FROM [" + TABLE + "] WHERE ID_Anlage = ? ORDER BY Rang",
                new OleDbParameter("@anl", OleDbType.Integer) { Value = idAnlage });

            AusTabelle(dt, liste);
            return liste;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Ersetzt die Senkenliste EINER Anlage vollständig: erst alle vorhandenen
        /// Zeilen dieser Anlage löschen, dann <paramref name="zeilen"/> in der
        /// übergebenen Reihenfolge neu anlegen. Rückgabe <c>false</c>, wenn nichts
        /// geschrieben wurde — dann steht auch nichts halb Geschriebenes in der
        /// Datenbank.
        ///
        /// <para>
        /// <b>Die Ränge werden neu vergeben</b>, lückenlos ab 1 in Listenreihenfolge.
        /// Was der Aufrufer in <c>Rang</c> mitgibt, wird ignoriert: Nach dem
        /// Umsortieren im Dialog wäre jede andere Regel eine zweite Wahrheit über die
        /// Reihenfolge, und Lücken oder Dubletten machten die Ladephasen je Rang
        /// (§ 5.2) mehrdeutig.
        /// </para>
        ///
        /// <para>
        /// <b>Eine Transaktion</b>, so weit ACE sie trägt: Löschen und Anlegen laufen
        /// auf EINER Verbindung. Scheitert ein Insert, wird zurückgerollt — sonst
        /// stünde die Anlage ohne jede Senke da, und Rang 1 ist Pflicht. Ein
        /// Rollback-Fehler wird gemeldet, nicht geworfen: Der Aufrufer erfährt das
        /// Scheitern ohnehin am Rückgabewert.
        /// </para>
        ///
        /// <para>
        /// Eine LEERE Liste ist zulässig und löscht die Senken der Anlage — der Weg,
        /// den <c>WErzeugerCtrl</c> beim Entfernen einer Anlage braucht. Die
        /// Rang-1-Pflicht ist eine Regel des DIALOGS (§ 5.1), nicht der Ablage.
        /// </para>
        /// </summary>
        public bool SchreibenJeAnlage(int idAnlage, List<Z_AnlageSenkeModel> zeilen)
        {
            if (idAnlage <= 0 || !SpalteVorhanden()) return false;

            OleDbConnection conn = null;
            OleDbTransaction trans = null;

            try
            {
                var tx = DataRepository.BeginTransaction();
                conn = tx.Item1;
                trans = tx.Item2;

                using (OleDbCommand del = new OleDbCommand(
                    "DELETE FROM [" + TABLE + "] WHERE ID_Anlage = ?", conn, trans))
                {
                    del.Parameters.Add(new OleDbParameter("@anl", OleDbType.Integer) { Value = idAnlage });
                    del.ExecuteNonQuery();
                }

                int rang = 0;
                if (zeilen != null)
                {
                    foreach (Z_AnlageSenkeModel z in zeilen)
                    {
                        if (z == null) continue;
                        rang++;

                        using (OleDbCommand ins = new OleDbCommand(
                            "INSERT INTO [" + TABLE + "] " +
                            "(ID_Anlage, Rang, Ziel, Bedarfsart, ID_Puffer, Ladeprio, " +
                            " Ladeprio_PV, Ladegrenze, Anschlusshoehe) " +
                            "VALUES (?,?,?,?,?,?,?,?,?)", conn, trans))
                        {
                            // Reihenfolge der Parameter = Reihenfolge der Spalten:
                            // OleDb bindet nach POSITION, nicht nach Namen.
                            ins.Parameters.Add(Par("@anl", OleDbType.Integer, idAnlage));
                            ins.Parameters.Add(Par("@rang", OleDbType.Integer, rang));
                            ins.Parameters.Add(Par("@ziel", OleDbType.VarWChar,
                                string.IsNullOrEmpty(z.Ziel) ? DbWerte.WS_ZIEL_HEIZKREIS : z.Ziel));
                            ins.Parameters.Add(Par("@art", OleDbType.VarWChar,
                                string.IsNullOrEmpty(z.Bedarfsart) ? DbWerte.WS_TYP_BEIDES : z.Bedarfsart));
                            // 0 wird NIE geschrieben - die Beziehung auf
                            // Tab_Pufferspeicher.ID ist erzwungen, "kein Puffer" ist NULL.
                            ins.Parameters.Add(Par("@puf", OleDbType.Integer,
                                z.ID_Puffer > 0 ? (object)z.ID_Puffer : null));
                            ins.Parameters.Add(Par("@prio", OleDbType.Integer, z.Ladeprio));
                            ins.Parameters.Add(Par("@prioPv", OleDbType.Integer, z.Ladeprio_PV));
                            ins.Parameters.Add(Par("@grenze", OleDbType.Double, z.Ladegrenze));
                            // -1 heisst "nicht gesetzt" -> NULL; 0 ist eine GUELTIGE
                            // Hoehe (ganz unten) und muss geschrieben werden.
                            ins.Parameters.Add(Par("@hoehe", OleDbType.Double,
                                z.Anschlusshoehe >= 0 ? (object)z.Anschlusshoehe : null));

                            ins.ExecuteNonQuery();
                        }
                    }
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (trans != null) { try { trans.Rollback(); } catch { } }
                Console.WriteLine("Die Senkenliste der Anlage " + idAnlage +
                                  " konnte nicht gespeichert werden: " + ex.Message);
                return false;
            }
            finally
            {
                if (trans != null) { try { trans.Dispose(); } catch { } }
                if (conn != null) { try { conn.Close(); } catch { } try { conn.Dispose(); } catch { } }
            }
        }

        // =====================================================================
        // Innenleben
        // =====================================================================

        private static void AusTabelle(DataTable dt, List<Z_AnlageSenkeModel> ziel)
        {
            if (dt == null) return;

            foreach (DataRow r in dt.Rows)
            {
                Z_AnlageSenkeModel z = new Z_AnlageSenkeModel();
                z.ID = Zahl(dt, r, "ID");
                z.ID_Anlage = Zahl(dt, r, "ID_Anlage");
                z.Rang = Zahl(dt, r, "Rang");

                string ziel1 = Text(dt, r, "Ziel");
                if (ziel1.Length > 0) z.Ziel = ziel1;

                string art = Text(dt, r, "Bedarfsart");
                if (art.Length > 0) z.Bedarfsart = art;

                z.ID_Puffer = Zahl(dt, r, "ID_Puffer");
                z.Ladeprio = Zahl(dt, r, "Ladeprio");
                z.Ladeprio_PV = Zahl(dt, r, "Ladeprio_PV");
                z.Ladegrenze = Kommazahl(dt, r, "Ladegrenze", 0);

                // NULL bleibt -1 ("nicht gesetzt"), 0 bleibt 0 (ganz unten).
                z.Anschlusshoehe = Kommazahl(dt, r, "Anschlusshoehe", -1);

                ziel.Add(z);
            }
        }

        private static OleDbParameter Par(string name, OleDbType typ, object wert)
        {
            // AUSDRUECKLICHER Spaltentyp, auch bei NULL: Aus DBNull allein leitet der
            // Provider keinen Typ ab - dieselbe Regel wie in ProjektPuffer.Par.
            return new OleDbParameter(name, typ) { Value = wert ?? DBNull.Value };
        }

        private static int Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture);
        }

        private static double Kommazahl(DataTable dt, DataRow r, string spalte, double leerwert)
        {
            if (!dt.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return leerwert;
            return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return (r[spalte].ToString() ?? "").Trim();
        }
    }
}
