using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjGebModel
    {
        public Z_ProjGebModel[] items;
        public int ID_Z;
        public int ID_Projekt;
        public int ID_Gebaeude;
        public string szGebaeude;
        public double Wohnflaeche;
        public string Einheit;
        public double Jahresnutzungsgrad;
        public bool DezentralWarmwasser;
        public string Baualtersklasse;
        public string Gebaeudename;
        public string Beschreibung;
        public string Gebaeudeart;

        /// <summary>
        /// Der Katalogsatz (<c>Tab_Gebaeude_STAMM.ID</c>), aus dem die Projektkopie stammt
        /// bzw. entstehen soll — <c>Tab_Gebaeude.ID_Gebaeude_Stamm</c> (Schemaschritt 121).
        /// <c>null</c> = kein Verweis (Altbestand, gelöschter Katalogsatz); dann sucht
        /// <c>WizardCtrl.Add_Projekt_ZuordungGebäude</c> über <see cref="Gebaeudename"/>.
        /// </summary>
        public int? ID_Gebaeude_Stamm;

        /// <summary>
        /// Die AUSSTEHENDE Herkunft einer neuen Zeile aus dem Gebäudeimport (Stufe G4, Welle 4) —
        /// Quelle und Paarungen, die <c>WizardCtrl.GebaeudeZuordnungAnlegen</c> nach dem Anlegen der
        /// Projektkopie im selben Vorgang schreibt. <c>null</c> = keine (jede gewöhnliche Zeile). Eine
        /// bleibende Zeile schreibt sie nicht noch einmal.
        /// </summary>
        internal GebaeudeImportHerkunft Importherkunft;

        public Z_ProjGebModel()
        {
            items = null;
            ID_Z = 0;
            ID_Projekt = 0;
            ID_Gebaeude = 0;
            szGebaeude = "";
            Wohnflaeche = 0.0;
            Einheit = "";
            Jahresnutzungsgrad = 0.0;
            DezentralWarmwasser = false;
        }
    }
}
