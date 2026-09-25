using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>Woher eine Eigenschaft stammt: vom Vorkommnis oder vom Typ.</summary>
    internal enum IfcEigenschaftsquelle
    {
        /// <summary>Am Bauteil selbst (<c>IsDefinedBy</c>).</summary>
        Vorkommnis = 0,
        /// <summary>Am Typ (<c>IsTypedBy → RelatingType.HasPropertySets</c>).</summary>
        Typ = 1,
    }

    /// <summary>Eine gefundene Eigenschaft samt Quelle und Satzname.</summary>
    internal sealed class IfcFund
    {
        /// <summary>Legt einen Fund an.</summary>
        public IfcFund(IIfcProperty eigenschaft, IfcEigenschaftsquelle quelle, string satz)
        {
            Eigenschaft = eigenschaft;
            Quelle = quelle;
            Satz = satz ?? "";
        }

        /// <summary>Die Eigenschaft.</summary>
        public IIfcProperty Eigenschaft { get; }

        /// <summary>Vorkommnis oder Typ.</summary>
        public IfcEigenschaftsquelle Quelle { get; }

        /// <summary>Der Name des Eigenschaftssatzes, wie in der Datei.</summary>
        public string Satz { get; }
    }

    /// <summary>
    /// <b>Pset- und Mengenzugriff, selbst geschrieben über die Schnittstellen</b> (Umsetzungskonzept 3.4,
    /// ADR-003): Die bequemen <c>GetPropertySingleValue</c>/<c>GetElementQuantity</c> hängen an der
    /// IFC4-KLASSE <c>Xbim.Ifc4.Kernel.IfcObject</c> und wären schemagebunden; sie lesen auch keine
    /// Typ-Eigenschaften. Hier gilt für alle drei Schemata:
    ///
    /// <list type="bullet">
    /// <item><b>Menge beachten:</b> <c>IsDefinedBy → RelatingPropertyDefinition</c> ist ein SELECT, das ein
    /// <c>IfcPropertySetDefinitionSet</c> sein kann — gelesen wird über <c>PropertySetDefinitions</c>, nie
    /// über eine direkte Wandlung nach <c>IIfcPropertySet</c>.</item>
    /// <item><b>Vorkommnis vor Typ:</b> Fehlt die Eigenschaft am Bauteil, wird der Typ gelesen
    /// (<c>IsTypedBy → RelatingType.HasPropertySets</c>).</item>
    /// <item><b>Mengensatz über den Namen:</b> <c>IIfcElementQuantity.Name</c> ∈ { <c>BaseQuantities</c>,
    /// <c>Qto_&lt;Klasse&gt;BaseQuantities</c>, <c>Qto_&lt;Klasse&gt;Quantities</c> } (Groß-/Kleinschreibung
    /// egal), die Größe über <c>IIfcPhysicalSimpleQuantity.Name</c>.</item>
    /// </list>
    /// </summary>
    internal static class IfcEigenschaften
    {
        /// <summary>Sucht eine Eigenschaft: erst am Vorkommnis, dann am Typ; <c>null</c> = keine.</summary>
        public static IfcFund Finden(IIfcObject objekt, string satz, string name)
        {
            if (objekt == null) return null;
            IIfcProperty p = InSaetzen(Saetze(objekt), satz, name);
            if (p != null) return new IfcFund(p, IfcEigenschaftsquelle.Vorkommnis, satz);
            foreach (IIfcRelDefinesByType rel in objekt.IsTypedBy ?? Enumerable.Empty<IIfcRelDefinesByType>())
            {
                IIfcTypeObject typ = rel?.RelatingType;
                if (typ?.HasPropertySets == null) continue;
                p = InSaetzen(typ.HasPropertySets, satz, name);
                if (p != null) return new IfcFund(p, IfcEigenschaftsquelle.Typ, satz);
            }
            return null;
        }

        /// <summary>Die Eigenschaftssätze eines Vorkommnisses — Mengen (<c>IfcPropertySetDefinitionSet</c>) aufgelöst.</summary>
        public static IEnumerable<IIfcPropertySetDefinition> Saetze(IIfcObject objekt)
        {
            if (objekt?.IsDefinedBy == null) yield break;
            foreach (IIfcRelDefinesByProperties rel in objekt.IsDefinedBy)
            {
                IIfcPropertySetDefinitionSelect auswahl = rel?.RelatingPropertyDefinition;
                if (auswahl?.PropertySetDefinitions == null) continue;
                foreach (IIfcPropertySetDefinition d in auswahl.PropertySetDefinitions)
                    if (d != null) yield return d;
            }
        }

        /// <summary>Die Eigenschaftssätze eines Kontexts (Projekt) — derselbe Weg wie am Objekt.</summary>
        public static IEnumerable<IIfcPropertySetDefinition> Saetze(IIfcContext kontext)
        {
            if (kontext?.IsDefinedBy == null) yield break;
            foreach (IIfcRelDefinesByProperties rel in kontext.IsDefinedBy)
            {
                IIfcPropertySetDefinitionSelect auswahl = rel?.RelatingPropertyDefinition;
                if (auswahl?.PropertySetDefinitions == null) continue;
                foreach (IIfcPropertySetDefinition d in auswahl.PropertySetDefinitions)
                    if (d != null) yield return d;
            }
        }

        private static IIfcProperty InSaetzen(IEnumerable<IIfcPropertySetDefinition> saetze, string satz, string name)
        {
            foreach (IIfcPropertySet ps in saetze.OfType<IIfcPropertySet>())
            {
                if (!Gleich(Text(ps.Name), satz)) continue;
                foreach (IIfcProperty p in ps.HasProperties)
                    if (p != null && Gleich(p.Name.ToString(), name)) return p;
            }
            return null;
        }

        /// <summary>Hat das Objekt (Vorkommnis) einen Eigenschaftssatz dieses Namens?</summary>
        public static bool HatSatz(IIfcObject objekt, string satz)
            => Saetze(objekt).OfType<IIfcPropertySet>().Any(ps => Gleich(Text(ps.Name), satz));

        // ==================================================================
        //  Mengen
        // ==================================================================

        /// <summary>
        /// Eine Menge des Vorkommnisses in SI (m, m², m³): der Wert mal dem Faktor der Einheit — der eigenen
        /// Einheit der Menge, sonst der des Projekts. <c>null</c> = nicht vorhanden.
        /// </summary>
        public static double? Menge(IIfcObject objekt, string klasse, string name, IfcEinheiten einheiten)
        {
            IIfcPhysicalSimpleQuantity q = MengeFinden(objekt, klasse, name);
            if (q == null) return null;
            switch (q)
            {
                case IIfcQuantityArea a:
                    return Wert(a.AreaValue) * (EigenerFaktor(q, 2) ?? einheiten.Flaeche);
                case IIfcQuantityLength l:
                    return Wert(l.LengthValue) * (EigenerFaktor(q, 1) ?? einheiten.Laenge);
                case IIfcQuantityVolume v:
                    return Wert(v.VolumeValue) * (EigenerFaktor(q, 3) ?? einheiten.Volumen);
                default:
                    return null;
            }
        }

        /// <summary>Trägt das Objekt überhaupt einen passenden Mengensatz?</summary>
        public static bool HatMengensatz(IIfcObject objekt, string klasse)
            => Saetze(objekt).OfType<IIfcElementQuantity>().Any(q => IstMengensatz(Text(q.Name), klasse));

        private static IIfcPhysicalSimpleQuantity MengeFinden(IIfcObject objekt, string klasse, string name)
        {
            foreach (IIfcElementQuantity satz in Saetze(objekt).OfType<IIfcElementQuantity>())
            {
                if (!IstMengensatz(Text(satz.Name), klasse)) continue;
                foreach (IIfcPhysicalSimpleQuantity q in satz.Quantities.OfType<IIfcPhysicalSimpleQuantity>())
                    if (Gleich(q.Name.ToString(), name)) return q;
            }
            return null;
        }

        /// <summary>Ist das der Mengensatz einer Klasse (<c>BaseQuantities</c>, <c>Qto_WallBaseQuantities</c> …)?</summary>
        internal static bool IstMengensatz(string satzname, string klasse)
            => Gleich(satzname, "BaseQuantities")
               || Gleich(satzname, "Qto_" + klasse + "BaseQuantities")
               || Gleich(satzname, "Qto_" + klasse + "Quantities");

        private static double? EigenerFaktor(IIfcPhysicalSimpleQuantity q, int potenz)
            => q.Unit == null ? null : IfcEinheiten.Faktor(q.Unit, potenz, "", null);

        // ==================================================================
        //  Werte
        // ==================================================================

        /// <summary>Der Zahlenwert eines Werts (Maß, Real, Integer); <c>null</c> = keine Zahl.</summary>
        public static double? Zahl(IIfcValue wert) => wert is IExpressValueType e ? Zahl(e.Value) : null;

        /// <summary>Der Zahlenwert eines Maßtyps (<c>IExpressValueType.Value</c>); <c>NaN</c> = keiner.</summary>
        public static double Wert(IExpressValueType wert) => wert == null ? double.NaN : Zahl(wert.Value) ?? double.NaN;

        /// <summary>Der Zahlenwert eines optionalen Maßtyps; <c>NaN</c> = keiner.</summary>
        public static double Wert<T>(T? wert) where T : struct, IExpressValueType => wert.HasValue ? Wert(wert.Value) : double.NaN;

        private static double? Zahl(object roh)
        {
            switch (roh)
            {
                case double d: return double.IsNaN(d) || double.IsInfinity(d) ? (double?)null : d;
                case float f: return f;
                case long l: return l;
                case int i: return i;
                case decimal m: return (double)m;
                default: return null;
            }
        }

        /// <summary>Ein Wahrheitswert (<c>IfcBoolean</c>, <c>IfcLogical</c>); <c>null</c> = keiner oder UNKNOWN.</summary>
        public static bool? Wahrheit(IIfcValue wert)
        {
            object roh = (wert as IExpressValueType)?.Value;
            if (roh is bool b) return b;
            return null;
        }

        /// <summary>Der Text eines Werts (<c>IfcLabel</c>, <c>IfcText</c>, <c>IfcIdentifier</c>); <c>null</c> = keiner.</summary>
        public static string Textwert(IIfcValue wert)
        {
            object roh = (wert as IExpressValueType)?.Value;
            if (roh is string s) return s;
            if (roh == null) return null;
            return Convert.ToString(roh, CultureInfo.InvariantCulture);
        }

        /// <summary>Der Text eines optionalen Bezeichners.</summary>
        public static string Text<T>(T? wert) where T : struct => wert.HasValue ? wert.Value.ToString() : null;

        /// <summary>Groß-/Kleinschreibung zählt nicht.</summary>
        public static bool Gleich(string a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
    }
}
