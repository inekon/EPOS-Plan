using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der WINDOWS-ADAPTER der Kostenverwaltung — alles, was von der alten
    /// Windows-Hülle übrig bleibt.
    ///
    /// <para><b>Die Hülle selbst ist plattformfrei</b> und liegt seit Etappe E3,
    /// Schritt 5 in <see cref="KostenKomponenteHuelle"/> (<c>EPOS.UI.Daten</c>):
    /// Sie lädt, rechnet und schreibt über Kern-Controller und kennt keine
    /// Plattform. Windows steuert nur noch das Fenster bei — eine
    /// <see cref="BlazorDialogForm{TKomponente}"/> um
    /// <see cref="KostenKomponenteDialog"/>, gebaut aus dem Parametersatz der
    /// Hülle. Wortgleich zu <see cref="EnergietraegerFenster"/> und
    /// <see cref="NutzungsdauerFenster"/>; auf iOS zeigt dieselbe Hülle dieselbe
    /// Komponente ohne diesen Adapter.</para>
    ///
    /// <para><b>Der Titel kommt aus der Hülle.</b> Anders als bei den beiden
    /// Nachbarn hängt er am Kontext (Komponente und Projektname) und entsteht
    /// deshalb erst beim Bauen des Parametersatzes — die Hülle reicht ihn als
    /// <c>out</c>-Wert heraus.</para>
    /// </summary>
    internal static class KostenKomponenteFenster
    {
        /// <summary>
        /// Zeigt die Kostenverwaltung im STAMMKONTEXT (Katalogpflege) als modales
        /// Fenster.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="komponente">Vorwahl der Komponente; <c>null</c> = die erste.</param>
        /// <param name="betrieb"><c>true</c> = auf die Betriebskostensicht schalten.</param>
        internal static void Oeffnen(IWin32Window besitzer, string komponente = null,
                                     bool betrieb = false)
        {
            Zeigen(KostenKomponenteHuelle.FuerStamm(), besitzer, komponente, betrieb, 0);
        }

        /// <summary>
        /// Zeigt dieselbe Verwaltung im PROJEKTMODUS (KD6a): derselbe Dialog
        /// pflegt die <c>Tab_ProjektWerte</c>-Positionen des Projekts.
        /// </summary>
        internal static void OeffnenProjekt(IWin32Window besitzer, int idProjekt, string projektname,
                                            string komponente = null, bool betrieb = false,
                                            int idAnlage = 0)
        {
            Zeigen(KostenKomponenteHuelle.FuerProjekt(idProjekt, projektname),
                   besitzer, komponente, betrieb, idAnlage);
        }

        private static void Zeigen(KostenKomponenteHuelle huelle, IWin32Window besitzer,
                                   string komponente, bool betrieb, int idAnlage)
        {
            // Die Hüllen-INSTANZ hält den Bearbeitungsstand; sie lebt über die
            // Rückrufe ihres Parametersatzes so lange wie das Fenster.
            BlazorDialogForm<KostenKomponenteDialog> dlg = null;

            string titel;
            var werte = new Dictionary<string, object>(
                huelle.Gaben(komponente, betrieb, idAnlage, out titel))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<KostenKomponenteDialog>(
                titel,
                new Size(KostenKomponenteHuelle.FENSTER_BREITE,
                         KostenKomponenteHuelle.FENSTER_HOEHE),
                werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }
    }
}
