using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1.Zeichnung;

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
            // Stufe 5 der Neuordnung der Administrationsdialoge (V16, A10): Die Typliste
            // ist eine Katalogliste (Katalogfilterzeilen, Profil FuerGebaeudetyp), Neu...
            // fragt die Kurvenzahl, Duplizieren... kopiert samt Tageskurven, und die
            // Loeschsperre nennt die Gebaeude, die den Typ fuehren. Die Beschriftungen
            // bringt die Komponente selbst aus MyResource mit.
            return new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(() => TagVCtrl.Katalogfilterzeilen()),
                ["Katalogprofil"] = Katalogfilterprofil.FuerGebaeudetyp(s => Text_(s, s)),
                ["Lies"] = new Func<string, GebaeudetypDaten>(Lesen),
                ["Speichern"] = new Func<int, double[,], bool>(TagVCtrl.Speichern),
                ["BeschreibungSpeichern"] = new Func<int, string, bool>(TagVCtrl.BeschreibungSchreiben),
                ["Anlegen"] = new Func<string, string, int, int>(TagVCtrl.Anlegen),
                ["Loeschen"] = new Func<int, bool>(TagVCtrl.Loeschen),
                ["Duplizieren"] = new Func<int, string, KatalogSpeicherErgebnis>(Duplizieren),
                // AD-Q15: das Schloss (ReadOnly und Veraenderbar) nach Rueckfrage umschalten.
                ["Schloss"] = Schlosswege.Aus(TagVCtrl.SchlossSetzen),
                ["Verwendung"] = new Func<IReadOnlyDictionary<string, int>>(() => TagVCtrl.Gebaeudeverwendung()),
                ["Bild"] = new Func<double[], Zeichenmodell>(Tagesbild),
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),

                ["TitelText"] = Text_("GTYP_TITEL", "Gebäudetypen Verwaltung"),
                ["Feldnamen"] = Feldnamen(),
                ["HilfeSchluessel"] = "Form_EingGebTyp.btn_Help"
            };
        }

        /// <summary>„Duplizieren…" — der Kern kopiert Kopf und Tageskurven in EINER Transaktion.</summary>
        private static KatalogSpeicherErgebnis Duplizieren(int id, string name)
        {
            Katalogkopie.Ergebnis e = TagVCtrl.Duplizieren(id, name);
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
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
        /// Das Tagesbild als ZEICHENMODELL (Etappe DG-E3, Gruppe (a)). Intervall 2 =
        /// jede zweite Stunde, wörtlich aus <c>init_Chart</c>
        /// (<c>AxisX.Interval = 2</c>, x von 0 bis 24).
        ///
        /// <para>Auf der x-Achse zählt der INDEX der Reihe: Die Fläche geht von 0 bis
        /// n, die Reihe von 1 bis n (Protokoll DG-E3, „Was x je Bild bedeutet").</para>
        /// </summary>
        private static Zeichenmodell Tagesbild(double[] werte)
            => ChartRenderer.StundenprofilModell("", werte, 2,
                   Text_("GTYP_ACHSE_X", "Stunde des Tages"),
                   Text_("GTYP_ACHSE_Y", "Tagesverteilung"));

        // =================================================================
        // Die Farbe einer Reihe (Farbrollen, Bedienung Teil 2)
        // =================================================================

        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags: Die Rolle bekommt
        /// anwendungsweit diese Farbe — Bildschirm wie Bericht.
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
