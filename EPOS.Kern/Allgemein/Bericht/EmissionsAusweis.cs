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

        // =====================================================================
        // ETAPPE B7 — die EINE Emissionsspalte der Kostenseite (Konzept § 2.5)
        // =====================================================================

        /// <summary>
        /// Der Spaltenkopf der Energieträgertabelle: <c>CO₂ [g/kWh]</c> im Modus
        /// <c>CO2</c>, <c>CO₂-Äquivalent [g/kWh]</c> im Modus <c>CO2E</c>.
        ///
        /// <para>Der Kopf folgt dem Modus, in dem DIESES Projekt rechnet
        /// (<c>Tab_Projekt.Emission_Berechnungsmodus</c>) — nicht der globalen Vorgabe.
        /// Ein Projekt trägt seine Rechenmethode dauerhaft in sich.</para>
        /// </summary>
        public static string SpaltenkopfEmission(string modus)
        {
            return IstAequivalent(modus)
                ? MyResource.Resource.BK_KOSTEN_SP_CO2E
                : MyResource.Resource.BK_KOSTEN_SP_CO2;
        }

        /// <summary>
        /// Die HERLEITUNG des angezeigten Emissionswertes als Klartext für den
        /// Kurztext der Zelle — die drei Fälle der Entscheidung E-1 (Konzept § 2.5).
        ///
        /// <para><b>Ein stiller Rückfall findet nicht statt.</b> Im Modus CO2E ohne
        /// Artenkatalog steht in der Spalte der reine CO₂-Faktor; dann sagt die
        /// Herleitung genau das, statt den Äquivalentkopf unwidersprochen stehen zu
        /// lassen. Wer diesen Satz entfernt, macht aus einer benannten Einschränkung
        /// eine falsche Beschriftung.</para>
        /// </summary>
        public static string HerleitungEmission(EmissionsFaktorSatz satz, string modus,
                                                System.Globalization.CultureInfo kultur)
        {
            if (satz == null) return "";
            if (kultur == null) kultur = System.Globalization.CultureInfo.CurrentCulture;
            string ebene = string.IsNullOrEmpty(satz.Co2Ebene) ? "-" : satz.Co2Ebene;

            // Modus CO2 — dort gibt es nichts herzuleiten, nur die Quelle zu nennen.
            if (!IstAequivalent(modus))
                return string.Format(kultur, MyResource.Resource.BK_KOSTEN_EMISSION_CO2, ebene);

            // Fall 3 (F3): der hinterlegte Wert IST bereits ein Äquivalent.
            if (satz.Co2IstAequivalent)
                return string.Format(kultur, MyResource.Resource.BK_KOSTEN_EMISSION_IST_CO2E, ebene);

            // Kein Artenkatalog: ausgewiesen wird der reine CO₂-Faktor — benannt.
            if (satz.ArtenkatalogFehlt)
                return string.Format(kultur, MyResource.Resource.BK_KOSTEN_EMISSION_OHNE_KATALOG, ebene);

            // Fall 1 (Regelfall): die gewichtete Summe, Summand für Summand.
            string formel = SummenFormel(satz, kultur);
            if (formel.Length > 0)
                return string.Format(kultur, MyResource.Resource.BK_KOSTEN_EMISSION_REGEL, formel, ebene);

            // Fall 2: außer CO₂ ist keine weitere Art hinterlegt.
            return string.Format(kultur, MyResource.Resource.BK_KOSTEN_EMISSION_NUR_CO2, ebene);
        }

        /// <summary>
        /// „CO₂ 240,0 + CH₄ 0,50 × 28 + N₂O 0,010 × 265 = 256,7 g/kWh" — leer, wenn
        /// außer CO₂ keine Art beiträgt (dann ist die Summe der CO₂-Faktor und die
        /// Formel sagte nichts, was die Zahl nicht schon sagt).
        /// </summary>
        private static string SummenFormel(EmissionsFaktorSatz satz,
                                           System.Globalization.CultureInfo kultur)
        {
            if (satz.Zeilen == null || satz.Zeilen.Count == 0) return "";

            var teile = new List<string>();
            int beitragende = 0;
            foreach (EmissionsZeile z in satz.Zeilen)
            {
                if (z == null || z.Art == null || !z.Wert.HasValue || z.BeitragGKwh == 0) continue;
                beitragende++;
                string t = z.Art.Kuerzel + " " + z.Wert.Value.ToString("N3", kultur);
                if (z.Art.Co2Aequivalent != 1.0)
                    t += " × " + z.Art.Co2Aequivalent.ToString("N0", kultur);
                teile.Add(t);
            }
            if (beitragende < 2) return "";      // eine einzige Art ist keine Summe

            return string.Join(" + ", teile.ToArray()) + " = " +
                   (satz.Co2eGKwh ?? 0).ToString("N1", kultur) + " g/kWh";
        }
    }
}
