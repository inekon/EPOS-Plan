using System.Globalization;
using EPOS.UI.Standards;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Die FELDTAFEL des Aufklappers „Alle Daten anzeigen" in den sechs Erzeugermasken des
/// Projekts (Welle #458, Stufe 2) — Heizkessel, BHKW, Pufferspeicher, Stromspeicher,
/// Solarkollektoren und Photovoltaik.
///
/// <para><b>Dieselbe Technik wie die Verwaltungen</b> (<see cref="KatalogBrowserKiSicht"/>,
/// Welle #456): Der Aufklapper führt den gewählten KATALOGsatz als Liste von
/// <see cref="BrowserFeldwert"/>, und der Dialogkatalog erzeugt seine Feldkarte aus
/// demselben Profil, aus dem die Hülle die Liste füllt (<c>KatalogBrowserProfil</c> bzw.
/// <c>ModulKatalogProfil</c>). Eine zweite Feldliste von Hand gibt es nicht. Die Sicht
/// der Maske beantwortet die Tafelfelder über diese Klasse; der Schlüssel im Katalog
/// trägt die Vorsilbe <see cref="KiDialoge.KATALOGFELD_VORSILBE"/>, damit er neben den
/// benannten Feldern der Anlage (<c>Vorlauf</c> gegen <c>VORLAUF</c>) eindeutig bleibt.</para>
///
/// <para><b>Der Schreibweg ist der des Aufklappers:</b> Gesetzt wird in die lebende
/// Feldliste, der Dialog merkt sich die Änderung wie nach einer Eingabe von Hand, und
/// <c>dialog_speichern</c> geht über seinen Knopf „Speichern" (<c>KatalogfelderSpeichern</c>).
/// Ein AUSLIEFERUNGSSATZ, ein zugeklappter Aufklapper und ein Katalog ohne Speicherweg
/// lehnen das Setzen benannt ab (<see cref="Grund"/>).</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jeder Zugriff ruft die Delegaten des Dialogs.</para>
/// </summary>
public sealed class AlleDatenTafel
{
    /// <summary>Die Felder des gewählten Katalogsatzes; <c>null</c> = Aufklapper zu oder kein Satz.</summary>
    public Func<IReadOnlyList<BrowserFeldwert>?>? Felder { get; init; }

    /// <summary>Warum gerade nichts gesetzt werden darf; <c>null</c>/leer = frei.</summary>
    public Func<string?>? Sperrgrund { get; init; }

    /// <summary>Nach jedem Setzen — der Dialog merkt sich die Änderung wie nach einer Eingabe.</summary>
    public Action? Gesetzt { get; init; }

    /// <summary>Der Wert unter diesem Tafelschlüssel; <c>null</c> = kein solches Feld.</summary>
    public object? Lesen(string schluessel)
    {
        BrowserFeldwert? feld = Suche(schluessel);
        return feld is null ? null : BrowserFeldwertWandler.Lesen(feld);
    }

    /// <summary>
    /// Setzt ein Feld des Aufklappers; ein gesperrter Aufklapper lehnt mit Grund ab, ein
    /// nicht editierbares Feld nimmt nichts an.
    /// </summary>
    public void Setzen(string schluessel, object? wert)
    {
        string? grund = Sperrgrund?.Invoke();
        if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);

        BrowserFeldwert? feld = Suche(schluessel);
        if (feld is null || !feld.Editierbar) return;

        BrowserFeldwertWandler.Setzen(feld, wert);
        Gesetzt?.Invoke();
    }

    /// <summary>
    /// Der Sperrgrund des Aufklappers — dieselbe Reihenfolge in allen sechs Masken: erst
    /// zu oder ohne Satz, dann ohne Speicherweg, dann der Auslieferungssatz.
    /// </summary>
    public static string? Grund(IReadOnlyList<BrowserFeldwert>? felder, bool mitSpeicherweg, bool auslieferung)
    {
        if (felder is null) return WindowsFormsApplication1.MyResource.Resource.KI_DLG_ALLE_DATEN_ZU;
        if (!mitSpeicherweg) return WindowsFormsApplication1.MyResource.Resource.KI_DLG_ALLE_DATEN_NUR_ANZEIGE;
        if (auslieferung) return WindowsFormsApplication1.MyResource.Resource.ADM_SPEICHERN_GESPERRT;
        return null;
    }

    private BrowserFeldwert? Suche(string schluessel)
    {
        if (string.IsNullOrEmpty(schluessel)) return null;

        string vorsilbe = KiDialoge.KATALOGFELD_VORSILBE;
        string profil = schluessel.StartsWith(vorsilbe, StringComparison.Ordinal)
            ? schluessel.Substring(vorsilbe.Length)
            : schluessel;

        IReadOnlyList<BrowserFeldwert>? felder = Felder?.Invoke();
        if (felder is null) return null;

        foreach (BrowserFeldwert f in felder)
            if (string.Equals(f.Schluessel, profil, StringComparison.OrdinalIgnoreCase)) return f;

        return null;
    }
}

/// <summary>
/// Die EINE Umrechnung zwischen einem <see cref="BrowserFeldwert"/> (Wert als Text) und
/// dem Wert, den der Assistent liest und setzt — mit denselben Regeln wie der Baustein
/// <c>Katalogfelder</c> (<see cref="Zahlen.ZahlParsen"/>, Ausgabe in der laufenden
/// Kultur). Die Verwaltungen (<see cref="KatalogBrowserKiSicht"/>) und die sechs
/// Projektmasken (<see cref="AlleDatenTafel"/>) nehmen sie beide.
/// </summary>
public static class BrowserFeldwertWandler
{
    /// <summary>
    /// Der Wert im Typ des Katalogfeldes. Ein Zahlenfeld, dessen Text sich nicht als Zahl
    /// lesen lässt (eine abgeleitete Anzeige), kommt als Text heraus — so, wie es
    /// dasteht, statt leer.
    /// </summary>
    public static object? Lesen(BrowserFeldwert feld)
    {
        switch (feld.Art)
        {
            case BrowserFeldArt.Schalter:
                return feld.Schalterwert;
            case BrowserFeldArt.Zahl:
                if (Zahlen.ZahlParsen(feld.Wert, out double d)) return d;
                return feld.Wert.Length == 0 ? null : feld.Wert;
            case BrowserFeldArt.Ganzzahl:
                if (Zahlen.GanzzahlParsen(feld.Wert, out int i)) return i;
                return feld.Wert.Length == 0 ? null : feld.Wert;
            default:
                return feld.Wert;
        }
    }

    /// <summary>Schreibt einen Wert in das Feld — als Text in der laufenden Kultur.</summary>
    public static void Setzen(BrowserFeldwert feld, object? wert)
    {
        switch (feld.Art)
        {
            case BrowserFeldArt.Schalter:
                feld.Schalterwert = wert is bool b && b;
                break;
            case BrowserFeldArt.Zahl:
            case BrowserFeldArt.Ganzzahl:
                feld.Wert = wert is null ? "" : Convert.ToString(wert, CultureInfo.CurrentCulture) ?? "";
                break;
            default:
                feld.Wert = Convert.ToString(wert, CultureInfo.CurrentCulture) ?? "";
                break;
        }
    }
}
