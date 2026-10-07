using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vermerke eines Dateikörpers (Datenaustauschkonzept 15.2, 15.3): sprachneutrale Schlüssel, nie Anzeigetext —
    /// den Text wählt die Ansicht.
    /// </summary>
    internal enum Koerpervermerk
    {
        /// <summary>Ein Bogen oder Kreis ist als Sehnenzug gelesen (<see cref="Dateikoerper.SEHNEN_VOLLKREIS"/> je Vollkreis).</summary>
        Bogen,

        /// <summary>Ein Loch ließ sich nicht anbinden; gezeichnet ist nur der Außenring.</summary>
        Loch,

        /// <summary>Eine Fläche ist nicht eben; gezeichnet als Fächer vom ersten Punkt.</summary>
        Uneben,

        /// <summary>Ein boolescher Körper ist allein mit seinem ersten Operanden gelesen — der Beschnitt fehlt.</summary>
        OhneBeschnitt,

        /// <summary>Die Schale ist offen (Flächenmodell, offene Schale, fehlende Fläche) und wird so gezeichnet.</summary>
        Offen,

        /// <summary>Ein Körper mit Hohlräumen ist allein mit seiner Außenschale gelesen.</summary>
        Mehrschale,

        /// <summary>Die Datei nennt keine Dicke; extrudiert ist mit der Vorgabedicke der Bauteilart (17.3 Nr. 2).</summary>
        Vorgabedicke,

        /// <summary>Die Bezugsebene ließ sich nicht am Raumkörper messen; extrudiert ist beidseitig um die Achse (17.3 Nr. 3).</summary>
        Bezugsebene_angenommen,
    }

    /// <summary>
    /// Die Herkunft eines Dateikörpers (Datenaustauschkonzept 17.5): sprachneutral, nie gespeichert.
    /// </summary>
    internal enum Koerperquelle
    {
        /// <summary>Der Körper steht so in der Datei (IFC) — Vorgabe.</summary>
        Datei,

        /// <summary>Der Kern hat ihn aus den Flächen der Datei gebildet (<see cref="Koerperbildner"/>) — kein unabhängiger Beleg.</summary>
        AusFlaechen,
    }

    /// <summary>
    /// <b>Der Körper eines Raums, wie die Datei ihn zeichnet</b> (Datenaustauschkonzept 15.3, Stufe G7f-1): ein
    /// Dreiecksnetz in Weltkoordinaten [m] — x Ost, y Nord des Modells, z oben; die Punkte bleiben im Modellsystem,
    /// so decken sich Körper und Grundriss —, je Dreieck die Normale (vom Raum weg, wo die Datei das hergibt), dazu
    /// die Randkanten der Ursprungsflächen ohne Triangulationsdiagonalen für die Linien der Ansicht.
    ///
    /// <para><b>Anzeige, dazu die Nachbarschaft:</b> Der Körper speist weder Fläche noch Volumen der Räume; die rechnen
    /// weiter aus Mengen, Raumbezügen und Raumgrenzen. Ohne Raumgrenzen kommen allein die Trennflächen zwischen Räumen aus
    /// den gemeinsamen Flächen der Körper (<see cref="Koerpernachbarschaft"/>). Nichts wird geschlossen oder repariert
    /// (15.6 Nr. 2).</para>
    ///
    /// <para><b>Deterministisch:</b> Punkte auf <see cref="Zonenkoerper.STELLEN"/> Nachkommastellen gerundet und in
    /// der Reihenfolge ihres ersten Auftretens geführt; dieselbe Datei ergibt dasselbe Netz, byteweise
    /// (<see cref="Text"/>).</para>
    /// </summary>
    internal sealed class Dateikoerper
    {
        /// <summary>
        /// Die Dreiecksgrenze je Gebäude (15.4): Darüber zeigt die Ansicht für alle Räume das Prisma aus dem Umriss.
        /// Der Kern zählt nur (<see cref="Zonengeometrie.DateikoerperDreiecke"/>); die Grenze wendet die Ansicht an.
        /// </summary>
        internal const int DREIECKSGRENZE = 300_000;

        /// <summary>Die Sehnen eines Vollkreises; ein Bogen bekommt seinen Anteil, mindestens zwei (15.2).</summary>
        internal const int SEHNEN_VOLLKREIS = 32;

        /// <summary>Die Punkte [m], je Punkt (x, y, z), gerundet auf <see cref="Zonenkoerper.STELLEN"/>.</summary>
        internal IReadOnlyList<double[]> PunkteM { get; init; } = Array.Empty<double[]>();

        /// <summary>Die Dreiecke als Indextripel in <see cref="PunkteM"/>; der Umlauf folgt der Normalen (rechte Hand).</summary>
        internal IReadOnlyList<int[]> Dreiecke { get; init; } = Array.Empty<int[]>();

        /// <summary>Die Einheitsnormale je Dreieck (x, y, z), gerundet auf <see cref="Zonenkoerper.STELLEN"/>.</summary>
        internal IReadOnlyList<double[]> Normalen { get; init; } = Array.Empty<double[]>();

        /// <summary>Die Randkanten der Ursprungsflächen als Indexpaare (kleinerer Index zuerst), ohne Doppel.</summary>
        internal IReadOnlyList<int[]> Randkanten { get; init; } = Array.Empty<int[]>();

        /// <summary>
        /// Die gelesene Darstellungsart als sprachneutraler Schlüssel: der EXPRESS-Name ohne „Ifc“ (etwa
        /// <c>FacetedBrep</c>, <c>ExtrudedAreaSolid</c>, <c>TriangulatedFaceSet</c>, <c>MappedItem</c>); mehrere Träger
        /// verschiedener Art mit „+“ in der Reihenfolge der Datei.
        /// </summary>
        internal string Art { get; init; } = "";

        /// <summary>Die Vermerke, aufsteigend und ohne Doppel; leer = exakt gelesen.</summary>
        internal IReadOnlyList<Koerpervermerk> Vermerke { get; init; } = Array.Empty<Koerpervermerk>();

        /// <summary>Die Herkunft des Körpers; <see cref="Koerperquelle.Datei"/>, solange nichts anderes gesetzt ist.</summary>
        internal Koerperquelle Quelle { get; init; } = Koerperquelle.Datei;

        /// <summary>
        /// Je Dreieck die Kennung seiner Quellfläche (Fläche bzw. Bauteil der Datei), gleich lang wie <see cref="Dreiecke"/>
        /// bei einem gebildeten Körper (17.4); leer bei einem Körper aus der Datei.
        /// </summary>
        internal IReadOnlyList<string> Quellflaechen { get; init; } = Array.Empty<string>();

        /// <summary>Die Zahl der Dreiecke.</summary>
        internal int DreieckZahl => Dreiecke.Count;

        /// <summary>Ist der Körper vereinfacht gelesen (mindestens ein Vermerk)?</summary>
        internal bool Vereinfacht => Vermerke.Count > 0;

        /// <summary>
        /// Die Ausgabe des Körpers in invarianter Kultur (Probe 25 und 28): Art, Vermerke, Punkte, Dreiecke mit
        /// Normalen und Randkanten, eine Zeile je Eintrag — byteweise gleich bei gleicher Datei.
        /// </summary>
        internal string Text()
        {
            var t = new StringBuilder();
            t.Append("Art ").Append(Art).Append('\n');
            t.Append("Vermerke ").Append(string.Join(",", Vermerke.Select(v => v.ToString()))).Append('\n');
            // Ein Körper aus der Datei schreibt weder Quelle noch Quellflächen — seine Ausgabe bleibt byteweise wie zuvor.
            if (Quelle != Koerperquelle.Datei) t.Append("Quelle ").Append(Quelle.ToString()).Append('\n');
            bool mitQuelle = Quellflaechen.Count == Dreiecke.Count && Quellflaechen.Count > 0;
            foreach (double[] p in PunkteM) t.Append("P ").Append(Z(p[0])).Append(' ').Append(Z(p[1])).Append(' ').Append(Z(p[2])).Append('\n');
            for (int i = 0; i < Dreiecke.Count; i++)
            {
                int[] d = Dreiecke[i];
                double[] n = Normalen[i];
                t.Append("D ").Append(G(d[0])).Append(' ').Append(G(d[1])).Append(' ').Append(G(d[2]))
                 .Append(" N ").Append(Z(n[0])).Append(' ').Append(Z(n[1])).Append(' ').Append(Z(n[2]));
                if (mitQuelle) t.Append(" Q ").Append(Quellflaechen[i]);
                t.Append('\n');
            }
            foreach (int[] k in Randkanten) t.Append("K ").Append(G(k[0])).Append(' ').Append(G(k[1])).Append('\n');
            return t.ToString();
        }

        private static string Z(double v) => v.ToString("R", CultureInfo.InvariantCulture);

        private static string G(int v) => v.ToString(CultureInfo.InvariantCulture);

        public override string ToString()
            => Art + " (" + DreieckZahl.ToString(CultureInfo.InvariantCulture) + " Dreiecke"
               + (Vermerke.Count > 0 ? ", " + string.Join(",", Vermerke) : "") + ")";
    }
}
