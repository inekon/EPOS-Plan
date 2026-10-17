using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Eine Zeile der Vorschau</b> „Nutzungsprofile aus der Projektdatei" (Konzept Nutzungsprofile Q46, Stufe NP4b): DIN-Nummer,
/// Name, die Werte in Kurzform, wie viele Zonen der Datei sie tragen und ob die Nummer schon in der Kategorie steht.
/// </summary>
public sealed record ProjektdateiProfilZeile(string Nummer, string Bezeichner, string Werte, int Zonen, bool Vorhanden);

/// <summary>
/// <b>Die Vorschau vor der Übernahme</b> (NP4b): Datei, Zielkategorie „Projektdatei &lt;Datei&gt;", ob sie schon besteht
/// (dann fragt die Komponente „Ergänzen | Ersetzen"), die Zeilen und die Meldungen der Abbildung — oder die benannte Ablehnung.
/// </summary>
public sealed record ProjektdateiProfileVorschau(string Dateiname, string Kategorie, bool KategorieVorhanden,
                                                 IReadOnlyList<ProjektdateiProfilZeile> Zeilen, IReadOnlyList<string> Meldungen,
                                                 string? Ablehnung = null)
{
    /// <summary>Ist die Datei abgelehnt (keine Projektdatei, zu groß, nicht lesbar)?</summary>
    public bool Abgelehnt => Ablehnung is not null;

    /// <summary>Wie viele Nummern stehen noch nicht in der Kategorie?</summary>
    public int Neue => Zeilen.Count(z => !z.Vorhanden);
}

/// <summary>Was die Übernahme ergab: <c>Ok</c> mit der Ergebniszeile und den Hinweisen, sonst der Grund.</summary>
public sealed record ProjektdateiProfileErgebnis(bool Ok, string Meldung, IReadOnlyList<string> Meldungen)
{
    /// <summary>Die benannte Ablehnung; nichts geschrieben.</summary>
    public static ProjektdateiProfileErgebnis Fehler(string meldung) => new(false, meldung ?? "", Array.Empty<string>());
}

/// <summary>Die Texte der Komponente <c>ProjektdateiProfile</c> (Ressourcen <c>RNP_PD_*</c>, belegt von der Hülle).</summary>
public sealed class ProjektdateiProfileTexte
{
    /// <summary>Der Kopfknopf im Blatt „Nutzungsprofile" (<c>RNP_PD_BTN_BLATT</c>).</summary>
    public string KnopfBlatt { get; set; } = "Aus Projektdatei…";

    /// <summary>Der Knopf im Gebäudeimport (<c>RNP_PD_BTN_IMPORT</c>).</summary>
    public string KnopfImport { get; set; } = "Nutzungsprofile der Datei übernehmen…";

    /// <summary>Der Tooltip beider Knöpfe (<c>RNP_PD_TOOLTIP</c>).</summary>
    public string Tooltip { get; set; } = "";

    /// <summary>Die Überschrift der Vorschau mit Datei {0} und Kategorie {1} (<c>RNP_PD_UEBERSCHRIFT</c>).</summary>
    public string Ueberschrift { get; set; } = "Nutzungsprofile aus {0} → Kategorie „{1}“";

    /// <summary>Spaltenkopf Nummer.</summary>
    public string SpalteNummer { get; set; } = "Nr.";

    /// <summary>Spaltenkopf Name.</summary>
    public string SpalteName { get; set; } = "Name";

    /// <summary>Spaltenkopf Werte.</summary>
    public string SpalteWerte { get; set; } = "Werte";

    /// <summary>Spaltenkopf Zonen.</summary>
    public string SpalteZonen { get; set; } = "Zonen";

    /// <summary>Spaltenkopf Stand.</summary>
    public string SpalteStand { get; set; } = "Stand";

    /// <summary>Stand „neu“.</summary>
    public string StandNeu { get; set; } = "neu";

    /// <summary>Stand „vorhanden“.</summary>
    public string StandVorhanden { get; set; } = "vorhanden";

    /// <summary>Die Rückfrage bei vorhandener Kategorie {0} (<c>RNP_PD_KATEGORIE_VORHANDEN</c>).</summary>
    public string KategorieVorhanden { get; set; } = "Die Kategorie „{0}“ besteht schon.";

    /// <summary>„Übernehmen“.</summary>
    public string KnopfUebernehmen { get; set; } = "Übernehmen";

    /// <summary>„Ergänzen“.</summary>
    public string KnopfErgaenzen { get; set; } = "Ergänzen";

    /// <summary>„Ersetzen“.</summary>
    public string KnopfErsetzen { get; set; } = "Ersetzen";

    /// <summary>„Schließen“.</summary>
    public string KnopfSchliessen { get; set; } = "Schließen";

    /// <summary>Grund der Sperre ohne Profile.</summary>
    public string GrundKeine { get; set; } = "";

    /// <summary>Grund der Sperre von „Ergänzen“, wenn alle Nummern schon da sind.</summary>
    public string GrundAlleDa { get; set; } = "";

    /// <summary>Grund der Sperre, solange gelesen oder geschrieben wird.</summary>
    public string GrundBeschaeftigt { get; set; } = "";

    /// <summary>Der Hinweis nach der Übernahme: Die Zuordnung bleibt, wie sie ist (<c>RNP_PD_HINWEIS_ZUORDNUNG</c>).</summary>
    public string HinweisZuordnung { get; set; } = "";

    /// <summary>Der Hinweis zur Herkunft der Werte (<c>RNP_PD_HINWEIS_HERKUNFT</c>).</summary>
    public string HinweisHerkunft { get; set; } = "";

    /// <summary>Die Überschrift der Hinweisliste.</summary>
    public string Meldungen { get; set; } = "Hinweise";
}

/// <summary>
/// <b>Der Weg der Komponente <c>ProjektdateiProfile</c></b> (NP4b): <see cref="Laden"/> liest die Projektdatei — im Blatt
/// über die Dateiwahl der Plattform, im Gebäudeimport die schon geladene Datei — und liefert die Vorschau (<c>null</c> =
/// abgebrochen); <see cref="Uebernehmen"/> schreibt sie (<c>true</c> = ersetzen). Ohne Delegat kein Knopf.
/// </summary>
public sealed class ProjektdateiProfileWeg
{
    /// <summary>Liest und bildet ab; <c>null</c> = abgebrochen.</summary>
    public Func<Task<ProjektdateiProfileVorschau?>>? Laden { get; init; }

    /// <summary>Schreibt die zuletzt geladene Vorschau in die Kategorie; das Argument heißt „ersetzen“.</summary>
    public Func<bool, ProjektdateiProfileErgebnis>? Uebernehmen { get; init; }

    /// <summary>Die Texte.</summary>
    public ProjektdateiProfileTexte Texte { get; init; } = new();
}
