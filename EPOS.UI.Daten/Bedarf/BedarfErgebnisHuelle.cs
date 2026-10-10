using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using SkiaSharp;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Bedarfs-Ergebnisdialogs (iU9-W8.2) — sie löst
    /// <c>Form_ErgStromverbraucher</c>, <c>Form_ErgProzesswaerme</c> und
    /// <c>Form_ErgBrauchwasserwaerme</c> ab.
    ///
    /// <para><b>Hier friert das Rechenobjekt ein.</b> Die drei Vorläufer bekamen das
    /// LEBENDE <c>SimulationStrombedarf</c> bzw. <c>SimulationWaermebedarf</c> und lasen
    /// bei jedem Optionswechsel neu daraus. Sie sind reine Anzeigen; nichts schreibt
    /// zurück. Also baut diese Hülle einmal ein <see cref="BedarfErgebnisDaten"/>, rendert
    /// die Bilder vorab und reicht beides hinein — die Komponente kennt die
    /// Simulationsklassen nicht (Risiko R-W8-2).</para>
    ///
    /// <para><b>Die Zahlen entstehen HIER, nicht in der Komponente</b> — die Hülle nennt
    /// je Kennzahl die EINHEIT, IN DER IHR WERT VORLIEGT, und die Komponente rechnet auf
    /// die gewählte Anzeigeeinheit um (<see cref="Energieeinheit"/>). Der nackte Teiler
    /// 1000, den nur die Brauchwasserfassung hatte (Befund W8‑B4), ist damit
    /// verschwunden.</para>
    ///
    /// <para><b>Seit W8‑O‑5b (07.09.2026) liegt JEDE Energiekennzahl in MWh.</b> Bis
    /// dahin nahm die Hülle <c>Waermebedarf_Brauchwasser</c> als kWh an — richtig für
    /// die Vorschau, falsch für den Lauf, der dasselbe Feld schon in MWh führte. Der
    /// Weg <c>Simulation → „Wärmebedarf-Details"</c> zeigte den Brauchwasserbedarf
    /// deshalb um den Faktor 1000 zu klein. Der Kern setzt das Feld jetzt auf BEIDEN
    /// Wegen über <c>SimulationWaermebedarf.BrauchwassersummeUebernehmen</c>, also in
    /// MWh; hier steht nur noch eine Einheit. Bei der Vorgabe MWh sind die angezeigten
    /// Zahlen zeichengleich zum Bestand.</para>
    ///
    /// <para><b>W8‑O‑5, Entscheid des Anwenders vom 04.09.2026:</b> MWh als Vorgabe, kWh
    /// wählbar, konsistent in den Ansichten. Die Wahl liegt in
    /// <c>BedarfEinheitWahl</c> — derselbe Schlüssel, den der Bedarfsprofildialog
    /// liest.</para>
    /// </summary>
    internal static class BedarfErgebnisHuelle
    {
        /// <summary>
        /// Die Farbrolle je Sicht (DG-E5): Jede Sicht nennt die GRÖSSE, die sie zeigt,
        /// und holt ihre Farbe aus der Palette — Strombedarf und Prozesse, Gebäude und
        /// Brauchwasser sind dieselben Größen wie in den Simulationsreitern.
        /// </summary>
        private static readonly Farbrolle ROLLE_STROM = Farbrolle.BEDARF;
        private static readonly Farbrolle ROLLE_PROZESS = Farbrolle.PROZESSWAERME;
        private static readonly Farbrolle ROLLE_GEBAEUDE = Farbrolle.HEIZWAERME;
        private static readonly Farbrolle ROLLE_BRAUCHWASSER = Farbrolle.WARMWASSER;

        /// <summary>
        /// Der Feldsatz des Strombedarfs — seit iU9-W9.5 eigene Methode.
        ///
        /// <para><b>DREI KATEGORIEN seit dem Anwenderwunsch W8‑E‑2</b> (Windows-Abnahme
        /// 05.09.2026). Der Bestand reihte vier gleich aussehende Zeilen untereinander,
        /// die erste davon eine LEISTUNG in kW mit der Beschriftung „max. Strombedarf" —
        /// die klang wie ein vierter Summand und war doch gar keiner. Jetzt gilt:</para>
        /// <list type="bullet">
        ///   <item><b>Leistung</b> — „max. Leistung" [kW], eigener Block, NICHT in der
        ///   Summe.</item>
        ///   <item><b>Energie</b> — die Posten „Stromganglinie" und „Strombedarf aus
        ///   Profil" (vormals „Strombedarf Gebäude"; die Zeile trägt den aus den Profilen
        ///   gerechneten Bedarf und heißt jetzt so).</item>
        ///   <item><b>Summe</b> — „Gesamter Strombedarf", abgesetzt am Fuß. Der Wert ist
        ///   der des KERNS (<c>StrombedarfGesamtMwh</c>), nicht eine hier addierte Zahl:
        ///   Die Anzeige rechnet nicht, sie zeigt.</item>
        /// </list>
        /// </summary>
        private static BedarfErgebnisDaten StromDaten(SimulationStrombedarf simulation,
                                                      int startReiter)
        {
            return new BedarfErgebnisDaten
            {
                Sicht = ErgebnisSicht.Strom,
                MitBrauchwasser = false,
                StartReiter = startReiter,
                Kennzahlen = new[]
                {
                    // Der Spitzenwert ist eine LEISTUNG in kW - deshalb "max. Leistung"
                    // und ein eigener Block (W8-E-2). Die zwei Posten und die Summe
                    // liegen in MWh (SimulationStrombedarf teilt selbst durch 4000
                    // bzw. 1000).
                    new ErgebnisKennzahl(Text_("BERG_LBL_MAX_LEISTUNG", "max. Leistung:"),
                                 F2(simulation.Strombedarf_Max), EINHEIT_KW)
                    { Art = Kennzahlart.Leistung },
                    Energie(Text_("BERG_LBL_STROMGANGLINIE", "Stromganglinie:"),
                            simulation.Stromganglinie_gesamt, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_STROM_PROFIL", "Strombedarf aus Profil:"),
                            simulation.Strombedarf_Gebaeude_gesamt, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_STROM_GESAMT", "Gesamter Strombedarf:"),
                            simulation.StrombedarfGesamtMwh, Energieeinheit.MWh,
                            Kennzahlart.Summe)
                },
                Sichten = new[]
                {
                    Sicht(Text_("BERG_OPT_STROM", "Strombedarf"), simulation.Strombedarf_monat,
                          Text_("BERG_BILD_STROM", "Strombedarf Monatsübersicht"), ROLLE_STROM)
                },
                Grafik = Grafikquelle(new[] { StromReihe(simulation) },
                                      Text_("BERG_OPT_STROM", "Strombedarf"),
                                      Text_("BERG_ACHSE_STROMBEDARF", "Strombedarf [kW]"))
            };
        }

        /// <summary>
        /// Die Stromreihe des Grafikreiters. <c>Strombedarf_viertelStundenwerte</c> trägt je nach
        /// Weg 8 760 Stunden- oder 35 040 Viertelstundenwerte [kW]; <c>Stuetzstellen</c> sagt, wie
        /// viele davon belegt sind.
        /// </summary>
        private static Grafikreihe StromReihe(SimulationStrombedarf simulation)
        {
            double[] reihe = simulation?.Strombedarf_viertelStundenwerte;
            if (reihe == null) return null;
            int belegt = Math.Min(simulation.Stuetzstellen, reihe.Length);
            if (belegt <= 0) return null;
            var werte = new double[belegt];
            Array.Copy(reihe, werte, belegt);
            return new Grafikreihe(Text_("BERG_OPT_STROM", "Strombedarf"), werte, ROLLE_STROM);
        }

        /// <summary>Eine Bedarfsreihe des Grafikreiters: Legendenname, Leistung je Stützstelle [kW], Farbrolle.</summary>
        private sealed record Grafikreihe(string Name, double[] Werte, Farbrolle Rolle)
        {
            /// <summary>Volles Jahr im Stunden- oder Viertelstundenraster?</summary>
            public bool Brauchbar => Werte != null && (Werte.Length == 8760 || Werte.Length == 35040);
        }

        /// <summary>
        /// <b>Die Bildquelle des Grafikreiters</b> (Anwenderwunsch WG vom 10.10.2026): je Auswahl der
        /// Sichten und Raster ein Bild — die Jahresganglinie (<c>JahresgangModell</c>, eine Linie je
        /// Sicht, Zeitachse mit Zoom) oder die Summen je Monat, Woche und Tag
        /// (<c>SaeulenstapelModell</c>, eine Schicht je Sicht). Die Summen rechnet der Kern
        /// (<see cref="Zeitsummen"/>); umgerechnet wird über <see cref="Energieeinheit"/>.
        /// <paramref name="reihen"/> steht Index für Index neben den Sichten; <c>null</c> = keine Sicht
        /// hat ein volles Jahr.
        /// </summary>
        private static Bedarfsgrafikquelle Grafikquelle(IReadOnlyList<Grafikreihe> reihen,
                                                        string sammelname, string yTitel)
        {
            var mit = new List<int>();
            for (int i = 0; i < reihen.Count; i++)
                if (reihen[i] != null && reihen[i].Brauchbar) mit.Add(i);
            if (mit.Count == 0) return null;

            List<Grafikreihe> Gewaehlt(IReadOnlyList<int> wahl)
                => (wahl ?? Array.Empty<int>()).Where(i => mit.Contains(i)).Distinct().OrderBy(i => i)
                                               .Select(i => reihen[i]).ToList();

            string Titel(List<Grafikreihe> g, Bedarfsraster raster)
            {
                string name = g.Count == 1 ? g[0].Name : sammelname;
                string format = raster switch
                {
                    Bedarfsraster.Monat => Text_("BED_GRAFIK_TITEL_MONAT", "{0} Monatssummen"),
                    Bedarfsraster.Woche => Text_("BED_GRAFIK_TITEL_WOCHE", "{0} Wochensummen"),
                    Bedarfsraster.Tag => Text_("BED_GRAFIK_TITEL_TAG", "{0} Tagessummen"),
                    _ => Text_("BED_GRAFIK_TITEL_JAHR", "{0} Jahresganglinie")
                };
                return string.Format(CultureInfo.CurrentCulture, format, name);
            }

            return new Bedarfsgrafikquelle
            {
                MitReihe = mit,
                Titel = (wahl, raster) =>
                {
                    List<Grafikreihe> g = Gewaehlt(wahl);
                    return g.Count == 0 ? null : Titel(g, raster);
                },
                Modell = (wahl, raster, einheit) =>
                {
                    List<Grafikreihe> g = Gewaehlt(wahl);
                    if (g.Count == 0) return null;
                    if (raster == Bedarfsraster.Jahr)
                    {
                        string xTitel = g[0].Werte.Length > 8760
                            ? Text_("BED_GRAFIK_ACHSE_VIERTELSTUNDE", "Viertelstunde")
                            : Text_("BED_GRAFIK_ACHSE_STUNDE", "Stunde");
                        return ChartRenderer.JahresgangModell(Titel(g, raster),
                            g.Select(r => new ChartRenderer.Reihe(r.Name, r.Werte, r.Rolle)).ToList(),
                            xTitel, yTitel, minimumNull: true);
                    }
                    Energieeinheit e = einheit ?? Energieeinheit.Vorgabe;
                    (string[] namen, string[] achse) = Faecher(raster);
                    return ChartRenderer.SaeulenstapelModell(Titel(g, raster), e.Text,
                        g.Select(r => new ChartRenderer.Reihe(r.Name, Summen(r.Werte, raster, e), r.Rolle)).ToList(),
                        namen, achse);
                },
                Spalten = (wahl, raster, einheit) =>
                {
                    List<Grafikreihe> g = Gewaehlt(wahl);
                    if (raster == Bedarfsraster.Jahr)
                        return g.Select(r => new ZeitreihenSpalte(r.Name, EINHEIT_KW, r.Werte)).ToList();
                    Energieeinheit e = einheit ?? Energieeinheit.Vorgabe;
                    return g.Select(r => new ZeitreihenSpalte(r.Name, e.Text, Summen(r.Werte, raster, e))).ToList();
                }
            };
        }

        /// <summary>Die Summen einer Reihe im Raster, aus den MWh des Kerns in die Anzeigeeinheit.</summary>
        private static double[] Summen(double[] werte, Bedarfsraster raster, Energieeinheit einheit)
        {
            Zeitraster zr = raster switch
            {
                Bedarfsraster.Woche => Zeitraster.Woche,
                Bedarfsraster.Tag => Zeitraster.Tag,
                _ => Zeitraster.Monat
            };
            double[] mwh = Zeitsummen.SummenMwh(werte, zr) ?? Array.Empty<double>();
            return mwh.Select(v => einheit.Aus(Energieeinheit.MWh, v)).ToArray();
        }

        /// <summary>
        /// Name je Fach (Zeigetext) und Beschriftung an der Achse: die zwölf Monate; jede vierte
        /// Woche ab der ersten; beim Tag der Monatsname am Monatsersten.
        /// </summary>
        private static (string[] Namen, string[] Achse) Faecher(Bedarfsraster raster)
        {
            string[] monate = MonateKurz();
            switch (raster)
            {
                case Bedarfsraster.Woche:
                {
                    string format = Text_("BED_GRAFIK_WOCHE_NR", "Woche {0}");
                    var namen = new string[Zeitsummen.WOCHEN];
                    var achse = new string[Zeitsummen.WOCHEN];
                    for (int w = 0; w < namen.Length; w++)
                    {
                        namen[w] = string.Format(CultureInfo.CurrentCulture, format, w + 1);
                        achse[w] = w % 4 == 0 ? (w + 1).ToString(CultureInfo.CurrentCulture) : "";
                    }
                    return (namen, achse);
                }
                case Bedarfsraster.Tag:
                {
                    string format = Text_("BED_GRAFIK_TAG_NR", "Tag {0}");
                    var namen = new string[Zeitsummen.TAGE];
                    var achse = new string[Zeitsummen.TAGE];
                    int erster = 0, monat = 0;
                    for (int t = 0; t < namen.Length; t++)
                    {
                        namen[t] = string.Format(CultureInfo.CurrentCulture, format, t + 1);
                        if (monat < 12 && t == erster)
                        {
                            achse[t] = monate[monat];
                            erster += Zeitsummen.MONATSTAGE[monat];
                            monat++;
                        }
                        else achse[t] = "";
                    }
                    return (namen, achse);
                }
                default:
                    return (monate, monate);
            }
        }

        /// <summary>Der Feldsatz des Wärmebedarfs — seit iU9-W9.5 eigene Methode.</summary>
        private static BedarfErgebnisDaten WaermeDaten(SimulationWaermebedarf simulation,
                                                       bool mitBrauchwasser, int startReiter,
                                                       string titelZusatz)
        {
            // Je Sicht die Stundenreihe [kW] fuer den Grafikreiter - die Reihe, deren Monatssummen
            // die Tabelle zeigt (reiner Profilanteil bzw. Heizkanal, ohne Netzverlust).
            var sichten = new List<Monatssicht>
            {
                Sicht(Text_("BERG_OPT_PROZESSE", "Prozesse"), simulation.Waermebedarf_Prozess_Monat,
                      Text_("BERG_BILD_PROZESS", "Prozesswärme"), ROLLE_PROZESS),
                Sicht(Text_("BERG_OPT_GEBAEUDE", "Gebäude (incl. ext. Wärmebedarf)"),
                      simulation.Waermebedarf_Gebaeude_Monat,
                      Text_("BERG_BILD_GEBAEUDE", "Gebäudewärme"), ROLLE_GEBAEUDE)
            };
            var reihen = new List<Grafikreihe>
            {
                new Grafikreihe(Text_("BERG_BILD_PROZESS", "Prozesswärme"),
                                AlsDouble(simulation.Waermebedarf_Prozess_Stunde), ROLLE_PROZESS),
                new Grafikreihe(Text_("BERG_BILD_GEBAEUDE", "Gebäudewärme"),
                                AlsDouble(simulation.Waermebedarf_Heizkanal_Stunde), ROLLE_GEBAEUDE)
            };

            // Zapfung und Zirkulation getrennt: auf dem Zapfprofilweg und auf dem Bestandsweg mit
            // Zirkulation (Entscheidungsvorlage Modellgrenzen BW4) - derselbe Posten, derselbe Stapel.
            bool zapfprofil = mitBrauchwasser &&
                              (simulation.Zapfprofil != null || simulation.Brauchwasser_Zirkulation_Mwh > 0);
            if (mitBrauchwasser)
            {
                Monatssicht brauchwasser = Sicht(Text_("BERG_OPT_BRAUCHWASSER", "Brauchwasser"),
                                                 simulation.Waermebedarf_Brauchwasser_Monat,
                                                 Text_("BERG_BILD_BRAUCHWASSER", "Brauchwasserwärme"),
                                                 ROLLE_BRAUCHWASSER, istBrauchwasser: true);
                // Der Zapfprofilweg (Umsetzungskonzept Zapfprofilgenerator 2.2, 5.2): dieselben
                // Monatswerte, das Bild aber gestapelt aus Zapfung und Zirkulation - die
                // Zirkulation ist eine eigene Teilreihe desselben Kanals. Es steht im Reiter, wenn
                // keine Stundenreihe vorliegt.
                if (zapfprofil) brauchwasser = ZapfprofilStapel(brauchwasser, simulation);
                sichten.Add(brauchwasser);
                reihen.Add(new Grafikreihe(Text_("BERG_BILD_BRAUCHWASSER", "Brauchwasserwärme"),
                                           AlsDouble(simulation.brauchwasserwerte), ROLLE_BRAUCHWASSER));
            }

            var daten = new BedarfErgebnisDaten
            {
                Sicht = ErgebnisSicht.Waerme,
                MitBrauchwasser = mitBrauchwasser,
                StartReiter = startReiter,
                TitelZusatz = titelZusatz ?? "",
                Grafik = Grafikquelle(reihen, Text_("BED_GRAFIK_WAERMEBEDARF", "Wärmebedarf"),
                                      Text_("BERG_ACHSE_WAERMEBEDARF", "Wärmebedarf [kW]")),
                Kennzahlen = new[]
                {
                    // DIESELBE GLIEDERUNG WIE BEIM STROM (Anwenderwunsch W8-E-2, hier
                    // konsequent mitgezogen): die LEISTUNG "max. Waermelast" [kW] zuerst
                    // und fuer sich, dann die Posten, und "Gesamter Waermebedarf" als
                    // abgesetzte Summe am Fuss - er stand bisher als ZWEITE Zeile
                    // mitten unter seinen eigenen Bestandteilen.
                    //
                    // ALLE Posten liegen in MWh - auch das Brauchwasser (W8-O-5b vom
                    // 07.09.2026). Es kam frueher als nackte Summe in kWh herein, aber
                    // NUR aus der Vorschau; nach einem Lauf lag dasselbe Feld in MWh und
                    // wurde hier ein zweites Mal geteilt. Der Kern fuehrt es jetzt auf
                    // beiden Wegen in MWh.
                    new ErgebnisKennzahl(Text_("BERG_LBL_MAX_WAERMELAST", "max. Wärmelast:"),
                                 F2(simulation.Waermebedarf_Max), EINHEIT_KW)
                    { Art = Kennzahlart.Leistung },
                    Energie(Text_("BERG_LBL_NETZVERLUSTE", "Netzverluste:"),
                            simulation.Waermebedarf_Netzverluste, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_WAERME_EXTERN", "Externer Wärmebedarf:"),
                            simulation.Waermebedarf_Extern_Gesamt, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_WAERME_PROZESS", "Wärmebedarf Prozess:"),
                            simulation.Waermebedarf_Prozess, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_WAERME_GEBAEUDE", "Wärmebedarf Gebäude:"),
                            simulation.Waermebedarf_Gebaeude_Gesamt, Energieeinheit.MWh),
                    Energie(mitBrauchwasser
                                ? Text_("BERG_LBL_WAERME_BRAUCHWASSER", "Wärmebedarf Brauchwasser:")
                                : Text_("BERG_LBL_DAVON_BRAUCHWASSER", "davon Brauchwasser:"),
                            simulation.Waermebedarf_Brauchwasser, Energieeinheit.MWh),
                    Energie(Text_("BERG_LBL_WAERME_GESAMT", "Gesamter Wärmebedarf:"),
                            simulation.Waermebedarf_Gesamt, Energieeinheit.MWh,
                            Kennzahlart.Summe)
                },
                Sichten = sichten
            };
            if (zapfprofil)
            {
                // Die Zirkulation als eigener Posten VOR der Summe (Umsetzungskonzept
                // Zapfprofilgenerator 5.2): Sie steckt im Brauchwasser und wird hier nur benannt.
                var liste = new List<ErgebnisKennzahl>(daten.Kennzahlen);
                int summe = liste.FindIndex(k => k.Art == Kennzahlart.Summe);
                liste.Insert(summe < 0 ? liste.Count : summe,
                             Energie(Text_("BERG_LBL_DAVON_ZIRKULATION", "davon Zirkulation:"),
                                     simulation.Brauchwasser_Zirkulation_Mwh, Energieeinheit.MWh));
                daten.Kennzahlen = liste;
            }
            if (mitBrauchwasser && simulation.Brauchwasser_Desinfektion_Mwh > 0)
            {
                // Die thermische Desinfektion (BW5, Konzept Simulationsablauf 21) als eigener Posten VOR der
                // Summe: Sie steht im Brauchwasserkanal, nicht im Profilanteil darüber.
                var liste = new List<ErgebnisKennzahl>(daten.Kennzahlen);
                int summe = liste.FindIndex(k => k.Art == Kennzahlart.Summe);
                liste.Insert(summe < 0 ? liste.Count : summe,
                             Energie(Text_("BERG_LBL_DAVON_DESINFEKTION", "davon thermische Desinfektion:"),
                                     simulation.Brauchwasser_Desinfektion_Mwh, Energieeinheit.MWh));
                daten.Kennzahlen = liste;
            }
            return daten;
        }

        /// <summary>
        /// Die Monatssicht des Brauchwassers auf dem Zapfprofilweg: dieselben Zahlen, das Bild
        /// gestapelt aus Zapfung und Zirkulation — beide Schichten so, wie der Kern sie führt
        /// (<c>Waermebedarf_Brauchwasser_Zapfung_Monat</c>, <c>…_Zirkulation_Monat</c>); die Hülle
        /// rechnet keine Schicht selbst. Je Einheit einmal, mit den Zeichenbausteinen des
        /// Zapfprofils (<c>ZapfprofilBilder.JahresgangModell</c>).
        /// </summary>
        private static Monatssicht ZapfprofilStapel(Monatssicht sicht, SimulationWaermebedarf simulation)
        {
            double[] zapfung = simulation.Waermebedarf_Brauchwasser_Zapfung_Monat;
            double[] zirkulation = simulation.Waermebedarf_Brauchwasser_Zirkulation_Monat;
            if (zapfung == null || zapfung.Length < 12 || zirkulation == null || zirkulation.Length < 12) return sicht;

            double[] zapfungMwh = (double[])zapfung.Clone();
            double[] zirkMwh = (double[])zirkulation.Clone();

            ZapfprofilBildtexte texte = ZapfprofilHuelle.Bildtexte();
            texte.TitelJahresgang = Text_("BERG_BILD_BRAUCHWASSER", "Brauchwasserwärme");
            return sicht with
            {
                Modell = ZapfprofilBilder.JahresgangModell(zapfungMwh, zirkMwh, Energieeinheit.MWh.Text, texte),
                ModellKWh = ZapfprofilBilder.JahresgangModell(InKWh(zapfungMwh), InKWh(zirkMwh), Energieeinheit.KWh.Text, texte)
            };
        }

        private static double[] InKWh(double[] mwh)
        {
            var kwh = new double[mwh.Length];
            for (int i = 0; i < mwh.Length; i++) kwh[i] = Energieeinheit.KWh.AusMWh(mwh[i]);
            return kwh;
        }

        // =================================================================================

        private const string EINHEIT_KW = "kW";

        /// <summary>
        /// Eine ENERGIEKENNZAHL: die Zahl samt der Einheit, IN DER SIE VORLIEGT. Der
        /// mitgegebene Text ist ihre MWh-Fassung — genau das, was der Bestand anzeigte
        /// — und dient der Komponente als Rückfall.
        /// </summary>
        private static ErgebnisKennzahl Energie(string bezeichnung, double wert,
                                                Energieeinheit quelle,
                                                Kennzahlart art = Kennzahlart.Energie)
        {
            return new ErgebnisKennzahl(bezeichnung,
                                        F2(Energieeinheit.MWh.Aus(quelle, wert)),
                                        Energieeinheit.MWh.Text)
            {
                Energie = wert,
                QuelleEinheit = quelle,
                Art = art
            };
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn seit iU9-W9.5
        /// auch die Ueberlagerung in <c>BedarfsProfileDialog</c> nehmen kann (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            BedarfErgebnisDaten daten, string reiterKennzahlen, string reiterMonate,
            string reiterGrafik, string gruppeMonate, string hilfeSchluessel)
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["TitelText"] = Text_("BERG_TITEL", "Simulation Ergebnisse"),
                ["ReiterKennzahlen"] = reiterKennzahlen,
                ["ReiterMonate"] = reiterMonate,
                ["ReiterGrafik"] = reiterGrafik,
                ["GruppeMonate"] = gruppeMonate,
                ["EinheitMonat"] = Energieeinheit.MWh.Text,
                // Die Anzeigeeinheit (Entscheid W8-O-5 vom 04.09.2026): MWh als Vorgabe,
                // kWh waehlbar - dieselbe gemerkte Wahl wie im Bedarfsprofildialog, aus
                // dem dieser Dialog als Ueberlagerung kommt.
                ["LabelEinheit"] = Text_("ALLG_LBL_EINHEIT", "Einheit:"),
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),
                // Die drei Kategorien (Anwenderwunsch W8-E-2).
                ["GruppeLeistung"] = Text_("BERG_GRP_LEISTUNG", "Leistung"),
                ["GruppeEnergie"] = Text_("BERG_GRP_ENERGIE", "Energie"),
                // Der Grafikreiter: Bedarfsart und Zeitraster als Knopfgruppen, CSV am Bild.
                ["RasterJahrText"] = Text_("BERG_STUFE_JAHR", "Jahr"),
                ["RasterMonatText"] = Text_("BED_GRAFIK_RASTER_MONAT", "Monat"),
                ["RasterWocheText"] = Text_("BERG_STUFE_WOCHE", "Woche"),
                ["RasterTagText"] = Text_("BERG_STUFE_TAG", "Tag"),
                ["GruppeReihenText"] = Text_("BED_GRAFIK_GRP_REIHEN", "Bedarfsart"),
                ["GruppeRasterText"] = Text_("BED_GRAFIK_GRP_RASTER", "Zeitraster"),
                ["CsvSpeichern"] = new Func<string, IReadOnlyList<ZeitreihenSpalte>, Task>(CsvSpeichern),
                ["Monatsnamen"] = Monatsnamen(),
                // Die Farbe einer Reihe gilt ANWENDUNGSWEIT (Farbrollen, Bedienung
                // Teil 2): Diagrammfarben schreibt sie ueber EinstellungenCtrl, und
                // schon das naechste Bild traegt sie - Bildschirm wie Bericht.
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["HilfeSchluessel"] = hilfeSchluessel
            };
        }

        /// <summary>
        /// Der Parametersatz zum STROMBEDARF — dieselben Daten wie <see cref="Zeigen"/>,
        /// nur ohne Fenster (iU9-W9.5).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            SimulationStrombedarf simulation, int startReiter)
        {
            return Gaben(StromDaten(simulation, startReiter),
                         Text_("BERG_REITER_STROM_ERG", "Strombedarf Ergebnisse"),
                         Text_("BERG_REITER_STROM_MONAT", "Strombedarf monatlich"),
                         Text_("BERG_REITER_STROM_GRAFIK", "Grafik Strombedarf"),
                         Text_("BERG_GRP_STROM_MONAT", "Strombedarf monatlicher Verlauf:"),
                         "Form_ErgStromverbraucher.btn_Help");
        }

        /// <summary>Der Parametersatz zum WAERMEBEDARF (iU9-W9.5).</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            SimulationWaermebedarf simulation, bool mitBrauchwasser, int startReiter,
            string titelZusatz)
        {
            return Gaben(WaermeDaten(simulation, mitBrauchwasser, startReiter, titelZusatz),
                         Text_("BERG_REITER_WAERME_ERG", "Wärmebedarf Ergebnisse"),
                         Text_("BERG_REITER_MONAT", "Übersicht monatlich"),
                         Text_("BERG_REITER_GRAFIK", "Grafik"),
                         Text_("BERG_GRP_MONAT", "monatlicher Verlauf:"),
                         mitBrauchwasser ? "Form_ErgBrauchwasserwaerme.btn_Help"
                                         : "Form_ErgProzesswaerme.btn_Help");
        }

        /// <summary>
        /// Eine Monatssicht: die zwölf Werte und das fertige Säulenbild. Eine fehlende
        /// Reihe bleibt <c>null</c> und zeigt „—" statt zwölf Nullen.
        ///
        /// <para><b>Die Monatswerte liegen in MWh</b> — <c>BhkwPlan.MonatsSumme</c>
        /// nimmt die Stundenwerte mal 0,001, <c>MonatsSumme_MW</c> beim Strom ebenso.
        /// Die Zahlen gehen deshalb samt <see cref="Energieeinheit.MWh"/> in die
        /// Komponente; die <c>F2</c>-Texte bleiben als Rückfall stehen.</para>
        ///
        /// <para><b>Das Bild entsteht ZWEIMAL</b>, einmal je Einheit. Weder ein PNG noch
        /// ein Zeichenmodell lässt sich umrechnen — beide tragen die fertig
        /// formatierten Achsen- und Zeigetexte —, und die Komponente ruft keinen
        /// Renderer (Risiko R‑W8‑2).</para>
        ///
        /// <para><b>Seit der Etappe DG-E3, Gruppe (c), ist es ein ZEICHENMODELL</b>
        /// (<c>MonatsSaeulenModell</c> statt <c>MonatsSaeulen</c>): Die zwölf Säulen
        /// stehen als SVG im Baustein <c>DiagrammSvg</c>, jede mit ihrem Wert am
        /// Mauszeiger (DG-E3-10). Beide Fassungen entstehen hier EINMAL und bleiben
        /// im Datensatz liegen; der Baustein baut seinen Knotenbaum nur neu, wenn die
        /// REFERENZ des Modells wechselt.</para>
        /// </summary>
        private static Monatssicht Sicht(string bezeichnung, double[] monat, string bildtitel,
                                         Farbrolle rolle, bool istBrauchwasser = false)
        {
            if (monat == null || monat.Length < 12)
                return new Monatssicht(bezeichnung, null, null, istBrauchwasser);

            var texte = new string[12];
            var mwh = new double[12];
            var kwh = new double[12];
            for (int m = 0; m < 12; m++)
            {
                texte[m] = F2(monat[m]);
                mwh[m] = monat[m];
                kwh[m] = Energieeinheit.KWh.AusMWh(monat[m]);
            }

            string[] monate = MonateKurz();
            Zeichenmodell modell = ChartRenderer.MonatsSaeulenModell(
                bildtitel, mwh, rolle, Energieeinheit.MWh.Text, monate);
            Zeichenmodell modellKWh = ChartRenderer.MonatsSaeulenModell(
                bildtitel, kwh, rolle, Energieeinheit.KWh.Text, monate);

            return new Monatssicht(bezeichnung, texte, modell, istBrauchwasser)
            {
                Zahlen = mwh,
                QuelleEinheit = Energieeinheit.MWh,
                ModellKWh = modellKWh
            };
        }

        /// <summary>Die Formatierung der Vorläufer: <c>ToString("F2")</c> in der Anzeigekultur.</summary>
        private static string F2(double wert) => wert.ToString("F2", CultureInfo.CurrentCulture);

        private static double[] AlsDouble(double[] reihe)
        {
            if (reihe == null) return null;
            var d = new double[reihe.Length];
            for (int i = 0; i < reihe.Length; i++) d[i] = reihe[i];
            return d;
        }

        /// <summary>Die zwölf Zeilenbeschriftungen der Monatstabelle (mit Doppelpunkt).</summary>
        private static string[] Monatsnamen()
        {
            var namen = new string[12];
            for (int m = 0; m < 12; m++)
                namen[m] = Text_("ALLG_MONAT_" + (m + 1), MONATE_DE[m]) + ":";
            return namen;
        }

        /// <summary>Die zwölf Kurzformen an der x-Achse des Säulenbildes.</summary>
        private static string[] MonateKurz()
        {
            var namen = new string[12];
            for (int m = 0; m < 12; m++)
                namen[m] = Text_("ALLG_MONAT_KURZ_" + (m + 1), MONATE_KURZ_DE[m]);
            return namen;
        }

        private static readonly string[] MONATE_DE =
        { "Januar", "Februar", "März", "April", "Mai", "Juni",
          "Juli", "August", "September", "Oktober", "November", "Dezember" };

        private static readonly string[] MONATE_KURZ_DE =
        { "Jan", "Feb", "Mrz", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez" };

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// <b>CSV am Bild des Grafikreiters</b>: dieselbe Datei wie „CSV…“ an den Diagrammen der
        /// Ergebnisseite (<see cref="CsvExportClass.ExportZeitreihen"/>, Dateiname aus dem Bildtitel
        /// nach dem Muster <c>…_Projekt_{n}.csv</c>), das Raster aus der Zeilenzahl.
        /// </summary>
        private static Task CsvSpeichern(string titel, IReadOnlyList<ZeitreihenSpalte> spalten)
            => CsvExportClass.ExportZeitreihen(
                string.Format(MyResource.Resource.CHART_DATEI_GANGLINIE, ZeitreihenCsv.Dateistamm(titel),
                              Dienste.Projekt.Id),
                ZeitreihenCsv.RasterAus(spalten), spalten);

        // =================================================================
        // Die Farbe einer Reihe (Farbrollen, Bedienung Teil 2)
        // =================================================================

        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags landet hier: Die Rolle
        /// bekommt anwendungsweit diese Farbe, und danach trägt sie jedes Diagramm
        /// und jeder Bericht — beide malen über dieselbe Palette.
        /// </summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }
    }
}
