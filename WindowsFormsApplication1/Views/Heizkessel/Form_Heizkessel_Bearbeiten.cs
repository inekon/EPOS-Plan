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

        // Primaerschluessel des geladenen Katalogsatzes (Tab_Heizkessel_STAMM.ID),
        // 0 in MODE_NEU. Ueber ihn schreibt btn_Ueberschreiben zurueck; gesetzt wird er
        // in SetControls, wo auch die Begruendung steht.
        private int m_nKesselId = 0;

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
            // H7: Die Maske fuehrt oben rechts eine SENKRECHTE Knopfleiste
            // (x 616..721 von y 19 bis 168), der KI-Aufrufknopf sitzt deshalb
            // darunter (KiDialoge.cs: AbstandRechts 8, AbstandOben 176). Nach der
            // Kollisionsregel gehoert der Infoknopf LINKS daneben, auf dieselbe Hoehe.
            InfoKnopf.Anbringen(this, abstandRechts: 60, abstandOben: 176);

            // Dezenter Einstieg in den Assistenten, oben rechts im Client-Bereich
            // (Fachkonzept 11.8). Programmatisch, damit Designer und .resx
            // unberuehrt bleiben.
            KiAufrufKnopf.Anbringen(this);

            m_mode = mode;

            // Vorsorge vor dem ersten Lese-/Schreibzugriff: auf einer nicht migrierten
            // Datenbank fehlt Wartungskosten_Einheit noch (Migrationsschritt 15).
            HeizkesselStammCtrl.StelleSpaltenSicher();
            WartungsfeldAufbauen();
            KostenzugriffAnbringen();   // ETAPPE KD6 (§ 9, FK8)

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
        /// Weg gehen die neueren Masken dieser Etappe (<c>ucKostenItem</c> hebt seine
        /// Betragsgrenze programmatisch an; <c>Form_PlanwertUebernahme</c> ist
        /// inzwischen auf eine Designer-Datei migriert).
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
        /// <summary>
        /// ETAPPE KD6 (Konzept Kostendialoge § 9, FK8): Die eingebetteten
        /// Kostenfelder („Eingabedaten zur Berechnung der Kosten") werden eine
        /// Version lang SCHREIBGESCHÜTZT — gepflegt wird in der Stammvorlage —
        /// und unten kommt der Kosten-Block mit den drei Aufrufen an
        /// (Serienbaustein <see cref="KostenKnoepfe"/>). Die Gerätewerte bleiben
        /// Datenquelle der Planwert-Übernahme, kein zweiter Pflegeort.
        /// </summary>
        private void KostenzugriffAnbringen()
        {
            KostenKnoepfe.Sperren(tb_Investitionskosten, tb_Raumbedarf, tb_Nutzungsdauer,
                                  tb_Wartungskosten, cb_WartungEinheit);
            var leiste = KostenKnoepfe.Leiste(this, DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
                () => 0, () => null, KostenKnoepfe.Fk8Hinweis());
            leiste.Dock = System.Windows.Forms.DockStyle.Bottom;
            Controls.Add(leiste);
            Height += 46;
        }

        private void WartungsfeldAufbauen()
        {
            if (groupBox3 == null || tb_Investitionskosten == null) return;

            // Rechte Spalte hinter dem BREITESTEN Einheitenzeichen der linken Spalte
            // beginnen (Nutzerbefund 25.08.2026: der alte Anker war das schmale
            // „€"-Label — das breitere „Jahre" ragte unter die Einheiten-Klappliste).
            int rechtsMax = tb_Investitionskosten.Right;
            foreach (Control c in groupBox3.Controls)
                if (c is Label && c.Left >= tb_Investitionskosten.Right - 4 &&
                    c.Left <= tb_Investitionskosten.Right + 60)
                    rechtsMax = Math.Max(rechtsMax, c.Right);
            int spalte = rechtsMax + 24;
            int feldX = spalte + 110;
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

            // Beschriftung und Auswahl auf DERSELBEN Zeile (zeile2) — bündig zur
            // Feldspalte des Wartungsbetrags; die frühere dritte Zeile schob die
            // Klappliste unter das „Jahre"-Label der linken Spalte. Reicht der Platz
            // rechts nicht („€/a Jahresbetrag" braucht ~180 px), rückt die Auswahl
            // wie früher auf zeile3 unter die Beschriftung — dann aber ebenfalls in
            // der Feldspalte, nicht mehr unter dem linken Einheitenzeichen.
            int breite = Math.Min(220, maxRechts - feldX);
            bool zweizeilig = breite < 150;
            cb_WartungEinheit = new ComboBox
            {
                Name = "cb_WartungEinheit",
                Location = zweizeilig ? new Point(feldX, zeile3) : new Point(feldX, zeile2),
                Width = Math.Max(140, breite),
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

            // Elemente einer Zeile auf eine Linie bringen (Nutzerbefund 25.08.2026):
            // Beschriftungen mittig zur Feldhöhe statt mit festem +2-Versatz.
            lblWartung.Top = tb_Wartungskosten.Top +
                (tb_Wartungskosten.Height - lblWartung.Height) / 2;
            lblEinheit.Top = cb_WartungEinheit.Top +
                (cb_WartungEinheit.Height - lblEinheit.Height) / 2;

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

        /// <summary>
        /// Füllt die Maske aus dem Katalog und merkt sich die ID der geladenen Zeile.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Quelle statt zwei.</b> Bis zum 18.08.2026 kamen die Anzeigefelder aus
        /// einem <c>RecordSet</c> und nur die Wartungskosten aus dem Controller — zwei
        /// unabhängige Abfragen auf dieselbe Tabelle, beide ohne <c>ORDER BY</c>. Solange
        /// <c>Bezeichner</c> eindeutig ist, fällt das nicht auf; im Bestand ist er es
        /// achtmal nicht, und dann darf die ACE-Engine den beiden Abfragen verschiedene
        /// Zeilen liefern — die Maske konnte Werte aus ZWEI Kesseln mischen. Jetzt liest
        /// <see cref="HeizkesselStammCtrl.ReadSingle"/> genau einmal, sortiert nach ID,
        /// und liefert alle Felder samt Wartungskosten und Einheit mit der dort
        /// beschriebenen Rückfallebene für eine nicht migrierte Datenbank.
        /// </para>
        /// <para>
        /// <b>Warum die ID festgehalten wird.</b> Sie ist die Adresse, unter der
        /// <see cref="btn_Ueberschreiben_Click"/> zurückschreibt. Damit gilt: geschrieben
        /// wird die Zeile, die auch angezeigt wurde. Vorher lief das UPDATE über den
        /// Bezeichner und traf bei einer Dublette BEIDE Katalogsätze.
        /// </para>
        /// <para>
        /// <b>Der Hinweis bei Mehrdeutigkeit hält niemanden auf.</b> Bearbeiten bleibt
        /// möglich und ist jetzt eindeutig; die Meldung sagt nur, dass der Katalog einen
        /// weiteren Eintrag gleichen Namens führt, den dieser Dialog nicht zeigt. Ohne
        /// sie bliebe unerklärlich, warum derselbe Name in der Auswahlliste mehrfach
        /// steht und die zweite Zeile sich nicht erreichen lässt.
        /// </para>
        /// </remarks>
        public void SetControls(string szName, string szBeschreibung)
        {
            textBox_Name.Text = szName;
            m_szKessel = szName;
            textBox_Beschreibung.Text = szBeschreibung;

            HeizkesselStammCtrl katalog = new HeizkesselStammCtrl();
            katalog.ReadSingle(szName);
            if (katalog.rows == 0) return;

            m_nKesselId = katalog.ID;

            textBox_Hersteller.Text = katalog.Firma;
            tb_th_Leistung.Text = katalog.Ptherm.ToString();
            tb_Wirkungsgrad.Text = katalog.Wirkungsgrad_Gas.ToString();
            tb_Wirkungsgrad_Öl.Text = katalog.Wirkungsgrad_Oel.ToString();
            tb_B_Verlust.Text = katalog.Betriebsbereitschaftverlust.ToString("F2");
            tb_Investitionskosten.Text = katalog.Investitionskosten.ToString("F2");
            tb_Wartungskosten.Text = katalog.Wartungskosten.ToString("F2");
            EinheitWaehlen(katalog.Wartungskosten_Einheit);
            tb_Nutzungsdauer.Text = katalog.Nutzungsdauer.ToString();
            tb_Raumbedarf.Text = katalog.Raumbedarf.ToString();
            tb_NOx.Text = katalog.NOx.ToString();
            tb_CO2.Text = katalog.CO2.ToString();
            tb_CO.Text = katalog.CO.ToString();
            tb_SO2.Text = katalog.SO2.ToString();
            tb_Staub.Text = katalog.Staub.ToString();
            checkBox_Brennwert.Checked = katalog.Brennwert;
            textBox_Vorlauf.Text = katalog.Vorlauf.ToString();
            textBox_Ruecklauf.Text = katalog.Ruecklauf.ToString();

            // Bereichsprüfung statt roher Zuweisung: Brennstoff ist eine 1-basierte ID
            // aus Tab_Brennstoff_Stamm, die Liste im Kombinationsfeld kann kürzer sein.
            int brennstoffIndex = katalog.Brennstoff >= 1 ? katalog.Brennstoff - 1 : 1;
            if (brennstoffIndex >= 0 && brennstoffIndex < comboBox_Brennstoff.Items.Count)
                comboBox_Brennstoff.SelectedIndex = brennstoffIndex;

            int gleiche = HeizkesselStammCtrl.AnzahlMitBezeichner(szName);
            if (gleiche > 1)
            {
                MessageBox.Show("Der Katalog führt den Namen \"" + szName + "\" " + gleiche +
                    "-mal. Bearbeitet wird der Eintrag mit der kleinsten ID (" + m_nKesselId +
                    "); die übrigen bleiben unverändert.",
                    "Name mehrdeutig", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Schreibt die Maske in GENAU den Katalogsatz zurück, den
        /// <see cref="SetControls"/> geladen hat.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die ID ist die Absicherung.</b> Ohne sie fiele
        /// <see cref="HeizkesselStammCtrl.Update"/> auf den Bezeichner zurück und träfe
        /// bei einem doppelt vergebenen Namen beide Zeilen — der eigentliche Befund vom
        /// 18.08.2026. <c>InitDatensatzUpdate</c> baut das Modell aus der Maske auf und
        /// kennt die Herkunft nicht, deshalb wird die ID danach gesetzt.
        /// </para>
        /// <para>
        /// <b>Ein Fehlschlag schließt den Dialog nicht mehr.</b> Vorher lief der Ablauf
        /// nach der Fehlermeldung in dieselbe Zeile
        /// <c>DialogResult = DialogResult.OK; Close();</c> — der Aufrufer lud die Liste
        /// neu, als wäre gespeichert worden, und die Eingaben waren weg. Da
        /// <see cref="HeizkesselStammCtrl.Update"/> jetzt auch aus fachlichem Grund
        /// ablehnen kann (schreibgeschützt, Name bereits vergeben, fehlende ID) und den
        /// Grund selbst meldet, bleibt der Dialog in diesem Fall offen.
        /// </para>
        /// </remarks>
        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            // Erst prüfen, dann schreiben: bei ungültiger Eingabe bleibt der Dialog offen
            if (!EingabenPruefen()) return;

            HeizkesselStammCtrl ctrl = new HeizkesselStammCtrl();

            try
            {
                // Tab_Heizkessel_STAMM fuehrt keinen eindeutigen Schluessel auf Bezeichner,
                // HeizkesselStammCtrl.Update() filtert aber genau darauf. Bei einer Dublette
                // wuerden beide Saetze zugleich ueberschrieben - deshalb hier abbrechen,
                // statt unbemerkt zwei Katalogsaetze zu veraendern (gleiche Bremse wie in
                // Form_Heizkessel_Admin).
                object anz = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM [" + HeizkesselStammCtrl.TABLE + "] WHERE Bezeichner = ?",
                    new DbParam("@nam", m_szKessel));
                int nAnzahl = (anz == null || anz == DBNull.Value) ? 0 : Convert.ToInt32(anz);
                if (nAnzahl > 1)
                {
                    MessageBox.Show(
                        string.Format(MyResource.Resource.ADM_MEHRDEUTIG_TEXT, m_szKessel, nAnzahl),
                        MyResource.Resource.ADM_MEHRDEUTIG_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;   // Dialog bleibt offen, nichts geschrieben
                }

                InitDatensatzUpdate(ctrl);
                ctrl.ID = m_nKesselId;

                if (!ctrl.Update()) return;   // Grund hat Update() bereits gemeldet

                // Der Aufrufer wählt über m_szKessel den Eintrag in seiner Liste wieder
                // aus - nach einer Umbenennung ist das der NEUE Name.
                m_szKessel = ctrl.Name;

                MessageBox.Show("Datensatz gespeichert");
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

            // iU9-W2.1: Namensabfrage ueber NamensDialogHuelle statt
            // Form_Sp_ItemNeu (mittig statt an der Knopfposition - die
            // Blazor-Huelle kennt kein PointToScreen; Name kommt getrimmt).
            string szName = NamensDialogHuelle.Bezeichner(this);

            if (szName != null)
            {
                HeizkesselModel model = new HeizkesselModel();

                // Zuerst das Model mit den UI-Daten füllen
                model = InitDatensatzUpdate();

                // Den neuen Namen aus dem Dialog setzen
                model.Name = szName;

                // Alles in einem Rutsch speichern
                if (Insert(model))
                {
                    textBox_Name.Text = szName;
                    m_szKessel = szName;

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