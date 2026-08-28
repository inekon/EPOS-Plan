using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kleiner modaler Eingabedialog (Titel, Beschriftung, Vorgabewert) — der
    /// gemeinsame Baustein der programmatischen Simulations-Dialoge.
    ///
    /// <para>Er stand bis Paket Q1 als private Methode <c>EingabeDialog</c> in
    /// <c>Form_Simulation_Config.Uebersicht.cs</c>. Mit dem Quellprofil-Dialog braucht
    /// ihn ein zweiter Aufrufer; herausgezogen statt kopiert, damit die beiden Fassungen
    /// nicht auseinanderlaufen können — dieselbe Begründung, mit der Etappe D5b
    /// <c>WaermequelleClass.QuelleAnzeige</c> aus demselben Formular herausgelöst hat.
    /// <c>Form_Simulation_Config.EingabeDialog</c> reicht seither nur noch durch.</para>
    /// </summary>
    internal static class Eingabefrage
    {
        /// <summary>
        /// Fragt einen Text ab; liefert die Eingabe oder <c>null</c> bei Abbruch.
        /// </summary>
        public static string Fragen(IWin32Window besitzer, string titel,
                                    string beschriftung, string vorgabe)
        {
            using Form frm = new Form();
            frm.Text = titel;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(340, 140);

            Label lbl = new Label { Text = beschriftung, AutoSize = true, Location = new Point(12, 12) };
            TextBox txt = new TextBox { Location = new Point(12, 75), Width = 316, Text = vorgabe ?? "" };
            Button ok = new Button { Text = MyResource.Resource.SIM_BTN_OK, DialogResult = DialogResult.OK, Location = new Point(172, 105), Width = 75 };
            Button abbruch = new Button { Text = MyResource.Resource.SIM_BTN_ABBRECHEN, DialogResult = DialogResult.Cancel, Location = new Point(253, 105), Width = 75 };

            frm.Controls.Add(lbl);
            frm.Controls.Add(txt);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            return frm.ShowDialog(besitzer) == DialogResult.OK ? txt.Text : null;
        }
    }
}
