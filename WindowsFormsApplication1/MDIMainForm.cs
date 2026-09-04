using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DAS ANWENDUNGSFENSTER — seit iU9-W16c.3 nur noch die HÜLLE
    /// (Vermessung § 12.4).
    ///
    /// <para><b>Was hier steht, steht hier, weil Windows es verlangt:</b> die
    /// Nachrichtenschleife (<c>Program.Main</c> gibt das Fenster an
    /// <c>Application.Run</c>), Fenstergeometrie und Titel, EINE
    /// <see cref="BlazorSeite{T}"/> mit <c>EPOS.UI/Seiten/Hauptfenster.razor</c>,
    /// der Besitzer für jede <see cref="BlazorDialogForm{T}"/>, <c>KeyPreview</c>
    /// samt F1 (die WebView fängt F1 nicht ab) und die stille
    /// Lizenz-Nachprüfung beim Start.</para>
    ///
    /// <para><b>Was hier NICHT mehr steht:</b> die 45 Menüpunkte, die 34
    /// Ereignishandler, die acht <c>Init*</c>-Methoden (Kopfband, KI-Hilfe,
    /// Lizenz, Peak-Shaving, Gesetze, Dubletten, Kostenvorlagen,
    /// Variantenmenü), die Ladeanzeige <c>label_OnlineDoku</c>, die Menüsuche
    /// über den Anzeigetext (Befund W16-B23) und die sieben stillen
    /// <c>Console.WriteLine</c> (Befund W16-B33). Sie sind Razor: das Menüband
    /// als Daten (<c>Menuetabelle</c>, 55 Punkte), das Kopfband als Markup, die
    /// Wege als <see cref="HauptfensterHuelle"/>.</para>
    ///
    /// <para><b>Der Name lügt seit langem</b> (Befund W16-B10): <c>IsMdiContainer</c>
    /// stand schon vor dieser Welle auf <c>false</c>, es gibt in der ganzen
    /// Anwendung kein MDI-Kind. Der Name bleibt bis zur Umbenennung nach dem
    /// Merge (Entscheid E-10) — er hängt an <c>HilfeKontext</c>, am
    /// Erreichbarkeitsgraphen und an <c>help_mapping.txt</c>.</para>
    ///
    /// <para><b>Kein Designer mehr.</b> <c>MDIMainForm.Designer.cs</c> und die
    /// drei <c>.resx</c> sind gelöscht; sie stehen eingefroren unter
    /// <c>Werkzeuge/Formularkarte.Tests/Pruefmuster/Hauptformular/</c>
    /// (Entscheid E-9).</para>
    /// </summary>
    public class MDIMainForm : Form
    {
        /// <summary>
        /// Produktname für Titelleiste, Kopfband und Meldungen. Ein Markenname
        /// wird nicht übersetzt; er bleibt deshalb eine Konstante und wandert
        /// nicht in den Ressourcenkatalog (die zwei anderen Produkttexte aus
        /// Befund W16-B25 sind es: START_GATTUNG und HAUPT_CLAIM).
        /// </summary>
        public const string PRODUKTNAME = "EPOS-Plan";

        /// <summary>
        /// Online-Dokumentation im Wiki (A4). Reiner Not-Fallback — führend ist
        /// der Einstellwert, den auch der Hilfekatalog verwendet (A2).
        /// </summary>
        public const string DOKU_URL = Program.WIKI_STANDARD;

        /// <summary>Die Datenseite des Fensters — Menüwege, Kopfband, Startseite.</summary>
        private HauptfensterHuelle _huelle;

        /// <summary>Die EINE WebView des Fensters (Risiko R5: eine je Fenster).</summary>
        private BlazorSeite<EPOS.UI.Seiten.Hauptfenster> _bild;

        public MDIMainForm()
        {
            // Der Name lügt (Befund W16-B10): reguläre SDI-Hauptform, kein
            // MDI-Wirt. Die Zeile stand schon vor dieser Welle hier.
            IsMdiContainer = false;

            // Beim Start vollflächig, aber später vom Nutzer skalierbar.
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            Text = PRODUKTNAME;

            // F1 auch unabhängig vom Menü: Die WebView fängt die Taste nicht ab,
            // und der Menüpunkt allein wäre in einer Razor-Oberfläche kein
            // Tastenkürzel mehr (im Bestand InitKiHilfe :357-374).
            KeyPreview = true;
            KeyDown += BeiTaste;

            Load += BeimLaden;
        }

        private void BeiTaste(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F1) return;

            e.Handled = true;
            KiChatHuelle.Oeffnen(this);
        }

        private void BeimLaden(object sender, EventArgs e)
        {
            // Verhindert, dass der Designer in Visual Studio die API blockiert.
            if (DesignMode) return;

            try
            {
                // Einmaliger Download der Slugs beim echten Programmstart. Await
                // bleibt bewusst weg: Der WordPress-Zugriff läuft im Hintergrund
                // weiter, der Start blockiert nicht (Konzept Hilfesystem, H3).
                // LoadAllAsync fängt jeden Fehler selbst ab; die Zuweisung an _
                // macht das sichtbar.
                //
                // iU9-W16c.3: Die zentrierte Ladeanzeige label_OnlineDoku ist
                // entfallen. Sie stand für den Bruchteil einer Sekunde zwischen
                // zwei Zeilen und war nie zu lesen; die WebView bringt ihre
                // eigene Themafläche mit, bevor sie zeichnet.
                _ = Program.HelpCatalog.LoadAllAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fehler beim Laden der Doku: " + ex.Message);
            }

            // Die ganze Oberfläche ist EINE Razor-Seite: Menüband, Kopfband und
            // die Ansicht darunter (EPOS.UI/Seiten/Hauptfenster.razor). Bis
            // W16b hing hier eine BlazorSeite<Startseite>, davor die
            // eingebettete Form_Start.
            _huelle = new HauptfensterHuelle(() => this, Program.projektkontext);
            _bild = new BlazorSeite<EPOS.UI.Seiten.Hauptfenster>(
                new Dictionary<string, object>(_huelle.Gaben()));
            Controls.Add(_bild);
            _bild.BringToFront();

            // Stille Nachprüfung des Lizenz-Tokens — Fehler bleiben bewusst
            // folgenlos, die Karenzzeit im LizenzManager fängt Offline-Phasen ab
            // (im Bestand am Ende von InitLizenzMenue, :440).
            _ = LizenzManager.NachpruefungImHintergrund();
        }
    }
}
