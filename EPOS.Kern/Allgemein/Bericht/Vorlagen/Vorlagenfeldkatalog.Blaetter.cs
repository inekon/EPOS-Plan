using System.Collections.Generic;
using System.Linq;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    public static partial class Vorlagenfeldkatalog
    {
        /// <summary>Die Fassung der Blattmarken (Etappe BV-E7, Katalog v5).</summary>
        private const int FASSUNG_BLAETTER = 5;

        /// <summary>Die Fassung der Excel-Diagramme (Etappe BV-E8, Katalog v6): <c>blatt.diagrammdaten</c>, die Bilder mit Ausgabe Excel.</summary>
        private const int FASSUNG_EXCEL_DIAGRAMME = 6;

        /// <summary>
        /// Katalog v5 (Konzept Berichtsvorlagen 4.6 BV-P9, 7.2, Anhang A; Etappe BV-E7): die Blattmarken der Excel-Vorlage —
        /// je erzeugtes Blatt ein Eintrag der Art Blatt, nur Excel, handgepflegt (eigene Beschreibung). Die Marke steht allein
        /// in A1 eines leeren Blattes; <c>blatt.detail</c> auf einem Blatt mit weiteren Zellen ist das Musterblatt, das je
        /// Stand geklont wird. Der Wert ist der heutige Name des Blattes (beim Detailblatt die Namen je Stand) — gefüllt
        /// wird die Marke nicht als Wert, sondern durch das erzeugte Blatt (<see cref="ExcelVorlagenfueller"/>).
        /// </summary>
        private static IEnumerable<Vorlagenfeld> Blaetter()
        {
            IReadOnlyDictionary<ExcelBerichtGenerator.Blattart, string> Feste() { return ExcelBerichtGenerator.FesteBlattnamen(); }
            yield return Blatt("blatt.uebersicht", w => Feste()[ExcelBerichtGenerator.Blattart.Uebersicht]);
            yield return Blatt("blatt.vergleich", w => Feste()[ExcelBerichtGenerator.Blattart.Vergleich]);
            yield return Blatt("blatt.wirtschaftlichkeit", w => Feste()[ExcelBerichtGenerator.Blattart.Wirtschaftlichkeit]);
            yield return Blatt("blatt.verlauf", w => ExcelBerichtGenerator.Verlaufsblattname());
            yield return Blatt("blatt.detail", w => w.Staende.Count == 0
                ? (object)new Leergrund(w.Text(nameof(R.BV_GRUND_KEIN_STAND)))
                : string.Join(", ", w.Staende.Select(s => s.IstStamm ? "Stamm" : s.Anzeige)));
            yield return Blatt("blatt.checkliste", w => Feste()[ExcelBerichtGenerator.Blattart.Checkliste]);
            // Katalog v6 (BV-E8): das Blatt mit den Zahlen der Excel-Diagramme.
            yield return Blatt("blatt.diagrammdaten", w => Diagrammplan.BLATTNAME, FASSUNG_EXCEL_DIAGRAMME);
        }

        private static Vorlagenfeld Blatt(string schluessel, System.Func<Berichtswerte, object> quelle, int seit = FASSUNG_BLAETTER)
        {
            return new Vorlagenfeld(schluessel, Vorlagenfeldart.Blatt, Vorlagenfeldkontext.Bericht, quelle)
            {
                Seit = seit,
                Ausgaben = Vorlagenausgabe.Excel,
                Leerwert = "",
            };
        }
    }
}
