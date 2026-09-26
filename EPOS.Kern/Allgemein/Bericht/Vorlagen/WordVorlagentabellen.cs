using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using T = WindowsFormsApplication1.WordVorlagentexte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Mustertabelle einer Vorlage</b> (Konzept Berichtsvorlagen 6.4 Nr. 2, Etappe BV-E5): eine Tabelle mit dem
    /// Alternativtext bzw. Tabellentitel <c>{{muster.tabelle}}</c>, je Rolle eine Zelle — ihr Text nennt die Rolle
    /// („Stamm“, „Gruppe“, „Summe“, „Warnung“; englisch „Base“, „Group“, „Total“, „Warning“), ihre Schattierung und
    /// das Zeichenformat ihres ersten Laufs sind das Format der Rolle. Die Engine liest sie und entfernt sie.
    /// </summary>
    public sealed class Tabellenmuster
    {
        /// <summary>Die Rollen in der Reihenfolge, in der eine Zelle mit mehreren Rollen ihr Format nimmt.</summary>
        public static readonly IReadOnlyList<Tabellenrolle> Vorrang = new[]
        {
            Tabellenrolle.Warnung, Tabellenrolle.Summe, Tabellenrolle.Gruppe, Tabellenrolle.Stamm,
        };

        private readonly Dictionary<Tabellenrolle, (Shading Schattierung, RunProperties Zeichen)> _formate =
            new Dictionary<Tabellenrolle, (Shading, RunProperties)>();

        /// <summary>Die Rollen, die die Mustertabelle trägt.</summary>
        public IReadOnlyCollection<Tabellenrolle> Rollen { get { return _formate.Keys; } }

        /// <summary>Hat das Muster ein Format für die Rolle?</summary>
        public bool Hat(Tabellenrolle rolle) { return _formate.ContainsKey(rolle); }

        /// <summary>Die Rolle, deren Format eine Zelle mit den Rollen <paramref name="rollen"/> nimmt; <c>Keine</c> = keins.</summary>
        public Tabellenrolle Waehle(Tabellenrolle rollen)
        {
            foreach (Tabellenrolle r in Vorrang)
                if ((rollen & r) != 0 && _formate.ContainsKey(r)) return r;
            return Tabellenrolle.Keine;
        }

        /// <summary>Eine Kopie der Schattierung der Rolle; <c>null</c> = keine.</summary>
        public Shading Schattierung(Tabellenrolle rolle)
        {
            return _formate.TryGetValue(rolle, out var f) && f.Schattierung != null ? (Shading)f.Schattierung.CloneNode(true) : null;
        }

        /// <summary>Eine Kopie des Zeichenformats der Rolle; <c>null</c> = keins.</summary>
        public RunProperties Zeichen(Tabellenrolle rolle)
        {
            return _formate.TryGetValue(rolle, out var f) && f.Zeichen != null ? (RunProperties)f.Zeichen.CloneNode(true) : null;
        }

        /// <summary>Ist die Tabelle eine Mustertabelle (Alternativtext oder Titel <c>{{muster.tabelle}}</c>)?</summary>
        public static bool IstMuster(Table t)
        {
            TableProperties tp = t?.GetFirstChild<TableProperties>();
            if (tp == null) return false;
            return IstMusterschluessel(tp.GetFirstChild<TableCaption>()?.Val?.Value) ||
                   IstMusterschluessel(tp.GetFirstChild<TableDescription>()?.Val?.Value);
        }

        /// <summary>Nennt der Text den Schlüssel <c>muster.tabelle</c> — mit oder ohne Klammern, gleich welcher Schreibung?</summary>
        public static bool IstMusterschluessel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim();
            Platzhalter p = t.StartsWith("{{", StringComparison.Ordinal) ? Platzhaltersyntax.Finde(t).FirstOrDefault() : Platzhaltersyntax.Lies(t);
            return p != null && p.Art == Platzhalterart.Feld &&
                   string.Equals(Vorlagenfeldkatalog.Finde(p.Schluessel)?.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, StringComparison.Ordinal);
        }

        /// <summary>Die Rolle, die ein Zelltext nennt; <c>Keine</c> = keine.</summary>
        public static Tabellenrolle RolleAus(string text)
        {
            string t = Platzhaltersyntax.Normiere(text ?? "").Replace(" ", "");
            switch (t)
            {
                case "stamm": case "stammprojekt": case "base": case "referenz": case "reference":
                    return Tabellenrolle.Stamm;
                case "gruppe": case "group":
                    return Tabellenrolle.Gruppe;
                case "summe": case "total": case "sum":
                    return Tabellenrolle.Summe;
                case "warnung": case "warning":
                    return Tabellenrolle.Warnung;
                default:
                    return Tabellenrolle.Keine;
            }
        }

        /// <summary>Liest die Rollen einer Mustertabelle (erste Zelle je Rolle gilt).</summary>
        public static Tabellenmuster Lies(Table t)
        {
            var m = new Tabellenmuster();
            foreach (TableCell zelle in t.Descendants<TableCell>())
            {
                Tabellenrolle rolle = RolleAus(zelle.InnerText);
                if (rolle == Tabellenrolle.Keine || m._formate.ContainsKey(rolle)) continue;
                Shading shd = zelle.TableCellProperties?.GetFirstChild<Shading>();
                RunProperties rp = zelle.Descendants<Run>().Select(r => r.RunProperties).FirstOrDefault(p => p != null);
                m._formate[rolle] = (shd == null ? null : (Shading)shd.CloneNode(true),
                                     rp == null ? null : (RunProperties)rp.CloneNode(true));
            }
            return m;
        }
    }

    public sealed partial class WordVorlagenfueller
    {
        private sealed partial class Lauf
        {
            /// <summary>Die Mustertabelle der Vorlage; <c>null</c> = keine.</summary>
            private Tabellenmuster _muster;

            /// <summary>
            /// Liest die erste Mustertabelle der Vorlage und entfernt ALLE (Konzept 6.4 Nr. 2) — vor den Blöcken, damit
            /// keine Wiederholung sie klont. Trägt sie keine erkennbare Rolle, gilt die Direktformatierung, mit Hinweis.
            /// </summary>
            private void LiesMustertabellen()
            {
                foreach (Teilinfo ti in _teile)
                    foreach (Table t in ti.Wurzel.Descendants<Table>().Where(Tabellenmuster.IstMuster).ToList())
                    {
                        if (t.Parent == null) continue;
                        if (_muster == null)
                        {
                            Tabellenmuster m = Tabellenmuster.Lies(t);
                            if (m.Rollen.Count > 0) _muster = m;
                            else _ergebnis.Hinweis(T.T(T.MUSTER_OHNE_ROLLEN, _englisch));
                        }
                        OpenXmlElement eltern = t.Parent;
                        ParagraphProperties format = t.PreviousSibling() is Paragraph davor ? davor.ParagraphProperties : null;
                        t.Remove();
                        SichereAbsatz(eltern, format);
                    }
            }

            /// <summary>
            /// Was mit einem Tabellenplatzhalter geschieht (Konzept 4.3, 4.10): allein im Absatz des Rumpfs, einer Zelle
            /// oder eines Block-Steuerelements die Tabelle, ohne Zeilen der Leertext mit Grund; anderswo bleibt er gelb
            /// stehen. <c>{{muster.tabelle}}</c> gilt nur als Alternativtext einer Tabelle.
            /// </summary>
            private Entscheid EntscheideTabelle(Platzhalter m, Vorlagenfeld feld, Ortsangabe ort, bool allein,
                                                OpenXmlElement bezug, Teilinfo ti, SdtForm form, Berichtswerte werte)
            {
                if (string.Equals(feld.Schluessel, Vorlagenfeldkatalog.MUSTER_TABELLE, StringComparison.Ordinal))
                    return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.MUSTER_ORT);
                if (form == SdtForm.ImSatz) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.SDT_IM_SATZ);
                if (!allein || !ort.ErlaubtListe) return Stehen(m, Fuellbefundart.FalscheStelle, bezug, ti, T.TABELLE_ORT);

                Platzhalterwert w = Loese(feld, m, werte);
                _ergebnis.Ersetzt++;
                // Ohne Zeilen der Leertext mit Grund (Konzept 4.10): „— (keine Zeilen für diese Tabelle)“.
                if (w.Tabelle == null)
                    return new Entscheid { Art = Entscheidart.Text, Text = string.IsNullOrEmpty(w.Grund) ? w.Text : w.Text + " (" + w.Grund + ")" };
                return new Entscheid { Art = Entscheidart.Tabelle, Tabelle = w.Tabelle, Angaben = m.Angaben };
            }

            /// <summary>
            /// Setzt die Tabelle an die Stelle des Absatzes (bzw. Steuerelements) <paramref name="bezug"/>: je Spaltenblock
            /// eine Word-Tabelle (<c>|block n</c>, sonst drei Varianten je Block), dazwischen ein leerer Absatz im Format
            /// der Stelle, darunter die Hinweise der Tabelle im Format „Hinweis“. Danach entfällt der Bezug.
            /// </summary>
            private void FuelleTabelle(OpenXmlElement bezug, Teilinfo ti, Entscheid e)
            {
                Berichtstabelle t = e.Tabelle;
                int groesse = e.Angaben.Where(a => a.Art == Formatangabeart.Block && a.Zahl.HasValue)
                                       .Select(a => a.Zahl.Value).DefaultIfEmpty(Berichtstabelle.BLOCKGROESSE).First();
                ParagraphProperties format = (bezug as Paragraph)?.ParagraphProperties
                                             ?? bezug.Descendants<Paragraph>().FirstOrDefault()?.ParagraphProperties;
                int inhaltsbreite = WordKontext.InhaltsbreiteAus(Einfuegeanker.Vor(bezug, ti.Teil).Abschnitt());
                inhaltsbreite = Math.Min(inhaltsbreite, Zellbreite(bezug) ?? inhaltsbreite);

                string stil = _stile.Finde(WordVorlagenstile.TABELLE);
                if (stil == null && _muster != null) stil = _stile.Id(WordVorlagenstile.TABELLE);

                var neue = new List<OpenXmlElement>();
                // Steht unmittelbar davor eine Tabelle (etwa die des vorigen Platzhalters), trennt sie ein Absatz.
                if (bezug.PreviousSibling() is Table) neue.Add(LeererAbsatz(format));
                IReadOnlyList<IReadOnlyList<int>> bloecke = t.Bloecke(groesse);
                for (int b = 0; b < bloecke.Count; b++)
                {
                    if (b > 0) neue.Add(LeererAbsatz(format));
                    neue.Add(WordTabellenbau.Baue(t, bloecke[b], inhaltsbreite, stil, _muster));
                }
                string hinweisstil = t.Hinweise.Count > 0 ? _stile.Id(WordVorlagenstile.HINWEIS) : null;
                foreach (string h in t.Hinweise)
                {
                    var p = new Paragraph();
                    if (hinweisstil != null) p.AppendChild(new ParagraphProperties(new ParagraphStyleId { Val = hinweisstil }));
                    p.AppendChild(new Run(new Text(h) { Space = SpaceProcessingModeValues.Preserve }));
                    neue.Add(p);
                }
                // Eine Tabelle braucht einen Absatz hinter sich, bevor eine weitere Tabelle, das Ende der Zelle oder die
                // Abschnittsangabe folgt — sonst verschmölze sie mit der nächsten oder das Dokument wäre ungültig.
                if (!(neue[neue.Count - 1] is Paragraph) && !(bezug.NextSibling() is Paragraph)) neue.Add(LeererAbsatz(format));
                ErsetzeAbsatz(bezug, neue);
            }

            /// <summary>Die Breite der Zelle, in der der Bezug steht (DXA); <c>null</c> außerhalb einer Zelle.</summary>
            private static int? Zellbreite(OpenXmlElement bezug)
            {
                TableCell zelle = bezug.Ancestors<TableCell>().FirstOrDefault();
                TableCellWidth w = zelle?.TableCellProperties?.TableCellWidth;
                if (w?.Width?.Value == null) return null;
                if (w.Type != null && w.Type.Value != TableWidthUnitValues.Dxa) return null;
                return int.TryParse(w.Width.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) && d > 0
                    ? Math.Max(WordKontext.MIN_INHALTSBREITE, d - 216) : (int?)null;
            }
        }
    }

    /// <summary>
    /// <b>Der Vorlagenweg einer <see cref="Berichtstabelle"/></b> (Konzept Berichtsvorlagen 6.4 Nr. 2): eine Word-Tabelle
    /// mit der Breite in Prozent (<c>w:tblW w:type="pct"</c>, Zellen ebenso; das Raster in DXA nach der Inhaltsbreite).
    /// <list type="bullet">
    /// <item>Mit Tabellenformatvorlage (<c>EPOS Tabelle</c>) trägt sie Rahmen, Kopfzeile und Schrift; die Kopfzeile ist
    /// als solche markiert und wiederholt sich je Seite.</item>
    /// <item>Ohne Formatvorlage die heutige Direktformatierung des Bausteinwegs (Rahmen, Kopfhinterlegung, 9 bzw.
    /// 7 pt).</item>
    /// <item>Die Rollen Stamm, Gruppe, Summe und Warnung nehmen Schattierung und Zeichenformat der Mustertabelle; eine
    /// Rolle ohne Muster und jede Zelle ohne Rolle die Direktformatierung ihrer Zelle.</item>
    /// </list>
    /// </summary>
    public static class WordTabellenbau
    {
        /// <summary>Baut den Block <paramref name="block"/> der Tabelle.</summary>
        public static Table Baue(Berichtstabelle t, IReadOnlyList<int> block, int inhaltsbreite, string stilId, Tabellenmuster muster)
        {
            bool mitStil = !string.IsNullOrEmpty(stilId);
            int schrift = t.Schmal ? WordBerichtGenerator.SCHRIFT_TABELLE_SCHMAL : WordBerichtGenerator.SCHRIFT_TABELLE;
            int[] pct = t.Prozente(block);
            int[] dxa = pct.Select(p => (int)Math.Round(p * (double)inhaltsbreite / 5000.0)).ToArray();

            var tabelle = new Table();
            var tp = new TableProperties();
            if (mitStil) tp.Append(new TableStyle { Val = stilId });
            tp.Append(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" });
            if (!mitStil)
                tp.Append(new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                    new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                    new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                    new RightBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = WordBerichtGenerator.RAHMEN }));
            tp.Append(new TableLayout { Type = TableLayoutValues.Fixed });
            if (mitStil)
                // Nur der Hexwert (Kopfzeile 0x0020, ohne Bänder 0x0600) — die einzelnen Attribute kennt Office 2007 nicht.
                tp.Append(new TableLook { Val = t.Kopf != null ? "0620" : "0600" });
            tabelle.Append(tp);

            var raster = new TableGrid();
            foreach (int b in dxa) raster.Append(new GridColumn { Width = b.ToString(CultureInfo.InvariantCulture) });
            tabelle.Append(raster);

            if (t.Kopf != null) tabelle.Append(Zeile(t.Kopf, block, pct, true, mitStil, muster, schrift));
            foreach (Tabellenzeile z in t.Zeilen) tabelle.Append(Zeile(z, block, pct, false, mitStil, muster, schrift));
            return tabelle;
        }

        private static TableRow Zeile(Tabellenzeile z, IReadOnlyList<int> block, int[] pct, bool kopf, bool mitStil,
                                      Tabellenmuster muster, int schrift)
        {
            var tr = new TableRow();
            if (kopf) tr.Append(new TableRowProperties(new TableHeader()));
            for (int i = 0; i < block.Count; i++)
            {
                int s = block[i];
                Tabellenzelle c = s < z.Zellen.Count ? z.Zellen[s] : new Tabellenzelle();
                tr.Append(Zelle(c, pct[i], kopf, mitStil, muster, schrift));
            }
            return tr;
        }

        private static TableCell Zelle(Tabellenzelle c, int pct, bool kopf, bool mitStil, Tabellenmuster muster, int schrift)
        {
            Tabellenrolle rolle = kopf || muster == null ? Tabellenrolle.Keine : muster.Waehle(c.Rolle);
            bool direkt = !(kopf && mitStil);   // die Kopfzeile formatiert die Formatvorlage

            var tcp = new TableCellProperties();
            tcp.Append(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = pct.ToString(CultureInfo.InvariantCulture) });
            Shading shd = rolle != Tabellenrolle.Keine ? muster.Schattierung(rolle)
                        : direkt && WordTabellenschreiber.Fuellung(c.Hinterlegung) != null
                            ? new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = WordTabellenschreiber.Fuellung(c.Hinterlegung) }
                            : null;
            if (shd != null) tcp.Append(shd);
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });

            var pp = new ParagraphProperties();
            pp.Append(new SpacingBetweenLines { Before = "20", After = "20" });
            pp.Append(new Justification { Val = WordTabellenschreiber.Ausrichtung(c.Ausrichtung) });

            RunProperties rp = rolle != Tabellenrolle.Keine ? muster.Zeichen(rolle) : null;
            if (rp == null)
            {
                rp = new RunProperties();
                if (direkt && c.Fett) rp.Append(new Bold());
                if (!mitStil || schrift != WordBerichtGenerator.SCHRIFT_TABELLE)
                    rp.Append(new FontSize { Val = schrift.ToString(CultureInfo.InvariantCulture) });
            }
            var r = new Run();
            if (rp.HasChildren) r.Append(rp);
            r.Append(new Text(c.Text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            return new TableCell(tcp, new Paragraph(pp, r));
        }
    }
}
