using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Kostenverwaltung für den Hilfe-Assistenten (Welle KI‑F7,
/// Anwenderentscheid 21.09.2026).
///
/// <para><b>Warum eine Sichtklasse — der Dialog HAT doch einen Stand.</b> Er hat einen,
/// und diese Klasse reicht jedes seiner Felder unverändert durch: Titel, Unterzeile,
/// Schreibschutz, die gewählte Variante und das ganze Positionsraster stehen weiterhin
/// an <see cref="KostenKomponenteStand"/>. DREI Größen der Maske stehen dort aber nicht
/// — die drei Lücken, die die Welle KI‑F4 benannt hat: die KOMPONENTENWAHL der
/// Kontextleiste liegt in einem privaten Feld des Dialogs, die PV‑WAHL und das
/// PV‑PROJEKT des Reiters „Ertrag" im Baustein <c>ErtragBonus</c>. Die Brücke kennt je
/// Maske EIN Daten-Objekt; also trägt die Sicht alle drei Herkünfte.</para>
///
/// <para><b>Sie hält keinen Zustand.</b> Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten — der Anwender wechselt die Komponente, legt Positionen an und löscht sie,
/// während der Dialog steht.</para>
///
/// <para><b>Zwei der drei sind nur LESBAR, und das ist der Punkt.</b> Komponente und
/// PV‑Vergütung zu WECHSELN ist kein Feldwert, sondern ein Ladevorgang: Die
/// Komponentenwahl holt einen anderen Positionssatz, die Vergütungswahl schreibt über
/// die Hülle und baut das Reiterblatt neu auf (und öffnet dabei je nach Wahl den
/// Vergütungsdialog). Dieselbe Begründung trägt schon das Wahlfeld <c>variante</c> aus
/// der Welle KI‑F4. Der Assistent NENNT beide samt ihren Alternativen und lehnt das
/// Setzen benannt ab. Gefehlt hatte ohnehin nicht das Setzen, sondern die Auskunft:
/// „woran arbeitet der Anwender gerade?"</para>
///
/// <para><b>Das PV‑PROJEKT ist setzbar</b>: Es wählt nur das Ziel des Knopfes
/// „PV‑Vergütungsdialog öffnen…" und lädt nichts nach. Es steht allein im
/// Admin-Kontext zur Wahl; im Projektmodus zeigt die Maske die Liste nicht (VV‑Q7),
/// und dann liefert <see cref="PvProjektWahl"/> keine Einträge — der Assistent lehnt
/// mit der Begründung ab, dass es dort nichts zu wählen gibt.</para>
/// </summary>
public sealed class KostenKomponenteKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Der lebende Arbeitsstand der Maske.</summary>
    public Func<KostenKomponenteStand>? Standquelle { get; init; }

    /// <summary>Die gewählte Komponente der Kontextleiste.</summary>
    public Func<int?>? KomponenteLesen { get; init; }

    /// <summary>Die Einträge der Komponenten-Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KomponenteEintraege { get; init; }

    /// <summary>
    /// Die PV‑Vergütungswahl des Reiters „Ertrag": 0 = vom Stammprojekt übernehmen,
    /// 1 = eigene Vergütung; <c>null</c> = der Reiter steht nicht.
    /// </summary>
    public Func<int?>? PvVerguetungLesen { get; init; }

    /// <summary>Die zwei Einträge der Vergütungswahl.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? PvVerguetungEintraege { get; init; }

    /// <summary>Das gewählte Stammprojekt des Reiters „Ertrag".</summary>
    public Func<int?>? PvProjektLesen { get; init; }

    /// <summary>Der Schreibweg der Projektwahl — derselbe wie ein Griff in die Liste.</summary>
    public Action<int?>? PvProjektSetzen { get; init; }

    /// <summary>Die wählbaren Stammprojekte; leer, wo die Maske die Liste nicht zeigt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? PvProjektEintraege { get; init; }

    private KostenKomponenteStand Stand => Standquelle?.Invoke() ?? LEER;

    private static readonly KostenKomponenteStand LEER = new();

    // =====================================================================
    //  Woran der Anwender gerade arbeitet — unverändert aus dem Stand
    // =====================================================================

    /// <summary>Überschrift der Maske.</summary>
    public string Titel => Stand.Titel;

    /// <summary>Unterzeile der Maske.</summary>
    public string Untertitel => Stand.Untertitel;

    /// <summary>Die gewählte Vorlage ist eine Auslieferungsvorlage.</summary>
    public bool NurLesen => Stand.NurLesen;

    /// <summary>Die gewählte Variante der Vorlage (Welle KI‑F4).</summary>
    public int? VarianteId => Stand.VarianteId;

    // =====================================================================
    //  Das Raster — dieselbe LEBENDE Liste, nicht eine Kopie
    // =====================================================================

    /// <summary>
    /// Die Positionen in Anzeigereihenfolge. Der Katalog löst daraus je vorhandener
    /// Zeile ein Feld auf und holt die Liste bei jedem Zugriff neu — der Anwender legt
    /// Positionen an und löscht sie, während der Dialog steht.
    /// </summary>
    public IReadOnlyList<KostenPositionZeile> Zeilen => Stand.Zeilen;

    /// <summary>
    /// Die wählbaren Bemessungen der Spalte <c>BemessungId</c> — die Anmeldung findet
    /// sie über die Namenskonvention <c>&lt;Eigenschaft&gt;Wahl</c> am Daten-Objekt und
    /// nicht am Zeilentyp; deshalb steht sie hier und nicht an der Zeile.
    /// </summary>
    public IReadOnlyList<KiWahleintrag> BemessungIdWahl => Stand.BemessungIdWahl;

    // =====================================================================
    //  Die drei Lücken der Welle KI-F4
    // =====================================================================

    /// <summary>
    /// Die gewählte KOMPONENTE der Kontextleiste (Wärmepumpe, BHKW, …) — nur lesbar,
    /// weil ein Wechsel einen anderen Positionssatz nachlädt.
    /// </summary>
    public int? Komponentenwahl => KomponenteLesen?.Invoke();

    /// <summary>Die Einträge der Komponenten-Klappliste (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KomponentenwahlWahl
        => KomponenteEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Die PV‑VERGÜTUNGSWAHL des Reiters „Ertrag" — nur lesbar, weil ein Wechsel über
    /// die Hülle schreibt und das Reiterblatt neu aufbaut.
    /// </summary>
    public int? PvVerguetung => PvVerguetungLesen?.Invoke();

    /// <summary>Die zwei Einträge der Vergütungswahl (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> PvVerguetungWahl
        => PvVerguetungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Das gewählte STAMMPROJEKT des Reiters „Ertrag" — das Ziel des Knopfes
    /// „PV‑Vergütungsdialog öffnen…". Setzbar: Es lädt nichts nach.
    /// </summary>
    public int? PvProjekt
    {
        get => PvProjektLesen?.Invoke();
        set => PvProjektSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die wählbaren Stammprojekte (KI‑D‑Q6) — LEER, wo die Maske die Liste nicht
    /// zeigt: Im Projektmodus steht das Ziel fest (VV‑Q7).
    /// </summary>
    public IReadOnlyList<KiWahleintrag> PvProjektWahl
        => PvProjektEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();
}
