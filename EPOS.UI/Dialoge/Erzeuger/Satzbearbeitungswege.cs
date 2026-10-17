using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Ein Satz der Satzbearbeitung „Bearbeiten…" (Konzept Projektdialoge mit Katalogauswahl,
/// 4.6): die Projektkopie bzw. der Katalogsatz, benannt über die ID der Tabelle.
/// </summary>
/// <param name="Id">ID der Projektkopie bzw. des Katalogsatzes.</param>
/// <param name="Name">Bezeichner für Blätterleiste und Hinweiszeile.</param>
/// <param name="Gesperrt">Trägt der Katalogsatz das Schloss? Bei Projektsätzen immer <c>false</c>.</param>
public sealed record Bearbeitungssatz(int Id, string Name, bool Gesperrt);

/// <summary>
/// <b>Die Wege der Satzbearbeitung eines Bereichs</b> — die Hülle reicht sie herein, die
/// Oberfläche hält keine Datenbank. <see cref="Speichern"/> schreibt ALLE geänderten Sätze in
/// einer Transaktion (alle oder keiner); ohne ihn ist die Bearbeitung reine Anzeige.
/// </summary>
public sealed class Satzbearbeitungswege
{
    /// <summary>Die Felder eines Satzes nach seiner ID (<c>null</c> = nicht gefunden).</summary>
    public required Func<int, IReadOnlyList<BrowserFeldwert>?> Lesen { get; init; }

    /// <summary>Schreibt die geänderten Sätze in einer Transaktion.</summary>
    public Func<IReadOnlyList<(int Id, IReadOnlyList<BrowserFeldwert> Felder)>, KatalogSpeicherErgebnis>? Speichern { get; init; }
}

/// <summary>Was die Satzbearbeitung aus einer Auswahl macht — die Regel ohne Oberfläche.</summary>
public static class Satzbearbeitungsregel
{
    /// <summary>
    /// Gesperrte Katalogsätze werden übersprungen; sind ALLE gesperrt, öffnet die Bearbeitung
    /// nur lesend mit allen. Projektsätze sind nie gesperrt.
    /// </summary>
    public static (IReadOnlyList<Bearbeitungssatz> Aktiv, IReadOnlyList<Bearbeitungssatz> Uebersprungen, bool NurLesen)
        Teilen(Satzmarke art, IReadOnlyList<Bearbeitungssatz> saetze)
    {
        ArgumentNullException.ThrowIfNull(saetze);
        if (art != Satzmarke.Katalogsatz) return (saetze, Array.Empty<Bearbeitungssatz>(), false);
        var gesperrt = saetze.Where(s => s.Gesperrt).ToList();
        if (gesperrt.Count == saetze.Count && saetze.Count > 0)
            return (saetze, Array.Empty<Bearbeitungssatz>(), true);
        return (saetze.Where(s => !s.Gesperrt).ToList(), gesperrt, false);
    }

    /// <summary>Der Titel der Überlagerung: „Projektsatz bearbeiten – Name" bzw. „3 Katalogsätze bearbeiten".</summary>
    public static string Titel(Satzmarke art, IReadOnlyList<Bearbeitungssatz> saetze)
    {
        var aktiv = Teilen(art, saetze).Aktiv;
        bool katalog = art == Satzmarke.Katalogsatz;
        return aktiv.Count > 1
            ? string.Format(Resource.Culture, katalog ? Resource.SATZBEARB_TITEL_KATALOG_N : Resource.SATZBEARB_TITEL_PROJEKT_N, aktiv.Count)
            : string.Format(Resource.Culture, katalog ? Resource.SATZBEARB_TITEL_KATALOG : Resource.SATZBEARB_TITEL_PROJEKT,
                            aktiv.Count == 1 ? aktiv[0].Name : "");
    }
}
