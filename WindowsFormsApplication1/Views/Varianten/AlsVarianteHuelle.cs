using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Projekt;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Ablauf „Als Variante speichern…" (Menü Projekte › Als Variante
    /// speichern…) — iU9-W2.1, seit Auftrag <b>#237</b> nur noch ein ADAPTER.
    ///
    /// <para><b>Was sich geändert hat.</b> Bis iU9-W2.1 war das die WinForms-Maske
    /// <c>Form_AlsVariante</c>: ein programmatisch aufgebautes Fenster mit einem
    /// Hinweistext, einem Bezeichnerfeld und zwei Knöpfen — also die fünfte
    /// zeichengleiche Namensabfrage des Bestands; danach stellte die generische
    /// <c>NamensDialog</c>-Komponente die Frage. <b>Seit dem Anwenderwunsch vom
    /// 12.09.2026 (#237) ist es ein eigener Dialog</b>
    /// (<see cref="ProjektVarianteDialog"/>): Ein Kontrollkästchen schaltet eine
    /// Projektauswahl frei, aus der die Variante ihren INHALT bekommt. Die
    /// generische Namensabfrage bleibt unverändert — sie bedient vier weitere
    /// Masken.</para>
    ///
    /// <para><b>Die Datenseite liegt in <c>EPOS.UI.Daten</c></b>
    /// (<c>Projekt/ProjektVarianteHuelle</c>, Regel Auftrag #208): Stamm bestimmen,
    /// Zeilen liefern, Zielnamensregel, Anlegen. Hier bleibt, was nur Windows kann —
    /// das Fenster, die Meldungskästen, der Wartezeiger und das Nachziehen der
    /// Startseite.</para>
    ///
    /// <para>Gerechnet wird in <c>VariantenCtrl.AnlegenAusStamm</c>; hier steht
    /// bewusst keine eigene Anlegelogik.</para>
    /// </summary>
    internal static class AlsVarianteHuelle
    {
        /// <summary>
        /// Innenmaß des Fensters. Es ist größer als das der abgelösten Namensabfrage
        /// (520 × 360), weil mit gesetztem Haken eine Projektliste darin steht — sie
        /// bringt Suchfeld, vier Spalten und ihren eigenen Rollbalken mit.
        /// </summary>
        private static readonly Size FENSTER = new Size(760, 640);

        /// <summary>
        /// Fragt Bezeichner und Quellprojekt ab und legt bei OK die Variante an.
        /// <paramref name="idProjekt"/> ist das geöffnete Projekt (Stamm oder
        /// Variante), <paramref name="projektname"/> dessen Name.
        /// </summary>
        /// <returns>
        /// <c>true</c>, wenn eine Variante entstanden ist. <b>Seit Auftrag #240</b>:
        /// Der Menüweg wertet den Ausgang wie bisher nicht aus, der dritte Einstieg —
        /// der Knopf „Variante anlegen" der Seite „Berichte &amp; Kosten › Übersicht" —
        /// braucht ihn, um seine Variantenliste danach neu zu laden.
        /// </returns>
        internal static bool Zeige(IWin32Window besitzer, int idProjekt, string projektname)
        {
            var vor = ProjektVarianteHuelle.Vorbereiten(idProjekt, projektname);
            if (!vor.Bereit)
            {
                MessageBox.Show(besitzer,
                    Text_(vor.Fehlerschluessel),
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK,
                    vor.Fehlerschluessel == "VAR_MSG_KEIN_PROJEKT"
                        ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                return false;
            }

            ProjektVarianteWahl? wahl = Fragen(besitzer, vor.IdStamm, vor.StammName);
            if (wahl == null) return false;

            Cursor alt = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                var ergebnis = ProjektVarianteHuelle.Anlegen(
                    vor.IdStamm, vor.StammName, wahl.Value,
                    () => StartseiteHuelle.Aktuelle?.VariantenAnzeigeAktualisieren());

                if (!ergebnis.Gelungen)
                {
                    MessageBox.Show(besitzer, ergebnis.Fehlertext,
                        MyResource.Resource.VAR_DLG_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                MessageBox.Show(besitzer,
                    string.Format(MyResource.Resource.BK_MSG_VARIANTE_ANGELEGT, wahl.Value.Bezeichner),
                    MyResource.Resource.VAR_DLG_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            finally { Cursor.Current = alt; }
        }

        /// <summary>
        /// Das FENSTER — der einzige Windows-Teil dieses Ablaufs, und deshalb die EINE
        /// Stelle, an der der Dialog aufgeht.
        ///
        /// <para><b>DREI Wege führen zu ihm</b> (seit Auftrag <b>#240</b>; die ersten
        /// zwei seit dem Anwenderwunsch vom 08.09.2026): der Menüpunkt „Als Variante
        /// speichern…" über <see cref="Zeige"/>, der Eintrag im Auswahlfeld „Projekt:"
        /// des Kopfbands über <c>StartseiteHuelle.VarianteAnlegenJetzt</c> und der Knopf
        /// „Variante anlegen" der Seite „Berichte &amp; Kosten › Übersicht" über
        /// <c>UebersichtSeiteGaben.VarianteAnlegenOeffnen</c> — der ruft
        /// <see cref="Zeige"/> mit dem STAMM, den die Seite gewählt hat, und der muss
        /// nicht das geöffnete Projekt sein. Sie unterscheiden sich NUR darin, wie sie
        /// den Erfolg melden — Meldungskasten hier und in der Übersicht, Kurzhinweis der
        /// Seite im Kopfband; der Dialog selbst ist derselbe und steht deshalb nur
        /// hier.</para>
        /// </summary>
        internal static ProjektVarianteWahl? Fragen(IWin32Window besitzer, int idStamm, string stammName)
        {
            ProjektVarianteWahl? ergebnis = null;
            BlazorDialogForm<ProjektVarianteDialog> dlg = null;

            var werte = new Dictionary<string, object>(ProjektVarianteHuelle.Gaben(idStamm, stammName))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<ProjektVarianteWahl?>(new object(), w =>
                {
                    ergebnis = w;
                    if (dlg != null) dlg.Schliessen(w != null);
                })
            };

            dlg = new BlazorDialogForm<ProjektVarianteDialog>(
                MyResource.Resource.VAR_DLG_TITEL, FENSTER, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>Anzeigetext zu einem Meldungsschluessel der Huelle (Rueckfall: der Schluessel).</summary>
        internal static string Text_(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { /* ein fehlender Katalog darf keine Meldung mitreissen */ }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }
    }
}
