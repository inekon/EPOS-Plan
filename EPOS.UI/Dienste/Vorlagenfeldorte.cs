using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Simulation;

namespace EPOS.UI.Dienste;

/// <summary>
/// Ein Ort in der App, an dem eine Platzhaltermarke steht (Konzept Berichtsvorlagen 9.4, 9.6).
/// </summary>
/// <param name="Ansicht">Der Schlüssel der Ansicht in der Navigation (<c>Seitenschluessel</c>,
/// <c>Ansichten</c>), etwa <c>BERICHTE_KOSTEN</c> oder <c>SIMULATION</c>.</param>
/// <param name="Reiter">Das erste Argument an <c>Dienste.Navigation.OeffneMaske</c>: bei
/// „Berichte &amp; Kosten“ die Seite (<c>WIRTSCHAFT</c>), bei der Simulation die Marke mit Schritt
/// und Blatt (<c>schritt=3;blatt=UEBERSICHT</c>, <see cref="Vorlagenfeldorte.Ergebnisblatt"/>) —
/// so springt „In der App zeigen“ unter Windows (<c>WinFormsNavigation</c>) wie auf iOS; leer =
/// die Ansicht entscheidet.</param>
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

    /// <summary>Blatt „Bedarf“ des Simulationsergebnisses.</summary>
    public const string BLATT_BEDARF = "BEDARF";

    /// <summary>Blatt „Heizkessel“ des Simulationsergebnisses.</summary>
    public const string BLATT_HEIZKESSEL = "HEIZKESSEL";

    /// <summary>Blatt „Solarthermie“ des Simulationsergebnisses.</summary>
    public const string BLATT_SOLARTHERMIE = "SOLARTHERMIE";

    /// <summary>Blatt „BHKW“ des Simulationsergebnisses.</summary>
    public const string BLATT_BHKW = "BHKW";

    /// <summary>Blatt „Photovoltaik“ des Simulationsergebnisses.</summary>
    public const string BLATT_PHOTOVOLTAIK = "PHOTOVOLTAIK";

    /// <summary>Blatt „Ergebnis“ des Simulationsergebnisses (Autarkieanalyse, Wärme- und Stromgang).</summary>
    public const string BLATT_ERGEBNIS = "ERGEBNIS";

    /// <summary>
    /// Die Marke eines Blatts im Schritt ③ Ergebnis der Simulation (<c>schritt=3;blatt=…</c>) —
    /// der Weg, den beide Schalen nehmen: Die Windows-Navigation kennt nur die Ansicht
    /// <c>SIMULATION</c> mit Marke, nicht das Simulationsergebnis als eigene Maske.
    /// </summary>
    public static string Ergebnisblatt(string blatt) => SimulationMarke.Schreiben(3, blatt);

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
        // Standwerte mit Positionsform (stand.<n>.…, variante.<n>.…): die Sensitivität nennt je Stand ihrer Tafel die
        // Form mit Namen, Mehrjahrestafel und Zahlungsstrom die des Stands, den die Klappliste von Block 2 zeigt.
        O("stand.tabelle.sensitivitaet", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.sensitivitaet");
        O("tabelle.wirtschaft.kennzahlen", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("tabelle.wirtschaft.kennzahlen.guenstig", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("tabelle.wirtschaft.kennzahlen.unguenstig", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.gliederung");
        O("bild.wirtschaft.bruecke", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.bruecke");
        O("tabelle.wirtschaft.nicht_monetaer", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "liste.wirkungen");
        O("stand.tabelle.mehrjahres", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.zahlungsreihen");
        O("stand.bild.zahlungsstrom", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "bild.zahlungsstrom");
        O("tabelle.anhang_e.checkliste", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "ueberlagerung.anhang_e");
        // Nur Excel (Katalog v7): der Parameterblock am Parameternachweis, der Verlauf je Jahr am Kapitalwertverlauf.
        O("tabelle.wirtschaft.parameter", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.parameter");
        O("tabelle.wirtschaft.verlauf", ANSICHT_BERICHTE, REITER_WIRTSCHAFT, "tafel.verlauf");

        // ---- Berichte & Kosten › Kosten: die Gegenüberstellung --------------------------
        // Nur Excel (Katalog v9): die Kennzahlen aller Stände als Liste; die App zeigt davon die Kosten.
        O("tabelle.vergleich.liste", ANSICHT_BERICHTE, REITER_KOSTEN, "tafel.gegenueberstellung");

        // ---- Simulation › Ergebnis › Übersicht ------------------------------------------
        // Die Kennzahlen des Dashboards: Stamm → stamm.kennzahl.*, Variante → stand.kennzahl.* (9.5).
        foreach (bool stamm in new[] { true, false })
        {
            O(Kennzahl(stamm, "energie.waermebedarf"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.waermebedarf");
            O(Kennzahl(stamm, "energie.waermerest"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.restwaerme");
            O(Kennzahl(stamm, "energie.netzbezug"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.reststrom");
            O(Kennzahl(stamm, "kaelte.jahresbedarf"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.kaeltebedarf");
            O(Kennzahl(stamm, "kaelte.spitze"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.kaeltelast");
            O(Kennzahl(stamm, "kaelte.rest"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.kaelte_ungedeckt");
            O(Kennzahl(stamm, "kaelte.deckungsgrad"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.kaelte_deckung");
            O(Kennzahl(stamm, "kaelte.strom"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.kaeltestrom");
            O(Kennzahl(stamm, "kaelte.jaz"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "kennzahl.jaz_kaelte");
        }
        // Die Ringe: im Bericht Kuchendiagramme je Stand (ähnlich).
        O("stand.bild.deckung_waerme", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "bild.ring_waerme");
        O("stand.bild.deckung_strom", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "bild.ring_strom");
        // Nur Excel (Katalog v9): die Kennzahlen des Stands als Liste.
        O("stand.tabelle.kennzahlen.liste", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_UEBERSICHT), "liste.kennzahlen");

        // ---- Simulation › Ergebnis › Bedarf und Erzeuger (Katalog v10, ähnlich) ----------
        // Die Bilder der Reiter: im Bericht alle Reihen im Jahresverlauf, ohne Schalter.
        O("stand.bild.bedarf_waerme", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_BEDARF), "bild.bedarf_waerme");
        O("stand.bild.bedarf_strom", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_BEDARF), "bild.bedarf_strom");
        O("stand.bild.bedarf_kaelte", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_BEDARF), "bild.bedarf_kaelte");
        O("stand.bild.waermepumpe_streuwolke", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_WAERMEPUMPE), "bild.streuwolke");
        O("stand.bild.waermepumpe", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_WAERMEPUMPE), "bild.produktion");
        O("stand.bild.waermepumpe_strom", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_WAERMEPUMPE), "bild.stromverbrauch");
        O("stand.bild.heizkessel", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_HEIZKESSEL), "bild.heizkessel");
        O("stand.bild.solarthermie", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_SOLARTHERMIE), "bild.solarthermie");
        O("stand.bild.bhkw", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_BHKW), "bild.bhkw");
        O("stand.bild.photovoltaik", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_PHOTOVOLTAIK), "bild.photovoltaik");

        // ---- Simulation › Ergebnis › Wärmepumpe und Stromspeicher ------------------------
        O("stamm.bild.speichertemperaturen", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_WAERMEPUMPE), "bild.speichertemperaturen");
        O("stand.bild.speicherverlauf", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_STROMSPEICHER), "bild.speicherbetrieb");
        // Die Kacheln des Speicherlaufs (Katalog v10): die Kennwerte des Laufs, aus dem gespeicherten Lauf (ähnlich).
        foreach (string kennwert in new[] { "betriebsart", "berechnungsart", "ertrag", "ueberschuss", "amortisation",
                                            "vollzyklen", "eigenverbrauch", "autarkie" })
            O("stand.speicher." + kennwert, ANSICHT_SIMULATION, Ergebnisblatt(BLATT_STROMSPEICHER), "kachel." + kennwert);

        // ---- Simulation › Ergebnis › Ergebnis: Autarkieanalyse, Wärme- und Stromgang ------
        O(Kennzahl(true, "eff.autarkie"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "kachel.autarkie_pv");
        O(Kennzahl(false, "eff.autarkie"), ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "kachel.autarkie_pv");
        O("stand.solarthermie.deckung", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "kachel.deckung_solarthermie");
        O("stand.bild.strombilanz_monate", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "bild.autarkie_monate");
        O("stand.tabelle.monatswerte", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "tafel.monatswerte");
        O("stand.bild.waerme_jahresverlauf", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "bild.waermegang");
        O("stand.bild.strombilanz_monate", ANSICHT_SIMULATION, Ergebnisblatt(BLATT_ERGEBNIS), "bild.stromgang");

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
