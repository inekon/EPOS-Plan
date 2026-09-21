using System.Globalization;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild der Maske „BHKW-Wirtschaftlichkeit" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Zwei Arbeitsstände, ein Feldsatz.</b> Oben die Angaben der GEWÄHLTEN
/// Anlage (<c>BhkwAnlagenstand</c>), darunter die projektweiten Vorgaben zu KWKG,
/// Energie- und Stromsteuer (<c>BhkwVorgabenstand</c>). Beide sind veränderliche
/// Klassen mit öffentlichen FELDERN; die Maskenbrücke löst über Eigenschaften auf,
/// und ein Katalog an einem von ihnen verschwiege den anderen. Die Feldnamen
/// trennen sie deshalb mit den Vorsilben <c>anlage_</c> und <c>projekt_</c>.</para>
///
/// <para><b>Die Wahlfelder tragen ihren STEUERWERT als Schlüssel</b> (KI‑D‑Q6) und
/// nicht den Listenplatz: Der Steuerwert steht in der Datenbank und in
/// <c>DbWerte</c>, der Listenplatz nur in der offenen Maske.</para>
///
/// <para><b>Geschrieben wird nichts.</b> Der Dialog lässt Anlagen und Parametersatz
/// bis zum OK unangetastet und führt bis dahin nur seine Arbeitsstände — die Sicht
/// schreibt in genau diese.</para>
/// </summary>
public sealed class BhkwWirtschaftlichkeitKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Der Arbeitsstand der gewählten Anlage; <c>null</c> = keine gewählt.</summary>
    public Func<BhkwAnlagenstand?>? AnlageLesen { get; init; }

    /// <summary>Der Arbeitsstand der Projektvorgaben.</summary>
    public Func<BhkwVorgabenstand?>? VorgabenLesen { get; init; }

    public Func<int?>? WahlLesen { get; init; }
    public Action<int?>? WahlSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AnlagenEintraege { get; init; }

    public Func<IReadOnlyList<KiWahleintrag>>? AnlagenartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? EigenfallEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? EnergiesteuerAEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AufteilungAEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? EnergiesteuerPEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AufteilungPEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? UnternehmensartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? BefreiungsmodusEintraege { get; init; }

    /// <summary>Die Maske merkt sich die Änderung und zeichnet neu.</summary>
    public Action? Nachziehen { get; init; }

    private BhkwAnlagenstand? A => AnlageLesen?.Invoke();
    private BhkwVorgabenstand? V => VorgabenLesen?.Invoke();

    private void Anlage(Action<BhkwAnlagenstand> schritt)
    {
        BhkwAnlagenstand? stand = A;
        if (stand is null) return;
        schritt(stand);
        Nachziehen?.Invoke();
    }

    private void Projekt(Action<BhkwVorgabenstand> schritt)
    {
        BhkwVorgabenstand? stand = V;
        if (stand is null) return;
        schritt(stand);
        Nachziehen?.Invoke();
    }

    private static string Tag(DateTime? wert)
        => wert?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    private static DateTime? Datum(string? wert)
    {
        if (string.IsNullOrWhiteSpace(wert)) return null;
        return DateTime.TryParse(wert, CultureInfo.InvariantCulture,
                                 DateTimeStyles.None, out DateTime d)
            || DateTime.TryParse(wert, CultureInfo.CurrentCulture,
                                 DateTimeStyles.None, out d)
                   ? d
                   : null;
    }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Die BHKW-Module des Projekts — Schlüssel ist ihr Listenplatz.</summary>
    public IReadOnlyList<KiWahleintrag> ModulWahl
        => AnlagenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> AnlageAnlagenartWahl
        => AnlagenartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> AnlageEigenfallWahl
        => EigenfallEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> AnlageEnergiesteuerWahl
        => EnergiesteuerAEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> AnlageAufteilungWahl
        => AufteilungAEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> ProjektEnergiesteuerWahl
        => EnergiesteuerPEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> ProjektAufteilungWahl
        => AufteilungPEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> ProjektUnternehmensartWahl
        => UnternehmensartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    public IReadOnlyList<KiWahleintrag> ProjektBefreiungsmodusWahl
        => BefreiungsmodusEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die gewählte Anlage
    // =====================================================================

    /// <summary>Das BHKW-Modul, dessen Angaben darunter stehen.</summary>
    public int? Modul
    {
        get => WahlLesen?.Invoke();
        set => WahlSetzen?.Invoke(value);
    }

    /// <summary>Der Stichtag der Bestellung oder Genehmigung (ISO).</summary>
    public string AnlageStichtag
    {
        get => Tag(A?.Stichtag);
        set => Anlage(s => s.Stichtag = Datum(value));
    }

    /// <summary>Die Inbetriebnahme der Anlage (ISO).</summary>
    public string AnlageInbetriebnahme
    {
        get => Tag(A?.Inbetriebnahme);
        set => Anlage(s => s.Inbetriebnahme = Datum(value));
    }

    /// <summary>Die Anlagenart nach KWKG.</summary>
    public string AnlageAnlagenart
    {
        get => A?.Anlagenart ?? "";
        set => Anlage(s => s.Anlagenart = value ?? "");
    }

    /// <summary>Der Eigenstromfall nach § 6 Abs. 3 KWKG.</summary>
    public string AnlageEigenfall
    {
        get => A?.Eigenfall ?? "";
        set => Anlage(s => s.Eigenfall = value ?? "");
    }

    /// <summary>Der KWKG-Zuschlag auf eingespeisten Strom; 0 = kein eigener Satz.</summary>
    public double? AnlageSatzEinspeisung
    {
        get => A?.SatzEinspCt;
        set => Anlage(s => s.SatzEinspCt = value);
    }

    /// <summary>Der KWKG-Zuschlag auf eigenverbrauchten Strom; 0 = kein eigener Satz.</summary>
    public double? AnlageSatzEigen
    {
        get => A?.SatzEigenCt;
        set => Anlage(s => s.SatzEigenCt = value);
    }

    /// <summary>Das Vollbenutzungsstunden-Kontingent; 0 = nach § 8 abgeleitet.</summary>
    public double? AnlageKontingent
    {
        get => A?.VbhKontingent;
        set => Anlage(s => s.VbhKontingent = value);
    }

    /// <summary>Der Vbh-Deckel je Jahr; 0 = degressive Staffel.</summary>
    public double? AnlageDeckel
    {
        get => A?.VbhDeckel;
        set => Anlage(s => s.VbhDeckel = value);
    }

    /// <summary>Der Kostenanteil der Anlage an der Gesamtinvestition.</summary>
    public double? AnlageKostenanteil
    {
        get => A?.Kostenanteil;
        set => Anlage(s => s.Kostenanteil = value);
    }

    /// <summary>Die Energiesteuerwahl DIESER Anlage.</summary>
    public string AnlageEnergiesteuer
    {
        get => A?.EnergiesteuerWahl ?? "";
        set => Anlage(s => s.EnergiesteuerWahl = value ?? "");
    }

    /// <summary>Die Aufteilungsmethode DIESER Anlage.</summary>
    public string AnlageAufteilung
    {
        get => A?.AufteilungMethode ?? "";
        set => Anlage(s => s.AufteilungMethode = value ?? "");
    }

    /// <summary>Der Hilfsenergieanteil der Anlage; 0 ist ein gültiger Wert.</summary>
    public double? AnlageHilfsenergie
    {
        get => A?.HilfsenergieAnteil;
        set => Anlage(s => s.HilfsenergieAnteil = value ?? 0);
    }

    // =====================================================================
    //  Die projektweiten Vorgaben
    // =====================================================================

    /// <summary>Die Einspeisevergütung für KWK-Strom; 0 = nicht gepflegt.</summary>
    public double? ProjektEinspeisungKwk
    {
        get => V?.EinspeiseverguetungKwk;
        set => Projekt(s => s.EinspeiseverguetungKwk = value);
    }

    /// <summary>Der Abschlag bei negativen Spotpreisen.</summary>
    public double ProjektAbschlag
    {
        get => V?.KwkgAbschlagNegativ ?? 0;
        set => Projekt(s => s.KwkgAbschlagNegativ = value);
    }

    /// <summary>Die Pauschale nach § 9 KWKG (nur bis 2 kWel, einmalig).</summary>
    public bool ProjektPauschalmodus
    {
        get => V?.KwkgPauschalmodus ?? false;
        set => Projekt(s => s.KwkgPauschalmodus = value);
    }

    /// <summary>Der projektweite KWKG-Stichtag (ISO).</summary>
    public string ProjektStichtag
    {
        get => Tag(V?.KwkgStichtag);
        set => Projekt(s => s.KwkgStichtag = Datum(value));
    }

    /// <summary>Der Förderbeginn — das Startjahr der KWKG-Reihen (ISO).</summary>
    public string ProjektInbetriebnahme
    {
        get => Tag(V?.KwkgInbetriebnahme);
        set => Projekt(s => s.KwkgInbetriebnahme = Datum(value));
    }

    /// <summary>Die Energiesteuerwahl des PROJEKTS.</summary>
    public string ProjektEnergiesteuer
    {
        get => V?.EnergiesteuerWahl ?? "";
        set => Projekt(s => s.EnergiesteuerWahl = value ?? "");
    }

    /// <summary>Die Aufteilungsmethode des PROJEKTS.</summary>
    public string ProjektAufteilung
    {
        get => V?.AufteilungMethode ?? "";
        set => Projekt(s => s.AufteilungMethode = value ?? "");
    }

    /// <summary>Der Jahresnutzungsgrad; 0 = nicht erfasst.</summary>
    public double? ProjektJahresnutzungsgrad
    {
        get => V?.Jahresnutzungsgrad;
        set => Projekt(s => s.Jahresnutzungsgrad = value);
    }

    /// <summary>Die Unternehmensart nach StromStG.</summary>
    public string ProjektUnternehmensart
    {
        get => V?.Unternehmensart ?? "";
        set => Projekt(s => s.Unternehmensart = value ?? "");
    }

    /// <summary>Ist der räumliche Zusammenhang (4,5 km) gegeben?</summary>
    public bool ProjektRaeumlich
    {
        get => V?.RaeumlicherZusammenhang ?? false;
        set => Projekt(s => s.RaeumlicherZusammenhang = value);
    }

    /// <summary>Liegt ein Hocheffizienznachweis vor?</summary>
    public bool ProjektHocheffizienz
    {
        get => V?.HocheffizienzNachweis ?? false;
        set => Projekt(s => s.HocheffizienzNachweis = value);
    }

    /// <summary>Der Modus des § 9 Abs. 1 Nr. 3 StromStG.</summary>
    public string ProjektBefreiungsmodus
    {
        get => V?.StromsteuerBefreiungModus ?? "";
        set => Projekt(s => s.StromsteuerBefreiungModus = value ?? "");
    }
}
