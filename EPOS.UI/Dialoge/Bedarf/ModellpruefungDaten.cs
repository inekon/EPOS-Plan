using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Ein Befund im Blatt „Modell prüfen“</b> (MA-1b, Entwurf Geführter Modellaufbau 2): Text des Kerns, Ampel, das benannte
/// Ziel (Zone, Bauteil) und der Vorschlag als Text; leer ohne Vorschlag. <see cref="IdZone"/> und <see cref="IdBauteil"/> sind die
/// Kennungen des Zielobjekts (<c>Tab_Zone.ID</c>, <c>Tab_Bauteil.ID</c>) — die Gebäudeansicht hebt damit hervor (MA-5).
/// </summary>
public sealed record ModellpruefungBefundzeile(string Text, Pruefampel Ampel, string Ziel, string Vorschlag,
                                               int? IdZone = null, int? IdBauteil = null)
{
    /// <summary>Trägt der Befund einen Vorschlag?</summary>
    public bool HatVorschlag => !string.IsNullOrEmpty(Vorschlag);
}

/// <summary>
/// <b>Eine Auswahl von Befunden einer Prüfgruppe</b> (MA-5): die Befunde, deren Bauteil in <see cref="Bauteile"/> oder deren Zone
/// (ohne Bauteil) in <see cref="Zonen"/> steht — eine Legendenzeile der Gebäudeansicht.
/// </summary>
public sealed record ModellpruefungAuswahl(Pruefgruppe Gruppe, IReadOnlyCollection<int> Bauteile, IReadOnlyCollection<int> Zonen)
{
    /// <summary>Gehört ein Befund mit diesen Kennungen zur Auswahl?</summary>
    public bool Enthaelt(int? idZone, int? idBauteil)
        => idBauteil is int b ? Bauteile.Contains(b) : idZone is int z && Zonen.Contains(z);
}

/// <summary>
/// <b>Eine Prüfgruppe des Blatts</b>: Ampel, Name, Zählung, Sprungziel in die Gebäudeansicht und die Befunde (erst auf Klick
/// gezeigt). <see cref="KannZuruecknehmen"/> sagt, ob die letzte Übernahme der Gruppe zurückgenommen werden kann.
/// </summary>
public sealed record ModellpruefungGruppenzeile(Pruefgruppe Gruppe, string Name, Pruefampel Ampel, int Anzahl, int Vorschlaege,
                                               Pruefsprungziel Sprungziel, bool KannZuruecknehmen,
                                               IReadOnlyList<ModellpruefungBefundzeile> Befunde);

/// <summary>
/// <b>Der Stand des Blatts</b>: Rechenstatus (drei Stufen) mit Text und die fünf Prüfgruppen.
/// </summary>
public sealed record ModellpruefungStand(Rechenstatus Status, string Statustext, IReadOnlyList<ModellpruefungGruppenzeile> Gruppen)
{
    /// <summary>Die Zahl aller Vorschläge über die Gruppen.</summary>
    public int Vorschlaege => Gruppen.Sum(g => g.Vorschlaege);
}

/// <summary>
/// <b>Der Ausgang einer Übernahme oder Rücknahme</b>: Erfolg, Meldung (benannter Grund oder Zusammenfassung) und der neu geprüfte
/// Stand; <c>null</c>, wenn das Gebäude nicht mehr zu lesen ist.
/// </summary>
public sealed record ModellpruefungAusgang(bool Ok, string Meldung, ModellpruefungStand? Stand);

/// <summary>
/// <b>Der Weg des Blatts „Modell prüfen“</b> (MA-1b): die Hülle (<c>ModellpruefungHuelle</c>) belegt die Delegaten aus dem
/// Kern-Controller <c>ModellpruefungCtrl</c>; die Oberfläche kennt keine Datenbank. Die Rücknahme gilt je Gruppe, solange
/// dieser Weg lebt (eine Instanz je Öffnen des Blatts).
/// </summary>
public sealed class ModellpruefungWeg
{
    /// <summary>Der Name des geprüften Gebäudes (Kopf des Blatts).</summary>
    public string Gebaeudename { get; init; } = "";

    /// <summary>Prüft (neu); <c>null</c>, wenn das Gebäude nicht zu lesen ist.</summary>
    public Func<ModellpruefungStand?> Pruefen { get; init; } = () => null;

    /// <summary>Übernimmt die Vorschläge einer Gruppe und prüft neu.</summary>
    public Func<Pruefgruppe, ModellpruefungAusgang> Uebernehmen { get; init; } = _ => new ModellpruefungAusgang(false, "", null);

    /// <summary>Übernimmt alle Vorschläge und prüft neu.</summary>
    public Func<ModellpruefungAusgang> AlleUebernehmen { get; init; } = () => new ModellpruefungAusgang(false, "", null);

    /// <summary>Nimmt die letzte Übernahme einer Gruppe zurück und prüft neu.</summary>
    public Func<Pruefgruppe, ModellpruefungAusgang> Zuruecknehmen { get; init; } = _ => new ModellpruefungAusgang(false, "", null);

    /// <summary>
    /// „Nordrichtung bestätigen“ (Gruppe 4): schreibt die gespeicherte Richtung als Eingabe; <c>null</c> = kein Weg (das Blatt
    /// nennt dann den Hinweis). Liefert die Meldung und ob es gelang.
    /// </summary>
    public Func<ModellpruefungAusgang>? NordrichtungBestaetigen { get; init; }

    /// <summary>MA-5: übernimmt die Vorschläge einer Auswahl (Legendenzeile der Ansicht) mit Rücknahme der Gruppe; <c>null</c> = kein Weg.</summary>
    public Func<ModellpruefungAuswahl, ModellpruefungAusgang>? UebernehmenAuswahl { get; init; }

    /// <summary>MA-5: verwirft die Vorschläge einer Auswahl für die Sitzung (schreibt nichts); <c>null</c> = kein Weg.</summary>
    public Func<ModellpruefungAuswahl, ModellpruefungAusgang>? VerwerfenAuswahl { get; init; }
}
