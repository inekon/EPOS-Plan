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
        /// Der ZIELNAME einer neuen Variante: „&lt;Stamm&gt; - &lt;Bezeichner&gt;", bei
        /// Kollision mit Zähler („ (2)", „ (3)" …).
        ///
        /// <para><b>Warum die Regel eine eigene Methode ist</b> (Auftrag #237): Der Dialog
        /// „Als Variante speichern" zeigt den Namen seit dem Anwenderwunsch vom 12.09.2026
        /// LIVE an, während der Anwender tippt. Er darf ihn nicht selbst zusammensetzen —
        /// eine zweite Fassung derselben Regel liefe über kurz oder lang auseinander
        /// (der Zähler ist es, der beim Nachbauen vergessen wird). Er fragt deshalb
        /// dieselbe Methode, die <see cref="AnlegenAusStamm(int, string, string, int, out string)"/>
        /// gleich darauf anwendet.</para>
        /// </summary>
        public string Zielname(string stammName, string bezeichner)
        {
            string basisName = (stammName ?? "") + " - " + (bezeichner ?? "").Trim();
            string neuerName = basisName;
            int n = 2;
            while (ProjektnameExistiert(neuerName)) { neuerName = basisName + " (" + n + ")"; n++; }
            return neuerName;
        }

        /// <summary>
        /// Legt aus einem Stammprojekt eine Variante an: Projekt duplizieren,
        /// Tab_Variante-Verknüpfung eintragen, Energieträger-Einstellungen kopieren.
        /// Rückgabe: neue Projekt-ID der Variante, -1 bei Fehler (fehler beschreibt die Ursache).
        /// </summary>
        public int AnlegenAusStamm(int idStamm, string stammName, string bezeichner, out string fehler)
        {
            return AnlegenAusStamm(idStamm, stammName, bezeichner, idStamm, out fehler);
        }

        /// <summary>
        /// Dieselbe Anlage, der INHALT aber aus einem beliebigen QUELLPROJEKT
        /// (Anwenderwunsch 12.09.2026, Auftrag #237: „Ermögliche eine Variante aus einem
        /// bestehenden Projekt anzulegen").
        ///
        /// <para><b>Zwei Projekte, zwei Rollen.</b> Der STAMM
        /// (<paramref name="idStamm"/>/<paramref name="stammName"/>) gibt den Namen und
        /// die Gruppenzugehörigkeit: Der neue Projektname bleibt
        /// „&lt;Stamm&gt; - &lt;Bezeichner&gt;", und <c>Tab_Variante.ID_ProjektRef</c> zeigt
        /// auf ihn — sonst wäre die Vergleichsgruppe der Wirtschaftlichkeit keine Gruppe
        /// mehr. Die QUELLE (<paramref name="idQuelle"/>) gibt den INHALT: Sie wird
        /// tiefkopiert, und auch die Energieträger-Einstellungen kommen aus ihr, damit die
        /// Variante fachlich das gewählte Projekt IST.</para>
        ///
        /// <para><b>Der bisherige Weg ist der Sonderfall Quelle = Stamm</b> — dann läuft
        /// hier Zeile für Zeile dasselbe wie vorher, einschließlich des Namens, unter dem
        /// dupliziert wird (<paramref name="stammName"/> selbst, nicht ein zweites Mal
        /// aus der Datenbank gelesen).</para>
        /// </summary>
        /// <param name="idQuelle">Das zu kopierende Projekt; <c>&lt;= 0</c> oder gleich
        /// <paramref name="idStamm"/> heißt: der Stamm selbst.</param>
        public int AnlegenAusStamm(int idStamm, string stammName, string bezeichner,
                                   int idQuelle, out string fehler)
        {
            fehler = null;
            bezeichner = (bezeichner ?? "").Trim();
            if (idStamm <= 0 || string.IsNullOrWhiteSpace(stammName)) { fehler = "Kein Stammprojekt angegeben."; return -1; }
            if (bezeichner.Length == 0) { fehler = "Bitte einen Bezeichner für die Variante eingeben."; return -1; }

            // Die Quelle: der Stamm, solange keine andere genannt ist. Ihr NAME ist der
            // Schlüssel des Kopierlaufs (ProjektDuplizierenCtrl arbeitet über den Namen,
            // Projektnamen sind eindeutig — W15a-O-3).
            bool eigeneQuelle = idQuelle > 0 && idQuelle != idStamm;
            string quellName = eigeneQuelle ? StartseiteCtrl.Projektname(idQuelle) : stammName;
            if (string.IsNullOrWhiteSpace(quellName)) { fehler = "Das Quellprojekt wurde nicht gefunden."; return -1; }
            int idInhalt = eigeneQuelle ? idQuelle : idStamm;

            StelleVariantentabelleSicher();

            // B5-Selbstheilung für Bestandsdatenbanken: verwaiste Zeilen räumen, bevor
            // eine neue Projekt-ID auf eine verwaiste ID_Projekt fällt (UQ_VarProj).
            EntferneWaisen();

            // Eindeutigen Projektnamen bilden: "<Stamm> - <Bezeichner>" (ggf. mit Zähler).
            string neuerName = Zielname(stammName, bezeichner);

            try
            {
                int neueId = new ProjektDuplizierenCtrl().Duplizieren(quellName, neuerName);
                if (neueId <= 0) { fehler = "Variante konnte nicht angelegt werden (Duplizieren fehlgeschlagen)."; return -1; }

                int vid = DataRepository.GetMaxID(TAB_VARIANTE, "ID") + 1;
                string ins = "INSERT INTO " + TAB_VARIANTE + " (ID, ID_Projekt, ID_ProjektRef, Variantenname) VALUES (?, ?, ?, ?)";
                DataRepository.ExecuteSQL(ins,
                    new DbParam("@id", vid),
                    new DbParam("@proj", neueId),
                    new DbParam("@ref", idStamm),
                    new DbParam("@name", bezeichner));

                KopiereEnergieEinstellungen(idInhalt, neueId);
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
        // ------------------------------------------------------------- Umbenennen

        /// <summary>
        /// Benennt eine VARIANTE um (Anwenderwunsch 08.09.2026, Kopfband der Startseite und
        /// Übersicht): neuer Bezeichner in <c>Tab_Variante</c>, neuer Projektname
        /// „&lt;Stamm&gt; - &lt;Bezeichner&gt;" in <c>Tab_Projekt</c> (eindeutig, ggf. mit
        /// Zähler wie beim Anlegen), und der Zeiger des geöffneten Projekts
        /// (<c>Tab_Applikation.Projektname</c>) zieht mit — der Projektkontext hängt am Namen.
        ///
        /// <para>Ein STAMMPROJEKT lehnt die Methode ab: Sein Name ist der Schlüssel der
        /// Gruppe (er steckt in den Projektnamen aller Varianten) und wird über
        /// „Speichern unter…" vergeben.</para>
        /// </summary>
        /// <returns><c>true</c> bei Erfolg; sonst <paramref name="fehler"/>.</returns>
        public bool Umbenennen(int idProjekt, string neuerBezeichner, out string fehler,
                               out string neuerProjektname)
        {
            fehler = null;
            neuerProjektname = null;
            neuerBezeichner = (neuerBezeichner ?? "").Trim();
            if (idProjekt <= 0) { fehler = "Kein Projekt angegeben."; return false; }
            if (neuerBezeichner.Length == 0) { fehler = "Bitte einen Bezeichner für die Variante eingeben."; return false; }

            int idStamm = StammRefDerVariante(idProjekt);
            if (idStamm <= 0)
            {
                fehler = "Nur eine Variante lässt sich hier umbenennen; ein Stammprojekt bekommt seinen Namen über „Speichern unter…\".";
                return false;
            }
            string stammName = StartseiteCtrl.Projektname(idStamm);
            string alterName = StartseiteCtrl.Projektname(idProjekt);
            if (string.IsNullOrWhiteSpace(stammName)) { fehler = "Das Stammprojekt der Variante wurde nicht gefunden."; return false; }

            string basisName = stammName + " - " + neuerBezeichner;
            string neuerName = basisName;
            int n = 2;
            while (!string.Equals(neuerName, alterName, StringComparison.Ordinal) && ProjektnameExistiert(neuerName))
            {
                neuerName = basisName + " (" + n + ")";
                n++;
            }

            try
            {
                DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Projektname = ? WHERE ID = ?",
                    new DbParam("@name", neuerName),
                    new DbParam("@id", idProjekt));
                DataRepository.ExecuteSQL("UPDATE " + TAB_VARIANTE + " SET Variantenname = ? WHERE ID_Projekt = ?",
                    new DbParam("@name", neuerBezeichner),
                    new DbParam("@proj", idProjekt));
                if (!string.IsNullOrEmpty(alterName) && !string.Equals(alterName, neuerName, StringComparison.Ordinal))
                    DataRepository.ExecuteSQL("UPDATE Tab_Applikation SET Projektname = ? WHERE Projektname = ?",
                        new DbParam("@neu", neuerName),
                        new DbParam("@alt", alterName));
                neuerProjektname = neuerName;
                return true;
            }
            catch (Exception ex)
            {
                fehler = "Fehler beim Umbenennen: " + ex.Message;
                return false;
            }
        }

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
