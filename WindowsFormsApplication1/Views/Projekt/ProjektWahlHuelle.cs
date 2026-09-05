using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Projekt;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Projektauswahl (iU9-W15a.2) — sie löst
    /// <c>Form_ProjektAuswahl</c> („Projekt öffnen") <b>und</b>
    /// <c>Form_ProjektDelete</c> („Projekt löschen") ab.
    ///
    /// <para><b>Zwei Masken, ein Dialog.</b> Beide taten dasselbe: Sie wählten ein
    /// Projekt aus und lieferten <c>(Id, Name)</c>; getan wurde die eigentliche Tat vom
    /// Aufrufer. Der Unterschied ist der Zweck — und damit Titel, Knopftext und die
    /// Sicherheitsabfrage.</para>
    ///
    /// <para><b>Das Rückgabefach bleibt unverändert</b> (Befund W15a-B45): Die Hülle
    /// füllt dieselbe <c>Projektwahl</c>, die <c>WinFormsNavigation.WahlUebernehmen</c>
    /// seit iU5 füllt — an ihr hängt der ganze Projektwechsel.</para>
    /// </summary>
    internal static class ProjektWahlHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer <c>Form_ProjektAuswahl</c>: 564 × 428).</summary>
        private static readonly Size MASS = new Size(760, 560);

        /// <summary>
        /// Öffnet die Auswahl. Rückgabe <c>true</c>, wenn ein Projekt gewählt wurde;
        /// <paramref name="ergebnis"/> trägt dann Id und Name.
        /// </summary>
        /// <param name="besitzer">Das aufrufende Fenster; <c>null</c> = ohne Besitzer.</param>
        /// <param name="zweck">Öffnen oder Löschen.</param>
        /// <param name="vorauswahl">Name, der scharfgestellt werden soll; leer = keiner.</param>
        /// <param name="zuletztGeaendertZuerst">
        /// Sicht „zuletzt geändert zuerst" — der Weg der Startmasken-Kachel
        /// „Zuletzt geöffnet" (<c>Form_ProjektAuswahl.ZuletztGeaendertZuerst</c>).
        /// </param>
        internal static bool Oeffnen(IWin32Window besitzer, ProjektWahlDialog.ProjektZweck zweck,
                                     out ProjektKopfZeile ergebnis,
                                     string vorauswahl = "", bool zuletztGeaendertZuerst = false)
            => Oeffnen(besitzer, zweck, out ergebnis, out _, vorauswahl, zuletztGeaendertZuerst);

        /// <summary>
        /// Dieselbe Auswahl, dazu die Antwort auf die Mehrdeutigkeits-Rückfrage
        /// (iU9-W15a, Entscheid O-3 vom 04.09.2026).
        /// </summary>
        /// <param name="alleGleichenNamens">
        /// <c>true</c> = der Anwender hat dem Löschen ALLER Projekte dieses Namens
        /// ausdrücklich zugestimmt. Regulär <c>false</c> — <c>Tab_Projekt</c> trägt den
        /// eindeutigen Index <c>Projektname</c>.
        /// </param>
        internal static bool Oeffnen(IWin32Window besitzer, ProjektWahlDialog.ProjektZweck zweck,
                                     out ProjektKopfZeile ergebnis, out bool alleGleichenNamens,
                                     string vorauswahl = "", bool zuletztGeaendertZuerst = false)
            => Oeffnen(besitzer, zweck, out ergebnis, out alleGleichenNamens, out _, vorauswahl,
                       zuletztGeaendertZuerst);

        /// <summary>
        /// Wie oben, dazu der Loeschauftrag der Mehrfachauswahl (Nutzerauftrag 02.09.2026,
        /// Merge 5) - null, wenn der Dialog keinen erteilt hat (Oeffnen, Abbruch).
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, ProjektWahlDialog.ProjektZweck zweck,
                                     out ProjektKopfZeile ergebnis, out bool alleGleichenNamens,
                                     out ProjektLoeschauftrag auftrag,
                                     string vorauswahl = "", bool zuletztGeaendertZuerst = false)
        {
            ProjektKopfZeile gewaehlt = null;
            bool alle = false;
            ProjektLoeschauftrag erteilt = null;
            BlazorDialogForm<ProjektWahlDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(zweck, vorauswahl, zuletztGeaendertZuerst))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<ProjektKopfZeile>(new object(), z =>
                {
                    gewaehlt = z;
                    if (dlg != null) dlg.Schliessen(z != null);
                }),

                // Entscheid O-3: Der Dialog fragt nach, WENN der Name mehrere Projekte
                // trifft; die Antwort reicht der Aufrufer an den Kern weiter.
                ["MehrdeutigZugelassen"] = EventCallback.Factory.Create<bool>(new object(), b => alle = b),
                ["LoeschauftragErteilt"] = EventCallback.Factory.Create<ProjektLoeschauftrag>(
                    new object(), a => erteilt = a)
            };

            dlg = new BlazorDialogForm<ProjektWahlDialog>(Titel(zweck), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            ergebnis = gewaehlt;
            alleGleichenNamens = alle;
            auftrag = erteilt;
            return gewaehlt != null && gewaehlt.Id > 0;
        }

        /// <summary>
        /// Der Weg der Sprungtabelle: öffnen und das Ergebnis in das
        /// <see cref="Projektwahl"/>-Fach des Aufrufers legen.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, ProjektWahlDialog.ProjektZweck zweck,
                                     object[] argumente)
        {
            if (!Oeffnen(besitzer, zweck, out ProjektKopfZeile gewaehlt, out bool alleGleichenNamens,
                         out ProjektLoeschauftrag auftrag))
                return false;

            Projektwahl fach = argumente != null && argumente.Length > 0
                ? argumente[0] as Projektwahl
                : null;

            if (fach == null) return true;   // Aufrufer will das Ergebnis nicht

            fach.Id = gewaehlt.Id;
            fach.Name = gewaehlt.Name ?? "";
            fach.AlleGleichenNamens = alleGleichenNamens;
            // Mehrfachauswahl (Merge 5): die Liste in Loeschreihenfolge und der Sicherungswunsch;
            // ohne Auftrag (Einzelwahl) genau das eine Projekt.
            fach.Mehrere = auftrag != null
                ? new List<ProjektKopfZeile>(auftrag.Projekte)
                : new List<ProjektKopfZeile> { gewaehlt };
            fach.SicherungGewuenscht = auftrag != null && auftrag.Sicherung;
            return true;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            ProjektWahlDialog.ProjektZweck zweck, string vorauswahl, bool zuletztGeaendertZuerst)
        {
            bool loeschen = zweck == ProjektWahlDialog.ProjektZweck.Loeschen;

            return new Dictionary<string, object>
            {
                ["Zweck"] = zweck,
                // Mehrfachauswahl und Sicherungskopie (Nutzerauftrag 02.09.2026, Merge 5) - nur beim Loeschen.
                ["Mehrfach"] = loeschen,
                ["SicherungAngeboten"] = loeschen,
                ["HinweisText"] = loeschen ? Text_("PDLG_HINWEIS",
                    "Wählen Sie die zu löschenden Projekte per Häkchen. Ein Stammprojekt nimmt seine "
                    + "Varianten mit. Das Löschen ist unwiderruflich.") : "",
                ["AlleText"] = Text_("PDLG_ALLE", "Alle sichtbaren auswählen"),
                ["KeineText"] = Text_("PDLG_KEINE", "Auswahl aufheben"),
                ["SicherungText"] = Text_("PDLG_SICHERUNG", "Sicherungskopie der Datenbank vor dem Löschen anlegen"),
                ["AusgewaehltFormat"] = Text_("PA_AUSGEWAEHLT", "{0} ausgewählt"),
                ["FrageMehrereFormat"] = Text_("PDLG_RUECKFRAGE",
                    "{0} Projekt(e) werden mit allen zugehörigen Daten unwiderruflich gelöscht:"),
                ["VarianteText"] = Text_("PDLG_VARIANTE", "Variante"),
                ["WeitereFormat"] = Text_("PDLG_WEITERE", "… und {0} weitere"),
                ["Zeilen"] = ProjektCtrl.NamenListe(),
                ["Vorauswahl"] = vorauswahl ?? "",
                ["SortSpalte"] = zuletztGeaendertZuerst
                                    ? ProjektListe.SPALTE_GEAENDERT
                                    : ProjektListe.SPALTE_NAME,
                ["SortAbsteigend"] = zuletztGeaendertZuerst,

                ["TitelText"] = Titel(zweck),
                ["OkText"] = loeschen
                                ? Text_("PRJ_DEL_BTN_LOESCHEN", "Löschen")
                                : MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["FrageTitel"] = Text_("PRJ_DEL_FRAGE_TITEL", "Projekt löschen bestätigen"),
                ["FrageFormat"] = loeschen ? Text_("PRJ_DEL_FRAGE",
                    "Sind Sie sicher, dass Sie das Projekt '{0}' und alle dazugehörigen Daten "
                    + "unwiderruflich löschen möchten?") : "",

                // Entscheid O-3 vom 04.09.2026: Trifft der Name MEHRERE Projekte, wird
                // gefragt statt still beide zu loeschen. Die Zaehlung ist DIESELBE, mit
                // der ProjektCtrl.LoeschenMitVorarbeiten abbricht - die Komponente
                // ruehrt keine Datenbank an (Hausregel EPOS.UI).
                ["NamensAnzahl"] = loeschen ? (Func<string, int>)ProjektCtrl.AnzahlGleicherNamen : null,
                ["MehrdeutigTitel"] = Text_("PROJ_MSG_NAME_MEHRDEUTIG_TITEL",
                    "Projektname mehrfach vergeben"),
                ["MehrdeutigFormat"] = loeschen ? Text_("PROJ_MSG_NAME_MEHRDEUTIG",
                    "Der Projektname „{0}“ ist {1}-mal vergeben. Alle {1} Projekte werden "
                    + "gelöscht. Fortfahren?") : "",

                ["MeldungKeineWahl"] = MyResource.Resource.Text_Select,

                ["AnzahlFormat"] = Text_("PRJ_LIST_ANZAHL", "{0} von {1} Projekten"),
                ["SpalteName"] = Text_("PRJ_LIST_SP_NAME", "Projektname"),
                ["SpalteKunde"] = Text_("PRJ_LIST_SP_KUNDE", "Kunde"),
                ["SpalteGeaendert"] = Text_("PRJ_LIST_SP_GEAENDERT", "Geändert"),
                ["SpalteArt"] = Text_("PRJ_LIST_SP_ART", "Art"),
                ["ArtStammText"] = Text_("PRJ_LIST_ART_STAMM", "Stamm"),
                ["ArtVarianteText"] = Text_("PRJ_LIST_ART_VARIANTE", "Variante"),
                ["VarianteVonFormat"] = Text_("PRJ_LIST_VARIANTE_VON", "Variante von {0}"),
                ["SucheText"] = Text_("PRJ_LIST_LBL_SUCHE", "Suchen:"),
                ["LeerText"] = Text_("PRJ_LIST_LEER", "Es ist noch kein Projekt angelegt."),

                // Form_ProjektDelete hatte als einzige der drei Projektdialoge KEINEN
                // Hilfeknopf (Befund W15a-B8); der Loeschweg fuehrt aber in denselben
                // Wikibereich. Beide bekommen ihn.
                ["HilfeSchluessel"] = loeschen
                                        ? "Form_ProjektDelete.btn_Help"
                                        : "Form_ProjektAuswahl.btn_Help"
            };
        }

        private static string Titel(ProjektWahlDialog.ProjektZweck zweck)
            => zweck == ProjektWahlDialog.ProjektZweck.Loeschen
                ? Text_("PRJ_DEL_TITEL", "Projekt Löschen")
                : Text_("PRJ_WAHL_TITEL", "Projekt öffnen");

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
