using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Dienste;

/// <summary>
/// Ein Ort in der App, an dem eine Platzhaltermarke steht (Konzept Berichtsvorlagen 9.4, 9.6).
/// </summary>
/// <param name="Ansicht">Der Schlüssel der Ansicht in der Navigation (<c>Seitenschluessel</c>,
/// <c>Ansichten</c>), etwa <c>BERICHTE_KOSTEN</c> oder <c>SIMULATION</c>.</param>
/// <param name="Reiter">Die Seite bzw. das Blatt in der Ansicht, etwa <c>WIRTSCHAFT</c> oder
/// <c>UEBERSICHT</c>; leer = die Ansicht hat keine.</param>
/// <param name="Element">Die Kennung des markierten Elements auf der Seite, etwa
/// <c>kachel.kapitalwert</c> — eine Beschreibung, keine DOM-Kennung: Gefunden wird die Marke
/// über ihren Schlüssel (<c>data-vorlagenfeld</c>), nie über die Kennung eines Diagramms.</param>
/// <param name="NurLesend">Ist der Ort lesend erreichbar? „In der App zeigen“ springt nur
/// dorthin; der Projektassistent (Bearbeitungsmodus) ist es nicht.</param>
public sealed record Vorlagenfeldort(string Ansicht, string Reiter, string Element, bool NurLesend);

/// <summary>Eine Zeile der Ortstabelle: ein Katalogschlüssel an einem Ort.</summary>
/// <param name="Schluessel">Der Katalogschlüssel (<c>Vorlagenfeldkatalog</c>).</param>
/// <param name="Ort">Wo die Marke steht.</param>
public sealed record Vorlagenfeldortzeile(string Schluessel, Vorlagenfeldort Ort);

/// <summary>
/// <b>Die Orte aller Platzhaltermarken der App</b> (BV-E6, Konzept Berichtsvorlagen 9.5, 9.6):
/// je Katalogschlüssel die Stellen, an denen ein Baustein ihn als <c>Vorlagenfeld</c> trägt.
/// Der Platzhalterkatalog fragt hier, wohin „In der App zeigen“ springt.
///
/// <para><b>Die Regel dahinter (9.5):</b> Eine Marke nennt nur einen Schlüssel, der genau den
/// angezeigten Wert erzeugt; zeigt die App ein ähnliches Element (Ring statt Kuchen, alle Stände
/// statt eines), trägt die Marke die Stufe „ähnlich im Bericht“. Welcher Schlüssel an einem Wert
/// steht, entscheidet die Hülle (<c>EPOS.UI.Daten</c>) — sie kennt Stamm und Variante, Szenario
/// und beste Variante; die Bausteine kennen keinen Katalog.</para>
///
/// <para><b>Gehalten</b> von <c>VorlagenfeldorteWacheTests</c> (jede Zeile ein Katalogschlüssel
/// mit <c>Seit</c> ≤ Katalogfassung, jeder Ort ein bekanntes Blatt, jede literale Marke in den
/// Seiten eine Zeile) und <c>VorlagenfeldAnzeigewertWacheTests</c> (jede gesetzte Marke der
/// Hüllen steht hier, und ihr Wert ist der aufgelöste Katalogwert).</para>
/// </summary>
public static class Vorlagenfeldorte
{
    // =====================================================================
    //  Ansichten und Blätter
    // =====================================================================

    /// <summary>Ansicht „Berichte &amp; Kosten“ (<c>Ansichten.BerichteKosten</c>).</summary>
    public const string ANSICHT_BERICHTE = "BERICHTE_KOSTEN";

    /// <summary>Ansicht „Simulation“ (<c>Masken.Simulation</c>), Schritt ③ Ergebnis.</summary>
    public const string ANSICHT_SIMULATION = "SIMULATION";

    /// <summary>Der Projektassistent (<c>Seitenschluessel.Assistent</c>) — Bearbeitungsmodus.</summary>
    public const string ANSICHT_ASSISTENT = "ASSISTENT";

    /// <summary>Seite „Übersicht“ von „Berichte &amp; Kosten“.</summary>
    public const string REITER_UEBERSICHT = "UEBERSICHT";

    /// <summary>Seite „Kosten“ von „Berichte &amp; Kosten“.</summary>
    public const string REITER_KOSTEN = "KOSTEN";

    /// <summary>Seite „Wirtschaftlichkeit“ von „Berichte &amp; Kosten“.</summary>
    public const string REITER_WIRTSCHAFT = "WIRTSCHAFT";

