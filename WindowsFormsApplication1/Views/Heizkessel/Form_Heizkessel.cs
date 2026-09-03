using MathNet.Numerics.Optimization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// Der Dialog „Energietraeger Variante“ ist seit iZ5 eine Razor-Komponente
// (iU8, iU9-1). Bewusst NUR dieser eine Namensraum: EventCallback wird unten
// ausgeschrieben, damit sich Microsoft.AspNetCore.Components nicht mit
// System.Windows.Forms um Namen streitet.
using EPOS.UI.Dialoge.Kosten;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private HeizkesselStammCtrl heizkesselctrl = new HeizkesselStammCtrl();
        public List<WErzeugerModel> list_heizkesselmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.KESSEL_TYP;
        public int m_ID_Projekt = 0;
        int startindex = 100000;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;
        // Blockt cmbBrennstoffArt_SelectedIndexChanged waehrend des programmatischen
        // Befuellens, damit die Bindung nicht m.ID_Carrier ueberschreibt.
        private bool _updateCarrierCombo = false;

        public Form_Heizkessel()
        {
            InitializeComponent();
            InitKesselListe();
            listBox_Kessel_DB.Items.Clear();
            listBox_Kessel.Items.Clear();

            // Handler GENAU EINMAL abonnieren (nur echte Benutzerauswahl aendert ID_Carrier).
            cmbBrennstoffArt.SelectedIndexChanged += cmbBrennstoffArt_SelectedIndexChanged;
        }

        // Konfiguriert die Auswahl-ListView (Details, Spalten Name + ID). Der Steuerungsname
        // bleibt "listBox_Kessel" (jetzt ListView), damit die .resx-Eintraege weiter passen.
        private void InitKesselListe()
        {
            listBox_Kessel.View = View.Details;
            listBox_Kessel.FullRowSelect = true;
            listBox_Kessel.HeaderStyle = ColumnHeaderStyle.None;   // keine Spaltenueberschrift
            listBox_Kessel.MultiSelect = false;
            listBox_Kessel.Scrollable = true;
            if (listBox_Kessel.Columns.Count == 0)
            {
                // nur die Bezeichner-Spalte sichtbar (fuellt die Breite); die eindeutige
                // Zuordnung laeuft ueber ListViewItem.Tag, eine ID-Spalte ist nicht noetig.
                int w = listBox_Kessel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                if (w < 50) w = 200;
                listBox_Kessel.Columns.Add("", w);
            }
        }

        // Liefert das zur markierten Zeile gehoerende Modell (oder null).
        private WErzeugerModel GetSelectedKessel()
        {
            if (listBox_Kessel.SelectedItems.Count == 0) return null;
            return listBox_Kessel.SelectedItems[0].Tag as WErzeugerModel;
        }

        // Fuegt eine Zeile fuer ein Modell hinzu (Tag = Modell, Spalte ID = eindeutige Instanz-ID).
        private void AddKesselRow(WErzeugerModel m)
        {
            ListViewItem lvi = new ListViewItem(m.Bezeichner);
            lvi.Tag = m;
            listBox_Kessel.Items.Add(lvi);
            FitColumn();
        }

        // Spalte auf den laengsten Bezeichner anpassen; bei langen Namen entsteht so eine
        // horizontale Scrollbar, bei kurzen fuellt die Spalte mindestens die Breite.
        private void FitColumn()
        {
            if (listBox_Kessel.Columns.Count == 0) return;
            // Breite explizit aus der gemessenen Textbreite setzen (handle-unabhaengig, damit
            // die Spalte auch im Wizard vor dem Anzeigen breiter als der Client werden kann
            // -> horizontale Scrollbar bei langen Bezeichnern).
            int max = 0;
            foreach (ListViewItem it in listBox_Kessel.Items)
            {
                int wItem = TextRenderer.MeasureText(it.Text, listBox_Kessel.Font).Width;
                if (wItem > max) max = wItem;
            }
            int avail = listBox_Kessel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            int w = max + 24;
            if (w < avail) w = avail;
            listBox_Kessel.Columns[0].Width = w;
        }

        // Liest einen ganzzahligen Spaltenwert; probiert mehrere Spaltennamen
        // (z.B. "Ruecklauf" ASCII bzw. "Rücklauf" mit Umlaut).
        private static int IntCol(DataRow row, params string[] cols)
        {
            foreach (string c in cols)
                if (row.Table.Columns.Contains(c) && row[c] != DBNull.Value) return Convert.ToInt32(row[c]);
            return 0;
        }

        /// <summary>ETAPPE KD6 (§ 9): einmal gebaut (nicht im Wizard-Modus).</summary>
        private bool _kostenLeisteGebaut;

        /// <summary>ETAPPE KD6 (§ 9): Kosten-Aufrufe des Projekt-Anlagendialogs —
        /// Projekt und Träger werden zur KLICKZEIT aufgelöst (Delegaten), weil
        /// <c>SetControls</c> das Projekt erst nach dem Konstruktor setzt.</summary>
        private void KostenzugriffAnbringen()
        {
            var leiste = KostenKnoepfe.Leiste(this, DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
                () => m_ID_Projekt,
                () => KostenKnoepfe.TraegerDerKomponente(m_ID_Projekt, "ID_Kessel"));
            leiste.Dock = DockStyle.Bottom;
            Controls.Add(leiste);
            Height += 46;
        }

        public void SetControls(int IDProjekt, bool bWizard = false)
        {
            m_ID_Projekt = IDProjekt;
            if (!bWizard && !_kostenLeisteGebaut) { _kostenLeisteGebaut = true; KostenzugriffAnbringen(); }
            if (bWizard)
            {
                m_bWizard = true;
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_heizkesselmodel = wizardparent.list_werzmodel;
            }
            listBox_Kessel.Items.Clear();
            for (int i = 0; i < list_heizkesselmodel.Count; i++)
            {
                if (list_heizkesselmodel[i].ID_Type == WizardItemClass.KESSEL_TYP)
                {
                    AddKesselRow(list_heizkesselmodel[i]);
                }
            }
            if (listBox_Kessel.Items.Count > 0) listBox_Kessel.Items[0].Selected = true;
        }

        private void Form_Heizkessel_Load(object sender, EventArgs e)
        {
            heizkesselctrl.ReadAll();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);

            }

            comboBox_Brennstoffart.Items.Add("Alle");
            for (int i = 0; i < heizkesselctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoffart.Items.Add(heizkesselctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            comboBox_Leistung.Items.Add("bis 50 kW");
            comboBox_Leistung.Items.Add(">50 bis 200 kW");
            comboBox_Leistung.Items.Add(">200 bis 500 kW");
            comboBox_Leistung.Items.Add(">500 bis 1.000 kW");
            comboBox_Leistung.Items.Add("über 1.000 kW");
            comboBox_Leistung.Text = "Alle";
            comboBox_Brennstoffart.Text = "Alle";
        }

        private Form getWizardPage()
        {
            // P4: typisierte Erkennung ueber WizardParent.Aktiver. Die frueheren elf
            // Kopien suchten den Rahmen als Zeichenkette "WizardParent" in
            // Application.OpenForms; der Rahmen meldet sich jetzt selbst an.
            return WizardParent.Aktiver as Form;
        }

        private void btn_Kessel_Hinzu_Click(object sender, EventArgs e)
        {
            int nBrennstoff = 0;

            WizardParent wizardparent = (WizardParent)getWizardPage();

            if (listBox_Kessel_DB.Text == "") return;

            // Stamm-ID des ausgewaehlten Heizkessels ermitteln.
            int stammId = DataRepository.GetIdByName(HeizkesselStammCtrl.TABLE, "Bezeichner", listBox_Kessel_DB.Text);
            if (stammId <= 0)
            {
                MessageBox.Show("Der ausgewählte Heizkessel wurde in den Stammdaten nicht gefunden.");
                return;
            }

            WErzeugerModel model = new WErzeugerModel();
            model.ID = startindex++;
            model.ID_Projekt = m_ID_Projekt;
            model.ID_Type = m_nType;
            model.Bezeichner = listBox_Kessel_DB.Text;

            // Vorlauf/Ruecklauf aus dem Stamm-Datensatz vorbelegen -> fliessen als
            // Default in Tab_Energieanlagen (Vorlauf, Ruecklauf) beim Speichern.
            DataTable dtStamm = DataRepository.GetDataTable(
                "SELECT * FROM " + HeizkesselStammCtrl.TABLE + " WHERE ID = ?",
                new DbParam("@id", stammId));
            if (dtStamm != null && dtStamm.Rows.Count > 0)
            {
                DataRow sr = dtStamm.Rows[0];
                model.Vorlauf = IntCol(sr, "Vorlauf");
                model.Ruecklauf = IntCol(sr, "Ruecklauf", "Rücklauf");
                nBrennstoff = IntCol(sr, "Brennstoff");
            }

            // Punkt 2: Energieträgervariante ZUERST wählen/anlegen. Bricht der Nutzer den
            // Dialog ab oder schlägt das Anlegen fehl (carrierID <= 0), wird KEIN Kessel
            // hinzugefügt – kein verwaister Eintrag mit ID_Carrier = 0 und keine
            // Tab_Heizkessel-Projektkopie, die sonst zurückbliebe.
            int carrierID = 0;
            CreateNewEnergyCarrier(nBrennstoff, ref carrierID);
            if (carrierID <= 0) return;
            model.ID_Carrier = carrierID;

            // Analog zu BHKW: im direkten Projektmodus den Stammdatensatz sofort in die Projekt-Tabelle
            // kopieren (idempotent) und die PROJEKT-ID referenzieren. Im Wizard-Vorschaumodus nur die
            // Stamm-ID als Platzhalter; die eigentliche Kopie macht WizardCtrl.Add_WP_Waermeerzeuger beim Speichern.
            if (!m_bWizard && m_ID_Projekt > 0)
            {
                int projektId = new HeizkesselCtrl().CopyFromStamm(stammId, m_ID_Projekt);
                if (projektId <= 0)
                {
                    MessageBox.Show("Der Datensatz konnte nicht in das Projekt übernommen werden.");
                    return;
                }
                // WICHTIG: ID_Kessel referenziert die Projekt-Tabelle (Tab_Heizkessel), NICHT die Stammdaten.
                model.ID_Kessel = projektId;
            }
            else
            {
                model.ID_Kessel = stammId;
            }

            list_heizkesselmodel.Add(model);
            AddKesselRow(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_heizkesselmodel;
        }

        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        /// <summary>
        /// Waehlt oder legt die Energietraegervariante zum Brennstoff des Kessels
        /// an - Knopf "◀" (btn_Kessel_Hinzu).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Paket iU9-1 (03.09.2026).</b> Der Dialog ist seit dem Stichtag iZ5 die
        /// Razor-Komponente <c>EnergietraegerVarianteDialog</c> in <c>EPOS.UI</c>; die
        /// WinForms-Fassung <c>Form_Kosten_VarAuswahl</c> ist mit diesem Schritt
        /// GELOESCHT (Regel M1: keine zweite Fassung derselben Maske). Angezeigt wird
        /// die Komponente von der Huelle <see cref="BlazorDialogForm{TKomponente}"/> -
        /// genau wie in <c>Views\Kosten\Form_Kosten.cs</c>.
        /// </para>
        /// <para>
        /// <b>Befund 03.09.2026.</b> Die Umstellung in iU8-9 hing am Kosteneditor
        /// <c>Form_Kosten</c> - der ist seit KD6a aber kein Einstieg mehr. Die beiden
        /// ERREICHBAREN Aufrufer sind diese Maske und <c>Form_BHKWEing</c>; deshalb
        /// wurde die erste iU9-Welle vorgezogen.
        /// </para>
        /// <para>
        /// <b>Fuer diese Methode aendert sich nur die Herkunft der Werte.</b> Was der
        /// Anwender eingegeben hat, steht im Ergebnis-Record; die sechs daraus
        /// ABGELEITETEN Werte holt <c>EnergietraegerVarianteCtrl.Ergaenzen</c> mit
        /// denselben Abfragen, die der geloeschte Dialog beim Schliessen selbst
        /// ausfuehrte. Alles danach - Transaktion, Katalogsuche, INSERT, Preishistorie,
        /// Projektzuordnung - ist unveraendert.
        /// </para>
        /// <para>
        /// <b>Was entfaellt.</b> Die drei Vorabfragen auf <c>Bezeichner</c>,
        /// <c>ID_Kategorie</c> und <c>Gruppe</c> dienten allein dem alten Dialog:
        /// <c>m_szBrennstoff</c> war seine Vorwahl (jetzt <c>VorwahlId</c> - dieselbe
        /// Auswahl, nur ueber die Id statt ueber den Anzeigenamen), <c>m_KategorieID</c>
        /// und <c>m_szKategorie</c> engten seine beiden Listen ein. NACH dem Dialog hat
        /// keiner der drei Werte je eine Rolle gespielt; die Gruppe kommt hier wie im
        /// Kostendialog aus <c>Ergaenzen</c> (<c>GroupCode</c>) und richtet sich damit
        /// nach dem WIRKLICH gewaehlten Traeger. <c>bOhneVariante</c> war ein totes Feld
        /// (K6): beide Aufrufer setzten es auf seinen Vorgabewert <c>false</c>, die
        /// Maske selbst hat es nie gelesen.
        /// </para>
        /// <para>
        /// <b>Bewusste Abweichung.</b> Die Auswahlliste zeigt jetzt ALLE Energietraeger
        /// des Stamms, nicht nur die der Kategorie des vorgewaehlten Brennstoffs. Der
        /// angelegte Traeger bleibt trotzdem stimmig, weil <c>group_code</c>,
        /// <c>pricing_model</c>, <c>billing_unit</c>, Hi, Hs und die Umrechnung
        /// ausnahmslos aus dem gewaehlten Traeger abgeleitet werden.
        /// </para>
        /// </remarks>
        private string CreateNewEnergyCarrier(int nBrennstoff, ref int carrierId)
        {
            carrierId = 0;

            // Das Ergebnis kommt nicht als Rueckgabewert, sondern ueber den Rueckruf
            // der Komponente; die Huelle schliesst daraufhin das Fenster.
            EnergietraegerVarianteErgebnis ergebnis = null;
            BlazorDialogForm<EnergietraegerVarianteDialog> dlg = null;

            var parameter = new Dictionary<string, object>
            {
                // Die Komponente bleibt datenbankfrei: Sie bekommt die Liste fertig.
                ["Energietraeger"] = EnergietraegerVarianteCtrl.Energietraeger(EnergietraegerVarianteCtrl.KategorieZu(nBrennstoff)),   // nur die Kategorie der Komponente (Befund 03.09.2026)

                // nBrennstoff stammt aus dem Stammsatz und ist 0, wenn dort kein
                // Brennstoff hinterlegt ist. Dann gibt es keine Vorwahl und die
                // Komponente zeigt den ersten Eintrag; der alte Dialog lief in diesem
                // Fall in eine leere Liste und damit in einen Absturz.
                ["VorwahlId"] = nBrennstoff > 0 ? (int?)nBrennstoff : null,

                ["TitelText"] = MyResource.Resource.KAUSW_TITEL,
                ["LabelEnergietraeger"] = MyResource.Resource.KAUSW_LBL_ENERGIETRAEGER,
                ["LabelVariante"] = MyResource.Resource.KAUSW_LBL_VARIANTE,
                ["MeldungNameFehlt"] = MyResource.Resource.KAUSW_MSG_NAME_FEHLT,
                ["MeldungTraegerFehlt"] = MyResource.Resource.KAUSW_MSG_TRAEGER_FEHLT,
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["Geschlossen"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<EnergietraegerVarianteErgebnis>(this, e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null);
                    })
            };

            dlg = new BlazorDialogForm<EnergietraegerVarianteDialog>(
                MyResource.Resource.KAUSW_TITEL, new Size(460, 320), parameter);

            using (dlg)
            {
                if (dlg.ShowDialog() != DialogResult.OK || ergebnis == null) return "";

                // Die sechs abgeleiteten Werte - frueher FetchAdditionalData und
                // GetConvID im Dialog selbst, jetzt derselbe Weg im Kern.
                EnergietraegerDaten daten = EnergietraegerVarianteCtrl.Ergaenzen(ergebnis.BrennstoffId);

                // Default-Werte (reine Lesezugriffe) VOR der Transaktion ermitteln.
                double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", ergebnis.BrennstoffId));
                double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", ergebnis.BrennstoffId));
                double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", ergebnis.BrennstoffId));
                double default_co2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", ergebnis.BrennstoffId));
                double default_so2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", ergebnis.BrennstoffId));
                double default_nox = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", ergebnis.BrennstoffId));

                // Punkt 1: Katalog-Träger, Preishistorie und Projekt-Einstellungen in EINER
                // Transaktion schreiben. Schlägt ein Insert fehl, macht Rollback alles rückgängig
                // – kein halbfertiger Zustand (Träger/Preis ohne zugehörige Settings).
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        // 1) Katalog-Träger suchen; existiert er, wird er wiederverwendet.
                        carrierId = -1;
                        {
                            List<DbParam> ps = new List<DbParam>();
                            ps.Add(new DbParam("@name", ergebnis.VariantenName));
                            object existing = v.Skalar("SELECT id FROM energy_carrier WHERE name = ?", ps.ToArray());
                            if (existing != null && existing != DBNull.Value)
                                carrierId = Convert.ToInt32(existing);
                        }

                        // Katalog-Datensatz nur anlegen, wenn wirklich neu.
                        if (carrierId < 0)
                        {
                            List<DbParam> pTraeger = new List<DbParam>();
                            pTraeger.Add(new DbParam("@idB", ergebnis.BrennstoffId));
                            pTraeger.Add(new DbParam("@code", ergebnis.BrennstoffName));
                            pTraeger.Add(new DbParam("@name", ergebnis.VariantenName));
                            pTraeger.Add(new DbParam("@gc", daten.GroupCode));
                            pTraeger.Add(new DbParam("@pm", daten.Code));
                            pTraeger.Add(new DbParam("@unit", daten.BillingUnit));
                            pTraeger.Add(new DbParam("@shi", daten.Hi));
                            pTraeger.Add(new DbParam("@shs", daten.Hs));
                            pTraeger.Add(new DbParam("@defap", default_arbeitspreis));
                            pTraeger.Add(new DbParam("@defgp", default_grundpreis));
                            pTraeger.Add(new DbParam("@co2", default_co2));
                            pTraeger.Add(new DbParam("@so2", default_so2));
                            pTraeger.Add(new DbParam("@nox", default_nox));
                            pTraeger.Add(new DbParam("@active", DbParamTyp.Boolean) { Wert = true });
                            // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
                            // Verbindung des Vorgangs (frueher SELECT @@IDENTITY auf con/tx).
                            carrierId = v.EinfuegenUndId(
                                @"INSERT INTO energy_carrier
                                         (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                                          hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active)
                                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                pTraeger.ToArray());
                        }

                        if (carrierId <= 0)
                        {
                            v.Rollback();
                            carrierId = 0;
                            MessageBox.Show("Der Energieträger konnte nicht angelegt werden.");
                            return "";
                        }

                        // 1b) Wizard / kein echtes Projekt: nur der Katalog-Träger. energy_price
                        // und energy_Project_settings haben eine Beziehung auf Tab_Projekt.ID, die
                        // im Wizard noch nicht existiert -> die trägt WizardCtrl beim Speichern nach.
                        if (m_bWizard || m_ID_Projekt <= 0)
                        {
                            v.Commit();
                            MessageBox.Show("Energieträgervariante vorgemerkt. Die Preis- und Emissionssätze " +
                                            "werden beim Speichern des Projekts angelegt.");
                            return ergebnis.VariantenName;
                        }

                        // 2) Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln.
                        int vorhanden;
                        {
                            List<DbParam> ps = new List<DbParam>();
                            ps.Add(new DbParam("@pid", m_ID_Projekt));
                            ps.Add(new DbParam("@eid", carrierId));
                            vorhanden = Convert.ToInt32(v.Skalar("SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?", ps.ToArray()));
                        }
                        if (vorhanden > 0)
                        {
                            // Träger existierte bereits und ist zugeordnet: nichts Neues
                            // geschrieben -> Transaktion sauber beenden, carrierId bleibt gültig.
                            v.Commit();
                            MessageBox.Show($"Die Energieträgervariante '{ergebnis.VariantenName}' ist diesem Projekt bereits zugeordnet.");
                            return ergebnis.VariantenName;
                        }

                        // 3) Projektbezogene Sätze anlegen (Preis-Historie + Projekt-Einstellungen).
                        {
                            List<DbParam> ps = new List<DbParam>();
                            ps.Add(new DbParam("@cid", carrierId));
                            ps.Add(new DbParam("@prid", m_ID_Projekt));
                            ps.Add(new DbParam("@ap", Math.Round(default_arbeitspreis, 4)));
                            ps.Add(new DbParam("@hi", Math.Round(daten.Hi, 4)));
                            ps.Add(new DbParam("@gp", Math.Round(default_grundpreis, 4)));
                            ps.Add(new DbParam("@date", DbParamTyp.Date) { Wert = DateTime.Now });
                            ps.Add(new DbParam("@au", daten.BillingUnit));
                            ps.Add(new DbParam("@lp", Math.Round(default_leistungspreis, 4)));
                            v.Ausfuehren(@"INSERT INTO energy_price
                                     (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?)", ps.ToArray());
                        }

                        {
                            List<DbParam> ps = new List<DbParam>();
                            ps.Add(new DbParam("@pid", m_ID_Projekt));
                            ps.Add(new DbParam("@eid", carrierId));
                            ps.Add(new DbParam("@p", Math.Round(default_arbeitspreis, 4)));
                            ps.Add(new DbParam("@pl", Math.Round(default_leistungspreis, 4)));
                            ps.Add(new DbParam("@h", Math.Round(daten.Hi, 4)));
                            ps.Add(new DbParam("@hs", Math.Round(daten.Hs, 4)));
                            ps.Add(new DbParam("@b", Math.Round(default_grundpreis, 4)));
                            ps.Add(new DbParam("@convid", daten.ConvID));
                            ps.Add(new DbParam("@co2", default_co2));
                            ps.Add(new DbParam("@so2", default_so2));
                            ps.Add(new DbParam("@nox", default_nox));
                            v.Ausfuehren(@"INSERT INTO energy_Project_settings
                                     (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                                      custom_price_base, ID_Umrechnung, co2, so2, nox)
                                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", ps.ToArray());
                        }

                        v.Commit();
                        MessageBox.Show("Energieträgervariante erfolgreich angelegt.");
                        return ergebnis.VariantenName;
                    }
                    catch (Exception ex)
                    {
                        try { v.Rollback(); } catch { /* Rollback darf den Originalfehler nicht verdecken */ }
                        carrierId = 0;   // Signal an den Aufrufer: nichts angelegt -> kein Kessel hinzufügen
                        MessageBox.Show("Fehler beim Speichern: " + ex.Message);
                    }
                }
            }
            return "";
        }

        private void btn_Kessel_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel.SelectedItems.Count == 0) return;
            ListViewItem lvi = listBox_Kessel.SelectedItems[0];
            WErzeugerModel m = lvi.Tag as WErzeugerModel;
            if (m == null) return;
            string szName = m.Bezeichner;

            list_heizkesselmodel.Remove(m);
            listBox_Kessel.Items.Remove(lvi);
            FitColumn();
            if (m_bWizard) wizardparent.list_werzmodel = list_heizkesselmodel;

            // Projekt-Kopie nur entfernen, wenn keine weitere Auswahl mehr darauf verweist
            // (mehrere Instanzen desselben Kessels teilen sich eine Tab_Heizkessel-Kopie).
            bool nochReferenziert = false;
            foreach (WErzeugerModel it in list_heizkesselmodel)
                if (it.ID_Type == WizardItemClass.KESSEL_TYP && it.ID_Kessel == m.ID_Kessel) { nochReferenziert = true; break; }
            if (!m_bWizard && m_ID_Projekt > 0 && !nochReferenziert)
            {
                new HeizkesselCtrl().DeleteFromProjekt(szName, m_ID_Projekt);
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBox_Kessel_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            ApplySelectedKessel();
        }

        // Aktualisiert die Detailanzeige aus dem aktuell selektierten Kessel-Eintrag.
        private void ApplySelectedKessel()
        {
            WErzeugerModel m = GetSelectedKessel();
            if (m == null) return;

            cmbBrennstoffArt.Visible = true;
            label_BrennstoffArt.Visible = true;

            textBox_Vorlauf.Text = m.Vorlauf.ToString();
            textBox_Ruecklauf.Text = m.Ruecklauf.ToString();

            // cmbBrennstoffArt mit den Varianten der Carrier-Gruppe füllen und den
            // zugeordneten Träger vorwählen (analog Form_BHKWEing). Während des
            // programmatischen Befüllens den Handler per Flag blocken.
            _updateCarrierCombo = true;
            try
            {
                DataTable dtCar = DataRepository.GetDataTable(
                    "SELECT name, group_code FROM energy_carrier WHERE id = ?",
                    new DbParam("@id", m.ID_Carrier));
                if (dtCar != null && dtCar.Rows.Count > 0)
                {
                    string code = dtCar.Rows[0]["group_code"].ToString();

                    cmbBrennstoffArt.DataSource = DataRepository.GetDataTable(
                        "SELECT id, name FROM energy_carrier WHERE group_code = ? ORDER BY name",
                        new DbParam("@gc", code));
                    cmbBrennstoffArt.DisplayMember = "name";
                    cmbBrennstoffArt.ValueMember = "id";
                    cmbBrennstoffArt.SelectedValue = m.ID_Carrier;
                }
                else
                {
                    cmbBrennstoffArt.DataSource = null;
                }
            }
            finally
            {
                _updateCarrierCombo = false;
            }

            RecordSet rs = new RecordSet();
            rs.Open("select * from [Tab_Heizkessel] where ID=" + m.ID_Kessel);
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Bezeichner");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = heizkesselctrl.Brennstoffart[(int)rs.Read("Brennstoff") - 1].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            }
            rs.Close();
        }

        private void cmbBrennstoffArt_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Programmatisches Befüllen (ApplySelectedKessel) ignorieren.
            if (_updateCarrierCombo) return;

            WErzeugerModel m = GetSelectedKessel();
            if (m == null) return;

            object val = cmbBrennstoffArt.SelectedValue;
            if (val == null || val == DBNull.Value) return;
            int idcarrier_alt = m.ID_Carrier;
            
            m.ID_Carrier = Convert.ToInt32(val);

            string sqlUpdate =
                "UPDATE energy_Project_settings " +
                "SET ID_Energieträger = ? " +
                "WHERE ID_Projekt = ? AND ID_Energieträger = ?";

            DataRepository.ExecuteSQL(sqlUpdate, new DbParam[] {
                new DbParam("@neu", m.ID_Carrier),   // SET-Wert
                new DbParam("@pid", m_ID_Projekt),    // Filter Projekt
                new DbParam("@alt", idcarrier_alt)    // Filter bisheriger Träger
            });
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbBrennstoffArt.Visible = false;
            label_BrennstoffArt.Visible = false;    
            
            RecordSet rs = new RecordSet();

            rs.Open("select * from [Tab_Heizkessel_STAMM] where Bezeichner='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Bezeichner");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = heizkesselctrl.Brennstoffart[(int)rs.Read("Brennstoff") - 1].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            }
            rs.Close();
        }

        private void comboBox_Brennstoffart_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            // Vorbelegung "alle Leistungen" (gleiche Fehlerklasse wie B0-10 im Pufferspeicher):
            // ohne Treffer in der Literalkette blieb der Leistungsteil sonst leer und das
            // SQL endete in "... and  order by ...". Auslöser ist Freitext in der
            // editierbaren ComboBox; das Symptom war eine stumme Leerliste.
            szFilterLeistung = "Ptherm Like '%'";
            if (comboBox_Leistung.Text == "Alle" || comboBox_Leistung.Text == "") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "bis 50 kW") szFilterLeistung = "Ptherm <50";
            else if (comboBox_Leistung.Text == ">50 bis 200 kW") szFilterLeistung = "Ptherm >=50 and Ptherm <200";
            else if (comboBox_Leistung.Text == ">200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == ">500 bis 1.000 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <1000";
            else if (comboBox_Leistung.Text == "über 1.000 kW") szFilterLeistung = "Ptherm >=1000";


            if (comboBox_Brennstoffart.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoffart.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoffart.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoffart.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoffart.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoffart.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoffart.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoffart.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoffart.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoffart.Text == "Sonstige") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoffart.Text == "Alle") szFilter = "Brennstoff Like '%'";

            listBox_Kessel_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilterLeistung + " order by Bezeichner";
            else
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilter + " and " + szFilterLeistung + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Kessel_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_EDIT);

            if (listBox_Kessel_DB.Text == "") return;
            int index = listBox_Kessel_DB.SelectedIndex;
            frm.SetControls(listBox_Kessel_DB.Text, textBox_Kesselbeschreibung.Text);
            DialogResult ret = frm.ShowDialog();

            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szKessel;
                listBox_Kessel.SelectedItems.Clear();
                listBox_Kessel_DB.SelectedItems.Clear();
                heizkesselctrl.ReadAll();

                for (int i = 0; i < heizkesselctrl.rows; i++)
                {
                    listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);
                }
                listBox_Kessel_DB.SelectedIndex = -1;
                listBox_Kessel_DB.SelectedIndex = index;
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel_DB.SelectedIndex == -1) { MessageBox.Show("Bitte ein Modul auswählen!"); return; }

            if (!heizkesselctrl.Delete(listBox_Kessel_DB.Text)) return;

            listBox_Kessel_DB.Items.RemoveAt(listBox_Kessel_DB.SelectedIndex);
        }

        private void btn_Admin_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin();
            frm.ShowDialog();
            heizkesselctrl.ReadAll();
            listBox_Kessel_DB.Items.Clear();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);

            }
        }

        private void textBox_Ruecklauf_Validating(object sender, CancelEventArgs e)
        {
            // Nach fehlgeschlagener Prüfung zurück (früher lief Int32.Parse trotzdem
            // -> FormatException auf leerem/wiederhergestelltem Text). Zusätzlich TryParse.
            if (!Program.checkInt(textBox_Ruecklauf, textBox_Ruecklauf.Text)) { textBox_Ruecklauf.Undo(); return; }
            WErzeugerModel m = GetSelectedKessel();
            int val;
            if (m != null && m.ID_Type == WizardItemClass.KESSEL_TYP && Int32.TryParse(textBox_Ruecklauf.Text, out val))
                m.Ruecklauf = val;
        }

        private void textBox_Vorlauf_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Vorlauf, textBox_Vorlauf.Text)) { textBox_Vorlauf.Undo(); return; }
            WErzeugerModel m = GetSelectedKessel();
            int val;
            if (m != null && m.ID_Type == WizardItemClass.KESSEL_TYP && Int32.TryParse(textBox_Vorlauf.Text, out val))
                m.Vorlauf = val;
        }

        // ListView loest SelectedIndexChanged nicht aus, wenn sich der Index nicht aendert
        // (Klick auf das bereits selektierte bzw. einzige Item). Deshalb per Klick nachziehen.
        private void listBox_Kessel_MouseClick(object sender, MouseEventArgs e)
        {
            ApplySelectedKessel();
        }
    }
}
