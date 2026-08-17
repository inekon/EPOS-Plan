namespace SpeicherEngine
{
    /// <summary>
    /// Rechenmodus der Speicherstrategien (Fachkonzept 5.2).
    /// </summary>
    public enum SpeicherModus
    {
        /// <summary>
        /// Produktivmodus: energetisches Verlustmodell mit
        /// <c>eta_ch = eta_dis = sqrt(eta_RT)</c>, SoC-Band
        /// <c>SoC_min .. SoC_max</c>, Start bei <c>StartSoC</c>,
        /// alle Intervalle ab Index 0.
        /// </summary>
        Energetisch = 0,

        /// <summary>
        /// Excel-Kompatibilitaetsmodus, nur fuer die Referenztests gegen die
        /// V7-Mappe: <c>eta_ch = eta_dis = 1</c>, Start-SoC 0, Intervall 0 wird
        /// nicht simuliert, keine Degradation, Verluste erst pauschal auf der
        /// Euro-Summe (<c>N10 = Summe(F) * (1 - Verlustfaktor)</c>).
        /// </summary>
        ExcelKompatibilitaet = 1
    }
}
