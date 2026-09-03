using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_AdminStromspeicher : Form
    {
        private StromspeicherModel model = new StromspeicherModel();
        public List<WErzeugerModel> list_spmodel = new List<WErzeugerModel>();
        public bool m_bItemBearbeiten = false;
        private bool m_Neu = false;

        public Form_AdminStromspeicher()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            EinheitenBeschriftungKorrigieren();
            InitGeraetefelder();
        }

        /// <summary>
        /// Berichtigt die beiden falschen EINHEITEN der Bestandsbeschriftung
        /// (Abnahmebefund 1 zum ersten App-Start) — dieselbe Korrektur wie in
        /// <see cref="Form_Stromspeicher"/>, hier nur an anderen Steuerelementen:
        /// <c>label2</c> beschriftet das Kapazitätsfeld, <c>label8</c> trägt dessen
        /// Einheit (Designer: „kW", richtig ist <b>kWh</b>), <c>label11</c> die Einheit
        /// der Modulkosten (Designer: „€", richtig ist <b>€/kWh</b>, AP0-Entscheid vom
        /// 16.08.2026).
        ///
        /// <para>
        /// Die Wortmarke kommt aus <c>MyResource</c> (zweisprachig), das reine
        /// Einheitensymbol steht sprachneutral direkt am Label — genau die Aufteilung,
        /// die <see cref="InitGeraetefelder"/> für die AP3-Felder schon verwendet.
        /// </para>
        /// </summary>
        private void EinheitenBeschriftungKorrigieren()
        {
            label2.Text = MyResource.Resource.SP_LABEL_ENERGIE_KURZ;
            label8.Text = "kWh";
            label11.Text = "€/kWh";
        }

        public void SetControls(string projekt)
        {
            listBox_Stromspeicher.Items.Clear();
            for (int i = 0; i < list_spmodel.Count; i++)
            {
                listBox_Stromspeicher.Items.Add(list_spmodel[i].Bezeichner);
            }
            if (listBox_Stromspeicher.Items.Count > 0) listBox_Stromspeicher.SelectedIndex = 0;
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_Stromspeicher_Load(object sender, EventArgs e)
        {
            if (m_bItemBearbeiten) return;

            string sql = "SELECT Bezeichner FROM Tab_Stromspeicher_STAMM";
            DataTable dt = DataRepository.GetDataTable(sql);

            listBox_Stromspeicher.Items.Clear();
            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row["Bezeichner"] != DBNull.Value)
                    {
                        listBox_Stromspeicher.Items.Add(row["Bezeichner"].ToString());
                    }
                }
            }

            if (listBox_Stromspeicher.Items.Count > 0)
            {
                listBox_Stromspeicher.SelectedIndex = 0;
            }
        }

        private TextBox GetTextBox_Energie()
        {
            return textBox_Energie;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (textBox_Typ.Text == "")
            {
                MessageBox.Show("Eingaben überprüfen!");
                return;
            }

            // Zahlen erst hier pruefen (Folgepaket zu ab5bf32): das erste ungueltige
            // oder leere Feld meldet sprechend, bekommt den Fokus, und der Dialog
            // bleibt offen. Alle fuenf Werte sind double (StromspeicherModel), daher
            // durchgaengig ZahlPruefen. Leer meldet wie zuvor - vier Felder liefen in
            // die frueher hier stehende Leerpruefung, Modulkosten in eine Exception.
            double dEnergie, dLeistung, dDegradation, dLadezustand, dModulkosten;
            if (!Program.ZahlPruefen(textBox_Energie, "Energie", out dEnergie)) return;
            if (!Program.ZahlPruefen(textBox_Leistung, "Leistung", out dLeistung)) return;
            if (!Program.ZahlPruefen(textBox_Degradation, "Degradation", out dDegradation)) return;
            if (!Program.ZahlPruefen(textBox_Ladezustand, "Ladezustand", out dLadezustand)) return;
            if (!Program.ZahlPruefen(textBox_Modulkosten, "Modulkosten", out dModulkosten)) return;

            // AP3-Geraetefelder nach demselben Muster. Leer ist hier ERLAUBT und heisst
            // "nicht gepflegt" (Wert 0): der Katalog enthaelt Altdatensaetze, die diese
            // Groessen nie hatten, und ein Pflichtfeld wuerde deren Bearbeitung sperren.
            double dWirkungsgrad, dVerschleiss, dLeistungskosten, dInvestFix, dStandby;
            int nZyklen;
            if (!Program.ZahlPruefen(textBox_WirkungsgradRT, MyResource.Resource.SP_LABEL_WIRKUNGSGRAD_RT, out dWirkungsgrad, true)) return;
            if (!Program.GanzzahlPruefen(textBox_Zyklen, MyResource.Resource.SP_LABEL_ZYKLEN, out nZyklen, true)) return;
            if (!Program.ZahlPruefen(textBox_Verschleisskosten, MyResource.Resource.SP_LABEL_VERSCHLEISSKOSTEN, out dVerschleiss, true)) return;
            if (!Program.ZahlPruefen(textBox_Leistungskosten, MyResource.Resource.SP_LABEL_LEISTUNGSKOSTEN, out dLeistungskosten, true)) return;
            if (!Program.ZahlPruefen(textBox_InvestitionFix, MyResource.Resource.SP_LABEL_INVESTITION_FIX, out dInvestFix, true)) return;
            if (!Program.ZahlPruefen(textBox_Standby, MyResource.Resource.SP_LABEL_STANDBY, out dStandby, true)) return;

            try
            {
                model.m_Energie = dEnergie;
                model.m_Leistung = dLeistung;
                model.m_Degradation = dDegradation;
                model.m_Ladezustand = dLadezustand;
                model.m_Modulkosten = dModulkosten;

                model.m_WirkungsgradRT = dWirkungsgrad;
                model.m_ZyklenZugesichert = nZyklen;
                model.m_Verschleisskosten = dVerschleiss;
                model.m_Leistungskosten = dLeistungskosten;
                model.m_InvestitionFix = dInvestFix;
                model.m_StandbyVerbrauch = dStandby;

                if (m_Neu)
                {
                    StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                    sctrl.m_szBezeichner = textBox_Bezeichner.Text;
                    sctrl.m_szTyp = textBox_Typ.Text;
                    sctrl.m_Leistung = model.m_Leistung;
                    sctrl.m_Energie = model.m_Energie;
                    sctrl.m_Degradation = model.m_Degradation;
                    sctrl.m_Ladezustand = model.m_Ladezustand;
                    sctrl.m_Modulkosten = model.m_Modulkosten;
                    GeraetefelderUebernehmen(sctrl);

                    if (!sctrl.Insert()) { MessageBox.Show("Fehler beim Speichern der Daten!"); return; }

                    listBox_Stromspeicher.Items.Add(textBox_Bezeichner.Text);
                    listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
                    m_Neu = false;
                    MessageBox.Show("Daten gespeichert!");
                }
                else
                {
                    StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                    sctrl.m_szBezeichner = textBox_Bezeichner.Text;
                    sctrl.m_szTyp = textBox_Typ.Text;
                    sctrl.m_Leistung = model.m_Leistung;
                    sctrl.m_Energie = model.m_Energie;
                    sctrl.m_Degradation = model.m_Degradation;
                    sctrl.m_Ladezustand = model.m_Ladezustand;
                    sctrl.m_Modulkosten = model.m_Modulkosten;
                    GeraetefelderUebernehmen(sctrl);

                    if (!sctrl.Update(listBox_Stromspeicher.Text)) return;

                    int currentIndex = listBox_Stromspeicher.SelectedIndex;
                    if (currentIndex != -1)
                    {
                        listBox_Stromspeicher.Items[currentIndex] = textBox_Bezeichner.Text;
                    }

                    MessageBox.Show("Daten gespeichert!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern des Stromspeichers: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern der Daten!");
                m_Neu = false;
                InitControls();
                return;
            }
        }

        private void listBox_Stromspeicher_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Stromspeicher.Text)) return;

            textBox_Bezeichner.Text = listBox_Stromspeicher.Text;
            model.m_szBezeichner = textBox_Bezeichner.Text;

            string sql = "SELECT * FROM Tab_Stromspeicher_STAMM WHERE Bezeichner = ?";
            DbParam parameter = new DbParam("?", listBox_Stromspeicher.Text);
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];

                // Durchklicken des Katalogs darf nicht an NULL-Spalten scheitern:
                // stille Parser statt double.Parse, ein nicht lesbarer Wert zaehlt
                // wie 0 (so fuellt auch StromspeicherStammCtrl.Fill). Keine Meldung.
                double dWert;

                textBox_Energie.Text = row["Energie"].ToString();
                model.m_Energie = Program.ZahlParsen(textBox_Energie.Text, out dWert) ? dWert : 0.0;

                textBox_Leistung.Text = row["Leistung"].ToString();
                model.m_Leistung = Program.ZahlParsen(textBox_Leistung.Text, out dWert) ? dWert : 0.0; // Fehler korrigiert: war vorher model.m_Energie

                textBox_Typ.Text = row["Typ"] != DBNull.Value ? row["Typ"].ToString() : "";
                model.m_szTyp = textBox_Typ.Text;

                textBox_Degradation.Text = row["Degradation"].ToString();
                model.m_Degradation = Program.ZahlParsen(textBox_Degradation.Text, out dWert) ? dWert : 0.0;

                textBox_Ladezustand.Text = row["Ladezustand"].ToString();
                model.m_Ladezustand = Program.ZahlParsen(textBox_Ladezustand.Text, out dWert) ? dWert : 0.0;

                textBox_Modulkosten.Text = row["Modulkosten"].ToString();
                model.m_Modulkosten = Program.ZahlParsen(textBox_Modulkosten.Text, out dWert) ? dWert : 0.0;

                textBox_Bezeichner.Text = row["Bezeichner"].ToString();
                model.m_szBezeichner = textBox_Bezeichner.Text;

                GeraetefelderAnzeigen(row);
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            InitControls();
            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                m_Neu = true;
                textBox_Bezeichner.Text = szName;
                textBox_Typ.Text = "Lithium-Ionen";
                textBox_Degradation.Text = "0";
                textBox_Ladezustand.Text = "0";
                textBox_Modulkosten.Text = "0";
                textBox_Leistung.Text = "0";
                textBox_Energie.Text = "0";

                // AP3: fachliche Vorbelegung statt Nullen - eta_RT = 0,90 und
                // c_ver = 0,025 sind die Vorgaben aus Fachkonzept 5.2/5.4, und eine 0
                // beim Wirkungsgrad waere kein brauchbarer Startwert (die Engine weist
                // sie zurueck). Die uebrigen drei Investitionsanteile starten bei 0 -
                // das ist auch ihre fachliche Vorgabe.
                textBox_WirkungsgradRT.Text = ZahlAnzeigen(StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE);
                textBox_Zyklen.Text = "0";
                textBox_Verschleisskosten.Text = ZahlAnzeigen(C_VER_VORGABE);
                textBox_Leistungskosten.Text = "0";
                textBox_InvestitionFix.Text = "0";
                textBox_Standby.Text = "0";
            }
        }

        private void InitControls()
        {
            m_Neu = false;
            textBox_Bezeichner.Text = "";
            textBox_Typ.Text = "";
            textBox_Ladezustand.Text = "";
            textBox_Degradation.Text = "";
            textBox_Energie.Text = "";
            textBox_Leistung.Text = "";
            textBox_Modulkosten.Text = "";

            textBox_WirkungsgradRT.Text = "";
            textBox_Zyklen.Text = "";
            textBox_Verschleisskosten.Text = "";
            textBox_Leistungskosten.Text = "";
            textBox_InvestitionFix.Text = "";
            textBox_Standby.Text = "";
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_Stromspeicher.SelectedIndex == -1)
            {
                MessageBox.Show("Stromspeicher in Liste auswählen!");
                return;
            }

            DialogResult confirmResult = MessageBox.Show(
                $"Möchten Sie den Stromspeicher '{textBox_Bezeichner.Text}' wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButtons.YesNo
            );

            if (confirmResult == DialogResult.No) return;

            try
            {
                StromspeicherStammCtrl sctrl = new StromspeicherStammCtrl();
                if (!sctrl.Delete(textBox_Bezeichner.Text)) return;

                string geloeschterText = textBox_Bezeichner.Text;
                InitControls();

                listBox_Stromspeicher.Items.Remove(geloeschterText);

                if (listBox_Stromspeicher.Items.Count > 0)
                {
                    listBox_Stromspeicher.SelectedIndex = listBox_Stromspeicher.Items.Count - 1;
                }
            }
            catch (Exception ex)
            {
                // Fehler beim Datenbankzugriff abfangen (z.B. Fremdschlüssel-Einschränkungen)
                MessageBox.Show("Stromspeicher kann nicht gelöscht werden.\nEs besteht eine Projektzuordnung!");
                Console.WriteLine("Fehler beim Löschen des Stromspeichers: " + ex.Message);
            }
        }

        private void textBox_Typ_Validating(object sender, CancelEventArgs e)
        {
            if (textBox_Typ.Text == "") { MessageBox.Show("Eingabe überprüfen!"); }
        }

        // Validating faerbt nur noch (Folgepaket zu ab5bf32): kein modales Melden und
        // kein Undo() mehr beim Verlassen des Feldes - gemeldet wird erst am
        // Speichern-Knopf. Gefaerbt wird nach den Zahlregeln, weil alle vier Werte
        // als double gespeichert werden (StromspeicherModel).
        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Energie_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Ladezustand_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void textBox_Degradation_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        // =====================================================================
        // AP3 - Gerätetechnik (Fachkonzept Stromspeicher 5.1)
        //
        // Die sechs neuen Felder entstehen HIER IM CODE und nicht im Designer.
        // Grund: Das Formular legt jede Position, Größe und Beschriftung in
        // Form_AdminStromspeicher.resx ab (durchgängig resources.ApplyResources).
        // Neue Steuerelemente dort einzutragen hieße, die Designer- und
        // Ressourcendateien von Hand zu schreiben - genau das, was CLAUDE.md
        // ausschließt. Der Code-Weg ist außerdem das Muster, dem die jüngeren
        // Masken des Projekts folgen (Form_Quellprofil, ErzeugerKarte,
        // SpeicherKarte: vollständig programmatisch).
        //
        // Layout: zweite Spalte rechts neben den Bestandsfeldern, deren Raster
        // (Label bei x = 240, Feld bei x = 336, Einheit bei x = 451, Zeilenhöhe
        // 32 px) unverändert übernommen wird.
        //
        // Texte über MyResource.Resource.* in beiden Sprachen; Einheitensymbole
        // ohne Wortanteil (-, EUR/kW, EUR, W) stehen sprachneutral direkt am
        // Label, wie schon bei den Bestandseinheiten "kW" und "%".
        // =====================================================================

        /// <summary>Vorgabe der Zyklus-Verschleißkosten c_ver [€/(kWh·Zyklus)] (Fachkonzept 5.4).</summary>
        private const double C_VER_VORGABE = 0.025;

        private const int SPALTE_LABEL = 620;
        private const int SPALTE_FELD = 800;
        private const int SPALTE_EINHEIT = 916;
        private const int ZEILE_ERSTE = 50;
        private const int ZEILE_HOEHE = 32;
        private const int FELD_BREITE = 110;

        private TextBox textBox_WirkungsgradRT;
        private TextBox textBox_Zyklen;
        private TextBox textBox_Verschleisskosten;
        private TextBox textBox_Leistungskosten;
        private TextBox textBox_InvestitionFix;
        private TextBox textBox_Standby;

        private void InitGeraetefelder()
        {
            Label kopf = new Label();
            kopf.Text = MyResource.Resource.SP_GRUPPE_GERAETETECHNIK;
            kopf.Location = new Point(SPALTE_LABEL, ZEILE_ERSTE - 28);
            kopf.AutoSize = true;
            kopf.Font = new Font(Font, FontStyle.Bold);
            Controls.Add(kopf);

            int zeile = ZEILE_ERSTE;
            textBox_WirkungsgradRT = FeldAnlegen(MyResource.Resource.SP_LABEL_WIRKUNGSGRAD_RT, "-", zeile, false);
            zeile += ZEILE_HOEHE;
            textBox_Zyklen = FeldAnlegen(MyResource.Resource.SP_LABEL_ZYKLEN, "-", zeile, true);
            zeile += ZEILE_HOEHE;
            textBox_Verschleisskosten = FeldAnlegen(MyResource.Resource.SP_LABEL_VERSCHLEISSKOSTEN,
                                                    MyResource.Resource.SP_EINHEIT_ZYKLUSKOSTEN, zeile, false);
            zeile += ZEILE_HOEHE;
            textBox_Leistungskosten = FeldAnlegen(MyResource.Resource.SP_LABEL_LEISTUNGSKOSTEN, "€/kW", zeile, false);
            zeile += ZEILE_HOEHE;
            textBox_InvestitionFix = FeldAnlegen(MyResource.Resource.SP_LABEL_INVESTITION_FIX, "€", zeile, false);
            zeile += ZEILE_HOEHE;
            textBox_Standby = FeldAnlegen(MyResource.Resource.SP_LABEL_STANDBY, "W", zeile, false);

            // Das Formular ist im Designer 904 px breit; die zweite Spalte braucht mehr.
            if (ClientSize.Width < SPALTE_EINHEIT + 120)
                ClientSize = new Size(SPALTE_EINHEIT + 120, ClientSize.Height);
        }

        /// <summary>
        /// Legt Beschriftung, Eingabefeld und Einheit einer Zeile an. Die Prüfung
        /// folgt dem Bestandsmuster dieser Maske: Validating färbt nur
        /// (<see cref="Program.ZahlFaerben"/> bzw. <see cref="Program.GanzzahlFaerben"/>),
        /// gemeldet wird erst am Speichern-Knopf.
        /// </summary>
        private TextBox FeldAnlegen(string beschriftung, string einheit, int oben, bool ganzzahl)
        {
            Label lbl = new Label();
            lbl.Text = beschriftung;
            lbl.Location = new Point(SPALTE_LABEL, oben + 4);
            lbl.AutoSize = true;
            Controls.Add(lbl);

            TextBox tb = new TextBox();
            tb.Location = new Point(SPALTE_FELD, oben);
            tb.Size = new Size(FELD_BREITE, 25);
            if (ganzzahl) tb.Validating += (s, e) => Program.GanzzahlFaerben(s);
            else tb.Validating += (s, e) => Program.ZahlFaerben(s);
            Controls.Add(tb);

            Label lblEinheit = new Label();
            lblEinheit.Text = einheit;
            lblEinheit.Location = new Point(SPALTE_EINHEIT, oben + 4);
            lblEinheit.AutoSize = true;
            Controls.Add(lblEinheit);

            return tb;
        }

        /// <summary>
        /// Zeigt die Gerätefelder eines Katalogsatzes an. Wie bei den Bestandsfeldern
        /// darf das Durchklicken nicht an fehlenden Spalten oder NULL scheitern: Auf
        /// einer Datenbank vor Migrationsschritt 11 gibt es die Spalten nicht, dann
        /// bleibt das Feld leer.
        /// </summary>
        private void GeraetefelderAnzeigen(DataRow row)
        {
            textBox_WirkungsgradRT.Text = Spaltentext(row, "Wirkungsgrad_RT");
            textBox_Zyklen.Text = Spaltentext(row, "Zyklen_Zugesichert");
            textBox_Verschleisskosten.Text = Spaltentext(row, "Verschleisskosten");
            textBox_Leistungskosten.Text = Spaltentext(row, "Leistungskosten");
            textBox_InvestitionFix.Text = Spaltentext(row, "Investition_Fix");
            textBox_Standby.Text = Spaltentext(row, "Standby_Verbrauch");
        }

        private void GeraetefelderUebernehmen(StromspeicherStammCtrl sctrl)
        {
            sctrl.m_WirkungsgradRT = model.m_WirkungsgradRT;
            sctrl.m_ZyklenZugesichert = model.m_ZyklenZugesichert;
            sctrl.m_Verschleisskosten = model.m_Verschleisskosten;
            sctrl.m_Leistungskosten = model.m_Leistungskosten;
            sctrl.m_InvestitionFix = model.m_InvestitionFix;
            sctrl.m_StandbyVerbrauch = model.m_StandbyVerbrauch;
        }

        private static string Spaltentext(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return "";
            return row[spalte].ToString();
        }

        /// <summary>
        /// Vorbelegungen für die Anzeige: in der Kultur des Anwenders, damit die
        /// Zahl so aussieht wie eine selbst getippte (Fachkonzept 8.5 - UI in
        /// CurrentCulture, Datei und Datenbank invariant).
        /// </summary>
        private static string ZahlAnzeigen(double wert)
        {
            return wert.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
