using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// WP1b (Fensterprobe, Fall <c>satz-steckbrief</c>, Abfrage <c>?steckbrief=1</c>): ein voller Feldsatz „Alle Daten" je
/// Wirt aus dem Katalogprofil des Kerns — dieselben Schlüssel, Beschriftungen und Einheiten wie in der Anwendung, damit
/// der Gerätesteckbrief links so viele Zeilen zeigt wie mit echten Daten. Die Werte sind neutral (Text „Muster",
/// Zahl 12,5, Ganzzahl 70, Schalter ein); ohne die Abfrage bleibt der knappe Feldsatz der übrigen Fälle.
/// </summary>
public static class Steckbrieffelder
{
    /// <summary>Der Feldsatz zum Fall; leer = der Fall hat kein Profil.</summary>
    public static List<BrowserFeldwert> Fuer(string fall)
    {
        IEnumerable<(string Schluessel, string Bezeichnung, string Einheit, BrowserFeldArt Art)> felder = fall switch
        {
            "heizkessel" => Browser(KatalogBrowserArt.Heizkessel),
            "bhkw" => Browser(KatalogBrowserArt.Bhkw),
            "pufferspeicher" => Browser(KatalogBrowserArt.Pufferspeicher),
            "solarkollektoren" => Browser(KatalogBrowserArt.Solarkollektoren),
            "stromspeicher" => Modul(ModulKatalogArt.Stromspeicher),
            "photovoltaik" => Modul(ModulKatalogArt.Photovoltaik),
            _ => Enumerable.Empty<(string, string, string, BrowserFeldArt)>(),
        };
        return felder.Select(f => new BrowserFeldwert
        {
            Schluessel = f.Schluessel, Bezeichnung = f.Bezeichnung, Einheit = f.Einheit, Art = f.Art, Editierbar = true,
            Wert = f.Art switch
            {
                BrowserFeldArt.Zahl => "12,5",
                BrowserFeldArt.Ganzzahl => "70",
                BrowserFeldArt.Schalter => "1",
                _ => "Muster",
            },
        }).ToList();
    }

    /// <summary>Die Beschriftungen aus den Ressourcen der laufenden Kultur, wie in der Anwendung.</summary>
    private static string Text(string schluessel)
        => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel) ?? schluessel;

    private static IEnumerable<(string, string, string, BrowserFeldArt)> Browser(KatalogBrowserArt art)
        => KatalogBrowserProfil.Finde(art, Text).Detailfelder.Select(f => (f.Schluessel, f.Bezeichnung, f.Einheit, f.Art));

    private static IEnumerable<(string, string, string, BrowserFeldArt)> Modul(ModulKatalogArt art)
        => ModulKatalogProfil.Finde(art, Text).Felder.Select(f => (f.Schluessel, f.Bezeichnung, f.Einheit, f.Art));
}
