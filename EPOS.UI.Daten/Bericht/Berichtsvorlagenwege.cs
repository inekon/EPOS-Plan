using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT der Berichtsvorlagen (Etappe BV-E1; Konzept Berichtsvorlagen 8.4, 10.3): die Wege um
    /// eine Vorlagendatei, die nur die Plattform kennt.
    ///
    /// <para><b>Warum es sie gibt.</b> <c>IDateiDienst</c> kennt „Im Ordner zeigen" nicht, und
    /// <c>MitSystemOeffnen</c> öffnet eine Datei mit dem Programm, das das System dafür hat — für
    /// eine <c>.dotx</c> ist das unter Windows ein NEUES Dokument aus der Vorlage, nicht die Vorlage
    /// selbst. „In Word öffnen" und „Schreibgeschützt öffnen" brauchen deshalb Word als Programm, und
    /// das kennt allein die Windows-Schale. Sie hängt die Wege in <c>Program.Main</c> ein
    /// (<see cref="Plattform"/>); iOS lässt sie leer. Bauform wie <see cref="Katalogwege"/> und
    /// <see cref="Gebaeudewege"/>, als Instanz, damit ein Prüfstand seine eigenen Wege
    /// hineinreichen kann.</para>
    ///
    /// <para><b>Kein Delegat ist kein Menüeintrag.</b> Welche Einträge das Menü „…" der Gruppe
    /// „Vorlage" trägt, entscheidet die Hülle aus den belegten Wegen und der Quelle der Vorlage
    /// (<see cref="BerichtsvorlagenGaben"/>): mit <see cref="InWordOeffnen"/> „In Word öffnen", mit
    /// <see cref="ImOrdnerZeigen"/> „Im Ordner zeigen", ohne beide „Teilen…" über
    /// <c>Dienste.Datei.MitSystemOeffnen</c> (auf iOS das Teilen-Blatt). Was die Plattform nicht
    /// kann, lehnt die Hülle BENANNT ab — ein Weg, der <c>false</c> liefert, wird zur Meldung.</para>
    /// </summary>
    internal sealed class Berichtsvorlagenwege
    {
        /// <summary>
        /// Die Wege der laufenden Schale. Ohne Registrierung (iOS, Prüfstand, Konsolenlauf) keine —
        /// dann bietet das Menü „Teilen…", und der Vorlagenordner ist fest.
        /// </summary>
        internal static Berichtsvorlagenwege Plattform { get; set; } = new Berichtsvorlagenwege();

        /// <summary>
        /// Zeigt die Datei in der Dateiverwaltung der Plattform (Windows: der Explorer mit der Datei
        /// markiert). <c>false</c> = ging nicht; <c>null</c> = diese Plattform kennt den Weg nicht.
        /// </summary>
        internal Func<string, bool> ImOrdnerZeigen { get; init; }

        /// <summary>
        /// Öffnet die Vorlage SELBST in Word zum Bearbeiten — auch eine <c>.dotx</c>. <c>false</c> =
        /// Word fehlt oder ließ sich nicht starten; <c>null</c> = diese Plattform kennt den Weg nicht.
        /// </summary>
        internal Func<string, bool> InWordOeffnen { get; init; }

        /// <summary>
        /// Öffnet eine mitgelieferte Vorlage schreibgeschützt in Word (Windows: eine schreibgeschützte
        /// Kopie im Temp-Ordner). <c>false</c> = Word fehlt; <c>null</c> = der Weg fehlt.
        /// </summary>
        internal Func<string, bool> SchreibgeschuetztOeffnen { get; init; }

        /// <summary>
        /// Lässt die Plattform den Vorlagenordner wählen (<c>Dienste.Datei.OrdnerWaehlen</c>)? Auf iOS
        /// nicht — <c>OrdnerWaehlen</c> liefert dort immer <c>""</c>, die Ablage ist fest die Sandbox;
        /// die Hülle der Einstellungen sperrt das Feld dann mit Grund.
        /// </summary>
        internal bool OrdnerWaehlbar { get; init; }
    }
}
