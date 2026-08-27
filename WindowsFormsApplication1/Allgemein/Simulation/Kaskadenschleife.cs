using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Stundenschleife der DREIKANALIGEN Kaskade — die Reihenfolge-Invariante aus
    /// Konzept 6.3 für ALLE speicherfähigen Erzeuger.
    ///
    /// <code>
    /// je Stunde h:
    ///   A)  Vorabentladung   — Speicher decken Bedarf in JEDEM Kanal ihres Klassen-Sets
    ///                          (Hysterese), Kanäle in KNAPPHEITSREIHENFOLGE (4.3),
    ///                          darin Reihenfolge nach Entladepriorität (3.6)
    ///   B)  Bedarfsdeckung   — Erzeugerstufen in KASKADENREIHENFOLGE, je Stufe deckt
    ///                          jede Anlage über ihre DIREKTsenken in Rangfolge
    ///                          (SenkeAbziehen mit der Maske der Senkenzeile)
    ///   C…) Ladephasen       — RANG FÜR RANG aufsteigend: alle Puffersenken des Rangs r
    ///                          aller Anlagen, KASKADENÜBERGREIFEND nach Ladepriorität der
    ///                          Stunde (3.4/3.5), KEIN SenkeAbziehen
    ///   E)  Nachentladung    — Speicher decken den noch offenen Bedarf
    ///   F)  Heizstab         — auf den dann verbleibenden Kanalrest
    ///   G)  StundeAbschliessen je Registry-Speicher — GENAU EINMAL
    /// </code>
    ///
    /// PAKET K2 — WAS SICH GEGENÜBER DER ZWEIKANALIGEN FASSUNG GEÄNDERT HAT: Der
    /// Stundenzustand ist ein <c>double[Kanal.ANZAHL]</c> statt zweier
    /// <c>ref</c>-Parameter, die Entladeordnung eine Liste JE KANAL, das Durchsatzbudget
    /// ein Kanalfeld, und die Zurechnung der Speicherentladung läuft nach Erzeugerart
    /// UND Kanal (Konzept 4.1).
    ///
    /// PAKET S1 — WAS SICH MIT DER SENKENLISTE GEÄNDERT HAT (Konzept 5.1/5.2): Eine
    /// Anlage hat nicht mehr eine Hauptsenke und optional eine Zweitsenke, sondern eine
    /// GEORDNETE LISTE von n Senken. Daraus folgen zwei Dinge in dieser Schleife:
    /// <list type="number">
    /// <item>Phase B deckt über die DIREKTSENKEN-KETTE einer Anlage — Zeile für Zeile in
    /// Rangfolge, jede mit ihrer eigenen Kanalmaske.</item>
    /// <item>Aus den Phasen C und D werden LADEPHASEN JE RANG: erst alle Rang-1-Aufträge
    /// kaskadenübergreifend nach Ladeordnung, dann Rang 2, dann Rang 3 … Die bisherigen
    /// Phasen C/D sind exakt der Sonderfall Rang 1 / Rang 2.</item>
    /// </list>
    /// Die beiden Interimsregeln I1 und I2 aus K2 (Prozesswärme an den Heizungssenken
    /// bzw. an jedem Heizungspuffer) sind damit ERSATZLOS entfallen — siehe den Abschnitt
    /// „Kanalmasken der Senken" weiter unten.
    ///
    /// WARUM DIE SCHLEIFE IN PAKET 5 AUS DEM WÄRMEPUMPEN-MODUL HERAUSGEWANDERT IST:
    /// In Etappe 4b war die Wärmepumpe der einzige Erzeuger mit Senkenauswertung, und die
    /// Schleife stand in ihrem Modul. Mit Paket 5 laden auch Solarthermie und Heizkessel
    /// Puffer. Zwei Erzeuger, die denselben Speicher bedienen, MÜSSEN in derselben
    /// Stundenschleife laufen: Ein Vektormodul, das sein ganzes Jahr durchrechnet, würde
    /// den Speicher bis Stunde 8759 füllen und der nächsten Stufe einen Füllstand aus dem
    /// Silvesterabend in ihre Stunde 0 reichen. Außerdem verlangt Konzept 6.3
    /// <c>StundeAbschliessen</c> GENAU EINMAL je Stunde und Speicher — das kann nur eine
    /// Stelle leisten, die alle Stufen kennt. Und schließlich gibt es Projekte ohne
    /// Wärmepumpe (in der Referenzmenge 1017 und 1018), deren Kessel trotzdem einen Puffer
    /// laden können soll.
    ///
    /// WAS NICHT IN DIESER SCHLEIFE LÄUFT: Erzeugerstufen OHNE Speicherbeteiligung. Sie
    /// berühren keinen Speicher, ihr Ergebnis hängt nur vom Kanalzustand an ihrer
    /// Kaskadenposition ab, und sie bleiben deshalb Vektorstufen an genau dieser Position
    /// (<c>SimulationSolarthermie.Berechnung_Zweikanalig</c>,
    /// <c>SimulationSPK.Berechnung_Zweikanalig</c>,
    /// <c>SimulationBHKW.Berechnung_Zweikanalig</c>).
    ///
    /// SEIT PAKET 6 ist auch das BHKW Schleifenmitglied, sobald es einen Speicher hat.
    /// Der Kompatibilitätsanker <c>Waermekanaele.Uebernehmen</c> ist damit im
    /// zweikanaligen Weg vollständig abgelöst — keine Erzeugerart rechnet dort mehr auf
    /// der Kanalsumme. Die drei Fahrweisen des BHKW bleiben fachlich unangetastet: Sie
    /// bestimmen, WANN die Maschine läuft; die Speicherinteraktion läuft einheitlich über
    /// die Phasen A/C/D/E/G.
    /// </summary>
    public class Kaskadenschleife
    {
        /// <summary>Wärmepumpen-Modul; <c>null</c>, wenn keine Wärmepumpe in der Kaskade steht.</summary>
        public SimulationWaermepumpe WP;

        /// <summary>Solarthermie-Modul; nur gesetzt, wenn die Stufe in der Schleife läuft.</summary>
        public SimulationSolarthermie Solar;

        /// <summary>Heizkessel-Modul; nur gesetzt, wenn die Stufe in der Schleife läuft.</summary>
        public SimulationSPK Kessel;

        /// <summary>
        /// BHKW-Modul; nur gesetzt, wenn die Stufe in der Schleife läuft (Paket 6).
        ///
        /// Bis Paket 5 rechnete das BHKW als letztes Modul einkanalig auf
        /// <c>Waermekanaele.Summe()</c> und verteilte seinen Rest über
        /// <c>Uebernehmen()</c> zurück. Mit Paket 6 ist dieser Kompatibilitätsanker
        /// aufgelöst: Das BHKW deckt seinen Kanal nach <c>WS_Typ</c>, lädt Puffer über
        /// die Ladeordnung (Vorgaberang 30) und trägt seinen Eigenanteil zur
        /// Herkunftsrechnung bei.
        /// </summary>
        public SimulationBHKW BHKW;

        /// <summary>Speicher-, Entlade- und Ladeordnung des Laufs (Konzept 6.1).</summary>
        public Kaskadenkontext Kontext;

        /// <summary>
        /// Erzeugerarten (<c>ProjektPuffer.TYP_*</c>) der Phase B in KASKADENREIHENFOLGE.
        /// Sie bestimmt, wer den Momentanbedarf zuerst deckt — anders als die Ladeordnung
        /// der Phasen C/D, die kaskadenübergreifend nach Ladepriorität arbeitet (3.4).
        /// </summary>
        public List<int> Bedarfsreihenfolge = new List<int>();

        public bool MitWP { get { return WP != null; } }
        public bool MitSolar { get { return Solar != null; } }
        public bool MitKessel { get { return Kessel != null; } }
        public bool MitBHKW { get { return BHKW != null; } }

        /// <summary>
        /// Fehlertext eines Abbruchs (Konzept 13.4: die Engine bleibt dialogfrei).
        /// Gesetzt wird er heute allein vom ZYKLUS-GUARD der Rechenebenen (Etappe D5a);
        /// der Aufrufer reicht ihn an <c>SimulationRunner</c> weiter.
        /// </summary>
        public string Fehlertext = "";

        // ==================================================================
        // RECHENEBENEN (Etappe D5a) — „der Erzeuger mit Puffer-Quelle rechnet
        // NACH seinem Puffer"
        //
        // Konzept_KonfigUI_Hydraulik, Abschnitt 5: Ein Erzeuger mit WQ_Typ =
        // Pufferspeicher bezieht seine Eintrittstemperatur aus einem Puffer, den ein
        // ANDERER Erzeuger lädt. Damit er in der Stunde etwas vorfindet, muss er nach
        // dessen Ladephase rechnen — die Rechenreihenfolge ergibt sich also aus dem
        // Quellbezug, nicht aus der Kaskadenposition.
        //
        // UMSETZUNG: Die Phasen B/C/D laufen je EBENE aufsteigend; zwischen zwei Ebenen
        // gibt der Speicher seinen DURCHSATZ an den Kanal ab (siehe Rechnen()). Ebene 0
        // sind alle Anlagen ohne Quellpuffer oder mit einem Quellpuffer, den in diesem
        // Lauf niemand lädt; Ebene n+1 sind die Anlagen, deren Quellpuffer von einer
        // Anlage der Ebene n geladen wird. Mehrstufige Ketten (WP 1 → Puffer 1 → WP 2
        // → Puffer 2, die Booster-Konstellation des Konzepts) fallen ohne Sonderfall
        // heraus.
        //
        // MIT NUR EINER EBENE — jedes Bestandsprojekt und jeder Lauf ohne Quellbezug auf
        // einen geladenen Puffer — läuft der Rumpf GENAU EINMAL und ist Anweisung für
        // Anweisung die bisherige Schleife. Das ist die Regressionszusage dieser Etappe.
        // ==================================================================

        /// <summary>Höchste vorkommende Rechenebene; 0 = keine Kaskade über Quellbezüge.</summary>
        private int _maxEbene = 0;

        /// <summary>Rechenebene je <c>Tab_Energieanlagen.ID</c>.</summary>
        private readonly Dictionary<int, int> _ebeneJeAnlage = new Dictionary<int, int>();

        /// <summary>Erzeugerarten der Phase B je Ebene, in Kaskadenreihenfolge.</summary>
        private List<int>[] _bedarfJeEbene;

        /// <summary>
        /// KNAPPHEITSREIHENFOLGE dieses Laufs (Konzept 4.3) — aufgelöst zu Beginn von
        /// <see cref="Rechnen"/> aus <c>Kaskadenkontext.Knappheit</c>, damit die
        /// Stundenschleife nicht 8760-mal durch zwei Null-Prüfungen läuft.
        /// </summary>
        private int[] _knappheit = Kanal.KnappheitVorgabe();

        /// <summary>
        /// Hysterese-Entscheidung der laufenden Phase A je Speicher (Etappe D5a,
        /// verallgemeinert in Paket K2).
        ///
        /// Ein Speicher mit MEHRELEMENTIGEM Klassen-Set steht in mehreren
        /// Entladereihenfolgen und wird in Phase A deshalb mehrfach besucht.
        /// <see cref="SimulationPufferspeicher.HystereseFortschreiben"/> ist aber ein
        /// ZUSTANDSÜBERGANG: Der zweite Aufruf sähe den bereits abgesenkten Füllstand und
        /// könnte den Speicher mitten in der Stunde in den Nachladebetrieb kippen. Die
        /// Entscheidung fällt deshalb je Speicher und Stunde genau einmal.
        ///
        /// SEIT PAKET K2 OHNE VORBEDINGUNG. Bis dahin lief die Merkung nur, wenn ein
        /// Kombispeicher mitrechnete. Für einen Speicher, der nur einmal besucht wird, ist
        /// die Merkung wirkungsgleich mit dem direkten Aufruf — sie kostet einen
        /// Wörterbuchzugriff und nimmt dafür eine Fallunterscheidung heraus, die genau
        /// einmal falsch sein müsste, um still falsch zu rechnen.
        /// </summary>
        private readonly Dictionary<SimulationPufferspeicher, bool> _hysteresePhaseA =
            new Dictionary<SimulationPufferspeicher, bool>();

        /// <summary>
        /// HÖCHSTER Rang der Ladeaufträge dieses Laufs (Paket S1) — die Zahl der
        /// Ladephasen je Rechenebene, aufgelöst zu Beginn von <see cref="Rechnen"/>.
        /// 1 = eine Ladephase; 2 = das Bestandsbild der Phasen C und D.
        /// </summary>
        private int _maxRang = 1;

        // ==================================================================
        // KANALMASKEN DER SENKEN (Konzept 4.3/5.2) — PAKET S1
        //
        // HIER STANDEN BIS S1 DIE INTERIMSREGELN I1 UND I2. Sie haben den Prozesskanal
        // übergangsweise an die Heizungs-Direktsenken und an jeden Heizungspuffer
        // gehängt, weil es bis dahin keine Senkenzeile gab, die eine Anlage dem
        // Prozesskanal zuordnet. Beide sind mit diesem Paket ERSATZLOS entfallen:
        //
        //   I1 -> die Maske kommt jetzt aus der SENKENZEILE (Z_AnlageSenke.Ziel /
        //         .Bedarfsart, siehe SenkenMaske). Prozesswärme deckt nur noch, wer eine
        //         Zeile mit Ziel „Prozesswaerme" hat; Bestandsanlagen haben sie über die
        //         Migrationsregel R-Prozess bekommen (Konzept 4.4/5.1).
        //   I2 -> die Entladung folgt dem ECHTEN Klassen-Set des Speichers
        //         (SimulationPufferspeicher.BedientKanal): Kombi = {H, B}, und den
        //         Prozesskanal bedient nur ein Puffer mit Nutzung_Prozess.
        //
        // Was davon BLEIBT: die Masken selbst und die Zusage, dass sie GETEILTE,
        // UNVERÄNDERLICHE Instanzen sind. SenkeAbziehen liest sie nur; wer sie beschriebe,
        // verstellte die Abzugsregel aller folgenden Stunden.
        // ==================================================================

        /// <summary>
        /// Kanalmaske {H} / {B} / {P} — je eine unveränderliche Instanz, siehe
        /// <see cref="SenkeAbziehen(bool[], double, double[], int[])"/>.
        /// </summary>
        private static readonly bool[][] MASKE_EINZELKANAL = MaskenBauen();

        /// <summary>Maske der Bedarfsart „Warmwasser" = {BRAUCHWASSER}.</summary>
        private static readonly bool[] MASKE_WARMWASSER = MaskeBauen(Kanal.BRAUCHWASSER);

        /// <summary>Maske der Bedarfsart „Heizung" = {HEIZUNG}.</summary>
        private static readonly bool[] MASKE_HEIZUNG = MaskeBauen(Kanal.HEIZUNG);

        /// <summary>
        /// Maske der Bedarfsart „Beides" = {BRAUCHWASSER, HEIZUNG} — die beiden Kanäle,
        /// die der Heizkreis bedient. Der Prozesskanal ist seit S1 NICHT mehr dabei
        /// (Abriss I1, siehe Abschnittskopf).
        /// </summary>
        private static readonly bool[] MASKE_BEIDES =
            MaskeBauen(Kanal.BRAUCHWASSER, Kanal.HEIZUNG);

        /// <summary>Maske der Direktsenke „Prozesswaerme" = {PROZESS}.</summary>
        private static readonly bool[] MASKE_PROZESS = MaskeBauen(Kanal.PROZESS);

        private static bool[] MaskeBauen(params int[] kanaele)
        {
            bool[] m = new bool[Kanal.ANZAHL];
            for (int i = 0; i < kanaele.Length; i++) m[kanaele[i]] = true;
            return m;
        }

        private static bool[][] MaskenBauen()
        {
            bool[][] m = new bool[Kanal.ANZAHL][];
            for (int k = 0; k < Kanal.ANZAHL; k++) m[k] = MaskeBauen(k);
            return m;
        }

        /// <summary>
        /// Kanalmaske einer Direktsenke HEIZKREIS aus ihrer Bedarfsart
        /// (<c>Z_AnlageSenke.Bedarfsart</c>, bis S1 <c>Tab_Energieanlagen.WS_Typ</c>).
        ///
        /// <code>
        /// Warmwasser  -> {BRAUCHWASSER}
        /// Heizung     -> {HEIZUNG}
        /// Beides      -> {BRAUCHWASSER, HEIZUNG}      (und alles Unbekannte)
        /// </code>
        ///
        /// Die gelieferten Masken sind gemeinsam benutzte, UNVERÄNDERLICHE Instanzen.
        /// </summary>
        public static bool[] DirektsenkeMaske(string bedarfsart)
        {
            if (bedarfsart == WaermequelleClass.SENKE_WARMWASSER) return MASKE_WARMWASSER;
            if (bedarfsart == WaermequelleClass.SENKE_HEIZUNG) return MASKE_HEIZUNG;
            return MASKE_BEIDES;      // SENKE_BEIDES und alles Unbekannte
        }

        /// <summary>
        /// Kanalmaske EINER SENKENZEILE (Paket S1, Konzept 5.2) — die eine Stelle, an der
        /// aus einer Zeile von <c>Z_AnlageSenke</c> die Kanäle werden, die sie decken darf.
        ///
        /// <code>
        /// Heizkreis      -> Maske der Bedarfsart (DirektsenkeMaske)
        /// Prozesswaerme  -> {PROZESS}
        /// Puffer-Ziele   -> null   (sie decken keinen Bedarf, sie LADEN)
        /// </code>
        ///
        /// <c>null</c> für Puffersenken ist Absicht und kein Randfall: Eine Puffersenke
        /// hat in <c>SenkeAbziehen</c> nichts zu suchen. Die Aufrufer prüfen deshalb
        /// <c>Senkenzeile.IstDirektsenke</c>, bevor sie hier fragen; die Rückgabe ist die
        /// zweite Sicherung.
        /// </summary>
        public static bool[] SenkenMaske(Senkenzeile zeile)
        {
            if (zeile == null) return MASKE_BEIDES;
            if (zeile.Ziel == Senke.Prozesswaerme) return MASKE_PROZESS;
            if (zeile.IstPuffersenke) return null;
            return DirektsenkeMaske(zeile.Bedarfsart);
        }

        // ------------------------------------------------------------------
        // ZURECHNUNG DER SPEICHERENTLADUNG (Paket-5-Nacharbeit, Befund N2)
        //
        // Sobald ZWEI Erzeuger in der Speicherstufe rechnen, ist „Stufeneingang minus
        // Rest nach der Stufe" kein Eigenanteil mehr, sondern die Lieferung der GANZEN
        // Stufe — meldet jeder Erzeuger diese Größe, wird dieselbe kWh mehrfach als
        // Deckung ausgewiesen, und die Balken des 100-%-Diagramms addieren sich über die
        // tatsächliche Projektdeckung hinaus (gemessen: 1023 85,7 % bei tatsächlich
        // 67,1 %).
        //
        // Der Eigenanteil eines Erzeugers ist deshalb
        //     Direktdeckung (Phase B, je Erzeuger bekannt)
        //   + sein Anteil an der bedarfsdeckenden Speicherentladung (Phasen A/E)
        //   + Heizstab (nur Wärmepumpe — er gehört zu ihr)
        //
        // ZURECHNUNGSREGEL für den mittleren Summanden — „VERMISCHUNG IM SPEICHER":
        // Der Speicherinhalt wird als Mischung geführt; jede Ladung schreibt ihre Menge
        // dem ladenden Erzeuger gut, jede bedarfsdeckende Entladung wird nach den
        // ANTEILEN AM AKTUELLEN INHALT aufgeteilt, und die Bereitschaftsverluste tragen
        // alle Anteile proportional (Angleichung an den Füllstand nach Phase G).
        //
        // WARUM DIESE REGEL: Sie ist die einfachste, die (a) jede kWh genau einem
        // Erzeuger zurechnet — die Summe der Eigenanteile ist damit exakt die Deckung
        // der Stufe, nie mehr —, (b) ohne neue Konfigurationsgröße auskommt und (c) bei
        // GENAU EINEM Lader je Speicher — dem Fall aller neun Referenzprojekte — die
        // gesamte Entladung wie bisher der Wärmepumpe zurechnet. Sie ist die
        // Umsetzung der Variante C aus der Nutzerentscheidung 5-1 — am 15.08.2026
        // BESTÄTIGT (samt Momentanmischung statt Jahres-Ladeanteil, proportionaler
        // Verlusttragung und Zurechnung je Erzeugerart) und damit keine Interimsregel
        // mehr, sondern die gültige Regel. Siehe Paket5_SolarKessel_Protokoll.md,
        // Kapitel 10.
        // ------------------------------------------------------------------

        private const int ART_WP = 0;
        private const int ART_SOLAR = 1;
        private const int ART_KESSEL = 2;
        private const int ART_BHKW = 3;          // Paket 6
        private const int ART_ANZAHL = 4;

        /// <summary>Inhaltsanteile je Speicher und Erzeugerart [kWh].</summary>
        private readonly Dictionary<SimulationPufferspeicher, double[]> _inhaltsanteile =
            new Dictionary<SimulationPufferspeicher, double[]>();

        /// <summary>
        /// Bedarfsdeckende Speicherentladung je Erzeugerart [kWh] — das AGGREGAT über
        /// alle Kanäle.
        ///
        /// Es bleibt neben der kanalindizierten Buchführung bestehen und wird weiter
        /// getrennt fortgeschrieben: <c>SimulationRunner</c> und die Ergebnispersistenz
        /// lesen über <c>Modul.Speicherentladung_Anteil</c> genau diese Zahl, und sie
        /// soll sich mit Paket K2 nicht um die Rundung einer Summenbildung verschieben.
        /// </summary>
        private readonly double[] _entladungJeArt = new double[ART_ANZAHL];

        /// <summary>
        /// Dieselbe Jahressumme, aber KANALINDIZIERT (Konzept 4.1, letzte Tabellenzeilen,
        /// und 4.4): <c>_entladungJeArtKanal[art][kanal]</c> [kWh].
        ///
        /// Die Entladung eines Speichers wird der Erzeugerart UND dem bedienten Kanal
        /// zugerechnet — die Voraussetzung für Deckungsgrade je Kanal. Sie geht als
        /// <c>Modul.Speicherentladung_Kanal</c> an die Erzeugermodule; die Skalare
        /// bleiben daneben bestehen (siehe <see cref="_entladungJeArt"/>).
        ///
        /// JAGGED, nicht <c>[,]</c>: Eine Zeile muss als <c>double[]</c> an ein Modul
        /// übergeben werden können, ohne sie vorher umzukopieren.
        /// </summary>
        private readonly double[][] _entladungJeArtKanal = ZeilenBauen();

        /// <summary>
        /// Dieselbe Zurechnung, aber nur für die LAUFENDE Stunde [kWh] (Nacharbeit
        /// Paket 6, Befund N4) — seit Paket K2 ebenfalls kanalindiziert.
        ///
        /// Der Restwärmebedarf eines Erzeugers ist „Stufeneingang − Direktdeckung −
        /// zugerechnete Entladung". Als Jahressumme steht er in
        /// <c>Tab_ErgebnisBHKW.Restwaermebedarf</c>; damit die GANGLINIE dieselbe Größe
        /// zeigt, braucht sie den Stundenwert der Zurechnung — die Jahressumme allein
        /// lässt sich nicht auf Stunden verteilen.
        /// </summary>
        private readonly double[][] _entladungJeArtStunde = ZeilenBauen();

        private static double[][] ZeilenBauen()
        {
            double[][] z = new double[ART_ANZAHL][];
            for (int a = 0; a < ART_ANZAHL; a++) z[a] = new double[Kanal.ANZAHL];
            return z;
        }

        private static void ZeilenNullen(double[][] z)
        {
            for (int a = 0; a < ART_ANZAHL; a++) Array.Clear(z[a], 0, z[a].Length);
        }

        /// <summary>Erzeugerart (<c>ProjektPuffer.TYP_*</c>) als Index; −1 = nicht geführt.</summary>
        private static int ArtIndex(int typ)
        {
            if (typ == ProjektPuffer.TYP_WP) return ART_WP;
            if (typ == ProjektPuffer.TYP_SOLARTHERMIE) return ART_SOLAR;
            if (typ == ProjektPuffer.TYP_KESSEL) return ART_KESSEL;
            if (typ == ProjektPuffer.TYP_BHKW) return ART_BHKW;
            return -1;
        }

        private double[] Anteile(SimulationPufferspeicher sp)
        {
            double[] a;
            if (!_inhaltsanteile.TryGetValue(sp, out a))
            {
                a = new double[ART_ANZAHL];
                _inhaltsanteile[sp] = a;
            }
            return a;
        }

        /// <summary>Eine Ladung dem ladenden Erzeuger im Speicherinhalt gutschreiben.</summary>
        private void Anteil_Laden(SimulationPufferspeicher sp, int typ, double ladung)
        {
            if (sp == null || ladung <= 0) return;
            int idx = ArtIndex(typ);
            if (idx < 0) return;
            Anteile(sp)[idx] += ladung;
        }

        /// <summary>
        /// Eine bedarfsdeckende Entladung nach den Anteilen am aktuellen Inhalt auf die
        /// Erzeugerarten aufteilen — und zusätzlich dem KANAL zurechnen, in den sie
        /// geflossen ist (Konzept 4.1/4.4, Paket K2).
        ///
        /// Die INHALTSANTEILE selbst bleiben eindimensional je Speicher (Konzept 7.6):
        /// Ein Vorrat ist eine Mischung nach Herkunft, nicht nach Verwendungszweck — es
        /// gibt keine „Brauchwasser-kWh" im Behälter. Kanalindiziert wird allein die
        /// AUSGABE, also die Frage, welcher Bedarf mit dieser Wärme gedeckt wurde.
        /// </summary>
        /// <param name="kanal">Bedarfskanal, den diese Entladung gedeckt hat.</param>
        private void Anteil_Entladen(SimulationPufferspeicher sp, double gedeckt, int kanal)
        {
            if (sp == null || gedeckt <= 0) return;
            if (kanal < 0 || kanal >= Kanal.ANZAHL) return;

            double[] a = Anteile(sp);
            double summe = 0;
            for (int i = 0; i < ART_ANZAHL; i++) if (a[i] > 0) summe += a[i];

            // Inhalt ohne bekannte Herkunft (kann nur entstehen, wenn ein Speicher schon
            // gefüllt in den Lauf geht — Senkenspeicher tun das nicht): nichts zurechnen.
            if (summe <= 0) return;

            for (int i = 0; i < ART_ANZAHL; i++)
            {
                if (a[i] <= 0) { a[i] = 0; continue; }

                double teil = gedeckt * (a[i] / summe);
                if (teil > a[i]) teil = a[i];
                a[i] -= teil;
                _entladungJeArt[i] += teil;
                _entladungJeArtKanal[i][kanal] += teil;
                _entladungJeArtStunde[i][kanal] += teil;   // N4: Stundenwert für die Ganglinie
            }
        }

        /// <summary>
        /// Zurechnung einer Entnahme, deren Kanal nicht feststeht — die Entnahme eines
        /// nachgelagerten Erzeugers aus seinem Quellpuffer mit DIREKTDECKUNG
        /// (<c>Quellentnahme.Ziel == null</c>, Etappe D5a).
        ///
        /// Das Modul meldet die Menge, nicht den Kanal: Es hat sie über sein eigenes
        /// <c>SenkeAbziehen</c> auf mehrere Kanäle verteilt. Für die ART-Summe ist das
        /// gleichgültig — sie ist die Größe, die der Runner liest. Die KANALZEILE bekommt
        /// die Menge deshalb auf dem Heizkanal, der altverhaltenserhaltenden Vorbelegung
        /// des ganzen Kanalmodells (Konzept 4.2/F18).
        ///
        /// Der Fall tritt heute nur bei Wärmepumpe und Heizkessel MIT Quellpuffer auf und
        /// betrifft ausschließlich die kanalfeine Aufteilung der Ergebnisanzeige (E1),
        /// nie eine Bilanzsumme. Mit der einheitlichen <c>Stunde_*</c>-Schnittstelle aus
        /// S1 kann das Modul den Kanal mitmelden; bis dahin steht die Näherung hier an
        /// EINER Stelle statt in vier Modulen.
        /// </summary>
        private void Anteil_Entladen(SimulationPufferspeicher sp, double gedeckt)
        {
            Anteil_Entladen(sp, gedeckt, Kanal.HEIZUNG);
        }

        /// <summary>
        /// UMBUCHUNG der Herkunftsanteile von einem Speicher in einen anderen
        /// (Etappe D5a, Kessel-Kaskade).
        ///
        /// Entnimmt ein nachgelagerter Erzeuger Wärme aus seinem Quellpuffer und lädt sie
        /// — angehoben — in seinen Senkenpuffer, wechselt sie nur den Speicher. Ihre
        /// HERKUNFT wechselt dabei nicht: Erzeugt hat sie der Lader des Quellpuffers.
        /// Genau das leistet diese Buchung; ohne sie bekäme der anhebende Erzeuger die
        /// Menge ein zweites Mal gutgeschrieben, und die Summe der ausgewiesenen
        /// Deckungen liefe über die tatsächliche hinaus (der Fehler, den Befund N2
        /// beseitigt hat).
        ///
        /// Aufgeteilt wird — wie überall in dieser Zurechnung — nach den ANTEILEN AM
        /// AKTUELLEN INHALT des Quellpuffers.
        /// </summary>
        private void Anteil_Umbuchen(SimulationPufferspeicher quelle,
                                     SimulationPufferspeicher ziel, double menge)
        {
            if (quelle == null || ziel == null || menge <= 0) return;

            double[] a = Anteile(quelle);
            double summe = 0;
            for (int i = 0; i < ART_ANZAHL; i++) if (a[i] > 0) summe += a[i];
            if (summe <= 0) return;         // Inhalt ohne bekannte Herkunft

            double[] b = Anteile(ziel);
            for (int i = 0; i < ART_ANZAHL; i++)
            {
                if (a[i] <= 0) { a[i] = 0; continue; }

                double teil = menge * (a[i] / summe);
                if (teil > a[i]) teil = a[i];
                a[i] -= teil;
                b[i] += teil;
            }
        }

        /// <summary>
        /// Verbucht die von einem Erzeugermodul gemeldeten Quellentnahmen in der
        /// Herkunftsrechnung und leert die Meldeliste (Etappe D5a).
        ///
        /// Die PHYSIK der Entnahme hat das Modul schon gebucht (es hat
        /// <c>Entladen</c> gerufen); hier geht es allein um die Frage, WEM die Wärme
        /// zugerechnet bleibt. Siehe <see cref="Quellentnahme"/>.
        /// </summary>
        /// <returns>
        /// Summe der Mengen, die in einen ZIELSPEICHER umgebucht wurden [kWh] — genau der
        /// Betrag, den der ladende Erzeuger sich NICHT gutschreiben darf.
        /// </returns>
        private double QuellentnahmenVerbuchen(List<Quellentnahme> meldungen)
        {
            if (meldungen == null || meldungen.Count == 0) return 0;

            double umgebucht = 0;
            for (int i = 0; i < meldungen.Count; i++)
            {
                Quellentnahme q = meldungen[i];
                if (q == null || q.Quelle == null || q.Menge <= 0) continue;

                if (q.Ziel == null)
                {
                    Anteil_Entladen(q.Quelle, q.Menge);
                }
                else
                {
                    Anteil_Umbuchen(q.Quelle, q.Ziel, q.Menge);
                    umgebucht += q.Menge;
                }
            }

            meldungen.Clear();
            return umgebucht;
        }

        // ------------------------------------------------------------------
        // DURCHSATZBUDGET DER STUNDE — eine Fassung für alle vier Erzeugerarten
        //
        // Bis Etappe D5a rechnete jedes Modul die beiden Zeilen selbst und griff dabei
        // über „IstBrauchwasserkanal ? 1 : 0" auf GENAU EINEN Kanal zu. Der
        // KOMBISPEICHER bedient beide, also ist auch sein Durchsatz die Summe beider
        // Kanäle — und die Abbuchung muss auf beide gehen. Vier Kopien dieser Regel
        // wären vier Gelegenheiten, sie unterschiedlich zu treffen.
        //
        // PAKET K2: Das Budget ist double[Kanal.ANZAHL].
        //
        // PAKET S1: Maßgeblich ist das ECHTE Klassen-Set des Speichers
        // (SimulationPufferspeicher.BedientKanal) — die Interimsregel I2 ist abgerissen.
        // Entladung und Budget fragen damit weiterhin DIESELBE Quelle; liefen sie
        // auseinander, bekäme die hydraulische Weiche einen Bedarf zu sehen, den die
        // Entladung nicht bedienen darf (oder umgekehrt).
        //
        // Ein Speicher mit genau einem Kanal rechnet Anweisung für Anweisung wie zuvor.
        // ------------------------------------------------------------------

        /// <summary>
        /// Absehbare Entnahme, die dieser Speicher in der laufenden Stunde zusätzlich
        /// durchreichen kann [kWh] (Nutzerentscheidung zu Befund 4b-1) — die Summe der
        /// offenen Bedarfe ALLER Kanäle, die er entlädt, gedeckelt auf seine
        /// Entnahmefähigkeit.
        /// </summary>
        public static double DurchlassBudget(SimulationPufferspeicher sp, double[] absehbar)
        {
            if (sp == null || absehbar == null) return 0;

            double offen = 0;
            for (int k = 0; k < Kanal.ANZAHL && k < absehbar.Length; k++)
                if (absehbar[k] > 0 && sp.BedientKanal(k)) offen += absehbar[k];

            return Math.Min(offen, sp.Entnahmefaehigkeit());
        }

        /// <summary>
        /// Bucht den tatsächlich genutzten Durchlass vom Budget ab — in der
        /// KNAPPHEITSREIHENFOLGE des Laufs (Konzept 4.3). Sie ist dieselbe Ordnung, in
        /// der die Entladung den Vorrat vergibt; die frühere Sonderregel „beim
        /// Kombispeicher zuerst vom Warmwasserkanal" (K-1) ist genau ihr Sonderfall und
        /// geht darin auf.
        /// </summary>
        public static void DurchlassBuchen(SimulationPufferspeicher sp, double[] absehbar,
                                           double genutzt)
        {
            if (sp == null || absehbar == null || genutzt <= 0) return;

            int[] ordnung = KnappheitDesLaufs();
            double offen = genutzt;

            for (int i = 0; i < ordnung.Length; i++)
            {
                int k = ordnung[i];
                if (k < 0 || k >= absehbar.Length) continue;
                if (!sp.BedientKanal(k)) continue;

                double teil = Math.Min(offen, absehbar[k] > 0 ? absehbar[k] : 0);
                absehbar[k] -= teil;
                if (absehbar[k] < 0) absehbar[k] = 0;

                offen -= teil;
                if (offen <= 0) return;
            }

            // Rest ohne Deckung im Budget: Er kann nur aus einer Rundung stammen (das
            // Modul hat gegen DASSELBE Budget geladen). Der letzte bediente Kanal trägt
            // ihn - dieselbe Klemmung wie bisher, nur ohne feste Kanalnummer.
            for (int i = ordnung.Length - 1; i >= 0 && offen > 0; i--)
            {
                int k = ordnung[i];
                if (k < 0 || k >= absehbar.Length) continue;
                if (!sp.BedientKanal(k)) continue;

                absehbar[k] -= offen;
                if (absehbar[k] < 0) absehbar[k] = 0;
                return;
            }
        }

        /// <summary>
        /// Knappheitsreihenfolge des LAUFENDEN Laufs für die beiden statischen Methoden,
        /// die die Erzeugermodule rufen (<see cref="DurchlassBuchen"/> und die
        /// Kompatibilitätsfassung von <see cref="SenkeAbziehen(string, double, double[])"/>).
        ///
        /// WARUM EIN STATISCHES FELD. Beide Methoden sind static, weil sie aus vier
        /// Modulen und aus den VEKTORSTUFEN heraus gerufen werden — also auch außerhalb
        /// von <see cref="Rechnen"/>, wo es gar keine Schleifeninstanz gibt. Die
        /// Reihenfolge ist eine Eigenschaft des LAUFS, nicht des Aufrufers; sie durch
        /// vier Modulsignaturen zu reichen hieße, sie an vier Stellen setzen zu können.
        /// Gesetzt wird sie genau einmal, von <c>SimulationControl</c> zu Beginn der
        /// zweikanaligen Kaskade (<see cref="KnappheitFuerLauf"/>).
        /// </summary>
        private static int[] _knappheitLauf = Kanal.KnappheitVorgabe();

        /// <summary>
        /// Setzt die Knappheitsreihenfolge des Laufs (siehe <see cref="_knappheitLauf"/>).
        /// <c>null</c> oder eine unbrauchbare Länge stellen die Vorbelegung wieder her —
        /// ein Lauf ohne gesetzte Reihenfolge rechnet mit {B, P, H}.
        /// </summary>
        public static void KnappheitFuerLauf(int[] ordnung)
        {
            _knappheitLauf = (ordnung != null && ordnung.Length == Kanal.ANZAHL)
                ? ordnung : Kanal.KnappheitVorgabe();
        }

        private static int[] KnappheitDesLaufs()
        {
            return _knappheitLauf ?? Kanal.KnappheitVorgabe();
        }

        /// <summary>
        /// Anteile nach Phase G an den Füllstand angleichen: Die Bereitschaftsverluste
        /// des Speichers tragen alle Erzeuger proportional zu ihrem Anteil.
        /// </summary>
        private void Anteil_Angleichen(SimulationPufferspeicher sp)
        {
            if (sp == null) return;

            double[] a;
            if (!_inhaltsanteile.TryGetValue(sp, out a)) return;

            double summe = 0;
            for (int i = 0; i < ART_ANZAHL; i++) if (a[i] > 0) summe += a[i];
            if (summe <= 0) return;

            double soc = sp.SOC > 0 ? sp.SOC : 0;
            double faktor = soc / summe;
            for (int i = 0; i < ART_ANZAHL; i++) a[i] = (a[i] > 0) ? a[i] * faktor : 0;
        }

        /// <summary>
        /// Rechnet das ganze Jahr. Die Kanäle werden IN PLACE fortgeschrieben: Am Ende
        /// jeder Stunde stehen in <paramref name="kanaele"/> die Restbedarfe, mit denen
        /// die nächste Stufe der Kaskade weiterrechnet.
        /// </summary>
        /// <returns>false = Abbruch (Kennlinienauswertung der Wärmepumpe).</returns>
        public bool Rechnen(Kanalsatz kanaele)
        {
            if (kanaele == null || Kontext == null) return false;

            Fehlertext = "";

            // ETAPPE D5a: Rechenebenen aus den Quellbezügen. Schlägt die Auflösung fehl,
            // liegt ein RING vor (A lädt B, B ist Quelle von A) - dann gibt es keine
            // Reihenfolge, in der beide „nach ihrem Puffer" rechnen. Der Lauf bricht ab,
            // statt eine willkürliche Reihenfolge zu wählen und ein plausibel aussehendes
            // Ergebnis zu speichern.
            if (!EbenenAufloesen()) return false;

            // PAKET K2: Knappheitsreihenfolge des Laufs EINMAL auflösen - sie steuert
            // Abzug, Entladung und Durchsatzabbuchung (Konzept 4.3).
            _knappheit = (Kontext.Knappheit != null && Kontext.Knappheit.Length == Kanal.ANZAHL)
                ? Kontext.Knappheit : Kanal.KnappheitVorgabe();
            KnappheitFuerLauf(_knappheit);

            // PAKET S1: Zahl der Ladephasen je Rechenebene - EINMAL aufgelöst statt
            // 8760-mal über die Auftragsliste gesucht. Ohne Senken jenseits von Rang 2
            // (jedes migrierte Bestandsprojekt) sind es genau die bisherigen zwei
            // Durchläufe C und D.
            _maxRang = Kontext.MaxLaderang();

            List<double> biv = new List<double>();

            // N2: Zurechnung der Speicherentladung auf den Laufanfang.
            _inhaltsanteile.Clear();
            Array.Clear(_entladungJeArt, 0, _entladungJeArt.Length);
            ZeilenNullen(_entladungJeArtKanal);

            if (MitWP)
            {
                if (!WP.Zweikanalig_Start(kanaele, Kontext)) return false;
            }
            else
            {
                // Ohne Wärmepumpe erledigt die Schleife selbst, was sonst
                // Zweikanalig_Start tut: Senkenspeicher auf den Laufanfang. QUELLspeicher
                // NICHT — sie starten gefüllt.
                foreach (SimulationPufferspeicher sp in Kontext.AlleSpeicher)
                    if (sp != null && !sp.IstQuelle) sp.Reset();
            }

            float[] pvUeberschussVektor = MitWP ? WP.PV_Ueberschuss_stuendlich : null;

            // Paket 6: Das BHKW braucht seine Ladeaufträge schon in Phase B — die
            // Ladefähigkeit seiner (Ersatz-)Zweitsenke ist der Speicherraum, mit dem die
            // Fahrweise ihre Motoren zuschaltet (im Altpfad der Pendelspeicher).
            if (MitBHKW) BhkwAuftraegeZuordnen();

            // BEFUND N5: Der Durchsatzterm des Bilanzraums gilt nur, wenn das BHKW die
            // LETZTE Stufe der Bedarfsreihenfolge ist — nur dann ist der Kanalstand, den
            // es in Phase B sieht, das Durchsatzbudget der Ladephase (siehe
            // SimulationBHKW.ZweitsenkenRaum).
            if (MitBHKW)
                BHKW.LetzteBedarfsstufe = Bedarfsreihenfolge.Count == 0 ||
                    Bedarfsreihenfolge[Bedarfsreihenfolge.Count - 1] == ProjektPuffer.TYP_BHKW;

            // Absehbare Entnahme je Kanal in der laufenden Stunde [kWh] — der Durchsatz
            // der hydraulischen Weiche (Nutzerentscheidung zu 4b-1), indiziert nach
            // <see cref="Kanal"/>. Das Budget wird über die Phasen C und D hinweg NUR
            // EINMAL vergeben: Zwei Speicher desselben Kanals dürfen nicht beide dieselbe
            // Entnahme durchreichen, sonst bliebe nach Phase E Wärme im Speicher stehen,
            // die niemand angefordert hat.
            double[] absehbar = new double[Kanal.ANZAHL];

            // STUNDENZUSTAND der Kanäle [kWh] — der Restbedarf, den die Phasen A bis F
            // fortschreiben. EIN Feld statt der beiden ref-Parameter rest_heiz/rest_ww:
            // Es wird an die Module durchgereicht und dort IN PLACE verändert; damit gibt
            // es keine Modulsignatur mehr, die beim Hinzukommen eines Kanals wächst.
            // Bewusst VOR der Stundenschleife angelegt und je Stunde neu befüllt (dieselbe
            // Konvention wie beim Budget) — 8760 Feldanlagen wären reine Arbeit für den
            // Sammler.
            double[] rest = new double[Kanal.ANZAHL];

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                for (int k = 0; k < Kanal.ANZAHL; k++) rest[k] = kanaele.Bedarf[k][stunde];

                // N4: Zurechnung der Entladung auf den Anfang DIESER Stunde.
                ZeilenNullen(_entladungJeArtStunde);

                // N3: Reservierungen der Vorstunde verfallen. Sie gelten nur innerhalb
                // einer Stunde - zwischen Phase B (Motorzuschaltung) und Phase C/D
                // (Einlagerung). Eine nicht eingelöste Reservierung darf sich nicht in
                // die nächste Stunde schleppen und dort Ladefähigkeit sperren.
                foreach (SimulationPufferspeicher sp in Kontext.AlleSpeicher)
                    if (sp != null) sp.Reserviert = 0;

                // STUFENEINGANG je Erzeugerstufe (N1): der Kanalstand VOR Phase A.
                if (MitWP) WP.Zweikanalig_StundeStart(stunde);
                if (MitSolar) Solar.Stunde_Start(stunde, rest);
                if (MitKessel) Kessel.Stunde_Start(stunde, rest);
                if (MitBHKW) BHKW.Stunde_Start(stunde, rest);

                double pvRest = (pvUeberschussVektor != null && stunde < pvUeberschussVektor.Length)
                    ? pvUeberschussVektor[stunde] : 0;

                // Kriterium der zeitabhängigen Ladepriorität (Konzept 3.5): der
                // PV-Überschuss VOR seinem Verbrauch in dieser Stunde.
                bool pvUeberschuss = pvRest > 0;

                // Regeneration der Quellspeicher — EINMAL je Speicher und Stunde. Im
                // Altpfad steht sie in der Modulschleife; mit der gemeinsamen Instanz
                // (QuellspeicherZusammenfuehren) würde sie dort mehrfach gutgeschrieben.
                foreach (SimulationPufferspeicher q in Kontext.AlleSpeicher)
                    if (q != null && q.IstQuelle && q.RegenerationProStunde > 0)
                        q.Laden(q.RegenerationProStunde, stunde);

                // --- A) Vorabentladung ------------------------------------------------
                Entladephase(stunde, true, rest);

                // --- B/C/D je RECHENEBENE (Etappe D5a) ---------------------------------
                // Mit genau einer Ebene - jedes Bestandsprojekt - läuft der Rumpf einmal
                // und ist die bisherige Folge B, Budget, C, D.
                for (int ebene = 0; ebene <= _maxEbene; ebene++)
                {
                    // --- B) Bedarfsdeckung in Kaskadenreihenfolge ----------------------
                    List<int> arten = BedarfsreihenfolgeDerEbene(ebene);
                    ModulEbeneSetzen(ebene);

                    for (int s = 0; s < arten.Count; s++)
                    {
                        int art = arten[s];

                        if (art == ProjektPuffer.TYP_WP && MitWP)
                        {
                            if (!WP.Zweikanalig_Bedarfsphase(stunde, Kontext, pvUeberschuss, pvRest,
                                                             rest))
                                return false;
                            QuellentnahmenVerbuchen(WP.Quellentnahmen);
                        }
                        else if (art == ProjektPuffer.TYP_SOLARTHERMIE && MitSolar)
                        {
                            Solar.Stunde_Bedarf(stunde, rest);
                        }
                        else if (art == ProjektPuffer.TYP_KESSEL && MitKessel)
                        {
                            Kessel.Stunde_Bedarf(stunde, rest);
                            QuellentnahmenVerbuchen(Kessel.Quellentnahmen);
                        }
                        else if (art == ProjektPuffer.TYP_BHKW && MitBHKW)
                        {
                            BHKW.Stunde_Bedarf(stunde, pvUeberschuss, rest);
                        }
                    }

                    // Durchsatzbudget der Stunde festhalten — Stand NACH der
                    // Bedarfsdeckung. Genau diesen Rest kann Phase E aus den Speichern
                    // ziehen; zwischen C und E verändert ihn nichts.
                    for (int k = 0; k < Kanal.ANZAHL; k++)
                        absehbar[k] = rest[k] > 0 ? rest[k] : 0;

                    // --- C…) LADEPHASEN JE RANG (Paket S1, Konzept 5.2) -------------------
                    // Rang für Rang aufsteigend, jede Ebene kaskadenübergreifend nach
                    // Ladeordnung. Mit den migrierten Bestandsdaten (Rang 1 = bisherige
                    // Hauptsenke, Rang 2 = bisherige Zweitsenke) sind das Anweisung für
                    // Anweisung die bisherigen Phasen C und D.
                    for (int rang = 1; rang <= _maxRang; rang++)
                        Ladephase(stunde, rang, pvUeberschuss, ref pvRest, absehbar, ebene);

                    // ZWISCHENSCHRITT DER KASKADE (Etappe D5a): Was die Speicher dieser
                    // Ebene gerade DURCHGEREICHT haben, gehört dem Verbraucher — nicht dem
                    // Erzeuger der nächsten Ebene. Ohne diese Rückgabe sähe die nächste
                    // Ebene einen Bedarf, den die vorige Ebene bereits über ihre
                    // hydraulische Weiche bedient hat, und deckte ihn ein zweites Mal.
                    // Der gespeicherte INHALT bleibt liegen — ihn holt Phase E am Ende der
                    // Stunde, nachdem alle Ebenen ihre Quellentnahme hatten.
                    if (ebene < _maxEbene)
                        DurchsatzPhase(stunde, rest);
                }

                // --- E) Nachentladung -----------------------------------------------------
                Entladephase(stunde, false, rest);

                // Bivalenzpunkt — dieselbe Stelle wie im Altpfad: nach der Entladung,
                // vor dem Heizstab. Maßgeblich ist der offene GESAMTbedarf; welcher Kanal
                // ihn trägt, spielt für die Bivalenztemperatur keine Rolle.
                if (MitWP && RestSumme(rest) > 0) biv.Add(WP.Temperatur[stunde]);

                // --- F) Heizstab ----------------------------------------------------------
                if (MitWP) WP.Heizstabphase(stunde, rest);

                // --- G) StundeAbschliessen je Registry-Speicher, GENAU EINMAL -------------
                foreach (SimulationPufferspeicher sp in Kontext.AlleSpeicher)
                {
                    if (sp == null) continue;

                    // Abschaltprüfung VOR den Bereitschaftsverlusten (wie im Altpfad),
                    // sonst wird der Vollstand nie erreicht.
                    if (!sp.IstQuelle && sp.Q_max > 0 && sp.LaedtGerade &&
                        sp.SOC >= sp.Q_max * sp.SchwelleAus)
                        sp.LaedtGerade = false;

                    sp.StundeAbschliessen(stunde);

                    // N2: Die Bereitschaftsverluste dieser Stunde tragen alle Erzeuger
                    // anteilig - der Speicherinhalt bleibt eine Mischung.
                    Anteil_Angleichen(sp);
                }

                // Brennstoffbilanz der Kessel — ebenfalls GENAU EINMAL je Stunde und
                // Kessel, und erst jetzt: Vorher steht nicht fest, ob der Kessel in
                // dieser Stunde gelaufen ist (Bedarfsdeckung ODER Speicherladung) oder
                // ob ihm der Bereitschaftsverlust anzulasten ist (Konzept 6.5).
                if (MitKessel) Kessel.Stunde_Abschluss(stunde);

                // Restbedarf in die Kanäle zurückschreiben — Eingang der nächsten Stufe
                // der Kaskade.
                for (int k = 0; k < Kanal.ANZAHL; k++)
                {
                    if (rest[k] < 0) rest[k] = 0;
                    kanaele.Bedarf[k][stunde] = (float)rest[k];
                }

                if (MitWP) WP.Zweikanalig_StundeEnde(stunde, rest);

                // Solarthermie: Was weder gedeckt noch gespeichert wurde, ist verworfen.
                if (MitSolar) Solar.Stunde_Ende(stunde);

                // BHKW: Was weder gedeckt noch gespeichert wurde, ist Wärmeüberschuss
                // (Paket 6 — im Altpfad kannte nur die stromgeführte Fahrweise diese
                // Größe, als Überlauf des Pendelspeichers). Dazu die Ganglinie seines
                // Restwärmebedarfs, gebildet an der BHKW-Position aus Stufeneingang,
                // Direktdeckung und der ihm in dieser Stunde zugerechneten Entladung (N4).
                // Die Ganglinie des BHKW-Restwärmebedarfs ist eine KANALLOSE Größe
                // („Stufeneingang − Direktdeckung − zugerechnete Entladung", N4); sie
                // bekommt deshalb die Kanalsumme der Stundenzurechnung.
                if (MitBHKW) BHKW.Stunde_Ende(stunde, ZeilenSumme(_entladungJeArtStunde[ART_BHKW]));

            } // end alle Stunden

            if (MitWP) WP.Zweikanalig_Ende(biv);
            if (MitSolar) Solar.Abschluss_Zweikanalig();
            if (MitKessel) Kessel.Abschluss_Zweikanalig();
            if (MitBHKW) BHKW.Abschluss_Zweikanalig();

            // N2: Zugerechnete Speicherentladung an die Erzeugermodule geben. Sie ist der
            // zweite Summand ihres EIGENANTEILS an der Bedarfsdeckung; den ersten
            // (Direktdeckung) führt jedes Modul selbst.
            //
            // PAKET K2: dazu die KANALZEILE derselben Größe (Konzept 4.1/4.4). Der Skalar
            // bleibt die führende Zahl für Runner und Ergebnispersistenz und wird
            // ausdrücklich NICHT aus der Zeile aufsummiert — er ist getrennt akkumuliert
            // und soll sich durch den Umbau nicht um die Rundung einer Summe verschieben.
            if (MitWP)
            {
                WP.Speicherentladung_Anteil = _entladungJeArt[ART_WP];
                KanalzeileUebergeben(ART_WP, WP.Speicherentladung_Kanal);
            }
            if (MitSolar)
            {
                Solar.Speicherentladung_Anteil = _entladungJeArt[ART_SOLAR];
                KanalzeileUebergeben(ART_SOLAR, Solar.Speicherentladung_Kanal);
            }
            if (MitKessel)
            {
                Kessel.Speicherentladung_Anteil = _entladungJeArt[ART_KESSEL];
                KanalzeileUebergeben(ART_KESSEL, Kessel.Speicherentladung_Kanal);
            }
            if (MitBHKW)
            {
                BHKW.Speicherentladung_Anteil = _entladungJeArt[ART_BHKW];
                KanalzeileUebergeben(ART_BHKW, BHKW.Speicherentladung_Kanal);
            }

            return true;
        }

        /// <summary>
        /// Schreibt die Kanalzeile einer Erzeugerart in das Zielfeld des Moduls [kWh].
        ///
        /// KOPIERT statt zugewiesen: Das Modul legt sein Feld selbst an und nullt es in
        /// seiner Vorbereitung; ein ausgetauschtes Array wäre eine Aliasing-Falle für
        /// jeden, der sich die Referenz gemerkt hat (Regel B0-2).
        /// </summary>
        private void KanalzeileUebergeben(int art, double[] ziel)
        {
            if (ziel == null) return;

            double[] quelle = _entladungJeArtKanal[art];
            for (int k = 0; k < Kanal.ANZAHL && k < ziel.Length; k++) ziel[k] = quelle[k];
        }

        /// <summary>Summe einer Kanalzeile [kWh].</summary>
        private static double ZeilenSumme(double[] zeile)
        {
            double s = 0;
            for (int k = 0; k < zeile.Length; k++) s += zeile[k];
            return s;
        }

        /// <summary>
        /// Offener Bedarf ÜBER ALLE KANÄLE [kWh] — der Stufeneingang bzw. der Restbedarf,
        /// den die Erzeugermodule als EINE Zahl führen (Ganglinien, Maxima, Jahressummen).
        /// Vor Paket K2 stand dafür überall <c>rest_heiz + rest_ww</c>.
        ///
        /// <b>PUBLIC seit Paket S1 (K2-O1):</b> Bis dahin gab es dieselbe Schleife ein
        /// zweites Mal als <c>Kanalabzug.Summe</c> im Wärmepumpen-Modul. Zwei Fassungen
        /// derselben Summe sind zwei Gelegenheiten, sie unterschiedlich zu bilden — und
        /// bei float-Akkumulation ist „unterschiedlich" nicht nur eine Formfrage. Es gibt
        /// jetzt nur noch diese hier.
        /// </summary>
        public static double RestSumme(double[] rest)
        {
            if (rest == null) return 0;

            double s = 0;
            for (int k = 0; k < Kanal.ANZAHL && k < rest.Length; k++) s += rest[k];
            return s;
        }

        /// <summary>
        /// Ordnet dem BHKW seine Ladeaufträge zu (Paket 6).
        ///
        /// Anders als Wärmepumpe, Solarthermie und Heizkessel braucht das BHKW seine
        /// Aufträge nicht erst in der Ladephase: Die Fahrweisen entscheiden die
        /// Motorzuschaltung gegen <c>Bedarf + Speicherraum</c>, und der Speicherraum ist
        /// die Summe der Ladefähigkeiten seiner Puffersenken. Bei einer Direktsenke fällt
        /// diese Entscheidung in Phase B, also vor der Ladephase — deshalb werden die
        /// Aufträge hier einmal je Lauf herausgesucht statt je Stunde.
        ///
        /// <para><b>PAKET S1 (Konzept 5.2/F11).</b> Aus den beiden Auftragsslots
        /// <c>Auftrag_Haupt</c>/<c>Auftrag_Zweit</c> ist EINE LISTE je Stufe geworden:
        /// alle Puffersenken-Aufträge der führenden BHKW-Anlage, nach
        /// <see cref="Ladeauftrag.Rang"/> aufsteigend. Bei höchstens zwei Senken — jedes
        /// migrierte Bestandsprojekt — enthält sie genau die beiden bisherigen Aufträge in
        /// genau der bisherigen Reihenfolge.</para>
        /// </summary>
        private void BhkwAuftraegeZuordnen()
        {
            if (BHKW.Auftraege == null) BHKW.Auftraege = new List<Ladeauftrag>();
            BHKW.Auftraege.Clear();

            if (Kontext == null || Kontext.LadenOhnePV == null) return;

            foreach (Ladeauftrag a in Kontext.LadenOhnePV)
            {
                if (a == null || a.Erzeugerart != ProjektPuffer.TYP_BHKW) continue;
                if (a.AnlagenID != BHKW.FuehrendeAnlage) continue;

                BHKW.Auftraege.Add(a);
            }

            // NACH RANG, nicht nach Ladeordnung: Die Liste ist die SENKENKETTE der Stufe
            // (Konzept 5.2) - die Reihenfolge, in der das BHKW seine Speicher bedient.
            // Die kaskadenübergreifende Ladeordnung steht daneben und bleibt die Ordnung
            // der Ladephasen selbst.
            BHKW.Auftraege.Sort(delegate (Ladeauftrag a, Ladeauftrag b)
            {
                return a.Rang.CompareTo(b.Rang);
            });

            // PAKET BHKW-REGULÄR: Die Speicher, auf die das BHKW angewiesen ist, bekommen
            // ihre NOTRESERVE scharf gestellt. Erst hier steht fest, WELCHE das sind - die
            // Ladeaufträge entstehen aus der Senkenliste und der Ladeordnung, nicht aus der
            // Puffertabelle.
            //
            // Das Feld wird ausschließlich hier gesetzt und nirgends zurückgenommen: Ein
            // Speicher, den das BHKW in diesem Lauf lädt, behält die Reserve über das ganze
            // Jahr. Ohne BHKW in der Stufe läuft diese Methode nicht (MitBHKW), und die
            // Reserve bleibt an jedem Speicher unwirksam.
            foreach (Ladeauftrag a in BHKW.Auftraege) ReserveScharfstellen(a);
        }

        /// <summary>
        /// Stellt den Mindestfüllstand des Zielspeichers eines BHKW-Ladeauftrags scharf
        /// (Paket BHKW-Regulär). Ohne Auftrag oder ohne Speicher ein No-op.
        ///
        /// Die PROZENTZAHL aus <c>Tab_Pufferspeicher.Schwelle_Reserve</c> steht bereits als
        /// ANTEIL (0…1) im Speicherobjekt — umgerechnet wird sie beim Übertragen der
        /// Puffer-Parameter (<c>SimulationControl</c>), wie bei Ein- und Abschaltschwelle
        /// auch. Hier wird nur der Schalter gesetzt.
        /// </summary>
        private static void ReserveScharfstellen(Ladeauftrag a)
        {
            if (a == null || a.Speicher == null) return;
            a.Speicher.BhkwReserveGilt = true;
        }

        /// <summary>
        /// EINE LADEPHASE — alle Puffersenken EINES RANGS, aus der Kaskade gelöst
        /// (Konzept 6.3, seit Paket S1 je Rang statt der beiden festen Phasen C und D).
        ///
        /// Iteriert über die kaskadenübergreifende Prioritätsordnung der Stunde — nicht
        /// über eine Modulliste. Dass Solarthermie in Kaskadenposition 3 vor einer
        /// Wärmepumpe in Position 1 laden darf, ist der Zweck der Ladepriorität (3.4);
        /// seit Paket 5 stehen Solarthermie (Vorgaberang 10), Wärmepumpe (20) und
        /// Heizkessel (40) gemeinsam in dieser Ordnung.
        ///
        /// Die Buchung übernimmt das jeweilige Erzeugermodul: Es kennt sein Potenzial,
        /// seinen Strom- bzw. Brennstoffbedarf und seine Wärmequelle. Gemeinsam sind
        /// allein die Ordnung, der Bilanzraum und das Durchsatzbudget.
        /// </summary>
        /// <param name="rang">
        /// Rang der Senkenzeile (Konzept 5.2). 1 = die bisherige Phase C (Hauptsenken),
        /// 2 = die bisherige Phase D (Zweitsenken), darüber die mit S1 neu möglichen
        /// weiteren Senken. Das BUDGET der Stunde ist über alle Ränge dasselbe: Was ein
        /// früherer Rang aufgenommen hat, fehlt dem späteren — genau das ist die Regel
        /// „eine kWh, genau ein Ziel".
        /// </param>
        /// <param name="ebene">
        /// Rechenebene dieses Durchlaufs (Etappe D5a). Es laden ausschließlich die
        /// Anlagen dieser Ebene; ohne Quellbezug auf einen geladenen Puffer tragen alle
        /// Aufträge Ebene 0 und die Methode arbeitet die ganze Ordnung ab wie bisher.
        /// </param>
        private void Ladephase(int stunde, int rang, bool pvUeberschuss,
                               ref double pvRest, double[] absehbar, int ebene)
        {
            List<Ladeauftrag> ordnung = Kontext.Ladeordnung_Stunde(pvUeberschuss);
            if (ordnung == null) return;

            for (int n = 0; n < ordnung.Count; n++)
            {
                Ladeauftrag a = ordnung[n];
                if (a == null || a.Rang != rang) continue;
                if (a.Ebene != ebene) continue;

                // Die geladene Menge geht zusätzlich in die Herkunftsrechnung des
                // Speichers (N2) — sie entscheidet später, wem seine Entladung als
                // Bedarfsdeckung gutgeschrieben wird.
                //
                // ETAPPE D5a: Hat der Erzeuger einen Teil der Ladung aus SEINEM
                // Quellpuffer geholt, ist dieser Teil nicht seine Wärme. Er wird über
                // Anteil_Umbuchen mit seiner Herkunft in den Zielspeicher übertragen; dem
                // Erzeuger bleibt nur, was er selbst beigesteuert hat. Ohne Quellpuffer
                // ist der Abzug exakt 0 und die Buchung die bisherige.
                if (a.Erzeugerart == ProjektPuffer.TYP_WP)
                {
                    if (MitWP)
                    {
                        double geladen = WP.Zweikanalig_Laden(a, stunde, pvUeberschuss, absehbar, ref pvRest);
                        Anteil_Laden(a.Speicher, a.Erzeugerart,
                                     geladen - QuellentnahmenVerbuchen(WP.Quellentnahmen));
                    }
                }
                else if (a.Erzeugerart == ProjektPuffer.TYP_SOLARTHERMIE)
                {
                    if (MitSolar)
                        Anteil_Laden(a.Speicher, a.Erzeugerart,
                                     Solar.Zweikanalig_Laden(a, stunde, pvUeberschuss, absehbar));
                }
                else if (a.Erzeugerart == ProjektPuffer.TYP_KESSEL)
                {
                    if (MitKessel)
                    {
                        double geladen = Kessel.Zweikanalig_Laden(a, stunde, pvUeberschuss, absehbar);
                        Anteil_Laden(a.Speicher, a.Erzeugerart,
                                     geladen - QuellentnahmenVerbuchen(Kessel.Quellentnahmen));
                    }
                }
                else if (a.Erzeugerart == ProjektPuffer.TYP_BHKW)
                {
                    if (MitBHKW)
                        Anteil_Laden(a.Speicher, a.Erzeugerart,
                                     BHKW.Zweikanalig_Laden(a, stunde, pvUeberschuss, absehbar));
                }
            }
        }

        // ==================================================================
        // AUFLÖSUNG DER RECHENEBENEN (Etappe D5a)
        // ==================================================================

        /// <summary>
        /// Bestimmt je Anlage ihre Rechenebene aus den Quellbezügen und schreibt sie in
        /// die Ladeaufträge und die Modulmasken.
        ///
        /// REGEL: Ebene(A) = 0, wenn A keinen Quellpuffer hat oder ihren Quellpuffer in
        /// diesem Lauf niemand lädt; sonst Ebene(A) = 1 + max{ Ebene(L) : L lädt den
        /// Quellpuffer von A }. Aufgelöst wird iterativ; ein Ring lässt die Ebenen
        /// unbegrenzt wachsen und wird nach so vielen Durchläufen erkannt, wie es
        /// Anlagen gibt — mehr kann eine zyklenfreie Kette nicht brauchen.
        /// </summary>
        /// <returns>false = Ring erkannt, <see cref="Fehlertext"/> ist gesetzt.</returns>
        private bool EbenenAufloesen()
        {
            _ebeneJeAnlage.Clear();
            _maxEbene = 0;
            _bedarfJeEbene = null;

            List<int> alleAnlagen = AlleAnlagen();
            foreach (int id in alleAnlagen) _ebeneJeAnlage[id] = 0;

            // Lader je Speicher aus den Ladeaufträgen — die eine Quelle der Wahrheit,
            // aus der auch die Ladeordnung selbst gebildet ist (Konzept 3.4).
            Dictionary<SimulationPufferspeicher, List<int>> laderJeSpeicher =
                new Dictionary<SimulationPufferspeicher, List<int>>();

            if (Kontext.LadenOhnePV != null)
            {
                foreach (Ladeauftrag a in Kontext.LadenOhnePV)
                {
                    if (a == null || a.Speicher == null || a.AnlagenID <= 0) continue;

                    List<int> lader;
                    if (!laderJeSpeicher.TryGetValue(a.Speicher, out lader))
                    {
                        lader = new List<int>();
                        laderJeSpeicher[a.Speicher] = lader;
                    }
                    if (!lader.Contains(a.AnlagenID)) lader.Add(a.AnlagenID);
                }
            }

            // Ohne einen einzigen Quellbezug auf einen GELADENEN Speicher gibt es keine
            // Kaskade: Alles bleibt Ebene 0, und die Stundenschleife läuft wie bisher.
            bool relevant = false;
            foreach (KeyValuePair<int, SimulationPufferspeicher> q in Kontext.QuellpufferJeAnlage)
                if (q.Value != null && laderJeSpeicher.ContainsKey(q.Value) &&
                    _ebeneJeAnlage.ContainsKey(q.Key)) { relevant = true; break; }

            if (relevant && !EbenenRelaxieren(laderJeSpeicher, alleAnlagen.Count)) return false;

            // Ebene in die Ladeaufträge schreiben (beide Ordnungen zeigen auf DIESELBEN
            // Auftragsobjekte — die Zuweisung wirkt in beiden).
            SchreibeAuftragsebenen(Kontext.LadenOhnePV);
            SchreibeAuftragsebenen(Kontext.LadenMitPV);

            BedarfsordnungJeEbeneBilden();
            ModulmaskenSchreiben();
            return true;
        }

        /// <summary>Iterative Auflösung der Ebenen; false = Ring (siehe <see cref="EbenenAufloesen"/>).</summary>
        private bool EbenenRelaxieren(Dictionary<SimulationPufferspeicher, List<int>> laderJeSpeicher,
                                      int anzahlAnlagen)
        {
            for (int runde = 0; runde <= anzahlAnlagen; runde++)
            {
                bool geaendert = false;

                foreach (KeyValuePair<int, SimulationPufferspeicher> bezug in Kontext.QuellpufferJeAnlage)
                {
                    int idAnlage = bezug.Key;
                    if (!_ebeneJeAnlage.ContainsKey(idAnlage)) continue;   // rechnet nicht mit

                    List<int> lader;
                    if (bezug.Value == null || !laderJeSpeicher.TryGetValue(bezug.Value, out lader))
                        continue;                                          // Quelle lädt niemand

                    int soll = 0;
                    foreach (int idLader in lader)
                    {
                        if (idLader == idAnlage) continue;                 // sich selbst laden ist
                                                                           // der Kurzschluss aus 4.6
                        int e;
                        if (!_ebeneJeAnlage.TryGetValue(idLader, out e)) continue;
                        if (e + 1 > soll) soll = e + 1;
                    }

                    if (soll > _ebeneJeAnlage[idAnlage])
                    {
                        _ebeneJeAnlage[idAnlage] = soll;
                        geaendert = true;
                    }
                }

                if (!geaendert)
                {
                    foreach (KeyValuePair<int, int> e in _ebeneJeAnlage)
                        if (e.Value > _maxEbene) _maxEbene = e.Value;
                    return true;
                }
            }

            // Nach so vielen Runden, wie es Anlagen gibt, wächst nur noch ein Ring.
            Fehlertext = ZyklusMeldung(laderJeSpeicher);
            SimulationProtokoll.Aktuell.Fehlermeldung(Fehlertext);
            return false;
        }

        /// <summary>Sprechende Meldung des Zyklus-Guards mit den beteiligten Anlagen.</summary>
        private string ZyklusMeldung(Dictionary<SimulationPufferspeicher, List<int>> laderJeSpeicher)
        {
            // Die Anlagen mit der höchsten erreichten Ebene stecken im Ring - sie sind es,
            // die den Anwender interessieren.
            int hoechste = 0;
            foreach (KeyValuePair<int, int> e in _ebeneJeAnlage)
                if (e.Value > hoechste) hoechste = e.Value;

            List<string> beteiligt = new List<string>();
            foreach (KeyValuePair<int, SimulationPufferspeicher> bezug in Kontext.QuellpufferJeAnlage)
            {
                int ebene;
                if (!_ebeneJeAnlage.TryGetValue(bezug.Key, out ebene) || ebene < hoechste) continue;
                if (bezug.Value == null || !laderJeSpeicher.ContainsKey(bezug.Value)) continue;

                beteiligt.Add("Anlage " + bezug.Key + " (Quelle: Puffer " +
                              bezug.Value.ID_Pufferspeicher + " „" +
                              bezug.Value.BezeichnerAnzeige() + "\")");
            }

            return "Kaskade: Die Quellbezüge der Pufferspeicher bilden einen RING — " +
                   "eine Anlage lädt einen Speicher, aus dem sie über weitere Erzeuger " +
                   "wieder ihre eigene Quellwärme bezieht. Damit gibt es keine " +
                   "Rechenreihenfolge, in der jeder Erzeuger nach seinem Puffer rechnet; " +
                   "der Lauf bricht ab. Beteiligt: " +
                   (beteiligt.Count > 0 ? string.Join(", ", beteiligt.ToArray()) : "—") +
                   ". Bitte die Wärmequelle einer dieser Anlagen ändern.";
        }

        private void SchreibeAuftragsebenen(List<Ladeauftrag> ordnung)
        {
            if (ordnung == null) return;

            foreach (Ladeauftrag a in ordnung)
            {
                if (a == null) continue;

                int ebene;
                a.Ebene = _ebeneJeAnlage.TryGetValue(a.AnlagenID, out ebene) ? ebene : 0;
            }
        }

        /// <summary>Alle Anlagen-IDs, die in dieser Schleife als Modul rechnen.</summary>
        private List<int> AlleAnlagen()
        {
            List<int> ids = new List<int>();
            if (MitWP) ids.AddRange(WP.wp_list);
            if (MitSolar) ids.AddRange(Solar.solar_anlagen_ids);
            if (MitKessel) ids.AddRange(Kessel.spk_anlagen_ids);
            if (MitBHKW) ids.AddRange(BHKW.bhkw_anlagen_ids);
            return ids;
        }

        /// <summary>
        /// Bedarfsreihenfolge je Ebene: dieselbe Kaskadenreihenfolge, aber nur die Arten,
        /// die auf dieser Ebene überhaupt ein Modul haben.
        /// </summary>
        private void BedarfsordnungJeEbeneBilden()
        {
            _bedarfJeEbene = new List<int>[_maxEbene + 1];

            for (int ebene = 0; ebene <= _maxEbene; ebene++)
            {
                List<int> arten = new List<int>();
                foreach (int art in Bedarfsreihenfolge)
                    if (ArtHatModulAufEbene(art, ebene)) arten.Add(art);
                _bedarfJeEbene[ebene] = arten;
            }
        }

        private bool ArtHatModulAufEbene(int art, int ebene)
        {
            List<int> anlagen = AnlagenDerArt(art);
            if (anlagen == null) return false;

            foreach (int id in anlagen)
            {
                int e;
                if (!_ebeneJeAnlage.TryGetValue(id, out e)) e = 0;
                if (e == ebene) return true;
            }
            return false;
        }

        private List<int> AnlagenDerArt(int art)
        {
            if (art == ProjektPuffer.TYP_WP && MitWP) return WP.wp_list;
            if (art == ProjektPuffer.TYP_SOLARTHERMIE && MitSolar) return Solar.solar_anlagen_ids;
            if (art == ProjektPuffer.TYP_KESSEL && MitKessel) return Kessel.spk_anlagen_ids;
            if (art == ProjektPuffer.TYP_BHKW && MitBHKW) return BHKW.bhkw_anlagen_ids;
            return null;
        }

        private List<int> BedarfsreihenfolgeDerEbene(int ebene)
        {
            if (_bedarfJeEbene == null || ebene < 0 || ebene >= _bedarfJeEbene.Length)
                return Bedarfsreihenfolge;
            return _bedarfJeEbene[ebene];
        }

        /// <summary>
        /// Schreibt die Ebene je Modul in die Module, die Anlagen mit Quellbezug tragen
        /// können (Wärmepumpe und Heizkessel — Konzept Anforderung 6). Solarthermie und
        /// BHKW kennen keine Wärmequelle und bleiben auf Ebene 0.
        /// </summary>
        private void ModulmaskenSchreiben()
        {
            if (MitWP) WP.ModulEbenen = EbenenVektor(WP.wp_list);
            if (MitKessel) Kessel.ModulEbenen = EbenenVektor(Kessel.spk_anlagen_ids);
        }

        private int[] EbenenVektor(List<int> anlagen)
        {
            int[] v = new int[anlagen.Count];
            for (int i = 0; i < anlagen.Count; i++)
            {
                int e;
                v[i] = _ebeneJeAnlage.TryGetValue(anlagen[i], out e) ? e : 0;
            }
            return v;
        }

        private void ModulEbeneSetzen(int ebene)
        {
            if (MitWP) WP.AktiveEbene = ebene;
            if (MitKessel) Kessel.AktiveEbene = ebene;
        }

        /// <summary>
        /// Phasen A und E der Reihenfolge-Invariante: Die Speicher decken den Bedarf in
        /// IHREM Kanal, sortiert nach Entladepriorität (Konzept 3.6).
        ///
        /// Unverändert aus <c>SimulationWaermepumpe</c> übernommen (Paket 4, Etappe 4b);
        /// die Entladung gehört zum Speicher und nicht zu einem Erzeuger — mit
        /// Solarthermie und Kessel als weiteren Ladern wäre sie im WP-Modul am falschen
        /// Ort.
        /// </summary>
        /// <param name="vorab">
        /// true = Phase A. Dann entscheidet die Hysterese des Speichers, ob er entlädt;
        /// ein Speicher im Nachladebetrieb bleibt zu. false = Phase E: Dort greift der
        /// Speicher unabhängig von der Hysterese auf den noch offenen Rest zu — genau wie
        /// die heutige Entladung vor Heizstab und Folge-Erzeuger.
        /// </param>
        private void Entladephase(int stunde, bool vorab, double[] rest)
        {
            if (!vorab)
            {
                // DURCHSATZ ZUERST (Nutzerentscheidung zu 4b-1): Was Phase C über die
                // Ladefähigkeit hinaus aufgenommen hat, war nie ein Speicherinhalt,
                // sondern der Durchfluss der hydraulischen Weiche. Er wird vor der
                // regulären Entladereihenfolge zurückgegeben, damit er zuverlässig
                // beim Verbraucher landet und nicht bei einem anderen Speicher desselben
                // Kanals hängen bleibt, der in der Entladeordnung vor ihm steht. Bei nur
                // einem Speicher je Kanal — dem heute geprüften Fall — ändert die
                // Vorziehung nichts: dieselbe Menge, derselbe Speicher.
                DurchsatzPhase(stunde, rest);
            }
            else
            {
                // Die Hysterese-Entscheidung dieser Stunde beginnt neu (siehe
                // _hysteresePhaseA).
                _hysteresePhaseA.Clear();
            }

            // KANALREIHENFOLGE = KNAPPHEITSREIHENFOLGE (Konzept 4.3, Paket K2).
            //
            // Bis K1 stand hier eine Fallunterscheidung: ohne Kombispeicher „Heizung
            // zuerst" (die bisherige Reihenfolge, bewusst festgehalten), mit
            // Kombispeicher „Warmwasser zuerst" (Entwurfsentscheidung K-1, die
            // App-Konvention „Beides (Warmwasser zuerst)"). Beide Fälle gehen in der
            // Knappheitsreihenfolge auf: Sie IST die Aussage „reicht der Vorrat nicht für
            // alle Kanäle, bekommt dieser zuerst" — nur eben für drei Kanäle, projektweit
            // einstellbar und an allen drei Stellen dieselbe (Abzug, Entladung,
            // Durchsatzabbuchung). Die kanalweise Entladereihenfolge (3.6) bleibt
            // innerhalb jedes Kanaldurchlaufs unangetastet.
            //
            // DOKUMENTIERTE FOLGE FÜR BESTANDSPROJEKTE OHNE KOMBISPEICHER: Die beiden
            // Kanaldurchläufe tauschen die Reihenfolge (jetzt Brauchwasser zuerst). Auf
            // disjunkten Speicherlisten ändert das die vergebene Wärme nicht — wohl aber
            // die Additionsreihenfolge der double-Akkumulatoren der Herkunftsrechnung.
            // Die Byte-Zusage aus Etappe D5a ist damit auf einen Toleranzvergleich
            // zurückgenommen (Konzept 11.2, Rundungsklasse).
            for (int i = 0; i < _knappheit.Length; i++)
            {
                int kanal = _knappheit[i];
                EntladeKanal(Kontext.Entladeordnung(kanal), kanal, vorab, stunde, rest);
            }
        }

        /// <summary>
        /// Rückgabe des DURCHSATZES aller Kanäle — als eigene Methode, weil sie seit
        /// Etappe D5a an zwei Stellen steht: vor der Nachentladung (Phase E) und zwischen
        /// zwei Rechenebenen der Kaskade. Kanalreihenfolge wie in
        /// <see cref="Entladephase"/> (Knappheitsreihenfolge, 4.3).
        /// </summary>
        private void DurchsatzPhase(int stunde, double[] rest)
        {
            for (int i = 0; i < _knappheit.Length; i++)
            {
                int kanal = _knappheit[i];
                DurchsatzEntladen(Kontext.Entladeordnung(kanal), kanal, stunde, rest);
            }
        }

        /// <summary>
        /// Gibt den Teil des Füllstands zurück, der über <see cref="SimulationPufferspeicher.Q_max"/>
        /// hinausgeht — der Durchfluss dieser Stunde (siehe <see cref="Entladephase"/>).
        /// Ohne Durchlass in Phase C gibt es diesen Anteil nicht, und die Methode tut nichts.
        /// </summary>
        private void DurchsatzEntladen(List<SimulationPufferspeicher> speicher, int kanal,
                                       int stunde, double[] rest)
        {
            if (speicher == null) return;

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null || sp.Q_max <= 0) continue;

                double ueber = sp.SOC - sp.Q_max;
                if (ueber <= 0) continue;

                double bedarf = rest[kanal];
                if (bedarf <= 0) continue;

                double gedeckt = sp.Entladen(Math.Min(ueber, bedarf), stunde);
                if (gedeckt <= 0) continue;

                // KANAL DES DURCHLAUFS entscheidet — die Menge stammt aus der
                // Entladeordnung genau dieses Kanals, und nur seinen Bedarf darf sie
                // decken (Konzept 6.3).
                SenkeAbziehen(MASKE_EINZELKANAL[kanal], gedeckt, rest, _knappheit);

                Anteil_Entladen(sp, gedeckt, kanal);   // N2: Eigenanteil der Lader

