using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Volumen- und Herstellerfilter des Pufferspeicher-Katalogdialogs
    /// <see cref="Form_PufferSp_Admin"/> — die COMBOBOX-Seite.
    ///
    /// <para>
    /// <b>Wo die Tabellen jetzt stehen (iU9-W6.0c).</b> Die sechs SQL-Prädikate und ihre
    /// sechs Anzeigetexte sind nach <see cref="PufferSpStammCtrl"/> in den Kern gewandert
    /// (<c>VOLUMEN_SQL</c>, <c>VolumenTexte()</c>). Grund: Der Projektdialog
    /// <c>Form_PufferSp</c> ist seit iU9-W6.7 die Razor-Komponente
    /// <c>PufferspeicherDialog</c>, und die kennt keine <see cref="ComboBox"/> — an eine
    /// Klasse, die eine erwartet, kam sie nicht heran. Der Kern führt die Tabellen damit
    /// EINMAL für beide Dialoge.
    /// </para>
    /// <para>
    /// <b>Was hier bleibt.</b> Genau die drei Handgriffe an der ComboBox:
    /// füllen, vorbelegen und die Auswahl in ein Prädikat übersetzen.
    /// <c>Form_PufferSp_Admin</c> ist bis Welle 14 eine WinForms-Maske und braucht sie
    /// unverändert. Der Wortlaut der Prädikate und die Regel „Index ist der Steuerwert"
    /// (Paket 9 / L5, Bestandsfehler B0-10) stehen im Kern beschrieben.
    /// </para>
    /// </summary>
    internal static class PufferSpFilter
    {
        /// <summary>
        /// Füllt den Volumenfilter und stellt ihn auf „Alle". Bewusst über
        /// <c>SelectedIndex</c> statt über <c>Text</c>: Damit stimmen Anzeige und
        /// Auswahlindex von Anfang an überein — der Index ist der Steuerwert.
        ///
        /// <para>
        /// <b>Das Auslösen von <c>SelectedIndexChanged</c> ist gewollt</b> und wurde in
        /// der Paket-9-Nacharbeit ausdrücklich NICHT unterdrückt: Die Bestandsvorbelegung
        /// <c>comboBox_Volumen.Text = "Alle"</c> löste es ebenfalls genau einmal aus (der
        /// <c>Text</c>-Setzer sucht den Eintrag und setzt <c>SelectedIndex</c>).
        /// </para>
        /// </summary>
        public static void VolumenfilterFuellen(ComboBox cb)
        {
            if (cb == null) return;

            cb.Items.Clear();
            foreach (string t in PufferSpStammCtrl.VolumenTexte()) cb.Items.Add(t);
            cb.SelectedIndex = 0;   // "Alle"
        }

        /// <summary>
        /// Das SQL-Prädikat zur aktuellen Auswahl. Freitext (die ComboBox ist
        /// editierbar) und eine leere Auswahl liefern „alle Volumina".
        /// </summary>
        public static string VolumenSql(ComboBox cb)
        {
            if (cb == null) return PufferSpStammCtrl.VOLUMEN_SQL[0];

            int index = cb.SelectedIndex;
            if (index < 0 || index >= PufferSpStammCtrl.VOLUMEN_SQL.Length)
            {
                // Freitext: über den angezeigten Text versuchen, sonst "alle".
                index = -1;
                string gesucht = (cb.Text ?? "").Trim();
                var texte = PufferSpStammCtrl.VolumenTexte();
                for (int i = 0; i < texte.Count; i++)
                    if (string.Equals(texte[i], gesucht, StringComparison.OrdinalIgnoreCase))
                    { index = i; break; }

                if (index < 0) return PufferSpStammCtrl.VOLUMEN_SQL[0];
            }

            return PufferSpStammCtrl.VOLUMEN_SQL[index];
        }

        /// <summary>
        /// Das SQL-Prädikat des Herstellerfilters. „Alle" und die leere Eingabe stehen
        /// für „ohne Einschränkung".
        ///
        /// Der Herstellerfilter kennt keinen festen Eintrag „Alle" — die Liste kommt aus
        /// den Stammdaten, der Text wird nur vorbelegt (Bestand). Verglichen wird
        /// deshalb weiterhin gegen einen Text, aber gegen den RESSOURCENWERT und nicht
        /// mehr gegen ein deutsches Literal; damit passen Vorbelegung und Vergleich in
        /// jeder Sprache zusammen.
        /// </summary>
        public static string HerstellerSql(ComboBox cb)
        {
            string text = cb == null ? "" : (cb.Text ?? "").Trim();

            if (text.Length == 0 ||
                string.Equals(text, MyResource.Resource.PSP_FILTER_ALLE, StringComparison.OrdinalIgnoreCase))
                return "Hersteller Like '%'";

            // Bestand: einfaches Anführungszeichen verdoppeln, damit ein Herstellername
            // mit Apostroph das Prädikat nicht zerreißt.
            return "Hersteller='" + text.Replace("'", "''") + "'";
        }

        /// <summary>Vorbelegung des Herstellerfilters („Alle").</summary>
        public static void HerstellerfilterVorbelegen(ComboBox cb)
        {
            if (cb != null) cb.Text = MyResource.Resource.PSP_FILTER_ALLE;
        }
    }
}
