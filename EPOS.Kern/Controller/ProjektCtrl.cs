using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class ProjektCtrl : ProjektModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<ProjektModel> _internalList = new List<ProjektModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable
        public new int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array (als Liste, die sich wie ein Array verhält)
        public List<ProjektModel> items => _internalList;

        public ProjektCtrl()
        {
            _hasSingleData = false;
        }

        #region --- DATABASE OPERATIONS ---

        public int GetMaxID() => DataRepository.GetMaxID("Tab_Projekt", "ID");

        /// <summary>
        /// Zeigt <paramref name="idProjekt"/> auf eine wirklich vorhandene
        /// <c>Tab_Projekt</c>-Zeile? (iU9-W9.0d)
        ///
        /// <para>Im Neuanlage-Zweig des Assistenten ist die Projekt-Id nur die in
        /// <c>WizardParent.Next</c> geratene <c>GetMaxID() + 1</c>; ein UPDATE auf die
        /// Projektzeile traefe dort 0 Zeilen und meldete trotzdem Erfolg. Die Pruefung
        /// stand wortgleich in <c>Form_Prozesswaerme.ProjektIstGespeichert</c>:387 und
        /// <c>Form_Stromverbraucher.ProjektIstGespeichert</c>:254.</para>
        /// </summary>
        public static bool Existiert(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE ID = ?",
                new DbParam("@id", idProjekt));

            return anzahl != null && anzahl != DBNull.Value && Convert.ToInt32(anzahl) > 0;
        }

        /// <summary>
        /// Die Id zu einem Projektnamen; 0, wenn es ihn nicht gibt (iU9-W15a.0a).
        ///
        /// <para><b>Was sie abloest.</b> <c>Form_ProjektDelete.comboBox_Projekte_SelectedIndexChanged</c>
        /// tat dasselbe in fuenf Zeilen mit drei Fehlern (Befund W15a-B1): verkettetes SQL
        /// mit einem ANWENDERTEXT im WHERE (ein Projektname mit Apostroph brach die
        /// Anweisung), <c>rs.Next()</c> ohne Rueckgabepruefung mit anschliessendem
        /// <c>(int)rs.Read("ID")</c>, und <c>SELECT *</c> fuer eine einzige Spalte.</para>
        /// </summary>
        public static int IdVonName(string projektname)
        {
            if (string.IsNullOrWhiteSpace(projektname)) return 0;

            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Projekt WHERE Projektname = ?",
                new DbParam("@name", projektname));

            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        /// <summary>
        /// Wie viele Projekte tragen diesen Namen? (iU9-W15a, Entscheid O-3 vom
        /// 04.09.2026.)
        ///
        /// <para><b>Wozu.</b> Der ganze Loeschweg laeuft ueber den NAMEN — <see cref="Delete"/>
        /// und seine drei Vorarbeiten setzen alle bei <c>WHERE Projektname=?</c> an
        /// (Befund W15a-B49). Das ist richtig, solange der Name eindeutig ist, und das
        /// ist er: <c>Tab_Projekt</c> traegt seit der SQLite-Migration den eindeutigen
        /// Index <c>Projektname</c>, und „Speichern unter" prueft zusaetzlich ueber
        /// <c>ProjektDuplizierenCtrl.PruefeNamen</c>. Ein ALTBESTAND ohne diesen Index
        /// kann den Fall trotzdem fuehren — dann wird gefragt, statt still beide zu
        /// loeschen.</para>
        /// </summary>
        /// <returns>Die Zahl der Projekte dieses Namens; 0 bei leerem Namen.</returns>
        public static int AnzahlGleicherNamen(string projektname)
        {
            if (string.IsNullOrEmpty(projektname)) return 0;

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?",
                new DbParam("@pname", projektname));

            return anzahl != null && anzahl != DBNull.Value ? Convert.ToInt32(anzahl) : 0;
        }

        /// <summary>
        /// Alle Projekte als Anzeigezeilen, nach Namen sortiert (iU9-W15a.0a).
        ///
        /// <para><b>Die EINE Projektliste.</b> Bis iU9-W15a las jede der vier Masken
        /// ihre eigene (Befund W15a-B52). Sortiert wird wie in <see cref="ReadAll"/>,
        /// damit sich an der bisherigen Reihenfolge nichts aendert; die Feinsortierung
        /// nach Kunde oder Aenderungsdatum macht die Anzeige.</para>
        ///
        /// <para><b>Die Beschreibung reist mit, obwohl sie keine Spalte hat</b> — die
        /// Suche greift ueber sie (Befund W15a-B22).</para>
        ///
        /// <para><b>Die Variantenherkunft reist seit dem Anwenderwunsch vom 05.09.2026
        /// mit (W15a-E-1)</b> — Stamm-Id, Bezeichner und Stammname. Sie kommt aus
        /// EINER Abfrage mit zwei LEFT JOINs, nicht aus einer zweiten Abfrage je Zeile:
        /// <c>VariantenCtrl.StammRefDerVariante</c> je Projekt waere bei 24 Projekten
        /// 24 zusaetzliche Rundlaeufe, und die Liste wird bei jedem Suchtastendruck
        /// neu gezeichnet.</para>
        ///
        /// <para><b>Ohne <c>Tab_Variante</c> laeuft die alte Abfrage.</b> Die Tabelle
        /// legt <c>VariantenCtrl.StelleVariantentabelleSicher</c> erst beim ersten
        /// Anlegen einer Variante an; ein Bestand ohne Variantenmodul hat sie nicht.
        /// Ein LEFT JOIN auf eine fehlende Tabelle braeche die GANZE Abfrage — der
        /// Anwender saehe eine leere Projektliste. Also wird vorher gefragt.</para>
        /// </summary>
        public static IReadOnlyList<ProjektKopfZeile> NamenListe()
        {
            var liste = new List<ProjektKopfZeile>();
            bool mitVarianten = VariantentabelleLesbar();
            try
            {
                // Jet verlangt bei zwei JOINs die Klammerung im FROM (wie in
                // VariantenCtrl.EntferneWaisen); SQLite nimmt sie klaglos an.
                DataTable dt = mitVarianten
                    ? DataRepository.GetDataTable(
                        "SELECT p.ID, p.Projektname, p.Kunde, p.Beschreibung, p.Aenderungsdatum, " +
                        "v.ID_ProjektRef AS StammId, v.Variantenname AS Bezeichner, " +
                        "s.Projektname AS Stammname " +
                        "FROM (Tab_Projekt p LEFT JOIN " + SchemaKatalog.TAB_VARIANTE + " v " +
                        "ON v.ID_Projekt = p.ID) " +
                        "LEFT JOIN Tab_Projekt s ON s.ID = v.ID_ProjektRef " +
                        "ORDER BY p.Projektname")
                    : DataRepository.GetDataTable(
                        "SELECT ID, Projektname, Kunde, Beschreibung, Aenderungsdatum " +
                        "FROM Tab_Projekt ORDER BY Projektname");
                if (dt == null) return liste;

                foreach (DataRow r in dt.Rows)
                    liste.Add(new ProjektKopfZeile(
                        r["ID"] != DBNull.Value ? Convert.ToInt32(r["ID"]) : 0,
                        Convert.ToString(r["Projektname"]) ?? "",
                        Convert.ToString(r["Kunde"]) ?? "",
                        Convert.ToString(r["Beschreibung"]) ?? "",
                        r["Aenderungsdatum"] != DBNull.Value ? Convert.ToDateTime(r["Aenderungsdatum"]) : (DateTime?)null,
                        "",
                        "",
                        SpaltenZahl(dt, r, "StammId"),
                        SpaltenText(dt, r, "Bezeichner"),
                        SpaltenText(dt, r, "Stammname")));
            }
            catch (Exception ex)
            {
                // Wie im Vorlaeufer (ProjektAuswahl.Laden, Befund W15a-B23): eine
                // unlesbare Liste ist kein Dialog wert - sie bleibt leer, und der
                // Aufrufer zeigt seinen Leertext.
                Console.WriteLine("Projektliste konnte nicht gelesen werden: " + ex.Message);
            }
            return liste;
        }

        /// <summary>
        /// Die Klimaregion des AKTIVEN Projekts (<c>Tab_Applikation.ID_Projekt</c>), 0 ohne
        /// Projekt - die Vorbelegung eines neuen Projekts (Nutzerauftrag 02.09.2026, Merge 5).
        /// </summary>
        public static int KlimaregionDesAktivenProjekts()
        {
            try
            {
                DataTable app = DataRepository.GetDataTable("SELECT ID_Projekt FROM Tab_Applikation");
                if (app == null || app.Rows.Count == 0 || app.Rows[0]["ID_Projekt"] == DBNull.Value) return 0;
                int id = Convert.ToInt32(app.Rows[0]["ID_Projekt"]);
                if (id <= 0) return 0;
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", new DbParam("@id", id));
                if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["ID_Klimaregion"] == DBNull.Value) return 0;
                return Convert.ToInt32(dt.Rows[0]["ID_Klimaregion"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Klimaregion des aktiven Projekts konnte nicht gelesen werden: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Gibt es <c>Tab_Variante</c>? Still ueber <see cref="StilleDb"/> — eine
        /// Auskunft ist kein Bedienschritt und darf keinen Dialog zeigen.
        /// </summary>
        private static bool VariantentabelleLesbar()
        {
            try { return StilleDb.TabelleVorhanden(SchemaKatalog.TAB_VARIANTE); }
            catch { return false; }
        }

        /// <summary>Ganzzahl einer Spalte, die es nur in der Variantenabfrage gibt (0 sonst).</summary>
        private static int SpaltenZahl(DataTable dt, DataRow r, string spalte)
            => dt.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToInt32(r[spalte]) : 0;

        /// <summary>Text einer Spalte, die es nur in der Variantenabfrage gibt (leer sonst).</summary>
        private static string SpaltenText(DataTable dt, DataRow r, string spalte)
            => dt.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToString(r[spalte]) ?? "" : "";

        /// <summary>
        /// Die neun Kopffelder eines Projekts fuer die erste Assistentenseite
        /// (iU9-W15a.0g); <c>null</c>, wenn es das Projekt nicht gibt.
        ///
        /// <para>Ein leerer Name liefert einen LEEREN Satz mit heutigem Datum — genau
        /// der Zweig <c>Wizard_Projekt.SetProjektbezeichner("")</c> des Neu-Modus.</para>
        ///
        /// <para>Der Anzeigename der Klimaregion kommt aus
        /// <c>KlimaregionStammCtrl.NameZuProjektregion</c> (mit dem STAMM-Rueckfall fuer
        /// aeltere Projekte, iU9-W15a.0f).</para>
        /// </summary>
        public static ProjektKopfDaten Kopf(string projektname)
        {
            if (string.IsNullOrEmpty(projektname))
                return new ProjektKopfDaten();

            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(projektname);
            if (ctrl.rows == 0) return null;

            return new ProjektKopfDaten
            {
                Name = ctrl.m_szProjektname ?? "",
                Beschreibung = ctrl.m_szBeschreibung ?? "",
                Kunde = ctrl.m_szKunde ?? "",
                Bearbeiter = ctrl.m_szBearbeiter ?? "",
                Erstelldatum = ctrl.m_Erstelldatum,
                Aenderungsdatum = ctrl.m_Aenderungsdatum,
                IdKlimaregion = ctrl.m_ID_Klimaregion,
                Klimaname = KlimaregionStammCtrl.NameZuProjektregion(ctrl.m_ID_Klimaregion, ctrl.m_ID)
            };
        }

        public bool Insert()
        {
            m_ID = GetMaxID() + 1;

            string sql = @"INSERT INTO Tab_Projekt 
                           (Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) 
                           VALUES (?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@name", m_szProjektname ?? ""),
                new DbParam("@bearb", m_szBearbeiter ?? ""),
                new DbParam("@besch", m_szBeschreibung ?? ""),
                new DbParam("@kunde", m_szKunde ?? ""),
                new DbParam("@date", DbParamTyp.Date) { Wert = ValidateDate(m_Aenderungsdatum) },
                new DbParam("@klima", m_ID_Klimaregion),
                new DbParam("@edate", DbParamTyp.Date) { Wert = ValidateDate(m_Erstelldatum) }
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Update()
        {
            string sql = @"UPDATE Tab_Projekt SET 
                            Bearbeiter=?, Beschreibung=?, Kunde=?, 
                            Aenderungsdatum=?, ID_Klimaregion=?, Erstelldatum=? 
                           WHERE Projektname=?";

            DbParam[] ps = {
                new DbParam("@bearb", (object)m_szBearbeiter ?? ""),
                new DbParam("@besch", (object)m_szBeschreibung ?? ""),
                new DbParam("@kunde", (object)m_szKunde ?? ""),
                new DbParam("@date", DbParamTyp.Date) { Wert = ValidateDate(m_Aenderungsdatum) },
                new DbParam("@klima", m_ID_Klimaregion),
                new DbParam("@edate", DbParamTyp.Date) { Wert = ValidateDate(m_Erstelldatum) },
                new DbParam("@pname", m_szProjektname)
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Löscht das Projekt. Die Detailtabellen fallen über ihre Löschweitergaben mit
        /// weg - seit Schritt 4 der SchemaMigration auch die Puffer-Projektkopien
        /// (B0-6b: Tab_Projekt.ID -> Tab_Pufferspeicher.ID_Projekt, ON DELETE CASCADE).
        ///
        /// Vorher werden die Anlagen-Verweise auf diese Puffer gelöst. Grund: die vier
        /// Referenzen ID_PUFFER / WS_ID_Puffer / WS_ID_Puffer2 / WQ_ID_Puffer sind
        /// bewusst RESTRIKTIV angelegt (keine Löschweitergabe, sonst risse ein gelöschter
        /// Speicher die referenzierende Wärmepumpe mit). Zeigt beim Projekt-DELETE noch
        /// eine Anlage auf einen Projekt-Puffer, lehnt Access die gesamte Kaskade ab.
        ///
        /// Die Aufrufer (MenueCtrl.ProjektDelete, VariantenCtrl.LoescheVariante) löschen
        /// die Energieanlagen zwar vorher - aber die B0-6b-Kaskade soll nicht von der
        /// Aufrufreihenfolge abhängen. Deshalb steht das Lösen hier, an der einen
        /// zentralen Stelle, durch die beide Wege laufen.
        ///
        /// <para><b>Der Schlüssel ist die ID, nicht der NAME</b> (Auftrag Kostenbereich,
        /// Nebenbefund 4). Bis dahin liefen dieses DELETE und seine drei Vorarbeiten über
        /// <c>WHERE Projektname=?</c>; zwei Projekte desselben Namens fielen also
        /// gemeinsam, samt ihrer <c>Tab_ProjektWerte</c>-Kosten. Geschützt war das allein
        /// durch eine Zählung beim AUFRUFER — eine Prüfung, die den Löschweg selbst nicht
        /// erreicht. Der eindeutige Index <c>Projektname</c> macht den Fall im gesunden
        /// Bestand unmöglich, ein Altbestand ohne ihn kann ihn führen, und ein Schlüssel,
        /// der eine Prüfung braucht, ist der falsche Schlüssel. Die Kaskade
        /// <c>Tab_Projekt → Tab_ProjektWerte</c> und alle anderen Löschweitergaben bleiben
        /// unberührt: Sie hängen ohnehin an <c>Tab_Projekt.ID</c>.</para>
        /// </summary>
        public bool Delete(int idProjekt)
        {
            if (idProjekt <= 0) return false;

            PufferReferenzenLoesen(idProjekt);
            BerichtsKonfigurationEntfernen(idProjekt);
            VariantenVerknuepfungenEntfernen(idProjekt);

            string sql = "DELETE FROM Tab_Projekt WHERE ID=?";
            DbParam[] ps = { new DbParam("@id", idProjekt) };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        /// <summary>
        /// Der VOLLSTAENDIGE Loeschweg eines Projekts (iU9-W15a.0d) — die sechs Schritte,
        /// die bis dahin in <c>MenueCtrl.ProjektDelete</c> standen, OHNE die zwei
        /// Dialogaufrufe. Rueckfrage und Erfolgsmeldung bleiben Oberflaeche.
        ///
        /// <para><b>Die Reihenfolge ist die alte:</b> (3) <c>Tab_Applikation</c>
        /// zuruecksetzen, falls das geloeschte Projekt das gemerkte ist — scheitert das,
        /// wird ABGEBROCHEN und nichts geloescht; (4) die Energieanlagen ueber
        /// <see cref="WErzeugerCtrl.Delete"/>; (5) <see cref="Delete"/> mit seinen drei
        /// Vorarbeiten. Die Schritte 1, 2 und 6 — Auswahl, Sicherheitsabfrage,
        /// Erfolgsmeldung — gehoeren der Oberflaeche.</para>
        ///
        /// <para><b>Die doppelte Sicherung bleibt, und das ist Absicht</b> (Befund
        /// W15a-B50, Risiko R-W15a-12): Schritt 4 loescht die Energieanlagen vorher,
        /// <see cref="Delete"/> loest ueber <c>PufferReferenzenLoesen</c> dieselben
        /// Verweise noch einmal — damit die B0-6b-Kaskade nicht von der
        /// Aufrufreihenfolge abhaengt. Wer das „aufraeumt", bricht sie.</para>
        ///
        /// <para><b>Was gegenueber dem Vorlaeufer BERICHTIGT ist:</b> Das UPDATE auf
        /// <c>Tab_Applikation</c> band EINEN Parameter fuer zwei Zuweisungen und las
        /// vorher <c>SELECT *</c> fuer eine einzige Spalte (Befund W15a-B48). Hier steht
        /// die eine Spalte im SELECT; die Zuweisung <c>ID_Projekt = 0</c> ist wie zuvor
        /// eine Konstante.</para>
        ///
        /// <para><b>JEDER Schritt laeuft ueber die ID</b> (Auftrag Kostenbereich,
        /// Nebenbefund 4). Bis dahin nahm der letzte Schritt — <see cref="Delete"/> samt
        /// seinen drei Vorarbeiten — den NAMEN, und zwei Projekte desselben Namens fielen
        /// gemeinsam; die Zaehlung hier war der einzige Schutz davor. Geloescht wird jetzt
        /// genau das eine Projekt, dessen Id hereinkommt.</para>
        ///
        /// <para><b>Die Zaehlung bleibt als ANZEIGE</b> (Entscheid O-3 vom 04.09.2026 —
        /// woertlich: „Projektname darf nicht gleich sein, daher löschen. Rückfragen in
        /// diesem Fall."). Ein doppelt vergebener Name ist im gesunden Bestand unmoeglich
        /// (eindeutiger Index <c>Projektname</c> auf <c>Tab_Projekt</c> seit der
        /// SQLite-Migration, dazu <c>PruefeNamen</c> in „Speichern unter") und in einem
        /// Altbestand ohne diesen Index ein Befund, den der Anwender sehen soll: Der Weg
        /// meldet <see cref="LoeschStand.Mehrdeutig"/> mit der Anzahl und fasst NICHTS an.
        /// Mit <paramref name="mehrdeutigZugelassen"/> laeuft er weiter — und nimmt dann
        /// NUR das gewaehlte Projekt, nicht mehr seine Namensvettern.</para>
        /// </summary>
        /// <param name="idProjekt">Id des zu loeschenden Projekts — der Schluessel jedes Schritts.</param>
        /// <param name="projektname">Name des Projekts; Anzeige und Zaehlung, nicht mehr Schluessel.</param>
        /// <param name="mehrdeutigZugelassen">
        /// <c>true</c> = der Anwender hat die Rueckfrage zum doppelt vergebenen Namen
        /// bejaht. Vorgabe <c>false</c>: mehrdeutig heisst abbrechen.
        /// </param>
        public static LoeschBefund LoeschenMitVorarbeiten(int idProjekt, string projektname,
                                                          bool mehrdeutigZugelassen = false)
        {
            if (string.IsNullOrEmpty(projektname))
                return new LoeschBefund(LoeschStand.NameLeer, "", "", 0);

            // Entscheid O-3: VOR dem ersten Schritt zaehlen. Nichts ist bis hierher
            // angefasst - der Abbruch laesst die Datenbank unberuehrt.
            int gleichnamige = AnzahlGleicherNamen(projektname);
            if (gleichnamige > 1 && !mehrdeutigZugelassen)
                return new LoeschBefund(LoeschStand.Mehrdeutig, projektname, "", gleichnamige);

            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT ID_Projekt FROM Tab_Applikation");

                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    if (row["ID_Projekt"] != DBNull.Value && Convert.ToInt32(row["ID_Projekt"]) == idProjekt)
                        DataRepository.ExecuteNonQuery(
                            "UPDATE Tab_Applikation SET Projektname = ?, ID_Projekt = 0",
                            new DbParam("@name", ""));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Zurücksetzen der Tab_Applikation: " + ex.Message);
                return new LoeschBefund(LoeschStand.ApplikationsdatenFehler, projektname,
                                        ex.Message, gleichnamige);
            }

            var anlagen = new WErzeugerCtrl { ID_Projekt = idProjekt };
            anlagen.Delete();

            // Nutzerauftrag 02.09.2026 (Merge 5): die gespeicherten Ergebnisse gehen mit -
            // bisher blieben sie als Rueckstand stehen (kein FK auf Tab_Projekt).
            new ErgebnisCtrl().Delete(idProjekt);

            var projekt = new ProjektCtrl { m_szProjektname = projektname };
            projekt.Delete(idProjekt);

            return new LoeschBefund(LoeschStand.Geloescht, projektname, "", gleichnamige);
        }

        /// <summary>
        /// Entfernt die Berichtskonfiguration DIESES Projekts VOR dem Projekt-DELETE. Die
        /// Tabelle Berichtskonfiguration hängt an keiner
        /// Löschweitergabe (Ad-hoc-DDL ohne Beziehung, BerichtCtrl) — verbliebe die
        /// Zeile, kollidierte eine spätere Projektkopie am eindeutigen Index
        /// UQ_BerichtKonfigProj, sobald die neue Projekt-ID (MAX+1) auf die verwaiste
        /// ProjektID fällt (Duplizier-Abbruch vom 21.08.2026). Still über StilleDb:
        /// Fehlt die Tabelle (Datenbank ohne Berichtsmodul), läuft das Löschen ohne
        /// Dialog weiter.
        /// </summary>
        private static void BerichtsKonfigurationEntfernen(int idProjekt)
        {
            try
            {
                StilleDb.NonQuery(
                    "DELETE FROM " + SchemaKatalog.TAB_BERICHTSKONFIGURATION + " WHERE ProjektID = ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Berichtskonfiguration des Projekts konnte nicht entfernt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Entfernt die Tab_Variante-Verknüpfungen DIESES Projekts VOR dem
        /// Projekt-DELETE (Befund B5: Tab_Variante hängt an keiner Löschweitergabe).
        /// Beide Richtungen: die Verknüpfungszeile des Projekts selbst (ID_Projekt) und
        /// die seiner Varianten (ID_ProjektRef) — deren Projekte bleiben bestehen und
        /// werden wieder eigenständig, wie es EntferneWaisen ebenfalls täte. Verbliebe
        /// die Zeile, kollidierte ein späteres „Variante anlegen" am eindeutigen Index
        /// UQ_VarProj, sobald die neue Projekt-ID auf die verwaiste ID_Projekt fällt
        /// (gleiche Falle wie UQ_BerichtKonfigProj). Still über StilleDb: Fehlt die
        /// Tabelle (Datenbank ohne Variantenmodul), läuft das Löschen ohne Dialog weiter.
        /// </summary>
        private static void VariantenVerknuepfungenEntfernen(int idProjekt)
        {
            try
            {
                StilleDb.NonQuery(
                    "DELETE FROM " + SchemaKatalog.TAB_VARIANTE + " WHERE ID_Projekt = ? OR ID_ProjektRef = ?",
                    StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt),
                    StilleDb.Par("@ref", DbParamTyp.Integer, idProjekt));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Varianten-Verknüpfungen des Projekts konnten nicht entfernt werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Löst die Anlagen-Verweise auf die Pufferspeicher DIESES Projekts.
        /// Still: schlägt es fehl, soll das Löschen trotzdem versucht werden - die
        /// Beziehung meldet sich dann von selbst.
        /// </summary>
        private static void PufferReferenzenLoesen(int idProjekt)
        {
            try
            {
                PufferSpCtrl.ReferenzenLoesenFuerProjekt(idProjekt);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Puffer-Referenzen des Projekts konnten nicht gelöst werden: " + ex.Message);
            }
        }

        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Projekt ORDER BY Projektname");
            _internalList.Clear();
            _hasSingleData = false;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string projektName)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE Projektname=?";
            DbParam[] ps = { new DbParam("@pname", projektName) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }

        public void ReadSingle(int IDProjekt)
        {
            string sql = "SELECT * FROM Tab_Projekt WHERE ID=?";
            DbParam[] ps = { new DbParam("@id", IDProjekt) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            _internalList.Clear(); // Liste leeren, da wir nur einen Datensatz laden

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                ProjektModel model = MapRowToModel(row);

                // Daten in die aktuelle Instanz mappen
                this.m_ID = model.m_ID;
                this.m_szProjektname = model.m_szProjektname;
                this.m_szBearbeiter = model.m_szBearbeiter;
                this.m_szBeschreibung = model.m_szBeschreibung;
                this.m_szKunde = model.m_szKunde;
                this.m_Aenderungsdatum = model.m_Aenderungsdatum;
                this.m_ID_Klimaregion = model.m_ID_Klimaregion;
                this.m_Erstelldatum = model.m_Erstelldatum;

                _hasSingleData = true;
            }
            else
            {
                _hasSingleData = false;
            }
        }
        #endregion

        #region --- UI FILL METHODS ---


        #endregion

        #region --- HELPER METHODS ---

        private DateTime ValidateDate(DateTime date)
        {
            if (date < new DateTime(1900, 1, 1)) return DateTime.Now;
            return date;
        }

        private ProjektModel MapRowToModel(DataRow row)
        {
            return new ProjektModel
            {
                m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                m_szProjektname = row["Projektname"].ToString(),
                m_szBearbeiter = row["Bearbeiter"]?.ToString() ?? "",
                m_szBeschreibung = row["Beschreibung"]?.ToString() ?? "",
                m_szKunde = row["Kunde"]?.ToString() ?? "",
                m_Aenderungsdatum = row["Aenderungsdatum"] != DBNull.Value ? Convert.ToDateTime(row["Aenderungsdatum"]) : DateTime.Now,
                m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0,
                m_Erstelldatum = row["Erstelldatum"] != DBNull.Value ? Convert.ToDateTime(row["Erstelldatum"]) : DateTime.Now
            };
        }

        #endregion
    }
}