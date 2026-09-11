// Ist der Hilfe-Assistent auf DIESER Installation grundsaetzlich moeglich?
// (Auftrag #199, Stufe S1 des Konzepts "Der Hilfe-Assistent im Dialog", 3.1)
//
// WARUM ES DIESE AUSKUNFT GIBT. Der KI-Knopf steht seit S1 nicht mehr in 88 Dialogen
// einzeln, sondern IM InfoKnopf (Weg 1). Damit braucht die Oberflaeche genau EINE
// Frage, die sie stellen darf, ohne den Kern weiter zu kennen: "Darf dieser Rechner
// den Assistenten ueberhaupt anbieten?" Stuende die Antwort in der Komponente,
// stuende sie 88-mal.
//
// WAS SIE NICHT PRUEFT - Anwenderentscheid KI-D-Q1 (11.09.2026): "Ist der KI-Knopf
// auch ohne Einrichtung sichtbar? Ja; er fuehrt in die Einstellungen mit Hinweis, was
// fehlt. Ein fehlender Knopf ist nicht erklaerbar." Netz, API-Schluessel, Einwilligung
// und Tageslimit sind deshalb AUSDRUECKLICH keine Bedingung. Sie entscheiden, was der
// Chat TUT, nicht, ob der Weg dorthin sichtbar ist; der Riegel bleibt, wo er hingehoert
// (KiEinwilligung, KiChatService), und ein Chat ohne Schluessel arbeitet als reine
// Hilfesuche weiter (Hilfe-Betrieb, Fachkonzept 11.9).
//
// WAS SIE PRUEFT. Den EINEN Zustand, in dem es den Assistenten auf dieser Installation
// nicht gibt: den Abschalter KiEinwilligung.Abgeschaltet. Er wird benutzerbezogen
// gesetzt und kann von der Verwaltung maschinenweit vorgegeben werden (HKLM) - das ist
// die Stelle, an der eine Lizenz bzw. eine Kundeninstallation die KI verbietet. Ein
// eigenes Lizenzmerkmal "KI" gibt es im Lizenzmodul heute nicht; kaeme eines, ist
// dieser Haken die einzige Stelle, die es erfaehrt.
//
// DER LESEMODUS BLENDET NICHTS AUS (iF30). Fragen und Suchen sind LESEND; wer seine
// Lizenz erneuern muss, braucht die Hilfe eher mehr als weniger.

using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die eine Auskunft „darf diese Installation den Hilfe-Assistenten anbieten?"
    /// für die Oberfläche (Stufe S1, Weg 1).
    /// </summary>
    public static class KiVerfuegbarkeit
    {
        /// <summary>
        /// Der austauschbare Haken — dieselbe Bauart wie
        /// <see cref="KiChatKontext.AktiverBereich"/> und <c>KiAusfuehrer.Uhr</c>.
        /// <c>null</c> = die Standardauskunft gilt.
        /// </summary>
        /// <remarks>
        /// Gedacht für Prüfstände und für eine Hülle, die den Assistenten aus einem
        /// eigenen Grund nicht anbieten kann (eine Plattform ohne Chatansicht). Ein
        /// werfender Haken gilt als „nicht möglich": Eine Auskunft, die scheitert, darf
        /// keinen Knopf zeichnen, der ins Leere führt.
        /// </remarks>
        public static Func<bool> Haken { get; set; }

        /// <summary>
        /// Ist der Assistent auf dieser Installation grundsätzlich möglich? Ohne Netz,
        /// ohne Schlüssel und ohne Einwilligung ist er das ausdrücklich auch
        /// (KI‑D‑Q1) — der Knopf führt dann in die Einrichtung.
        /// </summary>
        public static bool Moeglich
        {
            get
            {
                Func<bool> haken = Haken;
                if (haken != null)
                {
                    try { return haken(); }
                    catch { return false; }
                }

                try { return !KiEinwilligung.Abgeschaltet; }
                catch { return false; }
            }
        }
    }
}