#if DEBUG
                if (Entladeprobe != null)
                    Entladeprobe(stunde, false, kanal, sp.ID_Pufferspeicher, gedeckt, sp.SOC);
#endif
            }
        }

        private void EntladeKanal(List<SimulationPufferspeicher> speicher, int kanal,
                                  bool vorab, int stunde, double[] rest)
        {
            if (speicher == null) return;

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null || sp.Q_max <= 0) continue;

                // Die Hysterese wird in Phase A für JEDEN Speicher fortgeschrieben, auch
                // wenn sein Kanal gerade keinen Bedarf hat — sonst bliebe ein Speicher
                // ohne Bedarf für immer im zuletzt gesetzten Zustand.
                //
                // Ein Speicher mit mehrelementigem Klassen-Set wird in dieser Phase
                // mehrfach besucht (er steht in mehreren Kanallisten). Fortgeschrieben
                // wird trotzdem nur einmal je Stunde — HystereseFortschreiben ist ein
                // Zustandsübergang, kein Test.
                bool darfEntladen = vorab ? HystereseDerStunde(sp) : true;
                if (!darfEntladen) continue;

                double bedarf = rest[kanal];
                if (bedarf <= 0) continue;

                // PAKET BHKW-REGULÄR: MINDESTFÜLLSTAND/NOTRESERVE. Diese eine Zeile ist
                // die gesamte Wirkung des Puffer-Parameters Schwelle_Reserve - sie klemmt
                // den ANGEFORDERTEN Bedarf, nicht die Speicherphysik.
                //
                // WARUM HIER. EntladeKanal ist die einzige Stelle, an der ein Speicher
                // bedarfsdeckend aus seinem VORRAT entlädt (Phase A Vorabentladung und
                // Phase E Nachentladung laufen beide durch sie). Die Durchleitung derselben
                // Stunde geht über DurchsatzEntladen und bleibt bewusst unberührt: Dort
                // wird nur der Überhang über Q_max entnommen, und der liegt konstruktiv
                // oberhalb der Reservemarke.
                //
                // WARUM NICHT IN SimulationPufferspeicher.Entladen. Das ist die
                // Speicherphysik ALLER Erzeuger und Phasen; eine Untergrenze dort wäre die
                // globale Verhaltensänderung, die die Entscheidung des Anwenders
                // ausdrücklich ausschließt (andere Erzeuger entladen unverändert bis 0).
                //
                // VERHALTENSNEUTRAL OHNE BHKW: EntnahmeObergrenze liefert double.MaxValue,
                // solange der Speicher nicht im Bilanzraum eines BHKW steht oder keine
                // Reserve gepflegt ist. Math.Min mit MaxValue gibt den Bedarf unverändert
                // zurück - kein Projekt ohne BHKW ändert sein Ergebnis.
                double entnehmbar = sp.EntnahmeObergrenze();
                if (bedarf > entnehmbar) bedarf = entnehmbar;

                // Reservemarke erreicht: nichts mehr entnehmen. Der Speicher geht in den
                // NACHLADEBETRIEB - der Bedarf bleibt offen und wird von der nächsten
                // Kaskadenstufe bzw. vom Heizstab gedeckt, während das BHKW seinen Vorrat
                // wieder aufbaut. Das ist dieselbe Markierung, die am Ende dieser Schleife
                // ein nicht ausreichender Speicher bekommt.
                //
                // Dieser Zweig ist NUR bei aktiver Reserve erreichbar: Ohne sie liefert
                // EntnahmeObergrenze double.MaxValue, und bedarf war oben schon > 0.
                if (bedarf <= 0)
                {
                    if (vorab) sp.LaedtGerade = true;
                    continue;
                }

                double gedeckt = sp.Entladen(bedarf, stunde);
                if (gedeckt <= 0) continue;

                // KANAL DES DURCHLAUFS entscheidet, nicht die Bedarfsart (Konzept 6.3):
                // Ein Brauchwasserspeicher darf keinen Heizbedarf decken. Ein Speicher
                // mit mehrelementigem Set kommt für jeden seiner Kanäle EINMAL hierher —
                // die Aufteilung entsteht aus der Knappheitsreihenfolge der Durchläufe,
                // nicht aus einer Maske.
                SenkeAbziehen(MASKE_EINZELKANAL[kanal], gedeckt, rest, _knappheit);

                Anteil_Entladen(sp, gedeckt, kanal);   // N2: Eigenanteil der Lader

