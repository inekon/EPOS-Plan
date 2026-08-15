using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Stundenschleife der zweikanaligen Kaskade — die Reihenfolge-Invariante aus
    /// Konzept 6.3 für ALLE speicherfähigen Erzeuger.
    ///
    /// <code>
    /// je Stunde h:
    ///   A) Vorabentladung   — Speicher decken Bedarf in IHREM Kanal (Hysterese),
    ///                         Reihenfolge nach Entladepriorität (3.6)
    ///   B) Bedarfsdeckung   — Erzeugerstufen in KASKADENREIHENFOLGE, je Stufe nur
    ///                         Anlagen mit Hauptsenke HEIZKREIS, SenkeAbziehen(WS_Typ)
    ///   C) Speicherladung   — Anlagen mit Hauptsenke PUFFER_*, KASKADENÜBERGREIFEND
    ///                         nach Ladepriorität der Stunde (3.4/3.5), KEIN SenkeAbziehen
    ///   D) Zweitsenken      — aus dem verbleibenden Ladepotenzial, gleiche Ordnung
    ///   E) Nachentladung    — Speicher decken den noch offenen Bedarf
    ///   F) Heizstab         — auf den dann verbleibenden Kanalrest
    ///   G) StundeAbschliessen je Registry-Speicher — GENAU EINMAL
    /// </code>
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

        /// <summary>true, wenn ein KOMBISPEICHER mitrechnet (Etappe D5a, K-1).</summary>
        private bool _hatKombi;

        /// <summary>
        /// Hysterese-Entscheidung der laufenden Phase A je Speicher (Etappe D5a).
        ///
        /// Ein Kombispeicher steht in BEIDEN Entladereihenfolgen und wird in Phase A
        /// deshalb zweimal besucht. <see cref="SimulationPufferspeicher.HystereseFortschreiben"/>
        /// ist aber ein ZUSTANDSÜBERGANG: Der zweite Aufruf sähe den bereits abgesenkten
        /// Füllstand und könnte den Speicher mitten in der Stunde in den Nachladebetrieb
        /// kippen. Die Entscheidung fällt deshalb je Speicher und Stunde genau einmal.
        /// </summary>
        private readonly Dictionary<SimulationPufferspeicher, bool> _hysteresePhaseA =
            new Dictionary<SimulationPufferspeicher, bool>();

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

        /// <summary>Bedarfsdeckende Speicherentladung je Erzeugerart [kWh].</summary>
        private readonly double[] _entladungJeArt = new double[ART_ANZAHL];

        /// <summary>
        /// Dieselbe Zurechnung, aber nur für die LAUFENDE Stunde [kWh] (Nacharbeit
        /// Paket 6, Befund N4).
        ///
        /// Der Restwärmebedarf eines Erzeugers ist „Stufeneingang − Direktdeckung −
        /// zugerechnete Entladung". Als Jahressumme steht er in
        /// <c>Tab_ErgebnisBHKW.Restwaermebedarf</c>; damit die GANGLINIE dieselbe Größe
        /// zeigt, braucht sie den Stundenwert der Zurechnung — die Jahressumme allein
        /// lässt sich nicht auf Stunden verteilen.
        /// </summary>
        private readonly double[] _entladungJeArtStunde = new double[ART_ANZAHL];

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
        /// Erzeugerarten aufteilen.
        /// </summary>
        private void Anteil_Entladen(SimulationPufferspeicher sp, double gedeckt)
        {
            if (sp == null || gedeckt <= 0) return;

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
                _entladungJeArtStunde[i] += teil;      // N4: Stundenwert für die Ganglinie
            }
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
        // OHNE Kombispeicher liefern beide Methoden Anweisung für Anweisung das, was
        // vorher in den Modulen stand.
        // ------------------------------------------------------------------

        /// <summary>
        /// Absehbare Entnahme, die dieser Speicher in der laufenden Stunde zusätzlich
        /// durchreichen kann [kWh] (Nutzerentscheidung zu Befund 4b-1).
        /// </summary>
        public static double DurchlassBudget(SimulationPufferspeicher sp, double[] absehbar)
        {
            if (sp == null || absehbar == null) return 0;

            double offen;
            if (sp.IstKombi)
            {
                // D5a: EIN Vorrat für beide Kanäle - also auch ein gemeinsames Budget.
                offen = (absehbar[0] > 0 ? absehbar[0] : 0) + (absehbar[1] > 0 ? absehbar[1] : 0);
            }
            else
            {
                int kanal = sp.IstBrauchwasserkanal ? 1 : 0;
                offen = absehbar[kanal] > 0 ? absehbar[kanal] : 0;
            }

            return Math.Min(offen, sp.Entnahmefaehigkeit());
        }

        /// <summary>
        /// Bucht den tatsächlich genutzten Durchlass vom Budget ab. Beim Kombispeicher
        /// zuerst vom WARMWASSERkanal — dieselbe Vorrangregel, mit der die Entladung
        /// arbeitet (K-1).
        /// </summary>
        public static void DurchlassBuchen(SimulationPufferspeicher sp, double[] absehbar,
                                           double genutzt)
        {
            if (sp == null || absehbar == null || genutzt <= 0) return;

            if (!sp.IstKombi)
            {
                int kanal = sp.IstBrauchwasserkanal ? 1 : 0;
                absehbar[kanal] -= genutzt;
                if (absehbar[kanal] < 0) absehbar[kanal] = 0;
                return;
            }

            double ww = Math.Min(genutzt, absehbar[1] > 0 ? absehbar[1] : 0);
            absehbar[1] -= ww;
            if (absehbar[1] < 0) absehbar[1] = 0;

            absehbar[0] -= (genutzt - ww);
            if (absehbar[0] < 0) absehbar[0] = 0;
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
        public bool Rechnen(Waermekanaele kanaele)
        {
            if (kanaele == null || Kontext == null) return false;

            Fehlertext = "";

            // ETAPPE D5a: Rechenebenen aus den Quellbezügen. Schlägt die Auflösung fehl,
            // liegt ein RING vor (A lädt B, B ist Quelle von A) - dann gibt es keine
            // Reihenfolge, in der beide „nach ihrem Puffer" rechnen. Der Lauf bricht ab,
            // statt eine willkürliche Reihenfolge zu wählen und ein plausibel aussehendes
            // Ergebnis zu speichern.
            if (!EbenenAufloesen()) return false;

            _hatKombi = Kontext.HatKombispeicher();

            List<double> biv = new List<double>();

            // N2: Zurechnung der Speicherentladung auf den Laufanfang.
            _inhaltsanteile.Clear();
            Array.Clear(_entladungJeArt, 0, _entladungJeArt.Length);

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
            // der hydraulischen Weiche (Nutzerentscheidung zu 4b-1). Index 0 = Heizkanal,
            // 1 = Warmwasserkanal. Das Budget wird über die Phasen C und D hinweg NUR
            // EINMAL vergeben: Zwei Speicher desselben Kanals dürfen nicht beide dieselbe
            // Entnahme durchreichen, sonst bliebe nach Phase E Wärme im Speicher stehen,
            // die niemand angefordert hat.
            double[] absehbar = new double[2];

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                double rest_heiz = kanaele.Heiz[stunde];
                double rest_ww = kanaele.WW[stunde];

                // N4: Zurechnung der Entladung auf den Anfang DIESER Stunde.
                Array.Clear(_entladungJeArtStunde, 0, _entladungJeArtStunde.Length);

                // N3: Reservierungen der Vorstunde verfallen. Sie gelten nur innerhalb
                // einer Stunde - zwischen Phase B (Motorzuschaltung) und Phase C/D
                // (Einlagerung). Eine nicht eingelöste Reservierung darf sich nicht in
                // die nächste Stunde schleppen und dort Ladefähigkeit sperren.
                foreach (SimulationPufferspeicher sp in Kontext.AlleSpeicher)
                    if (sp != null) sp.Reserviert = 0;

                // STUFENEINGANG je Erzeugerstufe (N1): der Kanalstand VOR Phase A.
                if (MitWP) WP.Zweikanalig_StundeStart(stunde);
                if (MitSolar) Solar.Stunde_Start(stunde, rest_heiz, rest_ww);
                if (MitKessel) Kessel.Stunde_Start(stunde, rest_heiz, rest_ww);
                if (MitBHKW) BHKW.Stunde_Start(stunde, rest_heiz, rest_ww);

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
                Entladephase(stunde, true, ref rest_heiz, ref rest_ww);

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
                                                             ref rest_heiz, ref rest_ww))
                                return false;
                            QuellentnahmenVerbuchen(WP.Quellentnahmen);
                        }
                        else if (art == ProjektPuffer.TYP_SOLARTHERMIE && MitSolar)
                        {
                            Solar.Stunde_Bedarf(stunde, ref rest_heiz, ref rest_ww);
                        }
                        else if (art == ProjektPuffer.TYP_KESSEL && MitKessel)
                        {
                            Kessel.Stunde_Bedarf(stunde, ref rest_heiz, ref rest_ww);
                            QuellentnahmenVerbuchen(Kessel.Quellentnahmen);
                        }
                        else if (art == ProjektPuffer.TYP_BHKW && MitBHKW)
                        {
                            BHKW.Stunde_Bedarf(stunde, pvUeberschuss, ref rest_heiz, ref rest_ww);
                        }
                    }

                    // Durchsatzbudget der Stunde festhalten — Stand NACH der
                    // Bedarfsdeckung. Genau diesen Rest kann Phase E aus den Speichern
                    // ziehen; zwischen C und E verändert ihn nichts.
                    absehbar[0] = rest_heiz > 0 ? rest_heiz : 0;
                    absehbar[1] = rest_ww > 0 ? rest_ww : 0;

                    // --- C) Speicherladung (Hauptsenken) ---------------------------------
                    Ladephase(stunde, false, pvUeberschuss, ref pvRest, absehbar, ebene);

                    // --- D) Zweitsenken --------------------------------------------------
                    Ladephase(stunde, true, pvUeberschuss, ref pvRest, absehbar, ebene);

                    // ZWISCHENSCHRITT DER KASKADE (Etappe D5a): Was die Speicher dieser
                    // Ebene gerade DURCHGEREICHT haben, gehört dem Verbraucher — nicht dem
                    // Erzeuger der nächsten Ebene. Ohne diese Rückgabe sähe die nächste
                    // Ebene einen Bedarf, den die vorige Ebene bereits über ihre
                    // hydraulische Weiche bedient hat, und deckte ihn ein zweites Mal.
                    // Der gespeicherte INHALT bleibt liegen — ihn holt Phase E am Ende der
                    // Stunde, nachdem alle Ebenen ihre Quellentnahme hatten.
                    if (ebene < _maxEbene)
                        DurchsatzPhase(stunde, ref rest_heiz, ref rest_ww);
                }

                // --- E) Nachentladung -----------------------------------------------------
                Entladephase(stunde, false, ref rest_heiz, ref rest_ww);

                // Bivalenzpunkt — dieselbe Stelle wie im Altpfad: nach der Entladung,
                // vor dem Heizstab.
                if (MitWP && rest_heiz + rest_ww > 0) biv.Add(WP.Temperatur[stunde]);

                // --- F) Heizstab ----------------------------------------------------------
                if (MitWP) WP.Heizstabphase(stunde, ref rest_heiz, ref rest_ww);

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
                if (rest_heiz < 0) rest_heiz = 0;
                if (rest_ww < 0) rest_ww = 0;
                kanaele.Heiz[stunde] = (float)rest_heiz;
                kanaele.WW[stunde] = (float)rest_ww;

                if (MitWP) WP.Zweikanalig_StundeEnde(stunde, rest_heiz, rest_ww);

                // Solarthermie: Was weder gedeckt noch gespeichert wurde, ist verworfen.
                if (MitSolar) Solar.Stunde_Ende(stunde);

                // BHKW: Was weder gedeckt noch gespeichert wurde, ist Wärmeüberschuss
                // (Paket 6 — im Altpfad kannte nur die stromgeführte Fahrweise diese
                // Größe, als Überlauf des Pendelspeichers). Dazu die Ganglinie seines
                // Restwärmebedarfs, gebildet an der BHKW-Position aus Stufeneingang,
                // Direktdeckung und der ihm in dieser Stunde zugerechneten Entladung (N4).
                if (MitBHKW) BHKW.Stunde_Ende(stunde, _entladungJeArtStunde[ART_BHKW]);

            } // end alle Stunden

            if (MitWP) WP.Zweikanalig_Ende(biv);
            if (MitSolar) Solar.Abschluss_Zweikanalig();
            if (MitKessel) Kessel.Abschluss_Zweikanalig();
            if (MitBHKW) BHKW.Abschluss_Zweikanalig();

            // N2: Zugerechnete Speicherentladung an die Erzeugermodule geben. Sie ist der
            // zweite Summand ihres EIGENANTEILS an der Bedarfsdeckung; den ersten
            // (Direktdeckung) führt jedes Modul selbst.
            if (MitWP) WP.Speicherentladung_Anteil = _entladungJeArt[ART_WP];
            if (MitSolar) Solar.Speicherentladung_Anteil = _entladungJeArt[ART_SOLAR];
            if (MitKessel) Kessel.Speicherentladung_Anteil = _entladungJeArt[ART_KESSEL];
            if (MitBHKW) BHKW.Speicherentladung_Anteil = _entladungJeArt[ART_BHKW];

            return true;
        }

        /// <summary>
        /// Ordnet dem BHKW seine Ladeaufträge zu (Paket 6).
        ///
        /// Anders als Wärmepumpe, Solarthermie und Heizkessel braucht das BHKW seine
        /// Aufträge nicht erst in der Ladephase: Die Fahrweisen entscheiden die
        /// Motorzuschaltung gegen <c>Bedarf + Speicherraum</c>, und der Speicherraum ist
        /// die Ladefähigkeit seiner Senke. Bei Hauptsenke HEIZKREIS fällt diese
        /// Entscheidung in Phase B, also vor der Ladephase — deshalb wird der Auftrag hier
        /// einmal je Lauf herausgesucht statt je Stunde.
        /// </summary>
        private void BhkwAuftraegeZuordnen()
        {
            BHKW.Auftrag_Haupt = null;
            BHKW.Auftrag_Zweit = null;

            if (Kontext == null || Kontext.LadenOhnePV == null) return;

            foreach (Ladeauftrag a in Kontext.LadenOhnePV)
            {
                if (a == null || a.Erzeugerart != ProjektPuffer.TYP_BHKW) continue;
                if (a.AnlagenID != BHKW.FuehrendeAnlage) continue;

                if (a.Zweitsenke) { if (BHKW.Auftrag_Zweit == null) BHKW.Auftrag_Zweit = a; }
                else { if (BHKW.Auftrag_Haupt == null) BHKW.Auftrag_Haupt = a; }
            }
        }

        /// <summary>
        /// Phasen C und D: die aus der Kaskade GELÖSTE Ladephase (Konzept 6.3).
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
        /// <param name="ebene">
        /// Rechenebene dieses Durchlaufs (Etappe D5a). Es laden ausschließlich die
        /// Anlagen dieser Ebene; ohne Quellbezug auf einen geladenen Puffer tragen alle
        /// Aufträge Ebene 0 und die Methode arbeitet die ganze Ordnung ab wie bisher.
        /// </param>
        private void Ladephase(int stunde, bool zweitsenken, bool pvUeberschuss,
                               ref double pvRest, double[] absehbar, int ebene)
        {
            List<Ladeauftrag> ordnung = Kontext.Ladeordnung_Stunde(pvUeberschuss);
            if (ordnung == null) return;

            for (int n = 0; n < ordnung.Count; n++)
            {
                Ladeauftrag a = ordnung[n];
                if (a == null || a.Zweitsenke != zweitsenken) continue;
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
        private void Entladephase(int stunde, bool vorab, ref double rest_heiz, ref double rest_ww)
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
                DurchsatzPhase(stunde, ref rest_heiz, ref rest_ww);
            }
            else
            {
                // D5a: Die Hysterese-Entscheidung dieser Stunde beginnt neu (siehe
                // _hysteresePhaseA).
                if (_hatKombi) _hysteresePhaseA.Clear();
            }

            // KANALREIHENFOLGE (Etappe D5a, Entwurfsentscheidung K-1):
            //
            // OHNE Kombispeicher sind die beiden Listen disjunkt und rühren getrennte
            // Größen an — die Reihenfolge ist dann gleichgültig, und sie bleibt bewusst
            // die bisherige (Heizung zuerst). Das ist keine Kosmetik: Die
            // Zurechnungssummen der Herkunftsrechnung sind double-Akkumulatoren, und eine
            // vertauschte Additionsreihenfolge kann ihr letztes Bit verändern. Die
            // Regressionszusage „Flag an, unverändertes Projekt = byte-gleich" hängt
            // daran.
            //
            // MIT Kombispeicher steht derselbe Speicher in BEIDEN Listen und bedient
            // beide Kanäle aus EINEM Vorrat. Reicht er nicht für beide, gilt WARMWASSER
            // ZUERST — die App-Konvention „Beides (Warmwasser zuerst)", umgesetzt als
            // vollständiger Warmwasserdurchlauf VOR dem Heizungsdurchlauf. Die
            // kanalweise Entladereihenfolge (3.6) bleibt innerhalb jedes Durchlaufs
            // unangetastet.
            if (_hatKombi)
            {
                EntladeKanal(Kontext.EntladenBrauchwasser, true, vorab, stunde, ref rest_heiz, ref rest_ww);
                EntladeKanal(Kontext.EntladenHeizung, false, vorab, stunde, ref rest_heiz, ref rest_ww);
            }
            else
            {
                EntladeKanal(Kontext.EntladenHeizung, false, vorab, stunde, ref rest_heiz, ref rest_ww);
                EntladeKanal(Kontext.EntladenBrauchwasser, true, vorab, stunde, ref rest_heiz, ref rest_ww);
            }
        }

        /// <summary>
        /// Rückgabe des DURCHSATZES beider Kanäle — als eigene Methode, weil sie seit
        /// Etappe D5a an zwei Stellen steht: vor der Nachentladung (Phase E) und zwischen
        /// zwei Rechenebenen der Kaskade. Kanalreihenfolge wie in
        /// <see cref="Entladephase"/> (K-1).
        /// </summary>
        private void DurchsatzPhase(int stunde, ref double rest_heiz, ref double rest_ww)
        {
            if (_hatKombi)
            {
                DurchsatzEntladen(Kontext.EntladenBrauchwasser, true, stunde, ref rest_heiz, ref rest_ww);
                DurchsatzEntladen(Kontext.EntladenHeizung, false, stunde, ref rest_heiz, ref rest_ww);
            }
            else
            {
                DurchsatzEntladen(Kontext.EntladenHeizung, false, stunde, ref rest_heiz, ref rest_ww);
                DurchsatzEntladen(Kontext.EntladenBrauchwasser, true, stunde, ref rest_heiz, ref rest_ww);
            }
        }

        /// <summary>
        /// Gibt den Teil des Füllstands zurück, der über <see cref="SimulationPufferspeicher.Q_max"/>
        /// hinausgeht — der Durchfluss dieser Stunde (siehe <see cref="Entladephase"/>).
        /// Ohne Durchlass in Phase C gibt es diesen Anteil nicht, und die Methode tut nichts.
        /// </summary>
        private void DurchsatzEntladen(List<SimulationPufferspeicher> speicher, bool brauchwasser,
                                       int stunde, ref double rest_heiz, ref double rest_ww)
        {
            if (speicher == null) return;

            for (int i = 0; i < speicher.Count; i++)
            {
                SimulationPufferspeicher sp = speicher[i];
                if (sp == null || sp.Q_max <= 0) continue;

                double ueber = sp.SOC - sp.Q_max;
                if (ueber <= 0) continue;

                double bedarf = brauchwasser ? rest_ww : rest_heiz;
                if (bedarf <= 0) continue;

                double gedeckt = sp.Entladen(Math.Min(ueber, bedarf), stunde);
                if (gedeckt <= 0) continue;

                SenkeAbziehen(brauchwasser ? WaermequelleClass.SENKE_WARMWASSER
                                           : WaermequelleClass.SENKE_HEIZUNG,
                              gedeckt, ref rest_ww, ref rest_heiz);

                Anteil_Entladen(sp, gedeckt);   // N2: Eigenanteil der Lader

#if DEBUG
                if (Entladeprobe != null)
                    Entladeprobe(stunde, false, brauchwasser, sp.ID_Pufferspeicher, gedeckt, sp.SOC);
#endif
            }
        }

        private void EntladeKanal(List<SimulationPufferspeicher> speicher, bool brauchwasser,
                                  bool vorab, int stunde, ref double rest_heiz, ref double rest_ww)
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
                // D5a: Ein Kombispeicher wird in dieser Phase zweimal besucht (er steht in
                // beiden Kanallisten). Fortgeschrieben wird trotzdem nur einmal je Stunde
                // — HystereseFortschreiben ist ein Zustandsübergang, kein Test.
                bool darfEntladen = vorab ? HystereseDerStunde(sp) : true;
                if (!darfEntladen) continue;

                double bedarf = brauchwasser ? rest_ww : rest_heiz;
                if (bedarf <= 0) continue;

                double gedeckt = sp.Entladen(bedarf, stunde);
                if (gedeckt <= 0) continue;

                // KANAL DES PUFFERS entscheidet, nicht SENKE_BEIDES (Konzept 6.3):
                // Ein Brauchwasserspeicher darf keinen Heizbedarf decken.
                SenkeAbziehen(brauchwasser ? WaermequelleClass.SENKE_WARMWASSER
                                           : WaermequelleClass.SENKE_HEIZUNG,
                              gedeckt, ref rest_ww, ref rest_heiz);

                Anteil_Entladen(sp, gedeckt);   // N2: Eigenanteil der Lader

