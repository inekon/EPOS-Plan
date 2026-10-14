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

        /// <summary>Die gewählte Zonierung (E87, F1); Vorgabe die DIN-V-18599-Zonen.</summary>
        internal SqprojZonierung Gewaehlt { get; set; } = SqprojZonierung.Din18599;

        /// <summary>Trägt die Datei DIN-V-18599-Zonen (Typ 5) mit abgeglichenen Räumen?</summary>
        internal bool HatDinZonen => SqprojZonen.Belegte(Abbild, Abgleich, SqprojZonierung.Din18599) > 0;

        /// <summary>Trägt die Datei Simulationszonen (Typ 6) mit abgeglichenen Räumen?</summary>
        internal bool HatSimulationszonen => SqprojZonen.Belegte(Abbild, Abgleich, SqprojZonierung.Simulation) > 0;

        /// <summary>Trägt die Datei beide Zonierungen — nur dann gibt es die Wahl?</summary>
        internal bool BeideZonierungen => HatDinZonen && HatSimulationszonen;

        /// <summary>Die wirksame Zonierung: die gewählte, wenn vorhanden, sonst die vorhandene (<see cref="SqprojZonen.Wirksam"/>).</summary>
        internal SqprojZonierung Zonierung => SqprojZonen.Wirksam(Abbild, Abgleich, Gewaehlt);

        /// <summary>
        /// Die Standprüfung gegen die IFC (nur beim Weg „IFC + Projektdatei“, Anwenderentscheid vom 08.10.2026); <c>null</c> =
        /// nicht geprüft (allein aus der Projektdatei, abgelehnt).
        /// </summary>
        internal Standpruefung Standpruefung { get; set; }

        /// <summary>
        /// Die Wahl der Aufbauquelle: <see cref="WindowsFormsApplication1.Aufbauquelle.Offen"/>, solange die Standprüfung angeschlagen hat
        /// und nicht gewählt ist; ohne Anschlag <see cref="WindowsFormsApplication1.Aufbauquelle.Ifc"/> (der heutige Weg). Wird nicht
        /// gespeichert — ein neues Lesen beginnt wieder bei der Prüfung.
        /// </summary>
        internal Aufbauquelle Aufbauquelle { get; set; } = Aufbauquelle.Ifc;

        /// <summary>Gelten Aufbau und U der Projektdatei (Wahl <see cref="WindowsFormsApplication1.Aufbauquelle.Projektdatei"/>)?</summary>
        internal bool ProjektdateiGilt => !Abgelehnt && Aufbauquelle == Aufbauquelle.Projektdatei;

        /// <summary>
        /// <b>Die Sperre am Übernehmen</b>: Ist die Aufbauquelle offen, ist der Zuordnungsstand unvollständig — eine Meldung der
        /// Stufe Fehler (<see cref="SqprojProtokoll.AUFBAUQUELLE_OFFEN"/>) mit Flächenanteil; <c>null</c> = übernehmbar.
        /// </summary>
        internal PruefMeldung Aufbauquellenpruefung()
            => Abgelehnt || Aufbauquelle != Aufbauquelle.Offen ? null
             : new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.AUFBAUQUELLE_OFFEN,
                                System.Math.Round(100.0 * (Standpruefung?.Anteil ?? 0.0), 1).ToString(System.Globalization.CultureInfo.CurrentCulture));

        /// <summary>Das U der Projektdatei eines IFC-Bauteils, wenn sie gilt; sonst <c>null</c> (es gilt das U der IFC).</summary>
        internal double? UWirksam(AbbildBauteil b) => ProjektdateiGilt ? Standpruefung?.UProjektdatei(b) : null;

        /// <summary>Der Protokollsatz der wirksamen Zonierung (<c>IMP_SQ_PROT_ZONIERUNG</c> bzw. <c>…_EINE</c>); <c>null</c> bei einer Ablehnung.</summary>
        internal PruefMeldung Zonierungsmeldung() => Abgelehnt || Abbild == null ? null : SqprojZonen.Zonierungsmeldung(Abbild, Abgleich, Zonierung);
    }
}
