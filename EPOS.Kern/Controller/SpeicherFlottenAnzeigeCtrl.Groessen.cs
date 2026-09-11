using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// Die Achsen und Werte EINER Rasterkarte der Auslegung (Auftrag #193, Konzept
/// „Stromspeicher-Dialoge" 2.5).
/// </summary>
/// <remarks>
/// <para><b>Zeilen sind Kapazitäten, Spalten C-Raten</b> — dieselbe Anordnung, die
/// <see cref="ChartRenderer.Optimierungsraster"/> seit W11b‑B‑5 für den Einzelspeicher
/// zeichnet. Die C-Rate IST die Leistungsachse in normierter Form (<c>P = E · C</c>);
/// sie steht dort, weil ein Raster über Kapazität UND absoluter Leistung bei jedem
/// Kapazitätsschritt eine andere Leistungsreihe hätte und damit keine Karte wäre.</para>
/// <para><b>Ein Loch im Raster ist <c>double.NaN</c></b> — eine Stelle, an der kein
/// Kandidat gerechnet wurde. Sie geht nicht in die Farbskala ein (der Renderer
/// überspringt nicht endliche Werte) und ist etwas anderes als ein Kandidat mit dem
/// Kapitalwert 0.</para>
/// </remarks>
/// <param name="CRaten">Die C-Raten der Spalten [1/h], aufsteigend.</param>
/// <param name="KapazitaetenKwh">Die Kapazitäten der Zeilen [kWh], aufsteigend.</param>
/// <param name="Werte">Kapitalwert [€] je Stelle <c>[iKapazität][iCRate]</c>; <c>NaN</c> = Loch.</param>
/// <param name="Unzulaessig">Je Stelle: Der dort stehende Kandidat verletzt eine harte Grenze.</param>
/// <param name="BesteZeile">Zeile des Optimums, oder <c>-1</c>.</param>
/// <param name="BesteSpalte">Spalte des Optimums, oder <c>-1</c>.</param>
public sealed record FlottenRasterdaten(IReadOnlyList<double> CRaten,
                                        IReadOnlyList<double> KapazitaetenKwh,
                                        double[][] Werte,
                                        bool[][] Unzulaessig,
                                        int BesteZeile, int BesteSpalte)
{
    /// <summary>Es gibt keine Karte — kein Kandidat, oder nur die Nullvariante.</summary>
    public bool IstLeer => CRaten.Count == 0 || KapazitaetenKwh.Count == 0;

    /// <summary>Die leere Karte; sie ersetzt jedes <c>null</c> bei den Aufrufern.</summary>
    public static FlottenRasterdaten Leer { get; } =
        new(Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double[]>(),
            Array.Empty<bool[]>(), -1, -1);
}

/// <summary>
/// EIN Schnitt durch das Raster (Auftrag #193): der Kapitalwert über einer Achse, die
/// zweite Größe festgehalten.
/// </summary>
/// <param name="Achse">Die Stützstellen der Achse — Kapazität [kWh] bzw. Entladeleistung [kW].</param>
/// <param name="Werte">Der Kapitalwert [€] dazu; <c>NaN</c> an einer Stelle ohne Kandidat.</param>
/// <param name="OptimumAchse">Der Achsenwert des besten Punktes DIESES Schnitts; <c>NaN</c> = keine Marke.</param>
/// <param name="OptimumWert">Sein Kapitalwert; <c>NaN</c> = keine Marke.</param>
public sealed record FlottenSchnittdaten(IReadOnlyList<double> Achse,
                                         IReadOnlyList<double> Werte,
                                         double OptimumAchse, double OptimumWert)
{
    /// <summary>Weniger als zwei Stützstellen ergeben keine Kurve.</summary>
    public bool IstLeer => Achse.Count < 2;

    /// <summary>Der leere Schnitt.</summary>
    public static FlottenSchnittdaten Leer { get; } =
        new(Array.Empty<double>(), Array.Empty<double>(), double.NaN, double.NaN);
}

