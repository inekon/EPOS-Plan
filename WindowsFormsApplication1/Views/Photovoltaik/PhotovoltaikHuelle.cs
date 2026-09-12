using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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

            // W6-B-8: die KATALOGSAETZE als Kernmodelle, an denen die Auslegungshilfe
            // rechnet. Sie werden EINMAL je Dialoglauf gelesen (siehe Geraetespeicher).
            var wrKatalog = new Geraetespeicher();

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

            // Stufe S2: die PROJEKTKOPIEN der Wechselrichter, an denen die Ampel rechnet.
            // Sie werden nach jedem Uebernehmen aus dem Katalog neu gezogen - eine frisch
            // kopierte Zeile muss die Pruefung sofort sehen.
            var wrKopien = new WechselrichterCtrl();
            wrKopien.ReadAll(projektId);

            // W6-B-11 (09.09.2026): die zwei AUSLEGUNGSTEMPERATUREN des Projekts. Sie
            // werden EINMAL gelesen und hier gehalten; "AuslegungstemperaturenSetzen"
            // schreibt sie in die Einstellungen UND legt den neuen Stand hier ab, damit
            // die naechste Ampelpruefung sofort mit ihm rechnet. Ein zweites Lesen je
            // Tastendruck waere eine Abfrage fuer eine Zahl, die die Maske gerade selbst
            // gesetzt hat.
            Auslegungstemperaturen temperaturen =
                KonfigurationCtrl.AuslegungstemperaturenLesen(projektId);

            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                // W14a-E-10 / S2.1: DAS PROFIL statt der Herstellerklappliste, plus
                // die Spalte "im Projekt verwendet" (Q12). Bei 20 749 CEC-Modulen ist
                // das der Unterschied zwischen einer Klappliste mit 258 Eintraegen
                // und einem Feld "enthaelt ...".
                ["Katalogprofil"] = Katalogfilterprofil.MitVerwendung(
                    Anlagenart.Photovoltaik, Text_),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Photovoltaik, n, Text_)),

                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    PhotovoltaikStammCtrl.Katalogfilterzeilen),

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

                // W6-B-8 (Anwenderwunsch 08.09.2026): die Auslegungshilfe in der
                // Oberflaeche. Die Klappliste ueber der Strangtabelle steht in der
                // Reihenfolge von StrangAuslegung.GeraeteBewerten und traegt DC/AC und
                // Geraetezahl im Text; der Knopf "Auslegung vorschlagen" fuellt die
                // Tabelle aus Vorschlagen + Aufteilen. Gerechnet wird im KERN,
                // formatiert HIER - die Komponente zeigt nur.
                ["WechselrichterBewerten"] =
                    new Func<ErzeugerZeile, string, IReadOnlyList<(int Id, string Text)>>(
                        (zeile, hersteller) => Bewerten(wrStamm, wrKatalog, zeile, hersteller)),

                ["AuslegungVorschlagen"] = new Func<ErzeugerZeile, int, StrangVorschlag>(
                    (zeile, stammId) => Auslegen(wrKatalog, zeile, stammId)),

                // W6-B-4: Anzahl_Mppt des KATALOGgeraets - der neue Strang bekommt
                // damit den naechsten freien Tracker statt immer den ersten. Die
                // CEC-Liste fuehrt die Angabe nicht (W6-O-2); dann kommt null zurueck,
                // und es bleibt bei Tracker 1.
                ["Trackerzahl"] = new Func<int, int?>(WechselrichterStammCtrl.TrackerZahl),

                // W6-E-6 (Anwenderentscheid 07.09.2026): der Hersteller des Moduls der
                // gewaehlten Anlage - die VORAUSWAHL des Herstellerfilters. Die Huelle
                // liest nur den Katalogsatz; ob es dazu ein Geraet gibt und was daraus
                // folgt, entscheidet die Komponente an ihrer Herstellerliste.
                ["Modulhersteller"] = new Func<ErzeugerZeile, string>(
                    zeile =>
                    {
                        PhotovoltaikStammCtrl.ModulDetail d = ModulDer(zeile);
                        return d == null ? "" : (d.Firma ?? "");
                    }),

                // W6-O-6: die Modulspalte je Strang. Die Klappliste zeigt den
                // MODULKATALOG, die Strangzeile traegt die Projektkopie - genau wie
                // beim Wechselrichter.
                ["Strangmodule"] = ModulEintraege(stamm),

                ["ModulUebernehmen"] = new Func<int, GeraetWahl>(
                    stammId => ModulUebernehmen(projektId, stammId)),

                // W6-O-5: die GEWAEHLTE Projektzeile geht mit - sie sagt, gegen welches
                // Modul die Ampel prueft.
                // W6-B-12: und sie sagt den GESPEICHERTEN Anlagenwert, gegen den P8
                // prueft - nicht mehr die abgeleitete Summe.
                ["StraengePruefen"] = new Func<ErzeugerZeile, IReadOnlyList<StrangZeile>, StrangBefund>(
                    (zeile, straenge) => Pruefen(straenge, zeile, ModulDer(zeile), wrKopien,
                                                 temperaturen)),

                // --- W6-B-11: die Auslegungstemperaturen des Projekts ---------------
                ["AuslegungKalt"] = temperaturen.Kalt,
                ["AuslegungHeiss"] = temperaturen.Heiss,
                ["AuslegungKaltVorgabe"] = StrangPlausibilitaet.T_KALT,
                ["AuslegungHeissVorgabe"] = StrangPlausibilitaet.T_HEISS,

                ["AuslegungstemperaturenSetzen"] = new Action<double?, double?>(
                    (kalt, heiss) =>
                    {
                        temperaturen = new Auslegungstemperaturen(kalt, heiss);
                        KonfigurationCtrl.AuslegungstemperaturenSchreiben(projektId, kalt, heiss);
                    }),

                // Gerechnet wird im KERN (AuslegungstemperaturVorschlag): Die
                // Klimareihe liest SolardatenCtrl, und der ist dort internal. Die
                // Huelle steuert nur bei, was der Kern nicht wissen kann - T_NOCT des
                // Anlagenmoduls der GEWAEHLTEN Zeile.
                ["Temperaturvorschlag"] = new Func<ErzeugerZeile, Temperaturvorschlag>(
                    zeile => Temperaturen(projektId, zeile)),

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
                ["BtnBearbeitenText"] = Text_("PVD_BTN_BEARBEITEN", "Modul Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("PVD_BTN_LOESCHEN", "Modul Löschen"),
                ["GruppeAnlage"] = Text_("PVD_GRP_ANLAGE", "PV Anlage Eigenschaften:"),
                ["LabelNeigung"] = Text_("PVD_LBL_NEIGUNG", "Neigung [°]:"),
                ["LabelAzimut"] = Text_("PVD_LBL_AZIMUT", "Azimut [°]:"),

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
                Bezeichner = bezeichner,
                // ET-5: Vorgabe der Stromtraeger des Projekts.
                ID_Carrier = ErzeugerTraegerHuelle.Standard(projektId)
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
                CarrierId = m.ID_Carrier,
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

        // =================================================================================
        // W6-B-8: die Auslegungshilfe in der Oberflaeche (Anwenderwunsch 08.09.2026)
        // =================================================================================

        /// <summary>
        /// <b>Der Gerätekatalog in der Reihenfolge der AUSLEGUNGSHILFE</b>, beschriftet
        /// mit DC/AC und Gerätezahl — die Klappliste „Wechselrichter aus dem Katalog"
        /// über der Strangtabelle (<b>W6‑B‑8</b>).
        ///
        /// <para><b>Gerechnet wird im Kern</b> (<c>StrangAuslegung.GeraeteBewerten</c>):
        /// passende Geräte zuerst, unter ihnen wenige Geräte vor vielen und DC/AC nahe
        /// der Bandmitte vor entfernterem; unpassende am Ende. Die Hülle bildet nur ab
        /// und formatiert (Hausregel: rechnen tut der Kern, die Komponente zeigt).</para>
        ///
        /// <para><b>Ohne Modul oder ohne Modulzahl bleibt die Liste, wie sie ist</b> —
        /// alphabetisch und unbeschriftet: Ohne diese zwei Angaben hat die Hilfe nichts,
        /// woran sie messen könnte, und eine erfundene Rangfolge wäre schlechter als
        /// gar keine.</para>
        ///
        /// <para>Die Ids bleiben <c>Tab_Wechselrichter_STAMM.ID</c> — Übernehmen
        /// (<c>CopyFromStamm</c>) und Trackerzahl lesen unverändert weiter.</para>
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Bewerten(
            WechselrichterStammCtrl stamm, Geraetespeicher katalog,
            ErzeugerZeile zeile, string hersteller)
        {
            IReadOnlyList<(int Id, string Text)> roh = WechselrichterEintraege(stamm, hersteller);

            PhotovoltaikModel modul = ModulModell(ModulDer(zeile));
            int module = Modulzahl(zeile);
            if (modul == null || module <= 0 || roh.Count == 0) return roh;

            // Der Name kommt aus der KATALOGLISTE, nicht aus dem Modell: Beide tragen
            // denselben Bezeichner, und so bleibt die Liste Zeichen fuer Zeichen die,
            // die der Anwender ohne Bewertung saehe.
            var namen = new Dictionary<int, string>();
            var geraete = new List<WechselrichterModel>();
            foreach (var e in roh)
            {
                WechselrichterModel g = katalog.Modell(e.Id);
                if (g == null || namen.ContainsKey(e.Id)) continue;
                namen[e.Id] = e.Text;
                geraete.Add(g);
            }
            if (geraete.Count == 0) return roh;

            var liste = new List<(int, string)>();
            foreach (StrangAuslegung.Bewertung b in
                     StrangAuslegung.GeraeteBewerten(modul, module, geraete))
            {
                string name;
                if (b.Geraet == null || !namen.TryGetValue(b.Geraet.m_ID, out name)) continue;
                liste.Add((b.Geraet.m_ID, Beschriften(name, b.Vorschlag)));
            }

            // Ein Katalogsatz, den der Zwischenspeicher nicht (mehr) kennt, faellt sonst
            // aus der Klappliste - er kommt unbeschriftet ans Ende.
            foreach (var e in roh)
                if (!namen.ContainsKey(e.Id)) liste.Add(e);

            return liste;
        }

        /// <summary>
        /// Der Klapplisteneintrag eines bewerteten Geräts: „Muster 2500TL — DC/AC 1,10 ·
        /// 1 Gerät" bzw. „Gross 100TL — passt nicht". Zahlen in der Kultur des Anwenders,
        /// DC/AC mit zwei Nachkommastellen wie in der Ampel.
        /// </summary>
        private static string Beschriften(string name, StrangAuslegung.Vorschlag v)
        {
            string zusatz;
            if (v == null || !v.Moeglich)
            {
                zusatz = MyResource.Resource.PVS_BEW_UNPASSEND;
            }
            else
            {
                zusatz = string.Format(CultureInfo.CurrentCulture,
                             MyResource.Resource.PVS_BEW_DCAC, Komma(v.DcAc))
                       + MyResource.Resource.PVS_TRENNER
                       + Geraetezahl(v.Geraete);
            }
            return name + MyResource.Resource.PVS_BEW_TRENNER + zusatz;
        }

        /// <summary>
        /// <b>Der Vorschlag für eine ganze Strangtabelle</b> (<b>W6‑B‑8</b>): Der Kern
        /// rechnet die Aufteilung (<c>StrangAuslegung.Vorschlagen</c>) und legt sie in
        /// Zeilen (<c>Aufteilen</c>, je Gerät und Tracker eine); die Hülle formuliert den
        /// Satz, den die Maske darunter zeigt.
        ///
        /// <para><b>Ohne Aufteilung bleibt die Tabelle stehen</b>, und der Satz nennt den
        /// Grund des Kerns — „Kein Vorschlag: Die Modulzahl lässt sich nicht in gleich
        /// lange Stränge und gleich belegte Geräte teilen."</para>
        /// </summary>
        private static StrangVorschlag Auslegen(Geraetespeicher katalog, ErzeugerZeile zeile,
                                                int stammId)
        {
            PhotovoltaikModel modul = ModulModell(ModulDer(zeile));
            WechselrichterModel geraet = katalog.Modell(stammId);

            StrangAuslegung.Vorschlag v = StrangAuslegung.Vorschlagen(modul, geraet, Modulzahl(zeile));
            if (!v.Moeglich)
                return new StrangVorschlag(false, new List<StrangVorgabe>(),
                    string.Format(CultureInfo.CurrentCulture,
                                  MyResource.Resource.PVS_VORSCHLAG_KEIN, v.Grund));

            // Dieselbe konservative Annahme wie bei der Pruefung P4/P5: Fehlt die Zahl
            // der Tracker (die CEC-Liste fuehrt sie nicht, W6-O-2), wird auf EINEM
            // gerechnet.
            int mppts = geraet != null && geraet.m_Anzahl_Mppt.HasValue
                        && geraet.m_Anzahl_Mppt.Value >= 1
                      ? geraet.m_Anzahl_Mppt.Value : 1;

            var zeilen = new List<StrangVorgabe>();
            foreach (StrangAuslegung.Strangvorgabe g in StrangAuslegung.Aufteilen(v, mppts))
                zeilen.Add(new StrangVorgabe(g.Geraetenummer, g.Mppt, g.ModuleReihe,
                                             g.StraengeParallel));

            return new StrangVorschlag(true, zeilen, Vorschlagsatz(v));
        }

        /// <summary>
        /// „Vorschlag: 2 Geräte, je 1 Strang mit 10 Modulen in Reihe, DC/AC 1,10 — die
        /// Strangtabelle wurde ersetzt." Ein und Mehrzahl haben eigene Schlüssel; „1
        /// Geräte" wäre kein Satz.
        /// </summary>
        private static string Vorschlagsatz(StrangAuslegung.Vorschlag v)
        {
            string straenge = string.Format(CultureInfo.CurrentCulture,
                v.Parallel == 1 ? MyResource.Resource.PVS_VORSCHLAG_STRANG
                                : MyResource.Resource.PVS_VORSCHLAG_STRAENGE,
                Ganz(v.Parallel), Ganz(v.Reihe));

            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_VORSCHLAG,
                                 Geraetezahl(v.Geraete), straenge, Komma(v.DcAc));
        }

        /// <summary>„1 Gerät" oder „3 Geräte" — die Zahl mit ihrem Wort.</summary>
        private static string Geraetezahl(int geraete)
        {
            return string.Format(CultureInfo.CurrentCulture,
                                 geraete == 1 ? MyResource.Resource.PVS_BEW_GERAET
                                              : MyResource.Resource.PVS_BEW_GERAETE,
                                 Ganz(geraete));
        }

        private static string Ganz(int wert) => wert.ToString("N0", CultureInfo.CurrentCulture);

        private static string Komma(double wert) => wert.ToString("N2", CultureInfo.CurrentCulture);

        /// <summary>
        /// Die Modulzahl, gegen die die Auslegungshilfe rechnet: die ABGELEITETE Summe
        /// der Stränge, sobald eine Strangtabelle steht — sonst der Anlagenwert.
        /// Dieselbe Regel wie bei der Ampel (Entscheidungsfrage <b>Q9</b>), damit
        /// Bewertung, Vorschlag und Prüfung auf derselben Zahl stehen.
        /// </summary>
        private static int Modulzahl(ErzeugerZeile zeile)
        {
            if (zeile == null) return 0;
            if (zeile.Straenge == null || zeile.Straenge.Count == 0)
                return (int)Math.Round(zeile.AnzahlModule ?? 0.0, MidpointRounding.AwayFromZero);

            int summe = 0;
            foreach (StrangZeile s in zeile.Straenge) summe += s.Modulzahl;
            return summe;
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
                                            ErzeugerZeile zeile,
                                            PhotovoltaikStammCtrl.ModulDetail modul,
                                            WechselrichterCtrl kopien,
                                            Auslegungstemperaturen temperaturen)
        {
            var geraete = new Dictionary<int, WechselrichterModel>();
            foreach (WechselrichterModel g in kopien.items)
                if (g != null && !geraete.ContainsKey(g.m_ID)) geraete[g.m_ID] = g;


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

                    // W6-B-12 (Anwenderentscheid 09.09.2026): P8 vergleicht gegen den
                    // GESPEICHERTEN Anlagenwert. Bis dahin gab die Huelle hier die
                    // abgeleitete Summe herein - dieselbe Zahl, aus derselben Liste,
                    // nach derselben Formel; P8 konnte deshalb nie anschlagen (Befund
                    // A11 des Pruefberichts). Der Q9-Abgleich der Maske
                    // (BeiStrangaenderung schreibt die Summe zurueck) bleibt: Die
                    // Meldung trifft damit Altdaten und ist nach dem naechsten
                    // Handgriff wieder still.
                    AnzahlModuleAnlage = zeile?.AnzahlModule ?? 0.0,

                    // W6-B-11: die Auslegungstemperaturen des Projekts; null heisst
                    // Vorgabe, und die kennt der Kern selbst.
                    TKalt = temperaturen?.Kalt,
                    THeiss = temperaturen?.Heiss
                });

            var straenge = new List<Ampelzeile>();
            foreach (StrangPlausibilitaet.Strangbefund s in b.Straenge)
                straenge.Add(new Ampelzeile(Farbe(s.Farbe), MitEmpfehlung(s.Satz, s.Empfehlung)));

            var chips = new List<Ampelzeile>();
            foreach (StrangPlausibilitaet.Geraetebefund g in b.Geraete)
                chips.Add(new Ampelzeile(Farbe(g.Farbe), MitEmpfehlung(g.Satz, g.Empfehlung)));

            return new StrangBefund(straenge, chips, b.Modulsumme, b.Werkzeugtipp);
        }

        /// <summary>
        /// <b>Der Vorschlag fuer die zwei Auslegungstemperaturen</b> (<b>W6‑B‑11</b>) —
        /// gerechnet im Kern (<c>AuslegungstemperaturVorschlag.Fuer</c>), der die
        /// Klimareihe des Projekts liest. Die Huelle steuert nur <c>T_NOCT</c> des
        /// Anlagenmoduls bei; ohne gepflegten Wert nimmt der Kern seinen Rueckfall.
        /// </summary>
        private static Temperaturvorschlag Temperaturen(int projektId, ErzeugerZeile zeile)
        {
            PhotovoltaikStammCtrl.ModulDetail d = ModulDer(zeile);
            AuslegungstemperaturVorschlag.Vorschlag v =
                AuslegungstemperaturVorschlag.Fuer(projektId, d?.TNoct);

            return new Temperaturvorschlag(v.Moeglich, v.Kalt, v.Heiss, v.Satz);
        }

        /// <summary>Der Katalogsatz des Moduls als Kernmodell; <c>null</c> bleibt <c>null</c>.</summary>
        /// <summary>
        /// Der Befund und dahinter, was passen wuerde (Auslegungshilfe 08.09.2026,
        /// <c>StrangAuslegung</c>) - mit demselben Trenner wie die Teile des Satzes.
        /// </summary>
        private static string MitEmpfehlung(string satz, string empfehlung)
        {
            return string.IsNullOrEmpty(empfehlung)
                ? satz
                : satz + MyResource.Resource.PVS_TRENNER + empfehlung;
        }

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
        /// <b>Die Katalogsätze der Wechselrichter als Kernmodelle</b> — der
        /// Zwischenspeicher der Auslegungshilfe (<b>W6‑B‑8</b>).
        ///
        /// <para><b>Warum ein Speicher.</b> <c>StrangAuslegung</c> rechnet an den
        /// VOLLEN Katalogsätzen (Spannungsfenster, Ströme, Leistungen); die Klappliste
        /// kennt nur Id und Bezeichner. Die Maske zeichnet nach JEDER Zellenänderung
        /// neu, und die Bewertung läuft dabei jedes Mal — ohne Speicher wären das
        /// ebenso viele <c>SELECT *</c> über den ganzen Gerätekatalog (die CEC-Liste
        /// bringt über zweitausend Sätze mit). Gelesen wird deshalb EINMAL je
        /// Dialoglauf, beim ersten Bedarf: Wer den Katalogweg nie öffnet, liest gar
        /// nichts.</para>
        ///
        /// <para>Ein Satz, der während des Dialogs im Katalog entsteht, fehlt dem
        /// Speicher — er erscheint dann unbeschriftet am Ende der Klappliste
        /// (<see cref="Bewerten"/>) und bleibt wählbar. Der Gerätekatalog ist aus dem
        /// PV-Dialog heraus nicht zu ändern; für den Import gilt ohnehin ein neuer
        /// Dialoglauf.</para>
        /// </summary>
        private sealed class Geraetespeicher
        {
            private Dictionary<int, WechselrichterModel> _satz;

            /// <summary>Der Katalogsatz zu einer Stamm-Id; <c>null</c>, wenn es ihn nicht gibt.</summary>
            internal WechselrichterModel Modell(int stammId)
            {
                if (_satz == null)
                {
                    _satz = new Dictionary<int, WechselrichterModel>();
                    var ctrl = new WechselrichterStammCtrl();
                    ctrl.ReadAll();
                    foreach (WechselrichterModel g in ctrl.items)
                        if (g != null && !_satz.ContainsKey(g.m_ID)) _satz[g.m_ID] = g;
                }

                WechselrichterModel m;
                return _satz.TryGetValue(stammId, out m) ? m : null;
            }
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
