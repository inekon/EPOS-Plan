using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ConstrainedExecution;

namespace WindowsFormsApplication1
{
    // Die fehleranfällige und pauschale Jahres-Verlustberechnung (mit den fiktiven Betriebsstunden und der asymmetrischen Bereitschaft)
    // wurde komplett entfernt. Stattdessen wird der Brennstoffverbrauch nun stündlich direkt in der Simulationsschleife ermittelt:
    //
    // - Läuft ein Kessel in einer Stunde, wird sein Verbrauch über den Wirkungsgrad seiner Laststufe
    //   ermittelt (Teillastkennlinie, Kesselkennlinie.Eta; Konzept Kesselkennlinie 4.1), beim
    //   Brennwertkessel mit Brennwertkennlinie zusätzlich über den Rücklauf der Stunde
    //   (Kesselkennlinie.EtaBrennwert, Etappe E3).
    // - Steht er in einer Stunde still und ist er betriebsbereit (Heiztag oder Nachlauf, #568), wird
    //   ihm für diese exakte Stunde der Bereitschaftsverlust als Brennstoffverbrauch (Wärmeverlust)
    //   aufgeschlagen; außerhalb der Betriebsbereitschaft ist er abgeschaltet und verliert nichts.
    //
    // Am Ende des Jahres wird der Jahresnutzungsgrad in Schritt 5 absolut präzise aus der summierten Nutzwärme und dem summierten Gesamtverbrauch gebildet.

    public class SimulationSPK
    {
        public const int MAX_SPK = 10;

        // Listen und Projektdaten
        public List<string> spk_list = new List<string>();

        public Dictionary<string, int> spk_carrier = new Dictionary<string, int>();


        /// <summary>
        /// <c>Tab_Energieanlagen.ID</c> je Kessel, INDEXGLEICH zu <see cref="spk_list"/>
        /// (Konzept 6.2). Gefüllt von <c>SimulationControl.Simulation_SPK_Ctrl</c>.
        ///
        /// Warum eine zweite Liste statt einer Umstellung von <see cref="spk_list"/>:
        /// Der Bezeichner dort ist nicht nur Suchschlüssel der Kesseldaten, er ist
        /// zugleich der MODULNAME der Ergebniszeile (<c>SimulationRunner</c>). Eine
        /// Umstellung auf IDs hätte die Modulnamen aller Kesselergebnisse verändert.
        ///
        /// Gefüllt, aber noch von keinem Rechenpfad ausgewertet — auch der zweikanalige
        /// Weg wertet in Etappe 4b nur Wärmepumpen-Senken aus. Vorbereitung für
        /// Senkenauswertung und Ladepriorität je Kessel (Paket 5).
        /// </summary>
        public List<int> spk_anlagen_ids = new List<int>();

        public int m_ID_Projekt = 0;
        public double Max_Waermebedarf;
        public double[] Waermebedarf = new double[8760];
        public double[] Restwaerme = new double[8760];
        public double[] Strombedarf_stuendlich = new double[8760];
        public double[] Stromverbrauch_stuendlich = new double[8760];
        public double[] Kesselleistung_stuendlich = new double[8760];

        /// <summary>
        /// BETRIEBSBEREITSCHAFT des Projekts [h/a] (<c>Tab_Einstellungen.Kessel_Betriebsbereitschaft</c>,
        /// Schritt ① an der Heizkessel-Karte): die Stunden je Jahr, in denen ein Kessel
        /// warm gehalten wird — Laufstunden eingeschlossen. Größer 0 DECKELT die
        /// Bereitschaftsstunden je Kessel auf <c>Vorgabe − Laufstunden</c>
        /// (<see cref="BereitschaftDeckeln"/>); 0 heißt „kein Deckel", dann gilt allein die
        /// Stundenregel <see cref="IstBetriebsbereit"/>.
        /// </summary>
        public int Vorgabe_Betriebsbereitschaft;

        /// <summary>
        /// AUSSENTEMPERATUR des Laufs [°C je Stunde], 8760 Werte — dieselbe Reihe, mit der
        /// die Simulation rechnet (<c>SimulationControl.Stundentemperatur</c>, Klimaregion des
        /// Projekts in Ortszeit); die Grundlage der HEIZTAGE (<see cref="HeiztageAus"/>).
        /// Gesetzt von <c>SimulationControl</c> vor <see cref="Vorbereiten_Zweikanalig"/>, wie
        /// <see cref="Vorgabe_Betriebsbereitschaft"/>; <see cref="Init"/> lässt sie stehen, denn
        /// sie ist Eingang, nicht Laufzustand. <c>null</c> = keine Temperaturreihe; dann gilt
        /// jeder Tag als Heiztag.
        /// </summary>
        public double[] Aussentemperatur_Projekt;

        /// <summary>
        /// HEIZGRENZE des Projekts [°C] (<c>Tab_Einstellungen.Kessel_Heizgrenze</c>): Ein Tag ist
        /// Heiztag, wenn das Tagesmittel der Außentemperatur darunter liegt. <c>null</c> = die
        /// Vorgabe <see cref="HEIZGRENZE_VORGABE_C"/>. Gesetzt von <c>SimulationControl</c> wie
        /// <see cref="Aussentemperatur_Projekt"/>; wirksam ist <see cref="Heizgrenze_C"/>.
        /// </summary>
        public double? Vorgabe_Heizgrenze;

        /// <summary>
        /// Die VORGABE der Heizgrenze [°C] — sie gilt, solange das Projekt keine eigene führt
        /// (Anwenderentscheid 27.09.2026 zu #568).
        /// </summary>
        public const double HEIZGRENZE_VORGABE_C = 15;

        /// <summary>Untere Plausibilitätsgrenze der Heizgrenze [°C] — die Oberfläche lässt nichts darunter zu.</summary>
        public const double HEIZGRENZE_MIN_C = 0;

        /// <summary>Obere Plausibilitätsgrenze der Heizgrenze [°C] — die Oberfläche lässt nichts darüber zu.</summary>
        public const double HEIZGRENZE_MAX_C = 30;

        /// <summary>
        /// Ist <paramref name="heizgrenze"/> eine zulässige Eingabe? Leer (<c>null</c> = Vorgabe) oder
        /// eine Zahl von <see cref="HEIZGRENZE_MIN_C"/> bis <see cref="HEIZGRENZE_MAX_C"/>. Die Regel
        /// der Oberfläche; der Rechenweg selbst nimmt jede endliche Zahl
        /// (<see cref="HeizgrenzeWirksam"/>).
        /// </summary>
        public static bool HeizgrenzePlausibel(double? heizgrenze)
        {
            return !heizgrenze.HasValue ||
                   (heizgrenze.Value >= HEIZGRENZE_MIN_C && heizgrenze.Value <= HEIZGRENZE_MAX_C);
        }

        /// <summary>
        /// Die WIRKSAME Heizgrenze des Laufs [°C] — <see cref="HeizgrenzeWirksam"/> aus
        /// <see cref="Vorgabe_Heizgrenze"/>, gebildet in <see cref="Vorbereiten_Zweikanalig"/>.
        /// </summary>
        public double Heizgrenze_C { get; private set; } = HEIZGRENZE_VORGABE_C;

        /// <summary>
        /// Die Zahl der HEIZTAGE des Laufs (0 … 365) — Tage, an denen ein stillstehender Kessel
        /// betriebsbereit ist, gleich wann er zuletzt lief; ohne Temperaturreihe 365.
        /// </summary>
        public int Heiztage_Anzahl { get; private set; } = 365;

        /// <summary>
        /// Stunden, die ein Kessel nach seiner letzten Laufstunde betriebsbereit bleibt
        /// (Nachlauf), auch an einem Tag über der Heizgrenze — ein Kessel, der im Sommer
        /// Warmwasser oder Prozesswärme bereitet, wird zwischen seinen Laufstunden warm gehalten.
        /// </summary>
        internal const int BEREITSCHAFT_NACHLAUF_STUNDEN = 24;

        /// <summary>Heiztage des Laufs (365), aus <see cref="Aussentemperatur_Projekt"/>; <c>null</c> = jeder Tag.</summary>
        private bool[] _heiztage;

        /// <summary>Letzte Laufstunde je Kessel; <see cref="int.MinValue"/> = noch nie gelaufen.</summary>
        private readonly int[] _letzteLaufstunde = new int[MAX_SPK];

        /// <summary>Lief der Kessel in der Vorstunde? Grundlage der Startzählung.</summary>
        private readonly bool[] _liefVorstunde = new bool[MAX_SPK];

        /// <summary>
        /// LAUFSTUNDEN je Kessel [h/a]: Stunden mit Wärmeabgabe (Bedarfsdeckung,
        /// Speicherladung oder Anhub aus dem Quellpuffer) — dieselbe Entscheidung
        /// „läuft der Kessel?", nach der <see cref="Stunde_Abschluss"/> Brennstoff oder
        /// Bereitschaft bucht. Indexgleich zu <see cref="spk_list"/>.
        /// </summary>
        public int[] Laufstunden_Spk = new int[MAX_SPK];

        /// <summary>
        /// STARTS je Kessel [1/a] nach Konzept Kesselkennlinie 4.2 (Etappe E4): In einer
        /// TAKTSTUNDE (0 &lt; Q &lt; Mindestleistung, <see cref="Kesselkennlinie.Taktet"/>) zählt die
        /// Stunde so viele Starts, wie Mindestläufe ihre Wärme braucht
        /// (<see cref="Kesselkennlinie.StartsImTakt"/>); jede andere Laufstunde zählt einen Start,
        /// wenn der Kessel in der Vorstunde stand (die erste Laufstunde des Jahres zählt als Start).
        /// Der Elektrokessel hat kein Taktmodell: Seine Starts sind seine <see cref="Laufphasen_Spk"/>.
        ///
        /// <para><b>Wie beide Zählungen zusammenhängen:</b> Außerhalb der Taktstunden ist ein Start
        /// genau ein Übergang aus → an, also eine Laufphase. In einer Taktstunde ersetzt die Startzahl
        /// des Takts den Übergang (er ist ihr erster Start, wenn die Vorstunde stand). Damit gilt
        /// Starts = Laufphasen + Σ über die Taktstunden (Starts der Stunde − 1, wenn die Vorstunde
        /// stand, sonst − 0) — nie weniger als die Laufphasen, gleich ihnen ohne Taktstunde.</para>
        /// </summary>
        public int[] Starts_Spk = new int[MAX_SPK];

        /// <summary>
        /// LAUFPHASEN je Kessel [1/a]: Laufstunden, denen eine Stillstandsstunde vorausgeht (die erste
        /// Laufstunde des Jahres zählt mit) — die Übergänge aus → an im Stundenraster, die bis zur
        /// Etappe E4 als „Starts“ gezählt wurden.
        /// </summary>
        public int[] Laufphasen_Spk = new int[MAX_SPK];

        /// <summary>
        /// TAKTSTUNDEN je Kessel [h/a]: Laufstunden eines Brennstoffkessels, deren Wärme unter der
        /// Mindestleistung liegt (<see cref="Kesselkennlinie.Taktet"/>).
        /// </summary>
        public int[] Taktstunden_Spk = new int[MAX_SPK];

        /// <summary>
        /// ANFAHRVERLUST je Kessel [kWh/a]: Starts mal Anfahrverlust je Start
        /// (Konzept 4.2) — Brennstoff, aber keine Wärme; Teil von <see cref="Kessel_Verbrauch_MWh_Spk"/>.
        /// Beim Elektrokessel 0.
        /// </summary>
        public double[] Anfahrverlust_KWh_Spk = new double[MAX_SPK];

        /// <summary>
        /// BEREITSCHAFTSSTUNDEN je Kessel [h/a]: Stillstandsstunden, in denen der Kessel
        /// betriebsbereit ist (<see cref="IstBetriebsbereit"/>), nach dem Deckel der
        /// <see cref="Vorgabe_Betriebsbereitschaft"/>. Nur in ihnen fällt der
        /// Bereitschaftsverlust an.
        /// </summary>
        public int[] Bereitschaftsstunden_Spk = new int[MAX_SPK];

        /// <summary>
        /// BEREITSCHAFTSVERLUST je Kessel [kWh/a] — Bereitschaftsleistung mal
        /// <see cref="Bereitschaftsstunden_Spk"/>; Teil von <see cref="Kessel_Verbrauch_MWh_Spk"/>.
        /// Keine Wärme: Er geht in den Brennstoffeinsatz, in keine Nutzwärme und keine Zeitreihe.
        /// </summary>
        public double[] Bereitschaftsverlust_KWh_Spk = new double[MAX_SPK];

        /// <summary>
        /// Bedarfsdeckende SPEICHERENTLADUNG der ANDEREN Erzeuger der Speicherstufe [kWh/a]
        /// (Wärmepumpe, Solarthermie, BHKW) — der Teil des Restwärmebedarfs nach dem Kessel
        /// (Stufeneingang minus Kesselanteil), den der Puffer aus fremder Ladung gedeckt hat.
        /// Gesetzt von der <see cref="Kaskadenschleife"/> nach derselben Zurechnungsregel
        /// wie <see cref="Speicherentladung_Anteil"/>; als Vektorstufe ohne Speicher 0.
        /// </summary>
        public double SpeicherentladungAndere_Kwh = 0;

        /// <summary>
        /// Stundenfassung von <see cref="SpeicherentladungAndere_Kwh"/> je Kanal [kWh]
        /// (Wärmepumpe, Solarthermie, BHKW) — gefüllt von der <see cref="Kaskadenschleife"/>
        /// aus derselben Zurechnung. Nur Anzeige (Kesselbild); keine Rechengröße.
        /// </summary>
        public readonly Kanalganglinie SpeicherentladungAndere_KanalStuendlich = new Kanalganglinie();

        // Globale Ergebnisse
        public double WaermebedarfGesamtMwh = 0;
        public double StrombedarfGesamtKwh = 0;
        public double Maximale_Kesselleistung_Spk = 0;
        public double StromverbrauchSpkMwh = 0;
        public double BruttoWaermeSpkErzeugungMwh = 0;
        public double SWaermeSpkMwh = 0;
        public double Gasspitze_Spk = 0;

        // Globale Brennstoffzähler (in MWh)
        public double GasverbrauchSpkMwh = 0;
        public double OelverbrauchSpkMwh = 0;
        public double RapsoelverbrauchSpkMwh = 0;
        public double HolzverbrauchSpkMwh = 0;
        public double SonstigverbrauchSpkMwh = 0;
        public double KoksSpkMwh = 0;
        public double KohleSpkMwh = 0;
        public double PelletsSpkMwh = 0;
        public double TierischeFetteSpkMwh = 0;

        // Emissionen gesamt der Kesselstufe. EINHEITEN, seit W14a-E-8-B1 benannt statt
        // pauschal "kg": CO2 in t/a, SO2/NOx/CO/Staub in kg/a - dieselbe Konvention wie
        // EmissionsBilanzRechner (MWh x g/kWh / 1000 = t; MWh x mg/kWh / 1000 = kg).
        public double Em_CO2_SPK = 0;
        public double Em_CO_SPK = 0;
        public double Em_SO2_SPK = 0;
        public double Em_NOX_SPK = 0;
        public double Em_Staub_SPK = 0;

        // Emissionsfaktoren je Kessel aus DER EINEN Quelle (Emissionsquelle,
        // W14a-E-8-B1): CO2 in g/kWh - im Modus CO2E das Aequivalent (F7) -,
        // SO2/NOx/CO/Staub in mg/kWh. CO bleibt 0, solange der Artenkatalog keine
        // CO-Art fuehrt.
        public double[] CO2_SPK = new double[MAX_SPK];
        public double[] CO_SPK = new double[MAX_SPK];
        public double[] SO2_SPK = new double[MAX_SPK];
        public double[] NOX_SPK = new double[MAX_SPK];
        public double[] Staub_SPK = new double[MAX_SPK];

        // Kesselspezifische Arrays (Nutzwärme und Wirkungsgrade)
        public double[] s_waerme_Oel_Spk = new double[MAX_SPK];
        public double[] s_waerme_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Gas_Spk = new double[MAX_SPK];
        public double[] Kessel_Wirk_Oel_Spk = new double[MAX_SPK];

        // Speicher für die korrekte Nutzungsgrad-Bilanz
        public double[] Kessel_Jahresnutzungsgrad_Spk = new double[MAX_SPK];

        /// <summary>
        /// Brennstoffeinsatz JE KESSEL [MWh/a] — Nutzwärme über den Wirkungsgrad plus
        /// der Anfahrverlust der Starts (Etappe E4) plus die Bereitschaftsverluste der
        /// Stillstandsstunden, indexgleich zu <see cref="spk_list"/>.
        ///
        /// <para><b>Öffentlich wie seine drei Nachbarn</b> (<c>s_waerme_Gas_Spk</c>,
        /// <c>s_waerme_Oel_Spk</c>, <c>Kessel_Jahresnutzungsgrad_Spk</c>): Der
        /// <c>SimulationRunner</c> übernimmt den Wert in die Modulzeile
        /// (<c>Tab_ErgebnisHeizkesselModul.Verbrauch</c>), aus der die Kostenkette
        /// Energiekosten, CO₂-Bilanz und BEHG-Abgabe bildet. Solange das Feld
        /// <c>private</c> war, konnte der Runner es nicht lesen; die Spalte blieb im
        /// gesamten Bestand 0, und der Kesselbrennstoff fehlte in allen drei Größen.</para>
        ///
        /// <para><b>Anlagenebene und Modulebene führen dieselbe Größe:</b>
        /// <see cref="Bilanz_und_Nutzungsgrad"/> bucht genau diesen Wert je Brennstoffart
        /// auf die Anlagensummen (<c>GasverbrauchSpkMwh</c> und Geschwister). Die
        /// Modulzeile ist damit keine zweite Wahrheit, sondern dieselbe Zahl je Kessel
        /// statt je Träger — mit der Ausnahme des Elektrokessels, siehe
        /// <see cref="IstStromkessel"/>.</para>
        /// </summary>
        public double[] Kessel_Verbrauch_MWh_Spk = new double[MAX_SPK];

        // Interne Kesselkonfigurationen

        /// <summary>
        /// BEREITSCHAFTSLEISTUNG je Kessel [kW] — der Brennstoffeinsatz einer
        /// Stillstandsstunde ist dieser Wert mal eine Stunde (<see cref="Stunde_Abschluss"/>).
        /// Quelle ist <c>Tab_Heizkessel.Betriebsbereitschaftverlust</c> in der Einheit
        /// <c>Tab_Heizkessel.Bereitschaft_Einheit</c>: kW (Vorgabe; der Import liest die
        /// Bereitschaftsleistung aus VDI 3805 Blatt 3, Satz 700, Spalte 28, im Katalog
        /// 0,03 … 0,16 kW) oder Prozent der Nennleistung. Hier steht immer die Leistung in kW
        /// (<see cref="KesselBereitschaft.LeistungKw"/>, gerufen über
        /// <see cref="BereitschaftsleistungKw(double, string, double)"/>).
        /// </summary>
        double[] Betriebsbereitschaft_Verluste = new double[MAX_SPK];
        string[] Kessel_Name = new string[MAX_SPK];
        int[] Brennstoff_Betrieb_Spk = new int[MAX_SPK];
        int[] Brennstoff_Art = new int[MAX_SPK];
        double[] Kessel_Leistung_Spk = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // TEILLASTKENNLINIE (Konzept Kesselkennlinie 4.1, Etappe E2)
        //
        // Je Stunde rechnet ein Brennstoffkessel mit dem Wirkungsgrad seiner Laststufe
        // (Kesselkennlinie.Eta), gestützt auf η₁₀₀ (Wirkungsgrad_Gas/_Öl) und η₃₀
        // (Wirkungsgrad_Teillast30, leer = Normvorgabe nach Bauart, Entscheid F1). Das
        // wirksame η₃₀ bildet Stunde_Abschluss aus dem η₁₀₀ derselben Stunde — so folgt die
        // Vorgabe immer dem Nennwert, mit dem der Kessel wirklich rechnet.
        // ------------------------------------------------------------------

