using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der OBERFLAECHENTEIL von <see cref="WPStammCtrl"/> (Umsetzungskonzept iU3).
    ///
    /// <para>Die Fuell-Methode schreibt eine gelesene Liste in ein WinForms-Steuerelement -
    /// Anzeige, kein Rechnen. Der Kern verlinkt diese Datei nicht.</para>
    /// </summary>
    partial class WPStammCtrl
    {
        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
                if (item != null) ctrl.Items.Add(item.WPName);
        }
    }
}
