using System.IO;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Naht des Gebäudeexports</b> (Softwarearchitektur 1.5; Stufe G7a) — das Spiegelbild von
    /// <see cref="IGebaeudeLeser"/>: Ein Schreiber bekommt das fertige, formatfreie
    /// <see cref="GebaeudeAbbild"/> und schreibt es in einen <see cref="Stream"/>. Er berührt weder die
    /// Datenbank noch eine Datei: Die Dateiwahl bleibt außerhalb, über <c>Dienste.Datei</c> aus der
    /// Hülle; die Regeln, die aus EPOS-Zeilen ein Abbild machen, stehen im Ablauf
    /// (<c>GebaeudeExportAblauf.Vorbereiten</c>).
    ///
    /// <para><b>Abweichung von der Softwarearchitektur</b> (1.5, Klassendiagramm): Dort heißt es
    /// <c>Schreiben(satz, stream, profil) → ImportBilanz</c>. Der Schreiber bekommt aber das
    /// <b>Abbild</b> — der Satz ist die Leseseite der Hülle —, und er liefert eine eigene
    /// <see cref="GebaeudeExportBilanz"/>: Die Zähler der <c>ImportBilanz</c> sind die eines
    /// Katalogimports und passen nicht (Umsetzungsauftrag G7a, 2.1 Nr. 5).</para>
    /// </summary>
    internal interface IGebaeudeSchreiber
    {
        /// <summary>
        /// Schreibt das Abbild in den Strom. Schreibt entweder die ganze Datei oder nichts: Wird
        /// abgebrochen oder ist das Abbild nicht schreibbar, bleibt der Strom unberührt.
        /// </summary>
        /// <param name="abbild">Das Abbild eines Gebäudes, wie der Ablauf es vorbereitet hat.</param>
        /// <param name="ziel">Der Zielstrom; er bleibt offen.</param>
        /// <param name="profil">Uhr, Sprache, Programmangaben und Testlizenz-Kennzeichen.</param>
        /// <param name="abbruch">Abbruch des Laufs.</param>
        /// <returns>Was geschrieben wurde.</returns>
        GebaeudeExportBilanz Schreiben(GebaeudeAbbild abbild, Stream ziel, GebaeudeExportProfil profil, CancellationToken abbruch);
    }
}
