using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das WINDOWS-FENSTER der Gebäude eines Projekts — seit Stufe G1 nur noch der Adapter
    /// (Umsetzungskonzept Gebäudesimulation 2.8, Entscheid E27/A10).
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b> (<see cref="GebaeudeHuelle"/>,
    /// dazu <c>GebaeudeKatalogHuelle</c>, <c>GebaeudeBedarfHuelle</c>,
    /// <c>GebaeudeWohnflaecheHuelle</c> und die Naht <c>Gebaeudewege</c>). Hier bleibt, was
    /// nur Windows kann: das modale Fenster, sein Maß und der Rückruf beim Schließen.</para>
    /// </summary>
    internal static class GebaeudeFenster
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 812 × 573).</summary>
        private static readonly Size MASS = new Size(1060, 720);

        /// <summary>
        /// Die vorläufige Id einer noch nicht gespeicherten Zuordnung — derselbe
        /// Startwert wie <c>Form_Gebaeude.startindex</c>.
        /// </summary>
        private const int STARTINDEX = 100000;

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>
        /// Zeigt die Gebäude eines Projekts als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Gebaude_Click</c> und den beiden Kontextmenüpunkten.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjGebModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<GebaeudeDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                GebaeudeHuelle.Gaben(projektId, projektName, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GebaeudeDialog>(GebaeudeHuelle.Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Die KATALOGVERWALTUNG (<c>Masken.GebaeudeAdmin</c>) — seit Stufe 5 der Neuordnung
        /// der Administrationsdialoge (V16) eine eigene Komponente, <see cref="GebaeudeAdminDialog"/>,
        /// im Gerüst der übrigen Verwaltungen; die Datenseite ist <see cref="GebaeudeAdminHuelle"/>.
        /// </summary>
        internal static bool Katalogverwaltung(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<GebaeudeAdminDialog> dlg = null;

            var werte = new Dictionary<string, object>(GebaeudeAdminHuelle.Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GebaeudeAdminDialog>(GebaeudeAdminHuelle.Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }
    }
}
