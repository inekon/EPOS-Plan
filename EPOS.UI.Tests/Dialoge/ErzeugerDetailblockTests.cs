using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die WACHE über den DETAILBLOCK der Erzeuger-Projektdialoge — die Feldliste, die
/// <c>DetailZu</c> in der Windows-Hülle unter dem Gruppenkopf „Modul" aufbaut.
///
/// <para><b>Worum es geht.</b> Der Block ist eine reine ANZEIGE: Er zeigt die Kennwerte
/// des gewählten Katalogsatzes, und niemand kann darin etwas ändern. Ein Kennwert, der
/// im Aufklapper „Alle Daten anzeigen" desselben Dialogs bearbeitet und gespeichert
/// wird, gehört deshalb nicht zusätzlich hierher — er stünde zweimal auf derselben
/// Maske, einmal tot und einmal lebendig. Genau das war der Fall beim Preis
/// (Anwenderentscheid 21.09.2026: „Die Anzeige der Investitionskosten an dieser Stelle
/// hat keine Funktion").
/// </para>
///
/// <para><b>Warum der Fall den QUELLTEXT liest.</b> Die Hüllen liegen in einem
/// <c>net10.0-windows</c>-Projekt; ein Test, der es referenziert, liefe weder auf dem
/// ubuntu-Läufer noch auf macOS. <c>DetailZu</c> ist zudem privat. Dieser Fall geht
/// deshalb denselben Weg wie <c>KostenknopfWegeTests</c>, <c>ParametersatzTests</c> und
/// <c>HuellenwegTests</c>: Er liest die Hüllendatei als Text.
/// </para>
///
/// <para>Keine Sprachbindung nach aussen: geprüft werden die Ressourcenschlüssel, die
/// im Quelltext stehen.</para>
/// </summary>
public sealed class ErzeugerDetailblockTests
{
    private const string HEIZKESSEL =
        "WindowsFormsApplication1/Views/Heizkessel/HeizkesselHuelle.cs";

    private const string PUFFERSPEICHER =
        "WindowsFormsApplication1/Views/Pufferspeicher/PufferspeicherHuelle.cs";

    // =====================================================================
    //  Die Fälle
    // =====================================================================

