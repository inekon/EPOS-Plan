using System.Collections.Generic;
using KiKern;
using WindowsFormsApplication1;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Die acht Felder der Feldgruppe „Deckung“ einer Kältezuordnung (Konzept Kältebedarf 3.4, Schritt 213) — das
/// plattformfreie Abbild der Deckungsfelder von <c>Z_ProjWaermebedarfModel</c> bzw. <c>Z_ProjektKaeltebedarfModel</c>.
/// Der Baustein <see cref="DeckungsartFelder"/> schreibt an Ort und Stelle hinein; Hülle und Wirt bilden es auf ihr
/// Modell ab.
///
/// <para><b>Die Steuerwerte</b> sind die des Kerns (<see cref="KaeltebedarfSchema.DECKUNG_ZENTRAL"/>,
/// <see cref="KaeltebedarfSchema.DECKUNG_SPLIT"/>, <see cref="KaeltebedarfSchema.EER_FEST"/>,
/// <see cref="KaeltebedarfSchema.EER_LINEAR"/>), nie Anzeigetexte. Bei „zentral“ bleiben die Split-Felder in der
/// Arbeitskopie stehen (ein Zurückschalten stellt sie wieder her); geleert werden sie beim Speichern durch den Kern
/// (<c>Z_ProjektKaeltebedarfCtrl.Normalisieren</c>).</para>
///
/// <para><b>Die Prüfregel</b> ist die des Kerns (<see cref="Kaeltedeckungsregel.Pruefe"/>),
/// dieselbe, die der Schreibweg und das Warnkriterium <c>DEZENTRALE_KAELTE_OHNE_EER</c> anwenden.</para>
/// </summary>
public sealed class DeckungsartDaten
{
    /// <summary>Vorgabe des festen EER bzw. des EER am Punkt 1 — runder Ordnungswert für Kleinsplitgeräte, kein Produktwert.</summary>
    public const double VORGABE_EER_1 = 3.0;

    /// <summary>Vorgabe der Außentemperatur am Punkt 1 [°C].</summary>
    public const double VORGABE_TAUSSEN_1 = 35.0;

    /// <summary>Vorgabe des EER am Punkt 2 (nur linear).</summary>
    public const double VORGABE_EER_2 = 4.0;

    /// <summary>Vorgabe der Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
    public const double VORGABE_TAUSSEN_2 = 25.0;

    /// <summary>Deckungsart als Steuerwert; Vorgabe „zentral“.</summary>
    public string Deckungsart { get; set; } = KaeltebedarfSchema.DECKUNG_ZENTRAL;

    /// <summary>EER-Weg bei Split (<c>fest</c>/<c>linear</c>); leer bei zentral.</summary>
    public string? EerWeg { get; set; }

    /// <summary>Fester EER bzw. EER am Punkt 1.</summary>
    public double? Eer1 { get; set; }

    /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
    public double? Taussen1 { get; set; }

    /// <summary>EER am Punkt 2 (nur linear).</summary>
    public double? Eer2 { get; set; }

    /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
    public double? Taussen2 { get; set; }

    /// <summary>Kühlträger des Split-Stroms; <c>null</c> = Stromträger des Projekts.</summary>
    public int? KuehlIdCarrier { get; set; }

    /// <summary>Eigener Zähler des Split-Stroms.</summary>
    public bool KuehlEigenerZaehler { get; set; }

    /// <summary>Dezentral (Split)?</summary>
    public bool IstSplit => Deckungsart == KaeltebedarfSchema.DECKUNG_SPLIT;

    /// <summary>Dezentral mit EER linear über der Außentemperatur?</summary>
    public bool IstLinear => IstSplit && EerWeg == KaeltebedarfSchema.EER_LINEAR;

    /// <summary>
    /// Setzt die Deckungsart; der Wechsel auf „dezentral“ füllt leere Felder mit den Vorgaben (EER-Weg fest,
    /// 3,0 · 35 °C).
    /// </summary>
    public void DeckungSetzen(string deckung)
    {
        Deckungsart = deckung == KaeltebedarfSchema.DECKUNG_SPLIT ? KaeltebedarfSchema.DECKUNG_SPLIT : KaeltebedarfSchema.DECKUNG_ZENTRAL;
        if (!IstSplit) return;
        if (EerWeg != KaeltebedarfSchema.EER_FEST && EerWeg != KaeltebedarfSchema.EER_LINEAR) EerWeg = KaeltebedarfSchema.EER_FEST;
        Eer1 ??= VORGABE_EER_1;
        Taussen1 ??= VORGABE_TAUSSEN_1;
        if (IstLinear) PunktZweiVorbelegen();
    }

    /// <summary>Setzt den EER-Weg; der Wechsel auf „linear“ füllt einen leeren Punkt 2 mit der Vorgabe 4,0 · 25 °C.</summary>
    public void EerWegSetzen(string weg)
    {
        EerWeg = weg == KaeltebedarfSchema.EER_LINEAR ? KaeltebedarfSchema.EER_LINEAR : KaeltebedarfSchema.EER_FEST;
        if (IstLinear) PunktZweiVorbelegen();
    }