#if DEBUG
                if (Entladeprobe != null)
                    Entladeprobe(stunde, vorab, brauchwasser, sp.ID_Pufferspeicher, gedeckt, sp.SOC);
#endif

                // Reicht der Speicher nicht, muss wieder nachgeladen werden.
                if (vorab && (brauchwasser ? rest_ww : rest_heiz) > 0.0001) sp.LaedtGerade = true;
            }
        }

#if DEBUG

        /// <summary>
        /// PRÜFHAKEN der Entladung — ausschließlich im Debug-Build, nach dem Muster von
        /// <see cref="Waermekanaele.Selbsttest"/> (kein Prüfcode im Release-Assembly).
        ///
        /// Die Knappheitsregel K-1 des Kombispeichers („reicht der Inhalt nicht für
        /// beide Bedarfe, gilt Warmwasser zuerst") ist eine Aussage über die REIHENFOLGE
        /// innerhalb einer Stunde. Aus den Jahres- und Stundenganglinien der
        /// Ergebnispersistenz lässt sie sich nicht ablesen: Dort steht die Entladung als
        /// EINE Zahl je Speicher und Stunde, ohne Kanal. Der Haken macht sie messbar,
        /// ohne dem Rechenkern eine Ausgabe zu geben.
        ///
        /// Parameter: Stunde, Phase A (<c>true</c>) oder E, Warmwasserkanal, Puffer-ID,
        /// gedeckte Menge [kWh], Füllstand danach [kWh].
        /// </summary>
        public static Action<int, bool, bool, int, double, double> Entladeprobe;

