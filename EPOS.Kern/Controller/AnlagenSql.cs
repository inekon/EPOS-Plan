using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EINE Anweisung, mit der eine Anlagenzeile entsteht — und die Parameterfabrik
    /// dazu.
    ///
    /// <para><b>Warum eigene Klasse (Umsetzungskonzept iU3, Kante K6).</b> Zwei ganz
    /// verschiedene Wege legen Anlagenzeilen an: der Wizard-Weg
    /// (<c>WizardCtrl.Add_WP_Waermeerzeuger</c>) und <see cref="WErzeugerCtrl.Insert"/>.
    /// Der Kommentar bei <see cref="SQL_ANLAGE_INSERT"/> verlangt seit Paket 1
    /// ausdrücklich „EINE WAHRHEIT" — zwei Einfügewege mit unterschiedlichem Spaltensatz
    /// waren genau der Fehler, den er verhindern soll. Bis iU3 stand diese Wahrheit im
    /// <c>WizardCtrl</c>, und <see cref="WErzeugerCtrl"/> zog damit den gesamten Wizard
    /// samt Oberfläche in den Rechenpfad. Jetzt steht sie hier: keine Oberfläche, kein
    /// Dialog, nur SQL und Parameter.</para>
    ///
    /// <para><c>WizardCtrl</c> behält seine Fläche und LEITET WEITER
    /// (<c>WizardCtrl.SQL_ANLAGE_INSERT</c>, <c>WizardCtrl.AnlagenParameter</c>) — alle
    /// bestehenden Aufrufer bleiben gültig.</para>
    /// </summary>
    internal static class AnlagenSql
    {
        /// <summary>
        /// VOLLSTAENDIGES INSERT der Anlagenzeile - alle 57 Spalten.
        ///
        /// <para>
        /// WARUM VOLLSTAENDIG. Der Speicherweg aller Erzeuger ist Loeschen + Neuanlegen
        /// (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> gefolgt von
        /// <c>WizardCtrl.Add_WP_Waermeerzeuger</c>). Jede Spalte, die hier fehlt, ist
        /// damit bei JEDEM Speichern verloren - nicht nur beim Bearbeiten im Wizard,
        /// sondern auch ueber Karten und Kontextmenues. Bis Paket 1 fuehrte die Anweisung
        /// 29 Spalten; die 27 Spalten der Quellen-/Senken-Konfiguration
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
        public static DbParam[] AnlagenParameter(int projektID, WErzeugerModel item,
                                                        Dictionary<int, bool> pufferCache = null)
        {
            return new[] {
                        new DbParam("@pID", projektID),
                        new DbParam("@bez", item.Bezeichner ?? (object)DBNull.Value),
                        new DbParam("@art", item.Betriebsart ?? (object)DBNull.Value),
                        new DbParam("@sperr", item.Sperrung),
                        new DbParam("@svon", item.Sperrzeit_von),
                        new DbParam("@sbis", item.Sperrzeit_bis),
                        new DbParam("@vor", item.Vorlauf),
                        new DbParam("@rueck", item.Ruecklauf),
                        new DbParam("@biv", item.Bivalenter_Betrieb),
                        new DbParam("@ab", item.Abschaltpunkt),
                        new DbParam("@nutz", item.Nutzungszeit),
                        new DbParam("@grenz", item.Grenzleistung),
                        new DbParam("@koll", item.Kollektormodulanzahl),
                        new DbParam("@pvleist", item.PV_Leistung),
                        new DbParam("@neig", item.m_Neigung),
                        new DbParam("@azim", item.m_Azimut),
                        new DbParam("@type", item.ID_Type),

                        // Fremdschlüssel-Logik (IDs nur setzen, wenn der Typ passt)
                        new DbParam("@wp", CheckType(item, WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP) ? item.ID_WP : (object)DBNull.Value),
                        new DbParam("@sol", CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP) ? item.ID_Solar : (object)DBNull.Value),
                        new DbParam("@pv", CheckType(item, WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP) ? item.ID_PV : (object)DBNull.Value),
                        new DbParam("@sp", CheckType(item, WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP) ? item.ID_SP : (object)DBNull.Value),
                        new DbParam("@kes", CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP) ? item.ID_Kessel : (object)DBNull.Value),
                        new DbParam("@bhkw", (item.ID_Type == WizardItemClass.BHKW_TYP) ? item.ID_BHKW : (object)DBNull.Value),
                        new DbParam("@puf", (item.ID_Type == WizardItemClass.PUFFER_TYP && item.ID_PUFFER > 0) ? item.ID_PUFFER : (object)DBNull.Value),

                        new DbParam("@stab", item.Heizstab),
                        new DbParam("@vol", item.Volumen),
                        new DbParam("@mix", item.rendeMix),
                        new DbParam("@solan", item.Solaranteil),
                        // Rohwert: NULL bleibt NULL. 0 und NULL heißen beide "kein
                        // Energieträger" (SchemaKatalog, Schritt 8), der Bestand führt
                        // aber beide Schreibweisen - und ein Speichern soll keine davon
                        // in die andere umschreiben.
                        ProjektPuffer.Par("@idcarrier", DbParamTyp.Integer, Wert(item.ID_CarrierRoh)),

                        // --- Kaskade und Betriebsmodus ---------------------------------
                        ProjektPuffer.Par("@prio",      DbParamTyp.Integer,   Wert(item.Prioritaet)),
                        ProjektPuffer.Par("@bmtyp",     DbParamTyp.VarWChar,  item.BM_Typ),

                        // --- Wärmequelle ----------------------------------------------
                        ProjektPuffer.Par("@wqtyp",     DbParamTyp.VarWChar,  item.WQ_Typ),
                        ProjektPuffer.Par("@wqtemp",    DbParamTyp.Double,    Wert(item.WQ_Temp)),
                        ProjektPuffer.Par("@wqmon",     DbParamTyp.VarWChar,  item.WQ_Monatswerte),
                        ProjektPuffer.Par("@wqwoch",    DbParamTyp.VarWChar,  item.WQ_Wochenwerte),
                        ProjektPuffer.Par("@wqcsv",     DbParamTyp.VarWChar,  item.WQ_CSV),
                        ProjektPuffer.Par("@wqpuf",     DbParamTyp.VarWChar,  item.WQ_Puffer),
                        ProjektPuffer.Par("@wqidpuf",   DbParamTyp.Integer,
                            PufferFkOderNull(item.WQ_ID_Puffer, "WQ_ID_Puffer", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wqspreiz",  DbParamTyp.Double,    Wert(item.WQ_Spreizung)),
                        ProjektPuffer.Par("@wqregen",   DbParamTyp.Double,    Wert(item.WQ_Regeneration)),
                        ProjektPuffer.Par("@wqunbeg",   DbParamTyp.Boolean,   item.WQ_Unbegrenzt),
                        ProjektPuffer.Par("@wqtiefe",   DbParamTyp.Double,    Wert(item.WQ_Tiefe)),
                        ProjektPuffer.Par("@wqflaeche", DbParamTyp.Double,    Wert(item.WQ_Flaeche)),
                        ProjektPuffer.Par("@wqanzahl",  DbParamTyp.Integer,   Wert(item.WQ_Anzahl)),
                        ProjektPuffer.Par("@wqboden",   DbParamTyp.VarWChar,  item.WQ_Bodentyp),
                        ProjektPuffer.Par("@wqquell",   DbParamTyp.VarWChar,  item.WQ_Quellsystem),

                        // --- Wärmesenke -----------------------------------------------
                        ProjektPuffer.Par("@wstyp",     DbParamTyp.VarWChar,  item.WS_Typ),
                        ProjektPuffer.Par("@wsziel",    DbParamTyp.VarWChar,  item.WS_Ziel),
                        ProjektPuffer.Par("@wsidpuf",   DbParamTyp.Integer,
                            PufferFkOderNull(item.WS_ID_Puffer, "WS_ID_Puffer", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wslprio",   DbParamTyp.Integer,   Wert(item.WS_Ladeprio)),
                        ProjektPuffer.Par("@wslgrenz",  DbParamTyp.Double,    Wert(item.WS_Ladegrenze)),
                        ProjektPuffer.Par("@wslprioPV", DbParamTyp.Integer,   Wert(item.WS_Ladeprio_PV)),
                        ProjektPuffer.Par("@wsziel2",   DbParamTyp.VarWChar,  item.WS_Ziel2),
                        ProjektPuffer.Par("@wsidpuf2",  DbParamTyp.Integer,
                            PufferFkOderNull(item.WS_ID_Puffer2, "WS_ID_Puffer2", item.Bezeichner, pufferCache)),
                        ProjektPuffer.Par("@wslprio2",  DbParamTyp.Integer,   Wert(item.WS_Ladeprio2)),
                        ProjektPuffer.Par("@wslgrenz2", DbParamTyp.Double,    Wert(item.WS_Ladegrenze2))
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
        /// Schutz des gesamten Speichervorgangs: <c>WizardCtrl.Add_WP_Waermeerzeuger</c>
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
        /// Gibt es die Speicherzeile? Nur TREFFER werden gemerkt: Ein Puffer kann
        /// waehrend derselben Schleife noch entstehen (<c>PufferSpCtrl.CopyFromStamm</c>),
        /// ein negatives Ergebnis darf deshalb nicht zwischengespeichert werden.
        /// </summary>
        internal static bool PufferVorhanden(int id, Dictionary<int, bool> cache)
        {
            if (cache != null && cache.ContainsKey(id)) return true;

            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Pufferspeicher WHERE ID = ?",
                new DbParam[] { new DbParam("@id", id) });

            bool vorhanden = (v != null && v != DBNull.Value && Convert.ToInt32(v) > 0);
            if (vorhanden && cache != null) cache[id] = true;
            return vorhanden;
        }

        /// <summary>Kleine Hilfsfunktion für die Typprüfung (Typ oder Referenztyp).</summary>
        internal static bool CheckType(WErzeugerModel item, int typ, int refTyp)
        {
            return item.ID_Type == typ || item.ID_Type == refTyp;
        }
    }
}
