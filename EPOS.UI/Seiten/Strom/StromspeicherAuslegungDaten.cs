using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;
using WindowsFormsApplication1;

namespace EPOS.UI.Seiten.Strom;

/// <summary>
/// Die sprachneutralen Schlüssel der Ablaufleiste (Konzept „Stromspeicher-Dialoge" 2.1).
/// </summary>
/// <remarks>
/// <b>Schritt 4 ist KEIN Blatt</b> — er ist der Rechenknopf und steht deshalb nicht in
/// dieser Liste. Die Ablaufleiste zeigt ihn trotzdem an vierter Stelle, weil der Ablauf
/// genau dort seine Zäsur hat.
/// </remarks>
public static class AuslegungSchritt
{
    /// <summary>Blatt 1 — die Speichereinheiten; eine Einheit ist der Einzelspeicher (SD‑E‑8).</summary>
    public const string Speicher = "SPEICHER";

    /// <summary>Blatt 2 — Quellen, Kosten und Profile.</summary>
    public const string Daten = "DATEN";

    /// <summary>Blatt 3 — Betriebsführung und Peak-Ziel.</summary>
    public const string Betrieb = "BETRIEB";

    /// <summary>Blatt 5 — das Ergebnis; erst nach einem Lauf betretbar.</summary>
    public const string Ergebnis = "ERGEBNIS";

    /// <summary>Die vier Blätter in der Reihenfolge der Leiste.</summary>
    public static readonly IReadOnlyList<string> Alle = new[] { Speicher, Daten, Betrieb, Ergebnis };
}

/// <summary>Ein Eintrag der Ablaufleiste.</summary>
/// <param name="Schluessel">Der sprachneutrale Schlüssel; leer beim Rechenknopf.</param>
/// <param name="Titel">Die Beschriftung.</param>
/// <param name="Bedienbar">Darf der Schritt betreten werden?</param>
/// <param name="Veraltet">Der Schritt zeigt einen überholten Stand.</param>
/// <param name="Sperrgrund">Warum er nicht bedienbar ist; leer = kein Grund zu nennen.</param>
public sealed record Ablaufeintrag(string Schluessel, string Titel, bool Bedienbar,
                                   bool Veraltet, string Sperrgrund);

/// <summary>
/// Die DATENSEITE der Ansicht „Stromspeicher-Auslegung" (Paket P3, Auftrag #192).
///
/// <para><b>Ein Bündel statt zwanzig Parameter</b> — dasselbe Muster wie
/// <see cref="SimulationErgebnisDienste"/>. Jeder Eintrag ist ein Weg in den Kern
/// (<c>WindowsFormsApplication1.StromspeicherAuslegungCtrl</c>); die Plattformhülle
/// legt nur das <c>Task.Run</c>, den Dateiwähler und den Fensterbesitz darum.</para>
///
/// <para><b>Kein Delegat ist kein Knopf.</b> Fehlt ein Eintrag, blendet die Ansicht die
/// zugehörige Bedienung aus, statt sie gesperrt oder wirkungslos zu zeigen.</para>
/// </summary>
public sealed class StromspeicherAuslegungDienste
{
    /// <summary>Die Vorbelegung — Flotte, Suchraum, Quellen, Kosten, Profile.</summary>
    public Func<SpeicherOptimierungVorgaben>? Vorgaben;

    /// <summary>
    /// Liegt ein gerechneter Simulationslauf vor? Ohne ihn stehen die EPOS-Zeitreihen
    /// nicht zur Verfügung, und die Ansicht bietet <see cref="Simulationslauf"/> an.
    /// </summary>
    public Func<bool>? LaufVorhanden;

    /// <summary>
    /// Rechnet den fehlenden Simulationslauf nach (Muster iU9‑W11a); <c>melder</c>
    /// bekommt Anteil und Phasentext.
    /// </summary>
    public Func<Action<double?, string>, Task<Rueckmeldung>>? Simulationslauf;

