using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Was eine Excel-Vorlage an Bausteinen führt</b> (Entscheid BV-Q2 (c), Konzept Berichtsvorlagen 7.2,
    /// 10.2 „Häkchen (BV-Q1 c)“) — die Entsprechung des Kapitelstands der Word-Vorlage, aus der Vorlage
    /// gemessen und vom <see cref="ExcelVorlagenpruefer"/> in <see cref="Pruefbefund.Bausteine"/> gelegt.
    ///
    /// <para><b>Die Regel</b>, Stück für Stück wie in Word:</para>
    /// <list type="bullet">
    /// <item>Die <b>Blattmarke</b> <c>{{blatt.…}}</c> ist der Kapitelplatzhalter der Mappe: Sie führt die
    /// Häkchen ihres Blattes (<see cref="ExcelBerichtGenerator.Blattbausteine"/>).</item>
    /// <item>Das <b>Anhängen der erzeugten Blätter</b> ist der Sammelanker <c>{{bericht.inhalt}}</c>: Eine
    /// Vorlage ohne <c>EPOS.Blattanhang</c> = <c>nein</c> bekommt jedes erzeugte Blatt hinten angehängt und
    /// führt damit jeden Baustein, den die Mappe überhaupt kennt.</item>
    /// <item>Eine Vorlage, die das Anhängen abschaltet, <b>bildet ihre Blätter aus Einzelelementen nach</b>
    /// (Nachtrag BV-E9). Ein Baustein gilt dann auch als geführt, wenn die Vorlage einen Schlüssel trägt,
    /// den sein Kapitel deckt (<see cref="Vorlagenfeld.Deckt"/>) — sonst stünde die ausgelieferte
    /// ausführliche Vorlage mit „in dieser Vorlage nicht enthalten“ neben Blättern, die sie sehr wohl führt.
    /// Excel kennt keinen Kapitelplatzhalter; ein nachgebildetes Blatt ist allein an seinen Einzelelementen
    /// zu erkennen, und die weiche Sperre irrt hier lieber zugunsten der Wahl.</item>
    /// <item>Die Bausteine, die nur der Wortbericht kennt (<see cref="BerichtsKonfiguration.BausteinDef.NurWord"/>:
    /// Deckblatt, Inhaltsverzeichnis, Anhang), führt keine Mappe.</item>
    /// </list>
    /// </summary>
    internal static class ExcelBlattstand
    {
        /// <summary>Die Bausteine, die eine Mappe überhaupt tragen kann (alles außer <c>NurWord</c>).</summary>
        internal static IEnumerable<string> Moegliche
        {
            get { return BerichtsKonfiguration.AlleBausteine.Where(b => !b.NurWord).Select(b => b.Schluessel); }
        }

        /// <summary>
        /// Führt die Vorlage überhaupt Blätter — trägt sie eine Blattmarke oder hängt sie die erzeugten
        /// Blätter an? Ist die Antwort nein, bestimmt die Vorlage ihren Inhalt allein (die Häkchenliste
        /// weicht der leisen Zeile, wie bei einer Word-Vorlage ohne Kapitel).
        /// </summary>
        internal static bool FuehrtBlaetter(IEnumerable<ExcelBerichtGenerator.Blattart> marken, bool haengtAn)
        {
            return haengtAn || (marken != null && marken.Any());
        }

        /// <summary>
        /// Die Bausteine, die die Vorlage führt, in der Folge des Katalogs
        /// (<see cref="BerichtsKonfiguration.AlleBausteine"/>).
        /// </summary>
        /// <param name="marken">Die Arten der Blattmarken der Vorlage.</param>
        /// <param name="haengtAn">Hängt die Vorlage die erzeugten Blätter ohne Marke an?</param>
        /// <param name="schluessel">Alle Schlüssel, die die Vorlage nutzt (für die Deckung aus Einzelelementen).</param>
        internal static IReadOnlyList<string> Gefuehrt(IEnumerable<ExcelBerichtGenerator.Blattart> marken, bool haengtAn,
                                                       IEnumerable<string> schluessel)
        {
            if (haengtAn) return Moegliche.ToList();

            var gefuehrt = new HashSet<string>(StringComparer.Ordinal);
            foreach (ExcelBerichtGenerator.Blattart art in marken ?? Enumerable.Empty<ExcelBerichtGenerator.Blattart>())
                if (ExcelBerichtGenerator.Blattbausteine.TryGetValue(art, out IReadOnlyList<string> b))
                    foreach (string s in b) gefuehrt.Add(s);

            var genutzt = new HashSet<string>(schluessel ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (genutzt.Count > 0)
                foreach (string baustein in Moegliche)
                    if (!gefuehrt.Contains(baustein) && DeckungAusEinzelelementen(baustein, genutzt))
                        gefuehrt.Add(baustein);

            return Moegliche.Where(gefuehrt.Contains).ToList();
        }

        /// <summary>
        /// Trägt die Vorlage einen Schlüssel, den das Kapitel dieses Bausteins deckt (Kapitelkopf und
        /// Schalter zählen mit — beide gehören nur zu ihrem Kapitel)?
        /// </summary>
        private static bool DeckungAusEinzelelementen(string baustein, ICollection<string> genutzt)
        {
            foreach (Berichtskapitel k in Berichtskapitel.Alle)
            {
                if (!string.Equals(k.Baustein, baustein, StringComparison.Ordinal)) continue;
                Vorlagenfeld kapitel = Vorlagenfeldkatalog.Finde(k.Schluessel);
                if (kapitel == null) continue;
                foreach (string gedeckt in kapitel.Deckt)
                    if (genutzt.Contains(gedeckt)) return true;
            }
            return false;
        }
    }
}
