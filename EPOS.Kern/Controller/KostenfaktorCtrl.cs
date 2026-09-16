using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

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
    /// <para><b>Lesen und Anlegen sind zeichengleich uebernommen</b>, samt der
    /// beiden Schutzfilter aus Befund B4 (11.08.2026): Gelesen und geloescht wird
    /// ausschliesslich, was <c>IsMainComponent = False</c> traegt — die
    /// Hauptkomponenten der Kostenrechnung sind damit gegen Loeschen gesichert.
    /// IDs entstehen per MAX+1 (kein AutoWert, ADR-001).</para>
    ///
    /// <para><b>Das LOESCHEN ist seit Auftrag #302 nicht mehr zeichengleich:</b> Es
    /// zaehlt erst, wer den Eintrag benutzt, und lehnt benannt ab. Der Grund und die
    /// zweite Schicht (Schemaschritt 81) stehen bei
    /// <see cref="Loeschen(int, out string)"/>.</para>
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
        /// Loescht einen Kostenfaktor — MIT VERWENDUNGSSCHUTZ. Liefert <c>false</c>,
        /// wenn nichts geloescht wurde; <paramref name="grund"/> nennt dann, was den
        /// Eintrag haelt (leer, wenn es nichts zu benennen gibt).
        /// </summary>
        /// <remarks>
        /// <para><b>Der Befund (Auftrag #302).</b> Bis hierher stand hier nur der
        /// <c>DELETE</c>. Der Fremdschluessel
        /// <c>Tab_ProjektWerte.StammID → Tab_Kostenfaktor(StammID)</c> trug dabei
        /// <c>ON DELETE CASCADE</c>, und <c>PRAGMA foreign_keys = ON</c> steht je
        /// Verbindung (<c>SqliteDatenzugriff</c>): EIN Katalogeintrag zu loeschen riss
        /// JEDE Projektposition derselben <c>StammID</c> mit — in ALLEN Projekten und
        /// ALLEN Gewerken. Die Rueckfrage des Dialogs nannte davon nichts. Die Kaskade
        /// war nie gewollt; <c>Tab_KostenVorlagePosition.StammID</c> traegt
        /// bezeichnenderweise gar keinen Fremdschluessel.</para>
        ///
        /// <para><b>Zwei Schichten, und das ist Absicht.</b> Hier steht die erste: Es
        /// wird GEZAEHLT, bevor geloescht wird, und der Anwender bekommt eine Zahl statt
        /// eines stillen Verlusts. Die zweite ist die Datenbank selbst — Schemaschritt 81
        /// (<see cref="ProjektWerteLoeschschutz"/>) stellt den Fremdschluessel auf
        /// <c>ON DELETE RESTRICT</c>. Auch wer an diesem Controller vorbeigeht, reisst
        /// danach keine Projektposition mehr mit.</para>
        ///
        /// <para><b>Muster:</b> <c>EnergietraegerKatalogCtrl.Loeschen(int, out string)</c>
        /// — derselbe Aufbau, dieselbe Zusage „benannt abgelehnt, nie still
        /// uebergangen".</para>
        ///
        /// <para><b>Der Schutzfilter <c>IsMainComponent = False</c></b> ist wortgleich aus
        /// <c>btnDeleteKostenfaktor_Click</c> uebernommen (Befund B4) und bleibt. NEU ist,
        /// dass eine Hauptkomponente auch <c>false</c> MELDET:
        /// <c>DataRepository.ExecuteSQL</c> sagt „die Anweisung lief", nicht „eine Zeile
        /// wurde getroffen" — ein <c>DELETE</c>, den der Filter leer laufen liess, kam
        /// bisher als <c>true</c> zurueck. Deshalb die Probe VOR dem Loeschen.</para>
        ///
        /// <para><b>Eine Abweichung zum Vorlaeufer:</b> Geloescht wird ueber die
        /// <c>StammID</c>, nicht mehr ueber die <c>Bezeichnung</c>. Die <c>ListView</c>
        /// des Vorlaeufers fuehrte nur den Text und konnte deshalb gar nicht anders; bei
        /// zwei gleichnamigen Saetzen traf der Loeschbefehl beide. Der Satz, den der
        /// Anwender markiert hat, ist der Satz, der verschwindet.</para>
        /// </remarks>
        public static bool Loeschen(int stammId, out string grund)
        {
            grund = "";
            if (stammId <= 0) return false;

            // Erst zaehlen. Projektpositionen UND Vorlagenpositionen halten den Eintrag -
            // die einen, weil die Kaskade sie mitrisse, die anderen, weil eine Vorlage
            // ohne ihren Kostenfaktor eine Position ohne Namen erzeugte.
            int positionen = Zaehle(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE StammID = ?", stammId);
            int projekte = Zaehle(
                "SELECT COUNT(DISTINCT ProjektID) FROM Tab_ProjektWerte WHERE StammID = ?", stammId);
            int vorlagen = Zaehle(
                "SELECT COUNT(*) FROM Tab_KostenVorlagePosition WHERE StammID = ?", stammId);

            if (positionen > 0 || vorlagen > 0)
            {
                grund = string.Format(
                    CultureInfo.CurrentCulture,
                    MyResource.Resource.KFAK_MSG_IN_BENUTZUNG,
                    positionen, projekte, vorlagen);
                return false;
            }

            // Die Probe auf den loeschbaren Satz: Eine Hauptkomponente und ein gar nicht
            // vorhandener Satz melden ab hier beide false statt eines leer gelaufenen
            // DELETE, der als true zurueckkaeme.
            if (Zaehle("SELECT COUNT(*) FROM Tab_Kostenfaktor " +
                       "WHERE StammID = ? AND IsMainComponent = False", stammId) != 1)
                return false;

            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Kostenfaktor WHERE StammID = ? AND IsMainComponent = False",
                new DbParam("@sid", stammId));
        }

        /// <summary>
        /// Eine Zaehlung ueber die <c>StammID</c>. Eine nicht lesbare Zaehlung gilt als
        /// 0 — wortgleich <c>EnergietraegerKatalogCtrl.Zaehle</c>.
        /// </summary>
        private static int Zaehle(string sql, int stammId)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new DbParam("@sid", stammId));
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }
    }
}
