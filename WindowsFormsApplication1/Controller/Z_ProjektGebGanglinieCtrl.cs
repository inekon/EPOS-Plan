using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class Z_ProjektGebGanglinieCtrl : Z_ProjWaermebedarfModel
    {
        /// <summary>Ergebnis der einmaligen Spaltenvorsorge (siehe <see cref="StelleKanalSpalteSicher"/>).</summary>
        private static bool? _kanalSpalteBereit;

        private List<Z_ProjWaermebedarfModel> _internalList = new List<Z_ProjWaermebedarfModel>();
        public int rows => _internalList.Count;
        public new List<Z_ProjWaermebedarfModel> items => _internalList;

        public Z_ProjektGebGanglinieCtrl()
        {
        }

        public void ReadAll(string sql)
        {
            // Abfrage über das zentrale DataRepository laden
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // Interne Liste vor dem erneuten Befüllen leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                Z_ProjWaermebedarfModel item = new Z_ProjWaermebedarfModel();

                // Sicheres Auslesen über Spaltennamen statt fehleranfälliger numerischer Indizes
                if (dt.Columns.Contains("ID_Z") && row["ID_Z"] != DBNull.Value)
                    item.m_ID_Z = Convert.ToInt32(row["ID_Z"]);

                if (dt.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value)
                    item.m_ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);

                if (dt.Columns.Contains("ID_Ganglinie") && row["ID_Ganglinie"] != DBNull.Value)
                    item.m_ID_Ganglinie = Convert.ToInt32(row["ID_Ganglinie"]);

                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();

                // Fallback, falls die Spalte in Access exakt wie die Variable heißt
                else if (dt.Columns.Contains("m_szBezeichner") && row["m_szBezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["m_szBezeichner"].ToString();

                // Kanalzuordnung (Migrationsschritt 48, F18). Doppelt tolerant:
                // fehlende Spalte (Datenbank vor der Migration) UND NULL/Leerwert
                // fallen auf Heizung zurueck - genau der Weg, den jede externe
                // Ganglinie vor diesem Schritt nahm.
                item.Kanal = KanalOderHeizung(
                    dt.Columns.Contains(SchemaKatalog.SPALTE_ZPW_KANAL)
                        ? row[SchemaKatalog.SPALTE_ZPW_KANAL]
                        : null);

                // Das Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }

        /// <summary>
        /// Tolerante Vorsorge für <c>Z_ProjektWaermebedarf.Kanal</c>
        /// (Migrationsschritt 48) unmittelbar vor dem SCHREIBEN.
        ///
        /// <para>Die Leseseite braucht sie nicht — <see cref="ReadAll"/> prüft den
        /// Spaltennamen und fällt auf Heizung zurück. Der Speicherweg dagegen schreibt
        /// die Spalte ausgeschrieben; fehlte sie, scheiterte das INSERT und mit ihm das
        /// Speichern der ganzen Zuordnung. Dasselbe Muster wie
        /// <c>KostenPositionCtrl.StelleSpaltenSicher</c> für die Schritte 19/38/45/46:
        /// <see cref="StilleDb"/> statt <c>DataRepository</c>, weil eine Vorsorge kein
        /// Bedienschritt ist und keine MessageBox zeigen darf. Einmal je Prozess.</para>
        ///
        /// <para>ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, SQLite-Spaltentyp
        /// statt Access-Typ (S4d vorgezogen). Das blinde <c>ALTER TABLE</c> mit
        /// geschlucktem Fehler wird zur VORABPROBE über die Schema-Auskunft — dieselbe
        /// Aussage, aber ohne eine Fehlermeldung deuten zu müssen. Die anschliessende
        /// Leseprobe bleibt der Nachweis.</para>
        /// </summary>
        internal static bool StelleKanalSpalteSicher()
        {
            if (_kanalSpalteBereit.HasValue) return _kanalSpalteBereit.Value;

            bool ok = false;
            try
            {
                HashSet<string> vorhanden = StilleDb.SpaltenNamen(SchemaKatalog.Z_PROJEKTWAERMEBEDARF);

                // Spalte fehlt -> anlegen. Fehlt die TABELLE (null), ist das nicht
                // Aufgabe dieser Vorsorge; die Leseprobe unten faellt dann ohnehin durch.
                if (vorhanden != null && !vorhanden.Contains(SchemaKatalog.SPALTE_ZPW_KANAL))
                {
                    StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                        SchemaKatalog.Z_PROJEKTWAERMEBEDARF, SchemaKatalog.SPALTE_ZPW_KANAL, "TEXT(50)"));
                }

                // Nachweis statt Annahme: erst diese Leseprobe belegt, dass die
                // Spalte da ist.
                ok = StilleDb.Scalar(
                        "SELECT COUNT(*) FROM [" + SchemaKatalog.Z_PROJEKTWAERMEBEDARF +
                        "] WHERE [" + SchemaKatalog.SPALTE_ZPW_KANAL + "] IS NULL") != null;
            }
            catch { ok = false; }

            _kanalSpalteBereit = ok;
            return ok;
        }

        /// <summary>
        /// Der Kanal eines gelesenen Feldes; NULL, DBNull und Leerwert ergeben
        /// <see cref="DbWerte.KANAL_HEIZUNG"/> (Vorbelegung nach F18).
        /// </summary>
        public static string KanalOderHeizung(object feld)
        {
            if (feld == null || feld == DBNull.Value) return DbWerte.KANAL_HEIZUNG;
            string wert = feld.ToString().Trim();
            return wert.Length == 0 ? DbWerte.KANAL_HEIZUNG : wert;
        }

        /// <summary>
        /// Die gespeicherten Kanäle eines Projekts, nach <c>ID_Z</c> aufgeschlüsselt.
        ///
        /// <para>Notwendig, weil der Speicherweg der Zuordnung LÖSCHEN + NEU ANLEGEN
        /// ist (<c>WizardCtrl.Del_WaermebedarfExtern</c> +
        /// <c>Add_WaermebedarfExtern</c>): Wer eine Modellliste aus einer
        /// ausgeschriebenen SELECT-Liste oder aus den ListView-Spalten der Startseite
        /// aufbaut, hat den Kanal nicht dabei und würde ihn beim nächsten Speichern
        /// still auf Heizung zurücksetzen. Diese Methode holt ihn zurück, bevor
        /// gespeichert wird.</para>
        /// </summary>
        public static Dictionary<int, string> KanaeleLesen(int idProjekt)
        {
            var map = new Dictionary<int, string>();
            if (idProjekt <= 0) return map;

            // Bewusst SELECT * : so bleibt der Weg auch auf einer Datenbank ohne
            // Migrationsschritt 48 lesbar (ReadAll prueft den Spaltennamen selbst).
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("select * from Z_ProjektWaermebedarf where ID_Projekt=" + idProjekt);

            foreach (Z_ProjWaermebedarfModel item in ctrl.items)
                if (item.m_ID_Z > 0) map[item.m_ID_Z] = KanalOderHeizung(item.Kanal);

            return map;
        }

        /// <summary>
        /// Trägt die gespeicherten Kanäle in eine bereits gefüllte Modellliste nach —
        /// Zuordnung über <c>ID_Z</c>. Zeilen ohne Treffer (neu hinzugefügte
        /// Ganglinien, <c>ID_Z = 0</c>) behalten ihren Wert.
        /// </summary>
        public static void KanaeleNachladen(int idProjekt, IEnumerable<Z_ProjWaermebedarfModel> liste)
        {
            if (liste == null) return;
            Dictionary<int, string> map = KanaeleLesen(idProjekt);
            if (map.Count == 0) return;

            foreach (Z_ProjWaermebedarfModel item in liste)
            {
                string kanal;
                if (item != null && map.TryGetValue(item.m_ID_Z, out kanal)) item.Kanal = kanal;
            }
        }
    }
}