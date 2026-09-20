using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// Die Kurve „Kapitalwert über Stückzahl" — die Ergebnissicht der Methode
/// <see cref="FlottenSuchmethode.Stueckzahl"/> bei EINER variierten Einheit
/// (Auftrag #247, Anwenderentscheid SD‑E‑10 / SD‑Q17).
/// </summary>
/// <remarks>
/// <para><b>Ganze Zahlen, deshalb Balken.</b> Zwischen „zwei Geräten" und „drei Geräten"
/// gibt es nichts; eine Linie behauptete einen Zwischenwert, den die Suche nie gerechnet
/// hat. Aus demselben Grund gibt es unter dieser Methode kein Feinraster.</para>
/// <para><b>Stehen zwei Kandidaten auf derselben Stückzahl</b> — dieselbe Bestückung mit
/// zwei Betriebszielen —, <b>besetzt der bessere sie</b>: erst Zulässigkeit, dann
/// Kapitalwert. Dieselbe Rangfolge wie in der Rasterkarte (Spezifikation 9.4).</para>
/// </remarks>
/// <param name="Stueckzahlen">Die geprüften Stückzahlen, aufsteigend.</param>
/// <param name="Werte">Der Kapitalwert [€] je Stückzahl; <c>NaN</c> = kein brauchbarer Kandidat.</param>
/// <param name="Unzulaessig">Je Stelle: Der dort stehende Kandidat verletzt eine harte Grenze.</param>
/// <param name="BesteStelle">Stelle des Optimums in den Listen, oder <c>-1</c>.</param>
public sealed record FlottenStueckzahlkurve(IReadOnlyList<int> Stueckzahlen,
                                            IReadOnlyList<double> Werte,
                                            IReadOnlyList<bool> Unzulaessig,
                                            int BesteStelle)
{
    /// <summary>Es gibt keine Kurve — kein Kandidat mit genau einer variierten Einheit.</summary>
    public bool IstLeer => Stueckzahlen.Count == 0;

    /// <summary>Die leere Kurve; sie ersetzt jedes <c>null</c> bei den Aufrufern.</summary>
    public static FlottenStueckzahlkurve Leer { get; } =
        new(Array.Empty<int>(), Array.Empty<double>(), Array.Empty<bool>(), -1);
}

/// <summary>
/// Die Rasterkarte n₁ × n₂ — die Ergebnissicht der Methode
/// <see cref="FlottenSuchmethode.Stueckzahl"/> bei ZWEI variierten Einheiten
/// (Auftrag #247, SD‑Q17).
/// </summary>
/// <remarks>
/// Es ist DIESELBE Karte wie die der Größensuche, nur mit ganzzahligen Achsen: gezeichnet
/// wird sie von <c>ChartRenderer.Optimierungsraster</c>, und ein Loch (keine Stückzahl-
/// Kombination gerechnet) ist wie dort <c>double.NaN</c> und wird hellgrau statt in der
/// Minimumfarbe gezeichnet (#226).
/// </remarks>
/// <param name="Spaltenzahlen">Die Stückzahlen der ZWEITEN variierten Einheit, aufsteigend.</param>
/// <param name="Zeilenzahlen">Die Stückzahlen der ERSTEN variierten Einheit, aufsteigend.</param>
/// <param name="Werte">Kapitalwert [€] je Stelle <c>[iZeile][iSpalte]</c>; <c>NaN</c> = Loch.</param>
/// <param name="Unzulaessig">Je Stelle: Der dort stehende Kandidat verletzt eine harte Grenze.</param>
/// <param name="BesteZeile">Zeile des Optimums, oder <c>-1</c>.</param>
/// <param name="BesteSpalte">Spalte des Optimums, oder <c>-1</c>.</param>
public sealed record FlottenStueckzahlraster(IReadOnlyList<int> Spaltenzahlen,
                                             IReadOnlyList<int> Zeilenzahlen,
                                             double[][] Werte,
                                             bool[][] Unzulaessig,
                                             int BesteZeile, int BesteSpalte)
{
    /// <summary>Es gibt keine Karte — weniger als zwei variierte Einheiten.</summary>
    public bool IstLeer => Spaltenzahlen.Count == 0 || Zeilenzahlen.Count == 0;

    /// <summary>Die leere Karte.</summary>
    public static FlottenStueckzahlraster Leer { get; } =
        new(Array.Empty<int>(), Array.Empty<int>(), Array.Empty<double[]>(),
            Array.Empty<bool[]>(), -1, -1);
}

