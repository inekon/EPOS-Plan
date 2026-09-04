using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Strom;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Stromganglinien-Zuordnung (iU9-W12.5).
    ///
    /// <para><b>Sie schreibt nichts</b> — genau wie der Vorläufer. Die Liste kommt
    /// herein, wird an Ort und Stelle bearbeitet und geht zurück; abgelegt wird sie
    /// beim Aufrufer (<c>Form_Start.pBox_StromMessdaten_Click</c> und
    /// <c>StromganglinieKontextMenuCtrl.ContextMenuItemNeu_Click</c>, beide mit
    /// <c>WizardCtrl.Del_Stromganglinie</c> + <c>Add_Stromganglinie</c>). Das ist
    /// Risiko R‑W12‑4 des Wellenplans: Eine Hülle, die selbst ablegt, würde
    /// dieselbe Zuordnung zweimal schreiben.</para>
    ///
    /// <para><b>Die Ganglinien-Id holt die Hülle.</b> Die Komponente kennt den
    /// Katalog nur über Bezeichner; beim Zurückschreiben löst
    /// <see cref="StromganglinieStammCtrl.FindeStamm"/> ihn in die Id auf — die
    /// Abfrage, die bis iU9-W12.0g als konkatenierter <c>SELECT *</c> in der Maske
    /// stand (Befund W12-B4).</para>
    ///
    /// <para><b>Die Verwaltung ist eine ÜBERLAGERUNG, kein zweites Fenster.</b>
    /// „Bearbeiten…" zeigt <c>StromganglinieAdminDialog</c> in derselben WebView;
    /// die Hülle reicht dafür nur den Parametersatz
    /// <see cref="StromganglinieAdminHuelle.Gaben"/> durch (Risiko R2).</para>
    /// </summary>
    internal static class StromganglinieHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 678 × 345).</summary>
        private static readonly Size MASS = new Size(880, 520);

        /// <summary>
        /// Zeigt die Zuordnung modal und schreibt die Liste an Ort und Stelle
        /// zurück.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Das Projekt.</param>
        /// <param name="liste">
        /// Die Zuordnungen des Projekts — sie wird bearbeitet, nicht kopiert.
        /// </param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<Z_ProjektStromganglinieModel> liste)
        {
            if (liste == null) throw new ArgumentNullException(nameof(liste));

            List<GanglinienProjektZeile> zeilen = new List<GanglinienProjektZeile>();
            foreach (Z_ProjektStromganglinieModel m in liste)
                zeilen.Add(new GanglinienProjektZeile(m.m_ID_Z, m.m_ID_Stromganglinie,
                                                      m.m_szStromganglinie ?? ""));

            bool ok = false;
            BlazorDialogForm<StromganglinieDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Katalog"] = new Func<Task<List<GanglinienKatalogZeile>>>(KatalogLesen),
                ["Verwaltung"] = StromganglinieAdminHuelle.Gaben(),
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<StromganglinieDialog>(
                MyResource.Resource.STROMGL_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            Zurueckschreiben(projektId, zeilen, liste);
            return ok;
        }

        /// <summary>
        /// Die bearbeitete Liste zurück in die Modelle des Aufrufers — AUCH bei
        /// Abbruch: Der Vorläufer führte dieselbe Liste, und die Aufrufer leiten
        /// ihren Kachelstatus UNABHÄNGIG vom Ergebnis aus <c>DateiListe.Count</c> ab
        /// (<c>Form_Start</c> :487-490).
        /// </summary>
        private static void Zurueckschreiben(int projektId,
                                             List<GanglinienProjektZeile> zeilen,
                                             List<Z_ProjektStromganglinieModel> ziel)
        {
            ziel.Clear();

            foreach (GanglinienProjektZeile z in zeilen)
            {
                int idGanglinie = z.GanglinieId;
                if (idGanglinie == 0)
                {
                    StromganglinieModel satz = StromganglinieStammCtrl.FindeStamm(z.Bezeichner);
                    if (satz == null) continue;      // der Katalogeintrag ist weg
                    idGanglinie = satz.ID;
                }

                ziel.Add(new Z_ProjektStromganglinieModel
                {
                    m_ID_Z = z.Schluessel,
                    m_ID_Projekt = projektId,
                    m_ID_Stromganglinie = idGanglinie,
                    m_szStromganglinie = z.Bezeichner
                });
            }
        }

        /// <summary>Der Katalog — dieselbe Quelle wie in der Verwaltung.</summary>
        private static Task<List<GanglinienKatalogZeile>> KatalogLesen()
        {
            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            ctrl.ReadAll();

            List<GanglinienKatalogZeile> liste = new List<GanglinienKatalogZeile>();
            for (int i = 0; i < ctrl.rows; i++)
                liste.Add(new GanglinienKatalogZeile(ctrl.items[i].m_szBezeichner,
                                                     ctrl.items[i].m_Zeitinterval, false));
            return Task.FromResult(liste);
        }
    }
}
