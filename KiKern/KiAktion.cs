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
        /// <param name="formularaktion">
        /// Wirkt die Aktion in eine offene MASKE statt in die Datenbank (Stufe „2F",
        /// Fachkonzept 11.4)? Siehe <see cref="Formularaktion"/>.
        /// </param>
        /// <param name="datenbankwirksam">
        /// Kann diese Aktion den Datenbestand veraendern? Siehe
        /// <see cref="Datenbankwirksam"/>. Nur eine <paramref name="formularaktion"/> darf
        /// hier <c>false</c> sagen.
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
                        bool umkehrbar = false,
                        bool formularaktion = false,
                        bool datenbankwirksam = true)
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

            // Eine Formularaktion auf Stufe 1 waere ein Loch: Die Bestaetigungspflicht
            // haengt an der STUFE (KiRiegel), und die Modalitaetsweiche des Ausfuehrers
            // laesst gerade Formularaktionen an einen offenen Dialog heran. Zusammen ergaebe
            // das einen Eingriff in eine Maske ohne jeden Klick. Deshalb faellt der Fall
            // schon beim Deklarieren auf - und nicht erst, wenn er laeuft.
            if (formularaktion && stufe == Schutzstufe.Lesen)
                throw new ArgumentException(
                    "Die Aktion '" + name + "' ist als Formularaktion deklariert und darf deshalb " +
                    "nicht zu Stufe 1 gehoeren (Fachkonzept 11.4).", nameof(formularaktion));

            // Vom Sicherungspunkt darf sich NUR eine Aktion freistellen, die ausschliesslich
            // in die Oberflaeche wirkt. Waere das auch einer gewoehnlichen Schreibaktion
            // erlaubt, entstuende der eine Fall, den Fachkonzept 4.4 Punkt 1 ausschliesst:
            // eine Aenderung am Datenbestand ohne Rueckweg. Der Fall faellt deshalb schon
            // beim Deklarieren auf - und nicht erst, wenn die Aktion laeuft.
            if (!formularaktion && !datenbankwirksam)
                throw new ArgumentException(
                    "Die Aktion '" + name + "' ist nicht als Formularaktion deklariert und darf " +
                    "sich deshalb nicht vom Sicherungspunkt freistellen (Fachkonzept 4.4).",
                    nameof(datenbankwirksam));

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
            Formularaktion = formularaktion;
            Datenbankwirksam = datenbankwirksam;

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

        /// <summary>
        /// Wirkt die Aktion in eine offene Maske statt in die Datenbank?
        /// (Stufe „2F", Fachkonzept 11.4.)
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Am Riegel aendert dieses Kennzeichen NICHTS.</b> Es ist keine vierte Stufe
        /// und keine Ausnahme: Eine Formularaktion gehoert zu
        /// <see cref="Schutzstufe.Schreiben"/> und braucht damit dieselbe Bestaetigung wie
        /// jede andere Schreibaktion - <see cref="KiRiegel.BrauchtBestaetigung(KiAktion)"/>
        /// haengt weiterhin allein an der Stufe. Waere das Kennzeichen eine Ausnahme,
        /// stuende in <c>KiRiegel</c> wieder eine Namensliste, und genau die soll es dort
        /// nicht geben.
        /// </para>
        /// <para>
        /// <b>Wofuer es dann da ist.</b> Zwei Dinge im Anwendungsprojekt haengen daran:
        /// die Modalitaetsweiche (eine Formularaktion VERLANGT die offene Zielmaske, waehrend
        /// alle uebrigen Aktionen bei offenem modalem Dialog abgewiesen werden) und die
        /// Feldsicherung (<see cref="KiFeldsicherung"/>), die nur fuer diese Aktionen
        /// abschaltbar ist. Beides braucht ein Merkmal an der DEKLARATION - abgeleitet aus
        /// dem Aktionsnamen waere es wieder eine Liste, die altert.
        /// </para>
        /// </remarks>
        public bool Formularaktion { get; }

        /// <summary>
        /// Kann diese Aktion den Datenbestand veraendern? Standard <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Wofuer das Kennzeichen da ist:</b> fuer den Sicherungspunkt
        /// (<see cref="BrauchtSicherungspunkt"/>). Vor der ersten Aenderung am Datenbestand
        /// entsteht eine Kopie der Datenbank (Fachkonzept 4.4, Punkt 1) - bei rund 90 MB
        /// ist das nichts, was man ohne Anlass tut. <c>feld_setzen</c> und
        /// <c>formular_ausfuellen</c> tragen aber nur TEXT in ein Eingabefeld ein; die
        /// Datenbank sehen sie nie. Eine Kopie dafuer sicherte einen Zustand, den die
        /// Aktion gar nicht verlassen kann.
        /// </para>
        /// <para>
        /// <b>Warum die Vorgabe <c>true</c> ist.</b> Wer das Kennzeichen vergisst, bekommt
        /// eine ueberfluessige Kopie - wer es faelschlich auf <c>false</c> setzte, verloere
        /// den Rueckweg. Die Vorgabe zeigt deshalb in die unschaedliche Richtung, und der
        /// Konstruktor laesst <c>false</c> ueberhaupt nur einer
        /// <see cref="Formularaktion"/> durchgehen.
        /// </para>
        /// <para>
        /// <b>Warum am Kennzeichen und nicht am Aktionsnamen.</b> Aus dem Namen abgeleitet
        /// waere es wieder eine Liste im Ausfuehrer, die altert - dieselbe Begruendung wie
        /// bei <see cref="Formularaktion"/>. Und die Unterscheidung liegt nicht am Namen,
        /// sondern an der Sache: <c>dialog_aktion_ausfuehren</c> ist ebenfalls eine
        /// Formularaktion, loest aber einen Knopf der Maske aus - und der schreibt ueber den
        /// Bestand sehr wohl in die Datenbank. Diese Aktion behaelt ihren Sicherungspunkt.
        /// </para>
        /// </remarks>
        public bool Datenbankwirksam { get; }

        /// <summary>
        /// Muss vor dieser Aktion ein Sicherungspunkt vorliegen (Fachkonzept 4.4, Punkt 1)?
        /// </summary>
        /// <remarks>
        /// Zwei Bedingungen, beide notwendig: Die Aktion muss ueber das reine Lesen
        /// hinausgehen UND den Datenbestand erreichen koennen
        /// (<see cref="Datenbankwirksam"/>).
        /// </remarks>
        public bool BrauchtSicherungspunkt => Stufe != Schutzstufe.Lesen && Datenbankwirksam;

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
