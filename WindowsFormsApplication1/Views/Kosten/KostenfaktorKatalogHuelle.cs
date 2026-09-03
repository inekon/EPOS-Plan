using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HUELLE des Dialogs „Administration Kostenfaktoren" (iU9-W1.5).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="KostenfaktorKatalogDialog"/> kennt keine Datenbank; alle drei
    /// Anweisungen der gelöschten Maske <c>Form_KostenAdmin</c> stehen seit iU9-W1.5
    /// im Kern-Controller <see cref="KostenfaktorCtrl"/>. Diese Hülle verbindet
    /// beides und reicht die Rückfrage vor dem Löschen an
    /// <c>Dienste.Dialog.Frage</c> weiter — ein Ja/Nein-Baustein in
    /// <c>EPOS.UI</c> entsteht erst in Welle 4 (Bausteinlücke 8).</para>
    /// </summary>
    internal static class KostenfaktorKatalogHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 569 × 414;
        /// die Liste steht jetzt untereinander mit Anlegezeile und Leiste.</summary>
        private static readonly Size FENSTER = new Size(600, 560);

        /// <summary>Zeigt den Dialog. Der Vorlaeufer kannte kein Ergebnis — sein
        /// „OK" schloss nur das Fenster (<c>btn_OK_Click</c>: <c>Close()</c>).</summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        internal static void Oeffnen(IWin32Window besitzer)
        {
            BlazorDialogForm<KostenfaktorKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Zeilen"] = Zeilen(),
                ["NeuLaden"] = new Func<IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>>(Zeilen),
                ["Neu"] = new Func<string, int>(KostenfaktorCtrl.Neu),
                ["Loeschen"] = new Func<int, bool>(KostenfaktorCtrl.Loeschen),

                // Die Rückfrage von btnDeleteKostenfaktor_Click — derselbe Text,
                // dieselbe Vorgabe (Ja/Nein mit Fragezeichen).
                ["Rueckfrage"] = new Func<string, bool>(
                    text => Dienste.Dialog.Frage(text, Text_("KFAK_TITEL", "Kostenfaktoren"))),

                ["TitelText"] = Text_("KFAK_TITEL", "Administration Kostenfaktoren"),
                ["EinleitungText"] = Text_("KFAK_EINLEITUNG", "Verwalten Sie hier die Kostenfaktoren"),
                ["SpalteBezeichnung"] = Text_("KFAK_SP_BEZEICHNUNG", "Kostenfaktoren:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelNeu"] = Text_("KFAK_LBL_NEU", "Bezeichner"),
                ["NeuText"] = Text_("KFAK_BTN_NEU", "➕ Neu"),
                ["LoeschenText"] = Text_("KFAK_BTN_LOESCHEN", "🗑️ Löschen"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["VorlageRueckfrage"] = Text_("KFAK_MSG_LOESCHEN", "Kostenfaktor '{0}' wirklich löschen?"),
                ["MeldungNeuFehler"] = Text_("KFAK_MSG_NEU_FEHLER",
                    "Der Kostenfaktor konnte nicht angelegt werden."),
                ["MeldungLoeschenFehler"] = Text_("KFAK_MSG_LOESCHEN_FEHLER",
                    "Der Kostenfaktor konnte nicht gelöscht werden."),

                ["Geschlossen"] = EventCallback.Factory.Create(new object(), () =>
                {
                    if (dlg != null) dlg.Schliessen(true);
                })
            };

            dlg = new BlazorDialogForm<KostenfaktorKatalogDialog>(
                Text_("KFAK_TITEL", "Administration Kostenfaktoren"), FENSTER, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        /// <summary>Die Katalogzeilen aus dem Kern, in die Zeilenform der Komponente.</summary>
        private static IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile> Zeilen()
        {
            var liste = new List<KostenfaktorKatalogDialog.KostenfaktorZeile>();
            foreach (KostenfaktorCtrl.Eintrag e in KostenfaktorCtrl.Alle())
                liste.Add(new KostenfaktorKatalogDialog.KostenfaktorZeile(e.StammId, e.Bezeichnung));
            return liste;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
