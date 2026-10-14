using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Der Grundzustand des Abschnitts „Ergebnisse — Energie“, wie ihn der Anwender im Dialog „Anzeige anpassen…“
/// festlegt (Anwenderentscheid 13.10.2026): je Gruppe ein Zustand, die abgewählten Kennzahlen und der Schalter
/// „Zeilen ohne Wert ausblenden“. Gilt je Benutzer für alle Projekte; die Hülle liest und schreibt ihn über die
/// Einstellungsnaht des Kerns.
/// </summary>
public sealed class ErgebnisAnzeigeEinstellung
{
    /// <summary>
    /// Der vom Anwender gesetzte Zustand je Gruppenschlüssel. Eine Gruppe ohne Eintrag nimmt ihre Vorgabe aus dem Kern
    /// (<c>Energievergleich.GRUPPEN_DATEN</c>).
    /// </summary>
    public Dictionary<string, ErgebnisGruppenZustand> Gruppen { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Die abgewählten Kennzahlenschlüssel (Katalogschlüssel, „endenergie:&lt;CarrierId&gt;“).</summary>
    public HashSet<string> KennzahlenAus { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Zeilen, deren Wert in allen gezeigten Versionen 0 oder leer ist, ausblenden? Vorgabe ja.</summary>
    public bool OhneWertAus { get; init; } = true;

    /// <summary>Die Vorgabe: kein Gruppenzustand, alle Kennzahlen an, „ohne Wert“ ausgeblendet.</summary>
    public static ErgebnisAnzeigeEinstellung Standard() => new();

    /// <summary>Gleicht die Einstellung der Vorgabe? Dann schreibt die Hülle nichts und löscht ihre Schlüssel.</summary>
    public bool IstStandard => Gruppen.Count == 0 && KennzahlenAus.Count == 0 && OhneWertAus;

    /// <summary>Eine unabhängige Kopie (Arbeitsstand des Dialogs).</summary>
    public ErgebnisAnzeigeEinstellung Kopie() => new()
    {
        Gruppen = new Dictionary<string, ErgebnisGruppenZustand>(Gruppen, StringComparer.Ordinal),
        KennzahlenAus = new HashSet<string>(KennzahlenAus, StringComparer.Ordinal),
        OhneWertAus = OhneWertAus
    };
}

/// <summary>Eine Kennzahl, die der Dialog „Anzeige anpassen…“ zur Wahl stellt.</summary>
public sealed class ErgebnisAnpassKennzahl
{
    /// <summary>Der stabile Schlüssel (Katalogschlüssel, „endenergie:&lt;CarrierId&gt;“).</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Die Beschriftung samt Einheit, wie in der Tabelle.</summary>
    public string Bezeichnung { get; init; } = "";
}

/// <summary>Eine Gruppe des Dialogs „Anzeige anpassen…“: Vorgabe des Kerns und alle bekannten Kennzahlen.</summary>
public sealed class ErgebnisAnpassGruppe
{
    /// <summary>Der Gruppenschlüssel des Kerns.</summary>
    public string Schluessel { get; init; } = "";

    /// <summary>Der Gruppentitel.</summary>
    public string Titel { get; init; } = "";

    /// <summary>Der Vorgabezustand des Kerns (offen oder zugeklappt).</summary>
    public ErgebnisGruppenZustand Vorgabe { get; init; } = ErgebnisGruppenZustand.Offen;

    /// <summary>Alle bekannten Kennzahlen der Gruppe: die des Katalogs und die Trägerzeilen der aktuellen Daten.</summary>
    public IReadOnlyList<ErgebnisAnpassKennzahl> Kennzahlen { get; init; } = Array.Empty<ErgebnisAnpassKennzahl>();
}

/// <summary>Was die Anzeigeeinstellung aus den Gruppen macht: die gezeigten Gruppen und was „ohne Wert“ ausblendet.</summary>
public sealed record ErgebnisAnzeigeAuswertung(IReadOnlyList<ErgebnisVergleichGruppe> Gruppen, int OhneWertAusgeblendet);

/// <summary>
/// Die reinen Funktionen der Anzeigeeinstellung des Abschnitts „Ergebnisse — Energie“: die Auflösung des
/// Grundzustands (Benutzerzustand vor Vorgabe), das Filtern der Kennzahlen und der Zeilen ohne Wert und die
/// Textform der drei Einstellungsschlüssel. Ohne Datenbank, ohne Einstellungsnaht — die Hülle ruft sie.
///
/// <para><b>Die Rangfolge.</b> Vorgabe des Kerns &lt; Benutzerzustand (Dialog, gespeichert) für den Zustand einer
/// Gruppe; ob eine zugeklappte Gruppe gerade aufgeklappt ist, entscheidet allein die Sitzungsablage der Hülle
/// (<c>KlappgruppeOffenLesen</c>) — gespeichert wird nur der Grundzustand.</para>
/// </summary>
public static class UebersichtErgebnisAnzeige
{
    /// <summary>Der Einstellungsschlüssel der Gruppenzustände („bedarf=offen;kaelte=ausgeblendet“).</summary>
    public const string SCHLUESSEL_GRUPPEN = "Uebersicht.Ergebnisse.Gruppen";

    /// <summary>Der Einstellungsschlüssel der abgewählten Kennzahlen („energie.strommax;endenergie:1“).</summary>
    public const string SCHLUESSEL_KENNZAHLEN_AUS = "Uebersicht.Ergebnisse.KennzahlenAus";

    /// <summary>Der Einstellungsschlüssel des Schalters „Zeilen ohne Wert ausblenden“ (1 = an, 0 = aus).</summary>
    public const string SCHLUESSEL_OHNE_WERT_AUS = "Uebersicht.Ergebnisse.OhneWertAus";

    private static readonly Dictionary<string, ErgebnisGruppenZustand> s_woerter = new(StringComparer.OrdinalIgnoreCase)
    {
        ["offen"] = ErgebnisGruppenZustand.Offen,
        ["zugeklappt"] = ErgebnisGruppenZustand.Zugeklappt,
        ["ausgeblendet"] = ErgebnisGruppenZustand.Ausgeblendet
    };

    /// <summary>Der Grundzustand einer Gruppe: der Benutzerzustand, sonst die Vorgabe.</summary>
    public static ErgebnisGruppenZustand Grundzustand(string gruppe, ErgebnisGruppenZustand vorgabe,
                                                      ErgebnisAnzeigeEinstellung? benutzer)
        => benutzer is not null && benutzer.Gruppen.TryGetValue(gruppe, out ErgebnisGruppenZustand z) ? z : vorgabe;

    /// <summary>Ist die Zeile ohne Wert — in allen Versionen 0 oder leer?</summary>
    public static bool OhneWert(IReadOnlyList<double?> werte)
    {
        foreach (double? w in werte)
            if (w is double d && d != 0 && !double.IsNaN(d)) return false;
        return true;
    }

    /// <summary>
    /// Wendet die Einstellung auf die Gruppen der Hülle an (deren <see cref="ErgebnisVergleichGruppe.Zustand"/> trägt die
    /// Vorgabe des Kerns): Zustand je Gruppe nach <see cref="Grundzustand"/>, abgewählte Kennzahlen entfallen, unter
    /// <see cref="ErgebnisAnzeigeEinstellung.OhneWertAus"/> auch die Zeilen ohne Wert (gezählt nur in nicht
    /// ausgeblendeten Gruppen). Eine Gruppe ohne verbleibende Zeile entfällt. Die Eingabe bleibt unverändert;
    /// unbekannte Schlüssel der Einstellung wirken nicht.
    /// </summary>
    public static ErgebnisAnzeigeAuswertung Anwenden(IReadOnlyList<ErgebnisVergleichGruppe> gruppen,
                                                     ErgebnisAnzeigeEinstellung? benutzer)
    {
        benutzer ??= ErgebnisAnzeigeEinstellung.Standard();
        var erg = new List<ErgebnisVergleichGruppe>();
        int ohneWert = 0;
        foreach (ErgebnisVergleichGruppe g in gruppen ?? Array.Empty<ErgebnisVergleichGruppe>())
        {
            ErgebnisGruppenZustand zustand = Grundzustand(g.Schluessel, g.Zustand, benutzer);
            var zeilen = new List<ErgebnisVergleichZeile>();
            foreach (ErgebnisVergleichZeile z in g.Zeilen)
            {
                if (benutzer.KennzahlenAus.Contains(z.Schluessel)) continue;
                if (benutzer.OhneWertAus && z.OhneWert)
                {
                    if (zustand != ErgebnisGruppenZustand.Ausgeblendet) ohneWert++;
                    continue;
                }
                zeilen.Add(z);
            }
            if (zeilen.Count == 0) continue;
            erg.Add(new ErgebnisVergleichGruppe { Schluessel = g.Schluessel, Titel = g.Titel, Zeilen = zeilen, Zustand = zustand });
        }
        return new ErgebnisAnzeigeAuswertung(erg, ohneWert);
    }

    /// <summary>
    /// Liest die Einstellung aus der Textform der drei Schlüssel. Unbekannte Gruppen und unbekannte Zustandswörter
    /// werden still übergangen; ein gespeicherter Zustand, der der Vorgabe gleicht, bleibt außen vor.
    /// </summary>
    /// <param name="gruppen">Der Wert von <see cref="SCHLUESSEL_GRUPPEN"/>; leer = keiner.</param>
    /// <param name="kennzahlenAus">Der Wert von <see cref="SCHLUESSEL_KENNZAHLEN_AUS"/>; leer = keine.</param>
    /// <param name="ohneWertAus">Der Wert von <see cref="SCHLUESSEL_OHNE_WERT_AUS"/> (0 = aus, sonst an).</param>
    /// <param name="vorgaben">Die bekannten Gruppen mit ihrer Vorgabe (Kern).</param>
    public static ErgebnisAnzeigeEinstellung AusText(string? gruppen, string? kennzahlenAus, int ohneWertAus,
                                                     IReadOnlyDictionary<string, ErgebnisGruppenZustand> vorgaben)
    {
        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = ohneWertAus != 0 };
        foreach (string teil in (gruppen ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int gleich = teil.IndexOf('=');
            if (gleich <= 0) continue;
            string g = teil.Substring(0, gleich).Trim();
            if (!vorgaben.TryGetValue(g, out ErgebnisGruppenZustand vorgabe)) continue;
            if (!s_woerter.TryGetValue(teil.Substring(gleich + 1).Trim(), out ErgebnisGruppenZustand z)) continue;
            if (z != vorgabe) e.Gruppen[g] = z;
        }
        foreach (string k in (kennzahlenAus ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            e.KennzahlenAus.Add(k);
        return e;
    }

    /// <summary>Die Textform der Gruppenzustände, nach Schlüssel geordnet; leer ohne Eintrag.</summary>
    public static string GruppenText(ErgebnisAnzeigeEinstellung e)
        => string.Join(";", e.Gruppen.OrderBy(p => p.Key, StringComparer.Ordinal)
                                     .Select(p => p.Key + "=" + Wort(p.Value)));

    /// <summary>Die Textform der abgewählten Kennzahlen, geordnet; leer ohne Eintrag.</summary>
    public static string KennzahlenText(ErgebnisAnzeigeEinstellung e)
        => string.Join(";", e.KennzahlenAus.Where(k => k.Length > 0 && k.IndexOf(';') < 0)
                                           .OrderBy(k => k, StringComparer.Ordinal));

    /// <summary>Das Zustandswort der Textform.</summary>
    public static string Wort(ErgebnisGruppenZustand z) => z switch
    {
        ErgebnisGruppenZustand.Zugeklappt => "zugeklappt",
        ErgebnisGruppenZustand.Ausgeblendet => "ausgeblendet",
        _ => "offen"
    };

    /// <summary>
    /// Die Einstellung, die der Dialog bei OK meldet: nur die Gruppen, deren Zustand von der Vorgabe abweicht (eine
    /// spätere Änderung der Vorgabe erreicht so jeden, der sie nicht übersteuert hat), die abgewählten Kennzahlen samt
    /// der heute nicht gezeigten (eine Trägerzeile eines anderen Projekts bleibt abgewählt) und der Schalter.
    /// </summary>
    public static ErgebnisAnzeigeEinstellung Ergebnis(IReadOnlyList<ErgebnisAnpassGruppe> gruppen,
                                                      IReadOnlyDictionary<string, ErgebnisGruppenZustand> zustaende,
                                                      IEnumerable<string> kennzahlenAus, bool ohneWertAus)
    {
        var e = new ErgebnisAnzeigeEinstellung { OhneWertAus = ohneWertAus };
        foreach (ErgebnisAnpassGruppe g in gruppen)
            if (zustaende.TryGetValue(g.Schluessel, out ErgebnisGruppenZustand z) && z != g.Vorgabe) e.Gruppen[g.Schluessel] = z;
        foreach (string k in kennzahlenAus) e.KennzahlenAus.Add(k);
        return e;
    }
}
