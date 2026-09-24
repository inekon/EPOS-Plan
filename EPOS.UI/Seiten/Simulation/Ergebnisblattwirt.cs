using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;

namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die gemeinsame Grundlage der Ergebnisblätter der Simulation (Welle #458, Stufe 2):
/// die Farbwahl am Bild (<see cref="Farbwahlwirt"/>) und die Anmeldung der
/// ANZEIGESCHALTER beim Register der Seite (<see cref="Ergebnisanzeige"/>).
/// </summary>
/// <remarks>
/// <para><b>Warum eine Grundklasse.</b> Neun Blätter melden dieselbe Sache auf dieselbe
/// Weise an — beim Aufbau ihre Schalter, beim Abbau nichts mehr. Ein Blatt nennt nur,
/// WELCHE Schalter es führt (<see cref="Blattschalter"/>), und baut jeden über
/// <see cref="Umschalter"/>: Der setzt über den Weg des Schalters auf dem Blatt und
/// zeichnet danach neu.</para>
/// <para><b>Ohne Register</b> (ein Prüfstand, ein Wirt ohne Kaskadenwert) meldet das
/// Blatt nichts an; es zeichnet wie bisher.</para>
/// </remarks>
public abstract class Ergebnisblattwirt : Farbwahlwirt, IDisposable
{
    /// <summary>Das Register der Seite, bei dem sich das Blatt anmeldet.</summary>
    [CascadingParameter] public Ergebnisanzeige? Anzeige { get; set; }

    /// <summary>Die Anzeigeschalter dieses Blattes, von oben nach unten — bei jedem Zugriff neu.</summary>
    protected abstract IReadOnlyList<Anzeigeschalter> Blattschalter();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Anzeige?.Melden(this, Blattschalter);
    }

    /// <summary>
    /// Ein Schalter über die Wege des Blattes — gesetzt wird wie von Hand, danach zeichnet
    /// das Blatt neu.
    /// </summary>
    protected Anzeigeschalter Umschalter(string name, Func<bool> lesen, Action<bool> setzen)
        => new(name, lesen, wert =>
        {
            setzen(wert);
            _ = InvokeAsync(StateHasChanged);
        });

    /// <summary>
    /// Die Option einer WAHL als Schalter: „an" wählt sie, „aus" lehnt benannt ab — eine
    /// Wahl verlässt man, indem man eine andere wählt.
    /// </summary>
    protected Anzeigeschalter Wahloption(string name, Func<bool> gewaehlt, Action waehlen)
        => Umschalter(name, gewaehlt, an =>
        {
            if (an) { waehlen(); return; }
            if (gewaehlt())
                throw new InvalidOperationException(string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    WindowsFormsApplication1.MyResource.Resource.KI_DLG_SIM_ANZEIGE_WAHL, name));
        });

    /// <summary>
    /// Die Plätze einer Mehrfachauswahl mit diesem Platz an oder aus — aufsteigend, wie
    /// die Auswahl sie meldet.
    /// </summary>
    protected static IReadOnlyList<int> MitPlatz(IReadOnlyList<int> gewaehlt, int platz, bool an)
    {
        var liste = new SortedSet<int>(gewaehlt);
        if (an) liste.Add(platz); else liste.Remove(platz);
        return liste.ToList();
    }

    /// <inheritdoc/>
    public virtual void Dispose() => Anzeige?.Abmelden(this);
}
