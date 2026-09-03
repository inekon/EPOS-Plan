using EPOS.UI.Dialoge.Kosten;
using WindowsFormsApplication1;

namespace EPOS.UI.Dienste;

/// <summary>
/// Eine Zeile der Projektliste - genau das, was der Einstieg anzeigt.
///
/// <para>Die Werte stammen aus <c>Tab_Projekt</c> und werden von der Huelle
/// geladen; die Komponente sieht keine Datenbank (Hausregel EPOS.UI).</para>
/// </summary>
/// <param name="Id"><c>Tab_Projekt.ID</c>.</param>
/// <param name="Name">Projektname - der fuehrende Schluessel des Bestands.</param>
/// <param name="Klimazone">Klimaregion des Projekts; leer, wenn unbekannt.</param>
/// <param name="Ausstattung">Kurzform der belegten Gewerke, z. B. "WP+BHKW+PV".</param>
public sealed record ProjektZeile(int Id, string Name, string Klimazone, string Ausstattung);

/// <summary>
/// Der fertig geladene Parametersatz des Dialogs „BHKW-Wirtschaftlichkeit".
///
/// <para><b>Warum ein Buendel und keine acht Einzelaufrufe.</b> Die
/// WinForms-Huelle <c>Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle</c>
/// laedt genau diese Dinge in einem Zug, bevor sie den Dialog oeffnet -
/// Parametersatz, Anlagen, Erzeugerkennzeichen, Doppelpflegepruefung,
/// Gesetzeskatalog und den Schreibweg. Dasselbe Buendel reicht die iOS-Huelle
/// herein; die Reihenfolge der Ladeschritte bleibt damit an EINER Stelle und
/// nicht in der Oberflaeche.</para>
///
/// <para><see cref="Anlagen"/> und <see cref="Parameter"/> werden vom Dialog
/// AN ORT UND STELLE fortgeschrieben - genau wie unter Windows; deshalb sind es
/// veraenderliche Objekte und keine Kopien.</para>
/// </summary>
/// <param name="IdStamm">Id des Stammprojekts der Vergleichsgruppe.</param>
/// <param name="StammName">Anzeigename des Stammprojekts.</param>
/// <param name="Anlagen">Die BHKW-Anlagen der Gruppe (<c>KwkgAnlagenCtrl.LadeGruppe</c>).</param>
/// <param name="Parameter">Der Parametersatz (<c>WirtschaftlichkeitCtrl.LadeParameter</c>).</param>
/// <param name="HatHeizkessel">true, wenn die Gruppe einen Heizkessel fuehrt.</param>
/// <param name="Doppelpflege">Die laufunabhaengige Kohaerenzpruefung.</param>
/// <param name="Katalog">Lesefassade auf <c>Tab_Gesetzesparameter</c>; <c>null</c> = Rueckfallwerte.</param>
/// <param name="ErgebnisseLaden">Der gebuchte Ergebnisstand aus der Datenbank.</param>
/// <param name="Speichern">Schreibt den Bildschirmzustand fort; Rueckgabe = Zahl der gescheiterten Saetze.</param>
public sealed record BhkwDialogDaten(
    int IdStamm,
    string StammName,
    IList<KwkgAnlagenAngabe> Anlagen,
    WirtschaftlichkeitParameter Parameter,
    bool HatHeizkessel,
    IReadOnlyList<KohaerenzHinweis> Doppelpflege,
    Func<string, int, GesetzParameter>? Katalog,
    Func<IReadOnlyList<int>, IReadOnlyList<WirtschaftlichkeitErgebnis>>? ErgebnisseLaden,
    Func<int>? Speichern);

