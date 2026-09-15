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
/// <para><b>Die Sache ist immer dieselbe:</b> eine Methode, die einen <c>Task</c>
/// verspricht, ihn aber schon fertig zurückgibt, weil sie ihre Arbeit SYNCHRON
/// getan hat. <b>Geschrieben wird sie auf zwei Weisen</b> — und deshalb steht hier
/// nicht eine Regel, sondern zwei:</para>
///
/// <list type="number">
///   <item><description><b>Als Ausdruck:</b>
///   <c>Task.FromResult(Etwas.Oeffnen(…))</c>. Genau das stand in
///   <c>KiChatHuelle.Gaben.cs</c>:
///   <c>Task.FromResult(KiEinstellungenHuelle.Oeffnen(_fenster))</c>. Diese
///   Schreibweise findet <see cref="ModalRegex"/> an einer einzigen
///   Stelle.</description></item>
///   <item><description><b>Als Anweisung:</b> der modale Aufruf steht für sich,
///   danach schließt die Methode mit <c>return Task.CompletedTask;</c>. Das war
///   <c>WaermepumpeAnlageHuelle.KostenOeffnen</c> — dieselbe Sache, andere
///   Schreibweise, und der Ausdrucksregel entgangen. Diese Frage gilt dem ganzen
///   METHODENRUMPF (Rückgabetyp <c>Task</c>, kein <c>await</c>, kein Nachlauf,
///   synchroner Abschluss) und lässt sich nicht als ein Ausdrucksmuster stellen;
///   sie bringt deshalb ihren eigenen Leser mit
///   (<see cref="AnweisungsfundeIn"/>).</description></item>
///   <item><description><b>Als Lambda:</b> dasselbe, nur ohne eigene Methode —
///   gleich an Ort und Stelle in den Gabensatz geschrieben. Derselbe Leser misst
///   auch diesen Rumpf.</description></item>
/// </list>
///
/// <para><b>Der Regelweg</b> ist der Baustein <c>Ueberlagerung</c>
/// (Entscheid E‑5 — kein zweites Fenster), <c>Blazornachlauf.Nachgelagert</c>
/// (Hausregel (d) — eine gepostete Nachricht später) oder der wartbare Zwilling
/// des Dienstes (<c>DateiSpeichernAsync</c> und seinesgleichen), der selbst über
/// den Nachlauf geht.</para>
///
/// <para><b>Eine Ausnahmeliste gibt es nicht.</b> Trägt eine Stelle das Muster,
/// ohne ein Systemfenster hochzufahren, dann ist die Regel zu schärfen — eine
/// ausgenommene Datei macht die Wache an genau der Stelle wieder blind.</para>
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
        @"Task\.FromResult\s*\((?:[^();]|\([^()]*\))*?" + MODALE_AUFRUFE,
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Derselbe modale Aufruf, aber OHNE die Klammer davor — für die zweite Regel,
    /// die einen ganzen Methodenrumpf liest statt eines Ausdrucks.
    /// </summary>
    private static readonly Regex ModalAufrufRegex = new(
        MODALE_AUFRUFE, RegexOptions.Compiled);

    /// <summary>
    /// Die Aufrufe selbst — einmal geschrieben, von beiden Regeln benutzt. Das
    /// <c>Async</c>-Geschwister fällt von selbst heraus: Auf den Namen folgt hier
    /// unmittelbar die öffnende Klammer, und <c>DateiSpeichernAsync(</c> trägt
    /// dazwischen noch fünf Buchstaben.
    /// </summary>
    private const string MODALE_AUFRUFE =
        @"(?:Huelle\.(?:Oeffnen|Anzeigen|Einholen)\w*|\.ShowDialog|" +
        @"Datei\.(?:DateiOeffnen|DateienOeffnen|DateiSpeichern|OrdnerWaehlen)|" +
        @"Dialog\.(?:Meldung|Warnung|Frage))\s*\(";

    /// <summary>
    /// Der Kopf einer Methode, die einen <c>Task</c> verspricht UND einen Rumpf in
    /// geschweiften Klammern hat. Die Zeichenklasse zwischen Sichtbarkeit und
    /// <c>Task</c> lässt weder <c>;</c> noch Klammern zu — so kann der Kopf nicht
    /// aus einer Methode in die nächste laufen.
    /// </summary>
    /// <remarks>
    /// Ausdrucksrümpfe (<c>=&gt; …;</c>) stehen bewusst nicht darin: Wer einen
    /// <c>Task</c> als Ausdruck liefert, kommt an <c>Task.FromResult</c> nicht
    /// vorbei — und das ist der Fall der ersten Regel.
    /// </remarks>
    private static readonly Regex TaskKopfRegex = new(
        @"(?<![\w.])(?:private|internal|protected|public)[^;{}()=]*?\bTask\b" +
        @"(?:\s*<[^<>{}()]*>)?\s+(\w+)\s*\([^{};]*\)\s*\{",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Derselbe Rumpf, nur ohne eigene Methode: das LAMBDA mit geschweiften
    /// Klammern, wie es gleich an Ort und Stelle in den Gabensatz geschrieben wird
    /// (<c>new Func&lt;Task&gt;(() =&gt; { … })</c>). Auch das ist eine Schreibweise,
    /// die man beim Schreiben der ersten Regel nicht im Kopf hatte.
    /// </summary>
    /// <remarks>
    /// Der Rumpf eines <c>Blazornachlauf.Nachgelagert(() =&gt; { … })</c> fällt von
    /// selbst heraus: Er gibt keinen fertigen <c>Task</c> heraus — das tut die
    /// Methode um ihn herum, und die führt den Nachlauf.
    /// </remarks>
    private static readonly Regex LambdaKopfRegex = new(@"=>\s*\{", RegexOptions.Compiled);

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

        Assert.Matches(ModalRegex, schlecht);
        Assert.DoesNotMatch(ModalRegex, gut);
    }

    // =====================================================================
    //  Der zweite Fall: der modale Aufruf als ANWEISUNG
    // =====================================================================

    /// <summary>
    /// Keine Hülle ruft ein modales Systemfenster als gewöhnliche Anweisung und
    /// schließt danach mit einem schon fertigen <c>Task</c> ab.
    /// </summary>
    [Fact]
    public void Keine_Huelle_ruft_ein_modales_Fenster_als_Anweisung()
    {
        var funde = new List<string>();
        int dateien = 0;

        foreach (string pfad in Huellen())
        {
            dateien++;
            string quelltext = File.ReadAllText(pfad);
            string name = Path.GetFileName(pfad);

            foreach (string fund in AnweisungsfundeIn(quelltext))
                funde.Add(name + ":" + fund);
        }

        Assert.True(dateien >= MINDESTDATEIEN,
                    "Nur " + dateien + " Hüllendateien gelesen — der Leser findet sie nicht mehr.");

        Assert.True(funde.Count == 0,
                    "Ein modales Systemfenster darf nicht synchron aus einem Blazor-Ereignis " +
                    "aufgehen — auch nicht als eigene Anweisung mit einem fertigen Task " +
                    "dahinter (Befunde W13‑B‑1 und W15b‑B‑1). Regelweg: der Baustein " +
                    "Ueberlagerung, Blazornachlauf.Nachgelagert oder der wartbare Zwilling " +
                    "des Dienstes.\n" +
                    string.Join("\n", funde));
    }

    /// <summary>
    /// <b>Gegenprobe zur zweiten Regel</b> — und zugleich der Beleg, dass sie den
    /// Fall findet, an dem die erste vorbeigelesen hat: der Wortlaut von
    /// <c>WaermepumpeAnlageHuelle.KostenOeffnen</c>, wie er vor seiner Umstellung
    /// dastand.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_den_Aufruf_als_Anweisung()
    {
        const string schlecht =
            "private static Task KostenOeffnen(IWin32Window besitzer, WErzeugerModel modell)\n" +
            "{\n" +
            "    if (modell == null) return Task.CompletedTask;\n" +
            "    KostenKomponenteHuelle.OeffnenProjekt(besitzer, 7, \"P\", \"WP\", false, 3);\n" +
            "    return Task.CompletedTask;\n" +
            "}\n";

        // Dieselbe Stelle, nachgelagert.
        const string gutNachlauf =
            "private static Task KostenOeffnen(IWin32Window besitzer, WErzeugerModel modell)\n" +
            "{\n" +
            "    if (modell == null) return Task.CompletedTask;\n" +
            "    return Blazornachlauf.Nachgelagert(() =>\n" +
            "        KostenKomponenteHuelle.OeffnenProjekt(besitzer, 7, \"P\", \"WP\", false, 3));\n" +
            "}\n";

        // Der zweite Regelweg: der wartbare Zwilling des Dienstes.
        const string gutAwait =
            "private static async Task<string> ProtokollSpeichern(IReadOnlyList<string> zeilen)\n" +
            "{\n" +
            "    string pfad = await Dienste.Datei.DateiSpeichernAsync(\"T\", \"*.txt\", \"P\");\n" +
            "    if (string.IsNullOrEmpty(pfad)) return \"\";\n" +
            "    return pfad;\n" +
            "}\n";

        // Eine Methode ohne modales Fenster darf nie auffallen — die Regel greift
        // am Aufruf, nicht am fertigen Task.
        const string gutOhneFenster =
            "private static Task<int> Zaehlen(int a)\n" +
            "{\n" +
            "    int n = a + 1;\n" +
            "    return Task.FromResult(n);\n" +
            "}\n";

        // Dieselbe Sache ohne eigene Methode — gleich im Gabensatz.
        const string schlechtLambda =
            "[\"KostenOeffnen\"] = new Func<Task>(() =>\n" +
            "{\n" +
            "    KostenKomponenteHuelle.OeffnenProjekt(besitzer, 7, \"P\", \"WP\", false, 3);\n" +
            "    return Task.CompletedTask;\n" +
            "}),\n";

        Assert.Single(AnweisungsfundeIn(schlecht));
        Assert.Contains("KostenOeffnen", AnweisungsfundeIn(schlecht)[0], StringComparison.Ordinal);
        Assert.Single(AnweisungsfundeIn(schlechtLambda));
        Assert.Empty(AnweisungsfundeIn(gutNachlauf));
        Assert.Empty(AnweisungsfundeIn(gutAwait));
        Assert.Empty(AnweisungsfundeIn(gutOhneFenster));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Die Fundstellen der zweiten Regel in einem Quelltext: je
    /// <c>Task</c>-liefernde Methode mit Rumpf, die weder <c>await</c>et noch
    /// nachlagert, aber mit einem schon fertigen <c>Task</c> abschließt, jeder
    /// modale Aufruf darin.
    /// </summary>
    /// <remarks>
    /// <c>await</c>, <c>Blazornachlauf</c> und <c>Blazorsprung</c> im Rumpf sind die
    /// drei Regelwege; wer einen davon geht, ist fertig. <c>Task.CompletedTask</c>
    /// bzw. <c>Task.FromResult</c> muss vorkommen — ohne sie kann die Methode ihren
    /// <c>Task</c> gar nicht synchron fertig herausgeben.
    /// </remarks>
    private static List<string> AnweisungsfundeIn(string quelltext)
    {
        var funde = new List<string>();
        var gesehen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match kopf in TaskKopfRegex.Matches(quelltext))
            Sammle(quelltext, kopf.Index + kopf.Length - 1, kopf.Groups[1].Value, funde, gesehen);

        foreach (Match kopf in LambdaKopfRegex.Matches(quelltext))
            Sammle(quelltext, kopf.Index + kopf.Length - 1, "(Lambda)", funde, gesehen);

        return funde;
    }

    /// <summary>
    /// Ein Rumpf, gemessen. <paramref name="gesehen"/> hält dieselbe Stelle davon
    /// ab, zweimal zu erscheinen: Ein Lambda IN einer gemeldeten Methode trägt
    /// denselben Aufruf in derselben Zeile.
    /// </summary>
    private static void Sammle(string quelltext, int klammer, string benennung,
                               List<string> funde, HashSet<string> gesehen)
    {
        string? rumpf = Rumpf(quelltext, klammer);
        if (rumpf is null) return;

        if (rumpf.Contains("await", StringComparison.Ordinal)) return;
        if (rumpf.Contains("Blazornachlauf", StringComparison.Ordinal)) return;
        if (rumpf.Contains("Blazorsprung", StringComparison.Ordinal)) return;

        if (!rumpf.Contains("Task.CompletedTask", StringComparison.Ordinal) &&
            !rumpf.Contains("Task.FromResult", StringComparison.Ordinal)) return;

        string ohneWorte = OhneKommentarzeilen(rumpf);
        int kopfzeile = Zeile(quelltext, klammer);

        foreach (Match treffer in ModalAufrufRegex.Matches(ohneWorte))
        {
            int zeile = kopfzeile + Zeile(ohneWorte, treffer.Index) - 1;
            string aufruf = Einzeilig(treffer.Value);

            if (!gesehen.Add(zeile + "|" + aufruf)) continue;

            funde.Add(zeile + "  " + benennung + "  " + aufruf);
        }
    }

    /// <summary>
    /// Der Methodenrumpf ab der öffnenden Klammer, über die Klammertiefe gezählt.
    /// <c>null</c>, wenn er nicht schließt.
    /// </summary>
    private static string? Rumpf(string quelltext, int klammer)
    {
        int tiefe = 0;

        for (int i = klammer; i < quelltext.Length; i++)
        {
            if (quelltext[i] == '{') tiefe++;
            else if (quelltext[i] == '}')
            {
                tiefe--;
                if (tiefe == 0) return quelltext.Substring(klammer, i - klammer + 1);
            }
        }

        return null;
    }

    /// <summary>
    /// Kommentarzeilen geleert — die Zeilen selbst bleiben stehen, damit die
    /// gemeldete Zeilennummer stimmt.
    /// </summary>
    private static string OhneKommentarzeilen(string text)
    {
        string[] zeilen = text.Split('\n');

        for (int i = 0; i < zeilen.Length; i++)
        {
            string schlank = zeilen[i].TrimStart();
            if (schlank.StartsWith("//", StringComparison.Ordinal) ||
                schlank.StartsWith("*", StringComparison.Ordinal))
                zeilen[i] = "";
        }

        return string.Join("\n", zeilen);
    }

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
