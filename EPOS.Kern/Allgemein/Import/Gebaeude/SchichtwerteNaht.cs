namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die schmale Naht „U-Wert und Masse aus Schichten"</b> des Gebäudeimports.
    ///
    /// <para><b>Warum eine Naht und keine Rechnung.</b> Die Rechnung — R = Σ d/λ (+ R der
    /// Luftschichten), C = Σ ρ·c·d, U = 1/(R_si + R + R_se) mit R_si nach der Neigung und R_se nach
    /// der Randbedingung — entsteht EINMAL, und zwar in der Stufe G3 (Bauteilkatalog) als statische
    /// Funktion in <c>Allgemein/Simulation/Gebaeude/Bauteilreduktion.cs</c>; der Import benutzt sie
    /// mit. Bis dahin liefern beide Methoden <c>null</c> = „nicht gerechnet": Ein Bauteil ohne
    /// eingetragenen U-Wert hat dann keinen, und die Gruppe fällt nach der 30-%-Regel auf die
    /// Vorgabe der Baualtersklasse zurück (E2). <b>Wird auf Bauteilreduktion (G3) umgestellt.</b></para>
    ///
    /// <para>Ein eingetragener U-Wert (<c>Construction/U-value</c>) hat immer Vorrang vor der
    /// Rechnung (Mehrzonenkonzept 3.4); diese Naht wird nur für Bauteile ohne ihn gefragt.</para>
    /// </summary>
    internal static class SchichtwerteNaht
    {
        /// <summary>
        /// U-Wert [W/(m²K)] eines Aufbaus aus seinen Schichten; <c>null</c> = nicht gerechnet.
        /// Wird auf Bauteilreduktion (G3) umgestellt.
        /// </summary>
        /// <param name="aufbau">Der Aufbau (vollständig oder masselos).</param>
        /// <param name="neigungGrad">Neigung des Bauteils — sie bestimmt R_si (Wärmestromrichtung).</param>
        /// <param name="randbedingung">Randbedingung des Bauteils — sie bestimmt R_se.</param>
        internal static double? UWertAusSchichten(AbbildAufbau aufbau, double? neigungGrad, Randbedingung randbedingung)
            => null;

        /// <summary>
        /// Flächenbezogene Wärmekapazität [J/(m²K)] eines vollständigen Aufbaus; <c>null</c> = nicht
        /// gerechnet. Gebraucht für die Bauweise aus Schichten (Umsetzungskonzept 3.4, Zeile
        /// Bauweise) — bis dahin bleibt die Bauweise leer und die Bauart bei der Vorgabe. Wird auf
        /// Bauteilreduktion (G3) umgestellt.
        /// </summary>
        internal static double? KapazitaetAusSchichten(AbbildAufbau aufbau) => null;
    }
}
