using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SpeicherEngine
{
    /// <summary>
    /// DIE KULTUR DES AUFRUFERS AUF DEN ARBEITSFAEDEN (Auftrag #232, Anwenderentscheid
    /// „#231: Empfehlung umsetzen" vom 12.09.2026).
    ///
    /// <para><b>Das Problem.</b> Ein Faden, der KEINE eigene Kultur gesetzt hat, liest
    /// bei JEDEM Zugriff auf <see cref="CultureInfo.CurrentCulture"/> beziehungsweise
    /// <see cref="CultureInfo.CurrentUICulture"/> den prozessweiten Vorgabewert
    /// (<c>CultureInfo.DefaultThreadCurrentCulture</c> / <c>…UICulture</c>). Dieser
    /// Vorgabewert ist VERAENDERLICH: Im Produkt setzt ihn der Sprachwechsel, im
    /// Testprozess setzen und entfernen ihn ueber 50 Testklassen bei jedem
    /// Konstruktor- und <c>Dispose</c>-Aufruf. Faellt eine solche Umschaltung mitten in
    /// eine laufende Rechnung, rechnet der eine Arbeitsfaden deutsch und der naechste
    /// englisch — und ein Etikett, das ueber seinen deutschen Text gesucht wird, ist
    /// nicht mehr zu finden (Befund #231, Windows-Lauf 319:
    /// <c>SpeicherOptimierungCtrlTests</c> mit „Sequence contains no matching
    /// element").</para>
    ///
    /// <para><b>Was hier gemessen wurde — und was daraus folgt.</b> Setzt der Aufrufer
    /// seine Kultur AUSDRUECKLICH (<c>Thread.CurrentThread.CurrentCulture = …</c> oder
    /// <c>CultureInfo.CurrentCulture = …</c>), so traegt .NET sie seit .NET Core ueber
    /// den <c>ExecutionContext</c> von selbst in jeden <c>Task.Run</c>, in jedes
    /// Arbeitspaket eines <c>Parallel.For</c> und sogar in einen frisch gestarteten
    /// <c>Thread</c> — die Setzer schreiben intern einen <c>AsyncLocal</c>-Wert. Die
    /// Luecke ist genau der ANDERE Fall: Hat der Aufrufer NICHTS gesetzt und bezieht
    /// seine Kultur selbst nur aus dem prozessweiten Vorgabewert, dann gibt es nichts
    /// zu erben — Aufrufer und Arbeitsfaeden lesen denselben veraenderlichen Wert immer
    /// wieder neu, und eine Umschaltung von aussen trifft sie einzeln und zu
    /// verschiedenen Zeitpunkten.</para>
    ///
    /// <para><b>Die Loesung.</b> Diese Vorrichtung ERFASST die Kultur des Aufrufers
    /// EINMAL am Einstieg — den aufgeloesten Wert, gleichgueltig ob er gepinnt war oder
    /// aus dem Vorgabewert stammt — und setzt sie je Arbeitspaket auf dem Arbeitsfaden,
    /// bevor die Arbeit beginnt. Damit liest kein Arbeitsfaden mehr den veraenderlichen
    /// Vorgabewert, und eine Umschaltung mitten im Lauf erreicht ihn nicht mehr.
    /// Danach wird der Vorstand des Fadens wiederhergestellt: Arbeitsfaeden sind
    /// Pool-Faeden, und ein stehengelassener Wert waere genau der Fehler, den die
    /// Vorrichtung verhindern soll.</para>
    ///
    /// <para><b>Kein <c>AsyncLocal</c> von uns.</b> Aus demselben Grund, aus dem
    /// <c>Vorgangsklammer</c> im Kern <c>[ThreadStatic]</c> nimmt: Ein eigener
    /// <c>AsyncLocal</c> flosse unkontrolliert in jeden Nebenlaeufer weiter und truege
    /// dort einen Zustand hinein, den niemand zurueckstellt. Hier wird ausschliesslich
    /// der Faden des Arbeitspakets fuer dessen Dauer gesetzt.</para>
    ///
    /// <para><b>Und kein Griff an den prozessweiten Vorgabewert.</b>
    /// <c>DefaultThreadCurrentCulture</c> zu setzen waere der Fehler selbst — er wirkt
    /// auf JEDEN Faden des Prozesses, auch auf unbeteiligte.</para>
    ///
    /// <para><b>Warum sie in <c>SpeicherEngine</c> steht und nicht im Kern.</b> Die
    /// Abhaengigkeit laeuft <c>EPOS.Kern → SpeicherEngine</c>, nicht umgekehrt: Die
    /// Rastersuche (<see cref="SpeicherOptimierer"/>) ist die einzige Stelle des
    /// Bestands mit echter Rechenparallelitaet und liegt HIER, koennte eine Vorrichtung
    /// im Kern also gar nicht rufen. Umgekehrt sehen <c>EPOS.Kern</c> und
    /// <c>EPOS.UI.Daten</c> dieses Projekt und benutzen dieselbe Klasse — eine
    /// Vorrichtung, nicht zwei baugleiche.</para>
    ///
    /// <para><b>Hausregel.</b> Parallelitaet in <c>EPOS.Kern</c>, <c>SpeicherEngine</c>,
    /// <c>KiKern</c> und <c>EPOS.UI.Daten</c> laeuft ausschliesslich ueber diese
    /// Vorrichtung; ein nacktes <c>Parallel.For</c>, <c>Task.Run</c>, <c>new Thread</c>
    /// oder <c>AsParallel()</c> faellt im Waechter
    /// <c>EPOS.Kern.Tests/ParallelitaetWacheTests</c> auf.</para>
    /// </summary>
    public static class Kulturweitergabe
    {
        /// <summary>
        /// Die aufgeloeste Kultur des RUFENDEN Fadens als Momentaufnahme — der Einstieg
        /// in jede Weitergabe von Hand (etwa fuer einen eigenen Faden, den keine der
        /// Huellen unten abdeckt).
        /// </summary>
        public static Kulturstand Erfassen()
        {
            return new Kulturstand(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        }

        // ==================================================================
        //  Parallel-Huellen
        // ==================================================================

        /// <summary>
        /// <c>Parallel.For</c> mit der Kultur des Aufrufers auf jedem Arbeitspaket.
        /// </summary>
        /// <param name="vonEinschliesslich">Erster Index.</param>
        /// <param name="bisAusschliesslich">Erster Index, der NICHT mehr gerechnet wird.</param>
        /// <param name="optionen">
        /// Die Optionen des Laufs (Abbruchmarke, Hoechstgrad der Parallelitaet);
        /// <c>null</c> nimmt die Vorgabe von <c>Parallel.For</c>.
        /// </param>
        /// <param name="arbeit">Das Arbeitspaket je Index.</param>
        /// <remarks>
        /// Abbruch, Ausnahmen und Rueckgabewert sind unveraendert die von
        /// <c>Parallel.For</c>: Eine Abbruchmarke in <paramref name="optionen"/> loest
        /// wie bisher eine <see cref="OperationCanceledException"/> aus, Ausnahmen der
        /// Arbeitspakete kommen wie bisher als <see cref="AggregateException"/> heraus.
        /// </remarks>
        public static ParallelLoopResult For(
            int vonEinschliesslich, int bisAusschliesslich, ParallelOptions? optionen, Action<int> arbeit)
        {
            if (arbeit == null) throw new ArgumentNullException(nameof(arbeit));

            Kulturstand stand = Erfassen();
            Action<int> gehuellt = index =>
            {
                using (stand.Auftragen()) arbeit(index);
            };

            return optionen == null
                ? Parallel.For(vonEinschliesslich, bisAusschliesslich, gehuellt)
                : Parallel.For(vonEinschliesslich, bisAusschliesslich, optionen, gehuellt);
        }

        /// <summary>
        /// <c>Parallel.ForEach</c> mit der Kultur des Aufrufers auf jedem Arbeitspaket.
        /// </summary>
        /// <typeparam name="T">Typ der Elemente.</typeparam>
        /// <param name="quelle">Die Elemente.</param>
        /// <param name="optionen">Wie bei <see cref="For"/>; <c>null</c> nimmt die Vorgabe.</param>
        /// <param name="arbeit">Das Arbeitspaket je Element.</param>
        public static ParallelLoopResult ForEach<T>(
            IEnumerable<T> quelle, ParallelOptions? optionen, Action<T> arbeit)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            if (arbeit == null) throw new ArgumentNullException(nameof(arbeit));

            Kulturstand stand = Erfassen();
            Action<T> gehuellt = element =>
            {
                using (stand.Auftragen()) arbeit(element);
            };

            return optionen == null
                ? Parallel.ForEach(quelle, gehuellt)
                : Parallel.ForEach(quelle, optionen, gehuellt);
        }

        // ==================================================================
        //  Task.Run-Huellen
        // ==================================================================

        /// <summary>
        /// <c>Task.Run</c> mit der Kultur des Aufrufers auf dem Arbeitsfaden.
        /// </summary>
        /// <param name="arbeit">Die Arbeit.</param>
        /// <param name="abbruch">Abbruchmarke wie bei <c>Task.Run</c>.</param>
        public static Task Starten(Action arbeit, CancellationToken abbruch = default)
        {
            if (arbeit == null) throw new ArgumentNullException(nameof(arbeit));

            Kulturstand stand = Erfassen();
            return Task.Run(() =>
            {
                using (stand.Auftragen()) arbeit();
            }, abbruch);
        }

        /// <summary>
        /// <c>Task.Run</c> mit Ergebnis und mit der Kultur des Aufrufers auf dem
        /// Arbeitsfaden.
        /// </summary>
        /// <typeparam name="T">Typ des Ergebnisses.</typeparam>
        /// <param name="arbeit">Die Arbeit.</param>
        /// <param name="abbruch">Abbruchmarke wie bei <c>Task.Run</c>.</param>
        public static Task<T> Starten<T>(Func<T> arbeit, CancellationToken abbruch = default)
        {
            if (arbeit == null) throw new ArgumentNullException(nameof(arbeit));

            Kulturstand stand = Erfassen();
            return Task.Run(() =>
            {
                using (stand.Auftragen()) return arbeit();
            }, abbruch);
        }

        /// <summary>
        /// <c>Task.Run</c> fuer eine bereits asynchrone Arbeit
        /// (<c>Task.Run(async () =&gt; …)</c>), mit der Kultur des Aufrufers.
        /// </summary>
        /// <param name="arbeit">Die Arbeit; ihre Aufgabe wird ausgepackt wie bei <c>Task.Run</c>.</param>
        /// <param name="abbruch">Abbruchmarke wie bei <c>Task.Run</c>.</param>
        /// <remarks>
        /// Gesetzt und zurueckgestellt wird um den SYNCHRONEN Anlauf herum, also auf
        /// genau dem Faden, auf dem beides gehoert. Ueber die <c>await</c>-Stellen
        /// hinweg traegt .NET die gesetzte Kultur von selbst weiter — sie ist nach dem
        /// Setzen Teil des <c>ExecutionContext</c> dieses Ablaufs.
        /// </remarks>
        public static Task StartenAsync(Func<Task> arbeit, CancellationToken abbruch = default)
        {
            if (arbeit == null) throw new ArgumentNullException(nameof(arbeit));

            Kulturstand stand = Erfassen();
            return Task.Run(() =>
            {
                Kulturstand.Halter halter = stand.Auftragen();
                try { return arbeit(); }
                finally { halter.Dispose(); }
            }, abbruch);
        }
    }

    /// <summary>
    /// Die Momentaufnahme einer Kultur — was <see cref="Kulturweitergabe.Erfassen"/>
    /// liefert und was <see cref="Auftragen"/> auf einen Faden legt.
    /// </summary>
    public sealed class Kulturstand
    {
        private readonly CultureInfo _kultur;
        private readonly CultureInfo _oberflaeche;

        internal Kulturstand(CultureInfo kultur, CultureInfo oberflaeche)
        {
            _kultur = kultur ?? CultureInfo.InvariantCulture;
            _oberflaeche = oberflaeche ?? CultureInfo.InvariantCulture;
        }

        /// <summary>Die Rechenkultur — Zahlen-, Datums- und Sortierregeln.</summary>
        public CultureInfo Kultur { get { return _kultur; } }

        /// <summary>Die Anzeigekultur — sie waehlt die Ressourcensprache.</summary>
        public CultureInfo Oberflaeche { get { return _oberflaeche; } }

        /// <summary>
        /// Legt beide Werte auf den AKTUELLEN Faden und gibt den Halter zurueck, der sie
        /// in <c>Dispose</c> wieder auf den Vorstand zurueckstellt.
        /// </summary>
        public Halter Auftragen()
        {
            return new Halter(_kultur, _oberflaeche);
        }

        /// <summary>
        /// Der Rueckgabewert von <see cref="Auftragen"/> — stellt in <c>Dispose</c> den
        /// Vorstand des Fadens wieder her.
        /// </summary>
        public readonly struct Halter : IDisposable
        {
            // Nullbar, weil ein default(Halter) - den niemand baut, den die Sprache aber
            // zulaesst - sonst in Dispose mit einem null-Wert zuschlagen wuerde.
            private readonly CultureInfo? _vorherKultur;
            private readonly CultureInfo? _vorherOberflaeche;

            internal Halter(CultureInfo kultur, CultureInfo oberflaeche)
            {
                _vorherKultur = CultureInfo.CurrentCulture;
                _vorherOberflaeche = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentCulture = kultur;
                CultureInfo.CurrentUICulture = oberflaeche;
            }

            /// <summary>Stellt den Vorstand des Fadens wieder her.</summary>
            public void Dispose()
            {
                if (_vorherKultur != null) CultureInfo.CurrentCulture = _vorherKultur;
                if (_vorherOberflaeche != null) CultureInfo.CurrentUICulture = _vorherOberflaeche;
            }
        }
    }
}
