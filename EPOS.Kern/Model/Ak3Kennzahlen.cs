namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennzahlen des geschlossenen Kreises</b> (Anlagenkopplung AK3; Entwurf AK3 Festlegung 22) eines Projektlaufs
    /// — gelesen aus den Spalten <c>Ak3_*</c> der Projektzeile des letzten Laufs (<see cref="Ak3Schema"/>). Für den
    /// Bedarfsdialog und die KI-Sicht; der Bericht liest sie aus dem <see cref="ErgebnisEnergiebedarfModel"/>.
    /// </summary>
    internal sealed record Ak3Kennzahlen
    {
        /// <summary><c>Ak3_Durchlaeufe_Mittel</c> [–]: Mittel der begrenzten Durchläufe je Stunde.</summary>
        internal double DurchlaeufeMittel { get; init; }

        /// <summary><c>Ak3_Durchlaeufe_Max</c> [–]: die größte Zahl der Durchläufe einer Stunde.</summary>
        internal int? DurchlaeufeMax { get; init; }

        /// <summary><c>Ak3_Fallwechsel</c> [–]: Wechsel der Stützstelle und des Betriebsfalls je Zone.</summary>
        internal int? Fallwechsel { get; init; }

        /// <summary><c>Ak3_Schranke_Stunden</c> [h]: Stunden, in denen die Schranke des Angebots eine Zone begrenzte.</summary>
        internal int? SchrankeStundenH { get; init; }

        /// <summary><c>Ak3_Speicher_Leer_Stunden</c> [h]: Stunden mit Heizungspuffer am Kreis und nichts entnehmbar.</summary>
        internal int? SpeicherLeerStundenH { get; init; }

        /// <summary><c>Ak3_Restbedarf_Stunden</c> [h]: Stunden mit Restbedarf der Kaskade.</summary>
        internal int? RestbedarfStundenH { get; init; }

        /// <summary>Die Kennzahlen aus der Projektzeile eines Ergebnisses; <c>null</c>, wenn der Lauf den Kreis nicht rechnete.</summary>
        internal static Ak3Kennzahlen Aus(ErgebnisEnergiebedarfModel e)
            => e?.Ak3DurchlaeufeMittel is double mittel
                ? new Ak3Kennzahlen
                {
                    DurchlaeufeMittel = mittel,
                    DurchlaeufeMax = e.Ak3DurchlaeufeMax,
                    Fallwechsel = e.Ak3Fallwechsel,
                    SchrankeStundenH = e.Ak3SchrankeStundenH,
                    SpeicherLeerStundenH = e.Ak3SpeicherLeerStundenH,
                    RestbedarfStundenH = e.Ak3RestbedarfStundenH,
                }
                : null;
    }
}
