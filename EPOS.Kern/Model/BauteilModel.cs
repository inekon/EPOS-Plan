namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Bauteil einer Zone — eine Zeile aus <c>Tab_Bauteil</c> (Schemaschritt S-C,
    /// <see cref="ZonenSchema"/>). Die Aussage „außen oder innen" trägt allein
    /// <see cref="Randbedingung"/> (W6).
    ///
    /// <para><b>Vorläufige Zeilen tragen eine NEGATIVE Id</b> — wie bei der Zone (Muster A6).</para>
    /// </summary>
    public class BauteilModel
    {
        /// <summary>Primärschlüssel; ≤ 0 = vorläufig (neu anzulegen).</summary>
        public int ID;

        /// <summary>Die Zone des Bauteils — Eltern, Kaskade; setzt der Schreibweg.</summary>
        public int ID_Zone;

        /// <summary>Reihenfolge in der Zone, lückenlos ab 1 — vergibt der Schreibweg.</summary>
        public int Rang;

        /// <summary>Name des Bauteils (Pflicht, höchstens 80 Zeichen).</summary>
        public string Bezeichner = "";

        /// <summary>Bauteilart (<see cref="DbWerte.BAUTEILARTEN"/>), Pflicht.</summary>
        public string Bauteilart = DbWerte.BAUTEILART_AUSSENWAND;

        /// <summary>Aufbau (<c>Tab_Bauteilaufbau.ID</c> desselben Projekts); <c>null</c> = nur U-Wert.</summary>
        public int? ID_Aufbau;

        /// <summary>Fläche [m²] (Pflicht, größer als null).</summary>
        public double Flaeche;

        /// <summary>U-Wert [W/(m²·K)]; <c>null</c> = aus dem Aufbau gerechnet.</summary>
        public double? U_Wert;

        /// <summary>g-Wert (Fenster, Vorhangfassade); <c>null</c> = Vorgabe.</summary>
        public double? g_Wert;

        /// <summary>Rahmenanteil (Fenster, Vorhangfassade); <c>null</c> = Vorgabe.</summary>
        public double? Rahmenanteil;

        /// <summary>Verschattungsfaktor (Fenster, Vorhangfassade); <c>null</c> = Vorgabe.</summary>
        public double? Verschattungsfaktor;

        /// <summary>Neigung [°]; <c>null</c> = nach Bauteilart (<c>GebaeudeZonenCtrl.NeigungVorgabe</c>).</summary>
        public double? Neigung;

        /// <summary>Azimut [°], 0° = Nord; <c>null</c> nur bei Neigung 0° oder 180°.</summary>
        public double? Azimut;

        /// <summary>Randbedingung (<see cref="DbWerte.RANDBEDINGUNGEN"/>); <c>null</c> = Außenluft.</summary>
        public string Randbedingung;

        /// <summary>Wärmebrücke ψ·L [W/K]; <c>null</c> = keine.</summary>
        public double? Psi_L;

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); <c>null</c> = nicht angegeben.</summary>
        public string Herkunft;

        /// <summary>IFC-GUID oder gbXML-id der Quellentität; <c>null</c> = keine.</summary>
        public string Quellkennung;

        /// <summary>Eine entkoppelte Kopie.</summary>
        public BauteilModel Kopie() => (BauteilModel)MemberwiseClone();
    }
}
