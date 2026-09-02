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
    /// <c>Program.Main</c> setzt es vor allem anderen auf die WinForms-Fassung, und ohne
    /// Oberfläche bleibt die Vorbelegung stehen.</para>
    ///
    /// <para><b>Die Vorbelegung schluckt nichts.</b> <see cref="Zeigen"/>,
    /// <see cref="Hinweis"/> und <see cref="Warnung"/> schreiben auf die Konsole — im
    /// Referenzlauf landet die Meldung damit im Laufprotokoll statt in einem Dialog, auf
    /// den niemand klickt. Nur <see cref="Warten"/> ist ohne Oberfläche folgenlos: Eine
    /// Sanduhr gibt es dort nicht.</para>
    ///
    /// <para><b>Kein Ersatz für <c>DataRepository.FehlerMelden</c>.</b> Datenbankfehler
    /// gehen weiter dorthin: Dessen Entscheidung „Dialog oder Protokoll" hängt am
    /// Engine-Modus und sammelt die Meldungen eines stillen Laufs ein. Diese Klasse hier
    /// ist die Ebene darunter — sie beantwortet nur, WIE ein Dialog erzeugt wird, nicht
    /// OB einer erscheinen soll.</para>
    /// </summary>
    public static class Meldung
    {
        /// <summary>
        /// Schlichte Meldung ohne Titel — die Entsprechung von
        /// <c>MessageBox.Show(text)</c>.
        /// </summary>
        public static Action<string> Zeigen = text =>
        {
            try { Console.WriteLine(text); } catch { }
        };

        /// <summary>
        /// Hinweis mit Titel — <c>MessageBox.Show(text, titel)</c>. Der Titel steht in
        /// der Konsolenfassung vor dem Text, damit die Zuordnung erhalten bleibt.
        /// </summary>
        public static Action<string, string> Hinweis = (text, titel) =>
        {
            try { Console.WriteLine((titel ?? "") + ": " + text); } catch { }
        };

        /// <summary>
        /// Warnung mit Titel — <c>MessageBox.Show(text, titel, OK, Warning)</c>. Getrennt
        /// von <see cref="Hinweis"/>, weil das Warnsymbol eine Aussage ist und nicht
        /// verlorengehen soll.
        /// </summary>
        public static Action<string, string> Warnung = (text, titel) =>
        {
            try { Console.WriteLine("WARNUNG - " + (titel ?? "") + ": " + text); } catch { }
        };

        /// <summary>
        /// Wartekurve an (<c>true</c>) oder aus (<c>false</c>) —
        /// <c>Cursor.Current = Cursors.WaitCursor</c> bzw. <c>Cursors.Default</c>.
        /// Ohne Oberfläche folgenlos.
        /// </summary>
        public static Action<bool> Warten = an => { };
    }
}
