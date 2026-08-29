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
    /// Die Oberfläche steht in <c>Form_WirtschaftlichkeitVerlauf.Designer.cs</c>, weiterhin
    /// ohne eigene <c>.resx</c>: Im Designer stehen nur Platzhalter (der Feldname), die
    /// echten Texte setzt <see cref="TexteSetzen"/> unmittelbar nach
    /// <c>InitializeComponent()</c>. Nicht serialisierbar und deshalb im
    /// Konstruktor-Nachlauf: die Vorbelegung aus der Datenbank
    /// (<see cref="ParameterVorbelegen"/>), die Szenarioliste
    /// (<see cref="SzenarienFuellen"/> — das sind DB-Persistenzwerte und gehören nicht in
    /// Designer-Code) und die Deckelung auf den Arbeitsbereich
    /// (<see cref="GroesseAufArbeitsflaecheDeckeln"/>).
    /// </summary>
    public partial class Form_WirtschaftlichkeitVerlauf : Form
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

        public Form_WirtschaftlichkeitVerlauf(int idStamm, string stammName, List<int> variantenIds)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _variantenIds = variantenIds ?? new List<int>();

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Bisher stand hier AutoScaleMode.Font OHNE
            // AutoScaleDimensions, der Skalierfaktor blieb damit (1,1) — es wurde also
            // faktisch nie skaliert. Die Anwendung läuft ohnehin DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). None hält genau dieses Verhalten
            // fest und verhindert, dass ein Designer-Speichern die Skalierung erstmals
            // scharf schaltet.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
            ParameterVorbelegen();
            SzenarienFuellen();
            GroesseAufArbeitsflaecheDeckeln();

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        // -------------------------------------------------- Aufbau-Nachlauf

        /// <summary>
        /// Setzt alle sichtbaren Texte. Läuft direkt nach <c>InitializeComponent()</c> und
        /// ersetzt die dortigen Platzhalter. Die Texte sind (wie im Bestand) deutsche
        /// Literale — die Lokalisierung dieses Dialogs ist ein eigener Vorgang; hier steht
        /// nur, dass sie an genau einer Stelle liegen.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = "Kapitalwert-Verlauf über den Nutzungszeitraum — Stamm: " + _stammName;
            lblZeitraum.Text = "Zeitraum [Jahre]:";
            lblSzenario.Text = "Szenario:";
            btnZeichnen.Text = "Aktualisieren";
            btnSchliessen.Text = "Schließen";
        }

        /// <summary>
        /// Vorbelegung von <c>numJahre</c> aus den gespeicherten Parametern. Steht nicht im
        /// Designer: Das ist ein Datenbankzugriff und hängt am Konstruktorargument.
        /// </summary>
        private void ParameterVorbelegen()
        {
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            if (p.Betrachtungszeitraum >= 2 && p.Betrachtungszeitraum <= 60)
                numJahre.Value = p.Betrachtungszeitraum;
        }

        /// <summary>
        /// Füllt die Szenarioliste. Steht bewusst NICHT im Designer: Die drei Werte sind
        /// DB-Persistenzwerte (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) und dürfen
        /// nicht als Literale in Designer-Code oder gar in eine <c>.resx</c> geraten.
        /// </summary>
        private void SzenarienFuellen()
        {
            cbSzenario.Items.AddRange(new object[]
            {
                WirtschaftlichkeitSzenario.ERWARTET,
                WirtschaftlichkeitSzenario.BEST,
                WirtschaftlichkeitSzenario.WORST
            });
            cbSzenario.SelectedIndex = 0;
        }

        /// <summary>
        /// ERST nach dem Layoutlauf auf den Arbeitsbereich deckeln — dann führen die
        /// Anchors die Controls korrekt nach (Review-Verifikation 11). Die Entwurfsgröße
        /// 898 x 744 steht im Designer und gilt für den Layoutlauf.
        /// </summary>
        private void GroesseAufArbeitsflaecheDeckeln()
        {
            this.ClientSize = new Size(
                Math.Min(898, Math.Max(744, Screen.PrimaryScreen.WorkingArea.Width - 60)),
                Math.Min(744, Math.Max(513, Screen.PrimaryScreen.WorkingArea.Height - 90)));
        }

        // -------------------------------------------------- Fenster-Ereignisse

        private void Form_WirtschaftlichkeitVerlauf_Load(object sender, EventArgs e)
        {
            btnZeichnen_Click(sender, e);
        }

        private void Form_WirtschaftlichkeitVerlauf_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); _schliessenNachAbbruch = true; e.Cancel = true; }
        }

        private void Form_WirtschaftlichkeitVerlauf_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (picDiff.Image != null) picDiff.Image.Dispose();
            if (picAbsolut.Image != null) picAbsolut.Image.Dispose();
            // _tooltip wird NICHT mehr von Hand entsorgt: Er hängt jetzt als Komponente in
            // components und geht über das Standard-Dispose der Designer-Datei mit.
        }

        private void btnSchliessen_Click(object sender, EventArgs e)
        {
            if (_cts != null) _cts.Cancel(); else Close();
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
