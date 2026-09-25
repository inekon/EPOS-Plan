namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Importquelle — eine Zeile aus <c>Tab_Importquelle</c> (Schemaschritt S-F,
    /// <see cref="ImportzuordnungSchema"/>): die Datei eines Gebäudeimportlaufs.
    ///
    /// <para><b>Öffentliche Felder statt Eigenschaften</b> — Hausmuster aller <c>*Model</c>-Klassen:
    /// reine Datenträger zwischen Controller und Oberfläche. Die Feldnamen folgen den
    /// Spaltennamen. Geschrieben wird allein über <see cref="GebaeudeImportCtrl"/>.</para>
    /// </summary>
    public class ImportquelleModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>Das Projektgebäude des Laufs (<c>Tab_Gebaeude.ID</c>).</summary>
        public int ID_Gebaeude;

        /// <summary>Format (<see cref="DbWerte.IMPORT_FORMATE"/>) — Persistenzwert, kein Anzeigetext.</summary>
        public string Format = "";

        /// <summary>Nur der Dateiname, nie der Pfad.</summary>
        public string Dateiname = "";

        /// <summary>SHA-256 des Dateiinhalts, hexadezimal klein, 64 Zeichen.</summary>
        public string Hash = "";

        /// <summary>Größe der Datei [Byte].</summary>
        public long Groesse;

        /// <summary>Schema- bzw. Versionswert, wie gelesen; <c>null</c> = keiner.</summary>
        public string Schemastand;

        /// <summary>Zeitpunkt des Laufs, ISO 8601 invariant.</summary>
        public string Zeitpunkt = "";

        /// <summary>EPOS-Programmfassung des Laufs; <c>null</c> = unbekannt.</summary>
        public string Programmfassung;

        /// <summary>Zonenregel des Laufs; <c>null</c> = keine.</summary>
        public string Zonenregel;

        /// <summary>Beim Lesen verlorene Entitäten (IFC); &gt; 0 sperrt den Round-Trip.</summary>
        public int FehlendeEntitaeten;
    }
}
