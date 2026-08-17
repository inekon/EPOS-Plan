namespace SpeicherEngine
{
    /// <summary>
    /// Energetische Kennzahlen eines Simulationslaufs (Fachkonzept 5.4 und 7.1).
    /// Ergaenzt <see cref="SpeicherErgebnis"/> um Quellenaufteilung, Zyklen,
    /// Verschleissausweis und die Jahresbilanz.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Der Typ ist ein <c>record</c> mit <c>init</c>-Settern und damit nach der
    /// Konstruktion unveraenderlich. Er wird von der Strategie gefuellt; Aufrufer
    /// lesen nur.
    /// </para>
    /// <para>
    /// <b>Bilanzkonvention.</b> "ohne Speicher" bedeutet: dieselben Last- und
    /// Erzeugungsreihen, aber ohne jede Speicherwirkung - also
    /// <c>Netzbezug = Sigma E_defizit</c> und <c>Einspeisung = Sigma (E_pv_frei +
    /// E_bhkw_frei)</c>. "mit Speicher" zieht davon die Entlade- bzw. Ladeenergie ab.
    /// Beide Groessen stammen aus derselben Vorverarbeitung und sind deshalb direkt
    /// vergleichbar.
    /// </para>
    /// <para>
    /// Im Excel-Kompatibilitaetsmodus bleiben die Bilanz- und Quellengroessen leer
    /// (0): dort existiert weder eine Quellen-Matrix noch ein Verlustmodell, und die
    /// V7-Mappe weist nichts dergleichen aus. Gefuellt sind dort nur
    /// <see cref="LadeenergiePvKwh"/>, <see cref="EntladeenergieDcKwh"/> und
    /// <see cref="AequivalenteVollzyklen"/>; <see cref="VerschleisskostenEurProA"/>
    /// ist 0, weil der Kompatibilitaetsmodus per Definition mit c_ver = 0 rechnet
    /// (Fachkonzept 5.2).
    /// </para>
    /// </remarks>
    public sealed record SpeicherKennzahlen
    {
        /// <summary>Leerer Satz - Vorbelegung fuer Ergebnisse ohne Kennzahlenblock.</summary>
        public static readonly SpeicherKennzahlen Leer = new SpeicherKennzahlen();

        // ---------------------------------------------------------------- Quellen

        /// <summary>AC-seitig aus PV-Ueberschuss geladene Energie [kWh/a].</summary>
        public double LadeenergiePvKwh { get; init; }

        /// <summary>AC-seitig aus BHKW-Ueberschuss geladene Energie [kWh/a].</summary>
        public double LadeenergieBhkwKwh { get; init; }

        /// <summary>Summe der Ladeenergie ueber alle Quellen [kWh/a] (AC-seitig).</summary>
        public double LadeenergieKwh => LadeenergiePvKwh + LadeenergieBhkwKwh;

        // ----------------------------------------------------------------- Zyklen

        /// <summary>
        /// DC-seitig entnommene Energie <c>Sigma E_ac_dis / eta_dis</c> [kWh/a] -
        /// Bezugsgroesse der Vollzyklen.
        /// </summary>
        public double EntladeenergieDcKwh { get; init; }

        /// <summary>
        /// Aequivalente Vollzyklen <c>n_zyk = Sigma E_dc,entnommen / C_nutz</c> [1/a]
        /// (Fachkonzept 5.4). 0, wenn <c>C_nutz = 0</c> ist.
        /// </summary>
        public double AequivalenteVollzyklen { get; init; }

        /// <summary>
        /// Jahres-Verschleisskosten <c>K_ver = n_zyk * C_nom * c_ver</c> [EUR/a]
        /// (Fachkonzept 5.4).
        /// </summary>
        /// <remarks>
        /// <b>Reiner Ausweis.</b> K_ver geht in diesem Arbeitspaket bewusst
        /// <b>nicht</b> in <see cref="SpeicherErgebnis.SummeGeldwertEur"/> und nicht in
        /// den Jahresueberschuss ein: Annuitaet und Verschleisskosten bepreisen
        /// denselben Sachverhalt, und solange c_ver aus der Investition abgeleitet ist
        /// (Default), waere die Einrechnung eine Doppelzaehlung. Die Zielfunktions-Option
        /// aus Fachkonzept 5.4 (Default AUS) folgt mit der Parameter-UI in AP3.
        /// </remarks>
        public double VerschleisskostenEurProA { get; init; }

        /// <summary>
        /// Speicherverluste des Jahres [kWh/a]:
        /// <c>Ladeenergie - Entladeenergie - (SoC_Ende - SoC_Start)</c>, jeweils AC-seitig.
        /// </summary>
        public double SpeicherverlusteKwh { get; init; }

        // ---------------------------------------------------------------- Bilanz

        /// <summary>Jahreslastenergie <c>Sigma E_last</c> [kWh/a].</summary>
        public double LastKwh { get; init; }

        /// <summary>PV-Jahreserzeugung <c>Sigma E_pv</c> [kWh/a].</summary>
        public double ErzeugungPvKwh { get; init; }

        /// <summary>BHKW-Jahreserzeugung <c>Sigma E_bhkw</c> [kWh/a].</summary>
        public double ErzeugungBhkwKwh { get; init; }

        /// <summary>Jahreserzeugung aller Quellen [kWh/a].</summary>
        public double ErzeugungKwh => ErzeugungPvKwh + ErzeugungBhkwKwh;

        /// <summary>Direkt aus der Erzeugung gedeckte Last <c>Sigma E_direkt</c> [kWh/a].</summary>
        public double DirektverbrauchKwh { get; init; }

        /// <summary>Netzbezug ohne Speicher <c>Sigma E_defizit</c> [kWh/a].</summary>
        public double NetzbezugOhneSpeicherKwh { get; init; }

        /// <summary>Netzbezug mit Speicher <c>Sigma (E_defizit - E_ac_dis)</c> [kWh/a].</summary>
        public double NetzbezugMitSpeicherKwh { get; init; }

        /// <summary>Netzeinspeisung ohne Speicher <c>Sigma (E_pv_frei + E_bhkw_frei)</c> [kWh/a].</summary>
        public double EinspeisungOhneSpeicherKwh { get; init; }

        /// <summary>Netzeinspeisung mit Speicher <c>Sigma (Ueberschuss - E_ac_ch)</c> [kWh/a].</summary>
        public double EinspeisungMitSpeicherKwh { get; init; }

        // ------------------------------------------------------------- Abgeleitet

        /// <summary>Autarkiegrad mit Speicher [-] = 1 - Netzbezug/Last; 0 bei Last = 0.</summary>
        public double AutarkiegradMitSpeicher
            => LastKwh > 0.0 ? (LastKwh - NetzbezugMitSpeicherKwh) / LastKwh : 0.0;

        /// <summary>Autarkiegrad ohne Speicher [-].</summary>
        public double AutarkiegradOhneSpeicher
            => LastKwh > 0.0 ? (LastKwh - NetzbezugOhneSpeicherKwh) / LastKwh : 0.0;

        /// <summary>Eigenverbrauchsquote mit Speicher [-] = 1 - Einspeisung/Erzeugung; 0 bei Erzeugung = 0.</summary>
        public double EigenverbrauchsquoteMitSpeicher
            => ErzeugungKwh > 0.0 ? (ErzeugungKwh - EinspeisungMitSpeicherKwh) / ErzeugungKwh : 0.0;

        /// <summary>Eigenverbrauchsquote ohne Speicher [-].</summary>
        public double EigenverbrauchsquoteOhneSpeicher
            => ErzeugungKwh > 0.0 ? (ErzeugungKwh - EinspeisungOhneSpeicherKwh) / ErzeugungKwh : 0.0;
    }
}
