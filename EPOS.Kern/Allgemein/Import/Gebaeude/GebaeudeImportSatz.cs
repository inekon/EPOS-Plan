using System;
using System.Collections.Generic;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Worauf eine Quellentität gepaart wird — die fünf Fremdschlüssel von <c>Tab_Importzuordnung</c> (Datenaustauschkonzept 7.2).</summary>
    internal enum ImportZiel
    {
        /// <summary><c>ID_Gebaeude</c> — in G4c (Zonenregel X4) das einzige Ziel.</summary>
        Gebaeude = 0,
        /// <summary><c>ID_Zone</c> (G6c).</summary>
        Zone = 1,
        /// <summary><c>ID_Bauteil</c> (G6c).</summary>
        Bauteil = 2,
        /// <summary><c>ID_Aufbau</c> (G3/G6c).</summary>
        Aufbau = 3,
        /// <summary><c>ID_Baustoff</c> (G3/G6c).</summary>
        Baustoff = 4,
    }

    /// <summary>
    /// <b>Eine Paarung Quellentität ↔ EPOS-Ziel</b> — später eine Zeile in
    /// <c>Tab_Importzuordnung</c> (7.2). Geschrieben wird in dieser Welle nichts.
    /// </summary>
    internal sealed class GebaeudeQuellzuordnung
    {
        /// <summary>Legt eine Paarung an; die Kennung wird auf 64 Zeichen gekürzt (<see cref="Quellkennung.Kuerzen"/>).</summary>
        public GebaeudeQuellzuordnung(string quelltyp, string quellkennung, ImportZiel ziel)
        {
            Quelltyp = quelltyp ?? "";
            Quellkennung = WindowsFormsApplication1.Quellkennung.Kuerzen(quellkennung ?? "");
            Ziel = ziel;
        }

        /// <summary>Typ der Quellentität (<c>Building</c>, <c>Space</c>, <c>Surface</c>, <c>Opening</c> …), höchstens 40 Zeichen.</summary>
        public string Quelltyp { get; }

        /// <summary>Kennung der Quellentität, höchstens 64 Zeichen.</summary>
        public string Quellkennung { get; }

        /// <summary>Das EPOS-Ziel der Paarung.</summary>
        public ImportZiel Ziel { get; }

        /// <summary>Kurzfassung für Tests.</summary>
        public override string ToString() => Quelltyp + " " + Quellkennung + " -> " + Ziel;
    }

    /// <summary>
    /// <b>Der Satz eines Gebäudeimports</b> — was nach der Zuordnung eines Gebäudes vorliegt und
    /// nach dem OK geschrieben würde (Datenaustauschkonzept 1.4 Nr. 6, 2.1): je Zielfeld eine
    /// <see cref="GebaeudeFeldzeile"/>, dazu Quelle, Gebäude, Baualtersklasse, Meldungen,
    /// Zonenregel und die Quellzuordnungen für die spätere Persistenz.
    ///
    /// <para>Der Satz beschreibt das ZIEL, nicht die Datei — deshalb ist er beiden Lesern gemeinsam
    /// (gbXML jetzt, IFC mit G4a). Er zeigt nichts an und schreibt nichts.</para>
    /// </summary>
    internal sealed class GebaeudeImportSatz
    {
        private readonly List<GebaeudeFeldzeile> _zeilen;
        private readonly List<PruefMeldung> _meldungen;
        private readonly List<GebaeudeQuellzuordnung> _zuordnungen;

        internal GebaeudeImportSatz(GebaeudeQuelle quelle, string gebaeudename, string gebaeudekennung,
                                    char? baualtersklasse, string zonenregel, string zonenvorschlag,
                                    List<GebaeudeFeldzeile> zeilen, List<PruefMeldung> meldungen,
                                    List<GebaeudeQuellzuordnung> zuordnungen)
        {
            Quelle = quelle;
            Gebaeudename = gebaeudename ?? "";
            Gebaeudekennung = gebaeudekennung ?? "";
            Baualtersklasse = baualtersklasse;
            Zonenregel = zonenregel ?? "";
            Zonenvorschlag = zonenvorschlag ?? "";
            _zeilen = zeilen ?? new List<GebaeudeFeldzeile>();
            _meldungen = meldungen ?? new List<PruefMeldung>();
            _zuordnungen = zuordnungen ?? new List<GebaeudeQuellzuordnung>();
        }

        /// <summary>Die Quelle des Laufs (Datei, Hash, Größe, Schema …).</summary>
        public GebaeudeQuelle Quelle { get; }

        /// <summary>Der Gebäudename aus der Datei; leer, wenn sie keinen trägt.</summary>
        public string Gebaeudename { get; }

        /// <summary>Die Gebäudekennung aus der Datei, ungekürzt.</summary>
        public string Gebaeudekennung { get; }

        /// <summary>Die gewählte Baualtersklasse (A…U); <c>null</c> = keine — dann gibt es keine Vorgaben.</summary>
        public char? Baualtersklasse { get; }

        /// <summary>Die angewandte Zonenregel (<c>X4</c> in G4c).</summary>
        public string Zonenregel { get; }

        /// <summary>Die Regel, die der Leser vorschlüge (<c>X1</c>, <c>X2</c> oder <c>X4</c>) — nur Auskunft, in G4c nicht wählbar.</summary>
        public string Zonenvorschlag { get; }

        /// <summary>Je Zielfeld eine Zeile, in der Reihenfolge von <see cref="GebaeudeZielfelder.Alle"/>.</summary>
        public IReadOnlyList<GebaeudeFeldzeile> Zeilen => _zeilen;

        /// <summary>Lese- und Zuordnungsmeldungen dieses Gebäudes samt der dateiweiten.</summary>
        public IReadOnlyList<PruefMeldung> Meldungen => _meldungen;

        /// <summary>Die Paarungen Quellentität ↔ Gebäude für <c>Tab_Importzuordnung</c>.</summary>
        public IReadOnlyList<GebaeudeQuellzuordnung> Quellzuordnungen => _zuordnungen;

        /// <summary>
        /// Eine benannte Ablehnung des ganzen Satzes (etwa zu viele Zonen, Datenaustauschkonzept 3.3);
        /// <c>null</c> = keine. Sie hängt an keiner Zeile und sperrt deshalb unabhängig von den Haken
        /// (<see cref="GebaeudeImportAblauf.Pruefen"/>).
        /// </summary>
        public PruefMeldung Ablehnung { get; internal set; }

        /// <summary>Die Zeile zu einem Zielfeld; <c>null</c>, wenn es sie nicht gibt.</summary>
        public GebaeudeFeldzeile Zeile(string zielfeld)
        {
            foreach (GebaeudeFeldzeile z in _zeilen)
                if (string.Equals(z.Zielfeld, zielfeld, StringComparison.Ordinal)) return z;
            return null;
        }
    }
}
