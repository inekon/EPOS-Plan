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

    /// <summary>Der eingegebene Abschaltpunkt [°C], sofern er nach der Betriebsart gilt; sonst <c>null</c>.</summary>
    public double? AbschaltpunktC { get; init; }

    /// <summary>Der maßgebende Abschaltpunkt [°C] (UB‑Q4 a: der wärmere aus eingegeben und berechnet); NaN = keiner.</summary>
    public double MassgebendC { get; init; } = double.NaN;

    /// <summary>Die Lesewerte der Gerätegrenzen; <c>null</c> = keine (ohne Abbildung).</summary>
    public WaermepumpeGrenzwerte? Grenzen { get; init; }

    /// <summary>Die weichen Sperren und Hinweise der Dialogprüfung des Kerns, in fester Reihenfolge.</summary>
    public IReadOnlyList<WaermepumpeBivalenzBefund> Befunde { get; init; } = Array.Empty<WaermepumpeBivalenzBefund>();
}

/// <summary>Woher ein Lesewert der Gerätegrenzen stammt (Umsetzungskonzept Übergabegrenze 6.2).</summary>
public enum GrenzwertHerkunft
{
    /// <summary>Gepflegter Wert des Geräts („Katalog").</summary>
    Katalog = 0,

    /// <summary>„Vorgabe nach Kältemittel".</summary>
    VorgabeKaeltemittel = 1,

    /// <summary>„Vorgabe" (allgemein, unterkritisch).</summary>
    Vorgabe = 2,

    /// <summary>„abgeleitet" (Höchstvorlauf − Mindestspreizung).</summary>
    Abgeleitet = 3,
}

/// <summary>
/// Die Lesewerte der Gruppe „Bivalenz und Übergabe": Spreizungen [K], Mindestvolumenstrom [–], höchster Rücklauf [°C]
/// je mit Herkunft, Höchstvorlauf [°C] (NaN = keiner) und bei R744 Bezugsrücklauf, Abwertung [%/K] und Grenze.
/// </summary>
public sealed record WaermepumpeGrenzwerte(
    double SpreizungAuslegungK,
    double SpreizungMaxK,
    double SpreizungMinK,
    GrenzwertHerkunft SpreizungHerkunft,
    double MindestvolumenstromAnteil,
    GrenzwertHerkunft MindestvolumenstromHerkunft,
    double RuecklaufMaxC,
    GrenzwertHerkunft RuecklaufHerkunft,
    double HoechstvorlaufC,
    double? BezugsruecklaufC = null,
    double? AbwertungProzentJeK = null,
    double? RuecklaufGrenzeR744C = null);

/// <summary>Die Art eines Befunds der Dialogprüfung (Abbild von <c>Pruefbefundart</c> des Kerns).</summary>
public enum BivalenzBefundArt
{
    /// <summary>σ_min ≥ σ_max.</summary>
    Spreizung = 0,

    /// <summary>Höchstvorlauf unter dem Auslegungsvorlauf einer Flächenheizung.</summary>
    Hoechstvorlauf = 1,

    /// <summary>Rücklaufgrenze unter dem Auslegungsrücklauf aller Zonen.</summary>
    RuecklaufNie = 2,

    /// <summary>Vorwärmbetrieb ohne Kessel oder Heizstab in der Kaskade.</summary>
    VorwaermOhneKessel = 3,

    /// <summary>Wärmepumpe hinter dem Kessel bei Vorwärmbetrieb.</summary>
    Kaskade = 4,

    /// <summary>Hinweis: Hybrid-Mindestanteil nach § 43 GModG unterschritten.</summary>
    Gmodg = 5,

    /// <summary>Hinweis: Die Heizflächen begrenzen stärker als das Kennfeld.</summary>
    UebergabeBegrenzt = 6,
}

/// <summary>Ein Befund mit Zahlen für den Wortlaut; <see cref="NurHinweis"/> = Stufe Hinweis, sonst Warnung. Speichern bleibt möglich.</summary>
public sealed record WaermepumpeBivalenzBefund(BivalenzBefundArt Art, bool NurHinweis, double Wert1 = double.NaN,
                                               double Wert2 = double.NaN);

