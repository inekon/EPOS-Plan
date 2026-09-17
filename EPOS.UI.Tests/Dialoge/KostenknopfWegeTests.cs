using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die WACHE über die KOSTENKNÖPFE der Erzeuger-Projektdialoge — Auftrag 277.
///
/// <para><b>Worum es geht.</b> Alle sieben Anlagendialoge zeichnen die
/// <c>KostenKnoepfeLeiste</c>. Ob ihre Knöpfe erscheinen und wohin sie führen,
/// entscheidet allein der WIRT: Er legt <c>KostenOeffnen</c> und
/// <c>EnergiekostenOeffnen</c> in seinen Parametersatz. Bleiben die Schlüssel weg,
/// zeichnet die Leiste gar nichts — und genau das war der Zustand, den dieser Fall
/// festhält: Die beiden Windows-Hüllen reichten nur die BESCHRIFTUNGEN herein
/// (<c>KostenInvestText</c> und die zwei anderen), die Wege nicht.</para>
///
/// <para><b>Zwei Hüllen tragen nur ZWEI Knöpfe</b> (Anwenderentscheid 15.09.2026):
/// „Energiekosten…" führt in die Energieträgerverwaltung, und weder ein Pufferspeicher
/// noch ein Sonnenkollektor verbraucht einen gekauften Träger. Ohne Delegat zeichnet die
/// Leiste den Knopf gar nicht erst — deshalb steht der dritte Knopf hier als
/// EIGENSCHAFT der Familie in der Tabelle und nicht als Ausnahme im Fall.</para>
///
/// <para><b>Warum der Fall HIER steht und nicht in einem Windows-Test.</b> Die Hüllen
/// liegen in einem <c>net10.0-windows</c>-Projekt; ein Test, der es referenziert, liefe
/// weder auf dem ubuntu-Läufer noch auf macOS. Dieser Fall liest deshalb den QUELLTEXT
/// — derselbe Weg, den <c>ParametersatzTests</c> zu den Gaben und <c>HuellenwegTests</c>
/// zum modalen Fenster geht.</para>
///
/// <para><b>Die Katalogeditoren stehen bewusst NICHT hier.</b>
/// <c>HeizkesselKatalogDialog</c> und <c>BhkwKatalogDialog</c> tragen dieselbe Leiste,
/// und ihre Wirte lassen die Wege weg — ein Katalogsatz gehört keinem Projekt, es gäbe
/// weder Kostenpositionen noch einen Projektträger zu zeigen. Ohne Delegat bleibt die
/// Leiste dort leer, und das ist die richtige Aussage.</para>
///
/// <para>Keine Sprachbindung: geprüft werden ausschließlich Bezeichner.</para>
/// </summary>
public sealed class KostenknopfWegeTests
{
    /// <summary>
    /// Die Wirte der Kostenleiste mit Projektbezug: Hüllendatei, die Kostenkomponente,
    /// mit der sie die Verwaltungen aufschlägt, und ob die Familie den dritten Knopf
    /// „Energiekosten…" trägt.
    /// </summary>
    private static readonly (string Datei, string Komponente, bool Energiekosten)[] Wirte =
    {
        ("WindowsFormsApplication1/Views/Heizkessel/HeizkesselHuelle.cs",
         "ERZEUGER_HEIZKESSEL", true),
        ("WindowsFormsApplication1/Views/BHKW/BhkwHuelle.cs",
         "ERZEUGER_BHKW", true),
        ("WindowsFormsApplication1/Views/Photovoltaik/PhotovoltaikHuelle.cs",
         "ERZEUGER_PHOTOVOLTAIK", true),
        ("WindowsFormsApplication1/Views/Stromspeicher/StromspeicherHuelle.cs",
         "ERZEUGER_STROMSPEICHER", true),

        // Die WÄRMEPUMPE seit dem 17.09.2026 (Anwenderentscheid „angleichen!"). Bis
        // dahin führte ihr Dialog EINEN Knopf „Kosten bearbeiten…", der Investitions-
        // und Betriebskosten zusammen aufschlug, und gar keinen Weg „Energiekosten…" —
        // obwohl sie Strom bezieht (die Einengung darauf steht seit Auftrag 268/311 im
        // Kern). Sie ist damit kein Sonderfall mehr, sondern der siebte Wirt.
        ("WindowsFormsApplication1/Views/Wärmepumpe/WaermepumpeAnlageHuelle.cs",
         "ERZEUGER_WAERMEPUMPE", true),

        // Ohne Energieträger und damit ohne dritten Knopf.
        ("WindowsFormsApplication1/Views/Pufferspeicher/PufferspeicherHuelle.cs",
         "KOSTEN_KOMPONENTE_PUFFERSPEICHER", false),
        ("WindowsFormsApplication1/Views/Solarthermie/SolarkollektorHuelle.cs",
         "ERZEUGER_SOLARTHERMIE", false),
    };

