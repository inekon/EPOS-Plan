using System;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE BILDREGELN DES VORLAGENPRÜFERS (Konzept Berichtsvorlagen 4.2, 4.10, 6.5, 6.8;
    // Etappe BV-E5): Bildschlüssel als getippter Text nur allein im Absatz, vorgemerkte
    // App-Diagramme als „später“, und die Warnung „Bildrahmen unter 80 %“, wenn der Rahmen
    // schmaler ist als die Breite, in der das Bild gezeichnet wird (Stufe 2 greift nicht).
    // ---------------------------------------------------------------------------

    public static partial class Vorlagenpruefer
    {
        /// <summary>Unter diesem Anteil der Zeichenbreite warnt der Prüfer (Konzept 6.5, 6.8).</summary>
        public const double BILDRAHMEN_GRENZE = 0.8;

        private sealed partial class Sitzung
        {
            /// <summary>
            /// Ein Diagramm (nicht das Logo) als getippter Text oder Steuerelement (Konzept 4.2): allein im
            /// Absatz gilt es — das Bild in Satzspiegelbreite —, im Satz ist es ein Fehler mit Rat. <c>true</c>,
            /// wenn die Regel gemeldet hat (die übrigen Ortsregeln entfallen dann).
            /// </summary>
            private bool PruefeDiagrammAlsText(Vorlagenfund f, Vorlagenfeld feld)
            {
                if (f.Quelle != Fundquelle.Text || IstAllein(f)) return false;
                OrtFehler(f, feld.Art, nameof(R.VF_PRUEF_STELLE_SATZ),
                          T(nameof(R.VF_PRUEF_ORT_TUN_BILD_SATZ), "{{" + feld.Schluessel + "}}"));
                return true;
            }

            /// <summary>
            /// Ein vorgemerktes App-Diagramm (Anhang A: <see cref="Vorlagenfeldkatalog.VorgemerkteBilder"/>): kein
            /// Tippfehler, sondern „erst in einer späteren Programmfassung“ — ein Fehler, die Stelle bliebe stehen.
            /// </summary>
            private bool PruefeVorgemerkt(Vorlagenfund f)
            {
                if (!Vorlagenfeldkatalog.IstVorgemerktesBild(f.Platzhalter.Schluessel)) return false;
                Spaeter(f, Befundstufe.Fehler, T(nameof(R.VF_PRUEF_SPAETER_TUN_VORGEMERKT)));
                PruefeAngaben(f, null);
                return true;
            }

            /// <summary>
            /// <b>Bildrahmen unter 80 %</b> (volle Prüfung, Konzept 6.5, 6.8): Ist der Rahmen eines Diagramms
            /// schmaler, als der Renderer das Bild mindestens zeichnet, greift Stufe 1 — das Bild wird verkleinert,
            /// die Schrift schrumpft mit. Unter <see cref="BILDRAHMEN_GRENZE"/> der Zeichenbreite eine Warnung mit
            /// der Breite, ab der das Bild in Zielgröße entsteht.
            /// </summary>
            public void PruefeBildrahmen()
            {
                foreach (Vorlagenfund f in _funde)
                {
                    if (f.Quelle != Fundquelle.Bild || !f.ZaehltMit || f.Feld == null || f.Feld.Art != Vorlagenfeldart.Bild) continue;
                    long breite = Rahmenbreite(f.Bild?.Element);
                    double? anteil = Vorlagenfeldkatalog.Rahmenanteil(f.Feld.Schluessel, breite);
                    if (!anteil.HasValue || anteil.Value >= BILDRAHMEN_GRENZE) continue;
                    double mindestCm = breite / anteil.Value * BILDRAHMEN_GRENZE / 360000.0;
                    string marke = f.Platzhalter.Normalform;
                    Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_BILDRAHMEN),
                          T(nameof(R.VF_PRUEF_BILDRAHMEN), marke, (anteil.Value * 100.0).ToString("N0", _kultur)),
                          Fundort(f), T(nameof(R.VF_PRUEF_BILDRAHMEN_TUN), Math.Ceiling(mindestCm * 10.0) / 10.0),
                          marke);
                }
            }

            /// <summary>Die Breite des Rahmens (<c>wp:extent/@cx</c>) zu einem <c>wp:docPr</c> in EMU; 0 ohne.</summary>
            private static long Rahmenbreite(OpenXmlElement docPr)
            {
                OpenXmlElement ausdehnung = docPr?.Parent?.ChildElements.FirstOrDefault(e => e.LocalName == "extent");
                if (ausdehnung == null) return 0L;
                string cx = ausdehnung.GetAttributes().FirstOrDefault(a => a.LocalName == "cx").Value;
                return long.TryParse(cx, NumberStyles.Integer, CultureInfo.InvariantCulture, out long wert) ? wert : 0L;
            }
        }
    }
}
