using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Die Bündelwache der Berichtsvorlagen</b> (Etappe BV-E1): Jeder Schlüssel, den eines der fünf
/// Textbündel nennt — <see cref="BerichtSeiteVorlagentexte"/>, <see cref="PrueflisteTexte"/>,
/// <see cref="PlatzhalterkatalogTexte"/>, <see cref="EinstellungenBerichtTexte"/>, <c>VorlagenfeldTexte</c> —, steht in BEIDEN
/// Ressourcendateien mit einem Text; der deutsche Rückfall im Bündel ist der Text der neutralen
/// Ressource, und die Platzhalter <c>{n}</c> gleichen sich in beiden Sprachen.
///
/// <para><b>Warum.</b> Ein Bündel fällt still auf seinen deutschen Rückfall zurück, wenn ein
/// Schlüssel fehlt — die Oberfläche bliebe dann auch in Englisch deutsch, und kein anderer Test
/// merkte es. Gelesen wird der Quelltext (<c>T("SCHLUESSEL", "Rückfall")</c>), die englischen Texte
/// OHNE Rückgriff auf die neutrale Ressource (<c>GetResourceSet(…, tryParents: false)</c>) — sonst
/// hielte der deutsche Text eine fehlende Übersetzung für vorhanden. Muster:
/// <c>ZapfprofilDatenTests</c>.</para>
/// </summary>
public sealed class BerichtsvorlagenTextbuendelWacheTests
{
    private static readonly Regex Aufruf = new(
        @"\bT\(\s*""(?<k>[A-Z0-9_]+)""\s*,\s*""(?<v>(?:[^""\\]|\\.)*)""\s*\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Die fünf Bündel: Quelldatei und Klasse (die fünfte mit BV-E6: <see cref="EPOS.UI.Bausteine.VorlagenfeldTexte"/>).</summary>
    public static TheoryData<string, Type> Buendel() => new()
    {
        { "EPOS.UI/Seiten/Berichte/BerichtSeiteVorlagentexte.cs", typeof(BerichtSeiteVorlagentexte) },
        { "EPOS.UI/Dialoge/Berichte/PrueflisteTexte.cs", typeof(PrueflisteTexte) },
        { "EPOS.UI/Dialoge/Berichte/PlatzhalterkatalogTexte.cs", typeof(PlatzhalterkatalogTexte) },
        { "EPOS.UI/Dialoge/Admin/EinstellungenBerichtTexte.cs", typeof(EinstellungenBerichtTexte) },
        // BV-E6: Marke, Umschalter und Zeile der Platzhalteranzeige.
        { "EPOS.UI/Bausteine/VorlagenfeldTexte.cs", typeof(EPOS.UI.Bausteine.VorlagenfeldTexte) },
    };

    [Theory]
    [MemberData(nameof(Buendel))]
    public void Jeder_Schluessel_des_Buendels_steht_in_beiden_Sprachen(string datei, Type buendel)
    {
        string quelle = File.ReadAllText(Pfad(datei));
        List<Match> treffer = Aufruf.Matches(quelle).ToList();

        // Jede Beschriftung des Bündels füllt sich über GENAU einen Aufruf mit Schlüssel.
        int beschriftungen = buendel.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Count(p => p.PropertyType == typeof(string));
        Assert.True(beschriftungen >= 8, buendel.Name + ": nur " + beschriftungen + " Beschriftungen");
        Assert.Equal(beschriftungen, treffer.Count);

        ResourceSet deutsch = Satz(CultureInfo.InvariantCulture);
        ResourceSet englisch = Satz(CultureInfo.GetCultureInfo("en-US"));
        var funde = new List<string>();
        foreach (Match m in treffer)
        {
            string schluessel = m.Groups["k"].Value;
            string rueckfall = m.Groups["v"].Value;
            string de = deutsch.GetString(schluessel) ?? "";
            string en = englisch.GetString(schluessel) ?? "";

            if (de.Trim().Length == 0) funde.Add(schluessel + ": fehlt in Resource.resx");
            if (en.Trim().Length == 0) funde.Add(schluessel + ": fehlt in Resource.en-US.resx");
            if (de.Length > 0 && !string.Equals(de, rueckfall, StringComparison.Ordinal))
                funde.Add(schluessel + ": Rückfall „" + rueckfall + "“ ≠ Ressource „" + de + "“");
            if (en.Length > 0 && !Platzhalter(de).SequenceEqual(Platzhalter(en)))
                funde.Add(schluessel + ": Platzhalter „" + de + "“ / „" + en + "“");
        }
        Assert.True(funde.Count == 0, string.Join("\n", funde));
    }

    /// <summary>
    /// Die Gegenprobe: Ein erfundener Schlüssel fällt durch — die Wache sieht die englische
    /// Ressource wirklich ohne Rückgriff.
    /// </summary>
    [Fact]
    public void Ein_fehlender_Schluessel_faellt_auf()
    {
        Assert.Null(Satz(CultureInfo.GetCultureInfo("en-US")).GetString("BK_BER_VORLAGE_GIBT_ES_NICHT"));
        Assert.Equal("Word template:", Satz(CultureInfo.GetCultureInfo("en-US")).GetString("BK_BER_VORLAGE_LBL_WORD"));
    }

    private static ResourceSet Satz(CultureInfo kultur)
    {
        ResourceSet? satz = Resource.ResourceManager.GetResourceSet(kultur, true, false);
        Assert.NotNull(satz);
        return satz!;
    }

    private static string[] Platzhalter(string text)
        => Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

    private static string Pfad(string repoRelativ)
        => Path.Combine(Wurzel(), repoRelativ.Replace('/', Path.DirectorySeparatorChar));

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
