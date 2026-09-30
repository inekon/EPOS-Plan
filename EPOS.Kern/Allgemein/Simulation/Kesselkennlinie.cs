using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die BAUART eines Heizkessels, soweit sie die Normvorgabe des Teillastwirkungsgrads
    /// bestimmt (Konzept Kesselkennlinie 7.1, Entscheid F1).
    /// </summary>
    public enum KesselBauart
    {
        /// <summary>Niedertemperaturkessel — und jeder Kessel, den weder Schalter noch Bauart anders ausweisen.</summary>
        Niedertemperatur = 0,

        /// <summary>Brennwertkessel: <c>Tab_Heizkessel.Brennwert</c> = 1.</summary>
        Brennwert = 1,

        /// <summary>Standardkessel: die Beschreibung nennt die VDI-Bauart „Standard…“.</summary>
        Standard = 2
    }

    /// <summary>
    /// <b>Die Teillastkennlinie des Heizkessels</b> (Konzept Kesselkennlinie 4.1, Etappe E2):
    /// der Wirkungsgrad einer Stunde aus ihrer Laststufe, gestützt auf den Wirkungsgrad bei
    /// Nennlast η₁₀₀ und bei 30 % Last η₃₀ — reine, zustandslose Funktionen, die Rechenweg,
    /// Oberfläche und Tests gleichermaßen rufen.
    /// </summary>
    /// <remarks>
    /// <para><b>Die Kurve (Option A).</b> Zwischen β = 0,3 und β = 1 linear von η₃₀ nach η₁₀₀,
    /// darunter η₃₀: η(β) = η₃₀ + (η₁₀₀ − η₃₀) · (β − 0,3)/0,7. Sie trifft beide Prüfpunkte der
    /// Richtlinie 92/42/EWG exakt; alle Werte sind heizwertbezogene Faktoren.</para>
    /// <para><b>Leeres η₃₀ nimmt die Normvorgabe nach Bauart</b> (Konzept 7.1, Entscheid F1):
    /// Brennwertkessel η₁₀₀ + 0,06, höchstens Hs/Hi des Brennstoffs, Niedertemperaturkessel
    /// η₁₀₀, Standardkessel η₁₀₀ − 0,03. Ein gepflegter Wert geht immer vor. Beim
    /// Niedertemperaturkessel ist die Kurve damit flach, und der Kessel rechnet Stunde für
    /// Stunde bitgleich mit dem festen Wirkungsgrad.</para>
    /// <para><b>Keine Betriebsschwelle, kein Rand.</b> Die Kurve ist in β stetig; die Klemmung
    /// an 0,3 und an 1 schaltet keinen Zustand, sie knickt nur die Gerade. Ein β, das am letzten
    /// Bit vor oder hinter dem Knick liegt, verschiebt η um höchstens ein ulp — anders als eine
    /// Speicherhysterese, die an derselben Stelle Stunden später anders entscheidet. Die einzige
    /// Betriebsentscheidung des Kessels bleibt „läuft er?“ (<see cref="SimulationSPK.KesselLaeuft"/>,
    /// mit dem Zahlenrand). Die Kurve ist lineare Arithmetik und braucht keine Plattformnaht.</para>
    /// <para><b>Elektrokessel</b> (Brennstoff 13) rechnen nie mit Kennlinie
    /// (<see cref="RechnetMitKennlinie"/>).</para>
    /// <para>Die Brennwertkennlinie (Rücklauf, Kondensationsgewinn) und das Takten folgen mit den
    /// Etappen E3 und E4; der Schalter <c>Kennlinie_Brennwert</c> und die Felder
    /// <c>Mindestleistung</c>, <c>Anfahrverlust_kWh</c>, <c>Mindestlaufzeit_min</c> rechnen hier
    /// noch nicht mit.</para>
    /// </remarks>
    public static class Kesselkennlinie
    {
        /// <summary>Die Laststufe des Teillast-Prüfpunkts (30 % der Nennleistung).</summary>
        public const double LASTSTUFE_TEILLAST = 0.3;

        /// <summary>Normvorgabe: η₃₀ des Brennwertkessels liegt so weit über η₁₀₀ (Konzept 7.1).</summary>
        public const double VORGABE_ZUSCHLAG_BRENNWERT = 0.06;

        /// <summary>Normvorgabe: η₃₀ des Standardkessels liegt so weit unter η₁₀₀ (Konzept 7.1).</summary>
        public const double VORGABE_ABSCHLAG_STANDARD = 0.03;

        /// <summary>Verhältnis Brennwert zu Heizwert der Gase (Erdgas, Flüssiggas, Biogas), gerundet.</summary>
        public const double HSHI_GAS = 1.11;

        /// <summary>Verhältnis Brennwert zu Heizwert der Heizöle, gerundet.</summary>
        public const double HSHI_OEL = 1.06;

        /// <summary>Verhältnis Brennwert zu Heizwert von Holz und Holzpellets, gerundet.</summary>
        public const double HSHI_HOLZ = 1.08;

        /// <summary>
        /// Verhältnis für jeden übrigen Brennstoff: 1, also kein Kondensationsgewinn — ohne bekannte
        /// Näherung setzt die Normvorgabe keine Anhebung über den Heizwert an.
        /// </summary>
        public const double HSHI_OHNE_BRENNWERT = 1.0;

        /// <summary>
        /// Rechnet ein Kessel dieses Brennstoffs mit Kennlinie? Alle außer dem Elektrokessel
        /// (<see cref="SimulationSPK.IstStromkesselBrennstoff"/>).
        /// </summary>
        public static bool RechnetMitKennlinie(int brennstoffArt)
            => !SimulationSPK.IstStromkesselBrennstoff(brennstoffArt);

        /// <summary>
        /// Die Bauart für die Normvorgabe (Konzept 7.1): <paramref name="brennwert"/> gesetzt →
        /// Brennwertkessel; eine Beschreibung, die mit der VDI-Bauart „Standard…“ beginnt →
        /// Standardkessel; sonst Niedertemperaturkessel.
        /// </summary>
        public static KesselBauart Bauart(bool brennwert, string beschreibung)
        {
            if (brennwert) return KesselBauart.Brennwert;
            string b = (beschreibung ?? "").Trim();
            if (b.StartsWith("Standard", StringComparison.OrdinalIgnoreCase)) return KesselBauart.Standard;
            return KesselBauart.Niedertemperatur;
        }

        /// <summary>
        /// Das Verhältnis Brennwert zu Heizwert (Hs/Hi) nach Brennstoffgruppe — die Obergrenze
        /// jedes heizwertbezogenen Wirkungsgrads. Gase (<c>Tab_Brennstoff_Stamm</c> 1–5, 14)
        /// <see cref="HSHI_GAS"/>, Heizöle (6–9, 18–22) <see cref="HSHI_OEL"/>, Holz und Pellets
        /// (12, 15) <see cref="HSHI_HOLZ"/>, alle übrigen <see cref="HSHI_OHNE_BRENNWERT"/>.
        /// Dieselben Bereiche wie die Brennstoffzähler in <c>SimulationSPK</c>.
        /// </summary>
        public static double HsHi(int brennstoffArt)
        {
            if ((brennstoffArt >= 1 && brennstoffArt <= 5) || brennstoffArt == 14) return HSHI_GAS;
            if ((brennstoffArt >= 6 && brennstoffArt <= 9) || (brennstoffArt >= 18 && brennstoffArt <= 22)) return HSHI_OEL;
            if (brennstoffArt == 12 || brennstoffArt == 15) return HSHI_HOLZ;
            return HSHI_OHNE_BRENNWERT;
        }

        /// <summary>
        /// Die NORMVORGABE für ein leeres η₃₀ (Konzept 7.1, Entscheid F1).
        /// </summary>
        /// <remarks>
        /// Brennwertkessel: η₁₀₀ + <see cref="VORGABE_ZUSCHLAG_BRENNWERT"/>, höchstens
        /// <see cref="HsHi"/> des Brennstoffs — die Obergrenze nimmt nur die Anhebung zurück,
        /// sie drückt η₃₀ nie unter η₁₀₀. Standardkessel: η₁₀₀ − <see cref="VORGABE_ABSCHLAG_STANDARD"/>.
        /// Niedertemperaturkessel: η₁₀₀.
        /// </remarks>
        public static double Eta30Vorgabe(double eta100, KesselBauart bauart, int brennstoffArt)
        {
            switch (bauart)
            {
                case KesselBauart.Brennwert:
                    double angehoben = eta100 + VORGABE_ZUSCHLAG_BRENNWERT;
                    double grenze = HsHi(brennstoffArt);
                    if (angehoben <= grenze) return angehoben;
                    return grenze > eta100 ? grenze : eta100;
                case KesselBauart.Standard:
                    return eta100 - VORGABE_ABSCHLAG_STANDARD;
                default:
                    return eta100;
            }
        }

        /// <summary>
        /// Das WIRKSAME η₃₀: der gepflegte Wert als Faktor (über 1,5 als Prozentangabe gelesen,
        /// <see cref="KesselKennlinieWerte.AlsFaktor"/>), sonst — leer, nicht endlich oder nicht
        /// positiv — die <see cref="Eta30Vorgabe"/>.
        /// </summary>
        public static double Eta30Wirksam(double? eta30Gepflegt, double eta100, KesselBauart bauart, int brennstoffArt)
        {
            double? gepflegt = KesselKennlinieWerte.AlsFaktor(eta30Gepflegt);
            if (Eta30IstGepflegt(gepflegt)) return gepflegt.Value;
            return Eta30Vorgabe(eta100, bauart, brennstoffArt);
        }

        /// <summary>Trägt der Kessel ein verwendbares eigenes η₃₀ (endlich und positiv)?</summary>
        public static bool Eta30IstGepflegt(double? eta30Gepflegt)
        {
            if (!eta30Gepflegt.HasValue) return false;
            double v = eta30Gepflegt.Value;
            return !double.IsNaN(v) && !double.IsInfinity(v) && v > 0;
        }

        /// <summary>
        /// Die LASTSTUFE β einer Stunde: brennstoffbasierte Wärme durch Nennleistung, auf [0, 1]
        /// geklemmt. Ohne Nennleistung gilt Volllast (β = 1, der Kessel rechnet mit η₁₀₀).
        /// </summary>
        public static double Laststufe(double waermeKwh, double nennleistungKw)
        {
            if (!(nennleistungKw > 0) || double.IsInfinity(nennleistungKw)) return 1.0;
            double beta = waermeKwh / nennleistungKw;
            if (!(beta > 0)) return 0.0;
            return beta < 1.0 ? beta : 1.0;
        }

        /// <summary>
        /// Der WIRKUNGSGRAD bei der Laststufe <paramref name="laststufe"/> (Option A, trockene
        /// Teillastkurve): η₃₀ + (η₁₀₀ − η₃₀) · (β − 0,3)/0,7, β auf [0,3; 1] geklemmt.
        /// </summary>
        /// <remarks>
        /// Bei β = 0,3 und darunter η₃₀, bei β = 1 η₁₀₀ — für η-Werte, die sich um weniger als
        /// den Faktor zwei unterscheiden, bitgenau (die Differenz ist dann exakt). Ist η₃₀ = η₁₀₀,
        /// ist das Ergebnis für jedes β bitgleich η₁₀₀. Ein nicht endliches β zählt wie Teillast.
        /// </remarks>
        public static double Eta(double laststufe, double eta100, double eta30)
        {
            double b = laststufe >= LASTSTUFE_TEILLAST
                ? (laststufe <= 1.0 ? laststufe : 1.0)
                : LASTSTUFE_TEILLAST;
            return eta30 + (eta100 - eta30) * ((b - LASTSTUFE_TEILLAST) / (1.0 - LASTSTUFE_TEILLAST));
        }
    }
}
