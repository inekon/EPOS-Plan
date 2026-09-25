namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Profil des gbXML-Imports</b> (Stufe G4c): Format <c>GBXML</c>, Dateifilter
    /// <c>*.xml;*.gbxml</c>, Größengrenze je Plattform (D15), als einzige Zonierungsregel X4 und der
    /// Leser <see cref="GbxmlLeser"/>.
    ///
    /// <para><b>Die Größengrenze</b> ist mit D15 gesetzt und mit E42 für iOS angehoben: 25 MB unter Windows und auf iOS,
    /// benannt abgelehnt statt versucht — die größte gemessene Datei hat 16,3 MB und 648 885 Knoten
    /// (Befund R 4.3), und ein vollständiges <c>XDocument</c> kostet ein Vielfaches davon an
    /// Arbeitsspeicher. Gemessen im iOS-Lauf zur Abnahme von G4a (getrimmt, AOT): rund 7,5 MB
    /// Prozessspeicher je MB Datei, 25 MB kosten also rund 190 MB. Die Hülle belegt
    /// <see cref="GebaeudeImportProfil.MaxBytes"/> je Plattform mit einer der beiden Konstanten.</para>
    /// </summary>
    internal sealed class GbxmlImportProfil : GebaeudeImportProfil
    {
        /// <summary>Größengrenze unter Windows: 25 MB (D15), Megabyte zu 1 024 × 1 024 Byte.</summary>
        public const long MAX_BYTES_WINDOWS = 25L * 1024 * 1024;

        /// <summary>Größengrenze auf iOS: 25 MB (E42, nach der Messung im iOS-Lauf zur Abnahme von G4a).</summary>
        public const long MAX_BYTES_IOS = 25L * 1024 * 1024;

        /// <summary>Dateifilter des Wählers.</summary>
        public const string DATEIFILTER = "(*.xml;*.gbxml)|*.xml;*.gbxml";

        /// <summary>Präfix der gbXML-Meldungsschlüssel (Datenaustauschkonzept 3.8).</summary>
        public const string MELDUNGSPRAEFIX = "IMP_GBXML_PROT_";

        /// <summary>
        /// Bereichsschlüssel des Infoknopfs — die eigene Hilfeseite „Gebäudeimport" des
        /// Zuordnungsdialogs, dieselbe für beide Formate (<see cref="GebaeudeImportProfil.HILFE_ZUORDNUNG"/>).
        /// </summary>
        public const string HILFESCHLUESSEL = HILFE_ZUORDNUNG;

        /// <summary>Legt das Profil an; ohne Angabe gilt die Windows-Grenze.</summary>
        public GbxmlImportProfil(long maxBytes = MAX_BYTES_WINDOWS)
            : base(GebaeudeQuelle.FORMAT_GBXML, DATEIFILTER, maxBytes, new[] { ZONENREGEL_X4 },
                   HILFESCHLUESSEL, MELDUNGSPRAEFIX, "GIMP_SCHEMA_GBXML")
        {
        }

        /// <summary>25 MB unter Windows und auf iOS (D15, E42).</summary>
        public override long GrenzeFuerPlattform(bool ios) => ios ? MAX_BYTES_IOS : MAX_BYTES_WINDOWS;

        /// <summary>Ein neuer <see cref="GbxmlLeser"/> je Lauf.</summary>
        public override IGebaeudeLeser LeserErzeugen() => new GbxmlLeser();
    }
}
