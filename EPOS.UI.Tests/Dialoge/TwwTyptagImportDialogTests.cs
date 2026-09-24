using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using KiKern;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „VDI-4655-Typtage" (Umsetzungskonzept Zapfprofilgenerator 4.2, 5.7, Kapitel 6;
/// Stufe Z4b, Gruppe 2): die Anzeige des eingespielten Stands, der Leerzustand samt Grund, die
/// Paketwahl mit sofortiger Prüfung, der Bericht einer Ablehnung, das Einspielen mit Rückfrage,
/// das Löschen mit Rückfrage, die weichen Sperren, Esc wie Beenden, der Fall ohne Gaben und die
/// Sicht des Assistenten — dazu die Wache über das Textbündel.
///
/// <para>Die Datenseite kommt aus Prüfdelegaten; der Dialog schreibt nie selbst. Kultur de-DE,
/// alle Werte erfunden — kein Wert der Richtlinie steht in dieser Datei.</para>
/// </summary>
public class TwwTyptagImportDialogTests : EposBunitContext
{
    public TwwTyptagImportDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfstand (erfunden)
    // =================================================================================

    /// <summary>Ein erfundener Stand mit zwei Zonen, einer Gebäudeart und vier Typtagen.</summary>
    private static TwwTyptagStandDaten Voll() => new()
    {
        Vorhanden = true,
        Zeilen = 42,
        Quelle = "Probenrichtlinie",
        Ausgabe = "Ausgabe P",
        DatumImport = "2026-09-24",
        Klimazonen = new List<int> { 3, 7 },
        Gebaeudearten = new List<string> { "probehaus" },
        Typtage = new List<string> { "TT01", "TT02", "TT03", "TT04" },
        AufloesungenMin = new List<int> { 60 },
        MitTagesgaenge = true
    };

    private static TwwTyptagStandDaten Leer(string grund = "") => new() { Grund = grund };

    /// <summary>Der Prüfstand: er merkt sich, was gerufen wurde, und gibt vorbereitete Antworten.</summary>
    private sealed class Pruefstand
    {
        internal TwwTyptagStandDaten Stand = Leer();
        internal TwwTyptagPruefberichtDaten? Bericht;
        internal TwwTyptagErgebnisDaten? Einspielergebnis;
        internal TwwTyptagErgebnisDaten? Loeschergebnis;

        internal readonly List<string> Gewaehlt = new();
        internal readonly List<string> Geprueft = new();
        internal readonly List<string> Eingespielt = new();
        internal int Geloescht;

        internal TwwTyptagStandDaten StandLesen() => Stand;

        internal TwwTyptagPruefberichtDaten Pruefen(string pfad)
        {
            Geprueft.Add(pfad);
            return Bericht ?? Gut();
        }

        internal TwwTyptagErgebnisDaten Einspielen(string pfad)
        {
            Eingespielt.Add(pfad);
            TwwTyptagErgebnisDaten e = Einspielergebnis ?? new TwwTyptagErgebnisDaten
            {
                Ok = true,
                Meldung = "42 Zeile(n) eingespielt, 0 ersetzt.",
                Stand = Voll()
            };
            if (e.Ok) Stand = e.Stand;
            return e;
        }

        internal TwwTyptagErgebnisDaten Loeschen()
        {
            Geloescht++;
            TwwTyptagErgebnisDaten e = Loeschergebnis ?? new TwwTyptagErgebnisDaten
            {
                Ok = true,
                Meldung = "42 Zeile(n) entfernt.",
                Stand = Leer()
            };
            if (e.Ok) Stand = e.Stand;
            return e;
        }