/// <summary>
/// <b>Die STÜCKZAHL-SICHT der Speicherflotte</b> (Auftrag #247, Konzept
/// „Stromspeicher-Dialoge" 8.3, Anwenderentscheid SD‑E‑10 / SD‑Q17).
///
/// <para><b>Warum sie hier steht und nicht in der Oberfläche</b> — derselbe Grund wie bei
/// der Größen-Sicht: Aus der Kandidatenliste eine Achse zu machen ist eine FACHaufgabe.
/// Welche Stückzahlen eine Achse bilden, wann zwei Kandidaten auf derselben Stelle stehen
/// und welcher davon sie besetzt, entscheidet nicht das Markup.</para>
///
/// <para><b>Es wird nichts nachgerechnet.</b> Die Stückzahl je Achse steht seit #247 in
/// <see cref="FlottenKandidatZusammenfassung.Stueckzahlen"/>; die Rastersuche hat sie
/// mitgeschrieben. Aus den Einheitenkennungen abgelesen wäre sie eine Textvereinbarung.</para>
/// </summary>
public static partial class SpeicherFlottenAnzeigeCtrl
{
    // =====================================================================
    //  Wie viele Einheiten hat der Lauf variiert?
    // =====================================================================

    /// <summary>
    /// Die Zahl der VARIIERTEN Einheiten eines Laufs — also die Zahl der aktiven
    /// Suchachsen, wie sie die Kandidaten führen.
    /// </summary>
    /// <remarks>
    /// Sie entscheidet die Ergebnissicht unter „Stückzahl suchen" (SD‑Q17): 1 → die Kurve
    /// „Kapitalwert über Stückzahl", 2 → die Rasterkarte n₁ × n₂, darüber hinaus nur die
    /// Kandidatentabelle. 0 heißt „nichts variiert" (reine Bewertung oder ein Lauf vor
    /// #247, dessen Kandidaten die Liste nicht führen).
    /// </remarks>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = 0.</param>
    public static int VariierteEinheiten(FlottenAuslegungErgebnis ergebnis)
    {
        if (ergebnis?.Kandidaten is not { Count: > 0 } kandidaten) return 0;
        int zahl = 0;
        foreach (FlottenKandidatZusammenfassung k in kandidaten)
        {
            int n = k.Stueckzahlen?.Count ?? 0;
            if (n > zahl) zahl = n;
        }
        return zahl;
    }

    // =====================================================================
    //  Die Kurve — EINE variierte Einheit
    // =====================================================================

    /// <summary>
    /// Die Kurve „Kapitalwert über Stückzahl": je geprüfter Stückzahl der beste dort
    /// stehende Kandidat.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = leere Kurve.</param>
    public static FlottenStueckzahlkurve Stueckzahlkurve(FlottenAuslegungErgebnis ergebnis)
    {
        if (VariierteEinheiten(ergebnis) != 1) return FlottenStueckzahlkurve.Leer;

        var besetzt = new SortedDictionary<int, FlottenKandidatZusammenfassung>();
        foreach (FlottenKandidatZusammenfassung k in ergebnis.Kandidaten)
        {
            if (k.Stueckzahlen is not { Count: 1 }) continue;
            int n = k.Stueckzahlen[0];
            if (besetzt.TryGetValue(n, out FlottenKandidatZusammenfassung alt) && !IstBesser(k, alt)) continue;
            besetzt[n] = k;
        }
        if (besetzt.Count == 0) return FlottenStueckzahlkurve.Leer;

        var zahlen = new List<int>(besetzt.Count);
        var werte = new List<double>(besetzt.Count);
        var gesperrt = new List<bool>(besetzt.Count);
        int besteStelle = -1;
        foreach (KeyValuePair<int, FlottenKandidatZusammenfassung> paar in besetzt)
        {
            if (ReferenceEquals(paar.Value, ergebnis.BesterKandidat)) besteStelle = zahlen.Count;
            zahlen.Add(paar.Key);
            werte.Add(double.IsFinite(paar.Value.KapitalwertEuro) ? paar.Value.KapitalwertEuro : double.NaN);
            gesperrt.Add(!paar.Value.Zulaessig);
        }
        return new FlottenStueckzahlkurve(zahlen, werte, gesperrt, besteStelle);
    }

