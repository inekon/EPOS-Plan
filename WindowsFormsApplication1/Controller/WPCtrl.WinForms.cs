using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der OBERFLAECHENTEIL von <see cref="WPCtrl"/> (Umsetzungskonzept iU3, Schritt 2).
    ///
    /// <para>Die Fuell-Methode schreibt eine gelesene Liste in ein WinForms-Steuerelement.
    /// Das ist Anzeige, kein Rechnen - und es war die einzige Stelle, an der
    /// <c>WPCtrl</c> die Oberflaeche brauchte. Der Kern verlinkt diese Datei nicht.</para>
    /// </summary>
    partial class WPCtrl
    {
        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
            {
                if (item != null)
                {
                    ctrl.Items.Add(item.WPName);
                }
            }
        }
    }
}
