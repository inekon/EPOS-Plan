using KiKern;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Das FLACHE Abbild der Maske „Lastspitzenkappung" für den Hilfe-Assistenten
/// (Welle KI‑F6).
///
/// <para><b>Warum eine Sichtklasse und kein Daten-Objekt.</b> Der Dialog führt
/// seinen Arbeitsstand nicht in einem DTO, sondern in privaten Feldern der
/// Komponente (<c>_pKw</c>, <c>_kapazitaet</c>, <c>_adaptiv</c> …); ein
/// <c>PeakShavingEingaben</c> entsteht erst im Rechenweg und ist danach fort.
/// Ein daran angemeldeter Katalog zeigte den Stand von vorhin und setzte ins
/// Leere — dieselbe Lage wie bei den Masken der Simulationskonfiguration
/// (Welle KI‑F2).</para>
///
/// <para><b>Sie hält KEINEN Zustand.</b> Jede Eigenschaft greift über ihren
/// Delegaten auf das lebende Feld der offenen Maske; gesetzt wird über denselben
/// Weg, den auch eine Hand nimmt.</para>
///
/// <para><b>Drei Eigenschaften sind ABGELEITET</b> — das offene Reiterblatt, die
/// Reihenzeile und die Herkunftszeile. Ein Blattwechsel ist eine Bedienhandlung
/// und kein Feldwert (dieselbe Begründung wie bei
/// <c>StromspeicherKiSicht.Schritt</c>); Reihen- und Herkunftszeile sind
/// gerechnete Anzeigen des geladenen Lastgangs.</para>
///
/// <para><b>Die DATEIWAHL bleibt draußen.</b> Eine Datei einzulesen ist ein
/// Ladevorgang und kein Feldwert (KI‑D‑Q6); der Pfad allein setzt nichts, und
/// die Maske liest erst nach dem Import.</para>
/// </summary>
public sealed class PeakShavingKiSicht
{
    // ---- Lastgang ----------------------------------------------------------

    /// <summary>Liest die gewählte Quelle (0 = Ganglinie, 1 = Datei).</summary>
    public Func<int?>? QuelleLesen { get; init; }

    /// <summary>Setzt die Quelle — denselben Weg wie ein Griff in die Optionsgruppe.</summary>
    public Action<int?>? QuelleSetzen { get; init; }

    /// <summary>Die zwei Einträge der Optionsgruppe.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? QuelleEintraege { get; init; }

    /// <summary>Liest die gewählte Katalogganglinie.</summary>
    public Func<int?>? GanglinieLesen { get; init; }

    /// <summary>Setzt die Katalogganglinie und lädt sie nach.</summary>
    public Action<int?>? GanglinieSetzen { get; init; }

    /// <summary>Die Ganglinien des Projekts, wie die Klappliste sie zeigt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? GanglinieEintraege { get; init; }

    /// <summary>Die Reihenzeile über dem Diagramm — Anzeige.</summary>
    public Func<string>? ReiheLesen { get; init; }

    /// <summary>Die Herkunftszeile der Vorbelegung — Anzeige.</summary>
    public Func<string>? HerkunftLesen { get; init; }

    /// <summary>Das offene Reiterblatt (KENNZAHLEN, CHART, MONATE) — Anzeige.</summary>
    public Func<string>? ReiterLesen { get; init; }

    // ---- Die Zahlen der Maske ---------------------------------------------

    /// <summary>Je Feld ein Leser; <c>null</c> = die Maske meldet es nicht.</summary>
    public Func<string, double?>? ZahlLesen { get; init; }

    /// <summary>Je Feld ein Setzer auf das lebende Eingabefeld.</summary>
    public Action<string, double?>? ZahlSetzen { get; init; }

    /// <summary>Je Schalter ein Leser.</summary>
    public Func<string, bool>? SchalterLesen { get; init; }

    /// <summary>Je Schalter ein Setzer.</summary>
    public Action<string, bool>? SchalterSetzen { get; init; }

