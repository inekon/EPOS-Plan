using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Gemeinsamer Vorverarbeitungsschritt aller Betriebsstrategien (Fachkonzept 6,
    /// Einleitung). Zerlegt Last, PV und BHKW eines Intervalls in Direktdeckung,
    /// ladefaehigen Ueberschuss je Quelle und Residuallast.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Formeln, unveraendert aus dem Fachkonzept (dt = 0,25 h, Energien in kWh):
    /// </para>
    /// <code>
    /// E_last      = P_last*dt ;  E_pv = P_pv*dt ;  E_bhkw = P_bhkw*dt
    /// E_restlast  = max(0, E_last  - E_bhkw)          # BHKW deckt vorrangig
    /// E_pv_frei   = max(0, E_pv    - E_restlast)      # ladefaehiger PV-Ueberschuss
    /// E_bhkw_frei = max(0, E_bhkw  - E_last)          # ladefaehiger BHKW-Ueberschuss
    /// E_defizit   = max(0, E_last  - E_pv - E_bhkw)   # Residuallast
    /// E_quelle    = (PV zulaessig ? E_pv_frei : 0) + (BHKW zulaessig ? E_bhkw_frei : 0)
    /// </code>
    /// <para>
    /// Die Konvention "das BHKW deckt die Last vorrangig" stammt aus der Merit-Order
    /// (Fachkonzept 2.2) und deckt sich mit der De-facto-Reihenfolge der
    /// Bestandssimulation (BHKW wird vor PV vom Reststrom abgezogen,
    /// <c>SimulationControl</c>). Fuer die Energiebilanz ist sie neutral, fuer die
    /// Quellenbeschraenkung des Gruenspeichers entscheidend: PV soll als guenstigere
    /// Ladequelle uebrig bleiben.
    /// </para>
    /// <para>
    /// <b>Bilanzidentitaeten</b>, die daraus exakt folgen (und die der Bilanztest
    /// nachrechnet):
    /// </para>
    /// <code>
    /// E_last            = E_direkt + E_defizit
    /// E_pv + E_bhkw     = E_direkt + E_pv_frei + E_bhkw_frei
    /// </code>
    /// <para>
    /// Ueberschuss und Defizit schliessen einander aus - Laden und Entladen im selben
    /// Intervall ist damit konstruktiv ausgeschlossen.
    /// </para>
    /// </remarks>
    public static class Vorverarbeitung
    {
        /// <summary>
        /// Zerlegt ein Intervall. Leistungen in kW, Ergebnis in kWh.
        /// </summary>
        /// <param name="lastKw">Lastgang P_last [kW] inklusive Anlagen-Eigenbedarf (Fachkonzept 3.1).</param>
        /// <param name="pvKw">PV-Erzeugung P_pv [kW] nach Wechselrichter.</param>
        /// <param name="bhkwKw">BHKW-Erzeugung P_bhkw [kW]; 0, wenn kein BHKW vorhanden ist.</param>
        /// <param name="dtH">Intervalldauer dt [h].</param>
        /// <param name="pvZulaessig">PV als Ladequelle zulaessig (Quellen-Matrix 2.1).</param>
        /// <param name="bhkwUeberschussZulaessig">BHKW-Ueberschuss als Ladequelle zulaessig.</param>
        public static IntervallEnergien Berechne(
            double lastKw,
            double pvKw,
            double bhkwKw,
            double dtH,
            bool pvZulaessig,
            bool bhkwUeberschussZulaessig)
        {
            double eLast = lastKw * dtH;
            double ePv = pvKw * dtH;
            double eBhkw = bhkwKw * dtH;

            double eRestlast = eLast - eBhkw;
            if (eRestlast < 0.0) eRestlast = 0.0;

            double ePvFrei = ePv - eRestlast;
            if (ePvFrei < 0.0) ePvFrei = 0.0;

            double eBhkwFrei = eBhkw - eLast;
            if (eBhkwFrei < 0.0) eBhkwFrei = 0.0;

            double eDefizit = eLast - ePv - eBhkw;
            if (eDefizit < 0.0) eDefizit = 0.0;

            double ePvQuelle = pvZulaessig ? ePvFrei : 0.0;
            double eBhkwQuelle = bhkwUeberschussZulaessig ? eBhkwFrei : 0.0;

            return new IntervallEnergien(
                eLast, ePv, eBhkw, eRestlast, ePvFrei, eBhkwFrei, eDefizit, ePvQuelle, eBhkwQuelle);
        }

        /// <summary>
        /// Zerlegt ein Intervall mit den Quellenflags aus <paramref name="p"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="p"/> <c>null</c> ist.</exception>
        public static IntervallEnergien Berechne(double lastKw, double pvKw, double bhkwKw, SpeicherParameter p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            return Berechne(lastKw, pvKw, bhkwKw, p.DtH, p.PvZulaessig, p.BhkwUeberschussZulaessig);
        }
    }

    /// <summary>
    /// Ergebnis der Vorverarbeitung eines Intervalls, alle Groessen in kWh
    /// (Fachkonzept 6). Unveraenderlicher Wertetyp.
    /// </summary>
    public readonly struct IntervallEnergien
    {
        /// <summary>Erzeugt den Satz. Wird ausschliesslich von <see cref="Vorverarbeitung"/> aufgerufen.</summary>
        public IntervallEnergien(
            double eLastKwh, double ePvKwh, double eBhkwKwh, double eRestlastKwh,
            double ePvFreiKwh, double eBhkwFreiKwh, double eDefizitKwh,
            double ePvQuelleKwh, double eBhkwQuelleKwh)
        {
            ELastKwh = eLastKwh;
            EPvKwh = ePvKwh;
            EBhkwKwh = eBhkwKwh;
            ERestlastKwh = eRestlastKwh;
            EPvFreiKwh = ePvFreiKwh;
            EBhkwFreiKwh = eBhkwFreiKwh;
            EDefizitKwh = eDefizitKwh;
            EPvQuelleKwh = ePvQuelleKwh;
            EBhkwQuelleKwh = eBhkwQuelleKwh;
        }

        /// <summary>E_last: Lastenergie des Intervalls [kWh].</summary>
        public double ELastKwh { get; }

        /// <summary>E_pv: PV-Erzeugung des Intervalls [kWh].</summary>
        public double EPvKwh { get; }

        /// <summary>E_bhkw: BHKW-Erzeugung des Intervalls [kWh].</summary>
        public double EBhkwKwh { get; }

        /// <summary>E_restlast = max(0, E_last - E_bhkw): Last nach BHKW-Deckung [kWh].</summary>
        public double ERestlastKwh { get; }

        /// <summary>E_pv_frei: ladefaehiger PV-Ueberschuss [kWh].</summary>
        public double EPvFreiKwh { get; }

        /// <summary>E_bhkw_frei: ladefaehiger BHKW-Ueberschuss [kWh].</summary>
        public double EBhkwFreiKwh { get; }

        /// <summary>E_defizit: Residuallast, die der Speicher decken kann [kWh].</summary>
        public double EDefizitKwh { get; }

        /// <summary>Nach der Quellen-Matrix zugelassener PV-Anteil an <see cref="EQuelleKwh"/> [kWh].</summary>
        public double EPvQuelleKwh { get; }

        /// <summary>Nach der Quellen-Matrix zugelassener BHKW-Anteil an <see cref="EQuelleKwh"/> [kWh].</summary>
        public double EBhkwQuelleKwh { get; }

        /// <summary>E_quelle: insgesamt zum Laden zugelassene Energie [kWh].</summary>
        public double EQuelleKwh => EPvQuelleKwh + EBhkwQuelleKwh;

        /// <summary>
        /// Direkt aus der Erzeugung gedeckte Last E_direkt = E_last - E_defizit [kWh].
        /// </summary>
        public double EDirektKwh => ELastKwh - EDefizitKwh;

        /// <summary>
        /// Gesamter Erzeugungsueberschuss E_pv_frei + E_bhkw_frei [kWh] - ohne Speicher
        /// ist das die Netzeinspeisung des Intervalls.
        /// </summary>
        public double EUeberschussKwh => EPvFreiKwh + EBhkwFreiKwh;
    }
}
