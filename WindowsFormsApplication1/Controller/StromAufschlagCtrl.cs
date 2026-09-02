using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf den Aufschlagsblock und die Verguetungssaetze in
    // energy_project_settings (Fachkonzept Stromspeicher 4.2/4.3, Arbeitspaket AP4;
    // Spalten aus SchemaMigration Schritt 12a).
    //
    // Durchgaengig NAMENSBASIERT mit Columns.Contains-Wache: Auf einer Datenbank, deren
    // Migration noch nicht durchgelaufen ist, liefert der Controller die Vorbelegung des
    // Modells statt einer Ausnahme - dasselbe Vorgehen wie StromspeicherVarianteCtrl.
    // Die Ordinalkette-Falle von Tab_Einstellungen gibt es hier nicht:
    // energy_project_settings wird im Bestand ausschliesslich ueber SELECT * mit
    // Spaltennamen-Zugriff gelesen.
    //
    // Kulturregel: Es wird nirgends eine Zeichenkette in eine Zahl umgewandelt - die
    // Werte kommen typisiert aus der DataTable.
    // ---------------------------------------------------------------------------
    public class StromAufschlagCtrl
    {
        public const string TABLE = "energy_project_settings";

        /// <summary>Preismodell-Code des Strom-Carriers in <c>pricing_model</c>.</summary>
        public const string PRICING_MODEL_STROM = "ELECTRICITY";

        // --- Sprachneutrale Komponentenschluessel (Schicht 2 der Drei-Schichten-Regel) ---
        //
        // Sie verbinden die Datenbankspalte, den Engine-Satz und den Anzeigetext, ohne
        // selbst Anzeigetext zu sein. Die Beschriftung holt die Oberflaeche ueber
        // MyResource.Resource.PREIS_KOMP_*.

        public const string KOMP_NETZENTGELT = "NETZENTGELT";
        public const string KOMP_UMLAGEN = "UMLAGEN";
        public const string KOMP_STROMSTEUER = "STROMSTEUER";
        public const string KOMP_KONZESSION = "KONZESSION";
        public const string KOMP_VERTRIEB = "VERTRIEB";

        /// <summary>Die fuenf Komponenten in Anzeigereihenfolge (Fachkonzept 4.2).</summary>
        public static readonly string[] KOMPONENTEN =
        {
            KOMP_NETZENTGELT, KOMP_UMLAGEN, KOMP_STROMSTEUER, KOMP_KONZESSION, KOMP_VERTRIEB
        };

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die Aufschlagsspalten an, falls die Migration noch nicht gelaufen ist -
        /// die tolerante Rueckfallebene nach dem Muster
        /// <c>ErgebnisCtrl.StelleKesselSpaltenSicher</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bewusst OHNE Vorbelegung: Die Vorschlagswerte setzt Migrationsschritt 12d.
        /// Hier entstehen nur die Spalten, damit ein Lesezugriff nicht scheitert; die
        /// Leseseite faellt dann auf die Vorgaben des <see cref="StromAufschlagModel"/>
        /// zurueck - dieselben Zahlen.
        /// </para>
        /// <para>
        /// <b>JE TABELLE pruefen.</b> <c>SchemaKatalog.Schritt12_Preismodell</c> fuehrt
        /// ZWEI Tabellen: die vierzehn Aufschlags- und Verguetungsspalten an
        /// <c>energy_project_settings</c> und die drei Preisquellen-Verweise an
        /// <c>Tab_StromspeicherVariante</c>. Wird das Schema nur EINER Tabelle gelesen,
        /// greift die Existenzpruefung fuer die Spalten der anderen nie - das
        /// <c>ALTER TABLE</c> lief dann bei jedem Oeffnen der Kostenverwaltung erneut und
        /// quittierte mit "Field … already exists". Deshalb dasselbe Vorgehen wie in
        /// <c>SchemaMigration.SpaltenAnlegen</c>: Schema je Tabelle, einmal gelesen und
        /// gemerkt.
        /// </para>
        /// <para>
        /// <b>Ohne Dialog.</b> Eine Vorsorge ist kein Bedienschritt - sie darf den
        /// Anwender nicht mit MessageBoxen behelligen. Das DDL laeuft deshalb ueber
        /// <see cref="StilleDb"/> statt ueber
        /// <c>DataRepository.ExecuteSQL</c>, das seine Fehler selbst als Dialog zeigt und
        /// damit am umschliessenden <c>try/catch</c> vorbeikommt. Muster ist
        /// <c>ErgebnisCtrl.ErgaenzeSpalte</c>. Echte Fehler bleiben sichtbar: Scheitert
        /// das Anlegen wirklich (Datei schreibgeschuetzt, Datenbank exklusiv geoeffnet),
        /// meldet der nachfolgende Lese- bzw. Schreibzugriff ueber
        /// <see cref="DataRepository"/> ganz regulaer.
        /// </para>
        /// <para>
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht, Schemaprobe statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen), SQLite-Spaltentypen statt
        /// Access-Typen (S4d vorgezogen).
        /// </para>
        /// </remarks>
        public static void StelleSpaltenSicher()
        {
            try
            {
                // Schema je Tabelle - einmal gelesen, dann gemerkt.
                Dictionary<string, HashSet<string>> schemaJeTabelle =
                    new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (SchemaSpalte s in SchemaKatalog.Schritt12_Preismodell)
                {
                    HashSet<string> vorhanden;
                    if (!schemaJeTabelle.TryGetValue(s.Tabelle, out vorhanden))
                    {
                        vorhanden = StilleDb.SpaltenNamen(s.Tabelle);
                        schemaJeTabelle[s.Tabelle] = vorhanden;
                    }

                    // null = Tabelle gibt es (noch) nicht. Sie hier anzulegen ist nicht
                    // Aufgabe dieser Vorsorge - das erledigen die Migration bzw.
                    // StromspeicherVarianteCtrl.StelleTabelleSicher.
                    if (vorhanden == null) continue;
                    if (vorhanden.Contains(s.Name)) continue;

                    // Protokoll statt Dialog - siehe <remarks>.
                    if (StilleDb.NonQuery(StilleDb.AlterTableAddColumn(
                            s.Tabelle, s.Name, s.TypDefinition)) < 0)
                        Protokoll(s.Tabelle + "." + s.Name + ": Spalte konnte nicht angelegt werden.");
                }
            }
            catch (Exception ex)
            {
                // Keine Verbindung, kein Schema - der eigentliche Zugriff meldet es.
                Protokoll(ex.Message);
            }
        }

        /// <summary>Protokolliert einen Vorsorge-Fehlschlag, ohne den Anwender zu stoeren.</summary>
        private static void Protokoll(string meldung)
        {
            try { Console.WriteLine("StromAufschlagCtrl.StelleSpaltenSicher: " + meldung); }
            catch { }
        }

        // =====================================================================
        // Energietraeger
        // =====================================================================

        /// <summary>
        /// Der Strom-Energietraeger eines Projekts (<c>pricing_model = 'ELECTRICITY'</c>),
        /// oder 0, wenn das Projekt keinen fuehrt.
        /// </summary>
        /// <remarks>
        /// Bei mehreren Stromtraegern - moeglich, aber unueblich - gilt der mit der
        /// kleinsten ID. Eine Auswahl anzubieten waere eine Entscheidung, die der
        /// Anwender im Kostenmodul ohnehin schon getroffen hat.
        /// </remarks>
        public static int StromCarrierId(int idProjekt)
        {
            if (idProjekt <= 0) return 0;

            object v = DataRepository.ExecuteScalar(
                "SELECT MIN(ec.id) FROM [" + TABLE + "] AS eps " +
                "INNER JOIN energy_carrier AS ec ON eps.[ID_Energieträger] = ec.id " +
                "WHERE eps.ID_Projekt = ? AND ec.pricing_model = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@pm", PRICING_MODEL_STROM));

            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Liest den Aufschlagsblock einer (Projekt, Energietraeger)-Zeile. Fehlt die
        /// Zeile oder fehlen die Spalten, kommt ein Modell mit den Vorgabewerten
        /// zurueck und <see cref="StromAufschlagModel.AusDatenbank"/> steht auf false.
        /// </summary>
        public StromAufschlagModel Read(int idProjekt, int idEnergietraeger)
        {
            StromAufschlagModel m = new StromAufschlagModel();
            m.ID_Projekt = idProjekt;
            m.ID_Energietraeger = idEnergietraeger;

            if (idProjekt <= 0 || idEnergietraeger <= 0) return m;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new OleDbParameter("@proj", idProjekt),
                new OleDbParameter("@eid", idEnergietraeger));

            if (dt == null || dt.Rows.Count == 0) return m;

            DataRow r = dt.Rows[0];

            // Nur was wirklich gepflegt ist, ueberschreibt die Vorgabe. NULL heisst
            // "nicht gepflegt" - dann gilt der Vorschlagswert des Fachkonzepts.
            Komponente(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT, ref m.Netzentgelt, ref m.Netzentgelt_Aktiv);
            Komponente(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN, ref m.Umlagen, ref m.Umlagen_Aktiv);
            Komponente(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER, ref m.Stromsteuer, ref m.Stromsteuer_Aktiv);
            Komponente(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION, ref m.Konzession, ref m.Konzession_Aktiv);
            Komponente(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB, ref m.Vertrieb, ref m.Vertrieb_Aktiv);

            Zahl(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_OVERRIDE, ref m.Override);
            Zahl(dt, r, SchemaKatalog.SPALTE_VERGUETUNG_PV, ref m.Verguetung_PV);
            Zahl(dt, r, SchemaKatalog.SPALTE_VERGUETUNG_BHKW, ref m.Verguetung_BHKW);

            string modus = Text(dt, r, SchemaKatalog.SPALTE_AUFSCHLAG_MODUS);
            if (modus.Length > 0) m.Modus = modus;

            m.AusDatenbank = true;
            return m;
        }

        /// <summary>
        /// Der Aufschlagsblock des Strom-Carriers eines Projekts - die Kurzform, die
        /// Simulation und Ergebnisanzeige brauchen.
        /// </summary>
        public StromAufschlagModel ReadStrom(int idProjekt)
        {
            return Read(idProjekt, StromCarrierId(idProjekt));
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Schreibt den Aufschlagsblock zurueck - ein zielgenaues UPDATE ueber
        /// (Projekt, Energietraeger), das die uebrigen Spalten der Zeile (Arbeitspreis,
        /// Heizwert, Emissionen) nicht anfasst.
        /// </summary>
        /// <returns>
        /// true, wenn eine Zeile geschrieben wurde. false heisst: Es gibt keine Zeile -
        /// der Energietraeger ist dem Projekt nicht zugeordnet. Angelegt wird sie hier
        /// NICHT; das ist Sache des Kostenmoduls (<c>ucFuelSettings</c>), das die
        /// Pflichtfelder kennt.
        /// </returns>
        public bool Update(StromAufschlagModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (m.ID_Projekt <= 0 || m.ID_Energietraeger <= 0) return false;

            StelleSpaltenSicher();

            string sql =
                "UPDATE [" + TABLE + "] SET " +
                Feld(SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT) +
                Feld(SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN) +
                Feld(SchemaKatalog.SPALTE_AUFSCHLAG_STROMSTEUER) +
                Feld(SchemaKatalog.SPALTE_AUFSCHLAG_KONZESSION) +
                Feld(SchemaKatalog.SPALTE_AUFSCHLAG_VERTRIEB) +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_MODUS + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_AUFSCHLAG_OVERRIDE + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_VERGUETUNG_PV + "] = ?, " +
                "[" + SchemaKatalog.SPALTE_VERGUETUNG_BHKW + "] = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?";

            int betroffen = DataRepository.ExecuteNonQuery(sql,
                new OleDbParameter("@netz", OleDbType.Double) { Value = m.Netzentgelt },
                new OleDbParameter("@netzA", OleDbType.Boolean) { Value = m.Netzentgelt_Aktiv },
                new OleDbParameter("@uml", OleDbType.Double) { Value = m.Umlagen },
                new OleDbParameter("@umlA", OleDbType.Boolean) { Value = m.Umlagen_Aktiv },
                new OleDbParameter("@st", OleDbType.Double) { Value = m.Stromsteuer },
                new OleDbParameter("@stA", OleDbType.Boolean) { Value = m.Stromsteuer_Aktiv },
                new OleDbParameter("@kz", OleDbType.Double) { Value = m.Konzession },
                new OleDbParameter("@kzA", OleDbType.Boolean) { Value = m.Konzession_Aktiv },
                new OleDbParameter("@vt", OleDbType.Double) { Value = m.Vertrieb },
                new OleDbParameter("@vtA", OleDbType.Boolean) { Value = m.Vertrieb_Aktiv },
                new OleDbParameter("@modus", OleDbType.VarWChar) { Value = m.Modus ?? DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT },
                new OleDbParameter("@over", OleDbType.Double) { Value = m.Override },
                new OleDbParameter("@vpv", OleDbType.Double) { Value = m.Verguetung_PV },
                new OleDbParameter("@vbhkw", OleDbType.Double) { Value = m.Verguetung_BHKW },
                new OleDbParameter("@proj", OleDbType.Integer) { Value = m.ID_Projekt },
                new OleDbParameter("@eid", OleDbType.Integer) { Value = m.ID_Energietraeger });

            if (betroffen > 0) m.AusDatenbank = true;
            return betroffen > 0;
        }

        /// <summary>Wert- und Aktiv-Spalte einer Komponente als SET-Fragment.</summary>
        private static string Feld(string spalte)
        {
            return "[" + spalte + "] = ?, [" + spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX + "] = ?, ";
        }

        // =====================================================================
        // Abbildung auf die Engine
        // =====================================================================

        /// <summary>
        /// Bildet den Aufschlagsblock auf den Engine-Satz ab (Fachkonzept 4.2). Ab hier
        /// rechnet ausschliesslich die Engine - Summe, wirksamer Wert und der nicht
        /// aufgeschluesselte Rest stehen dort und sind headless getestet.
        /// </summary>
        public static Aufschlagssatz AlsAufschlagssatz(StromAufschlagModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            List<Aufschlagskomponente> k = new List<Aufschlagskomponente>
            {
                new Aufschlagskomponente(KOMP_NETZENTGELT, m.Netzentgelt, m.Netzentgelt_Aktiv),
                new Aufschlagskomponente(KOMP_UMLAGEN, m.Umlagen, m.Umlagen_Aktiv),
                new Aufschlagskomponente(KOMP_STROMSTEUER, m.Stromsteuer, m.Stromsteuer_Aktiv),
                new Aufschlagskomponente(KOMP_KONZESSION, m.Konzession, m.Konzession_Aktiv),
                new Aufschlagskomponente(KOMP_VERTRIEB, m.Vertrieb, m.Vertrieb_Aktiv)
            };

            AufschlagsModus modus = m.Modus == DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT
                ? AufschlagsModus.Gesamtwert
                : AufschlagsModus.Aufgeschluesselt;

            return new Aufschlagssatz(k, modus, m.Override);
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        /// <summary>
        /// Uebernimmt Wert UND Aktiv-Schalter einer Komponente - aber nur, wenn der
        /// WERT gepflegt ist.
        /// </summary>
        /// <remarks>
        /// <b>Warum der Wert ueber den Schalter entscheidet.</b> Access kennt fuer YESNO
        /// kein NULL: Eine per <c>ADD COLUMN … YESNO</c> angelegte Spalte steht in jeder
        /// bestehenden Zeile sofort auf <c>False</c>. Wuerde der Schalter fuer sich
        /// gelesen, staende jede Zeile, deren Spalten die stille Rueckfallebene angelegt
        /// hat (ohne Migrationsschritt 12d), auf "alle Komponenten inaktiv" - und der
        /// Aufschlag waere stillschweigend 0. Der DOUBLE-Wert dagegen ist NULL, solange
        /// nichts gepflegt wurde, und ist damit das verlaessliche Kennzeichen. Ist er
        /// gepflegt, ist auch der Schalter gepflegt.
        /// </remarks>
        private static void Komponente(DataTable dt, DataRow r, string spalte,
                                       ref double wert, ref bool aktiv)
        {
            if (!dt.Columns.Contains(spalte)) return;

            object v = r[spalte];
            if (v == null || v == DBNull.Value) return;   // nicht gepflegt -> Vorgabe bleibt

            wert = Convert.ToDouble(v);

            string schalter = spalte + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX;
            if (!dt.Columns.Contains(schalter)) return;

            object s = r[schalter];
            if (s == null || s == DBNull.Value) return;
            aktiv = Convert.ToBoolean(s);
        }

        /// <summary>
        /// Uebernimmt einen Zahlenwert, wenn Spalte UND Wert vorhanden sind. NULL
        /// laesst die Vorgabe stehen; das unterscheidet "nicht gepflegt" von einer
        /// bewusst eingetragenen 0.
        /// </summary>
        private static void Zahl(DataTable dt, DataRow r, string spalte, ref double ziel)
        {
            if (!dt.Columns.Contains(spalte)) return;
            object v = r[spalte];
            if (v == null || v == DBNull.Value) return;
            ziel = Convert.ToDouble(v);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
