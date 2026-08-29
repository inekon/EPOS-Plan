using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf die Preisbestandteile eines BRENNSTOFFpreises in
    // energy_project_settings (Konzept BHKW-Wirtschaftlichkeit § 5.1, Etappe B2
    // Paket A; Spalten aus SchemaMigration Schritt 60).
    //
    // Bauform wie StromAufschlagCtrl: durchgängig NAMENSBASIERT mit
    // Columns.Contains-Wache, kein Zeichenkette-zu-Zahl, DDL-Vorsorge ohne Dialog.
    //
    // DER EINE UNTERSCHIED — und er ist der Grund für diese eigene Klasse:
    // StromAufschlagCtrl.Read lässt NULL auf die VORSCHLAGSWERTE des Modells
    // zurückfallen. Dieser Controller tut das NICHT. NULL heisst hier „kein Anteil
    // erfasst" und bleibt null (Konzept § 5.1, E5-Falle: bei Projekt 1030 wurden so
    // 11,746 ct/kWh wirksam, obwohl alle fünf Flags aus waren). Die Werte sind
    // deshalb double? und nicht double.
    // ---------------------------------------------------------------------------
    public class BrennstoffBestandteilCtrl
    {
        public const string TABLE = "energy_project_settings";

        // --- Sprachneutrale Komponentenschluessel (Schicht 2 der Drei-Schichten-Regel) ---
        //
        // Sie verbinden die Datenbankspalte, den Engine-Satz und den Anzeigetext, ohne
        // selbst Anzeigetext zu sein. Die Beschriftung holt die Oberflaeche ueber
        // MyResource.

        public const string KOMP_ENERGIESTEUER = "ENERGIESTEUER";
        public const string KOMP_CO2 = "CO2";
        public const string KOMP_NETZENTGELT = "NETZENTGELT";
        public const string KOMP_VERTRIEB = "VERTRIEB";

        /// <summary>Die vier Bestandteile in Anzeigereihenfolge (Konzept § 6.2).</summary>
        public static readonly string[] KOMPONENTEN =
        {
            KOMP_ENERGIESTEUER, KOMP_CO2, KOMP_NETZENTGELT, KOMP_VERTRIEB
        };

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die Bestandteilsspalten an, falls die Migration noch nicht gelaufen ist —
        /// die tolerante Rückfallebene nach dem Muster
        /// <c>StromAufschlagCtrl.StelleSpaltenSicher</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Bewusst OHNE jede Vorbelegung</b> — und zwar auch ohne die des Modus, die
        /// Migrationsschritt 60 vornimmt. Hier entstehen nur die Spalten, damit ein
        /// Lesezugriff nicht scheitert; die Leseseite fällt dann auf die Vorgabe des
        /// <see cref="BrennstoffBestandteilModel"/> zurück — denselben Wert
        /// (<c>Gesamtwert</c>). Für die ANTEILE gibt es ohnehin nichts vorzubelegen:
        /// NULL ist ihre fachliche Aussage, nicht ihr Mangel.
        /// </para>
        /// <para>
        /// <b>Ohne Dialog.</b> Eine Vorsorge ist kein Bedienschritt. Das DDL läuft
        /// deshalb über eine eigene <see cref="OleDbConnection"/> statt über
        /// <c>DataRepository.ExecuteSQL</c>, das seine Fehler selbst als Dialog zeigt und
        /// damit am umschliessenden <c>try/catch</c> vorbeikäme. Echte Fehler bleiben
        /// sichtbar: Scheitert das Anlegen wirklich (Datei schreibgeschützt, Datenbank
        /// exklusiv geöffnet), meldet der nachfolgende Lese- bzw. Schreibzugriff über
        /// <see cref="DataRepository"/> ganz regulär.
        /// </para>
        /// <para>
        /// Der Katalog führt hier nur EINE Tabelle; das Schema wird deshalb einmal
        /// gelesen und für alle neun Spalten verwendet — dieselbe Sparsamkeit wie in
        /// <c>SchemaMigration.SpaltenAnlegen</c>, ohne deren Tabellen-Wörterbuch zu
        /// brauchen.
        /// </para>
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();

                    HashSet<string> vorhanden = SpaltenNamen(conn, TABLE);

                    // null = Tabelle gibt es (noch) nicht. Sie hier anzulegen ist nicht
                    // Aufgabe dieser Vorsorge - das erledigt das Kostenmodul.
                    if (vorhanden == null) return;

                    foreach (SchemaSpalte s in SchemaKatalog.Schritt60_BrennstoffBestandteile)
                    {
                        if (vorhanden.Contains(s.Name)) continue;

                        try
                        {
                            using (OleDbCommand cmd = new OleDbCommand(
                                "ALTER TABLE [" + s.Tabelle + "] ADD COLUMN [" + s.Name + "] " +
                                s.TypDefinition, conn))
                                cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            // Protokoll statt Dialog - siehe <remarks>.
                            Protokoll(s.Tabelle + "." + s.Name + ": " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Keine Verbindung, kein Schema - der eigentliche Zugriff meldet es.
                Protokoll(ex.Message);
            }
        }

        /// <summary>
        /// Die Spaltennamen einer Tabelle, oder <c>null</c>, wenn es die Tabelle nicht
        /// gibt bzw. das Schema nicht lesbar ist. Eine Tabelle ohne Spalten kennt Access
        /// nicht — „keine Zeilen" heisst deshalb zuverlässig „keine Tabelle".
        /// </summary>
        private static HashSet<string> SpaltenNamen(OleDbConnection conn, string tabelle)
        {
            try
            {
                DataTable cols = conn.GetOleDbSchemaTable(
                    OleDbSchemaGuid.Columns, new object[] { null, null, tabelle, null });

                if (cols == null || cols.Rows.Count == 0) return null;

                HashSet<string> namen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in cols.Rows) namen.Add(Convert.ToString(r["COLUMN_NAME"]));
                return namen;
            }
            catch { return null; }
        }

        /// <summary>Protokolliert einen Vorsorge-Fehlschlag, ohne den Anwender zu stören.</summary>
        private static void Protokoll(string meldung)
        {
            try { Console.WriteLine("BrennstoffBestandteilCtrl.StelleSpaltenSicher: " + meldung); }
            catch { }
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Liest die Preisbestandteile einer (Projekt, Energieträger)-Zeile. Fehlt die
        /// Zeile oder fehlen die Spalten, kommt ein leeres Modell zurück und
        /// <see cref="BrennstoffBestandteilModel.AusDatenbank"/> steht auf false.
        /// </summary>
        /// <remarks>
        /// <b>NULL bleibt null.</b> Anders als <c>StromAufschlagCtrl.Read</c> setzt
        /// dieser Weg bei einem nicht gepflegten Wert KEINEN Vorschlagssatz ein. Ein
        /// Anteil, den niemand erfasst hat, ist kein Anteil — die Kohärenzprüfung (BW2)
        /// hängt genau an dieser Unterscheidung.
        /// </remarks>
        public BrennstoffBestandteilModel Read(int idProjekt, int idEnergietraeger)
        {
            BrennstoffBestandteilModel m = new BrennstoffBestandteilModel();
            m.ID_Projekt = idProjekt;
            m.ID_Energietraeger = idEnergietraeger;

            if (idProjekt <= 0 || idEnergietraeger <= 0) return m;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@eid", idEnergietraeger));

            if (dt == null || dt.Rows.Count == 0) return m;

            DataRow r = dt.Rows[0];

            Bestandteil(dt, r, SchemaKatalog.SPALTE_BB_ENERGIESTEUER, ref m.Energiesteuer, ref m.Energiesteuer_Aktiv);
            Bestandteil(dt, r, SchemaKatalog.SPALTE_BB_CO2, ref m.CO2, ref m.CO2_Aktiv);
            Bestandteil(dt, r, SchemaKatalog.SPALTE_BB_NETZENTGELT, ref m.Netzentgelt, ref m.Netzentgelt_Aktiv);
            Bestandteil(dt, r, SchemaKatalog.SPALTE_BB_VERTRIEB, ref m.Vertrieb, ref m.Vertrieb_Aktiv);

            string modus = Text(dt, r, SchemaKatalog.SPALTE_BB_MODUS);
            if (modus.Length > 0) m.Modus = modus;

            m.AusDatenbank = true;
            return m;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Schreibt die Bestandteile zurück — ein zielgenaues UPDATE über (Projekt,
        /// Energieträger), das die übrigen Spalten der Zeile (Arbeitspreis, Heizwert,
        /// Emissionen, den Strom-Aufschlagsblock) nicht anfasst.
        /// </summary>
        /// <remarks>
        /// <b>null wird DBNull, nicht 0.</b> Eine 0 wäre die Aussage „der Anteil ist
        /// null ct/kWh"; NULL ist die Aussage „es ist keiner erfasst". Der Unterschied
        /// ist genau der, den die Kohärenzprüfung braucht, und er muss deshalb auch den
        /// Weg in die Datenbank überstehen.
        /// </remarks>
        /// <returns>
        /// true, wenn eine Zeile geschrieben wurde. false heisst: Es gibt keine Zeile —
        /// der Energieträger ist dem Projekt nicht zugeordnet. Angelegt wird sie hier
        /// NICHT; das ist Sache des Kostenmoduls (<c>ucFuelSettings</c>), das die
        /// Pflichtfelder kennt.
        /// </returns>
        public bool Update(BrennstoffBestandteilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (m.ID_Projekt <= 0 || m.ID_Energietraeger <= 0) return false;

            StelleSpaltenSicher();

            string sql =
                "UPDATE [" + TABLE + "] SET " +
                Feld(SchemaKatalog.SPALTE_BB_ENERGIESTEUER) +
                Feld(SchemaKatalog.SPALTE_BB_CO2) +
                Feld(SchemaKatalog.SPALTE_BB_NETZENTGELT) +
                Feld(SchemaKatalog.SPALTE_BB_VERTRIEB) +
                "[" + SchemaKatalog.SPALTE_BB_MODUS + "] = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?";

            int betroffen = DataRepository.ExecuteNonQuery(sql,
                Wert("@est", m.Energiesteuer),
                new OleDbParameter("@estA", OleDbType.Boolean) { Value = m.Energiesteuer_Aktiv },
                Wert("@co2", m.CO2),
                new OleDbParameter("@co2A", OleDbType.Boolean) { Value = m.CO2_Aktiv },
                Wert("@netz", m.Netzentgelt),
                new OleDbParameter("@netzA", OleDbType.Boolean) { Value = m.Netzentgelt_Aktiv },
                Wert("@vt", m.Vertrieb),
                new OleDbParameter("@vtA", OleDbType.Boolean) { Value = m.Vertrieb_Aktiv },
                new OleDbParameter("@modus", OleDbType.VarWChar)
                {
                    Value = string.IsNullOrEmpty(m.Modus)
                            ? DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT : m.Modus
                },
                new OleDbParameter("@proj", OleDbType.Integer) { Value = m.ID_Projekt },
                new OleDbParameter("@eid", OleDbType.Integer) { Value = m.ID_Energietraeger });

            if (betroffen > 0) m.AusDatenbank = true;
            return betroffen > 0;
        }

        /// <summary>Wert- und Aktiv-Spalte eines Bestandteils als SET-Fragment.</summary>
        private static string Feld(string spalte)
        {
            return "[" + spalte + "] = ?, [" + spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX + "] = ?, ";
        }

        /// <summary>Ein DOUBLE-Parameter, der <c>null</c> als <c>DBNull</c> weitergibt.</summary>
        private static OleDbParameter Wert(string name, double? wert)
        {
            return new OleDbParameter(name, OleDbType.Double)
            {
                Value = wert.HasValue ? (object)wert.Value : DBNull.Value
            };
        }

        // =====================================================================
        // Abbildung auf die Engine
        // =====================================================================

        /// <summary>
        /// Bildet die Preisbestandteile auf den Engine-Satz ab. Ab hier rechnet
        /// ausschliesslich die Engine — Summe, wirksamer Wert und der nicht
        /// aufgeschlüsselte Rest stehen dort und sind headless getestet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>null geht als 0 in die Komponente.</b> Die Engine kennt keinen
        /// unbekannten Wert, und sie braucht auch keinen: Ein nicht erfasster Anteil
        /// trägt zur Summe nichts bei. Die Unterscheidung „null" gegen „0" bleibt im
        /// Modell, wo Dialog und Kohärenzprüfung sie brauchen — die Engine bekommt nur
        /// die Zahl.
        /// </para>
        /// <para>
        /// <b>Kein Override.</b> Dieser Block zerlegt einen Preis, statt ihn zu
        /// erhöhen; einen Gesamtaufschlag gibt es hier nicht (siehe
        /// <see cref="BrennstoffBestandteilModel"/>). Der Satz kommt deshalb mit
        /// <c>overrideCtKwh = 0</c>, und im Modus <c>Gesamtwert</c> ist sein
        /// <c>WirksamCtKwh</c> folgerichtig 0: Der erfasste Arbeitspreis bleibt
        /// unverändert, die Bestandteile sind Ausweis. Der aussagekräftige Wert ist in
        /// beiden Modi <c>SummeAktivCtKwh</c> — „soviel des Preises ist ausgewiesen".
        /// </para>
        /// </remarks>
        public static Aufschlagssatz AlsAufschlagssatz(BrennstoffBestandteilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            List<Aufschlagskomponente> k = new List<Aufschlagskomponente>
            {
                new Aufschlagskomponente(KOMP_ENERGIESTEUER, m.Energiesteuer ?? 0.0, m.Energiesteuer_Aktiv),
                new Aufschlagskomponente(KOMP_CO2, m.CO2 ?? 0.0, m.CO2_Aktiv),
                new Aufschlagskomponente(KOMP_NETZENTGELT, m.Netzentgelt ?? 0.0, m.Netzentgelt_Aktiv),
                new Aufschlagskomponente(KOMP_VERTRIEB, m.Vertrieb ?? 0.0, m.Vertrieb_Aktiv)
            };

            AufschlagsModus modus = m.Modus == DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT
                ? AufschlagsModus.Gesamtwert
                : AufschlagsModus.Aufgeschluesselt;

            return new Aufschlagssatz(k, modus, 0.0);
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        /// <summary>
        /// Übernimmt Wert UND Aktiv-Schalter eines Bestandteils. Fehlt die Spalte oder
        /// steht NULL darin, bleibt der Wert <c>null</c> — „kein Anteil erfasst".
        /// </summary>
        /// <remarks>
        /// <b>Warum der Schalter hier eigenständig gelesen wird.</b>
        /// <c>StromAufschlagCtrl.Komponente</c> liest ihn nur, wenn der WERT gepflegt
        /// ist: Access kennt für YESNO kein NULL, und eine per <c>ADD COLUMN</c>
        /// angelegte Spalte steht überall auf <c>False</c> — dort hätte das ohne diese
        /// Wache jeden Aufschlag stillschweigend auf 0 gesetzt. Hier ist <c>False</c>
        /// genau die richtige Aussage („Anteil nicht ausgewiesen"), es gibt keine
        /// Vorgabe zu verteidigen. Der Schalter wird deshalb gelesen, wie er dasteht;
        /// ein aktiver Schalter ohne Wert trägt 0 bei und ist damit ehrlich abgebildet.
        /// </remarks>
        private static void Bestandteil(DataTable dt, DataRow r, string spalte,
                                        ref double? wert, ref bool aktiv)
        {
            if (!dt.Columns.Contains(spalte)) return;

            object v = r[spalte];
            if (v != null && v != DBNull.Value) wert = Convert.ToDouble(v);

            string schalter = spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX;
            if (!dt.Columns.Contains(schalter)) return;

            object s = r[schalter];
            if (s == null || s == DBNull.Value) return;
            aktiv = Convert.ToBoolean(s);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
