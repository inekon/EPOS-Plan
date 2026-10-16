using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    // =========================================================================
    // DER EINGABETYP DES ENERGIEFLUSSBILDS (Konzept Energieflussbild 3, Schritt SK-2).
    //
    // Rein darstellungsbezogen: Knoten mit Spalte, Name, Wert und Farbrolle, Bänder
    // mit Von, Nach, Wert, Rolle und fertigem Zeigertext. Er kennt das Domänenmodell
    // des Flussbaus nicht; die Abbildung Flussmodell → Bilddaten ist Sache der Hülle
    // bzw. des Berichtsbilds (SK-3, SK-4). Der Renderer rechnet nichts Fachliches:
    // Er setzt Knoten in Spalten, misst Bänder nach ihrem Wert und zeichnet.
    // =========================================================================

    /// <summary>Was ein Knoten des Energieflussbilds ist — bestimmt Lage der Beschriftung und Rückfallrolle.</summary>
    public enum EnergieflussKnotenart
    {
        /// <summary>Eine Quelle (Netzbezug, Photovoltaik, Brennstoffträger, Umweltwärme …); Beschriftung links.</summary>
        Quelle = 0,

        /// <summary>Ein Wandler (BHKW, Kessel, Wärmepumpe, Kältemaschine …); Beschriftung über dem Knoten.</summary>
        Wandler = 1,

        /// <summary>Ein Sammelknoten (Strom, Wärme, Kälte); Beschriftung über dem Knoten, auch wenn er flach ist.</summary>
        Bus = 2,

        /// <summary>Ein Speicher (Puffer, Kältespeicher, Stromspeicher); Beschriftung über dem Knoten.</summary>
        Speicher = 3,

        /// <summary>Eine Senke (Heizung, Warmwasser, Strombedarf, Einspeisung …); Beschriftung rechts.</summary>
        Senke = 4,

        /// <summary>
        /// Ein Verlustknoten: steht in der Lücke rechts neben dem Knoten, aus dem sein Band kommt (Spalte + ½), unter
        /// dessen Fuß; Beschriftung darunter. Seine <see cref="EnergieflussBildKnoten.Spalte"/> ist die des Erzeugers.
        /// </summary>
        Verlust = 5
    }

    /// <summary>Wie ein Band gezeichnet wird.</summary>
    public enum EnergieflussBandart
    {
        /// <summary>Ein gewöhnliches Band: Fläche in der Rolle, ohne Rand.</summary>
        Normal = 0,

        /// <summary>Ein Verlustband in seinen Verlustknoten (Rückfallrolle <see cref="Farbrolle.VERLUST"/>).</summary>
        Verlust = 1,

        /// <summary>Ein Band „nicht zugeordnet“ bzw. „ungedeckt“: Fläche mit gestricheltem Rand (Rückfallrolle <see cref="Farbrolle.REST"/>).</summary>
        NichtZugeordnet = 2
    }

    /// <summary>Ein Knoten des Energieflussbilds.</summary>
    public sealed class EnergieflussBildKnoten
    {
        /// <summary>Die eindeutige Kennung (sprachneutral, etwa „strom“, „wp_heizen“); Teil der Marke <c>knoten:&lt;Kennung&gt;</c>.</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Die Spalte 0 … 6 (Quellen, Wandler I, Bus Strom, Wandler II, Busse Wärme/Kälte, Speicher, Senken).</summary>
        public int Spalte { get; set; }

        /// <summary>Der Anzeigename in der Kultur des Bildes.</summary>
        public string Name { get; set; } = "";

        /// <summary>Die Kurzform für die schmale Fassung; leer = <see cref="Name"/>.</summary>
        public string Kurzname { get; set; } = "";

        /// <summary>
        /// Die Jahressumme [MWh/a], die die Berichtsfassung am Knoten nennt; nicht endlich = das Größere aus Zufluss und
        /// Abfluss der gezeichneten Bänder. Die Höhe des Knotens folgt immer seinen Bändern.
        /// </summary>
        public double Mwh { get; set; } = double.NaN;

        /// <summary>Die Farbrolle des Rechtecks; <c>null</c> = <see cref="Farbrolle.KNOTEN"/>, beim Verlustknoten <see cref="Farbrolle.VERLUST"/>.</summary>
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
        /// Die Farbrolle der Fläche (Konzept 3.4: ein Band in einen Bus trägt die Rolle seiner Quelle, eines aus dem Bus die
        /// seiner Verwendung); <c>null</c> = nach <see cref="Art"/> (<see cref="Farbrolle.VERLUST"/>,
        /// <see cref="Farbrolle.REST"/>, sonst <see cref="Farbrolle.UNBENANNT"/>).
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

    /// <summary>Die Eingabe des Renderers <c>ChartRenderer.EnergieflussModell</c>.</summary>
    public sealed class EnergieflussBildDaten
    {
        /// <summary>Die Knoten; innerhalb einer Spalte in Zeichenreihenfolge von oben. Ein Knoten ohne Band entfällt.</summary>
        public IReadOnlyList<EnergieflussBildKnoten> Knoten { get; set; } = Array.Empty<EnergieflussBildKnoten>();

        /// <summary>Die Bänder; ein Band mit unbekanntem Knoten oder ohne endlichen positiven Wert entfällt.</summary>
        public IReadOnlyList<EnergieflussBildBand> Baender { get; set; } = Array.Empty<EnergieflussBildBand>();

        /// <summary>Die Spaltentitel je Spalte („Quellen“, „Wandler“, …); fehlt einer oder ist er leer, steht keiner.</summary>
        public IReadOnlyList<string> Spaltentitel { get; set; } = Array.Empty<string>();
    }
}
