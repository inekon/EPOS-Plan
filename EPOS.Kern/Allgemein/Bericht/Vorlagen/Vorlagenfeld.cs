using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>Die Platzhalterklassen BV-P1 bis BV-P9 (Konzept Berichtsvorlagen 4.6).</summary>
    public enum Vorlagenfeldart
    {
        /// <summary>BV-P1: Text; Absatz- und Zeichenformat bleiben, Zeilenumbrüche werden in Word zu <c>w:br</c>.</summary>
        Text = 1,

        /// <summary>BV-P2: Zahl mit Format und Einheit aus dem Katalog; in Excel eine echte Zahl.</summary>
        Zahl = 2,

        /// <summary>BV-P3: Datum nach Kultur; in Excel ein echtes Datum.</summary>
        Datum = 3,

        /// <summary>BV-P4: Strukturtabelle (ab BV-E5).</summary>
        Tabelle = 4,

        /// <summary>BV-P5: Bild aus dem Zeichenmodell (ab BV-E5).</summary>
        Bild = 5,

        /// <summary>BV-P6: Aufzählung, etwa die Warnungen.</summary>
        Liste = 6,

        /// <summary>BV-P7: ein heutiger Baustein, vollständig erzeugt.</summary>
        Kapitel = 7,

        /// <summary>BV-P8: ja/nein, nur als Bedingung in <c>{{#wenn}}</c> (ab BV-E4).</summary>
        Schalter = 8,

        /// <summary>BV-P9: erzeugtes Excel-Blatt an der Stelle der Blattmarke (ab BV-E7).</summary>
        Blatt = 9,
    }

    /// <summary>Wo ein Platzhalter gilt (Konzept 4.7).</summary>
    public enum Vorlagenfeldkontext
    {
        /// <summary>Der Bericht als Ganzes — überall gültig.</summary>
        Bericht,

        /// <summary>Die Installation, die den Bericht erstellt (<c>ersteller.*</c>) — überall gültig.</summary>
        Installation,

        /// <summary>Stammdaten und Ergebnisse des Stammprojekts — überall gültig.</summary>
        Stamm,

        /// <summary>Der laufende Stand in <c>{{#je stand}}</c>/<c>{{#je variante}}</c> (ab BV-E4).</summary>
        Stand,

        /// <summary>Das laufende Gebäude in <c>{{#je gebaeude}}</c> (ab BV-E4).</summary>
        Gebaeude,

        /// <summary>Über alle gewählten Stände (<c>vergleich.*</c>, <c>wirtschaft.*</c>, ab BV-E4) — überall gültig.</summary>
        Gruppe,
    }

    /// <summary>In welche Ausgabe ein Platzhalter gehört (Konzept 5.1).</summary>
    [Flags]
    public enum Vorlagenausgabe
    {
        /// <summary>Keine.</summary>
        Keine = 0,

        /// <summary>Word-Bericht.</summary>
        Word = 1,

        /// <summary>Excel-Mappe.</summary>
        Excel = 2,

        /// <summary>Beide Ausgaben.</summary>
        Beide = Word | Excel,
    }

    /// <summary>
    /// Was der Sammler für einen Platzhalter zusätzlich erheben muss (Konzept 5.1, 8.5) — die
    /// Grundlage, auf der ab BV-E3 der Bedarf eines Laufs gesteuert wird.
    /// </summary>
    [Flags]
    public enum Vorlagenbedarf
    {
        /// <summary>Nichts über den Regellauf hinaus.</summary>
        Keiner = 0,

        /// <summary>Die Stundenreihen des Laufs (<see cref="ZeitreihenSatz"/>).</summary>
        Zeitreihen = 1,

        /// <summary>Der Kapitalwertverlauf der Wirtschaftlichkeit.</summary>
        Verlauf = 2,

        /// <summary>Die Emissionsbilanz gekoppelte gegen getrennte Erzeugung.</summary>
        Emissionsbilanz = 4,
    }

    /// <summary>
    /// Die Herkunft eines ERZEUGTEN Katalogeintrags (Konzept 5.2, 5.5): Musterschlüssel,
    /// Ressource des Beschreibungsmusters und der Parameter, der im Muster für <c>&lt;k&gt;</c>
    /// steht. Erzeugte Einträge haben keine eigene Beschreibung; sie nehmen das Muster mit dem
    /// Parameter als <c>{0}</c>.
    /// </summary>
    public sealed class Vorlagenfeldableitung
    {
        /// <summary>Legt die Ableitung an.</summary>
        public Vorlagenfeldableitung(string muster, string musterId, string parameter)
        {
            Muster = muster ?? "";
            MusterId = musterId ?? "";
            Parameter = parameter ?? "";
        }

        /// <summary>Der Musterschlüssel, etwa <c>stamm.kennzahl.&lt;k&gt;</c>.</summary>
        public string Muster { get; }

        /// <summary>Der Ressourcenschlüssel des Beschreibungsmusters, etwa <c>VF_MUSTER_STAMM_KENNZAHL</c>.</summary>
        public string MusterId { get; }

        /// <summary>Der eingesetzte Parameter, etwa der Kennzahlschlüssel <c>eff.jaz</c>.</summary>
        public string Parameter { get; }

        /// <summary>
        /// Die Bezeichnung des Parameters für <c>{0}</c> in der Kultur der Beschreibung — etwa der Titel einer
        /// Zeile der Wirtschaftlichkeit oder die Beschreibung des Standeintrags hinter <c>stand.a.*</c>;
        /// <c>null</c> = die Beschriftung der Kennzahl <see cref="Parameter"/>, sonst der Parameter selbst.
        /// </summary>
        public Func<CultureInfo, string> Bezeichnung { get; init; }

        /// <summary>Der Zusatz für <c>{1}</c>, etwa der Name des Szenarios; <c>null</c> = leer.</summary>
        public Func<CultureInfo, string> Zusatz { get; init; }
    }

    /// <summary>
    /// <b>Ein Eintrag des Platzhalterkatalogs</b> (Konzept 5.1) — Schlüssel, Art, Kontext, die
    /// Quelle auf dem Wertesatz und alles, was Engine, Prüfer und Katalogansicht über den
    /// Platzhalter wissen müssen. Die Einträge stehen als Daten in
    /// <see cref="Vorlagenfeldkatalog"/>.
    /// </summary>
    public sealed class Vorlagenfeld
    {
        /// <summary>Der Leerwert für Zahl, Datum und Text: der Gedankenstrich, nie 0 (Konzept 4.10).</summary>
        public const string STRICH = "—";

        private string _leerwert;
        private Vorlagenausgabe? _ausgaben;

        /// <summary>Legt einen Eintrag an; die übrigen Angaben setzt der Objektinitialisierer.</summary>
        public Vorlagenfeld(string schluessel, Vorlagenfeldart art, Vorlagenfeldkontext kontext,
                            Func<Berichtswerte, object> quelle)
        {
            Schluessel = schluessel ?? "";
            Art = art;
            Kontext = kontext;
            Quelle = quelle;
        }

        /// <summary>Der Schlüssel nach dem Schlüsselmuster (Konzept 4.5), etwa <c>projekt.kunde</c>.</summary>
        public string Schluessel { get; }

        /// <summary>Die Platzhalterklasse BV-P1 bis BV-P9.</summary>
        public Vorlagenfeldart Art { get; }

        /// <summary>Wo der Platzhalter gilt.</summary>
        public Vorlagenfeldkontext Kontext { get; }

        /// <summary>
        /// Der Wert auf dem Wertesatz: je nach Art eine Zeichenkette, eine Zahl, ein Datum, eine
        /// Folge von Zeichenketten (Liste, Kapitel) — oder ein <see cref="Leergrund"/>, wenn die
        /// Quelle weiß, warum es keinen Wert gibt. <c>null</c> heißt leer ohne Grund.
        /// </summary>
        public Func<Berichtswerte, object> Quelle { get; }

        /// <summary>Das .NET-Format ohne Formatangabe: Zahl etwa <c>N1</c>, Datum <c>d</c> (kurz) oder
        /// <c>g</c> (mit Uhrzeit); <c>null</c> = Vorgabe der Art.</summary>
        public string Format { get; init; }

        /// <summary>Die Einheit einer Zahl, sprachneutral wie im Kennzahlenkatalog; <c>null</c> = keine.</summary>
        public string Einheit { get; init; }

        /// <summary>
        /// Der Text, wenn kein Wert vorliegt (Konzept 4.10): Zahl und Datum „—“, Text „—“ oder
        /// leer je Eintrag, Liste und Kapitel leer. Nie 0.
        /// </summary>
        public string Leerwert
        {
            get { return _leerwert ?? StandardLeerwert(Art); }
            init { _leerwert = value; }
        }

        /// <summary>
        /// Der Ressourcenschlüssel der Beschreibung — nur bei handgepflegten Einträgen, gebildet
        /// nach <see cref="Vorlagenfeldkatalog.RessourcenName"/> (<c>projekt.kunde</c> →
        /// <c>VF_PROJEKT__KUNDE</c>); bei erzeugten <c>null</c>, sie nehmen das Muster ihrer
        /// <see cref="Ableitung"/>.
        /// </summary>
        public string BeschreibungId
        {
            get { return Ableitung == null ? Vorlagenfeldkatalog.RessourcenName(Schluessel) : null; }
        }

        /// <summary>Die Herkunft eines erzeugten Eintrags; <c>null</c> = handgepflegt.</summary>
        public Vorlagenfeldableitung Ableitung { get; init; }

        /// <summary>Ressourcenschlüssel eines Beispielwerts für die Katalogansicht; <c>null</c> = keiner
        /// (Katalog v1 führt keine).</summary>
        public string BeispielId { get; init; }

        /// <summary>Frühere oder gleichwertige Schlüssel, die auf diesen Eintrag führen.</summary>
        public IReadOnlyList<string> Aliasse { get; init; } = Array.Empty<string>();

        /// <summary>In welche Ausgaben der Platzhalter gehört; Vorgabe: Kapitel nur Word, sonst beide.</summary>
        public Vorlagenausgabe Ausgaben
        {
            get { return _ausgaben ?? (Art == Vorlagenfeldart.Kapitel ? Vorlagenausgabe.Word : Vorlagenausgabe.Beide); }
            init { _ausgaben = value; }
        }

        /// <summary>Was der Sammler für diesen Platzhalter zusätzlich erheben muss.</summary>
        public Vorlagenbedarf Bedarf { get; init; }

        /// <summary>Die Katalogfassung, seit der es den Schlüssel gibt (Konzept 5.6).</summary>
        public int Seit { get; init; } = 1;

        /// <summary>
        /// Bei Kapiteln die Katalogschlüssel, die das Kapitel deckt (Konzept 5.1, 12): die Einzelschlüssel,
        /// deren Inhalt es schreibt, dazu Kapitelkopf und Schalter; beim Sammelanker <c>bericht.inhalt</c>
        /// alle Kapitel. Sonst leer. Ausgewertet über <see cref="Vorlagenfeldkatalog.Gedeckt"/>.
        /// </summary>
        public IReadOnlyList<string> Deckt { get; init; } = Array.Empty<string>();

        /// <summary>Handgepflegt (eigene Beschreibung) oder aus einer Quelle erzeugt?</summary>
        public bool Handgepflegt { get { return Ableitung == null; } }

        /// <inheritdoc/>
        public override string ToString() { return Schluessel; }

        private static string StandardLeerwert(Vorlagenfeldart art)
        {
            switch (art)
            {
                case Vorlagenfeldart.Zahl:
                case Vorlagenfeldart.Datum:
                case Vorlagenfeldart.Text:
                    return STRICH;
                default:
                    return "";
            }
        }
    }
}
