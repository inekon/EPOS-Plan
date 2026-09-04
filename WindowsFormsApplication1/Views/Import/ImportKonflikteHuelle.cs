using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Import;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des gemeinsamen Konfliktdialogs (iU9-W12.3).
    ///
    /// <para><b>Warum es sie gibt — und warum nur für eine Welle.</b> Der
    /// Konfliktdialog hat fünf Aufrufer. Einer davon ist die
    /// Stromganglinien-Verwaltung, die mit dieser Welle Blazor wird; die anderen
    /// vier sind Importmasken der Welle 13 und bleiben bis dahin WinForms. Bliebe
    /// <c>Form_ImportKonflikte</c> stehen, müsste die Razor-Komponente
    /// <c>StromganglinieAdminDialog</c> mitten in einem Rückruf ein modales
    /// WinForms-Fenster öffnen UND eine <c>List&lt;KonfliktEntscheidung&gt;</c>
    /// zurückbekommen — das kann die <c>Sprungbruecke</c> nicht (sie löst
    /// Schlüssel → Form auf und liefert einen <c>bool</c>). Also andersherum: Die
    /// Komponente entsteht jetzt, und die vier WinForms-Aufrufer erreichen sie über
    /// diese Hülle. <b>Mit Welle 13 wird die Datei gelöscht.</b></para>
    ///
    /// <para>Die Signatur ist die des Vorläufers
    /// (<c>Form_ImportKonflikte.Zeigen</c>) — die vier Aufrufer ändern je EINE
    /// Zeile, sonst nichts.</para>
    /// </summary>
    internal static class ImportKonflikteHuelle
    {
        /// <summary>Gewünschtes Innenmaß — die Maße des Vorläufers (860 × 420).</summary>
        private static readonly Size MASS = new Size(880, 460);

        /// <summary>
        /// Zeigt den Konfliktdialog modal.
        /// </summary>
        /// <param name="owner">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="pruefungen">Ergebnis von <c>DublettenPruefung.PruefeKandidaten</c>.</param>
        /// <param name="vergebeneNamen">Normalisierte Bestandsnamen (<c>DublettenPruefung.VergebeneNamen</c>).</param>
        /// <returns>
        /// Je Prüfung eine Entscheidung — auch für die konfliktfreien Zeilen, die
        /// Aufrufer zählen daraus „übersprungen" —, oder <c>null</c> bei Abbruch;
        /// dann wird gar nichts importiert.
        /// </returns>
        internal static List<KonfliktEntscheidung> Zeigen(IWin32Window owner,
            List<ImportPruefung> pruefungen, HashSet<string> vergebeneNamen)
        {
            List<KonfliktEntscheidung> ergebnis = null;
            BlazorDialogForm<ImportKonflikteDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Pruefungen"] = pruefungen ?? new List<ImportPruefung>(),
                ["VergebeneNamen"] = (IReadOnlyCollection<string>)
                    (vergebeneNamen ?? new HashSet<string>(StringComparer.Ordinal)),
                ["Geschlossen"] = EventCallback.Factory.Create<List<KonfliktEntscheidung>>(
                    new object(), liste =>
                    {
                        ergebnis = liste;
                        if (dlg != null) dlg.Schliessen(liste != null);
                    })
            };

            dlg = new BlazorDialogForm<ImportKonflikteDialog>(
                MyResource.Resource.IMP_KONFLIKT_TITEL, MASS, werte);

            using (dlg)
            {
                if (owner != null) dlg.ShowDialog(owner); else dlg.ShowDialog();
            }

            return ergebnis;
        }
    }
}
