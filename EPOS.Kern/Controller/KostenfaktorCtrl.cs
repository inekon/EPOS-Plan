using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Datenbankseite des Katalogs der Kostenfaktoren
    /// (<c>Tab_Kostenfaktor</c>) — iU9-W1.5.
    ///
    /// <para><b>Wozu.</b> Der Vorlaeufer <c>Views\Kosten\Form_KostenAdmin</c>
    /// sprach selbst mit der Datenbank: <c>LoadKostenfaktoren</c> mit einem
    /// <c>SELECT</c>, <c>btnNeuKostenfaktor_Click</c> mit <c>GetMaxID</c> und
    /// einem <c>INSERT</c>, <c>btnDeleteKostenfaktor_Click</c> mit einem
    /// <c>DELETE</c>. Eine Maske, die das tut, laesst sich weder ohne Datenbank
    /// pruefen noch auf iOS wiederverwenden. Die drei Anweisungen stehen deshalb
    /// hier; die Komponente
    /// <c>EPOS.UI\Dialoge\Kosten\KostenfaktorKatalogDialog.razor</c> bekommt die
    /// Liste fertig herein.</para>
    ///
    /// <para><b>Die Anweisungen sind zeichengleich uebernommen</b>, samt der
    /// beiden Schutzfilter aus Befund B4 (11.08.2026): Gelesen und geloescht wird
    /// ausschliesslich, was <c>IsMainComponent = False</c> traegt — die
    /// Hauptkomponenten der Kostenrechnung sind damit gegen Loeschen gesichert.
    /// IDs entstehen per MAX+1 (kein AutoWert, ADR-001).</para>
    /// </summary>
    public static class KostenfaktorCtrl
    {
        /// <summary>Ein Eintrag des Katalogs.</summary>
        /// <param name="StammId"><c>Tab_Kostenfaktor.StammID</c>.</param>
        /// <param name="Bezeichnung">Der angezeigte Name.</param>
        public sealed record Eintrag(int StammId, string Bezeichnung);

        /// <summary>
        /// Alle pflegbaren Kostenfaktoren, nach Bezeichnung sortiert.
        /// </summary>
        /// <remarks>
        /// Wortgleich <c>Form_KostenAdmin.LoadKostenfaktoren</c> — dieselbe
        /// Abfrage, derselbe Filter, dieselbe Sortierung. Die <c>ListView</c> zeigte
        /// nur die Bezeichnung; die <c>StammID</c> kommt jetzt mit, damit die
        /// Loeschung den Satz meint und nicht seinen Namen.
        /// </remarks>
        public static IReadOnlyList<Eintrag> Alle()
        {
            var liste = new List<Eintrag>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT StammID, Bezeichnung FROM Tab_Kostenfaktor " +
                "WHERE IsMainComponent = False ORDER BY Bezeichnung");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                if (row["StammID"] == null || row["StammID"] == DBNull.Value) continue;
                liste.Add(new Eintrag(
                    Convert.ToInt32(row["StammID"]),
                    row["Bezeichnung"] == DBNull.Value ? "" : row["Bezeichnung"].ToString()));
            }
            return liste;
        }

        /// <summary>
        /// Legt einen Kostenfaktor an. Liefert die vergebene <c>StammID</c>, oder 0,
        /// wenn nichts geschrieben wurde (leerer Name oder Fehler).
        /// </summary>
        /// <remarks>
        /// Wortgleich <c>btnNeuKostenfaktor_Click</c>: leerer Name wird still
        /// uebergangen (Z. 57), die Id entsteht als <c>MAX + 1</c>, und
        /// <c>IsMainComponent</c> ist immer <c>False</c> — ueber diese Maske
        /// entsteht keine Hauptkomponente.
        /// </remarks>
        public static int Neu(string bezeichnung)
        {
            string name = (bezeichnung ?? "").Trim();
            if (name.Length == 0) return 0;

            int stammId = DataRepository.GetMaxID("Tab_Kostenfaktor", "StammID") + 1;
            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) VALUES (?, ?, ?)",
                new DbParam("@sid", stammId),
                new DbParam("@bez", name),
                new DbParam("@main", DbParamTyp.Boolean) { Wert = false });
            return ok ? stammId : 0;
        }

        /// <summary>
        /// Loescht einen Kostenfaktor. Liefert <c>false</c>, wenn nichts geloescht
        /// wurde — insbesondere bei einer Hauptkomponente.
        /// </summary>
        /// <remarks>
        /// <para>Der Schutzfilter <c>IsMainComponent = False</c> ist wortgleich aus
        /// <c>btnDeleteKostenfaktor_Click</c> uebernommen (Befund B4).</para>
        /// <para><b>Eine Abweichung:</b> Geloescht wird ueber die <c>StammID</c>,
        /// nicht mehr ueber die <c>Bezeichnung</c>. Die <c>ListView</c> des
        /// Vorlaeufers fuehrte nur den Text und konnte deshalb gar nicht anders;
        /// bei zwei gleichnamigen Saetzen traf der Loeschbefehl beide. Der Satz,
        /// den der Anwender markiert hat, ist der Satz, der verschwindet.</para>
        /// </remarks>
        public static bool Loeschen(int stammId)
        {
            if (stammId <= 0) return false;
            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Kostenfaktor WHERE StammID = ? AND IsMainComponent = False",
                new DbParam("@sid", stammId));
        }
    }
}
