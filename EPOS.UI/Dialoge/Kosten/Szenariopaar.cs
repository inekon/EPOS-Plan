using System.Globalization;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// ETAPPE E9b (Konzept § 2.11.5 „Pflege"; Entscheid E9b‑Q1, Lesart a) — der
/// <b>±-Knopf als einheitliches Muster</b>: der Parametersatz, mit dem ein Wirt den
/// <see cref="CaseEingabeDialog"/> als SZENARIOPAAR öffnet, und die Regeln, die alle
/// Wirte teilen.
///
/// <para><b>Vier Wirte, ein Weg.</b> Die Trägerkarte (Arbeits-, Grund- und
/// Leistungspreis), der Parameterdialog (Einspeisevergütung PV), der Dialog
/// „BHKW-Wirtschaftlichkeit" (Einspeisevergütung KWK) und der PV-Vergütungsdialog
/// (DV-Entgelt, PPA-Preis) bauen ihren Satz hier — mit denselben Beschriftungen,
/// derselben Nullregel und demselben Hilfeschlüssel. Was ein Wirt selbst beisteuert,
/// sind Wert, Einheit, Stellen und seine Kohärenzzeilen.</para>
///
/// <para><b>Die Nullregel des Kerns</b> (<see cref="SzenarioSatz.Gepflegt"/>): leer
/// oder 0 heißt „wie Erwartet", und ein Wert, der sich nicht um mehr als 1e−9 vom
/// Erwartet-Wert unterscheidet, ist keine Pflege. Der Dialog gibt „nicht gepflegt" als
/// 0 zurück; <see cref="Wert"/> macht daraus das <c>null</c> der Datenhaltung.</para>
/// </summary>
public static class Szenariopaar
{
    /// <summary>
    /// Der Hilfeschlüssel der neuen Verwendung (<c>help_mapping.txt</c> →
    /// Wirtschaftlichkeit, Abschnitt „Weitere Werte je Szenario"). Die Kostenposition
    /// behält <c>Form_CaseEingabe.btn_Help</c>; das Präfix <c>Form_Wirtschaftlichkeit</c>
    /// ordnet den Assistenten dem Bereich Wirtschaftlichkeit zu.
    /// </summary>
    public const string HILFESCHLUESSEL = "Form_WirtschaftlichkeitSzenariowerte.btn_Help";

    /// <summary>Die Texte — einmal aufgelöst.</summary>
    public static SzenariopaarTexte Texte => new();

    /// <summary>
    /// Ist das Paar gepflegt — trägt Best oder Worst einen Wert, der sich vom Erwartet-Wert
    /// unterscheidet? Dieselbe Regel wie im Kern (<see cref="SzenarioSatz.Gepflegt"/>).
    /// </summary>
    public static bool Gepflegt(double? best, double? worst, double erwartet)
        => SzenarioSatz.Gepflegt(best, erwartet) || SzenarioSatz.Gepflegt(worst, erwartet);

    /// <summary>
    /// Der Rückweg des Dialogs in die Datenhaltung: 0 (der Dialog meldet „nicht gepflegt"
    /// als 0) wird <c>null</c> — „wie Erwartet". Jeder andere Wert bleibt, auch einer gleich
    /// dem Erwartet-Wert: Er ist folgenlos, und wer ihn eingetragen hat, sieht ihn wieder.
    /// </summary>
    public static double? Wert(double ausDialog) => ausDialog == 0 ? null : ausDialog;

    /// <summary>
    /// Der Kurztext des ±-Knopfes: was er tut, und — wenn gepflegt — die zwei Werte.
    /// </summary>
    public static string Kurztext(double? best, double? worst, double erwartet, string einheit,
                                  int stellen)
    {
        SzenariopaarTexte t = Texte;
        if (!Gepflegt(best, worst, erwartet)) return t.KnopfKurztext;
        return string.Format(Kultur, t.KnopfGepflegt,
                             Zahl(best, einheit, stellen, t), Zahl(worst, einheit, stellen, t));
    }

