using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>GebaeudeWohnflaecheDialog</c> (iU9-W9.3) — der Ersatz für
    /// <c>Form_GebWohnflaeche</c>.
    ///
    /// <para><b>Keine Datenbank, keine Delegaten.</b> Der Vorläufer las nichts und schrieb
    /// nichts; er bekam ein <c>Z_ProjGebModel</c> in die Hand, zeigte es an und legte drei
    /// Werte zurück. Die Hülle reicht deshalb nur Werte hinein und nimmt einen
    /// Ergebnis-Record entgegen.</para>
    ///
    /// <para><b>Sie ist ein Zwischenschritt.</b> Mit W9.2 wird der Dialog eine
    /// ÜBERLAGERUNG in <c>GebaeudeDialog</c> — dann gibt es kein zweites Fenster mehr
    /// (Risiko R2), und diese Datei entfällt. Bis dahin trägt sie den einen Aufrufweg
    /// <c>Form_Gebaeude.btn_Aendern_Click</c>.</para>
    /// </summary>
    internal static class GebaeudeWohnflaecheHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 849 × 478).</summary>
        private static readonly Size MASS = new Size(880, 640);

        /// <summary>
        /// Zeigt den Dialog. Rückgabe <c>null</c>, wenn abgebrochen wurde.
        /// </summary>
        internal static GebaeudeWohnflaecheErgebnis Oeffnen(
            IWin32Window besitzer, Z_ProjGebModel zeile, string baujahrText)
        {
            GebaeudeWohnflaecheErgebnis ergebnis = null;
            BlazorDialogForm<GebaeudeWohnflaecheDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(zeile, baujahrText))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<GebaeudeWohnflaecheErgebnis>(
                    new object(), e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null);
                    })
            };

            dlg = new BlazorDialogForm<GebaeudeWohnflaecheDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

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

        private static string Titel()
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
