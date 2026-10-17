using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER der Verwaltung „Kältemaschinen" — nur der Adapter, nach dem Muster der
    /// Wärmepumpe (<see cref="WaermepumpeStammHuelle"/>).
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<c>KaeltemaschineKatalogHuelle.Gaben</c>): Katalog, Stammblatt, Speichern, Löschen,
    /// Duplizieren, Schloss, die Teillastrechnungen und „Typkennfelder laden…". Hier kommt allein der
    /// Importweg mit der Windows-Dateiwahl (<c>KatalogImportHuelle.Gaben</c>) hinzu — derselbe Satz
    /// wie in der freien Ansicht.</para>
    ///
    /// <para><b>Schließen.</b> „Beenden", Esc und das Kreuz im Dialogkopf rufen <c>Geschlossen</c>; der
    /// Dialog hält dabei selbst an, solange geänderte Felder ungespeichert sind
    /// (<c>ADM_MSG_UNGESPEICHERT</c>) — derselbe Weg wie im Wärmepumpen-Stammdialog. Fensterkreuz und
    /// Alt+F4 führen über den <c>Fensterschliessweg</c> erst offene Blätter zurück.</para>
    ///
    /// <para><b>Auf iOS bleibt es bei der freien Ansicht der <c>AppWurzel</c></b>
    /// (<c>IosProjektQuelle.KaeltemaschineKatalogGaben</c>); unter Windows fängt
    /// <c>WinFormsNavigation</c> den Schlüssel <c>Seitenschluessel.KaeltemaschineKatalog</c> ab, bevor er
    /// die Wurzel erreicht.</para>
    /// </summary>
    internal static class KaeltemaschineKatalogFensterHuelle
    {
        /// <summary>Gewünschtes Innenmaß — dasselbe wie der Wärmepumpen-Stammdialog.</summary>
        private static readonly Size MASS = new Size(1000, 760);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>WinFormsNavigation.OeffneMaske(Seitenschluessel.KaeltemaschineKatalog)</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit „Beenden" (Esc, Kreuz) geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<KaeltemaschineKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KaeltemaschineKatalogDialog>(MyResource.Resource.KM_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der Parametersatz — dieselben Gaben wie die freie Ansicht der Wurzel
        /// (<c>HauptfensterHuelle</c>, <c>KaeltemaschineKatalogGaben</c>), samt Import über die Dateiwahl.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return KaeltemaschineKatalogHuelle.Gaben(
                () => KatalogImportHuelle.Gaben(KatalogImportArt.Kaeltemaschine));
        }
    }
}