        /// <summary>Gepflegtes η₃₀ je Kessel, wie es in der Projektkopie steht; <c>null</c> = leer.</summary>
        private readonly double?[] _eta30Gepflegt = new double?[MAX_SPK];

        /// <summary>Bauart je Kessel für die Normvorgabe (<see cref="Kesselkennlinie.Bauart"/>).</summary>
        private readonly KesselBauart[] _bauart = new KesselBauart[MAX_SPK];

        /// <summary>Brennstoffbasierte Wärme der Laufstunden je Kessel [kWh/a] — Bezug der Mittelwerte.</summary>
        private readonly double[] _waermeBetriebKwh = new double[MAX_SPK];

        /// <summary>
        /// Wirkungsgrad je Kessel und Stunde (Faktor) — in Laufstunden η(β), sonst 0; angelegt in
        /// <see cref="Vorbereiten_Zweikanalig"/> für die Kessel des Laufs, sonst <c>null</c>.
        /// Nur Anzeige und Export (Konzept 5), keine Rechengröße.
        /// </summary>
        private readonly double[][] _wirkungsgradStunde = new double[MAX_SPK][];

        /// <summary>
        /// BRENNSTOFF DER LAUFSTUNDEN je Kessel [kWh/a]: Σ Wärme/η(β) — der Brennstoffeinsatz
        /// nach der Kennlinie, ohne Anfahr- und Bereitschaftsverlust. <see cref="Kessel_Verbrauch_MWh_Spk"/>
        /// ist dieser Wert plus <see cref="Anfahrverlust_KWh_Spk"/> plus <see cref="Bereitschaftsverlust_KWh_Spk"/>.
        /// </summary>
        public double[] BrennstoffBetrieb_KWh_Spk = new double[MAX_SPK];

        /// <summary>
        /// MEHRBRENNSTOFF AUS TEILLAST je Kessel [kWh/a] gegenüber dem Betrieb mit η₁₀₀:
        /// Σ (Wärme/η(β) − Wärme/η₁₀₀) über die Laufstunden (Konzept 4.1 Punkt 7). Negativ, wo
        /// der Kessel in Teillast besser arbeitet als bei Nennlast (Brennwertkessel); beim
        /// Niedertemperatur- und beim Elektrokessel 0.
        /// </summary>
        public double[] TeillastMehrbrennstoff_KWh_Spk = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // BRENNWERTKENNLINIE (Konzept Kesselkennlinie 4.1 Punkte 3 bis 5, Etappe E3)
        //
        // Ein Brennwertkessel mit Kennlinie_Brennwert = 1 rechnet je Laufstunde mit
        // η_eff = η_tr(β) + Δ₃₀ · g(T_RL) (Kesselkennlinie.EtaBrennwert). Den Rücklauf der Stunde
        // liefert die Kette (Kesselkennlinie.Ruecklauf): (a) Heizkreisrücklauf der
        // Anlagenkopplung, (b) Senkenspeicher, EINMAL je Stunde in Stunde_Start gelesen,
        // (c) gepflegtes Paar, (d) Rückfall 50 °C. Jeder andere Kessel rechnet Stunde für
        // Stunde wie in E2.
        // ------------------------------------------------------------------

        /// <summary>
        /// Der gerechnete RÜCKLAUF DES HEIZKREISES [°C je Stunde], NaN ohne gekoppelten Bedarf
        /// (Anlagenkopplung AK1, <c>HeizkreisProjekt.RuecklaufC</c>) — Stufe (a) der Rücklaufkette.
        /// Eingang wie <see cref="Aussentemperatur_Projekt"/>, gesetzt von <c>SimulationControl</c>
        /// vor <see cref="Vorbereiten_Zweikanalig"/>; <c>null</c> ohne Kopplung.
        /// </summary>
        public double[] Heizkreisruecklauf;

        /// <summary>
        /// Liest das GEPFLEGTE Paar einer Anlage (<c>Tab_Energieanlagen.ID</c>) und liefert seinen
        /// Rücklauf [°C], <c>null</c> ohne Paar — Stufe (c) der Rücklaufkette. Eingang, gesetzt von
        /// <c>SimulationControl</c> (dieselbe Kette Anlage → Heizkessel wie der Kessel-Hub);
        /// <c>null</c> = keine Stufe (c). Gefragt nur für Kessel mit Brennwertkennlinie.
        /// </summary>
        public Func<int, double?> RuecklaufPaarLesen;

        // ------------------------------------------------------------------
        // TEMPERATURNIVEAU DES PROZESSKANALS (Entscheidungsvorlage Modellgrenzen PW1 Stufe 1)
        //
        // (b) Ein Kessel, dessen GEPFLEGTER Vorlauf (Kette Anlage -> Heizkessel, wie W3) unter dem
        // geforderten Prozessvorlauf der Stunde liegt, deckt den Prozesskanal in dieser Stunde
        // nicht. (c) Ein Brennwertkessel mit Kennlinie sieht für den Anteil seiner Wärme, der in den
        // Prozesskanal ging, den Prozessrücklauf. Ohne Temperaturniveau (null) ist beides wirkungslos.
        // ------------------------------------------------------------------

        /// <summary>
        /// Das Temperaturniveau des Prozesskanals (PW1 Stufe 1); <c>null</c> = kein Prozess mit
        /// Temperaturpaar. Eingang, gesetzt von <c>SimulationControl</c> vor <see cref="Vorbereiten_Zweikanalig"/>.
        /// </summary>
        public Prozesstemperatur Prozesstemperatur;

        /// <summary>
        /// Liest den GEPFLEGTEN Vorlauf einer Anlage (<c>Tab_Energieanlagen.ID</c>) [°C], <c>null</c>
        /// ohne vollständiges Paar — dieselbe Kette Anlage → Heizkessel wie <see cref="RuecklaufPaarLesen"/>.
        /// Gefragt nur mit <see cref="Prozesstemperatur"/>.
        /// </summary>
        public Func<int, double?> VorlaufPaarLesen;

        /// <summary>Gepflegter Vorlauf je Kessel [°C]; <c>null</c> = keiner (erreicht jeden Prozessvorlauf).</summary>
        private readonly double?[] _prozessErzeugerVorlauf = new double?[MAX_SPK];

        /// <summary>In der laufenden Stunde in den Prozesskanal abgegebene Wärme je Kessel [kWh].</summary>
        private readonly double[] _prozessAbgabe = new double[MAX_SPK];

        private readonly int[] _prozessGesperrtStunden = new int[MAX_SPK];
        private readonly double[] _prozessGesperrtMax = new double[MAX_SPK];
        private readonly int[] _prozessRuecklaufStunden = new int[MAX_SPK];
        private readonly double[] _prozessRuecklaufSumme = new double[MAX_SPK];
        private readonly double[] _prozessRuecklaufGewicht = new double[MAX_SPK];

        /// <summary>Stunden, in denen Kessel <paramref name="index"/> den Prozesskanal nicht deckte (PW1 Stufe 1).</summary>
        public int ProzessGesperrtStunden(int index)
            => index >= 0 && index < MAX_SPK ? _prozessGesperrtStunden[index] : 0;

        /// <summary>Laufstunden, in denen der Prozessrücklauf in den Rücklauf der Brennwertkennlinie einging (PW1 Stufe 1).</summary>
        public int ProzessRuecklaufStunden(int index)
            => index >= 0 && index < MAX_SPK ? _prozessRuecklaufStunden[index] : 0;

