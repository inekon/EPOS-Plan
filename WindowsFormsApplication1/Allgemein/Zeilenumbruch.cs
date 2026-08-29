using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Bringt mehrzeilige Anzeigetexte auf die Plattformform von
    /// <see cref="Environment.NewLine"/>.
    ///
    /// <para>
    /// Hintergrund (nachgemessen am 16.08.2026 an den kompilierten
    /// <c>.resources</c> beider Sprachen): Der Ressourcenleser liefert die
    /// Umbrüche der <c>MyResource\Resource.resx</c> zur Laufzeit bereits als
    /// CRLF — die .resx speichert sie physisch als CRLF, und die Werkzeugkette
    /// normalisiert sie beim Kompilieren nicht auf LF. Das im Bestand übliche
    /// <c>Replace("\n", Environment.NewLine)</c> machte aus jedem CRLF ein
    /// CR+CRLF, also eine Leerzeile zu viel je Umbruch.
    /// </para>
    ///
    /// <para>
    /// Die Normalisierung hier ist <b>idempotent</b> und unabhängig davon, ob
    /// die Quelle CRLF, LF oder bereits <see cref="Environment.NewLine"/>
    /// enthält: erst CRLF → LF, dann LF → <see cref="Environment.NewLine"/>.
    /// Sie bleibt damit auch richtig, falls eine künftige Werkzeugkette die
    /// Ressourcen wieder mit LF ausliefert.
    /// </para>
    /// </summary>
    public static class Zeilenumbruch
    {
        /// <summary>
        /// <paramref name="text"/> mit allen Zeilenumbrüchen (CRLF oder LF) als
        /// <see cref="Environment.NewLine"/>; <c>null</c> und Leerstring
        /// bleiben unverändert.
        /// </summary>
        public static string Normalisieren(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        /// <summary>
        /// Das Gegenstück für ZEILENORIENTIERTE Ausgaben: alle Umbrüche werden zu einem
        /// Leerzeichen, Mehrfachleerzeichen fallen zusammen.
        ///
        /// <para>Gebraucht wird das, wo ein Text, der für eine MessageBox mit Absätzen
        /// geschrieben ist, in eine EINZEILE muss — das Simulationsprotokoll
        /// (<see cref="SimulationProtokoll"/>) führt eine Meldung je Zeile, und ein
        /// eingebetteter Umbruch zerlegte sie dort in zwei Meldungen, von denen die
        /// zweite ohne Kopf dasteht. Der Wortlaut bleibt derselbe; nur die Absätze
        /// werden zu Satzabständen.</para>
        /// </summary>
        public static string Einzeilig(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string s = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", " ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s.Trim();
        }
    }
}
