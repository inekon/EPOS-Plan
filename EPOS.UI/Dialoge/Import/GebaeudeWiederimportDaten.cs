using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Import
{
    /// <summary>Eine Zeile der Vorschau des Wiederimports (MA-6): Zone oder Bauteil, Zone, Name, Stand, Felder der Datei, Hand.</summary>
    public sealed record WiederimportZeile(bool IstZone, string Zone, string Name, Wiederimportstand Stand,
                                           IReadOnlyList<string> Felder, IReadOnlyList<string> Hand);

    /// <summary>
    /// <b>Die Vorschau des Wiederimports</b> (MA-6, Entwurf Geführter Modellaufbau 2.6) als Daten für den Importdialog: die
    /// Zeilen je Zone und Bauteil, ob die Datei eine geänderte Fassung ist, und die Wahl, entfallende Elemente zu entfernen
    /// (<see cref="EntfernenSetzen"/> merkt sie an der Hülle vor). <see cref="Meldung"/> nicht leer = keine Vorschau möglich.
    /// </summary>
    public sealed class GebaeudeWiederimportDaten
    {
        /// <summary>Der Grund, wenn keine Vorschau möglich ist; leer = Vorschau steht.</summary>
        public string Meldung { get; init; } = "";

        /// <summary>Die Datei ist eine geänderte Fassung der gespeicherten Quelle (andere Prüfsumme).</summary>
        public bool GeaenderteFassung { get; init; }

        /// <summary>Die Zeilen in Anzeigereihenfolge.</summary>
        public IReadOnlyList<WiederimportZeile> Zeilen { get; init; } = Array.Empty<WiederimportZeile>();

        /// <summary>Die Wahl „entfallende Elemente entfernen“ (Vorgabe: behalten).</summary>
        public bool Entfernen { get; set; }

        /// <summary>Meldet die Wahl an die Datenseite; <c>null</c> = ohne Datenseite (Prüfstand).</summary>
        public Action<bool>? EntfernenSetzen { get; init; }

        /// <summary>Die Zahl der Zeilen eines Stands, nur Zonen bzw. nur Bauteile.</summary>
        public int Anzahl(Wiederimportstand stand, bool zonen) => Zeilen.Count(z => z.Stand == stand && z.IstZone == zonen);

        /// <summary>Gibt es entfallende Elemente?</summary>
        public bool MitEntfallenden => Zeilen.Any(z => z.Stand == Wiederimportstand.Entfaellt);
    }
}
