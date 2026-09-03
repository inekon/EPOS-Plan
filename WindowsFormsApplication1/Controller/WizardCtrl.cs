using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class WizardCtrl
    {
        /// <summary>
        /// Der eine Assistenten-Controller des laufenden Programms.
        ///
        /// <para><b>Wozu.</b> Bis iU5 lag er als <c>Program.wizardctrl</c> im
        /// WinForms-Einstiegspunkt; Kern-naher Programmtext, der den Rahmen des
        /// Assistenten anmelden musste, kam nur über <c>Program</c> dorthin. Die
        /// Anmeldung hier ist dasselbe Hausmuster wie <c>WizardParent.Aktiver</c>:
        /// EIN statischer Halter, gesetzt von <c>Program.Main</c>,
        /// <c>Program.wizardctrl</c> ist seither nur noch die Weiterleitung für die
        /// Masken.</para>
        /// </summary>
        public static WizardCtrl Aktueller { get; set; }

        public WizardParent parentform;
        public bool speichern;
        public string Projektname;
        public string Klimazone;

        public WizardCtrl()
        {
            speichern = false;
            Projektname = "";
            Klimazone = "";
        }

        private object GetIdForType(WErzeugerModel item, int targetType, object value)
        {
            return (item.ID_Type == targetType) ? value : DBNull.Value;
        }

        /// <summary>
        /// Loescht die Anlagenzeilen eines Projekts fuer den Del+Add-Speicherweg -
        /// alle Typen AUSSER den Pufferspeichern (<c>ID_Type</c> 12).
        ///
        /// <para>
        /// WARUM DIE PUFFER STEHEN BLEIBEN (FR-1, Befund 27.08.2026). Einziger
        /// Aufrufer dieser Ueberladung ist der Bearbeiten-Zweig des Wizards
        /// (<c>WizardParent.btnSpeichern_Click</c>) - und der Wizard hat keine
        /// Puffer-Seite. Loeschte der Rundumschlag die ID_Type-12-Zeilen mit, muesste
        /// die Dialogliste sie zurueckschreiben: Jede Liste ohne die Puffer beraubte
        /// das Projekt seiner Speicher, und <c>GeraeteWaisen.Aufraeumen</c> am Ende von
        /// <see cref="Add_WP_Waermeerzeuger"/> raeumte anschliessend die nicht mehr
        /// referenzierten Geraetezeilen in <c>Tab_Pufferspeicher</c> ab (Feldbeleg:
        /// Projekte 1027/1009 mit <c>WS_Ziel = 'PufferHeizung'</c> und
        /// <c>WS_ID_Puffer = NULL</c>). Gegenstueck im Wizard:
        /// <c>entferne_nicht_aktive_elemente</c> nimmt die ID_Type-12-Modelle aus der
        /// Liste, sonst legte <see cref="Add_WP_Waermeerzeuger"/> die stehen
        /// gebliebenen Anlagenzeilen ein zweites Mal an.
        /// </para>
        ///
        /// <para>
        /// Pufferspeicher LOESCHEN koennen weiterhin: die typisierte Ueberladung
        /// (Puffer-Karte und -Kontextmenue rufen sie mit ID_Type 12),
        /// <see cref="Del_Projekt_ID_Waermeerzeuger"/> (Einzelzeile) und der
        /// Projekt-Loeschweg <c>WErzeugerCtrl.Delete</c>.
        /// </para>
        /// </summary>
        public bool Del_Projekt_Waermeerzeuger(int projektID)
        {
            SpVariantenSichern(projektID, TYP_ALLE);
            SenkenSichern(projektID);

            // ID_Type fest im SQL statt als Parameter - dieselbe Begruendung wie bei
            // SP_TYPEN: Programmkonstante, keine Anwendereingabe.
            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type <> " +
                WizardItemClass.PUFFER_TYP.ToString(CultureInfo.InvariantCulture),
                new DbParam[] { new DbParam("@pID", projektID) });
        }

        public bool Del_Projekt_Waermeerzeuger(int projektID, int nType)
        {
            SpVariantenSichern(projektID, nType);

            // S1: Die Senkenlisten werden AUCH im typgefilterten Weg gesichert - die
            // Loeschweitergabe FK_AnlageSenke_Anlage trifft jede geloeschte Anlage,
            // gleich ob mit oder ohne Typfilter. Anlagen, die den Filter ueberleben,
            // behalten ihre Senken und werden beim Wiederherstellen uebergangen.
            SenkenSichern(projektID);

            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@type", nType) });
        }

        public bool Del_Projekt_ID_Waermeerzeuger(int projektID, int ID_Waermeerzeuger)
        {
            // Ä21: Das gezielte Entfernen EINER Anlage nimmt ihre Kostenpositionen
            // mit (Nutzerauftrag 27.08.2026: eine nicht angelegte Anlage darf keine
            // Kosten hinterlassen). NUR hier — die Typ-/Alle-Löschwege sind auch
            // der destruktive Wizard-Neuaufbau; dort heilt die Zuordnung über den
            // Geräteanker (KostenProjektPositionenCtrl.ZuordnungReparieren).
            try
            {
                if (KostenPositionCtrl.StelleSpaltenSicher())
                    DataRepository.ExecuteSQL(
                        "DELETE FROM Tab_ProjektWerte WHERE ProjektID = ? AND ID_Anlage = ?",
                        new DbParam("@p", projektID),
                        new DbParam("@a", ID_Waermeerzeuger));
            }
            catch { }

            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID = ?",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@id", ID_Waermeerzeuger) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID)
        {
            // Tagesverteilungen der Projekt-Gebaeude entfernen (Detail vor Kopf).
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?))",
                new DbParam[] { new DbParam("@pID", projektID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?)",
                new DbParam[] { new DbParam("@pID", projektID) });
            // Erst die Projekt-Gebaeudekopien (Kind: FK ID_ProjektGebaeude -> Z_ProjektGebaeude.ID), dann die Zuordnung.
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                new DbParam[] { new DbParam("@pID", projektID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ?",
                new DbParam[] { new DbParam("@pID", projektID) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID, int ID)
        {
            // ID = Z_ProjektGebaeude.ID; zugehoerige Gebaeude-Kopie via ID_ProjektGebaeude.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?))",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@idpg", ID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?)",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@idpg", ID) });
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@idpg", ID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ? AND ID = ?",
                new DbParam[] { new DbParam("@pID", projektID), new DbParam("@id", ID) });
        }

        public bool Del_WaermebedarfExtern(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?",
                new DbParam[] { new DbParam("@pID", projektID) });
        }

        public bool Del_Projekt_Prozess(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?";

            List<DbParam> ps = new List<DbParam> { new DbParam("@pID", projektID) };
            if (ID > 0) ps.Add(new DbParam("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Stromganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektStromganglinie WHERE ID_Projekt = ?",
                new DbParam[] { new DbParam("@pID", projektID) });
        }

        public bool Del_Solarganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektSolarganglinie WHERE ID_Projekt = ?",
                new DbParam[] { new DbParam("@pID", projektID) });
        }

        public bool Del_Projekt_Stromverbraucher(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?";

            List<DbParam> ps = new List<DbParam> { new DbParam("@pID", projektID) };
            if (ID > 0) ps.Add(new DbParam("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Projekt_Brauchwasser(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?";

            List<DbParam> ps = new List<DbParam> { new DbParam("@pID", projektID) };
            if (ID > 0) ps.Add(new DbParam("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        // =================================================================================
        //  iU3 Kante K6 - die Einfuegeanweisung liegt jetzt bei AnlagenSql
        // =================================================================================
        //
        // Der Kommentar bei AnlagenSql.SQL_ANLAGE_INSERT verlangt "EINE WAHRHEIT" ueber den
        // Spaltensatz. Diese Wahrheit stand bis iU3 hier - und zog damit ueber
        // WErzeugerCtrl.Insert den gesamten Wizard samt Oberflaeche in den Rechenpfad.
        // Sie steht jetzt in Controller/AnlagenSql.cs (kein Dialog, nur SQL und
        // Parameter); hier bleiben die Weiterleitungen, damit alle Aufrufer gueltig
        // bleiben.

        /// <summary>
        /// Die EINE Einfuegeanweisung fuer <c>Tab_Energieanlagen</c> - 56 der 57 Spalten
        /// (<c>ID</c> ist AutoWert und wird nie gesetzt).
        /// Weiterleitung auf <see cref="AnlagenSql.SQL_ANLAGE_INSERT"/>.
        /// </summary>
        public const string SQL_ANLAGE_INSERT = AnlagenSql.SQL_ANLAGE_INSERT;

        /// <summary>
        /// Parameter zu <see cref="SQL_ANLAGE_INSERT"/>, exakt in der Reihenfolge der
        /// Anweisung. Weiterleitung auf <see cref="AnlagenSql.AnlagenParameter"/>.
        /// </summary>
        public static DbParam[] AnlagenParameter(int projektID, WErzeugerModel item,
                                                        Dictionary<int, bool> pufferCache = null)
        {
            return AnlagenSql.AnlagenParameter(projektID, item, pufferCache);
        }

        /// <summary>
        /// Gibt es die Speicherzeile? Weiterleitung auf
        /// <see cref="AnlagenSql.PufferVorhanden"/> - dieselbe Pruefung, die auch die
        /// Parameterfabrik benutzt.
        /// </summary>
        private static bool PufferVorhanden(int id, Dictionary<int, bool> cache)
        {
            return AnlagenSql.PufferVorhanden(id, cache);
        }

        /// <summary>
        /// Gehört die Speicherzeile zu diesem Projekt? Kriterium für den Erhalt einer
        /// bereits gesetzten <c>ID_PUFFER</c>, wenn die Katalogauflösung scheitert.
        /// </summary>
        private static bool PufferGehoertZuProjekt(int idPuffer, int projektID)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?",
                new DbParam[] {
                    new DbParam("@id", idPuffer),
                    new DbParam("@proj", projektID)
                });

            return (v != null && v != DBNull.Value && Convert.ToInt32(v) > 0);
        }

        // =================================================================================
        //  AP9b - Rettung der Speicher-Variantenparameter ueber den Del+Add-Speicherweg
        // =================================================================================
        //
        // DAS PROBLEM. Der Speicherweg aller Erzeuger ist Loeschen + Neuanlegen:
        // Del_Projekt_Waermeerzeuger loescht die Anlagenzeilen des Projekts (wahlweise
        // eines Typs), Add_WP_Waermeerzeuger schreibt die Liste des Dialogs komplett neu.
        // Tab_Energieanlagen.ID ist ein AutoWert - die neuen Zeilen bekommen also NEUE
        // IDs. Seit Migrationsschritt 11b haengt an jeder Speicheranlage eine Zeile in
        // Tab_StromspeicherVariante, verbunden ueber ID_Energieanlage und mit
        // Loeschweitergabe (FK_SpVariante_Anlage). Ohne Gegenmassnahme raeumt damit JEDES
        // Speichern ueber Karte, Kontextmenue oder Wizard saemtliche Betriebsparameter des
        // Projektspeichers ab: Betriebsart, Quellen-Flags, SoC-Band, Berechnungsart,
        // Preisquelle, Zins, Nutzungsdauer und die Aktiv-Markierung.
        //
        // WARUM HIER UND NICHT AN DEN AUFRUFSTELLEN. Es sind zehn Del+Add-Paare in sechs
        // Dateien (Karten der Startseite, Kontextmenues, Wizard, Simulationsdetail), und
        // zwei davon loeschen ohne Typfilter ALLE Anlagen des Projekts. Eine Rettung je
        // Aufrufstelle waere zehnmal dieselbe Wahrheit - und die elfte Aufrufstelle haette
        // sie wieder nicht. Del und Add liegen an jeder Stelle auf DEMSELBEN
        // WizardCtrl-Objekt; die Sicherung darf deshalb ein Feld dieses Objekts sein.
        //
        // ZUORDNUNG UEBER (ID_Type, Bezeichner). Die alte ID ist nach dem Loeschen wertlos,
        // die Geraete-ID (ID_SP) nicht eindeutig - Varianten desselben Speichers teilen sich
        // eine Geraetekopie. Der Bezeichner IST der Variantenname (Fachkonzept 7.3,
        // Schritt 2) und ueberlebt den Rundumschlag, weil der Dialog ihn mitfuehrt. Wer
        // eine Variante im Dialog UMBENENNT, verliert ihre Parameter - dieselbe Grenze wie
        // bei CopyFromStamm, das ebenfalls ueber den Bezeichner sucht.
        //
        // ENTFERNTE ANLAGEN verfallen (gewollt), NEU HINZUGEKOMMENE bekommen die
        // Standard-Variantenzeile - dieselbe Vorbelegung, die Migrationsschritt 11d und
        // SpKontextMenuCtrl.VarianteSicherstellen schreiben.

        /// <summary>Kein Typfilter - die Sicherung nimmt beide Speichertypen.</summary>
        private const int TYP_ALLE = 0;

        /// <summary>
        /// <c>ID_Type IN (…)</c> der Speicheranlagen. Fest im SQL statt als Parameter:
        /// OleDb bindet nach POSITION, und eine IN-Liste aus Parametern waere genau die
        /// Reihenfolgefalle, die <see cref="AnlagenParameter"/> schon einmal gekostet hat.
        /// Die Werte sind Konstanten des Programms, keine Anwendereingabe.
        /// </summary>
        private static readonly string SP_TYPEN =
            WizardItemClass.SP_TYP.ToString(CultureInfo.InvariantCulture) + ", " +
            WizardItemClass.REF_SP_TYP.ToString(CultureInfo.InvariantCulture);

        /// <summary>Eine gesicherte Variantenzeile samt ihrem Wiedererkennungsmerkmal.</summary>
        private sealed class SpVariantenSicherung
        {
            public int ID_Type;
            public string Bezeichner = "";
            public bool Aktiv;
            public StromspeicherVarianteModel Parameter;
        }

        /// <summary>
        /// Die Sicherung des laufenden Speichervorgangs. <c>null</c> heisst „dieser
        /// Loeschbefehl hat keine Speicheranlage betroffen" - dann ruehrt das
        /// anschliessende Add die Variantentabelle nicht an.
        /// </summary>
        private List<SpVariantenSicherung> m_SpVariantenSicherung;

        /// <summary>
        /// Das Projekt, zu dem <see cref="m_SpVariantenSicherung"/> gehoert.
        ///
        /// <para>
        /// NOETIG, WEIL DIE INSTANZ UEBERLEBT. <c>Program.wizardctrl</c> ist ein
        /// prozessweites Objekt: Der Wizard fuehrt ueber dieselbe Instanz sowohl den
        /// Bearbeiten-Zweig (Del + Add) als auch den Neuanlage-Zweig, der
        /// <see cref="Add_WP_Waermeerzeuger"/> OHNE vorheriges Loeschen aufruft
        /// (<c>WizardParent.btnSpeichern_Click</c>). Bliebe eine Sicherung aus einem
        /// abgebrochenen Speichervorgang liegen, koennte sie sonst in einem FREMDEN
        /// Projekt landen, sobald dort zufaellig derselbe Bezeichner vorkommt.
        /// </para>
        /// </summary>
        private int m_SpVariantenProjekt;

        /// <summary>
        /// Sichert die Betriebsparameter der Speichervarianten, die der folgende
        /// Loeschbefehl mitnimmt - <b>nur im Arbeitsspeicher</b>, es wird nichts
        /// geschrieben.
        /// </summary>
        /// <param name="projektID">Projekt-ID.</param>
        /// <param name="nType">
        /// Der zu loeschende Anlagentyp, oder <see cref="TYP_ALLE"/> fuer den
        /// Rundumschlag ohne Typfilter. Ein anderer Typ (Kessel, BHKW, PV …) laesst die
        /// Speicheranlagen unberuehrt - dann gibt es nichts zu sichern.
        /// </param>
        private void SpVariantenSichern(int projektID, int nType)
        {
            m_SpVariantenSicherung = null;
            m_SpVariantenProjekt = 0;

            if (projektID <= 0) return;
            if (nType != TYP_ALLE &&
                nType != WizardItemClass.SP_TYP && nType != WizardItemClass.REF_SP_TYP) return;

            try
            {
                string sql = "SELECT ID, ID_Type, Bezeichner FROM Tab_Energieanlagen " +
                             "WHERE ID_Projekt = ? AND ID_Type IN (" + SP_TYPEN + ")";

                List<DbParam> ps = new List<DbParam>
                    { new DbParam("@pID", projektID) };

                if (nType != TYP_ALLE)
                {
                    sql += " AND ID_Type = ?";
                    ps.Add(new DbParam("@type", nType));
                }

                sql += " ORDER BY ID";

                DataTable dt = DataRepository.GetDataTable(sql, ps.ToArray());
                if (dt == null || dt.Rows.Count == 0) return;

                StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
                List<SpVariantenSicherung> sicherung = new List<SpVariantenSicherung>();

                foreach (DataRow r in dt.Rows)
                {
                    int idAnlage = SpZahl(r, "ID");
                    if (idAnlage <= 0) continue;

                    StromspeicherVarianteModel v = ctrl.ReadByEnergieanlage(idAnlage);
                    if (v == null) continue;          // Anlage ohne Variantenzeile - nichts zu retten

                    int idType = SpZahl(r, "ID_Type");
                    string bezeichner = SpText(r, "Bezeichner");

                    // Doppelte Bezeichner sind im Schema moeglich (der Primaerschluessel ist
                    // ID + ID_Projekt). Die erste Zeile gewinnt - genau die Wahl, die auch
                    // CopyFromStamm und NameVergeben treffen -, der Rest wird protokolliert.
                    if (SpTreffer(sicherung, idType, bezeichner) != null)
                    {
                        Console.WriteLine("Speichervarianten-Rettung: \"" + bezeichner +
                                          "\" kommt im Projekt " + projektID + " mehrfach vor - " +
                                          "gesichert wird die erste Zeile, die Parameter der " +
                                          "weiteren gehen verloren.");
                        continue;
                    }

                    sicherung.Add(new SpVariantenSicherung
                    {
                        ID_Type = idType,
                        Bezeichner = bezeichner,
                        Aktiv = v.Aktiv,
                        Parameter = v
                    });
                }

                if (sicherung.Count > 0)
                {
                    m_SpVariantenSicherung = sicherung;
                    m_SpVariantenProjekt = projektID;
                }
            }
            catch (Exception ex)
            {
                // Eine misslungene Sicherung darf den Speichervorgang nicht anhalten - sie
                // fuehrt zurueck auf das Verhalten vor diesem Paket, nicht auf einen Fehler.
                m_SpVariantenSicherung = null;
                m_SpVariantenProjekt = 0;
                Console.WriteLine("Die Speichervarianten konnten vor dem Loeschen nicht " +
                                  "gesichert werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Schreibt die gesicherten Betriebsparameter auf die NEUEN Anlagenzeilen zurueck
        /// und stellt genau eine aktive Variante her.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Erst nach einem vollstaendig gelungenen Add.</b> Scheitert das Neuanlegen,
        /// wird gar nichts geschrieben (<see cref="SpVariantenVerwerfen"/>) - eine
        /// Rettung auf halb wiederhergestellte Anlagenzeilen waere schlimmer als keine.
        /// </para>
        /// <para>
        /// <b>Alles oder nichts.</b> Der Bestandsweg kennt keine Transaktion ueber
        /// Del+Add hinweg (jeder <c>ExecuteSQL</c> oeffnet seine eigene Verbindung), eine
        /// hier eingezogene Klammer koennte das Loeschen davor ohnehin nicht mehr
        /// zuruecknehmen. Statt dessen raeumt diese Methode ihre EIGENEN Schreibvorgaenge
        /// wieder ab, sobald einer scheitert: Der Zustand danach ist „keine
        /// Variantenzeilen" - derselbe, den ein Datenbestand ohne Migrationslauf hat und
        /// den <see cref="StromspeicherSimCtrl"/> als Rueckfall traegt. Eine halb
        /// geschriebene Sicherung mit widerspruechlicher Aktiv-Markierung gibt es nicht.
        /// </para>
        /// <para>
        /// <b>Aktiv ausschliesslich ueber <c>SetzeAktiv</c>.</b> Eingefuegt wird jede Zeile
        /// mit <c>Aktiv = false</c>; die Markierung setzt am Ende der eine Schreibweg, der
        /// die Zusage „hoechstens eine aktive Variante je Projekt" traegt. Zwischenstaende
        /// mit zwei aktiven Varianten kann es dadurch nicht geben.
        /// </para>
        /// </remarks>
        private void SpVariantenWiederherstellen(int projektID)
        {
            List<SpVariantenSicherung> sicherung = m_SpVariantenSicherung;
            int projektDerSicherung = m_SpVariantenProjekt;

            m_SpVariantenSicherung = null;            // eine Sicherung, ein Wiederherstellen
            m_SpVariantenProjekt = 0;

            if (sicherung == null || sicherung.Count == 0 || projektID <= 0) return;

            if (projektDerSicherung != projektID)
            {
                Console.WriteLine("Speichervarianten-Rettung nicht ausgefuehrt: Die Sicherung " +
                                  "gehoert zu Projekt " + projektDerSicherung + ", geschrieben wird " +
                                  "aber Projekt " + projektID + ".");
                return;
            }

            List<int> geschrieben = new List<int>();

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, ID_Type, Bezeichner FROM Tab_Energieanlagen " +
                    "WHERE ID_Projekt = ? AND ID_Type IN (" + SP_TYPEN + ") ORDER BY ID",
                    new DbParam("@pID", projektID));

                if (dt == null || dt.Rows.Count == 0) return;

                StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
                int idVarianteAktiv = 0;
                int idVarianteErsatz = 0;
                int uebernommen = 0;
                int neu = 0;

                foreach (DataRow r in dt.Rows)
                {
                    int idAnlage = SpZahl(r, "ID");
                    if (idAnlage <= 0) continue;

                    // Fuehrt die Zeile schon eine Variante, ist nichts zu tun. Nach der
                    // Loeschweitergabe kann das nicht sein - auf einer Datenbank ohne die
                    // Beziehung aber sehr wohl, und dann ist der vorhandene Satz der
                    // juengere.
                    if (ctrl.ReadByEnergieanlage(idAnlage) != null) continue;

                    int idType = SpZahl(r, "ID_Type");
                    string bezeichner = SpText(r, "Bezeichner");

                    SpVariantenSicherung treffer = SpTreffer(sicherung, idType, bezeichner);

                    // Ohne Treffer ist die Anlage im Dialog NEU hinzugekommen: Sie bekommt
                    // die Vorbelegung des Modells - dieselben Werte wie aus
                    // Migrationsschritt 11d.
                    StromspeicherVarianteModel neuesatz = treffer != null
                        ? SpParameterUebernehmen(treffer.Parameter)
                        : new StromspeicherVarianteModel();

                    neuesatz.ID_Energieanlage = idAnlage;
                    neuesatz.Aktiv = false;           // SetzeAktiv ist die einzige Schreibstelle

                    int idVariante = ctrl.Insert(neuesatz);
                    if (idVariante <= 0)
                        throw new InvalidOperationException(
                            "Die Variantenzeile zu Anlage " + idAnlage + " (\"" + bezeichner +
                            "\") konnte nicht angelegt werden.");

                    geschrieben.Add(idVariante);

                    if (treffer != null) { uebernommen++; if (treffer.Aktiv) idVarianteAktiv = idVariante; }
                    else neu++;

                    // Ersatzwahl, falls die gesicherte aktive Variante nicht wiederkehrt
                    // (im Dialog entfernt oder umbenannt): die erste echte Speicheranlage
                    // in Anlagenreihenfolge - dieselbe Wahl wie Migrationsschritt 11d und
                    // SpKontextMenuCtrl.AktiveVarianteSicherstellen. Die Referenzliste
                    // (REF_SP_TYP) kommt dafuer nicht in Frage: Sie fuehrt den
                    // Vergleichsfall des Projekts, nicht dessen Planvarianten.
                    if (idVarianteErsatz == 0 && idType == WizardItemClass.SP_TYP)
                        idVarianteErsatz = idVariante;
                }

                if (geschrieben.Count == 0) return;

                // Genau eine aktive Variante - ohne sie faellt die Gesamtsimulation auf die
                // Aggregation ueber alle Speicheranlagen zurueck (StromspeicherSimCtrl).
                int idAktiv = idVarianteAktiv > 0 ? idVarianteAktiv : idVarianteErsatz;
                if (idAktiv > 0 && !ctrl.SetzeAktiv(projektID, idAktiv))
                    Console.WriteLine("Speichervarianten-Rettung: Die aktive Variante des " +
                                      "Projekts " + projektID + " konnte nicht gesetzt werden.");

                Console.WriteLine("Speichervarianten-Rettung: " + uebernommen +
                                  " Betriebsparametersaetze uebernommen, " + neu +
                                  " neue Anlage(n) mit Vorgabewerten, aktiv = Variante " + idAktiv + ".");
            }
            catch (Exception ex)
            {
                SpVariantenZuruecknehmen(geschrieben, ex.Message);
            }
        }

        /// <summary>
        /// Nimmt die bereits geschriebenen Variantenzeilen dieses Rettungslaufs wieder
        /// zurueck. Der Zustand danach ist derselbe wie ohne Rettung; halb wiederhergestellte
        /// Betriebsparameter waeren nicht erkennbar und damit gefaehrlicher als keine.
        /// </summary>
        private static void SpVariantenZuruecknehmen(List<int> geschrieben, string grund)
        {
            Console.WriteLine("Speichervarianten-Rettung abgebrochen: " + grund);

            if (geschrieben == null || geschrieben.Count == 0) return;

            try
            {
                StromspeicherVarianteCtrl ctrl = new StromspeicherVarianteCtrl();
                foreach (int id in geschrieben) ctrl.Delete(id);

                Console.WriteLine("Speichervarianten-Rettung: " + geschrieben.Count +
                                  " bereits geschriebene Zeile(n) wieder entfernt.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die angefangene Speichervarianten-Rettung konnte nicht " +
                                  "zurueckgenommen werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Verwirft eine Sicherung, ohne sie zu schreiben - der Weg bei einem
        /// gescheiterten <see cref="Add_WP_Waermeerzeuger"/>.
        /// </summary>
        private void SpVariantenVerwerfen(string grund)
        {
            if (m_SpVariantenSicherung == null) return;

            m_SpVariantenSicherung = null;
            m_SpVariantenProjekt = 0;
            Console.WriteLine("Speichervarianten-Rettung nicht ausgefuehrt (" + grund +
                              ") - die Betriebsparameter des Projektspeichers sind verloren.");
        }

        /// <summary>
        /// Betriebsparameter einer Sicherung in ein frisches Modell - ohne <c>ID</c>,
        /// <c>ID_Energieanlage</c> und <c>Aktiv</c>. Wortgleich zu
        /// <c>SpKontextMenuCtrl.ParameterUebernehmen</c>: Die drei sind Eigenschaften der
        /// ZEILE, nicht der Betriebsfuehrung.
        /// </summary>
        private static StromspeicherVarianteModel SpParameterUebernehmen(StromspeicherVarianteModel vorlage)
        {
            if (vorlage == null) return new StromspeicherVarianteModel();

            return new StromspeicherVarianteModel
            {
                Betriebsart = vorlage.Betriebsart,
                PV_Zulaessig = vorlage.PV_Zulaessig,
                BHKW_Ueberschuss_Zulaessig = vorlage.BHKW_Ueberschuss_Zulaessig,
                BHKW_Stromgefuehrt = vorlage.BHKW_Stromgefuehrt,
                Netzentladung = vorlage.Netzentladung,
                SoC_Min_Prozent = vorlage.SoC_Min_Prozent,
                SoC_Max_Prozent = vorlage.SoC_Max_Prozent,
                Berechnungsart = vorlage.Berechnungsart,
                Preisquelle = vorlage.Preisquelle,
                ID_Preisreihe = vorlage.ID_Preisreihe,
                ID_Kostenprofil = vorlage.ID_Kostenprofil,
                Aufschlag_Anwenden = vorlage.Aufschlag_Anwenden,
                Kompatibilitaetsmodus = vorlage.Kompatibilitaetsmodus,
                Kapitalzins = vorlage.Kapitalzins,
                Nutzungsdauer = vorlage.Nutzungsdauer,
                L_P = vorlage.L_P,
                A_Netzlade = vorlage.A_Netzlade,
                Ladeschwellwert = vorlage.Ladeschwellwert
            };
        }

        /// <summary>
        /// Die Sicherung zu (<paramref name="idType"/>, <paramref name="bezeichner"/>),
        /// oder <c>null</c>. Verglichen wird ohne Gross-/Kleinschreibung und ohne
        /// Randleerzeichen - so, wie Access den Bezeichner in
        /// <c>SpKontextMenuCtrl.NameVergeben</c> ebenfalls vergleicht.
        /// </summary>
        private static SpVariantenSicherung SpTreffer(List<SpVariantenSicherung> sicherung,
                                                      int idType, string bezeichner)
        {
            foreach (SpVariantenSicherung s in sicherung)
                if (s.ID_Type == idType &&
                    string.Equals(s.Bezeichner, bezeichner, StringComparison.OrdinalIgnoreCase))
                    return s;

            return null;
        }

        private static int SpZahl(DataRow r, string spalte)
        {
            return (r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value)
                ? Convert.ToInt32(r[spalte]) : 0;
        }

        private static string SpText(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            return (r[spalte].ToString() ?? "").Trim();
        }

        // =================================================================================
        //  S1 - Rettung der SENKENLISTE ueber den Del+Add-Speicherweg
        // =================================================================================
        //
        // DIESELBE FALLE WIE BEI DEN SPEICHERVARIANTEN (AP9b), EIN GEWERK WEITER. Seit
        // Migrationsschritt 50 haengt an jeder Erzeuger-Anlage eine geordnete Senkenliste
        // in Z_AnlageSenke, verbunden ueber ID_Anlage und mit Loeschweitergabe
        // (FK_AnlageSenke_Anlage). Die Loeschweitergabe ist dort nicht wahlweise, sondern
        // zwingend: Restriktiv scheiterte bereits das DELETE des Speicherwegs, und es
        // liesse sich kein Projekt mehr speichern (gemessen 27.08.2026, Begruendung bei
        // SchemaMigration.SQL_FK_SENKE_ANLAGE). Der Preis dafuer ist genau der, den AP9b
        // fuer die Speichervarianten schon einmal bezahlt hat: Ohne Gegenmassnahme raeumte
        // JEDES Speichern - ueber Karte, Kontextmenue oder Wizard - die komplette
        // Senkenkonfiguration des Projekts ab. Die PUFFER-Anlagenzeilen selbst brauchen
        // keine Rettung mehr: Der typlose Loeschweg verschont ID_Type 12 (FR-1, Kommentar
        // an Del_Projekt_Waermeerzeuger), ihre Ids und damit ihre Senkenzeilen bleiben
        // stehen.
        //
        // ZUORDNUNG UEBER (ID_Type, Bezeichner), wortgleich zur Variantenrettung: Die
        // alte Anlagen-Id ist nach dem Loeschen wertlos (AutoWert), die Geraete-Id
        // nicht eindeutig. Wer eine Anlage im Dialog UMBENENNT, verliert ihre Senken -
        // dieselbe Grenze wie bei CopyFromStamm und bei den Speichervarianten.
        //
        // BEIDE LOESCHWEGE sichern. Der typgefilterte Weg loescht nur die Anlagen eines
        // Gewerks; die Senken der uebrigen bleiben unangetastet und werden unten
        // uebergangen, weil ihre Anlagenzeile noch Senken FUEHRT. Die Sicherung kostet
        // dort also nichts und schuetzt den Fall, dass ein Aufrufer doch mehr loescht
        // als erwartet.

        /// <summary>Eine gesicherte Senkenliste samt ihrem Wiedererkennungsmerkmal.</summary>
        private sealed class SenkenSicherung
        {
            public int ID_Type;
            public string Bezeichner = "";
            public List<Z_AnlageSenkeModel> Senken = new List<Z_AnlageSenkeModel>();
        }

        /// <summary>Die Sicherung des laufenden Speichervorgangs; <c>null</c> = nichts zu retten.</summary>
        private List<SenkenSicherung> m_SenkenSicherung;

        /// <summary>Das Projekt, zu dem <see cref="m_SenkenSicherung"/> gehoert (siehe <see cref="m_SpVariantenProjekt"/>).</summary>
        private int m_SenkenProjekt;

        /// <summary>
        /// Sichert die Senkenlisten des Projekts - <b>nur im Arbeitsspeicher</b>. Fehlt
        /// die Tabelle (Migrationsschritt 50 noch nicht gelaufen), gibt es nichts zu
        /// sichern und nichts zu tun.
        /// </summary>
        private void SenkenSichern(int projektID)
        {
            m_SenkenSicherung = null;
            m_SenkenProjekt = 0;

            if (projektID <= 0 || !Z_AnlageSenkeCtrl.SpalteVorhanden()) return;

            try
            {
                List<Z_AnlageSenkeModel> alle = new Z_AnlageSenkeCtrl().LesenJeProjekt(projektID);
                if (alle.Count == 0) return;

                // Die Merkmale der Anlagen EINMAL lesen - die Senkenzeilen fuehren nur
                // die Anlagen-Id, wiedererkannt wird aber ueber (ID_Type, Bezeichner).
                Dictionary<int, SenkenSicherung> jeAnlage = new Dictionary<int, SenkenSicherung>();
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, ID_Type, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID",
                    new DbParam("@pID", projektID));

                if (dt == null || dt.Rows.Count == 0) return;

                foreach (DataRow r in dt.Rows)
                {
                    int idAnlage = SpZahl(r, "ID");
                    if (idAnlage <= 0) continue;

                    jeAnlage[idAnlage] = new SenkenSicherung
                    {
                        ID_Type = SpZahl(r, "ID_Type"),
                        Bezeichner = SpText(r, "Bezeichner")
                    };
                }

                foreach (Z_AnlageSenkeModel z in alle)
                {
                    SenkenSicherung s;
                    if (jeAnlage.TryGetValue(z.ID_Anlage, out s)) s.Senken.Add(z);
                }

                List<SenkenSicherung> sicherung = new List<SenkenSicherung>();
                foreach (SenkenSicherung s in jeAnlage.Values)
                {
                    if (s.Senken.Count == 0) continue;

                    // Doppelte Bezeichner sind im Schema moeglich - die erste Zeile
                    // gewinnt, wie bei der Variantenrettung.
                    if (SenkenTreffer(sicherung, s.ID_Type, s.Bezeichner) != null)
                    {
                        Console.WriteLine("Senken-Rettung: \"" + s.Bezeichner + "\" kommt im Projekt " +
                                          projektID + " mehrfach vor - gesichert wird die erste Zeile, " +
                                          "die Senken der weiteren gehen verloren.");
                        continue;
                    }

                    sicherung.Add(s);
                }

                if (sicherung.Count > 0)
                {
                    m_SenkenSicherung = sicherung;
                    m_SenkenProjekt = projektID;
                }
            }
            catch (Exception ex)
            {
                m_SenkenSicherung = null;
                m_SenkenProjekt = 0;
                Console.WriteLine("Die Senkenlisten konnten vor dem Loeschen nicht gesichert " +
                                  "werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Schreibt die gesicherten Senkenlisten auf die NEUEN Anlagenzeilen zurueck.
        ///
        /// <para>
        /// Geschrieben wird ausschliesslich auf Anlagen, die JETZT keine Senkenzeile
        /// fuehren. Damit ist die Methode idempotent, sie ueberschreibt nichts, was der
        /// Dialog gerade gespeichert hat, und eine im Dialog neu hinzugekommene Anlage
        /// bleibt ohne Senke - dort gilt wie bisher die Rueckfallregel
        /// <c>Heizkreis</c>/<c>Beides</c>.
        /// </para>
        ///
        /// <para>
        /// <b>Vor dem Aufraeumlauf.</b> <c>Z_AnlageSenke.ID_Puffer</c> zaehlt fuer
        /// <c>GeraeteWaisen</c> als Verweis auf den Speicher - ab der dritten Senke
        /// sogar als EINZIGER. Stuende die Rettung dahinter, loeschte der Aufraeumlauf
        /// genau die Puffer, deren Senkenzeile eine Zeile spaeter zurueckkaeme.
        /// </para>
        ///
        /// <para><b>BEST EFFORT</b> - ein gelungenes Speichern scheitert nicht daran.</para>
        /// </summary>
        private void SenkenWiederherstellen(int projektID)
        {
            List<SenkenSicherung> sicherung = m_SenkenSicherung;
            int projektDerSicherung = m_SenkenProjekt;

            m_SenkenSicherung = null;                 // eine Sicherung, ein Wiederherstellen
            m_SenkenProjekt = 0;

            if (sicherung == null || sicherung.Count == 0 || projektID <= 0) return;

            if (projektDerSicherung != projektID)
            {
                Console.WriteLine("Senken-Rettung nicht ausgefuehrt: Die Sicherung gehoert zu " +
                                  "Projekt " + projektDerSicherung + ", geschrieben wird aber " +
                                  "Projekt " + projektID + ".");
                return;
            }

            try
            {
                Z_AnlageSenkeCtrl ctrl = new Z_AnlageSenkeCtrl();

                // Wer fuehrt jetzt schon Senken? Ein Aufruf statt einer Abfrage je Anlage.
                HashSet<int> hatSenken = new HashSet<int>();
                foreach (Z_AnlageSenkeModel z in ctrl.LesenJeProjekt(projektID))
                    hatSenken.Add(z.ID_Anlage);

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, ID_Type, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ? ORDER BY ID",
                    new DbParam("@pID", projektID));

                if (dt == null || dt.Rows.Count == 0) return;

                int wieder = 0;
                foreach (DataRow r in dt.Rows)
                {
                    int idAnlage = SpZahl(r, "ID");
                    if (idAnlage <= 0 || hatSenken.Contains(idAnlage)) continue;

                    SenkenSicherung treffer = SenkenTreffer(sicherung, SpZahl(r, "ID_Type"),
                                                            SpText(r, "Bezeichner"));
                    if (treffer == null) continue;

                    // Der Puffer einer geretteten Senke kann zwischenzeitlich fort sein
                    // (im Dialog entfernt). Die Referenz faellt dann weg statt die ganze
                    // Zeile - dieselbe Normalisierung, die WaermesenkeClass beim Lesen
                    // vornimmt, und derselbe Schutz wie in PufferFkOderNull: Ein
                    // gescheitertes Insert hier haette die Anlage ohne jede Senke
                    // zurueckgelassen.
                    Dictionary<int, bool> pufferCache = new Dictionary<int, bool>();
                    List<Z_AnlageSenkeModel> zeilen = new List<Z_AnlageSenkeModel>();

                    foreach (Z_AnlageSenkeModel alt in treffer.Senken)
                    {
                        if (alt.ID_Puffer > 0 && !PufferVorhanden(alt.ID_Puffer, pufferCache))
                        {
                            Console.WriteLine("Senken-Rettung: \"" + treffer.Bezeichner +
                                              "\", Rang " + alt.Rang + " zeigte auf den Puffer " +
                                              alt.ID_Puffer + ", den es nicht mehr gibt - die " +
                                              "Referenz wird als leer gespeichert.");
                            alt.ID_Puffer = 0;
                        }

                        alt.ID = 0;                   // frische Zeile, neuer AutoWert
                        alt.ID_Anlage = idAnlage;
                        zeilen.Add(alt);
                    }

                    if (ctrl.SchreibenJeAnlage(idAnlage, zeilen)) wieder += zeilen.Count;
                }

                if (wieder > 0)
                    Console.WriteLine("Senken-Rettung: " + wieder + " Senkenzeile(n) des Projekts " +
                                      projektID + " wiederhergestellt.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Senkenlisten konnten nicht wiederhergestellt werden: " +
                                  ex.Message);
            }
        }

        /// <summary>
        /// Die Sicherung zu (<paramref name="idType"/>, <paramref name="bezeichner"/>),
        /// oder <c>null</c>. Verglichen wird wie in <see cref="SpTreffer"/>.
        /// </summary>
        private static SenkenSicherung SenkenTreffer(List<SenkenSicherung> sicherung,
                                                     int idType, string bezeichner)
        {
            foreach (SenkenSicherung s in sicherung)
                if (s.ID_Type == idType &&
                    string.Equals(s.Bezeichner, bezeichner, StringComparison.OrdinalIgnoreCase))
                    return s;

            return null;
        }

        public bool Add_WP_Waermeerzeuger(int projektID, List<WErzeugerModel> list)
        {
            try
            {
                // Ein Zwischenspeicher fuer den ganzen Durchlauf: dieselbe Puffer-ID
                // taucht in mehreren Anlagen auf und muss nicht mehrfach geprueft werden.
                Dictionary<int, bool> pufferCache = new Dictionary<int, bool>();

                // EINE ZEILE JE PROJEKT UND GERAET (Teil A der Anlagenzeilen-Eindeutigkeit).
                //
                // WARUM DIE PRUEFUNG HIER STEHT UND NICHT NUR IM DIALOG. Dies ist der EINE
                // Schreibweg aller Erzeuger - Wizard, Startseitenkarten, Kontextmenues und
                // das Simulationsdetail laufen samt und sonders hier hindurch. Eine
                // Pruefung je Dialog waere zwoelfmal dieselbe Wahrheit, und der
                // dreizehnte Dialog haette sie wieder nicht.
                //
                // WARUM DIE BELEGUNG UND NICHT NUR EIN SELECT. Der Weg ist Loeschen +
                // Neuanlegen: Beim Eintritt sind die alten Anlagenzeilen bereits fort, die
                // neuen noch nicht alle da. Die Dublette entsteht INNERHALB der Liste -
                // zwei Eintraege gleichen Bezeichners loesen ueber CopyFromStamm auf
                // dieselbe Projektkopie auf - und genau dort wird sie hier erkannt.
                AnlagenEindeutigkeit.Belegung belegt = new AnlagenEindeutigkeit.Belegung();
                List<WErzeugerModel> geschrieben = new List<WErzeugerModel>();
                bool feldHinweisGezeigt = false;

                foreach (var item in list)
                {
                    // Ä24: Gerätestand VOR der Materialisierung merken — tauscht
                    // dieser Lauf die Gerätekopie der Anlage (CopyFromStamm nach
                    // Neuwahl/Umbenennung, Duplikat-Gerätekopie), ziehen die
                    // KOSTENANKER ihrer Positionen unten mit um. Ohne den Umzug
                    // zeigten sie auf die alte Kopie, die GeraeteWaisen.Aufraeumen
                    // abräumt — die Selbstheilung löste die Zuordnung dann
                    // „ehrlich“ und die Kosten der Anlage standen als „ohne
                    // Anlagenzuordnung“ da (Befund 27.08.2026, Projekt 1037).
                    // FR-5: Frisch im Dialog aufgenommene Zeilen tragen eine VORLAEUFIGE
                    // Id ab 100000 (Hausmuster startindex, siehe
                    // WizardItemClass.ID_UNGESPEICHERT_START) — sie ist KEINE Anlagen-Id.
                    // Ohne die Normalisierung zöge der Kostenanker-Umzug unten die
                    // Positionen einer ECHTEN Anlage um, sobald die AutoWerte diese
                    // Marke erreichen.
                    int anlageAlt = (item.ID >= WizardItemClass.ID_UNGESPEICHERT_START) ? 0 : item.ID;
                    int wpAlt = item.ID_WP, bhkwAlt = item.ID_BHKW, kesselAlt = item.ID_Kessel,
                        spAlt = item.ID_SP, pufferAlt = item.ID_PUFFER, pvAlt = item.ID_PV,
                        solarAlt = item.ID_Solar;

                    // Gesperrte Verweisspalte dieses Anlagentyps (null = keine Sperre).
                    string sperrSpalte = null;
                    // Stammdatensatz der jeweiligen Energieanlage bei Bedarf ins Projekt kopieren
                    // (Dispatch ueber ID_Type / Tab_Typ_Energieanlagen). Idempotent (dedup per Bezeichner + Projekt).
                    // Weitere Typen (Heizkessel, PV, Stromspeicher, Solar, Pufferspeicher) hier analog ergaenzen,
                    // sobald deren CopyFromStamm vorhanden ist.
                    if (CheckType(item, WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP))
                    {
                        int idWp = new WPCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idWp > 0) item.ID_WP = idWp;
                        sperrSpalte = AnlagenEindeutigkeit.SPALTE_WP;
                    }
                    else if (item.ID_Type == WizardItemClass.BHKW_TYP)
                    {
                        int idBhkw = new BHKWCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idBhkw > 0) item.ID_BHKW = idBhkw;
                        sperrSpalte = AnlagenEindeutigkeit.SPALTE_BHKW;
                    }
                    else if (CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP))
                    {
                        int idKessel = new HeizkesselCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idKessel > 0) item.ID_Kessel = idKessel;
                        sperrSpalte = AnlagenEindeutigkeit.SPALTE_KESSEL;
                    }
                    else if (CheckType(item, WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP))
                    {
                        int idSp = new StromspeicherCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idSp > 0) item.ID_SP = idSp;

                        // KEINE Geraetesperre: eine zweite Zeile auf denselben Speicher ist
                        // eine weitere VARIANTE (Fachkonzept Stromspeicher 7.3). Was auch
                        // dort nicht vorkommen darf, sind zwei Varianten GLEICHEN NAMENS -
                        // SpVariantenWiederherstellen ordnet die geretteten
                        // Betriebsparameter ueber (ID_Type, Bezeichner) zu und traefe sonst
                        // immer dieselbe Zeile. Die Pruefung stammt aus
                        // StromspeicherKontextMenuCtrl.VarianteAnlegen und gilt jetzt auch
                        // hier; nur kann sie an dieser Stelle nicht abbrechen (das DELETE
                        // ist bereits gelaufen), sondern vergibt ein Suffix.
                        item.Bezeichner = AnlagenEindeutigkeit.SpeichervarianteBenennen(item.Bezeichner, belegt);
                        belegt.NameMerken(item.Bezeichner);
                    }
                    else if (item.ID_Type == WizardItemClass.PUFFER_TYP)
                    {
                        int idPuf = new PufferSpCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        // Seit Schritt 4 der SchemaMigration hat ID_PUFFER eine erzwungene
                        // Beziehung auf Tab_Pufferspeicher.ID. Scheitert die Auflösung, darf
                        // die alte ID nicht stehen bleiben - Form_PufferSp schreibt dort die
                        // STAMM-ID (Konzept 2.3), und die verletzt die Beziehung. 0 bedeutet
                        // "kein Puffer" und wird unten als NULL geschrieben.
                        //
                        // ABER: CopyFromStamm sucht den Bezeichner im KATALOG
                        // (Tab_Pufferspeicher_STAMM). Ein Projekt-Puffer, der dort nicht
                        // steht - umbenannt oder frei angelegt, etwa "Vitocell 140-E 600
                        // Liter" gegenüber dem Katalognamen "Vitocell 140-E 600 Ltr" -
                        // ergibt -1, und die Anlage verlor ihren Speicher bei JEDEM
                        // Speichern (gemessen an 1023/1024: drei von sechs Puffer-Anlagen).
                        // Eine bereits vorhandene ID_PUFFER bleibt deshalb stehen, wenn sie
                        // auf eine Projektkopie DIESES Projekts zeigt. Genau diese
                        // Bedingung schließt den Fall aus, vor dem der 0-Rückfall schützen
                        // soll: eine STAMM-ID trägt kein ID_Projekt dieses Projekts.
                        if (idPuf <= 0 && item.ID_PUFFER > 0 &&
                            PufferGehoertZuProjekt(item.ID_PUFFER, projektID))
                            idPuf = item.ID_PUFFER;

                        item.ID_PUFFER = (idPuf > 0) ? idPuf : 0;
                        sperrSpalte = AnlagenEindeutigkeit.SPALTE_PUFFER;
                    }
                    else if (CheckType(item, WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP))
                    {
                        int idPv = new PhotovoltaikCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idPv > 0) item.ID_PV = idPv;

                        // KEINE Sperre: mehrere Felder desselben Modultyps sind richtig -
                        // die Engine rechnet PV und Solarthermie bewusst je Zeile. Gemeldet
                        // wird nur die exakte Wiederholung (Neigung UND Azimut UND
                        // Modulanzahl gleich), und auch die nur als Hinweis.
                        if (!feldHinweisGezeigt)
                            feldHinweisGezeigt = AnlagenEindeutigkeit.FeldHinweisPruefen(item, geschrieben);
                    }
                    else if (CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP))
                    {
                        int idSol = new SolarkollektorenCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idSol > 0) item.ID_Solar = idSol;

                        if (!feldHinweisGezeigt)
                            feldHinweisGezeigt = AnlagenEindeutigkeit.FeldHinweisPruefen(item, geschrieben);
                    }

                    // --- EINE ZEILE JE PROJEKT UND GERAET -----------------------------
                    // Zeigt bereits eine Zeile dieses Projekts auf dasselbe Geraet, fragt
                    // Aufnehmen nach und legt bei "Ja" eine eigene Geraetekopie an (dabei
                    // wandert auch der Bezeichner der Anlagenzeile auf den neuen Namen).
                    // 0 heisst "der Anwender will die Zeile nicht" - sie wird uebergangen.
                    if (sperrSpalte != null)
                    {
                        GeraeteSperre sperre = AnlagenEindeutigkeit.Sperre(sperrSpalte);
                        int idAlt = Verweis(item, sperrSpalte);

                        if (idAlt > 0)
                        {
                            int idNeu = AnlagenEindeutigkeit.Aufnehmen(
                                sperre, projektID, idAlt, item, belegt, item.GeraetekopieErzwingen);

                            if (idNeu <= 0) continue;   // Aufnahme verworfen
                            if (idNeu != idAlt) VerweisSetzen(item, sperrSpalte, idNeu);
                        }
                    }

                    // Ä24: Kostenanker der Anlage auf die neue Gerätekopie umziehen —
                    // NUR die Positionen DIESER Anlagenzeile (item.ID); höchstens einer
                    // der sieben Aufrufe sieht einen echten Wechsel.
                    KostenAnkerUmziehen(projektID, anlageAlt, wpAlt, item.ID_WP);
                    KostenAnkerUmziehen(projektID, anlageAlt, bhkwAlt, item.ID_BHKW);
                    KostenAnkerUmziehen(projektID, anlageAlt, kesselAlt, item.ID_Kessel);
                    KostenAnkerUmziehen(projektID, anlageAlt, spAlt, item.ID_SP);
                    KostenAnkerUmziehen(projektID, anlageAlt, pufferAlt, item.ID_PUFFER);
                    KostenAnkerUmziehen(projektID, anlageAlt, pvAlt, item.ID_PV);
                    KostenAnkerUmziehen(projektID, anlageAlt, solarAlt, item.ID_Solar);

                    // Anweisung und Parameter stehen zentral (siehe SQL_ANLAGE_INSERT):
                    // dieselbe Wahrheit, die auch WErzeugerCtrl.Insert benutzt.
                    if (!DataRepository.ExecuteSQL(SQL_ANLAGE_INSERT,
                                                   AnlagenParameter(projektID, item, pufferCache)))
                    {
                        SpVariantenVerwerfen("das Neuanlegen der Anlagen ist gescheitert");
                        return false;
                    }

                    // Ä24: Die frische Anlagen-Id (AutoWert) zurück ans
                    // Listenobjekt — die Session-Liste bleibt so über
                    // Folge-Speicherungen an ihrer Zeile (Kostenanker-Umzug oben,
                    // Kosten-Knöpfe der Detailansicht).
                    try
                    {
                        object neuAnlage = DataRepository.ExecuteScalar(
                            "SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = " + projektID);
                        if (neuAnlage != null && neuAnlage != DBNull.Value)
                            item.ID = Convert.ToInt32(neuAnlage);
                    }
                    catch { }

                    geschrieben.Add(item);
                }

                // AP9b: Erst jetzt, mit vollstaendig neu geschriebenen Anlagenzeilen, sind
                // die neuen IDs bekannt und die Betriebsparameter des Projektspeichers
                // koennen zurueck an ihre Variante (siehe Block ueber dieser Methode).
                SpVariantenWiederherstellen(projektID);

                // DIE ANDERE HAELFTE DES SPEICHERWEGS (Befund 22.08.2026).
                //
                // Del_Projekt_Waermeerzeuger + diese Methode schreiben die ANLAGENZEILEN
                // neu und fassen die Geraetetabellen nicht an. Wer ein Geraet abwaehlt
                // oder gegen ein anderes tauscht, liess dessen Projektkopie in Tab_WP &
                // Co. also stehen - unerreichbar, aber mitgezaehlt von jeder Auswertung,
                // die noch ueber WHERE ID_Projekt = ? liest (WirtschaftlichkeitCtrl
                // summiert SUM(Pel) ueber Tab_BHKW, sucht den groessten Kessel ueber
                // ORDER BY Ptherm DESC; WaermesenkeClass.ProjektPufferListe fuellt die
                // Speicherauswahl). Auf der Arbeitskopie standen so 218 WP-Zeilen in
                // Projekt 1023, verbaut waren zwei.
                //
                // WARUM HIER UND NICHT IN Del_Projekt_Waermeerzeuger. Dort waeren die
                // Geraetezeilen VOR dem Neuschreiben weg, und das anschliessende
                // CopyFromStamm muesste sie aus dem KATALOG neu holen: Projektbezogene
                // Aenderungen (Investitionskosten, Vor-/Ruecklauf, Schwellen des Puffers)
                // waeren bei jedem Speichern verloren, und ein Projektgeraet, das im
                // Katalog nicht mehr steht, kaeme gar nicht wieder - genau der Fall, den
                // der ID_PUFFER-Rueckfall weiter oben abfaengt. Nach dem Schreiben ist
                // dagegen zweifelsfrei, was noch gebraucht wird.
                //
                // BEST EFFORT: Der Aufraeumlauf kann ein gelungenes Speichern nicht mehr
                // scheitern lassen. Was er stehen laesst, holt der Migrationsschritt.

                // Ä25 (Nutzerbefund 27.08.2026, „Pufferkosten verschwinden“):
                // ZUORDNUNG UND ANKER NACHZIEHEN - und zwar VOR dem Aufraeumlauf.
                //
                // Loeschen + Neuanlegen hat allen Kostenpositionen des Projekts die
                // Anlagen-Id unter den Fuessen weggezogen (neue AutoWerte). Geheilt
                // wurde das bisher erst beim naechsten UI-Aufbau ueber
                // KostenProjektPositionenCtrl.ZuordnungReparieren (Kosten-Seite,
                // Kostenverwaltung) - die Heilung laeuft ueber den GERAETEANKER.
                // Genau dieses Zeitfenster ist die Gefahr: GeraeteWaisen.Aufraeumen
                // direkt darunter loescht Geraetekopien, auf die keine Anlagenzeile
                // mehr zeigt, und mit der Geraetezeile stirbt der Anker. Danach ist
                // die Zuordnung nicht mehr herleitbar und die Position steht als
                // „ohne Anlagenzuordnung" da - beim Pufferspeicher der haeufigste
                // Fall, weil ein Projekt dort mehrere Anlagen fuehrt und der
                // Speichersatz oft komplett neu gesetzt wird.
                //
                // An DIESER Stelle stehen die alten Geraetezeilen noch:
                // ZuordnungReparieren findet ueber den Anker die neue Anlagenzeile,
                // AnkerNachziehen schreibt den Anker danach aus ihr neu - dieselbe
                // Ableitung, die Migrationsschritt 47 einmalig fuer den Bestand
                // macht. Ein LAUFZEIT-Nachzug statt eines neuen Migrationsschritts,
                // weil der Nummernblock ab 48 bereits von den Puffer-Paketen belegt
                // ist.
                //
                // GRENZE (bewusst): Entfernt der Anwender eine Anlage ganz aus der
                // Verwaltung, gibt es keine Zeile mehr, auf die zu heilen waere -
                // die Zuordnung wird dann wie bisher ehrlich geloest (gelbe Zeile).
                // BEST EFFORT - ein gelungenes Speichern scheitert daran nicht.
                try
                {
                    KostenProjektPositionenCtrl.ZuordnungReparieren(projektID);
                    KostenProjektPositionenCtrl.AnkerNachziehen(projektID);
                }
                catch { }

                // S1: Die Senkenlisten auf die NEUEN Anlagenzeilen zurueck - VOR dem
                // Aufraeumlauf, weil Z_AnlageSenke.ID_Puffer dort als Verweis zaehlt
                // (Begruendung im Block ueber SenkenSichern).
                SenkenWiederherstellen(projektID);

                // ETAPPE H3 (H1-3): Pflichtpositionen der Standardvorlagen an jeder
                // Anlagenzeile sicherstellen - NACH ZuordnungReparieren/AnkerNachziehen
                // (die Bestandspositionen haengen dann wieder an den neuen Anlagen-Ids,
                // der NurAnlegen-Dublettencheck je Anlage greift und es entstehen keine
                // Doppel). Saetze bleiben leer, der Lauf ist ergebnisneutral - es
                // entsteht nur Struktur. BEST EFFORT wie die Nachbarn: ein gelungenes
                // Speichern scheitert nicht an der Kostenseite.
                try { KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(projektID); }
                catch { }

                GeraeteWaisen.Aufraeumen(projektID);

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                SpVariantenVerwerfen("beim Neuanlegen der Anlagen kam es zu einem Fehler");
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Ä24: Zieht die Geräteanker der Kostenpositionen EINER Anlagenzeile auf
        /// die neue Gerätekopie um (Gerätetausch im Del+Add-Speicherweg). Die
        /// ID_Anlage-Seite heilt anschließend
        /// <c>KostenProjektPositionenCtrl.ZuordnungReparieren</c> über den Anker.
        /// Best effort — ein Speichern scheitert daran nicht.
        /// </summary>
        private static void KostenAnkerUmziehen(int projektID, int anlageAlt, int geraetAlt, int geraetNeu)
        {
            if (projektID <= 0 || anlageAlt <= 0 || geraetAlt <= 0 || geraetNeu <= 0 ||
                geraetAlt == geraetNeu) return;
            try
            {
                if (!KostenPositionCtrl.StelleSpaltenSicher()) return;
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = ? " +
                    "WHERE ProjektID = ? AND ID_Anlage = ? AND ID_AnlageGeraet = ?",
                    new DbParam("@neu", geraetNeu),
                    new DbParam("@p", projektID),
                    new DbParam("@a", anlageAlt),
                    new DbParam("@g", geraetAlt));
            }
            catch { }
        }

        // Kleine Hilfsfunktion für die Typprüfung - Weiterleitung auf AnlagenSql (K6),
        // damit Parameterfabrik und Wizard-Zweige dieselbe Regel benutzen.
        private static bool CheckType(WErzeugerModel item, int typ, int refTyp)
        {
            return AnlagenSql.CheckType(item, typ, refTyp);
        }

        /// <summary>
        /// Liest den Geräteverweis einer gesperrten Spalte aus dem Modell. Zwei
        /// Zuordnungen an einer Stelle - der Gegenpart ist <see cref="VerweisSetzen"/>.
        /// </summary>
        private static int Verweis(WErzeugerModel item, string spalte)
        {
            if (spalte == AnlagenEindeutigkeit.SPALTE_WP) return item.ID_WP;
            if (spalte == AnlagenEindeutigkeit.SPALTE_KESSEL) return item.ID_Kessel;
            if (spalte == AnlagenEindeutigkeit.SPALTE_BHKW) return item.ID_BHKW;
            if (spalte == AnlagenEindeutigkeit.SPALTE_PUFFER) return item.ID_PUFFER;
            return 0;
        }

        /// <summary>Setzt den Geräteverweis einer gesperrten Spalte im Modell.</summary>
        private static void VerweisSetzen(WErzeugerModel item, string spalte, int id)
        {
            if (spalte == AnlagenEindeutigkeit.SPALTE_WP) item.ID_WP = id;
            else if (spalte == AnlagenEindeutigkeit.SPALTE_KESSEL) item.ID_Kessel = id;
            else if (spalte == AnlagenEindeutigkeit.SPALTE_BHKW) item.ID_BHKW = id;
            else if (spalte == AnlagenEindeutigkeit.SPALTE_PUFFER) item.ID_PUFFER = id;
        }
        
        /// <summary>
        /// Legt die PROJEKTGEBUNDENEN Energieträgersätze zu den Anlagen einer Wizard-Auswahl
        /// an: je DISTINKTEM <c>ID_Carrier</c> ein Paar aus <c>energy_price</c> (Preishistorie)
        /// und <c>energy_Project_settings</c> (Projekteinstellungen) - dasselbe Datenbild, das
        /// eine Zuordnung über den Kosten-Dialog erzeugt (<c>Form_Kosten.CreateNewEnergyCarrier</c>).
        ///
        /// WARUM HIER UND NICHT IM FORMULAR. Beide Tabellen haben eine erzwungene Beziehung auf
        /// <c>Tab_Projekt.ID</c>. Im Neuanlage-Wizard existiert die Projektzeile beim Auswählen
        /// von Kessel oder BHKW aber noch nicht - <c>WizardParent</c> führt dort nur eine
        /// GERATENE ID (<c>ProjektCtrl.GetMaxID() + 1</c>), die echte entsteht erst in
        /// <c>Add_Projekt</c> über @@IDENTITY. Die Formulare legen deshalb nur noch den
        /// KATALOG-Träger an (<c>energy_carrier</c>, projektfrei) und merken dessen ID am Modell
        /// vor; die projektgebundenen Sätze entstehen erst hier, mit der echten Projekt-ID.
        ///
        /// WERTEHERKUNFT. Aus der Katalogzeile kommt nur <c>ID_Brennstoff</c>; die PREISE und
        /// Heizwerte stammen danach aus <c>Tab_Brennstoff_Stamm</c> - exakt die Quellen, aus
        /// denen auch der Kosten-Weg liest: <c>Form_Kosten_Auswahl</c> holt Hi, Hs und Einheit
        /// von dort, <c>Form_Kosten</c> die Standardpreise. <c>ID_Umrechnung</c> ist im Dialog
        /// ebenfalls abgeleitet (Brennstoff + Einheit) und wird hier genauso ermittelt. Die
        /// EMISSIONEN werden seit dem Anwenderentscheid vom 30.08.2026 NICHT mehr mitkopiert -
        /// Begründung im Block vor dem INSERT in <see cref="TraegerSatzAnlegen"/>.
        ///
        /// IDEMPOTENT. Derselbe COUNT-Test wie im Kosten-Dialog verhindert doppelte Sätze - er
        /// trägt den Bearbeiten-Zweig des Wizards, der bei jedem Speichern erneut durchläuft.
        ///
        /// SEIT 30.08.2026 ZWEI QUELLEN (Anwenderentscheid). Erstens wie bisher jeder
        /// distinkte ID_Carrier der Anlagenliste - das sind im Bestand ausschliesslich
        /// Brenner. Zweitens der STANDARD-STROMTRAEGER
        /// (ProjektEnergietraegerCtrl.StandardStromTraeger), sobald das Projekt eine
        /// Waermepumpe, eine Photovoltaikanlage, einen Stromspeicher oder einen Heizstab
        /// fuehrt: Diese Gewerke tragen keinen eigenen Traegerverweis und blieben deshalb
        /// bisher ohne Zuordnung. Beide Quellen schreiben ueber dieselbe Mechanik
        /// (<see cref="TraegerSatzAnlegen"/>), also mit denselben Stammwerten.
        /// </summary>
        public bool Add_Projekt_Energietraeger(int projektID, List<WErzeugerModel> list)
        {
            if (projektID <= 0 || list == null) return true;

            // je Träger nur EIN Satz, auch wenn mehrere Anlagen denselben Träger nutzen
            List<int> erledigt = new List<int>();

            foreach (var item in list)
            {
                int carrierId = item.ID_Carrier;
                if (carrierId <= 0 || erledigt.Contains(carrierId)) continue;
                erledigt.Add(carrierId);

                if (!TraegerSatzAnlegen(projektID, carrierId)) return false;
            }

            // ---------------------------------------------------------------------
            // ANWENDERENTSCHEID 30.08.2026 — die elektrische Welt bekommt ihren Träger
            // ---------------------------------------------------------------------
            //
            // BEFUND. Die Schleife oben kennt nur ID_Carrier, und den tragen im
            // Bestand ausschliesslich Brenner (BHKW, Heizkessel). Waermepumpe,
            // Photovoltaik und Stromspeicher fuehren keinen eigenen Traegerverweis -
            // die Automatik ordnete ihnen deshalb NIE einen Traeger zu. Auf der
            // Kostenseite fehlte danach der Strom: keine Zeile in der
            // Traegertabelle, kein Preis, keine Emissionen, und die
            // Emissionsrechnung fiel still auf den Strommix-Vorgabewert zurueck
            // (KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH).
            //
            // ERKENNUNG UEBER DEN GERAETEVERWEIS, nicht ueber ID_Type. Die
            // Typ-Landkarte (WizardItemClass: 1 = WP, 3 = PV, 4 = Stromspeicher)
            // fuehrt zu jedem dieser Gewerke eine zweite, REFERENZ-Nummer (7/9/6),
            // und der Heizstab ist ueberhaupt kein eigener Typ, sondern ein Merkmal
            // der Anlagenzeile. Geprueft wird deshalb genau das, was auch
            // ProjektEnergietraegerCtrl.Verwendete prueft: ID_WP / ID_PV / ID_SP > 0
            // oder gesetzter Heizstab. Nur so nennen Automatik und Anzeige dieselbe
            // Menge - sonst bliebe die rote Fehlzeile der Kostenseite nach dem
            // Speichern stehen.
            //
            // IDEMPOTENT auf zwei Ebenen: "erledigt" faengt den Fall ab, dass eine
            // Anlage den Stromtraeger schon ueber ID_Carrier beigetragen hat, und
            // TraegerSatzAnlegen selbst prueft per COUNT gegen
            // energy_Project_settings. Zweiter Wizard-Save = keine zweite Zeile.
            if (BrauchtStromTraeger(list))
            {
                int stromId = ProjektEnergietraegerCtrl.StandardStromTraeger(projektID);
                if (stromId > 0 && !erledigt.Contains(stromId))
                {
                    erledigt.Add(stromId);
                    if (!TraegerSatzAnlegen(projektID, stromId)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Führt das Projekt mindestens einen Erzeuger, dessen Energie ELEKTRISCH ist?
        /// Wärmepumpe, Photovoltaik, Stromspeicher — oder eine beliebige Anlage mit
        /// gesetztem Heizstab. Dieselbe Bedingung wie in
        /// <c>ProjektEnergietraegerCtrl.Verwendete</c> (siehe Begründung dort).
        /// </summary>
        private static bool BrauchtStromTraeger(List<WErzeugerModel> list)
        {
            foreach (var item in list)
            {
                if (item == null) continue;
                if (item.ID_WP > 0 || item.ID_PV > 0 || item.ID_SP > 0 || item.Heizstab)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Legt das Satzpaar <c>energy_price</c> + <c>energy_Project_settings</c> für
        /// EINEN Träger an — die Mechanik, die bis zum 30.08.2026 inline in
        /// <see cref="Add_Projekt_Energietraeger"/> stand. Herausgezogen, damit die
        /// Brenner-Zuordnung und die neue Stromträger-Zuordnung nachweislich
        /// DIESELBE Zeile schreiben (Werteherkunft, Rundung, Umrechnungssatz).
        ///
        /// <para>Rückgabe <c>false</c> nur bei einem echten Schreibfehler. Eine
        /// fehlende Katalogzeile und ein bereits zugeordneter Träger sind kein
        /// Fehler — dann gibt es schlicht nichts anzulegen.</para>
        /// </summary>
        private bool TraegerSatzAnlegen(int projektID, int carrierId)
        {
            object oBrennstoff = DataRepository.ExecuteScalar(
                "SELECT ID_Brennstoff FROM energy_carrier WHERE id = ?",
                new DbParam[] { new DbParam("@cid", carrierId) });
            if (oBrennstoff == null) return true;   // Katalogzeile fehlt -> nichts anzulegen
            int idBrennstoff = Convert.ToInt32(oBrennstoff);

            // Default-Werte aus dem Brennstoff-Stamm (nur noch PREISE — zu den
            // Emissionen siehe den Block vor dem INSERT der Projekt-Einstellungen)
            double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", idBrennstoff));
            double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", idBrennstoff));
            double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", idBrennstoff));

            // Hi, Hs und Abrechnungseinheit - im Kosten-Dialog die Felder
            // SelectedHi / SelectedHs / SelectedBillingUnit aus derselben Stammzeile
            double hi = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Hi", idBrennstoff));
            double hs = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Hs", idBrennstoff));
            object oEinheit = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Einheit", idBrennstoff);
            string einheit = (oEinheit != null) ? oEinheit.ToString() : "";

            int convId = ConvIdErmitteln(idBrennstoff, einheit);

            // Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln
            object oVorhanden = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam[] {
                    new DbParam("@pid", projektID),
                    new DbParam("@eid", carrierId)
                });
            if (oVorhanden != null && Convert.ToInt32(oVorhanden) > 0) return true;

            // Preis-Historie. leistungspreis wird ausdrücklich mitgeschrieben (Befund B5).
            string sqlHistory = @"INSERT INTO energy_price
                 (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                 VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
            if (!DataRepository.ExecuteSQL(sqlHistory, new DbParam[] {
                new DbParam("@cid",  carrierId),
                new DbParam("@prid", projektID),
                new DbParam("@ap",   Math.Round(default_arbeitspreis, 4)),
                new DbParam("@hi",   Math.Round(hi, 4)),
                new DbParam("@gp",   Math.Round(default_grundpreis, 4)),
                new DbParam("@date", DbParamTyp.Date) { Wert = DateTime.Now },
                new DbParam("@au",   einheit),
                new DbParam("@lp",   Math.Round(default_leistungspreis, 4))
            })) return false;

            // ---------------------------------------------------------------------
            // ANWENDERENTSCHEID 30.08.2026 („435 g/kWh") — KEINE Emissions-Stammkopie
            // ---------------------------------------------------------------------
            //
            // BEFUND (BK1 § 5). Bis hierher kopierte diese Stelle
            // Tab_Brennstoff_Stamm.CO2/SO2/NOx in die Projektzeile. Das ist keine
            // harmlose Vorbelegung: energy_project_settings.co2/so2/nox ist die
            // OBERSTE Ebene der Lesekette des EmissionsFaktorLaders und übersteuert
            // damit die AKTIVE Katalogzeile (emissionswert). Eine bloße Zuordnung
            // fror also den Stammwert im Projekt ein — Strom: Stamm 560 gegen
            // Katalog 435 (BAFA_EEW), Erdgas: Stamm 240 gegen Katalog 201 — und die
            // Pflege des Katalogs blieb an solchen Projekten wirkungslos.
            //
            // ENTSCHEID. Die KATALOGWAHRHEIT soll gelten. Die drei Emissionsspalten
            // bleiben deshalb bei der Neuanlage LEER; die Lesekette liefert dann von
            // selbst der Reihe nach: aktive Katalogzeile -> Tab_Brennstoff_Stamm ->
            // Altspalte energy_carrier. Es geht also nichts verloren (der Stamm ist
            // weiterhin Ebene 3), nur die Einfrierung entfällt: Die Zuordnung ist
            // emissionsneutral, und beide Rechner (KostenEmissionRechner,
            // EmissionsBilanzRechner) sehen dieselbe Wahrheit wie der Katalog.
            // Ein Projektwert entsteht künftig nur noch durch bewusste Pflege im
            // Projektkontext — genau das, was diese Ebene bedeuten soll.
            //
            // AUSDRÜCKLICH DBNull STATT WEGLASSEN. Die drei Spalten tragen in
            // Access den Spaltendefault 0; ein Weglassen aus der Spaltenliste
            // schriebe also eine 0 statt NULL. Zwar fällt auch eine 0 in der
            // Lesekette durch (dort gilt „gepflegt" = größer als 0), aber NULL ist
            // die ehrliche Aussage „nicht gepflegt" und die einzige, die sich in der
            // Datenbank von einem echten Nullwert unterscheiden lässt.
            //
            // NUR DIE EMISSIONEN. Preise (custom_price_work/_power/_base), Heizwerte
            // (custom_hi/custom_Hs), Abrechnungseinheit und ID_Umrechnung werden
            // weiterhin aus dem Stamm vorbelegt — dieselbe Vorbelegung erzeugt der
            // Kosten-Dialog, und der Entscheid galt allein den Emissionen.
            //
            // BESTANDSZEILEN BLEIBEN UNANGETASTET (kein Heilungsschritt): Die sieben
            // Projekte mit den alten 560/200/280 behalten ihre Werte, bis sie im
            // Projektkontext gepflegt werden.
            string sqlInsert = @"INSERT INTO energy_Project_settings
                 (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                  custom_price_base, ID_Umrechnung, co2, so2, nox)
                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            if (!DataRepository.ExecuteSQL(sqlInsert, new DbParam[] {
                new DbParam("@pid",    projektID),
                new DbParam("@eid",    carrierId),
                new DbParam("@p",      Math.Round(default_arbeitspreis, 4)),
                new DbParam("@pl",     Math.Round(default_leistungspreis, 4)),
                new DbParam("@h",      Math.Round(hi, 4)),
                new DbParam("@hs",     Math.Round(hs, 4)),
                new DbParam("@b",      Math.Round(default_grundpreis, 4)),
                new DbParam("@convid", convId),
                new DbParam("@co2",    DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@so2",    DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@nox",    DbParamTyp.Double) { Wert = DBNull.Value }
            })) return false;

            return true;
        }

        /// <summary>
        /// Dieselbe Ableitung wie <c>Form_Kosten_Auswahl.GetConvID</c>: Umrechnungssatz über
        /// Brennstoff und Abrechnungseinheit (from_unit = to_unit). -1, wenn es keinen gibt -
        /// genau der Wert, den der Dialog in diesem Fall ebenfalls schreibt.
        ///
        /// <para><c>internal</c> seit Etappe BK3: Der manuelle Zuordnungsweg
        /// <c>EnergietraegerKatalogCtrl.InsProjekt</c> (Ä10) benennt die Recheneinheit
        /// seiner neuen Zeile über DIESELBE Regel — beide Wege sollen denselben
        /// Umrechnungssatz eintragen.</para>
        /// </summary>
        internal static int ConvIdErmitteln(int idBrennstoff, string einheit)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?",
                new DbParam[] {
                    new DbParam("@cid", idBrennstoff),
                    new DbParam("@fu", einheit),
                    new DbParam("@tu", einheit)
                });
            return (o != null) ? Convert.ToInt32(o) : -1;
        }

        /// <summary>Kleiner Helfer gegen null/DBNull - wie in Form_Kosten.</summary>
        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        public bool Add_Projekt_ZuordungGebäude(int projektID, List<Z_ProjGebModel> list)
        {
            GebaeudeStammCtrl ctrlStamm = new GebaeudeStammCtrl();
            foreach (var item in list)
            {
                // 1) Projekt-Zuordnung (Z_ProjektGebaeude) mit eigener ID anlegen.
                int zID = DataRepository.GetMaxID("Z_ProjektGebaeude") + 1;
                string sqlZ = "INSERT INTO Z_ProjektGebaeude (ID, ID_Projekt, Wohnflaeche_Waermebedarf, " +
                    "Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, dezWarmwasserbereitung) VALUES (?,?,?,?,?,?)";
                DbParam[] psZ = {
                    new DbParam("@id", DbParamTyp.Integer) { Wert = zID },
                    new DbParam("@pid", DbParamTyp.Integer) { Wert = projektID },
                    new DbParam("@fl", DbParamTyp.Double) { Wert = item.Wohnflaeche },
                    new DbParam("@Einheit", DbParamTyp.VarWChar) { Wert = (object)(item.Einheit ?? "") },
                    new DbParam("@jng", DbParamTyp.Double) { Wert = item.Jahresnutzungsgrad },
                    new DbParam("@dez", DbParamTyp.Boolean) { Wert = item.DezentralWarmwasser }
                };
                if (!DataRepository.ExecuteSQL(sqlZ, psZ)) return false;

                // 2) Gebaeude-Stammdatensatz in die Projekt-Tabelle Tab_Gebaeude kopieren
                //    (setzt ID_Projekt und die Verknuepfung ID_ProjektGebaeude = zID).
                if (ctrlStamm.CopyFromStamm(item.Gebaeudename, projektID, zID) <= 0) return false;
            }
            return true;
        }

        public bool Add_Projekt(ref int projektID, ProjektModel model)
        {
            string sql = "INSERT INTO Tab_Projekt (Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) VALUES (?,?,?,?,?,?,?)";

            DbParam[] ps = {
                new DbParam("@name", model.m_szProjektname),
                new DbParam("@bearb", model.m_szBearbeiter),
                new DbParam("@besch", model.m_szBeschreibung),
                new DbParam("@kunde", model.m_szKunde),
                new DbParam("@date", DbParamTyp.Date) { Wert = model.m_Aenderungsdatum },
                new DbParam("@klima", model.m_ID_Klimaregion),
                new DbParam("@edate", DbParamTyp.Date) { Wert = model.m_Erstelldatum }
            };

            // Aufruf deiner neuen, zentralen Methode
            int generierteId = DataRepository.ExecuteInsertAndGetId(sql, ps);

            // Wenn die ID größer als 0 ist, war das Einfügen erfolgreich
            if (generierteId > 0)
            {
                projektID = generierteId; // Über ref-Parameter an den Aufrufer zurückgeben

                // Klimadaten-Kopie fuer das Projekt anlegen (falls noetig) und
                // Tab_Projekt.ID_Klimaregion auf die Projekt-Kopie setzen (gefuehrt wird nur der Name).
                KlimaregionStammCtrl.ApplyRegionByNameToProjekt(model.m_szKlimaregion, projektID);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Update_Projekt(int projektID, ProjektModel model)
        {
            // Klimadaten-Kopie fuer das Projekt anlegen (falls noetig); liefert die Projekt-Region-ID.
            int projRegId = KlimaregionStammCtrl.ApplyRegionByNameToProjekt(model.m_szKlimaregion, projektID);
            if (projRegId > 0) model.m_ID_Klimaregion = projRegId;

            string sql = "UPDATE Tab_Projekt SET Projektname=?, Bearbeiter=?, ID_Klimaregion=?, Aenderungsdatum=?, Kunde=?, Beschreibung=? WHERE ID=?";
            DbParam[] ps = {
                new DbParam("@name", model.m_szProjektname),
                new DbParam("@bearb", model.m_szBearbeiter),
                new DbParam("@klima", model.m_ID_Klimaregion),
                new DbParam("@date", DbParamTyp.Date) { Wert = DateTime.Now },
                new DbParam("@kunde", model.m_szKunde),
                new DbParam("@besch", model.m_szBeschreibung),
                new DbParam("@id", projektID)
            };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Add_SP(int projektID, List<StromspeicherModel> list)
        {
            foreach (var item in list)
            {
                string sql = @"INSERT INTO Tab_Energieanlagen 
                               (ID_Projekt, Bezeichner, ID_Type, ID_SP) 
                               VALUES (?, ?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@pID", projektID),
                    new DbParam("@bez", item.m_szBezeichner ?? ""),
                    new DbParam("@type", 4), // Typ 4 Stromspeicher
                    new DbParam("@spID", item.m_ID)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_WaermebedarfExtern(int projektID, List<Z_ProjWaermebedarfModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektWaermebedarf", "ID_Z") + 1;

            // Migrationsschritt 48 (F18): Der Speicherweg der Zuordnung ist LOESCHEN +
            // NEU ANLEGEN. Die Kanalspalte muss deshalb in JEDER Schreibstelle stehen -
            // sonst faellt der Kanal beim naechsten Speichern still auf Heizung zurueck.
            // Die Vorsorge legt sie auf einer noch nicht migrierten Datenbank an.
            bool kanalSpalte = Z_ProjektGebGanglinieCtrl.StelleKanalSpalteSicher();

            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = WaermebedarfStammCtrl.ApplyGanglinieToProjekt(item.m_szBezeichner, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Ganglinie;

                string sql = kanalSpalte
                    ? "INSERT INTO Z_ProjektWaermebedarf (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner, Kanal) VALUES (?, ?, ?, ?, ?)"
                    : "INSERT INTO Z_ProjektWaermebedarf (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                var ps = new List<DbParam>
                {
                    new DbParam("@id", nextID++),
                    new DbParam("@pID", projektID),
                    new DbParam("@gID", projGanglinieId),
                    new DbParam("@bez", item.m_szBezeichner ?? "")
                };
                if (kanalSpalte)
                    ps.Add(new DbParam("@kanal",
                        Z_ProjektGebGanglinieCtrl.KanalOderHeizung(item.Kanal)));

                if (!DataRepository.ExecuteSQL(sql, ps.ToArray())) return false;
            }
            return true;
        }

        public bool Add_Projekt_Prozess(int projektID, List<Z_ProjektProzesswaermeModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Prozesswaerme", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Prozess (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projPwId = ProzesswaermeStammCtrl.CopyFromStamm(item.szProzessname, projektID);
                if (projPwId > 0) item.ID_Prozesswaerme = projPwId;

                string sql = "INSERT INTO Z_Projekt_Prozesswaerme (ID, ID_Projekt, ID_Prozesswaerme, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@id", nextID++),
                    new DbParam("@pID", projektID),
                    new DbParam("@pwID", item.ID_Prozesswaerme),
                    new DbParam("@bez", item.szProzessname ?? ""),
                    new DbParam("@sum", item.Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt_Stromverbraucher(int projektID, List<Z_ProjektStromverbraucherModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Stromverbraucher", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Stromverbraucher (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projSvId = StromverbraucherStammCtrl.CopyFromStamm(item.m_szVerbraucher, projektID);
                if (projSvId > 0) item.m_ID_Stromverbraucher = projSvId;

                string sql = "INSERT INTO Z_Projekt_Stromverbraucher (ID, ID_Projekt, ID_Stromverbraucher, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@id", nextID++),
                    new DbParam("@pID", projektID),
                    new DbParam("@svID", item.m_ID_Stromverbraucher),
                    new DbParam("@bez", item.m_szVerbraucher ?? ""),
                    new DbParam("@sum", item.m_Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Stromganglinie(int projektID, List<Z_ProjektStromganglinieModel> list)
        {
            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = StromganglinieStammCtrl.ApplyGanglinieToProjekt(item.m_szStromganglinie, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Stromganglinie;

                string sql = "INSERT INTO Z_ProjektStromganglinie (ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@pID", projektID),
                    new DbParam("@gID", projGanglinieId),
                    new DbParam("@bez", item.m_szStromganglinie ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Solarganglinie(int projektID, List<Z_ProjektSolarganglinieModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektSolarganglinie", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = SolarganglinieStammCtrl.ApplyGanglinieToProjekt(item.m_szSolarganglinie, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Solarganglinie;

                string sql = "INSERT INTO Z_ProjektSolarganglinie (ID, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@id", nextID++),
                    new DbParam("@pID", projektID),
                    new DbParam("@gID", projGanglinieId),
                    new DbParam("@bez", item.m_szSolarganglinie ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt_Brauchwasser(int projektID, List<Z_ProjektBrauchwasserModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Brauchwasser", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Brauchwasser (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projBwId = BrauchwasserStammCtrl.CopyFromStamm(item.szBezeichner, projektID);
                if (projBwId > 0) item.ID_Brauchwasser = projBwId;

                string sql = "INSERT INTO Z_Projekt_Brauchwasser (ID, ID_Projekt, ID_Brauchwasser, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                DbParam[] ps = {
                    new DbParam("@id", nextID++),
                    new DbParam("@pID", projektID),
                    new DbParam("@bwID", item.ID_Brauchwasser),
                    new DbParam("@bez", item.szBezeichner ?? ""),
                    new DbParam("@sum", item.Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }
 
    }
}