/// <summary>
/// Die zweite Aussenschnittstelle von EPOS.UI neben <see cref="IHilfeDienst"/>:
/// alles, was die Seiten dieser Bibliothek an DATEN brauchen.
///
/// <para><b>Warum es sie gibt.</b> Die Hausregel lautet „keine Datenbank in
/// EPOS.UI" - eine Komponente bekommt ihre Daten als <c>[Parameter]</c> oder
/// ueber einen hier eingetragenen Dienst (dasselbe Muster, das
/// <c>WindowsFormsApplication1/Allgemein/Blazor/BlazorDienste.cs</c> im
/// Klassenkopf beschreibt). Ein einzelner Dialog kommt mit Parametern aus. Die
/// SEITE dagegen - die Projektliste des iPads - muss nachladen koennen, wenn
/// der Anwender einen Dialog geschlossen hat; sie braucht deshalb einen
/// Dienst und keinen einmaligen Parametersatz.</para>
///
/// <para><b>Wer sie bedient.</b> Auf iOS <c>EPOS.iOS/Dienste/IosProjektQuelle</c>
/// (iU10-7), im Test <see cref="KeineProjekte"/>. Eine Windows-Fassung gibt es
/// nicht: Dort ist die Startmaske der Einstieg, nicht diese Seite.</para>
///
/// <para><b>Alle Methoden sind SYNCHRON.</b> Der Rechenkern ist es auch; ein
/// <c>async</c> hier wuerde nur eine Nebenlaeufigkeit vortaeuschen, die die
/// Zugriffsschicht nicht hat. Die Huelle ruft sie deshalb dort, wo Blazor
/// ohnehin auf dem Hauptfaden steht.</para>
/// </summary>
public interface IProjektQuelle
{
    /// <summary>
    /// Die Projekte der Datenbank in Anzeigereihenfolge. Leere Liste = keine
    /// Datenbank oder kein Projekt; die Seite zeigt dann ihren Leertext.
    /// </summary>
    IReadOnlyList<ProjektZeile> Projekte();

    /// <summary>
    /// Die waehlbaren Energietraeger fuer den Dialog „Energietraeger anlegen"
    /// (<c>EnergietraegerVarianteCtrl.Energietraeger()</c>).
    /// </summary>
    IReadOnlyList<(int Id, string Name)> Energietraeger();

    /// <summary>
    /// Uebernimmt das Ergebnis des Dialogs „Energietraeger anlegen" - Katalogsuche,
    /// Anlegen und Projektzuordnung, genau der Weg, den
    /// <c>Views/Kosten/Form_Kosten.CreateNewEnergyCarrier</c> unter Windows nach dem
    /// Schliessen geht.
    /// </summary>
    /// <returns>Der Name der angelegten Variante; <c>""</c>, wenn nichts angelegt wurde.</returns>
    string EnergietraegerUebernehmen(int idProjekt, EnergietraegerVarianteErgebnis ergebnis);

    /// <summary>
    /// Der fertig geladene Parametersatz des BHKW-Dialogs zu einem Projekt;
    /// <c>null</c>, wenn er sich nicht laden laesst (kein Stammprojekt, keine
    /// Datenbank). Die Seite bleibt dann in der Liste stehen.
    /// </summary>
    BhkwDialogDaten? BhkwDaten(int idProjekt);
}

/// <summary>
/// Projektquelle, die nichts kennt.
///
/// <para>Gegenstueck zu <see cref="KeineHilfe"/>: Tests und Huellen ohne
/// Datenbank sollen die Seiten zeichnen koennen, ohne einen Kern-Controller zu
/// stellen. Leere Listen und <c>null</c> sind dabei keine Fehlerzustaende,
/// sondern der Zustand „noch keine Daten" - genau den zeigt die Seite an.</para>
/// </summary>
public sealed class KeineProjekte : IProjektQuelle
{
    /// <inheritdoc />
    public IReadOnlyList<ProjektZeile> Projekte() => Array.Empty<ProjektZeile>();

    /// <inheritdoc />
    public IReadOnlyList<(int Id, string Name)> Energietraeger() => Array.Empty<(int, string)>();

    /// <inheritdoc />
    public string EnergietraegerUebernehmen(int idProjekt, EnergietraegerVarianteErgebnis ergebnis) => "";

    /// <inheritdoc />
    public BhkwDialogDaten? BhkwDaten(int idProjekt) => null;
}
