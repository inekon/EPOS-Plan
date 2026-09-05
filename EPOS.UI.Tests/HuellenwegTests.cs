using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die WACHE gegen ein modales Systemfenster IM Blazor-Ereignis — Anwenderbefund
/// <b>W15b‑B‑1</b> der Windows-Abnahme vom 05.09.2026 („Einstellungen… öffnet ein
/// leeres Fenster, dann stürzt die Anwendung ab"), dieselbe Sache wie
/// <b>W13‑B‑1</b> (Dateiwähler) und <b>W16b‑B‑1</b> (leere Startkachel-Dialoge).
///
/// <para><b>Worum es geht.</b> Eine Hülle reicht einer Razor-Komponente Delegaten
/// herein. Was die Komponente daraus ruft, läuft im
/// <c>WebMessageReceived</c>-Rückruf der WebView2 — und wer dort ein modales
/// Fenster hochfährt, startet eine verschachtelte Nachrichtenschleife, während
/// Blazor zeichnet. Die WebView2 liefert darin weitere Nachrichten aus, ein
/// zweiter Zeichenlauf beginnt im ersten. Ob das gutgeht, hängt an der Zeitlage —
/// beim Anwender ging es nicht.</para>
///
/// <para><b>Das Muster, das dabei entsteht, ist immer dasselbe:</b>
/// <c>Task.FromResult(Etwas.Oeffnen(…))</c> — eine Methode, die einen
/// <c>Task</c> verspricht, ihn aber schon fertig zurückgibt, weil sie ihre Arbeit
/// SYNCHRON getan hat. Genau das stand in
/// <c>KiChatHuelle.Gaben.cs</c>:
/// <c>Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster))</c>.</para>
///
/// <para><b>Der Regelweg</b> ist entweder der Baustein <c>Ueberlagerung</c>
/// (Entscheid E‑5 — kein zweites Fenster) oder <c>Blazornachlauf.Nachgelagert</c>
/// (Hausregel (d) — eine gepostete Nachricht später).</para>
///
/// <para><b>Warum der Fall HIER steht und nicht in einem Windows-Test.</b> Die
/// Hüllen liegen in einem <c>net10.0-windows</c>-Projekt; ein Test, der es
/// referenziert, liefe weder auf dem ubuntu-Läufer noch auf macOS. Dieser Fall
/// liest deshalb den QUELLTEXT — derselbe Weg, den <c>StilblattTests</c> zum
/// Stilblatt und <c>ParametersatzTests</c> zu den Gaben geht.</para>
///
/// <para>Keine Sprachbindung: geprüft werden ausschließlich Bezeichner.</para>
/// </summary>
public sealed class HuellenwegTests
{
    /// <summary>
    /// Kleinste Zahl gelesener Dateien, unter der der Leser als kaputt gilt.
    /// Am 05.09.2026 waren es 63.
    /// </summary>
    private const int MINDESTDATEIEN = 40;

    /// <summary>
    /// Was ein modales SYSTEMFENSTER hochfährt — und deshalb nie synchron aus einem
    /// Blazor-Ereignis kommen darf.
    /// </summary>
    /// <remarks>
    /// <c>MitSystemOeffnen</c> steht bewusst NICHT dabei: Es startet die
    /// Shell-Zuordnung in einem anderen Prozess und pumpt keine verschachtelte
    /// Nachrichtenschleife.
    /// </remarks>
    private static readonly Regex ModalRegex = new(
        @"Task\.FromResult\s*\((?:[^();]|\([^()]*\))*?" +
        @"(?:Huelle\.(?:Oeffnen|Anzeigen|Einholen)|\.ShowDialog|" +
        @"Datei\.(?:DateiOeffnen|DateiSpeichern|OrdnerWaehlen)|" +
        @"Dialog\.(?:Meldung|Warnung|Frage))\s*\(",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // =====================================================================
    //  Der Fall
    // =====================================================================

    /// <summary>
    /// Keine Hülle gibt ein modales Systemfenster als bereits erfüllten
    /// <c>Task</c> heraus.
    /// </summary>
    [Fact]
    public void Keine_Huelle_faehrt_ein_modales_Fenster_synchron_hoch()
    {
        var funde = new List<string>();
        int dateien = 0;

        foreach (string pfad in Huellen())
        {
            dateien++;
            string quelltext = File.ReadAllText(pfad);
            string name = Path.GetFileName(pfad);

            foreach (Match treffer in ModalRegex.Matches(quelltext))
            {
                if (InKommentar(quelltext, treffer.Index)) continue;

                funde.Add(name + ":" + Zeile(quelltext, treffer.Index) + "  " +
                          Einzeilig(treffer.Value));
            }
        }

        Assert.True(dateien >= MINDESTDATEIEN,
                    "Nur " + dateien + " Hüllendateien gelesen — der Leser findet sie nicht mehr.");

        Assert.True(funde.Count == 0,
                    "Ein modales Systemfenster darf nicht synchron aus einem Blazor-Ereignis " +
                    "aufgehen (Befunde W13‑B‑1 und W15b‑B‑1). Regelweg: der Baustein " +
                    "Ueberlagerung oder Blazornachlauf.Nachgelagert.\n" +
                    string.Join("\n", funde));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser findet das Muster wirklich — sonst wäre der Fall
    /// oben stumm und niemand merkte es.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_das_Muster()
    {
        const string schlecht =
            "private Task<bool> EinstellungenAsync()\n" +
            "    => Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster));";

        const string gut =
            "private Task<bool> EinstellungenAsync()\n" +
            "    => Blazornachlauf.Nachgelagert(() => KiEinstellungenHuelle.Oeffnen(_fenster));";

        Assert.True(ModalRegex.IsMatch(schlecht));
        Assert.False(ModalRegex.IsMatch(gut));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Alle Hüllen- und Gaben-Dateien unter <c>WindowsFormsApplication1/Views</c>.
    /// </summary>
    private static IEnumerable<string> Huellen()
    {
        string wurzel = Wurzel();
        return Directory.EnumerateFiles(Path.Combine(wurzel, "WindowsFormsApplication1", "Views"),
                                        "*.cs", SearchOption.AllDirectories)
                        .Where(p => Path.GetFileName(p).Contains("Huelle", StringComparison.Ordinal)
                                    || Path.GetFileName(p).Contains("Gaben", StringComparison.Ordinal))
                        .OrderBy(p => p, StringComparer.Ordinal);
    }

    /// <summary>Die Wurzel des Arbeitsbaums, vom Testausgabeordner aus gesucht.</summary>
    private static string Wurzel()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;

        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!.FullName;
    }

    /// <summary>
    /// Steht die Fundstelle in einem Kommentar? Die Hüllen ERKLÄREN den Befund im
    /// Klassenkopf und nennen dabei das falsche Muster — das ist Absicht.
    /// </summary>
    private static bool InKommentar(string quelltext, int stelle)
    {
        int zeilenanfang = quelltext.LastIndexOf('\n', Math.Max(0, stelle - 1)) + 1;
        string vorn = quelltext.Substring(zeilenanfang, stelle - zeilenanfang).TrimStart();
        return vorn.StartsWith("//", StringComparison.Ordinal)
               || vorn.StartsWith("///", StringComparison.Ordinal)
               || vorn.StartsWith("*", StringComparison.Ordinal)
               || vorn.Contains("<c>", StringComparison.Ordinal);
    }

    private static int Zeile(string quelltext, int stelle)
        => quelltext.Take(stelle).Count(z => z == '\n') + 1;

    private static string Einzeilig(string text)
        => Regex.Replace(text, @"\s+", " ").Trim();
}
