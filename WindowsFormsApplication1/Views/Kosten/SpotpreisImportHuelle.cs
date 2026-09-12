using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Spotmarktpreise importieren" (iU9-W3.2).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="SpotpreisImportDialog"/> kennt weder Datei noch Datenbank. Sie
    /// ruft drei Delegaten, und die machen genau das, was
    /// <c>Form_SpotpreisImport</c> tat: den Dateiwähler öffnen
    /// (<c>Dienste.Datei.DateiOeffnen</c> statt <c>OpenFileDialog</c>), die Datei
    /// über <see cref="SpotpreisImportCtrl"/> prüfen und die geprüfte Reihe
    /// schreiben — mit demselben Fortschrittsmelder.</para>
    ///
    /// <para><b>Prüfen und Schreiben laufen auf einem eigenen Faden</b>
    /// (<c>Task.Run</c>, Muster <c>KapitalwertVerlaufHuelle</c>). Der Vorläufer
    /// setzte dafür den Sanduhrzeiger und blockierte den Oberflächenfaden; in
    /// einer WebView stünde damit auch der Dialog still. Der Ablauf bleibt
    /// derselbe: Erst prüfen, dann — und nur bei Erfolg — schreiben.</para>
    ///
    /// <para><b>Der Lauf bleibt zwischen den Schritten stehen.</b> Genau wie das
    /// Feld <c>_lauf</c> der Maske: „Übernehmen" schreibt die Werte, die
    /// „Datei wählen" gelesen und geprüft hat, kein zweites Mal gelesen.</para>
    /// </summary>
    internal static class SpotpreisImportHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 720 × 528 mit
        /// einem 300 px hohen Protokollfeld; die Blazor-Fassung stellt Feld und
        /// Beschriftung übereinander und braucht deshalb mehr Höhe.</summary>
        private static readonly Size FENSTER = new Size(760, 720);

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W4.4). Bis Welle 3 zeigte diese
        /// Hülle ein eigenes Fenster; seit die Energieträgerverwaltung selbst
        /// eine Razor-Komponente ist, erscheint der Import in einer
        /// <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe WebView
        /// (Risiko R2). <c>Geschlossen</c> setzt der Wirt.
        /// </summary>
        /// <param name="idProjekt">Projekt, dem eine Projektreihe gehören würde;
        /// der Schalter „allen Projekten zur Verfügung stellen" schreibt statt
        /// dessen nach Projekt 0.</param>
        internal static IReadOnlyDictionary<string, object> Gaben(int idProjekt)
        {
            PreisreiheCtrl.StelleTabellenSicher();

            var ctrl = new SpotpreisImportCtrl();
            SpotpreisImportCtrl.Lauf lauf = null;

            return new Dictionary<string, object>
            {
                // btnWaehlen_Click: derselbe Filter, dieselbe Prüfung auf Vorhandensein
                // (Dienste.Datei liefert "" bei Abbruch). Der Wähler läuft HINTER dem
                // Blazor-Ereignis (Befund W13‑B‑1, siehe IDateiDienst).
                ["Waehlen"] = new Func<string, Task<string>>(filter =>
                    Dienste.Datei.DateiOeffnenAsync(MyResource.Resource.PREIS_IMPORT_TITEL,
                                                    filter, null)),

                ["Pruefen"] = new Func<string, Task<SpotpreisPruefung>>(pfad => Task.Run(() =>
                {
                    lauf = ctrl.Pruefe(pfad);
                    return new SpotpreisPruefung(
                        lauf.Erfolgreich,
                        (lauf.Protokoll ?? "").Replace("\n", Environment.NewLine),
                        lauf.Jahr);
                })),

                ["Speichern"] = new Func<string, bool, Action<int>, Task<SpotpreisSpeicherung>>(
                    (bezeichner, stamm, fortschritt) => Task.Run(() =>
                    {
                        if (lauf == null || !lauf.Erfolgreich)
                            return new SpotpreisSpeicherung(0, 0);

                        int ziel = stamm ? 0 : idProjekt;
                        int id = ctrl.Speichere(lauf, bezeichner, ziel, fortschritt);
                        if (id <= 0) return new SpotpreisSpeicherung(0, 0);

                        return new SpotpreisSpeicherung(id, lauf.Reihe.StundenreiheCtKwh.Length);
                    })),

                ["TitelText"] = MyResource.Resource.PREIS_IMPORT_TITEL,
                ["InfoText"] = MyResource.Resource.PREIS_IMPORT_INFO,
                ["LabelDatei"] = MyResource.Resource.PREIS_IMPORT_LABEL_DATEI,
                ["WaehlenText"] = MyResource.Resource.PREIS_IMPORT_BTN_DATEI,
                ["Dateifilter"] = MyResource.Resource.PREIS_IMPORT_DATEIFILTER,
                ["LabelBezeichner"] = MyResource.Resource.PREIS_IMPORT_LABEL_BEZEICHNER,
                ["LabelStamm"] = MyResource.Resource.PREIS_IMPORT_CHK_STAMM,
                ["LabelProtokoll"] = MyResource.Resource.PREIS_IMPORT_LABEL_PROTOKOLL,
                ["UebernehmenText"] = MyResource.Resource.PREIS_IMPORT_BTN_UEBERNEHMEN,
                ["SchliessenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,
                ["VorlageBereit"] = MyResource.Resource.PREIS_IMPORT_STATUS_BEREIT,
                ["TextUnbrauchbar"] = MyResource.Resource.PREIS_IMPORT_STATUS_UNBRAUCHBAR,
                ["VorlageSchreibt"] = Schreibvorlage(),
                ["VorlageGespeichert"] = MyResource.Resource.PREIS_IMPORT_STATUS_GESPEICHERT,
                ["TextNichtGespeichert"] = MyResource.Resource.PREIS_IMPORT_STATUS_NICHT_GESPEICHERT,
                ["TextPrueft"] = Text_("SPOT_STATUS_PRUEFT", "Datei wird geprüft …")
            };
        }

        /// <summary>
        /// Die Fortschrittszeile. Der Vorläufer formatierte die Zahl selbst
        /// (<c>geschrieben.ToString("N0")</c>) und setzte sie dann in
        /// <c>PREIS_IMPORT_STATUS_SCHREIBT</c> ein. Die Komponente kennt nur
        /// <c>string.Format</c> mit der Zahl — die Tausendertrennung steht deshalb
        /// hier in der Vorlage.
        /// </summary>
        private static string Schreibvorlage()
        {
            return MyResource.Resource.PREIS_IMPORT_STATUS_SCHREIBT
                       .Replace("{0}", "{0:N0}");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