    /// <summary>Der gemeinsame Weg hinter den drei Knöpfen.</summary>
    private const string WEGE = "WindowsFormsApplication1/Views/Kosten/ErzeugerKostenwege.cs";

    // =====================================================================
    //  Die Fälle
    // =====================================================================

    /// <summary>
    /// Jede Erzeugerhülle belegt die Wege ihrer Knöpfe. Vor Auftrag 277 stand hier nur
    /// die Beschriftung — die Knöpfe wurden gezeichnet und taten nichts.
    /// </summary>
    [Fact]
    public void Jede_Erzeugerhuelle_belegt_die_Wege_ihrer_Knoepfe()
    {
        var funde = new List<string>();

        foreach ((string datei, _, bool energiekosten) in Wirte)
        {
            string quelltext = Lesen(datei);

            if (!Belegt(quelltext, "KostenOeffnen"))
                funde.Add(Path.GetFileName(datei) + ": [\"KostenOeffnen\"] fehlt.");

            // Der dritte Knopf ist eine Eigenschaft der Familie - und wo er fehlt, darf
            // auch seine BESCHRIFTUNG nicht dastehen: Sie waere der Hinweis auf einen
            // Knopf, den die Leiste nie zeichnet.
            if (energiekosten && !Belegt(quelltext, "EnergiekostenOeffnen"))
                funde.Add(Path.GetFileName(datei) + ": [\"EnergiekostenOeffnen\"] fehlt.");
            if (!energiekosten && Belegt(quelltext, "EnergiekostenOeffnen"))
                funde.Add(Path.GetFileName(datei) +
                          ": [\"EnergiekostenOeffnen\"] steht da, obwohl die Familie keinen " +
                          "Energieträger kauft.");
            if (!energiekosten && Belegt(quelltext, "KostenEnergieText"))
                funde.Add(Path.GetFileName(datei) +
                          ": [\"KostenEnergieText\"] beschriftet einen Knopf, den es nicht gibt.");

            // Gegenprobe im selben Atemzug: Die Beschriftungen allein genuegen nicht -
            // genau diese Lage (Text ja, Weg nein) war der Leerlauf.
            if (!Belegt(quelltext, "KostenInvestText"))
                funde.Add(Path.GetFileName(datei) +
                          ": [\"KostenInvestText\"] fehlt - der Leser findet die Gaben nicht mehr.");
        }

        Assert.True(funde.Count == 0,
            "Eine gezeichnete Schaltfläche ohne Wirkung bleibt nicht stehen: Wer die " +
            "KostenKnoepfeLeiste zeigt, belegt ihre Wege oder lässt sie weg.\n  " +
            string.Join("\n  ", funde));
    }

    /// <summary>
    /// Alle Hüllen gehen denselben Weg — <c>ErzeugerKostenwege</c> — und je mit
    /// IHRER Kostenkomponente. Zwei Abschriften desselben Ablaufs liefen auseinander.
    /// </summary>
    [Fact]
    public void Jede_Huelle_geht_denselben_Weg_mit_eigener_Komponente()
    {
        var funde = new List<string>();

        foreach ((string datei, string komponente, bool energiekosten) in Wirte)
        {
            string quelltext = Lesen(datei);
            string name = Path.GetFileName(datei);

            if (!quelltext.Contains("ErzeugerKostenwege.Kosten", StringComparison.Ordinal))
                funde.Add(name + ": ruft ErzeugerKostenwege.Kosten nicht.");
            if (energiekosten
                && !quelltext.Contains("ErzeugerKostenwege.Energiekosten", StringComparison.Ordinal))
                funde.Add(name + ": ruft ErzeugerKostenwege.Energiekosten nicht.");
            if (!quelltext.Contains("DbWerte." + komponente, StringComparison.Ordinal))
                funde.Add(name + ": nennt DbWerte." + komponente + " nicht.");
        }

        Assert.True(funde.Count == 0, string.Join("\n  ", funde));
    }

