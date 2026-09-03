using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
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
    }
}
