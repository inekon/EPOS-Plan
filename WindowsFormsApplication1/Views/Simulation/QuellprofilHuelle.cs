using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>QuellprofilDialog</c> (iU9-W10a.6) — der Ersatz für
    /// <c>Form_Quellprofil</c>.
    ///
    /// <para><b>Der Dialog SPEICHERT selbst</b> (Vorläufer <c>btnOk_Click</c>:1020), und
    /// zwar über <c>QuellprofilCtrl.Speichern</c>. Die Hülle liefert dafür den
    /// Delegaten; herauskommt die Id, die der Aufrufer an <c>WQ_ID_Quellprofil</c>
    /// schreibt.</para>
    ///
    /// <para><b>Die Dateiwahl gehört der Plattform.</b> <c>CsvLesen</c> öffnet den
    /// Wähler über <c>Dienste.Datei</c> und liest die Werte mit
    /// <c>WaermequelleClass.WerteAusCsv</c> — beides kennt die Komponente nicht.
    /// <c>null</c> heißt „nichts gelesen"; ein ABGEBROCHENER Wähler liefert dagegen ein
    /// leeres Feld, damit der Dialog nicht meldet, wo nichts schiefging.</para>
    /// </summary>
    internal static class QuellprofilHuelle
    {
        // iU9-W10b.1: Der FENSTERWEG dieser Huelle ist entfallen. Ihr einziger
        // Aufrufer war Form_Simulation_Config; seit die Simulationskonfiguration
        // selbst eine Razor-Seite ist, erscheint der Dialog als UEBERLAGERUNG in
        // ihrem Fenster (Risiko R2 - nie zwei WebViews uebereinander). Was bleibt,
        // ist der PARAMETERSATZ unten: Er war von Anfang an fuer genau diesen Tag
        // getrennt gehalten (W10a, "Gaben ohne Geschlossen").

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(QuellprofilDaten daten)
        {
            int idProjekt = daten?.IdProjekt ?? 0;

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Profile"] = Profile(idProjekt),
                ["ProfilLesen"] = new Func<int, QuellprofilInhalt>(ProfilLesen),
                ["Speichern"] = new Func<QuellprofilInhalt, int>(k => Speichern(idProjekt, k)),
                ["CsvLesen"] = new Func<int, Task<double[]>>(CsvLesen),
                ["Jahresbild"] = Bildzeichner(daten),
                ["Werteanzahl"] = new Func<string, int>(DbWerte.QuellprofilWerteanzahl),

                // Die drei Steuerwerte der Betriebsart - sprachneutral, nie ein
                // Anzeigetext (Drei-Schichten-Regel).
                ["Betriebsarten"] = new[]
                {
                    DbWerte.WQ_PROFIL_BETRIEBSART_MONAT,
                    DbWerte.WQ_PROFIL_BETRIEBSART_TAG,
                    DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE
                },
                ["BetriebsartTexte"] = new[]
                {
                    MyResource.Resource.SIMQ_QUELLPROFIL_BA_MONAT,
                    MyResource.Resource.SIMQ_QUELLPROFIL_BA_TAG,
                    MyResource.Resource.SIMQ_QUELLPROFIL_BA_STUNDE
                },
                ["Monatsnamen"] = Monatsnamen(),
                ["Wochentage"] = Wochentage(),
                ["VorgabeWert"] = QuellprofilCtrl.VORGABE_MONATSWERT,

                ["TitelText"] = MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                ["TitelMitWp"] = MyResource.Resource.SIMQ_QUELLPROFIL_TITEL_MIT_WP,
                ["InfoText"] = MyResource.Resource.SIMQ_QUELLPROFIL_INFO,
                ["LabelProfil"] = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_PROFIL,
                ["ProfilNeu"] = MyResource.Resource.SIMQ_QUELLPROFIL_NEU,
                ["LabelBetriebsart"] = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BETRIEBSART,
                ["LabelBezeichner"] = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BEZEICHNER,
                ["LabelBeschreibung"] = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BESCHREIBUNG,
                ["LabelWochentag"] = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_WOCHENTAG,
                ["TabMonatswerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_MONATSWERTE,
                ["TabWochenwerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_WOCHENWERTE,
                ["TabTageswerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_TAGESWERTE,
                ["TabStundenwerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_STUNDENWERTE,
                ["TabGrafik"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_GRAFIK,
                ["KopfMonat"] = MyResource.Resource.SIMQ_QUELLPROFIL_KOPF_MONAT,
                ["HinweisAltweg"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_ALTWEG),
                ["HinweisTag"] = MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_TAG,
                ["HinweisStunde"] = MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_STUNDE,
                ["SpalteNr"] = MyResource.Resource.SIMQ_QUELLPROFIL_SPALTE_NR,
                ["SpalteWert"] = MyResource.Resource.SIMQ_QUELLPROFIL_SPALTE_WERT,
                ["BtnAlleMonate"] = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_MONATE,
                ["BtnAlleWerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_WERTE,
                ["BtnCsv"] = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_CSV,
                ["AlleWerteText"] = MyResource.Resource.SIMQ_QUELLPROFIL_ALLE_WERTE_TEXT,
                ["InfoWerte"] = MyResource.Resource.SIMQ_QUELLPROFIL_INFO_WERTE,
                ["CsvHinweis"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_QUELLPROFIL_CSV_HINWEIS),
                ["BildAlt"] = MyResource.Resource.SIMQ_QUELLPROFIL_TAB_GRAFIK,
                ["PlatzhalterText"] = MyResource.Resource.SIMQ_ERDREICH_BILD_PLATZHALTER,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,

                ["MsgMonatUngueltig"] = MyResource.Resource.SIMQ_QUELLPROFIL_MSG_MONAT_UNGUELTIG,
                ["MsgWerteFehlen"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_WERTE_FEHLEN),
                ["MsgBezeichner"] = MyResource.Resource.SIMQ_QUELLPROFIL_MSG_BEZEICHNER,
                ["MsgSpeichern"] = MyResource.Resource.SIMQ_QUELLPROFIL_MSG_SPEICHERN,
                ["MsgCsvFehler"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_CSV_FEHLER),

                ["HilfeSchluessel"] = "Form_Quellprofil.btn_Help"
            };
        }

        /// <summary>Die Quellprofile des Projekts (<c>ProfillisteLaden</c>:695-712).</summary>
        private static IReadOnlyList<QuellprofilZeile> Profile(int idProjekt)
        {
            var l = new List<QuellprofilZeile>();
            foreach (QuellprofilCtrl.Kopf k in QuellprofilCtrl.LesenJeProjekt(idProjekt))
                l.Add(new QuellprofilZeile(k.ID, k.ToString()));
            return l;
        }

        /// <summary>Kopf und Werte eines Profils (<c>ProfilUebernehmen</c>:737-763).</summary>
        private static QuellprofilInhalt ProfilLesen(int idProfil)
        {
            QuellprofilCtrl.Kopf k = QuellprofilCtrl.Lesen(idProfil);
            if (k == null) return null;

            return new QuellprofilInhalt(k.Bezeichner, k.Beschreibung, k.Betriebsart,
                                         QuellprofilCtrl.WerteLesen(idProfil));
        }

        /// <summary>
        /// Speichert Kopf und Werte. Die Einheit ist fest <c>°C</c> — die Spalte
        /// dokumentiert, sie steuert nicht (§ 8.1).
        /// </summary>
        private static int Speichern(int idProjekt, QuellprofilInhalt inhalt)
        {
            if (inhalt == null) return 0;

            var kopf = new QuellprofilCtrl.Kopf
            {
                ID = 0,
                ID_Projekt = idProjekt,
                Bezeichner = inhalt.Bezeichner,
                Betriebsart = inhalt.Betriebsart,
                Einheit = QuellprofilCtrl.EINHEIT_GRAD_CELSIUS,
                Beschreibung = inhalt.Beschreibung
            };

            return QuellprofilCtrl.Speichern(kopf, inhalt.Werte);
        }

        /// <summary>
        /// Der CSV-Weg: Dateiwahl über <c>Dienste.Datei</c>, danach
        /// <c>WaermequelleClass.WerteAusCsv</c>.
        ///
        /// <para>Ein ABGEBROCHENER Wähler liefert ein LEERES Feld, kein <c>null</c>: Der
        /// Dialog soll nicht melden, wo nichts schiefgegangen ist — der Vorläufer kehrte
        /// dort ebenfalls still zurück (<c>btnCsv_Click</c>:896).</para>
        /// </summary>
        private static Task<double[]> CsvLesen(int soll)
        {
            string pfad = Dienste.Datei.DateiOeffnen(
                MyResource.Resource.SIMQ_CSV_DATEIDIALOG_TITEL,
                MyResource.Resource.SIMQ_CSV_DATEIFILTER, null);

            if (string.IsNullOrEmpty(pfad)) return Task.FromResult(new double[0]);

            return Task.Run(() => WaermequelleClass.WerteAusCsv(pfad, soll));
        }

        /// <summary>
        /// Der Delegat <c>Jahresbild</c>: Betriebsart und Werte hinein, ein PNG heraus.
        /// Gezeichnet wird auf einem eigenen Faden — 8 760 Punkte.
        ///
        /// <para><b>Der ALTWEG hat Vorrang, solange kein Profil gespeichert ist</b>
        /// (<c>ChartAktualisieren</c>:1054-1056): Trägt die Anlage noch einen Wochengang,
        /// zeigt die Grafik das aus Monats- und Wochenwerten konstruierte Profil.</para>
        /// </summary>
        private static Func<string, double[], Task<byte[]>> Bildzeichner(QuellprofilDaten daten)
        {
            return (betriebsart, werte) => Task.Run(() =>
            {
                float[] profil = QuellprofilCtrl.Jahresprofil(betriebsart, werte);
                if (profil == null) return null;

                var jahr = new double[profil.Length];
                for (int i = 0; i < profil.Length; i++) jahr[i] = profil[i];

                return ChartRenderer.Jahresverlauf(
                    MyResource.Resource.SIMQ_QUELLPROFIL_TAB_GRAFIK, jahr,
                    MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR,
                    SkiaSharp.SKColors.SteelBlue);
            });
        }

        /// <summary>Die zwölf Monatsnamen der eingestellten Sprache.</summary>
        private static string[] Monatsnamen()
        {
            string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
            var l = new string[12];
            for (int m = 0; m < 12; m++) l[m] = m < namen.Length ? namen[m] : "";
            return l;
        }

        /// <summary>Die sieben Wochentage AB MONTAG (<c>Form_Quellprofil</c>:74-83).</summary>
        private static string[] Wochentage()
        {
            string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.DayNames;
            var l = new string[7];
            for (int t = 0; t < 7; t++) l[t] = namen[(t + 1) % 7];
            return l;
        }
    }
}
