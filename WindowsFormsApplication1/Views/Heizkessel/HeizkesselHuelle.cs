using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Heizkessel-Dialoge (iU9-W6.1 und W6.3).
    ///
    /// <para><b>Eine Datei für beide.</b> Der Katalogeditor und der Projektdialog teilen
    /// sich ihre Datenseite — <see cref="HeizkesselStammCtrl"/>, die Brennstoffliste und
    /// die Wartungseinheiten —, und der Projektdialog zeigt den Katalogeditor in einer
    /// <c>Ueberlagerung</c>. Zwei Hüllen wären zwei Orte für dieselben Abbildungen.</para>
    ///
    /// <para><b>Die Abbildung zwischen den Welten liegt hier.</b>
    /// <see cref="HeizkesselKatalogDaten"/> ist der Feldsatz der Oberfläche,
    /// <see cref="HeizkesselModel"/> der des Kerns. Die Komponente kennt die Fachklassen
    /// des Kerns nicht (<c>EPOS.UI/CLAUDE.md</c>), der Kern kennt <c>EPOS.UI</c> nicht —
    /// die Hülle ist der einzige Ort, an dem beide zugleich sichtbar sind.</para>
    /// </summary>
    internal static class HeizkesselHuelle
    {
        /// <summary>Gewünschtes Innenmaß des Katalogeditors (Vorläufer: 744 × 589 zzgl. Kostenleiste).</summary>
        private static readonly Size KATALOG_MASS = new Size(900, 760);

        // =================================================================================
        // W6.1 - Katalogeditor
        // =================================================================================

        /// <summary>
        /// Zeigt den Katalogeditor als eigenes Fenster — der Weg der beiden
        /// WinForms-Aufrufer <c>Form_Heizkessel_Admin.btn_Bearbeiten_Click</c> und
        /// <c>btn_Neu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Editor erscheint.</param>
        /// <param name="name">
        /// Bezeichner des zu ladenden Katalogsatzes; im Modus „Neu" der gewünschte Name.
        /// </param>
        /// <param name="beschreibung">Beschreibung, die der Aufrufer bereits anzeigt.</param>
        /// <param name="neu"><c>true</c> = Modus „Neu" (nur „Speichern" ist aktiv).</param>
        /// <returns>
        /// Der Name, unter dem der Satz jetzt steht — nach einer Umbenennung der NEUE;
        /// <c>null</c> bei Abbruch. Bestandsverhalten von <c>frm.m_szKessel</c>.
        /// </returns>
        internal static string KatalogBearbeiten(IWin32Window besitzer, string name,
                                                 string beschreibung, bool neu)
        {
            string ergebnis = null;
            BlazorDialogForm<HeizkesselKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(name, beschreibung, neu))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), n =>
                {
                    ergebnis = n;
                    if (dlg != null) dlg.Schliessen(n != null);
                })
            };

            dlg = new BlazorDialogForm<HeizkesselKatalogDialog>(
                Text_("HZKK_TITEL", "Administration Heizkessel"), KATALOG_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Katalogeditors — für die Anzeige in einer
        /// <c>Ueberlagerung</c> des Projektdialogs (W6.3). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(string name, string beschreibung, bool neu)
        {
            HeizkesselStammCtrl.StelleSpaltenSicher();   // Vorsorge wie im Konstruktor der Maske

            var ctrl = new HeizkesselStammCtrl();
            var daten = new HeizkesselKatalogDaten();
            string hinweis = "";

            if (neu)
            {
                // Vorgaben von MODE_NEU (Konstruktor Z. 65-80): alles 0, der
                // Gaswirkungsgrad 0,94, kein Brennwert.
                daten.Name = name ?? "";
                daten.Beschreibung = "";
                daten.Firma = "";
                daten.Ptherm = 0;
                daten.Wirkungsgrad_Gas = 0.94;
                daten.Wirkungsgrad_Oel = 0;
                daten.Betriebsbereitschaftverlust = 0;
                daten.Investitionskosten = 0;
                daten.Wartungskosten = 0;
                daten.Nutzungsdauer = 0;
                daten.Raumbedarf = 0;
                daten.NOx = 0; daten.CO2 = 0; daten.CO = 0; daten.SO2 = 0; daten.Staub = 0;
                daten.Brennwert = false;
            }
            else
            {
                ctrl.ReadSingle(name);
                if (ctrl.rows > 0)
                {
                    AusModell(daten, ctrl);
                    daten.Name = name ?? "";
                    daten.Beschreibung = beschreibung ?? "";

                    // Der Mehrdeutigkeitshinweis von SetControls (Z. 364): Er haelt
                    // niemanden auf, er erklaert nur, warum derselbe Name in der
                    // Auswahlliste mehrfach steht.
                    int gleiche = HeizkesselStammCtrl.AnzahlMitBezeichner(name);
                    if (gleiche > 1)
                        hinweis = string.Format(
                            Text_("HZKK_MSG_MEHRDEUTIG",
                                  "Der Katalog führt den Namen \"{0}\" {1}-mal. Bearbeitet wird der " +
                                  "Eintrag mit der kleinsten ID ({2}); die übrigen bleiben unverändert."),
                            name, gleiche, ctrl.ID);
                }
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = neu ? KatalogModus.Neu : KatalogModus.Bearbeiten,
                ["Brennstoffe"] = Brennstoffe(ctrl),
                ["WartungEinheiten"] = WartungEinheiten(),
                ["HinweisBeimOeffnen"] = hinweis,

                ["Ueberschreiben"] = new Func<HeizkesselKatalogDaten, KatalogSpeicherErgebnis>(
                    d => Uebersetzen(HeizkesselStammCtrl.Ueberschreiben(NachModell(d)))),

                ["Anlegen"] = new Func<HeizkesselKatalogDaten, string, KatalogSpeicherErgebnis>(
                    (d, n) => Uebersetzen(HeizkesselStammCtrl.Anlegen(NachModell(d), n))),

                ["Co2Vorgabe"] = new Func<string, double>(EmissionsVorgaben.HeizkesselCo2),

                ["TitelText"] = Text_("HZKK_TITEL", "Administration Heizkessel"),
                ["GruppeBezeichnung"] = Text_("HZKK_GRP_BEZEICHNUNG", "Kessel"),
                ["LabelName"] = Text_("HZKK_LBL_NAME", "Kesselbezeichnung:"),
                ["LabelHersteller"] = Text_("HZKK_LBL_HERSTELLER", "Hersteller:"),
                ["LabelBeschreibung"] = Text_("HZKK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["GruppeTechnik"] = Text_("HZKK_GRP_TECHNIK", "Technische Daten"),
                ["LabelPtherm"] = Text_("HZKK_LBL_PTHERM", "Thermische Leistung:"),
                ["LabelPthermKurz"] = Text_("HZKK_FELD_PTHERM", "Thermische Leistung"),
                ["LabelEnergietraeger"] = Text_("HZKK_LBL_ENERGIETRAEGER", "Energieträger:"),
                ["LabelWirkungsgradGas"] = Text_("HZKK_LBL_WG_GAS",
                    "Wirkungsgrad Gas, Biogas, Holz und Sonstiges:"),
                ["LabelWirkungsgradGasKurz"] = Text_("HZKK_FELD_WG_GAS",
                    "Wirkungsgrad Gas, Biogas, Holz und Sonstiges"),
                ["LabelWirkungsgradOel"] = Text_("HZKK_LBL_WG_OEL", "Wirkungsgrad Öl:"),
                ["LabelWirkungsgradOelKurz"] = Text_("HZKK_FELD_WG_OEL", "Wirkungsgrad Öl"),
                ["HinweisWirkungsgrad"] = Text_("HZKK_HINT_WIRKUNGSGRAD", "(z. B. 0,9)"),
                ["LabelBBVerlust"] = Text_("HZKK_LBL_BBVERLUST", "Betriebsbereitschaftsverluste:"),
                ["LabelBBVerlustKurz"] = Text_("HZKK_FELD_BBVERLUST", "Betriebsbereitschaftsverluste"),
                ["LabelBrennwert"] = Text_("HZKK_LBL_BRENNWERT", "Brennwertkessel"),
                ["LabelVorlauf"] = Text_("HZKK_LBL_VORLAUF", "Vorlauf:"),
                ["LabelVorlaufKurz"] = Text_("HZKK_FELD_VORLAUF", "Vorlauf"),
                ["LabelRuecklauf"] = Text_("HZKK_LBL_RUECKLAUF", "Rücklauf:"),
                ["LabelRuecklaufKurz"] = Text_("HZKK_FELD_RUECKLAUF", "Rücklauf"),
                ["GruppeKosten"] = Text_("HZKK_GRP_KOSTEN", "Eingabedaten zur Berechnung der Kosten"),
                ["LabelInvest"] = Text_("HZKK_LBL_INVEST", "Investitionskosten:"),
                ["LabelInvestKurz"] = Text_("HZKK_FELD_INVEST", "Investitionskosten"),
                ["LabelWartung"] = MyResource.Resource.KESSEL_WARTUNG_LBL + ":",
                ["LabelWartungKurz"] = MyResource.Resource.KESSEL_WARTUNG_LBL,
                ["LabelWartungEinheit"] = MyResource.Resource.KESSEL_WARTUNG_EINHEIT_LBL + ":",
                ["LabelRaumbedarf"] = Text_("HZKK_LBL_RAUMBEDARF", "Raumbedarf:"),
                ["LabelRaumbedarfKurz"] = Text_("HZKK_FELD_RAUMBEDARF", "Raumbedarf"),
                ["LabelNutzungsdauer"] = Text_("HZKK_LBL_NUTZUNGSDAUER", "Nutzungsdauer:"),
                ["LabelNutzungsdauerKurz"] = Text_("HZKK_FELD_NUTZUNGSDAUER", "Nutzungsdauer"),
                ["EinheitJahre"] = Text_("HZKK_EINHEIT_JAHRE", "Jahre"),
                ["GruppeBehg"] = Text_("HZKK_GRP_BEHG", "Emissionen nach BEHG-V"),
                ["BehgZeile"] = Text_("HZKK_BEHG_ZEILE", "für Heizzwecke in t CO2 / GJ"),
                ["BehgOel"] = Text_("HZKK_BEHG_OEL", "Heizöl: 0,0808"),
                ["BehgFluessiggas"] = Text_("HZKK_BEHG_FLUESSIGGAS", "Flüssiggas: 0,0663"),
                ["BehgErdgas"] = Text_("HZKK_BEHG_ERDGAS", "Erdgas: 0,056"),
                ["BtnCo2Text"] = Text_("HZKK_BTN_CO2", "CO2 BEHG"),
                ["GruppeEmissionen"] = Text_("HZKK_GRP_EMISSIONEN",
                    "Emissionsfaktoren bezogen auf den Brennstoffverbrauch"),
                ["LabelStaub"] = Text_("HZKK_LBL_STAUB", "Staub:"),
                ["LabelStaubKurz"] = Text_("HZKK_FELD_STAUB", "Staub"),
                ["BtnUeberschreibenText"] = Text_("HZKK_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("HZKK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungZahlUngueltig"] = Text_("HZKK_MSG_ZAHL",
                    "Bitte für \"{0}\" eine Zahl eingeben (Dezimaltrennzeichen Komma oder Punkt)."),
                ["MeldungNameFehlt"] = Text_("HZKK_MSG_NAME_FEHLT", "Bitte einen gültigen Namen eingeben!")
            };
        }

        // =================================================================================
        // Abbildung zwischen Oberflaechen- und Kernfeldsatz
        // =================================================================================

        /// <summary>Kern → Oberfläche.</summary>
        private static void AusModell(HeizkesselKatalogDaten d, HeizkesselStammCtrl m)
        {
            d.KatalogId = m.ID;
            d.Firma = m.Firma ?? "";
            d.Ptherm = m.Ptherm;
            d.Wirkungsgrad_Gas = m.Wirkungsgrad_Gas;
            d.Wirkungsgrad_Oel = m.Wirkungsgrad_Oel;
            d.Betriebsbereitschaftverlust = m.Betriebsbereitschaftverlust;
            d.Investitionskosten = m.Investitionskosten;
            d.Wartungskosten = m.Wartungskosten;
            d.WartungEinheit = EinheitIndex(m.Wartungskosten_Einheit);
            d.Nutzungsdauer = m.Nutzungsdauer;
            d.Raumbedarf = m.Raumbedarf;
            d.NOx = m.NOx;
            d.CO2 = m.CO2;
            d.CO = m.CO;
            d.SO2 = m.SO2;
            d.Staub = m.Staub;
            d.Brennwert = m.Brennwert;
            d.Vorlauf = m.Vorlauf;
            d.Ruecklauf = m.Ruecklauf;

            // Bereichspruefung wie in SetControls (Z. 358-362): Brennstoff ist eine
            // 1-basierte Id, die Liste kann kuerzer sein.
            d.Brennstoff = m.Brennstoff >= 1 ? m.Brennstoff : (int?)null;
        }

        /// <summary>
        /// Oberfläche → Kern. Hier gilt die Bestandsregel „leer = 0" (der Vorläufer
        /// prüfte mit <c>leerErlaubt: true</c> und übernahm den ausgelesenen 0-Wert).
        /// </summary>
        private static HeizkesselModel NachModell(HeizkesselKatalogDaten d)
        {
            var m = new HeizkesselModel
            {
                ID = d.KatalogId,
                Name = (d.Name ?? "").Trim(),
                Firma = (d.Firma ?? "").Trim(),
                Beschreibung = (d.Beschreibung ?? "").Trim(),
                Ptherm = d.Ptherm ?? 0,
                Wirkungsgrad_Gas = d.Wirkungsgrad_Gas ?? 0,
                Wirkungsgrad_Oel = d.Wirkungsgrad_Oel ?? 0,
                Betriebsbereitschaftverlust = d.Betriebsbereitschaftverlust ?? 0,
                Investitionskosten = d.Investitionskosten ?? 0,
                Wartungskosten = d.Wartungskosten ?? 0,
                Wartungskosten_Einheit = EinheitWert(d.WartungEinheit),
                Nutzungsdauer = d.Nutzungsdauer ?? 0,
                Raumbedarf = d.Raumbedarf ?? 0,
                NOx = d.NOx ?? 0,
                CO2 = d.CO2 ?? 0,
                CO = d.CO ?? 0,
                SO2 = d.SO2 ?? 0,
                Staub = d.Staub ?? 0,
                Brennwert = d.Brennwert,
                Vorlauf = d.Vorlauf ?? 0,
                Ruecklauf = d.Ruecklauf ?? 0,

                // Wie InitDatensatzUpdate (Z. 606-610): ohne Wahl gilt die 1.
                Brennstoff = d.Brennstoff.HasValue && d.Brennstoff.Value >= 1 ? d.Brennstoff.Value : 1
            };
            return m;
        }

        private static KatalogSpeicherErgebnis Uebersetzen(HeizkesselStammCtrl.SpeicherErgebnis e)
        {
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        /// <summary>
        /// Die Energieträger als (Id, Text) — die Id ist die 1-basierte Nummer aus
        /// <c>Tab_Brennstoff_Stamm</c>, so wie der Vorläufer sie über
        /// <c>SelectedIndex + 1</c> bildete.
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Brennstoffe(HeizkesselStammCtrl ctrl)
        {
            var liste = new List<(int, string)>();
            for (int i = 0; i < ctrl.Brennstoffart.Count; i++)
                liste.Add((i + 1, ctrl.Brennstoffart[i]));
            return liste;
        }

        /// <summary>
        /// Die drei Wartungseinheiten; die Id ist der Index in
        /// <c>TechnikPlanwertCtrl.WARTUNG_SCHLUESSEL</c>. Der sprachneutrale Schlüssel
        /// und der Persistenzwert bleiben hier — sie sind Datenbankinhalt
        /// (Drei-Schichten-Regel).
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> WartungEinheiten()
        {
            var liste = new List<(int, string)>();
            string[] schluessel = TechnikPlanwertCtrl.WARTUNG_SCHLUESSEL;
            for (int i = 0; i < schluessel.Length; i++)
                liste.Add((i, TechnikPlanwertCtrl.WartungName(schluessel[i])));
            return liste;
        }

        /// <summary>Persistenzwert → Listenindex (Vorbild <c>EinheitWaehlen</c>).</summary>
        private static int EinheitIndex(string dbWert)
        {
            string gesucht = TechnikPlanwertCtrl.WartungSchluessel(dbWert);
            string[] schluessel = TechnikPlanwertCtrl.WARTUNG_SCHLUESSEL;
            for (int i = 0; i < schluessel.Length; i++)
                if (string.Equals(schluessel[i], gesucht, StringComparison.Ordinal)) return i;
            return 0;
        }

        /// <summary>Listenindex → Persistenzwert (Vorbild <c>GewaehlteEinheit</c>).</summary>
        private static string EinheitWert(int index)
        {
            string[] schluessel = TechnikPlanwertCtrl.WARTUNG_SCHLUESSEL;
            string s = (index >= 0 && index < schluessel.Length) ? schluessel[index] : null;
            return TechnikPlanwertCtrl.WartungDbWert(s);
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        // =================================================================================
        // W6.3 - Projektdialog
        // =================================================================================

        /// <summary>Gewünschtes Innenmaß des Projektdialogs (Vorläufer: 769 × 527).</summary>
        private static readonly Size PROJEKT_MASS = new Size(1000, 720);

        /// <summary>
        /// Zeigt den Projektdialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Heizkessel_Click</c> und
        /// <c>HeizkesselKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Projekt; 0 = keines (dann keine Projektkopien).</param>
        /// <param name="idType">
        /// <c>KESSEL_TYP</c> oder <c>REF_KESSEL_TYP</c>. Er entscheidet, welche Zeilen der
        /// geteilten Liste gezeigt werden UND welchen Typ eine neue Zeile bekommt.
        /// </param>
        /// <param name="modelle">
        /// Die geteilte Erzeugerliste. Sie wird AN ORT UND STELLE bearbeitet; der Aufrufer
        /// schreibt sie danach wie bisher über <c>WizardCtrl</c> zurück.
        /// </param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<HeizkesselDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<HeizkesselDialog>(
                Text_("HZK_TITEL", "Verwaltung Heizkessel"), PROJEKT_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Projektdialogs — auch die Assistentenseite baut sich
        /// daraus (<see cref="BlazorAssistentSeite{TKomponente}"/>).
        /// </summary>
        /// <remarks>
        /// <b>Die Zeilenliste ist ein SPIEGEL der geteilten Modellliste.</b> Die Komponente
        /// bearbeitet <see cref="ErzeugerZeile"/>-Objekte; die Zuordnung zum
        /// <see cref="WErzeugerModel"/> hält diese Hülle über
        /// <c>ErzeugerZeile.Schluessel</c> = <c>WErzeugerModel.ID</c>. Jede Änderung wird
        /// über die Delegaten sofort in die Modellliste zurückgeschrieben — genau so, wie
        /// der Vorläufer sein <c>ListViewItem.Tag</c> benutzte.
        /// </remarks>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType,
            List<WErzeugerModel> modelle, bool wizard)
        {
            var stamm = new HeizkesselStammCtrl();
            var projekt = new HeizkesselCtrl();

            // Zeilen des eigenen Typs spiegeln. BEFUND W6-O-3: SetControls filterte hart
            // auf KESSEL_TYP, auch wenn der Aufrufer REF_KESSEL_TYP meinte - im
            // Referenzfall blieb die linke Liste deshalb leer, obwohl Zeilen vorhanden
            // waren. Hier gilt der übergebene Typ (Abweichung A-14).
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, WErzeugerModel>();
            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;
                zeilen.Add(ZeileZu(m));
                zuModell[m.ID] = m;
            }

            // Zähler für neue Zeilen - wie startindex im Vorläufer. Als Objekt, damit
            // die Delegaten ihn TEILEN: Ein ref-Parameter ließe sich in einem Lambda
            // nicht einfangen, und zwei Zähler vergäben denselben Schlüssel zweimal.
            var zaehler = new Zaehler();
            foreach (var m in modelle) if (m.ID >= zaehler.Naechster) zaehler.Naechster = m.ID + 1;

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,

                // Filter: "Alle" voran, dann die Gruppen bzw. die fünf Leistungsstufen -
                // dieselbe Reihenfolge wie Form_Heizkessel_Load.
                ["Gruppen"] = Gruppen(stamm),
                ["Leistungsstufen"] = Leistungsstufen(),

                ["Filtern"] = new Func<string, int, IReadOnlyList<KatalogZeile>>(
                    (gruppe, stufe) => KatalogZeilen(stamm.Filtern(gruppe, stufe))),

                ["KatalogDetail"] = new Func<string, ErzeugerDetail>(
                    name => DetailZu(projekt.KatalogDetail(name))),

                ["ProjektDetail"] = new Func<int, ErzeugerDetail>(
                    id => DetailZu(projekt.ProjektDetail(id))),

                ["Varianten"] = new Func<int, IReadOnlyList<(int Id, string Text)>>(
                    carrierId =>
                    {
                        var (_, liste) = EnergietraegerVarianteCtrl.VariantenDerGruppe(carrierId);
                        var eintraege = new List<(int, string)>();
                        foreach (var v in liste) eintraege.Add((v.Id, v.Name));
                        return eintraege;
                    }),

                ["Vorbereiten"] = new Func<int, TraegerVorbereitung>(
                    stammId => Vorbereiten(stamm, stammId)),

                ["Aufnehmen"] = new Func<int, EnergietraegerVarianteErgebnis, AufnahmeErgebnis>(
                    (stammId, ergebnis) => Aufnehmen(stamm, projektId, idType, wizard, modelle,
                                                     zuModell, zaehler, stammId, ergebnis)),

                ["Entfernen"] = new Action<ErzeugerZeile>(
                    zeile => Entfernen(projektId, idType, wizard, modelle, zuModell, zeile)),

                ["TraegerWechseln"] = new Action<ErzeugerZeile, int>(
                    (zeile, neu) =>
                    {
                        EnergietraegerVarianteCtrl.TraegerUmhaengen(projektId, zeile.CarrierId, neu);
                        if (zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) m.ID_Carrier = neu;
                    }),

                ["Uebernehmen"] = new Action<ErzeugerZeile>(
                    zeile =>
                    {
                        if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;
                        m.Vorlauf = zeile.Vorlauf ?? 0;
                        m.Ruecklauf = zeile.Ruecklauf ?? 0;
                    }),

                ["EditorGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => Gaben(name, "", neu: false)),

                ["TraegerGaben"] = new Func<TraegerVorbereitung, IReadOnlyDictionary<string, object>>(
                    TraegerGaben),

                ["KatalogLoeschen"] = new Func<int, bool>(id => stamm.Delete(id)),

                // iU9-W14a.1: Die Katalogverwaltung ist die Razor-Komponente
                // KatalogBrowserDialog und erscheint als UEBERLAGERUNG im selben
                // Fenster - der Sprung ueber die Bruecke entfaellt (Risiko R2).
                ["VerwaltungGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    HeizkesselAdminHuelle.Gaben),

                ["TitelText"] = Text_("HZK_TITEL", "Verwaltung Heizkessel"),
                ["KopfbandText"] = Text_("HZK_KOPFBAND", "Geben Sie Daten des Spitzenlastkessels ein"),
                ["LabelProjektliste"] = Text_("HZK_LBL_PROJEKTLISTE", "ausgewählt im Projekt"),
                ["LabelKatalogliste"] = Text_("HZK_LBL_KATALOGLISTE", "Kessel aus Datenbank"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LabelFilterBrennstoff"] = Text_("HZK_LBL_FILTER_BRENNSTOFF", "Filtern nach Brennstoffart:"),
                ["LabelFilterLeistung"] = Text_("HZK_LBL_FILTER_LEISTUNG", "Filtern nach Leistung:"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["BtnLoeschenText"] = Text_("HZK_BTN_LOESCHEN", "Löschen"),
                ["BtnAdminText"] = Text_("HZK_BTN_ADMIN", "Administration..."),
                ["GruppeModul"] = Text_("HZK_GRP_MODUL", "Modul"),
                ["LabelName"] = Text_("HZK_LBL_NAME", "Name:"),
                ["LabelBeschreibung"] = Text_("HZKK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelTraeger"] = Text_("HZK_LBL_TRAEGER", "Brennstoff Variante:"),
                ["LabelVorlauf"] = Text_("HZKK_LBL_VORLAUF", "Vorlauf:"),
                ["LabelRuecklauf"] = Text_("HZKK_LBL_RUECKLAUF", "Rücklauf:"),
                ["TraegerTitel"] = MyResource.Resource.KAUSW_TITEL,
                ["EditorTitel"] = Text_("HZKK_TITEL", "Administration Heizkessel"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["FrageLoeschen"] = Text_("HZK_FRAGE_LOESCHEN",
                    "Der Katalogeintrag \"{0}\" wird für ALLE Projekte gelöscht. Fortfahren?"),
                ["TitelLoeschen"] = Text_("HZK_TITEL_LOESCHEN", "Löschen"),
                ["MeldungLoeschFehler"] = Text_("HZK_MSG_LOESCHFEHLER",
                    "Der Katalogeintrag konnte nicht gelöscht werden."),

                ["KostenInvestText"] = Text_("KDLG_KNOPF_INVEST", "Investitionskosten…"),
                ["KostenBetriebText"] = Text_("KDLG_KNOPF_BETRIEB", "Betriebskosten…"),
                ["KostenEnergieText"] = Text_("KDLG_KNOPF_ENERGIE", "Energiekosten…")
            };
        }

        // =================================================================================
        // Die Schreibwege hinter den Delegaten
        // =================================================================================

        /// <summary>
        /// Die Werte, die <c>btn_Kessel_Hinzu_Click</c> aus dem Stammsatz las, plus die
        /// Auswahlliste des Trägerdialogs.
        /// </summary>
        private static TraegerVorbereitung Vorbereiten(HeizkesselStammCtrl stamm, int stammId)
        {
            stamm.ReadById(stammId);
            if (stamm.rows == 0)
                return new TraegerVorbereitung(Array.Empty<(int, string)>(), null,
                    Text_("HZK_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Heizkessel wurde in den Stammdaten nicht gefunden."));

            int nBrennstoff = stamm.Brennstoff;

            // Nur die Kategorie des Kessels anbieten (Befund 03.09.2026) - dieselbe
            // Einengung wie im gelöschten CreateNewEnergyCarrier.
            var liste = EnergietraegerVarianteCtrl.Energietraeger(
                EnergietraegerVarianteCtrl.KategorieZu(nBrennstoff));

            return new TraegerVorbereitung(liste, nBrennstoff > 0 ? (int?)nBrennstoff : null);
        }

        /// <summary>
        /// Nimmt den Kessel auf: Träger anlegen, Projektkopie ziehen, Modell in die
        /// geteilte Liste. Reihenfolge und Abbruchbedingungen wie in
        /// <c>btn_Kessel_Hinzu_Click</c>.
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(
            HeizkesselStammCtrl stamm, int projektId, int idType, bool wizard,
            List<WErzeugerModel> modelle, Dictionary<int, WErzeugerModel> zuModell,
            Zaehler zaehler, int stammId, EnergietraegerVarianteErgebnis ergebnis)
        {
            stamm.ReadById(stammId);
            if (stamm.rows == 0)
                return new AufnahmeErgebnis(null,
                    Text_("HZK_MSG_NICHT_GEFUNDEN",
                          "Der ausgewählte Heizkessel wurde in den Stammdaten nicht gefunden."), true);

            // Punkt 2: Energieträgervariante ZUERST. Schlägt das Anlegen fehl, wird KEIN
            // Kessel hinzugefügt - kein verwaister Eintrag mit ID_Carrier = 0.
            EnergietraegerVarianteCtrl.VariantenErgebnis traeger =
                EnergietraegerVarianteCtrl.Anlegen(projektId, wizard,
                    ergebnis.BrennstoffId, ergebnis.BrennstoffName, ergebnis.VariantenName);

            if (traeger.CarrierId <= 0)
                return new AufnahmeErgebnis(null, traeger.Meldung, true);

            var model = new WErzeugerModel
            {
                ID = zaehler.Naechster++,
                ID_Projekt = projektId,
                ID_Type = idType,
                Bezeichner = stamm.Name,
                Vorlauf = stamm.Vorlauf,
                Ruecklauf = stamm.Ruecklauf,
                ID_Carrier = traeger.CarrierId
            };

            // Außerhalb des Assistenten den Stammsatz sofort in die Projekttabelle
            // kopieren (idempotent) und die PROJEKT-Id referenzieren; im Wizard nur die
            // Stamm-Id als Platzhalter - die Kopie macht WizardCtrl beim Speichern.
            if (!wizard && projektId > 0)
            {
                int projektKopie = new HeizkesselCtrl().CopyFromStamm(stammId, projektId);
                if (projektKopie <= 0)
                    return new AufnahmeErgebnis(null,
                        Text_("HZK_MSG_KOPIE_FEHLER",
                              "Der Datensatz konnte nicht in das Projekt übernommen werden."), true);
                model.ID_Kessel = projektKopie;
            }
            else
            {
                model.ID_Kessel = stammId;
            }

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model), traeger.Meldung, false);
        }

        /// <summary>
        /// Entfernt die Zeile aus der geteilten Liste. Die Projektkopie geht nur mit,
        /// wenn keine zweite Zeile mehr darauf verweist — mehrere Instanzen desselben
        /// Kessels teilen sich EINE <c>Tab_Heizkessel</c>-Kopie.
        /// </summary>
        private static void Entfernen(int projektId, int idType, bool wizard,
                                      List<WErzeugerModel> modelle,
                                      Dictionary<int, WErzeugerModel> zuModell,
                                      ErzeugerZeile zeile)
        {
            if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;

            modelle.Remove(m);
            zuModell.Remove(zeile.Schluessel);

            bool nochReferenziert = false;
            foreach (WErzeugerModel it in modelle)
                if (it.ID_Type == idType && it.ID_Kessel == m.ID_Kessel) { nochReferenziert = true; break; }

            if (!wizard && projektId > 0 && !nochReferenziert)
                new HeizkesselCtrl().DeleteFromProjekt(m.Bezeichner, projektId);
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
                GeraetId = m.ID_Kessel,
                CarrierId = m.ID_Carrier,
                Vorlauf = m.Vorlauf,
                Ruecklauf = m.Ruecklauf
            };
        }

        /// <summary>
        /// Der Detailblock. Reihenfolge und Formatierung wie in
        /// <c>ApplySelectedKessel</c>: Leistung und Investition mit zwei
        /// Nachkommastellen.
        /// </summary>
        private static ErzeugerDetail DetailZu(HeizkesselCtrl.KesselDetail d)
        {
            if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            var felder = new List<(string, string)>
            {
                (Text_("HZK_LBL_BRENNSTOFFTYP", "Brennstoff Typ:"), d.Brennstoff),
                (Text_("HZK_LBL_LEISTUNG", "Leistung [kW]:"), d.Ptherm.ToString("F2")),
                (Text_("HZK_LBL_INVEST", "Investitionskosten [€]:"), d.Investitionskosten.ToString("F2"))
            };

            return new ErzeugerDetail(d.Bezeichner, d.Beschreibung, felder,
                                      (Text_("HZKK_LBL_BRENNWERT", "Brennwertkessel"), d.Brennwert));
        }

        private static IReadOnlyList<KatalogZeile> KatalogZeilen(
            IReadOnlyList<HeizkesselStammCtrl.KatalogZeile> quelle)
        {
            var liste = new List<KatalogZeile>();
            foreach (var z in quelle) liste.Add(new KatalogZeile(z.Id, z.Bezeichner));
            return liste;
        }

        /// <summary>„Alle" voran, dann die Brennstoffgruppen — wie <c>Form_Heizkessel_Load</c>.</summary>
        private static IReadOnlyList<string> Gruppen(HeizkesselStammCtrl stamm)
        {
            var liste = new List<string> { "Alle" };
            liste.AddRange(stamm.Brennstoffart_Gruppe);
            return liste;
        }

        /// <summary>Die sechs Leistungsstufen in der Reihenfolge von <c>LEISTUNG_SQL</c>.</summary>
        private static IReadOnlyList<string> Leistungsstufen()
        {
            return new[]
            {
                Text_("HZK_STUFE_ALLE", "Alle"),
                Text_("HZK_STUFE_BIS50", "bis 50 kW"),
                Text_("HZK_STUFE_50_200", ">50 bis 200 kW"),
                Text_("HZK_STUFE_200_500", ">200 bis 500 kW"),
                Text_("HZK_STUFE_500_1000", ">500 bis 1.000 kW"),
                Text_("HZK_STUFE_UEBER1000", "über 1.000 kW")
            };
        }

        /// <summary>
        /// Der Parametersatz des Trägerdialogs — dieselben Werte, die
        /// <c>CreateNewEnergyCarrier</c> dem Fenster mitgab (Z. 314-333).
        /// </summary>
        private static IReadOnlyDictionary<string, object> TraegerGaben(TraegerVorbereitung vor)
        {
            return new Dictionary<string, object>
            {
                ["Energietraeger"] = vor.Energietraeger,
                ["VorwahlId"] = vor.VorwahlId,
                ["TitelText"] = MyResource.Resource.KAUSW_TITEL,
                ["LabelEnergietraeger"] = MyResource.Resource.KAUSW_LBL_ENERGIETRAEGER,
                ["LabelVariante"] = MyResource.Resource.KAUSW_LBL_VARIANTE,
                ["MeldungNameFehlt"] = MyResource.Resource.KAUSW_MSG_NAME_FEHLT,
                ["MeldungTraegerFehlt"] = MyResource.Resource.KAUSW_MSG_TRAEGER_FEHLT,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
        }

        /// <summary>
        /// Der Zeilenzähler eines Dialoglaufs — das Gegenstück zu <c>startindex</c> des
        /// Vorläufers. Als Objekt, weil die Delegaten ihn gemeinsam fortschreiben.
        /// </summary>
        private sealed class Zaehler
        {
            /// <summary>Der nächste freie Zeilenschlüssel.</summary>
            internal int Naechster = 100000;
        }

        /// <summary>
        /// Die Kesselseite des ASSISTENTEN (iU9-W6.3) — dieselbe Komponente, andere
        /// Hülle: randlos, <c>TopLevel = false</c>-tauglich, ohne OK/Abbrechen und ohne
        /// Kostenleiste (<c>Wizard = true</c>).
        /// </summary>
        /// <remarks>
        /// Der Parametersatz wird erst in <c>Bestuecken</c> erfragt, weil Projekt-Id,
        /// Projektname und die geteilte Liste erst dort feststehen —
        /// <c>AssistentSeiten.Erzeugen</c> baut alle dreizehn Seiten im Voraus.
        /// </remarks>
        internal static Form AssistentSeite()
        {
            return new BlazorAssistentSeite<HeizkesselDialog>(
                (projektId, projektName, modelle) =>
                    new Dictionary<string, object>(
                        Gaben(null, projektId, WizardItemClass.KESSEL_TYP, modelle, wizard: true)),
                PROJEKT_MASS);
        }
    }
}