    /// <summary>
    /// Der gemeinsame Weg fährt seine beiden modalen Fenster NACHGELAGERT hoch.
    /// Ein modales Systemfenster unmittelbar aus einem Blazor-Ereignis pumpt eine
    /// verschachtelte Nachrichtenschleife, während Blazor zeichnet (Regel (b) der
    /// Hüllenschicht); der Regelweg ist <c>Blazornachlauf.Nachgelagert</c>.
    /// </summary>
    [Fact]
    public void Die_beiden_Verwaltungen_gehen_nachgelagert_auf()
    {
        string quelltext = Lesen(WEGE);

        foreach (string ziel in new[] { "KostenKomponenteHuelle.OeffnenProjekt",
                                        "EnergietraegerFenster.Oeffnen" })
        {
            var treffer = Regex.Matches(quelltext,
                @"Blazornachlauf\.Nachgelagert\s*\(\s*\(\s*\)\s*=>\s*" + Regex.Escape(ziel));
            Assert.True(treffer.Count == 1,
                ziel + " geht nicht (genau einmal) über Blazornachlauf.Nachgelagert auf.");
        }
    }

    /// <summary>
    /// Der Bezug ist die GEWÄHLTE Zeile: Beide Wege nehmen eine <c>ErzeugerZeile</c>
    /// entgegen, und die Energieträgerverwaltung bekommt Träger UND Gerät daraus
    /// (der Kontextdurchgriff aus Auftrag 268).
    /// </summary>
    [Fact]
    public void Der_Weg_reicht_die_gewaehlte_Zeile_durch()
    {
        string quelltext = Lesen(WEGE);

        foreach (string wortlaut in new[] { "ErzeugerZeile zeile", "zeile.CarrierId", "zeile.GeraetId" })
            Assert.True(quelltext.Contains(wortlaut, StringComparison.Ordinal),
                        "Der Weg nennt „" + wortlaut + "\" nicht.");

        // Ohne Zeile bleibt es bei 0 - die Verwaltung engt dann nicht auf ein Geraet ein.
        Assert.Matches(@"zeile\s*!=\s*null\s*\?\s*zeile\.CarrierId\s*:\s*0", quelltext);
        Assert.Matches(@"zeile\s*!=\s*null\s*\?\s*zeile\.GeraetId\s*:\s*0", quelltext);
    }

    /// <summary>
    /// Die sieben Dialoge, die die Leiste zeichnen — ihre Razor-Quelle.
    /// </summary>
    private static readonly string[] Dialoge =
    {
        "EPOS.UI/Dialoge/Erzeuger/HeizkesselDialog.razor",
        "EPOS.UI/Dialoge/Erzeuger/BhkwDialog.razor",
        "EPOS.UI/Dialoge/Erzeuger/PhotovoltaikDialog.razor",
        "EPOS.UI/Dialoge/Erzeuger/StromspeicherDialog.razor",
        "EPOS.UI/Dialoge/Erzeuger/PufferspeicherDialog.razor",
        "EPOS.UI/Dialoge/Solarthermie/SolarkollektorenDialog.razor",

        // Die Wärmepumpe steht eingebettet in der Wärmepumpen Verwaltung, und DIE ist
        // Schritt 7 des Assistenten. Ihre Weiche ist derselbe Schalter, den der Wirt
        // durchreicht.
        "EPOS.UI/Dialoge/Waermepumpe/WaermepumpeAnlageDialog.razor",
    };

