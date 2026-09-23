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
    /// <para><b>Gedeckt von niemandem — benannt.</b> Kälteerzeuger kommen mit Stufe KU2. Bis dahin
    /// ist die ungedeckte Kälte (<see cref="Kaelterestbedarf"/>) der ganze Bedarf, und der Lauf
    /// sagt es als Warnung (F-K12, Kühlkonzept 5.5): kein stiller Rest.</para>
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
        /// Ungedeckte Kälte [MWh] — Zwilling von <c>Waermerestbedarf</c>. In KU1 deckt NIEMAND
        /// Kälte: Sie ist der ganze Bedarf (benannt, F-K12). Ab KU2 bildet sie die
        /// Kältekaskade dieser Fassade.
        /// </summary>
        public double Kaelterestbedarf = 0;

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
            Array.Clear(_probe, 0, STUNDEN);

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
        }

        /// <summary>
        /// Ein externer Lastgang mit dem Kanal „Kuehlung" (K3, F-K5) — Kältebedarf ohne
        /// Gebäudemodell. Er geht NIE in einen Wärmekanal und nicht in die Energieprobe der
        /// Wärmeseite (4.2), sondern hierher und in die Bedarfsprobe Kälte. Rechnet das Projekt
        /// keine Kälte, bleibt er unberücksichtigt, und der Lauf sagt es.
        /// </summary>
        /// <param name="werte">Die 8 760 Stundenwerte [kWh].</param>
        internal void GanglinieBuchen(double[] werte)
        {
            if (werte == null) return;
            _kaelteGanglinien++;
            if (!_kuehlbetrieb || _kanaele == null) return;

            double[] kanal = _kanaele.Kuehlung;
            double summe = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                kanal[h] = ((double)kanal[h] + werte[h]);
                Kaeltebedarf_Extern[h] = ((double)Kaeltebedarf_Extern[h] + werte[h]);
                _probe[h] += werte[h];
                summe += werte[h];
            }
            Kaeltebedarf_Extern_Gesamt += summe / 1000;
        }

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
                if (_gebaeudeMitKuehlung > 0 || _kaelteGanglinien > 0)
                    protokoll.HinweisEinmal("kuehlung-projekt-aus",
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_PROJEKT_AUS,
                                      _gebaeudeMitKuehlung, _kaelteGanglinien));
                return true;
            }

            double[] summe = _kanaele.SummeKaelte();
            Array.Copy(summe, Kaeltebedarf, STUNDEN);

            if (!Bedarfsprobe(summe)) return false;

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

            // KU1: Kälteerzeuger gibt es noch nicht - die ungedeckte Kälte ist der Bedarf.
            Kaelterestbedarf = Kaeltebedarf_Gesamt;
            Gerechnet = true;

            if (Kaeltebedarf_Gesamt > 0)
            {
                // F-K12: Unterdeckung ist eine benannte Meldung, kein stiller Rest.
                protokoll.Warnung(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMENG_KAELTE_OHNE_ERZEUGER,
                    Kaeltebedarf_Gesamt.ToString("N2", CultureInfo.CurrentCulture),
                    Kaeltebedarf_Max.ToString("N1", CultureInfo.CurrentCulture)));

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
                            "In KU1 gedeckt von niemandem: der ganze Bedarf, benannt als Warnung (F-K12)."),

            // ---- KU2: angekündigt ----
            new Gegenstueck("KennzahlenKatalog.DeckungKanal", "KennzahlenKatalog.DeckungKanalKaelte", "KU2",
                            "Eigener Zweig mit Kaeltebedarf_Gesamt als Nenner, kein vierter Fall (6.4)."),
            new Gegenstueck("Jahresarbeitszahl der Wärmepumpe", "Jahresarbeitszahl Kälte", "KU2",
                            "Kommt mit dem Kälteerzeuger (reversible Wärmepumpe)."),

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

        /// <summary>Der Satz, der mit jeder exportierten Kältezahl reist (K5): sensible Kälte ohne Entfeuchtung.</summary>
        public static string Grenze => SimulationKaeltebedarf.GrenzeFeuchte;
    }
}
