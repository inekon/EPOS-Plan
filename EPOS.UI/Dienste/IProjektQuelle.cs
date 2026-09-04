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

    /// <summary>
    /// Der fertige PARAMETERSATZ der Simulationskonfiguration zu einem Projekt
    /// (iU9-W10b.1); <c>null</c> = geht gerade nicht. Die Seite bleibt dann in der
    /// Liste stehen.
    ///
    /// <para><b>Warum ein Woerterbuch und kein Datensatz.</b> Die Seite traegt ueber
    /// vierzig Parameter — Delegatensatz, Zustand und die Anzeigetexte. Sie als
    /// Woerterbuch zu liefern und mit <c>@@attributes</c> hineinzuschuetten, ist
    /// dasselbe Muster, mit dem jede Huelle ihre Unterdialoge fuellt (<c>Gaben</c>);
    /// eine eigene Klasse dafuer waere eine zweite Wahrheit ueber dieselbe Liste.</para>
    ///
    /// <para><b>Mit Standardumsetzung</b>, damit eine vorhandene Quelle
    /// (<c>EPOS.iOS/Dienste/IosProjektQuelle</c>) durch die Erweiterung nicht bricht:
    /// Wer sie nicht umsetzt, kennt die Seite eben nicht.</para>
    /// </summary>
    IReadOnlyDictionary<string, object>? SimulationKonfigGaben(int idProjekt) => null;

    /// <summary>
    /// Der fertige PARAMETERSATZ der Ergebnisseite zu einem Projekt
    /// (iU9-W11b.13); <c>null</c> = geht gerade nicht. Die Seite bleibt dann in
    /// der Liste stehen.
    ///
    /// <para>Dieselbe Form und derselbe Grund wie bei
    /// <see cref="SimulationKonfigGaben"/> — ein Woerterbuch, das die Wurzel mit
    /// <c>@@attributes</c> hineinschuettet, und eine Standardumsetzung, damit eine
    /// vorhandene Quelle durch die Erweiterung nicht bricht.</para>
    /// </summary>
    IReadOnlyDictionary<string, object>? SimulationErgebnisGaben(int idProjekt) => null;

    /// <summary>
    /// Der fertige PARAMETERSATZ des KI-Hilfe-Assistenten (iU9-W15b.7,
    /// Entscheid E-10).
    ///
    /// <para>Wie <see cref="SimulationKonfigGaben"/> ein Woerterbuch <b>mit
    /// Standardumsetzung</b>: Solange die iOS-Huelle den Assistenten nicht bedient
    /// (iU11), liefert sie <c>null</c>, und <c>AppWurzel</c> bleibt bei der Liste
    /// stehen — derselbe Ausgang wie „Dialog geht nicht auf" unter Windows. Der
    /// Assistent haengt an keinem PROJEKT; der Parameter bleibt der Form halber.</para>
    /// </summary>
    IReadOnlyDictionary<string, object>? KiAssistentGaben(int idProjekt) => null;

    /// <summary>
    /// Alles, was der Dialog „Projekt exportieren / importieren" braucht
    /// (iU9-W15a.0h); <c>null</c> = diese Huelle kann keinen Projekttransfer.
    ///
    /// <para>Derselbe Weg und derselbe Grund wie bei
    /// <see cref="SimulationKonfigGaben"/> — <b>mit Standardumsetzung</b>, damit eine
    /// vorhandene Quelle (<c>EPOS.iOS/Dienste/IosProjektQuelle</c>) durch die
    /// Erweiterung nicht bricht. Anders als dort ist das Ergebnis ein DATENSATZ und
    /// kein Woerterbuch: Es sind neun benannte Dinge, nicht vierzig, und drei davon
    /// sind Pfaddelegaten, deren Fehlen der Dialog SEHEN muss (kein Delegat = kein
    /// Knopf).</para>
    /// </summary>
    ProjektTransferDaten? TransferDaten() => null;

    /// <summary>
    /// Der fertige PARAMETERSATZ des PROJEKTASSISTENTEN (iU9-W16a.5, K6);
    /// <c>null</c> = diese Huelle kann keinen Assistentenlauf.
    ///
    /// <para>Derselbe Weg und derselbe Grund wie bei
    /// <see cref="SimulationKonfigGaben"/> — ein Woerterbuch, das die Wurzel mit
    /// <c>@@attributes</c> hineinschuettet, und eine Standardumsetzung, damit eine
    /// vorhandene Quelle (<c>EPOS.iOS/Dienste/IosProjektQuelle</c>) durch die
    /// Erweiterung nicht bricht.</para>
    ///
    /// <para><b>Zwei Angaben statt einer.</b> Der Assistent hat ZWEI Einstiege, die
    /// sich nur in der Betriebsart unterscheiden (neues Projekt / vorhandenes
    /// bearbeiten) — das war schon im Bestand so (<c>MenueCtrl.ProjektNeu</c> und
    /// <c>…ProjektBearbeiten</c> riefen dieselbe Seitenliste mit anderem
    /// <c>SetWizardMode</c>). Die Projekt-Id ist im Bearbeiten-Zweig die Vorauswahl
    /// des linken Bandes; 0 heisst „noch keine".</para>
    /// </summary>
    /// <param name="betriebsart">0 = neues Projekt, 1 = vorhandenes bearbeiten.</param>
    /// <param name="idProjekt">Vorausgewaehltes Projekt im Bearbeiten-Zweig; 0 = keines.</param>
    IReadOnlyDictionary<string, object>? AssistentGaben(int betriebsart, int idProjekt) => null;
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
