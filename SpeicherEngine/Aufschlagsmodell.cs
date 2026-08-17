using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Die beiden Modi des Aufschlagsblocks (Fachkonzept 4.2).
    /// </summary>
    public enum AufschlagsModus
    {
        /// <summary>
        /// Standard: Der wirksame Aufschlag ist die Summe der AKTIVEN Komponenten.
        /// </summary>
        Aufgeschluesselt = 0,

        /// <summary>
        /// Der Anwender traegt einen Gesamtaufschlag ein. Die Komponentenliste bleibt
        /// sichtbar und informativ; die Differenz zur Komponentensumme wird als
        /// "nicht aufgeschluesselter Rest" ausgewiesen.
        /// </summary>
        Gesamtwert = 1
    }

    /// <summary>
    /// Eine Aufschlagskomponente: Wert [ct/kWh] und Aktiv-Schalter (Fachkonzept 4.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Schluessel ist sprachneutral.</b> <see cref="Schluessel"/> traegt einen
    /// ASCII-Bezeichner (<c>NETZENTGELT</c>, <c>UMLAGEN</c>, ...), keinen Anzeigetext -
    /// Schicht 2 der Drei-Schichten-Regel. Die Beschriftung holt das Hauptprojekt aus
    /// <c>MyResource</c>; die Engine kennt keine Oberflaechensprache.
    /// </para>
    /// <para>
    /// Unveraenderlich: Ein Satz wird gebaut, gerechnet und weggeworfen. Damit kann
    /// dieselbe Instanz gefahrlos in der Rastersuche gelesen werden - dieselbe Zusage
    /// wie bei <see cref="SpeicherEingang"/>.
    /// </para>
    /// </remarks>
    public sealed class Aufschlagskomponente
    {
        /// <summary>Sprachneutraler ASCII-Schluessel der Komponente.</summary>
        public string Schluessel { get; }

        /// <summary>Wert der Komponente [ct/kWh]. Darf 0 sein.</summary>
        public double WertCtKwh { get; }

        /// <summary>true, wenn die Komponente in die Summe eingeht.</summary>
        public bool Aktiv { get; }

        /// <summary>Erzeugt eine Komponente.</summary>
        /// <exception cref="ArgumentException">Wenn der Schluessel leer ist.</exception>
        public Aufschlagskomponente(string schluessel, double wertCtKwh, bool aktiv)
        {
            if (string.IsNullOrWhiteSpace(schluessel))
                throw new ArgumentException("Der Schluessel einer Aufschlagskomponente darf nicht leer sein.",
                                            nameof(schluessel));

            Schluessel = schluessel;
            WertCtKwh = wertCtKwh;
            Aktiv = aktiv;
        }

        /// <summary>Der Beitrag dieser Komponente zur Summe: der Wert, oder 0 wenn inaktiv.</summary>
        public double BeitragCtKwh
        {
            get { return Aktiv ? WertCtKwh : 0.0; }
        }
    }

    /// <summary>
    /// Der vollstaendige Aufschlagssatz eines Projekts (Fachkonzept 4.2): die
    /// Komponentenliste, der Modus und der Override-Gesamtwert.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum in der Engine und nicht im Controller.</b> Die Regel "wirksamer
    /// Aufschlag = Summe der aktiven Komponenten ODER Override" entscheidet ueber
    /// jeden Geldwert des Laufs. Sie steht deshalb dort, wo sie ohne Datenbank und
    /// ohne Oberflaeche geprueft werden kann - genau die Trennung, mit der AP2b die
    /// zwei Speichermodelle des Bestands beseitigt hat.
    /// </para>
    /// <para>
    /// <b>Die Vorschlagswerte stehen NICHT hier.</b> 6,44 / 2,946 / 2,05 / 0,11 /
    /// 0,20 ct/kWh sind Vorbelegungen der Datenbank (Migrationsschritt 12) und
    /// gehoeren dorthin - die Engine rechnet mit dem, was gepflegt ist, und
    /// behauptet nichts ueber Netzentgelte.
    /// </para>
    /// </remarks>
    public sealed class Aufschlagssatz
    {
        private readonly Aufschlagskomponente[] _komponenten;

        /// <summary>Die Komponenten in Eingabereihenfolge.</summary>
        public IReadOnlyList<Aufschlagskomponente> Komponenten
        {
            get { return _komponenten; }
        }

        /// <summary>Gewaehlter Modus.</summary>
        public AufschlagsModus Modus { get; }

        /// <summary>Gesamtaufschlag [ct/kWh] im Modus <see cref="AufschlagsModus.Gesamtwert"/>.</summary>
        public double OverrideCtKwh { get; }

        /// <summary>
        /// Erzeugt einen Aufschlagssatz. Die Komponentenliste wird kopiert; die
        /// Instanz ist danach unveraenderlich.
        /// </summary>
        /// <param name="komponenten">Komponentenliste, darf leer, aber nicht <c>null</c> sein.</param>
        /// <param name="modus">Aufgeschluesselt oder Gesamtwert.</param>
        /// <param name="overrideCtKwh">Gesamtaufschlag; nur im Modus Gesamtwert wirksam.</param>
        /// <exception cref="ArgumentNullException">Wenn die Liste oder ein Eintrag <c>null</c> ist.</exception>
        public Aufschlagssatz(IEnumerable<Aufschlagskomponente> komponenten,
                              AufschlagsModus modus = AufschlagsModus.Aufgeschluesselt,
                              double overrideCtKwh = 0.0)
        {
            if (komponenten == null) throw new ArgumentNullException(nameof(komponenten));

            List<Aufschlagskomponente> liste = new List<Aufschlagskomponente>();
            foreach (Aufschlagskomponente k in komponenten)
            {
                if (k == null) throw new ArgumentNullException(nameof(komponenten),
                    "Die Komponentenliste enthaelt einen null-Eintrag.");
                liste.Add(k);
            }

            _komponenten = liste.ToArray();
            Modus = modus;
            OverrideCtKwh = overrideCtKwh;
        }

        /// <summary>
        /// Summe der AKTIVEN Komponenten [ct/kWh] - die Live-Summe der Oberflaeche.
        /// </summary>
        /// <remarks>
        /// Sequenzielle Summation ueber <see cref="Numerik.SummeSequenziell(double[])"/>: Der
        /// Wert erscheint auf dem Bildschirm UND geht in den Geldwert ein; zwei
        /// Summationsreihenfolgen ergaeben zwei Zahlen fuer dieselbe Groesse.
        /// </remarks>
        public double SummeAktivCtKwh
        {
            get
            {
                double[] beitraege = new double[_komponenten.Length];
                for (int i = 0; i < _komponenten.Length; i++) beitraege[i] = _komponenten[i].BeitragCtKwh;
                return Numerik.SummeSequenziell(beitraege);
            }
        }

        /// <summary>
        /// Der Aufschlag, mit dem tatsaechlich gerechnet wird [ct/kWh]:
        /// im Modus <see cref="AufschlagsModus.Aufgeschluesselt"/> die
        /// <see cref="SummeAktivCtKwh"/>, im Modus
        /// <see cref="AufschlagsModus.Gesamtwert"/> der <see cref="OverrideCtKwh"/>.
        /// </summary>
        public double WirksamCtKwh
        {
            get { return Modus == AufschlagsModus.Gesamtwert ? OverrideCtKwh : SummeAktivCtKwh; }
        }

        /// <summary>
        /// Der "nicht aufgeschluesselte Rest" [ct/kWh] (Fachkonzept 4.2):
        /// <c>Override - Summe der aktiven Komponenten</c>. Im Modus
        /// <see cref="AufschlagsModus.Aufgeschluesselt"/> immer exakt 0.
        /// </summary>
        /// <remarks>
        /// Beispiel des Fachkonzepts: Bei 20 ct/kWh Gesamtaufschlag und 11,746 ct/kWh
        /// aufgeschluesselt bleiben 8,254 ct/kWh Rest (Regelfall) bzw. 10,254 ct/kWh
        /// im reduzierten Stromsteuerfall. Ein NEGATIVER Rest ist zulaessig und
        /// bedeutet: Der eingetragene Gesamtwert liegt unter der Komponentensumme -
        /// die Oberflaeche weist ihn aus, statt ihn zu verschweigen.
        /// </remarks>
        public double NichtAufgeschluesselterRestCtKwh
        {
            get { return Modus == AufschlagsModus.Gesamtwert ? OverrideCtKwh - SummeAktivCtKwh : 0.0; }
        }

        /// <summary>
        /// Legt den wirksamen Aufschlag auf eine Preisreihe (Kurzform fuer
        /// <see cref="PreisModell.MitAufschlag"/> mit <see cref="WirksamCtKwh"/>).
        /// </summary>
        /// <exception cref="ArgumentNullException">Wenn <paramref name="reihe"/> <c>null</c> ist.</exception>
        public double[] AufReihe(double[] reihe)
        {
            return PreisModell.MitAufschlag(reihe, WirksamCtKwh);
        }
    }
}
