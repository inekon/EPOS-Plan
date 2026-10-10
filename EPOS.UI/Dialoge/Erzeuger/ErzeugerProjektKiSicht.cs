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
/// ist — sind die benannten Felder leer. Ein Setzen lehnt dann BENANNT ab
/// (<c>KI_ERZ_KEINE_PROJEKTZEILE</c>, als Ausnahme des Setzers, die <c>feld_setzen</c> in
/// seine Absage „ließ sich nicht setzen" hebt) und verwirft den Wert nie still. Trägt das
/// Projekt genau eine Anlage, wählt <see cref="Einzelwahl"/> sie wie das Öffnen des
/// Dialogs.</para>
///
/// <para><b>Nach dem Setzen geht die Zeile den Weg der Hand</b>: <see cref="Uebernommen"/>
/// ist derselbe Übernahmeweg, den die Eingabefelder rufen (<c>Uebernehmen</c> des Dialogs,
/// in die Arbeitskopie der Hülle — nicht in die Datenbank).</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jeder Zugriff ruft die Delegaten des Dialogs.</para>
/// </summary>
public sealed class ErzeugerProjektKiSicht : IKiFeldtafel
{
    /// <summary>Die GEWÄHLTE Projektzeile; <c>null</c> = keine gewählt.</summary>
    public Func<ErzeugerZeile?>? Zeilenquelle { get; init; }

    /// <summary>
    /// Wählt die EINZIGE Projektzeile, wenn keine gewählt ist, und liefert sie;
    /// <c>null</c> = keine oder mehrere Zeilen (dann wird nicht geraten).
    /// </summary>
    public Func<ErzeugerZeile?>? Einzelwahl { get; init; }

    /// <summary>
    /// Der Übernahmeweg der Hand nach dem Setzen eines Zeilenfelds (Vorlauf, Rücklauf,
    /// Grenzleistung) — der Dialog reicht hier sein <c>Uebernehmen</c> herein.
    /// </summary>
    public Action<ErzeugerZeile>? Uebernommen { get; init; }

    /// <summary>Die Feldtafel des Aufklappers „Alle Daten".</summary>
    public AlleDatenTafel? AlleDaten { get; init; }

    private ErzeugerZeile? Zeile => Zeilenquelle?.Invoke();

    /// <summary>Der Name der gewählten Anlage (nur lesbar).</summary>
    public string? Bezeichner => Zeile?.Bezeichner;

    /// <summary>Vorlauftemperatur der Anlage [°C].</summary>
    public int? Vorlauf
    {
        get => Zeile?.Vorlauf;
        set => Schreibe(z => { z.Vorlauf = value; z.TemperaturHerleitung = ""; }, uebernehmen: true);
    }

    /// <summary>Rücklauftemperatur der Anlage [°C].</summary>
    public int? Ruecklauf
    {
        get => Zeile?.Ruecklauf;
        set => Schreibe(z => { z.Ruecklauf = value; z.TemperaturHerleitung = ""; }, uebernehmen: true);
    }

    /// <summary>
    /// Die zugeordnete Energieträgervariante (Wahlfeld). Sie geht NICHT über
    /// <see cref="Uebernommen"/>: Den Träger hängt von Hand ein eigener Weg um, der sofort
    /// schreibt (<c>TraegerWechseln</c>) — den nimmt <c>feld_setzen</c> nicht.
    /// </summary>
    public int? CarrierId
    {
        get => Zeile?.CarrierId;
        set => Schreibe(z => { if (value is int id) z.CarrierId = id; }, uebernehmen: false);
    }

    /// <summary>Untere Grenzleistung des BHKW [%]; 0 = Projektvorgabe.</summary>
    public double? Grenzleistung
    {
        get => Zeile?.Grenzleistung;
        set => Schreibe(z => z.Grenzleistung = value, uebernehmen: true);
    }

    /// <summary>
    /// Schreibt in die gewählte Zeile — oder in die einzige, die <see cref="Einzelwahl"/>
    /// wählt — und geht danach den Übernahmeweg; ohne Zeile eine benannte Absage.
    /// </summary>
    private void Schreibe(Action<ErzeugerZeile> schreiben, bool uebernehmen)
    {
        ErzeugerZeile z = Zeile ?? Einzelwahl?.Invoke()
            ?? throw new InvalidOperationException(
                   WindowsFormsApplication1.MyResource.Resource.KI_ERZ_KEINE_PROJEKTZEILE);
        schreiben(z);
        if (uebernehmen) Uebernommen?.Invoke(z);
    }

    /// <inheritdoc/>
    public object? Lesen(string schluessel) => AlleDaten?.Lesen(schluessel);

    /// <inheritdoc/>
    public void Setzen(string schluessel, object? wert) => AlleDaten?.Setzen(schluessel, wert);
}
