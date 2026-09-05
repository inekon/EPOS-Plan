using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WEGE der Ergebnishülle (iU9-W11b.13): die sechs CSV-Exporte, die zwei
    /// Überlagerungen mit Rückgabe und der Variantenvergleich.
    ///
    /// <para>Der CSV-Export läuft über <c>CsvExportClass</c> und damit über
    /// <c>Dienste.Datei</c>/<c>Dienste.Dialog</c> — er hat seinen Speichern-Dialog seit
    /// iU5 nicht mehr selbst.</para>
    /// </summary>
    internal sealed partial class SimulationErgebnisHuelle
    {
        // =================================================================
        // Die CSV-Exporte
        // =================================================================

        /// <summary>
        /// Energiebedarf: Zeitstempel, Außentemperatur, Wärmelast, je Kanal eine Spalte,
        /// Strombedarf (wörtlich <c>btn_CsvExportBedarf_Click</c> :2696-2744).
        /// </summary>
        private void CsvBedarf()
        {
            if (_waermebedarf == null || _waermebedarf.Waermebedarf_Gesamt <= 0)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_ENERGIEBEDARF,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            var spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMELAST, _waermebedarf.Waermebedarf)
            };

            // Je Kanal mit Jahressumme > 0 eine Spalte - der kWh-Vektor, NICHT die
            // normierte Prozentkurve des Diagramms (Begründung :2714-2721).
            for (int k = 0; k < Kanal.ANZAHL; k++)
            {
                float[] werte = SimulationControl.BedarfKanalStuendlich(_waermebedarf, k);
                if (werte == null || Jahressumme(werte) <= 0) continue;
                spalten.Add(new CsvSpalte(
                    MyResource.Resource.CHART_CSV_WAERMELAST + " " + KANALNAMEN[k], werte));
            }

            spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_STROMBEDARF,
                                      _strombedarf.Strombedarf_viertelStundenwerte));

            CsvExportClass.Export(
                string.Format(MyResource.Resource.CHART_DATEI_ENERGIEBEDARF, m_ID_Projekt),
                _waermebedarf.Stundentemperatur, spalten, false);
        }

        /// <summary>
        /// Wärmepumpe: Bedarf, Heizstab, Produktion, Strombedarf und je Speicher DREI
        /// Spalten (wörtlich <c>btn_CsvExportWP_Click</c> :2745-2775).
        /// </summary>
        private void CsvWaermepumpe()
        {
            if (!_ergebnisGueltig || !sim.bSimulationWP || sim.simulation_wp == null)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_WAERMEPUMPE,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            var spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF,
                              sim.simulation_wp.Waermebedarf_stuendlich),
                new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZSTAB,
                              sim.simulation_wp.Heizstab_stuendlich),
                new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEPRODUKTION_WP,
                              sim.simulation_wp.WP_Waermeproduktion_stuendlich),
                new CsvSpalte(MyResource.Resource.CHART_CSV_STROMBEDARF_WP,
                              sim.simulation_wp.WP_Strombedarf_stuendlich)
            };

            foreach (SimulationPufferspeicher sp in sim.AlleSpeicher())
            {
                string name = sp.Anzeige();
                spalten.Add(new CsvSpalte(
                    string.Format(MyResource.Resource.CHART_CSV_SPEICHER_LADUNG, name),
                    sp.Ladung_stuendlich));
                spalten.Add(new CsvSpalte(
                    string.Format(MyResource.Resource.CHART_CSV_SPEICHER_ENTLADUNG, name),
                    sp.Entladung_stuendlich));
                spalten.Add(new CsvSpalte(
                    string.Format(MyResource.Resource.CHART_CSV_SPEICHER_INHALT, name),
                    sp.SOC_stuendlich));
            }

            CsvExportClass.Export(
                string.Format(MyResource.Resource.CHART_DATEI_WAERMEPUMPE, m_ID_Projekt),
                sim.simulation_wp.Temperatur, spalten, false);
        }

        /// <summary>Heizkessel (wörtlich <c>btn_CsvExportKessel_Click</c> :1228-1245).</summary>
        private void CsvHeizkessel()
        {
            if (!_ergebnisGueltig || !sim.bSimulationKessel || sim.simulation_spk == null)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_HEIZKESSEL,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            var spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF_GESAMT,
                              _waermebedarf.Waermebedarf),
                new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEBEDARF_KESSELSTUFE,
                              sim.simulation_spk.Waermebedarf),
                new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZKESSEL,
                              sim.simulation_spk.Kesselleistung_stuendlich),
                new CsvSpalte(MyResource.Resource.CHART_CSV_RESTWAERME,
                              sim.simulation_spk.Restwaerme)
            };

            CsvExportClass.Export(
                string.Format(MyResource.Resource.CHART_DATEI_HEIZKESSEL, m_ID_Projekt),
                _waermebedarf.Stundentemperatur, spalten, false);
        }

        /// <summary>
        /// Stromspeicher: SoC, Ladung, Entladung, Geldwert — und die zwei Netzpfade nur
        /// mit Preissteuerung (wörtlich <c>btn_CsvExportSpeicher_Click</c> :7477-7512).
        /// </summary>
        private void CsvSpeicher()
        {
            if (!_ergebnisGueltig || sim.Speicherergebnis == null)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SP_ERG_KEIN_LAUF,
                                       MyResource.Resource.SIM_STROMSPEICHER);
                return;
            }

            SpeicherErgebnis erg = sim.Speicherergebnis;

            var spalten = new List<CsvSpalte>
            {
                new CsvSpalte(MyResource.Resource.SP_CSV_SOC, RasterAdapter.ZuFloat(erg.SoCKwh)),
                new CsvSpalte(MyResource.Resource.SP_CSV_LADUNG, RasterAdapter.ZuFloat(erg.LadungAcKwh)),
                new CsvSpalte(MyResource.Resource.SP_CSV_ENTLADUNG, RasterAdapter.ZuFloat(erg.EntladungAcKwh)),
                new CsvSpalte(MyResource.Resource.SP_CSV_GELDWERT, RasterAdapter.ZuFloat(erg.GeldwertEur))
            };

            ArbitrageErgebnis arb = sim.Speicherkontext != null
                ? sim.Speicherkontext.Arbitrageergebnis : null;
            if (arb != null)
            {
                spalten.Add(new CsvSpalte(MyResource.Resource.ARB_CSV_LADUNG_NETZ,
                                          RasterAdapter.ZuFloat(arb.LadungNetzAcKwh)));
                spalten.Add(new CsvSpalte(MyResource.Resource.ARB_CSV_VERKAUF,
                                          RasterAdapter.ZuFloat(arb.VerkaufAcKwh)));
            }

            CsvExportClass.Export(
                string.Format(MyResource.Resource.SP_DATEI_STROMSPEICHER, m_ID_Projekt),
                _waermebedarf.Stundentemperatur, spalten, true);
        }

        /// <summary>
        /// Wärmegang: je angehakter Reihe eine Spalte, der Spaltenkopf um die Bedarfsart
        /// ergänzt, dazu je sichtbarem Speicher eine Füllstandsspalte.
        /// <b>IMMER CHRONOLOGISCH</b>, auch im sortierten Modus (Begründung
        /// <c>NavigatorWaerme</c> :295-297).
        /// </summary>
        private void CsvWaermegang(int kanal, IReadOnlyList<string> erzeuger,
                                   IReadOnlyList<string> speicher)
        {
            if (!_ergebnisGueltig)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            string zusatz = kanal < 0 ? "" : " " + KANALNAMEN[kanal];
            var spalten = new List<CsvSpalte>();

            foreach (string s in erzeuger ?? new List<string>())
            {
                float[] werte = WaermegangVektor(s, kanal);
                if (werte == null) continue;
                spalten.Add(new CsvSpalte(WaermegangName(s) + zusatz, werte));
            }

            List<SimulationPufferspeicher> alle = sim.AlleSpeicher();
            for (int i = 0; i < alle.Count; i++)
            {
                SimulationPufferspeicher sp = alle[i];
                if (sp == null) continue;
                if (speicher == null || !speicher.Contains(sp.Schluessel(i))) continue;
                spalten.Add(new CsvSpalte(
                    string.Format(MyResource.Resource.CHART_CSV_SPEICHERFUELLSTAND, sp.Anzeige()),
                    sp.SOC_stuendlich));
            }

            if (spalten.Count == 0)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            CsvExportClass.Export(
                string.Format(MyResource.Resource.CHART_DATEI_WAERMEPUMPE, m_ID_Projekt),
                _waermebedarf.Stundentemperatur, spalten, false);
        }

        private float[] WaermegangVektor(string schluessel, int kanal)
        {
            switch (schluessel)
            {
                case "WAERMEPUMPE":
                    return kanal < 0 ? sim.simulation_wp.WP_Waermeproduktion_stuendlich
                                     : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_WP, kanal);
                case "HEIZSTAB":
                    return kanal < 0 ? sim.simulation_wp.Heizstab_stuendlich
                                     : sim.HeizstabKanalStuendlich(kanal);
                case "HEIZKESSEL":
                    return kanal < 0 ? sim.simulation_spk.Kesselleistung_stuendlich
                                     : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_KESSEL, kanal);
                case "SOLARTHERMIE":
                    return kanal < 0
                        ? Array.ConvertAll(sim.simulation_solarthermie.Waermeproduktion, x => (float)x)
                        : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_SOLARTHERMIE, kanal);
                case "BHKW_WAERME":
                    return kanal < 0 ? sim.simulation_bhkw.waermeproduktion
                                     : sim.DeckungKanalStuendlich(ProjektPuffer.TYP_BHKW, kanal);
                default:
                    return null;
            }
        }

        private static string WaermegangName(string schluessel)
        {
            switch (schluessel)
            {
                case "WAERMEPUMPE": return MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
                case "HEIZSTAB": return MyResource.Resource.CHART_SEGMENT_HEIZSTAB;
                case "HEIZKESSEL": return MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
                case "SOLARTHERMIE": return MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE;
                case "BHKW_WAERME": return MyResource.Resource.SIM_ERZEUGERNAME_BHKW;
                default: return schluessel;
            }
        }

        /// <summary>
        /// Stromgang: je angehakter Serie eine Spalte (wörtlich
        /// <c>NavigatorStrom.btn_CsvExport_Click</c> :105-129).
        /// </summary>
        private void CsvStromgang(IReadOnlyList<string> reihen)
        {
            if (!_ergebnisGueltig)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            var spalten = new List<CsvSpalte>();
            foreach (string s in reihen ?? new List<string>())
            {
                switch (s)
                {
                    case "PROFIL_LASTGANG":
                        spalten.Add(new CsvSpalte(MyResource.Resource.CHART_LEGENDE_PROFIL_LASTGANG,
                                                  _strombedarf.Strombedarf_viertelStundenwerte));
                        break;
                    case "WAERMEPUMPE":
                        spalten.Add(new CsvSpalte(MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE,
                                                  sim.simulation_wp.WP_Strombedarf_stuendlich));
                        break;
                    case "HEIZSTAB":
                        spalten.Add(new CsvSpalte(MyResource.Resource.CHART_SEGMENT_HEIZSTAB,
                                                  sim.simulation_wp.Heizstab_stuendlich));
                        break;
                    case "HEIZKESSEL":
                        spalten.Add(new CsvSpalte(MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL,
                                                  sim.simulation_spk.Strombedarf_stuendlich));
                        break;
                    case "BHKW_STROM":
                        spalten.Add(new CsvSpalte(MyResource.Resource.SIM_ERZEUGERNAME_BHKW,
                                                  sim.simulation_bhkw.stromproduktion));
                        break;
                    case "PV":
                        spalten.Add(new CsvSpalte(MyResource.Resource.SIM_PHOTOVOLTAIK,
                                                  sim.simulation_pv.Stromproduktion_viertelstunde));
                        break;
                }
            }

            if (spalten.Count == 0)
            {
                WindowsFormsApplication1.Dienste.Dialog.Meldung(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION,
                                       MyResource.Resource.SIM_BTN_CSV_EXPORT);
                return;
            }

            CsvExportClass.Export(
                string.Format(MyResource.Resource.CHART_DATEI_STROMBEDARF, m_ID_Projekt),
                _waermebedarf.Stundentemperatur, spalten, true);
        }

        // =================================================================
        // Der Wärmepumpendialog (Doppelklick auf eine Modulzeile)
        // =================================================================

        /// <summary>Die WP-Anlagen des Projekts — die Liste, die der Dialog bearbeitet.</summary>
        private List<WErzeugerModel> _wpModelle;

        private IReadOnlyDictionary<string, object> WaermepumpenGaben()
        {
            _wpModelle = WErzeugerCtrl.ModelleJeTyp(m_ID_Projekt, WizardItemClass.WP_TYP);
            return WaermepumpenHuelle.Gaben(_fenster, m_ID_Projekt, _wpModelle, wizard: false);
        }

        /// <summary>
        /// Nach dem Übernehmen die Anlagen des Projekts neu schreiben — wörtlich
        /// <c>listView_SimWP_MouseDown</c> :5145-5150.
        /// </summary>
        private void WaermepumpenFertig(bool uebernommen)
        {
            if (!uebernommen || _wpModelle == null) return;

            WizardCtrl wizctrl = new WizardCtrl();
            wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.WP_TYP);
            wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, _wpModelle);
        }

        // =================================================================
        // Der Variantenvergleich
        // =================================================================

        private List<WErzeugerCtrl.AnlagenZeile> _vergleichsanlagen;

        /// <summary>
        /// Rechnet jede Speichervariante des Projekts einmal auf dem vorliegenden
        /// Simulationslauf — NEBENLÄUFIG, mit echtem Fortschritt („n von m"). Der
        /// Vorläufer rechnete synchron mit <c>Cursor.WaitCursor</c> und ohne Abbruch.
        ///
        /// <para>Die Reihenfolge ist die der Anlagenliste (<c>ORDER BY ID</c>) — dieselbe,
        /// die die Übersicht im Hauptformular und <c>ReadAllByProjekt</c> benutzen.</para>
        /// </summary>
        private async Task<VergleichDaten> VergleichRechnen(Action<double?, string> melder)
        {
            var d = new VergleichDaten();

            Dictionary<int, StromspeicherVarianteModel> varianten = VariantenLesen();
            _vergleichsanlagen = WErzeugerCtrl.AnlagenJeTyp(m_ID_Projekt, WizardItemClass.SP_TYP);

            if (_vergleichsanlagen == null || _vergleichsanlagen.Count == 0)
            {
                d.Status = MyResource.Resource.VAR_VGL_STATUS_LEER;
                d.StatusWarnung = true;
                return d;
            }

            var protokoll = new List<string>();
            var zeilen = new List<Vergleichszeile>();
            Stopwatch uhr = Stopwatch.StartNew();

            for (int i = 0; i < _vergleichsanlagen.Count; i++)
            {
                WErzeugerCtrl.AnlagenZeile r = _vergleichsanlagen[i];
                melder((i + 1.0) / _vergleichsanlagen.Count,
                       string.Format(CultureInfo.CurrentCulture, "{0} / {1}",
                                     i + 1, _vergleichsanlagen.Count));

                Vergleichszeile z = await Task.Run(() => ZeileRechnen(r, varianten, protokoll));
                zeilen.Add(z);
            }

            uhr.Stop();

            d.Zeilen = zeilen;
            d.BesteZeile = BesteZeileNachDeltaJ(zeilen);
            d.Protokoll = string.Join(Environment.NewLine, protokoll);
            d.HinweisKeineAktive = zeilen.Count > 0 && !zeilen.Exists(z => z.Aktiv);

            if (d.BesteZeile < 0)
            {
                d.Status = MyResource.Resource.VAR_VGL_STATUS_OHNE_ERGEBNIS;
                d.StatusWarnung = true;
            }
            else
            {
                d.Status = string.Format(CultureInfo.CurrentCulture,
                                         MyResource.Resource.VAR_VGL_STATUS,
                                         zeilen.Count, uhr.ElapsedMilliseconds,
                                         zeilen[d.BesteZeile].Bezeichnung);
            }

            return d;
        }

        private Dictionary<int, StromspeicherVarianteModel> VariantenLesen()
        {
            var treffer = new Dictionary<int, StromspeicherVarianteModel>();

            try
            {
                foreach (StromspeicherVarianteModel v in
                         new StromspeicherVarianteCtrl().ReadAllByProjekt(m_ID_Projekt))
                    if (v.ID_Energieanlage > 0 && !treffer.ContainsKey(v.ID_Energieanlage))
                        treffer.Add(v.ID_Energieanlage, v);
            }
            catch (Exception ex)
            {
                // Datenbank vor Migrationsschritt 11b: Der Vergleich läuft dann mit den
                // Vorgabewerten der Engine weiter, nur ohne Aktiv-Kennzeichnung.
                Console.WriteLine("Die Speichervarianten konnten nicht gelesen werden: " + ex.Message);
            }

            return treffer;
        }

        /// <summary>Ein Lauf. Wirft nicht — ein Fehlschlag wird zur Fehlerzeile.</summary>
        private Vergleichszeile ZeileRechnen(WErzeugerCtrl.AnlagenZeile r,
                                             Dictionary<int, StromspeicherVarianteModel> varianten,
                                             List<string> protokoll)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            var z = new Vergleichszeile
            {
                IdEnergieanlage = r.Id,
                Bezeichnung = r.Bezeichner ?? ""
            };

            // Betriebs- und Berechnungsart stehen in der Variantenzeile und sind auch
            // dann bekannt, wenn der Lauf scheitert - die Fehlerzeile bleibt aussagekräftig.
            StromspeicherVarianteModel v;
            if (varianten.TryGetValue(r.Id, out v) && v != null)
            {
                z.Aktiv = v.Aktiv;
                z.Betriebsart = SpeicherAnzeigeCtrl.BetriebsartText(v.Betriebsart);
                z.Berechnungsart = SpeicherAnzeigeCtrl.BerechnungsartText(v.Berechnungsart);
            }

            StromspeicherSimCtrl ctrlSp = new StromspeicherSimCtrl();
            SpeicherErgebnis ergebnis;

            try
            {
                ergebnis = ctrlSp.RechneVariante(sim, m_ID_Projekt, r.Id);
            }
            catch (Exception ex)
            {
                z.Hinweis = ex.Message;
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE,
                                            z.Bezeichnung, ex.Message));
                return z;
            }

            if (ergebnis == null)
            {
                z.Hinweis = string.IsNullOrEmpty(ctrlSp.LetzterHinweis)
                    ? MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER
                    : ctrlSp.LetzterHinweis;
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE,
                                            z.Bezeichnung, z.Hinweis));
                return z;
            }

            StromspeicherLaufKontext kontext = ctrlSp.LetzterKontext;
            ErgebnisStromspeicherModel m = StromspeicherSimCtrl.AlsErgebnismodell(ergebnis, kontext);

            z.Gerechnet = true;
            z.Kapazitaet = (kontext != null ? kontext.Parameter.CNomKwh : 0.0).ToString("N1", k);
            z.Leistung = (kontext != null ? kontext.Parameter.PKw : 0.0).ToString("N1", k);
            z.Investition = m.Investition.ToString("N0", k);
            z.Ertrag = m.Ertrag_Aequivalent.ToString("N0", k);
            z.DeltaJ = m.Jahresueberschuss.ToString("N0", k);
            z.Kapitalwert = m.Kapitalwert.ToString("N0", k);
            z.Vollzyklen = m.Vollzyklen.ToString("N1", k);
            z.Amortisation = SpeicherAnzeigeCtrl.AmortisationText(
                                 ergebnis.Wirtschaftlichkeit.StatischeAmortisation);
            _deltaJ[z] = m.Jahresueberschuss;

            // Die Betriebsführung aus dem LAUF überschreibt die aus der Datenbank - sie
            // ist dieselbe, aber so hängt die Anzeige an dem, was gerechnet wurde.
            if (!string.IsNullOrEmpty(m.Betriebsart))
                z.Betriebsart = SpeicherAnzeigeCtrl.BetriebsartText(m.Betriebsart);
            if (!string.IsNullOrEmpty(m.Berechnungsart))
                z.Berechnungsart = SpeicherAnzeigeCtrl.BerechnungsartText(m.Berechnungsart);

            // Hinweise eines GELUNGENEN Laufs gehören ebenfalls ins Protokoll - sie
            // erklären Unterschiede zwischen zwei Zeilen.
            if (!string.IsNullOrEmpty(ctrlSp.LetzterHinweis))
                protokoll.Add(string.Format(MyResource.Resource.VAR_VGL_PROTOKOLL_ZEILE,
                                            z.Bezeichnung,
                                            ctrlSp.LetzterHinweis.Replace(Environment.NewLine, "  ")));

            return z;
        }

        /// <summary>ΔJ je Zeile — die Zahl hinter dem formatierten Text.</summary>
        private readonly Dictionary<Vergleichszeile, double> _deltaJ =
            new Dictionary<Vergleichszeile, double>();

        /// <summary>
        /// Index der besten Variante nach ΔJ, oder −1. Bei Gleichstand gewinnt die erste
        /// — die Reihenfolge ist die der Anlagenliste und damit stabil (wörtlich :487-503).
        /// </summary>
        private int BesteZeileNachDeltaJ(List<Vergleichszeile> zeilen)
        {
            int treffer = -1;
            double bestwert = 0.0;

            for (int i = 0; i < zeilen.Count; i++)
            {
                if (!zeilen[i].Gerechnet) continue;
                double wert;
                if (!_deltaJ.TryGetValue(zeilen[i], out wert)) continue;

                if (treffer < 0 || wert > bestwert) { treffer = i; bestwert = wert; }
            }

            return treffer;
        }

        /// <summary>
        /// Macht die Variante der Anlagenzeile zur aktiven Variante des Projekts.
        /// Geschrieben wird ausschließlich über <c>StromspeicherVarianteCtrl.SetzeAktiv</c>
        /// — die eine Schreibstelle, die „genau eine aktive Variante je Projekt" trägt.
        /// </summary>
        private EPOS.UI.Seiten.Simulation.Rueckmeldung VarianteAktivSetzen(int idAnlage)
        {
            StromspeicherVarianteCtrl ctrlV = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel variante = ctrlV.ReadByEnergieanlage(idAnlage);

            if (variante == null)
            {
                // Anlagenzeile ohne Variantenzeile (Datenbank vor Migrationsschritt 11b):
                // Sie entsteht hier mit den Vorgabewerten des Modells.
                variante = new StromspeicherVarianteModel { ID_Energieanlage = idAnlage };
                if (ctrlV.Insert(variante) <= 0)
                    return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                        false, MyResource.Resource.VAR_MSG_AKTIV_FEHLER);
            }

            if (!ctrlV.SetzeAktiv(m_ID_Projekt, variante.ID))
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, MyResource.Resource.VAR_MSG_AKTIV_FEHLER);

            // iU9-W16b.1 (E-7, K6-a): Hier stand ein Auffrischen der
            // Stromspeicherliste im Detailformular (Program.mainfrm.SetSPControl,
            // :652-669). FormMain ist gelöscht; die Startseite liest ihren Bestand
            // beim nächsten Zeichnen ohnehin neu.

            VarianteLesen();
            return EPOS.UI.Seiten.Simulation.Rueckmeldung.Still;
        }

        /// <summary>
        /// Die Vergleichstabelle als CSV — ein EIGENER Schreiber (Begründung :678-684:
        /// Semikolon, Dezimalkomma der Kultur, UTF-8 <b>mit</b> BOM), weil
        /// <c>CsvExportClass</c> Ganglinien schreibt und keine Tabelle.
        /// </summary>
        private async Task<EPOS.UI.Seiten.Simulation.Rueckmeldung> VergleichCsv()
        {
            string vorschlag = string.Format(MyResource.Resource.VAR_VGL_DATEI, m_ID_Projekt);
            string pfad = await Task.FromResult(
                WindowsFormsApplication1.Dienste.Datei.DateiSpeichern(MyResource.Resource.OPT_CSV_TITEL,
                                             "CSV (*.csv)|*.csv|Alle Dateien (*.*)|*.*",
                                             vorschlag));

            if (string.IsNullOrEmpty(pfad)) return EPOS.UI.Seiten.Simulation.Rueckmeldung.Still;

            try
            {
                File.WriteAllText(pfad, VergleichAlsText(), new UTF8Encoding(true));
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    true, string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, pfad));
            }
            catch (Exception ex)
            {
                return new EPOS.UI.Seiten.Simulation.Rueckmeldung(
                    false, string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message));
            }
        }

        private string VergleichAlsText()
        {
            var b = new StringBuilder();
            b.AppendLine(string.Join(";", new[]
            {
                MyResource.Resource.VAR_VGL_SP_AKTIV,
                MyResource.Resource.VAR_VGL_SP_BEZEICHNUNG,
                MyResource.Resource.VAR_VGL_SP_BETRIEBSART,
                MyResource.Resource.VAR_VGL_SP_BERECHNUNGSART,
                MyResource.Resource.VAR_VGL_SP_KAPAZITAET,
                MyResource.Resource.VAR_VGL_SP_LEISTUNG,
                MyResource.Resource.VAR_VGL_SP_INVESTITION,
                MyResource.Resource.VAR_VGL_SP_ERTRAG,
                MyResource.Resource.VAR_VGL_SP_DELTAJ,
                MyResource.Resource.VAR_VGL_SP_AMORTISATION,
                MyResource.Resource.VAR_VGL_SP_NPV,
                MyResource.Resource.VAR_VGL_SP_VOLLZYKLEN
            }));

            foreach (var paar in _deltaJ)
            {
                Vergleichszeile z = paar.Key;
                b.AppendLine(string.Join(";", new[]
                {
                    z.Aktiv ? MyResource.Resource.VAR_VGL_MARKER_AKTIV : "",
                    Feld(z.Bezeichnung), Feld(z.Betriebsart), Feld(z.Berechnungsart),
                    z.Kapazitaet, z.Leistung, z.Investition, z.Ertrag, z.DeltaJ,
                    Feld(z.Amortisation), z.Kapitalwert, z.Vollzyklen
                }));
            }

            return b.ToString();
        }

        /// <summary>Die Jahressumme einer Stundenganglinie.</summary>
        private static double Jahressumme(float[] werte)
        {
            double summe = 0.0;
            if (werte == null) return summe;
            foreach (float w in werte) summe += w;
            return summe;
        }

        /// <summary>Ersetzt Semikolon durch Komma und Umbrüche durch Leerzeichen (:771-775).</summary>
        private static string Feld(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace(';', ',').Replace("\r", " ").Replace("\n", " ");
        }
    }
}
