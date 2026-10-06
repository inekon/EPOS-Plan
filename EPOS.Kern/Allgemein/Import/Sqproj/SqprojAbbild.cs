using System;
using System.Collections.Generic;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Geschoss der Projektdatei</b> (<c>BmFloor</c>): Kennung und Name.
    /// </summary>
    internal sealed record SqprojGeschoss(string Uuid, string Name);

    /// <summary>
    /// <b>Ein Raum der Projektdatei</b> (<c>BmRoom</c>): Kennung, Name (<c>ShortDesc</c>), Geschoss über <c>FloorUUID</c>,
    /// die GUID <c>GId</c> (dieselbe GUID wie die <c>IfcSpace.GlobalId</c> des HottCAD-Exports, Befund Kapitel 6), der
    /// Raumartcode <c>RoomType</c> und Fläche und Volumen der Datei.
    /// </summary>
    internal sealed class SqprojRaum
    {
        internal string Uuid { get; init; } = "";
        internal string Name { get; init; } = "";
        internal string GeschossUuid { get; init; }
        internal string GeschossName { get; init; }
        internal string Gid { get; init; }
        internal int? Raumart { get; init; }
        internal double? FlaecheM2 { get; init; }
        internal double? VolumenM3 { get; init; }

        public override string ToString() => (GeschossName ?? "?") + "/" + Name;
    }

    /// <summary>
    /// <b>Eine Zone der Projektdatei</b> (<c>BmZone</c>) mit ihrem <c>ZoneType</c> (belegt: 5 Nutzungszone, 6 Simulationszone),
    /// den Räumen aus <c>BmZoneReference</c> in fester Reihenfolge, dem DIN-V-18599-Nutzungsprofil (Typ 5, über
    /// <c>PdProfileReference</c>) und der Profilgruppe (Typ 6, über <c>ProfileGroupUUID</c>).
    /// </summary>
    internal sealed class SqprojZone
    {
        internal string Uuid { get; init; } = "";
        internal string Name { get; init; } = "";
        internal int? Zonentyp { get; init; }
        internal double? FlaecheM2 { get; init; }
        internal double? VolumenM3 { get; init; }
        internal List<string> Raeume { get; } = new List<string>();
        internal SqprojNutzungsprofil Nutzungsprofil { get; set; }
        internal SqprojProfilgruppe Gruppe { get; set; }

        /// <summary>Eine Simulationszone (Typ 6)?</summary>
        internal bool IstSimulationszone => Zonentyp == SqprojAbbild.ZONENTYP_SIMULATION;

        public override string ToString() => Name + " [" + (Zonentyp?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "?") + "]";
    }

    /// <summary>
    /// <b>Das Abbild der Projektdatei</b> (Datenaustauschkonzept 16.2, 16.3): was der <see cref="SqprojLeser"/> aus der
    /// HottCAD-Projektdatei gelesen hat — Gebäude, Geschosse, Räume, Zonen (nur die belegten Typen 5 und 6), Profile mit
    /// Tagesganglinien und Abschnitten — samt den Zählern des Übersprungenen und den Meldungen. Formatfrei bis auf die
    /// Belegtexte; ohne Datenbank, ohne Verbindung (der Leser hält keine über das Lesen hinaus).
    /// </summary>
    internal sealed class SqprojAbbild
    {
        /// <summary>Der belegte Code der Nutzungszone nach DIN V 18599 (Befund 3.2).</summary>
        internal const int ZONENTYP_NUTZUNG = 5;

        /// <summary>Der belegte Code der Simulationszone mit Profilgruppe (Befund 3.2).</summary>
        internal const int ZONENTYP_SIMULATION = 6;

        /// <summary>Die Fassung der Raumtabelle aus <c>XmTables.Version</c>; <c>null</c> = nicht genannt.</summary>
        internal string Fassung { get; set; }

        /// <summary>Der Name des (ersten) Gebäudes aus <c>BmBuilding</c>.</summary>
        internal string Gebaeudename { get; set; }

        /// <summary>Die Gebäudegruppe (<c>BmBuilding.ProfileGroupUUID</c>, <c>ProfileGroupType</c> 4) — der Weg für die Gebäudeebene; <c>null</c> = keine.</summary>
        internal SqprojProfilgruppe Gebaeudegruppe { get; set; }

        /// <summary>Die Geschosse nach Name.</summary>
        internal List<SqprojGeschoss> Geschosse { get; } = new List<SqprojGeschoss>();

        /// <summary>Die Räume nach Geschoss und Name (Regel 16.6 Nr. 5).</summary>
        internal List<SqprojRaum> Raeume { get; } = new List<SqprojRaum>();

        /// <summary>Die Zonen der Typen 5 und 6 nach Name und Kennung.</summary>
        internal List<SqprojZone> Zonen { get; } = new List<SqprojZone>();

        /// <summary>Die übrigen Zonentypen: Code → Zahl (gezählt und übersprungen).</summary>
        internal SortedDictionary<int, int> ZonentypenUebersprungen { get; } = new SortedDictionary<int, int>();

        /// <summary>Die übersprungenen Profilklassen (Beleuchtung, Elektro, Trinkwasser, Feuchte, Sonnenschutz …): Code → Zahl.</summary>
        internal SortedDictionary<int, int> KlassenUebersprungen { get; } = new SortedDictionary<int, int>();

        /// <summary>Die Betriebsarten der Zeitkurven (<c>OperatingModeType</c>, 1 Betriebsstunde, 2 außerhalb der Nutzungszeit): Code → Zahl der Profile, die ihn tragen.</summary>
        internal SortedDictionary<int, int> Betriebsarten { get; } = new SortedDictionary<int, int>();

        /// <summary>Was beim Lesen aufgefallen ist.</summary>
        internal List<PruefMeldung> Meldungen { get; } = new List<PruefMeldung>();

        /// <summary>Die benannte Ablehnung (fehlende Tabelle, keine SQLite-Datei); <c>null</c> = gelesen.</summary>
        internal PruefMeldung Ablehnung { get; set; }

        /// <summary>Ist die Projektdatei abgelehnt?</summary>
        internal bool Abgelehnt => Ablehnung != null;

        /// <summary>Die Zahl der Zeitprofile der fünf gelesenen Klassen über alle Gruppen.</summary>
        internal int Zeitprofile { get; set; }

        /// <summary>Die Zahl der gelesenen Abschnitte (<c>PdProfileTaskSerial</c>) der gelesenen Zeitprofile.</summary>
        internal int Abschnitte { get; set; }

        /// <summary>Der Raum zu einer Kennung; <c>null</c> = unbekannt.</summary>
        /// <summary>Die raumbezogenen Hüllflächen (<c>BmElement</c> Level 3, BA-4b); leer ohne Bauteiltabellen.</summary>
        internal List<SqprojHuellflaeche> Huellflaechen { get; } = new List<SqprojHuellflaeche>();

        /// <summary>Der Aufbaukatalog der Datei (<c>TcBuildingElementDimension</c>, auch unbenutzte) nach Kennung (Normalform).</summary>
        internal Dictionary<string, SqprojAufbau> Aufbauten { get; } = new Dictionary<string, SqprojAufbau>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Sind die vier Bauteiltabellen gelesen? Fehlen sie, bleibt es beim Stand ohne Aufbauten (benannt gemeldet).</summary>
        internal bool BauteileGelesen { get; set; }

        internal SqprojRaum Raum(string uuid)
            => uuid == null ? null : Raeume.Find(r => string.Equals(r.Uuid, uuid, StringComparison.OrdinalIgnoreCase));
    }
}
