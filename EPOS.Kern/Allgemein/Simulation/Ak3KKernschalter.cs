using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Kernschalter AK3-K</b> (Entwurf AK3-K, Festlegung 21; Muster <see cref="Ak3Kernstufe"/>): ohne Schema,
    /// Vorgabe <b>aus</b>. Erst mit dem Schalter wirkt auf der Stufe AK3 die Kälteseite des Kreises — der Kühlkanal folgt
    /// der Kreisreihe (Fehler 1.1 (a)), die Wärmeschranke kennt die Heizsperre der reversiblen Wärmepumpe am Kühltag
    /// (Fehler 1.1 (b), K8a), und die Kältekaskade rechnet je Stunde nach der Wärmestunde
    /// (<see cref="Kaeltekaskade.StundeRechnen"/>, Festlegung 16). Ausgeschaltet rechnet jeder Lauf Zeichen für Zeichen
    /// wie zuvor (Gate „Referenzlauf 23/23 byte-gleich gegen R42“); Proben und der Referenzlauf <c>--ak3k ein</c> schalten
    /// ein. Mit der Basiswelle K5 wird der Schalter eingeschaltet und entfernt.
    /// </summary>
    public static class Ak3KKernschalter
    {
        /// <summary>Der Schalter des Prozesses; Vorgabe <c>false</c> (aus).</summary>
        public static bool Ein { get; set; }

        /// <summary>
        /// Wirkt AK3-K für ein Projekt mit der Kopplungsstufe <paramref name="stufe"/>? Nur auf der Stufe AK3
        /// (<see cref="Ak3Kernstufe"/>) <b>und</b> mit eingeschaltetem Schalter.
        /// </summary>
        internal static bool Wirksam(string stufe) => Ein && Ak3Kernstufe.Wirksam(stufe);

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
