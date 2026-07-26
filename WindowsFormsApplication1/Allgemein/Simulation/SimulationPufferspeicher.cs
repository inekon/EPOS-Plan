using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einfaches Energiebilanz-Modell eines thermischen Pufferspeichers für die
    /// Jahressimulation (Stundenschritte, 1 h => kW entspricht kWh).
    ///
    /// Stufe 1 der Pufferspeicher-Integration:
    /// - Nutzbare Kapazität aus Volumen und Temperaturspreizung der Zuordnung
    ///   (Z_ProjektPufferSp: Vorlauf/Rücklauf; Tab_Pufferspeicher: Gesamtvolumen):
    ///     Q_max [kWh] = Volumen [l] * 1,16 Wh/(l*K) * (Vorlauf - Rücklauf) / 1000
    /// - Bereitschaftsverluste [kWh/24h] wirken stündlich, anteilig zum Füllstand.
    /// - Keine Temperaturschichtung, keine Begrenzung der Be-/Entladeleistung
    ///   (bewusste Vereinfachung, siehe Konzept).
    /// </summary>
    public class SimulationPufferspeicher
    {
        public string Bezeichner = "";
        public string Erzeuger = "";

        /// <summary>Nutzbare Speicherkapazität [kWh]</summary>
        public double Q_max = 0;

        /// <summary>Aktueller Speicherinhalt (State of Charge) [kWh]</summary>
        public double SOC = 0;

        /// <summary>Bereitschaftsverlust bei vollem Speicher [kWh je Stunde]</summary>
        public double VerlustProStunde = 0;

        /// <summary>
        /// Regeneration/Nachladung [kW] - nur bei Verwendung als Wärmequelle
        /// (der Speicher wird laufend aus Umwelt-/Abwärme nachgeladen).
        /// </summary>
        public double RegenerationProStunde = 0;

        /// <summary>
        /// Einschaltschwelle der Speicherregelung als Anteil der nutzbaren
        /// Kapazität (0..1): Fällt der Füllstand darunter, läuft der Erzeuger an.
        /// </summary>
        public double SchwelleEin = 0.10;

        /// <summary>
        /// Abschaltschwelle als Anteil der nutzbaren Kapazität (0..1): Ab diesem
        /// Füllstand gilt der Speicher als voll und der Erzeuger schaltet ab.
        /// Bewusst unter 100 %, da die Bereitschaftsverluste den Füllstand jede
        /// Stunde absenken.
        /// </summary>
        public double SchwelleAus = 0.95;

        // Ganglinien für Auswertung, Charts und CSV-Export
        public float[] SOC_stuendlich = new float[8760];
        public float[] Ladung_stuendlich = new float[8760];
        public float[] Entladung_stuendlich = new float[8760];

        // Jahressummen [kWh]
        public double Ladung_gesamt = 0;
        public double Entladung_gesamt = 0;
        public double Verluste_gesamt = 0;

        /// <summary>
        /// Initialisiert den Speicher aus den Zuordnungs- und Stammdaten.
        /// </summary>
        /// <param name="volumenLiter">Gesamtvolumen [l] (Tab_Pufferspeicher)</param>
        /// <param name="vorlauf">Vorlauftemperatur [°C] (Z_ProjektPufferSp)</param>
        /// <param name="ruecklauf">Rücklauftemperatur [°C] (Z_ProjektPufferSp)</param>
        /// <param name="bereitschaftsverlusteProTag">Bereitschaftsverluste [kWh/24h] (Tab_Pufferspeicher)</param>
        public void Init(double volumenLiter, int vorlauf, int ruecklauf, double bereitschaftsverlusteProTag)
        {
            double deltaT = vorlauf - ruecklauf;
            if (deltaT <= 0) deltaT = 10; // Fallback, falls keine Temperaturen gepflegt sind

            // 1,16 Wh/(l*K) -> kWh
            Q_max = volumenLiter * 1.16 * deltaT / 1000.0;
            VerlustProStunde = bereitschaftsverlusteProTag / 24.0;
            Reset();
        }

        /// <summary>Setzt den Speicherzustand für einen neuen Simulationslauf zurück.</summary>
        public void Reset()
        {
            SOC = 0;
            Ladung_gesamt = 0;
            Entladung_gesamt = 0;
            Verluste_gesamt = 0;
            Array.Clear(SOC_stuendlich, 0, SOC_stuendlich.Length);
            Array.Clear(Ladung_stuendlich, 0, Ladung_stuendlich.Length);
            Array.Clear(Entladung_stuendlich, 0, Entladung_stuendlich.Length);
        }

        /// <summary>
        /// Lädt den Speicher mit der angebotenen Energie [kWh] und liefert zurück,
        /// wie viel davon tatsächlich aufgenommen wurde (Rest: Speicher voll).
        /// </summary>
        public double Laden(double energieKWh, int stunde)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            double frei = Q_max - SOC;
            double ladung = Math.Min(energieKWh, frei);
            if (ladung <= 0) return 0;

            SOC += ladung;
            Ladung_gesamt += ladung;
            if (stunde >= 0 && stunde < 8760) Ladung_stuendlich[stunde] += (float)ladung;
            return ladung;
        }

        /// <summary>
        /// Entnimmt die angeforderte Energie [kWh] aus dem Speicher und liefert
        /// zurück, wie viel tatsächlich geliefert werden konnte (Rest: Speicher leer).
        /// </summary>
        public double Entladen(double energieKWh, int stunde)
        {
            if (energieKWh <= 0 || Q_max <= 0) return 0;

            double entnahme = Math.Min(energieKWh, SOC);
            if (entnahme <= 0) return 0;

            SOC -= entnahme;
            Entladung_gesamt += entnahme;
            if (stunde >= 0 && stunde < 8760) Entladung_stuendlich[stunde] += (float)entnahme;
            return entnahme;
        }

        /// <summary>
        /// Verrechnet den stündlichen Bereitschaftsverlust (anteilig zum Füllstand)
        /// und speichert den Speicherzustand der Stunde für die Auswertung.
        /// </summary>
        public void StundeAbschliessen(int stunde)
        {
            if (Q_max > 0 && SOC > 0)
            {
                double verlust = VerlustProStunde * (SOC / Q_max);
                if (verlust > SOC) verlust = SOC;
                SOC -= verlust;
                Verluste_gesamt += verlust;
            }

            if (stunde >= 0 && stunde < 8760) SOC_stuendlich[stunde] = (float)SOC;
        }
    }
}
