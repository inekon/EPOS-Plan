using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class HeizkesselModel
    {
        public int ID;
        public string Name;
        public string Firma;
        public string Beschreibung;
        public double Ptherm;
        public int Brennstoff;
        public double Wirkungsgrad_Gas;
        public double Wirkungsgrad_Oel;
        public double Investitionskosten;
        public double Raumbedarf;
        public double Wartungskosten;

        /// <summary>
        /// Bezugsgröße von <see cref="Wartungskosten"/> — einer der drei Persistenzwerte
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>,
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_ARBEIT"/> oder
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT"/>
        /// (Entscheidung des Anwenders 18.08.2026, Migrationsschritt 15).
        /// Vorgabe ist der feste Jahresbetrag — Begründung bei
        /// <see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>.
        /// </summary>
        public string Wartungskosten_Einheit;

        public double Nutzungsdauer;
        public double CO2;
        public double SO2;
        public double NOx;
        public double CO;
        public double Staub;
        public double Betriebsbereitschaftverlust;
        public bool Brennwert;
        public int Vorlauf;
        public int Ruecklauf;

        public HeizkesselModel()
        {
            ID = 0;
            Name = "";
            Firma = "";
            Beschreibung = "";
            Ptherm = 0.0;
            Brennstoff = 0;
            Wirkungsgrad_Gas = 0;
            Wirkungsgrad_Oel = 0;
            Investitionskosten = 0;
            Raumbedarf = 0;
            Wartungskosten = 0;
            Wartungskosten_Einheit = DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR;
            Nutzungsdauer = 0;
            CO2 = 0;    
            SO2 = 0;    
            NOx = 0;    
            CO = 0;
            Staub = 0;
            Betriebsbereitschaftverlust = 0;
            Brennwert = false;
            Vorlauf = 0;
            Ruecklauf = 0;
        }
    }
}
