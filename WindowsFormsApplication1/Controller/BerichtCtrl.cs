using System;
using System.Data;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ablauf- und Persistenzsteuerung des Berichtsmoduls (Konzept Kap. 8.4).
    /// Berichtskonfiguration je Stammprojekt in der DB (Tabelle Berichtskonfiguration:
    /// ProjektID, KonfigJson, GeaendertAm) sowie Word- und Excel-Erzeugung
    /// (Dateiname, Zielordner, Kollisionsbehandlung → WordBerichtGenerator /
    /// ExcelBerichtGenerator).
    /// </summary>
    public class BerichtCtrl
    {
        /// <summary>
        /// Tabelle der Berichtskonfiguration. Der Name steht seit iU3 (Kante K7) bei
        /// <see cref="SchemaKatalog.TAB_BERICHTSKONFIGURATION"/>; hier bleibt die
        /// Weiterleitung.
        /// </summary>
        public const string TAB_KONFIG = SchemaKatalog.TAB_BERICHTSKONFIGURATION;

        /// <summary>
        /// Erzeugt den Word-Bericht (Konzept Kap. 3.1: Dateiname
        /// &lt;Projektname&gt;_Bericht_&lt;JJJJ-MM-TT&gt;.docx, kein stilles Überschreiben —
        /// bei Kollision/Sperre wird automatisch _2, _3 … angehängt).
        /// Rückgabe: Pfad der geschriebenen Datei.
        /// </summary>
        public string ErzeugeWord(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            string ordner = konfig != null && !string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                ? konfig.ZielOrdner
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(ordner);

            string basis = BereinigeDateiname(daten.Stammprojektname) + "_Bericht_" +
                           DateTime.Now.ToString("yyyy-MM-dd");
            // Vorhandene Dateien nicht still überschreiben (Konzept Kap. 10):
            // freier Name basis.docx, basis_2.docx, … wird gewählt.
            string pfad = Path.Combine(ordner, basis + ".docx");
            int n = 2;
            while (File.Exists(pfad)) { pfad = Path.Combine(ordner, basis + "_" + n + ".docx"); n++; }

            while (true)
            {
                try
                {
                    return new WordBerichtGenerator().Erzeuge(daten, konfig, pfad);
                }
                catch (IOException)
                {
                    // Datei gesperrt/nicht schreibbar → Alternativname versuchen.
                    pfad = Path.Combine(ordner, basis + "_" + n + ".docx");
                    if (++n > 20) throw;
                }
            }
        }

        /// <summary>
        /// Erzeugt die Excel-Ausgabe (Konzept Kap. 9; ClosedXML) — gleiche Namens-
        /// und Kollisionslogik wie ErzeugeWord, Endung .xlsx.
        /// Rückgabe: Pfad der geschriebenen Datei.
        /// </summary>
        public string ErzeugeExcel(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            string ordner = konfig != null && !string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                ? konfig.ZielOrdner
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Directory.CreateDirectory(ordner);

            string basis = BereinigeDateiname(daten.Stammprojektname) + "_Bericht_" +
                           DateTime.Now.ToString("yyyy-MM-dd");
            string pfad = Path.Combine(ordner, basis + ".xlsx");
            int n = 2;
            while (File.Exists(pfad)) { pfad = Path.Combine(ordner, basis + "_" + n + ".xlsx"); n++; }

            while (true)
            {
                try
                {
                    return new ExcelBerichtGenerator().Erzeuge(daten, konfig, pfad);
                }
                catch (IOException)
                {
                    pfad = Path.Combine(ordner, basis + "_" + n + ".xlsx");
                    if (++n > 20) throw;
                }
            }
        }

        private static string BereinigeDateiname(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "EPOS-Plan";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }

        /// <summary>Lädt die gespeicherte Konfiguration des Stammprojekts (sonst Standard).</summary>
        public BerichtsKonfiguration Lade(int idStammProjekt)
        {
            StelleKonfigTabelleSicher();
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT KonfigJson FROM " + TAB_KONFIG + " WHERE ProjektID = ?",
                    new DbParam("@p", idStammProjekt));
                return BerichtsKonfiguration.AusJson(o as string);
            }
            catch { return BerichtsKonfiguration.Standard(); }
        }

        /// <summary>Speichert die Konfiguration des Stammprojekts (Insert oder Update).</summary>
        public bool Speichere(int idStammProjekt, BerichtsKonfiguration konfig)
        {
            if (idStammProjekt <= 0 || konfig == null) return false;
            StelleKonfigTabelleSicher();

            string json = konfig.NachJson();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_KONFIG + " SET KonfigJson = ?, GeaendertAm = ? WHERE ProjektID = ?",
                    new DbParam("@json", json),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now },
                    new DbParam("@p", idStammProjekt));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_KONFIG, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_KONFIG + " (ID, ProjektID, KonfigJson, GeaendertAm) VALUES (?,?,?,?)",
                    new DbParam("@id", id),
                    new DbParam("@p", idStammProjekt),
                    new DbParam("@json", json),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
            }
            catch { return false; }
        }

        /// <summary>
        /// Legt die Konfigurationstabelle an, falls sie fehlt (tolerant, Muster
        /// Tab_Variante). Das JSON steht in einer TEXT-Spalte (frueher Access-Memo).
        /// </summary>
        /// <remarks>
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht; Schemaprobe statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen), SQLite-DDL statt Access-DDL
        /// (S4d vorgezogen). Der Aufbau folgt <c>sql\schema\001_grundschema.sql</c>.
        /// Die stille Fassung (<see cref="StilleDb"/>) haelt die Zusage des
        /// <c>catch</c>-Zweigs ein: eine Vorsorge zeigt keinen Dialog.
        ///
        /// Der UNIQUE-Index auf ProjektID kann in SQLite nicht in der Spaltenzeile
        /// stehen wie in Access - er wird wie im Grundschema getrennt angelegt
        /// (003_indizes_fk.sql, "UQ_BerichtKonfigProj").
        /// </remarks>
        public void StelleKonfigTabelleSicher()
        {
            try
            {
                if (StilleDb.TabelleVorhanden(TAB_KONFIG)) return;

                string ddl = "CREATE TABLE IF NOT EXISTS [" + TAB_KONFIG + "] (" +
                             "\"ID\" INTEGER PRIMARY KEY, " +
                             "\"ProjektID\" INTEGER, " +
                             "\"KonfigJson\" TEXT, " +
                             "\"GeaendertAm\" TEXT)";
                if (StilleDb.NonQuery(ddl) < 0) return;

                StilleDb.NonQuery("CREATE UNIQUE INDEX IF NOT EXISTS \"UQ_BerichtKonfigProj\" " +
                                  "ON [" + TAB_KONFIG + "] (\"ProjektID\")");
            }
            catch { /* best effort — existiert dann ggf. schon */ }
        }
    }
}
