namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild des Pufferspeicher-Projektdialogs für den Hilfe-Assistenten
/// (Welle KI‑F2).
///
/// <para><b>Warum ein Sichtmodell und nicht das Daten-Objekt.</b> Dieser Dialog reicht
/// seine Werte nicht als veränderliches DTO herein und hinaus: Was hereinkommt, sind
/// <c>IdProjekt</c> und <c>IdPuffer</c>, und was der Anwender bearbeitet, steht bis zum
/// „Übernehmen" in den Eingabefeldern der Maske — erst dort entsteht der unveränderliche
/// Record <c>PspEingaben</c>. Ein an <c>PspEingaben</c> angemeldeter Katalog zeigte dem
/// Assistenten den Stand von vorhin und setzte ins Leere. Diese Klasse legt sich statt
/// dessen über die LEBENDEN Felder der Maske: Jeder Wert geht durch denselben Weg wie
/// die Tastatur — lesen, was dasteht, und setzen, was der Anwender sähe, wenn er es
/// tippte.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten; der Zustand bleibt im Dialog, wohin er gehört (Muster
/// <c>SimulationKiSicht</c>, <c>StromspeicherKiSicht</c>).</para>
///
/// <para><b>Ein nicht gesetzter Weg liest den Vorgabewert und schreibt nichts.</b> Das
/// ist derselbe Zustand, den der Anwender sieht, wenn der Block gar nicht steht —
/// die erweiterten Schichtfelder etwa gibt es erst ab zwei Schichten.</para>
/// </summary>
public sealed class PufferSpProjektKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? BezeichnerLesen { get; init; }
    public Action<string>? BezeichnerSetzen { get; init; }

    public Func<int?>? VolumenLesen { get; init; }
    public Action<int?>? VolumenSetzen { get; init; }

    public Func<double?>? VerlusteLesen { get; init; }
    public Action<double?>? VerlusteSetzen { get; init; }

    public Func<int?>? VorlaufLesen { get; init; }
    public Action<int?>? VorlaufSetzen { get; init; }

    public Func<int?>? RuecklaufLesen { get; init; }
    public Action<int?>? RuecklaufSetzen { get; init; }

    public Func<double?>? SchwelleEinLesen { get; init; }
    public Action<double?>? SchwelleEinSetzen { get; init; }

    public Func<double?>? SchwelleAusLesen { get; init; }
    public Action<double?>? SchwelleAusSetzen { get; init; }

    public Func<double?>? SchwelleNachrangLesen { get; init; }
    public Action<double?>? SchwelleNachrangSetzen { get; init; }

    public Func<double?>? MindestfuellstandLesen { get; init; }
    public Action<double?>? MindestfuellstandSetzen { get; init; }

    public Func<int?>? SchichtenLesen { get; init; }
    public Action<int?>? SchichtenSetzen { get; init; }

    public Func<double?>? HoeheLesen { get; init; }
    public Action<double?>? HoeheSetzen { get; init; }

    public Func<double?>? LambdaLesen { get; init; }
    public Action<double?>? LambdaSetzen { get; init; }

    public Func<double?>? NutztemperaturBwLesen { get; init; }
    public Action<double?>? NutztemperaturBwSetzen { get; init; }

    public Func<double?>? LadeleistungLesen { get; init; }
    public Action<double?>? LadeleistungSetzen { get; init; }

    public Func<double?>? EntladeleistungLesen { get; init; }
    public Action<double?>? EntladeleistungSetzen { get; init; }

    public Func<int?>? EntladeprioritaetLesen { get; init; }
    public Action<int?>? EntladeprioritaetSetzen { get; init; }

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Name des Speichers — Pflichtangabe der Maske.</summary>
    public string Bezeichner
    {
        get => BezeichnerLesen?.Invoke() ?? "";
        set => BezeichnerSetzen?.Invoke(value ?? "");
    }

    /// <summary>Das Gesamtvolumen [l]; es muss größer als 0 sein.</summary>
    public int? Volumen
    {
        get => VolumenLesen?.Invoke();
        set => VolumenSetzen?.Invoke(value);
    }

    /// <summary>Die Bereitschaftsverluste [kWh/24 h]; leer gilt als 0.</summary>
    public double? Bereitschaftsverluste
    {
        get => VerlusteLesen?.Invoke();
        set => VerlusteSetzen?.Invoke(value);
    }

    /// <summary>Die Vorlauftemperatur des Speichers [°C].</summary>
    public int? Vorlauf
    {
        get => VorlaufLesen?.Invoke();
        set => VorlaufSetzen?.Invoke(value);
    }

    /// <summary>Die Rücklauftemperatur des Speichers [°C].</summary>
    public int? Ruecklauf
    {
        get => RuecklaufLesen?.Invoke();
        set => RuecklaufSetzen?.Invoke(value);
    }

    /// <summary>Die Einschaltschwelle der Ladung [%].</summary>
    public double? Einschaltschwelle
    {
        get => SchwelleEinLesen?.Invoke();
        set => SchwelleEinSetzen?.Invoke(value);
    }

    /// <summary>Die Abschaltschwelle der Ladung [%].</summary>
    public double? Abschaltschwelle
    {
        get => SchwelleAusLesen?.Invoke();
        set => SchwelleAusSetzen?.Invoke(value);
    }

    /// <summary>Die Schwelle, ab der nachrangige Erzeuger laden [%].</summary>
    public double? SchwelleNachrangig
    {
        get => SchwelleNachrangLesen?.Invoke();
        set => SchwelleNachrangSetzen?.Invoke(value);
    }

    /// <summary>Der Mindestfüllstand, der nicht entladen wird [%]; 0 ist gültig.</summary>
    public double? Mindestfuellstand
    {
        get => MindestfuellstandLesen?.Invoke();
        set => MindestfuellstandSetzen?.Invoke(value);
    }

    /// <summary>Die Zahl der Schichten des Schichtmodells; mindestens 1.</summary>
    public int? Schichten
    {
        get => SchichtenLesen?.Invoke();
        set => SchichtenSetzen?.Invoke(value);
    }

    /// <summary>Die Bauhöhe des Speichers [m]; leer heißt „Vorgabe gilt".</summary>
    public double? Hoehe
    {
        get => HoeheLesen?.Invoke();
        set => HoeheSetzen?.Invoke(value);
    }

    /// <summary>Die effektive Wärmeleitfähigkeit der Schichtung [W/(m·K)].</summary>
    public double? Lambda
    {
        get => LambdaLesen?.Invoke();
        set => LambdaSetzen?.Invoke(value);
    }

    /// <summary>Die Nutztemperatur des Brauchwassers [°C].</summary>
    public double? NutztemperaturBw
    {
        get => NutztemperaturBwLesen?.Invoke();
        set => NutztemperaturBwSetzen?.Invoke(value);
    }

    /// <summary>Die höchste Ladeleistung [kW]; leer heißt „unbegrenzt".</summary>
    public double? Ladeleistung
    {
        get => LadeleistungLesen?.Invoke();
        set => LadeleistungSetzen?.Invoke(value);
    }

    /// <summary>Die höchste Entladeleistung [kW]; leer heißt „unbegrenzt".</summary>
    public double? Entladeleistung
    {
        get => EntladeleistungLesen?.Invoke();
        set => EntladeleistungSetzen?.Invoke(value);
    }

    /// <summary>Die Entladepriorität 0…9; 0 heißt „automatisch".</summary>
    public int? Entladeprioritaet
    {
        get => EntladeprioritaetLesen?.Invoke();
        set => EntladeprioritaetSetzen?.Invoke(value);
    }
}
