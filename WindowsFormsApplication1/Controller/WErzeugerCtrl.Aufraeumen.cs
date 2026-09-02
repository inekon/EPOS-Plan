using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der PROJEKT-LOESCHWEG von <see cref="WErzeugerCtrl"/> - abgetrennt, weil er als
    /// einziger Teil der Klasse den Aufraeumlauf <see cref="GeraeteWaisen"/> braucht und
    /// der wiederum die Oberflaeche (Umsetzungskonzept iU3, Kante K6).
    ///
    /// <para><b>Der Kern verlinkt diese Datei NICHT.</b> Ein Referenzlauf loescht keine
    /// Projekte; er liest sie und rechnet. Alles Uebrige von <see cref="WErzeugerCtrl"/>
    /// - Lesen, Aendern, Einfuegen - bleibt in <c>WErzeugerCtrl.cs</c> und damit im
    /// Kern.</para>
    /// </summary>
    partial class WErzeugerCtrl
    {
        /// <summary>
        /// Entfernt ALLE Anlagenzeilen eines Projekts - und seit dem 22.08.2026 auch die
        /// Gerätezeilen, auf die danach nichts mehr zeigt.
        ///
        /// <para>
        /// DIESE METHODE IST DER PROJEKT-LÖSCHWEG, nicht der Speicherweg. Ihre beiden
        /// Aufrufer sind <c>MenueCtrl.ProjektDelete</c> und
        /// <c>VariantenCtrl.LoescheVariante</c>; gespeichert wird über
        /// <see cref="WizardCtrl.Del_Projekt_Waermeerzeuger"/> +
        /// <see cref="WizardCtrl.Add_WP_Waermeerzeuger"/>. Weil hier alle Anlagenzeilen
        /// fallen, ist danach JEDE Gerätezeile des Projekts verwaist.
        /// </para>
        ///
        /// <para>
        /// WARUM DAS NÖTIG IST. Von den sieben Gerätetabellen hängt nur
        /// <c>Tab_Pufferspeicher</c> mit Löschweitergabe an <c>Tab_Projekt</c>. Die
        /// übrigen sechs behielten ihre Zeilen: Auf der Arbeitskopie standen am
        /// 22.08.2026 Gerätezeilen zu sieben Projekt-IDs, die es in <c>Tab_Projekt</c>
        /// längst nicht mehr gibt. Sie waren über keine Oberfläche mehr erreichbar und
        /// wuchsen mit jedem gelöschten Projekt weiter.
        /// </para>
        ///
        /// <para>
        /// DER AUFRÄUMLAUF DARF DAS LÖSCHEN NICHT SCHEITERN LASSEN. Er läuft NACH dem
        /// erfolgreichen DELETE und sein Ergebnis geht nicht in den Rückgabewert ein:
        /// Was er nicht wegräumt, ist Altbestand wie bisher - der Migrationsschritt holt
        /// ihn beim nächsten Programmstart nach.
        /// </para>
        /// </summary>
        public bool Delete()
        {
            try
            {
                // Korrektur: DELETE * FROM bzw. DELETE FROM statt der alten fehlerhaften Syntax "DELETE ID_Projekt FROM..."
                string sql = "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?";
                DbParam[] ps = { new DbParam("@idProj", ID_Projekt) };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;

                GeraeteWaisen.Aufraeumen(ID_Projekt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }
    }
}
