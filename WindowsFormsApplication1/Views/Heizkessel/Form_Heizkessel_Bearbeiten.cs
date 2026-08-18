using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_Bearbeiten : BaseForm
    {
        public const int MODE_EDIT = 0;
        public const int MODE_NEU = 1;
        public string m_szKessel = "";
        private int m_mode = MODE_EDIT;

        // Beim Knopfdruck geprüft (EingabenPruefen) und von InitDatensatzUpdate
        // unverändert ins Modell übernommen - so kommt "12.5" wie "12,5" als 12,5 an.
        private double m_dPtherm, m_dWirkungsgradGas, m_dWirkungsgradOel, m_dBBVerlust;
        private double m_dInvestitionskosten, m_dNutzungsdauer, m_dRaumbedarf;
        private double m_dNOx, m_dCO2, m_dCO, m_dSO2, m_dStaub;
        private double m_dWartungskosten;
        private int m_nVorlauf, m_nRuecklauf;

        // --- Wartungskosten: zur Laufzeit aufgebaut, siehe WartungsfeldAufbauen() ---
        private TextBox tb_Wartungskosten;
        private ComboBox cb_WartungEinheit;

        public Form_Heizkessel_Bearbeiten(int mode)
        {
            InitializeComponent();
            m_mode = mode;

            // Vorsorge vor dem ersten Lese-/Schreibzugriff: auf einer nicht migrierten
            // Datenbank fehlt Wartungskosten_Einheit noch (Migrationsschritt 15).
            HeizkesselStammCtrl.StelleSpaltenSicher();
            WartungsfeldAufbauen();

            if (mode == MODE_EDIT)
            {
                btn_Speichern.Enabled = false;
                btn_Speichern_Unter.Enabled = true;
                btn_Ueberschreiben.Enabled = true;
            }
            else
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false;

                textBox_Beschreibung.Text = "";
                textBox_Hersteller.Text = "";
                tb_th_Leistung.Text = "0";
                tb_Wirkungsgrad.Text = "0.94";
                tb_Wirkungsgrad_Öl.Text = "0";
                tb_B_Verlust.Text = "0";
                tb_Investitionskosten.Text = "0";
                tb_Wartungskosten.Text = "0";
                tb_Nutzungsdauer.Text = "0";
                tb_Raumbedarf.Text = "0";
                tb_NOx.Text = "0";
                tb_CO2.Text = "0";
                tb_CO.Text = "0";
                tb_SO2.Text = "0";
                tb_Staub.Text = "0";
                checkBox_Brennwert.Checked = false;
            }

            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();
            comboBox_Brennstoff.DataSource = ctrl.Brennstoffart;
        }

        /// <summary>
        /// Ergänzt die Kostenrubrik („Eingabedaten zur Berechnung der Kosten") um
        /// Wartungskosten und ihre Bezugsgröße — Umsetzung der Nutzerentscheidung vom
        /// 18.08.2026, Punkt 1: Die Einheit ist wählbar statt fest verdrahtet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum dieser Dialog.</b> Er ist der EINZIGE Eingabeweg für Kesseldaten —
        /// sowohl der Katalogbrowser <c>Form_Heizkessel_Admin</c> als auch der
        /// Projektdialog <c>Form_Heizkessel</c> öffnen für „Bearbeiten" dieses Formular;
        /// die Kostenfelder <c>Investitionskosten</c>, <c>Raumbedarf</c> und
        /// <c>Nutzungsdauer</c> stehen bereits hier. Damit ist er das Gegenstück zu
        /// <c>Form_DBBHKW</c>, wo die BHKW-Wartungskosten mit dem Einheitensuffix
        /// „€ / kWhel" sitzen. In die Projektkopie <c>Tab_Heizkessel</c> gelangen die
        /// Werte auf demselben Weg wie alle übrigen: über
        /// <c>HeizkesselCtrl.CopyFromStamm</c>.
        /// </para>
        /// <para>
        /// <b>Warum zur Laufzeit statt im Designer.</b> Projektregel: Designer- und
        /// <c>.resx</c>-Dateien werden nicht von Hand editiert. Der WinForms-Designer
        /// scheidet hier praktisch aus, weil dieses Formular seine Koordinaten in
        /// <c>Form_Heizkessel_Bearbeiten.resx</c> UND in
        /// <c>Form_Heizkessel_Bearbeiten.en-US.resx</c> führt — ein von Hand ergänztes
        /// Control müsste in beiden stehen, sonst springt es beim Sprachwechsel. Denselben
        /// Weg gehen die neueren Masken dieser Etappe (<c>Form_PlanwertUebernahme</c> kommt
        /// ganz ohne Designer-Datei aus, <c>ucKostenItem</c> hebt seine Betragsgrenze
        /// programmatisch an).
        /// </para>
        /// <para>
        /// <b>Maße relativ statt absolut.</b> Alle Positionen leiten sich aus den bereits
        /// vorhandenen Controls der Rubrik ab. Damit stimmt das Ergebnis unabhängig davon,
        /// ob und wie stark <c>AutoScaleMode.Font</c> das Formular gestreckt hat — feste
        /// Pixelwerte aus der Designer-Datei wären nur bei der Entwurfsauflösung richtig.
        /// Die Rubrik wächst dabei nach RECHTS in den freien Bereich; wie weit, sagt
        /// <see cref="FreieBreite"/>, statt eine Zahl aus der Designer-Datei abzuschreiben.
        /// Nach unten wächst sie NICHT — dort stehen nur wenige Pixel bis
        /// <c>groupBox4</c> zur Verfügung, deshalb liegt die Einheitenauswahl auf der
        /// dritten vorhandenen Zeile statt auf einer vierten neuen.
        /// </para>
        /// </remarks>
        private void WartungsfeldAufbauen()
        {
            if (groupBox3 == null || tb_Investitionskosten == null) return;

            // Rechte Spalte hinter dem Einheitenzeichen der linken Spalte beginnen.
            int spalte = (Label17 != null ? Label17.Right : tb_Investitionskosten.Right) + 20;
            int feldX = spalte + 100;
            int zeile1 = tb_Investitionskosten.Top;
            int zeile2 = (tb_Raumbedarf != null ? tb_Raumbedarf.Top : zeile1 + 27);
            int zeile3 = (tb_Nutzungsdauer != null ? tb_Nutzungsdauer.Top : zeile2 + 27);

            // Wie breit die Rubrik höchstens werden darf, ohne den rechten Nachbarn
            // zu überdecken.
            int maxRechts = FreieBreite() - 12;

            Label lblWartung = new Label
            {
                AutoSize = true,
                Text = MyResource.Resource.KESSEL_WARTUNG_LBL + ":",
                Location = new Point(spalte, zeile1 + 2)
            };

            tb_Wartungskosten = new TextBox
            {
                Name = "tb_Wartungskosten",
                Location = new Point(feldX, zeile1),
                Size = tb_Investitionskosten.Size,
                TabIndex = tb_Investitionskosten.TabIndex + 1
            };
            tb_Wartungskosten.TextChanged += tb_Wartungskosten_TextChanged;

            Label lblEinheit = new Label
            {
                AutoSize = true,
                Text = MyResource.Resource.KESSEL_WARTUNG_EINHEIT_LBL + ":",
                Location = new Point(spalte, zeile2 + 2)
            };

            // Die Auswahl steht UNTER ihrer Beschriftung und nutzt die Spalte in voller
            // Breite: neben der Beschriftung blieben je nach Schriftgröße unter 120 Pixel
            // übrig, in denen „€/a Jahresbetrag" abgeschnitten würde.
            int breite = Math.Min(220, maxRechts - spalte);
            cb_WartungEinheit = new ComboBox
            {
                Name = "cb_WartungEinheit",
                Location = new Point(spalte, zeile3),
                Width = Math.Max(100, breite),
                DropDownWidth = 240,
                DropDownStyle = ComboBoxStyle.DropDownList,
                TabIndex = tb_Investitionskosten.TabIndex + 2
            };

            // Der STEUERWERT jedes Eintrags ist der sprachneutrale Schlüssel, angezeigt
            // wird der lokalisierte Name — kein Anzeigetext ist je Steuerwert
            // (Drei-Schichten-Regel, Konzept 13.6).
            foreach (string s in TechnikPlanwertCtrl.WARTUNG_SCHLUESSEL)
                cb_WartungEinheit.Items.Add(new EinheitItem(s));
            cb_WartungEinheit.SelectedIndex = 0;

            groupBox3.Controls.Add(lblWartung);
            groupBox3.Controls.Add(tb_Wartungskosten);
            groupBox3.Controls.Add(lblEinheit);
            groupBox3.Controls.Add(cb_WartungEinheit);

            // Rubrik so weit verbreitern, wie die neue Spalte es verlangt - aber nie
            // über den freien Bereich hinaus.
            int noetig = Math.Max(tb_Wartungskosten.Right, cb_WartungEinheit.Right) + 12;
            groupBox3.Width = Math.Max(groupBox3.Width, Math.Min(noetig, maxRechts + 12));
        }

        /// <summary>
        /// Die größte lokale Breite, die <c>groupBox3</c> annehmen darf, ohne einen
        /// rechts davon liegenden Nachbarn zu überdecken.
        /// </summary>
        /// <remarks>
        /// Ermittelt aus den tatsächlichen Geschwistern des Formulars statt aus einer
        /// abgeschriebenen Koordinate: Die Rubriken liegen im Designer nebeneinander
        /// (<c>groupBox5</c> beginnt rechts von <c>groupBox3</c>), und ihre Positionen
        /// stehen in zwei <c>.resx</c>-Dateien, die sich beim Sprachwechsel
        /// unterscheiden dürfen. Eine feste Zahl im Code wäre in genau einer Sprache und
        /// bei genau einer Schriftgröße richtig.
        /// </remarks>
        private int FreieBreite()
        {
            int grenze = ClientSize.Width - 12;

            foreach (Control c in Controls)
            {
                if (ReferenceEquals(c, groupBox3)) continue;
                if (c.Left < groupBox3.Right) continue;                  // links davon oder darunter
                if (c.Bottom <= groupBox3.Top || c.Top >= groupBox3.Bottom) continue;  // andere Zeile
                if (c.Left - 8 < grenze) grenze = c.Left - 8;
            }

            return grenze - groupBox3.Left;
        }

        /// <summary>
        /// Ein Eintrag der Einheitenauswahl: trägt den sprachneutralen Steuerwert und
        /// zeigt den lokalisierten Namen. Bewusst ein eigener Typ statt des
        /// <c>Format</c>-Ereignisses von <see cref="ListControl"/> — das setzt
        /// <c>FormattingEnabled</c> voraus und feuert nur über einen Umweg, den ein
        /// Kopfrechner beim Lesen nicht nachvollzieht.
        /// </summary>
        private sealed class EinheitItem
        {
            public readonly string Schluessel;
            public EinheitItem(string schluessel) { Schluessel = schluessel; }
            public override string ToString() { return TechnikPlanwertCtrl.WartungName(Schluessel); }
        }

        /// <summary>Setzt die Auswahl auf den gespeicherten Persistenzwert.</summary>
        private void EinheitWaehlen(string dbWert)
        {
            if (cb_WartungEinheit == null) return;

            string gesucht = TechnikPlanwertCtrl.WartungSchluessel(dbWert);
            for (int i = 0; i < cb_WartungEinheit.Items.Count; i++)
            {
                EinheitItem e = cb_WartungEinheit.Items[i] as EinheitItem;
                if (e != null && string.Equals(e.Schluessel, gesucht, StringComparison.Ordinal))
                { cb_WartungEinheit.SelectedIndex = i; return; }
            }
            cb_WartungEinheit.SelectedIndex = 0;
        }

        /// <summary>Gespeicherter Persistenzwert der aktuellen Auswahl.</summary>
        private string GewaehlteEinheit()
        {
            EinheitItem e = (cb_WartungEinheit != null)
                ? cb_WartungEinheit.SelectedItem as EinheitItem : null;
            return TechnikPlanwertCtrl.WartungDbWert(e != null ? e.Schluessel : null);
        }

        public void SetControls(string szName, string szBeschreibung)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = szName;
            m_szKessel = szName;
            textBox_Beschreibung.Text = szBeschreibung;

            rs.Open("select * from [Tab_Heizkessel_STAMM] where Bezeichner='" + szName + "'");
            if (!rs.Next()) { rs.Close(); return; }

            textBox_Hersteller.Text = rs.GetString("Firma");
            tb_th_Leistung.Text = rs.Read("Ptherm").ToString();
            tb_Wirkungsgrad.Text = rs.Read("Wirkungsgrad_Gas").ToString();
            tb_Wirkungsgrad_Öl.Text = rs.Read("Wirkungsgrad_Öl").ToString();
            tb_B_Verlust.Text = ((double)rs.Read("Betriebsbereitschaftverlust")).ToString("F2");
            tb_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
            tb_Nutzungsdauer.Text = rs.Read("Nutzungsdauer").ToString();
            tb_Raumbedarf.Text = rs.Read("Raumbedarf").ToString();
            tb_NOx.Text = rs.Read("NOx").ToString();
            tb_CO2.Text = rs.Read("CO2").ToString();
            tb_CO.Text = rs.Read("CO").ToString();
            tb_SO2.Text = rs.Read("SO2").ToString();
            tb_Staub.Text = rs.Read("Staub").ToString();
            checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            textBox_Vorlauf.Text = rs.Read("Vorlauf").ToString();
            textBox_Ruecklauf.Text = rs.Read("Ruecklauf").ToString();

            if (rs.Read("Brennstoff") != DBNull.Value)
            {
                int brennstoff = (int)rs.Read("Brennstoff");
                comboBox_Brennstoff.SelectedIndex = brennstoff >= 1 ? brennstoff - 1 : 1;
            }
            rs.Close();

            // Wartungskosten bewusst NICHT über das RecordSet: dessen Read() kennt keine
            // Spaltenprüfung, und Wartungskosten_Einheit fehlt auf einer nicht migrierten
            // Datenbank. Der Controller liest beides mit Rückfallebene.
            HeizkesselStammCtrl katalog = new HeizkesselStammCtrl();
            katalog.ReadSingle(szName);
            tb_Wartungskosten.Text = katalog.Wartungskosten.ToString("F2");
            EinheitWaehlen(katalog.Wartungskosten_Einheit);
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            // Erst prüfen, dann schreiben: bei ungültiger Eingabe bleibt der Dialog offen
            if (!EingabenPruefen()) return;

            HeizkesselModel model = new HeizkesselModel();
            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();

            try
            {
                InitDatensatzUpdate(ctrl);

                if (ctrl.Update())
                {
                    MessageBox.Show("Datensatz gespeichert");
                }
                else
                {
                    MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Überschreiben des Datensatzes!");
            }
        }

        public bool Insert(HeizkesselModel model)
        {
            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();
            if (ctrl.Exists(model.Name)) return false;

            ctrl.Name = model.Name;
            ctrl.Beschreibung = model.Beschreibung;
            ctrl.Firma = model.Firma;
            ctrl.Ptherm = model.Ptherm;
            ctrl.Brennstoff = model.Brennstoff;
            ctrl.Wirkungsgrad_Gas = model.Wirkungsgrad_Gas;
            ctrl.Wirkungsgrad_Oel = model.Wirkungsgrad_Oel;
            ctrl.Investitionskosten = model.Investitionskosten;
            ctrl.Raumbedarf = model.Raumbedarf;
            ctrl.Wartungskosten = model.Wartungskosten;
            ctrl.Wartungskosten_Einheit = model.Wartungskosten_Einheit;
            ctrl.Nutzungsdauer = model.Nutzungsdauer;
            ctrl.CO2 = model.CO2;
            ctrl.SO2 = model.SO2;
            ctrl.NOx = model.NOx;
            ctrl.CO = model.CO;
            ctrl.Staub = model.Staub;
            ctrl.Betriebsbereitschaftverlust = model.Betriebsbereitschaftverlust;
            ctrl.Brennwert = model.Brennwert;
            ctrl.Vorlauf = model.Vorlauf;
            ctrl.Ruecklauf = model.Ruecklauf;

            return ctrl.Insert();
        }

        // Folgepaket zu ab5bf32: Die TextChanged-Handler färben nur noch. Gemeldet wird
        // erst beim Speichern (EingabenPruefen), damit keine Zwischeneingabe modal stört
        // und das früher hier stehende Undo() nicht mehr zwischen Fehleingabe und
        // Leerstand pendeln kann. Das Auffüllen leerer Felder mit "0" entfällt; leer
        // gilt beim Speichern weiterhin als 0.
        private void tb_th_Leistung_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Wirkungsgrad_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Wirkungsgrad_Öl_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_B_Verlust_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Investitionskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Wartungskosten_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Raumbedarf_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Nutzungsdauer_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_CO2_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_SO2_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_NOx_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_CO_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void tb_Staub_TextChanged(object sender, EventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

        private void btn_CO2_Click(object sender, EventArgs e)
        {
            // Wir holen uns den Namen aus der Liste der BrennstoffCtrl
            string name = comboBox_Brennstoff.Text;

            // Logik für CO2-Werte basierend auf dem Namen
            if (name.ToUpper().Contains("ÖL"))
            {
                tb_CO2.Text = "290880";
            }
            else if (name.ToUpper().Contains("GAS") && !name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "201600";
            }
            else if (name.Contains("Flüssiggas"))
            {
                tb_CO2.Text = "238680";
            }
            else tb_CO2.Text = "0";
        }

        /// <summary>
        /// Prüft alle Zahlenfelder beim Knopfdruck (Folgepaket zu ab5bf32): sprechende
        /// Meldung, Fokus ins Feld, Dialog bleibt offen. Leer gilt wie bisher als 0 -
        /// früher füllte der TextChanged leere Felder sofort mit "0" auf.
        /// </summary>
        private bool EingabenPruefen()
        {
            if (!Program.ZahlPruefen(tb_th_Leistung, "Thermische Leistung", out m_dPtherm, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Wirkungsgrad, "Wirkungsgrad Gas, Biogas, Holz und Sonstiges", out m_dWirkungsgradGas, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Wirkungsgrad_Öl, "Wirkungsgrad Öl", out m_dWirkungsgradOel, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_B_Verlust, "Betriebsbereitschaftsverluste", out m_dBBVerlust, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Investitionskosten, "Investitionskosten", out m_dInvestitionskosten, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Wartungskosten, MyResource.Resource.KESSEL_WARTUNG_LBL, out m_dWartungskosten, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Raumbedarf, "Raumbedarf", out m_dRaumbedarf, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Nutzungsdauer, "Nutzungsdauer", out m_dNutzungsdauer, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_CO2, "CO2", out m_dCO2, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_SO2, "SO2", out m_dSO2, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_NOx, "NOx", out m_dNOx, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_CO, "CO", out m_dCO, leerErlaubt: true)) return false;
            if (!Program.ZahlPruefen(tb_Staub, "Staub", out m_dStaub, leerErlaubt: true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Vorlauf, "Vorlauf", out m_nVorlauf, leerErlaubt: true)) return false;
            if (!Program.GanzzahlPruefen(textBox_Ruecklauf, "Rücklauf", out m_nRuecklauf, leerErlaubt: true)) return false;

            return true;
        }

        HeizkesselModel InitDatensatzUpdate(HeizkesselStammCtrl model = null)
        {
            if (model == null) model = new HeizkesselStammCtrl();

            // Strings sind unkritisch, wir nutzen aber .Trim() gegen versehentliche Leerzeichen
            model.Name = textBox_Name.Text.Trim();
            model.Firma = textBox_Hersteller.Text.Trim();
            model.Beschreibung = textBox_Beschreibung.Text.Trim();

            // Zahlen kommen fertig geparst aus EingabenPruefen
            model.Ptherm = m_dPtherm;

            // Brennstoff: Sicherstellen, dass ein gültiger Index gewählt wurde
            // Falls nichts gewählt ist (-1), wird hier die ID 1 gesetzt
            model.Brennstoff = comboBox_Brennstoff.SelectedIndex >= 0
                               ? comboBox_Brennstoff.SelectedIndex + 1
                               : 1;

            model.Wirkungsgrad_Gas = m_dWirkungsgradGas;
            model.Wirkungsgrad_Oel = m_dWirkungsgradOel;
            model.Betriebsbereitschaftverlust = m_dBBVerlust;
            model.Investitionskosten = m_dInvestitionskosten;
            // Bis zum 18.08.2026 fehlten diese beiden Zeilen: InitDatensatzUpdate setzte
            // Wartungskosten nie, das frisch angelegte Modell trug 0, und jedes Speichern
            // schrieb den Wert im Katalog auf 0 zurück. Genau deshalb stand das Feld in
            // allen 21 Katalog- und 44 Projektzeilen auf 0.
            model.Wartungskosten = m_dWartungskosten;
            model.Wartungskosten_Einheit = GewaehlteEinheit();
            model.Nutzungsdauer = m_dNutzungsdauer;
            model.Raumbedarf = m_dRaumbedarf;
            model.NOx = m_dNOx;
            model.CO2 = m_dCO2;
            model.CO = m_dCO;
            model.SO2 = m_dSO2;
            model.Staub = m_dStaub;
            model.Brennwert = checkBox_Brennwert.Checked;
            model.Vorlauf = m_nVorlauf;
            model.Ruecklauf = m_nRuecklauf;

            return model;
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            // Prüfung vor der Namensabfrage, damit kein Name für einen Datensatz
            // vergeben wird, der anschließend an der Zahlenprüfung scheitert
            if (!EingabenPruefen()) return;

            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                HeizkesselModel model = new HeizkesselModel();

                // Zuerst das Model mit den UI-Daten füllen
                model = InitDatensatzUpdate();

                // Den neuen Namen aus dem Dialog setzen
                model.Name = frmLabel.m_szName;

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    textBox_Name.Text = frmLabel.m_szName;
                    m_szKessel = frmLabel.m_szName;

                    MessageBox.Show("Datensatz erfolgreich neu angelegt.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");
                }
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            // Erst prüfen, dann anlegen: bei ungültiger Eingabe bleibt der Dialog offen
            if (!EingabenPruefen()) return;

            try
            {
                HeizkesselModel model = new HeizkesselModel();
                model = InitDatensatzUpdate();

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    MessageBox.Show("Datensatz gespeichert");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Fehler beim Speichern des Datensatzes!");
                    this.DialogResult = DialogResult.Cancel;
                }
                Close();
            }
            catch
            {
                MessageBox.Show("Fehler beim Speichern des Datensatzes!");
            }
        }

        // Vorlauf/Rücklauf werden als ganze Grad gespeichert (Modellfelder int),
        // deshalb hier die Ganzzahl-Färbung.
        private void textBox_Vorlauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }

        private void textBox_Ruecklauf_TextChanged(object sender, EventArgs e)
        {
            Program.GanzzahlFaerben(sender);
        }
    }
}