using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER des Gesetzeskatalogs — alles, was von der alten
    /// Windows-Hülle übrig bleibt.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt seit Etappe E3,
    /// Schritt 6 in <see cref="GesetzeskatalogHuelle"/> (<c>EPOS.UI.Daten</c>):
    /// Sie liest und schreibt über <c>GesetzKatalog</c> und kennt keine
    /// Plattform. Hier steht nur das Fenster des Menüpunkts „Administration →
    /// Gesetzeskatalog" — eine <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="GesetzeskatalogDialog"/>, gebaut aus dem Parametersatz der
    /// Hülle. Wortgleich zu <see cref="EnergietraegerFenster"/>.</para>
    ///
    /// <para><b>Die zwei anderen Aufrufer brauchen ihn nicht.</b> Kostendialog
    /// und Wirtschaftlichkeits-Parameterdialog zeigen denselben Satz als
    /// <c>Ueberlagerung</c> im eigenen Fenster (Risiko R2) — auf jeder
    /// Plattform.</para>
    /// </summary>
    internal static class GesetzeskatalogFenster
    {
        /// <summary>
        /// Zeigt den Katalog als eigenes Fenster — der Weg von
        /// <c>Hauptfensterrahmen.InitGesetzeMenue</c>.
        /// </summary>
        /// <param name="besitzer">Das Fenster, über dem der Dialog erscheint.</param>
        /// <param name="vorwahlKlasse">Vorgewählter Bereich; leer = der erste.</param>
        /// <returns><c>true</c>, wenn der Dialog etwas geschrieben hat.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, string vorwahlKlasse = "")
        {
            bool ok = false;
            BlazorDialogForm<GesetzeskatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                GesetzeskatalogHuelle.Gaben(vorwahlKlasse))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GesetzeskatalogDialog>(
                GesetzeskatalogHuelle.Titel(),
                new Size(GesetzeskatalogHuelle.FENSTER_BREITE,
                         GesetzeskatalogHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }
    }
}
