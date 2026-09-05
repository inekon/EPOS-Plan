using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;
using SkiaSharp;

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
    /// verschwunden: <c>Waermebedarf_Brauchwasser</c> kommt aus
    /// <c>brauchwasserwerte.Sum()</c> und liegt in kWh, alle übrigen Energiekennzahlen
    /// liegen in MWh. Bei der Vorgabe MWh sind die angezeigten Zahlen zeichengleich zum
    /// Bestand.</para>
    ///
    /// <para><b>W8‑O‑5, Entscheid des Anwenders vom 04.09.2026:</b> MWh als Vorgabe, kWh
    /// wählbar, konsistent in den Ansichten. Die Wahl liegt in
    /// <c>BedarfEinheitWahl</c> — derselbe Schlüssel, den der Bedarfsprofildialog
    /// liest.</para>
    /// </summary>
    internal static class BedarfErgebnisHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 568 × 346 bzw. 563 × 425).</summary>
        private static readonly Size MASS = new Size(900, 640);

        /// <summary>Die vier Sichtfarben der Vorläufer — wörtlich aus den drei Masken.</summary>
        private static readonly SKColor FARBE_STROM = SKColors.YellowGreen;
        private static readonly SKColor FARBE_PROZESS = SKColors.Red;
        private static readonly SKColor FARBE_GEBAEUDE = SKColors.Blue;
        private static readonly SKColor FARBE_BRAUCHWASSER = SKColors.Orange;
        private static readonly SKColor FARBE_JAHR = SKColors.SteelBlue;

        // =================================================================================
        // Die beiden Einstiege
        // =================================================================================

        /// <summary>
        /// Der Strombedarf (<c>Form_ErgStromverbraucher</c>). Der Startreiter ist
        /// Vorgabesache des Aufrufers: Die meisten Wege setzten <c>SetPage(1)</c>, der
        /// Weg aus <c>Form_Simulation_Detail</c> gar nichts (also Reiter 0).
        /// </summary>
        internal static void Zeigen(IWin32Window besitzer, SimulationStrombedarf simulation,
                                   int startReiter = 0)
        {
            Oeffnen(besitzer, StromDaten(simulation, startReiter),
                    Text_("BERG_REITER_STROM_ERG", "Strombedarf Ergebnisse"),
                    Text_("BERG_REITER_STROM_MONAT", "Strombedarf monatlich"),
                    Text_("BERG_REITER_STROM_GRAFIK", "Grafik Strombedarf"),
                    Text_("BERG_GRP_STROM_MONAT", "Strombedarf monatlicher Verlauf:"),
                    "Form_ErgStromverbraucher.btn_Help");
        }

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
        ///   der des KERNS (<c>Strombedarf_gesamt</c>), nicht eine hier addierte Zahl:
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
                            simulation.Strombedarf_gesamt, Energieeinheit.MWh,
                            Kennzahlart.Summe)
                },
                Sichten = new[]
                {
                    Sicht(Text_("BERG_OPT_STROM", "Strombedarf"), simulation.Strombedarf_monat,
                          Text_("BERG_BILD_STROM", "Strombedarf Monatsübersicht"), FARBE_STROM)
                },
                Ganglinie = Gangquelle(simulation)
            };
        }

        /// <summary>
        /// Die Bildquelle der Zeitstufen WOCHE und TAG (Anwenderwunsch W8‑E‑2).
        ///
        /// <para><b>Auf Zuruf gezeichnet, nicht auf Vorrat.</b> 52 Wochen und 365 Tage
        /// sind 417 Bilder; die Hülle gibt deshalb einen Delegaten hinein und zeichnet
        /// erst, wenn der Anwender die Stufe wählt — dasselbe Muster wie beim
        /// Stromgang-Reiter der Ergebnisseite (W11b).</para>
        ///
        /// <para><b>Kein neues Renderer-Bild.</b> Gezeichnet wird mit
        /// <c>ChartRenderer.Jahresverlauf</c> und einem <c>Achsenfenster</c> — dem
        /// Zuschnitt, den die Ergebnisseite für ihren Datenzoom schon benutzt.</para>
        ///
        /// <para><b>Das Raster kommt aus dem Rechenobjekt.</b>
        /// <c>Strombedarf_viertelStundenwerte</c> trägt je nach Weg 8 760 Stunden- oder
        /// 35 040 Viertelstundenwerte; <c>Stuetzstellen</c> sagt, welches von beidem
        /// vorliegt. Ohne diese Angabe träfe „Woche 12" die falschen Stunden.</para>
        /// </summary>
        private static Ganglinienquelle Gangquelle(SimulationStrombedarf simulation)
        {
            if (simulation == null) return null;

            float[] reihe = simulation.Strombedarf_viertelStundenwerte;
            int belegt = Math.Min(simulation.Stuetzstellen, reihe == null ? 0 : reihe.Length);
            if (belegt < 48) return null;

            // Werte je Stunde: 1 im Vorschauraster, 4 nach einem vollen Lauf.
            int jeStunde = belegt > 8760 ? 4 : 1;
            double[] werte = new double[belegt];
            for (int i = 0; i < belegt; i++) werte[i] = reihe[i];

            string titel = Text_("BERG_BILD_STROM_GANG", "Strombedarf Ganglinie");
            string yTitel = Text_("BERG_ACHSE_STROMBEDARF", "Strombedarf [kW]");

            return new Ganglinienquelle
            {
                Wochen = Math.Max(1, belegt / (168 * jeStunde)),
                Tage = Math.Max(1, belegt / (24 * jeStunde)),
                Bild = (stufe, nummer) =>
                {
                    int schritt = (stufe == Gangstufe.Woche ? 168 : 24) * jeStunde;
                    int von = nummer * schritt;
                    if (von < 0 || von >= belegt) return null;
                    int bis = Math.Min(belegt, von + schritt);

                    return ChartRenderer.Jahresverlauf(titel, werte, yTitel, FARBE_STROM,
                        new ChartRenderer.Achsenfenster(von, bis));
                }
            };
        }

        /// <summary>
        /// Der Wärmebedarf (<c>Form_ErgProzesswaerme</c> ohne, <c>Form_ErgBrauchwasserwaerme</c>
        /// mit Brauchwassersicht).
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="simulation">Das Rechenobjekt; es wird nur GELESEN.</param>
        /// <param name="mitBrauchwasser">
        /// <c>true</c> = die dritte Sicht samt Jahresverlauf (Brauchwasserfassung).
        /// </param>
        /// <param name="startReiter">0 = Kennzahlen, 1 = Monatswerte, 2 = Grafik.</param>
        /// <param name="titelZusatz">Zusatz hinter dem Titel; leer = ohne.</param>
        internal static void Zeigen(IWin32Window besitzer, SimulationWaermebedarf simulation,
                                    bool mitBrauchwasser, int startReiter = 0, string titelZusatz = "")
        {
            Oeffnen(besitzer, WaermeDaten(simulation, mitBrauchwasser, startReiter, titelZusatz),
                    Text_("BERG_REITER_WAERME_ERG", "Wärmebedarf Ergebnisse"),
                    Text_("BERG_REITER_MONAT", "Übersicht monatlich"),
                    Text_("BERG_REITER_GRAFIK", "Grafik"),
                    Text_("BERG_GRP_MONAT", "monatlicher Verlauf:"),
                    mitBrauchwasser ? "Form_ErgBrauchwasserwaerme.btn_Help"
                                    : "Form_ErgProzesswaerme.btn_Help");
        }

        /// <summary>Der Feldsatz des Wärmebedarfs — seit iU9-W9.5 eigene Methode.</summary>
        private static BedarfErgebnisDaten WaermeDaten(SimulationWaermebedarf simulation,
                                                       bool mitBrauchwasser, int startReiter,
                                                       string titelZusatz)
        {
            var sichten = new List<Monatssicht>
            {
                Sicht(Text_("BERG_OPT_PROZESSE", "Prozesse"), simulation.Waermebedarf_Prozess_Monat,
                      Text_("BERG_BILD_PROZESS", "Prozesswärme"), FARBE_PROZESS),
                Sicht(Text_("BERG_OPT_GEBAEUDE", "Gebäude (incl. ext. Wärmebedarf)"),
                      simulation.Waermebedarf_Gebaeude_Monat,
                      Text_("BERG_BILD_GEBAEUDE", "Gebäudewärme"), FARBE_GEBAEUDE)
            };

            byte[] jahresbild = null;
            if (mitBrauchwasser)
            {
                sichten.Add(Sicht(Text_("BERG_OPT_BRAUCHWASSER", "Brauchwasser"),
                                  simulation.Waermebedarf_Brauchwasser_Monat,
                                  Text_("BERG_BILD_BRAUCHWASSER", "Brauchwasserwärme"),
                                  FARBE_BRAUCHWASSER, istBrauchwasser: true));

                jahresbild = ChartRenderer.Jahresverlauf(
                    Text_("BERG_BILD_JAHR", "Jahresübersicht"),
                    AlsDouble(simulation.brauchwasserwerte),
                    Text_("BERG_ACHSE_WAERMEBEDARF", "Wärmebedarf [kW]"), FARBE_JAHR);
            }

            return new BedarfErgebnisDaten
            {
                Sicht = ErgebnisSicht.Waerme,
                MitBrauchwasser = mitBrauchwasser,
                StartReiter = startReiter,
                TitelZusatz = titelZusatz ?? "",
                JahresverlaufBild = jahresbild,
                Kennzahlen = new[]
                {
                    // DIESELBE GLIEDERUNG WIE BEIM STROM (Anwenderwunsch W8-E-2, hier
                    // konsequent mitgezogen): die LEISTUNG "max. Waermelast" [kW] zuerst
                    // und fuer sich, dann die Posten, und "Gesamter Waermebedarf" als
                    // abgesetzte Summe am Fuss - er stand bisher als ZWEITE Zeile
                    // mitten unter seinen eigenen Bestandteilen.
                    //
                    // Die Posten liegen in MWh - bis auf das Brauchwasser in kWh: Es
                    // kommt aus brauchwasserwerte.Sum(), waehrend SimulationWaermebedarf
                    // jede andere Groesse selbst durch 1000 teilt (Befund W8-B4). Genau
                    // das war der Sonderteiler, den nur die Brauchwasserfassung hatte;
                    // seit W8-O-5 steht die Einheit am Wert.
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
                            simulation.Waermebedarf_Brauchwasser, Energieeinheit.KWh),
                    Energie(Text_("BERG_LBL_WAERME_GESAMT", "Gesamter Wärmebedarf:"),
                            simulation.Waermebedarf_Gesamt, Energieeinheit.MWh,
                            Kennzahlart.Summe)
                },
                Sichten = sichten
            };
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

        private static void Oeffnen(IWin32Window besitzer, BedarfErgebnisDaten daten,
                                    string reiterKennzahlen, string reiterMonate, string reiterGrafik,
                                    string gruppeMonate, string hilfeSchluessel)
        {
            BlazorDialogForm<BedarfErgebnisDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(daten, reiterKennzahlen, reiterMonate, reiterGrafik, gruppeMonate,
                      hilfeSchluessel))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(),
                    _ => { if (dlg != null) dlg.Schliessen(true); })
            };

            string titel = Text_("BERG_TITEL", "Simulation Ergebnisse");
            if (!string.IsNullOrEmpty(daten.TitelZusatz)) titel += " - " + daten.TitelZusatz;

            dlg = new BlazorDialogForm<BedarfErgebnisDialog>(titel, MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
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
                ["LabelJahresverlauf"] = Text_("BERG_SCH_JAHRESVERLAUF", "Jahresverlauf"),
                // Die drei Kategorien und der Zeitnavigator (Anwenderwunsch W8-E-2).
                ["GruppeLeistung"] = Text_("BERG_GRP_LEISTUNG", "Leistung"),
                ["GruppeEnergie"] = Text_("BERG_GRP_ENERGIE", "Energie"),
                ["StufeJahrText"] = Text_("BERG_STUFE_JAHR", "Jahr"),
                ["StufeWocheText"] = Text_("BERG_STUFE_WOCHE", "Woche"),
                ["StufeTagText"] = Text_("BERG_STUFE_TAG", "Tag"),
                ["MarkeFormat"] = Text_("BERG_GANG_MARKE", "{2} {0} von {1}"),
                ["Monatsnamen"] = Monatsnamen(),
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
        /// <para><b>Das Bild entsteht ZWEIMAL</b>, einmal je Einheit. Ein PNG lässt sich
        /// nicht umrechnen, und die Komponente ruft keinen Renderer (Risiko
        /// R‑W8‑2).</para>
        /// </summary>
        private static Monatssicht Sicht(string bezeichnung, float[] monat, string bildtitel,
                                         SKColor farbe, bool istBrauchwasser = false)
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
            byte[] bild = ChartRenderer.MonatsSaeulen(bildtitel, mwh, farbe,
                                                      Energieeinheit.MWh.Text, monate);
            byte[] bildKWh = ChartRenderer.MonatsSaeulen(bildtitel, kwh, farbe,
                                                         Energieeinheit.KWh.Text, monate);

            return new Monatssicht(bezeichnung, texte, bild, istBrauchwasser)
            {
                Zahlen = mwh,
                QuelleEinheit = Energieeinheit.MWh,
                BildKWh = bildKWh
            };
        }

        /// <summary>Die Formatierung der Vorläufer: <c>ToString("F2")</c> in der Anzeigekultur.</summary>
        private static string F2(double wert) => wert.ToString("F2", CultureInfo.CurrentCulture);

        private static double[] AlsDouble(float[] reihe)
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
    }
}