    /// <summary>
    /// Der Parametersatz des <see cref="CaseEingabeDialog"/> für ein Szenariopaar.
    /// </summary>
    /// <param name="feld">Die Beschriftung der Gruppe („Arbeitspreis").</param>
    /// <param name="groesse">Was gepflegt wird („Arbeitspreis Erdgas E") — Auskunft für
    /// den Assistenten; der Wirt nimmt sie auch für den Titel seiner Überlagerung.</param>
    /// <param name="einheit">Die Einheit der Werte („€/Nm³").</param>
    /// <param name="erwartet">Der wirksame Erwartet-Wert; <c>null</c> oder 0 = keiner.</param>
    /// <param name="best">Der gepflegte Best-Wert; <c>null</c> = wie Erwartet.</param>
    /// <param name="worst">Der gepflegte Worst-Wert; <c>null</c> = wie Erwartet.</param>
    /// <param name="stellen">Nachkommastellen von Anzeige und Rundung.</param>
    /// <param name="maxWert">Obergrenze im Absolutmodus (die des Feldes daneben).</param>
    /// <param name="hinweise">Die Kohärenzzeilen des Wirts; <c>null</c> = keine.</param>
    /// <param name="warnenOhneErwartet">E9b‑Q4: Warnt der Dialog, wenn kein Erwartet-Wert
    /// gepflegt ist? <c>false</c> beim Grundpreis — 0 €/a ist dort ein gültiger
    /// Vertragswert und keine Lücke.</param>
    public static IReadOnlyDictionary<string, object> Gaben(
        string feld, string groesse, string einheit, double? erwartet,
        double? best, double? worst, int stellen, double maxWert,
        IEnumerable<string>? hinweise = null, bool warnenOhneErwartet = true)
    {
        SzenariopaarTexte t = Texte;
        double betrag = erwartet ?? 0.0;
        return new Dictionary<string, object>
        {
            ["Szenariopaar"] = true,
            ["Groesse"] = groesse ?? "",
            ["Betrag"] = betrag,
            ["BestCase"] = best ?? 0.0,
            ["WorstCase"] = worst ?? 0.0,
            ["Einheit"] = einheit ?? "",
            ["Nachkommastellen"] = stellen,
            ["MinWert"] = 0.0,
            ["MaxWert"] = maxWert,
            ["LabelKosten"] = feld ?? "",
            ["LabelBestKosten"] = t.LabelBest,
            ["LabelWorstKosten"] = t.LabelWorst,
            ["LabelAbsolut"] = string.Format(Kultur, t.Absolut, einheit ?? ""),
            ["LabelProzent"] = t.Prozent,
            ["VorlageUmrechnung"] = t.Umrechnung,
            ["ErwartetText"] = betrag != 0
                ? string.Format(Kultur, t.Erwartet, betrag.ToString("N" + stellen, Kultur), einheit ?? "")
                : t.ErwartetLeer,
            ["WarnungOhneErwartet"] = warnenOhneErwartet ? t.OhneErwartet : "",
            ["HinweisLeer"] = t.Leer,
            ["Hinweise"] = (hinweise ?? Array.Empty<string>())
                .Where(h => !string.IsNullOrWhiteSpace(h)).ToList(),
            ["HilfeSchluessel"] = HILFESCHLUESSEL,
            ["OkText"] = t.Ok,
            ["AbbrechenText"] = t.Abbrechen
        };
    }

    /// <summary>Der Titel der Überlagerung: „Szenariowerte — ‹Größe›".</summary>
    public static string Titel(string groesse)
        => string.Format(Kultur, Texte.Titel, groesse ?? "");

    private static CultureInfo Kultur => BhwTexte.Kultur;

    private static string Zahl(double? wert, string einheit, int stellen, SzenariopaarTexte t)
        => wert.HasValue && wert.Value != 0
           ? wert.Value.ToString("N" + stellen, Kultur) + " " + einheit
           : t.WieErwartet;
}

/// <summary>
/// ETAPPE E9b — die Beschriftungen des Szenariopaars (<c>SZP_*</c>), einmal aufgelöst;
/// jeder Text mit deutschem Rückfall (Bauweise <see cref="WirtschaftlichkeitParameterTexte"/>).
/// </summary>
public sealed class SzenariopaarTexte
{
    private static string T(string schluessel, string rueckfall) => BhwTexte.T(schluessel, rueckfall);

    /// <summary>SZP_TITEL — der Titel der Überlagerung, <c>{0}</c> = die Größe.</summary>
    public string Titel { get; } = T("SZP_TITEL", "Szenariowerte — {0}");

