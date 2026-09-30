using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Die Abdeckungswache der Platzhaltermarken</b> (BV-E6, Konzept Berichtsvorlagen 9.5): Jede
/// <c>Kennzahlkachel</c>, jedes <c>DiagrammSvg</c>, jede <c>Vergleichstabelle</c> und jeder Aufruf des
/// Helfers <c>@Kennzahl(…)</c> unter <c>EPOS.UI/Seiten/Berichte</c> und <c>EPOS.UI/Seiten/Simulation</c>
/// trägt ein <c>Vorlagenfeld</c> — oder steht hier als benannte Ausnahme mit Grund. Der Helfer nennt
/// seinen Schlüssel ausdrücklich (<c>vorlagenfeld:</c>); ein leerer Schlüssel ist eine Ausnahme wie
/// jede andere. Eine Ausnahme, die nichts mehr trifft, ist veraltet und macht die Wache rot.
///
/// <para>Die Direkttabellen der Seiten (Wirtschaftlichkeit, Übersicht, Anhang E) tragen ihre Marke am
/// Untertitel; die Wache hält die Liste ihrer Schlüssel in <see cref="Direkttabellen"/>. Die
/// <c>Vergleichstabelle</c> der Administrationsdialoge trägt keine Marke.</para>
/// </summary>
public class VorlagenfeldAbdeckungWacheTests
{
    /// <summary>
    /// Die benannten Ausnahmen: Datei (relativ zu <c>EPOS.UI/Seiten</c>), ein Anker, der im Elementtext
    /// steht, die Zahl der Treffer und der Grund.
    /// </summary>
    internal static readonly (string Datei, string Anker, int Anzahl, string Grund)[] Ausnahmen =
    {
        // ---- Simulation › Übersicht --------------------------------------------------------
        ("Simulation/UebersichtReiter.razor", "Bedarf?.WaermebedarfGesamtMwh", 1,
         "Leerzustand ohne Lauf: der Bedarf aus der Bedarfsrechnung, im Bericht gibt es ohne Ergebnis keinen Wert"),
        ("Simulation/UebersichtReiter.razor", "D.WaermedeckungProzent", 1,
         "Deckung der Wärme bzw. des Stroms über alle Erzeuger: der Katalog führt die Deckung je Kanal und die Autarkie, nicht diese Zahl"),
        ("Simulation/UebersichtReiter.razor", "simerg-ring-kaelte", 1,
         "kein Kältebild im Katalog: der Bericht zeigt die Kältedeckung als Kennzahlen"),
        // ---- Simulation › Ergebnis (die Erzeuger- und Bedarfsbilder tragen seit Katalog v10 stand.bild.*) ----
        ("Simulation/ErgebnisReiter.razor", "simerg-waermemonate", 1,
         "Wärme-Autarkie der Solarthermie je Monat: Direktdeckung und Speicheranteil führt der Zeitreihensatz des Berichts nicht"),
        ("Simulation/ErgebnisReiter.razor", "Resource.SIM_ERGEBNIS", 1,
         "Autarkieanalyse: CO₂-Ersparnis nach den Substitutionsfaktoren der Seite und Speichernutzen der Was-wäre-wenn-Kapazität, kein Katalogschlüssel"),
    };

    /// <summary>Die Direkttabellen der Seiten mit ihrer Marke (Konzept 9.5): Datei und Schlüssel.</summary>
    internal static readonly (string Datei, string Schluessel)[] Direkttabellen =
    {
        ("Berichte/WirtschaftlichkeitSeite.razor", "tabelle.varianten"),
        ("Berichte/WirtschaftlichkeitSeite.razor", "tabelle.wirtschaft.kennzahlen"),
        ("Berichte/WirtschaftlichkeitSeite.razor", "tabelle.wirtschaft.szenarien"),
        ("Berichte/WirtschaftlichkeitSeite.razor", "stand.tabelle.sensitivitaet"),
        ("Berichte/WirtschaftlichkeitSeite.razor", "stand.tabelle.mehrjahres"),
        ("Berichte/WirtschaftlichkeitSeite.razor", "tabelle.wirtschaft.nicht_monetaer"),
        ("Berichte/UebersichtSeite.razor", "tabelle.komponenten.matrix"),
        ("Berichte/AnhangEChecklisteKnopf.razor", "tabelle.anhang_e.checkliste"),
        ("Simulation/HeizkesselReiter.razor", "stand.tabelle.heizkessel"),
    };

