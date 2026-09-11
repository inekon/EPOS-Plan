using WindowsFormsApplication1;

namespace EPOS.UI.Dienste;

/// <summary>
/// Der EINE Weg aus einer Maske in den Hilfe-Assistenten (Auftrag #199, Stufe S1).
///
/// <para><b>Wozu ein Helfer und nicht zwei Zeilen in der Komponente.</b> Der Weg besteht
/// aus ZWEI Schritten, die zusammengehören: Der Aufruf muss im Kern gemeldet werden
/// (<see cref="KiChatKontext.AufrufMelden"/> — sonst weiss der Chat nichts von seinem
/// Bereich, und auf iOS bliebe es bei „Unbekannter Bereich"), und DANN wird die Maske
/// geöffnet. Stünden die zwei Schritte im <c>InfoKnopf</c> und noch einmal im
/// <c>Warnbanner</c>, liefe die Reihenfolge beim ersten Umbau auseinander.</para>
///
/// <para><b>Er kennt keine Plattform.</b> <c>Dienste.Navigation</c> entscheidet, was ein
/// Maskenschlüssel bedeutet: Unter Windows öffnet <c>WinFormsNavigation</c> die
/// nicht-modale <c>KiChatHuelle</c> mit diesem Kontext, auf iOS wechselt die
/// <c>AppWurzel</c> die Ansicht und kehrt danach zurück. Ohne belegte Navigation
/// (Prüfstand, Konsolenlauf) läuft der Aufruf leer und liefert <c>false</c> — derselbe
/// Ausgang wie bei jedem anderen Maskenschlüssel.</para>
/// </summary>
public static class KiAssistentWeg
{
    /// <summary>
    /// Ist der Assistent auf dieser Installation grundsätzlich möglich? Die Auskunft
    /// kommt aus dem Kern (<see cref="KiVerfuegbarkeit"/>); Netz, Schlüssel und
    /// Einwilligung sind ausdrücklich keine Bedingung (KI‑D‑Q1).
    /// </summary>
    public static bool Moeglich => KiVerfuegbarkeit.Moeglich;

    /// <summary>
    /// Meldet den Aufruf im Kern und öffnet den Assistenten.
    /// </summary>
    /// <param name="kontext">Bereich, Dialogname, Hilfeschlüssel, Frage, Kennung.</param>
    /// <returns><c>false</c>, wenn keine Oberfläche den Schlüssel kennt.</returns>
    public static bool Oeffnen(KiAufrufkontext kontext)
    {
        if (kontext is null) return false;

        KiChatKontext.AufrufMelden(kontext);
        return WindowsFormsApplication1.Dienste.Navigation
                   .OeffneMaske(Masken.KiAssistent, kontext);
    }

    /// <summary>
    /// Der Weg aus einem DIALOGKOPF (Weg 1): Der Bereich folgt aus dem Hilfeschlüssel,
    /// eine Frage wird nicht vorbelegt — der Anwender hat noch keine gestellt.
    /// </summary>
    public static bool AusDialog(string hilfeschluessel, string? dialogname = null)
        => Oeffnen(KiAufrufkontext.AusHilfeschluessel(hilfeschluessel, dialogname));

    /// <summary>
    /// Der Weg von einem BANNER (Weg 2): mit Meldungskennung und vorbelegter Frage.
    /// Abgeschickt wird die Frage nicht — sie steht in der Eingabezeile.
    /// </summary>
    public static bool AusMeldung(string kennung, string frage,
                                  string? hilfeschluessel = null, string? dialogname = null)
        => Oeffnen(KiAufrufkontext.AusHilfeschluessel(hilfeschluessel, dialogname, frage, kennung));
}
