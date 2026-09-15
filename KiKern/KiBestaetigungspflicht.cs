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
    /// <b>Seit dem 14.09.2026 gibt es keinen Schalter mehr.</b> Bis dahin konnte ein
    /// Befehlszeilenschalter (<c>/ki-feldsicherung-aus</c>) die Feldbestaetigung der
    /// Formularaktionen aufheben - vorgesehen als Entwicklerkanal (Fachkonzept 11.5).
    /// Der Auftraggeber hat ihn abbestellt: „eine Bestaetigung was gesetzt wird sollte
    /// immer erscheinen". <see cref="KiFeldsicherung"/> ist damit ersatzlos entfallen,
    /// und diese Klasse antwortet genau das, was der Riegel sagt.
    /// </para>
    /// <para>
    /// <b>Warum die Klasse trotzdem bleibt.</b> Sie ist die EINE Stelle, die drei
    /// Aufrufer gemeinsam fragen; die Begruendung dafuer steht oben und haengt nicht am
    /// Schalter. Kaeme je wieder eine zweite Bedingung hinzu, stuende sie hier - und
    /// nicht dreimal verteilt.
    /// </para>
    /// </remarks>
    public static class KiBestaetigungspflicht
    {
        /// <summary>
        /// Braucht diese Aktion die ausdrueckliche Bestaetigung des Anwenders?
        /// </summary>
        /// <remarks>
        /// Deckt auch <c>null</c> ab: Der Riegel antwortet dort mit <c>false</c>.
        /// </remarks>
        public static bool Gilt(KiAktion? aktion) => KiRiegel.BrauchtBestaetigung(aktion);

        /// <summary>Dieselbe Frage fuer einen gepruefen Aufruf.</summary>
        public static bool Gilt(KiAufruf? aufruf) => Gilt(aufruf?.Aktion);
    }
}
