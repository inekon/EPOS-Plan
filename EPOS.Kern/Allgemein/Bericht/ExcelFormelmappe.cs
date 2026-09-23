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
        //  Stufe 1 — die Mehrjahrestabelle in Formeln
        // =====================================================================

        /// <summary>Überschrift der Hilfsspalte „Basis des Betriebs-Topfes".</summary>
        private static string KopfBasisPB { get { return MyResource.Resource.WIRT_FM_MJ_BASIS_PB; } }

        /// <summary>Überschrift der Hilfsspalte „Basis des Endenergie-Topfes".</summary>
        private static string KopfBasisPE { get { return MyResource.Resource.WIRT_FM_MJ_BASIS_PE; } }

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
        /// <param name="p">Der Parametersatz des Laufs — derselbe, aus dem der
        /// Parameterblock steht und mit dem die Tabelle gerechnet wurde.</param>
        /// <returns>Die Lage der Tabelle; <c>null</c>, wenn es kein Zahlungsbild gibt.</returns>
        internal static MehrjahresTafel Mehrjahrestabelle(IXLWorksheet ws, int kopfZeile,
            int idProjekt, Mehrjahresbild bild, VerlaufSerie serie, WirtschaftlichkeitParameter p,
            Formelregister register)
        {
            KapitalwertRechner.Zahlungsbild zb = serie != null ? serie.Bild : null;
            if (ws == null || bild == null || zb == null || p == null || register == null) return null;

            var tafel = new MehrjahresTafel
            {
                IdProjekt = idProjekt,
                KopfZeile = kopfZeile,
                Jahr0Zeile = kopfZeile + 1,
                Jahre = bild.Jahre,
                AbschlussZeile = kopfZeile + 1 + bild.Jahre + 1,
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

            double i = p.Zinssatz / 100.0;
            double pE = p.PreissteigerungEnergie / 100.0;
            double pB = p.PreissteigerungBetrieb / 100.0;

            // ---- Energie und CO₂-Abgabe (Rückfallzweig): Fortschreibung ab Jahr 2 ----
            Fortschreibung(ws, tafel, bild, "ENERGIE", pE, register);
            if (zb.BehgFortgeschrieben) Fortschreibung(ws, tafel, bild, "BEHG", pE, register);

            // ---- Betrieb als zwei Terme über Hilfsspalten ----
            int cBetrieb = SpalteVon(bild, "BETRIEB");
            if (cBetrieb > 0 && zb.BetriebBasisJeJahr != null && zb.EndenergieBasisJeJahr != null &&
                zb.BetriebBasisJeJahr.Length > T && zb.EndenergieBasisJeJahr.Length > T)
            {
                bool mitEndenergie = false;
                for (int t = 1; t <= T; t++) if (zb.EndenergieBasisJeJahr[t] != 0) mitEndenergie = true;

                int hB = tafel.FreieSpalte++;
                int hE = mitEndenergie ? tafel.FreieSpalte++ : -1;
                Kopf(ws, kopfZeile, hB, KopfBasisPB);
                if (hE > 0) Kopf(ws, kopfZeile, hE, KopfBasisPE);
                MehrjahresSpalte betrieb = bild.Spalten[cBetrieb - 2];

                for (int t = 1; t <= T; t++)
                {
                    int r = tafel.Zeile(t);
                    Zahl(ws.Cell(r, hB), zb.BetriebBasisJeJahr[t]);
                    if (hE > 0) Zahl(ws.Cell(r, hE), zb.EndenergieBasisJeJahr[t]);

                    string jahr = Bezug(r, 1);
                    string formel = hE > 0
                        ? "-(" + Bezug(r, hB) + "*(1+" + PREIS_B + ")^(" + jahr + "-1)+" +
                          Bezug(r, hE) + "*(1+" + PREIS_E + ")^(" + jahr + "-1))"
                        : "-" + Bezug(r, hB) + "*(1+" + PREIS_B + ")^(" + jahr + "-1)";
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
                    Bezug(r, tafel.SpalteNetto) + "*(1+" + ZINS + ")^(-" + Bezug(r, 1) + ")",
                    barwert.Wert(t), barwertNach);

                kum = t == 0 ? barwert.Wert(0) : kum + barwert.Wert(t);
                register.Formel(ws.Cell(r, tafel.SpalteKumuliert),
                    t == 0 ? Bezug(r, tafel.SpalteBarwert)
                           : Bezug(r - 1, tafel.SpalteKumuliert) + "+" + Bezug(r, tafel.SpalteBarwert),
                    kumuliert.Wert(t), kum);
            }

            // ---- Abschlusszeile: nominaler Restwert, sein Barwert, der Nettobarwert ----
            int rA = tafel.AbschlussZeile;
            Zahl(ws.Cell(rA, tafel.SpalteNetto), zb.RestwertNominal);
            ws.Cell(rA, tafel.SpalteNetto).Style.Font.Bold = true;
            ws.Cell(rA, tafel.SpalteNetto).Style.Fill.BackgroundColor = ExcelBerichtGenerator.STAMM;
            register.Formel(ws.Cell(rA, tafel.SpalteBarwert),
                Bezug(rA, tafel.SpalteNetto) + "*(1+" + ZINS + ")^(-" + Bezug(tafel.Zeile(T), 1) + ")",
                bild.RestwertBarwert, zb.RestwertNominal * Math.Pow(1.0 + i, -T));
            register.Formel(ws.Cell(rA, tafel.SpalteKumuliert),
                Bezug(tafel.Zeile(T), tafel.SpalteKumuliert) + "+" + Bezug(rA, tafel.SpalteBarwert),
                bild.Kapitalwert, kumuliert.Wert(T) + bild.RestwertBarwert);
            return tafel;
        }

        /// <summary>Fortschreibung einer Spalte ab Jahr 2: Jahr 1 × (1+p)^(t−1).</summary>
        private static void Fortschreibung(IXLWorksheet ws, MehrjahresTafel tafel, Mehrjahresbild bild,
                                           string schluessel, double satz, Formelregister register)
        {
            int c = SpalteVon(bild, schluessel);
            if (c < 0) return;
            MehrjahresSpalte s = bild.Spalten[c - 2];
            string name = string.Equals(schluessel, "ENERGIE", StringComparison.Ordinal) ||
                          string.Equals(schluessel, "BEHG", StringComparison.Ordinal) ? PREIS_E : PREIS_B;
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
        internal int KopfZeile;
        internal int Jahr0Zeile;
        internal int Jahre;
        internal int AbschlussZeile;
        internal int SpalteNetto = -1;
        internal int SpalteBarwert = -1;
        internal int SpalteKumuliert = -1;

        /// <summary>Die erste Spalte rechts der Tabelle und ihrer Hilfsspalten.</summary>
        internal int FreieSpalte;

        internal Mehrjahresbild Tabelle;
        internal KapitalwertRechner.Zahlungsbild Bild;

        /// <summary>Die Zeile des Jahres <paramref name="jahr"/> (0…T).</summary>
        internal int Zeile(int jahr) { return Jahr0Zeile + jahr; }

        /// <summary>true, wenn Netto, Barwert und Kumuliert als Spalten stehen.</summary>
        internal bool Vollstaendig
        {
            get { return SpalteNetto > 0 && SpalteBarwert > 0 && SpalteKumuliert > 0 && Jahre >= 1; }
        }
    }

    /// <summary>
    /// ETAPPE E8b — das <b>Register der Formelzellen</b> einer Mappe und der Nachtrag
    /// ihrer Ergebnisse.
    ///
    /// <para><b>Warum es das gibt</b> (Befund E8b/0, <c>FormelmappeClosedXmlBefundTests</c>):
    /// ClosedXML 0.105.1 legt Formeln ohne zwischengespeichertes Ergebnis ab, und seine
    /// Rechenmaschine kennt NPV, PMT und IRR nicht. Eine Mappe nur aus Formeln zeigte in
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