/// <summary>
/// <b>Die GRÖSSEN-SICHT der Speicherflotte</b> (Auftrag #193, Paket P4 des Konzepts
/// „Stromspeicher-Dialoge", Abschnitt 2.5).
///
/// <para><b>Warum die Rechnung hier steht und nicht in der Oberfläche.</b> Aus der
/// Kandidatenliste eine Karte zu machen ist eine FACHaufgabe: Welche Werte eine Achse
/// bilden, wann zwei Kandidaten auf derselben Stelle stehen, welcher davon die Stelle
/// besetzt und was ein Loch ist — das entscheidet nicht das Markup. Die Oberfläche
/// bekommt fertige Achsen, eine fertige Matrix und fertige Bilder; auf iOS gilt
/// dieselbe Aufteilung.</para>
///
/// <para><b>Es wird nichts nachgerechnet.</b> Alle Zahlen stehen in der
/// <see cref="FlottenKandidatZusammenfassung"/>, die der <c>FlottenOptimierer</c>
/// während der Rastersuche gefüllt hat. Eine zweite Simulation gäbe es hier nicht zu
/// rechtfertigen — und sie stünde in der Oberfläche.</para>
/// </summary>
public static partial class SpeicherFlottenAnzeigeCtrl
{
    /// <summary>Die Einheitenwahl „Summe der Flotte" (Konzept 2.5: bei Anzahl &gt; 1 die Summe).</summary>
    public const int FLOTTE_GESAMT = -1;

    /// <summary>
    /// Relative Schranke, unter der zwei Achsenwerte ALS DERSELBE gelten.
    /// </summary>
    /// <remarks>
    /// Eine C-Rate entsteht als <c>Entladeleistung / Kapazität</c>, und die Leistung
    /// entstand ihrerseits als <c>Kapazität · C-Rate</c>: Derselbe nominelle Wert kommt
    /// dabei um ein bis zwei Bit verschieden heraus. Ohne diese Schranke zerfiele eine
    /// Spalte in so viele Spalten, wie es Kapazitätszeilen gibt — die Karte wäre eine
    /// Diagonale. Die Schranke ist RELATIV, weil Kapazitäten in kWh und C-Raten in 1/h
    /// drei Größenordnungen auseinanderliegen.
    /// </remarks>
    private const double ACHSENSCHRANKE = 1e-9;

    // =====================================================================
    //  Die Rasterkarte
    // =====================================================================

