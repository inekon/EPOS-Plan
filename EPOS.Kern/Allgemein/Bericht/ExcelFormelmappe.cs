using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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

        /// <summary>ETAPPE E15 — mit Zinszuschlag: der Kalkulationszins OHNE Risiko; der Name
        /// <see cref="ZINS"/> trägt dann die Summe Basis + Zuschlag.</summary>
        internal const string ZINS_BASIS = "Zins_Basis";

        /// <summary>ETAPPE E15 — der Risikozuschlag auf den Zins als Dezimalzahl.</summary>
        internal const string RISIKO_ZUSCHLAG = "Risiko_Zuschlag";

        /// <summary>ETAPPE E15 — die Rückflusseinbuße R_loss [€ je Periode].</summary>
        internal const string RISIKO_VERLUST = "Risiko_Verlust";

        /// <summary>ETAPPE E15 — die Eintrittswahrscheinlichkeit p_loss als Dezimalzahl.</summary>
        internal const string RISIKO_P = "Risiko_p";

        /// <summary>ETAPPE E15 — der Risikoabzug je Periode [€] = R_loss × p_loss.</summary>
        internal const string RISIKO_ABZUG = "Risiko_Abzug";

        /// <summary>Anhang der Namen für die Spalten der beiden anderen Szenarien.</summary>
        internal const string ANHANG_GUENSTIG = "_Guenstig";

        /// <summary>Anhang der Namen für die Spalte „Ungünstig".</summary>
        internal const string ANHANG_UNGUENSTIG = "_Unguenstig";

        /// <summary>Zeilen, um die der Parameterblock das Blatt unter der Prosazeile
        /// verschiebt: Kopf, elf Parameterzeilen, Hinweis, Grenze, Leerzeile.
        /// <para><b>ETAPPE E9a:</b> Zu den acht Zeilen aus E8b kommen Mengenänderung,
        /// Einspeisevergütung PV und Einspeisevergütung KWK je Szenario (Konzept § 2.11.5,
        /// „die Kalkulationstabelle je Szenario nennt die Parametereinstellungen
        /// vollständig"). Gepflegte Trägerpreise hängen je Stand, Träger und Preisart eine
        /// weitere Zeile an — ohne Pflege bleibt es bei dieser Zahl.</para></summary>
        internal const int PARAMETERBLOCK_ZEILEN = 15;

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
            return Parameterblock(ws, r, p, null);
        }

        /// <summary>
        /// ETAPPE E9a — derselbe Parameterblock mit den Größen der vollständigen
        /// Szenarioabdeckung: Betrachtungszeitraum je Szenario (Schritt B), Mengenänderung
        /// (Schritt B), Einspeisevergütung PV und KWK (Schritt D) und — nur wo gepflegt — die
        /// Trägerpreise der <paramref name="staende"/> (Schritt C), je Stand, Träger und
        /// Preisart eine Zeile mit den WIRKSAMEN Preisen, mit denen gerechnet wird.
        /// </summary>
        internal static int Parameterblock(IXLWorksheet ws, int r, WirtschaftlichkeitParameter p,
                                           IEnumerable<VariantenDaten> staende)
        {
            return Parameterblock(ws, r, p, staende, null);
        }

        /// <summary>
        /// ETAPPE E15 (V‑G7) — derselbe Parameterblock mit den <b>Risikozeilen</b>, nur wenn ein
        /// Risiko gepflegt ist (<see cref="RisikoModul.Gepflegt"/>): beim Zinszuschlag heißt die
        /// Zeile „Kalkulationszins" <see cref="ZINS_BASIS"/>, darunter stehen der Zuschlag
        /// (<see cref="RISIKO_ZUSCHLAG"/>) und der Zins mit Zuschlag als Formel — er trägt den
        /// Namen <see cref="ZINS"/>, auf den jede Barwertformel der Mappe zeigt. Beim Abzug
        /// folgen R_loss, p_loss und der Abzug je Periode als Formel
        /// (<see cref="RISIKO_ABZUG"/>). Ohne Risiko ist der Block Zelle für Zelle der von vorher.
        /// </summary>
        /// <param name="register">Das Formelregister der Mappe; <c>null</c> = die Risikoformeln
        /// werden ohne zwischengespeicherten Wert geschrieben (Excel rechnet beim Öffnen).</param>
        internal static int Parameterblock(IXLWorksheet ws, int r, WirtschaftlichkeitParameter p,
                                           IEnumerable<VariantenDaten> staende, Formelregister register)
        {
            return Parameterblock(ws, r, p, staende, register, v => Traegerpreissatz.Lies(v.IdProjekt));
        }

        /// <summary>
        /// ETAPPE BV-E3 (Konzept Berichtsvorlagen 5.1) — derselbe Parameterblock mit den gepflegten
        /// Trägerpreisen der Stände aus dem Wertesatz des Laufs
        /// (<see cref="WirtschaftsBerichtswerte.Traegerpreise"/>): Der Tabellenbericht schreibt ihn, ohne die
        /// Datenbank zu berühren. Die übrigen Überladungen lesen die Preise über denselben Weg
        /// (<see cref="Traegerpreissatz.Lies"/>) selbst.
        /// </summary>
        /// <param name="traegerpreise">Die Trägerpreise eines Stands; nur gefragt, wenn
        /// <paramref name="staende"/> gesetzt ist.</param>
        internal static int Parameterblock(IXLWorksheet ws, int r, WirtschaftlichkeitParameter p,
                                           IEnumerable<VariantenDaten> staende, Formelregister register,
                                           Func<VariantenDaten, IReadOnlyList<Traegerpreissatz>> traegerpreise)
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
            bool zinsRisiko = RisikoModul.ZinsAktiv(p);
            int zeileZins = r;
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_ZINS, zinsRisiko ? ZINS_BASIS : ZINS, FORMAT_SATZ,
                      p.Zinssatz / 100.0,
                      best.ZinsWirksam(p.Zinssatz) / 100.0,
                      worst.ZinsWirksam(p.Zinssatz) / 100.0);
            if (zinsRisiko)
            {
                // ETAPPE E15 (V‑G7, 6.5): Zuschlag und Zins mit Zuschlag — in allen drei
                // Szenarien derselbe Zuschlag (E15‑Q1 a). Die Summe trägt den Namen Zins_i, damit
                // jede Barwertformel der Mappe unverändert auf den gerechneten Zins zeigt; ihre
                // Werte sind die Zinssätze, mit denen der Lauf gerechnet hat (FuerSzenario).
                double zuschlag = RisikoModul.Zinszuschlag(p) / 100.0;
                int zeileZuschlag = r;
                Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_RISIKO_ZUSCHLAG, RISIKO_ZUSCHLAG, FORMAT_SATZ,
                          zuschlag, zuschlag, zuschlag);
                Satzzeile(ws, r, MyResource.Resource.WIRT_FM_PARAM_ZINS_RISIKO, ZINS, FORMAT_SATZ,
                          p.FuerSzenario(WirtschaftlichkeitSzenario.ERWARTET).Zinssatz / 100.0,
                          p.FuerSzenario(WirtschaftlichkeitSzenario.BEST).Zinssatz / 100.0,
                          p.FuerSzenario(WirtschaftlichkeitSzenario.WORST).Zinssatz / 100.0);
                for (int c = 2; c <= 4; c++)
                    Parameterformel(ws.Cell(r, c), Bezug(zeileZins, c) + "+" + Bezug(zeileZuschlag, c),
                                    ws.Cell(zeileZins, c).GetDouble() + ws.Cell(zeileZuschlag, c).GetDouble(),
                                    register);
                r++;
            }
            // ETAPPE E9a (Schritt B): der Zeitraum JE SZENARIO — ohne Pflege dreimal T.
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_ZEITRAUM, ZEITRAUM, "0",
                      p.Betrachtungszeitraum,
                      best.ZeitraumWirksam(p.Betrachtungszeitraum),
                      worst.ZeitraumWirksam(p.Betrachtungszeitraum));
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

            // ETAPPE E9a (Schritte B und D): Mengenänderung und Erlössätze je Szenario —
            // ohne Pflege die Erwartet-Werte (Menge 0 %).
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_MENGE, null, "+0.0%;-0.0%;0.0%",
                      0.0, best.MengeWirksam / 100.0, worst.MengeWirksam / 100.0);
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_VERGUETUNG, null, "0.0000",
                      p.Einspeiseverguetung,
                      best.EinspeiseverguetungWirksam(p.Einspeiseverguetung),
                      worst.EinspeiseverguetungWirksam(p.Einspeiseverguetung));
            Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_VERGUETUNG_KWK, null, "0.0000",
                      p.EinspeiseverguetungKWK ?? 0.0,
                      best.EinspeiseverguetungKwkWirksam(p.EinspeiseverguetungKWK) ?? 0.0,
                      worst.EinspeiseverguetungKwkWirksam(p.EinspeiseverguetungKWK) ?? 0.0);

            // ETAPPE E15 (V‑G7, Anhang F): der Zahlungsstromabzug — R_loss, p_loss und der Abzug
            // je Periode als Formel; in allen drei Szenarien gleich (E15‑Q1 a).
            if (RisikoModul.AbzugAktiv(p))
            {
                double verlust = p.RisikoVerlust.Value;
                double wahrsch = RisikoModul.Wahrscheinlichkeit(p) / 100.0;
                int zeileVerlust = r;
                Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_RISIKO_VERLUST, RISIKO_VERLUST, "#,##0",
                          verlust, verlust, verlust);
                int zeileP = r;
                Satzzeile(ws, r++, MyResource.Resource.WIRT_FM_PARAM_RISIKO_P, RISIKO_P, FORMAT_SATZ,
                          wahrsch, wahrsch, wahrsch);
                double abzug = RisikoModul.AbzugJeJahr(p);
                Satzzeile(ws, r, MyResource.Resource.WIRT_FM_PARAM_RISIKO_ABZUG, RISIKO_ABZUG, "#,##0.00",
                          abzug, abzug, abzug);
                for (int c = 2; c <= 4; c++)
                    Parameterformel(ws.Cell(r, c), Bezug(zeileVerlust, c) + "*" + Bezug(zeileP, c),
                                    verlust * wahrsch, register);
                r++;
            }

            // ETAPPE E9a (Schritt C): gepflegte Trägerpreise — je Stand, Träger und Preisart
            // eine Zeile mit den wirksamen Preisen der drei Szenarien.
            if (staende != null)
                foreach (VariantenDaten v in staende)
                    if (v != null) r = Traegerpreiszeilen(ws, r, v, traegerpreise(v));

            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_FM_PARAM_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_FM_GRENZE;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r++;
            return r + 1;   // Leerzeile unter dem Block
        }

        /// <summary>
        /// ETAPPE E9a (Schritt C) — die Zeilen der gepflegten Trägerpreise EINES Standes:
        /// für jeden Träger mit Szenariopreis und jede Preisart (Arbeit, Grund, Leistung), in
        /// der Günstig oder Ungünstig gepflegt ist, eine Zeile mit den wirksamen Preisen
        /// Erwartet / Günstig / Ungünstig — dieselben, mit denen die Energiekosten rechnen
        /// (<see cref="KostenEmissionRechner.PreisSatz"/>). Rückgabe: die nächste Zeile.
        ///
        /// <para><b>BV-E3:</b> Die Leseschritte (Träger mit Szenariopreis, Name, wirksame Preise) stehen in
        /// <see cref="Traegerpreissatz.Lies"/>; hier wird nur noch geschrieben.</para>
        /// </summary>
        private static int Traegerpreiszeilen(IXLWorksheet ws, int r, VariantenDaten v,
                                              IReadOnlyList<Traegerpreissatz> saetze)
        {
            string stand = v.IstStamm ? "Stamm" : v.Anzeige;
            foreach (Traegerpreissatz s in saetze ?? Array.Empty<Traegerpreissatz>())
            {
                string traeger = s.Name;
                if (s.Szenario.ArbeitspreisBest.HasValue || s.Szenario.ArbeitspreisWorst.HasValue)
                    Satzzeile(ws, r++, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_PARAM_TP_ARBEIT,
                                                     traeger, stand), null, "0.0000",
                              s.ArbeitErwartet ?? 0.0, s.ArbeitGuenstig ?? 0.0, s.ArbeitUnguenstig ?? 0.0);
                if (s.Szenario.GrundpreisBest.HasValue || s.Szenario.GrundpreisWorst.HasValue)
                    Satzzeile(ws, r++, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_PARAM_TP_GRUND,
                                                     traeger, stand), null, "#,##0.00",
                              s.GrundErwartet ?? 0.0, s.GrundGuenstig ?? 0.0, s.GrundUnguenstig ?? 0.0);
                if (s.Szenario.LeistungspreisBest.HasValue || s.Szenario.LeistungspreisWorst.HasValue)
                    Satzzeile(ws, r++, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_PARAM_TP_LEISTUNG,
                                                     traeger, stand), null, "#,##0.00",
                              s.LeistungErwartet ?? 0.0, s.LeistungGuenstig ?? 0.0, s.LeistungUnguenstig ?? 0.0);
            }
            return r;
        }

        /// <summary>
        /// ETAPPE E15 — eine Formel im Parameterblock: über das Register, wenn es eines gibt
        /// (dann trägt die Zelle Formel UND den Wert, der dort stand); sonst bleibt der Wert
        /// stehen, und die Formel kommt dazu.
        /// </summary>
        private static void Parameterformel(IXLCell zelle, string formel, double nachgerechnet,
                                            Formelregister register)
        {
            double wert = zelle.GetDouble();
            if (register != null) register.Formel(zelle, formel, wert, nachgerechnet);
            else zelle.FormulaA1 = formel;
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
        //  Stufe 1 — die Mehrjahrestabelle in Formeln
        // =====================================================================

        /// <summary>Überschrift der Hilfsspalte „Basis des Betriebs-Topfes".</summary>
        private static string KopfBasisPB { get { return MyResource.Resource.WIRT_FM_MJ_BASIS_PB; } }

        /// <summary>Überschrift der Hilfsspalte „Basis des Endenergie-Topfes".</summary>
        private static string KopfBasisPE { get { return MyResource.Resource.WIRT_FM_MJ_BASIS_PE; } }

        /// <summary>ETAPPE E16: Überschrift der Hilfsspalte „Positionen alle n Jahre" im
        /// Betriebs-Topf (p_B).</summary>
        private static string KopfWiederholtPB { get { return MyResource.Resource.WIRT_FM_MJ_WDH_PB; } }

        /// <summary>ETAPPE E16: dieselbe Hilfsspalte im Endenergie-Topf (p_E).</summary>
        private static string KopfWiederholtPE { get { return MyResource.Resource.WIRT_FM_MJ_WDH_PE; } }

        /// <summary>
        /// ETAPPE E16 (V‑G3): der Betrag der Positionen „alle n Jahre", die im Jahr
        /// <paramref name="t"/> zahlen [€/a], Preisstand Jahr 1 — dieselbe Regel wie der
        /// Rechenkern (<see cref="KapitalwertRechner.ZahltImJahr"/>) und in derselben
        /// Reihenfolge summiert.
        /// </summary>
        internal static double WiederholtImJahr(IList<KapitalwertRechner.Wiederholposten> liste, int t)
        {
            double s = 0;
            if (liste != null)
                foreach (KapitalwertRechner.Wiederholposten w in liste)
                    if (w != null && KapitalwertRechner.ZahltImJahr(w.StartJahr, w.Periode, t)) s += w.Betrag;
            return s;
        }

        /// <summary>
        /// ETAPPE E16 (V‑G3): die Formel einer Zelle „Positionen alle n Jahre" — je Position
        /// die Schutzformel <c>IF(AND(Jahr&gt;=s,MOD(Jahr-s,n)=0),Betrag,0)</c>, summiert. Sie
        /// rechnet dieselben Zahlungsjahre wie <see cref="KapitalwertRechner.ZahltImJahr"/>.
        /// </summary>
        /// <param name="jahr">Der Bezug auf die Jahreszelle der Zeile.</param>
        internal static string WiederholFormel(IList<KapitalwertRechner.Wiederholposten> liste, string jahr)
        {
            var teile = new List<string>();
            foreach (KapitalwertRechner.Wiederholposten w in liste)
            {
                if (w == null) continue;
                string s = (w.StartJahr > 1 ? w.StartJahr : 1).ToString(CultureInfo.InvariantCulture);
                string n = Math.Max(1, w.Periode).ToString(CultureInfo.InvariantCulture);
                teile.Add("IF(AND(" + jahr + ">=" + s + ",MOD(" + jahr + "-" + s + "," + n + ")=0)," +
                          w.Betrag.ToString("R", CultureInfo.InvariantCulture) + ",0)");
            }
            return teile.Count == 0 ? "0" : string.Join("+", teile);
        }

        /// <summary>
        /// Stufe 1 (Konzept § 2.11.6): legt die Formeln über die fertig geschriebene
        /// Mehrjahrestabelle EINES Projekts. Die Zellen tragen danach die Formel und — über
        /// das <paramref name="register"/> — als zwischengespeichertes Ergebnis genau die
        /// Zahl, die sie vorher als Wert trugen.
        ///
        /// <list type="bullet">
        ///   <item><description><b>Energie</b> (und die <b>CO₂-Abgabe</b> nur im
        ///     Rückfallzweig): ab dem Jahr 2 Jahr 1 × (1+p_E)^(t−1); mit jahresscharfer
        ///     CO₂-Reihe bleibt sie zugelieferter Preispfad (Wert).</description></item>
        ///   <item><description><b>Betrieb</b> als zwei Terme — Betriebs-Topf mit p_B,
        ///     Endenergie-Topf mit p_E — über Hilfsspalten rechts der Tabelle, die die Basis
        ///     je Jahr tragen (Preisstand Jahr 1, samt Stufen der Positionen mit späterem
        ///     Startjahr).</description></item>
        ///   <item><description><b>Netto</b> als Zeilensumme der Positionsspalten,
        ///     <b>Barwert</b> als Netto × (1+i)^−t, <b>Kumuliert</b> als Laufsumme.</description></item>
        ///   <item><description>Die <b>Abschlusszeile</b> trägt in der Nettospalte den
        ///     nominalen Restwert (neu), im Barwert dessen Abzinsung und in Kumuliert den
        ///     Nettobarwert.</description></item>
        /// </list>
        ///
        /// <para>Investition und Ersatz, Einspeisung und die gesetzlichen Erlösreihen
        /// (KWK-Zuschlag, Steuergutschriften, PV-Vergütung, Pauschale) bleiben Werte: Sie
        /// hängen an Nutzungsdauern, Kontingenten und Rechtsständen, nicht an einer
        /// Fortschreibung.</para>
        ///
        /// <para><b>Jede Formel wird gegengerechnet</b>, bevor sie die Zelle ersetzt: Weicht
        /// ihr Ergebnis (in C# nachgerechnet) vom Wert ab, bleibt der Wert stehen
        /// (<see cref="Formelregister.Abweichungen"/>).</para>
        /// </summary>
        /// <param name="kopfZeile">Die Zeile der Spaltenköpfe (Jahr 0 steht darunter).</param>
        /// <param name="ps">Der Parametersatz DES SZENARIOS — derselbe, mit dem der Lauf die
        /// Tabelle gerechnet hat (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>; für
        /// „Erwartet" der Projektsatz selbst) und dessen Spalte der Parameterblock trägt.</param>
        /// <param name="szenario">Das Szenario der Tabelle; es wählt die Namen der Formeln
        /// (<see cref="Name"/>: Erwartet ohne Anhang, Günstig/Ungünstig mit).</param>
        /// <param name="jahreTabelle">ETAPPE E14 (E14‑Q1 a): die Zahl der Jahreszeilen — der
        /// längste Betrachtungszeitraum der drei Szenarien. Jahre jenseits von T_s tragen in
        /// den Summenspalten nur die Schutzformel <c>IF(Jahr&lt;=T_s;…;"")</c>.</param>
        /// <returns>Die Lage der Tabelle; <c>null</c>, wenn es kein Zahlungsbild gibt.</returns>
        internal static MehrjahresTafel Mehrjahrestabelle(IXLWorksheet ws, int kopfZeile,
            int idProjekt, Mehrjahresbild bild, VerlaufSerie serie, WirtschaftlichkeitParameter ps,
            string szenario, int jahreTabelle, Formelregister register)
        {
            KapitalwertRechner.Zahlungsbild zb = serie != null ? serie.Bild : null;
            if (ws == null || bild == null || zb == null || ps == null || register == null) return null;

            int zeilenJahre = Math.Max(bild.Jahre, jahreTabelle);
            var tafel = new MehrjahresTafel
            {
                IdProjekt = idProjekt,
                Szenario = szenario ?? WirtschaftlichkeitSzenario.ERWARTET,
                KopfZeile = kopfZeile,
                Jahr0Zeile = kopfZeile + 1,
                Jahre = bild.Jahre,
                JahreTabelle = zeilenJahre,
                AbschlussZeile = kopfZeile + 1 + zeilenJahre + 1,
                Tabelle = bild,
                Bild = zb,
                SpalteNetto = SpalteVon(bild, "NETTO"),
                SpalteBarwert = SpalteVon(bild, "BARWERT"),
                SpalteKumuliert = SpalteVon(bild, "KUMULIERT"),
                FreieSpalte = 2 + bild.Spalten.Count
            };
            int T = tafel.Jahre;
            if (tafel.SpalteNetto < 0 || tafel.SpalteBarwert < 0 || tafel.SpalteKumuliert < 0 || T < 1)
                return tafel;

            double i = ps.Zinssatz / 100.0;
            double pE = ps.PreissteigerungEnergie / 100.0;
            double pB = ps.PreissteigerungBetrieb / 100.0;

            // ETAPPE E14: die Namen DES SZENARIOS — Erwartet „Zins_i", Günstig „Zins_i_Guenstig" …
            string zins = Name(ZINS, tafel.Szenario);
            string nameE = Name(PREIS_E, tafel.Szenario);
            string nameB = Name(PREIS_B, tafel.Szenario);
            string zeitraum = Name(ZEITRAUM, tafel.Szenario);

            // ---- ETAPPE E15 (V‑G7, Anhang F): der Risikoabzug als Bezug auf den Parameterblock ----
            int cRisiko = SpalteVon(bild, Mehrjahresbild.RISIKO);
            if (cRisiko > 0 && zb.RisikoJeJahr != null)
            {
                MehrjahresSpalte risiko = bild.Spalten[cRisiko - 2];
                for (int t = 1; t <= T; t++)
                    register.Formel(ws.Cell(tafel.Zeile(t), cRisiko), "-" + Name(RISIKO_ABZUG, tafel.Szenario),
                                    risiko.Wert(t), -RisikoModul.AbzugJeJahr(ps));
            }

            // ---- Energie und CO₂-Abgabe (Rückfallzweig): Fortschreibung ab Jahr 2 ----
            Fortschreibung(ws, tafel, bild, "ENERGIE", nameE, pE, register);
            if (zb.BehgFortgeschrieben) Fortschreibung(ws, tafel, bild, "BEHG", nameE, pE, register);

            // ---- Betrieb als zwei Terme über Hilfsspalten ----
            int cBetrieb = SpalteVon(bild, "BETRIEB");
            if (cBetrieb > 0 && zb.BetriebBasisJeJahr != null && zb.EndenergieBasisJeJahr != null &&
                zb.BetriebBasisJeJahr.Length > T && zb.EndenergieBasisJeJahr.Length > T)
            {
                bool mitEndenergie = false;
                for (int t = 1; t <= T; t++) if (zb.EndenergieBasisJeJahr[t] != 0) mitEndenergie = true;

                // ETAPPE E16 (V‑G3, DIN EN 17463 6.3.1): die Positionen „alle n Jahre" je Topf.
                // Sie bekommen je Topf eine EIGENE Hilfsspalte, deren Zelle die Periode als
                // Formel trägt — je Position IF(AND(Jahr>=s,MOD(Jahr-s,n)=0),Betrag,0) —, und die
                // Basisspalte des Topfes trägt nur noch den jährlichen Rest. Ohne solche
                // Positionen entsteht keine Spalte, und jede Zelle ist die von vorher.
                var wdhB = new List<KapitalwertRechner.Wiederholposten>();
                var wdhE = new List<KapitalwertRechner.Wiederholposten>();
                if (zb.Wiederholt != null)
                    foreach (KapitalwertRechner.Wiederholposten w in zb.Wiederholt)
                        if (w != null) (w.Endenergie ? wdhE : wdhB).Add(w);
                if (wdhE.Count > 0) mitEndenergie = true;

                int hB = tafel.FreieSpalte++;
                int hWB = wdhB.Count > 0 ? tafel.FreieSpalte++ : -1;
                int hE = mitEndenergie ? tafel.FreieSpalte++ : -1;
                int hWE = wdhE.Count > 0 ? tafel.FreieSpalte++ : -1;
                Kopf(ws, kopfZeile, hB, KopfBasisPB);
                if (hWB > 0) Kopf(ws, kopfZeile, hWB, KopfWiederholtPB);
                if (hE > 0) Kopf(ws, kopfZeile, hE, KopfBasisPE);
                if (hWE > 0) Kopf(ws, kopfZeile, hWE, KopfWiederholtPE);
                MehrjahresSpalte betrieb = bild.Spalten[cBetrieb - 2];

                for (int t = 1; t <= T; t++)
                {
                    int r = tafel.Zeile(t);
                    string jahr = Bezug(r, 1);

                    // Die Basis je Topf: ohne Positionen „alle n Jahre" die Zahl von vorher,
                    // mit ihnen der jährliche Rest (Basis − Wiederholanteil des Jahres).
                    double wB = WiederholtImJahr(wdhB, t);
                    double wE = WiederholtImJahr(wdhE, t);
                    Zahl(ws.Cell(r, hB), hWB > 0 ? zb.BetriebBasisJeJahr[t] - wB : zb.BetriebBasisJeJahr[t]);
                    if (hE > 0) Zahl(ws.Cell(r, hE), hWE > 0 ? zb.EndenergieBasisJeJahr[t] - wE : zb.EndenergieBasisJeJahr[t]);
                    if (hWB > 0)
                    {
                        Zahl(ws.Cell(r, hWB), wB);
                        register.Formel(ws.Cell(r, hWB), WiederholFormel(wdhB, jahr), wB, wB);
                    }
                    if (hWE > 0)
                    {
                        Zahl(ws.Cell(r, hWE), wE);
                        register.Formel(ws.Cell(r, hWE), WiederholFormel(wdhE, jahr), wE, wE);
                    }

                    string basisB = hWB > 0 ? "(" + Bezug(r, hB) + "+" + Bezug(r, hWB) + ")" : Bezug(r, hB);
                    string basisE = hE > 0
                        ? (hWE > 0 ? "(" + Bezug(r, hE) + "+" + Bezug(r, hWE) + ")" : Bezug(r, hE))
                        : "";
                    string formel = hE > 0
                        ? "-(" + basisB + "*(1+" + nameB + ")^(" + jahr + "-1)+" +
                          basisE + "*(1+" + nameE + ")^(" + jahr + "-1))"
                        : "-" + basisB + "*(1+" + nameB + ")^(" + jahr + "-1)";
                    double nach = -(zb.BetriebBasisJeJahr[t] * Math.Pow(1.0 + pB, t - 1) +
                                    (hE > 0 ? zb.EndenergieBasisJeJahr[t] * Math.Pow(1.0 + pE, t - 1) : 0.0));
                    register.Formel(ws.Cell(r, cBetrieb), formel, betrieb.Wert(t), nach);
                }
            }

            // ---- Netto (Zeilensumme), Barwert, Kumuliert ----
            MehrjahresSpalte netto = bild.Spalten[tafel.SpalteNetto - 2];
            MehrjahresSpalte barwert = bild.Spalten[tafel.SpalteBarwert - 2];
            MehrjahresSpalte kumuliert = bild.Spalten[tafel.SpalteKumuliert - 2];
            int letztePosition = tafel.SpalteNetto - 1;
            double kum = 0;
            for (int t = 0; t <= T; t++)
            {
                int r = tafel.Zeile(t);

                double nettoNach = 0;
                for (int c = 2; c <= letztePosition; c++) nettoNach += bild.Spalten[c - 2].Wert(t);
                if (letztePosition >= 2)
                    register.Formel(ws.Cell(r, tafel.SpalteNetto),
                        "SUM(" + Bezug(r, 2) + ":" + Bezug(r, letztePosition) + ")", netto.Wert(t), nettoNach);

                double barwertNach = netto.Wert(t) * Math.Pow(1.0 + i, -t);
                register.Formel(ws.Cell(r, tafel.SpalteBarwert),
                    Bezug(r, tafel.SpalteNetto) + "*(1+" + zins + ")^(-" + Bezug(r, 1) + ")",
                    barwert.Wert(t), barwertNach);

                kum = t == 0 ? barwert.Wert(0) : kum + barwert.Wert(t);
                register.Formel(ws.Cell(r, tafel.SpalteKumuliert),
                    t == 0 ? Bezug(r, tafel.SpalteBarwert)
                           : Bezug(r - 1, tafel.SpalteKumuliert) + "+" + Bezug(r, tafel.SpalteBarwert),
                    kumuliert.Wert(t), kum);
            }

            // ---- ETAPPE E14 (E14‑Q1 a): Jahre jenseits von T_s — die Schutzformel ----
            // Die Tabelle läuft bis zum längsten Zeitraum der drei Szenarien; ein Szenario mit
            // kürzerem T_s trägt dort keine Zahlung. Die Summenspalten bekommen dieselbe Formel
            // wie in den Jahren davor, eingefasst in IF(Jahr<=T_s;…;"") — ihr Ergebnis ist leer.
            for (int t = T + 1; t <= tafel.JahreTabelle; t++)
            {
                int r = tafel.Zeile(t);
                string bedingung = Bezug(r, 1) + "<=" + zeitraum;
                if (letztePosition >= 2)
                    register.FormelText(ws.Cell(r, tafel.SpalteNetto),
                        Schutz(bedingung, "SUM(" + Bezug(r, 2) + ":" + Bezug(r, letztePosition) + ")"), "");
                register.FormelText(ws.Cell(r, tafel.SpalteBarwert),
                    Schutz(bedingung, Bezug(r, tafel.SpalteNetto) + "*(1+" + zins + ")^(-" + Bezug(r, 1) + ")"), "");
                register.FormelText(ws.Cell(r, tafel.SpalteKumuliert),
                    Schutz(bedingung, Bezug(r - 1, tafel.SpalteKumuliert) + "+" + Bezug(r, tafel.SpalteBarwert)), "");
            }

            // ---- Abschlusszeile: nominaler Restwert, sein Barwert, der Nettobarwert ----
            // Der Restwert steht am Ende von T_s (E9a‑Q4 a) — abgezinst über das Jahr T_s.
            int rA = tafel.AbschlussZeile;
            Zahl(ws.Cell(rA, tafel.SpalteNetto), zb.RestwertNominal);
            ws.Cell(rA, tafel.SpalteNetto).Style.Font.Bold = true;
            ws.Cell(rA, tafel.SpalteNetto).Style.Fill.BackgroundColor = ExcelBerichtGenerator.STAMM;
            register.Formel(ws.Cell(rA, tafel.SpalteBarwert),
                Bezug(rA, tafel.SpalteNetto) + "*(1+" + zins + ")^(-" + Bezug(tafel.Zeile(T), 1) + ")",
                bild.RestwertBarwert, zb.RestwertNominal * Math.Pow(1.0 + i, -T));
            register.Formel(ws.Cell(rA, tafel.SpalteKumuliert),
                Bezug(tafel.Zeile(T), tafel.SpalteKumuliert) + "+" + Bezug(rA, tafel.SpalteBarwert),
                bild.Kapitalwert, kumuliert.Wert(T) + bild.RestwertBarwert);
            return tafel;
        }

        // =====================================================================
        //  Stufe 2 — die Kennzahlen je Szenario
        // =====================================================================

        /// <summary>
        /// Stufe 2 (Konzept § 2.11.6): die Kennzahlen EINES Szenarios in Formeln — auf die
        /// Mehrjahrestabellen desselben Szenarios und mit den Namen seiner Spalte im
        /// Parameterblock (ETAPPE E14: für Erwartet, Günstig und Ungünstig; E14‑Q2 a ersetzt
        /// E8b‑Q1 a „Werte").
        ///
        /// <list type="bullet">
        ///   <item><description><b>Nettobarwert</b> je Stand über NBW (Excel: <c>NPV</c>) auf
        ///     die Nettospalte seiner Tabelle, plus Jahr 0 und Restwert-Barwert.</description></item>
        ///   <item><description><b>Kapitalwertdifferenz</b> als Zellbezug Nettobarwert −
        ///     Nettobarwert der Referenz; <b>Annuität</b> über RMZ (<c>PMT</c>).</description></item>
        ///   <item><description><b>Differenzreihe Variante − Referenz</b> — neu je Tabelle
        ///     einer Variante, rechts der Tabelle: nominal (im Jahr T samt
        ///     Restwert-Nominaldifferenz), als Barwert ohne Restwert, kumuliert, dazu eine
        ///     Hilfsspalte mit dem Nulldurchgang je Jahr.</description></item>
        ///   <item><description><b>Interner Zinsfuß</b> über IKV (<c>IRR</c>) auf die
        ///     nominale Differenzreihe, <b>dynamische Amortisation</b> über die Hilfsspalte;
        ///     ohne Vorzeichenwechsel bzw. ohne Nulldurchgang ein benannter Leerwert als Text
        ///     (dieselben Sätze wie auf der Seite), kein Zellfehler.</description></item>
        /// </list>
        ///
        /// <para><b>Nur wo die Formel dieselbe Zahl liefert:</b> Jede Kennzahl wird in C#
        /// gegengerechnet (dieselben Rechenkern-Methoden wie der Lauf: Differenzreihe,
        /// Vorzeichenzähler, Zinsfuß, Amortisation). Ein mehrdeutiger Zinsfuß (mehr als ein
        /// Vorzeichenwechsel) trägt als Ergebnis den Text „nicht eindeutig" (E14‑Q3 a) — die
        /// Zinsfußgleichung hat dann mehrere Lösungen, und welche Excel fände, ist nicht die,
        /// die der Bericht nennt; die Zahl der Wechsel zählt die Hilfsspalte der
        /// Differenzreihe.</para>
        /// </summary>
        /// <param name="ps">Der Parametersatz des Szenarios (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>):
        /// i und T_s, mit denen der Lauf Kapitalwert und Annuität gerechnet hat.</param>
        internal static void Kennzahlen(IXLWorksheet ws, KennzahlBlock block, List<MehrjahresTafel> tafeln,
            WirtschaftlichkeitVerlauf verlauf, List<WirtschaftlichkeitErgebnis> alle,
            WirtschaftlichkeitParameter ps, string szenario, Formelregister register)
        {
            if (ws == null || block == null || tafeln == null || verlauf == null || alle == null ||
                ps == null || register == null) return;
            szenario = szenario ?? WirtschaftlichkeitSzenario.ERWARTET;
            double i = ps.Zinssatz / 100.0;
            int T = ps.Betrachtungszeitraum;
            string zins = Name(ZINS, szenario);
            string zeitraum = Name(ZEITRAUM, szenario);

            // ---- Nettobarwert je Stand über NPV ----
            bool mitNbw = block.Zeilen.TryGetValue("NETTOBARWERT", out int zNbw);
            if (mitNbw)
                foreach (MehrjahresTafel tafel in tafeln)
                {
                    int c;
                    WirtschaftlichkeitErgebnis e = Ergebnis(alle, tafel.IdProjekt, szenario);
                    if (!tafel.Vollstaendig || tafel.Jahre != T || e == null || !e.Kapitalwert.HasValue ||
                        !block.Spalten.TryGetValue(tafel.IdProjekt, out c)) continue;

                    MehrjahresSpalte netto = tafel.Tabelle.Spalten[tafel.SpalteNetto - 2];
                    double nach = netto.Wert(0) + tafel.Tabelle.RestwertBarwert;
                    double npv = 0;
                    for (int t = 1; t <= T; t++) npv += netto.Wert(t) / Math.Pow(1.0 + i, t);
                    nach += npv;
                    string formel = "NPV(" + zins + "," + Bezug(tafel.Zeile(1), tafel.SpalteNetto) + ":" +
                                    Bezug(tafel.Zeile(T), tafel.SpalteNetto) + ")+" +
                                    Bezug(tafel.Zeile(0), tafel.SpalteNetto) + "+" +
                                    Bezug(tafel.AbschlussZeile, tafel.SpalteBarwert);
                    register.Formel(ws.Cell(zNbw, c), formel, e.Kapitalwert.Value, nach);
                }

            // ---- Die Referenz: ihre Tabelle und ihre Spalte im Block ----
            int idRef = verlauf.IdReferenz;
            MehrjahresTafel refTafel = tafeln.FirstOrDefault(x => x.IdProjekt == idRef);
            WirtschaftlichkeitErgebnis refErg = Ergebnis(alle, idRef, szenario);
            int cRef;
            bool refImBlock = block.Spalten.TryGetValue(idRef, out cRef);
            string refName = RefName(verlauf);

            foreach (MehrjahresTafel tafel in tafeln)
            {
                if (tafel.IdProjekt == idRef) continue;
                WirtschaftlichkeitErgebnis e = Ergebnis(alle, tafel.IdProjekt, szenario);
                int c;
                bool imBlock = block.Spalten.TryGetValue(tafel.IdProjekt, out c);

                // ---- Kapitalwertdifferenz als Zellbezug, Annuität über PMT ----
                int zDiff, zAnn;
                bool diffFormel = false;
                if (mitNbw && imBlock && refImBlock && e != null && refErg != null && e.KapitalwertDiff.HasValue &&
                    e.Kapitalwert.HasValue && refErg.Kapitalwert.HasValue &&
                    block.Zeilen.TryGetValue("KAPITALWERT_DIFF", out zDiff))
                    diffFormel = register.Formel(ws.Cell(zDiff, c),
                        Bezug(zNbw, c) + "-" + Bezug(zNbw, cRef),
                        e.KapitalwertDiff.Value, e.Kapitalwert.Value - refErg.Kapitalwert.Value);
                if (diffFormel && e.AnnuitaetKW.HasValue && block.Zeilen.TryGetValue("ANNUITAET", out zAnn) &&
                    block.Zeilen.TryGetValue("KAPITALWERT_DIFF", out zDiff))
                    register.Formel(ws.Cell(zAnn, c),
                        "PMT(" + zins + "," + zeitraum + ",-" + Bezug(zDiff, c) + ")",
                        e.AnnuitaetKW.Value, e.KapitalwertDiff.Value * KapitalwertRechner.Annuitaet(i, T));

                // ---- Die Differenzreihe rechts der Tabelle ----
                if (refTafel == null || !tafel.Vollstaendig || !refTafel.Vollstaendig ||
                    tafel.Jahre != refTafel.Jahre) continue;
                if (!Differenzspalten(ws, tafel, refTafel, refName, zeitraum, register)) continue;

                // ---- Amortisation über die Hilfsspalte, Zinsfuß über IRR ----
                int zAmo, zIrr;
                if (imBlock && e != null && e.KapitalwertDiff.HasValue &&
                    block.Zeilen.TryGetValue("AMORTISATION", out zAmo))
                    AmortisationFormel(ws.Cell(zAmo, c), tafel, e, register);
                if (imBlock && e != null && e.KapitalwertDiff.HasValue &&
                    block.Zeilen.TryGetValue("IRR", out zIrr))
                    ZinsfussFormel(ws.Cell(zIrr, c), tafel, refTafel, e, register);
            }
        }

        /// <summary>Das Ergebnis eines Standes im Szenario <paramref name="szenario"/>.</summary>
        private static WirtschaftlichkeitErgebnis Ergebnis(List<WirtschaftlichkeitErgebnis> alle, int idProjekt,
                                                           string szenario)
        {
            return alle.FirstOrDefault(x => x.IdProjekt == idProjekt &&
                                            string.Equals(x.Szenario, szenario, StringComparison.Ordinal));
        }

        /// <summary>
        /// ETAPPE E14 — der Anhang der Namen eines Szenarios: Erwartet ohne, Günstig
        /// <see cref="ANHANG_GUENSTIG"/>, Ungünstig <see cref="ANHANG_UNGUENSTIG"/> — dieselbe
        /// Regel, nach der der Parameterblock die Zellen benennt (<c>Satzzeile</c>).
        /// </summary>
        internal static string Anhang(string szenario)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal)) return ANHANG_GUENSTIG;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal)) return ANHANG_UNGUENSTIG;
            return "";
        }

        /// <summary>ETAPPE E14 — der Name einer Größe des Parameterblocks im Szenario
        /// (<c>Zins_i</c>, <c>Zins_i_Guenstig</c>, <c>Zins_i_Unguenstig</c>).</summary>
        internal static string Name(string basis, string szenario)
        {
            return basis + Anhang(szenario);
        }

        /// <summary>ETAPPE E14 (E14‑Q1 a) — die Schutzformel eines Jahres jenseits von T_s:
        /// <c>IF(Bedingung,Ausdruck,"")</c>.</summary>
        private static string Schutz(string bedingung, string ausdruck)
        {
            return "IF(" + bedingung + "," + ausdruck + ",\"\")";
        }

        /// <summary>Der Name der Referenz, wie ihre Linie im Verlauf ihn trägt.</summary>
        private static string RefName(WirtschaftlichkeitVerlauf verlauf)
        {
            VerlaufSerie s = verlauf.Absolut.FirstOrDefault(x => x != null && x.IdProjekt == verlauf.IdReferenz);
            return s != null && !string.IsNullOrEmpty(s.Anzeige) ? s.Anzeige : BerichtTexte.T("Stamm");
        }

        /// <summary>
        /// Die vier Spalten der Differenzreihe rechts einer Tabelle: nominal (Jahr T mit
        /// Restwert-Nominaldifferenz), Barwert ohne Restwert, kumuliert, Nulldurchgang je
        /// Jahr. <c>false</c>, wenn eine Zelle die Gegenrechnung nicht besteht — dann gibt es
        /// für diese Variante keine Kennzahlformeln aus der Reihe.
        /// </summary>
        private static bool Differenzspalten(IXLWorksheet ws, MehrjahresTafel tafel, MehrjahresTafel refTafel,
                                             string refName, string zeitraum, Formelregister register)
        {
            double[] fluss = KapitalwertRechner.Differenzreihe(tafel.Bild, refTafel.Bild);
            int T = tafel.Jahre;
            if (fluss == null || fluss.Length != T + 1) return false;

            int cN = tafel.FreieSpalte, cB = cN + 1, cK = cN + 2, cH = cN + 3, cV = cN + 4, cW = cN + 5;
            tafel.Referenz = refTafel;
            tafel.SpalteDeltaNominal = cN;
            tafel.SpalteDeltaBarwert = cB;
            tafel.SpalteDeltaKumuliert = cK;
            tafel.SpalteAmortHilfe = cH;
            tafel.SpalteVorzeichen = cV;
            tafel.SpalteWechsel = cW;
            tafel.FreieSpalte = cW + 1;
            Kopf(ws, tafel.KopfZeile, cN, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_NOMINAL, refName));
            Kopf(ws, tafel.KopfZeile, cB, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_BARWERT, refName));
            Kopf(ws, tafel.KopfZeile, cK, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_KUMULIERT, refName));
            Kopf(ws, tafel.KopfZeile, cH, MyResource.Resource.WIRT_FM_MJ_AMORT_HILFE);
            Kopf(ws, tafel.KopfZeile, cV, MyResource.Resource.WIRT_FM_MJ_VORZEICHEN);
            Kopf(ws, tafel.KopfZeile, cW, MyResource.Resource.WIRT_FM_MJ_WECHSEL);

            // ETAPPE E14 (E14‑Q3 a): Vorzeichen der nominalen Differenz, fortgeschrieben über
            // Nullwerte (|Δ| ≤ Nullgrenze), und die Zahl der Wechsel bis zum Jahr — dieselbe
            // Zählregel wie KapitalwertRechner.Vorzeichenwechsel; der Zinsfuß liest den Endstand.
            string grenze = GrenzeText();
            int vorzeichen = 0, wechsel = 0;

            MehrjahresSpalte netto = tafel.Tabelle.Spalten[tafel.SpalteNetto - 2];
            MehrjahresSpalte nettoRef = refTafel.Tabelle.Spalten[refTafel.SpalteNetto - 2];
            MehrjahresSpalte bw = tafel.Tabelle.Spalten[tafel.SpalteBarwert - 2];
            MehrjahresSpalte bwRef = refTafel.Tabelle.Spalten[refTafel.SpalteBarwert - 2];
            bool alle = true;
            double kum = 0;
            for (int t = 0; t <= T; t++)
            {
                int r = tafel.Zeile(t), rr = refTafel.Zeile(t);
                ZahlFormat(ws.Cell(r, cN)); ZahlFormat(ws.Cell(r, cB)); ZahlFormat(ws.Cell(r, cK));

                // nominal — im Jahr T samt Restwert-Nominaldifferenz (Abschlusszeilen)
                string fN = Bezug(r, tafel.SpalteNetto) + "-" + Bezug(rr, refTafel.SpalteNetto);
                double nachN = netto.Wert(t) - nettoRef.Wert(t);
                if (t == T)
                {
                    fN += "+" + Bezug(tafel.AbschlussZeile, tafel.SpalteNetto) + "-" +
                          Bezug(refTafel.AbschlussZeile, refTafel.SpalteNetto);
                    nachN = nachN + tafel.Bild.RestwertNominal - refTafel.Bild.RestwertNominal;
                }
                alle &= Setze(ws.Cell(r, cN), fN, fluss[t], nachN, register);

                // Vorzeichen (fortgeschrieben) und Wechsel bis hier
                double d = fluss[t];
                int neu = double.IsNaN(d) || Math.Abs(d) <= KapitalwertRechner.VORZEICHEN_NULLGRENZE_EUR
                        ? vorzeichen : Math.Sign(d);
                string zN = Bezug(r, cN);
                string fV = "IF(ABS(" + zN + ")<=" + grenze + "," + (t == 0 ? "0" : Bezug(r - 1, cV)) +
                            ",SIGN(" + zN + "))";
                alle &= Setze(ws.Cell(r, cV), fV, neu, neu, register);
                if (t == 0)
                {
                    ws.Cell(r, cW).Value = 0;
                }
                else
                {
                    if (vorzeichen != 0 && neu != vorzeichen) wechsel++;
                    string fW = Bezug(r - 1, cW) + "+IF(AND(" + Bezug(r - 1, cV) + "<>0," + Bezug(r, cV) + "<>" +
                                Bezug(r - 1, cV) + "),1,0)";
                    alle &= Setze(ws.Cell(r, cW), fW, wechsel, wechsel, register);
                }
                vorzeichen = neu;

                // Barwert ohne Restwert und Laufsumme — die Reihe der Amortisation
                double dBw = tafel.Bild.BarwertReihe[t] - refTafel.Bild.BarwertReihe[t];
                alle &= Setze(ws.Cell(r, cB), Bezug(r, tafel.SpalteBarwert) + "-" + Bezug(rr, refTafel.SpalteBarwert),
                              dBw, bw.Wert(t) - bwRef.Wert(t), register);
                double vorher = kum;
                kum = t == 0 ? dBw : kum + dBw;
                alle &= Setze(ws.Cell(r, cK), t == 0 ? Bezug(r, cB) : Bezug(r - 1, cK) + "+" + Bezug(r, cB),
                              kum, kum, register);

                // Nulldurchgang je Jahr: (t−1) − K(t−1) / ΔBW(t), sonst ""
                if (t >= 1)
                {
                    string fH = "IF(AND(" + Bezug(r - 1, cK) + "<0," + Bezug(r, cK) + ">=0)," +
                                Bezug(r - 1, 1) + "-" + Bezug(r - 1, cK) + "/" + Bezug(r, cB) + ",\"\")";
                    if (vorher < 0 && kum >= 0)
                    {
                        double h = (t - 1) - vorher / dBw;
                        alle &= Setze(ws.Cell(r, cH), fH, h, h, register);
                        ws.Cell(r, cH).Style.NumberFormat.Format = "#,##0.00";
                    }
                    else register.FormelText(ws.Cell(r, cH), fH, "");
                }
            }

            // ETAPPE E14 (E14‑Q1 a): jenseits von T_s die Schutzformel — dieselben Ausdrücke,
            // ihr Ergebnis ist leer, solange das Jahr hinter dem Zeitraum des Szenarios liegt.
            for (int t = T + 1; t <= Math.Min(tafel.JahreTabelle, refTafel.JahreTabelle); t++)
            {
                int r = tafel.Zeile(t), rr = refTafel.Zeile(t);
                string bedingung = Bezug(r, 1) + "<=" + zeitraum;
                register.FormelText(ws.Cell(r, cN),
                    Schutz(bedingung, Bezug(r, tafel.SpalteNetto) + "-" + Bezug(rr, refTafel.SpalteNetto)), "");
                register.FormelText(ws.Cell(r, cB),
                    Schutz(bedingung, Bezug(r, tafel.SpalteBarwert) + "-" + Bezug(rr, refTafel.SpalteBarwert)), "");
                register.FormelText(ws.Cell(r, cK),
                    Schutz(bedingung, Bezug(r - 1, cK) + "+" + Bezug(r, cB)), "");
            }
            return alle;
        }

        /// <summary>Setzt eine Formel in eine NEUE Zelle — ohne vorherigen Wert; besteht
        /// die Gegenrechnung nicht, bleibt die Zelle als Wert stehen.</summary>
        private static bool Setze(IXLCell zelle, string formel, double wert, double nachgerechnet,
                                  Formelregister register)
        {
            if (register.Formel(zelle, formel, wert, nachgerechnet)) return true;
            if (!double.IsNaN(wert) && !double.IsInfinity(wert)) zelle.Value = wert;
            return false;
        }

        /// <summary>
        /// Die Amortisation einer Variante über die Hilfsspalte: ohne Mehrinvestition 0 (wenn
        /// auch das Ende nicht negativ ist), sonst der erste Nulldurchgang; ohne einen der
        /// benannte Leerwert. Dieselbe Regel wie <see cref="KapitalwertRechner.AmortisationDifferenz"/>.
        /// </summary>
        private static void AmortisationFormel(IXLCell zelle, MehrjahresTafel tafel,
                                               WirtschaftlichkeitErgebnis e, Formelregister register)
        {
            int T = tafel.Jahre;
            string k0 = Bezug(tafel.Zeile(0), tafel.SpalteDeltaKumuliert);
            string kT = Bezug(tafel.Zeile(T), tafel.SpalteDeltaKumuliert);
            string hilfe = Bezug(tafel.Zeile(1), tafel.SpalteAmortHilfe) + ":" +
                           Bezug(tafel.Zeile(T), tafel.SpalteAmortHilfe);
            string leer = MyResource.Resource.WIRT_GRUND_KEINE_AMORTISATION.Replace("\"", "\"\"");
            string formel = "IF(" + k0 + ">=0,IF(" + kT + ">=0,0,\"" + leer + "\"),IF(COUNT(" + hilfe +
                            ")=0,\"" + leer + "\",MIN(" + hilfe + ")))";

            double? nach = KapitalwertRechner.AmortisationDifferenz(tafel.Bild, RefBild(tafel));
            if (nach.HasValue != e.AmortisationJahre.HasValue) return;
            if (nach.HasValue) register.Formel(zelle, formel, e.AmortisationJahre.Value, nach.Value);
            else register.FormelText(zelle, formel, MyResource.Resource.WIRT_GRUND_KEINE_AMORTISATION);
        }

        /// <summary>
        /// Der interne Zinsfuß einer Variante über IRR auf die nominale Differenzreihe,
        /// gerundet wie im Rechenkern (zwei Nachkommastellen in Prozent); der Wert des Laufs
        /// ist der Startwert. Die Zahl der Vorzeichenwechsel liest die Formel aus der
        /// Hilfsspalte (ETAPPE E14): keiner — der benannte Leerwert „kein Zinsfuß
        /// bestimmbar"; mehr als einer — der Text „nicht eindeutig" (E14‑Q3 a; die
        /// Zinsfußgleichung hat dann mehrere Lösungen); genau einer — IRR.
        /// </summary>
        private static void ZinsfussFormel(IXLCell zelle, MehrjahresTafel tafel, MehrjahresTafel refTafel,
                                           WirtschaftlichkeitErgebnis e, Formelregister register)
        {
            int? wechsel = KapitalwertRechner.Vorzeichenwechsel(tafel.Bild, refTafel.Bild);
            if (!wechsel.HasValue || wechsel != e.IrrVorzeichenwechsel || tafel.SpalteWechsel < 0) return;
            string reihe = Bezug(tafel.Zeile(0), tafel.SpalteDeltaNominal) + ":" +
                           Bezug(tafel.Zeile(tafel.Jahre), tafel.SpalteDeltaNominal);
            string zahl = Bezug(tafel.Zeile(tafel.Jahre), tafel.SpalteWechsel);
            string kein = MyResource.Resource.WIRT_IZF_KEIN_WERT.Replace("\"", "\"\"");
            string mehrdeutig = MyResource.Resource.WIRT_FM_IZF_NICHT_EINDEUTIG.Replace("\"", "\"\"");
            string irr = e.IRR.HasValue
                ? "ROUND(IRR(" + reihe + "," + Math.Round(e.IRR.Value / 100.0, 6).ToString("R", CultureInfo.InvariantCulture) + ")*100,2)"
                : "ROUND(IRR(" + reihe + ")*100,2)";
            string formel = "IF(" + zahl + "=0,\"" + kein + "\",IF(" + zahl + ">1,\"" + mehrdeutig + "\"," + irr + "))";

            if (wechsel == 1 && e.IRR.HasValue)
            {
                double? nach = KapitalwertRechner.InternerZinsfuss(tafel.Bild, refTafel.Bild);
                if (!nach.HasValue) return;
                register.Formel(zelle, formel, e.IRR.Value, nach.Value);
            }
            else if (wechsel == 0 && !e.IRR.HasValue)
                register.FormelText(zelle, formel, MyResource.Resource.WIRT_IZF_KEIN_WERT);
            else if (wechsel > 1)
                register.FormelText(zelle, formel, MyResource.Resource.WIRT_FM_IZF_NICHT_EINDEUTIG);
        }

        /// <summary>Die Nullgrenze der Vorzeichenzählung als Formeltext („1E-6").</summary>
        private static string GrenzeText()
        {
            return KapitalwertRechner.VORZEICHEN_NULLGRENZE_EUR.ToString("0E0", CultureInfo.InvariantCulture);
        }

        /// <summary>Das Zahlungsbild der Referenz einer Tabelle mit Differenzspalten.</summary>
        private static KapitalwertRechner.Zahlungsbild RefBild(MehrjahresTafel tafel)
        {
            return tafel.Referenz != null ? tafel.Referenz.Bild : null;
        }

        /// <summary>
        /// ETAPPE E14 (Stufe 2) — eine Zeile der <b>Bandbreitentafel</b> in Formeln: ΔKW
        /// Ungünstig / Erwartet / Günstig (Spalten B, C, D) als Zellbezug auf die Zeile
        /// „Kapitalwert gegenüber Referenz" des jeweiligen Szenarioblocks, die Spanne (E) als
        /// <c>MAX(B:D)−MIN(B:D)</c> — dieselbe Regel wie <see cref="BandbreitenZeile.Spanne"/>
        /// (leere Zellen zählen nicht). Nur, wo der Block dieselbe Zahl trägt; sonst bleibt der Wert.
        /// </summary>
        internal static void Bandbreitenzeile(IXLWorksheet ws, int r, BandbreitenZeile z,
                                              Dictionary<string, KennzahlBlock> lagen, Formelregister register)
        {
            if (ws == null || z == null || lagen == null || register == null) return;
            string[] szenarien = { WirtschaftlichkeitSzenario.WORST, WirtschaftlichkeitSzenario.ERWARTET,
                                   WirtschaftlichkeitSzenario.BEST };
            double?[] werte = { z.Worst, z.Erwartet, z.Best };
            for (int k = 0; k < 3; k++)
            {
                KennzahlBlock lage;
                int zDiff, c;
                double blockwert;
                if (!werte[k].HasValue || !lagen.TryGetValue(szenarien[k], out lage) ||
                    !lage.Zeilen.TryGetValue("KAPITALWERT_DIFF", out zDiff) ||
                    !lage.Spalten.TryGetValue(z.IdProjekt, out c) ||
                    !lage.Werte.TryGetValue((zDiff, c), out blockwert)) continue;
                register.Formel(ws.Cell(r, 2 + k), Bezug(zDiff, c), werte[k].Value, blockwert);
            }
            if (z.Spanne.HasValue)
            {
                double gross = double.MinValue, klein = double.MaxValue;
                foreach (double? w in werte)
                    if (w.HasValue) { gross = Math.Max(gross, w.Value); klein = Math.Min(klein, w.Value); }
                string bereich = Bezug(r, 2) + ":" + Bezug(r, 4);
                register.Formel(ws.Cell(r, 5), "MAX(" + bereich + ")-MIN(" + bereich + ")",
                                z.Spanne.Value, gross - klein);
            }
        }

        private static void ZahlFormat(IXLCell zelle)
        {
            zelle.Style.NumberFormat.Format = "#,##0";
        }

        // =====================================================================
        //  Stufe 3 — Betriebskostenblock und Δ%-Block
        // =====================================================================

        /// <summary>Spalte „Menge" des Betriebskostenblocks (rechts neben dem Betrag — die
        /// Spalten davor bleiben, wo sie sind).</summary>
        internal const int BK_SPALTE_BETRAG = 5;
        internal const int BK_SPALTE_MENGE = 6;
        internal const int BK_SPALTE_SATZ = 7;

        /// <summary>
        /// Stufe 3 (Konzept § 2.11.6): eine BEMESSENE Betriebskostenposition bekommt Menge
        /// und Satz in eigene Spalten, der Betrag wird ihr Produkt — bei einer
        /// Prozentbemessung geteilt durch 100, bei einer Erlösposition negativ. Welche
        /// Rechnung gilt, sagt der Rechenweg selbst (derselbe Rechenweg, keine zweite Liste
        /// der Bemessungsarten): <see cref="BetriebskostenCtrl.Bemessungsfaktor"/> ist 0,01
        /// (Prozent) oder 1 (Satz je Einheit) — für jede der sechzehn bemessenen Arten; ETAPPE
        /// E8c fragt dort auch die Herleitungsspalte. Feste Beträge, Jahresbeträge,
        /// szenariogepflegte und unvollständige Positionen bleiben Werte — für sie trägt die
        /// Spalte „Herleitung" die Erklärung.
        /// </summary>
        /// <returns><c>true</c>, wenn die Zeile eine Formel bekam.</returns>
        internal static bool Betriebskostenzeile(IXLWorksheet ws, int r, KostenPositionNachweis n,
                                                 Formelregister register)
        {
            if (ws == null || n == null || register == null || n.SzenarioGepflegt ||
                !n.Menge.HasValue || !n.Einheitpreis.HasValue) return false;
            double? faktor = BetriebskostenCtrl.Bemessungsfaktor(n.Bemessung);
            if (!faktor.HasValue) return false;
            bool prozent = faktor.Value != 1.0;

            ws.Cell(r, BK_SPALTE_MENGE).Value = n.Menge.Value;
            ws.Cell(r, BK_SPALTE_MENGE).Style.NumberFormat.Format =
                MitEinheit("#,##0.00", BetriebskostenCtrl.MengenEinheit(n.Bemessung, n.Komponente));
            ws.Cell(r, BK_SPALTE_SATZ).Value = n.Einheitpreis.Value;
            ws.Cell(r, BK_SPALTE_SATZ).Style.NumberFormat.Format =
                MitEinheit("#,##0.000", BetriebskostenCtrl.SatzEinheit(n.Bemessung, n.Komponente, true));

            string produkt = Bezug(r, BK_SPALTE_MENGE) + "*" + Bezug(r, BK_SPALTE_SATZ) + (prozent ? "/100" : "");
            string formel = n.IstErloes ? "-ABS(" + produkt + ")" : produkt;
            double nach = BetriebskostenCtrl.Betrag(n.Bemessung, 0.0, n.Menge, n.Einheitpreis, n.IstErloes);
            if (register.Formel(ws.Cell(r, BK_SPALTE_BETRAG), formel, n.BetragJahr, nach)) return true;

            // Besteht die Gegenrechnung nicht, bleiben auch Menge und Satz leer — die
            // Herleitung sagt dann, was gerechnet wurde.
            ws.Cell(r, BK_SPALTE_MENGE).Clear();
            ws.Cell(r, BK_SPALTE_SATZ).Clear();
            return false;
        }

        /// <summary>Stufe 3: die Köpfe „Menge" und „Satz" über den neuen Spalten.</summary>
        internal static void BetriebskostenKopf(IXLWorksheet ws, int kopfZeile)
        {
            Kopf(ws, kopfZeile, BK_SPALTE_MENGE, MyResource.Resource.WIRT_FM_BK_MENGE);
            Kopf(ws, kopfZeile, BK_SPALTE_SATZ, MyResource.Resource.WIRT_FM_BK_SATZ);
        }

        /// <summary>Stufe 3: die Summe der Positionen als Spaltensumme der Beträge.</summary>
        internal static void BetriebskostenSumme(IXLWorksheet ws, int r, int ersteZeile, int letzteZeile,
                                                 double summe, double nachgerechnet, Formelregister register)
        {
            if (ws == null || register == null || letzteZeile < ersteZeile) return;
            register.Formel(ws.Cell(r, BK_SPALTE_BETRAG),
                "SUM(" + Bezug(ersteZeile, BK_SPALTE_BETRAG) + ":" + Bezug(letzteZeile, BK_SPALTE_BETRAG) + ")",
                summe, nachgerechnet);
        }

        /// <summary>
        /// Stufe 3: eine Zelle des Δ%-Blocks im Vergleichsblatt als Zellbezug —
        /// (Wert − Stamm) / |Stamm| · 100, derselbe Ausdruck, der den Wert gerechnet hat.
        /// </summary>
        internal static void Deltazelle(IXLCell zelle, int zeile, int spalteWert, int spalteStamm,
                                        double wert, double stamm, double delta, Formelregister register)
        {
            if (zelle == null || register == null) return;
            string s = Bezug(zeile, spalteStamm);
            register.Formel(zelle, "(" + Bezug(zeile, spalteWert) + "-" + s + ")/ABS(" + s + ")*100",
                            delta, (wert - stamm) / Math.Abs(stamm) * 100.0);
        }

        /// <summary>Ein Zahlformat mit Einheit als Literal („#,##0.00" kWh/a"") — die Zelle
        /// bleibt eine Zahl, das Prozentzeichen im Literal multipliziert nicht.</summary>
        private static string MitEinheit(string format, string einheit)
        {
            if (string.IsNullOrWhiteSpace(einheit)) return format;
            return format + "\" " + einheit.Replace("\"", "") + "\"";
        }

        /// <summary>Fortschreibung einer Spalte ab Jahr 2: Jahr 1 × (1+p)^(t−1) — mit dem
        /// Namen <paramref name="name"/> des Satzes im Szenario der Tabelle.</summary>
        private static void Fortschreibung(IXLWorksheet ws, MehrjahresTafel tafel, Mehrjahresbild bild,
                                           string schluessel, string name, double satz, Formelregister register)
        {
            int c = SpalteVon(bild, schluessel);
            if (c < 0) return;
            MehrjahresSpalte s = bild.Spalten[c - 2];
            string jahr1 = Bezug(tafel.Zeile(1), c, true);
            for (int t = 2; t <= tafel.Jahre; t++)
            {
                int r = tafel.Zeile(t);
                register.Formel(ws.Cell(r, c),
                    jahr1 + "*(1+" + name + ")^(" + Bezug(r, 1) + "-1)",
                    s.Wert(t), s.Wert(1) * Math.Pow(1.0 + satz, t - 1));
            }
        }

        /// <summary>Spalte (1-basiert) der Positionsspalte mit dem Schlüssel; −1 = keine.</summary>
        internal static int SpalteVon(Mehrjahresbild bild, string schluessel)
        {
            if (bild == null) return -1;
            for (int k = 0; k < bild.Spalten.Count; k++)
                if (string.Equals(bild.Spalten[k].Schluessel, schluessel, StringComparison.Ordinal))
                    return 2 + k;
            return -1;
        }

        private static void Kopf(IXLWorksheet ws, int zeile, int spalte, string text)
        {
            ws.Cell(zeile, spalte).Value = text;
            ws.Cell(zeile, spalte).Style.Font.Bold = true;
            ws.Cell(zeile, spalte).Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
        }

        private static void Zahl(IXLCell zelle, double wert)
        {
            zelle.Value = wert;
            zelle.Style.NumberFormat.Format = "#,##0";
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

    /// <summary>
    /// ETAPPE E8b — die Lage EINER Mehrjahrestabelle im Blatt „Wirtschaftlichkeit": wo
    /// ihre Jahre stehen, welche Spalten Netto, Barwert und Kumuliert tragen und welche
    /// Spalte rechts von ihr frei ist. Die Kennzahlen der Stufe 2 beziehen sich darauf.
    /// </summary>
    internal sealed class MehrjahresTafel
    {
        internal int IdProjekt;

        /// <summary>ETAPPE E14 — das Szenario der Tabelle (<see cref="WirtschaftlichkeitSzenario"/>);
        /// es wählt die Namen, auf die ihre Formeln rechnen.</summary>
        internal string Szenario = WirtschaftlichkeitSzenario.ERWARTET;
        internal int KopfZeile;
        internal int Jahr0Zeile;

        /// <summary>Der Betrachtungszeitraum T_s des Szenarios [a] — die Jahre mit Zahlungen.</summary>
        internal int Jahre;

        /// <summary>ETAPPE E14 (E14‑Q1 a) — die Zahl der Jahreszeilen: der längste Zeitraum
        /// der drei Szenarien (≥ <see cref="Jahre"/>); dahinter steht die Abschlusszeile.</summary>
        internal int JahreTabelle;
        internal int AbschlussZeile;
        internal int SpalteNetto = -1;
        internal int SpalteBarwert = -1;
        internal int SpalteKumuliert = -1;

        /// <summary>Die erste Spalte rechts der Tabelle und ihrer Hilfsspalten.</summary>
        internal int FreieSpalte;

        internal Mehrjahresbild Tabelle;
        internal KapitalwertRechner.Zahlungsbild Bild;

        // ---- Stufe 2: die Differenzreihe Variante − Referenz rechts der Tabelle ----

        /// <summary>Die Tabelle der Referenz, gegen die die Differenzspalten rechnen;
        /// <c>null</c> = keine Differenzspalten (die Referenz selbst oder keine Reihe).</summary>
        internal MehrjahresTafel Referenz;
        internal int SpalteDeltaNominal = -1;
        internal int SpalteDeltaBarwert = -1;
        internal int SpalteDeltaKumuliert = -1;
        internal int SpalteAmortHilfe = -1;

        /// <summary>ETAPPE E14 (E14‑Q3 a): Vorzeichen der nominalen Differenz und die Zahl
        /// der Vorzeichenwechsel bis zum Jahr — die Grundlage des Zinsfußes.</summary>
        internal int SpalteVorzeichen = -1;
        internal int SpalteWechsel = -1;

        /// <summary>Die Zeile des Jahres <paramref name="jahr"/> (0…T).</summary>
        internal int Zeile(int jahr) { return Jahr0Zeile + jahr; }

        /// <summary>true, wenn Netto, Barwert und Kumuliert als Spalten stehen.</summary>
        internal bool Vollstaendig
        {
            get { return SpalteNetto > 0 && SpalteBarwert > 0 && SpalteKumuliert > 0 && Jahre >= 1; }
        }
    }

    /// <summary>
    /// ETAPPE E8b, Stufe 2 — die Lage eines Kennzahlblocks im Blatt: die Zeile je
    /// Kennzahlschlüssel (<see cref="WirtZeile.Schluessel"/>) und die Spalte je Stand
    /// (<c>Tab_Projekt.ID</c>).
    /// </summary>
    internal sealed class KennzahlBlock
    {
        internal readonly Dictionary<string, int> Zeilen = new Dictionary<string, int>(StringComparer.Ordinal);
        internal readonly Dictionary<int, int> Spalten = new Dictionary<int, int>();

        /// <summary>ETAPPE E14 — die Zahl, die der Block in eine Zelle (Zeile, Spalte)
        /// geschrieben hat; die Bandbreitentafel verweist nur auf Zellen, deren Zahl sie trägt.</summary>
        internal readonly Dictionary<(int, int), double> Werte = new Dictionary<(int, int), double>();
    }

    /// <summary>
    /// ETAPPE E8b — das <b>Register der Formelzellen</b> einer Mappe und der Nachtrag
    /// ihrer Ergebnisse.
    ///
    /// <para><b>Warum es das gibt</b> (Befund E8b/0, <c>FormelmappeClosedXmlBefundTests</c>):
    /// ClosedXML 0.105.1 legt Formeln ohne zwischengespeichertes Ergebnis ab, und seine
    /// Rechenmaschine kennt NPV und IRR nicht. Eine Mappe nur aus Formeln zeigte in
    /// jedem Betrachter ohne eigene Rechenmaschine leere Zellen. Deshalb merkt sich dieses
    /// Register zu jeder Formelzelle die Zahl, die sie als Wert trug, und trägt sie nach dem
    /// Speichern als Ergebnis ein (OpenXML SDK, volle Stellenzahl); die Mappe verlangt
    /// zugleich die Neuberechnung beim Öffnen (<c>fullCalcOnLoad</c>) — Excel und LibreOffice
    /// rechnen dann selbst.</para>
    ///
    /// <para><b>Keine Formel ohne Gegenrechnung:</b> <see cref="Formel"/> nimmt das in C#
    /// nachgerechnete Ergebnis der Formel mit. Weicht es vom Wert ab (mehr als
    /// <see cref="TOLERANZ_RELATIV"/> bzw. <see cref="TOLERANZ_ABSOLUT"/>), bleibt die Zelle
    /// ein Wert — die Mappe zeigt dann nie eine Formel, die etwas anderes rechnet als der
    /// Bericht sagt.</para>
    /// </summary>
    internal sealed class Formelregister
    {
        /// <summary>Relative Toleranz der Gegenrechnung.</summary>
        internal const double TOLERANZ_RELATIV = 1e-9;

        /// <summary>Absolute Toleranz der Gegenrechnung [€ bzw. Einheit der Zelle].</summary>
        internal const double TOLERANZ_ABSOLUT = 1e-6;

        private sealed class Eintrag
        {
            internal string Blatt = "";
            internal int Zeile;
            internal int Spalte;
            internal double Zahl;
            internal string Text;
        }

        private readonly List<Eintrag> _eintraege = new List<Eintrag>();

        /// <summary>Zahl der geschriebenen Formeln.</summary>
        internal int Anzahl { get { return _eintraege.Count; } }

        /// <summary>Zahl der Zellen, deren Formel die Gegenrechnung nicht bestand — sie
        /// blieben Werte.</summary>
        internal int Abweichungen { get; private set; }

        /// <summary>Gleich im Sinne der Gegenrechnung.</summary>
        internal static bool Gleich(double a, double b)
        {
            return Math.Abs(a - b) <= Math.Max(TOLERANZ_ABSOLUT, TOLERANZ_RELATIV * Math.Max(Math.Abs(a), Math.Abs(b)));
        }

        /// <summary>
        /// Schreibt die Formel in die Zelle, wenn ihr nachgerechnetes Ergebnis dem Wert
        /// gleicht; der Wert wird als Ergebnis vorgemerkt. Sonst bleibt der Wert stehen.
        /// </summary>
        /// <param name="wert">Die Zahl, die die Zelle als Wert trägt (und künftig als
        /// zwischengespeichertes Ergebnis).</param>
        /// <param name="nachgerechnet">Das Ergebnis der Formel, in C# nachgerechnet.</param>
        internal bool Formel(IXLCell zelle, string formelA1, double wert, double nachgerechnet)
        {
            if (zelle == null || string.IsNullOrEmpty(formelA1)) return false;
            if (double.IsNaN(wert) || double.IsInfinity(wert) || double.IsNaN(nachgerechnet) ||
                double.IsInfinity(nachgerechnet) || !Gleich(wert, nachgerechnet))
            {
                Abweichungen++;
                return false;
            }
            zelle.FormulaA1 = formelA1;
            _eintraege.Add(new Eintrag
            {
                Blatt = zelle.Worksheet.Name,
                Zeile = zelle.Address.RowNumber,
                Spalte = zelle.Address.ColumnNumber,
                Zahl = wert
            });
            return true;
        }

        /// <summary>
        /// Eine Formel, deren Ergebnis ein TEXT ist (benannter Leerwert, Konzept § 2.11.6
        /// Stufe 2) — ohne Gegenrechnung; der Aufrufer entscheidet, dass der Text gilt.
        /// </summary>
        internal void FormelText(IXLCell zelle, string formelA1, string text)
        {
            if (zelle == null || string.IsNullOrEmpty(formelA1) || text == null) return;
            zelle.FormulaA1 = formelA1;
            _eintraege.Add(new Eintrag
            {
                Blatt = zelle.Worksheet.Name,
                Zeile = zelle.Address.RowNumber,
                Spalte = zelle.Address.ColumnNumber,
                Text = text
            });
        }

        /// <summary>
        /// Trägt nach dem Speichern je Formelzelle ihr Ergebnis ein (<c>&lt;v&gt;</c>, Zahl
        /// mit voller Stellenzahl bzw. Text mit <c>t="str"</c>). Ohne Formeln geschieht nichts.
        /// </summary>
        internal void Nachtragen(string datei)
        {
            if (_eintraege.Count == 0) return;
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(datei, true))
            {
                WorkbookPart mappe = doc.WorkbookPart;
                foreach (IGrouping<string, Eintrag> gruppe in _eintraege.GroupBy(e => e.Blatt))
                {
                    Sheet blatt = mappe.Workbook.Descendants<Sheet>()
                                       .FirstOrDefault(s => s.Name != null && s.Name.Value == gruppe.Key);
                    if (blatt == null || blatt.Id == null) continue;
                    var teil = (WorksheetPart)mappe.GetPartById(blatt.Id.Value);

                    var zellen = new Dictionary<string, Cell>(StringComparer.Ordinal);
                    foreach (Cell c in teil.Worksheet.Descendants<Cell>())
                        if (c.CellReference != null && c.CellReference.Value != null)
                            zellen[c.CellReference.Value] = c;

                    foreach (Eintrag e in gruppe)
                    {
                        Cell c;
                        if (!zellen.TryGetValue(ExcelFormelmappe.Bezug(e.Zeile, e.Spalte), out c) ||
                            c.CellFormula == null) continue;
                        if (e.Text != null)
                        {
                            c.DataType = CellValues.String;
                            c.CellValue = new CellValue(e.Text);
                        }
                        else
                        {
                            c.DataType = null;
                            c.CellValue = new CellValue(e.Zahl.ToString("R", CultureInfo.InvariantCulture));
                        }
                    }
                    teil.Worksheet.Save();
                }
            }
        }
    }
}
