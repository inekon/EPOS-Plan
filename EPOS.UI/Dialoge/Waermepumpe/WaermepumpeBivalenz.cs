using System.Globalization;

namespace EPOS.UI.Dialoge.Waermepumpe;

/// <summary>Der Zustand der Herleitungszeile „Übergabe und Bivalenz" (Fachkonzept Übergabegrenze 6.2; Umsetzungskonzept 6.2).</summary>
public enum BivalenzKennzeichen
{
    /// <summary>Übergabe beschrieben und Einbindung gesetzt: Die Übergabegrenze wirkt.</summary>
    Wirksam = 0,

    /// <summary>Übergabe beschrieben, Einbindung nicht gesetzt: Die Übergabegrenze ruht (U‑1); die Werte stehen trotzdem da.</summary>
    NichtWirksam = 1,

    /// <summary>Keine Übergabedaten (Kopplung aus oder Übergabe „ideal"): ohne Übergabegrenze.</summary>
    OhneKopplung = 2,

    /// <summary>Gekoppelt, aber ohne Lauf nicht herleitbar (Zonen, Verbrauchsangabe, keine Klimaregion).</summary>
    Unvollstaendig = 3,

    /// <summary>Die Wärmepumpe führt keine Kennlinie — keine Bivalenzpunkte.</summary>
    OhneKennfeld = 4,

    /// <summary>Der Eingangsbauer lehnt ein Gebäude ab; der Grund steht in <see cref="WaermepumpeBivalenzWerte.Befund"/>.</summary>
    Befund = 5,
}

/// <summary>
/// <b>Die Werte der Herleitungszeile</b> — das plattformfreie Abbild der <c>Bivalenzherleitung</c> des Kerns,
/// gefüllt von der Abbildung <c>BivalenzAbbildung</c> (EPOS.UI.Daten). Leistungen in kW, Anteile als Bruch,
/// Temperaturen in °C; NaN = kein Wert (ein Bivalenzpunkt außerhalb des Auslegungsbereichs, kein Kessel).
/// </summary>
public sealed record WaermepumpeBivalenzWerte
{
    /// <summary>Zustand der Zeile.</summary>
    public BivalenzKennzeichen Kennzeichen { get; init; }

    /// <summary>Höchstvorlauf θ_WP,max [°C], zu dem gerechnet wurde.</summary>
    public double HoechstvorlaufC { get; init; } = double.NaN;

    /// <summary>Übergabegrenze Φ_UE,max [kW].</summary>
    public double UebergabeKw { get; init; } = double.NaN;

    /// <summary>Heizlast Φ_N [kW].</summary>
    public double HeizlastKw { get; init; } = double.NaN;

    /// <summary>Anteil Φ_UE,max/Φ_N [–].</summary>
    public double Anteil { get; init; } = double.NaN;

    /// <summary>Rücklauf bei Übergabegrenze θ_R,UE [°C].</summary>
    public double RuecklaufC { get; init; } = double.NaN;

    /// <summary>Spreizung Δθ_UE [K].</summary>
    public double SpreizungK { get; init; } = double.NaN;

    /// <summary>Erster Bivalenzpunkt θ_biv,1 [°C].</summary>
    public double ErsterC { get; init; } = double.NaN;

    /// <summary>Bivalenzpunkt nach Kennfeld allein [°C].</summary>
    public double KennfeldAlleinC { get; init; } = double.NaN;

    /// <summary>Zweiter Bivalenzpunkt θ_biv,2 [°C].</summary>
    public double ZweiterC { get; init; } = double.NaN;

    /// <summary>Rechnet der zweite Punkt mit Vorwärmbetrieb?</summary>
    public bool Vorwaermbetrieb { get; init; }

    /// <summary>Anteil der Kennfeldleistung bei −7 °C an der Kesselleistung [–]; NaN ohne Kessel.</summary>
    public double KesselAnteil { get; init; } = double.NaN;

    /// <summary>Mindestanteil nach § 43 GModG [–] (30 % parallel/teilparallel, 40 % alternativ).</summary>
    public double KesselMindestanteil { get; init; } = double.NaN;

    /// <summary>Der benannte Grund bei <see cref="BivalenzKennzeichen.Befund"/>; sonst leer.</summary>
    public string Befund { get; init; } = "";
}

