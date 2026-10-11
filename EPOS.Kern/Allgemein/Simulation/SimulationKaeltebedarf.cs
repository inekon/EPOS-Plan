using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Fassade der Kälteseite</b> (Stufe KU1 der Kühlung; Entscheid E21, Kühlkonzept
    /// 3.7, 4.2, 4.4, 6.4) — das Gegenstück zu <see cref="SimulationWaermebedarf"/>, mit denselben
    /// Mustern und denselben Namensbildungen, nur auf der anderen Seite des Kanalfelds.
    ///
    /// <para><b>Sie rechnet das Gebäude nicht, sie verteilt</b> (E21, „Ein Lauf, zwei Reihen").
    /// Die Stundenschleife des Moduls <c>Gebaeude/</c> liefert je Gebäude Heizlast UND Kühlreihe
    /// in EINEM Lauf (<see cref="GebaeudeModellErgebnis.KuehlbedarfKwh"/>); diese Fassade bucht die
    /// Kühlreihe der Gebäude mit wirksamer Kühlung und die externen Lastgänge mit dem Kanal
    /// „Kuehlung" (K3) in den Kühlkanal des EINEN Kanalsatzes (<see cref="Kanal.KUEHLUNG"/>) und
    /// bildet daraus Summe, Spitze, Monatswerte und die eigene Dauerlinie. Das Kanalfeld lebt in
    /// <see cref="SimulationWaermebedarf"/>; deshalb reicht die Wärmefassade ihre Gebäude- und
    /// Lastgangschleife hierher durch (<see cref="GebaeudeBuchen"/>, <see cref="GanglinieBuchen"/>)
    /// — gebucht wird im selben Durchlauf wie die Wärme, entschieden und gemessen hier.</para>
    ///
    /// <para><b>Gedeckt von den Kälteerzeugern — der Rest benannt.</b> Die Deckung rechnet die
    /// <see cref="Kaeltekaskade"/> (Stufe KU2) NACH der Wärmekaskade; ihr Ergebnis steht in
    /// <see cref="Kaskade"/>, und <see cref="Kaelterestbedarf"/> ist danach der ungedeckte Rest. Ohne
    /// Kälteerzeuger bleibt er der ganze Bedarf, und der Lauf sagt es als Warnung (F-K12,
    /// Kühlkonzept 5.5): kein stiller Rest.</para>
    ///
    /// <para><b>Nur mit dem Projektschalter.</b> Rechnet das Projekt keine Kälte
    /// (<c>Tab_Einstellungen.Kuehlbetrieb</c> = 0, jedes Bestands- und Referenzprojekt), bleibt
    /// der Kühlkanal leer, <see cref="Gerechnet"/> ist <c>false</c>, und die Ergebnisspalten der
    /// Kälte bleiben NULL — „nicht erhoben", nie 0 (Kühlkonzept 7.4, 10.5).</para>
    ///
    /// <para><b>Die Grenze der Zahl</b> (K5, E31): Die gerechnete Kältemenge ist SENSIBEL — ohne
    /// Feuchte, Entfeuchtung und latente Last. Der Satz dazu steht an jeder Kältezahl
    /// (<see cref="GrenzeFeuchte"/>) und einmal je Lauf im Protokoll.</para>
    ///
    /// <para>Einheiten wie auf der Wärmeseite: Stundenreihen in kWh je Stunde (= kW),
    /// Jahressummen und Monatswerte in MWh, Spitzen in kW.</para>
    /// </summary>
    public class SimulationKaeltebedarf
    {
        private const int STUNDEN = Kanalsatz.STUNDEN_JAHR;

        // =====================================================================
        //  Ergebnis der Kälteseite — die Namenszwillinge der Wärmeseite (E21)
        // =====================================================================

        /// <summary>
        /// Hat der Lauf Kälte ERHOBEN? <c>true</c> genau dann, wenn das Projekt Kälte rechnet
        /// (<c>Tab_Einstellungen.Kuehlbetrieb</c>) und die Bedarfsrechnung abgeschlossen ist.
        /// <c>false</c> heißt „nicht erhoben": Alle Kältezahlen sind dann ohne Aussage, und die
        /// Ergebnisspalten bleiben NULL.
        /// </summary>
        public bool Gerechnet { get; private set; }

        /// <summary>Kältebedarf je Stunde [kWh] = <see cref="Kanalsatz.SummeKaelte"/> — Zwilling von <c>Waermebedarf</c>.</summary>
        public double[] Kaeltebedarf = new double[STUNDEN];

        /// <summary>Kühlreihe der Gebäude mit wirksamer Kühlung, summiert [kWh] — Zwilling von <c>Waermebedarf_Gebaeude</c>.</summary>
        public double[] Kaeltebedarf_Gebaeude = new double[STUNDEN];

        /// <summary>Summe der externen Lastgänge mit dem Kanal „Kuehlung" [kWh] — Zwilling von <c>Waermebedarf_Extern</c>.</summary>
        public double[] Kaeltebedarf_Extern = new double[STUNDEN];

        /// <summary>
        /// Summe der Kältebedarfsprofile (<c>Z_Projekt_Kaeltebedarf</c>, K3, Konzept Kältebedarf 4.1) [kWh] — zentral und
        /// dezentral gedeckt; Zwilling von <c>prozesswerte</c>.
        /// </summary>
        public double[] Kaeltebedarf_Profil = new double[STUNDEN];

        /// <summary>
        /// <b>Dezentral (Split) gedeckter Kältebedarf</b> [kWh je Stunde] (K3, Konzept Kältebedarf 4.2): Lastgang- und
        /// Profilzeilen mit Deckungsart „split“. Er steht im Kühlkanal (der Kanal bleibt die Summe aller Kälte) und wird
        /// vor der Kaskade abgezogen (<see cref="BedarfDerKaskade()"/>); Lieferung = Bedarf.
        /// </summary>
        public double[] Kaeltebedarf_Dezentral = new double[STUNDEN];

        /// <summary>Strom der dezentralen Deckung je Stunde [kWh] = Q_dez / EER(θ_e) (Konzept 4.2).</summary>
        public double[] Strom_Dezentral = new double[STUNDEN];

        /// <summary>Jahressumme der Kältebedarfsprofile [MWh]; 0 ohne Profilzeile.</summary>
        public double Kaeltebedarf_Profil_Gesamt = 0;

        /// <summary>Jahressumme des dezentral gedeckten Kältebedarfs [MWh].</summary>
        public double Kaeltebedarf_Dezentral_Gesamt = 0;

        /// <summary>Spitze des dezentral gedeckten Kältebedarfs [kW].</summary>
        public double Kaelte_Dezentral_Max = 0;

        /// <summary>Jahressumme des Stroms der dezentralen Deckung [MWh].</summary>
        public double Strom_Dezentral_Gesamt = 0;

        /// <summary>Jahres-EER der dezentralen Deckung = Σ Q / Σ P; 0 ohne dezentrale Deckung.</summary>
        public double Jahres_EER_Dezentral
        {
            get { return Strom_Dezentral_Gesamt > 0 ? Kaeltebedarf_Dezentral_Gesamt / Strom_Dezentral_Gesamt : 0.0; }
        }

        /// <summary>Hat eine Profilzeile gerechnet? Dann trägt die Ergebniszeile <c>Kaeltebedarf_Profil_MWh</c>.</summary>
        public bool MitProfil { get; private set; }

        /// <summary>Hat eine Zeile dezentral gerechnet? Dann tragen die Ergebniszeile und der Export die Split-Größen.</summary>
        public bool MitDezentral => DezentraleZeilen.Count > 0;

        /// <summary>
        /// Eine dezentral gedeckte Zeile mit ihrem Strom, ihrem Kühlträger und ihrem Zähler (Konzept 4.2) — die Abrechnung
        /// liest sie (<c>SimulationControl.DezentralenKaeltestromBuchen</c>).
        /// </summary>
        public sealed class DezentraleZeile
        {
            /// <summary>Bezeichner der Zeile.</summary>
            public string Bezeichner = "";

            /// <summary><c>true</c> = Profilzeile, <c>false</c> = Lastgangzeile.</summary>
            public bool Profil;

            /// <summary>Kühlträger; <c>null</c> = Stromträger des Projekts.</summary>
            public int? KuehlIdCarrier;

            /// <summary>Eigener Zähler.</summary>
            public bool EigenerZaehler;

            /// <summary>Kälte der Zeile [kWh/a].</summary>
            public double KaelteKwh;

            /// <summary>Strom der Zeile [kWh/a].</summary>
            public double StromKwh;

            /// <summary>Strom der Zeile je Stunde [kWh].</summary>
            public double[] Strom_stuendlich = new double[STUNDEN];
        }

        /// <summary>Die dezentral gedeckten Zeilen des Laufs in Buchungsfolge (Lastgänge, dann Profile).</summary>
        public List<DezentraleZeile> DezentraleZeilen = new List<DezentraleZeile>();

        /// <summary>Monatswerte des Kältebedarfs [MWh] — Zwilling von <c>Waermebedarf_Gebaeude_Monat</c>.</summary>
        public double[] Kaeltebedarf_Monat = new double[12];

        /// <summary>
        /// Die EIGENE Dauerlinie der Kälteseite [% der Kältespitze], absteigend — nie im
        /// Wärmebild, sonst normierte der Kältewert die Wärmedauerlinie mit (Kühlkonzept 4.2,
        /// 8.4). Ohne Kältebedarf bleibt sie 0.
        /// </summary>
        public double[] Dauerlinie = new double[STUNDEN];

        /// <summary>Der Kältebedarf je Stunde, normiert auf die Spitze [%], in Jahresfolge — Zwilling von <c>Dauerlinie_nicht_sortiert</c>.</summary>
        public double[] Dauerlinie_nicht_sortiert = new double[STUNDEN];

        /// <summary>
        /// Kältespitze [kW]: das Maximum von <see cref="Kanalsatz.SummeKaelte"/> — Zwilling von
        /// <c>Waermebedarf_Max</c>, ausgewiesen als <c>Kaeltelast_Max</c> (K16, Kühlkonzept 4.4:
        /// <c>Kaeltebedarf_Max</c> → <c>Kaeltelast_Max</c> wie <c>Waermebedarf_Max</c> →
        /// <c>Waermelast_Max</c>). Sie steht NEBEN der Wärmespitze und berührt sie nicht.
        /// </summary>
        public double Kaeltebedarf_Max = 0;

        /// <summary>
        /// Jahreskälte [MWh] — Zwilling von <c>Waermebedarf_Gesamt</c> und Nenner des Deckungsgrads
        /// der Kälteseite (6.4). Gebildet wie <c>SimulationRunner.BedarfJeKanal</c> den Kanal
        /// bildet (Summe in Stundenfolge, dann / 1000), damit er mit einem Kältekanal
        /// BITGLEICH mit <c>Waermebedarf_Kuehlung</c> ist (Probe „Kanalsumme der Kälteseite", 10.2).
        /// </summary>
        public double Kaeltebedarf_Gesamt = 0;

        /// <summary>Gebäudeanteil der Jahreskälte [MWh] — Zwilling von <c>Waermebedarf_Gebaeude_Gesamt</c>.</summary>
        public double Kaeltebedarf_Gebaeude_Gesamt = 0;

        /// <summary>Anteil der externen Kältelastgänge [MWh] — Zwilling von <c>Waermebedarf_Extern_Gesamt</c>.</summary>
        public double Kaeltebedarf_Extern_Gesamt = 0;

        /// <summary>
        /// Ungedeckte Kälte [MWh] — Zwilling von <c>Waermerestbedarf</c>. Nach dem Bedarfslauf der
        /// ganze Bedarf; hat eine <see cref="Kaeltekaskade"/> gerechnet (<see cref="DeckungUebernehmen"/>),
        /// ihr Rest. Ohne Kälteerzeuger bleibt sie der Bedarf (benannt, F-K12).
        /// </summary>
        public double Kaelterestbedarf = 0;

        /// <summary>
        /// Die Kältekaskade des Laufs (Stufe KU2, Kühlkonzept 5.5) — Deckung, Kältestrom und
        /// EER-Jahreswert je Erzeuger und gesamt. <c>null</c>, solange kein Kälteerzeuger gerechnet
        /// hat (kein Kühlbetrieb, alle benannt gesperrt oder die Kälte nicht erhoben).
        /// </summary>
        public Kaeltekaskade Kaskade { get; private set; }

        /// <summary>
        /// Zahl der im Projekt ANGELEGTEN Kälteerzeuger (Wärmepumpen mit Kühlbetrieb), gesetzt vom
        /// Bedarfslauf. Ist sie größer 0, meldet erst die Kältekaskade die Unterdeckung — mit ihrem
        /// Grund —, nicht schon der Bedarfslauf: Dort stünde sonst „ohne Kälteerzeuger" über einem
        /// Projekt, das einen hat.
        /// </summary>
        public int KaelteerzeugerAngelegt = 0;

        /// <summary>Stunden mit Kältebedarf [h] (<c>kaelte.stunden</c>) — gezählt am Kanalvektor, nicht als Summe der Gebäude.</summary>
        public int StundenMitKuehlbedarf = 0;

        /// <summary>Vollbenutzungsstunden der Kälte [h/a] = Jahreskälte / Kältespitze — aus beiden gebildet, nicht gemittelt (6.4).</summary>
        public double VollbenutzungsstundenKaelte
        {
            get { return Kaeltebedarf_Max > 0 ? Kaeltebedarf_Gesamt * 1000.0 / Kaeltebedarf_Max : 0.0; }
        }

        /// <summary>
        /// STUNDEN MIT GLEICHZEITIGEM HEIZEN UND KÜHLEN [h] — die Kennzahl aus K6 (E31): nicht
        /// saldiert, sondern ausgewiesen. Projektwert = Maximum über die gekühlten Gebäude, mit
        /// dem führenden Gebäude als Herkunft (<see cref="StundenHeizenUndKuehlenGebaeude"/>;
        /// Kühlkonzept 3.5, 6.4). Im Einzonenfall sind das die Umschaltstunden.
        /// </summary>
        public int StundenHeizenUndKuehlen = 0;

        /// <summary>Das Gebäude, von dem <see cref="StundenHeizenUndKuehlen"/> stammt; leer ohne solche Stunden.</summary>
        public string StundenHeizenUndKuehlenGebaeude = "";

        /// <summary>Zahl der Gebäude, deren Kühlreihe in den Kühlkanal ging.</summary>
        public int GekuehlteGebaeude = 0;

        /// <summary>
        /// Gebäude mit eingeschalteter Kühlung auf dem BESTANDSWEG (Tagesbilanz): Sie tragen
        /// Kältebedarf 0 mit benanntem Hinweis (F-K18, E20) — bis zur Stufe GA, Löschliste
        /// Umsetzungskonzept 6.1.
        /// </summary>
        public List<int> GebaeudeBestandsweg = new List<int>();

        /// <summary>Stunden, in denen die Bedarfsprobe Kälte die Toleranz überschritten hat — Erwartungswert 0.</summary>
        public int Bedarfsprobe_Verletzungen = 0;

        /// <summary>Größte Abweichung der Bedarfsprobe Kälte [kWh].</summary>
        public double Bedarfsprobe_MaxAbweichung = 0;

        /// <summary>
        /// Benannter Abbruch (Muster <c>SimulationWaermebedarf.Fehlertext</c>): leer = gerechnet.
        /// Gesetzt allein von der Bedarfsprobe Kälte; die Wärmefassade reicht ihn weiter, und der
        /// Lauf speichert dann kein Ergebnis (Kühlkonzept 4.4: Stufe Fehler, Gesamtergebnis
        /// fehlgeschlagen).
        /// </summary>
        public string Fehlertext = "";

        /// <summary>
        /// Der Satz, der an JEDER Kältezahl steht (K5, E31; Kühlkonzept 3.6): sensible Kälte, ohne
        /// Feuchte, Entfeuchtung und latente Last. Oberfläche, Bericht und Export nehmen ihn von
        /// hier — ein Text, eine Stelle.
        /// </summary>
        public static string GrenzeFeuchte => MyResource.Resource.KAELTE_GRENZE_FEUCHTE;

        // =====================================================================
        //  Laufzustand
        // =====================================================================

        private Kanalsatz _kanaele;
        private bool _kuehlbetrieb;
        private double[] _probe = new double[STUNDEN];
        private int _gebaeudeMitKuehlung;
        private int _kaelteGanglinien;
        private int _kaelteProfile;
        private readonly List<string> _splitOhneEer = new List<string>();

        /// <summary>
        /// Die Außenlufttemperatur der Klimaregion des Laufs je Stunde [°C] — dieselbe Reihe wie Kältemaschine und Gebäude
        /// (<c>SimulationWaermebedarf.Stundentemperatur</c>); der EER der dezentralen Deckung hängt an ihr.
        /// </summary>
        internal double[] Aussentemperatur;

        // =====================================================================
        //  Die drei Schritte eines Laufs: Beginnen — Buchen — Abschliessen
        // =====================================================================

        /// <summary>
        /// Beginn eines Bedarfslaufs: an das EINE Kanalfeld andocken und alles Vorige verwerfen.
        /// Gerufen von <see cref="SimulationWaermebedarf.Waermebedarf_berechnen"/>, unmittelbar
        /// nachdem der Kanalsatz neu angelegt ist.
        /// </summary>
        /// <param name="kanaele">Der Kanalsatz des Laufs; der Kühlkanal wird hier gefüllt.</param>
        /// <param name="kuehlbetrieb">Rechnet das Projekt Kälte (<c>Tab_Einstellungen.Kuehlbetrieb</c>)?</param>
        internal void Beginnen(Kanalsatz kanaele, bool kuehlbetrieb)
        {
            _kanaele = kanaele;
            _kuehlbetrieb = kuehlbetrieb;
            _gebaeudeMitKuehlung = 0;
            _kaelteGanglinien = 0;
            _kaelteProfile = 0;
            _splitOhneEer.Clear();
            Array.Clear(_probe, 0, STUNDEN);
            Array.Clear(Kaeltebedarf_Profil, 0, STUNDEN);
            Array.Clear(Kaeltebedarf_Dezentral, 0, STUNDEN);
            Array.Clear(Strom_Dezentral, 0, STUNDEN);
            Kaeltebedarf_Profil_Gesamt = 0;
            Kaeltebedarf_Dezentral_Gesamt = 0;
            Kaelte_Dezentral_Max = 0;
            Strom_Dezentral_Gesamt = 0;
            MitProfil = false;
            DezentraleZeilen.Clear();

            Gerechnet = false;
            Array.Clear(Kaeltebedarf, 0, STUNDEN);
            Array.Clear(Kaeltebedarf_Gebaeude, 0, STUNDEN);
            Array.Clear(Kaeltebedarf_Extern, 0, STUNDEN);
            Array.Clear(Kaeltebedarf_Monat, 0, Kaeltebedarf_Monat.Length);
            Array.Clear(Dauerlinie, 0, STUNDEN);
            Array.Clear(Dauerlinie_nicht_sortiert, 0, STUNDEN);
            Kaeltebedarf_Max = 0;
            Kaeltebedarf_Gesamt = 0;
            Kaeltebedarf_Gebaeude_Gesamt = 0;
            Kaeltebedarf_Extern_Gesamt = 0;
            Kaelterestbedarf = 0;
            Kaskade = null;
            KaelteerzeugerAngelegt = 0;
            StundenMitKuehlbedarf = 0;
            StundenHeizenUndKuehlen = 0;
            StundenHeizenUndKuehlenGebaeude = "";
            GekuehlteGebaeude = 0;
            GebaeudeBestandsweg.Clear();
            Bedarfsprobe_Verletzungen = 0;
            Bedarfsprobe_MaxAbweichung = 0;
            Fehlertext = "";
        }

        /// <summary>
        /// Ein Gebäude nach seiner Rechnung (Gebäudeschleife der Wärmefassade). Die Kühlreihe
        /// kommt aus DEMSELBEN Ergebnis wie die Heizlast (<paramref name="ergebnis"/>, skaliert
        /// nach E8) — es gibt keine zweite Gebäuderechnung für die Kälte (E21, 3.7).
        /// </summary>
        /// <param name="index">Merkplatz des Gebäudes im Lauf.</param>
        /// <param name="gebaeude">Die Gebäudezeile (Kühlschalter, Name).</param>
        /// <param name="ergebnis">Das Ergebnis des VDI-Wegs; <c>null</c> = Bestandsweg (Tagesbilanz).</param>
        internal void GebaeudeBuchen(int index, ProjektGebaeudeModel gebaeude, GebaeudeModellErgebnis ergebnis)
        {
            if (gebaeude == null || !gebaeude.Kuehlung_Aktiv) return;
            _gebaeudeMitKuehlung++;
            if (!_kuehlbetrieb || _kanaele == null) return;      // Projekt aus: nichts gerechnet

            string name = Gebaeudename(gebaeude);

            // F-K18 (E20, E23, E26): Der Bestandsweg hat keine Stundenschleife und damit keine
            // Kühllast - Kältebedarf 0, und das sagt der Lauf, einmal je Gebäude. Der Sonderfall
            // steht in der Löschliste der Stufe GA.
            if (ergebnis == null)
            {
                GebaeudeBestandsweg.Add(gebaeude.ID_Gebaeude);
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "kuehlung-bestandsweg-" + gebaeude.ID_Gebaeude.ToString(CultureInfo.InvariantCulture),
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_BESTANDSWEG, name));
                return;
            }

            // F-K1: Ein leerer Kühlsollwert heißt „Kühlung aus" - ausdrücklich, nicht still.
            if (!ergebnis.KuehlungWirksam)
            {
                SimulationProtokoll.Aktuell.HinweisEinmal(
                    "kuehlung-ohne-sollwert-" + gebaeude.ID_Gebaeude.ToString(CultureInfo.InvariantCulture),
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_OHNE_SOLLWERT, name));
                return;
            }

            double[] reihe = ergebnis.KuehlbedarfKwh;
            double[] kanal = _kanaele.Kuehlung;
            for (int h = 0; h < STUNDEN; h++)
            {
                kanal[h] = ((double)kanal[h] + reihe[h]);
                Kaeltebedarf_Gebaeude[h] = ((double)Kaeltebedarf_Gebaeude[h] + reihe[h]);
                _probe[h] += reihe[h];
            }
            GekuehlteGebaeude++;

            // K6: nicht saldieren, ausweisen - Maximum mit dem führenden Gebäude (6.4).
            if (ergebnis.StundenHeizenUndKuehlen > StundenHeizenUndKuehlen)
            {
                StundenHeizenUndKuehlen = ergebnis.StundenHeizenUndKuehlen;
                StundenHeizenUndKuehlenGebaeude = name;
            }
            // KU3-3 (F-K15): im Mehrzonenweg je Gebäude die Energie je Richtung in diesen Stunden.
            if (ergebnis.StundenHeizenUndKuehlen > 0
                && ergebnis.GleichzeitigHeizenKwh is double gleichHeiz && ergebnis.GleichzeitigKuehlenKwh is double gleichKuehl)
                SimulationProtokoll.Aktuell.HinweisEinmal("ku3-gleichzeitig-" + name,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KU3_ZONEN_GLEICHZEITIG, name,
                                  ergebnis.StundenHeizenUndKuehlen, gleichHeiz.ToString("0", CultureInfo.CurrentCulture),
                                  gleichKuehl.ToString("0", CultureInfo.CurrentCulture)));
        }

        /// <summary>
        /// Ein externer Lastgang mit dem Kanal „Kuehlung" (K3, F-K5) — Kältebedarf ohne
        /// Gebäudemodell. Er geht NIE in einen Wärmekanal und nicht in die Energieprobe der
        /// Wärmeseite (4.2), sondern hierher und in die Bedarfsprobe Kälte. Rechnet das Projekt
        /// keine Kälte, bleibt er unberücksichtigt, und der Lauf sagt es. Zentral gedeckt; die
        /// Deckungsart der Zeile nimmt <see cref="BedarfBuchen"/>.
        /// </summary>
        /// <param name="werte">Die 8 760 Stundenwerte [kWh].</param>
        internal void GanglinieBuchen(double[] werte) => BedarfBuchen(werte, Kaeltedeckung.Zentral, false);

        /// <summary>Zählt Profilzeilen eines Projekts ohne Kühlbetrieb für die Meldung „ohne Wirkung“ (Konzept 4.4).</summary>
        internal void ProfileZaehlen(int anzahl) { if (anzahl > 0) _kaelteProfile += anzahl; }

        /// <summary>
        /// <b>Der Eingang für Lastgang- und Profilzeilen</b> (Konzept Kältebedarf 4.2): die Reihe geht in den Kühlkanal,
        /// in <see cref="Kaeltebedarf_Extern"/> (Lastgang) bzw. <see cref="Kaeltebedarf_Profil"/> (Profil) und in die
        /// Bedarfsprobe Kälte. Ist die Zeile dezentral (Split) gedeckt, geht sie zusätzlich in
        /// <see cref="Kaeltebedarf_Dezentral"/> — die Kaskade bekommt den Kanal ohne sie (<see cref="BedarfDerKaskade()"/>) —,
        /// und ihr Strom je Stunde ist Q / EER(θ_e). Ohne Kühlbetrieb wird nur gezählt.
        /// </summary>
        /// <param name="werte">Die 8 760 Stundenwerte [kWh].</param>
        /// <param name="deckung">Die Deckungsart der Zeile; <c>null</c> = zentral.</param>
        /// <param name="profil"><c>true</c> = Profilzeile (<c>Z_Projekt_Kaeltebedarf</c>), <c>false</c> = Lastgangzeile.</param>
        internal void BedarfBuchen(double[] werte, Kaeltedeckung deckung, bool profil)
        {
            if (werte == null) return;
            if (profil) _kaelteProfile++; else _kaelteGanglinien++;
            if (!_kuehlbetrieb || _kanaele == null) return;
            if (deckung == null) deckung = Kaeltedeckung.Zentral;

            double[] kanal = _kanaele.Kuehlung;
            double[] posten = profil ? Kaeltebedarf_Profil : Kaeltebedarf_Extern;
            double summe = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                kanal[h] = ((double)kanal[h] + werte[h]);
                posten[h] = ((double)posten[h] + werte[h]);
                _probe[h] += werte[h];
                summe += werte[h];
            }
            if (profil) { Kaeltebedarf_Profil_Gesamt += summe / 1000; MitProfil = true; }
            else Kaeltebedarf_Extern_Gesamt += summe / 1000;

            if (!deckung.Split) return;

            // Split ohne rechnenden EER: benannter Abbruch im Abschluss (Konzept 4.4), kein stiller Strom 0.
            if (deckung.Pruefgrund != null || Aussentemperatur == null)
            {
                _splitOhneEer.Add(deckung.Bezeichner + " (" + (deckung.Pruefgrund ?? "ohne Aussentemperatur") + ")");
                return;
            }

            var zeile = new DezentraleZeile
            {
                Bezeichner = deckung.Bezeichner, Profil = profil,
                KuehlIdCarrier = deckung.KuehlIdCarrier, EigenerZaehler = deckung.EigenerZaehler,
            };
            for (int h = 0; h < STUNDEN; h++)
            {
                double q = werte[h];
                if (q == 0.0) continue;
                double p = q / deckung.Eer(Aussentemperatur[h]);
                Kaeltebedarf_Dezentral[h] += q;
                Strom_Dezentral[h] += p;
                zeile.Strom_stuendlich[h] = p;
                zeile.KaelteKwh += q;
                zeile.StromKwh += p;
            }
            DezentraleZeilen.Add(zeile);
        }

        /// <summary>
        /// <b>Die Abzugsstelle vor der Kaskade</b> (Konzept Kältebedarf 4.2) — Andockpunkt für K-D (Raumgeräte, Sitzung
        /// Kälteanlagen): Der Kühlkanal bleibt die Summe aller Kälte; die Kältekaskade bekommt ihn ohne die Posten, die
        /// neben ihr gedeckt werden. Heute ist das <see cref="Kaeltebedarf_Dezentral"/>; K-D zieht
        /// <c>Kaeltebedarf_Raumgeraete</c> HIER mit ab (<see cref="AbzugVorDerKaskade"/>), ohne zweite Naht. Ohne Abzug ist
        /// das Ergebnis Zeichen für Zeichen <see cref="Kaeltebedarf"/>.
        /// </summary>
        /// <param name="h">Jahresstunde.</param>
        /// <param name="bedarfKwh">Der Kältebedarf dieser Stunde, wie die Kaskade ihn sonst bekäme [kWh].</param>
        public double BedarfDerKaskade(int h, double bedarfKwh)
        {
            double abzug = AbzugVorDerKaskade(h);
            return abzug != 0.0 ? bedarfKwh - abzug : bedarfKwh;
        }

        /// <summary>Die ganze Jahresreihe für die Kaskade: <see cref="Kaeltebedarf"/> selbst ohne Abzug, sonst eine Kopie mit Abzug.</summary>
        public double[] BedarfDerKaskade()
        {
            if (!MitDezentral) return Kaeltebedarf;
            var reihe = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++) reihe[h] = BedarfDerKaskade(h, Kaeltebedarf[h]);
            return reihe;
        }

        /// <summary>
        /// Der Abzug einer Stunde [kWh]: was neben der Kaskade gedeckt wird. Andockpunkt für K-D (Raumgeräte, Sitzung
        /// Kälteanlagen): <c>+ Kaeltebedarf_Raumgeraete[h]</c> gehört hierher.
        /// </summary>
        public double AbzugVorDerKaskade(int h) => Kaeltebedarf_Dezentral[h];

        /// <summary>
        /// Abschluss des Bedarfslaufs: Summe, Spitze, Monatswerte, Dauerlinie, Stunden, die
        /// Bedarfsprobe Kälte und die Meldungen. Gerufen am Ende von
        /// <see cref="SimulationWaermebedarf.Waermebedarf_berechnen"/> — NACH der Wärmeseite,
        /// damit die Probe auch sieht, was die Wärmeseite danach mit dem Kanalsatz getan hat
        /// (Netzverluste, 4.2 b).
        /// </summary>
        /// <returns><c>false</c> = die Bedarfsprobe Kälte ist verletzt (<see cref="Fehlertext"/>).</returns>
        internal bool Abschliessen(int[] moAnfang, int[] moEnde)
        {
            SimulationProtokoll protokoll = SimulationProtokoll.Aktuell;

            if (!_kuehlbetrieb || _kanaele == null)
            {
                // Projekt aus: Der Kanal bleibt leer, gerechnet wird nichts (7.2, 8.3). Liegen
                // Kühleingaben vor, sagt der Lauf, warum sie nicht wirken.
                if (_kaelteProfile > 0)
                    protokoll.HinweisEinmal("kuehlung-projekt-aus",
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_PROJEKT_AUS_PROFILE,
                                      _gebaeudeMitKuehlung, _kaelteGanglinien, _kaelteProfile));
                else if (_gebaeudeMitKuehlung > 0 || _kaelteGanglinien > 0)
                    protokoll.HinweisEinmal("kuehlung-projekt-aus",
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_PROJEKT_AUS,
                                      _gebaeudeMitKuehlung, _kaelteGanglinien));
                return true;
            }

            // Konzept Kältebedarf 4.4: eine Split-Zeile ohne rechnenden EER ist ein benannter Fehler, kein Strom 0.
            if (_splitOhneEer.Count > 0)
            {
                Fehlertext = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_SPLIT_OHNE_EER,
                                           string.Join("; ", _splitOhneEer));
                protokoll.Fehlermeldung(Fehlertext);
                return false;
            }

            double[] summe = _kanaele.SummeKaelte();
            Array.Copy(summe, Kaeltebedarf, STUNDEN);

            if (!Bedarfsprobe(summe)) return false;

            KennzahlenBilden(moAnfang, moEnde);
            Gerechnet = true;
            DezentralMelden();

            if (Kaeltebedarf_Gesamt > 0)
            {
                // F-K12: Unterdeckung ist eine benannte Meldung, kein stiller Rest. Hat das
                // Projekt Kälteerzeuger, sagt es die Kältekaskade - mit Menge und Grund. Der
                // dezentral gedeckte Teil ist gedeckt (K3) und zählt hier nicht.
                if (KaelteerzeugerAngelegt <= 0 && Kaelterestbedarf > 0) OhneErzeugerMelden();

                // K5: die Grenze der Zahl, einmal je Lauf.
                protokoll.HinweisEinmal("kaelte-grenze-feuchte", GrenzeFeuchte);
            }

            if (StundenHeizenUndKuehlen > 0)
                protokoll.Hinweis(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KAELTE_HEIZEN_UND_KUEHLEN,
                    StundenHeizenUndKuehlen, StundenHeizenUndKuehlenGebaeude));

            return true;
        }

        /// <summary>
        /// Summe, Spitze, Stunden, Monatswerte und die eigene Dauerlinie aus <see cref="Kaeltebedarf"/>; danach ist die
        /// ungedeckte Kälte der Bedarf. Gerufen von <see cref="Abschliessen"/> und <see cref="KreisNachfuehren"/>.
        /// </summary>
        private void KennzahlenBilden(int[] moAnfang, int[] moEnde)
        {
            // Die Jahressummen in Stundenfolge (siehe Kaeltebedarf_Gesamt).
            Kaeltebedarf_Gesamt = Jahressumme(Kaeltebedarf) / 1000.0;
            Kaeltebedarf_Gebaeude_Gesamt = Jahressumme(Kaeltebedarf_Gebaeude) / 1000.0;

            Kaeltebedarf_Max = 0;
            StundenMitKuehlbedarf = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (Kaeltebedarf_Max < Kaeltebedarf[h]) Kaeltebedarf_Max = Kaeltebedarf[h];
                if (Kaeltebedarf[h] > 0.0) StundenMitKuehlbedarf++;
            }

            if (moAnfang != null && moEnde != null)
                WPPlan.Core.BhkwPlan.MonatsSumme(Kaeltebedarf, Kaeltebedarf_Monat, moAnfang, moEnde);

            // Die eigene Dauerlinie - dieselbe Bildung wie auf der Wärmeseite, nur mit der
            // Kältereihe. Ohne Kältebedarf keine Normierung (sie teilte durch 0).
            if (Kaeltebedarf_Max > 0)
            {
                double[] sortiert = (double[])Kaeltebedarf.Clone();
                Array.Copy(Kaeltebedarf, Dauerlinie_nicht_sortiert, STUNDEN);
                WPPlan.Core.BhkwPlan.Normieren(sortiert, Kaeltebedarf_Max);
                WPPlan.Core.BhkwPlan.Normieren(Dauerlinie_nicht_sortiert, Kaeltebedarf_Max);
                WPPlan.Core.BhkwPlan.Heapsort(sortiert, Dauerlinie);
                Array.Reverse(Dauerlinie);
            }

            // K3 (Konzept Kältebedarf 4.3): die dezentrale Deckung - Lieferung = Bedarf.
            if (MitDezentral)
            {
                Kaeltebedarf_Dezentral_Gesamt = Jahressumme(Kaeltebedarf_Dezentral) / 1000.0;
                Strom_Dezentral_Gesamt = Jahressumme(Strom_Dezentral) / 1000.0;
                Kaelte_Dezentral_Max = 0;
                for (int h = 0; h < STUNDEN; h++)
                    if (Kaelte_Dezentral_Max < Kaeltebedarf_Dezentral[h]) Kaelte_Dezentral_Max = Kaeltebedarf_Dezentral[h];
            }

            // Vor der Deckung ist die ungedeckte Kälte der Bedarf; die Kältekaskade (KU2) setzt
            // danach ihren Rest (DeckungUebernehmen). Der dezentral gedeckte Teil ist kein Rest (K3).
            Kaelterestbedarf = MitDezentral ? Jahressumme(BedarfDerKaskade()) / 1000.0 : Kaeltebedarf_Gesamt;
        }

        /// <summary>
        /// Laufmeldungen der dezentralen Deckung (Konzept Kältebedarf 4.3/4.4): Menge, Strom und Jahres-EER je Lauf, der
        /// Hinweis „Jahres-EER unter 2“ und je Zeile mit abweichendem Kühlträger, wie ihr Strom abgerechnet wird.
        /// </summary>
        private void DezentralMelden()
        {
            if (!MitDezentral) return;
            SimulationProtokoll protokoll = SimulationProtokoll.Aktuell;
            protokoll.Hinweis(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_DEZENTRAL,
                DezentraleZeilen.Count,
                Kaeltebedarf_Dezentral_Gesamt.ToString("N2", CultureInfo.CurrentCulture),
                Kaelte_Dezentral_Max.ToString("N1", CultureInfo.CurrentCulture),
                Strom_Dezentral_Gesamt.ToString("N2", CultureInfo.CurrentCulture),
                Jahres_EER_Dezentral.ToString("N2", CultureInfo.CurrentCulture)));
            if (Strom_Dezentral_Gesamt > 0 && Jahres_EER_Dezentral < KaeltebedarfFestwerte.JAHRES_EER_HINWEIS)
                protokoll.Hinweis(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_DEZENTRAL_EER_NIEDRIG,
                    Jahres_EER_Dezentral.ToString("N2", CultureInfo.CurrentCulture),
                    KaeltebedarfFestwerte.JAHRES_EER_HINWEIS.ToString("N1", CultureInfo.CurrentCulture)));
        }

        // =====================================================================
        //  AK3-K (Fehler 1.1 (a)): der Kühlkanal folgt dem Kreis
        // =====================================================================

        /// <summary>
        /// Bucht <see cref="GebaeudeBuchen"/> die Kühlreihe dieses Gebäudeergebnisses in den Kühlkanal? Dieselbe
        /// Bedingung wie dort: Projekt rechnet Kälte, Gebäude mit Kühlung, VDI-Weg mit wirksamer Kühlung.
        /// </summary>
        internal bool Bucht(ProjektGebaeudeModel gebaeude, GebaeudeModellErgebnis ergebnis)
            => gebaeude != null && gebaeude.Kuehlung_Aktiv && _kuehlbetrieb && _kanaele != null
               && ergebnis != null && ergebnis.KuehlungWirksam && ergebnis.KuehlbedarfKwh != null;

        /// <summary>
        /// <b>Führt die Kälteseite um die Abweichung des Kreises nach</b> (Entwurf AK3-K 1.1 (a)): Kühlkanal, <see cref="Kaeltebedarf_Gebaeude"/> und der Prüfakkumulator der
        /// Bedarfsprobe nehmen in jeder Stunde mit Abweichung dieselbe Abweichung auf; danach laufen die Bedarfsprobe
        /// ein zweites Mal und Summe, Spitze, Stunden, Monatswerte und Dauerlinie neu — ohne Meldungen, die stehen
        /// schon aus Pass 1. Stunden ohne Abweichung bleiben Zeichen für Zeichen Pass 1.
        /// </summary>
        /// <param name="deltaKwh">Abweichung der Kühlreihe des Kreises gegen Pass 1 je Stunde [kWh] (0 = keine).</param>
        /// <returns><c>false</c> = die Bedarfsprobe Kälte ist verletzt (<see cref="Fehlertext"/>).</returns>
        internal bool KreisNachfuehren(double[] deltaKwh, int[] moAnfang, int[] moEnde)
        {
            if (!Gerechnet || _kanaele == null || deltaKwh == null) return true;
            double[] kanal = _kanaele.Kuehlung;
            bool geaendert = false;
            for (int h = 0; h < STUNDEN; h++)
            {
                double d = deltaKwh[h];
                if (d == 0.0) continue;
                kanal[h] += d;
                Kaeltebedarf_Gebaeude[h] += d;
                _probe[h] += d;
                geaendert = true;
            }
            if (!geaendert) return true;
            double[] summe = _kanaele.SummeKaelte();
            Array.Copy(summe, Kaeltebedarf, STUNDEN);
            if (!Bedarfsprobe(summe)) return false;
            KennzahlenBilden(moAnfang, moEnde);
            return true;
        }

        /// <summary>Der Stand der Kälteseite nach Pass 1 (AK3-K; zweiter Feldlauf der Erdsonde).</summary>
        internal sealed class Stand
        {
            internal double[] Kuehlkanal, Kaeltebedarf, Gebaeude, Monat, Dauerlinie, DauerlinieNichtSortiert, Probe;
            internal double Max, Gesamt, GebaeudeGesamt, Restbedarf, ProbeMax;
            internal int Stunden, ProbeVerletzungen;
            internal bool Gerechnet;
        }

        /// <summary>Sichert den Stand nach Pass 1, den <see cref="KreisNachfuehren"/> verändert.</summary>
        internal Stand Sichern() => new Stand
        {
            Kuehlkanal = _kanaele != null ? (double[])_kanaele.Kuehlung.Clone() : null,
            Kaeltebedarf = (double[])Kaeltebedarf.Clone(),
            Gebaeude = (double[])Kaeltebedarf_Gebaeude.Clone(),
            Monat = (double[])Kaeltebedarf_Monat.Clone(),
            Dauerlinie = (double[])Dauerlinie.Clone(),
            DauerlinieNichtSortiert = (double[])Dauerlinie_nicht_sortiert.Clone(),
            Probe = (double[])_probe.Clone(),
            Max = Kaeltebedarf_Max,
            Gesamt = Kaeltebedarf_Gesamt,
            GebaeudeGesamt = Kaeltebedarf_Gebaeude_Gesamt,
            Restbedarf = Kaelterestbedarf,
            ProbeMax = Bedarfsprobe_MaxAbweichung,
            Stunden = StundenMitKuehlbedarf,
            ProbeVerletzungen = Bedarfsprobe_Verletzungen,
            Gerechnet = Gerechnet,
        };

        /// <summary>Stellt den mit <see cref="Sichern"/> gesicherten Stand Zeichen für Zeichen wieder her.</summary>
        internal void Herstellen(Stand s)
        {
            if (s == null) return;
            if (s.Kuehlkanal != null && _kanaele != null) Array.Copy(s.Kuehlkanal, _kanaele.Kuehlung, STUNDEN);
            Array.Copy(s.Kaeltebedarf, Kaeltebedarf, STUNDEN);
            Array.Copy(s.Gebaeude, Kaeltebedarf_Gebaeude, STUNDEN);
            Array.Copy(s.Monat, Kaeltebedarf_Monat, s.Monat.Length);
            Array.Copy(s.Dauerlinie, Dauerlinie, STUNDEN);
            Array.Copy(s.DauerlinieNichtSortiert, Dauerlinie_nicht_sortiert, STUNDEN);
            Array.Copy(s.Probe, _probe, STUNDEN);
            Kaeltebedarf_Max = s.Max;
            Kaeltebedarf_Gesamt = s.Gesamt;
            Kaeltebedarf_Gebaeude_Gesamt = s.GebaeudeGesamt;
            Kaelterestbedarf = s.Restbedarf;
            Bedarfsprobe_MaxAbweichung = s.ProbeMax;
            StundenMitKuehlbedarf = s.Stunden;
            Bedarfsprobe_Verletzungen = s.ProbeVerletzungen;
            Gerechnet = s.Gerechnet;
            Fehlertext = "";
        }

        /// <summary>
        /// Die Warnung „Kältebedarf ohne Kälteerzeuger" (F-K12, 8.5) — aus dem Bedarfslauf, wenn das
        /// Projekt keinen Kälteerzeuger anlegt, sonst aus der Kältekaskade, wenn keiner rechnen kann.
        /// </summary>
        internal void OhneErzeugerMelden()
        {
            if (!Gerechnet || !(Kaeltebedarf_Gesamt > 0)) return;
            // K3: ohne dezentrale Deckung der ganze Bedarf (wie zuvor), sonst nur der Teil für die Kaskade.
            double menge = MitDezentral ? Kaelterestbedarf : Kaeltebedarf_Gesamt;
            if (!(menge > 0)) return;
            SimulationProtokoll.Aktuell.Warnung(string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.SIMENG_KAELTE_OHNE_ERZEUGER,
                menge.ToString("N2", CultureInfo.CurrentCulture),
                Kaeltebedarf_Max.ToString("N1", CultureInfo.CurrentCulture)));
        }

        /// <summary>
        /// Übernimmt das Ergebnis der <see cref="Kaeltekaskade"/> (Stufe KU2): Die ungedeckte Kälte
        /// ist danach ihr Rest [MWh] — gebildet wie <see cref="Kaeltebedarf_Gesamt"/> (Summe in
        /// Stundenfolge, dann / 1000).
        /// </summary>
        internal void DeckungUebernehmen(Kaeltekaskade kaskade)
        {
            if (!Gerechnet || kaskade == null) return;
            Kaskade = kaskade;
            Kaelterestbedarf = Jahressumme(kaskade.Rest_stuendlich) / 1000.0;
        }

        /// <summary>
        /// <b>Bedarfsprobe Kälte</b> (Kühlkonzept 4.3 #30, 4.4; F-K6): <see cref="Kanalsatz.SummeKaelte"/>
        /// gegen den eigenen Referenzakkumulator, in den jede Kältebuchung unabhängig eingetragen
        /// wurde. Sie hält fest, dass der Kühlkanal GENAU die gebuchten Beiträge trägt und kein
        /// Wärmebeitrag hineingeraten ist — auch keiner, den die Wärmeseite nach der Buchung
        /// verteilt hätte (Netzverluste, 4.2 b).
        ///
        /// <para><b>Schärfer als die Energieprobe der Wärmeseite, ausdrücklich so festgelegt:</b>
        /// Sie meldet einmal je Lauf mit der Stufe FEHLER und setzt das Gesamtergebnis auf
        /// fehlgeschlagen. Eine verletzte Kälteprobe heißt, dass die Trennung der Deckungswelten
        /// nicht hält — dann ist jede Zahl des Laufs unbrauchbar.</para>
        /// </summary>
        private bool Bedarfsprobe(double[] summe)
        {
            Bedarfsprobe_Verletzungen = 0;
            Bedarfsprobe_MaxAbweichung = 0;

            for (int h = 0; h < STUNDEN; h++)
            {
                double abweichung = Math.Abs(summe[h] - _probe[h]);
                if (abweichung > Bedarfsprobe_MaxAbweichung) Bedarfsprobe_MaxAbweichung = abweichung;
                if (!Kanalsatz.ErhaltungOk(_probe[h], summe[h], Kanalsatz.ERHALTUNG_SCHRITTE_SUMME_KAELTE))
                    Bedarfsprobe_Verletzungen++;
            }

            if (Bedarfsprobe_Verletzungen == 0) return true;

            Fehlertext = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_BEDARFSPROBE,
                                       Bedarfsprobe_Verletzungen,
                                       Bedarfsprobe_MaxAbweichung.ToString("G4", CultureInfo.CurrentCulture));
            SimulationProtokoll.Aktuell.Fehlermeldung(Fehlertext);
            Gerechnet = false;
            return false;
        }

        /// <summary>Summe einer Stundenreihe in Stundenfolge [kWh] — die Bildung von <c>SimulationRunner.BedarfJeKanal</c>.</summary>
        private static double Jahressumme(double[] reihe)
        {
            double s = 0;
            for (int h = 0; h < reihe.Length; h++) s += reihe[h];
            return s;
        }

        private static string Gebaeudename(ProjektGebaeudeModel g)
        {
            return string.IsNullOrEmpty(g.Gebaeudename)
                ? g.ID_Gebaeude.ToString(CultureInfo.InvariantCulture)
                : g.Gebaeudename;
        }

        // =====================================================================
        //  Die Gegenüberstellung, die E21 verlangt (Kühlkonzept 4.2, 5.5, 6.4; F-K19)
        // =====================================================================

        /// <summary>
        /// Eine Zeile der Gegenüberstellung Wärmeseite ↔ Kälteseite: die Größe der Wärmeseite,
        /// ihr Gegenstück (Mitglied von <see cref="SimulationKaeltebedarf"/>, Ergebnisfeld oder
        /// Kanalsatz) und die Stufe — oder, wo es kein Gegenstück gibt, die benannte Abweichung.
        /// </summary>
        public sealed class Gegenstueck
        {
            internal Gegenstueck(string waerme, string kaelte, string stufe, string abweichung = null)
            {
                Waerme = waerme;
                Kaelte = kaelte;
                Stufe = stufe;
                Abweichung = abweichung;
            }

            /// <summary>Die Größe der Wärmeseite (Mitgliedsname, bei Ergebnisfeldern mit Typpräfix).</summary>
            public string Waerme { get; }

            /// <summary>Ihr Gegenstück auf der Kälteseite; <c>null</c> = benannte Abweichung.</summary>
            public string Kaelte { get; }

            /// <summary>Die Stufe, in der das Gegenstück gebaut ist bzw. wird (<c>KU1</c>, <c>KU2</c>).</summary>
            public string Stufe { get; }

            /// <summary>Die Begründung einer Abweichung oder eines Zugewinns; <c>null</c> = echtes Paar.</summary>
            public string Abweichung { get; }
        }

        /// <summary>
        /// <b>Die Symmetrieliste</b> — die Probe „Symmetrie der Kennzahlen" (Kühlkonzept 10.2,
        /// F-K19) läuft über DIESE Liste, nicht über Zahlen: Jede Größe der Wärmeseite hat ihr
        /// Gegenstück — gebaut (KU1), angekündigt (KU2) oder als Abweichung benannt. Die Probe
        /// fällt, sobald eine Seite eine Größe bekommt, die hier weder als Paar noch als
        /// Abweichung steht. Namen: Mitglieder von <see cref="SimulationWaermebedarf"/> bzw.
        /// <see cref="SimulationKaeltebedarf"/>; <c>Ergebnis.</c> meint
        /// <see cref="ErgebnisEnergiebedarfModel"/>, <c>Kanalsatz.</c> den Kanalsatz.
        /// </summary>
        public static readonly IReadOnlyList<Gegenstueck> GEGENSTUECKE = new[]
        {
            // ---- KU1: gebaut ----
            new Gegenstueck("Waermebedarf", "Kaeltebedarf", "KU1"),
            new Gegenstueck("Waermebedarf_Gebaeude", "Kaeltebedarf_Gebaeude", "KU1"),
            new Gegenstueck("Waermebedarf_Extern", "Kaeltebedarf_Extern", "KU1"),
            new Gegenstueck("Waermebedarf_Gebaeude_Monat", "Kaeltebedarf_Monat", "KU1"),
            new Gegenstueck("Dauerlinie", "Dauerlinie", "KU1"),
            new Gegenstueck("Dauerlinie_nicht_sortiert", "Dauerlinie_nicht_sortiert", "KU1"),
            new Gegenstueck("Waermebedarf_Max", "Kaeltebedarf_Max", "KU1"),
            new Gegenstueck("Waermebedarf_Gesamt", "Kaeltebedarf_Gesamt", "KU1"),
            new Gegenstueck("Waermebedarf_Gebaeude_Gesamt", "Kaeltebedarf_Gebaeude_Gesamt", "KU1"),
            new Gegenstueck("Waermebedarf_Extern_Gesamt", "Kaeltebedarf_Extern_Gesamt", "KU1"),
            new Gegenstueck("Energieprobe_Verletzungen", "Bedarfsprobe_Verletzungen", "KU1"),
            new Gegenstueck("Energieprobe_MaxAbweichung", "Bedarfsprobe_MaxAbweichung", "KU1"),
            new Gegenstueck("Fehlertext", "Fehlertext", "KU1"),
            new Gegenstueck("Kanalsatz.Summe", "Kanalsatz.SummeKaelte", "KU1"),
            new Gegenstueck("Ergebnis.Waermelast_Max", "Ergebnis.Kaeltelast_Max", "KU1"),
            new Gegenstueck("Ergebnis.Waermebedarf_Gesamt", "Ergebnis.Kaeltebedarf_Gesamt", "KU1"),
            new Gegenstueck("Ergebnis.Waermerestbedarf", "Ergebnis.Kaelterestbedarf", "KU1"),
            new Gegenstueck("Stunden mit Wärmebedarf", "StundenMitKuehlbedarf", "KU1"),
            new Gegenstueck("Vollbenutzungsstunden Wärme", "VollbenutzungsstundenKaelte", "KU1"),
            new Gegenstueck("Ergebnis.Waermerestbedarf (Restwaerme nach der Kaskade)", "Kaelterestbedarf", "KU1",
                            "Nach der Kältekaskade ihr Rest (KU2); ohne Kälteerzeuger der ganze Bedarf, benannt als Warnung (F-K12)."),

            // ---- KU2: gebaut ----
            new Gegenstueck("Kaskadenschleife (Wärmekaskade)", "Kaskade", "KU2",
                            "Eigene Stundenschleife Kaeltekaskade nach der Wärmekaskade (5.5); Reihenfolge = Kaskadenplätze, gefiltert auf die Kälteerzeuger."),
            new Gegenstueck("KennzahlenKatalog.DeckungKanal", "KennzahlenKatalog.DeckungKanalKaelte", "KU2",
                            "Eigener Zweig mit Kaeltebedarf_Gesamt als Nenner, kein vierter Fall (6.4)."),
            new Gegenstueck("Jahresarbeitszahl der Wärmepumpe", "Kaeltekaskade.EerJahreswert", "KU2",
                            "Kälte / Kältestrom einschließlich Hilfsstrom (6.1, 6.4)."),
            new Gegenstueck("WP_Strombedarf_stuendlich", "Kaeltekaskade.Stromverbrauch_Kuehlung_stuendlich", "KU2",
                            "Eigene Reihe, nicht die der Wärmepumpe (5.1 Festlegung 5, 6.1)."),

            // ---- Benannte Abweichungen ----
            new Gegenstueck("Kanalsatz.NetzverlusteVerteilen", null, "—",
                            "Wärmenetzverluste gehören nicht in einen Kältekreis; Kältenetzverluste werden nicht gerechnet (4.2 b, Kapitel 14)."),
            new Gegenstueck("Waermebedarf_Netzverluste", null, "—",
                            "Keine Kältenetzverluste (4.2 b)."),
            new Gegenstueck("Knappheitsreihenfolge über die Wärmekanäle", null, "—",
                            "Ein Kältekanal braucht keine Rangfolge (4.5, 5.5)."),
            new Gegenstueck("Waermebedarf_Brauchwasser, Waermebedarf_Prozess", null, "—",
                            "Die Kälteseite hat einen Kanal; Brauchwasser und Prozess sind Wärmekanäle."),

            // ---- Zugewinne der Kälteseite (kein Fehlen auf der Wärmeseite, 6.4) ----
            new Gegenstueck(null, "StundenHeizenUndKuehlen", "KU1",
                            "Zugewinn (K6): misst Zonierung und Umschaltstunden, hängt an der Kühlung."),
            new Gegenstueck(null, "StundenHeizenUndKuehlenGebaeude", "KU1",
                            "Herkunftszeile der Kennzahl K6 (führendes Gebäude)."),
            new Gegenstueck(null, "GekuehlteGebaeude", "KU1",
                            "Zahl der Gebäude mit wirksamer Kühlung - die Kühlung ist je Gebäude schaltbar."),
            new Gegenstueck(null, "GebaeudeBestandsweg", "KU1",
                            "Bestandsweg-Gebäude mit Kältebedarf 0 und Hinweis (F-K18) - bis zur Stufe GA."),
            new Gegenstueck(null, "Gerechnet", "KU1",
                            "Die Kälte ist je Projekt schaltbar (K10); die Wärme wird immer erhoben."),
            new Gegenstueck(null, "GrenzeFeuchte", "KU1",
                            "Die Grenze der Kältezahl (K5): sensible Kälte ohne Entfeuchtung."),
            new Gegenstueck(null, "Überhitzungsstunden (Gebäudekennzahl)", "KU1",
                            "Zugewinn (6.4): misst, was ohne Anlage geschieht."),
            new Gegenstueck(null, "KaelteerzeugerAngelegt", "KU2",
                            "Angekündigte Kälteerzeuger: Die Unterdeckung meldet dann die Kältekaskade mit ihrem Grund (5.5)."),
            new Gegenstueck(null, "Kaeltekaskade.Kuehltage", "KU2",
                            "Tagesbetriebsart der reversiblen Maschine (K8a, 5.2) - die Wärmeseite kennt keine Umschaltung."),
        };
    }

    /// <summary>
    /// <b>Die Kernseite des Kanalexports der Kälte</b> (Stufe KU1; Kühlkonzept 4.7, 9.2; K17) —
    /// WAS der Ergebnisexport für den Kühlkanal schreibt: die Kanalreihe [kWh je Stunde] als
    /// <see cref="DATEI"/>, im Format jeder Vektordatei. Muster ist
    /// <see cref="GebaeudeErgebnisexport"/>: Der Kern legt Dateiname und Bedingung fest,
    /// <c>Referenzlauf/Ergebnisexport.cs</c> schreibt.
    ///
    /// <para><b>Nur wenn erhoben und nicht leer.</b> Die Datei entsteht allein für einen Lauf, der
    /// Kälte ERHOBEN hat und einen Kältebedarf &gt; 0 führt — „nicht mit Nullen gefüllt" (Muster
    /// Erdreichblock). Jedes Projekt ohne Kühlung, darunter alle Referenzprojekte, bekommt damit
    /// weder eine neue Datei noch einen neuen Summenschlüssel, und die Basis bleibt byte-gleich;
    /// eine unbedingte Datei wäre für jedes eingefrorene Projekt ein FAIL ohne Schalter (4.7).</para>
    ///
    /// <para><b>Die Grenze reist mit</b> (K5): <see cref="Grenze"/> ist der Satz, den der Export
    /// neben die Datei schreibt (Protokoll des Laufs, Kopfzeile des CSV-Exports der Oberfläche) —
    /// die Vektordatei selbst bleibt „Index;Wert".</para>
    ///
    /// <para>Öffentlich, weil der Referenzlauf den Kern ohne <c>InternalsVisibleTo</c> liest.</para>
    /// </summary>
    public static class KaelteErgebnisexport
    {
        /// <summary>Der Dateiname der Kanalreihe — nach dem Muster der Kanaldateien <c>waermebedarf_&lt;kanal&gt;.csv</c> (K17).</summary>
        public const string DATEI = "waermebedarf_kuehlung.csv";

        /// <summary>
        /// Die Kanalreihe unter ihrem Dateinamen — oder gar keine, wenn der Lauf keine Kälte
        /// erhoben hat oder sein Kältebedarf 0 ist.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, double[]>> Reihen(SimulationKaeltebedarf kaelte)
        {
            var reihen = new List<KeyValuePair<string, double[]>>();
            if (kaelte != null && kaelte.Gerechnet && kaelte.Kaeltebedarf_Gesamt > 0)
                reihen.Add(new KeyValuePair<string, double[]>(DATEI, kaelte.Kaeltebedarf));
            return reihen;
        }

        /// <summary>
        /// <b>Die Skalare der Kältedeckung</b> für die Kennzahlendatei <c>aggregate.csv</c>
        /// (Stufe KU2; Kühlkonzept 4.7, 7.4, 7.6 — der Kältestrom bleibt Skalar, K24/E27): gedeckte
        /// Kälte, Kältestrom samt Hilfsstrom, EER-Jahreswert, Deckungsgrad, Kühltage und je Erzeuger
        /// Kälte und Kältestrom; Jahressummen in MWh, die Einheit im Namen.
        ///
        /// <para><b>Nur mit gerechnetem Kälteerzeuger</b> — dieselbe Bedingung wie die Kanaldatei: Ein
        /// Projekt ohne Kühlbetrieb, darunter jedes Referenzprojekt bis zum Einfrierschritt von KU2,
        /// bekommt keinen neuen Schlüssel, und die Basis bleibt byte-gleich.</para>
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, double>> Skalare(SimulationKaeltebedarf kaelte)
        {
            var s = new List<KeyValuePair<string, double>>();
            Kaeltekaskade k = kaelte?.Kaskade;
            if (kaelte == null || !kaelte.Gerechnet || k == null || k.Erzeuger.Count == 0) return s;

            s.Add(new KeyValuePair<string, double>("Kaelte.ErzeugungMwh", k.DeckungGesamtKwh / 1000.0));
            s.Add(new KeyValuePair<string, double>("Kaelte.StromMwh", k.StromGesamtKwh / 1000.0));
            s.Add(new KeyValuePair<string, double>("Kaelte.HilfsstromMwh", k.HilfsstromGesamtKwh / 1000.0));
            s.Add(new KeyValuePair<string, double>("Kaelte.EerJahreswert", k.EerJahreswert));
            s.Add(new KeyValuePair<string, double>("Kaelte.DeckungsgradProzent",
                SimulationRunner.DeckungProzent(k.DeckungGesamtKwh / 1000.0, kaelte.Kaeltebedarf_Gesamt)));
            s.Add(new KeyValuePair<string, double>("Kaelte.Kuehltage", k.AnzahlKuehltage));
            for (int i = 0; i < k.Erzeuger.Count; i++)
            {
                Kaelteerzeuger e = k.Erzeuger[i];
                string p = "Kaelte[" + i.ToString(CultureInfo.InvariantCulture) + "].";
                s.Add(new KeyValuePair<string, double>(p + "ID_Anlage", e.AnlagenID));
                s.Add(new KeyValuePair<string, double>(p + "Vorlauf",
                    e.Maschine != null ? e.Maschine.Kaltwassertemperatur : (e.Kennlinie != null ? e.Kennlinie.Vorlauf : 0)));
                s.Add(new KeyValuePair<string, double>(p + "ErzeugungMwh", e.KaelteGesamtKwh / 1000.0));
                s.Add(new KeyValuePair<string, double>(p + "StromMwh", e.StromGesamtKwh / 1000.0));
                // KU3-2: die Kältemaschine trägt ihre Kennung, die freie Kühlung und den Hilfsstrom der
                // Rückkühlung - nur sie, die Schlüssel der Wärmepumpe bleiben unverändert.
                if (e.Maschine != null)
                {
                    s.Add(new KeyValuePair<string, double>(p + "ID_Kaeltemaschine", e.Maschine.Id));
                    s.Add(new KeyValuePair<string, double>(p + "HilfsstromMwh", e.HilfsstromGesamtKwh / 1000.0));
                    s.Add(new KeyValuePair<string, double>(p + "FreieKuehlungStunden", e.StundenFreieKuehlung));
                    s.Add(new KeyValuePair<string, double>(p + "FreieKuehlungMwh", e.KaelteFreiKwh / 1000.0));
                    // KM3 (Fachkonzept Teillast und Takten 5.3, 8.1): nur eine Maschine mit Teillast_Weg bzw. Randweg
                    // GUETEGRAD trägt die neuen Schlüssel; ohne Weg bleibt die Datei Zeichen für Zeichen wie zuvor.
                    if (e.Maschine.TeillastWirksam)
                    {
                        s.Add(new KeyValuePair<string, double>(p + "TaktstromMwh", e.TaktstromKwh / 1000.0));
                        s.Add(new KeyValuePair<string, double>(p + "Starts", e.Starts));
                        s.Add(new KeyValuePair<string, double>(p + "Taktstunden", e.Taktstunden));
                        s.Add(new KeyValuePair<string, double>(p + "Teillaststunden", e.StundenTeillast));
                        s.Add(new KeyValuePair<string, double>(p + "LastgradMittel", e.LastgradMittel));
                    }
                    if (e.Maschine.GuetegradWirksam)
                        s.Add(new KeyValuePair<string, double>(p + "StundenExtrapoliert", e.StundenExtrapoliert));
                }
                // KU3-6: die Wärmepumpe mit wirksamer freier Kühlung über die Wärmequelle - eigene
                // Schlüssel; ohne sie bleibt die Datei Zeichen für Zeichen wie zuvor.
                else if (e.FreieKuehlungSole)
                {
                    s.Add(new KeyValuePair<string, double>(p + "WpFreieKuehlungStunden", e.StundenFreieKuehlung));
                    s.Add(new KeyValuePair<string, double>(p + "WpFreieKuehlungMwh", e.KaelteFreiKwh / 1000.0));
                }
            }
            // KU3-5: die Kältespeicher - nur, wenn einer rechnet; sonst kein neuer Schlüssel.
            if (k.Speicher != null)
                for (int i = 0; i < k.Speicher.Count; i++)
                {
                    SimulationPufferspeicher sp = k.Speicher[i];
                    string p = "Kaeltespeicher[" + i.ToString(CultureInfo.InvariantCulture) + "].";
                    s.Add(new KeyValuePair<string, double>(p + "ID_Pufferspeicher", sp.ID_Pufferspeicher));
                    s.Add(new KeyValuePair<string, double>(p + "KapazitaetKwh", sp.Q_max));
                    s.Add(new KeyValuePair<string, double>(p + "EntladungMwh", sp.Entladung_gesamt / 1000.0));
                    s.Add(new KeyValuePair<string, double>(p + "LadungMwh", sp.Ladung_gesamt / 1000.0));
                    s.Add(new KeyValuePair<string, double>(p + "VerlusteMwh", sp.Verluste_gesamt / 1000.0));
                    s.Add(new KeyValuePair<string, double>(p + "Vollzyklen", sp.Vollzyklen));
                }
            return s;
        }

        /// <summary>Der Satz, der mit jeder exportierten Kältezahl reist (K5): sensible Kälte ohne Entfeuchtung.</summary>
        public static string Grenze => SimulationKaeltebedarf.GrenzeFeuchte;
    }
}
