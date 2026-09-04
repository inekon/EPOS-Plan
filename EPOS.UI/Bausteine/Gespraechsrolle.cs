namespace EPOS.UI.Bausteine;

/// <summary>
/// Die Rolle einer Zeile im <c>Gespraechsverlauf</c> — sie bestimmt Farbe,
/// Schriftschnitt und Vorlesereihenfolge.
///
/// <para><b>Woher die zehn Rollen kommen.</b> Der WinForms-Vorlaeufer
/// <c>Form_KiChat</c> hatte GENAU EINE Ausgabestelle: <c>SchreibeZeile(text,
/// farbe, fett)</c> (<c>Form_KiChat.cs:1593</c>). Ueber die ganze Maske hinweg
/// bekam sie acht verschiedene Farben und zwei Schriftschnitte — mehr
/// Ausgabewortschatz gab es nicht. Genau diese acht Farben sind hier zu Rollen
/// geworden, dazu die Leerzeile als Absatztrenner und die getrennte Rolle
/// „AssistentKopf", weil „Assistent:" fett und der Antworttext darunter normal
/// gesetzt ist.</para>
///
/// <para><b>Warum Rollen und nicht Farben.</b> Eine Farbe ist eine Entscheidung
/// der Darstellung; sie gehoert in <c>epos-ui.css</c> und darf sich je Plattform
/// und je Kontrastmodus unterscheiden. Was der Aufrufer weiss, ist die BEDEUTUNG
/// der Zeile — dieselbe Trennung wie bei <c>WarnStufe</c>.</para>
/// </summary>
public enum Gespraechsrolle
{
    /// <summary>Die Frage des Anwenders („Sie: …") — gruen, fett.</summary>
    Anwender,

    /// <summary>Der Antworttext des Modells — schwarz, normal.</summary>
    Assistent,

    /// <summary>Die Zeile „Assistent:" ueber der Antwort — blau, fett.</summary>
    AssistentKopf,

    /// <summary>Eine Zwischenueberschrift („Gefundene Hilfeabschnitte:") — blau, fett.</summary>
    Ueberschrift,

    /// <summary>Quellen, Tageszaehler, Cache-Vermerk, Ergebniszeilen, Protokollzeile — grau.</summary>
    Leise,

    /// <summary>Ausgefuehrt, Bestaetigung erteilt, „gespeichert" — gruen.</summary>
    Erfolg,

    /// <summary>Hinweise, Verfall, „kein Schluessel hinterlegt" — orange.</summary>
    Warnung,

    /// <summary>Fehler, abgelehnt, nicht ausgefuehrt — rot.</summary>
    Fehler,

    /// <summary>Der Titel des Bestaetigungsblocks — dunkelocker, fett.</summary>
    Bestaetigung,

    /// <summary>Ein Absatztrenner ohne Text.</summary>
    Leerzeile
}
