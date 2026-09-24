using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Quelle eines Gebäudeimportlaufs</b> — was später genau eine Zeile in
    /// <c>Tab_Importquelle</c> wird (Datenaustauschkonzept 2.3, 7.1). Geschrieben wird sie in
    /// Stufe G4c Welle 3 (Schemaschritt S-F); hier entsteht sie beim Lesen und reist mit dem
    /// <see cref="GebaeudeImportSatz"/>.
    ///
    /// <para><b>Drei Festlegungen aus 2.3:</b> nur der Dateiname, nie der Pfad (iOS-Sandboxpfade
    /// tragen eine GUID, die bei jeder Neuinstallation wechselt); SHA-256 des Dateiinhalts,
    /// hexadezimal klein, 64 Zeichen — er beantwortet beim Round-Trip die einzige Frage, die
    /// zählt: <i>dieselbe Datei?</i>; und der Zeitpunkt invariant nach ISO 8601.</para>
    ///
    /// <para>Unveränderlich; ohne Datenbank.</para>
    /// </summary>
    internal sealed class GebaeudeQuelle
    {
        /// <summary>Persistenzwert des Formats gbXML (<c>Tab_Importquelle.Format</c>).</summary>
        public const string FORMAT_GBXML = "GBXML";

        /// <summary>Persistenzwert des Formats IFC (<c>Tab_Importquelle.Format</c>).</summary>
        public const string FORMAT_IFC = "IFC";

        /// <summary>Legt eine Quelle an; <paramref name="dateiname"/> darf einen Pfad tragen, er wird abgeschnitten.</summary>
        public GebaeudeQuelle(string format, string dateiname, string hash, long groesse, string schemastand,
                              string zeitpunkt, string programmfassung, string zonenregel, int fehlendeEntitaeten)
        {
            Format = format ?? "";
            Dateiname = NurName(dateiname);
            Hash = hash ?? "";
            Groesse = groesse;
            Schemastand = schemastand;
            Zeitpunkt = zeitpunkt ?? "";
            Programmfassung = programmfassung;
            Zonenregel = zonenregel;
            FehlendeEntitaeten = fehlendeEntitaeten;
        }

        /// <summary><see cref="FORMAT_GBXML"/> oder <see cref="FORMAT_IFC"/> — Persistenzwert, kein Anzeigetext.</summary>
        public string Format { get; }

        /// <summary>Nur der Dateiname, nie der Pfad (2.3).</summary>
        public string Dateiname { get; }

        /// <summary>SHA-256 des Dateiinhalts, hexadezimal klein, 64 Zeichen.</summary>
        public string Hash { get; }

        /// <summary>Größe der Datei in Byte.</summary>
        public long Groesse { get; }

        /// <summary>Der gelesene Schema- bzw. Versionswert, wie er in der Datei steht (gbXML: <c>@version</c>); <c>null</c> = keiner.</summary>
        public string Schemastand { get; }

        /// <summary>Zeitpunkt des Laufs nach ISO 8601, invariant formatiert (<c>yyyy-MM-ddTHH:mm:sszzz</c>).</summary>
        public string Zeitpunkt { get; }

        /// <summary>EPOS-Programmfassung des Laufs; <c>null</c> = unbekannt.</summary>
        public string Programmfassung { get; }

        /// <summary>Die Zonenregel des Laufs (<c>X4</c> in G4c, später <c>X1…X4</c> bzw. <c>Z1…Z5</c>).</summary>
        public string Zonenregel { get; }

        /// <summary>
        /// Zahl der beim Lesen verlorenen Entitäten (IFC: Summe beider Verlustkanäle, 7.1); gbXML
        /// kennt keinen Verlustkanal und führt 0.
        /// </summary>
        public int FehlendeEntitaeten { get; }

        /// <summary>
        /// Schneidet jeden Pfadanteil ab — unter Windows <c>\</c>, unter iOS und Linux <c>/</c>, und
        /// zwar unabhängig von der Plattform, auf der der Kern gerade läuft
        /// (<see cref="System.IO.Path.GetFileName(string)"/> kennt je Plattform nur ihr Trennzeichen).
        /// </summary>
        public static string NurName(string dateiname)
        {
            if (string.IsNullOrEmpty(dateiname)) return "";
            int trenner = Math.Max(dateiname.LastIndexOf('/'), dateiname.LastIndexOf('\\'));
            return trenner < 0 ? dateiname : dateiname.Substring(trenner + 1);
        }
    }
}
