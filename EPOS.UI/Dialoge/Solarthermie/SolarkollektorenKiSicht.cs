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
/// Felder sind dann leer und nehmen nichts an.</para>
/// </summary>
public sealed class SolarkollektorenKiSicht : IKiFeldtafel
{
    /// <summary>Der Arbeitsstand der Kollektorgruppe; <c>null</c> = keine Zeile gewählt.</summary>
    public Func<SolarkollektorenEingaben?>? Eingabenquelle { get; init; }

    /// <summary>Die Feldtafel des Aufklappers „Alle Daten".</summary>
    public AlleDatenTafel? AlleDaten { get; init; }

    private SolarkollektorenEingaben? Stand => Eingabenquelle?.Invoke();

    public int? Anzahl
    {
        get => Stand?.Anzahl;
        set { if (Stand is SolarkollektorenEingaben s) s.Anzahl = value; }
    }

    public int? Neigung
    {
        get => Stand?.Neigung;
        set { if (Stand is SolarkollektorenEingaben s) s.Neigung = value; }
    }

    public int? Azimut
    {
        get => Stand?.Azimut;
        set { if (Stand is SolarkollektorenEingaben s) s.Azimut = value; }
    }


    /// <inheritdoc/>
    public object? Lesen(string schluessel) => AlleDaten?.Lesen(schluessel);

    /// <inheritdoc/>
    public void Setzen(string schluessel, object? wert) => AlleDaten?.Setzen(schluessel, wert);
}
