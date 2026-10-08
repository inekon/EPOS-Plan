using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Kernschalter der Kühlkurve</b> (Entwurf KK, Festlegung 13; Muster <see cref="Ak3Kernstufe"/> und des Kernschalters
    /// AK3-K vor K5a): ohne Schema, Vorgabe <b>aus</b>. Erst mit dem Schalter wirkt auf der Stufe AK3 (E106, Q-KK-2 (a)) die
    /// Kühlkurve — der Kühlvorlauf des Gebäudes wird eine Jahresreihe nach <see cref="Kuehlkurve"/>. Ausgeschaltet rechnet
    /// jeder Lauf Zeichen für Zeichen wie zuvor (Gate „Referenzlauf 24/24 byte-gleich gegen R43“); Proben schalten ein.
    /// Mit der Basiswelle KK5 wird der Schalter eingeschaltet und entfernt.
    /// </summary>
    public static class KuehlkurveKernschalter
    {
        /// <summary>Der Schalter des Prozesses; Vorgabe <c>false</c> (aus).</summary>
        public static bool Ein { get; set; }

        /// <summary>
        /// Wirkt die Kühlkurve für ein Projekt mit der Kopplungsstufe <paramref name="stufe"/>? Nur auf der Stufe AK3
        /// (<see cref="Ak3Kernstufe"/>) <b>und</b> mit eingeschaltetem Schalter.
        /// </summary>
        internal static bool Wirksam(string stufe) => Ein && Ak3Kernstufe.Wirksam(stufe);

        /// <summary>
        /// Rechnet der Mehrzonenweg die Kühlübergabe je Zone (Entwurf KK, KZ1)? Allein der Schalter — die Stufe entscheidet
        /// wie im Einzonenweg die Kälteseite selbst, ab AK1 (E106, Q-KK-7 (a)).
        /// </summary>
        internal static bool Zonenkuehlung => Ein;

        /// <summary>Setzt den Schalter für die Dauer eines <c>using</c>-Blocks und stellt danach den vorherigen zurück.</summary>
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
    }
}