    /// <summary>Blatt „Übersicht“ des Simulationsergebnisses.</summary>
    public const string BLATT_UEBERSICHT = "UEBERSICHT";

    /// <summary>Blatt „Wärmepumpe“ des Simulationsergebnisses.</summary>
    public const string BLATT_WAERMEPUMPE = "WAERMEPUMPE";

    /// <summary>Blatt „Stromspeicher“ des Simulationsergebnisses.</summary>
    public const string BLATT_STROMSPEICHER = "STROMSPEICHER";

    /// <summary>Schritt 1 des Projektassistenten: der Projektkopf.</summary>
    public const string SCHRITT_PROJEKTKOPF = "PROJEKTKOPF";

    // =====================================================================
    //  Schlüssel, die eine Hülle nach dem Stand bildet
    // =====================================================================

    /// <summary>Die Vorsilbe des Stammprojekts (Kontext Stamm).</summary>
    public const string STAMM = "stamm";

    /// <summary>Die Vorsilbe eines Stands im Block <c>{{#je stand}}</c> (Kontext Stand).</summary>
    public const string STAND = "stand";

    /// <summary>
    /// Der Kennzahlschlüssel des gezeigten Stands (9.5): beim Stammprojekt
    /// <c>stamm.kennzahl.&lt;k&gt;</c>, bei einer Variante <c>stand.kennzahl.&lt;k&gt;</c>.
    /// </summary>
    public static string Kennzahl(bool stamm, string kennzahl) => (stamm ? STAMM : STAND) + ".kennzahl." + kennzahl;

    /// <summary>
    /// Eine Zeile der Wirtschaftlichkeit des gezeigten Stands (9.5, Kostenkacheln): beim Stamm
    /// <c>stamm.wirtschaft.&lt;zeile&gt;</c>, bei einer Variante <c>stand.wirtschaft.&lt;zeile&gt;</c>.
    /// </summary>
    public static string Wirtschaft(bool stamm, string zeile) => (stamm ? STAMM : STAND) + ".wirtschaft." + zeile;

    /// <summary>Der Anhang eines Szenarios an Schlüsseln mit Szenario: Erwartet ohne Anhang.</summary>
    public static string Szenarioanhang(string szenarioschluessel) => szenarioschluessel switch
    {
        "guenstig" => ".guenstig",
        "unguenstig" => ".unguenstig",
        _ => "",
    };

    // =====================================================================
    //  Die Tabelle
    // =====================================================================

    private static readonly IReadOnlyList<Vorlagenfeldortzeile> _alle = Bilde();

    /// <summary>Alle Zeilen in Seitenfolge.</summary>
    public static IReadOnlyList<Vorlagenfeldortzeile> Alle => _alle;

    /// <summary>Die Orte eines Schlüssels (ordinal, wie der Katalog normiert); leer = keine Marke.</summary>
    public static IReadOnlyList<Vorlagenfeldort> Finde(string schluessel)
    {
        if (string.IsNullOrWhiteSpace(schluessel)) return Array.Empty<Vorlagenfeldort>();
        string s = schluessel.Trim().ToLowerInvariant();
        return _alle.Where(z => string.Equals(z.Schluessel, s, StringComparison.Ordinal)).Select(z => z.Ort).ToList();
    }

