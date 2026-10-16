﻿using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    // =========================================================================
    // DER EINGABETYP DES ENERGIEFLUSSBILDS (Konzept Energieflussbild 3, Schritte SK-2 und SK-2c).
    //
    // Rein darstellungsbezogen: Knoten mit Spur, Name, Wert und Farbrolle, Bänder
    // mit Von, Nach, Wert, Rolle und fertigem Zeigertext, die Legende der
    // vorkommenden Energieformen. Mit der Bilanz (EnergieflussBilanz) teilt er nur
    // die Knotenart (EnergieflussKnotenart); die Abbildung Bilanz → Bilddaten ist
    // EnergieflussAbbildung.Bild. Der Renderer rechnet nichts Fachliches: Er
    // bestimmt die Spalte jedes Knotens aus dem längsten Weg zur Senke, ordnet die
    // Knoten in ihren Spuren, misst Bänder nach ihrem Wert und zeichnet.
    // =========================================================================

    /// <summary>Wie ein Band gezeichnet wird.</summary>
    public enum EnergieflussBandart
    {
        /// <summary>Ein gewöhnliches Band: Fläche in der Rolle, weißer Saum.</summary>
        Normal = 0,

        /// <summary>Ein Verlustband: kurzer Abzweig am Erzeuger bzw. Bus ohne Knotenbalken (Rückfallrolle <see cref="Farbrolle.VERLUST"/>).</summary>
        Verlust = 1,

        /// <summary>Ein Band „nicht zugeordnet“ bzw. „ungedeckt“: Fläche mit gestricheltem Rand (Rückfallrolle <see cref="Farbrolle.REST"/>).</summary>
        NichtZugeordnet = 2
    }

    /// <summary>
    /// Die Spur (Zeile) eines Knotens im Bild (Konzept 3.1, Variante A „Gestraffte Kette“): In jeder Spalte stehen die Knoten
    /// der Stromspur oben, Gemischtes (das BHKW, das Strom und Wärme liefert) dazwischen, die Wärmespur in der Mitte, die
    /// Kältespur unten; zwischen den Spuren hält der Renderer einen größeren Abstand.
    /// </summary>
    public enum EnergieflussSpur
    {
        Strom = 0,
        Gemischt = 1,
        Waerme = 2,
        Kaelte = 3
    }

    /// <summary>
    /// Die Darstellungsart des Bildes (Anwenderentscheid SK-Q17): <see cref="Kette"/> ist Variante A „Gestraffte Kette“
    /// (Spalten nach Bedarf, Vorgabe, auch im Wortbericht); <see cref="Stufen"/> ist Variante B „Einsatz | Anlagen |
    /// Energieform | Nutzen“ mit Verbindern für den Strom an die Wärme- und Kälteerzeuger — sie kommt mit SK-2d und wird
    /// bis dahin benannt abgelehnt. Dieselben Bilddaten tragen beide Darstellungen; die Abbildung kennt keine.
    /// </summary>
    public enum EnergieflussDarstellung
    {
        Kette = 0,
        Stufen = 1
    }

    /// <summary>Ein Knoten des Energieflussbilds.</summary>
    public sealed class EnergieflussBildKnoten
    {
        /// <summary>Die eindeutige Kennung (sprachneutral, etwa „strom“, „wp_heizen“); Teil der Marke <c>knoten:&lt;Kennung&gt;</c>.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Die Spur des Knotens (Strom, Gemischt, Wärme, Kälte). Die Spalte bestimmt der Renderer aus dem längsten Weg zur Senke.</summary>
        public EnergieflussSpur Spur { get; set; } = EnergieflussSpur.Waerme;

        /// <summary>Der Anzeigename in der Kultur des Bildes.</summary>
        public string Name { get; set; } = "";

        /// <summary>Die Kurzform: Beschriftung der Wandlerkästen und der schmalen Fassung; leer = <see cref="Name"/>.</summary>
        public string Kurzname { get; set; } = "";

        /// <summary>
        /// Die Jahressumme [MWh/a], die die Berichtsfassung am Knoten nennt; nicht endlich = das Größere aus Zufluss und
        /// Abfluss der gezeichneten Bänder. Die Höhe des Knotens folgt immer seinen Bändern.
        /// </summary>
        public double Mwh { get; set; } = double.NaN;

        /// <summary>Die Farbrolle des Rechtecks; <c>null</c> = <see cref="Farbrolle.KNOTEN"/>. Ein Verlustknoten hat kein Rechteck.</summary>
        public Farbrolle Rolle { get; set; }

        /// <summary>Die Art des Knotens.</summary>
        public EnergieflussKnotenart Art { get; set; } = EnergieflussKnotenart.Wandler;

        /// <summary>Der fertige Zeigertext („Wärme: Zufluss 71,4 · Abfluss 71,4 MWh/a“); <c>null</c> = „Name: Wert MWh/a“.</summary>
        public string Zeigertext { get; set; }
    }

    /// <summary>Ein Band (Fluss) des Energieflussbilds.</summary>
    public sealed class EnergieflussBildBand
    {
        /// <summary>Die Kennung des Knotens, aus dem das Band kommt.</summary>
        public string Von { get; set; } = "";

        /// <summary>Die Kennung des Knotens, in den das Band geht.</summary>
        public string Nach { get; set; } = "";

        /// <summary>Die Energie [MWh/a]; nicht endlich oder ≤ 0 = kein Band.</summary>
        public double Mwh { get; set; }

        /// <summary>
        /// Die Farbrolle der Fläche — die Energieform des Bandes (Konzept 3.4: <see cref="Farbrolle.STROM"/>,
        /// <see cref="Farbrolle.WAERME"/>, <see cref="Farbrolle.KAELTE"/>, <see cref="Farbrolle.BRENNSTOFF"/>,
        /// <see cref="Farbrolle.UMWELTWAERME"/>, <see cref="Farbrolle.VERLUST"/>, <see cref="Farbrolle.REST"/>); <c>null</c> =
        /// nach <see cref="Art"/> (<see cref="Farbrolle.VERLUST"/>, <see cref="Farbrolle.REST"/>, sonst <see cref="Farbrolle.UNBENANNT"/>).
        /// </summary>
        public Farbrolle Rolle { get; set; }

        /// <summary>Die Art des Bandes.</summary>
        public EnergieflussBandart Art { get; set; } = EnergieflussBandart.Normal;

        /// <summary>
        /// Der fertige Zeigertext („Wärme → Heizung: 67,3 MWh/a (94 % des Wärmebedarfs)“); <c>null</c> = „Von → Nach:
        /// Wert MWh/a“ aus den Knotennamen.
        /// </summary>
        public string Zeigertext { get; set; }
    }

    /// <summary>Ein Eintrag der Legende unter dem Bild: eine Energieform mit ihrer Rolle.</summary>
    public sealed class EnergieflussBildLegende
    {
        /// <summary>Die Farbrolle des Farbfelds.</summary>
        public Farbrolle Rolle { get; set; }

        /// <summary>Der Text in der Kultur des Bildes („Strom“, „Wärme“ …).</summary>
        public string Text { get; set; } = "";

        /// <summary>Das Farbfeld trägt einen gestrichelten Rand (nicht zugeordnet, ungedeckt).</summary>
        public bool Gestrichelt { get; set; }
    }

    /// <summary>Die Eingabe des Renderers <c>ChartRenderer.EnergieflussModell</c>.</summary>
    public sealed class EnergieflussBildDaten
    {
        /// <summary>Die Knoten; innerhalb einer Spur in Vorzugsreihenfolge von oben. Ein Knoten ohne Band entfällt.</summary>
        public IReadOnlyList<EnergieflussBildKnoten> Knoten { get; set; } = Array.Empty<EnergieflussBildKnoten>();

        /// <summary>Die Bänder; ein Band mit unbekanntem Knoten oder ohne endlichen positiven Wert entfällt.</summary>
        public IReadOnlyList<EnergieflussBildBand> Baender { get; set; } = Array.Empty<EnergieflussBildBand>();

        /// <summary>Die Legende der vorkommenden Energieformen unter dem Bild; leer = keine Legende.</summary>
        public IReadOnlyList<EnergieflussBildLegende> Legende { get; set; } = Array.Empty<EnergieflussBildLegende>();
    }
}
