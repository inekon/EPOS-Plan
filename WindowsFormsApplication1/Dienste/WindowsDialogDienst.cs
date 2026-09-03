using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IDialogDienst"/> — <c>MessageBox</c> und
    /// Mauszeiger (Umsetzungskonzept iOS, Paket iU5).
    ///
    /// <para><b>Das Informationssymbol kehrt zurück.</b> Bis iU3-2 trugen die
    /// Hinweisdialoge des Kerns ein <c>MessageBoxIcon.Information</c>. Beim Umzug hinter
    /// die Melde-Haken fiel es weg, weil <c>Meldung.Hinweis</c> auf
    /// <c>MessageBox.Show(text, titel)</c> gesetzt wurde — die Überladung ohne Symbol.
    /// <see cref="Meldung(string, string)"/> stellt es hier wieder her. Ohne Titel bleibt
    /// es beim symbollosen <c>MessageBox.Show(text)</c>: Diese Form wird für schlichte
    /// Kurzmeldungen benutzt, die nie eines hatten.</para>
    /// </summary>
    public sealed class WindowsDialogDienst : IDialogDienst
    {
        /// <inheritdoc/>
        public void Meldung(string text, string titel = null)
        {
            if (titel == null) MessageBox.Show(text);
            else MessageBox.Show(text, titel, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <inheritdoc/>
        public void Warnung(string text, string titel = null)
        {
            MessageBox.Show(text, titel ?? "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <inheritdoc/>
        public void Fehler(string text, string titel = null)
        {
            MessageBox.Show(text, titel ?? "", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <inheritdoc/>
        public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false)
        {
            MessageBoxIcon symbol = warnend ? MessageBoxIcon.Warning : MessageBoxIcon.Question;
            MessageBoxDefaultButton vorgabe = vorgabeNein
                ? MessageBoxDefaultButton.Button2
                : MessageBoxDefaultButton.Button1;

            return MessageBox.Show(text, titel ?? "", MessageBoxButtons.YesNo, symbol, vorgabe)
                   == DialogResult.Yes;
        }

        /// <inheritdoc/>
        public JaNeinAbbruch Wahl(string text, string titel = null)
        {
            DialogResult antwort = MessageBox.Show(text, titel ?? "",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (antwort == DialogResult.Yes) return JaNeinAbbruch.Ja;
            if (antwort == DialogResult.No) return JaNeinAbbruch.Nein;
            return JaNeinAbbruch.Abbruch;
        }

        /// <inheritdoc/>
        public void Warten(bool an)
        {
            Cursor.Current = an ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
