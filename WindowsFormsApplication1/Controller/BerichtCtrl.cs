using System;
using System.Data;
using System.Data.OleDb;
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
        public const string TAB_KONFIG = "Berichtskonfiguration";

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
                    new OleDbParameter("@p", idStammProjekt));
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
                    new OleDbParameter("@json", json),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now },
                    new OleDbParameter("@p", idStammProjekt));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_KONFIG, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_KONFIG + " (ID, ProjektID, KonfigJson, GeaendertAm) VALUES (?,?,?,?)",
                    new OleDbParameter("@id", id),
                    new OleDbParameter("@p", idStammProjekt),
                    new OleDbParameter("@json", json),
                    new OleDbParameter("@am", OleDbType.Date) { Value = DateTime.Now });
            }
            catch { return false; }
        }

        /// <summary>
        /// Legt die Konfigurationstabelle an, falls sie fehlt (tolerant, Muster
        /// Tab_Variante). LONGTEXT = Access-Memo, ausreichend für das Konfig-JSON.
        /// </summary>
        public void StelleKonfigTabelleSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                        new object[] { null, null, TAB_KONFIG, "TABLE" });
                    if (schema != null && schema.Rows.Count > 0) return;

                    string ddl = "CREATE TABLE " + TAB_KONFIG + " (" +
                                 "ID LONG CONSTRAINT PK_BerichtKonfig PRIMARY KEY, " +
                                 "ProjektID LONG CONSTRAINT UQ_BerichtKonfigProj UNIQUE, " +
                                 "KonfigJson LONGTEXT, " +
                                 "GeaendertAm DATETIME)";
                    using (OleDbCommand cmd = new OleDbCommand(ddl, conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { /* best effort — existiert dann ggf. schon */ }
        }
    }
}