/// <summary>
/// Ein Eintrag der Klappliste „Kältemittel" (Tafel 6.3 des Fachkonzepts): Code, Höchstvorlauf der Klasse [°C]
/// (<c>null</c> = aus dem Gerät), bei R744 Rücklaufgrenze und Bezugsrücklauf der Abwertung [°C].
///
/// <para><b>Schnellwahl im Stammblatt</b> (UB‑E3‑b): <see cref="Klasse"/> sagt, ob der Code eine eigene Zeile der Tafel
/// 6.3 hat; dann füllt seine Wahl leere Gerätefelder mit den Spreizungen [K], dem Mindestvolumenstrom [%] und bei R744
/// mit Rücklaufgrenze, Bezugsrücklauf und Abwertung [%/K]. Ohne Klasse (allgemeine Vorgabe) füllt sie nichts.</para>
/// </summary>
public sealed record KaeltemittelEintrag(string Code, double? HoechstvorlaufC,
                                         double? RuecklaufGrenzeC = null, double? BezugsruecklaufC = null,
                                         bool Klasse = false,
                                         double? SpreizungAuslegungK = null, double? SpreizungMaxK = null,
                                         double? SpreizungMinK = null, double? MindestvolumenstromProzent = null,
                                         double? AbwertungProzentJeK = null);

/// <summary>
/// <b>Die Herleitungszeile in Worten</b> — EINE Stelle für Dialog und KI-Sicht. Zahlen in der Oberflächenkultur
/// (<see cref="CultureInfo.CurrentCulture"/>), Bivalenzpunkte mit Vorzeichen (+1,8 °C, −3,6 °C).
/// </summary>
public static class WaermepumpeBivalenzText
{
    private const string FORMAT_PUNKT = "+0.0;−0.0;0.0";

    /// <summary>Eingegebener und maßgebender Abschaltpunkt: höchstens eine Stelle, mit Vorzeichen.</summary>
    private const string FORMAT_EINGABE = "+0.#;−0.#;0";

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

    /// <summary>
    /// Die Herleitungszeile des Abschaltpunkts (<c>WPA_HERLEITUNG_ABSCHALTPUNKT</c>): eingegeben · aus der Übergabe
    /// berechnet · maßgebend. Leer ohne eingegebenen Abschaltpunkt oder ohne gerechnete Bivalenzpunkte.
    /// </summary>
    public static string AbschaltpunktZeile(WaermepumpeBivalenzWerte? w, WaermepumpeKonfigurationTexte texte)
    {
        if (w?.AbschaltpunktC is not double eingegeben) return "";
        if (w.Kennzeichen is not (BivalenzKennzeichen.Wirksam or BivalenzKennzeichen.NichtWirksam)) return "";
        texte ??= new WaermepumpeKonfigurationTexte();
        CultureInfo k = CultureInfo.CurrentCulture;
        // Eingegeben und maßgebend ohne überflüssige Null (−10 °C, +3 °C), berechnet mit einer Stelle (Mockup).
        string Eingabe(double c) => double.IsNaN(c) ? texte.HerleitungKeinPunkt : c.ToString(FORMAT_EINGABE, k) + " °C";
        return string.Format(k, texte.HerleitungAbschaltpunkt, Eingabe(eingegeben), Punkt(w.ZweiterC, texte, k),
                             Eingabe(w.MassgebendC));
    }