/// <summary>
/// Ein Eintrag der Klappliste „Kältemittel" (Tafel 6.3 des Fachkonzepts): Code, Höchstvorlauf der Klasse [°C]
/// (<c>null</c> = aus dem Gerät), bei R744 Rücklaufgrenze und Bezugsrücklauf der Abwertung [°C].
/// </summary>
public sealed record KaeltemittelEintrag(string Code, double? HoechstvorlaufC,
                                         double? RuecklaufGrenzeC = null, double? BezugsruecklaufC = null);

/// <summary>
/// <b>Die Herleitungszeile in Worten</b> — EINE Stelle für Dialog und KI-Sicht. Zahlen in der Oberflächenkultur
/// (<see cref="CultureInfo.CurrentCulture"/>), Bivalenzpunkte mit Vorzeichen (+1,8 °C, −3,6 °C).
/// </summary>
public static class WaermepumpeBivalenzText
{
    private const string FORMAT_PUNKT = "+0.0;−0.0;0.0";

    /// <summary>Die Werte zum Arbeitsstand: neu gerechnet über <see cref="WaermepumpeAnlageDaten.BivalenzRechnen"/>, sonst die gelesenen.</summary>
    public static WaermepumpeBivalenzWerte? Werte(WaermepumpeAnlageDaten? d)
        => d is null ? null : d.BivalenzRechnen?.Invoke(d) ?? d.Bivalenz;

    /// <summary>Die Zeile zum Arbeitsstand von <paramref name="d"/>; leer ohne Herleitung.</summary>
    public static string Zeile(WaermepumpeAnlageDaten? d, WaermepumpeKonfigurationTexte texte)
        => Zeile(Werte(d), texte);

    /// <summary>Die Zeile zu <paramref name="w"/>; leer ohne Werte.</summary>
    public static string Zeile(WaermepumpeBivalenzWerte? w, WaermepumpeKonfigurationTexte texte)
    {
        if (w is null) return "";
        texte ??= new WaermepumpeKonfigurationTexte();
        CultureInfo k = CultureInfo.CurrentCulture;
        switch (w.Kennzeichen)
        {
            case BivalenzKennzeichen.OhneKopplung: return texte.HerleitungOhneKopplung;
            case BivalenzKennzeichen.Unvollstaendig: return texte.HerleitungUnvollstaendig;
            case BivalenzKennzeichen.Befund: return string.Format(k, texte.HerleitungBefund, w.Befund);
        }

        var teile = new List<string>
        {
            string.Format(k, texte.HerleitungUebergabe,
                          w.HoechstvorlaufC.ToString("0.#", k), w.UebergabeKw.ToString("0.0", k),
                          w.HeizlastKw.ToString("0.0", k), (w.Anteil * 100.0).ToString("0", k),
                          w.RuecklaufC.ToString("0.0", k), w.SpreizungK.ToString("0.0", k)),
        };
        if (w.Kennzeichen == BivalenzKennzeichen.OhneKennfeld)
            teile.Add(texte.HerleitungOhneKennfeld);
        else
        {
            teile.Add(string.Format(k, texte.HerleitungBivalenzpunkte, Punkt(w.ErsterC, texte, k),
                                    Punkt(w.KennfeldAlleinC, texte, k), Punkt(w.ZweiterC, texte, k),
                                    w.Vorwaermbetrieb ? texte.HerleitungVorwaermbetrieb : ""));
            if (!double.IsNaN(w.KesselAnteil))
                teile.Add(string.Format(k, texte.HerleitungKesselanteil, (w.KesselAnteil * 100.0).ToString("0", k),
                                        (w.KesselMindestanteil * 100.0).ToString("0", k)));
        }
        string zeile = string.Join(" · ", teile) + ".";
        return w.Kennzeichen == BivalenzKennzeichen.NichtWirksam
            ? texte.HerleitungNichtWirksam + " " + zeile
            : zeile;
    }

    /// <summary>Ein Bivalenzpunkt mit Vorzeichen und Einheit; „keiner" außerhalb des Auslegungsbereichs.</summary>
    private static string Punkt(double c, WaermepumpeKonfigurationTexte texte, CultureInfo k)
        => double.IsNaN(c) ? texte.HerleitungKeinPunkt : c.ToString(FORMAT_PUNKT, k) + " °C";
}
