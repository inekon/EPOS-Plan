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
    [Fact]
    public void DieBlazorNachfolgeStehtImRepoUndDieWinFormsMaskeNichtMehr()
    {
        Assert.True(File.Exists(Repowurzel.Datei("EPOS.UI/Dialoge/Kosten/EnergietraegerVarianteDialog.razor")),
                    "Die Nachfolge EnergietraegerVarianteDialog.razor fehlt.");

        foreach (var endung in new[] { ".Designer.cs", ".cs", ".resx" })
        {
            var alt = Repowurzel.Designer("Kosten/Form_Kosten_Auswahl" + endung);
            Assert.False(File.Exists(alt), "Die WinForms-Fassung lebt wieder: " + alt);

            var muster = Repowurzel.Pruefmuster("Kosten/Form_Kosten_Auswahl" + endung);
            Assert.True(File.Exists(muster), "Pruefmuster fehlt: " + muster);
        }

        // Das Pruefmuster ist Lesevorlage, kein Bestand: Der Stapellauf ueber das
        // Repo darf es nicht als 120. Maske mitzaehlen.
        Assert.DoesNotContain(Stapel.Dateien(Repowurzel.Pfad),
                              d => d.Contains("Pruefmuster", StringComparison.Ordinal));
    }
}
