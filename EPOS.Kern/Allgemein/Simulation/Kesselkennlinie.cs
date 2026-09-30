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
    /// <para><b>Die Brennwertkennlinie (Konzept 4.1 Punkte 3 bis 5, Etappe E3)</b> rechnet nur ein
    /// Brennwertkessel mit <c>Kennlinie_Brennwert</c> = 1 (<see cref="RechnetMitBrennwertkennlinie"/>):
    /// Die trockene Teillastkurve stützt sich auf η₃₀,tr = η₃₀ − Δ₃₀, weil η₃₀ bei 30 °C Rücklauf
    /// gemessen ist, und der Kondensationsgewinn Δ₃₀ · g(T_RL) kommt nach dem Rücklauf der Stunde
    /// dazu (<see cref="EtaBrennwert"/>). Den Rücklauf wählt <see cref="Ruecklauf"/> aus der
    /// Rücklaufkette. Die Kurve ist in β und in T_RL stetig, lineare Arithmetik ohne Funktion der
    /// Plattformnaht; die EINE neue Schwelle — läuft die Stunde im Brennwertbetrieb? — entscheidet
    /// <see cref="Brennwertbetrieb"/> über den Zahlenrand. Das Takten folgt mit Etappe E4; die Felder
    /// <c>Mindestleistung</c>, <c>Anfahrverlust_kWh</c>, <c>Mindestlaufzeit_min</c> rechnen hier noch
    /// nicht mit.</para>
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

        // =====================================================================
        //  Brennwertkennlinie (Konzept 4.1 Punkte 3 bis 5, Etappe E3)
        // =====================================================================

        /// <summary>
        /// Der PRÜFRÜCKLAUF des Teillastwirkungsgrads beim Brennwertkessel [°C]: η₃₀ ist bei 30 %
        /// Last und 30 °C Rücklauf gemessen (Richtlinie 92/42/EWG). Bei diesem Rücklauf ist der
        /// Kondensationsanteil 1.
        /// </summary>
        public const double RUECKLAUF_PRUEFPUNKT_C = 30;

        /// <summary>
        /// Obergrenze des Kondensationsanteils g: Unter dem Prüfrücklauf steigt der Gewinn weiter,
        /// höchstens auf das 1,2-Fache von Δ₃₀ (Konzept 4.1 Punkt 5).
        /// </summary>
        public const double KONDENSATIONSANTEIL_MAX = 1.2;

        /// <summary>
        /// RÜCKFALL-RÜCKLAUF [°C], wenn die Kette weder Heizkreis noch Speicher noch gepflegtes Paar
        /// findet (Anwenderentscheid F5: 50 °C) — dieselbe Zahl wie der Rückfall des Kessel-Hubs
        /// (<see cref="SimulationControl.KESSEL_RUECKLAUF_RUECKFALL"/>, Rückfallpaar 70/50 °C).
        /// </summary>
        public const double RUECKLAUF_RUECKFALL_C = SimulationControl.KESSEL_RUECKLAUF_RUECKFALL;

        /// <summary>Abgastaupunkt der Gase (Erdgas, Flüssiggas, Biogas) [°C], gerundet.</summary>
        public const double TAUPUNKT_GAS_C = 57;

        /// <summary>Abgastaupunkt der Heizöle [°C], gerundet.</summary>
        public const double TAUPUNKT_OEL_C = 47;

        /// <summary>Abgastaupunkt von Holz und Holzpellets [°C], gerundet.</summary>
        public const double TAUPUNKT_HOLZ_C = 50;

        /// <summary>Kondensationsgewinn bei 30 °C Rücklauf, Gase (auf Hi bezogen, Faktor).</summary>
        public const double KONDENSATIONSGEWINN30_GAS = 0.08;

        /// <summary>Kondensationsgewinn bei 30 °C Rücklauf, Heizöle (auf Hi bezogen, Faktor).</summary>
        public const double KONDENSATIONSGEWINN30_OEL = 0.04;

        /// <summary>
        /// Kondensationsgewinn bei 30 °C Rücklauf, Holz und Holzpellets (auf Hi bezogen, Faktor) —
        /// eigene gerundete Ableitung: wie beim Heizöl rund zwei Drittel des Brennwertüberschusses
        /// (Hs/Hi − 1 = 0,08).
        /// </summary>
        public const double KONDENSATIONSGEWINN30_HOLZ = 0.05;

        /// <summary>
        /// Der ABGASTAUPUNKT des Brennstoffs [°C] — dieselben Brennstoffgruppen wie <see cref="HsHi"/>;
        /// <see cref="double.NaN"/> für einen Brennstoff ohne Brennwertnutzung.
        /// </summary>
        public static double Taupunkt(int brennstoffArt)
        {
            if ((brennstoffArt >= 1 && brennstoffArt <= 5) || brennstoffArt == 14) return TAUPUNKT_GAS_C;
            if ((brennstoffArt >= 6 && brennstoffArt <= 9) || (brennstoffArt >= 18 && brennstoffArt <= 22)) return TAUPUNKT_OEL_C;
            if (brennstoffArt == 12 || brennstoffArt == 15) return TAUPUNKT_HOLZ_C;
            return double.NaN;
        }

        /// <summary>
        /// Der KONDENSATIONSGEWINN Δ₃₀ des Brennstoffs bei 30 °C Rücklauf (Faktor, auf Hi bezogen);
        /// 0 für einen Brennstoff ohne Brennwertnutzung.
        /// </summary>
        public static double Kondensationsgewinn30(int brennstoffArt)
        {
            if ((brennstoffArt >= 1 && brennstoffArt <= 5) || brennstoffArt == 14) return KONDENSATIONSGEWINN30_GAS;
            if ((brennstoffArt >= 6 && brennstoffArt <= 9) || (brennstoffArt >= 18 && brennstoffArt <= 22)) return KONDENSATIONSGEWINN30_OEL;
            if (brennstoffArt == 12 || brennstoffArt == 15) return KONDENSATIONSGEWINN30_HOLZ;
            return 0.0;
        }

        /// <summary>
        /// Rechnet ein Kessel mit der BRENNWERTKENNLINIE? Nur ein Brennwertkessel
        /// (<paramref name="brennwert"/>) mit gewählter Kennlinie (<paramref name="kennlinieBrennwert"/>,
        /// Konzept 3.1) und einem Brennstoff mit Kondensationsgewinn; der Elektrokessel nie.
        /// </summary>
        public static bool RechnetMitBrennwertkennlinie(bool brennwert, bool kennlinieBrennwert, int brennstoffArt)
            => brennwert && kennlinieBrennwert && RechnetMitKennlinie(brennstoffArt) &&
               Kondensationsgewinn30(brennstoffArt) > 0;

        /// <summary>
        /// Der KONDENSATIONSANTEIL g(T_RL) = (T_Tau − T_RL)/(T_Tau − 30), auf [0, 1,2] geklemmt:
        /// 0 am und über dem Taupunkt, 1 beim Prüfrücklauf 30 °C. Stetig in T_RL — die Klemmung
        /// schaltet keinen Zustand. Ein nicht endlicher Rücklauf oder Taupunkt trägt keinen Gewinn.
        /// </summary>
        public static double Kondensationsanteil(double ruecklaufC, double taupunktC)
        {
            if (!Endlich(ruecklaufC) || !Endlich(taupunktC)) return 0.0;
            double spanne = taupunktC - RUECKLAUF_PRUEFPUNKT_C;
            if (!(spanne > 0)) return 0.0;
            double g = (taupunktC - ruecklaufC) / spanne;
            if (!(g > 0)) return 0.0;
            return g < KONDENSATIONSANTEIL_MAX ? g : KONDENSATIONSANTEIL_MAX;
        }

        /// <summary>Stellen, auf die <see cref="Eta30Trocken"/> die Differenz rundet.</summary>
        public const int ETA30_TROCKEN_STELLEN = 12;

        /// <summary>
        /// Das TROCKENE η₃₀ des Brennwertkessels: η₃₀ − Δ₃₀ — der Kondensationsanteil, den der bei
        /// 30 °C Rücklauf gemessene Wert enthält, wird herausgenommen (Konzept 4.1 Punkt 3).
        /// </summary>
        /// <remarks>
        /// Die Differenz zweier Dezimalwerte (Katalogwerte tragen höchstens sechs Nachkommastellen)
        /// wird auf <see cref="ETA30_TROCKEN_STELLEN"/> Stellen gerundet: 1,05 − 0,08 ist binär
        /// 0,97000000000000008, gerundet dieselbe Zahl wie der Katalogwert 0,97. Ist η₃₀,tr dezimal
        /// gleich η₁₀₀, ist die trockene Kurve damit flach und bitgleich η₁₀₀ — wie beim
        /// Niedertemperaturkessel in E2 —, und der Teillastbrennstoff ist genau 0 statt eines Rests am
        /// letzten Bit, der mit jeder Stundenwärme anders ausfiele.
        /// </remarks>
        public static double Eta30Trocken(double eta30, int brennstoffArt)
            => Math.Round(eta30 - Kondensationsgewinn30(brennstoffArt), ETA30_TROCKEN_STELLEN, MidpointRounding.ToEven);

        /// <summary>
        /// Der WIRKUNGSGRAD DER BRENNWERTKENNLINIE (Konzept 4.1 Punkte 3 bis 5):
        /// η_eff = η_tr(β) + Δ₃₀ · g(T_RL), mit der trockenen Teillastkurve
        /// η_tr(β) = <see cref="Eta"/>(β, η₁₀₀, η₃₀ − Δ₃₀). Höchstens Hs/Hi des Brennstoffs; die
        /// Obergrenze nimmt nur den Kondensationsgewinn zurück, nie unter die trockene Kurve (wie die
        /// Normvorgabe in E2). <paramref name="etaTrocken"/> gibt η_tr(β) zurück — die Aufteilung des
        /// Mehrbrennstoffs in Teillast und Brennwertnutzung.
        /// </summary>
        /// <remarks>
        /// Probe (Konzept 4.1 Punkt 5): β = 0,3 und T_RL = 30 °C ergibt η₃₀, β = 1 und T_RL am oder über
        /// dem Taupunkt ergibt η₁₀₀ — der Katalog wird an beiden Prüfpunkten getroffen.
        /// </remarks>
        public static double EtaBrennwert(double laststufe, double eta100, double eta30, double ruecklaufC,
                                          int brennstoffArt, out double etaTrocken)
        {
            double delta30 = Kondensationsgewinn30(brennstoffArt);
            etaTrocken = Eta(laststufe, eta100, Eta30Trocken(eta30, brennstoffArt));
            double eff = etaTrocken + delta30 * Kondensationsanteil(ruecklaufC, Taupunkt(brennstoffArt));
            double grenze = HsHi(brennstoffArt);
            if (eff <= grenze) return eff;
            return grenze > etaTrocken ? grenze : etaTrocken;
        }

        /// <summary>
        /// Läuft eine Stunde im BRENNWERTBETRIEB — liegt der Rücklauf unter dem Taupunkt? Eine neue
        /// Betriebsschwelle, deshalb über den Zahlenrand (<see cref="Rechenrand.SchwelleErreicht"/>,
        /// Schwelle ist der Taupunkt, Wert der Rücklauf): Ein Rücklauf, der dezimal genau auf dem
        /// Taupunkt liegt, zählt nicht und kippt nicht am letzten Bit.
        /// </summary>
        public static bool Brennwertbetrieb(double ruecklaufC, double taupunktC)
            => Endlich(ruecklaufC) && Endlich(taupunktC) && !Rechenrand.SchwelleErreicht(ruecklaufC, taupunktC);

        /// <summary>
        /// Die RÜCKLAUFKETTE einer Stunde (Konzept 4.1 Punkt 4) — die erste belegte Stufe gilt:
        /// (a) der gerechnete Rücklauf des Heizkreises (Anlagenkopplung), NaN fällt durch; (b) der
        /// Senkenspeicher (<c>RL_eff</c>, geschichtet die unterste Schicht); (c) das gepflegte Paar
        /// Anlage → Katalog; (d) der Rückfall <see cref="RUECKLAUF_RUECKFALL_C"/>. Eine Stufe ohne
        /// endlichen Wert fällt durch.
        /// </summary>
        public static double Ruecklauf(double heizkreisC, double speicherC, double? paarC, out Ruecklaufstufe stufe)
        {
            if (Endlich(heizkreisC)) { stufe = Ruecklaufstufe.Heizkreis; return heizkreisC; }
            if (Endlich(speicherC)) { stufe = Ruecklaufstufe.Speicher; return speicherC; }
            if (paarC.HasValue && Endlich(paarC.Value)) { stufe = Ruecklaufstufe.Paar; return paarC.Value; }
            stufe = Ruecklaufstufe.Rueckfall;
            return RUECKLAUF_RUECKFALL_C;
        }

        private static bool Endlich(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }

    /// <summary>
    /// Die Stufe der Rücklaufkette, aus der der Rücklauf einer Stunde stammt
    /// (<see cref="Kesselkennlinie.Ruecklauf"/>, Konzept Kesselkennlinie 4.1 Punkt 4).
    /// </summary>
    public enum Ruecklaufstufe
    {
        /// <summary>(a) Der gerechnete Rücklauf des Heizkreises (Anlagenkopplung).</summary>
        Heizkreis = 0,

        /// <summary>(b) Der Senkenspeicher des Kessels.</summary>
        Speicher = 1,

        /// <summary>(c) Das gepflegte Temperaturpaar an Anlage bzw. Heizkessel.</summary>
        Paar = 2,

        /// <summary>(d) Der Rückfall 50 °C.</summary>
        Rueckfall = 3
    }
}
