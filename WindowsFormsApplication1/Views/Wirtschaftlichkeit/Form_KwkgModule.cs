using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „KWK-Zuschlag je BHKW-Modul" (Etappe E6, Nutzerentscheidung 18.08.2026:
    /// „Je BHKW-Modul — erst damit sind die gesetzlichen Leistungsklassen abbildbar").
    ///
    /// <para>Er pflegt die acht Angaben aus Migrationsschritt 22 an
    /// <c>Tab_Energieanlagen</c> und zeigt zu jeder Anlage den <b>Katalogvorschlag</b>
    /// samt Herleitung. Der Vorschlag wird ausdrücklich <b>nicht</b> automatisch
    /// angesetzt: Er erscheint als Text, und erst „Vorschlag übernehmen" schreibt ihn in
    /// die Satzfelder. Ohne diesen Griff bleibt jede Anlage beim Projektsatz — genau
    /// daran hängt die Ergebnisneutralität für Bestandsprojekte.</para>
    ///
    /// <para>Alle Felder sind leer zulässig; leer heißt „kein eigener Wert, es gilt der
    /// Projektwert". Die Projektvorgaben stehen im Parameterdialog und sind dort als
    /// Vorgabe benannt.</para>
    ///
    /// <para>Komplett im Code aufgebaut (kein Designer/.resx nötig) — Muster
    /// <c>Form_WirtschaftlichkeitParameter</c>. Wie dieser ist der Dialog nicht
    /// lokalisiert; die Herleitungstexte kommen dagegen aus <c>MyResource</c>, weil
    /// dieselben Texte auch im Ergebnis erscheinen.</para>
    /// </summary>
    public class Form_KwkgModule : Form
    {
        private readonly KwkgAnlagenCtrl _ctrl = new KwkgAnlagenCtrl();
        private readonly List<KwkgAnlagenAngabe> _anlagen;
        private readonly WirtschaftlichkeitParameter _projekt;
        private readonly GesetzKatalog _katalog = new GesetzKatalog();

        private ListBox _liste;
        private DateTimePicker _dtStichtag, _dtIbn;
        private ComboBox _cbArt, _cbFall;
        private NumericUpDown _numEinsp, _numEigen, _numKontingent, _numDeckel;
        private Label _lblKopf, _lblVorschlag;
        private Button _btnUebernehmen, _btnOk, _btnAbbrechen;

        private int _aktuell = -1;
        private bool _stumm;   // true, während der Dialog die Felder selbst füllt

        /// <summary>true, wenn mindestens eine Zeile gespeichert wurde.</summary>
        public bool Gespeichert { get; private set; }

        public Form_KwkgModule(int idStamm, string stammName, WirtschaftlichkeitParameter projekt)
        {
            _projekt = projekt ?? new WirtschaftlichkeitParameter();
            _anlagen = _ctrl.LadeGruppe(idStamm, stammName);
            Aufbauen();
        }

        // ------------------------------------------------------------- Aufbau

        private void Aufbauen()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);

            _liste = new ListBox
            {
                Location = new Point(12, 30),
                Size = new Size(250, 230),
                IntegralHeight = false
            };
            foreach (KwkgAnlagenAngabe g in _anlagen)
                _liste.Items.Add(g.Projektname + " · " + g.Bezeichner +
                                 " (" + g.PelKW.ToString("N0") + " kW)");
            _liste.SelectedIndexChanged += new EventHandler(Liste_Wechsel);
            this.Controls.Add(Beschriftung("BHKW-Anlagen der Vergleichsgruppe:", 12, 10, 250));
            this.Controls.Add(_liste);

            int x = 278, y = 30;
            _dtStichtag = DatumZeile("Stichtag (Bestellung/Genehmigung):", x, ref y);
            _dtIbn = DatumZeile("Inbetriebnahme:", x, ref y);
            _cbArt = AuswahlZeile("Anlagenart:", x, ref y, new[]
            {
                new Steuerwahl("", "(nicht erfasst — gilt als Neuanlage)"),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_NEU,           "neue Anlage (§ 8 Abs. 1)"),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_MODERNISIERT,  "modernisiert (§ 8 Abs. 2)"),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_NACHGERUESTET, "nachgerüstet (§ 8 Abs. 3)")
            });
            _cbFall = AuswahlZeile("Eigenstrom nach § 6 Abs. 3:", x, ref y, new[]
            {
                new Steuerwahl(DbWerte.KWKG_EIGENFALL_KEINER, "kein Tatbestand (kein Eigenstromzuschlag)"),
                new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR1,    "Nr. 1 — Anlage bis 100 kW"),
                new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR2,    "Nr. 2 — Kundenanlage / geschl. Netz"),
                new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR3,    "Nr. 3 — stromkostenintensiv")
            });
            _numEinsp = ZahlZeile("Satz Einspeisung [ct/kWh] (0 = Projektsatz):", x, ref y, 0m, 30m, 2, 0.1m);
            _numEigen = ZahlZeile("Satz Eigenstrom [ct/kWh] (0 = Projektsatz):", x, ref y, 0m, 30m, 2, 0.1m);
            _numKontingent = ZahlZeile("Vbh-Kontingent [h] (0 = Projektwert):", x, ref y, 0m, 200000m, 0, 1000m);
            _numDeckel = ZahlZeile("Vbh-Jahresdeckel [h/a] (0 = Staffel):", x, ref y, 0m, 8760m, 0, 100m);

            _lblKopf = new Label
            {
                Location = new Point(x, y + 6),
                Size = new Size(430, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = "Katalogvorschlag (§ 7 KWKG 2025)"
            };
            this.Controls.Add(_lblKopf);
            y += 26;

            _lblVorschlag = new Label
            {
                Location = new Point(x, y),
                Size = new Size(430, 96),
                ForeColor = Color.DimGray
            };
            this.Controls.Add(_lblVorschlag);
            y += 102;

            _btnUebernehmen = new Button
            {
                Location = new Point(x, y),
                Size = new Size(200, 26),
                Text = "Vorschlag in die Satzfelder übernehmen"
            };
            _btnUebernehmen.Click += new EventHandler(Uebernehmen_Klick);
            this.Controls.Add(_btnUebernehmen);
            y += 36;

            string hinweis =
                "Leere Felder heißen „kein eigener Wert“ — dann gilt die Projektvorgabe aus dem " +
                "Parameterdialog. Der Vorschlag wird NICHT automatisch angesetzt: Erst die " +
                "Schaltfläche schreibt ihn in die Satzfelder, und erst dann rechnet diese Anlage " +
                "mit einem eigenen Satz. Vollbenutzungsstunden, Jahresdeckel und Kontingent " +
                "gelten nach § 8 KWKG je Anlage.";
            var lblHinweis = new Label { Location = new Point(12, y + 4), ForeColor = Color.DimGray };
            lblHinweis.Size = new Size(696, TextRenderer.MeasureText(
                hinweis, this.Font, new Size(696, 0), TextFormatFlags.WordBreak).Height + 6);
            lblHinweis.Text = hinweis;
            this.Controls.Add(lblHinweis);
            y += lblHinweis.Height + 12;

            _btnOk = new Button
            {
                Location = new Point(488, y),
                Size = new Size(120, 28),
                Text = "Speichern"
            };
            _btnOk.Click += new EventHandler(Speichern_Klick);
            _btnAbbrechen = new Button
            {
                Location = new Point(614, y),
                Size = new Size(94, 28),
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_btnOk);
            this.Controls.Add(_btnAbbrechen);

            this.ClientSize = new Size(720, y + 44);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScroll = true;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.AcceptButton = _btnOk;
            this.CancelButton = _btnAbbrechen;
            this.Name = "Form_KwkgModule";
            this.Text = "KWK-Zuschlag je BHKW-Modul";
            this.ResumeLayout(false);

            if (_liste.Items.Count > 0) _liste.SelectedIndex = 0;
            else FelderAktiv(false);
        }

        // --------------------------------------------------------- Layout-Helfer

        private Label Beschriftung(string text, int x, int y, int breite)
        {
            return new Label { Location = new Point(x, y), Size = new Size(breite, 18), Text = text };
        }

        private DateTimePicker DatumZeile(string text, int x, ref int y)
        {
            this.Controls.Add(Beschriftung(text, x, y + 3, 240));
            var dt = new DateTimePicker
            {
                Location = new Point(x + 244, y),
                Size = new Size(160, 23),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false
            };
            this.Controls.Add(dt);
            y += 30;
            return dt;
        }

        private ComboBox AuswahlZeile(string text, int x, ref int y, Steuerwahl[] eintraege)
        {
            this.Controls.Add(Beschriftung(text, x, y + 3, 240));
            var cb = new ComboBox
            {
                Location = new Point(x + 244, y),
                Size = new Size(186, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (Steuerwahl w in eintraege) cb.Items.Add(w);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += new EventHandler(Feld_Wechsel);
            this.Controls.Add(cb);
            y += 30;
            return cb;
        }

        private NumericUpDown ZahlZeile(string text, int x, ref int y,
                                        decimal min, decimal max, int dez, decimal schritt)
        {
            this.Controls.Add(Beschriftung(text, x, y + 3, 240));
            var num = new NumericUpDown
            {
                Location = new Point(x + 244, y),
                Size = new Size(160, 23),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = dez,
                Increment = schritt,
                TextAlign = HorizontalAlignment.Right
            };
            this.Controls.Add(num);
            y += 29;
            return num;
        }

        /// <summary>Ein Eintrag einer Auswahlliste: sprachneutraler Steuerwert für die
        /// Datenbank, deutscher Text für die Anzeige (Muster Form_WirtschaftlichkeitParameter).</summary>
        private class Steuerwahl
        {
            public readonly string Wert;
            private readonly string _text;
            public Steuerwahl(string wert, string text) { Wert = wert; _text = text; }
            public override string ToString() { return _text; }
        }

        // ------------------------------------------------------------- Bedienung

        private void FelderAktiv(bool an)
        {
            _dtStichtag.Enabled = an; _dtIbn.Enabled = an;
            _cbArt.Enabled = an; _cbFall.Enabled = an;
            _numEinsp.Enabled = an; _numEigen.Enabled = an;
            _numKontingent.Enabled = an; _numDeckel.Enabled = an;
            _btnUebernehmen.Enabled = an;
        }

        private void Liste_Wechsel(object sender, EventArgs e)
        {
            UebernimmFelder();                 // die zuvor gewählte Zeile sichern
            _aktuell = _liste.SelectedIndex;
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) { FelderAktiv(false); return; }

            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            _stumm = true;
            try
            {
                FelderAktiv(true);
                Datum(_dtStichtag, g.Stichtag);
                Datum(_dtIbn, g.Inbetriebnahme);
                Waehle(_cbArt, g.Anlagenart);
                Waehle(_cbFall, g.Eigenfall);
                _numEinsp.Value = Geklemmt(_numEinsp, g.SatzEinspCt);
                _numEigen.Value = Geklemmt(_numEigen, g.SatzEigenCt);
                _numKontingent.Value = Geklemmt(_numKontingent, g.VbhKontingent);
                _numDeckel.Value = Geklemmt(_numDeckel, g.VbhDeckel);
            }
            finally { _stumm = false; }
            VorschlagZeigen();
        }

        /// <summary>Anlagenart und Eigenstromfall verändern den Vorschlag, deshalb wird er
        /// unmittelbar neu gebildet.</summary>
        private void Feld_Wechsel(object sender, EventArgs e)
        {
            if (_stumm) return;
            UebernimmFelder();
            VorschlagZeigen();
        }

        /// <summary>Die Bildschirmfelder in die Liste zurückschreiben (ohne Datenbank).</summary>
        private void UebernimmFelder()
        {
            if (_stumm || _aktuell < 0 || _aktuell >= _anlagen.Count) return;
            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            g.Stichtag = _dtStichtag.Checked ? (DateTime?)_dtStichtag.Value.Date : null;
            g.Inbetriebnahme = _dtIbn.Checked ? (DateTime?)_dtIbn.Value.Date : null;
            g.Anlagenart = Gewaehlt(_cbArt);
            g.Eigenfall = Gewaehlt(_cbFall);
            // 0 heißt „kein eigener Wert": Ein Zuschlagssatz von null wäre fachlich kein
            // Satz, und ein Kontingent von null keine Laufzeit. Die Unterscheidung ist
            // wichtig, weil sonst niemand mehr zum Projektwert zurückkäme.
            g.SatzEinspCt = _numEinsp.Value > 0 ? (double?)_numEinsp.Value : null;
            g.SatzEigenCt = _numEigen.Value > 0 ? (double?)_numEigen.Value : null;
            g.VbhKontingent = _numKontingent.Value > 0 ? (double?)_numKontingent.Value : null;
            g.VbhDeckel = _numDeckel.Value > 0 ? (double?)_numDeckel.Value : null;
        }

        private void VorschlagZeigen()
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) { _lblVorschlag.Text = ""; return; }
            KwkgAnlagenAngabe g = _anlagen[_aktuell];
            KwkgSatzVorschlag v = Vorschlag(g);
            _lblVorschlag.Text =
                "Einspeisung " + v.SatzEinspeisungCt.ToString("N2", BerichtTexte.Kultur) + " ct/kWh — " +
                v.HerleitungEinspeisung + Environment.NewLine + Environment.NewLine +
                "Eigenstrom " + v.SatzEigenCt.ToString("N2", BerichtTexte.Kultur) + " ct/kWh — " +
                v.HerleitungEigen;
        }

        /// <summary>Der Vorschlag für die gewählte Anlage — mit dem Inbetriebnahmejahr
        /// DIESER Anlage als Stichtag, ersatzweise dem des Projekts.</summary>
        private KwkgSatzVorschlag Vorschlag(KwkgAnlagenAngabe g)
        {
            int jahr = g.Inbetriebnahme.HasValue
                ? g.Inbetriebnahme.Value.Year
                : (_projekt.KwkgInbetriebnahme.HasValue
                    ? _projekt.KwkgInbetriebnahme.Value.Year
                    : DateTime.Now.Year + 1);
            return KwkgSatzRechner.Vorschlag(g.PelKW, jahr, g.Anlagenart, g.Eigenfall,
                                             _katalog.WertMitHerkunft, BerichtTexte.Kultur);
        }

        private void Uebernehmen_Klick(object sender, EventArgs e)
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) return;
            UebernimmFelder();
            KwkgSatzVorschlag v = Vorschlag(_anlagen[_aktuell]);
            _stumm = true;
            try
            {
                _numEinsp.Value = Geklemmt(_numEinsp, v.SatzEinspeisungCt);
                _numEigen.Value = Geklemmt(_numEigen, v.SatzEigenCt);
            }
            finally { _stumm = false; }
            UebernimmFelder();
            VorschlagZeigen();
        }

        private void Speichern_Klick(object sender, EventArgs e)
        {
            UebernimmFelder();
            int fehler = 0;
            foreach (KwkgAnlagenAngabe g in _anlagen)
                if (!_ctrl.Speichere(g)) fehler++;

            if (fehler > 0)
            {
                MessageBox.Show(fehler + " von " + _anlagen.Count +
                                " Anlagen konnten nicht gespeichert werden.", "Fehler",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Gespeichert = _anlagen.Count > 0;
            this.DialogResult = DialogResult.OK;
            Close();
        }

        // ------------------------------------------------------------- Kleinkram

        private static void Datum(DateTimePicker dt, DateTime? wert)
        {
            if (wert.HasValue && wert.Value >= dt.MinDate && wert.Value <= dt.MaxDate)
            {
                dt.Value = wert.Value;
                dt.Checked = true;
            }
            else dt.Checked = false;
        }

        private static void Waehle(ComboBox cb, string wert)
        {
            for (int i = 0; i < cb.Items.Count; i++)
                if (string.Equals(((Steuerwahl)cb.Items[i]).Wert, wert ?? "", StringComparison.Ordinal))
                { cb.SelectedIndex = i; return; }
            cb.SelectedIndex = 0;
        }

        private static string Gewaehlt(ComboBox cb)
        {
            var w = cb.SelectedItem as Steuerwahl;
            return w != null ? w.Wert : "";
        }

        private static decimal Geklemmt(NumericUpDown num, double? wert)
        {
            if (!wert.HasValue) return num.Minimum;
            decimal d;
            try { d = Convert.ToDecimal(wert.Value); } catch { return num.Minimum; }
            return d < num.Minimum ? num.Minimum : (d > num.Maximum ? num.Maximum : d);
        }
    }
}
