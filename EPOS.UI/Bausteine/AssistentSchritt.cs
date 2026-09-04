using Microsoft.AspNetCore.Components;

namespace EPOS.UI.Bausteine;

/// <summary>
/// EIN Schritt des Assistenten (iU9-W16a.5) — Titel, Inhalt und Schaltzustand.
///
/// <para><b>Der Vorläufer war eine <c>Form</c>.</b> <c>WizardSeite</c> trug ein
/// fertig gebautes Fensterobjekt, das <c>WizardParent.LoadNewForm</c> mit
/// <c>TopLevel = false</c> in sein Inhaltspanel steckte — samt 32 Zeilen
/// Größenrechnung, weil eine eingebettete <c>Form</c> keine Wunschgröße meldet.
/// Hier ist ein Schritt ein <see cref="RenderFragment"/>: Der Rahmen zeigt eines
/// davon, und das Maß macht das CSS.</para>
///
/// <para><b><see cref="Aktiv"/> ist die Freischaltung</b>, nicht die Sichtbarkeit:
/// Der Komponentenschritt schaltet mit jeder Kachel eine Fachseite frei oder ab,
/// und „Weiter"/„Zurück" überspringen die abgeschalteten.</para>
/// </summary>
public sealed class AssistentSchritt
{
    /// <summary>Beschriftung des Schrittes im Seitenband.</summary>
    public string Titel { get; set; } = "";

    /// <summary>Was der Schritt zeigt.</summary>
    public RenderFragment? Inhalt { get; set; }

    /// <summary>Ist der Schritt freigeschaltet?</summary>
    public bool Aktiv { get; set; }
}
