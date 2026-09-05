namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Ein Gebäudetyp samt seinen Tagesverteilungen (iU9-W8.4) — das plattformfreie Abbild
/// von <c>Tab_DBTagV_STAMM</c> (Kopf) und <c>Tab_DBTagVDaten_STAMM</c> (24 Zeilen je
/// Tageskurve).
///
/// <para><b>Das einzige Kopf-Detail-Modell der Welle.</b> Ein Typ führt FÜNF oder ACHT
/// Kurven zu je 24 Stunden; welche es sind, entscheidet die Kurvenzahl und nicht die
/// Listenposition (<c>GetTagVName</c>:108 — die Typliste ist alphabetisch sortiert).</para>
/// </summary>
public sealed class GebaeudetypDaten
{
    /// <summary>Stunden je Tageskurve.</summary>
    public const int STUNDEN = 24;

    /// <summary>Die Id des Kopfsatzes (<c>Tab_DBTagV_STAMM.ID</c>); 0 = keiner geladen.</summary>
    public int Id { get; set; }

    /// <summary>Der Name des Typs (Spalte <c>Bezeichner</c>).</summary>
    public string Name { get; set; } = "";

    /// <summary>Freitext des Typs (Spalte <c>Beschreibung</c>) — im Dialog nur lesbar.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>
    /// Darf der Typ geändert werden? <c>Veraenderbar &amp;&amp; !ReadOnly</c> — der
    /// Auslieferungsbestand des Softwareherstellers ist es nicht.
    /// </summary>
    public bool Aenderbar { get; set; }

    /// <summary>Die Tagesverteilungen [Kurve, Stunde].</summary>
    public double[,] Verteilung { get; set; } = new double[0, STUNDEN];

    /// <summary>Die Namen der Kurven in Anzeigereihenfolge (fünf oder acht).</summary>
    public IReadOnlyList<string> Kurvennamen { get; set; } = Array.Empty<string>();

    /// <summary>Die Zahl der Kurven.</summary>
    public int Kurven => Verteilung.GetLength(0);

    /// <summary>Eine Kopie der 24 Werte einer Kurve.</summary>
    public double[] Kurve(int n)
    {
        var w = new double[STUNDEN];
        if (n < 0 || n >= Kurven) return w;
        for (int s = 0; s < STUNDEN; s++) w[s] = Verteilung[n, s];
        return w;
    }
}
