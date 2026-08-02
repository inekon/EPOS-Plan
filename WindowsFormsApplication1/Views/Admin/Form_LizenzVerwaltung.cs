using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lizenzverwaltung (Administration → Lizenz): Status der Lizenz auf
    /// diesem Arbeitsplatz, Aktivierung per Lizenzschlüssel oder Lizenzdatei
    /// (.lic), Anforderung einer Testversion und Freigabe des Geräts.
    ///
    /// Bewusst vollständig programmatisch aufgebaut (kein Designer, keine
    /// .resx), analog zu InitMarke/InitKiHilfe in MDIMainForm.
    /// </summary>
    public class Form_LizenzVerwaltung : Form
    {
        private Label _statusWert;
        private Label _detailWert;
        private TextBox _schluessel;
        private TextBox _email;
        private Button _aktivieren;
        private Button _licLaden;
        private Button _trial;
        private Button _freigeben;
        private Label _hinweis;

        public Form_LizenzVerwaltung()
        {
            AufbauOberflaeche();
            StatusAnzeigen();
        }

        private void AufbauOberflaeche()
        {
            this.Text = "Lizenz — " + MDIMainForm.PRODUKTNAME;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(560, 470);
            this.Font = new Font("Segoe UI", 9f);

            int rand = 16;
            int breite = this.ClientSize.Width - 2 * rand;

            // --- Status ---------------------------------------------------
            GroupBox statusBox = new GroupBox
            {
                Text = "Lizenzstatus auf diesem Arbeitsplatz",
                Location = new Point(rand, 12),
                Size = new Size(breite, 120),
            };
            _statusWert = new Label
            {
                Location = new Point(12, 24),
                Size = new Size(statusBox.Width - 24, 22),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            };
            _detailWert = new Label
            {
                Location = new Point(12, 48),
                Size = new Size(statusBox.Width - 24, 44),
                ForeColor = Color.FromArgb(90, 96, 102),
            };
            LinkLabel portal = new LinkLabel
            {
                Text = "Lizenzportal öffnen (Benutzer und Geräte verwalten, Schlüssel neu erzeugen)",
                Location = new Point(12, 94),
                AutoSize = true,
            };
            portal.LinkClicked += (s, e) => LinkOeffnen(LizenzManager.PORTAL_URL);
            statusBox.Controls.Add(_statusWert);
            statusBox.Controls.Add(_detailWert);
            statusBox.Controls.Add(portal);

            // --- Aktivierung ----------------------------------------------
            GroupBox aktivBox = new GroupBox
            {
                Text = "Aktivieren",
                Location = new Point(rand, 144),
                Size = new Size(breite, 168),
            };
            Label schluesselLabel = new Label { Text = "Lizenzschlüssel:", Location = new Point(12, 28), AutoSize = true };
            _schluessel = new TextBox
            {
                Location = new Point(130, 25),
                Size = new Size(280, 24),
                CharacterCasing = CharacterCasing.Upper,
            };
            _licLaden = new Button
            {
                Text = "Lizenzdatei (.lic)…",
                Location = new Point(418, 24),
                Size = new Size(118, 26),
            };
            _licLaden.Click += LicLaden_Click;

            Label emailLabel = new Label { Text = "E-Mail (Benutzer):", Location = new Point(12, 62), AutoSize = true };
            _email = new TextBox
            {
                Location = new Point(130, 59),
                Size = new Size(280, 24),
            };

            _aktivieren = new Button
            {
                Text = "Jetzt aktivieren",
                Location = new Point(130, 96),
                Size = new Size(140, 30),
            };
            _aktivieren.Click += Aktivieren_Click;

            Label aktivHinweis = new Label
            {
                Text = "Die Aktivierung benötigt einmalig eine Internetverbindung. Übertragen werden nur\n" +
                       "Lizenzschlüssel, E-Mail und ein anonymer Geräte-Hash — keine Projekt- oder Kundendaten.",
                Location = new Point(12, 132),
                Size = new Size(aktivBox.Width - 24, 30),
                ForeColor = Color.FromArgb(120, 126, 132),
                Font = new Font("Segoe UI", 8f),
            };
            aktivBox.Controls.AddRange(new Control[] { schluesselLabel, _schluessel, _licLaden, emailLabel, _email, _aktivieren, aktivHinweis });

            // --- Testversion / Freigabe -----------------------------------
            GroupBox aktionenBox = new GroupBox
            {
                Text = "Weitere Aktionen",
                Location = new Point(rand, 324),
                Size = new Size(breite, 76),
            };
            _trial = new Button
            {
                Text = "Testversion anfordern…",
                Location = new Point(12, 28),
                Size = new Size(170, 30),
            };
            _trial.Click += Trial_Click;
            _freigeben = new Button
            {
                Text = "Gerät von der Lizenz lösen",
                Location = new Point(196, 28),
                Size = new Size(190, 30),
            };
            _freigeben.Click += Freigeben_Click;
            aktionenBox.Controls.Add(_trial);
            aktionenBox.Controls.Add(_freigeben);

            // --- Fußzeile -------------------------------------------------
            _hinweis = new Label
            {
                Location = new Point(rand, 408),
                Size = new Size(breite - 100, 52),
                ForeColor = Color.FromArgb(90, 96, 102),
            };
            Button schliessen = new Button
            {
                Text = "Schließen",
                Location = new Point(this.ClientSize.Width - rand - 90, 428),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel,
            };
            this.CancelButton = schliessen;

            this.Controls.AddRange(new Control[] { statusBox, aktivBox, aktionenBox, _hinweis, schliessen });
        }

        // ------------------------------------------------------------------

        private void StatusAnzeigen()
        {
            LizenzStatus status = LizenzManager.Pruefe();
            LizenzToken token = LizenzManager.Token;

            _statusWert.Text = LizenzManager.StatusText();
            _statusWert.ForeColor = (status == LizenzStatus.Gueltig) ? Color.FromArgb(0, 128, 60)
                : (status == LizenzStatus.NichtAktiviert || status == LizenzStatus.Lesemodus || status == LizenzStatus.UhrManipuliert)
                    ? Color.FromArgb(190, 40, 40)
                    : Color.FromArgb(190, 120, 0);

            if (token != null)
            {
                _detailWert.Text =
                    "Lizenz " + token.LizenzId + " · " + token.Firma + Environment.NewLine +
                    "Benutzer: " + token.Benutzer + " · Gerät: " + GeraeteId.Anzeigename();
                _freigeben.Enabled = true;
            }
            else
            {
                _detailWert.Text = "Auf diesem Arbeitsplatz ist keine Lizenz hinterlegt.";
                _freigeben.Enabled = false;
            }

            _trial.Enabled = (token == null);
        }

        private async void Aktivieren_Click(object sender, EventArgs e)
        {
            string schluessel = (_schluessel.Text ?? "").Trim();
            string email = (_email.Text ?? "").Trim();

            if (schluessel.Length == 0 || email.Length == 0)
            {
                MessageBox.Show(this, "Bitte Lizenzschlüssel und E-Mail-Adresse angeben.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!EmailGueltig(email))
            {
                MessageBox.Show(this, "Die E-Mail-Adresse \"" + email + "\" ist ungültig — bitte prüfen (Beispiel: name@firma.de).",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BedienungSperren(true, "Aktivierung läuft…");
            LizenzServerAntwort antwort = await LizenzManager.Aktivieren(schluessel, email);
            BedienungSperren(false, null);

            if (antwort.Ok)
            {
                MessageBox.Show(this, "Die Lizenz wurde erfolgreich aktiviert." + Environment.NewLine + Environment.NewLine +
                    LizenzManager.StatusText(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _schluessel.Clear();
            }
            else
            {
                MessageBox.Show(this, antwort.Meldung ?? "Die Aktivierung ist fehlgeschlagen.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            StatusAnzeigen();
        }

        private void LicLaden_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Lizenzdatei laden",
                Filter = "EPOS-Plan Lizenzdatei (*.lic)|*.lic|Alle Dateien (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            LizenzManager.LicDateiLesen(dialog.FileName, out string schluessel, out string email);
            if (string.IsNullOrWhiteSpace(schluessel))
            {
                MessageBox.Show(this, "In der gewählten Datei wurde kein gültiger Lizenzschlüssel gefunden.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _schluessel.Text = schluessel;
            if (!string.IsNullOrWhiteSpace(email)) _email.Text = email;
            _hinweis.Text = "Lizenzdatei geladen — bitte mit \"Jetzt aktivieren\" abschließen.";
        }

        private async void Trial_Click(object sender, EventArgs e)
        {
            string email = (_email.Text ?? "").Trim();
            if (email.Length == 0 || !EmailGueltig(email))
            {
                MessageBox.Show(this, "Bitte oben eine gültige E-Mail-Adresse eintragen (Beispiel: name@firma.de) — der Test-Lizenzschlüssel wird dorthin gesendet.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BedienungSperren(true, "Testversion wird angefordert…");
            LizenzServerAntwort antwort = await new LizenzServerClient().TrialAnfordern(email, Environment.UserName);
            BedienungSperren(false, null);

            MessageBox.Show(this,
                antwort.Meldung ?? (antwort.Ok ? "Der Test-Lizenzschlüssel wurde per E-Mail versandt." : "Die Anforderung ist fehlgeschlagen."),
                this.Text, MessageBoxButtons.OK, antwort.Ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async void Freigeben_Click(object sender, EventArgs e)
        {
            DialogResult wahl = MessageBox.Show(this,
                "Dieses Gerät von der Lizenz lösen?" + Environment.NewLine + Environment.NewLine +
                "Der Platz wird für ein anderes Gerät frei; zum Weiterarbeiten ist eine Neuaktivierung nötig.",
                this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (wahl != DialogResult.Yes) return;

            BedienungSperren(true, "Gerät wird freigegeben…");
            LizenzServerAntwort antwort = await LizenzManager.Freigeben();
            BedienungSperren(false, null);

            if (!antwort.Ok && antwort.NetzwerkFehler)
            {
                MessageBox.Show(this, "Der Lizenzserver ist zurzeit nicht erreichbar — bitte später erneut versuchen.",
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            StatusAnzeigen();
        }

        private void BedienungSperren(bool sperren, string text)
        {
            _aktivieren.Enabled = !sperren;
            _licLaden.Enabled = !sperren;
            _trial.Enabled = !sperren;
            _freigeben.Enabled = !sperren;
            _hinweis.Text = text ?? "";
            this.UseWaitCursor = sperren;
        }

        /// <summary>
        /// Gleiche Maßstäbe wie WordPress' is_email(): Lokalteil@Domain,
        /// Domain mit mindestens einem Punkt, keine Leer- oder Sonderzeichen.
        /// </summary>
        private static bool EmailGueltig(string email)
        {
            if (!System.Net.Mail.MailAddress.TryCreate(email, out System.Net.Mail.MailAddress adresse))
                return false;
            // WordPress verlangt einen Punkt in der Domain (name@firma ist dort ungültig)
            return adresse.Host.Contains('.') && adresse.Address == email;
        }

        private void LinkOeffnen(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Link konnte nicht geöffnet werden: " + ex.Message);
            }
        }
    }
}
