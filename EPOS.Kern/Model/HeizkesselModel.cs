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

        /// <summary>
        /// Nutzungsdauer laut Gerätekatalog [a] — Gerätedaten, NICHT rechenwirksam; maßgeblich
        /// ist die Nutzungsdauertabelle (Etappe E10, Kennzeichnung A8, Empfehlung E10-Q4 a).
        /// </summary>
        public double Nutzungsdauer;
        public double CO2;
        public double SO2;
        public double NOx;
        public double CO;
        public double Staub;
        public double Betriebsbereitschaftverlust;

        /// <summary>
        /// Einheit von <see cref="Betriebsbereitschaftverlust"/> —
        /// <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW"/> (Vorgabe, Bestand und Import)
        /// oder <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT"/> der Nennleistung.
        /// Gerechnet wird in kW (<see cref="KesselBereitschaft.LeistungKw"/>).
        /// </summary>
        public string Bereitschaft_Einheit;
        public bool Brennwert;
        public int Vorlauf;
        public int Ruecklauf;

        // --- Kennlinie (Konzept Kesselkennlinie 3.1, Schemaschritt KesselKennlinieSchema.SCHRITT) ---
        //
        // Leer (null) heisst „nicht gepflegt" und ist etwas anderes als 0: Was dann gilt, legen
        // die Etappen E2 bis E4 fest (Normvorgaben, Entscheid F1). In E1 liest kein Rechenweg
        // die Felder.

        /// <summary>
        /// Wirkungsgrad bei 30 % Last, heizwertbezogen, als Faktor (Konzept 3.1). Ein Wert über
        /// 1,5 gilt als Prozentangabe (<see cref="KesselKennlinieWerte.AlsFaktor"/>).
        /// <c>null</c> = nicht gepflegt.
        /// </summary>
        public double? Wirkungsgrad_Teillast30;

        /// <summary>
        /// Die Brennwertkennlinie rechnen? Zulässig nur bei <see cref="Brennwert"/> — die
        /// Controller schreiben den Schalter nie ohne ihn.
        /// </summary>
        public bool Kennlinie_Brennwert;

        /// <summary>Untere Modulationsgrenze [kW]; <c>null</c> = nicht gepflegt.</summary>
        public double? Mindestleistung;

        /// <summary>Brennstoff je Start [kWh]; <c>null</c> = nicht gepflegt.</summary>
        public double? Anfahrverlust_kWh;

        /// <summary>Mindestlaufzeit je Start im Takten [min]; <c>null</c> = nicht gepflegt.</summary>
        public int? Mindestlaufzeit_min;

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
            Bereitschaft_Einheit = DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW;
            Brennwert = false;
            Vorlauf = 0;
            Ruecklauf = 0;
            Wirkungsgrad_Teillast30 = null;
            Kennlinie_Brennwert = false;
            Mindestleistung = null;
            Anfahrverlust_kWh = null;
            Mindestlaufzeit_min = null;
        }
    }
}
