using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER der Energieträgerverwaltung — alles, was von der
    /// alten Windows-Hülle übrig bleibt.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt in
    /// <see cref="EnergietraegerHuelle"/> (<c>EPOS.UI.Daten</c>): Sie lädt,
    /// rechnet und schreibt über Kern-Controller und kennt keine Plattform.
    /// Windows steuert nur noch das Fenster bei — eine
    /// <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="EnergietraegerDialog"/>, gebaut aus dem Parametersatz der
    /// Hülle. Auf iOS zeigt dieselbe Hülle dieselbe Komponente ohne diesen
    /// Adapter.</para>
    /// </summary>
    internal static class EnergietraegerFenster
    {
        /// <summary>
        /// Zeigt die Energieträgerverwaltung als modales Fenster.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="projektId">0 = Katalogkontext (Stammdaten).</param>
        /// <param name="traegerId">Vorwahl (KD6 § 9: „Energiekosten…" springt
        /// direkt auf den Träger der Komponente); 0 = der erste.</param>
        /// <param name="erzeugerart">Komponente, aus der heraus geöffnet wurde
        /// (<c>DbWerte.ERZEUGER_*</c>); <c>null</c> = ohne Komponentenkontext — so ruft
        /// der Katalogeinstieg des Menüs. Die Einengung auf die zulässigen Träger rechnet
        /// die Hülle, nicht dieser Adapter (Auftrag 268); im Projektkontext geht der
        /// Komponentenbezug über die Kostenseite, die die Hülle unmittelbar füllt.</param>
        /// <param name="geraeteId">Gerätezeile des Brenners (<c>Tab_Heizkessel.ID</c>
        /// bzw. <c>Tab_BHKW.ID</c>); 0 = unbekannt.</param>
        internal static void Oeffnen(IWin32Window besitzer, int projektId, int traegerId = 0,
                                     string erzeugerart = null, int geraeteId = 0)
        {
            // Die Hüllen-INSTANZ hält den Bearbeitungsstand; sie lebt über die
            // Rückrufe ihres Parametersatzes so lange wie das Fenster.
            var huelle = new EnergietraegerHuelle(projektId);

            BlazorDialogForm<EnergietraegerDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                huelle.Gaben(traegerId, erzeugerart, geraeteId))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<EnergietraegerDialog>(
                EnergietraegerHuelle.Titel(),
                new Size(EnergietraegerHuelle.FENSTER_BREITE, EnergietraegerHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }
    }
}
