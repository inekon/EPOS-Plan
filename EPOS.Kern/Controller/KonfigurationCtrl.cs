using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    public class KonfigurationCtrl : KonfigurationModel
    {
        public KonfigurationModel model = new KonfigurationModel();
        public int rows;

        public enum Energieerzeuger
        {
            BHKW = 0,
            HEIZKESSEL = 1,
            PHOTOVOLTAIK = 2,
            SOLARTHERMIE = 3,
            WAERMEPUMPE = 4
        }


        public KonfigurationCtrl()
        {
            rows = 0;
        }

        ~KonfigurationCtrl()
        {
            rows = 0;
        }

        /// <summary>
        /// Liest den Einstellungssatz EINES Projekts — die eine Wahrheit dieser
        /// Abfrage (iU9-W10b.0b und iU9-W11a.2, Befund W11-B24).
        ///
        /// <para><b>Warum es diese Methode gibt.</b> Die Zeile
        /// <c>"select * from Tab_Einstellungen where ID_Projekt=" + id</c> stand ACHTMAL
        /// im Bestand — sechsmal in <c>Form_Simulation_Detail</c>, einmal in
        /// <c>Form_Start</c> und einmal im <see cref="SimulationRunner"/>: die
        /// Projektnummer als Zeichenkette in die Anweisung geklebt, also gegen die
        /// Hausregel „Datenzugriff ausschliesslich ueber <c>DataRepository</c> mit
        /// <c>new DbParam(…)</c>". Der Weg ueber einen <see cref="DbParam"/> steht jetzt
        /// an EINER Stelle; an der Ordinalkette der Zeilenauswertung
        /// (<see cref="ZeileUebernehmen"/>) aendert sich nichts.</para>
        ///
        /// <para><b>Zwei Wellen, eine Methode.</b> W10b (Konfigurationsseite) und W11a
        /// (Ergebnisseite) haben sie gleichzeitig gebraucht und unabhaengig gebaut. Beim
        /// Zusammenfuehren ist die Signatur die des Kerns geblieben; wer ein
        /// STEUEROBJEKT fuellen will statt ein frisches Modell zu bekommen, nimmt
        /// <see cref="ProjektLesen"/>.</para>
        ///
        /// <para>Rueckgabe <c>null</c> = kein Satz zum Projekt (neues Projekt). Der
        /// Aufrufer legt dann selbst einen leeren an — genau das taten die
        /// Aufrufstellen bisher ueber <c>rows == 0</c>.</para>
        /// </summary>
        public static KonfigurationModel LiesProjekt(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            KonfigurationModel m = new KonfigurationModel();
            return ZeileUebernehmen(TabelleJeProjekt(idProjekt), m) ? m : null;
        }

        /// <summary>
        /// Dasselbe fuer ein STEUEROBJEKT: fuellt <see cref="model"/> an Ort und Stelle
        /// und setzt <see cref="rows"/> — der wortgleiche Ersatz fuer
        /// <c>ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + id)</c>.
        ///
        /// <para>Bewusst AN ORT UND STELLE und nicht mit einem frischen Modell: Die
        /// Aufrufer reichen <c>ctrl.model</c> weiter (<c>SimulationControl.ctrl_konfig</c>
        /// haelt das Steuerobjekt und liest es waehrend des Laufs). Und bewusst mit
        /// derselben Feldregel wie <see cref="ReadSingle"/>: Ein DBNull laesst den
        /// bisherigen Wert stehen.</para>
        /// </summary>
        public bool ProjektLesen(int idProjekt)
        {
            rows = 0;
            if (idProjekt <= 0) return false;
            if (!ZeileUebernehmen(TabelleJeProjekt(idProjekt), model)) return false;
            rows = 1;
            return true;
        }

        /// <summary>Die eine Abfrage — parametrisiert, nicht konkateniert.</summary>
        private static DataTable TabelleJeProjekt(int idProjekt)
        {
            return DataRepository.GetDataTable(
                "SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                new DbParam("?", idProjekt));
        }

        /// <summary>
        /// Wie <see cref="ReadSingle(string)"/>, aber mit Parametern statt zusammen-
        /// gesetztem Text (iU9-W10b.0b).
        /// </summary>
        public void ReadSingle(string sql, params DbParam[] parameter)
        {
            ReadZeile(DataRepository.GetDataTable(sql, parameter));
        }

        public void ReadSingle(string sql)
        {
            ReadZeile(DataRepository.GetDataTable(sql));
        }

        private void ReadZeile(DataTable dt)
        {
            rows = 0;

            if (ZeileUebernehmen(dt, model)) rows = 1;
        }

        /// <summary>
        /// Uebernimmt die erste Zeile einer gelesenen Tabelle in ein Modell — die
        /// Abbildung, die <see cref="ReadSingle"/> und <see cref="LiesProjekt"/> teilen.
        /// Rueckgabe <c>false</c>, wenn nichts zu uebernehmen war.
        /// </summary>
        private static bool ZeileUebernehmen(DataTable dt, KonfigurationModel model)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                if (row[0] != DBNull.Value) model.m_ID = Convert.ToInt32(row[0]);
                if (row[1] != DBNull.Value) model.m_ID_Projekt = Convert.ToInt32(row[1]);
                if (row[2] != DBNull.Value) model.m_BHKW_Grenzleistung = Convert.ToDouble(row[2]);
                if (row[3] != DBNull.Value) model.m_Netzverluste = Convert.ToDouble(row[3]);
                if (row[4] != DBNull.Value) model.m_szNetzverlusteEinheit = row[4].ToString();
                if (row[5] != DBNull.Value) model.m_WP_Heizstab = Convert.ToBoolean(row[5]);
                if (row[6] != DBNull.Value) model.m_Kessel_Betriebsbereitschaft = Convert.ToInt32(row[6]);
                if (row[7] != DBNull.Value) model.m_Tool_1 = row[7].ToString();
                if (row[8] != DBNull.Value) model.m_Tool_2 = row[8].ToString();
                if (row[9] != DBNull.Value) model.m_Tool_3 = row[9].ToString();
                if (row[10] != DBNull.Value) model.m_Tool_4 = row[10].ToString();
                if (row[11] != DBNull.Value) model.m_Tool_5 = row[11].ToString();
                if (row[12] != DBNull.Value) model.m_Tool_6 = row[12].ToString();
                if (row[13] != DBNull.Value) model.m_Ladefuellstand_Min = Convert.ToInt32(row[13]);
                if (row[14] != DBNull.Value) model.m_Ladefuellstand_Max = Convert.ToInt32(row[14]);
                if (row[15] != DBNull.Value) model.m_Ladeleistung_Max = Convert.ToInt32(row[15]);
                if (row[16] != DBNull.Value) model.m_Ladefuellstand_Min_Auswahl = row[16].ToString();
                if (row[17] != DBNull.Value) model.m_Ladefuellstand_Max_Auswahl = row[17].ToString();
                if (row[18] != DBNull.Value) model.m_Ladeleistung_Max_Auswahl = row[18].ToString();
                if (row[19] != DBNull.Value) model.m_Ladeschwellwert = Convert.ToDouble(row[19]);
                if (row[20] != DBNull.Value) model.Betriebsart = Convert.ToInt32(row[20]);
                if (row[21] != DBNull.Value) model.Leistungsgrenze = Convert.ToInt32(row[21]);
                if (row[22] != DBNull.Value) model.Pendelspeicher = Convert.ToDouble(row[22]);

                // PAKET L (Aufräumen): Hier stand die namensbasierte Lesung des
                // Feature-Flags Kaskade_Zweikanalig. Sie ist mit dem Feld
                // KonfigurationModel.Kaskade_Zweikanalig entfallen - seit Paket A1 gibt
                // es nur EINEN Rechenweg, und mit diesem Paket auch keinen Leser mehr.
                // Die Ordinalkette row[0..22] ist davon unberührt: Die Lesung war
                // namensbasiert und hing an keiner Position.

                // --- Einstellung Extrapolation_erlaubt (Paket 8, Konzept 13.4) --------
                //
                // NAMENSBASIERT, bewusst NICHT als row[23] an die Ordinalkette angehängt:
                // Die Kette oben ist an die physische Spaltenreihenfolge von
                // Tab_Einstellungen gebunden und damit die brüchigste Stelle des
                // Datenzugriffs - jede weitere Position macht sie nur länger. Über den
                // Spaltennamen ist der Zugriff unabhängig davon, an welcher Position die
                // Migration die Spalte angehängt hat.
                //
                // Der Wert wird in BEIDEN Zweigen gesetzt und nicht nur bei Treffer: ein
                // wiederverwendetes Model dürfte sonst den Stand des zuvor gelesenen
                // Projekts behalten. Anders als beim entfallenen Flag mit
                // UMGEKEHRTER Vorbelegung: Fehlt die Spalte (Datenbank noch nicht auf
                // Schemastand 7) oder steht dort NULL, gilt ERLAUBT. Das ist genau das
                // bisherige Verhalten - die Engine fragte nach, und die Antwort war in
                // jedem dokumentierten Lauf "Ja". Ein "verboten" darf deshalb nur aus
                // einem ausdrücklich gesetzten FALSE kommen, nie aus einer Datenlücke.
                //
                // NACHARBEIT PAKET 8, BEFUND N8 — der nie vorbelegte Zustand.
                // Es reicht nicht, fehlende Spalte und NULL abzufangen: Die Spalte steht
                // seit Paket 1 in SchemaKatalog.Schritt2_Speicher und wird deshalb auch
                // von der stillen Rückfallebene (WaermequelleClass.SchemaSicherstellen)
                // angelegt - mit dem Access-Default FALSE, denn ein Ja/Nein-Feld kennt
                // kein NULL. Auf einer Datenbank, die diese Spalte hat, aber
                // Migrationsschritt 7 noch nicht gelaufen ist, stünde damit überall
                // "verboten", und jeder extrapolierende Wärmepumpenlauf bräche ab. Genau
                // das trifft die Referenzlauf-Suite in Weg B: Der Modus "projekt"
                // migriert nicht. Solange der Schemastand unter 7 liegt, ist das FALSE
                // deshalb kein Anwenderwille, sondern eine Datenlücke - und die bedeutet
                // ERLAUBT, wie überall sonst bei dieser Einstellung.
                model.Extrapolation_erlaubt =
                    !dt.Columns.Contains(SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT) ||
                    row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT] == DBNull.Value ||
                    Convert.ToBoolean(row[SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT]) ||
                    ExtrapolationVorbelegungFehlt();

                // --- Kanal-Knappheitsreihenfolge (Paket K2, Konzept 4.3, F10) ---------
                //
                // Zweites Feld nach demselben namensbasierten Muster: Die Ordinalkette
                // oben endet bei row[22], und sie soll dort enden. Fehlt die Spalte
                // (Datenbank noch nicht auf Schemastand 49) oder steht dort NULL bzw.
                // ein Leerwert, gilt DbWerte.KNAPPHEIT_DEFAULT - also genau die
                // Reihenfolge, die die Kaskade vor diesem Paket fest verdrahtet kannte.
                //
                // Wie beim Feld darueber wird der Wert in BEIDEN Zweigen
                // gesetzt und nicht nur bei Treffer: ein wiederverwendetes Model
                // duerfte sonst die Reihenfolge des zuvor gelesenen Projekts behalten.
                model.Kanal_Knappheitsreihenfolge = KnappheitsreihenfolgeOderDefault(
                    dt.Columns.Contains(SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE)
                        ? row[SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE]
                        : null);

                return true;
            }

            return false;
        }

        // =====================================================================
        // ENTFALLEN MIT PAKET L (Aufraeumen) - Altlast Kaskade_Zweikanalig
        //
        // Hier standen KaskadeZweikanaligLesen, KaskadeZweikanaligSchreiben und die
        // Automatik KaskadeNotwendig (zwei Ueberladungen) samt ihrer beiden privaten
        // Helfer KaskadeErzeuger und ErzeugerZuTyp. Sie bedienten die
        // Projekteinstellung Tab_Einstellungen.Kaskade_Zweikanalig - bis Paket A1 die
        // WEICHE zwischen zwei Rechenwegen.
        //
        // Mit Paket A1 (Leitentscheidung L1) ist der einkanalige Altpfad ersatzlos
        // entfallen; mit dem Fusszeilenschalter des Konfigurationsdialogs und dem
        // Uebergangshinweis des Senkendialogs verschwand ihr letzter Aufrufer. Paket L
        // schneidet die aufruferfreien Bausteine heraus (A1-O3): Es gibt nur EINEN
        // Rechenweg, es gibt also nichts mehr umzuschalten und nichts zu begruenden.
        //
        // DIE SPALTE BLEIBT. Konzept Kapitel 15 fuehrt
        // Tab_Einstellungen.Kaskade_Zweikanalig als "stillgelegt (Lese-Altlast nach
        // Migration)"; Migrationsschritt 51 setzt sie im Bestand auf WAHR und loescht
        // nichts. Wer sie je wieder braucht, liest sie ueber StilleDb - der Weg dorthin
        // ist eine Zeile, die Namenskonstante steht in
        // SchemaKatalog.SPALTE_KASKADE_ZWEIKANALIG.
        // =====================================================================

        /// <summary>
        /// Liest die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts DIALOGFREI
        /// (Paket 8, Konzept 13.4) — für die Oberfläche, die den Schalter anzeigt, ohne
        /// den ganzen Einstellungssatz zu laden.
        ///
        /// Fehlende Spalte, fehlende Zeile und NULL liefern gleichermaßen <c>true</c>;
        /// das ist die Vorbelegung der Einstellung und das bisherige Verhalten.
        /// </summary>
        public static bool ExtrapolationErlaubtLesen(int idProjekt)
        {
            if (idProjekt <= 0) return true;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            if (v == null) return true;
            try { if (Convert.ToBoolean(v)) return true; }
            catch { return true; }

            // Befund N8: ein FALSE aus einer Datenbank ohne Migrationsschritt 7 ist die
            // Vorbelegung von Access, nicht der Wille des Anwenders (Begründung in
            // ReadSingle).
            return ExtrapolationVorbelegungFehlt();
        }

        /// <summary>
        /// true, solange die Datenbank den Migrationsschritt 7 (Vorbelegung
        /// <c>Extrapolation_erlaubt = WAHR</c>) noch nicht hinter sich hat — dann ist ein
        /// gespeichertes FALSE die Access-Vorbelegung einer angehängten YESNO-Spalte und
        /// nicht die Entscheidung des Anwenders (Nacharbeit Paket 8, Befund N8).
        ///
        /// Bewusst LESEND: Die Alternative wäre gewesen, die stille Rückfallebene
        /// <c>WaermequelleClass.SchemaSicherstellen</c> die Spalte nachvorbelegen zu
        /// lassen. Das trägt nicht — sie läuft erst in <c>Do_Simulation</c>, also NACH
        /// dem Lesen der Konfiguration im <c>SimulationRunner</c>, und hätte den
        /// laufenden Lauf nicht mehr erreicht. Ein Leser, der die Datenlücke erkennt,
        /// wirkt sofort und schreibt nichts in eine fremde Datenbank.
        ///
        /// Der erreichte Zielstand wird gemerkt: Auf einer gepflegten Datenbank fällt
        /// genau ein Marker-Lesevorgang je Programmlauf an, danach nichts mehr.
        /// </summary>
        private static bool _schemastand7Erreicht = false;

        private static bool ExtrapolationVorbelegungFehlt()
        {
            if (_schemastand7Erreicht) return false;

            try
            {
                if (ApplikationCtrl.GetSchemaVersion() >= SchemaStand.SCHRITT_7_EXTRAPOLATION)
                {
                    _schemastand7Erreicht = true;
                    return false;
                }
            }
            catch { /* Marker nicht lesbar - dann gilt die Datenlücke */ }

            return true;
        }

        /// <summary>
        /// Schreibt die Einstellung <c>Extrapolation_erlaubt</c> eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool ExtrapolationErlaubtSchreiben(int idProjekt, bool wert)
        {
            if (idProjekt <= 0) return false;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" + SchemaKatalog.SPALTE_EXTRAPOLATION_ERLAUBT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.Boolean, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Kanal-Knappheitsreihenfolge (Paket K2, Konzept 4.3, Entscheidung F10) -----

        /// <summary>
        /// Der Wert eines gelesenen Feldes; <c>null</c>, <c>DBNull</c> und Leerwert
        /// ergeben <see cref="DbWerte.KNAPPHEIT_DEFAULT"/> — die Vorbelegung nach F10
        /// und zugleich die bis Paket K2 fest verdrahtete Reihenfolge.
        ///
        /// <para>Bewusst OHNE inhaltliche Prüfung: Ein unbekanntes Glied oder ein
        /// fehlender Kanal ist kein Grund, den Anwenderwillen zu verwerfen. Die
        /// Auswertung im Rechenkern ist tolerant — sie übergeht, was sie nicht kennt,
        /// und ergänzt fehlende Kanäle hinten in der Reihenfolge des Vorgabewerts.</para>
        /// </summary>
        public static string KnappheitsreihenfolgeOderDefault(object feld)
        {
            if (feld == null || feld == DBNull.Value) return DbWerte.KNAPPHEIT_DEFAULT;

            string wert = (feld.ToString() ?? "").Trim();
            return wert.Length == 0 ? DbWerte.KNAPPHEIT_DEFAULT : wert;
        }

        /// <summary>
        /// Liest die Knappheitsreihenfolge eines Projekts DIALOGFREI — für Aufrufer, die
        /// nicht den ganzen Einstellungssatz laden (Rechenkern, Oberfläche).
        ///
        /// Fehlende Spalte, fehlende Zeile, NULL und Leerwert liefern gleichermaßen
        /// <see cref="DbWerte.KNAPPHEIT_DEFAULT"/>.
        /// </summary>
        public static string KnappheitsreihenfolgeLesen(int idProjekt)
        {
            if (idProjekt <= 0) return DbWerte.KNAPPHEIT_DEFAULT;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return KnappheitsreihenfolgeOderDefault(v);
        }

        /// <summary>
        /// Schreibt die Knappheitsreihenfolge eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="ExtrapolationErlaubtSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Ein leerer Wert wird als <see cref="DbWerte.KNAPPHEIT_DEFAULT"/> geschrieben,
        /// nicht als NULL: Die Spalte soll die geltende Reihenfolge zeigen, auch wenn
        /// sie die Vorgabe ist.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool KnappheitsreihenfolgeSchreiben(int idProjekt, string reihenfolge)
        {
            if (idProjekt <= 0) return false;

            string wert = (reihenfolge ?? "").Trim();
            if (wert.Length == 0) wert = DbWerte.KNAPPHEIT_DEFAULT;

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                SchemaKatalog.SPALTE_KANAL_KNAPPHEITSREIHENFOLGE + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.VarWChar, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        // --- Booster-Lesepunkt (Paket B2, Nutzerauftrag 28.08.2026) --------------------

        /// <summary>
        /// Liest den LESEPUNKT der Booster-Quelltemperatur eines Projekts DIALOGFREI —
        /// für den Rechenkern und den Konfigurationsdialog.
        ///
        /// <para>Fehlende Spalte (Datenbank noch nicht auf Schemastand 55), fehlende
        /// Zeile, NULL, Leerwert und jeder unbekannte Wert liefern gleichermaßen
        /// <see cref="DbWerte.BOOSTER_LESEPUNKT_DAVOR"/> — die Vorbelegung des
        /// Nutzerauftrags.</para>
        ///
        /// <para><b>Anders als bei <see cref="ExtrapolationErlaubtLesen"/> braucht es
        /// hier KEINE Markerprüfung</b> (Befund N8): Die Spalte ist ein TEXTfeld, und
        /// eine angehängte Textspalte steht in Access auf NULL — nicht auf einem Wert,
        /// der wie eine Anwenderentscheidung aussieht. Die Datenlücke ist damit von
        /// selbst als solche erkennbar.</para>
        /// </summary>
        public static string BoosterLesepunktLesen(int idProjekt)
        {
            if (idProjekt <= 0) return DbWerte.BOOSTER_LESEPUNKT_DAVOR;

            object v = StilleDb.Scalar(
                "SELECT [" + SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT + "] " +
                "FROM Tab_Einstellungen WHERE ID_Projekt = ?",
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return DbWerte.BoosterLesepunktOderDefault(v);
        }

        /// <summary>
        /// Schreibt den Booster-Lesepunkt eines Projekts.
        ///
        /// Bewusst ein EIGENES, zielgenaues UPDATE statt einer Erweiterung von
        /// <see cref="Update"/> — dieselbe Begründung wie bei
        /// <see cref="KnappheitsreihenfolgeSchreiben"/>: Die Spaltenlisten von
        /// <see cref="Insert"/>/<see cref="Update"/> hängen an der Ordinalkette in
        /// <see cref="ReadSingle"/>, und auf einer Datenbank ohne die Spalte würde ein
        /// erweitertes UPDATE das Speichern der GESAMTEN Konfiguration scheitern lassen.
        ///
        /// Ein unbekannter Wert wird als Vorbelegung geschrieben, nicht als NULL: Die
        /// Spalte soll den geltenden Lesepunkt zeigen, auch wenn er die Vorgabe ist.
        ///
        /// Dialogfrei (Konzept 13.4). Rückgabe false, wenn keine Zeile getroffen wurde
        /// oder die Spalte fehlt.
        /// </summary>
        public static bool BoosterLesepunktSchreiben(int idProjekt, string lesepunkt)
        {
            if (idProjekt <= 0) return false;

            string wert = DbWerte.BoosterLesepunktOderDefault(lesepunkt);

            int betroffen = StilleDb.NonQuery(
                "UPDATE Tab_Einstellungen SET [" +
                SchemaKatalog.SPALTE_BOOSTER_LESEPUNKT + "] = ? " +
                "WHERE ID_Projekt = ?",
                StilleDb.Par("@wert", DbParamTyp.VarWChar, wert),
                StilleDb.Par("@proj", DbParamTyp.Integer, idProjekt));

            return betroffen > 0;
        }

        public bool Insert(int ID_Projekt)
        {
            try
            {
                // Umstellung auf sichere Parameter-Marker (?) statt ungesicherter String-Verkettung
                string sql = @"
                    INSERT INTO TAB_Einstellungen 
                    (
                        ID_Projekt, BHKW_Grenzleistung, Netzverluste, NetzverlusteEinheit, 
                        WP_Heizstab, Kessel_Betriebsbereitschaft, 
                        Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6,
                        Ladefuellstand_Min, Ladefuellstand_Max, Ladeleistung_Max,
                        Ladefuellstand_Min_Auswahl, Ladefuellstand_Max_Auswahl, 
                        Ladeleistung_Max_Auswahl, Ladeschwellwert, Betriebsart, Leistungsgrenze, Pendelspeicher
                    ) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                // Die Parameter werden als OLEDB-Objekte an dein DataRepository gereicht
                DbParam[] parameters = new DbParam[]
                {
                    new DbParam("?", ID_Projekt),
                    new DbParam("?", model.m_BHKW_Grenzleistung),
                    new DbParam("?", model.m_Netzverluste),
                    new DbParam("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
                    new DbParam("?", model.m_WP_Heizstab),
                    new DbParam("?", model.m_Kessel_Betriebsbereitschaft),
                    new DbParam("?", model.m_Tool_1 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_2 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_3 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_4 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_5 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Tool_6 ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladefuellstand_Min),
                    new DbParam("?", model.m_Ladefuellstand_Max),
                    new DbParam("?", model.m_Ladeleistung_Max),
                    new DbParam("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
                    new DbParam("?", model.m_Ladeschwellwert),
                    new DbParam("?", model.Betriebsart),
                    new DbParam("?", model.Leistungsgrenze),
                    new DbParam("?", model.Pendelspeicher)
                };

                // Übergabe an das DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);

                // PAKET 8 (Konzept 13.4): Die Spaltenliste oben bleibt unangetastet -
                // sie gehört zur Ordinalkette von ReadSingle, und auf einer Datenbank
                // ohne Schemastand 7 würde ein erweitertes INSERT das Anlegen der
                // GESAMTEN Konfiguration scheitern lassen. Die Vorbelegung kommt
                // deshalb als eigenes, stilles UPDATE hinterher: Access belegt eine
                // angehängte YESNO-Spalte in einer neuen Zeile mit False - ohne diese
                // Zeile stünde jedes NEUE Projekt auf "Extrapolation verboten" und
                // damit auf anderem Verhalten als der migrierte Bestand.
                ExtrapolationErlaubtSchreiben(ID_Projekt, true);

                // PAKET K2 (F10): dieselbe Nachreichung für die Knappheitsreihenfolge.
                // Anders als beim Ja/Nein-Feld darüber wäre NULL hier kein Fehler - die
                // Leseseite macht daraus ohnehin den Vorgabewert. Geschrieben wird sie
                // trotzdem, damit ein neues Projekt dieselbe Zeile zeigt wie ein
                // migriertes: Die Spalte soll die geltende Reihenfolge nennen, nicht
                // schweigen.
                KnappheitsreihenfolgeSchreiben(ID_Projekt, DbWerte.KNAPPHEIT_DEFAULT);

                // PAKET B2: dieselbe Nachreichung für den Booster-Lesepunkt. Ein neues
                // Projekt soll dieselbe Zeile zeigen wie ein migriertes - die Spalte
                // nennt den geltenden Lesepunkt, statt zu schweigen.
                BoosterLesepunktSchreiben(ID_Projekt, DbWerte.BOOSTER_LESEPUNKT_DAVOR);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Einfügen der Konfiguration: " + ex.Message);
                DataRepository.FehlerMelden("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
        }

        public bool Update(int ID_Projekt)
        {
            try
            {
                // SQL-Update-String mit Positions-Parametern (?)
                string sql = @"
            UPDATE TAB_Einstellungen 
            SET 
                BHKW_Grenzleistung = ?, 
                Netzverluste = ?, 
                NetzverlusteEinheit = ?, 
                WP_Heizstab = ?, 
                Kessel_Betriebsbereitschaft = ?, 
                Tool_1 = ?, 
                Tool_2 = ?, 
                Tool_3 = ?, 
                Tool_4 = ?, 
                Tool_5 = ?, 
                Tool_6 = ?,
                Ladefuellstand_Min = ?, 
                Ladefuellstand_Max = ?, 
                Ladeleistung_Max = ?,
                Ladefuellstand_Min_Auswahl = ?, 
                Ladefuellstand_Max_Auswahl = ?, 
                Ladeleistung_Max_Auswahl = ?, 
                Ladeschwellwert = ?,
                Betriebsart = ?,
                Leistungsgrenze = ?,
                Pendelspeicher = ?
            WHERE ID_Projekt = ?";

                // Die Parameter-Reihenfolge entspricht exakt den Fragezeichen im SQL-String
                DbParam[] parameters = new DbParam[]
                {
            new DbParam("?", model.m_BHKW_Grenzleistung),
            new DbParam("?", model.m_Netzverluste),
            new DbParam("?", model.m_szNetzverlusteEinheit ?? (object)DBNull.Value),
            new DbParam("?", model.m_WP_Heizstab),
            new DbParam("?", model.m_Kessel_Betriebsbereitschaft),
            new DbParam("?", model.m_Tool_1 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_2 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_3 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_4 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_5 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Tool_6 ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladefuellstand_Min),
            new DbParam("?", model.m_Ladefuellstand_Max),
            new DbParam("?", model.m_Ladeleistung_Max),
            new DbParam("?", model.m_Ladefuellstand_Min_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladefuellstand_Max_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladeleistung_Max_Auswahl ?? (object)DBNull.Value),
            new DbParam("?", model.m_Ladeschwellwert),
            new DbParam("?", model.Betriebsart),
            new DbParam("?", model.Leistungsgrenze),
            new DbParam("?", model.Pendelspeicher),
            // ID_Projekt steht am Ende, weil das WHERE-Statement ganz unten steht!
            new DbParam("?", ID_Projekt)
                };

                // Übergabe an dein bestehendes DataRepository
                DataRepository.ExecuteNonQuery(sql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Konfiguration: " + ex.Message);
                DataRepository.FehlerMelden("Allgemeiner Fehler beim Speichern: " + ex.Message);
                return false;
            }
        }

        public bool Delete(int ID_Projekt)
        {
            try
            {
                // Sauberes ANSI-SQL für OLEDB ohne das ungültige "DELETE *"
                string sql = "DELETE FROM Tab_Einstellungen WHERE ID_Projekt = ?";
                DbParam parameter = new DbParam("?", ID_Projekt);

                DataRepository.ExecuteNonQuery(sql, parameter);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Löschen der Konfiguration: " + ex.Message);
                return false;
            }
        }
    }
}
