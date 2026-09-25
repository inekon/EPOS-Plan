using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Rückbeziehungen eines IFC-Modells als Index</b> — einmal je Modell aufgebaut, damit der Leser
    /// linear mit der Dateigröße geht.
    ///
    /// <para><b>Warum.</b> Eine Rückbeziehung von xBIM (<c>IsDefinedBy</c>, <c>IsTypedBy</c>,
    /// <c>HasAssociations</c>, <c>IsDecomposedBy</c>, <c>ContainsElements</c>, <c>HasOpenings</c>,
    /// <c>HasFillings</c>, <c>HasProperties</c>) ist ohne Zwischenspeicher eine Suche über ALLE Beziehungen
    /// ihres Typs im Modell. Der Leser fragt sie je Raum, Bauteil und Eigenschaft mehrmals — ohne Index
    /// ist das O(n²). Gemessen an den synthetischen Großfällen (<see cref="ImportmessungProben"/>, Windows,
    /// Release): ohne Index 2 MB ≈ 6 s, 8 MB ≈ 95 s, 20 MB über 5 min; mit Index 2 MB ≈ 0,4 s,
    /// 8 MB ≈ 1,4 s, 20 MB ≈ 1,6 s, 50 MB ≈ 4,5 s.</para>
    ///
    /// <para><b>Warum nicht <c>IModel.BeginInverseCaching()</c>.</b> Der Zwischenspeicher der Bibliothek
    /// (<c>MemoryInverseCache</c>, xBIM 6.1.605) ist NICHT gleichwertig: Er indiziert je Typ nur die
    /// Verweise, die das Schema DER DATEI als Ziel einer Rückbeziehung führt
    /// (<c>IContainsIndexedReferences</c>), und findet er für einen Typ keinen, liefert er eine LEERE
    /// Menge — ohne Meldung. In IFC2X3 gibt es <c>IfcMaterial.HasProperties</c> nicht; die
    /// IFC4-Schnittstelle bildet sie über <c>IfcMaterialProperties.Material</c> nach, und dieser Verweis
    /// ist in IFC2X3 nicht indiziert. Mit Zwischenspeicher hätte jeder IFC2X3-Baustoff also keine
    /// Stoffsätze, und die Meldung <c>IMP_IFC_PROT_STOFFWERTE_NICHT_GELESEN</c> entfiele still (Befund
    /// im Test <c>IfcRueckbezuegeTests</c>). Dazu wertet er den Namen der Beziehung nicht aus, und
    /// nach einer Frage nach einem Untertyp beantwortet er die nach dem Obertyp nur aus dem schon
    /// Indizierten.</para>
    ///
    /// <para><b>Gleiche Antwort wie die Bibliothek.</b> Jeder Index durchläuft
    /// <c>Instances.OfType&lt;IIfc…&gt;()</c> EINMAL — dieselbe Schnittstelle, derselbe Vorwärtsverweis und
    /// damit dieselbe Bedingung wie die Rückbeziehung selbst (die Implementierungen in IFC2X3 und IFC4X3
    /// fragen genau so; die in IFC4 über die Klasse, deren Vorkommnisse dieselben sind). Die Reihenfolge
    /// je Schlüssel ist die des Durchlaufs, also die, in der die Bibliothek die Beziehungen liefern würde;
    /// eine Beziehung steht je Ziel höchstens einmal (wie <c>Contains</c>). Schlüssel ist die
    /// <c>EntityLabel</c> des Ziels. Gebaut wird jeder Index erst bei der ersten Frage; wirft der
    /// Vorwärtsverweis einer Beziehung, wirft jede Frage an diesen Index dieselbe Ausnahme — so wie jede
    /// Suche der Bibliothek über diese Beziehung sie geworfen hätte. Kein eigener Faden, keine globale
    /// Einstellung der Bibliothek.</para>
    /// </summary>
    internal sealed class IfcRueckbezuege
    {
        private readonly IModel _modell;

        private Index<IIfcRelDefinesByProperties> _definiertDurch;
        private Index<IIfcRelDefinesByType> _typisiertDurch;
        private Index<IIfcRelAssociates> _zuordnungen;
        private Index<IIfcRelAggregates> _zerlegtDurch;
        private Index<IIfcRelContainedInSpatialStructure> _enthaelt;
        private Index<IIfcRelVoidsElement> _oeffnungen;
        private Index<IIfcRelFillsElement> _fuellungen;
        private Index<IIfcMaterialProperties> _stoffsaetze;

        /// <summary>Legt den (noch leeren) Index eines geladenen Modells an.</summary>
        public IfcRueckbezuege(IModel modell)
        {
            _modell = modell ?? throw new ArgumentNullException(nameof(modell));
        }

        /// <summary><c>IsDefinedBy</c> eines Objekts oder Kontexts: die <c>IfcRelDefinesByProperties</c>, deren <c>RelatedObjects</c> es enthalten.</summary>
        public IReadOnlyList<IIfcRelDefinesByProperties> DefiniertDurch(IPersistEntity objekt)
            => Holen(ref _definiertDurch, objekt, r => r.RelatedObjects);

        /// <summary><c>IsTypedBy</c> eines Objekts: die <c>IfcRelDefinesByType</c>, deren <c>RelatedObjects</c> es enthalten.</summary>
        public IReadOnlyList<IIfcRelDefinesByType> TypisiertDurch(IPersistEntity objekt)
            => Holen(ref _typisiertDurch, objekt, r => r.RelatedObjects);

        /// <summary><c>HasAssociations</c> einer Objektdefinition (auch eines Typs): die <c>IfcRelAssociates</c>, deren <c>RelatedObjects</c> sie enthalten.</summary>
        public IReadOnlyList<IIfcRelAssociates> Zuordnungen(IPersistEntity objekt)
            => Holen(ref _zuordnungen, objekt, r => r.RelatedObjects?.OfType<IPersistEntity>());

        /// <summary><c>IsDecomposedBy</c>: die <c>IfcRelAggregates</c> mit <c>RelatingObject</c> = diesem Objekt.</summary>
        public IReadOnlyList<IIfcRelAggregates> ZerlegtDurch(IPersistEntity objekt)
            => Holen(ref _zerlegtDurch, objekt, r => r.RelatingObject);

        /// <summary><c>ContainsElements</c>: die <c>IfcRelContainedInSpatialStructure</c> mit <c>RelatingStructure</c> = diesem räumlichen Element.</summary>
        public IReadOnlyList<IIfcRelContainedInSpatialStructure> Enthaelt(IPersistEntity ort)
            => Holen(ref _enthaelt, ort, r => r.RelatingStructure);

        /// <summary><c>HasOpenings</c>: die <c>IfcRelVoidsElement</c> mit <c>RelatingBuildingElement</c> = diesem Bauteil.</summary>
        public IReadOnlyList<IIfcRelVoidsElement> Oeffnungen(IPersistEntity bauteil)
            => Holen(ref _oeffnungen, bauteil, r => r.RelatingBuildingElement);

        /// <summary><c>HasFillings</c>: die <c>IfcRelFillsElement</c> mit <c>RelatingOpeningElement</c> = dieser Öffnung.</summary>
        public IReadOnlyList<IIfcRelFillsElement> Fuellungen(IPersistEntity oeffnung)
            => Holen(ref _fuellungen, oeffnung, r => r.RelatingOpeningElement);

        /// <summary><c>HasProperties</c> eines Baustoffs: die <c>IfcMaterialProperties</c> mit <c>Material</c> = diesem Baustoff — auch in IFC2X3.</summary>
        public IReadOnlyList<IIfcMaterialProperties> Stoffsaetze(IPersistEntity stoff)
            => Holen(ref _stoffsaetze, stoff, r => r.Material);

        // ==================================================================
        //  Aufbau
        // ==================================================================

        /// <summary>Ein Index: Ziel (EntityLabel) → Beziehungen in Durchlaufreihenfolge, oder die Ausnahme des Aufbaus.</summary>
        private sealed class Index<T>
        {
            public Dictionary<int, List<T>> Eintraege;
            public ExceptionDispatchInfo Fehler;
        }

        private IReadOnlyList<T> Holen<T>(ref Index<T> index, IPersistEntity ziel, Func<T, IEnumerable<IPersistEntity>> verweise)
            where T : IPersistEntity
        {
            if (index == null) index = Aufbauen(verweise);
            return Nachschlagen(index, ziel);
        }

        private IReadOnlyList<T> Holen<T>(ref Index<T> index, IPersistEntity ziel, Func<T, IPersistEntity> verweis)
            where T : IPersistEntity
        {
            if (index == null) index = Aufbauen<T>(r => Einzeln(verweis(r)));
            return Nachschlagen(index, ziel);
        }

        private static IEnumerable<IPersistEntity> Einzeln(IPersistEntity e)
        {
            if (e != null) yield return e;
        }

        private static IReadOnlyList<T> Nachschlagen<T>(Index<T> index, IPersistEntity ziel)
        {
            index.Fehler?.Throw();
            if (ziel == null || !index.Eintraege.TryGetValue(ziel.EntityLabel, out List<T> liste)) return Array.Empty<T>();
            return liste;
        }

        /// <summary>Ein Durchlauf über alle Beziehungen des Typs; jede steht je Ziel höchstens einmal.</summary>
        private Index<T> Aufbauen<T>(Func<T, IEnumerable<IPersistEntity>> verweise) where T : IPersistEntity
        {
            var index = new Index<T>();
            try
            {
                var eintraege = new Dictionary<int, List<T>>();
                foreach (T rel in _modell.Instances.OfType<T>())
                {
                    IEnumerable<IPersistEntity> ziele = verweise(rel);
                    if (ziele == null) continue;
                    foreach (IPersistEntity ziel in ziele)
                    {
                        if (ziel == null) continue;
                        if (!eintraege.TryGetValue(ziel.EntityLabel, out List<T> liste))
                            eintraege[ziel.EntityLabel] = liste = new List<T>();
                        if (liste.Count == 0 || !ReferenceEquals(liste[liste.Count - 1], rel)) liste.Add(rel);
                    }
                }
                index.Eintraege = eintraege;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                index.Fehler = ExceptionDispatchInfo.Capture(ex);
            }
            return index;
        }
    }
}
