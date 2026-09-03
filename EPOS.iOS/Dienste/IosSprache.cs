using Foundation;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="ISprache"/>: dieselbe Kultur wie
/// <see cref="StandardSprache"/>, zusaetzlich der gespeicherte Wert, aus dem
/// der naechste Start liest.
///
/// <para><b>Wo der Wert liegt.</b> Unter Windows in der Registry
/// (<c>HKCU\Software\wp-plan\Language</c>), hier in den
/// <c>NSUserDefaults</c> - und zwar ueber <c>Dienste.Einstellungen</c> und
/// damit unter demselben Schluesselnamen <c>Language</c>. Ein zweiter,
/// eigener Ablageweg waere die Stelle, an der Einstellungsdialog und
/// Sprachumschaltung auseinanderlaufen.</para>
///
/// <para><b>Beim Erststart entscheidet das Geraet.</b> Ohne gespeicherten Wert
/// gilt die bevorzugte Sprache des iPads: beginnt sie mit „de", ist es
/// Deutsch, sonst Englisch. Unter Windows gilt in diesem Fall Deutsch, weil
/// der Registry-Wert dort mit 0 angelegt wird - auf einem Geraet, das der
/// Anwender englisch fuehrt, waere das die falsche Antwort.</para>
///
/// <para><b>Wirksam wird eine Umstellung sofort</b>, anders als unter Windows:
/// Dort schreiben die Menuepunkte den Wert und rufen
/// <c>Application.Restart</c>, weil die Textressourcen bereits geoeffneter
/// Masken nicht mehr wechseln. Eine Blazor-Oberflaeche zeichnet neu und liest
/// die Texte dabei erneut - ein Neustart ist nicht noetig.</para>
/// </summary>
public sealed class IosSprache : StandardSprache
{
    /// <summary>Name des Einstellwerts - derselbe wie im Registry-Zweig unter Windows.</summary>
    private const string WERT = "Language";

    /// <summary>
    /// Uebernimmt die zuletzt eingestellte Sprache; ohne gespeicherten Wert die
    /// bevorzugte Sprache des Geraets. Das ist der Startweg, den unter Windows
    /// <c>WindowsSprache.AusRegistryUebernehmen</c> geht.
    /// </summary>
    public void AusEinstellungUebernehmen()
    {
        bool englisch;

        string? gespeichert = Dienste.Einstellungen.Lies(WERT, null);
        if (!string.IsNullOrEmpty(gespeichert))
        {
            // Der Bestand speichert 0 = Deutsch, 1 = Englisch.
            englisch = gespeichert.Trim() != "0";
        }
        else
        {
            englisch = !GeraetesprachIstDeutsch();
        }

        KulturUebernehmen(englisch);
    }

    /// <inheritdoc/>
    public override void Setzen(string kuerzel)
    {
        bool englisch = IstEnglischesKuerzel(kuerzel);
        Dienste.Einstellungen.SchreibZahl(WERT, englisch ? 1 : 0);
        KulturUebernehmen(englisch);
    }

    /// <summary>
    /// <c>true</c>, wenn die erste bevorzugte Sprache des Geraets Deutsch ist.
    /// Bei jedem Zweifel Deutsch - das ist die Auslieferungssprache.
    /// </summary>
    private static bool GeraetesprachIstDeutsch()
    {
        try
        {
            string[] sprachen = NSLocale.PreferredLanguages;
            if (sprachen.Length == 0) return true;
            return sprachen[0].StartsWith(DE, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}
