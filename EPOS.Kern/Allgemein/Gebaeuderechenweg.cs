using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Welcher Rechenweg für ein Gebäude gilt</b> — die Auskunft für Dialog und Hülle
    /// (Stufe G1; Umsetzungskonzept Gebäudesimulation 2.3, 2.7; ADR-006).
    ///
    /// <para><b>Die Anzeige folgt der Rechnung.</b> Die Regel steht einmal, an der Weiche
    /// (<c>SimulationWaermebedarf.RechenwegWaehlen</c>): <c>VDI6007</c> führt auf den
    /// VDI-Weg, <c>TAGESBILANZ</c> und jeder unbekannte Wert auf den Tagesbilanz-Weg, und
    /// NULL folgt <c>SimulationWaermebedarf.MODELL_OHNE_ANGABE</c>. Diese Klasse liest
    /// dieselbe Konstante — schaltet die Schlusswelle G1 + G2 sie auf <c>VDI6007</c>, zeigt
    /// der Dialog ein Gebäude ohne Angabe ohne weiteres Zutun als „VDI 6007".</para>
    ///
    /// <para>Rein, ohne Datenbank; öffentlich, weil der Katalogeditor in <c>EPOS.UI</c> sie
    /// braucht (dieselbe Lage wie <see cref="Gebaeudebauweise"/>).</para>
    /// </summary>
    public static class Gebaeuderechenweg
    {
        /// <summary>
        /// Der Rechenweg eines Gebäudes ohne Angabe (Spaltenwert NULL) — in dieser Welle
        /// <see cref="DbWerte.GEBAEUDE_MODELL_TAGESBILANZ"/>, ab der Schlusswelle G1 + G2
        /// <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/>.
        /// </summary>
        public static string OhneAngabe => SimulationWaermebedarf.MODELL_OHNE_ANGABE;

        /// <summary>
        /// Der Rechenweg, auf dem ein Gebäude mit dem Spaltenwert <paramref name="modell"/>
        /// tatsächlich rechnet: <see cref="DbWerte.GEBAEUDE_MODELL_VDI6007"/> oder
        /// <see cref="DbWerte.GEBAEUDE_MODELL_TAGESBILANZ"/>.
        /// </summary>
        public static string Wirksam(string modell)
        {
            string m = modell ?? OhneAngabe;
            return string.Equals(m, DbWerte.GEBAEUDE_MODELL_VDI6007, StringComparison.Ordinal)
                ? DbWerte.GEBAEUDE_MODELL_VDI6007
                : DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
        }

        /// <summary>Rechnet ein Gebäude mit diesem Spaltenwert auf dem VDI-Weg?</summary>
        public static bool IstVdi6007(string modell)
            => Wirksam(modell) == DbWerte.GEBAEUDE_MODELL_VDI6007;
    }

    /// <summary>
    /// <b>Die Vorgaben der Modellparameter</b>, die der Kern für eine leere Spalte einsetzt
    /// (<c>GebaeudeFestwerte.VORGABE_*</c>) — öffentlich, damit der Gebäudedialog sie als
    /// Platzhalter „Vorgabe …" zeigt, statt eine zweite Zahl zu führen (Umsetzungskonzept
    /// Gebäudesimulation 2.4). Der Dialog schreibt NULL, nicht diese Werte.
    /// </summary>
    public static class Gebaeudemodellvorgaben
    {
        /// <summary>Rahmenanteil der Fenster [–].</summary>
        public static double Rahmenanteil => GebaeudeFestwerte.VORGABE_RAHMENANTEIL;

        /// <summary>Verschattungsfaktor [–].</summary>
        public static double Verschattungsfaktor => GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR;

        /// <summary>Masseanteil außen [–].</summary>
        public static double MasseanteilAussen => GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN;

        /// <summary>Innenflächenfaktor [–].</summary>
        public static double Innenflaechenfaktor => GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR;

        /// <summary>Strahlungsanteil der Heizung [–].</summary>
        public static double HeizungStrahlungsanteil => GebaeudeFestwerte.VORGABE_HEIZUNG_STRAHLUNGSANTEIL;

        /// <summary>Kellertemperatur [°C] bei Randbedingung Keller.</summary>
        public static double Kellertemperatur => GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR;
    }
}
