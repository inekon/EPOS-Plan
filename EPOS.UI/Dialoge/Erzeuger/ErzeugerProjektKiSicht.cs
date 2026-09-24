using EPOS.UI.Dienste;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Das Abbild der vier Erzeugermasken des Projekts, die eine Projektzeile führen —
/// Heizkessel, BHKW, Pufferspeicher und Stromspeicher (Welle #458, Stufe 2).
///
/// <para><b>Warum eine Sichtklasse.</b> Die benannten Felder stehen an der gewählten
/// <see cref="ErzeugerZeile"/>, und diese Klasse reicht sie unverändert durch — mit
/// denselben Namen, damit die Markup-Probe sie weiter an der Maske findet
/// (<c>_projektZeile.Vorlauf</c>). Dazu kommt der Aufklapper „Alle Daten": Er zeigt den
/// gewählten KATALOGsatz, nicht die Zeile, und führt seine Felder als Daten eines
/// Profils — beantwortet als <see cref="IKiFeldtafel"/> über <see cref="AlleDaten"/>.
/// Die Brücke kennt je Maske EIN Daten-Objekt; also trägt die Sicht beide Herkünfte
/// (Muster <see cref="PhotovoltaikKiSicht"/>).</para>
///
/// <para><b>Ohne gewählte Projektzeile</b> — auch, solange eine Katalogzeile gewählt
/// ist — sind die benannten Felder leer und nehmen nichts an, genau wie auf der Maske.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jeder Zugriff ruft die Delegaten des Dialogs.</para>
/// </summary>
public sealed class ErzeugerProjektKiSicht : IKiFeldtafel
{
    /// <summary>Die GEWÄHLTE Projektzeile; <c>null</c> = keine gewählt.</summary>
    public Func<ErzeugerZeile?>? Zeilenquelle { get; init; }

    /// <summary>Die Feldtafel des Aufklappers „Alle Daten".</summary>
    public AlleDatenTafel? AlleDaten { get; init; }

    private ErzeugerZeile? Zeile => Zeilenquelle?.Invoke();

    /// <summary>Der Name der gewählten Anlage (nur lesbar).</summary>
    public string? Bezeichner => Zeile?.Bezeichner;

    /// <summary>Vorlauftemperatur der Anlage [°C].</summary>
    public int? Vorlauf
    {
        get => Zeile?.Vorlauf;
        set { if (Zeile is ErzeugerZeile z) z.Vorlauf = value; }
    }

    /// <summary>Rücklauftemperatur der Anlage [°C].</summary>
    public int? Ruecklauf
    {
        get => Zeile?.Ruecklauf;
        set { if (Zeile is ErzeugerZeile z) z.Ruecklauf = value; }
    }

    /// <summary>Die zugeordnete Energieträgervariante (Wahlfeld).</summary>
    public int? CarrierId
    {
        get => Zeile?.CarrierId;
        set { if (Zeile is ErzeugerZeile z && value is int id) z.CarrierId = id; }
    }

    /// <summary>Untere Grenzleistung des BHKW [%]; 0 = Projektvorgabe.</summary>
    public double? Grenzleistung
    {
        get => Zeile?.Grenzleistung;
        set { if (Zeile is ErzeugerZeile z) z.Grenzleistung = value; }
    }

    /// <inheritdoc/>
    public object? Lesen(string schluessel) => AlleDaten?.Lesen(schluessel);

    /// <inheritdoc/>
    public void Setzen(string schluessel, object? wert) => AlleDaten?.Setzen(schluessel, wert);
}
