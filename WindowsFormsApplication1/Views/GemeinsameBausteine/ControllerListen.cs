using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FUELL-METHODEN der Kern-Controller als Erweiterungsmethoden
    /// (Umsetzungskonzept iU4, Schritt 3).
    ///
    /// <para><b>Warum hier und nicht mehr als Partial.</b> Bis iU3 lagen sie als
    /// <c>*Ctrl.WinForms.cs</c> neben dem jeweiligen Controller: dieselbe Klasse, nur in
    /// einer Datei, die der Kern nicht verlinkte. Eine partielle Klasse laesst sich aber
    /// nicht ueber eine Assemblygrenze hinweg zusammensetzen - sobald die Controller in
    /// EPOS.Kern liegen, muss die Oberflaechenhaelfte woanders stehen. Eine
    /// Erweiterungsmethode leistet genau das, ohne dass sich an der AUFRUFSYNTAX etwas
    /// aendert: <c>ctrl.FillComboBox(box)</c> steht unveraendert in den Masken.</para>
    ///
    /// <para>Sie schreiben eine bereits gelesene Liste in ein Steuerelement - Anzeige,
    /// kein Rechnen. Die Rumpfe sind wortgleich uebernommen; statt des privaten
    /// <c>_internalList</c> lesen sie die oeffentliche Eigenschaft <c>items</c>, die
    /// genau dieselbe Liste liefert.</para>
    /// </summary>
    // internal, weil ProjektCtrl selbst internal ist - eine oeffentliche Methode
    // mit einem internen Parametertyp waere CS0051.
    internal static class ControllerListen
    {
        /// <summary>
        /// Fuellt eine ComboBox mit den Bezeichnern des BHKW-Katalogs
        /// (vormals <c>BHKWStammCtrl.WinForms.cs</c>).
        /// </summary>
        public static void FillComboBox(this BHKWStammCtrl c, ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in c.items)
            {
                ctrl.Items.Add(item.m_szBezeichner);
            }
        }

        /// <summary>
        /// Fuellt eine ComboBox mit den Projektnamen
        /// (vormals <c>ProjektCtrl.WinForms.cs</c>).
        /// </summary>
        public static void FillComboBox(this ProjektCtrl c, ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var p in c.items) ctrl.Items.Add(p.m_szProjektname);
        }
    }
}