    // =====================================================================
    //  Die Feldnamen — sprachneutral und je einmal
    // =====================================================================

    /// <summary>Feldschlüssel der Speicherleistung.</summary>
    public const string F_LEISTUNG = "leistung";

    /// <summary>Feldschlüssel der Speicherkapazität.</summary>
    public const string F_KAPAZITAET = "kapazitaet";

    /// <summary>Feldschlüssel des Umlaufwirkungsgrads.</summary>
    public const string F_WIRKUNGSGRAD = "wirkungsgrad";

    /// <summary>Feldschlüssel der SoC-Untergrenze.</summary>
    public const string F_SOC_MIN = "soc_min";

    /// <summary>Feldschlüssel der SoC-Obergrenze.</summary>
    public const string F_SOC_MAX = "soc_max";

    /// <summary>Feldschlüssel des Anfangsladezustands.</summary>
    public const string F_START_SOC = "start_soc";

    /// <summary>Feldschlüssel der Zielschwelle.</summary>
    public const string F_ZIEL = "ziel";

    /// <summary>Feldschlüssel des Leistungspreises.</summary>
    public const string F_LEISTUNGSPREIS = "leistungspreis";

    /// <summary>Feldschlüssel des mittleren Bezugspreises.</summary>
    public const string F_BEZUGSPREIS = "bezugspreis";

    /// <summary>Feldschlüssel der kapazitätsbezogenen Investition.</summary>
    public const string F_C_CAP = "c_cap";

    /// <summary>Feldschlüssel der leistungsbezogenen Investition.</summary>
    public const string F_C_POW = "c_pow";

    /// <summary>Feldschlüssel der festen Investition.</summary>
    public const string F_I_FIX = "i_fix";

    /// <summary>Feldschlüssel des Kapitalzinses.</summary>
    public const string F_ZINS = "zins";

    /// <summary>Feldschlüssel der Nutzungsdauer.</summary>
    public const string F_NUTZUNGSDAUER = "nutzungsdauer";

    /// <summary>Schalterschlüssel der adaptiven Schwelle.</summary>
    public const string S_ADAPTIV = "adaptiv";

    /// <summary>Schalterschlüssel des Kompatibilitätsmodus.</summary>
    public const string S_KOMPATIBEL = "kompatibel";

    /// <summary>Schalterschlüssel der Ladezustandskurve im Bild.</summary>
    public const string S_LADEZUSTAND = "ladezustand";

    // =====================================================================
    //  Was der Katalog sieht
    // =====================================================================

    /// <summary>Die zwei Quellen des Lastgangs.</summary>
    public IReadOnlyList<KiWahleintrag> QuelleWahl
        => QuelleEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Woher der Lastgang kommt — vorhandene Ganglinie oder Datei.</summary>
    public int? Quelle
    {
        get => QuelleLesen?.Invoke();
        set => QuelleSetzen?.Invoke(value);
    }

    /// <summary>Die wählbaren Ganglinien des Projekts.</summary>
    public IReadOnlyList<KiWahleintrag> GanglinieWahl
        => GanglinieEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die gewählte Katalogganglinie.</summary>
    public int? Ganglinie
    {
        get => GanglinieLesen?.Invoke();
        set => GanglinieSetzen?.Invoke(value);
    }

    /// <summary>Die geladene Reihe samt Wertzahl und Maximum — Anzeige.</summary>
    public string Reihe => ReiheLesen?.Invoke() ?? "";

    /// <summary>Woher die Vorbelegung der Zahlen stammt — Anzeige.</summary>
    public string Herkunft => HerkunftLesen?.Invoke() ?? "";

    /// <summary>Das offene Reiterblatt — Anzeige.</summary>
    public string Reiter => ReiterLesen?.Invoke() ?? "";

    /// <summary>Entladeleistung des Speichers.</summary>
    public double? Leistung
    {
        get => Zahl(F_LEISTUNG);
        set => Setze(F_LEISTUNG, value);
    }

