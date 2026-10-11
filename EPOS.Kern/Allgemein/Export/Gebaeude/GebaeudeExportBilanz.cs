using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// <b>Die Bilanz einer Round-Trip-Anreicherung</b> (Stufe G7d, <see cref="GebaeudeExportAblauf.Anreichern"/>):
    /// angereicherte Quellentitäten, ergänzte (neue Sätze und hinzugekommene Eigenschaften), ersetzte
    /// (Fachwerte und EPOS-Sätze eines früheren Durchlaufs) und übersprungene Zuordnungen, dazu die Meldungen —
    /// darunter der Beipackzettel <c>GEXP_PROT_ANR_BEIPACK_FREMDDATEI</c>, den der Dialog bestätigen lässt.
    /// <see cref="Verweigert"/>: eine der Sperren griff — dann bietet der Dialog die eigene Datei nach G7c an.
    /// </summary>
    internal sealed class GebaeudeAnreicherungBilanz
    {
        internal GebaeudeAnreicherungBilanz(bool geschrieben, bool verweigert, int objekte, int ergaenzt, int ersetzt, int uebersprungen,
                                            IReadOnlyList<PruefMeldung> meldungen, long bytes)
        {
            Geschrieben = geschrieben;
            Verweigert = verweigert;
            Objekte = objekte;
            Ergaenzt = ergaenzt;
            Ersetzt = ersetzt;
            Uebersprungen = uebersprungen;
            Meldungen = meldungen ?? new List<PruefMeldung>();
            Bytes = bytes;
        }

        /// <summary>Eine Verweigerung durch die Sperren: nichts geschrieben, eigene Datei nach G7c angeboten.</summary>
        internal static GebaeudeAnreicherungBilanz Verweigerung(IReadOnlyList<PruefMeldung> sperren)
            => new GebaeudeAnreicherungBilanz(false, true, 0, 0, 0, 0, sperren, 0);

        /// <summary>Wurde die angereicherte Datei geschrieben?</summary>
        internal bool Geschrieben { get; }

        /// <summary>Griff eine Sperre (Hash, Schemastand, Entitätenverlust, keine Quelle, kein STEP)?</summary>
        internal bool Verweigert { get; }

        /// <summary>Die Fehlermeldung, die den Lauf beendet hat; <c>null</c> = geschrieben.</summary>
        internal PruefMeldung Grund => Geschrieben ? null : Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler);

        /// <summary>Angereicherte Quellentitäten.</summary>
        internal int Objekte { get; }

        /// <summary>Neu angelegte Sätze und hinzugekommene Eigenschaften.</summary>
        internal int Ergaenzt { get; }

        /// <summary>Ersetzte Eigenschaften und ersetzte EPOS-Sätze.</summary>
        internal int Ersetzt { get; }

        /// <summary>Übersprungene Zuordnungen.</summary>
        internal int Uebersprungen { get; }

        /// <summary>Die Meldungen des Laufs.</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen { get; }

        /// <summary>Geschriebene Bytes.</summary>
        internal long Bytes { get; }
    }
}
