using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Räumt VERWAISTE GERÄTEZEILEN eines Projekts weg - Zeilen in <c>Tab_WP</c>,
    /// <c>Tab_Heizkessel</c>, <c>Tab_BHKW</c>, <c>Tab_Pufferspeicher</c>, <c>Tab_PV</c>,
    /// <c>Tab_Solarkollektoren</c> und <c>Tab_Stromspeicher</c>, auf die keine Zeile in
    /// <c>Tab_Energieanlagen</c> desselben Projekts mehr zeigt.
    ///
    /// <para>
    /// <b>Warum es diese Zeilen überhaupt gibt.</b> Die Gerätetabellen sind keine
    /// Bestandslisten, sondern Ablagen für PROJEKTKOPIEN eines Katalogsatzes
    /// (Kopiersemantik, <c>KatalogRegistry</c>). Verbaut ist ausschließlich, worauf eine
    /// Zeile in <c>Tab_Energieanlagen</c> zeigt. ANGELEGT werden die Kopien an vielen
    /// Stellen (<c>CopyFromStamm</c> aus dem Schreibweg und aus den Geräte-Dialogen,
    /// <c>AnlagenEindeutigkeit.ProjektkopieAnlegen</c>, <c>ProjektDuplizierenCtrl</c>) -
    /// ENTFERNT wurden sie bis hierher nur an drei Stellen von Hand
    /// (<c>Form_Heizkessel</c>, <c>Form_BHKWEing</c>, <c>Form_SolarKollektoren</c>). Die
    /// übrigen Wege ließen sie liegen: Der Speicherweg aller Erzeuger ist Löschen +
    /// Neuanlegen der ANLAGENZEILEN (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> +
    /// <c>Add_WP_Waermeerzeuger</c>) und fasst die Gerätetabellen nicht an; das
    /// Projekt-Löschen (<c>WErzeugerCtrl.Delete</c>) ebenso wenig, und eine Beziehung mit
    /// Löschweitergabe von <c>Tab_Projekt</c> hat von den sieben Tabellen nur
    /// <c>Tab_Pufferspeicher</c>. Was einmal abgewählt, umgetauscht oder mit dem Projekt
    /// gelöscht wurde, blieb also stehen und wuchs mit.
    /// </para>
    ///
    /// <para>
    /// <b>Warum das nicht nur unschön ist.</b> Mehrere Auswertungen lesen die
    /// Gerätetabellen weiterhin über <c>WHERE ID_Projekt = ?</c> statt über die
    /// Anlagenzeilen und zählen den Altbestand mit: <c>WirtschaftlichkeitCtrl</c>
    /// summiert <c>SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?</c> und sucht den
    /// größten Kessel über <c>ORDER BY Ptherm DESC</c>,
    /// <c>WaermesenkeClass.ProjektPufferListe</c> füllt daraus die Speicherauswahl.
    /// </para>
    ///
    /// <para>
    /// <b>EINE Landkarte.</b> Welche Gerätetabelle zu welcher Verweisspalte gehört und
    /// welche Kindtabellen mitgehen, steht in
    /// <see cref="KomponentenUebernahmeCtrl.Plaene"/> - dieselbe Landkarte, die die
    /// Komponentenübernahme und die Bestandsanzeige des Berichts benutzen. Hier wird sie
    /// nur gelesen, nicht nachgebaut.
    /// </para>
    ///
    /// <para>
    /// <b>ACE-FALLE: KEINE PARAMETER IN UNTERABFRAGEN.</b> Ein <c>?</c> in der
    /// UNTERABFRAGE eines <c>DELETE</c>/<c>UPDATE</c> trifft bei ACE still 0 Zeilen, ohne
    /// einen Fehler zu melden. Deshalb wird hier durchgehend ZWEISTUFIG gearbeitet: erst
    /// die IDs parametrisiert SELECTen, dann mit einer Liste aus GANZZAHLEN löschen. Die
    /// Liste ist keine Einschleusungslücke - sie besteht ausschließlich aus
    /// <see cref="int"/>-Werten, die diese Klasse selbst aus der Datenbank gelesen hat.
    /// </para>
    ///
    /// <para>
    /// <b>Idempotent.</b> Ein zweiter Lauf findet nichts mehr und ändert nichts - genau
    /// das protokolliert der Migrationsschritt als Nachweis.
    /// </para>
    /// </summary>
    public static class GeraeteWaisen
    {
        /// <summary>Höchstens so viele IDs stehen in einer IN-Liste (Jet-Abfragelänge).</summary>
        private const int BLOCK = 200;

        /// <summary>Ergebnis eines Aufräumlaufs - Zahlen für Protokoll und Gegenmessung.</summary>
        public sealed class Bericht
        {
            /// <summary>Entfernte Gerätezeilen über alle Gewerke.</summary>
            public int Geraete;

            /// <summary>Entfernte Kindzeilen (heute nur die Kennlinien der Wärmepumpe).</summary>
            public int Kindzeilen;

            /// <summary>
            /// true, wenn ein Gewerk übersprungen werden musste (Tabelle nicht lesbar,
            /// Löschen gescheitert). Der Bestand ist dann nicht vollständig geräumt -
            /// aber auch nichts Falsches gelöscht.
            /// </summary>
            public bool Unvollstaendig;

            /// <summary>Klartextzeilen für das Migrationsprotokoll.</summary>
            public readonly List<string> Notizen = new List<string>();

            public bool EtwasGetan { get { return Geraete > 0 || Kindzeilen > 0; } }

            internal void Notiz(string t) { Notizen.Add(t); }
        }

        // =================================================================================
        // Öffentlicher Einstieg
        // =================================================================================

        /// <summary>
        /// Räumt die verwaisten Gerätezeilen EINES Projekts.
        /// </summary>
        /// <param name="idProjekt">
        /// Projekt-ID. Sie muss NICHT in <c>Tab_Projekt</c> stehen: Nach einem
        /// Projekt-Löschen bleiben genau diese Zeilen ohne Projekt zurück, und der
        /// Migrationsschritt räumt sie über dieselbe Methode.
        /// </param>
        /// <param name="conn">
        /// Offene Verbindung, die weiterbenutzt werden soll (der Migrationslauf hat
        /// eine). <c>null</c> = eine eigene öffnen und wieder schließen.
        /// </param>
        public static Bericht Aufraeumen(int idProjekt, OleDbConnection conn = null)
        {
            var b = new Bericht();
            if (idProjekt <= 0) return b;

            if (conn != null) { AufraeumenIntern(b, idProjekt, conn); return b; }

            try
            {
                using (var eigene = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    eigene.Open();
                    AufraeumenIntern(b, idProjekt, eigene);
                }
            }
            catch (Exception ex)
            {
                b.Unvollstaendig = true;
                b.Notiz("Projekt " + idProjekt + ": Der Aufräumlauf konnte die Datenbank nicht öffnen - " + Kurz(ex));
                Console.WriteLine("GeraeteWaisen.Aufraeumen fehlgeschlagen: " + ex.Message);
            }

            return b;
        }

        /// <summary>
        /// Alle Projekt-IDs, zu denen überhaupt Gerätezeilen stehen - EINSCHLIESSLICH der
        /// Projekte, die es in <c>Tab_Projekt</c> nicht mehr gibt. Nur so erreicht der
        /// Aufräumlauf auch den Rückstand gelöschter Projekte (von den sieben
        /// Gerätetabellen hängt nur <c>Tab_Pufferspeicher</c> mit Löschweitergabe an
        /// <c>Tab_Projekt</c>; die übrigen sechs behalten ihre Zeilen).
        /// </summary>
        public static List<int> ProjekteMitGeraetezeilen(OleDbConnection conn = null)
        {
            var ids = new List<int>();
            if (conn != null) { ProjekteSammeln(ids, conn); return ids; }

            try
            {
                using (var eigene = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    eigene.Open();
                    ProjekteSammeln(ids, eigene);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GeraeteWaisen.ProjekteMitGeraetezeilen fehlgeschlagen: " + ex.Message);
            }

            return ids;
        }

        /// <summary>
        /// Die verwaisten Gerätezeilen EINES Gewerks in EINEM Projekt - ohne zu löschen.
        /// Für Gegenmessungen und den Bericht.
        /// </summary>
        /// <param name="sicher">
        /// false, wenn die Verweise nicht vollständig ermittelt werden konnten. Dann ist
        /// die Rückgabe LEER: "unbekannte Verweise" heißt hier ausdrücklich "nichts
        /// löschen", nie "nichts referenziert".
        /// </param>
        public static List<int> Waisen(KomponentenUebernahmeCtrl.GewerkPlan plan, int idProjekt,
                                       OleDbConnection conn, out bool sicher)
        {
            sicher = false;
            var leer = new List<int>();
            if (plan == null || idProjekt <= 0 || conn == null) return leer;

            List<int> vorhanden = Spalte(conn,
                "SELECT [ID] FROM [" + plan.Geraetetabelle + "] WHERE [ID_Projekt] = ?",
                Par(idProjekt));
            if (vorhanden == null) return leer;
            if (vorhanden.Count == 0) { sicher = true; return leer; }

            HashSet<int> referenziert = Referenzen(plan, idProjekt, conn);
            if (referenziert == null) return leer;

            sicher = true;
            var waisen = new List<int>();
            foreach (int id in vorhanden)
                if (!referenziert.Contains(id)) waisen.Add(id);
            return waisen;
        }

        // =================================================================================
        // Innenleben
        // =================================================================================

        private static void AufraeumenIntern(Bericht b, int idProjekt, OleDbConnection conn)
        {
            foreach (KomponentenUebernahmeCtrl.GewerkPlan plan in KomponentenUebernahmeCtrl.Plaene.Values)
            {
                bool sicher;
                List<int> waisen = Waisen(plan, idProjekt, conn, out sicher);

                if (!sicher)
                {
                    b.Unvollstaendig = true;
                    b.Notiz("Projekt " + idProjekt + ", " + plan.Gewerk + ": Die Verweise ließen sich nicht " +
                            "vollständig lesen - dieses Gewerk bleibt unverändert.");
                    continue;
                }

                if (waisen.Count == 0) continue;

                // Die Kindzeilen fallen bei der Wärmepumpe auch über die Löschweitergabe
                // (Tab_WP.ID -> Tab_Kenndaten.ID_WP) weg. Sie werden trotzdem ZUERST und
                // ausdrücklich gelöscht: Nur so ist ihre Zahl im Protokoll nachweisbar,
                // und auf einer Datenbank ohne diese Beziehung bliebe das Löschen der
                // Gerätezeile sonst an ihr hängen.
                int kinder = 0;
                bool kinderOk = true;

                foreach (string kind in plan.Kindtabellen)
                {
                    int n = Loeschen(conn, kind, plan.KindFk, waisen);
                    if (n < 0) { kinderOk = false; break; }
                    kinder += n;
                }

                if (!kinderOk)
                {
                    b.Unvollstaendig = true;
                    b.Notiz("Projekt " + idProjekt + ", " + plan.Gewerk + ": Die Kindzeilen ließen sich nicht " +
                            "entfernen - die " + waisen.Count + " verwaisten Gerätezeilen bleiben stehen.");
                    continue;
                }

                int geraete = Loeschen(conn, plan.Geraetetabelle, "ID", waisen);
                if (geraete < 0)
                {
                    b.Unvollstaendig = true;
                    b.Notiz("Projekt " + idProjekt + ", " + plan.Gewerk + ": " + waisen.Count +
                            " verwaiste Gerätezeilen ließen sich nicht entfernen.");
                    continue;
                }

                b.Geraete += geraete;
                b.Kindzeilen += kinder;
                b.Notiz("Projekt " + idProjekt + ", " + plan.Gewerk + ": " + geraete + " verwaiste Zeilen aus " +
                        plan.Geraetetabelle + " entfernt" +
                        (kinder > 0 ? " (dazu " + kinder + " Kindzeilen)" : "") + ".");
            }
        }

        /// <summary>
        /// Die Geräte-IDs, auf die im Projekt noch gezeigt wird. <c>null</c> bedeutet
        /// "nicht ermittelbar" und ist AUSDRÜCKLICH nicht dasselbe wie "leer".
        ///
        /// <para>
        /// OHNE Typfilter auf <c>ID_Type</c>: Eine Anlagenzeile, die den Verweis führt,
        /// schützt die Gerätezeile - auch wenn ihr Typ nicht dazu passt. Beim Löschen ist
        /// die vorsichtigere Lesart die richtige (auf der Arbeitskopie vom 22.08.2026
        /// gibt es keine einzige typfremde Belegung, die Wahl kostet dort also nichts).
        /// </para>
        ///
        /// <para>
        /// PUFFERSPEICHER SIND DER SONDERFALL. Auf <c>Tab_Pufferspeicher</c> zeigen außer
        /// <c>ID_PUFFER</c> auch die Quellen-/Senken-Spalten FREMDER Gewerke
        /// (<see cref="KomponentenUebernahmeCtrl.PUFFER_VERWEISE"/>), die Verbundzuordnung
        /// <c>Z_AnlagePufferVerbund</c> und die Alt-Zuordnung <c>Z_ProjektPufferSp</c>, aus
        /// der die Simulation den Senkenspeicher der Wärmepumpe noch liest. Alle drei
        /// zählen als Verweis.
        /// </para>
        /// </summary>
        private static HashSet<int> Referenzen(KomponentenUebernahmeCtrl.GewerkPlan plan, int idProjekt,
                                               OleDbConnection conn)
        {
            var menge = new HashSet<int>();

            List<int> direkt = Spalte(conn,
                "SELECT [" + plan.AnlagenFk + "] FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                "WHERE [ID_Projekt] = ? AND [" + plan.AnlagenFk + "] IS NOT NULL",
                Par(idProjekt));
            if (direkt == null) return null;
            foreach (int id in direkt) menge.Add(id);

            if (!string.Equals(plan.Geraetetabelle, SchemaKatalog.TAB_PUFFERSPEICHER,
                               StringComparison.OrdinalIgnoreCase))
                return menge;

            foreach (string spalte in KomponentenUebernahmeCtrl.PUFFER_VERWEISE)
            {
                List<int> weitere = Spalte(conn,
                    "SELECT [" + spalte + "] FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                    "WHERE [ID_Projekt] = ? AND [" + spalte + "] IS NOT NULL",
                    Par(idProjekt));
                if (weitere == null) return null;
                foreach (int id in weitere) menge.Add(id);
            }

            // Beide Zuordnungstabellen OHNE Projektfilter: Der Verbund führt die
            // Projekt-ID gar nicht, und ein Verweis von außerhalb wäre erst recht ein
            // Grund, die Zeile stehen zu lassen. Fehlt die Tabelle auf einer Datenbank vor
            // Migrationsschritt 14, gilt sie als leer - jeder andere Fehler bleibt einer.
            List<int> verbund = SpalteOhneTabelle(conn,
                "SELECT [ID_Puffer] FROM [" + SchemaKatalog.Z_ANLAGEPUFFERVERBUND + "] " +
                "WHERE [ID_Puffer] IS NOT NULL");
            if (verbund == null) return null;
            foreach (int id in verbund) menge.Add(id);

            List<int> altZuordnung = SpalteOhneTabelle(conn,
                "SELECT [ID_Pufferspeicher] FROM [" + SchemaKatalog.Z_PROJEKTPUFFERSP + "] " +
                "WHERE [ID_Pufferspeicher] IS NOT NULL");
            if (altZuordnung == null) return null;
            foreach (int id in altZuordnung) menge.Add(id);

            return menge;
        }

        private static void ProjekteSammeln(List<int> ids, OleDbConnection conn)
        {
            var gesehen = new HashSet<int>();

            foreach (KomponentenUebernahmeCtrl.GewerkPlan plan in KomponentenUebernahmeCtrl.Plaene.Values)
            {
                List<int> je = Spalte(conn,
                    "SELECT DISTINCT [ID_Projekt] FROM [" + plan.Geraetetabelle + "] " +
                    "WHERE [ID_Projekt] IS NOT NULL");
                if (je == null) continue;

                foreach (int id in je)
                    if (id > 0 && gesehen.Add(id)) ids.Add(id);
            }

            ids.Sort();
        }

        // =================================================================================
        // Datenbankzugriff - dialogfrei, Fehler werden GEMELDET, nicht verschluckt
        // =================================================================================

        /// <summary>Erste Spalte einer Abfrage als Ganzzahlliste; <c>null</c> bei jedem Fehler.</summary>
        private static List<int> Spalte(OleDbConnection conn, string sql, params OleDbParameter[] ps)
        {
            try
            {
                var liste = new List<int>();
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
                    using (OleDbDataReader r = cmd.ExecuteReader())
                        while (r.Read())
                            if (!r.IsDBNull(0))
                                liste.Add(Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture));
                }
                return liste;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GeraeteWaisen: Abfrage fehlgeschlagen (" + ex.Message + "): " + sql);
                return null;
            }
        }

        /// <summary>
        /// Wie <see cref="Spalte"/>, aber eine FEHLENDE TABELLE gilt als leer. Nur für die
        /// beiden Puffer-Zuordnungstabellen: Auf einer Datenbank vor Migrationsschritt 14
        /// gibt es <c>Z_AnlagePufferVerbund</c> noch nicht, und "die Tabelle ist nicht da"
        /// heißt zweifelsfrei "sie enthält keinen Verweis". Jeder ANDERE Fehler bleibt ein
        /// Fehler und führt weiterhin zu <c>null</c>.
        /// </summary>
        private static List<int> SpalteOhneTabelle(OleDbConnection conn, string sql)
        {
            try
            {
                var liste = new List<int>();
                using (var cmd = new OleDbCommand(sql, conn))
                using (OleDbDataReader r = cmd.ExecuteReader())
                    while (r.Read())
                        if (!r.IsDBNull(0))
                            liste.Add(Convert.ToInt32(r.GetValue(0), CultureInfo.InvariantCulture));
                return liste;
            }
            catch (Exception ex)
            {
                if (IstTabelleFehlt(ex)) return new List<int>();
                Console.WriteLine("GeraeteWaisen: Abfrage fehlgeschlagen (" + ex.Message + "): " + sql);
                return null;
            }
        }

        /// <summary>
        /// "Das Datenbankmodul konnte die Eingabetabelle nicht finden" - Jet/ACE-Fehler
        /// 3078, ersatzweise am Text erkannt. <c>OleDbException.Errors</c> ist unter .NET 8
        /// bei ACE-Fehlern leer (nachgewiesen in <c>SchemaMigration.IstBereitsVorhanden</c>),
        /// tragend ist deshalb auch hier der Textvergleich.
        /// </summary>
        private static bool IstTabelleFehlt(Exception ex)
        {
            var oledb = ex as OleDbException;
            if (oledb != null)
                foreach (OleDbError e in oledb.Errors)
                    if (e.SQLState == "3078") return true;

            string m = (ex.Message ?? "").ToLowerInvariant();
            return m.Contains("cannot find the input table")
                || m.Contains("eingabetabelle");
        }

        /// <summary>
        /// Löscht die Zeilen einer Tabelle zu einer ID-Liste, blockweise. Rückgabe: Zahl
        /// der gelöschten Zeilen, -1 sobald ein Block scheitert.
        /// </summary>
        private static int Loeschen(OleDbConnection conn, string tabelle, string spalte, List<int> ids)
        {
            if (string.IsNullOrEmpty(tabelle) || string.IsNullOrEmpty(spalte)) return 0;
            if (ids == null || ids.Count == 0) return 0;

            int summe = 0;

            for (int von = 0; von < ids.Count; von += BLOCK)
            {
                string sql = "DELETE FROM [" + tabelle + "] WHERE [" + spalte + "] IN (" +
                             IdListe(ids, von, BLOCK) + ")";
                try
                {
                    using (var cmd = new OleDbCommand(sql, conn)) summe += cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GeraeteWaisen: Löschen in " + tabelle + " fehlgeschlagen: " + ex.Message);
                    return -1;
                }
            }

            return summe;
        }

        /// <summary>
        /// Ein Block der ID-Liste als Klartext. Die Werte sind <see cref="int"/> und
        /// werden mit <see cref="CultureInfo.InvariantCulture"/> geschrieben - kein
        /// Anwendertext, keine Einschleusung, kein Tausenderpunkt.
        /// </summary>
        private static string IdListe(List<int> ids, int von, int anzahl)
        {
            var sb = new StringBuilder();
            int bis = Math.Min(von + anzahl, ids.Count);

            for (int i = von; i < bis; i++)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(ids[i].ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static OleDbParameter Par(int wert)
        {
            return new OleDbParameter("@p", OleDbType.Integer) { Value = wert };
        }

        private static string Kurz(Exception ex)
        {
            if (ex == null) return "";
            string m = (ex.Message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return m.Length > 200 ? m.Substring(0, 197) + "..." : m;
        }
    }
}