    /// <summary>Rechnet die Flottenstudie bzw. die Flotten-Rastersuche.</summary>
    public Func<SpeicherOptimierungEingaben, Action<double?, string>,
                Task<SpeicherFlottenErgebnis>>? FlotteRechnen;

    /// <summary>Bricht einen laufenden Hintergrundlauf ab; ohne Delegat kein Knopf.</summary>
    public Action? Abbrechen;

    /// <summary>Speichert den Arbeitsstand; leerer Rückgabewert = Erfolg.</summary>
    public Func<SpeicherOptimierungEingaben, Task<string>>? EinstellungenSpeichern;

    /// <summary>Speichert ein benanntes Profil und liefert die frisch gelesenen Vorgaben.</summary>
    public Func<SpeicherOptimierungEingaben, string, Task<SpeicherOptimierungVorgaben>>? ProfilSpeichern;

    /// <summary>Wählt und liest eine CSV-Datei in der Plattformhülle.</summary>
    public Func<Task<SpeicherImportDatei>>? DateiWaehlen;

    /// <summary>Schreibt einen CSV-Text in eine Datei.</summary>
    public Func<string, Task<Rueckmeldung>>? Csv;

    /// <summary>
    /// Übernimmt Kapazität [kWh] und Leistung [kW] EINER Einheit in die Speicheranlage
    /// des Projekts (Schritt 5, „Größe in die Projektanlage übernehmen").
    /// </summary>
    /// <remarks>
    /// <b>Er bleibt mit SD‑E‑8</b>: Der Projektlauf ohne aktivierte Flotte rechnet
    /// weiter die Einzelanlage (SD‑Q2), und ohne diesen Weg käme die ausgelegte Größe
    /// dort nie an.
    /// </remarks>
    public Func<double, double, Rueckmeldung>? AuslegungUebernehmen;

    /// <summary>
    /// Schreibt den Leistungspreis L_P [€/(kW·a)] sofort in die aktive Variante.
    /// </summary>
    /// <remarks>
    /// Es ist DERSELBE Wert wie der Leistungspreis der Flotte
    /// (<c>FlottenTarif.LeistungspreisEuroProKw</c>) — eine Eingabe in Schritt 2, keine
    /// zweite (SD‑E‑8).
    /// </remarks>
    public Action<double>? LeistungspreisSchreiben;

    /// <summary>Ist die Flotte dieses Projekts für den Projektlauf aktiviert?</summary>
    public Func<bool>? ProjektflotteAktiv;

    /// <summary>Aktiviert den gerechneten Stand für den Projektlauf; leer = Erfolg.</summary>
    public Func<SpeicherFlottenErgebnis, Task<string>>? ProjektflotteAktivieren;

    /// <summary>Nimmt die Flotte aus dem Projektlauf heraus; leer = Erfolg.</summary>
    public Func<Task<string>>? ProjektflotteDeaktivieren;

    /// <summary>
    /// Die VORPRÜFUNG vor dem Lauf (Konzept 2.4 Punkt 3) — sie warnt, sie sperrt nicht.
    /// </summary>
    public Func<SpeicherOptimierungEingaben, IReadOnlyList<FlottenHinweis>>? Vorpruefen;

    /// <summary>Der hergeleitete Vorschlag für das Peak-Ziel H₀ (SD‑Q3).</summary>
    public Func<SpeicherOptimierungEingaben, FlottenPeakZielVorschlag>? PeakZielVorschlag;

    /// <summary>
    /// Bestimmt das kleinste haltbare Peak-Ziel per Bisektion; <c>melder</c> bekommt
    /// Anteil und Lauftext. Ohne Delegat kein Knopf.
    /// </summary>
    public Func<SpeicherOptimierungEingaben, Action<double?, string>,
                Task<FlottenPeakZielErgebnis>>? PeakZielBestimmen;
}
