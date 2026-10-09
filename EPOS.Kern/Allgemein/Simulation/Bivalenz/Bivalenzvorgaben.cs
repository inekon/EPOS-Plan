#nullable enable

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Woher ein Vorgabesatz der Wärmepumpe stammt (Umsetzungskonzept U‑2).</summary>
    internal enum Kaeltemittelherkunft
    {
        /// <summary>Allgemeine (unterkritische) Vorgabe — Kältemittel leer, unbekannt oder <c>SONSTIGES</c>.</summary>
        Allgemein = 0,

        /// <summary>Vorgabe nach der Kältemittelklasse des gewählten Codes.</summary>
        Kaeltemittelklasse = 1,
    }

    /// <summary>
    /// Ein Satz Gerätevorgaben der Wärmepumpe für leere Felder (Fachkonzept Übergabegrenze 6.3,
    /// Umsetzungskonzept U‑2). Ein leerer Wert (<c>null</c>) heißt „ohne Vorgabe": Der Höchstvorlauf
    /// kommt dann aus dem Gerät (<c>Vorlauf_Max</c>, sonst <c>Vorlauf</c>), die Rücklaufgrenze wird
    /// unterkritisch abgeleitet (θ_WP,max − σ_min).
    /// </summary>
    /// <param name="Code">Kältemittelcode der Klappliste; leer für die allgemeine Vorgabe.</param>
    /// <param name="HoechstvorlaufC">Höchstvorlauf θ_WP,max [°C]; null = aus dem Gerät.</param>
    /// <param name="SpreizungAuslegungK">Spreizung am Verflüssiger in der Auslegung σ_A [K].</param>
    /// <param name="SpreizungMaxK">Höchstspreizung σ_max [K].</param>
    /// <param name="SpreizungMinK">Mindestspreizung σ_min [K].</param>
    /// <param name="MindestvolumenstromAnteil">Mindestvolumenstrom als Anteil des Nennvolumenstroms [–].</param>
    /// <param name="RuecklaufGrenzeC">Rücklaufgrenze θ_R,grenz [°C]; null = abgeleitet θ_WP,max − σ_min.</param>
    /// <param name="BezugsruecklaufC">Bezugsrücklauf der Abwertung [°C]; null = ohne Abwertung.</param>
    /// <param name="AbwertungProzentJeK">Abwertung der Leistung je K über dem Bezugsrücklauf [%/K]; null = ohne.</param>
    /// <param name="Herkunft">Allgemein oder nach Kältemittelklasse.</param>
    internal sealed record Kaeltemittelvorgabe(
        string Code,
        double? HoechstvorlaufC,
        double SpreizungAuslegungK,
        double SpreizungMaxK,
        double SpreizungMinK,
        double MindestvolumenstromAnteil,
        double? RuecklaufGrenzeC,
        double? BezugsruecklaufC,
        double? AbwertungProzentJeK,
        Kaeltemittelherkunft Herkunft);

    /// <summary>
    /// <b>Die Vorgabewerte der Bivalenz und Übergabe</b> (Fachkonzept Übergabegrenze, Tafel 6.3;
    /// UB‑Q6 a; Umsetzungskonzept 3.1 und U‑2) — gerundete Konstanten mit Quellkürzel nach
    /// Fachkonzept Abschnitt 11, keine Normtafel. Die Exponenten und Auslegungspaare der Übergabe
    /// bleiben die AK1-Vorgaben in <see cref="Waermeuebergabe"/> und stehen hier nicht doppelt.
    ///
    /// <para><b>Klappliste der Kältemittel</b> (U‑2): Die Werteliste führt der Kern, das Schema
    /// trägt den Code als freien Text ohne CHECK-Liste. Ein Code ohne eigene Zeile der Tafel 6.3,
    /// ein unbekannter Code, ein leerer Code und <c>SONSTIGES</c> erhalten die allgemeine
    /// (unterkritische) Vorgabe.</para>
    /// </summary>
    internal static class Bivalenzvorgaben
    {
        // ---------------------------------------------------------------------
        //  Spreizungen und Volumenstrom der Wärmepumpe
        // ---------------------------------------------------------------------

        /// <summary>Spreizung am Verflüssiger in der Auslegung σ_A [K]. [Q3], [Q8], [Q11]</summary>
        internal const double SPREIZUNG_AUSLEGUNG_K = 5.0;

        /// <summary>Höchstspreizung σ_max allgemein [K]; ein Herstellerwert hat Vorrang. [Q8]</summary>
        internal const double SPREIZUNG_MAX_K = 10.0;

        /// <summary>Höchstspreizung σ_max bei R744 [K] — dort begrenzt der Rücklauf, nicht die Spreizung. [Q42], [Q44]</summary>
        internal const double SPREIZUNG_MAX_R744_K = 30.0;

        /// <summary>Mindestspreizung σ_min [K]. [Q8] (Abl.)</summary>
        internal const double SPREIZUNG_MIN_K = 3.0;

        /// <summary>Mindestvolumenstrom als Anteil des Nennvolumenstroms [–]. (Abl.), [Q6], [Q8], [Q50], [Q51]</summary>
        internal const double MINDESTVOLUMENSTROM_ANTEIL = 0.60;

        // ---------------------------------------------------------------------
        //  Höchstvorlauf je Kältemittelklasse
        // ---------------------------------------------------------------------

        /// <summary>Höchstvorlauf R410A und R32 [°C]. [Q16]; R32-Band (sek.)</summary>
        internal const double HOECHSTVORLAUF_R410A_R32_C = 55.0;

        /// <summary>Höchstvorlauf R290 [°C]. [Q18]</summary>
        internal const double HOECHSTVORLAUF_R290_C = 70.0;

        /// <summary>Höchstvorlauf R744 [°C]. [Q19], [Q20]</summary>
        internal const double HOECHSTVORLAUF_R744_C = 80.0;

        /// <summary>Höchstvorlauf R1234ze(E) [°C]. [Q21]</summary>
        internal const double HOECHSTVORLAUF_R1234ZE_C = 80.0;

        // ---------------------------------------------------------------------
        //  Rücklaufgrenzen
        // ---------------------------------------------------------------------

        /// <summary>Rücklaufgrenze R744 mit Abwertung [°C]. [Q19], [Q42], [Q45], [Q46]</summary>
        internal const double RUECKLAUF_GRENZE_R744_C = 40.0;

        /// <summary>Bezugsrücklauf der R744-Abwertung [°C]. [Q14], [Q42]</summary>
        internal const double BEZUGSRUECKLAUF_R744_C = 30.0;

        /// <summary>Abwertung der R744-Leistung je K über dem Bezugsrücklauf [%/K]. [Q42], [Q43] (sek.)</summary>
        internal const double ABWERTUNG_R744_PROZENT_JE_K = 2.5;

        /// <summary>Rücklaufgrenze am BHKW (Option, Abschaltgrenze des Motorkühlkreises) [°C]. [Q63], [Q64]</summary>
        internal const double RUECKLAUF_GRENZE_BHKW_C = 70.0;

        // ---------------------------------------------------------------------
        //  Hybrid-Mindestanteil
        // ---------------------------------------------------------------------

        /// <summary>Hybrid-Mindestanteil der Wärmepumpe an der Kesselleistung, parallel [–]. [Q27], [Q32]</summary>
        internal const double HYBRID_MINDESTANTEIL_PARALLEL = 0.30;

        /// <summary>Hybrid-Mindestanteil der Wärmepumpe an der Kesselleistung, alternativ [–]. [Q27], [Q32]</summary>
        internal const double HYBRID_MINDESTANTEIL_ALTERNATIV = 0.40;

        /// <summary>Außentemperatur, bei der der Hybrid-Anteil verglichen wird [°C]. [Q27], [Q32]</summary>
        internal const double HYBRID_VERGLEICH_AUSSEN_C = -7.0;

        // ---------------------------------------------------------------------
        //  Klappliste der Kältemittel (U‑2)
        // ---------------------------------------------------------------------

        internal const string KM_R410A = "R410A";
        internal const string KM_R32 = "R32";
        internal const string KM_R290 = "R290";
        internal const string KM_R744 = "R744";
        internal const string KM_R134A = "R134a";
        internal const string KM_R1234ZE = "R1234ze(E)";
        internal const string KM_R407C = "R407C";
        internal const string KM_R454C = "R454C";
        internal const string KM_R455A = "R455A";
        internal const string KM_R1233ZD = "R1233zd(E)";
        internal const string KM_SONSTIGES = "SONSTIGES";

        /// <summary>Die Codes der Klappliste in Anzeigereihenfolge (U‑2).</summary>
        internal static readonly IReadOnlyList<string> Kaeltemittelcodes = new[]
        {
            KM_R410A, KM_R32, KM_R290, KM_R744, KM_R134A, KM_R1234ZE,
            KM_R407C, KM_R454C, KM_R455A, KM_R1233ZD, KM_SONSTIGES,
        };

        /// <summary>Die allgemeine (unterkritische) Vorgabe — Höchstvorlauf aus dem Gerät, Rücklaufgrenze abgeleitet.</summary>
        internal static readonly Kaeltemittelvorgabe Allgemein = new Kaeltemittelvorgabe(
            string.Empty, null, SPREIZUNG_AUSLEGUNG_K, SPREIZUNG_MAX_K, SPREIZUNG_MIN_K,
            MINDESTVOLUMENSTROM_ANTEIL, null, null, null, Kaeltemittelherkunft.Allgemein);

        /// <summary>Die Codes mit eigener Zeile der Tafel 6.3 und ihre Vorgaben.</summary>
        private static readonly Dictionary<string, Kaeltemittelvorgabe> Klassen =
            new Dictionary<string, Kaeltemittelvorgabe>(StringComparer.OrdinalIgnoreCase)
            {
                [KM_R410A] = Unterkritisch(KM_R410A, HOECHSTVORLAUF_R410A_R32_C),
                [KM_R32] = Unterkritisch(KM_R32, HOECHSTVORLAUF_R410A_R32_C),
                [KM_R290] = Unterkritisch(KM_R290, HOECHSTVORLAUF_R290_C),
                [KM_R1234ZE] = Unterkritisch(KM_R1234ZE, HOECHSTVORLAUF_R1234ZE_C),
                [KM_R744] = new Kaeltemittelvorgabe(KM_R744, HOECHSTVORLAUF_R744_C, SPREIZUNG_AUSLEGUNG_K,
                    SPREIZUNG_MAX_R744_K, SPREIZUNG_MIN_K, MINDESTVOLUMENSTROM_ANTEIL, RUECKLAUF_GRENZE_R744_C,
                    BEZUGSRUECKLAUF_R744_C, ABWERTUNG_R744_PROZENT_JE_K, Kaeltemittelherkunft.Kaeltemittelklasse),
            };

        private static Kaeltemittelvorgabe Unterkritisch(string code, double hoechstvorlaufC)
            => new Kaeltemittelvorgabe(code, hoechstvorlaufC, SPREIZUNG_AUSLEGUNG_K, SPREIZUNG_MAX_K,
                SPREIZUNG_MIN_K, MINDESTVOLUMENSTROM_ANTEIL, null, null, null, Kaeltemittelherkunft.Kaeltemittelklasse);

        /// <summary>Ist der Code ein Eintrag der Klappliste (Groß-/Kleinschreibung gleichgültig)?</summary>
        internal static bool IstBekannt(string? kaeltemittel)
        {
            if (string.IsNullOrWhiteSpace(kaeltemittel)) return false;
            string c = kaeltemittel.Trim();
            foreach (string k in Kaeltemittelcodes)
                if (string.Equals(k, c, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Der Vorgabesatz zum Kältemittelcode: die Klasse der Tafel 6.3, sonst — leer, unbekannt,
        /// <c>SONSTIGES</c> oder ein Code der Klappliste ohne eigene Zeile — die allgemeine Vorgabe.
        /// </summary>
        internal static Kaeltemittelvorgabe Vorgabe(string? kaeltemittel)
        {
            if (string.IsNullOrWhiteSpace(kaeltemittel)) return Allgemein;
            return Klassen.TryGetValue(kaeltemittel.Trim(), out Kaeltemittelvorgabe? v) ? v : Allgemein;
        }
    }
}
