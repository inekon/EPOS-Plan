using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class WizardItemClass
    {
        public const int KOMPONENTEN_ITEM = 0;
        public const int PROJEKT_ITEM = 1;
        public const int KLIMA_ITEM = 2;
        public const int GEBAEUDE_ITEM = 3;
        public const int WP_ITEM = 4;
        public const int SOLAR_ITEM = 5;
        public const int PV_ITEM = 6;
        public const int SP_ITEM = 7;
        public const int REFERENZ_ITEM = 8;
        public const int PROZESS_ITEM = 9;
        public const int WAERMELASTGANG_ITEM = 10;

        public const int WP_TYP = 1;
        public const int SOLAR_TYP = 2;
        public const int PV_TYP = 3;
        public const int SP_TYP = 4;
        public const int REF_KESSEL_TYP = 5;
        public const int REF_SP_TYP = 6;
        public const int REF_WP_TYP = 7;
        public const int REF_SOLAR_TYP = 8;
        public const int REF_PV_TYP = 9;
        
        public Form wizardform;
        public int formtype;
        public bool aktiv;

        public WizardItemClass()
        {

        }

        public WizardItemClass(Form frm, int type)
        {
            wizardform = frm;
            formtype = type;
            aktiv = false;
        }
    
    }
}
