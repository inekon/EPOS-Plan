using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using R = WindowsFormsApplication1.MyResource.Resource;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // DIE BLOCKREGELN DES VORLAGENPRÜFERS (Konzept Berichtsvorlagen 4.2, 4.3, 4.7, 4.8,
    // 6.4, 6.6; Etappe BV-E4) — dieselben Regeln, nach denen die Word-Engine Blöcke
    // auswertet: Blockbereiche aus getippten Paaren und Block-Steuerelementen, die Ebenen,
    // die Kontexte der Werte je Stand und je Gebäude und der Paarvergleich.
    // ---------------------------------------------------------------------------

    public static partial class Vorlagenpruefer
    {
        /// <summary>Die Wiederholbereiche, in denen Werte je Stand gelten (Konzept 4.7).</summary>
        private static readonly string[] Standbereiche = { "stand", "variante" };

        /// <summary>Ein Schlüssel des Paarvergleichs (<c>stand.a.*</c>, <c>stand.b.*</c>, Konzept 4.7)?</summary>
        internal static bool IstPaarschluessel(string schluessel)
        {
            return schluessel != null &&
                   (schluessel.StartsWith("stand.a.", StringComparison.Ordinal) ||
                    schluessel.StartsWith("stand.b.", StringComparison.Ordinal));
        }

        /// <summary>
        /// Der Text des Hinweises „Vorlage nutzt Stand 5, gewählt sind 3 Stände“ (BV-E9), wenn <paramref name="feld"/> eine
        /// Position nennt, die der Lauf im <paramref name="kontext"/> nicht hat — gezählt werden das Stammprojekt und die
        /// gewählten Varianten; sonst <c>null</c>. Word- und Excel-Prüfer teilen die Regel.
        /// </summary>
        internal static string Positionshinweis(Vorlagenfeld feld, Pruefkontext kontext, bool englisch, out bool variante)
        {
            variante = false;
            if (feld == null || kontext == null ||
                !Vorlagenfeldkatalog.IstPositionsschluessel(feld.Schluessel, out variante, out int position, out _)) return null;
            int varianten = Math.Max(0, kontext.AnzahlVarianten);
            int staende = varianten + 1;
            if (position <= (variante ? varianten : staende)) return null;
            System.Globalization.CultureInfo kultur = BerichtTexte.KulturFuer(englisch);
            string muster = R.ResourceManager.GetString(nameof(R.VF_PRUEF_POSITION), kultur) ?? nameof(R.VF_PRUEF_POSITION);
            string marke = "{{" + feld.Schluessel + "}}";
            return string.Format(kultur, muster, marke, staende, varianten);
        }

        /// <summary>Ein Blockbereich: ein getipptes Paar oder ein Block-Steuerelement.</summary>
        private sealed class Blockbereich
        {
            /// <summary>Der Fund des Anfangs.</summary>
            internal Vorlagenfund Anfang;

            /// <summary>Der Absatz des Anfangs bzw. das Steuerelement.</summary>
            internal OpenXmlElement Von;

            /// <summary>Der Absatz des Endes; <c>null</c> beim Steuerelement.</summary>
            internal OpenXmlElement Bis;

            /// <summary>Die Musterzeile eines Paars in erster und letzter Zelle; sonst <c>null</c>.</summary>
            internal OpenXmlElement Zeile;

            /// <summary>Das Block-Steuerelement; sonst <c>null</c>.</summary>
            internal OpenXmlElement Steuerelement;

            internal Platzhalter Marke { get { return Anfang.Platzhalter; } }

            /// <summary>Ein Wiederholblock <c>{{#je …}}</c> mit <c>|block n</c> — er wiederholt Gruppen, keine Stände.</summary>
            internal bool Gruppe { get { return Marke.Angaben.Any(a => a.Art == Formatangabeart.Block); } }
        }

        private sealed partial class Sitzung
        {
            private readonly List<Blockbereich> _bereiche = new List<Blockbereich>();
            private readonly Dictionary<OpenXmlElement, Dictionary<OpenXmlElement, int>> _folgen =
                new Dictionary<OpenXmlElement, Dictionary<OpenXmlElement, int>>();
            private bool _steuerelementeGesammelt;
            private bool _paarsichtGemeldet;

            /// <summary>Welche Art der Positionsadressierung schon gemeldet ist (Stand, Variante; einmal je Vorlage).</summary>
            private readonly HashSet<bool> _positionGemeldet = new HashSet<bool>();

            /// <summary>Alle Blockbereiche: die getippten Paare aus <see cref="PruefeBloecke"/>, dazu die Block-Steuerelemente.</summary>
            private List<Blockbereich> Bereiche
            {
                get
                {
                    if (_steuerelementeGesammelt) return _bereiche;
                    _steuerelementeGesammelt = true;
                    foreach (Vorlagenfund f in _funde)
                    {
                        if (f.Quelle != Fundquelle.Steuerelement || !f.ZaehltMit || f.Steuerelement?.Element == null) continue;
                        Platzhalterart art = f.Platzhalter.Art;
                        if (art != Platzhalterart.BlockAnfang && art != Platzhalterart.WennAnfang) continue;
                        if (f.Steuerelement.Ebene != Steuerelementebene.Block && f.Steuerelement.Ebene != Steuerelementebene.Zeile) continue;
                        _bereiche.Add(new Blockbereich { Anfang = f, Von = f.Steuerelement.Element, Steuerelement = f.Steuerelement.Element });
                    }
                    return _bereiche;
                }
            }

            /// <summary>Merkt ein getipptes Paar als Blockbereich (Musterzeile, wenn Anfang und Ende die Zeile umfassen).</summary>
            private void MerkeBereich(Vorlagenfund anfang, Vorlagenfund ende)
            {
                OpenXmlElement von = anfang.Absatz?.Element, bis = ende.Absatz?.Element;
                if (von == null || bis == null) return;
                var bereich = new Blockbereich { Anfang = anfang, Von = von, Bis = bis };
                Vorlagenort a = anfang.Ort, b = ende.Ort;
                bool eineZeile = a.InTabelle && b.InTabelle && a.Tabelle == b.Tabelle && a.Zeile == b.Zeile;
                if (eineZeile && ((a.Zelle == 1 && b.Zelle == b.ZellenInZeile && a.Zelle != b.Zelle) || b.ZellenInZeile == 1))
                    bereich.Zeile = von.Ancestors<W.TableRow>().FirstOrDefault();
                _bereiche.Add(bereich);
            }

            /// <summary>Das Element eines Funds: Absatz, Steuerelement oder Bild.</summary>
            private static OpenXmlElement ElementVon(Vorlagenfund f)
            {
                switch (f.Quelle)
                {
                    case Fundquelle.Text: return f.Absatz?.Element;
                    case Fundquelle.Steuerelement: return f.Steuerelement?.Element;
                    default: return f.Bild?.Element;
                }
            }

            /// <summary>Liegt der Fund im Bereich (nicht die Marke des Bereichs selbst)?</summary>
            private bool Enthaelt(Blockbereich b, Vorlagenfund f)
            {
                if (ReferenceEquals(b.Anfang, f)) return false;
                OpenXmlElement e = ElementVon(f);
                if (e == null) return false;
                if (b.Steuerelement != null)
                    return !ReferenceEquals(e, b.Steuerelement) && e.Ancestors().Any(x => ReferenceEquals(x, b.Steuerelement));
                if (b.Zeile != null)
                    return e.Ancestors().Any(x => ReferenceEquals(x, b.Zeile));
                OpenXmlElement wurzel = Wurzel(e);
                if (!ReferenceEquals(wurzel, Wurzel(b.Von))) return false;
                Dictionary<OpenXmlElement, int> folge = Folge(wurzel);
                return folge.TryGetValue(e, out int i) && folge.TryGetValue(b.Von, out int von) &&
                       folge.TryGetValue(b.Bis, out int bis) && i > von && i < bis;
            }

            private static OpenXmlElement Wurzel(OpenXmlElement e)
            {
                OpenXmlElement x = e;
                while (x.Parent != null) x = x.Parent;
                return x;
            }

            /// <summary>Die Dokumentfolge eines Teils (je Element sein Index), einmal je Wurzel.</summary>
            private Dictionary<OpenXmlElement, int> Folge(OpenXmlElement wurzel)
            {
                if (_folgen.TryGetValue(wurzel, out Dictionary<OpenXmlElement, int> folge)) return folge;
                folge = new Dictionary<OpenXmlElement, int>();
                int i = 0;
                foreach (OpenXmlElement e in wurzel.Descendants()) folge[e] = i++;
                _folgen[wurzel] = folge;
                return folge;
            }

            /// <summary>Die Bereiche, die den Fund umschließen.</summary>
            private IEnumerable<Blockbereich> Umschliessend(Vorlagenfund f)
            {
                return Bereiche.Where(b => Enthaelt(b, f));
            }

            /// <summary>
            /// Gilt ein Wert dieses Kontexts an der Stelle (Konzept 4.7)? Stand: in <c>{{#je stand}}</c> oder
            /// <c>{{#je variante}}</c> ohne <c>|block n</c>, der Paarvergleich überall; Gebäude: in
            /// <c>{{#je gebaeude}}</c>; alle übrigen Kontexte überall.
            /// </summary>
            private bool ImKontext(Vorlagenfund f, Vorlagenfeldkontext kontext, string schluessel)
            {
                switch (kontext)
                {
                    case Vorlagenfeldkontext.Stand:
                        if (IstPaarschluessel(schluessel)) return true;
                        return Umschliessend(f).Any(b => b.Marke.Art == Platzhalterart.BlockAnfang &&
                                                         Standbereiche.Contains(b.Marke.Schluessel) && !b.Gruppe);
                    case Vorlagenfeldkontext.Gebaeude:
                        return Umschliessend(f).Any(b => b.Marke.Art == Platzhalterart.BlockAnfang && b.Marke.Schluessel == "gebaeude");
                    default:
                        return true;
                }
            }

            /// <summary>
            /// Der Paarvergleich (Konzept 4.7): <c>stand.a.*</c>/<c>stand.b.*</c> gelten in der Paarsicht; in
            /// Sicht 1 nur, wenn genau eine Variante gewählt ist. Gemeldet wird einmal je Vorlage und nur,
            /// wenn der Kontext eine Auswahl kennt (mehr als eine Variante).
            /// </summary>
            private void PruefePaarsicht(Vorlagenfund f, string schluessel)
            {
                if (_paarsichtGemeldet || !IstPaarschluessel(schluessel)) return;
                if (Kontext.Sicht != 1 || Kontext.AnzahlVarianten <= 1) return;
                _paarsichtGemeldet = true;
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_PAARSICHT), T(nameof(R.VF_PRUEF_PAARSICHT)), Fundort(f),
                      T(nameof(R.VF_PRUEF_PAARSICHT_TUN)), f.Platzhalter.Normalform);
            }

            /// <summary>
            /// Die Positionsadressierung (BV-E9): Nutzt die Vorlage eine Position, die der Lauf nicht hat
            /// (<c>stand.5.*</c> bei drei Ständen), bleibt die Stelle leer mit Grund — ein Hinweis, einmal je Art.
            /// </summary>
            private void PruefePosition(Vorlagenfund f, Vorlagenfeld feld)
            {
                string text = Positionshinweis(feld, Kontext, Kontext.Englisch, out bool variante);
                if (text == null || !_positionGemeldet.Add(variante)) return;
                Melde(Befundstufe.Hinweis, nameof(R.VF_PRUEF_POSITION), text, Fundort(f),
                      T(nameof(R.VF_PRUEF_POSITION_TUN)), f.Platzhalter.Normalform);
            }

            /// <summary><c>|block n</c> gilt nur an <c>{{#je variante}}</c> um ganze Absätze und Tabellen (Konzept 4.8, 6.4 Nr. 3).</summary>
            private static bool BlockangabeErlaubt(Vorlagenfund f)
            {
                if (f.Platzhalter.Schluessel != "variante") return false;
                if (f.Quelle == Fundquelle.Text) return !f.Ort.InTabelle;
                return f.Quelle == Fundquelle.Steuerelement && f.Steuerelement?.Ebene == Steuerelementebene.Block;
            }

            /// <summary>
            /// Ein Block als Steuerelement (Konzept 6.6): nur ein Anfang (es begrenzt seinen Block selbst) und nur
            /// um ganze Absätze, Tabellen oder Tabellenzeilen — im Satz oder um eine Zelle wertet die Engine ihn nicht aus.
            /// </summary>
            private void PruefeBlocksteuerelement(Vorlagenfund f)
            {
                Platzhalterart art = f.Platzhalter.Art;
                Steuerelementebene? ebene = f.Steuerelement?.Ebene;
                bool ende = art == Platzhalterart.BlockEnde || art == Platzhalterart.WennEnde;
                bool ort = ebene == Steuerelementebene.Satz || ebene == Steuerelementebene.Zelle;
                if (!ende && !ort) return;
                string marke = f.Platzhalter.Normalform;
                Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT),
                      T(nameof(R.VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT), marke), Fundort(f),
                      T(nameof(R.VF_PRUEF_BLOCK_NICHT_UNTERSTUETZT_TUN)), marke);
            }

            /// <summary>Anfang oder Ende einer Musterzeile (dort stehen die Marken im Satz der ersten bzw. letzten Zelle)?</summary>
            private bool InMusterzeile(Vorlagenfund f)
            {
                return f.Ort.InTabelle && Bereiche.Any(b => b.Zeile != null &&
                    ElementVon(f)?.Ancestors().Any(x => ReferenceEquals(x, b.Zeile)) == true);
            }

            /// <summary>Höchstens zwei Ebenen (Konzept 4.2): gezählt über getippte Paare und Block-Steuerelemente.</summary>
            private void PruefeTiefe()
            {
                foreach (Vorlagenfund f in _funde)
                {
                    Platzhalterart art = f.Platzhalter.Art;
                    if ((art != Platzhalterart.BlockAnfang && art != Platzhalterart.WennAnfang) || !f.ZaehltMit) continue;
                    if (f.Quelle == Fundquelle.Bild) continue;
                    if (Umschliessend(f).Count() < WordVorlagenfueller.BLOCK_EBENEN) continue;
                    Melde(Befundstufe.Fehler, nameof(R.VF_PRUEF_BLOCK_TIEFE),
                          T(nameof(R.VF_PRUEF_BLOCK_TIEFE), f.Platzhalter.Normalform), Fundort(f),
                          T(nameof(R.VF_PRUEF_BLOCK_TIEFE_TUN)), f.Platzhalter.Normalform);
                }
            }
        }
    }
}