#if DEBUG
                if (Entladeprobe != null)
                    Entladeprobe(stunde, vorab, kanal, sp.ID_Pufferspeicher, gedeckt, sp.SOC);
#endif

                // Reicht der Speicher nicht, muss wieder nachgeladen werden.
                //
                // GEMESSEN WIRD DER KANAL DIESES DURCHLAUFS. Bis K1 war das bei einem
                // Heizungspuffer der zusammengefasste Rest aus Heizung UND Prozess; jetzt
                // sind es zwei Durchläufe mit je eigenem Rest. Die Markierung fällt
                // dadurch in genau einem Grenzfall anders aus: wenn beide Kanalreste für
                // sich unter 0,0001 kWh liegen, zusammen aber darüber. Das ist ein
                // Zehntelwattstundenbereich; ihn zu behalten hieße, den Speicher an einer
                // Summe zu messen, die er im Dreikanalmodell gar nicht mehr sieht.
                if (vorab && rest[kanal] > 0.0001) sp.LaedtGerade = true;
            }
        }

#if DEBUG

        /// <summary>
        /// PRÜFHAKEN der Entladung — ausschließlich im Debug-Build, nach dem Muster von
        /// <see cref="Waermekanaele.Selbsttest"/> (kein Prüfcode im Release-Assembly).
        ///
        /// Die Knappheitsregel (Konzept 4.3; bis K1 die Kombi-Sonderregel K-1 „reicht der
        /// Inhalt nicht für beide Bedarfe, gilt Warmwasser zuerst") ist eine Aussage über
        /// die REIHENFOLGE innerhalb einer Stunde. Aus den Jahres- und Stundenganglinien
        /// der Ergebnispersistenz lässt sie sich nicht ablesen: Dort steht die Entladung
        /// als EINE Zahl je Speicher und Stunde, ohne Kanal. Der Haken macht sie messbar,
        /// ohne dem Rechenkern eine Ausgabe zu geben.
        ///
        /// Parameter: Stunde, Phase A (<c>true</c>) oder E, KANALINDEX
        /// (<see cref="Kanal"/>; bis Paket K2 ein <c>bool</c> „Warmwasserkanal"),
        /// Puffer-ID, gedeckte Menge [kWh], Füllstand danach [kWh].
        /// </summary>
        public static Action<int, bool, int, int, double, double> Entladeprobe;

