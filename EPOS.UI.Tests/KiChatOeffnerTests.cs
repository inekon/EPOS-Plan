using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die WACHEN um den HILFE-ASSISTENTEN auf der Hüllenseite — Auftrag #219, Befunde
/// <b>KI‑D‑B‑1</b> („die Eingabe funktioniert nicht", Anwender 11.09.2026) und
/// <b>KI‑D‑B‑2</b> („Online-Dokumentation öffnen tut nichts", derselbe Tag).
///
/// <para><b>Warum am Quelltext.</b> Die drei geprüften Dateien liegen in einem
/// <c>net10.0-windows</c>-Projekt; ein Test, der es referenziert, liefe weder auf dem
/// ubuntu-Läufer noch auf macOS. Dieser Zeuge liest deshalb den QUELLTEXT — derselbe Weg,
/// den <c>HuellenwegTests</c> zum modalen Systemfenster, <c>ParametersatzTests</c> zu den
/// Gaben und <c>StilblattTests</c> zum Stilblatt gehen.</para>
///
/// <para><b>Was er NICHT ersetzt.</b> Ob die Tastatur beim Anwender wirklich im
/// Chatfenster landet, sagt nur das Gerät: Es hängt am Verhalten von WebView2, wenn ein
/// zweites, nicht-modales Fenster mit einer zweiten WebView2 aufgeht. Geprüft wird hier,
/// dass der Weg dorthin überhaupt gebaut ist — der verzögerte Sprung und die
/// ausdrückliche Fokusübergabe.</para>
///
/// <para>Keine Sprachbindung: geprüft werden ausschließlich Bezeichner.</para>
/// </summary>
public sealed class KiChatOeffnerTests
{
    // =====================================================================
    //  KI-D-B-1: der Öffnungsweg
    // =====================================================================

