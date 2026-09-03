using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der BHKW-Dialoge (iU9-W6.2 und W6.4).
    ///
    /// <para><b>Eine Datei für beide</b> — wie <see cref="HeizkesselHuelle"/> und aus
    /// demselben Grund: Katalogeditor und Projektdialog teilen ihre Datenseite
    /// (<see cref="BHKWStammCtrl"/>, die Brennstoffliste), und der Projektdialog zeigt
    /// den Katalogeditor in einer <c>Ueberlagerung</c>.</para>
    ///
    /// <para><b>Die Abbildung zwischen den Welten liegt hier.</b>
    /// <see cref="BhkwKatalogDaten"/> ist der Feldsatz der Oberfläche,
    /// <see cref="BHKWStammModel"/> der des Kerns.</para>
    /// </summary>
    internal static class BhkwHuelle
    {
        /// <summary>Gewünschtes Innenmaß des Katalogeditors (Vorläufer: 791 × 686 zzgl. Kostenleiste).</summary>
        private static readonly Size KATALOG_MASS = new Size(980, 820);

        // =================================================================================
        // W6.2 - Katalogeditor
        // =================================================================================

        /// <summary>
        /// Zeigt den Katalogeditor als eigenes Fenster — der Weg der WinForms-Aufrufer
        /// <c>Form_BHKWAdmin.btn_Bearbeiten_Click</c> und <c>btn_Neu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Editor erscheint.</param>
        /// <param name="name">Bezeichner des Katalogsatzes bzw. der gewünschte neue Name.</param>
        /// <param name="neu"><c>true</c> = Modus „Neu" (nur „Speichern" ist aktiv).</param>
        /// <returns><c>true</c>, wenn geschrieben wurde — dann lädt der Aufrufer neu.</returns>
        internal static bool KatalogBearbeiten(IWin32Window besitzer, string name, bool neu)
        {
            bool gespeichert = false;
            BlazorDialogForm<BhkwKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(KatalogGaben(name, neu))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<string>(new object(), n =>
                {
                    gespeichert = n != null;
                    if (dlg != null) dlg.Schliessen(n != null);
                })
            };

            dlg = new BlazorDialogForm<BhkwKatalogDialog>(
                Text_("BHKWK_TITEL", "BHKW Eigenschaften"), KATALOG_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return gespeichert;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Katalogeditors — für die Anzeige in einer
        /// <c>Ueberlagerung</c> des Projektdialogs (W6.4). <c>Geschlossen</c> setzt dort
        /// der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben(string name, bool neu)
        {
            var ctrl = new BHKWStammCtrl();
            var daten = new BhkwKatalogDaten();

            if (neu)
            {
                // SetControls, Zweig MODE_NEU: ein frisches Modell, nur der Name steht.
                daten.Bezeichner = name ?? "";
            }
            else
            {
                BHKWStammModel m = ctrl.ReadModel(name);
                if (m != null) AusModell(daten, m);
                daten.Bezeichner = name ?? "";
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Modus"] = neu ? KatalogModus.Neu : KatalogModus.Bearbeiten,
                ["Brennstoffe"] = Brennstoffe(ctrl),

                ["Ueberschreiben"] = new Func<BhkwKatalogDaten, bool, KatalogSpeicherErgebnis>(
                    (d, schutz) => Uebersetzen(BHKWStammCtrl.Ueberschreiben(NachModell(d), schutz))),

                ["Anlegen"] = new Func<BhkwKatalogDaten, string, KatalogSpeicherErgebnis>(
                    (d, n) => Uebersetzen(BHKWStammCtrl.Anlegen(NachModell(d), n))),

                ["Co2Vorgabe"] = new Func<string, double?>(EmissionsVorgaben.BhkwCo2),

                ["EmissionsVorgabe"] = new Func<string, bool, double,
                        (double? SO2, double? CO2, double? NOx, double? CO, double? Staub)>(
                    (brennstoff, scr, ptherm) =>
                    {
                        EmissionsVorgaben.BhkwSatz s = EmissionsVorgaben.Bhkw(brennstoff, scr, ptherm);
                        return (s.SO2, s.CO2, s.NOx, s.CO, s.Staub);
                    }),

                ["Summe"] = new Func<double, double, double, double, double, double>(BHKWKosten.Summe),
                ["JeKWelBestimmbar"] = new Func<double, bool>(BHKWKosten.JeKWelBestimmbar),
                ["JeKWel"] = new Func<double, double, double>(BHKWKosten.JeKWel),

                ["TitelText"] = Text_("BHKWK_TITEL", "BHKW Eigenschaften"),
                ["GruppeBezeichnung"] = Text_("BHKWK_GRP_BEZEICHNUNG", "Modul"),
                ["LabelName"] = Text_("BHKWK_LBL_NAME", "Modulname:"),
                ["LabelHersteller"] = Text_("BHKWK_LBL_HERSTELLER", "Hersteller:"),
                ["LabelMotortyp"] = Text_("BHKWK_LBL_MOTORTYP", "Motortyp:"),
                ["LabelBeschreibung"] = Text_("BHKWK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["GruppeTechnik"] = Text_("BHKWK_GRP_TECHNIK", "Technische Daten"),
                ["LabelPtherm"] = Text_("BHKWK_LBL_PTHERM", "Thermische Leistung:"),
                ["FeldPtherm"] = Text_("BHKWK_FELD_PTHERM", "thermische Leistung"),
                ["LabelPel"] = Text_("BHKWK_LBL_PEL", "Elektrische Leistung:"),
                ["FeldPel"] = Text_("BHKWK_FELD_PEL", "elektrische Leistung"),
                ["LabelWirkungsgrad"] = Text_("BHKWK_LBL_WIRKUNGSGRAD", "Ges. Wirkungsgrad:"),
                ["FeldWirkungsgrad"] = Text_("BHKWK_FELD_WIRKUNGSGRAD", "Gesamtwirkungsgrad"),
                ["HinweisWirkungsgrad"] = Text_("BHKWK_HINT_WIRKUNGSGRAD", "(z. B. 0,85)"),
                ["LabelGrenzleistung"] = Text_("BHKWK_LBL_GRENZLEISTUNG", "Untere Grenzleistung:"),
                ["FeldGrenzleistung"] = Text_("BHKWK_FELD_GRENZLEISTUNG", "untere Grenzleistung"),
                ["LabelEnergietraeger"] = Text_("BHKWK_LBL_ENERGIETRAEGER", "Energieträger:"),
                ["LabelVorlauf"] = Text_("BHKWK_LBL_VORLAUF", "Vorlauf:"),
                ["FeldVorlauf"] = Text_("BHKWK_FELD_VORLAUF", "Vorlauftemperatur"),
                ["LabelRuecklauf"] = Text_("BHKWK_LBL_RUECKLAUF", "Rücklauf:"),
                ["FeldRuecklauf"] = Text_("BHKWK_FELD_RUECKLAUF", "Rücklauftemperatur"),
                ["GruppeKosten"] = Text_("BHKWK_GRP_KOSTEN", "Eingabedaten zur Berechnung der Kosten"),
                ["LabelModul"] = Text_("BHKWK_LBL_MODUL", "Modul:"),
                ["FeldModul"] = Text_("BHKWK_FELD_MODUL", "Kosten Modul"),
                ["LabelMontage"] = Text_("BHKWK_LBL_MONTAGE", "Montage und Inbetriebnahme:"),
                ["FeldMontage"] = Text_("BHKWK_FELD_MONTAGE", "Kosten Montage und Inbetriebnahme"),
                ["LabelLieferung"] = Text_("BHKWK_LBL_LIEFERUNG", "Lieferung (50 km Umkreis):"),
                ["FeldLieferung"] = Text_("BHKWK_FELD_LIEFERUNG", "Kosten Lieferung"),
                ["LabelSchallschutz"] = Text_("BHKWK_LBL_SCHALLSCHUTZ", "Schallschutzhaube:"),
                ["FeldSchallschutz"] = Text_("BHKWK_FELD_SCHALLSCHUTZ", "Kosten Schallschutzhaube"),
                ["LabelAbgasreinigung"] = Text_("BHKWK_LBL_ABGASREINIGUNG", "Abgasreinigung, z. B. Kat:"),
                ["FeldAbgasreinigung"] = Text_("BHKWK_FELD_ABGASREINIGUNG", "Kosten Abgasreinigung"),
                ["LabelSumme"] = MyResource.Resource.BHKW_SUMME_LBL,
                ["LabelInvest"] = Text_("BHKWK_LBL_INVEST", "Investitionskosten [€ / kWel]:"),
                ["LabelRaumbedarf"] = Text_("BHKWK_LBL_RAUMBEDARF", "Raumbedarf:"),
                ["FeldRaumbedarf"] = Text_("BHKWK_FELD_RAUMBEDARF", "Raumbedarf"),
                ["LabelWartung"] = Text_("BHKWK_LBL_WARTUNG", "Wartungskosten:"),
                ["FeldWartung"] = Text_("BHKWK_FELD_WARTUNG", "Wartungskosten"),
                ["LabelNutzungsdauer"] = Text_("BHKWK_LBL_NUTZUNGSDAUER", "Nutzungsdauer:"),
                ["FeldNutzungsdauer"] = Text_("BHKWK_FELD_NUTZUNGSDAUER", "Nutzungsdauer"),
                ["EinheitJahre"] = Text_("BHKWK_EINHEIT_JAHRE", "Jahre"),
                ["InvestUnbestimmt"] = MyResource.Resource.BHKW_INVEST_UNBESTIMMT,
                ["HinweisAbgeleitet"] = MyResource.Resource.BHKW_INVEST_HINWEIS_ABGELEITET,
                ["HinweisUnbestimmt"] = MyResource.Resource.BHKW_INVEST_HINWEIS_UNBESTIMMT,
                ["HinweisAbweichung"] = MyResource.Resource.BHKW_INVEST_HINWEIS_ABWEICHUNG,
                ["GruppeBehg"] = Text_("BHKWK_GRP_BEHG", "Emissionen nach BEHG-V"),
                ["BehgZeile"] = Text_("HZKK_BEHG_ZEILE", "für Heizzwecke in t CO2 / GJ"),
                ["BehgOel"] = Text_("HZKK_BEHG_OEL", "Heizöl: 0,0808"),
                ["BehgFluessiggas"] = Text_("HZKK_BEHG_FLUESSIGGAS", "Flüssiggas: 0,0663"),
                ["BehgErdgas"] = Text_("HZKK_BEHG_ERDGAS", "Erdgas: 0,056"),
                ["BtnCo2Text"] = Text_("HZKK_BTN_CO2", "CO2 BEHG"),
                ["GruppeEmissionen"] = Text_("HZKK_GRP_EMISSIONEN",
                    "Emissionsfaktoren bezogen auf den Brennstoffverbrauch"),
                ["LabelStaub"] = Text_("HZKK_LBL_STAUB", "Staub:"),
                ["FeldStaub"] = Text_("BHKWK_FELD_STAUB", "Staub-Emission"),
                ["LabelScr"] = Text_("BHKWK_LBL_SCR", "mit SCR"),
                ["BtnEintragenText"] = Text_("BHKWK_BTN_EINTRAGEN", "Eintragen"),
                ["BtnUeberschreibenText"] = Text_("HZKK_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("HZKK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = MyResource.Resource.ADM_BTN_SPEICHERN,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["FrageSchreibschutz"] = Text_("BHKWK_FRAGE_SCHREIBSCHUTZ",
                    "Dieser Datensatz stammt aus dem Auslieferungskatalog und ist schreibgeschützt." +
                    Environment.NewLine + Environment.NewLine +
                    "Soll er trotzdem überschrieben werden?"),
                ["TitelSchreibschutz"] = Text_("BHKWK_TITEL_SCHREIBSCHUTZ", "Schreibgeschützter Datensatz"),
                ["MeldungZahlUngueltig"] = Text_("HZKK_MSG_ZAHL",
                    "Bitte für \"{0}\" eine Zahl eingeben (Dezimaltrennzeichen Komma oder Punkt)."),
                ["MeldungNameFehlt"] = Text_("HZKK_MSG_NAME_FEHLT", "Bitte einen gültigen Namen eingeben!")
            };
        }

        // =================================================================================
        // Abbildung zwischen Oberflaechen- und Kernfeldsatz
        // =================================================================================

        /// <summary>Kern → Oberfläche.</summary>
        private static void AusModell(BhkwKatalogDaten d, BHKWStammModel m)
        {
            d.Firma = m.m_szFirma ?? "";
            d.Beschreibung = m.m_szBeschreibung ?? "";
            d.Motortyp = m.m_szMotortyp ?? "";
            d.Ptherm = m.m_Ptherm;
            d.Pel = m.m_Pel;
            d.Wirkungsgrad = m.m_Wirkungsgrad;
            d.Grenzleistung = m.m_Grenzleistung;
            d.Vorlauf = m.m_Vorlauf;
            d.Ruecklauf = m.m_Ruecklauf;
            d.KostenModul = m.m_Kosten_Modul;
            d.KostenMontage = m.m_Kosten_Montage;
            d.KostenLieferung = m.m_Kosten_Lieferung;
            d.KostenSchallschutzhaube = m.m_Kosten_Schallschutzhaube;
            d.KostenAbgasreinigung = m.m_Kosten_Abgasreinigung;
            d.Raumbedarf = m.m_Raumbedarf;
            d.WartungskostenJeKWhel = m.m_Wartungskosten_kWhel;
            d.Nutzungsdauer = m.m_Nutzungsdauer;
            d.InvestitionJeKWel = m.m_Investition_KWel;
            d.CO2 = m.m_CO2;
            d.SO2 = m.m_SO2;
            d.NOx = m.m_NOx;
            d.CO = m.m_CO;
            d.Staub = m.m_Staub;
            d.Katalogsatz = m.m_bReadOnly;

            // SetControls liest 0-basiert zurueck: comboBox_Brennstoff.SelectedIndex =
            // brennstoff >= 1 ? brennstoff : 1. Der Vorlaeufer nahm die 1 auch fuer die
            // 0 - dieselbe Regel hier, damit die Anzeige gleich bleibt.
            d.Brennstoff = m.m_Brennstoff >= 1 ? m.m_Brennstoff : 1;
        }

        /// <summary>
        /// Oberfläche → Kern. Hier gilt „leer = 0"; der Wert je kWel entsteht NEU aus den
        /// Posten und Pel (<c>EingabenPruefen</c>, Z. 355) und nicht aus dem Anzeigefeld.
        /// </summary>
        private static BHKWStammModel NachModell(BhkwKatalogDaten d)
        {
            double modul = d.KostenModul ?? 0;
            double montage = d.KostenMontage ?? 0;
            double lieferung = d.KostenLieferung ?? 0;
            double schall = d.KostenSchallschutzhaube ?? 0;
            double abgas = d.KostenAbgasreinigung ?? 0;
            double pel = d.Pel ?? 0;

            return new BHKWStammModel
            {
                m_szBezeichner = d.Bezeichner ?? "",
                m_szFirma = d.Firma ?? "",
                m_szBeschreibung = d.Beschreibung ?? "",
                m_szMotortyp = d.Motortyp ?? "",
                m_Ptherm = d.Ptherm ?? 0,
                m_Pel = pel,
                m_Wirkungsgrad = d.Wirkungsgrad ?? 0,
                m_Grenzleistung = d.Grenzleistung ?? 0,
                m_Vorlauf = d.Vorlauf ?? 0,
                m_Ruecklauf = d.Ruecklauf ?? 0,
                m_Kosten_Modul = modul,
                m_Kosten_Montage = montage,
                m_Kosten_Lieferung = lieferung,
                m_Kosten_Schallschutzhaube = schall,
                m_Kosten_Abgasreinigung = abgas,
                m_Raumbedarf = d.Raumbedarf ?? 0,
                m_Wartungskosten_kWhel = d.WartungskostenJeKWhel ?? 0,
                m_Nutzungsdauer = d.Nutzungsdauer ?? 0,
                m_CO2 = d.CO2 ?? 0,
                m_SO2 = d.SO2 ?? 0,
                m_NOx = d.NOx ?? 0,
                m_CO = d.CO ?? 0,
                m_Staub = d.Staub ?? 0,

                // Abgeleitet, nie aus dem Anzeigefeld gelesen.
                m_Investition_KWel = BHKWKosten.JeKWel(
                    BHKWKosten.Summe(modul, montage, lieferung, schall, abgas), pel),

                // InitDatensatzUpdate: SelectedIndex OHNE + 1, ohne Wahl die 1.
                m_Brennstoff = d.Brennstoff ?? 1
            };
        }

        private static KatalogSpeicherErgebnis Uebersetzen(BHKWStammCtrl.SpeicherErgebnis e)
        {
            return new KatalogSpeicherErgebnis(e.Ok, e.Meldung, e.Name);
        }

        /// <summary>
        /// Die Energieträger als (Id, Text). Die Id ist der 0-BASIERTE Listenindex — so
        /// liest und schreibt der Vorläufer (<c>SelectedIndex</c> ohne <c>+ 1</c>).
        /// </summary>
        private static IReadOnlyList<(int Id, string Text)> Brennstoffe(BHKWStammCtrl ctrl)
        {
            var liste = new List<(int, string)>();
            for (int i = 0; i < ctrl.Brennstoffart.Count; i++)
                liste.Add((i, ctrl.Brennstoffart[i]));
            return liste;
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
