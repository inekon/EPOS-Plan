using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Erzeugt den Projektvergleich als echte Word-Datei (.docx) über das OpenXML SDK
    /// (NuGet: DocumentFormat.OpenXml – kein installiertes Word nötig).
    ///
    /// Datengrundlage: jedes Projekt der Gruppe wird zu Beginn FRISCH simuliert
    /// (SimulationRunner.SimuliereUndSpeichere, Muster Form_Variantentest), danach
    /// liest ErgebnisCtrl.Load(idProjekt) den eben geschriebenen Lauf. Damit steht auch
    /// dieser Weg nie auf veralteten Ergebnissen (Nutzeranforderung 15.08.2026) — er
    /// wird mit Phase 2 des Berichtsmoduls ohnehin durch Form_Bericht abgelöst.
    /// Verglichen werden Stamm + Variante spaltenweise. Zusätzlich Kuchendiagramme
    /// (Wärme-/Stromdeckung) je Projekt, gerendert als PNG (System.Drawing) und eingebettet.
    /// </summary>
    public class ProjektvergleichBericht
    {
        /// <summary>
        /// Meldungen der Simulationsläufe dieses Berichts (Warnungen/Hinweise der Engine
        /// je Projekt, Paket-8-Fehlerkanal). Der Aufrufer zeigt sie mit der
        /// Abschlussmeldung an — stille Ersatzannahmen bleiben so sichtbar.
        /// </summary>
        public readonly List<string> Laufmeldungen = new List<string>();
        public class Projekt
        {
            public int Id;
            public string Name = "";
            public string Bezeichner = "";
            public bool IstStamm;
        }

        private const int CONTENT_W = 9026;      // A4 abzüglich 1"-Ränder (DXA)
        private const string HEAD_FILL = "D9E1F2";
        private const string STAMM_FILL = "F2F2F2";
        private const string GREY = "595959";
        private const string BLUE = "1F4E79";

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // Farben für die Diagramm-Segmente.
        private static readonly System.Drawing.Color CWP = System.Drawing.Color.FromArgb(0x41, 0x72, 0xC4);
        private static readonly System.Drawing.Color CBHKW = System.Drawing.Color.FromArgb(0xED, 0x7D, 0x31);
        private static readonly System.Drawing.Color CKessel = System.Drawing.Color.FromArgb(0x80, 0x80, 0x80);
        private static readonly System.Drawing.Color CSolar = System.Drawing.Color.FromArgb(0xFF, 0xC0, 0x00);
        private static readonly System.Drawing.Color CPV = System.Drawing.Color.FromArgb(0x70, 0xAD, 0x47);
        private static readonly System.Drawing.Color CRest = System.Drawing.Color.FromArgb(0xBF, 0xBF, 0xBF);

        private uint _bildId = 1;   // fortlaufende DrawingObject-ID

        private class KZ
        {
            public string Label;
            public Func<ErgebnisModel, string> Wert;
            public KZ(string label, Func<ErgebnisModel, string> wert) { Label = label; Wert = wert; }
        }

        // Ein Kuchen-Segment: Beschriftung, Wert (%), Farbe.
        private class Segment
        {
            public string Label;
            public double Wert;
            public System.Drawing.Color Farbe;
            public Segment(string l, double w, System.Drawing.Color f) { Label = l; Wert = w; Farbe = f; }
        }

        /// <summary>
        /// Simuliert jedes Projekt der Vergleichsgruppe frisch und speichert das Ergebnis
        /// (frische SimulationRunner-Instanz je Projekt — Muster
        /// Form_Variantentest.btnSimulieren_Click). Ein gescheitertes Projekt bricht den
        /// Bericht nicht ab: es wird mit Namen gemeldet und der Bericht läuft mit dem
        /// zuletzt gespeicherten Stand weiter — dasselbe Verhalten wie im
        /// BerichtsDatenSammler.
        /// </summary>
        private void SimuliereGruppe(List<Projekt> gruppe)
        {
            var erledigt = new HashSet<int>();
            foreach (Projekt pr in gruppe)
            {
                if (!erledigt.Add(pr.Id)) continue;
                string wer = (pr.IstStamm ? "Stamm" : "Variante") + " '" +
                             (string.IsNullOrEmpty(pr.Bezeichner) ? pr.Name : pr.Bezeichner) + "'";
                try
                {
                    var runner = new SimulationRunner();
                    string fehler;
                    int erg = runner.SimuliereUndSpeichere(pr.Id, out fehler);

                    if (!runner.LaufOk)
                        Laufmeldungen.Add(wer + ": Simulation fehlgeschlagen — " +
                                          (fehler ?? "unbekannter Fehler") +
                                          ". Der Bericht zeigt den zuletzt gespeicherten Stand.");
                    else if (erg <= 0)
                        Laufmeldungen.Add(wer + ": Das frisch gerechnete Ergebnis konnte nicht " +
                                          "gespeichert werden" +
                                          (string.IsNullOrEmpty(fehler) ? "." : " (" + fehler + ")."));

                    // Paket-8-Fehlerkanal: auch ein erfolgreicher Lauf kann mit einer
                    // Ersatzannahme gerechnet haben — das gehört in die Abschlussmeldung.
                    if (runner.Protokoll != null)
                    {
                        foreach (string w in runner.Protokoll.Warnungen) Laufmeldungen.Add(wer + ": " + w);
                        foreach (string h in runner.Protokoll.Hinweise) Laufmeldungen.Add(wer + ": " + h);
                    }
                }
                catch (Exception ex)
                {
                    Laufmeldungen.Add(wer + ": Simulation fehlgeschlagen — " + ex.Message +
                                      ". Der Bericht zeigt den zuletzt gespeicherten Stand.");
                }
            }
        }

        public void Erzeuge(string pfad, List<Projekt> gruppe)
        {
            if (gruppe == null || gruppe.Count == 0) throw new ArgumentException("Vergleichsgruppe ist leer.");

            Laufmeldungen.Clear();
            SimuliereGruppe(gruppe);

            var ergs = new Dictionary<int, ErgebnisModel>();
            foreach (Projekt pr in gruppe)
                if (!ergs.ContainsKey(pr.Id)) ergs[pr.Id] = new ErgebnisCtrl().Load(pr.Id);

            Dictionary<int, string> klimaNamen = LadeKlimaregionen();

            int cols = gruppe.Count;
            // Δ-Spalte (Variante − Stamm) nur bei genau einem Stamm + einer Variante.
            bool mitDelta = gruppe.Count == 2 && gruppe.Any(x => x.IstStamm) && gruppe.Any(x => !x.IstStamm);
            int dataCols = cols + (mitDelta ? 1 : 0);
            int wLabel = 2800;
            int wCol = (CONTENT_W - wLabel) / dataCols;
            int[] W = new int[dataCols + 1];
            W[0] = wLabel;
            for (int i = 1; i <= dataCols; i++) W[i] = wCol;
            W[dataCols] = CONTENT_W - wLabel - wCol * (dataCols - 1);

            using (WordprocessingDocument doc = WordprocessingDocument.Create(pfad, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                main.Document = new Document();
                Body body = main.Document.AppendChild(new Body());

                // Kopf / Titel
                body.Append(Par("INEKON – Heizungssimulation", false, "18", GREY, JustificationValues.Left, 0, 0));
                body.Append(Trennlinie());
                body.Append(Par("Projektvergleich – Wirtschaftlichkeit & Energie", true, "40", null, JustificationValues.Left, 120, 40));
                body.Append(Par("Datengrundlage: Tab_Ergebnis je Projekt (jeweils letzter Lauf).", true, "18", GREY, JustificationValues.Left, 0, 120));

                // 1. Übersicht
                body.Append(Heading("1. Übersicht der Vergleichsgruppe"));
                body.Append(UebersichtTabelle(gruppe, ergs, klimaNamen));

                // 2. Energiebedarf
                body.Append(Heading("2. Energiebedarf"));
                body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                {
                    new KZ("Wärmebedarf gesamt [MWh/a]", m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Waermebedarf_Gesamt, 0)),
                    new KZ("Wärmelast max. [kW]",        m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Waermelast_Max, 0)),
                    new KZ("Strombedarf gesamt [MWh/a]", m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Strombedarf_Gesamt, 0)),
                    new KZ("Strombedarf max. [kW]",      m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Strombedarf_Max, 0)),
                }));

                // 3. Erzeuger
                bool anyWP = gruppe.Any(pr => ergs[pr.Id] != null && ergs[pr.Id].Waermepumpe != null);
                bool anyBHKW = gruppe.Any(pr => ergs[pr.Id] != null && ergs[pr.Id].BHKW != null);
                bool anySPK = gruppe.Any(pr => ergs[pr.Id] != null && ergs[pr.Id].Heizkessel != null);
                bool anySolar = gruppe.Any(pr => ergs[pr.Id] != null && ergs[pr.Id].Solarthermie != null);
                bool anyPV = gruppe.Any(pr => ergs[pr.Id] != null && ergs[pr.Id].Photovoltaik != null);

                int sub = 0;
                if (anyWP || anyBHKW || anySPK || anySolar || anyPV)
                    body.Append(Heading("3. Wärme- & Stromversorgung (Deckung je Erzeuger)"));

                if (anyWP)
                {
                    body.Append(Heading2("3." + (++sub) + " Wärmepumpe"));
                    body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                    {
                        new KZ("Wärmeproduktion WP [MWh/a]", m => m.Waermepumpe == null ? "–" : F(m.Waermepumpe.Waermeproduktion_WP, 0)),
                        new KZ("Stromverbrauch WP [MWh/a]",  m => m.Waermepumpe == null ? "–" : F(m.Waermepumpe.Stromverbrauch_WP, 0)),
                        new KZ("Heizstab [MWh/a]",           m => m.Waermepumpe == null ? "–" : F(m.Waermepumpe.Stromverbrauch_Heizstab, 0)),
                        new KZ("Wärmebedarfsdeckung [%]",    m => m.Waermepumpe == null ? "–" : F(m.Waermepumpe.Waermebedarfsdeckung, 1)),
                        new KZ("Vollbenutzungsstunden [h]",  m => m.Waermepumpe == null ? "–" : F(m.Waermepumpe.Vollbenutzungsstunden, 0)),
                        new KZ("Bivalenzpunkt [°C]",         m => (m.Waermepumpe == null || !m.Waermepumpe.Bivalenzpunkt.HasValue) ? "–" : F(m.Waermepumpe.Bivalenzpunkt.Value, 1)),
                    }));
                }

                if (anyBHKW)
                {
                    body.Append(Heading2("3." + (++sub) + " BHKW"));
                    body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                    {
                        new KZ("Wärmeproduktion [MWh/a]",   m => m.BHKW == null ? "–" : F(m.BHKW.Waermeproduktion, 0)),
                        new KZ("Stromproduktion [MWh/a]",   m => m.BHKW == null ? "–" : F(m.BHKW.Stromproduktion, 0)),
                        new KZ("Wärmebedarfsdeckung [%]",   m => m.BHKW == null ? "–" : F(m.BHKW.Waermebedarfsdeckung, 1)),
                        new KZ("Betriebsstunden [h]",       m => m.BHKW == null ? "–" : F(m.BHKW.Betriebsstunden_Gesamt, 0)),
                    }));
                }

                if (anySPK)
                {
                    body.Append(Heading2("3." + (++sub) + " Spitzenkessel"));
                    body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                    {
                        new KZ("Wärmeproduktion [MWh/a]", m => m.Heizkessel == null ? "–" : F(m.Heizkessel.Waermeproduktion, 0)),
                        new KZ("Wärmebedarfsdeckung [%]", m => m.Heizkessel == null ? "–" : F(m.Heizkessel.Waermebedarfsdeckung, 1)),
                    }));
                }

                if (anySolar)
                {
                    body.Append(Heading2("3." + (++sub) + " Solarthermie"));
                    body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                    {
                        new KZ("Wärmeproduktion [MWh/a]", m => m.Solarthermie == null ? "–" : F(m.Solarthermie.Waermeproduktion, 0)),
                        new KZ("Wärmebedarfsdeckung [%]", m => m.Solarthermie == null ? "–" : F(m.Solarthermie.Waermebedarfsdeckung, 1)),
                        new KZ("Überschuss [MWh/a]",      m => m.Solarthermie == null ? "–" : F(m.Solarthermie.Ueberschuss, 0)),
                    }));
                }

                if (anyPV)
                {
                    body.Append(Heading2("3." + (++sub) + " Photovoltaik"));
                    body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                    {
                        new KZ("Stromproduktion [MWh/a]", m => m.Photovoltaik == null ? "–" : F(m.Photovoltaik.Stromproduktion, 0)),
                        new KZ("Strombedarfsdeckung [%]", m => m.Photovoltaik == null ? "–" : F(m.Photovoltaik.Strombedarfsdeckung, 1)),
                        new KZ("Netzüberschuss [MWh/a]",  m => m.Photovoltaik == null ? "–" : F(m.Photovoltaik.Ueberschuss, 0)),
                    }));
                }

                // Erzeuger – Einzelauflistung je Projekt (fortlaufende Unternummer)
                if (anyWP || anyBHKW || anySPK || anySolar || anyPV)
                {
                    body.Append(Heading2("3." + (++sub) + " Erzeuger – Einzelauflistung je Projekt"));
                    body.Append(Par("Je Projekt eine Zeile pro einzelnem Gerät (Modul) mit Name und erzeugter Energie; " +
                                    "bei BHKW/Spitzenkessel der Brennstoff, bei der Wärmepumpe Strom (inkl. Heizstab).",
                                    false, "18", GREY, JustificationValues.Left, 0, 100));
                    foreach (Projekt pr in gruppe)
                    {
                        ErgebnisModel me = ergs[pr.Id];
                        body.Append(Par((pr.IstStamm ? "Stamm – " : "Variante – ") + (pr.Name ?? ""),
                                        true, "20", BLUE, JustificationValues.Left, 120, 40));
                        if (me == null) { body.Append(Par("(kein Ergebnis vorhanden)", false, "18", GREY, JustificationValues.Left, 0, 60)); continue; }
                        body.Append(ErzeugerEinzelTabelle(me));
                    }
                }

                // Brennstoffmengen (BHKW & Spitzenkessel) – Verbrauch über effektiven Heizwert umgerechnet
                if (anyBHKW || anySPK)
                {
                    body.Append(Heading2("3." + (++sub) + " Brennstoffmengen (BHKW & Spitzenkessel)"));
                    body.Append(Par("Aus dem Brennstoffverbrauch über den projektspezifischen effektiven Heizwert " +
                                    "(custom_hi/hs mit Fallback auf den Katalog-Default) umgerechnete Menge in der Abrechnungseinheit.",
                                    false, "18", GREY, JustificationValues.Left, 0, 100));
                    foreach (Projekt pr in gruppe)
                    {
                        body.Append(Par((pr.IstStamm ? "Stamm – " : "Variante – ") + (pr.Name ?? ""),
                                        true, "20", BLUE, JustificationValues.Left, 120, 40));
                        body.Append(BrennstoffmengenTabelle(pr.Id));
                    }
                }

                // 4. Restbedarf (nach den Erzeugern)
                body.Append(Heading("4. Wärmerestbedarf & Stromrestbedarf"));
                body.Append(VergleichsTabelle(W, "Kennzahl", gruppe, ergs, new List<KZ>
                {
                    new KZ("Wärmerestbedarf [MWh/a]", m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Waermerestbedarf, 0)),
                    new KZ("Stromrestbedarf [MWh/a]", m => m.Energiebedarf == null ? "–" : F(m.Energiebedarf.Stromrestbedarf, 0)),
                }));

                // 5. Deckungsdiagramme (Kuchendiagramme je Projekt)
                body.Append(Heading("5. Deckungsdiagramme"));
                body.Append(Par("Anteile an der Wärme- bzw. Stromdeckung je Projekt (aus den Deckungsgraden der Erzeuger; " +
                                "der Rest ist ungedeckte Wärme bzw. Netzbezug).", false, "18", GREY, JustificationValues.Left, 0, 100));
                foreach (Projekt pr in gruppe)
                {
                    ErgebnisModel m = ergs[pr.Id];
                    body.Append(Heading2((pr.IstStamm ? "Stamm – " : "Variante – ") + pr.Name));
                    if (m == null) { body.Append(Par("(kein Ergebnis vorhanden)", false, "18", GREY, JustificationValues.Left, 0, 60)); continue; }
                    DiagrammeEinfuegen(body, main, m, pr.Name);
                }

                // 6. Wirtschaftlichkeit (Platzhalter)
                body.Append(Heading("6. Wirtschaftlichkeit"));
                body.Append(Par("Die Kostenkennzahlen (Investition, Energie-/Betriebskosten, Amortisation, CO₂) sind im " +
                                "Ergebnismodell vorgesehen, werden aber aktuell noch nicht berechnet.", false, "20", null, JustificationValues.Left, 0, 120));

                // 7. Datengrundlage
                body.Append(Heading("7. Datengrundlage & Methodik"));
                body.Append(Par("Grundlage sind die je Projekt gespeicherten Simulationsergebnisse (Tab_Ergebnis, jeweils " +
                                "letzter Lauf, verknüpft über ID_Projekt).", false, "20", null, JustificationValues.Left, 0, 120));

                main.Document.Save();
            }
        }

        // -------------------------------------------------------------- Diagramme

        private void DiagrammeEinfuegen(Body body, MainDocumentPart main, ErgebnisModel m, string projektName)
        {
            try
            {
                // Wärmedeckung
                var segW = new List<Segment>();
                double sumW = 0;
                if (m.Waermepumpe != null && m.Waermepumpe.Waermebedarfsdeckung > 0) { segW.Add(new Segment("Wärmepumpe", m.Waermepumpe.Waermebedarfsdeckung, CWP)); sumW += m.Waermepumpe.Waermebedarfsdeckung; }
                if (m.BHKW != null && m.BHKW.Waermebedarfsdeckung > 0) { segW.Add(new Segment("BHKW", m.BHKW.Waermebedarfsdeckung, CBHKW)); sumW += m.BHKW.Waermebedarfsdeckung; }
                if (m.Heizkessel != null && m.Heizkessel.Waermebedarfsdeckung > 0) { segW.Add(new Segment("Spitzenkessel", m.Heizkessel.Waermebedarfsdeckung, CKessel)); sumW += m.Heizkessel.Waermebedarfsdeckung; }
                if (m.Solarthermie != null && m.Solarthermie.Waermebedarfsdeckung > 0) { segW.Add(new Segment("Solarthermie", m.Solarthermie.Waermebedarfsdeckung, CSolar)); sumW += m.Solarthermie.Waermebedarfsdeckung; }
                if (100.0 - sumW > 0.05) segW.Add(new Segment("Rest/ungedeckt", 100.0 - sumW, CRest));
                // Stromdeckung
                var segS = new List<Segment>();
                double sumS = 0;
                if (m.Photovoltaik != null && m.Photovoltaik.Strombedarfsdeckung > 0) { segS.Add(new Segment("Photovoltaik", m.Photovoltaik.Strombedarfsdeckung, CPV)); sumS += m.Photovoltaik.Strombedarfsdeckung; }
                if (m.BHKW != null && m.BHKW.Strombedarfsdeckung > 0) { segS.Add(new Segment("BHKW", m.BHKW.Strombedarfsdeckung, CBHKW)); sumS += m.BHKW.Strombedarfsdeckung; }
                if (100.0 - sumS > 0.05) segS.Add(new Segment("Netzbezug", 100.0 - sumS, CKessel));
                // Beide Diagramme nebeneinander in einem Absatz
                Paragraph pDia = new Paragraph();
                bool hatBild = false;
                if (segW.Count > 0) { pDia.Append(new Run(BildDrawing(main, PieChartPng("Wärmedeckung", segW), 240, 150))); hatBild = true; }
                if (segS.Count > 0) { if (hatBild) pDia.Append(RunT("     ", false, "18", null)); pDia.Append(new Run(BildDrawing(main, PieChartPng("Stromdeckung", segS), 240, 150))); hatBild = true; }
                if (hatBild) body.Append(pDia);
            }
            catch
            {
                body.Append(Par("(Diagramm konnte nicht erzeugt werden)", false, "18", GREY, JustificationValues.Left, 0, 60));
            }
        }

        // Rendert ein Kuchendiagramm als PNG (System.Drawing).
        private static byte[] PieChartPng(string titel, List<Segment> segmente)
        {
            int Wpx = 480, Hpx = 300;
            using (var bmp = new System.Drawing.Bitmap(Wpx, Hpx))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.White);

                using (var tf = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold))
                    g.DrawString(titel, tf, System.Drawing.Brushes.Black, 10f, 8f);

                double total = 0; foreach (var s in segmente) total += s.Wert;
                if (total <= 0) total = 1;

                var pieRect = new System.Drawing.RectangleF(20f, 40f, 230f, 230f);
                float start = -90f;
                foreach (var s in segmente)
                {
                    float sweep = (float)(s.Wert / total * 360.0);
                    using (var b = new System.Drawing.SolidBrush(s.Farbe)) g.FillPie(b, pieRect.X, pieRect.Y, pieRect.Width, pieRect.Height, start, sweep);
                    start += sweep;
                }
                using (var wp = new System.Drawing.Pen(System.Drawing.Color.White, 1.5f)) g.DrawEllipse(wp, pieRect);

                float lx = 280f, ly = 50f;
                using (var lf = new System.Drawing.Font("Segoe UI", 9.5f))
                {
                    foreach (var s in segmente)
                    {
                        using (var b = new System.Drawing.SolidBrush(s.Farbe)) g.FillRectangle(b, lx, ly, 14f, 14f);
                        using (var gp = new System.Drawing.Pen(System.Drawing.Color.Gray)) g.DrawRectangle(gp, lx, ly, 14f, 14f);
                        string txt = s.Label + "   " + (s.Wert / total * 100.0).ToString("N1", DE) + " %";
                        g.DrawString(txt, lf, System.Drawing.Brushes.Black, lx + 20f, ly - 1f);
                        ly += 24f;
                    }
                }

                using (var ms = new System.IO.MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        // Baut eine Inline-Grafik aus einem PNG (Größe in Pixel -> EMU) und liefert das Drawing.
        private Drawing BildDrawing(MainDocumentPart main, byte[] png, int wPx, int hPx)
        {
            ImagePart imgPart = main.AddImagePart(ImagePartType.Png);
            using (var ms = new System.IO.MemoryStream(png)) imgPart.FeedData(ms);
            string relId = main.GetIdOfPart(imgPart);

            long cx = (long)wPx * 9525L;   // 1 px @96dpi = 9525 EMU
            long cy = (long)hPx * 9525L;
            uint id = _bildId++;

            var drawing = new Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = cx, Cy = cy },
                    new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties() { Id = id, Name = "Diagramm" + id },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties() { Id = 0U, Name = "Diagramm" + id + ".png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip() { Embed = relId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

            return drawing;
        }

        // -------------------------------------------------------------- Tabellen

        private Table UebersichtTabelle(List<Projekt> gruppe, Dictionary<int, ErgebnisModel> ergs, Dictionary<int, string> klimaNamen)
        {
            int[] w = { 1400, 3826, 1900, 1900 };   // Rolle, Projektname, Klimaregion, Ergebnis vom
            Table t = NewTable(w);
            t.Append(new TableRow(
                Cell("Rolle", w[0], true, HEAD_FILL, JustificationValues.Left),
                Cell("Projektname", w[1], true, HEAD_FILL, JustificationValues.Left),
                Cell("Klimaregion", w[2], true, HEAD_FILL, JustificationValues.Left),
                Cell("Ergebnis vom", w[3], true, HEAD_FILL, JustificationValues.Center)));

            foreach (Projekt pr in gruppe)
            {
                ErgebnisModel m = ergs[pr.Id];
                string klima = "–";
                if (m != null)
                    klima = (klimaNamen != null && klimaNamen.ContainsKey(m.ID_Klimaregion))
                        ? klimaNamen[m.ID_Klimaregion] : m.ID_Klimaregion.ToString(DE);
                string stand = (m != null && m.Zeitstempel != default(DateTime)) ? m.Zeitstempel.ToString("dd.MM.yyyy HH:mm", DE)
                              : (m != null ? "(ohne Datum)" : "(kein Ergebnis)");
                t.Append(new TableRow(
                    Cell(pr.IstStamm ? "Stamm" : "Variante", w[0], pr.IstStamm, pr.IstStamm ? STAMM_FILL : null, JustificationValues.Left),
                    Cell(pr.Name ?? "", w[1], false, null, JustificationValues.Left),
                    Cell(klima, w[2], false, null, JustificationValues.Left),
                    Cell(stand, w[3], false, null, JustificationValues.Center)));
            }
            return t;
        }

        private Table VergleichsTabelle(int[] w, string kzHeader, List<Projekt> gruppe,
                                        Dictionary<int, ErgebnisModel> ergs, List<KZ> zeilen)
        {
            bool mitDelta = gruppe.Count == 2 && gruppe.Any(x => x.IstStamm) && gruppe.Any(x => !x.IstStamm);
            int deltaCol = gruppe.Count + 1;   // Spaltenindex der Δ-Spalte in w

            Table t = NewTable(w);

            TableRow head = new TableRow();
            head.Append(Cell(kzHeader, w[0], true, HEAD_FILL, JustificationValues.Left));
            for (int i = 0; i < gruppe.Count; i++)
            {
                Projekt pr = gruppe[i];
                string titel = pr.IstStamm ? "Stamm" : (string.IsNullOrEmpty(pr.Bezeichner) ? "Variante" : pr.Bezeichner);
                head.Append(Cell(titel, w[i + 1], true, HEAD_FILL, JustificationValues.Center));
            }
            if (mitDelta) head.Append(Cell("Δ (Var. − Stamm)", w[deltaCol], true, HEAD_FILL, JustificationValues.Center));
            t.Append(head);

            foreach (KZ z in zeilen)
            {
                TableRow row = new TableRow();
                row.Append(Cell(z.Label, w[0], false, null, JustificationValues.Left));
                string stammTxt = null, varTxt = null;
                for (int i = 0; i < gruppe.Count; i++)
                {
                    Projekt pr = gruppe[i];
                    ErgebnisModel m = ergs[pr.Id];
                    string wert = m == null ? "–" : z.Wert(m);
                    if (pr.IstStamm) stammTxt = wert; else varTxt = wert;
                    row.Append(Cell(wert, w[i + 1], false, pr.IstStamm ? STAMM_FILL : null, Just(wert, JustificationValues.Right)));
                }
                if (mitDelta)
                {
                    string d = Delta(stammTxt, varTxt);
                    row.Append(Cell(d, w[deltaCol], false, null, Just(d, JustificationValues.Right)));
                }
                t.Append(row);
            }
            return t;
        }

        // Einzelauflistung der Erzeuger – eine Zeile je einzelnem Gerät (Modul).
        private Table ErzeugerEinzelTabelle(ErgebnisModel m)
        {
            int[] w = { 2600, 1500, 1500, 1826, 1600 };   // Erzeuger, Wärme, Strom, Energieträger, Verbrauch
            Table t = NewTable(w);
            t.Append(new TableRow(
                Cell("Erzeuger", w[0], true, HEAD_FILL, JustificationValues.Left),
                Cell("Wärme [MWh/a]", w[1], true, HEAD_FILL, JustificationValues.Center),
                Cell("Strom [MWh/a]", w[2], true, HEAD_FILL, JustificationValues.Center),
                Cell("Energieträger", w[3], true, HEAD_FILL, JustificationValues.Left),
                Cell("Verbrauch [MWh/a]", w[4], true, HEAD_FILL, JustificationValues.Center)));

            // Wärmepumpe(n) – je Modul; Energieträger Strom, Verbrauch = Stromverbrauch + Heizstab.
            // Differenz zum WP-Aggregat (Pufferspeicher-Ladung aus WP-Überschuss) als Extrazeile,
            // damit die Modulsummen wieder mit dem Aggregat (3.1) zusammenpassen.
            if (m.Waermepumpe != null)
            {
                var mods = m.Waermepumpe.Module;
                if (mods != null && mods.Count > 0)
                {
                    double wSum = 0, vSum = 0;
                    foreach (var mo in mods)
                    {
                        t.Append(ErzeugerZeile(w, Name(mo.Modul, "Wärmepumpe"), F(mo.Waermeproduktion, 0), "–",
                                               "Strom", F(mo.Stromverbrauch + mo.Heizstab, 0)));
                        wSum += mo.Waermeproduktion;
                        vSum += mo.Stromverbrauch + mo.Heizstab;
                    }
                    double pWaerme = m.Waermepumpe.Waermeproduktion_WP - wSum;
                    double pStrom = (m.Waermepumpe.Stromverbrauch_WP + m.Waermepumpe.Stromverbrauch_Heizstab) - vSum;
                    if (pWaerme > 0.05 || pStrom > 0.05)
                        t.Append(ErzeugerZeile(w, "Pufferspeicher (WP-Überschuss)", F(pWaerme, 0), "–", "Strom", F(pStrom, 0)));
                }
                else
                    t.Append(ErzeugerZeile(w, "Wärmepumpe", F(m.Waermepumpe.Waermeproduktion_WP, 0), "–",
                                           "Strom", F(m.Waermepumpe.Stromverbrauch_WP + m.Waermepumpe.Stromverbrauch_Heizstab, 0)));
            }

            // BHKW – je Modul; der eine Brennstoff (aus dem Aggregat) in der ersten Zeile
            if (m.BHKW != null)
            {
                string art; double verb; BHKWBrennstoff(m.BHKW, out art, out verb);
                string vTxt = art == "–" ? "–" : F(verb, 0);
                var mods = m.BHKW.Module;
                if (mods != null && mods.Count > 0)
                {
                    bool first = true;
                    foreach (var mo in mods)
                    {
                        t.Append(ErzeugerZeile(w, Name(mo.Modul, "BHKW"), F(mo.Waermeproduktion, 0), F(mo.Stromproduktion, 0),
                                               first ? art : "–", first ? vTxt : "–"));
                        first = false;
                    }
                }
                else
                    t.Append(ErzeugerZeile(w, "BHKW", F(m.BHKW.Waermeproduktion, 0), F(m.BHKW.Stromproduktion, 0), art, vTxt));
            }

            // Spitzenkessel – je Modul; Energieträger aus Aggregat (inkl. Strom beim E-Kessel).
            if (m.Heizkessel != null)
            {
                string art; double verb; HeizkesselBrennstoff(m.Heizkessel, out art, out verb);
                string vTxt = art == "–" ? "–" : F(verb, 0);
                var mods = m.Heizkessel.Module;
                double modSumme = 0; if (mods != null) foreach (var mo in mods) modSumme += (mo.Waerme_Gas + mo.Waerme_Oel);
                if (mods != null && mods.Count > 0 && modSumme > 0)
                {
                    bool first = true;
                    foreach (var mo in mods)
                    {
                        t.Append(ErzeugerZeile(w, Name(mo.Modul, "Spitzenkessel"), F(mo.Waerme_Gas + mo.Waerme_Oel, 0), "–",
                                               first ? art : "–", first ? vTxt : "–"));
                        first = false;
                    }
                }
                else
                    t.Append(ErzeugerZeile(w, "Spitzenkessel", F(m.Heizkessel.Waermeproduktion, 0), "–", art, vTxt));
            }

            // Solarthermie – je Kollektorfeld
            if (m.Solarthermie != null)
            {
                var mods = m.Solarthermie.Module;
                if (mods != null && mods.Count > 0)
                    foreach (var mo in mods)
                        t.Append(ErzeugerZeile(w, Name(mo.Modul, "Solarthermie"), F(mo.Waermeproduktion, 0), "–", "–", "–"));
                else
                    t.Append(ErzeugerZeile(w, "Solarthermie", F(m.Solarthermie.Waermeproduktion, 0), "–", "–", "–"));
            }

            // Photovoltaik – je Feld
            if (m.Photovoltaik != null)
            {
                var mods = m.Photovoltaik.Module;
                if (mods != null && mods.Count > 0)
                    foreach (var mo in mods)
                        t.Append(ErzeugerZeile(w, Name(mo.Modul, "Photovoltaik"), "–", F(mo.Stromproduktion, 0), "–", "–"));
                else
                    t.Append(ErzeugerZeile(w, "Photovoltaik", "–", F(m.Photovoltaik.Stromproduktion, 0), "–", "–"));
            }

            return t;
        }

        // Modulname oder – falls leer – die generische Typbezeichnung.
        private static string Name(string modul, string fallback)
        {
            return string.IsNullOrWhiteSpace(modul) ? fallback : modul.Trim();
        }

        // Leerwert-Strich "–" wird zentriert, sonst die normale Ausrichtung.
        private static JustificationValues Just(string text, JustificationValues normal)
        {
            return text == "–" ? JustificationValues.Center : normal;
        }

        private TableRow ErzeugerZeile(int[] w, string name, string waerme, string strom, string brennstoff, string verbrauch)
        {
            return new TableRow(
                Cell(name, w[0], false, null, JustificationValues.Left),
                Cell(waerme, w[1], false, null, Just(waerme, JustificationValues.Right)),
                Cell(strom, w[2], false, null, Just(strom, JustificationValues.Right)),
                Cell(brennstoff, w[3], false, null, Just(brennstoff, JustificationValues.Left)),
                Cell(verbrauch, w[4], false, null, Just(verbrauch, JustificationValues.Right)));
        }

        // Dominanter (einziger) Brennstoff des BHKW samt Verbrauch (MWh/a); der mit Verbrauch > 0.
        private static void BHKWBrennstoff(ErgebnisBHKWModel b, out string art, out double verbrauch)
        {
            string[] arten = { "Gas", "Öl", "Pellets", "Holz", "Kohle", "Koks", "Rapsöl", "Tierische Fette", "Sonstige" };
            double[] werte = { b.Gasverbrauch, b.Oelverbrauch, b.Pellets, b.Holzverbrauch, b.Kohle,
                               b.Koks, b.Rapsoelverbrauch, b.TierischeFette, b.Sonstigverbrauch };
            art = "–"; verbrauch = 0;
            for (int i = 0; i < arten.Length; i++)
                if (werte[i] > verbrauch) { art = arten[i]; verbrauch = werte[i]; }
            if (verbrauch <= 0) { art = "–"; verbrauch = 0; }
        }

        // Dominanter (einziger) Brennstoff des Spitzenkessels; bei E-Kessel Energieträger "Strom" = Stromverbrauch.
        private static void HeizkesselBrennstoff(ErgebnisHeizkesselModel h, out string art, out double verbrauch)
        {
            string[] arten = { "Gas", "Öl", "Pellets", "Holz", "Kohle", "Koks", "Rapsöl", "Tierische Fette", "Sonstige" };
            double[] werte = { h.Gasverbrauch, h.Oelverbrauch, h.Pellets, h.Holzverbrauch, h.Kohle,
                               h.Koks, h.Rapsoelverbrauch, h.TierischeFette, h.Sonstigverbrauch };
            art = "–"; verbrauch = 0;
            for (int i = 0; i < arten.Length; i++)
                if (werte[i] > verbrauch) { art = arten[i]; verbrauch = werte[i]; }
            if (verbrauch <= 0 && h.Stromverbrauch > 0) { art = "Strom"; verbrauch = h.Stromverbrauch; }
            if (verbrauch <= 0) { art = "–"; verbrauch = 0; }
        }

        // Δ = Variante − Stamm, mit Vorzeichen; Dezimalstellen wie der Stammwert. "–" wenn nicht berechenbar.
        private static string Delta(string stammTxt, string varTxt)
        {
            double? a = ParseDE(stammTxt);
            double? b = ParseDE(varTxt);
            if (a == null || b == null) return "–";
            double d = b.Value - a.Value;
            int dec = Nachkommastellen(stammTxt);
            string betrag = Math.Abs(d).ToString("N" + dec, DE);
            if (d > 0) return "+" + betrag;
            if (d < 0) return "−" + betrag;
            return "±0" + (dec > 0 ? "," + new string('0', dec) : "");
        }

        private static double? ParseDE(string t)
        {
            if (string.IsNullOrWhiteSpace(t) || t == "–") return null;
            double v;
            return double.TryParse(t, System.Globalization.NumberStyles.Any, DE, out v) ? (double?)v : (double?)null;
        }

        private static int Nachkommastellen(string t)
        {
            int k = t.IndexOf(',');
            return k < 0 ? 0 : (t.Length - k - 1);
        }

        // 3-Spalten-Tabelle Erzeuger | Bezeichner | Menge (Brennstoffmengen je BHKW/Heizkessel).
        private Table BrennstoffmengenTabelle(int projektId)
        {
            int[] w = { 2600, 3826, 2600 };   // Erzeuger, Bezeichner, Menge
            Table t = NewTable(w);
            t.Append(new TableRow(
                Cell("Erzeuger", w[0], true, HEAD_FILL, JustificationValues.Left),
                Cell("Bezeichner", w[1], true, HEAD_FILL, JustificationValues.Left),
                Cell("Menge", w[2], true, HEAD_FILL, JustificationValues.Center)));

            DataTable dt = null;
            try { dt = EnergieMengen.BaueBrennstoffmengen(projektId); }
            catch { dt = null; }

            if (dt == null || dt.Rows.Count == 0)
            {
                t.Append(new TableRow(
                    Cell("–", w[0], false, null, JustificationValues.Center),
                    Cell("(keine Brennstoffdaten)", w[1], false, null, JustificationValues.Left),
                    Cell("–", w[2], false, null, JustificationValues.Center)));
                return t;
            }

            foreach (DataRow r in dt.Rows)
            {
                string erz = r["Erzeuger"] != DBNull.Value ? r["Erzeuger"].ToString() : "";
                string bez = r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : "";
                string menge = r["Menge"] != DBNull.Value ? r["Menge"].ToString() : "–";
                t.Append(new TableRow(
                    Cell(erz, w[0], false, null, JustificationValues.Left),
                    Cell(bez, w[1], false, null, JustificationValues.Left),
                    Cell(menge, w[2], false, null, Just(menge, JustificationValues.Right))));
            }
            return t;
        }

        // -------------------------------------------------------------- Daten

        // ID_Klimaregion -> Name (robust gegen ID- bzw. ID_Klimaregion-Spaltenname).
        private static Dictionary<int, string> LadeKlimaregionen()
        {
            var d = new Dictionary<int, string>();
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Klimaregion");
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                    {
                        int id = 0;
                        if (dt.Columns.Contains("ID_Klimaregion") && r["ID_Klimaregion"] != DBNull.Value) id = Convert.ToInt32(r["ID_Klimaregion"]);
                        else if (dt.Columns.Contains("ID") && r["ID"] != DBNull.Value) id = Convert.ToInt32(r["ID"]);
                        string name = (dt.Columns.Contains("Name") && r["Name"] != DBNull.Value) ? r["Name"].ToString() : "";
                        if (id > 0) d[id] = name;
                    }
            }
            catch { }
            return d;
        }

        // -------------------------------------------------------------- Bausteine

        private static string F(double v, int dec) { return v.ToString("N" + dec, DE); }

        private static Run RunT(string text, bool bold, string szHalf, string color)
        {
            RunProperties rp = new RunProperties();
            rp.Append(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
            if (bold) rp.Append(new Bold());
            if (color != null) rp.Append(new Color { Val = color });
            rp.Append(new FontSize { Val = szHalf });
            Run r = new Run();
            r.Append(rp);
            r.Append(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            return r;
        }

        private static Paragraph Par(string text, bool bold, string szHalf, string color,
                                     JustificationValues just, int before, int after)
        {
            Paragraph p = new Paragraph();
            ParagraphProperties pp = new ParagraphProperties();
            pp.Append(new SpacingBetweenLines { Before = before.ToString(), After = after.ToString() });
            pp.Append(new Justification { Val = just });
            p.Append(pp);
            p.Append(RunT(text, bold, szHalf, color));
            return p;
        }

        private static Paragraph Heading(string text) { return Par(text, true, "30", BLUE, JustificationValues.Left, 220, 100); }
        private static Paragraph Heading2(string text) { return Par(text, true, "24", BLUE, JustificationValues.Left, 160, 60); }

        private static Paragraph Trennlinie()
        {
            Paragraph p = new Paragraph();
            ParagraphProperties pp = new ParagraphProperties();
            pp.Append(new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 6U, Color = "BFBFBF", Space = 4U }));
            p.Append(pp);
            return p;
        }

        private static TableCell Cell(string text, int widthDxa, bool bold, string fill, JustificationValues just)
        {
            TableCell tc = new TableCell();
            TableCellProperties tcp = new TableCellProperties();
            tcp.Append(new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = widthDxa.ToString() });
            if (fill != null) tcp.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = fill });
            tcp.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            tc.Append(tcp);

            Paragraph p = new Paragraph();
            ParagraphProperties pp = new ParagraphProperties();
            pp.Append(new SpacingBetweenLines { Before = "20", After = "20" });
            pp.Append(new Justification { Val = just });
            p.Append(pp);
            p.Append(RunT(text, bold, "18", null));
            tc.Append(p);
            return tc;
        }

        private static Table NewTable(int[] widths)
        {
            Table t = new Table();
            TableProperties tp = new TableProperties();
            tp.Append(new TableWidth { Type = TableWidthUnitValues.Dxa, Width = widths.Sum().ToString() });
            tp.Append(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new RightBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = "BFBFBF" }));
            t.Append(tp);

            TableGrid grid = new TableGrid();
            foreach (int wv in widths) grid.Append(new GridColumn { Width = wv.ToString() });
            t.Append(grid);
            return t;
        }
    }
}