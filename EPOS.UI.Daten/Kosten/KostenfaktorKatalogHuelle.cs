using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Kosten;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HUELLE des Dialogs „Administration Kostenfaktoren" (iU9-W1.5), seit
    /// Etappe E3 Schritt 1 plattformfrei in <c>EPOS.UI.Daten</c>: keine
    /// WinForms-Anweisung, kein Fenster, alle Quellen im Kern.
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die Komponente
    /// <see cref="KostenfaktorKatalogDialog"/> kennt keine Datenbank; alle drei
    /// Anweisungen der gelöschten Maske <c>Form_KostenAdmin</c> stehen seit iU9-W1.5
    /// im Kern-Controller <see cref="KostenfaktorCtrl"/>. Diese Hülle verbindet
    /// beides. Die Rückfrage vor dem Löschen reicht sie NICHT mehr weiter: Sie
    /// steht als Baustein <c>Rueckfrage</c> im Dialog selbst, weil ein modales
    /// Systemfenster unmittelbar aus einem Blazor-Ereignis hier verboten ist
    /// (Regel (b)) und es auf iOS keine MessageBox gibt.</para>
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
                ["Loeschen"] = new Func<int, ValueTuple<bool, string>>(Loeschen),

                // Die Rückfrage von btnDeleteKostenfaktor_Click steht seit dem
                // Umbau IM Dialog (Baustein Rueckfrage, Vorgabe „Nein"). Hier
                // darf kein Schlüssel „Rueckfrage" mehr stehen: Der Parametersatz
                // trifft nur [Parameter], und ein unbekannter Schlüssel bricht
                // beim ersten Zeichnen im Blazor-Verteiler.

                // Leer: Die Ueberlagerung (KostenKomponenteDialog) traegt den
                // Titel schon (KatalogTitel, derselbe Schluessel KFAK_TITEL) -
                // ein zweiter Kopf waere ein doppelter Titel (Hausregel
                // W11b-B-9, #187). KostenfaktorKatalogHuelle hat heute keinen
                // zweiten, eigenstaendigen Aufrufer.
                ["TitelText"] = "",
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

        /// <summary>
        /// Löschen MIT dem benannten Grund (Auftrag #302). Der Kern zählt vorher, wer
        /// den Kostenfaktor benutzt; bleibt er stehen, reicht die Hülle den Grund als
        /// zweiten Rückgabewert durch, und der Dialog zeigt ihn statt der allgemeinen
        /// Meldung <c>KFAK_MSG_LOESCHEN_FEHLER</c>.
        ///
        /// <para>Derselbe Aufbau wie <c>EnergietraegerHuelle.TraegerLoeschen</c>: Die
        /// Hülle kennt kein SQL, sie übersetzt nur <c>out string</c> in das Wertepaar,
        /// das eine Razor-Komponente als Parameter nehmen kann.</para>
        /// </summary>
        private static ValueTuple<bool, string> Loeschen(int stammId)
        {
            string grund;
            bool ok = KostenfaktorCtrl.Loeschen(stammId, out grund);
            return new ValueTuple<bool, string>(ok, grund ?? "");
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
