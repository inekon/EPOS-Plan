using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>Die Beschriftungen des Gerätesteckbriefs des Heizkessels (WP1b, KA‑E‑25); füllt sich selbst aus den Ressourcen.</summary>
public sealed class HeizkesselSteckbriefTexte : SatzsteckbriefTexte
{
}

/// <summary>
/// <b>Die Zeilen des Gerätesteckbriefs des Heizkessels</b> (WP1b, Anwenderentscheid 12.10.2026, KA‑E‑25: „auf der linken Seite
/// Daten zur ausgewählten Anlage — aus dem Datensatz die wichtigsten Daten") — welche Felder des Satzes in welcher
/// Folge; gelesen wird über <see cref="Satzsteckbrief"/> aus dem Feldsatz, den die Felder rechts bearbeiten.
/// </summary>
public static class HeizkesselSteckbrief
{
    /// <summary>Die Felder des Steckbriefs (Profilschlüssel, Prüfanker) in Anzeigereihenfolge.</summary>
    public static readonly IReadOnlyList<(string Schluessel, string Kennung)> Auswahl = new (string, string)[]
    {
        (KatalogBrowserProfil.FeldBezeichner, "name"),
        (KatalogBrowserProfil.FeldFirma, "hersteller"),
        (KatalogBrowserProfil.FeldBrennwert, "brennwert"),
        (KatalogBrowserProfil.FeldBrennstoff, "brennstoff"),
        (KatalogBrowserProfil.FeldPtherm, "nennleistung"),
        (KatalogBrowserProfil.FeldMindestleistung, "mindestleistung"),
        (KatalogBrowserProfil.FeldTeillast30, "eta30"),
        (KatalogBrowserProfil.FeldWirkungsgradGas, "eta_gas"),
        (KatalogBrowserProfil.FeldWirkungsgradOel, "eta_oel"),
        (KatalogBrowserProfil.FeldBBVerlust, "bereitschaftsverlust"),
        (KatalogBrowserProfil.FeldVorlauf, "vorlauf"),
        (KatalogBrowserProfil.FeldInvestitionskosten, "invest"),
    };

    /// <summary>Die Zeilen aus Feldsatz bzw. Detailblock, am Projektsatz mit den Kostensummen der Anlage.</summary>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(IReadOnlyList<BrowserFeldwert>? felder, ErzeugerDetail? detail,
        HeizkesselSteckbriefTexte texte, (double Invest, double Betrieb)? kosten = null, IEnumerable<Steckbriefzeile>? zusatz = null)
        => Satzsteckbrief.Zeilen(felder, detail, Auswahl, texte, kosten, zusatz);
}