    /// <summary>
    /// Die Rasterkarte einer Auslegung: Kapitalwert über Kapazität (Zeilen) und C-Rate
    /// (Spalten), dazu die Schraffurmatrix und die Stelle des Optimums.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = leere Karte.</param>
    /// <param name="einheit">
    /// <see cref="FLOTTE_GESAMT"/> (-1) = die Summen der Flotte; sonst die Stelle der
    /// Einheit, deren Größen die Achsen bilden (Konzept 2.5: „je Einheit wählbar").
    /// </param>
    /// <remarks>
    /// <para><b>Stehen zwei Kandidaten auf derselben Stelle</b> — dieselbe Hardware mit
    /// zwei Betriebszielen —, <b>besetzt der bessere sie</b>: erst Zulässigkeit, dann
    /// Kapitalwert. Das ist dieselbe Rangfolge, nach der der <c>FlottenOptimierer</c>
    /// seinen Besten wählt (Spezifikation 9.4); eine Karte, die den schlechteren zeigte,
    /// widerspräche der Marke des Optimums im selben Bild.</para>
    /// <para><b>Die Achsen kommen aus den WERTEN, nicht aus dem Rasterindex</b>
    /// (<see cref="FlottenKandidatZusammenfassung.Rasterzeile"/>). Den Index gibt es nur
    /// bei genau EINER aktiven Suchachse; er sagt außerdem nichts über die
    /// Beschriftung — im Modus <c>KapazitaetUndLeistung</c> ist die zweite Achse eine
    /// Leistungsreihe, und dieselbe Spalte trägt dann in jeder Zeile eine ANDERE C-Rate.
    /// Aus den Werten gebaut stimmt die Karte in allen drei Achsenmodi und auch für einen
    /// Stand, der von anderswo kommt.</para>
    /// </remarks>
    public static FlottenRasterdaten Rasterdaten(FlottenAuslegungErgebnis ergebnis,
                                                 int einheit = FLOTTE_GESAMT)
    {
        List<Rasterpunkt> punkte = Rasterpunkte(ergebnis, einheit);
        if (punkte.Count == 0) return FlottenRasterdaten.Leer;

        IReadOnlyList<double> kapazitaeten = Achse(punkte.Select(p => p.KapazitaetKWh));
        IReadOnlyList<double> cRaten = Achse(punkte.Select(p => p.CRate));

        int zeilen = kapazitaeten.Count, spalten = cRaten.Count;
        var werte = new double[zeilen][];
        var unzulaessig = new bool[zeilen][];
        var belegt = new Rasterpunkt[zeilen][];
        for (int i = 0; i < zeilen; i++)
        {
            werte[i] = new double[spalten];
            unzulaessig[i] = new bool[spalten];
            belegt[i] = new Rasterpunkt[spalten];
            for (int s = 0; s < spalten; s++) werte[i][s] = double.NaN;
        }

        foreach (Rasterpunkt p in punkte)
        {
            int zeile = Stelle(kapazitaeten, p.KapazitaetKWh);
            int spalte = Stelle(cRaten, p.CRate);
            if (zeile < 0 || spalte < 0) continue;
            if (belegt[zeile][spalte] is { } alt && !IstBesser(p, alt)) continue;

            belegt[zeile][spalte] = p;
            werte[zeile][spalte] = double.IsFinite(p.Kandidat.KapitalwertEuro)
                ? p.Kandidat.KapitalwertEuro : double.NaN;
            unzulaessig[zeile][spalte] = !p.Kandidat.Zulaessig;
        }

        int besteZeile = -1, besteSpalte = -1;
        if (ergebnis.BesterKandidat is { } bester)
        {
            for (int i = 0; i < zeilen && besteZeile < 0; i++)
                for (int s = 0; s < spalten; s++)
                    if (ReferenceEquals(belegt[i][s]?.Kandidat, bester))
                    {
                        besteZeile = i;
                        besteSpalte = s;
                        break;
                    }
        }

        return new FlottenRasterdaten(cRaten, kapazitaeten, werte, unzulaessig,
                                      besteZeile, besteSpalte);
    }

    // =====================================================================
    //  Die zwei Schnitte
    // =====================================================================

    /// <summary>
    /// Der Schnitt bei fester C-Rate: Kapitalwert über der KAPAZITÄT (Konzept 2.5).
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="cRate">Die festgehaltene C-Rate [1/h]; sie muss auf der Achse liegen.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static FlottenSchnittdaten Schnittdaten(FlottenAuslegungErgebnis ergebnis,
                                                   double cRate, int einheit = FLOTTE_GESAMT)
    {
        FlottenRasterdaten raster = Rasterdaten(ergebnis, einheit);
        int spalte = Stelle(raster.CRaten, cRate);
        if (spalte < 0) return FlottenSchnittdaten.Leer;

        var werte = new double[raster.KapazitaetenKwh.Count];
        for (int i = 0; i < werte.Length; i++) werte[i] = raster.Werte[i][spalte];
        return Schnitt(raster.KapazitaetenKwh, werte);
    }

