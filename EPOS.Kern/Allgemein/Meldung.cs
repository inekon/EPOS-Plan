using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HAKEN, über die Rechenkern-Code eine Meldung an den Anwender abgibt, ohne
    /// WinForms zu kennen (Umsetzungskonzept iU3, Schritt 2).
    ///
    /// <para><b>Warum Haken und nicht Weglassen.</b> Die betroffenen Stellen — der
    /// Fehlerweg der Zugriffsschicht, die Wartekurve der Wärmepumpen-Simulation, die
    /// Rückmeldungen der Stammdaten-Dialoge — SOLLEN unter Windows weiterhin genau das
    /// tun, was sie heute tun. Nur der Rechenkern, der dieselben Dateien mitübersetzt,
    /// darf nicht daran hängen. Ein Feld vom Typ <see cref="Action{T}"/> trennt beides:
    /// Die Vorbelegung reicht an <see cref="Dienste.Dialog"/> weiter, den
    /// <c>Program.Main</c> unter Windows auf die <c>MessageBox</c>-Fassung setzt; ein
    /// Prüfstand kann statt dessen auch den Haken selbst überschreiben.</para>
    ///
    /// <para><b>Die Vorbelegung schluckt nichts.</b> <see cref="Zeigen"/>,
    /// <see cref="Hinweis"/> und <see cref="Warnung"/> landen ohne Oberfläche über
    /// <see cref="StilleDialoge"/> auf der Konsole — im Referenzlauf steht die Meldung
    /// damit im Laufprotokoll statt in einem Dialog, auf den niemand klickt. Nur
    /// <see cref="Warten"/> ist ohne Oberfläche folgenlos: Eine Sanduhr gibt es dort
    /// nicht.</para>
    ///
    /// <para><b>Kein Ersatz für <c>DataRepository.FehlerMelden</c>.</b> Datenbankfehler
    /// gehen weiter dorthin: Dessen Entscheidung „Dialog oder Protokoll" hängt am
    /// Engine-Modus und sammelt die Meldungen eines stillen Laufs ein. Diese Klasse hier
    /// ist die Ebene darunter — sie beantwortet nur, WIE ein Dialog erzeugt wird, nicht
    /// OB einer erscheinen soll.</para>
    ///
    /// <para><b>Seit iU5 zeigen die Vorbelegungen auf <see cref="Dienste.Dialog"/>.</b>
    /// Die vier Haken bleiben, was sie waren — austauschbare Felder, die ein Prüfstand
    /// überschreiben darf —, aber ihre Vorgabe ruft nicht mehr die Konsole, sondern den
    /// Dialogdienst. Der wiederum ist ohne Oberfläche die Konsole
    /// (<see cref="StilleDialoge"/>) und unter Windows die <c>MessageBox</c>. Folgen:
    /// <c>Program.Main</c> belegt <b>nur noch</b> <c>Dienste.*</c> und keinen einzigen
    /// <c>Meldung</c>-Haken mehr; ein Konsolenwerkzeug bekommt sein bisheriges Verhalten
    /// ohne eine Zeile Belegung; und die Hinweise des Kerns tragen unter Windows wieder
    /// das Informationssymbol, das sie bis iU3-2 hatten.</para>
    ///
    /// <para>Die Lambdas sind mit Absicht KEINE Methodengruppen: Sie lesen
    /// <c>Dienste.Dialog</c> bei jedem Aufruf neu. Ein später ausgetauschter Dienst wirkt
    /// dadurch sofort auch über diese Haken.</para>
    /// </summary>
    public static class Meldung
    {
        /// <summary>
        /// Schlichte Meldung ohne Titel — die Entsprechung von
        /// <c>MessageBox.Show(text)</c>. Geht an <see cref="IDialogDienst.Meldung"/>
        /// ohne Titel und behält damit die symbollose Form.
        /// </summary>
        public static Action<string> Zeigen = text => Dienste.Dialog.Meldung(text, null);

        /// <summary>
        /// Hinweis mit Titel — <c>MessageBox.Show(text, titel, OK, Information)</c>.
        /// Der Titel steht in der Konsolenfassung vor dem Text, damit die Zuordnung
        /// erhalten bleibt.
        /// </summary>
        public static Action<string, string> Hinweis = (text, titel) => Dienste.Dialog.Meldung(text, titel);

        /// <summary>
        /// Warnung mit Titel — <c>MessageBox.Show(text, titel, OK, Warning)</c>. Getrennt
        /// von <see cref="Hinweis"/>, weil das Warnsymbol eine Aussage ist und nicht
        /// verlorengehen soll.
        /// </summary>
        public static Action<string, string> Warnung = (text, titel) => Dienste.Dialog.Warnung(text, titel);

        /// <summary>
        /// Wartekurve an (<c>true</c>) oder aus (<c>false</c>) —
        /// <c>Cursor.Current = Cursors.WaitCursor</c> bzw. <c>Cursors.Default</c>.
        /// Ohne Oberfläche folgenlos.
        /// </summary>
        public static Action<bool> Warten = an => Dienste.Dialog.Warten(an);
    }
}
