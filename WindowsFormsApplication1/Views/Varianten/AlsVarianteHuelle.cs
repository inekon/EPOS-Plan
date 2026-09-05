using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Ablauf „Als Variante speichern…" (Menü Projekte › Als Variante
    /// speichern…) — iU9-W2.1.
    ///
    /// <para><b>Was sich geändert hat.</b> Bis iU9-W2.1 war das die WinForms-Maske
    /// <c>Form_AlsVariante</c>: ein programmatisch aufgebautes Fenster mit einem
    /// Hinweistext, einem Bezeichnerfeld und zwei Knöpfen — also die fünfte
    /// zeichengleiche Namensabfrage des Bestands. Die Abfrage stellt jetzt
    /// <see cref="NamensDialogHuelle.FragenMitHinweis"/> (Razor-Komponente
    /// <c>EPOS.UI/Dialoge/Allgemein/NamensDialog.razor</c>); die Maske ist im
    /// selben Schritt gelöscht (Regel M1). Übrig bleibt der ABLAUF, und der
    /// gehört auf die Windows-Seite: Er redet mit <see cref="VariantenCtrl"/>,
    /// mit <c>Program.startfrm</c> und mit dem Meldungsdienst.</para>
    ///
    /// <para>
    /// IST DAS GEÖFFNETE PROJEKT SELBST EINE VARIANTE, wird ihr Stammprojekt
    /// verwendet: Eine Variante hängt immer am Stamm, nie an einer anderen Variante —
    /// sonst wäre die Vergleichsgruppe keine Gruppe mehr, sondern eine Kette, und die
    /// Differenz-Kennzahlen der Wirtschaftlichkeit hätten keinen gemeinsamen Bezug.
    /// </para>
    ///
    /// <para>Gerechnet wird in <see cref="VariantenCtrl.AnlegenAusStamm"/>; hier
    /// steht bewusst keine eigene Anlegelogik.</para>
    /// </summary>
    internal static class AlsVarianteHuelle
    {
        /// <summary>
        /// Fragt den Bezeichner ab und legt bei OK die Variante an.
        /// <paramref name="idProjekt"/> ist das in Form_Start geöffnete Projekt
        /// (Stamm oder Variante), <paramref name="projektname"/> dessen Name.
        /// </summary>
        internal static void Zeige(IWin32Window besitzer, int idProjekt, string projektname)
        {
            if (idProjekt <= 0)
            {
                MessageBox.Show(besitzer,
                    MyResource.Resource.VAR_MSG_KEIN_PROJEKT,
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            VariantenCtrl ctrl = new VariantenCtrl();

            // Stamm bestimmen: ist das geöffnete Projekt eine Variante, deren Stamm nehmen.
            int idStamm = ctrl.StammRefDerVariante(idProjekt);
            bool istVariante = idStamm > 0;
            if (!istVariante) idStamm = idProjekt;

            string stammName = istVariante ? LiesProjektname(idStamm) : (projektname ?? "");
            if (string.IsNullOrWhiteSpace(stammName)) stammName = LiesProjektname(idStamm);
            if (string.IsNullOrWhiteSpace(stammName))
            {
                MessageBox.Show(besitzer, MyResource.Resource.BK_MSG_KEIN_STAMM,
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Der Dialog: Hinweis, Bezeichnerfeld, „Variante anlegen"/„Abbrechen".
            // Der Anlegeknopf bleibt gesperrt, solange das Feld leer ist —
            // wortgleich zu btnAnlegen.Enabled = Bezeichner.Length > 0.
            string bezeichner = NamensDialogHuelle.FragenMitHinweis(
                besitzer,
                MyResource.Resource.VAR_DLG_TITEL,
                string.Format(MyResource.Resource.VAR_DLG_HINWEIS, stammName),
                MyResource.Resource.BK_LBL_BEZEICHNER,
                MyResource.Resource.BK_BTN_ANLEGEN,
                MyResource.Resource.SIM_BTN_ABBRECHEN);
            if (bezeichner == null) return;

            Cursor alt = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                string fehler;
                int neueId = ctrl.AnlegenAusStamm(idStamm, stammName, bezeichner, out fehler);
                if (neueId <= 0)
                {
                    MessageBox.Show(besitzer,
                        string.IsNullOrEmpty(fehler)
                            ? MyResource.Resource.BK_MSG_ANLEGEN_FEHLGESCHLAGEN : fehler,
                        MyResource.Resource.VAR_DLG_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Startseite nachziehen: Variantenauswahl und – falls schon aufgebaut –
                // der Reiter „Berichte & Kosten" kennen die neue Variante sonst nicht.
                StartseiteHuelle.Aktuelle?.VariantenAnzeigeAktualisieren();

                MessageBox.Show(besitzer,
                    string.Format(MyResource.Resource.BK_MSG_VARIANTE_ANGELEGT, bezeichner),
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(besitzer,
                    string.Format(MyResource.Resource.BK_MSG_ANLEGEFEHLER, ex.Message),
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { Cursor.Current = alt; }
        }

        // Liest den Projektnamen zu einer ID (leer, wenn nicht gefunden) – wie in Form_Start.
        private static string LiesProjektname(int idProjekt)
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadAll();
            foreach (ProjektModel p in pc.items)
                if (p.m_ID == idProjekt) return p.m_szProjektname;
            return "";
        }
    }
}
