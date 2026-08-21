using System;
using System.Threading;

namespace KiKern
{
    /// <summary>
    /// Die Feldsicherung: die zusaetzliche Bestaetigung vor jeder Feldsetzung
    /// (Fachkonzept 11.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ein Startzustand, kein Betriebsmodus.</b> Die Sicherung ist an, solange niemand
    /// sie beim Programmstart abgeschaltet hat (Befehlszeilenschalter
    /// <c>/ki-feldsicherung-aus</c>, Abnahme 20.08.2026). <see cref="Abschalten"/> wirkt
    /// GENAU EINMAL, und es gibt keinen Weg zurueck: Ein Wiedereinschalten zur Laufzeit
    /// haette den Schalter zu einem Betriebsmodus gemacht, den irgendeine Stelle im
    /// Programm hin- und herlegt - und damit zu etwas, worauf sich der Anwender nicht mehr
    /// verlassen kann. Wer die Sicherung zurueckhaben will, startet das Programm neu.
    /// </para>
    /// <para>
    /// <b>Warum hier ein Schalter erlaubt ist, wo <see cref="KiRiegel"/> keinen duldet.</b>
    /// Er wirkt AUSSCHLIESSLICH auf die Feldbestaetigung der Formularaktionen
    /// (<see cref="KiAktion.Formularaktion"/>). Die Bestaetigung der DB-Schreibaktionen und
    /// die Sperre der Stufe 3 bleiben in jedem Fall bestehen - dort gilt die Begruendung
    /// „Konstante, keine Einstellung" unveraendert. Hinter der Feldsicherung stehen zwei
    /// weitere Linien: die Knopfpruefung der Maske und, sobald etwas in die Datenbank soll,
    /// die unabschaltbare Bestaetigung der Stufe 2 (Fachkonzept 11.5).
    /// </para>
    /// <para>
    /// <b>Abgeschaltet heisst sichtbar abgeschaltet.</b> Der Zustand bleibt nicht
    /// stillschweigend: Das Chatfenster traegt dauerhaft <see cref="KiTexte.FeldsicherungAus"/>,
    /// und jede Protokollzeile fuehrt <see cref="KiTexte.FeldsicherungVermerk"/> mit. Beides
    /// steht hier als fertiger Text bereit, damit nicht jede Aufrufstelle ihren eigenen
    /// Wortlaut erfindet.
    /// </para>
    /// </remarks>
    public static class KiFeldsicherung
    {
        /// <summary>
        /// Der Grund der Abschaltung - zugleich der Zustand selbst.
        /// </summary>
        /// <remarks>
        /// EIN Feld statt Wahrheitswert und Text: So kann es keinen Zwischenzustand geben,
        /// in dem die Sicherung schon aus ist, der Grund aber noch fehlt. Der Uebergang
        /// laeuft ueber <c>Interlocked.CompareExchange</c> von <c>null</c> auf den Grund -
        /// genau einmal, und es gibt im ganzen Kern keine Stelle, die wieder <c>null</c>
        /// schreibt.
        /// </remarks>
        private static string? _grund;

        /// <summary>Ist die Feldsicherung aktiv? Standard: ja.</summary>
        public static bool Aktiv => Volatile.Read(ref _grund) == null;

        /// <summary>
        /// Der Grund der Abschaltung im Klartext; leer, solange die Sicherung aktiv ist.
        /// </summary>
        public static string Grund => Volatile.Read(ref _grund) ?? "";

        /// <summary>
        /// Schaltet die Feldsicherung ab - einmalig und unwiderruflich fuer diesen
        /// Programmlauf.
        /// </summary>
        /// <param name="grund">
        /// Woher die Abschaltung kommt, z. B. „Befehlszeilenschalter /ki-feldsicherung-aus".
        /// Der Grund ist Pflicht: Er steht spaeter im Chat und im Protokoll, und eine
        /// Abschaltung ohne nachvollziehbare Herkunft soll es nicht geben.
        /// </param>
        /// <returns>
        /// <c>true</c>, wenn dieser Aufruf die Sicherung abgeschaltet hat; <c>false</c>,
        /// wenn sie bereits abgeschaltet war - dann bleibt der ERSTE Grund stehen.
        /// </returns>
        public static bool Abschalten(string grund)
        {
            // Die Argumentpruefung steht VOR dem Zustand: Ein Aufruf ohne Grund ist ein
            // Programmierfehler und soll auch dann auffallen, wenn die Sicherung ohnehin
            // schon aus ist.
            if (string.IsNullOrWhiteSpace(grund))
                throw new ArgumentException(
                    "Das Abschalten der Feldsicherung braucht einen Grund im Klartext.", nameof(grund));

            return Interlocked.CompareExchange(ref _grund, grund.Trim(), null) == null;
        }

        /// <summary>
        /// Der dauerhafte Hinweis im Chatfenster; leer, solange die Sicherung aktiv ist.
        /// </summary>
        public static string Chathinweis() => Aktiv ? "" : KiTexte.FeldsicherungAus;

        /// <summary>
        /// Der Vermerk fuer die Protokollzeile; leer, solange die Sicherung aktiv ist.
        /// </summary>
        public static string Protokollvermerk() => Aktiv ? "" : KiTexte.FeldsicherungVermerk;
    }
}
