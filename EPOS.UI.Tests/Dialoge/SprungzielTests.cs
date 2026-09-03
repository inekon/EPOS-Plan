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
    public static TheoryData<string> AlleZiele => new()
    {
        Sprungziel.Gesetzesparameter,
        Sprungziel.GesetzesparameterCo2,
    };

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
        Assert.NotEqual(Sprungziel.Gesetzesparameter, Sprungziel.GesetzesparameterCo2);
    }

    [Fact]
    public void Die_Schluessel_stehen_fest()
    {
        // Sie stehen als Zeichenkette auch in der Windows-Bruecke; wer hier
        // umbenennt, muss dort nachziehen. Der Test macht das Paar sichtbar.
        Assert.Equal("GESETZESPARAMETER", Sprungziel.Gesetzesparameter);
        Assert.Equal("GESETZESPARAMETER_CO2", Sprungziel.GesetzesparameterCo2);
    }
}
