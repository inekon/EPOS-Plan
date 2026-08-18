using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „Kapitalwert-Verlauf" (Phase 11): grafische Darstellung der
    /// Wirtschaftlichkeit über den Nutzungszeitraum — kumulierte diskontierte
    /// Zahlungsströme je Projekt (absolut) und als Differenz zur Stamm-Referenz
    /// (Nulldurchgang = dynamische Amortisation). Der Zeitraum ist frei wählbar,
    /// auch über den Betrachtungszeitraum T hinaus (Entscheidung 12.08.2026):
    /// gerechnet wird dann mit verlängertem Horizont; gespeicherte Parameter und
    /// persistierte Ergebnisse bleiben unverändert.
    ///
    /// Die Simulationsdaten (BerichtsDatenSammler) werden EINMAL beim ersten
    /// Zeichnen gesammelt und für Zeitraum-/Szenariowechsel wiederverwendet —
    /// nur die Kapitalwertrechnung läuft dann neu (schnell).
    ///
    /// Komplett im Code aufgebaut (kein Designer/.resx nötig) — Muster Form_Bericht.
    /// </summary>
    public class Form_WirtschaftlichkeitVerlauf : Form
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly List<int> _variantenIds;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private BerichtsDaten _daten;             // Cache des Simulationsstands
        private CancellationTokenSource _cts;
        private bool _schliessenNachAbbruch;      // FormClosing während laufender Rechnung

        /// <summary>true, wenn beim Sammeln der Daten neu simuliert wurde — der
        /// Aufrufer sollte dann seine Anzeige auffrischen (Review Phase 11).</summary>
        public bool DatenNeuGesammelt { get; private set; }

        private Label lblZeitraum, lblSzenario, lblStatus, lblRestwert;
        private readonly ToolTip _tooltip = new ToolTip();
        private NumericUpDown numJahre;
        private ComboBox cbSzenario;
        private Button btnZeichnen, btnSchliessen;
        private PictureBox picDiff, picAbsolut;
        private ProgressBar progress;

        public Form_WirtschaftlichkeitVerlauf(int idStamm, string stammName, List<int> variantenIds)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _variantenIds = variantenIds ?? new List<int>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new Font("Segoe UI", 9f);

            lblZeitraum = new Label { AutoSize = true, Location = new Point(12, 16),
                                      Text = "Zeitraum [Jahre]:" };
            numJahre = new NumericUpDown
            {
                Location = new Point(118, 12),
                Size = new Size(70, 23),
                Minimum = 2,
                Maximum = 60,
                Value = 20,
                TextAlign = HorizontalAlignment.Right
            };
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            if (p.Betrachtungszeitraum >= 2 && p.Betrachtungszeitraum <= 60)
                numJahre.Value = p.Betrachtungszeitraum;

            lblSzenario = new Label { AutoSize = true, Location = new Point(206, 16),
                                      Text = "Szenario:" };
            cbSzenario = new ComboBox
            {
                Location = new Point(272, 12),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbSzenario.Items.AddRange(new object[]
            {
                WirtschaftlichkeitSzenario.ERWARTET,
                WirtschaftlichkeitSzenario.BEST,
                WirtschaftlichkeitSzenario.WORST
            });
            cbSzenario.SelectedIndex = 0;

            btnZeichnen = new Button
            {
                Location = new Point(418, 10),
                Size = new Size(110, 27),
                Text = "Aktualisieren"
            };
            btnZeichnen.Click += new EventHandler(btnZeichnen_Click);

            btnSchliessen = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(786, 10),
                Size = new Size(100, 27),
                Text = "Schließen"
            };
            btnSchliessen.Click += (s, e) => { if (_cts != null) _cts.Cancel(); else Close(); };

            picDiff = new PictureBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(12, 46),
                Size = new Size(874, 320),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            picAbsolut = new PictureBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(12, 372),
                Size = new Size(874, 316),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            lblRestwert = new Label
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.DimGray,
                Location = new Point(12, 694),
                Size = new Size(874, 30),
                AutoEllipsis = true
            };
            lblStatus = new Label
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                ForeColor = Color.DimGray,
                Location = new Point(12, 726),
                Size = new Size(600, 18)
            };
            progress = new ProgressBar
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(636, 728),
                Size = new Size(250, 14),
                Visible = false
            };

            this.ClientSize = new Size(898, 744);   // Designgröße für den Layoutlauf
            this.MinimumSize = new Size(760, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_WirtschaftlichkeitVerlauf";
            this.Text = "Kapitalwert-Verlauf über den Nutzungszeitraum — Stamm: " + _stammName;
            this.Controls.Add(lblZeitraum);
            this.Controls.Add(numJahre);
            this.Controls.Add(lblSzenario);
            this.Controls.Add(cbSzenario);
            this.Controls.Add(btnZeichnen);
            this.Controls.Add(btnSchliessen);
            this.Controls.Add(picDiff);
            this.Controls.Add(picAbsolut);
            this.Controls.Add(lblRestwert);
            this.Controls.Add(lblStatus);
            this.Controls.Add(progress);
            this.Load += (s, e) => btnZeichnen_Click(s, e);
            this.FormClosing += (s, e) =>
            {
                if (_cts != null) { _cts.Cancel(); _schliessenNachAbbruch = true; e.Cancel = true; }
            };
            this.FormClosed += (s, e) =>
            {
                if (picDiff.Image != null) picDiff.Image.Dispose();
                if (picAbsolut.Image != null) picAbsolut.Image.Dispose();
                _tooltip.Dispose();
            };
            this.ResumeLayout(false);
            this.PerformLayout();

            // ERST nach dem Layoutlauf auf den Arbeitsbereich deckeln — dann
            // führen die Anchors die Controls korrekt nach (Review-Verifikation 11).
            this.ClientSize = new Size(
                Math.Min(898, Math.Max(744, Screen.PrimaryScreen.WorkingArea.Width - 60)),
                Math.Min(744, Math.Max(513, Screen.PrimaryScreen.WorkingArea.Height - 90)));

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        // ------------------------------------------------------------- Zeichnen

        private async void btnZeichnen_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;
            int jahre = (int)numJahre.Value;
            string szenario = cbSzenario.SelectedItem as string ?? WirtschaftlichkeitSzenario.ERWARTET;

            _cts = new CancellationTokenSource();
            SetBusy(true);
            var melder = new Progress<BerichtsDatenSammler.Fortschritt>(f =>
            {
                if (f.Gesamt > 0)
                {
                    progress.Maximum = f.Gesamt;
                    progress.Value = Math.Min(f.Aktuell, f.Gesamt);
                }
                lblStatus.Text = string.Format("({0}/{1}) {2}", f.Aktuell, f.Gesamt, f.Text);
            });

            try
            {
                CancellationToken ct = _cts.Token;
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter tarif = _ctrl.LadeTarif(_idStamm);
                bool mitZeitreihen = tarif.Aktiv || p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;

                bool warGecacht = _daten != null;
                WirtschaftlichkeitVerlauf verlauf = await Task.Run(() =>
                {
                    if (_daten == null)   // Simulationsdaten nur einmal sammeln
                        _daten = new BerichtsDatenSammler().Sammle(
                            _idStamm, _stammName, _variantenIds,
                            false, mitZeitreihen, melder, ct);
                    return _ctrl.BerechneVerlauf(_daten, p, jahre, szenario);
                }, ct);
                // Exaktes Kriterium: der Sammler markiert neu simulierte (und damit
                // neu persistierte) Projekte selbst (Review-Verifikation 11).
                if (!warGecacht && _daten != null &&
                    _daten.Varianten.Any(v => v.FrischSimuliert))
                    DatenNeuGesammelt = true;

                ZeigeDiagramme(verlauf);
                lblStatus.Text = "Verlauf über " + jahre + " Jahre, Szenario „" + szenario + "“" +
                                 (jahre != p.Betrachtungszeitraum
                                  ? " (abweichend von T = " + p.Betrachtungszeitraum + " a — nur Anzeige, " +
                                    "gespeicherte Ergebnisse unverändert" +
                                    (jahre > p.Betrachtungszeitraum
                                     ? "; Nulldurchgänge jenseits von T erscheinen nicht in der " +
                                       "gespeicherten Amortisationskennzahl"
                                     : "") + ")."
                                  : ".");
            }
            catch (OperationCanceledException) { lblStatus.Text = "Vorgang abgebrochen."; }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Berechnen des Verlaufs: " + ex.Message,
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetBusy(false);
                if (_schliessenNachAbbruch) Close();
            }
        }

        private void ZeigeDiagramme(WirtschaftlichkeitVerlauf verlauf)
        {
            var kultur = BerichtTexte.Kultur;

            SetzeBild(picDiff, ChartRenderer.KapitalwertVerlauf(
                "Kapitalwert-Verlauf: Differenz zur Stamm-Referenz",
                ChartRenderer.VerlaufsReihen(verlauf.Differenz, false),
                "Kumulierte diskontierte Differenz-Zahlungsströme Variante − Stamm; " +
                "Schnitt mit der Nulllinie = dynamische Amortisation. Ohne Restwert."));

            SetzeBild(picAbsolut, ChartRenderer.KapitalwertVerlauf(
                "Kapitalwert-Verlauf: kumulierte Barwerte je Projekt",
                ChartRenderer.VerlaufsReihen(verlauf.Absolut, true),
                "Kumulierte diskontierte Zahlungsströme (Kosten negativ). " +
                "Ohne Restwert — Nettobarwert = Endwert + Restwert-Barwert."));

            // Restwerte am gewählten Horizont ausweisen (Reihen sind ohne Restwert).
            var teile = new List<string>();
            foreach (VerlaufSerie s in verlauf.Absolut)
                if (s.Kumuliert != null && Math.Abs(s.RestwertBarwert) > 0.5)
                    teile.Add(s.Anzeige + " " + s.RestwertBarwert.ToString("N0", kultur) + " €");
            lblRestwert.Text = teile.Count > 0
                ? "Restwert-Barwerte am Horizontende (nicht in den Linien enthalten): " +
                  string.Join(" · ", teile)
                : "";

            // Nicht berechenbare Projekte offen ausweisen.
            var fehler = verlauf.Absolut.Where(s => s.Fehlgrund != null).ToList();
            if (fehler.Count > 0)
                lblRestwert.Text = ("⚠ Ohne Reihe: " + string.Join("; ",
                    fehler.Select(s => s.Anzeige + " (" + s.Fehlgrund + ")")) + "   " +
                    lblRestwert.Text).Trim();
            _tooltip.SetToolTip(lblRestwert, lblRestwert.Text);   // Volltext bei Abschneiden
        }

        private static Image Bild(byte[] png)
        {
            // Image.FromStream verlangt einen offenen Stream über die gesamte
            // Lebensdauer — daher Kopie in eine eigenständige Bitmap (Review 11).
            using (var ms = new MemoryStream(png))
            using (var img = Image.FromStream(ms))
                return new Bitmap(img);
        }

        /// <summary>Bild einer PictureBox ersetzen und das alte freigeben
        /// (unmanaged GDI+-Speicher, x86-Prozess — Review Phase 11).</summary>
        private static void SetzeBild(PictureBox pic, byte[] png)
        {
            Image alt = pic.Image;
            pic.Image = Bild(png);
            if (alt != null) alt.Dispose();
        }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            if (!busy) progress.Value = 0;
            numJahre.Enabled = !busy;
            cbSzenario.Enabled = !busy;
            btnZeichnen.Enabled = !busy;
            btnSchliessen.Text = busy ? "Abbrechen" : "Schließen";
            this.UseWaitCursor = busy;
        }
    }
}
