using System;
using System.IO;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Naht des Formats</b> (Architekturentscheid A2, Softwarearchitektur 1.5): Strom hinein,
    /// normiertes Abbild heraus — keine Datenbank, keine Oberfläche. Zwei Ausprägungen:
    /// <see cref="GbxmlLeser"/> (G4c) und später <c>IfcLeser</c> (G4a).
    ///
    /// <para><b>Strom statt Pfad</b>: Auf iOS liefert der Dateiwähler einen Strom, und die Datei
    /// wird nie als Text gelesen (UTF-16-Dateien, Datenaustauschkonzept 3.1). <b>Profil in der
    /// Signatur</b>, weil es trägt, was je Format verschieden ist (Regel 3).</para>
    ///
    /// <para><b>Vertrag.</b> Ein Lesefehler ist eine <see cref="SpeicherEngine.PruefMeldung"/> der
    /// Stufe Fehler im Abbild, keine Ausnahme. Einzig <see cref="OperationCanceledException"/>
    /// verlässt den Leser — der Abbruch des Anwenders. Der Strom wird gelesen, nicht geschlossen.</para>
    /// </summary>
    internal interface IGebaeudeLeser
    {
        /// <summary>Liest eine Gebäudedatei in das normierte Abbild.</summary>
        /// <param name="quelle">Der Inhalt der Datei; wird nicht geschlossen.</param>
        /// <param name="profil">Die Ausprägung des Laufs.</param>
        /// <param name="melder">Fortschrittsmelder; darf <c>null</c> sein.</param>
        /// <param name="abbruch">Abbruchzeichen des Anwenders.</param>
        GebaeudeAbbild Lesen(Stream quelle, GebaeudeImportProfil profil,
                             IProgress<ImportFortschritt> melder, CancellationToken abbruch);
    }
}
