using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DAS TEMPERATURNIVEAU DES PROZESSKANALS - Welle M3a, Entscheidungsvorlage Modellgrenzen
    // PW1 Stufe 1 (Schema: ProzesswaermeTemperaturSchema).
    //
    // WAS. Je Stunde des Laufs die HÖCHSTE geforderte Vorlauftemperatur der Prozesse, die in
    // dieser Stunde Wärme verlangen und ein Temperaturpaar tragen, und ihr Rücklauf,
    // mengengewichtet über dieselben Prozesse:
    //
    //   T_VL(t) = max { T_VL,p : q_p(t) > 0, Paar gepflegt }
    //   T_RL(t) = Σ q_p(t) · T_RL,p / Σ q_p(t)      (über dieselben p)
    //
    // In Stunden ohne einen solchen Prozess steht NaN - dort gilt der Bestand.
    //
    // WER LIEST. Die Wärmepumpe (Kennlinie für den Prozessanteil am Prozessvorlauf), der
    // Heizkessel (Ausschluss bei zu niedrigem Vorlauf, Prozessrücklauf in der
    // Brennwertkennlinie) und die Kaskadenschleife (Entnahme aus dem Puffer nur ab der Zone, die
    // den Prozessvorlauf hält). Ohne ein einziges gepflegtes Paar entsteht KEIN Objekt
    // (SimulationWaermebedarf.ProzessTemperatur bleibt null), und jeder Leser rechnet Zeichen für
    // Zeichen wie zuvor - die Referenzbasis bleibt unberührt.
    // ====================================================================================

    /// <summary>
    /// Das Temperaturniveau des Prozesskanals je Stunde (PW1 Stufe 1) — höchster geforderter
    /// Vorlauf und mengengewichteter Rücklauf der in der Stunde aktiven Prozesse mit
    /// Temperaturpaar. Regeln und Leser stehen im Kopf der Datei.
    /// </summary>
    public sealed class Prozesstemperatur
    {
        /// <summary>Die Stunden des Jahres.</summary>
        public const int STUNDEN = 8760;

        /// <summary>Höchster geforderter Vorlauf je Stunde [°C]; NaN = kein Prozess mit Paar aktiv.</summary>
        public readonly double[] VorlaufC = new double[STUNDEN];

        /// <summary>Mengengewichteter Rücklauf je Stunde [°C]; NaN = kein Prozess mit Paar aktiv.</summary>
        public readonly double[] RuecklaufC = new double[STUNDEN];

        private readonly double[] _ruecklaufSumme = new double[STUNDEN];
        private readonly double[] _gewicht = new double[STUNDEN];
        private bool _abgeschlossen;

        /// <summary>Zahl der aufgenommenen Profile mit Temperaturpaar.</summary>
        public int Profile { get; private set; }

        /// <summary>Höchster Vorlauf aller aufgenommenen Profile [°C]; NaN ohne Profil.</summary>
        public double VorlaufMax { get; private set; } = double.NaN;

        /// <summary>Zahl der Stunden mit gefordertem Vorlauf (nach <see cref="Abschliessen"/>).</summary>
        public int Stunden { get; private set; }

        /// <summary>Ein leeres Niveau: jede Stunde NaN.</summary>
        public Prozesstemperatur()
        {
            for (int h = 0; h < STUNDEN; h++)
            {
                VorlaufC[h] = double.NaN;
                RuecklaufC[h] = double.NaN;
            }
        }

        /// <summary>
        /// Nimmt EIN Profil auf: <paramref name="stundenwerte"/> ist seine Jahresreihe [kWh]; jede
        /// Stunde mit Bedarf hebt den Vorlauf auf mindestens <paramref name="vorlauf"/> und trägt ihren
        /// Rücklauf mit der Menge als Gewicht bei.
        /// </summary>
        public void Aufnehmen(double vorlauf, double ruecklauf, double[] stundenwerte)
        {
            if (stundenwerte == null) throw new ArgumentNullException(nameof(stundenwerte));
            if (_abgeschlossen) throw new InvalidOperationException("Das Temperaturniveau ist abgeschlossen.");

            int n = Math.Min(STUNDEN, stundenwerte.Length);
            for (int h = 0; h < n; h++)
            {
                double q = stundenwerte[h];
                if (!(q > 0)) continue;
                if (double.IsNaN(VorlaufC[h]) || vorlauf > VorlaufC[h]) VorlaufC[h] = vorlauf;
                _ruecklaufSumme[h] += q * ruecklauf;
                _gewicht[h] += q;
            }

            Profile++;
            if (double.IsNaN(VorlaufMax) || vorlauf > VorlaufMax) VorlaufMax = vorlauf;
        }

        /// <summary>Bildet den gewichteten Rücklauf und zählt die Stunden. Danach nimmt das Objekt nichts mehr auf.</summary>
        public void Abschliessen()
        {
            int stunden = 0;
            for (int h = 0; h < STUNDEN; h++)
            {
                if (_gewicht[h] > 0)
                {
                    RuecklaufC[h] = _ruecklaufSumme[h] / _gewicht[h];
                    stunden++;
                }
                else
                {
                    VorlaufC[h] = double.NaN;
                    RuecklaufC[h] = double.NaN;
                }
            }
            Stunden = stunden;
            _abgeschlossen = true;
        }

        /// <summary>Der geforderte Vorlauf der Stunde [°C]; NaN außerhalb des Jahres und ohne Prozess mit Paar.</summary>
        public double Vorlauf(int stunde)
            => stunde >= 0 && stunde < STUNDEN ? VorlaufC[stunde] : double.NaN;

        /// <summary>Der Rücklauf der Stunde [°C]; NaN außerhalb des Jahres und ohne Prozess mit Paar.</summary>
        public double Ruecklauf(int stunde)
            => stunde >= 0 && stunde < STUNDEN ? RuecklaufC[stunde] : double.NaN;

        // =================================================================================
        // Die Regeln, die mehrere Leser teilen
        // =================================================================================

        /// <summary>
        /// <b>Erreicht ein Erzeuger den geforderten Vorlauf?</b> Nur ein GEPFLEGTER Erzeugervorlauf
        /// (&gt; 0) kann ausschließen — 0 heißt „nicht angegeben", nicht „0 °C" (dieselbe Lesart wie
        /// Warnkriterium W3). Ohne Forderung (NaN) gilt jeder als erreichend. Der Vergleich trägt den
        /// Zahlenrand (<see cref="Rechenrand.SchwelleErreicht"/>).
        /// </summary>
        public static bool Erreicht(double erzeugerVorlauf, double gefordert)
        {
            if (double.IsNaN(gefordert)) return true;
            if (!(erzeugerVorlauf > 0)) return true;
            return Rechenrand.SchwelleErreicht(erzeugerVorlauf, gefordert);
        }

        /// <summary>
        /// Der Rücklauf einer Kesselstunde, wenn ein Teil ihrer Wärme in den Prozesskanal ging: der
        /// Prozessrücklauf für den Prozessanteil, der Rücklauf der Kette für den Rest, gewichtet mit
        /// der Wärme. <paramref name="anteilProzess"/> wird auf 0 … 1 geklemmt; ohne Prozessrücklauf
        /// (NaN) bleibt der Rücklauf der Kette.
        /// </summary>
        public static double MischRuecklauf(double anteilProzess, double prozessRuecklauf, double sonstRuecklauf)
        {
            if (double.IsNaN(prozessRuecklauf) || !(anteilProzess > 0)) return sonstRuecklauf;
            double a = anteilProzess > 1 ? 1 : anteilProzess;
            return a * prozessRuecklauf + (1 - a) * sonstRuecklauf;
        }

        /// <summary>
        /// Das Temperaturpaar einer Kopfzeile (<c>Tab_Prozesswaerme(_STAMM)</c>): <c>true</c> nur bei
        /// VOLLSTÄNDIGEM Paar. Spaltentolerant — eine Datenbank vor dem Schemaschritt hat keine
        /// Spalten und liefert <c>false</c>.
        /// </summary>
        public static bool PaarAusZeile(DataRow kopf, out double vorlauf, out double ruecklauf)
        {
            vorlauf = double.NaN;
            ruecklauf = double.NaN;
            if (kopf == null) return false;
            double? v = Zahl(kopf, ProzesswaermeTemperaturSchema.SPALTE_VORLAUF);
            double? r = Zahl(kopf, ProzesswaermeTemperaturSchema.SPALTE_RUECKLAUF);
            if (v == null || r == null) return false;
            vorlauf = v.Value;
            ruecklauf = r.Value;
            return true;
        }

        /// <summary>
        /// <b>Die Prüfung eines Paars</b> — dieselben Grenzen wie die Prüfklauseln des Schemas: beide
        /// leer oder beide gesetzt, je 0 … 250 °C, Vorlauf nicht unter dem Rücklauf. <c>null</c> =
        /// zulässig; sonst der Grund (Ressourcentext).
        /// </summary>
        public static string Paarpruefung(double? vorlauf, double? ruecklauf)
        {
            if (vorlauf == null && ruecklauf == null) return null;
            if (vorlauf == null || ruecklauf == null) return MyResource.Resource.PW_MSG_TEMPERATUR_PAAR;
            double v = vorlauf.Value, r = ruecklauf.Value;
            if (double.IsNaN(v) || double.IsNaN(r) ||
                v < ProzesswaermeTemperaturSchema.MIN_GRAD || v > ProzesswaermeTemperaturSchema.MAX_GRAD ||
                r < ProzesswaermeTemperaturSchema.MIN_GRAD || r > ProzesswaermeTemperaturSchema.MAX_GRAD)
                return MyResource.Resource.PW_MSG_TEMPERATUR_BEREICH;
            if (v < r) return MyResource.Resource.PW_MSG_TEMPERATUR_REIHENFOLGE;
            return null;
        }

        /// <summary>Das Paar als Anzeigetext „90 / 60 °C"; leer ohne vollständiges Paar.</summary>
        public static string Anzeige(double? vorlauf, double? ruecklauf, CultureInfo kultur = null)
        {
            if (vorlauf == null || ruecklauf == null) return "";
            CultureInfo k = kultur ?? CultureInfo.CurrentCulture;
            return vorlauf.Value.ToString("0.#", k) + " / " + ruecklauf.Value.ToString("0.#", k) + " °C";
        }

        private static double? Zahl(DataRow r, string spalte)
        {
            if (r == null || !r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return null; }
        }
    }
}
