using EPOS.UI.Bausteine;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Ein Gebäudesatz im Stammblatt der Verwaltung</b> (Konzept Administrationsdialoge,
/// Stufe 5, V16; Bestand A9) — das plattformfreie Abbild eines Satzes aus
/// <c>Tab_Gebaeude_STAMM</c>, so weit das Stammblatt ihn zeigt.
///
/// <para><b>Drei Teile.</b> Die KENNDATEN (Gebäudetyp, Gebäudeart, Verwendung, Baujahr,
/// Beschreibung) sind im Stammblatt direkt bedienbar; Name und Wohnfläche sind Lesewerte —
/// umbenannt wird über „Duplizieren…", die Wohnfläche hängt über die Bauweise an der Bauart
/// und bleibt dem Katalogeditor. Die HÜLLE (Flächen und U-Werte der vier Bauteile) und
/// „ALLE DATEN" stehen als fertige Name-Wert-Paare da; sie baut die Hülle aus dem Kern.</para>
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

    /// <summary>Die Gruppe „Hülle": je Bauteil Fläche und U-Wert, fertig formatiert.</summary>
    public IReadOnlyList<Stammblattwert> Huelle { get; set; } = Stammblattwert.Keine;

    /// <summary>„Alle Daten": die übrigen Felder des Katalogeditors, mit Abschnitten.</summary>
    public IReadOnlyList<Stammblattwert> AlleDaten { get; set; } = Stammblattwert.Keine;
}

/// <summary>
/// <b>Was „Speichern" im Stammblatt der Gebäudeverwaltung schreibt</b> (Stufe 5) — genau die
/// fünf direkt bedienbaren Kenndaten des Satzes <paramref name="Name"/>.
/// </summary>
/// <param name="Name">Der Bezeichner des Satzes (nicht änderbar).</param>
/// <param name="Typ">Der Gebäudetyp.</param>
/// <param name="Gebaeudeart">Die Gebäudeart.</param>
/// <param name="Verwendung">Die Verwendung als Steuerwert.</param>
/// <param name="Baualtersklasse">Der Index der Baualtersklasse; <c>null</c> = keine.</param>
/// <param name="Beschreibung">Die Beschreibung.</param>
public sealed record GebaeudeKenndaten(string Name, string Typ, string Gebaeudeart, string Verwendung,
                                       int? Baualtersklasse, string Beschreibung);
