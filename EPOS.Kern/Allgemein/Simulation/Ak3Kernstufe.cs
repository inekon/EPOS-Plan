using System;

namespace WindowsFormsApplication1
{
    /// <summary>Wie der Kern die Stufe AK3 behandelt (Testschalter; Vorgabe: die gespeicherte Stufe gilt).</summary>
    public enum Ak3Kernmodus
    {
        /// <summary>Testschalter: kein AK3-Weg; ein gespeichertes AK3 rechnet als AK2 auf dem Profilweg (wie vor W4).</summary>
        Aus = 0,

        /// <summary>
        /// <b>Vorgabe</b> (W4a; E102 Q-AK3-1): Ein Projekt mit der Projektspalte <c>Tab_Einstellungen.Anlagenkopplung</c>
        /// = AK3 rechnet im Projektlauf den Kreis.
        /// </summary>
        GespeicherteStufe = 1,

        /// <summary>Für Proben und Messungen: jedes gekoppelte Projekt (AK1, AK2, AK3) rechnet den Kreis.</summary>
        AlleGekoppelten = 2,
    }

    /// <summary>
    /// <b>Die Stufe AK3 im Kern</b> (Entwurf AK3, Festlegungen 3 und 20; E102 Q-AK3-1). AK3 ist eine eigene, wählbare
    /// Stufe (<see cref="Waermeuebergabevorgaben.StufeGebaut"/>): Ein Projekt, das AK3 gespeichert hat, rechnet im
    /// Projektlauf den geschlossenen Kreis. Der Modus bleibt nur als <b>Testschalter</b> (Proben, Referenzlauf
    /// <c>--ak3</c>); die Testdatenbank führt kein AK3-Projekt, jeder andere Lauf rechnet Zeichen für Zeichen wie zuvor.
    /// <para><b>Rückstufe der Auskünfte</b> (Festlegung 20): Eine Auskunft, die je Abfrage einen Projektlauf bräuchte
    /// (Bedarfsdialog, Aufheizauskunft, Verhältnisrechnung E8), rechnet bei Stufe AK3 auf dem Profilweg (AK1/AK2) und nennt
    /// die Rückstufe (<see cref="Rueckstufe"/>, <see cref="RUECKSTUFE_TEXT"/>).</para>
    /// </summary>
    public static class Ak3Kernstufe
    {
        /// <summary>Der Modus des Prozesses; Vorgabe <see cref="Ak3Kernmodus.GespeicherteStufe"/>.</summary>
        public static Ak3Kernmodus Modus { get; set; } = Ak3Kernmodus.GespeicherteStufe;

        /// <summary>Der benannte Hinweis der Rückstufe einer Auskunft (Festlegung 20; Ressource folgt mit W4b).</summary>
        public const string RUECKSTUFE_TEXT =
            "Anlagenkopplung AK3: Diese Auskunft rechnet ohne geschlossenen Kreis auf dem Profilweg (AK2); " +
            "der Projektlauf rechnet AK3.";

        /// <summary>Rechnet ein Projekt mit der Kopplungsstufe <paramref name="stufe"/> im AK3-Weg?</summary>
        internal static bool Wirksam(string stufe)
        {
            switch (Modus)
            {
                case Ak3Kernmodus.GespeicherteStufe: return stufe == DbWerte.ANLAGENKOPPLUNG_AK3;
                case Ak3Kernmodus.AlleGekoppelten: return Waermeuebergabe.StufeAn(stufe);
                default: return false;
            }
        }

        /// <summary>
        /// Stuft eine Auskunft mit der Stufe <paramref name="stufe"/> zurück (Festlegung 20)? true, wenn der Projektlauf
        /// den Kreis rechnen würde — die Auskunft rechnet dann auf dem Profilweg und nennt es.
        /// </summary>
        public static bool Rueckstufe(string stufe) => Wirksam(stufe);

        /// <summary>Setzt den Modus für die Dauer eines <c>using</c>-Blocks und stellt danach den vorherigen zurück.</summary>
        public static IDisposable Schalten(Ak3Kernmodus modus)
        {
            Ak3Kernmodus vorher = Modus;
            Modus = modus;
            return new Rueckstellung(vorher);
        }

        private sealed class Rueckstellung : IDisposable
        {
            private readonly Ak3Kernmodus _vorher;
            private bool _erledigt;

            internal Rueckstellung(Ak3Kernmodus vorher) { _vorher = vorher; }

            public void Dispose()
            {
                if (_erledigt) return;
                _erledigt = true;
                Modus = _vorher;
            }
        }
    }
}