#endif

        /// <summary>
        /// Hysterese-Entscheidung der Phase A, je Speicher und Stunde GENAU EINMAL
        /// gebildet (Etappe D5a — siehe <see cref="_hysteresePhaseA"/>).
        ///
        /// Ohne Kombispeicher wird jeder Speicher ohnehin nur einmal besucht; dann geht
        /// der Aufruf ohne Umweg über das Wörterbuch, und das Verhalten ist Anweisung für
        /// Anweisung das bisherige.
        /// </summary>
        private bool HystereseDerStunde(SimulationPufferspeicher sp)
        {
            if (!_hatKombi || !sp.IstKombi) return sp.HystereseFortschreiben();

            bool entscheidung;
            if (_hysteresePhaseA.TryGetValue(sp, out entscheidung)) return entscheidung;

            entscheidung = sp.HystereseFortschreiben();
            _hysteresePhaseA[sp] = entscheidung;
            return entscheidung;
        }

        /// <summary>
        /// Zieht die erzeugte Wärmemenge vom passenden Bedarfsanteil ab.
        /// Bei der Wärmesenke "Beides" gilt Warmwasservorrang: zuerst wird der
        /// Warmwasserbedarf gedeckt, der Rest geht auf die Heizwärme.
        ///
        /// EINE Implementierung für alle Stufen (Paket 5): Wärmepumpe, Solarthermie,
        /// Heizkessel, Heizstab und Speicherentladung müssen dieselbe Kanalregel
        /// benutzen, sonst laufen die Kanäle auseinander. Der Rumpf ist der aus
        /// <c>SimulationWaermepumpe</c>, unverändert.
        /// </summary>
        public static void SenkeAbziehen(string senke, double menge,
                                         ref double rest_ww, ref double rest_heiz)
        {
            if (menge <= 0) return;

            if (senke == WaermequelleClass.SENKE_WARMWASSER)
            {
                rest_ww -= menge;
            }
            else if (senke == WaermequelleClass.SENKE_HEIZUNG)
            {
                rest_heiz -= menge;
            }
            else
            {
                double ww = Math.Min(menge, rest_ww);
                rest_ww -= ww;
                rest_heiz -= (menge - ww);
            }

            if (rest_ww < 0) rest_ww = 0;
            if (rest_heiz < 0) rest_heiz = 0;
        }
    }
}