    /// <summary>
    /// Der Kandidat, der auf einer Stückzahl der Kurve steht — der Rückweg von einer
    /// Säule zum Kandidaten, den „Kandidat übernehmen" braucht.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="stueckzahl">Die gewählte Stückzahl.</param>
    /// <returns>Der Kandidat oder <c>null</c>.</returns>
    public static FlottenKandidatZusammenfassung KandidatZuStueckzahl(
        FlottenAuslegungErgebnis ergebnis, int stueckzahl)
    {
        if (ergebnis?.Kandidaten is not { Count: > 0 } kandidaten) return null;
        FlottenKandidatZusammenfassung beste = null;
        foreach (FlottenKandidatZusammenfassung k in kandidaten)
        {
            if (k.Stueckzahlen is not { Count: 1 } || k.Stueckzahlen[0] != stueckzahl) continue;
            if (beste == null || IstBesser(k, beste)) beste = k;
        }
        return beste;
    }

    // =====================================================================
    //  Die Karte — ZWEI variierte Einheiten
    // =====================================================================

    /// <summary>
    /// Die Rasterkarte n₁ × n₂: Kapitalwert über den Stückzahlen der zwei variierten
    /// Einheiten.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = leere Karte.</param>
    public static FlottenStueckzahlraster Stueckzahlraster(FlottenAuslegungErgebnis ergebnis)
    {
        if (VariierteEinheiten(ergebnis) != 2) return FlottenStueckzahlraster.Leer;

        var passende = ergebnis.Kandidaten.Where(k => k.Stueckzahlen is { Count: 2 }).ToList();
        if (passende.Count == 0) return FlottenStueckzahlraster.Leer;

        List<int> zeilen = Ganzachse(passende.Select(k => k.Stueckzahlen[0]));
        List<int> spalten = Ganzachse(passende.Select(k => k.Stueckzahlen[1]));

        var werte = new double[zeilen.Count][];
        var gesperrt = new bool[zeilen.Count][];
        var belegt = new FlottenKandidatZusammenfassung[zeilen.Count][];
        for (int i = 0; i < zeilen.Count; i++)
        {
            werte[i] = new double[spalten.Count];
            gesperrt[i] = new bool[spalten.Count];
            belegt[i] = new FlottenKandidatZusammenfassung[spalten.Count];
            for (int s = 0; s < spalten.Count; s++) werte[i][s] = double.NaN;
        }

        foreach (FlottenKandidatZusammenfassung k in passende)
        {
            int zeile = zeilen.IndexOf(k.Stueckzahlen[0]);
            int spalte = spalten.IndexOf(k.Stueckzahlen[1]);
            if (zeile < 0 || spalte < 0) continue;
            if (belegt[zeile][spalte] is { } alt && !IstBesser(k, alt)) continue;

            belegt[zeile][spalte] = k;
            werte[zeile][spalte] = double.IsFinite(k.KapitalwertEuro) ? k.KapitalwertEuro : double.NaN;
            gesperrt[zeile][spalte] = !k.Zulaessig;
        }

        int besteZeile = -1, besteSpalte = -1;
        if (ergebnis.BesterKandidat is { } bester)
            for (int i = 0; i < zeilen.Count && besteZeile < 0; i++)
                for (int s = 0; s < spalten.Count; s++)
                    if (ReferenceEquals(belegt[i][s], bester)) { besteZeile = i; besteSpalte = s; break; }

        return new FlottenStueckzahlraster(spalten, zeilen, werte, gesperrt, besteZeile, besteSpalte);
    }

    // =====================================================================
    //  Die zwei Bilder
    // =====================================================================

    /// <summary>
    /// Die KURVE „Kapitalwert über Stückzahl" als PNG; ohne Kurve <c>null</c>.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="einheitenname">Name der variierten Einheit für die Überschrift; leer = ohne.</param>
    public static byte[] Stueckzahlbild(FlottenAuslegungErgebnis ergebnis, string einheitenname = null)
        => Gemalt(StueckzahlModell(ergebnis, einheitenname));

    /// <summary>
    /// <b>DIESELBE KURVE ALS ZEICHENMODELL</b> — der Zwilling von
    /// <see cref="Stueckzahlbild"/> (Etappe DG-E3).
    ///
    /// <para>Sie hat keine Zeichenfläche (DG-E3-7): Zwischen zwei Stückzahlen liegt
    /// nichts, worauf ein Zoom zeigen könnte. Jede Säule nennt dafür ihren Wert, und die
    /// Oberfläche zeigt ihn am Zeiger (DG-E3-10).</para>
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="einheitenname">Name der variierten Einheit für die Überschrift; leer = ohne.</param>
    public static Zeichnung.Zeichenmodell StueckzahlModell(FlottenAuslegungErgebnis ergebnis,
                                                           string einheitenname = null)
    {
        FlottenStueckzahlkurve kurve = Stueckzahlkurve(ergebnis);
        if (kurve.IstLeer) return null;

        return ChartRenderer.StueckzahlkurveModell(
            Stueckzahltitel(einheitenname),
            MyResource.Resource.FLOTTE_STUECK_ACHSE,
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            kurve.Stueckzahlen, kurve.Werte, kurve.BesteStelle, kurve.Unzulaessig);
    }

