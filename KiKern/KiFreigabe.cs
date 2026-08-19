using System;
using System.Threading;

namespace KiKern
{
    /// <summary>
    /// Ausgang der Anwenderentscheidung ueber eine Freigabe (Fachkonzept 3.5, Punkt 3).
    /// </summary>
    public enum KiEntscheidung
    {
        /// <summary>Der Anwender hat noch nicht entschieden.</summary>
        Offen = 0,

        /// <summary>Der Anwender hat „Ausfuehren" geklickt.</summary>
        Erteilt = 1,

        /// <summary>Der Anwender hat „Abbrechen" geklickt.</summary>
        Abgelehnt = 2,

        /// <summary>Die Minute ist verstrichen, ohne dass jemand geklickt hat.</summary>
        Verfallen = 3,

        /// <summary>Der Lauf wurde abgebrochen (Fenster zu, Abbruchmarke).</summary>
        Abgebrochen = 4
    }

    /// <summary>
    /// Die Freigabe EINES Aufrufs - der Gegenstand, den der Anwender bestaetigt
    /// (Fachkonzept 3.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein Gegenstand und kein Wahrheitswert.</b> Ein <c>bool bestaetigt</c>
    /// haette drei Loecher: Er sagt nicht, WELCHER Aufruf bestaetigt wurde, er laesst sich
    /// zweimal verwenden, und er kennt kein Alter. Diese Freigabe schliesst alle drei:
    /// sie traegt den <see cref="Aufruf"/> mit sich (Vergleich per Verweis, nicht per
    /// Name), sie ist EINMALIG (<see cref="Verbrauchen"/>), und sie verfaellt nach
    /// <see cref="VerfallSekunden"/> Sekunden.
    /// </para>
    /// <para>
    /// <b>Ein Klick gilt fuer einen Aufruf</b> (Fachkonzept 3.5, Punkt 4). Es gibt keine
    /// Sammelfreigabe und kein „ab jetzt immer": Die Freigabe entsteht je Aufruf neu, und
    /// wer sie einmal verbraucht hat, bekommt sie nicht wieder.
    /// </para>
    /// <para>
    /// <b>Zwei Verfallgruende, nicht einer</b> (Fachkonzept 3.5, Punkt 5). Verworfen wird
    /// eine Vorschau, die aelter als eine Minute ist - UND eine, auf die inzwischen eine
    /// ANDERE Aktion gefolgt ist. Dafuer merkt sich die Freigabe die
    /// <see cref="Laufmarke"/>: den Stand des Aktionszaehlers bei ihrer Entstehung. Sonst
    /// bestaetigte der Anwender einen Zustand, den es nicht mehr gibt.
    /// </para>
    /// <para>
    /// <b>Die Uhr ist einstellbar.</b> Nicht aus Bequemlichkeit, sondern weil der
    /// Verfall sonst nur mit echtem Warten pruefbar waere. Der Aktionsharnisch rueckt die
    /// Zeit vor und weist damit nach, dass eine verfallene Freigabe NICHT mehr schreibt.
    /// </para>
    /// </remarks>
    public sealed class KiFreigabe
    {
        /// <summary>Lebensdauer einer Vorschau in Sekunden (Fachkonzept 3.5, Punkt 5).</summary>
        public const int VerfallSekunden = 60;

        private readonly Func<DateTime> _uhr;
        private int _stand;         // KiEntscheidung als int - Interlocked braucht int
        private int _verbraucht;    // 0 = frei, 1 = verbraucht

        private KiFreigabe(KiAufruf aufruf, string text, Func<DateTime> uhr,
                           TimeSpan frist, long laufmarke)
        {
            Aufruf = aufruf;
            Text = text;
            _uhr = uhr;
            Frist = frist;
            Laufmarke = laufmarke;
            Erzeugt = uhr();
            Verfaellt = Erzeugt + frist;
        }

        /// <summary>
        /// Legt eine offene Freigabe an. Der Text stammt aus <see cref="KiBestaetigung"/> -
        /// NIE aus Modelltext (Fachkonzept 3.5, Punkt 2).
        /// </summary>
        /// <param name="aufruf">Der gepruefte Aufruf, um den es geht.</param>
        /// <param name="bestaetigungstext">Der angezeigte Klartext.</param>
        /// <param name="uhr">Zeitquelle; <c>null</c> = <see cref="DateTime.Now"/>.</param>
        /// <param name="frist">Lebensdauer; <c>null</c> = <see cref="VerfallSekunden"/>.</param>
        /// <param name="laufmarke">Stand des Aktionszaehlers bei Entstehung.</param>
        public static KiFreigabe Erzeuge(KiAufruf aufruf, string bestaetigungstext,
                                         Func<DateTime>? uhr = null, TimeSpan? frist = null,
                                         long laufmarke = 0)
        {
            if (aufruf == null) throw new ArgumentNullException(nameof(aufruf));
            if (string.IsNullOrWhiteSpace(bestaetigungstext))
                throw new ArgumentException("Eine Freigabe ohne Bestaetigungstext gibt es nicht.",
                                            nameof(bestaetigungstext));

            TimeSpan f = frist ?? TimeSpan.FromSeconds(VerfallSekunden);
            if (f <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(frist), "Die Frist muss positiv sein.");

            return new KiFreigabe(aufruf, bestaetigungstext, uhr ?? (() => DateTime.Now), f, laufmarke);
        }

        // ==================================================================== Sicht

