using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Solarthermie;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Solarthermie-Dialoge (iU9-W7.6 und W7.7).
    ///
    /// <para><b>Eine Datei für beide.</b> Der Katalogeditor
    /// (<see cref="SolarkollektorKatalogDialog"/>) und der Projektdialog
    /// (<c>SolarkollektorenDialog</c>, W7.7) teilen sich ihre Datenseite —
    /// <see cref="SolarkollektorenStammCtrl"/> und <see cref="SolarkollektorenCtrl"/> —,
    /// und der Projektdialog zeigt den Katalogeditor in einer <c>Ueberlagerung</c>.
    /// Zwei Hüllen wären zwei Orte für dieselben Abbildungen; dieselbe Aufteilung wie
    /// bei <see cref="HeizkesselHuelle"/> (W6.1/W6.3).</para>
    ///
    /// <para><b>Die Abbildung zwischen den Welten liegt hier.</b>
    /// <see cref="SolarkollektorKatalogDaten"/> ist der Feldsatz der Oberfläche,
    /// <see cref="SolarkollektorenModel"/> der des Kerns. Die Komponente kennt die
    /// Fachklassen des Kerns nicht, der Kern kennt <c>EPOS.UI</c> nicht — die Hülle ist
    /// der einzige Ort, an dem beide zugleich sichtbar sind.</para>
    /// </summary>
    internal static class SolarkollektorHuelle
    {
        /// <summary>Gewünschtes Innenmaß des Katalogeditors (Vorläufer: 536 × 460).</summary>
        private static readonly Size KATALOG_MASS = new Size(760, 640);

        // =================================================================================
        // W7.6 - Katalogeditor
        // =================================================================================

        /// <summary>
        /// Zeigt den Katalogeditor als eigenes Fenster — der Weg der WinForms-Aufrufer
        /// <c>Form_SolarKollektorenAdmin.btn_Kollektor_DB_Edit_Click</c> und
        /// <c>btn_Kollektor_DB_neu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Editor erscheint.</param>
        /// <param name="name">
        /// Bezeichner des zu ladenden Katalogsatzes; im Modus „Neu" der gewünschte Name,
        /// den der Aufrufer vorher über <see cref="NamensDialogHuelle"/> erfragt hat.
        /// </param>
        /// <param name="neu"><c>true</c> = Modus „Neu" (nur „Speichern" ist aktiv).</param>
        /// <returns><c>true</c>, wenn geschrieben wurde.</returns>
        internal static bool KatalogBearbeiten(IWin32Window besitzer, string name, bool neu)
        {
            bool ok = false;
            BlazorDialogForm<SolarkollektorKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(KatalogGaben(name, neu))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<SolarkollektorKatalogDialog>(
                Text_("SKK_TITEL", "Kollektor Bearbeiten"), KATALOG_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Katalogeditors — für die Anzeige in einer
        /// <c>Ueberlagerung</c> des Projektdialogs (W7.7). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben(string name, bool neu)
        {
            var daten = new SolarkollektorKatalogDaten();

            if (neu)
            {
                // MODE_NEU (SetControls:19, else-Zweig): ein leeres Modell mit dem
                // vorgegebenen Namen. Alle Zahlen stehen dort auf 0 - hier bleiben sie
                // LEER, damit die Pflichtpruefung sie einfordert (A-11).
                daten.Name = name ?? "";
            }
            else
            {
                var ctrl = new SolarkollektorenStammCtrl();
                ctrl.ReadAll("Bezeichner='" + (name ?? "").Replace("'", "''") + "'");
                if (ctrl.rows > 0) AusModell(daten, ctrl.items[0]);
                else daten.Name = name ?? "";
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = neu ? KatalogModus.Neu : KatalogModus.Bearbeiten,

                ["Ueberschreiben"] = new Func<SolarkollektorKatalogDaten, KatalogSpeicherErgebnis>(Ueberschreiben),
                ["Anlegen"] = new Func<SolarkollektorKatalogDaten, string, KatalogSpeicherErgebnis>(Anlegen),

                ["TitelText"] = Text_("SKK_TITEL", "Kollektor Bearbeiten"),
                ["GruppeBezeichnung"] = Text_("SKK_GRP_BEZEICHNUNG", "Bezeichnung"),
                ["GruppeTechnik"] = Text_("SKK_GRP_TECHNIK", "Technische Daten"),
                ["LabelName"] = Text_("SKK_LBL_NAME", "Kollektorname :"),
                ["LabelHersteller"] = Text_("SKK_LBL_HERSTELLER", "Hersteller :"),
                ["LabelBeschreibung"] = Text_("SKK_LBL_BESCHREIBUNG", "Beschreibung :"),
                ["LabelKollektortyp"] = Text_("SKK_LBL_TYP", "Kollektortype :"),
                ["LabelModulflaeche"] = Text_("SKK_LBL_MODULFLAECHE", "Modulfläche :"),
                ["LabelAperturflaeche"] = Text_("SKK_LBL_APERTURFLAECHE", "Aperturfläche :"),
                ["LabelKosten"] = Text_("SKK_LBL_KOSTEN", "Investitionskosten :"),
                ["LabelVorlauf"] = Text_("SKK_LBL_VORLAUF", "Vorlauf:"),
                ["LabelRuecklauf"] = Text_("SKK_LBL_RUECKLAUF", "Rücklauf:"),
                ["BtnUeberschreibenText"] = Text_("SKK_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("SKK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["MeldungNameFehlt"] = Text_("SKK_MSG_NAME_FEHLT", "Bitte einen Kollektorname eingeben!"),
                ["MeldungZahlFehlt"] = Text_("SKK_MSG_ZAHL_FEHLT", "Bitte {0} als Zahl eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // W7.7 - Projektdialog
        // =================================================================================

        /// <summary>Gewünschtes Innenmaß des Projektdialogs (Vorläufer: 825 × 616).</summary>
        private static readonly Size PROJEKT_MASS = new Size(1000, 720);

        /// <summary>
        /// Zeigt den Projektdialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Solarthermie_Click</c> (Zweig Kollektorprofil) und
        /// <c>SolarKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<SolarkollektorenDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                ProjektGaben(projektId, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<SolarkollektorenDialog>(
                Text_("SKV_TITEL", "Eingabe der Solarkollektoren"), PROJEKT_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Die Solarseite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Projektdialogs.</summary>
        internal static IReadOnlyDictionary<string, object> ProjektGaben(
            int projektId, List<WErzeugerModel> modelle, bool wizard)
        {
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();

            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != WizardItemClass.SOLAR_TYP) continue;
                zeilen.Add(ZeileZu(m));
                zuModell[m.ID] = m;
            }

            var zaehler = new Zaehler();
            foreach (WErzeugerModel m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                ["Katalog"] = new Func<IReadOnlyList<KatalogZeile>>(Katalogzeilen),
                ["Detail"] = new Func<string, ErzeugerDetail>(DetailZu),
                ["Modulflaeche"] = new Func<string, double>(ModulflaecheZu),

                ["Aufnehmen"] = new Func<int, AufnahmeErgebnis>(
                    stammId => Aufnehmen(projektId, modelle, zuModell, zaehler, stammId, wizard)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile => Entfernen(projektId, modelle, zuModell, zeile, wizard)),

                ["Uebernehmen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        m.Kollektormodulanzahl = (int)(zeile.AnzahlModule ?? 0);
                        m.m_Neigung = zeile.Neigung ?? 0;
                        m.m_Azimut = zeile.Azimut ?? 0;
                        m.Vorlauf = zeile.Vorlauf ?? 0;
                        m.Ruecklauf = zeile.Ruecklauf ?? 0;
                    }),

                ["EditorGaben"] = new Func<string, bool, IReadOnlyDictionary<string, object>>(KatalogGaben),
                ["KatalogLoeschen"] = new Func<string, bool>(
                    name => new SolarkollektorenStammCtrl().Delete(name)),

                ["TitelText"] = Text_("SKV_TITEL", "Eingabe der Solarkollektoren"),
                ["KopfbandText"] = Text_("SKV_KOPFBAND", "Eingabe der Solarkollektoren"),
                ["LabelProjektliste"] = Text_("SKV_LBL_PROJEKTLISTE", "Auswahl in Projekt:"),
                ["LabelKatalogliste"] = Text_("SKV_LBL_KATALOGLISTE", "Auswahl in DB:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["SpalteEigenschaften"] = Text_("BHKWV_SP_EIGENSCHAFTEN", "Eigenschaften"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["GruppeModul"] = Text_("HZK_GRP_MODUL", "Modul"),
                ["GruppeKollektor"] = Text_("SKV_GRP_KOLLEKTOR", "Kollektor"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["LabelAnzahl"] = Text_("SKV_LBL_ANZAHL", "Modulanzahl:"),
                ["LabelAperturflaeche"] = Text_("SKV_LBL_APERTURFLAECHE", "Aperturfläche [m²]:"),
                ["LabelNeigung"] = Text_("SKV_LBL_NEIGUNG", "Neigung [°]:"),
                ["LabelAzimut"] = Text_("SKV_LBL_AZIMUT", "Azimut [°]:"),
                ["LabelVorlauf"] = Text_("SKK_LBL_VORLAUF", "Vorlauf:"),
                ["LabelRuecklauf"] = Text_("SKK_LBL_RUECKLAUF", "Rücklauf:"),
                ["BtnUebernehmenText"] = Text_("SKV_BTN_UEBERNEHMEN", "Übernehmen"),
                ["BtnKatalogAendernText"] = Text_("SKV_BTN_DB_AENDERN", "Kollektor in DB ändern..."),
                ["BtnKatalogNeuText"] = Text_("SKV_BTN_DB_NEU", "Kollektor in DB neu..."),
                ["BtnKatalogLoeschenText"] = Text_("SKV_BTN_DB_LOESCHEN", "Kollektor in DB löschen"),
                ["FrageLoeschen"] = Text_("SKV_FRAGE_LOESCHEN",
                    "Wollen Sie wirklich den Solarkollektor löschen?"),
                ["MeldungUebernommen"] = Text_("SKV_MSG_UEBERNOMMEN", "Die Angaben sind übernommen."),
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        /// <summary>
        /// „◀" (<c>btn_Hinzzu_Click</c>:193): Vor- und Rücklauf kommen aus dem
        /// Stammsatz, die Modulanzahl steht auf 1, Neigung und Azimut auf 0. Im
        /// PROJEKTMODUS wird der Stammsatz sofort in die Projekttabelle kopiert und die
        /// PROJEKT-Id referenziert; im Assistenten bleibt es bei der Stamm-Id als
        /// Platzhalter — die Kopie macht dort <c>WizardCtrl</c> beim Speichern.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(int projektId, List<WErzeugerModel> modelle,
                                                  Dictionary<int, WErzeugerModel> zuModell,
                                                  Zaehler zaehler, int stammId, bool wizard)
        {
            SolarkollektorenModel stamm = SolarkollektorenStammCtrl.ReadById(stammId);
            if (stamm == null)
                return new AufnahmeErgebnis(null, Text_("SKV_MSG_NICHT_GEFUNDEN",
                    "Der ausgewählte Solarkollektor wurde in den Stammdaten nicht gefunden."), true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                Bezeichner = stamm.m_szKollektorname,
                ID_Type = WizardItemClass.SOLAR_TYP,
                Kollektormodulanzahl = 1,
                m_Azimut = 0,
                m_Neigung = 0
            };

            // W6-E-4 (06.09.2026): Vor- und Ruecklauf kommen aus dem Katalogsatz - aus
            // der EINEN Wahrheit im Kern statt aus einer dritten Abschrift
            // "Vorlauf = (int)stamm.m_Vorlauf". Sie setzt das Paar nur, wenn der
            // Feldsatz noch keines traegt; ein frisches Modell traegt 0/0.
            AnlagenTemperaturen.AusStammsatz(model, stammId);

            if (!wizard && projektId > 0)
            {
                int kopieId = new SolarkollektorenCtrl().CopyFromStamm(stammId, projektId);
                if (kopieId <= 0)
                    return new AufnahmeErgebnis(null, Text_("SKV_MSG_KOPIE_FEHLER",
                        "Der Datensatz konnte nicht in das Projekt übernommen werden."), true);
                model.ID_Solar = kopieId;
            }
            else
            {
                model.ID_Solar = stammId;
            }

            modelle.Add(model);
            zuModell[model.ID] = model;
            return new AufnahmeErgebnis(ZeileZu(model));
        }

        /// <summary>
        /// „▶" (<c>btn_Entfernen_Click</c>:251): Die Projektkopie geht nur mit, wenn
        /// keine zweite Zeile mehr auf sie verweist — zwei gleiche Kollektoren im
        /// Projekt teilen sich EINE Kopie in <c>Tab_Solarkollektoren</c>.
        /// </summary>
        private static void Entfernen(int projektId, List<WErzeugerModel> modelle,
                                      Dictionary<int, WErzeugerModel> zuModell,
                                      ErzeugerZeile zeile, bool wizard)
        {
            if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;

            modelle.Remove(m);
            zuModell.Remove(zeile.Schluessel);

            bool nochReferenziert = false;
            foreach (WErzeugerModel it in modelle)
                if (it.ID_Type == WizardItemClass.SOLAR_TYP && it.ID_Solar == m.ID_Solar)
                { nochReferenziert = true; break; }

            if (!wizard && projektId > 0 && !nochReferenziert)
                new SolarkollektorenCtrl().DeleteFromProjekt(m.Bezeichner, projektId);
        }

        // =================================================================================
        // Abbildungen des Projektdialogs
        // =================================================================================

        private static ErzeugerZeile ZeileZu(WErzeugerModel m)
        {
            return new ErzeugerZeile
            {
                Schluessel = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                GeraetId = m.ID_Solar,
                Vorlauf = m.Vorlauf,
                Ruecklauf = m.Ruecklauf,
                Neigung = m.m_Neigung,
                Azimut = m.m_Azimut,
                AnzahlModule = m.Kollektormodulanzahl
            };
        }

        /// <summary>
        /// Die Katalogzeilen samt der zweiten Spalte — im Vorläufer Firma, Kollektortyp,
        /// Modulfläche und Aperturfläche untereinander (<c>SetDBList</c>:181).
        /// </summary>
        private static IReadOnlyList<KatalogZeile> Katalogzeilen()
        {
            var ctrl = new SolarkollektorenStammCtrl();
            ctrl.ReadAll();

            var liste = new List<KatalogZeile>();
            for (int i = 0; i < ctrl.rows; i++)
            {
                SolarkollektorenModel k = ctrl.items[i];
                liste.Add(new KatalogZeile(k.m_ID, k.m_szKollektorname,
                    k.m_szFirma + "\nKollektortyp: " + k.m_szKollektortyp +
                    "\nModulfläche: " + k.m_Modulfläche + " m²" +
                    "\nAperturfläche: " + k.m_Aperturfläche + " m²"));
            }
            return liste;
        }

        /// <summary>Der Detailblock (<c>ApplySelectedSolar</c>:324) — immer aus dem Katalog.</summary>
        private static ErzeugerDetail DetailZu(string name)
        {
            var ctrl = new SolarkollektorenStammCtrl();
            ctrl.ReadSingle(name);
            if (ctrl.rows == 0) return new ErzeugerDetail("", "", new List<(string, string)>());

            SolarkollektorenModel k = ctrl.items[0];
            var felder = new List<(string, string)>
            {
                (Text_("SKV_LBL_KOLLEKTOR", "Kollektor:"), k.m_szKollektortyp ?? ""),
                (Text_("SKK_LBL_HERSTELLER", "Hersteller :"), k.m_szFirma ?? ""),
                (Text_("SKK_LBL_BESCHREIBUNG", "Beschreibung :"), k.m_szBeschreibung ?? ""),
                (Text_("SKV_LBL_MODULAPERTUR", "Aperturfläche:"), k.m_Aperturfläche.ToString())
            };
            return new ErzeugerDetail(k.m_szKollektorname ?? "", "", felder);
        }

        /// <summary>
        /// Die Fläche EINES Moduls. Der Vorläufer las dafür die Spalte
        /// <c>Aperturflaeche</c> des Stammsatzes — die lokale Variable dort heißt
        /// <c>modulflaeche</c>, gelesen wird aber die Aperturfläche
        /// (<c>ApplySelectedSolar</c>:344). Wörtlich übernommen.
        /// </summary>
        private static double ModulflaecheZu(string name)
        {
            var ctrl = new SolarkollektorenStammCtrl();
            ctrl.ReadSingle(name);
            return ctrl.rows == 0 ? 0 : ctrl.items[0].m_Aperturfläche;
        }

        /// <summary>
        /// Der Zeilenzähler eines Dialoglaufs — dieselbe Rolle wie im Stromspeicher
        /// (W6.6): Zwei gleiche Kollektoren wären ohne eindeutigen Schlüssel für die
        /// Hülle ununterscheidbar. Der Vorläufer zählte ab 100000.
        /// </summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }

        // =================================================================================
        // Die Wege hinter den Delegaten des Katalogeditors
        // =================================================================================

        /// <summary>„Überschreiben" (<c>btn_Überschreiben_Click</c>:115).</summary>
        private static KatalogSpeicherErgebnis Ueberschreiben(SolarkollektorKatalogDaten daten)
        {
            try
            {
                var ctrl = new SolarkollektorenStammCtrl();
                if (!ctrl.UpdateFrom(NachModell(daten, daten.Name)))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");

                return new KatalogSpeicherErgebnis(true,
                    Text_("SKK_MSG_GESPEICHERT", "Datensatz gespeichert"), daten.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Überschreiben des Solarkollektors: " + ex.Message);
                return new KatalogSpeicherErgebnis(false,
                    Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// „Speichern" und „Speichern unter" (<c>btn_Speichern_Click</c>:188,
        /// <c>btn_Speichern_Unter_Click</c>:225) — beide prüfen zuerst auf den
        /// vorhandenen Namen und legen dann an.
        /// </summary>
        private static KatalogSpeicherErgebnis Anlegen(SolarkollektorKatalogDaten daten, string name)
        {
            try
            {
                var ctrl = new SolarkollektorenStammCtrl();
                if (ctrl.Exists(name))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_NAME_BELEGT", "Name existiert bereits!"), "");

                if (!ctrl.InsertFrom(NachModell(daten, name)))
                    return new KatalogSpeicherErgebnis(false,
                        Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");

                return new KatalogSpeicherErgebnis(true,
                    Text_("SKK_MSG_GESPEICHERT", "Datensatz gespeichert"), name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei 'Speichern unter' des Solarkollektors: " + ex.Message);
                return new KatalogSpeicherErgebnis(false,
                    Text_("SKK_MSG_FEHLER", "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        // =================================================================================
        // Abbildungen
        // =================================================================================

        private static void AusModell(SolarkollektorKatalogDaten ziel, SolarkollektorenModel m)
        {
            ziel.KatalogId = m.m_ID;
            ziel.Name = m.m_szKollektorname ?? "";
            ziel.Firma = m.m_szFirma ?? "";
            ziel.Beschreibung = m.m_szBeschreibung ?? "";
            ziel.Kollektortyp = m.m_szKollektortyp ?? "";
            ziel.Modulflaeche = m.m_Modulfläche;
            ziel.Aperturflaeche = m.m_Aperturfläche;
            ziel.H0 = m.m_h0;
            ziel.K1 = m.m_k1;
            ziel.K2 = m.m_k2;
            ziel.Kdir = m.m_Kdir;
            ziel.Kdiff = m.m_Kdfu;
            ziel.Kosten = m.m_Kosten;
            ziel.Vorlauf = (int)m.m_Vorlauf;
            ziel.Ruecklauf = (int)m.m_Ruecklauf;
        }

        /// <summary>
        /// Zurück in die Fachklasse. Leere Zahlenfelder werden 0 — dieselbe Regel wie
        /// <c>Program.GanzzahlPruefen(..., leerErlaubt: true)</c> im Vorläufer. Die acht
        /// Pflichtzahlen sind an dieser Stelle bereits geprüft; ihr <c>?? 0</c> ist
        /// Absicherung, kein Weg.
        /// </summary>
        private static SolarkollektorenModel NachModell(SolarkollektorKatalogDaten d, string name)
        {
            return new SolarkollektorenModel
            {
                m_ID = d.KatalogId,
                m_szKollektorname = name,
                m_szFirma = d.Firma,
                m_szBeschreibung = d.Beschreibung,
                m_szKollektortyp = d.Kollektortyp,
                m_Modulfläche = d.Modulflaeche ?? 0,
                m_Aperturfläche = d.Aperturflaeche ?? 0,
                m_h0 = d.H0 ?? 0,
                m_k1 = d.K1 ?? 0,
                m_k2 = d.K2 ?? 0,
                m_Kdir = d.Kdir ?? 0,
                m_Kdfu = d.Kdiff ?? 0,
                m_Kosten = d.Kosten ?? 0,
                m_Vorlauf = d.Vorlauf ?? 0,
                m_Ruecklauf = d.Ruecklauf ?? 0
            };
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
