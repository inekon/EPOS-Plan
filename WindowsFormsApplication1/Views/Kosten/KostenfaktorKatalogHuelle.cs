using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;

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
        /// <summary>
        /// Der PARAMETERSATZ des Dialogs (iU9-W4.2). Bis Welle 3 zeigte diese
        /// Hülle ein eigenes Fenster; seit die Kostenverwaltung selbst eine
        /// Razor-Komponente ist, erscheint der Katalog in einer
        /// <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe WebView
        /// (Risiko R2). <c>Geschlossen</c> setzt der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Zeilen"] = Zeilen(),
                ["NeuLaden"] = new Func<IReadOnlyList<KostenfaktorKatalogDialog.KostenfaktorZeile>>(Zeilen),
                ["Neu"] = new Func<string, int>(KostenfaktorCtrl.Neu),
                ["Loeschen"] = new Func<int, bool>(KostenfaktorCtrl.Loeschen),

                // Die Rückfrage von btnDeleteKostenfaktor_Click — derselbe Text,
                // dieselbe Vorgabe (Ja/Nein mit Fragezeichen).
                ["Rueckfrage"] = new Func<string, bool>(
                    text => Dienste.Dialog.Frage(text, Text_("KFAK_FRAGE_TITEL", "Kostenfaktoren"))),

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
                    "Der Kostenfaktor konnte nicht gelöscht werden.")
            };
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
