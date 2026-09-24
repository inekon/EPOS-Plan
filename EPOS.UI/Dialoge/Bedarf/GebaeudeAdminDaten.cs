using EPOS.UI.Bausteine;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Ein Gebäudesatz im Stammblatt der Verwaltung</b> (Konzept Administrationsdialoge,
/// Stufe 5, V16; Bestand A9; Welle #465) — das plattformfreie Abbild eines Satzes aus
/// <c>Tab_Gebaeude_STAMM</c>.
///
/// <para><b>Zwei Teile.</b> Der FELDSATZ (<see cref="Feldsatz"/>) ist der Satz des
/// Katalogeditors — das Stammblatt bearbeitet ihn über denselben Arbeitsstand
/// (<see cref="GebaeudeArbeitsstand"/>), prüft ihn mit denselben Regeln und schreibt ihn
/// über denselben Weg. Die übrigen Eigenschaften sind ANZEIGE: Kopf, Kennzahlen, Vergleich
/// und der Lesemodus eines Auslieferungssatzes (Hülle und „Alle Daten" als fertige
/// Name-Wert-Paare aus dem Kern).</para>
/// </summary>
public sealed class GebaeudeStammblattDaten
{
    /// <summary>Der Primärschlüssel (<c>Tab_Gebaeude_STAMM.ID</c>).</summary>
    public int Id { get; set; }

    /// <summary>Der Bezeichner — der Schlüssel jeder Aktion.</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Gebäudetyp (<c>Typ</c>) — der Name der Tagesverteilung (Gebäudetypen).</summary>
    public string Typ { get; set; } = "";

    /// <summary>Die Gebäudeart (<c>Gebaeudeart</c>).</summary>
    public string Gebaeudeart { get; set; } = "";

    /// <summary>
    /// Die Verwendung als STEUERWERT (<c>Wohngebaeude</c> / <c>Nicht Wohngebaeude</c>) — er
    /// wird nie übersetzt; die Anzeige kommt aus den Einträgen des Wirts.
    /// </summary>
    public string Verwendung { get; set; } = "";

    /// <summary>Der Index der Baualtersklasse (0 = „A"); <c>null</c> = keine gespeichert.</summary>
    public int? Baualtersklasse { get; set; }

    /// <summary>Die Beschreibung.</summary>
    public string Beschreibung { get; set; } = "";

    /// <summary>Die Wohn- bzw. Nutzfläche in m²; <c>null</c> = nicht gepflegt.</summary>
    public double? Wohnflaeche { get; set; }

    /// <summary>Der Wärmeleitwert H_ges in W/K; <c>null</c> = nicht bekannt.</summary>
    public double? HgesWK { get; set; }

    /// <summary>Der Rechenweg als Anzeigetext („VDI 6007", „Tagesbilanz (Bestandsweg)").</summary>
    public string Rechenweg { get; set; } = "";

    /// <summary>Ein Auslieferungssatz (<c>ReadOnly</c>) — nur lesbar, Duplizieren erlaubt.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Die Gruppe „Hülle" im Lesemodus: je Bauteil Fläche und U-Wert, fertig formatiert.</summary>
    public IReadOnlyList<Stammblattwert> Huelle { get; set; } = Stammblattwert.Keine;

    /// <summary>„Alle Daten" im Lesemodus: die übrigen Felder des Katalogeditors, mit Abschnitten.</summary>
    public IReadOnlyList<Stammblattwert> AlleDaten { get; set; } = Stammblattwert.Keine;

    /// <summary>
    /// <b>Der Feldsatz des Katalogeditors</b> (<c>GebaeudeKatalogHuelle.AusModell</c>, #465)
    /// — der Ausgangspunkt des Arbeitsstands im Stammblatt; <c>null</c> = der Wirt reicht
    /// keinen herein, dann entsteht er aus den Kenndaten oben (<see cref="FeldsatzOderKenndaten"/>).
    /// </summary>
    public GebaeudeKatalogDaten? Feldsatz { get; set; }

    /// <summary>
    /// Der Feldsatz — oder, ohne ihn, einer aus den Kenndaten dieses Satzes (Name, Typ,
    /// Gebäudeart, Verwendung, Baujahr, Beschreibung, Fläche); die übrigen Felder bleiben
    /// leer, und die Prüfung meldet sie beim Speichern.
    /// </summary>
    public GebaeudeKatalogDaten FeldsatzOderKenndaten() => Feldsatz ?? new GebaeudeKatalogDaten
    {
        Name = Name,
        Typ = Typ,
        Gebaeudeart = Gebaeudeart,
        Verwendung = Verwendung,
        Baualtersklasse = Baualtersklasse ?? 0,
        Beschreibung = Beschreibung,
        WohnflaecheGesamt = Wohnflaeche
    };
}
