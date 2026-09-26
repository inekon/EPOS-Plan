using System.Reflection;
using System.Text.RegularExpressions;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Die Ortswache</b> (BV-E6, Konzept Berichtsvorlagen 9.6): Jede Zeile der Ortstabelle
/// <see cref="Vorlagenfeldorte"/> nennt einen Katalogschlüssel (kein Alias, <c>Seit</c> ≤ Katalogfassung)
/// und einen bekannten Ort (Ansicht, Blatt); jeder Schlüssel einer Zeile steht als Parameter in den
/// Seiten oder Hüllen; und jede literale Marke der Seiten (<c>Vorlagenfeld="…"</c>) steht in der Tabelle.
/// Die Marken, die eine Hülle zur Laufzeit setzt, hält <c>VorlagenfeldAnzeigewertWacheTests</c> (Kern)
/// gegen dieselbe Tabelle.
/// </summary>
public class VorlagenfeldorteWacheTests
{
    [Fact]
    public void Jede_Zeile_nennt_einen_Katalogschluessel_der_Fassung()
    {
        Assert.NotEmpty(Vorlagenfeldorte.Alle);
        foreach (Vorlagenfeldortzeile z in Vorlagenfeldorte.Alle)
        {
            Vorlagenfeld? feld = Vorlagenfeldkatalog.Finde(z.Schluessel);
            Assert.True(feld is not null, z.Schluessel + " ist kein Katalogschlüssel");
            Assert.Equal(z.Schluessel, feld!.Schluessel);   // kein Alias, keine Großschreibung
            Assert.True(feld.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG, z.Schluessel + " ist jünger als die Fassung");
        }
    }

    [Fact]
    public void Jeder_Ort_ist_eine_bekannte_Ansicht_mit_bekanntem_Blatt()
    {
        Assert.Equal(Ansichten.BerichteKosten, Vorlagenfeldorte.ANSICHT_BERICHTE);
        Assert.Equal(Seitenschluessel.Simulation, Vorlagenfeldorte.ANSICHT_SIMULATION);
        Assert.Equal(Seitenschluessel.Assistent, Vorlagenfeldorte.ANSICHT_ASSISTENT);

        var blaetter = new Dictionary<string, HashSet<string>>
        {
            [Vorlagenfeldorte.ANSICHT_BERICHTE] = new()
            {
                BerichteKostenSeite.SEITE_UEBERSICHT, BerichteKostenSeite.SEITE_KOSTEN,
                BerichteKostenSeite.SEITE_WIRTSCHAFT, BerichteKostenSeite.SEITE_BERICHT,
            },
            [Vorlagenfeldorte.ANSICHT_SIMULATION] = typeof(SimulationErgebnisSeite.Blatt)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(f => Vorlagenfeldorte.Ergebnisblatt((string)f.GetValue(null)!)).ToHashSet(),
            [Vorlagenfeldorte.ANSICHT_ASSISTENT] = new() { Vorlagenfeldorte.SCHRITT_PROJEKTKOPF },
        };

        var gesehen = new HashSet<(string, Vorlagenfeldort)>();
        foreach (Vorlagenfeldortzeile z in Vorlagenfeldorte.Alle)
        {
            Assert.True(blaetter.ContainsKey(z.Ort.Ansicht), z.Schluessel + ": unbekannte Ansicht " + z.Ort.Ansicht);
            Assert.True(blaetter[z.Ort.Ansicht].Contains(z.Ort.Reiter), z.Schluessel + ": unbekanntes Blatt " + z.Ort.Reiter);
            Assert.False(string.IsNullOrWhiteSpace(z.Ort.Element), z.Schluessel + ": ohne Element");
            // Der Assistent ist Bearbeitungsmodus: „In der App zeigen“ springt nicht dorthin (9.4).
            Assert.Equal(z.Ort.Ansicht != Vorlagenfeldorte.ANSICHT_ASSISTENT, z.Ort.NurLesend);
            Assert.True(gesehen.Add((z.Schluessel, z.Ort)), z.Schluessel + " doppelt an " + z.Ort);
        }
    }

