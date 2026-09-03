using System;
using System.Collections.Generic;
using System.Data;
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
        private Label _lblEffektivpreis;
        private CheckBox _chkAufschlagAnwenden;

        /// <summary>Ä16: Bezugspreis = Arbeitspreis + wirksamer Aufschlag [ct/kWh].</summary>
        private void EffektivpreisAnzeigen()
        {
            if (_lblEffektivpreis == null || _aufschlaege == null) return;
            try
            {
                double arbeitCt = (double)numArbeitspreis.Value * 100.0;
                double aufschlagCt = _aufschlaege.WirksamCtKwh;
                _lblEffektivpreis.Text = string.Format(
                    TKd4("KDLG_EFFEKTIVPREIS",
                        "Bezugspreis inkl. Aufschläge: {0:N2} ct/kWh  (Arbeitspreis {1:N2} + Aufschlag {2:N2})"),
                    arbeitCt + aufschlagCt, arbeitCt, aufschlagCt);
            }
            catch { _lblEffektivpreis.Text = ""; }
        }

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
            BaueBrennstoffblock();    // ETAPPE B2 - Gegenstueck zum Aufschlagsblock, andere Traegerfamilie
            BaueLeistungspreisZusatz();   // ETAPPE KD4 (FK6/FK6a) - nach LoadData, das die Einheit setzt
            BaueEmissionsReiter();        // ETAPPE E3 - ZULETZT: haengt den fertigen Bestand um

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
        /// Zugang zu den Saisonreihen (FK6a).
        ///
        /// <para>Ä18 (Nutzerauftrag 26.08.2026): Der frühere Strom-Sonderfall
        /// (Feld gesperrt, Verweis bzw. Einstieg zur Tarifstruktur) ist entfallen —
        /// der Stromträger pflegt seinen FLAT-Leistungspreis hier wie jeder andere
        /// Träger (Jahres-/Monatssatz, Migrationsschritt 44 schaltet das Merkmal
        /// frei). Die Tarifstruktur ist das DETAILMODELL und wird komponentenbezogen
        /// auf der Wirtschaftlichkeitsseite gepflegt (Strombezug/BHKW/PV); ist sie
        /// aktiv, ersetzt sie die Flat-Strompreise einschließlich dieses Satzes.</para>
        /// </summary>
        private void BaueLeistungspreisZusatz()
        {
            Control eltern = numLeistungspreis.Parent;
            if (eltern == null) return;

            // Ä18: kein Strom-Sonderfall mehr — ELECTRICITY führt has_powerprice
            // seit Migrationsschritt 44 und läuft durch denselben Zweig wie Gas.
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
                    new DbParam("@id", _carrier.ID));
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
                    new DbParam("@m", cmbLeistungsModus.SelectedIndex == 1
                        ? DbWerte.LEISTUNGSPREIS_MODUS_MONAT
                        : DbWerte.LEISTUNGSPREIS_MODUS_JAHR),
                    new DbParam("@id", _carrier.ID));
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

                // Ä16: Der Gesamtpreis inkl. Aufschlag steht IM Dialog, und die
                // Entscheidung „Aufschläge in der Wirtschaftlichkeit anwenden“
                // wird HIER getroffen (der Parameterdialog zeigt sie nur noch).
                _lblEffektivpreis = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(26, 50, 97),
                    Location = new Point(17, _aufschlaege.Location.Y + ucStromAufschlaege.HOEHE + 4)
                };
                this.Controls.Add(_lblEffektivpreis);

                if (_projectId > 0)
                {
                    _chkAufschlagAnwenden = new CheckBox
                    {
                        AutoSize = true,
                        Text = TKd4("KDLG_AUFSCHLAG_ANWENDEN",
                            "Aufschläge in der Wirtschaftlichkeit berücksichtigen"),
                        Location = new Point(17,
                            _aufschlaege.Location.Y + ucStromAufschlaege.HOEHE + 28)
                    };
                    try
                    {
                        _chkAufschlagAnwenden.Checked = new WirtschaftlichkeitCtrl()
                            .LadeParameter(_projectId).AufschlaegeAnwenden;
                    }
                    catch { }
                    _chkAufschlagAnwenden.CheckedChanged += (s, e2) =>
                    {
                        try
                        {
                            var ctrlW = new WirtschaftlichkeitCtrl();
                            WirtschaftlichkeitParameter pw = ctrlW.LadeParameter(_projectId);
                            pw.AufschlaegeAnwenden = _chkAufschlagAnwenden.Checked;
                            ctrlW.SpeichereParameter(pw);
                        }
                        catch { }
                    };
                    this.Controls.Add(_chkAufschlagAnwenden);
                }
                this.Height += 52;

                _aufschlaege.WirksamGeaendert += (s, e2) => EffektivpreisAnzeigen();
                numArbeitspreis.ValueChanged += (s, e2) => EffektivpreisAnzeigen();
                EffektivpreisAnzeigen();
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
        // ETAPPE B2 - Preisbestandteile der BRENNSTOFF-Traeger
        // (Konzept BHKW-Wirtschaftlichkeit § 4.1 / § 6.2, Befund BW1)
        // =====================================================================

        /// <summary>
        /// Preiszerlegung des Brennstoffs (Energiesteuer, CO2-Anteil, Netz-/Messentgelt,
        /// Vertrieb) - nur bei der Brennstoff-Familie belegt, sonst <c>null</c>.
        /// </summary>
        private ucBrennstoffBestandteile _bestandteile;

        /// <summary>
        /// Preismodelle, deren Traeger eine Preiszerlegung nach § 4.1 bekommen.
        /// </summary>
        /// <remarks>
        /// <para>Der Bestand fuehrt sechs Codes in <c>pricing_model</c> (gemessen am
        /// 30.08.2026: ANIMAL_FAT, ELECTRICITY, GASEOUS_FUEL, HEAT, LIQUID_FUEL,
        /// SOLID_FUEL). Vier davon sind Brennstoffe und stehen hier.</para>
        ///
        /// <para><b>ELECTRICITY</b> hat seinen eigenen Block
        /// (<see cref="BaueAufschlagsblock"/>) - Netzentgelt, Umlagen und Stromsteuer
        /// sind dort schon zerlegt, und beide Bloecke nebeneinander waeren zwei
        /// Wahrheiten ueber denselben Preis.</para>
        ///
        /// <para><b>HEAT</b> (Fernwaerme) bleibt ausdruecklich aussen vor: Energiesteuer
        /// und BEHG-Abgabe entstehen beim ERZEUGER der Waerme, nicht beim Bezieher. Im
        /// Fernwaermepreis stecken sie allenfalls eingepreist - einen ausweisbaren
        /// gesetzlichen Satz je bezogener Kilowattstunde gibt es nicht, und die
        /// Schnellwahl haette nichts zu lesen (der Traeger fuehrt weder Brennstoff-ID
        /// noch Heizwert). Eine leere Maske waere schlechter als keine.</para>
        /// </remarks>
        private static readonly string[] PREISMODELLE_BRENNSTOFF =
        {
            "GASEOUS_FUEL", "LIQUID_FUEL", "SOLID_FUEL", "ANIMAL_FAT"
        };

        /// <summary>
        /// Haengt bei den Brennstoff-Traegern die Preiszerlegung unter den Bestand und
        /// waechst um deren Hoehe - dieselbe Bauform wie
        /// <see cref="BaueAufschlagsblock"/> (programmatisch, Designer unberuehrt).
        /// </summary>
        /// <remarks>
        /// Die beiden Bloecke schliessen einander aus: Strom bekommt den Aufschlagsblock,
        /// Brennstoff die Zerlegung. Deshalb dockt dieser Block an dieselbe Stelle an -
        /// zum Zeitpunkt des Aufrufs hat <see cref="BaueAufschlagsblock"/> die Hoehe bei
        /// einem Brennstoff-Traeger nicht angefasst.
        /// </remarks>
        private void BaueBrennstoffblock()
        {
            if (_carrier == null) return;
            if (Array.IndexOf(PREISMODELLE_BRENNSTOFF, (_carrier.PricingModel ?? "").ToUpperInvariant()) < 0)
                return;

            try
            {
                _bestandteile = new ucBrennstoffBestandteile(_projectId, _carrier.ID);
                _bestandteile.Location = new Point(17, this.Height + 8);
                this.Height += ucBrennstoffBestandteile.HOEHE + 16;
                this.Controls.Add(_bestandteile);

                // Der Arbeitspreis ist die Bezugsgroesse der Restzeile und wird bei jeder
                // Aenderung nachgezogen - auch beim Einheitenwechsel, der beide Felder
                // umrechnet. Der Heizwert geht in die Einheitenkette der Schnellwahl ein.
                _bestandteile.ArbeitspreisCtKwh = ArbeitspreisInCtKwh();
                numArbeitspreis.ValueChanged += (s, e2) => BestandteileNachziehen();
                numHeizwert.ValueChanged += (s, e2) => BestandteileNachziehen();
                numBrennwert.ValueChanged += (s, e2) => BestandteileNachziehen();

                // Der Block schreibt den Preis NIE selbst: Er meldet nur, dass der
                // Anwender ihn uebernehmen moechte. Eingetragen wird er hier.
                _bestandteile.InArbeitspreisUebernehmen += (s, e2) => ArbeitspreisAusBestandteilen();
            }
            catch (Exception ex)
            {
                // Ein fehlender Zerlegungsblock darf die Preispflege nicht blockieren -
                // etwa auf einer Datenbank, deren Migrationsschritt M-1 nicht durchlief.
                Console.WriteLine("Der Zerlegungsblock konnte nicht aufgebaut werden: " + ex.Message);
                _bestandteile = null;
            }
        }

        /// <summary>
        /// Der Arbeitspreis dieses Traegers in ct/kWh - die Einheit, in der die
        /// Preisbestandteile gefuehrt werden.
        /// </summary>
        /// <remarks>
        /// <para><b>Warum der Quotient und nicht der Feldwert.</b> Der Arbeitspreis steht
        /// bei einem Brennstoff in <b>€ je Abrechnungseinheit</b> (€/Nm³, €/L, €/kg), nur
        /// bei Traegern ohne Heizwert (Strom, Fernwaerme) direkt in €/kWh - das ist
        /// dieselbe Fallunterscheidung wie in <see cref="UpdatePricePerKWh"/>.</para>
        ///
        /// <para><b>Warum die Basiswerte und nicht die Eingabefelder.</b>
        /// <c>_baseWorkPrice</c> und <c>_baseHi</c> sind auf die Abrechnungseinheit
        /// normiert und bleiben beim Einheitenwechsel unveraendert. Die Felder dagegen
        /// werden in <see cref="CmbUnit_SelectedIndexChanged"/> NACHEINANDER umgerechnet -
        /// zwischen den beiden Zuweisungen stuenden Preis und Heizwert kurz in
        /// verschiedenen Einheiten, und der Quotient waere fuer diesen Moment falsch.
        /// </para>
        /// </remarks>
        private double ArbeitspreisInCtKwh()
        {
            try
            {
                if (!_carrier.HasHi) return _baseWorkPrice * 100.0;
                if (_baseHi <= 0.0) return 0.0;
                return _baseWorkPrice / _baseHi * 100.0;
            }
            catch { return 0.0; }
        }

        /// <summary>Zieht Arbeitspreis und Heizwerte im Zerlegungsblock nach.</summary>
        private void BestandteileNachziehen()
        {
            if (_bestandteile == null) return;
            try
            {
                _bestandteile.ArbeitspreisCtKwh = ArbeitspreisInCtKwh();
                _bestandteile.HeizwerteAktualisieren(_baseHi, _baseHs);
            }
            catch { }
        }

        /// <summary>
        /// Traegt den Preis aus den Bestandteilen in das Arbeitspreisfeld ein - der
        /// Rueckweg von ct/kWh in die Abrechnungseinheit des Traegers.
        /// </summary>
        /// <remarks>
        /// Geschrieben wird erst mit dem Speichern des Dialogs; hier steht nur der Wert
        /// im Feld. Ohne Heizwert gibt es keinen Rueckweg - dann bleibt das Feld, wie es
        /// war, statt eine geratene Umrechnung zu zeigen.
        /// </remarks>
        private void ArbeitspreisAusBestandteilen()
        {
            if (_bestandteile == null) return;
            try
            {
                double ctKwh = _bestandteile.PreisAusBestandteilenCtKwh;
                double jeEinheit;

                if (!_carrier.HasHi) jeEinheit = ctKwh / 100.0;
                else
                {
                    double hi = (double)numHeizwert.Value;
                    if (hi <= 0.0) return;
                    jeEinheit = ctKwh / 100.0 * hi;
                }

                decimal wert = (decimal)jeEinheit;
                if (wert < numArbeitspreis.Minimum) wert = numArbeitspreis.Minimum;
                if (wert > numArbeitspreis.Maximum) wert = numArbeitspreis.Maximum;
                numArbeitspreis.Value = wert;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Preis aus den Bestandteilen konnte nicht uebernommen werden: "
                                  + ex.Message);
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
                        new DbParam[]
                        {
                            new DbParam("@von", r.Von),
                            new DbParam("@nach", r.Nach),
                            new DbParam("@f", r.Faktor),
                            new DbParam("@n", r.Name ?? ""),
                            new DbParam("@a", r.Aktiv),
                            new DbParam("@id", r.Id)
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
                        new DbParam[]
                        {
                            new DbParam("@id", neueId),
                            new DbParam("@b", r.IdBrennstoff),
                            new DbParam("@von", r.Von),
                            new DbParam("@nach", r.Nach),
                            new DbParam("@f", r.Faktor),
                            new DbParam("@n", r.Name ?? ""),
                            new DbParam("@a", r.Aktiv)
                        });
                    r.Id = neueId;
                }
            }
        }

        // =====================================================================
        // ETAPPE E3 - Emissions-Reiter
        //   Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md § 4.1.
        //   Der Detailbereich bekommt zwei Reiter: „Preise & Umrechnung" traegt
        //   den vollstaendigen Bestand (UMGEHAENGT, nicht neu gebaut), „Emissionen"
        //   die dynamische Feldliste der ausgewaehlten Arten, die CO2e-Summe und
        //   den Modus-Schalter. Die Regeln stehen in EmissionenCtrl; hier steht
        //   nur die Darstellung.
        // =====================================================================

        private TabControl tabDetails;
        private TabPage tabPreise;
        private TabPage tabEmissionen;

        /// <summary>Der UI-freie Bearbeitungsstand der Emissionswerte dieses Trägers.</summary>
        private EmissionenCtrl _emissionen;

        private readonly List<EmissionsFeld> _emissionsFelder = new List<EmissionsFeld>();
        private Panel _pnlEmissionsZeilen;
        private Label _lblEmissionsSumme;
        private Label _lblEmissionsHinweis;
        private Button _btnEmissionVerwalten;
        private RadioButton _rbModusCo2;
        private RadioButton _rbModusCo2e;
        private int _emissionsZeilenOben;

        /// <summary>Sperre gegen Rückkopplung, während der Emissionsblock gefüllt wird.</summary>
        private bool _emissionsblockWirdGefuellt;

        /// <summary>Spaltenraster des Emissions-Tabs (§ 4.1: Art · Wert · Einheit ·
        /// Herkunft · Katalog-Knopf).</summary>
        private const int EM_X_NAME = 10, EM_X_WERT = 150, EM_X_EINHEIT = 246,
                          EM_X_HERKUNFT = 306, EM_X_KATALOG = 450;

        private const int EM_ZEILENHOEHE = 28;

        /// <summary>Die Steuerelemente EINER Emissionszeile samt ihrem
        /// Bearbeitungsstand. Die Zahl steht im <see cref="EmissionsZeile"/>, nicht
        /// im Feld — geschrieben wird das Objekt, nicht der Text.</summary>
        private sealed class EmissionsFeld
        {
            public EmissionsZeile Zeile;
            public TextBox Wert;
            public Label Herkunft;
            public Button Katalog;

            /// <summary>Trägt den VOLLSTÄNDIGEN Herkunftstext, den die Spalte
            /// nur gekürzt zeigt.</summary>
            public ToolTip Tip;

            /// <summary>true, solange im Feld etwas steht, das keine Zahl ist —
            /// dann wurde NICHTS übernommen und der Verlassen-Handler setzt zurück.</summary>
            public bool Ungueltig;
        }

        /// <summary>
        /// Gliedert den Detailbereich in die beiden Reiter und baut den
        /// Emissions-Tab (§ 4.1).
        /// </summary>
        /// <remarks>
        /// <para><b>Zuletzt aufgerufen und rein umhängend</b>: Der Bestand entsteht
        /// wie bisher (Designer-Raster, Umrechnungsblock, Aufschlagsblock,
        /// Leistungspreis-Zusatz) und wird erst danach in die Seite „Preise &amp;
        /// Umrechnung" verschoben. So bleibt jede vorhandene Positionsrechnung
        /// gültig — sie läuft auf denselben Koordinaten wie zuvor, nur um die
        /// Höhe der beiden Kopfzeilen versetzt.</para>
        ///
        /// <para><b>Die drei Bestandsfelder bleiben als WERTTRÄGER</b>:
        /// <c>numCO2</c>/<c>numSO2</c>/<c>numNOx</c> wandern unsichtbar in den
        /// Emissions-Tab. Sichtbar sind stattdessen Textfelder ohne Drehpfeile
        /// (Anforderung 7). Der Schreibweg des Dialogs
        /// (<see cref="SpeichereWerte"/>) liest die drei Felder unverändert weiter
        /// — genau das ist der Spiegel in die Altspalten, den Konzept F9 verlangt;
        /// jede Änderung im Textfeld wird deshalb sofort dorthin gespiegelt.</para>
        ///
        /// <para><b>Ohne Artenkatalog</b> (Migrationsschritt 57 nicht gelaufen)
        /// bleiben die drei Bestandsfelder SICHTBAR im Emissions-Tab stehen. Eine
        /// leere Emissionsmaske wäre schlechter als die alte.</para>
        /// </remarks>
        private void BaueEmissionsReiter()
        {
            try
            {
                _emissionen = new EmissionenCtrl(_projectId, _carrier.ID);
                _emissionen.Laden();

                // Beide Kopfzeilen sind Dock.Top und bleiben ueber den Reitern stehen.
                int oben = lblCarrierName.Height + lblGruppe.Height;

                Control[] altfelder = { label12, label11, numSO2, label3, numCO2,
                                        label10, numNOx };
                int altOben = int.MaxValue;
                foreach (Control c in altfelder) if (c.Top < altOben) altOben = c.Top;

                // Die Emissionszeile des Designer-Rasters verlaesst die Preisseite -
                // ihre Luecke wuerde sonst als Loch vor der Speichern-Zeile stehen
                // bleiben. Der Betrag wird gemessen, nicht gesetzt.
                if (_emissionen.Verfuegbar)
                {
                    int frei = btn_Save.Top - altOben;
                    foreach (Control c in new Control[] { btn_Save, dtpValidFrom, label9, dgvHistory })
                        c.Top -= frei;
                    this.Height -= frei;
                }

                tabPreise = new TabPage(TKd4("KDLG_ET_TAB_PREISE", "Preise && Umrechnung"))
                { AutoScroll = true };
                tabEmissionen = new TabPage(TKd4("KDLG_ET_TAB_EMISSIONEN", "Emissionen"))
                { AutoScroll = true };

                var bestand = new List<Control>();
                foreach (Control c in this.Controls)
                    if (c != lblCarrierName && c != lblGruppe) bestand.Add(c);

                int unterkante = 0;
                foreach (Control c in bestand)
                {
                    this.Controls.Remove(c);
                    bool alt = Array.IndexOf(altfelder, c) >= 0;

                    if (alt)
                    {
                        c.Top = c.Top - altOben + 12;
                        c.Visible = !_emissionen.Verfuegbar;
                        tabEmissionen.Controls.Add(c);
                        continue;
                    }

                    c.Top -= oben;
                    tabPreise.Controls.Add(c);
                    if (c.Visible && c.Bottom > unterkante) unterkante = c.Bottom;
                }

                int unterkanteEmission = BaueEmissionsInhalt();

                tabDetails = new TabControl
                {
                    Location = new Point(0, oben),
                    Size = new Size(this.ClientSize.Width,
                                    Math.Max(unterkante, unterkanteEmission) + 40),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                tabDetails.TabPages.Add(tabPreise);
                tabDetails.TabPages.Add(tabEmissionen);
                this.Controls.Add(tabDetails);
                this.Height = oben + tabDetails.Height;
            }
            catch (Exception ex)
            {
                // Ein misslungener Reiter darf die Preispflege nicht blockieren -
                // dieselbe Zusage wie beim Umrechnungs- und Aufschlagsblock.
                Console.WriteLine("Der Emissions-Reiter konnte nicht aufgebaut werden: " + ex.Message);
            }
        }

        /// <summary>Baut den Inhalt des Emissions-Tabs; Rückgabe ist seine
        /// Unterkante (für die Höhe des Reiters).</summary>
        private int BaueEmissionsInhalt()
        {
            if (!_emissionen.Verfuegbar)
            {
                var fehlt = new Label
                {
                    AutoSize = false,
                    Location = new Point(EM_X_NAME, 112),
                    Size = new Size(520, 34),
                    ForeColor = Color.Firebrick,
                    Text = TKd4("KDLG_EM_KEIN_KATALOG",
                        "Der Emissionsarten-Katalog ist auf dieser Datenbank nicht verfügbar " +
                        "(Migrationsschritt 57 fehlt). Es gelten die drei Bestandsfelder.")
                };
                tabEmissionen.Controls.Add(fehlt);
                return fehlt.Bottom;
            }

            // --- Modus-Schalter (F7) --------------------------------------------
            // Im PROJEKTkontext trifft er das Projektfeld, im Katalogkontext die
            // globale Vorgabe - beides schreibt erst „Speichern" (Ä12/Ä14).
            var pnlModus = new Panel
            {
                Location = new Point(EM_X_NAME, 8),
                Size = new Size(540, 26)
            };
            pnlModus.Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(0, 5),
                Text = TKd4("KDLG_EM_MODUS", "CO₂-Berechnung:")
            });
            _rbModusCo2 = new RadioButton
            {
                AutoSize = true,
                Location = new Point(118, 3),
                Text = TKd4("KDLG_EM_MODUS_CO2", "CO₂")
            };
            _rbModusCo2e = new RadioButton
            {
                AutoSize = true,
                Location = new Point(178, 3),
                Text = TKd4("KDLG_EM_MODUS_CO2E", "CO₂-Äquivalent (GWP₁₀₀)")
            };
            pnlModus.Controls.Add(_rbModusCo2);
            pnlModus.Controls.Add(_rbModusCo2e);
            pnlModus.Controls.Add(new Label
            {
                AutoSize = true,
                Location = new Point(370, 5),
                ForeColor = Color.FromArgb(90, 90, 90),
                Text = _projectId > 0
                    ? TKd4("KDLG_EM_MODUS_ORT_PROJEKT", "[Projekt]")
                    : TKd4("KDLG_EM_MODUS_ORT_VORGABE", "[globale Vorgabe]")
            });

            _emissionsblockWirdGefuellt = true;
            try
            {
                _rbModusCo2e.Checked = string.Equals(_emissionen.Modus,
                    DbWerte.EMISSION_MODUS_CO2E, StringComparison.Ordinal);
                _rbModusCo2.Checked = !_rbModusCo2e.Checked;
            }
            finally { _emissionsblockWirdGefuellt = false; }

            _rbModusCo2.CheckedChanged += (s, e) =>
            {
                if (!_emissionsblockWirdGefuellt && _rbModusCo2.Checked)
                    _emissionen.Modus = DbWerte.EMISSION_MODUS_CO2;
            };
            _rbModusCo2e.CheckedChanged += (s, e) =>
            {
                if (!_emissionsblockWirdGefuellt && _rbModusCo2e.Checked)
                    _emissionen.Modus = DbWerte.EMISSION_MODUS_CO2E;
            };
            tabEmissionen.Controls.Add(pnlModus);

            // --- Spaltenkopf -----------------------------------------------------
            tabEmissionen.Controls.Add(Spaltenkopf(TKd4("KDLG_EM_SP_ART", "Art"), EM_X_NAME, 44, 134));
            tabEmissionen.Controls.Add(Spaltenkopf(TKd4("KDLG_EM_SP_WERT", "Wert"), EM_X_WERT, 44, 88));
            tabEmissionen.Controls.Add(Spaltenkopf(TKd4("KDLG_EM_SP_EINHEIT", "Einheit"), EM_X_EINHEIT, 44, 56));
            tabEmissionen.Controls.Add(Spaltenkopf(TKd4("KDLG_EM_SP_HERKUNFT", "Herkunft"), EM_X_HERKUNFT, 44, 140));

            _emissionsZeilenOben = 66;
            _pnlEmissionsZeilen = new Panel
            {
                Location = new Point(0, _emissionsZeilenOben),
                Size = new Size(550, EM_ZEILENHOEHE)
            };
            tabEmissionen.Controls.Add(_pnlEmissionsZeilen);

            _lblEmissionsSumme = new Label
            {
                AutoSize = false,
                Location = new Point(EM_X_NAME, 0),
                Size = new Size(520, 20),
                Font = new Font(this.Font, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 50, 97)
            };
            tabEmissionen.Controls.Add(_lblEmissionsSumme);

            _lblEmissionsHinweis = new Label
            {
                AutoSize = false,
                Location = new Point(EM_X_NAME, 0),
                Size = new Size(526, 34),
                ForeColor = Color.FromArgb(150, 90, 0)
            };
            tabEmissionen.Controls.Add(_lblEmissionsHinweis);

            _btnEmissionVerwalten = new Button
            {
                Location = new Point(EM_X_NAME, 0),
                Size = new Size(250, 27),
                Text = TKd4("KDLG_EM_VERWALTEN", "Emissionsarten && Katalog verwalten…")
            };
            _btnEmissionVerwalten.Click += (s, e) => KatalogVerwalten();
            tabEmissionen.Controls.Add(_btnEmissionVerwalten);

            ZeigeEmissionszeilen();
            return _btnEmissionVerwalten.Bottom + 8;
        }

        private static Label Spaltenkopf(string text, int x, int y, int breite)
        {
            var l = new Label
            {
                AutoSize = false,
                Location = new Point(x, y),
                Size = new Size(breite, 19),
                Text = text
            };
            l.Font = new Font(l.Font, FontStyle.Bold);
            return l;
        }

        /// <summary>
        /// Baut die Feldzeilen aus den ausgewählten Arten neu (F5) und richtet
        /// Summenzeile, Hinweis und Verwalten-Knopf darunter aus.
        /// </summary>
        private void ZeigeEmissionszeilen()
        {
            if (_pnlEmissionsZeilen == null || _emissionen == null) return;

            _emissionsblockWirdGefuellt = true;
            try
            {
                _pnlEmissionsZeilen.Controls.Clear();
                _emissionsFelder.Clear();

                int y = 0;
                foreach (EmissionsZeile z in _emissionen.Zeilen)
                {
                    var feld = new EmissionsFeld { Zeile = z };

                    _pnlEmissionsZeilen.Controls.Add(new Label
                    {
                        AutoSize = false,
                        Location = new Point(EM_X_NAME, y + 5),
                        Size = new Size(134, 19),
                        Text = z.Art.Name
                    });

                    feld.Wert = new TextBox
                    {
                        Location = new Point(EM_X_WERT, y + 1),
                        Size = new Size(88, 25),
                        TextAlign = HorizontalAlignment.Right,
                        Text = EmissionsZahlText(z.Wert)
                    };

                    _pnlEmissionsZeilen.Controls.Add(new Label
                    {
                        AutoSize = false,
                        Location = new Point(EM_X_EINHEIT, y + 5),
                        Size = new Size(56, 19),
                        Text = z.Art.Einheit
                    });

                    // Der Herkunftstext ist oft länger als die Spalte („EBeV 2030,
                    // Anlage 2 Teil 4 …"). Er wird deshalb mit Auslassungspunkten
                    // gekürzt und steht vollständig im Tooltip - abschneiden ohne
                    // Hinweis wäre eine halbe Quellenangabe.
                    feld.Herkunft = new Label
                    {
                        AutoSize = false,
                        AutoEllipsis = true,
                        Location = new Point(EM_X_HERKUNFT, y + 5),
                        Size = new Size(140, 19),
                        ForeColor = Color.FromArgb(90, 90, 90),
                        Text = z.QuelleText
                    };
                    feld.Tip = new ToolTip();
                    feld.Tip.SetToolTip(feld.Herkunft, z.QuelleText);

                    feld.Katalog = new Button
                    {
                        Location = new Point(EM_X_KATALOG, y),
                        Size = new Size(86, 26),
                        Text = TKd4("KDLG_EM_KATALOG", "Katalog…")
                    };

                    if (z.NurLesend)
                    {
                        // Projektkontext: nur die drei Kernarten sind editierbar
                        // (Kontext-Regel zu § 4.1). Die uebrigen stehen mit ihrem
                        // KATALOGWERT da - lesbar, aber nicht hier pflegbar.
                        feld.Wert.ReadOnly = true;
                        feld.Wert.BackColor = SystemColors.Control;
                        feld.Katalog.Enabled = false;
                        var tip = new ToolTip();
                        tip.SetToolTip(feld.Wert, TKd4("KDLG_EM_NUR_KATALOG",
                            "Pflege im Katalogkontext"));
                        tip.SetToolTip(feld.Katalog, TKd4("KDLG_EM_NUR_KATALOG",
                            "Pflege im Katalogkontext"));
                    }
                    else
                    {
                        EmissionsFeld f = feld;   // Schleifenvariable festhalten
                        feld.Wert.TextChanged += (s, e) =>
                        { if (!_emissionsblockWirdGefuellt) EmissionsWertGetippt(f); };
                        feld.Wert.Leave += (s, e) =>
                        { if (!_emissionsblockWirdGefuellt) EmissionsWertVerlassen(f); };
                        feld.Katalog.Click += (s, e) => KatalogFuerZeile(f);
                    }

                    _pnlEmissionsZeilen.Controls.Add(feld.Wert);
                    _pnlEmissionsZeilen.Controls.Add(feld.Herkunft);
                    _pnlEmissionsZeilen.Controls.Add(feld.Katalog);
                    _emissionsFelder.Add(feld);

                    y += EM_ZEILENHOEHE;
                }

                _pnlEmissionsZeilen.Height = Math.Max(y, EM_ZEILENHOEHE);
            }
            finally { _emissionsblockWirdGefuellt = false; }

            _lblEmissionsSumme.Top = _pnlEmissionsZeilen.Bottom + 10;
            _lblEmissionsHinweis.Top = _lblEmissionsSumme.Bottom + 2;
            _btnEmissionVerwalten.Top = _lblEmissionsHinweis.Bottom + 6;

            AktualisiereEmissionsSumme();
        }

        /// <summary>Live-Prüfung der Handeingabe (F8): Was keine Zahl ist, wird
        /// NICHT übernommen und rot hinterlegt; was eine ist, setzt die Herkunft
        /// auf „Eigener Wert", spiegelt in das Bestandsfeld und rechnet die Summe
        /// neu.</summary>
        private void EmissionsWertGetippt(EmissionsFeld feld)
        {
            if (!_emissionen.WertEingeben(feld.Zeile, feld.Wert.Text))
            {
                feld.Ungueltig = true;
                feld.Wert.BackColor = Color.MistyRose;
                return;
            }
            feld.Ungueltig = false;
            feld.Wert.BackColor = SystemColors.Window;
            feld.Herkunft.Text = feld.Zeile.QuelleText;
            if (feld.Tip != null) feld.Tip.SetToolTip(feld.Herkunft, feld.Zeile.QuelleText);
            SpiegelKernwert(feld.Zeile);
            AktualisiereEmissionsSumme();
        }

        private void EmissionsWertVerlassen(EmissionsFeld feld)
        {
            if (!feld.Ungueltig) return;

            _emissionsblockWirdGefuellt = true;
            try
            {
                feld.Wert.Text = EmissionsZahlText(feld.Zeile.Wert);
                feld.Wert.BackColor = SystemColors.Window;
                feld.Ungueltig = false;
            }
            finally { _emissionsblockWirdGefuellt = false; }
        }

        /// <summary>Summenzeile nach F6 und der F3-Hinweis daneben.</summary>
        private void AktualisiereEmissionsSumme()
        {
            if (_lblEmissionsSumme == null || _emissionen == null) return;

            _lblEmissionsSumme.Text = string.Format(CultureInfo.CurrentCulture,
                TKd4("KDLG_EM_SUMME", "CO₂-Äquivalent gesamt (ausgewählte Arten): {0} g/kWh"),
                _emissionen.SummeCo2eGKwh().ToString("N2", CultureInfo.CurrentCulture));

            _lblEmissionsHinweis.Text = _emissionen.SummeIstBereitsAequivalent()
                ? TKd4("KDLG_EM_SUMME_F3",
                    "CO₂-Wert ist bereits Äquivalent — Summe = Wert, weitere Arten werden " +
                    "nicht aufsummiert.")
                : "";
        }

        /// <summary>
        /// Spiegelt eine Kernart in ihr Bestandsfeld. Das ist die Stelle, an der
        /// der Altschreibweg (<see cref="SpeichereWerte"/>) seinen Wert bekommt —
        /// Konzept F9: Die Rechner lesen bis Etappe E5 die alten Spalten.
        /// </summary>
        private void SpiegelKernwert(EmissionsZeile z)
        {
            NumericUpDown feld = KernartFeld(z);
            if (feld == null) return;

            decimal wert = (decimal)(z.Wert ?? 0.0);
            if (wert < feld.Minimum) wert = feld.Minimum;
            if (wert > feld.Maximum) wert = feld.Maximum;

            bool vorher = _isUpdatingUi;
            _isUpdatingUi = true;
            try { feld.Value = wert; }
            finally { _isUpdatingUi = vorher; }
        }

        private void SpiegelKernwerte()
        {
            if (_emissionen == null) return;
            foreach (EmissionsZeile z in _emissionen.Zeilen) SpiegelKernwert(z);
        }

        private NumericUpDown KernartFeld(EmissionsZeile z)
        {
            if (z == null) return null;
            if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_CO2, StringComparison.OrdinalIgnoreCase))
                return numCO2;
            if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_SO2, StringComparison.OrdinalIgnoreCase))
                return numSO2;
            if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_NOX, StringComparison.OrdinalIgnoreCase))
                return numNOx;
            return null;
        }

        /// <summary>„Katalog…" einer Zeile: der E4-Dialog, vorgefiltert auf Art und
        /// Träger. Die Übernahme wird ZURÜCKGEREICHT und lebt bis zum Speichern nur
        /// im Objekt (Ä12/Ä14).</summary>
        private void KatalogFuerZeile(EmissionsFeld feld)
        {
            using (var dlg = new Form_Emissionskatalog())
            {
                dlg.SetControls(_carrier.ID, _carrier.Name, feld.Zeile.Kuerzel, true);
                dlg.ShowDialog(FindForm());

                if (dlg.Uebernommen != null)
                    _emissionen.KatalogwertUebernehmen(feld.Zeile, dlg.Uebernommen);
                if (dlg.ArtenGeaendert || dlg.WerteGeaendert)
                    _emissionen.NeuLadenMitBearbeitungsstand();

                ZeigeEmissionszeilen();
                SpiegelKernwerte();
            }
        }

        /// <summary>„Emissionsarten &amp; Katalog verwalten…": derselbe Dialog im
        /// Verwaltungsmodus. Danach wird die Feldliste neu gelesen (die Auswahl kann
        /// sich geändert haben, F5) — der Bearbeitungsstand bleibt erhalten.</summary>
        private void KatalogVerwalten()
        {
            using (var dlg = new Form_Emissionskatalog())
            {
                dlg.SetControls(_carrier.ID, _carrier.Name, null, false);
                dlg.ShowDialog(FindForm());
            }
            _emissionen.NeuLadenMitBearbeitungsstand();
            ZeigeEmissionszeilen();
            SpiegelKernwerte();
        }

        /// <summary>
        /// Schreibt den Emissionsstand (Etappe E3). Im Katalogkontext sind das die
        /// aktiven <c>emissionswert</c>-Zeilen samt Spiegel in die Altspalten; im
        /// Projektkontext bleibt der Zahlenweg beim Bestand
        /// (<see cref="SpeichereWerte"/>) und hier läuft nur der Modus mit.
        /// </summary>
        private void EmissionenSpeichern()
        {
            if (_emissionen == null || !_emissionen.Verfuegbar) return;
            try { _emissionen.Speichern(); }
            catch (Exception ex)
            {
                Console.WriteLine("Die Emissionswerte konnten nicht gespeichert werden: " +
                                  ex.Message);
            }
        }

        /// <summary>Zahlanzeige der Emissionsfelder — deutsche Dezimaltrennung wie
        /// bei den übrigen Zahlfeldern des Dialogs; leer heißt „nicht gepflegt".</summary>
        private static string EmissionsZahlText(double? wert)
        {
            return wert.HasValue
                ? wert.Value.ToString("0.####", CultureInfo.CurrentCulture)
                : "";
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
                // Ä-BK3: Die drei Preisspalten dürfen NULL sein. Seit die
                // Trägerzuordnung (EnergietraegerKatalogCtrl.InsProjekt) ihr
                // Versprechen „es gelten die Katalogwerte" auch schreibt, kommt NULL
                // hier regulär vor; der frühere nackte (decimal)-Cast warf dann
                // (gemessen: RuntimeBinderException „Cannot convert null to
                // 'decimal' because it is a non-nullable value type"). Rückfall ist derselbe
                // Katalogwert, den auch der else-Zweig ohne Projektzeile setzt —
                // gleiche Spalte, gleiche Einheit, gleiches Zielfeld.
                numArbeitspreis.Value = (decimal)(projectSettings.ArbeitspreisEurYear ?? _carrier.price_work);
                numGrundpreis.Value = (decimal)(projectSettings.GrundpreisEurYear ?? _carrier.price_base);
                numLeistungspreis.Value = (decimal)(projectSettings.LeistungspreisEurYear ?? _carrier.price_power);

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

                // Ä-BK3: ID_Umrechnung darf NULL sein — SpeichereWerte schreibt bei
                // leerer Auswahl selbst DBNull, und die Trägerzuordnung trägt seit
                // BK3 -1 ein, wenn es keine Identitätsregel gibt. Der dynamic-Aufruf
                // reichte NULL bisher in einen int-Parameter und warf
                // (RuntimeBinderException); -1 findet planmäßig keine Regel und lässt
                // die Vorwahl schlicht aus.
                int idUmrechnung = projectSettings.IDUmrechnung ?? -1;
                string project_conversion = GetTargetUnitByConversionId(idUmrechnung);
                var selectedUnit = _conversions.FirstOrDefault(c => c.ToUnitCode == project_conversion);

                if (selectedUnit != null)
                {
                    cmbUnit.SelectedItem = selectedUnit;
                    id_conversion = idUmrechnung;
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
            EmissionenSpeichern();   // ETAPPE E3 - NACH dem Bestandsweg (siehe dort)
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
                    new DbParam("@ap", Math.Round(currentPriceBase, 4)),
                    new DbParam("@gp", Math.Round(currentGroundPrice, 4)),
                    new DbParam("@lp", Math.Round(currentPowerPrice, 4)),
                    new DbParam("@hi", Math.Round(currentHiBase, 4)),
                    new DbParam("@hs", Math.Round(currentHsBase, 4)),
                    new DbParam("@co2", currentCO2),
                    new DbParam("@so2", currentSO2),
                    new DbParam("@nox", currentNOx),
                    new DbParam("@id", _carrier.ID));

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
                DbParam[] checkParams = {
                    new DbParam("@cid", _carrier.ID),
                    new DbParam("@prid", _projectId),
                    new DbParam("@date", DbParamTyp.Date) { Wert = chosenDate.Date } // .Date ignoriert Uhrzeit-Störfaktoren
                };

                int existingCount = Convert.ToInt32(DataRepository.ExecuteScalar(sqlCheck, checkParams));

                if (existingCount > 0)
                {
                    // Existiert bereits -> UPDATE
                    string sqlUpdateHistory = @"UPDATE energy_price 
                                                SET arbeitspreis = ?, heizwert = ?, grundpreis = ?, 
                                                    arbeitspreis_unit = ?, leistungspreis = ?
                                                WHERE carrier_id = ? AND id_projekt = ? AND valid_from = ?";

                    DataRepository.ExecuteSQL(sqlUpdateHistory, new DbParam[] {
                        new DbParam("@ap", Math.Round(currentPriceBase, 4)),
                        new DbParam("@hi", Math.Round(currentHiBase, 4)),
                        new DbParam("@gp", Math.Round(currentGroundPrice, 4)),
                        new DbParam("@au", lblBasisnheit.Text),
                        new DbParam("@lp", Math.Round(currentPowerPrice, 4)),
                        new DbParam("@cid", _carrier.ID),
                        new DbParam("@prid", _projectId),
                        new DbParam("@date", DbParamTyp.Date) { Wert = chosenDate.Date }
                    });
                }
                else
                {
                    // Existiert noch nicht -> INSERT
                    string sqlInsertHistory = @"INSERT INTO energy_price 
                                    (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, 
                                    valid_from, arbeitspreis_unit, leistungspreis) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                    DataRepository.ExecuteSQL(sqlInsertHistory, new DbParam[] {
	                    new DbParam("@cid", _carrier.ID),
	                    new DbParam("@prid", _projectId),
	                    new DbParam("@ap", Math.Round(currentPriceBase, 4)),
	                    new DbParam("@hi", Math.Round(currentHiBase, 4)),
	                    new DbParam("@gp", Math.Round(currentGroundPrice, 4)),
	                        new DbParam("@date", DbParamTyp.Date) { Wert = chosenDate.Date },
	                    new DbParam("@au", lblBasisnheit.Text),
	                    new DbParam("@lp", Math.Round(currentPowerPrice, 4))
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

            int rows = (int)DataRepository.ExecuteNonQuery(sqlUpsert, new DbParam[] {
                new DbParam("@p", currentPriceBase),
                new DbParam("@pl", currentPowerPrice),
                new DbParam("@hi", currentHiBase),
                new DbParam("@hs", currentHsBase),
                new DbParam("@b", currentGroundPrice),
                new DbParam("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                new DbParam("@co2", currentCO2),
                new DbParam("@so2", currentSO2),
                new DbParam("@nox", currentNOx),
                new DbParam("@pid", _projectId),
                new DbParam("@eid", _carrier.ID)
            });

            if (rows == 0)
            {
                string sqlInsert = @"INSERT INTO energy_Project_settings 
                                    (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, 
                                    custom_price_base, ID_Umrechnung, co2, so2, nox) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                DataRepository.ExecuteSQL(sqlInsert, new DbParam[] {
                    new DbParam("@pid", _projectId),
                    new DbParam("@eid", _carrier.ID),
                    new DbParam("@p", currentPriceBase),
                    new DbParam("@pl", currentPowerPrice),
                    new DbParam("@h", currentHiBase),
                    new DbParam("@hs", currentHsBase),
                    new DbParam("@b", currentGroundPrice),
                    new DbParam("@cid", currentConvID != -1 ? (object)currentConvID : DBNull.Value),
                    new DbParam("@co2", currentCO2),
                    new DbParam("@so2", currentSO2),
                    new DbParam("@nox", currentNOx)
                });
            }

            // AP4: Der Aufschlagsblock schreibt in dieselbe Zeile und deshalb ERST
            // JETZT - vor dem Upsert oben gäbe es beim ersten Speichern noch keine.
            if (_aufschlaege != null) _aufschlaege.Uebernehmen();

            // ETAPPE B2: derselbe Grund, dieselbe Zeile in energy_project_settings -
            // nur die andere Trägerfamilie. Der Block schreibt AUSSCHLIESSLICH die
            // Bestandteile; custom_price_work oben bleibt seine einzige Wahrheit.
            if (_bestandteile != null) _bestandteile.Uebernehmen();
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

            DbParam[] ps = { new DbParam("@id", Idbrennstoff) };
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

            DbParam[] ps = {
                new DbParam("@p", projectId),
                new DbParam("@c", carrierId)
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
                DbParam[] ps = {
                    new DbParam("@cid", conv.IDBrennstoff),
                    new DbParam("@fu", conv.FromUnit),
                    new DbParam("@tu", conv.ToUnitCode)
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
                HeaderText = "Grundpreis [€/a]",
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

            List<DbParam> parameters = new List<DbParam>();
            parameters.Add(new DbParam("@cid", carrierId));

            if (projectId.HasValue)
            {
                sql += " AND id_projekt = ?";
                parameters.Add(new DbParam("@pid", projectId.Value));
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