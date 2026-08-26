using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class WizardCtrl
    {
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

        public bool Del_Projekt_Waermeerzeuger(int projektID)
        {
            SpVariantenSichern(projektID, TYP_ALLE);

            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Waermeerzeuger(int projektID, int nType)
        {
            SpVariantenSichern(projektID, nType);

            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@type", nType) });
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
                        new OleDbParameter("@p", projektID),
                        new OleDbParameter("@a", ID_Waermeerzeuger));
            }
            catch { }

            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@id", ID_Waermeerzeuger) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID)
        {
            // Tagesverteilungen der Projekt-Gebaeude entfernen (Detail vor Kopf).
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?))",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?)",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            // Erst die Projekt-Gebaeudekopien (Kind: FK ID_ProjektGebaeude -> Z_ProjektGebaeude.ID), dann die Zuordnung.
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID, int ID)
        {
            // ID = Z_ProjektGebaeude.ID; zugehoerige Gebaeude-Kopie via ID_ProjektGebaeude.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?))",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?)",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ? AND ID = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@id", ID) });
        }

        public bool Del_WaermebedarfExtern(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Prozess(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Stromganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektStromganglinie WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Solarganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektSolarganglinie WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Stromverbraucher(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Projekt_Brauchwasser(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        /// <summary>
        /// Die EINE Einfuegeanweisung fuer <c>Tab_Energieanlagen</c> - 56 der 57 Spalten
        /// (<c>ID</c> ist AutoWert und wird nie gesetzt).
        ///
        /// <para>
        /// WARUM VOLLSTAENDIG. Der Speicherweg aller Erzeuger ist Loeschen + Neuanlegen
        /// (<see cref="Del_Projekt_Waermeerzeuger(int)"/> gefolgt von
        /// <see cref="Add_WP_Waermeerzeuger"/>). Jede Spalte, die hier fehlt, ist damit
        /// bei JEDEM Speichern verloren - nicht nur beim Bearbeiten im Wizard, sondern
        /// auch ueber Karten und Kontextmenues. Bis Paket 1 fuehrte die Anweisung 29
        /// Spalten; die 27 Spalten der Quellen-/Senken-Konfiguration
        /// (<c>WS_*</c>, <c>WQ_*</c>, <c>Prioritaet</c>, <c>BM_Typ</c>) gingen still
        /// verloren.
        /// </para>
        ///
        /// <para>
        /// EINE WAHRHEIT. <see cref="WErzeugerCtrl.Insert"/> benutzt dieselbe Anweisung
        /// und dieselben Parameter - zwei Einfuegewege mit unterschiedlichem
        /// Spaltensatz waeren genau die Halbwahrheit, die diesen Fehler erzeugt hat.
        /// </para>
        /// </summary>
        public const string SQL_ANLAGE_INSERT = @"INSERT INTO Tab_Energieanlagen
                        (ID_Projekt, Bezeichner, Betriebsart, Sperrung, Sperrzeit_von, Sperrzeit_bis,
                         Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung,
                         Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_Type,
                         ID_WP, ID_Solar, ID_PV, ID_SP, ID_KESSEL, ID_BHKW, ID_PUFFER,
                         Heizstab, Volumen, rendeMix, Solaranteil, ID_Carrier,
                         Prioritaet, BM_Typ,
                         WQ_Typ, WQ_Temp, WQ_Monatswerte, WQ_Wochenwerte, WQ_CSV, WQ_Puffer, WQ_ID_Puffer,
                         WQ_Spreizung, WQ_Regeneration, WQ_Unbegrenzt, WQ_Tiefe, WQ_Flaeche, WQ_Anzahl,
                         WQ_Bodentyp, WQ_Quellsystem,
                         WS_Typ, WS_Ziel, WS_ID_Puffer, WS_Ladeprio, WS_Ladegrenze, WS_Ladeprio_PV,
                         WS_Ziel2, WS_ID_Puffer2, WS_Ladeprio2, WS_Ladegrenze2)
                        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
                                ?,?,
                                ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,
                                ?,?,?,?,?,?,?,?,?,?)";

        /// <summary>
        /// Parameter zu <see cref="SQL_ANLAGE_INSERT"/>, exakt in der Reihenfolge der
        /// Anweisung.
        ///
        /// <para>
        /// DIE NEUEN 27 SPALTEN GEHEN DURCHGEHEND UEBER <c>ProjektPuffer.Par</c> mit
        /// AUSDRUECKLICHEM Spaltentyp. Der Grund ist derselbe wie bei
        /// <c>WaermequelleClass.WertSchreiben</c>: Aus <see cref="DBNull"/> allein leitet
        /// der Provider keinen Typ ab, und NULL ist bei diesen Spalten der Normalfall.
        /// Die 29 Bestandsspalten bleiben bei der typableitenden Kurzform - sie fuehren
        /// nie DBNull ausser bei den Komponenten-Fremdschluesseln, wo der Typ aus dem
        /// Kontext bereits feststeht.
        /// </para>
        /// </summary>
        /// <param name="pufferCache">
        /// Optionaler Zwischenspeicher fuer die Existenzpruefung der Puffer-Fremd-
        /// schluessel; siehe <see cref="PufferFkOderNull"/>. Innerhalb einer Schleife
        /// weiterreichen, damit dieselbe ID nicht mehrfach nachgeschlagen wird.
        /// </param>
        public static OleDbParameter[] AnlagenParameter(int projektID, WErzeugerModel item,
                                                        Dictionary<int, bool> pufferCache = null)
        {
            return new[] {
                        new OleDbParameter("@pID", projektID),
                        new OleDbParameter("@bez", item.Bezeichner ?? (object)DBNull.Value),
                        new OleDbParameter("@art", item.Betriebsart ?? (object)DBNull.Value),
                        new OleDbParameter("@sperr", item.Sperrung),
                        new OleDbParameter("@svon", item.Sperrzeit_von),
                        new OleDbParameter("@sbis", item.Sperrzeit_bis),
                        new OleDbParameter("@vor", item.Vorlauf),
                        new OleDbParameter("@rueck", item.Ruecklauf),
                        new OleDbParameter("@biv", item.Bivalenter_Betrieb),
                        new OleDbParameter("@ab", item.Abschaltpunkt),
                        new OleDbParameter("@nutz", item.Nutzungszeit),
                        new OleDbParameter("@grenz", item.Grenzleistung),
                        new OleDbParameter("@koll", item.Kollektormodulanzahl),
                        new OleDbParameter("@pvleist", item.PV_Leistung),
                        new OleDbParameter("@neig", item.m_Neigung),
                        new OleDbParameter("@azim", item.m_Azimut),
                        new OleDbParameter("@type", item.ID_Type),

                        // Fremdschlüssel-Logik (IDs nur setzen, wenn der Typ passt)
                        new OleDbParameter("@wp", CheckType(item, WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP) ? item.ID_WP : (object)DBNull.Value),
                        new OleDbParameter("@sol", CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP) ? item.ID_Solar : (object)DBNull.Value),
                        new OleDbParameter("@pv", CheckType(item, WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP) ? item.ID_PV : (object)DBNull.Value),
                        new OleDbParameter("@sp", CheckType(item, WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP) ? item.ID_SP : (object)DBNull.Value),
                        new OleDbParameter("@kes", CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP) ? item.ID_Kessel : (object)DBNull.Value),
                        new OleDbParameter("@bhkw", (item.ID_Type == WizardItemClass.BHKW_TYP) ? item.ID_BHKW : (object)DBNull.Value),
                        new OleDbParameter("@puf", (item.ID_Type == WizardItemClass.PUFFER_TYP && item.ID_PUFFER > 0) ? item.ID_PUFFER : (object)DBNull.Value),

                        new OleDbParameter("@stab", item.Heizstab),
                        new OleDbParameter("@vol", item.Volumen),
                        new OleDbParameter("@mix", item.rendeMix),
                        new OleDbParameter("@solan", item.Solaranteil),
                        // Rohwert: NULL bleibt NULL. 0 und NULL heißen beide "kein
                        // Energieträger" (SchemaKatalog, Schritt 8), der Bestand führt
                        // aber beide Schreibweisen - und ein Speichern soll keine davon
                        // in die andere umschreiben.
                        ProjektPuffer.Par("@idcarrier", OleDbType.Integer, Wert(item.ID_CarrierRoh)),

                        // --- Kaskade und Betriebsmodus ---------------------------------
                        ProjektPuffer.Par("@prio",      OleDbType.Integer,   Wert(item.Prioritaet)),
                        ProjektPuffer.Par("@bmtyp",     OleDbType.VarWChar,  item.BM_Typ),

                        // --- Wärmequelle ----------------------------------------------
                        ProjektPuffer.Par("@wqtyp",     OleDbType.VarWChar,  item.WQ_Typ),
                        ProjektPuffer.Par("@wqtemp",    OleDbType.Double,    Wert(item.WQ_Temp)),
                        ProjektPuffer.Par("@wqmon",     OleDbType.VarWChar,  item.WQ_Monatswerte),
                        ProjektPuffer.Par("@wqwoch",    OleDbType.VarWChar,  item.WQ_Wochenwerte),
                        ProjektPuffer.Par("@wqcsv",     OleDbType.VarWChar,  item.WQ_CSV),
                        ProjektPuffer.Par("@wqpuf",     OleDbType.VarWChar,  item.WQ_Puffer),
                        ProjektPuffer.Par("@wqidpuf",   OleDbType.Integer,
                            PufferFkOderNull(item.WQ_ID_Puffer, "WQ_ID_Puffer", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wqspreiz",  OleDbType.Double,    Wert(item.WQ_Spreizung)),
                        ProjektPuffer.Par("@wqregen",   OleDbType.Double,    Wert(item.WQ_Regeneration)),
                        ProjektPuffer.Par("@wqunbeg",   OleDbType.Boolean,   item.WQ_Unbegrenzt),
                        ProjektPuffer.Par("@wqtiefe",   OleDbType.Double,    Wert(item.WQ_Tiefe)),
                        ProjektPuffer.Par("@wqflaeche", OleDbType.Double,    Wert(item.WQ_Flaeche)),
                        ProjektPuffer.Par("@wqanzahl",  OleDbType.Integer,   Wert(item.WQ_Anzahl)),
                        ProjektPuffer.Par("@wqboden",   OleDbType.VarWChar,  item.WQ_Bodentyp),
                        ProjektPuffer.Par("@wqquell",   OleDbType.VarWChar,  item.WQ_Quellsystem),

                        // --- Wärmesenke -----------------------------------------------
                        ProjektPuffer.Par("@wstyp",     OleDbType.VarWChar,  item.WS_Typ),
                        ProjektPuffer.Par("@wsziel",    OleDbType.VarWChar,  item.WS_Ziel),
                        ProjektPuffer.Par("@wsidpuf",   OleDbType.Integer,
                            PufferFkOderNull(item.WS_ID_Puffer, "WS_ID_Puffer", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wslprio",   OleDbType.Integer,   Wert(item.WS_Ladeprio)),
                        ProjektPuffer.Par("@wslgrenz",  OleDbType.Double,    Wert(item.WS_Ladegrenze)),
                        ProjektPuffer.Par("@wslprioPV", OleDbType.Integer,   Wert(item.WS_Ladeprio_PV)),
                        ProjektPuffer.Par("@wsziel2",   OleDbType.VarWChar,  item.WS_Ziel2),
                        ProjektPuffer.Par("@wsidpuf2",  OleDbType.Integer,
                            PufferFkOderNull(item.WS_ID_Puffer2, "WS_ID_Puffer2", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wslprio2",  OleDbType.Integer,   Wert(item.WS_Ladeprio2)),
                        ProjektPuffer.Par("@wslgrenz2", OleDbType.Double,    Wert(item.WS_Ladegrenze2))
                    };
        }

        /// <summary>
        /// Nullable-Wert als Parameterwert: <c>null</c> bleibt <c>null</c> und wird von
        /// <c>ProjektPuffer.Par</c> zu <see cref="DBNull"/>. Ohne diese Umschachtelung
        /// wuerde ein <c>int?</c> beim Boxen zwar korrekt zu null - der ausdrueckliche
        /// Weg macht aber sichtbar, dass hier NICHT auf 0 ausgewichen wird.
        /// </summary>
        private static object Wert(int? v)
        {
            return v.HasValue ? (object)v.Value : null;
        }

        private static object Wert(double? v)
        {
            return v.HasValue ? (object)v.Value : null;
        }

        /// <summary>
        /// Puffer-Fremdschluessel fuer das INSERT.
        ///
        /// <para>
        /// ZWEI REGELN. Erstens: NULL bleibt NULL, 0 wird NIE geschrieben -
        /// <c>WS_ID_Puffer</c>, <c>WS_ID_Puffer2</c> und <c>WQ_ID_Puffer</c> stehen unter
        /// einer erzwungenen Beziehung auf <c>Tab_Pufferspeicher.ID</c> (SchemaMigration
        /// Schritt 4), 0 ist dort keine gueltige ID und waere eine Phantom-Referenz.
        /// </para>
        ///
        /// <para>
        /// Zweitens: Eine ID, die auf KEINE Speicherzeile mehr zeigt, wird zu NULL
        /// abgeraeumt statt geschrieben. Das ist kein Schoenheitsfehler, sondern der
        /// Schutz des gesamten Speichervorgangs: <see cref="Add_WP_Waermeerzeuger"/>
        /// laeuft IMMER nach einem DELETE. Wuerde das INSERT an der Beziehung scheitern,
        /// waeren die Anlagen bereits geloescht und der Abbruch haette mehr zerstoert als
        /// der Datenverlust, den diese Anweisung beheben soll. Eine verwaiste Referenz
        /// wird deshalb protokolliert und faellt weg - genau die Normalisierung, die
        /// <c>WaermesenkeClass.Normalisieren</c> beim Lesen ohnehin vornimmt.
        /// </para>
        /// </summary>
        private static object PufferFkOderNull(int? id, string spalte, string bezeichner,
                                               Dictionary<int, bool> cache)
        {
            if (!id.HasValue || id.Value <= 0) return null;   // -> DBNull, nie 0

            if (PufferVorhanden(id.Value, cache)) return id.Value;

            Console.WriteLine("Energieanlage \"" + (bezeichner ?? "") + "\": " + spalte + " = " +
                              id.Value + " zeigt auf keinen Pufferspeicher mehr - " +
                              "die Referenz wird als leer gespeichert.");
            return null;
        }

        /// <summary>
        /// Gehört die Speicherzeile zu diesem Projekt? Kriterium für den Erhalt einer
        /// bereits gesetzten <c>ID_PUFFER</c>, wenn die Katalogauflösung scheitert.
        /// </summary>
        private static bool PufferGehoertZuProjekt(int idPuffer, int projektID)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ? AND ID_Projekt = ?",
                new OleDbParameter[] {
                    new OleDbParameter("@id", idPuffer),
                    new OleDbParameter("@proj", projektID)
                });

            return (v != null && v != DBNull.Value && Convert.ToInt32(v) > 0);
        }

        /// <summary>
        /// Gibt es die Speicherzeile? Nur TREFFER werden gemerkt: Ein Puffer kann
        /// waehrend derselben Schleife noch entstehen (<c>PufferSpCtrl.CopyFromStamm</c>),
        /// ein negatives Ergebnis darf deshalb nicht zwischengespeichert werden.
        /// </summary>
        private static bool PufferVorhanden(int id, Dictionary<int, bool> cache)
        {
            if (cache != null && cache.ContainsKey(id)) return true;

            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ?",
                new OleDbParameter[] { new OleDbParameter("@id", id) });

            bool vorhanden = (v != null && v != DBNull.Value && Convert.ToInt32(v) > 0);
            if (vorhanden && cache != null) cache[id] = true;
            return vorhanden;
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

                List<OleDbParameter> ps = new List<OleDbParameter>
                    { new OleDbParameter("@pID", projektID) };

                if (nType != TYP_ALLE)
                {
                    sql += " AND ID_Type = ?";
                    ps.Add(new OleDbParameter("@type", nType));
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
                    new OleDbParameter("@pID", projektID));

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

                    // Anweisung und Parameter stehen zentral (siehe SQL_ANLAGE_INSERT):
                    // dieselbe Wahrheit, die auch WErzeugerCtrl.Insert benutzt.
                    if (!DataRepository.ExecuteSQL(SQL_ANLAGE_INSERT,
                                                   AnlagenParameter(projektID, item, pufferCache)))
                    {
                        SpVariantenVerwerfen("das Neuanlegen der Anlagen ist gescheitert");
                        return false;
                    }

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

        // Kleine Hilfsfunktion für die Typprüfung (kommt mit in die Ctrl)
        private static bool CheckType(WErzeugerModel item, int typ, int refTyp)
        {
            return item.ID_Type == typ || item.ID_Type == refTyp;
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
        /// WERTEHERKUNFT. Aus der Katalogzeile kommt nur <c>ID_Brennstoff</c>; alle Zahlen
        /// stammen danach aus <c>Tab_Brennstoff_Stamm</c> - exakt die Quellen, aus denen auch
        /// der Kosten-Weg liest: <c>Form_Kosten_Auswahl</c> holt Hi, Hs und Einheit von dort,
        /// <c>Form_Kosten</c> die Standardpreise und Emissionsfaktoren. <c>ID_Umrechnung</c> ist
        /// im Dialog ebenfalls abgeleitet (Brennstoff + Einheit) und wird hier genauso ermittelt.
        ///
        /// IDEMPOTENT. Derselbe COUNT-Test wie im Kosten-Dialog verhindert doppelte Sätze - er
        /// trägt den Bearbeiten-Zweig des Wizards, der bei jedem Speichern erneut durchläuft.
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

                object oBrennstoff = DataRepository.ExecuteScalar(
                    "SELECT ID_Brennstoff FROM energy_carrier WHERE id = ?",
                    new OleDbParameter[] { new OleDbParameter("@cid", carrierId) });
                if (oBrennstoff == null) continue;   // Katalogzeile fehlt -> nichts anzulegen
                int idBrennstoff = Convert.ToInt32(oBrennstoff);

                // Default-Werte aus dem Brennstoff-Stamm (Preise/Emissionen)
                double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", idBrennstoff));
                double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", idBrennstoff));
                double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", idBrennstoff));
                double default_co2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", idBrennstoff));
                double default_so2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", idBrennstoff));
                double default_nox = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", idBrennstoff));

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
                    new OleDbParameter[] {
                        new OleDbParameter("@pid", projektID),
                        new OleDbParameter("@eid", carrierId)
                    });
                if (oVorhanden != null && Convert.ToInt32(oVorhanden) > 0) continue;

                // Preis-Historie. leistungspreis wird ausdrücklich mitgeschrieben (Befund B5).
                string sqlHistory = @"INSERT INTO energy_price
                     (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                if (!DataRepository.ExecuteSQL(sqlHistory, new OleDbParameter[] {
                    new OleDbParameter("@cid",  carrierId),
                    new OleDbParameter("@prid", projektID),
                    new OleDbParameter("@ap",   Math.Round(default_arbeitspreis, 4)),
                    new OleDbParameter("@hi",   Math.Round(hi, 4)),
                    new OleDbParameter("@gp",   Math.Round(default_grundpreis, 4)),
                    new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                    new OleDbParameter("@au",   einheit),
                    new OleDbParameter("@lp",   Math.Round(default_leistungspreis, 4))
                })) return false;

                // Projekt-Einstellungen
                string sqlInsert = @"INSERT INTO energy_Project_settings
                     (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                      custom_price_base, ID_Umrechnung, co2, so2, nox)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                if (!DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                    new OleDbParameter("@pid",    projektID),
                    new OleDbParameter("@eid",    carrierId),
                    new OleDbParameter("@p",      Math.Round(default_arbeitspreis, 4)),
                    new OleDbParameter("@pl",     Math.Round(default_leistungspreis, 4)),
                    new OleDbParameter("@h",      Math.Round(hi, 4)),
                    new OleDbParameter("@hs",     Math.Round(hs, 4)),
                    new OleDbParameter("@b",      Math.Round(default_grundpreis, 4)),
                    new OleDbParameter("@convid", convId),
                    new OleDbParameter("@co2",    default_co2),
                    new OleDbParameter("@so2",    default_so2),
                    new OleDbParameter("@nox",    default_nox)
                })) return false;
            }

            return true;
        }

        /// <summary>
        /// Dieselbe Ableitung wie <c>Form_Kosten_Auswahl.GetConvID</c>: Umrechnungssatz über
        /// Brennstoff und Abrechnungseinheit (from_unit = to_unit). -1, wenn es keinen gibt -
        /// genau der Wert, den der Dialog in diesem Fall ebenfalls schreibt.
        /// </summary>
        private static int ConvIdErmitteln(int idBrennstoff, string einheit)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?",
                new OleDbParameter[] {
                    new OleDbParameter("@cid", idBrennstoff),
                    new OleDbParameter("@fu", einheit),
                    new OleDbParameter("@tu", einheit)
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
                OleDbParameter[] psZ = {
                    new OleDbParameter("@id", OleDbType.Integer) { Value = zID },
                    new OleDbParameter("@pid", OleDbType.Integer) { Value = projektID },
                    new OleDbParameter("@fl", OleDbType.Double) { Value = item.Wohnflaeche },
                    new OleDbParameter("@Einheit", OleDbType.VarWChar) { Value = (object)(item.Einheit ?? "") },
                    new OleDbParameter("@jng", OleDbType.Double) { Value = item.Jahresnutzungsgrad },
                    new OleDbParameter("@dez", OleDbType.Boolean) { Value = item.DezentralWarmwasser }
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

            OleDbParameter[] ps = {
                new OleDbParameter("@name", model.m_szProjektname),
                new OleDbParameter("@bearb", model.m_szBearbeiter),
                new OleDbParameter("@besch", model.m_szBeschreibung),
                new OleDbParameter("@kunde", model.m_szKunde),
                new OleDbParameter("@date", OleDbType.Date) { Value = model.m_Aenderungsdatum },
                new OleDbParameter("@klima", model.m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = model.m_Erstelldatum }
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
            OleDbParameter[] ps = {
                new OleDbParameter("@name", model.m_szProjektname),
                new OleDbParameter("@bearb", model.m_szBearbeiter),
                new OleDbParameter("@klima", model.m_ID_Klimaregion),
                new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                new OleDbParameter("@kunde", model.m_szKunde),
                new OleDbParameter("@besch", model.m_szBeschreibung),
                new OleDbParameter("@id", projektID)
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@bez", item.m_szBezeichner ?? ""),
                    new OleDbParameter("@type", 4), // Typ 4 Stromspeicher
                    new OleDbParameter("@spID", item.m_ID)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_WaermebedarfExtern(int projektID, List<Z_ProjWaermebedarfModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektWaermebedarf", "ID_Z") + 1;

            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = WaermebedarfStammCtrl.ApplyGanglinieToProjekt(item.m_szBezeichner, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Ganglinie;

                string sql = "INSERT INTO Z_ProjektWaermebedarf (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szBezeichner ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@pwID", item.ID_Prozesswaerme),
                    new OleDbParameter("@bez", item.szProzessname ?? ""),
                    new OleDbParameter("@sum", item.Summe)
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@svID", item.m_ID_Stromverbraucher),
                    new OleDbParameter("@bez", item.m_szVerbraucher ?? ""),
                    new OleDbParameter("@sum", item.m_Summe)
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szStromganglinie ?? "")
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szSolarganglinie ?? "")
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

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@bwID", item.ID_Brauchwasser),
                    new OleDbParameter("@bez", item.szBezeichner ?? ""),
                    new OleDbParameter("@sum", item.Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }
 
    }
}