    /// <summary>
    /// Die reinen Excel-Tabellen (Katalog v7 und v9) und die Stelle, an der die App ihren Inhalt zeigt: Datei (relativ zur
    /// Repowurzel), in der der Schlüssel steht, und die Ressource des Hinweises „nur Excel“.
    /// </summary>
    internal static readonly (string Datei, string Schluessel, string Hinweis)[] NurExcel =
    {
        ("EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor", "tabelle.wirtschaft.parameter", "VF_ORT_NUR_EXCEL_PARAMETER"),
        ("EPOS.UI/Seiten/Berichte/KapitalwertVerlaufAbschnitt.razor", "tabelle.wirtschaft.verlauf", "VF_ORT_NUR_EXCEL_VERLAUF"),
        ("EPOS.UI/Seiten/Berichte/KostenSeite.razor", "tabelle.vergleich.liste", "VF_ORT_NUR_EXCEL_VERGLEICH"),
        ("EPOS.UI/Seiten/Simulation/UebersichtReiter.razor", "stand.tabelle.kennzahlen.liste", "VF_ORT_NUR_EXCEL_KENNZAHLEN"),
        ("EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.cs", "stand.tabelle.monatswerte", "VF_ORT_NUR_EXCEL_MONATSWERTE"),
    };

    private static readonly Regex Element = new(@"<(Kennzahlkachel|DiagrammSvg|Vergleichstabelle)\b", RegexOptions.Compiled);

    // =====================================================================
    //  Die Wache
    // =====================================================================

    [Fact]
    public void Jedes_Element_traegt_ein_Vorlagenfeld_oder_steht_als_Ausnahme()
    {
        string seiten = Path.Combine(Wurzel(), "EPOS.UI", "Seiten");
        var dateien = new List<string>();
        foreach (string ordner in new[] { "Berichte", "Simulation" })
            dateien.AddRange(Directory.GetFiles(Path.Combine(seiten, ordner), "*.razor"));
        Assert.NotEmpty(dateien);

        var offen = new List<(string Datei, string Text)>();
        int gedeckt = 0;
        foreach (string pfad in dateien)
        {
            string rel = Path.GetRelativePath(seiten, pfad).Replace('\\', '/');
            foreach ((string text, bool traegt) in Fundstellen(File.ReadAllText(pfad)))
            {
                if (traegt) gedeckt++;
                else offen.Add((rel, text));
            }
        }

        var fehler = new List<string>();
        foreach ((string datei, string anker, int anzahl, string grund) in Ausnahmen)
        {
            Assert.False(string.IsNullOrWhiteSpace(grund), datei + " " + anker + ": Ausnahme ohne Grund");
            int treffer = offen.RemoveAll(o => o.Datei == datei && o.Text.Contains(anker, StringComparison.Ordinal));
            if (treffer != anzahl) fehler.Add($"Ausnahme {datei} „{anker}“: erwartet {anzahl}, getroffen {treffer} (veraltet?)");
        }
        foreach ((string datei, string text) in offen)
            fehler.Add($"{datei}: ohne Vorlagenfeld und ohne Ausnahme — {Kurz(text)}");

        Assert.True(fehler.Count == 0, string.Join(Environment.NewLine, fehler));
        Assert.True(gedeckt >= 15, gedeckt + " markierte Elemente — zu wenig, die Wache sieht die Seiten nicht");
    }

    [Fact]
    public void Die_Direkttabellen_der_Seiten_tragen_ihre_Marke()
    {
        string seiten = Path.Combine(Wurzel(), "EPOS.UI", "Seiten");
        foreach ((string datei, string schluessel) in Direkttabellen)
        {
            string text = File.ReadAllText(Path.Combine(seiten, datei));
            Assert.True(text.Contains("\"" + schluessel + "\"", StringComparison.Ordinal), datei + " trägt " + schluessel + " nicht");
        }
    }

    /// <summary>
    /// Die reinen Excel-Tabellen tragen dort, wo die App ihren Inhalt zeigt, eine Marke mit dem Hinweis „nur Excel“: Der
    /// Schlüssel hat keine Ausgabe Word, steht in der Ortstabelle und an seiner Stelle, und der Hinweis wird benutzt.
    /// </summary>
    [Fact]
    public void Die_reinen_Excel_Tabellen_tragen_eine_Marke_nur_Excel()
    {
        string wurzel = Wurzel();
        string seiten = string.Join("\n", Directory.GetFiles(Path.Combine(wurzel, "EPOS.UI", "Seiten"), "*.razor", SearchOption.AllDirectories)
                                              .Select(File.ReadAllText));
        foreach ((string datei, string schluessel, string hinweis) in NurExcel)
        {
            WindowsFormsApplication1.Vorlagenfeld? feld = WindowsFormsApplication1.Vorlagenfeldkatalog.Finde(schluessel);
            Assert.True(feld is not null, schluessel + " ist kein Katalogschlüssel");
            Assert.True((feld!.Ausgaben & WindowsFormsApplication1.Vorlagenausgabe.Word) == 0, schluessel + " hat eine Ausgabe Word");
            Assert.NotEmpty(EPOS.UI.Dienste.Vorlagenfeldorte.Finde(schluessel));
            string text = File.ReadAllText(Path.Combine(wurzel, datei.Replace('/', Path.DirectorySeparatorChar)));
            Assert.True(text.Contains("\"" + schluessel + "\"", StringComparison.Ordinal), datei + " nennt " + schluessel + " nicht");
            Assert.True(seiten.Contains("Resource." + hinweis, StringComparison.Ordinal), hinweis + " steht an keiner Marke");
        }
    }

