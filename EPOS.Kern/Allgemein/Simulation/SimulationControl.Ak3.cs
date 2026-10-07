using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Vorbereitung des AK3-Wegs an der Kaskade</b> (AK3-W2; Entwurf AK3, Festlegung 13 und 2.1
    /// Schritt 4). Beide Schalter sind im Kern gesetzt und noch nicht wählbar: Ohne sie rechnet
    /// <see cref="Kaskade_Zweikanalig"/> Anweisung für Anweisung wie zuvor.
    /// </summary>
    public partial class SimulationControl
    {
        /// <summary>
        /// <b>Vektorstufen als Schleifenmitglieder</b> (Festlegung 13): Heizerzeuger ohne Speicher
        /// (Heizkessel, Solarthermie, BHKW), die in der Kaskade VOR der Speicherstufe stehen, rechnen
        /// im AK3-Weg als Mitglieder der Stundenschleife (Muster <see cref="ZwischenstufenAufnehmen"/>);
        /// Vektorstufen danach rechnen wie heute über den Rest. Ein Projekt ohne Speicherstufe rechnet
        /// alle Heizerzeuger als Schleifenmitglieder ohne Speicher. Die Abweichung zum Vektorweg ist
        /// benannt; Vorgabe <c>false</c> = der heutige Weg.
        /// </summary>
        internal bool Ak3VektorstufenInSchleife { get; set; }

        /// <summary>
        /// Bedarfsnaht der Kaskadenstunde (<see cref="Kaskadenschleife.Stundenbedarf"/>), an die
        /// Speicherstufe durchgereicht. <c>null</c> = Vorgabe <see cref="VektorStundenbedarf"/>.
        /// </summary>
        internal IStundenbedarf Stundenbedarf { get; set; }

        /// <summary>
        /// Die Kaskadenpositionen, deren Vektorstufe im AK3-Weg Schleifenmitglied wird
        /// (Festlegung 13) — rein, ohne Datenbank: jede Position mit Heizkessel, Solarthermie oder
        /// BHKW, die noch kein Mitglied ist und VOR dem ersten Mitglied steht; ohne Mitglied jede
        /// solche Position. Die Wärmepumpe ist immer Mitglied und taucht hier nie auf.
        /// </summary>
        /// <param name="tool">Kaskadenplätze (<c>Tool_1</c> bis <c>Tool_4</c>, Erzeugercodes nach <see cref="DbWerte"/>).</param>
        /// <param name="mitglied">je Platz: schon Mitglied der Speicherstufe.</param>
        internal static List<int> Ak3Aufnahmepositionen(IReadOnlyList<string> tool, IReadOnlyList<bool> mitglied)
        {
            var positionen = new List<int>();
            if (tool == null || mitglied == null) return positionen;

            int n = tool.Count < 4 ? tool.Count : 4;
            if (mitglied.Count < n) n = mitglied.Count;

            int erste = -1;
            for (int i = 0; i < n; i++)
                if (mitglied[i]) { erste = i; break; }

            int ende = erste < 0 ? n : erste;
            for (int i = 0; i < ende; i++)
            {
                string t = tool[i];
                if (t == DbWerte.ERZEUGER_HEIZKESSEL || t == DbWerte.ERZEUGER_SOLARTHERMIE || t == DbWerte.ERZEUGER_BHKW)
                    positionen.Add(i);
            }
            return positionen;
        }

        /// <summary>
        /// Nimmt im AK3-Weg die Vektorstufen nach <see cref="Ak3Aufnahmepositionen"/> in die
        /// Stundenschleife auf. Gerufen in <see cref="Kaskade_Zweikanalig"/> NACH
        /// <see cref="ZwischenstufenAufnehmen"/> und nur bei <see cref="Ak3VektorstufenInSchleife"/>.
        /// </summary>
        private void Ak3VektorstufenAufnehmen()
        {
            if (tool == null) return;

            var mitglied = new bool[tool.Length];
            for (int i = 0; i < tool.Length; i++) mitglied[i] = IstSchleifenstufe(i);

            foreach (int i in Ak3Aufnahmepositionen(tool, mitglied))
            {
                if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL) _kesselInSchleife = true;
                else if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE) _solarInSchleife = true;
                else if (tool[i] == DbWerte.ERZEUGER_BHKW) _bhkwInSchleife = true;
            }
        }

        /// <summary>Ist der Heizkessel Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool KesselInSchleife => _kesselInSchleife;

        /// <summary>Ist die Solarthermie Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool SolarInSchleife => _solarInSchleife;

        /// <summary>Ist das BHKW Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool BhkwInSchleife => _bhkwInSchleife;
    }
}
