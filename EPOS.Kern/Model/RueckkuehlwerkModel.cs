namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Rückkühlwerk</b> — Katalogsatz (<c>Tab_Rueckkuehlwerk_STAMM</c>) oder Projektkopie (<c>Tab_Rueckkuehlwerk</c>),
    /// K-F1. Spaltennamen, Persistenzwerte und Grenzen stehen bei <see cref="RueckkuehlwerkSchema"/>; NULL bleibt
    /// <c>null</c>, und <c>null</c> heißt Vorgabe (die Festwerte der passenden Rückkühlart).
    /// </summary>
    public sealed class RueckkuehlwerkModel
    {
        /// <summary>ID des Satzes; 0 = neu.</summary>
        public int Id { get; set; }

        /// <summary>Projekt der Kopie; <c>null</c> am Katalogsatz.</summary>
        public int? IdProjekt { get; set; }

        /// <summary>Katalogsatz, aus dem die Projektkopie stammt; <c>null</c> am Katalogsatz oder nach dessen Löschen.</summary>
        public int? IdStamm { get; set; }

        /// <summary>Name des Rückkühlwerks.</summary>
        public string Bezeichner { get; set; } = "";

        /// <summary>Freitext.</summary>
        public string Beschreibung { get; set; }

        /// <summary>Bauart (<see cref="RueckkuehlwerkSchema.BAUARTEN"/>); <c>null</c> = trocken.</summary>
        public string Bauart { get; set; }

        /// <summary>Abzuführende Wärme im Nennpunkt [kW].</summary>
        public double? Nennleistung_kW { get; set; }

        /// <summary>Grädigkeit im Nennpunkt [K]; <c>null</c> = Festwert der passenden Rückkühlart.</summary>
        public double? Annaeherung_Nenn_K { get; set; }

        /// <summary>Weg der Annäherung (<c>FEST</c>/<c>LASTABHAENGIG</c>); <c>null</c> = fest.</summary>
        public string Annaeherung_Weg { get; set; }

        /// <summary>Ventilatorleistung im Nennpunkt [kW]; <c>null</c> = Hilfsstrom der Rückkühlung an der Kältemaschine.</summary>
        public double? Ventilator_Nenn_kW { get; set; }

        /// <summary>Regelung des Ventilators (<c>EIN_AUS</c>/<c>STUFEN</c>/<c>DREHZAHL</c>).</summary>
        public string Ventilator_Regelung { get; set; }

        /// <summary>Zahl der Stufen bei <c>STUFEN</c> (≥ 2).</summary>
        public int? Ventilator_Stufen { get; set; }

        /// <summary>Kleinste Drehzahl als Anteil der Nenndrehzahl [0…1] bei <c>DREHZAHL</c>.</summary>
        public double? Ventilator_Drehzahl_Min { get; set; }

        /// <summary>Befeuchtungswirkungsgrad (adiabat/hybrid) [0…1].</summary>
        public double? Befeuchtung_Wirkungsgrad { get; set; }

        /// <summary>Außentemperatur, ab der befeuchtet wird [°C].</summary>
        public double? Befeuchtung_Ab_C { get; set; }

        /// <summary>Faktor auf die rechnerische Verdunstung [—].</summary>
        public double? Verdunstung_Faktor { get; set; }

        /// <summary>Eindickung (&gt; 1) [—].</summary>
        public double? Eindickung { get; set; }

        /// <summary>Driftverlust als Anteil des Umlaufs [0…1].</summary>
        public double? Drift_Anteil { get; set; }

        /// <summary>Schaltung der freien Kühlung (<c>PARALLEL</c>/<c>REIHE</c>); <c>null</c> = parallel.</summary>
        public string Freikuehlung_Schaltung { get; set; }

        /// <summary>Gerätepreis [€].</summary>
        public double? Modulkosten { get; set; }

        /// <summary>Auslieferungssatz (<c>ReadOnly = 1</c>); nur am Katalogsatz.</summary>
        public bool ReadOnly { get; set; }
    }
}
