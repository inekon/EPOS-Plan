using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf Tab_Preisreihe / Tab_PreisreiheDaten - die Ablage importierter
    // Preiszeitreihen (Fachkonzept Stromspeicher 4.1 a und 8.4, angelegt von
    // SchemaMigration Schritt 12b).
    //
    // Bauform nach dem Ganglinienmuster (StromganglinieStammCtrl.ImportGanglinie):
    // Kopf und alle Werte in EINER Transaktion, IDs explizit ueber MAX(ID)+1 auf der
    // TRANSAKTIONSVERBINDUNG ermittelt, Werte in Einfuegereihenfolge, Lesen mit
    // ORDER BY ID.
    //
    // Bewusst NICHT uebernommen wurden drei Stolperstellen des Vorbilds:
    //   * ID_Projekt wird hier immer mitgeschrieben (StromganglinieCtrl.Insert laesst
    //     es weg, obwohl spaeter danach gefiltert wird),
    //   * die Daten-ID wird explizit vergeben statt als AutoWert - die Reihenfolge der
    //     35.040 Werte haengt damit nicht mehr an der Vergabestrategie des Providers,
    //   * das Feld heisst ID_Preisreihe und traegt auch die Preisreihen-ID (im Vorbild
    //     heisst es m_ID_GanglinieDaten und traegt die KOPF-ID).
    //
    // Durchgaengig ueber DataRepository bzw. eine eigene Transaktionsverbindung mit
    // ?-Parametern; kein RecordSet (CLAUDE.md).
    // ---------------------------------------------------------------------------
    public class PreisreiheCtrl
    {
        public const string TABLE_KOPF = "Tab_Preisreihe";
        public const string TABLE_DATEN = "Tab_PreisreiheDaten";

        /// <summary>
        /// Groesse eines Zwischenberichts beim Schreiben - der Fortschrittsanzeige des
        /// Importdialogs. 35.040 Einzel-INSERTs dauern auf einer Netzwerkdatenbank
        /// sichtbar lange; ohne Rueckmeldung wirkt das Programm eingefroren.
        /// </summary>
        public const int FORTSCHRITT_SCHRITT = 1000;

        private readonly List<PreisreiheModel> _internalList = new List<PreisreiheModel>();

        public int rows => _internalList.Count;
        public List<PreisreiheModel> items => _internalList;

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die beiden Tabellen an, falls die Migration noch nicht gelaufen ist -
        /// die tolerante Rueckfallebene nach dem Muster
        /// <c>StromspeicherVarianteCtrl.StelleTabelleSicher</c> bzw.
        /// <c>ErgebnisCtrl.StelleStromspeicherTabelleSicher</c>.
        /// </summary>
        /// <remarks>
        /// Bewusst still: Schlaegt die Anlage fehl, meldet der nachfolgende Zugriff den
        /// Fehler an der Stelle, an der er den Anwender wirklich betrifft. Zweimal
        /// dieselbe Meldung waere nur laestig.
        /// </remarks>
        public static void StelleTabellenSicher()
        {
            try
            {
                if (!TabelleVorhanden(TABLE_KOPF))
                {
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_CREATE_PREISREIHE);
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_INDEX_PREISREIHE);
                }

                if (!TabelleVorhanden(TABLE_DATEN))
                {
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_CREATE_PREISREIHEDATEN);
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_INDEX_PREISREIHEDATEN);
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_FK_PREISREIHEDATEN);
                }
            }
            catch { /* der eigentliche Zugriff meldet den Fehler */ }
        }

        private static bool TabelleVorhanden(string tabelle)
        {
            try
            {
                DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]");
                return true;
            }
            catch { return false; }
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Alle Reihen, die einem Projekt zur Verfuegung stehen: seine eigenen und die
        /// Stammreihen (<c>ID_Projekt IS NULL</c>).
        /// </summary>
        /// <remarks>
        /// Die Werteanzahl wird je Reihe mitgezaehlt, damit die Auswahlliste "8760
        /// Werte" anzeigen kann, ohne 35.040 Zeilen zu laden. Ein GROUP-BY-Join ueber
        /// beide Tabellen waere kuerzer, liefert in Access aber keine Reihen ohne
        /// Werte - und genau die will man sehen, um sie loeschen zu koennen.
        /// </remarks>
        public List<PreisreiheModel> ReadVerfuegbare(int idProjekt)
        {
            _internalList.Clear();

            // Nur SPOTREIHEN: Traegerreihen (KD4/FK6a, ID_Energietraeger gesetzt) und
            // Monatsreihen gehoeren nicht in die Spot-Auswahl - sonst wuerde die
            // Stichtagsregel (ReadZumJahr) der Simulation eine Leistungspreis-Reihe
            // mit 12 Werten als Spotreihe kueren.
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE_KOPF + "] " +
                "WHERE (ID_Projekt IS NULL OR ID_Projekt = ?) " +
                "AND ID_Energietraeger IS NULL " +
                "AND (Aufloesung = '" + DbWerte.PREISREIHE_AUFLOESUNG_STUNDE + "' " +
                "OR Aufloesung = '" + DbWerte.PREISREIHE_AUFLOESUNG_VIERTELSTUNDE + "') " +
                "ORDER BY Jahr DESC, Bezeichner",
                new DbParam("@proj", idProjekt));

            if (dt == null) return _internalList;

            foreach (DataRow r in dt.Rows)
            {
                PreisreiheModel m = AusZeile(dt, r);
                m.Werteanzahl = Werteanzahl(m.ID);
                _internalList.Add(m);
            }
            return _internalList;
        }

        /// <summary>
        /// Die geltende Leistungspreis-Reihe eines Energietraegers (Etappe KD4, FK6a):
        /// Projektreihe vor Stammreihe, je Ebene die mit dem juengsten Jahr.
        /// <c>null</c>, wenn der Traeger keine Reihe hat - dann gilt der konstante
        /// Satz (<c>price_power</c> bzw. <c>custom_price_power</c>).
        /// </summary>
        public PreisreiheModel ReadTraegerReihe(int idProjekt, int idEnergietraeger)
        {
            if (idEnergietraeger <= 0) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE_KOPF + "] " +
                "WHERE ID_Energietraeger = ? AND (ID_Projekt IS NULL OR ID_Projekt = ?) " +
                "ORDER BY Jahr DESC",
                new DbParam("@traeger", idEnergietraeger),
                new DbParam("@proj", idProjekt));

            if (dt == null || dt.Rows.Count == 0) return null;

            PreisreiheModel projekt = null, stamm = null;
            foreach (DataRow r in dt.Rows)
            {
                PreisreiheModel m = AusZeile(dt, r);
                if (m.ID_Projekt > 0) { if (projekt == null) projekt = m; }
                else if (stamm == null) stamm = m;
            }

            PreisreiheModel treffer = projekt ?? stamm;
            if (treffer != null) treffer.Werteanzahl = Werteanzahl(treffer.ID);
            return treffer;
        }

        /// <summary>Eine Reihe ueber ihre ID; <c>null</c>, wenn es sie nicht gibt.</summary>
        public PreisreiheModel ReadSingle(int id)
        {
            if (id <= 0) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE_KOPF + "] WHERE ID = ?",
                new DbParam("@id", id));

            if (dt == null || dt.Rows.Count == 0) return null;

            PreisreiheModel m = AusZeile(dt, dt.Rows[0]);
            m.Werteanzahl = Werteanzahl(m.ID);
            return m;
        }

        /// <summary>
        /// Die zum Stichtagsjahr passende Reihe eines Projekts (Fachkonzept 4.1):
        /// bevorzugt das gesuchte Jahr, sonst das naechstaeltere, sonst das
        /// naechstjuengere. <c>null</c>, wenn ueberhaupt keine Reihe vorliegt.
        /// </summary>
        /// <remarks>
        /// Dieselbe Regelrichtung wie bei der Preisversion des Arbeitspreises: Ein
        /// aelteres Jahr ist die konservativere Aussage als ein juengeres, das es zum
        /// Zeitpunkt der Planung noch gar nicht gab. Erst wenn es kein aelteres gibt,
        /// wird das juengste genommen - dann ist die Alternative "gar kein Preis".
        /// </remarks>
        public PreisreiheModel ReadZumJahr(int idProjekt, int jahr)
        {
            List<PreisreiheModel> alle = ReadVerfuegbare(idProjekt);
            if (alle.Count == 0) return null;

            PreisreiheModel treffer = null;
            PreisreiheModel aelteste = null;
            PreisreiheModel juengste = null;

            foreach (PreisreiheModel m in alle)
            {
                if (m.Jahr == jahr && (treffer == null || m.ID_Projekt > 0)) treffer = m;
                if (m.Jahr <= jahr && (aelteste == null || m.Jahr > aelteste.Jahr)) aelteste = m;
                if (juengste == null || m.Jahr < juengste.Jahr) juengste = m;
            }

            return treffer ?? aelteste ?? juengste;
        }

        /// <summary>
        /// Die Werte einer Reihe in Einfuegereihenfolge (<c>ORDER BY ID</c>), oder ein
        /// leeres Array.
        /// </summary>
        public double[] ReadWerte(int idPreisreihe)
        {
            if (idPreisreihe <= 0) return new double[0];

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wert FROM [" + TABLE_DATEN + "] WHERE ID_Preisreihe = ? ORDER BY ID",
                new DbParam("@id", idPreisreihe));

            if (dt == null) return new double[0];

            double[] werte = new double[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                object w = dt.Rows[i]["Wert"];
                werte[i] = (w == null || w == DBNull.Value) ? 0.0 : Convert.ToDouble(w);
            }
            return werte;
        }

        /// <summary>Anzahl der Werte einer Reihe, ohne sie zu laden.</summary>
        public int Werteanzahl(int idPreisreihe)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE_DATEN + "] WHERE ID_Preisreihe = ?",
                new DbParam("@id", idPreisreihe));

            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Legt Kopf und Werte einer Preisreihe in EINER Transaktion an und traegt die
        /// vergebene ID in das Modell zurueck. Rueckgabe -1 bei Fehler.
        /// </summary>
        /// <param name="kopf">Kopfdaten; <c>ID</c> wird ueberschrieben.</param>
        /// <param name="werte">Die Zeitreihe [ct/kWh]; darf nicht leer sein.</param>
        /// <param name="fortschritt">
        /// Wird alle <see cref="FORTSCHRITT_SCHRITT"/> Werte mit der Anzahl der bereits
        /// geschriebenen Zeilen aufgerufen. <c>null</c> = keine Rueckmeldung.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>Alles oder nichts.</b> Kopf und Werte gehoeren zusammen; ein Abbruch nach
        /// dem Kopf hinterliesse eine leere Reihe in der Auswahlliste. Deshalb EINE
        /// Transaktion ueber beides - dasselbe Vorgehen wie
        /// <c>StromganglinieStammCtrl.ImportGanglinie</c>.
        /// </para>
        /// <para>
        /// <b>Die IDs kommen von der Transaktionsverbindung.</b>
        /// <c>DataRepository.ExecuteScalar</c> oeffnet eine ZWEITE Verbindung und saehe
        /// die noch nicht bestaetigten Zeilen nicht - der MAX(ID)+1 stuende dann fuer
        /// jede Zeile auf demselben Wert.
        /// </para>
        /// <para>
        /// <b>Ein Kommando, nur getauschte Werte.</b> Die Parameter werden einmal
        /// angelegt; je Zeile wechselt nur <c>.Value</c>. Bei 35.040 Werten spart das
        /// den Aufbau von 70.080 Parameterobjekten.
        /// </para>
        /// </remarks>
        public int Insert(PreisreiheModel kopf, double[] werte, Action<int> fortschritt = null)
        {
            if (kopf == null) throw new ArgumentNullException(nameof(kopf));
            if (werte == null || werte.Length == 0)
                throw new ArgumentException("Eine Preisreihe ohne Werte wird nicht gespeichert.", nameof(werte));

            StelleTabellenSicher();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int neueId = MaxId(v, TABLE_KOPF) + 1;

                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = neueId });
                        p.Add(new DbParam("@proj", DbParamTyp.Integer) { Wert = kopf.ID_Projekt > 0 ? (object)kopf.ID_Projekt : DBNull.Value });
                        p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = kopf.Bezeichner ?? "" });
                        p.Add(new DbParam("@jahr", DbParamTyp.Integer) { Wert = kopf.Jahr });
                        p.Add(new DbParam("@aufl", DbParamTyp.VarWChar) { Wert = kopf.Aufloesung ?? DbWerte.PREISREIHE_AUFLOESUNG_STUNDE });
                        p.Add(new DbParam("@einh", DbParamTyp.VarWChar) { Wert = kopf.Einheit ?? DbWerte.PREISREIHE_EINHEIT_CT_KWH });
                        p.Add(new DbParam("@traeger", DbParamTyp.Integer) { Wert = kopf.ID_Energietraeger > 0 ? (object)kopf.ID_Energietraeger : DBNull.Value });
                        v.Ausfuehren("INSERT INTO [" + TABLE_KOPF + "] (ID, ID_Projekt, Bezeichner, Jahr, " +
                        "Aufloesung, Einheit, ID_Energietraeger) " +
                        "VALUES (?, ?, ?, ?, ?, ?, ?)", p.ToArray());
                    }

                    int datenId = MaxId(v, TABLE_DATEN);

                    for (int i = 0; i < werte.Length; i++)
                    {
                        v.Ausfuehren(
                            "INSERT INTO [" + TABLE_DATEN + "] (ID, ID_Preisreihe, Wert) VALUES (?, ?, ?)",
                            new DbParam("@id", DbParamTyp.Integer) { Wert = ++datenId },
                            new DbParam("@kopf", DbParamTyp.Integer) { Wert = neueId },
                            new DbParam("@wert", DbParamTyp.Double) { Wert = werte[i] });

                        if (fortschritt != null && (i + 1) % FORTSCHRITT_SCHRITT == 0) fortschritt(i + 1);
                    }

                    v.Commit();
                    kopf.ID = neueId;
                    kopf.Werteanzahl = werte.Length;

                    if (fortschritt != null) fortschritt(werte.Length);
                    return neueId;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    DataRepository.FehlerMelden("Die Preisreihe konnte nicht gespeichert werden: " + ex.Message);
                    return -1;
                }
            }
        }

        /// <summary>
        /// Loescht eine Reihe samt Werten. Die Werte werden AUSDRUECKLICH mitgeloescht,
        /// nicht der Loeschweitergabe ueberlassen: Auf einer Datenbank, deren Beziehung
        /// FK_PreisreiheDaten nicht angelegt werden konnte (Migrationsprotokoll), blieben
        /// sonst bis zu 35.040 Waisenzeilen stehen.
        /// </summary>
        public bool Delete(int id)
        {
            if (id <= 0) return false;

            DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE_DATEN + "] WHERE ID_Preisreihe = ?",
                new DbParam("@id", id));

            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE_KOPF + "] WHERE ID = ?",
                new DbParam("@id", id));
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        private static int MaxId(DbVorgang v, string tabelle)
        {
            object m = v.Skalar("SELECT MAX(ID) FROM [" + tabelle + "]");
            return (m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0;
        }

        /// <summary>Namensbasierte Abbildung mit <c>Columns.Contains</c>-Wache.</summary>
        private static PreisreiheModel AusZeile(DataTable dt, DataRow r)
        {
            PreisreiheModel m = new PreisreiheModel();

            m.ID = Zahl(dt, r, "ID");
            m.ID_Projekt = Zahl(dt, r, "ID_Projekt");
            m.ID_Energietraeger = Zahl(dt, r, "ID_Energietraeger");
            m.Bezeichner = Text(dt, r, "Bezeichner");
            m.Jahr = Zahl(dt, r, "Jahr");

            string aufl = Text(dt, r, "Aufloesung");
            if (aufl.Length > 0) m.Aufloesung = aufl;

            string einh = Text(dt, r, "Einheit");
            if (einh.Length > 0) m.Einheit = einh;

            return m;
        }

        private static int Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
