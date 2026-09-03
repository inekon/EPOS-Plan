using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Die eingefrorenen Pruefmuster unter <c>Pruefmuster/</c>.
///
/// <para>
/// Der Rest der Tests liest die ECHTEN Designer-Dateien des Bestands - das ist
/// Absicht und bleibt so. Eine Maske, die umgestellt ist, gibt es aber nicht
/// mehr: Mit iU8-9 (Stichtag iZ5) hat Form_Kosten_Auswahl ihre WinForms-Fassung
/// verloren und laeuft als Blazor-Komponente. Damit die Handkarte aus dem
/// Umsetzungsplan weiter geprueft werden kann, liegt der letzte Stand der Maske
/// unveraendert unter <c>Pruefmuster/Kosten/</c>.
/// </para>
/// <para>
/// Dieser Test haelt den Stichtag fest: die Nachfolge steht im Repo, die
/// Vorgaengerin nicht mehr, und das Pruefmuster zaehlt nicht zum Bestand. Faellt
/// er, ist entweder die Umstellung zurueckgenommen worden oder das Pruefmuster
/// ist in den Produktivbaum gerutscht - beides muss auffallen.
/// </para>
/// </summary>
public sealed class PruefmusterTests
{
    /// <summary>
    /// Je Muster: der Fachordner, der Maskenname und die Razor-Nachfolge.
    /// </summary>
    public static TheoryData<string, string, string> Muster => new()
    {
        { "Kosten", "Form_Kosten_Auswahl",
          "EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor" },

        // iU9-W1.3: Die vier Drehfelder von Form_CaseEingabe sind der einzige
        // Beleg fuer die Bereichsspalte einer NumericUpDown.
        { "Kosten", "Form_CaseEingabe",
          "EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor" },
    };

    /// <summary>
    /// Muster STILLGELEGTER Masken (Anwenderentscheid iF29): Sie sind geloescht,
    /// nicht umgestellt - eine Nachfolge gibt es also nicht. Das Muster bleibt,
    /// weil an ihm Werkzeugmechanik haengt.
    /// </summary>
    public static TheoryData<string, string> StillgelegteMuster => new()
    {
        // iU9-W0: Form_KostenfaktorItem ist der einzige Beleg fuer die alte
        // Designer-Schreibweise mit "this." und fuer die Kette "Oeffner ohne Wurzel".
        { "Kosten", "Form_KostenfaktorItem" },
    };

    [Theory]
    [MemberData(nameof(Muster))]
    public void DieBlazorNachfolgeStehtImRepoUndDieWinFormsMaskeNichtMehr(
        string fach, string maske, string nachfolge)
    {
        Assert.True(File.Exists(Repowurzel.Datei(nachfolge)),
                    "Die Nachfolge " + nachfolge + " fehlt.");

        foreach (var endung in new[] { ".Designer.cs", ".cs", ".resx" })
        {
            var alt = Repowurzel.Designer(fach + "/" + maske + endung);
            Assert.False(File.Exists(alt), "Die WinForms-Fassung lebt wieder: " + alt);

            var muster = Repowurzel.Pruefmuster(fach + "/" + maske + endung);
            Assert.True(File.Exists(muster), "Pruefmuster fehlt: " + muster);
        }
    }

    [Theory]
    [MemberData(nameof(StillgelegteMuster))]
    public void DieStillgelegteWinFormsMaskeIstWegUndDasMusterVollstaendig(string fach, string maske)
    {
        foreach (var endung in new[] { ".Designer.cs", ".cs", ".resx" })
        {
            var alt = Repowurzel.Designer(fach + "/" + maske + endung);
            Assert.False(File.Exists(alt), "Die WinForms-Fassung lebt wieder: " + alt);

            var muster = Repowurzel.Pruefmuster(fach + "/" + maske + endung);
            Assert.True(File.Exists(muster), "Pruefmuster fehlt: " + muster);
        }
    }

    [Fact]
    public void DiePruefmusterZaehlenNichtZumBestand()
    {
        // Das Pruefmuster ist Lesevorlage, kein Bestand: Der Stapellauf ueber das
        // Repo darf es nicht als weitere Maske mitzaehlen.
        Assert.DoesNotContain(Stapel.Dateien(Repowurzel.Pfad),
                              d => d.Contains("Pruefmuster", StringComparison.Ordinal));
    }
}
