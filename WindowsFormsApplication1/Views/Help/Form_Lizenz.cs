using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zeigt die Lizenzvereinbarung und AGB an (Menü "Hilfe &gt; Lizenz").
    ///
    /// Grundlage ist die Datei "LIZENZ-INEKON.rtf" aus dem Projektstammverzeichnis.
    /// Sie wird formatiert in eine RichTextBox geladen; alternativ wird die
    /// DOCX-Fassung als Download angeboten. Wird keine Datei gefunden, zeigt das
    /// Fenster einen Hinweis mit den durchsuchten Pfaden - so bleibt nachvollziehbar,
    /// wo die Datei erwartet wird.
    ///
    /// Komplett programmatisch aufgebaut (kein Designer, keine .resx).
    /// </summary>
    public class Form_Lizenz : Form
    {
        /// <summary>Dateinamen, nach denen gesucht wird (in dieser Reihenfolge).</summary>
        private static readonly string[] DATEINAMEN =
        {
            "LIZENZ-INEKON.rtf",
            "LIZENZVEREINBARUNG UND ALLGEMEINE GESCHÄFTSBEDINGUNGEN- Wärmeplan.docx"
        };

        private RichTextBox _text;
        private Label _lblQuelle;
        private string _gefundeneDatei = "";
        private PrintDocument _druck;
        private string _druckText = "";
        private int _druckPosition = 0;

        public Form_Lizenz()
        {
            BaueOberflaeche();
            LizenzLaden();
        }

        private void BaueOberflaeche()
        {
            this.Text = "Lizenzvereinbarung und AGB";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(820, 620);
            this.MinimumSize = new Size(560, 400);
            this.MinimizeBox = false;

            _text = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                DetectUrls = true
            };
            _text.LinkClicked += (s, e) => LinkOeffnen(e.LinkText);

            Panel unten = new Panel { Dock = DockStyle.Bottom, Height = 46 };

            _lblQuelle = new Label
            {
                Location = new Point(12, 14),
                Size = new Size(430, 20),
                ForeColor = Color.DimGray,
                AutoEllipsis = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Button btnDrucken = new Button
            {
                Text = "Drucken...",
                Size = new Size(100, 28),
                Location = new Point(this.ClientSize.Width - 330, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnDrucken.Click += (s, e) => Drucken();

            Button btnSpeichern = new Button
            {
                Text = "Speichern unter...",
                Size = new Size(130, 28),
                Location = new Point(this.ClientSize.Width - 222, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSpeichern.Click += (s, e) => SpeichernUnter();

            Button btnSchliessen = new Button
            {
                Text = "Schließen",
                Size = new Size(84, 28),
                Location = new Point(this.ClientSize.Width - 84 - 12, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            unten.Controls.Add(_lblQuelle);
            unten.Controls.Add(btnDrucken);
            unten.Controls.Add(btnSpeichern);
            unten.Controls.Add(btnSchliessen);

            this.Controls.Add(_text);
            this.Controls.Add(unten);
            this.AcceptButton = btnSchliessen;
            this.CancelButton = btnSchliessen;
        }

        /// <summary>Sucht die Lizenzdatei und lädt sie in die Anzeige.</summary>
        private void LizenzLaden()
        {
            System.Collections.Generic.List<string> gesucht = new System.Collections.Generic.List<string>();
            string treffer = DateiSuchen(gesucht);

            if (treffer == null)
            {
                _text.Text =
                    "Die Lizenzdatei wurde nicht gefunden." + Environment.NewLine + Environment.NewLine +
                    "Erwartet wird eine der folgenden Dateien im Programm- oder Projektverzeichnis:" +
                    Environment.NewLine +
                    "  • " + DATEINAMEN[0] + Environment.NewLine +
                    "  • " + DATEINAMEN[1] + Environment.NewLine + Environment.NewLine +
                    "Durchsuchte Verzeichnisse:" + Environment.NewLine +
                    string.Join(Environment.NewLine, gesucht) + Environment.NewLine + Environment.NewLine +
                    "Die gültige Lizenzvereinbarung erhalten Sie bei:" + Environment.NewLine +
                    "Dr. Dirk Engelmann, INEKON, Breitwiesenstr. 13, 70565 Stuttgart";

                _lblQuelle.Text = "Quelle: keine Lizenzdatei gefunden";
                return;
            }

            _gefundeneDatei = treffer;
            _lblQuelle.Text = "Quelle: " + Path.GetFileName(treffer);

            try
            {
                if (treffer.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
                {
                    // RTF kann die RichTextBox direkt formatiert darstellen
                    _text.LoadFile(treffer, RichTextBoxStreamType.RichText);
                }
                else
                {
                    // DOCX lässt sich hier nicht darstellen - Hinweis und Öffnen anbieten
                    _text.Text =
                        "Die Lizenzvereinbarung liegt als Word-Dokument vor:" + Environment.NewLine +
                        treffer + Environment.NewLine + Environment.NewLine +
                        "Über 'Speichern unter...' können Sie das Dokument ablegen und mit Word öffnen.";
                }
            }
            catch (Exception ex)
            {
                _text.Text = "Die Lizenzdatei konnte nicht gelesen werden:" + Environment.NewLine +
                             treffer + Environment.NewLine + Environment.NewLine + ex.Message;
            }
        }

        /// <summary>
        /// Durchsucht die üblichen Ablageorte nach der Lizenzdatei und
        /// protokolliert dabei die geprüften Verzeichnisse.
        /// </summary>
        private string DateiSuchen(System.Collections.Generic.List<string> protokoll)
        {
            System.Collections.Generic.List<string> ordner = new System.Collections.Generic.List<string>();

            try
            {
                string basis = AppDomain.CurrentDomain.BaseDirectory;
                ordner.Add(basis);

                // Übergeordnete Ebenen mitnehmen: bin\x86\Debug\net8.0-windows -> Projektstamm
                DirectoryInfo di = new DirectoryInfo(basis);
                for (int i = 0; i < 6 && di.Parent != null; i++)
                {
                    di = di.Parent;
                    ordner.Add(di.FullName);
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(Program.ApplicationPath_Common)) ordner.Add(Program.ApplicationPath_Common);
                if (!string.IsNullOrEmpty(Program.ApplicationPath_User)) ordner.Add(Program.ApplicationPath_User);
            }
            catch { }

            foreach (string o in ordner)
            {
                if (string.IsNullOrEmpty(o)) continue;
                if (protokoll != null && !protokoll.Contains(o)) protokoll.Add(o);

                foreach (string name in DATEINAMEN)
                {
                    try
                    {
                        string pfad = Path.Combine(o, name);
                        if (File.Exists(pfad)) return pfad;
                    }
                    catch { }
                }
            }

            return null;
        }

        /// <summary>Speichert die gefundene Lizenzdatei an einem gewählten Ort.</summary>
        private void SpeichernUnter()
        {
            if (string.IsNullOrEmpty(_gefundeneDatei) || !File.Exists(_gefundeneDatei))
            {
                MessageBox.Show("Es wurde keine Lizenzdatei gefunden, die gespeichert werden könnte.",
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Lizenzvereinbarung speichern";
            dlg.FileName = Path.GetFileName(_gefundeneDatei);
            dlg.Filter = "Alle Dateien (*.*)|*.*";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                File.Copy(_gefundeneDatei, dlg.FileName, true);
                MessageBox.Show("Die Lizenzvereinbarung wurde gespeichert:\n" + dlg.FileName,
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die Datei konnte nicht gespeichert werden:\n" + ex.Message,
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Druckt den angezeigten Text (einfacher Fließtextdruck).</summary>
        private void Drucken()
        {
            try
            {
                _druckText = _text.Text;
                _druckPosition = 0;

                _druck = new PrintDocument();
                _druck.DocumentName = "Lizenzvereinbarung";
                _druck.PrintPage += Druck_PrintPage;

                PrintDialog dlg = new PrintDialog();
                dlg.Document = _druck;
                if (dlg.ShowDialog(this) == DialogResult.OK) _druck.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Der Druck konnte nicht gestartet werden:\n" + ex.Message,
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Druck_PrintPage(object sender, PrintPageEventArgs e)
        {
            using (Font f = new Font("Segoe UI", 9f))
            {
                RectangleF bereich = e.MarginBounds;
                string rest = _druckText.Substring(_druckPosition);

                int zeichen, zeilen;
                e.Graphics.MeasureString(rest, f, bereich.Size, StringFormat.GenericTypographic,
                    out zeichen, out zeilen);

                e.Graphics.DrawString(rest.Substring(0, zeichen), f, Brushes.Black, bereich,
                    StringFormat.GenericTypographic);

                _druckPosition += zeichen;
                e.HasMorePages = _druckPosition < _druckText.Length;
                if (!e.HasMorePages) _druckPosition = 0;
            }
        }

        private void LinkOeffnen(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }

        /// <summary>Bequemer Einstiegspunkt für den Menüaufruf.</summary>
        public static void Anzeigen(IWin32Window besitzer = null)
        {
            Form_Lizenz frm = new Form_Lizenz();
            if (besitzer != null) frm.ShowDialog(besitzer); else frm.ShowDialog();
        }
    }
}
