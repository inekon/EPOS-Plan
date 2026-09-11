// Der AUFRUFKONTEXT des Hilfe-Assistenten: Wer ruft ihn, aus welchem Dialog, wegen
// welcher Meldung? (Auftrag #199, Stufe S1 des Konzepts "Der Hilfe-Assistent im
// Dialog", 3.1 und 3.2)
//
// WARUM ER IM KERN STEHT. Er ist das EINE Argument von
// Dienste.Navigation.OeffneMaske(Masken.KiAssistent, kontext) - und den Weg dorthin
// kennen beide Plattformen: Unter Windows oeffnet WinFormsNavigation die nicht-modale
// KiChatHuelle, auf iOS wechselt AppWurzel die Ansicht. Eine Klasse in der Oberflaeche
// koennte der Kern nicht annehmen (EPOS.UI kennt EPOS.Kern, nicht umgekehrt).
//
// FUENF ANGABEN UND KEINE SECHSTE. Bereich, Dialogname, Hilfeschluessel, Frage,
// Kennung. Feldwerte des Dialogs stehen ABSICHTLICH nicht darin - das ist Stufe S2
// (Weg 4, Maskenbruecke) und braucht eine eigene Einwilligung (KI-D-Q2). Was Stufe S1
// uebertraegt, ist damit genau das, was das Konzept 3.2 zusagt: Bereich, Dialogname,
// Kennung und der Bannertext - keine Projektdaten.
//
// DER BEREICH KOMMT AUS DEM HILFESCHLUESSEL. Jeder Dialog traegt ihn ohnehin am
// InfoKnopf (die einzige Stelle, die ihn fachlich benennt); KiChatKontext.
// BereichFuerHilfeschluessel macht daraus einen Eintrag der Positivliste. Deshalb
// bekommt kein einziger Dialog dafuer eine Zeile.

using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was der Hilfe-Assistent über seinen Aufruf aus einem Dialog erfährt
    /// (Stufe S1, Wege 1 und 2).
    /// </summary>
    public sealed class KiAufrufkontext
    {
        /// <summary>
        /// Der Bedienbereich — immer ein Eintrag der Positivliste von
        /// <see cref="KiChatKontext"/>, sonst <see cref="KiChatKontext.BEREICH_UNBEKANNT"/>.
        /// </summary>
        public string Bereich { get; set; } = KiChatKontext.BEREICH_UNBEKANNT;

        /// <summary>
        /// Der Name des Dialogs in der Oberflächensprache („Heizkessel bearbeiten") —
        /// leer, wenn ihn der Aufrufer nicht nennt.
        /// </summary>
        /// <remarks>
        /// Er ist ein BEDIENBEGRIFF und kein Projektdatum; dieselbe Grenze wie beim
        /// Bereich. Wer hier einen Fenstertitel mit Projekt- oder Kundennamen einträgt,
        /// verletzt die Zusage aus Konzept 3.2 — der Titel eines Dialogs trägt sie im
        /// Haus nicht.
        /// </remarks>
        public string Dialogname { get; set; } = "";

        /// <summary>
        /// Der Hilfeschlüssel des Dialogs (<c>Form_Heizkessel.btn_Help</c>) — die
        /// Herkunft des <see cref="Bereich"/>. Leer, wenn der Aufruf nicht aus einem
        /// Info-Knopf kommt.
        /// </summary>
        public string Hilfeschluessel { get; set; } = "";

        /// <summary>
        /// Die vorbelegte Frage. Sie steht in der Eingabezeile und wird
        /// <b>nicht</b> von selbst abgeschickt (Konzept 5: „kein Assistent ohne
        /// Anwenderfrage").
        /// </summary>
        public string Frage { get; set; } = "";

        /// <summary>
        /// Die sprachneutrale Kennung der Meldung, zu der gefragt wird
        /// (<c>FLOTTE_ARBEITSLOS</c>, <c>LAUF_W_ERZEUGER_OHNE_KASKADENPLATZ</c>,
        /// <c>PV_STRANG_P1</c>) — leer beim Aufruf über den KI-Knopf.
        /// </summary>
        /// <remarks>
        /// Sie ist zugleich der Suchbegriff: <see cref="HilfeWissen.Suchen"/> findet
        /// den Aktionswissen-Abschnitt über sie auch ohne Modell.
        /// </remarks>
        public string Kennung { get; set; } = "";

        /// <summary>Ein Aufruf aus einem Dialog, dessen Bereich aus dem Hilfeschlüssel folgt.</summary>
        /// <param name="hilfeschluessel">Der Schlüssel des Info-Knopfes; leer ist erlaubt.</param>
        /// <param name="dialogname">Der Name des Dialogs; leer ist erlaubt.</param>
        /// <param name="frage">Vorbelegte Frage; leer ist erlaubt.</param>
        /// <param name="kennung">Meldungskennung; leer ist erlaubt.</param>
        public static KiAufrufkontext AusHilfeschluessel(string hilfeschluessel,
                                                        string dialogname = null,
                                                        string frage = null,
                                                        string kennung = null)
        {
            return new KiAufrufkontext
            {
                Bereich = KiChatKontext.BereichFuerHilfeschluessel(hilfeschluessel),
                Hilfeschluessel = hilfeschluessel ?? "",
                Dialogname = dialogname ?? "",
                Frage = frage ?? "",
                Kennung = kennung ?? ""
            };
        }

        /// <summary>Die Kurzfassung für das Protokoll (Konzept 4, „Protokoll").</summary>
        public override string ToString()
        {
            string s = Bereich ?? "";
            if (!string.IsNullOrEmpty(Dialogname)) s += " | " + Dialogname;
            if (!string.IsNullOrEmpty(Kennung)) s += " | " + Kennung;
            return s;
        }
    }
}
