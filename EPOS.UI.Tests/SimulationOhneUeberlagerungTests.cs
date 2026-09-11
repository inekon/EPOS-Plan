using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Strukturwache zur Regel <b>„Die zwei Simulationsseiten stehen in KEINER
/// Überlagerung"</b> (Auftrag <b>#207</b>, Anwenderentscheid <b>SIM‑Q1</b>,
/// Konzept „Simulationsablauf ohne Dialog" 2).
///
/// <para><b>Der Befund, den sie festhält.</b> Bis #207 hatten die zwei fertigen
/// Razor-Seiten <c>SimulationKonfigSeite</c> und <c>SimulationErgebnisSeite</c> DREI
/// Wirte, und zwei davon waren Überlagerungen: Die <c>Startseite</c> zeigte das
/// Ergebnis als breite <c>Ueberlagerung</c> (96 vw × 94 vh), und die Ergebnisseite
/// öffnete die Konfiguration als ZWEITE Überlagerung darin — Fenster im Fenster im
/// Fenster. Der Anwender gab genau das zurück: „Der Simulationsdialog im Fenster ist
/// nicht gut" (11.09.2026). Seither sind beide Schritte EINER freien Ansicht
/// (<c>SimulationSeite</c>), und eine Überlagerung darf es für sie nicht mehr
/// geben.</para>
///
/// <para><b>Sie prüft das MARKUP</b>, nicht eine Hülle — dieselbe Bauart wie
/// <see cref="UeberlagerungstitelTests"/> (Bauart A): Steht irgendwo in
/// <c>EPOS.UI</c> eine <c>&lt;Ueberlagerung …&gt;</c>, deren Block eine der zwei
/// Komponenten enthält, ist das ein Fund. Kurze UNTERDIALOGE der beiden Seiten
/// bleiben ausdrücklich Überlagerungen (Regel SD‑Q1) — sie sind nicht diese zwei
/// Komponenten und stören die Wache nicht.</para>
///
/// <para><b>Ausnahmeliste: leer.</b></para>
/// </summary>
public sealed class SimulationOhneUeberlagerungTests
{
    /// <summary>Die zwei Komponenten, die in keiner Überlagerung stehen dürfen.</summary>
    private static readonly string[] Verboten =
    {
        "SimulationKonfigSeite",
        "SimulationErgebnisSeite"
    };

    private readonly record struct Fund(string Datei, int Zeile, string Komponente);

    [Fact]
    public void Keine_Ueberlagerung_zeigt_eine_der_zwei_Simulationsseiten()
    {
        List<Fund> funde = Funde(RazorDateien());

        Assert.True(funde.Count == 0,
            "Eine Simulationsseite steht wieder in einer Ueberlagerung (SIM-Q1):\n" +
            string.Join("\n", funde.Select(f => "  " + f.Datei + ":" + f.Zeile + " → " + f.Komponente)));
    }

    /// <summary>
    /// Die Wache muss auch etwas finden können (Lehre W6‑B‑1: eine grüne Wache, die
    /// nie rot werden kann, prüft nichts). Der eingefrorene Bestand der
    /// <c>Startseite</c> VOR #207 — das Ergebnis in der breiten Überlagerung — muss
    /// als Fund erscheinen.
    /// </summary>
    [Fact]
    public void Die_Wache_findet_den_eingefrorenen_Befund_vor_207()
    {
        const string vorher =
            "    <Ueberlagerung Offen=\"@(_ergebnis is not null)\" Titel=\"@ErgebnisTitelText\"\n" +
            "                   Zusatzklasse=\"epos-ueberlagerung--breit\"\n" +
            "                   Geschlossen=\"ErgebnisSchliessen\">\n" +
            "        <KindInhalt>\n" +
            "            @if (_ergebnis is not null)\n" +
            "            {\n" +
            "                <EPOS.UI.Seiten.Simulation.SimulationErgebnisSeite @attributes=\"_ergebnis\"\n" +
            "                                                                   Geschlossen=\"ErgebnisSchliessen\" />\n" +
            "            }\n" +
            "        </KindInhalt>\n" +
            "    </Ueberlagerung>\n";

        List<Fund> funde = Funde(new Dictionary<string, string> { ["Startseite.razor"] = vorher });

        Fund fund = Assert.Single(funde);
        Assert.Equal("SimulationErgebnisSeite", fund.Komponente);
    }

    /// <summary>
    /// Eine Überlagerung, die eine ANDERE Komponente zeigt, ist kein Fund — die
    /// kurzen Unterdialoge der zwei Seiten bleiben Überlagerungen (SD‑Q1).
    /// </summary>
    [Fact]
    public void Ein_kurzer_Unterdialog_bleibt_eine_Ueberlagerung()
    {
        const string erlaubt =
            "    <Ueberlagerung Offen=\"true\" Titel=\"@Resource.SIMERG_TAB_BEDARF\">\n" +
            "        <KindInhalt>\n" +
            "            <EPOS.UI.Dialoge.Bedarf.BedarfErgebnisDialog @attributes=\"_bedarf\" />\n" +
            "        </KindInhalt>\n" +
            "    </Ueberlagerung>\n";

        Assert.Empty(Funde(new Dictionary<string, string> { ["X.razor"] = erlaubt }));
    }

    // =====================================================================
    //  Der Leser
    // =====================================================================

    /// <summary>
    /// Jeder <c>&lt;Ueberlagerung&gt;</c>-Block je Datei; gefunden wird über die
    /// Zeilen zwischen öffnendem und schließendem Element. Verschachtelte
    /// Überlagerungen gibt es im Bestand nicht — und gäbe es sie, fände der Leser
    /// die äußere und damit erst recht den Fund.
    /// </summary>
    private static List<Fund> Funde(IReadOnlyDictionary<string, string> dateien)
    {
        var funde = new List<Fund>();

        foreach (KeyValuePair<string, string> datei in dateien)
        {
            string[] zeilen = datei.Value.Replace("\r\n", "\n").Split('\n');
            int tiefe = 0;

            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];

                if (tiefe > 0)
                    foreach (string komponente in Verboten)
                        if (Regex.IsMatch(zeile, @"<\s*(?:[\w.]+\.)?" + komponente + @"\b"))
                            funde.Add(new Fund(datei.Key, i + 1, komponente));

                // Ein selbstschliessendes <Ueberlagerung … /> oeffnet keinen Block.
                if (Regex.IsMatch(zeile, @"<\s*Ueberlagerung\b") && !zeile.Contains("/>")) tiefe++;
                if (Regex.IsMatch(zeile, @"</\s*Ueberlagerung\s*>") && tiefe > 0) tiefe--;
            }
        }

        return funde;
    }

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    private static Dictionary<string, string> RazorDateien()
    {
        string wurzel = Path.Combine(Wurzel(), "EPOS.UI");
        var ergebnis = new Dictionary<string, string>();

        foreach (string p in Directory.GetFiles(wurzel, "*.razor", SearchOption.AllDirectories))
            ergebnis[Path.GetFileName(p)] = File.ReadAllText(p);

        // Die Wache liest den ganzen Baum; unter 50 Dateien ist der Leser kaputt.
        Assert.True(ergebnis.Count > 50, "Der Razor-Leser hat kaum Dateien gefunden.");
        return ergebnis;
    }
}
