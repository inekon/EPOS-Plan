using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using R = WindowsFormsApplication1.MyResource.Resource;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE TABELLENREGELN DES VORLAGENPRÜFERS (Konzept Berichtsvorlagen 4.3, 6.4 Nr. 2, 6.8;
    // Etappe BV-E5) — dieselben Regeln, nach denen die Word-Engine Strukturtabellen setzt:
    // Tabellenplatzhalter allein im Absatz des Rumpfs, einer Zelle oder eines Block-
    // Steuerelements (Vorlagenpruefer.PruefeOrt), {{muster.tabelle}} nur als Alternativtext
    // oder Titel einer Tabelle, und eine Mustertabelle nennt ihre Rollen.
    // ---------------------------------------------------------------------------

    public static partial class Vorlagenpruefer
    {
        private sealed partial class Sitzung
        {
            /// <summary>
            /// Die Mustertabellen der Vorlage (Konzept 6.4 Nr. 2): Eine Tabelle mit dem Alternativtext oder Titel
            /// <c>{{muster.tabelle}}</c> nennt je Rolle eine Zelle; nennt sie keine (Stamm, Gruppe, Summe, Warnung), warnt
            /// der Prüfer — die Engine entfernt sie und nimmt die Direktformatierung.
            /// </summary>
            public void PruefeMustertabellen(WordprocessingDocument doc)
            {
                var wurzeln = new List<OpenXmlElement>();
                if (doc.MainDocumentPart?.Document?.Body != null) wurzeln.Add(doc.MainDocumentPart.Document.Body);
                foreach (HeaderPart k in doc.MainDocumentPart?.HeaderParts ?? Enumerable.Empty<HeaderPart>())
                    if (k.Header != null) wurzeln.Add(k.Header);
                foreach (FooterPart f in doc.MainDocumentPart?.FooterParts ?? Enumerable.Empty<FooterPart>())
                    if (f.Footer != null) wurzeln.Add(f.Footer);

                foreach (OpenXmlElement wurzel in wurzeln)
                {
                    List<W.Table> tabellen = wurzel.Descendants<W.Table>().ToList();
                    for (int i = 0; i < tabellen.Count; i++)
                    {
                        if (!Tabellenmuster.IstMuster(tabellen[i])) continue;
                        if (Tabellenmuster.Lies(tabellen[i]).Rollen.Count > 0) continue;
                        Melde(Befundstufe.Warnung, nameof(R.VF_PRUEF_MUSTER_OHNE_ROLLEN),
                              T(nameof(R.VF_PRUEF_MUSTER_OHNE_ROLLEN), "{{" + Vorlagenfeldkatalog.MUSTER_TABELLE + "}}"),
                              T(nameof(R.VF_PRUEF_ORT_MUSTER), i + 1),
                              T(nameof(R.VF_PRUEF_MUSTER_OHNE_ROLLEN_TUN)));
                    }
                }
            }
        }
    }
}
