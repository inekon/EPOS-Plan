using System.Globalization;

namespace EPOS.UI.Dienste;

/// <summary>
/// <b>Ein Platzhalter, wie ihn die Oberfläche zeigt</b> (Konzept Berichtsvorlagen 9.4, 9.6) — das
/// neutrale DTO hinter <c>Vorlagenfeldknopf</c> und der Zeile „Katalog…": fertige Anzeigetexte in
/// der Sprache der Oberfläche, dazu die zwei sprachneutralen Kennungen, nach denen sich das
/// Kopieren richtet. Keine Fachklasse des Kerns; die Hülle
/// (<c>EPOS.UI.Daten/Bericht/VorlagenfeldanzeigeHuelle.cs</c>) formt den Katalogeintrag hierher um.
/// </summary>
/// <param name="Schluessel">Der Schlüssel ohne Klammern („projekt.kunde").</param>
/// <param name="Art">Die Art als Anzeigetext („Text", „Tabelle").</param>
/// <param name="ArtKennung">Die Art sprachneutral, wie der Kern sie nennt: <c>Text</c>, <c>Zahl</c>,
/// <c>Datum</c>, <c>Tabelle</c>, <c>Bild</c>, <c>Liste</c>, <c>Kapitel</c>, <c>Schalter</c>, <c>Blatt</c>.</param>
/// <param name="Kontext">Der Kontext als Anzeigetext („Stamm", „je Stand").</param>
/// <param name="KontextKennung">Der Kontext sprachneutral: <c>Bericht</c>, <c>Installation</c>,
/// <c>Stamm</c>, <c>Stand</c>, <c>Gebaeude</c>, <c>Gruppe</c>.</param>
/// <param name="Beschreibung">Die Beschreibung aus dem Katalog.</param>
/// <param name="Beispiel">Eine Beispielausgabe („1.234"); leer = keine.</param>
/// <param name="Einheit">Die Einheit einer Zahl („MWh/a"); leer = keine.</param>
/// <param name="Leerwert">Was ohne Wert im Bericht steht („—"); leer = bleibt leer.</param>
/// <param name="Excel">Der Excel-Name („EPOS.projekt.kunde") oder der Satz, wie der Platzhalter in
/// Excel erscheint („in Excel nur als Listenzeile"); leer = nicht in Excel.</param>
public sealed record Vorlagenfeldanzeige(
    string Schluessel,
    string Art,
    string ArtKennung,
    string Kontext,
    string KontextKennung,
    string Beschreibung,
    string Beispiel = "",
    string Einheit = "",
    string Leerwert = "",
    string Excel = "")
{
    /// <summary>
    /// Die Kurzbeschreibung für den <c>title</c> der Marke: der erste Satz der Beschreibung,
    /// höchstens rund 120 Zeichen.
    /// </summary>
    public string Kurzbeschreibung
    {
        get
        {
            string b = (Beschreibung ?? "").Trim();
            int punkt = b.IndexOf(". ", StringComparison.Ordinal);
            if (punkt > 0) b = b.Substring(0, punkt + 1);
            return b.Length <= 120 ? b : b.Substring(0, 119).TrimEnd() + "…";
        }
    }

    /// <summary>Die Beispielausgabe samt Einheit („1.234 MWh/a"); leer = keine.</summary>
    public string BeispielMitEinheit
        => string.IsNullOrWhiteSpace(Beispiel) ? ""
           : string.IsNullOrWhiteSpace(Einheit) ? Beispiel.Trim()
           : string.Format(CultureInfo.InvariantCulture, "{0} {1}", Beispiel.Trim(), Einheit.Trim());
}

