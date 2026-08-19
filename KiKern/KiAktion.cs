using System;
using System.Collections.Generic;

namespace KiKern
{
    /// <summary>
    /// Eine benannte Aktion des Registers (Fachkonzept 3.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eine Deklaration, drei Verwendungen.</b> Aus genau diesem Objekt entstehen
    /// (a) das JSON-Schema fuer das Modell (<see cref="KiSchema"/>),
    /// (b) die Parameterpruefung in C# (<see cref="KiPruefung"/>) und
    /// (c) der Klartext fuer Bestaetigung und Protokoll (<see cref="KiBestaetigung"/>,
    /// <see cref="KiProtokoll"/>). Damit koennen sie nicht auseinanderlaufen.
    /// </para>
    /// <para>
    /// <b>Nur benannte Aktionen.</b> Es gibt kein generisches „SQL ausfuehren" und keinen
    /// Aufruf per Reflexion. Was hier nicht deklariert ist, kann der Assistent nicht tun.
    /// </para>
    /// <para>
    /// Die drei Delegaten laufen im ANWENDUNGSPROJEKT und duerfen dort Controller und
    /// Datenbank anfassen; der Kern kennt nur ihre Signatur. <see cref="Vorschau"/> und
    /// <see cref="Vorbedingung"/> schreiben NIE.
    /// </para>
    /// </remarks>
    public sealed class KiAktion
    {
        /// <summary>
        /// Deklariert eine Aktion.
        /// </summary>
        /// <param name="name">Sprachneutraler Schluessel, ASCII, hoechstens 64 Zeichen.</param>
        /// <param name="zweck">Eine Zeile Klartext - fuer das Modell UND fuer die Bestaetigung.</param>
        /// <param name="stufe">Schutzstufe nach Fachkonzept 4.1.</param>
        /// <param name="andockpunkt">Aufgerufene Bestandsmethode, z. B. <c>ProjektCtrl.ReadAll</c>. Nur fuers Protokoll.</param>
        /// <param name="parameter">Parameterdeklaration in Anzeigereihenfolge.</param>
        /// <param name="ausfuehren">Der eigentliche Aufruf des Bestands.</param>
        /// <param name="vorbedingung">Liefert den Klartextgrund, warum es GERADE NICHT geht - sonst <c>null</c>.</param>
        /// <param name="vorschau">„Ich wuerde X tun" - schreibt nichts. Nur fuer Stufe 2 und 3 noetig.</param>
        /// <param name="wirkung">Ein Satz „was danach anders ist" fuer die Bestaetigung.</param>
        /// <param name="umkehrbar">
        /// Laesst sich die Aenderung als neue, ebenfalls bestaetigungspflichtige Aktion
        /// zurueckschreiben? Steht woertlich in der Bestaetigung (Fachkonzept 4.4, Punkt 3).
        /// </param>
        public KiAktion(string name,
                        string zweck,
                        Schutzstufe stufe,
                        string andockpunkt,
                        IReadOnlyList<KiParameter>? parameter = null,
                        Func<KiAufruf, KiErgebnis>? ausfuehren = null,
                        Func<KiAufruf, string?>? vorbedingung = null,
                        Func<KiAufruf, string>? vorschau = null,
                        string? wirkung = null,
                        bool umkehrbar = false)
        {
            if (!KiName.IstGueltig(name))
                throw new ArgumentException(
                    "Aktionsname '" + name + "' ist nicht zulaessig (erlaubt: a-z, 0-9, _; hoechstens 64 Zeichen).",
                    nameof(name));
            if (string.IsNullOrWhiteSpace(zweck))
                throw new ArgumentException("Eine Aktion braucht einen Zweck in einem Satz.", nameof(zweck));

            // VORSCHAUPFLICHT ab Stufe 2 (Fachkonzept 3.5, Punkt 1). Eine Aktion, die
            // Daten veraendert, MUSS vorher sagen koennen, was sie veraendern wuerde -
            // sonst bestaetigt der Anwender eine Ueberschrift. Der Riegel dagegen sitzt
            // erst im Ablauf; diese Bedingung greift schon beim Registrieren, damit eine
            // vergessene Vorschau nicht bis zur Laufzeit unentdeckt bleibt.
            if (stufe != Schutzstufe.Lesen && vorschau == null)
                throw new ArgumentException(
                    "Die Aktion '" + name + "' gehoert zu Stufe " + (int)stufe +
                    " und braucht deshalb eine Vorschau (Fachkonzept 3.5).", nameof(vorschau));

            Name = name;
            Zweck = zweck;
            Stufe = stufe;
            Andockpunkt = andockpunkt ?? "";
            Parameter = parameter ?? Array.Empty<KiParameter>();
            Ausfuehren = ausfuehren;
            Vorbedingung = vorbedingung;
            Vorschau = vorschau;
            Wirkung = wirkung ?? (stufe == Schutzstufe.Lesen ? KiTexte.WirkungLesen : "");
            Umkehrbar = umkehrbar;

            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KiParameter p in Parameter)
                if (!gesehen.Add(p.Name))
                    throw new ArgumentException(
                        "Der Parameter '" + p.Name + "' ist in '" + name + "' doppelt deklariert.", nameof(parameter));
        }

