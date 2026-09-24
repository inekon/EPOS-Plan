namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Abbild einer gbXML-Datei</b> — das normierte <see cref="GebaeudeAbbild"/> samt dem, was
    /// nur gbXML trägt: Versionswert und die Einheitenangaben der Wurzel, wie gelesen. Die
    /// Nordrichtung (<c>Location/CADModelAzimuth</c>) steht in
    /// <see cref="GebaeudeAbbild.NordwinkelGrad"/> und wird nie still angewandt (3.2).
    /// </summary>
    public sealed class GbxmlAbbild : GebaeudeAbbild
    {
        /// <summary>Legt ein leeres Abbild im Format gbXML an.</summary>
        public GbxmlAbbild()
        {
            Format = GebaeudeQuelle.FORMAT_GBXML;
        }

        /// <summary>Der Versionswert der Wurzel (<c>gbXML/@version</c>), wie gelesen; <c>null</c> = keiner.</summary>
        public string Version { get; set; }

        /// <summary>Das globale Längenattribut (<c>lengthUnit</c>), wie gelesen.</summary>
        public string Laengeneinheit { get; set; }

        /// <summary>Das globale Flächenattribut (<c>areaUnit</c>), wie gelesen.</summary>
        public string Flaecheneinheit { get; set; }

        /// <summary>Das globale Volumenattribut (<c>volumeUnit</c>), wie gelesen.</summary>
        public string Volumeneinheit { get; set; }

        /// <summary>Das globale Temperaturattribut (<c>temperatureUnit</c>), wie gelesen.</summary>
        public string Temperatureinheit { get; set; }
    }
}
