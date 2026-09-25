using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Warum ein Platzhalter beim Füllen stehen blieb (Konzept Berichtsvorlagen 4.10).</summary>
    public enum Fuellbefundart
    {
        /// <summary>Den Schlüssel kennt der Katalog nicht (auch doppelte Klammern ohne gültige Form).</summary>
        Unbekannt,

        /// <summary>Blöcke, Stand- und Gebäudewerte, Tabellen, Bilder, Schalter: in dieser Fassung noch nicht gefüllt.</summary>
        NichtUnterstuetzt,

        /// <summary>Die Art passt nicht an die Stelle (Kapitel im Satz, Liste in der Kopfzeile …).</summary>
        FalscheStelle,

        /// <summary>Dasselbe Kapitel steht schon an einer früheren Stelle — nur die erste wird gefüllt (Konzept 5.3).</summary>
        Doppelt,
    }

    /// <summary>
    /// Ein Platzhalter, der nach dem Füllen stehen blieb — gelb hervorgehoben im Bericht, hier mit
    /// Normalform, menschlichem Fundort und Grund für die Laufmeldung.
    /// </summary>
    public sealed class Fuellbefund
    {
        /// <summary>Legt den Befund an.</summary>
        public Fuellbefund(string normalform, string fundort, Fuellbefundart art, string grund)
        {
            Normalform = normalform ?? "";
            Fundort = fundort ?? "";
            Art = art;
            Grund = grund ?? "";
        }

        /// <summary>Die kanonische Schreibweise, etwa <c>{{kapitel.inhalt}}</c> (<see cref="Platzhalter.Normalform"/>).</summary>
        public string Normalform { get; }

        /// <summary>Wo er steht, etwa „Rumpf, Absatz 7: „{{kapitel.inhalt}}““ — in der Sprache des Berichts.</summary>
        public string Fundort { get; }

        /// <summary>Warum er stehen blieb.</summary>
        public Fuellbefundart Art { get; }

        /// <summary>Der Grund als Text in der Sprache des Berichts.</summary>
        public string Grund { get; }

        /// <inheritdoc/>
        public override string ToString() { return Normalform + " — " + Grund + " — " + Fundort; }
    }

    /// <summary>
    /// <b>Das Ergebnis eines Füllens</b> (<see cref="WordVorlagenfueller"/>, Konzept Berichtsvorlagen
    /// 4.10, 6.1, 6.7): was ersetzt wurde, was leer blieb, was stehen blieb und was die Engine an
    /// der Vorlage entfernt oder angelegt hat. Der Bericht entsteht in jedem dieser Fälle; die
    /// Liste ist die Grundlage der Laufmeldung.
    /// </summary>
    public sealed class Fuellergebnis
    {
        private readonly List<Fuellbefund> _unbekannte = new List<Fuellbefund>();
        private readonly Dictionary<string, int> _leere = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _stellen = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _hinweise = new List<string>();
        private readonly List<string> _warnungen = new List<string>();
        private readonly List<string> _fehler = new List<string>();

        internal Fuellergebnis(string zieldatei, bool englisch)
        {
            Zieldatei = zieldatei;
            Englisch = englisch;
        }

        /// <summary>Die geschriebene Berichtsdatei.</summary>
        public string Zieldatei { get; }

        /// <summary>In welcher Sprache die Texte des Ergebnisses stehen.</summary>
        public bool Englisch { get; }

        /// <summary>Wie viele Platzhalter ersetzt wurden — auch mit Leerwert, auch Inhaltssteuerelemente und Kapitel.</summary>
        public int Ersetzt { get; internal set; }

        /// <summary>
        /// Die Platzhalter, die stehen blieben (gelb hervorgehoben): unbekannte Schlüssel, noch nicht
        /// unterstützte Blöcke und Werte, Arten an der falschen Stelle — je Fundstelle ein Eintrag.
        /// </summary>
        public IReadOnlyList<Fuellbefund> Unbekannte { get { return _unbekannte; } }

        /// <summary>Je Schlüssel, wie oft er ohne Wert blieb (dann steht sein Leerwert, nie 0).</summary>
        public IReadOnlyDictionary<string, int> Leere { get { return _leere; } }

        /// <summary>
        /// Je Schlüssel, an wie vielen Stellen er aufgelöst wurde — mit und ohne Wert. Zusammen mit
        /// <see cref="Leere"/> ergibt das die zusammengefasste Laufmeldung „leer bei 1 von 3 Stellen“
        /// (Konzept 4.10); mit den Blöcken ab BV-E4 zählt jede Wiederholung als eigene Stelle.
        /// </summary>
        public IReadOnlyDictionary<string, int> Stellen { get { return _stellen; } }

        /// <summary>
        /// Wie viele Kommentare die Vorlage trug — die Engine entfernt alle (Konzept 6.7). Gezählt wird
        /// wie im Prüfer (<see cref="Vorlagenteile.Kommentarzahl"/>, <see cref="Pruefbefund.Kommentare"/>).
        /// </summary>
        public int EntfernteKommentare { get; internal set; }

        /// <summary>Trug die Vorlage keinen einzigen Platzhalter (dann stehen die Kapitel am Ende, Konzept 6.1)?</summary>
        public bool OhnePlatzhalter { get; internal set; }

        /// <summary>
        /// Die Stelle jedes Kapitels im gefüllten Bericht (Konzept 11 Nr. 3): je
        /// <see cref="Berichtskapitel.Stellenschluessel"/> die Überschrift vor dem Anker — der Kapitelkopf
        /// der Vorlage, sonst die eigene Überschrift des Bausteins —, <c>null</c> = nicht im Bericht.
        /// Dieselben Stellen nennt die Anhang-E-Checkliste des Berichts.
        /// </summary>
        public IReadOnlyDictionary<string, string> Kapitelstellen { get; internal set; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Hinweise: angelegte Stile, entfernte Kommentare, der Verweis auf die Dokumentvorlage, Datumsfelder.</summary>
        public IReadOnlyList<string> Hinweise { get { return _hinweise; } }

        /// <summary>Warnungen: Vorlage ohne Platzhalter, entfernte Verknüpfungen, Ausnahmen bei Einzelwerten.</summary>
        public IReadOnlyList<string> Warnungen { get { return _warnungen; } }

        /// <summary>Fehler der Vorlage, bei denen der Platzhalter stehen blieb (Kapitel im Satz …).</summary>
        public IReadOnlyList<string> Fehler { get { return _fehler; } }

        /// <summary>
        /// Die Laufmeldung in einer Liste: Fehler, Warnungen, stehen gebliebene Platzhalter, leere
        /// Schlüssel („stamm.kennzahl.eff.jaz: leer (1×)“), Hinweise.
        /// </summary>
        public IReadOnlyList<string> Meldungen()
        {
            var zeilen = new List<string>();
            zeilen.AddRange(_fehler);
            zeilen.AddRange(_warnungen);
            zeilen.AddRange(_unbekannte.Select(b => b.ToString()));
            zeilen.AddRange(_leere.OrderBy(p => p.Key, StringComparer.Ordinal)
                                  .Select(p => WordVorlagentexte.F(Englisch, WordVorlagentexte.LEER, p.Key, p.Value)));
            zeilen.AddRange(_hinweise);
            return zeilen;
        }

        internal void Unbekannt(Fuellbefund befund) { _unbekannte.Add(befund); }

        internal void Leer(string schluessel)
        {
            _leere.TryGetValue(schluessel ?? "", out int zahl);
            _leere[schluessel ?? ""] = zahl + 1;
        }

        internal void Aufgeloest(string schluessel)
        {
            _stellen.TryGetValue(schluessel ?? "", out int zahl);
            _stellen[schluessel ?? ""] = zahl + 1;
        }

        internal void Hinweis(string text) { if (!_hinweise.Contains(text)) _hinweise.Add(text); }

        internal void Warnung(string text) { _warnungen.Add(text); }

        internal void Fehlermeldung(string text) { _fehler.Add(text); }
    }
}
