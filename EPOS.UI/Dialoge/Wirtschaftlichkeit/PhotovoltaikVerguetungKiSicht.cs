using System.Globalization;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Wirtschaftlichkeit;

/// <summary>
/// Das FLACHE Abbild der Maske „Photovoltaik-Vergütung" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Warum ein Sichtmodell, obwohl die Maske ein Daten-Objekt hat.</b> Sie
/// bindet an <c>ProjektPhotovoltaikModel</c> — aber nicht unmittelbar: Leistung,
/// Inbetriebnahme, Degradation, Einspeiseart und Vermarktungsform laufen über
/// eigene Wege, die die ZULÄSSIGKEIT neu prüfen und eine 0 als „nicht gepflegt"
/// führen. Ein Katalog am Modell schriebe an diesen Wegen vorbei; die Maske zeigte
/// dann eine Leistung von 0 statt „keine" und ihre Warnungen von vorhin.</para>
///
/// <para><b>Drei WAHLFELDER tragen ihren Steuerwert</b> (KI‑D‑Q6): Einspeiseart,
/// Vermarktungsform und die zwei Auto/Ja/Nein-Schalter der §-51- und der
/// Kappungsgruppe.</para>
///
/// <para><b>Draußen bleiben die Herleitungen</b> — die Vorschau, die
/// Kennzahlzeile, der Zulässigkeitsstatus und die Anlagenwarnungen: Sie stehen als
/// Text unter den Feldern, aus denen sie entstehen, und niemand tippt sie.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten.</para>
/// </summary>
public sealed class PhotovoltaikVerguetungKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Das Vergütungsmodell des Projekts.</summary>
    public Func<ProjektPhotovoltaikModel?>? ModellLesen { get; init; }

    public Action<double?>? LeistungSetzen { get; init; }
    public Action<double?>? DegradationSetzen { get; init; }
    public Action<string>? IbnSetzen { get; init; }
    public Action<string>? EinspeiseartSetzen { get; init; }
    public Action<string>? VermarktungSetzen { get; init; }

    public Func<IReadOnlyList<KiWahleintrag>>? EinspeiseartEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? VermarktungEintraege { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? SchalterEintraege { get; init; }

    /// <summary>Die Maske zeichnet neu — nach jedem Setzen.</summary>
    public Action? Nachziehen { get; init; }

    private ProjektPhotovoltaikModel? M => ModellLesen?.Invoke();

    private void Setze(Action<ProjektPhotovoltaikModel> schritt)
    {
        ProjektPhotovoltaikModel? modell = M;
        if (modell is null) return;
        schritt(modell);
        Nachziehen?.Invoke();
    }

    // =====================================================================
    //  Die Wahllisten (KI-D-Q6)
    // =====================================================================

    /// <summary>Überschuss- oder Volleinspeisung — Schlüssel ist der Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> EinspeiseartWahl
        => EinspeiseartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die vier Vermarktungsformen — Schlüssel ist der Steuerwert.</summary>
    public IReadOnlyList<KiWahleintrag> VermarktungsformWahl
        => VermarktungEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Automatisch, ja oder nein — für § 51.</summary>
    public IReadOnlyList<KiWahleintrag> Paragraf51Wahl
        => SchalterEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Automatisch, ja oder nein — für die 60-Prozent-Kappung.</summary>
    public IReadOnlyList<KiWahleintrag> KappungWahl
        => SchalterEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Wird die Vergütungsrechnung überhaupt angewendet?</summary>
    public bool Aktiv
    {
        get => M?.Aktiv ?? false;
        set => Setze(m => m.Aktiv = value);
    }

    /// <summary>
    /// Die gesetzte Anlagenleistung; 0 = keine, dann gilt die rechnerische Leistung
    /// aus den Anlagen des Projekts.
    /// </summary>
    public double? Leistung
    {
        get => M?.KwpOverride;
        set { LeistungSetzen?.Invoke(value); Nachziehen?.Invoke(); }
    }

    /// <summary>Die Inbetriebnahme der Anlage (ISO) — Pflicht, wenn die Rechnung gilt.</summary>
    public string Inbetriebnahme
    {
        get
        {
            DateTime? tag = M?.Inbetriebnahme;
            return tag is null || tag.Value <= DateTime.MinValue
                       ? ""
                       : tag.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        set { IbnSetzen?.Invoke(value ?? ""); Nachziehen?.Invoke(); }
    }

    /// <summary>Die jährliche Degradation der Module; 0 = keine.</summary>
    public double? Degradation
    {
        get => M?.Degradation;
        set { DegradationSetzen?.Invoke(value); Nachziehen?.Invoke(); }
    }

    /// <summary>Überschuss- oder Volleinspeisung.</summary>
    public string Einspeiseart
    {
        get => M?.Einspeiseart ?? "";
        set { EinspeiseartSetzen?.Invoke(value ?? ""); Nachziehen?.Invoke(); }
    }

    /// <summary>Der gesetzte anzulegende Wert; 0 = Satz aus dem Katalog.</summary>
    public double? AnzulegenderWert
    {
        get => M?.AwOverride;
        set => Setze(m => m.AwOverride = value > 0 ? value : null);
    }

    /// <summary>Die Vermarktungsform.</summary>
    public string Vermarktungsform
    {
        get => M?.Vermarktungsform ?? "";
        set { VermarktungSetzen?.Invoke(value ?? ""); Nachziehen?.Invoke(); }
    }

    /// <summary>Das Direktvermarktungsentgelt — nur bei Marktprämie wirksam.</summary>
    public double? Direktvermarktungsentgelt
    {
        get => M?.DvEntgelt;
        set => Setze(m => m.DvEntgelt = value);
    }

    /// <summary>Der PPA-Preis; 0 = keiner.</summary>
    public double? PpaPreis
    {
        get => M?.PpaPreis;
        set => Setze(m => m.PpaPreis = value > 0 ? value : null);
    }

    /// <summary>Der Auf- oder Abschlag auf den Spotpreis im PPA.</summary>
    public double? PpaSpotaufschlag
    {
        get => M?.PpaSpotAufschlag;
        set => Setze(m => m.PpaSpotAufschlag = value != 0 ? value : null);
    }

    /// <summary>Gilt § 51 (Erlösausfall bei negativen Preisen)? Auto, ja oder nein.</summary>
    public string Paragraf51
    {
        get => M?.Par51_Anwenden ?? "";
        set => Setze(m => m.Par51_Anwenden = value ?? "");
    }

    /// <summary>Das Einbaujahr des intelligenten Messsystems; 0 = keins.</summary>
    public int? Messsystemjahr
    {
        get => M?.IMSys_Einbaujahr;
        set => Setze(m => m.IMSys_Einbaujahr = value >= 2000 ? value : null);
    }

    /// <summary>Der angenommene Ausfallanteil der Erlöse.</summary>
    public double? Ausfallanteil
    {
        get => M?.AusfallanteilProzent;
        set => Setze(m => m.AusfallanteilProzent = value);
    }

    /// <summary>Wird der Ausfall nach § 51a durch Laufzeitverlängerung kompensiert?</summary>
    public bool Paragraf51aKompensation
    {
        get => M?.Par51a_Kompensieren ?? false;
        set => Setze(m => m.Par51a_Kompensieren = value);
    }

    /// <summary>Wird der Eigenverbrauch aus der Preisreihe bewertet?</summary>
    public bool BezugAusPreisreihe
    {
        get => M?.BezugAusPreisreihe ?? false;
        set => Setze(m => m.BezugAusPreisreihe = value);
    }

    /// <summary>Gilt die 60-Prozent-Kappung? Auto, ja oder nein.</summary>
    public string Kappung
    {
        get => M?.Kappung60_Anwenden ?? "";
        set => Setze(m => m.Kappung60_Anwenden = value ?? "");
    }
}
