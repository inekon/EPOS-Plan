using System;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// PV-Vergütungsdialog (PV-Konzept § 7, Etappe P5) — STAMMPROJEKTBEZOGEN
    /// (Muster <c>Form_Tarifstruktur</c>): sieben Gruppen Anlage · Vermarktung ·
    /// Anzulegender Wert · § 51/§ 51a · Bezugsbewertung · 60-%-Begrenzung ·
    /// Vorschau. Der Aufrufer prüft <see cref="Gespeichert"/> und stößt die
    /// Neuberechnung an.
    ///
    /// <para><b>Eine Vergütungswahrheit (V4/F7):</b> Was hier steht, führt —
    /// die Erlösbildung (<see cref="PvErloesRechner"/>) und die Speicherbewertung
    /// (<c>v_pv</c>) lesen dieselbe Tabelle. Inaktiv = exakt Bestandsverhalten.</para>
    ///
    /// <para><b>Designer-fähig statt programmatisch:</b> bewusste Abweichung vom
    /// § 7-Wortlaut der Rev. 1 — die jüngere Entscheidung FK1/Ä6 des
    /// Kostendialoge-Konzepts (25.08.2026) verlangt Designer-Dateien für alle
    /// neuen Dialoge; Texte kommen weiter aus MyResource (PVW_*).</para>
    ///
    /// <para><b>Vorschau ohne Zweitrechnung:</b> Der Block ruft denselben
    /// <see cref="PvErloesRechner"/>, der auch die Wirtschaftlichkeit speist —
    /// mit der Einspeisemenge des LETZTEN Simulationsergebnisses
    /// (<c>ErgebnisCtrl.Load</c>); ohne Ergebnis zeigt er nur die Sätze.</para>
    /// </summary>
    public partial class Form_PhotovoltaikVerguetung : Form
    {
        private readonly ProjektPhotovoltaikCtrl _ctrl = new ProjektPhotovoltaikCtrl();
        private int _idStamm;
        private ProjektPhotovoltaikModel _modell;
        private double _kwpRechnerisch;
        private double _einspeisungMWh;

        // ---- Kennzahlen-Grundlagen (P6, N.3) — alles vorhandene Wahrheiten ----
        private double _erzeugungMWh;                 // Simulationsergebnis
        private double _bedarfMWh;
        private double? _evQuoteSpeicher;             // Speicherrechnung, falls gelaufen
        private double? _autarkieSpeicher;
        private double? _investPv;                    // Kostenwelt, Komponente Photovoltaik
        private double? _betriebPv;
        private double? _strompreisEurKwh;            // Vorrangkette der Energiekosten
        private WirtschaftlichkeitParameter _wirtParameter;
        private bool _laden;

        /// <summary>true, wenn „Übernehmen" erfolgreich geschrieben hat.</summary>
        public bool Gespeichert { get; private set; }

        public Form_PhotovoltaikVerguetung()
        {
            InitializeComponent();
            TexteSetzen();
            cmbPar51.Items.AddRange(new object[]
                { T("PVW_AUTO", "Automatisch"), T("PVW_JA", "Ja"), T("PVW_NEIN", "Nein") });
            cmbKappung.Items.AddRange(new object[]
                { T("PVW_AUTO", "Automatisch"), T("PVW_JA", "Ja"), T("PVW_NEIN", "Nein") });
        }

        /// <summary>Stammprojekt setzen und Werte laden — vor <c>ShowDialog</c>.</summary>
        public void SetControls(int idStamm)
        {
            _idStamm = idStamm;
            _laden = true;
            try
            {
                _modell = _ctrl.LiesOderVorbelegt(idStamm);
                _kwpRechnerisch = PhotovoltaikCtrl.KwpDesProjekts(idStamm);

                _einspeisungMWh = 0;
                try
                {
                    ErgebnisModel erg = new ErgebnisCtrl().Load(idStamm);
                    if (erg != null && erg.Photovoltaik != null)
                    {
                        _einspeisungMWh = erg.Photovoltaik.Ueberschuss;
                        _erzeugungMWh = erg.Photovoltaik.Stromproduktion;
                        _bedarfMWh = erg.Photovoltaik.Strombedarf;
                    }
                    // Quoten MIT Speicher aus der Speicherrechnung (N.3: stets als
                    // Paar); bei mehreren Anlagen die erste Zeile mit Werten.
                    if (erg != null && erg.Stromspeicher != null)
                        foreach (ErgebnisStromspeicherModel sp in erg.Stromspeicher)
                            if (sp.Eigenverbrauchsquote > 0 || sp.Autarkiegrad > 0)
                            {
                                _evQuoteSpeicher = sp.Eigenverbrauchsquote;
                                _autarkieSpeicher = sp.Autarkiegrad;
                                break;
                            }
                }
                catch { }

                // PV-Kosten aus der Kostenwelt (dieselbe Leselogik wie Bericht und
                // Kostendialog); Betrieb: fehlende Zeile bleibt null (nicht 0).
                _investPv = null; _betriebPv = null;
                try
                {
                    _investPv = KomponentenSumme(idStamm, Form_Kosten.KATEGORIE_INVESTITION);
                    _betriebPv = KomponentenSumme(idStamm, Form_Kosten.KATEGORIE_BETRIEB);
                }
                catch { }
                try { _strompreisEurKwh = WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh(idStamm); }
                catch { }
                try { _wirtParameter = new WirtschaftlichkeitCtrl().LadeParameter(idStamm); }
                catch { }

                chkAktiv.Checked = _modell.Aktiv;
                numKwpOverride.Value = (decimal)Math.Min(100000, _modell.KwpOverride ?? 0);
                dtpIbn.Value = _modell.Inbetriebnahme > dtpIbn.MinDate
                    ? _modell.Inbetriebnahme : DateTime.Now;
                rbVoll.Checked = string.Equals(_modell.Einspeiseart,
                    DbWerte.PV_EINSPEISEART_VOLL, StringComparison.Ordinal);
                rbUeberschuss.Checked = !rbVoll.Checked;

                rbMarktpraemie.Checked = string.Equals(_modell.Vermarktungsform,
                    DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE, StringComparison.Ordinal);
                rbPpa.Checked = string.Equals(_modell.Vermarktungsform,
                    DbWerte.PV_VERMARKTUNG_SONSTIGE_DV, StringComparison.Ordinal);
                rbKeine.Checked = string.Equals(_modell.Vermarktungsform,
                    DbWerte.PV_VERMARKTUNG_KEINE, StringComparison.Ordinal);
                rbEv.Checked = !rbMarktpraemie.Checked && !rbPpa.Checked && !rbKeine.Checked;

                numDv.Value = (decimal)Math.Min(10, _modell.DvEntgelt ?? 0.40);
                numPpaPreis.Value = (decimal)Math.Min(100, _modell.PpaPreis ?? 0);
                numPpaAufschlag.Value = (decimal)Math.Max(-100, Math.Min(100, _modell.PpaSpotAufschlag ?? 0));
                numAwOverride.Value = (decimal)Math.Min(100, _modell.AwOverride ?? 0);
                cmbPar51.SelectedIndex = SchalterIndex(_modell.Par51_Anwenden);
                numIMSys.Value = Math.Min(2100, _modell.IMSys_Einbaujahr ?? 0);
                numAusfall.Value = (decimal)Math.Min(100, _modell.AusfallanteilProzent ?? 20.0);
                chk51a.Checked = _modell.Par51a_Kompensieren;
                chkBezugReihe.Checked = _modell.BezugAusPreisreihe;
                cmbKappung.SelectedIndex = SchalterIndex(_modell.Kappung60_Anwenden);
            }
            finally { _laden = false; }

            Aktualisieren();
        }

        // =====================================================================
        // Live-Logik
        // =====================================================================

        private void EingabeGeaendert(object sender, EventArgs e)
        {
            if (_laden) return;
            Aktualisieren();
        }

        /// <summary>Plausibilitäten, Enabled-Umschaltung und Vorschau — alles aus
        /// EINEM Rechenweg (EegSatzRechner/PvErloesRechner, keine Zweitrechnung).</summary>
        private void Aktualisieren()
        {
            ProjektPhotovoltaikModel m = AusMaske();
            double kwp = m.KwpOverride ?? _kwpRechnerisch;

            lblKwpWert.Text = string.Format(CultureInfo.CurrentCulture,
                T("PVW_KWP_WERT", "rechnerisch {0:N1} kWp"), _kwpRechnerisch)
                + (m.KwpOverride.HasValue
                    ? string.Format(CultureInfo.CurrentCulture,
                        T("PVW_KWP_OVERRIDE", " — Override {0:N1}"), m.KwpOverride.Value)
                    : "");

            // Warnungen der Gruppe Anlage (§ 7 Punkt 1).
            string warnung = "";
            if (kwp > 1000) warnung = T("PVW_WARN_AUSSCHREIBUNG",
                "über 1 MW: Ausschreibung — AW-Override nötig.");
            else if (kwp > 2000) warnung = T("PVW_WARN_STROMSTEUER",
                "über 2 MW: Stromsteuer auf Eigenverbrauch prüfen.");
            lblAnlageWarnung.Text = warnung;

            // Zulässigkeiten (N3/N4): feste EV nur bis 100 kW, unentgeltlich < 200 kW.
            GesetzKatalog katalog = new GesetzKatalog();
            double evGrenze = katalog.Wert(DbWerte.GESETZ_EEG_EV_GRENZE_KW, m.Inbetriebnahme.Year) ?? 100;
            double unentgeltGrenze = katalog.Wert(DbWerte.GESETZ_EEG_UNENTGELTLICH_GRENZE_KW,
                                                  m.Inbetriebnahme.Year) ?? 200;
            rbEv.Enabled = kwp <= evGrenze + 0.0001;
            rbKeine.Enabled = kwp < unentgeltGrenze;
            if (!rbEv.Enabled && rbEv.Checked) rbMarktpraemie.Checked = true;
            if (!rbKeine.Enabled && rbKeine.Checked) rbMarktpraemie.Checked = true;

            numDv.Enabled = rbMarktpraemie.Checked;
            numPpaPreis.Enabled = rbPpa.Checked;
            numPpaAufschlag.Enabled = rbPpa.Checked;
            lblVermarktungHinweis.Text = rbEv.Enabled
                ? T("PVW_HINWEIS_21C", "Ohne aktive Zuordnung beim Netzbetreiber gilt < 200 kW die unentgeltliche Abnahme (§ 21c).")
                : T("PVW_EV_GESPERRT", "Feste EV nur bis 100 kW (§ 21 Abs. 1 Nr. 1).");

            // Anzulegender Wert mit Herkunft.
            EegSatzErgebnis satz = EegSatzRechner.AnzulegenderWert(Math.Max(0.001, kwp),
                m.Inbetriebnahme, m.Einspeiseart, katalog.Wert, CultureInfo.CurrentCulture);
            double aw = m.AwOverride ?? satz.AwMixCt;
            lblAwWert.Text = string.Format(CultureInfo.CurrentCulture,
                "AW_mix: {0:0.00} ct/kWh{1}", aw,
                m.AwOverride.HasValue ? " " + T("PVW_AW_OVERRIDE", "(Override)") : "");
            lblAwHerkunft.Text = satz.Herleitung;
            double evAbschlag = katalog.Wert(DbWerte.GESETZ_EEG_EV_ABSCHLAG, m.Inbetriebnahme.Year) ?? 0.4;
            lblEv.Text = string.Format(CultureInfo.CurrentCulture,
                T("PVW_EV_SATZ", "Feste EV (AW − {0:0.00}): {1:0.00} ct/kWh"),
                evAbschlag, Math.Max(0, aw - evAbschlag));

            // § 51-Status.
            bool nachStichtag = m.Inbetriebnahme >= PvErloesRechner.Par51Stichtag;
            double grenze51 = katalog.Wert(DbWerte.GESETZ_EEG_51_GRENZE_KW, m.Inbetriebnahme.Year) ?? 100;
            string status;
            if (!nachStichtag) status = T("PVW_51_ALTANLAGE", "greift nicht: IBN vor 25.02.2025.");
            else if (kwp >= grenze51) status = T("PVW_51_GREIFT", "greift ab der ersten negativen Viertelstunde.");
            else if (m.IMSys_Einbaujahr.HasValue)
                status = string.Format(CultureInfo.CurrentCulture,
                    T("PVW_51_IMSYS", "greift ab {0} (Folgejahr des iMSys-Einbaus)."),
                    m.IMSys_Einbaujahr.Value + 1);
            else status = T("PVW_51_VERSCHONT", "greift nicht: Anlage < 100 kW ohne iMSys.");
            lblPar51Status.Text = status;

            // Kappungs-Status (AUTO-Bedingungen 3.5).
            bool kappungAuto = rbEv.Checked && !m.IMSys_Einbaujahr.HasValue;
            lblKappungStatus.Text = cmbKappung.SelectedIndex == 2
                ? T("PVW_KAP_AUS", "abgeschaltet.")
                : cmbKappung.SelectedIndex == 1 || kappungAuto
                    ? T("PVW_KAP_AN", "aktiv: Einspeisung auf 60 % der kWp begrenzt (ohne iMSys).")
                    : T("PVW_KAP_INAKTIV", "greift nicht (Direktvermarktung oder iMSys vorhanden).");

            lblStromsteuer.Text = T("PVW_STROMSTEUER",
                "Eigenverbrauch aus Anlagen ≤ 2 MW im räumlichen Zusammenhang ist stromsteuerfrei " +
                "(§ 9 StromStG); bei Lieferung an Dritte gelten andere Regeln.");

            // Vorschau — derselbe Rechenweg wie die Wirtschaftlichkeit.
            PvErloesErgebnis pe = PvErloesRechner.Rechne(m, _kwpRechnerisch, _einspeisungMWh,
                null, null, 20, katalog.Wert,
                jahr => _ctrl.Jahresmarktwert(jahr, m), CultureInfo.CurrentCulture);
            lblVorschau.Text = _einspeisungMWh > 0
                ? string.Format(CultureInfo.CurrentCulture,
                    T("PVW_VORSCHAU",
                      "Einspeisung {0:N1} MWh/a · Satz Jahr 1: {1:0.00} ct/kWh · Erlös Jahr 1: {2:N0} €/a · " +
                      "Vergütungsausfall {3:N0} kWh ({4:N0} €) · § 51a-Gutschrift {5:N0} € (Jahr {6})"),
                    _einspeisungMWh, pe.SatzJahr1Ct, pe.JeJahr != null && pe.JeJahr.Length > 1 ? pe.JeJahr[1] : 0,
                    pe.VerguetungsausfallKwh, pe.VerguetungsausfallEur,
                    pe.Kompensation51aEur, pe.LetztesVerguetungsjahr)
                : T("PVW_VORSCHAU_OHNE_ERGEBNIS",
                    "Noch kein Simulationsergebnis — die Vorschau zeigt erst nach einem Lauf Mengen und Erlöse; die Sätze oben gelten bereits.");

            // Kennzahlzeile (P6, N.3 Nr. 3) — aus denselben Wahrheiten, keine
            // Zweitrechnung: Vergütungsreihe pe, Kosten aus der Kostenwelt,
            // Mengen aus dem Lauf, Zins/T/Preissteigerung der Wirtschaftlichkeit.
            if (_erzeugungMWh > 0 && _wirtParameter != null)
            {
                PvKennzahlen kz = PvKennzahlenRechner.Rechne(
                    _erzeugungMWh, _einspeisungMWh, _bedarfMWh,
                    _evQuoteSpeicher, _autarkieSpeicher,
                    _investPv, _betriebPv,
                    _wirtParameter.Zinssatz, _wirtParameter.Betrachtungszeitraum,
                    _wirtParameter.PreissteigerungEnergie,
                    _strompreisEurKwh, pe.JeJahr, CultureInfo.CurrentCulture);
                lblKennzahlen.Text = PvKennzahlenRechner.Anzeige(kz, CultureInfo.CurrentCulture);
            }
            else lblKennzahlen.Text = "—";
        }

        // =====================================================================
        // Speichern
        // =====================================================================

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            ProjektPhotovoltaikModel m = AusMaske();
            if (m.Aktiv && m.Inbetriebnahme == DateTime.MinValue)
            {
                MessageBox.Show(T("PVW_IBN_PFLICHT", "Bitte das Inbetriebnahmedatum angeben."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_ctrl.Speichern(m))
            {
                Gespeichert = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        /// <summary>P6 (Konzept 6.3): netztransparenz-CSV in die Marktwert-Stammreihen.</summary>
        private void btnMarktwerte_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = T("PVW_IMPORT_FILTER", "CSV-Dateien (*.csv)|*.csv|Alle Dateien (*.*)|*.*"),
                Title = btnMarktwerte.Text
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string bericht;
                bool ok = _ctrl.ImportiereMarktwerteCsv(dlg.FileName, out bericht);
                MessageBox.Show(ok
                        ? T("PVW_IMPORT_OK", "Marktwerte übernommen: ") + bericht
                        : T("PVW_IMPORT_FEHLER", "Import nicht möglich: ") + bericht,
                    Text, MessageBoxButtons.OK,
                    ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                if (ok) Aktualisieren();
            }
        }

        /// <summary>Summe der PV-Komponente einer Kostenkategorie; null = keine Zeile.</summary>
        private static double? KomponentenSumme(int idProjekt, int kategorie)
        {
            System.Data.DataTable dt = Form_Kosten.LiesKomponentenSummen(idProjekt, kategorie);
            if (dt == null) return null;
            foreach (System.Data.DataRow r in dt.Rows)
            {
                if (!string.Equals(Convert.ToString(r["Komponente"]),
                                   DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal))
                    continue;
                return r["Summe"] == DBNull.Value ? (double?)null : Convert.ToDouble(r["Summe"]);
            }
            return null;
        }

        /// <summary>Maske → Modell (0 in den Override-Feldern heißt NULL).</summary>
        private ProjektPhotovoltaikModel AusMaske()
        {
            ProjektPhotovoltaikModel m = _modell ?? new ProjektPhotovoltaikModel();
            m.ID_Projekt = _idStamm;
            m.Aktiv = chkAktiv.Checked;
            m.Einspeiseart = rbVoll.Checked
                ? DbWerte.PV_EINSPEISEART_VOLL : DbWerte.PV_EINSPEISEART_UEBERSCHUSS;
            m.Vermarktungsform = rbMarktpraemie.Checked ? DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE
                : rbPpa.Checked ? DbWerte.PV_VERMARKTUNG_SONSTIGE_DV
                : rbKeine.Checked ? DbWerte.PV_VERMARKTUNG_KEINE
                : DbWerte.PV_VERMARKTUNG_EV;
            m.Inbetriebnahme = dtpIbn.Value.Date;
            m.KwpOverride = numKwpOverride.Value > 0 ? (double?)numKwpOverride.Value : null;
            m.AwOverride = numAwOverride.Value > 0 ? (double?)numAwOverride.Value : null;
            m.DvEntgelt = (double)numDv.Value;
            m.PpaPreis = numPpaPreis.Value > 0 ? (double?)numPpaPreis.Value : null;
            m.PpaSpotAufschlag = numPpaAufschlag.Value != 0 ? (double?)numPpaAufschlag.Value : null;
            m.Par51_Anwenden = SchalterWert(cmbPar51.SelectedIndex);
            m.IMSys_Einbaujahr = numIMSys.Value >= 2000 ? (int?)numIMSys.Value : null;
            m.AusfallanteilProzent = (double)numAusfall.Value;
            m.Par51a_Kompensieren = chk51a.Checked;
            m.Kappung60_Anwenden = SchalterWert(cmbKappung.SelectedIndex);
            m.BezugAusPreisreihe = chkBezugReihe.Checked;
            return m;
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static int SchalterIndex(string wert)
        {
            if (string.Equals(wert, DbWerte.PV_SCHALTER_JA, StringComparison.Ordinal)) return 1;
            if (string.Equals(wert, DbWerte.PV_SCHALTER_NEIN, StringComparison.Ordinal)) return 2;
            return 0;
        }

        private static string SchalterWert(int index)
        {
            return index == 1 ? DbWerte.PV_SCHALTER_JA
                 : index == 2 ? DbWerte.PV_SCHALTER_NEIN
                 : DbWerte.PV_SCHALTER_AUTO;
        }

        private void TexteSetzen()
        {
            Text = T("PVW_TITEL", "PV-Vergütung (EEG)");
            lblKopfTitel.Text = Text;
            chkAktiv.Text = T("PVW_AKTIV", "Vergütung anwenden");
            grpAnlage.Text = T("PVW_G_ANLAGE", "Anlage");
            lblKwp.Text = T("PVW_KWP", "Installierte Leistung:");
            lblKwpOverride.Text = T("PVW_KWP_OVR", "Override [kWp] (0 = keiner):");
            lblIbn.Text = T("PVW_IBN", "Inbetriebnahme:");
            rbUeberschuss.Text = T("PVW_UEBERSCHUSS", "Überschusseinspeisung");
            rbVoll.Text = T("PVW_VOLL", "Volleinspeisung");
            grpVermarktung.Text = T("PVW_G_VERMARKTUNG", "Vermarktung");
            rbEv.Text = T("PVW_EV", "Feste Einspeisevergütung");
            rbMarktpraemie.Text = T("PVW_MP", "Direktvermarktung mit Marktprämie");
            rbPpa.Text = T("PVW_PPA", "Sonstige Direktvermarktung / PPA");
            rbKeine.Text = T("PVW_KEINE", "Keine Vergütung (unentgeltlich)");
            lblDv.Text = T("PVW_DV", "DV-Entgelt [ct/kWh]:");
            lblPpaPreis.Text = T("PVW_PPA_PREIS", "PPA-Festpreis [ct/kWh] (0 = keiner):");
            lblPpaAufschlag.Text = T("PVW_PPA_AUFSCHLAG", "PPA-Aufschlag auf Spot [ct/kWh]:");
            grpAw.Text = T("PVW_G_AW", "Anzulegender Wert");
            lblAwOverride.Text = T("PVW_AW_OVR", "AW-Override [ct/kWh] (0 = Katalog):");
            grpPar51.Text = T("PVW_G_51", "Vergütungsausfall (§ 51 / § 51a)");
            lblPar51.Text = T("PVW_ANWENDEN", "Anwenden:");
            lblIMSys.Text = T("PVW_IMSYS", "iMSys-Einbaujahr (0 = keins):");
            lblAusfall.Text = T("PVW_AUSFALL", "Ausfallanteil der Einspeisearbeit [%]:");
            chk51a.Text = T("PVW_51A", "§ 51a-Kompensation (Laufzeitverlängerung)");
            grpBezug.Text = T("PVW_G_BEZUG", "Strompreis / Bezugsbewertung");
            chkBezugReihe.Text = T("PVW_BEZUG_REIHE",
                "Netzbezug stundenscharf aus Preiszeitreihe bewerten");
            grpKappung.Text = T("PVW_G_KAPPUNG", "60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2)");
            lblKappung.Text = T("PVW_ANWENDEN", "Anwenden:");
            lblVorschauTitel.Text = T("PVW_G_VORSCHAU", "Vorschau");
            btnUebernehmen.Text = T("PVW_UEBERNEHMEN", "Übernehmen");
            btnAbbrechen.Text = T("PVW_ABBRECHEN", "Abbrechen");
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
