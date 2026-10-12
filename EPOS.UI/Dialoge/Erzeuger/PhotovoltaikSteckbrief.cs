using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>Die Beschriftungen des Gerätesteckbriefs des Photovoltaikmoduls (WP1b, KA‑E‑25); füllt sich selbst aus den Ressourcen.</summary>
public sealed class PhotovoltaikSteckbriefTexte : SatzsteckbriefTexte
{
}

/// <summary>
/// <b>Die Zeilen des Gerätesteckbriefs des Photovoltaikmoduls</b> (WP1b, Anwenderentscheid 12.10.2026, KA‑E‑25: „auf der linken Seite
/// Daten zur ausgewählten Anlage — aus dem Datensatz die wichtigsten Daten") — welche Felder des Satzes in welcher
/// Folge; gelesen wird über <see cref="Satzsteckbrief"/> aus dem Feldsatz, den die Felder rechts bearbeiten.
/// </summary>
public static class PhotovoltaikSteckbrief
{
    /// <summary>Die Felder des Steckbriefs (Profilschlüssel, Prüfanker) in Anzeigereihenfolge.</summary>
    public static readonly IReadOnlyList<(string Schluessel, string Kennung)> Auswahl = new (string, string)[]
    {
        (ModulKatalogProfil.FeldBezeichner, "name"),
        (ModulKatalogProfil.FeldFirma, "hersteller"),
        (ModulKatalogProfil.FeldTechnologie, "technologie"),
        (ModulKatalogProfil.FeldLeistung, "pmpp"),
        (ModulKatalogProfil.FeldWirkungsgrad, "wirkungsgrad"),
        (ModulKatalogProfil.FeldLaenge, "laenge"),
        (ModulKatalogProfil.FeldBreite, "breite"),
        ("ALPHA_SC", "alpha"),
        ("BETA_OC", "beta"),
        (ModulKatalogProfil.FeldTempKoeff, "gamma"),
        (ModulKatalogProfil.FeldTNoct, "tnoct"),
        (ModulKatalogProfil.FeldUMpp, "umpp"),
        (ModulKatalogProfil.FeldIMpp, "impp"),
        (ModulKatalogProfil.FeldModulkosten, "invest"),
    };

    /// <summary>Die Zeilen aus Feldsatz bzw. Detailblock, am Projektsatz mit den Kostensummen der Anlage.</summary>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(IReadOnlyList<BrowserFeldwert>? felder, ErzeugerDetail? detail,
        PhotovoltaikSteckbriefTexte texte, (double Invest, double Betrieb)? kosten = null, IEnumerable<Steckbriefzeile>? zusatz = null)
        => Satzsteckbrief.Zeilen(felder, detail, Auswahl, texte, kosten, zusatz);
}
