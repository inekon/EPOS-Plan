namespace EPOS.UI.Dialoge.Solarthermie;

/// <summary>
/// Der ARBEITSSTAND der Kollektorgruppe im Projektdialog — die drei Zahlen, die
/// „Übernehmen" in die gewählte <c>ErzeugerZeile</c> schreibt. Vor- und Rücklauf führt
/// die Gruppe nicht: Sie hätten beim Solarkollektor keinen Rechenweg.
///
/// <para><b>Warum sie ein Objekt sind und keine drei Felder der Komponente.</b> Der
/// Dialog schreibt die Zeile erst beim Knopf „Übernehmen"; bis dahin führt er die
/// Eingaben für sich (Hausregel „Geschrieben wird im OK-Weg"). Genau dieser Stand ist
/// das, was der Anwender sieht — und damit das, was der Hilfe-Assistent lesen und
/// setzen muss (<c>KiDialoge.SolarkollektorenProjekt</c>). Läge er in der gewählten
/// Zeile, zeigte die Maske nach einer Feldsetzung weiter die alte Zahl, und das
/// nächste „Übernehmen" überschriebe die neue wortlos.</para>
///
/// <para><b>Er wird AN ORT UND STELLE gefüllt</b>, nicht ersetzt: Ein Zeilenwechsel
/// schreibt die drei Eigenschaften neu, das Objekt bleibt dasselbe. So zeigt der
/// Getter der Maskenanmeldung immer auf den lebenden Stand.</para>
/// </summary>
public sealed class SolarkollektorenEingaben
{
    /// <summary>Modulanzahl; <c>null</c> = leeres Feld (gilt als 0).</summary>
    public int? Anzahl { get; set; }

    /// <summary>Modulneigung [°]; <c>null</c> = leeres Feld.</summary>
    public int? Neigung { get; set; }

    /// <summary>Azimut [°]; <c>null</c> = leeres Feld.</summary>
    public int? Azimut { get; set; }
}
