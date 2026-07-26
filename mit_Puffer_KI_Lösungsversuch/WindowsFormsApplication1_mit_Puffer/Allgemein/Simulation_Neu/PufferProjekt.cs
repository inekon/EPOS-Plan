using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Gemeinsamer, dem Projekt zugeordneter Pufferspeicher (Ein-Knoten-Energiemodell)
    /// fuer das neue gekoppelte Modell. Ersetzt den bisher BHKW-internen Pendelspeicher:
    /// im gekoppelten Lauf teilen sich alle Erzeuger genau diesen einen Speicher.
    ///
    /// Kapazitaet wird identisch zum Bestand aus dem Pendelspeicher-Wert abgeleitet
    /// (siehe SimulationControl.Simulation_BHKW_Ctrl: Volumen * 20000 / 860), damit
    /// altes und neues Modell auf derselben Speichergroesse vergleichbar sind.
    /// </summary>
    public class PufferProjekt
    {
        public float Kapazitaet_kWh = 0f;   // nutzbarer Energieinhalt
        public float Inhalt_kWh = 0f;       // aktueller Ladezustand
        public float Verlustrate_kWh_h = 0f;// stehende Verluste je Stunde

        // Temperatur-Randbedingungen (Auslegung), fuer den Vorlauf-Check / spaetere Schichtung
        public float VorlaufSoll = 55f;
        public float Ruecklauf = 40f;

        public float FreieKapazitaet_kWh
        {
            get
            {
                float frei = Kapazitaet_kWh - Inhalt_kWh;
                return frei > 0f ? frei : 0f;
            }
        }

        /// <summary>Ladegrad 0..1.</summary>
        public float Ladegrad
        {
            get { return Kapazitaet_kWh > 0f ? Inhalt_kWh / Kapazitaet_kWh : 0f; }
        }

        /// <summary>Mittlere Puffertemperatur (Ein-Knoten-Naeherung: Ruecklauf .. Vorlauf).</summary>
        public float MittlereTemperatur()
        {
            return Ruecklauf + Ladegrad * (VorlaufSoll - Ruecklauf);
        }

        /// <summary>Laedt kWh; gibt die tatsaechlich aufgenommene Menge zurueck (Rest = Ueberschuss).</summary>
        public float Laden(float kWh)
        {
            if (kWh <= 0f) return 0f;
            float frei = FreieKapazitaet_kWh;
            float aufnahme = kWh <= frei ? kWh : frei;
            Inhalt_kWh += aufnahme;
            return aufnahme;
        }

        /// <summary>
        /// Entnimmt bis zu bedarf_kWh; nur nutzbar, wenn die Puffertemperatur den
        /// geforderten Vorlauf erreicht. Gibt die gedeckte Menge zurueck.
        /// </summary>
        public float Entladen(float bedarf_kWh)
        {
            if (bedarf_kWh <= 0f || Inhalt_kWh <= 0f) return 0f;
            if (MittlereTemperatur() < VorlaufSoll) return 0f;
            float abgabe = bedarf_kWh <= Inhalt_kWh ? bedarf_kWh : Inhalt_kWh;
            Inhalt_kWh -= abgabe;
            return abgabe;
        }

        public void Verluste()
        {
            Inhalt_kWh -= Verlustrate_kWh_h;
            if (Inhalt_kWh < 0f) Inhalt_kWh = 0f;
        }

        public void Reset()
        {
            Inhalt_kWh = 0f;
        }
    }
}