    [Fact]
    public void Die_Vergleichstabelle_der_Administrationsdialoge_traegt_keine_Marke()
    {
        string ui = Path.Combine(Wurzel(), "EPOS.UI");
        foreach (string pfad in Directory.GetFiles(ui, "*.razor", SearchOption.AllDirectories)
                                         .Where(p => !p.Replace('\\', '/').Contains("/Seiten/", StringComparison.Ordinal)))
            foreach ((string text, bool traegt) in Fundstellen(File.ReadAllText(pfad)))
                if (text.StartsWith("<Vergleichstabelle", StringComparison.Ordinal))
                    Assert.False(traegt, Path.GetFileName(pfad) + ": " + Kurz(text));
    }

    // =====================================================================
    //  Gegenproben: die Wache sieht, was sie sehen soll
    // =====================================================================

    [Fact]
    public void Gegenprobe_die_Wache_erkennt_Elemente_mit_und_ohne_Vorlagenfeld()
    {
        const string probe = """
            <Kennzahlkachel Titel="@k.Titel" Wert="@k.Wert" />
            <Kennzahlkachel Titel="a" Wert="b" Vorlagenfeld="@k.Vorlagenfeld" />
            <DiagrammSvg Modell="@M" Kennung="@($"x-{a}")"
                         Vorlagenfeld="bild.wirtschaft.spanne" />
            <Vergleichstabelle Koepfe="@K" Zeilen="@Z" />
            @Kennzahl(Resource.A, Zahl(x, "N2"), MWH, false, vorlagenfeld: D.Feld)
            @Kennzahl(Resource.B, Zahl(y, "N2"), MWH, false, vorlagenfeld: "")
            @Kennzahl(Resource.C, Zahl(z, "N2"), MWH, false)
            """;
        List<(string Text, bool Traegt)> f = Fundstellen(probe).ToList();
        Assert.Equal(7, f.Count);
        Assert.Equal(new[] { false, true, true, false, true, false, false }, f.Select(x => x.Traegt));
    }

    // =====================================================================
    //  Helfer
    // =====================================================================

    /// <summary>
    /// Die Fundstellen eines Razor-Texts: jedes Element bis zu seinem <c>/&gt;</c> und jeder Aufruf
    /// <c>@Kennzahl(…)</c> mit ausgeglichenen Klammern — je mit der Angabe, ob ein Schlüssel gesetzt ist.
    /// </summary>
    internal static IEnumerable<(string Text, bool Traegt)> Fundstellen(string razor)
    {
        var treffer = new List<(int Stelle, string Text, bool Traegt)>();
        foreach (Match m in Element.Matches(razor))
        {
            int ende = razor.IndexOf("/>", m.Index, StringComparison.Ordinal);
            string text = ende < 0 ? razor.Substring(m.Index) : razor.Substring(m.Index, ende + 2 - m.Index);
            treffer.Add((m.Index, text, Regex.IsMatch(text, @"\sVorlagenfeld=""[^""]+""")));
        }
        int von = 0;
        while ((von = razor.IndexOf("@Kennzahl(", von, StringComparison.Ordinal)) >= 0)
        {
            int tiefe = 0, i = von + "@Kennzahl".Length;
            for (; i < razor.Length; i++)
            {
                if (razor[i] == '(') tiefe++;
                else if (razor[i] == ')' && --tiefe == 0) break;
            }
            string text = razor.Substring(von, Math.Min(razor.Length, i + 1) - von);
            bool traegt = Regex.IsMatch(text, @"vorlagenfeld:\s*[^\s""),]");
            treffer.Add((von, text, traegt));
            von = i;
        }
        return treffer.OrderBy(t => t.Stelle).Select(t => (t.Text, t.Traegt));
    }

    private static string Kurz(string text)
    {
        string eine = Regex.Replace(text, @"\s+", " ");
        return eine.Length > 140 ? eine.Substring(0, 140) + "…" : eine;
    }

    internal static string Wurzel()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
