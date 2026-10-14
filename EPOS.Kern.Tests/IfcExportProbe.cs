using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorrichtung der IFC-Exportproben</b> (Stufe G7c): Profil mit fester Uhr und Format IFC,
    /// Schreiben in den Speicher, Zurücklesen als Modell (xBIM) und über den eigenen
    /// <see cref="IfcLeser"/>, die Eigenschaften eines Objekts nach Satz und Name.
    /// </summary>
    internal static class IfcExportProbe
    {
        /// <summary>Ein Profil mit fester Uhr im Format IFC.</summary>
        internal static GebaeudeExportProfil Profil(DateTime? zeit = null, string sprache = "de-DE")
        {
            DateTime t = zeit ?? GbxmlExportProbe.Zeitpunkt;
            return new GebaeudeExportProfil(CultureInfo.GetCultureInfo(sprache), () => t, false, GbxmlExportProbe.VERSION,
                                            format: GebaeudeQuelle.FORMAT_IFC);
        }

        /// <summary>Schreibt ein Abbild in den Speicher.</summary>
        internal static byte[] Schreiben(GebaeudeAbbild abbild, GebaeudeExportProfil profil = null, IfcErgebnisse ergebnisse = null)
            => Schreiben(abbild, profil, ergebnisse, null, out _);

        /// <summary>Schreibt ein Abbild in den Speicher und liefert die Bilanz; <paramref name="eingriff"/> greift vor der Prüfung ins Modell.</summary>
        internal static byte[] Schreiben(GebaeudeAbbild abbild, GebaeudeExportProfil profil, IfcErgebnisse ergebnisse,
                                         Action<IModel> eingriff, out GebaeudeExportBilanz bilanz)
        {
            using (var ziel = new MemoryStream())
            {
                var schreiber = new IfcSchreiber(ergebnisse) { VorDerPruefung = eingriff };
                bilanz = schreiber.Schreiben(abbild, ziel, profil ?? Profil(), CancellationToken.None);
                return ziel.ToArray();
            }
        }

        /// <summary>Liest eine geschriebene Datei als Modell zurück.</summary>
        internal static MemoryModel Modell(byte[] datei)
        {
            var m = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4(), NullLoggerFactory.Instance, 0);
            using (var s = new MemoryStream(datei, false))
                m.LoadStep21(s, datei.LongLength, null, null);
            return m;
        }

        /// <summary>Liest eine geschriebene Datei mit dem eigenen IFC-Leser.</summary>
        internal static GebaeudeAbbild Lesen(byte[] datei)
            => new IfcLeser().Lesen(new MemoryStream(datei, false), new IfcImportProfil(), null, CancellationToken.None);

        /// <summary>Alle Einzelwerte eines Objekts: Satzname → Eigenschaftsname → Wert.</summary>
        internal static Dictionary<string, Dictionary<string, IIfcPropertySingleValue>> Eigenschaften(IIfcObject o)
        {
            var r = new Dictionary<string, Dictionary<string, IIfcPropertySingleValue>>(StringComparer.Ordinal);
            foreach (IIfcRelDefinesByProperties rel in o.IsDefinedBy)
                if (rel.RelatingPropertyDefinition is IIfcPropertySet satz)
                    r[satz.Name.ToString()] = satz.HasProperties.OfType<IIfcPropertySingleValue>().ToDictionary(p => p.Name.ToString(), StringComparer.Ordinal);
            return r;
        }

        /// <summary>Ein Zahlenwert einer Eigenschaft.</summary>
        internal static double Zahl(IIfcPropertySingleValue p) => Convert.ToDouble(p.NominalValue.Value, CultureInfo.InvariantCulture);

        /// <summary>Alle Mengen eines Objekts: Mengenname → Wert.</summary>
        internal static Dictionary<string, double> Mengen(IIfcObject o, string satzname)
        {
            var r = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (IIfcRelDefinesByProperties rel in o.IsDefinedBy)
                if (rel.RelatingPropertyDefinition is IIfcElementQuantity q && q.Name == satzname)
                    foreach (IIfcPhysicalQuantity m in q.Quantities)
                    {
                        if (m is IIfcQuantityArea a) r[a.Name] = a.AreaValue;
                        else if (m is IIfcQuantityLength l) r[l.Name] = l.LengthValue;
                        else if (m is IIfcQuantityVolume v) r[v.Name] = v.VolumeValue;
                    }
            return r;
        }
    }
}
