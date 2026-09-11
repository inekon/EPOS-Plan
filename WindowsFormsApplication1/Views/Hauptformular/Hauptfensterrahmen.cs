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
    /// der Besitzer für jede <see cref="BlazorDialogForm{T}"/>, <c>ProcessCmdKey</c>
    /// samt F1 (der Weg, der die Taste auch AUS der WebView2 erreicht — Befund
    /// KI‑D‑B‑3) und die stille Lizenz-Nachprüfung beim Start.</para>
    ///
    /// <para><b>Was hier NICHT mehr steht:</b> die 45 Menüpunkte, die 34
    /// Ereignishandler, die acht <c>Init*</c>-Methoden (Kopfband, KI-Hilfe,
    /// Lizenz, Peak-Shaving, Gesetze, Dubletten, Kostenvorlagen,
    /// Variantenmenü), die Ladeanzeige <c>label_OnlineDoku</c>, die Menüsuche
    /// über den Anzeigetext (Befund W16-B23) und die sieben stillen
    /// <c>Console.WriteLine</c> (Befund W16-B33). Sie sind Razor: das Menüband
    /// als Daten (<c>Menuetabelle</c>, 54 Punkte), das Kopfband als Markup, die
    /// Wege als <see cref="HauptfensterHuelle"/>.</para>
    ///
    /// <para><b>Der Name hieß bis zum 04.09.2026 <c>MDIMainForm</c></b> und log
    /// seit langem (Befund W16-B10): <c>IsMdiContainer</c> stand schon vor
    /// dieser Welle auf <c>false</c>, es gibt in der ganzen Anwendung kein
    /// MDI-Kind. Mit Anwenderentscheid E-10 heißt die Klasse
    /// <c>Hauptfensterrahmen</c> — nicht <c>Hauptfenster</c> (so heißt die
    /// Razor-SEITE in <c>EPOS.UI.Seiten</c>) und nicht
    /// <c>HauptfensterHuelle</c> (so heißt die Blazor-Hülle dieser Seite): Der
    /// RAHMEN ist das Fenster mit <c>Application.Run</c>, der
    /// <c>BlazorWebView</c>, F1 und dem Sprachwechsel.</para>
    ///
    /// <para><b>Kein Designer mehr.</b> <c>MDIMainForm.Designer.cs</c> und die
    /// drei <c>.resx</c> sind gelöscht; sie stehen eingefroren unter
    /// <c>Werkzeuge/Formularkarte.Tests/Pruefmuster/Hauptformular/</c>
    /// (Entscheid E-9).</para>
    /// </summary>
    public class Hauptfensterrahmen : Form
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

        public Hauptfensterrahmen()
        {
            // Reguläre SDI-Hauptform, kein MDI-Wirt (Befund W16-B10). Die Zeile
            // stand schon vor dieser Welle hier; seit E-10 sagt es auch der Name.
            IsMdiContainer = false;

            // Beim Start vollflächig, aber später vom Nutzer skalierbar.
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            Text = PRODUKTNAME;

            // Programmsymbol (Auftrag #229): DAS Fenster der Taskleiste und des
            // Alt-Tab-Wechslers — ohne diese Zeile zeigt es das .NET-Standardsymbol,
            // obwohl die EXE seit demselben Auftrag ihr eigenes ApplicationIcon traegt.
            Programmsymbol.Anwenden(this);

            // F1 auch unabhängig vom Menü: Der Menüpunkt allein wäre in einer
            // Razor-Oberfläche kein Tastenkürzel mehr (im Bestand InitKiHilfe
            // :357-374). Die Taste fängt seit Befund KI-D-B-3 ProcessCmdKey ab
            // und nicht mehr KeyPreview/KeyDown — Begründung dort.
            Load += BeimLaden;

            // ANWENDERENTSCHEID 62b-E-1 (11.09.2026), Festlegung 4: Auch das
            // SCHLIESSEN DES PROGRAMMS fragt nach, wenn der Projektassistent
            // ungespeicherte Eingaben hat. Das ist die einzige Stelle des
            // Entscheids, die diesen Rahmen beruehrt - die Rueckfrage selbst steht
            // in der Ansicht, nicht hier (EPOS.UI kennt keine MessageBox).
            FormClosing += BeimSchliessen;
        }

        /// <summary>
        /// <c>true</c>, sobald die Ansicht dem Schließen zugestimmt hat — dann läuft
        /// das zweite <c>Close()</c> ohne Rückfrage durch.
        /// </summary>
        private bool _schliessenFrei;

        /// <summary>
        /// Fragt die Ansicht, ob geschlossen werden darf (62b-E-1).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum abbrechen und später noch einmal schließen.</b>
        /// <c>FormClosing</c> ist synchron, die Rückfrage steht als Überlagerung in
        /// der WebView und wird erst mit dem nächsten Klick des Anwenders
        /// beantwortet. Ein blockierendes Warten hielte genau den Faden an, auf dem
        /// die WebView zeichnet — also wird das Schließen abgebrochen, die Antwort
        /// abgewartet und danach (bei „ja") <see cref="Form.Close"/> erneut
        /// gerufen.</para>
        /// <para>Ohne Oberfläche oder ohne stehenden Assistenten antwortet
        /// <c>DarfVerlassen</c> unmittelbar <c>true</c>; dann sieht der Anwender
        /// nichts als das Schließen.</para>
        /// </remarks>
        private void BeimSchliessen(object sender, FormClosingEventArgs e)
        {
            if (_schliessenFrei || DesignMode) return;

            EPOS.UI.Dienste.INavigationsZiel ziel = EPOS.UI.Dienste.Navigationsziel.Aktuell;

            // Die Vorfrage ist SYNCHRON und entscheidet, ob ueberhaupt abgebrochen
            // wird: Ein Schliessen, das nichts zu fragen hat, laeuft unveraendert
            // durch - auch das Application.Restart des Sprachwechsels.
            if (ziel == null || !ziel.VerlassenFraglich) return;

            e.Cancel = true;

            // EINE NACHRICHT SPAETER, nicht hier: Steht nichts zu fragen, antwortet
            // DarfVerlassen sofort, und ein unmittelbares Close() liefe MITTEN in
            // diesem FormClosing - das Fenster raeumte sich unter seinem eigenen
            // Ereignis weg. BeginInvoke laesst das Ereignis erst zu Ende laufen;
            // derselbe Gedanke wie bei Blazorsprung.
            BeginInvoke(new Action(() => FragenUndSchliessen(ziel)));
        }

        private async void FragenUndSchliessen(EPOS.UI.Dienste.INavigationsZiel ziel)
        {
            bool frei;
            try { frei = await ziel.DarfVerlassen(); }
            catch (Exception ex)
            {
                // Eine gescheiterte Rueckfrage darf das Programm nicht festhalten.
                System.Diagnostics.Debug.WriteLine("Rueckfrage beim Schliessen: " + ex.Message);
                frei = true;
            }

            if (!frei) return;

            _schliessenFrei = true;
            Close();
        }

        /// <summary>
        /// <b>F1 öffnet den Hilfe-Assistenten</b> — Befund <b>KI‑D‑B‑3</b>
        /// (Anwender, 11.09.2026: „auch mit F1 nicht").
        /// </summary>
        /// <remarks>
        /// <para><b>Warum <see cref="Form.ProcessCmdKey"/> und nicht
        /// <c>KeyPreview</c>.</b> Bis hierher stand hier ein <c>KeyDown</c> mit
        /// <c>KeyPreview = true</c>. <c>KeyPreview</c> wirkt aber nur für Tasten,
        /// die im <c>WndProc</c> eines WinForms-Steuerelements ankommen: Erst
        /// <c>Control.ProcessKeyMessage</c> fragt <c>parent.ProcessKeyPreview</c>.
        /// Seit W16c ist das ganze Fenster EINE <see cref="BlazorSeite{T}"/>, der
        /// Tastaturzeiger sitzt also praktisch immer in der WebView2 — und deren
        /// Tastenmeldungen gehen an das Browserfenster, ein natives Kindfenster,
        /// dessen <c>WndProc</c> kein WinForms-Steuerelement ist. Das
        /// <c>KeyDown</c> des Rahmens wurde damit nie ausgelöst.</para>
        /// <para><see cref="Form.ProcessCmdKey"/> erreicht der Tastendruck
        /// dagegen: <c>Application.ThreadContext.PreTranslateMessage</c> sucht
        /// über <c>Control.FromChildHandle</c> das nächste verwaltete
        /// Steuerelement — das ist die <c>WebView2</c>, deren Kindfenster das
        /// Browserfenster ist —, ruft dort <c>PreProcessMessage</c>, und das
        /// reicht <c>ProcessCmdKey</c> die Elternkette hinauf bis zu diesem
        /// Fenster. Dieser Weg deckt den bisherigen mit ab: Er läuft auch, wenn
        /// ein gewöhnliches WinForms-Kind den Zeiger hat.</para>
        /// <para><c>true</c> hält die Taste an — es gibt keinen zweiten Empfänger,
        /// und ein zweites <c>Oeffnen</c> wäre ohnehin nur ein Nach-vorn-Holen.</para>
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message nachricht, Keys taste)
        {
            if (taste == Keys.F1 && !DesignMode)
            {
                // OHNE Aufrufkontext (Auftrag #199): F1 ist der globale Weg und kennt
                // keinen Dialog - die Huelle baut sich den Kontext des aktiven BEREICHS.
                KiChatHuelle.Oeffnen(this);
                return true;
            }

            return base.ProcessCmdKey(ref nachricht, taste);
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