        /// <summary>
        /// Meldet am Ende des Laufs je Kessel die Stunden ohne Prozessdeckung und die Stunden mit
        /// Prozessrücklauf (PW1 Stufe 1). Ohne Temperaturniveau meldet sie nichts.
        /// </summary>
        public void ProzessMelden()
        {
            if (Prozesstemperatur == null) return;
            for (int i = 0; i < _anzahlZweikanalig && i < spk_list.Count; i++)
            {
                if (_prozessGesperrtStunden[i] > 0)
                    SimulationProtokoll.Aktuell.Hinweis(MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                        System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_PROZESS_NICHT_ERREICHT,
                        spk_list[i], _prozessGesperrtStunden[i], _prozessGesperrtMax[i], _prozessErzeugerVorlauf[i] ?? 0));
                if (_prozessRuecklaufStunden[i] > 0 && _prozessRuecklaufGewicht[i] > 0)
                    SimulationProtokoll.Aktuell.Hinweis(MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                        System.Globalization.CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_PROZESS_KESSEL_RUECKLAUF,
                        spk_list[i], _prozessRuecklaufStunden[i],
                        _prozessRuecklaufSumme[i] / _prozessRuecklaufGewicht[i]));
            }
        }

        /// <summary>Rechnet der Kessel mit der Brennwertkennlinie?</summary>
        private readonly bool[] _brennwertKennlinie = new bool[MAX_SPK];

        /// <summary>Rücklauf des gepflegten Paars je Kessel [°C]; <c>null</c> = keins (Stufe c).</summary>
        private readonly double?[] _ruecklaufPaar = new double?[MAX_SPK];

        /// <summary>Senkenspeicher je Kessel (Stufe b); <c>null</c> = keiner.</summary>
        private readonly SimulationPufferspeicher[] _ruecklaufSpeicher = new SimulationPufferspeicher[MAX_SPK];

        /// <summary>Rücklauf aus dem Senkenspeicher der laufenden Stunde [°C]; NaN ohne Speicher.</summary>
        private readonly double[] _speicherRuecklauf = new double[MAX_SPK];

        /// <summary>
        /// Rücklauf je Kessel und Stunde [°C] — in Laufstunden der Brennwertkennlinie T_RL, sonst 0;
        /// angelegt nur für Kessel mit Brennwertkennlinie. Nur Anzeige und Export (Konzept 5).
        /// </summary>
        private readonly double[][] _ruecklaufStunde = new double[MAX_SPK][];

        /// <summary>Laufstunden je Kessel und Stufe der Rücklaufkette (Protokoll).</summary>
        private readonly int[][] _ruecklaufStufen = new int[MAX_SPK][];

        /// <summary>Σ Wärme · Rücklauf der Laufstunden je Kessel [kWh·°C] — Zähler des mittleren Rücklaufs.</summary>
        private readonly double[] _ruecklaufGewichtet = new double[MAX_SPK];

        /// <summary>
        /// BRENNWERTSTUNDEN je Kessel [h/a]: Laufstunden der Brennwertkennlinie mit einem Rücklauf
        /// unter dem Taupunkt (<see cref="Kesselkennlinie.Brennwertbetrieb"/>).
        /// </summary>
        public int[] Brennwertstunden_Spk = new int[MAX_SPK];

        /// <summary>Brennstoffbasierte Wärme der <see cref="Brennwertstunden_Spk"/> je Kessel [kWh/a].</summary>
        public double[] BrennwertWaerme_KWh_Spk = new double[MAX_SPK];

        /// <summary>
        /// MEHRBRENNSTOFF AUS BRENNWERTNUTZUNG je Kessel [kWh/a] gegenüber der trockenen Teillastkurve:
        /// Σ (Wärme/η_eff − Wärme/η_tr(β)) über die Laufstunden (Konzept 4.1 Punkt 7) — negativ, wo der
        /// Kondensationsgewinn Brennstoff spart; ohne Brennwertkennlinie 0. Zusammen mit
        /// <see cref="TeillastMehrbrennstoff_KWh_Spk"/> (dann gegenüber η_tr) ist das der Mehrbrennstoff
        /// gegenüber η₁₀₀.
        /// </summary>
        public double[] BrennwertMehrbrennstoff_KWh_Spk = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // TAKTEN (Konzept Kesselkennlinie 4.2, Etappe E4)
        //
        // Ein Brennstoffkessel, dessen Wärme in einer Laufstunde unter der Mindestleistung liegt,
        // taktet: Die Stunde zählt so viele Starts, wie Mindestläufe die Wärme braucht, und jeder
        // Start kostet den Anfahrverlust. Mindestleistung, Anfahrverlust und Mindestlaufzeit sind
        // gepflegt oder nehmen die Normvorgaben (Konzept 7.1, Entscheid F1); einmal je Lauf in
        // Kesseldaten_Einlesen gebildet. Der Elektrokessel rechnet ohne Taktmodell.
        // ------------------------------------------------------------------

        /// <summary>Rechnet der Kessel das Takten (jeder Brennstoffkessel)?</summary>
        private readonly bool[] _takten = new bool[MAX_SPK];

        /// <summary>Wirksame Mindestleistung je Kessel [kW].</summary>
        private readonly double[] _mindestleistungKw = new double[MAX_SPK];

        /// <summary>Wirksamer Anfahrverlust je Start und Kessel [kWh].</summary>
        private readonly double[] _anfahrverlustKwh = new double[MAX_SPK];

        /// <summary>Wirksame Mindestlaufzeit je Kessel [min].</summary>
        private readonly int[] _mindestlaufzeitMin = new int[MAX_SPK];

        /// <summary>Die drei Taktwerte der Projektkopie, wie sie dort stehen (für die Herkunft „Vorgabe“).</summary>
        private readonly double?[] _mindestleistungGepflegt = new double?[MAX_SPK];
        private readonly double?[] _anfahrverlustGepflegt = new double?[MAX_SPK];
        private readonly int?[] _mindestlaufzeitGepflegt = new int?[MAX_SPK];

        // PAKET A1: Hier stand "Berechnung(int ID_Projekt)" - der Einstieg des
        // einkanaligen Altpfads (Jahressumme, Kesseldaten_Einlesen,
        // Heizkessel_Simulation, Bilanz_und_Nutzungsgrad auf EINEM Bedarfsvektor). Er
        // ist mit dem Altpfad ersatzlos entfallen; der Einstieg des Moduls ist
        // Vorbereiten_Zweikanalig(), gerechnet wird in der Kaskadenschleife oder als
        // Vektorstufe (Berechnung_Zweikanalig).

        /// <summary>
        /// Schritt 2 der Kesselbilanz: Kesseldaten, Emissionsfaktoren, Wirkungsgrade und
        /// Bereitschaftsverluste je Kessel einlesen (Paket-5-Nacharbeit, Befund N6).
        /// </summary>
        /// <param name="heizkesselctrl">bereits erzeugter Controller des Aufrufers</param>
        /// <param name="Anzahl">Zahl der zu lesenden Kessel (bereits auf MAX_SPK begrenzt)</param>
        /// <returns>false = Abbruch (Kessel im Projekt nicht hinterlegt, B0-3).</returns>
        /// <remarks>
        /// PAKET 8 (Konzept 13.4): Der Parameter <c>mitDialog</c> ist entfallen. Er
        /// unterschied bis dahin den Altpfad (MessageBox) vom zweikanaligen Weg
        /// (<see cref="Fehlertext"/>, Nacharbeit N10) — Paket 8 verallgemeinert den
        /// Fehlerkanal, also wird dialogfrei gemeldet. Die Oberfläche zeigt den Text
        /// nach dem Lauf; dort ist ein Dialog richtig aufgehoben, mitten in der
        /// Kaskade war er es nie.
        /// </remarks>
        private bool Kesseldaten_Einlesen(HeizkesselCtrl heizkesselctrl, int Anzahl)
        {
            // W14a-E-8-B1: Der Berechnungsmodus (CO2 oder CO2-Aequivalent, Konzept F7)
            // gilt fuer den ganzen Lauf - EINMAL gelesen, nicht je Kessel.
            string modus = Emissionsquelle.Modus(m_ID_Projekt);

            for (int i = 0; i < Anzahl; i++)
            {
                // B0-3: Projektfilter — gleicher Kesselname in mehreren Projekten lieferte
                // sonst die Daten des ersten Treffers (falsche Leistung/Brennstoff/Emissionen).
                heizkesselctrl.ReadAll("Bezeichner='" + spk_list[i].Replace("'", "''") + "' AND ID_Projekt=" + m_ID_Projekt);

                // B0-3: Mit dem Projektfilter kann die Treffermenge leer sein (Kessel aus
                // dem Projekt entfernt, Altdaten ohne ID_Projekt) — vorher lieferte der
                // erste Namenstreffer falsche, aber vorhandene Daten. Sauber abbrechen
                // statt items[0]-Zugriff mit ArgumentOutOfRangeException.
                if (heizkesselctrl.rows == 0)
                {
                    string text = string.Format(MyResource.Resource.SIMENG_KESSEL_NICHT_HINTERLEGT,
                                                spk_list[i]);
                    Fehlertext = text;
                    SimulationProtokoll.Aktuell.Fehlermeldung(
                        MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + text);
                    return false;
                }

                Kessel_Name[i] = heizkesselctrl.items[0].Name;
                Kessel_Leistung_Spk[i] = heizkesselctrl.items[0].Ptherm;

                // ANWENDERENTSCHEID W14a-E-8-B1 (07.09.2026): HIER STAND DIE ZWEITE
                // EMISSIONSQUELLE DES HAUSES, und sie ist gefallen.
                //
                //     DataTable dt = DataRepository.GetDataTable(
                //         "select * from Tab_Brennstoff_Stamm where ID=?", ...Brennstoff);
                //     CO2_SPK[i] = row["CO2"] ... SO2 ... NOX ... Staub
                //
                // Der Kessel las damit die ALTE Brennstofftabelle unmittelbar, am
                // Emissionskatalog vorbei: kein Projektwert, keine aktive
                // emissionswert-Zeile, kein Berechnungsmodus. Die Wirtschaftlichkeit
                // desselben Projekts rechnete zur selben Zeit mit dem Katalogwert -
                // zwei Zahlen fuer dieselbe Anlage, und keine Anzeige, die den
                // Unterschied genannt haette.
                //
                // SEITHER GILT DIE EINE KETTE (Emissionsquelle -> EmissionsFaktorLader):
                // Projektwert -> aktive emissionswert-Zeile -> Tab_Brennstoff_Stamm ->
                // energy_carrier, im Modus des Projekts (F7). Hat die Anlage keinen
                // Energieträger, gilt der Brennstoff des Geraets gegen dieselbe
                // Tab_Brennstoff_Stamm - das ist das bisherige Verhalten und wird
                // protokolliert, damit die fehlende Zuordnung sichtbar bleibt.
                //
                // EINHEITEN: CO2 in g/kWh, SO2/NOx/Staub in mg/kWh (Katalogeinheiten,
                // Konzept F4) - dieselben Groessen, die EmissionsBilanzRechner fuehrt.
                // Die fuenf Emissionsspalten des KESSELKATALOGS (Tab_Heizkessel.CO2 …
                // .Staub) rechnen weiterhin nicht mit; sie sind seit B1 ausdruecklich
                // "nur Anzeige" (ParameterVerwendung).
                Emissionsfaktoren ef = Emissionsquelle.Fuer(
                    m_ID_Projekt, CarrierZuKessel(spk_list[i]),
                    heizkesselctrl.items[0].Brennstoff, modus);

                CO2_SPK[i] = ef.Co2GKwh;
                SO2_SPK[i] = ef.So2MgKwh;
                NOX_SPK[i] = ef.NoxMgKwh;
                CO_SPK[i] = ef.CoMgKwh;
                Staub_SPK[i] = ef.StaubMgKwh;

                if (ef.CarrierId <= 0)
                    SimulationProtokoll.Aktuell.HinweisEinmal(
                        "EMISSION_OHNE_TRAEGER_KESSEL_" + spk_list[i],
                        MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL +
                        "Emissionsfaktoren: Der Kessel „" + spk_list[i] + "\" hat keinen " +
                        "Energieträger zugeordnet - es gilt ersatzweise " + ef.Herkunft +
                        ". Mit zugeordnetem Energieträger rechnet der Lauf mit dem " +
                        "gepflegten Wert aus dem Emissionskatalog.");

                // Wirkungsgrade einlesen
                Kessel_Wirk_Gas_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Gas;
                Kessel_Wirk_Oel_Spk[i] = heizkesselctrl.items[0].Wirkungsgrad_Oel;

                // Absicherung Prozentwerte -> Faktor. Schwelle 1.5 statt 1.0 (18.08.2026):
                // Brennwertkessel liefern Hi-basierte Volllastwirkungsgrade bis ~104 % —
                // als Faktor gespeichert (710.01-Rückfall des Imports, z. B. Hoval
                // 103.5 % -> 1.035) hätte die alte Schwelle sie als Prozentwert gedeutet
                // und auf ~0.01 zerlegt. Echte Prozentwerte liegen >= 50, echte Faktoren
                // <= ~1.1; 1.5 trennt beide sauber (dieselbe Schwelle nutzt
                // WirtschaftlichkeitCtrl.LiesReferenzkessel seit Review 11).
                // Dieselbe Regel nimmt die Kurve des Katalogeditors (Kesselkennlinie.WirkungsgradAlsFaktor).
                Kessel_Wirk_Gas_Spk[i] = Kesselkennlinie.WirkungsgradAlsFaktor(Kessel_Wirk_Gas_Spk[i]);
                Kessel_Wirk_Oel_Spk[i] = Kesselkennlinie.WirkungsgradAlsFaktor(Kessel_Wirk_Oel_Spk[i]);

                Brennstoff_Betrieb_Spk[i] = heizkesselctrl.items[0].Brennstoff;
                Brennstoff_Art[i] = Brennstoff_Betrieb_Spk[i];

                // Konzept Kesselkennlinie 4.1 (Etappe E2): η₃₀ der Projektkopie und die Bauart
                // für die Normvorgabe eines leeren Felds (7.1, Entscheid F1).
                _eta30Gepflegt[i] = heizkesselctrl.items[0].Wirkungsgrad_Teillast30;
                _bauart[i] = Kesselkennlinie.Bauart(heizkesselctrl.items[0].Brennwert,
                                                    heizkesselctrl.items[0].Beschreibung);
                // Etappe E3: die Brennwertkennlinie nur auf ausdrückliche Wahl am Brennwertkessel
                // (Konzept 3.1) und mit einem Brennstoff, der kondensiert.
                _brennwertKennlinie[i] = Kesselkennlinie.RechnetMitBrennwertkennlinie(
                    heizkesselctrl.items[0].Brennwert, heizkesselctrl.items[0].Kennlinie_Brennwert, Brennstoff_Art[i]);
                if (Kesselkennlinie.RechnetMitKennlinie(Brennstoff_Art[i]))
                    SimulationProtokoll.Aktuell.HinweisEinmal(
                        "KESSEL_KENNLINIE_" + spk_list[i],
                        MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            TeillastwirkungsgradIstVorgabe(i)
                                ? MyResource.Resource.SIMENG_KESSEL_KENNLINIE_VORGABE
                                : MyResource.Resource.SIMENG_KESSEL_KENNLINIE_GEPFLEGT,
                            spk_list[i],
                            Nennwirkungsgrad(i).ToString("N3", System.Globalization.CultureInfo.CurrentCulture),
                            Teillastwirkungsgrad(i).ToString("N3", System.Globalization.CultureInfo.CurrentCulture),
                            BauartText(_bauart[i])));

                // Etappe E4 (Konzept 4.2): die Taktwerte - gepflegt oder die Normvorgabe (7.1, F1).
                TaktwerteBilden(i, heizkesselctrl.items[0]);

                Betriebsbereitschaft_Verluste[i] =
                    BereitschaftsleistungKw(heizkesselctrl.items[0].Betriebsbereitschaftverlust,
                                            heizkesselctrl.items[0].Bereitschaft_Einheit,
                                            heizkesselctrl.items[0].Ptherm);

                // Ein Katalogwert über 2 % der Nennleistung ist für eine Bereitschafts-
                // leistung ungewöhnlich hoch — der Lauf rechnet mit ihm, nennt ihn aber,
                // damit ein als Prozentwert gepflegter Eintrag auffällt.
                if (Kessel_Leistung_Spk[i] > 0 &&
                    Betriebsbereitschaft_Verluste[i] > BEREITSCHAFT_PLAUSIBEL_ANTEIL * Kessel_Leistung_Spk[i])
                    SimulationProtokoll.Aktuell.HinweisEinmal(
                        "KESSEL_BEREITSCHAFT_HOCH_" + spk_list[i],
                        MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_HOCH,
                            spk_list[i],
                            Betriebsbereitschaft_Verluste[i].ToString("N3", System.Globalization.CultureInfo.CurrentCulture),
                            Kessel_Leistung_Spk[i].ToString("N1", System.Globalization.CultureInfo.CurrentCulture)));

                Maximale_Kesselleistung_Spk += Kessel_Leistung_Spk[i];
            }

            return true;
        }

        /// <summary>
        /// Die TAKTWERTE des Kessels <paramref name="i"/> (Konzept Kesselkennlinie 4.2, Etappe E4):
        /// Mindestleistung, Anfahrverlust je Start und Mindestlaufzeit aus der Projektkopie, ein leeres
        /// Feld mit der Normvorgabe (7.1, Entscheid F1). Braucht Nennleistung, Brennstoff und Bauart des
        /// Kessels, also nach deren Einlesen gerufen. Der Elektrokessel bekommt kein Taktmodell.
        /// </summary>
        private void TaktwerteBilden(int i, HeizkesselModel kessel)
        {
            _mindestleistungGepflegt[i] = kessel.Mindestleistung;
            _anfahrverlustGepflegt[i] = kessel.Anfahrverlust_kWh;
            _mindestlaufzeitGepflegt[i] = kessel.Mindestlaufzeit_min;

            _takten[i] = Kesselkennlinie.RechnetMitTakten(Brennstoff_Art[i]);
            if (!_takten[i])
            {
                _mindestleistungKw[i] = 0;
                _anfahrverlustKwh[i] = 0;
                _mindestlaufzeitMin[i] = 0;
                return;
            }

            _mindestleistungKw[i] = Kesselkennlinie.MindestleistungWirksam(
                kessel.Mindestleistung, Kessel_Leistung_Spk[i], _bauart[i], Brennstoff_Art[i]);
            _anfahrverlustKwh[i] = Kesselkennlinie.AnfahrverlustWirksam(kessel.Anfahrverlust_kWh, Kessel_Leistung_Spk[i]);
            _mindestlaufzeitMin[i] = Kesselkennlinie.MindestlaufzeitWirksam(kessel.Mindestlaufzeit_min);

            var k = System.Globalization.CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.HinweisEinmal(
                "KESSEL_TAKTWERTE_" + spk_list[i],
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(k,
                    MyResource.Resource.SIMENG_KESSEL_TAKTWERTE,
                    spk_list[i],
                    _mindestleistungKw[i].ToString("N2", k), Herkunft(!MindestleistungIstVorgabe(i)),
                    _mindestlaufzeitMin[i], Herkunft(!MindestlaufzeitIstVorgabe(i)),
                    _anfahrverlustKwh[i].ToString("N3", k), Herkunft(!AnfahrverlustIstVorgabe(i))));
        }

        /// <summary>Der Herkunftstext eines Kennlinienwerts im Laufprotokoll: „gepflegt“ oder „Normvorgabe“.</summary>
        private static string Herkunft(bool gepflegt)
            => gepflegt ? MyResource.Resource.KESSEL_WERT_GEPFLEGT : MyResource.Resource.KESSEL_WERT_VORGABE;

        /// <summary>
        /// Der Energieträger EINES Kessels aus <see cref="spk_carrier"/> (W14a-E-8-B1);
        /// 0 = keiner zugeordnet.
        ///
        /// <para>Der Schlüssel ist der Anlagen-Bezeichner — dieselbe Spalte, aus der
        /// <c>SimulationControl.SPK_Liste_Laden</c> die <see cref="spk_list"/> füllt und
        /// aus der <c>EnergietraegerZuordnungLesen</c> das Verzeichnis baut. Der zweite
        /// Versuch OHNE Randleerzeichen ist die Vorsorge des
        /// <c>SimulationRunner</c> (:751), wo derselbe Zugriff seit jeher
        /// <c>Trim()</c> nutzt.</para>
        /// </summary>
        /// <summary>
        /// Anteil der Nennleistung, ab dem eine Bereitschaftsleistung im Laufprotokoll
        /// genannt wird (Hinweis, keine Korrektur). Gepflegte Katalogwerte liegen bei
        /// 0,1 … 0,8 % der Nennleistung.
        /// </summary>
        internal const double BEREITSCHAFT_PLAUSIBEL_ANTEIL = 0.02;

        /// <summary>
        /// Die Bereitschaftsleistung eines Kessels [kW] aus dem Katalogwert
        /// <c>Tab_Heizkessel.Betriebsbereitschaftverlust</c>, der in kW gepflegt ist.
        ///
        /// <para>Der Wert wird NICHT mit der Nennleistung multipliziert und NICHT als
        /// Prozentwert gedeutet: Beides hat ihn als Anteil gelesen — 0,075 kW eines
        /// 22-kW-Kessels wurden so zu 7,5 % oder 1,65 kW je Stillstandsstunde, das
        /// Zweiundzwanzigfache. Negativ oder nicht gesetzt heißt: kein Bereitschaftsverlust.</para>
        /// </summary>
        internal static double BereitschaftsleistungKw(double katalogwertKw)
            => KesselBereitschaft.LeistungKw(katalogwertKw, DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW, 0);

        /// <summary>
        /// Die Bereitschaftsleistung [kW] aus Wert und Einheit des Katalogsatzes — bei kW der
        /// Wert selbst (wie <see cref="BereitschaftsleistungKw(double)"/>), bei Prozent
        /// <c>Wert × Nennleistung / 100</c>. Die Regel steht einmal in
        /// <see cref="KesselBereitschaft.LeistungKw"/>.
        /// </summary>
        internal static double BereitschaftsleistungKw(double wert, string einheit, double nennleistungKw)
            => KesselBereitschaft.LeistungKw(wert, einheit, nennleistungKw);

        private int CarrierZuKessel(string bezeichner)
        {
            if (spk_carrier == null || string.IsNullOrEmpty(bezeichner)) return 0;

            int id;
            if (spk_carrier.TryGetValue(bezeichner, out id)) return id;
            if (spk_carrier.TryGetValue(bezeichner.Trim(), out id)) return id;
            return 0;
        }

        /// <summary>
        /// <c>Tab_Brennstoff_Stamm.ID</c> des ELEKTROKESSELS. Die Verzweigung in
        /// <see cref="Bilanz_und_Nutzungsgrad"/> führt dieselbe Zahl.
        /// </summary>
        public const int BRENNSTOFF_STROM = 13;

        /// <summary>
        /// Läuft DIESER Kessel auf Strom (<c>Tab_Heizkessel.Brennstoff</c> =
        /// <see cref="BRENNSTOFF_STROM"/>)?
        ///
        /// <para><b>Warum die Frage zählt.</b> Ein Elektrokessel bucht seinen Einsatz
        /// in <see cref="Bilanz_und_Nutzungsgrad"/> auf den STROMzähler
        /// (<c>StromverbrauchSpkMwh</c>) und über <c>Stromverbrauch_stuendlich</c> in
        /// die Stundenreihe; von dort steht er im Reststrombedarf und damit im
        /// Netzbezug, den die Kostenrechnung eigens bepreist. Seine Modulzeile führt
        /// deshalb bewusst KEINEN Brennstoffverbrauch: Derselbe Strom stünde sonst
        /// zweimal in Energiekosten, CO₂-Bilanz und BEHG-Abgabe — einmal als Netzbezug
        /// und einmal als „Brennstoff" seines Trägers.</para>
        ///
        /// <para>Die Zeile selbst bleibt (Wärme, Nutzungsgrad, Träger): Sie ist die
        /// Anzeigezeile des Kessels, und die Referenzlauf-Suite exportiert die Module
        /// indexgleich zu <see cref="spk_list"/> — eine fehlende Zeile verschöbe jede
        /// folgende.</para>
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        public bool IstStromkessel(int index)
        {
            if (index < 0 || index >= Brennstoff_Art.Length) return false;
            return IstStromkesselBrennstoff(Brennstoff_Art[index]);
        }

        /// <summary>
        /// <b>DIE eine Regel „das ist ein Elektrokessel"</b> — allein die
        /// Brennstoffangabe des Geräts (<c>Tab_Heizkessel.Brennstoff</c> =
        /// <see cref="BRENNSTOFF_STROM"/>) entscheidet es.
        ///
        /// <para>Statisch, weil außerhalb der Simulation dieselbe Frage gestellt wird
        /// und sie überall dieselbe Antwort bekommen muss: die Trägerzulassung
        /// (<c>EnergietraegerZulaessigkeit</c>, <c>ProjektEnergietraegerCtrl</c> — der
        /// Elektrokessel gehört zur elektrischen Welt wie Wärmepumpe und Heizstab) und
        /// der Bezugsgrößen-Auflöser (<c>EndenergieAufloeser</c>, der seinen
        /// Stromeinsatz ausweist). Das Brennstoffwort der Modulzeile taugt dafür nicht:
        /// Gespeicherte Läufe von vor Befund B-1 führen es leer.</para>
        /// </summary>
        /// <param name="brennstoffArt"><c>Tab_Heizkessel.Brennstoff</c>; 0 = unbekannt.</param>
        public static bool IstStromkesselBrennstoff(int brennstoffArt)
        {
            return brennstoffArt == BRENNSTOFF_STROM;
        }

        /// <summary>
        /// Rechnet der Brennstoffkessel <paramref name="index"/> mit dem Wirkungsgrad 1,0 —
        /// dem Platzhalter eines ungepflegten Katalogwerts (#568)? Maßgeblich ist derselbe
        /// Wirkungsgrad wie in der Stundenbilanz (Öl oder Gas); sein Brennstoffeinsatz ist
        /// dann seine Nutzwärme. Der Elektrokessel rechnet bewusst mit 1 und zählt nicht.
        /// </summary>
        public bool WirkungsgradIstPlatzhalter(int index)
        {
            if (index < 0 || index >= MAX_SPK || IstStromkessel(index)) return false;
            double wirk = Kesselkennlinie.IstOel(Brennstoff_Art[index]) ? Kessel_Wirk_Oel_Spk[index] : Kessel_Wirk_Gas_Spk[index];
            return Math.Abs(wirk - 1.0) < 1e-9;
        }

        /// <summary>
        /// <b>DIE eine Regel „wie viel Strom setzt ein Elektrokessel ein"</b> [MWh/a]:
        /// seine NUTZWÄRME. Der Rechenkern führt den Elektrokessel mit Nutzungsgrad 1 —
        /// <see cref="Bilanz_und_Nutzungsgrad"/> bucht genau diese Summe auf
        /// <see cref="StromverbrauchSpkMwh"/>, und die Stundenreihe trägt dieselbe
        /// Kesselleistung.
        ///
        /// <para><b>Warum es eine benannte Funktion ist.</b> Die Simulation bucht die
        /// Menge, der <c>EndenergieAufloeser</c> weist sie aus. Zwei Formeln an zwei
        /// Orten wären zwei Wahrheiten über denselben Strom; die Simulation rechnet
        /// unverändert weiter, ihre Formel hat nur einen Namen bekommen.</para>
        ///
        /// <para>Die beiden Summanden sind die Wärmekanäle der Kesselbilanz
        /// (<c>s_waerme_Gas_Spk</c>, <c>s_waerme_Oel_Spk</c>) bzw. in der Modulzeile
        /// <c>Waerme_Gas</c> und <c>Waerme_Oel</c> — beim Elektrokessel steht die
        /// Wärme auf dem Gaskanal, weil die Verzweigung der Stundenschleife nur
        /// „Öl oder nicht Öl" kennt.</para>
        /// </summary>
        public static double StromeinsatzElektrokesselMwh(double waermeGasMwh, double waermeOelMwh)
        {
            return waermeGasMwh + waermeOelMwh;
        }

        /// <summary>
        /// Das BRENNSTOFFWORT dieses Kessels für <c>Tab_ErgebnisHeizkesselModul.Brennstoff</c>
        /// — dieselben neun Wörter, die <c>ErgebnisCtrl.BHKWBrennstoff</c> für die
        /// BHKW-Modulzeile schreibt, plus „Strom" für den Elektrokessel.
        ///
        /// <para><b>Die Verzweigung ist Bereich für Bereich die aus
        /// <see cref="Bilanz_und_Nutzungsgrad"/></b> (<c>Tab_Brennstoff_Stamm.ID_Kategorie</c>):
        /// Wer dort einen Bereich verschiebt, verschiebt ihn hier mit, sonst nennt die
        /// Modulzeile einen anderen Brennstoff als den, auf dessen Anlagenzähler der
        /// Verbrauch gebucht wurde.</para>
        ///
        /// <para><b>Persistenzwert, immer deutsch</b> (Drei-Schichten-Regel, wie
        /// <c>mo.Modul</c>): Das Wort wird nach <c>Tab_ErgebnisHeizkesselModul</c>
        /// GESCHRIEBEN und von der Referenzlauf-Suite als Skalar exportiert. Ein
        /// übersetzter Ersatzname ließe DE- und EN-Läufe auseinanderlaufen.</para>
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        public string BrennstoffWort(int index)
        {
            if (index < 0 || index >= Brennstoff_Art.Length) return "";
            int art = Brennstoff_Art[index];

            if ((art >= 1 && art <= 5) || art == 14) return "Gas";
            if ((art >= 6 && art <= 9) || (art >= 18 && art <= 22)) return "Öl";
            if (art == 10) return "Koks";
            if (art == 11) return "Kohle";
            if (art == 12) return "Holz";
            if (art == BRENNSTOFF_STROM) return "Strom";
            if (art == 15) return "Pellets";
            if (art == 16) return "Rapsöl";
            if (art == 17) return "Tierische Fette";
            return "Sonstige";
        }

        /// <summary>
        /// Schritte 4 und 5 der Kesselbilanz: globale Brennstoffzähler, Emissionen und
        /// Jahresnutzungsgrad je Kessel. Beide Rechenwege (einkanalig und zweikanalig)
        /// benutzen sie unverändert.
        ///
        /// Voraussetzung: <c>s_waerme_Gas_Spk</c>, <c>s_waerme_Oel_Spk</c> und
        /// <c>Kessel_Verbrauch_MWh_Spk</c> stehen bereits in MWh, und <c>Gasspitze_Spk</c>
        /// ist aufsummiert.
        /// </summary>
        private void Bilanz_und_Nutzungsgrad(int Anzahl)
        {
            // 4. Verbrauch global bilanzieren und Emissionen berechnen
            for (int i = 0; i < Anzahl; i++)
            {
                double Kessel_Nutzkraft_Jahr = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i];
                SWaermeSpkMwh += Kessel_Nutzkraft_Jahr;

                double Kessel_Gesamtverbrauch_MWh = Kessel_Verbrauch_MWh_Spk[i];
                BruttoWaermeSpkErzeugungMwh += Kessel_Gesamtverbrauch_MWh;

                // Den Verbrauch auf die globalen Brennstoffzähler buchen. Die Bereiche
                // spiegeln Tab_Brennstoff_Stamm.ID_Kategorie: 14 (Biogas) ist Kategorie 1
                // und zählt zum Gas. Was keinen eigenen Zähler hat (23 Fernwärme,
                // 24 Sonstige, 25 Wasserstoff, künftige IDs), fängt das else als
                // Sammelposten — dieselbe Verzweigung erwartet die Anzeige
                // (Form_Simulation_Detail, _kesselBrennstoffIds) UND das Brennstoffwort
                // der Modulzeile (BrennstoffWort): Wer hier einen Bereich verschiebt,
                // verschiebt ihn dort mit.
                if ((Brennstoff_Art[i] >= 1 && Brennstoff_Art[i] <= 5) || Brennstoff_Art[i] == 14) GasverbrauchSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if ((Brennstoff_Art[i] >= 6 && Brennstoff_Art[i] <= 9) || (Brennstoff_Art[i] >= 18 && Brennstoff_Art[i] <= 22)) OelverbrauchSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 10) KoksSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 11) KohleSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 12) HolzverbrauchSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 17) TierischeFetteSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == BRENNSTOFF_STROM)
                {
                    // Elektrowärme / Wärmepumpe. DIE STELLE, an der der Elektrokessel
                    // seinen Einsatz auf den STROMzähler bucht statt auf einen
                    // Brennstoffzähler — der Grund, weshalb seine Modulzeile keinen
                    // Brennstoffverbrauch führt (IstStromkessel).
                    // Dieselbe Summe wie Kessel_Nutzkraft_Jahr, nur benannt: Die Regel
                    // „Stromeinsatz des Elektrokessels" steht EINMAL im Haus, und der
                    // EndenergieAufloeser ruft sie für die Anzeige (StromeinsatzElektrokesselMwh).
                    StromverbrauchSpkMwh += StromeinsatzElektrokesselMwh(s_waerme_Gas_Spk[i],
                                                                         s_waerme_Oel_Spk[i]);
                    // B0-2: auch hier kein Aliasing — sonst bleibt der Strom-Vektor ab dem
                    // zweiten Lauf dauerhaft an die Kessel-Ganglinie gebunden.
                    Stromverbrauch_stuendlich = (double[])Kesselleistung_stuendlich.Clone();
                }
                else if (Brennstoff_Art[i] == 15) PelletsSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else if (Brennstoff_Art[i] == 16) RapsoelverbrauchSpkMwh += Kessel_Gesamtverbrauch_MWh;
                else SonstigverbrauchSpkMwh += Kessel_Gesamtverbrauch_MWh;

                // Emissionen basierend auf dem echten stündlich ermittelten Gesamtverbrauch.
                //
                // DER ELEKTROKESSEL TRÄGT KEINE KESSELEMISSION (#568). Sein Strom steht über
                // Stromverbrauch_stuendlich im Reststrombedarf und damit im Netzbezug, den
                // die Emissionsbilanz mit dem Faktor des Stromträgers bewertet und die
                // Kostenrechnung bepreist; seine Modulzeile führt deshalb keinen Verbrauch
                // (IstStromkessel, SimulationRunner). Eine Kesselemission aus
                // Kessel_Verbrauch mal Stromfaktor wiese dieselbe Energie ein zweites Mal
                // aus - dieselbe Regel wie beim Verbrauch: einmal zählen, als Netzbezug.
                if (IstStromkessel(i)) continue;

                Em_CO2_SPK += Kessel_Gesamtverbrauch_MWh * CO2_SPK[i];
                Em_SO2_SPK += Kessel_Gesamtverbrauch_MWh * SO2_SPK[i];
                Em_NOX_SPK += Kessel_Gesamtverbrauch_MWh * NOX_SPK[i];
                Em_CO_SPK += Kessel_Gesamtverbrauch_MWh * CO_SPK[i];
                Em_Staub_SPK += Kessel_Gesamtverbrauch_MWh * Staub_SPK[i];
            }

            // Umrechnung der Summen: CO2 [MWh x g/kWh] -> t/a, SO2/NOx/CO/Staub
            // [MWh x mg/kWh] -> kg/a. Beide Male derselbe Teiler 1 000, weil die
            // Katalogeinheiten sich um genau diesen Faktor unterscheiden (F4).
            Em_CO2_SPK /= 1000;
            Em_SO2_SPK /= 1000;
            Em_NOX_SPK /= 1000;
            Em_CO_SPK /= 1000;
            Em_Staub_SPK /= 1000;
            if (GasverbrauchSpkMwh < 0.1) Gasspitze_Spk = 0;

            // 5. JAHRESNUTZUNGSGRAD PRO KESSEL SAUBER ERMITTELN
            for (int i = 0; i < Anzahl; i++)
            {
                double erzeugteWaerme = s_waerme_Gas_Spk[i] + s_waerme_Oel_Spk[i]; // Nutzwärme (MWh)
                double verbrauchterBrennstoff = Kessel_Verbrauch_MWh_Spk[i];       // Gesamtverbrauch inkl. Stillstand (MWh)

                if (erzeugteWaerme > 0 && verbrauchterBrennstoff > 0)
                {
                    double ngrad = (erzeugteWaerme / verbrauchterBrennstoff) * 100;

                    // Plausibilitätsgrenzen nach DIN
                    if (ngrad > 110.0) ngrad = 108.0;
                    if (ngrad < 1.0) ngrad = 1.0;

                    Kessel_Jahresnutzungsgrad_Spk[i] = ngrad;
                }
                else
                {
                    Kessel_Jahresnutzungsgrad_Spk[i] = 0; // Kessel stand still
                }
            }
        }

        // PAKET A1: Hier stand "Heizkessel_Simulation" - die EINKANALIGE Jahresschleife
        // der Kessel-Lastverteilung. Ihr einziger Aufrufer war Berechnung(int); beide
        // sind mit dem Altpfad entfallen. Die zweikanalige Fassung der Lastverteilung
        // steht in Stunde_Bedarf/Stunde_Abschluss.

        // ===================================================================
        // Zweikanaliger Weg (Paket 5 - Konzept 6.5, erster Punkt)
        // ===================================================================

        /// <summary>Anzahl der Kessel des zweikanaligen Wegs (nach der MAX_SPK-Grenze).</summary>
        private int _anzahlZweikanalig = 0;

        /// <summary>
        /// BRENNSTOFFBASIERTE Wärme, die ein Kessel in der LAUFENDEN Stunde erzeugt hat
        /// [kWh] — die Bezugsgröße von Verbrauch, Emissionen und Jahresnutzungsgrad.
        ///
        /// Ohne Quellpuffer ist sie identisch mit der ABGABE (<see cref="_kesselAbgabe"/>).
        /// Mit Quellpuffer (Etappe D5a) trennen sich beide: Der Kessel gibt weiterhin die
        /// volle Nutzwärme ab, hebt aber nur noch von der Puffertemperatur aus an — den
        /// Rest hat der Puffer beigesteuert.
        /// </summary>
        private readonly double[] _kesselStunde = new double[MAX_SPK];

        /// <summary>
        /// GESAMTE Wärmeabgabe eines Kessels in der laufenden Stunde [kWh], also
        /// brennstoffbasierte Wärme PLUS Quellwärme aus dem Puffer (Etappe D5a).
        ///
        /// Sie entscheidet allein darüber, ob der Kessel in dieser Stunde LÄUFT — und
        /// damit, ob ihm der Bereitschaftsverlust anzulasten ist. Ein Kessel, der
        /// vorgewärmtes Wasser nur noch geringfügig anhebt, steht nicht still.
        /// Ohne Quellpuffer ist das Feld Wert für Wert <see cref="_kesselStunde"/>.
        /// </summary>
        private readonly double[] _kesselAbgabe = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // WÄRMEQUELLE PUFFERSPEICHER (Etappe D5a, Konzept_KonfigUI_Hydraulik
        // Anforderung 6 — „Kessel-Kaskade")
        //
        // Ein Kessel mit WQ_Typ = Pufferspeicher und gültiger WQ_ID_Puffer bezieht seine
        // EINTRITTSTEMPERATUR aus diesem Puffer statt aus dem Systemrücklauf. Er muss die
        // Nutzwärme deshalb nicht mehr über die ganze Spreizung anheben, sondern nur noch
        // über den Rest:
        //
        //     Anteil = (T_Quelle − T_Rücklauf) / (T_Vorlauf − T_Rücklauf)      [0…1]
        //     Q_Puffer = Anteil · Q_nutz         (Entnahme = ENTLADUNG des Quellpuffers)
        //     Q_Kessel = Q_nutz − Q_Puffer       (nur DAS kostet Brennstoff)
        //
        // Das ist Zeile für Zeile die Konstruktion des Wärmepumpen-Quellbezugs, nur mit
        // dem Temperaturhub statt der Leistungszahl als Aufteilungsschlüssel: Dort gilt
        // Q_Quelle = Q · (1 − 1/COP), hier Q_Quelle = Q · Anteil.
        //
        // LIEFERT DER PUFFER WENIGER als Anteil · Q_nutz (er ist leer), springt der
        // Brennstoff für den Fehlbetrag ein — die Abgabe an den Kanal bleibt dieselbe,
        // die Energiebilanz geht auf. Die MENGE dieser Stunde ist dann aber nicht mehr
        // frei: Sie ist so zu wählen, dass der Fehlbetrag noch in die Nennleistung passt
        // (siehe MaxAbgabe, Nacharbeit E-K1-1).
        //
        // MODELLENTSCHEIDUNG zur Kesselleistung: Die Nennleistung begrenzt den EIGENEN,
        // brennstoffbasierten Beitrag — die Wärme aus dem Puffer kommt obendrauf:
        //
        //     Q_nutz(max) = min( P_nenn / (1 − Anteil),  P_nenn + Inhalt(Quellpuffer) )
        //
        // Das ist die hydraulische Kaskade, wie das Konzept sie beschreibt: Der Brenner
        // hebt weiter an, was der Puffer schon vorgewärmt hat — aber nur so weit, wie er
        // wirklich vorgewärmt hat. Die naheliegende Variante „Nennleistung begrenzt die
        // ganze Abgabe" ist an Szenario (d) gemessen und verworfen; die Begründung steht
        // in D5a_KombiKaskade_Protokoll.md, Abschnitt „Nacharbeit nach Reviews".
        // Eine Begrenzung nach Massenstrom und Wärmeübertrager kennt das Modell an keiner
        // Stelle. Der SPEICHER hat seit Paket P1 eine Lade-/Entladeleistungsgrenze
        // (Tab_Pufferspeicher.Ladeleistung_Max/Entladeleistung_Max, 0 = unbegrenzt); sie
        // greift über Entnahmefaehigkeit() auch hier. Eine eigene Grenze des
        // ÜBERTRAGERS zwischen Puffer und Kessel gibt es weiterhin nicht.
        // ------------------------------------------------------------------

        /// <summary>Quellpuffer je Kessel; <c>null</c> = keiner (Regelfall).</summary>
        private readonly SimulationPufferspeicher[] _quellSpeicher =
            new SimulationPufferspeicher[MAX_SPK];

        /// <summary>Anteil der Nutzwärme, den der Quellpuffer beisteuert (0…1); 0 = kein Bezug.</summary>
        private readonly double[] _quellAnteil = new double[MAX_SPK];

        // ------------------------------------------------------------------
        // PAKET B1 — TEMPERATURKOPPLUNG DES KESSEL-QUELLBEZUGS (Konzept 8.4)
        //
        // GLEICHBEHANDLUNG mit der Wärmepumpe (Konzept 8.4, Punkt 1): Bis P1 war
        // T_Quelle die VORLAUFTEMPERATUR der Speicherzeile — eine Jahreskonstante, und
        // damit auch _quellAnteil. Für einen GETEILTEN Quellpuffer (zugleich Senke eines
        // anderen Erzeugers) liefert jetzt der Speicherzustand die Temperatur:
        //
        //     T_Quelle(h) = SchichtTemperatur an der Quell-Entnahmehöhe (bis Q1: oben)
        //     Anteil(h)   = (T_Quelle(h) − T_Rücklauf) / (T_Vorlauf − T_Rücklauf), 0…1
        //
        // KEINE NEUE PHYSIK: Die Formel, die Mengenrechnung, die beiden Schranken in
        // MaxAbgabe und die Buchung in QuellwaermeHolen bleiben Zeichen für Zeichen die
        // von D5a. Getauscht ist allein die HERKUNFT von T_Quelle — aus der
        // Speicherzeile wird der Speicherzustand.
        //
        // DERSELBE LESEZEITPUNKT wie bei der WP: je Stunde GENAU EINMAL, vor Phase B der
        // Rechenebene (Quelltemperatur_Stunde, gerufen aus der Kaskadenschleife). Der
        // Wert gilt für Bedarfs- UND Ladephase derselben Stunde.
        //
        // EIGENSTÄNDIGE Quellspeicher bleiben statisch — dieselbe Grenze wie in 8.2.
        // ------------------------------------------------------------------

        /// <summary>Je Kessel: true = <see cref="_quellAnteil"/> folgt stündlich dem Speicherzustand.</summary>
        private readonly bool[] _quellKopplung = new bool[MAX_SPK];

        /// <summary>Vorlauftemperatur des Hubs je Kessel [°C] (nur bei Kopplung belegt).</summary>
        private readonly double[] _quellVorlauf = new double[MAX_SPK];

        /// <summary>Rücklauftemperatur des Hubs je Kessel [°C] (nur bei Kopplung belegt).</summary>
        private readonly double[] _quellRuecklauf = new double[MAX_SPK];

        /// <summary>
        /// PAKET Q1: Quell-Entnahmehöhe je Kessel, 0…1 (1 = ganz oben), aus
        /// <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c> (Schema-Schritt 54).
        /// <c>QuellkopplungSetzen</c> belegt sie; ohne gepflegten Wert steht dort
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> und damit das
        /// B1-Verhalten.
        /// </summary>
        private readonly double[] _quellHoehe = new double[MAX_SPK];

        /// <summary>
        /// Quelltemperatur-Ganglinie je gekoppeltem Kessel [°C] — LAUFERGEBNIS
        /// (Konzept 8.4). <c>null</c> für jeden Kessel ohne Kopplung.
        /// </summary>
        private readonly double[][] _quellTemperatur = new double[MAX_SPK][];

        /// <summary>Stunden je Kessel, in denen der gekoppelte Puffer nicht über den Rücklauf kam.</summary>
        private readonly int[] _quellZuKalt = new int[MAX_SPK];

        /// <summary>true = der Quellbezug des Kessels folgt stündlich dem Speicher (Paket B1).</summary>
        public bool QuelleGekoppelt(int index)
        {
            return index >= 0 && index < MAX_SPK && _quellKopplung[index];
        }

        /// <summary>
        /// Quelltemperatur-Ganglinie eines gekoppelten Kessels [°C]; <c>null</c> ohne
        /// Kopplung (Paket B1) — Lesezugriff für Anzeige und Zeitreihen-Export.
        /// </summary>
        public double[] Quelltemperaturen(int index)
        {
            return (index >= 0 && index < MAX_SPK) ? _quellTemperatur[index] : null;
        }

        /// <summary>
        /// Richtet die TEMPERATURKOPPLUNG eines Kessel-Quellbezugs ein (Paket B1,
        /// Konzept 8.4). Aufgerufen von <c>SimulationControl.KesselQuellbezugSetzen</c>
        /// anstelle von <see cref="QuellbezugSetzen"/>, sobald der Quellpuffer ein
        /// GETEILTER Puffer ist.
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        /// <param name="speicher">geteilter Quellpuffer</param>
        /// <param name="vorlauf">Vorlauf des Kessel-Hubs [°C]</param>
        /// <param name="ruecklauf">Rücklauf des Kessel-Hubs [°C]</param>
        /// <param name="anschlusshoehe">
        /// PAKET Q1: Quell-Entnahmehöhe 0…1 aus <c>WQ_Anschlusshoehe</c>
        /// (Schema-Schritt 54); <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> ohne
        /// gepflegten Wert — das ist exakt das Verhalten von Paket B1.
        /// </param>
        public void QuellkopplungSetzen(int index, SimulationPufferspeicher speicher,
                                        double vorlauf, double ruecklauf,
                                        double anschlusshoehe = SimulationPufferspeicher.HOEHE_OBEN)
        {
            if (index < 0 || index >= MAX_SPK) return;
            if (speicher == null || vorlauf <= ruecklauf) return;

            _quellSpeicher[index] = speicher;
            _quellVorlauf[index] = vorlauf;
            _quellRuecklauf[index] = ruecklauf;
            _quellHoehe[index] = anschlusshoehe;
            _quellKopplung[index] = true;
            _quellTemperatur[index] = new double[8760];

            // Startwert aus dem aktuellen Zustand; die Stundenabfrage übersteuert ihn vor
            // jeder Phase B. Ohne diese Zeile stünde bis zur ersten Abfrage ein Anteil
            // von 0 — dasselbe Ergebnis, aber der Zustand wäre nicht selbsterklärend.
            _quellAnteil[index] = AnteilAus(speicher.QuellEntnahmeTemperatur(anschlusshoehe), index);
        }

        /// <summary>
        /// PAKET Q1: die Quell-Entnahmehöhe des Kessels <paramref name="index"/>, 0…1;
        /// <see cref="SimulationPufferspeicher.HOEHE_OBEN"/> ohne Kopplung —
        /// Lesezugriff für Protokoll und Wirkproben.
        /// </summary>
        public double QuellAnschlusshoehe(int index)
        {
            if (index < 0 || index >= MAX_SPK || !_quellKopplung[index])
                return SimulationPufferspeicher.HOEHE_OBEN;
            return _quellHoehe[index];
        }

        /// <summary>Anteil (0…1) aus einer Quelltemperatur und dem Hub des Kessels.</summary>
        private double AnteilAus(double tQuelle, int index)
        {
            double spanne = _quellVorlauf[index] - _quellRuecklauf[index];
            if (spanne <= 0) return 0;

            double anteil = (tQuelle - _quellRuecklauf[index]) / spanne;
            if (anteil < 0) return 0;
            return anteil > 1 ? 1 : anteil;
        }

        /// <summary>
        /// Bildet den Quellanteil der Stunde für alle gekoppelten Kessel der AKTIVEN
        /// Rechenebene (Paket B1, Konzept 8.4) — GENAU EINMAL je Stunde und Ebene, vor
        /// Phase B, gerufen aus der Kaskadenschleife.
        ///
        /// <para>Ohne gekoppelten Kessel ist die Methode ein sofortiger Rücksprung.</para>
        /// </summary>
        /// <param name="alleEbenen">
        /// PAKET B2: true = ALLE gekoppelten Kessel, unabhängig von der aktiven
        /// Rechenebene — der Lesepunkt „Davor" (Vorbelegung) läuft am Stundenanfang und
        /// kennt deshalb noch keine Ebene. false = nur die Kessel der aktiven Ebene, der
        /// Lesepunkt „Danach" von Paket B1. In BEIDEN Modi wird je Stunde genau einmal je
        /// Kessel gelesen — und damit auch <see cref="_quellZuKalt"/> höchstens einmal je
        /// Stunde erhöht.
        /// </param>
        public void Quelltemperatur_Stunde(int stunde, bool alleEbenen = false)
        {
            if (stunde < 0 || stunde >= 8760) return;

            for (int i = 0; i < _anzahlZweikanalig && i < MAX_SPK; i++)
            {
                if (!_quellKopplung[i]) continue;
                if (!alleEbenen && !EbeneAktiv(i)) continue;

                SimulationPufferspeicher q = _quellSpeicher[i];
                if (q == null) continue;

                // PAKET Q1: an der gepflegten Quell-Entnahmehöhe statt fest oben.
                double tQuelle = q.QuellEntnahmeTemperatur(_quellHoehe[i]);
                if (_quellTemperatur[i] != null) _quellTemperatur[i][stunde] = (double)tQuelle;

                double anteil = AnteilAus(tQuelle, i);
                _quellAnteil[i] = anteil;

                // Der Puffer steht auf Rücklaufniveau: In dieser Stunde trägt er nichts
                // bei, der Kessel hebt wie ohne Kaskade von seinem Systemrücklauf aus an.
                // Gezählt und am Laufende EINMAL gemeldet (Gegenstück zur F13-Kappung der
                // Wärmepumpe) - stumm bliebe sonst ein Booster, der nie boostet.
                if (anteil <= 0) _quellZuKalt[i]++;
            }
        }

        /// <summary>
        /// PAKET B1: meldet je gekoppeltem Kessel EINMAL, in wie vielen Stunden der
        /// Quellpuffer nicht über den Systemrücklauf kam, und den Temperaturbereich der
        /// Quelle über das Jahr. Gerufen am Ende des Jahresdurchlaufs.
        /// </summary>
        public void QuellkopplungMelden()
        {
            for (int i = 0; i < _anzahlZweikanalig && i < MAX_SPK; i++)
            {
                if (!_quellKopplung[i] || _quellTemperatur[i] == null) continue;

                double min = double.MaxValue, max = double.MinValue;
                double summe = 0;
                for (int h = 0; h < 8760; h++)
                {
                    double v = _quellTemperatur[i][h];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    summe += v;
                }

                string name = (i < Kessel_Name.Length && Kessel_Name[i] != null) ? Kessel_Name[i] : "";

                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "Kessel_Quellkopplung_" + i + "_" + name,
                    string.Format(MyResource.Resource.SIMENG_KESSEL_QUELLKOPPLUNG_HINWEIS,
                                  name,
                                  min.ToString("F1"), max.ToString("F1"),
                                  (summe / 8760.0).ToString("F1"),
                                  _quellRuecklauf[i].ToString("F1"),
                                  _quellVorlauf[i].ToString("F1"),
                                  _quellZuKalt[i]));
            }
        }

        /// <summary>
        /// Meldungen über Quellentnahmen der laufenden Phase — die Kaskadenschleife führt
        /// daraus die Herkunftsrechnung fort und leert die Liste (siehe
        /// <see cref="Quellentnahme"/>). Ohne Quellpuffer bleibt sie durchgehend leer.
        /// </summary>
        public readonly List<Quellentnahme> Quellentnahmen = new List<Quellentnahme>();

        /// <summary>Aus Quellpuffern bezogene Wärme je Stunde [kWh]; ohne Quellbezug exakt 0.</summary>
        public double[] Quellwaerme_stuendlich = new double[8760];

        /// <summary>Jahressumme der Quellwärme [kWh]; ohne Quellbezug exakt 0.</summary>
        public double QuellwaermeGesamtKwh = 0;

        /// <summary>
        /// RECHENEBENE je Kessel (Etappe D5a) — indexgleich zu <see cref="spk_list"/>.
        /// Gesetzt von der Kaskadenschleife aus den Quellbezügen; <c>null</c> oder ein
        /// Vektor aus Nullen bedeutet „alle Kessel rechnen auf Ebene 0", also wie bisher.
        /// </summary>
        public int[] ModulEbenen;

        /// <summary>Ebene, die die Kaskadenschleife gerade abarbeitet (Etappe D5a).</summary>
        public int AktiveEbene = 0;

        /// <summary>true, wenn Kessel <paramref name="i"/> auf der aktiven Ebene rechnet.</summary>
        private bool EbeneAktiv(int i)
        {
            if (ModulEbenen == null || i < 0 || i >= ModulEbenen.Length) return AktiveEbene == 0;
            return ModulEbenen[i] == AktiveEbene;
        }

        /// <summary>
        /// Setzt den Quellbezug eines Kessels (Etappe D5a). Aufgerufen von
        /// <c>SimulationControl</c>, nachdem die Speicher-Registry offen ist.
        /// </summary>
        /// <param name="index">Kesselindex, wie in <see cref="spk_list"/></param>
        /// <param name="speicher">Quellpuffer; <c>null</c> hebt den Bezug auf</param>
        /// <param name="anteil">Anteil der Nutzwärme aus dem Puffer (0…1)</param>
        public void QuellbezugSetzen(int index, SimulationPufferspeicher speicher, double anteil)
        {
            if (index < 0 || index >= MAX_SPK) return;

            if (anteil < 0) anteil = 0;
            if (anteil > 1) anteil = 1;

            _quellSpeicher[index] = (anteil > 0) ? speicher : null;
            _quellAnteil[index] = (speicher != null) ? anteil : 0;
        }

        /// <summary>Quellpuffer eines Kessels; <c>null</c> = keiner (für Anzeige und Protokoll).</summary>
        public SimulationPufferspeicher QuellSpeicher(int index)
        {
            if (index < 0 || index >= MAX_SPK) return null;
            return _quellSpeicher[index];
        }

        /// <summary>Quellanteil eines Kessels (0…1); 0 = kein Bezug.</summary>
        public double QuellAnteil(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            return _quellAnteil[index];
        }

        /// <summary>
        /// Höchste Wärmeabgabe eines Kessels in der laufenden Stunde [kWh].
        ///
        /// Ohne Quellpuffer ist das die verbliebene Nennleistung — Wert für Wert der
        /// bisherige Ausdruck. Mit Quellpuffer kommt der Puffer-Anteil obendrauf (siehe
        /// den Blockkommentar zum Quellbezug).
        ///
        /// <para><b>ZWEI Schranken, nicht eine</b> (Nacharbeit E-K1-1). Der Puffer ist
        /// nach der Formel mit <c>Anteil</c> beteiligt — LIEFERN kann er aber nur, was in
        /// ihm steht. Deshalb sind beide Grenzen zu bilden und die kleinere gilt:
        /// <code>
        ///   nachLeistung = P_rest / (1 − Anteil)     // der Puffer liefert wie gerechnet
        ///   nachQuelle   = P_rest + Inhalt           // der Puffer liefert weniger,
        ///                                            // der Brennstoff deckt den Rest
        /// </code>
        /// In beiden Fällen bleibt der BRENNSTOFFBASIERTE Beitrag
        /// <c>eigen = menge − geliefert</c> damit ≤ <see cref="_restLeistung"/>, und die
        /// Nennleistung ist je Stunde eingehalten. Vorher stand hier nur die erste
        /// Schranke; klemmte <c>Entladen</c> am Füllstand, wurde die Differenz
        /// brennstoffbasiert und <c>_restLeistung</c> beliebig negativ — der Kessel lief
        /// über seiner Nennleistung (gemessen: 200,8 MWh bei 19,3 kW · 8760 h = 169,1 MWh).
        /// </para>
        ///
        /// <para>Der Ausdruck ist STETIG in <c>Anteil</c>: Je näher der Anteil an 1 rückt,
        /// desto größer wird <c>nachLeistung</c>, und die bindende Schranke ist der
        /// Speicherinhalt. Bei <c>Anteil = 1</c> — der Puffer trägt die ganze Anhebung —
        /// bleibt <c>nachQuelle</c>: sein Inhalt plus die Nennleistung, mit der der
        /// Brenner den Fehlbetrag von der Rücklauftemperatur aus deckt.</para>
        /// </summary>
        private double MaxAbgabe(int i)
        {
            double eigen = _restLeistung[i];
            if (eigen <= 0) return 0;

            SimulationPufferspeicher q = _quellSpeicher[i];
            if (q == null || _quellAnteil[i] <= 0) return eigen;

            // Was der Quellpuffer in DIESER Stunde höchstens beisteuern kann. Entladen()
            // klemmt am Füllstand; die Entnahmefähigkeit liefert seit Paket P1 den Rest
            // des Stundenbudgets aus Entladeleistung_Max (0 = unbegrenzt, der Regelfall).
            double ausQuelle = Math.Min(q.SOC > 0 ? q.SOC : 0, q.Entnahmefaehigkeit());

            double nachQuelle = eigen + ausQuelle;
            if (_quellAnteil[i] >= 1) return nachQuelle;

            double nachLeistung = eigen / (1.0 - _quellAnteil[i]);
            return Math.Min(nachLeistung, nachQuelle);
        }

        /// <summary>
        /// Holt den Quellanteil einer gerade abgegebenen Wärmemenge aus dem Quellpuffer
        /// und meldet die Entnahme an die Herkunftsrechnung.
        /// </summary>
        /// <param name="ziel">Zielspeicher der Wärme; <c>null</c> = Direktdeckung</param>
        /// <returns>tatsächlich aus dem Puffer bezogene Wärme [kWh]</returns>
        private double QuellwaermeHolen(int i, double menge, int stunde,
                                        SimulationPufferspeicher ziel)
        {
            if (menge <= 0) return 0;

            SimulationPufferspeicher q = _quellSpeicher[i];
            if (q == null || _quellAnteil[i] <= 0) return 0;

            // PAKET E1: OHNE Kanalangabe — eine Quellentnahme trägt keinen Bedarfskanal.
            // Sie wird deshalb auf dem Heizkanal gebucht (Vorbelegung von Entladen,
            // dieselbe Näherung wie Kaskadenschleife.Anteil_Entladen ohne Kanal).
            double geliefert = q.Entladen(menge * _quellAnteil[i], stunde);
            if (geliefert <= 0) return 0;

            QuellwaermeGesamtKwh += geliefert;
            if (stunde >= 0 && stunde < 8760) Quellwaerme_stuendlich[stunde] += geliefert;

            Quellentnahmen.Add(new Quellentnahme { Quelle = q, Menge = geliefert, Ziel = ziel });
            return geliefert;
        }

        /// <summary>Noch nicht vergebene Leistung eines Kessels in der laufenden Stunde [kW].</summary>
        private readonly double[] _restLeistung = new double[MAX_SPK];

        /// <summary>Gasspitze je Kessel [kW] (zweikanaliger Weg).</summary>
        private readonly double[] _gasspitzeKessel = new double[MAX_SPK];

        /// <summary>Senkenliste je Kessel, indexgleich zu <see cref="spk_list"/> (Paket S1).</summary>
        private readonly List<Senkenliste> _kesselSenke = new List<Senkenliste>();

        /// <summary>
        /// In Pufferspeicher geladene Kesselwärme je Stunde [kWh] (zweikanaliger Weg,
        /// Nacharbeit N1).
        ///
        /// Sie ist ein TEIL der Nutzwärme (<see cref="SWaermeSpkMwh"/>): Dort steht die
        /// gesamte abgegebene Wärme, also Direktdeckung PLUS Speicherladung — und genau
        /// so gehört sie dorthin, denn der Brennstoffverbrauch und der Jahresnutzungsgrad
        /// beziehen sich auf sie. Getrennt geführt wird die Ladung, weil die
        /// Ergebnispersistenz Restbedarf und Deckungsgrad aus der DIREKTDECKUNG bilden
        /// muss — sonst wird der Restbedarf negativ und die Summe der Deckungen
        /// überschreitet 100 % (dieselbe Mitkorrektur wie bei der Solarthermie,
        /// Konzept 6.4).
        /// </summary>
        public double[] Speicherladung_stuendlich = new double[8760];

        /// <summary>Jahressumme der Speicherladung [kWh]; ohne Puffer-Senke exakt 0.</summary>
        public double SpeicherladungGesamtKwh = 0;

        /// <summary>
        /// Der Anteil dieses Erzeugers an der SPEICHERENTLADUNG, die Bedarf gedeckt hat
        /// [kWh] (Nacharbeit N2, Interimsregel „Vermischung im Speicher").
        ///
        /// Gefüllt von <see cref="Kaskadenschleife"/>; ohne Puffer-Senke
        /// exakt 0. Zusammen mit der Direktdeckung ergibt sich daraus der EIGENANTEIL des
        /// Kessels an der Bedarfsdeckung — die Größe, die
        /// <c>Tab_ErgebnisHeizkessel.Waermebedarfsdeckung</c> ausweist.
        /// </summary>
        public double Speicherentladung_Anteil = 0;

        // ------------------------------------------------------------------
        // KANALINDIZIERTE DECKUNGSBUCHFÜHRUNG (Paket K2, Konzept 4.4)
        //
        // ZUSÄTZLICHE Aufschlüsselung, kein Ersatz: Die Skalare des Moduls
        // (SWaermeSpkMwh, SpeicherladungGesamtKwh, Speicherentladung_Anteil,
        // Kessel_Verbrauch_MWh_Spk …) werden unverändert gebildet und von
        // SimulationRunner unverändert gelesen. Es gilt
        //
        //   Σ Direktdeckung_Kanal[k]     == die in Phase B abgegebene Nutzwärme
        //                                   (= Kesselabgabe − Speicherladung)
        //   Σ Speicherentladung_Kanal[k] == Speicherentladung_Anteil
        //
        // bis auf die Rundungsklasse der getrennten Kanalarithmetik.
        // ------------------------------------------------------------------

        /// <summary>
        /// In Phase B direkt an den Bedarf abgegebene Kesselwärme je Kanal [kWh]
        /// (Konzept 4.4). Einen Skalar dieser Größe führt das Modul nicht — er steckt in
        /// <c>SWaermeSpkMwh</c> zusammen mit der Speicherladung; die Summe über die Kanäle
        /// ist genau der Direktanteil.
        /// </summary>
        public double[] Direktdeckung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// Anteil dieses Kessels an der bedarfsdeckenden Speicherentladung je Kanal [kWh]
        /// — die Aufschlüsselung von <see cref="Speicherentladung_Anteil"/>. Gefüllt von
        /// der <see cref="Kaskadenschleife"/>, wie der Skalar selbst.
        /// </summary>
        public double[] Speicherentladung_Kanal = new double[Kanal.ANZAHL];

        // ------------------------------------------------------------------
        // PAKET E2 (Nachtrag zu Konzept 4.4) — DIESELBEN GRÖSSEN ALS GANGLINIE,
        // gebucht an genau derselben Stelle und aus derselben Variablen. Je Kanal k gilt
        //   Σ_h Direktdeckung_KanalStuendlich[k][h]     == Direktdeckung_Kanal[k]
        //   Σ_h Speicherentladung_KanalStuendlich[k][h] == Speicherentladung_Kanal[k]
        // bis auf die Assoziativität der double-Addition.
        // ------------------------------------------------------------------

        /// <summary>Stundenfassung von <see cref="Direktdeckung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Direktdeckung_KanalStuendlich = new Kanalganglinie();

        /// <summary>Stundenfassung von <see cref="Speicherentladung_Kanal"/> [kWh] (Paket E2).</summary>
        public readonly Kanalganglinie Speicherentladung_KanalStuendlich = new Kanalganglinie();

        /// <summary>
        /// Fehlertext des zweikanaligen Wegs (Konzept 13.4: die Engine bleibt dialogfrei).
        /// Statt einer MessageBox mitten im Rechenlauf
        /// geht die Meldung über den Fehlerkanal Richtung
        /// <c>SimulationRunner.SimuliereUndSpeichere(… out fehler)</c> (Nacharbeit N10).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>Anzahl der Kessel, die im zweikanaligen Weg rechnen.</summary>
        public int KesselAnzahl { get { return _anzahlZweikanalig; } }

        /// <summary>
        /// Bezeichnung eines Kessels (<c>Tab_Heizkessel.Name</c>), indexgleich zu
        /// <see cref="spk_list"/>; "" außerhalb des Bereichs. Lesezugriff für Anzeigen
        /// und Zeitreihen-Beschriftungen (Paket B1).
        /// </summary>
        public string KesselName(int index)
        {
            if (index < 0 || index >= MAX_SPK || Kessel_Name[index] == null) return "";
            return Kessel_Name[index];
        }

        /// <summary>Senkenliste eines Kessels; <c>null</c> außerhalb des Indexbereichs (Paket S1).</summary>
        public Senkenliste KesselSenke(int index)
        {
            if (index < 0 || index >= _kesselSenke.Count) return null;
            return _kesselSenke[index];
        }

        /// <summary>
        /// Baut die Kessel des zweikanaligen Wegs auf — Schritte 1 und 2 aus
        /// <see cref="Berechnung"/>, Zeile für Zeile dieselben Abfragen und dieselben
        /// Absicherungen (B0-3, B0-12).
        /// </summary>
        /// <returns>false = Abbruch (Kessel nicht im Projekt hinterlegt).</returns>
        public bool Vorbereiten_Zweikanalig(int ID_Projekt, List<Senkenliste> senken)
        {
            m_ID_Projekt = ID_Projekt;

            Init();
            Fehlertext = "";
            Array.Clear(Waermebedarf, 0, Waermebedarf.Length);
            Array.Clear(_kesselStunde, 0, _kesselStunde.Length);
            Array.Clear(_restLeistung, 0, _restLeistung.Length);
            Array.Clear(_gasspitzeKessel, 0, _gasspitzeKessel.Length);
            _kesselSenke.Clear();

            WaermebedarfGesamtMwh = 0;
            Max_Waermebedarf = 0;
            StrombedarfGesamtKwh = Strombedarf_stuendlich.Sum();

            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();
            int Anzahl = spk_list.Count;

            // B0-12, dialogfrei (Nacharbeit N10, seit Paket 8 auf BEIDEN Wegen): Der Lauf
            // rechnet mit den ersten MAX_SPK Kesseln weiter und meldet das als Warnung im
            // Protokollkanal (Konzept 13.4). Das VERHALTEN ist dasselbe wie vorher.
            if (Anzahl > MAX_SPK)
            {
                SimulationProtokoll.Aktuell.Warnung(string.Format(
                    MyResource.Resource.SIMENG_KESSEL_MAX_UEBERSCHRITTEN, Anzahl, MAX_SPK, MAX_SPK));
                Anzahl = MAX_SPK;
            }

            // Schritt 2 aus Berechnung() — EINE Fassung für beide Wege (Nacharbeit N6).
            if (!Kesseldaten_Einlesen(heizkesselctrl, Anzahl)) return false;

            // #568: Heiztage des Laufs - Grundlage der Betriebsbereitschaft: Tagesmittel der
            // Aussentemperatur unter der Heizgrenze des Projekts.
            Heizgrenze_C = HeizgrenzeWirksam(Vorgabe_Heizgrenze);
            _heiztage = HeiztageAus(Aussentemperatur_Projekt, Heizgrenze_C);
            Heiztage_Anzahl = _heiztage == null ? 365 : _heiztage.Count(t => t);
            if (Anzahl > 0)
                SimulationProtokoll.Aktuell.Hinweis(
                    MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KESSEL_HEIZGRENZE,
                        Heizgrenze_C, Heiztage_Anzahl));

            // PW1 Stufe 1: die Zähler des Prozesskanals auf den Laufanfang.
            Array.Clear(_prozessErzeugerVorlauf, 0, MAX_SPK);
            Array.Clear(_prozessAbgabe, 0, MAX_SPK);
            Array.Clear(_prozessGesperrtStunden, 0, MAX_SPK);
            Array.Clear(_prozessGesperrtMax, 0, MAX_SPK);
            Array.Clear(_prozessRuecklaufStunden, 0, MAX_SPK);
            Array.Clear(_prozessRuecklaufSumme, 0, MAX_SPK);
            Array.Clear(_prozessRuecklaufGewicht, 0, MAX_SPK);

            // Senkenliste je Kessel: keine Physik, sondern die Konfiguration des
            // zweikanaligen Wegs — deshalb hier und nicht im gemeinsamen Einlesen.
            for (int i = 0; i < Anzahl; i++)
            {
                int idAnlage = (i < spk_anlagen_ids.Count) ? spk_anlagen_ids[i] : 0;
                _kesselSenke.Add(SenkeZuAnlage(senken, idAnlage));
                _wirkungsgradStunde[i] = new double[8760];

                // PW1 Stufe 1: der gepflegte Vorlauf - nur mit Temperaturniveau des Prozesskanals.
                _prozessErzeugerVorlauf[i] = (Prozesstemperatur != null && VorlaufPaarLesen != null && idAnlage > 0)
                    ? VorlaufPaarLesen(idAnlage) : null;

                // Etappe E3: Stufe (c) der Rücklaufkette und die Mitschrift - nur für Kessel mit
                // Brennwertkennlinie; jeder andere Kessel liest nichts zusätzlich.
                if (_brennwertKennlinie[i])
                {
                    _ruecklaufPaar[i] = (RuecklaufPaarLesen != null && idAnlage > 0) ? RuecklaufPaarLesen(idAnlage) : null;
                    _ruecklaufStunde[i] = new double[8760];
                    _ruecklaufStufen[i] = new int[4];
                    BrennwertkennlinieMelden(i);
                }
            }

            _anzahlZweikanalig = Anzahl;
            return true;
        }

        /// <summary>
        /// Senkenliste einer Anlage; ohne Zeile gilt die Rang-1-Invariante
        /// Heizkreis/Beides (Konzept 4.6/5.1).
        /// </summary>
        private static Senkenliste SenkeZuAnlage(List<Senkenliste> senken, int idAnlage)
        {
            if (senken != null && idAnlage > 0)
                foreach (Senkenliste s in senken)
                    if (s != null && s.AnlagenID == idAnlage) return s;

            return Senkenliste.Vorbelegung(idAnlage);
        }

        /// <summary>
        /// Stundenbeginn: jeder Kessel hat seine volle Nennleistung zur Verfügung, und
        /// der STUFENEINGANG wird festgehalten.
        ///
        /// NACHARBEIT PAKET 6, BEFUND N1: Der Stufeneingang ist der Kanalstand VOR der
        /// Vorabentladung (Phase A) — dieselbe Bezugsgröße, die die Stufe an ihrer
        /// Kaskadenposition sieht und die die Wärmepumpe seit Etappe 4b führt. Bis dahin
        /// stand er in <see cref="Stunde_Bedarf"/> und damit NACH Phase A; die Größe
        /// <c>Tab_ErgebnisHeizkessel.Waermebedarf</c> fiel dadurch still ab, sobald ein
        /// Speicher vorab entlud. Ohne Speicher in der Stufe ändert sich nichts — dann
        /// gibt Phase A nichts ab.
        ///
        /// <para>PAKET K2: Der Stufeneingang ist die Summe ÜBER ALLE Kanäle des
        /// Restbedarfsfeldes — ohne Prozesswärmeanteil Zeichen für Zeichen die bisherige
        /// Größe <c>rest_heiz + rest_ww</c>.</para>
        /// </summary>
        public void Stunde_Start(int stunde, double[] rest)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                _kesselStunde[i] = 0;
                _kesselAbgabe[i] = 0;
                _restLeistung[i] = Kessel_Leistung_Spk[i];
                _prozessAbgabe[i] = 0;

                // Etappe E3, Stufe (b) der Rücklaufkette: der Senkenspeicher EINMAL je Stunde, am
                // Stundenanfang - der Zustand am Ende der Vorstunde, wie der Lesepunkt „Davor" der
                // Booster-Quelltemperatur. Geschichtet die unterste Schicht, sonst RL_eff.
                if (_brennwertKennlinie[i])
                {
                    SimulationPufferspeicher sp = _ruecklaufSpeicher[i];
                    _speicherRuecklauf[i] = sp == null ? double.NaN : (sp.Geschichtet ? sp.T_unten : sp.RL_eff);
                }
            }

            double eingang = Kaskadenschleife.RestSumme(rest);
            if (eingang < 0) eingang = 0;
            if (stunde >= 0 && stunde < 8760) Waermebedarf[stunde] = (double)eingang;
            if (Max_Waermebedarf < eingang) Max_Waermebedarf = eingang;
        }

        /// <summary>
        /// Phase B der Reihenfolge-Invariante (Konzept 6.3) für die Heizkessel: die
        /// KANALGERECHTE Fassung der Lastverteilung (bis Paket A1: Heizkessel_Simulation).
        ///
        /// Konzept 6.5 beschreibt sie als „zweiten Schleifendurchlauf mit erhaltenem
        /// Zwischenzustand". Umgesetzt ist genau das, nur ohne zweiten Durchlauf: Der
        /// Kessel bedient in EINER Stunde erst den einen, dann den anderen Kanal — bei
        /// Bedarfsart <c>Beides</c> mit Warmwasservorrang, wie überall in dieser Engine
        /// (<c>SenkeAbziehen</c>) —, und die abgegebene Nutzwärme sammelt sich in
        /// <see cref="_kesselStunde"/>. Der Zwischenzustand ist damit erhalten, und die
        /// BEREITSCHAFTSVERLUSTE fallen nur EINMAL je Stunde und Kessel an: Sie werden
        /// nicht hier, sondern in <see cref="Stunde_Abschluss"/> gebucht, und zwar an
        /// genau einer Stelle für beide Kanäle und die Speicherladung zusammen.
        ///
        /// Ein Kessel OHNE Direktsenke deckt hier NICHTS — er lädt ausschließlich
        /// (Ladephasen), und damit gilt derselbe Doppelzählungs-Freibeweis wie bei der
        /// Wärmepumpe.
        ///
        /// <para>PAKET K2: <paramref name="rest"/> ist der offene Bedarf je Kanal und
        /// tritt an die Stelle des Paares <c>ref rest_heiz, ref rest_ww</c>; es wird
        /// IN-PLACE fortgeschrieben. Die Bezugsgröße <c>verfuegbar</c> kommt nicht mehr
        /// aus einer eigenen Dreifach-Verzweigung über <c>WS_Typ</c>, sondern aus
        /// <c>Kanalabzug.Offen</c> — derselben Quelle, gegen die gleich abgezogen
        /// wird (Konzept 4.3). PAKET S1: gefragt wird die ganze SENKENLISTE des Kessels
        /// statt einer einzelnen Bedarfsart (Konzept 5.2).</para>
        /// </summary>
        public void Stunde_Bedarf(int stunde, double[] rest)
        {
            // Der Stufeneingang steht seit der Nacharbeit N1 in Stunde_Start - VOR der
            // Vorabentladung (Phase A).
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                if (_restLeistung[i] <= 0) continue;

                // D5a: In dieser Phase rechnen nur die Kessel der aktiven Rechenebene.
                // Ohne Quellbezug steht jeder Kessel auf Ebene 0 und die Prüfung ist
                // immer wahr.
                if (!EbeneAktiv(i)) continue;

                // PAKET S1: Gefragt wird die DIREKTSENKEN-KETTE des Kessels
                // (Konzept 5.2). Ein Kessel ganz ohne Direktsenke lädt ausschließlich und
                // deckt hier nichts - die Nachfolge der Prüfung „Hauptsenke != Heizkreis".
                Senkenliste senken = _kesselSenke[i];
                if (senken != null && !senken.HatDirektsenke) continue;

                // PW1 STUFE 1 (b): Erreicht der gepflegte Vorlauf des Kessels den geforderten
                // Prozessvorlauf der Stunde nicht, ist der Prozesskanal für ihn gesperrt - für die
                // Dauer dieses Kessels auf 0, danach unverändert zurückgelegt (Muster KU2 der
                // Wärmepumpe). Ohne Temperaturniveau ist die Bedingung falsch.
                bool prozessDirekt = Prozesstemperatur != null && senken != null && senken.BedientProzessDirekt;
                bool prozessGesperrt = false;
                double prozessZurueck = 0;
                if (prozessDirekt && rest[Kanal.PROZESS] > 0)
                {
                    double gefordert = Prozesstemperatur.Vorlauf(stunde);
                    if (!Prozesstemperatur.Erreicht(_prozessErzeugerVorlauf[i] ?? 0, gefordert))
                    {
                        prozessGesperrt = true;
                        prozessZurueck = rest[Kanal.PROZESS];
                        rest[Kanal.PROZESS] = 0;
                        _prozessGesperrtStunden[i]++;
                        if (gefordert > _prozessGesperrtMax[i]) _prozessGesperrtMax[i] = gefordert;
                    }
                }
                try
                {

                double verfuegbar = Kanalabzug.Offen(senken, rest);

                if (verfuegbar <= 0) continue;

                double menge = Math.Min(MaxAbgabe(i), verfuegbar);
                if (menge <= 0) continue;

                // PW1 Stufe 1 (c): der Prozessanteil dieser Abgabe, gemessen am Kanalrest.
                double prozessVorher = rest[Kanal.PROZESS];

                // K2: Abzug über die eine Kanalregel, mit gemessener Aufschlüsselung je
                // Kanal (Konzept 4.4). Die abgezogene Gesamtmenge ist konstruktiv genau
                // "menge" - sie ist auf den offenen Kanalbedarf begrenzt.
                //
                // PAKET E2: derselbe Abzug schreibt zusätzlich die Kanalganglinie der
                // Stunde - aus derselben gemessenen rest-Differenz.
                Kanalabzug.Abziehen(senken, menge, rest, Direktdeckung_Kanal,
                                    Direktdeckung_KanalStuendlich, stunde);

                if (prozessDirekt) _prozessAbgabe[i] += prozessVorher - rest[Kanal.PROZESS];

                _kesselAbgabe[i] += menge;

                // D5a: Der Quellpuffer trägt seinen Temperaturhub bei; nur der Rest kostet
                // Brennstoff und verbraucht Nennleistung. Ohne Quellbezug ist der Abzug
                // exakt 0 und beide Zeilen sind die bisherigen.
                double eigen = menge - QuellwaermeHolen(i, menge, stunde, null);
                _restLeistung[i] -= eigen;
                _kesselStunde[i] += eigen;

                // E-K1-1: MaxAbgabe hält „eigen ≤ Restleistung" rechnerisch ein; die
                // Klemmung fängt allein die Gleitkomma-Reste. Ohne Quellbezug ist
                // _restLeistung nie negativ und die Zeile wirkungslos.
                if (_restLeistung[i] < 0) _restLeistung[i] = 0;
                }
                finally
                {
                    if (prozessGesperrt) rest[Kanal.PROZESS] = prozessZurueck;
                }
            }

            if (stunde >= 0 && stunde < 8760)
                Restwaerme[stunde] = (double)Kaskadenschleife.RestSumme(rest);
        }

        /// <summary>
        /// Phasen C/D für EINEN Ladeauftrag (Konzept 6.5: „Senkenauswertung je Kessel —
        /// Puffer laden bis Abschaltschwelle").
        ///
        /// Die Abschaltschwelle steckt in <see cref="Ladeauftrag.ObergrenzeStunde"/>: Sie
        /// ist nach der Auflösungsregel 3.4 bereits bestimmt — eigene Ladegrenze, sonst
        /// <c>Schwelle_Aus</c> für die vorrangige und <c>Schwelle_Aus_Nachrang</c> für
        /// nachrangige Anlagen. Der Kessel ist mit Vorgaberang 40 der letzte Lader; wo
        /// eine Solar-Reservezone gepflegt ist, lädt er also nur bis dorthin.
        ///
        /// KEIN <c>SenkeAbziehen</c>; Bilanzraum und Durchsatzbudget wie in Paket 4.
        /// </summary>
        /// <returns>tatsächlich geladene Wärmemenge [kWh]</returns>
        public double Zweikanalig_Laden(Ladeauftrag a, int stunde, bool pvUeberschuss, double[] absehbar)
        {
            if (a == null || a.Speicher == null) return 0;

            int i = a.Modulindex;
            if (i < 0 || i >= _anzahlZweikanalig) return 0;
            if (_restLeistung[i] <= 0) return 0;

            SimulationPufferspeicher sp = a.Speicher;

            // D5a: Beim KOMBISPEICHER ist das Durchsatzbudget die Summe beider Kanäle;
            // die gemeinsame Fassung steht in der Kaskadenschleife. Ohne Kombispeicher
            // liefert sie Anweisung für Anweisung das Bisherige.
            double ladefaehig = sp.Ladefaehigkeit(a.ObergrenzeStunde(pvUeberschuss));
            double durchlass = Kaskadenschleife.DurchlassBudget(sp, absehbar);
            if (ladefaehig + durchlass <= 0) return 0;

            double menge = Math.Min(MaxAbgabe(i), ladefaehig + durchlass);
            if (menge <= 0) return 0;

            double ladung = sp.Laden(menge, stunde, durchlass);
            if (ladung <= 0) return 0;

            double genutzterDurchlass = ladung - ladefaehig;
            if (genutzterDurchlass > 0)
                Kaskadenschleife.DurchlassBuchen(sp, absehbar, genutzterDurchlass);

            _kesselAbgabe[i] += ladung;

            // D5a: Auch die Speicherladung kann zum Teil aus dem Quellpuffer stammen.
            // Gemeldet wird sie mit ZIEL — die Kaskadenschleife bucht die Herkunft in den
            // Zielspeicher um, statt sie dem Kessel gutzuschreiben.
            double ausQuelle = QuellwaermeHolen(i, ladung, stunde, sp);
            _restLeistung[i] -= (ladung - ausQuelle);
            _kesselStunde[i] += ladung - ausQuelle;

            // E-K1-1: siehe Stunde_Bedarf — nur die Gleitkomma-Klemmung.
            if (_restLeistung[i] < 0) _restLeistung[i] = 0;

            // N1 (Paket-5-Nacharbeit): Die Speicherladung getrennt mitführen. Sie bleibt
            // Teil der Nutzwärme (der Brennstoff dafür ist geflossen), darf aber nicht als
            // BEDARFSDECKUNG gelten — sonst meldet Tab_ErgebnisHeizkessel einen negativen
            // Restwärmebedarf und eine Deckungssumme über 100 % (gemessen an 1018/1023).
            //
            // D5a: Geführt wird der EIGENE Anteil. Die Ergebnisbildung zieht diese Größe
            // von der (ebenfalls brennstoffbasierten) Nutzwärme ab; stünde hier die volle
            // Ladung, würde die Differenz um die Quellwärme zu klein — bei reiner
            // Puffer-Hauptsenke sogar negativ.
            SpeicherladungGesamtKwh += ladung - ausQuelle;
            if (stunde >= 0 && stunde < 8760)
                Speicherladung_stuendlich[stunde] += ladung - ausQuelle;

            return ladung;
        }

        /// <summary>
        /// Brennstoffbilanz der Stunde — GENAU EINMAL je Stunde und Kessel (Konzept 6.5).
        ///
        /// Das ist die zentrale Bedingung der zweikanaligen Umstellung: Läuft der Kessel,
        /// folgt sein Verbrauch dem Wirkungsgrad, dazu je Start der Anfahrverlust (Etappe E4,
        /// Konzept Kesselkennlinie 4.2); steht er und ist er betriebsbereit
        /// (<see cref="IstBetriebsbereit"/>, #568), wird ihm der
        /// BEREITSCHAFTSVERLUST als Verbrauch aufgeschlagen. Würde diese Entscheidung je
        /// Kanal getroffen, fiele der Stillstandsverlust in einer Stunde zweimal an — der
        /// Jahresnutzungsgrad (Schritt 5) kippte entsprechend.
        ///
        /// Aufgerufen wird die Methode in Phase G, also nach Bedarfsdeckung, Ladephase und
        /// Nachentladung: Erst dann steht fest, was der Kessel in dieser Stunde insgesamt
        /// abgegeben hat.
        /// </summary>
        public void Stunde_Abschluss(int stunde)
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                double KesselLeistung = _kesselStunde[i];
                // Ein Rest unter dem Zahlenrand ist kein Lauf (KesselLaeuft).
                bool laeuft = KesselLaeuft(_kesselAbgabe[i]);

                bool oel = Kesselkennlinie.IstOel(Brennstoff_Art[i]);

                // η₁₀₀ nach Brennstoff, 0,90 für einen fehlenden Wert - dieselbe Funktion wie die Kurve des
                // Katalogeditors (Kesselkennlinie.Kurven).
                double eta100 = Kesselkennlinie.Nennwirkungsgrad(Kessel_Wirk_Gas_Spk[i], Kessel_Wirk_Oel_Spk[i], Brennstoff_Art[i]);

                // Konzept Kesselkennlinie 4.1 (Etappe E2): der Wirkungsgrad der Laststufe
                // β = Wärme/Nennleistung. Der Elektrokessel rechnet mit η₁₀₀ wie bisher; beim
                // Niedertemperaturkessel ohne eigenes η₃₀ ist η₃₀ = η₁₀₀ und die Kurve flach —
                // beide Wege sind Stunde für Stunde bitgleich zum festen Wirkungsgrad.
                double wirk = WirkungsgradDerStunde(i, KesselLeistung, eta100);

                double stuendlicherBrennstoffverbrauchKW;

                // D5a: „Läuft der Kessel?" entscheidet die ABGABE, nicht der
                // brennstoffbasierte Anteil. Ohne Quellpuffer sind beide gleich, und die
                // Verzweigung ist Wort für Wort die bisherige.
                if (laeuft)
                {
                    // Etappe E3 (Konzept 4.1 Punkte 3 bis 5): Der Brennwertkessel mit Kennlinie rechnet
                    // mit η_eff aus Laststufe UND Rücklauf der Stunde; die trockene Kurve η_tr trennt den
                    // Teillastanteil vom Kondensationsgewinn. Jeder andere Kessel: wirk wie in E2.
                    double wirkTrocken = wirk;
                    double ruecklauf = double.NaN;
                    if (_brennwertKennlinie[i])
                    {
                        ruecklauf = RuecklaufDerStunde(i, stunde, out Ruecklaufstufe stufe);

                        // PW1 Stufe 1 (c): Ging ein Teil der Wärme in den Prozesskanal, sieht dieser
                        // Anteil den Prozessrücklauf der Stunde - gewichtet mit der Abgabe der Stunde.
                        if (_prozessAbgabe[i] > 0 && Prozesstemperatur != null)
                        {
                            double rlProzess = Prozesstemperatur.Ruecklauf(stunde);
                            if (!double.IsNaN(rlProzess))
                            {
                                double anteil = _prozessAbgabe[i] / _kesselAbgabe[i];
                                ruecklauf = Prozesstemperatur.MischRuecklauf(anteil, rlProzess, ruecklauf);
                                _prozessRuecklaufStunden[i]++;
                                double w = Math.Min(_prozessAbgabe[i], _kesselAbgabe[i]);
                                _prozessRuecklaufSumme[i] += w * rlProzess;
                                _prozessRuecklaufGewicht[i] += w;
                            }
                        }
                        wirk = WirkungsgradBrennwert(i, KesselLeistung, eta100, ruecklauf, out wirkTrocken);
                        _ruecklaufStufen[i][(int)stufe]++;
                    }

                    // Kessel läuft -> Verbrauch über Wirkungsgrad (in dieser Stunde kein Stillstandsverlust)
                    double brennstoffKennlinie = KesselLeistung / wirk;

                    // Etappe E4 (Konzept 4.2): die Starts der Stunde - im Takt unter der Mindestleistung
                    // so viele, wie Mindestläufe die Wärme braucht, sonst einer nach einer
                    // Stillstandsstunde -, und je Start der Anfahrverlust. Der Elektrokessel taktet nicht.
                    int starts;
                    if (_takten[i] && Kesselkennlinie.Taktet(KesselLeistung, _mindestleistungKw[i]))
                    {
                        starts = Kesselkennlinie.StartsImTakt(KesselLeistung, _mindestleistungKw[i], _mindestlaufzeitMin[i]);
                        Taktstunden_Spk[i]++;
                    }
                    else
                    {
                        starts = _liefVorstunde[i] ? 0 : 1;
                    }
                    double anfahrverlust = _takten[i] ? starts * _anfahrverlustKwh[i] : 0;
                    Starts_Spk[i] += starts;
                    if (!_liefVorstunde[i]) Laufphasen_Spk[i]++;
                    Anfahrverlust_KWh_Spk[i] += anfahrverlust;

                    stuendlicherBrennstoffverbrauchKW = brennstoffKennlinie + anfahrverlust;

                    // E2: der Brennstoff der Laufstunde und sein Teillastanteil gegenüber η₁₀₀ - beim
                    // Brennwertkessel mit Kennlinie der trockenen Kurve, der Rest ist Brennwertnutzung (E3).
                    // Beides ohne den Anfahrverlust (E4), der für sich gezählt wird.
                    _waermeBetriebKwh[i] += KesselLeistung;
                    BrennstoffBetrieb_KWh_Spk[i] += brennstoffKennlinie;
                    if (_brennwertKennlinie[i])
                    {
                        double brennstoffTrocken = KesselLeistung / wirkTrocken;
                        TeillastMehrbrennstoff_KWh_Spk[i] += brennstoffTrocken - KesselLeistung / eta100;
                        BrennwertMehrbrennstoff_KWh_Spk[i] += brennstoffKennlinie - brennstoffTrocken;
                        _ruecklaufGewichtet[i] += KesselLeistung * ruecklauf;
                        if (_ruecklaufStunde[i] != null && stunde >= 0 && stunde < 8760)
                            _ruecklaufStunde[i][stunde] = ruecklauf;
                        if (Kesselkennlinie.Brennwertbetrieb(ruecklauf, Kesselkennlinie.Taupunkt(Brennstoff_Art[i])))
                        {
                            Brennwertstunden_Spk[i]++;
                            BrennwertWaerme_KWh_Spk[i] += KesselLeistung;
                        }
                    }
                    else
                    {
                        TeillastMehrbrennstoff_KWh_Spk[i] += brennstoffKennlinie - KesselLeistung / eta100;
                    }
                    if (_wirkungsgradStunde[i] != null && stunde >= 0 && stunde < 8760)
                        _wirkungsgradStunde[i][stunde] = wirk;

                    Laufstunden_Spk[i]++;
                    _letzteLaufstunde[i] = stunde;

                    if (oel)
                    {
                        s_waerme_Oel_Spk[i] += KesselLeistung;
                    }
                    else
                    {
                        s_waerme_Gas_Spk[i] += KesselLeistung;

                        // Die Gasspitze aus demselben Wert wie der Brennstoff der Stunde (Konzept 4.1
                        // Punkt 6), also samt Anfahrverlust.
                        double Gasleistung = stuendlicherBrennstoffverbrauchKW;
                        if (_gasspitzeKessel[i] < Gasleistung) _gasspitzeKessel[i] = Gasleistung;
                    }
                }
                else if (IstBetriebsbereit(_heiztage, stunde, _letzteLaufstunde[i]))
                {
                    // Kessel steht still, ist aber BETRIEBSBEREIT (Heiztag oder Nachlauf)
                    // -> Bereitschaftsverlust, EINMAL: die Bereitschaftsleistung [kW] über
                    // eine Stunde. Sie ist eine Leistung, kein Anteil der Nennleistung
                    // (BereitschaftsleistungKw).
                    stuendlicherBrennstoffverbrauchKW = Betriebsbereitschaft_Verluste[i];
                    Bereitschaftsstunden_Spk[i]++;
                    Bereitschaftsverlust_KWh_Spk[i] += stuendlicherBrennstoffverbrauchKW;
                }
                else
                {
                    // Kessel steht still und ist abgeschaltet (Tag über der Heizgrenze,
                    // Nachlauf abgelaufen): kein Bereitschaftsverlust.
                    stuendlicherBrennstoffverbrauchKW = 0;
                }

                _liefVorstunde[i] = laeuft;
                Kessel_Verbrauch_MWh_Spk[i] += stuendlicherBrennstoffverbrauchKW;

                if (stunde >= 0 && stunde < 8760)
                    Kesselleistung_stuendlich[stunde] += (double)KesselLeistung;
            }
        }

        /// <summary>
        /// Der Wirkungsgrad des Kessels <paramref name="i"/> in einer Stunde mit der
        /// brennstoffbasierten Wärme <paramref name="waermeKwh"/> (Konzept Kesselkennlinie 4.1):
        /// <see cref="Kesselkennlinie.Eta"/> bei der Laststufe der Stunde, mit dem wirksamen η₃₀
        /// zu <paramref name="eta100"/>. Der Elektrokessel rechnet mit <paramref name="eta100"/>.
        /// </summary>
        private double WirkungsgradDerStunde(int i, double waermeKwh, double eta100)
        {
            if (!Kesselkennlinie.RechnetMitKennlinie(Brennstoff_Art[i])) return eta100;
            double eta30 = Kesselkennlinie.Eta30Wirksam(_eta30Gepflegt[i], eta100, _bauart[i], Brennstoff_Art[i]);
            return Kesselkennlinie.Eta(Kesselkennlinie.Laststufe(waermeKwh, Kessel_Leistung_Spk[i]), eta100, eta30);
        }

        /// <summary>
        /// Der Wirkungsgrad eines Kessels MIT Brennwertkennlinie in einer Laufstunde (Konzept 4.1
        /// Punkte 3 bis 5, Etappe E3): <see cref="Kesselkennlinie.EtaBrennwert"/> bei Laststufe und
        /// Rücklauf der Stunde, mit dem wirksamen η₃₀ (gepflegt oder Normvorgabe) zu
        /// <paramref name="eta100"/>. <paramref name="wirkTrocken"/> ist η_tr(β).
        /// </summary>
        private double WirkungsgradBrennwert(int i, double waermeKwh, double eta100, double ruecklaufC,
                                             out double wirkTrocken)
        {
            double eta30 = Kesselkennlinie.Eta30Wirksam(_eta30Gepflegt[i], eta100, _bauart[i], Brennstoff_Art[i]);
            return Kesselkennlinie.EtaBrennwert(Kesselkennlinie.Laststufe(waermeKwh, Kessel_Leistung_Spk[i]),
                                                eta100, eta30, ruecklaufC, Brennstoff_Art[i], out wirkTrocken);
        }

        /// <summary>
        /// Der RÜCKLAUF einer Stunde für den Kessel <paramref name="i"/> nach der Kette
        /// (<see cref="Kesselkennlinie.Ruecklauf"/>): Heizkreis der Anlagenkopplung, Senkenspeicher (in
        /// <see cref="Stunde_Start"/> gelesen), gepflegtes Paar, Rückfall 50 °C.
        /// </summary>
        private double RuecklaufDerStunde(int i, int stunde, out Ruecklaufstufe stufe)
        {
            double heizkreis = (Heizkreisruecklauf != null && stunde >= 0 && stunde < Heizkreisruecklauf.Length)
                ? Heizkreisruecklauf[stunde] : double.NaN;
            return Kesselkennlinie.Ruecklauf(heizkreis, _speicherRuecklauf[i], _ruecklaufPaar[i], out stufe);
        }

        /// <summary>
        /// Setzt den SENKENSPEICHER eines Kessels mit Brennwertkennlinie — Stufe (b) der Rücklaufkette
        /// (Etappe E3). Aufgerufen von <c>SimulationControl</c>, nachdem die Speicher-Registry offen ist,
        /// mit dem ersten Puffer der Senkenliste in Rangfolge. Ohne Brennwertkennlinie wirkungslos.
        /// </summary>
        public void RuecklaufSpeicherSetzen(int index, SimulationPufferspeicher speicher)
        {
            if (index < 0 || index >= MAX_SPK || !_brennwertKennlinie[index]) return;
            _ruecklaufSpeicher[index] = speicher;
        }

        /// <summary>Rechnet der Kessel <paramref name="index"/> mit der Brennwertkennlinie (Etappe E3)?</summary>
        public bool RechnetMitBrennwertkennlinie(int index)
            => index >= 0 && index < MAX_SPK && _brennwertKennlinie[index];

        /// <summary>
        /// Der MITTLERE RÜCKLAUF der Laufstunden des Kessels <paramref name="index"/> [°C], wärmegewichtet;
        /// NaN ohne Brennwertkennlinie oder ohne Laufstunde.
        /// </summary>
        public double RuecklaufMittel(int index)
        {
            if (!RechnetMitBrennwertkennlinie(index)) return double.NaN;
            double w = _waermeBetriebKwh[index];
            return w > 0 ? _ruecklaufGewichtet[index] / w : double.NaN;
        }

        /// <summary>
        /// Die Stundenreihe des Rücklaufs des Kessels <paramref name="index"/> [°C; in Stillstandsstunden
        /// 0] — nur mit Brennwertkennlinie, sonst <c>null</c>. Lesezugriff für den Zeitreihen-Export.
        /// </summary>
        public double[] RuecklaufStunden(int index)
            => RechnetMitBrennwertkennlinie(index) ? _ruecklaufStunde[index] : null;

        /// <summary>
        /// Laufstunden des Kessels <paramref name="index"/> je Stufe der Rücklaufkette; 0 ohne
        /// Brennwertkennlinie.
        /// </summary>
        public int RuecklaufStufenstunden(int index, Ruecklaufstufe stufe)
            => RechnetMitBrennwertkennlinie(index) && _ruecklaufStufen[index] != null ? _ruecklaufStufen[index][(int)stufe] : 0;

        /// <summary>Laufprotokoll beim Aufbau: Stützwerte der Brennwertkennlinie eines Kessels.</summary>
        private void BrennwertkennlinieMelden(int i)
        {
            int art = Brennstoff_Art[i];
            SimulationProtokoll.Aktuell.HinweisEinmal(
                "KESSEL_BRENNWERTKENNLINIE_" + spk_list[i],
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KESSEL_BRENNWERTKENNLINIE,
                    spk_list[i],
                    Kesselkennlinie.Taupunkt(art).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture),
                    Kesselkennlinie.Kondensationsgewinn30(art).ToString("N2", System.Globalization.CultureInfo.CurrentCulture),
                    Kesselkennlinie.Eta30Trocken(Teillastwirkungsgrad(i), art).ToString("N3", System.Globalization.CultureInfo.CurrentCulture),
                    Kesselkennlinie.HsHi(art).ToString("N2", System.Globalization.CultureInfo.CurrentCulture),
                    (_ruecklaufPaar[i].HasValue ? _ruecklaufPaar[i].Value : Kesselkennlinie.RUECKLAUF_RUECKFALL_C)
                        .ToString("0.#", System.Globalization.CultureInfo.CurrentCulture)));
        }

        /// <summary>
        /// Laufprotokoll am Jahresende: woher der Rücklauf der Laufstunden kam, sein Mittel und der
        /// Brennwertbetrieb; dazu die Kohärenzzeile (Konzept 5), wenn der Rücklauf in mindestens der
        /// Hälfte der Betriebsstunden über dem Taupunkt lag.
        /// </summary>
        private void BrennwertBetriebMelden(int i)
        {
            if (!_brennwertKennlinie[i] || Laufstunden_Spk[i] <= 0) return;
            var k = System.Globalization.CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.Hinweis(
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(k,
                    MyResource.Resource.SIMENG_KESSEL_BRENNWERT_BETRIEB,
                    spk_list[i],
                    RuecklaufStufenstunden(i, Ruecklaufstufe.Heizkreis),
                    RuecklaufStufenstunden(i, Ruecklaufstufe.Speicher),
                    RuecklaufStufenstunden(i, Ruecklaufstufe.Paar),
                    RuecklaufStufenstunden(i, Ruecklaufstufe.Rueckfall),
                    RuecklaufMittel(i).ToString("0.0", k),
                    Brennwertstunden_Spk[i], Laufstunden_Spk[i]));

            // Ganzzahlig verglichen (Stunden über dem Taupunkt ≥ die Hälfte der Laufstunden) - die
            // Kohärenzzeile kippt so nicht an der Rundung eines Quotienten.
            int stundenUeber = Laufstunden_Spk[i] - Brennwertstunden_Spk[i];
            double ueber = stundenUeber / (double)Laufstunden_Spk[i];
            if (stundenUeber * 2 >= Laufstunden_Spk[i])
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "KESSEL_BRENNWERT_UEBER_TAUPUNKT_" + spk_list[i],
                    MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(k,
                        MyResource.Resource.SIMENG_KESSEL_BRENNWERT_UEBER_TAUPUNKT,
                        spk_list[i], (ueber * 100.0).ToString("0", k),
                        Kesselkennlinie.Taupunkt(Brennstoff_Art[i]).ToString("0.#", k)));
        }

        /// <summary>
        /// Der Wirkungsgrad bei Nennlast η₁₀₀, mit dem der Kessel <paramref name="index"/> rechnet
        /// (Faktor, Öl- oder Gasfeld nach Brennstoff, 0,90 für einen fehlenden Wert); 0 außerhalb.
        /// </summary>
        public double Nennwirkungsgrad(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            return Kesselkennlinie.Nennwirkungsgrad(Kessel_Wirk_Gas_Spk[index], Kessel_Wirk_Oel_Spk[index], Brennstoff_Art[index]);
        }

        /// <summary>
        /// Das wirksame η₃₀ des Kessels <paramref name="index"/> (Faktor) — gepflegt oder die
        /// Normvorgabe nach Bauart; beim Elektrokessel η₁₀₀ (keine Kennlinie). 0 außerhalb.
        /// </summary>
        public double Teillastwirkungsgrad(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            double eta100 = Nennwirkungsgrad(index);
            if (!Kesselkennlinie.RechnetMitKennlinie(Brennstoff_Art[index])) return eta100;
            return Kesselkennlinie.Eta30Wirksam(_eta30Gepflegt[index], eta100, _bauart[index], Brennstoff_Art[index]);
        }

        /// <summary>
        /// Rechnet der Kessel <paramref name="index"/> mit der NORMVORGABE für η₃₀ (Feld leer,
        /// Konzept 7.1)? Der Elektrokessel nicht — er hat keine Kennlinie.
        /// </summary>
        public bool TeillastwirkungsgradIstVorgabe(int index)
        {
            if (index < 0 || index >= MAX_SPK || IstStromkessel(index)) return false;
            return !Kesselkennlinie.Eta30IstGepflegt(KesselKennlinieWerte.AlsFaktor(_eta30Gepflegt[index]));
        }

        /// <summary>Die Bauart des Kessels <paramref name="index"/> für die Normvorgabe.</summary>
        public KesselBauart Bauart(int index)
            => index >= 0 && index < MAX_SPK ? _bauart[index] : KesselBauart.Niedertemperatur;

        /// <summary>Der Anzeigetext einer Bauart (Laufprotokoll, Ergebnisreiter).</summary>
        public static string BauartText(KesselBauart bauart)
        {
            switch (bauart)
            {
                case KesselBauart.Brennwert: return MyResource.Resource.KESSEL_BAUART_BRENNWERT;
                case KesselBauart.Standard: return MyResource.Resource.KESSEL_BAUART_STANDARD;
                default: return MyResource.Resource.KESSEL_BAUART_NIEDERTEMPERATUR;
            }
        }

        /// <summary>
        /// Der MITTLERE WIRKUNGSGRAD IM BETRIEB des Kessels <paramref name="index"/> (Faktor):
        /// Wärme der Laufstunden durch ihren Brennstoff, also η(β) wärmegewichtet über das Jahr —
        /// ohne Bereitschaftsverlust, anders als der Jahresnutzungsgrad. 0 ohne Laufstunde.
        /// </summary>
        public double WirkungsgradBetrieb(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            double b = BrennstoffBetrieb_KWh_Spk[index];
            return b > 0 ? _waermeBetriebKwh[index] / b : 0;
        }

        /// <summary>
        /// Die Stundenreihe des Wirkungsgrads des Kessels <paramref name="index"/> (Faktor; in
        /// Stillstandsstunden 0) — Lesezugriff für den Zeitreihen-Export; <c>null</c> außerhalb.
        /// </summary>
        public double[] WirkungsgradStunden(int index)
            => index >= 0 && index < MAX_SPK ? _wirkungsgradStunde[index] : null;

        /// <summary>
        /// Die brennstoffbasierte WÄRME DER LAUFSTUNDEN des Kessels <paramref name="index"/>
        /// [kWh/a] — der Zähler von <see cref="WirkungsgradBetrieb"/>. 0 außerhalb.
        /// </summary>
        public double WaermeBetriebKwh(int index)
            => index >= 0 && index < MAX_SPK ? _waermeBetriebKwh[index] : 0;

        /// <summary>
        /// Die MITTLERE LASTSTUFE IM BETRIEB des Kessels <paramref name="index"/> (0 … 1):
        /// Wärme der Laufstunden durch Laufstunden mal Nennleistung. 0 ohne Laufstunde.
        /// </summary>
        public double LaststufeMittel(int index)
        {
            if (index < 0 || index >= MAX_SPK) return 0;
            double nenn = Kessel_Leistung_Spk[index] * Laufstunden_Spk[index];
            return nenn > 0 ? _waermeBetriebKwh[index] / nenn : 0;
        }

        /// <summary>Rechnet der Kessel <paramref name="index"/> das Takten (Etappe E4)? Jeder Brennstoffkessel.</summary>
        public bool RechnetMitTakten(int index)
            => index >= 0 && index < MAX_SPK && _takten[index];

        /// <summary>Die wirksame MINDESTLEISTUNG des Kessels <paramref name="index"/> [kW]; 0 ohne Taktmodell.</summary>
        public double Mindestleistung(int index)
            => RechnetMitTakten(index) ? _mindestleistungKw[index] : 0;

        /// <summary>Der wirksame ANFAHRVERLUST je Start des Kessels <paramref name="index"/> [kWh]; 0 ohne Taktmodell.</summary>
        public double AnfahrverlustJeStart(int index)
            => RechnetMitTakten(index) ? _anfahrverlustKwh[index] : 0;

        /// <summary>Die wirksame MINDESTLAUFZEIT des Kessels <paramref name="index"/> [min]; 0 ohne Taktmodell.</summary>
        public int Mindestlaufzeit(int index)
            => RechnetMitTakten(index) ? _mindestlaufzeitMin[index] : 0;

        /// <summary>Rechnet der Kessel mit der Normvorgabe der Mindestleistung (Feld leer, Konzept 7.1)?</summary>
        public bool MindestleistungIstVorgabe(int index)
            => RechnetMitTakten(index) && !Kesselkennlinie.MindestleistungIstGepflegt(_mindestleistungGepflegt[index]);

        /// <summary>Rechnet der Kessel mit der Normvorgabe des Anfahrverlusts (Feld leer, Konzept 7.1)?</summary>
        public bool AnfahrverlustIstVorgabe(int index)
            => RechnetMitTakten(index) && !Kesselkennlinie.AnfahrverlustIstGepflegt(_anfahrverlustGepflegt[index]);

        /// <summary>Rechnet der Kessel mit der Normvorgabe der Mindestlaufzeit (Feld leer, Konzept 7.1)?</summary>
        public bool MindestlaufzeitIstVorgabe(int index)
            => RechnetMitTakten(index) && !Kesselkennlinie.MindestlaufzeitIstGepflegt(_mindestlaufzeitGepflegt[index]);

        /// <summary>
        /// Laufprotokoll am Jahresende: Starts, Laufphasen, Taktstunden und Anfahrverlust eines Kessels
        /// mit Taktmodell (Etappe E4).
        /// </summary>
        private void TaktenMelden(int i)
        {
            if (!_takten[i] || Laufstunden_Spk[i] <= 0) return;
            var k = System.Globalization.CultureInfo.CurrentCulture;
            SimulationProtokoll.Aktuell.Hinweis(
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(k,
                    MyResource.Resource.SIMENG_KESSEL_TAKTEN_BETRIEB,
                    spk_list[i], Starts_Spk[i], Laufphasen_Spk[i], Taktstunden_Spk[i],
                    Anfahrverlust_KWh_Spk[i]));
        }

        /// <summary>Jahressummen, Emissionen und Jahresnutzungsgrad des zweikanaligen Wegs.</summary>
        public void Abschluss_Zweikanalig()
        {
            for (int i = 0; i < _anzahlZweikanalig; i++)
            {
                BereitschaftDeckeln(i);
                BrennwertBetriebMelden(i);
                TaktenMelden(i);

                s_waerme_Gas_Spk[i] /= 1000;
                s_waerme_Oel_Spk[i] /= 1000;
                Kessel_Verbrauch_MWh_Spk[i] /= 1000;
                Gasspitze_Spk += _gasspitzeKessel[i];
            }

            WaermebedarfGesamtMwh = 0;
            Array.ForEach(Waermebedarf, value => WaermebedarfGesamtMwh += value);
            WaermebedarfGesamtMwh /= 1000;

            Bilanz_und_Nutzungsgrad(_anzahlZweikanalig);
        }

        /// <summary>
        /// Zweikanalige Stufe OHNE Speicherbeteiligung: dieselben Stundenschritte in einer
        /// eigenen Jahresschleife an der Kaskadenposition des Heizkessels.
        ///
        /// Der Weg für Projekte, in denen kein Kessel eine Puffer-Senke trägt. Ohne
        /// Speicher haben die Phasen A, C, D und E für diese Stufe keinen Inhalt; Phase G
        /// beschränkt sich auf die Brennstoffbilanz der Stunde. Gegenüber der bis Paket A1
        /// ändert sich allein die Kanalführung — die je Stunde und Kessel abgegebene
        /// Nutzwärme, der Brennstoffverbrauch und die Restwärme sind dieselben Zahlen.
        /// </summary>
        public bool Berechnung_Zweikanalig(int ID_Projekt, Kanalsatz kanaele,
                                           List<Senkenliste> senken)
        {
            if (kanaele == null) return false;
            if (!Vorbereiten_Zweikanalig(ID_Projekt, senken)) return false;

            double[] rest = new double[Kanal.ANZAHL];

            for (int stunde = 0; stunde < 8760; stunde++)
            {
                // Kuehlkonzept 4.2 (F-K4): nur die Waermekanaele - der Kuehlkanal bleibt in
                // rest[] 0 und im Kanalsatz unberuehrt; kein Waermeerzeuger sieht Kaeltebedarf.
                foreach (int k in Kanal.KANAELE_WAERME) rest[k] = kanaele.Bedarf[k][stunde];

                // Ohne Speicher gibt es keine Vorabentladung: Der Stufeneingang ist der
                // Kanalstand an dieser Kaskadenposition.
                Stunde_Start(stunde, rest);
                Stunde_Bedarf(stunde, rest);
                Stunde_Abschluss(stunde);

                foreach (int k in Kanal.KANAELE_WAERME)
                    kanaele.Bedarf[k][stunde] = (double)rest[k];
            }

            Abschluss_Zweikanalig();
            return true;
        }

        // ===================================================================
        // Betriebsbereitschaft (#568)
        // ===================================================================

        /// <summary>
        /// LÄUFT der Kessel in dieser Stunde? Er läuft, wenn seine Abgabe den Zahlenrand
        /// <see cref="Rechenrand.ABSOLUT"/> erreicht; ein Rest darunter ist kein Lauf — keine
        /// Laufstunde, kein Start, und die Stunde bleibt eine Stillstands- oder
        /// Bereitschaftsstunde.
        ///
        /// <para><b>Warum</b> (Plattformbefund PB‑1, Anwenderentscheid vom 29.09.2026 zum
        /// Nachzug): Deckt der Kessel nur den Rest einer Vorstufe, sind das 10⁻¹⁶ kWh — ein ulp
        /// des Bedarfs, den die Stufe davor fast ganz gedeckt hat. Ob dieser Rest 0 ist oder
        /// nicht, hängt am letzten Bit von <c>Math.Exp</c>/<c>Math.Sin</c> weiter vorn, und das
        /// rundet Windows anders als Linux: In Projekt 1024 zählte der blanke Vergleich
        /// <c>&gt; 0</c> je Plattform andere Stunden als Laufstunden (Windows 2577, Linux 5204).
        /// Derselbe Rand wie am Quellspeicher (<see cref="SimulationWaermepumpe.QuellInhalt"/>).</para>
        /// </summary>
        internal static bool KesselLaeuft(double abgabe)
        {
            return abgabe >= Rechenrand.ABSOLUT;
        }

        /// <summary>
        /// DIE STUNDENREGEL der Betriebsbereitschaft: Ein stillstehender Kessel ist in der
        /// Stunde <paramref name="stunde"/> betriebsbereit — und trägt dann seinen
        /// Bereitschaftsverlust —, wenn
        /// <list type="number">
        /// <item>der Tag der Stunde ein HEIZTAG ist (<see cref="HeiztageAus"/>: Tagesmittel der
        /// Außentemperatur unter der Heizgrenze) oder</item>
        /// <item>der Kessel in den <see cref="BEREITSCHAFT_NACHLAUF_STUNDEN"/> Stunden davor
        /// gelaufen ist (Nachlauf).</item>
        /// </list>
        /// An einem Tag über der Heizgrenze und nach Ablauf des Nachlaufs ist er abgeschaltet und
        /// verliert nichts. <paramref name="heiztage"/> <c>null</c> = jeder Tag ist Heiztag.
        /// </summary>
        internal static bool IstBetriebsbereit(bool[] heiztage, int stunde, int letzteLaufstunde)
        {
            int tag = stunde / 24;
            if (heiztage == null || tag < 0 || tag >= heiztage.Length || heiztage[tag]) return true;
            return letzteLaufstunde != int.MinValue &&
                   stunde - letzteLaufstunde <= BEREITSCHAFT_NACHLAUF_STUNDEN;
        }

        /// <summary>
        /// Die HEIZTAGE des Jahres aus der stündlichen Außentemperatur [°C]: ein Tag, dessen
        /// Mittel über seine 24 Stunden UNTER der <paramref name="heizgrenze"/> liegt — genau
        /// auf der Grenze ist kein Heiztag. Tage statt Stunden, weil ein Kessel zwischen einer
        /// kalten Nacht und einem warmen Mittag nicht abkühlt. Eine Regel für jedes
        /// Gebäudemodell und für Projekte ohne Gebäude.
        ///
        /// <para><b>Der Vergleich trägt den Zahlenrand</b> (<see cref="Rechenrand.SchwelleErreicht"/>):
        /// Tagesmittel und Heizgrenze stammen aus getrennten Rechenketten, und ein Mittel, das
        /// dezimal genau auf der Grenze liegt, darf nicht an der Rundung der Summe kippen.</para>
        ///
        /// <para><c>null</c> ohne Reihe (dann ist jeder Tag Heiztag); ein Tag, dessen Stunden die
        /// Reihe nicht vollständig trägt, gilt ebenso als Heiztag.</para>
        /// </summary>
        internal static bool[] HeiztageAus(double[] aussentemperatur, double heizgrenze)
        {
            if (aussentemperatur == null) return null;

            bool[] tage = new bool[365];
            for (int tag = 0; tag < 365; tag++)
            {
                int beginn = tag * 24;
                if (beginn + 24 > aussentemperatur.Length) { tage[tag] = true; continue; }

                double summe = 0;
                for (int h = beginn; h < beginn + 24; h++) summe += aussentemperatur[h];
                double mittel = summe / 24.0;
                tage[tag] = !Rechenrand.SchwelleErreicht(mittel, heizgrenze);
            }
            return tage;
        }

        /// <summary>
        /// Die WIRKSAME Heizgrenze [°C]: der Wert des Projekts, ohne ihn — oder bei einem Wert,
        /// der keine endliche Zahl ist — die Vorgabe <see cref="HEIZGRENZE_VORGABE_C"/>. Die
        /// Plausibilitätsgrenzen hält die Oberfläche; der Rechenweg nimmt jede endliche Zahl.
        /// </summary>
        public static double HeizgrenzeWirksam(double? heizgrenze)
        {
            if (!heizgrenze.HasValue || double.IsNaN(heizgrenze.Value) || double.IsInfinity(heizgrenze.Value))
                return HEIZGRENZE_VORGABE_C;
            return heizgrenze.Value;
        }

        /// <summary>
        /// DECKEL DER VORGABE: Ist <see cref="Vorgabe_Betriebsbereitschaft"/> größer 0, darf
        /// ein Kessel höchstens <c>Vorgabe − Laufstunden</c> Bereitschaftsstunden tragen; den
        /// Überhang nimmt der Deckel samt seinem Verbrauch zurück und meldet ihn. Ohne
        /// Kalenderwillkür: Die Bereitschaftsleistung ist je Kessel fest, der Überhang ist
        /// also Stundenzahl mal Leistung, gleich wo im Jahr er liegt. Aufruf vor der
        /// Umrechnung in MWh. Danach nennt das Laufprotokoll je Kessel Laufstunden, Starts,
        /// Bereitschaftsstunden und Bereitschaftsverlust.
        /// </summary>
        private void BereitschaftDeckeln(int i)
        {
            int erlaubt = BereitschaftsstundenGedeckelt(Vorgabe_Betriebsbereitschaft,
                                                        Laufstunden_Spk[i], Bereitschaftsstunden_Spk[i]);
            int ueberhang = Bereitschaftsstunden_Spk[i] - erlaubt;
            if (ueberhang <= 0)
            {
                BereitschaftMelden(i);
                return;
            }

            double zuviel = ueberhang * Betriebsbereitschaft_Verluste[i];
            Kessel_Verbrauch_MWh_Spk[i] -= zuviel;
            Bereitschaftsverlust_KWh_Spk[i] -= zuviel;
            Bereitschaftsstunden_Spk[i] = erlaubt;

            SimulationProtokoll.Aktuell.Hinweis(
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_GEDECKELT,
                    spk_list[i], Vorgabe_Betriebsbereitschaft, Laufstunden_Spk[i],
                    Bereitschaftsstunden_Spk[i] + ueberhang, erlaubt));
            BereitschaftMelden(i);
        }

        /// <summary>
        /// Die Bereitschaftsstunden nach dem Deckel: <paramref name="vorgabe"/> ≤ 0 lässt sie
        /// stehen, sonst höchstens <c>vorgabe − laufstunden</c> (nicht unter 0).
        /// </summary>
        internal static int BereitschaftsstundenGedeckelt(int vorgabe, int laufstunden,
                                                         int bereitschaftsstunden)
        {
            if (vorgabe <= 0) return bereitschaftsstunden;
            return Math.Min(bereitschaftsstunden, Math.Max(0, vorgabe - laufstunden));
        }

        /// <summary>
        /// Laufprotokoll je Kessel: Laufstunden, Starts, betriebsbereite Stillstandsstunden
        /// und der Bereitschaftsverlust [kWh/a] nach dem Deckel — die Zahlen, auf denen der
        /// Jahresnutzungsgrad steht.
        /// </summary>
        private void BereitschaftMelden(int i)
        {
            SimulationProtokoll.Aktuell.Hinweis(
                MyResource.Resource.SIMENG_PRAEFIX_HEIZKESSEL + string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KESSEL_BEREITSCHAFT_STUNDEN,
                    spk_list[i], Laufstunden_Spk[i], Starts_Spk[i],
                    Bereitschaftsstunden_Spk[i], Bereitschaftsverlust_KWh_Spk[i]));
        }

        public double[] AddVectors(double[] array1, double[] array2)
        {
            if (array1.Length != array2.Length)
                throw new ArgumentException("Arrays müssen die gleiche Länge aufweisen.");

            double[] result = new double[array1.Length];
            for (int i = 0; i < array1.Length; i++) { result[i] = array1[i] + array2[i]; }
            return result;
        }

        public void Init()
        {
            // N8 (Paket-5-Nacharbeit): Die Kesselzahl des zweikanaligen Wegs gehört zum
            // Zustand dieses Moduls und muss deshalb HIER zurückgesetzt werden. Bisher
            // stand sie nur am Ende von Vorbereiten_Zweikanalig - bricht das mittendrin
            // ab (Kessel nicht im Projekt), stünde der Wert des Vorlaufs neben einer
            // bereits geleerten _kesselSenke, und die Stundenschritte liefen über
            // Kessel, die es in diesem Lauf nicht gibt.
            _anzahlZweikanalig = 0;

            // Speichergrößen (Paket 5 / Nacharbeit N1, N2): Ohne Puffer-Senke bleiben diese
            // Größen auf 0, damit die Ergebnisbildung in SimulationRunner dort
            // nachweislich bitgleich der bisherigen ist.
            Array.Clear(Speicherladung_stuendlich, 0, Speicherladung_stuendlich.Length);
            SpeicherladungGesamtKwh = 0;
            Speicherentladung_Anteil = 0;
            SpeicherentladungAndere_Kwh = 0;

            // #568: Betriebsbereitschaft - Zähler und Laufgedächtnis sind Laufzustand;
            // Heizgrenze und Heiztage bildet Vorbereiten_Zweikanalig aus der Außentemperatur neu.
            _heiztage = null;
            Heizgrenze_C = HEIZGRENZE_VORGABE_C;
            Heiztage_Anzahl = 365;
            Array.Clear(Laufstunden_Spk, 0, MAX_SPK);
            Array.Clear(Starts_Spk, 0, MAX_SPK);
            Array.Clear(Bereitschaftsstunden_Spk, 0, MAX_SPK);
            Array.Clear(Bereitschaftsverlust_KWh_Spk, 0, MAX_SPK);
            Array.Clear(_liefVorstunde, 0, MAX_SPK);

            // Kesselkennlinie (Etappe E2): Kennlinienwerte und Mitschrift sind Laufzustand.
            Array.Clear(_eta30Gepflegt, 0, MAX_SPK);
            Array.Clear(_bauart, 0, MAX_SPK);
            Array.Clear(_waermeBetriebKwh, 0, MAX_SPK);
            Array.Clear(BrennstoffBetrieb_KWh_Spk, 0, MAX_SPK);
            Array.Clear(TeillastMehrbrennstoff_KWh_Spk, 0, MAX_SPK);
            Array.Clear(_wirkungsgradStunde, 0, MAX_SPK);
            for (int j = 0; j < MAX_SPK; j++) _letzteLaufstunde[j] = int.MinValue;

            // Brennwertkennlinie (Etappe E3): Schalter, Rücklaufkette und Mitschrift sind Laufzustand;
            // Heizkreisruecklauf und RuecklaufPaarLesen sind Eingang und bleiben.
            Array.Clear(_brennwertKennlinie, 0, MAX_SPK);
            Array.Clear(_ruecklaufPaar, 0, MAX_SPK);
            Array.Clear(_ruecklaufSpeicher, 0, MAX_SPK);
            for (int j = 0; j < MAX_SPK; j++) _speicherRuecklauf[j] = double.NaN;
            Array.Clear(_ruecklaufStunde, 0, MAX_SPK);
            Array.Clear(_ruecklaufStufen, 0, MAX_SPK);
            Array.Clear(_ruecklaufGewichtet, 0, MAX_SPK);
            Array.Clear(Brennwertstunden_Spk, 0, MAX_SPK);
            Array.Clear(BrennwertWaerme_KWh_Spk, 0, MAX_SPK);
            Array.Clear(BrennwertMehrbrennstoff_KWh_Spk, 0, MAX_SPK);

            // Takten (Etappe E4): Taktwerte und Zähler sind Laufzustand.
            Array.Clear(_takten, 0, MAX_SPK);
            Array.Clear(_mindestleistungKw, 0, MAX_SPK);
            Array.Clear(_anfahrverlustKwh, 0, MAX_SPK);
            Array.Clear(_mindestlaufzeitMin, 0, MAX_SPK);
            Array.Clear(_mindestleistungGepflegt, 0, MAX_SPK);
            Array.Clear(_anfahrverlustGepflegt, 0, MAX_SPK);
            Array.Clear(_mindestlaufzeitGepflegt, 0, MAX_SPK);
            Array.Clear(Laufphasen_Spk, 0, MAX_SPK);
            Array.Clear(Taktstunden_Spk, 0, MAX_SPK);
            Array.Clear(Anfahrverlust_KWh_Spk, 0, MAX_SPK);

            // K2: die Kanalaufschlüsselung derselben Größen (Konzept 4.4).
            Array.Clear(Direktdeckung_Kanal, 0, Kanal.ANZAHL);
            Array.Clear(Speicherentladung_Kanal, 0, Kanal.ANZAHL);

            // E2: und ihre Ganglinienfassung, an derselben Stelle.
            Direktdeckung_KanalStuendlich.Nullen();
            Speicherentladung_KanalStuendlich.Nullen();
            SpeicherentladungAndere_KanalStuendlich.Nullen();

            // D5a: Der Quellbezug gehört zum Laufzustand. ModulEbenen/AktiveEbene setzt
            // die Kaskadenschleife je Lauf neu; die Quellpuffer setzt SimulationControl,
            // nachdem die Registry offen ist.
            Array.Clear(Quellwaerme_stuendlich, 0, Quellwaerme_stuendlich.Length);
            QuellwaermeGesamtKwh = 0;
            Quellentnahmen.Clear();
            Array.Clear(_quellSpeicher, 0, _quellSpeicher.Length);
            Array.Clear(_quellAnteil, 0, _quellAnteil.Length);
            Array.Clear(_kesselAbgabe, 0, _kesselAbgabe.Length);

            // PAKET B1: Die Temperaturkopplung gehört aus demselben Grund zum
            // Laufzustand - sie wird beim Aufbau der Quellbezüge neu gesetzt.
            Array.Clear(_quellKopplung, 0, _quellKopplung.Length);
            Array.Clear(_quellVorlauf, 0, _quellVorlauf.Length);
            Array.Clear(_quellRuecklauf, 0, _quellRuecklauf.Length);
            Array.Clear(_quellTemperatur, 0, _quellTemperatur.Length);
            Array.Clear(_quellZuKalt, 0, _quellZuKalt.Length);
            ModulEbenen = null;
            AktiveEbene = 0;

            Maximale_Kesselleistung_Spk = 0;
            StromverbrauchSpkMwh = 0;

            for (int j = 0; j < MAX_SPK; j++)
            {
                s_waerme_Gas_Spk[j] = 0;
                s_waerme_Oel_Spk[j] = 0;
                Kessel_Wirk_Gas_Spk[j] = 0;
                Kessel_Wirk_Oel_Spk[j] = 0;
                Betriebsbereitschaft_Verluste[j] = 0;
                Kessel_Name[j] = "";
                Brennstoff_Betrieb_Spk[j] = 0;
                Kessel_Leistung_Spk[j] = 0;
                Kessel_Verbrauch_MWh_Spk[j] = 0;
                Kessel_Jahresnutzungsgrad_Spk[j] = 0;

                CO2_SPK[j] = 0;
                CO_SPK[j] = 0;
                CO2_SPK[j] = 0;
                SO2_SPK[j] = 0;
                NOX_SPK[j] = 0;
                Staub_SPK[j] = 0;
            }

            BruttoWaermeSpkErzeugungMwh = 0;
            SWaermeSpkMwh = 0;
            GasverbrauchSpkMwh = 0;
            OelverbrauchSpkMwh = 0;
            RapsoelverbrauchSpkMwh = 0;
            HolzverbrauchSpkMwh = 0;
            SonstigverbrauchSpkMwh = 0;
            StromverbrauchSpkMwh = 0;
            KohleSpkMwh = 0;
            KoksSpkMwh = 0;
            PelletsSpkMwh = 0;
            TierischeFetteSpkMwh = 0;

            Em_CO2_SPK = 0;
            Em_CO_SPK = 0;
            Em_SO2_SPK = 0;
            Em_NOX_SPK = 0;
            Em_Staub_SPK = 0;

            Gasspitze_Spk = 0;

            Array.Clear(Restwaerme, 0, Restwaerme.Length);
            Array.Clear(Stromverbrauch_stuendlich, 0, Stromverbrauch_stuendlich.Length);
            Array.Clear(Kesselleistung_stuendlich, 0, Kesselleistung_stuendlich.Length);
        }
    }
}