namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Feldsatz des BHKW-Katalogeditors — das plattformfreie Abbild von
/// <c>BHKWStammModel</c> (iU9-W6.2).
///
/// <para><b>Die fünf Einzelposten führen.</b> Modul + Montage + Lieferung +
/// Schallschutzhaube + Abgasreinigung ergeben die Investition;
/// <see cref="InvestitionJeKWel"/> [€/kWel] ist daraus ABGELEITET, nur Anzeige und
/// schreibgeschützt (Nutzerentscheid 22.08.2026, Regel in <c>BHKWKosten</c>). Zuvor
/// führten beide Wege dieselbe Größe und liefen auseinander.</para>
///
/// <para><b>Die Emissionen sind ganzzahlig.</b> <c>BHKWStammModel</c> führt NOx, CO2,
/// CO, SO2 und Staub als <c>int</c> — anders als der Heizkessel, der sie als
/// <c>double</c> speichert. Das ist Bestand und bleibt so.</para>
/// </summary>
public sealed class BhkwKatalogDaten
{
    // --- Gruppe 1: Bezeichnung -------------------------------------------------

    /// <summary>
    /// Modulname. Im Modus „Bearbeiten" nur lesbar — <c>BHKWStammCtrl.Update</c>
    /// filtert per <c>Bezeichner</c>, ein hier geänderter Name träfe keinen Satz
    /// (Abweichung A-3 des Protokolls W6). Umbenannt wird über „Speichern unter".
    /// </summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Hersteller.</summary>
    public string Firma { get; set; } = "";

    /// <summary>Beschreibung (mehrzeilig).</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Motortyp.</summary>
    public string Motortyp { get; set; } = "";

    // --- Gruppe 2: Technische Daten --------------------------------------------

    /// <summary>Thermische Leistung [kW].</summary>
    public double? Ptherm { get; set; }

    /// <summary>Elektrische Leistung [kW]. Bei 0 ist der Wert je kWel unbestimmt.</summary>
    public double? Pel { get; set; }

    /// <summary>Gesamtwirkungsgrad.</summary>
    public double? Wirkungsgrad { get; set; }

    /// <summary>Untere Grenzleistung [%].</summary>
    public double? Grenzleistung { get; set; }

    /// <summary>
    /// Energieträger als <b>0-basierter</b> Listenindex — anders als beim Heizkessel!
    /// <c>Form_DBBHKW.InitDatensatzUpdate</c> setzt <c>m_Brennstoff = SelectedIndex</c>
    /// ohne <c>+ 1</c>, während <c>SetControls</c> mit
    /// <c>SelectedIndex = brennstoff</c> zurückliest. Bestand, hier unverändert
    /// übernommen (Regel F3).
    /// </summary>
    public int? Brennstoff { get; set; }

    /// <summary>Vorlauftemperatur [°C], ganzzahlig.</summary>
    public int? Vorlauf { get; set; }

    /// <summary>Rücklauftemperatur [°C], ganzzahlig.</summary>
    public int? Ruecklauf { get; set; }

    // --- Gruppe 3: Kosten ------------------------------------------------------

    /// <summary>Kosten des Moduls [€].</summary>
    public double? KostenModul { get; set; }

    /// <summary>Montage und Inbetriebnahme [€].</summary>
    public double? KostenMontage { get; set; }

    /// <summary>Lieferung (50 km Umkreis) [€].</summary>
    public double? KostenLieferung { get; set; }

    /// <summary>Schallschutzhaube [€].</summary>
    public double? KostenSchallschutzhaube { get; set; }

    /// <summary>Abgasreinigung, z. B. Kat [€].</summary>
    public double? KostenAbgasreinigung { get; set; }

    /// <summary>Raumbedarf [m³].</summary>
    public double? Raumbedarf { get; set; }

    /// <summary>Wartungskosten [€/kWhel].</summary>
    public double? WartungskostenJeKWhel { get; set; }

    /// <summary>Nutzungsdauer [Jahre], ganzzahlig.</summary>
    public int? Nutzungsdauer { get; set; }

    /// <summary>
    /// Der beim Laden gespeicherte Wert je kWel [€/kWel]. Er wird angezeigt, solange
    /// nichts geändert wurde; passt er nicht zur Summe, benennt die Hinweiszeile das.
    /// Beim Speichern entsteht der Wert neu aus Posten und Pel.
    /// </summary>
    public double? InvestitionJeKWel { get; set; }

    // --- Gruppe 4: Emissionsfaktoren -------------------------------------------

    /// <summary>CO2 [g/MWh].</summary>
    public int? CO2 { get; set; }

    /// <summary>SO2 [g/MWh].</summary>
    public int? SO2 { get; set; }

    /// <summary>NOx [g/MWh].</summary>
    public int? NOx { get; set; }

    /// <summary>CO [g/MWh].</summary>
    public int? CO { get; set; }

    /// <summary>Staub [g/MWh].</summary>
    public int? Staub { get; set; }

    /// <summary>Schalter „mit SCR" — er steuert nur die Vorgabewerte, er wird nicht gespeichert.</summary>
    public bool ScrVorhanden { get; set; }

    // --- Herkunft --------------------------------------------------------------

    /// <summary>
    /// Stammt der geladene Satz aus dem Auslieferungskatalog (<c>ReadOnly</c>)?
    /// Überschreiben bleibt möglich, verlangt dann aber eine ausdrückliche Bestätigung.
    /// </summary>
    public bool Katalogsatz { get; set; }
}
