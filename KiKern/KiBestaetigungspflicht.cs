using System;

namespace KiKern
{
    /// <summary>
    /// Die EINE Stelle, an der entschieden wird, ob ein Aufruf die ausdrueckliche
    /// Bestaetigung des Anwenders braucht (Fachkonzept 11.5, Umsetzungspaket F4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum es diese Klasse gibt und die Frage nicht in <see cref="KiRiegel"/> steht.</b>
    /// Der Riegel beantwortet die Frage „braucht diese STUFE eine Bestaetigung?" - und er
    /// beantwortet sie als Konstante, ohne Schalter und ohne Namensliste
    /// (<c>KiRiegel.cs:19-24</c>). Genau so soll es bleiben: Waere die abschaltbare
    /// Feldsicherung dort eingebaut, stuende im Riegel wieder etwas, das sich zur Laufzeit
    /// verstellen laesst. Die Feldsicherung ist deshalb eine ZWEITE Frage, die auf die
    /// Antwort des Riegels aufsetzt - und sie kann sie nur einschraenken, nie erweitern.
    /// </para>
    /// <para>
    /// <b>Warum an EINER Stelle.</b> Im Anwendungsprojekt fragen drei Stellen nach der
    /// Bestaetigungspflicht: die Werkzeugrunde (<c>KiChatService</c>, sie entscheidet, ob
    /// ueberhaupt gefragt wird), die Vorbereitung und der Lauf (<c>KiAusfuehrer</c>, er
    /// verlangt die eingeloeste Freigabe). Liefen die drei auseinander, entstuende der
    /// schlimmste denkbare Zustand: Der Chat fragt nicht, der Ausfuehrer verlangt aber eine
    /// Freigabe - oder umgekehrt. Sie rufen deshalb alle diese Methode.
    /// </para>
    /// <para>
    /// <b>Die Reichweite des Schalters, in einer Zeile.</b> Er wirkt AUSSCHLIESSLICH auf
    /// Aktionen mit <see cref="KiAktion.Formularaktion"/>. Fuer jede gewoehnliche
    /// Schreibaktion (<c>kostenposition_setzen</c> und alles andere der Stufe 2) bleibt die
    /// Antwort unveraendert die des Riegels - unabhaengig davon, ob die Feldsicherung an
    /// oder aus ist. Das ist keine Absichtserklaerung, sondern am Ausdruck unten ablesbar:
    /// Ist <c>Formularaktion</c> falsch, kommt <see cref="KiFeldsicherung"/> gar nicht zum
    /// Tragen.
    /// </para>
    /// </remarks>
    public static class KiBestaetigungspflicht
    {
        /// <summary>
        /// Braucht diese Aktion die ausdrueckliche Bestaetigung des Anwenders - unter
        /// Beruecksichtigung der Feldsicherung?
        /// </summary>
        /// <remarks>
        /// Die Reihenfolge der beiden Fragen ist Absicht: Erst der Riegel, dann - und nur
        /// dann - die Feldsicherung. So kann der Schalter niemals etwas
        /// bestaetigungspflichtig machen, was der Riegel durchlaesst, und niemals etwas
        /// oberhalb der Formularaktionen freistellen.
        /// </remarks>
        public static bool Gilt(KiAktion? aktion)
        {
            // Deckt auch den Fall aktion == null ab: der Riegel antwortet dort mit false.
            if (!KiRiegel.BrauchtBestaetigung(aktion)) return false;

            return KiFeldsicherung.Aktiv || !aktion!.Formularaktion;
        }

        /// <summary>Dieselbe Frage fuer einen gepruefen Aufruf.</summary>
        public static bool Gilt(KiAufruf? aufruf) => Gilt(aufruf?.Aktion);
    }
}
