using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der OBERFLAECHENTEIL von <see cref="BHKWCtrl"/> (Umsetzungskonzept iU3, Schritt 2).
    ///
    /// <para>Die Fuell-Methoden schreiben eine gelesene Liste in ein WinForms-Steuerelement.
    /// Das ist Anzeige, kein Rechnen - und es war die einzige Stelle, an der
    /// <c>BHKWCtrl</c> <c>System.Windows.Forms</c> brauchte. Der Kern verlinkt diese
    /// Datei nicht; unter Windows aendert sich nichts, weil beide Teile dieselbe Klasse
    /// bilden.</para>
    /// </summary>
    partial class BHKWCtrl
    {
        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
            {
                ctrl.Items.Add(item.m_szBezeichner);
            }
        }
    }
}
