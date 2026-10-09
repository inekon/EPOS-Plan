using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Was einen Tag des Jahresrasters bestimmt — die Farbe in der Anzeige (Konzept 7.8).</summary>
    public enum Rastertagart
    {
        /// <summary>Die Standardwoche (bzw. Grundangabe), Werktag.</summary>
        Grundwoche,

        /// <summary>Die Standardwoche an Samstag oder Sonntag.</summary>
        Wochenende,

        /// <summary>Eine Zuordnungszeile über mehrere Tage.</summary>
        Zeitraum,

        /// <summary>Ein Einzeltag mit festem Datum.</summary>
        Einzeltag,

        /// <summary>Ferien — die Ferienperioden 1–4 des Generators oder eine Ferienzeile ab 5.</summary>
        Ferien,

        /// <summary>Eine Feiertagsregel.</summary>
        Feiertag,

        /// <summary>Außerhalb der Saison („aus", Rang 900).</summary>
        Saison,
    }

    /// <summary>Ein Tag des Jahresrasters: Datum, Wochentag (0 = Montag), Quelle und — bei einer eigenen Zeile — ihr Schlüssel.</summary>
    public sealed record Rastertag(int Tag, int Monat, int TagImMonat, int Wochentag, Rastertagart Art, string Quelle,
                                   int? Rang, Zuordnungsschluessel Schluessel);

    /// <summary>Ein Abschnitt des Jahresbands: aufeinanderfolgende Tage derselben Quelle.</summary>
    public sealed record Bandabschnitt(int Beginn, int Ende, Rastertagart Art, string Quelle, int? Rang,
                                       Zuordnungsschluessel Schluessel);

    public static partial class Kalenderbedienung
    {
        /// <summary>
        /// <b>Das Jahresraster einer Größe</b> (Konzept 7.8): je Tag 1 … 365 des Bezugsjahrs die wirksame Quelle nach
        /// der Rangregel (die ranghöchste Periode, die den Tag enthält) im Kalender, der am Ort angezeigt wird
        /// (<see cref="Konditionierungsarbeitsstand.Ansichtskalender"/>). Ohne Kalender: 365 Tage der Grundwoche.
        /// </summary>
        public static IReadOnlyList<Rastertag> Jahresraster(Konditionierungsarbeitsstand stand, Konditionierungsort ort)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            if (ort == null) throw new ArgumentNullException(nameof(ort));
            Konditionierungskalender k = stand.Ansichtskalender(ort.Groesse, ort.Zone);
            var tage = new Rastertag[Kalenderregel.TAG_MAX];
            for (int d = 0; d < tage.Length; d++)
            {
                Feiertage.Datum(d + 1, out int monat, out int tagImMonat);
                int w = (stand.W0 + d) % 7;
                Kalenderregel r = Quelle(k, d, stand.Referenzjahr);
                Rastertagart art = Art(r, w);
                Zuordnungsschluessel s = r != null && !Konditionierungsarbeit.IstMatrixbereich(r) ? Zuordnungsschluessel.Von(r) : null;
                tage[d] = new Rastertag(d + 1, monat, tagImMonat, w, art, r?.Bezeichner, r?.Rang, s);
            }
            return tage;
        }

        /// <summary><b>Das Jahresband</b>: das Raster in Abschnitte gleicher Quelle (Art und Rang) zusammengefasst.</summary>
        public static IReadOnlyList<Bandabschnitt> Jahresband(IReadOnlyList<Rastertag> raster)
        {
            var liste = new List<Bandabschnitt>();
            if (raster == null) return liste;
            int i = 0;
            while (i < raster.Count)
            {
                Rastertag t = raster[i];
                int j = i;
                while (j + 1 < raster.Count && Gleich(raster[j + 1], t)) j++;
                liste.Add(new Bandabschnitt(t.Tag, raster[j].Tag, t.Art, t.Quelle, t.Rang, t.Schluessel));
                i = j + 1;
            }
            return liste;
        }

        /// <summary>
        /// <b>Klick auf einen Tag</b> (1 … 365): der Schlüssel der eigenen Zeile bzw. des Einzeltags, der den Tag
        /// bestimmt; <c>null</c> für Standardwoche, Ferien 1–4, Saison oder einen Tag außerhalb.
        /// </summary>
        public static Zuordnungsschluessel TagAufloesen(Konditionierungsarbeitsstand stand, Konditionierungsort ort, int tag)
        {
            if (tag < Kalenderregel.TAG_MIN || tag > Kalenderregel.TAG_MAX) return null;
            return Jahresraster(stand, ort)[tag - 1].Schluessel;
        }

        /// <summary>
        /// Die ranghöchste Periode, die den Tag enthält — wie <see cref="Konditionierungskalender.Quellperiode"/>, aber
        /// auch dann, wenn „wie Wochentag X" ohne Standardwoche nicht greift: Der Tag rechnet dann mit der
        /// Grundangabe, die Anzeige nennt aber die Zeile, die der Anwender gesetzt hat.
        /// </summary>
        private static Kalenderregel Quelle(Konditionierungskalender k, int tag0, int referenzjahr)
        {
            if (k == null) return null;
            foreach (Kalenderregel r in k.Perioden)
            {
                int f0 = r.IstFeiertag ? Feiertage.Jahrestag(r.Feiertagsregel, referenzjahr) - 1 : -1;
                if (r.Enthaelt(tag0, f0)) return r;
            }
            return null;
        }

        private static Rastertagart Art(Kalenderregel r, int wochentag)
        {
            if (r == null) return Wochenendtage.Contains(wochentag) ? Rastertagart.Wochenende : Rastertagart.Grundwoche;
            if (string.Equals(r.Art, DbWerte.KOND_ART_BETRIEBSPAUSE, StringComparison.Ordinal)) return Rastertagart.Saison;
            if (string.Equals(r.Art, DbWerte.KOND_ART_FERIEN, StringComparison.Ordinal)) return Rastertagart.Ferien;
            if (r.IstFeiertag) return Rastertagart.Feiertag;
            if (IstFerienzeile(Zuordnungsschluessel.Von(r))) return Rastertagart.Ferien;
            return r.Beginn == r.Ende ? Rastertagart.Einzeltag : Rastertagart.Zeitraum;
        }

        private static bool Gleich(Rastertag a, Rastertag b) => a.Art == b.Art && a.Rang == b.Rang;
    }
}