        /// <summary>Ein Bericht, der das Paket annimmt.</summary>
        internal static TwwTyptagPruefberichtDaten Gut()
        {
            var b = new TwwTyptagPruefberichtDaten
            {
                Zusammenfassung = "Das Paket trägt 2 Klimazone(n), 1 Gebäudeart(en) und 4 Typtag(e) — 42 Zeile(n)."
            };
            b.Angaben.Add(new TwwTyptagAngabeDaten("Quelle", "Probenrichtlinie"));
            b.Angaben.Add(new TwwTyptagAngabeDaten("Klimazonen", "3, 7"));
            b.Hinweise.Add("Die Datei notizen.csv gehört nicht zum Paket und wurde übergangen.");
            return b;
        }

        /// <summary>Ein Bericht, der das Paket mit Datei und Zeile ablehnt.</summary>
        internal static TwwTyptagPruefberichtDaten Schlecht() => new()
        {
            Abgebrochen = true,
            Abbruch = "Das Paket ist abgelehnt: In typtage.csv, Zeile 4, fehlt die Spalte „tagart“."
        };
    }

    private IRenderedComponent<TwwTyptagImportDialog> Aufbauen(Pruefstand p, Action<bool>? geschlossen = null,
                                                              string paket = "C:/paket/typtage.zip")
        => Render<TwwTyptagImportDialog>(b => b
            .Add(x => x.Stand, p.StandLesen)
            .Add(x => x.PaketWaehlen, f => { p.Gewaehlt.Add(f); return Task.FromResult(paket); })
            .Add(x => x.Pruefen, p.Pruefen)
            .Add(x => x.Einspielen, p.Einspielen)
            .Add(x => x.Loeschen, p.Loeschen)
            .Add(x => x.Geschlossen, g => geschlossen?.Invoke(g)));

    private static IElement Knopf<T>(IRenderedComponent<T> cut, string text) where T : Microsoft.AspNetCore.Components.IComponent
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Der eingespielte Stand
    // =================================================================================

    [Fact]
    public void Der_eingespielte_Stand_steht_als_Tabelle_mit_acht_Zeilen()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        Assert.Equal(8, cut.FindAll(".epos-tww-typtage-stand tbody tr").Count);
        string text = cut.Find(".epos-tww-typtage-stand").TextContent;
        Assert.Contains("Probenrichtlinie", text);
        Assert.Contains("Ausgabe P", text);
        Assert.Contains("2026-09-24", text);
        Assert.Contains("3, 7", text);
        Assert.Contains("probehaus", text);
        Assert.Contains("TT01, TT02, TT03, TT04", text);
        Assert.Contains("60", text);
        Assert.Contains("42", text);

