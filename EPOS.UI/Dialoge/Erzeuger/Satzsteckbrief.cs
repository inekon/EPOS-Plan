using System.Globalization;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Die gemeinsamen Beschriftungen des Gerätesteckbriefs in den Satz-Überlagerungen der Erzeugerverwaltungen (WP1b,
/// KA‑E‑25) — ein Bündel, das sich selbst aus <c>MyResource</c> füllt; ein fehlender Schlüssel fällt auf den
/// deutschen Wortlaut zurück. Die Zeilenbeschriftungen selbst kommen aus dem Katalogprofil des Feldsatzes (dieselben
/// Ressourcen wie die Felder rechts).
/// </summary>
public class SatzsteckbriefTexte
{
    /// <summary>Liest einen Ressourcentext; fehlt er, gilt der Rückfall.</summary>
    protected static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>WPA_GRP_GERAET — der Gruppenkopf über dem Steckbrief.</summary>
    public string GruppeGeraet { get; set; } = T("WPA_GRP_GERAET", "Gewähltes Gerät");

    /// <summary>SST_GRP_PROJEKT — der Gruppenkopf über den Projektfeldern der linken Spalte.</summary>
    public string GruppeProjekt { get; set; } = T("SST_GRP_PROJEKT", "Anlage im Projekt");

    /// <summary>KBROW_LBL_NAME — die Namenszeile, wenn kein Feldsatz da ist.</summary>
    public string LabelName { get; set; } = T("KBROW_LBL_NAME", "Name:");

    /// <summary>KBROW_LBL_VORLAUF / KBROW_LBL_RUECKLAUF — das Temperaturpaar der Anlage, wo der Wirt es zeigt.</summary>
    public string LabelVorlauf { get; set; } = T("KBROW_LBL_VORLAUF", "Vorlauf:");
    public string LabelRuecklauf { get; set; } = T("KBROW_LBL_RUECKLAUF", "Rücklauf:");

    /// <summary>WPA_STB_INVEST — die Investitionssumme der Anlage.</summary>
    public string LabelInvest { get; set; } = T("WPA_STB_INVEST", "Invest");

    /// <summary>WPA_STB_BETRIEB — die Betriebskostensumme der Anlage.</summary>
    public string LabelBetrieb { get; set; } = T("WPA_STB_BETRIEB", "Betrieb");

    /// <summary>PVW_JA / PVW_NEIN — der Wert eines Schalters.</summary>
    public string Ja { get; set; } = T("PVW_JA", "Ja");
    public string Nein { get; set; } = T("PVW_NEIN", "Nein");

    /// <summary>Einheiten; in beiden Sprachen gleich.</summary>
    public string EinheitEuro { get; set; } = "€";
    public string EinheitGrad { get; set; } = "°C";
    public string EinheitEuroJahr { get; set; } = "€/a";
}

/// <summary>
/// <b>Die Zeilen eines Gerätesteckbriefs aus dem Feldsatz</b> (WP1b, KA‑E‑25) — EINE Stelle für die sechs
/// Erzeugerverwaltungen. Gelesen wird der Feldsatz „Alle Daten" (<see cref="BrowserFeldwert"/>), den die Felder
/// rechts in der Überlagerung bearbeiten; so folgt der Steckbrief jeder Eingabe im selben Zeichenlauf. Beschriftung
/// und Einheit kommen aus dem Feld (Katalogprofil), Zahlen stehen mit <see cref="Zahlen.Anzeigetext(double?, int?)"/>,
/// ein Schalter als Ja/Nein, ein leeres Feld als „—". Ohne Feldsatz (Aufklapper zu, kein Speicherweg) stehen Name und
/// die Anzeigefelder des Detailblocks. Kein Rechenweg.
/// </summary>
public static class Satzsteckbrief
{
    /// <summary>Die Kennung der Investitionszeile — sie weicht den Kostensummen der Anlage.</summary>
    public const string KENNUNG_INVEST = "invest";

    /// <summary>Die Kennung der Namenszeile — sie steht immer, notfalls aus dem Detailblock.</summary>
    public const string KENNUNG_NAME = "name";

    /// <summary>Höchstens so viele Zeilen aus dem Detailblock, wenn kein Feldsatz da ist.</summary>
    public const int HOECHSTE_DETAILZEILEN = 12;

    /// <summary>
    /// Die Zeilen in der Reihenfolge der <paramref name="auswahl"/>. Ein Schlüssel, den der Feldsatz nicht führt,
    /// entfällt. Mit <paramref name="kosten"/> (nur am Projektsatz) stehen am Ende Invest und Betrieb der Anlage, und die
    /// Katalogzeile mit der Kennung <see cref="KENNUNG_INVEST"/> entfällt.
    /// </summary>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(IReadOnlyList<BrowserFeldwert>? felder, ErzeugerDetail? detail,
        IReadOnlyList<(string Schluessel, string Kennung)> auswahl, SatzsteckbriefTexte texte,
        (double Invest, double Betrieb)? kosten = null, IEnumerable<Steckbriefzeile>? zusatz = null)
    {
        var z = new List<Steckbriefzeile>();
        if (felder is { Count: > 0 })
        {
            foreach (var (schluessel, kennung) in auswahl)
            {
                if (kosten is not null && kennung == KENNUNG_INVEST) continue;
                BrowserFeldwert? f = felder.FirstOrDefault(x => string.Equals(x.Schluessel, schluessel, StringComparison.OrdinalIgnoreCase));
                if (f is not null) z.Add(Zeile(f, kennung, texte));
                // Der Name steht immer: führt der Feldsatz ihn nicht, kommt er aus dem Detailblock.
                else if (kennung == KENNUNG_NAME && detail is not null)
                    z.Add(new(Beschriftung(texte.LabelName), detail.Bezeichner, Kennung: KENNUNG_NAME));
            }
        }
        else if (detail is not null)
        {
            z.Add(new(Beschriftung(texte.LabelName), detail.Bezeichner, Kennung: KENNUNG_NAME));
            foreach (var (feld, wert) in detail.Felder.Take(HOECHSTE_DETAILZEILEN))
                z.Add(new(Beschriftung(feld), wert));
        }

        if (zusatz is not null) z.AddRange(zusatz);

        if (kosten is { } k)
        {
            var kultur = CultureInfo.CurrentCulture;
            z.Add(new(texte.LabelInvest, k.Invest.ToString("N0", kultur), texte.EinheitEuro, Kennung: KENNUNG_INVEST));
            z.Add(new(texte.LabelBetrieb, k.Betrieb.ToString("N0", kultur), texte.EinheitEuroJahr, Kennung: "betrieb"));
        }
        return z;
    }

    /// <summary>Eine Zeile aus einem Feld: Beschriftung ohne „:", Wert nach Feldart, Einheit des Profils.</summary>
    public static Steckbriefzeile Zeile(BrowserFeldwert f, string kennung, SatzsteckbriefTexte texte)
    {
        string? wert = f.Art switch
        {
            BrowserFeldArt.Schalter => f.Schalterwert ? texte.Ja : texte.Nein,
            BrowserFeldArt.Zahl => Zahlen.ZahlParsen(f.Wert, out double d) ? Zahlen.Anzeigetext(d) : f.Wert,
            BrowserFeldArt.Ganzzahl => Zahlen.GanzzahlParsen(f.Wert, out int i) ? Zahlen.Anzeigetext(i) : f.Wert,
            _ => f.Wert,
        };
        return new(Beschriftung(f.Bezeichnung), wert, string.IsNullOrWhiteSpace(wert) ? "" : f.Einheit ?? "", Kennung: kennung);
    }

    /// <summary>Eine Feldbeschriftung ohne Doppelpunkt am Ende („Leistung:" → „Leistung").</summary>
    public static string Beschriftung(string? text) => (text ?? "").Trim().TrimEnd(':').TrimEnd();
}
