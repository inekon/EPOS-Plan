using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Parameterdialog „Wirtschaftlichkeits-Parameter" (Konzept Kap. 6, Punkt 4).
    /// Eine Zeile je STAMMprojekt in Tab_ProjektWirtschaftlichkeit — die Parameter
    /// gelten für die ganze Vergleichsgruppe (Stamm + Varianten), damit alle
    /// Projekte mit identischen Annahmen verglichen werden (Normanforderung
    /// Nachvollziehbarkeit, DIN EN 17463).
    ///
    /// KATEGORISIERT (Vorgabe 12.08.2026): die Parameter sind nach Zugehörigkeit
    /// gruppiert — „Allgemein" immer sichtbar; Erzeuger-Gruppen (Photovoltaik,
    /// BHKW/KWKG, BEHG + Emissionsbilanz für Brennstoff-Erzeuger) erscheinen NUR, wenn
    /// der Erzeugertyp in der Vergleichsgruppe tatsächlich vorkommt
    /// (WirtschaftlichkeitCtrl.ErzeugerDerGruppe). Werte ausgeblendeter Gruppen
    /// bleiben beim Speichern unverändert erhalten.
    ///
    /// Komplett im Code aufgebaut (kein Designer/.resx nötig) — Muster Form_Bericht.
    /// </summary>
    public class Form_WirtschaftlichkeitParameter : Form
    {
        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private readonly WirtschaftlichkeitParameter _parameter;
        private readonly WirtschaftlichkeitCtrl.ErzeugerFlags _erzeuger;

        // Steuerelemente — null, wenn die zugehörige Gruppe ausgeblendet ist.
        private NumericUpDown numZins, numJahre, numPreisE, numPreisB, numCO2;
        private NumericUpDown numEinspeisung;
        private NumericUpDown numKwkg, numKwkgEinsp, numVbhDeckel, numVbhKontingent, numAbschlagNeg;
        private DateTimePicker dtStichtag, dtInbetriebnahme;
        private ComboBox cbPark;
        // ETAPPE E4 — Steuerangaben (nur bei BHKW in der Vergleichsgruppe).
        private ComboBox cbUnternehmensart, cbEnergiesteuer, cbAufteilung;
        private CheckBox chkRaeumlich, chkHocheffizienz;
        private NumericUpDown numNutzungsgrad;
        private Button btnOk, btnAbbrechen;

        /// <summary>true, wenn gespeichert wurde (Aufrufer rechnet dann neu).</summary>
        public bool Gespeichert { get; private set; }

        public Form_WirtschaftlichkeitParameter(int idStamm)
        {
            _parameter = _ctrl.LadeParameter(idStamm);
            _erzeuger = _ctrl.ErzeugerDerGruppe(idStamm);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);   // vor der Hinweis-Textmessung setzen
            int y = 12;

            // ---------------- Allgemein (immer sichtbar) ----------------
            Gruppe("Allgemein", ref y);
            numZins = Zeile("Kalkulationszinssatz i [%]:", ref y, 0m, 15m, 2, (decimal)_parameter.Zinssatz, 0.1m);
            numJahre = Zeile("Betrachtungszeitraum T [a]:", ref y, 1m, 50m, 0, _parameter.Betrachtungszeitraum, 1m);
            numPreisE = Zeile("Preissteigerung Energie [%/a]:", ref y, -10m, 20m, 2, (decimal)_parameter.PreissteigerungEnergie, 0.1m);
            numPreisB = Zeile("Preissteigerung Betrieb [%/a]:", ref y, -10m, 20m, 2, (decimal)_parameter.PreissteigerungBetrieb, 0.1m);

            // ---------------- Photovoltaik ----------------
            if (_erzeuger.Photovoltaik)
            {
                Gruppe("Photovoltaik", ref y);
                numEinspeisung = Zeile("Einspeisevergütung PV [€/kWh]:", ref y, 0m, 2m, 4, (decimal)_parameter.Einspeiseverguetung, 0.001m);
            }

            // ---------------- BHKW — KWKG 2025 ----------------
            if (_erzeuger.Bhkw)
            {
                Gruppe("BHKW — KWKG 2025", ref y);
                numKwkg = Zeile("Bonus Eigenstrom [ct/kWh] (0 = aus):", ref y, 0m, 30m, 2, (decimal)_parameter.KwkgBonus, 0.1m);
                numKwkgEinsp = Zeile("Bonus Einspeisung [ct/kWh]:", ref y, 0m, 30m, 2, (decimal)_parameter.KwkgBonusEinspeisung, 0.1m);
                numVbhDeckel = Zeile("Vbh-Deckel-Override [h/a]:", ref y, 0m, 8760m, 0, (decimal)_parameter.KwkgVbhJahresdeckel, 100m);
                numVbhKontingent = Zeile("Vbh-Kontingent gesamt [h]:", ref y, 0m, 200000m, 0, (decimal)_parameter.KwkgVbhKontingent, 1000m);
                numAbschlagNeg = Zeile("Abschlag Negativstunden [%]:", ref y, 0m, 50m, 1, (decimal)_parameter.KwkgAbschlagNegativ, 0.5m);
                dtStichtag = DatumZeile("Stichtag (Bestellung/Genehmigung):", ref y, _parameter.KwkgStichtag);
                dtInbetriebnahme = DatumZeile("Geplante Inbetriebnahme:", ref y, _parameter.KwkgInbetriebnahme);

                // ---------------- BHKW — Energie- und Stromsteuer (Etappe E4) --------
                // Die gesetzlichen Bedingungen werden ERFASST statt angenommen. Jeder
                // Vorgabewert ist der, der KEINE Gutschrift auslöst.
                Gruppe("BHKW — Energie- und Stromsteuer", ref y);
                cbUnternehmensart = AuswahlZeile("Unternehmensart:", ref y, _parameter.Unternehmensart,
                    new[]
                    {
                        new Steuerwahl(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE, "kein produzierendes Gewerbe"),
                        new Steuerwahl(DbWerte.UNTERNEHMENSART_PROD_GEWERBE,      "produzierendes Gewerbe"),
                        new Steuerwahl(DbWerte.UNTERNEHMENSART_LAND_FORST,        "Land- und Forstwirtschaft")
                    });
                chkRaeumlich = SchalterZeile("Räumlicher Zusammenhang (4,5 km) gegeben",
                                             ref y, _parameter.RaeumlicherZusammenhang);
                chkHocheffizienz = SchalterZeile("Hocheffizienz nachgewiesen",
                                                 ref y, _parameter.HocheffizienzNachweis);
                numNutzungsgrad = Zeile("Jahresnutzungsgrad [%] (0 = nicht erfasst):", ref y,
                                        0m, 100m, 1, (decimal)(_parameter.Jahresnutzungsgrad ?? 0), 1m);
                cbEnergiesteuer = AuswahlZeile("Energiesteuerentlastung:", ref y, _parameter.EnergiesteuerWahl,
                    new[]
                    {
                        new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_KEINE, "keine"),
                        new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_53,    "§ 53 EnergieStG (Formular 1131)"),
                        new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_53A,   "§ 53a Abs. 5 EnergieStG (1135)")
                    });
                cbAufteilung = AuswahlZeile("Brennstoff auf Strom/Wärme:", ref y, _parameter.AufteilungMethode,
                    new[]
                    {
                        new Steuerwahl(DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF, "voller BHKW-Brennstoff (§ 53 Abs. 2)"),
                        new Steuerwahl(DbWerte.AUFTEILUNG_ENERGETISCH,       "energetisch (konservativ)")
                    });
            }

            // ---------------- Emissionsbilanz (Brennstoff-Erzeuger) ----------------
            if (_erzeuger.Brennstoff)
            {
                Gruppe("Brennstoff — BEHG und Emissionsbilanz (BHKW/Kessel)", ref y);
                numCO2 = Zeile("CO₂-Preis BEHG [€/t] (0 = aus):", ref y, 0m, 500m, 0, (decimal)_parameter.CO2Preis, 5m);

                var lblPark = new Label { Location = new Point(28, y + 3), Size = new Size(237, 20),
                                          Text = "Referenz-Kraftwerkspark:" };
                cbPark = new ComboBox
                {
                    Location = new Point(270, y),
                    Size = new Size(160, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cbPark.Items.Add(new ParkEintrag(0, "(keine Emissionsbilanz)"));
                foreach (Kraftwerkspark park in EmissionsBilanzRechner.LadeKatalog())
                    cbPark.Items.Add(new ParkEintrag(park.Id, park.Bezeichner));
                int idx = 0;
                for (int i = 0; i < cbPark.Items.Count; i++)
                    if (((ParkEintrag)cbPark.Items[i]).Id == _parameter.IdKraftwerkspark) idx = i;
                cbPark.SelectedIndex = idx;
                this.Controls.Add(lblPark);
                this.Controls.Add(cbPark);
                y += 32;

                // Referenzkessel (η + Brennstoff) kommt seit Phase 11 aus der DB
                // (größter Heizkessel des Stammprojekts) — hier nur noch Anzeige.
                ReferenzkesselInfo rk = _ctrl.LiesReferenzkessel(_parameter.IdStamm);
                string rkText = rk != null && rk.Gefunden
                    ? "Referenzkessel (aus Projekt): " + rk.Bezeichner +
                      " — η " + rk.WirkungsgradProzent.ToString("N0") + " %" +
                      (rk.BrennstoffName.Length > 0 ? ", " + rk.BrennstoffName
                                                    : ", Brennstoff aus Vorgabe")
                    : "Referenzkessel: kein Heizkessel im Stammprojekt gepflegt — Vorgabe η " +
                      _parameter.RefKesselWirkungsgrad.ToString("N0") + " % gilt.";
                var lblRefKessel = new Label
                {
                    Location = new Point(28, y + 3),
                    Size = new Size(402, 34),
                    ForeColor = Color.DimGray,
                    Text = rkText
                };
                this.Controls.Add(lblRefKessel);
                y += 42;
            }

            // ---------------- Hinweis + Schaltflächen ----------------
            string hinweis =
                "Die Parameter gelten für Stamm und alle Varianten der Vergleichsgruppe; " +
                "Erzeuger-Gruppen erscheinen nur, wenn der Erzeugertyp in der Gruppe " +
                "vorkommt (ausgeblendete Werte bleiben erhalten). Energie- und Strompreise " +
                "kommen aus der Kostenmaske.";
            if (_erzeuger.Bhkw)
                hinweis += " KWKG: Deckel-Override 0 = degressive Vbh-Staffel 2025 ab dem " +
                           "Inbetriebnahmejahr; förderfähig nur mit Stichtag bis 31.12.2026 " +
                           "+ Realisierung bis Ablauf des 4. Folgejahres." +
                           " Steuern: Ohne ausdrückliche Angabe entsteht KEINE Gutschrift — " +
                           "§ 53 und § 53a schließen einander aus, die Sätze und Grenzwerte " +
                           "kommen aus dem Katalog „Gesetzliche Parameter". Der Jahresnutzungsgrad " +
                           "wird nur für § 53a gebraucht (Schwelle 70 %).";
            var lblHinweis = new Label
            {
                Location = new Point(15, y + 4),
                ForeColor = Color.DimGray,
                Text = hinweis
            };
            // Höhe messen statt raten — die letzte Zeile darf nicht abgeschnitten
            // werden (Review Phase 10).
            lblHinweis.Size = new Size(415, TextRenderer.MeasureText(
                hinweis, this.Font, new Size(415, 0), TextFormatFlags.WordBreak).Height + 6);
            this.Controls.Add(lblHinweis);
            y += lblHinweis.Height + 12;

            btnOk = new Button
            {
                Location = new Point(214, y),
                Size = new Size(120, 28),
                Text = "Speichern"
            };
            btnOk.Click += new EventHandler(btnOk_Click);

            btnAbbrechen = new Button
            {
                Location = new Point(340, y),
                Size = new Size(90, 28),
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbrechen);

            // Höhe auf den Arbeitsbereich deckeln, damit AutoScroll wirklich greift
            // (bei voller Sichtbarkeit + kleiner Auflösung; Review Phase 10). Wird
            // gescrollt, braucht die Bildlaufleiste zusätzliche Breite.
            int inhaltHoehe = y + 45;
            int maxHoehe = Screen.PrimaryScreen.WorkingArea.Height - 90;
            int hoehe = Math.Min(inhaltHoehe, Math.Max(320, maxHoehe));
            this.ClientSize = new Size(hoehe < inhaltHoehe ? 465 : 445, hoehe);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = true;   // Schutz bei hoher DPI-Skalierung / kleiner Auflösung
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbrechen;
            this.Name = "Form_WirtschaftlichkeitParameter";
            this.Text = "Wirtschaftlichkeits-Parameter";
            this.ResumeLayout(false);
        }

        // ------------------------------------------------------------- Layout-Helfer

        private void Gruppe(string text, ref int y)
        {
            var lbl = new Label
            {
                Location = new Point(15, y + 4),
                Size = new Size(415, 18),
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(lbl);
            y += 26;
        }

        private NumericUpDown Zeile(string beschriftung, ref int y,
                                    decimal min, decimal max, int dez, decimal wert, decimal schritt)
        {
            var lbl = new Label { Location = new Point(28, y + 3), Size = new Size(237, 20), Text = beschriftung };
            var num = new NumericUpDown
            {
                Location = new Point(270, y),
                Size = new Size(160, 23),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = dez,
                Increment = schritt,
                TextAlign = HorizontalAlignment.Right
            };
            num.Value = wert < min ? min : (wert > max ? max : wert);
            this.Controls.Add(lbl);
            this.Controls.Add(num);
            y += 29;
            return num;
        }

        /// <summary>Datumszeile mit abwählbarem Wert (Checkbox aus = kein Datum).</summary>
        private DateTimePicker DatumZeile(string beschriftung, ref int y, DateTime? wert)
        {
            var lbl = new Label { Location = new Point(28, y + 3), Size = new Size(237, 20), Text = beschriftung };
            var dt = new DateTimePicker
            {
                Location = new Point(270, y),
                Size = new Size(160, 23),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = wert.HasValue
            };
            if (wert.HasValue && wert.Value >= dt.MinDate && wert.Value <= dt.MaxDate)
                dt.Value = wert.Value;
            else if (wert.HasValue)
                dt.Checked = false;   // unplausibles DB-Datum: nicht übernehmen, nicht abstürzen
            this.Controls.Add(lbl);
            this.Controls.Add(dt);
            y += 32;
            return dt;
        }

        /// <summary>
        /// Ein Eintrag der Steuerauswahl (Etappe E4): sprachneutraler Steuerwert für die
        /// Datenbank, deutscher Text für die Anzeige — die Drei-Schichten-Regel in einer
        /// Zeile. Der Dialog ist wie seine Nachbarn nicht lokalisiert; die Steuerwerte
        /// stehen in <see cref="DbWerte"/> und bleiben davon unberührt.
        /// </summary>
        private class Steuerwahl
        {
            public readonly string Wert;
            private readonly string _text;
            public Steuerwahl(string wert, string text) { Wert = wert; _text = text; }
            public override string ToString() { return _text; }
        }

        /// <summary>Auswahlzeile über feste Steuerwerte; unbekannte Bestandswerte fallen
        /// auf den ersten Eintrag zurück (= der Wert ohne Gutschrift).</summary>
        private ComboBox AuswahlZeile(string beschriftung, ref int y, string wert, Steuerwahl[] eintraege)
        {
            var lbl = new Label { Location = new Point(28, y + 3), Size = new Size(237, 20), Text = beschriftung };
            var cb = new ComboBox
            {
                Location = new Point(270, y),
                Size = new Size(160, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            int idx = 0;
            for (int i = 0; i < eintraege.Length; i++)
            {
                cb.Items.Add(eintraege[i]);
                if (string.Equals(eintraege[i].Wert, wert, StringComparison.Ordinal)) idx = i;
            }
            cb.SelectedIndex = idx;
            this.Controls.Add(lbl);
            this.Controls.Add(cb);
            y += 32;
            return cb;
        }

        /// <summary>Ja/Nein-Zeile für eine gesetzliche Bedingung.</summary>
        private CheckBox SchalterZeile(string beschriftung, ref int y, bool wert)
        {
            var chk = new CheckBox
            {
                Location = new Point(28, y + 2),
                Size = new Size(402, 22),
                Text = beschriftung,
                Checked = wert
            };
            this.Controls.Add(chk);
            y += 27;
            return chk;
        }

        private class ParkEintrag
        {
            public readonly int Id;
            private readonly string _name;
            public ParkEintrag(int id, string name) { Id = id; _name = name; }
            public override string ToString() { return _name; }
        }

        // ------------------------------------------------------------- Speichern

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Allgemein (immer vorhanden).
            _parameter.Zinssatz = (double)numZins.Value;
            _parameter.Betrachtungszeitraum = (int)numJahre.Value;
            _parameter.PreissteigerungEnergie = (double)numPreisE.Value;
            _parameter.PreissteigerungBetrieb = (double)numPreisB.Value;

            // Erzeuger-Gruppen: nur übernehmen, wenn die Gruppe sichtbar war —
            // ausgeblendete Werte bleiben unverändert (kein stilles Nullen).
            // Guard = dieselben Flags, die auch den Aufbau steuern (Review Phase 10).
            if (_erzeuger.Photovoltaik)
                _parameter.Einspeiseverguetung = (double)numEinspeisung.Value;

            if (_erzeuger.Bhkw)
            {
                _parameter.KwkgBonus = (double)numKwkg.Value;
                _parameter.KwkgBonusEinspeisung = (double)numKwkgEinsp.Value;
                _parameter.KwkgVbhJahresdeckel = (double)numVbhDeckel.Value;
                _parameter.KwkgVbhKontingent = (double)numVbhKontingent.Value;
                _parameter.KwkgAbschlagNegativ = (double)numAbschlagNeg.Value;
                _parameter.KwkgStichtag = dtStichtag.Checked ? (DateTime?)dtStichtag.Value.Date : null;
                _parameter.KwkgInbetriebnahme = dtInbetriebnahme.Checked ? (DateTime?)dtInbetriebnahme.Value.Date : null;

                // ETAPPE E4 — Steuerangaben. 0 % Jahresnutzungsgrad heißt „nicht
                // erfasst": Ein Nutzungsgrad von null ist fachlich kein Wert, und die
                // Begründung soll „nicht erfasst" von „erfasst und zu niedrig"
                // unterscheiden können.
                _parameter.Unternehmensart = Gewaehlt(cbUnternehmensart,
                                                      DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE);
                _parameter.RaeumlicherZusammenhang = chkRaeumlich.Checked;
                _parameter.HocheffizienzNachweis = chkHocheffizienz.Checked;
                _parameter.Jahresnutzungsgrad = numNutzungsgrad.Value > 0
                                              ? (double?)numNutzungsgrad.Value : null;
                _parameter.EnergiesteuerWahl = Gewaehlt(cbEnergiesteuer,
                                                        DbWerte.ENERGIESTEUER_WAHL_KEINE);
                _parameter.AufteilungMethode = Gewaehlt(cbAufteilung,
                                                        DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
            }

            if (_erzeuger.Brennstoff)
            {
                _parameter.CO2Preis = (double)numCO2.Value;
                ParkEintrag park = cbPark.SelectedItem as ParkEintrag;
                _parameter.IdKraftwerkspark = park != null ? park.Id : 0;
                // Referenzkessel (η + Brennstoff) wird nicht mehr hier gepflegt —
                // LadeParameter übernimmt ihn aus Tab_Heizkessel des Stammprojekts
                // (Phase 11); _parameter enthält bereits die DB-Werte.
            }

            if (!_ctrl.SpeichereParameter(_parameter))
            {
                MessageBox.Show("Die Parameter konnten nicht gespeichert werden.", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = true;
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
