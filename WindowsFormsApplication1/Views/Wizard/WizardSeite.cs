using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Seite des Projektassistenten - Formular plus Nummer plus Schaltzustand
    /// (Umsetzungskonzept iU4, Schritt 3).
    ///
    /// <para><b>Warum eine eigene Klasse und kein Partial mehr.</b> Bis iU3 war
    /// <see cref="WizardItemClass"/> partial: Der Typ- und Nummernkatalog stand in
    /// <c>WizardItemClass.cs</c> (vom Kern verlinkt), das Feld <c>wizardform</c> mit
    /// seinem Konstruktor in <c>WizardItemClass.WinForms.cs</c> (nicht verlinkt). Eine
    /// partielle Klasse kann aber nicht ueber eine Assemblygrenze hinweg zusammengesetzt
    /// werden - sobald der Katalog in EPOS.Kern liegt, muss die Oberflaechenhaelfte ein
    /// eigener Typ sein. Ableiten statt Ergaenzen: <c>WizardSeite</c> IST eine
    /// <see cref="WizardItemClass"/>, alle Nummernkonstanten bleiben unter dem
    /// gewohnten Namen erreichbar, und <c>formtype</c>/<c>aktiv</c> sind geerbt.</para>
    /// </summary>
    public sealed class WizardSeite : WizardItemClass
    {
        /// <summary>Das Formular, das dieser Assistentenschritt zeigt.</summary>
        public Form wizardform;

        public WizardSeite(Form frm, int type)
        {
            wizardform = frm;
            formtype = type;
            aktiv = false;
        }
    }
}
