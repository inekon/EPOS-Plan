using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Datenseite des Dialogs „Brauchwasser-Zapfprofil" (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.3; Stufe Z1, Gruppe 3): die DTO aus
/// <c>ZapfprofilDaten.cs</c> und das Textbündel <see cref="ZapfprofilTexte"/>.
///
/// <para><b>Was hier geprüft wird.</b> Die Vorgaben der DTO (Stufe Einfach, Niveau mittel, Weg
/// Generator), dass der Dialog auf unabhängigen Kopien arbeitet (Abbrechen verwirft), dass die
/// Rückgabe ihren Weg aus dem Arbeitsstand nimmt, dass eine Vorschau ohne Rechnung keine
/// Summe vortäuscht — und als Wache, dass JEDE Eigenschaft des Bündels ihren Schlüssel im
/// Kommentar trägt, der Schlüssel in BEIDEN Sprachen steht und der Vorgabewert der deutsche
/// Ressourcentext ist. Die Abbildung auf den Arbeitsstand des Kerns prüft
/// <c>EPOS.Kern.Tests/ZapfprofilHuelleTests</c> (die Hülle ist nur dort sichtbar).</para>
///
/// <para>Kulturpinnung: Die Ressourcen werden mit ausdrücklicher Kultur gelesen; die
/// Vorrichtung pinnt zusätzlich de-DE für alles Übrige.</para>
/// </summary>
public sealed class ZapfprofilDatenTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose() => _kultur.Dispose();

    private static readonly CultureInfo DE = new("de-DE");
    private static readonly CultureInfo EN = new("en-US");

    [Fact]
    public void Die_Vorgaben_sind_die_der_Stufe_Einfach()
    {
        var daten = new ZapfprofilDaten();
        Assert.Equal(ZapfprofilStufe.Einfach, daten.Stufe);
        Assert.Equal(ZapfprofilWeg.Generator, daten.Eingabe.Weg);
        Assert.Empty(daten.Eingabe.Zonen);
        Assert.Null(daten.Vorschau);
        Assert.False(daten.Verfuegbar);

        var zone = new ZapfprofilZoneDaten();
        Assert.Equal(ZapfprofilNiveau.Mittel, zone.Niveau);
        Assert.Null(zone.Bezugsmenge);
        Assert.Equal(0, zone.IdNutzungsart);

        // Die Niveaus tragen die Zahlen des Kerns (1 … 3).
        Assert.Equal(new[] { 1, 2, 3 },
            Enum.GetValues<ZapfprofilNiveau>().Select(n => (int)n).ToArray());
    }

    [Fact]
    public void Die_Kopie_ist_unabhaengig()
    {
        var e = new ZapfprofilEingabeDaten
        {
            Weg = ZapfprofilWeg.Bestand,
            Zonen =
            {
                new ZapfprofilZoneDaten { Id = 4, Name = "Zone 1", IdNutzungsart = 7, Bezugsmenge = 20, Niveau = ZapfprofilNiveau.Hoch, Ueberschrieben = 2 },
                new ZapfprofilZoneDaten { IdVorlage = 4, Name = "Zone 1 (Kopie)", IdNutzungsart = 7, Bezugsmenge = 20 }
            }
        };
        ZapfprofilEingabeDaten k = e.Kopie();

        k.Weg = ZapfprofilWeg.Generator;
        k.Zonen[0].Name = "geändert";
        k.Zonen[0].Bezugsmenge = 99;
        k.Zonen.RemoveAt(1);

        Assert.Equal(ZapfprofilWeg.Bestand, e.Weg);
        Assert.Equal(2, e.Zonen.Count);
        Assert.Equal("Zone 1", e.Zonen[0].Name);
        Assert.Equal(20.0, e.Zonen[0].Bezugsmenge);

        // Jedes Feld geht mit — auch Vorlage und Zähler.
        ZapfprofilZoneDaten z = e.Zonen[1].Kopie();
        Assert.Equal(4, z.IdVorlage);
        Assert.Equal(2, e.Zonen[0].Kopie().Ueberschrieben);
        Assert.Equal(ZapfprofilNiveau.Hoch, e.Zonen[0].Kopie().Niveau);
    }

    [Fact]
    public void Das_Ergebnis_nimmt_den_Weg_aus_dem_Arbeitsstand_und_der_Zaehler_summiert_die_Zonen()
    {
        var e = new ZapfprofilEingabeDaten { Weg = ZapfprofilWeg.Bestand };
        Assert.Equal(ZapfprofilWeg.Bestand, new ZapfprofilErgebnisDaten(e).Weg);
        e.Weg = ZapfprofilWeg.Generator;
        Assert.Equal(ZapfprofilWeg.Generator, new ZapfprofilErgebnisDaten(e).Weg);

        var daten = new ZapfprofilDaten
        {
            Eingabe =
            {
                Zonen = { new ZapfprofilZoneDaten { Ueberschrieben = 2 }, new ZapfprofilZoneDaten { Ueberschrieben = 3 } }
            }
        };
        Assert.Equal(5, daten.Ueberschrieben);
    }

    /// <summary>Hausregel „kein vorbelegtes DTO als Ergebnis": ohne Rechnung keine Summe, aber ein Zustand.</summary>
    [Fact]
    public void Eine_Vorschau_ohne_Rechnung_hat_keine_Summe()
    {
        var v = new ZapfprofilVorschauDaten();
        Assert.Equal(ZapfprofilVorschauZustand.NichtGerechnet, v.Zustand);
        Assert.Null(v.Summe);

        var a = new ZapfprofilAnsichtDaten { Titel = "Summe" };
        v.Ansichten.Add(a);
        Assert.Same(a, v.Summe);
        Assert.Null(a.WerktagKw);
        Assert.Null(a.TagesgangModell);
        Assert.Null(new ZapfprofilKennzahlenDaten().GroessterStundenwertKw);
    }

    // =================================================================================
    //  Wache über das Textbündel
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \} = ""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    /// <summary>
    /// Jede Zeichenketten-Eigenschaft von <see cref="ZapfprofilTexte"/> trägt ihren Schlüssel im
    /// Kommentar; der Schlüssel steht in beiden Sprachen, und der Vorgabewert ist der deutsche
    /// Text der neutralen Ressource — ein Rückfall, der nicht vom Katalog abweicht.
    /// </summary>
    [Fact]
    public void Jede_Beschriftung_steht_mit_ihrem_Schluessel_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", "ZapfprofilTexte.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).ToList();

        string[] eigenschaften = typeof(ZapfprofilTexte)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.True(eigenschaften.Length >= 80, "Nur " + eigenschaften.Length + " Beschriftungen im Bündel.");
        Assert.Equal(eigenschaften, treffer.Select(m => m.Groups["name"].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        var vorgabe = new ZapfprofilTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string schluessel = m.Groups["schluessel"].Value;
            string name = m.Groups["name"].Value;
            string deutsch = Resource.ResourceManager.GetString(schluessel, DE) ?? "";
            string englisch = Resource.ResourceManager.GetString(schluessel, EN) ?? "";
            string rueckfall = (string)typeof(ZapfprofilTexte).GetProperty(name)!.GetValue(vorgabe)!;

            if (deutsch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.resx");
            if (englisch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
            if (!string.Equals(deutsch, rueckfall, StringComparison.Ordinal))
                funde.Add(schluessel + ": Rückfall „" + rueckfall + "“ ≠ Ressource „" + deutsch + "“");
            if (!schluessel.StartsWith("ZPG_", StringComparison.Ordinal))
                funde.Add(schluessel + ": kein ZPG_-Schlüssel");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    /// <summary>Die Platzhalter stimmen in beiden Sprachen überein — sonst fällt ein Wert weg.</summary>
    [Fact]
    public void Die_Platzhalter_der_Beschriftungen_gleichen_sich_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", "ZapfprofilTexte.cs"));
        var funde = new List<string>();
        foreach (Match m in Eigenschaft.Matches(quelle))
        {
            string schluessel = m.Groups["schluessel"].Value;
            string de = Resource.ResourceManager.GetString(schluessel, DE) ?? "";
            string en = Resource.ResourceManager.GetString(schluessel, EN) ?? "";
            if (!Platzhalter(de).SequenceEqual(Platzhalter(en)))
                funde.Add(schluessel + ": „" + de + "“ / „" + en + "“");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    private static string[] Platzhalter(string text)
        => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

    private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

    /// <summary>Die Wurzel des Arbeitsbaums — über den Pfad dieser Quelldatei, wie die übrigen Wachen.</summary>
    private static string Wurzel([CallerFilePath] string eigeneDatei = "")
    {
        string? ordner = Path.GetDirectoryName(eigeneDatei);
        while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
            ordner = Path.GetDirectoryName(ordner);
        Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!;
    }
}
