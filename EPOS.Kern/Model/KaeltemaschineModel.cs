using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Kältemaschine</b> — Katalogsatz (<c>Tab_Kaeltemaschine_STAMM</c>) oder Projektkopie
    /// (<c>Tab_Kaeltemaschine</c>), KU3-1. Spaltennamen und Persistenzwerte stehen bei
    /// <see cref="KaeltemaschineSchema"/>; NULL bleibt <c>null</c>.
    /// </summary>
    public sealed class KaeltemaschineModel
    {
        /// <summary>ID des Satzes; 0 = neu.</summary>
        public int Id { get; set; }

        /// <summary>Projekt der Kopie; <c>null</c> am Katalogsatz.</summary>
        public int? IdProjekt { get; set; }

        /// <summary>Katalogsatz, aus dem die Projektkopie stammt; <c>null</c> am Katalogsatz oder nach dessen Löschen.</summary>
        public int? IdStamm { get; set; }

        /// <summary>Name des Geräts.</summary>
        public string Bezeichner { get; set; } = "";

        /// <summary>Hersteller (Text).</summary>
        public string Firma { get; set; }

        /// <summary>Typbezeichnung (Text).</summary>
        public string Typ { get; set; }

        /// <summary>Freitext.</summary>
        public string Beschreibung { get; set; }

        /// <summary>Nennkälteleistung [kW].</summary>
        public double? Nennkaelteleistung_kW { get; set; }

        /// <summary>EER im Nennpunkt [—].</summary>
        public double? Nenn_EER { get; set; }

        /// <summary>Kältemittel (Text).</summary>
        public string Kaeltemittel { get; set; }

        /// <summary>Rückkühlart — Persistenzwert aus <see cref="KaeltemaschineSchema.RUECKKUEHLARTEN"/>.</summary>
        public string Rueckkuehlart { get; set; }

        /// <summary>Kleinste Teillast [%].</summary>
        public double? Mindestteillast_Prozent { get; set; }

        /// <summary>Elektrische Leistung der Rückkühlung im Nennpunkt [kW]; <c>null</c> = im EER enthalten.</summary>
        public double? Hilfsstrom_Rueckkuehlung_kW { get; set; }

        /// <summary>Kleinster zulässiger Kaltwasservorlauf [°C].</summary>
        public double? Kaltwasser_Vorlauf_Min { get; set; }

        /// <summary>Gerätepreis [€].</summary>
        public double? Modulkosten { get; set; }

        /// <summary>Gehört zur Auslieferung (nur am Katalogsatz).</summary>
        public bool ReadOnly { get; set; }

        /// <summary>Kaltwasservorlauf der Projektkopie [°C] (<c>Kuehl_Vorlauf</c>, Schritt 183); <c>null</c> = kleinste Stützstelle.</summary>
        public double? Kuehl_Vorlauf { get; set; }

        /// <summary>Hilfsstromanteil des Kältekreises [0…1) (<c>Kuehl_Hilfsstromanteil</c>, Schritt 183); <c>null</c> = kein Zuschlag.</summary>
        public double? Kuehl_Hilfsstromanteil { get; set; }

        /// <summary>Die Kennlinie des Geräts.</summary>
        public List<KaeltemaschineKenndatenModel> Kennlinie { get; set; } = new List<KaeltemaschineKenndatenModel>();
    }

    /// <summary>
    /// <b>Ein Kennlinienpunkt der Kältemaschine</b> (<c>Tab_Kenndaten_Kaeltemaschine(_STAMM)</c>): EER und
    /// Kälteleistung über Rückkühl- und Kaltwassertemperatur.
    /// </summary>
    public sealed class KaeltemaschineKenndatenModel
    {
        /// <summary>Eintrittstemperatur des Rückkühlmediums [°C] (luftgekühlt: Außenluft).</summary>
        public double Rueckkuehltemperatur { get; set; }

        /// <summary>Kaltwasservorlauf [°C].</summary>
        public double Kaltwassertemperatur { get; set; }

        /// <summary>EER im Punkt [—].</summary>
        public double? EER { get; set; }

        /// <summary>Kälteleistung im Punkt [kW].</summary>
        public double? Kaelteleistung_kW { get; set; }
    }
}
