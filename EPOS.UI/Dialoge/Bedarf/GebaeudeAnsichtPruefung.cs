using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

// =====================================================================================
//  MA-5 „Ansicht als Werkzeug" (Entwurf Geführter Modellaufbau 2.5): die Daten, mit denen die Gebäudeansicht
//  die Befunde einer Prüfgruppe am Körper zeigt — Hervorhebung, Vorschläge je Legendenzeile, Farbmodus „Nutzung".
//  Keine Datenbank: Die Kennungen kommen aus dem Blatt „Modell prüfen" (ModellpruefungBefundzeile) und aus den
//  Daten der Ansicht (BauteilIds, ZonenIds, Nutzungen), die die Hülle aus dem Kern füllt.
// =====================================================================================

/// <summary>Das Nutzungsprofil einer Zone im Farbmodus „Nutzung" (MA-5); <see cref="Profil"/> leer = ohne Profil („fehlt").</summary>
public sealed record GebaeudeAnsichtNutzung(string Profil, string Kategorie);

/// <summary>Eine Zeile der Legende „Nutzung": Profil, Kategorie, Farbstelle der Zonenpalette und Zahl der Zonen.</summary>
public sealed record GebaeudeAnsichtNutzungssumme(string Profil, string Kategorie, int Stelle, int Zonen);

