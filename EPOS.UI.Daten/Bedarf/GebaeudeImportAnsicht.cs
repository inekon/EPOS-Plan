using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Grundriss eines Gebäudeimports als Daten der Ansicht</b> (Stufe G6c, Welle D; Entscheid E11,
    /// Mehrzonenkonzept 6.7) — plattformfrei neben der <see cref="GebaeudeImportHuelle"/>, ohne Datenbank.
    /// Übersetzt die <see cref="Zonengeometrie"/> des Kerns in das DTO der Komponente
    /// <c>GebaeudeAnsicht</c>: je Geschoss die Räume mit ihren Polygonen in Metern, der Zone als Schlüssel
    /// und der Herkunft als Wert; je Zone Schlüssel, Name (derselbe wie in der Zonenliste), Stelle und
    /// Beheizung; die Hinweise der Geometrie als Anzeigetexte. Gerechnet wird hier nichts — die Geometrie
    /// ist die des Kerns, nur ihre Gestalt wechselt.
    /// </summary>
    internal static class GebaeudeImportAnsicht
    {
        /// <summary>
        /// Die Daten der Ansicht; <c>null</c> ohne Geometrie.
        /// </summary>
        /// <param name="geometrie">Die Zonengeometrie der Anfrage.</param>
        /// <param name="umhaengbar">Lässt die Regel Zuordnungen von Hand zu (mehrere Zonen)?</param>
        /// <param name="zonenname">Der Name einer Zone nach ihrer Stelle, wie ihn die Zonenliste zeigt; <c>null</c> = der der Geometrie.</param>
        internal static GebaeudeAnsichtDaten AnsichtDaten(Zonengeometrie geometrie, bool umhaengbar, Func<int, string> zonenname = null)
        {
            if (geometrie == null) return null;
            var geschosse = new List<GebaeudeAnsichtGeschoss>(geometrie.Geschosse.Count);
            foreach (Geschossangabe g in geometrie.Geschosse)
            {
                List<GebaeudeAnsichtRaum> raeume = geometrie.RaeumeIm(g.Stelle).Select(r => Raum(geometrie, r)).ToList();
                geschosse.Add(new GebaeudeAnsichtGeschoss(
                    g.Kennung ?? "", string.IsNullOrWhiteSpace(g.Name) ? g.Kennung ?? "" : g.Name.Trim(), g.Schematisch,
                    g.MinX, g.MinY, g.MaxX, g.MaxY, raeume));
            }
            List<GebaeudeAnsichtZone> zonen = geometrie.Zonen
                .Select(z => new GebaeudeAnsichtZone(z.Schluessel, zonenname?.Invoke(z.Stelle) ?? z.Name, z.Stelle, z.IstBeheizt,
                                                     z.Herkunft == Geometrieherkunft.Schematisch, z.Handgeaendert))
                .ToList();
            return new GebaeudeAnsichtDaten
            {
                Geschosse = geschosse,
                Zonen = zonen,
                Schematisch = geometrie.Schematisch,
                Umhaengbar = umhaengbar,
                Hinweise = geometrie.Meldungen.Select(GebaeudeZuordnungsModell.MeldungText).ToList(),
            };
        }

        private static GebaeudeAnsichtRaum Raum(Zonengeometrie geometrie, Raumumriss r)
        {
            string zone = r.Zone >= 0 && r.Zone < geometrie.Zonen.Count ? geometrie.Zonen[r.Zone].Schluessel : null;
            double? flaeche = r.FlaecheM2 ?? (r.Polygone.Count > 0 ? r.PolygonflaecheM2 : (double?)null);
            List<IReadOnlyList<GebaeudeAnsichtPunkt>> polygone = r.Polygone
                .Select(p => (IReadOnlyList<GebaeudeAnsichtPunkt>)p.Punkte.Select(q => new GebaeudeAnsichtPunkt(q[0], q[1])).ToList())
                .ToList();
            return new GebaeudeAnsichtRaum(
                r.RaumKennung, r.Name, zone, r.Beheizt, r.Herkunft == Geometrieherkunft.Schematisch,
                flaeche.HasValue ? GebaeudeZuordnungsModell.ZahlText(Math.Round(flaeche.Value, 2)) + " m²" : MyResource.Resource.GIMP_WERT_LEER,
                polygone);
        }
    }
}
