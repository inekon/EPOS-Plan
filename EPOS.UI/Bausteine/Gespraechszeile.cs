namespace EPOS.UI.Bausteine;

/// <summary>
/// Eine Zeile des <c>Gespraechsverlauf</c>s.
/// </summary>
/// <param name="Rolle">Bestimmt Farbe, Schnitt und Vorlesereihenfolge.</param>
/// <param name="Text">Der Inhalt. Zeilenumbrueche darin bleiben erhalten.</param>
/// <param name="Adresse">
/// Optionale Adresse; ist sie gesetzt, wird die Zeile zum Verweis.
///
/// <para><b>Warum eine Adresse und kein Erraten.</b> Der WinForms-Vorlaeufer
/// liess die <c>RichTextBox</c> Adressen selbst finden (<c>DetectUrls = true</c>)
/// und filterte erst beim Klick auf <c>http</c>/<c>https</c> — mit dem
/// ausdruecklichen Grund, dass „ein Antworttext des Modells in derselben Anzeige
/// landet, und der ist Fremdtext" (<c>Form_KiChat.cs:1546–1549</c>). Ein
/// HTML-Baustein darf gar nicht erst raten: Wer die Adresse kennt, gibt sie mit;
/// wer sie nicht mitgibt, bekommt Text. <c>QuellenZeigen</c> kannte sie ohnehin
/// (<c>a.QuellUrl</c>).</para>
/// </param>
/// <param name="Kennung">
/// Stabile Kennung fuer <c>@key</c> — ueblich eine fortlaufende Nummer der
/// Sitzung. Ohne sie baut Blazor bei jedem Anhaengen die ganze Liste neu auf.
/// </param>
public sealed record Gespraechszeile(
    Gespraechsrolle Rolle,
    string Text,
    string? Adresse = null,
    long Kennung = 0);