        /// <summary>Sprachneutraler Schluessel der Aktion.</summary>
        public string Name { get; }

        /// <summary>Eine Zeile Klartext, deutsch.</summary>
        public string Zweck { get; }

        /// <summary>Schutzstufe.</summary>
        public Schutzstufe Stufe { get; }

        /// <summary>Aufgerufene Bestandsmethode - Nachweis im Protokoll.</summary>
        public string Andockpunkt { get; }

        /// <summary>Parameter in Anzeigereihenfolge.</summary>
        public IReadOnlyList<KiParameter> Parameter { get; }

        /// <summary>Der eigentliche Aufruf des Bestands (im Anwendungsprojekt).</summary>
        public Func<KiAufruf, KiErgebnis>? Ausfuehren { get; }

        /// <summary>Klartextgrund, warum die Aktion gerade nicht geht; <c>null</c> = nichts spricht dagegen.</summary>
        public Func<KiAufruf, string?>? Vorbedingung { get; }

        /// <summary>Trockenlauf fuer die Bestaetigung (Stufe 2/3). Schreibt nichts.</summary>
        public Func<KiAufruf, string>? Vorschau { get; }

        /// <summary>Ein Satz „was danach anders ist".</summary>
        public string Wirkung { get; }

        /// <summary>
        /// Laesst sich die Aenderung zurueckschreiben? (Fachkonzept 4.4, Punkt 3.)
        /// </summary>
        /// <remarks>
        /// „Umkehrbar" heisst hier ausschliesslich: der Vorzustand ist VOR der Aenderung
        /// bekannt und liesse sich als neue, ebenfalls bestaetigungspflichtige Aktion
        /// zurueckschreiben. Es ist KEIN Rueckgaengig-Stapel und kein Versprechen der
        /// Anwendung - den gibt es im Bestand nicht.
        /// </remarks>
        public bool Umkehrbar { get; }

        /// <summary>Pflichtparameter dieser Aktion.</summary>
        public IEnumerable<KiParameter> Pflichtparameter()
        {
            foreach (KiParameter p in Parameter)
                if (p.Pflicht) yield return p;
        }

        /// <summary>Findet einen Parameter, oder <c>null</c>.</summary>
        public KiParameter? Finde(string name)
        {
            foreach (KiParameter p in Parameter)
                if (string.Equals(p.Name, name, StringComparison.Ordinal)) return p;
            return null;
        }

        /// <inheritdoc/>
        public override string ToString() => Name + " (" + SchutzstufeText.Schluessel(Stufe) + ")";
    }
}
