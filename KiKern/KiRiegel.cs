using System;
using System.Globalization;

namespace KiKern
{
    /// <summary>
    /// Der Schutzstufen-Riegel: was ohne Bestaetigung laufen darf und was nicht
    /// (Fachkonzept 4.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum es diesen Riegel schon jetzt gibt.</b> Die Bestaetigungsschicht kommt erst
    /// mit Etappe 3. Bis dahin darf ausschliesslich <see cref="Schutzstufe.Lesen"/> laufen
    /// („lesen: sofort", Fachkonzept 4.1). Der Riegel steht deshalb IM CODE und nicht als
    /// Vorsatz: heute ist ohnehin nur Stufe 1 registriert, aber die erste Schreibaktion
    /// wuerde sonst ungebremst durchlaufen, sobald jemand sie einträgt.
    /// </para>
    /// <para>
    /// <b>Warum eine Konstante und keine Einstellung.</b> Eine umschaltbare Obergrenze
    /// waere genau der Schalter, den ein spaeterer Bequemlichkeitswunsch umlegt.
    /// <see cref="OhneBestaetigung"/> wird mit Etappe 3 angehoben - aber nur gemeinsam mit
    /// der Bestaetigungsschicht, die dann den Klick verlangt, nie allein. Die Ueberladung
    /// mit ausdruecklicher Obergrenze gibt es nur, damit die Tests beide Seiten der Grenze
    /// pruefen koennen.
    /// </para>
    /// </remarks>
    public static class KiRiegel
    {
        /// <summary>
        /// Hoechste Schutzstufe, die OHNE ausdrueckliche Bestaetigung ausgefuehrt werden
        /// darf. Bis Etappe 3: nur lesend.
        /// </summary>
        public const Schutzstufe OhneBestaetigung = Schutzstufe.Lesen;

        /// <summary>
        /// Hoechste Schutzstufe, die der Assistent UEBERHAUPT ausfuehren kann - dann aber
        /// nur mit Bestaetigung. Mit Etappe 3: bis einschliesslich
        /// <see cref="Schutzstufe.Schreiben"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Zwei Grenzen, nicht eine.</b> <see cref="OhneBestaetigung"/> beantwortet
        /// "was laeuft sofort", <see cref="HoechsteStufe"/> beantwortet "was gibt es
        /// ueberhaupt". Dazwischen liegt genau das Feld der Bestaetigungsschicht: Stufe 2
        /// laeuft, aber nur nach einem Klick.
        /// </para>
        /// <para>
        /// <b>Warum nicht EINE angehobene Grenze.</b> Waere mit Etappe 3 schlicht
        /// <see cref="OhneBestaetigung"/> auf <see cref="Schutzstufe.Schreiben"/> gehoben
        /// worden, liefe jede Schreibaktion OHNE Rueckfrage durch - genau der Zustand, den
        /// diese Etappe verhindert. Angehoben wird deshalb die andere Grenze. Die
        /// Werkzeugrunde fragt seither ausschliesslich <see cref="PruefeStufe(KiAufruf)"/>
        /// ab und traegt keine eigene Stufenangabe mehr; damit gibt es genau EINE
        /// Fundstelle, die sich mit Etappe 4 auf <see cref="Schutzstufe.Rechnen"/> hebt.
        /// </para>
        /// </remarks>
        public const Schutzstufe HoechsteStufe = Schutzstufe.Schreiben;

        /// <summary>
        /// Klartextgrund, warum die Aktion nicht laufen darf; <c>null</c>, wenn nichts
        /// dagegen spricht.
        /// </summary>
        public static string? Pruefe(KiAktion? aktion) => Pruefe(aktion, OhneBestaetigung);

        /// <summary>Wie <see cref="Pruefe(KiAktion)"/>, mit ausdruecklicher Obergrenze.</summary>
        public static string? Pruefe(KiAktion? aktion, Schutzstufe hoechsteFreigegebene)
        {
            if (aktion == null) return null;
            if (aktion.Stufe <= hoechsteFreigegebene) return null;

            return string.Format(CultureInfo.CurrentCulture, KiTexte.RiegelZu,
                                 aktion.Name, KiTexte.Stufe(aktion.Stufe));
        }

        /// <summary>Klartextgrund fuer einen gepruefen Aufruf.</summary>
        public static string? Pruefe(KiAufruf? aufruf) => Pruefe(aufruf?.Aktion, OhneBestaetigung);

        /// <summary>Klartextgrund fuer einen gepruefen Aufruf, mit ausdruecklicher Obergrenze.</summary>
        public static string? Pruefe(KiAufruf? aufruf, Schutzstufe hoechsteFreigegebene)
            => Pruefe(aufruf?.Aktion, hoechsteFreigegebene);