/// <summary>
/// <b>Der Halter der Platzhalter für die Oberfläche</b> (Konzept 9.6) — statisch wie
/// <see cref="Navigationsziel"/>, ohne Fachklasse. Die Schale hängt beim Start eine
/// <see cref="Quelle"/> ein (die Hülle in <c>EPOS.UI.Daten</c>); gelesen wird beim ersten Zugriff,
/// je Oberflächensprache einmal. Ein Prüfstand setzt die Einträge unmittelbar
/// (<see cref="Setzen"/>) und stellt in <c>Dispose</c> zurück (<see cref="Zuruecksetzen"/>).
/// </summary>
/// <remarks>
/// Ohne Quelle und ohne gesetzte Einträge ist der Halter leer — dann zeichnet keine Marke
/// (die Marke rendert nichts, wenn ihr Schlüssel fehlt).
/// </remarks>
public static class Vorlagenfeldhalter
{
    private static readonly object _sperre = new();
    private static Func<IReadOnlyList<Vorlagenfeldanzeige>>? _quelle;
    private static IReadOnlyList<Vorlagenfeldanzeige>? _gesetzt;
    private static IReadOnlyList<Vorlagenfeldanzeige>? _geladen;
    private static Dictionary<string, Vorlagenfeldanzeige>? _index;
    private static string? _kultur;

    /// <summary>
    /// Die Quelle der Einträge — gesetzt von der Schale beim Start; <c>null</c> = keine. Ein neuer
    /// Wert verwirft das Geladene.
    /// </summary>
    public static Func<IReadOnlyList<Vorlagenfeldanzeige>>? Quelle
    {
        get { lock (_sperre) return _quelle; }
        set { lock (_sperre) { _quelle = value; _geladen = null; _index = null; _kultur = null; } }
    }

    /// <summary>Setzt die Einträge unmittelbar (Prüfstand); sie gehen der <see cref="Quelle"/> vor.</summary>
    public static void Setzen(IReadOnlyList<Vorlagenfeldanzeige>? eintraege)
    {
        lock (_sperre)
        {
            _gesetzt = eintraege;
            _geladen = null;
            _index = null;
            _kultur = null;
        }
    }

    /// <summary>Nimmt gesetzte Einträge und die Quelle zurück.</summary>
    public static void Zuruecksetzen()
    {
        lock (_sperre)
        {
            _gesetzt = null;
            _quelle = null;
            _geladen = null;
            _index = null;
            _kultur = null;
        }
    }

    /// <summary>Alle Einträge in Katalogreihenfolge; leer = keine.</summary>
    public static IReadOnlyList<Vorlagenfeldanzeige> Alle
    {
        get
        {
            lock (_sperre)
            {
                Laden();
                return _geladen ?? Array.Empty<Vorlagenfeldanzeige>();
            }
        }
    }

    /// <summary>Der Eintrag zu einem Schlüssel; <c>null</c> = unbekannt oder leerer Schlüssel.</summary>
    public static Vorlagenfeldanzeige? Finde(string? schluessel)
    {
        if (string.IsNullOrWhiteSpace(schluessel)) return null;
        lock (_sperre)
        {
            Laden();
            return _index is not null && _index.TryGetValue(schluessel.Trim(), out Vorlagenfeldanzeige? a) ? a : null;
        }
    }

    /// <summary>Lädt je Oberflächensprache einmal — unter der Sperre gerufen.</summary>
    private static void Laden()
    {
        string kultur = CultureInfo.CurrentUICulture.Name;
        if (_geladen is not null && string.Equals(_kultur, kultur, StringComparison.Ordinal)) return;

        IReadOnlyList<Vorlagenfeldanzeige> liste;
        if (_gesetzt is not null) liste = _gesetzt;
        else if (_quelle is not null)
        {
            try { liste = _quelle() ?? Array.Empty<Vorlagenfeldanzeige>(); }
            catch (Exception) { liste = Array.Empty<Vorlagenfeldanzeige>(); }
        }
        else liste = Array.Empty<Vorlagenfeldanzeige>();

        var index = new Dictionary<string, Vorlagenfeldanzeige>(StringComparer.Ordinal);
        foreach (Vorlagenfeldanzeige a in liste)
            if (a is not null && !string.IsNullOrWhiteSpace(a.Schluessel)) index.TryAdd(a.Schluessel, a);

        _geladen = liste;
        _index = index;
        _kultur = kultur;
    }
}
