namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Abbild einer IFC-Datei</b> — das normierte <see cref="GebaeudeAbbild"/> samt dem, was
    /// nur IFC trägt (Umsetzungskonzept 3.3): Schemastand, Nordrichtung und Koordinatenumrechnung,
    /// die Einheitenfaktoren, der Behälter und die Zähler, an denen die Importprobe den Leser misst
    /// (Raumgrenzen, U-Werte, Sollwertsätze, beide Verlustkanäle).
    ///
    /// <para><b>Die Azimute im Abbild sind schon gedreht</b> (3.4): um <c>TrueNorth</c>, oder — trägt
    /// der Modellkontext eine <c>IfcMapConversion</c> — um deren Drehung, dann OHNE <c>TrueNorth</c>.
    /// <see cref="GebaeudeAbbild.NordwinkelGrad"/> nennt die angewandte Drehung, die beiden Einzelwerte
    /// stehen hier.</para>
    /// </summary>
    internal sealed class IfcGebaeudeAbbild : GebaeudeAbbild
    {
        /// <summary>Legt ein leeres Abbild im Format IFC an.</summary>
        public IfcGebaeudeAbbild()
        {
            Format = GebaeudeQuelle.FORMAT_IFC;
        }

        /// <summary>Der angenommene Schemastand.</summary>
        public IfcSchemaStand SchemaStand { get; set; }

        /// <summary>Die Art des Inhalts: <c>STEP</c>, <c>XML</c>; <c>null</c> vor dem Erkennen.</summary>
        public string Inhaltsart { get; set; }

        /// <summary>Name des gelesenen Eintrags im <c>.ifczip</c>-Behälter; <c>null</c> = kein Behälter.</summary>
        public string Behaeltereintrag { get; set; }

        /// <summary>Entpackte Größe des Behältereintrags in Byte, wie das Zip-Verzeichnis sie ausweist; 0 = kein Behälter.</summary>
        public long EntpackteGroesse { get; set; }

        /// <summary>
        /// Die Drehung aus <c>TrueNorth</c> des Modellkontexts [°] — der Azimut der Nordrichtung im
        /// Modellsystem, im Uhrzeigersinn von +y; <c>null</c> = keine Angabe (Vorgabe [0,1], also 0°).
        /// </summary>
        public double? TrueNorthGrad { get; set; }

        /// <summary>Trägt der Modellkontext eine <c>IfcMapConversion</c>? Dann gilt deren Drehung statt <see cref="TrueNorthGrad"/>.</summary>
        public bool MapConversionVorhanden { get; set; }

        /// <summary>Die Drehung der <c>IfcMapConversion</c> [°] = atan2(XAxisOrdinate, XAxisAbscissa); <c>null</c> = keine.</summary>
        public double? MapConversionGrad { get; set; }

        /// <summary>Faktor der Längeneinheit nach Meter (<c>MILLI METRE</c> → 0,001).</summary>
        public double LaengenFaktorNachMeter { get; set; } = 1.0;

        /// <summary>Faktor der Flächeneinheit nach m² — aus <c>AREAUNIT</c> mit eigenem Prefix, nie still aus der Länge quadriert.</summary>
        public double FlaechenFaktorNachM2 { get; set; } = 1.0;

        /// <summary>Faktor der Volumeneinheit nach m³ — aus <c>VOLUMEUNIT</c> mit eigenem Prefix.</summary>
        public double VolumenFaktorNachM3 { get; set; } = 1.0;

        /// <summary>Zahl aller Raumgrenzen (<c>IfcRelSpaceBoundary</c> samt Untertypen).</summary>
        public int ZahlRaumgrenzen { get; set; }

        /// <summary>Zahl der Raumgrenzen 2. Ebene — über den Typ ODER über <c>Name='2ndLevel'</c>/<c>Description='2a'|'2b'</c>.</summary>
        public int ZahlRaumgrenzenZweiteEbene { get; set; }

        /// <summary>Zahl der Raumgrenzen 2. Ebene, die NUR über Name oder Beschreibung erkannt sind (Basisklasse, Archicad-Muster).</summary>
        public int ZahlRaumgrenzenNachName { get; set; }

        /// <summary>Zahl der gelesenen Werte <c>ThermalTransmittance</c> (Vorkommnis und Typ).</summary>
        public int ZahlUWerte { get; set; }

        /// <summary>Zahl der Räume mit <c>Pset_SpaceThermalRequirements</c> (in IFC4X3 nicht gelesen).</summary>
        public int ZahlSollwertsaetze { get; set; }

        /// <summary>Verlustkanal 1: Entitäten, die der Parser nicht anlegen konnte (<c>FailedEntity</c> und Parserfehler).</summary>
        public int VerlusteNichtAngelegt { get; set; }

        /// <summary>Verlustkanal 2: Warnungen „Entity #… is referenced but could not be instantiated".</summary>
        public int VerlusteVerweise { get; set; }
    }
}
