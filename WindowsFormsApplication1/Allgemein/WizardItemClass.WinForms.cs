using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der OBERFLAECHENTEIL von <see cref="WizardItemClass"/> (Umsetzungskonzept iU3,
    /// Schritt 2).
    ///
    /// <para>Die Klasse ist im Uebrigen ein reiner Typ- und Nummernkatalog, den der
    /// Rechenkern an vielen Stellen braucht (<c>WizardItemClass.KESSEL_TYP</c> &amp; Co.).
    /// Nur das Feld <c>wizardform</c> und der Konstruktor, der es setzt, gehoeren zum
    /// Assistenten - sie stehen deshalb hier und werden vom Kern nicht verlinkt.</para>
    /// </summary>
    public partial class WizardItemClass
    {
        /// <summary>Das Formular, das dieser Assistentenschritt zeigt.</summary>
        public Form wizardform;

        public WizardItemClass(Form frm, int type)
        {
            wizardform = frm;
            formtype = type;
            aktiv = false;
        }
    }
}
