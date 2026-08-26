using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class ucFuelSettings : UserControl
    {
        private int _projectId;
        private EnergyCarrier _carrier;
        private List<EnergyConversion> _conversions;

        // Aktuelle Live-Werte (immer auf Basiseinheit normiert)
        private double _baseHi;
        private double _baseHs;
        private double _baseGroundPrice;
        private double _basePowerPrice;
        private double _baseWorkPrice;
        private double _baseCO2;
        private double _baseSO2;
        private double _baseNOx;

        // Reine Speicher-Anker des originalen DB-Zustands für den hasChanged-Vergleich
        private double _dbWorkPrice;
        private double _dbGroundPrice;
        private double _dbPowerPrice;
        private double _dbHi;
        private double _dbHs;
        private double _dbCO2;
        private double _dbSO2;
        private double _dbNOx;

        private int id_conversion; // Speichert die ID der aktuell ausgewählten Umrechnung
        private bool _isUpdatingUi = false; // Verhindert Endlosschleifen bei automatischer UI-Anpassung

        /// <summary>
        /// Aufschlagsblock des Stromtarifs (AP4, Fachkonzept 4.2/4.3) — nur beim
        /// Strom-Carrier belegt, sonst <c>null</c>. Netzentgelt, Umlagen, Stromsteuer,
        /// Konzessionsabgabe und Vertrieb gibt es bei Gas oder Fernwärme nicht; der
        /// Block wird deshalb nicht ausgegraut, sondern gar nicht erst angelegt.
        /// </summary>
        private ucStromAufschlaege _aufschlaege;

        // =====================================================================
        // ETAPPE K3 - Umrechnungsblock (Konzept Kosten/Energietraeger § 4.3)
        // =====================================================================

        /// <summary>
        /// Bearbeitungsstand der Umrechnungsregeln dieses Brennstoffs. Der Block
        /// arbeitet bewusst auf einer SPEICHERKOPIE: Der Prüfer beantwortet damit die
        /// Frage „was wäre, wenn ich diese Regel abschalte?", ohne dass dafür etwas
        /// geschrieben oder erneut gelesen werden müsste.
        /// </summary>
        private List<UmrechnungsRegel> _regeln = new List<UmrechnungsRegel>();

        private DataGridView dgvRegeln;
        private Label lblEffektiv;
        private Label lblVerstoss;
        private Button btnRegelNeu;

        /// <summary>Sperre gegen Rückkopplung, während der Regelblock neu befüllt wird.</summary>
        private bool _regelblockWirdGefuellt;

        /// <summary>Platzbedarf des Blocks ab seiner Oberkante: 190 px Inhalt plus
        /// 6 px Abstand zum nachrückenden Bestand. Um wie viel das Control wächst,
        /// ergibt sich daraus je Sichtbarkeitszweig in <see cref="BaueUmrechnungsblock"/>.</summary>
        private const int HOEHE_UMRECHNUNGSBLOCK = 196;

        public ucFuelSettings(int projectId, EnergyCarrier carrier)
        {
            InitializeComponent();

            _projectId = projectId;
            _carrier = carrier;

            // Events abonnieren (Live-Berechnung & Werterfassung)
            numArbeitspreis.ValueChanged += numArbeitspreis_ValueChanged;
            numHeizwert.ValueChanged += numHeizwert_ValueChanged;
            numBrennwert.ValueChanged += numBrennwert_ValueChanged;

            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            numArbeitspreis.Maximum = 1000000;
            numLeistungspreis.Maximum = 1000000;
            numGrundpreis.Maximum = 1000000;

            numBrennwert.Maximum = 1000000;
            numHeizwert.Maximum = 1000000;

            // DYNAMISCHE UI-STEUERUNG AUS DER TABELLE 'pricing_model'

            // 1. Heizwert (has_hi) Sichtbarkeit
            numHeizwert.Visible = _carrier.HasHi;
            lbl_Heizwert.Visible = _carrier.HasHi;
            lbl_Unit_Heizwert.Visible = _carrier.HasHi;

            // 2. Brennwert (has_hs) Sichtbarkeit
            numBrennwert.Visible = _carrier.HasHs;
            lbl_Brennwert.Visible = _carrier.HasHs;
            lbl_Unit_Brennwert.Visible = _carrier.HasHs;

            // 3. Leistungspreis (has_powerprice) Sichtbarkeit
            numLeistungspreis.Visible = _carrier.HasPowerPrice;
            lbl_Leistungspreis.Visible = _carrier.HasPowerPrice;
            lbl_Unit_Leistungspreis.Visible = _carrier.HasPowerPrice;

            // 4. Sonderlogik für leitungsgebundene Energieträger (z.B. Strom, Fernwärme ohne Heizwert)
            if (!_carrier.HasHi)
            {
                lbl_Unit_Arbeitspreis.Text = "€ / kWh";
                groupBox_Formel.Visible = false; // Ohne Heizwert gibt es keine Formel
                cmbUnit.Enabled = true;         // Einheitenwechsel sperren
            }
            else
            {
                cmbUnit.Enabled = true;
                groupBox_Formel.Visible = true;
            }

            LoadData();
            BaueUmrechnungsblock();   // ETAPPE K3 - vor dem Aufschlagsblock, der an this.Height andockt
            BaueAufschlagsblock();
            BaueLeistungspreisZusatz();   // ETAPPE KD4 (FK6/FK6a) - nach LoadData, das die Einheit setzt

            // Ä9 (26.08.2026): Der Katalogkontext (Projekt 0) SCHREIBT jetzt —
            // „Speichern“ aktualisiert die Katalogzeile selbst (energy_carrier),
            // ohne Projekt-Settings und ohne Preishistorie. Die KD4-Sperre ist
            // damit Geschichte; Trägervarianten laufen über die Katalogleiste des
            // Dialogs (EnergietraegerKatalogCtrl.Variante).
            if (_projectId <= 0)
                new ToolTip().SetToolTip(btn_Save, TKd4("KDLG_ET_KATALOG_SPEICHERN",
                    "Schreibt die KATALOGwerte dieses Trägers (gilt überall, wo kein Projektwert gepflegt ist)."));
        }

        // =====================================================================
        // ETAPPE KD4 (Konzept Kostendialoge § 7.1, FK6/FK6a) — Leistungspreis:
        // Modus (Jahr/Monat), saisonale Reihen, Strom-Sonderfall.
        // =====================================================================

        private ComboBox cmbLeistungsModus;
        private Button btnSaisonSaetze;
        private Label lblLeistungsHinweis;

        /// <summary>
        /// Ergänzt die Leistungspreis-Zeile: Modus-Klappliste (schreibt
        /// <c>energy_carrier.price_power_modus</c> — der Modus ist KATALOGSACHE je
        /// Träger, auch im Projektkontext; dokumentierte Zwischenlösung) und den
        /// Zugang zu den Saisonreihen (FK6a). Beim Stromträger wird das Feld
        /// GESPERRT: Der Strom-Leistungspreis ist die Tarifstruktur
        /// (StromMatrix, Migrationsschritt 21) — keine zweite Wahrheit.
        /// Programmatisch nach dem Bestandsmuster der übrigen Zusatzblöcke.
        /// </summary>
        private void BaueLeistungspreisZusatz()
        {
            Control eltern = numLeistungspreis.Parent;
            if (eltern == null) return;

            bool istStrom = string.Equals(_carrier.PricingModel, "ELECTRICITY",
                                          StringComparison.OrdinalIgnoreCase);
            if (istStrom)
            {
                // Beim Strom ist der Leistungspreis TARIFWELT (StromMatrix,
                // Schritt 21): has_powerprice ist dort false, das Feld unsichtbar —
                // der Hinweis sagt dem Suchenden, wo die Wahrheit liegt.
                numLeistungspreis.Enabled = false;
                lblLeistungsHinweis = new Label
                {
                    AutoSize = true,
                    ForeColor = Color.FromArgb(90, 90, 90),
                    Location = new Point(lbl_Leistungspreis.Left, lbl_Leistungspreis.Top),
                    Text = TKd4("KDLG_LP_STROM_TARIF",
                        "Leistungspreis Strom: über die Tarifstruktur.")
                };
                eltern.Controls.Add(lblLeistungsHinweis);
                return;
            }

            if (!_carrier.HasPowerPrice) return;

            cmbLeistungsModus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Location = new Point(lbl_Unit_Leistungspreis.Right + 12,
                                     numLeistungspreis.Top - 1)
            };
            cmbLeistungsModus.Items.Add(TKd4("KDLG_LP_MODUS_JAHR", "Jahresleistungspreis"));
            cmbLeistungsModus.Items.Add(TKd4("KDLG_LP_MODUS_MONAT", "Monatsleistungspreis"));
            cmbLeistungsModus.SelectedIndex = string.Equals(LiesLeistungsModus(),
                DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal) ? 1 : 0;
            SetzeLeistungsEinheit();
            cmbLeistungsModus.SelectedIndexChanged += (s, e) =>
            {
                SchreibeLeistungsModus();
                SetzeLeistungsEinheit();
            };
            eltern.Controls.Add(cmbLeistungsModus);

            btnSaisonSaetze = new Button
            {
                Text = TKd4("KDLG_LP_SAISON", "Saisonale Sätze…"),
                Size = new Size(130, 26),
                Location = new Point(cmbLeistungsModus.Right + 8, numLeistungspreis.Top - 2)
            };
            btnSaisonSaetze.Click += (s, e) =>
            {
                using (Form_LeistungspreisReihe dlg = new Form_LeistungspreisReihe())
                {
                    dlg.SetControls(_projectId, _carrier.ID, _carrier.Name);
                    dlg.ShowDialog(FindForm());
                }
                ZeigeReihenStatus();
            };
            eltern.Controls.Add(btnSaisonSaetze);

            lblLeistungsHinweis = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(btnSaisonSaetze.Right + 10, numLeistungspreis.Top + 3)
            };
            eltern.Controls.Add(lblLeistungsHinweis);
            ZeigeReihenStatus();
        }

        /// <summary>Einheit des Leistungspreis-Felds je Modus — €/(kW·a) bzw.
        /// €/(kW·Monat); ersetzt die frühere €/&lt;Bezugseinheit&gt;-Beschriftung
        /// (der Leistungspreis bemisst sich nach kW, nicht nach der Brennstoffeinheit).</summary>
        private void SetzeLeistungsEinheit()
        {
            if (!_carrier.HasPowerPrice) return;
            bool monat = cmbLeistungsModus != null
                ? cmbLeistungsModus.SelectedIndex == 1
                : string.Equals(LiesLeistungsModus(),
                    DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);
            lbl_Unit_Leistungspreis.Text = monat ? "€/(kW·Monat)" : "€/(kW·a)";
        }

        private string LiesLeistungsModus()
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT price_power_modus FROM energy_carrier WHERE id = ?",
                    new OleDbParameter("@id", _carrier.ID));
                string s = (o == null || o == DBNull.Value) ? null : Convert.ToString(o);
                return string.Equals(s, DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal)
                    ? DbWerte.LEISTUNGSPREIS_MODUS_MONAT
                    : DbWerte.LEISTUNGSPREIS_MODUS_JAHR;
            }
            catch { return DbWerte.LEISTUNGSPREIS_MODUS_JAHR; }
        }

        private void SchreibeLeistungsModus()
        {
            try
            {
                DataRepository.ExecuteSQL(
                    "UPDATE energy_carrier SET price_power_modus = ? WHERE id = ?",
                    new OleDbParameter("@m", cmbLeistungsModus.SelectedIndex == 1
                        ? DbWerte.LEISTUNGSPREIS_MODUS_MONAT
                        : DbWerte.LEISTUNGSPREIS_MODUS_JAHR),
                    new OleDbParameter("@id", _carrier.ID));
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden(
                    "Der Leistungspreis-Modus konnte nicht gespeichert werden: " + ex.Message);
            }
        }

        /// <summary>Statuszeile: gilt eine Saisonreihe (Projekt- vor Stammreihe)?</summary>
        private void ZeigeReihenStatus()
        {
            if (lblLeistungsHinweis == null) return;
            try
            {
                PreisreiheModel r = new PreisreiheCtrl().ReadTraegerReihe(_projectId, _carrier.ID);
                lblLeistungsHinweis.Text = r == null
                    ? ""
                    : string.Format(CultureInfo.CurrentCulture,
                        TKd4("KDLG_LP_REIHE_STATUS", "Saisonreihe {0} ({1}) — gilt vor dem Satz."),
                        r.Jahr,
                        r.IstStamm ? TKd4("KDLG_LPR_EBENE_STAMM", "Stammreihe (Katalog)")
                                   : TKd4("KDLG_LPR_EBENE_PROJEKT", "Projektreihe"));
            }
            catch { lblLeistungsHinweis.Text = ""; }
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string TKd4(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        /// <summary>
        /// Hängt beim Strom-Carrier den Aufschlagsblock unter die Preishistorie
        /// (AP4, Fachkonzept 4.2/4.3) und wächst um dessen Höhe.
        /// </summary>
        /// <remarks>
        /// Programmatisch angehängt statt in den Designer eingebaut: <c>ucFuelSettings</c>
        /// gilt für JEDEN Energieträger, der Aufschlagsblock nur für Strom — und
        /// Designer-Dateien werden im Projekt nicht von Hand editiert (CLAUDE.md). Der
        /// Bestandsdialog bleibt dadurch unverändert, für alle anderen Träger auch in
        /// seiner Höhe.
        /// </remarks>
        private void BaueAufschlagsblock()
        {
            if (_carrier == null) return;
            if (!string.Equals(_carrier.PricingModel, StromAufschlagCtrl.PRICING_MODEL_STROM,
                               StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                _aufschlaege = new ucStromAufschlaege(_projectId, _carrier.ID);
                _aufschlaege.Location = new Point(17, this.Height + 8);
                this.Height += ucStromAufschlaege.HOEHE + 16;
                this.Controls.Add(_aufschlaege);
            }
            catch (Exception ex)
            {
                // Ein fehlender Aufschlagsblock darf die Preispflege nicht blockieren -
                // etwa auf einer Datenbank, deren Migrationsschritt 12 nicht durchlief.
                Console.WriteLine("Der Aufschlagsblock konnte nicht aufgebaut werden: " + ex.Message);
                _aufschlaege = null;
            }
        }

        // =====================================================================
        // ETAPPE K3 - Umrechnungsblock (Konzept § 4.3)
        // =====================================================================

        /// <summary>
        /// Baut den Umrechnungsblock unter die Formelgruppe: Regelliste, Knopf
        /// „Regel hinzufügen", Effektivanzeige und Verstoßhinweis.
        /// </summary>
        /// <remarks>
        /// <para><b>Programmatisch, Designer unberührt</b> — dieselbe Hausregel und
        /// dieselbe Bauform wie <see cref="BaueAufschlagsblock"/>. Alle Bestandssteuer-
        /// elemente unterhalb der Einbaustelle (Emissionsfaktoren, Speichern-Zeile samt
        /// Gültig-ab-Datum, Preishistorie) wandern unter den Block, und das Control
        /// wächst mit; der Aufschlagsblock dockt danach an die NEUE Höhe an, weshalb er
        /// nach diesem Aufruf gebaut wird.</para>
        ///
        /// <para><b>Ein Fehler hier darf die Preispflege nicht blockieren</b> — etwa auf
        /// einer Datenbank vor Migrationsschritt 25, die die Spalten
        /// <c>faktor_name</c>/<c>aktiv</c> nicht führt. Dann bleibt der Block leer und
        /// der Hinweis nennt die ausstehende Migration.</para>
        /// </remarks>
        private void BaueUmrechnungsblock()
        {
            try
            {
                int oben = groupBox_Formel.Bottom + 8;
                if (!groupBox_Formel.Visible) oben = panel1.Bottom + 8;

                // ALLES unterhalb der Einbaustelle rückt unter den Block - neben
                // Speichern-Zeile und Preishistorie auch die Emissionsfaktoren und das
                // Gültig-ab-Datum, die im Designer-Raster dazwischen liegen. Der Versatz
                // hängt an der Oberkante des obersten Bestandscontrols statt an einer
                // festen Zahl: beim Strom-Träger sitzt die Einbaustelle 77 px höher
                // (Formelgruppe unsichtbar), eine starre Verschiebung ließe dort eine
                // ebenso große Lücke vor der Preishistorie.
                Control[] bestand = { label12, label11, numSO2, label3, numCO2, label10,
                                      numNOx, btn_Save, dtpValidFrom, label9, dgvHistory };
                int versatz = oben + HOEHE_UMRECHNUNGSBLOCK - bestand.Min(c => c.Top);
                foreach (Control c in bestand)
                    c.Top += versatz;
                this.Height += versatz;

                var titel = new Label
                {
                    AutoSize = true,
                    Location = new Point(17, oben),
                    Text = MyResource.Resource.KOSTEN_UMRECHNUNG_TITEL
                };
                titel.Font = new Font(titel.Font, FontStyle.Bold);
                this.Controls.Add(titel);

                dgvRegeln = new DataGridView
                {
                    Location = new Point(17, oben + 20),
                    Size = new Size(531, 104),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.CellSelect,
                    AutoGenerateColumns = false,
                    EditMode = DataGridViewEditMode.EditOnEnter
                };
                BaueRegelSpalten();
                dgvRegeln.CellValueChanged += DgvRegeln_CellValueChanged;
                dgvRegeln.CurrentCellDirtyStateChanged += DgvRegeln_CurrentCellDirtyStateChanged;
                this.Controls.Add(dgvRegeln);

                btnRegelNeu = new Button
                {
                    Location = new Point(17, oben + 130),
                    Size = new Size(150, 25),
                    Text = MyResource.Resource.KOSTEN_UMRECHNUNG_NEU
                };
                btnRegelNeu.Click += BtnRegelNeu_Click;
                this.Controls.Add(btnRegelNeu);

                lblEffektiv = new Label
                {
                    AutoSize = true,
                    Location = new Point(177, oben + 134),
                    Text = ""
                };
                this.Controls.Add(lblEffektiv);

                // Roter Text STATT MessageBox (Konzept § 4.3): Der Hinweis steht neben
                // der Ursache und blockiert die Bedienung nicht.
                lblVerstoss = new Label
                {
                    AutoSize = false,
                    Location = new Point(17, oben + 158),
                    Size = new Size(531, 32),
                    ForeColor = Color.Firebrick,
                    Text = ""
                };
                this.Controls.Add(lblVerstoss);

                LadeRegeln();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Umrechnungsblock konnte nicht aufgebaut werden: " + ex.Message);
                dgvRegeln = null;
            }
        }

        private void BaueRegelSpalten()
        {
            dgvRegeln.Columns.Clear();

            dgvRegeln.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NAME,
                Width = 150
            });
            dgvRegeln.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Von",
                HeaderText = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_VON,
                Width = 90
            });
            dgvRegeln.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nach",
                HeaderText = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NACH,
                Width = 90
            });
            dgvRegeln.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Faktor",
                HeaderText = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_FAKTOR,
                Width = 100
            });
            dgvRegeln.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Aktiv",
                HeaderText = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_AKTIV,
                Width = 60
            });
        }

        /// <summary>Liest die Regeln des Brennstoffs und zeigt sie an.</summary>
        private void LadeRegeln()
        {
            _regeln = EnergieEinheitenPruefung.RegelnDesBrennstoffs(_carrier.ID_Brennstoff);
            ZeigeRegeln();
        }

        private void ZeigeRegeln()
        {
            if (dgvRegeln == null) return;

            _regelblockWirdGefuellt = true;
            try
            {
                dgvRegeln.Rows.Clear();
                foreach (UmrechnungsRegel r in _regeln)
                    dgvRegeln.Rows.Add(r.Name, r.Von, r.Nach,
                                       r.Faktor.ToString("0.######", CultureInfo.CurrentCulture),
                                       r.Aktiv);
            }
            finally { _regelblockWirdGefuellt = false; }

            AktualisiereEffektivUndVerstoss();
        }

        /// <summary>
        /// Ein Häkchen soll sofort wirken, nicht erst beim Zellwechsel — sonst liefe der
        /// Riegel erst, wenn der Anwender die Zeile längst verlassen hat.
        /// </summary>
        private void DgvRegeln_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_regelblockWirdGefuellt || dgvRegeln == null) return;
            if (dgvRegeln.IsCurrentCellDirty && dgvRegeln.CurrentCell is DataGridViewCheckBoxCell)
                dgvRegeln.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvRegeln_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_regelblockWirdGefuellt || dgvRegeln == null) return;
            if (e.RowIndex < 0 || e.RowIndex >= _regeln.Count) return;

            UmrechnungsRegel r = _regeln[e.RowIndex];
            DataGridViewRow zeile = dgvRegeln.Rows[e.RowIndex];
            string spalte = dgvRegeln.Columns[e.ColumnIndex].Name;

            switch (spalte)
            {
                case "Name":
                    r.Name = Convert.ToString(zeile.Cells["Name"].Value ?? "").Trim();
                    break;

                case "Von":
                    r.Von = Convert.ToString(zeile.Cells["Von"].Value ?? "").Trim();
                    break;

                case "Nach":
                    r.Nach = Convert.ToString(zeile.Cells["Nach"].Value ?? "").Trim();
                    break;

                case "Faktor":
                    {
                        // Zahlprüfung im Haus-Muster (Program.ZahlParsen: Komma ODER
                        // Punkt). Eine unbrauchbare Eingabe wird NICHT übernommen und
                        // die Zelle springt auf den letzten gültigen Wert zurück - der
                        // rote Hinweis sagt, warum.
                        string text = Convert.ToString(zeile.Cells["Faktor"].Value ?? "").Trim();
                        double wert;
                        if (Program.ZahlParsen(text, out wert) && wert > 0)
                        {
                            r.Faktor = wert;
                            lblVerstoss.Text = "";
                        }
                        else
                        {
                            lblVerstoss.Text = MyResource.Resource.KOSTEN_UMRECHNUNG_FAKTOR_UNGUELTIG;
                            _regelblockWirdGefuellt = true;
                            zeile.Cells["Faktor"].Value =
                                r.Faktor.ToString("0.######", CultureInfo.CurrentCulture);
                            _regelblockWirdGefuellt = false;
                            return;
                        }
                        break;
                    }

                case "Aktiv":
                    {
                        bool neu = Convert.ToBoolean(zeile.Cells["Aktiv"].Value ?? false);

                        // DER RIEGEL (Konzept § 4.3): Die Regel, die den Träger nach kWh
                        // trägt, lässt sich nicht abschalten. Gefragt wird der Prüfer -
                        // damit gibt es keine zweite Fassung der Fachregel.
                        if (!neu)
                        {
                            string grund;
                            if (!EnergieEinheitenPruefung.DarfAbschalten(
                                    AktuelleEinheit(), (double)numHeizwert.Value,
                                    (double)numBrennwert.Value, _regeln, e.RowIndex, out grund))
                            {
                                lblVerstoss.Text = string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.KOSTEN_UMRECHNUNG_RIEGEL, grund);
                                _regelblockWirdGefuellt = true;
                                zeile.Cells["Aktiv"].Value = true;
                                _regelblockWirdGefuellt = false;
                                return;
                            }
                        }
                        r.Aktiv = neu;
                        break;
                    }
            }

            // Jede Handänderung macht die Zeile zu einer gepflegten - ab dann fasst sie
            // keine Migration mehr an (L5).
            r.UserEdited = true;
            AktualisiereEffektivUndVerstoss();
        }

        private void BtnRegelNeu_Click(object sender, EventArgs e)
        {
            bool gas = string.Equals(_carrier.PricingModel, "GASEOUS_FUEL",
                                     StringComparison.OrdinalIgnoreCase);

            _regeln.Add(new UmrechnungsRegel
            {
                Id = 0,                       // 0 = neu, die ID vergibt das Speichern
                IdBrennstoff = _carrier.ID_Brennstoff,
                Name = gas ? DbWerte.UMRECHNUNG_NAME_Z_FAKTOR : DbWerte.UMRECHNUNG_NAME_STANDARD,
                Von = AktuelleEinheit(),
                Nach = "",
                Faktor = 1,
                Aktiv = true,
                UserEdited = true
            });
            ZeigeRegeln();
        }

        /// <summary>Die Einheit, in der das Projekt gerade rechnet.</summary>
        private string AktuelleEinheit()
        {
            if (cmbUnit.SelectedItem is EnergyConversion conv &&
                !string.IsNullOrEmpty(conv.ToUnitCode))
                return conv.ToUnitCode;
            return _carrier.BillingUnit ?? "";
        }

        /// <summary>
        /// Die Live-Anzeige „effektiv: 1 ⟨Einheit⟩ = X kWh (Hi) / Y kWh (Hs)" und der
        /// rote Verstoßhinweis — beide aus demselben Bearbeitungsstand.
        /// </summary>
        private void AktualisiereEffektivUndVerstoss()
        {
            if (lblEffektiv == null || lblVerstoss == null) return;

            string einheit = AktuelleEinheit();
            double hi = (double)numHeizwert.Value;
            double hs = (double)numBrennwert.Value;

            if (string.Equals(einheit, DbWerte.EINHEIT_KWH, StringComparison.OrdinalIgnoreCase))
                lblEffektiv.Text = MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV_KWH;
            else
                lblEffektiv.Text = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV,
                    einheit, hi.ToString("N2", CultureInfo.CurrentCulture),
                    hs.ToString("N2", CultureInfo.CurrentCulture));

            string grund;
            lblVerstoss.Text = EnergieEinheitenPruefung.ErreichtKwh(einheit, hi, hs, _regeln, out grund)
                             ? "" : grund;
        }

        /// <summary>
        /// Schreibt den Bearbeitungsstand des Regelblocks nach <c>energy_conversion</c>.
        ///
        /// <para>Geschrieben wird ausschließlich, was der Anwender angefasst hat
        /// (<c>UserEdited</c>) — der Block ist eine Pflegemaske, kein Massenschreiber.
        /// Neue Zeilen (<c>Id = 0</c>) bekommen ihre ID als <c>MAX(ID)+1</c>, wie überall
        /// in diesem Schema; eine Regel ohne Zieleinheit wird übersprungen statt
        /// halbfertig gespeichert.</para>
        /// </summary>
        private void SpeichereRegeln()
        {
            if (dgvRegeln == null || _regeln == null) return;

            foreach (UmrechnungsRegel r in _regeln)
            {
                if (!r.UserEdited) continue;
                if (string.IsNullOrEmpty(r.Von) || string.IsNullOrEmpty(r.Nach)) continue;
                if (r.Faktor <= 0) continue;

                if (r.Id > 0)
                {
                    DataRepository.ExecuteSQL(
                        "UPDATE [energy_conversion] SET [from_unit] = ?, [to_unit] = ?, " +
                        "[factor] = ?, [user_edited] = TRUE, [" +
                        SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "] = ?, [" +
                        SchemaKatalog.SPALTE_EC_AKTIV + "] = ? WHERE [ID] = ?",
                        new OleDbParameter[]
                        {
                            new OleDbParameter("@von", r.Von),
                            new OleDbParameter("@nach", r.Nach),
                            new OleDbParameter("@f", r.Faktor),
                            new OleDbParameter("@n", r.Name ?? ""),
                            new OleDbParameter("@a", r.Aktiv),
                            new OleDbParameter("@id", r.Id)
                        });
                }
                else
                {
                    object max = DataRepository.ExecuteScalar(
                        "SELECT MAX([ID]) FROM [energy_conversion]");
                    int neueId = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;

                    DataRepository.ExecuteSQL(
                        "INSERT INTO [energy_conversion] ([ID], [id_brennstoff], [from_unit], " +
                        "[to_unit], [factor], [user_edited], [" +
                        SchemaKatalog.SPALTE_EC_FAKTOR_NAME + "], [" +
                        SchemaKatalog.SPALTE_EC_AKTIV + "]) VALUES (?, ?, ?, ?, ?, TRUE, ?, ?)",
                        new OleDbParameter[]
                        {
                            new OleDbParameter("@id", neueId),
                            new OleDbParameter("@b", r.IdBrennstoff),
                            new OleDbParameter("@von", r.Von),
                            new OleDbParameter("@nach", r.Nach),
                            new OleDbParameter("@f", r.Faktor),
                            new OleDbParameter("@n", r.Name ?? ""),
                            new OleDbParameter("@a", r.Aktiv)
                        });
                    r.Id = neueId;
                }
            }
        }

        private void LoadData()
        {
            lblCarrierName.Text = $"{_carrier.Name}  (VDI 3805 {_carrier.Code})";
            lblGruppe.Text = $"Gruppe: {_carrier.GroupCode}";
            // Das neue Datum-Feld standardmäßig auf JETZT setzen
            dtpValidFrom.Value = DateTime.Now;

            _conversions = GetConversions(_carrier.ID_Brennstoff);

            cmbUnit.SelectedIndexChanged -= CmbUnit_SelectedIndexChanged;
            cmbUnit.DataSource = _conversions;
            cmbUnit.DisplayMember = "ToUnitCode";
            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;

            var baseUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == _carrier.BillingUnit);
            if (baseUnit != null) lblBasisnheit.Text = $"{_carrier.BillingUnit}";

            var projectSettings = GetProjectPrice(_projectId, _carrier.ID);

            _isUpdatingUi = true; // Zwingt die NumericUpDowns beim ersten Laden, keine Events zu feuern

            if (projectSettings != null)
            {
                numArbeitspreis.Value = (decimal)projectSettings.ArbeitspreisEurYear;
                numGrundpreis.Value = (decimal)projectSettings.GrundpreisEurYear;
                numLeistungspreis.Value = (decimal)projectSettings.LeistungspreisEurYear;

                numHeizwert.Value = (decimal)(projectSettings.CustomHi ?? _carrier.HiKwhPerUnit);
                numBrennwert.Value = (decimal)(projectSettings.CustomHs ?? _carrier.HsKwhPerUnit);

                numCO2.Value = (decimal)(projectSettings.CO2 ?? _carrier.CO2);
                numSO2.Value = (decimal)(projectSettings.SO2 ?? _carrier.SO2);
                numNOx.Value = (decimal)(projectSettings.NOx ?? _carrier.NOx);

                _baseHi = projectSettings.CustomHi ?? _carrier.HiKwhPerUnit;
                _baseHs = projectSettings.CustomHs ?? _carrier.HsKwhPerUnit;
                _baseGroundPrice = (double)numGrundpreis.Value;
                _baseWorkPrice = (double)numArbeitspreis.Value;
                _basePowerPrice = (double)numLeistungspreis.Value;
                _baseCO2 = (double)numCO2.Value;
                _baseSO2 = (double)numSO2.Value;
                _baseNOx = (double)numNOx.Value;

                string project_conversion = GetTargetUnitByConversionId(projectSettings.IDUmrechnung);
                var selectedUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == project_conversion);

                if (selectedUnit != null)
                {
                    cmbUnit.SelectedItem = selectedUnit;
                    id_conversion = projectSettings.IDUmrechnung ?? -1;
                }
            }
            else
            {
                numArbeitspreis.Value = (decimal)_carrier.price_work;
                numGrundpreis.Value = (decimal)_carrier.price_base;
                numLeistungspreis.Value = (decimal)_carrier.price_power;
                numCO2.Value = (decimal)_carrier.CO2;
                numSO2.Value = (decimal)_carrier.SO2;
                numNOx.Value = (decimal)_carrier.NOx;
                numBrennwert.Value = (decimal)_carrier.HsKwhPerUnit;
                numHeizwert.Value = (decimal)_carrier.HiKwhPerUnit;

                _baseWorkPrice = _carrier.price_work;
                _baseGroundPrice = _carrier.price_base;
                _basePowerPrice = _carrier.price_power;
                _baseHi = _carrier.HiKwhPerUnit;
                _baseHs = _carrier.HsKwhPerUnit;
                _baseCO2 = _carrier.CO2;
                _baseSO2 = _carrier.SO2;
                _baseNOx = _carrier.NOx;

                cmbUnit.SelectedItem = baseUnit;
            }

            // Sichern des unveränderten DB-Zustands für den späteren Historien-Vergleich
            _dbWorkPrice = _baseWorkPrice;
            _dbGroundPrice = _baseGroundPrice;
            _dbPowerPrice = _basePowerPrice;
            _dbHi = _baseHi;
            _dbHs = _baseHs;
            _dbCO2 = _baseCO2;
            _dbSO2 = _baseSO2;
            _dbNOx = _baseNOx;

            _isUpdatingUi = false; // UI-Befüllung beendet, Handler scharf schalten

            // Labels initialisieren (falls ein Heizwert existiert, sonst bleibt es bei €/kWh)
            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                lbl_Unit_Arbeitspreis.Text = $"€/{conv.ToUnitCode}";
                SetzeLeistungsEinheit();   // KD4: kW-Bezug, nicht Brennstoffeinheit
                lbl_Unit_Heizwert.Text = $"kWh/{conv.ToUnitCode}";
                lbl_Unit_Brennwert.Text = $"kWh/{conv.ToUnitCode}";
            }
            UpdatePricePerKWh();
            SetupHistoryGrid();
            LoadHistory(_carrier.ID, _projectId);
        }

        private void numArbeitspreis_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi) return;

            if (!_carrier.HasHi)
            {
                // Strom/Fernwärme hat keine Umrechnung über Faktoren, Basis ist direkt der eingegebene Wert
                _baseWorkPrice = (double)numArbeitspreis.Value;
            }
            else if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseWorkPrice = (double)numArbeitspreis.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void numHeizwert_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi || !_carrier.HasHi) return;

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseHi = (double)numHeizwert.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void numBrennwert_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUi || !_carrier.HasHs) return;

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _baseHs = (double)numBrennwert.Value * conv.Factor;
                UpdatePricePerKWh();
            }
        }

        private void CmbUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
         //   if (!_carrier.HasHi) return; // Wenn kein Heizwert existiert (Strom), ignorieren wir den Einheitenwechsel

            if (cmbUnit.SelectedItem is EnergyConversion conv)
            {
                _isUpdatingUi = true;

                numArbeitspreis.Value = (decimal)(_baseWorkPrice / conv.Factor);
                numHeizwert.Value = (decimal)(_baseHi / conv.Factor);

                if (_carrier.HasPowerPrice) numLeistungspreis.Value = (decimal)(_basePowerPrice / conv.Factor);
                if (_carrier.HasHs) numBrennwert.Value = (decimal)(_baseHs / conv.Factor);

                id_conversion = GetConvID(cmbUnit.SelectedItem);

                lbl_Unit_Arbeitspreis.Text = $"€/{conv.ToUnitCode}";
                lbl_Unit_Heizwert.Text = $"kWh/{conv.ToUnitCode}";

                if (_carrier.HasPowerPrice) SetzeLeistungsEinheit();   // KD4: kW-Bezug
                if (_carrier.HasHs) lbl_Unit_Brennwert.Text = $"kWh/{conv.ToUnitCode}";

                UpdatePricePerKWh();
                AktualisiereEffektivUndVerstoss();   // ETAPPE K3: neue Einheit, neue Effektivzeile

                _isUpdatingUi = false;
            }
        }

        private void UpdatePricePerKWh()
        {
            if (!_carrier.HasHi)
            {
                // Bei Strom/Fernwärme entspricht der Arbeitspreis bereits dem Preis/kWh
                lblResult.Text = $"{numArbeitspreis.Value:N4} €";
                lblFormula.Text = "Direktabrechnung nach kWh";
                return;
            }

            decimal price = numArbeitspreis.Value;
            decimal hi = numHeizwert.Value;

            if (hi > 0)
            {
                decimal result = price / hi;
                lblResult.Text = $"{result:N4} €";
                lblFormula.Text = $"{price:N2} € ÷ {hi:N2} kWh = {result:N4} €/kWh";
            }

            // ETAPPE K3: Heizwert und Brennwert gehen in die Effektivzeile und in die
            // kWh-Bedingung ein - beide werden mit jedem Wert neu gebildet.
            AktualisiereEffektivUndVerstoss();
        }

        /// <summary>Träger-ID dieses Controls (für den Zuordnungs-Check beim Schließen, Phase 7).</summary>
        public int CarrierId
        {
            get { return _carrier != null ? _carrier.ID : 0; }
        }

        /// <summary>
        /// ETAPPE K3 (Konzept § 4.3): die BLOCKIERENDE Prüfung beim Speichern.
        ///
        /// <para><c>false</c> heißt: Der Träger erfüllt die kWh-Bedingung aus L2 nicht,
        /// und <paramref name="grund"/> sagt im Klartext, woran es liegt. Anders als die
        /// Protokollwarnung im Wirtschaftlichkeitslauf (Etappe K2) ist die Prüfung hier
        /// blockierend — das ist die Stelle, an der die Daten ENTSTEHEN, und ein Mangel
        /// lässt sich genau hier beheben statt später in jeder Rechnung zu melden.</para>
        ///
        /// <para>Der Hinweis „Heizwert fehlt" blockiert NICHT: Er ist kein L2-Verstoß
        /// (der Träger erreicht kWh über die Regelkette), sondern der Hinweis auf einen
        /// brüchigen Zustand. Er steht im roten Feld und hindert niemanden am
        /// Speichern.</para>
        /// </summary>
        public bool SpeichernErlaubt(out string grund)
        {
            return EnergieEinheitenPruefung.ErreichtKwh(
                AktuelleEinheit(), (double)numHeizwert.Value, (double)numBrennwert.Value,
                _regeln, out grund);
        }

        /// <summary>
        /// Speichert Preise, Projektwerte und — seit Etappe K3 — die Umrechnungsregeln.
        /// <c>false</c> = abgelehnt, weil der Träger die kWh-Bedingung verletzt; dann
        /// wurde NICHTS geschrieben.
        /// </summary>
        public bool SaveProjectAndHistory()
        {
            string verstoss;
            if (!SpeichernErlaubt(out verstoss))
            {
                // Nichts schreiben. Der rote Hinweis im Block nennt den Grund bereits;
                // der Aufrufer entscheidet, ob er ihn zusätzlich als Meldung zeigt.
                if (lblVerstoss != null) lblVerstoss.Text = verstoss;
                return false;
            }

            SpeichereRegeln();
            SpeichereWerte();
            return true;
        }

        private void SpeichereWerte()
        {
            // Bei Strom gibt es keine Conversion aus der Combo
            //int currentConvID = _carrier.HasHi ? GetConvID(cmbUnit.SelectedItem) : -1;
            int currentConvID = GetConvID(cmbUnit.SelectedItem);

            double currentPriceBase = _baseWorkPrice;
            double currentHiBase = _baseHi;
            double currentHsBase = _baseHs;

            double currentGroundPrice = (double)numGrundpreis.Value;
            double currentPowerPrice = (double)numLeistungspreis.Value;
            double currentCO2 = (double)numCO2.Value;
            double currentSO2 = (double)numSO2.Value;
            double currentNOx = (double)numNOx.Value;

            // Ä9: Katalogkontext — die Werte gehen in die Katalogzeile selbst;
            // Historie (energy_price) und Projekt-Settings sind Projektsache.
            if (_projectId <= 0)
            {
                DataRepository.ExecuteSQL(
                    @"UPDATE energy_carrier
                      SET price_work = ?, price_base = ?, price_power = ?,
                          hi_kwh_per_unit = ?, hs_kwh_per_unit = ?,
                          co2 = ?, so2 = ?, nox = ?
                      WHERE id = ?",
                    new OleDbParameter("@ap", Math.Round(currentPriceBase, 4)),
                    new OleDbParameter("@gp", Math.Round(currentGroundPrice, 4)),
                    new OleDbParameter("@lp", Math.Round(currentPowerPrice, 4)),
                    new OleDbParameter("@hi", Math.Round(currentHiBase, 4)),
                    new OleDbParameter("@hs", Math.Round(currentHsBase, 4)),
                    new OleDbParameter("@co2", currentCO2),
                    new OleDbParameter("@so2", currentSO2),
                    new OleDbParameter("@nox", currentNOx),
                    new OleDbParameter("@id", _carrier.ID));

                _carrier.price_work = currentPriceBase;
                _carrier.price_base = currentGroundPrice;
                _carrier.price_power = currentPowerPrice;
                _carrier.HiKwhPerUnit = currentHiBase;
                _carrier.HsKwhPerUnit = currentHsBase;
                _carrier.CO2 = currentCO2;
                _carrier.SO2 = currentSO2;
                _carrier.NOx = currentNOx;

                _dbWorkPrice = currentPriceBase;
                _dbGroundPrice = currentGroundPrice;
                _dbPowerPrice = currentPowerPrice;
                _dbHi = currentHiBase;
                _dbHs = currentHsBase;
                _dbCO2 = currentCO2;
                _dbSO2 = currentSO2;
                _dbNOx = currentNOx;
                return;
            }

            // Das vom Benutzer gewählte (ggf. zukünftige) Datum abgreifen
            DateTime chosenDate = dtpValidFrom.Value;

            // Vergleich auf Basis der unberührten DB-Urwerte
            bool hasChanged = Math.Abs(currentPriceBase - _dbWorkPrice) > 0.0001 ||
                              Math.Abs(currentHiBase - _dbHi) > 0.0001 ||
                              Math.Abs(currentHsBase - _dbHs) > 0.0001 ||
                              Math.Abs(currentGroundPrice - _dbGroundPrice) > 0.01 ||
                              Math.Abs(currentPowerPrice - _dbPowerPrice) > 0.01 ||
                              Math.Abs(currentCO2 - _dbCO2) > 0.01 ||
                              Math.Abs(currentSO2 - _dbSO2) > 0.01 ||
                              Math.Abs(currentNOx - _dbNOx) > 0.01;

            if (hasChanged)
            {
                // NEU: Prüfen, ob für dieses Projekt, diesen Energieträger und GENAU dieses Datum bereits ein Eintrag existiert
                string sqlCheck = "SELECT COUNT(*) FROM energy_price WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?";
                OleDbParameter[] checkParams = {
                    new OleDbParameter("@cid", _carrier.ID),
                    new OleDbParameter("@prid", _projectId),
                    new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date } // .Date ignoriert Uhrzeit-Störfaktoren
                };

                int existingCount = Convert.ToInt32(DataRepository.ExecuteScalar(sqlCheck, checkParams));

                if (existingCount > 0)
                {
                    // Existiert bereits -> UPDATE
                    string sqlUpdateHistory = @"UPDATE energy_price 
                                                SET arbeitspreis = ?, heizwert = ?, grundpreis = ?, 
                                                    arbeitspreis_unit = ?, leistungspreis = ?
                                                WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?";

                    DataRepository.ExecuteSQL(sqlUpdateHistory, new OleDbParameter[] {
                        new OleDbParameter("@ap", Math.Round(currentPriceBase, 4)),
                        new OleDbParameter("@hi", Math.Round(currentHiBase, 4)),
                        new OleDbParameter("@gp", Math.Round(currentGroundPrice, 4)),
                        new OleDbParameter("@au", lblBasisnheit.Text),
                        new OleDbParameter("@lp", Math.Round(currentPowerPrice, 4)),
                        new OleDbParameter("@cid", _carrier.ID),
                        new OleDbParameter("@prid", _projectId),
                        new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date }
                    });
                }
                else
                {
                    // Existiert noch nicht -> INSERT
                    string sqlInsertHistory = @"INSERT INTO energy_price 
                                    (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, 
                                    valid_from, arbeitspreis_unit, leistungspreis) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                    DataRepository.ExecuteSQL(sqlInsertHistory, new OleDbParameter[] {
	                    new OleDbParameter("@cid", _carrier.ID),
	                    new OleDbParameter("@prid", _projectId),
	                    new OleDbParameter("@ap", Math.Round(currentPriceBase, 4)),
	                    new OleDbParameter("@hi", Math.Round(currentHiBase, 4)),
	                    new OleDbParameter("@gp", Math.Round(currentGroundPrice, 4)),
	                        new OleDbParameter("@date", OleDbType.Date) { Value = chosenDate.Date },
	                    new OleDbParameter("@au", lblBasisnheit.Text),
	                    new OleDbParameter("@lp", Math.Round(currentPowerPrice, 4))
	                });
                }

                // Speicher-Anker aktualisieren
                _dbWorkPrice = currentPriceBase;
                _dbGroundPrice = currentGroundPrice;
                _dbPowerPrice = currentPowerPrice;
                _dbHi = currentHiBase;
                _dbHs = currentHsBase;
                _dbCO2 = currentCO2;
                _dbSO2 = currentSO2;
                _dbNOx = currentNOx;
            }

            // Projekt-Settings Upsert
            string sqlUpsert = @"UPDATE energy_Project_settings 
                                SET custom_price_work = ?, custom_price_power = ?, custom_hi = ?, custom_hs = ?,
                                custom_price_base = ?, ID_Umrechnung = ?,
                                co2 = ?, so2 = ?, nox = ?
                                WHERE ID_Projekt = ? AND ID_Energieträger = ?";

            int rows = (int)DataRepository.ExecuteNonQuery(sqlUpsert, new OleDbParameter[] {
                new OleDbParameter("@p", currentPriceBase),
                new OleDbParameter("@pl", currentPowerPrice),
                new OleDbParameter("@hi", currentHiBase),
                new OleDbParameter("@hs", currentHsBase),
                new OleDbParameter("@b", currentGroundPrice),
                new OleDbParameter("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                new OleDbParameter("@co2", currentCO2),
                new OleDbParameter("@so2", currentSO2),
                new OleDbParameter("@nox", currentNOx),
                new OleDbParameter("@pid", _projectId),
                new OleDbParameter("@eid", _carrier.ID)
            });

            if (rows == 0)
            {
                string sqlInsert = @"INSERT INTO energy_Project_settings 
                                    (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, 
                                    custom_price_base, ID_Umrechnung, co2, so2, nox) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                    new OleDbParameter("@pid", _projectId),
                    new OleDbParameter("@eid", _carrier.ID),
                    new OleDbParameter("@p", currentPriceBase),
                    new OleDbParameter("@pl", currentPowerPrice),
                    new OleDbParameter("@h", currentHiBase),
                    new OleDbParameter("@hs", currentHsBase),
                    new OleDbParameter("@b", currentGroundPrice),
                    new OleDbParameter("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                    new OleDbParameter("@co2", currentCO2),
                    new OleDbParameter("@so2", currentSO2),
                    new OleDbParameter("@nox", currentNOx)
                });
            }

            // AP4: Der Aufschlagsblock schreibt in dieselbe Zeile und deshalb ERST
            // JETZT - vor dem Upsert oben gäbe es beim ersten Speichern noch keine.
            if (_aufschlaege != null) _aufschlaege.Uebernehmen();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            // ETAPPE K3: Der ausdrückliche Speicherbefehl bekommt eine ausdrückliche
            // Antwort. Der rote Hinweis im Regelblock steht ohnehin schon; die Meldung
            // ist die Bestätigung, dass NICHT gespeichert wurde.
            if (!SaveProjectAndHistory())
            {
                string grund;
                SpeichernErlaubt(out grund);
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.KOSTEN_UMRECHNUNG_SPEICHERN_ABGELEHNT, grund),
                                MyResource.Resource.KOSTEN_UMRECHNUNG_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            LoadHistory(_carrier.ID, _projectId);
        }

        public static List<EnergyConversion> GetConversions(int Idbrennstoff)
        {
            List<EnergyConversion> list = new List<EnergyConversion>();
            string sql = "SELECT id_brennstoff, from_unit, to_unit, factor FROM ENERGY_CONVERSION WHERE id_brennstoff = ?";

            OleDbParameter[] ps = { new OleDbParameter("@id", Idbrennstoff) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new EnergyConversion
                {
                    IDBrennstoff = Convert.ToInt32(row["Id_brennstoff"]),
                    FromUnit = row["from_unit"].ToString(),
                    ToUnitCode = row["to_unit"].ToString(),
                    Factor = Convert.ToDouble(row["factor"])
                });
            }
            return list;
        }

        public static dynamic GetProjectPrice(int projectId, int carrierId)
        {
            string sql = "SELECT * FROM ENERGY_PROJECT_SETTINGS WHERE ID_PROJEKT = ? AND id_ENERGIETRÄGER  = ?";

            OleDbParameter[] ps = {
                new OleDbParameter("@p", projectId),
                new OleDbParameter("@c", carrierId)
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                return new
                {
                    ArbeitspreisEurYear = row["custom_price_work"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_work"]) : null,
                    GrundpreisEurYear = row["custom_price_base"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_base"]) : null,
                    LeistungspreisEurYear = row["custom_price_power"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_price_power"]) : null,
                    CustomHi = row["custom_hi"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hi"]) : null,
                    CustomHs = row["custom_hs"] != DBNull.Value ? (double?)Convert.ToDouble(row["custom_hs"]) : null,
                    CO2 = row["co2"] != DBNull.Value ? (double?)Convert.ToDouble(row["co2"]) : null,
                    SO2 = row["so2"] != DBNull.Value ? (double?)Convert.ToDouble(row["so2"]) : null,
                    NOx = row["nox"] != DBNull.Value ? (double?)Convert.ToDouble(row["nox"]) : null,
                    IDUmrechnung = row["ID_Umrechnung"] != DBNull.Value ? (int?)Convert.ToInt32(row["ID_Umrechnung"]) : null
                };
            }
            return null;
        }

        public string GetTargetUnitByConversionId(int idumrechnung)
        {
            // Befund 26.08.2026: Eine leere oder verwaiste Umrechnungs-ID (z. B.
            // ein frisch aus dem Katalog übernommener Träger, ID_Umrechnung noch
            // NULL) liefert KEINE Zeile — der Rückgabewert von Next() wurde nie
            // ausgewertet und Read warf „No data exists for the row/column“.
            // Ohne Zeile gilt schlicht: keine Zieleinheit.
            RecordSet rs = new RecordSet();
            rs.Open("select to_unit from energy_conversion where id=" + idumrechnung);
            string unit = rs.Next() ? (string)rs.Read("to_Unit") : null;
            rs.Close();
            return unit;
        }

        public int GetConvID(object selectedItem)
        {
            if (selectedItem is EnergyConversion conv)
            {
                string sql = "SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?";
                OleDbParameter[] ps = {
                    new OleDbParameter("@cid", conv.IDBrennstoff),
                    new OleDbParameter("@fu", conv.FromUnit),
                    new OleDbParameter("@tu", conv.ToUnitCode)
                };
                DataTable dt = DataRepository.GetDataTable(sql, ps);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return Convert.ToInt32(dt.Rows[0]["ID"]);
                }
            }
            return -1;
        }

        private void SetupHistoryGrid()
        {
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.AllowUserToAddRows = false;
            dgvHistory.ReadOnly = true;
            dgvHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistory.RowHeadersVisible = false;
            dgvHistory.Columns.Clear();

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "valid_from",
                HeaderText = "Gültig ab",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } // Format auf reines Datum gekürzt
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "heizwert",
                HeaderText = "Heizwert",
                Width = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis_unit",
                HeaderText = "Basis Einheit",
                Width = 50
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "arbeitspreis",
                HeaderText = "Arbeitspreis",
                Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" }
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "grundpreis",
                HeaderText = "Grundpreis",
                Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" },
            });

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "leistungspreis",
                HeaderText = "Leistungspreis",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" },
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dgvHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        }

        public void LoadHistory(int carrierId, int? projectId = null)
        {
            string sql = "SELECT valid_from, heizwert, arbeitspreis, grundpreis, " +
                         "arbeitspreis_unit, leistungspreis " +
                         "FROM energy_price WHERE carrier_id = ?";

            List<OleDbParameter> parameters = new List<OleDbParameter>();
            parameters.Add(new OleDbParameter("@cid", carrierId));

            if (projectId.HasValue)
            {
                sql += " AND id_projekt = ?";
                parameters.Add(new OleDbParameter("@pid", projectId.Value));
            }

            sql += " ORDER BY valid_from DESC";

            try
            {
                DataTable dt = DataRepository.GetDataTable(sql, parameters.ToArray());
                dgvHistory.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Historie: " + ex.Message);
            }
        }
 
    }
}