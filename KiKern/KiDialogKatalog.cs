using System;
using System.Collections;
using System.Collections.Generic;

namespace KiKern
{
    /// <summary>
    /// Der Dialogkatalog: die abschliessende Liste der Masken, die der Assistent steuern
    /// darf (Fachkonzept 11.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Was hier nicht steht, gibt es fuer den Assistenten nicht - das ist dieselbe Regel
    /// wie beim <see cref="KiRegister"/>. Gefuellt wird der Katalog im Anwendungsprojekt
    /// (<c>Allgemein\KI\Dialoge\</c>), weil nur dort die Masken stehen; der Kern haelt die
    /// Verwaltung und die Bauartsperre.
    /// </para>
    /// <para>
    /// <b>Warum der Katalog unveraenderlich ist</b> und nicht wie das Aktionsregister
    /// nachtraeglich befuellt wird: Ein Katalog, dem zur Laufzeit eine Maske zuwachsen
    /// kann, waere genau der Weg, auf dem eine nicht freigegebene Maske doch noch
    /// steuerbar wuerde. Der Bestand deklariert einmal beim Start - danach steht die Liste.
    /// </para>
    /// <para>
    /// <b>Nachschlag ohne Ruecksicht auf Gross-/Kleinschreibung.</b> Der Maskenname kommt
    /// als Parameterwert aus einem Modellaufruf; ob es „Form_PV" oder „form_pv" schreibt,
    /// darf ueber Treffer und Fehltreffer nicht entscheiden. Zwei Katalogeintraege, die
    /// sich nur in der Schreibweise unterscheiden, werden deshalb schon hier abgewiesen.
    /// </para>
    /// </remarks>
    public sealed class KiDialogKatalog : IEnumerable<KiDialog>
    {
        private readonly List<KiDialog> _reihenfolge = new List<KiDialog>();
        private readonly Dictionary<string, KiDialog> _nachName =
            new Dictionary<string, KiDialog>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Legt den Katalog aus den deklarierten Masken an.</summary>
        public KiDialogKatalog(params KiDialog[] dialoge)
            : this((IEnumerable<KiDialog>)(dialoge ?? Array.Empty<KiDialog>()))
        {
        }

        /// <summary>Legt den Katalog aus den deklarierten Masken an.</summary>
        public KiDialogKatalog(IEnumerable<KiDialog> dialoge)
        {
            if (dialoge == null) throw new ArgumentNullException(nameof(dialoge));

            foreach (KiDialog d in dialoge)
            {
                if (d == null)
                    throw new ArgumentException("Der Katalog nimmt keine leeren Eintraege auf.", nameof(dialoge));
                if (_nachName.ContainsKey(d.Maskenname))
                    throw new ArgumentException(
                        "Die Maske '" + d.Maskenname + "' ist bereits im Katalog.", nameof(dialoge));

                // ZWEITE LINIE zur Positivliste (Fachkonzept 11.3). Der Konstruktor von
                // KiDialogKnopf laesst einen Loeschknopf gar nicht erst entstehen; diese
                // Schleife prueft es noch einmal fuer den ganzen Katalog. Sie ist heute
                // nicht erreichbar - und genau deshalb steht sie hier: Wird die Sperre am
                // Knopf jemals gelockert, faellt es an der Stelle auf, die den Katalog
                // zusammensetzt, und nicht erst beim Anwender.
                foreach (KiDialogKnopf k in d.Knoepfe)
                    if (KiDialogKnopf.IstLoeschbezeichnung(k.Name) ||
                        KiDialogKnopf.IstLoeschbezeichnung(k.Controlpfad))
                        throw new ArgumentException(
                            "Die Maske '" + d.Maskenname + "' fuehrt mit '" + k.Name +
                            "' einen Loeschknopf; Loeschen ist nicht steuerbar (Fachkonzept 1.2/11.7).",
                            nameof(dialoge));

                _nachName.Add(d.Maskenname, d);
                _reihenfolge.Add(d);
            }
        }

        /// <summary>Alle Masken in Deklarationsreihenfolge.</summary>
        public IReadOnlyList<KiDialog> Alle => _reihenfolge;