        /// <summary>
        /// Klartextgrund, warum diese Aktion in dieser Ausbaustufe UEBERHAUPT nicht laufen
        /// darf - auch nicht mit Bestaetigung. <c>null</c> = sie ist freigegeben.
        /// </summary>
        /// <remarks>
        /// Das ist der Riegel der Werkzeugrunde seit Etappe 3. Er trifft heute die
        /// Rechenaktionen der Stufe 3 (kommen mit Etappe 4); Stufe 2 laesst er durch und
        /// uebergibt an die Bestaetigungsschicht
        /// (<see cref="BrauchtBestaetigung(KiAktion)"/>).
        /// </remarks>
        public static string? PruefeStufe(KiAktion? aktion)
        {
            if (aktion == null) return null;
            if (aktion.Stufe <= HoechsteStufe) return null;

            return string.Format(CultureInfo.CurrentCulture, KiTexte.RiegelStufeGesperrt,
                                 aktion.Name, KiTexte.Stufe(aktion.Stufe));
        }

        /// <summary>Klartextgrund fuer einen gepruefen Aufruf.</summary>
        public static string? PruefeStufe(KiAufruf? aufruf) => PruefeStufe(aufruf?.Aktion);

        /// <summary>Braucht diese Aktion die ausdrueckliche Bestaetigung des Anwenders?</summary>
        /// <remarks>
        /// Die Frage haengt an der STUFE, nicht an einer Namensliste - eine neue
        /// Schreibaktion ist damit ohne Zutun mit erfasst.
        /// </remarks>
        public static bool BrauchtBestaetigung(KiAktion? aktion)
            => aktion != null && aktion.Stufe > OhneBestaetigung;

        /// <summary>Braucht dieser Aufruf die ausdrueckliche Bestaetigung des Anwenders?</summary>
        public static bool BrauchtBestaetigung(KiAufruf? aufruf) => BrauchtBestaetigung(aufruf?.Aktion);

        /// <summary>Darf diese Aktion ohne Bestaetigung laufen?</summary>
        public static bool DarfDirektLaufen(KiAktion? aktion) => Pruefe(aktion) == null;

        /// <summary>Darf dieser Aufruf ohne Bestaetigung laufen?</summary>
        public static bool DarfDirektLaufen(KiAufruf? aufruf) => Pruefe(aufruf) == null;
    }

    /// <summary>
    /// Der Rundendeckel einer Anwenderaeusserung (Fachkonzept 3.3, Festlegung 5).
    /// </summary>
    /// <remarks>
    /// Hoechstens <see cref="KiWerkzeuge.Rundendeckel"/> Modellrunden: Aufruf, Ergebnis,
    /// Antwort - beziehungsweise Aufruf, Korrektur, Antwort. Danach Abbruch mit Klartext.
    /// Der Deckel schuetzt vor zwei Dingen zugleich: vor der Schleife, in der ein Modell
    /// dieselbe Aktion immer wieder ruft, und vor dem Tageslimit, das eine einzige Frage
    /// sonst aufbrauchen koennte.
    /// </remarks>
    public sealed class KiRunden
    {
        /// <summary>Legt einen Zaehler mit dem Regeldeckel an.</summary>
        public KiRunden() : this(KiWerkzeuge.Rundendeckel) { }

        /// <summary>Legt einen Zaehler mit eigenem Deckel an (Tests, Sonderfaelle).</summary>
        public KiRunden(int deckel)
        {
            if (deckel < 1)
                throw new ArgumentOutOfRangeException(nameof(deckel), "Mindestens eine Runde.");
            Deckel = deckel;
        }

        /// <summary>Hoechstzahl der Runden.</summary>
        public int Deckel { get; }

        /// <summary>Bereits begonnene Runden.</summary>
        public int Verbraucht { get; private set; }

        /// <summary>Ist noch eine Runde frei?</summary>
        public bool DarfWeiter => Verbraucht < Deckel;

        /// <summary>
        /// Beginnt die naechste Runde. Liefert <c>false</c>, wenn der Deckel erreicht ist -
        /// dann wurde auch nichts verbraucht.
        /// </summary>
        public bool Beginne()
        {
            if (!DarfWeiter) return false;
            Verbraucht++;
            return true;
        }

        /// <summary>Klartext fuer den Abbruch nach der letzten Runde.</summary>
        public string Abbruchtext()
            => string.Format(CultureInfo.CurrentCulture, KiTexte.RundendeckelErreicht, Deckel);

        /// <inheritdoc/>
        public override string ToString() => Verbraucht + "/" + Deckel;
    }
}
