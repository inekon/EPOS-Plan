using System;
using System.Collections.Generic;

namespace SpeicherEngine
{
    /// <summary>
    /// Ein Preisanteil des Strombezugs- oder Brennstoffpreises: Wert [ct/kWh] und
    /// Aktiv-Schalter (Fachkonzept Stromspeicher 4.2, Konzept BHKW § 4.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Der Schluessel ist sprachneutral.</b> <see cref="Schluessel"/> traegt einen
    /// ASCII-Bezeichner (<c>BESCHAFFUNG</c>, <c>NETZENTGELT</c>, ...), keinen
    /// Anzeigetext - Schicht 2 der Drei-Schichten-Regel. Die Beschriftung holt das
    /// Hauptprojekt aus <c>MyResource</c>; die Engine kennt keine Oberflaechensprache.
    /// </para>
    /// <para>
    /// Unveraenderlich: Ein Satz wird gebaut, gerechnet und weggeworfen. Damit kann
    /// dieselbe Instanz gefahrlos in der Rastersuche gelesen werden - dieselbe Zusage
    /// wie bei <see cref="SpeicherEingang"/>.
    /// </para>
    /// </remarks>
    public sealed class Preisanteil
    {
        /// <summary>Sprachneutraler ASCII-Schluessel des Anteils.</summary>
        public string Schluessel { get; }

        /// <summary>Wert des Anteils [ct/kWh]. Darf 0 sein.</summary>
        public double WertCtKwh { get; }

        /// <summary>true, wenn der Anteil in die Summe eingeht.</summary>
        public bool Aktiv { get; }

        /// <summary>Erzeugt einen Anteil.</summary>
        /// <exception cref="ArgumentException">Wenn der Schluessel leer ist.</exception>
        public Preisanteil(string schluessel, double wertCtKwh, bool aktiv)
        {
            if (string.IsNullOrWhiteSpace(schluessel))
                throw new ArgumentException("Der Schluessel eines Preisanteils darf nicht leer sein.",
                                            nameof(schluessel));

            Schluessel = schluessel;
            WertCtKwh = wertCtKwh;
            Aktiv = aktiv;
        }

        /// <summary>Der Beitrag dieses Anteils zur Summe: der Wert, oder 0 wenn inaktiv.</summary>
        public double BeitragCtKwh
        {
            get { return Aktiv ? WertCtKwh : 0.0; }
        }
    }

    /// <summary>
    /// Die <b>Zerlegung EINES Preises</b> in seine Anteile (Fachkonzept Stromspeicher
    /// 4.2 in der Fassung des Anwenderentscheids SP-E-2 vom 17.09.2026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Es wird nichts aufgeschlagen.</b> Der Satz beschreibt, WORAUS der
    /// Arbeitspreis besteht - Beschaffung, Vertrieb, Netzentgelt, Steuern, Abgaben und
    /// Umlagen -, er addiert nichts auf ihn. Die aussagekraeftige Groesse ist die
    /// <see cref="SummeAktivCtKwh"/>; sie steht neben dem Arbeitspreis und wird mit ihm
    /// verglichen (Kohaerenzzeile), nie zu ihm addiert. Damit gilt fuer den Strom
    /// dieselbe Regel wie fuer den Brennstoff seit Etappe B2: <b>es gibt genau eine
    /// Preiswahrheit, und das ist der Arbeitspreis des Traegers.</b>
    /// </para>
    /// <para>
    /// <b>Die einzige Ausnahme ist die Spot- bzw. Profilreihe.</b> Dort IST die Reihe
    /// die Beschaffung; was fehlt, sind die uebrigen Anteile. Dafuer - und nur dafuer -
    /// gibt es <see cref="SummeAktivOhneCtKwh"/>: die Summe ohne einen benannten
    /// Anteil. Der Aufrufer nennt den Schluessel, die Engine kennt keinen bevorzugten.
    /// </para>
    /// <para>
    /// <b>Kein Modus mehr.</b> Bis SP-W2/W3 entschied ein Modus („kein Aufschlag" /
    /// „Gesamtwert" / „aufgeschluesselt") ueber den wirksamen Wert. Er ist entfallen:
    /// Ein Anteil, der nicht gepflegt ist, ist 0 und inaktiv, und die Summe ist damit
    /// von selbst 0 - „ein aktiver Anteil vorhanden" sagt alles, was der Modus sagte.
    /// Ein Gesamtwert-Override liess sich ohnehin nicht in Anteile zerlegen; er ist mit
    /// Schemaschritt 83 in den Arbeitspreis gefaltet.
    /// </para>
    /// <para>
    /// <b>Die Vorschlagswerte stehen NICHT hier.</b> 6,44 / 2,946 / 2,05 / 0,11 /
    /// 0,20 ct/kWh sind Katalog- und Vorbelegungswerte des Hauptprojekts - die Engine
    /// rechnet mit dem, was gepflegt ist, und behauptet nichts ueber Netzentgelte.
    /// </para>
    /// </remarks>
    public sealed class Preiszerlegung
    {
        private readonly Preisanteil[] _komponenten;

