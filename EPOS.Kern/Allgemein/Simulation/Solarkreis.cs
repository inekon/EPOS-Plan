using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln des Solarkreises</b> — Verluste, Pumpenstrom, Bezugsfläche,
    /// Einfallswinkelkorrektur und Arbeitstemperatur des Kollektorfelds (Welle M2 der
    /// Entscheidungsvorlage „Modellgrenzen der Rechenwege": ST1 bis ST6). Ohne Datenbank und
    /// ohne Zustand: <see cref="SimulationSolarthermie"/> ruft sie im Lauf, die Tests rufen sie
    /// gegen die Handrechnung.
    ///
    /// <para><b>Jede Vorgabe rechnet wie vor der Welle:</b> Verluste 8 %, Aperturfläche, kein
    /// Pumpenstrom ohne gepflegten Wert, feste Arbeitstemperatur 50 °C, Diffusanteil mit dem
    /// Faktor der Direktstrahlung, solange der Katalog kein <c>K_dfu</c> führt.</para>
    /// </summary>
    public static class Solarkreis
    {
        /// <summary>Verluste des Solarkreises ohne gepflegten Wert [% des Bruttoertrags].</summary>
        public const double VERLUSTE_VORGABE_PROZENT = 8;

        /// <summary>
        /// Der Faktor auf das Bruttopotenzial, der nach den Verlusten des Solarkreises bleibt
        /// (ST3 Stufe 1): <c>(100 − Verluste) / 100</c>. Leer, nicht endlich oder außerhalb
        /// 0 … 50 % gilt die Vorgabe 8 % — der Faktor ist dann bitgleich das Literal 0,92 des
        /// Rechenwegs vor der Welle.
        /// </summary>
        public static double Verlustfaktor(double? verlusteProzent)
        {
            double p = VERLUSTE_VORGABE_PROZENT;
            if (verlusteProzent.HasValue && !double.IsNaN(verlusteProzent.Value) &&
                verlusteProzent.Value >= 0 && verlusteProzent.Value <= SolarthermieFelderSchema.VERLUSTE_MAX_PROZENT)
                p = verlusteProzent.Value;
            return (100.0 - p) / 100.0;
        }

        /// <summary>
        /// Pumpenstrom des Kollektorfelds in EINER Stunde [kWh] (ST1, VDI 6002 Blatt 1,
        /// EN 15316-4-3): <c>E_P = P_P · 1 h</c>, wenn das Feld in der Stunde Wärme abgibt
        /// (Direktdeckung oder Ladung &gt; 0), sonst 0. Ohne gepflegte Pumpenleistung hilfsweise
        /// der Hilfsenergieanteil der Anlage [%] auf die in der Stunde genutzte Wärme; ohne
        /// beides 0.
        /// </summary>
        /// <param name="pumpenleistungW">Pumpenleistung [W]; leer oder ≤ 0 = nicht gepflegt.</param>
        /// <param name="hilfsenergieAnteilProzent"><c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> [%].</param>
        /// <param name="genutztKwh">In der Stunde genutzte Wärme des Felds (Deckung plus Ladung) [kWh].</param>
        public static double PumpenstromKwh(double? pumpenleistungW, double? hilfsenergieAnteilProzent,
                                            double genutztKwh)
        {
            if (!(genutztKwh > 0)) return 0;
            if (pumpenleistungW.HasValue && pumpenleistungW.Value > 0)
                return pumpenleistungW.Value / 1000.0;
            if (hilfsenergieAnteilProzent.HasValue && hilfsenergieAnteilProzent.Value > 0)
                return genutztKwh * hilfsenergieAnteilProzent.Value / 100.0;
            return 0;
        }

        // =================================================================
        // ST6 - Bezugsfläche der Kollektorkennwerte
        // =================================================================

        /// <summary>Die zulässigen Werte der Bezugsfläche, in der Reihenfolge der Auswahl.</summary>
        public static readonly string[] BEZUGSFLAECHEN =
            { DbWerte.SOLAR_BEZUGSFLAECHE_APERTUR, DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO };

        /// <summary>
        /// Die Bezugsfläche eines Kollektorsatzes als Persistenzwert: „brutto" nur, wenn es dasteht
        /// (Groß- und Kleinschreibung gleich); alles andere — leer, NULL, fehlende Spalte — ist
        /// „apertur", die Vorgabe.
        /// </summary>
        public static string Bezugsflaeche(string wert)
            => string.Equals((wert ?? "").Trim(), DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO, StringComparison.OrdinalIgnoreCase)
                ? DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO
                : DbWerte.SOLAR_BEZUGSFLAECHE_APERTUR;

        /// <summary>
        /// Die Fläche EINES Moduls, auf die η₀, a₁ und a₂ bezogen sind (ST6) [m²]: die Aperturfläche
        /// (Vorgabe; ältere Datenblätter nach EN 12975) oder die Bruttofläche (Modulfläche; Prüfberichte
        /// nach EN ISO 9806:2017). Passen Fläche und Kennwerte nicht zusammen, liegt der Ertrag um das
        /// Verhältnis Apertur/Brutto daneben.
        /// </summary>
        /// <param name="bezug"><c>Bezugsflaeche</c> des Kollektorsatzes.</param>
        /// <param name="apertur">Aperturfläche eines Moduls [m²].</param>
        /// <param name="brutto">Bruttofläche (Modulfläche) eines Moduls [m²].</param>
        /// <param name="rueckfall">
        /// true, wenn „brutto" gewählt ist, der Satz aber keine Bruttofläche führt — dann gilt die
        /// Aperturfläche, und der Lauf sagt es.
        /// </param>
        public static double Modulbezugsflaeche(string bezug, double apertur, double brutto, out bool rueckfall)
        {
            rueckfall = false;
            if (Bezugsflaeche(bezug) != DbWerte.SOLAR_BEZUGSFLAECHE_BRUTTO) return apertur;
            if (brutto > 0) return brutto;
            rueckfall = true;
            return apertur;
        }

        // =================================================================
        // ST5 - Einfallswinkelkorrektur getrennt für Direkt- und Diffusstrahlung
        // =================================================================

        /// <summary>Führt der Kollektorsatz eine Einfallswinkelkorrektur der Diffusstrahlung (<c>K_dfu</c> &gt; 0)?</summary>
        public static bool KdfuGepflegt(double kDfu) => kDfu > 0 && !double.IsNaN(kDfu);

        /// <summary>
        /// Einfallswinkelkorrektur der DIREKTstrahlung <c>K_b(θ)</c> nach der b₀-Näherung aus
        /// <c>K_dir50</c> (EN ISO 9806): <c>b₀ = (1 − K_dir50)/(1/cos 50° − 1)</c>,
        /// <c>K_b = 1 − b₀·(1/cos θ − 1)</c>, auf 0 … 1 geklemmt — Rechenschritt für Rechenschritt
        /// die Korrektur in <c>SimulationSolarthermie.CalculateThermalPower</c>.
        /// </summary>
        public static double IamDirekt(double cosTheta, double kDir50)
        {
            double cos50 = Plattformrundung.Cos(50.0 * Math.PI / 180.0);
            double b0 = (1.0 - kDir50) / (1.0 / cos50 - 1.0);
            double cosThetaClamped = Math.Max(cosTheta, 0.001);
            double iam = 1.0 - b0 * (1.0 / cosThetaClamped - 1.0);
            return Math.Max(Math.Min(iam, 1.0), 0.0);
        }

        /// <summary>
        /// Spezifische Kollektorleistung [W/m²] mit GETRENNTER Einfallswinkelkorrektur (ST5,
        /// EN ISO 9806): <c>q = η₀·(K_b(θ)·G_b + K_d·G_dr) − a₁·ΔT − a₂·ΔT²</c>, nicht negativ, mit
        /// <c>G_b</c> der Direktstrahlung auf die geneigte Fläche, <c>G_dr</c> der Diffus- und
        /// Bodenreflexstrahlung, <c>K_d = K_dfu</c> und <c>ΔT = ϑ_m − ϑ_a</c>. Ohne <c>K_dfu</c>
        /// gilt <c>K_d = K_b(θ)</c> — algebraisch die Rechnung vor der Welle.
        /// </summary>
        /// <param name="gDirekt">Direktstrahlung auf die Kollektorebene [W/m²].</param>
        /// <param name="gDiffusReflex">Diffus- und Bodenreflexstrahlung auf die Kollektorebene [W/m²].</param>
        /// <param name="tAussen">Außentemperatur [°C].</param>
        /// <param name="tMittel">Mittlere Fluidtemperatur des Kollektors [°C].</param>
        /// <param name="cosTheta">Kosinus des Einfallswinkels der Direktstrahlung.</param>
        public static double LeistungJeQm(double gDirekt, double gDiffusReflex, double tAussen, double tMittel,
                                          double cosTheta, double h0, double a1, double a2,
                                          double kDir50, double kDfu)
        {
            double g = gDirekt + gDiffusReflex;
            if (!(g > 0)) return 0;

            double kb = IamDirekt(cosTheta, kDir50);
            double kd = KdfuGepflegt(kDfu) ? kDfu : kb;
            double dT = tMittel - tAussen;
            double q = h0 * (kb * gDirekt + kd * gDiffusReflex) - a1 * dT - a2 * dT * dT;
            return Math.Max(0, q);
        }

        /// <summary>Rechnet das Feld überhaupt einen Pumpenstrom?</summary>
        public static bool RechnetPumpenstrom(double? pumpenleistungW, double? hilfsenergieAnteilProzent)
            => (pumpenleistungW.HasValue && pumpenleistungW.Value > 0) ||
               (hilfsenergieAnteilProzent.HasValue && hilfsenergieAnteilProzent.Value > 0);
    }
}