        /// <summary>Zahl der deklarierten Masken.</summary>
        public int Anzahl => _reihenfolge.Count;

        /// <summary>Kennt der Katalog diese Maske?</summary>
        public bool Kennt(string? maskenname) => maskenname != null && _nachName.ContainsKey(maskenname);

        /// <summary>Liefert den Katalogeintrag, oder <c>null</c>.</summary>
        public KiDialog? Finde(string? maskenname)
            => maskenname != null && _nachName.TryGetValue(maskenname, out KiDialog? d) ? d : null;

        /// <summary>
        /// Die Maske, die ein Aufruf mit <paramref name="genannt"/> meint: beim Schluessel
        /// ODER beim Anzeigenamen. <c>null</c>, wenn keine oder mehrere passen - dann
        /// stehen die Kandidaten („Anzeigename (Schluessel)") in <paramref name="kandidaten"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Das Modell nennt die Maske, wie es sie liest.</b> Der Feldblock im Prompt und
        /// jede Absage fuehren den Anzeigenamen; ein Aufruf mit „Wärmepumpe im Projekt"
        /// meint <c>Form_WP_Anlage</c> und darf daran nicht scheitern.
        /// </para>
        /// <para>
        /// Es gilt die Namensregel <see cref="KiWahl.Treffer"/>: Schluessel
        /// buchstabengetreu, Schluessel gefaltet, Anzeigename gefaltet, eindeutiger Anfang,
        /// eindeutig enthaltener Teil. Trifft keine Stufe, werden Schluessel und Anzeigename
        /// zusaetzlich ohne Bindestriche verglichen („Photovoltaik Module").
        /// </para>
        /// </remarks>
        public KiDialog? Aufloesen(string? genannt, out IReadOnlyList<string> kandidaten)
        {
            kandidaten = Array.Empty<string>();

            string roh = (genannt ?? "").Trim();
            if (roh.Length == 0) return null;

            if (_nachName.TryGetValue(roh, out KiDialog? direkt)) return direkt;

            var eintraege = new List<KiWahleintrag>(_reihenfolge.Count);
            foreach (KiDialog d in _reihenfolge) eintraege.Add(new KiWahleintrag(d.Maskenname, d.Anzeigename));

            KiWahltreffer treffer = KiWahl.Treffer(eintraege, roh);
            if (treffer.Eindeutig) return _reihenfolge[treffer.Stelle];
            if (treffer.Mehrdeutig)
            {
                kandidaten = treffer.Kandidaten;
                return null;
            }

            string ohneStrich = OhneBindestrich(roh);
            if (ohneStrich.Length == 0) return null;

            KiDialog? gefunden = null;
            var mehrere = new List<string>();
            foreach (KiDialog d in _reihenfolge)
            {
                if (OhneBindestrich(d.Maskenname) != ohneStrich && OhneBindestrich(d.Anzeigename) != ohneStrich)
                    continue;
                mehrere.Add(d.Anzeigename + " (" + d.Maskenname + ")");
                gefunden ??= d;
            }

            if (mehrere.Count == 1) return gefunden;
            if (mehrere.Count > 1) kandidaten = mehrere;
            return null;
        }

        /// <summary>Die Vergleichsform von <see cref="KiWahl.Falte"/>, zusaetzlich ohne Bindestriche.</summary>
        private static string OhneBindestrich(string? text)
            => KiWahl.Falte(text).Replace("-", "").Replace("\u2010", "").Replace("\u2011", "");

        /// <summary>
        /// Die Namen aller Masken, alphabetisch - fuer die Aufzaehlung in
        /// <c>dialog_lesen</c> und fuer die Ablehnung einer nicht freigegebenen Maske.
        /// </summary>
        public IReadOnlyList<string> Maskennamen()
        {
            var namen = new List<string>();
            foreach (KiDialog d in _reihenfolge) namen.Add(d.Maskenname);
            namen.Sort(StringComparer.Ordinal);
            return namen;
        }

        /// <inheritdoc/>
        public IEnumerator<KiDialog> GetEnumerator() => _reihenfolge.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