    /// <summary>
    /// <b>Der Assistent geht über <c>Blazorsprung</c> auf</b> — auch auf dem Weg aus
    /// einer Ansicht (der Hilfe-Pille).
    /// </summary>
    /// <remarks>
    /// <para>Der MENÜweg tat das seit W16b: <c>HauptfensterHuelle.Weg</c> verzögert
    /// jeden Punkt. Der Weg aus einer Maske lief bis #219 daran vorbei —
    /// <c>InfoKnopf</c> → <c>KiAssistentWeg.AusDialog</c> →
    /// <c>Dienste.Navigation.OeffneMaske</c> → <c>WinFormsNavigation</c>, und dort stand
    /// der blanke Aufruf. Damit entstand die zweite WebView2 SYNCHRON im
    /// <c>WebMessageReceived</c>-Rückruf der ersten: genau die Lage der Befunde
    /// W16b‑B‑1, W13‑B‑1 und W15b‑B‑1.</para>
    /// </remarks>
    [Fact]
    public void Der_Assistent_geht_ueber_den_Blazorsprung_auf()
    {
        string quelle = Lies("WindowsFormsApplication1", "Dienste", "WinFormsNavigation.cs");
        string fall = Fallblock(quelle, "Masken.KiAssistent");

        Assert.Contains("Blazorsprung.Verzoegert", fall, StringComparison.Ordinal);
        Assert.Contains("KiChatHuelle.Oeffnen", fall, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser findet den Fallblock wirklich und sieht einen
    /// blanken Aufruf als das, was er ist.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_einen_blanken_Aufruf()
    {
        const string schlecht =
            "case Masken.KiAssistent:\n" +
            "    KiChatHuelle.Oeffnen(Form.ActiveForm, Aufrufkontext(argumente));\n" +
            "    return true;\n" +
            "case Masken.Simulation:\n";

        string fall = Fallblock(schlecht, "Masken.KiAssistent");

        Assert.Contains("KiChatHuelle.Oeffnen", fall, StringComparison.Ordinal);
        Assert.DoesNotContain("Blazorsprung", fall, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die Hülle übergibt die Tastatur</b> — auf BEIDEN Wegen: beim Aufbau des
    /// Fensters (hinter <c>Show</c>) und beim Nach-vorn-Holen eines bereits offenen.
    /// </summary>
    /// <remarks>
    /// Ein <c>Show(besitzer)</c> holt die Eingabe nicht von selbst, anders als ein
    /// <c>ShowDialog</c> mit seiner eigenen Nachrichtenschleife; und ein blosses
    /// <c>Activate()</c> stellt das Fenster nur vor die anderen.
    /// </remarks>
    [Fact]
    public void Die_Chathuelle_uebergibt_die_Tastatur_auf_beiden_Wegen()
    {
        string quelle = Lies("WindowsFormsApplication1", "Views", "Help", "KiChatHuelle.cs");
        string[] zeilen = OhneKommentare(quelle);

        int uebergaben = zeilen.Count(z => z.Contains("TastaturUebergeben()", StringComparison.Ordinal));

        Assert.True(uebergaben >= 2,
                    "KiChatHuelle übergibt die Tastatur an " + uebergaben +
                    " Stelle(n) — erwartet werden zwei: hinter Show(...) und beim " +
                    "Nach-vorn-Holen eines offenen Fensters (Befund KI-D-B-1).");
    }

    /// <summary>
    /// <b>Die Fokusübergabe steht in der Hülle</b>, nicht in jedem Aufrufer:
    /// <c>BlazorDialogForm.TastaturUebergeben</c> ist die eine Stelle, an der WinForms
    /// und WebView2 sich über die Tastatur einigen.
    /// </summary>
    [Fact]
    public void Die_Dialoghuelle_fuehrt_die_Fokusuebergabe()
    {
        string quelle = Lies("WindowsFormsApplication1", "Allgemein", "Blazor", "BlazorDialogForm.cs");

        Assert.Contains("public void TastaturUebergeben()", quelle, StringComparison.Ordinal);

        // Zweimal greifen: jetzt (falls die WebView schon steht) und nach ihrer
        // Initialisierung - sie baut sich asynchron auf.
        Assert.Contains("CoreWebView2InitializationCompleted", quelle, StringComparison.Ordinal);
        Assert.Contains("Activate()", quelle, StringComparison.Ordinal);
    }

    // =====================================================================
    //  KI-D-B-2: eine Adresse ist keine Datei
    // =====================================================================

    /// <summary>
    /// <b>Eine ADRESSE geht über <c>Dienste.Datei.AdresseOeffnen</c></b>, nie über
    /// <c>MitSystemOeffnen</c>.
    /// </summary>
    /// <remarks>
    /// <para>Das ist Befund <b>KI‑D‑B‑2</b>, und die Begründung steht seit iU9‑W16c.3 in
    /// <c>IDateiDienst</c> selbst: <c>MitSystemOeffnen</c> beginnt mit
    /// <c>if (!File.Exists(pfad)) return false;</c>. Für eine Adresse ist das immer
    /// <c>false</c> — der Aufruf kehrt ohne Wirkung und ohne Meldung zurück. Betroffen
    /// waren der Fußleistenverweis des Chats, jeder Wikitreffer der Suche und jeder
    /// Verweis aus einer Modellantwort.</para>
    /// <para>Geprüft wird jede Datei unter <c>WindowsFormsApplication1</c>, in der ein
    /// <c>MitSystemOeffnen</c> ein Argument bekommt, das nach einer Adresse aussieht
    /// (<c>adresse</c>, <c>url</c>, <c>http</c>).</para>
    /// </remarks>
    [Fact]
    public void Eine_Adresse_geht_nie_ueber_MitSystemOeffnen()
    {
        var funde = new List<string>();
        int dateien = 0;

        foreach (string pfad in Quelldateien())
        {
            dateien++;
            string quelltext = File.ReadAllText(pfad);

            foreach (Match treffer in AufrufRegex.Matches(quelltext))
            {
                if (InKommentar(quelltext, treffer.Index)) continue;
                if (!NachAdresse(treffer.Groups["arg"].Value)) continue;

                funde.Add(Path.GetFileName(pfad) + ":" + Zeile(quelltext, treffer.Index) + "  " +
                          Einzeilig(treffer.Value));
            }
        }

        Assert.True(dateien >= 100,
                    "Nur " + dateien + " Quelldateien gelesen — der Leser findet sie nicht mehr.");

        Assert.True(funde.Count == 0,
                    "Eine Adresse ist keine Datei: MitSystemOeffnen prüft File.Exists und " +
                    "liefert dafür immer false (Befund KI-D-B-2, iU9-W16c.3). Regelweg: " +
                    "Dienste.Datei.AdresseOeffnen.\n" + string.Join("\n", funde));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser erkennt das Muster — und lässt den richtigen Weg
    /// sowie einen echten Dateipfad in Ruhe.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_die_verwechselte_Adresse()
    {
        Assert.True(Verwechselt("Dienste.Datei.MitSystemOeffnen(DokuUebersetzung.FuerAnzeige(adresse));"));
        Assert.True(Verwechselt("if (eintrag.Url.Length > 0) Dienste.Datei.MitSystemOeffnen(eintrag.Url);"));
        Assert.False(Verwechselt("Dienste.Datei.AdresseOeffnen(adresse);"));
        Assert.False(Verwechselt("Dienste.Datei.MitSystemOeffnen(zieldatei);"));
        Assert.False(Verwechselt("try { Dienste.Datei.MitSystemOeffnen(pfad); } catch { }"));
    }

    /// <summary>Trägt dieser Ausschnitt einen Adressaufruf an <c>MitSystemOeffnen</c>?</summary>
    private static bool Verwechselt(string ausschnitt)
        => AufrufRegex.Matches(ausschnitt).Any(t => NachAdresse(t.Groups["arg"].Value));

    /// <summary>
    /// Sieht dieses Argument nach einer ADRESSE aus? Geprüft werden Bezeichnerteile, die
    /// im Haus nur an Adressen vorkommen; ein Dateipfad heißt <c>pfad</c>,
    /// <c>zieldatei</c> oder <c>datei</c>.
    /// </summary>
    private static bool NachAdresse(string argument)
    {
        string a = argument ?? "";
        return a.Contains("adresse", StringComparison.OrdinalIgnoreCase)
               || a.Contains("url", StringComparison.OrdinalIgnoreCase)
               || a.Contains("http", StringComparison.OrdinalIgnoreCase);
    }

    // =====================================================================
    //  Kein Bedienelement ohne Weg
    // =====================================================================

    /// <summary>
    /// <b>Kein Delegat der Gaben bleibt leer.</b> Ein <c>() =&gt; { }</c> oder ein
    /// <c>() =&gt; Task.CompletedTask</c> sieht im Parametersatz aus wie ein Weg, tut
    /// aber nichts — und der Knopf davor wird zu einem Knopf, der nichts tut. Genau das
    /// war die erste Vermutung zum Befund KI‑D‑B‑2.
    /// </summary>
    [Fact]
    public void Kein_Weg_des_Assistenten_ist_ein_leerer_Delegat()
    {
        string quelle = Lies("WindowsFormsApplication1", "Views", "Help", "KiChatHuelle.Gaben.cs");
        var funde = new List<string>();

        foreach (Match treffer in GabenRegex.Matches(quelle))
        {
            if (InKommentar(quelle, treffer.Index)) continue;
            string wert = treffer.Groups["wert"].Value;
            if (!LeerRegex.IsMatch(wert)) continue;

            funde.Add(treffer.Groups["schluessel"].Value + " (Zeile " +
                      Zeile(quelle, treffer.Index) + "): " + Einzeilig(wert));
        }

        Assert.True(funde.Count == 0,
                    "Diese Gaben tragen einen leeren Delegaten — das Bedienelement davor " +
                    "tut beim Anwender nichts (Befund KI-D-B-2):\n" + string.Join("\n", funde));
    }

    /// <summary>
    /// <b>Gegenprobe:</b> Der Leser erkennt einen leeren Delegaten und lässt einen
    /// echten in Ruhe.
    /// </summary>
    [Fact]
    public void Der_Leser_erkennt_einen_leeren_Delegaten()
    {
        Assert.Matches(LeerRegex, "(Func<Task>)(() => Task.CompletedTask)");
        Assert.Matches(LeerRegex, "(Action<string>)(_ => { })");
        Assert.DoesNotMatch(LeerRegex, "(Func<Task<string>>)ProtokollAsync");
        Assert.DoesNotMatch(LeerRegex, "(Func<bool>)(() => KiAusfuehrungWindows.Aktuell.Belegt)");
    }

    /// <summary>
    /// <b>Jedes Bedienelement des Assistenten hat seinen Weg in den Gaben.</b> Die
    /// Liste ist die Elementtabelle aus Auftrag #219 — Fußleiste, Eingabe,
    /// Überlagerungen und Rückwege.
    /// </summary>
    /// <remarks>
    /// Fehlt ein Schlüssel, bleibt der zugehörige Parameter der Komponente <c>null</c>,
    /// und der Klick verläuft still im Sand — der Dialog prüft jeden Delegaten vor dem
    /// Aufruf auf <c>null</c> (richtig so; ein Wurf wäre schlechter), und damit fällt
    /// das Fehlen ohne diese Wache niemandem auf.
    /// </remarks>
    [Fact]
    public void Jedes_Bedienelement_des_Assistenten_hat_seinen_Weg()
    {
        string quelle = Lies("WindowsFormsApplication1", "Views", "Help", "KiChatHuelle.Gaben.cs");

        HashSet<string> vorhanden = GabenRegex.Matches(quelle)
            .Where(t => !InKommentar(quelle, t.Index))
            .Select(t => t.Groups["schluessel"].Value)
            .ToHashSet(StringComparer.Ordinal);

        string[] pflicht =
        {
            "Fragen", "Suchen", "Einwilligen", "Ausfuehren",      // Eingabe und Werkzeuge
            "Vorschau", "Protokoll",                              // Fussleiste links
            "AdresseGewaehlt", "Kopieren",                        // Doku-Verweis, Verlauf kopieren
            "Rechtshinweisinhalt", "Einstellungsinhalt",          // die zwei Ueberlagerungen
            "Geschlossen", "UeberlagerungGeaendert", "Anmelden",  // Rueckwege
            "Belegt", "Aktionen", "Texte"                         // Zustand und Beschriftungen
        };

        string[] fehlen = pflicht.Where(s => !vorhanden.Contains(s)).ToArray();

        Assert.True(fehlen.Length == 0,
                    "Diese Wege fehlen im Parametersatz des Assistenten: " +
                    string.Join(", ", fehlen));
    }

    /// <summary>
    /// Die Adresse der Online-Dokumentation kommt aus der EINEN Quelle, die auch der
    /// Hilfekatalog und der Menüpunkt „Dokumentation" benutzen — nicht aus einem
    /// zweiten Einstellwert.
    /// </summary>
    [Fact]
    public void Die_Dokumentationsadresse_kommt_aus_der_Wikibasis()
    {
        string quelle = Lies("WindowsFormsApplication1", "Views", "Help", "KiChatHuelle.Gaben.cs");

        Assert.Contains("DokuAdresse = WikiWissen.Basis()", quelle, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Muster
    // =====================================================================

    /// <summary>
    /// Ein <c>MitSystemOeffnen</c> samt seinem Argument — ob das eine Adresse ist,
    /// entscheidet <see cref="NachAdresse"/> und nicht das Muster: Ein Argument kann
    /// geschachtelte Klammern tragen (<c>DokuUebersetzung.FuerAnzeige(adresse)</c>),
    /// und dafür ist ein regulärer Ausdruck das falsche Werkzeug.
    /// </summary>
    private static readonly Regex AufrufRegex = new(
        @"MitSystemOeffnen\s*\((?<arg>[^;\r\n]*)\)",
        RegexOptions.Compiled);

    /// <summary>Ein Eintrag des Parametersatzes: <c>["Schluessel"] = Wert,</c>.</summary>
    private static readonly Regex GabenRegex = new(
        @"\[""(?<schluessel>[A-Za-z][A-Za-z0-9_]*)""\]\s*=\s*(?<wert>[^\r\n]*)",
        RegexOptions.Compiled);

    /// <summary>Ein Delegat, der nichts tut.</summary>
    private static readonly Regex LeerRegex = new(
        @"(?:\(\s*\)|_|[A-Za-z_][A-Za-z0-9_]*)\s*=>\s*(?:\{\s*\}|Task\.CompletedTask|null)\s*\)?\s*,?\s*$",
        RegexOptions.Compiled);

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Der Textblock eines <c>case</c>-Zweiges: von der Marke bis zum nächsten
    /// <c>case</c> oder zum Ende.
    /// </summary>
    private static string Fallblock(string quelltext, string marke)
    {
        int anfang = quelltext.IndexOf("case " + marke, StringComparison.Ordinal);
        Assert.True(anfang >= 0, "Der Fall '" + marke + "' steht nicht mehr in der Tabelle.");

        int naechster = quelltext.IndexOf("case ", anfang + 5, StringComparison.Ordinal);
        return naechster < 0 ? quelltext.Substring(anfang)
                             : quelltext.Substring(anfang, naechster - anfang);
    }

    /// <summary>Die Zeilen einer Datei ohne Zeilen, die mit einem Kommentar beginnen.</summary>
    private static string[] OhneKommentare(string quelltext)
        => quelltext.Split('\n')
                    .Where(z => !z.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                && !z.TrimStart().StartsWith("*", StringComparison.Ordinal))
                    .ToArray();

    private static IEnumerable<string> Quelldateien()
        => Directory.EnumerateFiles(Path.Combine(Wurzel(), "WindowsFormsApplication1"),
                                    "*.cs", SearchOption.AllDirectories)
                    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
                                            StringComparison.Ordinal)
                                && !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
                                               StringComparison.Ordinal))
                    .OrderBy(p => p, StringComparer.Ordinal);

    private static string Lies(params string[] teile)
    {
        string pfad = Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());
        Assert.True(File.Exists(pfad), "Die Datei fehlt: " + pfad);
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

    /// <summary>
    /// Steht die Fundstelle in einem Kommentar? Die Hüllen ERKLÄREN ihre Befunde im
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
