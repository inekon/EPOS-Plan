using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// PAKET Q1 (Migrationsschritt 54, Konzept Brauchwasser/Heizung/Pufferspeicher
    /// § 8.1) — Lesen und Schreiben der QUELLPROFILE
    /// (<c>Tab_Quellprofil</c>/<c>Tab_QuellprofilDaten</c>).
    ///
    /// <para><b>Wozu.</b> Bis Q1 lag das Temperaturprofil einer Wärmequelle als zwei
    /// delimitierte Zeichenketten an der Anlage (<c>WQ_Monatswerte</c> „t1;…;t12",
    /// <c>WQ_Wochenwerte</c> „w1;…;w168"), und ein Stundenprofil lag gar nicht in der
    /// Datenbank — dort stand nur ein Dateipfad (<c>WQ_CSV</c>), der bei jeder
    /// Projektweitergabe ins Leere zeigte. Ein Quellprofil ist jetzt ein eigener
    /// Gegenstand mit Namen, Betriebsart und Werten; die Anlage verweist über
    /// <c>WQ_ID_Quellprofil</c> darauf (§ 8.1 Punkt 4, „Schlüssel- statt
    /// Indexkopplung").</para>
    ///
    /// <para><b>Drei Betriebsarten</b> (<c>DbWerte.WQ_PROFIL_BETRIEBSART_*</c>) mit
    /// 12, 365 oder 8760 Werten. Die Kachelung auf die 8760 Jahresstunden macht
    /// <see cref="Jahresprofil(string,double[])"/> — und zwar für die Tagesvariante
    /// ausdrücklich <b>kalenderunabhängig</b>: Tag <c>i</c> gilt für die 24 Stunden des
    /// Tages <c>i</c>, ohne jeden Wochentagsbezug. Das ist der fachliche Kern der
    /// Tagesvariante (§ 8.1 Punkt 2) und zugleich die Ablösung der DRITTEN
    /// Kalenderkonvention, die der additive Wochengang der Monatsvariante mitbrachte
    /// (Befund K1-O6).</para>
    ///
    /// <para><b>Dialogfrei.</b> Alle Methoden laufen über <see cref="StilleDb"/> bzw.
    /// eine eigene <see cref="OleDbConnection"/> — nie über <c>DataRepository</c>-Wege
    /// mit MessageBox, weil <see cref="Jahresprofil(int)"/> aus dem ENGINE-Pfad heraus
    /// gerufen wird (Konzept 13.4: ein Fehlerdialog mitten im Rechenlauf ist ein
    /// hängender Referenzlauf).</para>
    ///
    /// <para><b>Spaltentolerant.</b> Auf einer Datenbank vor Schritt 54 gibt es die
    /// Tabellen nicht; jede Lesemethode liefert dann leer bzw. <c>null</c>, und der
    /// Aufrufer fällt auf den Altweg zurück. Kein Aufruf wirft.</para>
    /// </summary>
    public static class QuellprofilCtrl
    {
        /// <summary>Kopfsatz eines Quellprofils — eine Zeile aus <c>Tab_Quellprofil</c>.</summary>
        public sealed class Kopf
        {
            public int ID;
            public int ID_Projekt;
            public string Bezeichner = "";

            /// <summary>Steuerwert aus <c>DbWerte.WQ_PROFIL_BETRIEBSART_*</c>.</summary>
            public string Betriebsart = DbWerte.WQ_PROFIL_BETRIEBSART_MONAT;

            /// <summary>Maßeinheit der Werte; heute ausnahmslos <c>°C</c>.</summary>
            public string Einheit = EINHEIT_GRAD_CELSIUS;

            /// <summary>Herkunft der Werte (Messstelle, Datei, Norm) — reines Anwenderfeld.</summary>
            public string Beschreibung = "";

            /// <summary>Zahl der Werte, die zu <see cref="Betriebsart"/> gehören; 0 bei unbekannter Betriebsart.</summary>
            public int Werteanzahl { get { return DbWerte.QuellprofilWerteanzahl(Betriebsart); } }

            /// <summary>Anzeigename für Auswahllisten; nie leer.</summary>
            public override string ToString()
            {
                return string.IsNullOrEmpty(Bezeichner) ? ("#" + ID) : Bezeichner;
            }
        }

        /// <summary>
        /// Die Einheit, in der die Engine rechnet. Sie steht als Vorbelegung in der
        /// Spalte <c>Einheit</c>; ausgewertet wird sie nicht (§ 8.1 — die Spalte
        /// dokumentiert, sie steuert nicht).
        /// </summary>
        public const string EINHEIT_GRAD_CELSIUS = "°C";

        /// <summary>Feldgrößen des Rechenkerns — hier nur als Ziel der Kachelung.</summary>
        private const int STUNDEN_JAHR = 8760;

        /// <summary>Tage je Monat im Nicht-Schaltjahr; Summe 365, mal 24 genau 8760.</summary>
        private static readonly int[] TAGE_PRO_MONAT = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>
        /// Die Quellprofile EINES Projekts, nach Bezeichner sortiert; nie <c>null</c>,
        /// aber leer, solange Schritt 54 nicht gelaufen ist.
        /// </summary>
        public static List<Kopf> LesenJeProjekt(int idProjekt)
        {
            List<Kopf> liste = new List<Kopf>();
            if (idProjekt <= 0) return liste;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Betriebsart, Einheit, Beschreibung " +
                "FROM [" + SchemaKatalog.TAB_QUELLPROFIL + "] WHERE ID_Projekt = ? " +
                "ORDER BY Bezeichner, ID",
                StilleDb.Par("@p", DbParamTyp.Integer, idProjekt));
            if (dt == null) return liste;

            foreach (DataRow r in dt.Rows) liste.Add(AusZeile(r));
            return liste;
        }

        /// <summary>Ein Quellprofil über seine ID; <c>null</c>, wenn es nicht (mehr) existiert.</summary>
        public static Kopf Lesen(int idProfil)
        {
            if (idProfil <= 0) return null;

            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, ID_Projekt, Bezeichner, Betriebsart, Einheit, Beschreibung " +
                "FROM [" + SchemaKatalog.TAB_QUELLPROFIL + "] WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idProfil));

            return (dt != null && dt.Rows.Count > 0) ? AusZeile(dt.Rows[0]) : null;
        }

        private static Kopf AusZeile(DataRow r)
        {
            return new Kopf
            {
                ID = StilleDb.Zahl(StilleDb.Feld(r, "ID")),
                ID_Projekt = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_QP_ID_PROJEKT)),
                Bezeichner = StilleDb.Text(StilleDb.Feld(r, SchemaKatalog.SPALTE_QP_BEZEICHNER)),
                Betriebsart = StilleDb.Text(StilleDb.Feld(r, SchemaKatalog.SPALTE_QP_BETRIEBSART)),
                Einheit = StilleDb.Text(StilleDb.Feld(r, SchemaKatalog.SPALTE_QP_EINHEIT)),
                Beschreibung = StilleDb.Text(StilleDb.Feld(r, SchemaKatalog.SPALTE_QP_BESCHREIBUNG))
            };
        }

        /// <summary>
        /// Die Werte eines Profils in POSITIONSREIHENFOLGE; <c>null</c> bei Fehler oder
        /// leerem Profil.
        ///
        /// <para>Sortiert wird über <c>[Index]</c>, nicht über <c>ID</c>: Die
        /// Positionsspalte ist genau dafür da, dass ein nachträglich geänderter Wert die
        /// Zuordnung Wert → Stunde nicht verschiebt (Muster-Abweichung gegenüber
        /// <c>Tab_StromganglinieDaten</c>, begründet bei
        /// <c>SchemaKatalog.SPALTE_QPD_INDEX</c>). Gelesen wird ROBUST: Der Rückgabewert
        /// hat die Länge <c>max(Index)+1</c>, Lücken bleiben 0 — die
        /// Vollständigkeitsprüfung macht der Aufrufer gegen
        /// <see cref="Kopf.Werteanzahl"/>.</para>
        ///
        /// <para><c>[Index]</c> steht in eckigen Klammern, weil <c>Index</c> in
        /// Access-SQL ein reserviertes Wort ist.</para>
        /// </summary>
        public static double[] WerteLesen(int idProfil)
        {
            if (idProfil <= 0) return null;

            DataTable dt = StilleDb.Tabelle(
                "SELECT [" + SchemaKatalog.SPALTE_QPD_INDEX + "], [" + SchemaKatalog.SPALTE_QPD_WERT + "] " +
                "FROM [" + SchemaKatalog.TAB_QUELLPROFILDATEN + "] WHERE ID_Quellprofil = ? " +
                "ORDER BY [" + SchemaKatalog.SPALTE_QPD_INDEX + "]",
                StilleDb.Par("@id", DbParamTyp.Integer, idProfil));
            if (dt == null || dt.Rows.Count == 0) return null;

            int hoechster = -1;
            foreach (DataRow r in dt.Rows)
            {
                int i = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_QPD_INDEX), -1);
                if (i > hoechster) hoechster = i;
            }
            if (hoechster < 0) return null;

            double[] werte = new double[hoechster + 1];
            foreach (DataRow r in dt.Rows)
            {
                int i = StilleDb.Zahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_QPD_INDEX), -1);
                if (i < 0 || i >= werte.Length) continue;
                werte[i] = StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_QPD_WERT));
            }
            return werte;
        }

        // =====================================================================
        // Kachelung auf das Jahresraster
        // =====================================================================

        /// <summary>
        /// Das Jahresprofil (8760 Stundenwerte) eines gespeicherten Quellprofils;
        /// <c>null</c>, wenn es nicht existiert, seine Betriebsart unbekannt ist oder
        /// die Wertzahl nicht zur Betriebsart passt.
        ///
        /// <para>Der Aufrufer im Engine-Pfad
        /// (<c>WaermequelleClass.Quelltemperatur</c>) fällt bei <c>null</c> auf die
        /// Außentemperatur zurück und meldet den Grund.</para>
        /// </summary>
        public static float[] Jahresprofil(int idProfil)
        {
            Kopf k = Lesen(idProfil);
            if (k == null) return null;

            return Jahresprofil(k.Betriebsart, WerteLesen(idProfil));
        }

        /// <summary>
        /// Die Kachelung selbst — ohne Datenbank, damit sie prüfbar und aus der
        /// Dialogvorschau heraus aufrufbar ist.
        ///
        /// <list type="bullet">
        ///   <item><b>Monat</b> (12 Werte): Jeder Wert gilt für alle Stunden seines
        ///   Monats; die Monatslängen sind die des Nicht-Schaltjahres (31, 28, 31, …),
        ///   ihre Summe × 24 ist genau 8760.</item>
        ///   <item><b>Tag</b> (365 Werte): <c>profil[h] = werte[h / 24]</c> — Tag
        ///   <c>i</c> gilt für die Stunden <c>24·i … 24·i+23</c>. <b>Kalenderunabhängig:
        ///   kein Wochentag, kein Jahresbezug, keine Verschiebung.</b> Genau darin
        ///   unterscheidet sie sich vom additiven Wochengang der Monatsvariante, der
        ///   eine dritte Kalenderkonvention mitbrachte (§ 8.1 Punkt 2, Randnotiz).</item>
        ///   <item><b>Stunde</b> (8760 Werte): unmittelbar das Jahresprofil.</item>
        /// </list>
        ///
        /// <para>Die Wertzahl MUSS zur Betriebsart passen — ein halb gefülltes Profil
        /// ergibt <c>null</c> statt eines stillschweigend mit Nullen aufgefüllten
        /// Jahres. Ein Temperaturprofil, das ab Mitte des Jahres 0 °C behauptet, wäre
        /// die schlimmere Antwort als „geht nicht".</para>
        /// </summary>
        public static float[] Jahresprofil(string betriebsart, double[] werte)
        {
            int soll = DbWerte.QuellprofilWerteanzahl(betriebsart);
            if (soll <= 0 || werte == null || werte.Length != soll) return null;

            float[] profil = new float[STUNDEN_JAHR];

            if (betriebsart == DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE)
            {
                for (int h = 0; h < STUNDEN_JAHR; h++) profil[h] = (float)werte[h];
                return profil;
            }

            if (betriebsart == DbWerte.WQ_PROFIL_BETRIEBSART_TAG)
            {
                // KALENDERUNABHÄNGIG: Tag i -> die 24 Stunden des Tages i.
                for (int h = 0; h < STUNDEN_JAHR; h++) profil[h] = (float)werte[h / 24];
                return profil;
            }

            // Monat: jeder Wert über die Stunden seines Monats.
            int index = 0;
            for (int m = 0; m < 12; m++)
                for (int tag = 0; tag < TAGE_PRO_MONAT[m]; tag++)
                    for (int h = 0; h < 24; h++)
                        profil[index++] = (float)werte[m];

            return profil;
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Legt ein Quellprofil an oder schreibt es neu und ersetzt dabei seinen
        /// gesamten Wertesatz. Rückgabe: die ID des Profils, 0 bei Fehler.
        ///
        /// <para><b>Alles in EINER Transaktion</b> — Kopf, Löschen der alten Werte und
        /// bis zu 8760 neue Zeilen. Ein abgebrochener Schreibvorgang darf kein halbes
        /// Profil hinterlassen; dieselbe Bauart wie
        /// <c>StromganglinieDatenCtrl.SpeichereGanglinieMitDaten</c>. Ein einziges,
        /// wiederverwendetes <see cref="OleDbCommand"/> mit drei Parametern hält den
        /// Vorgang auch bei 8760 Zeilen unter einer Sekunde (gemessen 1,1 s auf einer
        /// Kopie der produktiven Datenbank).</para>
        ///
        /// <para>Die Wertzahl wird gegen die Betriebsart geprüft — ein Profil mit
        /// falscher Länge kommt gar nicht erst in die Datenbank.</para>
        /// </summary>
        public static int Speichern(Kopf kopf, double[] werte)
        {
            if (kopf == null) return 0;

            int soll = DbWerte.QuellprofilWerteanzahl(kopf.Betriebsart);
            if (soll <= 0 || werte == null || werte.Length != soll) return 0;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int id = kopf.ID;

                    if (id > 0)
                    {
                        List<DbParam> pKopf = new List<DbParam>();
                        KopfParameter(pKopf, kopf);
                        pKopf.Add(StilleDb.Par("@id", DbParamTyp.Integer, id));
                        v.Ausfuehren(
                            "UPDATE [" + SchemaKatalog.TAB_QUELLPROFIL + "] SET " +
                            "[" + SchemaKatalog.SPALTE_QP_ID_PROJEKT + "] = ?, " +
                            "[" + SchemaKatalog.SPALTE_QP_BEZEICHNER + "] = ?, " +
                            "[" + SchemaKatalog.SPALTE_QP_BETRIEBSART + "] = ?, " +
                            "[" + SchemaKatalog.SPALTE_QP_EINHEIT + "] = ?, " +
                            "[" + SchemaKatalog.SPALTE_QP_BESCHREIBUNG + "] = ? WHERE ID = ?",
                            pKopf.ToArray());

                        v.Ausfuehren(
                            "DELETE FROM [" + SchemaKatalog.TAB_QUELLPROFILDATEN + "] " +
                            "WHERE ID_Quellprofil = ?",
                            StilleDb.Par("@id", DbParamTyp.Integer, id));
                    }
                    else
                    {
                        List<DbParam> pKopf = new List<DbParam>();
                        KopfParameter(pKopf, kopf);

                        // AUTOINCREMENT: Die vergebene ID liefert derselbe Aufruf zurueck -
                        // auf DERSELBEN Verbindung und in derselben Transaktion, sonst
                        // gehoerte sie einem fremden Vorgang. (Bis S4e: SELECT @@IDENTITY.)
                        id = v.EinfuegenUndId(
                            "INSERT INTO [" + SchemaKatalog.TAB_QUELLPROFIL + "] " +
                            "([" + SchemaKatalog.SPALTE_QP_ID_PROJEKT + "], " +
                            " [" + SchemaKatalog.SPALTE_QP_BEZEICHNER + "], " +
                            " [" + SchemaKatalog.SPALTE_QP_BETRIEBSART + "], " +
                            " [" + SchemaKatalog.SPALTE_QP_EINHEIT + "], " +
                            " [" + SchemaKatalog.SPALTE_QP_BESCHREIBUNG + "]) VALUES (?,?,?,?,?)",
                            pKopf.ToArray());

                        if (id <= 0)
                        {
                            v.Rollback();
                            Console.WriteLine("QuellprofilCtrl.Speichern: keine ID aus dem Einfügen.");
                            return 0;
                        }
                    }

                    string sqlDaten =
                        "INSERT INTO [" + SchemaKatalog.TAB_QUELLPROFILDATEN + "] " +
                        "([" + SchemaKatalog.SPALTE_QPD_ID_QUELLPROFIL + "], " +
                        " [" + SchemaKatalog.SPALTE_QPD_INDEX + "], " +
                        " [" + SchemaKatalog.SPALTE_QPD_WERT + "]) VALUES (?,?,?)";
                    for (int i = 0; i < werte.Length; i++)
                    {
                        v.Ausfuehren(sqlDaten,
                            new DbParam("@p", DbParamTyp.Integer) { Wert = id },
                            new DbParam("@i", DbParamTyp.Integer) { Wert = i },
                            new DbParam("@w", DbParamTyp.Double) { Wert = werte[i] });
                    }

                    v.Commit();
                    kopf.ID = id;
                    return id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("QuellprofilCtrl.Speichern fehlgeschlagen: " + ex.Message);
                return 0;
            }
        }

        private static void KopfParameter(List<DbParam> p, Kopf k)
        {
            p.Add(StilleDb.Par("@proj", DbParamTyp.Integer,
                k.ID_Projekt > 0 ? (object)k.ID_Projekt : null));
            p.Add(StilleDb.Par("@bez", DbParamTyp.VarWChar, k.Bezeichner ?? ""));
            p.Add(StilleDb.Par("@art", DbParamTyp.VarWChar, k.Betriebsart ?? ""));
            p.Add(StilleDb.Par("@einh", DbParamTyp.VarWChar, k.Einheit ?? ""));
            p.Add(StilleDb.Par("@besch", DbParamTyp.VarWChar, k.Beschreibung ?? ""));
        }

        /// <summary>
        /// Löscht ein Quellprofil samt Werten. Die Wertzeilen räumt die Beziehung
        /// <c>FK_QuellprofilDaten_Kopf</c> mit Löschweitergabe; das ausdrückliche DELETE
        /// davor ist der Rückfall für Datenbanken, auf denen die Beziehung nicht
        /// angelegt werden konnte (Schritt 54 legt sie WEICH an).
        ///
        /// <para>Zeigt noch eine Anlage über <c>WQ_ID_Quellprofil</c> auf das Profil,
        /// weist die restriktive Beziehung <c>FK_Anlage_Quellprofil</c> das Löschen ab —
        /// das ist so gewollt und wird als <c>false</c> gemeldet.</para>
        /// </summary>
        public static bool Loeschen(int idProfil)
        {
            if (idProfil <= 0) return false;

            StilleDb.NonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_QUELLPROFILDATEN + "] WHERE ID_Quellprofil = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idProfil));

            return StilleDb.NonQuery(
                "DELETE FROM [" + SchemaKatalog.TAB_QUELLPROFIL + "] WHERE ID = ?",
                StilleDb.Par("@id", DbParamTyp.Integer, idProfil)) > 0;
        }

        /// <summary>
        /// Die Anlagen, die ein Profil noch benutzen — für die Rückfrage vor dem
        /// Löschen. Nie <c>null</c>.
        /// </summary>
        public static List<string> NutzerDesProfils(int idProfil)
        {
            List<string> namen = new List<string>();
            if (idProfil <= 0) return namen;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Bezeichner FROM [" + SchemaKatalog.TAB_ENERGIEANLAGEN + "] " +
                "WHERE [" + SchemaKatalog.SPALTE_ANLAGE_WQ_ID_QUELLPROFIL + "] = ? ORDER BY Bezeichner",
                StilleDb.Par("@id", DbParamTyp.Integer, idProfil));
            if (dt == null) return namen;

            foreach (DataRow r in dt.Rows) namen.Add(StilleDb.Text(StilleDb.Feld(r, "Bezeichner")));
            return namen;
        }
    }
}
