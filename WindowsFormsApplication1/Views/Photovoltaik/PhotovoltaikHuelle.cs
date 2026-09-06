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
    ///
    /// <para><b>W6-O-5</b> (Anwenderentscheid 05.09.2026): Die zwei Leistungsfelder
    /// tragen ihre wahre Einheit. <c>Tab_PV.Leistung</c> ist WATT je Modul
    /// („Modul Leistung [W]"), die Gesamtleistung erscheint in kW
    /// („Gesamtleistung [kW]"). Geändert ist nur die ANZEIGE — der Rechenweg
    /// (<c>AnlagenKwp</c>, Simulation, Wirtschaftlichkeit) ist unberührt.</para>
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
            var wrStamm = new WechselrichterStammCtrl();

            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                zeilen.Add(ZeileZu(m));
                zuModell[m.ID] = m;
            }

            // Stufe S2: die PROJEKTKOPIEN der Wechselrichter, an denen die Ampel rechnet.
            // Sie werden nach jedem Uebernehmen aus dem Katalog neu gezogen - eine frisch
            // kopierte Zeile muss die Pruefung sofort sehen.
            var wrKopien = new WechselrichterCtrl();
            wrKopien.ReadAll(projektId);

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

                        // Stufe S2 (W6-E-3): der SICHTBARE Wechselrichterweg. "Mit
                        // Wechselrichter" wird ausdruecklich gesetzt; zurueck auf
                        // "vereinfacht" nur, wenn der Bestand den Katalogweg trug - NULL
                        // ("nie gewaehlt") bleibt NULL, damit ein Speichern ohne
                        // Entscheidung keine Entscheidung erfindet.
                        m.PV_Wechselrichterweg = zeile.MitWechselrichter
                            ? DbWerte.PV_WR_WEG_KATALOG
                            : (string.Equals(m.PV_Wechselrichterweg, DbWerte.PV_WR_WEG_KATALOG,
                                             StringComparison.Ordinal)
                                   ? DbWerte.PV_WR_WEG_VEREINFACHT
                                   : m.PV_Wechselrichterweg);

                        // Die Straenge reisen auf dem Modell mit; geschrieben werden sie
                        // in WizardCtrl.Add_WP_Waermeerzeuger, wo die frische Anlagen-Id
                        // entsteht (Begruendung bei WErzeugerModel.PV_Straenge).
                        m.PV_Straenge = StraengeZuModell(zeile.Straenge);
                    }),

                // W6-O-5 (Anwenderentscheid 05.09.2026): Die Summe ist in WATT -
                // die Anzeige in kW. Die Wandlung macht der Kern
                // (PhotovoltaikCtrl.GesamtleistungText), damit sie neben
                // KwpSumme steht und nicht daneben.
                ["Gesamtleistung"] = new Func<string>(
                    () => PhotovoltaikCtrl.GesamtleistungText(
                              GesamtleistungWatt(idType, modelle))),
                // Paket B (Merge 5): kWp der Anlage fuer die DC/AC-Anzeige des Wechselrichter-
                // dialogs - Modulleistung (W) mal Anzahl, wie Form_PV.btn_Wechselrichter_Click.
                ["AnlagenKwp"] = new Func<ErzeugerZeile, double>(zeile =>
                {
                    PhotovoltaikStammCtrl.ModulDetail d = PhotovoltaikStammCtrl.Detail(zeile.Bezeichner);
                    return d == null ? 0.0 : d.Leistung * (zeile.AnzahlModule ?? 0) / 1000.0;
                }),

                ["KatalogLoeschen"] = new Func<int, bool>(id => stamm.Delete(id)),

                // --- Wechselrichter und Straenge, Stufe S2 (W6-E-2 und W6-E-3) -------
                // Die Klappliste zeigt den KATALOG; uebernommen wird beim Waehlen, wie
                // bei einem Modul.
                ["Wechselrichter"] = WechselrichterEintraege(wrStamm, ""),

                ["WechselrichterUebernehmen"] = new Func<int, GeraetWahl>(
                    stammId => WechselrichterUebernehmen(projektId, stammId, wrKopien)),

                // W6-O-4 (Anwenderentscheid 06.09.2026): der Herstellerfilter UEBER der
                // Strangtabelle - dieselben zwei Gaben wie ueber der Modulliste
                // (Hersteller + Filtern). Er ist vom MODULfilter unabhaengig: "Hersteller
                // kann vom Modul verschieden sein."
                ["WechselrichterHersteller"] = WechselrichterHersteller(),

                ["WechselrichterFiltern"] =
                    new Func<string, IReadOnlyList<(int Id, string Text)>>(
                        hersteller => WechselrichterEintraege(wrStamm, hersteller)),

                // W6-O-6: die Modulspalte je Strang. Die Klappliste zeigt den
                // MODULKATALOG, die Strangzeile traegt die Projektkopie - genau wie
                // beim Wechselrichter.
                ["Strangmodule"] = ModulEintraege(stamm),

                ["ModulUebernehmen"] = new Func<int, GeraetWahl>(
                    stammId => ModulUebernehmen(projektId, stammId)),

                // W6-O-5: die GEWAEHLTE Projektzeile geht mit - sie sagt, gegen welches
                // Modul die Ampel prueft.
                ["StraengePruefen"] = new Func<ErzeugerZeile, IReadOnlyList<StrangZeile>, StrangBefund>(
                    (zeile, straenge) => Pruefen(straenge, ModulDer(zeile), wrKopien)),

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
                // Q9: Sobald ein Strang besteht, ist "Anzahl Module" abgeleitet - und
                // sagt es.
                ["LabelAnzahlAbgeleitet"] = Text_("PVS_ANZAHL_ABGELEITET",
                                                  "aus der Strangtabelle"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["LabelBeschreibung"] = Text_("HZKK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelGesamtleistung"] = Text_("PVD_LBL_GESAMTLEISTUNG", "Gesamtleistung [kW]:"),
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
        /// Die Gesamtleistung in WATT (<c>UpdateGesamtleistung</c>, Z. 314): Summe aus
        /// Anzahl Module mal Modulleistung über alle Zeilen dieses Typs.
        /// </summary>
        /// <remarks>
        /// <b>W6-O-5</b> (Anwenderentscheid 05.09.2026, „Gesamtleistung in kW"): Der
        /// Name sagt seither die Einheit an. <c>Tab_PV.Leistung</c> führt die
        /// Modulleistung in Watt — die Summe ist damit Watt, und erst
        /// <see cref="PhotovoltaikCtrl.GesamtleistungText"/> macht daraus die Anzeige
        /// in kW. Die Summe selbst ist unverändert.
        /// </remarks>
        private static double GesamtleistungWatt(int idType, List<WErzeugerModel> modelle)
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
                WrEta100 = m.PV_WrEta100,

                // Stufe S2: der sichtbare Weg (NULL heisst "vereinfacht") und die
                // Straenge dieser Anlage.
                MitWechselrichter = string.Equals(m.PV_Wechselrichterweg,
                                                  DbWerte.PV_WR_WEG_KATALOG, StringComparison.Ordinal),
                Straenge = StraengeZuZeile(m)
            };
        }

        // =================================================================================
        // Wechselrichter und Straenge (Stufe S2, W6-E-2 und W6-E-3)
        // =================================================================================

        /// <summary>
        /// Die Straenge einer Anlage als Zeilen der Maske. Sie stehen bereits auf dem
        /// MODELL, wenn der Dialog in derselben Sitzung schon einmal offen war
        /// (<c>PV_Straenge</c>); sonst kommen sie aus <c>Z_AnlageStrang</c>.
        /// </summary>
        /// <remarks>
        /// <b>Warum erst das Modell.</b> Der Assistent oeffnet die PV-Seite mehrfach,
        /// ohne zwischendurch zu speichern. Laese die Huelle jedes Mal die Datenbank,
        /// waere jede noch nicht gespeicherte Strangzeile beim zweiten Oeffnen fort.
        /// Eine noch nie gespeicherte Anlage hat ausserdem keine Id in der Datenbank -
        /// die Leseabfrage liefert dann eine leere Liste, und das ist richtig.
        /// </remarks>
        private static List<StrangZeile> StraengeZuZeile(WErzeugerModel m)
        {
            var liste = new List<StrangZeile>();

            List<AnlageStrangModel> quelle = m.PV_Straenge
                                             ?? new AnlageStrangCtrl().LesenJeAnlage(m.ID);

            var namen = new Dictionary<int, string>();
            var modulnamen = new Dictionary<int, string>();
            foreach (AnlageStrangModel z in quelle)
            {
                if (z == null) continue;

                int wr = z.ID_Wechselrichter ?? 0;
                if (wr > 0 && !namen.ContainsKey(wr))
                {
                    WechselrichterModel g = new WechselrichterCtrl().ReadSingle(wr);
                    namen[wr] = g == null ? "" : (g.m_szName ?? "");
                }

                // W6-O-6: der abweichende Modultyp. Sein NAME ist das Band zur
                // Klappliste - Katalogsatz und Projektkopie tragen denselben.
                int pv = z.ID_PV ?? 0;
                if (pv > 0 && !modulnamen.ContainsKey(pv))
                {
                    var modul = new PhotovoltaikCtrl();
                    modul.ReadSingle(pv);
                    modulnamen[pv] = modul.rows > 0 ? (modul.m_szName ?? "") : "";
                }

                liste.Add(new StrangZeile
                {
                    Rang = z.Rang,
                    Bezeichner = z.Bezeichner ?? "",
                    WechselrichterId = wr,
                    WechselrichterName = wr > 0 ? namen[wr] : "",
                    ModulId = pv,
                    ModulName = pv > 0 ? modulnamen[pv] : "",
                    Geraetenummer = z.Geraetenummer,
                    Mppt = z.Mppt,
                    ModuleReihe = z.Module_Reihe,
                    StraengeParallel = z.Straenge_Parallel,
                    Neigung = z.Neigung,
                    Azimut = z.Azimut
                });
            }

            return liste;
        }

        /// <summary>
        /// Die Zeilen der Maske zurueck ins Kernmodell. <c>ID_Anlage</c> bleibt 0 - die
        /// setzt der Schreibweg, wenn die Anlagenzeile entstanden ist.
        /// </summary>
        private static List<AnlageStrangModel> StraengeZuModell(IReadOnlyList<StrangZeile> zeilen)
        {
            var liste = new List<AnlageStrangModel>();
            if (zeilen == null) return liste;

            foreach (StrangZeile z in zeilen)
            {
                if (z == null) continue;
                liste.Add(new AnlageStrangModel
                {
                    Rang = z.Rang,
                    Bezeichner = z.Bezeichner ?? "",
                    ID_Wechselrichter = z.WechselrichterId > 0 ? z.WechselrichterId : (int?)null,
                    // 0 wird NIE geschrieben: "das Modul der Anlage" ist NULL.
                    ID_PV = z.ModulId > 0 ? z.ModulId : (int?)null,
                    Geraetenummer = z.Geraetenummer,
                    Mppt = z.Mppt,
                    Module_Reihe = z.ModuleReihe,
                    Straenge_Parallel = z.StraengeParallel,
                    Neigung = z.Neigung,
                    Azimut = z.Azimut
                });
            }
            return liste;
        }

        /// <summary>
        /// Der Geraetekatalog als Klapplisteneintraege (Id = Stammsatz), wahlweise auf
        /// einen Hersteller eingeengt (<b>W6‑O‑4</b>). Leer und „Alle" heben die
        /// Einengung auf — derselbe Steuerwert wie beim Modulfilter.
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> WechselrichterEintraege(
            WechselrichterStammCtrl stamm, string hersteller)
        {
            var liste = new List<(int, string)>();
            foreach (WechselrichterStammCtrl.KatalogZeile z in stamm.Filtern(hersteller))
                liste.Add((z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>
        /// Die Hersteller des WECHSELRICHTERkatalogs, „Alle" voran — Bauart
        /// <see cref="Hersteller"/> ueber der Modulliste (W6‑O‑4).
        /// </summary>
        private static IReadOnlyList<string> WechselrichterHersteller()
        {
            var liste = new List<string> { Text_("HZK_STUFE_ALLE", "Alle") };
            foreach (string h in WechselrichterStammCtrl.Hersteller()) liste.Add(h);
            return liste;
        }

        /// <summary>
        /// Der MODULKATALOG als Klapplisteneintraege (Id = <c>Tab_PV_STAMM.ID</c>) —
        /// die Auswahl der Modulspalte je Strang (<b>W6‑O‑6</b>). Ohne Herstellerfilter:
        /// Die Spalte steht in einer Tabellenzelle, und die Modulliste des Dialogs hat
        /// ihren eigenen Filter gleich daneben.
        ///
        /// <para><b>Warum der KATALOG und nicht die Projektkopien</b> — dieselbe Bauart
        /// wie bei der Wechselrichter-Klappliste: Jede Projektkopie traegt den
        /// <c>Bezeichner</c> ihres Katalogsatzes, und ueber genau diesen Namen findet
        /// <c>CopyFromStamm</c> eine vorhandene Kopie wieder, statt eine zweite
        /// anzulegen. Die Liste zeigt damit auch jedes Modul, das im Projekt schon
        /// liegt. Nur der Sonderfall „Katalogsatz geloescht, Projektkopie noch da" fehlt
        /// darin — den haelt die Komponente selbst offen und zeigt den Namen der Zeile
        /// weiter an (<c>PvStraengeFelder.Modulwahl</c>).</para>
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> ModulEintraege(
            PhotovoltaikStammCtrl stamm)
        {
            var liste = new List<(int, string)>();
            foreach (PhotovoltaikStammCtrl.KatalogZeile z in stamm.Filtern(""))
                liste.Add((z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>
        /// Nimmt einen MODUL-Katalogsatz in das Projekt auf — <c>CopyFromStamm</c>, wie
        /// beim Wechselrichter (W6‑O‑6). Zurueck kommt die Projektkopie samt ihrem
        /// Bezeichner; er ist das Band zur Klappliste.
        /// </summary>
        private static GeraetWahl ModulUebernehmen(int projektId, int stammId)
        {
            int id = new PhotovoltaikCtrl().CopyFromStamm(stammId, projektId);
            if (id <= 0) return new GeraetWahl(0, "");

            return new GeraetWahl(id, PhotovoltaikStammCtrl.BezeichnerZu(stammId));
        }

        /// <summary>
        /// Nimmt einen Katalogsatz in das Projekt auf - <c>CopyFromStamm</c>, genau wie
        /// bei einem Modul (Konzept 7). Der Zwischenspeicher der Projektkopien wird
        /// danach neu gezogen, damit die Ampel das frische Geraet sofort sieht.
        /// </summary>
        private static GeraetWahl WechselrichterUebernehmen(int projektId, int stammId,
                                                            WechselrichterCtrl kopien)
        {
            int id = new WechselrichterCtrl().CopyFromStamm(stammId, projektId);
            if (id <= 0) return new GeraetWahl(0, "");

            kopien.ReadAll(projektId);
            return new GeraetWahl(id, WechselrichterStammCtrl.BezeichnerZu(stammId));
        }

        /// <summary>
        /// Das MODUL der GEWAEHLTEN Projektzeile, gegen das P1 bis P4 rechnen
        /// (<b>W6‑O‑5</b>, Anwenderentscheid 06.09.2026: „Modul der gewaehlten Zeile").
        ///
        /// <para><b>Was sich damit aendert.</b> Bis hierher nahm die Huelle das ERSTE
        /// Modul, das der Katalog kannte — welche Zeile gewaehlt ist, wusste nur die
        /// Komponente. Fuehrt ein Projekt mehrere PV-Zeilen mit VERSCHIEDENEN Modulen,
        /// prueft die Ampel seither gegen das richtige. Der Delegat bekommt die Zeile
        /// dafuer mitgereicht.</para>
        ///
        /// <para>Ohne Zeile oder ohne Katalogsatz bleibt es <c>null</c>, und die Ampel
        /// meldet „das Modul der Anlage fehlt".</para>
        /// </summary>
        private static PhotovoltaikStammCtrl.ModulDetail ModulDer(ErzeugerZeile zeile)
        {
            if (zeile == null) return null;
            return PhotovoltaikStammCtrl.Detail(zeile.Bezeichner);
        }

        /// <summary>
        /// Die AMPEL: Der Kern prueft (<c>StrangPlausibilitaet</c>), die Huelle bildet
        /// das Ergebnis auf die Anzeigezeilen ab. Gerechnet wird hier nichts.
        /// </summary>
        private static StrangBefund Pruefen(IReadOnlyList<StrangZeile> zeilen,
                                            PhotovoltaikStammCtrl.ModulDetail modul,
                                            WechselrichterCtrl kopien)
        {
            var geraete = new Dictionary<int, WechselrichterModel>();
            foreach (WechselrichterModel g in kopien.items)
                if (g != null && !geraete.ContainsKey(g.m_ID)) geraete[g.m_ID] = g;

            int module = 0;
            foreach (StrangZeile z in zeilen) module += z.Modulzahl;

            // W6-O-6: die ABWEICHENDEN Modultypen der Straenge. Der Katalogsatz wird
            // ueber den Bezeichner geholt - dieselbe Quelle wie beim Anlagenmodul, und
            // Projektkopie wie Katalogsatz tragen denselben Namen.
            var strangmodule = new Dictionary<int, PhotovoltaikModel>();
            foreach (StrangZeile z in zeilen)
            {
                if (z == null || z.ModulId <= 0 || strangmodule.ContainsKey(z.ModulId)) continue;
                PhotovoltaikModel m = ModulModell(PhotovoltaikStammCtrl.Detail(z.ModulName));
                if (m != null) strangmodule[z.ModulId] = m;
            }

            StrangPlausibilitaet.Befund b = StrangPlausibilitaet.Pruefe(
                new StrangPlausibilitaet.Gaben
                {
                    Straenge = StraengeZuModell(zeilen),
                    Modul = ModulModell(modul),
                    Module = strangmodule,
                    Geraete = geraete,
                    // P8 vergleicht gegen die ABGELEITETE Zahl: Die Maske schreibt sie
                    // ohnehin in den Anlagenwert zurueck (Q9), und waehrend des
                    // Bearbeitens ist der Anlagenwert noch der alte.
                    AnzahlModuleAnlage = module
                });

            var straenge = new List<Ampelzeile>();
            foreach (StrangPlausibilitaet.Strangbefund s in b.Straenge)
                straenge.Add(new Ampelzeile(Farbe(s.Farbe), s.Satz));

            var chips = new List<Ampelzeile>();
            foreach (StrangPlausibilitaet.Geraetebefund g in b.Geraete)
                chips.Add(new Ampelzeile(Farbe(g.Farbe), g.Satz));

            return new StrangBefund(straenge, chips, b.Modulsumme, b.NaeherungMpp);
        }

        /// <summary>Der Katalogsatz des Moduls als Kernmodell; <c>null</c> bleibt <c>null</c>.</summary>
        private static PhotovoltaikModel ModulModell(PhotovoltaikStammCtrl.ModulDetail d)
        {
            if (d == null) return null;
            return new PhotovoltaikModel
            {
                m_szName = d.Bezeichner,
                m_Leistung = d.Leistung,
                m_U_Mpp = d.UMpp ?? 0,
                m_U_Leerlauf = d.ULeerlauf ?? 0,
                m_I_Kurzschluss = d.IKurzschluss ?? 0,
                m_alpha_SC = d.AlphaSc ?? 0,
                m_beta_OC = d.BetaOc ?? 0
            };
        }

        private static Ampelfarbe Farbe(StrangPlausibilitaet.Ampel a)
        {
            if (a == StrangPlausibilitaet.Ampel.Rot) return Ampelfarbe.Rot;
            if (a == StrangPlausibilitaet.Ampel.Gelb) return Ampelfarbe.Gelb;
            return Ampelfarbe.Gruen;
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
                (Text_("PVD_LBL_LEISTUNG", "Modul Leistung [W]:"), d.Leistung.ToString("F2"))
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
