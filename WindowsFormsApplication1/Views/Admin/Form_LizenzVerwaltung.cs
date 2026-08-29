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
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Oberfläche steht in <c>Form_LizenzVerwaltung.Designer.cs</c> — bis zur
    /// Umstellung war sie vollständig programmatisch aufgebaut (analog zu
    /// InitMarke/InitKiHilfe in MDIMainForm). Eine eigene <c>.resx</c> gibt es
    /// weiterhin nicht: Alle sichtbaren Texte kommen aus <c>MyResource</c> und
    /// werden in <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur
    /// Platzhalter.
    /// </para>
    /// <para>
    /// Der Aufbau ist logikfrei; <see cref="StatusAnzeigen"/> läuft danach und
    /// füllt Status, Detailzeile und die Bedienbarkeit der beiden Schaltflächen
    /// unter „Weitere Aktionen" — diese Reihenfolge ist unverändert.
    /// </para>
    /// </remarks>
    public partial class Form_LizenzVerwaltung : Form
    {
        public Form_LizenzVerwaltung()
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
            StatusAnzeigen();

            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Oberfläche — Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen in Form_LizenzVerwaltung.Designer.cs.
        // Designer-Code trägt keine Kommentare; die Pixelentscheidungen stehen
        // deshalb hier (Muster Form_PufferSp_Projekt).
        //
        // DESIGN-POLITUR 21.08.2026
        // * Im Designer stehen jetzt die deutschen ECHTTEXTE statt der Feldnamen.
        //   TexteSetzen() überschreibt sie beim Start unverändert.
        //   _statusWert und _detailWert bekommen zusätzlich einen Musterinhalt —
        //   beide füllt StatusAnzeigen() noch im Konstruktor, der Entwurfstext ist
        //   also reine Entwurfszeit-Anschauung. _hinweis bleibt BEWUSST leer: Die
        //   Fußzeile meldet nur laufende Vorgänge und ist beim Öffnen leer; ein
        //   Entwurfstext stünde dort bis zur ersten Aktion sichtbar herum.
        // * _statusWert 504 x 22 -> 504 x 40 (Lage 24 -> 22). Der längste Status
        //   („Die Systemuhr wurde zurückgestellt — …", Segoe UI Semibold 9,5 pt)
        //   misst 526 px und passte damit NICHT in die 504 px; einzeilig wurde er
        //   abgeschnitten. Zwei Zeilen brauchen 39 px.
        // * _detailWert 504 x 44 -> 504 x 38 an y = 64 (vorher 48): Der Zweizeiler
        //   aus LIZ_DETAIL braucht 37 px, die frei werdenden 6 px gehen an den
        //   Statuswert. _portal rückt auf y = 105 — als AutoSize-LinkLabel ist es
        //   21 px hoch, nicht 15 —, _statusBox wächst 120 -> 132.
        // * _licLaden stand bei x = 418 mit 118 px Breite — rechte Kante 536 und
        //   damit 8 px ÜBER dem Rahmen der 528 px breiten Gruppe. Neu x = 398
        //   (rechte Kante 516, 12 px Innenrand). Damit der Abstand zum Eingabefeld
        //   stimmt, werden _schluessel und _email von 280 auf 260 px schmaler
        //   (rechte Kante 390, 8 px Abstand zum Knopf).
        // * _aktivHinweis 504 x 30 -> 504 x 34: Der fest zweizeilige Hinweis
        //   (8 pt) braucht 33 px. _aktivBox wächst dafür 168 -> 176 und rückt auf
        //   y = 156; _aktionenBox folgt auf y = 338 (6 px Abstand).
        // * Fußzeile: _schliessen 90 x 30 -> 110 x 30 an (434, 444). Die rechte
        //   Kante bleibt bei x = 544 (ClientSize 560 minus 16 Rand). _hinweis wird
        //   dafür von 428 auf 412 px schmaler (6 px Abstand zum Knopf) und rückt
        //   auf y = 422. ClientSize 560 x 470 -> 560 x 486 (12 px unter dem Knopf).
        // * Knopf-Semantik unverändert: Aktionsknöpfe (Aktivieren, Testversion,
        //   Freigeben) plus „Schließen" als CancelButton. Kein OK/Abbrechen-Paar —
        //   die Aktivierung läuft in Aktivieren_Click sofort gegen den Lizenzserver
        //   und ist beim Schließen längst geschehen; ein „Abbrechen" würde eine
        //   Rücknahme zusagen, die der Dialog nicht leisten kann.

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            // PRODUKTNAME ist eine Anwendungskonstante und kein Übersetzungsgut —
            // lokalisiert wird nur das Wort davor, der Titel selbst bleibt ein
            // zur Laufzeit zusammengesetzter Wert.
            this.Text = MyResource.Resource.LIZ_TITEL + " — " + MDIMainForm.PRODUKTNAME;

            // --- Status ---------------------------------------------------
            _statusBox.Text = MyResource.Resource.LIZ_GRP_STATUS;
            _portal.Text = MyResource.Resource.LIZ_LINK_PORTAL;

            // --- Aktivierung ----------------------------------------------
            _aktivBox.Text = MyResource.Resource.LIZ_GRP_AKTIVIEREN;
            _schluesselLabel.Text = MyResource.Resource.LIZ_LBL_SCHLUESSEL;
            _licLaden.Text = MyResource.Resource.LIZ_BTN_LIC;
            _emailLabel.Text = MyResource.Resource.LIZ_LBL_EMAIL;
            _aktivieren.Text = MyResource.Resource.LIZ_BTN_AKTIVIEREN;
            _aktivHinweis.Text = MyResource.Resource.LIZ_HINWEIS_AKTIVIERUNG;

            // --- Testversion / Freigabe -----------------------------------
            _aktionenBox.Text = MyResource.Resource.LIZ_GRP_AKTIONEN;
            _trial.Text = MyResource.Resource.LIZ_BTN_TRIAL;
            _freigeben.Text = MyResource.Resource.LIZ_BTN_FREIGEBEN;

            // --- Fußzeile -------------------------------------------------
            _schliessen.Text = MyResource.Resource.LIZ_BTN_SCHLIESSEN;
        }

        // ==================================================================
        // Anzeige
        // ==================================================================

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
                _detailWert.Text = string.Format(MyResource.Resource.LIZ_DETAIL,
                    token.LizenzId, token.Firma, token.Benutzer, GeraeteId.Anzeigename());
                _freigeben.Enabled = true;
            }
            else
            {
                _detailWert.Text = MyResource.Resource.LIZ_DETAIL_KEINE;
                _freigeben.Enabled = false;
            }

            _trial.Enabled = (token == null);
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        private async void Aktivieren_Click(object sender, EventArgs e)
        {
            string schluessel = (_schluessel.Text ?? "").Trim();
            string email = (_email.Text ?? "").Trim();

            if (schluessel.Length == 0 || email.Length == 0)
            {
                MessageBox.Show(this, MyResource.Resource.LIZ_MSG_EINGABE_FEHLT,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!EmailGueltig(email))
            {
                MessageBox.Show(this, string.Format(MyResource.Resource.LIZ_MSG_EMAIL_UNGUELTIG, email),
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BedienungSperren(true, MyResource.Resource.LIZ_STATUS_AKTIVIERUNG);
            LizenzServerAntwort antwort = await LizenzManager.Aktivieren(schluessel, email);
            BedienungSperren(false, null);

            if (antwort.Ok)
            {
                MessageBox.Show(this, MyResource.Resource.LIZ_MSG_AKTIVIERT + Environment.NewLine + Environment.NewLine +
                    LizenzManager.StatusText(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _schluessel.Clear();
            }
            else
            {
                MessageBox.Show(this, antwort.Meldung ?? MyResource.Resource.LIZ_MSG_AKTIVIERUNG_FEHLER,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            StatusAnzeigen();
        }

        private void LicLaden_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = MyResource.Resource.LIZ_DLG_LIC_TITEL,
                Filter = MyResource.Resource.LIZ_DLG_LIC_FILTER,
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            LizenzManager.LicDateiLesen(dialog.FileName, out string schluessel, out string email);
            if (string.IsNullOrWhiteSpace(schluessel))
            {
                MessageBox.Show(this, MyResource.Resource.LIZ_MSG_LIC_OHNE_SCHLUESSEL,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _schluessel.Text = schluessel;
            if (!string.IsNullOrWhiteSpace(email)) _email.Text = email;
            _hinweis.Text = MyResource.Resource.LIZ_HINWEIS_LIC_GELADEN;
        }

        private async void Trial_Click(object sender, EventArgs e)
        {
            string email = (_email.Text ?? "").Trim();
            if (email.Length == 0 || !EmailGueltig(email))
            {
                MessageBox.Show(this, MyResource.Resource.LIZ_MSG_TRIAL_EMAIL,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BedienungSperren(true, MyResource.Resource.LIZ_STATUS_TRIAL);
            LizenzServerAntwort antwort = await new LizenzServerClient().TrialAnfordern(email, Environment.UserName);
            BedienungSperren(false, null);

            MessageBox.Show(this,
                antwort.Meldung ?? (antwort.Ok ? MyResource.Resource.LIZ_MSG_TRIAL_OK : MyResource.Resource.LIZ_MSG_TRIAL_FEHLER),
                this.Text, MessageBoxButtons.OK, antwort.Ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async void Freigeben_Click(object sender, EventArgs e)
        {
            DialogResult wahl = MessageBox.Show(this,
                MyResource.Resource.LIZ_MSG_FREIGEBEN_FRAGE,
                this.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (wahl != DialogResult.Yes) return;

            BedienungSperren(true, MyResource.Resource.LIZ_STATUS_FREIGABE);
            LizenzServerAntwort antwort = await LizenzManager.Freigeben();
            BedienungSperren(false, null);

            if (!antwort.Ok && antwort.NetzwerkFehler)
            {
                MessageBox.Show(this, MyResource.Resource.LIZ_MSG_SERVER_NICHT_ERREICHBAR,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            StatusAnzeigen();
        }

        /// <summary>
        /// Der Verweis auf das Lizenzportal. Vor der Designer-Umstellung ein
        /// Lambda an <c>LinkClicked</c>; der Designer verdrahtet ausschließlich
        /// Methodenverweise, deshalb steht er jetzt hier.
        /// </summary>
        private void Portal_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkOeffnen(LizenzManager.PORTAL_URL);
        }

        // ==================================================================
        // Hilfsmittel
        // ==================================================================

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
                // Ablaufverfolgung, keine Anzeige — bleibt bewusst unlokalisiert.
                Debug.WriteLine("Link konnte nicht geöffnet werden: " + ex.Message);
            }
        }
    }
}
