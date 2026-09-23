using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE von <c>GebaeudeWohnflaecheDialog</c> — dem Skalierungsdialog des
    /// Gebäudedialogs (Stufe G1; Umsetzungskonzept Gebäudesimulation 2.7, 2.8). Seit G1 in
    /// <c>EPOS.UI.Daten</c>: Der Dialog erscheint allein als Überlagerung im Gebäudedialog;
    /// das eigene Fenster der Windows-Schale hatte keinen Aufrufer mehr und ist entfallen.
    ///
    /// <para><b>Keine Datenbank, keine Delegaten.</b> Die Hülle reicht Werte hinein und
    /// nimmt einen Ergebnis-Record entgegen. Die vier Größen des Dialogs liest JEDER
    /// Rechenweg — aus Fläche bzw. Verbrauch entsteht der Skalierungsfaktor nach E8.</para>
    /// </summary>
    internal static class GebaeudeWohnflaecheHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn ab W9.2 auch
        /// die Überlagerung in <c>GebaeudeDialog</c> nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            Z_ProjGebModel zeile, string baujahrText)
        {
            return new Dictionary<string, object>
            {
                ["Gebaeudename"] = zeile?.Gebaeudename ?? "",
                ["Beschreibung"] = zeile?.Beschreibung ?? "",
                ["Gebaeudeart"] = zeile?.Gebaeudeart ?? "",
                ["Baujahr"] = baujahrText ?? "",
                ["Wert"] = zeile?.Wohnflaeche ?? 0.0,
                ["Jahresnutzungsgrad"] = zeile?.Jahresnutzungsgrad ?? 0.0,
                ["Einheit"] = zeile?.Einheit ?? "",
                ["DezentralWarmwasser"] = zeile != null && zeile.DezentralWarmwasser,

                ["Bedarfsarten"] = Bedarfsarten(),

                ["TitelText"] = Titel(),
                ["GruppeInfo"] = Text_("GEBW_GRP_INFO", "Info ausgewähltes Gebäude"),
                ["GruppeEingabe"] = Text_("GEBW_GRP_EINGABE", "Eingabe für das ausgewählte Gebäude"),
                ["LabelGebaeudeart"] = Text_("GEBW_LBL_GEBAEUDEART", "Gebäudeart:"),
                ["LabelGebaeudename"] = Text_("GEBW_LBL_GEBAEUDENAME", "Gebäudename:"),
                ["LabelBeschreibung"] = Text_("GEBW_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelBaujahr"] = Text_("GEBW_LBL_BAUJAHR", "Baujahr:"),
                ["LabelBedarfsart"] = Text_("GEBW_LBL_BEDARFSART", "Bedarfsart:"),
                ["LabelArtDerAngabe"] = Text_("GEBW_LBL_ART_ANGABE", "Art der Angabe:"),
                ["LabelVerbrauch"] = Text_("GEBW_LBL_VERBRAUCH", "Wärmebedarf/Wohnfläche:"),
                ["LabelJahresnutzungsgrad"] = Text_("GEBW_LBL_NUTZUNGSGRAD", "Jahresnutzungsgrad:"),
                ["LabelDezentralWarmwasser"] =
                    Text_("GEBW_LBL_DEZ_WARMWASSER", "Dezentrale Warmwasserbereitung"),
                ["HinweisSkalierung"] = Text_("GEBW_HINWEIS_SKALIERUNG",
                    "Aus Nutzfläche bzw. Verbrauch entsteht der Skalierungsfaktor der " +
                    "Gebäuderechnung; jeder Rechenweg liest diese Angaben."),
                ["HinweisJahresnutzungsgrad"] = Text_("GEBW_HINWEIS_NUTZUNGSGRAD",
                    "Bei Brennstoffangaben bitte Heizkessel Jahresnutzungsgrad eingeben: " +
                    "z.B. 0.85 für 85%"),
                ["MeldungZahlFehlt"] = Text_("GEBW_MSG_ZAHL", "Bitte {0} als Zahl eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["HilfeSchluessel"] = "Form_GebWohnflaeche.btn_Help"
            };
        }

        /// <summary>
        /// Die sechs Bedarfsarten. <b>Sie sind zugleich Steuerwerte</b> — die Zeichenkette
        /// landet in <c>Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche</c> und wird
        /// beim nächsten Öffnen wieder mit der Liste verglichen. Deshalb stehen sie
        /// wörtlich wie im Vorläufer da, samt der beiden Leerzeichen in
        /// „Verbrauch  [MWh/a]", und sind NICHT übersetzt (Drei-Schichten-Regel,
        /// Persistenzschicht).
        /// </summary>
        internal static string[] Bedarfsarten()
        {
            return new[]
            {
                "Ölverbrauch [l/a]",
                "Gasverbrauch [m³/a]",
                "Gasverbrauch [MWh/a] (Ho)",
                "Brennstoffverbrauch [MWh/a]",
                "Verbrauch  [MWh/a]",
                "Wohnfläche [m²]"
            };
        }

        internal static string Titel()
        {
            return Text_("GEBW_TITEL",
                "Eingabe der gesamten Wohn-/Nutzfläche des ausgewählten Gebäudes");
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
