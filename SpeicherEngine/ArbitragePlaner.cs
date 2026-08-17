using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Rolling-Horizon-Greedy ueber 24-Stunden-Fenster mit Day-Ahead-Preisvoraussicht
    /// (Fachkonzept 6.5, Arbeitspaket AP10). Erzeugt den <see cref="ArbitragePlan"/>,
    /// den die <see cref="Arbitrage"/> anschliessend abfaehrt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum neu und nicht portiert.</b> Die Arbitragelogik der V7-Mappe war zur
    /// Laufzeit nicht ausfuehrbar, und zwei Konstruktionsfehler legten den Planer nach
    /// dem ersten Tag stillt: Der Fenster-Ladezustand wurde nie um die geplante
    /// Entladung reduziert (Fehler G3), und der Commit-Schritt verwarf systematisch
    /// die Abend-Entladeslots. Beides ist hier konstruktiv ausgeschlossen - der Planer
    /// <b>simuliert nach jeder Paarung den kompletten Ladezustandspfad des Fensters
    /// neu</b> und nimmt die Paarung nur an, wenn dieser Pfad zulaessig bleibt. Ein
    /// stilles Klemmen auf die Bandgrenzen gibt es nicht; wo geklemmt werden muesste,
    /// wird verworfen.
    /// </para>
    /// <para><b>Ablauf je Fenster</b> (96 Intervalle, das letzte kuerzer):</para>
    /// <list type="number">
    ///   <item><description><b>Grundlauf.</b> Das Fenster wird ohne jeden Netzpfad
    ///     simuliert - mit dem Ladezustand, den das Vorfenster hinterlassen hat. Daraus
    ///     ergibt sich, in welchen Intervallen der Eigenverbrauchsfluss "sonst nichts
    ///     tut" (Fachkonzept 6.2, Erweiterungsbloecke).</description></item>
    ///   <item><description><b>Paarung</b> (nur bei zugelassener Netzladung, also im
    ///     Graustrombetrieb). Guenstigster Ladeslot x teuerster Verkaufsslot
    ///     <b>nach</b> ihm; angenommen nur, wenn
    ///     <c>Erloes(t_e) - p_netzlade(t_l)/eta_RT - k_ver &gt; 0</c>
    ///     ist.</description></item>
    ///   <item><description><b>Ungepaarter Verkauf</b> aus vorhandenem Ladezustand -
    ///     der Fall "Gruenstrom + Netzentladung" (Fachkonzept 2.1); die dort gesetzte
    ///     Rentabilitaetsregel steht bei <c>Lauf.BesterVerkaufsslot</c>.</description></item>
    ///   <item><description><b>Vollstaendige Uebernahme.</b> Das Fenster wird ganz
    ///     uebernommen, nicht nur ein Viertel; sein Endladezustand ist der Startwert des
    ///     naechsten Fensters.</description></item>
    /// </list>
    /// <para>
    /// <b>Deterministische Reduktionsregel bei Pfadverletzung.</b> Die Energiemenge
    /// eines Kandidaten wird zuerst <i>analytisch</i> auf das reduziert, was der
    /// aktuell geprueft-zulaessige Pfad hergibt (Leistungsgrenze, SoC-Kopf bzw. Band,
    /// Restbudget). Bleibt danach nichts uebrig, wird der <b>engpassbildende Slot</b>
    /// fuer dieses Fenster gesperrt - bei der Paarung der Ladeslot (dort fehlt der
    /// SoC-Kopf), beim ungepaarten Verkauf der Verkaufsslot. Ist die Menge groesser 0,
    /// der <b>vollstaendig neu simulierte Pfad</b> aber trotzdem unzulaessig - das kann
    /// nur ueber die Rueckwirkung auf schon geplante Netzpfade oder auf den
    /// Eigenverbrauchsfluss passieren -, wird der Kandidat <b>ganz verworfen</b> und
    /// sein Verkaufsslot gesperrt. Damit endet die Schleife garantiert (jede Runde nimmt
    /// einen Kandidaten an oder sperrt einen Slot, beides begrenzt durch die
    /// Fensterlaenge), und die Reihenfolge der Entscheidungen haengt nur von den Daten
    /// ab, nicht von der Laufzeit.
    /// </para>
    /// <para>
    /// <b>Eigenverbrauch hat Vorrang</b> (Fachkonzept 2.2, Entladeprioritaet 1 vor 2).
    /// Das wird nicht nur je Intervall durchgesetzt (Netzpfade greifen nur, wo der
    /// Eigenverbrauchsfluss nichts tut), sondern auch ueber das Fenster: Eine Paarung,
    /// die die Eigenverbrauchs-Lade- oder -Entladeenergie des Fensters senkt, gilt als
    /// Pfadverletzung und wird verworfen.
    /// </para>
    /// <para>
    /// Die Instanz haelt keinen Zustand - der gesamte Lauf steckt in einem privaten
    /// <c>Lauf</c>-Objekt. Dieselbe Planerinstanz darf deshalb mehrfach und nebenlaeufig
    /// verwendet werden (Fachkonzept 8.1).
    /// </para>
    /// </remarks>
    public sealed class ArbitragePlaner
    {
        /// <summary>
        /// Sicherheitsnetz gegen Endlosschleifen: hoechstens so viele Planungsrunden je
        /// Fenster wie Intervalle mal diesem Faktor.
        /// </summary>
        /// <remarks>
        /// Der Wert ist rechnerisch nie bindend - jede Runde nimmt einen Kandidaten an
        /// (verbraucht mindestens einen Slot) oder sperrt einen Verkaufsslot, und beides
        /// ist durch die Fensterlaenge begrenzt. Er steht hier nur, damit eine kuenftige
        /// Aenderung an der Kandidatenauswahl nicht unbemerkt zur Dauerschleife wird.
        /// </remarks>
        public const int MaxRundenFaktor = 2;

        /// <summary>Plant die Netzpfade eines Jahreslaufs.</summary>
        /// <param name="eingang">Zeitreihen Last, Erzeugung, Bezugspreis.</param>
        /// <param name="p">Speicherparameter (Band, Leistung, Wirkungsgrade, c_ver).</param>
        /// <param name="optionen">Preisreihen und Schalter der Preissteuerung.</param>
        /// <returns>
        /// Der Plan; bei abgeschalteten Netzpfaden oder unbrauchbarem Band ein leerer
        /// Plan (<see cref="ArbitragePlan.Leer"/>).
        /// </returns>
        /// <exception cref="ArgumentNullException">Wenn ein Argument <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Wenn die Optionsreihen nicht zum Eingang passen.</exception>
        public ArbitragePlan Plane(SpeicherEingang eingang, SpeicherParameter p, ArbitrageOptionen optionen)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (optionen == null) throw new ArgumentNullException(nameof(optionen));
            if (optionen.Anzahl != eingang.Anzahl)
                throw new ArgumentException(
                    "Die Preisreihen der Preissteuerung muessen so lang sein wie die Eingangsreihen.",
                    nameof(optionen));

            p.Pruefe();

            // Ohne Netzpfad und ohne nutzbares Band gibt es nichts zu planen; die
            // Arbitrage rechnet dann bitgleich zur Dauernutzung.
            if (!optionen.HatNetzpfad || !(p.CNutzKwh > 0.0) || !(p.PKw > 0.0))
                return ArbitragePlan.Leer(eingang.Anzahl);

            return new Lauf(eingang, p, optionen).Plane();
        }

        // ==================================================================
        // Ein Planungslauf
        // ==================================================================

        private sealed class Lauf
        {
            // --- Eingang ---
            private readonly double[] _pBezug;
            private readonly double[] _pNetz;
            private readonly double[] _erloes;
            private readonly IntervallEnergien[] _energien;
            private readonly int _n;

            // --- Parameter ---
            private readonly double _dt;
            private readonly double _maxPower;
            private readonly double _socMin;
            private readonly double _socMax;
            private readonly double _etaCh;
            private readonly double _etaDis;
            private readonly double _etaRt;
            private readonly double _startSoC;

            // --- Optionen ---
            private readonly bool _netzladung;
            private readonly bool _netzentladung;
            private readonly double _schwelle;
            private readonly double _budget;
            private readonly double _reserve;
            private readonly int _fensterLaenge;
            private readonly double _kVer;

            // --- Toleranzen ---
            private readonly double _tolSoC;
            private readonly double _tolEnergie;

            // --- Ergebnis ---
            private readonly double[] _netzCap;
            private readonly double[] _verkCap;

            // --- Fensterpuffer ---
            private readonly double[] _socVor;
            private readonly double[] _socNach;
            private readonly double[] _eigenCh;
            private readonly double[] _eigenDis;
            private readonly double[] _netzCh;
            private readonly double[] _verk;
            private readonly double[] _suffixMaxBezug;
            private readonly bool[] _gesperrtVerkauf;
            private readonly bool[] _gesperrtLaden;

            private double _eigenChSumme;
            private double _eigenDisSumme;
            private double _dcSumme;

            private double _refEigenCh;
            private double _refEigenDis;

            private double _dcFestgeschrieben;

            private int _paare;
            private int _verkaufsslots;
            private int _verworfenPfad;
            private int _verworfenOhneEnergie;
            private int _fensteranzahl;
            private bool _budgetErschoepft;

            internal Lauf(SpeicherEingang eingang, SpeicherParameter p, ArbitrageOptionen o)
            {
                _n = eingang.Anzahl;
                _pBezug = eingang.PreisCtKwh;
                _pNetz = o.NetzladepreisCtKwh;
                _erloes = o.ErloesCtKwh;

                _dt = p.DtH;
                _maxPower = p.PKw;
                _socMin = p.SoCMinKwh;
                _socMax = p.SoCMaxKwh;
                _etaCh = p.EtaCh;
                _etaDis = p.EtaDis;
                _etaRt = p.RoundTripWirkungsgrad;
                _startSoC = p.StartSoCEffektivKwh;

                _netzladung = o.Netzladung;
                _netzentladung = o.Netzentladung;
                _schwelle = o.LadeschwellwertCtKwh;
                _budget = o.ZyklenbudgetDcKwhProA;
                _reserve = o.ReservepufferKwh;
                _fensterLaenge = o.FensterIntervalle;
                _kVer = ArbitrageOptionen.VerschleissCtKwh(p);

                _tolSoC = 1e-9 * Math.Max(1.0, p.CNutzKwh);
                _tolEnergie = 1e-9 * Math.Max(1.0, _maxPower * _dt);

                // Die Zerlegung je Intervall haengt nicht vom Plan ab - sie wird einmal
                // gerechnet statt in jeder Probesimulation erneut.
                _energien = new IntervallEnergien[_n];
                double[] last = eingang.LastKw;
                double[] pv = eingang.PvKw;
                double[]? bhkw = eingang.BhkwKw;
                for (int i = 0; i < _n; i++)
                {
                    _energien[i] = Vorverarbeitung.Berechne(
                        last[i], pv[i], bhkw == null ? 0.0 : bhkw[i], _dt,
                        p.PvZulaessig, p.BhkwUeberschussZulaessig);
                }

                _netzCap = new double[_n];
                _verkCap = new double[_n];

                int puffer = Math.Min(_fensterLaenge, _n);
                _socVor = new double[puffer];
                _socNach = new double[puffer];
                _eigenCh = new double[puffer];
                _eigenDis = new double[puffer];
                _netzCh = new double[puffer];
                _verk = new double[puffer];
                _suffixMaxBezug = new double[puffer];
                _gesperrtVerkauf = new bool[puffer];
                _gesperrtLaden = new bool[puffer];
            }

            internal ArbitragePlan Plane()
            {
                double soc = _startSoC;

                for (int start = 0; start < _n; start += _fensterLaenge)
                {
                    int laenge = Math.Min(_fensterLaenge, _n - start);
                    _fensteranzahl++;

                    PlaneFenster(start, laenge, soc);

                    // Vollstaendige Uebernahme (Fachkonzept 6.5): Der Endladezustand des
                    // Fensters ist der Startwert des naechsten - kein Teil-Commit.
                    soc = _socNach[laenge - 1];
                    _dcFestgeschrieben += _dcSumme;

                    if (BudgetErschoepft(0.0))
                    {
                        // "Bei erschoepftem Budget endet die Planung" (6.5). Der
                        // Eigenverbrauchsfluss laeuft danach unveraendert weiter - er
                        // wird nicht geplant.
                        _budgetErschoepft = true;
                        break;
                    }
                }

                return new ArbitragePlan(_netzCap, _verkCap, _fensteranzahl, _paare, _verkaufsslots,
                                         _verworfenPfad, _verworfenOhneEnergie, _budget,
                                         _budgetErschoepft, _kVer);
            }

            // --------------------------------------------------------------
            // Ein Fenster
            // --------------------------------------------------------------

            private void PlaneFenster(int start, int laenge, double socStart)
            {
                Sperren(laenge);

                // Grundlauf ohne Netzpfade - er legt fest, wo "sonst nichts passiert".
                Simuliere(start, laenge, socStart);
                _refEigenCh = _eigenChSumme;
                _refEigenDis = _eigenDisSumme;

                if (_netzladung && _netzentladung) PhasePaarung(start, laenge, socStart);

                if (_netzentladung)
                {
                    // Frische Sperrliste fuer die zweite Phase: Ein Verkaufsslot, der als
                    // Partner einer Paarung nicht taugte, kann als ungepaarter Verkauf
                    // sehr wohl tragen - die Ablehnung galt der Paarung, nicht dem Slot.
                    Sperren(laenge);
                    PhaseVerkauf(start, laenge, socStart);
                }
            }

            private void Sperren(int laenge)
            {
                for (int k = 0; k < laenge; k++)
                {
                    _gesperrtVerkauf[k] = false;
                    _gesperrtLaden[k] = false;
                }
            }

            /// <summary>
            /// Paarung nach Fachkonzept 6.5: guenstigster Ladeslot x teuerster
            /// Verkaufsslot dahinter, gepruefte Rentabilitaet, gepruefter Pfad.
            /// </summary>
            private void PhasePaarung(int start, int laenge, double socStart)
            {
                int maxRunden = laenge * MaxRundenFaktor;

                for (int runde = 0; runde < maxRunden; runde++)
                {
                    if (BudgetErschoepft(_dcSumme)) return;

                    int l, e;
                    double spread;
                    if (!BestesPaar(start, laenge, out l, out e, out spread)) return;

                    // Rentabilitaetsbedingung je ausgespeister kWh AC (6.5). k_ver ist
                    // hier NICHT abschaltbar (5.4, Verwendung 1). Ist schon das beste
                    // Paar unrentabel, ist es kein schlechteres auch - Abbruch.
                    if (!(spread - _kVer > 0.0)) return;

                    double eCh = _maxPower * _dt;
                    double kopf = (_socMax - _socVor[l]) / _etaCh;
                    if (eCh > kopf) eCh = kopf;

                    // Budget: die Entladung eDis = eCh*eta_RT kostet eDis/eta_dis
                    // DC-Entnahme, also eCh*eta_ch.
                    double restDc = RestBudgetDc(_dcSumme);
                    double grenze = restDc / _etaCh;
                    if (eCh > grenze) eCh = grenze;
                    if (eCh < 0.0) eCh = 0.0;

                    if (eCh <= _tolEnergie)
                    {
                        // Hier fehlt der SoC-Kopf am LADESLOT, nicht die Energie am
                        // Verkaufsslot - gesperrt wird deshalb der Ladeslot. Der
                        // Verkaufsslot bleibt fuer einen anderen Partner verfuegbar.
                        _gesperrtLaden[l] = true;
                        _verworfenOhneEnergie++;
                        continue;
                    }

                    // Die Ladung deckt ihre eigene Entladung: eDis = eCh*eta_RT liegt
                    // konstruktiv im Band, weil der Pfad bis hierher zulaessig ist.
                    double eDis = eCh * _etaRt;

                    _netzCap[start + l] = eCh;
                    _verkCap[start + e] = eDis;
                    Simuliere(start, laenge, socStart);

                    if (PfadZulaessig(start, laenge))
                    {
                        _paare++;
                        _refEigenCh = _eigenChSumme;
                        _refEigenDis = _eigenDisSumme;
                    }
                    else
                    {
                        _netzCap[start + l] = 0.0;
                        _verkCap[start + e] = 0.0;
                        Simuliere(start, laenge, socStart);
                        _gesperrtVerkauf[e] = true;
                        _verworfenPfad++;
                    }
                }
            }

            /// <summary>
            /// Ungepaarter Verkauf aus vorhandenem Ladezustand (Fachkonzept 2.1:
            /// "auch ein Gruenstromspeicher darf verkaufen").
            /// </summary>
            private void PhaseVerkauf(int start, int laenge, double socStart)
            {
                SuffixMaxBezug(start, laenge);
                double netzTeil = HoechsterGeplanterNetzladepreis(start, laenge);
                int maxRunden = laenge * MaxRundenFaktor;

                for (int runde = 0; runde < maxRunden; runde++)
                {
                    if (BudgetErschoepft(_dcSumme)) return;

                    int e;
                    double vorteil;
                    if (!BesterVerkaufsslot(start, laenge, netzTeil, out e, out vorteil)) return;
                    if (!(vorteil - _kVer > 0.0)) return;

                    double eDis = _maxPower * _dt;
                    double band = (_socVor[e] - _socMin - _reserve) * _etaDis;
                    if (eDis > band) eDis = band;

                    double grenze = RestBudgetDc(_dcSumme) * _etaDis;
                    if (eDis > grenze) eDis = grenze;
                    if (eDis < 0.0) eDis = 0.0;

                    if (eDis <= _tolEnergie)
                    {
                        _gesperrtVerkauf[e] = true;
                        _verworfenOhneEnergie++;
                        continue;
                    }

                    _verkCap[start + e] = eDis;
                    Simuliere(start, laenge, socStart);

                    if (PfadZulaessig(start, laenge))
                    {
                        _verkaufsslots++;
                        _refEigenCh = _eigenChSumme;
                        _refEigenDis = _eigenDisSumme;
                    }
                    else
                    {
                        _verkCap[start + e] = 0.0;
                        Simuliere(start, laenge, socStart);
                        _gesperrtVerkauf[e] = true;
                        _verworfenPfad++;
                    }
                }
            }

            // --------------------------------------------------------------
            // Kandidatenauswahl
            // --------------------------------------------------------------

            /// <summary>
            /// Ladeslot: Netzladung zugelassen, im Intervall passiert sonst nichts
            /// (Fachkonzept 6.2), noch kein Netzpfad geplant, und der manuelle
            /// Ladeschwellwert ist eingehalten (5.6).
            /// </summary>
            private bool IstLadeslot(int start, int k)
            {
                if (!_netzladung || _gesperrtLaden[k]) return false;
                if (_eigenCh[k] != 0.0 || _eigenDis[k] != 0.0) return false;
                int i = start + k;
                if (_netzCap[i] != 0.0 || _verkCap[i] != 0.0) return false;
                return !(_schwelle > 0.0) || _pNetz[i] <= _schwelle;
            }

            /// <summary>
            /// Verkaufsslot: Netzentladung zugelassen, im Intervall passiert sonst
            /// nichts, noch kein Netzpfad geplant, nicht gesperrt.
            /// </summary>
            /// <remarks>
            /// Der Pseudocode 6.2 verlangt fuer den Verkauf nur <c>E_ac_dis == 0</c>.
            /// Hier kommt <c>E_ac_ch == 0</c> hinzu - <b>bewusste Verschaerfung</b>:
            /// Laden und Entladen schliessen einander in dieser Engine je Intervall aus
            /// (<see cref="SpeicherErgebnis.EntladungAcKwh"/>), und die Leistungsgrenze
            /// P*dt gilt je Richtung. Ohne die Zusatzbedingung koennte ein Intervall
            /// gleichzeitig aus dem PV-Ueberschuss laden und aus dem Speicher verkaufen -
            /// physikalisch unsinnig und in der Bilanz nicht mehr trennbar.
            /// </remarks>
            private bool IstVerkaufsslot(int start, int k)
            {
                if (!_netzentladung || _gesperrtVerkauf[k]) return false;
                if (_eigenCh[k] != 0.0 || _eigenDis[k] != 0.0) return false;
                int i = start + k;
                return _netzCap[i] == 0.0 && _verkCap[i] == 0.0;
            }

            /// <summary>
            /// Bestes Paar (Ladeslot vor Verkaufsslot) nach dem groessten Rohspread
            /// <c>Erloes(t_e) - p_netzlade(t_l)/eta_RT</c> [ct/kWh].
            /// </summary>
            /// <remarks>
            /// Ein Durchlauf mit mitgefuehrtem guenstigsten Ladeslot - dadurch ist die
            /// Bedingung "Ladung zeitlich VOR der Entladung" konstruktiv erfuellt und die
            /// Suche linear statt quadratisch. Gleichstaende entscheidet der kleinere
            /// Index (erst Verkaufsslot, dann Ladeslot); die Auswahl haengt damit nur von
            /// den Daten ab.
            /// </remarks>
            private bool BestesPaar(int start, int laenge, out int lBest, out int eBest, out double spreadBest)
            {
                lBest = -1;
                eBest = -1;
                spreadBest = double.NegativeInfinity;

                int lauf = -1;   // Fensterindex des bisher guenstigsten Ladeslots

                for (int k = 0; k < laenge; k++)
                {
                    if (lauf >= 0 && IstVerkaufsslot(start, k))
                    {
                        double spread = _erloes[start + k] - _pNetz[start + lauf] / _etaRt;
                        if (spread > spreadBest)
                        {
                            spreadBest = spread;
                            lBest = lauf;
                            eBest = k;
                        }
                    }

                    if (IstLadeslot(start, k) && (lauf < 0 || _pNetz[start + k] < _pNetz[start + lauf]))
                        lauf = k;
                }

                return lBest >= 0;
            }

            /// <summary>
            /// Bester ungepaarter Verkaufsslot nach dem Vorteil gegenueber der
            /// <b>entgangenen Eigenverbrauchsnutzung</b> [ct/kWh].
            /// </summary>
            /// <remarks>
            /// <para>
            /// <b>Gesetzter Default.</b> Fuer den ungepaarten Verkauf nennt das
            /// Fachkonzept keine Rentabilitaetsbedingung - 6.5 bewertet nur Paare. Die
            /// hier gesetzte Regel ist bewusst konservativ: Verglichen wird der Erloes
            /// mit dem <b>hoechsten Bezugspreis, der im restlichen Fenster noch
            /// vermieden werden koennte</b>, und - falls im Fenster Netzladung geplant
            /// ist - zusaetzlich mit deren teuerster kWh je ausgespeister kWh
            /// (<c>p_netzlade/eta_RT</c>). Verkauft wird also nur, was selbst gegen die
            /// beste denkbare Alternativverwendung noch traegt, und auch dann erst nach
            /// Abzug von k_ver. Damit bleibt die Entladeprioritaet aus 2.2
            /// (Eigenverbrauch vor Verkauf) gewahrt, ohne dass die Ausnahme
            /// "Spotpreisspitze" verbaut waere.
            /// </para>
            /// <para>
            /// Der Vergleichspreis stammt aus dem <b>Fenster</b>, nicht aus dem
            /// Restjahr: Die Voraussicht des Verfahrens endet mit dem
            /// Day-Ahead-Horizont.
            /// </para>
            /// </remarks>
            private bool BesterVerkaufsslot(int start, int laenge, double netzTeil, out int eBest, out double vorteilBest)
            {
                eBest = -1;
                vorteilBest = double.NegativeInfinity;

                for (int k = 0; k < laenge; k++)
                {
                    if (!IstVerkaufsslot(start, k)) continue;

                    double referenz = _suffixMaxBezug[k];
                    double ausNetzladung = netzTeil / _etaRt;
                    if (ausNetzladung > referenz) referenz = ausNetzladung;

                    double vorteil = _erloes[start + k] - referenz;
                    if (vorteil > vorteilBest)
                    {
                        vorteilBest = vorteil;
                        eBest = k;
                    }
                }

                return eBest >= 0;
            }

            /// <summary>
            /// Hoechster Bezugspreis <b>nach</b> dem jeweiligen Intervall im Fenster;
            /// im letzten Intervall der eigene Preis (dort gibt es keine spaetere
            /// Alternative mehr).
            /// </summary>
            private void SuffixMaxBezug(int start, int laenge)
            {
                _suffixMaxBezug[laenge - 1] = _pBezug[start + laenge - 1];
                for (int k = laenge - 2; k >= 0; k--)
                {
                    double spaeter = _pBezug[start + k + 1];
                    double folge = _suffixMaxBezug[k + 1];
                    _suffixMaxBezug[k] = spaeter > folge ? spaeter : folge;
                }
            }

            /// <summary>
            /// Hoechster Netzladepreis unter den im Fenster bereits geplanten Ladeslots,
            /// oder <c>-Unendlich</c>, wenn keiner geplant ist.
            /// </summary>
            private double HoechsterGeplanterNetzladepreis(int start, int laenge)
            {
                double hoechster = double.NegativeInfinity;
                for (int k = 0; k < laenge; k++)
                {
                    int i = start + k;
                    if (_netzCap[i] > 0.0 && _pNetz[i] > hoechster) hoechster = _pNetz[i];
                }
                return hoechster;
            }

            // --------------------------------------------------------------
            // Fenstersimulation und Pfadpruefung
            // --------------------------------------------------------------

            /// <summary>
            /// Simuliert das Fenster mit dem aktuellen Plan.
            /// </summary>
            /// <remarks>
            /// <b>Ausdruck fuer Ausdruck derselbe Dispatch</b> wie
            /// <c>Arbitrage.BerechneEnergetisch</c> (dort steht die Fundstelle der
            /// Vorlage). Nur so ist die Zusage haltbar, dass die Strategie den Plan
            /// spaeter ohne Klemmen abfaehrt; wer eine der beiden Schleifen aendert,
            /// muss die andere mitziehen.
            /// </remarks>
            private void Simuliere(int start, int laenge, double socStart)
            {
                double prev = socStart;
                double eigenCh = 0.0;
                double eigenDis = 0.0;
                double dc = 0.0;

                for (int k = 0; k < laenge; k++)
                {
                    int i = start + k;
                    IntervallEnergien e = _energien[i];

                    double charge = 0.0;
                    double discharge = 0.0;

                    if (e.EQuelleKwh > 0.0)
                    {
                        charge = e.EQuelleKwh;
                        if (charge > _maxPower * _dt) charge = _maxPower * _dt;
                        if (charge > (_socMax - prev) / _etaCh) charge = (_socMax - prev) / _etaCh;
                        if (charge < 0) charge = 0.0;
                    }
                    else
                    {
                        discharge = e.EDefizitKwh;
                        if (discharge > _maxPower * _dt) discharge = _maxPower * _dt;
                        if (discharge > (prev - _socMin) * _etaDis) discharge = (prev - _socMin) * _etaDis;
                        if (discharge < 0) discharge = 0.0;
                    }

                    double chNetz = 0.0;
                    double verkauf = 0.0;

                    if (charge == 0.0 && discharge == 0.0)
                    {
                        if (_netzCap[i] > 0.0)
                        {
                            chNetz = _maxPower * _dt;
                            if (chNetz > (_socMax - prev) / _etaCh) chNetz = (_socMax - prev) / _etaCh;
                            if (chNetz > _netzCap[i]) chNetz = _netzCap[i];
                            if (chNetz < 0) chNetz = 0.0;
                        }
                        else if (_verkCap[i] > 0.0)
                        {
                            verkauf = _maxPower * _dt;
                            if (verkauf > (prev - _socMin - _reserve) * _etaDis) verkauf = (prev - _socMin - _reserve) * _etaDis;
                            if (verkauf > _verkCap[i]) verkauf = _verkCap[i];
                            if (verkauf < 0) verkauf = 0.0;
                        }
                    }

                    double newLevel = prev + charge * _etaCh - discharge / _etaDis;
                    if (chNetz > 0.0) newLevel += chNetz * _etaCh;
                    if (verkauf > 0.0) newLevel -= verkauf / _etaDis;

                    _socVor[k] = prev;
                    _socNach[k] = newLevel;
                    _eigenCh[k] = charge;
                    _eigenDis[k] = discharge;
                    _netzCh[k] = chNetz;
                    _verk[k] = verkauf;

                    eigenCh += charge;
                    eigenDis += discharge;
                    dc += discharge / _etaDis;
                    if (verkauf > 0.0) dc += verkauf / _etaDis;

                    prev = newLevel;
                }

                _eigenChSumme = eigenCh;
                _eigenDisSumme = eigenDis;
                _dcSumme = dc;
            }

            /// <summary>
            /// Prueft den <b>gesamten</b> Ladezustandspfad des Fensters - die Antwort
            /// auf den V7-Fehler G3.
            /// </summary>
            /// <remarks>
            /// Vier Bedingungen, jede einzeln ein Ablehnungsgrund:
            /// <list type="number">
            ///   <item><description>Der Ladezustand bleibt in jedem Intervall im Band
            ///     [SoC_min, SoC_max].</description></item>
            ///   <item><description>Die Leistungsgrenze P*dt haelt je Richtung.</description></item>
            ///   <item><description><b>Kein Klemmen:</b> Jeder geplante Netzpfad wird in
            ///     voller geplanter Hoehe gefahren. Wird er beschnitten, ist der Plan
            ///     nicht mehr das, was geprueft wurde - genau der Fehler, den die
            ///     V7-Mappe still hingenommen hat.</description></item>
            ///   <item><description>Der Eigenverbrauchsfluss des Fensters wird nicht
            ///     kleiner (Vorrang, Fachkonzept 2.2).</description></item>
            /// </list>
            /// </remarks>
            private bool PfadZulaessig(int start, int laenge)
            {
                double grenze = _maxPower * _dt + _tolEnergie;

                for (int k = 0; k < laenge; k++)
                {
                    if (_socNach[k] < _socMin - _tolSoC) return false;
                    if (_socNach[k] > _socMax + _tolSoC) return false;

                    if (_eigenCh[k] + _netzCh[k] > grenze) return false;
                    if (_eigenDis[k] + _verk[k] > grenze) return false;

                    int i = start + k;
                    if (_netzCap[i] > 0.0 && _netzCh[k] < _netzCap[i] - _tolEnergie) return false;
                    if (_verkCap[i] > 0.0 && _verk[k] < _verkCap[i] - _tolEnergie) return false;
                }

                if (_eigenChSumme < _refEigenCh - _tolEnergie) return false;
                if (_eigenDisSumme < _refEigenDis - _tolEnergie) return false;

                return !BudgetUeberschritten(_dcSumme);
            }

            // --------------------------------------------------------------
            // Zyklenbudget
            // --------------------------------------------------------------

            private double RestBudgetDc(double dcImFenster)
            {
                if (!(_budget > 0.0)) return double.PositiveInfinity;
                double rest = _budget - _dcFestgeschrieben - dcImFenster;
                return rest > 0.0 ? rest : 0.0;
            }

            private bool BudgetErschoepft(double dcImFenster)
            {
                if (!(_budget > 0.0)) return false;
                return _dcFestgeschrieben + dcImFenster >= _budget - _tolEnergie;
            }

            private bool BudgetUeberschritten(double dcImFenster)
            {
                if (!(_budget > 0.0)) return false;
                return _dcFestgeschrieben + dcImFenster > _budget + _tolEnergie;
            }
        }
    }
}
