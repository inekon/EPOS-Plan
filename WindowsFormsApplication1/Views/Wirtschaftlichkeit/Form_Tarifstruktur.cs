using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „Tarifstruktur Strom" — zwei Modelle nebeneinander, eine Zeile je Stamm
    /// (Tab_ProjektTarif); inaktiv = Flat-Preise der Kostenmaske gelten weiter.
    ///
    /// <para><b>Zonenmodell (Stufe W3, Phase 8)</b> — vereinfachtes Modell laut
    /// Entscheidung 11.08.2026: Winterzeitraum als Monatsspanne, EIN HT-Fenster Mo–Fr,
    /// je vier Zonenpreise für Bezug und Einspeisung, zweistufige
    /// Leistungspreis-Staffel.</para>
    ///
    /// <para><b>Rollenmodell (Etappe E5)</b> — die drei Tarifrollen der Altanwendung:
    /// Bezug (ohne BHKW), Reststrom (mit BHKW) und Einspeisung, je mit EINEM
    /// Durchschnitts-Arbeitspreis (HT/NT entfällt, Leitentscheidung L10). Für die beiden
    /// Bezugsrollen ist das Leistungspreismodell frei wählbar; erst dieser Modus schaltet
    /// die Differenzmethode („vermiedene Kosten") ein.</para>
    ///
    /// <para><b>Die vier Fallen des Altkatalogs sind hier sichtbar vermieden</b>
    /// (Analyse Abschnitt 7.1): Die Staffelgrenzen sind kumulierte OBERGRENZEN statt
    /// Stufenbreiten, die vierte Stufe wird geführt, das Leistungsmodell ist eine
    /// sichtbare Auswahl statt der Schalterlogik „Sommerpreis = 0", und ein Feld
    /// „Preisstand" hält fest, aus welchem Jahr die Preise stammen.</para>
    ///
    /// Komplett im Code aufgebaut (kein Designer/.resx nötig).
    /// Die Modellblöcke werden UMGESCHALTET, nicht aus- und eingeblendet: Ein
    /// handle-loses Control, das erst zur Laufzeit sichtbar wird, verrutscht in einem
    /// scrollenden Formular (Befund d49075e). Enabled ist hier ohnehin die ehrlichere
    /// Anzeige — die Werte des anderen Modells bleiben lesbar und erhalten.
    /// </summary>
    /// <summary>
    /// Ä18 (Nutzerauftrag 26.08.2026): Komponentensicht des Tarifdialogs. Es gilt
    /// weiterhin EIN Tarifsatz je Stamm (Tab_ProjektTarif) — die Sicht bestimmt nur,
    /// welche Blöcke der Dialog baut. Geteilte Felder (Kopf, Einspeisepreise,
    /// Bezugs-Referenzrolle) erscheinen in mehreren Sichten und meinen dieselben
    /// Werte; nicht gebaute Felder behält der Speichervorgang unverändert bei.
    /// </summary>
    public enum TarifSicht
    {
        /// <summary>Alle Blöcke (Bestandsverhalten, Rueckfall).</summary>
        Komplett,
        /// <summary>Strom-EINKAUF: Zonen-Bezugspreise, Leistungspreis-Staffel und
        /// die Bezugsrolle — die Tarifseite der Wärmepumpe und aller Verbraucher.</summary>
        Strombezug,
        /// <summary>BHKW: das Rollenmodell (Differenzmethode) samt Referenzbezug
        /// und Einspeisung sowie die Zonen-Einspeisepreise (KWK-Anteil).</summary>
        Bhkw,
        /// <summary>Photovoltaik: die Einspeisepreise beider Modelle
        /// (Zonenpreise bzw. Rollen-Einspeisung).</summary>
        Photovoltaik
    }

    public class Form_Tarifstruktur : Form
    {
        private const int BREITE = 620;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private readonly TarifParameter _tarif;

        private CheckBox chkAktiv;
        private ComboBox cbModus;
        private DateTimePicker dtGueltigAb;
        private NumericUpDown numWinterVon, numWinterBis, numHtVon, numHtBis;
        private GroupBox grpZonen, grpRollen;

        // Zonenmodell (Stufe W3)
        private NumericUpDown numBezugWHT, numBezugWNT, numBezugSHT, numBezugSNT;
        private NumericUpDown numEinspWHT, numEinspWNT, numEinspSHT, numEinspSNT;
        private NumericUpDown numGrenze, numPreis1, numPreis2;

        // Rollenmodell (Etappe E5)
        private RollenFelder _bezug, _rest;
        private NumericUpDown numEinspArbeit, numEinspGrund;

        private Button btnOk, btnAbbrechen;

        /// <summary>true, wenn gespeichert wurde (Aufrufer rechnet dann neu).</summary>
        public bool Gespeichert { get; private set; }

        public Form_Tarifstruktur(int idStamm)
            : this(idStamm, TarifSicht.Komplett)
        {
        }

        /// <summary>Ä18: Aufruf in einer Komponentensicht (Wirtschaftlichkeitsseite
        /// bzw. PV-Vergütungsdialog).</summary>
        public Form_Tarifstruktur(int idStamm, TarifSicht sicht)
        {
            _tarif = _ctrl.LadeTarif(idStamm);
            _sicht = sicht;
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        private readonly TarifSicht _sicht;

        // Ä18 — welcher Block in dieser Sicht gebaut wird. Die Bezugsrolle bleibt
        // auch in der BHKW-Sicht stehen: Sie ist die REFERENZ der Differenzmethode.
        private bool MitZonenBezug
        { get { return _sicht == TarifSicht.Komplett || _sicht == TarifSicht.Strombezug; } }
        private bool MitZonenEinspeisung
        { get { return _sicht != TarifSicht.Strombezug; } }
        private bool MitRolleBezug
        { get { return _sicht != TarifSicht.Photovoltaik; } }
        private bool MitRolleReststrom
        { get { return _sicht == TarifSicht.Komplett || _sicht == TarifSicht.Bhkw; } }
        private bool MitRolleEinspeisung
        { get { return _sicht != TarifSicht.Strombezug; } }

        /// <summary>Die Eingabefelder EINER Tarifrolle (Bezug oder Reststrom).</summary>
        private class RollenFelder
        {
            public NumericUpDown Arbeit, Grund, Monat;
            public ComboBox Modell;
            public NumericUpDown[] Grenze = new NumericUpDown[4];
            public NumericUpDown[] Sommer = new NumericUpDown[4];
            public NumericUpDown[] Winter = new NumericUpDown[4];
        }

        /// <summary>Ein Eintrag der Modell-/Modusauswahl: sprachneutraler Steuerwert für
        /// die Datenbank, deutscher Text für die Anzeige (Drei-Schichten-Regel).</summary>
        private class Wahl
        {
            public readonly string Wert;
            private readonly string _text;
            public Wahl(string wert, string text) { Wert = wert; _text = text; }
            public override string ToString() { return _text; }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);
            int y = 12;

            chkAktiv = new CheckBox
            {
                Location = new Point(15, y),
                AutoSize = true,
                Text = "Tarifstruktur aktiv (ersetzt die Flat-Strompreise der Kostenmaske)",
                Checked = _tarif.Aktiv
            };
            this.Controls.Add(chkAktiv);
            y += 30;

            cbModus = AuswahlZeile(this, "Tarifmodell:", 28, ref y, _tarif.Modus, new[]
            {
                new Wahl(DbWerte.TARIF_MODUS_ZONEN,  "Zonenmodell (Winter/Sommer × HT/NT)"),
                new Wahl(DbWerte.TARIF_MODUS_ROLLEN, "Rollenmodell (Bezug / Reststrom / Einspeisung)")
            }, 300);
            cbModus.SelectedIndexChanged += new EventHandler(cbModus_SelectedIndexChanged);

            dtGueltigAb = DatumZeile(this, "Preisstand (gültig ab):", 28, ref y, _tarif.GueltigAb);

            if (_sicht != TarifSicht.Komplett)
            {
                // Ä18: Die Sicht zeigt einen AUSSCHNITT desselben Tarifsatzes — das
                // muss dastehen, sonst wirken die anderen Sichten wie eigene Tarife.
                var lblSicht = new Label
                {
                    Location = new Point(15, y),
                    Size = new Size(BREITE - 30, 30),
                    ForeColor = Color.DimGray,
                    Text = "Komponentensicht: Es gilt EIN Tarifsatz je Stamm. Kopfdaten und " +
                           "geteilte Preisfelder erscheinen in mehreren Sichten und meinen " +
                           "dieselben Werte."
                };
                this.Controls.Add(lblSicht);
                y += 34;
            }

            Gruppe(this, "Zeitzonen (HT gilt Mo–Fr; Referenzjahr 2026)", ref y);
            numWinterVon = Zeile(this, "Winter von Monat:", 28, ref y, 1, 12, 0, _tarif.WinterVonMonat, 1);
            numWinterBis = Zeile(this, "Winter bis Monat:", 28, ref y, 1, 12, 0, _tarif.WinterBisMonat, 1);
            numHtVon = Zeile(this, "HT von Stunde (nur Zonenmodell):", 28, ref y, 0, 23, 0, _tarif.HtVonStunde, 1);
            numHtBis = Zeile(this, "HT bis Stunde (exklusiv):", 28, ref y, 1, 24, 0, _tarif.HtBisStunde, 1);
            y += 8;

            BaueZonenblock(ref y);
            BaueRollenblock(ref y);
            ModusUebernehmen();

            y += 8;
            btnOk = new Button
            {
                Location = new Point(BREITE - 236, y),
                Size = new Size(120, 28),
                Text = "Speichern"
            };
            btnOk.Click += new EventHandler(btnOk_Click);
            btnAbbrechen = new Button
            {
                Location = new Point(BREITE - 110, y),
                Size = new Size(90, 28),
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbrechen);

            // Höhe auf den Arbeitsbereich deckeln, damit AutoScroll wirklich greift
            // (Muster Form_WirtschaftlichkeitParameter).
            int inhaltHoehe = y + 45;
            int maxHoehe = Screen.PrimaryScreen.WorkingArea.Height - 90;
            int hoehe = Math.Min(inhaltHoehe, Math.Max(320, maxHoehe));
            this.ClientSize = new Size(hoehe < inhaltHoehe ? BREITE + 20 : BREITE, hoehe);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = true;   // Schutz bei hoher DPI-Skalierung
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbrechen;
            this.Name = "Form_Tarifstruktur";
            switch (_sicht)
            {
                case TarifSicht.Strombezug:
                    this.Text = "Tarifstruktur Strombezug (Wärmepumpe & Verbraucher)"; break;
                case TarifSicht.Bhkw:
                    this.Text = "Tarifstruktur BHKW (Strom)"; break;
                case TarifSicht.Photovoltaik:
                    this.Text = "Tarifstruktur PV-Einspeisung"; break;
                default:
                    this.Text = "Tarifstruktur Strom"; break;
            }
            this.ResumeLayout(false);
        }

        // ------------------------------------------------------------- Blöcke

        private void BaueZonenblock(ref int y)
        {
            grpZonen = new GroupBox
            {
                Location = new Point(15, y),
                Size = new Size(BREITE - 30, 330),
                Text = MitZonenBezug
                    ? "Zonenmodell (Stufe W3) — vier Zonenpreise, zweistufige Staffel"
                    : "Zonenmodell (Stufe W3) — Einspeisepreise"
            };
            this.Controls.Add(grpZonen);
            int gy = 22;

            if (MitZonenBezug)
            {
                Gruppe(grpZonen, "Bezugspreise [€/kWh]", ref gy);
                numBezugWHT = Zeile(grpZonen, "Winter HT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisBezugWinterHT, 0.005m);
                numBezugWNT = Zeile(grpZonen, "Winter NT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisBezugWinterNT, 0.005m);
                numBezugSHT = Zeile(grpZonen, "Sommer HT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisBezugSommerHT, 0.005m);
                numBezugSNT = Zeile(grpZonen, "Sommer NT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisBezugSommerNT, 0.005m);
            }

            if (MitZonenEinspeisung)
            {
                Gruppe(grpZonen, "Einspeisepreise [€/kWh] (PV- und KWK-Einspeisung — geteiltes Feld)", ref gy);
                numEinspWHT = Zeile(grpZonen, "Winter HT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisEinspWinterHT, 0.005m);
                numEinspWNT = Zeile(grpZonen, "Winter NT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisEinspWinterNT, 0.005m);
                numEinspSHT = Zeile(grpZonen, "Sommer HT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisEinspSommerHT, 0.005m);
                numEinspSNT = Zeile(grpZonen, "Sommer NT:", 20, ref gy, 0, 5, 4, (decimal)_tarif.PreisEinspSommerNT, 0.005m);
            }

            if (MitZonenBezug)
            {
                Gruppe(grpZonen, "Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)", ref gy);
                numGrenze = Zeile(grpZonen, "Staffelgrenze [kW]:", 20, ref gy, 0, 100000, 0, (decimal)_tarif.StaffelGrenzeKW, 10);
                numPreis1 = Zeile(grpZonen, "Preis bis Grenze [€/kW·a]:", 20, ref gy, 0, 1000, 2, (decimal)_tarif.StaffelPreis1EurKW, 1);
                numPreis2 = Zeile(grpZonen, "Preis über Grenze [€/kW·a]:", 20, ref gy, 0, 1000, 2, (decimal)_tarif.StaffelPreis2EurKW, 1);
            }

            grpZonen.Height = gy + 12;
            y += grpZonen.Height + 12;
        }

        private void BaueRollenblock(ref int y)
        {
            grpRollen = new GroupBox
            {
                Location = new Point(15, y),
                Size = new Size(BREITE - 30, 600),
                Text = "Rollenmodell (Etappe E5) — Differenzmethode „vermiedene Kosten“"
            };
            this.Controls.Add(grpRollen);
            int gy = 22;

            if (MitRolleBezug)
                _bezug = BaueRolle(grpRollen, "Bezugstarif OHNE BHKW (Referenz)", _tarif.Bezug, ref gy);
            if (MitRolleReststrom)
                _rest = BaueRolle(grpRollen, "Reststromtarif MIT BHKW (kleinere Abnahme, meist teurer)",
                                  _tarif.Reststrom, ref gy);

            if (MitRolleEinspeisung)
            {
                Gruppe(grpRollen, "Einspeisung (kein Leistungspreis — Befund 11 der Altanwendung; " +
                                  "geteiltes Feld für PV- und KWK-Einspeisung)", ref gy);
                numEinspArbeit = Zeile(grpRollen, "Einspeisepreis [€/kWh]:", 20, ref gy,
                                       0, 5, 4, (decimal)_tarif.Einspeisung.ArbeitspreisEurKWh, 0.005m);
                numEinspGrund = Zeile(grpRollen, "Grundpreis [€/a]:", 20, ref gy,
                                      0, 1000000, 2, (decimal)_tarif.Einspeisung.GrundpreisEurJahr, 10);
            }

            string hinweis =
                "Die Staffelgrenzen sind KUMULIERTE Obergrenzen: „500 / 2.000 / 8.000 kW“ heißt " +
                "bis 500 kW Stufe 1, 500–2.000 kW Stufe 2, 2.000–8.000 kW Stufe 3, darüber Stufe 4. " +
                "Eine Obergrenze von 0 bedeutet „nach oben offen“ und beendet die Staffel. " +
                "Der Altkatalog speichert an dieser Stelle Stufen-BREITEN — alte Zahlenreihen sind " +
                "vor der Übernahme umzurechnen.";
            if (MitRolleBezug || MitRolleReststrom)
            {
                var lbl = new Label
                {
                    Location = new Point(20, gy + 4),
                    ForeColor = Color.DimGray,
                    Text = hinweis
                };
                lbl.Size = new Size(BREITE - 70, TextRenderer.MeasureText(
                    hinweis, this.Font, new Size(BREITE - 70, 0), TextFormatFlags.WordBreak).Height + 6);
                grpRollen.Controls.Add(lbl);
                gy += lbl.Height + 10;
            }

            grpRollen.Height = gy;
            y += grpRollen.Height + 12;
        }

        /// <summary>Ein Rollenblock: Arbeits-, Grund- und Monatspreis, Modellauswahl und
        /// die vierstufige Staffel als Raster Grenze | Sommer | Winter.</summary>
        private RollenFelder BaueRolle(Control parent, string titel, TarifRolle rolle, ref int gy)
        {
            var f = new RollenFelder();
            Gruppe(parent, titel, ref gy);
            f.Arbeit = Zeile(parent, "Arbeitspreis (Durchschnitt) [€/kWh]:", 20, ref gy,
                             0, 5, 4, (decimal)rolle.ArbeitspreisEurKWh, 0.005m);
            f.Grund = Zeile(parent, "Grundpreis [€/a]:", 20, ref gy,
                            0, 1000000, 2, (decimal)rolle.GrundpreisEurJahr, 10);
            f.Modell = AuswahlZeile(parent, "Leistungspreismodell:", 20, ref gy, rolle.Leistungsmodell, new[]
            {
                new Wahl(DbWerte.LEISTUNGSMODELL_MONATLICH,
                         "monatlich (Σ zwölf Monatsmaxima × €/kW·Monat)"),
                new Wahl(DbWerte.LEISTUNGSMODELL_STAFFEL,
                         "Staffel (Sommer- und Wintermaximum getrennt)"),
                new Wahl(DbWerte.LEISTUNGSMODELL_JAHRESHOECHSTLAST,
                         "Jahreshöchstlast (Staffel mit Winterpreisen)")
            }, 300);
            f.Monat = Zeile(parent, "Monatlicher Leistungspreis [€/kW·Monat]:", 20, ref gy,
                            0, 1000, 3, (decimal)rolle.MonatspreisEurKWMonat, 0.5m);

            // Kopfzeile der Staffel.
            parent.Controls.Add(new Label
            {
                Location = new Point(40, gy + 3), Size = new Size(120, 20),
                Text = "Staffelstufe"
            });
            Kopf(parent, 165, gy, "Obergrenze [kW]");
            Kopf(parent, 295, gy, "Sommer [€/kW·a]");
            Kopf(parent, 425, gy, "Winter [€/kW·a]");
            gy += 22;

            for (int i = 0; i < 4; i++)
            {
                LeistungsStufe s = i < rolle.Stufen.Count ? rolle.Stufen[i] : new LeistungsStufe();
                parent.Controls.Add(new Label
                {
                    Location = new Point(40, gy + 3), Size = new Size(120, 20),
                    Text = "Stufe " + (i + 1) + (i == 3 ? " (Rest)" : "")
                });
                f.Grenze[i] = Feld(parent, 165, gy, 0, 1000000, 0, (decimal)s.ObergrenzeKW, 100);
                f.Sommer[i] = Feld(parent, 295, gy, 0, 10000, 2, (decimal)s.PreisSommer, 1);
                f.Winter[i] = Feld(parent, 425, gy, 0, 10000, 2, (decimal)s.PreisWinter, 1);
                gy += 27;
            }
            gy += 6;
            return f;
        }

        // ------------------------------------------------------------- Layout-Helfer

        private void Gruppe(Control parent, string text, ref int y)
        {
            parent.Controls.Add(new Label
            {
                Location = new Point(parent == this ? 15 : 12, y + 4),
                Size = new Size(BREITE - 60, 18),
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            y += 26;
        }

        private static void Kopf(Control parent, int x, int y, string text)
        {
            parent.Controls.Add(new Label
            {
                Location = new Point(x, y + 3), Size = new Size(125, 20), Text = text,
                ForeColor = Color.DimGray
            });
        }

        private NumericUpDown Zeile(Control parent, string beschriftung, int x, ref int y,
                                    decimal min, decimal max, int dez, decimal wert, decimal schritt)
        {
            parent.Controls.Add(new Label
            { Location = new Point(x, y + 3), Size = new Size(260, 20), Text = beschriftung });
            NumericUpDown num = Feld(parent, x + 265, y, min, max, dez, wert, schritt);
            y += 29;
            return num;
        }

        private static NumericUpDown Feld(Control parent, int x, int y,
                                          decimal min, decimal max, int dez, decimal wert, decimal schritt)
        {
            var num = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(125, 23),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = dez,
                Increment = schritt,
                TextAlign = HorizontalAlignment.Right
            };
            num.Value = wert < min ? min : (wert > max ? max : wert);
            parent.Controls.Add(num);
            return num;
        }

        private ComboBox AuswahlZeile(Control parent, string beschriftung, int x, ref int y,
                                      string wert, Wahl[] eintraege, int breite)
        {
            parent.Controls.Add(new Label
            { Location = new Point(x, y + 3), Size = new Size(260, 20), Text = beschriftung });
            var cb = new ComboBox
            {
                Location = new Point(x + 265, y),
                Size = new Size(breite, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            int idx = 0;
            for (int i = 0; i < eintraege.Length; i++)
            {
                cb.Items.Add(eintraege[i]);
                if (string.Equals(eintraege[i].Wert, wert, StringComparison.Ordinal)) idx = i;
            }
            cb.SelectedIndex = idx;
            parent.Controls.Add(cb);
            y += 32;
            return cb;
        }

        private DateTimePicker DatumZeile(Control parent, string beschriftung, int x, ref int y, DateTime? wert)
        {
            parent.Controls.Add(new Label
            { Location = new Point(x, y + 3), Size = new Size(260, 20), Text = beschriftung });
            var dt = new DateTimePicker
            {
                Location = new Point(x + 265, y),
                Size = new Size(160, 23),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = wert.HasValue
            };
            if (wert.HasValue && wert.Value >= dt.MinDate && wert.Value <= dt.MaxDate) dt.Value = wert.Value;
            else if (wert.HasValue) dt.Checked = false;   // unplausibles DB-Datum: nicht übernehmen
            parent.Controls.Add(dt);
            y += 32;
            return dt;
        }

        // ------------------------------------------------------------- Umschalten

        private void cbModus_SelectedIndexChanged(object sender, EventArgs e)
        { ModusUebernehmen(); }

        /// <summary>Schaltet die Modellblöcke um — Enabled, NICHT Visible (siehe
        /// Klassenkommentar).</summary>
        private void ModusUebernehmen()
        {
            bool rollen = string.Equals(Gewaehlt(cbModus, DbWerte.TARIF_MODUS_ZONEN),
                                        DbWerte.TARIF_MODUS_ROLLEN, StringComparison.Ordinal);
            if (grpZonen != null) grpZonen.Enabled = !rollen;
            if (grpRollen != null) grpRollen.Enabled = rollen;
            if (numHtVon != null) numHtVon.Enabled = !rollen;   // HT/NT entfällt im Rollenmodell
            if (numHtBis != null) numHtBis.Enabled = !rollen;
        }

        private static string Gewaehlt(ComboBox cb, string vorgabe)
        {
            var w = cb != null ? cb.SelectedItem as Wahl : null;
            return w != null ? w.Wert : vorgabe;
        }

        // ------------------------------------------------------------- Speichern

        private void btnOk_Click(object sender, EventArgs e)
        {
            string modus = Gewaehlt(cbModus, DbWerte.TARIF_MODUS_ZONEN);
            bool rollen = string.Equals(modus, DbWerte.TARIF_MODUS_ROLLEN, StringComparison.Ordinal);

            if (!rollen && (int)numHtVon.Value >= (int)numHtBis.Value)
            {
                MessageBox.Show("Das HT-Fenster ist leer (von ≥ bis).", "Tarifstruktur",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chkAktiv.Checked && !rollen && numBezugWHT != null &&
                numBezugWHT.Value <= 0 && numBezugWNT.Value <= 0 &&
                numBezugSHT.Value <= 0 && numBezugSNT.Value <= 0)
            {
                MessageBox.Show("Die Tarifstruktur ist aktiv, aber es ist kein Bezugspreis gepflegt — " +
                    "die Berechnung fällt dann auf die Flat-Preise der Kostenmaske zurück.",
                    "Tarifstruktur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (chkAktiv.Checked && rollen && _bezug != null && _rest != null &&
                _bezug.Arbeit.Value <= 0 && _rest.Arbeit.Value <= 0)
            {
                MessageBox.Show("Das Rollenmodell ist aktiv, aber weder für den Bezug noch für den " +
                    "Reststrom ist ein Arbeitspreis gepflegt — die Berechnung fällt dann auf die " +
                    "Flat-Preise der Kostenmaske zurück.",
                    "Tarifstruktur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _tarif.Aktiv = chkAktiv.Checked;
            _tarif.Modus = modus;
            _tarif.GueltigAb = dtGueltigAb.Checked ? (DateTime?)dtGueltigAb.Value.Date : null;
            _tarif.WinterVonMonat = (int)numWinterVon.Value;
            _tarif.WinterBisMonat = (int)numWinterBis.Value;
            _tarif.HtVonStunde = (int)numHtVon.Value;
            _tarif.HtBisStunde = (int)numHtBis.Value;
            // Ä18: Nur die in dieser Sicht GEBAUTEN Felder überschreiben den
            // geladenen Satz — alles andere behält seine Werte (eine Wahrheit,
            // mehrere Fenster).
            if (numBezugWHT != null)
            {
                _tarif.PreisBezugWinterHT = (double)numBezugWHT.Value;
                _tarif.PreisBezugWinterNT = (double)numBezugWNT.Value;
                _tarif.PreisBezugSommerHT = (double)numBezugSHT.Value;
                _tarif.PreisBezugSommerNT = (double)numBezugSNT.Value;
            }
            if (numEinspWHT != null)
            {
                _tarif.PreisEinspWinterHT = (double)numEinspWHT.Value;
                _tarif.PreisEinspWinterNT = (double)numEinspWNT.Value;
                _tarif.PreisEinspSommerHT = (double)numEinspSHT.Value;
                _tarif.PreisEinspSommerNT = (double)numEinspSNT.Value;
            }
            if (numGrenze != null)
            {
                _tarif.StaffelGrenzeKW = (double)numGrenze.Value;
                _tarif.StaffelPreis1EurKW = (double)numPreis1.Value;
                _tarif.StaffelPreis2EurKW = (double)numPreis2.Value;
            }

            if (_bezug != null) RolleUebernehmen(_bezug, _tarif.Bezug);
            if (_rest != null) RolleUebernehmen(_rest, _tarif.Reststrom);
            if (numEinspArbeit != null)
            {
                _tarif.Einspeisung.ArbeitspreisEurKWh = (double)numEinspArbeit.Value;
                _tarif.Einspeisung.GrundpreisEurJahr = (double)numEinspGrund.Value;
            }

            if (!_ctrl.SpeichereTarif(_tarif))
            {
                MessageBox.Show("Die Tarifstruktur konnte nicht gespeichert werden.", "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = true;
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private static void RolleUebernehmen(RollenFelder f, TarifRolle r)
        {
            r.ArbeitspreisEurKWh = (double)f.Arbeit.Value;
            r.GrundpreisEurJahr = (double)f.Grund.Value;
            r.MonatspreisEurKWMonat = (double)f.Monat.Value;
            r.Leistungsmodell = Gewaehlt(f.Modell, DbWerte.LEISTUNGSMODELL_MONATLICH);
            for (int i = 0; i < 4 && i < r.Stufen.Count; i++)
            {
                r.Stufen[i].ObergrenzeKW = (double)f.Grenze[i].Value;
                r.Stufen[i].PreisSommer = (double)f.Sommer[i].Value;
                r.Stufen[i].PreisWinter = (double)f.Winter[i].Value;
            }
        }
    }
}
