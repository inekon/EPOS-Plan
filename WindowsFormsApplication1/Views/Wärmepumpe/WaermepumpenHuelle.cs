using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Wärmepumpen-VERWALTUNG (iU9-W7.5) — der Ersatz für
    /// <c>Form_WPAuswahl</c>.
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Wie bei den fünf
    /// Erzeugerdialogen der Welle 6 gehört die <c>List&lt;WErzeugerModel&gt;</c> dem
    /// Aufrufer; die Hülle bearbeitet sie an Ort und Stelle. Der Assistent reicht sogar
    /// dieselbe Liste über ALLE Erzeugertypen herein — deshalb filtert die Anzeige auf
    /// <c>WP_TYP</c>, und deshalb entfernt „Löschen" die ZEILE und nicht ihren
    /// Anzeigeindex.</para>
    ///
    /// <para><b>Zwei Ebenen Übersetzung.</b> Die Komponente kennt nur
    /// <see cref="WaermepumpeAnlageDaten"/>; der Kern nur <see cref="WErzeugerModel"/>.
    /// Die Hülle hält die Zuordnung in einem Wörterbuch und überträgt beim OK der
    /// Detailansicht zurück — <see cref="WaermepumpeAnlageHuelle.NachModell"/> ist
    /// dieselbe Abbildung, die auch der Fensterweg benutzt.</para>
    /// </summary>
    internal static class WaermepumpenHuelle
    {
        /// <summary>
        /// Gewünschtes Innenmaß. Der Vorläufer maß 581 × 299 — mit fünf Spalten,
        /// 44‑px-Zeilen und der Aktionsspalte braucht die Razor-Fassung mehr.
        ///
        /// <para><b>Seit W7‑E‑2 (Windows-Abnahme 06.09.2026) deutlich mehr:</b> Die
        /// Detailansicht erscheint als <c>Ueberlagerung</c> DIESES Fensters und steht
        /// darin in drei Spalten wie ihr Vorbild <c>Wizard_WPItem</c> (1126 × 752).
        /// Das Wunschmaß ist eine UNTERGRENZE (<c>Fenstermass.Vorgabe</c>: Maximum aus
        /// Wunsch und 85 % / 90 % des Arbeitsbereichs, gedeckelt auf 92 %) — mit
        /// 900 × 600 blieb auf einem kleinen Schirm zu wenig übrig, und die
        /// Überlagerung rollte innen. Genau das zeigte das Bildschirmfoto der
        /// Abnahme.</para>
        /// </summary>
        private static readonly Size MASS = new Size(1280, 860);

        /// <summary>
        /// Zeigt die Verwaltung als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_WP_Click</c> und
        /// <c>Form_Simulation_Detail.listView_SimWP_MouseDown</c>.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<WaermepumpenDialog> dlg = null;
            // KATALOGAUSWAHL V1, STUFE 3: Die Projektkopie entsteht beim Uebernehmen; Abbrechen raeumt die in dieser
            // Sitzung neu angelegten ab, Entfernen geht erst mit OK (Projektkopievormerkung).
            var vormerkung = new Projektkopievormerkung(name => new WPCtrl().DeleteFromProjekt(name, projektId));

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, modelle, wizard: false, vormerkung: vormerkung))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<WaermepumpenDialog>(
                Text_("WPV_TITEL", "Wärmepumpen Verwaltung"), MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            vormerkung.Abschliessen(ok, id => modelle.Exists(it => it.ID_WP == id &&
                (it.ID_Type == WizardItemClass.WP_TYP || it.ID_Type == WizardItemClass.REF_WP_TYP)));
            return ok;
        }

        /// <summary>Die Wärmepumpenseite des ASSISTENTEN — dieselbe Komponente, randlose Hülle.</summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>Der PARAMETERSATZ des Dialogs.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, List<WErzeugerModel> modelle, bool wizard,
            Projektkopievormerkung vormerkung = null)
        {
            // KATALOGAUSWAHL V1, STUFE 3: Im Projekt (nicht im Assistenten) traegt jede Zeile ihre PROJEKTKOPIE - erst
            // damit haben Bearbeiten…, Kenndaten der Kopie und der Rueckweg einen Projektsatz.
            bool mitKopie = !wizard && projektId > 0;
            // Die Anzeige fuehrt nur die WP-Zeilen; das Woerterbuch haelt die
            // Zuordnung zurueck in die geteilte Liste.
            var zeilen = new List<WaermepumpeAnlageDaten>();
            var zuModell = new Dictionary<WaermepumpeAnlageDaten, WErzeugerModel>();

            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != WizardItemClass.WP_TYP) continue;

                // Ä22: Die Stammfelder der Zeile zweistufig nachziehen - sonst zeigte
                // die Liste 0 kW (SetControls:31).
                WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);

                WaermepumpeAnlageDaten daten = WaermepumpeAnlageHuelle.AusModell(m);

                // Anwenderauftrag 30.09.2026: Einen vorbelegten Ruecklauf (AusModell) zeigt
                // die Liste links fuer JEDE Zeile - also geht er auch in das Modell, sonst
                // stuende er in der Liste und wuerde fuer eine nie markierte Zeile beim OK
                // doch mit 0 gespeichert. Abbrechen verwirft die Liste des Aufrufers.
                if (!string.IsNullOrEmpty(daten.RuecklaufHerleitung))
                    m.Ruecklauf = daten.Ruecklauf ?? 0;

                zeilen.Add(daten);
                zuModell[daten] = m;
            }

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                // W14a-E-10 / S2.2: Der Katalog kommt aus DEMSELBEN Weg wie in der
                // Verwaltung - neun Spalten mit Trichter statt elf Bedienelementen in
                // einer Filterleiste. Hier ist es das Profil MIT der Spalte "im
                // Projekt verwendet" (Q12): Dieser Dialog ist der einzige der drei
                // Katalogwirte mit einer Projektliste.
                ["Katalog"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    () => new WPStammCtrl().Katalogfilterzeilen()),
                ["Katalogprofil"] = Katalogfilterprofil.MitVerwendung(
                    Anlagenart.Waermepumpe, KatalogBrowserHuelle.Text),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Waermepumpe, n, KatalogBrowserHuelle.Text)),

                // "Anlage…" (KA-E-11): die Anlagenseite als Ueberlagerung. Ihr "In Stamm übernehmen…" entfaellt hier -
                // der Rueckweg steht in der Projekt-Kopfleiste (KA-E-9), eine Handlung hat einen Ort.
                ["AnlageGaben"] = new Func<WaermepumpeAnlageDaten, IReadOnlyDictionary<string, object>>(
                    daten =>
                    {
                        var gaben = new Dictionary<string, object>(WaermepumpeAnlageHuelle.Gaben(
                            besitzer, daten, Modell(zuModell, modelle, projektId, daten), projektId));
                        gaben.Remove("InStammUebernehmen");
                        return gaben;
                    }),

                ["Anlegen"] = new Func<string, WaermepumpeAnlageDaten>(
                    bezeichner => Anlegen(zuModell, projektId, bezeichner, mitKopie, vormerkung)),
                ["Umstellen"] = new Func<WaermepumpeAnlageDaten, int, bool>(
                    (daten, stammId) => Umstellen(zuModell, modelle, projektId, daten, stammId, mitKopie, vormerkung)),

                // Die Detailzeile: Kenndaten und Kennlinie der Projektkopie bzw. des Katalogsatzes.
                ["ProjektSatz"] = new Func<WaermepumpeAnlageDaten, WaermepumpeStammDaten>(
                    daten => WaermepumpeAnlageHuelle.StammdatenZu(daten.IdWp)),
                ["ProjektBilder"] = new Func<WaermepumpeAnlageDaten, KennlinienBilder>(
                    daten => WaermepumpeAnlageHuelle.BilderZuAnlage(daten.IdWp)),
                ["KatalogSatz"] = new Func<int, WaermepumpeStammDaten>(WaermepumpeStammHuelle.SatzZu),
                ["KatalogBilder"] = new Func<int, KennlinienBilder>(id => WaermepumpeStammHuelle.BilderZu(id, false)),

                // Katalogpflege im Katalogfuss: Schloss, Loeschen, Bearbeiten… (4.9, ohne Vergleichen und Neu…).
                ["Schloss"] = Schlosswege.Aus(WPStammCtrl.SchlossSetzen),
                ["KatalogLoeschen"] = new Func<int, string>(KatalogLoeschen),
                ["EditorGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => new Dictionary<string, object>(WaermepumpeStammHuelle.Gaben()) { ["Vorwahl"] = name ?? "" }),
                ["KatalogsatzWege"] = new Satzbearbeitungswege
                {
                    Lesen = id => SatzFelder(false, id),
                    Speichern = saetze => SammelSchreiben(false, saetze, zuModell)
                },
                ["ProjektsatzWege"] = mitKopie ? new Satzbearbeitungswege
                {
                    Lesen = id => SatzFelder(true, id),
                    Speichern = saetze => SammelSchreiben(true, saetze, zuModell)
                } : null,
                ["FrageLoeschen"] = Text_("WPS_FRAGE_LOESCHEN",
                    "Der Katalogeintrag \"{0}\" wird für ALLE Projekte gelöscht. Fortfahren?"),
                ["BtnLoeschenText"] = Text_("WPS_BTN_LOESCHEN", "Löschen"),
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                // KA-E-12: die Kostenknoepfe beim Projektsatz - die Anlagenzeile muss dafuer gespeichert sein.
                ["KostenOeffnen"] = mitKopie
                    ? new Func<WaermepumpeAnlageDaten, bool, System.Threading.Tasks.Task>(
                        (daten, betrieb) =>
                        {
                            WErzeugerModel m = KostenBereit(zuModell, modelle, projektId, daten);
                            return m == null ? System.Threading.Tasks.Task.CompletedTask
                                : ErzeugerKostenwege.Kosten(besitzer, projektId, DbWerte.ERZEUGER_WAERMEPUMPE, m.ID, betrieb);
                        })
                    : null,
                ["EnergiekostenOeffnen"] = mitKopie
                    ? new Func<WaermepumpeAnlageDaten, System.Threading.Tasks.Task>(
                        daten => KostenBereit(zuModell, modelle, projektId, daten) == null
                            ? System.Threading.Tasks.Task.CompletedTask
                            : ErzeugerKostenwege.Energiekosten(besitzer, projektId, DbWerte.ERZEUGER_WAERMEPUMPE,
                                                               daten.CarrierId, daten.IdWp))
                    : null,

                ["Uebernehmen"] = new Action<WaermepumpeAnlageDaten>(
                    daten =>
                    {
                        WErzeugerModel m = Modell(zuModell, modelle, projektId, daten);
                        WaermepumpeAnlageHuelle.NachModell(daten, m);
                        // ET-5: der gewaehlte Traeger gehoert dem Projekt zugeordnet.
                        ErzeugerTraegerHuelle.Zuordnen(projektId, wizard, m.ID_Carrier);
                        if (!modelle.Contains(m))
                        {
                            m.ID_Type = WizardItemClass.WP_TYP;
                            m.ID_Projekt = projektId;
                            modelle.Add(m);
                        }
                    }),

                ["Entfernen"] = new Action<WaermepumpeAnlageDaten>(
                    daten =>
                    {
                        if (!zuModell.TryGetValue(daten, out WErzeugerModel m)) return;
                        modelle.Remove(m);
                        zuModell.Remove(daten);
                        // Die Projektkopie geht erst mit OK, und nur, wenn keine Zeile mehr auf sie zeigt.
                        if (mitKopie && m.ID_WP > 0)
                            vormerkung?.Entfernt(KopieName(m.ID_WP) ?? m.Bezeichner, m.ID_WP);
                    }),

                ["TitelText"] = Text_("WPV_TITEL", "Wärmepumpen Verwaltung"),
                ["KopfbandText"] = Text_("WPV_KOPFBAND", "Geben Sie die Daten der Wärmepumpe ein"),
                // W7-B-3 (08.09.2026): EIN Dialog - Auswahl links, Katalog rechts, Detail
                // darunter. Die Texte der Zweispaltenauswahl sind die der Nachbardialoge.
                ["LabelProjektliste"] = Text_("WPV_LBL_PROJEKTLISTE", "ausgewählte Wärmepumpen:"),
                ["LabelKatalogliste"] = Text_("WPV_LBL_KATALOGLISTE", "Wärmepumpen aus Datenbank:"),
                ["LabelUmstellen"] = Text_("WPV_BTN_UMSTELLEN", "Markierte auf diese Wärmepumpe umstellen"),
                ["TipUmstellen"] = Text_("WPV_TIP_UMSTELLEN",
                    "Die links markierte Wärmepumpe durch die rechts markierte ersetzen — Betriebsdaten und Kosten der Zeile bleiben."),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LeerText"] = Text_("WPV_LEER",
                    "Links eine Wärmepumpe markieren oder rechts eine aus der Datenbank übernehmen."),
                ["MangelFormat"] = Text_("WPV_MANGEL", "„{0}“: {1}"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),

                // W7-B-1 (Windows-Abnahme 06.09.2026): "Anstelle Name sollte Typ
                // stehen, es fehlt der Hersteller (vor Typ)."
                ["SpalteHersteller"] = Text_("WPV_SP_HERSTELLER", "Hersteller"),
                ["SpalteTyp"] = Text_("WPV_SP_TYP", "Typ"),

                ["SpalteLeistung"] = Text_("WPV_SP_LEISTUNG", "Leistung [kW]"),
                ["SpalteVorlauf"] = Text_("WPV_SP_VORLAUF", "Vorlauf [°C]"),
                ["SpalteRuecklauf"] = Text_("WPV_SP_RUECKLAUF", "Rücklauf [°C]"),
                ["SpalteBetriebsart"] = Text_("WPA_LBL_BETRIEBSART", "Betriebsart"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["LabelKennlinien"] = Text_("WPS_LBL_KENNLINIEN", "Kenndaten Kennlinien:"),
                ["ReiterCop"] = Text_("WPS_REITER_COP", "COP"),
                ["ReiterLeistung"] = Text_("WPS_REITER_LEISTUNG", "Leistung"),
                ["PlatzhalterBild"] = Text_("WPS_PLATZHALTER_BILD", "Keine Kennlinien vorhanden"),
                ["KostenInvestText"] = Text_("KDLG_KNOPF_INVEST", "Investitionskosten…"),
                ["KostenBetriebText"] = Text_("KDLG_KNOPF_BETRIEB", "Betriebskosten…"),
                ["KostenEnergieText"] = Text_("KDLG_KNOPF_ENERGIE", "Energiekosten…"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Das Modell zu einem Feldsatz. Ein Satz aus <see cref="Anlegen"/> steht schon
        /// im Wörterbuch, aber noch NICHT in der geteilten Liste — das holt erst
        /// „Übernehmen" nach, wenn der Anwender die Detailansicht mit OK schließt.
        /// </summary>
        private static WErzeugerModel Modell(
            Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
            List<WErzeugerModel> modelle, int projektId, WaermepumpeAnlageDaten daten)
        {
            if (zuModell.TryGetValue(daten, out WErzeugerModel vorhanden)) return vorhanden;

            var neu = new WErzeugerModel { ID_Type = WizardItemClass.WP_TYP, ID_Projekt = projektId };
            zuModell[daten] = neu;
            return neu;
        }

        /// <summary>
        /// „Neu..": Aus der Katalogwahl entsteht eine Zeile mit der STAMM-Id und den
        /// Stammfeldern — <c>btn_Neu_Click</c>:260 tat dasselbe über
        /// <c>GeraetedatenFuellen</c>. In die Anzeige kommt sie erst, wenn die
        /// Detailansicht mit OK schließt (der Vorläufer prüfte dafür <c>CloseWithOK</c>).
        /// </summary>
        private static WaermepumpeAnlageDaten Anlegen(
            Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
            int projektId, string bezeichner, bool mitKopie, Projektkopievormerkung vormerkung)
        {
            var modell = new WErzeugerModel
            {
                Bezeichner = bezeichner ?? "",
                ID_Type = WizardItemClass.WP_TYP,
                ID_Projekt = projektId,
                ID_WP = DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", bezeichner)
            };
            // KATALOGAUSWAHL V1, STUFE 3: die Projektkopie samt Kennlinien SOFORT - die Zeile traegt dann deren Id.
            if (mitKopie)
            {
                int kopie = KopieAnlegen(modell.ID_WP, projektId, vormerkung);
                if (kopie <= 0) return null;
                modell.ID_WP = kopie;
            }
            // ET-5 (08.09.2026): Vorgabe der Stromtraeger des Projekts (Anwender: "default Strom").
            modell.ID_Carrier = ErzeugerTraegerHuelle.Vorauswahl(DbWerte.ERZEUGER_WAERMEPUMPE, 0, projektId);
            WaermepumpeGeraeteCtrl.GeraetedatenFuellen(modell, modell.ID_WP);

            // W6-E-4 (06.09.2026): Die Waermepumpe hat keine Katalogtemperaturen - ihr
            // "Katalog" sind die Vorlaufstufen der Kennlinien. Die kleinste steht als
            // Vorschlag im Vorlauffeld, sobald die Detailansicht aufgeht; der Ruecklauf
            // bleibt leer (fuer ihn gibt es keine eindeutige Regel, siehe
            // AnlagenTemperaturen.VorlaufAusKennlinien).
            AnlagenTemperaturen.VorlaufAusKennlinien(modell);
            // Uebergabegrenze UB-E2: eine NEUE Anlage bekommt die Einbindung vorbelegt (Puffer bzw. direkt).
            BivalenzAbbildung.Vorbelegen(modell);

            WaermepumpeAnlageDaten daten = WaermepumpeAnlageHuelle.AusModell(modell);
            zuModell[daten] = modell;
            return daten;
        }

        /// <summary>
        /// Legt die Projektkopie des Katalogsatzes <paramref name="stammId"/> an (<c>WPCtrl.CopyFromStamm</c>, idempotent
        /// über Ursprung und Name) und merkt eine NEUE Kopie vor, damit Abbrechen sie wieder abräumt. −1 = Fehler.
        /// </summary>
        private static int KopieAnlegen(int stammId, int projektId, Projektkopievormerkung vormerkung)
        {
            if (stammId <= 0) return -1;
            var ctrl = new WPCtrl();
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM " + WPStammCtrl.TABLE + " WHERE ID = ?", new DbParam("@id", stammId))) ?? "";
            bool schonDa = ctrl.GetProjektIdZuStamm(stammId, projektId) > 0 || ctrl.GetProjektId(name, projektId) > 0;
            int kopie = ctrl.CopyFromStamm(stammId, projektId);
            if (kopie > 0 && !schonDa) vormerkung?.Angelegt(KopieName(kopie) ?? name, kopie);
            return kopie;
        }

        /// <summary>Der Name der Projektkopie <paramref name="idKopie"/>; <c>null</c> = keine.</summary>
        private static string KopieName(int idKopie)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM " + WPStammCtrl.TABELLE_PROJEKT + " WHERE ID = ?", new DbParam("@id", idKopie));
            return v == null || v == DBNull.Value ? null : Convert.ToString(v);
        }

        /// <summary>
        /// „Umstellen" (4.2): Die Zeile bekommt das Gerät des Katalogsatzes <paramref name="stammId"/> — im Projekt
        /// dessen Projektkopie (vorgemerkt), im Assistenten den Katalogsatz. Erst geht der Stand der Zeile ins Modell,
        /// dann zieht das Modell Gerät und Gerätefelder nach, und die Zeile liest es zurück: Betriebsdaten, Sperrzeiten,
        /// Bivalenz, Heizstab und Kosten der Anlage bleiben.
        /// </summary>
        private static bool Umstellen(Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
                                      List<WErzeugerModel> modelle, int projektId, WaermepumpeAnlageDaten daten,
                                      int stammId, bool mitKopie, Projektkopievormerkung vormerkung)
        {
            if (daten == null || stammId <= 0) return false;
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM " + WPStammCtrl.TABLE + " WHERE ID = ?", new DbParam("@id", stammId)));
            if (string.IsNullOrEmpty(name)) return false;
            int geraet = mitKopie ? KopieAnlegen(stammId, projektId, vormerkung) : stammId;
            if (geraet <= 0) return false;

            WErzeugerModel m = Modell(zuModell, modelle, projektId, daten);
            WaermepumpeAnlageHuelle.NachModell(daten, m);
            int alt = m.ID_WP;
            m.ID_WP = geraet;
            m.Bezeichner = name;
            m.KuehlfelderGeladen = false;
            if (!WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, geraet)) { m.ID_WP = alt; return false; }

            WaermepumpeAnlageDaten neu = WaermepumpeAnlageHuelle.AusModell(m);
            daten.WerteVon(neu);
            // Die alte Kopie wird wie beim Entfernen vorgemerkt - sie geht mit OK, wenn keine Zeile mehr auf sie zeigt.
            if (mitKopie && alt > 0 && alt != geraet) vormerkung?.Entfernt(KopieName(alt) ?? "", alt);
            return true;
        }

        /// <summary>Die Anlagenzeile für die Kostenknöpfe: erst ins Modell, dann gespeichert; <c>null</c> = nicht bereit.</summary>
        private static WErzeugerModel KostenBereit(Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell,
                                                   List<WErzeugerModel> modelle, int projektId, WaermepumpeAnlageDaten daten)
        {
            if (daten == null) return null;
            WErzeugerModel m = Modell(zuModell, modelle, projektId, daten);
            WaermepumpeAnlageHuelle.NachModell(daten, m);
            return WErzeugerCtrl.AnlagenzeileNachziehen(m, projektId) ? m : null;
        }

        /// <summary>Löscht einen Katalogsatz samt Kennlinien; leer = gelöscht, sonst der Grund.</summary>
        private static string KatalogLoeschen(int id)
        {
            WPStammCtrl.Sammelsatz satz = WPStammCtrl.SammelsatzLesen(false, id);
            if (satz == null || satz.Gesperrt)
                return Text_("KBROW_MSG_LOESCHEN_FEHLER", "Der Datensatz konnte nicht gelöscht werden.");
            var ctrl = new WPStammCtrl();
            ctrl.ReadAll("ID=" + id);
            if (ctrl.rows == 0) return Text_("KBROW_MSG_LOESCHEN_FEHLER", "Der Datensatz konnte nicht gelöscht werden.");
            WPStammCtrl geraet = ctrl;
            geraet.WPName = satz.Bezeichner;
            return geraet.Delete() ? "" : Text_("KBROW_MSG_LOESCHEN_FEHLER", "Der Datensatz konnte nicht gelöscht werden.");
        }

        // =================================================================================
        // Die Satzbearbeitung (KA-E-8): Hersteller, Beschreibung, Modulkosten
        // =================================================================================

        private const string FELD_FIRMA = "Firma";
        private const string FELD_BESCHREIBUNG = "Beschreibung";
        private const string FELD_MODULKOSTEN = "Modulkosten";

        /// <summary>Die drei Felder der Satzbearbeitung eines Satzes nach Id (Projektkopie bzw. Katalog).</summary>
        internal static IReadOnlyList<BrowserFeldwert> SatzFelder(bool projektkopie, int id)
        {
            WPStammCtrl.Sammelsatz satz = WPStammCtrl.SammelsatzLesen(projektkopie, id);
            if (satz == null) return null;
            return new[]
            {
                new BrowserFeldwert { Schluessel = FELD_FIRMA, Bezeichnung = Text_("WPS_LBL_HERSTELLER", "Hersteller") + ":",
                                      Art = BrowserFeldArt.Text, Editierbar = true, Wert = satz.Felder.Firma },
                new BrowserFeldwert { Schluessel = FELD_BESCHREIBUNG, Bezeichnung = Text_("WPS_LBL_BESCHREIBUNG", "Beschreibung") + ":",
                                      Art = BrowserFeldArt.Mehrzeilig, Editierbar = true, Wert = satz.Felder.Beschreibung },
                new BrowserFeldwert { Schluessel = FELD_MODULKOSTEN, Bezeichnung = Text_("MODK_LBL_MODULKOSTEN", "Modulkosten") + ":",
                                      Einheit = "€", Art = BrowserFeldArt.Zahl, Editierbar = true,
                                      Wert = satz.Felder.Modulkosten is double k ? k.ToString(System.Globalization.CultureInfo.CurrentCulture) : "" },
            };
        }

        /// <summary>
        /// Schreibt alle Sätze einer Satzbearbeitung in EINER Transaktion (<see cref="WPStammCtrl.SammelfelderSchreibenAlle"/>).
        /// Bei den Projektkopien ziehen die Zeilen danach Hersteller, Beschreibung und Modulkosten nach — sonst schriebe der
        /// Gerätenachzug beim OK den alten Stand der Zeile zurück.
        /// </summary>
        internal static KatalogSpeicherErgebnis SammelSchreiben(
            bool projektkopie, IReadOnlyList<(int Id, IReadOnlyList<BrowserFeldwert> Felder)> saetze,
            Dictionary<WaermepumpeAnlageDaten, WErzeugerModel> zuModell = null)
        {
            var liste = new List<WPStammCtrl.Sammelaenderung>();
            foreach (var (id, felder) in saetze ?? Array.Empty<(int, IReadOnlyList<BrowserFeldwert>)>())
            {
                string Wert(string schluessel)
                {
                    foreach (BrowserFeldwert f in felder ?? Array.Empty<BrowserFeldwert>())
                        if (f.Schluessel == schluessel) return f.Wert ?? "";
                    return null;
                }
                WPStammCtrl.Sammelsatz alt = WPStammCtrl.SammelsatzLesen(projektkopie, id);
                string kostenText = Wert(FELD_MODULKOSTEN);
                double? kosten = alt?.Felder.Modulkosten;
                if (kostenText != null)
                {
                    if (kostenText.Trim().Length == 0) kosten = null;
                    else if (ZahlText.Parsen(kostenText, out double k)) kosten = k;
                    else return new KatalogSpeicherErgebnis(false, string.Format(
                        Text_("KAT_MSG_SAMMEL_VERSTOSS", "„{0}“: {1} Es wurde nichts gespeichert."), alt?.Bezeichner ?? "",
                        Text_("MODK_LBL_MODULKOSTEN", "Modulkosten")), alt?.Bezeichner ?? "");
                }
                liste.Add(new WPStammCtrl.Sammelaenderung(id, new WPStammCtrl.Sammelfelder(
                    Wert(FELD_FIRMA) ?? alt?.Felder.Firma ?? "", Wert(FELD_BESCHREIBUNG) ?? alt?.Felder.Beschreibung ?? "", kosten)));
            }
            WPStammCtrl.SpeicherErgebnis e = WPStammCtrl.SammelfelderSchreibenAlle(projektkopie, liste);
            if (e.Ok && projektkopie && zuModell != null)
            {
                foreach (KeyValuePair<WaermepumpeAnlageDaten, WErzeugerModel> paar in zuModell)
                {
                    WPStammCtrl.Sammelaenderung s = liste.Find(x => x.Id == paar.Key.IdWp);
                    if (s == null) continue;
                    paar.Key.Firma = paar.Value.Firma = s.Felder.Firma;
                    paar.Key.Beschreibung = paar.Value.Beschreibung = s.Felder.Beschreibung;
                    paar.Key.Modulkosten = paar.Value.Modulkosten = (int)Math.Round(s.Felder.Modulkosten ?? 0);
                }
            }
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
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
