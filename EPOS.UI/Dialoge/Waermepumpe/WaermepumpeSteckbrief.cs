using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>
/// Der ARBEITSPUNKT einer Wärmepumpe am eingestellten Vorlauf (WP1a): Heizleistung und COP der Projektkennlinie
/// am Normpunkt der Quelle. Gerechnet im Kern (<c>Kennlinienarbeitspunkt</c>, dieselbe Auswertung wie der Lauf);
/// die Hülle übersetzt.
/// </summary>
/// <param name="Normpunkt">Der Normpunkt als Kurzzeichen, etwa „B0/W55".</param>
/// <param name="HeizleistungKw">Die Heizleistung [kW].</param>
/// <param name="Cop">Die Leistungszahl.</param>
public sealed record WaermepumpeArbeitspunkt(string Normpunkt, double HeizleistungKw, double Cop);

/// <summary>
/// Die Beschriftungen des Gerätesteckbriefs der Wärmepumpe (WP1a) — ein Bündel, das sich selbst aus
/// <c>MyResource</c> füllt; ein fehlender Schlüssel fällt auf den deutschen Wortlaut zurück. Die
/// Feldbeschriftungen der Stammfelder (Name, Hersteller, …) kommen aus <see cref="WaermepumpeStammFelderTexte"/>.
/// </summary>
public sealed class WaermepumpeSteckbriefTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>WPA_GRP_GERAET — der Gruppenkopf über dem Steckbrief.</summary>
    public string GruppeGeraet { get; set; } = T("WPA_GRP_GERAET", "Gewähltes Gerät");

    /// <summary>WPA_STB_HEIZLEISTUNG — „Heizleistung bei {0}" ({0} = Normpunkt).</summary>
    public string LabelHeizleistung { get; set; } = T("WPA_STB_HEIZLEISTUNG", "Heizleistung bei {0}");

    /// <summary>WPA_STB_COP — „COP bei {0}" ({0} = Normpunkt).</summary>
    public string LabelCop { get; set; } = T("WPA_STB_COP", "COP bei {0}");

    /// <summary>WPA_STB_HEIZLEISTUNG_OHNE — die Beschriftung ohne Arbeitspunkt.</summary>
    public string LabelHeizleistungOhne { get; set; } = T("WPA_STB_HEIZLEISTUNG_OHNE", "Heizleistung am Vorlauf");

    /// <summary>WPA_STB_COP_OHNE — die Beschriftung ohne Arbeitspunkt.</summary>
    public string LabelCopOhne { get; set; } = T("WPA_STB_COP_OHNE", "COP am Vorlauf");

    /// <summary>WPA_STB_ARBEITSPUNKT_HERLEITUNG — die leise Zeile unter der Heizleistung.</summary>
    public string HerleitungArbeitspunkt { get; set; } = T("WPA_STB_ARBEITSPUNKT_HERLEITUNG",
        "Projektkennlinie am eingestellten Vorlauf, interpoliert wie in der Simulation");

    /// <summary>WPA_STB_INVEST</summary>
    public string LabelInvest { get; set; } = T("WPA_STB_INVEST", "Invest");

    /// <summary>WPA_STB_BETRIEB</summary>
    public string LabelBetrieb { get; set; } = T("WPA_STB_BETRIEB", "Betrieb");

    /// <summary>Einheiten; in beiden Sprachen gleich.</summary>
    public string EinheitKw { get; set; } = "kW";
    public string EinheitGrad { get; set; } = "°C";
    public string EinheitEuro { get; set; } = "€";
    public string EinheitEuroJahr { get; set; } = "€/a";
}

/// <summary>
/// <b>Die Zeilen des Gerätesteckbriefs der Wärmepumpe</b> (WP1a, Anwenderentscheid 12.10.2026) — EINE Stelle,
/// lesend aus dem Feldsatz der Anlage (<see cref="WaermepumpeAnlageDaten"/>), den die Felder rechts bearbeiten;
/// so folgt der Steckbrief jeder Eingabe im selben Zeichenlauf. Fehlende Werte stehen als „—".
/// </summary>
public static class WaermepumpeSteckbrief
{
    /// <summary>Die Zeilen in fester Reihenfolge.</summary>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(WaermepumpeAnlageDaten daten, WaermepumpeStammFelderTexte stamm,
        WaermepumpeKonfigurationTexte konfig, WaermepumpeSteckbriefTexte texte, WaermepumpeArbeitspunkt? arbeitspunkt,
        (double Invest, double Betrieb)? kosten)
    {
        var k = CultureInfo.CurrentCulture;
        var z = new List<Steckbriefzeile>
        {
            new(stamm.LabelName, daten.Bezeichner, Kennung: "name"),
            new(stamm.LabelHersteller, daten.Firma, Kennung: "hersteller"),
            new(stamm.LabelTyp, daten.Typ, Kennung: "typ"),
            new(stamm.LabelRegelung, daten.Regelung, Kennung: "stufen"),
            new(stamm.LabelAufstellung, daten.Aufstellung, Kennung: "aufstellung"),
            new(stamm.LabelBaujahr, daten.Baujahr > 0 ? daten.Baujahr.ToString(CultureInfo.InvariantCulture) : null,
                Kennung: "baujahr"),
            new(stamm.LabelNennleistung, daten.Nennleistung > 0 ? daten.Nennleistung.ToString(CultureInfo.InvariantCulture) : null,
                texte.EinheitKw, Kennung: "nennleistung"),
            new(stamm.LabelHeizstab, daten.HeizstabLeistung is int hs && hs > 0 ? hs.ToString(CultureInfo.InvariantCulture) : null,
                texte.EinheitKw, Kennung: "heizstab"),
            new(stamm.LabelKaeltemittel, string.IsNullOrWhiteSpace(daten.Kaeltemittel) ? null : konfig.KaeltemittelText(daten.Kaeltemittel),
                Kennung: "kaeltemittel"),
            new(konfig.LabelHoechstvorlaufLesen, daten.VorlaufMax is double vm ? Zahlen.Anzeigetext(vm) : null,
                texte.EinheitGrad, Kennung: "hoechstvorlauf"),
        };

        if (arbeitspunkt is { } a)
        {
            z.Add(new(string.Format(k, texte.LabelHeizleistung, a.Normpunkt), Zahlen.Anzeigetext(a.HeizleistungKw, 1),
                      texte.EinheitKw, texte.HerleitungArbeitspunkt, "heizleistung"));
            z.Add(new(string.Format(k, texte.LabelCop, a.Normpunkt), Zahlen.Anzeigetext(a.Cop, 2), Kennung: "cop"));
        }
        else
        {
            z.Add(new(texte.LabelHeizleistungOhne, null, Kennung: "heizleistung"));
            z.Add(new(texte.LabelCopOhne, null, Kennung: "cop"));
        }

        z.Add(new(texte.LabelInvest, kosten is { } ki ? ki.Invest.ToString("N0", k) : null, texte.EinheitEuro, Kennung: "invest"));
        z.Add(new(texte.LabelBetrieb, kosten is { } kb ? kb.Betrieb.ToString("N0", k) : null, texte.EinheitEuroJahr, Kennung: "betrieb"));
        return z;
    }
}
