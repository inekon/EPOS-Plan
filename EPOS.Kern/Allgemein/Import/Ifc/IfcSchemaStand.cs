using Xbim.Common.Step21;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schemastand einer IFC-Datei</b>, wie der Leser ihn annimmt (Umsetzungskonzept 3.2,
    /// Schritt 4; 3.3): genau die drei Schemata, die <c>Xbim.IO.MemoryModel</c> mitbringt. Werte, keine
    /// Anzeigetexte.
    /// </summary>
    internal enum IfcSchemaStand
    {
        /// <summary>Nicht angenommen: <c>Unsupported</c>, <c>Cobie2X4</c>, <c>Ifc4x1</c> oder kein Kopf.</summary>
        Unbekannt = 0,
        /// <summary>IFC2X3.</summary>
        Ifc2x3 = 1,
        /// <summary>IFC4 (samt ADD1/ADD2).</summary>
        Ifc4 = 2,
        /// <summary>IFC4X3 (samt ADD2).</summary>
        Ifc4x3 = 3,
    }

    /// <summary>Die Abbildung der xBIM-Schemafassung auf <see cref="IfcSchemaStand"/>.</summary>
    internal static class IfcSchemaStaende
    {
        /// <summary>
        /// Angenommen werden genau <c>Ifc2X3</c>, <c>Ifc4</c> und <c>Ifc4x3</c>; <c>Ifc4x1</c>,
        /// <c>Cobie2X4</c> und <c>Unsupported</c> ergeben <see cref="IfcSchemaStand.Unbekannt"/> — der
        /// Leser lehnt dann mit <c>IMP_IFC_PROT_SCHEMA_UNBEKANNT</c> ab.
        /// </summary>
        public static IfcSchemaStand AusXbim(XbimSchemaVersion fassung)
        {
            switch (fassung)
            {
                case XbimSchemaVersion.Ifc2X3: return IfcSchemaStand.Ifc2x3;
                case XbimSchemaVersion.Ifc4: return IfcSchemaStand.Ifc4;
                case XbimSchemaVersion.Ifc4x3: return IfcSchemaStand.Ifc4x3;
                default: return IfcSchemaStand.Unbekannt;
            }
        }

        /// <summary>Die xBIM-Fassung zu einem angenommenen Stand (für die Fabrikwahl).</summary>
        public static XbimSchemaVersion NachXbim(IfcSchemaStand stand)
        {
            switch (stand)
            {
                case IfcSchemaStand.Ifc2x3: return XbimSchemaVersion.Ifc2X3;
                case IfcSchemaStand.Ifc4: return XbimSchemaVersion.Ifc4;
                case IfcSchemaStand.Ifc4x3: return XbimSchemaVersion.Ifc4x3;
                default: return XbimSchemaVersion.Unsupported;
            }
        }

        /// <summary>Die Kurzform für Anzeige und Persistenz (<c>IFC2X3</c>, <c>IFC4</c>, <c>IFC4X3</c>); <c>null</c> = unbekannt.</summary>
        public static string Kurzform(IfcSchemaStand stand)
        {
            switch (stand)
            {
                case IfcSchemaStand.Ifc2x3: return "IFC2X3";
                case IfcSchemaStand.Ifc4: return "IFC4";
                case IfcSchemaStand.Ifc4x3: return "IFC4X3";
                default: return null;
            }
        }
    }
}
