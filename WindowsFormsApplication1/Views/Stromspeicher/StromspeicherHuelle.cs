using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Stromspeicher-Projektdialogs (iU9-W6.6).
    ///
    /// <para><b>Keine neue SQL.</b> Alles, was der Dialog braucht, kann
    /// <see cref="StromspeicherStammCtrl"/> bereits: <c>ReadAll</c> für die Katalogliste
    /// und <c>ReadSingle</c> für den Detailblock. Die Maske hat kein Filter, keine
    /// Projektkopie, keine Trägervariante und keine einzige <c>MessageBox</c>.</para>
    ///
    /// <para><b>Zwei Beschriftungen kommen aus dem Ressourcenkatalog</b>, nicht aus dem
    /// Designer: <c>SP_LABEL_ENERGIE</c> und <c>SP_LABEL_MODULKOSTEN</c>. Der Designer
    /// trug „Energie [kW]" und „Modulkosten" — beides fachlich falsch (Abnahmebefund 1
    /// zum ersten App-Start; <c>Tab_Stromspeicher.Energie</c> ist die nutzbare
    /// Nennkapazität in kWh, <c>Modulkosten</c> der kapazitätsbezogene Satz in €/kWh).
    /// Der Vorläufer korrigierte sie im Code (<c>EinheitenBeschriftungKorrigieren</c>);
    /// hier setzt sie die Hülle ein.</para>
    /// </summary>
    internal static class StromspeicherHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 838 × 501).</summary>
        private static readonly Size MASS = new Size(900, 600);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Stromspeicher_Click</c> und
        /// <c>StromspeicherKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<StromspeicherDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<StromspeicherDialog>(
                Text_("SPD_TITEL", "Verwaltung Stromspeicher"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Die Speicherseite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType,
            List<WErzeugerModel> modelle, bool wizard)
        {
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            // ET-5 (08.09.2026): Die Zeile zeigt den Traeger der Anlage - gespeichert oder,
            // solange keiner gespeichert ist, den Stromtraeger des Projekts (die Vorgabe).
            int stromVorgabe = ErzeugerTraegerHuelle.Standard(projektId);
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                ErzeugerZeile zeile = ZeileZu(m);
                if (zeile.CarrierId <= 0) zeile.CarrierId = stromVorgabe;
                zeilen.Add(zeile);
                zuModell[m.ID] = m;
            }

            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                // W14a-E-10 / S2.1: Der Speicherkatalog bekommt seine ACHT Spalten -
                // er hatte als einziger gar keinen Filter. Dazu die Spalte "im
                // Projekt verwendet" (Q12).
                ["Katalogprofil"] = Katalogfilterprofil.MitVerwendung(
                    Anlagenart.Stromspeicher, Text_),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Stromspeicher, n, Text_)),

                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    StromspeicherStammCtrl.Katalogfilterzeilen),
                ["Detail"] = new Func<string, ErzeugerDetail>(DetailZu),

                ["Aufnehmen"] = new Func<int, AufnahmeErgebnis>(
                    stammId => Aufnehmen(projektId, idType, modelle, zuModell, zaehler, stammId)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(zeile.Schluessel);
                    }),

                // Die Speicherverwaltung ist bis Welle 14 eine WinForms-Maske.
                // iU9-W14a.3: Der Modulkatalog ist die Razor-Komponente
                // ModulKatalogDialog und erscheint als UEBERLAGERUNG im selben
                // Fenster - der Sprung ueber die Bruecke entfaellt (Risiko R2).
                ["VerwaltungGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    StromspeicherAdminHuelle.Gaben),

                ["TitelText"] = Text_("SPD_TITEL", "Verwaltung Stromspeicher"),
                ["KopfbandText"] = Text_("SPD_KOPFBAND", "Geben Sie Daten der Stromspeicher ein"),
                ["LabelProjektliste"] = Text_("SPD_LBL_PROJEKTLISTE", "ausgewählte Stromspeicher:"),
                ["LabelKatalogliste"] = Text_("SPD_LBL_KATALOGLISTE", "Stromspeicher aus Datenbank:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["GruppeModul"] = Text_("HZK_GRP_MODUL", "Modul"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),

                // ET-5 (Anwenderentscheid 08.09.2026): Traegerwahl in der Katalog-Gliederung
                // Gruppe > Art, gespeichert je Anlage; der gewaehlte Traeger wird dem Projekt
                // zugeordnet (ausserhalb des Assistenten).
                ["Traegerkatalog"] = ErzeugerTraegerHuelle.Katalog(),
                ["LabelTraegerGruppe"] = ErzeugerTraegerHuelle.LabelGruppe,
                ["LabelTraegerArt"] = ErzeugerTraegerHuelle.LabelArt,
                ["TraegerWechseln"] = new Action<ErzeugerZeile, int>(
                    (zeile, neu) =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        m.ID_Carrier = neu;
                        ErzeugerTraegerHuelle.Zuordnen(projektId, wizard, neu);
                    }),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Nimmt den Speicher auf (<c>btn_Hinzu_Click</c>, Z. 123): je Klick eine EIGENE
        /// Modellinstanz mit der STAMM-Id in <c>ID_SP</c>.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, int idType,
                                                  List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId)
        {
            var stamm = new StromspeicherStammCtrl();
            stamm.ReadAll();

            StromspeicherModel satz = null;
            foreach (StromspeicherModel s in stamm.items)
                if (s.m_ID == stammId) { satz = s; break; }

            if (satz == null)
                return new AufnahmeErgebnis(null,
                    Text_("SPD_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Stromspeicher wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_SP = satz.m_ID,
                ID_Type = idType,
                Bezeichner = satz.m_szBezeichner,
                // ET-5: Vorgabe der Stromtraeger des Projekts.
                ID_Carrier = ErzeugerTraegerHuelle.Standard(projektId)
            };

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model));
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        private static ErzeugerZeile ZeileZu(WErzeugerModel m)
        {
            return new ErzeugerZeile
            {
                Schluessel = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                CarrierId = m.ID_Carrier,
                GeraetId = m.ID_SP
            };
        }


        /// <summary>
        /// Der Detailblock (<c>listBox_SP_SelectedIndexChanged</c>, Z. 206). Er kommt
        /// IMMER aus dem Katalog — es gibt keine Projektkopie, die abweichen könnte.
        /// </summary>
        private static ErzeugerDetail DetailZu(string name)
        {
            var ctrl = new StromspeicherStammCtrl();
            ctrl.ReadSingle(name);
            if (ctrl.rows == 0) return new ErzeugerDetail("", "", new List<(string, string)>());

            StromspeicherModel s = ctrl.items[0];

            var felder = new List<(string, string)>
            {
                (Text_("SPD_LBL_TYP", "Typ:"), s.m_szTyp ?? ""),
                (Text_("SPD_LBL_LEISTUNG", "Leistung [kW]:"), s.m_Leistung.ToString()),
                (MyResource.Resource.SP_LABEL_ENERGIE, s.m_Energie.ToString()),
                (Text_("SPD_LBL_DEGRADATION", "Degradation [%/a]:"), s.m_Degradation.ToString()),
                (Text_("SPD_LBL_LADEZUSTAND", "Ladezustand [%]:"), s.m_Ladezustand.ToString()),
                (MyResource.Resource.SP_LABEL_MODULKOSTEN, s.m_Modulkosten.ToString())
            };

            return new ErzeugerDetail(s.m_szBezeichner ?? "", "", felder);
        }


        /// <summary>
        /// Der Uebersetzer, den <see cref="Katalogfilterprofil.MitVerwendung"/>
        /// entgegennimmt: Schluessel rein, Text raus — ein fehlender Schluessel bleibt
        /// als Schluessel stehen, damit er auffaellt (Muster
        /// <c>KatalogBrowserProfil.Finde</c>).
        /// </summary>
        private static string Text_(string schluessel)
        {
            return Text_(schluessel, schluessel);
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// Der Zeilenzähler eines Dialoglaufs. Der Vorläufer hatte hier keinen — er legte
        /// die Zeilen ohne <c>ID</c> an. Für die Zuordnung Zeile ↔ Modell braucht die
        /// Hülle einen eindeutigen Schlüssel, sonst wären zwei gleiche Speicher
        /// ununterscheidbar (Abweichung A-18).
        /// </summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }
    }
}