#endif

        /// <summary>
        /// Hysterese-Entscheidung der Phase A, je Speicher und Stunde GENAU EINMAL
        /// gebildet (Etappe D5a — siehe <see cref="_hysteresePhaseA"/>).
        ///
        /// Wird ein Speicher nur einmal besucht, liefert die Merkung genau das, was der
        /// direkte Aufruf liefern würde — der Unterschied ist ein Wörterbuchzugriff.
        /// </summary>
        private bool HystereseDerStunde(SimulationPufferspeicher sp)
        {
            bool entscheidung;
            if (_hysteresePhaseA.TryGetValue(sp, out entscheidung)) return entscheidung;

            entscheidung = sp.HystereseFortschreiben();
            _hysteresePhaseA[sp] = entscheidung;
            return entscheidung;
        }

        /// <summary>
        /// Zieht eine Wärmemenge von den Bedarfskanälen einer KANALMASKE ab, in der
        /// vorgegebenen Reihenfolge (Konzept 4.3) — die eine Abzugsregel des
        /// Rechenkerns.
        ///
        /// EINE Implementierung für alle Stufen (Paket 5): Wärmepumpe, Solarthermie,
        /// Heizkessel, BHKW, Heizstab und Speicherentladung müssen dieselbe Kanalregel
        /// benutzen, sonst laufen die Kanäle auseinander.
        ///
        /// <para><b>Regel.</b> Die Menge wird der Reihe nach auf die maskierten Kanäle
        /// verteilt; jeder Kanal nimmt höchstens seinen offenen Bedarf auf. Was danach
        /// übrig ist, VERFÄLLT — die Deckung kann den Bedarf nicht überschreiten. Genau
        /// das tat die zweikanalige Fassung mit ihrer Klemmung auf 0, nur ohne es
        /// zurückzumelden.</para>
        ///
        /// <para><b>Warum ein Rückgabewert.</b> Ein Aufrufer, der 10 kWh anbietet und
        /// 6 kWh los wird, muss das erfahren können — sonst schreibt er sich eine Deckung
        /// gut, die nie stattgefunden hat. Bis Paket K2 stand diese Prüfung, wo sie stand,
        /// oder gar nicht; jetzt liefert die Regel sie mit. Wer sie nicht braucht,
        /// ignoriert den Wert (der Rechenkern tut das an vielen Stellen bewusst,
        /// Konvention B0).</para>
        ///
        /// <para><b>Nichtnegativität.</b> Am Ende wird JEDER Kanal auf ≥ 0 geklemmt, nicht
        /// nur die maskierten. Das ist keine Bequemlichkeit, sondern die Zusage der
        /// zweikanaligen Fassung, die beide Kanäle bei jedem Aufruf geklemmt hat: Der
        /// Rest, mit dem die nächste Stufe rechnet, ist nie negativ.</para>
        /// </summary>
        /// <param name="maske">
        /// Kanäle, aus denen abgezogen werden darf (Länge <see cref="Kanal.ANZAHL"/>);
        /// <c>null</c> = alle. Die Masken der Direktsenken liefert
        /// <see cref="DirektsenkeMaske"/>; sie sind GETEILTE, unveränderliche Instanzen
        /// und werden hier nur gelesen.
        /// </param>
        /// <param name="menge">Angebotene Wärmemenge [kWh]; ≤ 0 ist ein No-op.</param>
        /// <param name="rest">Stundenzustand der Kanäle [kWh], wird IN PLACE verändert.</param>
        /// <param name="reihenfolge">
        /// Kanalindizes in Abzugsreihenfolge; <c>null</c> = Knappheitsreihenfolge des
        /// Laufs.
        /// </param>
        /// <returns>Tatsächlich abgezogene Summe [kWh].</returns>
        public static double SenkeAbziehen(bool[] maske, double menge, double[] rest,
                                           int[] reihenfolge)
        {
            if (rest == null) return 0;

            double gezogen = 0;

            if (menge > 0)
            {
                int[] ordnung = (reihenfolge != null && reihenfolge.Length > 0)
                    ? reihenfolge : KnappheitDesLaufs();
                double offen = menge;

                for (int i = 0; i < ordnung.Length; i++)
                {
                    int k = ordnung[i];
                    if (k < 0 || k >= rest.Length) continue;
                    if (maske != null && (k >= maske.Length || !maske[k])) continue;
                    if (rest[k] <= 0) continue;

                    double teil = Math.Min(offen, rest[k]);
                    rest[k] -= teil;
                    gezogen += teil;

                    offen -= teil;
                    if (offen <= 0) break;
                }
            }

            for (int k = 0; k < rest.Length; k++)
                if (rest[k] < 0) rest[k] = 0;

            return gezogen;
        }

        /// <summary>
        /// KOMPATIBILITÄTSFASSUNG für die Erzeugermodule: Abzug nach der BEDARFSART einer
        /// Direktsenke Heizkreis statt nach einer Kanalmaske.
        ///
        /// Sie löst zwei Dinge selbst auf, die der Aufrufer nicht kennen soll: die
        /// Kanalmaske der Bedarfsart (<see cref="DirektsenkeMaske"/>) und die
        /// Knappheitsreihenfolge des Laufs (<see cref="KnappheitFuerLauf"/>). Sie ist der
        /// Ersatz für alle bisherigen Aufrufe der Form
        /// <c>SenkeAbziehen(wsTyp, menge, ref rest_ww, ref rest_heiz)</c>.
        ///
        /// <b>Seit Paket S1 ist sie der ZWEITE Weg.</b> Wer eine ganze Senkenliste hat,
        /// nimmt <see cref="SenkeAbziehen(Senkenliste, double, double[], double[])"/> —
        /// nur die kennt die Direktsenken-Kette und damit die Prozesswärme-Zeilen. Diese
        /// Fassung bleibt für Aufrufer, die genau EINE Heizkreis-Senke bedienen (die
        /// Heizstabphase, der BHKW-Übergang).
        /// </summary>
        /// <returns>Tatsächlich abgezogene Summe [kWh].</returns>
        public static double SenkeAbziehen(string bedarfsart, double menge, double[] rest)
        {
            return SenkeAbziehen(DirektsenkeMaske(bedarfsart), menge, rest, KnappheitDesLaufs());
        }

        /// <summary>
        /// DIREKTSENKEN-KETTE einer Anlage (Paket S1, Konzept 5.2) — die
        /// Verteilungsregel „eine kWh, genau ein Ziel" für Phase B.
        ///
        /// <para><b>Regel.</b> Die angebotene Menge läuft SEQUENZIELL über die
        /// DIREKTsenken der Liste, in Rangfolge. Jede Zeile nimmt auf, was ihre Kanäle
        /// (<see cref="SenkenMaske"/>) noch offen haben; was danach bleibt, geht an die
        /// nächste Zeile. Puffersenken werden übersprungen — sie decken keinen Bedarf,
        /// sie laden (Ladephase ihres Rangs).</para>
        ///
        /// <para><b>Keine Doppelzählung.</b> Der Abzug läuft auf DEMSELBEN
        /// <paramref name="rest"/>-Feld: Was Zeile r abgezogen hat, findet Zeile r+1 nicht
        /// mehr vor. Überlappende Masken (etwa <c>Heizung</c> auf Rang 1 und
        /// <c>Beides</c> auf Rang 2) sind damit unschädlich.</para>
        ///
        /// <para><b>Bestandsbild.</b> Eine Anlage mit genau einer Direktsenke
        /// <c>Heizkreis/Beides</c> — jedes migrierte Bestandsprojekt — durchläuft die
        /// Schleife genau einmal und ruft genau einmal
        /// <see cref="SenkeAbziehen(bool[], double, double[], int[])"/>: Anweisung für
        /// Anweisung die bisherige Rechnung.</para>
        /// </summary>
        /// <param name="jeKanal">
        /// Aufschlüsselung der tatsächlich abgezogenen Beträge je Kanal (<c>+=</c>;
        /// <c>null</c> = nicht gewünscht) — GEMESSEN an der Differenz von
        /// <paramref name="rest"/>, nicht zweitgerechnet (Konzept 4.4).
        /// </param>
        /// <returns>Tatsächlich abgezogene Summe [kWh].</returns>
        public static double SenkeAbziehen(Senkenliste liste, double menge, double[] rest,
                                           double[] jeKanal)
        {
            return Kanalabzug.Abziehen(liste, menge, rest, jeKanal);
        }

        // PAKET A1: Hier stand die ZWEIKANALIGE ref-Fassung
        // „SenkeAbziehen(string senke, double menge, ref double rest_ww,
        // ref double rest_heiz)" — der Abzug auf den beiden Skalaren rest_ww/rest_heiz
        // der einkanaligen WP-Stundenschleife. Ihr einziger Aufrufer war
        // SimulationWaermepumpe.SenkeAbziehen(string, double, ref double, ref double),
        // und der wurde ausschließlich aus Berechnung_Stundenschleife gerufen. Mit dem
        // Altpfad sind beide entfallen; der Abzug läuft ausnahmslos über das indizierte
        // Restbedarfsfeld (SenkeAbziehen(bool[], double, double[], int[]) und die
        // Fassungen darüber).
    }

    /// <summary>
    /// Kanalabzug auf dem indizierten Restbedarfsfeld (Paket K2, Konzept 4.1/4.3) — die
    /// Brücke, über die ALLE vier Erzeugermodule denselben Abzug benutzen.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Die eine Kanalregel ist und bleibt
    /// <see cref="Kaskadenschleife.SenkeAbziehen(bool[], double, double[], int[])"/>: Sie
    /// löst Kanalmaske und Knappheitsreihenfolge auf. Die Module brauchen darüber hinaus
    /// zwei Größen, die man aus ihr ABLEITEN, aber nicht ohne Wissen über die Maske selbst
    /// bilden kann:</para>
    ///
    /// <list type="number">
    /// <item><see cref="Offen(Senkenliste, double[])"/> — der offene Bedarf GENAU DER
    /// Kanäle, die eine Senke bzw. eine ganze Direktsenken-Kette bedient. Bis Paket K1
    /// stand dafür in jedem Modul dieselbe Dreifach-Verzweigung (<c>Warmwasser →
    /// rest_ww</c>, <c>Heizung → rest_heiz</c>, sonst die Summe). Sie hier ein viertes und
    /// fünftes Mal auszuschreiben hieße, die Kanalzuordnung an fünf Stellen zu führen —
    /// dieselbe Doppelung, die Paket 5 mit <c>SenkeAbziehen</c> beseitigt hat.</item>
    /// <item><see cref="Abziehen(Senkenliste, double, double[], double[])"/> — der Abzug
    /// MIT Aufschlüsselung, welcher Kanal wie viel abgegeben hat (Konzept 4.4,
    /// Voraussetzung für die Deckungsgrade je Kanal).</item>
    /// </list>
    ///
    /// <para>Beide führen KEINE eigene Kanalzuordnung: <c>Offen</c> liest die Maske, die
    /// <see cref="Kaskadenschleife.SenkenMaske"/> für dieselbe Zeile liefert, und
    /// <c>Abziehen</c> MISST die Aufschlüsselung an der Differenz von <c>rest</c> vor und
    /// nach dem Abzug. Damit kann diese Klasse von der einen Kanalregel nicht abweichen.</para>
    ///
    /// <para><b>K2-O1 — SIE STEHT JETZT AM RICHTIGEN ORT.</b> Bis Paket S1 lag sie
    /// whitelist-bedingt am Ende von <c>SimulationWaermepumpe.cs</c>; ihr Platz ist neben
    /// der Regel, die sie befragt. Mit dem Umzug ist auch die Doppelung <c>Summe</c> /
    /// <c>Kaskadenschleife.RestSumme</c> aufgelöst: Es gibt nur noch
    /// <see cref="Kaskadenschleife.RestSumme"/>.</para>
    /// </summary>
    internal static class Kanalabzug
    {
        [ThreadStatic] private static double[] _vorher;

        /// <summary>
        /// Offener Bedarf über ALLE Kanäle [kWh].
        ///
        /// <b>NUR NOCH DURCHREICHUNG</b> (K2-O1): Die Implementierung steht seit Paket S1
        /// ausschließlich in <see cref="Kaskadenschleife.RestSumme"/>. Der Name bleibt für
        /// die Modulaufrufe stehen, die ihn noch tragen; neuer Code ruft
        /// <c>Kaskadenschleife.RestSumme</c> unmittelbar.
        /// </summary>
        public static double Summe(double[] rest)
        {
            return Kaskadenschleife.RestSumme(rest);
        }

        /// <summary>
        /// Offener Bedarf der Kanäle, in die DIESER Speicher entlädt [kWh] — die
        /// Durchsatzgröße eines Puffers als hydraulische Weiche (Bilanzraum, Konzept 3.4).
        /// Bis Paket K1 stand dafür in der Wärmepumpe und im BHKW je eine
        /// Fallunterscheidung <c>IstKombi / IstBrauchwasserkanal</c>.
        ///
        /// <para>Maßgeblich ist das KLASSEN-SET des Speichers
        /// (<see cref="SimulationPufferspeicher.BedientKanal(int)"/>) — dieselbe Frage
        /// beantwortet <see cref="Kaskadenschleife.DurchlassBudget"/> beim Vergeben des
        /// Durchsatzbudgets, und beide Seiten müssen dieselbe Antwort bekommen, sonst
        /// schätzt die Bedarfsphase einen Durchsatz, den die Ladephase nicht vergibt.
        /// (Bis Paket K2 lief die Frage über <c>Kaskadenschleife.EntladetKanal</c>, also
        /// samt der Interimsregel I2; die ist mit S1 abgerissen.)</para>
        ///
        /// <para>NACHARBEIT E-K2-3, die Vorgeschichte dieser Regel: Der KOMBISPEICHER
        /// bedient beide Kanäle aus einem Vorrat, sein Durchsatz ist deshalb die Summe
        /// beider. Solange die Bedarfsphase mit „nur Heizbedarf" rechnete und die
        /// Ladephase mit „Heiz + Warmwasser", war in einer Sommerstunde
        /// (kein Heizbedarf, offener Warmwasserbedarf, Kombispeicher auf Abschaltschwelle)
        /// der Bilanzraum 0: Das Modul wurde übersprungen und der Heizstab sprang ein,
        /// obwohl der Kombispeicher den Bedarf hätte durchreichen können.</para>
        /// </summary>
        public static double OffenFuerSpeicher(SimulationPufferspeicher sp, double[] rest)
        {
            if (sp == null || rest == null) return 0;

            double offen = 0;
            for (int k = 0; k < Kanal.ANZAHL && k < rest.Length; k++)
                if (rest[k] > 0 && sp.BedientKanal(k)) offen += rest[k];

            return offen;
        }

        /// <summary>
        /// Offener Bedarf der Kanäle, die die Bedarfsart <paramref name="bedarfsart"/>
        /// einer Heizkreis-Direktsenke bedient [kWh].
        ///
        /// Gezählt werden GENAU die Kanäle der Maske und nur ihre POSITIVEN Restbeträge —
        /// dieselben Kanäle und dieselbe Bedingung, unter denen
        /// <see cref="Kaskadenschleife.SenkeAbziehen(string, double, double[])"/> gleich
        /// abziehen wird. Damit ist der Rückgabewert exakt die Menge, die dieser Abzug
        /// höchstens unterbringen kann.
        /// </summary>
        public static double Offen(string bedarfsart, double[] rest)
        {
            return OffenMaske(Kaskadenschleife.DirektsenkeMaske(bedarfsart), rest);
        }

        /// <summary>
        /// Offener Bedarf der DIREKTSENKEN-KETTE einer Anlage [kWh] (Paket S1,
        /// Konzept 5.2) — die Bezugsgröße, gegen die ein Erzeuger seine Stundenproduktion
        /// in Phase B begrenzt.
        ///
        /// Gezählt wird die VEREINIGUNG der Masken aller Direktsenken-Zeilen, jeder Kanal
        /// GENAU EINMAL: Zwei Zeilen, die denselben Kanal bedienen, verdoppeln seinen
        /// offenen Bedarf nicht — die zweite fände ihn nach dem Abzug der ersten leer vor.
        /// Genau das leistet <see cref="Abziehen(Senkenliste, double, double[], double[])"/>,
        /// und beide Größen müssen zueinander passen.
        ///
        /// Ohne Liste gilt die Vorbelegung <c>Heizkreis/Beides</c>; eine Liste ganz ohne
        /// Direktsenke liefert 0 (die Anlage lädt ausschließlich).
        /// </summary>
        public static double Offen(Senkenliste liste, double[] rest)
        {
            if (rest == null) return 0;
            if (liste == null) return Offen(WaermequelleClass.SENKE_BEIDES, rest);

            double offen = 0;
            for (int k = 0; k < Kanal.ANZAHL && k < rest.Length; k++)
            {
                if (rest[k] <= 0) continue;
                if (!KanalGedeckt(liste, k)) continue;
                offen += rest[k];
            }
            return offen;
        }

        /// <summary>true, wenn irgendeine DIREKTsenke der Liste diesen Kanal bedient.</summary>
        private static bool KanalGedeckt(Senkenliste liste, int kanal)
        {
            for (int i = 0; i < liste.Zeilen.Count; i++)
            {
                Senkenzeile z = liste.Zeilen[i];
                if (z == null || z.IstPuffersenke) continue;

                bool[] maske = Kaskadenschleife.SenkenMaske(z);
                if (maske != null && kanal < maske.Length && maske[kanal]) return true;
            }
            return false;
        }

        private static double OffenMaske(bool[] maske, double[] rest)
        {
            if (rest == null) return 0;

            double offen = 0;
            for (int k = 0; k < Kanal.ANZAHL && k < rest.Length; k++)
            {
                if (maske != null && (k >= maske.Length || !maske[k])) continue;
                if (rest[k] > 0) offen += rest[k];
            }
            return offen;
        }

        /// <summary>
        /// Zieht <paramref name="menge"/> nach der einen Kanalregel von
        /// <paramref name="rest"/> ab und schreibt die tatsächlich abgezogenen Beträge je
        /// Kanal auf <paramref name="jeKanal"/> auf (<c>+=</c>; <c>null</c> = keine
        /// Aufschlüsselung gewünscht).
        /// </summary>
        /// <returns>tatsächlich abgezogene Gesamtmenge [kWh]</returns>
        public static double Abziehen(string bedarfsart, double menge, double[] rest,
                                      double[] jeKanal)
        {
            if (rest == null || menge <= 0) return 0;

            double[] vorher = Zwischenablage(rest);
            Kaskadenschleife.SenkeAbziehen(bedarfsart, menge, rest);
            return Aufschluesseln(vorher, rest, jeKanal);
        }

        /// <summary>
        /// Dasselbe für die DIREKTSENKEN-KETTE einer Anlage (Paket S1, Konzept 5.2):
        /// Die Menge läuft in RANGFOLGE über die Direktsenken; jede Zeile nimmt auf, was
        /// ihre Kanäle noch offen haben, der Rest geht weiter. Puffersenken werden
        /// übersprungen.
        ///
        /// Mit genau einer Direktsenke — jedes migrierte Bestandsprojekt — ist das ein
        /// einziger Durchlauf und damit derselbe Aufruf wie zuvor.
        /// </summary>
        /// <returns>tatsächlich abgezogene Gesamtmenge [kWh]</returns>
        public static double Abziehen(Senkenliste liste, double menge, double[] rest,
                                      double[] jeKanal)
        {
            if (rest == null || menge <= 0) return 0;
            if (liste == null) return Abziehen(WaermequelleClass.SENKE_BEIDES, menge, rest, jeKanal);

            double[] vorher = Zwischenablage(rest);
            double offen = menge;

            for (int i = 0; i < liste.Zeilen.Count && offen > 0; i++)
            {
                Senkenzeile z = liste.Zeilen[i];
                if (z == null || z.IstPuffersenke) continue;

                offen -= Kaskadenschleife.SenkeAbziehen(Kaskadenschleife.SenkenMaske(z),
                                                        offen, rest, null);
            }

            return Aufschluesseln(vorher, rest, jeKanal);
        }

        /// <summary>
        /// Der Stand von <paramref name="rest"/> VOR dem Abzug — in einem
        /// wiederverwendeten Feld je Thread, damit ein Jahreslauf nicht 8760·n
        /// Kurzlebige anlegt.
        /// </summary>
        private static double[] Zwischenablage(double[] rest)
        {
            double[] vorher = _vorher;
            if (vorher == null) { vorher = new double[Kanal.ANZAHL]; _vorher = vorher; }

            Array.Copy(rest, vorher, Kanal.ANZAHL);
            return vorher;
        }

        /// <summary>Aufschlüsselung je Kanal aus der Differenz vorher/nachher (Konzept 4.4).</summary>
        private static double Aufschluesseln(double[] vorher, double[] rest, double[] jeKanal)
        {
            double summe = 0;
            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                double abgezogen = vorher[k] - rest[k];
                if (abgezogen == 0) continue;

                summe += abgezogen;
                if (jeKanal != null) jeKanal[k] += abgezogen;
            }
            return summe;
        }
    }
}
