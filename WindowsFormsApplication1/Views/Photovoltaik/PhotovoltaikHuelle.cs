using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Photovoltaik-Projektdialogs (iU9-W6.5).
    ///
    /// <para><b>Die einfachste der fünf Erzeugermasken.</b> Kein Trägerdialog, keine
    /// Projektkopie, kein Katalogeditor: Ein Modul wird mit seiner STAMM-Id in die
    /// geteilte Liste gelegt (<c>ID_PV</c>), und die Zeile trägt drei eigene Werte —
    /// Neigung, Azimut und Anzahl Module. Alles, was diese Hülle tut, ist Lesen,
    /// Abbilden und die Gesamtleistung rechnen.</para>
    ///
    /// <para><b>Die Modulverwaltung bleibt WinForms</b> (<c>Form_AdminPV</c>, bis Welle
    /// 14) und geht deshalb über die Sprungbrücke, nicht über eine zweite WebView.</para>
    /// </summary>
    internal static class PhotovoltaikHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 762 × 582).</summary>
        private static readonly Size MASS = new Size(900, 640);

        /// <summary>
        /// Zeigt den Dialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_PV_Click</c> und
        /// <c>PVKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<PhotovoltaikDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<PhotovoltaikDialog>(
                Text_("PVD_TITEL", "Verwaltung Photovoltaik Module"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Die PV-Seite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType,
            List<WErzeugerModel> modelle, bool wizard)
        {
            var stamm = new PhotovoltaikStammCtrl();

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
                ["Wizard"] = wizard,
                ["Hersteller"] = Hersteller(),

                ["Filtern"] = new Func<string, IReadOnlyList<KatalogZeile>>(
                    hersteller => KatalogZeilen(stamm.Filtern(hersteller))),

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

                ["Uebernehmen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        m.m_Neigung = zeile.Neigung ?? 0;
                        m.m_Azimut = zeile.Azimut ?? 0;
                        m.PV_Leistung = zeile.AnzahlModule ?? 0;

                        // Paket A/B des PV-Ertragsmodells (Merge 5, woertlich Form_PV.
                        // UpdateProerties): Anlagenparameter, Wechselrichter, Modellwahl.
                        // ERWEITERT wird ausdruecklich gesetzt; zurueck auf EINFACH nur, wenn
                        // der Bestand erweitert war - NULL (nie gewaehlt) bleibt NULL.
                        m.PV_WrWirkungsgrad = zeile.WrWirkungsgrad;
                        m.PV_Systemverluste = zeile.Systemverluste;
                        m.PV_WrNennleistungKw = zeile.WrNennleistungKw;
                        m.PV_WrEta10 = zeile.WrEta10;
                        m.PV_WrEta50 = zeile.WrEta50;
                        m.PV_WrEta100 = zeile.WrEta100;
                        m.PV_Modell = zeile.ModellErweitert
                            ? DbWerte.PV_MODELL_ERWEITERT
                            : (SimulationPV.IstErweitert(m) ? DbWerte.PV_MODELL_EINFACH : m.PV_Modell);
                    }),

                ["Gesamtleistung"] = new Func<string>(
                    () => Gesamtleistung(idType, modelle).ToString()),
                // Paket B (Merge 5): kWp der Anlage fuer die DC/AC-Anzeige des Wechselrichter-
                // dialogs - Modulleistung (W) mal Anzahl, wie Form_PV.btn_Wechselrichter_Click.
                ["AnlagenKwp"] = new Func<ErzeugerZeile, double>(zeile =>
                {
                    PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(zeile.Bezeichner);
                    return d == null ? 0.0 : d.Leistung * (zeile.AnzahlModule ?? 0) / 1000.0;
                }),

                ["KatalogLoeschen"] = new Func<int, bool>(id => stamm.Delete(id)),

                // Die Modulverwaltung ist bis Welle 14 eine WinForms-Maske.
                // iU9-W14a.3: Der Modulkatalog ist die Razor-Komponente
                // ModulKatalogDialog und erscheint als UEBERLAGERUNG im selben
                // Fenster - der Sprung ueber die Bruecke entfaellt (Risiko R2).
                ["VerwaltungGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    PvAdminHuelle.Gaben),

                ["TitelText"] = Text_("PVD_TITEL", "Verwaltung Photovoltaik Module"),
                ["KopfbandText"] = Text_("PVD_KOPFBAND", "Eingabe der Photovoltaik Anlagendaten"),
                ["LabelProjektliste"] = Text_("PVD_LBL_PROJEKTLISTE", "ausgewählte Module"),
                ["LabelKatalogliste"] = Text_("PVD_LBL_KATALOGLISTE", "Module aus Datenbank"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LabelFilterHersteller"] = Text_("PVD_LBL_FILTER_HERSTELLER", "Filtern nach Hersteller:"),
                ["BtnBearbeitenText"] = Text_("PVD_BTN_BEARBEITEN", "Modul Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("PVD_BTN_LOESCHEN", "Modul Löschen"),
                ["GruppeAnlage"] = Text_("PVD_GRP_ANLAGE", "PV Anlage Eigenschaften:"),
                ["LabelNeigung"] = Text_("PVD_LBL_NEIGUNG", "Neigung [°]:"),
                ["LabelAzimut"] = Text_("PVD_LBL_AZIMUT", "Azimut [°]:"),
                ["LabelAnzahl"] = Text_("PVD_LBL_ANZAHL", "Anzahl Module:"),
                ["GruppeModul"] = Text_("PVD_GRP_MODUL", "Modul Eigenschaften:"),
                // W6-E-1 (Windows-Abnahme 05.09.2026): der Aufklapper ueber allen
                // Modulparametern.
                ["LabelAlleParameter"] = Text_("PVD_AUFKLAPP_PARAMETER",
                                               "Alle Modulparameter anzeigen"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["LabelBeschreibung"] = Text_("HZKK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelGesamtleistung"] = Text_("PVD_LBL_GESAMTLEISTUNG", "Gesamtleistung [KW]:"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["FrageLoeschen"] = Text_("PVD_FRAGE_LOESCHEN", "Wollen Sie wirklich das Modul löschen?"),
                ["TitelLoeschen"] = Text_("HZK_TITEL_LOESCHEN", "Löschen"),
                ["MeldungLoeschFehler"] = Text_("HZK_MSG_LOESCHFEHLER",
                    "Der Katalogeintrag konnte nicht gelöscht werden.")
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Nimmt das Modul auf (<c>btn_Hinzu_Click</c>, Z. 88). Keine Trägervariante,
        /// keine Projektkopie: <c>ID_PV</c> ist die STAMM-Id.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, int idType,
                                                  List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId)
        {
            string bezeichner = PhotovoltaikStammCtrl.BezeichnerZu(stammId);
            if (bezeichner.Length == 0)
                return new AufnahmeErgebnis(null,
                    Text_("PVD_MSG_NICHT_GEFUNDEN",
                          "Das ausgewählte Modul wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_PV = stammId,
                ID_Type = idType,
                Bezeichner = bezeichner
            };

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model));
        }

        /// <summary>
        /// Die Gesamtleistung (<c>UpdateGesamtleistung</c>, Z. 314): Summe aus Anzahl
        /// Module mal Modulleistung über alle Zeilen dieses Typs.
        /// </summary>
        private static double Gesamtleistung(int idType, List<WErzeugerModel> modelle)
        {
            double gesamt = 0;
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;

                PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(m.Bezeichner);
                if (d != null) gesamt += m.PV_Leistung * d.Leistung;
            }
            return gesamt;
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
                GeraetId = m.ID_PV,
                Neigung = m.m_Neigung,
                Azimut = m.m_Azimut,
                AnzahlModule = m.PV_Leistung,
                // Paket A/B des PV-Ertragsmodells (Merge 5)
                WrWirkungsgrad = m.PV_WrWirkungsgrad,
                Systemverluste = m.PV_Systemverluste,
                ModellErweitert = SimulationPV.IstErweitert(m),
                WrNennleistungKw = m.PV_WrNennleistungKw,
                WrEta10 = m.PV_WrEta10,
                WrEta50 = m.PV_WrEta50,
                WrEta100 = m.PV_WrEta100
            };
        }

        /// <summary>
        /// Der Detailblock. Beide Listen lasen im Vorläufer denselben Katalogsatz —
        /// nur das Anlagen-Panel unterschied sie.
        /// </summary>
        /// <remarks>
        /// <b>W6‑E‑1</b> (Windows-Abnahme 05.09.2026): Dazu kommen ALLE übrigen
        /// Katalogparameter für den Aufklapper. Sie stehen im SELBEN Lesevorgang —
        /// <c>PhotovoltaikStammCtrl.Detail</c> liest sie seither mit —, und weil der
        /// Dialog diesen Weg bei jedem Wechsel der Modulwahl ruft, aktualisiert sich
        /// der Block von selbst.
        /// </remarks>
        private static ErzeugerDetail DetailZu(string name)
        {
            PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(name);
            if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            var felder = new List<(string, string)>
            {
                (Text_("PVD_LBL_HERSTELLER", "Hersteller:"), d.Firma),
                (Text_("PVD_LBL_LEISTUNG", "Modul Leistung [KW]:"), d.Leistung.ToString("F2"))
            };

            return new ErzeugerDetail(d.Bezeichner, d.Beschreibung, felder,
                                      null, Parameterzeilen(d));
        }

        /// <summary>
        /// Die dreizehn übrigen Katalogfelder als Anzeigezeilen (W6‑E‑1). Beschriftung,
        /// Einheit, Zahlenform und das „–" für einen nicht gepflegten Wert entscheidet
        /// der Kern — die Hülle bildet nur ab.
        /// </summary>
        private static IReadOnlyList<Modulparameter> Parameterzeilen(
            PhotovoltaikStammCtrl.ModulDetail d)
        {
            var liste = new List<Modulparameter>();
            foreach (PhotovoltaikStammCtrl.ModulParameter p in
                     PhotovoltaikStammCtrl.Parameterzeilen(d))
                liste.Add(new Modulparameter(p.Bezeichnung, p.Wert, p.Einheit));
            return liste;
        }

        private static IReadOnlyList<KatalogZeile> KatalogZeilen(
            IReadOnlyList<PhotovoltaikStammCtrl.KatalogZeile> quelle)
        {
            var liste = new List<KatalogZeile>();
            foreach (var z in quelle) liste.Add(new KatalogZeile(z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>„Alle" voran, dann die Hersteller — wie <c>Form_PV_Load</c>.</summary>
        private static IReadOnlyList<string> Hersteller()
        {
            var liste = new List<string> { Text_("HZK_STUFE_ALLE", "Alle") };
            foreach (string h in PhotovoltaikStammCtrl.Hersteller())
                if (h.Length > 0) liste.Add(h);
            return liste;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// Der Zeilenzähler eines Dialoglaufs — das Gegenstück zu <c>startindex</c> des
        /// Vorläufers.
        /// </summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }
    }
}
