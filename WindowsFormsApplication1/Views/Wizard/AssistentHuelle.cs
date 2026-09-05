using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Seiten.Assistent;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Projektassistenten (iU9-W16a.5) — sie löst
    /// <c>WizardParent</c> ab.
    ///
    /// <para><b>Sie ist die Datenseite EINES Assistentenlaufs.</b> Der Ablauf steht
    /// im Kern (<see cref="AssistentCtrl"/>, K3), der Rahmen als Baustein
    /// <c>Assistent</c> und die dreizehn Seiten als <c>AssistentSeite</c>; hier
    /// werden die dreizehn Parametersätze gebaut — dieselben <c>Gaben</c>-Wörterbücher,
    /// die die elf Hüllen schon für <c>BlazorAssistentSeite</c> lieferten.</para>
    ///
    /// <para><b>Modal, weil beide Aufrufer die Rückkehr brauchen.</b>
    /// <c>MenueCtrl.ProjektNeu</c> und <c>…ProjektBearbeiten</c> werten
    /// <c>gespeichert</c> aus; <c>Form_Start</c> und <c>Hauptfensterrahmen</c> ziehen danach
    /// den Projektkontext aus <c>WizardCtrl.Aktueller.Projektname</c> nach. Dieselbe
    /// Begründung wie bei den beiden Simulationsseiten (R‑W10b‑1 / R‑W11‑1).</para>
    ///
    /// <para><b>Der Rückweg „Projekt öffnen".</b> Er stand als
    /// <c>WizardParent.ProjektOeffnenUndSchliessen</c> (:940-960) im Rahmen: Projekt
    /// aktiv setzen, den Namen in <c>WizardCtrl</c> nachziehen, schließen und die
    /// Startmaske kurz melden lassen. Er steht jetzt hier — mit W16b wird aus dem
    /// letzten Schritt ein Rückruf an die Razor-Startseite.</para>
    /// </summary>
    internal static class AssistentHuelle
    {
        /// <summary>Fenstermaß des Assistenten (Vorläufer: 1264 × 900).</summary>
        private static readonly Size MASS = new Size(1264, 900);

        /// <summary>
        /// Zeigt den Assistenten modal und meldet, ob gespeichert wurde — der Ersatz
        /// für <c>WinFormsNavigation.AssistentZeigen</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Assistent erscheint.</param>
        /// <param name="betriebsart">
        /// <see cref="AssistentCtrl.BETRIEBSART_NEU"/> oder <c>…_BEARBEITEN</c>.
        /// </param>
        internal static bool Oeffnen(IWin32Window besitzer, int betriebsart)
        {
            AssistentCtrl ctrl = new AssistentCtrl { Betriebsart = betriebsart };

            BlazorDialogForm<AssistentSeite> dlg = null;
            bool gespeichert = false;

            var werte = new Dictionary<string, object>(Gaben(ctrl))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    gespeichert = ok;
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<AssistentSeite>(Text_("WIZ_TITEL", "Projektassistent"),
                                                       MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }

            // Der Rueckweg "Projekt oeffnen" schliesst den Assistenten OHNE zu
            // speichern; die Startseite meldet den Wechsel danach kurz. Close()
            // blendet den modalen Rahmen nur aus - der Hinweis liegt deshalb erst
            // hier ueber der Startseite und nicht unter dem Assistenten.
            //
            // iU9-W16b.3: Aus dem Aufruf an Program.startfrm ist ein Rueckruf an die
            // RAZOR-Startseite geworden (StartseiteHuelle merkt den Satz vor, die
            // Seite holt ihn beim naechsten Auffrischen ab und zeigt ihn als
            // Warnbanner mit Verfaellt = 3 s - genau die Lebensdauer von Form_Hinweis).
            if (_hinweisFaellig)
            {
                _hinweisFaellig = false;
                StartseiteHuelle.Aktuelle?.HinweisProjektGeoeffnet();
            }

            return gespeichert && ctrl.Gespeichert;
        }

        /// <summary>
        /// Der PARAMETERSATZ der Seite — die Delegaten der Vermessung § 12.8 (Laden,
        /// Speichern, Seite schalten) samt den Texten.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(AssistentCtrl ctrl)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));

            // Die geteilten Listen der dreizehn Seiten. Sie leben so lange wie der
            // Lauf; die Seiten bearbeiten sie an Ort und Stelle.
            List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> komponenten =
                new List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile>();

            string[] gewaehlterName = { "" };

            return new Dictionary<string, object>
            {
                ["Betriebsart"] = ctrl.Betriebsart,
                ["Projekte"] = Projektliste(),

                ["SeiteAktiv"] = new Func<int, bool>(ctrl.SeiteAktiv),

                ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(
                    nr => Seitengaben(ctrl, nr, komponenten, gewaehlterName[0])),

                ["SeiteVerlassen"] = new Action<int>(nr => SeiteVerlassen(ctrl, nr)),

                ["ProjektMarkiert"] = new Action<int, string>((id, name) =>
                {
                    // Woertlich ucProjektAuswahl_MarkierungGeaendert: Bestand neu
                    // lesen, Seiten danach stellen, Ladekennzeichen zuruecksetzen.
                    ctrl.ProjektId = id;
                    gewaehlterName[0] = name ?? "";
                    KomponentenauswahlHuelle.Gaben(id, komponenten, ctrl.Betriebsart,
                                                   ctrl.SeiteSchalten);
                    ctrl.BereitsGeladen = false;
                }),

                ["ProjektOeffnen"] = new Action<int, string>(ProjektOeffnen),

                ["Speichern"] = new Func<(string Text, string Titel)?>(() =>
                {
                    AssistentErgebnis e = ctrl.Speichern();
                    if (e.Erfolg) return null;
                    return (AssistentCtrl.Meldungstext(e), AssistentCtrl.Meldungstitel(e));
                }),

                ["AbbrechenText"] = Text_("WIZ_BTN_ABBRECHEN", "Abbrechen"),
                ["ZurueckText"] = Text_("WIZ_BTN_ZURUECK", "◀ Zurück"),
                ["WeiterText"] = Text_("WIZ_BTN_WEITER", "Weiter ▶"),
                ["SpeichernText"] = Text_("WIZ_BTN_SPEICHERN", "Speichern"),
                ["ProjektLabelText"] = Text_("WIZ_LBL_PROJEKT", "Bestehendes Projekt auswählen"),
                ["ProjektOeffnenText"] = Text_("WIZ_BTN_PROJEKT_OEFFNEN", "Projekt öffnen"),

                // W15a-E-1: Das linke Band zeigt nur den Namen; die Variantenherkunft
                // steht dort als leise Zeile darunter (keine Artspalte, kein Platz).
                ["ArtVarianteText"] = Text_("PRJ_LIST_ART_VARIANTE", "Variante"),
                ["VarianteVonFormat"] = Text_("PRJ_LIST_VARIANTE_VON", "Variante von {0}")
            };
        }

        // =================================================================================
        // Der Ablauf
        // =================================================================================

        /// <summary>
        /// Ein Schritt wird verlassen — wörtlich <c>WizardParent.Next</c> (:275-283):
        /// Beim Verlassen des Projektkopfes wandern seine sieben Felder in den
        /// Projektsatz, und beim ERSTEN Durchgang laufen die sechs Ladewege.
        /// </summary>
        private static void SeiteVerlassen(AssistentCtrl ctrl, int nr)
        {
            if (nr != WizardItemClass.PROJEKT_ITEM) return;

            ctrl.ProjektkopfUebernehmen();
            if (!ctrl.BereitsGeladen) ctrl.Laden(ctrl.Projekt.m_szProjektname);
        }

        /// <summary>
        /// Der Parametersatz EINER Seite. Die Zuordnung Nummer → Hülle ist die
        /// bitgleiche Übernahme von <c>AssistentSeiten.ERZEUGER</c>.
        /// </summary>
        private static IReadOnlyDictionary<string, object> Seitengaben(
            AssistentCtrl ctrl, int nr,
            List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> komponenten,
            string projektName)
        {
            // Woertlich WizardParent.Next (:322-325): Im Neu-Zweig bekommt der Lauf
            // vor JEDEM Seitenaufbau eine geratene Id, an der die Auswahl-Dialoge
            // ihre noch ungespeicherten Zeilen aufhaengen.
            if (ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_NEU)
                ctrl.ProjektId = new ProjektCtrl().GetMaxID() + 1;

            int id = ctrl.ProjektId;
            string name = ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_BEARBEITEN
                ? projektName ?? "" : "";

            switch (nr)
            {
                case WizardItemClass.KOMPONENTEN_ITEM:
                    return KomponentenauswahlHuelle.Gaben(id, komponenten, ctrl.Betriebsart,
                                                          ctrl.SeiteSchalten);

                case WizardItemClass.PROJEKT_ITEM:
                    // NameAenderbar wird VOR dem Bestuecken gesetzt - der Ersatz fuer
                    // SetEditProjektName(bool): Bearbeiten heisst "Name steht fest".
                    ctrl.Kopf[0].NameAenderbar =
                        ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_NEU;
                    return ProjektKopfHuelle.Gaben(name, ctrl.Kopf);

                case WizardItemClass.GEBAEUDE_ITEM:
                    return GebaeudeHuelle.Gaben(null, id, name, ctrl.Gebaeude,
                                                wizard: true, admin: false);

                case WizardItemClass.WAERMEBEDARF_ITEM:
                    return WaermebedarfExternHuelle.Gaben(null, id, name, ctrl.Waermebedarf,
                                                          wizard: true);

                case WizardItemClass.PROZESS_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenProzess(id, ctrl.Prozess);

                case WizardItemClass.STROMSTD_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenStrom(id, ctrl.Stromverbraucher);

                case WizardItemClass.STROMLASTGANG_ITEM:
                    return StromganglinieHuelle.Gaben(id, ctrl.Stromganglinie, wizard: true);

                case WizardItemClass.WP_ITEM:
                    return WaermepumpenHuelle.Gaben(null, id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SOLAR_ITEM:
                    return SolarkollektorHuelle.ProjektGaben(id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.PV_ITEM:
                    return PhotovoltaikHuelle.Gaben(null, id, WizardItemClass.PV_TYP,
                                                    ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SP_ITEM:
                    return StromspeicherHuelle.Gaben(null, id, WizardItemClass.SP_TYP,
                                                     ctrl.Erzeuger, wizard: true);

                case WizardItemClass.KESSEL_ITEM:
                    return HeizkesselHuelle.Gaben(null, id, WizardItemClass.KESSEL_TYP,
                                                  ctrl.Erzeuger, wizard: true);

                case WizardItemClass.BHKW_ITEM:
                    return BhkwHuelle.Gaben(null, id, WizardItemClass.BHKW_TYP,
                                            ctrl.Erzeuger, wizard: true);

                default:
                    return null;
            }
        }

        // =================================================================================
        // Der Rueckweg "Projekt oeffnen"
        // =================================================================================

        private static bool _hinweisFaellig;

        /// <summary>
        /// Setzt das gewählte Projekt aktiv und schließt den Assistenten — wörtlich
        /// <c>WizardParent.ProjektOeffnenUndSchliessen</c> (:940-960).
        ///
        /// <para><b>Kein Detailformular</b> (Nutzerwunsch 30.08.2026): Der Anwender
        /// wollte an dieser Stelle nur wechseln, nicht bearbeiten.</para>
        /// </summary>
        private static void ProjektOeffnen(int id, string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return;

            if (!Program.menuectrl.ProjektAktivSetzen(name, id)) return;

            // Der Assistent wird von zwei Stellen aus gestartet, die nach seinem
            // Schliessen den Projektkontext aus WizardCtrl.Aktueller.Projektname
            // nachziehen. Das Feld haelt den zuletzt GESPEICHERTEN Namen und wird
            // beim Start nicht geleert - ohne diese Zeile holte der Nachzug ein
            // frueher gespeichertes Projekt zurueck.
            if (WizardCtrl.Aktueller != null) WizardCtrl.Aktueller.Projektname = name;

            _hinweisFaellig = true;

            // Das Fenster schliesst der Rueckruf der Komponente (Geschlossen(false)) -
            // hier steht nur, was der Vorlaeufer VOR dem Close() tat.
            SchliesseAssistent();
        }

        private static void SchliesseAssistent()
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f is BlazorDialogForm<AssistentSeite> dlg)
                {
                    dlg.Schliessen(false);
                    return;
                }
            }
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Die Projekte des linken Bandes — dieselbe Liste wie in W15a.</summary>
        private static IReadOnlyList<ProjektKopfZeile> Projektliste()
        {
            try { return ProjektCtrl.NamenListe(); }
            catch (Exception ex)
            {
                Console.WriteLine("Projektliste konnte nicht gelesen werden: " + ex.Message);
                return new List<ProjektKopfZeile>();
            }
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