        // Der Lizenz- und der Formathinweis stehen als leise Zeilen da (Konzept Kapitel 6).
        string ganz = cut.Find(".epos-tww-typtage").TextContent;
        Assert.Contains("keine Werte der Richtlinie", ganz);
        Assert.Contains("ZIP-Archiv", ganz);
    }

    [Fact]
    public void Ohne_eingespielte_Typtage_steht_der_Grund_und_Loeschen_ist_weich_gesperrt()
    {
        var p = new Pruefstand { Stand = Leer("Diese Datenbank führt Tab_TwwTyptag_IMPORT nicht.") };
        var cut = Aufbauen(p);

        Assert.Empty(cut.FindAll(".epos-tww-typtage-stand"));
        Assert.Contains("führt Tab_TwwTyptag_IMPORT nicht", cut.Find(".epos-tww-typtage-leer").TextContent);

        IElement loeschen = Knopf(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        loeschen.Click();
        Assert.Equal(0, p.Geloescht);
        Assert.False(cut.Instance.FrageOffen);
        Assert.Contains("keine Typtage eingespielt", cut.Instance.Meldung);
    }

    [Fact]
    public void Ohne_Grund_steht_der_Leersatz_des_Buendels()
    {
        var cut = Aufbauen(new Pruefstand());
        Assert.Contains("keine Typtage eingespielt", cut.Find(".epos-tww-typtage-leer").TextContent);
    }

    // =================================================================================
    // Paketwahl und Prüfung
    // =================================================================================

    [Fact]
    public void Die_Paketwahl_prueft_sofort_und_zeigt_den_Bericht()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);

        // Ohne Paket ist „Einspielen" weich gesperrt und meldet den Grund.
        IElement einspielen = Knopf(cut, "Einspielen");
        Assert.Equal("true", einspielen.GetAttribute("aria-disabled"));
        einspielen.Click();
        Assert.Empty(p.Eingespielt);
        Assert.Contains("kein Paket gewählt", cut.Instance.Meldung);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Assert.Equal(new[] { "C:/paket/typtage.zip" }, p.Geprueft);
        Assert.Empty(p.Eingespielt);                       // die Prüfung schreibt NICHTS
        Assert.Equal(2, cut.FindAll(".epos-tww-typtage-pruefung tbody tr").Count);
        Assert.Contains("2 Klimazone(n)", cut.Find(".epos-tww-typtage-summe").TextContent);
        Assert.Contains("notizen.csv", cut.Find(".epos-tww-typtage-hinweis").TextContent);
        Assert.Null(Knopf(cut, "Einspielen").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Ein_abgelehntes_Paket_nennt_Datei_und_Zeile_und_sperrt_das_Einspielen()
    {
        var p = new Pruefstand { Bericht = Pruefstand.Schlecht() };
        var cut = Aufbauen(p);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.True(cut.Instance.Bericht?.Abgebrochen));
        Assert.Contains("typtage.csv, Zeile 4", cut.Find(".epos-warnbanner").TextContent);
        Assert.Empty(cut.FindAll(".epos-tww-typtage-pruefung"));

        Knopf(cut, "Einspielen").Click();
        Assert.Empty(p.Eingespielt);
        Assert.False(cut.Instance.FrageOffen);
    }

    // =================================================================================
    // Einspielen
    // =================================================================================

    [Fact]
    public void Einspielen_ohne_Stand_laeuft_ohne_Rueckfrage_durch()
    {
        var p = new Pruefstand();
        bool? geschlossen = null;
        var cut = Aufbauen(p, g => geschlossen = g);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();

        Assert.False(cut.Instance.FrageOffen);
        Assert.Equal(new[] { "C:/paket/typtage.zip" }, p.Eingespielt);
        Assert.True(cut.Instance.Standanzeige.Vorhanden);
        Assert.Contains("42 Zeile(n) eingespielt", cut.Instance.Status);
        Assert.Equal(8, cut.FindAll(".epos-tww-typtage-stand tbody tr").Count);

        Knopf(cut, "Beenden").Click();
        Assert.True(geschlossen);                          // der Wirt liest seine Wahllisten neu
    }

    [Fact]
    public void Einspielen_auf_einen_Stand_fragt_zurueck_und_Nein_aendert_nichts()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Contains("42 Zeilen", cut.Find(".epos-rueckfrage").TextContent);

        Knopf(cut, "Nein").Click();
        Assert.False(cut.Instance.FrageOffen);
        Assert.Empty(p.Eingespielt);

        Knopf(cut, "Einspielen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal(new[] { "C:/paket/typtage.zip" }, p.Eingespielt);
    }

    [Fact]
    public void Ein_abgelehntes_Einspielen_laesst_den_frueheren_Stand_stehen()
    {
        var p = new Pruefstand
        {
            Stand = Voll(),
            Einspielergebnis = new TwwTyptagErgebnisDaten
            {
                Ok = false,
                Meldung = "Das Paket ist abgelehnt: Die Summe der Kalendertage ist nicht 365.",
                Stand = Voll()
            }
        };
        var cut = Aufbauen(p);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();
        Knopf(cut, "Ja").Click();

        Assert.True(cut.Instance.Standanzeige.Vorhanden);
        Assert.Contains("nicht 365", cut.Find(".epos-warnbanner").TextContent);
        Assert.Equal("", cut.Instance.Status);
    }

    /// <summary>
    /// Ein gelungenes Einspielen mit Hinweisen zeigt sie: Der Prüfbericht wird durch den Bericht des
    /// Schreibwegs ersetzt, nicht weggeworfen — sonst sähe der Anwender nie, was übergangen wurde.
    /// </summary>
    [Fact]
    public void Einspielen_mit_Hinweis_zeigt_ihn()
    {
        var ergebnis = new TwwTyptagErgebnisDaten
        {
            Ok = true,
            Meldung = "42 Zeile(n) eingespielt, 0 ersetzt.",
            Stand = Voll()
        };
        ergebnis.Hinweise.Add("Die Spalte notiz war leer und wurde übergangen.");
        var p = new Pruefstand { Einspielergebnis = ergebnis };
        var cut = Aufbauen(p);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();

        Assert.Contains("42 Zeile(n) eingespielt", cut.Instance.Status);
        Assert.NotNull(cut.Instance.Bericht);
        Assert.Contains("notiz war leer", cut.Find(".epos-tww-typtage-hinweis").TextContent);
        // Der Hinweis der Prüfung ist weg — es steht nur noch der Bericht des Schreibwegs.
        Assert.Single(cut.FindAll(".epos-tww-typtage-hinweis"));
        Assert.Contains("42 Zeile(n) eingespielt", cut.Find(".epos-tww-typtage-summe").TextContent);
    }

    /// <summary>Ohne Hinweise verschwindet der Prüfbericht nach dem Einspielen.</summary>
    [Fact]
    public void Einspielen_ohne_Hinweis_nimmt_den_Pruefbericht_weg()
    {
        var p = new Pruefstand();
        var cut = Aufbauen(p);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Bericht));
        Knopf(cut, "Einspielen").Click();

        Assert.Null(cut.Instance.Bericht);
        Assert.Empty(cut.FindAll(".epos-tww-typtage-hinweis"));
    }

    // =================================================================================
    // Löschen
    // =================================================================================

    [Fact]
    public void Loeschen_fragt_zurueck_und_nimmt_den_Stand_weg()
    {
        var p = new Pruefstand { Stand = Voll() };
        bool? geschlossen = null;
        var cut = Aufbauen(p, g => geschlossen = g);

        Knopf(cut, "Löschen").Click();
        Assert.True(cut.Instance.FrageOffen);
        Assert.Contains("nicht mehr verfügbar", cut.Find(".epos-rueckfrage").TextContent);

        Knopf(cut, "Nein").Click();
        Assert.Equal(0, p.Geloescht);

        Knopf(cut, "Löschen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal(1, p.Geloescht);
        Assert.False(cut.Instance.Standanzeige.Vorhanden);
        Assert.Contains("42 Zeile(n) entfernt", cut.Instance.Status);

        Knopf(cut, "Beenden").Click();
        Assert.True(geschlossen);
    }

    // =================================================================================
    // Tastatur, Fall ohne Gaben
    // =================================================================================

    [Fact]
    public void Esc_beendet_nur_solange_keine_Rueckfrage_steht()
    {
        var p = new Pruefstand { Stand = Voll() };
        int beendet = 0;
        var cut = Aufbauen(p, _ => beendet++);

        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-tww-typtage").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(0, beendet);

        Knopf(cut, "Nein").Click();
        cut.Find(".epos-tww-typtage").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, beendet);
        Assert.False(cut.Instance.Standanzeige.Vorhanden == true && beendet == 0);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog_und_traegt_keinen_Schreibknopf()
    {
        var cut = Render<TwwTyptagImportDialog>();

        Assert.Contains("keine Typtage eingespielt", cut.Find(".epos-tww-typtage-leer").TextContent);
        Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Einspielen"));
        Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "Löschen"));
        Assert.NotNull(Knopf(cut, "Beenden"));
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    [Fact]
    public void Die_Maske_meldet_acht_Anzeigen_an_und_nimmt_keinen_Wert()
    {
        var p = new Pruefstand { Stand = Voll() };
        var cut = Aufbauen(p);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_TYPTAGE));
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.BRAUCHWASSER_TYPTAGE, feld)!;

        Assert.Equal("Probenrichtlinie", Zugang("quelle").Lesen());
        Assert.Equal("Ausgabe P", Zugang("ausgabe").Lesen());
        Assert.Equal("2026-09-24", Zugang("importdatum").Lesen());
        Assert.Equal("3, 7", Zugang("klimazonen").Lesen());
        Assert.Equal("probehaus", Zugang("gebaeudearten").Lesen());
        Assert.Equal(42, Zugang("zeilen").Lesen());
        Assert.Equal("", Zugang("grund").Lesen());

        // JEDES Feld ist nur lesend - es gibt keinen Setzweg (die Maske fuehrt keinen Einstellwert).
        foreach (string feld in new[] { "quelle", "ausgabe", "importdatum", "klimazonen",
                                        "gebaeudearten", "zeilen", "grund", "pruefbericht" })
            Assert.Null(Zugang(feld).Setzen);

        Knopf(cut, "Paket wählen…").Click();
        cut.WaitForAssertion(() => Assert.Contains("2 Klimazone(n)", Convert.ToString(Zugang("pruefbericht").Lesen()) ?? ""));

        cut.Instance.Dispose();
        Assert.False(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.BRAUCHWASSER_TYPTAGE));
    }

    // =================================================================================
    // Wache über das Textbündel
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \} = ""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    /// <summary>
    /// Jede Beschriftung des Bündels trägt ihren <c>ZPGT_</c>-Schlüssel, der steht in beiden
    /// Sprachen mit denselben Platzhaltern, und der Vorgabewert ist der Text der neutralen
    /// Ressource — sonst bliebe die Oberfläche in Englisch deutsch, und kein Test merkte es.
    /// </summary>
    [Fact]
    public void Jede_Beschriftung_des_Buendels_steht_mit_ihrem_Schluessel_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "Dialoge", "Bedarf", "TwwTyptagImportDaten.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).ToList();
        string[] eigenschaften = typeof(TwwTyptagImportTexte).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string)).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.True(eigenschaften.Length >= 30, "Nur " + eigenschaften.Length + " Beschriftungen.");
        Assert.Equal(eigenschaften, treffer.Select(m => m.Groups["name"].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        var vorgabe = new TwwTyptagImportTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string k = m.Groups["schluessel"].Value;
            if (!k.StartsWith("ZPGT_", StringComparison.Ordinal)) funde.Add(k + ": kein ZPGT_-Schlüssel");
            string? de = Resource.ResourceManager.GetString(k, CultureInfo.GetCultureInfo("de-DE"));
            string? en = Resource.ResourceManager.GetString(k, CultureInfo.GetCultureInfo("en-US"));
            if (string.IsNullOrWhiteSpace(de)) funde.Add(k + ": fehlt in Resource.resx");
            if (string.IsNullOrWhiteSpace(en)) funde.Add(k + ": fehlt in Resource.en-US.resx");
            if (de != null && en != null && Platzhalter(de) != Platzhalter(en))
                funde.Add(k + ": Platzhalter weichen ab (" + Platzhalter(de) + " / " + Platzhalter(en) + ")");

            string wert = typeof(TwwTyptagImportTexte).GetProperty(m.Groups["name"].Value)!.GetValue(vorgabe) as string ?? "";
            if (de != null && de != wert) funde.Add(k + ": Vorgabewert weicht von der Ressource ab");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    private static string Platzhalter(string muster)
        => string.Join(",", Regex.Matches(muster, @"\{(\d+)").Select(m => m.Groups[1].Value).Distinct().OrderBy(s => s, StringComparer.Ordinal));

    /// <summary>Die Repowurzel, von dieser Datei aus aufwärts gesucht.</summary>
    private static string Wurzel([CallerFilePath] string eigene = "")
    {
        DirectoryInfo? o = new FileInfo(eigene).Directory;
        while (o != null && !File.Exists(Path.Combine(o.FullName, "WP-Plan.sln"))) o = o.Parent;
        return o?.FullName ?? "";
    }
}
