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

        // =================================================================================
        // W6.4 - Projektdialog
        // =================================================================================

        /// <summary>Gewünschtes Innenmaß des Projektdialogs (Vorläufer: 1 022 × 610).</summary>
        private static readonly Size PROJEKT_MASS = new Size(1100, 760);

        /// <summary>
        /// Zeigt den Projektdialog als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_BHKW_Click</c> und
        /// <c>BHKWKontextMenuCtrl.ContextMenuItemNeu_Click</c>.
        /// </summary>
        /// <param name="besitzer">Fenster, über dem der Dialog erscheint.</param>
        /// <param name="projektId">Projekt; 0 = keines (dann keine Projektkopien).</param>
        /// <param name="idType"><c>BHKW_TYP</c> oder <c>REF_BHKW_TYP</c>.</param>
        /// <param name="modelle">Die geteilte Erzeugerliste — sie wird in place bearbeitet.</param>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, int idType,
                                     List<WErzeugerModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<BhkwDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, idType, modelle, wizard: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<BhkwDialog>(
                Text_("BHKWV_TITEL", "Verwaltung BHKW"), PROJEKT_MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Die BHKW-Seite des ASSISTENTEN — dieselbe Komponente, randlose Hülle,
        /// <c>Wizard = true</c>.
        /// </summary>
        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        /// <summary>
        /// Der PARAMETERSATZ des Projektdialogs. Aufbau und Begründung wie in
        /// <see cref="HeizkesselHuelle"/>; hier stehen nur die BHKW-Eigenheiten.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, int idType,
            List<WErzeugerModel> modelle, bool wizard)
        {
            var stamm = new BHKWStammCtrl();
            var projekt = new BHKWCtrl();

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
                ["Gruppen"] = Gruppen(stamm),
                ["Leistungsstufen"] = Leistungsstufen(),

                ["Filtern"] = new Func<string, int, IReadOnlyList<KatalogZeile>>(
                    (gruppe, stufe) => KatalogZeilen(stamm.Filtern(gruppe, stufe))),

                ["KatalogDetail"] = new Func<string, ErzeugerDetail>(
                    name => DetailZu(BHKWCtrl.StammDetail(name))),

                // FillDetailsFromProjekt: Im Assistenten (kein persistiertes Projekt)
                // stammen die Werte aus den Stammdaten - dieselbe Weiche wie im Bestand.
                ["ProjektDetail"] = new Func<string, ErzeugerDetail>(
                    name => DetailZu(projektId > 0 ? BHKWCtrl.ProjektDetail(name, projektId)
                                                   : BHKWCtrl.StammDetail(name))),

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
                    zeile => Entfernen(projektId, idType, modelle, zuModell, zeile)),

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
                        m.Grenzleistung = zeile.Grenzleistung ?? 0;
                        m.Vorlauf = zeile.Vorlauf ?? 0;
                        m.Ruecklauf = zeile.Ruecklauf ?? 0;
                    }),

                ["SummePtherm"] = new Func<string>(
                    () => SummeLeistung(projektId, idType, modelle).ToString()),

                ["EditorGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => KatalogGaben(name, neu: false)),

                ["EditorGabenNeu"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => KatalogGaben(name, neu: true)),

                ["TraegerGaben"] = new Func<TraegerVorbereitung, IReadOnlyDictionary<string, object>>(
                    TraegerGaben),

                ["KatalogLoeschen"] = new Func<int, string>(id => KatalogLoeschen(stamm, id)),

                ["TitelText"] = Text_("BHKWV_TITEL", "Verwaltung BHKW"),
                ["KopfbandText"] = Text_("BHKWV_KOPFBAND", "Geben Sie Daten zu BHKW ein"),
                ["LabelProjektliste"] = Text_("BHKWV_LBL_PROJEKTLISTE", "Ausgewählte Module:"),
                ["LabelKatalogliste"] = Text_("BHKWV_LBL_KATALOGLISTE", "Module in Datenbank:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["SpalteEigenschaften"] = Text_("BHKWV_SP_EIGENSCHAFTEN", "Eigenschaften"),
                ["LabelHinzu"] = Text_("HZK_TIP_HINZU", "In das Projekt übernehmen"),
                ["LabelEntfernen"] = Text_("HZK_TIP_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["LabelSumme"] = Text_("BHKWV_LBL_SUMME", "Summe aller ausgewählten Module [kWth]:"),
                ["LabelFilterBrennstoff"] = Text_("BHKWV_LBL_FILTER_BRENNSTOFF", "Filtern nach Brennstoffart"),
                ["LabelFilterLeistung"] = Text_("BHKWV_LBL_FILTER_LEISTUNG", "Filtern nach Leistung"),
                ["BtnBearbeitenText"] = Text_("HZK_BTN_BEARBEITEN", "Bearbeiten..."),
                ["BtnNeuText"] = Text_("BHKWV_BTN_NEU", "Neu.."),
                ["BtnLoeschenText"] = Text_("HZK_BTN_LOESCHEN", "Löschen"),
                ["GruppeModul"] = Text_("HZK_GRP_MODUL", "Modul"),
                ["LabelName"] = Text_("BHKWV_LBL_NAME", "Modul-Name:"),
                ["LabelBeschreibung"] = Text_("HZKK_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelTraeger"] = Text_("BHKWV_LBL_TRAEGER", "Brennstoff:"),
                ["LabelGrenzleistung"] = Text_("BHKWV_LBL_GRENZLEISTUNG",
                    "Untere Grenzleistung des ausgewählten Moduls:"),
                ["LabelVorlauf"] = Text_("BHKWV_LBL_VORLAUF", "Vorlauf"),
                ["LabelRuecklauf"] = Text_("BHKWV_LBL_RUECKLAUF", "Rücklauf"),
                ["TraegerTitel"] = MyResource.Resource.KAUSW_TITEL,
                ["EditorTitel"] = Text_("BHKWK_TITEL", "BHKW Eigenschaften"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = Text_("ALLG_BTN_JA", "Ja"),
                ["NeinText"] = Text_("ALLG_BTN_NEIN", "Nein"),
                ["FrageLoeschen"] = Text_("BHKWV_FRAGE_LOESCHEN", "Wollen Sie wirklich das BHKW löschen?"),
                ["TitelLoeschen"] = Text_("HZK_TITEL_LOESCHEN", "Löschen"),
                ["MeldungNameFehlt"] = Text_("HZKK_MSG_NAME_FEHLT", "Bitte einen gültigen Namen eingeben!"),

                ["KostenInvestText"] = Text_("KDLG_KNOPF_INVEST", "Investitionskosten…"),
                ["KostenBetriebText"] = Text_("KDLG_KNOPF_BETRIEB", "Betriebskosten…"),
                ["KostenEnergieText"] = Text_("KDLG_KNOPF_ENERGIE", "Energiekosten…")
            };
        }

        // =================================================================================
        // Die Schreibwege hinter den Delegaten
        // =================================================================================

        private static TraegerVorbereitung Vorbereiten(BHKWStammCtrl stamm, int stammId)
        {
            stamm.ReadSingle(stammId);
            if (stamm.rows == 0)
                return new TraegerVorbereitung(Array.Empty<(int, string)>(), null,
                    Text_("BHKWV_MSG_NICHT_GEFUNDEN",
                          "Das ausgewählte BHKW wurde in den Stammdaten nicht gefunden."));

            int nBrennstoff = stamm.m_Brennstoff;
            var liste = EnergietraegerVarianteCtrl.Energietraeger(
                EnergietraegerVarianteCtrl.KategorieZu(nBrennstoff));

            return new TraegerVorbereitung(liste, nBrennstoff > 0 ? (int?)nBrennstoff : null);
        }

        /// <summary>
        /// Nimmt das BHKW auf. Reihenfolge und Abbruchbedingungen wie in
        /// <c>btn_Hinzu_Click</c> (Z. 412).
        /// </summary>
        private static AufnahmeErgebnis Aufnehmen(
            BHKWStammCtrl stamm, int projektId, int idType, bool wizard,
            List<WErzeugerModel> modelle, Dictionary<int, WErzeugerModel> zuModell,
            Zaehler zaehler, int stammId, EnergietraegerVarianteErgebnis ergebnis)
        {
            stamm.ReadSingle(stammId);
            if (stamm.rows == 0)
                return new AufnahmeErgebnis(null,
                    Text_("BHKWV_MSG_NICHT_GEFUNDEN",
                          "Das ausgewählte BHKW wurde in den Stammdaten nicht gefunden."), true);

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
                Bezeichner = stamm.m_szBezeichner,
                ID_Carrier = traeger.CarrierId
            };

            // W6-E-4 (06.09.2026): Vor- und Ruecklauf kommen aus dem Katalogsatz - und
            // zwar aus der EINEN Wahrheit im Kern statt aus einer dritten Abschrift
            // "Vorlauf = stamm.m_Vorlauf". Sie setzt das Paar nur, wenn der Feldsatz
            // noch keines traegt; ein frisches Modell traegt 0/0.
            AnlagenTemperaturen.AusStammsatz(model, stammId);

            // Anders als beim Heizkessel prueft der Vorlaeufer hier NUR m_ID_Projekt > 0
            // und nicht zusaetzlich den Assistentenbetrieb - im Assistenten ist die
            // Projekt-Id 0, das laeuft also auf dasselbe hinaus.
            if (projektId > 0)
            {
                int projektKopie = new BHKWCtrl().CopyFromStamm(stammId, projektId);
                if (projektKopie <= 0)
                    return new AufnahmeErgebnis(null,
                        Text_("HZK_MSG_KOPIE_FEHLER",
                              "Der Datensatz konnte nicht in das Projekt übernommen werden."), true);
                model.ID_BHKW = projektKopie;
            }
            else
            {
                model.ID_BHKW = stammId;
            }

            modelle.Add(model);
            zuModell[model.ID] = model;

            return new AufnahmeErgebnis(ZeileZu(model), traeger.Meldung, false);
        }

        private static void Entfernen(int projektId, int idType,
                                      List<WErzeugerModel> modelle,
                                      Dictionary<int, WErzeugerModel> zuModell,
                                      ErzeugerZeile zeile)
        {
            if (!zuModell.TryGetValue(zeile.Schluessel, out WErzeugerModel m)) return;

            modelle.Remove(m);
            zuModell.Remove(zeile.Schluessel);

            // Projekt-Kopie nur entfernen, wenn keine weitere Auswahl mehr darauf
            // verweist (mehrere Instanzen desselben BHKW teilen sich eine Tab_BHKW-Kopie).
            bool nochReferenziert = false;
            foreach (WErzeugerModel it in modelle)
                if (it.ID_Type == idType && it.ID_BHKW == m.ID_BHKW) { nochReferenziert = true; break; }

            if (projektId > 0 && !nochReferenziert)
                new BHKWCtrl().DeleteFromProjekt(m.Bezeichner, projektId);
        }

        /// <summary>
        /// Die Summe der thermischen Leistungen (<c>SummeLeistung</c>, Z. 765).
        /// <c>ID_BHKW</c> zeigt bei vorhandenem Projekt auf <c>Tab_BHKW</c>, im
        /// Assistenten auf die Stammdaten.
        /// </summary>
        private static double SummeLeistung(int projektId, int idType, List<WErzeugerModel> modelle)
        {
            double summe = 0;
            var projekt = new BHKWCtrl();
            var stamm = new BHKWStammCtrl();

            foreach (WErzeugerModel m in modelle)
            {
                if (m.ID_Type != idType) continue;

                if (projektId > 0) { projekt.ReadSingle(m.ID_BHKW); summe += projekt.m_Ptherm; }
                else { stamm.ReadSingle(m.ID_BHKW); summe += stamm.m_Ptherm; }
            }
            return summe;
        }

        /// <summary>
        /// Löscht einen Katalogsatz. Leere Rückgabe = gelöscht; sonst der Grund.
        /// ReadOnly-Stammdatensätze dürfen nicht gelöscht werden
        /// (<c>btn_DBBHKW_Löschen_Click</c>, Z. 866).
        /// </summary>
        private static string KatalogLoeschen(BHKWStammCtrl stamm, int id)
        {
            if (stamm.IsReadOnly(id))
                return Text_("BHKWV_MSG_SCHREIBGESCHUETZT",
                    "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.");

            stamm.ReadSingle(id);
            if (stamm.rows == 0 || !stamm.Delete(stamm.m_szBezeichner))
                return Text_("BHKWV_MSG_LOESCHFEHLER", "Der Katalogeintrag konnte nicht gelöscht werden.");

            return "";
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
                GeraetId = m.ID_BHKW,
                CarrierId = m.ID_Carrier,
                Grenzleistung = m.Grenzleistung,
                Vorlauf = m.Vorlauf,
                Ruecklauf = m.Ruecklauf
            };
        }

        /// <summary>Der Detailblock (<c>FillDetailControls</c>, Z. 350).</summary>
        private static ErzeugerDetail DetailZu(BHKWCtrl.BhkwDetail d)
        {
            if (d == null) return new ErzeugerDetail("", "", new List<(string, string)>());

            var felder = new List<(string, string)>
            {
                (Text_("BHKWV_LBL_HERSTELLER", "Hersteller:"), d.Firma),
                (Text_("BHKWV_LBL_PTHERM", "thermische Leistung [kWth]:"), d.Ptherm.ToString()),
                (Text_("BHKWV_LBL_PEL", "elektrische Leistung [kWel]:"), d.Pel.ToString())
            };

            return new ErzeugerDetail(d.Bezeichner, d.Beschreibung, felder);
        }

        /// <summary>
        /// Die Katalogzeilen samt der zweiten Spalte „Eigenschaften" — vier Zeilen in
        /// einer Zelle, genau wie im <c>DataGridView</c> des Vorläufers (Z. 205-209).
        /// </summary>
        private static IReadOnlyList<KatalogZeile> KatalogZeilen(
            IReadOnlyList<BHKWStammCtrl.KatalogZeile> quelle)
        {
            var liste = new List<KatalogZeile>();
            foreach (var z in quelle)
                liste.Add(new KatalogZeile(z.Id, z.Bezeichner,
                    z.Firma + "\n" + Text_("BHKWV_ZELLE_BRENNSTOFF", "Brennstoff:") + " " + z.Brennstoff +
                    "\nPtherm: " + z.Ptherm + " kW" +
                    "\nPel: " + z.Pel + " kW"));
            return liste;
        }

        /// <summary>„Alle" voran, dann die Brennstoffgruppen — wie <c>SetControls</c>.</summary>
        private static IReadOnlyList<string> Gruppen(BHKWStammCtrl stamm)
        {
            var liste = new List<string> { "Alle" };
            liste.AddRange(stamm.Brennstoffart_Gruppe);
            return liste;
        }

        /// <summary>
        /// „Alle" voran, dann die acht Stufen aus <c>BHKWStammCtrl.LeistungText</c> —
        /// der Index passt damit auf <c>LeistungFilterText</c>.
        /// </summary>
        private static IReadOnlyList<string> Leistungsstufen()
        {
            var liste = new List<string> { Text_("HZK_STUFE_ALLE", "Alle") };
            foreach (string t in BHKWStammCtrl.LeistungText)
                if (t.Length > 0) liste.Add(t);
            return liste;
        }

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
    }
}
