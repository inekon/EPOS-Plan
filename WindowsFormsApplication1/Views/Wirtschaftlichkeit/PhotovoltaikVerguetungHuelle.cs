using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Dialogs „PV-Vergütung (EEG)" (iU9-W2.4).
    ///
    /// <para>Der Dialog lebt als Razor-Komponente
    /// <see cref="PhotovoltaikVerguetungDialog"/> in <c>EPOS.UI</c>; die
    /// WinForms-Fassung <c>Form_PhotovoltaikVerguetung</c> ist mit demselben
    /// Schritt GELÖSCHT (Regel M1).</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Alles, was der Dialog zeigt, wird
    /// hier geladen — mit denselben Controllern und in derselben Reihenfolge wie
    /// zuvor in <c>SetControls(idStamm)</c>: Vergütungssatz, rechnerische
    /// Leistung, Mengen des letzten Laufs, PV-Kosten aus der Kostenwelt,
    /// Strompreis und Wirtschaftlichkeitsparameter. Geschrieben wird über
    /// <c>ProjektPhotovoltaikCtrl.Speichern</c>.</para>
    ///
    /// <para><b>Zwei Wege nach draußen.</b> Der Marktwert-Import braucht einen
    /// <see cref="OpenFileDialog"/> — er läuft als Delegat AUS dem Rückruf der
    /// Komponente heraus, dieselbe verschachtelte Nachrichtenschleife, die die
    /// <see cref="Sprungbruecke"/> (iU9-W2.2) benutzt. Der Sprung in den
    /// Tarifdialog dagegen führt in eine BLAZOR-Hülle und bleibt deshalb
    /// nachgelagert (Risiko R2): Die Komponente meldet den Wunsch im Ergebnis,
    /// diese Hülle schließt den Dialog, öffnet das Ziel und bringt den Dialog
    /// mit frisch geladenen Daten zurück — Muster
    /// <see cref="BhkwWirtschaftlichkeitHuelle"/>.</para>
    /// </summary>
    internal static class PhotovoltaikVerguetungHuelle
    {
        /// <summary>Gewünschtes Innenmaß. Die Maske maß 914 × 724.</summary>
        private const int FENSTER_BREITE = 980;

        /// <summary>
        /// Zeigt den Dialog. Liefert <c>true</c>, wenn „Übernehmen" geschrieben
        /// hat — dann rechnet der Aufrufer neu (Bestandsverhalten von
        /// <c>Form_PhotovoltaikVerguetung.Gespeichert</c>).
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, int idStamm)
        {
            bool gespeichert = false;

            // Der Sprung in den Tarifdialog schliesst dieses Fenster und bringt
            // es danach zurueck; deshalb eine Schleife statt eines Aufrufs.
            while (true)
            {
                PvVerguetungErgebnis ergebnis = EinmalZeigen(besitzer, idStamm);
                if (ergebnis == null) return gespeichert;
                if (ergebnis.Gespeichert) gespeichert = true;
                if (ergebnis.Sprung == PvSprung.Keiner) return gespeichert;

                TarifstrukturHuelle.Oeffnen(besitzer, idStamm, TarifSicht.Photovoltaik);
            }
        }

        /// <summary>Ein Durchgang: laden, zeigen, Ergebnis melden.</summary>
        private static PvVerguetungErgebnis EinmalZeigen(IWin32Window besitzer, int idStamm)
        {
            PvVerguetungErgebnis ergebnis = null;
            BlazorDialogForm<PhotovoltaikVerguetungDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(idStamm, () => dlg))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<PvVerguetungErgebnis>(
                    new object(), e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null && e.Gespeichert);
                    })
            };

            int hoehe = Math.Max(420, Screen.PrimaryScreen.WorkingArea.Height - 90);
            dlg = new BlazorDialogForm<PhotovoltaikVerguetungDialog>(
                new PhotovoltaikVerguetungTexte().Titel, new Size(FENSTER_BREITE, hoehe), werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W5.3). Seit die
        /// Wirtschaftlichkeitsseite selbst eine Razor-Komponente ist, erscheint
        /// er in einer <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe
        /// WebView (Risiko R2). <c>Geschlossen</c> setzt der Wirt; den Sprung in
        /// den Tarifdialog wertet er selbst aus (<c>PvSprung</c>).
        /// </summary>
        /// <param name="besitzerHalter">
        /// Liefert das Fenster, über dem der Dateiwähler des Marktwert-Imports
        /// erscheint — die Dialoghülle bzw. das Fenster der Seite.
        /// </param>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int idStamm, Func<Form> besitzerHalter)
        {
            var ctrl = new ProjektPhotovoltaikCtrl();
            var katalog = new GesetzKatalog();

            ProjektPhotovoltaikModel modell = ctrl.LiesOderVorbelegt(idStamm);
            double kwpRechnerisch = PhotovoltaikCtrl.KwpDesProjekts(idStamm);

            double einspeisungMWh = 0, erzeugungMWh = 0, bedarfMWh = 0;
            double? evQuote = null, autarkie = null;
            try
            {
                ErgebnisModel erg = new ErgebnisCtrl().Load(idStamm);
                if (erg != null && erg.Photovoltaik != null)
                {
                    einspeisungMWh = erg.Photovoltaik.Ueberschuss;
                    erzeugungMWh = erg.Photovoltaik.Stromproduktion;
                    bedarfMWh = erg.Photovoltaik.Strombedarf;
                }
                // Quoten MIT Speicher aus der Speicherrechnung (N.3: stets als
                // Paar); bei mehreren Anlagen die erste Zeile mit Werten.
                if (erg != null && erg.Stromspeicher != null)
                    foreach (ErgebnisStromspeicherModel sp in erg.Stromspeicher)
                        if (sp.Eigenverbrauchsquote > 0 || sp.Autarkiegrad > 0)
                        {
                            evQuote = sp.Eigenverbrauchsquote;
                            autarkie = sp.Autarkiegrad;
                            break;
                        }
            }
            catch { }

            // PV-Kosten aus der Kostenwelt (dieselbe Leselogik wie Bericht und
            // Kostendialog); Betrieb: fehlende Zeile bleibt null (nicht 0).
            double? investPv = null, betriebPv = null;
            try
            {
                investPv = KomponentenSumme(idStamm, KostenSummenCtrl.KATEGORIE_INVESTITION);
                betriebPv = KomponentenSumme(idStamm, KostenSummenCtrl.KATEGORIE_BETRIEB);
            }
            catch { }

            double? strompreis = null;
            try { strompreis = WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh(idStamm); }
            catch { }

            WirtschaftlichkeitParameter wirt = null;
            try { wirt = new WirtschaftlichkeitCtrl().LadeParameter(idStamm); }
            catch { }

            return new Dictionary<string, object>
            {
                ["Modell"] = modell,
                ["KwpRechnerisch"] = kwpRechnerisch,
                ["EinspeisungMWh"] = einspeisungMWh,
                ["ErzeugungMWh"] = erzeugungMWh,
                ["BedarfMWh"] = bedarfMWh,
                ["EvQuoteSpeicher"] = evQuote,
                ["AutarkieSpeicher"] = autarkie,
                ["InvestPv"] = investPv,
                ["BetriebPv"] = betriebPv,
                ["StrompreisEurKwh"] = strompreis,
                ["WirtParameter"] = wirt,

                // Die beiden Kataloge als DELEGAT - dieselbe Uebergabe, die
                // EegSatzRechner und PvErloesRechner selbst verlangen (L9).
                ["Katalog"] = new Func<string, int, double?>(katalog.Wert),
                ["Jahresmarktwert"] = new Func<int, double?>(jahr => ctrl.Jahresmarktwert(jahr, modell)),

                ["Speichern"] = new Func<bool>(() =>
                {
                    try { return ctrl.Speichern(modell); }
                    catch { return false; }
                }),

                ["MarktwerteImportieren"] = new Func<MarktwertImport>(
                    () => MarktwerteImportieren(dlgHalter: besitzerHalter, ctrl: ctrl))
            };
        }

        /// <summary>
        /// P6 (Konzept 6.3): netztransparenz-CSV in die Marktwert-Stammreihen.
        /// <c>null</c> = der Anwender hat die Dateiauswahl abgebrochen.
        ///
        /// <para>Der <see cref="OpenFileDialog"/> erscheint AUS dem Rückruf der
        /// Komponente heraus — dieselbe verschachtelte Nachrichtenschleife, die
        /// die <see cref="Sprungbruecke"/> benutzt und die Windows für einen
        /// Standarddialog im Click-Ereignis vorsieht.</para>
        /// </summary>
        private static MarktwertImport MarktwerteImportieren(Func<Form> dlgHalter,
                                                             ProjektPhotovoltaikCtrl ctrl)
        {
            Form eltern = dlgHalter != null ? dlgHalter() : null;

            using (var wahl = new OpenFileDialog
            {
                Filter = Text("PVW_IMPORT_FILTER", "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*"),
                Title = Text("PVW_BTN_MARKTWERTE", "Marktwerte importieren…")
            })
            {
                DialogResult r = eltern != null && !eltern.IsDisposed
                               ? wahl.ShowDialog(eltern) : wahl.ShowDialog();
                if (r != DialogResult.OK) return null;

                string bericht;
                bool ok = ctrl.ImportiereMarktwerteCsv(wahl.FileName, out bericht);
                return new MarktwertImport(ok, bericht ?? "");
            }
        }

        /// <summary>Summe der PV-Komponente einer Kostenkategorie; null = keine Zeile.</summary>
        private static double? KomponentenSumme(int idProjekt, int kategorie)
        {
            DataTable dt = KostenSummenCtrl.LiesKomponentenSummen(idProjekt, kategorie);
            if (dt == null) return null;
            foreach (DataRow r in dt.Rows)
            {
                if (!string.Equals(Convert.ToString(r["Komponente"]),
                                   DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal))
                    continue;
                return r["Summe"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["Summe"]);
            }
            return null;
        }

        /// <summary>Anzeigetext mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
