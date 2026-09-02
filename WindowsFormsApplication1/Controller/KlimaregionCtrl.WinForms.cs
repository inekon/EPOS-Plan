using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der OBERFLAECHENTEIL von <see cref="KlimaregionCtrl"/> (Umsetzungskonzept iU3, Schritt 2).
    ///
    /// <para>Die Fuell-Methoden schreiben eine gelesene Liste in ein WinForms-Steuerelement.
    /// Das ist Anzeige, kein Rechnen - und es war die einzige Stelle, an der
    /// <c>KlimaregionCtrl</c> <c>System.Windows.Forms</c> brauchte. Der Kern verlinkt diese
    /// Datei nicht; unter Windows aendert sich nichts, weil beide Teile dieselbe Klasse
    /// bilden.</para>
    /// </summary>
    partial class KlimaregionCtrl
    {
        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }
    }
}
