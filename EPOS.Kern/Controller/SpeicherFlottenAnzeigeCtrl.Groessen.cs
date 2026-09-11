using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// WELCHE GRÖSSE eine Achse der Rasterkarte trägt (Auftrag #226).
/// </summary>
/// <remarks>
/// Sie folgt aus der Größenkopplung der Suchachse (<see cref="FlottenAuslegungsmodus"/>)
/// und entscheidet Achsentitel, Schieberbeschriftung und Zahlenformat. Sie steht als
/// eigener Aufzählungstyp da, weil die drei Kopplungen ihre sechs Achsen aus nur DIESEN
/// DREI Größen bilden: Eine Oberfläche wählt ihre Texte danach, ohne die drei Modi noch
/// einmal aufzuzählen.
/// </remarks>
public enum Flottenachsengroesse
{
    /// <summary>Kapazität [kWh].</summary>
    Kapazitaet,

    /// <summary>Entladeleistung [kW].</summary>
    Leistung,

    /// <summary>C-Rate [1/h] — die Leistung in normierter Form.</summary>
    CRate
}

/// <summary>
/// Die Achsen und Werte EINER Rasterkarte der Auslegung (Auftrag #193, Konzept
/// „Stromspeicher-Dialoge" 2.5; Achsen nach Kopplung seit Auftrag #226).
/// </summary>
/// <remarks>
/// <para><b>Die Achsen folgen der GRÖSSENKOPPLUNG der Suche</b>
/// (<see cref="FlottenAuslegungErgebnis.Achsenmodus"/>) und nicht einer festen
/// Anordnung:</para>
/// <list type="table">
///   <item><term><c>KapazitaetUndLeistung</c></term>
///     <description>Zeilen Kapazität [kWh], Spalten Entladeleistung [kW] — genau das
///     eingegebene Raster, ohne Loch.</description></item>
///   <item><term><c>KapazitaetUndCRate</c></term>
///     <description>Zeilen Kapazität [kWh], Spalten C-Rate [1/h].</description></item>
///   <item><term><c>LeistungUndCRate</c></term>
///     <description>Zeilen Entladeleistung [kW], Spalten C-Rate [1/h].</description></item>
/// </list>
/// <para><b>Warum das der Befund vom 11.09.2026 erzwungen hat.</b> Bis #226 stand die
/// C-Rate IMMER auf der Spaltenachse. Im Modus <c>KapazitaetUndLeistung</c> liegen die
/// Kandidaten aber auf einem Kapazität × Leistung-Gitter; P/C ist dort kein Gitter,
/// sondern bis zu <c>n · m</c> verschiedene Quotienten. Aus 13 × 13 Kandidaten wurde so
/// eine Karte mit einer krummen C-Raten-Achse und überwiegend Löchern — und die Löcher
/// zeichnete der Renderer in der Minimumfarbe, also rot.</para>
/// <para><b>Ein Loch im Raster ist <c>double.NaN</c></b> — eine Stelle, an der kein
/// Kandidat gerechnet wurde. Sie geht nicht in die Farbskala ein (der Renderer
/// überspringt nicht endliche Werte, und seit #226 zeichnet er sie hellgrau statt in
/// der Minimumfarbe) und ist etwas anderes als ein Kandidat mit dem Kapitalwert 0.</para>
/// </remarks>
/// <param name="Modus">Die Größenkopplung, aus der die Achsen folgen.</param>
/// <param name="Spaltenwerte">Die Werte der Spaltenachse, aufsteigend.</param>
/// <param name="Zeilenwerte">Die Werte der Zeilenachse, aufsteigend.</param>
/// <param name="Werte">Kapitalwert [€] je Stelle <c>[iZeile][iSpalte]</c>; <c>NaN</c> = Loch.</param>
/// <param name="Unzulaessig">Je Stelle: Der dort stehende Kandidat verletzt eine harte Grenze.</param>
/// <param name="BesteZeile">Zeile des Optimums, oder <c>-1</c>.</param>
/// <param name="BesteSpalte">Spalte des Optimums, oder <c>-1</c>.</param>
/// <param name="Feinraster">
/// Je Stelle: Der dort stehende Kandidat stammt aus der ZWEITEN Phase (Auftrag #224);
/// <c>null</c> = keine Auskunft, wie vor #224. Die zweite Phase legt ihre Punkte
/// ZWISCHEN die Stuetzstellen des Grobrasters — sie bekommen deshalb eigene Zeilen, und
/// die Schnittkurve zeichnet sie unterscheidbar.
/// </param>
public sealed record FlottenRasterdaten(FlottenAuslegungsmodus Modus,
                                        IReadOnlyList<double> Spaltenwerte,
                                        IReadOnlyList<double> Zeilenwerte,
                                        double[][] Werte,
                                        bool[][] Unzulaessig,
                                        int BesteZeile, int BesteSpalte,
                                        bool[][] Feinraster = null)
{
    /// <summary>Es gibt keine Karte — kein Kandidat, oder nur die Nullvariante.</summary>
    public bool IstLeer => Spaltenwerte.Count == 0 || Zeilenwerte.Count == 0;

    /// <summary>Welche Größe die ZEILEN tragen.</summary>
    public Flottenachsengroesse Zeilengroesse
        => SpeicherFlottenAnzeigeCtrl.Zeilengroesse(Modus);

    /// <summary>Welche Größe die SPALTEN tragen.</summary>
    public Flottenachsengroesse Spaltengroesse
        => SpeicherFlottenAnzeigeCtrl.Spaltengroesse(Modus);

    /// <summary>Die leere Karte; sie ersetzt jedes <c>null</c> bei den Aufrufern.</summary>
    public static FlottenRasterdaten Leer { get; } =
        new(FlottenAuslegungsmodus.KapazitaetUndCRate,
            Array.Empty<double>(), Array.Empty<double>(), Array.Empty<double[]>(),
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
/// <param name="Feinpunkte">
/// Je Stuetzstelle: Sie stammt aus der ZWEITEN Phase der Suche (Auftrag #224);
/// <c>null</c> = keine Auskunft. Der Renderer zeichnet einen Feinpunkt in einer eigenen
/// Farbe — „wo ist grob gerastert, wo ist nachgeschaerft?" ist genau die Frage, die die
/// Mappe V7 mit ihren zwei Punktreihen beantwortet.
/// </param>
public sealed record FlottenSchnittdaten(IReadOnlyList<double> Achse,
                                         IReadOnlyList<double> Werte,
                                         double OptimumAchse, double OptimumWert,
                                         IReadOnlyList<bool> Feinpunkte = null)
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
    /// <para><b>Sie ist kein Ersatz für die richtige Achse</b> (Auftrag #226): Im Modus
    /// <c>KapazitaetUndLeistung</c> trägt jede Zeile eine ANDERE C-Rate — dort liegen
    /// die Quotienten nicht ein Bit, sondern Größenordnungen auseinander, und keine
    /// Schranke fasst sie zusammen. Genau deshalb steht dort die Leistung an der
    /// Spaltenachse und nicht die C-Rate.</para>
    /// </remarks>
    private const double ACHSENSCHRANKE = 1e-9;

    // =====================================================================
    //  Welche Größe auf welcher Achse steht (Auftrag #226)
    // =====================================================================

    /// <summary>Die Größe der ZEILENachse zu einer Größenkopplung.</summary>
    public static Flottenachsengroesse Zeilengroesse(FlottenAuslegungsmodus modus)
        => modus == FlottenAuslegungsmodus.LeistungUndCRate
            ? Flottenachsengroesse.Leistung
            : Flottenachsengroesse.Kapazitaet;

    /// <summary>Die Größe der SPALTENachse zu einer Größenkopplung.</summary>
    public static Flottenachsengroesse Spaltengroesse(FlottenAuslegungsmodus modus)
        => modus == FlottenAuslegungsmodus.KapazitaetUndLeistung
            ? Flottenachsengroesse.Leistung
            : Flottenachsengroesse.CRate;

    /// <summary>
    /// Die Größe, über der der ZWEITE Schnitt läuft — die GEGENGRÖSSE zur Zeilenachse.
    /// </summary>
    /// <remarks>
    /// Beide Schnitte zeigen eine PHYSIKALISCHE Größe, nie die C-Rate: Der erste läuft
    /// über der Zeilengröße (Kapazität bzw. Leistung), der zweite über der jeweils
    /// anderen. Im Modus <c>KapazitaetUndCRate</c> entsteht sie als <c>P = E · C</c>, im
    /// Modus <c>LeistungUndCRate</c> als <c>E = P / C</c>, im Modus
    /// <c>KapazitaetUndLeistung</c> steht sie ohne Umrechnung auf der Spaltenachse.
    /// </remarks>
    public static Flottenachsengroesse Gegengroesse(FlottenAuslegungsmodus modus)
        => Zeilengroesse(modus) == Flottenachsengroesse.Kapazitaet
            ? Flottenachsengroesse.Leistung
            : Flottenachsengroesse.Kapazitaet;

    // =====================================================================
    //  Die Rasterkarte
    // =====================================================================

    /// <summary>
    /// Die Rasterkarte einer Auslegung: Kapitalwert über den zwei Größen, die die
    /// Suchachse gekoppelt hat, dazu die Schraffurmatrix und die Stelle des Optimums.
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
    /// bei genau EINER aktiven Suchachse, und er sagt nichts über die Beschriftung. Was
    /// die Werte BEDEUTEN, sagt dagegen der
    /// <see cref="FlottenAuslegungErgebnis.Achsenmodus"/> — und genau das fehlte bis
    /// Auftrag #226.</para>
    /// </remarks>
    public static FlottenRasterdaten Rasterdaten(FlottenAuslegungErgebnis ergebnis,
                                                 int einheit = FLOTTE_GESAMT)
    {
        FlottenAuslegungsmodus modus = ergebnis?.Achsenmodus
                                       ?? FlottenAuslegungsmodus.KapazitaetUndCRate;
        List<Rasterpunkt> punkte = Rasterpunkte(ergebnis, einheit, modus);
        if (punkte.Count == 0) return FlottenRasterdaten.Leer;

        IReadOnlyList<double> zeilenwerte = Achse(punkte.Select(p => p.Zeilenwert));
        IReadOnlyList<double> spaltenwerte = Achse(punkte.Select(p => p.Spaltenwert));

        int zeilen = zeilenwerte.Count, spalten = spaltenwerte.Count;
        var werte = new double[zeilen][];
        var unzulaessig = new bool[zeilen][];
        var feinraster = new bool[zeilen][];
        var belegt = new Rasterpunkt[zeilen][];
        for (int i = 0; i < zeilen; i++)
        {
            werte[i] = new double[spalten];
            unzulaessig[i] = new bool[spalten];
            feinraster[i] = new bool[spalten];
            belegt[i] = new Rasterpunkt[spalten];
            for (int s = 0; s < spalten; s++) werte[i][s] = double.NaN;
        }

        foreach (Rasterpunkt p in punkte)
        {
            int zeile = Stelle(zeilenwerte, p.Zeilenwert);
            int spalte = Stelle(spaltenwerte, p.Spaltenwert);
            if (zeile < 0 || spalte < 0) continue;
            if (belegt[zeile][spalte] is { } alt && !IstBesser(p, alt)) continue;

            belegt[zeile][spalte] = p;
            werte[zeile][spalte] = double.IsFinite(p.Kandidat.KapitalwertEuro)
                ? p.Kandidat.KapitalwertEuro : double.NaN;
            unzulaessig[zeile][spalte] = !p.Kandidat.Zulaessig;
            feinraster[zeile][spalte] = p.Kandidat.Phase == FlottenKandidatPhase.Fein;
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

        return new FlottenRasterdaten(modus, spaltenwerte, zeilenwerte, werte, unzulaessig,
                                      besteZeile, besteSpalte, feinraster);
    }

    // =====================================================================
    //  Die zwei Schnitte
    // =====================================================================

    /// <summary>
    /// Der Schnitt BEI EINEM SPALTENWERT: Kapitalwert über der Zeilengröße — Kapazität
    /// [kWh], im Modus <c>LeistungUndCRate</c> Entladeleistung [kW] (Konzept 2.5).
    /// </summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="spaltenwert">Der festgehaltene Spaltenwert; er muss auf der Achse liegen.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static FlottenSchnittdaten SchnittdatenBeiSpalte(FlottenAuslegungErgebnis ergebnis,
                                                            double spaltenwert,
                                                            int einheit = FLOTTE_GESAMT)
        => SchnittBeiSpalte(Rasterdaten(ergebnis, einheit), spaltenwert);

    /// <summary>
    /// Der Schnitt BEI EINEM ZEILENWERT: Kapitalwert über der GEGENGRÖSSE — der
    /// Entladeleistung, im Modus <c>LeistungUndCRate</c> der Kapazität
    /// (Konzept 2.5, „daneben dieselbe Kurve über der Leistung").
    /// </summary>
    /// <remarks>
    /// Die Achse entsteht je nach Kopplung unmittelbar aus den Spaltenwerten
    /// (<c>KapazitaetUndLeistung</c>), als <c>P = E · C</c> (<c>KapazitaetUndCRate</c>)
    /// oder als <c>E = P / C</c> (<c>LeistungUndCRate</c>). Gezeichnet wird sie mit
    /// <see cref="ChartRenderer.Schnittkurve"/> — derselben Funktion wie der erste
    /// Schnitt, nur mit anderer Achsenbeschriftung.
    /// </remarks>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="zeilenwert">Der festgehaltene Zeilenwert; er muss auf der Achse liegen.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static FlottenSchnittdaten SchnittdatenBeiZeile(FlottenAuslegungErgebnis ergebnis,
                                                           double zeilenwert,
                                                           int einheit = FLOTTE_GESAMT)
        => SchnittBeiZeile(Rasterdaten(ergebnis, einheit), zeilenwert);

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
            Bildtitel(Rastertitel(raster.Modus), einheit, einheitenname),
            Achsentext(raster.Spaltengroesse),
            Achsentext(raster.Zeilengroesse),
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            raster.Spaltenwerte, raster.Zeilenwerte, raster.Werte,
            raster.BesteZeile, raster.BesteSpalte,
            raster.Unzulaessig, MyResource.Resource.FLOTTE_GROESSEN_ENDLICHES_RASTER);
    }

    /// <summary>Die SCHNITTKURVE bei einem Spaltenwert als PNG; ohne Kurve <c>null</c>.</summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="spaltenwert">Der festgehaltene Spaltenwert.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static byte[] SchnittbildBeiSpalte(FlottenAuslegungErgebnis ergebnis,
                                              double spaltenwert,
                                              int einheit = FLOTTE_GESAMT)
    {
        FlottenRasterdaten raster = Rasterdaten(ergebnis, einheit);
        FlottenSchnittdaten schnitt = SchnittBeiSpalte(raster, spaltenwert);
        if (schnitt.IstLeer) return null;

        return ChartRenderer.Schnittkurve(
            Schnitttitel(raster.Zeilengroesse, raster.Spaltengroesse, spaltenwert),
            Achsentext(raster.Zeilengroesse),
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            schnitt.Achse, schnitt.Werte, schnitt.OptimumAchse, schnitt.OptimumWert,
            schnitt.Feinpunkte);
    }

    /// <summary>Die SCHNITTKURVE bei einem Zeilenwert als PNG; ohne Kurve <c>null</c>.</summary>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche.</param>
    /// <param name="zeilenwert">Der festgehaltene Zeilenwert.</param>
    /// <param name="einheit"><see cref="FLOTTE_GESAMT"/> oder die Stelle der Einheit.</param>
    public static byte[] SchnittbildBeiZeile(FlottenAuslegungErgebnis ergebnis,
                                             double zeilenwert,
                                             int einheit = FLOTTE_GESAMT)
    {
        FlottenRasterdaten raster = Rasterdaten(ergebnis, einheit);
        FlottenSchnittdaten schnitt = SchnittBeiZeile(raster, zeilenwert);
        if (schnitt.IstLeer) return null;

        return ChartRenderer.Schnittkurve(
            Schnitttitel(Gegengroesse(raster.Modus), raster.Zeilengroesse, zeilenwert),
            Achsentext(Gegengroesse(raster.Modus)),
            MyResource.Resource.FLOTTE_GROESSEN_SKALA,
            schnitt.Achse, schnitt.Werte, schnitt.OptimumAchse, schnitt.OptimumWert,
            schnitt.Feinpunkte);
    }

    // =====================================================================
    //  Die Texte je Achsengröße und Kopplung (Auftrag #226)
    // =====================================================================
    //
    //  Sie stehen HIER und nicht in der Oberflaeche, weil der Kern SELBST drei davon
    //  in die Bilder schreibt (Titel und die zwei Achsentitel). Zwei Wahrheiten -
    //  eine im Bild, eine daneben im Markup - liefen beim ersten neuen Modus
    //  auseinander.

    /// <summary>Der Achsentitel einer Größe — <c>„Kapazität [kWh]"</c> und so fort.</summary>
    /// <param name="groesse">Die Größe der Achse.</param>
    public static string Achsentext(Flottenachsengroesse groesse) => groesse switch
    {
        Flottenachsengroesse.Leistung => MyResource.Resource.FLOTTE_GROESSEN_ACHSE_LEISTUNG,
        Flottenachsengroesse.CRate => MyResource.Resource.FLOTTE_GROESSEN_ACHSE_CRATE,
        _ => MyResource.Resource.FLOTTE_GROESSEN_ACHSE_KAPAZITAET
    };

    /// <summary>Die Beschriftung des Schiebers, der diese Größe festhält.</summary>
    /// <param name="groesse">Die Größe, die der Schieber wählt.</param>
    public static string Schiebertext(Flottenachsengroesse groesse) => groesse switch
    {
        Flottenachsengroesse.Leistung => MyResource.Resource.FLOTTE_GROESSEN_LBL_LEISTUNG,
        Flottenachsengroesse.CRate => MyResource.Resource.FLOTTE_GROESSEN_LBL_CRATE,
        _ => MyResource.Resource.FLOTTE_GROESSEN_LBL_KAPAZITAET
    };

    /// <summary>Ein Achsenwert mit seiner Einheit — <c>„20 kWh"</c>, <c>„1 C"</c>.</summary>
    /// <param name="groesse">Die Größe, deren Einheit dahinter steht.</param>
    /// <param name="wert">Der Wert; nicht endlich lässt den Zahlteil leer.</param>
    public static string Werttext(Flottenachsengroesse groesse, double wert) => string.Format(
        CultureInfo.CurrentCulture,
        groesse switch
        {
            Flottenachsengroesse.Leistung => MyResource.Resource.FLOTTE_GROESSEN_WERT_LEISTUNG,
            Flottenachsengroesse.CRate => MyResource.Resource.FLOTTE_GROESSEN_WERT_CRATE,
            _ => MyResource.Resource.FLOTTE_GROESSEN_WERT_KAPAZITAET
        },
        Zahl(wert, Zahlenformat(groesse)));

    /// <summary>Die Überschrift der Rasterkarte je Größenkopplung.</summary>
    /// <param name="modus">Die Größenkopplung der Suchachse.</param>
    public static string Rastertitel(FlottenAuslegungsmodus modus) => modus switch
    {
        FlottenAuslegungsmodus.KapazitaetUndLeistung =>
            MyResource.Resource.FLOTTE_GROESSEN_CHART_RASTER_KW,
        FlottenAuslegungsmodus.LeistungUndCRate =>
            MyResource.Resource.FLOTTE_GROESSEN_CHART_RASTER_KW_C,
        _ => MyResource.Resource.FLOTTE_GROESSEN_CHART_RASTER
    };

    /// <summary>Die Bildbeschreibung der Rasterkarte je Größenkopplung (<c>alt</c>-Text).</summary>
    /// <param name="modus">Die Größenkopplung der Suchachse.</param>
    public static string Rasterbeschreibung(FlottenAuslegungsmodus modus) => modus switch
    {
        FlottenAuslegungsmodus.KapazitaetUndLeistung =>
            MyResource.Resource.FLOTTE_GROESSEN_ALT_RASTER_KW,
        FlottenAuslegungsmodus.LeistungUndCRate =>
            MyResource.Resource.FLOTTE_GROESSEN_ALT_RASTER_KW_C,
        _ => MyResource.Resource.FLOTTE_GROESSEN_ALT_RASTER
    };

    /// <summary>
    /// Die Überschrift einer Schnittkurve: worüber sie läuft und bei welchem Wert der
    /// anderen Größe sie steht.
    /// </summary>
    /// <param name="ueber">Die Größe der Achse — Kapazität oder Leistung, nie die C-Rate.</param>
    /// <param name="bei">Die festgehaltene Größe.</param>
    /// <param name="wert">Ihr Wert.</param>
    public static string Schnitttitel(Flottenachsengroesse ueber, Flottenachsengroesse bei,
                                      double wert)
        => string.Format(CultureInfo.CurrentCulture,
                         ueber == Flottenachsengroesse.Kapazitaet
                             ? bei == Flottenachsengroesse.CRate
                                 ? MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_KAPAZITAET
                                 : MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_KAP_BEI_KW
                             : bei == Flottenachsengroesse.CRate
                                 ? MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_LEI_BEI_C
                                 : MyResource.Resource.FLOTTE_GROESSEN_CHART_SCHNITT_LEISTUNG,
                         Zahl(wert, Zahlenformat(bei)));

    /// <summary>
    /// Die Bildbeschreibung einer Schnittkurve (<c>alt</c>-Text); die Rollen sind
    /// dieselben wie bei <see cref="Schnitttitel"/>.
    /// </summary>
    /// <param name="ueber">Die Größe der Achse.</param>
    /// <param name="bei">Die festgehaltene Größe.</param>
    public static string Schnittbeschreibung(Flottenachsengroesse ueber, Flottenachsengroesse bei)
        => ueber == Flottenachsengroesse.Kapazitaet
            ? bei == Flottenachsengroesse.CRate
                ? MyResource.Resource.FLOTTE_GROESSEN_ALT_SCHNITT
                : MyResource.Resource.FLOTTE_GROESSEN_ALT_SCHNITT_KAP_BEI_KW
            : bei == Flottenachsengroesse.CRate
                ? MyResource.Resource.FLOTTE_GROESSEN_ALT_SCHNITT_LEI_BEI_C
                : MyResource.Resource.FLOTTE_GROESSEN_ALT_SCHNITT_LEISTUNG;

    /// <summary>Das Zahlenformat einer Achsengröße: die C-Rate feiner als kWh und kW.</summary>
    private static string Zahlenformat(Flottenachsengroesse groesse)
        => groesse == Flottenachsengroesse.CRate ? "0.###" : "0.#";

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
    //  „Kandidat übernehmen" — vom Kandidaten zurück zur Flottenkonfiguration
    // =====================================================================

    /// <summary>
    /// Baut aus EINEM Kandidaten der Rastersuche die Flottenkonfiguration, die der
    /// Anwender damit übernimmt (Auftrag #196, Konzept „Stromspeicher-Dialoge" 2.5:
    /// „Kandidat übernehmen" je Zeile).
    /// </summary>
    /// <remarks>
    /// <para><b>Für den BESTEN Kandidaten wird nichts gebaut.</b> Der
    /// <c>FlottenOptimierer</c> hat dessen Konfiguration selbst gebildet und legt sie als
    /// <see cref="FlottenAuslegungErgebnis.BesteKonfiguration"/> bei — sie wird genommen.
    /// Das ist derselbe Weg, den der Knopf „Beste Flotte übernehmen" seit jeher geht; für
    /// die Optimum-Zeile der Kandidatentabelle darf es keine zweite Wahrheit geben.</para>
    /// <para><b>Für jeden anderen Kandidaten ist es eine RÜCKABBILDUNG, keine zweite
    /// Rastersuche.</b> Der Kandidat trägt seine Größen je Einheit
    /// (<see cref="FlottenKandidatEinheit"/>) und sein Betriebsziel; alles Übrige —
    /// Wirkungsgrade, SoC-Band, Kosten, Wirtschaftlichkeit, Betriebsoptionen — kommt
    /// unverändert aus dem Arbeitsstand. Als Vorlage EINER Einheit dient die Einheit
    /// gleicher Kennung, sonst die Vorlage der ersten aktiven Suchachse, sonst die erste
    /// Einheit des Arbeitsstands: Die Rastersuche ERSETZT Einheiten durch Abwandlungen
    /// ihrer Vorlage (<c>FlottenOptimierer.BildeAchse</c>), und genau deren Kennungen
    /// stehen im Kandidaten.</para>
    /// <para><b>Die Wirtschaftlichkeitsliste wird mitgezogen.</b>
    /// <c>FlottenWirtschaftlichkeitEingang.Einheiten</c> trägt Investition, Betrieb,
    /// Ersatz und Restwert; der Optimierer spiegelt sie je Kandidat, und der
    /// <c>SpeicherFlottenEditor</c> tut dasselbe bei jeder Änderung. Bliebe sie hier
    /// stehen, rechnete der nächste Lauf die Kosten der ALTEN Größen.</para>
    /// <para>Die <b>Suchachsen</b> bleiben unberührt — ob sie nach der Übernahme geleert
    /// werden, entscheidet der Aufrufer; der Weg „Beste Flotte übernehmen" tut es.</para>
    /// </remarks>
    /// <param name="ergebnis">Das Ergebnis der Rastersuche; <c>null</c> = kein Optimum bekannt.</param>
    /// <param name="arbeitsstand">Die Flotte, wie sie in Schritt 1 steht; <c>null</c> = leer.</param>
    /// <param name="kandidat">Der gewählte Kandidat.</param>
    /// <returns>Die neue Konfiguration; <c>null</c>, wenn kein Kandidat vorliegt.</returns>
    public static FlottenStudieKonfiguration KandidatKonfiguration(
        FlottenAuslegungErgebnis ergebnis,
        FlottenStudieKonfiguration arbeitsstand,
        FlottenKandidatZusammenfassung kandidat)
    {
        if (kandidat == null) return null;

        if (ergebnis != null && ReferenceEquals(kandidat, ergebnis.BesterKandidat) &&
            ergebnis.BesteKonfiguration != null)
            return SpeicherAuslegungKopie.Von(ergebnis.BesteKonfiguration);

        FlottenStudieKonfiguration ziel =
            SpeicherAuslegungKopie.Von(arbeitsstand) ?? new FlottenStudieKonfiguration();
        ziel.Einheiten ??= new List<FlottenEinheit>();
        ziel.Optionen ??= new FlottenSimulationOptionen();
        ziel.Wirtschaftlichkeit ??= new FlottenWirtschaftlichkeitEingang();
        ziel.Auslegung ??= new FlottenAuslegungEingang();

        ziel.Optionen.Betriebsziel = kandidat.Betriebsziel;

        var neue = new List<FlottenEinheit>(kandidat.Einheiten?.Count ?? 0);
        foreach (FlottenKandidatEinheit teil in kandidat.Einheiten ?? new List<FlottenKandidatEinheit>())
        {
            FlottenEinheit einheit = SpeicherAuslegungKopie.Von(Einheitenvorlage(ziel, teil.Id))
                                     ?? new FlottenEinheit();
            einheit.Id = teil.Id;
            if (string.IsNullOrWhiteSpace(einheit.Name)) einheit.Name = teil.Id;
            einheit.KapazitaetKWh = teil.KapazitaetKWh;
            einheit.LadeleistungKw = teil.LadeleistungKw;
            einheit.EntladeleistungKw = teil.EntladeleistungKw;
            neue.Add(einheit);
        }

        ziel.Einheiten = neue;
        ziel.Wirtschaftlichkeit.Einheiten =
            neue.Select(x => SpeicherAuslegungKopie.Von(x)).ToList();
        return ziel;
    }

    /// <summary>
    /// Die Vorlage für EINE Einheit des Kandidaten: die gleichnamige Einheit des
    /// Arbeitsstands, sonst die Vorlage der ersten aktiven Suchachse, sonst die erste
    /// Einheit. <c>null</c>, wenn es nichts davon gibt.
    /// </summary>
    private static FlottenEinheit Einheitenvorlage(FlottenStudieKonfiguration stand, string id)
    {
        FlottenEinheit gleich = stand.Einheiten
            .FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        if (gleich != null) return gleich;

        FlottenEinheit vorlage = stand.Auslegung?.Achsen?
            .FirstOrDefault(a => a.Aktiv && a.Vorlage != null)?.Vorlage;
        return vorlage ?? stand.Einheiten.FirstOrDefault();
    }

    // =====================================================================
    //  Innenleben
    // =====================================================================

    /// <summary>EIN Kandidat an seiner Stelle im Raster.</summary>
    /// <remarks>
    /// Er trägt seit Auftrag #226 ZEILEN- und SPALTENwert statt Kapazität und C-Rate:
    /// Welche Größe darin steht, sagt die Größenkopplung — der Punkt selbst weiß es
    /// nicht mehr, und genau das hält die Karte von der falschen Achse fern.
    /// </remarks>
    private sealed class Rasterpunkt
    {
        public double Zeilenwert { get; init; }

        public double Spaltenwert { get; init; }

        public FlottenKandidatZusammenfassung Kandidat { get; init; }
    }

    /// <summary>
    /// Die Kandidaten, die auf der Karte einen Platz haben, mit ihrer Stelle NACH DER
    /// GRÖSSENKOPPLUNG. Ohne Kapazität gibt es weder eine Kapazitätsachse noch eine
    /// C-Rate — die Nullvariante und ein Kandidat ohne Einheit stehen deshalb nicht im
    /// Raster, sondern in der Tabelle darunter.
    /// </summary>
    private static List<Rasterpunkt> Rasterpunkte(FlottenAuslegungErgebnis ergebnis, int einheit,
                                                  FlottenAuslegungsmodus modus)
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
                Zeilenwert = Zeilengroesse(modus) == Flottenachsengroesse.Leistung
                    ? leistung : kapazitaet,
                Spaltenwert = Spaltengroesse(modus) == Flottenachsengroesse.Leistung
                    ? leistung : leistung / kapazitaet,
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

    /// <summary>Der Schnitt bei einem Spaltenwert auf einer BEREITS gebauten Karte.</summary>
    private static FlottenSchnittdaten SchnittBeiSpalte(FlottenRasterdaten raster,
                                                        double spaltenwert)
    {
        int spalte = Stelle(raster.Spaltenwerte, spaltenwert);
        if (spalte < 0) return FlottenSchnittdaten.Leer;

        var werte = new double[raster.Zeilenwerte.Count];
        var fein = new bool[raster.Zeilenwerte.Count];
        for (int i = 0; i < werte.Length; i++)
        {
            werte[i] = raster.Werte[i][spalte];
            fein[i] = raster.Feinraster is { } f && f[i][spalte];
        }
        return Schnitt(raster.Zeilenwerte, werte, fein);
    }

    /// <summary>Der Schnitt bei einem Zeilenwert auf einer BEREITS gebauten Karte.</summary>
    private static FlottenSchnittdaten SchnittBeiZeile(FlottenRasterdaten raster,
                                                       double zeilenwert)
    {
        int zeile = Stelle(raster.Zeilenwerte, zeilenwert);
        if (zeile < 0) return FlottenSchnittdaten.Leer;

        return Schnitt(Gegenachse(raster, raster.Zeilenwerte[zeile]), raster.Werte[zeile],
                       raster.Feinraster?[zeile]);
    }

    /// <summary>
    /// Die Achse des ZWEITEN Schnitts: die Gegengröße zur festgehaltenen Zeile.
    /// </summary>
    /// <remarks>
    /// Im Modus <c>KapazitaetUndLeistung</c> steht sie schon da — die Spalten SIND die
    /// Leistungen. In den zwei C-Raten-Modi entsteht sie aus <c>P = E · C</c> bzw.
    /// <c>E = P / C</c>; eine C-Rate von 0 hätte dort keine Kapazität und wird zum Loch,
    /// statt die ganze Kurve mit einer Unendlichkeit zu verderben.
    /// </remarks>
    private static IReadOnlyList<double> Gegenachse(FlottenRasterdaten raster, double zeilenwert)
        => raster.Modus switch
        {
            FlottenAuslegungsmodus.KapazitaetUndLeistung => raster.Spaltenwerte,
            FlottenAuslegungsmodus.KapazitaetUndCRate =>
                raster.Spaltenwerte.Select(c => c * zeilenwert).ToArray(),
            _ => raster.Spaltenwerte
                       .Select(c => c > 0.0 ? zeilenwert / c : double.NaN).ToArray()
        };

    /// <summary>
    /// Aus Achse und Werten ein <see cref="FlottenSchnittdaten"/> samt Optimum-Marke; die
    /// Marke steht auf dem größten ENDLICHEN Wert der Kurve.
    /// </summary>
    /// <remarks>
    /// Die Stützstellen werden dabei AUFSTEIGEND gelegt: Im Modus
    /// <c>LeistungUndCRate</c> entsteht die Kapazitätsachse als <c>E = P / C</c> und
    /// fällt damit über der aufsteigenden C-Rate. In den übrigen Fällen steht die Achse
    /// bereits aufsteigend, und die Ordnung lässt sie unberührt.
    /// </remarks>
    /// <param name="achse">Die Stützstellen.</param>
    /// <param name="werte">Der Kapitalwert je Stützstelle.</param>
    /// <param name="fein">
    /// Je Stützstelle: Sie stammt aus der zweiten Phase (Auftrag #224); <c>null</c> =
    /// keine Auskunft. Sie wird MITSORTIERT — sonst zeigte die Marke auf die Stelle, an
    /// der der Punkt vor dem Sortieren stand.
    /// </param>
    private static FlottenSchnittdaten Schnitt(IReadOnlyList<double> achse,
                                               IReadOnlyList<double> werte,
                                               IReadOnlyList<bool> fein = null)
    {
        int n = Math.Min(achse.Count, werte.Count);
        var stellen = Enumerable.Range(0, n)
            .OrderBy(i => double.IsFinite(achse[i]) ? achse[i] : double.MaxValue)
            .ToArray();

        var a = new double[n];
        var w = new double[n];
        var f = fein is null ? null : new bool[n];
        double besteAchse = double.NaN, besterWert = double.NaN;
        for (int i = 0; i < n; i++)
        {
            a[i] = achse[stellen[i]];
            w[i] = werte[stellen[i]];
            if (f is not null) f[i] = stellen[i] < fein.Count && fein[stellen[i]];
            if (!double.IsFinite(w[i])) continue;
            if (double.IsNaN(besterWert) || w[i] > besterWert)
            {
                besterWert = w[i];
                besteAchse = a[i];
            }
        }
        return new FlottenSchnittdaten(a, w, besteAchse, besterWert, f);
    }

    /// <summary>Die Bildüberschrift — mit Einheitennamen, sobald eine gewählt ist.</summary>
    private static string Bildtitel(string muster, int einheit, string einheitenname)
        => einheit < 0 || string.IsNullOrWhiteSpace(einheitenname)
            ? muster
            : muster + " — " + einheitenname;

    private static string Zahl(double wert, string format)
        => double.IsFinite(wert) ? wert.ToString(format, CultureInfo.CurrentCulture) : "";
}
