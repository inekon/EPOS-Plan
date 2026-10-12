using System.Globalization;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Der Gerätesteckbrief der Kältemaschine</b> (KA‑E‑25, KM‑K1): die wichtigsten Daten des gewählten Satzes links in der
/// Bearbeiten-Überlagerung — Name, Hersteller, Bauart und Rückkühlart, Nennkälteleistung, EER/SEER, Kältemittel und GWP,
/// Mindestteillast, Verdichterregelung, Kaltwasser-Vorlauf, Investition und Betrieb. Lesend, aus denselben Gerätewerten wie
/// die Felder rechts; der Baustein <see cref="Geraetesteckbrief"/> zeichnet sie.
/// </summary>
public static class KaeltemaschineSteckbrief
{
    /// <summary>Die Zeilen zu Gerät <paramref name="g"/>, wahlweise mit Anlage (Kühlvorlauf) und Kostensummen.</summary>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(KaeltemaschineGeraetwerte? g, KaeltemaschineAnlageDaten? anlage,
                                                         (double Invest, double Betrieb)? kosten, KaeltemaschineAnlageTexte t)
    {
        if (g is null) return Array.Empty<Steckbriefzeile>();
        static string Z(double? w, int stellen) => w is double d ? d.ToString("N" + stellen, CultureInfo.CurrentCulture) : "";
        string bauart = string.Join(" / ", new[] { g.Geraeteart, g.Rueckkuehlart }.Where(s => !string.IsNullOrWhiteSpace(s)));
        string eer = Z(g.NennEer, 2) + (g.Saisonkennzahl is double s
            ? " / " + (string.IsNullOrWhiteSpace(g.SaisonkennzahlArt) ? "SEER" : g.SaisonkennzahlArt) + " " + Z(s, 1) : "");
        string kaeltemittel = g.Kaeltemittel + (g.Gwp is double gwp ? " (GWP " + Z(gwp, 0) + ")" : "");
        double? vorlauf = anlage?.KuehlVorlauf;
        var liste = new List<Steckbriefzeile>
        {
            new(t.LabelBezeichner, g.Bezeichner, Kennung: "name"),
            new(t.LabelFirma, g.Firma, Kennung: "hersteller"),
            new(Resource.KM_LBL_GERAETEART + " / " + t.LabelRueckkuehlart, bauart, Kennung: "bauart"),
            new(t.LabelNennkaelteleistung, Z(g.Nennkaelteleistung, 1), t.EinheitKw, Kennung: "leistung"),
            new(t.SpalteEer, eer.Trim(), Kennung: "eer"),
            new(Resource.KM_LBL_KAELTEMITTEL, kaeltemittel.Trim(), Kennung: "kaeltemittel"),
            new(t.LabelMindestteillast, Z(g.Mindestteillast, 0), t.EinheitProzent, Kennung: "mindestteillast"),
            new(t.LabelVerdichterregelung, g.Verdichterregelung, Kennung: "regelung"),
            new(vorlauf is null ? t.LabelKaltwasserMin : t.LabelVorlauf,
                vorlauf is double v ? Z(v, 1) : Z(g.KaltwasserMin, 1), t.EinheitGradC, Kennung: "vorlauf"),
        };
        if (kosten is (double invest, double betrieb))
            liste.Add(new(Resource.KMA_LBL_INVEST_BETRIEB, Z(invest, 0) + " / " + Z(betrieb, 0), "€", Kennung: "kosten"));
        return liste;
    }
}
