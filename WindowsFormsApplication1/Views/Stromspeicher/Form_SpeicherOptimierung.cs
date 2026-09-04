using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auslegungsoptimierung des Stromspeichers (AP8) — Rastersuche ueber Kapazitaet
    /// und C-Rate nach Fachkonzept 6.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zielfunktion.</b> Maximiert wird der degradationsbereinigte Jahresueberschuss
    /// nach Kapitaldienst <c>dJ = E_a,aeq − I · a(i_z, N)</c>, wahlweise abzueglich der
    /// Verschleisskosten K_ver (Fachkonzept 5.4, Default AUS). Die Amortisationszeit
    /// erscheint nur als Sekundaerkennzahl — als Zielgroesse ignoriert sie die
    /// Nutzungsdauer und liefert systematisch zu kleine Speicher.
    /// </para>
    /// <para>
    /// <b>Erste nebenlaeufige Rechnung des Programms.</b> Die gesamte
    /// Simulationskette laeuft bisher synchron im Bedienfaden (nur
    /// <c>Cursor.WaitCursor</c> und <c>Application.DoEvents()</c> als Notbehelf);
    /// <c>async</c> gab es bislang nur fuer einen Netzabruf
    /// (<c>Hauptfensterrahmen.BeimLaden</c> -&gt; <c>HelpCatalog.LoadAllAsync</c>), nicht fuer
    /// Rechenarbeit und ohne Fortschritt oder Abbruch. Die Rastersuche laeuft hier
    /// stattdessen in <c>Task.Run</c>, meldet ihren Fortschritt ueber
    /// <see cref="IProgress{T}"/> und laesst sich ueber einen
    /// <see cref="CancellationTokenSource"/> abbrechen. Die Aufteilung ist streng:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>UI-Thread:</b> Felder lesen, <b>Datenbankzugriff</b>
    ///     (<c>StromspeicherSimCtrl.BereiteOptimierungVor</c>), Anzeige.</description></item>
    ///   <item><description><b>Hintergrund-Task:</b> ausschliesslich
    ///     <c>StromspeicherSimCtrl.FuehreOptimierungAus</c> — reine Rechnung ohne jeden
    ///     Datenbankzugriff. <c>DataRepository.EngineModus</c> ist prozessweit und
    ///     nicht threadgebunden, ein Zugriff von dort waere ein Fehler.</description></item>
    ///   <item><description><b>Marshalling</b> ueber <c>Progress&lt;T&gt;</c>: Die
    ///     Instanz entsteht auf dem UI-Thread, uebernimmt dessen
    ///     <c>SynchronizationContext</c> und ruft den Fortschrittshandler damit selbst
    ///     wieder dort auf. Kein <c>Invoke</c> von Hand noetig.</description></item>
    /// </list>
    /// <para>
    /// <b>Gesetzte Defaults dieser Stufe (im Bericht ausgewiesen).</b>
    /// </para>
    /// <list type="number">
    ///   <item><description><b>Frage 6 — ScottPlot statt ChartManager.</b> Die Heatmap
    ///     und die Schnittkurve zeichnet <c>ScottPlot.WinForms</c> 5.1.57. Drei Gruende:
    ///     Der <c>ChartManager</c> ist <c>internal</c> und kennt ueberhaupt keine
    ///     Heatmap; ScottPlot ist im Projekt bereits referenziert, aber nirgends
    ///     verwendet — dieser Dialog ist isoliertes Neuland und damit der risikoaermste
    ///     Ort fuer den Einstieg; und fuer ein zoombares 2D-Feld ist ScottPlot die
    ///     technisch bessere Wahl. Der Hausstandard bleibt fuer alle
    ///     Bestandsdiagramme unberuehrt.</description></item>
    ///   <item><description><b>CSV mit eigenem Schreiber.</b>
    ///     <c>CsvExportClass</c> ist auf <b>Zeitreihen</b> zugeschnitten (Zeitstempel je
    ///     Zeile, eingebaute Rasterumrechnung 8760 ↔ 35040). Eine 10x6-Matrix ist keine
    ///     Zeitreihe; sie durch diese Klasse zu pressen haette Zeitstempel erfunden, die
    ///     es nicht gibt. Geschrieben wird deshalb hier, aber mit <b>denselben
    ///     Konventionen</b>: Semikolon als Trenner, Dezimalkomma, UTF-8 —
    ///     direkt in deutschem Excel zu oeffnen.</description></item>
    ///   <item><description><b>Vorbelegung des Suchraums.</b> Vorgabe ist der Vorschlag
    ///     des Fachkonzepts (500 … 5.000 kWh, 0,5 … 3,0 C). Liegt die aktuelle
    ///     Auslegung ausserhalb dieses Bandes, wird stattdessen ein um sie zentrierter
    ///     Bereich vorgeschlagen — sonst begaenne ein 50-kWh-Projekt seine
    ///     Auslegungssuche bei 500 kWh. Die Felder sind in jedem Fall
    ///     ueberschreibbar.</description></item>
    /// </list>
    /// <para>
    /// Der Dialog ist vollstaendig im Quelltext aufgebaut — keine
    /// <c>.Designer.cs</c>, keine <c>.resx</c> (Projektregel: Designer- und
    /// resx-Dateien nicht von Hand editieren). Alle Beschriftungen kommen aus
    /// <c>MyResource</c> (<c>OPT_*</c>) und sind damit zweisprachig; Zahlen werden nach
    /// dem Hausmuster mit <c>Program.ZahlPruefen</c> gelesen und mit
    /// <c>Program.ZahlFaerben</c> gefaerbt.
    /// </para>
    /// </remarks>
    public class Form_SpeicherOptimierung : Form
    {
        // ==================================================================
        // Zustand
        // ==================================================================

        private readonly SimulationControl m_Sim;
        private readonly int m_ID_Projekt;

        private OptimiererErgebnis m_Ergebnis;
        private CancellationTokenSource m_Abbruch;
        private bool m_bAufbau = true;

        /// <summary>
        /// <c>true</c>, wenn der Anwender die Auslegung in die Geraetedaten uebernommen
        /// hat — dann muss die aufrufende Parameterseite ihre Anzeige auffrischen.
        /// </summary>
        public bool AuslegungUebernommen { get; private set; }

        // --- Steuerelemente ---
        private TextBox tb_CMin, tb_CMax, tb_Stuetzstellen, tb_RMin, tb_RMax, tb_RSchritt;
        private CheckBox chk_Feinraster, chk_KVer;
        private ComboBox cbo_Strategie;
        private Label lbl_Punkte, lbl_Aktuell, lbl_KVerHinweis, lbl_Status, lbl_Zelle, lbl_Warnung;
        private Button btn_Start, btn_Abbruch, btn_Uebernehmen, btn_Csv, btn_Schliessen;
        private ProgressBar bar_Fortschritt;
        private ScottPlot.WinForms.FormsPlot plot_Heatmap;
        private ScottPlot.WinForms.FormsPlot plot_Schnitt;
        private ListView list_Kennzahlen;

        // --- Heatmap-Zuordnung Pixelkoordinate -> Rasterpunkt ---
        //
        // Die Heatmap laeuft bewusst auf INDEXACHSEN (x = 0 … Spalten, y = 0 … Zeilen)
        // statt auf den Zahlenwerten: Die Zellgrenzen liegen dann auf ganzen Zahlen,
        // das Zuordnen einer Mausposition ist eine Abrundung, und die Achsen tragen
        // ueber einen manuellen Tick-Generator die echten Werte als Beschriftung. Auf
        // Wertachsen waeren die Zellen des Feinrasters anders breit als die des
        // Grobrasters, ohne dass das irgendetwas aussagt.
        private OptimiererRaster m_Raster;

        /// <summary>Auswahleintrag der Strategieliste: Steuerwert und Anzeigetext getrennt.</summary>
        private sealed class StrategieEintrag
        {
            public readonly OptimiererStrategie Wert;
            private readonly string _anzeige;

            public StrategieEintrag(OptimiererStrategie wert, string anzeige)
            {
                Wert = wert;
                _anzeige = anzeige;
            }

            public override string ToString() { return _anzeige; }
        }

        // ==================================================================
        // Aufbau
        // ==================================================================

        /// <summary>
        /// Baut die Maske zu einer bereits gerechneten Simulation.
        /// </summary>
        /// <param name="sim">
        /// Simulationsobjekt der aufrufenden Seite. Ohne gerechneten Lauf bleibt der
        /// Startknopf gesperrt — die Rastersuche braucht Lastgang und Erzeugungsreihen.
        /// </param>
        /// <param name="idProjekt">Projekt-ID.</param>
        public Form_SpeicherOptimierung(SimulationControl sim, int idProjekt)
        {
            m_Sim = sim;
            m_ID_Projekt = idProjekt;

            AufbauSteuerelemente();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            VorbelegungSetzen();
            m_bAufbau = false;

            PunktzahlAktualisieren();
            ZustandSetzen(false);

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        private void AufbauSteuerelemente()
        {
            Text = MyResource.Resource.OPT_TITEL;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 862);
            MinimumSize = new Size(1000, 720);

            // ---------------------------------------------------------- Suchraum
            GroupBox grp = new GroupBox();
            grp.SetBounds(12, 8, 1160, 132);
            grp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grp.Text = MyResource.Resource.OPT_GRP_SUCHRAUM;
            Controls.Add(grp);

            const int s1 = 14, e1 = 166, s2 = 310, e2 = 462, s3 = 606, e3 = 758;
            int y = 24;

            Beschriftung(grp, MyResource.Resource.OPT_LBL_CMIN, s1, y);
            tb_CMin = Zahlfeld(grp, e1, y);
            Beschriftung(grp, MyResource.Resource.OPT_LBL_CMAX, s2, y);
            tb_CMax = Zahlfeld(grp, e2, y);
            Beschriftung(grp, MyResource.Resource.OPT_LBL_STUETZSTELLEN, s3, y);
            tb_Stuetzstellen = Ganzzahlfeld(grp, e3, y);
            tb_Stuetzstellen.TextChanged += (s, e) => PunktzahlAktualisieren();

            lbl_Punkte = new Label();
            lbl_Punkte.SetBounds(904, y + 4, 240, 18);
            lbl_Punkte.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            grp.Controls.Add(lbl_Punkte);

            y += 28;
            Beschriftung(grp, MyResource.Resource.OPT_LBL_RMIN, s1, y);
            tb_RMin = Zahlfeld(grp, e1, y);
            tb_RMin.TextChanged += (s, e) => PunktzahlAktualisieren();
            Beschriftung(grp, MyResource.Resource.OPT_LBL_RMAX, s2, y);
            tb_RMax = Zahlfeld(grp, e2, y);
            tb_RMax.TextChanged += (s, e) => PunktzahlAktualisieren();
            Beschriftung(grp, MyResource.Resource.OPT_LBL_RSCHRITT, s3, y);
            tb_RSchritt = Zahlfeld(grp, e3, y);
            tb_RSchritt.TextChanged += (s, e) => PunktzahlAktualisieren();

            chk_Feinraster = new CheckBox();
            chk_Feinraster.SetBounds(904, y, 240, 22);
            chk_Feinraster.Text = MyResource.Resource.OPT_CHK_FEINRASTER;
            chk_Feinraster.Checked = true;
            chk_Feinraster.CheckedChanged += (s, e) => PunktzahlAktualisieren();
            grp.Controls.Add(chk_Feinraster);

            y += 28;
            Beschriftung(grp, MyResource.Resource.OPT_LBL_STRATEGIE, s1, y);
            cbo_Strategie = new ComboBox();
            cbo_Strategie.SetBounds(e1, y, 200, 22);
            cbo_Strategie.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo_Strategie.Items.Add(new StrategieEintrag(OptimiererStrategie.Dauernutzung,
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_DAUERNUTZUNG));
            cbo_Strategie.Items.Add(new StrategieEintrag(OptimiererStrategie.Nachtnutzung,
                MyResource.Resource.SP_BERECHNUNG_ANZEIGE_NACHTNUTZUNG));
            cbo_Strategie.SelectedIndex = 0;
            grp.Controls.Add(cbo_Strategie);

            chk_KVer = new CheckBox();
            chk_KVer.SetBounds(s2, y, 280, 22);
            chk_KVer.Text = MyResource.Resource.OPT_CHK_KVER;
            chk_KVer.CheckedChanged += KVerGeaendert;
            grp.Controls.Add(chk_KVer);

            lbl_KVerHinweis = new Label();
            lbl_KVerHinweis.SetBounds(s2 + 284, y - 4, 560, 32);
            lbl_KVerHinweis.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            lbl_KVerHinweis.ForeColor = Color.Firebrick;
            grp.Controls.Add(lbl_KVerHinweis);

            y += 28;
            lbl_Aktuell = new Label();
            lbl_Aktuell.SetBounds(s1, y + 4, 400, 18);
            lbl_Aktuell.ForeColor = SystemColors.GrayText;
            grp.Controls.Add(lbl_Aktuell);

            Label hinweisZiel = new Label();
            hinweisZiel.SetBounds(s2, y, 830, 32);
            hinweisZiel.Text = MyResource.Resource.OPT_HINWEIS_ZIELFUNKTION;
            hinweisZiel.Font = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            hinweisZiel.ForeColor = SystemColors.GrayText;
            grp.Controls.Add(hinweisZiel);

            // ---------------------------------------------------------- Aktionen
            btn_Start = new Button();
            btn_Start.SetBounds(12, 150, 190, 30);
            btn_Start.Text = MyResource.Resource.OPT_BTN_START;
            btn_Start.Click += Start_Click;
            Controls.Add(btn_Start);

            btn_Abbruch = new Button();
            btn_Abbruch.SetBounds(210, 150, 130, 30);
            btn_Abbruch.Text = MyResource.Resource.OPT_BTN_ABBRUCH;
            btn_Abbruch.Enabled = false;
            btn_Abbruch.Click += Abbruch_Click;
            Controls.Add(btn_Abbruch);

            bar_Fortschritt = new ProgressBar();
            bar_Fortschritt.SetBounds(352, 155, 380, 20);
            bar_Fortschritt.Minimum = 0;
            bar_Fortschritt.Maximum = 100;
            Controls.Add(bar_Fortschritt);

            lbl_Status = new Label();
            lbl_Status.SetBounds(742, 158, 430, 18);
            lbl_Status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Status.AutoEllipsis = true;
            Controls.Add(lbl_Status);

            // ---------------------------------------------------------- Warnbanner
            lbl_Warnung = new Label();
            lbl_Warnung.SetBounds(12, 186, 1160, 40);
            lbl_Warnung.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Warnung.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
            lbl_Warnung.ForeColor = Color.Firebrick;
            Controls.Add(lbl_Warnung);

            // ---------------------------------------------------------- Heatmap
            plot_Heatmap = new ScottPlot.WinForms.FormsPlot();
            plot_Heatmap.SetBounds(12, 230, 646, 552);
            plot_Heatmap.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            plot_Heatmap.MouseMove += Heatmap_MouseMove;
            Controls.Add(plot_Heatmap);

            lbl_Zelle = new Label();
            lbl_Zelle.SetBounds(12, 786, 646, 18);
            lbl_Zelle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lbl_Zelle.Text = MyResource.Resource.OPT_ZELLE_LEER;
            lbl_Zelle.ForeColor = SystemColors.GrayText;
            Controls.Add(lbl_Zelle);

            // ---------------------------------------------------------- Schnittkurve
            plot_Schnitt = new ScottPlot.WinForms.FormsPlot();
            plot_Schnitt.SetBounds(666, 230, 506, 248);
            plot_Schnitt.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(plot_Schnitt);

            // ---------------------------------------------------------- Kennzahlen
            list_Kennzahlen = new ListView();
            list_Kennzahlen.SetBounds(666, 486, 506, 296);
            list_Kennzahlen.Anchor = AnchorStyles.Top | AnchorStyles.Bottom
                                     | AnchorStyles.Left | AnchorStyles.Right;
            list_Kennzahlen.View = View.Details;
            list_Kennzahlen.FullRowSelect = true;
            list_Kennzahlen.GridLines = true;
            list_Kennzahlen.MultiSelect = false;
            list_Kennzahlen.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list_Kennzahlen.Columns.Add(MyResource.Resource.OPT_SP_GROESSE, 250);
            list_Kennzahlen.Columns.Add(MyResource.Resource.OPT_SP_WERT, 130, HorizontalAlignment.Right);
            list_Kennzahlen.Columns.Add(MyResource.Resource.OPT_SP_EINHEIT, 100);
            Controls.Add(list_Kennzahlen);

            // ---------------------------------------------------------- Fusszeile
            btn_Uebernehmen = new Button();
            btn_Uebernehmen.SetBounds(12, 814, 210, 30);
            btn_Uebernehmen.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_Uebernehmen.Text = MyResource.Resource.OPT_BTN_UEBERNEHMEN;
            btn_Uebernehmen.Click += Uebernehmen_Click;
            Controls.Add(btn_Uebernehmen);

            btn_Csv = new Button();
            btn_Csv.SetBounds(230, 814, 190, 30);
            btn_Csv.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_Csv.Text = MyResource.Resource.OPT_BTN_CSV;
            btn_Csv.Click += Csv_Click;
            Controls.Add(btn_Csv);

            btn_Schliessen = new Button();
            btn_Schliessen.SetBounds(1078, 814, 94, 30);
            btn_Schliessen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Schliessen.Text = MyResource.Resource.OPT_BTN_SCHLIESSEN;
            btn_Schliessen.Click += (s, e) => Close();
            Controls.Add(btn_Schliessen);

            CancelButton = btn_Schliessen;
            FormClosing += Maske_FormClosing;

            DiagrammeLeeren();
        }

        private static Label Beschriftung(Control eltern, string text, int x, int y)
        {
            Label l = new Label();
            l.SetBounds(x, y + 4, 148, 18);
            l.Text = text;
            eltern.Controls.Add(l);
            return l;
        }

        /// <summary>Zahlfeld nach Hausmuster: <c>Program.ZahlFaerben</c> am <c>TextChanged</c>.</summary>
        private static TextBox Zahlfeld(Control eltern, int x, int y)
        {
            TextBox tb = LeeresFeld(eltern, x, y);
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            return tb;
        }

        /// <summary>
        /// Ganzzahlfeld nach Hausmuster. Eigener Weg, weil <c>GanzzahlFaerben</c> das
        /// Dezimaltrennzeichen ablehnt — die Stuetzstellenzahl ist eine Stueckzahl.
        /// </summary>
        private static TextBox Ganzzahlfeld(Control eltern, int x, int y)
        {
            TextBox tb = LeeresFeld(eltern, x, y);
            tb.TextChanged += (s, e) => Program.GanzzahlFaerben(s);
            return tb;
        }

        private static TextBox LeeresFeld(Control eltern, int x, int y)
        {
            TextBox tb = new TextBox();
            tb.SetBounds(x, y, 110, 22);
            tb.TextAlign = HorizontalAlignment.Right;
            eltern.Controls.Add(tb);
            return tb;
        }

        // ==================================================================
        // Vorbelegung
        // ==================================================================

        /// <summary>
        /// Belegt den Suchraum vor und zeigt die aktuelle Auslegung an.
        /// </summary>
        /// <remarks>
        /// Vorgabe ist der Vorschlag des Fachkonzepts (500 … 5.000 kWh). Liegt die
        /// aktuelle Kapazitaet ausserhalb, wird stattdessen ein um sie zentrierter
        /// Bereich vorgeschlagen — ein 50-kWh-Projekt bekaeme sonst ein Raster, das mit
        /// dem Zehnfachen seiner Groesse beginnt, und muesste vor dem ersten Lauf drei
        /// Felder korrigieren. Der Suchraum bleibt in jedem Fall frei ueberschreibbar.
        /// </remarks>
        private void VorbelegungSetzen()
        {
            OptimiererOptionen vorgabe = new OptimiererOptionen();
            CultureInfo k = CultureInfo.CurrentCulture;

            double cNom = 0.0;
            double pKw = 0.0;

            try
            {
                SpeicherParameter aktuell = new StromspeicherSimCtrl().LeseParameter(m_ID_Projekt);
                if (aktuell != null)
                {
                    cNom = aktuell.CNomKwh;
                    pKw = aktuell.PKw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die aktuelle Speicherauslegung konnte nicht gelesen werden: " + ex.Message);
            }

            double cMin = vorgabe.CMinKwh;
            double cMax = vorgabe.CMaxKwh;

            if (cNom > 0.0)
            {
                lbl_Aktuell.Text = string.Format(k, MyResource.Resource.OPT_LBL_AKTUELL,
                                                 cNom.ToString("0.#", k),
                                                 pKw.ToString("0.#", k),
                                                 (cNom > 0.0 ? pKw / cNom : 0.0).ToString("0.##", k));

                if (cNom < cMin || cNom > cMax)
                {
                    cMin = Math.Max(1.0, cNom * 0.25);
                    cMax = cNom * 2.5;
                }
            }

            tb_CMin.Text = cMin.ToString("0.#", k);
            tb_CMax.Text = cMax.ToString("0.#", k);
            tb_Stuetzstellen.Text = vorgabe.Stuetzstellen.ToString(k);
            tb_RMin.Text = vorgabe.RMin.ToString("0.##", k);
            tb_RMax.Text = vorgabe.RMax.ToString("0.##", k);
            tb_RSchritt.Text = vorgabe.RSchritt.ToString("0.##", k);

            lbl_Status.Text = LaufMoeglich()
                ? MyResource.Resource.OPT_STATUS_BEREIT
                : MyResource.Resource.OPT_MSG_KEIN_LAUF;
        }

        /// <summary>
        /// Ob ueberhaupt gerechnet werden kann: Die Rastersuche liest Lastgang und
        /// Erzeugungsreihen aus einem <b>gelaufenen</b> Simulationsdurchgang.
        /// </summary>
        private bool LaufMoeglich()
        {
            return m_Sim != null && m_Sim.simulation_Strombedarf != null;
        }

        private void KVerGeaendert(object sender, EventArgs e)
        {
            lbl_KVerHinweis.Text = chk_KVer.Checked ? MyResource.Resource.OPT_HINWEIS_KVER : "";
        }

        /// <summary>Zeigt an, wie viele Jahreslaeufe der eingestellte Suchraum kostet.</summary>
        private void PunktzahlAktualisieren()
        {
            if (m_bAufbau || lbl_Punkte == null) return;

            OptimiererOptionen opt = OptionenAusFeldern(false);
            lbl_Punkte.Text = opt == null
                ? ""
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.OPT_LBL_PUNKTE,
                                opt.PunkteGesamt);
        }

        // ==================================================================
        // Eingaben lesen
        // ==================================================================

        /// <summary>
        /// Baut die Optionen aus den Feldern.
        /// </summary>
        /// <param name="melden">
        /// <c>true</c> = unbrauchbare Eingaben werden gemeldet und der Fokus gesetzt
        /// (Knopfdruck); <c>false</c> = stilles Scheitern mit <c>null</c> (laufende
        /// Punktzahlanzeige waehrend der Eingabe).
        /// </param>
        private OptimiererOptionen OptionenAusFeldern(bool melden)
        {
            double cMin, cMax, rMin, rMax, rSchritt;
            int stuetzstellen;

            if (melden)
            {
                if (!Program.ZahlPruefen(tb_CMin, MyResource.Resource.OPT_LBL_CMIN, out cMin)) return null;
                if (!Program.ZahlPruefen(tb_CMax, MyResource.Resource.OPT_LBL_CMAX, out cMax)) return null;
                if (!Program.GanzzahlPruefen(tb_Stuetzstellen, MyResource.Resource.OPT_LBL_STUETZSTELLEN,
                                             out stuetzstellen)) return null;
                if (!Program.ZahlPruefen(tb_RMin, MyResource.Resource.OPT_LBL_RMIN, out rMin)) return null;
                if (!Program.ZahlPruefen(tb_RMax, MyResource.Resource.OPT_LBL_RMAX, out rMax)) return null;
                if (!Program.ZahlPruefen(tb_RSchritt, MyResource.Resource.OPT_LBL_RSCHRITT, out rSchritt)) return null;
            }
            else
            {
                if (!Program.ZahlParsen(tb_CMin.Text, out cMin)) return null;
                if (!Program.ZahlParsen(tb_CMax.Text, out cMax)) return null;
                if (!Program.GanzzahlParsen(tb_Stuetzstellen.Text, out stuetzstellen)) return null;
                if (!Program.ZahlParsen(tb_RMin.Text, out rMin)) return null;
                if (!Program.ZahlParsen(tb_RMax.Text, out rMax)) return null;
                if (!Program.ZahlParsen(tb_RSchritt.Text, out rSchritt)) return null;
            }

            // Fachliche Pruefung: dieselben Bedingungen wie OptimiererOptionen.Pruefe,
            // hier aber mit einer Meldung im Klartext statt einer Ausnahme.
            if (melden)
            {
                if (!(cMin > 0.0)) { Melden(MyResource.Resource.OPT_MSG_CMIN, tb_CMin); return null; }
                if (!(cMax > cMin)) { Melden(MyResource.Resource.OPT_MSG_CMAX, tb_CMax); return null; }
                if (stuetzstellen < 2) { Melden(MyResource.Resource.OPT_MSG_STUETZSTELLEN, tb_Stuetzstellen); return null; }
                if (!(rMin > 0.0)) { Melden(MyResource.Resource.OPT_MSG_RMIN, tb_RMin); return null; }
                if (rMax < rMin) { Melden(MyResource.Resource.OPT_MSG_RMAX, tb_RMax); return null; }
                if (!(rSchritt > 0.0)) { Melden(MyResource.Resource.OPT_MSG_RSCHRITT, tb_RSchritt); return null; }
            }
            else
            {
                if (!(cMin > 0.0) || !(cMax > cMin) || stuetzstellen < 2
                    || !(rMin > 0.0) || rMax < rMin || !(rSchritt > 0.0)) return null;
            }

            StrategieEintrag gewaehlt = cbo_Strategie.SelectedItem as StrategieEintrag;

            return new OptimiererOptionen
            {
                CMinKwh = cMin,
                CMaxKwh = cMax,
                Stuetzstellen = stuetzstellen,
                RMin = rMin,
                RMax = rMax,
                RSchritt = rSchritt,
                Feinraster = chk_Feinraster.Checked,
                KVerInZielfunktion = chk_KVer.Checked,
                Strategie = gewaehlt != null ? gewaehlt.Wert : OptimiererStrategie.Dauernutzung
            };
        }

        private void Melden(string text, Control fokus)
        {
            MessageBox.Show(this, text, MyResource.Resource.OPT_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (fokus != null) fokus.Focus();
        }

        // ==================================================================
        // Lauf (Task.Run + IProgress + CancellationToken)
        // ==================================================================

        /// <summary>
        /// Startet die Rastersuche im Hintergrund.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>async void</c> ist hier korrekt und nicht die bekannte Falle: Es ist ein
        /// Ereignisbehandler, und genau dafuer ist die Form vorgesehen. Alle Ausnahmen
        /// werden im Rumpf gefangen — es gibt keinen <c>Task</c>, auf den ein Aufrufer
        /// warten koennte, und damit auch keinen Ort, an dem eine unbehandelte Ausnahme
        /// sonst landen wuerde.
        /// </para>
        /// <para>
        /// <b>Reihenfolge.</b> (1) Felder lesen, (2) <b>Datenbankzugriff auf dem
        /// UI-Thread</b>, (3) reine Rechnung in <c>Task.Run</c>, (4) Anzeige wieder auf
        /// dem UI-Thread (nach <c>await</c> setzt der SynchronizationContext den
        /// Ablauf dort fort).
        /// </para>
        /// </remarks>
        private async void Start_Click(object sender, EventArgs e)
        {
            if (!LaufMoeglich())
            {
                Melden(MyResource.Resource.OPT_MSG_KEIN_LAUF, null);
                return;
            }

            OptimiererOptionen optionen = OptionenAusFeldern(true);
            if (optionen == null) return;

            // ---- (2) Datenbank, UI-Thread ---------------------------------
            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            StromspeicherOptimierungVorbereitung vorbereitung;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                vorbereitung = ctrl.BereiteOptimierungVor(m_Sim, m_ID_Projekt);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Melden(string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message), null);
                return;
            }
            finally { Cursor.Current = Cursors.Default; }

            if (vorbereitung == null)
            {
                Melden(string.IsNullOrEmpty(ctrl.LetzterHinweis)
                           ? MyResource.Resource.SIMENG_SPEICHER_KEIN_SPEICHER
                           : ctrl.LetzterHinweis, null);
                return;
            }

            // ---- (3) Rechnung, Hintergrund-Task ---------------------------
            m_Abbruch = new CancellationTokenSource();
            CancellationToken marke = m_Abbruch.Token;

            // Auf dem UI-Thread erzeugt: Progress<T> merkt sich dessen
            // SynchronizationContext und ruft FortschrittAnzeigen von selbst wieder
            // dort auf - kein Invoke von Hand.
            int gesamt = optionen.PunkteGesamt;
            IProgress<OptimiererFortschritt> melder =
                new Progress<OptimiererFortschritt>(FortschrittAnzeigen);

            ZustandSetzen(true);
            bar_Fortschritt.Value = 0;
            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture,
                                            MyResource.Resource.OPT_STATUS_PUNKT, 0, gesamt);
            ErgebnisLeeren();

            try
            {
                OptimiererErgebnis ergebnis = await Task.Run(
                    () => StromspeicherSimCtrl.FuehreOptimierungAus(vorbereitung, optionen, melder, marke),
                    marke);

                // ---- (4) Anzeige, wieder UI-Thread ------------------------
                //
                // Der haeufigste Grund fuer einen Abbruch ist, dass der Anwender die
                // Maske zumacht. Dann ist sie nach dem await bereits entsorgt (das
                // "using" des Aufrufers greift, sobald ShowDialog zurueckkehrt), und
                // jeder Zugriff auf ein Steuerelement wuerde mit
                // ObjectDisposedException in einer async-void-Fortsetzung landen -
                // also unbehandelt. Deshalb steht die Pruefung in JEDEM Zweig.
                if (Entsorgt()) return;

                m_Ergebnis = ergebnis;
                ErgebnisAnzeigen();
            }
            catch (OperationCanceledException)
            {
                if (Entsorgt()) return;
                lbl_Status.Text = MyResource.Resource.OPT_STATUS_ABGEBROCHEN;
                bar_Fortschritt.Value = 0;
            }
            catch (Exception ex)
            {
                if (Entsorgt()) return;
                lbl_Status.Text = "";
                Melden(string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message), null);
            }
            finally
            {
                CancellationTokenSource quelle = m_Abbruch;
                m_Abbruch = null;
                if (quelle != null) quelle.Dispose();

                if (!Entsorgt()) ZustandSetzen(false);
            }
        }

        /// <summary>Ob die Maske schon zu ist — dann darf kein Steuerelement mehr angefasst werden.</summary>
        private bool Entsorgt()
        {
            return IsDisposed || Disposing;
        }

        private void Abbruch_Click(object sender, EventArgs e)
        {
            Abbrechen();
            btn_Abbruch.Enabled = false;
        }

        /// <summary>
        /// Setzt die Abbruchmarke, falls ein Lauf laeuft.
        /// </summary>
        /// <remarks>
        /// Der <see cref="CancellationTokenSource"/> wird vom <c>finally</c> des Laufs
        /// entsorgt; zwischen Entsorgen und Nullsetzen kann ein zweiter Aufruf
        /// hineinlaufen, deshalb die lokale Kopie und der Fang der
        /// <see cref="ObjectDisposedException"/>.
        /// </remarks>
        private void Abbrechen()
        {
            CancellationTokenSource quelle = m_Abbruch;
            if (quelle == null) return;

            try { quelle.Cancel(); }
            catch (ObjectDisposedException) { /* Lauf war ohnehin schon zu Ende */ }
        }

        /// <summary>
        /// Beendet einen laufenden Suchlauf, wenn die Maske geschlossen wird.
        /// </summary>
        /// <remarks>
        /// Der Task laeuft sonst weiter und meldete seinen Fortschritt an
        /// Steuerelemente, die es nicht mehr gibt. Der Abbruch ist kooperativ und
        /// greift innerhalb eines Rasterpunkts, also im Millisekundenbereich — auf
        /// sein Ende zu warten ist deshalb nicht noetig, die Maske darf sofort zu.
        /// </remarks>
        private void Maske_FormClosing(object sender, FormClosingEventArgs e)
        {
            Abbrechen();
        }

        private void FortschrittAnzeigen(OptimiererFortschritt stand)
        {
            // Progress<T> stellt die Meldung in die Nachrichtenschlange; sie kann also
            // noch eintreffen, nachdem die Maske schon zu ist.
            if (Entsorgt() || bar_Fortschritt == null || bar_Fortschritt.IsDisposed) return;

            int prozent = (int)Math.Round(stand.Anteil * 100.0);
            if (prozent < 0) prozent = 0;
            if (prozent > 100) prozent = 100;
            bar_Fortschritt.Value = prozent;

            lbl_Status.Text = string.Format(CultureInfo.CurrentCulture,
                stand.IstFeinraster
                    ? MyResource.Resource.OPT_STATUS_PUNKT_FEIN
                    : MyResource.Resource.OPT_STATUS_PUNKT,
                stand.Erledigt, stand.Gesamt);
        }

        /// <summary>Sperrt beziehungsweise entsperrt die Bedienung fuer die Dauer des Laufs.</summary>
        private void ZustandSetzen(bool laeuft)
        {
            btn_Start.Enabled = !laeuft && LaufMoeglich();
            btn_Abbruch.Enabled = laeuft;
            btn_Uebernehmen.Enabled = !laeuft && m_Ergebnis != null;
            btn_Csv.Enabled = !laeuft && m_Ergebnis != null;

            tb_CMin.Enabled = !laeuft;
            tb_CMax.Enabled = !laeuft;
            tb_Stuetzstellen.Enabled = !laeuft;
            tb_RMin.Enabled = !laeuft;
            tb_RMax.Enabled = !laeuft;
            tb_RSchritt.Enabled = !laeuft;
            chk_Feinraster.Enabled = !laeuft;
            chk_KVer.Enabled = !laeuft;
            cbo_Strategie.Enabled = !laeuft;
        }

        // ==================================================================
        // Ergebnisanzeige
        // ==================================================================

        private void ErgebnisLeeren()
        {
            m_Ergebnis = null;
            m_Raster = null;
            list_Kennzahlen.Items.Clear();
            list_Kennzahlen.Groups.Clear();
            lbl_Warnung.Text = "";
            lbl_Zelle.Text = MyResource.Resource.OPT_ZELLE_LEER;
            DiagrammeLeeren();
        }

        private void DiagrammeLeeren()
        {
            plot_Heatmap.Plot.Clear();
            plot_Heatmap.Plot.Title(MyResource.Resource.OPT_CHART_HEATMAP_TITEL);
            plot_Heatmap.Plot.XLabel(MyResource.Resource.OPT_CHART_X_CRATE);
            plot_Heatmap.Plot.YLabel(MyResource.Resource.OPT_CHART_Y_KAPAZITAET);
            plot_Heatmap.Refresh();

            plot_Schnitt.Plot.Clear();
            plot_Schnitt.Plot.XLabel(MyResource.Resource.OPT_CHART_Y_KAPAZITAET);
            plot_Schnitt.Plot.YLabel(MyResource.Resource.OPT_CHART_SCHNITT_Y);
            plot_Schnitt.Refresh();
        }

        private void ErgebnisAnzeigen()
        {
            if (m_Ergebnis == null) return;

            CultureInfo k = CultureInfo.CurrentCulture;
            m_Raster = m_Ergebnis.BestRaster;

            lbl_Status.Text = string.Format(k, MyResource.Resource.OPT_STATUS_FERTIG,
                                            m_Ergebnis.PunkteGerechnet,
                                            m_Ergebnis.Dauer.TotalSeconds.ToString("0.0", k),
                                            m_Ergebnis.BestPunkt.CNomKwh.ToString("0.#", k),
                                            m_Ergebnis.BestPunkt.CRate.ToString("0.##", k));
            bar_Fortschritt.Value = 100;

            WarnungenSetzen();
            HeatmapZeichnen();
            SchnittkurveZeichnen();
            KennzahlenFuellen();

            btn_Uebernehmen.Enabled = true;
            btn_Csv.Enabled = true;
        }

        /// <summary>
        /// Setzt das Warnbanner: Randloesung, c_pow-Neutralitaet, K_ver-Option,
        /// Zyklenbudget (Fachkonzept 6.3 / 5.4).
        /// </summary>
        private void WarnungenSetzen()
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            List<string> zeilen = new List<string>();

            OptimiererRandlage rand = m_Ergebnis.Randlage;
            if (rand.Vorhanden)
            {
                List<string> kanten = new List<string>();
                if (rand.KapazitaetUnten) kanten.Add(MyResource.Resource.OPT_WARN_RAND_C_UNTEN);
                if (rand.KapazitaetOben) kanten.Add(MyResource.Resource.OPT_WARN_RAND_C_OBEN);
                if (rand.CRateUnten) kanten.Add(MyResource.Resource.OPT_WARN_RAND_R_UNTEN);
                if (rand.CRateOben) kanten.Add(MyResource.Resource.OPT_WARN_RAND_R_OBEN);
                zeilen.Add(string.Format(k, MyResource.Resource.OPT_WARN_RAND,
                                         string.Join(", ", kanten.ToArray())));
            }

            if (m_Ergebnis.CPowNeutral) zeilen.Add(MyResource.Resource.OPT_WARN_CPOW);
            if (m_Ergebnis.KVerInZielfunktion) zeilen.Add(MyResource.Resource.OPT_WARN_KVER_AKTIV);

            if (m_Ergebnis.BestPunkt.ZyklenbudgetUeberschritten)
                zeilen.Add(string.Format(k, MyResource.Resource.OPT_WARN_ZYKLEN,
                                         m_Ergebnis.BestPunkt.ZyklenNutzungsdauer.ToString("0", k),
                                         m_Ergebnis.Optionen.ZyklenZugesichert.ToString("0", k)));

            lbl_Warnung.Text = string.Join(Environment.NewLine, zeilen.ToArray());
        }

        // ------------------------------------------------------------------
        // Heatmap (ScottPlot 5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeichnet das Raster als Heatmap Kapazitaet x C-Rate mit Dreifarbskala und
        /// markiertem Optimum (Fachkonzept 6.3).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Achsen sind Indexachsen.</b> Die Zellen liegen auf ganzzahligen Grenzen
        /// (x = 0 … Spalten, y = 0 … Zeilen); die echten Werte stehen ueber einen
        /// manuellen Tick-Generator an den Zellmitten. Das haelt die Zellen gleich breit
        /// (auch im Feinraster mit seinem engeren Kapazitaetsband) und macht das
        /// Zuordnen einer Mausposition zu einer Abrundung.
        /// </para>
        /// <para>
        /// <b>Zeilenrichtung.</b> ScottPlot zeichnet Zeile 0 der Matrix <b>oben</b>. Die
        /// Matrix wird deshalb umgedreht befuellt (<c>zeilen-1-i</c>), damit die
        /// Kapazitaet wie erwartet nach oben waechst. Umgedreht wird beim Befuellen und
        /// nicht ueber <c>FlipVertically</c>, weil dieses Flag nur die Darstellung
        /// dreht, nicht aber die Indexrueckrechnung — Bild und Zellabfrage waeren dann
        /// gegeneinander verschoben.
        /// </para>
        /// <para>
        /// <b>Dreifarbskala</b> ueber <c>Colormaps.CustomInterpolated</c>: Rot fuer den
        /// schlechtesten, Gelb fuer den mittleren, Gruen fuer den besten Zielwert. Die
        /// Skala ist rein relativ zum gezeigten Raster — sie sagt nichts darueber, ob
        /// der beste Punkt wirtschaftlich ist; dafuer steht die Zahl daneben.
        /// </para>
        /// </remarks>
        private void HeatmapZeichnen()
        {
            OptimiererRaster raster = m_Raster;
            ScottPlot.Plot plot = plot_Heatmap.Plot;
            plot.Clear();

            int zeilen = raster.Zeilen;
            int spalten = raster.Spalten;

            double[,] werte = new double[zeilen, spalten];
            for (int i = 0; i < zeilen; i++)
                for (int s = 0; s < spalten; s++)
                    werte[zeilen - 1 - i, s] = raster.Punkte[i][s].ZielfunktionEur;

            ScottPlot.Plottables.Heatmap karte = plot.Add.Heatmap(werte);
            karte.Extent = new ScottPlot.CoordinateRect(0, spalten, 0, zeilen);
            karte.Smooth = false;
            karte.Colormap = new ScottPlot.Colormaps.CustomInterpolated(new ScottPlot.Color[]
            {
                ScottPlot.Color.FromColor(Color.Firebrick),
                ScottPlot.Color.FromColor(Color.Gold),
                ScottPlot.Color.FromColor(Color.ForestGreen)
            });

            ScottPlot.Panels.ColorBar skala = plot.Add.ColorBar(karte, ScottPlot.Edge.Right);
            skala.Label = MyResource.Resource.OPT_CHART_FARBSKALA;

            // Zellmitten: Matrixzeile r liegt bei y in [zeilen-1-r ; zeilen-r]. Wegen
            // der umgedrehten Befuellung (r = zeilen-1-i) faellt die Kapazitaet mit dem
            // Index i damit genau auf y = i + 0,5 - also aufsteigend nach oben.
            int besteZeile = ZeileVon(raster, m_Ergebnis.BestPunkt.CNomKwh);
            int besteSpalte = raster.IndexCRate(m_Ergebnis.BestPunkt.CRate);
            if (besteZeile >= 0 && besteSpalte >= 0)
            {
                plot.Add.Marker(besteSpalte + 0.5, besteZeile + 0.5,
                                ScottPlot.MarkerShape.OpenSquare, 22,
                                ScottPlot.Color.FromColor(Color.Black));
            }

            CultureInfo k = CultureInfo.CurrentCulture;

            ScottPlot.TickGenerators.NumericManual xTicks = new ScottPlot.TickGenerators.NumericManual();
            for (int s = 0; s < spalten; s++)
                xTicks.AddMajor(s + 0.5, raster.CRaten[s].ToString("0.##", k));
            plot.Axes.Bottom.TickGenerator = xTicks;

            ScottPlot.TickGenerators.NumericManual yTicks = new ScottPlot.TickGenerators.NumericManual();
            for (int i = 0; i < zeilen; i++)
                yTicks.AddMajor(i + 0.5, raster.KapazitaetenKwh[i].ToString("0.#", k));
            plot.Axes.Left.TickGenerator = yTicks;

            plot.Title(MyResource.Resource.OPT_CHART_HEATMAP_TITEL);
            plot.XLabel(MyResource.Resource.OPT_CHART_X_CRATE);
            plot.YLabel(MyResource.Resource.OPT_CHART_Y_KAPAZITAET);
            plot.Axes.SetLimits(0, spalten, 0, zeilen);

            plot_Heatmap.Refresh();
        }

        /// <summary>Index der Kapazitaetsachse, die dem Wert am naechsten liegt; -1 bei leerem Raster.</summary>
        private static int ZeileVon(OptimiererRaster raster, double kapazitaetKwh)
        {
            int treffer = -1;
            double abstand = double.MaxValue;
            for (int i = 0; i < raster.KapazitaetenKwh.Count; i++)
            {
                double d = Math.Abs(raster.KapazitaetenKwh[i] - kapazitaetKwh);
                if (d < abstand) { abstand = d; treffer = i; }
            }
            return treffer;
        }

        /// <summary>
        /// Zeigt die Werte der ueberfahrenen Zelle unter dem Diagramm — die
        /// „Tooltip"-Funktion der Heatmap.
        /// </summary>
        /// <remarks>
        /// Die Zuordnung rechnet selbst statt ueber <c>Heatmap.GetIndexes</c>: Auf den
        /// Indexachsen ist sie eine Abrundung, sie ueberlebt jedes Zoomen, und sie
        /// haengt nicht an der Reihenfolge des zurueckgegebenen Indexpaares.
        /// <c>DisplayScale</c> geht mit ein, damit die Zuordnung auch dann stimmt, wenn
        /// das Programm einmal nicht mehr DPI-unaware laeuft.
        /// </remarks>
        private void Heatmap_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_Raster == null || m_Ergebnis == null) return;

            try
            {
                float skala = plot_Heatmap.DisplayScale;
                ScottPlot.Coordinates ort = plot_Heatmap.Plot.GetCoordinates(
                    new ScottPlot.Pixel(e.X * skala, e.Y * skala));

                if (double.IsNaN(ort.X) || double.IsNaN(ort.Y)) return;

                // Auf den Indexachsen ist die Zuordnung eine Abrundung: x -> Spalte
                // (C-Rate), y -> Zeile (Kapazitaet, aufsteigend nach oben).
                int spalte = (int)Math.Floor(ort.X);
                int zeile = (int)Math.Floor(ort.Y);

                if (spalte < 0 || spalte >= m_Raster.Spalten
                    || zeile < 0 || zeile >= m_Raster.Zeilen)
                {
                    lbl_Zelle.Text = MyResource.Resource.OPT_ZELLE_LEER;
                    return;
                }

                OptimiererPunkt p = m_Raster.Punkte[zeile][spalte];
                CultureInfo k = CultureInfo.CurrentCulture;

                lbl_Zelle.Text = string.Format(k, MyResource.Resource.OPT_ZELLE,
                                               p.CNomKwh.ToString("0.#", k),
                                               p.CRate.ToString("0.##", k),
                                               p.PKw.ToString("0.#", k),
                                               p.ZielfunktionEur.ToString("0", k),
                                               AmortisationText(p.StatischeAmortisation));
            }
            catch (Exception ex)
            {
                // Die Zellanzeige ist Beiwerk - ein Fehler hier darf die Maske nicht stoeren.
                Console.WriteLine("Die Zellanzeige der Heatmap ist fehlgeschlagen: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Schnittkurve
        // ------------------------------------------------------------------

        /// <summary>
        /// Schnittkurve dJ(C) bei der besten C-Rate (Fachkonzept 6.3) als
        /// ScottPlot-Linienplot mit markiertem Optimum.
        /// </summary>
        private void SchnittkurveZeichnen()
        {
            OptimiererRaster raster = m_Raster;
            CultureInfo k = CultureInfo.CurrentCulture;

            int spalte = raster.IndexCRate(m_Ergebnis.BestPunkt.CRate);
            if (spalte < 0) return;

            double[] x = new double[raster.Zeilen];
            for (int i = 0; i < raster.Zeilen; i++) x[i] = raster.KapazitaetenKwh[i];
            double[] y = raster.Schnittkurve(spalte);

            ScottPlot.Plot plot = plot_Schnitt.Plot;
            plot.Clear();

            ScottPlot.Plottables.Scatter kurve = plot.Add.Scatter(x, y);
            kurve.LineWidth = 2;
            kurve.MarkerSize = 5;

            plot.Add.Marker(m_Ergebnis.BestPunkt.CNomKwh, m_Ergebnis.BestPunkt.ZielfunktionEur,
                            ScottPlot.MarkerShape.OpenCircle, 14,
                            ScottPlot.Color.FromColor(Color.Firebrick));

            plot.Title(string.Format(k, MyResource.Resource.OPT_CHART_SCHNITT_TITEL,
                                     m_Ergebnis.BestPunkt.CRate.ToString("0.##", k)));
            plot.XLabel(MyResource.Resource.OPT_CHART_Y_KAPAZITAET);
            plot.YLabel(MyResource.Resource.OPT_CHART_SCHNITT_Y);
            plot.Axes.AutoScale();

            plot_Schnitt.Refresh();
        }

        // ------------------------------------------------------------------
        // Kennzahlenblock
        // ------------------------------------------------------------------

        private void KennzahlenFuellen()
        {
            list_Kennzahlen.BeginUpdate();
            try
            {
                list_Kennzahlen.Items.Clear();
                list_Kennzahlen.Groups.Clear();

                OptimiererPunkt p = m_Ergebnis.BestPunkt;

                ListViewGroup auslegung = Gruppe(MyResource.Resource.OPT_KZ_GRUPPE_AUSLEGUNG);
                Kennzahl(auslegung, MyResource.Resource.OPT_KZ_KAPAZITAET, p.CNomKwh, "0.#", "kWh");
                Kennzahl(auslegung, MyResource.Resource.OPT_KZ_CRATE, p.CRate, "0.##", "1/h");
                Kennzahl(auslegung, MyResource.Resource.OPT_KZ_LEISTUNG, p.PKw, "0.#", "kW");

                ListViewGroup wirtschaft = Gruppe(MyResource.Resource.OPT_KZ_GRUPPE_WIRTSCHAFT);
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_ZIELFUNKTION, p.ZielfunktionEur, "0.00", "€/a");
                if (m_Ergebnis.KVerInZielfunktion)
                    Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_UEBERSCHUSS, p.JahresueberschussEur, "0.00", "€/a");
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_ERTRAG1, p.ErtragReferenzjahrEur, "0.00", "€/a");
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_ERTRAGAEQ, p.ErtragAequivalentEur, "0.00", "€/a");
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_INVEST, p.InvestitionEur, "0.00", "€");
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_ANNUITAET, p.AnnuitaetEur, "0.00", "€/a");
                Kennzahl(wirtschaft, MyResource.Resource.OPT_KZ_NPV, p.KapitalwertEur, "0.00", "€");
                Textzeile(wirtschaft, MyResource.Resource.OPT_KZ_AMORT_STAT,
                     AmortisationText(p.StatischeAmortisation), "a");
                Textzeile(wirtschaft, MyResource.Resource.OPT_KZ_AMORT_DYN,
                     AmortisationText(p.DynamischeAmortisation), "a");

                ListViewGroup speicher = Gruppe(MyResource.Resource.OPT_KZ_GRUPPE_SPEICHER);
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_ZYKLEN, p.AequivalenteVollzyklen, "0.0", "1/a");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_ZYKLEN_N, p.ZyklenNutzungsdauer, "0", "");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_KVER, p.VerschleisskostenEurProA, "0.00", "€/a");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_EIGENVERBRAUCH,
                         p.EigenverbrauchsquoteMitSpeicher * 100.0, "0.0", "%");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_AUTARKIE,
                         p.AutarkiegradMitSpeicher * 100.0, "0.0", "%");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_LADEENERGIE, p.LadeenergieKwh, "0", "kWh/a");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_ENTLADEENERGIE, p.EntladeenergieKwh, "0", "kWh/a");
                Kennzahl(speicher, MyResource.Resource.OPT_KZ_VERLUSTE, p.SpeicherverlusteKwh, "0", "kWh/a");
            }
            finally
            {
                list_Kennzahlen.EndUpdate();
            }
        }

        private ListViewGroup Gruppe(string kopf)
        {
            ListViewGroup g = new ListViewGroup(kopf);
            list_Kennzahlen.Groups.Add(g);
            return g;
        }

        /// <summary>Zahlzeile; negative Werte werden rot gefaerbt (<c>ZahlFaerben</c>-Muster).</summary>
        private void Kennzahl(ListViewGroup gruppe, string name, double wert, string format, string einheit)
        {
            ListViewItem eintrag = new ListViewItem(name, gruppe);
            eintrag.SubItems.Add(wert.ToString(format, CultureInfo.CurrentCulture));
            eintrag.SubItems.Add(einheit);
            if (wert < 0.0) eintrag.ForeColor = Color.Firebrick;
            list_Kennzahlen.Items.Add(eintrag);
        }

        /// <summary>
        /// Textzeile. Bewusst nicht <c>Text</c> genannt — das waere die
        /// <see cref="Form.Text"/>-Eigenschaft der Basisklasse und verdeckte sie.
        /// </summary>
        private void Textzeile(ListViewGroup gruppe, string name, string wert, string einheit)
        {
            ListViewItem eintrag = new ListViewItem(name, gruppe);
            eintrag.SubItems.Add(wert);
            eintrag.SubItems.Add(einheit);
            list_Kennzahlen.Items.Add(eintrag);
        }

        /// <summary>
        /// Amortisation als Text — seit iU9-W11a.5 im Kern
        /// (<see cref="SpeicherAnzeigeCtrl.AmortisationText"/>, Befund W11-B42). Diese
        /// Maske bleibt WinForms (iF22); die Weiterleitung haelt ihre sechs
        /// Aufrufstellen unveraendert.
        /// </summary>
        private static string AmortisationText(Amortisation a)
        {
            return SpeicherAnzeigeCtrl.AmortisationText(a);
        }

        // ==================================================================
        // Uebernahme in die Geraetedaten
        // ==================================================================

        /// <summary>
        /// Schreibt Kapazitaet und Leistung des Bestpunkts nach Rueckfrage in die
        /// Geraetedaten.
        /// </summary>
        /// <remarks>
        /// <b>Kein automatisches Nachrechnen.</b> Die Simulation ist danach nicht mehr
        /// aktuell — das steht in der Bestaetigungsmeldung, und der Anwender entscheidet
        /// selbst, wann er den Lauf wiederholt. Ein stiller Neulauf haette an dieser
        /// Stelle die ganze Kette angestossen, ohne dass jemand danach gefragt hat.
        /// </remarks>
        private void Uebernehmen_Click(object sender, EventArgs e)
        {
            if (m_Ergebnis == null)
            {
                Melden(MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS, null);
                return;
            }

            CultureInfo k = CultureInfo.CurrentCulture;
            OptimiererPunkt p = m_Ergebnis.BestPunkt;

            DialogResult antwort = MessageBox.Show(this,
                string.Format(k, MyResource.Resource.OPT_MSG_UEBERNAHME_FRAGE,
                              p.CNomKwh.ToString("0.#", k), p.PKw.ToString("0.#", k)),
                MyResource.Resource.OPT_TITEL, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (antwort != DialogResult.Yes) return;

            StromspeicherSimCtrl ctrl = new StromspeicherSimCtrl();
            bool ok;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                ok = ctrl.UebernehmeAuslegung(m_ID_Projekt, p.CNomKwh, p.PKw);
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                Melden(string.Format(MyResource.Resource.OPT_MSG_FEHLER, ex.Message), null);
                return;
            }
            finally { Cursor.Current = Cursors.Default; }

            if (!ok)
            {
                Melden(string.IsNullOrEmpty(ctrl.LetzterHinweis)
                           ? MyResource.Resource.OPT_MSG_UEBERNAHME_FEHLER
                           : ctrl.LetzterHinweis, null);
                return;
            }

            AuslegungUebernommen = true;
            DialogResult = DialogResult.OK;

            MessageBox.Show(this, MyResource.Resource.OPT_MSG_UEBERNOMMEN,
                            MyResource.Resource.OPT_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================================================================
        // CSV-Export der Rastermatrix
        // ==================================================================

        /// <summary>
        /// Schreibt beide Rasterphasen als CSV.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eigener Schreiber, nicht <c>CsvExportClass</c>.</b> Jene Klasse ist auf
        /// Zeitreihen zugeschnitten — sie stellt jeder Zeile einen Zeitstempel voran und
        /// rechnet zwischen 8760 und 35040 Werten um. Eine Rastermatrix hat weder
        /// Zeitbezug noch ein Zeitraster; durch diese Klasse gepresst haette sie
        /// Zeitstempel bekommen, die nichts bedeuten. Uebernommen sind aber ihre
        /// <b>Konventionen</b>: Semikolon als Feldtrenner, Dezimalkomma der aktuellen
        /// Kultur, UTF-8 mit BOM — so oeffnet die Datei in deutschem Excel direkt
        /// richtig.
        /// </para>
        /// <para>
        /// Geschrieben wird die <b>lange Form</b> (eine Zeile je Rasterpunkt mit allen
        /// Kennzahlen) statt einer reinen Zahlenmatrix: Die Matrix zeigt nur die
        /// Zielfunktion, und wer die Daten exportiert, will in aller Regel gerade die
        /// Sekundaerkennzahlen mit auswerten.
        /// </para>
        /// </remarks>
        private void Csv_Click(object sender, EventArgs e)
        {
            if (m_Ergebnis == null)
            {
                Melden(MyResource.Resource.OPT_MSG_KEIN_ERGEBNIS, null);
                return;
            }

            string vorschlag = string.Format(CultureInfo.CurrentCulture,
                                             MyResource.Resource.OPT_DATEI, m_ID_Projekt);

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = MyResource.Resource.OPT_CSV_TITEL;
            dlg.Filter = "CSV (*.csv)|*.csv|" + MyResource.Resource.OPT_CSV_TITEL + " (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
            dlg.FileName = vorschlag;

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                RasterSchreiben(dlg.FileName);
                MessageBox.Show(this,
                    string.Format(MyResource.Resource.OPT_CSV_GESCHRIEBEN, dlg.FileName),
                    MyResource.Resource.OPT_CSV_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    string.Format(MyResource.Resource.OPT_CSV_FEHLER, ex.Message),
                    MyResource.Resource.OPT_CSV_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RasterSchreiben(string dateiname)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            StringBuilder text = new StringBuilder();

            string[] kopf =
            {
                MyResource.Resource.OPT_CSV_PHASE,
                MyResource.Resource.OPT_KZ_KAPAZITAET + " [kWh]",
                MyResource.Resource.OPT_KZ_CRATE + " [1/h]",
                MyResource.Resource.OPT_KZ_LEISTUNG + " [kW]",
                MyResource.Resource.OPT_KZ_ZIELFUNKTION + " [€/a]",
                MyResource.Resource.OPT_KZ_UEBERSCHUSS + " [€/a]",
                MyResource.Resource.OPT_KZ_ERTRAG1 + " [€/a]",
                MyResource.Resource.OPT_KZ_ERTRAGAEQ + " [€/a]",
                MyResource.Resource.OPT_KZ_INVEST + " [€]",
                MyResource.Resource.OPT_KZ_ANNUITAET + " [€/a]",
                MyResource.Resource.OPT_KZ_NPV + " [€]",
                MyResource.Resource.OPT_KZ_AMORT_STAT + " [a]",
                MyResource.Resource.OPT_KZ_AMORT_DYN + " [a]",
                MyResource.Resource.OPT_KZ_ZYKLEN + " [1/a]",
                MyResource.Resource.OPT_KZ_ZYKLEN_N,
                MyResource.Resource.OPT_KZ_KVER + " [€/a]",
                MyResource.Resource.OPT_KZ_EIGENVERBRAUCH + " [%]",
                MyResource.Resource.OPT_KZ_AUTARKIE + " [%]",
                MyResource.Resource.OPT_KZ_LADEENERGIE + " [kWh/a]",
                MyResource.Resource.OPT_KZ_ENTLADEENERGIE + " [kWh/a]",
                MyResource.Resource.OPT_KZ_VERLUSTE + " [kWh/a]"
            };
            text.AppendLine(string.Join(";", kopf));

            RasterZeilenSchreiben(text, m_Ergebnis.Grobraster, MyResource.Resource.OPT_CSV_PHASE_GROB, k);
            if (m_Ergebnis.Feinraster != null)
                RasterZeilenSchreiben(text, m_Ergebnis.Feinraster, MyResource.Resource.OPT_CSV_PHASE_FEIN, k);

            File.WriteAllText(dateiname, text.ToString(), new UTF8Encoding(true));
        }

        private void RasterZeilenSchreiben(StringBuilder text, OptimiererRaster raster,
                                           string phase, CultureInfo k)
        {
            for (int i = 0; i < raster.Zeilen; i++)
                for (int s = 0; s < raster.Spalten; s++)
                {
                    OptimiererPunkt p = raster.Punkte[i][s];
                    string[] felder =
                    {
                        phase,
                        p.CNomKwh.ToString("0.###", k),
                        p.CRate.ToString("0.###", k),
                        p.PKw.ToString("0.###", k),
                        p.ZielfunktionEur.ToString("0.###", k),
                        p.JahresueberschussEur.ToString("0.###", k),
                        p.ErtragReferenzjahrEur.ToString("0.###", k),
                        p.ErtragAequivalentEur.ToString("0.###", k),
                        p.InvestitionEur.ToString("0.###", k),
                        p.AnnuitaetEur.ToString("0.###", k),
                        p.KapitalwertEur.ToString("0.###", k),
                        AmortisationText(p.StatischeAmortisation),
                        AmortisationText(p.DynamischeAmortisation),
                        p.AequivalenteVollzyklen.ToString("0.###", k),
                        p.ZyklenNutzungsdauer.ToString("0.###", k),
                        p.VerschleisskostenEurProA.ToString("0.###", k),
                        (p.EigenverbrauchsquoteMitSpeicher * 100.0).ToString("0.###", k),
                        (p.AutarkiegradMitSpeicher * 100.0).ToString("0.###", k),
                        p.LadeenergieKwh.ToString("0.###", k),
                        p.EntladeenergieKwh.ToString("0.###", k),
                        p.SpeicherverlusteKwh.ToString("0.###", k)
                    };
                    text.AppendLine(string.Join(";", felder));
                }
        }
    }
}
