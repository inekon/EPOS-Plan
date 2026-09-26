using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using T = WindowsFormsApplication1.WordVorlagentexte;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE BLÖCKE DER WORD-ENGINE (Konzept Berichtsvorlagen 4.2, 4.7, 4.8, 6.4, 6.6;
    // Etappe BV-E4): Wiederholblöcke {{#je stand}}, {{#je variante}}, {{#je gebaeude}}
    // … {{/je}} und Bedingungen {{#wenn schalter}}, {{#wenn nicht schalter}} … {{/wenn}}.
    // ---------------------------------------------------------------------------

    public sealed partial class WordVorlagenfueller
    {
        /// <summary>Höchste Verschachtelung von Blöcken (Konzept 4.2).</summary>
        public const int BLOCK_EBENEN = 2;

        /// <summary>
        /// Der Kontext einer Stelle im Rumpf: der Wertesatz mit laufendem Stand und Gebäude, bei einer
        /// Gruppe aus <c>{{#je variante|block n}}</c> ihre Varianten, und die Blockebene.
        /// </summary>
        private sealed class Blockkontext
        {
            internal Blockkontext(Berichtswerte werte, IReadOnlyList<VariantenDaten> gruppe, int ebene)
            {
                Werte = werte;
                Gruppe = gruppe;
                Ebene = ebene;
            }

            /// <summary>Der Wertesatz der Stelle (<see cref="Berichtswerte.MitStand"/>, <see cref="Berichtswerte.MitGebaeude"/>).</summary>
            internal Berichtswerte Werte { get; }

            /// <summary>Die Varianten einer Gruppe aus <c>|block n</c>; <c>null</c> = alle gewählten.</summary>
            internal IReadOnlyList<VariantenDaten> Gruppe { get; }

            /// <summary>Wie viele Blöcke die Stelle umschließen.</summary>
            internal int Ebene { get; }
        }

        private sealed partial class Lauf
        {
            /// <summary>Die Block-Steuerelemente (Tag ist eine Blockmarke), getrennt von den Wertesteuerelementen.</summary>
            private readonly List<Sdtstelle> _blockSdts = new List<Sdtstelle>();

            /// <summary>Je geklonter Wiederholung (oberstes Element) ihr Kontext; die nächste Zuordnung nach oben gilt.</summary>
            private readonly Dictionary<OpenXmlElement, Blockkontext> _kontexte = new Dictionary<OpenXmlElement, Blockkontext>();

            /// <summary>Welche Bedingungen schon als „ohne Wert“ gemeldet sind (einmal je Schlüssel und Stand).</summary>
            private readonly HashSet<string> _schalterGemeldet = new HashSet<string>(StringComparer.Ordinal);

            private string _kopfstil;
            /// <summary>Die Gliederungsebene je Überschriftenstil (<see cref="Berichtskapitel.UeberschriftEbenen"/>).</summary>
            private Dictionary<string, int> _ebenen;

            /// <summary>Der Wertesatz einer Stelle: der Kontext der nächsten Wiederholung darüber, sonst der Bericht.</summary>
            private Berichtswerte WerteVon(OpenXmlElement e)
            {
                for (OpenXmlElement x = e; x != null; x = x.Parent)
                    if (_kontexte.TryGetValue(x, out Blockkontext k)) return k.Werte;
                return _werte;
            }

            /// <summary>Ein Schlüssel des Paarvergleichs (<c>stand.a.*</c>, <c>stand.b.*</c>) gilt auch außerhalb der Blöcke (Konzept 4.7).</summary>
            internal static bool IstPaarschluessel(string schluessel)
            {
                return schluessel != null &&
                       (schluessel.StartsWith("stand.a.", StringComparison.Ordinal) ||
                        schluessel.StartsWith("stand.b.", StringComparison.Ordinal));
            }

            // ------------------------------------------------------------- Einstieg

            /// <summary>
            /// Wertet die Blöcke des Rumpfs aus — vor dem Ersetzen, damit jede Wiederholung ihre eigenen
            /// Platzhalter trägt. Blöcke in Kopf- und Fußzeilen, Fuß- und Endnoten und Textfeldern sind
            /// unzulässig (Konzept 4.3): Ihre Marken bleiben gelb stehen. Danach werden die
            /// Inhaltssteuerelemente neu gesammelt (die Wiederholungen tragen geklonte).
            /// </summary>
            private void ExpandiereBloecke()
            {
                _kopfstil = _stile.Finde(WordVorlagenstile.KAPITELKOPF);
                _ebenen = Berichtskapitel.UeberschriftEbenen(_stile);
                Teilinfo rumpf = _teile[0];
                BearbeiteFolge(rumpf.Wurzel.ChildElements.ToList(), new Blockkontext(_werte, null, 0), rumpf);

                _sdts.Clear();
                _sdtMenge.Clear();
                _blockSdts.Clear();
                SammleSdts();
            }

            /// <summary>Block-Steuerelemente, die nach dem Auswerten noch stehen (Kopfzeile, Satz, zu tief …): gemeldet.</summary>
            private void MeldeUebrigeBlocksdts()
            {
                foreach (Sdtstelle s in _blockSdts)
                    if (Haengt(s.Sdt, s.Teil.Wurzel)) Stehen(s.Marke, Fuellbefundart.Block, s.Sdt, s.Teil, null);
            }

            // ------------------------------------------------------------- Folgen von Geschwistern

            /// <summary>
            /// Wertet die Blöcke in einer Folge von Geschwistern aus (Rumpf, Zelle, Steuerelement, eine
            /// Wiederholung): Absatzblöcke zwischen zwei Markenabsätzen derselben Ebene, Block-Steuerelemente,
            /// Tabellen mit Musterzeilen, dazu die Zellen und Steuerelemente darin.
            /// </summary>
            private void BearbeiteFolge(List<OpenXmlElement> folge, Blockkontext k, Teilinfo ti)
            {
                int i = 0;
                while (i < folge.Count)
                {
                    OpenXmlElement e = folge[i];
                    if (e.Parent == null) { i++; continue; }

                    Platzhalter m = Markenabsatz(e);
                    if (m != null && IstAnfang(m))
                    {
                        int j = FindeEnde(folge, i, m);
                        if (j < 0)
                        {
                            MeldeBlock(T.BLOCK_OFFEN, m, e, ti);
                            i++;
                            continue;
                        }
                        List<OpenXmlElement> innen = folge.GetRange(i + 1, j - i - 1);
                        WerteAbsatzblockAus(m, e, folge[j], innen, k, ti);
                        i = j + 1;
                        continue;
                    }
                    if (m != null)
                    {
                        // Ein Ende ohne Anfang: Die Marke bleibt stehen, das Ersetzen meldet sie.
                        i++;
                        continue;
                    }

                    switch (e)
                    {
                        case SdtBlock sdt when BlockmarkeVon(sdt) is Platzhalter sm:
                            WerteSteuerelementAus(sdt, sm, sdt.SdtContentBlock, k, ti);
                            break;
                        case SdtBlock sdt when !_sdtMenge.Contains(sdt) && sdt.SdtContentBlock != null:
                            BearbeiteFolge(sdt.SdtContentBlock.ChildElements.ToList(), k, ti);
                            break;
                        case Table tabelle:
                            BearbeiteTabelle(tabelle, k, ti);
                            break;
                    }
                    i++;
                }
            }

            /// <summary>Der Blockanfang eines Wiederhol- oder Bedingungsblocks?</summary>
            private static bool IstAnfang(Platzhalter m)
            {
                return m.Art == Platzhalterart.BlockAnfang || m.Art == Platzhalterart.WennAnfang;
            }

            /// <summary>Das Ende, das zu einem Anfang dieser Art passt.</summary>
            private static Platzhalterart EndeZu(Platzhalter anfang)
            {
                return anfang.Art == Platzhalterart.BlockAnfang ? Platzhalterart.BlockEnde : Platzhalterart.WennEnde;
            }

            /// <summary>
            /// Die Blockmarke eines Absatzes, der allein aus ihr besteht (Konzept 4.2: „eigene Absätze“);
            /// sonst <c>null</c>.
            /// </summary>
            private static Platzhalter Markenabsatz(OpenXmlElement e)
            {
                if (!(e is Paragraph p)) return null;
                string text = string.Concat(EigeneTexte(p).Select(t => t.Text));
                if (text.IndexOf("{{", StringComparison.Ordinal) < 0) return null;
                List<Platzhalter> marken = Platzhaltersyntax.Finde(text).ToList();
                if (marken.Count != 1 || !marken[0].IstBlockmarke) return null;
                return IstAllein(p, marken[0]) ? marken[0] : null;
            }

            /// <summary>
            /// Das Ende zum Anfang an <paramref name="i"/> in derselben Folge: gezählt über alle Blockmarken;
            /// ein Ende der falschen Art auf der Ebene des Anfangs (verschränkt) oder keines: <c>-1</c>.
            /// </summary>
            private static int FindeEnde(List<OpenXmlElement> folge, int i, Platzhalter anfang)
            {
                int tiefe = 0;
                for (int x = i + 1; x < folge.Count; x++)
                {
                    Platzhalter m = Markenabsatz(folge[x]);
                    if (m == null) continue;
                    if (IstAnfang(m)) { tiefe++; continue; }
                    if (m.Art != Platzhalterart.BlockEnde && m.Art != Platzhalterart.WennEnde) continue;
                    if (tiefe > 0) { tiefe--; continue; }
                    return m.Art == EndeZu(anfang) ? x : -1;
                }
                return -1;
            }

            // ------------------------------------------------------------- Absatzblöcke

            /// <summary>
            /// Ein Block zwischen zwei Markenabsätzen: je Element eine Wiederholung vor dem Anfang, bzw. die
            /// Bedingung behält ihren Inhalt oder entfällt; die Markenabsätze entfallen immer.
            /// </summary>
            private void WerteAbsatzblockAus(Platzhalter m, OpenXmlElement anfang, OpenXmlElement ende,
                                             List<OpenXmlElement> innen, Blockkontext k, Teilinfo ti)
            {
                if (k.Ebene >= BLOCK_EBENEN)
                {
                    MeldeBlock(T.BLOCK_TIEFE, m, anfang, ti);
                    return;
                }

                if (m.Art == Platzhalterart.WennAnfang)
                {
                    bool? erfuellt = Bedingung(m, k, anfang, ti);
                    if (!erfuellt.HasValue)
                    {
                        // Kein gültiger Schalter: Der Bereich bleibt samt Marken stehen (nichts still entfernt).
                        BearbeiteFolge(innen, k, ti);
                        return;
                    }
                    if (erfuellt.Value)
                    {
                        BearbeiteFolge(innen, new Blockkontext(k.Werte, k.Gruppe, k.Ebene + 1), ti);
                        EntferneMarke(anfang);
                        EntferneMarke(ende);
                    }
                    else
                    {
                        EntferneBereich(anfang, innen, ende);
                    }
                    return;
                }

                List<Blockkontext> elemente = Elemente(m, k, anfang, ti);
                if (elemente == null)
                {
                    BearbeiteFolge(innen, k, ti);
                    return;
                }
                if (elemente.Count == 0)
                {
                    EntferneBereich(anfang, innen, ende);
                    return;
                }
                foreach (Blockkontext el in elemente)
                {
                    List<OpenXmlElement> klone = innen.Select(Klone).ToList();
                    foreach (OpenXmlElement klon in klone)
                    {
                        anfang.InsertBeforeSelf(klon);
                        _kontexte[klon] = el;
                    }
                    BearbeiteFolge(klone, el, ti);
                }
                foreach (OpenXmlElement e in innen) EntferneElement(e);
                EntferneMarke(anfang);
                EntferneMarke(ende);
            }

            // ------------------------------------------------------------- Steuerelemente (Konzept 6.6)

            /// <summary>Die Blockmarke im Tag eines Steuerelements (nur Anfänge); sonst <c>null</c>.</summary>
            private Platzhalter BlockmarkeVon(SdtElement sdt)
            {
                Sdtstelle s = _blockSdts.FirstOrDefault(b => ReferenceEquals(b.Sdt, sdt));
                Platzhalter m = s?.Marke ?? (_sdtMenge.Contains(sdt) ? null : MarkeAusTag(sdt));
                return m != null && IstAnfang(m) ? m : null;
            }

            /// <summary>
            /// Ein Wiederhol- oder Bedingungsabschnitt als Steuerelement (Block- oder Zeilenebene): Er
            /// begrenzt seinen Block selbst. Je Element eine Wiederholung seines Inhalts vor ihm, bzw. der
            /// Inhalt bleibt oder entfällt; danach wird das Steuerelement ausgepackt (Konzept 6.6) — es
            /// bleibt weder <c>w:sdt</c> noch <c>w:id</c> zurück.
            /// </summary>
            private void WerteSteuerelementAus(SdtElement sdt, Platzhalter m, OpenXmlElement inhalt, Blockkontext k, Teilinfo ti)
            {
                if (inhalt == null) return;
                if (k.Ebene >= BLOCK_EBENEN)
                {
                    MeldeBlock(T.BLOCK_TIEFE, m, sdt, ti);
                    return;
                }
                List<OpenXmlElement> teile = inhalt.ChildElements.ToList();
                var innerer = new Blockkontext(k.Werte, k.Gruppe, k.Ebene + 1);

                if (m.Art == Platzhalterart.WennAnfang)
                {
                    bool? erfuellt = Bedingung(m, k, sdt, ti);
                    if (!erfuellt.HasValue) return;
                    if (erfuellt.Value)
                    {
                        foreach (OpenXmlElement e in teile)
                        {
                            e.Remove();
                            sdt.InsertBeforeSelf(e);
                        }
                        BearbeiteTeile(teile, innerer, ti);
                        EntferneElement(sdt);
                    }
                    else
                    {
                        EntferneMitUeberschrift(sdt, sdt, sdt);
                    }
                    return;
                }

                List<Blockkontext> elemente = Elemente(m, k, sdt, ti);
                if (elemente == null) return;
                if (elemente.Count == 0)
                {
                    EntferneMitUeberschrift(sdt, sdt, sdt);
                    return;
                }
                foreach (Blockkontext el in elemente)
                {
                    List<OpenXmlElement> klone = teile.Select(Klone).ToList();
                    foreach (OpenXmlElement klon in klone)
                    {
                        sdt.InsertBeforeSelf(klon);
                        _kontexte[klon] = el;
                    }
                    BearbeiteTeile(klone, el, ti);
                }
                EntferneElement(sdt);
            }

            /// <summary>Wiederholte Teile weiter auswerten: Zeilen (Zeilen-Steuerelement) oder Blockinhalt.</summary>
            private void BearbeiteTeile(List<OpenXmlElement> teile, Blockkontext k, Teilinfo ti)
            {
                if (teile.Any(e => e is TableRow)) BearbeiteZeilen(teile, k, ti);
                else BearbeiteFolge(teile, k, ti);
            }

            // ------------------------------------------------------------- Tabellen (Konzept 6.4)

            /// <summary>
            /// Eine Tabelle: Musterzeilen und Zeilen-Steuerelemente werden wiederholt, die Zellen der übrigen
            /// Zeilen weiter ausgewertet. Bleibt keine Zeile, entfällt die Tabelle.
            /// </summary>
            private void BearbeiteTabelle(Table tabelle, Blockkontext k, Teilinfo ti)
            {
                BearbeiteZeilen(tabelle.ChildElements.ToList(), k, ti);
                if (tabelle.Parent != null && !tabelle.Elements<TableRow>().Any() && !tabelle.Elements<SdtRow>().Any())
                    EntferneElement(tabelle);
            }

            private void BearbeiteZeilen(List<OpenXmlElement> zeilen, Blockkontext k, Teilinfo ti)
            {
                foreach (OpenXmlElement e in zeilen)
                {
                    if (e.Parent == null) continue;
                    if (e is SdtRow sdtZeile)
                    {
                        Platzhalter sm = BlockmarkeVon(sdtZeile);
                        if (sm != null) WerteSteuerelementAus(sdtZeile, sm, sdtZeile.SdtContentRow, k, ti);
                        else if (sdtZeile.SdtContentRow != null) BearbeiteZeilen(sdtZeile.SdtContentRow.ChildElements.ToList(), k, ti);
                        continue;
                    }
                    if (!(e is TableRow zeile)) continue;

                    List<Zeilenmarke> marken = Zeilenmarken(zeile);
                    if (IstMusterzeile(zeile, marken))
                        WerteMusterzeileAus(zeile, marken[0], marken[marken.Count - 1], k, ti);
                    else
                        BearbeiteZellen(zeile, k, ti);
                }
            }

            private void BearbeiteZellen(TableRow zeile, Blockkontext k, Teilinfo ti)
            {
                foreach (TableCell zelle in zeile.Elements<TableCell>().ToList())
                    BearbeiteFolge(zelle.ChildElements.Where(c => !(c is TableCellProperties)).ToList(), k, ti);
            }

            /// <summary>Eine Blockmarke in einer Zeile: ihr <c>w:t</c>, die Zelle und die Marke.</summary>
            private sealed class Zeilenmarke
            {
                internal Text Text;
                internal Paragraph Absatz;
                internal int Zelle;
                internal Platzhalter Marke;
            }

            /// <summary>Die Blockmarken einer Zeile in Dokumentfolge (ohne verschachtelte Tabellen).</summary>
            private static List<Zeilenmarke> Zeilenmarken(TableRow zeile)
            {
                var marken = new List<Zeilenmarke>();
                List<TableCell> zellen = zeile.Elements<TableCell>().ToList();
                for (int z = 0; z < zellen.Count; z++)
                    foreach (Paragraph p in zellen[z].Descendants<Paragraph>())
                    {
                        if (!ReferenceEquals(p.Ancestors<TableRow>().FirstOrDefault(), zeile)) continue;
                        foreach (Text t in EigeneTexte(p))
                            foreach (Platzhalter m in Platzhaltersyntax.Finde(t.Text))
                                if (m.IstBlockmarke) marken.Add(new Zeilenmarke { Text = t, Absatz = p, Zelle = z, Marke = m });
                    }
                return marken;
            }

            /// <summary>
            /// Eine Musterzeile (Konzept 6.4 Nr. 1): Die erste Blockmarke ist ein Anfang in der ersten Zelle,
            /// die letzte das passende Ende in der letzten Zelle, und die beiden bilden ein Paar.
            /// </summary>
            private static bool IstMusterzeile(TableRow zeile, List<Zeilenmarke> marken)
            {
                if (marken.Count < 2) return false;
                Zeilenmarke a = marken[0], b = marken[marken.Count - 1];
                int zellen = zeile.Elements<TableCell>().Count();
                if (!IstAnfang(a.Marke) || a.Zelle != 0 || b.Zelle != zellen - 1 || b.Marke.Art != EndeZu(a.Marke)) return false;
                int tiefe = 0;
                for (int i = 0; i < marken.Count; i++)
                {
                    Platzhalter m = marken[i].Marke;
                    if (IstAnfang(m)) tiefe++;
                    else tiefe--;
                    if (tiefe == 0 && i < marken.Count - 1) return false;
                }
                return tiefe == 0;
            }

            /// <summary>
            /// Eine Musterzeile: die zwei Marken aus der Zeile nehmen, dann je Element eine geklonte Zeile
            /// davor (bzw. die Bedingung behält oder entfernt die Zeile); die Musterzeile entfällt.
            /// </summary>
            private void WerteMusterzeileAus(TableRow zeile, Zeilenmarke anfang, Zeilenmarke ende, Blockkontext k, Teilinfo ti)
            {
                Platzhalter m = anfang.Marke;
                if (k.Ebene >= BLOCK_EBENEN)
                {
                    MeldeBlock(T.BLOCK_TIEFE, m, anfang.Absatz, ti);
                    return;
                }

                List<Blockkontext> elemente = null;
                bool? erfuellt = null;
                if (m.Art == Platzhalterart.WennAnfang)
                {
                    erfuellt = Bedingung(m, k, anfang.Absatz, ti);
                    if (!erfuellt.HasValue) { BearbeiteZellen(zeile, k, ti); return; }
                }
                else
                {
                    elemente = Elemente(m, k, anfang.Absatz, ti);
                    if (elemente == null) { BearbeiteZellen(zeile, k, ti); return; }
                }

                SchneideMarke(ende);
                SchneideMarke(anfang);

                if (m.Art == Platzhalterart.WennAnfang)
                {
                    if (erfuellt.Value) BearbeiteZellen(zeile, new Blockkontext(k.Werte, k.Gruppe, k.Ebene + 1), ti);
                    else zeile.Remove();
                    return;
                }
                foreach (Blockkontext el in elemente)
                {
                    var klon = (TableRow)Klone(zeile);
                    zeile.InsertBeforeSelf(klon);
                    _kontexte[klon] = el;
                    BearbeiteZellen(klon, el, ti);
                }
                zeile.Remove();
            }

            /// <summary>
            /// Nimmt eine Marke aus ihrem <c>w:t</c>; ein danach leerer Lauf entfällt, ein danach leerer
            /// Absatz auch, wenn die Zelle noch einen anderen trägt.
            /// </summary>
            private static void SchneideMarke(Zeilenmarke z)
            {
                string text = z.Text.Text;
                int i = text.IndexOf(z.Marke.Roh, StringComparison.Ordinal);
                if (i < 0) return;
                z.Text.Text = text.Remove(i, z.Marke.Roh.Length);
                z.Text.Space = SpaceProcessingModeValues.Preserve;
                if (z.Text.Text.Length > 0) return;

                Run lauf = z.Text.Parent as Run;
                z.Text.Remove();
                if (lauf != null && lauf.ChildElements.All(c => c is RunProperties)) lauf.Remove();

                Paragraph p = z.Absatz;
                bool leer = !p.Descendants<Run>().Any(r => r.ChildElements.Any(c => !(c is RunProperties)));
                if (leer && p.Parent is TableCell zelle && zelle.Elements<Paragraph>().Count() > 1 &&
                    p.ParagraphProperties?.SectionProperties == null)
                    p.Remove();
            }

            // ------------------------------------------------------------- Elemente und Bedingungen

            /// <summary>
            /// Die Elemente eines Wiederholblocks im Kontext <paramref name="k"/>: <c>stand</c> — Stamm und
            /// Varianten (in einer Gruppe: Stamm und die Varianten der Gruppe); <c>variante</c> — die Varianten,
            /// mit <c>|block n</c> Gruppen zu höchstens n; <c>gebaeude</c> — die Gebäude des laufenden Stands,
            /// außerhalb eines Standblocks die des Stamms. <c>null</c> bei unbekanntem Bereich (gemeldet).
            /// </summary>
            private List<Blockkontext> Elemente(Platzhalter m, Blockkontext k, OpenXmlElement bezug, Teilinfo ti)
            {
                int ebene = k.Ebene + 1;
                Berichtswerte w = k.Werte;
                switch (m.Schluessel)
                {
                    case "stand":
                        {
                            IEnumerable<VariantenDaten> staende = k.Gruppe == null
                                ? w.Staende
                                : (w.Stamm != null ? new[] { w.Stamm } : Array.Empty<VariantenDaten>()).Concat(k.Gruppe);
                            return staende.Select(s => new Blockkontext(w.MitStand(s), null, ebene)).ToList();
                        }
                    case "variante":
                        {
                            IReadOnlyList<VariantenDaten> varianten = k.Gruppe ?? w.Varianten;
                            Formatangabe block = m.Angaben.FirstOrDefault(a => a.Art == Formatangabeart.Block && a.Zahl.HasValue);
                            if (block == null)
                                return varianten.Select(v => new Blockkontext(w.MitStand(v), null, ebene)).ToList();
                            int n = block.Zahl.Value;
                            var gruppen = new List<Blockkontext>();
                            for (int i = 0; i < varianten.Count; i += n)
                                gruppen.Add(new Blockkontext(w, varianten.Skip(i).Take(n).ToList(), ebene));
                            return gruppen;
                        }
                    case "gebaeude":
                        {
                            VariantenDaten stand = w.LaufenderStand ?? w.Stamm;
                            DataTable tabelle = stand?.Details?.Gebaeude;
                            if (tabelle == null) return new List<Blockkontext>();
                            return tabelle.Rows.Cast<DataRow>()
                                .Select(r => new Blockkontext(w.MitGebaeude(r), k.Gruppe, ebene)).ToList();
                        }
                    default:
                        MeldeBlock(T.BLOCK_BEREICH, m, bezug, ti);
                        return null;
                }
            }

            /// <summary>
            /// Wertet eine Bedingung aus (Konzept 4.2, 4.7): nur ein Katalogschalter, im Kontext der Stelle.
            /// Kein Schalter oder ein Stand- bzw. Gebäudeschalter außerhalb seines Blocks: <c>null</c> — der
            /// Bereich bleibt samt Marken stehen. Ein Schalter ohne Wert oder mit Fehler gilt als falsch,
            /// mit Laufmeldung.
            /// </summary>
            private bool? Bedingung(Platzhalter m, Blockkontext k, OpenXmlElement bezug, Teilinfo ti)
            {
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(m.Schluessel);
                if (feld == null || feld.Art != Vorlagenfeldart.Schalter)
                {
                    MeldeBlock(T.WENN_KEIN_SCHALTER, m, bezug, ti);
                    return null;
                }
                Berichtswerte w = k.Werte;
                if ((feld.Kontext == Vorlagenfeldkontext.Stand && !w.ImStandblock) ||
                    (feld.Kontext == Vorlagenfeldkontext.Gebaeude && !w.ImGebaeudeblock))
                {
                    MeldeBlock(T.WENN_KONTEXT, m, bezug, ti);
                    return null;
                }

                Platzhalterwert wert = Loese(feld, m, w);
                _ergebnis.Ersetzt++;
                if (!wert.Schalter.HasValue)
                {
                    string stand = w.LaufenderStand?.Anzeige ?? "";
                    if (_schalterGemeldet.Add(feld.Schluessel + "|" + stand))
                        _ergebnis.Warnung(T.F(_englisch, T.SCHALTER_LEER, m.Normalform, Fundort(bezug, ti)));
                    return false;
                }
                return wert.Schalter.Value != m.Verneint;
            }

            // ------------------------------------------------------------- Klonen und Entfernen

            /// <summary>
            /// Ein tiefer Klon für eine Wiederholung: Fundorte (Absatz- und Tabellennummern) wie im Muster;
            /// Textmarken und Steuerelement-Kennungen fallen weg — sie wären nach dem Klonen doppelt.
            /// </summary>
            private OpenXmlElement Klone(OpenXmlElement muster)
            {
                OpenXmlElement klon = muster.CloneNode(true);
                List<Paragraph> alt = Mit<Paragraph>(muster), neu = Mit<Paragraph>(klon);
                for (int i = 0; i < alt.Count && i < neu.Count; i++)
                    if (_absatzNummer.TryGetValue(alt[i], out int n)) _absatzNummer[neu[i]] = n;
                List<Table> altT = Mit<Table>(muster), neuT = Mit<Table>(klon);
                for (int i = 0; i < altT.Count && i < neuT.Count; i++)
                    if (_tabellenNummer.TryGetValue(altT[i], out int n)) _tabellenNummer[neuT[i]] = n;

                foreach (OpenXmlElement e in klon.Descendants().Where(d => d is BookmarkStart || d is BookmarkEnd).ToList()) e.Remove();
                foreach (SdtId id in klon.Descendants<SdtId>().ToList()) id.Remove();
                return klon;
            }

            private static List<TE> Mit<TE>(OpenXmlElement e) where TE : OpenXmlElement
            {
                var liste = new List<TE>();
                if (e is TE selbst) liste.Add(selbst);
                liste.AddRange(e.Descendants<TE>());
                return liste;
            }

            /// <summary>Entfernt einen Markenabsatz; trägt er eine Abschnittsangabe oder verlangt der Behälter einen Absatz, bleibt ein leerer.</summary>
            private static void EntferneMarke(OpenXmlElement absatz)
            {
                if (absatz.Parent != null) ErsetzeAbsatz(absatz, new List<OpenXmlElement>());
            }

            /// <summary>Entfernt ein Element wie einen Markenabsatz (Abschnittsangaben bleiben, Behälter nie leer).</summary>
            private static void EntferneElement(OpenXmlElement e)
            {
                if (e.Parent != null) ErsetzeAbsatz(e, new List<OpenXmlElement>());
            }

            /// <summary>Ein entfallender Block (Bedingung falsch, keine Elemente): Marken, Inhalt und eine verwaiste Überschrift davor.</summary>
            private void EntferneBereich(OpenXmlElement anfang, List<OpenXmlElement> innen, OpenXmlElement ende)
            {
                EntferneMitUeberschrift(anfang, ende, null);
                foreach (OpenXmlElement e in innen) EntferneElement(e);
                EntferneMarke(anfang);
                EntferneMarke(ende);
            }

            /// <summary>
            /// Eine Überschrift (Kapitelkopf, Überschrift 1 bis 9) unmittelbar vor einem entfallenden Block
            /// entfällt mit, wenn ihr danach nichts mehr folgt als eine Überschrift derselben oder einer höheren
            /// Gliederungsebene, ein Abschnittsende oder das Ende ihres Behälters — sie bliebe sonst verwaist
            /// (Konzept 5.3). Folgt eine TIEFERE Überschrift (auf den Kapitelkopf eine Überschrift 2), geht ihr
            /// Abschnitt weiter, und sie bleibt. <paramref name="auch"/> wird danach selbst entfernt (ein Steuerelement).
            /// </summary>
            private void EntferneMitUeberschrift(OpenXmlElement anfang, OpenXmlElement ende, OpenXmlElement auch)
            {
                OpenXmlElement vorher = anfang.PreviousSibling();
                OpenXmlElement danach = ende.NextSibling();
                int? ebene = vorher is Paragraph kopf && kopf.ParagraphProperties?.SectionProperties == null ? Ebene(kopf) : null;
                bool verwaist = danach == null || danach is SectionProperties ||
                                (danach is Paragraph p && (p.ParagraphProperties?.SectionProperties != null ||
                                                          (Ebene(p) is int folgt && folgt <= ebene)));
                if (ebene.HasValue && verwaist) vorher.Remove();
                if (auch != null) EntferneElement(auch);
            }

            /// <summary>Die Gliederungsebene eines Absatzes, wenn er eine Überschrift ist (Kapitelkopf 0); sonst <c>null</c>.</summary>
            private int? Ebene(Paragraph p)
            {
                string stil = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                return _ebenen != null && _ebenen.TryGetValue(stil, out int n) ? n : (int?)null;
            }

            /// <summary>Ein Fehler eines Blocks in der Laufmeldung (die Marke bleibt stehen und wird beim Ersetzen genannt).</summary>
            private void MeldeBlock(string muster, Platzhalter m, OpenXmlElement bezug, Teilinfo ti)
            {
                _ergebnis.Fehlermeldung(T.F(_englisch, muster, m.Normalform, Fundort(bezug, ti)));
            }
        }
    }
}
