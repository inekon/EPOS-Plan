using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Pufferspeicher-Projektdialogs (iU9-W6.7).
    ///
    /// <para><b>Zwei Besonderheiten gegenüber den vier Schwestern.</b></para>
    /// <list type="number">
    /// <item><b>Die Eindeutigkeitsfrage.</b> Steht derselbe Speicher schon in der Liste,
    /// fragt der Dialog nach, BEVOR er ihn aufnimmt — der Anwender soll die Meldung
    /// sehen, während er den Speicher aufnimmt, nicht erst beim Speichern. Die Antwort
    /// wandert als <c>GeraetekopieErzwingen</c> ins Modell, damit der Schreibweg nicht
    /// ein zweites Mal fragt. Die Prüfung selbst macht <see cref="AnlagenEindeutigkeit"/>
    /// auf der geteilten Liste; sie ist reine Listenarbeit und braucht keine
    /// Datenbank.</item>
    /// <item><b>Zwei Detailquellen.</b> Eine Projektzeile zeigt ihre KOPIE aus
    /// <c>Tab_Pufferspeicher</c> — sie kann anders heißen als die Vorlage („… 600 Liter"
    /// gegen „… 600 Ltr") und im selben Projekt doppelt vorkommen. Eine frisch
    /// hinzugefügte Zeile hat noch keine Kopie; dort steht in <c>ID_PUFFER</c> die
    /// STAMM-Id, und der Rückfall greift.</item>
    /// </list>
    ///
    /// <para><b>Keine Assistentenseite.</b> Der Pufferspeicher steht nicht in den
    /// dreizehn Seiten des Assistenten (FR‑1) — deshalb gibt es hier kein
    /// <c>AssistentSeite()</c>.</para>
    /// </summary>
    internal static class PufferspeicherHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 785 × 553).</summary>
        private static readonly Size MASS = new Size(900, 620);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Pufferspeicher_Click</c> und
        /// <c>PufferSpKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<PufferspeicherDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PufferspeicherDialog>(
                Text_("PSPD_TITEL", "Verwaltung Pufferspeicher"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType, List<WErzeugerModel> modelle)
        {
            var stamm = new PufferSpStammCtrl();

            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                zeilen.Add(ZeileZu(m));
                zuModell[m.ID] = m;
            }

            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Hersteller"] = Hersteller(),
                ["Volumenstufen"] = Volumenstufen(),

                ["Filtern"] = new Func<string, int, IReadOnlyList<KatalogZeile>>(
                    (hersteller, stufe) => KatalogZeilen(stamm.Filtern(hersteller, stufe))),

                ["KatalogDetail"] = new Func<int, ErzeugerDetail>(
                    id => DetailZu(PufferSpStammCtrl.Detail(id))),

                // listBox_PufferSp_SelectedIndexChanged (Z. 231): erst die Projektkopie,
                // dann der Katalogsatz. Frisch hinzugefuegte Zeilen tragen in ID_PUFFER
                // noch die STAMM-Id - die Kopie legt erst WizardCtrl beim Speichern an.
                ["ProjektDetail"] = new Func<int, ErzeugerDetail>(
                    id => DetailZu(PufferSpCtrl.Detail(id, projektId)
                                   ?? PufferSpStammCtrl.Detail(id))),

                ["Dublettenfrage"] = new Func<int, string>(
                    stammId => Dublettenfrage(idType, modelle, stammId)),

                ["Aufnehmen"] = new Func<int, bool, AufnahmeErgebnis>(
                    (stammId, erzwingen) => Aufnehmen(projektId, idType, modelle, zuModell,
                                                      zaehler, stammId, erzwingen)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(zeile.Schluessel);
                    }),

                ["KatalogLoeschen"] = new Func<int, bool>(id => stamm.Delete(id)),

                // Die Speicherverwaltung ist bis Welle 14 eine WinForms-Maske.
                ["Sprung"] = Sprungbruecke.Fuer(besitzer),

                ["TitelText"] = Text_("PSPD_TITEL", "Verwaltung Pufferspeicher"),
                ["KopfbandText"] = Text_("PSPD_KOPFBAND", "Geben Sie die Daten der Pufferspeicher ein"),
                ["LabelProjektliste"] = Text_("PSPD_LBL_PROJEKTLISTE", "ausgewählt im Projekt"),
                ["LabelKatalogliste"] = Text_("PSPD_LBL_KATALOGLISTE", "Pufferspeicher aus Datenbank"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LabelFilterHersteller"] = Text_("PSPD_LBL_FILTER_HERSTELLER", "Filtern nach Hersteller:"),
                ["LabelFilterVolumen"] = Text_("PSPD_LBL_FILTER_VOLUMEN", "Filtern nach Volumen:"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("HZK_BTN_LOESCHEN", "Löschen"),
                ["GruppeModul"] = Text_("HZK_GRP_MODUL", "Modul"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["TitelDublette"] = MyResource.Resource.ANL_DUBLETTE_TITEL,
                ["TitelLoeschen"] = MyResource.Resource.PSP_TITEL_KATALOG_LOESCHUNG,
                ["FrageLoeschen"] = MyResource.Resource.PSP_MELDUNG_KATALOG_LOESCHEN,
                ["MeldungModulWaehlen"] = MyResource.Resource.PSP_MELDUNG_MODUL_WAEHLEN,
                ["MeldungLoeschFehler"] = Text_("HZK_MSG_LOESCHFEHLER",
                    "Der Katalogeintrag konnte nicht gelöscht werden.")
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Die Frage „dasselbe Gerät steht schon in der Liste" — leer, wenn es sie nicht
        /// gibt.
        /// </summary>
        /// <remarks>
        /// <b>Warum der Text hier zusammengesetzt wird.</b>
        /// <c>AnlagenEindeutigkeit.ZweitesGeraetBestaetigen</c> STELLT die Frage über den
        /// statischen Delegaten <c>Frage</c> — das ist im Engine-Modus die Konsole und
        /// unter Windows eine <c>MessageBox</c>. Eine Razor-Komponente will die Frage
        /// selbst stellen (Baustein <c>Rueckfrage</c>), also braucht sie den TEXT, nicht
        /// die Handlung. Er kommt aus denselben zwei Ressourcenschlüsseln.
        /// </remarks>
        private static string Dublettenfrage(int idType, List<WErzeugerModel> modelle, int stammId)
        {
            string bezeichner = PufferSpStammCtrl.Detail(stammId)?.Bezeichner ?? "";
            if (bezeichner.Length == 0) return "";

            if (!AnlagenEindeutigkeit.BereitsInListe(modelle, idType, bezeichner)) return "";

            return string.Format(MyResource.Resource.ANL_DUBLETTE_FRAGE, bezeichner.Trim());
        }

        /// <summary>
        /// Nimmt den Speicher auf (<c>btn_PufferSp_Hinzu_Click</c>, Z. 151): keine
        /// Projektkopie, <c>ID_PUFFER</c> ist die STAMM-Id.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, int idType,
                                                  List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId, bool erzwingen)
        {
            PufferSpStammCtrl.SpeicherDetail satz = PufferSpStammCtrl.Detail(stammId);
            if (satz == null)
                return new AufnahmeErgebnis(null,
                    Text_("PSPD_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Pufferspeicher wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_PUFFER = stammId,
                ID_Type = idType,
                Bezeichner = satz.Bezeichner,

                // Antwort des Anwenders weitergeben - der Schreibweg fragt sonst erneut.
                GeraetekopieErzwingen = erzwingen
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
                GeraetId = m.ID_PUFFER
            };
        }

        /// <summary>
        /// Der Detailblock. Die Zahlen kommen bereits als Text mit einer Nachkommastelle
        /// aus dem Kern (<c>FeldText</c>) — genau wie im Vorläufer.
        /// </summary>
        private static ErzeugerDetail DetailZu(PufferSpStammCtrl.SpeicherDetail d)
        {
            if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            var felder = new List<(string, string)>
            {
                (Text_("PSPD_LBL_HERSTELLER", "Hersteller:"), d.Hersteller),
                (Text_("PSPD_LBL_TYP", "Speichertyp:"), d.Typ),
                (Text_("PSPD_LBL_VERLUSTE", "Bereitschaftsverluste:"), d.Bereitschaftsverluste),
                (Text_("PSPD_LBL_VOLUMEN", "Gesamtvolumen [l]:"), d.Gesamtvolumen),
                (Text_("PSPD_LBL_INVEST", "Investitionskosten [€]:"), d.Investitionskosten)
            };

            return new ErzeugerDetail(d.Bezeichner, "", felder);
        }

        private static IReadOnlyList<KatalogZeile> KatalogZeilen(
            IReadOnlyList<PufferSpStammCtrl.KatalogZeile> quelle)
        {
            var liste = new List<KatalogZeile>();
            foreach (var z in quelle) liste.Add(new KatalogZeile(z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>„Alle" voran, dann die Hersteller des Katalogs.</summary>
        private static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string> { MyResource.Resource.PSP_FILTER_ALLE };
            foreach (string h in PufferSpStammCtrl.Hersteller()) liste.Add(h);
            return liste;
        }

        /// <summary>
        /// Die sechs Volumenstufen in der Reihenfolge von <c>VOLUMEN_SQL</c> — der Index
        /// ist der Steuerwert (Paket 9 / L5, Bestandsfehler B0-10).
        /// </summary>
        private static IReadOnlyList<string> Volumenstufen()
        {
            return new List<string>(PufferSpStammCtrl.VolumenTexte());
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>Der Zeilenzähler eines Dialoglaufs (Vorbild <c>startindex</c>).</summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }
    }
}