    /// <summary>SZP_LABEL_BEST.</summary>
    public string LabelBest { get; } = T("SZP_LABEL_BEST", "Best (Günstig):");

    /// <summary>SZP_LABEL_WORST.</summary>
    public string LabelWorst { get; } = T("SZP_LABEL_WORST", "Worst (Ungünstig):");

    /// <summary>SZP_ABSOLUT — <c>{0}</c> = die Einheit.</summary>
    public string Absolut { get; } = T("SZP_ABSOLUT", "Eingabe absolut [{0}]");

    /// <summary>SZP_PROZENT.</summary>
    public string Prozent { get; } = T("SZP_PROZENT", "Eingabe in % vom Erwartet-Wert");

    /// <summary>SZP_UMRECHNUNG — die Umrechnungszeile des Prozentmodus, <c>{0}</c>/<c>{1}</c>
    /// sind fertig formatierte Werte mit Einheit.</summary>
    public string Umrechnung { get; } = T("SZP_UMRECHNUNG", "ergibt: Best {0} · Worst {1}");

    /// <summary>SZP_ERWARTET — <c>{0}</c> = Wert, <c>{1}</c> = Einheit.</summary>
    public string Erwartet { get; } = T("SZP_ERWARTET", "Erwartet: {0} {1}");

    /// <summary>SZP_ERWARTET_LEER.</summary>
    public string ErwartetLeer { get; } = T("SZP_ERWARTET_LEER", "Erwartet: kein Wert gepflegt");

    /// <summary>SZP_OHNE_ERWARTET — die Warnung nach E9b‑Q4 (warnen, nicht verweigern).</summary>
    public string OhneErwartet { get; } = T("SZP_OHNE_ERWARTET",
        "Kein Erwartet-Wert gepflegt: Günstig und Ungünstig rechnen mit ihrem Szenariowert, " +
        "Erwartet zeigt die Datenlücke. Gespeichert wird trotzdem.");

    /// <summary>SZP_LEER — was ein leeres Feld heißt.</summary>
    public string Leer { get; } = T("SZP_LEER",
        "Leer oder 0 heißt „wie Erwartet“; ein Wert gleich dem Erwartet-Wert ist keine Pflege.");

    /// <summary>SZP_KNOPF_KURZ — der Kurztext eines ±-Knopfes ohne Pflege.</summary>
    public string KnopfKurztext { get; } = T("SZP_KNOPF_KURZ", "Szenariowerte Best/Worst pflegen");

    /// <summary>SZP_KNOPF_GEPFLEGT — der Kurztext mit Pflege, <c>{0}</c>/<c>{1}</c> = Best/Worst.</summary>
    public string KnopfGepflegt { get; } = T("SZP_KNOPF_GEPFLEGT",
        "Szenariowerte gepflegt — Best {0} · Worst {1}");

    /// <summary>SZP_WIE_ERWARTET.</summary>
    public string WieErwartet { get; } = T("SZP_WIE_ERWARTET", "wie Erwartet");

    /// <summary>SZP_GEPFLEGT — das Wort im aria-label des Knopfes.</summary>
    public string Gepflegt { get; } = T("SZP_GEPFLEGT", "gepflegt");

    /// <summary>SZP_OHNE_ERWARTET_KURZ — das Wort im aria-label bei gesetzter Warnung.</summary>
    public string OhneErwartetKurz { get; } = T("SZP_OHNE_ERWARTET_KURZ", "ohne Erwartet-Wert");

    /// <summary>KI_DLG_CSE_NUR_KOSTEN — der Grund, mit dem der Assistent im Szenariopaar
    /// Nutzungsdauer, Startjahr und Zuschuss nicht setzt.</summary>
    public string NurKosten { get; } = T("KI_DLG_CSE_NUR_KOSTEN",
        "Im Szenariopaar führt die Maske weder Nutzungsdauer noch Startjahr noch Zuschuss — " +
        "sie pflegt nur den Best- und den Worst-Wert.");

    /// <summary>ALLG_BTN_OK — der Hausknopf.</summary>
    public string Ok { get; } = T("ALLG_BTN_OK", "OK");

    /// <summary>ALLG_BTN_ABBRECHEN — der Hausknopf.</summary>
    public string Abbrechen { get; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");
}
