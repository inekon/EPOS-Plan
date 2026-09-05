using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Die STILBLATT-Wache über den Hilfe-Assistenten — Anwenderbefund <b>W15b‑E‑3</b>
/// der Windows-Abnahme vom 05.09.2026 („Darstellung kann verbessert werden").
/// </summary>
/// <remarks>
/// <para>
/// <b>Warum nicht bunit.</b> Eine bunit-Probe rechnet keine Stilblätter aus: Das
/// Markup war die ganze Zeit richtig, und trotzdem war der Gesprächsverlauf beim
/// Anwender „nicht zu sehen (leer, ohne Rahmen)". Das ist die Lehre aus
/// <b>W6‑B‑1</b> — geprüft wird die REGEL, so wie in
/// <c>KostenSeiteTests.Die_Aktionszelle_traegt_im_Stilblatt_kein_display_flex</c>.
/// </para>
/// <para>
/// <b>Die zwei Ursachen des Befunds.</b> (a) <c>.epos-kichat</c> trug
/// <c>height: 100%</c>, und die Höhenkette darüber (html, body, #app) ist offen —
/// eine prozentuale Höhe braucht einen Wirt mit bestimmter Höhe (CSS 2.1, 10.5).
/// Der Chat war damit nur so hoch wie sein Inhalt, der Verlauf blieb bei seiner
/// Mindesthöhe unter dem Kopfkasten stehen. (b) Die Knopfreihe stand auf
/// <c>flex-direction: column</c> — die drei Knöpfe waren rechts gestapelt.
/// </para>
/// <para>
/// Geprüft wird jeweils die LETZTE Festlegung eines Selektors: Der Block
/// „Hilfe-Assistent" ist ANGEHÄNGT und gewinnt damit bei gleicher Spezifität.
/// </para>
/// </remarks>
public sealed class KiChatAnmutungTests
{
    private static string Blatt()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !File.Exists(Path.Combine(ordner.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            ordner = ordner.Parent;

        Assert.True(ordner is not null, "epos-ui.css ist nicht zu finden.");
        return File.ReadAllText(Path.Combine(ordner!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    /// <summary>Der Rumpf der LETZTEN Regel zu diesem Selektor.</summary>
    private static string LetzteRegel(string selektor)
    {
        var muster = new Regex(@"(?<![\w\-.>])" + Regex.Escape(selektor) + @"\s*\{([^}]*)\}",
                               RegexOptions.Compiled);
        MatchCollection treffer = muster.Matches(Blatt());

        Assert.True(treffer.Count > 0, "Keine Regel zu " + selektor + " im Stilblatt.");
        return treffer[treffer.Count - 1].Groups[1].Value;
    }

    // =====================================================================
    //  Die Hoehe (Ursache a)
    // =====================================================================

    /// <summary>
    /// <b>Der Chat bekommt eine Höhe, die nicht an der Höhenkette hängt.</b> Ohne sie
    /// hat der Verlauf nichts, worin er wachsen könnte.
    /// </summary>
    [Fact]
    public void Der_Chat_nimmt_die_Hoehe_des_Sichtfeldes()
    {
        string regel = LetzteRegel(".epos-kichat");

        Assert.Contains("height: 100vh", regel, StringComparison.Ordinal);
        Assert.DoesNotContain("height: 100%", regel, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Verlauf füllt, was zwischen Kopf und Eingabe übrig bleibt — und er hat
    /// einen sichtbaren Grund.
    /// </summary>
    [Fact]
    public void Der_Verlauf_fuellt_die_Flaeche_zwischen_Kopf_und_Eingabe()
    {
        string regel = LetzteRegel(".epos-kichat > .epos-verlauf");

        Assert.Contains("flex: 1 1 auto", regel, StringComparison.Ordinal);
        Assert.Contains("min-height", regel, StringComparison.Ordinal);
        Assert.Contains("--epos-flaeche-hell", regel, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Die Knopfreihe (Ursache b)
    // =====================================================================

    /// <summary>
    /// <b>Fragen, Nur suchen und „Werkzeuge…" stehen in EINER Reihe.</b> Auf dem
    /// Bildschirmfoto standen sie rechts untereinander.
    /// </summary>
    [Fact]
    public void Die_Knopfreihe_der_Eingabe_steht_in_einer_Reihe()
    {
        string regel = LetzteRegel(".epos-kieingabe-knoepfe");

        Assert.Contains("flex-direction: row", regel, StringComparison.Ordinal);
        Assert.DoesNotContain("flex-direction: column", regel, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Das Kennzeichen der Werkzeugliste (W15b-E-4)
    // =====================================================================

    /// <summary>
    /// <b>Zwei Zustände desselben Bedienelements müssen SICHTBAR verschieden sein</b>
    /// (Hausregel seit W16b‑B‑2b). „Liest nur" und „Ändert Daten" tragen deshalb
    /// verschiedene Rahmen- UND Textfarben, nicht zwei benachbarte Grautöne.
    /// </summary>
    [Fact]
    public void Lesend_und_aendernd_sind_sichtbar_verschieden()
    {
        string lesend = LetzteRegel(".epos-kiwerkzeuge-merkmal--lesend");
        string aendernd = LetzteRegel(".epos-kiwerkzeuge-merkmal--aendernd");

        Assert.Contains("--epos-quelle-text", lesend, StringComparison.Ordinal);
        Assert.Contains("--epos-warn-text", aendernd, StringComparison.Ordinal);
        Assert.NotEqual(lesend.Trim(), aendernd.Trim());
    }

    /// <summary>
    /// Der Block ist ANGEHÄNGT und steht genau einmal — wer ihn ein zweites Mal
    /// anlegt, hat zwei Wahrheiten über dieselbe Ansicht.
    /// </summary>
    [Fact]
    public void Der_Block_steht_genau_einmal_im_Blatt()
    {
        const string marke = "Hilfe-Assistent (Windows-Abnahme 05.09.2026";

        int erste = Blatt().IndexOf(marke, StringComparison.Ordinal);
        Assert.True(erste >= 0, "Der Block fehlt.");
        Assert.Equal(-1, Blatt().IndexOf(marke, erste + 1, StringComparison.Ordinal));
    }
}
