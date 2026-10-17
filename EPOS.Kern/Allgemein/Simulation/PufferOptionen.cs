using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Rechenregeln der Pufferoptionen</b> (Welle M7 der Entscheidungsvorlage Modellgrenzen:
    /// PS1 (c) Bereitschaftsverlust temperaturabhängig, PS1 (a) Zonenanteile, PS5 (a) Frischwassermodul;
    /// Konzept Simulationsablauf Abschnitt 21) — ohne Datenbank, ohne Zustand. Der Speicher
    /// (<see cref="SimulationPufferspeicher"/>) und der Dialog rufen dieselben Formeln.
    ///
    /// <para><b>Leer rechnet wie zuvor:</b> Bereitschaftsweg „tag", gleich große Zonen, kein
    /// Frischwassermodul. Die Felder stehen an der Projektkopie <c>Tab_Pufferspeicher</c>
    /// (<see cref="PufferOptionenSchema"/>).</para>
    /// </summary>
    public static class PufferOptionen
    {
        /// <summary>
        /// Prüftemperaturdifferenz des Bereitschaftswerts [K]: Der Katalogwert Q_B [kWh/24 h] gilt nach
        /// EN 12897 / EN 15332 bei 45 K zwischen Speicher und Umgebung (65 °C gegen 20 °C).
        /// </summary>
        public const double PRUEF_DELTA_T_K = 45.0;

        /// <summary>Temperatur des Aufstellraums ohne gepflegten Wert [°C].</summary>
        public const double AUFSTELLRAUM_VORGABE_C = 20.0;

        /// <summary>Grädigkeit des Frischwassermoduls ohne gepflegten Wert [K].</summary>
        public const double FWM_GRAEDIGKEIT_VORGABE_K = 5.0;

        /// <summary>Zapftemperatur, wenn das Projekt keine führt [°C].</summary>
        public const double ZAPFTEMPERATUR_VORGABE_C = 60.0;

        /// <summary>Zulässige Abweichung der Anteilssumme von 1.</summary>
        public const double ANTEILSUMME_TOLERANZ = 0.001;

        /// <summary>
        /// Vorschlag „Kombispeicher" für vier Zonen von oben: Bereitschaftsteil, Übergang, zwei
        /// Heizungszonen — 0,10 / 0,16 / 0,37 / 0,37. Ein Planungsvorschlag nach der Gliederung der
        /// Standardaufteilung in prEN 15316-5 (Anhang B), kein Normwert; die Norm selbst lag bei der
        /// Umsetzung nicht vor.
        /// </summary>
        public static readonly double[] VORSCHLAG_KOMBISPEICHER = { 0.10, 0.16, 0.37, 0.37 };

        // =====================================================================================
        //  PS1 (c) — Bereitschaftsverlust temperaturabhängig
        // =====================================================================================

        /// <summary>
        /// Ist der Weg „temperatur" gewählt? Leer, „tag" und jeder andere Text heißen Tageswert.
        /// </summary>
        public static bool IstTemperaturweg(string weg)
            => string.Equals(weg, DbWerte.PSP_BEREITSCHAFT_TEMPERATUR, StringComparison.Ordinal);

        /// <summary>
        /// Wärmeverlustkoeffizient des Speichers [W/K] aus dem Bereitschaftswert Q_B [kWh/24 h]:
        /// <c>H = Q_B · 1000 / (24 · 45 K)</c>. Ein negativer Wert gilt als 0.
        /// </summary>
        public static double VerlustkoeffizientWK(double bereitschaftKwhTag)
        {
            if (!(bereitschaftKwhTag > 0)) return 0;
            return bereitschaftKwhTag * 1000.0 / (24.0 * PRUEF_DELTA_T_K);
        }

        /// <summary>
        /// Verlust EINER Zone in einer Stunde [kWh]: <c>H · a_i · (ϑ_i − ϑ_Raum) · 1 h / 1000</c>,
        /// nicht negativ (eine Zone unter Raumtemperatur nimmt keine Wärme auf) und höchstens der
        /// Inhalt der Zone über dem Rücklauf (<paramref name="inhaltKwh"/>) — eine Zone fällt nicht
        /// unter die Rücklauftemperatur. Bei gleich großen Zonen ist <c>a_i = 1/N</c>.
        /// </summary>
        public static double ZonenverlustKwh(double hWK, double anteil, double zoneC, double raumC, double inhaltKwh)
        {
            double v = hWK * anteil * (zoneC - raumC) / 1000.0;
            if (!(v > 0)) return 0;
            if (inhaltKwh <= 0) return 0;
            return v < inhaltKwh ? v : inhaltKwh;
        }

        /// <summary>Temperatur des Aufstellraums [°C]: gepflegt und im Band 0 … 35, sonst 20 °C.</summary>
        public static double Aufstellraum(double? wertC)
        {
            if (wertC.HasValue && wertC.Value >= PufferOptionenSchema.AUFSTELLRAUM_MIN &&
                wertC.Value <= PufferOptionenSchema.AUFSTELLRAUM_MAX)
                return wertC.Value;
            return AUFSTELLRAUM_VORGABE_C;
        }

        // =====================================================================================
        //  PS1 (a) — Zonenanteile
        // =====================================================================================

        /// <summary>
        /// Liest und prüft den Anteiltext „0,10;0,16;0,37;0,37" (von oben, Komma oder Punkt als
        /// Dezimalzeichen, Semikolon als Trenner) gegen die Zonenzahl.
        /// </summary>
        /// <param name="text">Der Text; leer = gleich große Zonen (dann <c>null</c> ohne Fehler).</param>
        /// <param name="zonen">Die Zonenzahl N.</param>
        /// <param name="anteile">Die Anteile von oben; <c>null</c> = gleich groß.</param>
        /// <returns><c>null</c> = in Ordnung, sonst die benannte Ablehnung.</returns>
        public static string AnteilePruefen(string text, int zonen, out double[] anteile)
        {
            anteile = null;
            if (string.IsNullOrWhiteSpace(text)) return null;

            string[] teile = text.Split(';');
            var werte = new List<double>();
            foreach (string roh in teile)
            {
                string s = roh.Trim().Replace(',', '.');
                if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) ||
                    double.IsNaN(w) || double.IsInfinity(w))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_ANTEILE_ZAHL, roh.Trim());
                if (!(w > 0) || w >= 1 + ANTEILSUMME_TOLERANZ)
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_ANTEILE_BEREICH, roh.Trim());
                werte.Add(w);
            }

            if (werte.Count != zonen)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_ANTEILE_ANZAHL,
                                     werte.Count, zonen);

            double summe = 0;
            foreach (double w in werte) summe += w;
            if (Math.Abs(summe - 1.0) > ANTEILSUMME_TOLERANZ)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PSP_ANTEILE_SUMME,
                                     summe.ToString("0.###", CultureInfo.CurrentCulture));

            // Auf die Summe 1 normieren: Die Toleranz von 0,001 darf die Kapazität nicht verschieben.
            anteile = new double[werte.Count];
            for (int i = 0; i < werte.Count; i++) anteile[i] = werte[i] / summe;
            return null;
        }

        /// <summary>Die Anteile als Text von oben, zwei Nachkommastellen, Komma, Semikolon als Trenner.</summary>
        public static string AnteileText(IReadOnlyList<double> anteile)
        {
            if (anteile == null || anteile.Count == 0) return "";
            var teile = new string[anteile.Count];
            for (int i = 0; i < anteile.Count; i++)
                teile[i] = anteile[i].ToString("0.00##", CultureInfo.InvariantCulture).Replace('.', ',');
            return string.Join(";", teile);
        }

        /// <summary>Der Vorschlag „Kombispeicher" als Text (vier Zonen).</summary>
        public static string VorschlagKombispeicherText() => AnteileText(VORSCHLAG_KOMBISPEICHER);

        // =====================================================================================
        //  PS5 (a) — Frischwassermodul
        // =====================================================================================

        /// <summary>Grädigkeit des Frischwassermoduls [K]: gepflegt und im Band 0 … 20, sonst 5 K.</summary>
        public static double FwmGraedigkeit(double? wertK)
        {
            if (wertK.HasValue && wertK.Value >= 0 && wertK.Value <= PufferOptionenSchema.FWM_GRAEDIGKEIT_MAX)
                return wertK.Value;
            return FWM_GRAEDIGKEIT_VORGABE_K;
        }

        /// <summary>Mindesttemperatur oben im Speicher [°C]: <c>ϑ_Zapf + ΔT_FWM</c>.</summary>
        public static double FwmMindesttemperatur(double zapfC, double graedigkeitK) => zapfC + graedigkeitK;

        /// <summary>
        /// Darf das Frischwassermodul aus der obersten Zone zapfen? Nur, wenn sie die Mindesttemperatur
        /// hält — mit dem Zahlenrand der Betriebsschwellen (<see cref="Rechenrand.SchwelleErreicht"/>).
        /// </summary>
        public static bool FwmFreigabe(double obenC, double zapfC, double graedigkeitK)
            => Rechenrand.SchwelleErreicht(obenC, FwmMindesttemperatur(zapfC, graedigkeitK));
    }
}