    /// <summary>
    /// Derselbe Schnitt über der LEISTUNG: Kapitalwert über der Entladeleistung bei
    /// fester Kapazität (Konzept 2.5, „daneben dieselbe Kurve über der Leistung").
    /// </summary>
    /// <remarks>
    /// Die Achse entsteht aus <c>P = E · C</c>: Bei fester Kapazität ist die Leistung der
    /// C-Rate proportional, die Kurve also dieselbe Zeile des Rasters über einer anders
    /// beschrifteten Achse. Gezeichnet wird sie mit
    /// <see cref="ChartRenderer.Schnittkurve"/> — derselben Funktion, nur mit anderer
    /// Achsenbeschriftung.
    /// </remarks>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="kapazitaetKwh">Die festgehaltene Kapazität [kWh]; sie muss auf der Achse liegen.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static FlottenSchnittdaten SchnittdatenLeistung(FlottenAuslegungErgebnis ergebnis,
                                                           double kapazitaetKwh,
                                                           int einheit = FLOTTE_GESAMT)
    {
        FlottenRasterdaten raster = Rasterdaten(ergebnis, einheit);
        int zeile = Stelle(raster.KapazitaetenKwh, kapazitaetKwh);
        if (zeile < 0) return FlottenSchnittdaten.Leer;

        double kapazitaet = raster.KapazitaetenKwh[zeile];
        var achse = raster.CRaten.Select(c => c * kapazitaet).ToArray();
        return Schnitt(achse, raster.Werte[zeile]);
    }

    // =====================================================================
    //  Die drei Bilder
    // =====================================================================

    /// <summary>
    /// Die RASTERKARTE als PNG — unzulässige Stellen schraffiert, das Optimum markiert,
    /// darunter der SP‑O‑4-Hinweis. Ohne Karte <c>null</c>.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    /// <param name="einheitenname">Name der gewählten Einheit für die Überschrift; leer = Flotte gesamt.</param>
    public static byte[] Rasterbild(FlottenAuslegungErgebnis ergebnis,
                                    int einheit = FLOTTE_GESAMT, string einheitenname = null)
    {
        FlottenRasterdaten raster = Rasterdaten(ergebnis, einheit);
        if (raster.IstLeer) return null;

        return ChartRenderer.Optimierungsraster(
            Bildtitel(MyResource.Resource.FLOTTE_GROESSEN_CHART_RASTER, einheit, einheitenname),
            MyResource.Resource.FLOTTE_GROESSEN_ACHSE_CRATE,
            MyResource.Resource.FLOTTE_GROESSEN_ACHSE_KAPAZITAET,
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            raster.CRaten, raster.KapazitaetenKwh, raster.Werte,
            raster.BesteZeile, raster.BesteSpalte,
            raster.Unzulaessig, MyResource.Resource.FLOTTE_GROESSEN_ENDLICHES_RASTER);
    }

    /// <summary>Die SCHNITTKURVE über der Kapazität als PNG; ohne Kurve <c>null</c>.</summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="cRate">Die festgehaltene C-Rate [1/h].</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static byte[] Schnittbild(FlottenAuslegungErgebnis ergebnis, double cRate,
                                     int einheit = FLOTTE_GESAMT)
    {
        FlottenSchnittdaten schnitt = Schnittdaten(ergebnis, cRate, einheit);
        if (schnitt.IstLeer) return null;

        return ChartRenderer.Schnittkurve(
            string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_KAPAZITAET, Zahl(cRate, "0.###")),
            MyResource.Resource.FLOTTE_GROESSEN_ACHSE_KAPAZITAET,
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            schnitt.Achse, schnitt.Werte, schnitt.OptimumAchse, schnitt.OptimumWert);
    }

    /// <summary>Dieselbe Kurve über der ENTLADELEISTUNG als PNG; ohne Kurve <c>null</c>.</summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="kapazitaetKwh">Die festgehaltene Kapazität [kWh].</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static byte[] SchnittbildLeistung(FlottenAuslegungErgebnis ergebnis,
                                             double kapazitaetKwh,
                                             int einheit = FLOTTE_GESAMT)
    {
        FlottenSchnittdaten schnitt = SchnittdatenLeistung(ergebnis, kapazitaetKwh, einheit);
        if (schnitt.IstLeer) return null;

        return ChartRenderer.Schnittkurve(
            string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_LEISTUNG, Zahl(kapazitaetKwh, "0.#")),
            MyResource.Resource.FLOTTE_GROESSEN_ACHSE_LEISTUNG,
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            schnitt.Achse, schnitt.Werte, schnitt.OptimumAchse, schnitt.OptimumWert);
    }

    // =====================================================================
    //  Die Kandidatentabelle — Spalten und Zeilen für den Katalogfilter
    // =====================================================================

    /// <summary>Spaltenschlüssel der Kandidatentabelle — sprachneutral wie im Katalogfilter.</summary>
    public const string SP_KANDIDAT = "KANDIDAT";

    /// <summary>Spaltenschlüssel: das gerechnete Betriebsziel.</summary>
    public const string SP_ZIEL = "ZIEL";

    /// <summary>Spaltenschlüssel: Kapazität [kWh].</summary>
    public const string SP_KAPAZITAET = "KAPAZITAET";

    /// <summary>Spaltenschlüssel: Ladeleistung [kW].</summary>
    public const string SP_LADEN = "LADEN";

    /// <summary>Spaltenschlüssel: Entladeleistung [kW].</summary>
    public const string SP_ENTLADEN = "ENTLADEN";

    /// <summary>Spaltenschlüssel: C-Rate [1/h].</summary>
    public const string SP_CRATE = "CRATE";

    /// <summary>Spaltenschlüssel: Kapitalwert [€].</summary>
    public const string SP_KAPITALWERT = "KAPITALWERT";

    /// <summary>Spaltenschlüssel: Betriebsersparnis [€/a].</summary>
    public const string SP_ERSPARNIS = "ERSPARNIS";

    /// <summary>Spaltenschlüssel: Vollzyklen [1/a].</summary>
    public const string SP_VOLLZYKLEN = "VOLLZYKLEN";

    /// <summary>Spaltenschlüssel: Bezugsspitze [kW].</summary>
    public const string SP_SPITZE = "SPITZE";

    /// <summary>Spaltenschlüssel: zulässig (Kennzeichen).</summary>
    public const string SP_ZULAESSIG = "ZULAESSIG";

    /// <summary>Spaltenschlüssel: Hinweis / Grund der Unzulässigkeit.</summary>
    public const string SP_GRUND = "GRUND";

    /// <summary>
    /// Das Filterprofil der Kandidatentabelle (Konzept 2.5: „sortierbar, mit
    /// Spaltenfilter (Katalogfilter-Muster)").
    /// </summary>
    /// <remarks>
    /// Es entsteht über <see cref="Katalogfilterprofil.AusSpalten"/> — demselben Weg, den
    /// die zwei Importmasken seit S3.4 gehen: Die Zeilen sind KANDIDATEN und keine
    /// Katalogsätze, ihre Spalten hängen an der Rastersuche. Damit gelten für sie
    /// dieselbe Verknüpfung (Spaltenfilter UND, Suche ODER über die Spalten), derselbe
    /// Zahlenausdruck (<c>&gt;10</c>, <c>10..60</c>) und dieselbe Sortierung wie in den
    /// fünfzehn Katalogwirten — geprüft ist das dort, nicht hier noch einmal.
    /// </remarks>
    public static Katalogfilterprofil Kandidatenprofil() => Katalogfilterprofil.AusSpalten(
        "FLOTTE_KANDIDATEN",
        new[]
        {
            new Katalogspalte(SP_KANDIDAT, MyResource.Resource.FLOTTE_GROESSEN_SP_KANDIDAT),
            new Katalogspalte(SP_ZIEL, MyResource.Resource.FLOTTE_GROESSEN_SP_BETRIEBSZIEL),
            new Katalogspalte(SP_KAPAZITAET, MyResource.Resource.FLOTTE_GROESSEN_SP_KAPAZITAET,
                              "kWh", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_LADEN, MyResource.Resource.FLOTTE_GROESSEN_SP_LADEN,
                              "kW", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_ENTLADEN, MyResource.Resource.FLOTTE_GROESSEN_SP_ENTLADEN,
                              "kW", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_CRATE, MyResource.Resource.FLOTTE_GROESSEN_SP_CRATE,
                              "1/h", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_KAPITALWERT, MyResource.Resource.FLOTTE_GROESSEN_SP_KAPITALWERT,
                              "€", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_ERSPARNIS, MyResource.Resource.FLOTTE_GROESSEN_SP_ERSPARNIS,
                              "€/a", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_VOLLZYKLEN, MyResource.Resource.FLOTTE_GROESSEN_SP_VOLLZYKLEN,
                              "1/a", Katalogspaltenart.Zahl),
            new Katalogspalte(SP_SPITZE, MyResource.Resource.FLOTTE_GROESSEN_SP_SPITZE,
                              "kW", Katalogspaltenart.Zahl),
            // Kennzeichen: nur der Sortierpfeil (Konzept_Katalogfilter 5.6.2) - ein Feld
            // "enthaelt ja" fuer zwei Werte ist ein Bedienelement ohne Gewinn.
            new Katalogspalte(SP_ZULAESSIG, MyResource.Resource.FLOTTE_GROESSEN_SP_ZULAESSIG,
                              "", Katalogspaltenart.JaNein),
            new Katalogspalte(SP_GRUND, MyResource.Resource.FLOTTE_GROESSEN_SP_GRUND)
        });

    /// <summary>
    /// Die Kandidaten als Filterzeilen, in der Reihenfolge der Rastersuche. Der
    /// <see cref="Katalogfilterzeile.Schluessel"/> ist die
    /// <see cref="FlottenKandidatZusammenfassung.KandidatId"/> — sie trägt Betriebsziel
    /// und Hardware und ist damit eindeutig; über sie findet die Oberfläche zu einer
    /// gefilterten Zeile den Kandidaten zurück.
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = leere Liste.</param>
    public static IReadOnlyList<Katalogfilterzeile> Kandidatenzeilen(FlottenAuslegungErgebnis ergebnis)
    {
        if (ergebnis?.Kandidaten is not { Count: > 0 } kandidaten)
            return Array.Empty<Katalogfilterzeile>();

        var zeilen = new List<Katalogfilterzeile>(kandidaten.Count);
        for (int i = 0; i < kandidaten.Count; i++)
        {
            FlottenKandidatZusammenfassung k = kandidaten[i];
            var zeile = new Katalogfilterzeile(i, k.KandidatId) { Schluessel = k.KandidatId };
            zeile.MitText(SP_KANDIDAT, k.KandidatId);
            zeile.MitText(SP_ZIEL, Zieltext(k.Betriebsziel));
            zeile.MitZahl(SP_KAPAZITAET, k.KapazitaetKWh, 1);
            zeile.MitZahl(SP_LADEN, k.LadeleistungKw, 1);
            zeile.MitZahl(SP_ENTLADEN, k.EntladeleistungKw, 1);
            zeile.MitZahl(SP_CRATE, k.KapazitaetKWh > 0.0 ? k.CRate : (double?)null, 2);
            zeile.MitZahl(SP_KAPITALWERT, double.IsFinite(k.KapitalwertEuro)
                ? k.KapitalwertEuro : (double?)null, 0);
            zeile.MitZahl(SP_ERSPARNIS, k.ErsparnisEuroJahr, 0);
            zeile.MitZahl(SP_VOLLZYKLEN, k.Vollzyklen, 1);
            zeile.MitZahl(SP_SPITZE, k.BezugsspitzeKw, 2);
            zeile.MitKennzeichen(SP_ZULAESSIG, k.Zulaessig);
            zeile.MitText(SP_GRUND, k.Grund);
            zeilen.Add(zeile);
        }
        return zeilen;
    }

    /// <summary>Der Name eines Betriebsziels — DIESELBE Ressource wie im Betriebseditor.</summary>
    public static string Zieltext(FlottenBetriebsziel ziel) => ziel switch
    {
        FlottenBetriebsziel.PvGreedy => MyResource.Resource.FLOTTE_ZIEL_PVGREEDY,
        FlottenBetriebsziel.PeakShaving => MyResource.Resource.FLOTTE_ZIEL_PEAKSHAVING,
        FlottenBetriebsziel.PvPlanung => MyResource.Resource.FLOTTE_ZIEL_PVPLANUNG,
        FlottenBetriebsziel.Arbitrage => MyResource.Resource.FLOTTE_ZIEL_ARBITRAGE,
        FlottenBetriebsziel.MultiUse => MyResource.Resource.FLOTTE_ZIEL_MULTIUSE,
        _ => ziel.ToString()
    };

    // =====================================================================
    //  Innenleben
    // =====================================================================

    /// <summary>EIN Kandidat an seiner Stelle im Raster.</summary>
    private sealed class Rasterpunkt
    {
        public double KapazitaetKWh { get; init; }

        public double CRate { get; init; }

        public FlottenKandidatZusammenfassung Kandidat { get; init; }
    }

    /// <summary>
    /// Die Kandidaten, die auf der Karte einen Platz haben. Ohne Kapazität gibt es keine
    /// C-Rate — die Nullvariante und ein Kandidat ohne Einheit stehen deshalb nicht im
    /// Raster, sondern in der Tabelle darunter.
    /// </summary>
    private static List<Rasterpunkt> Rasterpunkte(FlottenAuslegungErgebnis ergebnis, int einheit)
    {
        var punkte = new List<Rasterpunkt>();
        if (ergebnis?.Kandidaten is not { Count: > 0 } kandidaten) return punkte;

        foreach (FlottenKandidatZusammenfassung k in kandidaten)
        {
            double kapazitaet, leistung;
            if (einheit < 0)
            {
                kapazitaet = k.KapazitaetKWh;
                leistung = k.EntladeleistungKw;
            }
            else
            {
                if (k.Einheiten.Count <= einheit) continue;
                FlottenKandidatEinheit e = k.Einheiten[einheit];
                kapazitaet = e.KapazitaetKWh;
                leistung = e.EntladeleistungKw;
            }

            if (!double.IsFinite(kapazitaet) || kapazitaet <= 0.0) continue;
            if (!double.IsFinite(leistung)) continue;

            punkte.Add(new Rasterpunkt
            {
                KapazitaetKWh = kapazitaet,
                CRate = leistung / kapazitaet,
                Kandidat = k
            });
        }
        return punkte;
    }

    /// <summary>
    /// Eine Achse aus den vorkommenden Werten: aufsteigend, und zwei Werte, die sich um
    /// weniger als <see cref="ACHSENSCHRANKE"/> RELATIV unterscheiden, fallen zusammen.
    /// </summary>
    private static IReadOnlyList<double> Achse(IEnumerable<double> werte)
    {
        var sortiert = werte.OrderBy(x => x).ToArray();
        var achse = new List<double>();
        foreach (double w in sortiert)
            if (achse.Count == 0 || !Gleich(achse[^1], w)) achse.Add(w);
        return achse;
    }

    /// <summary>Die Stelle eines Wertes auf einer Achse; <c>-1</c>, wenn er nicht darauf liegt.</summary>
    private static int Stelle(IReadOnlyList<double> achse, double wert)
    {
        for (int i = 0; i < achse.Count; i++)
            if (Gleich(achse[i], wert)) return i;
        return -1;
    }

    /// <summary>Zwei Achsenwerte sind DERSELBE — relativ gemessen (siehe <see cref="ACHSENSCHRANKE"/>).</summary>
    private static bool Gleich(double a, double b)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b)) return false;
        double mass = Math.Max(Math.Abs(a), Math.Abs(b));
        return Math.Abs(a - b) <= ACHSENSCHRANKE * Math.Max(1.0, mass);
    }

    /// <summary>
    /// Besetzt <paramref name="neu"/> die Stelle statt <paramref name="alt"/>? Erst
    /// Zulässigkeit, dann Kapitalwert — die Rangfolge der Spezifikation 9.4.
    /// </summary>
    private static bool IstBesser(Rasterpunkt neu, Rasterpunkt alt)
    {
        if (neu.Kandidat.Zulaessig != alt.Kandidat.Zulaessig) return neu.Kandidat.Zulaessig;
        return neu.Kandidat.KapitalwertEuro > alt.Kandidat.KapitalwertEuro;
    }

    /// <summary>
    /// Aus Achse und Werten ein <see cref="FlottenSchnittdaten"/> samt Optimum-Marke; die
    /// Marke steht auf dem größten ENDLICHEN Wert der Kurve.
    /// </summary>
    private static FlottenSchnittdaten Schnitt(IReadOnlyList<double> achse,
                                               IReadOnlyList<double> werte)
    {
        double besteAchse = double.NaN, besterWert = double.NaN;
        for (int i = 0; i < achse.Count && i < werte.Count; i++)
        {
            if (!double.IsFinite(werte[i])) continue;
            if (double.IsNaN(besterWert) || werte[i] > besterWert)
            {
                besterWert = werte[i];
                besteAchse = achse[i];
            }
        }
        return new FlottenSchnittdaten(achse, werte, besteAchse, besterWert);
    }

    /// <summary>Die Bildüberschrift — mit Einheitennamen, sobald eine gewählt ist.</summary>
    private static string Bildtitel(string muster, int einheit, string einheitenname)
        => einheit < 0 || string.IsNullOrWhiteSpace(einheitenname)
            ? muster
            : muster + " — " + einheitenname;

    private static string Zahl(double wert, string format)
        => double.IsFinite(wert) ? wert.ToString(format, CultureInfo.CurrentCulture) : "";
}
