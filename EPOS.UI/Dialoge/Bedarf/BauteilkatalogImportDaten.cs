using System.Collections.Generic;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>Eine Zeile der Vorschau: die Zahlen einer Satzart.</summary>
public sealed record BauteilkatalogVorschauZeile(string Satzart, int Neu, int MitKatalog, int InQuelle, int Wiedererkannt, int Abgelehnt);

/// <summary>
/// Die <b>Vorschau</b> des Bauteilkatalog-Imports (BK-3) für den <see cref="BauteilkatalogImportDialog"/> — ohne
/// Fachklasse des Kerns; die Hülle übersetzt. <see cref="Abgelehnt"/> gesetzt heißt: die ganze Quelle taugt nicht.
/// </summary>
public sealed record BauteilkatalogVorschauDaten(
    string? Abgelehnt,
    string Quelle,
    IReadOnlyList<BauteilkatalogVorschauZeile> Zeilen,
    int Gekennzeichnet,
    int SchichtenMitBaustoff,
    IReadOnlyList<string> Gruende,
    IReadOnlyList<string> CodesOhneDeutung,
    string Richtung,
    IReadOnlyList<string> Protokoll);

/// <summary>Das Ergebnis von „Übernehmen“: geschrieben oder nicht, und die Meldung mit Zahlen.</summary>
public sealed record BauteilkatalogImportErgebnis(bool Erfolg, string Meldung);

/// <summary>
/// Die Anzeigetexte des <see cref="BauteilkatalogImportDialog"/> — EIN Parameter statt vieler; jede Eigenschaft nennt
/// ihren Ressourcenschlüssel.
/// </summary>
public sealed class BauteilkatalogImportTexte
{
    /// <summary><c>BKI_TITEL</c></summary>
    public string Titel { get; set; } = Resource.BKI_TITEL;
    /// <summary><c>BKI_DATEI</c></summary>
    public string Datei { get; set; } = Resource.BKI_DATEI;
    /// <summary><c>BKI_DURCHSUCHEN</c></summary>
    public string Durchsuchen { get; set; } = Resource.BKI_DURCHSUCHEN;
    /// <summary><c>BKI_HINWEIS</c></summary>
    public string Hinweis { get; set; } = Resource.BKI_HINWEIS;
    /// <summary><c>BKI_FILTER</c></summary>
    public string Filter { get; set; } = Resource.BKI_FILTER;
    /// <summary><c>BKI_VORSCHAU</c></summary>
    public string Vorschau { get; set; } = Resource.BKI_VORSCHAU;
    /// <summary><c>BKI_SP_SATZART</c></summary>
    public string SpalteSatzart { get; set; } = Resource.BKI_SP_SATZART;
    /// <summary><c>BKI_SP_NEU</c></summary>
    public string SpalteNeu { get; set; } = Resource.BKI_SP_NEU;
    /// <summary><c>BKI_SP_KATALOG</c></summary>
    public string SpalteKatalog { get; set; } = Resource.BKI_SP_KATALOG;
    /// <summary><c>BKI_SP_QUELLE</c></summary>
    public string SpalteQuelle { get; set; } = Resource.BKI_SP_QUELLE;
    /// <summary><c>BKI_SP_WIEDER</c></summary>
    public string SpalteWieder { get; set; } = Resource.BKI_SP_WIEDER;
    /// <summary><c>BKI_SP_ABGELEHNT</c></summary>
    public string SpalteAbgelehnt { get; set; } = Resource.BKI_SP_ABGELEHNT;
    /// <summary><c>BKI_GEKENNZEICHNET</c> mit {0} = Zahl.</summary>
    public string Gekennzeichnet { get; set; } = Resource.BKI_GEKENNZEICHNET;
    /// <summary><c>BKI_MIT_BAUSTOFF</c> mit {0} = Zahl.</summary>
    public string MitBaustoff { get; set; } = Resource.BKI_MIT_BAUSTOFF;
    /// <summary><c>BKI_CODES</c></summary>
    public string Codes { get; set; } = Resource.BKI_CODES;
    /// <summary><c>BKI_GRUENDE</c></summary>
    public string Gruende { get; set; } = Resource.BKI_GRUENDE;
    /// <summary><c>BKI_PROTOKOLL</c></summary>
    public string Protokoll { get; set; } = Resource.BKI_PROTOKOLL;
    /// <summary><c>BKI_QUELLE</c> mit {0} = Quellangabe.</summary>
    public string Quelle { get; set; } = Resource.BKI_QUELLE;
    /// <summary><c>BKI_UEBERNEHMEN</c></summary>
    public string Uebernehmen { get; set; } = Resource.BKI_UEBERNEHMEN;
    /// <summary><c>BKI_SCHLIESSEN</c></summary>
    public string Schliessen { get; set; } = Resource.BKI_SCHLIESSEN;
    /// <summary><c>BKI_OK_SPERRE</c></summary>
    public string OkSperre { get; set; } = Resource.BKI_OK_SPERRE;
    /// <summary><c>BKI_ABGELEHNT</c> mit {0} = Grund.</summary>
    public string Abgelehnt { get; set; } = Resource.BKI_ABGELEHNT;
}
