using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zentrale Logik für Projektvarianten (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 3.3).
    ///
    /// Eine Variante ist ein vollwertiges Kopie-Projekt (ProjektDuplizierenCtrl);
    /// die Seitentabelle Tab_Variante (ID, ID_Projekt, ID_ProjektRef, Variantenname)
    /// verknüpft die Variante (ID_Projekt) mit ihrem Stammprojekt (ID_ProjektRef).
    ///
    /// Diese Klasse bündelt die bis iU9-W0 im Altdialog „Projektvarianten" verstreute
    /// Logik, damit
    /// Formular, Menüweg ("Als Variante speichern…") und Berichtsmodul dieselbe
    /// Implementierung nutzen. Kein UI-Bezug (Meldungen laufen über Rückgabewerte).
    /// </summary>
    public class VariantenCtrl
    {
        /// <summary>
        /// Verknüpfungstabelle Stammprojekt ↔ Variante. Der Name steht seit iU3
        /// (Kante K7) bei <see cref="SchemaKatalog.TAB_VARIANTE"/>; hier bleibt die
        /// Weiterleitung.
        /// </summary>
        public const string TAB_VARIANTE = SchemaKatalog.TAB_VARIANTE;

        /// <summary>Eine Zeile der Vergleichsgruppe (Stamm oder Variante).</summary>
        public class VarianteInfo
        {
            public int IdProjekt;
            public string Projektname = "";
            public string Variantenname = "";   // leer beim Stamm
            public bool IstStamm;
        }

        // ------------------------------------------------------------- Lesen

        /// <summary>Stamm + alle Varianten des Stammprojekts (Stamm als erste Zeile).</summary>
        public List<VarianteInfo> LadeGruppe(int idStamm, string stammName)
        {
            List<VarianteInfo> gruppe = new List<VarianteInfo>();
            gruppe.Add(new VarianteInfo { IdProjekt = idStamm, Projektname = stammName ?? "", IstStamm = true });

            try
            {
                string sql = "SELECT v.ID_Projekt, v.Variantenname, p.Projektname " +
                             "FROM " + TAB_VARIANTE + " v INNER JOIN Tab_Projekt p ON v.ID_Projekt = p.ID " +
                             "WHERE v.ID_ProjektRef = ? ORDER BY v.Variantenname";
                DataTable dt = DataRepository.GetDataTable(sql, new DbParam("?", idStamm));
                foreach (DataRow r in dt.Rows)
                {
                    gruppe.Add(new VarianteInfo
                    {
                        IdProjekt = Convert.ToInt32(r["ID_Projekt"]),
                        Variantenname = r["Variantenname"]?.ToString() ?? "",
                        Projektname = r["Projektname"]?.ToString() ?? "",
                        IstStamm = false
                    });
                }
            }
            catch { /* leere Gruppe genügt dem Aufrufer als Antwort */ }

            return gruppe;
        }

        /// <summary>IDs aller Projekte, die bereits als Stamm dienen (ID_ProjektRef in Tab_Variante).</summary>
        public HashSet<int> LiesStammProjektIds()
        {
            var set = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT DISTINCT ID_ProjektRef FROM " + TAB_VARIANTE);
                foreach (DataRow r in dt.Rows)
                    if (r[0] != DBNull.Value) set.Add(Convert.ToInt32(r[0]));
            }
            catch { }
            return set;
        }

        /// <summary>Liefert ID_ProjektRef, wenn idProjekt eine Variante ist, sonst -1.</summary>
        public int StammRefDerVariante(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID_ProjektRef FROM " + TAB_VARIANTE + " WHERE ID_Projekt = ?",
                    new DbParam("@proj", idProjekt));
                if (o != null) return Convert.ToInt32(o);
            }
            catch { }
            return -1;
        }

        public bool ProjektnameExistiert(string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?",
                new DbParam("@name", name));
            return o != null && Convert.ToInt32(o) > 0;
        }

        // ------------------------------------------------------------- Anlegen

        /// <summary>
        /// Legt aus einem Stammprojekt eine Variante an: Projekt duplizieren,
        /// Tab_Variante-Verknüpfung eintragen, Energieträger-Einstellungen kopieren.
        /// Rückgabe: neue Projekt-ID der Variante, -1 bei Fehler (fehler beschreibt die Ursache).
        /// </summary>
        public int AnlegenAusStamm(int idStamm, string stammName, string bezeichner, out string fehler)
        {
            fehler = null;
            bezeichner = (bezeichner ?? "").Trim();
            if (idStamm <= 0 || string.IsNullOrWhiteSpace(stammName)) { fehler = "Kein Stammprojekt angegeben."; return -1; }
            if (bezeichner.Length == 0) { fehler = "Bitte einen Bezeichner für die Variante eingeben."; return -1; }

            StelleVariantentabelleSicher();

            // B5-Selbstheilung für Bestandsdatenbanken: verwaiste Zeilen räumen, bevor
            // eine neue Projekt-ID auf eine verwaiste ID_Projekt fällt (UQ_VarProj).
            EntferneWaisen();

            // Eindeutigen Projektnamen bilden: "<Stamm> - <Bezeichner>" (ggf. mit Zähler).
            string basisName = stammName + " - " + bezeichner;
            string neuerName = basisName;
            int n = 2;
            while (ProjektnameExistiert(neuerName)) { neuerName = basisName + " (" + n + ")"; n++; }

            try
            {
                int neueId = new ProjektDuplizierenCtrl().Duplizieren(stammName, neuerName);
                if (neueId <= 0) { fehler = "Variante konnte nicht angelegt werden (Duplizieren fehlgeschlagen)."; return -1; }

                int vid = DataRepository.GetMaxID(TAB_VARIANTE, "ID") + 1;
                string ins = "INSERT INTO " + TAB_VARIANTE + " (ID, ID_Projekt, ID_ProjektRef, Variantenname) VALUES (?, ?, ?, ?)";
                DataRepository.ExecuteSQL(ins,
                    new DbParam("@id", vid),
                    new DbParam("@proj", neueId),
                    new DbParam("@ref", idStamm),
                    new DbParam("@name", bezeichner));

                KopiereEnergieEinstellungen(idStamm, neueId);
                return neueId;
            }
            catch (Exception ex)
            {
                fehler = "Fehler beim Anlegen: " + ex.Message;
                return -1;
            }
        }

        /// <summary>
        /// Kopiert projektbezogene Energieträger-Einstellungen (energy_project_settings)
        /// und die Preishistorie (energy_price) vom Stamm auf die Variante. Best effort:
        /// fehlen Kostenmodul/Tabellen, läuft der Anlegevorgang trotzdem weiter.
        ///
        /// <para>
        /// NUR NOCH FALLBACK: Die generische Projektkopie (ProjektDuplizierenCtrl)
        /// kopiert beide Tabellen über ihre ID_Projekt-Spalte bereits mit. Ein zweiter
        /// Durchgang verletzte den eindeutigen Index unq_price_date (carrier_id,
        /// valid_from, ID_Projekt) — Dialog „Datenbankfehler: … duplicate values …"
        /// beim Anlegen jeder Variante mit Preiszeilen — und hinterließ in
        /// energy_project_settings eine zweite Zeile je Energieträger (dort verhindert
        /// kein Index die Dublette). Kopiert wird deshalb je Tabelle nur noch, wenn sie
        /// für das ZIELprojekt noch leer ist — der Fall älterer Datenbanken, deren
        /// Schema der generische Kopierlauf nicht abdeckt.
        /// </para>
        /// </summary>
        public void KopiereEnergieEinstellungen(int vonProjekt, int nachProjekt)
        {
            try
            {
                if (!HatProjektZeilen("energy_project_settings", nachProjekt))
                {
                    string sqlSettings =
                        "INSERT INTO energy_project_settings " +
                        "(ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, " +
                        " custom_price_base, ID_Umrechnung, co2, so2, nox) " +
                        "SELECT ?, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, " +
                        " custom_price_base, ID_Umrechnung, co2, so2, nox " +
                        "FROM energy_project_settings WHERE ID_Projekt = ?";
                    DataRepository.ExecuteSQL(sqlSettings,
                        new DbParam("@neu", nachProjekt),
                        new DbParam("@von", vonProjekt));
                }

                if (!HatProjektZeilen("energy_price", nachProjekt))
                {
                    string sqlPrices =
                        "INSERT INTO energy_price " +
                        "(carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis) " +
                        "SELECT carrier_id, ?, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis " +
                        "FROM energy_price WHERE id_projekt = ?";
                    DataRepository.ExecuteSQL(sqlPrices,
                        new DbParam("@neu", nachProjekt),
                        new DbParam("@von", vonProjekt));
                }
            }
            catch { /* Hinweis obliegt dem Aufrufer; das Anlegen selbst gilt als gelungen */ }
        }

        /// <summary>
        /// Hat die Tabelle bereits Zeilen zum Projekt? Stumm über <see cref="StilleDb"/>:
        /// Fehlt Tabelle oder Spalte (ältere Datenbank), antwortet sie mit null → false,
        /// und der Kopier-Fallback greift wie bisher.
        /// </summary>
        private static bool HatProjektZeilen(string tabelle, int idProjekt)
        {
            return StilleDb.Zahl(StilleDb.Scalar(
                "SELECT COUNT(*) FROM [" + tabelle + "] WHERE [ID_Projekt] = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt))) > 0;
        }

        // ------------------------------------------------------------- Löschen

        /// <summary>
        /// Löscht eine Variante: Verknüpfung, Energieanlagen, Projekt (Detailtabellen
        /// fallen per Löschweitergabe mit weg). Kein Stammprojekt-Löschen über diesen Weg.
        ///
        /// <para><b>Ein Name, der MEHRERE Projekte trifft, wird nicht still mitgelöscht</b>
        /// (iU9-W15a, Entscheid O-4 vom 04.09.2026 — dieselbe Vorprüfung und dieselbe
        /// Rückfrage wie beim Projektlöschen, Entscheid O-3). Der letzte der drei
        /// Schritte ist <c>ProjektCtrl.Delete(projektname)</c> und läuft damit über den
        /// NAMEN; die beiden Schritte davor arbeiten über die Id. Trägt eine Datenbank
        /// zwei Projekte desselben Namens — regulär unmöglich, <c>Tab_Projekt</c> hat
        /// seit der SQLite-Migration den eindeutigen Index <c>Projektname</c>, ein
        /// Altbestand ohne ihn kann es —, dann nähme der letzte Schritt beide mit.
        /// Deshalb meldet der Weg <see cref="LoeschStand.Mehrdeutig"/> mit der Anzahl und
        /// fasst NICHTS an. Erst mit <paramref name="mehrdeutigZugelassen"/> läuft er
        /// bitgleich wie zuvor.</para>
        ///
        /// <para><b>Warum <see cref="LoeschBefund"/> statt <c>bool</c> + <c>out</c>.</b>
        /// Der Aufrufer muss „mehrdeutig" von „fehlgeschlagen" unterscheiden können und
        /// braucht die Anzahl für die Rückfrage — beides trägt ein Wahrheitswert nicht.
        /// Es ist derselbe Befund, den <c>ProjektCtrl.LoeschenMitVorarbeiten</c> liefert;
        /// eine zweite Bauform für denselben Zweck gäbe es sonst ohne Not.</para>
        /// </summary>
        /// <param name="idProjekt">Id der zu löschenden Variante.</param>
        /// <param name="projektname">Name des zugehörigen Projekts — der Schlüssel des letzten Schritts.</param>
        /// <param name="mehrdeutigZugelassen">
        /// <c>true</c> = der Anwender hat dem Löschen ALLER Projekte dieses Namens
        /// ausdrücklich zugestimmt. Vorgabe <c>false</c>: mehrdeutig heißt abbrechen.
        /// </param>
        public LoeschBefund LoescheVariante(int idProjekt, string projektname,
                                            bool mehrdeutigZugelassen = false)
        {
            if (StammRefDerVariante(idProjekt) <= 0)
                return new LoeschBefund(LoeschStand.KeineVariante, projektname ?? "",
                    "Das Projekt ist keine Variante (Stammprojekte werden hier nicht gelöscht).", 0);

            // Entscheid O-4: VOR dem ersten Schritt zählen. Bis hierher ist nichts
            // angefasst — der Abbruch lässt die Datenbank unberührt.
            int gleichnamige = ProjektCtrl.AnzahlGleicherNamen(projektname);
            if (gleichnamige > 1 && !mehrdeutigZugelassen)
                return new LoeschBefund(LoeschStand.Mehrdeutig, projektname, "", gleichnamige);

            try
            {
                DataRepository.ExecuteSQL("DELETE FROM " + TAB_VARIANTE + " WHERE ID_Projekt = ?",
                    new DbParam("@proj", idProjekt));

                WErzeugerCtrl werz = new WErzeugerCtrl { ID_Projekt = idProjekt };
                werz.Delete();

                new ProjektCtrl().Delete(projektname);
                return new LoeschBefund(LoeschStand.Geloescht, projektname ?? "", "", gleichnamige);
            }
            catch (Exception ex)
            {
                return new LoeschBefund(LoeschStand.Loeschfehler, projektname ?? "",
                                        "Fehler beim Löschen: " + ex.Message, gleichnamige);
            }
        }

        /// <summary>
        /// Entfernt Waisen aus Tab_Variante (Einträge, deren Projekt oder deren Stamm
        /// nicht mehr existiert — die Tabelle hat keine Löschweitergabe, Befund B5).
        /// Rückgabe: Anzahl entfernter Einträge.
        /// </summary>
        public int EntferneWaisen()
        {
            int entfernt = 0;
            try
            {
                // Jet verlangt bei zwei LEFT JOINs die Klammerung im FROM — ohne sie
                // bricht die Abfrage mit „Syntax error (missing operator)" ab.
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT v.ID FROM (" + TAB_VARIANTE + " v " +
                    "LEFT JOIN Tab_Projekt p ON v.ID_Projekt = p.ID) " +
                    "LEFT JOIN Tab_Projekt s ON v.ID_ProjektRef = s.ID " +
                    "WHERE p.ID IS NULL OR s.ID IS NULL");
                foreach (DataRow r in dt.Rows)
                {
                    DataRepository.ExecuteSQL("DELETE FROM " + TAB_VARIANTE + " WHERE ID = ?",
                        new DbParam("@id", Convert.ToInt32(r["ID"])));
                    entfernt++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Tab_Variante-Waisen konnten nicht entfernt werden: " + ex.Message);
            }
            return entfernt;
        }

        // ------------------------------------------------------------- Schema

        /// <summary>Legt Tab_Variante an, falls sie noch nicht existiert (tolerant).</summary>
        /// <remarks>
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht; Schemaprobe statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen), SQLite-DDL nach dem Muster von
        /// <c>sql\schema\001_grundschema.sql</c> (S4d vorgezogen). Der UNIQUE-Index auf
        /// ID_Projekt steht in SQLite nicht in der Spaltenzeile, sondern getrennt
        /// (003_indizes_fk.sql, "UQ_VarProj"). Still über <see cref="StilleDb"/>, damit
        /// die Zusage des <c>catch</c>-Zweigs (kein Dialog) erhalten bleibt.
        /// </remarks>
        public void StelleVariantentabelleSicher()
        {
            try
            {
                if (StilleDb.TabelleVorhanden(TAB_VARIANTE)) return;

                string ddl = "CREATE TABLE IF NOT EXISTS [" + TAB_VARIANTE + "] (" +
                             "\"ID\" INTEGER PRIMARY KEY, " +
                             "\"ID_Projekt\" INTEGER, " +
                             "\"ID_ProjektRef\" INTEGER, " +
                             "\"Variantenname\" TEXT CHECK (length(\"Variantenname\") <= 255))";
                if (StilleDb.NonQuery(ddl) < 0) return;

                StilleDb.NonQuery("CREATE UNIQUE INDEX IF NOT EXISTS \"UQ_VarProj\" " +
                                  "ON [" + TAB_VARIANTE + "] (\"ID_Projekt\")");
            }
            catch { /* best effort — existiert dann ggf. schon */ }
        }
    }
}
