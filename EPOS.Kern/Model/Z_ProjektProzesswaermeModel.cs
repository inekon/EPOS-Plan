using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Z_ProjektProzesswaermeModel
    {
        public Z_ProjektProzesswaermeModel[] items;
        public int ID_Z;
        public int ID_Projekt;
        public int ID_Prozesswaerme;
        public string szProzessname;
        public double Summe;

        /// <summary>
        /// Der Betriebskalender der Zuordnung (<c>ID_Betriebskalender</c>, PW2/BW2); <c>null</c> =
        /// keiner — das Wochenprofil gilt für alle Wochen.
        /// </summary>
        public int? ID_Betriebskalender;

        /// <summary>Temperaturpaar der Projektkopie [°C]; <c>null</c> = ohne (PW1 Stufe 1).</summary>
        public double? Vorlauf;

        /// <summary>Rücklauf der Projektkopie [°C]; <c>null</c> = ohne (PW1 Stufe 1).</summary>
        public double? Ruecklauf;

        /// <summary>
        /// true = der Anwender hat das Paar im Dialog geändert; erst dann schreibt das Speichern es in
        /// die Projektkopie (<see cref="WizardCtrl.Add_Projekt_Prozess"/>). Ohne Änderung bleibt die
        /// Kopie, wie sie ist — eine neue trägt die Vorbelegung des Katalogs.
        /// </summary>
        public bool TemperaturGeaendert;

        public Z_ProjektProzesswaermeModel()
        {
            items = null;
            ID_Z = 0;
            ID_Projekt = 0;
            ID_Prozesswaerme = 0;
            szProzessname = "";
            Summe = 0;
        }

    }
}
