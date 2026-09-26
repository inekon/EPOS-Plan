namespace EPOS.UI.Dienste;

/// <summary>Die drei Stellungen der Platzhalteranzeige (Konzept Berichtsvorlagen 9.1 C, 9.4).</summary>
public enum Vorlagenfeldstellung
{
    /// <summary>Vorgabe: keine Marke, kein DOM — die einzige Spur ist der ruhige Umschalter.</summary>
    Aus,

    /// <summary>Das Symbol <c>{ }</c> in der Randspur; Überfahren oder Fokus öffnet die Aufklappung.</summary>
    Marken,

    /// <summary>Der Vorlagenmodus: jede Marke als Chip mit ihrem Schlüssel, ein Klick kopiert.</summary>
    Schluessel,
}

/// <summary>
/// <b>Der Zustand der Platzhalteranzeige</b> (Konzept Berichtsvorlagen 9.4, 9.6) — Stellung,
/// das Element, das aufleuchten soll, und die Zahl der Marken, die gerade stehen.
///
/// <para><b>Ein Singleton im Dienstverzeichnis, kein <c>CascadingValue</c>.</b> Unter Windows
/// teilen alle <c>BlazorWebView</c>-Wurzeln EIN Verzeichnis (<c>BlazorDienste.cs</c>), auf iOS die
/// MAUI-Anwendung; ein kaskadierter Wert aus der <c>AppWurzel</c> erreichte die Wurzeln der
/// Windows-Dialoge nicht. Der Zustand gilt für die Sitzung, er wird nicht gespeichert.</para>
///
/// <para><b>Faden.</b> Geschrieben wird aus einem Blazor-Verteiler (Umschalter, Marke, Katalog);
/// unter Windows kann das der einer ANDEREN WebView sein. Jede Marke abonniert
/// <see cref="Geaendert"/> und zeichnet über <c>InvokeAsync</c> — dasselbe Muster wie
/// <see cref="SeitenZustand"/>.</para>
///
/// <para><b>Ohne Eintrag im Verzeichnis</b> (ein Prüfstand, der ihn nicht einträgt) nehmen die
/// Bausteine <see cref="Rueckfall"/> über <see cref="Aus(IServiceProvider?)"/> — eine fehlende
/// Einspritzung wirft so nie beim Aufbau.</para>
/// </summary>
public sealed class Vorlagenfeldansicht
{
    private readonly object _sperre = new();
    private readonly Dictionary<object, string> _marken = new();

    /// <summary>Der gemeinsame Rückfall ohne Dienstverzeichnis.</summary>
    public static Vorlagenfeldansicht Rueckfall { get; } = new();

    /// <summary>Der Zustand aus dem Dienstverzeichnis; ohne Eintrag <see cref="Rueckfall"/>.</summary>
    public static Vorlagenfeldansicht Aus(IServiceProvider? dienste)
    {
        try { return dienste?.GetService(typeof(Vorlagenfeldansicht)) as Vorlagenfeldansicht ?? Rueckfall; }
        catch (Exception) { return Rueckfall; }
    }

    /// <summary>Die Stellung; Vorgabe <see cref="Vorlagenfeldstellung.Aus"/>.</summary>
    public Vorlagenfeldstellung Stellung { get; private set; } = Vorlagenfeldstellung.Aus;

    /// <summary>Der Schlüssel, dessen Marke gerade aufleuchten soll; <c>null</c> = keiner.</summary>
    public string? Leuchtschluessel { get; private set; }

    /// <summary>Wie lange „kopiert" an der Marke steht (Konzept 9.7: 1,5 s).</summary>
    public TimeSpan Kopiertdauer { get; set; } = TimeSpan.FromSeconds(1.5);

    /// <summary>Wie lange eine Marke nach „In der App zeigen" aufleuchtet.</summary>
    public TimeSpan Leuchtdauer { get; set; } = TimeSpan.FromSeconds(2.5);

    /// <summary>Stellung oder Leuchtziel haben sich geändert.</summary>
    public event Action? Geaendert;

    /// <summary>Eine Marke kam hinzu oder ging — für die Zahl in der Zeile der Stellung „Schlüssel".</summary>
    public event Action? MarkenGeaendert;

    /// <summary>Setzt die Stellung; meldet nur bei einer Änderung.</summary>
    public void Setzen(Vorlagenfeldstellung stellung)
    {
        if (Stellung == stellung) return;
        Stellung = stellung;
        Melden(Geaendert);
    }

    /// <summary>
    /// „In der App zeigen": mindestens „Marken" und die Marke mit <paramref name="schluessel"/>
    /// leuchtet auf. Steht die Anzeige schon auf „Schlüssel", bleibt es dabei.
    /// </summary>
    public void Leuchten(string? schluessel)
    {
        if (Stellung == Vorlagenfeldstellung.Aus) Stellung = Vorlagenfeldstellung.Marken;
        Leuchtschluessel = string.IsNullOrWhiteSpace(schluessel) ? null : schluessel.Trim();
        Melden(Geaendert);
    }

    /// <summary>Beendet das Aufleuchten — nur, wenn es noch dasselbe Ziel ist.</summary>
    public void LeuchtenBeenden(string? schluessel)
    {
        if (Leuchtschluessel is null || !string.Equals(Leuchtschluessel, schluessel, StringComparison.Ordinal)) return;
        Leuchtschluessel = null;
        Melden(Geaendert);
    }

    /// <summary>Meldet eine gezeichnete Marke an (Schlüssel je Marke; ein neuer ersetzt den alten).</summary>
    public void MarkeAnmelden(object marke, string schluessel)
    {
        if (marke is null) return;
        bool neu;
        lock (_sperre)
        {
            neu = !_marken.TryGetValue(marke, out string? alt) || !string.Equals(alt, schluessel, StringComparison.Ordinal);
            _marken[marke] = schluessel ?? "";
        }
        if (neu) Melden(MarkenGeaendert);
    }

    /// <summary>Meldet eine Marke ab.</summary>
    public void MarkeAbmelden(object marke)
    {
        if (marke is null) return;
        bool weg;
        lock (_sperre) weg = _marken.Remove(marke);
        if (weg) Melden(MarkenGeaendert);
    }

    /// <summary>Die Zahl der verschiedenen Schlüssel der angemeldeten Marken.</summary>
    public int Markenzahl
    {
        get
        {
            lock (_sperre)
                return _marken.Values.Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).Count();
        }
    }

    private static void Melden(Action? h)
    {
        if (h is null) return;
        foreach (Action einzeln in h.GetInvocationList().Cast<Action>())
        {
            try { einzeln(); }
            catch (Exception) { /* ein Empfaenger bricht die anderen nicht ab */ }
        }
    }
}