    /// <summary>
    /// <b>Die zweite Plattform steht in derselben Wache.</b> Der einzige
    /// plattformfreie Wirt dieser sieben Dialoge ist die Assistentenseite — und damit
    /// der einzige Weg, auf dem sie auf iOS erscheinen. Dort gibt es das Projekt noch
    /// nicht, zu dem Kostenpositionen und Projektträger gehörten; die Leiste steht
    /// deshalb in JEDEM von ihnen hinter einer <c>Wizard</c>-Weiche und wird gar nicht
    /// erst gezeichnet. Ohne diese Weiche zeigte der Assistent drei Knöpfe, die ins
    /// Leere führten.
    ///
    /// <para>Der Gegenstand ist dieselbe Hausregel wie beim Wirt: Ein Weg, den eine
    /// Plattform nicht gehen kann, wird BENANNT weggelassen, nicht still angeboten.
    /// Der Assistent selbst lehnt seine elf Schritte auf einer Schale ohne
    /// Seitengaben über <c>AssistentPlattformwege.SeitenSperrgrund</c> ab.</para>
    /// </summary>
    [Fact]
    public void Im_Assistenten_zeichnet_keiner_der_Dialoge_die_Leiste()
    {
        var funde = new List<string>();

        foreach (string datei in Dialoge)
        {
            string quelltext = Lesen(datei);
            string name = Path.GetFileName(datei);

            var treffer = Regex.Matches(quelltext, @"<KostenKnoepfeLeiste\b");
            if (treffer.Count != 1)
            {
                funde.Add(name + ": zeichnet die Leiste " + treffer.Count + "-mal statt einmal.");
                continue;
            }

            // Die naechste @if-Weiche OBERHALB der Leiste muss !Wizard pruefen.
            string davor = quelltext.Substring(0, treffer[0].Index);
            int weiche = davor.LastIndexOf("@if", StringComparison.Ordinal);
            if (weiche < 0)
            {
                funde.Add(name + ": die Leiste steht ohne Weiche.");
                continue;
            }

            string bedingung = davor.Substring(weiche);
            if (!Regex.IsMatch(bedingung, @"@if\s*\(\s*!\s*Wizard\b"))
                funde.Add(name + ": die Weiche vor der Leiste prüft nicht !Wizard.");
        }

        Assert.True(funde.Count == 0,
            "Im Assistenten gibt es keinen Kostenkontext — dort bleibt die Leiste weg, " +
            "statt drei Knöpfe ins Leere zu zeigen.\n  " + string.Join("\n  ", funde));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser erkennt einen unbelegten Schlüssel wirklich —
    /// sonst wäre der erste Fall stumm und niemand merkte es.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_den_unbelegten_Schluessel()
    {
        const string belegt = "[\"KostenOeffnen\"] = new Func<ErzeugerZeile, bool, Task>(";
        const string nurKommentar = "// [\"KostenOeffnen\"] = ... waere der Weg";
        const string garnicht = "[\"KostenInvestText\"] = Text_(\"KDLG_KNOPF_INVEST\"),";

        Assert.True(Belegt(belegt, "KostenOeffnen"));
        Assert.False(Belegt(nurKommentar, "KostenOeffnen"));
        Assert.False(Belegt(garnicht, "KostenOeffnen"));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Steht der Schlüssel als Eintrag eines Parametersatzes im Quelltext — und
    /// nicht bloß in einem Kommentar, der ihn erklärt?
    /// </summary>
    private static bool Belegt(string quelltext, string schluessel)
    {
        foreach (Match treffer in Regex.Matches(
                     quelltext, @"\[""" + Regex.Escape(schluessel) + @"""\]\s*="))
            if (!InKommentar(quelltext, treffer.Index)) return true;
        return false;
    }

    private static bool InKommentar(string quelltext, int stelle)
    {
        int zeilenanfang = quelltext.LastIndexOf('\n', Math.Max(0, stelle - 1)) + 1;
        string vorn = quelltext.Substring(zeilenanfang, stelle - zeilenanfang).TrimStart();
        return vorn.StartsWith("//", StringComparison.Ordinal)
               || vorn.StartsWith("*", StringComparison.Ordinal);
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
