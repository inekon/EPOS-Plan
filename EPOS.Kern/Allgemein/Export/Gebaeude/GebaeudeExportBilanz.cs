using System.Collections.Generic;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Bilanz eines Gebäudeexports</b> (Stufe G7a) — was der Schreiber geschrieben hat. Ein
    /// eigener Datensatz, weil die Zähler der <c>ImportBilanz</c> die eines Katalogimports sind
    /// (<c>KatalogImportAblauf</c>) und auf eine Gebäudedatei nicht passen.
    /// </summary>
    internal sealed class GebaeudeExportBilanz
    {
        /// <summary>Legt die Bilanz an.</summary>
        internal GebaeudeExportBilanz(int flaechen, int oeffnungen, int aufbauten, int ersatzaufbauten,
                                      IReadOnlyList<PruefMeldung> meldungen, long bytes)
        {
            Flaechen = flaechen;
            Oeffnungen = oeffnungen;
            Aufbauten = aufbauten;
            Ersatzaufbauten = ersatzaufbauten;
            Meldungen = meldungen ?? new List<PruefMeldung>();
            Bytes = bytes;
        }

        /// <summary>Zahl der geschriebenen Flächen (<c>Surface</c>).</summary>
        internal int Flaechen { get; }

        /// <summary>Zahl der geschriebenen Öffnungen (<c>Opening</c>).</summary>
        internal int Oeffnungen { get; }

        /// <summary>Zahl der geschriebenen Aufbauten (<c>Construction</c>) — die Ersatzschichtungen eingeschlossen.</summary>
        internal int Aufbauten { get; }

        /// <summary>Davon gekennzeichnete Ersatzschichtungen (<see cref="AbbildAufbau.IstErsatz"/>).</summary>
        internal int Ersatzaufbauten { get; }

        /// <summary>Die Meldungen des Schreibens; der Schreiber selbst meldet nur, was er beim Schreiben feststellt.</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen { get; }

        /// <summary>Größe der geschriebenen Datei [Byte].</summary>
        internal long Bytes { get; }
    }
}
