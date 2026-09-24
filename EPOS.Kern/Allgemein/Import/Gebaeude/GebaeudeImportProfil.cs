using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Was den Gebäudeimport je Format unterscheidet — als DATEN</b> (Softwarearchitektur 1.5,
    /// Regel 3; Datenaustauschkonzept 2.4): Dateifilter, Größengrenze, Schemaanzeige, die Liste der
    /// Zonierungsregeln, Hilfeschlüssel und der Leser. Nichts davon steht im Quelltext des Dialogs.
    /// Muster <see cref="KatalogImportProfil"/>.
    ///
    /// <para><b>Die Größengrenze ist ein Datum des Profils, kein Glied der Plattformnaht</b>
    /// (Softwarearchitektur 1.5, Regel 2): Die abgeleiteten Profile führen je Plattform eine
    /// Konstante, die Hülle belegt <see cref="MaxBytes"/>. Abgelehnt wird benannt, vor dem Lesen.</para>
    /// </summary>
    internal abstract class GebaeudeImportProfil
    {
        /// <summary>Zonenregel X1: nach <c>Zone</c> (gbXML), mit G6c wählbar.</summary>
        public const string ZONENREGEL_X1 = "X1";

        /// <summary>Zonenregel X2: nach Geschoss (gbXML), mit G6c wählbar.</summary>
        public const string ZONENREGEL_X2 = "X2";

        /// <summary>Zonenregel X3: eine Zone je Raum (gbXML), mit G6c wählbar.</summary>
        public const string ZONENREGEL_X3 = "X3";

        /// <summary>Zonenregel X4: eine Zone je Gebäude — der Einzonen-Weg auf <c>Tab_Gebaeude</c>, die einzige Regel in G4c.</summary>
        public const string ZONENREGEL_X4 = "X4";

        /// <summary>Obergrenze der Zonenzahl je Gebäude (Mehrzonenkonzept 6.1, Frage M12).</summary>
        public const int MAX_ZONEN = 50;

        /// <summary>Legt ein Profil an.</summary>
        protected GebaeudeImportProfil(string format, string dateifilter, long maxBytes,
                                       IReadOnlyList<string> zonierungsregeln, string hilfeSchluessel,
                                       string meldungspraefix, string schemaanzeigeSchluessel)
        {
            Format = format ?? "";
            Dateifilter = dateifilter ?? "";
            MaxBytes = maxBytes;
            Zonierungsregeln = zonierungsregeln ?? Array.Empty<string>();
            HilfeSchluessel = hilfeSchluessel ?? "";
            Meldungspraefix = meldungspraefix ?? "";
            SchemaanzeigeSchluessel = schemaanzeigeSchluessel ?? "";
        }

        /// <summary>Persistenzwert des Formats (<see cref="GebaeudeQuelle.FORMAT_GBXML"/>, <see cref="GebaeudeQuelle.FORMAT_IFC"/>).</summary>
        public string Format { get; }

        /// <summary>Dateifilter des Wählers in der Schreibweise von <see cref="IDateiDienst.DateiOeffnen"/>.</summary>
        public string Dateifilter { get; }

        /// <summary>
        /// Größte zulässige Datei in Byte — die Hülle belegt sie je Plattform (Konstanten im
        /// abgeleiteten Profil). Eine größere Datei wird benannt abgelehnt, bevor sie gelesen wird.
        /// </summary>
        public long MaxBytes { get; set; }

        /// <summary>Die wählbaren Zonierungsregeln; die erste ist die Vorbelegung. In G4c nur <see cref="ZONENREGEL_X4"/>.</summary>
        public IReadOnlyList<string> Zonierungsregeln { get; }

        /// <summary>Die angewandte Zonenregel — die erste der Liste.</summary>
        public string Zonenregel => Zonierungsregeln.Count > 0 ? Zonierungsregeln[0] : ZONENREGEL_X4;

        /// <summary>Bereichsschlüssel des Infoknopfs.</summary>
        public string HilfeSchluessel { get; }

        /// <summary>
        /// Präfix der formatgebundenen Meldungsschlüssel (<c>IMP_GBXML_PROT_</c>, <c>IMP_IFC_PROT_</c>):
        /// Auch die Ablehnungen des gemeinsamen Ablaufs — zu groß, Lesefehler — tragen den Schlüssel
        /// ihres Formats (Datenaustauschkonzept 3.8).
        /// </summary>
        public string Meldungspraefix { get; }

        /// <summary>Ressourcenschlüssel der Schemaanzeige im Dialogkopf (Platzhalter <c>{0}</c> = gelesener Stand).</summary>
        public string SchemaanzeigeSchluessel { get; }

        /// <summary>Der Meldungsschlüssel dieses Formats zu einem Namen (<c>ZU_GROSS</c> → <c>IMP_GBXML_PROT_ZU_GROSS</c>).</summary>
        public string Meldung(string name) => Meldungspraefix + name;

        /// <summary>Die Leserfabrik: ein neuer Leser je Lauf.</summary>
        public abstract IGebaeudeLeser LeserErzeugen();
    }
}