    /// <summary>Der Wortlaut eines Befunds der Dialogprüfung.</summary>
    public static string Befundtext(WaermepumpeBivalenzBefund b, WaermepumpeKonfigurationTexte texte)
    {
        texte ??= new WaermepumpeKonfigurationTexte();
        CultureInfo k = CultureInfo.CurrentCulture;
        string Zahl(double v) => v.ToString("0.#", k);
        return b.Art switch
        {
            BivalenzBefundArt.Spreizung => string.Format(k, texte.WarnSpreizung, Zahl(b.Wert1), Zahl(b.Wert2)),
            BivalenzBefundArt.Hoechstvorlauf => string.Format(k, texte.WarnHoechstvorlauf, Zahl(b.Wert1), Zahl(b.Wert2)),
            BivalenzBefundArt.RuecklaufNie => string.Format(k, texte.WarnRuecklaufNie, Zahl(b.Wert1), Zahl(b.Wert2)),
            BivalenzBefundArt.VorwaermOhneKessel => texte.WarnVorwaermOhneKessel,
            BivalenzBefundArt.Kaskade => string.Format(k, texte.WarnKaskade, Zahl(b.Wert1), Zahl(b.Wert2)),
            BivalenzBefundArt.Gmodg => string.Format(k, texte.WarnGmodg, (b.Wert1 * 100.0).ToString("0", k),
                                                     (b.Wert2 * 100.0).ToString("0", k)),
            BivalenzBefundArt.UebergabeBegrenzt => string.Format(k, texte.WarnUebergabeBegrenzt, Punkt(b.Wert1, texte, k)),
            _ => "",
        };
    }

    /// <summary>Der Wortlaut einer Herkunft.</summary>
    public static string Herkunft(GrenzwertHerkunft h, WaermepumpeKonfigurationTexte texte) => h switch
    {
        GrenzwertHerkunft.Katalog => texte.HerkunftKatalog,
        GrenzwertHerkunft.VorgabeKaeltemittel => texte.HerkunftVorgabeKaeltemittel,
        GrenzwertHerkunft.Abgeleitet => texte.HerkunftAbgeleitet,
        _ => texte.HerkunftVorgabe,
    };

    /// <summary>„5 / 10 / 3" — Spreizung Auslegung / max. / min. ohne Einheit.</summary>
    public static string Spreizungen(WaermepumpeGrenzwerte g)
    {
        CultureInfo k = CultureInfo.CurrentCulture;
        return string.Join(" / ", new[] { g.SpreizungAuslegungK, g.SpreizungMaxK, g.SpreizungMinK }.Select(v => v.ToString("0.#", k)));
    }

    /// <summary>„30 / 2,5 / 40" bei R744, sonst „— / — / —".</summary>
    public static string R744Werte(WaermepumpeGrenzwerte g)
    {
        CultureInfo k = CultureInfo.CurrentCulture;
        string W(double? v) => v is double x ? x.ToString("0.#", k) : "—";
        return W(g.BezugsruecklaufC) + " / " + W(g.AbwertungProzentJeK) + " / " + W(g.RuecklaufGrenzeR744C);
    }

    /// <summary>Die Herleitung des höchsten Rücklaufs: abgeleitet mit Rechnung, bei R744 Bezug/Abwertung/Grenze; sonst leer.</summary>
    public static string RuecklaufZeile(WaermepumpeGrenzwerte? g, WaermepumpeKonfigurationTexte texte)
    {
        if (g is null) return "";
        texte ??= new WaermepumpeKonfigurationTexte();
        CultureInfo k = CultureInfo.CurrentCulture;
        if (g.RuecklaufGrenzeR744C is double grenze && g.BezugsruecklaufC is double bezug && g.AbwertungProzentJeK is double ab)
            return string.Format(k, texte.HerleitungR744, bezug.ToString("0.#", k), ab.ToString("0.#", k), grenze.ToString("0.#", k));
        if (g.RuecklaufHerkunft == GrenzwertHerkunft.Abgeleitet && !double.IsNaN(g.RuecklaufMaxC))
            return string.Format(k, texte.HerleitungRuecklaufAbgeleitet, g.RuecklaufMaxC.ToString("0.#", k),
                                 g.HoechstvorlaufC.ToString("0.#", k), g.SpreizungMinK.ToString("0.#", k));
        return "";
    }

    /// <summary>Ein Bivalenzpunkt mit Vorzeichen und Einheit; „keiner" außerhalb des Auslegungsbereichs.</summary>
    private static string Punkt(double c, WaermepumpeKonfigurationTexte texte, CultureInfo k)
        => double.IsNaN(c) ? texte.HerleitungKeinPunkt : c.ToString(FORMAT_PUNKT, k) + " °C";
}