    /// <summary>Nutzbare Speicherkapazität.</summary>
    public double? Kapazitaet
    {
        get => Zahl(F_KAPAZITAET);
        set => Setze(F_KAPAZITAET, value);
    }

    /// <summary>Umlaufwirkungsgrad des Speichers.</summary>
    public double? Wirkungsgrad
    {
        get => Zahl(F_WIRKUNGSGRAD);
        set => Setze(F_WIRKUNGSGRAD, value);
    }

    /// <summary>Untere Grenze des Ladezustands.</summary>
    public double? SocMin
    {
        get => Zahl(F_SOC_MIN);
        set => Setze(F_SOC_MIN, value);
    }

    /// <summary>Obere Grenze des Ladezustands.</summary>
    public double? SocMax
    {
        get => Zahl(F_SOC_MAX);
        set => Setze(F_SOC_MAX, value);
    }

    /// <summary>Ladezustand zu Beginn der Rechnung.</summary>
    public double? StartSoc
    {
        get => Zahl(F_START_SOC);
        set => Setze(F_START_SOC, value);
    }

    /// <summary>Die feste Zielschwelle des Netzbezugs.</summary>
    public double? Zielschwelle
    {
        get => Zahl(F_ZIEL);
        set => Setze(F_ZIEL, value);
    }

    /// <summary>Der Leistungspreis des Netzanschlusses.</summary>
    public double? Leistungspreis
    {
        get => Zahl(F_LEISTUNGSPREIS);
        set => Setze(F_LEISTUNGSPREIS, value);
    }

    /// <summary>Der mittlere Arbeitspreis des Netzbezugs.</summary>
    public double? Bezugspreis
    {
        get => Zahl(F_BEZUGSPREIS);
        set => Setze(F_BEZUGSPREIS, value);
    }

    /// <summary>Kapazitätsbezogene Investition.</summary>
    public double? KostenKapazitaet
    {
        get => Zahl(F_C_CAP);
        set => Setze(F_C_CAP, value);
    }

    /// <summary>Leistungsbezogene Investition.</summary>
    public double? KostenLeistung
    {
        get => Zahl(F_C_POW);
        set => Setze(F_C_POW, value);
    }

    /// <summary>Feste Investition unabhängig von Größe und Leistung.</summary>
    public double? InvestitionFix
    {
        get => Zahl(F_I_FIX);
        set => Setze(F_I_FIX, value);
    }

    /// <summary>Kalkulationszins der Annuität.</summary>
    public double? Zins
    {
        get => Zahl(F_ZINS);
        set => Setze(F_ZINS, value);
    }

    /// <summary>Nutzungsdauer des Speichers.</summary>
    public double? Nutzungsdauer
    {
        get => Zahl(F_NUTZUNGSDAUER);
        set => Setze(F_NUTZUNGSDAUER, value);
    }

    /// <summary>Zieht die Schwelle im Lauf nach (Ratsche).</summary>
    public bool Adaptiv
    {
        get => Schalter(S_ADAPTIV);
        set => Setze(S_ADAPTIV, value);
    }

    /// <summary>Rechnet ohne Verluste und ohne SoC-Untergrenze.</summary>
    public bool Kompatibel
    {
        get => Schalter(S_KOMPATIBEL);
        set => Setze(S_KOMPATIBEL, value);
    }

    /// <summary>Zeigt die Ladezustandskurve im Diagramm.</summary>
    public bool Ladezustand
    {
        get => Schalter(S_LADEZUSTAND);
        set => Setze(S_LADEZUSTAND, value);
    }

    // ---- Hilfen ------------------------------------------------------------

    private double? Zahl(string feld) => ZahlLesen?.Invoke(feld);

    private void Setze(string feld, double? wert) => ZahlSetzen?.Invoke(feld, wert);

    private bool Schalter(string feld) => SchalterLesen?.Invoke(feld) ?? false;

    private void Setze(string feld, bool wert) => SchalterSetzen?.Invoke(feld, wert);
}
