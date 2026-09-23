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

        // =====================================================================
        //  Stufe 2 — die Kennzahlen des Szenarios „Erwartet"
        // =====================================================================

        /// <summary>
        /// Stufe 2 (Konzept § 2.11.6): die Kennzahlen des Szenarios „Erwartet" in Formeln.
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
        /// Vorzeichenwechsel) bleibt ein Wert — die Zinsfußgleichung hat dann mehrere
        /// Lösungen, und welche Excel fände, ist nicht die, die der Bericht nennt. Die
        /// Szenarien „Günstig" und „Ungünstig" haben keine Mehrjahrestabelle; ihre Kennzahlen
        /// bleiben Werte.</para>
        /// </summary>
        internal static void Kennzahlen(IXLWorksheet ws, KennzahlBlock block, List<MehrjahresTafel> tafeln,
            WirtschaftlichkeitVerlauf verlauf, List<WirtschaftlichkeitErgebnis> alle,
            WirtschaftlichkeitParameter p, Formelregister register)
        {
            if (ws == null || block == null || tafeln == null || verlauf == null || alle == null ||
                p == null || register == null) return;
            double i = p.Zinssatz / 100.0;
            int T = p.Betrachtungszeitraum;

            // ---- Nettobarwert je Stand über NPV ----
            bool mitNbw = block.Zeilen.TryGetValue("NETTOBARWERT", out int zNbw);
            if (mitNbw)
                foreach (MehrjahresTafel tafel in tafeln)
                {
                    int c;
                    WirtschaftlichkeitErgebnis e = Erwartet(alle, tafel.IdProjekt);
                    if (!tafel.Vollstaendig || tafel.Jahre != T || e == null || !e.Kapitalwert.HasValue ||
                        !block.Spalten.TryGetValue(tafel.IdProjekt, out c)) continue;

                    MehrjahresSpalte netto = tafel.Tabelle.Spalten[tafel.SpalteNetto - 2];
                    double nach = netto.Wert(0) + tafel.Tabelle.RestwertBarwert;
                    double npv = 0;
                    for (int t = 1; t <= T; t++) npv += netto.Wert(t) / Math.Pow(1.0 + i, t);
                    nach += npv;
                    string formel = "NPV(" + ZINS + "," + Bezug(tafel.Zeile(1), tafel.SpalteNetto) + ":" +
                                    Bezug(tafel.Zeile(T), tafel.SpalteNetto) + ")+" +
                                    Bezug(tafel.Zeile(0), tafel.SpalteNetto) + "+" +
                                    Bezug(tafel.AbschlussZeile, tafel.SpalteBarwert);
                    register.Formel(ws.Cell(zNbw, c), formel, e.Kapitalwert.Value, nach);
                }

            // ---- Die Referenz: ihre Tabelle und ihre Spalte im Block ----
            int idRef = verlauf.IdReferenz;
            MehrjahresTafel refTafel = tafeln.FirstOrDefault(x => x.IdProjekt == idRef);
            WirtschaftlichkeitErgebnis refErg = Erwartet(alle, idRef);
            int cRef;
            bool refImBlock = block.Spalten.TryGetValue(idRef, out cRef);
            string refName = RefName(verlauf);

            foreach (MehrjahresTafel tafel in tafeln)
            {
                if (tafel.IdProjekt == idRef) continue;
                WirtschaftlichkeitErgebnis e = Erwartet(alle, tafel.IdProjekt);
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
                        "PMT(" + ZINS + "," + ZEITRAUM + ",-" + Bezug(zDiff, c) + ")",
                        e.AnnuitaetKW.Value, e.KapitalwertDiff.Value * KapitalwertRechner.Annuitaet(i, T));

                // ---- Die Differenzreihe rechts der Tabelle ----
                if (refTafel == null || !tafel.Vollstaendig || !refTafel.Vollstaendig ||
                    tafel.Jahre != refTafel.Jahre) continue;
                if (!Differenzspalten(ws, tafel, refTafel, refName, register)) continue;

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

        private static WirtschaftlichkeitErgebnis Erwartet(List<WirtschaftlichkeitErgebnis> alle, int idProjekt)
        {
            return alle.FirstOrDefault(x => x.IdProjekt == idProjekt &&
                                            x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
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
                                             string refName, Formelregister register)
        {
            double[] fluss = KapitalwertRechner.Differenzreihe(tafel.Bild, refTafel.Bild);
            int T = tafel.Jahre;
            if (fluss == null || fluss.Length != T + 1) return false;

            int cN = tafel.FreieSpalte, cB = cN + 1, cK = cN + 2, cH = cN + 3;
            tafel.Referenz = refTafel;
            tafel.SpalteDeltaNominal = cN;
            tafel.SpalteDeltaBarwert = cB;
            tafel.SpalteDeltaKumuliert = cK;
            tafel.SpalteAmortHilfe = cH;
            tafel.FreieSpalte = cH + 1;
            Kopf(ws, tafel.KopfZeile, cN, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_NOMINAL, refName));
            Kopf(ws, tafel.KopfZeile, cB, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_BARWERT, refName));
            Kopf(ws, tafel.KopfZeile, cK, string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_FM_MJ_DELTA_KUMULIERT, refName));
            Kopf(ws, tafel.KopfZeile, cH, MyResource.Resource.WIRT_FM_MJ_AMORT_HILFE);

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
        /// ist der Startwert. Ohne Vorzeichenwechsel der benannte Leerwert. Mehrdeutig (mehr als
        /// ein Wechsel) bleibt die Zelle ein Wert.
        /// </summary>
        private static void ZinsfussFormel(IXLCell zelle, MehrjahresTafel tafel, MehrjahresTafel refTafel,
                                           WirtschaftlichkeitErgebnis e, Formelregister register)
        {
            int? wechsel = KapitalwertRechner.Vorzeichenwechsel(tafel.Bild, refTafel.Bild);
            if (!wechsel.HasValue || wechsel != e.IrrVorzeichenwechsel) return;
            string reihe = Bezug(tafel.Zeile(0), tafel.SpalteDeltaNominal) + ":" +
                           Bezug(tafel.Zeile(tafel.Jahre), tafel.SpalteDeltaNominal);
            string leer = MyResource.Resource.WIRT_IZF_KEIN_WERT.Replace("\"", "\"\"");
            string grenze = KapitalwertRechner.VORZEICHEN_NULLGRENZE_EUR.ToString("0E0", CultureInfo.InvariantCulture);

            if (wechsel == 1 && e.IRR.HasValue)
            {
                double? nach = KapitalwertRechner.InternerZinsfuss(tafel.Bild, refTafel.Bild);
                if (!nach.HasValue) return;
                string start = Math.Round(e.IRR.Value / 100.0, 6).ToString("R", CultureInfo.InvariantCulture);
                string formel = "IF(AND(COUNTIF(" + reihe + ",\">" + grenze + "\")>0,COUNTIF(" + reihe +
                                ",\"<-" + grenze + "\")>0),ROUND(IRR(" + reihe + "," + start + ")*100,2),\"" +
                                leer + "\")";
                register.Formel(zelle, formel, e.IRR.Value, nach.Value);
            }
            else if (wechsel == 0 && !e.IRR.HasValue)
            {
                string formel = "IF(AND(COUNTIF(" + reihe + ",\">" + grenze + "\")>0,COUNTIF(" + reihe +
                                ",\"<-" + grenze + "\")>0),ROUND(IRR(" + reihe + ")*100,2),\"" + leer + "\")";
                register.FormelText(zelle, formel, MyResource.Resource.WIRT_IZF_KEIN_WERT);
            }
        }

        /// <summary>Das Zahlungsbild der Referenz einer Tabelle mit Differenzspalten.</summary>
        private static KapitalwertRechner.Zahlungsbild RefBild(MehrjahresTafel tafel)
        {
            return tafel.Referenz != null ? tafel.Referenz.Bild : null;
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
        /// Rechnung gilt, sagt <see cref="BetriebskostenCtrl.Betrag"/> selbst (derselbe
        /// Rechenweg, keine zweite Liste der Bemessungsarten): Er rechnet für Menge 1 und
        /// Satz 1 genau 0,01 (Prozent) oder 1 (Satz je Einheit). Feste Beträge,
        /// Jahresbeträge, szenariogepflegte und unvollständige Positionen bleiben Werte —
        /// für sie trägt die Spalte „Herleitung" die Erklärung.
        /// </summary>
        /// <returns><c>true</c>, wenn die Zeile eine Formel bekam.</returns>
        internal static bool Betriebskostenzeile(IXLWorksheet ws, int r, KostenPositionNachweis n,
                                                 Formelregister register)
        {
            if (ws == null || n == null || register == null || n.SzenarioGepflegt ||
                !n.Menge.HasValue || !n.Einheitpreis.HasValue) return false;
            double faktor = BetriebskostenCtrl.Betrag(n.Bemessung, 0.0, 1.0, 1.0, false);
            bool prozent = Math.Abs(faktor - 0.01) < 1e-15;
            if (!prozent && faktor != 1.0) return false;

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

        // ---- Stufe 2: die Differenzreihe Variante − Referenz rechts der Tabelle ----

        /// <summary>Die Tabelle der Referenz, gegen die die Differenzspalten rechnen;
        /// <c>null</c> = keine Differenzspalten (die Referenz selbst oder keine Reihe).</summary>
        internal MehrjahresTafel Referenz;
        internal int SpalteDeltaNominal = -1;
        internal int SpalteDeltaBarwert = -1;
        internal int SpalteDeltaKumuliert = -1;
        internal int SpalteAmortHilfe = -1;

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
