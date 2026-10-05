using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Stand der dazugeladenen Projektdatei</b> im Gebäudeimport (Datenaustauschkonzept 16.4) — der hüllennahe Satz für
    /// den Dialog der Stufe SQ-2: Datei, Fassung, Abbild, Raumabgleich, Bilanz und Meldungen. Ohne Datenbank.
    /// </summary>
    internal sealed class SqprojStand
    {
        internal SqprojStand(string dateiname, string hash, long bytes, SqprojAbbild abbild, SqprojRaumabgleich abgleich,
                             IEnumerable<PruefMeldung> meldungen, PruefMeldung ablehnung)
        {
            Dateiname = dateiname;
            Hash = hash;
            Bytes = bytes;
            Abbild = abbild;
            Abgleich = abgleich;
            Meldungen = (meldungen ?? Enumerable.Empty<PruefMeldung>()).ToList();
            Ablehnung = ablehnung;
        }

        /// <summary>Der Dateiname ohne Pfad.</summary>
        internal string Dateiname { get; }

        /// <summary>SHA-256 der Datei (Kleinbuchstaben); <c>null</c> vor dem Lesen.</summary>
        internal string Hash { get; }

        /// <summary>Die Größe der Datei in Byte.</summary>
        internal long Bytes { get; }

        /// <summary>Das gelesene Abbild; <c>null</c> bei einer Ablehnung vor dem Lesen.</summary>
        internal SqprojAbbild Abbild { get; }

        /// <summary>Der Raumabgleich gegen das IFC-Abbild; <c>null</c> bei einer Ablehnung.</summary>
        internal SqprojRaumabgleich Abgleich { get; }

        /// <summary>Alle Meldungen: Leser und Abgleich.</summary>
        internal IReadOnlyList<PruefMeldung> Meldungen { get; }

        /// <summary>Die benannte Ablehnung (kein HottCAD, zu groß, keine Projektdatei, Tabelle fehlt); <c>null</c> = gelesen.</summary>
        internal PruefMeldung Ablehnung { get; }

        /// <summary>Ist die Projektdatei abgelehnt? Die IFC-Daten bleiben davon unberührt.</summary>
        internal bool Abgelehnt => Ablehnung != null;

        /// <summary>Die Fassung der Projektdatei (<c>XmTables.Version</c> der Raumtabelle).</summary>
        internal string Fassung => Abbild?.Fassung;

        /// <summary>Räume abgeglichen.</summary>
        internal int Abgeglichen => Abgleich?.Abgeglichen ?? 0;

        /// <summary>Räume der Projektdatei ohne Treffer.</summary>
        internal int NichtAbgeglichen => Abgleich?.OhneTreffer.Count ?? 0;

        /// <summary>IFC-Räume ohne Gegenstück.</summary>
        internal int IfcOhneGegenstueck => Abgleich?.IfcOhneGegenstueck.Count ?? 0;

        /// <summary>Zonen der belegten Typen 5 und 6.</summary>
        internal int Zonen => Abbild?.Zonen.Count ?? 0;

        /// <summary>Gelesene Zeitprofile der fünf Klassen.</summary>
        internal int Zeitprofile => Abbild?.Zeitprofile ?? 0;

        /// <summary>Gelesene Abschnitte.</summary>
        internal int Abschnitte => Abbild?.Abschnitte ?? 0;
    }
}
