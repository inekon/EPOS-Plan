using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;

namespace EPOS.UI.Dialoge.Solarthermie;

/// <summary>
/// Das Abbild der Solarkollektoren im Projekt (Welle #458, Stufe 2): die drei Zahlen des
/// ARBEITSSTANDES der Kollektorgruppe und der Aufklapper „Alle Daten".
///
/// <para><b>Warum eine Sichtklasse.</b> Die drei Zahlen stehen am Arbeitsstand
/// <see cref="SolarkollektorenEingaben"/>, den „Übernehmen" in die Zeile trägt; diese
/// Klasse reicht sie mit denselben Namen durch. Der Aufklapper zeigt dagegen den
/// gewählten KATALOGsatz über ein Profil — beantwortet als <see cref="IKiFeldtafel"/>
/// (<see cref="AlleDatenTafel"/>). Die Brücke kennt je Maske EIN Daten-Objekt.</para>
///
/// <para><b>Ohne gewählte Projektzeile</b> gibt es die Kollektorgruppe nicht; die drei
/// Felder sind dann leer, und ein Setzen lehnt benannt ab.</para>
/// </summary>
public sealed class SolarkollektorenKiSicht : IKiFeldtafel
{
    /// <summary>Der Arbeitsstand der Kollektorgruppe; <c>null</c> = keine Zeile gewählt.</summary>
    public Func<SolarkollektorenEingaben?>? Eingabenquelle { get; init; }

    /// <summary>Die Feldtafel des Aufklappers „Alle Daten".</summary>
    public AlleDatenTafel? AlleDaten { get; init; }

    private SolarkollektorenEingaben? Stand => Eingabenquelle?.Invoke();

    /// <summary>
    /// Wählt die EINZIGE Projektzeile, wenn keine gewählt ist, und liefert ihren
    /// Arbeitsstand; <c>null</c> = keine oder mehrere Zeilen (dann wird nicht geraten).
    /// </summary>
    public Func<SolarkollektorenEingaben?>? Einzelwahl { get; init; }

    /// <summary>
    /// Die Zahl der Projektzeilen — sie sagt dem <see cref="Sperrgrund"/>, ob die
    /// <see cref="Einzelwahl"/> greift (genau eine), ohne dass er schon wählt.
    /// </summary>
    public Func<int>? Zeilenzahl { get; init; }

    /// <summary>
    /// Der Sperrgrund je Feld (<c>KiMaskenhaken.Sperrgrund</c>, <see cref="ErzeugerSperre"/>):
    /// die Felder der Kollektorgruppe ohne gewählte Zeile, wenn nicht genau eine vorhanden
    /// ist — vor der Bestätigung statt erst beim Setzen.
    /// </summary>
    public string? Sperrgrund(string feld)
        => ErzeugerSperre.Grund(feld, Stand is not null, Zeilenzahl, mitTraeger: false);

    /// <summary>
    /// Schreibt in den Arbeitsstand der gewählten Zeile — wie die Eingabefelder, die
    /// erst „Übernehmen" in die Zeile trägt; ohne gewählte Zeile eine BENANNTE Absage
    /// statt eines still verworfenen Werts (Meldung 10.10.2026).
    /// </summary>
    private void Schreibe(Action<SolarkollektorenEingaben> schreiben)
    {
        SolarkollektorenEingaben s = Stand ?? Einzelwahl?.Invoke()
            ?? throw new InvalidOperationException(
                   WindowsFormsApplication1.MyResource.Resource.KI_ERZ_KEINE_PROJEKTZEILE);
        schreiben(s);
    }

    public int? Anzahl
    {
        get => Stand?.Anzahl;
        set => Schreibe(s => s.Anzahl = value);
    }

    public int? Neigung
    {
        get => Stand?.Neigung;
        set => Schreibe(s => s.Neigung = value);
    }

    public int? Azimut
    {
        get => Stand?.Azimut;
        set => Schreibe(s => s.Azimut = value);
    }

    /// <summary>Bodenalbedo vor dem Kollektorfeld (0…1); <c>null</c> = 0,2.</summary>
    public double? Albedo
    {
        get => Stand?.Albedo;
        set => Schreibe(s => s.Albedo = value);
    }

    // --- Solarkreis (Welle M2) ---------------------------------------------------------

    public double? PumpenleistungW
    {
        get => Stand?.PumpenleistungW;
        set => Schreibe(s => s.PumpenleistungW = value);
    }

    public double? VerlusteProzent
    {
        get => Stand?.VerlusteProzent;
        set => Schreibe(s => s.VerlusteProzent = value);
    }

    public double? GraedigkeitK
    {
        get => Stand?.GraedigkeitK;
        set => Schreibe(s => s.GraedigkeitK = value);
    }

    public double? SpreizungK
    {
        get => Stand?.SpreizungK;
        set => Schreibe(s => s.SpreizungK = value);
    }

    public bool ArbeitstemperaturAusSpeicher
    {
        get => Stand?.ArbeitstemperaturAusSpeicher ?? false;
        set => Schreibe(s => s.ArbeitstemperaturAusSpeicher = value);
    }


    /// <inheritdoc/>
    public object? Lesen(string schluessel) => AlleDaten?.Lesen(schluessel);

    /// <inheritdoc/>
    public void Setzen(string schluessel, object? wert) => AlleDaten?.Setzen(schluessel, wert);
}
