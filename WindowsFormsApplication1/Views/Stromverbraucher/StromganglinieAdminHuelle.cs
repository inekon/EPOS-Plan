using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Stromganglinien-Verwaltung (iU9-W12.4).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Der
    /// Katalog kommt aus <see cref="StromganglinieStammCtrl"/>, die Importkette aus
    /// <see cref="GanglinienImportAblauf"/> — die Komponente sieht davon nur
    /// Delegaten.</para>
    ///
    /// <para><b>Die Kette läuft in <c>Task.Run</c>.</b> Lesen und Prüfen einer
    /// 525 600-Zeilen-Datei dauert; in einer WebView ist der Renderfaden derselbe
    /// Faden. Die drei Entscheidungen kommen aber aus der Oberfläche zurück — sie
    /// werden deshalb über den <see cref="TaskScheduler"/> des Bedienfadens
    /// gerufen, damit die Überlagerung dort erscheint, wo Blazor zeichnet.</para>
    /// </summary>
    internal static class StromganglinieAdminHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 664 × 316).</summary>
        private static readonly Size MASS = new Size(880, 620);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation</c> (<c>Masken.StromganglinieAdmin</c>) und von
        /// <c>MenueCtrl.Stromganglinie</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<StromganglinieAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<StromganglinieAdminDialog>(
                MyResource.Resource.IMPORT_TITEL_ADMIN, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Komponente — für die Überlagerung in
        /// <c>StromganglinieDialog</c> (W12.5), die kein zweites Fenster aufmachen
        /// darf (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Katalog"] = new Func<Task<List<GanglinienKatalogZeile>>>(KatalogLesen),
                ["Loeschen"] = new Func<string, Task<bool>>(Loeschen),
                ["DateiWaehlen"] = new Func<string, Task<string>>(DateiWaehlen),
                ["Einlesen"] = new Func<string, GanglinienRaster, GanglinienImportRueckrufe,
                                        Task<GanglinienImportErgebnis>>(Einlesen),
                ["Vorschau"] = new Func<string, GanglinienImportOptionen, Task<GanglinienVorschau>>(Vorschau)
            };
        }

        // =====================================================================
        // Die Datenwege
        // =====================================================================

        /// <summary>Der Katalog samt ReadOnly-Kennzeichen.</summary>
        private static Task<List<GanglinienKatalogZeile>> KatalogLesen()
        {
            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            ctrl.ReadAll();

            List<GanglinienKatalogZeile> liste = new List<GanglinienKatalogZeile>();
            for (int i = 0; i < ctrl.rows; i++)
            {
                StromganglinieModel m = ctrl.items[i];
                liste.Add(new GanglinienKatalogZeile(m.m_szBezeichner, m.m_Zeitinterval,
                                                     ctrl.IsReadOnly(m.m_szBezeichner)));
            }
            return Task.FromResult(liste);
        }

        /// <summary>
        /// Löscht einen Katalogeintrag. <see cref="StromganglinieStammCtrl.Delete"/>
        /// prüft ReadOnly selbst — die Komponente tut es vorher noch einmal, damit
        /// die Rückfrage gar nicht erst kommt.
        /// </summary>
        private static Task<bool> Loeschen(string bezeichner)
            => Task.FromResult(new StromganglinieStammCtrl().Delete(bezeichner));

        /// <summary>
        /// Der Dateiwähler der Plattform, mit dem Ablageordner als Startpunkt —
        /// HINTER dem Blazor-Ereignis (Befund W13‑B‑1, siehe <c>IDateiDienst</c>).
        /// </summary>
        private static Task<string> DateiWaehlen(string filter)
        {
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.IMPORT_TITEL_ADMIN,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.IMPORT_DATEIFILTER : filter,
                GanglinienImportAblauf.AblageOrdner());
        }

        /// <summary>
        /// Die Importkette MIT Ablage. Sie läuft in <c>Task.Run</c>; die drei
        /// Rückrufe der Komponente werden von dort aus gerufen und laufen über die
        /// <c>InvokeAsync</c> des Blazor-Verteilers wieder auf dem richtigen Faden.
        /// </summary>
        private static Task<GanglinienImportErgebnis> Einlesen(
            string pfad, GanglinienRaster raster, GanglinienImportRueckrufe rueckrufe)
            => Task.Run(() => GanglinienImportAblauf.MitAblage(pfad, raster, rueckrufe));

        /// <summary>Neuzerlegung mit den gewählten Optionen (für den Optionendialog).</summary>
        private static Task<GanglinienVorschau> Vorschau(string pfad, GanglinienImportOptionen optionen)
            => Task.Run(() => GanglinienDatei.Vorschau(pfad, optionen));
    }
}
