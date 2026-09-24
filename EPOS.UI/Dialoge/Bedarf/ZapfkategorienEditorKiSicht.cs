namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das flache Abbild der Überlagerung „Zapfkategorien und Streuung" für den Hilfe-Assistenten
/// (Umsetzungskonzept Zapfprofilgenerator 4.4, 5.3; Stufe Z4, Gruppe 2b).
///
/// <para><b>Eine ÜBERLAGERUNG, die zum Katalog gehört.</b> Ihr OK schreibt die Kategorien in
/// EINER Transaktion — das bleibt der Klick des Anwenders; einen Speicherweg meldet sie nicht
/// an.</para>
///
/// <para><b>Die Kategorien sind SPALTEN</b> (<see cref="Zeilen"/>) mit ihrem Namen als
/// Kennzeichen; gesetzt wird wie in den Feldern des Rasters, in deren Grenzen und nur, solange
/// die Zeilen bedienbar sind (eine gesperrte Nutzungsart erst nach „Als eigene Kopie
/// bearbeiten…"). Neu, Entfernen, Reihenfolge und „Vorgabesatz laden" bleiben Klicks des
/// Anwenders. Die Sicht hält keinen Zustand.</para>
/// </summary>
public sealed class ZapfkategorienEditorKiSicht
{
    /// <summary>Die Zeilen des Rasters — je Kategorie eine, frisch bei jedem Zugriff.</summary>
    public Func<IReadOnlyList<ZapfkategorienKiZeile>>? ZeilenLesen { get; init; }

    public Func<string>? KatalogversionLesen { get; init; }
    public Func<string, string?>? KatalogversionSetzen { get; init; }

    /// <summary>Die Kategorien der Nutzungsart.</summary>
    public IReadOnlyList<ZapfkategorienKiZeile> Zeilen => ZeilenLesen?.Invoke() ?? Array.Empty<ZapfkategorienKiZeile>();

    /// <summary>Die Katalogversion der eigenen Kopie — nur bei einer gesperrten Nutzungsart im Kopiermodus.</summary>
    public string Katalogversion
    {
        get => KatalogversionLesen?.Invoke() ?? "";
        set => ZapfprofilKiRegeln.Setze(KatalogversionSetzen, value);
    }
}

/// <summary>
/// Eine Zapfkategorie als ZEILE der Sichtklasse <see cref="ZapfkategorienEditorKiSicht"/>: Sie
/// liest die Zeile selbst und setzt über die Wege des Editors, die Bedienbarkeit und Grenzen
/// prüfen.
/// </summary>
public sealed class ZapfkategorienKiZeile
{
    private readonly ZapfprofilKategorieDaten _zeile;

    /// <summary>Legt die Zeile zu einer Zeile des Editors an.</summary>
    public ZapfkategorienKiZeile(ZapfprofilKategorieDaten zeile)
    {
        _zeile = zeile ?? throw new ArgumentNullException(nameof(zeile));
    }

    public Func<string, string?>? NameSetzen { get; init; }
    public Func<double?, string?>? VolumenstromSetzen { get; init; }
    public Func<int?, string?>? DauerSetzen { get; init; }
    public Func<double?, string?>? AnteilSetzen { get; init; }
    public Func<double?, string?>? StreuungSetzen { get; init; }
    public Func<double?, string?>? KappungSetzen { get; init; }

    /// <summary>Das ZEILENKENNZEICHEN: der Name der Kategorie.</summary>
    public string Kennzeichen => (_zeile.Name ?? "").Trim();

    /// <summary>Der Name der Kategorie — eindeutig in der Nutzungsart.</summary>
    public string Name
    {
        get => _zeile.Name ?? "";
        set => ZapfprofilKiRegeln.Setze(NameSetzen, value);
    }

    /// <summary>Der mittlere Volumenstrom μ [l/min].</summary>
    public double? Volumenstrom
    {
        get => _zeile.VolumenstromLJeMin;
        set => ZapfprofilKiRegeln.Setze(VolumenstromSetzen, value);
    }

    /// <summary>Die Dauer eines Ereignisses [min].</summary>
    public int? Dauer
    {
        get => _zeile.DauerMin;
        set => ZapfprofilKiRegeln.Setze(DauerSetzen, value);
    }

    /// <summary>Der Anteil an der Tagesmenge [-].</summary>
    public double? Anteil
    {
        get => _zeile.Anteil;
        set => ZapfprofilKiRegeln.Setze(AnteilSetzen, value);
    }

    /// <summary>Die Streuung σ [l/min].</summary>
    public double? Streuung
    {
        get => _zeile.StreuungLJeMin;
        set => ZapfprofilKiRegeln.Setze(StreuungSetzen, value);
    }

    /// <summary>Die obere Kappung [l/min]; leer = keine.</summary>
    public double? Kappung
    {
        get => _zeile.KappungLJeMin;
        set => ZapfprofilKiRegeln.Setze(KappungSetzen, value);
    }

    /// <summary>Die Herkunft als Kurztext (nur Anzeige).</summary>
    public string Herkunft => _zeile.Herkunft ?? "";
}
