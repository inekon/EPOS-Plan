using EPOS.UI.Dienste;
using WindowsFormsApplication1;

namespace EPOS.iOS;

/// <summary>
/// Die iOS-Fassung von <see cref="IHilfeDienst"/> - der Zugang zur
/// Wiki-Dokumentation.
///
/// <para><b>Warum nicht dieselbe Kette wie unter Windows.</b>
/// <c>WindowsHilfeDienst</c> loest ueber <c>HelpExtender.ZielFuer</c> auf und
/// zeigt ein angeheftetes <c>Form_HelpPopup</c>. Beides sind WinForms-Bauteile
/// und bleiben in der Windows-Huelle. Auf iOS bleibt von der Kette das, was
/// plattformfrei ist: die ZUORDNUNGSDATEI <c>help_mapping.txt</c> - dieselbe
/// Datei, dieselben Schluessel, dieselben Ziele - und die Adressbildung des
/// Kerns (<c>WikiWissen</c>).</para>
///
/// <para><b>Was iU10 noch nicht kann.</b> Der Windows-Katalog liefert zu jedem
/// Ziel auch Kurztext und Beschreibung; sie stammen aus dem Wiki-Zwischen-
/// speicher, den <c>HelpCatalog</c> pflegt. Den gibt es auf iOS noch nicht.
/// <see cref="Aufloesen"/> liefert deshalb den Zielnamen als Kurztext und
/// einen Hinweis als Beschreibung - der Infoknopf ist damit sichtbar und
/// wirksam (er oeffnet die richtige Seite), nur ohne Vorschautext. Das
/// Nachziehen des Katalogs gehoert zu iU11.</para>
/// </summary>
public sealed class IosHilfeDienst : IHilfeDienst
{
    /// <summary>Der Name der Zuordnungsdatei im Anwendungspaket.</summary>
    internal const string ZUORDNUNGSDATEI = "help_mapping.txt";

    private readonly Func<Stream?> _zuordnungen;
    private readonly Action<string> _oeffneAdresse;

    private Dictionary<string, string>? _tabelle;

    /// <param name="zuordnungen">
    /// Oeffnet <c>help_mapping.txt</c> aus dem Anwendungspaket; <c>null</c> =
    /// es gibt keine Zuordnungen, dann bleibt jeder Infoknopf folgenlos.
    /// </param>
    /// <param name="oeffneAdresse">
    /// Oeffnet eine Adresse im Browser. Wird von der Huelle mit
    /// <c>Launcher.OpenAsync</c> belegt; im Test mit einem Zaehler.
    /// </param>
    public IosHilfeDienst(Func<Stream?> zuordnungen, Action<string> oeffneAdresse)
    {
        _zuordnungen = zuordnungen;
        _oeffneAdresse = oeffneAdresse;
    }

    /// <inheritdoc />
    public HilfeEintrag? Aufloesen(string schluessel)
    {
        if (string.IsNullOrWhiteSpace(schluessel)) return null;

        string ziel = Ziel(schluessel);
        if (ziel.Length == 0) return null;

        string adresse = Adresse(ziel);
        if (adresse.Length == 0) return null;

        return new HilfeEintrag(
            ziel,
            "Die ausführliche Beschreibung steht in der Dokumentation.",
            adresse);
    }

    /// <inheritdoc />
    public void Oeffnen(string schluessel)
    {
        HilfeEintrag? eintrag = Aufloesen(schluessel);
        if (eintrag == null || string.IsNullOrEmpty(eintrag.Url)) return;

        try { _oeffneAdresse(eintrag.Url); } catch { }
    }

    // =====================================================================

    /// <summary>
    /// Das Ziel zu einem Schluessel aus <c>help_mapping.txt</c>; <c>""</c>,
    /// wenn die Datei ihn nicht kennt.
    /// </summary>
    private string Ziel(string schluessel)
    {
        _tabelle ??= Lies();
        return _tabelle.TryGetValue(schluessel.Trim(), out string? ziel) ? ziel : "";
    }

    /// <summary>
    /// Liest die Zuordnungsdatei. Aufbau je Zeile:
    /// <c>Praefix.Steuerelement = Ziel</c>; <c>#</c> leitet einen Kommentar
    /// ein, eine spaetere Zeile schlaegt eine fruehere. Wortgleich zur
    /// Auswertung in <c>HelpExtender.ZielFuer</c>.
    /// </summary>
    private Dictionary<string, string> Lies()
    {
        var tabelle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using Stream? datei = _zuordnungen();
            if (datei == null) return tabelle;

            using var leser = new StreamReader(datei);
            string? rohzeile;
            while ((rohzeile = leser.ReadLine()) != null)
            {
                string zeile = rohzeile.Trim('\uFEFF', ' ', '\t');
                if (zeile.Length == 0 || zeile.StartsWith("#")) continue;

                int gleich = zeile.IndexOf('=');
                if (gleich <= 0) continue;

                string links = zeile.Substring(0, gleich).Trim();
                string rechts = zeile.Substring(gleich + 1).Trim();
                if (links.Length == 0 || rechts.Length == 0) continue;

                tabelle[links] = rechts;   // spaetere Zeile schlaegt fruehere
            }
        }
        catch
        {
            // Eine unlesbare Zuordnungsdatei macht die Hilfe still, nicht das
            // Programm kaputt.
        }

        return tabelle;
    }

    /// <summary>
    /// Die Wiki-Adresse zu einem Ziel. Ein Ziel ist entweder ein Kurzname
    /// („Pufferspeicher") oder bereits ein Seitenpfad
    /// („/wiki/Programm_Dokumentation/Pufferspeicher"); ein <c>#</c> trennt die
    /// Sprungmarke ab (F7/A3 des Hilfekonzepts).
    /// </summary>
    internal static string Adresse(string ziel)
    {
        if (string.IsNullOrWhiteSpace(ziel)) return "";

        string rest = ziel.Trim();
        string anker = "";

        int raute = rest.IndexOf('#');
        if (raute >= 0)
        {
            anker = rest.Substring(raute + 1).Trim();
            rest = rest.Substring(0, raute).Trim();
        }
        if (rest.Length == 0) return "";

        string basis = WikiWissen.Basis();

        // Ein fertiger Seitenpfad wird nur angehaengt, ein Kurzname bekommt
        // erst die Rubrik davor.
        if (rest.StartsWith("/"))
        {
            string url = basis + rest;
            return anker.Length > 0 ? url + "#" + anker : url;
        }

        return WikiWissen.SeitenUrl(basis, WikiWissen.RubrikTitel(rest), anker);
    }
}
