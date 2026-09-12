namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Ein Abschnitt des Rechtshinweises: eine Ueberschrift und ein Absatz.
/// </summary>
/// <remarks>
/// Der Vorlaeufer <c>Form_KiHinweis</c> schrieb die sieben Abschnitte mit einer
/// Hilfsmethode <c>Abschnitt(ueberschrift, inhalt)</c> nacheinander in eine
/// <c>RichTextBox</c> (<c>:209-238</c>). Genau diese Paare stehen hier als Liste -
/// die Reihenfolge ist Teil der Aussage: erst was uebertragen wird, dann was im
/// Aktionsbetrieb dazukommt, dann was NICHT hinausgeht, dann Empfaenger,
/// Anwenderpflichten, Verantwortung und der Abschalter.
/// </remarks>
/// <param name="Ueberschrift">Die Abschnittsueberschrift (<c>KI_HINWEIS_UEB_*</c>).</param>
/// <param name="Inhalt">Der Absatz darunter (<c>KI_HINWEIS_*</c>).</param>
public sealed record KiHinweisAbschnitt(string Ueberschrift, string Inhalt);
