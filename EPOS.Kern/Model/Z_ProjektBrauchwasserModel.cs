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

        /// <summary>
        /// KBP-5a (Entscheid OE-5): Eingabeart der Zuordnung — 'jahr' (Vorgabe, Jahressumme mit Vorlage), 'monat' oder
        /// 'woche' (Schritt <see cref="BedarfsprofilEingabeartSchema.SCHRITT"/>). Gewählt im Bedarfsprofil-Dialog,
        /// geschrieben von WizardCtrl.Add_Projekt_Brauchwasser.
        /// </summary>
        public string Eingabeart = KaeltebedarfsprofilVorlagenSchema.EINGABE_JAHR;

        /// <summary>KBP-5a: bearbeitete Monatsanteile [%] der Projektkopie (12 Werte); <c>null</c> = unverändert.</summary>
        public double[] Monatsanteile;

        /// <summary>KBP-5a: bearbeitetes Wochenprofil der Projektkopie (168 Werte, Montag 0 Uhr zuerst); <c>null</c> = unverändert.</summary>
        public double[] Wochenprofil;

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
