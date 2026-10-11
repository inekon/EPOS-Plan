using System.Globalization;
using KiKern;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Die Sicht des Assistenten auf „Kältemaschinen im Projekt"</b> (KU3-4c; Feldkarte
/// <c>KiDialoge.KaeltemaschineAnlage</c>). Die Anlagenwahl nimmt den Weg eines Klicks in die Liste, die
/// Eingaben schreiben in den Arbeitsstand der gewählten Anlage — geschrieben wird erst mit „OK".
///
/// <para><b>Der Kühlträger ist eine Wahl über die Id des Trägers</b> (<see cref="KuehltraegerWahl"/>), nie über
/// den Anzeigetext; leer = Stromträger des Projekts. Der Hilfsstromanteil steht wie in der Maske in Prozent.
/// Die Prüfregeln sind die des Knopfes (<c>KaeltemaschineAnlageCtrl.Pruefen</c>).</para>
/// </summary>
public sealed class KaeltemaschineAnlageKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    /// <summary>Liest den Schlüssel der gewählten Anlage.</summary>
    public Func<string>? AnlageLesen { get; init; }

    /// <summary>Wählt eine Anlage wie ein Klick in die Liste.</summary>
    public Action<string>? AnlageSetzen { get; init; }

    /// <summary>Die Einträge der Anlagenwahl.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? AnlageEintraege { get; init; }

    /// <summary>Die Stromträger des Projekts (Id, Anzeigetext).</summary>
    public Func<IReadOnlyList<(int Id, string Text)>>? Stromtraeger { get; init; }

    /// <summary>Der Arbeitsstand der gewählten Anlage; <c>null</c> ohne Wahl.</summary>
    public Func<KaeltemaschineAnlageDaten?>? ArbeitLesen { get; init; }

    /// <summary>Nach jeder Änderung — der Dialog zeichnet neu.</summary>
    public Action? Geaendert { get; init; }

    // =====================================================================
    //  Anlagenwahl
    // =====================================================================

    /// <summary>Die Wahl der Anlage (Schlüssel der Listenzeile).</summary>
    public string Anlage
    {
        get => AnlageLesen?.Invoke() ?? "";
        set => AnlageSetzen?.Invoke(value ?? "");
    }

    /// <summary>Begleiteigenschaft der Anlagenwahl.</summary>
    public IReadOnlyList<KiWahleintrag> AnlageWahl => AnlageEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Eingaben
    // =====================================================================

    public string Bezeichner
    {
        get => ArbeitLesen?.Invoke()?.Bezeichner ?? "";
        set => Setzen(d => d.Bezeichner = value ?? "");
    }

    public int? Anzahl { get => ArbeitLesen?.Invoke()?.Anzahl; set => Setzen(d => d.Anzahl = value); }

    public double? KuehlVorlauf { get => ArbeitLesen?.Invoke()?.KuehlVorlauf; set => Setzen(d => d.KuehlVorlauf = value); }

    /// <summary>Der Hilfsstromanteil in Prozent — gespeichert wird der Anteil [—].</summary>
    public double? Hilfsstrom
    {
        get => KaeltemaschineAnlageDialog.AnteilAlsProzent(ArbeitLesen?.Invoke()?.KuehlHilfsstromanteil);
        set => Setzen(d => d.KuehlHilfsstromanteil = KaeltemaschineAnlageDialog.ProzentAlsAnteil(value));
    }

    /// <summary>Die Id des Kühlträgers als Schlüssel; leer = Stromträger des Projekts.</summary>
    public string Kuehltraeger
    {
        get => ArbeitLesen?.Invoke()?.KuehlCarrierId?.ToString(CultureInfo.InvariantCulture) ?? "";
        set
        {
            int? id = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                      && (Stromtraeger?.Invoke() ?? Array.Empty<(int, string)>()).Any(t => t.Id == n) ? n : null;
            Setzen(d => d.KuehlCarrierId = id);
        }
    }

    /// <summary>Begleiteigenschaft des Kühlträgers: Id und Anzeigetext.</summary>
    public IReadOnlyList<KiWahleintrag> KuehltraegerWahl
        => (Stromtraeger?.Invoke() ?? Array.Empty<(int, string)>())
           .Select(t => new KiWahleintrag(t.Id.ToString(CultureInfo.InvariantCulture), t.Text))
           .ToList();

    public bool EigenerZaehler
    {
        get => ArbeitLesen?.Invoke()?.KuehlEigenerZaehler ?? false;
        set => Setzen(d => d.KuehlEigenerZaehler = value);
    }

    // =====================================================================
    //  KM3-E3-b: die Lesewerte der Teillastrechnung (nur zu lesen)
    // =====================================================================

    /// <summary>Teillastrechnung der Projektkopie samt Herkunft der Kurve (Spalte <c>Teillast_Weg</c>).</summary>
    public string TeillastWeg => ArbeitLesen?.Invoke()?.Geraet?.TeillastWeg ?? "";

    /// <summary>Verdichterregelung der Projektkopie (Spalte <c>Verdichterregelung</c>).</summary>
    public string Verdichterregelung => ArbeitLesen?.Invoke()?.Geraet?.Verdichterregelung ?? "";

    /// <summary>Taktverlustfaktor C_d der Projektkopie mit Herkunft (Spalte <c>Taktverlustfaktor_Cd</c>).</summary>
    public string TaktverlustfaktorCd => ArbeitLesen?.Invoke()?.Geraet?.Taktverlustfaktor ?? "";

    /// <summary>Weg am Kennfeldrand (Spalte <c>Kennfeld_Randweg</c>).</summary>
    public string KennfeldRandweg => ArbeitLesen?.Invoke()?.Geraet?.Kennfeldrand ?? "";

    private void Setzen(Action<KaeltemaschineAnlageDaten> schritt)
    {
        KaeltemaschineAnlageDaten? d = ArbeitLesen?.Invoke();
        if (d is null) return;
        schritt(d);
        Geaendert?.Invoke();
    }
}
