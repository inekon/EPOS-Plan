using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Projekt;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von „Projekt Speichern unter" (iU9-W15a.4) — sie löst
    /// <c>Form_ProjektSpeichernUnter</c> ab.
    ///
    /// <para><b>Der Fachteil lag schon vorher im Kern.</b> <c>ProjektDuplizierenCtrl</c>
    /// (768 Z.) kopiert alle Projekttabellen in EINER Transaktion; seit iU9-W15a.0b/0c
    /// liegen dort auch die drei Vorprüfungen und das Schreiben der drei
    /// Verwaltungsfelder. Diese Hülle reicht nur noch durch.</para>
    ///
    /// <para><b>Der Kopierlauf läuft NEBENLÄUFIG</b>, wie beim Vorläufer: Der Bedienfaden
    /// liest und zeigt, <c>Task.Run</c> kopiert, <c>Progress&lt;T&gt;</c> besorgt das
    /// Marshalling. Neu ist der Abbruch (A-2): Ein <c>CancellationToken</c> geht mit in den
    /// Kern, und ein Abbruch rollt die eine Transaktion zurück.</para>
    /// </summary>
    internal static class ProjektKopieHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 544 × 622).</summary>
        private static readonly Size MASS = new Size(940, 660);

        /// <summary>
        /// Öffnet den Dialog. Rückgabe <c>true</c>, wenn dupliziert wurde —
        /// <c>WinFormsNavigation</c> reicht das als Ergebnis weiter (der Vorläufer
        /// wertete ebenfalls nur das <c>DialogResult</c> aus).
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<ProjektKopieDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<ProjektKopieDialog>(
                Text_("PRJ_KOPIE_TITEL", "Projekt Speichern unter"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Zeilen"] = ProjektCtrl.NamenListe(),
                ["Quellfelder"] = new Func<string, ProjektKopfDaten>(ProjektCtrl.Kopf),
                ["Pruefen"] = new Func<string, string, DuplizierBefund>(
                    (quelle, neu) => new ProjektDuplizierenCtrl().PruefeNamen(quelle, neu)),
                ["Duplizieren"] = new Func<string, string,
                                           IProgress<KopierStand>, CancellationToken, Task<int>>(Duplizieren),
                ["Verwaltungsfelder"] = new Func<string, string, string, string,
                                                 VerwaltungsfelderErgebnis>(Verwaltungsfelder),

                ["TitelText"] = Text_("PRJ_KOPIE_TITEL", "Projekt Speichern unter"),
                ["LabelAuswahl"] = Text_("PRJ_KOPIE_LBL_AUSWAHL", "Projektauswahl:"),
                ["LabelNeuerName"] = Text_("PRJ_KOPIE_LBL_NEUERNAME", "Neuer Projektname:"),
                ["LabelBeschreibung"] = Text_("PRJ_KOPIE_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelKunde"] = Text_("PRJ_KOPIE_LBL_KUNDE", "Kunde:"),
                ["LabelBearbeiter"] = Text_("PRJ_KOPIE_LBL_BEARBEITER", "Bearbeiter:"),

                // A-1: das ❌ des Vorlaeufers ist weg - eine Beschriftung "Abbrechen"
                // neben einer "OK" braucht kein Symbol (Befund W15a-B16).
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["MeldungNameLeer"] = Text_("PRJ_KOPIE_MSG_NAME_LEER",
                    "Bitte einen neuen Projektnamen eingeben."),
                ["MeldungNameBelegt"] = Text_("PRJ_KOPIE_MSG_NAME_BELEGT",
                    "Projektname bereits vorhanden!"),
                ["MeldungQuelleFehlt"] = Text_("PRJ_KOPIE_MSG_QUELLE_FEHLT",
                    "Quellprojekt '{0}' wurde nicht gefunden."),
                ["MeldungFehler"] = Text_("PRJ_KOPIE_MSG_FEHLER", "Fehler beim Speichern unter: {0}"),
                ["MeldungKopieFehlt"] = Text_("PRJ_KOPIE_MSG_KOPIE_FEHLT",
                    "Die Kopie '{0}' wurde nicht gefunden. Beschreibung, Kunde und Bearbeiter "
                    + "wurden nicht übernommen."),
                ["MeldungFelderNicht"] = Text_("PRJ_KOPIE_MSG_FELDER_NICHT",
                    "Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden. "
                    + "Die Projektkopie selbst ist angelegt."),
                ["MeldungFelderFehler"] = Text_("PRJ_KOPIE_MSG_FELDER_FEHLER",
                    "Beschreibung, Kunde und Bearbeiter konnten nicht gespeichert werden: {0}\n"
                    + "Die Projektkopie selbst ist angelegt."),

                ["FortschrittFormat"] = Text_("PRJ_KOPIE_FORTSCHRITT", "Kopiere Tabelle {0}/{1}: {2}"),
                ["FortschrittFertigstellen"] = Text_("PRJ_KOPIE_FERTIGSTELLEN", "Fertigstellen ..."),
                ["FortschrittFertig"] = Text_("PRJ_KOPIE_FERTIG", "Fertig"),

                ["AnzahlFormat"] = Text_("PRJ_LIST_ANZAHL", "{0} von {1} Projekten"),
                ["SpalteName"] = Text_("PRJ_LIST_SP_NAME", "Projektname"),
                ["SpalteKunde"] = Text_("PRJ_LIST_SP_KUNDE", "Kunde"),
                ["SpalteGeaendert"] = Text_("PRJ_LIST_SP_GEAENDERT", "Geändert"),
                ["SucheText"] = Text_("PRJ_LIST_LBL_SUCHE", "Suchen:"),
                ["LeerText"] = Text_("PRJ_LIST_LEER", "Es ist noch kein Projekt angelegt."),

                ["HilfeSchluessel"] = "Form_ProjektSpeichernUnter.btn_Help"
            };
        }

        /// <summary>
        /// Der Kopierlauf im Hintergrund. Der Bedienfaden hat den
        /// <c>Progress&lt;T&gt;</c> erzeugt und bekommt die Meldungen deshalb auf sich
        /// zurück (Hausmuster <c>Form_SpeicherOptimierung</c>).
        /// </summary>
        private static Task<int> Duplizieren(string quelle, string neu,
                                             IProgress<KopierStand> melder, CancellationToken abbruch)
        {
            IProgress<ProjektDuplizierenCtrl.Fortschritt> brueckeninhalt =
                new Progress<ProjektDuplizierenCtrl.Fortschritt>(f =>
                    melder?.Report(new KopierStand(f.Aktuell, f.Gesamt, f.Tabelle ?? "")));

            return Task.Run(() => new ProjektDuplizierenCtrl()
                                      .Duplizieren(quelle, neu, brueckeninhalt, abbruch));
        }

        private static VerwaltungsfelderErgebnis Verwaltungsfelder(
            string neuerName, string beschreibung, string kunde, string bearbeiter)
        {
            VerwaltungsfelderBefund befund = new ProjektDuplizierenCtrl()
                .VerwaltungsfelderSetzen(neuerName, beschreibung, kunde, bearbeiter, out string fehlertext);
            return new VerwaltungsfelderErgebnis(befund, fehlertext ?? "");
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
