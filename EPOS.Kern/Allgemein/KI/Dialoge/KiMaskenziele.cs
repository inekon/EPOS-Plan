// WOHIN dialog_oeffnen FUEHRT (Auftrag #201, Stufe S3, Punkt 3).
//
// Der Dialogkatalog nennt seine Masken unter ihren KATALOGSCHLUESSELN
// (Form_Heizkessel_Bearbeiten, Form_PV, Form_PufferSp_Bearbeiten, Form_WP,
// StromspeicherAuslegung, …). Dienste.Navigation kennt dagegen NAVIGATIONSSCHLUESSEL
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

        /// <summary>
        /// Der Seitenschluessel der Kostenverwaltung.
        /// </summary>
        /// <remarks>
        /// <b>Ein Ziel, das heute nirgends aufgeht — und das ist die richtige Angabe.</b>
        /// Die Kostenverwaltung haengt an einer gewaehlten KOMPONENTE eines Projekts;
        /// einen kontextfreien Weg dorthin gibt es nicht, und einen zu erfinden hiesse,
        /// die Maske ohne Bezug zu oeffnen. <c>OeffneMaske</c> liefert deshalb
        /// <c>false</c>, und <c>dialog_oeffnen</c> lehnt benannt ab, statt still nichts
        /// zu tun — genau wie bei der Stromspeicher-Ansicht unter Windows. LESEN und
        /// SETZEN erreichen die Maske trotzdem, sobald der Anwender sie offen hat: Dafuer
        /// zaehlt die Anmeldung an der Maskenbruecke, nicht dieses Ziel.
        /// </remarks>
        public const string KOSTENVERWALTUNG = "KOSTENVERWALTUNG";

        /// <summary>
        /// Der Seitenschluessel der STARTSEITE — das Ziel der Erzeugermasken des
        /// Projekts (Welle KI‑F1).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum die Startseite und nicht die Maske selbst.</b> Die sechs
        /// Projektmasken gehen aus der Erzeugerkarte der Startseite auf (Reiter
        /// „Energieerzeuger") und brauchen eine gewaehlte Anlage; kontextfrei lassen
        /// sie sich nicht oeffnen — dieselbe Lage wie bei den Katalogeditoren, deren
        /// Ziel die Verwaltung ist. Der Assistent fuehrt den Anwender deshalb dorthin,
        /// wo er die Anlage waehlt: auf die Startseite. Das ist der Weg, den er auch
        /// von Hand ginge.
        /// </para>
        /// <para>
        /// <b>Ein Reiterwunsch geht dabei nicht mit.</b> Die <c>AppWurzel</c> fuehrt
        /// zwar einen (<c>Reiterwunsch</c> der <c>Startseite</c>), sie setzt ihn aber
        /// aus ihrem RUECKWEG und nicht aus den Argumenten von <c>OeffneMaske</c>; es
        /// gibt also keinen Schluessel, der die Startseite auf dem Reiter
        /// „Energieerzeuger" aufmachte. Ein solcher waere eine neue Naht durch drei
        /// Schichten und gehoert nicht in diese Welle.
        /// </para>
        /// <para>
        /// <b>Warum die Zeichenkette und nicht <c>Seitenschluessel.Startseite</c>:</b>
        /// dieselbe Begruendung wie bei <see cref="STROMSPEICHER_AUSLEGUNG"/> — jene
        /// Konstante steht in <c>EPOS.UI</c>, und der Kern kennt die Oberflaeche nicht.
        /// Ein Waechter in <c>EPOS.UI.Tests</c> haelt beide gegeneinander. Unter
        /// Windows kennt <c>WinFormsNavigation</c> den Schluessel nicht; dort liefert
        /// <c>OeffneMaske</c> <c>false</c>, und <c>dialog_oeffnen</c> lehnt benannt ab,
        /// statt still nichts zu tun. Auf iOS wechselt die Wurzel die Ansicht.
        /// </para>
        /// </remarks>
        public const string STARTSEITE = "STARTSEITE";

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
                { KiMaskennamen.SIMULATION,       Masken.Simulation },

                // Die Kostenverwaltung geht nur AUS einer gewaehlten Komponente auf;
                // siehe KOSTENVERWALTUNG.
                { KiMaskennamen.KOSTENVERWALTUNG, KOSTENVERWALTUNG },

                // Welle KI-F1: Die Erzeugermasken des PROJEKTS gehen aus der
                // Erzeugerkarte der Startseite auf und brauchen eine gewaehlte Anlage;
                // ihr Ziel ist deshalb die Startseite - siehe STARTSEITE.
                { KiMaskennamen.HEIZKESSEL_PROJEKT,     STARTSEITE },
                { KiMaskennamen.BHKW_PROJEKT,           STARTSEITE },
                { KiMaskennamen.PUFFERSPEICHER_PROJEKT, STARTSEITE },
                { KiMaskennamen.STROMSPEICHER_PROJEKT,  STARTSEITE }
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
