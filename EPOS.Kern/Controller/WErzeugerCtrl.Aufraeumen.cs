using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der PROJEKT-LOESCHWEG von <see cref="WErzeugerCtrl"/> - abgetrennt, weil er als
    /// einziger Teil der Klasse den Aufraeumlauf <see cref="GeraeteWaisen"/> ruft.
    ///
    /// <para>Beide liegen im Kern, der Aufruf geht unmittelbar. Damit raeumt der
    /// Loeschweg auf JEDER Plattform auf - in der Windows-Anwendung wie in der
    /// iOS-App.</para>
    /// </summary>
    partial class WErzeugerCtrl
    {
        /// <summary>
        /// Entfernt ALLE Anlagenzeilen eines Projekts - und die Gerätezeilen, auf die
        /// danach nichts mehr zeigt.
        ///
        /// <para>
        /// DIESE METHODE IST DER PROJEKT-LÖSCHWEG, nicht der Speicherweg. Ihre beiden
        /// Aufrufer sind <c>MenueCtrl.ProjektDelete</c> und
        /// <c>VariantenCtrl.LoescheVariante</c>; gespeichert wird über
        /// <c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> +
        /// <c>WizardCtrl.Add_WP_Waermeerzeuger</c>. Weil hier alle Anlagenzeilen
        /// fallen, ist danach JEDE Gerätezeile des Projekts verwaist.
        /// </para>
        ///
        /// <para>
        /// WARUM DAS NÖTIG IST. Von den sieben Gerätetabellen hängt nur
        /// <c>Tab_Pufferspeicher</c> mit Löschweitergabe an <c>Tab_Projekt</c>. Die
        /// übrigen sechs behalten ihre Zeilen: Ohne den Aufräumlauf stünden nach jedem
        /// gelöschten Projekt Gerätezeilen zu einer Projekt-ID da, die es in
        /// <c>Tab_Projekt</c> nicht mehr gibt - über keine Oberfläche erreichbar und mit
        /// jedem weiteren Löschen wachsend.
        /// </para>
        ///
        /// <para>
        /// DER AUFRÄUMLAUF DARF DAS LÖSCHEN NICHT SCHEITERN LASSEN. Er läuft NACH dem
        /// erfolgreichen DELETE und sein Ergebnis geht nicht in den Rückgabewert ein.
        /// Verschluckt wird es deshalb trotzdem nicht: Einen nachholenden Lauf gibt es
        /// nicht, also MELDET diese Methode jeden unvollständigen Aufräumlauf mit
        /// Projekt-Id und Grund - sonst bliebe der Rückstand unbemerkt stehen.
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

                // Unmittelbar: GeraeteWaisen liegt im Kern und braucht keine Oberflaeche.
                // Der Bericht geht NICHT in den Rueckgabewert ein (siehe oben), ein
                // unvollstaendiger Lauf wird aber gemeldet statt verschluckt.
                GeraeteWaisen.Bericht bericht = GeraeteWaisen.Aufraeumen(ID_Projekt);
                if (bericht != null && bericht.Unvollstaendig)
                    Console.WriteLine("WARNUNG: Projekt " + ID_Projekt + ": Der Aufraeumlauf der " +
                                      "verwaisten Geraetezeilen blieb unvollstaendig - " +
                                      string.Join(" | ", bericht.Notizen));
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
