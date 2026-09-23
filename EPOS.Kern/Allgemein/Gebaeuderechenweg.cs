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
    /// NULL folgt <c>SimulationWaermebedarf.MODELL_OHNE_ANGABE</c> — dem VDI-Weg. Diese Klasse
    /// liest dieselbe Konstante; der Dialog zeigt ein Gebäude ohne Angabe deshalb als
    /// „VDI 6007".</para>
    ///
    /// <para>Rein, ohne Datenbank; öffentlich, weil der Katalogeditor in <c>EPOS.UI</c> sie
    /// braucht (dieselbe Lage wie <see cref="Gebaeudebauweise"/>).</para>
    /// </summary>
    public static class Gebaeuderechenweg
    {
        /// <summary>
        /// Der Rechenweg eines Gebäudes ohne Angabe (Spaltenwert NULL):
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

        /// <summary>Infiltration [1/h] (Stufe G2).</summary>
        public static double LuftwechselInfiltration => GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION;

        /// <summary>Nutzerlüftung [1/h] (Stufe G2).</summary>
        public static double LuftwechselNutzer => GebaeudeFestwerte.VORGABE_LUFTWECHSEL_NUTZER;

        /// <summary>Luftwechsel der Sommerlüftung [1/h] (Stufe G2).</summary>
        public static double LuftwechselSommer => GebaeudeFestwerte.SOMMERLUEFTUNG_LUFTWECHSEL;

        /// <summary>Einschaltschwelle der Sommerlüftung [°C] (Stufe G2).</summary>
        public static double SommerlueftungSchwelle => GebaeudeFestwerte.SOMMERLUEFTUNG_SCHWELLE;

        /// <summary>
        /// <b>Der Luftwechsel, mit dem der VDI-Weg rechnet</b> [1/h] (Stufe G2;
        /// Softwarearchitektur 2.8, Rechenschritte A7) — die eine Stelle der Regel, die
        /// Eingangsbauer und Gebäudedialog teilen:
        /// <list type="number">
        /// <item>ist Infiltration oder Nutzerlüftung gesetzt, gilt ihre Summe, das fehlende
        /// Glied mit seiner Vorgabe (0,3 bzw. 0,4 1/h);</item>
        /// <item>sind beide leer, gilt die <c>Luftwechselrate</c> des Gebäudes, sofern sie größer
        /// null ist;</item>
        /// <item>sonst die Summe der beiden Vorgaben (0,7 1/h).</item>
        /// </list>
        /// Der Rückfall wird nie still überschrieben — <paramref name="herkunft"/> nennt ihn.
        /// </summary>
        public static double WirksamerLuftwechsel(double? luftwechselrate, double? infiltration,
                                                  double? nutzer, out Luftwechselherkunft herkunft)
        {
            if (infiltration.HasValue || nutzer.HasValue)
            {
                herkunft = Luftwechselherkunft.InfiltrationUndNutzer;
                return (infiltration ?? LuftwechselInfiltration) + (nutzer ?? LuftwechselNutzer);
            }
            if (luftwechselrate is double n && n > 0.0 && !double.IsInfinity(n))
            {
                herkunft = Luftwechselherkunft.Luftwechselrate;
                return n;
            }
            herkunft = Luftwechselherkunft.Vorgabe;
            return LuftwechselInfiltration + LuftwechselNutzer;
        }

        /// <summary>Dasselbe ohne Herkunft.</summary>
        public static double WirksamerLuftwechsel(double? luftwechselrate, double? infiltration, double? nutzer)
            => WirksamerLuftwechsel(luftwechselrate, infiltration, nutzer, out _);
    }

    /// <summary>Woher der Luftwechsel des VDI-Wegs kommt (<see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?, out Luftwechselherkunft)"/>).</summary>
    public enum Luftwechselherkunft
    {
        /// <summary>Summe aus Infiltration und Nutzerlüftung (Stufe G2).</summary>
        InfiltrationUndNutzer,

        /// <summary>Beide leer: die Luftwechselrate des Gebäudes.</summary>
        Luftwechselrate,

        /// <summary>Alles leer bzw. null: die Summe der beiden Vorgaben.</summary>
        Vorgabe,
    }
}
