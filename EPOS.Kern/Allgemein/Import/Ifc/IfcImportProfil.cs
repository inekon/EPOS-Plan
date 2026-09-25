namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Profil des IFC-Imports</b> (Stufe G4a; Umsetzungskonzept 3.3, Softwarearchitektur 1.5):
    /// Format <c>IFC</c>, Dateifilter <c>*.ifc;*.ifcxml;*.ifczip</c>, Größengrenze je Plattform (U11),
    /// als einzige Zonierungsregel der Einzonen-Weg Z5 und der Leser <see cref="IfcLeser"/>.
    ///
    /// <para><b>Die Größengrenze</b> ist mit E18/U11 gesetzt: 50 MB unter Windows, 20 MB auf iOS,
    /// benannt abgelehnt statt versucht — <c>MemoryModel</c> hält das ganze Modell im Arbeitsspeicher,
    /// Faustzahl 10 bis 20 MB je MB STEP-Text. Die iOS-Zahl ist geschätzt und in G4-8 zu messen. Die
    /// Hülle belegt <see cref="GebaeudeImportProfil.MaxBytes"/> je Plattform mit einer der beiden
    /// Konstanten. Bei <c>.ifczip</c> gilt die Grenze für die ENTPACKTE Größe des IFC-Eintrags
    /// (Softwarearchitektur 1.5, Regel 2) — der Leser prüft sie im Zip-Verzeichnis, ohne zu entpacken.</para>
    /// </summary>
    internal sealed class IfcImportProfil : GebaeudeImportProfil
    {
        /// <summary>Größengrenze unter Windows: 50 MB (U11), Megabyte zu 1 024 × 1 024 Byte.</summary>
        public const long MAX_BYTES_WINDOWS = 50L * 1024 * 1024;

        /// <summary>Größengrenze auf iOS: 20 MB (U11) — geschätzt, in G4-8 zu messen.</summary>
        public const long MAX_BYTES_IOS = 20L * 1024 * 1024;

        /// <summary>Dateifilter des Wählers.</summary>
        public const string DATEIFILTER = "(*.ifc;*.ifcxml;*.ifczip)|*.ifc;*.ifcxml;*.ifczip";

        /// <summary>Präfix der IFC-Meldungsschlüssel (Befund N 4.5, Umsetzungskonzept 3.2).</summary>
        public const string MELDUNGSPRAEFIX = "IMP_IFC_PROT_";

        /// <summary>Zonenregel Z4 „je Geschoss" (Mehrzonenkonzept 6.5) — in G4a nur Vorschlag, nicht wählbar.</summary>
        public const string ZONENREGEL_Z4 = "Z4";

        /// <summary>Zonenregel Z5: eine Zone je Gebäude — der Einzonen-Weg auf <c>Tab_Gebaeude</c>, die einzige Regel in G4a.</summary>
        public const string ZONENREGEL_Z5 = "Z5";

        /// <summary>
        /// Bereichsschlüssel des Infoknopfs — die Hilfeseite des Gebäudeeditors, in dem der Import als
        /// Überlagerung steht (Umsetzungskonzept 3.1, A17); eine eigene Seite kommt mit der Oberfläche.
        /// </summary>
        public const string HILFESCHLUESSEL = "Form_Gebaeude1.btn_Help";

        /// <summary>Legt das Profil an; ohne Angabe gilt die Windows-Grenze.</summary>
        public IfcImportProfil(long maxBytes = MAX_BYTES_WINDOWS)
            : base(GebaeudeQuelle.FORMAT_IFC, DATEIFILTER, maxBytes, new[] { ZONENREGEL_Z5 },
                   HILFESCHLUESSEL, MELDUNGSPRAEFIX, "GIMP_SCHEMA_IFC")
        {
        }

        /// <summary>Die Vorgabe der Raumhöhe ohne <c>Height</c> und ohne <c>NetVolume</c>/<c>NetFloorArea</c> [m] (Umsetzungskonzept 3.4).</summary>
        public const double RUECKFALL_RAUMHOEHE_M = 2.5;

        /// <summary>Ein neuer <see cref="IfcLeser"/> je Lauf.</summary>
        public override IGebaeudeLeser LeserErzeugen() => new IfcLeser();

        /// <inheritdoc />
        public override double? RueckfallRaumhoeheM => RUECKFALL_RAUMHOEHE_M;

        /// <inheritdoc />
        public override bool FlaechenRueckfaelle => true;
    }
}
