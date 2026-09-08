using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Probezeilen der drei ZEITREIHENKATALOGE</b> (Stufe <b>S3.2</b> des
/// <c>Konzept_Katalogfilter</c>, Anwenderentscheid W14a‑E‑10 vom 07.09.2026).
///
/// <para>Seit S3.2 tragen die fünf Ganglinien-Dialoge die EINE
/// <c>Katalogliste</c> des Hauses; ihre Zeilen sind deshalb
/// <see cref="Katalogfilterzeile"/> und keine dialogeigenen Datensätze mehr. Die
/// Bauart steht hier EINMAL, damit fünf Prüfstände nicht fünfmal dieselbe Zeile
/// zusammensetzen.</para>
///
/// <para>Der Übersetzer ist <c>s =&gt; s</c> — die Prüfstände sehen den
/// Ressourcenschlüssel und nicht den deutschen Text; so hängt kein Fall an einer
/// Übersetzung (Muster <c>KatalogBrowserProfil.Finde</c>).</para>
/// </summary>
internal static class Zeitreihenproben
{
    /// <summary>Das Profil einer Ausprägung, ohne die Spalte „im Projekt verwendet".</summary>
    internal static Katalogfilterprofil Profil(Zeitreihenart art)
        => Katalogfilterprofil.FuerZeitreihe(art, s => s);

    /// <summary>Dasselbe Profil MIT der Spalte „im Projekt verwendet" (Q12).</summary>
    internal static Katalogfilterprofil ProjektProfil(Zeitreihenart art)
        => Profil(art).MitVerwendungsspalte(s => s);

    /// <summary>
    /// Eine Zeile eines Zeitreihenkatalogs. <paramref name="zeitintervall"/> steht nur
    /// in der Stromganglinie, <paramref name="beschreibung"/> nur in der
    /// Solarganglinie — genau wie in <c>ZeitreihenKatalogCtrl</c>.
    /// </summary>
    internal static Katalogfilterzeile Zeile(int id, string bezeichner,
                                             bool geschuetzt = false,
                                             int? zeitintervall = null,
                                             string? beschreibung = null,
                                             double? jahresarbeitMwh = null,
                                             double? spitzeKw = null)
    {
        var zeile = new Katalogfilterzeile(id, bezeichner) { Geschuetzt = geschuetzt };

        zeile.MitText(Katalogfilterprofil.SpBezeichner, bezeichner);

        if (zeitintervall is not null)
            zeile.MitZahl(Katalogfilterprofil.SpZeitintervall, zeitintervall.Value, 0);

        if (beschreibung is not null)
            zeile.MitText(Katalogfilterprofil.SpBeschreibung, beschreibung);

        zeile.MitZahl(Katalogfilterprofil.SpJahresarbeitMwh, jahresarbeitMwh, 1);
        zeile.MitZahl(Katalogfilterprofil.SpSpitzeKw, spitzeKw, 1);

        return zeile;
    }

    /// <summary>Zwei Stromganglinien — eine eigene (Viertelstunden) und eine der Auslieferung.</summary>
    internal static IReadOnlyList<Katalogfilterzeile> Stromganglinien() => new List<Katalogfilterzeile>
    {
        Zeile(1, "Werk Nord", zeitintervall: 4, jahresarbeitMwh: 4790.0, spitzeKw: 1513.5),
        Zeile(2, "Auslieferung", geschuetzt: true, zeitintervall: 1,
              jahresarbeitMwh: 4789.9, spitzeKw: 2070.0)
    };
}
