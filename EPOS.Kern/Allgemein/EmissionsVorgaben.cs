using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die EMISSIONS-VORGABEWERTE der beiden Katalogeditoren (iU9-W6.1/W6.2).
    ///
    /// <para><b>Wozu.</b> Heizkessel- und BHKW-Katalogeditor tragen je einen Knopf, der
    /// Emissionswerte nach dem gewaehlten Brennstoff EINTRAEGT statt sie nachschlagen zu
    /// lassen: <c>Form_Heizkessel_Bearbeiten.btn_CO2_Click</c> (Z. 547),
    /// <c>Form_DBBHKW.btn_CO2_Click</c> (Z. 690) und
    /// <c>Form_DBBHKW.btn_Eintragen_Click</c> (Z. 640). Die Zahlen standen dreimal im
    /// Oberflaechencode; eine Razor-Komponente rechnet nicht (sie zeigt nur), und zwei
    /// Komponenten sollen dieselbe Quelle nutzen.</para>
    ///
    /// <para><b>Die Fallunterscheidungen sind WORTGLEICH uebernommen</b> - samt ihrer
    /// Eigenheiten (Regel F3 des Wellenplans). Sie unterscheiden sich zwischen den beiden
    /// Masken, und das bleibt so:</para>
    /// <list type="bullet">
    /// <item>Der Heizkessel prueft <c>ÖL</c> in Grossschreibung, dann <c>GAS</c> UND NICHT
    /// <c>Flüssiggas</c> (in Originalschreibweise!), dann <c>Flüssiggas</c>, sonst 0.</item>
    /// <item>Das BHKW prueft <c>HEIZÖL</c>, dann <c>GAS</c>, dann <c>FLÜSSIGGAS</c> - ohne
    /// <c>else</c>. „Flüssiggas" trifft dort deshalb ZWEI Zweige, und der letzte gewinnt;
    /// ein Brennstoff ohne Treffer laesst den Wert stehen, statt ihn auf 0 zu setzen.</item>
    /// </list>
    ///
    /// <para><b>Die Zahlen selbst.</b> CO2 in g/MWh bezogen auf den Brennstoffverbrauch;
    /// die BEHG-Faktoren fuer Heizzwecke stehen als Beschriftung neben dem Knopf
    /// (Heizöl 0,0808 / Flüssiggas 0,0663 / Erdgas 0,056 t CO2 je GJ).</para>
    /// </summary>
    public static class EmissionsVorgaben
    {
        /// <summary>
        /// CO2-Vorgabe des HEIZKESSEL-Editors [g/MWh] nach dem Anzeigenamen des
        /// Brennstoffs. Wortgleich <c>Form_Heizkessel_Bearbeiten.btn_CO2_Click</c>.
        /// </summary>
        public static double HeizkesselCo2(string brennstoff)
        {
            string name = brennstoff ?? "";

            if (name.ToUpper().Contains("ÖL")) return 290880;
            if (name.ToUpper().Contains("GAS") && !name.Contains("Flüssiggas")) return 201600;
            if (name.Contains("Flüssiggas")) return 238680;
            return 0;
        }

        /// <summary>
        /// CO2-Vorgabe des BHKW-Editors [g/MWh]. Wortgleich
        /// <c>Form_DBBHKW.btn_CO2_Click</c> - ohne <c>else</c>, deshalb gewinnt bei
        /// „Flüssiggas" der letzte Zweig, und ohne Treffer bleibt der bisherige Wert
        /// stehen (<c>null</c>).
        /// </summary>
        public static double? BhkwCo2(string brennstoff)
        {
            string oben = (brennstoff ?? "").ToUpper();

            double? wert = null;
            if (oben.Contains("HEIZÖL")) wert = 290880;
            if (oben.Contains("GAS")) wert = 201600;
            if (oben.Contains("FLÜSSIGGAS")) wert = 238680;
            return wert;
        }

        /// <summary>
        /// Der Satz der fuenf Emissionswerte, den der BHKW-Editor mit „Eintragen" setzt.
        /// <c>null</c> in einem Feld heisst: dieser Brennstoff hat dafuer keine Vorgabe,
        /// der bisherige Wert bleibt stehen.
        /// </summary>
        public sealed record BhkwSatz(double? SO2, double? CO2, double? NOx, double? CO, double? Staub);

        /// <summary>
        /// Die Emissionsvorgaben des BHKW-Editors. Wortgleich
        /// <c>Form_DBBHKW.btn_Eintragen_Click</c>: Heizöl mit und ohne SCR, Gasarten
        /// nach thermischer Leistung (Grenze 1 000 kW).
        /// </summary>
        /// <param name="brennstoff">Anzeigename des gewaehlten Brennstoffs.</param>
        /// <param name="scr">Schalter „mit SCR".</param>
        /// <param name="ptherm">Thermische Leistung [kW] - nur bei den Gasarten wirksam.</param>
        /// <returns>
        /// Fuenf Werte, jeder <c>null</c>, wenn der Bestand ihn nicht gesetzt hat. Trifft
        /// kein Brennstoff, sind alle fuenf <c>null</c> und nichts aendert sich.
        /// </returns>
        public static BhkwSatz Bhkw(string brennstoff, bool scr, double ptherm)
        {
            string oben = (brennstoff ?? "").ToUpper();

            double? so2 = null, co2 = null, nox = null, co = null, staub = null;

            // Wenn Heizöl aktiviert ist, trage die entsprechenden Werte ein
            if (oben.Contains("HEIZÖL"))
            {
                so2 = 270;
                co2 = 265000;
                if (scr) { nox = 450; co = 280; staub = 80; }
                else { nox = 4400; co = 140; staub = 80; }
            }

            // Wenn Gas oder Biogas aktiviert ist, trage die entsprechenden Werte ein.
            // Kein else: Ein Brennstoff, der beide Bedingungen erfuellt, bekommt die
            // Gaswerte - Bestandsverhalten.
            if (oben.Contains("STADTGAS") || oben.Contains("ERDGAS") || oben.Contains("BIOGAS"))
            {
                so2 = 0;
                co2 = 200000;
                if (ptherm > 1000) { nox = 250; co = 250; staub = 0; }
                else { nox = 285; co = 370; staub = 0; }
            }

            return new BhkwSatz(so2, co2, nox, co, staub);
        }
    }
}