    /// <summary>
    /// Der Detailblock des Heizkessels führt Brennstoff Typ und Leistung, dazu den
    /// Schalter „Brennwertkessel" — und KEIN Feld „Investitionskosten".
    /// </summary>
    [Fact]
    public void Der_Detailblock_des_Heizkessels_fuehrt_keine_Investitionskosten()
    {
        string quelltext = Lesen(HEIZKESSEL);
        string block = Feldliste(quelltext);

        Assert.Contains("HZK_LBL_BRENNSTOFFTYP", block, StringComparison.Ordinal);
        Assert.Contains("HZK_LBL_LEISTUNG", block, StringComparison.Ordinal);
        Assert.Contains("HZKK_LBL_BRENNWERT", Rueckgabe(quelltext), StringComparison.Ordinal);

        Assert.DoesNotContain("HZK_LBL_INVEST", block, StringComparison.Ordinal);
        Assert.DoesNotContain("Investitionskosten", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// Der Detailblock des Pufferspeichers führt Hersteller, Speichertyp,
    /// Bereitschaftsverluste und Gesamtvolumen — und KEIN Feld „Investitionskosten".
    /// </summary>
    [Fact]
    public void Der_Detailblock_des_Pufferspeichers_fuehrt_keine_Investitionskosten()
    {
        string block = Feldliste(Lesen(PUFFERSPEICHER));

        Assert.Contains("PSPD_LBL_HERSTELLER", block, StringComparison.Ordinal);
        Assert.Contains("PSPD_LBL_TYP", block, StringComparison.Ordinal);
        Assert.Contains("PSPD_LBL_VERLUSTE", block, StringComparison.Ordinal);
        Assert.Contains("PSPD_LBL_VOLUMEN", block, StringComparison.Ordinal);

        Assert.DoesNotContain("PSPD_LBL_INVEST", block, StringComparison.Ordinal);
        Assert.DoesNotContain("Investitionskosten", block, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser schneidet wirklich die Feldliste aus und lässt die
    /// Kommentare weg — sonst wären beide Fälle stumm. Der erklärende Kommentar steht
    /// in BEIDEN Hüllen und nennt das Wort „Investitionskosten"; läse der Leser ihn
    /// mit, schlüge die Wache an, obwohl das Feld längst weg ist. Umgekehrt muss eine
    /// echte Feldzeile auffallen.
    /// </summary>
    [Fact]
    public void Der_Leser_liest_die_Feldliste_und_nicht_den_Kommentar()
    {
        // Der Kommentar steht wirklich da - sonst prüfte die Gegenprobe nichts.
        Assert.Contains("Investitionskosten", Lesen(HEIZKESSEL), StringComparison.Ordinal);
        Assert.Contains("Investitionskosten", Lesen(PUFFERSPEICHER), StringComparison.Ordinal);

        const string ohneFeld = """
            private static ErzeugerDetail DetailZu(KesselDetail d)
            {
                if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

                // HIER STAND DAS FELD "Investitionskosten [EUR]:" (X_LBL_INVEST).
                var felder = new List<(string, string)>
                {
                    // Auch hier drin nennt ein Kommentar die Investitionskosten.
                    (Text_("X_LBL_LEISTUNG", "Leistung [kW]:"), d.Ptherm.ToString("F2"))
                };

                return new ErzeugerDetail(d.Bezeichner, "", felder,
                                          (Text_("X_LBL_BRENNWERT", "Brennwertkessel"), d.Brennwert));
            """;

        Assert.DoesNotContain("Investitionskosten", Feldliste(ohneFeld), StringComparison.Ordinal);
        Assert.Contains("X_LBL_LEISTUNG", Feldliste(ohneFeld), StringComparison.Ordinal);

        // Der Rückgabewert ist der ZWEITE "return new ErzeugerDetail(" der Methode -
        // der erste ist die Leerprüfung und führt nie einen Schalter.
        Assert.Contains("X_LBL_BRENNWERT", Rueckgabe(ohneFeld), StringComparison.Ordinal);

        const string mitFeld = """
                var felder = new List<(string, string)>
                {
                    (Text_("X_LBL_INVEST", "Investitionskosten [EUR]:"), d.Investitionskosten)
                };
            """;

        Assert.Contains("Investitionskosten", Feldliste(mitFeld), StringComparison.Ordinal);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Die Feldliste des Detailblocks OHNE ihre Kommentarzeilen: von
    /// <c>var felder = new List&lt;(string, string)&gt;</c> bis zum schliessenden
    /// <c>};</c>.
    /// </summary>
    private static string Feldliste(string quelltext)
    {
        int anfang = quelltext.IndexOf("var felder = new List<(string, string)>",
                                       StringComparison.Ordinal);
        Assert.True(anfang >= 0, "Die Feldliste des Detailblocks ist nicht zu finden.");

        int ende = quelltext.IndexOf("};", anfang, StringComparison.Ordinal);
        Assert.True(ende > anfang, "Die Feldliste ist nicht geschlossen.");

        IEnumerable<string> zeilen = quelltext
            .Substring(anfang, ende - anfang)
            .Split('\n')
            .Where(z => !z.TrimStart().StartsWith("//", StringComparison.Ordinal));

        return string.Join("\n", zeilen);
    }

    /// <summary>
    /// Der gebaute Rückgabewert hinter der Feldliste — beim Heizkessel trägt er als
    /// vierten Wert das Schalter-Tupel „Brennwertkessel". Gesucht wird ab dem Ende der
    /// Feldliste, damit die Leerprüfung am Methodenanfang nicht dazwischenkommt.
    /// </summary>
    private static string Rueckgabe(string quelltext)
    {
        int anfang = quelltext.IndexOf("var felder = new List<(string, string)>",
                                       StringComparison.Ordinal);
        Assert.True(anfang >= 0, "Die Feldliste des Detailblocks ist nicht zu finden.");

        Match treffer = Regex.Match(quelltext.Substring(anfang),
                                    @"return new ErzeugerDetail\([^;]*;");
        Assert.True(treffer.Success, "Der Rückgabewert von DetailZu ist nicht zu finden.");
        return treffer.Value;
    }

    private static string Lesen(string repopfad)
    {
        string pfad = Path.Combine(Wurzel(), repopfad.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(pfad), "Es gibt " + repopfad + " nicht (mehr).");
        return File.ReadAllText(pfad);
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
}
