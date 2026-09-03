using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Übernahme EINES Merkmals aus einer anderen Version desselben Stammprojekts
    /// (Seite „Übersicht" des Reiters „Berichte &amp; Kosten", Stufe-3-Zeilen der
    /// Unterschiedsanzeige).
    ///
    /// <para>
    /// DIE ZEILENZUORDNUNG IST DIE DES <see cref="AbweichungsErmittler"/> — nicht eine
    /// zweite. Geschrieben wird ausschließlich in GENAU DIE ZEILE, aus der die Anzeige
    /// ihren Zielwert gelesen hat: <see cref="AbweichungsErmittler.ZeileFuer"/> liefert
    /// sie, <see cref="ProjektDetails"/> hat sie mit <c>SELECT *</c> geladen, ihre
    /// <c>ID</c> steht also darin. Das UPDATE adressiert diese ID (zusätzlich gegen
    /// <c>ID_Projekt</c> abgesichert) und genau eine Spalte — ein Rundumschlag über den
    /// ganzen Datensatz käme hier nicht in Frage.
    /// </para>
    ///
    /// <para>
    /// WO DIE ZUORDNUNG NICHT TRÄGT, WIRD SIE ABGELEHNT statt geraten: Der Ermittler
    /// vergleicht je Gewerk die ERSTE Zeile nach <c>ID</c>. Führt Quelle oder Ziel im
    /// Gewerk mehrere Komponenten, ist diese erste Zeile keine belastbare Auswahl — dann
    /// meldet <see cref="Pruefe"/> das und verweist auf die Komponenten-Übernahme
    /// (<see cref="KomponentenUebernahmeCtrl"/>). Für <c>Tab_Energieanlagen</c> und
    /// <c>Tab_Gebaeude</c> ist „erste Zeile" dagegen die ausdrücklich erklärte Konvention
    /// der Feldliste (Anlagen-Anker bzw. „erstes Gebäude"); dort wird die betroffene Zeile
    /// im Dialog benannt statt die Übernahme zu verweigern.
    /// </para>
    ///
    /// <para>
    /// SCHLÜSSELSPALTEN SIND AUSGESCHLOSSEN. <c>Bezeichner</c> ist in allen
    /// Gerätetabellen der Name, über den Projektkopie, Katalogauflösung und die
    /// Paarung selbst laufen (<c>CopyFromStamm</c> sucht darüber). Ein feldweises
    /// Überschreiben würde die Zuordnung verändern, aus der es hervorgegangen ist —
    /// für einen Komponentenwechsel ist die Komponenten-Übernahme zuständig.
    /// </para>
    /// </summary>
    public static class MerkmalUebernahmeCtrl
    {
        /// <summary>Name der Schlüsselspalte, die von der Feld-Übernahme ausgenommen ist.</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        private const string TAB_ANLAGEN = "Tab_Energieanlagen";
        private const string TAB_GEBAEUDE = "Tab_Gebaeude";

        /// <summary>
        /// Die eine Datenzeile eines Projekts, aus der ein Merkmal gelesen bzw. in die es
        /// geschrieben wird.
        /// </summary>
        public class Zeilenbezug
        {
            /// <summary>Führt das Projekt zu diesem Merkmal überhaupt eine Zeile?</summary>
            public bool Vorhanden;

            /// <summary>Primärschlüssel der Zeile (0 = unbekannt → nicht beschreibbar).</summary>
            public int Id;

            /// <summary>Bezeichner der Zeile — benennt die betroffene Komponente im Dialog.</summary>
            public string Bezeichner = "";

            /// <summary>Zahl der Zeilen, aus denen die erste gewählt wurde (1 = eindeutig).</summary>
            public int Anzahl;

            /// <summary>Formatierter Anzeigewert (wie in der Unterschiedsliste).</summary>
            public string Anzeigewert = "—";

            /// <summary>Die geladene Zeile selbst (Rohwerte für das UPDATE).</summary>
            public DataRow Zeile;
        }

        /// <summary>Ergebnis der Vorprüfung — Grundlage des Bestätigungsdialogs.</summary>
        public class Befund
        {
            public bool Moeglich;
            public string Grund = "";
            public Zeilenbezug Quelle = new Zeilenbezug();
            public Zeilenbezug Ziel = new Zeilenbezug();

            /// <summary>Quelle und Ziel führen bereits denselben Wert.</summary>
            public bool Gleichstand;
        }

        /// <summary>
        /// Ist die Spalte der Schlüssel der Komponentenzuordnung und damit von der
        /// feldweisen Übernahme ausgenommen?
        /// </summary>
        public static bool IstSchluesselspalte(string spalte)
        {
            return string.Equals(spalte, SPALTE_BEZEICHNER, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Prüft, ob das Merkmal von <paramref name="idQuelle"/> nach
        /// <paramref name="idZiel"/> übernommen werden kann, und liefert die Werte
        /// beider Seiten für den Dialog. Schreibt nichts.
        /// </summary>
        public static Befund Pruefe(int idQuelle, int idZiel, AbweichungsErmittler.Merkmal f)
        {
            var b = new Befund();
            if (f == null || string.IsNullOrEmpty(f.Tabelle) || string.IsNullOrEmpty(f.Spalte))
            { b.Grund = MyResource.Resource.BK_MSG_UEB_KEIN_FELD; return b; }

            if (IstSchluesselspalte(f.Spalte))
            { b.Grund = MyResource.Resource.BK_MSG_UEB_SCHLUESSEL; return b; }

            if (idQuelle <= 0 || idZiel <= 0 || idQuelle == idZiel)
            { b.Grund = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE; return b; }

            b.Quelle = Bezug(ProjektDetails.Lade(idQuelle), f);
            b.Ziel = Bezug(ProjektDetails.Lade(idZiel), f);

            if (!b.Quelle.Vorhanden) { b.Grund = MyResource.Resource.BK_MSG_UEB_KEINE_QUELLZEILE; return b; }
            if (!b.Ziel.Vorhanden) { b.Grund = MyResource.Resource.BK_MSG_UEB_KEINE_ZIELZEILE; return b; }
            if (b.Ziel.Id <= 0) { b.Grund = MyResource.Resource.BK_MSG_UEB_KEINE_ZIELZEILE; return b; }

            // „Erste Zeile nach ID" trägt nur, solange es genau eine gibt. Bei den
            // Gerätetabellen der Gewerke ist die Zahl selbst ein gemeldeter Unterschied —
            // dort wird nicht geraten (Anlage/Gebäude: erklärte Konvention, siehe Klassenkopf).
            if (IstGewerkTabelle(f.Tabelle))
            {
                if (b.Quelle.Anzahl > 1 || b.Ziel.Anzahl > 1)
                {
                    b.Grund = string.Format(MyResource.Resource.BK_MSG_UEB_NICHT_EINDEUTIG,
                                            f.Gewerk, b.Quelle.Anzahl, b.Ziel.Anzahl);
                    return b;
                }
            }

            b.Gleichstand = string.Equals(b.Quelle.Anzeigewert, b.Ziel.Anzeigewert, StringComparison.Ordinal);
            b.Moeglich = true;
            return b;
        }

        /// <summary>
        /// Schreibt den Quellwert in die Zielzeile — EIN zielgenaues UPDATE genau dieser
        /// einen Spalte. Der Wert wird als Rohwert der Quellzeile übergeben; Quelle und
        /// Ziel sind dieselbe Tabelle, der Spaltentyp ist damit identisch.
        /// Setzt anschließend das Änderungsdatum des Zielprojekts, damit der vorhandene
        /// Simulationsstand-Mechanismus die Ergebnisse als veraltet ausweist.
        /// </summary>
        public static bool Schreibe(Befund b, int idZiel, AbweichungsErmittler.Merkmal f, out string fehler)
        {
            fehler = null;
            if (b == null || !b.Moeglich) { fehler = b != null ? b.Grund : ""; return false; }

            try
            {
                string sql = "UPDATE [" + f.Tabelle + "] SET [" + f.Spalte + "] = ? " +
                             "WHERE ID = ? AND ID_Projekt = ?";

                int betroffen = DataRepository.ExecuteNonQuery(sql,
                    WertParameter("@wert", b.Quelle.Zeile, f.Spalte),
                    new DbParam("@id", b.Ziel.Id),
                    new DbParam("@proj", idZiel));

                if (betroffen < 0) { fehler = MyResource.Resource.BK_MSG_UEB_SCHREIBFEHLER; return false; }
                if (betroffen == 0) { fehler = MyResource.Resource.BK_MSG_UEB_KEINE_ZIELZEILE; return false; }

                MarkiereProjektGeaendert(idZiel);
                return true;
            }
            catch (Exception ex) { fehler = ex.Message; return false; }
        }

        /// <summary>
        /// Setzt <c>Tab_Projekt.Aenderungsdatum</c> auf jetzt. Damit meldet
        /// <see cref="BerichtsDatenSammler.ErmittleStatus"/> den Simulationsstand des
        /// Projekts als veraltet (⚠) — es gibt bewusst keinen zweiten Veraltet-Merker.
        /// </summary>
        public static void MarkiereProjektGeaendert(int idProjekt)
        {
            try
            {
                DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                    new DbParam("@d", DbParamTyp.Date) { Wert = DateTime.Now },
                    new DbParam("@id", idProjekt));
            }
            catch { /* der Hinweis an den Anwender hängt nicht daran */ }
        }

        /// <summary>Führt das Projekt bereits ein gespeichertes Simulationsergebnis?</summary>
        public static bool HatErgebnisse(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + ErgebnisCtrl.TAB_KOPF + " WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------ intern

        private static bool IstGewerkTabelle(string tabelle)
        {
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                if (string.Equals(g.Value, tabelle, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Die Zeile eines Projekts zu einem Merkmal — über DIESELBE Auswahl wie der
        // Ermittler, ergänzt um ID, Bezeichner und Zeilenzahl für Dialog und UPDATE.
        private static Zeilenbezug Bezug(ProjektDetails d, AbweichungsErmittler.Merkmal f)
        {
            var z = new Zeilenbezug();
            DataRow r = AbweichungsErmittler.ZeileFuer(d, f);
            if (r == null) return z;

            z.Vorhanden = true;
            z.Zeile = r;
            z.Id = (int)(ProjektDetails.D(r, "ID") ?? 0);
            z.Bezeichner = ProjektDetails.S(r, SPALTE_BEZEICHNER).Trim();
            if (z.Bezeichner.Length == 0) z.Bezeichner = ProjektDetails.S(r, "Gebaeudename").Trim();
            z.Anzahl = Zeilenzahl(d, f);
            z.Anzeigewert = r.Table.Columns.Contains(f.Spalte)
                ? AbweichungsErmittler.Formatiere(r, f) : "—";
            return z;
        }

        private static int Zeilenzahl(ProjektDetails d, AbweichungsErmittler.Merkmal f)
        {
            if (string.Equals(f.Tabelle, TAB_ANLAGEN, StringComparison.OrdinalIgnoreCase))
                return d.Anlagen != null ? d.Anlagen.Rows.Count : 0;
            if (string.Equals(f.Tabelle, TAB_GEBAEUDE, StringComparison.OrdinalIgnoreCase))
                return d.Gebaeude != null ? d.Gebaeude.Rows.Count : 0;
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                if (string.Equals(g.Value, f.Tabelle, StringComparison.OrdinalIgnoreCase))
                    return d.KomponentenAnzahl.ContainsKey(g.Key) ? d.KomponentenAnzahl[g.Key] : 0;
            return 0;
        }

        /// <summary>
        /// Parameter für den Zielwert. Der Typ kommt aus der SPALTE der Quellzeile —
        /// Quelle und Ziel sind dieselbe Tabelle, damit ist es auch der Zieltyp. Aus
        /// <see cref="DBNull"/> allein leitet der Provider keinen Typ ab (dieselbe
        /// Begründung wie bei <c>WizardCtrl.AnlagenParameter</c>).
        /// </summary>
        private static DbParam WertParameter(string name, DataRow quelle, string spalte)
        {
            DbParamTyp typ = DbParamTyp.Variant;
            object wert = DBNull.Value;

            if (quelle != null && quelle.Table.Columns.Contains(spalte))
            {
                Type t = quelle.Table.Columns[spalte].DataType;
                if (t == typeof(string)) typ = DbParamTyp.VarWChar;
                else if (t == typeof(bool)) typ = DbParamTyp.Boolean;
                else if (t == typeof(byte) || t == typeof(short) || t == typeof(int) || t == typeof(long))
                    typ = DbParamTyp.Integer;
                else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                    typ = DbParamTyp.Double;
                else if (t == typeof(DateTime)) typ = DbParamTyp.Date;

                if (quelle[spalte] != DBNull.Value) wert = quelle[spalte];
            }

            return new DbParam(name, typ) { Wert = wert };
        }
    }
}
