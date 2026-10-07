using System;

namespace WindowsFormsApplication1
{
    /// <summary>Wie der Kern die Stufe AK3 behandelt (AK3-W3b; bis W4 nur im Kern schaltbar).</summary>
    public enum Ak3Kernmodus
    {
        /// <summary>Vorgabe: kein AK3-Weg; ein gespeichertes AK3 rechnet als AK1 mit Hinweis (Festlegung 3).</summary>
        Aus = 0,

        /// <summary>Ein Projekt mit der Projektspalte <c>Tab_Einstellungen.Anlagenkopplung</c> = AK3 rechnet den Kreis.</summary>
        GespeicherteStufe = 1,

        /// <summary>Für Proben und Messungen: jedes gekoppelte Projekt (AK1, AK2, AK3) rechnet den Kreis.</summary>
        AlleGekoppelten = 2,
    }

    /// <summary>
    /// <b>Die Stufe AK3 als Kernschalter</b> (Entwurf AK3, Wellenplan W3; Festlegung 3). Die Oberfläche
    /// (<c>StufeGebaut</c>, Dialog) folgt mit W4; bis dahin setzen nur Proben und der Referenzlauf
    /// (<c>--ak3</c>) den Modus. Vorgabe <see cref="Ak3Kernmodus.Aus"/>: Jeder Lauf rechnet Zeichen für Zeichen
    /// wie zuvor.
    /// </summary>
    public static class Ak3Kernstufe
    {
        /// <summary>Der Modus des Prozesses; Vorgabe <see cref="Ak3Kernmodus.Aus"/>.</summary>
        public static Ak3Kernmodus Modus { get; set; }

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