    private static List<Vorlagenfeldortzeile> Bilde()
    {
        var l = new List<Vorlagenfeldortzeile>();
        void O(string schluessel, string ansicht, string reiter, string element, bool nurLesend = true)
            => l.Add(new Vorlagenfeldortzeile(schluessel, new Vorlagenfeldort(ansicht, reiter, element, nurLesend)));

        // ---- Berichte & Kosten › Übersicht --------------------------------------------
        // Die Gegenüberstellung der Komponenten je Version (ähnlich: im Bericht die
        // Komponentenmatrix des Kapitels „Projekt“ mit allen Merkmalen).
        O("tabelle.komponenten.matrix", ANSICHT_BERICHTE, REITER_UEBERSICHT, "tafel.komponenten");

        // ---- Berichte & Kosten › Wirtschaftlichkeit: die Vergleichsgruppe --------------
        // Die App listet alle Stände zur Wahl, der Bericht die gewählten (ähnlich).
        O("tabelle.varianten", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.varianten");

        // ---- Berichte & Kosten › Kosten -----------------------------------------------
        // Die drei Kacheln zeigen den angezeigten Stand: Stamm oder Variante (9.5).
        foreach (string zeile in new[] { "investition", "betriebskosten", "energiekosten" })
        {
            O(Wirtschaft(true, zeile), ANSICHT_BERICHTE, REITER_KOSTEN, "kachel." + zeile);
            O(Wirtschaft(false, zeile), ANSICHT_BERICHTE, REITER_KOSTEN, "kachel." + zeile);
        }

        // ---- Berichte & Kosten › Wirtschaftlichkeit ------------------------------------
        // Die vier Kacheln der besten Variante bzw. des Stammfalls (BesteVariante.Waehle).
        O("wirtschaft.beste.kapitalwert", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "kachel.kapitalwert");
        O("wirtschaft.beste.annuitaet", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "kachel.annuitaet");
        O("wirtschaft.beste.amortisation", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "kachel.amortisation");
        O("wirtschaft.beste.irr", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "kachel.irr");
        O("tabelle.wirtschaft.kennzahlen", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.kennzahlen");
        O("tabelle.wirtschaft.szenarien", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.bandbreite");
        O("bild.wirtschaft.spanne", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.spanne");
        O("bild.wirtschaft.kapitalwert_szenarien", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.verlauf");
        O("stand.tabelle.sensitivitaet", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.sensitivitaet");
        O("tabelle.wirtschaft.kennzahlen", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("tabelle.wirtschaft.kennzahlen.guenstig", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("tabelle.wirtschaft.kennzahlen.unguenstig", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("bild.wirtschaft.bruecke", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.bruecke");
        O("tabelle.wirtschaft.nicht_monetaer", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "liste.wirkungen");
        O("stand.tabelle.mehrjahres", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.zahlungsreihen");
        O("stand.bild.zahlungsstrom", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.zahlungsstrom");
        O("tabelle.anhang_e.checkliste", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "ueberlagerung.anhang_e");

        // ---- Simulation › Ergebnis › Übersicht ------------------------------------------
        // Die Kennzahlen des Dashboards: Stamm → stamm.kennzahl.*, Variante → stand.kennzahl.* (9.5).
        foreach (bool stamm in new[] { true, false })
        {
            O(Kennzahl(stamm, "energie.waermebedarf"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.waermebedarf");
            O(Kennzahl(stamm, "energie.waermerest"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.restwaerme");
            O(Kennzahl(stamm, "energie.netzbezug"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.reststrom");
            O(Kennzahl(stamm, "kaelte.jahresbedarf"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.kaeltebedarf");
            O(Kennzahl(stamm, "kaelte.spitze"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.kaeltelast");
            O(Kennzahl(stamm, "kaelte.rest"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.kaelte_ungedeckt");
            O(Kennzahl(stamm, "kaelte.deckungsgrad"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.kaelte_deckung");
            O(Kennzahl(stamm, "kaelte.strom"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.kaeltestrom");
            O(Kennzahl(stamm, "kaelte.jaz"), ANSICHT_SIMULATION, BLATT_UEBERSICHT, "kennzahl.jaz_kaelte");
        }
        // Die Ringe: im Bericht Kuchendiagramme je Stand (ähnlich).
        O("stand.bild.deckung_waerme", ANSICHT_SIMULATION, BLATT_UEBERSICHT, "bild.ring_waerme");
        O("stand.bild.deckung_strom", ANSICHT_SIMULATION, BLATT_UEBERSICHT, "bild.ring_strom");

        // ---- Simulation › Ergebnis › Wärmepumpe und Stromspeicher ------------------------
        O("stamm.bild.speichertemperaturen", ANSICHT_SIMULATION, BLATT_WAERMEPUMPE, "bild.speichertemperaturen");
        O("stand.bild.speicherverlauf", ANSICHT_SIMULATION, BLATT_STROMSPEICHER, "bild.speicherbetrieb");

        // ---- Projektassistent › Projektkopf (Bearbeitungsmodus, nicht lesend) -------------
        O("projekt.name", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.name", false);
        O("projekt.kunde", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.kunde", false);
        O("projekt.bearbeiter", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.bearbeiter", false);
        O("projekt.geaendert", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.geaendert", false);
        O("projekt.angelegt", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.angelegt", false);
        O("projekt.beschreibung", ANSICHT_ASSISTENT, SCHRITT_PROJEKTKOPF, "feld.beschreibung", false);
        return l;
    }
}
