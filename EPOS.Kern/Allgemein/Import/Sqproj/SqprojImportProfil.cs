namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Profil des Gebäudeimports allein aus der Projektdatei</b> (<c>.sqproj</c>, Importoption „nur Projektdatei“ neben
    /// „IFC“ und „IFC + Projektdatei“): Format <see cref="GebaeudeQuelle.FORMAT_SQPROJ"/>, Dateifilter <c>*.sqproj</c>, die
    /// Größengrenze der Projektdatei (<see cref="SqprojProfil"/>, losgelöst von der IFC-Grenze), als Zonierungsregel X4 und der
    /// Leser <see cref="SqprojGebaeudeLeser"/>.
    ///
    /// <para><b>Die Größengrenze</b> ist die der dazugeladenen Projektdatei: Die Größe stammt aus eingebetteten Bildern, die
    /// nie gelesen werden; die Hülle belegt <see cref="GebaeudeImportProfil.MaxBytes"/> je Plattform über
    /// <see cref="GrenzeFuerPlattform"/>.</para>
    /// </summary>
    internal sealed class SqprojImportProfil : GebaeudeImportProfil
    {
        /// <summary>Dateifilter des Wählers.</summary>
        public const string DATEIFILTER = "(*.sqproj)|*.sqproj";

        /// <summary>Präfix der Meldungsschlüssel des Formats (Datenaustauschkonzept 3.8).</summary>
        public const string MELDUNGSPRAEFIX = SqprojGebaeudeLeser.PRAEFIX;

        /// <summary>Bereichsschlüssel des Infoknopfs — die Hilfeseite „Gebäudeimport“ wie bei IFC und gbXML.</summary>
        public const string HILFESCHLUESSEL = HILFE_ZUORDNUNG;

        /// <summary>Ressourcenschlüssel der Schemaanzeige (Platzhalter <c>{0}</c> = Fassung der Raumtabelle).</summary>
        public const string SCHEMAANZEIGE = "GIMP_SCHEMA_SQPROJ";

        /// <summary>Legt das Profil an; ohne Angabe gilt die Windows-Grenze der Projektdatei.</summary>
        public SqprojImportProfil(long maxBytes = SqprojProfil.MAX_BYTES)
            : base(GebaeudeQuelle.FORMAT_SQPROJ, DATEIFILTER, maxBytes, new[] { ZONENREGEL_X4 },
                   HILFESCHLUESSEL, MELDUNGSPRAEFIX, SCHEMAANZEIGE)
        {
        }

        /// <summary>Die Grenze der Projektdatei je Plattform (<see cref="SqprojProfil.GrenzeFuerPlattform"/>).</summary>
        public override long GrenzeFuerPlattform(bool ios) => SqprojProfil.GrenzeFuerPlattform(ios);

        /// <summary>Ein neuer <see cref="SqprojGebaeudeLeser"/> je Lauf.</summary>
        public override IGebaeudeLeser LeserErzeugen() => new SqprojGebaeudeLeser();
    }
}
