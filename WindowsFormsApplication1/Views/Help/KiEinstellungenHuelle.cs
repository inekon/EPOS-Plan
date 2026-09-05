using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der KI-Einstellungen (iU9-W15b.4) — Ersatz für
    /// <c>Views/Help/Form_KiEinstellungen.cs</c> (149 Z. + 191 Designer).
    ///
    /// <para><b>Der Dialog speichert nichts.</b> Das war schon der Vertrag des
    /// Vorläufers: Er las seine Anfangswerte aus <see cref="KiChatService"/> und gab
    /// die Eingaben zurück; geschrieben hat der Aufrufer. Hier bleibt es dabei —
    /// die Hülle liest die Anfangswerte und reicht das Ergebnis heraus.</para>
    ///
    /// <para><b>Die eine Ausnahme ist bitgleich mitgezogen</b> (Entscheid E-5,
    /// Befund W15b-B11): „Modell neu erkennen" setzt den Schlüssel SOFORT, vor OK —
    /// sonst könnte die Modellabfrage gar nicht laufen. Ein anschließendes
    /// „Abbrechen" nimmt das nicht zurück. Das steht in
    /// <see cref="ModellNeuErkennen"/>, nicht in der Komponente: Nur hier gibt es
    /// <see cref="KiChatService"/>.</para>
    ///
    /// <para><b>Regel S-1.</b> Der Schlüssel geht als Vorbelegung hinein und über das
    /// Ergebnis wieder heraus; gelesen und geschrieben wird er ausschließlich in
    /// <see cref="KiChatService"/>, das ihn DPAPI-verschlüsselt in
    /// <c>ki-schluessel.dat</c> ablegt.</para>
    /// </summary>
    internal static class KiEinstellungenHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 500 × 358, FixedDialog).</summary>
        private static readonly Size MASS = new Size(620, 480);

        /// <summary>
        /// Öffnet die Einstellungen. Rückgabe <c>true</c>, wenn mit OK geschlossen
        /// wurde; die Werte sind dann bereits in <see cref="KiChatService"/>
        /// geschrieben.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            KiEinstellungenErgebnis ergebnis = null;
            BlazorDialogForm<KiEinstellungenDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<KiEinstellungenErgebnis>(
                    new object(), e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null);
                    })
            };

            // KLEIN (Anwenderwunsch 05.09.2026): vier Felder, keine Liste.
            dlg = new BlazorDialogForm<KiEinstellungenDialog>(
                MyResource.Resource.KI_EINST_TITEL, MASS, werte,
                EPOS.UI.Dienste.Dialogart.Klein);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            if (ergebnis == null) return false;

            Uebernehmen(ergebnis);
            return true;
        }

        /// <summary>
        /// Schreibt das Ergebnis nach <see cref="KiChatService"/> — genau die zwei
        /// Zuweisungen, die im Bestand hinter <c>ShowDialog</c> standen
        /// (<c>Form_KiChat.cs:1490-1491</c>).
        /// </summary>
        internal static void Uebernehmen(KiEinstellungenErgebnis ergebnis)
        {
            if (ergebnis == null) return;
            KiChatService.ApiKey = ergebnis.ApiSchluessel;
            KiChatService.WegBErzwingen = ergebnis.WegBErzwingen;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>. So kann ihn auch
        /// das Chatfenster verwenden, das die Einstellungen als ÜBERLAGERUNG zeigt
        /// statt als zweites Fenster (Risiko R2).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Schluessel"] = KiChatService.ApiKey,
                ["Tageslimit"] = KiChatService.Tageslimit,
                ["Modellzeile"] = string.Format(MyResource.Resource.KI_EINST_HINWEIS_MODELL,
                                                KiChatService.MODELL),
                ["WegB"] = KiChatService.WegBErzwingen,
                ["ModellNeuErkennen"] = (Func<string, Task<string>>)ModellNeuErkennen,

                ["SchluesselText"] = MyResource.Resource.KI_EINST_LBL_SCHLUESSEL,
                ["ModellNeuText"] = MyResource.Resource.KI_EINST_BTN_MODELL,
                ["TageslimitText"] = MyResource.Resource.KI_EINST_LBL_TAGESLIMIT,
                ["TageslimitFormat"] = MyResource.Resource.KI_EINST_LIMIT_FEST,
                ["TageslimitTipp"] = MyResource.Resource.KI_EINST_TIP_TAGESLIMIT,
                ["HinweisDaten"] = MyResource.Resource.KI_EINST_HINWEIS_DATEN,
                ["HinweisKontingent"] = MyResource.Resource.KI_EINST_HINWEIS_KONTINGENT,
                ["WegBText"] = MyResource.Resource.KI_AKT_WEGB_EINSTELLUNG,
                ["OkText"] = MyResource.Resource.KI_EINST_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.KI_EINST_BTN_ABBRECHEN
            };
        }

        /// <summary>
        /// „Modell neu erkennen" — <b>mit Seiteneffekt</b> (E-5): Der Schlüssel wird
        /// sofort übernommen, sonst könnte die Abfrage nicht laufen. Zurück kommt die
        /// neue Modellzeile.
        /// </summary>
        private static Task<string> ModellNeuErkennen(string schluessel)
        {
            KiChatService.ApiKey = (schluessel ?? "").Trim();
            KiChatService.ModellZuruecksetzen();

            return Task.FromResult(
                string.Format(MyResource.Resource.KI_EINST_HINWEIS_MODELL_NEU,
                              KiChatService.MODELL));
        }
    }
}
