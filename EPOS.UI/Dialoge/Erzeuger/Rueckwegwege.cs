using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>Warum „Ursprung überschreiben" ausgegraut ist (Konzept Katalogauswahl 5.2 Nr. 2); <see cref="Keine"/> = möglich.</summary>
public enum Rueckwegsperre
{
    Keine,
    /// <summary>„Erst Schloss aufheben" — der Ursprung ist gesperrt.</summary>
    Gesperrt,
    /// <summary>„Ursprung nicht mehr vorhanden" — der Verweis zeigt ins Leere.</summary>
    UrsprungFehlt,
    /// <summary>„Ursprung nicht bekannt" — die Kopie hat keinen Verweis.</summary>
    UrsprungUnbekannt,
}

/// <summary>Eine Zeile der Rückfrage, wie die Hülle sie aus dem Kern baut.</summary>
public sealed record Rueckwegvorschlag(int Id, string NameKopie, string NameUrsprung, Rueckwegsperre Sperre,
                                      string Namensvorschlag);

/// <summary>Die Wahl einer Zeile: überschreiben oder neu unter <see cref="Name"/>.</summary>
public sealed record Rueckwegwahl(int Id, bool Ueberschreiben, string Name);

/// <summary>
/// <b>Die Wege des Rückwegs „In die Datenbank übernehmen…"</b> (KA‑E‑9) — die Hülle reicht sie aus dem Kernweg
/// (<c>HeizkesselStammCtrl.RueckwegVorschau</c>, <c>RueckwegNameBelegt</c>, <c>AusProjektUebernehmen</c>); die
/// Oberfläche fragt nur.
/// </summary>
public sealed class Rueckwegwege
{
    /// <summary>Die Zeilen der Rückfrage zu den Projektkopien (je Gerät einmal).</summary>
    public required Func<IReadOnlyList<int>, IReadOnlyList<Rueckwegvorschlag>> Vorschau { get; init; }
    /// <summary>Ist der Name im Katalog vergeben?</summary>
    public required Func<string, bool> NameBelegt { get; init; }
    /// <summary>Schreibt alle Wahlen in EINEM Vorgang — alle oder keine.</summary>
    public required Func<IReadOnlyList<Rueckwegwahl>, KatalogSpeicherErgebnis> Uebernehmen { get; init; }
}

/// <summary>Die Regeln der Rückfrage ohne Komponente (prüfbar).</summary>
public static class Rueckwegregel
{
    /// <summary>Der Grund, warum „Ursprung überschreiben" ausgegraut ist; leer = möglich.</summary>
    public static string Grund(Rueckwegsperre s) => s switch
    {
        Rueckwegsperre.Gesperrt => Resource.KATRUECK_GRUND_GESPERRT,
        Rueckwegsperre.UrsprungFehlt => Resource.KATRUECK_GRUND_FEHLT,
        Rueckwegsperre.UrsprungUnbekannt => Resource.KATRUECK_GRUND_UNBEKANNT,
        _ => "",
    };

    /// <summary>
    /// Der Hinweis am Namensfeld einer „neu"-Zeile: leer, im Katalog vergeben oder zweimal gewählt; leer = in Ordnung.
    /// </summary>
    public static string Namenshinweis(string name, IEnumerable<string> andereNamen, Func<string, bool> belegt)
    {
        string n = (name ?? "").Trim();
        if (n.Length == 0) return Resource.KATRUECK_HINWEIS_NAME_LEER;
        if (andereNamen.Any(a => string.Equals((a ?? "").Trim(), n, StringComparison.Ordinal)))
            return Resource.KATRUECK_HINWEIS_NAME_DOPPELT;
        return belegt(n) ? Resource.KATRUECK_HINWEIS_NAME_BELEGT : "";
    }

    /// <summary>Der Titel der Überlagerung.</summary>
    public static string Titel(IReadOnlyList<Rueckwegvorschlag> zeilen)
        => zeilen.Count == 1
            ? string.Format(Resource.Culture, Resource.KATRUECK_TITEL, zeilen[0].NameKopie)
            : string.Format(Resource.Culture, Resource.KATRUECK_TITEL_N, zeilen.Count);
}
