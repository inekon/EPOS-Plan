using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DER AUSWEIS DES BERECHNUNGSMODUS (Etappe E5, Konzept F7): Jede Stelle, die eine
    /// modusabhängige CO₂-Kennzahl beschriftet, holt ihren Text hier — Bildschirm,
    /// Word und Excel führen damit denselben Wortlaut.
    ///
    /// <para><b>Warum überhaupt:</b> „CO₂-Emissionen 120 t/a" und „CO₂-Äquivalent
    /// 132 t/a" sind zwei verschiedene Größen. Stünde an beiden nur „CO₂", wären zwei
    /// Berichte desselben Projekts stillschweigend nicht vergleichbar — der Leser
    /// sähe eine Verbesserung oder Verschlechterung, wo nur die Methode gewechselt
    /// hat.</para>
    ///
    /// <para><b>Nicht modusabhängig</b> und deshalb hier NICHT vertreten: die
    /// BEHG-Abgabemenge (gesetzlich reines CO₂ nach EBeV — ein Äquivalent wäre dort
    /// falsch), die SO₂-/NOx-Kennzahlen und alles aus der Klasse <c>EF_NACHWEIS</c>.</para>
    /// </summary>
    public static class EmissionsAusweis
    {
        /// <summary>Kennung für einen Variantensatz, dessen Projekte in
        /// VERSCHIEDENEN Modi gerechnet wurden. Kein Speicherwert — sie entsteht nur
        /// beim Beschriften eines Vergleichs.</summary>
        public const string MODUS_GEMISCHT = "GEMISCHT";

        /// <summary>true, wenn in diesem Modus das CO₂-Äquivalent ausgewiesen wird.</summary>
        public static bool IstAequivalent(string modus)
        {
            return string.Equals(modus, DbWerte.EMISSION_MODUS_CO2E, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Der Modus, den ein VERGLEICH ausweist: der gemeinsame Modus aller
        /// Varianten — und <see cref="MODUS_GEMISCHT"/>, sobald zwei Projekte
        /// verschieden gerechnet haben. Eine stillschweigende Wahl des ersten
        /// Projekts wäre hier die schlechteste Antwort: Sie beschriftete fremde
        /// Zahlen mit einem Modus, in dem sie nicht entstanden sind.
        /// </summary>
        public static string ModusAusVarianten(IEnumerable<VariantenDaten> varianten)
        {
            string gemeinsam = null;
            if (varianten != null)
                foreach (VariantenDaten v in varianten)
                {
                    if (v == null) continue;
                    string m = IstAequivalent(v.EmissionsModus)
                        ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
                    if (gemeinsam == null) gemeinsam = m;
                    else if (!string.Equals(gemeinsam, m, StringComparison.Ordinal))
                        return MODUS_GEMISCHT;
                }
            return gemeinsam ?? DbWerte.EMISSION_MODUS_CO2;
        }

        /// <summary>Kurzbenennung der Größe („CO₂-Emissionen" / „CO₂-Äquivalent
        /// (GWP₁₀₀)") — für Fließtext und Hinweiszeilen.</summary>
        public static string Groesse(string modus, bool englisch)
        {
            if (Gemischt(modus))
                return englisch ? "CO₂ / CO₂ equivalent (mode differs per variant)"
                                : "CO₂ / CO₂-Äquivalent (Modus je Variante verschieden)";
            if (IstAequivalent(modus))
                return englisch ? "CO₂ equivalent (GWP₁₀₀)" : "CO₂-Äquivalent (GWP₁₀₀)";
            return englisch ? "CO₂ emissions" : "CO₂-Emissionen";
        }

        /// <summary>Beschriftung der Kennzahl <c>em.co2</c> (Jahresmenge).</summary>
        public static string KennzahlGesamt(string modus, bool englisch)
        {
            if (Gemischt(modus))
                return englisch ? "Total CO₂ / CO₂ equivalent (mode differs per variant)"
                                : "CO₂ bzw. CO₂-Äquivalent gesamt (Modus je Variante verschieden)";
            if (IstAequivalent(modus))
                return englisch ? "Total CO₂ equivalent (GWP₁₀₀)" : "CO₂-Äquivalent gesamt (GWP₁₀₀)";
            return englisch ? "Total CO₂ emissions" : "CO₂-Emissionen gesamt";
        }

        /// <summary>Beschriftung der Kennzahl <c>em.co2_spez</c> (je kWh Wärme).</summary>
        public static string KennzahlSpezifisch(string modus, bool englisch)
        {
            if (Gemischt(modus))
                return englisch ? "Specific CO₂ / CO₂ equivalent (heat, mode differs per variant)"
                                : "CO₂ bzw. CO₂-Äquivalent spezifisch (Wärme, Modus je Variante verschieden)";
            if (IstAequivalent(modus))
                return englisch ? "Specific CO₂ equivalent (heat, GWP₁₀₀)"
                                : "CO₂-Äquivalent spezifisch (Wärme, GWP₁₀₀)";
            return englisch ? "Specific CO₂ (heat)" : "CO₂ spezifisch (Wärme)";
        }

        /// <summary>
        /// Der Modus, den eine ZEILE über mehrere Bilanzen ausweist — wie
        /// <see cref="ModusAusVarianten"/>, nur für die Emissionsbilanz.
        /// </summary>
        public static string ModusAusBilanzen(IEnumerable<EmissionsBilanz> bilanzen)
        {
            string gemeinsam = null;
            if (bilanzen != null)
                foreach (EmissionsBilanz b in bilanzen)
                {
                    if (b == null) continue;
                    string m = IstAequivalent(b.Modus)
                        ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
                    if (gemeinsam == null) gemeinsam = m;
                    else if (!string.Equals(gemeinsam, m, StringComparison.Ordinal))
                        return MODUS_GEMISCHT;
                }
            return gemeinsam ?? DbWerte.EMISSION_MODUS_CO2;
        }

        /// <summary>Zeilentitel der Emissionsbilanz („CO₂ [t/a]").</summary>
        public static string BilanzZeile(string modus)
        {
            if (Gemischt(modus)) return "CO₂ bzw. CO₂-Äquivalent (Modus je Projekt verschieden) [t/a]";
            return IstAequivalent(modus) ? "CO₂-Äquivalent (GWP₁₀₀) [t/a]" : "CO₂ [t/a]";
        }

        /// <summary>Zeilentitel der Vermeidung gegenüber der getrennten Erzeugung.</summary>
        public static string BilanzVermeidung(string modus)
        {
            if (Gemischt(modus))
                return "CO₂- bzw. CO₂-Äquivalent-Vermeidung vs. getrennt " +
                       "(Modus je Projekt verschieden) [t/a]";
            return IstAequivalent(modus)
                ? "CO₂-Äquivalent-Vermeidung vs. getrennt (GWP₁₀₀) [t/a]"
                : "CO₂-Vermeidung vs. getrennt [t/a]";
        }

        private static bool Gemischt(string modus)
        {
            return string.Equals(modus, MODUS_GEMISCHT, StringComparison.Ordinal);
        }
    }
}
