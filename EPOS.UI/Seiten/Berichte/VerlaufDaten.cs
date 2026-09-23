using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// ETAPPE E6 (Konzept § 2.13 (5), Mockup Kategorie 8 „Der Verlauf über die Zeit — alle drei
/// Szenarien") — was der Abschnitt „Verlauf" der Wirtschaftlichkeitsseite zeigt: das Bild
/// als ZEICHENMODELL, die wählbaren Stände und Szenarien und die Zeilen darunter.
///
/// <para>Die Hülle baut es aus dem gerechneten Verlauf; gerechnet wird hier nichts. Ein
/// Abschnitt ohne Rechnung bekommt eine Ansicht OHNE Modell und mit dem Grund im
/// <see cref="Hinweis"/> — kein vorbelegtes Bild.</para>
/// </summary>
public sealed class VerlaufAnsicht
{
    /// <summary>Das Bild; <c>null</c> = noch kein Verlauf gerechnet.</summary>
    public Zeichenmodell? Modell { get; set; }

    /// <summary>Die Stände mit Linie, die gewählt werden können (Id, Name), in der
    /// Reihenfolge der Gruppe — nur die, die im Vergleich angehakt sind.</summary>
    public IReadOnlyList<(int Id, string Text)> Staende { get; set; } = Array.Empty<(int, string)>();

    /// <summary>Die Stände, die das Bild zeichnet (Ids).</summary>
    public IReadOnlyList<int> GewaehlteStaende { get; set; } = Array.Empty<int>();

    /// <summary>Die drei Szenarien (Nummer, Anzeigetext) in der Reihenfolge Ungünstig ·
    /// Erwartet · Günstig. Die Persistenzwerte kennt nur die Hülle.</summary>
    public IReadOnlyList<(int Id, string Text)> Szenarien { get; set; } = Array.Empty<(int, string)>();

    /// <summary>Die Szenarien, die das Bild zeichnet (Nummern).</summary>
    public IReadOnlyList<int> GewaehlteSzenarien { get; set; } = Array.Empty<int>();

    /// <summary>Der Horizont des gerechneten Verlaufs [a]; 0 = noch keiner.</summary>
    public int Jahre { get; set; }

    /// <summary>Die Nulldurchgänge je Stand und Szenario — die dynamische Amortisation.</summary>
    public string Nulldurchgangszeile { get; set; } = "";

    /// <summary>Die Restwert-Barwerte am Horizontende (nicht in den Linien).</summary>
    public string Restwertzeile { get; set; } = "";

    /// <summary>Horizont, gegebenenfalls abweichend vom Betrachtungszeitraum.</summary>
    public string Statuszeile { get; set; } = "";

    /// <summary>Ein Hinweis: noch nicht gerechnet, Stände ohne Reihe, fehlende Stände.
    /// Leer = keiner.</summary>
    public string Hinweis { get; set; } = "";

    /// <summary>Hat die Rechnung neu simuliert? Dann passen die gespeicherten Ergebnisse
    /// der Seite nicht mehr zum Simulationsstand — die Seite liest neu.</summary>
    public bool NeuSimuliert { get; set; }

    /// <summary>Die Meldung für die Statuszeile der Seite; leer = keine.</summary>
    public string Meldung { get; set; } = "";
}

/// <summary>
/// ETAPPE E6 — die Wahl im Abschnitt „Verlauf": welche Stände und welche Szenarien das Bild
/// zeichnet. <c>null</c> heißt jeweils „alle".
/// </summary>
/// <param name="Staende">Die gewählten Stände (<c>Tab_Projekt.ID</c>).</param>
/// <param name="Szenarien">Die gewählten Szenarien (Nummern der <see cref="VerlaufAnsicht.Szenarien"/>).</param>
public sealed record VerlaufWahl(IReadOnlyCollection<int>? Staende, IReadOnlyCollection<int>? Szenarien)
{
    /// <summary>Alles gewählt.</summary>
    public static readonly VerlaufWahl Alle = new VerlaufWahl(null, null);
}

/// <summary>
/// ETAPPE E6 — die Datenseite des Abschnitts „Verlauf"; die Hülle legt sie ein. Ohne
/// Delegat geschieht an der Stelle nichts: ohne <see cref="Berechnen"/> kein Knopf
/// „Aktualisieren", ohne <see cref="NachExcel"/> kein Knopf „Verlauf nach Excel…".
/// </summary>
public sealed class VerlaufDienste
{
    /// <summary>
    /// Die Ansicht zur Wahl — aus dem Verlauf, der schon gerechnet ist (billig; ein Wechsel
    /// der Sicht rechnet ihn aus denselben Eingangsdaten nach). <c>null</c> = nichts zu zeigen.
    /// </summary>
    public Func<VerlaufWahl, VerlaufAnsicht?>? Zeichnen { get; set; }

    /// <summary>
    /// Rechnet den Verlauf über den Horizont (Jahre) neu — sammelt die Simulationsdaten, wo
    /// sie fehlen, und rechnet drei vollständige Läufe; abbrechbar.
    /// </summary>
    public Func<int, VerlaufWahl, CancellationToken, Task<VerlaufAnsicht>>? Berechnen { get; set; }

    /// <summary>
    /// „Verlauf nach Excel…" (U13): schreibt das Blatt „Verlauf" über den Dateidienst der
    /// Plattform; die Rückmeldung geht in die Statuszeile.
    /// </summary>
    public Func<VerlaufWahl, Task<Rueckmeldung>>? NachExcel { get; set; }

    /// <summary>Der Horizont, mit dem das Zeitraumfeld beginnt — der Betrachtungszeitraum.</summary>
    public int JahreVorgabe { get; set; } = 20;
}
