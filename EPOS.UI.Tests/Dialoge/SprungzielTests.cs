using System.Linq;
using System.Reflection;
using EPOS.UI.Dialoge.Allgemein;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die Sprungziele (iU9-W2.2). Sie sind STEUERWERTE, keine Anzeigetexte:
/// sprachneutral, ASCII, Grossbuchstaben (Drei-Schichten-Regel, Konzept 13.6).
/// Die Windows-Seite <c>WindowsFormsApplication1.Sprungbruecke</c> schlaegt
/// genau diese Zeichenketten in einem <c>switch</c> nach — ein umbenannter
/// Schluessel liefe dort still ins Leere.
/// </summary>
public sealed class SprungzielTests
{
    /// <summary>
    /// Alle Ziele ueber Spiegelung statt von Hand aufgezaehlt: Ein in Welle 6
    /// nachgetragenes Ziel geht so nicht durch die Pruefung hindurch, ohne dass
    /// jemand diese Liste pflegt.
    /// </summary>
    private static string[] Schluessel()
        => typeof(Sprungziel)
           .GetFields(BindingFlags.Public | BindingFlags.Static)
           .Where(f => f.IsLiteral && f.FieldType == typeof(string))
           .Select(f => (string)f.GetRawConstantValue()!)
           .ToArray();

    /// <summary>
    /// Die Regel gilt WEITER, auch wenn die Liste heute leer ist: Sie ist die
    /// Bedingung fuer den naechsten Schluessel, den jemand hier anlegt.
    ///
    /// <para>Bis W11b-B-5 stand das als <c>[Theory]</c> mit <c>MemberData</c>. Mit
    /// dem letzten Ziel faellt eine solche Theory nicht gruen aus, sondern gar nicht:
    /// xUnit meldet „No data found". Eine Schleife im <c>[Fact]</c> sagt dasselbe und
    /// vertraegt die leere Liste.</para>
    /// </summary>
    [Fact]
    public void Jedes_Ziel_ist_ein_sprachneutraler_ASCII_Schluessel()
    {
        foreach (string schluessel in Schluessel())
        {
            Assert.False(string.IsNullOrWhiteSpace(schluessel));
            Assert.All(schluessel, z => Assert.True(
                (z >= 'A' && z <= 'Z') || (z >= '0' && z <= '9') || z == '_',
                "Unerlaubtes Zeichen '" + z + "' in " + schluessel));
        }
    }

    [Fact]
    public void Die_Schluessel_sind_eindeutig()
    {
        string[] alle = Schluessel();
        Assert.Equal(alle.Length, alle.Distinct().Count());
    }

    [Fact]
    public void Es_gibt_kein_Sprungziel_mehr()
    {
        // W11b-B-5 (Windows-Abnahme V2, 07.09.2026): SPEICHER_OPTIMIERUNG war das
        // LETZTE Ziel - das einzige mit einem Parameter (dem gerechneten Lauf) und
        // das einzige, dessen Antwort nicht "mit OK geschlossen" hiess, sondern
        // Form_SpeicherOptimierung.AuslegungUebernommen. Der Entscheid iF22 ("die
        // Maske bleibt WinForms, sie ist der einzige Ort mit ScottPlot") ist mit den
        // zwei Befunden des Anwenders ueberholt worden; die Maske ist gefallen, das
        // Ziel ist eine Ueberlagerung.
        //
        // Der Weg dorthin, in Kuerze: W13.2 WaermebedarfExternAdmin, W14a.4 die fuenf
        // Katalogverwaltungen, W14b.2 SolarganglinieAdmin, W14c.3 die zwei
        // Gesetzesziele. Jedes Mal derselbe Grund: Das Ziel wurde selbst Blazor, und
        // ein Blazor-Ziel gehoert NICHT in die Bruecke (Risiko R2).
        //
        // Zaehlwert statt Aufzaehlung - er faellt auf, sobald jemand hier wieder
        // einen Schluessel anlegt, ohne die Windows-Bruecke mitzubringen. Die gibt es
        // seit W11b-B-5 naemlich nicht mehr: Sprungbruecke.cs ist geloescht, weil sie
        // keinen switch-Zweig mehr hatte.
        Assert.Empty(Schluessel());
    }
}
