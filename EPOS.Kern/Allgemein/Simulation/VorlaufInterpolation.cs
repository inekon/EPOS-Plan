using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Kernschalter der Interpolation über den Vorlauf</b> (AK3-I, Entwurf AK3 Abschnitt 3,
    /// Festlegungen I-1 bis I-5; Anwenderentscheid Q-AK3-6) — ohne Schema, nicht wählbar in der
    /// Oberfläche, <b>Vorgabe aus</b>.
    ///
    /// <para><b>Aus</b> rechnet der Lauf Zeichen für Zeichen wie der Bestand: die Heizseite mit der
    /// nächstgelegenen Stützstelle (H-F4), die Kälteseite mit der nächsten Stützstelle des
    /// <c>Kuehl_Vorlauf</c> (K21). <b>Ein</b> interpoliert die Heizseite am gerechneten Vorlauf
    /// zwischen den beiden einschließenden Kennlinien (I-1; Heizleistung und COP je für sich
    /// linear, die elektrische Leistung folgt als Φ/COP), die Kälteseite zwischen den beiden
    /// einschließenden Kühl-Vorläufen (I-3); außerhalb der Stützstellen gilt in beiden Fällen die
    /// Randregel des Bestands (I-2, F-A8). Prozessanteil und Desinfektion behalten die Regel
    /// „die Kennlinie muss die Temperatur erreichen“ (I-5).</para>
    ///
    /// <para>Der Schalter wird beim Aufbau eines Laufs gelesen (Kennlinien der Wärmepumpe, Kühlkennlinie);
    /// Proben und Messläufe setzen ihn mit <see cref="Schalten"/> für die Dauer eines Blocks.</para>
    /// </summary>
    public static class VorlaufInterpolation
    {
        /// <summary>true = Interpolation über den Vorlauf (I-1, I-3); Vorgabe false = Bestand.</summary>
        public static bool Ein { get; set; }

        /// <summary>
        /// Setzt den Schalter für die Dauer eines <c>using</c>-Blocks und stellt danach den
        /// vorherigen Wert zurück.
        /// </summary>
        public static IDisposable Schalten(bool ein)
        {
            bool vorher = Ein;
            Ein = ein;
            return new Rueckstellung(vorher);
        }

        private sealed class Rueckstellung : IDisposable
        {
            private readonly bool _vorher;
            private bool _erledigt;

            internal Rueckstellung(bool vorher) { _vorher = vorher; }

            public void Dispose()
            {
                if (_erledigt) return;
                _erledigt = true;
                Ein = _vorher;
            }
        }

        /// <summary>
        /// Die Lage eines Vorlaufs <paramref name="vorlauf"/> zwischen den aufsteigenden Stützstellen
        /// <paramref name="stuetzstellen"/> (I-1, I-3): true, wenn er STRENG zwischen zwei benachbarten
        /// liegt — dann ist <paramref name="unten"/> der Index der unteren und <paramref name="gewicht"/>
        /// der Anteil der oberen, (V − V_u)/(V_o − V_u) in (0, 1). Auf einer Stützstelle und außerhalb
        /// false (I-2: dort gilt die Stützstelle bzw. die Randregel).
        /// </summary>
        public static bool Einschliessend(IReadOnlyList<int> stuetzstellen, double vorlauf,
                                          out int unten, out double gewicht)
        {
            unten = -1;
            gewicht = 0.0;
            if (stuetzstellen == null || double.IsNaN(vorlauf) || double.IsInfinity(vorlauf)) return false;
            for (int i = 0; i + 1 < stuetzstellen.Count; i++)
            {
                double vu = stuetzstellen[i], vo = stuetzstellen[i + 1];
                if (vorlauf > vu && vorlauf < vo)
                {
                    unten = i;
                    gewicht = (vorlauf - vu) / (vo - vu);
                    return true;
                }
            }
            return false;
        }
    }
}
