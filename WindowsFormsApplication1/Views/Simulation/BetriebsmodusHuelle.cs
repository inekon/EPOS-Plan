using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>BetriebsmodusDialog</c> (iU9-W10a.1) — der Ersatz für
    /// <c>Form_Betriebsmodus</c>.
    ///
    /// <para><b>Keine Datenbank, keine Delegaten.</b> Der Vorläufer las nichts und
    /// schrieb nichts; er bekam Bezeichner und bisherigen <c>BM_Typ</c> und lieferte
    /// den gewählten zurück. Geschrieben wird beim Aufrufer
    /// (<c>Form_Simulation_Config.BetriebsmodusBearbeiten</c>), und dort bleiben auch
    /// die Vorprüfung „nur für Wärmepumpen", der PV-Hinweis und das Auffrischen der
    /// Übersicht.</para>
    ///
    /// <para><b>Die drei Steuerwerte kommen von hier.</b> Die Komponente darf die
    /// Kernklassen nicht kennen; <c>WaermequelleClass.MODUS_*</c> geht deshalb als
    /// Parameter hinein und kommt als Ergebnis zurück — die Zuordnung Listenplatz ↔
    /// Steuerwert bleibt damit an EINER Stelle, so wie die
    /// <c>SchluesselEintrag</c>-Regel aus Paket Q1 es verlangt.</para>
    ///
    /// <para><b>Ab W10b wird daraus eine Überlagerung.</b> Dann ist die
    /// Simulationskonfiguration selbst eine Razor-Seite, und der Dialog erscheint
    /// darin statt in einem zweiten Fenster (Risiko R2). Bis dahin trägt diese Datei
    /// den einen Aufrufweg.</para>
    /// </summary>
    internal static class BetriebsmodusHuelle
    {
        // iU9-W10b.1: Der FENSTERWEG dieser Huelle ist entfallen. Ihr einziger
        // Aufrufer war Form_Simulation_Config; seit die Simulationskonfiguration
        // selbst eine Razor-Seite ist, erscheint der Dialog als UEBERLAGERUNG in
        // ihrem Fenster (Risiko R2 - nie zwei WebViews uebereinander). Was bleibt,
        // ist der PARAMETERSATZ unten: Er war von Anfang an fuer genau diesen Tag
        // getrennt gehalten (W10a, "Gaben ohne Geschlossen").

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn ab W10b
        /// auch die Überlagerung in der Simulationsseite nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(string bezeichner,
                                                                  string aktuellerModus)
        {
            return new Dictionary<string, object>
            {
                ["Bezeichner"] = bezeichner ?? "",
                ["AktuellerModus"] = aktuellerModus ?? "",

                // Die Steuerwerte der Persistenz - nie ein Anzeigetext.
                ["SteuerwertLaufzeit"] = WaermequelleClass.MODUS_LAUFZEIT,
                ["SteuerwertLeistung"] = WaermequelleClass.MODUS_LEISTUNG,
                ["SteuerwertPv"] = WaermequelleClass.MODUS_PV,

                ["TitelText"] = MyResource.Resource.SIM_BETRIEBSMODUS_FENSTERTITEL,
                ["KopfText"] = MyResource.Resource.SIM_BETRIEBSMODUS_KOPF,
                ["RbLaufzeit"] = MyResource.Resource.SIM_BM_RB_LAUFZEIT,
                ["TextLaufzeit"] = MyResource.Resource.SIM_BM_TEXT_LAUFZEIT,
                ["RbLeistung"] = MyResource.Resource.SIM_BM_RB_LEISTUNG,
                ["TextLeistung"] = MyResource.Resource.SIM_BM_TEXT_LEISTUNG,
                ["RbPv"] = MyResource.Resource.SIM_BM_RB_PV,
                ["TextPv"] = MyResource.Resource.SIM_BM_TEXT_PV,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,
                ["HilfeSchluessel"] = "Form_Betriebsmodus.btn_Help"
            };
        }
    }
}