    private void PunktZweiVorbelegen()
    {
        Eer2 ??= VORGABE_EER_2;
        Taussen2 ??= VORGABE_TAUSSEN_2;
    }

    /// <summary>Steht Punkt 1 auf der Vorgabe 3,0 · 35 °C? Dann nennt der Baustein die Herleitungszeile.</summary>
    public bool PunktEinsIstVorgabe => IstSplit && Eer1 == VORGABE_EER_1 && Taussen1 == VORGABE_TAUSSEN_1;

    /// <summary>Steht Punkt 2 auf der Vorgabe 4,0 · 25 °C (nur linear)?</summary>
    public bool PunktZweiIstVorgabe => IstLinear && Eer2 == VORGABE_EER_2 && Taussen2 == VORGABE_TAUSSEN_2;

    /// <summary>
    /// Der Grund, aus dem die Felder unzulässig sind; <c>null</c> = zulässig. Die Regel des Kerns
    /// (<see cref="Kaeltedeckungsregel.Pruefe"/>) — der Grund ist ein
    /// Prüftext des Kerns, der Dialog meldet dem Anwender <c>DECK_MSG_OHNE_EER</c>.
    /// </summary>
    public string? Pruefgrund() => Kaeltedeckungsregel.Pruefe(AlsModell());

    /// <summary>Die Felder als Profilzeile des Kerns — für die Prüfregel.</summary>
    public Z_ProjektKaeltebedarfModel AlsModell() => new()
    {
        Deckung = Deckungsart, EerWeg = EerWeg, Eer1 = Eer1, Taussen1 = Taussen1, Eer2 = Eer2, Taussen2 = Taussen2,
        KuehlIdCarrier = KuehlIdCarrier, KuehlEigenerZaehler = KuehlEigenerZaehler
    };

    /// <summary>
    /// Die Zusammenfassung in einer Zeile: „zentral (Kältefolge)“ bzw. „dezentral (Split) · EER 3,00 (35 °C)“, linear
    /// mit beiden Punkten.
    /// </summary>
    public string Zusammenfassung()
    {
        string art = Deckungsarten[IstSplit ? 1 : 0].Text;
        if (!IstSplit) return art;
        var c = System.Globalization.CultureInfo.CurrentCulture;
        string Punkt(double? eer, double? t)
            => (eer?.ToString("0.00", c) ?? "–") + " (" + (t?.ToString("0.#", c) ?? "–") + " °C)";
        string eerText = R.DECK_LBL_EER + " " + Punkt(Eer1, Taussen1);
        if (IstLinear) eerText += " … " + Punkt(Eer2, Taussen2);
        return art + " · " + eerText;
    }

    /// <summary>Eine flache Kopie.</summary>
    public DeckungsartDaten Kopie() => (DeckungsartDaten)MemberwiseClone();

    /// <summary>Die Deckungsarten: Steuerwert und Anzeigetext.</summary>
    public static IReadOnlyList<(string Wert, string Text)> Deckungsarten => new[]
    {
        (KaeltebedarfSchema.DECKUNG_ZENTRAL, R.DECK_ART_ZENTRAL),
        (KaeltebedarfSchema.DECKUNG_SPLIT, R.DECK_ART_SPLIT)
    };

    /// <summary>Die EER-Wege: Steuerwert und Anzeigetext.</summary>
    public static IReadOnlyList<(string Wert, string Text)> EerWege => new[]
    {
        (KaeltebedarfSchema.EER_FEST, R.DECK_EER_FEST),
        (KaeltebedarfSchema.EER_LINEAR, R.DECK_EER_LINEAR)
    };

    /// <summary>Die Einträge einer Werteliste für den Hilfe-Assistenten.</summary>
    internal static IReadOnlyList<KiWahleintrag> KiEintraege(IReadOnlyList<(string Wert, string Text)> liste)
    {
        var l = new List<KiWahleintrag>();
        foreach ((string wert, string text) in liste) l.Add(new KiWahleintrag(wert, text));
        return l;
    }
}

/// <summary>
/// Der Modus des Dialogs „Wärmebedarf extern“ (Konzept Kältebedarf 3.2): <see cref="Alle"/> zeigt jeden Kanal (Vorgabe),
/// <see cref="NurKuehlung"/> ist der Dialog „Kältebedarf extern“ — nur die Zeilen des Kanals Kühlung, Kanalwahl fest.
/// </summary>
public enum WaermebedarfExternModus
{
    /// <summary>Alle Kanäle — der Dialog „Wärmebedarf extern“.</summary>
    Alle = 0,

    /// <summary>Nur der Kanal Kühlung — der Dialog „Kältebedarf extern“.</summary>
    NurKuehlung = 1
}