        /// <summary>Die Anteile in Eingabereihenfolge.</summary>
        public IReadOnlyList<Preisanteil> Komponenten
        {
            get { return _komponenten; }
        }

        /// <summary>
        /// Erzeugt eine Preiszerlegung. Die Anteilsliste wird kopiert; die Instanz ist
        /// danach unveraenderlich.
        /// </summary>
        /// <param name="komponenten">Anteilsliste, darf leer, aber nicht <c>null</c> sein.</param>
        /// <exception cref="ArgumentNullException">Wenn die Liste oder ein Eintrag <c>null</c> ist.</exception>
        public Preiszerlegung(IEnumerable<Preisanteil> komponenten)
        {
            if (komponenten == null) throw new ArgumentNullException(nameof(komponenten));

            List<Preisanteil> liste = new List<Preisanteil>();
            foreach (Preisanteil k in komponenten)
            {
                if (k == null) throw new ArgumentNullException(nameof(komponenten),
                    "Die Anteilsliste enthaelt einen null-Eintrag.");
                liste.Add(k);
            }

            _komponenten = liste.ToArray();
        }

        /// <summary>
        /// Summe der AKTIVEN Anteile [ct/kWh] - die Live-Summe der Oberflaeche und die
        /// Groesse, die gegen den Arbeitspreis gehalten wird.
        /// </summary>
        /// <remarks>
        /// Sequenzielle Summation ueber <see cref="Numerik.SummeSequenziell(double[])"/>: Der
        /// Wert erscheint auf dem Bildschirm UND geht in den Geldwert ein; zwei
        /// Summationsreihenfolgen ergaeben zwei Zahlen fuer dieselbe Groesse.
        /// </remarks>
        public double SummeAktivCtKwh
        {
            get { return Summe(""); }
        }

        /// <summary>
        /// Summe der AKTIVEN Anteile [ct/kWh] <b>ohne</b> den Anteil mit diesem
        /// Schluessel - der Satz, der auf eine Spot- oder Profilreihe gehoert, weil
        /// diese Reihe die Beschaffung schon enthaelt (Fachkonzept 4.1 a/b).
        /// </summary>
        /// <param name="schluessel">
        /// Der auszulassende Schluessel; ein leerer oder unbekannter laesst nichts aus
        /// und liefert damit <see cref="SummeAktivCtKwh"/>.
        /// </param>
        public double SummeAktivOhneCtKwh(string schluessel)
        {
            return Summe(schluessel);
        }

        private double Summe(string ohneSchluessel)
        {
            bool alle = string.IsNullOrEmpty(ohneSchluessel);
            double[] beitraege = new double[_komponenten.Length];
            for (int i = 0; i < _komponenten.Length; i++)
                beitraege[i] = !alle && string.Equals(_komponenten[i].Schluessel, ohneSchluessel,
                                                      StringComparison.Ordinal)
                    ? 0.0
                    : _komponenten[i].BeitragCtKwh;
            return Numerik.SummeSequenziell(beitraege);
        }
    }
}
