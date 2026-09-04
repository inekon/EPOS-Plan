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

    public static TheoryData<string> AlleZiele
    {
        get
        {
            var daten = new TheoryData<string>();
            foreach (string s in Schluessel()) daten.Add(s);
            return daten;
        }
    }

    [Theory]
    [MemberData(nameof(AlleZiele))]
    public void Jedes_Ziel_ist_ein_sprachneutraler_ASCII_Schluessel(string schluessel)
    {
        Assert.False(string.IsNullOrWhiteSpace(schluessel));
        Assert.All(schluessel, z => Assert.True(
            (z >= 'A' && z <= 'Z') || (z >= '0' && z <= '9') || z == '_',
            "Unerlaubtes Zeichen '" + z + "' in " + schluessel));
    }

    [Fact]
    public void Die_Schluessel_sind_eindeutig()
    {
        string[] alle = Schluessel();
        Assert.Equal(alle.Length, alle.Distinct().Count());
    }

    [Fact]
    public void Die_Schluessel_stehen_fest()
    {
        // Sie stehen als Zeichenkette auch in der Windows-Bruecke; wer hier
        // umbenennt, muss dort nachziehen. Der Test macht das Paar sichtbar.
        Assert.Equal("GESETZESPARAMETER", Sprungziel.Gesetzesparameter);
        Assert.Equal("GESETZESPARAMETER_CO2", Sprungziel.GesetzesparameterCo2);

        // iU9-W6.0d: die vier Katalogverwaltungen der Erzeugerdialoge.
        Assert.Equal("HEIZKESSEL_ADMIN", Sprungziel.HeizkesselAdmin);
        Assert.Equal("STROMSPEICHER_ADMIN", Sprungziel.StromspeicherAdmin);
        Assert.Equal("PV_ADMIN", Sprungziel.PvAdmin);
        Assert.Equal("PUFFERSP_ADMIN", Sprungziel.PufferSpAdmin);

        // iU9-W7.0f: die Stammdaten der Solarthermieganglinien.
        Assert.Equal("SOLARGANGLINIE_ADMIN", Sprungziel.SolarganglinieAdmin);

        // iU9-W10a.0c: derselbe Pufferspeicherkatalog NUR ZUM ANSEHEN. Er braucht
        // einen eigenen Schluessel, weil die Bruecke dort m_bReadOnly setzen muss -
        // sonst waere aus dem Nachschlagen das Bearbeiten des Auslieferungskatalogs
        // geworden (Befund W10-B28).
        Assert.Equal("PUFFERSP_ADMIN_NUR_LESEN", Sprungziel.PufferSpAdminNurLesen);
        Assert.NotEqual(Sprungziel.PufferSpAdmin, Sprungziel.PufferSpAdminNurLesen);

        // iU9-W11b.0: die Auslegungsoptimierung des Stromspeichers. Sie ist das
        // erste Brueckenziel MIT Parameter (der gerechnete Lauf) und das erste,
        // dessen Antwort NICHT "mit OK geschlossen" heisst, sondern
        // Form_SpeicherOptimierung.AuslegungUebernommen.
        Assert.Equal("SPEICHER_OPTIMIERUNG", Sprungziel.SpeicherOptimierung);
    }

    [Fact]
    public void Alle_zehn_Ziele_sind_da()
    {
        // Zaehlwert statt Aufzaehlung: Er faellt auf, sobald ein Ziel wegfaellt -
        // die Bruecke hat dann einen toten switch-Zweig.
        Assert.Equal(10, Schluessel().Length);
    }
}
