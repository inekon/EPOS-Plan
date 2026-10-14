using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjektBrauchwasserModel
    {
        public Z_ProjektBrauchwasserModel[] items;
        public int ID_Z;
        public int ID_Projekt;
        public int ID_Brauchwasser;
        public string szBezeichner; 
        public double Summe;

        /// <summary>
        /// Der Betriebskalender der Zuordnung (<c>ID_Betriebskalender</c>, PW2/BW2); <c>null</c> =
        /// keiner — das Wochenprofil gilt für alle Wochen.
        /// </summary>
        public int? ID_Betriebskalender;

        public Z_ProjektBrauchwasserModel()
        {
            items = null;
            ID_Z = 0;
            ID_Projekt = 0;
            ID_Brauchwasser = 0;
            szBezeichner = "";
            Summe = 0;
        }

    }
}
