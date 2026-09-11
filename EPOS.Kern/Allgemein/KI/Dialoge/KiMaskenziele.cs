// WOHIN dialog_oeffnen FUEHRT (Auftrag #201, Stufe S3, Punkt 3).
//
// Der Dialogkatalog nennt fuenf Masken unter ihren KATALOGSCHLUESSELN
// (Form_Heizkessel_Bearbeiten, Form_PV, Form_PufferSp_Bearbeiten, Form_WP,
// StromspeicherAuslegung). Dienste.Navigation kennt dagegen NAVIGATIONSSCHLUESSEL
// (Masken.* bzw. die Seitenschluessel der AppWurzel). Beide Namensraeume gibt es aus
// gutem Grund - der eine steht im Werkzeugvertrag des Modells und im Protokoll, der
// andere in der Navigationstabelle der jeweiligen Huelle -, und zwischen ihnen fehlte
// bis hierher die Zuordnung.
//
// SIE IST DATEN UND KEINE LOGIK. Eine Tabelle, kein switch mit Sonderfaellen: Ein
// Waechter (EPOS.Kern.Tests) haelt sie gegen den Katalog und verlangt, dass JEDER
// Katalogeintrag ein Ziel hat. Waechst dem Katalog eine sechste Maske zu, faellt die
// fehlende Zeile im Test auf und nicht beim Anwender.
//
// WARUM DIE ZIELE DIE VERWALTUNGSMASKEN SIND. Drei der vier Katalogmasken sind EDITOREN,
// die aus einer Liste heraus aufgehen (Heizkessel, Pufferspeicher, Photovoltaik) - sie
// brauchen einen gewaehlten Satz und lassen sich nicht kontextfrei oeffnen (dieselbe
// Begruendung, aus der maske_oeffnen nie ins Register kam, KiAktionen). Der Assistent
// fuehrt den Anwender deshalb DORTHIN, wo er den Satz waehlt; das ist der Weg, den er
// auch von Hand ginge.

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zuordnung Katalogmaske → Navigationsschluessel fuer <c>dialog_oeffnen</c>.
    /// </summary>
    public static class KiMaskenziele
    {
        /// <summary>
        /// Der Seitenschluessel der Stromspeicher-Ansicht.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.StromspeicherAuslegung</c>.</b>
        /// Jene Konstante steht in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht
        /// (die Abhaengigkeit laeuft in die andere Richtung). Die AppWurzel setzt dieselbe
        /// Zeichenkette; ein Waechter in <c>EPOS.UI.Tests</c> haelt beide gegeneinander,
        /// damit aus zwei Fundstellen nicht zwei Wahrheiten werden — dasselbe Muster, mit
        /// dem <c>Masken.KiAssistent</c> und <c>Seitenschluessel.KiAssistent</c> seit #199
        /// zusammengehalten werden.
        /// </remarks>
        public const string STROMSPEICHER_AUSLEGUNG = "STROMSPEICHER_AUSLEGUNG";

        private static readonly Dictionary<string, string> ZIELE =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Der Heizkesseleditor geht aus der Heizkesselverwaltung auf.
                { KiMaskennamen.HEIZKESSEL,       Masken.HeizkesselAdmin },

                // Der Photovoltaikdialog haengt an der gewaehlten Projektzeile; der
                // kontextfreie Weg ist die Modulverwaltung.
                { KiMaskennamen.PHOTOVOLTAIK,     Masken.PvAdmin },

                // Ebenso der Pufferspeichereditor.
                { KiMaskennamen.PUFFERSPEICHER,   Masken.PufferSpAdmin },

                // Die Waermepumpenverwaltung IST die Maske des Katalogeintrags - hier
                // fallen Katalogschluessel und Navigationsschluessel zusammen.
                { KiMaskennamen.WAERMEPUMPE,      Masken.WpAdministration },

                // Die Stromspeicher-Ansicht ist eine freie ANSICHT der AppWurzel und
                // keine Maske der WinForms-Navigationstabelle. Unter Windows braucht sie
                // einen gerechneten Simulationslauf und geht deshalb ueber den Knopf der
                // Ergebnisseite auf; dort liefert OeffneMaske false, und die Aktion lehnt
                // benannt ab. Auf iOS wechselt die Wurzel die Ansicht.
                { KiMaskennamen.STROMSPEICHER_AUSLEGUNG, STROMSPEICHER_AUSLEGUNG },

                // Die Ansicht „Simulation" ist eine freie ANSICHT der AppWurzel und
                // zugleich ein Maskenschluessel der Windows-Navigationstabelle (SIM-Q3,
                // #207) - beide Wege fuehren ueber denselben Schluessel, und deshalb
                // steht hier Masken.Simulation und keine zweite Zeichenkette.
                { KiMaskennamen.SIMULATION,       Masken.Simulation }
            };

        /// <summary>Alle zugeordneten Katalogmasken.</summary>
        public static IReadOnlyCollection<string> Masken_ => ZIELE.Keys;

        /// <summary>
        /// Der Navigationsschluessel zur Katalogmaske; leer, wenn es keinen gibt.
        /// </summary>
        public static string Ziel(string maskenname)
        {
            if (string.IsNullOrWhiteSpace(maskenname)) return "";
            string ziel;
            return ZIELE.TryGetValue(maskenname.Trim(), out ziel) ? ziel : "";
        }

        /// <summary>Gibt es fuer diese Katalogmaske ein Ziel?</summary>
        public static bool Kennt(string maskenname) => Ziel(maskenname).Length > 0;
    }
}
