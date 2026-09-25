namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Profil des gbXML-Imports</b> (Stufe G4c): Format <c>GBXML</c>, Dateifilter
    /// <c>*.xml;*.gbxml</c>, Größengrenze je Plattform (D15), als einzige Zonierungsregel X4 und der
    /// Leser <see cref="GbxmlLeser"/>.
    ///
    /// <para><b>Die Größengrenze</b> ist mit D15 gesetzt: 25 MB unter Windows, 10 MB auf iOS,
    /// benannt abgelehnt statt versucht — die größte gemessene Datei hat 16,3 MB und 648 885 Knoten
    /// (Befund R 4.3), und ein vollständiges <c>XDocument</c> kostet ein Vielfaches davon an
    /// Arbeitsspeicher. Die iOS-Zahl ist zu messen, nicht zu schätzen. Die Hülle belegt
    /// <see cref="GebaeudeImportProfil.MaxBytes"/> je Plattform mit einer der beiden Konstanten.</para>
    /// </summary>
    internal sealed class GbxmlImportProfil : GebaeudeImportProfil
    {
        /// <summary>Größengrenze unter Windows: 25 MB (D15), Megabyte zu 1 024 × 1 024 Byte.</summary>
        public const long MAX_BYTES_WINDOWS = 25L * 1024 * 1024;

        /// <summary>Größengrenze auf iOS: 10 MB (D15) — zu messen, nicht zu schätzen.</summary>
        public const long MAX_BYTES_IOS = 10L * 1024 * 1024;

        /// <summary>Dateifilter des Wählers.</summary>
        public const string DATEIFILTER = "(*.xml;*.gbxml)|*.xml;*.gbxml";

        /// <summary>Präfix der gbXML-Meldungsschlüssel (Datenaustauschkonzept 3.8).</summary>
        public const string MELDUNGSPRAEFIX = "IMP_GBXML_PROT_";

        /// <summary>
        /// Bereichsschlüssel des Infoknopfs — die Hilfeseite des Gebäudeeditors, in dem der Import als
        /// Überlagerung steht (A17). Eine eigene Seite samt Anker in <c>help_mapping.txt</c> kommt mit
        /// der Welle, die den Import im Gebäudedialog anbindet (Wiki-Quelle, <c>HelpMappingAnkerWacheTests</c>).
        /// </summary>
        public const string HILFESCHLUESSEL = "Form_Gebaeude1.btn_Help";

        /// <summary>Legt das Profil an; ohne Angabe gilt die Windows-Grenze.</summary>
        public GbxmlImportProfil(long maxBytes = MAX_BYTES_WINDOWS)
            : base(GebaeudeQuelle.FORMAT_GBXML, DATEIFILTER, maxBytes, new[] { ZONENREGEL_X4 },
                   HILFESCHLUESSEL, MELDUNGSPRAEFIX, "GIMP_SCHEMA_GBXML")
        {
        }

        /// <summary>25 MB unter Windows, 10 MB auf iOS (D15).</summary>
        public override long GrenzeFuerPlattform(bool ios) => ios ? MAX_BYTES_IOS : MAX_BYTES_WINDOWS;

        /// <summary>Ein neuer <see cref="GbxmlLeser"/> je Lauf.</summary>
        public override IGebaeudeLeser LeserErzeugen() => new GbxmlLeser();
    }
}