        /// <summary>Der Aufruf, fuer den diese Freigabe gilt - und nur fuer diesen.</summary>
        public KiAufruf Aufruf { get; }

        /// <summary>Der angezeigte Bestaetigungstext (aus <see cref="KiBestaetigung"/>).</summary>
        public string Text { get; }

        /// <summary>Zeitpunkt der Entstehung.</summary>
        public DateTime Erzeugt { get; }

        /// <summary>Zeitpunkt, ab dem die Freigabe wertlos ist.</summary>
        public DateTime Verfaellt { get; }

        /// <summary>Lebensdauer.</summary>
        public TimeSpan Frist { get; }

        /// <summary>Stand des Aktionszaehlers bei Entstehung (Fachkonzept 3.5, Punkt 5).</summary>
        public long Laufmarke { get; }

        /// <summary>Die Entscheidung des Anwenders.</summary>
        public KiEntscheidung Stand => (KiEntscheidung)Volatile.Read(ref _stand);

        /// <summary>Wurde die Freigabe bereits eingeloest?</summary>
        public bool Verbraucht => Volatile.Read(ref _verbraucht) != 0;

        /// <summary>Verbleibende Zeit; nie negativ.</summary>
        public TimeSpan Restzeit()
        {
            TimeSpan rest = Verfaellt - _uhr();
            return rest > TimeSpan.Zero ? rest : TimeSpan.Zero;
        }

        /// <summary>Ist die Minute verstrichen?</summary>
        public bool IstVerfallen() => _uhr() >= Verfaellt;

        // ================================================================ Entscheiden

        /// <summary>
        /// „Ausfuehren" wurde geklickt. Liefert <c>false</c>, wenn die Freigabe bereits
        /// entschieden ODER verfallen ist - dann gilt sie als verfallen.
        /// </summary>
        public bool Erteilen()
        {
            if (IstVerfallen()) { AlsVerfallenMarkieren(); return false; }
            return Setze(KiEntscheidung.Erteilt);
        }

        /// <summary>„Abbrechen" wurde geklickt.</summary>
        public bool Ablehnen() => Setze(KiEntscheidung.Abgelehnt);

        /// <summary>Der Lauf wurde abgebrochen (Abbruchmarke, Fenster geschlossen).</summary>
        public bool Abbrechen() => Setze(KiEntscheidung.Abgebrochen);

        /// <summary>Die Frist ist abgelaufen, ohne dass jemand geklickt hat.</summary>
        public bool AlsVerfallenMarkieren() => Setze(KiEntscheidung.Verfallen);

        private bool Setze(KiEntscheidung neu)
            => Interlocked.CompareExchange(ref _stand, (int)neu, (int)KiEntscheidung.Offen)
               == (int)KiEntscheidung.Offen;

        // ================================================================== Einloesen

        /// <summary>
        /// Klartextgrund, warum diese Freigabe NICHT zur Ausfuehrung berechtigt;
        /// <c>null</c> heisst: sie berechtigt.
        /// </summary>
        /// <param name="laufmarkeJetzt">Aktueller Stand des Aktionszaehlers.</param>
        public string? Pruefe(long laufmarkeJetzt)
        {
            if (Verbraucht) return KiTexte.FreigabeVerbraucht;

            KiEntscheidung stand = Stand;

            // Der Verfall geht VOR der Entscheidung: eine erteilte, aber inzwischen
            // abgelaufene Freigabe darf nicht mehr schreiben (Fachkonzept 3.5, Punkt 5).
            if (stand == KiEntscheidung.Verfallen || IstVerfallen())
            {
                AlsVerfallenMarkieren();
                return KiTexte.FreigabeVerfallen;
            }

            switch (stand)
            {
                case KiEntscheidung.Erteilt: break;
                case KiEntscheidung.Abgelehnt: return KiTexte.FreigabeAbgelehnt;
                case KiEntscheidung.Abgebrochen: return KiTexte.FreigabeAbgebrochen;
                default: return KiTexte.FreigabeOffen;
            }

            if (laufmarkeJetzt != Laufmarke) return KiTexte.FreigabeUeberholt;

            return null;
        }

        /// <summary>
        /// Loest die Freigabe EINMALIG ein. Rueckgabe <c>null</c> = eingeloest, sonst der
        /// Klartextgrund. Nach dem ersten Erfolg liefert jeder weitere Versuch einen Grund.
        /// </summary>
        public string? Verbrauchen(long laufmarkeJetzt)
        {
            string? grund = Pruefe(laufmarkeJetzt);
            if (grund != null) return grund;

            if (Interlocked.CompareExchange(ref _verbraucht, 1, 0) != 0)
                return KiTexte.FreigabeVerbraucht;

            return null;
        }

        /// <summary>Gehoert diese Freigabe zu genau diesem Aufruf?</summary>
        /// <remarks>
        /// Vergleich ueber den VERWEIS, nicht ueber den Namen: zwei Aufrufe derselben
        /// Aktion mit anderen Werten sind zwei verschiedene Vorgaenge, und die Freigabe des
        /// einen darf den anderen nie decken.
        /// </remarks>
        public bool GiltFuer(KiAufruf? aufruf) => aufruf != null && ReferenceEquals(aufruf, Aufruf);

        /// <inheritdoc/>
        public override string ToString()
            => Aufruf.Name + " [" + Stand + (Verbraucht ? ", verbraucht" : "") + "]";
    }
}
