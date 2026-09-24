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
/// Die Datenseite der Überlagerung „Auslegung" (Umsetzungskonzept Zapfprofilgenerator 5.1, 5.3;
/// Stufe Z2, Gruppe 2): die DTO aus <c>ZapfprofilDaten.cs</c> und das Textbündel
/// <see cref="ZapfprofilAuslegungTexte"/>.
///
/// <para><b>Was hier geprüft wird.</b> Die Vorgaben (Vorgaberegel, Ladespeicher, keine
/// Laufangabe), dass die Überlagerung auf unabhängigen Kopien arbeitet (Abbrechen verwirft) samt
/// Entwurf, dass ein Ergebnis ohne Rechnung keinen Punkt vortäuscht und dass der Punkt die erste
/// rechenbare Speichergruppe ist — und als Wache, dass JEDE Eigenschaft des Bündels ihren Schlüssel
/// im Kommentar trägt, der Schlüssel in BEIDEN Sprachen mit denselben Platzhaltern steht und der
/// Vorgabewert der deutsche Ressourcentext ist.</para>
/// </summary>
public sealed class ZapfprofilAuslegungDatenTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose() => _kultur.Dispose();

    private static readonly CultureInfo DE = new("de-DE");
    private static readonly CultureInfo EN = new("en-US");

    [Fact]
    public void Die_Vorgaben_sind_Vorgaberegel_Ladespeicher_und_keine_Laufangabe()
    {
        var a = new ZapfprofilAuslegungEingabeDaten();
        Assert.Equal(ZapfprofilBedarfstagquelle.Vorgaberegel, a.Quelle);
        Assert.Equal(ZapfprofilSpeicherart.Ladespeicher, a.Speicherart);
        Assert.Equal(ZapfprofilErzeugerart.KeineAngabe, a.Erzeugerart);
        Assert.Equal(ZapfprofilWerkstoff.KeineAngabe, a.Werkstoff);
        Assert.Null(a.SpeicherC);
        Assert.Null(a.PunktVolumenL);
        Assert.Null(new ZapfprofilEingabeDaten().Auslegung);

        // Die Zahlen der Aufzählungen sind die des Kerns.
        Assert.Equal(new[] { 0, 1, 2, 3, 4, 5 }, Enum.GetValues<ZapfprofilBedarfstagquelle>().Select(q => (int)q).ToArray());
        Assert.Equal(new[] { 1, 2 }, Enum.GetValues<ZapfprofilSpeicherart>().Select(q => (int)q).ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4 }, Enum.GetValues<ZapfprofilKartenstand>().Select(q => (int)q).ToArray());
    }

    [Fact]
    public void Die_Kopie_ist_unabhaengig_samt_Entwurf()
    {
        var a = new ZapfprofilAuslegungEingabeDaten
        {
            Quelle = ZapfprofilBedarfstagquelle.Konstruktor,
            SpeicherC = 60,
            Werkstoff = ZapfprofilWerkstoff.Stahl,
            Entwurf = new ZapfprofilBedarfstagDaten
            {
                Bezeichner = "Tag", Ereignisse = { new ZapfprofilEreignisDaten(420, 10, 1.0) }
            }
        };
        var e = new ZapfprofilEingabeDaten { Auslegung = a };

        ZapfprofilEingabeDaten k = e.Kopie();
        k.Auslegung!.SpeicherC = 50;
        k.Auslegung.Entwurf!.Bezeichner = "anders";
        k.Auslegung.Entwurf.Ereignisse.Clear();

        Assert.Equal(60, a.SpeicherC);
        Assert.Equal("Tag", a.Entwurf.Bezeichner);
        Assert.Single(a.Entwurf.Ereignisse);
        Assert.Equal(ZapfprofilWerkstoff.Stahl, a.Kopie().Werkstoff);

        var zeile = new ZapfprofilKonstruktorZeileDaten { BeginnH = 7, EndeH = 8, Regel = "R", Anzahl = 2, Verbraucher = "Bad" };
        ZapfprofilKonstruktorZeileDaten z = zeile.Kopie();
        z.Anzahl = 5;
        Assert.Equal(2, zeile.Anzahl);
        Assert.Equal("Bad", z.Verbraucher);
    }

    /// <summary>
    /// Stufe Z3: „Stochastisch rechnen", Perzentil und Realisierungen gehen mit der Kopie; ohne
    /// Angabe steht die Vorgabe (<c>null</c>), der Schalter aus. Der Wert des Perzentils ist die
    /// Zeile des gewählten p im Streuband; ohne Streuband NaN, ohne Lauf kein Ergebnis.
    /// </summary>
    [Fact]
    public void Stochastik_Perzentil_und_Realisierungen_gehen_mit_der_Kopie()
    {
        var leer = new ZapfprofilAuslegungEingabeDaten();
        Assert.False(leer.Stochastisch);
        Assert.Null(leer.Perzentil);
        Assert.Null(leer.RealisierungenAuslegung);
        Assert.Null(new ZapfprofilAuslegungsgruppeDaten().PerzentilErgebnis);
        Assert.Empty(new ZapfprofilAuslegungStartDaten().Perzentile);

        var a = new ZapfprofilAuslegungEingabeDaten { Stochastisch = true, Perzentil = 95, RealisierungenAuslegung = 40 };
        ZapfprofilAuslegungEingabeDaten k = a.Kopie();
        Assert.True(k.Stochastisch);
        Assert.Equal(95, k.Perzentil);
        Assert.Equal(40, k.RealisierungenAuslegung);
        k.Perzentil = 99;
        Assert.Equal(95, a.Perzentil);

        var p = new ZapfprofilPerzentilDaten
        {
            Perzentil = 95,
            Streuband = { new(50, 10), new(90, 20), new(95, 25), new(99, double.PositiveInfinity) }
        };
        Assert.Equal(25, p.Wert);
        p.Perzentil = 99;
        Assert.True(double.IsPositiveInfinity(p.Wert));
        Assert.True(double.IsNaN(new ZapfprofilPerzentilDaten { Perzentil = 99 }.Wert));
    }

    /// <summary>Hausregel „kein vorbelegtes DTO als Ergebnis": ohne Rechnung kein Punkt, aber ein Zustand.</summary>
    [Fact]
    public void Ohne_Rechnung_gibt_es_keinen_Punkt_und_der_Punkt_ist_die_erste_rechenbare_Speichergruppe()
    {
        var d = new ZapfprofilAuslegungDaten();
        Assert.Equal(ZapfprofilAuslegungZustand.NichtGerechnet, d.Zustand);
        Assert.Null(d.Punktgruppe);
        Assert.Equal(ZapfprofilKartenstand.NichtGerechnet, new ZapfprofilKarteDaten().Stand);
        Assert.Null(new ZapfprofilAuslegungStartDaten().Ergebnis);

        var durchfluss = new ZapfprofilAuslegungsgruppeDaten { Empfehlung = { Rechenbar = true, LeistungKw = 30 } };
        var speicherOhne = new ZapfprofilAuslegungsgruppeDaten { Speicher = true };
        var speicher = new ZapfprofilAuslegungsgruppeDaten { Speicher = true, Empfehlung = { Rechenbar = true, Speicher = true, VolumenL = 300 } };
        d.Gruppen.AddRange(new[] { durchfluss, speicherOhne, speicher });
        Assert.Same(speicher, d.Punktgruppe);
        d.Gruppen.Remove(speicher);
        Assert.Same(durchfluss, d.Punktgruppe);
        Assert.Equal(40.0, new ZapfprofilRegelDaten("R", 10, 4, 40).VolumenJeVorgangL);
    }

    // =================================================================================
    //  Wache über das Textbündel
    // =================================================================================

    private static readonly Regex Eigenschaft = new(
        @"///\s*<summary><c>(?<schluessel>[A-Z0-9_]+)</c></summary>\s*\r?\n\s*public string (?<name>\w+) \{ get; set; \} = ""(?<wert>(?:[^""\\]|\\.)*)"";",
        RegexOptions.Compiled);

    [Fact]
    public void Jede_Beschriftung_steht_mit_ihrem_Schluessel_in_beiden_Sprachen()
    {
        string quelle = File.ReadAllText(Pfad("EPOS.UI", "Dialoge", "Bedarf", "ZapfprofilAuslegungTexte.cs"));
        List<Match> treffer = Eigenschaft.Matches(quelle).ToList();

        string[] eigenschaften = typeof(ZapfprofilAuslegungTexte)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.True(eigenschaften.Length >= 100, "Nur " + eigenschaften.Length + " Beschriftungen im Bündel.");
        Assert.Equal(eigenschaften, treffer.Select(m => m.Groups["name"].Value).OrderBy(n => n, StringComparer.Ordinal).ToArray());

        var vorgabe = new ZapfprofilAuslegungTexte();
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string schluessel = m.Groups["schluessel"].Value;
            string name = m.Groups["name"].Value;
            string deutsch = Resource.ResourceManager.GetString(schluessel, DE) ?? "";
            string englisch = Resource.ResourceManager.GetString(schluessel, EN) ?? "";
            string rueckfall = (string)typeof(ZapfprofilAuslegungTexte).GetProperty(name)!.GetValue(vorgabe)!;

            if (deutsch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.resx");
            if (englisch.Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
            if (!string.Equals(deutsch, rueckfall, StringComparison.Ordinal))
                funde.Add(schluessel + ": Rückfall „" + rueckfall + "“ ≠ Ressource „" + deutsch + "“");
            if (!schluessel.StartsWith("ZPG_AUS_", StringComparison.Ordinal))
                funde.Add(schluessel + ": kein ZPG_AUS_-Schlüssel");
            if (!Platzhalter(deutsch).SequenceEqual(Platzhalter(englisch)))
                funde.Add(schluessel + ": Platzhalter weichen ab");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    private static string[] Platzhalter(string text)
        => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

    private static string Pfad(params string[] teile) => Path.Combine(new[] { Wurzel() }.Concat(teile).ToArray());

    private static string Wurzel([CallerFilePath] string eigeneDatei = "")
    {
        string? ordner = Path.GetDirectoryName(eigeneDatei);
        while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
            ordner = Path.GetDirectoryName(ordner);
        Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!;
    }
}
