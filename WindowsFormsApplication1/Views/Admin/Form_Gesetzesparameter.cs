using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Pflegemaske „Gesetzliche Parameter" für <c>Tab_Gesetzesparameter</c>
    /// (Konzept_BHKW_Kosten_Erloese.md, Abschnitt 6, Etappe E1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Die Kernregel steht sichtbar auf der Maske.</b> Eine Gesetzesänderung ist
    /// eine NEUE Jahreszeile, kein Ändern der alten — sonst lässt sich eine 2026
    /// gerechnete Variante 2029 nicht mehr reproduzieren. Wer eine Zeile bearbeitet,
    /// deren <c>JahrVon</c> in der Vergangenheit liegt, wird deshalb gefragt, und die
    /// VORGABE der Rückfrage ist „neue Zeile anlegen". Das Ändern der alten Zeile
    /// bleibt möglich — für Tippfehler —, aber es ist die bewusste Ausnahme.
    /// </para>
    /// <para>
    /// <b>Ein leeres Wertfeld ist kein Nullwert.</b> Es bedeutet „der Satz ist
    /// entfallen" (Verdrängungsstrommix ab 2027) und wird als NULL gespeichert; die
    /// Lesefassade liefert dafür <c>null</c>, nicht 0.
    /// </para>
    /// <para>
    /// Vollständig programmatisch, ohne Designer und ohne eigene <c>.resx</c> —
    /// Muster <c>Form_SpotpreisImport</c>. Alle Anzeigetexte über <c>MyResource</c>,
    /// alle Datenbankwerte über <c>DbWerte.GESETZ_*</c>.
    /// </para>
    /// </remarks>
    public class Form_Gesetzesparameter : Form
    {
        private readonly GesetzKatalog _katalog = new GesetzKatalog();

        private ComboBox _cbKlasse;
        private ListView _lv;
        private Button _btnNeu;
        private Button _btnAendern;
        private Button _btnLoeschen;

        /// <summary>
        /// Rückfrage „neue Jahreszeile anlegen?" — Rückgabe <c>Yes</c> = neue Zeile,
        /// <c>No</c> = bestehende Zeile ändern, <c>Cancel</c> = abbrechen.
        /// Im Test überschreibbar, damit der Reflection-Harness beide Antworten
        /// prüfen kann, ohne auf eine modale MessageBox angewiesen zu sein.
        /// </summary>
        internal Func<GesetzParameter, DialogResult> FrageNeueZeile { get; set; }

        /// <summary>Rückfrage vor dem Löschen; im Test überschreibbar.</summary>
        internal Func<GesetzParameter, DialogResult> FrageLoeschen { get; set; }

        /// <summary>Zeilendialog; im Test überschreibbar (liefert null = Abbruch).</summary>
        internal Func<GesetzParameter, bool, GesetzParameter> ZeileBearbeiten { get; set; }

        public Form_Gesetzesparameter()
        {
            GesetzKatalog.StelleKatalogSicher();
            BaueOberflaeche();
            KlassenFuellen();
            Aktualisieren();
        }

        // ==================================================================
        // Oberfläche
        // ==================================================================

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.GESETZ_TITEL;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.ClientSize = new Size(940, 560);
            this.MinimumSize = new Size(760, 420);

            Label lblHinweis = new Label
            {
                Text = MyResource.Resource.GESETZ_LBL_HINWEIS,
                Location = new Point(12, 10),
                Size = new Size(916, 34),
                AutoSize = false,
                ForeColor = Color.FromArgb(0, 90, 160)
            };
            this.Controls.Add(lblHinweis);

            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.GESETZ_LBL_KLASSE,
                Location = new Point(12, 54),
                AutoSize = true
            });

            _cbKlasse = new ComboBox
            {
                Name = "cbKlasse",
                Location = new Point(90, 51),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbKlasse.SelectedIndexChanged += cbKlasse_SelectedIndexChanged;
            this.Controls.Add(_cbKlasse);

            _lv = new ListView
            {
                Name = "lvZeilen",
                Location = new Point(12, 84),
                Size = new Size(916, 424),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_SCHLUESSEL, 300);
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_JAHRVON, 70, HorizontalAlignment.Right);
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_WERT, 90, HorizontalAlignment.Right);
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_EINHEIT, 80);
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_STATUS, 90);
            _lv.Columns.Add(MyResource.Resource.GESETZ_SP_QUELLE, 270);
            _lv.DoubleClick += btnAendern_Click;
            this.Controls.Add(_lv);

            _btnNeu = new Button
            {
                Name = "btnNeu",
                Text = MyResource.Resource.GESETZ_BTN_NEU,
                Location = new Point(12, 520),
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnNeu.Click += btnNeu_Click;
            this.Controls.Add(_btnNeu);

            _btnAendern = new Button
            {
                Name = "btnAendern",
                Text = MyResource.Resource.GESETZ_BTN_AENDERN,
                Location = new Point(130, 520),
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnAendern.Click += btnAendern_Click;
            this.Controls.Add(_btnAendern);

            _btnLoeschen = new Button
            {
                Name = "btnLoeschen",
                Text = MyResource.Resource.GESETZ_BTN_LOESCHEN,
                Location = new Point(248, 520),
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnLoeschen.Click += btnLoeschen_Click;
            this.Controls.Add(_btnLoeschen);

            Button btnSchliessen = new Button
            {
                Name = "btnSchliessen",
                Text = MyResource.Resource.GESETZ_BTN_SCHLIESSEN,
                Location = new Point(818, 520),
                Width = 110,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnSchliessen.Click += delegate { this.Close(); };
            this.Controls.Add(btnSchliessen);
            this.CancelButton = btnSchliessen;
        }

        /// <summary>
        /// Trägt den DB-Wert und zeigt den lokalisierten Namen — kein Anzeigetext ist
        /// je Steuerwert (Drei-Schichten-Regel).
        /// </summary>
        internal sealed class KlasseItem
        {
            public KlasseItem(string wert, string anzeige) { Wert = wert; Anzeige = anzeige; }
            public string Wert { get; private set; }
            public string Anzeige { get; private set; }
            public override string ToString() { return Anzeige; }
        }

        private void KlassenFuellen()
        {
            _cbKlasse.Items.Clear();
            foreach (string k in _katalog.Klassen())
                _cbKlasse.Items.Add(new KlasseItem(k, KlasseAnzeige(k)));
            if (_cbKlasse.Items.Count > 0) _cbKlasse.SelectedIndex = 0;
        }

        /// <summary>Anzeigename einer Klasse; unbekannte Klassen zeigen ihren Rohwert.</summary>
        internal static string KlasseAnzeige(string klasse)
        {
            switch (klasse)
            {
                case DbWerte.GESETZ_KLASSE_KWKG: return MyResource.Resource.GESETZ_KLASSE_ANZ_KWKG;
                case DbWerte.GESETZ_KLASSE_STROMSTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_STROMSTEUER;
                case DbWerte.GESETZ_KLASSE_ENERGIESTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_ENERGIESTEUER;
                case DbWerte.GESETZ_KLASSE_CO2_PREIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_CO2_PREIS;
                case DbWerte.GESETZ_KLASSE_EF_NACHWEIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_EF_NACHWEIS;
                case DbWerte.GESETZ_KLASSE_EF_BILANZ: return MyResource.Resource.GESETZ_KLASSE_ANZ_EF_BILANZ;
                case DbWerte.GESETZ_KLASSE_PEF_NACHWEIS: return MyResource.Resource.GESETZ_KLASSE_ANZ_PEF_NACHWEIS;
                case DbWerte.GESETZ_KLASSE_UMSATZSTEUER: return MyResource.Resource.GESETZ_KLASSE_ANZ_UMSATZSTEUER;
                default: return klasse;
            }
        }

        /// <summary>Die gewählte Klasse als DB-Wert; leer, wenn nichts gewählt ist.</summary>
        internal string GewaehlteKlasse
        {
            get
            {
                KlasseItem i = _cbKlasse.SelectedItem as KlasseItem;
                return i == null ? "" : i.Wert;
            }
            set
            {
                for (int i = 0; i < _cbKlasse.Items.Count; i++)
                    if (((KlasseItem)_cbKlasse.Items[i]).Wert == value) { _cbKlasse.SelectedIndex = i; return; }
            }
        }

        // ==================================================================
        // Liste
        // ==================================================================

        /// <summary>Liest den Katalog neu und füllt die Liste der gewählten Klasse.</summary>
        internal void Aktualisieren()
        {
            _katalog.Neuladen();
            string klasse = GewaehlteKlasse;
            _lv.BeginUpdate();
            _lv.Items.Clear();
            if (klasse.Length > 0)
                foreach (GesetzParameter p in _katalog.AlleDerKlasse(klasse))
                {
                    ListViewItem it = new ListViewItem(p.Schluessel);
                    it.SubItems.Add(p.JahrVon.ToString(CultureInfo.CurrentCulture));
                    it.SubItems.Add(WertText(p.Wert));
                    it.SubItems.Add(p.Einheit);
                    it.SubItems.Add(p.Status);
                    it.SubItems.Add(p.Quelle);
                    it.Tag = p;
                    _lv.Items.Add(it);
                }
            _lv.EndUpdate();

            bool etwasDa = _lv.Items.Count > 0;
            _btnAendern.Enabled = etwasDa;
            _btnLoeschen.Enabled = etwasDa;
        }

        /// <summary>Anzeige des Werts; ein leerer Text steht für „Satz entfallen".</summary>
        internal static string WertText(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "";
        }

        /// <summary>Anzahl der gerade angezeigten Zeilen (Prüfhilfe des Harness).</summary>
        internal int ZeilenAnzahl { get { return _lv.Items.Count; } }

        /// <summary>Die ausgewählte Zeile; null, wenn nichts markiert ist.</summary>
        internal GesetzParameter Auswahl
        {
            get
            {
                if (_lv.SelectedItems.Count == 0) return null;
                return _lv.SelectedItems[0].Tag as GesetzParameter;
            }
        }

        /// <summary>Markiert die Zeile mit Schlüssel und Jahr; liefert false, wenn es sie nicht gibt.</summary>
        internal bool Waehle(string schluessel, int jahrVon)
        {
            _ = _lv.Handle;      // ohne Handle greift die ListView-Auswahl nicht
            foreach (ListViewItem it in _lv.Items)
            {
                GesetzParameter p = it.Tag as GesetzParameter;
                if (p != null && p.Schluessel == schluessel && p.JahrVon == jahrVon)
                {
                    it.Selected = true;
                    it.Focused = true;
                    return true;
                }
            }
            return false;
        }

        private void cbKlasse_SelectedIndexChanged(object sender, EventArgs e)
        {
            Aktualisieren();
        }

        // ==================================================================
        // Anlegen, Ändern, Löschen
        // ==================================================================

        private void btnNeu_Click(object sender, EventArgs e)
        {
            GesetzParameter vorlage = new GesetzParameter(
                0, "", GewaehlteKlasse.Length > 0 ? GewaehlteKlasse : DbWerte.GESETZ_KLASSE_KWKG,
                DateTime.Today.Year, null, DbWerte.GESETZ_EINHEIT_OHNE,
                DbWerte.GESETZ_STATUS_GESICHERT, "");

            GesetzParameter neu = Dialog(vorlage, true);
            if (neu == null) return;
            if (!PruefeNeu(neu, 0)) return;

            if (GesetzKatalog.Anlegen(neu.Schluessel, neu.Klasse, neu.JahrVon, neu.Wert,
                                      neu.Einheit, neu.Status, neu.Quelle) == 0)
                Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);

            KlassenErgaenzen(neu.Klasse);
            GewaehlteKlasse = neu.Klasse;
            Aktualisieren();
            Waehle(neu.Schluessel, neu.JahrVon);
        }

        private void btnAendern_Click(object sender, EventArgs e)
        {
            GesetzParameter alt = Auswahl;
            if (alt == null) return;

            GesetzParameter bearbeitet = Dialog(alt, false);
            if (bearbeitet == null) return;

            // Kernregel: liegt das Gültig-ab-Jahr der BESTEHENDEN Zeile in der
            // Vergangenheit, ist eine Änderung im Regelfall eine Gesetzesänderung —
            // und die gehört in eine neue Jahreszeile.
            bool alsNeueZeile = false;
            if (alt.JahrVon < DateTime.Today.Year)
            {
                DialogResult antwort = FrageNeueZeile != null
                    ? FrageNeueZeile(alt)
                    : MessageBox.Show(
                        string.Format(MyResource.Resource.GESETZ_FRAGE_NEUE_ZEILE,
                                      alt.Schluessel, alt.JahrVon),
                        MyResource.Resource.GESETZ_FRAGE_TITEL,
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                if (antwort == DialogResult.Cancel) return;
                alsNeueZeile = antwort == DialogResult.Yes;
            }

            if (alsNeueZeile)
            {
                if (!PruefeNeu(bearbeitet, 0)) return;
                if (GesetzKatalog.Anlegen(bearbeitet.Schluessel, bearbeitet.Klasse, bearbeitet.JahrVon,
                                          bearbeitet.Wert, bearbeitet.Einheit, bearbeitet.Status,
                                          bearbeitet.Quelle) == 0)
                    Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            }
            else
            {
                if (!PruefeNeu(bearbeitet, alt.Id)) return;
                if (!GesetzKatalog.Aendern(alt.Id, bearbeitet.JahrVon, bearbeitet.Wert,
                                           bearbeitet.Einheit, bearbeitet.Status, bearbeitet.Quelle))
                    Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            }

            Aktualisieren();
            Waehle(bearbeitet.Schluessel, bearbeitet.JahrVon);
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            GesetzParameter p = Auswahl;
            if (p == null) return;

            DialogResult antwort = FrageLoeschen != null
                ? FrageLoeschen(p)
                : MessageBox.Show(
                    string.Format(MyResource.Resource.GESETZ_FRAGE_LOESCHEN, p.Schluessel, p.JahrVon),
                    MyResource.Resource.GESETZ_LOESCHEN_TITEL,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
            if (antwort != DialogResult.Yes) return;

            if (!GesetzKatalog.Loeschen(p.Id))
                Melden(MyResource.Resource.GESETZ_MSG_SPEICHERN_FEHLER);
            Aktualisieren();
        }

        private GesetzParameter Dialog(GesetzParameter vorlage, bool istNeu)
        {
            if (ZeileBearbeiten != null) return ZeileBearbeiten(vorlage, istNeu);
            using (Form_GesetzparameterZeile dlg = new Form_GesetzparameterZeile(vorlage, istNeu))
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.Ergebnis : null;
        }

        /// <summary>
        /// Prüft Pflichtangaben und die Eindeutigkeit von Schlüssel plus Jahr.
        /// <paramref name="eigeneId"/> nimmt die gerade bearbeitete Zeile aus der
        /// Dublettenprüfung heraus.
        /// </summary>
        private bool PruefeNeu(GesetzParameter p, int eigeneId)
        {
            if (p.Schluessel.Length == 0)
            {
                Melden(MyResource.Resource.GESETZ_MSG_SCHLUESSEL_FEHLT);
                return false;
            }
            if (p.JahrVon < 1990 || p.JahrVon > 2100)
            {
                Melden(MyResource.Resource.GESETZ_MSG_JAHR_UNGUELTIG);
                return false;
            }

            GesetzKatalog frisch = new GesetzKatalog();
            foreach (GesetzParameter v in frisch.AlleDerKlasse(p.Klasse))
                if (v.Schluessel == p.Schluessel && v.JahrVon == p.JahrVon && v.Id != eigeneId)
                {
                    Melden(string.Format(MyResource.Resource.GESETZ_MSG_DOPPELT,
                                         p.Schluessel, p.JahrVon));
                    return false;
                }
            return true;
        }

        /// <summary>Nimmt eine noch nicht gelistete Klasse in die Auswahl auf.</summary>
        private void KlassenErgaenzen(string klasse)
        {
            if (klasse.Length == 0) return;
            foreach (object o in _cbKlasse.Items)
                if (((KlasseItem)o).Wert == klasse) return;
            _cbKlasse.Items.Add(new KlasseItem(klasse, KlasseAnzeige(klasse)));
        }

        /// <summary>Meldungskanal; im Test überschreibbar.</summary>
        internal Action<string> Meldung { get; set; }

        private void Melden(string text)
        {
            if (Meldung != null) { Meldung(text); return; }
            MessageBox.Show(text, MyResource.Resource.GESETZ_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Zeilendialog der Pflegemaske. Schlüssel und Klasse sind beim ÄNDERN gesperrt:
    /// Sie sind die Identität der Reihe und in der Datenbank eingefroren — wer sie
    /// ändern will, legt eine neue Zeile an und löscht die alte.
    /// </summary>
    public class Form_GesetzparameterZeile : Form
    {
        private readonly bool _istNeu;
        private readonly int _id;
        private TextBox _tbSchluessel;
        private ComboBox _cbKlasse;
        private TextBox _tbJahr;
        private TextBox _tbWert;
        private ComboBox _cbEinheit;
        private ComboBox _cbStatus;
        private TextBox _tbQuelle;

        /// <summary>Die eingegebene Zeile; erst nach <c>DialogResult.OK</c> gefüllt.</summary>
        public GesetzParameter Ergebnis { get; private set; }

        public Form_GesetzparameterZeile(GesetzParameter vorlage, bool istNeu)
        {
            _istNeu = istNeu;
            _id = vorlage == null ? 0 : vorlage.Id;
            BaueOberflaeche();
            Uebernehmen(vorlage);
        }

        private void BaueOberflaeche()
        {
            this.Text = _istNeu
                ? MyResource.Resource.GESETZ_DLG_TITEL_NEU
                : MyResource.Resource.GESETZ_DLG_TITEL_AENDERN;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(620, 260);

            int y = 14;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_SP_SCHLUESSEL, y));
            _tbSchluessel = new TextBox
            {
                Name = "tbSchluessel",
                Location = new Point(160, y - 3),
                Width = 440,
                ReadOnly = !_istNeu,
                CharacterCasing = CharacterCasing.Upper
            };
            this.Controls.Add(_tbSchluessel);

            y += 32;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_LBL_KLASSE, y));
            _cbKlasse = new ComboBox
            {
                Name = "cbKlasse",
                Location = new Point(160, y - 3),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = _istNeu
            };
            foreach (string k in Klassen())
                _cbKlasse.Items.Add(new Form_Gesetzesparameter.KlasseItem(
                    k, Form_Gesetzesparameter.KlasseAnzeige(k)));
            this.Controls.Add(_cbKlasse);

            y += 32;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_SP_JAHRVON, y));
            _tbJahr = new TextBox { Name = "tbJahr", Location = new Point(160, y - 3), Width = 80 };
            this.Controls.Add(_tbJahr);

            y += 32;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_SP_WERT, y));
            _tbWert = new TextBox { Name = "tbWert", Location = new Point(160, y - 3), Width = 120 };
            this.Controls.Add(_tbWert);
            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.GESETZ_LBL_WERT_LEER,
                Location = new Point(290, y),
                AutoSize = true,
                ForeColor = SystemColors.GrayText
            });

            y += 32;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_SP_EINHEIT, y));
            _cbEinheit = new ComboBox
            {
                Name = "cbEinheit",
                Location = new Point(160, y - 3),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbEinheit.Items.AddRange(Einheiten());
            this.Controls.Add(_cbEinheit);

            this.Controls.Add(new Label
            {
                Text = MyResource.Resource.GESETZ_SP_STATUS,
                Location = new Point(330, y),
                AutoSize = true
            });
            _cbStatus = new ComboBox
            {
                Name = "cbStatus",
                Location = new Point(400, y - 3),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbStatus.Items.AddRange(new object[]
            {
                DbWerte.GESETZ_STATUS_GESICHERT,
                DbWerte.GESETZ_STATUS_VORLAEUFIG,
                DbWerte.GESETZ_STATUS_PROGNOSE
            });
            this.Controls.Add(_cbStatus);

            y += 32;
            this.Controls.Add(Beschriftung(MyResource.Resource.GESETZ_SP_QUELLE, y));
            _tbQuelle = new TextBox
            {
                Name = "tbQuelle",
                Location = new Point(160, y - 3),
                Width = 440,
                MaxLength = 120
            };
            this.Controls.Add(_tbQuelle);

            Button btnOk = new Button
            {
                Name = "btnOk",
                Text = MyResource.Resource.GESETZ_BTN_UEBERNEHMEN,
                Location = new Point(400, 210),
                Width = 96
            };
            btnOk.Click += btnOk_Click;
            this.Controls.Add(btnOk);
            this.AcceptButton = btnOk;

            Button btnAbbruch = new Button
            {
                Name = "btnAbbruch",
                Text = MyResource.Resource.GESETZ_BTN_ABBRECHEN,
                Location = new Point(504, 210),
                Width = 96,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnAbbruch);
            this.CancelButton = btnAbbruch;
        }

        private static Label Beschriftung(string text, int y)
        {
            return new Label { Text = text, Location = new Point(12, y), AutoSize = true };
        }

        private static string[] Klassen()
        {
            return new string[]
            {
                DbWerte.GESETZ_KLASSE_KWKG,
                DbWerte.GESETZ_KLASSE_STROMSTEUER,
                DbWerte.GESETZ_KLASSE_ENERGIESTEUER,
                DbWerte.GESETZ_KLASSE_CO2_PREIS,
                DbWerte.GESETZ_KLASSE_EF_NACHWEIS,
                DbWerte.GESETZ_KLASSE_EF_BILANZ,
                DbWerte.GESETZ_KLASSE_PEF_NACHWEIS,
                DbWerte.GESETZ_KLASSE_UMSATZSTEUER
            };
        }

        /// <summary>
        /// Die zulässigen Einheiten — feste Liste, damit niemand „EUR/MWh" einmal so
        /// und einmal anders schreibt (L3).
        /// </summary>
        internal static object[] Einheiten()
        {
            return new object[]
            {
                DbWerte.GESETZ_EINHEIT_EUR_MWH,
                DbWerte.GESETZ_EINHEIT_EUR_1000L,
                DbWerte.GESETZ_EINHEIT_EUR_1000KG,
                DbWerte.GESETZ_EINHEIT_EUR_GJ,
                DbWerte.GESETZ_EINHEIT_EUR_T,
                DbWerte.GESETZ_EINHEIT_EUR_A,
                DbWerte.GESETZ_EINHEIT_CT_KWH,
                DbWerte.GESETZ_EINHEIT_G_KWH,
                DbWerte.GESETZ_EINHEIT_GJ_MWH,
                DbWerte.GESETZ_EINHEIT_H,
                DbWerte.GESETZ_EINHEIT_KW,
                DbWerte.GESETZ_EINHEIT_KM,
                DbWerte.GESETZ_EINHEIT_PROZENT,
                DbWerte.GESETZ_EINHEIT_JAHR,
                DbWerte.GESETZ_EINHEIT_OHNE
            };
        }

        private void Uebernehmen(GesetzParameter p)
        {
            if (p == null) return;
            _tbSchluessel.Text = p.Schluessel;
            _tbJahr.Text = p.JahrVon.ToString(CultureInfo.CurrentCulture);
            _tbWert.Text = Form_Gesetzesparameter.WertText(p.Wert);
            _tbQuelle.Text = p.Quelle;
            WaehleText(_cbEinheit, p.Einheit, DbWerte.GESETZ_EINHEIT_OHNE);
            WaehleText(_cbStatus, p.Status, DbWerte.GESETZ_STATUS_GESICHERT);
            for (int i = 0; i < _cbKlasse.Items.Count; i++)
                if (((Form_Gesetzesparameter.KlasseItem)_cbKlasse.Items[i]).Wert == p.Klasse)
                { _cbKlasse.SelectedIndex = i; break; }
            if (_cbKlasse.SelectedIndex < 0 && _cbKlasse.Items.Count > 0) _cbKlasse.SelectedIndex = 0;
        }

        private static void WaehleText(ComboBox cb, string wert, string ersatz)
        {
            int i = cb.Items.IndexOf(wert ?? "");
            if (i < 0) i = cb.Items.IndexOf(ersatz);
            cb.SelectedIndex = i < 0 ? (cb.Items.Count > 0 ? 0 : -1) : i;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            int jahr;
            if (!int.TryParse((_tbJahr.Text ?? "").Trim(), NumberStyles.Integer,
                              CultureInfo.CurrentCulture, out jahr) || jahr < 1990 || jahr > 2100)
            {
                MessageBox.Show(MyResource.Resource.GESETZ_MSG_JAHR_UNGUELTIG,
                                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Leeres Feld = der Satz ist entfallen; das ist etwas anderes als 0.
            double? wert = null;
            string roh = (_tbWert.Text ?? "").Trim();
            if (roh.Length > 0)
            {
                double w;
                if (!double.TryParse(roh, NumberStyles.Float, CultureInfo.CurrentCulture, out w) &&
                    !double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                {
                    MessageBox.Show(MyResource.Resource.GESETZ_MSG_WERT_UNGUELTIG,
                                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                wert = w;
            }

            string schluessel = (_tbSchluessel.Text ?? "").Trim();
            if (schluessel.Length == 0)
            {
                MessageBox.Show(MyResource.Resource.GESETZ_MSG_SCHLUESSEL_FEHLT,
                                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form_Gesetzesparameter.KlasseItem ki =
                _cbKlasse.SelectedItem as Form_Gesetzesparameter.KlasseItem;
            Ergebnis = new GesetzParameter(
                _id, schluessel,
                ki == null ? DbWerte.GESETZ_KLASSE_KWKG : ki.Wert,
                jahr, wert,
                _cbEinheit.SelectedItem == null ? DbWerte.GESETZ_EINHEIT_OHNE : _cbEinheit.SelectedItem.ToString(),
                _cbStatus.SelectedItem == null ? DbWerte.GESETZ_STATUS_GESICHERT : _cbStatus.SelectedItem.ToString(),
                (_tbQuelle.Text ?? "").Trim());

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
