using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Wärmepumpen-VERWALTUNG (iU9-W7.5) — der Ersatz für
    /// <c>Form_WPAuswahl</c>.
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Wie bei den fünf
    /// Erzeugerdialogen der Welle 6 gehört die <c>List&lt;WErzeugerModel&gt;</c> dem
    /// Aufrufer; die Hülle bearbeitet sie an Ort und Stelle. Der Assistent reicht sogar
    /// dieselbe Liste über ALLE Erzeugertypen herein — deshalb filtert die Anzeige auf
    /// <c>WP_TYP</c>, und deshalb entfernt „Löschen" die ZEILE und nicht ihren
    /// Anzeigeindex.</para>
    ///
    /// <para><b>Zwei Ebenen Übersetzung.</b> Die Komponente kennt nur
    /// <see cref="WaermepumpeAnlageDaten"/>; der Kern nur <see cref="WErzeugerModel"/>.
    /// Die Hülle hält die Zuordnung in einem Wörterbuch und überträgt beim OK der
    /// Detailansicht zurück — <see cref="WaermepumpeAnlageHuelle.NachModell"/> ist
    /// dieselbe Abbildung, die auch der Fensterweg benutzt.</para>
    /// </summary>
    internal static class WaermepumpenHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Der Vorläufer maß 581 × 299 — mit fünf Spalten,
        /// 44‑px-Zeilen und der Aktionsspalte braucht die Razor-Fassung mehr.
        /// </summary>
        private static readonly Size MASS = new Size(900, 600);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_WP_Click</c> und
        /// <c>Form_Simulation_Detail.listView_SimWP_MouseDown</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<WaermepumpenDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermepumpenDialog>(
                Text_("WPV_TITEL", "Wärmepumpen Verwaltung"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Die Wärmepumpenseite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, List<WErzeugerModel> modelle, bool wizard)
        {
            // Die Anzeige fuehrt nur die WP-Zeilen; das Woerterbuch haelt die
            // Zuordnung zurueck in die geteilte Liste.
            var zeilen = new List<WaermepumpeAnlageDaten>();
            var zuModell = new Dictionary<WaermepumpeAnlageDaten, WErzeugerModel>();

            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != WizardItemClass.WP_TYP) continue;

                // Ä22: Die Stammfelder der Zeile zweistufig nachziehen - sonst zeigte
                // die Liste 0 kW (SetControls:31).
                WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);

                WaermepumpeAnlageDaten daten = WaermepumpeAnlageHuelle.AusModell(m);
                zeilen.Add(daten);
                zuModell[daten] = m;
            }

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                ["Katalog"] = new Func<IReadOnlyList<WaermepumpenKatalogZeile>>(
                    () => new WPStammCtrl().KatalogZeilen()),

                ["AnlageGaben"] = new Func<WaermepumpeAnlageDaten, IReadOnlyDictionary<string, object>>(
                    daten => WaermepumpeAnlageHuelle.Gaben(
                        besitzer, daten, Modell(zuModell, modelle, projektId, daten), projektId)),

                ["Anlegen"] = new Func<string, WaermepumpeAnlageDaten>(
                    bezeichner => Anlegen(zuModell, projektId, bezeichner)),

                ["Uebernehmen"] = new Action<WaermepumpeAnlageDaten>(
                    daten =>
                    {
                        WErzeugerModel m = Modell(zuModell, modelle, projektId, daten);
                        WaermepumpeAnlageHuelle.NachModell(daten, m);
                        if (!modelle.Contains(m))
                        {
                            m.ID_Type = WizardItemClass.WP_TYP;
                            m.ID_Projekt = projektId;
                            modelle.Add(m);
                        }
                    }),

                ["Entfernen"] = new Action<WaermepumpeAnlageDaten>(
                    daten =>
                    {
                        if (!zuModell.TryGetValue(daten, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(daten);
                    }),

                ["TitelText"] = Text_("WPV_TITEL", "Wärmepumpen Verwaltung"),
                ["KopfbandText"] = Text_("WPV_KOPFBAND", "Geben Sie die Daten der Wärmepumpe ein"),
                ["TitelDetail"] = Text_("WPA_TITEL", "Detailansicht"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["SpalteLeistung"] = Text_("WPV_SP_LEISTUNG", "Leistung [kW]"),
                ["SpalteVorlauf"] = Text_("WPV_SP_VORLAUF", "Vorlauf [°C]"),
                ["SpalteRuecklauf"] = Text_("WPV_SP_RUECKLAUF", "Rücklauf [°C]"),
                ["SpalteBetriebsart"] = Text_("WPA_LBL_BETRIEBSART", "Betriebsart"),
                ["SpalteAktion"] = Text_("WPV_SP_AKTION", "Aktion"),
                ["BtnNeuText"] = Text_("WPV_BTN_NEU", "➕ Neu.."),
                ["BtnAendernText"] = Text_("WPV_BTN_AENDERN", "✏️ Ändern.."),
                ["BtnLoeschenText"] = Text_("WPV_BTN_LOESCHEN", "🗑️ Löschen"),
                ["BtnAnsichtText"] = Text_("WPV_BTN_ANSICHT", "Ansicht"),
                ["BtnKatalogText"] = Text_("WPK_BTN_KATALOG", "📋  Modul-Katalog..."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Das Modell zu einem Feldsatz. Ein Satz aus <see cref="Anlegen"/> steht schon
        /// im Wörterbuch, aber noch NICHT in der geteilten Liste — das holt erst
        /// „Übernehmen" nach, wenn der Anwender die Detailansicht mit OK schließt.
        /// </summary>
        private static WErzeugerModel Modell(
            Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
            List<WErzeugerModel> modelle, int projektId, WaermepumpeAnlageDaten daten)
        {
            if (zuModell.TryGetValue(daten, out WErzeugerModel vorhanden)) return vorhanden;

            var neu = new WErzeugerModel { ID_Type = WizardItemClass.WP_TYP, ID_Projekt = projektId };
            zuModell[daten] = neu;
            return neu;
        }

        /// <summary>
        /// „Neu..": Aus der Katalogwahl entsteht eine Zeile mit der STAMM-Id und den
        /// Stammfeldern — <c>btn_Neu_Click</c>:260 tat dasselbe über
        /// <c>GeraetedatenFuellen</c>. In die Anzeige kommt sie erst, wenn die
        /// Detailansicht mit OK schließt (der Vorläufer prüfte dafür <c>CloseWithOK</c>).
        /// </summary>
        private static WaermepumpeAnlageDaten Anlegen(
            Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
            int projektId, string bezeichner)
        {
            var modell = new WErzeugerModel
            {
                Bezeichner = bezeichner ?? "",
                ID_Type = WizardItemClass.WP_TYP,
                ID_Projekt = projektId,
                ID_WP = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", bezeichner)
            };
            WaermepumpeGeraeteCtrl.GeraetedatenFuellen(modell, modell.ID_WP);

            // W6-E-4 (06.09.2026): Die Waermepumpe hat keine Katalogtemperaturen - ihr
            // "Katalog" sind die Vorlaufstufen der Kennlinien. Die kleinste steht als
            // Vorschlag im Vorlauffeld, sobald die Detailansicht aufgeht; der Ruecklauf
            // bleibt leer (fuer ihn gibt es keine eindeutige Regel, siehe
            // AnlagenTemperaturen.VorlaufAusKennlinien).
            AnlagenTemperaturen.VorlaufAusKennlinien(modell);

            WaermepumpeAnlageDaten daten = WaermepumpeAnlageHuelle.AusModell(modell);
            zuModell[daten] = modell;
            return daten;
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
