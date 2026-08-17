using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Preisreihen und Schalter der Preissteuerung (Fachkonzept 6.5, Arbeitspaket
    /// AP10). Alles, was die <see cref="Arbitrage"/> ueber die
    /// <see cref="SpeicherEingang"/>-Reihen hinaus braucht.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigener Typ.</b> <see cref="SpeicherEingang"/> und
    /// <see cref="SpeicherParameter"/> sind bitgenau referenzgeprueft und werden von
    /// jeder Strategie gelesen; die beiden Netzpfad-Reihen gehen aber ausschliesslich
    /// die Arbitrage etwas an. Ein zusaetzlicher optionaler Parameter dort haette jede
    /// andere Strategie mitgeaendert - hier steht er additiv daneben.
    /// </para>
    /// <para>
    /// Der Konstruktor kopiert die uebergebenen Arrays; die Instanz ist danach
    /// unveraenderlich und darf von mehreren Threads gelesen werden (Fachkonzept 8.1).
    /// </para>
    /// <para>
    /// <b>Einheiten:</b> Preise und Erloese in ct/kWh, Energien in kWh, jeweils
    /// AC-seitig; das Zyklenbudget dagegen DC-seitig, weil die aequivalenten
    /// Vollzyklen nach Fachkonzept 5.4 ueber die DC-entnommene Energie definiert sind.
    /// </para>
    /// </remarks>
    public sealed class ArbitrageOptionen
    {
        /// <summary>
        /// Laenge eines Planungsfensters in Intervallen; Default 96 = 24 h bei
        /// dt = 0,25 h (Fachkonzept 6.5: "Rolling-Horizon-Greedy ueber
        /// 24-Stunden-Fenster").
        /// </summary>
        public const int FensterIntervalleStandard = 96;

        /// <summary>
        /// <b>Gesetzter Default:</b> Reservepuffer ueber SoC_min, aus dem <b>nicht</b>
        /// verkauft werden darf [kWh] - 0, also kein Puffer.
        /// </summary>
        /// <remarks>
        /// Das Fachkonzept nennt keinen Wert. 0 bedeutet: Der Verkauf darf das
        /// nutzbare Band bis SoC_min ausschoepfen, genau wie die
        /// Eigenverbrauchsentladung. Wer dem Verkauf eine Reserve fuer die
        /// Eigenverbrauchsdeckung vorenthalten will, setzt hier einen Wert &gt; 0.
        /// </remarks>
        public const double ReservepufferKwhStandard = 0.0;

        /// <summary>
        /// Netzladepreis p_netzlade [ct/kWh] je Intervall (Fachkonzept 4.4:
        /// <c>p_netzlade[i] = p_energie[i] + a_netzlade</c>, also der Energiepreis
        /// <b>ohne</b> die Aufschlaege aus 4.2).
        /// </summary>
        public double[] NetzladepreisCtKwh { get; }

        /// <summary>
        /// Erloes je ins Netz verkaufter kWh [ct/kWh] je Intervall - die Spotreihe,
        /// ersatzweise die Einspeiseverguetung (Fachkonzept 2.2, Entladeprioritaet 2).
        /// </summary>
        public double[] ErloesCtKwh { get; }

        /// <summary>
        /// Netzladung zugelassen - <b>nur im Graustrombetrieb</b> (Fachkonzept 2.1:
        /// im Gruenbetrieb haengen Verguetungsanspruch und Netzentgeltbefreiung an der
        /// Ausschliesslichkeit der Beladung aus erneuerbaren Quellen).
        /// </summary>
        public bool Netzladung { get; }

        /// <summary>
        /// Aktiver Verkauf ins Netz zugelassen - unabhaengig von der Betriebsart
        /// (Fachkonzept 2.1: "auch ein Gruenstromspeicher darf verkaufen").
        /// </summary>
        public bool Netzentladung { get; }

        /// <summary>
        /// Manuelle Zusatzschranke fuer Ladeslots [ct/kWh] (Fachkonzept 5.6:
        /// <c>Tab_Einstellungen.Ladeschwellwert</c> wird auf den
        /// Preissteuerungs-Schwellwert abgebildet). Geladen wird nur, wenn
        /// <c>p_netzlade[i] &lt;= Schwellwert</c> ist; <b>0 = keine Schranke</b>.
        /// </summary>
        /// <remarks>
        /// Die Schranke wirkt <b>zusaetzlich</b> zur Rentabilitaetsbedingung aus 6.5,
        /// nie an ihrer Stelle: Sie kann nur weniger Ladeslots zulassen, nie mehr.
        /// </remarks>
        public double LadeschwellwertCtKwh { get; }

        /// <summary>
        /// Jahres-Zyklenbudget als DC-entnommene Energie [kWh/a]; <b>0 = unbegrenzt</b>.
        /// Siehe <see cref="JahresbudgetDcKwh"/> zur Herkunft des Werts.
        /// </summary>
        public double ZyklenbudgetDcKwhProA { get; }

        /// <summary>Reservepuffer ueber SoC_min, aus dem nicht verkauft wird [kWh].</summary>
        public double ReservepufferKwh { get; }

        /// <summary>Laenge eines Planungsfensters in Intervallen.</summary>
        public int FensterIntervalle { get; }

        /// <summary>Anzahl der Intervalle, fuer die die Reihen gelten.</summary>
        public int Anzahl => NetzladepreisCtKwh.Length;

        /// <summary>
        /// true, wenn ueberhaupt ein Netzpfad offensteht. Ist das false, rechnet die
        /// <see cref="Arbitrage"/> bitgleich zur <see cref="Dauernutzung"/>.
        /// </summary>
        public bool HatNetzpfad => Netzladung || Netzentladung;

        /// <summary>Erzeugt den Optionssatz. Beide Reihen sind Pflicht und gleich lang.</summary>
        /// <param name="netzladepreisCtKwh">p_netzlade [ct/kWh] je Intervall.</param>
        /// <param name="erloesCtKwh">Verkaufserloes [ct/kWh] je Intervall.</param>
        /// <param name="netzladung">Netzladung zugelassen (nur Graustrom).</param>
        /// <param name="netzentladung">Verkauf ins Netz zugelassen.</param>
        /// <param name="ladeschwellwertCtKwh">Zusatzschranke fuer Ladeslots; 0 = keine.</param>
        /// <param name="zyklenbudgetDcKwhProA">Jahresbudget DC [kWh/a]; 0 = unbegrenzt.</param>
        /// <param name="reservepufferKwh">Reservepuffer ueber SoC_min [kWh].</param>
        /// <param name="fensterIntervalle">Fensterlaenge; Default 96.</param>
        /// <exception cref="ArgumentNullException">Wenn eine Reihe <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Wenn die Reihen leer oder unterschiedlich lang sind.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Bei unbrauchbarer Fensterlaenge.</exception>
        public ArbitrageOptionen(
            double[] netzladepreisCtKwh,
            double[] erloesCtKwh,
            bool netzladung = false,
            bool netzentladung = false,
            double ladeschwellwertCtKwh = 0.0,
            double zyklenbudgetDcKwhProA = 0.0,
            double reservepufferKwh = ReservepufferKwhStandard,
            int fensterIntervalle = FensterIntervalleStandard)
        {
            if (netzladepreisCtKwh == null) throw new ArgumentNullException(nameof(netzladepreisCtKwh));
            if (erloesCtKwh == null) throw new ArgumentNullException(nameof(erloesCtKwh));
            if (netzladepreisCtKwh.Length == 0)
                throw new ArgumentException("Die Preisreihen duerfen nicht leer sein.", nameof(netzladepreisCtKwh));
            if (erloesCtKwh.Length != netzladepreisCtKwh.Length)
                throw new ArgumentException("Erloes- und Netzladepreisreihe muessen gleich lang sein.", nameof(erloesCtKwh));
            if (fensterIntervalle <= 0)
                throw new ArgumentOutOfRangeException(nameof(fensterIntervalle), fensterIntervalle,
                                                     "Die Fensterlaenge muss groesser 0 sein.");

            NetzladepreisCtKwh = (double[])netzladepreisCtKwh.Clone();
            ErloesCtKwh = (double[])erloesCtKwh.Clone();
            Netzladung = netzladung;
            Netzentladung = netzentladung;
            LadeschwellwertCtKwh = ladeschwellwertCtKwh;
            ZyklenbudgetDcKwhProA = zyklenbudgetDcKwhProA < 0.0 ? 0.0 : zyklenbudgetDcKwhProA;
            ReservepufferKwh = reservepufferKwh < 0.0 ? 0.0 : reservepufferKwh;
            FensterIntervalle = fensterIntervalle;
        }

        /// <summary>
        /// Bequemer Weg fuer konstante Reihen (Tests, Fixpreisfall).
        /// </summary>
        public static ArbitrageOptionen Konstant(
            double netzladepreisCtKwh,
            double erloesCtKwh,
            int anzahl,
            bool netzladung = false,
            bool netzentladung = false,
            double ladeschwellwertCtKwh = 0.0,
            double zyklenbudgetDcKwhProA = 0.0)
        {
            return new ArbitrageOptionen(
                SpeicherEingang.KonstanteReihe(netzladepreisCtKwh, anzahl),
                SpeicherEingang.KonstanteReihe(erloesCtKwh, anzahl),
                netzladung, netzentladung, ladeschwellwertCtKwh, zyklenbudgetDcKwhProA);
        }

        /// <summary>
        /// Verschleiss je ausgespeicherter kWh AC
        /// <c>k_ver = 100 * c_ver * C_nom / (C_nutz * eta_dis)</c> [ct/kWh]
        /// (Fachkonzept 5.4; mit den dortigen Zahlen 3,29 ct/kWh).
        /// </summary>
        /// <remarks>
        /// <b>Nicht abschaltbar</b> (Fachkonzept 5.4, Verwendung 1): Ohne c_ver faehrt
        /// der Dispatch den Speicher fuer Cent-Spreads leer. Der Faktor 100 rechnet
        /// EUR/kWh in die ct/kWh der Preisreihen um.
        /// </remarks>
        public static double VerschleissCtKwh(SpeicherParameter p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (!(p.CNutzKwh > 0.0) || !(p.EtaDis > 0.0)) return 0.0;
            return 100.0 * p.CVerEurProKwhZyklus * p.CNomKwh / (p.CNutzKwh * p.EtaDis);
        }

        /// <summary>
        /// <b>Gesetzter Default:</b> Jahresanteil des Zyklenbudgets
        /// <c>N_zyk * C_nutz / N</c> [kWh DC/a].
        /// </summary>
        /// <remarks>
        /// <para>
        /// Fachkonzept 6.5 sagt "Das Zyklenbudget begrenzt die kumulierte
        /// Entladeenergie", laesst die <b>Jahresverteilung</b> aber offen. Gesetzt ist
        /// hier die gleichmaessige Aufteilung der zugesicherten Vollzyklen ueber die
        /// Nutzungsdauer: Ein Jahreslauf ist das Referenzjahr der
        /// Wirtschaftlichkeitsprojektion (5.3), und genau dieses Jahr darf nicht mehr
        /// Zyklen verbrauchen, als das Datenblatt ueber die Nutzungsdauer zusichert.
        /// Damit deckt sich die Planungsschranke mit der Ampelbewertung aus 7.1
        /// (<c>n_zyk * N</c> gegen <c>N_zyk</c>).
        /// </para>
        /// <para>
        /// Fehlt N_zyk oder die Nutzungsdauer (0 = nicht gepflegt), liefert die Methode
        /// 0 = unbegrenzt; die Planung laeuft dann ohne Budgetschranke, wie vor diesem
        /// Paket jede andere Strategie.
        /// </para>
        /// </remarks>
        /// <param name="zyklenZugesichert">N_zyk aus den Geraetedaten.</param>
        /// <param name="cNutzKwh">Nutzbare Kapazitaet C_nutz [kWh].</param>
        /// <param name="nutzungsdauerA">Nutzungsdauer N [a].</param>
        public static double JahresbudgetDcKwh(double zyklenZugesichert, double cNutzKwh, double nutzungsdauerA)
        {
            if (!(zyklenZugesichert > 0.0) || !(cNutzKwh > 0.0) || !(nutzungsdauerA > 0.0)) return 0.0;
            return zyklenZugesichert * cNutzKwh / nutzungsdauerA;
        }
    }
}
