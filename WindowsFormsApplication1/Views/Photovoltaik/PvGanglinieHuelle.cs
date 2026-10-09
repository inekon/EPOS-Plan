using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Dialogs „Photovoltaik Ganglinie"</b> (PVG, Schemaschritt 205) — dünn: Der Parametersatz
    /// und der Schreibweg stehen plattformfrei in <see cref="PvGanglinieKatalogGaben"/>; hier stehen nur das
    /// Fenster und die Dateiwege des Imports (Naht <see cref="Katalogwege.PvGanglinienDatei"/>).
    /// </summary>
    internal static class PvGanglinieHuelle
    {
        private static readonly Size MASS = new Size(800, 560);
        private static readonly Size MASS_KATALOG = new Size(880, 620);

        /// <summary>Der Unterordner der abgelegten Importdateien unter dem Herstellerdatenpfad.</summary>
        private const string UNTERORDNER = "Photovoltaik";

        /// <summary>
        /// Der Dialog MIT Projekt (Kachel „Photovoltaik", Wahl „Ganglinie"): OK schreibt die Zuordnungen in
        /// EINER Transaktion, Abbrechen verwirft.
        /// </summary>
        /// <returns><c>true</c>, wenn geschrieben wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId)
        {
            List<PvGanglinieZuordnung> liste = PvGanglinieKatalogGaben.Lesen(projektId);
            bool ok = Zeigen(besitzer, PvGanglinieKatalogGaben.ProjektGaben(projektId, liste),
                             MyResource.Resource.PVG_TITEL, MASS);
            return ok && PvGanglinieKatalogGaben.Speichern(projektId, liste);
        }

        /// <summary>Der Dialog OHNE Projekt — der Menüpunkt „Profile &amp; Lastgänge → PV-Ganglinie".</summary>
        internal static bool OeffnenKatalog(IWin32Window besitzer)
            => Zeigen(besitzer, PvGanglinieKatalogGaben.KatalogGaben(), MyResource.Resource.PVG_TITEL_KATALOG, MASS_KATALOG);

        private static bool Zeigen(IWin32Window besitzer, IReadOnlyDictionary<string, object> gaben, string titel, Size mass)
        {
            bool ok = false;
            BlazorDialogForm<PvGanglinieDialog> dlg = null;

            var werte = new Dictionary<string, object>(gaben)
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PvGanglinieDialog>(titel, mass, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        // =================================================================================
        // Die Dateiwege des Imports (Naht Katalogwege.PvGanglinienDatei)
        // =================================================================================

        /// <summary>Dateiwahl mit dem Ablageordner als Start, verlustfreie Ablage, Anzeigen mit der Systemanwendung.</summary>
        internal static GanglinienDateiwege Dateiwege()
        {
            return new GanglinienDateiwege
            {
                DateiWaehlen = DateiWaehlen,
                Ablegen = Ablegen,
                MitSystemOeffnen = pfad => System.Threading.Tasks.Task.FromResult(Dienste.Datei.MitSystemOeffnen(pfad)),
                Ordner = Ablageordner()
            };
        }

        /// <summary>Der Ablageordner: <c>&lt;Herstellerdatenpfad&gt;\Photovoltaik</c>.</summary>
        internal static string Ablageordner()
        {
            string basis = Properties.Settings.Default.VDI3805Path ?? "";
            return System.IO.Path.Combine(basis, UNTERORDNER);
        }

        private static System.Threading.Tasks.Task<string> DateiWaehlen(string filter)
        {
            return Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.PVG_TITEL_KATALOG,
                string.IsNullOrEmpty(filter) ? MyResource.Resource.WBAD_DATEIFILTER : filter,
                Ablageordner());
        }

        private static System.Threading.Tasks.Task<EPOS.UI.Dialoge.Bedarf.AblageErgebnis> Ablegen(string quelle)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                if (string.IsNullOrEmpty(quelle)) return new EPOS.UI.Dialoge.Bedarf.AblageErgebnis("");
                try
                {
                    string ordner = Ablageordner();
                    System.IO.Directory.CreateDirectory(ordner);
                    string ziel = System.IO.Path.Combine(ordner, System.IO.Path.GetFileName(quelle));
                    if (!System.IO.File.Exists(ziel)) System.IO.File.Copy(quelle, ziel, true);
                    return new EPOS.UI.Dialoge.Bedarf.AblageErgebnis(ziel);
                }
                catch (Exception ex)
                {
                    return new EPOS.UI.Dialoge.Bedarf.AblageErgebnis("",
                        string.Format(MyResource.Resource.WBAD_MSG_ABLAGE, ex.Message));
                }
            });
        }
    }
}
