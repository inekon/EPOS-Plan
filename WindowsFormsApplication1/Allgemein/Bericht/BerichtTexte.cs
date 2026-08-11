using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Berichtssprache = UI-Sprache (Konzept Eckpunkt 10; Program.nLanguage: 0=de, 1=en).
    ///
    /// Übersetzt bekannte Berichtstexte per Wörterbuch: T(text) liefert bei
    /// englischer UI die Übersetzung, sonst den Eingabetext unverändert — unbekannte
    /// (dynamische) Texte laufen unverändert durch. Der WordKontext wendet T() auf
    /// Überschriften, Beschriftungen und fette Tabellenkopf-Zellen an; Kennzahl-
    /// Beschriftungen sind über den KennzahlenKatalog (LabelDe/LabelEn) zweisprachig.
    /// Vollständige Übersetzung der Fließtexte bleibt Übersetzungsarbeit (LIESMICH).
    /// </summary>
    public static class BerichtTexte
    {
        public static bool Englisch
        {
            get { try { return Program.nLanguage == 1; } catch { return false; } }
        }

        /// <summary>Kultur der Berichtssprache (Zahlen-/Datumsformate).</summary>
        public static CultureInfo Kultur
        {
            get { return CultureInfo.GetCultureInfo(Englisch ? "en-US" : "de-DE"); }
        }

        public static string T(string de)
        {
            if (!Englisch || de == null) return de;
            string en;
            return _en.TryGetValue(de, out en) ? en : de;
        }

        private static readonly Dictionary<string, string> _en = new Dictionary<string, string>
        {
            // Kapitel
            { "Inhalt", "Contents" },
            { "Projektbeschreibung", "Project description" },
            { "Gebäude", "Buildings" },
            { "Energiebedarf (Simulationsergebnis Stamm)", "Energy demand (base simulation result)" },
            { "Komponenten & Varianten", "Components & variants" },
            { "Komponentenübersicht", "Component overview" },
            { "Abweichungen der Varianten gegenüber dem Stamm", "Deviations of the variants from the base" },
            { "Berechnungsergebnisse je Variante", "Results per variant" },
            { "Variantenvergleich", "Variant comparison" },
            { "Abweichung zum Stamm (Schlüsselkennzahlen, in %)", "Deviation from base (key figures, %)" },
            { "Kennzahlen im Vergleich (Diagramme)", "Key figures compared (charts)" },
            { "Deckungsdiagramme", "Coverage charts" },
            { "Erzeuger — Einzelauflistung je Projekt", "Generators — itemised per project" },
            { "Brennstoffmengen", "Fuel quantities" },
            { "Anhang", "Appendix" },
            { "Simulationsstände", "Simulation timestamps" },
            { "Datengrundlage und Methodik", "Data basis and methodology" },
            { "Hinweise dieses Berichtslaufs", "Notes of this report run" },
            { "Variantenvergleich — Energie- und Wärmeversorgung", "Variant comparison — energy and heat supply" },

            // Deckblatt/Eigenschaften
            { "Projekt", "Project" },
            { "Kunde", "Customer" },
            { "Bearbeiter", "Editor" },
            { "Verglichene Varianten", "Compared variants" },
            { "Berichtsdatum", "Report date" },
            { "EPOS-Plan-Version", "EPOS-Plan version" },
            { "Projektname", "Project name" },
            { "Beschreibung", "Description" },
            { "Klimaregion", "Climate region" },
            { "Angelegt", "Created" },
            { "Zuletzt geändert", "Last modified" },
            { "Simulationsstand", "Simulation timestamp" },

            // Tabellenköpfe
            { "Kennzahl (Einheit)", "Key figure (unit)" },
            { "Kennzahl", "Key figure" },
            { "Stamm", "Base" },
            { "Variante", "Variant" },
            { "Merkmal", "Characteristic" },
            { "Gewerk", "Trade" },
            { "Rolle", "Role" },
            { "Simulation vom", "Simulated on" },
            { "Hinweis", "Note" },
            { "Erzeuger", "Generator" },
            { "Wärme [MWh/a]", "Heat [MWh/a]" },
            { "Strom [MWh/a]", "Electricity [MWh/a]" },
            { "Energieträger", "Energy carrier" },
            { "Verbrauch [MWh/a]", "Consumption [MWh/a]" },
            { "Bezeichner", "Identifier" },
            { "Menge", "Quantity" },
            { "Δ (Var. − Stamm)", "Δ (var. − base)" },
            { "(Stammprojekt)", "(base project)" },

            // Kennzahlgruppen
            { "Energiebilanz", "Energy balance" },
            { "Effizienz", "Efficiency" },
            { "Emissionen", "Emissions" },
            { "Kosten", "Costs" },

            // Wirtschaftlichkeit (Phase 6)
            { "Wirtschaftlichkeit", "Economic viability" },
            { "Kapitalwertmethode (DIN EN 17463)", "Net present value method (DIN EN 17463)" },
            { "Kennzahlen im Szenario „Erwartet“", "Key figures, scenario \"Expected\"" },
            { "Szenarien Worst / Erwartet / Best", "Scenarios worst / expected / best" },
            { "Szenario", "Scenario" },
            { "Investition I₀ [€]", "Investment I₀ [€]" },
            { "Betriebskosten [€/a]", "Operating cost [€/a]" },
            { "Energiekosten [€/a]", "Energy cost [€/a]" },
            { "Einspeiseerlös [€/a]", "Feed-in revenue [€/a]" },
            { "Restwert (Barwert) [€]", "Residual value (present value) [€]" },
            { "Nettobarwert über T [€]", "Net present value over T [€]" },
            { "Kapitalwert vs. Stamm [€]", "NPV vs. base [€]" },
            { "Annuität des KW [€/a]", "Annuity of NPV [€/a]" },
            { "Amortisation [a]", "Payback [a]" },
            { "Wärmegestehungskosten [€/kWh]", "Levelised cost of heat [€/kWh]" },
            { "KW Worst [€]", "NPV worst [€]" },
            { "KW Erwartet [€]", "NPV expected [€]" },
            { "KW Best [€]", "NPV best [€]" },
            { "Referenz: Stammprojekt · Restwert linear", "Reference: base project · linear residual value" },
            { "Rechenstand", "Calculated" },
            { "Noch keine Wirtschaftlichkeitsberechnung gespeichert — im Bereich Berichte & Kosten → Wirtschaftlichkeit berechnen.",
              "No economic calculation stored yet — run it under Reports & Costs → Economic viability." },
        };
    }
}