/// <summary>
/// <b>Die Prüfgruppe, aus der die Ansicht geöffnet ist</b> (MA-5, Sprung aus dem Blatt „Modell prüfen"): Gruppe, ihr Name und
/// ihre Befunde mit den Kennungen der Zielobjekte. Daraus bildet die Ansicht die Hervorhebung (Bauteile über
/// <see cref="GebaeudeAnsichtDaten.BauteilIds"/>, Zonen ohne Bauteil über <see cref="GebaeudeAnsichtDaten.ZonenIds"/>) und je
/// Legendenzeile die Auswahl der Vorschläge für „Übernehmen" und „Verwerfen".
/// </summary>
public sealed record GebaeudeAnsichtPruefung(Pruefgruppe Gruppe, string Name, IReadOnlyList<ModellpruefungBefundzeile> Befunde)
{
    /// <summary>Die hervorgehobenen Bauteilkennungen der Ansicht: jedes Bauteil, das ein Befund der Gruppe nennt.</summary>
    public IReadOnlySet<string> Bauteilkennungen(GebaeudeAnsichtDaten daten)
    {
        var ids = Befunde.Where(b => b.IdBauteil.HasValue).Select(b => b.IdBauteil!.Value).ToHashSet();
        return daten.BauteilIds.Where(p => ids.Contains(p.Value)).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Die hervorgehobenen Zonenschlüssel der Ansicht: jede Zone, die ein Befund ohne Bauteil nennt.</summary>
    public IReadOnlySet<string> Zonenschluessel(GebaeudeAnsichtDaten daten)
    {
        var ids = Befunde.Where(b => !b.IdBauteil.HasValue && b.IdZone.HasValue).Select(b => b.IdZone!.Value).ToHashSet();
        return daten.ZonenIds.Where(p => ids.Contains(p.Value)).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Die hervorgehobenen Räume: Räume einer hervorgehobenen Zone und Räume mit einer Fläche eines hervorgehobenen Bauteils.</summary>
    public IReadOnlySet<string> Raeume(GebaeudeAnsichtDaten daten)
    {
        IReadOnlySet<string> bauteile = Bauteilkennungen(daten);
        IReadOnlySet<string> zonen = Zonenschluessel(daten);
        var raeume = new HashSet<string>(StringComparer.Ordinal);
        foreach (GebaeudeAnsichtRaum r in daten.Geschosse.SelectMany(g => g.Raeume))
            if (r.Zone is not null && zonen.Contains(r.Zone)) raeume.Add(r.Kennung);
        if (bauteile.Count > 0)
            foreach (GebaeudeAnsichtKoerperraum k in daten.Koerperraeume)
            {
                bool treffer = (k.Dreiecksbauteile?.Any(b => b is not null && bauteile.Contains(b)) ?? false)
                               || (k.Kantenbauteile?.Any(p => p.Any(b => b is not null && bauteile.Contains(b))) ?? false)
                               || k.Kantenmarken.Any(m => bauteile.Contains(m.Bauteil));
                if (treffer) raeume.Add(k.Kennung);
            }
        return raeume;
    }

    /// <summary>Ist überhaupt etwas der Ansicht hervorgehoben?</summary>
    public bool HebtHervor(GebaeudeAnsichtDaten daten) => Bauteilkennungen(daten).Count > 0 || Zonenschluessel(daten).Count > 0;

    /// <summary>
    /// Die Auswahl der Vorschläge, deren Bauteil die Bedingung <paramref name="bauteil"/> erfüllt (eine Legendenzeile nach Stufe
    /// oder Befund) — nur Bauteile, die die Ansicht zeigt.
    /// </summary>
    public ModellpruefungAuswahl AuswahlBauteile(GebaeudeAnsichtDaten daten, Func<string, bool> bauteil)
    {
        var ids = Befunde.Where(b => b.HatVorschlag && b.IdBauteil.HasValue).Select(b => b.IdBauteil!.Value).ToHashSet();
        var treffer = daten.BauteilIds.Where(p => ids.Contains(p.Value) && bauteil(p.Key)).Select(p => p.Value).Distinct().ToList();
        return new ModellpruefungAuswahl(Gruppe, treffer, Array.Empty<int>());
    }

    /// <summary>
    /// Die Auswahl der Vorschläge, deren Zone die Bedingung <paramref name="zone"/> erfüllt (eine Legendenzeile „Nutzung") — Befunde
    /// ohne Bauteil, nur Zonen, die die Ansicht zeigt.
    /// </summary>
    public ModellpruefungAuswahl AuswahlZonen(GebaeudeAnsichtDaten daten, Func<string, bool> zone)
    {
        var ids = Befunde.Where(b => b.HatVorschlag && !b.IdBauteil.HasValue && b.IdZone.HasValue).Select(b => b.IdZone!.Value).ToHashSet();
        var treffer = daten.ZonenIds.Where(p => ids.Contains(p.Value) && zone(p.Key)).Select(p => p.Value).Distinct().ToList();
        return new ModellpruefungAuswahl(Gruppe, Array.Empty<int>(), treffer);
    }

    /// <summary>Alle Vorschläge der Gruppe, die die Ansicht zeigt (Bauteile und Zonen).</summary>
    public ModellpruefungAuswahl AuswahlAlle(GebaeudeAnsichtDaten daten)
        => new ModellpruefungAuswahl(Gruppe, AuswahlBauteile(daten, _ => true).Bauteile, AuswahlZonen(daten, _ => true).Zonen);

    /// <summary>Die Zahl der Vorschläge einer Auswahl (je Befund, nicht je Bauteil).</summary>
    public int Vorschlaege(ModellpruefungAuswahl auswahl)
        => Befunde.Count(b => b.HatVorschlag && auswahl.Enthaelt(b.IdZone, b.IdBauteil));
}

/// <summary>
/// <b>Die Hervorhebung einer Prüfgruppe in der Ansicht</b> (MA-5): die Bauteilkennungen, Zonenschlüssel und Räume, die ein Befund
/// der Gruppe trifft. Der Grundriss zeichnet ihre Räume und Kanten mit Rand und dimmt die übrigen; die Szene trägt je Körper
/// <c>hervor</c>, das Modul dimmt die übrigen Körper und zieht die Kanten der getroffenen dunkel — die Farbtafeln bleiben.
/// </summary>
public sealed record GebaeudeAnsichtHervorhebung(IReadOnlySet<string> Bauteile, IReadOnlySet<string> Zonen, IReadOnlySet<string> Raeume)
{
    /// <summary>Die Hervorhebung der Prüfung in den Daten; <c>null</c>, wenn kein Befund der Gruppe in der Ansicht liegt.</summary>
    public static GebaeudeAnsichtHervorhebung? Von(GebaeudeAnsichtDaten daten, GebaeudeAnsichtPruefung? pruefung)
    {
        if (daten is null || pruefung is null || !pruefung.HebtHervor(daten)) return null;
        return new GebaeudeAnsichtHervorhebung(pruefung.Bauteilkennungen(daten), pruefung.Zonenschluessel(daten), pruefung.Raeume(daten));
    }

    /// <summary>Ein fester Schlüssel des Stands (für den Vergleich, ob das Modul neu zu versorgen ist).</summary>
    public string Schluessel
        => string.Join(",", Bauteile.OrderBy(x => x, StringComparer.Ordinal)) + "|" + string.Join(",", Zonen.OrderBy(x => x, StringComparer.Ordinal));
}
