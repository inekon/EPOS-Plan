using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Nicht schliessender Speichern-Knopf samt Statuszeile fuer die
    /// Verwaltungsdialoge der Kataloge (Bauart "Liste links, Infofelder rechts,
    /// OK unten rechts").
    ///
    /// <para>
    /// Hintergrund (Befund 18.08.2026): Mehrere dieser Masken zeigen die
    /// Stammdatenfelder editierbar an, hatten aber keinen Speicherweg — ihr
    /// OK-Knopf rief nur <c>Close()</c>, Eingaben gingen still verloren. Diese
    /// Klasse buendelt die Oberflaechenseite des nachgeruesteten Weges; WAS
    /// gespeichert wird, bleibt Sache des jeweiligen Dialogs.
    /// </para>
    ///
    /// <para>
    /// Beide Steuerelemente entstehen programmatisch — Designer- und
    /// <c>.resx</c>-Dateien werden in diesem Projekt nicht von Hand gepflegt
    /// (CLAUDE.md). Groesse, Schrift, Rand und Verankerung kommen vom vorhandenen
    /// OK-Knopf, der neue Knopf sitzt unmittelbar links daneben. Die Fenstergroesse
    /// aendert sich dadurch nicht, eine Einpassung ueber
    /// <see cref="FensterEinpassung"/> ist deshalb nicht noetig.
    /// </para>
    /// </summary>
    internal sealed class SpeichernLeiste
    {
        /// <summary>Waagerechter Abstand zwischen Speichern- und OK-Knopf.</summary>
        internal const int ABSTAND = 12;

        private readonly Button _knopf;
        private readonly Label _status;
        private readonly ToolTip _tip = new ToolTip();

        /// <summary>
        /// Legt Knopf und Statuszeile an und haengt sie in <paramref name="eltern"/> ein.
        /// </summary>
        /// <param name="eltern">Aufnehmendes Formular.</param>
        /// <param name="vorbild">Vorhandener OK-Knopf — liefert Groesse, Schrift,
        /// Rand, Verankerung, Oberkante und Tabulatorstelle.</param>
        /// <param name="statusFeld">Rechteck der Statuszeile in Formularkoordinaten.
        /// Der Aufrufer waehlt eine freie Flaeche seines Layouts.</param>
        /// <param name="beimKlick">Handler des Speichern-Knopfes. Er darf den Dialog
        /// nicht schliessen — genau das ist der Zweck dieses Knopfes.</param>
        internal SpeichernLeiste(Control eltern, Button vorbild, Rectangle statusFeld, EventHandler beimKlick)
        {
            _knopf = new Button();
            _knopf.Name = "btn_Speichern";
            _knopf.Text = MyResource.Resource.ADM_BTN_SPEICHERN;
            _knopf.Font = vorbild.Font;
            _knopf.Size = vorbild.Size;
            _knopf.Margin = vorbild.Margin;
            _knopf.Anchor = vorbild.Anchor;
            _knopf.Location = new Point(vorbild.Left - vorbild.Width - ABSTAND, vorbild.Top);
            _knopf.TabIndex = vorbild.TabIndex - 1;
            _knopf.UseVisualStyleBackColor = true;
            _knopf.Enabled = false;
            _knopf.Click += beimKlick;
            eltern.Controls.Add(_knopf);

            // Rueckmeldung als Statuszeile statt als MessageBox: der Dialog bleibt
            // offen, mehrfaches Speichern soll keine Meldungskette ausloesen.
            _status = new Label();
            _status.Name = "lbl_SpeichernStatus";
            _status.Font = vorbild.Font;
            _status.AutoSize = false;
            _status.TextAlign = ContentAlignment.MiddleRight;
            _status.Bounds = statusFeld;
            _status.Anchor = vorbild.Anchor;
            _status.ForeColor = SystemColors.GrayText;
            _status.Text = "";
            eltern.Controls.Add(_status);
        }

        /// <summary>Der angelegte Knopf (Pruefhilfe fuer den Headless-Harnisch).</summary>
        internal Button Knopf { get { return _knopf; } }

        /// <summary>Die angelegte Statuszeile (Pruefhilfe fuer den Headless-Harnisch).</summary>
        internal Label Status { get { return _status; } }

        /// <summary>
        /// Der Knopf ist nur aktiv, wenn es etwas zu speichern gibt: ein markierter
        /// Satz UND mindestens eine Aenderung. Der Tooltip nennt jeweils den Grund,
        /// damit der gesperrte Knopf nicht raetselhaft bleibt.
        /// </summary>
        internal void Zustand(bool bSatzMarkiert, bool bGeaendert)
        {
            _knopf.Enabled = bSatzMarkiert && bGeaendert;

            string szTip;
            if (!bSatzMarkiert) szTip = MyResource.Resource.ADM_TIP_SPEICHERN_LEER;
            else if (!bGeaendert) szTip = MyResource.Resource.ADM_TIP_SPEICHERN_UNVERAENDERT;
            else szTip = MyResource.Resource.ADM_TIP_SPEICHERN;
            _tip.SetToolTip(_knopf, szTip);
        }

        /// <summary>Erfolgsmeldung mit Uhrzeit in der Statuszeile.</summary>
        internal void Gespeichert()
        {
            Melden(string.Format(MyResource.Resource.ADM_STATUS_GESPEICHERT,
                                 DateTime.Now.ToString("T")), false);
        }

        /// <summary>
        /// Kurzhinweis, dass NICHT geschrieben wurde. Die Begruendung steht bereits
        /// in der Meldung, die der Dialog davor gezeigt hat.
        /// </summary>
        internal void Fehler()
        {
            Melden(MyResource.Resource.ADM_STATUS_FEHLER, true);
        }

        /// <summary>Loescht die Statuszeile (neue Eingabe, neuer Satz).</summary>
        internal void Leeren()
        {
            Melden("", false);
        }

        private void Melden(string szText, bool bFehler)
        {
            _status.Text = szText;
            _status.ForeColor = bFehler ? Color.Firebrick : SystemColors.GrayText;
        }
    }
}
