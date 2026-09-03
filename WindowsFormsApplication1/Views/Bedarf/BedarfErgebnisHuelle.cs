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
    /// <para><b>Die Zahlen entstehen HIER, nicht in der Komponente</b>, und zwar wörtlich
    /// je Ausprägung: <c>ToString("F2")</c> wie in den <c>Init</c>-Methoden, samt dem
    /// Teiler 1000 beim Brauchwasser, den NUR die Brauchwassermaske hat (Befund W8-B4,
    /// siehe unten).</para>
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
            var daten = new BedarfErgebnisDaten
            {
                Sicht = ErgebnisSicht.Strom,
                MitBrauchwasser = false,
                StartReiter = startReiter,
                Kennzahlen = new[]
                {
                    // Reihenfolge und Beschriftungen aus dem Designer (Karte, tabPage1).
                    new ErgebnisKennzahl(Text_("BERG_LBL_MAX_STROM", "max. Strombedarf:"),
                                 F2(simulation.Strombedarf_Max), EINHEIT_KW),
                    new ErgebnisKennzahl(Text_("BERG_LBL_STROM_GESAMT", "Gesamter Strombedarf:"),
                                 F2(simulation.Strombedarf_gesamt), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_STROMGANGLINIE", "Stromganglinie:"),
                                 F2(simulation.Stromganglinie_gesamt), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_STROM_GEBAEUDE", "Strombedarf Gebäude:"),
                                 F2(simulation.Strombedarf_Gebaeude_gesamt), EINHEIT_MWH)
                },
                Sichten = new[]
                {
                    Sicht(Text_("BERG_OPT_STROM", "Strombedarf"), simulation.Strombedarf_monat,
                          Text_("BERG_BILD_STROM", "Strombedarf Monatsübersicht"), FARBE_STROM)
                }
            };

            Oeffnen(besitzer, daten,
                    Text_("BERG_REITER_STROM_ERG", "Strombedarf Ergebnisse"),
                    Text_("BERG_REITER_STROM_MONAT", "Strombedarf monatlich"),
                    Text_("BERG_REITER_STROM_GRAFIK", "Grafik Strombedarf"),
                    Text_("BERG_GRP_STROM_MONAT", "Strombedarf monatlicher Verlauf:"),
                    "Form_ErgStromverbraucher.btn_Help");
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
            // BEFUND W8-B4: Der Brauchwasserwert wird NUR in der Brauchwassermaske durch
            // 1000 geteilt (Form_ErgBrauchwasserwaerme.Init:36 gegen
            // Form_ErgProzesswaerme.Init:32 ohne Teiler). Woertlich uebernommen; die Frage
            // an den Anwender steht im Protokoll.
            double brauchwasser = mitBrauchwasser
                ? simulation.Waermebedarf_Brauchwasser / 1000
                : simulation.Waermebedarf_Brauchwasser;

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

            var daten = new BedarfErgebnisDaten
            {
                Sicht = ErgebnisSicht.Waerme,
                MitBrauchwasser = mitBrauchwasser,
                StartReiter = startReiter,
                TitelZusatz = titelZusatz ?? "",
                JahresverlaufBild = jahresbild,
                Kennzahlen = new[]
                {
                    // Reihenfolge und Beschriftungen aus dem Designer (Karte, tabPage1).
                    new ErgebnisKennzahl(Text_("BERG_LBL_NETZVERLUSTE", "Netzverluste:"),
                                 F2(simulation.Waermebedarf_Netzverluste), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_WAERME_GESAMT", "Gesamter Wärmebedarf:"),
                                 F2(simulation.Waermebedarf_Gesamt), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_WAERME_EXTERN", "Externer Wärmebedarf:"),
                                 F2(simulation.Waermebedarf_Extern_Gesamt), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_WAERME_PROZESS", "Wärmebedarf Prozess:"),
                                 F2(simulation.Waermebedarf_Prozess), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_WAERME_GEBAEUDE", "Wärmebedarf Gebäude:"),
                                 F2(simulation.Waermebedarf_Gebaeude_Gesamt), EINHEIT_MWH),
                    new ErgebnisKennzahl(Text_("BERG_LBL_MAX_WAERMELAST", "max. Wärmelast:"),
                                 F2(simulation.Waermebedarf_Max), EINHEIT_KW),
                    new ErgebnisKennzahl(mitBrauchwasser
                                     ? Text_("BERG_LBL_WAERME_BRAUCHWASSER", "Wärmebedarf Brauchwasser:")
                                     : Text_("BERG_LBL_DAVON_BRAUCHWASSER", "davon Brauchwasser:"),
                                 F2(brauchwasser), EINHEIT_MWH)
                },
                Sichten = sichten
            };

            Oeffnen(besitzer, daten,
                    Text_("BERG_REITER_WAERME_ERG", "Wärmebedarf Ergebnisse"),
                    Text_("BERG_REITER_MONAT", "Übersicht monatlich"),
                    Text_("BERG_REITER_GRAFIK", "Grafik"),
                    Text_("BERG_GRP_MONAT", "monatlicher Verlauf:"),
                    mitBrauchwasser ? "Form_ErgBrauchwasserwaerme.btn_Help"
                                    : "Form_ErgProzesswaerme.btn_Help");
        }

        // =================================================================================

        private const string EINHEIT_MWH = "MWh";
        private const string EINHEIT_KW = "kW";

        private static void Oeffnen(IWin32Window besitzer, BedarfErgebnisDaten daten,
                                    string reiterKennzahlen, string reiterMonate, string reiterGrafik,
                                    string gruppeMonate, string hilfeSchluessel)
        {
            BlazorDialogForm<BedarfErgebnisDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["TitelText"] = Text_("BERG_TITEL", "Simulation Ergebnisse"),
                ["ReiterKennzahlen"] = reiterKennzahlen,
                ["ReiterMonate"] = reiterMonate,
                ["ReiterGrafik"] = reiterGrafik,
                ["GruppeMonate"] = gruppeMonate,
                ["EinheitMonat"] = EINHEIT_MWH,
                ["LabelJahresverlauf"] = Text_("BERG_SCH_JAHRESVERLAUF", "Jahresverlauf"),
                ["Monatsnamen"] = Monatsnamen(),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["HilfeSchluessel"] = hilfeSchluessel,
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
        /// Eine Monatssicht: die zwölf Werte als <c>F2</c>-Texte und das fertige Säulenbild.
        /// Eine fehlende Reihe bleibt <c>null</c> und zeigt „—" statt zwölf Nullen.
        /// </summary>
        private static Monatssicht Sicht(string bezeichnung, float[] monat, string bildtitel,
                                         SKColor farbe, bool istBrauchwasser = false)
        {
            if (monat == null || monat.Length < 12)
                return new Monatssicht(bezeichnung, null, null, istBrauchwasser);

            var texte = new string[12];
            var zahlen = new double[12];
            for (int m = 0; m < 12; m++)
            {
                texte[m] = F2(monat[m]);
                zahlen[m] = monat[m];
            }

            byte[] bild = ChartRenderer.MonatsSaeulen(bildtitel, zahlen, farbe, EINHEIT_MWH, MonateKurz());
            return new Monatssicht(bezeichnung, texte, bild, istBrauchwasser);
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
