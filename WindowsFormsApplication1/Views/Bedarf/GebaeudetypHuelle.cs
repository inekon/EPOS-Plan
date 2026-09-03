using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Gebäudetypen-Verwaltung (iU9-W8.4) — sie löst
    /// <c>Form_EingGebTyp</c> ab.
    ///
    /// <para><b>Kopf und Detail.</b> Ein Gebäudetyp ist ein Satz in
    /// <c>Tab_DBTagV_STAMM</c> und fünf oder acht Tageskurven zu je 24 Zeilen in
    /// <c>Tab_DBTagVDaten_STAMM</c>. Die ganze Datenseite liegt seit iU9-W8.0d in
    /// <see cref="TagVCtrl"/>; hier steht nur noch die Abbildung.</para>
    ///
    /// <para><b>Die Kurvennamen kommen aus dem Kern</b> (<c>TagVCtrl.KurvenNamen</c>),
    /// weil dort auch die Entscheidung fällt: fünf Namen bei bis zu fünf Kurven, sonst
    /// acht — nach der KURVENZAHL, nicht nach der Listenposition.</para>
    /// </summary>
    internal static class GebaeudetypHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 671 × 680).</summary>
        private static readonly Size MASS = new Size(960, 760);

        /// <summary>
        /// Öffnet die Verwaltung. Rückgabe <c>true</c>, wenn mit OK geschlossen wurde —
        /// <c>WinFormsNavigation</c> reicht das als <c>MitOk</c> weiter.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<GebaeudetypDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GebaeudetypDialog>(
                Text_("GTYP_TITEL", "Gebäudetypen Verwaltung"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn seit iU9-W9.2
        /// auch die Überlagerung in <c>GebaeudeDialog</c> nehmen kann (Risiko R2: kein
        /// zweites Fenster über einem Blazor-Dialog).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = new GebaeudetypDaten(),
                ["Typen"] = new Func<IReadOnlyList<string>>(() => TagVCtrl.Typen()),
                ["Lies"] = new Func<string, GebaeudetypDaten>(Lesen),
                ["Speichern"] = new Func<int, double[,], bool>(TagVCtrl.Speichern),
                ["Anlegen"] = new Func<string, string, int>(TagVCtrl.Anlegen),
                ["Loeschen"] = new Func<int, bool>(TagVCtrl.Loeschen),
                ["Bild"] = new Func<double[], byte[]>(Tagesbild),

                ["TitelText"] = Text_("GTYP_TITEL", "Gebäudetypen Verwaltung"),
                ["LabelName"] = Text_("GTYP_LBL_NAME", "Name:"),
                ["LabelBeschreibung"] = Text_("GTYP_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelKurve"] = Text_("GTYP_LBL_KURVE", "Kurvenverlauf für den Tag:"),
                ["GruppeStunden"] = Text_("GTYP_GRP_STUNDEN", "Stundenwerteeingabe [kW, kWh oder %]"),
                ["Feldnamen"] = Feldnamen(),

                ["BtnNeuText"] = Text_("GTYP_BTN_NEU", "Typ hinzufügen"),
                ["BtnLoeschenText"] = Text_("GTYP_BTN_LOESCHEN", "Typ Löschen"),
                ["BtnSpeichernText"] = Text_("GTYP_BTN_SPEICHERN", "Typ Speichern"),
                ["BtnSchliessenText"] = MyResource.Resource.ALLG_BTN_OK,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["MeldungZahlFehlt"] = Text_("BTYP_MSG_ZAHL", "Bitte {0} als Zahl eingeben."),
                ["MeldungNameFehlt"] = Text_("BTYP_MSG_NAME_LEER", "Bitte einen Namen eingeben!"),
                ["MeldungNameBelegt"] = Text_("BTYP_MSG_NAME_BELEGT", "Name existiert bereits!"),
                ["MeldungGespeichert"] = Text_("BTYP_MSG_GESPEICHERT", "Daten gespeichert!"),
                ["MeldungFehler"] = Text_("GTYP_MSG_FEHLER", "Speichern nicht möglich!"),
                ["FrageLoeschen"] = Text_("BPRO_FRAGE_LOESCHEN", "Soll {0} wirklich gelöscht werden ?"),
                ["HinweisGesperrt"] = Text_("GTYP_MSG_GESPERRT",
                    "Die vom Softwarehersteller gelieferten Gebäudetypen können nicht geändert werden"),

                ["HilfeSchluessel"] = "Form_EingGebTyp.btn_Help"
            };
        }

        // =================================================================================

        /// <summary>Kopf, Kurvennamen und Verteilung eines Gebäudetyps.</summary>
        private static GebaeudetypDaten Lesen(string name)
        {
            var gelesen = TagVCtrl.Lies(name);
            if (gelesen == null) return null;

            TagVModel kopf = gelesen.Value.Kopf;
            return new GebaeudetypDaten
            {
                Id = kopf.ID,
                Name = kopf.Name ?? "",
                Beschreibung = kopf.Beschreibung ?? "",
                Aenderbar = kopf.Veraenderbar && !kopf.ReadOnly,
                Verteilung = gelesen.Value.Verteilung,
                Kurvennamen = TagVCtrl.KurvenNamen(gelesen.Value.Kurven)
            };
        }

        /// <summary>
        /// Das Tagesbild. Intervall 2 = jede zweite Stunde, wörtlich aus
        /// <c>init_Chart</c> (<c>AxisX.Interval = 2</c>, x von 0 bis 24).
        /// </summary>
        private static byte[] Tagesbild(double[] werte)
            => ChartRenderer.Stundenprofil("", werte, 2,
                   Text_("GTYP_ACHSE_X", "Stunde des Tages"),
                   Text_("GTYP_ACHSE_Y", "Tagesverteilung"));

        /// <summary>Die 24 Feldnamen der Prüfmeldung — „Stunde 7" (<c>VerteilungUebernehmen</c>:158).</summary>
        private static string[] Feldnamen()
        {
            string vorsatz = Text_("BPRO_FELD_STUNDE", "Stunde");
            var namen = new string[24];
            for (int s = 0; s < 24; s++) namen[s] = vorsatz + " " + (s + 1);
            return namen;
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
