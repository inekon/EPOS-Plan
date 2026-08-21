using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Nur-Lese-Anzeige eines längeren Textes in fester Schrittweite (Consolas).
    ///
    /// Zusammenlegung der beiden wortgleichen Wegwerf-Dialoge aus
    /// <c>Form_KiChat.ProtokollZeigen()</c> (Aktionsprotokoll, Fachkonzept 3.6) und
    /// <c>Form_KiChat.VorschauZeigen()</c> (Selbstprüfung A5). Beide bauten dieselbe
    /// Maske: eine füllende <see cref="TextBox"/> ohne Zeilenumbruch mit beiden
    /// Bildlaufleisten, darunter „Schließen" in einem <see cref="FlowLayoutPanel"/>.
    /// Der einzige Aufbauunterschied war das Kopf-Label der Vorschau; die übrigen
    /// Abweichungen (Maße, Maximieren-Schaltfläche) stehen als Parameter.
    ///
    /// Bewusst OHNE Designer-Datei und ohne <c>.resx</c>: Die Maske ist klein und
    /// vollständig parametrisch — es gibt nichts zu entwerfen. Titel, Kopfzeile und
    /// Inhalt bringt die aufrufende Stelle mit, der Knopftext kommt aus
    /// <c>MyResource</c>.
    /// </summary>
    public class Form_TextAnzeige : Form
    {
        /// <summary>
        /// Baut die Anzeige auf. Die Vorgabewerte sind die des Aktionsprotokolls.
        /// </summary>
        /// <param name="titel">Fenstertitel.</param>
        /// <param name="text">Der anzuzeigende Inhalt.</param>
        /// <param name="kopf">
        /// Hinweiszeile über der Anzeige. <c>null</c> lässt das Kopf-Label ganz weg —
        /// genau so war das Aktionsprotokoll aufgebaut.
        /// </param>
        /// <param name="groesse">Client-Maß des Fensters.</param>
        /// <param name="mindestGroesse">Kleinstes Fenstermaß.</param>
        /// <param name="maximierbar">
        /// Maximieren-Schaltfläche. Das Protokoll hatte sie (Vorgabe der Klasse
        /// <see cref="Form"/>), die Vorschau schaltete sie ab.
        /// </param>
        public Form_TextAnzeige(string titel, string text, string kopf = null,
                                Size? groesse = null, Size? mindestGroesse = null,
                                bool maximierbar = true)
        {
            this.Text = titel;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = maximierbar;
            this.ShowInTaskbar = false;
            this.ClientSize = groesse ?? new Size(900, 480);
            this.MinimumSize = mindestGroesse ?? new Size(520, 320);

            TextBox anzeige = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.White,
                Font = new Font("Consolas", 9f),
                Text = text
            };

            // Design-Politur 21.08.2026 — Knopfmaß: 110 × 30 wie in allen
            // Fußzeilen (Form_KiEinstellungen, Form_BkUebernahme, UcBericht).
            // „Schließen" braucht 75 px, die Breite ist also reine Einheitlichkeit.
            // Reiner Anzeigedialog: der eine Knopf bleibt „Schließen" und ist
            // zugleich Accept- und Cancel-Knopf — daran ändert sich nichts.
            Button schliessen = new Button
            {
                Text = MyResource.Resource.KI_VORSCHAU_SCHLIESSEN,
                DialogResult = DialogResult.OK,
                Width = 110,
                Height = 30
            };

            FlowLayoutPanel fuss = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(8)
            };
            fuss.Controls.Add(schliessen);

            // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente.
            this.Controls.Add(anzeige);

            if (kopf != null)
            {
                Label kopfzeile = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 66,
                    Padding = new Padding(10, 8, 10, 4),
                    ForeColor = Color.DimGray,
                    Text = kopf
                };
                this.Controls.Add(kopfzeile);
            }

            this.Controls.Add(fuss);
            this.AcceptButton = schliessen;
            this.CancelButton = schliessen;

            FensterEinpassung.Einhaengen(this);
        }
    }
}
