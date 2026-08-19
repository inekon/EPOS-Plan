using System;

namespace KiKern
{
    /// <summary>
    /// Die sichtbaren Texte des Kerns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TODO(B5): Diese Konstanten sind vorerst deutschsprachig fest verdrahtet. Der Kern
    /// darf <c>MyResource.Resource</c> NICHT referenzieren (er ist UI- und ressourcenfrei,
    /// Fachkonzept 3.7). Mit Paket B5 bekommt er stattdessen einen Textlieferanten, den
    /// das Anwendungsprojekt aus <c>MyResource</c> speist; bis dahin sind alle
    /// Anwendertexte hier an EINER Stelle versammelt und damit leicht umzustellen.
    /// </para>
    /// <para>
    /// Formatplatzhalter sind bewusst durchnummeriert ({0}, {1}, …), damit die spaetere
    /// Uebersetzung die Reihenfolge aendern kann.
    /// </para>
    /// </remarks>
    public static class KiTexte
    {
        // ----------------------------------------------------------- Wirkungssaetze

        /// <summary>Wirkung einer Leseaktion - Vorbelegung fuer Stufe 1.</summary>
        public const string WirkungLesen = "Diese Aktion liest nur; sie ändert nichts.";

        // ----------------------------------------------------------- Registerfehler

        /// <summary>{0} = angefragter Name, {1} = Liste der bekannten Namen.</summary>
        public const string AktionUnbekannt =
            "Die Aktion „{0}“ gibt es nicht. Bekannt sind: {1}.";

        /// <summary>{0} = Aktionsname.</summary>
        public const string AktionOhneAusfuehrung =
            "Die Aktion „{0}“ ist deklariert, aber nicht verdrahtet.";

        // ----------------------------------------------------------- Parameterfehler

        /// <summary>{0} = Anzeigename, {1} = Parametername.</summary>
        public const string PflichtfeldFehlt =
            "Pflichtangabe „{0}“ ({1}) fehlt.";

        /// <summary>{0} = Parametername, {1} = Liste der erlaubten Namen.</summary>
        public const string ParameterUnbekannt =
            "Den Parameter „{0}“ kennt diese Aktion nicht. Erlaubt sind: {1}.";

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public const string KeineGanzzahl =
            "„{0}“ erwartet eine ganze Zahl; geliefert wurde „{1}“.";

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public const string KeineZahl =
            "„{0}“ erwartet eine Zahl; geliefert wurde „{1}“.";

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public const string KeinText =
            "„{0}“ erwartet einen Text; geliefert wurde „{1}“.";

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public const string KeinWahrheitswert =
            "„{0}“ erwartet ja oder nein; geliefert wurde „{1}“.";

        /// <summary>{0} = Anzeigename, {1} = gelieferter Wert.</summary>
        public const string KeineListe =
            "„{0}“ erwartet eine Liste ganzer Zahlen; geliefert wurde „{1}“.";

        /// <summary>{0} = Anzeigename.</summary>
        public const string ListeLeer =
            "Die Liste „{0}“ ist leer.";

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Untergrenze, {3} = Obergrenze.</summary>
        public const string AusserhalbBereich =
            "„{0}“ liegt mit {1} außerhalb des zulässigen Bereichs {2} bis {3}.";

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Untergrenze.</summary>
        public const string UnterGrenze =
            "„{0}“ ist mit {1} kleiner als der zulässige Mindestwert {2}.";

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = Obergrenze.</summary>
        public const string UeberGrenze =
            "„{0}“ ist mit {1} größer als der zulässige Höchstwert {2}.";

        /// <summary>{0} = Anzeigename, {1} = Wert, {2} = erlaubte Werte.</summary>
        public const string WertNichtErlaubt =
            "„{0}“ kennt den Wert „{1}“ nicht. Erlaubt sind: {2}.";

        /// <summary>{0} = Anzeigename, {1} = Hoechstlaenge.</summary>
        public const string TextZuLang =
            "„{0}“ ist zu lang (höchstens {1} Zeichen).";

        /// <summary>{0} = Anzeigename.</summary>
        public const string TextLeer =
            "„{0}“ darf nicht leer sein.";

        /// <summary>Der Aufrufrumpf war kein JSON-Objekt.</summary>
        public const string KeinObjekt =
            "Die Parameter müssen als JSON-Objekt kommen.";

        // ----------------------------------------------------------- Bestaetigungstext

        /// <summary>Ueberschriftfeld „Aktion".</summary>
        public const string FeldAktion = "Aktion";

        /// <summary>Ueberschriftfeld „Zweck".</summary>
        public const string FeldZweck = "Zweck";

        /// <summary>Ueberschriftfeld „Angaben".</summary>
        public const string FeldAngaben = "Angaben";

        /// <summary>Ueberschriftfeld „Wirkung".</summary>
        public const string FeldWirkung = "Wirkung";

        /// <summary>Ueberschriftfeld „Vorschau".</summary>
        public const string FeldVorschau = "Vorschau";

        /// <summary>Ueberschriftfeld „Andockpunkt".</summary>
        public const string FeldAndockpunkt = "Andockpunkt";

        /// <summary>Steht bei „Angaben", wenn die Aktion keine Parameter hat.</summary>
        public const string KeineAngaben = "keine";

        /// <summary>Klartext der Stufe 1.</summary>
        public const string StufeLesen = "Stufe 1 – nur lesend";

        /// <summary>Klartext der Stufe 2.</summary>
        public const string StufeSchreiben = "Stufe 2 – verändert Daten";

        /// <summary>Klartext der Stufe 3.</summary>
        public const string StufeRechnen = "Stufe 3 – rechnet, läuft länger";

        /// <summary>Klartextname einer Stufe fuer die Bestaetigung.</summary>
        public static string Stufe(Schutzstufe stufe)
        {
            switch (stufe)
            {
                case Schutzstufe.Lesen: return StufeLesen;
                case Schutzstufe.Schreiben: return StufeSchreiben;
                case Schutzstufe.Rechnen: return StufeRechnen;
                default: throw new ArgumentOutOfRangeException(nameof(stufe));
            }
        }
    }
}
