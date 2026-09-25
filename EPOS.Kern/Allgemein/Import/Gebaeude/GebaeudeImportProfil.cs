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

        /// <summary>
        /// Der Hilfeschlüssel des Zuordnungsdialogs — beide Formate zeigen auf dieselbe Wiki-Seite
        /// „Gebäudeimport" (Zeile in <c>help_mapping.txt</c>, Wache <c>HelpMappingAnkerWacheTests</c>).
        /// </summary>
        public const string HILFE_ZUORDNUNG = "Form_GebaeudeImport.btn_Help";

        /// <summary>
        /// <b>Der gemeinsame Dateifilter</b> des Einstiegs im Gebäudedialog: EINE Dateiwahl für beide
        /// Formate; das Profil folgt danach aus der Endung (<see cref="FuerDatei"/>).
        /// </summary>
        public const string DATEIFILTER_ALLE =
            "gbXML, IFC (*.xml;*.gbxml;*.ifc;*.ifcxml;*.ifczip)|*.xml;*.gbxml;*.ifc;*.ifcxml;*.ifczip";

        /// <summary>
        /// <b>Das Profil einer Datei nach ihrer Endung</b> — <c>.ifc</c>, <c>.ifcxml</c> und
        /// <c>.ifczip</c> sind IFC, <c>.xml</c> und <c>.gbxml</c> gbXML, Groß- und Kleinschreibung
        /// gleich; ein Pfadanteil zählt nicht. Jede andere Endung ergibt <c>null</c> — der Aufrufer
        /// lehnt dann benannt ab („Dateiart nicht unterstützt"). Das neue Profil trägt die
        /// Windows-Grenze; die Hülle belegt sie danach je Plattform
        /// (<see cref="GrenzeFuerPlattform"/>).
        /// </summary>
        public static GebaeudeImportProfil FuerDatei(string dateiname)
        {
            switch (Endung(dateiname))
            {
                case ".ifc":
                case ".ifcxml":
                case ".ifczip":
                    return new IfcImportProfil();
                case ".xml":
                case ".gbxml":
                    return new GbxmlImportProfil();
                default:
                    return null;
            }
        }

        /// <summary>Die Endung eines Dateinamens klein und mit Punkt (<c>.ifc</c>); ohne Endung leer.</summary>
        public static string Endung(string dateiname)
        {
            string name = GebaeudeQuelle.NurName(dateiname);
            int punkt = name.LastIndexOf('.');
            return punkt < 0 ? "" : name.Substring(punkt).ToLowerInvariant();
        }

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

        /// <summary>
        /// Die Grenze des Formats für eine Plattform — die Hülle belegt damit <see cref="MaxBytes"/>
        /// (<c>OperatingSystem.IsIOS()</c>). Ohne eigene Konstanten gilt die angelegte Grenze.
        /// </summary>
        public virtual long GrenzeFuerPlattform(bool ios) => MaxBytes;

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

        /// <summary>
        /// Die Vorgabe der Raumhöhe [m], wenn die Datei weder eine Höhe noch Volumen und Fläche der
        /// beheizten Räume trägt; <c>null</c> = keine — dann bleibt die Zeile leer (gbXML:
        /// „sonst NULL = Wert des Gebäudes", Datenaustauschkonzept 3.4). Die Regel gehört dem Format,
        /// deshalb steht sie im Profil; die Zuordnung setzt sie mit Herkunft „Vorgabe" und Beleg.
        /// </summary>
        public virtual double? RueckfallRaumhoeheM => null;

        /// <summary>
        /// Greifen die Vorgabe-Rückfälle der Hüllflächen (Umsetzungskonzept 3.4, Spalte „Rückfall"):
        /// Dachfläche → Grundfläche des obersten Geschosses, Grundfläche → Nutzfläche ÷ Geschosszahl,
        /// sonstige Flächen → 0 — jeweils nur, wenn für die Gruppe KEINE Fläche gelesen ist? gbXML
        /// liefert die Flächen aus der Geometrie und kennt diese Rückfälle nicht (<c>false</c>).
        /// </summary>
        public virtual bool FlaechenRueckfaelle => false;

        /// <summary>Die Leserfabrik: ein neuer Leser je Lauf.</summary>
        public abstract IGebaeudeLeser LeserErzeugen();
    }
}