    /// <summary>
    /// Die RASTERKARTE n₁ × n₂ als PNG — dieselbe Zeichnung wie die Größenkarte, nur mit
    /// ganzzahligen Achsen; ohne Karte <c>null</c>.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="ersteEinheit">Name der ersten variierten Einheit; leer = „Einheit 1".</param>
    /// <param name="zweiteEinheit">Name der zweiten variierten Einheit; leer = „Einheit 2".</param>
    public static byte[] Stueckzahlrasterbild(FlottenAuslegungErgebnis ergebnis,
                                              string ersteEinheit = null,
                                              string zweiteEinheit = null)
        => Gemalt(StueckzahlrasterModell(ergebnis, ersteEinheit, zweiteEinheit));

    /// <summary>
    /// <b>DIESELBE RASTERKARTE ALS ZEICHENMODELL</b> — der Zwilling von
    /// <see cref="Stueckzahlrasterbild"/> (Etappe DG-E3). Jede Zelle trägt ihren
    /// <c>data-wert</c>.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="ersteEinheit">Name der ersten variierten Einheit; leer = „Einheit 1".</param>
    /// <param name="zweiteEinheit">Name der zweiten variierten Einheit; leer = „Einheit 2".</param>
    public static Zeichnung.Zeichenmodell StueckzahlrasterModell(
        FlottenAuslegungErgebnis ergebnis, string ersteEinheit = null,
        string zweiteEinheit = null)
    {
        FlottenStueckzahlraster raster = Stueckzahlraster(ergebnis);
        if (raster.IstLeer) return null;

        return ChartRenderer.OptimierungsrasterModell(
            MyResource.Resource.FLOTTE_STUECK_RASTER_TITEL,
            Stueckzahlachse(zweiteEinheit, 2),
            Stueckzahlachse(ersteEinheit, 1),
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            raster.Spaltenzahlen.Select(x => (double)x).ToArray(),
            raster.Zeilenzahlen.Select(x => (double)x).ToArray(),
            raster.Werte, raster.BesteZeile, raster.BesteSpalte,
            raster.Unzulaessig, MyResource.Resource.FLOTTE_GROESSEN_ENDLICHES_RASTER);
    }

    // =====================================================================
    //  Texte
    // =====================================================================

    /// <summary>Die Überschrift der Stückzahlkurve — mit dem Einheitennamen, sobald einer da ist.</summary>
    public static string Stueckzahltitel(string einheitenname)
        => string.IsNullOrWhiteSpace(einheitenname)
            ? MyResource.Resource.FLOTTE_STUECK_TITEL
            : MyResource.Resource.FLOTTE_STUECK_TITEL + " — " + einheitenname;

    /// <summary>Die Achsenbeschriftung einer Stückzahlachse: „Stückzahl &lt;Einheit&gt;".</summary>
    /// <param name="einheitenname">Der Name der Einheit; leer = die laufende Nummer.</param>
    /// <param name="nummer">Die laufende Nummer der variierten Einheit (1-basiert).</param>
    public static string Stueckzahlachse(string einheitenname, int nummer)
        => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.FLOTTE_STUECK_ACHSE_EINHEIT,
                         string.IsNullOrWhiteSpace(einheitenname)
                             ? string.Format(CultureInfo.CurrentCulture,
                                             MyResource.Resource.FLOTTE_GROESSEN_EINHEIT, nummer)
                             : einheitenname);

    // =====================================================================
    //  Innenleben
    // =====================================================================

    /// <summary>
    /// Besetzt <paramref name="neu"/> die Stelle statt <paramref name="alt"/>? Erst
    /// Zulässigkeit, dann Kapitalwert — die Rangfolge der Spezifikation 9.4, dieselbe wie
    /// in der Rasterkarte der Größensuche.
    /// </summary>
    private static bool IstBesser(FlottenKandidatZusammenfassung neu,
                                  FlottenKandidatZusammenfassung alt)
    {
        if (neu.Zulaessig != alt.Zulaessig) return neu.Zulaessig;
        return neu.KapitalwertEuro > alt.KapitalwertEuro;
    }

    /// <summary>Eine ganzzahlige Achse aus den vorkommenden Stückzahlen: aufsteigend, ohne Dubletten.</summary>
    private static List<int> Ganzachse(IEnumerable<int> werte)
        => werte.Distinct().OrderBy(x => x).ToList();
}
