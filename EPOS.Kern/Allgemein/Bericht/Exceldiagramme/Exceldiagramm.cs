using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Wie eine Reihe eines Excel-Diagramms gezeichnet wird (Konzept Berichtsvorlagen 7.4, BV-Q11).</summary>
    internal enum Excelreihenart
    {
        /// <summary>Fläche — im Diagramm gestapelt, wenn <see cref="Exceldiagramm.Gestapelt"/>.</summary>
        Flaeche,

        /// <summary>Säule (senkrecht); gestapelt, wenn <see cref="Exceldiagramm.Gestapelt"/>.</summary>
        Saeule,

        /// <summary>Balken (waagerecht); gestapelt, wenn <see cref="Exceldiagramm.Gestapelt"/>.</summary>
        Balken,

        /// <summary>Linie ohne Punkte.</summary>
        Linie,

        /// <summary>Kreis — nur als einzige Reihe eines Diagramms.</summary>
        Kreis,
    }

    /// <summary>Die Strichart einer Linie (<see cref="ChartRenderer.Strichart"/>, ins Excel-Diagramm übertragen).</summary>
    internal enum Excelstrich
    {
        /// <summary>Durchgezogen.</summary>
        Durchgezogen,

        /// <summary>Gestrichelt.</summary>
        Gestrichelt,

        /// <summary>Gepunktet.</summary>
        Gepunktet,
    }

    /// <summary>
    /// Eine Reihe eines <see cref="Exceldiagramm"/>s: Name, Werte, Art, Farbe (sRGB <c>RRGGBB</c> aus den Farbrollen des
    /// <see cref="ChartRenderer"/>), Strichart und Stärke. Eine <see cref="Hilfsreihe"/> trägt eine gerechnete Hilfsgröße
    /// (die unsichtbare Basis eines Schwebebalkens, der Teil unter null) — ihre Spalte steht im Datenbereich rechts der
    /// Zahlen des Bildes, ihr Legendeneintrag entfällt.
    /// </summary>
    internal sealed class Excelreihe
    {
        internal Excelreihe(string name, double?[] werte, Excelreihenart art, string farbe)
        {
            Name = name ?? "";
            Werte = werte ?? Array.Empty<double?>();
            Art = art;
            Farbe = farbe;
        }

        /// <summary>Der Name der Reihe (Kopf ihrer Spalte, Legendeneintrag).</summary>
        internal string Name { get; }

        /// <summary>Die Werte, einer je Kategorie; <c>null</c> = leere Zelle (Lücke, nie 0).</summary>
        internal double?[] Werte { get; }

        /// <summary>Die Art der Reihe.</summary>
        internal Excelreihenart Art { get; }

        /// <summary>Die Farbe als sRGB-Hexwert <c>RRGGBB</c>; <c>null</c> = Excel wählt.</summary>
        internal string Farbe { get; }

        /// <summary>Die Deckung 0–255 der Farbe (255 = deckend).</summary>
        internal byte Deckung { get; set; } = 255;

        /// <summary>Die Strichart einer Linie.</summary>
        internal Excelstrich Strich { get; set; }

        /// <summary>Die Linienstärke in Punkt; <c>0</c> = Vorgabe (1,5 pt).</summary>
        internal double Staerke { get; set; }

        /// <summary>Eine Hilfsreihe: ohne Legendeneintrag, ihre Spalte hinter den Zahlen des Bildes.</summary>
        internal bool Hilfsreihe { get; set; }

        /// <summary>Unsichtbar gefüllt (die Basis eines Schwebebalkens).</summary>
        internal bool Unsichtbar { get; set; }

        /// <summary>Ohne Legendeneintrag (Hilfsreihen, der zweite Teil eines Abschnitts).</summary>
        internal bool OhneLegende { get; set; }

        /// <summary>
        /// Nur Zahlen, keine Reihe des Diagramms: die Werte des Bildes, die das Diagramm über Hilfsreihen zeichnet (die Beiträge
        /// der Brücke, die drei Szenariowerte der Spanne). Ihre Spalte steht im Datenbereich vor den Hilfsspalten.
        /// </summary>
        internal bool NurDaten { get; set; }

        /// <summary>Die Farbe je Punkt (Kreissegment, hervorgehobener Balken); <c>null</c> = die Farbe der Reihe.</summary>
        internal string[] Punktfarben { get; set; }
    }

    /// <summary>
    /// <b>Ein Excel-Diagramm mit seinen Zahlen</b> (Konzept Berichtsvorlagen 4.6 BV-P5, 7.4; Entscheid BV-Q11; Etappe BV-E8):
    /// der Titel des Berichtsbilds, die Kategorien (Tage, Stunden, Monate, Jahre, Stände, Schritte) und die Reihen mit den
    /// Zahlen, die das Bild zeichnet. Der <see cref="Diagrammplan"/> schreibt Kategorien und Reihen als Datenbereich ins Blatt
    /// „Diagrammdaten“, der <see cref="Exceldiagrammschreiber"/> setzt das Diagramm mit Reihenbezügen auf diese Zellen.
    /// Plattformfrei und ohne Datenbank; gebaut von <see cref="Exceldiagrammquellen"/>.
    /// </summary>
    internal sealed class Exceldiagramm
    {
        internal Exceldiagramm(string schluessel, string titel)
        {
            Schluessel = schluessel ?? "";
            Titel = titel ?? "";
        }

        /// <summary>Der Bildschlüssel des Katalogs (<c>stand.bild.waerme_dauerlinie</c> …).</summary>
        internal string Schluessel { get; }

        /// <summary>Der Titel — der Titel, den das Berichtsbild zeichnet (<see cref="Berichtsbilder.Titel"/>).</summary>
        internal string Titel { get; }

        /// <summary>Wovon das Diagramm handelt, wenn der Titel es nicht sagt: der Stand; sonst leer.</summary>
        internal string Bezug { get; set; } = "";

        /// <summary>Der Kopf der Kategorienspalte (Achsentitel der Rubrikenachse).</summary>
        internal string Kategorienkopf { get; set; } = "";

        /// <summary>Die Kategorien: Texte oder Zahlen (<see cref="string"/> bzw. <see cref="double"/>).</summary>
        internal List<object> Kategorien { get; } = new List<object>();

        /// <summary>Der Titel der Wertachse (Einheit); leer = keiner.</summary>
        internal string Wertachse { get; set; } = "";

        /// <summary>Das Zahlenformat der Werte (Zellen und Achse).</summary>
        internal string Zahlformat { get; set; } = "#,##0";

        /// <summary>Stapeln Säulen, Balken und Flächen?</summary>
        internal bool Gestapelt { get; set; }

        /// <summary>Überdecken sich die Säulen einer Kategorie ganz (Schwebebalken)?</summary>
        internal bool Ueberdeckt { get; set; }

        /// <summary>Die erste Kategorie oben (waagerechte Balken in Berichtsfolge).</summary>
        internal bool KategorienVonOben { get; set; }

        /// <summary>Jede n-te Kategorie beschriften (lange Raster); <c>0</c> = Excel entscheidet.</summary>
        internal int Beschriftungsabstand { get; set; }

        /// <summary>Die Reihen in Zeichenfolge.</summary>
        internal List<Excelreihe> Reihen { get; } = new List<Excelreihe>();

        /// <summary>Hängt eine Reihe an und gibt sie zurück.</summary>
        internal Excelreihe Reihe(string name, IEnumerable<double?> werte, Excelreihenart art, string farbe)
        {
            var r = new Excelreihe(name, (werte ?? Enumerable.Empty<double?>()).ToArray(), art, farbe);
            Reihen.Add(r);
            return r;
        }

        /// <summary>Die Zahl der Datenzeilen: die Kategorien, mindestens die längste Reihe.</summary>
        internal int Zeilen
        {
            get { return Math.Max(Kategorien.Count, Reihen.Count == 0 ? 0 : Reihen.Max(r => r.Werte.Length)); }
        }

        /// <summary>Die Reihen ohne Hilfsreihen — die Zahlen des Bildes.</summary>
        internal IEnumerable<Excelreihe> Bildreihen { get { return Reihen.Where(r => !r.Hilfsreihe); } }

        /// <summary>Die Reihen in Spaltenfolge des Datenbereichs: erst die Zahlen des Bildes, dann die Hilfsreihen.</summary>
        internal List<Excelreihe> Spaltenfolge()
        {
            return Reihen.Where(r => !r.Hilfsreihe).Concat(Reihen.Where(r => r.Hilfsreihe)).ToList();
        }

        /// <summary>Ist ein Wert endlich?</summary>
        internal static double? Endlich(double w)
        {
            return double.IsNaN(w) || double.IsInfinity(w) ? (double?)null : w;
        }

        /// <summary>Die endlichen Werte einer Zahlenreihe, nicht endliche als Lücke.</summary>
        internal static IEnumerable<double?> Endlich(IEnumerable<double> werte)
        {
            return (werte ?? Enumerable.Empty<double>()).Select(Endlich);
        }
    }
}