    [Fact]
    public void Finde_liefert_die_Orte_eines_Schluessels()
    {
        Vorlagenfeldort ort = Assert.Single(Vorlagenfeldorte.Finde("wirtschaft.beste.kapitalwert"));
        Assert.Equal(new Vorlagenfeldort("BERICHTE_KOSTEN", "WIRTSCHAFT", "kachel.kapitalwert", true), ort);
        Assert.Equal(3, Vorlagenfeldorte.Finde("tabelle.wirtschaft.kennzahlen").Count
                        + Vorlagenfeldorte.Finde("tabelle.wirtschaft.kennzahlen.guenstig").Count);
        Assert.Empty(Vorlagenfeldorte.Finde("projekt.gibtsnicht"));
        Assert.Empty(Vorlagenfeldorte.Finde(""));
        Assert.Equal("stamm.kennzahl.eff.jaz", Vorlagenfeldorte.Kennzahl(true, "eff.jaz"));
        Assert.Equal("stand.wirtschaft.investition", Vorlagenfeldorte.Wirtschaft(false, "investition"));
    }

    [Fact]
    public void Jede_literale_Marke_der_Seiten_steht_in_der_Tabelle()
    {
        string ui = Path.Combine(VorlagenfeldAbdeckungWacheTests.Wurzel(), "EPOS.UI");
        var literal = new Regex(@"\bVorlagenfeld=""([a-z][a-z0-9_.]*)""");
        var fehler = new List<string>();
        int anzahl = 0;
        foreach (string pfad in Directory.GetFiles(ui, "*.razor", SearchOption.AllDirectories))
            foreach (Match m in literal.Matches(File.ReadAllText(pfad)))
            {
                anzahl++;
                if (Vorlagenfeldorte.Finde(m.Groups[1].Value).Count == 0)
                    fehler.Add(Path.GetFileName(pfad) + ": " + m.Groups[1].Value);
            }
        Assert.True(fehler.Count == 0, "nicht in Vorlagenfeldorte: " + string.Join(", ", fehler));
        Assert.True(anzahl >= 10, anzahl + " literale Marken — die Wache sieht die Seiten nicht");
    }

    [Fact]
    public void Jede_Zeile_steht_als_Parameter_in_Seite_oder_Huelle()
    {
        string wurzel = VorlagenfeldAbdeckungWacheTests.Wurzel();
        string quellen = string.Join("\n",
            Directory.GetFiles(Path.Combine(wurzel, "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(wurzel, "EPOS.UI.Daten"), "*.cs", SearchOption.AllDirectories))
                .Select(File.ReadAllText));

        // Kennzahlschlüssel, die eine Hülle über die Konstanten des Kennzahlenkatalogs nennt.
        Dictionary<string, string> konstanten = typeof(KennzahlenKatalog)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => (string)f.GetRawConstantValue()!, f => "KennzahlenKatalog." + f.Name);

        var fehler = new List<string>();
        foreach (string schluessel in Vorlagenfeldorte.Alle.Select(z => z.Schluessel).Distinct())
        {
            // Was eine Hülle nach dem Stand bildet: stamm.|stand. + kennzahl.|wirtschaft. + Rest;
            // was sie nach dem Szenario bildet: der Tabellenschlüssel ohne Anhang.
            string kern = Regex.Replace(schluessel, @"^(stamm|stand)\.(kennzahl|wirtschaft)\.", "");
            kern = Regex.Replace(kern, @"\.(guenstig|unguenstig)$", "");
            bool genannt = quellen.Contains("\"" + schluessel + "\"", StringComparison.Ordinal)
                        || quellen.Contains("\"" + kern + "\"", StringComparison.Ordinal)
                        || (konstanten.TryGetValue(kern, out string? name) && quellen.Contains(name, StringComparison.Ordinal));
            if (!genannt) fehler.Add(schluessel);
        }
        Assert.True(fehler.Count == 0, "ohne Parameterort: " + string.Join(", ", fehler));
    }
}
