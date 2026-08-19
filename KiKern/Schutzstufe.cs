using System;

namespace KiKern
{
    /// <summary>
    /// Die drei Schutzstufen des Aktionsregisters (Fachkonzept 4.1).
    /// </summary>
    /// <remarks>
    /// Die Stufe entscheidet, ob eine Aktion sofort laufen darf (<see cref="Lesen"/>)
    /// oder erst nach ausdruecklicher Bestaetigung durch den Anwender
    /// (<see cref="Schreiben"/>, <see cref="Rechnen"/>). Die Zahlenwerte sind bewusst
    /// festgeschrieben: sie stehen so im Protokoll und duerfen sich nicht verschieben.
    /// </remarks>
    public enum Schutzstufe
    {
        /// <summary>Liest nur; veraendert weder Datenbank noch Dateien.</summary>
        Lesen = 1,

        /// <summary>Veraendert die Datenbank. Nur nach Bestaetigung (Etappe 3).</summary>
        Schreiben = 2,

        /// <summary>Rechnet, laeuft lang, schreibt gegebenenfalls. Nur nach Bestaetigung (Etappe 4).</summary>
        Rechnen = 3
    }

    /// <summary>
    /// Ausgang eines Ausfuehrungsversuchs - das Feld „Entscheidung" der Protokollzeile
    /// (Fachkonzept 3.6).
    /// </summary>
    public enum KiStatus
    {
        /// <summary>Die Aktion lief und lieferte ein Ergebnis.</summary>
        Ausgefuehrt = 0,

        /// <summary>Vorbedingung, Parameterpruefung oder Anwender haben die Aktion verhindert.</summary>
        Abgelehnt = 1,

        /// <summary>Der Lauf wurde abgebrochen (<see cref="System.Threading.CancellationToken"/>).</summary>
        Abgebrochen = 2,

        /// <summary>Die Aktion lief an und endete in einem Fehler.</summary>
        Fehlgeschlagen = 3
    }

    /// <summary>Klartextnamen der Aufzaehlungen - eine Quelle fuer Protokoll und Anzeige.</summary>
    public static class SchutzstufeText
    {
        /// <summary>Sprachneutraler, protokollfaehiger Name der Stufe.</summary>
        public static string Schluessel(Schutzstufe stufe)
        {
            switch (stufe)
            {
                case Schutzstufe.Lesen: return "lesen";
                case Schutzstufe.Schreiben: return "schreiben";
                case Schutzstufe.Rechnen: return "rechnen";
                default: throw new ArgumentOutOfRangeException(nameof(stufe));
            }
        }

        /// <summary>Sprachneutraler, protokollfaehiger Name des Ausgangs.</summary>
        public static string Schluessel(KiStatus status)
        {
            switch (status)
            {
                case KiStatus.Ausgefuehrt: return "ausgefuehrt";
                case KiStatus.Abgelehnt: return "abgelehnt";
                case KiStatus.Abgebrochen: return "abgebrochen";
                case KiStatus.Fehlgeschlagen: return "fehlgeschlagen";
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        /// <summary>Umkehrung von <see cref="Schluessel(Schutzstufe)"/>.</summary>
        public static Schutzstufe StufeAusSchluessel(string schluessel)
        {
            switch (schluessel)
            {
                case "lesen": return Schutzstufe.Lesen;
                case "schreiben": return Schutzstufe.Schreiben;
                case "rechnen": return Schutzstufe.Rechnen;
                default: throw new ArgumentException("Unbekannte Schutzstufe: " + schluessel, nameof(schluessel));
            }
        }

        /// <summary>Umkehrung von <see cref="Schluessel(KiStatus)"/>.</summary>
        public static KiStatus StatusAusSchluessel(string schluessel)
        {
            switch (schluessel)
            {
                case "ausgefuehrt": return KiStatus.Ausgefuehrt;
                case "abgelehnt": return KiStatus.Abgelehnt;
                case "abgebrochen": return KiStatus.Abgebrochen;
                case "fehlgeschlagen": return KiStatus.Fehlgeschlagen;
                default: throw new ArgumentException("Unbekannter Status: " + schluessel, nameof(schluessel));
            }
        }
    }
}
