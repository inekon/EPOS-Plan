using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public partial class WizardItemClass
    {
        public const int KOMPONENTEN_ITEM = 0;
        public const int PROJEKT_ITEM = 1;
        public const int GEBAEUDE_ITEM = 2;
        public const int WAERMEBEDARF_ITEM = 3;
        public const int PROZESS_ITEM = 4;
        public const int STROMSTD_ITEM = 5;
        public const int STROMLASTGANG_ITEM = 6;
        public const int WP_ITEM = 7;
        public const int SOLAR_ITEM = 8;
        public const int PV_ITEM = 9;
        public const int SP_ITEM = 10;
        public const int KESSEL_ITEM = 11;
        public const int BHKW_ITEM = 12;
        public const int PUFFER_ITEM = 13;

        public const int WP_TYP = 1;
        public const int SOLAR_TYP = 2;
        public const int PV_TYP = 3;
        public const int SP_TYP = 4;
        public const int REF_KESSEL_TYP = 5;
        public const int REF_SP_TYP = 6;
        public const int REF_WP_TYP = 7;
        public const int REF_SOLAR_TYP = 8;
        public const int REF_PV_TYP = 9;
        public const int KESSEL_TYP = 10;
        public const int BHKW_TYP = 11;
        public const int PUFFER_TYP = 12;

        /// <summary>
        /// Untergrenze der VORLAEUFIGEN Ids, die die Auswahl-Dialoge ungespeicherten
        /// Listenzeilen geben (Hausmuster "startindex = 100000" in zwoelf Dialogen,
        /// z. B. Form_PufferSp.cs, Form_Heizkessel.cs): Die Zeile braucht ein
        /// Unterscheidungsmerkmal fuer gleichnamige Eintraege, hat aber noch keinen
        /// AutoWert. Konsumenten echter Anlagen-Ids muessen solche Werte wie
        /// "keine Id" behandeln (FR-5) - echte AutoWerte koennten diese Marke
        /// sonst eines Tages erreichen und verwechselt werden.
        /// </summary>
        public const int ID_UNGESPEICHERT_START = 100000;

        public int formtype;
        public bool aktiv;

        public WizardItemClass()
        {

        }
    
    }
}
