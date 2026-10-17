namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Feldsatz des BHKW-Katalogeditors — das plattformfreie Abbild von
/// <c>BHKWStammModel</c> (iU9-W6.2).
///
/// <para><b>Kein Kostenfeld.</b> Die fünf Einzelposten, die Wartung je kWhel und die daraus
/// abgeleitete Investition je kWel sind mit Welle KB-4 entfallen (Anwenderentscheid
/// 13.10.2026 „eine Kostenbasis, keine Doppelhaltung", Schemaschritt
/// <c>GeraetekostenEntfallSchema</c>). Gepflegt werden die Gerätekosten in der
/// Kostenverwaltung — der Weg dorthin steht als Knopfleiste unter den Gruppen —, und der
/// Aufklapper „Alle Daten" zeigt die Summe der Satzvorlage als Lesewert.</para>
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

    /// <summary>
    /// Gesamtwirkungsgrad — die SUMME der zwei Anteile. Der Dialog ZEIGT ihn nur; wer
    /// ihn ändert, ändert die Anteile (Schemaschritt 99). Beim Speichern rechnet der
    /// Kern ihn aus den Anteilen nach (<c>BhkwWirkungsgrad.GesamtZumSchreiben</c>).
    /// </summary>
    public double? Wirkungsgrad { get; set; }

    /// <summary>
    /// Elektrischer Wirkungsgrad als Faktor. <c>null</c> = nicht gepflegt; der Dialog
    /// bietet dann die Aufteilung des Gesamtwerts als Vorschlag an.
    /// </summary>
    public double? WirkungsgradEl { get; set; }

    /// <summary>Thermischer Wirkungsgrad als Faktor; <c>null</c> = nicht gepflegt.</summary>
    public double? WirkungsgradTh { get; set; }

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

    /// <summary>
    /// Höchster Rücklauf — die Abschaltgrenze [°C] (<c>Ruecklauf_Max</c>, Bereich 40 … 90; UB‑E3‑b, U‑3): Liegt der
    /// Rücklauf zum Modul darüber, liefert es in der Stunde nichts. <c>null</c> = keine Grenze. Ein neuer Satz trägt
    /// die Vorgabe 70 °C (<see cref="WindowsFormsApplication1.GeraetegrenzWerte.BHKW_RUECKLAUF_MAX_VORGABE_C"/>); ein
    /// gelesener Satz überschreibt sie mit seinem Wert, auch mit leer.
    /// </summary>
    public double? RuecklaufMax { get; set; } = WindowsFormsApplication1.GeraetegrenzWerte.BHKW_RUECKLAUF_MAX_VORGABE_C;

    // --- Gruppe „Teillast und Takten" (Welle M4: BH1, BH2) ---------------------

    /// <summary>
    /// Elektrischer Wirkungsgrad bei 50 % elektrischer Last als Faktor (BH1). <c>null</c> = wie
    /// Volllast; mit Wert rechnet das Modul die Teillastkennlinie.
    /// </summary>
    public double? WirkungsgradEl50 { get; set; }

    /// <summary>Thermischer Wirkungsgrad bei 50 % elektrischer Last als Faktor (BH1); <c>null</c> = wie Volllast.</summary>
    public double? WirkungsgradTh50 { get; set; }

    /// <summary>Anfahrverlust je Start [kWh Brennstoff] (BH2); <c>null</c> = nicht gepflegt.</summary>
    public double? AnfahrverlustKwh { get; set; }

    /// <summary>
    /// Mindestlaufzeit je Start [min] (BH2); <c>null</c> = nicht gepflegt. Mit Anfahrverlust oder
    /// Mindestlaufzeit taktet das Modul unter seiner Untergrenze, sonst bleibt es dort aus.
    /// </summary>
    public int? MindestlaufzeitMin { get; set; }

    // --- Gruppe 3: Gerätedaten ohne Kosten -------------------------------------
    //
    // Die sieben Kostenfelder sind mit Welle KB-4 entfallen; der Satz führt keine
    // Kostenspalte mehr, also reist auch keine durch diesen Feldsatz.

    /// <summary>Raumbedarf [m³].</summary>
    public double? Raumbedarf { get; set; }

    /// <summary>Nutzungsdauer [Jahre], ganzzahlig.</summary>
    public int? Nutzungsdauer { get; set; }

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

    // OHNE den Schalter „mit SCR" seit dem 15.09.2026: Er steuerte ausschliesslich den
    // Knopf „Eintragen" der Emissionsgruppe, und die ist mit den Emissionsfeldern
    // ersatzlos entfallen (Anwenderentscheid). Gespeichert wurde er nie.

    // --- Herkunft --------------------------------------------------------------

    /// <summary>
    /// Stammt der geladene Satz aus dem Auslieferungskatalog (<c>ReadOnly</c>)?
    /// Überschreiben bleibt möglich, verlangt dann aber eine ausdrückliche Bestätigung.
    /// </summary>
    public bool Katalogsatz { get; set; }
}
