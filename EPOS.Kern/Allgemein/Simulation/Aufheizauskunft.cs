using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Aufheizbemessung eines Gebäudes ohne Jahreslauf</b> (Entwurf KP3, Welle D2; Grundsatz 3,
    /// Festlegungen 3 und 16, B14) — die Auskunft hinter der Herleitungszeile der Projekteinstellung
    /// „Aufheizoptimierung": Zustand, Variante, t_auf,max, T_a,B und P_auf samt Quelle, dieselben Zahlen wie
    /// der Lauf (<see cref="SimulationWaermebedarf.AufheizbemessungEinesGebaeudes"/>).
    ///
    /// <para><b>Skalierung</b> (Festlegung 16, B11): P_auf gilt dem Katalogbau; der Lauf multipliziert es wie
    /// die Spitzen mit dem Faktor nach E8. <see cref="LeistungUnskaliertKw"/> ist der Wert am Katalogbau (mit
    /// Quelle Grenze die Eingabe <c>Heizleistung_Max</c>), <see cref="Skalierungsfaktor"/> der Faktor der
    /// Flächenangabe, <see cref="LeistungKw"/> das Produkt — bitgleich zur Ergebniszeile. Mit Verbrauchsangabe
    /// entsteht der Faktor erst im Jahreslauf: dann ist er <c>null</c> (<see cref="FaktorErstImLauf"/>), und
    /// <see cref="LeistungKw"/> fehlt. Ein Gebäude mit Zone rechnet seine echte Hülle (Faktor 1).</para>
    /// </summary>
    internal sealed record Aufheizauskunft
    {
        /// <summary><c>Tab_Gebaeude.ID</c> der Projektkopie.</summary>
        internal int ID_Gebaeude { get; init; }

        /// <summary>Der Name der Projektkopie.</summary>
        internal string Gebaeudename { get; init; } = "";

        /// <summary>Rechnet das Gebäude auf dem Tagesbilanz-Weg? Dann gibt es keine Aufheizoptimierung.</summary>
        internal bool Tagesbilanz { get; init; }

        /// <summary>Der benannte Grund, warum keine Bemessung entstand (Prüfung des Eingangsbauers); sonst <c>null</c>.</summary>
        internal string Befund { get; init; }

        /// <summary>Der Zustand (<c>DbWerte.AUFHEIZ_ZUSTAND_*</c>): BEMESSEN, UNERREICHBAR oder GEKOPPELT; <c>null</c> ohne Bemessung.</summary>
        internal string Zustand { get; init; }

        /// <summary>Die Variante (<see cref="DbWerte.AUFHEIZ_BEMESSUNGEN"/>); <c>null</c> bei GEKOPPELT.</summary>
        internal string Bemessung { get; init; }

        /// <summary>t_auf,max [h]; <c>null</c> bei UNERREICHBAR und GEKOPPELT.</summary>
        internal int? AufheizzeitMaxH { get; init; }

        /// <summary>T_a,B [°C]: kälteste Stunde mit Heizsollwert, bei (b) abzüglich ΔT_K.</summary>
        internal double? AussenC { get; init; }

        /// <summary>P_auf am Katalogbau [kW] — mit Quelle Grenze die Eingabe.</summary>
        internal double? LeistungUnskaliertKw { get; init; }

        /// <summary>Der Faktor nach E8; 1 für ein Gebäude mit Zone; <c>null</c>, wenn erst der Lauf ihn kennt.</summary>
        internal double? Skalierungsfaktor { get; init; }

        /// <summary>P_auf, skaliert wie im Lauf [kW]; <c>null</c> ohne Faktor oder ohne Bemessung.</summary>
        internal double? LeistungKw { get; init; }

        /// <summary>Die Quelle von P_auf: GRENZE, ZIEL oder — im Mehrzonenweg — GEMISCHT.</summary>
        internal string Quelle { get; init; }

        /// <summary>Verbrauchsangabe: den Faktor bestimmt erst der Jahreslauf (Rückrechnung, E8).</summary>
        internal bool FaktorErstImLauf => Zustand != null && !Skalierungsfaktor.HasValue;
    }
}
