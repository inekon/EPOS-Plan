using System;
using System.Threading.Tasks;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>CSV am Diagramm, die Naht jeder Hülle:</b> der Delegat, den ein Dialog als
    /// <c>CsvSpeichern</c> bekommt und in seinen <c>Ganglinienexport</c> legt — Modell,
    /// Bildtitel und Raster gehen an <see cref="CsvExportClass.ExportDiagramm"/> (Dateiwahl über
    /// <c>Dienste.Datei</c>, Dateiname nach <c>CHART_DATEI_GANGLINIE</c> mit der Kennung).
    /// </summary>
    internal static class Diagrammexportnaht
    {
        /// <summary>Die Naht mit Projekt- oder Satzkennung im Dateinamen.</summary>
        internal static Func<Zeichenmodell, string, Zeitraster, Task> Fuer(object kennung)
            => (modell, titel, raster) => CsvExportClass.ExportDiagramm(modell, titel, kennung, raster);
    }
}
