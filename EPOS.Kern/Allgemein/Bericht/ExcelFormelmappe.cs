using System;
using System.Globalization;
using ClosedXML.Excel;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E8b — die <b>Formelmappe</b> des Tabellenberichts (Konzept § 2.11.6, V‑G10;
    /// Analysepapier § 5 E8): Stufe für Stufe wird das Blatt „Wirtschaftlichkeit" aus
    /// Werten in nachvollziehbare Zellformeln umgebaut, ohne dass sich eine Zahl bewegt.
    ///
    /// <para><b>Stufe 0 — der Parameterblock aus echten Zellen.</b> Bis hierher standen die
    /// Annahmen des Laufs nur als Prosa in einer Zelle („i = 3,0 % · T = 20 a · …"); an
    /// Prosa lässt sich keine Formel verankern. Der Block trägt je Szenario einen Satz —
    /// Kalkulationszins, Betrachtungszeitraum, die drei Preissteigerungen (Energie,
    /// Betrieb, Investition/Ersatz) und die Änderungen an Investition, Erträgen und
    /// Nutzungsdauer — und gibt der Spalte „Erwartet" Namen (<see cref="ZINS"/>,
    /// <see cref="ZEITRAUM"/>, <see cref="PREIS_E"/>, <see cref="PREIS_B"/>,
    /// <see cref="PREIS_I"/>), auf die sich die Formeln der Mappe beziehen. Die Prosazeile
    /// darüber bleibt: Sie nennt Einspeisevergütung, Tarif und Bilanzierung, die keine
    /// Formel braucht.</para>
    ///
    /// <para><b>Die Grenze</b> steht im Block selbst (Konzept § 2.11.6, „Drei Sätze"): Die
    /// Mappe rechnet feststehende Jahresmengen nach, bildet die Gesetzeslogik nicht ab und
    /// schreibt nichts ins Projekt zurück.</para>
    /// </summary>
    internal static class ExcelFormelmappe
    {
        // ---- Namen der Spalte „Erwartet" (Arbeitsmappe, sprachneutral, ASCII) ----

        /// <summary>Kalkulationszins i als Dezimalzahl (0,03 = 3 %).</summary>
        internal const string ZINS = "Zins_i";

        /// <summary>Betrachtungszeitraum T [a].</summary>
        internal const string ZEITRAUM = "Zeitraum_T";

        /// <summary>Preissteigerung Energie p_E als Dezimalzahl.</summary>
        internal const string PREIS_E = "p_E";

        /// <summary>Preissteigerung Betrieb p_B als Dezimalzahl.</summary>
        internal const string PREIS_B = "p_B";

        /// <summary>Preissteigerung Investition/Ersatz p_I als Dezimalzahl (wirksam).</summary>
        internal const string PREIS_I = "p_I";

        /// <summary>Anhang der Namen für die Spalten der beiden anderen Szenarien.</summary>
        internal const string ANHANG_GUENSTIG = "_Guenstig";

        /// <summary>Anhang der Namen für die Spalte „Ungünstig".</summary>
        internal const string ANHANG_UNGUENSTIG = "_Unguenstig";

        /// <summary>Zeilen, um die der Parameterblock das Blatt unter der Prosazeile
        /// verschiebt: Kopf, acht Parameterzeilen, Hinweis, Grenze, Leerzeile.</summary>
        internal const int PARAMETERBLOCK_ZEILEN = 12;

        /// <summary>Zellformat der Sätze (Dezimalzahl, als Prozent gezeigt).</summary>
        internal const string FORMAT_SATZ = "0.00%";

        /// <summary>
        /// Stufe 0: schreibt den Parameterblock ab Zeile <paramref name="r"/> und legt die
        /// Namen an. Rückgabe: die erste Zeile unter dem Block.
        /// </summary>
        /// <param name="p">Der Parametersatz des Laufs (Erwartet); die Sätze der beiden
        /// anderen Szenarien entstehen daraus wie im Rechenlauf
        /// (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>).</param>
        internal static int Parameterblock(IXLWorksheet ws, int r, WirtschaftlichkeitParameter p)
        {
            SzenarioSatz best = p.SatzFuer(WirtschaftlichkeitSzenario.BEST)
                                ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
            SzenarioSatz worst = p.SatzFuer(WirtschaftlichkeitSzenario.WORST)
                                 ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_FM_PARAM_TITEL;
            ws.Cell(r, 2).Value = VerlaufZeilen.Szenarioname(WirtschaftlichkeitSzenario.ERWARTET);
            ws.Cell(r, 3).Value = VerlaufZeilen.Szenarioname(WirtschaftlichkeitSzenario.BEST);
            ws.Cell(r, 4).Value = VerlaufZeilen.Szenarioname(WirtschaftlichkeitSzenario.WORST);
            ws.Cell(r, 5).Value = MyResource.Resource.WIRT_FM_PARAM_NAME;
            ws.Range(r, 1, r, 5).Style.Font.Bold = true;
            ws.Range(r, 1, r, 5).Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
            r++;

            // Die Sätze als Dezimalzahl — derselbe Ausdruck „Prozent / 100", mit dem der
            // Rechenkern sie liest (KapitalwertRechner.Rechne); eine Formel auf diese Zelle
            // rechnet deshalb mit dem Bit, mit dem der Lauf gerechnet hat.
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_ZINS, ZINS, FORMAT_SATZ,
                      p.Zinssatz / 100.0,
                      best.ZinsWirksam(p.Zinssatz) / 100.0,
                      worst.ZinsWirksam(p.Zinssatz) / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_ZEITRAUM, ZEITRAUM, "0",
                      p.Betrachtungszeitraum, p.Betrachtungszeitraum, p.Betrachtungszeitraum);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_PREIS_E, PREIS_E, FORMAT_SATZ,
                      p.PreissteigerungEnergie / 100.0,
                      best.PreisEnergieWirksam(p.PreissteigerungEnergie) / 100.0,
                      worst.PreisEnergieWirksam(p.PreissteigerungEnergie) / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_PREIS_B, PREIS_B, FORMAT_SATZ,
                      p.PreissteigerungBetrieb / 100.0,
                      best.PreisBetriebWirksam(p.PreissteigerungBetrieb) / 100.0,
                      worst.PreisBetriebWirksam(p.PreissteigerungBetrieb) / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_PREIS_I, PREIS_I, FORMAT_SATZ,
                      p.PreisInvestWirksam / 100.0,
                      best.PreisInvestWirksam(p.PreisInvestWirksam) / 100.0,
                      worst.PreisInvestWirksam(p.PreisInvestWirksam) / 100.0);

            // Die Änderungen der beiden Szenarien — im Erwartungsfall 0 (er IST der
            // Projektsatz). Norm 9 c: Die Kalkulation je Szenario nennt ihre Einstellungen
            // vollständig (Konzept § 2.11.5, „Ausweis im Bericht").
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_INVEST, null, "+0%;-0%;0%",
                      0.0, best.InvestWirksam / 100.0, worst.InvestWirksam / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_ERTRAG, null, "+0%;-0%;0%",
                      0.0, best.ErtragWirksam / 100.0, worst.ErtragWirksam / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_DAUER, null, "+0.#;-0.#;0",
                      0.0, best.DauerWirksam, worst.DauerWirksam);

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_FM_PARAM_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_FM_GRENZE;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;
            return r + 1;   // Leerzeile unter dem Block
        }

        /// <summary>Eine Zeile des Parameterblocks: Bezeichnung, Erwartet, Günstig,
        /// Ungünstig — und, wenn <paramref name="name"/> gesetzt ist, die Namen der drei
        /// Zellen (Erwartet ohne Anhang).</summary>
        private static void Satzzeile(IXLWorksheet ws, int r, string titel, string name, string format,
                                      double erwartet, double guenstig, double unguenstig)
        {
            ws.Cell(r, 1).Value = titel;
            double[] werte = { erwartet, guenstig, unguenstig };
            for (int i = 0; i < 3; i++)
            {
                ws.Cell(r, 2 + i).Value = werte[i];
                ws.Cell(r, 2 + i).Style.NumberFormat.Format = format;
            }
            ws.Cell(r, 2).Style.Fill.BackgroundColor = ExcelBerichtGenerator.STAMM;
            if (string.IsNullOrEmpty(name)) return;

            ws.Cell(r, 5).Value = name;
            ws.Cell(r, 5).Style.Font.FontColor = XLColor.FromHtml("#696969");
            XLWorkbook wb = ws.Workbook;
            wb.DefinedNames.Add(name, ws.Range(r, 2, r, 2));
            wb.DefinedNames.Add(name + ANHANG_GUENSTIG, ws.Range(r, 3, r, 3));
            wb.DefinedNames.Add(name + ANHANG_UNGUENSTIG, ws.Range(r, 4, r, 4));
        }

        // =====================================================================
        //  Zellbezüge
        // =====================================================================

        /// <summary>Spaltenbuchstaben einer Spaltennummer (1 = A, 27 = AA).</summary>
        internal static string Spalte(int spalte)
        {
            string s = "";
            for (int n = spalte; n > 0; n = (n - 1) / 26)
                s = (char)('A' + (n - 1) % 26) + s;
            return s;
        }

        /// <summary>Ein A1-Bezug; <paramref name="zeileFest"/> setzt das „$" vor die Zeile.</summary>
        internal static string Bezug(int zeile, int spalte, bool zeileFest = false)
        {
            return Spalte(spalte) + (zeileFest ? "$" : "") + zeile.ToString(CultureInfo.InvariantCulture);
        }
    }
}
