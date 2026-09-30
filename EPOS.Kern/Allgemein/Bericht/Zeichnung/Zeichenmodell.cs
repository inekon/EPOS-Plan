using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApplication1.Zeichnung
{
    // =========================================================================
    // DAS ZEICHENMODELL (Konzept Diagramme, Etappe E1)
    //
    // Ein Bild ist eine Flaeche (Breite, Hoehe, Hintergrund) und eine LISTE VON
    // BEFEHLEN in Zeichenreihenfolge. Die Befehle tragen fertige Koordinaten in
    // Bildpunkten; gerechnet wird VOR dem Befehl, im Layout des Renderers. Damit
    // kann derselbe Inhalt zweimal ausgegeben werden: als PNG ueber den
    // SkiaMaler (Bericht) und spaeter als SVG (Bildschirm).
    //
    // DAS MODELL KENNT KEINE SKIA-TYPEN. Es hat eigene Werttypen fuer Farbe,
    // Punkt und Rahmen; die Bruecke nach SkiaSharp steht in SkiaBruecke.cs.
    // Nur so kann ein Ausgabeweg ohne Skia (SVG) dasselbe Modell lesen.
    //
    // Die Textvermessung bleibt eine Kern-Funktion und gehoert NICHT ins
    // Modell: Wer einen Text mittig setzen will, misst ihn vorher ueber die
    // Schriftkette und gibt die fertige Koordinate in den Befehl.
    //
    // FARBEN stehen als Farbton (Rolle + Abwandlung) im Befehl, nie als nackte
    // Zahl; aufgeloest wird beim Malen gegen Farbpalette.Aktuell. Siehe
    // Farbpalette.cs.
    // =========================================================================

    /// <summary>Ein Punkt in Bildpunkten — der Skia-freie Ersatz für <c>SKPoint</c>.</summary>
    public readonly record struct Punkt(float X, float Y);

    /// <summary>Ein achsparalleles Rechteck in Bildpunkten — der Ersatz für <c>SKRect</c>.</summary>
    public readonly record struct Rahmen(float X, float Y, float Breite, float Hoehe)
    {
        public float Rechts => X + Breite;
        public float Unten => Y + Hoehe;
    }

    /// <summary>
    /// <b>Eine Stufe der Stufenregel des Stapels</b> (<see cref="Pfadregel.Stufen"/>): die
    /// Werte <paramref name="Von"/> bis <paramref name="Bis"/> eines Ausschnitts (Index,
    /// einschließlich), gezeichnet als EINE waagrechte Kante von <paramref name="Links"/>
    /// bis <paramref name="Rechts"/> — beides Anteile 0 … 1 der Breite des Ausschnitts.
    /// </summary>
    public readonly record struct Stufe(int Von, int Bis, double Links, double Rechts);

    /// <summary>
    /// <b>Wie eine Reihe in einer Ansicht zusammengefasst ist</b> (Stufenregel des Stapels,
    /// <c>SvgSchreiber.Stufenansicht</c>): gar nicht, je Tag oder je Bildpunktspalte. Die
    /// Oberfläche nennt danach unter der Achse, welche Stunde eine Stufe zeigt.
    /// </summary>
    public enum Stufenart
    {
        /// <summary>Nicht in Stufen — jede Stützstelle (oder gebündelt) steht im Pfad.</summary>
        Keine = 0,

        /// <summary>Je Tag die Spitzenstunde des Tages (Jahresbild).</summary>
        Tag = 1,

        /// <summary>Je Bildpunktspalte die Spitzenstunde der Spalte (Ausschnitt).</summary>
        Spalte = 2
    }

    /// <summary>Die Form des Strichendes (Skia: <c>SKStrokeCap</c>).</summary>
    public enum Strichkappe { Stumpf = 0, Rund = 1, Quadratisch = 2 }

    /// <summary>Die Form der Streckenverbindung (Skia: <c>SKStrokeJoin</c>).</summary>
    public enum Strichverbindung { Gehrung = 0, Rund = 1, Fase = 2 }

    /// <summary>Wie ein Text an seiner x-Koordinate hängt.</summary>
    public enum Ausrichtung { Links = 0, Mitte = 1, Rechts = 2 }

    /// <summary>
    /// Ein Strichmuster: <paramref name="Strich"/> Bildpunkte Linie,
    /// <paramref name="Luecke"/> Bildpunkte Lücke, ab <paramref name="Versatz"/>.
    /// Alle Muster des Bestands sind zweiteilig mit Versatz 0.
    /// </summary>
    public sealed record Strichmuster(float Strich, float Luecke, float Versatz = 0f);

    /// <summary>Ein Stift (Skia: <c>SKPaint</c> im Stroke-Stil).</summary>
    public sealed record Stift(
        Farbton Ton,
        float Breite,
        Strichmuster Muster = null,
        Strichkappe Kappe = Strichkappe.Stumpf,
        Strichverbindung Verbindung = Strichverbindung.Gehrung,
        bool Antialias = true);

    /// <summary>Eine Flächenfarbe (Skia: <c>SKPaint</c> im Fill-Stil).</summary>
    public sealed record Fuellung(Farbton Ton, bool Antialias = true);

    /// <summary>
    /// Eine Schrift in PUNKT — so, wie der Bestand sie führt. Die Umrechnung nach
    /// Bildpunkten und die Wahl der Schriftart macht die <see cref="Schriftkette"/>.
    /// </summary>
    public sealed record Schrift(float Punkt, bool Fett = false, bool Kursiv = false);

    // ----------------------------------------------------------------- Befehle

    /// <summary>
    /// Ein einzelner Zeichenbefehl.
    ///
    /// <para><b>Die MARKE (Etappe E2).</b> Ein Befehl darf sagen, WOZU er gehört —
    /// <c>"titel"</c>, <c>"xachse"</c>, <c>"yachse"</c>, <c>"yachse2"</c> (die RECHTE
    /// Achse, Etappe E3), <c>"reihe:&lt;Name&gt;"</c>,
    /// <c>"legende:&lt;Name&gt;"</c>, <c>"nulllinie"</c>, <c>"leerhinweis"</c>. Der
    /// <c>SkiaMaler</c> ÜBERGEHT sie; das PNG bleibt deshalb byte-gleich. Der
    /// <see cref="SvgSchreiber"/> gibt sie als <c>data-marke</c> weiter, und die
    /// Oberfläche schaltet damit Gruppen ein und aus (Legendenwahl) oder blendet
    /// beim Zoom die Achsenteilung aus und zeichnet sie neu.</para>
    ///
    /// <para><c>null</c> heißt „keine Marke" und ist die Vorgabe — jeder Befehl des
    /// Bestands bleibt damit wörtlich, wie er war.</para>
    ///
    /// <para><b>Der WERT am Element (Etappe E3, Entscheid DG-E3-6).</b> Neben der
    /// Marke darf ein Befehl den FERTIG FORMATIERTEN Text tragen, den die Oberfläche
    /// beim Zeigen auf das Element anzeigt — „Jan · Wärmepumpe: 12,5 MWh",
    /// „Kapazität 220 kWh · Entladeleistung 100 kW: 4.000 €", „Wärmepumpe: 48,0 %".
    /// Der <c>SkiaMaler</c> übergeht ihn wie die Marke (das PNG bleibt byte-gleich);
    /// der <see cref="SvgSchreiber"/> schreibt ihn als <c>data-wert</c> an dieselbe
    /// Stelle wie <c>data-marke</c>.</para>
    ///
    /// <para><b>Warum fertiger Text und keine Zahl.</b> Formatiert wird dort, wo auch
    /// die Beschriftung des Bildes entsteht — in der Kultur des Renderers und mit
    /// denselben Nachkommastellen. Eine nackte Zahl im Modell müsste die Oberfläche
    /// ein zweites Mal formatieren, und Bild und Zeigetext gingen auseinander.</para>
    /// </summary>
    public abstract record Zeichenbefehl
    {
        /// <summary>Wozu der Befehl gehört; <c>null</c> = keine Marke.</summary>
        public string Marke { get; init; }

        /// <summary>
        /// Der fertig formatierte Wert am Element (DG-E3-6); <c>null</c> = keiner,
        /// und das Attribut <c>data-wert</c> bleibt dann weg.
        /// </summary>
        public string Wert { get; init; }
    }

    /// <summary>Eine Strecke.</summary>
    public sealed record Linie(float X1, float Y1, float X2, float Y2, Stift Stift) : Zeichenbefehl;

    /// <summary>
    /// Ein Rechteck. Liegt beides an, wird ZUERST gefüllt und DANN umrandet —
    /// dieselbe Reihenfolge, in der der Bestand die zwei Aufrufe absetzt.
    /// </summary>
    public sealed record Rechteck(float X, float Y, float Breite, float Hoehe,
                                  Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>Ein Kreis um (<paramref name="X"/>, <paramref name="Y"/>).</summary>
    public sealed record Kreis(float X, float Y, float Radius,
                               Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>Eine Ellipse im umschließenden Rechteck.</summary>
    public sealed record Ellipse(float X, float Y, float Breite, float Hoehe,
                                 Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Kreissegment (Tortenstück) im umschließenden Rechteck, von
    /// <paramref name="Startwinkel"/> über <paramref name="Winkel"/> Grad.
    /// Ein Winkel ab 360° ist die volle <see cref="Ellipse"/> — Skia zieht sonst
    /// nichts (Befund zu Auftrag #222).
    /// </summary>
    public sealed record Kreissegment(float X, float Y, float Breite, float Hoehe,
                                      float Startwinkel, float Winkel,
                                      Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Streckenzug (<c>Geschlossen = false</c>) oder ein Vieleck
    /// (<c>Geschlossen = true</c>).
    /// </summary>
    public sealed record Pfad(Wertliste<Punkt> Punkte, bool Geschlossen,
                              Stift Rand = null, Fuellung Fuellung = null) : Zeichenbefehl;

    /// <summary>
    /// Ein Text an seiner linken OBEREN Ecke (dieselbe Bezugsecke wie im Bestand).
    /// Der Ausgabeweg setzt ihn auf die Grundlinie um.
    /// </summary>
    public sealed record Text(string Inhalt, float X, float Y, Schrift Schrift, Farbton Ton,
                              Ausrichtung Ausrichtung = Ausrichtung.Links) : Zeichenbefehl;

    /// <summary>
    /// Eine Gruppe von Befehlen, wahlweise auf ein Rechteck zugeschnitten
    /// (Skia: <c>Save</c>/<c>ClipRect</c>/<c>Restore</c>).
    /// </summary>
    public sealed record Gruppe(Rahmen? Zuschnitt, Wertliste<Zeichenbefehl> Befehle) : Zeichenbefehl;

    // ------------------------------------------------- Zeichenflaeche und Reihen

    /// <summary>
    /// Was auf einer Zeichenfläche links, rechts, unten und oben liegt — in
    /// DATENKOORDINATEN, nicht in Bildpunkten.
    /// </summary>
    /// <param name="XVon">Erste Stützstelle des Bildes (Jahresstunde als Index der Reihe).</param>
    /// <param name="XBis">Letzte Stützstelle des Bildes (einschließlich).</param>
    /// <param name="YVon">Unterkante der Fläche in Werteinheiten.</param>
    /// <param name="YBis">Oberkante der Fläche in Werteinheiten.</param>
    public sealed record Datenfenster(double XVon, double XBis, double YVon, double YBis);

    /// <summary>
    /// <b>Was die x-Achse ZÄHLT (Entscheid DG-E3-4).</b> Aus ihr folgt, wie
    /// <c>ChartRenderer.Achsenteilung</c> einen Ausschnitt teilt, wenn die Oberfläche
    /// beim Zoom die Marken der Achse neu setzt.
    /// </summary>
    public enum Achsenart
    {
        /// <summary>
        /// Die JAHRESSTUNDE (bzw. die Stützstelle einer Zeitreihe) — die Vorgabe und
        /// der Fall jedes Zeitreihenbildes. Geteilt wird über
        /// <c>ChartRenderer.Jahresstundenteilung</c>.
        /// </summary>
        Stunden = 0,

        /// <summary>
        /// Der INDEX der Reihe, 0 … n−1 — das Kostenprofil und das Stundenprofil legen
        /// ihre Reihe über deren EIGENE Länge auf eine feste Achse. Geteilt wird
        /// ganzzahlig.
        /// </summary>
        Index = 1,

        /// <summary>
        /// Eine freie GRÖSSE mit eigener Einheit: das Projektjahr, die Außentemperatur,
        /// die Kapazität. Geteilt wird mit denselben „schönen" Stufen
        /// (1 / 2 / 2,5 / 5 × 10^k), mit denen das Bild seine Achse setzt.
        /// </summary>
        Wert = 2
    }

    /// <summary>
    /// Die Zeichenfläche eines Bildes: ihr Pixelrechteck und das Datenfenster, das
    /// darin steht.
    ///
    /// <para><b>Wozu.</b> Die Befehlsliste trägt fertige Bildpunkte — daraus ist nicht
    /// mehr abzulesen, welche Stunde an welcher Stelle steht. Der SVG-Weg braucht
    /// genau das: Er setzt die Fläche als INNERES <c>&lt;svg&gt;</c> in
    /// Datenkoordinaten, und Zoom und Verschieben sind dann eine Änderung seiner
    /// <c>viewBox</c> — ohne Neuzeichnen und ohne Rundlauf.</para>
    /// </summary>
    /// <param name="Bild">Das Pixelrechteck der Fläche im Bild.</param>
    /// <param name="Daten">Das Datenfenster der LINKEN Achse (die Vorgabe jeder Reihe).</param>
    /// <param name="X">
    /// Was die x-Achse zählt (DG-E3-4); <see cref="Achsenart.Stunden"/> ist die Vorgabe
    /// und lässt jedes Zeitreihenbild, wie es war.
    /// </param>
    /// <param name="XEinheit">
    /// Die Einheit der x-Größe für die Achsenbeschriftung eines Ausschnitts — „°C",
    /// „kWh", „a"; <c>null</c> = keine (eine Stunden- oder Indexachse braucht keine,
    /// und ein Bild, dessen x-Größe je Aufruf wechselt, nennt sie nicht).
    /// </param>
    public sealed record Zeichenflaeche(Rahmen Bild, Datenfenster Daten,
                                        Achsenart X = Achsenart.Stunden,
                                        string XEinheit = null);

    /// <summary>
    /// <b>Auf WELCHER y-Achse eine Reihe steht (Entscheid DG-E3-12).</b>
    ///
    /// <para>Bis hierher musste die Oberfläche es RATEN: Sie verglich das y-Fenster der
    /// Reihe mit dem der Zeichenfläche und nannte jede Reihe mit abweichender Spanne
    /// „rechts". Das trifft zu, solange die rechte Achse eine andere Spanne hat — und
    /// geht fehl, sobald beide zufällig dieselbe führen. Wer die Achse kennt, sagt sie:
    /// Der Renderer setzt sie in <c>ErzeugerStapelModell</c> und
    /// <c>SpeicherbetriebModell</c>, und die Zeigerzeile liest sie, statt zu rechnen.</para>
    /// </summary>
    public enum Achsenseite
    {
        /// <summary>Die LINKE Achse — die Vorgabe und der Fall jeder Reihe ohne zweite Achse.</summary>
        Links = 0,

        /// <summary>
        /// Die ZWEITE, RECHTE Achse (Marke <c>yachse2</c>): der Speicherinhalt in kWh
        /// neben Leistungen in kW.
        /// </summary>
        Rechts = 1
    }

    /// <summary>
    /// Wie eine <see cref="Datenreihe"/> im SVG gezeichnet wird (Entscheid DG-E3-2).
    /// </summary>
    public enum Reihenart
    {
        /// <summary>Ein Streckenzug — der Regelfall jeder Ganglinie.</summary>
        Linie = 0,

        /// <summary>
        /// Eine gefüllte Fläche zwischen <see cref="Datenreihe.Unten"/> und
        /// <see cref="Datenreihe.Werte"/> — eine Schicht des Stapels oder das
        /// Profilband.
        /// </summary>
        Flaeche = 1,

        /// <summary>
        /// <b>Eine PUNKTWOLKE (Entscheid DG-E3-5).</b> Jeder Wert steht an seiner
        /// eigenen x-Stelle (<see cref="Datenreihe.XWerte"/>) und wird als PUNKT
        /// gezeichnet, nicht verbunden — die Streuwolke „Leistung über
        /// Außentemperatur".
        ///
        /// <para>Im SVG wird daraus EIN <c>&lt;path&gt;</c> aus Segmenten
        /// <c>M x,y h 0</c> mit runder Strichkappe; die Strichbreite ist der
        /// Punktdurchmesser des PNG. <b>Gebündelt wird nicht</b>: Eine Bündelung je
        /// Bildpunktspalte nähme genau die Verdichtung weg, die die Aussage der Wolke
        /// ist, und 8 760 Punkte sind EIN Knoten.</para>
        /// </summary>
        Punkte = 2
    }

    /// <summary>
    /// Eine Reihe des Bildes in DATENWERTEN — ungekürzt, Stützstelle für Stützstelle.
    ///
    /// <para><b>Warum neben dem Linienzug.</b> Der Pixelpfad im Befehl ist auf jeden
    /// n-ten Wert gekürzt (so zeichnet das PNG seit je). Der SVG-Weg zeichnet die
    /// Reihe stattdessen aus DIESEN Werten, nach <see cref="Pfadregel"/> roh oder
    /// gebündelt; damit zeigt ein Zoom die echte Stunde statt der Stützstellen der
    /// Schrittweite. Entscheid DG-E2-2.</para>
    /// </summary>
    /// <param name="Name">Der Name der Reihe — zugleich der Schlüssel der Legende.</param>
    /// <param name="Ton">
    /// Die Farbe als Rolle; aufgelöst wird beim Schreiben. Bei einer
    /// <see cref="Reihenart.Flaeche"/> ist es die FÜLLfarbe.
    /// </param>
    /// <param name="Staerke">Die Strichstärke [px], wie sie das PNG zeichnet.</param>
    /// <param name="Muster">Das Strichmuster oder <c>null</c>.</param>
    /// <param name="Werte">Die Werte des BILDES — bei einem Fenster die zugeschnittenen.</param>
    /// <param name="Fenster">
    /// <b>DAS EIGENE DATENFENSTER der Reihe (Entscheid DG-E3-1).</b> <c>XVon</c>/<c>XBis</c>
    /// sagen, wo die erste und die letzte Stützstelle in der Zeichenfläche stehen,
    /// <c>YVon</c>/<c>YBis</c> nennen die Grenzen IHRER Achse. <c>null</c> = das Fenster
    /// der Zeichenfläche (die linke Achse).
    ///
    /// <para>Nur damit trägt ein Bild eine ZWEITE y-Achse: Der Speicherinhalt rechts hat
    /// seine eigene Skala und steht trotzdem im selben inneren <c>&lt;svg&gt;</c>. Und nur
    /// damit sitzt eine Reihe, die das PNG über einen ANDEREN x-Bereich zeichnet (das
    /// Stundenprofil, die nebeneinander gestellten Stapelgruppen), im SVG an derselben
    /// Stelle wie im Bild.</para>
    /// </param>
    /// <param name="Art">Linie oder gefüllte Fläche (DG-E3-2).</param>
    /// <param name="Unten">
    /// Die UNTERKANTE einer Fläche, Stützstelle für Stützstelle — die Summe der
    /// Schichten darunter. <c>null</c> = die Achsennull, in das Fenster geklemmt.
    /// Bei einer <see cref="Reihenart.Linie"/> zeichnet sie nichts: Dort ist die Linie die
    /// KANTE einer überlagerten Stapelschicht (<c>Werte</c> die Summe bis einschließlich der
    /// Schicht), und <c>Unten</c> die Summe darunter — die Oberfläche nennt am Zeiger die
    /// Differenz, den Beitrag der Schicht.
    /// </param>
    /// <param name="Randton">
    /// Die Farbe der RANDLINIE einer Fläche; <c>null</c> = ohne Strich. Sie steht neben
    /// <paramref name="Ton"/>, weil das PNG die Randlinie des Profilbands in einer
    /// ANDEREN Farbe zieht als die Füllung — eine Fläche mit Rand wird im SVG so
    /// gezeichnet, wie das Bild sie zeichnet.
    /// </param>
    /// <param name="XWerte">
    /// <b>Die x-Stelle JE WERT (Entscheid DG-E3-5)</b>, in der Einheit der x-Achse;
    /// <c>null</c> = die Stützstellen liegen gleichmäßig von <c>Fenster.XVon</c> bis
    /// <c>Fenster.XBis</c> — der Regelfall jeder Zeitreihe.
    ///
    /// <para>Eine <see cref="Reihenart.Punkte"/> braucht sie zwingend (x = Temperatur,
    /// y = Leistung). Eine LINIE braucht sie, wo die Stützstellen ungleichmäßig
    /// liegen: Die Schnittkurve der Rastersuche mischt Grob- und Feinpunkte, und ohne
    /// die eigene x-Stelle säße jeder Feinpunkt an der falschen Kapazität.</para>
    /// </param>
    /// <param name="Einheit">
    /// Die Einheit der WERTE für die Zeigerzeile der Oberfläche — „kW", „€", „°C";
    /// <c>null</c> = keine. Sie steht an der REIHE und nicht an der Fläche, weil eine
    /// Reihe der zweiten Achse eine andere führt als die der linken (DG-E3-4).
    /// </param>
    /// <param name="Achsenseite">
    /// <b>Auf welcher y-Achse die Reihe steht (DG-E3-12)</b>; Vorgabe
    /// <see cref="Achsenseite.Links"/>. Der Renderer setzt
    /// <see cref="Achsenseite.Rechts"/> für die Reihen der zweiten Achse, damit die
    /// Oberfläche ihre Einheit nicht mehr aus der y-Spanne erraten muss.
    /// </param>
    /// <param name="Huelle">
    /// <b>Die Linie begleitet einen Stapel</b> (die Summe, die Kontur, der Bedarf über den
    /// Erzeugern). Führt sie mehr Stützstellen als die Zeichenfläche Bildpunktspalten hat,
    /// zeichnet sie je Stufe ihren Wert in der SPITZENSTUNDE der Stufe
    /// (<see cref="Pfadregel.Spitzenstunden"/>) — dieselbe Treppe wie die Kanten der
    /// Stapelschichten. So liegt sie als Kante auf dem Stapel, statt je Spalte die ganze
    /// Spanne ihres Zickzacks zu füllen und die Schichten zu überdecken. Vorgabe
    /// <c>false</c>: jede andere Linie bleibt, wie sie war.
    /// </param>
    /// <param name="Bezug">
    /// <b>Die Bezugsgröße der Stufenregel</b>, Stützstelle für Stützstelle wie
    /// <c>Werte</c>: Je Stufe zeigt eine Stapelschicht oder Hüllkurve ihren Wert in der
    /// Stunde, in der DIESE Reihe ihre Stufenspitze hat (<see cref="Pfadregel.Spitzenstunden"/>).
    /// Alle Schichten und Hüllkurven eines Bildes tragen dieselbe Bezugsgröße — die Summen-
    /// oder Bedarfslinie, sonst die Oberkante des Stapels —, damit alle dieselbe Stunde
    /// zeigen: Der Stapel bleibt je Stufe additiv, die Dicke einer Schicht ist ihr Wert in
    /// der Spitzenstunde. <c>null</c> (oder eine andere Länge als <c>Werte</c>) = die eigenen
    /// Werte.
    /// </param>
    public sealed record Datenreihe(string Name, Farbton Ton, float Staerke,
                                    Strichmuster Muster, double[] Werte,
                                    Datenfenster Fenster = null,
                                    Reihenart Art = Reihenart.Linie,
                                    double[] Unten = null,
                                    Farbton Randton = null,
                                    double[] XWerte = null,
                                    string Einheit = null,
                                    Achsenseite Achsenseite = Zeichnung.Achsenseite.Links,
                                    bool Huelle = false,
                                    double[] Bezug = null)
    {
        /// <summary>
        /// Wertgleichheit samt Werten. Ein Record vergliche <see cref="Werte"/> über
        /// die REFERENZ; zwei gleich gefüllte Reihen wären dann verschieden, und der
        /// Determinismusnachweis des Modells liefe ins Leere — derselbe Grund, aus dem
        /// die Punktfolgen in einer <see cref="Wertliste{T}"/> stehen. Dasselbe gilt
        /// seit DG-E3-2 für <see cref="Unten"/>, seit DG-E3-5 für
        /// <see cref="XWerte"/> und für die Bezugsgröße <see cref="Bezug"/>.
        /// </summary>
        public bool Gleicht(Datenreihe andere)
        {
            if (andere == null) return false;
            if (!string.Equals(Name, andere.Name, StringComparison.Ordinal)) return false;
            if (!Equals(Ton, andere.Ton) || Staerke != andere.Staerke) return false;
            if (!Equals(Muster, andere.Muster)) return false;
            if (!Equals(Fenster, andere.Fenster)) return false;
            if (Art != andere.Art) return false;
            if (!Equals(Randton, andere.Randton)) return false;
            if (!string.Equals(Einheit, andere.Einheit, StringComparison.Ordinal)) return false;
            if (Achsenseite != andere.Achsenseite) return false;
            if (Huelle != andere.Huelle) return false;
            if (!Werteliste(Werte, andere.Werte)) return false;
            if (!Werteliste(XWerte, andere.XWerte)) return false;
            if (!Werteliste(Bezug, andere.Bezug)) return false;
            return Werteliste(Unten, andere.Unten);
        }

        private static bool Werteliste(double[] a, double[] b)
        {
            if (a == null || b == null) return ReferenceEquals(a, b);
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!a[i].Equals(b[i])) return false;
            return true;
        }
    }

    /// <summary>
    /// <b>Die Pfadregel (Entscheid DG-Q5, „eine Konstante im Modell").</b> Wie viele
    /// Stützstellen eine Reihe in den SVG-Pfad bringt.
    ///
    /// <para>Bis <see cref="ROH_BIS_STUETZSTELLEN"/> Stützstellen und
    /// <see cref="ROH_BIS_REIHEN"/> Reihen geht der Pfad ROH — jede Stunde ein Punkt;
    /// der Zoom zeigt dann ohne Nachladen die echte Stunde. Darüber wird GEBÜNDELT:
    /// je Bildpunktspalte Minimum und Maximum in Indexreihenfolge, höchstens zwei
    /// Punkte je Spalte, bei 1:1 vom rohen Bild nicht zu unterscheiden und ohne
    /// verlorene Spitze (Konzept Diagramme § 5, Prüfstand vom 20.09.2026).</para>
    /// </summary>
    public static class Pfadregel
    {
        /// <summary>Bis hierher geht der Pfad roh — ein volles Stundenjahr.</summary>
        public const int ROH_BIS_STUETZSTELLEN = 8760;

        /// <summary>Und nur bis hierher: ab der vierten Reihe wird gebündelt.</summary>
        public const int ROH_BIS_REIHEN = 3;

        /// <summary>Geht die Reihe roh in den Pfad?</summary>
        public static bool Roh(int stuetzstellen, int reihen)
            => stuetzstellen <= ROH_BIS_STUETZSTELLEN && reihen <= ROH_BIS_REIHEN;

        /// <summary>
        /// Die gebündelten Punkte einer Reihe: je Bildpunktspalte das Minimum UND das
        /// Maximum, in INDEXREIHENFOLGE (erst das, was früher steht). Eine Spalte mit
        /// nur einem Extrem gibt einen Punkt.
        ///
        /// <para><c>Punkt.X</c> ist die Stunde (der Index in
        /// <paramref name="werte"/>), <c>Punkt.Y</c> der Wert — beides in
        /// DATENKOORDINATEN, nicht in Bildpunkten.</para>
        /// </summary>
        public static IReadOnlyList<Punkt> Gebuendelt(double[] werte, int spalten)
        {
            var punkte = new List<Punkt>();
            if (werte == null || werte.Length == 0) return punkte;
            if (spalten < 1) spalten = 1;

            int start = 0;
            for (int c = 1; c <= spalten; c++)
            {
                int ende = (int)((long)c * werte.Length / spalten);
                if (ende <= start) continue;

                int iMin = start, iMax = start;
                for (int i = start + 1; i < ende; i++)
                {
                    if (werte[i] < werte[iMin]) iMin = i;
                    if (werte[i] > werte[iMax]) iMax = i;
                }

                if (iMin == iMax) punkte.Add(new Punkt(iMin, (float)werte[iMin]));
                else if (iMin < iMax)
                {
                    punkte.Add(new Punkt(iMin, (float)werte[iMin]));
                    punkte.Add(new Punkt(iMax, (float)werte[iMax]));
                }
                else
                {
                    punkte.Add(new Punkt(iMax, (float)werte[iMax]));
                    punkte.Add(new Punkt(iMin, (float)werte[iMin]));
                }

                start = ende;
            }
            return punkte;
        }

        /// <summary>
        /// <b>Die gebündelte KANTE einer Fläche (Entscheid DG-E3-2).</b> Je
        /// Bildpunktspalte genau EIN Punkt: der Höchstwert, wenn
        /// <paramref name="hoechstwert"/> gesetzt ist (die Oberkante), sonst der
        /// Kleinstwert (die Unterkante). <c>Punkt.X</c> ist der Index, an dem das
        /// Extrem steht, <c>Punkt.Y</c> sein Wert — beides in DATENKOORDINATEN.
        ///
        /// <para><b>Warum ein Punkt je Spalte und nicht zwei.</b> Eine Fläche ist ein
        /// geschlossener Zug; zwei Punkte je Spalte ergäben an ihrer Oberkante ein
        /// Sägeblatt, das Fläche wegnähme, die im Bild steht. Höchstwert oben und
        /// Kleinstwert unten geben stattdessen die KONSERVATIVE HÜLLE: Sie ist nie
        /// kleiner als die rohe Fläche, und bei 1:1 ist sie von ihr nicht zu
        /// unterscheiden.</para>
        /// </summary>
        public static IReadOnlyList<Punkt> GebuendelteKante(double[] werte, int spalten,
                                                            bool hoechstwert)
        {
            var punkte = new List<Punkt>();
            if (werte == null || werte.Length == 0) return punkte;
            if (spalten < 1) spalten = 1;

            int start = 0;
            for (int c = 1; c <= spalten; c++)
            {
                int ende = (int)((long)c * werte.Length / spalten);
                if (ende <= start) continue;

                int treffer = start;
                for (int i = start + 1; i < ende; i++)
                    if (hoechstwert ? werte[i] > werte[treffer] : werte[i] < werte[treffer])
                        treffer = i;

                punkte.Add(new Punkt(treffer, (float)werte[treffer]));
                start = ende;
            }
            return punkte;
        }

        // =================================================================
        // DIE STUFENREGEL DES STAPELS
        //
        // Ein Stundenjahr hat 8 760 Werte, die Zeichenflaeche 1 100 Bildpunkt-
        // spalten: Auf jede Spalte fallen acht Stunden. Zeichnet eine Schicht
        // jede Stunde, zickzackt ihre Kante innerhalb der Spalte, und die
        // Kantenglaettung deckt dort jeden Bildpunkt nur zu dem Anteil der
        // Stunden, in denen die Schicht ihn erreicht - die Baender werden blass
        // und loechrig, eine Linie fuellt die ganze Spanne ihres Zickzacks.
        //
        // Die Regel: Eine Stapelschicht und eine Linie, die einen Stapel
        // begleitet (Datenreihe.Huelle), zeichnen bei mehr Werten als Bildpunkt-
        // spalten je STUFE eine waagrechte Kante - ihren Wert in der SPITZENSTUNDE
        // der Stufe. Die Spitzenstunde ist die Stunde, in der die BEZUGSGROESSE
        // des Bildes (Datenreihe.Bezug: die Summen- oder Bedarfslinie, sonst die
        // Oberkante des Stapels) in der Stufe am hoechsten steht, bei Gleichstand
        // die erste. ALLE Schichten und Linien eines Bildes zeigen je Stufe
        // dieselbe Stunde: Der Stapel bleibt additiv, die Dicke einer Schicht ist
        // ihr Wert in dieser Stunde, und die Oberkante trifft die Bezugslinie.
        // Je Schicht der eigene Hoechstwert waere nicht additiv - ein taktender
        // Erzeuger stuende als flaches Band auf Nennleistung da, auch ueber dem
        // Bedarf. Beide Kanten einer Schicht nehmen dieselbe Regel - die
        // Unterkante einer Schicht ist die Oberkante der darunter, Wert fuer Wert
        // dieselbe Zahl -, also teilen Nachbarschichten ihre Kante genau: keine
        // Luecke, keine Ueberlappung, jede Stufe bis zur Oberkante voll gedeckt.
        //
        // DIE STUFE IST DER TAG, solange ein Tag schmaler ist als TAG_MIN_SPALTEN
        // Bildpunktspalten - im Jahresbild rund drei. Je Spalte gebuendelt,
        // wechselten Nachbarspalten zwischen Tagesspitze und Tagestief (drei
        // Spalten je Tag), und der Stapel stuende als Streifen da. Ab vier Spalten
        // je Tag (etwa ab dem vierfachen Zoom) ist die Stufe die Bildpunktspalte:
        // Dann zeichnet sie den Tagesgang als Kurve ueber mehrere Stufen. Den Tag
        // kennen nur die festen Jahresraster (8 760 Stunden, 35 040
        // Viertelstunden); jede andere Reihe buendelt je Spalte.
        //
        // EINE LUECKE BLEIBT LUECKE: Ein nicht endlicher Wert (NaN - etwa "aus"
        // einer Kalenderwoche) waehlt keine Spitzenstunde; eine Stufe ohne
        // endlichen Bezugswert nimmt ihre erste Stunde. Steht eine Reihe in der
        // Spitzenstunde einer Stufe nicht endlich da - eine Schicht an Ober- oder
        // Unterkante -, ist die Stufe fuer sie eine LUECKE: Die Treppe bricht
        // dort ab und setzt nach der Luecke mit eigenem Teilpfad neu an (je
        // Stueck eine Schicht geschlossen, eine Linie offen). Die Luecke steht
        // nie als Null da, und keine Nachbarstufe reicht in sie hinein. Weil
        // alle Reihen dieselbe Stunde lesen, fehlt eine Schicht genau in den
        // Stufen, in denen auch die Zeigerzeile sie nicht nennt.
        // =================================================================

        /// <summary>
        /// Ab so vielen Bildpunktspalten je Tag bündelt die Stufenregel wieder je
        /// SPALTE statt je Tag (Kopf der Stufenregel): Dann steht der Tagesgang als Kurve
        /// über mehreren Stufen und nicht als Streifen.
        /// </summary>
        public const int TAG_MIN_SPALTEN = 4;

        /// <summary>
        /// Die Werte je Tag eines festen Jahresrasters — 24 bei 8 760 Stunden, 96 bei
        /// 35 040 Viertelstunden; <c>0</c> für jede andere Länge (kein Tagesraster).
        /// </summary>
        public static int WerteJeTag(int gesamt) => gesamt == 8760 ? 24 : gesamt == 35040 ? 96 : 0;

        /// <summary>
        /// <b>Ist die Reihe eine Stapelschicht?</b> Eine Fläche mit Unterkante — die
        /// Schichten der Stapelbilder tragen die Summe darunter als <c>Unten</c>, die
        /// unterste eine Nullreihe. Das Profilband und die Flächen, die auf der
        /// Achsennull schließen (<c>Unten</c> ist <c>null</c>), gehören nicht dazu.
        /// </summary>
        public static bool IstStapelschicht(Datenreihe reihe)
            => reihe != null && reihe.Art == Reihenart.Flaeche && reihe.Unten != null;

        /// <summary>
        /// <b>Wird die Reihe in Stufen gezeichnet?</b> Eine Stapelschicht oder eine
        /// Linie, die einen Stapel begleitet (<see cref="Datenreihe.Huelle"/>), mit
        /// gleichmäßigen Stützstellen (ohne <c>XWerte</c>) — und nur, wenn sie MEHR Werte
        /// führt, als die Fläche Bildpunktspalten hat. Sonst steht jede Stützstelle.
        /// </summary>
        /// <param name="reihe">Die Reihe.</param>
        /// <param name="stuetzstellen">Die Zahl der gezeichneten Werte (im Ausschnitt dessen Länge).</param>
        /// <param name="spalten">Die Bildpunktspalten der Zeichenfläche.</param>
        public static bool Spaltenweise(Datenreihe reihe, int stuetzstellen, int spalten)
            => reihe != null && (IstStapelschicht(reihe) || reihe.Huelle)
               && reihe.XWerte == null && spalten >= 1 && stuetzstellen > spalten;

        /// <summary>
        /// <b>Trägt der VOLLPFAD der Reihe jede Stützstelle?</b> Dann rechnet die
        /// Oberfläche beim Zoom nichts nach — der Ausschnitt wäre genau sein Stück. Eine
        /// Punktwolke ist immer roh (DG-E3-5), eine in Stufen gezeichnete Reihe nie;
        /// jede andere nach <see cref="Roh"/>.
        /// </summary>
        /// <param name="reihe">Die Reihe.</param>
        /// <param name="reihen">Die Zahl der Reihen des Bildes.</param>
        /// <param name="spalten">Die Bildpunktspalten der Zeichenfläche.</param>
        public static bool VollpfadRoh(Datenreihe reihe, int reihen, int spalten)
        {
            if (reihe == null || reihe.Art == Reihenart.Punkte) return true;
            int n = reihe.Werte == null ? 0 : reihe.Werte.Length;
            if (Spaltenweise(reihe, n, spalten)) return false;
            return Roh(n, reihen);
        }

        /// <summary>
        /// Die Bildpunktspalte der Stützstelle <paramref name="i"/> von
        /// <paramref name="n"/>: Die Stützstellen liegen gleichmäßig von der linken bis
        /// zur rechten Kante der Fläche, Stelle <c>i</c> bei <c>i · spalten / (n − 1)</c>
        /// Bildpunkten; die letzte zählt zur letzten Spalte.
        /// </summary>
        public static int Spalte(int i, int n, int spalten)
        {
            if (n <= 1 || spalten <= 1) return 0;
            long c = (long)i * spalten / (n - 1);
            return c >= spalten ? spalten - 1 : c < 0 ? 0 : (int)c;
        }

        /// <summary>
        /// <b>Stehen die Stufen eines Ausschnitts auf TAGEN?</b> (Kopf der Stufenregel) — ja,
        /// wenn die Reihe ein festes Jahresraster führt und ein Tag schmaler ist als
        /// <see cref="TAG_MIN_SPALTEN"/> Bildpunktspalten; sonst ist die Stufe die Spalte.
        /// Dieselbe Bedingung, nach der <see cref="Stufen"/> teilt.
        /// </summary>
        /// <param name="gesamt">Die Länge der GANZEN Reihe (sie sagt das Tagesraster).</param>
        /// <param name="laenge">Die Werte des Ausschnitts.</param>
        /// <param name="spalten">Die Bildpunktspalten, über die der Ausschnitt reicht.</param>
        public static bool TagesStufen(int gesamt, int laenge, int spalten)
        {
            int jeTag = WerteJeTag(gesamt);
            return jeTag > 0 && laenge > 1 && spalten >= 1
                   && (long)jeTag * spalten < (long)TAG_MIN_SPALTEN * (laenge - 1);
        }

        /// <summary>
        /// <b>Die Stufen eines Ausschnitts</b> (Kopf der Stufenregel): je Tag, solange ein
        /// Tag schmaler als <see cref="TAG_MIN_SPALTEN"/> Bildpunktspalten ist und die
        /// Reihe ein festes Jahresraster führt, sonst je Bildpunktspalte. Jede Stufe nennt
        /// ihren ersten und letzten Wert (Index im Ausschnitt, einschließlich) und ihre
        /// linke und rechte Grenze als Anteil 0 … 1 der Breite des Ausschnitts — von der
        /// ersten bis zur letzten Stützstelle. Die Stufen schließen lückenlos aneinander.
        /// </summary>
        /// <param name="gesamt">Die Länge der GANZEN Reihe (sie sagt das Tagesraster).</param>
        /// <param name="ab">Der erste Index des Ausschnitts in der ganzen Reihe (die Tage
        /// stehen auf dem Jahresanfang, nicht auf dem Ausschnitt).</param>
        /// <param name="laenge">Die Werte des Ausschnitts.</param>
        /// <param name="spalten">Die Bildpunktspalten, über die der Ausschnitt reicht.</param>
        public static IReadOnlyList<Stufe> Stufen(int gesamt, int ab, int laenge, int spalten)
        {
            var stufen = new List<Stufe>();
            if (laenge <= 0) return stufen;
            if (laenge == 1 || spalten < 1)
            {
                stufen.Add(new Stufe(0, laenge - 1, 0.0, 1.0));
                return stufen;
            }

            double schritt = laenge - 1;
            int jeTag = WerteJeTag(gesamt);
            if (TagesStufen(gesamt, laenge, spalten))
            {
                int letzter = ab + laenge - 1;
                for (int tag = ab / jeTag; (long)tag * jeTag <= letzter; tag++)
                {
                    int von = Math.Max(ab, tag * jeTag);
                    int bis = Math.Min(letzter, (tag + 1) * jeTag - 1);
                    int grenze = Math.Min(letzter, (tag + 1) * jeTag);
                    stufen.Add(new Stufe(von - ab, bis - ab, (von - ab) / schritt, (grenze - ab) / schritt));
                }
                return stufen;
            }

            int start = 0;
            while (start < laenge)
            {
                int spalte = Spalte(start, laenge, spalten);
                int ende = start;
                while (ende + 1 < laenge && Spalte(ende + 1, laenge, spalten) == spalte) ende++;
                stufen.Add(new Stufe(start, ende, (double)spalte / spalten, (double)(spalte + 1) / spalten));
                start = ende + 1;
            }
            return stufen;
        }

        /// <summary>
        /// <b>Die SPITZENSTUNDE je Stufe</b> (Kopf der Stufenregel): der Index — im
        /// Ausschnitt, wie <see cref="Stufe.Von"/> — des größten endlichen Werts der
        /// Bezugsgröße in der Stufe, bei Gleichstand der erste. In dieser Stunde zeigen ALLE
        /// Schichten und Hüllkurven eines Bildes ihren Wert (<see cref="Stundenwerte"/>) —
        /// so bleibt der Stapel je Stufe additiv. Eine Stufe ohne endlichen Bezugswert nimmt
        /// ihre erste Stunde.
        /// </summary>
        /// <param name="bezug">Die Bezugsgröße des Ausschnitts (<see cref="Datenreihe.Bezug"/>).</param>
        /// <param name="stufen">Die Stufen (<see cref="Stufen"/>).</param>
        public static int[] Spitzenstunden(double[] bezug, IReadOnlyList<Stufe> stufen)
        {
            if (stufen == null) return new int[0];
            var stunden = new int[stufen.Count];
            for (int s = 0; s < stufen.Count; s++)
            {
                int treffer = stufen[s].Von;
                bool belegt = false;
                double spitze = 0.0;
                for (int i = stufen[s].Von; i <= stufen[s].Bis && bezug != null && i < bezug.Length; i++)
                {
                    double w = bezug[i];
                    if (double.IsNaN(w) || double.IsInfinity(w)) continue;
                    if (!belegt || w > spitze)
                    {
                        spitze = w;
                        treffer = i;
                        belegt = true;
                    }
                }
                stunden[s] = treffer;
            }
            return stunden;
        }

        /// <summary>
        /// <b>Die Werte einer Reihe in den Spitzenstunden</b> (<see cref="Spitzenstunden"/>)
        /// — je Stufe die Kante einer Stapelschicht oder einer Hüllkurve. Ein nicht endlicher
        /// oder fehlender Wert ist eine LÜCKE und bleibt <see cref="double.NaN"/>
        /// (<see cref="Stufenstuecke"/> lässt die Stufe dann aus).
        /// </summary>
        /// <param name="werte">Die Werte des Ausschnitts in Datenkoordinaten.</param>
        /// <param name="stunden">Die Spitzenstunde je Stufe (Index im Ausschnitt).</param>
        public static double[] Stundenwerte(double[] werte, int[] stunden)
        {
            if (stunden == null) return new double[0];
            var ergebnis = new double[stunden.Length];
            for (int s = 0; s < stunden.Length; s++)
            {
                int i = stunden[s];
                double w = werte != null && i >= 0 && i < werte.Length ? werte[i] : double.NaN;
                ergebnis[s] = double.IsNaN(w) || double.IsInfinity(w) ? double.NaN : w;
            }
            return ergebnis;
        }

        /// <summary>
        /// <b>Die Stücke ohne Lücke</b> (Kopf der Stufenregel): die Läufe aufeinanderfolgender
        /// Stufen, in denen ALLE übergebenen Stufenwerte endlich sind, je als erste und
        /// letzte Stufe. Eine Schicht übergibt Ober- und Unterkante — beide Kanten brechen so
        /// in denselben Stufen ab. Ohne Lücke ist es ein einziges Stück über alle Stufen.
        /// </summary>
        /// <param name="stufenwerte">Die Werte je Stufe (<see cref="Stundenwerte"/>), eine
        /// Reihe oder mehrere gleich lange.</param>
        public static IReadOnlyList<(int Von, int Bis)> Stufenstuecke(params double[][] stufenwerte)
        {
            var stuecke = new List<(int, int)>();
            if (stufenwerte == null || stufenwerte.Length == 0) return stuecke;
            int n = int.MaxValue;
            foreach (double[] w in stufenwerte) n = Math.Min(n, w == null ? 0 : w.Length);
            int s = 0;
            while (s < n)
            {
                while (s < n && !AlleEndlich(stufenwerte, s)) s++;
                if (s >= n) break;
                int von = s;
                while (s < n && AlleEndlich(stufenwerte, s)) s++;
                stuecke.Add((von, s - 1));
            }
            return stuecke;
        }

        private static bool AlleEndlich(double[][] reihen, int s)
        {
            foreach (double[] w in reihen)
                if (double.IsNaN(w[s]) || double.IsInfinity(w[s])) return false;
            return true;
        }

        /// <summary>
        /// <b>Die Treppe der Stufenwerte</b> als Punktfolge von links nach rechts:
        /// je Stufe eine waagrechte Kante von ihrer linken zu ihrer rechten Grenze, an der
        /// Grenze zweier Stufen mit verschiedenen Werten ein senkrechter Sprung. Gleiche
        /// Nachbarwerte ergeben keinen Zwischenpunkt. <c>Anteil</c> ist die Lage des
        /// Punktes als Anteil 0 … 1 der Breite, <c>Wert</c> der Wert der Stufe.
        /// </summary>
        /// <param name="stufen">Die Stufen (<see cref="Stufen"/>).</param>
        /// <param name="stufenwerte">Die Werte je Stufe (<see cref="Stundenwerte"/>).</param>
        public static IReadOnlyList<(double Anteil, double Wert)> Treppe(IReadOnlyList<Stufe> stufen,
                                                                         double[] stufenwerte)
            => Treppe(stufen, stufenwerte, 0, stufen == null ? -1 : stufen.Count - 1);

        /// <summary>
        /// Die Treppe der Stufen <paramref name="von"/> … <paramref name="bis"/> — ein Stück
        /// ohne Lücke (<see cref="Stufenstuecke"/>); sonst wie <see cref="Treppe(IReadOnlyList{Stufe}, double[])"/>.
        /// </summary>
        public static IReadOnlyList<(double Anteil, double Wert)> Treppe(IReadOnlyList<Stufe> stufen,
                                                                         double[] stufenwerte,
                                                                         int von, int bis)
        {
            var punkte = new List<(double, double)>();
            if (stufen == null || stufenwerte == null || stufen.Count == 0
                || stufenwerte.Length < stufen.Count) return punkte;
            if (von < 0) von = 0;
            if (bis > stufen.Count - 1) bis = stufen.Count - 1;
            if (bis < von) return punkte;
            punkte.Add((stufen[von].Links, stufenwerte[von]));
            for (int s = von + 1; s <= bis; s++)
            {
                if (stufenwerte[s].Equals(stufenwerte[s - 1])) continue;
                punkte.Add((stufen[s].Links, stufenwerte[s - 1]));
                punkte.Add((stufen[s].Links, stufenwerte[s]));
            }
            punkte.Add((stufen[bis].Rechts, stufenwerte[bis]));
            return punkte;
        }

        /// <summary>
        /// <b>Der Zeitpunkt einer Stützstelle im festen Jahresraster</b> — 8 760 Stunden
        /// oder 35 040 Viertelstunden, ohne Schaltjahr, das Jahr beginnt am 1. Januar um
        /// 0 Uhr. Die Oberfläche nennt damit die Spitzenstunde einer Stufe mit Datum und
        /// Uhrzeit. <c>null</c> für jede andere Länge und für einen Index außerhalb.
        /// </summary>
        /// <param name="index">Der Index in der ganzen Reihe.</param>
        /// <param name="gesamt">Die Länge der ganzen Reihe.</param>
        public static DateTime? Zeitpunkt(int index, int gesamt)
        {
            int jeTag = WerteJeTag(gesamt);
            if (jeTag == 0 || index < 0 || index >= gesamt) return null;
            // 2001 ist kein Schaltjahr - wie das Jahresraster des Rechenkerns.
            return new DateTime(2001, 1, 1).AddMinutes((double)index * (24 * 60 / jeTag));
        }
    }

    // ------------------------------------------------------------------ Ziele

    /// <summary>
    /// Wohin ein Helfer seine Befehle gibt: in ein ganzes <see cref="Zeichenmodell"/>
    /// oder in einen <see cref="Befehlssammler"/>, der eine Gruppe füllt.
    ///
    /// <para>Die Schnittstelle trennt den HELFER vom Behälter: Titel, Raster, Achsen
    /// und Legende haben EINEN Rumpf, gleich ob sie ins Bild oder in eine
    /// zugeschnittene Gruppe schreiben. Ein Ziel, das unmittelbar malt, gibt es
    /// nicht mehr — gemalt wird erst das fertige Modell
    /// (<c>SkiaMaler.Png</c>).</para>
    /// </summary>
    public interface IZeichenziel
    {
        void Fuege(Zeichenbefehl befehl);
    }

    /// <summary>Eine Befehlsliste ohne eigene Fläche — der Sammler einer Gruppe.</summary>
    public sealed class Befehlssammler : IZeichenziel
    {
        private readonly List<Zeichenbefehl> _befehle = new List<Zeichenbefehl>();

        public IReadOnlyList<Zeichenbefehl> Befehle => _befehle;

        public void Fuege(Zeichenbefehl befehl) { if (befehl != null) _befehle.Add(befehl); }

        public Wertliste<Zeichenbefehl> Liste() => new Wertliste<Zeichenbefehl>(_befehle);
    }

    /// <summary>
    /// Ein ganzes Bild: Fläche, Hintergrund und die Befehle in Zeichenreihenfolge.
    /// </summary>
    public sealed class Zeichenmodell : IZeichenziel
    {
        private readonly List<Zeichenbefehl> _befehle = new List<Zeichenbefehl>();
        private readonly List<Datenreihe> _reihen = new List<Datenreihe>();

        public Zeichenmodell(int breite, int hoehe, Farbton hintergrund)
        {
            Breite = breite;
            Hoehe = hoehe;
            Hintergrund = hintergrund;
        }

        public int Breite { get; }
        public int Hoehe { get; }
        public Farbton Hintergrund { get; }

        public IReadOnlyList<Zeichenbefehl> Befehle => _befehle;

        /// <summary>
        /// Die Zeichenfläche samt Datenfenster; <c>null</c>, wenn das Bild keine hat
        /// (Kuchen, Ring, Balken). Nur mit ihr entsteht im SVG das innere
        /// <c>&lt;svg&gt;</c> in Datenkoordinaten.
        /// </summary>
        public Zeichenflaeche Flaeche { get; set; }

        /// <summary>Die Reihen des Bildes in Datenwerten, in Zeichenreihenfolge.</summary>
        public IReadOnlyList<Datenreihe> Reihen => _reihen;

        public void Fuege(Zeichenbefehl befehl) { if (befehl != null) _befehle.Add(befehl); }

        /// <summary>Eine Reihe in Datenwerten hinzufügen.</summary>
        public void FuegeReihe(Datenreihe reihe) { if (reihe != null) _reihen.Add(reihe); }

        /// <summary>
        /// Gleichheit zweier Modelle — Fläche, Hintergrund, jeder Befehl (samt Marke
        /// und, seit DG-E3-6, samt Wert) und seit Etappe E2 auch Zeichenfläche und
        /// Reihen. Damit prüft ein Test, dass zweimal Erzeugen dasselbe Modell
        /// liefert.
        /// </summary>
        public bool Gleicht(Zeichenmodell andere)
        {
            if (andere == null) return false;
            if (Breite != andere.Breite || Hoehe != andere.Hoehe ||
                !Equals(Hintergrund, andere.Hintergrund)) return false;
            if (!Equals(Flaeche, andere.Flaeche)) return false;
            if (_befehle.Count != andere._befehle.Count) return false;
            for (int i = 0; i < _befehle.Count; i++)
                if (!Equals(_befehle[i], andere._befehle[i])) return false;
            if (_reihen.Count != andere._reihen.Count) return false;
            for (int i = 0; i < _reihen.Count; i++)
                if (!_reihen[i].Gleicht(andere._reihen[i])) return false;
            return true;
        }
    }

    /// <summary>
    /// Eine unveränderliche Liste MIT WERTGLEICHHEIT. Ein Record mit einem
    /// gewöhnlichen Array oder einer <c>List</c> vergliche die Referenz — zwei
    /// gleich gefüllte Pfade wären dann verschieden, und der Determinismustest des
    /// Modells liefe ins Leere.
    /// </summary>
    public sealed class Wertliste<T> : IReadOnlyList<T>, IEquatable<Wertliste<T>>
    {
        private readonly T[] _werte;

        public Wertliste(IEnumerable<T> werte)
        {
            _werte = werte == null ? Array.Empty<T>() : new List<T>(werte).ToArray();
        }

        public T this[int i] => _werte[i];
        public int Count => _werte.Length;
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_werte).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _werte.GetEnumerator();

        public bool Equals(Wertliste<T> andere)
        {
            if (ReferenceEquals(this, andere)) return true;
            if (andere == null || andere._werte.Length != _werte.Length) return false;
            for (int i = 0; i < _werte.Length; i++)
                if (!EqualityComparer<T>.Default.Equals(_werte[i], andere._werte[i])) return false;
            return true;
        }

        public override bool Equals(object o) => Equals(o as Wertliste<T>);

        public override int GetHashCode()
        {
            int h = _werte.Length;
            foreach (T w in _werte) h = unchecked(h * 31 + (w == null ? 0 : w.GetHashCode()));
            return h;
        }
    }

    // --------------------------------------------------------------- Bequemes

    /// <summary>
    /// Die bequemen Schreibweisen für die Helfer des Renderers: <c>z.Linie(...)</c>
    /// statt <c>z.Fuege(new Zeichnung.Linie(...))</c>.
    /// </summary>
    public static class Zeichenhilfe
    {
        public static void Linie(this IZeichenziel z, float x1, float y1, float x2, float y2, Stift stift)
            => z.Fuege(new Zeichnung.Linie(x1, y1, x2, y2, stift));

        public static void Rechteck(this IZeichenziel z, float x, float y, float breite, float hoehe,
                                    Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Rechteck(x, y, breite, hoehe, rand, fuellung));

        public static void Kreis(this IZeichenziel z, float x, float y, float radius,
                                 Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Kreis(x, y, radius, rand, fuellung));

        public static void Ellipse(this IZeichenziel z, float x, float y, float breite, float hoehe,
                                   Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Ellipse(x, y, breite, hoehe, rand, fuellung));

        public static void Pfad(this IZeichenziel z, IEnumerable<Punkt> punkte, bool geschlossen,
                                Stift rand = null, Fuellung fuellung = null)
            => z.Fuege(new Zeichnung.Pfad(new Wertliste<Punkt>(punkte), geschlossen, rand, fuellung));

        public static void Text(this IZeichenziel z, string inhalt, float x, float y,
                                Schrift schrift, Farbton ton,
                                Ausrichtung ausrichtung = Ausrichtung.Links)
            => z.Fuege(new Zeichnung.Text(inhalt, x, y, schrift, ton, ausrichtung));

        /// <summary>Eine Gruppe, wahlweise zugeschnitten; <paramref name="inhalt"/> füllt sie.</summary>
        public static void Gruppe(this IZeichenziel z, Rahmen? zuschnitt, Action<IZeichenziel> inhalt)
        {
            var sammler = new Befehlssammler();
            inhalt(sammler);
            z.Fuege(new Zeichnung.Gruppe(zuschnitt, sammler.Liste()));
        }

        /// <summary>
        /// Alles, was <paramref name="inhalt"/> absetzt, bekommt die
        /// <see cref="Zeichenbefehl.Marke"/> <paramref name="marke"/> (Etappe E2).
        ///
        /// <para><b>Warum ein Sammler und kein Parameter an jedem Helfer.</b> Eine
        /// Marke gehört zu einem BLOCK — die x-Achse sind sechs Rasterlinien, sechs
        /// Beschriftungen und ein Titel —, und die Helfer, die sie absetzen, haben
        /// zwölf Nutzer. Ein Parameter an jedem von ihnen wäre Tippfehlerfläche ohne
        /// Gewinn; hier steht die Marke EINMAL an der Klammer. Die Befehle selbst
        /// bleiben unverändert (der Maler übergeht die Marke), das PNG deshalb
        /// byte-gleich.</para>
        ///
        /// <para>Eine Marke, die der Inhalt schon gesetzt hat, bleibt stehen — so
        /// überschreibt eine äußere Klammer keine feinere Marke.</para>
        /// </summary>
        public static void Markiert(this IZeichenziel z, string marke, Action<IZeichenziel> inhalt)
        {
            var sammler = new Befehlssammler();
            inhalt(sammler);
            foreach (Zeichenbefehl b in sammler.Befehle)
                z.Fuege(b.Marke == null ? b with { Marke = marke } : b);
        }

        /// <summary>
        /// Dieselbe Klammer MIT dem Wert am Element (Etappe E3, Entscheid DG-E3-6):
        /// Alles, was <paramref name="inhalt"/> absetzt, bekommt
        /// <see cref="Zeichenbefehl.Marke"/> <paramref name="marke"/> und
        /// <see cref="Zeichenbefehl.Wert"/> <paramref name="wert"/>.
        ///
        /// <para>Dieselbe Regel wie bei der Marke: Was der Inhalt schon gesetzt hat,
        /// bleibt stehen — beides für sich. So überschreibt eine äußere Klammer weder
        /// eine feinere Marke noch einen feineren Wert. Die Befehle bleiben im Übrigen
        /// unverändert, das PNG deshalb byte-gleich.</para>
        /// </summary>
        public static void Markiert(this IZeichenziel z, string marke, string wert,
                                    Action<IZeichenziel> inhalt)
        {
            var sammler = new Befehlssammler();
            inhalt(sammler);
            foreach (Zeichenbefehl b in sammler.Befehle)
                z.Fuege(b.Marke == null || b.Wert == null
                            ? b with { Marke = b.Marke ?? marke, Wert = b.Wert ?? wert }
                            : b);
        }
    }
}
