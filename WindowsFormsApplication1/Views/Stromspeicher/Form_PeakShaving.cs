using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SpeicherEngine;
// "Cursor" ist auch im Charting-Namensraum ein Typ; ohne diesen Alias greift
// Cursor.Current daneben. Der gleichnamige Typ WirtschaftlichkeitErgebnis des
// Hauptprojekts laesst sich so NICHT ueberdecken (Typen des umgebenden
// Namensraums gehen einem Alias der Uebersetzungseinheit vor) - er wird an der
// Verwendungsstelle voll qualifiziert.
using Cursor = System.Windows.Forms.Cursor;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eigener Einstieg der Lastspitzenkappung (Peak-Shaving), Arbeitspaket AP7 -
    /// Fachkonzept 6.4 "separate Funktionalitaet", Umsetzungskonzept 2.2 Aufrufweg 2.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ohne PV/BHKW-Kette lauffaehig.</b> Eingang ist allein ein Lastgang: eine
    /// Ganglinie aus Projekt oder Stammkatalog oder eine ad hoc importierte Datei.
    /// Die Speicherparameter stehen direkt an der Maske. Ein geoeffnetes Projekt ist
    /// <b>nicht</b> noetig - ohne Projekt bleiben Stammganglinien und Direktimport.
    /// </para>
    /// <para>
    /// <b>Wiederverwendung der AP5-Importkette.</b> Der Direktimport laeuft ueber
    /// dieselbe Folge wie <c>Form_Stromganglinie_Admin</c>:
    /// <c>GanglinienDatei.Erkenne</c> -&gt; <see cref="Form_GanglinieImportOptionen"/>
    /// -&gt; <c>GanglinienDatei.Lies</c> -&gt; <c>GanglinienPruefung.Pruefe</c> -&gt;
    /// <see cref="Form_GanglinieProtokoll"/>. Der einzige Unterschied ist der letzte
    /// Schritt: <b>hier wird nichts abgelegt</b>, die Reihe bleibt im Speicher.
    /// </para>
    /// <para>
    /// <b>Gesetzte Defaults dieser Stufe.</b> (1) Bezugsgroesse der Lastspitze ist das
    /// <b>Jahresmaximum</b> der Viertelstundenleistung; die Monatsauswertung steht als
    /// Option auf einer eigenen Seite (Fachkonzept offener Punkt 4). (2) Ergebnisse
    /// werden <b>nicht</b> in der Datenbank abgelegt (offener Punkt 10 ist nicht
    /// entschieden); Exportweg ist CSV. Beides steht als Hinweis im Formular.
    /// </para>
    /// <para>
    /// Der Dialog ist vollstaendig im Quelltext aufgebaut - keine
    /// <c>.Designer.cs</c>, keine <c>.resx</c> (Projektregel: Designer- und
    /// resx-Dateien nicht von Hand editieren). Alle Beschriftungen kommen aus
    /// <c>MyResource</c> (<c>PEAK_*</c>) und sind damit zweisprachig; Zahlen werden
    /// nach dem Hausmuster mit <c>Program.ZahlParsen</c> /
    /// <c>Program.ZahlPruefen</c> gelesen und mit <c>Program.ZahlFaerben</c>
    /// gefaerbt.
    /// </para>
    /// </remarks>
    public class Form_PeakShaving : Form
    {
        /// <summary>Serienschluessel des Charts - Steuerwerte, keine Anzeigetexte.</summary>
        private const string SerieAlt = "PS_ALT";
        private const string SerieNeu = "PS_NEU";
        private const string SerieSoC = "PS_SOC";

        private readonly int m_ID_Projekt;

        private List<GanglinienEintrag> m_Ganglinien = new List<GanglinienEintrag>();
        private GanglinienEintrag m_Import;
        private double[] m_Lastgang;
        private PeakShavingErgebnis m_Ergebnis;
        private ChartManager m_ChartManager;
        private bool m_bAufbau = true;

        private RadioButton rad_Ganglinie, rad_Datei;
        private ComboBox cbo_Ganglinie;
        private Button btn_Datei, btn_Rechnen, btn_Minimal, btn_Csv, btn_Schliessen;
        private Label lbl_Reihe, lbl_Hinweis, lbl_Herkunft;
        private TextBox tb_P, tb_Kapazitaet, tb_SoCMin, tb_SoCMax, tb_StartSoC, tb_Eta;
        private TextBox tb_Ziel, tb_Lp, tb_Bezugspreis;
        private TextBox tb_CCap, tb_CPow, tb_IFix, tb_Zins, tb_Nutzungsdauer;
        private CheckBox chk_Adaptiv, chk_Kompatibel, chk_SoC;
        private Label lbl_Ziel;
        private TabControl tab_Ergebnis;
        private TabPage tabKennzahlen, tabChart, tabMonate;
        private ListView list_Kennzahlen, list_Monate;
        private Chart chart_Lastgang;

        /// <summary>
        /// Baut die Maske. <paramref name="idProjekt"/> darf 0 sein - dann entfaellt
        /// die Vorbelegung aus Geraet und Variante und es stehen nur
        /// Stammganglinien und der Direktimport zur Verfuegung.
        /// </summary>
        public Form_PeakShaving(int idProjekt)
        {
            m_ID_Projekt = idProjekt;

            AufbauSteuerelemente();
            ListenFuellen();
            VorbelegungSetzen();
            m_bAufbau = false;
            QuelleGeaendert(null, EventArgs.Empty);

            // Notebook-Schutz: Fenster in die Arbeitsflaeche des Bildschirms einpassen und
            // den Inhalt per Bildlauf erreichbar halten (Allgemein\FensterEinpassung.cs).
            // Auf ausreichend grossen Schirmen wirkungslos.
            FensterEinpassung.Einhaengen(this);
        }

        // ==================================================================
        // Aufbau
        // ==================================================================

        private void AufbauSteuerelemente()
        {
            Text = MyResource.Resource.PEAK_TITEL;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1060, 830);
            MinimumSize = new Size(900, 700);

            GroupBox grpQuelle = new GroupBox();
            grpQuelle.SetBounds(12, 10, 1036, 96);
            grpQuelle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpQuelle.Text = MyResource.Resource.PEAK_GRP_QUELLE;
            Controls.Add(grpQuelle);

            rad_Ganglinie = new RadioButton();
            rad_Ganglinie.SetBounds(14, 24, 210, 22);
            rad_Ganglinie.Text = MyResource.Resource.PEAK_OPT_GANGLINIE;
            rad_Ganglinie.Checked = true;
            rad_Ganglinie.CheckedChanged += QuelleGeaendert;
            grpQuelle.Controls.Add(rad_Ganglinie);

            cbo_Ganglinie = new ComboBox();
            cbo_Ganglinie.SetBounds(230, 23, 480, 22);
            cbo_Ganglinie.DropDownStyle = ComboBoxStyle.DropDownList;
            cbo_Ganglinie.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbo_Ganglinie.SelectedIndexChanged += QuelleGeaendert;
            grpQuelle.Controls.Add(cbo_Ganglinie);

            rad_Datei = new RadioButton();
            rad_Datei.SetBounds(14, 54, 210, 22);
            rad_Datei.Text = MyResource.Resource.PEAK_OPT_DATEI;
            rad_Datei.CheckedChanged += QuelleGeaendert;
            grpQuelle.Controls.Add(rad_Datei);

            btn_Datei = new Button();
            btn_Datei.SetBounds(230, 52, 160, 26);
            btn_Datei.Text = MyResource.Resource.PEAK_BTN_DATEI;
            btn_Datei.Click += Datei_Click;
            grpQuelle.Controls.Add(btn_Datei);

            lbl_Reihe = new Label();
            lbl_Reihe.SetBounds(400, 57, 620, 18);
            lbl_Reihe.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Reihe.AutoEllipsis = true;
            grpQuelle.Controls.Add(lbl_Reihe);

            // ---------------------------------------------------------- Parameter
            GroupBox grpParameter = new GroupBox();
            grpParameter.SetBounds(12, 112, 1036, 168);
            grpParameter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpParameter.Text = MyResource.Resource.PEAK_GRP_PARAMETER;
            Controls.Add(grpParameter);

            const int s1 = 14, e1 = 210, s2 = 350, e2 = 546, s3 = 686, e3 = 882;
            int y = 24;

            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_P, s1, y);
            tb_P = Zahlfeld(grpParameter, e1, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_KAPAZITAET, s2, y);
            tb_Kapazitaet = Zahlfeld(grpParameter, e2, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_ETA, s3, y);
            tb_Eta = Zahlfeld(grpParameter, e3, y);

            y += 28;
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_SOCMIN, s1, y);
            tb_SoCMin = Zahlfeld(grpParameter, e1, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_SOCMAX, s2, y);
            tb_SoCMax = Zahlfeld(grpParameter, e2, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_STARTSOC, s3, y);
            tb_StartSoC = Zahlfeld(grpParameter, e3, y);

            y += 28;
            lbl_Ziel = Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_ZIEL, s1, y);
            tb_Ziel = Zahlfeld(grpParameter, e1, y);
            chk_Adaptiv = new CheckBox();
            chk_Adaptiv.SetBounds(s2, y, 180, 22);
            chk_Adaptiv.Text = MyResource.Resource.PEAK_CHK_ADAPTIV;
            chk_Adaptiv.CheckedChanged += AdaptivGeaendert;
            grpParameter.Controls.Add(chk_Adaptiv);

            btn_Minimal = new Button();
            btn_Minimal.SetBounds(s3 - 140, y - 2, 336, 26);
            btn_Minimal.Text = MyResource.Resource.PEAK_BTN_MINIMAL;
            btn_Minimal.Click += Minimal_Click;
            grpParameter.Controls.Add(btn_Minimal);

            y += 28;
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_LP, s1, y);
            tb_Lp = Zahlfeld(grpParameter, e1, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_BEZUGSPREIS, s2, y);
            tb_Bezugspreis = Zahlfeld(grpParameter, e2, y);
            chk_Kompatibel = new CheckBox();
            chk_Kompatibel.SetBounds(s3, y, 336, 22);
            chk_Kompatibel.Text = MyResource.Resource.PEAK_CHK_KOMPAT;
            grpParameter.Controls.Add(chk_Kompatibel);

            y += 28;
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_CCAP, s1, y);
            tb_CCap = Zahlfeld(grpParameter, e1, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_CPOW, s2, y);
            tb_CPow = Zahlfeld(grpParameter, e2, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_IFIX, s3, y);
            tb_IFix = Zahlfeld(grpParameter, e3, y);

            y += 28;
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_ZINS, s1, y);
            tb_Zins = Zahlfeld(grpParameter, e1, y);
            Beschriftung(grpParameter, MyResource.Resource.PEAK_LBL_NUTZUNGSDAUER, s2, y);
            tb_Nutzungsdauer = Zahlfeld(grpParameter, e2, y);
            lbl_Herkunft = new Label();
            lbl_Herkunft.SetBounds(s3, y + 4, 336, 18);
            lbl_Herkunft.AutoEllipsis = true;
            lbl_Herkunft.ForeColor = SystemColors.GrayText;
            grpParameter.Controls.Add(lbl_Herkunft);

            // ---------------------------------------------------------- Aktionen
            btn_Rechnen = new Button();
            btn_Rechnen.SetBounds(12, 288, 190, 30);
            btn_Rechnen.Text = MyResource.Resource.PEAK_BTN_RECHNEN;
            btn_Rechnen.Click += Rechnen_Click;
            Controls.Add(btn_Rechnen);

            chk_SoC = new CheckBox();
            chk_SoC.SetBounds(216, 293, 260, 22);
            chk_SoC.Text = MyResource.Resource.PEAK_CHK_SOC;
            chk_SoC.Checked = true;
            chk_SoC.CheckedChanged += SoCAnzeigeGeaendert;
            Controls.Add(chk_SoC);

            btn_Csv = new Button();
            btn_Csv.SetBounds(858, 288, 190, 30);
            btn_Csv.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_Csv.Text = MyResource.Resource.PEAK_BTN_CSV;
            btn_Csv.Click += Csv_Click;
            Controls.Add(btn_Csv);

            // ---------------------------------------------------------- Ergebnis
            tab_Ergebnis = new TabControl();
            tab_Ergebnis.SetBounds(12, 326, 1036, 430);
            tab_Ergebnis.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(tab_Ergebnis);

            tabKennzahlen = new TabPage(MyResource.Resource.PEAK_TAB_KENNZAHLEN);
            tab_Ergebnis.TabPages.Add(tabKennzahlen);
            list_Kennzahlen = Ergebnisliste();
            list_Kennzahlen.Columns.Add(MyResource.Resource.PEAK_SP_GROESSE, 420);
            list_Kennzahlen.Columns.Add(MyResource.Resource.PEAK_SP_WERT, 170, HorizontalAlignment.Right);
            list_Kennzahlen.Columns.Add(MyResource.Resource.PEAK_SP_EINHEIT, 150);
            tabKennzahlen.Controls.Add(list_Kennzahlen);

            tabChart = new TabPage(MyResource.Resource.PEAK_TAB_CHART);
            tab_Ergebnis.TabPages.Add(tabChart);
            chart_Lastgang = new Chart();
            chart_Lastgang.Name = "chart_PeakShaving";
            // Ein programmatisch erzeugtes Chart hat keine ChartArea - ChartManager.Init
            // steigt ohne sie wortlos aus.
            chart_Lastgang.ChartAreas.Add(new ChartArea("ChartArea_PeakShaving"));
            chart_Lastgang.BackColor = Color.WhiteSmoke;
            chart_Lastgang.BorderlineColor = Color.Transparent;
            chart_Lastgang.SetBounds(6, 6, 1016, 386);
            chart_Lastgang.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabChart.Controls.Add(chart_Lastgang);

            tabMonate = new TabPage(MyResource.Resource.PEAK_TAB_MONATE);
            tab_Ergebnis.TabPages.Add(tabMonate);
            list_Monate = Ergebnisliste();
            list_Monate.Columns.Add(MyResource.Resource.PEAK_SP_MONAT, 220);
            list_Monate.Columns.Add(MyResource.Resource.PEAK_SP_ALT, 170, HorizontalAlignment.Right);
            list_Monate.Columns.Add(MyResource.Resource.PEAK_SP_NEU, 170, HorizontalAlignment.Right);
            list_Monate.Columns.Add(MyResource.Resource.PEAK_SP_KAPPUNG, 170, HorizontalAlignment.Right);
            tabMonate.Controls.Add(list_Monate);

            // ---------------------------------------------------------- Fusszeile
            lbl_Hinweis = new Label();
            lbl_Hinweis.SetBounds(12, 764, 900, 52);
            lbl_Hinweis.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Hinweis.Text = MyResource.Resource.PEAK_HINWEIS;
            lbl_Hinweis.ForeColor = SystemColors.GrayText;
            Controls.Add(lbl_Hinweis);

            btn_Schliessen = new Button();
            btn_Schliessen.SetBounds(954, 790, 94, 28);
            btn_Schliessen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Schliessen.Text = MyResource.Resource.PEAK_BTN_SCHLIESSEN;
            btn_Schliessen.DialogResult = DialogResult.Cancel;
            Controls.Add(btn_Schliessen);

            CancelButton = btn_Schliessen;
        }

        private static Label Beschriftung(Control eltern, string text, int x, int y)
        {
            Label l = new Label();
            l.SetBounds(x, y + 4, 194, 18);
            l.Text = text;
            eltern.Controls.Add(l);
            return l;
        }

        /// <summary>
        /// Zahlfeld nach Hausmuster: <c>Program.ZahlFaerben</c> am
        /// <c>TextChanged</c>, gemeldet wird erst beim Knopf.
        /// </summary>
        private static TextBox Zahlfeld(Control eltern, int x, int y)
        {
            TextBox tb = new TextBox();
            tb.SetBounds(x, y, 120, 22);
            tb.TextAlign = HorizontalAlignment.Right;
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            eltern.Controls.Add(tb);
            return tb;
        }

        private static ListView Ergebnisliste()
        {
            ListView lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.MultiSelect = false;
            lv.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            return lv;
        }

        // ==================================================================
        // Listen und Vorbelegung
        // ==================================================================

        private void ListenFuellen()
        {
            m_Ganglinien = PeakShavingCtrl.LeseGanglinien(m_ID_Projekt);

            cbo_Ganglinie.Items.Clear();
            foreach (GanglinienEintrag e in m_Ganglinien)
            {
                string zusatz = e.AusStamm
                    ? MyResource.Resource.PEAK_QUELLE_STAMM
                    : MyResource.Resource.PEAK_QUELLE_PROJEKT;
                cbo_Ganglinie.Items.Add(string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.PEAK_GANGLINIE_EINTRAG, e.Bezeichner, zusatz));
            }
            if (cbo_Ganglinie.Items.Count > 0) cbo_Ganglinie.SelectedIndex = 0;
            else rad_Datei.Checked = true;
        }

        private void VorbelegungSetzen()
        {
            PeakShavingVorbelegung v = PeakShavingCtrl.LeseVorbelegung(m_ID_Projekt);

            Zahl(tb_P, v.PKw);
            Zahl(tb_Kapazitaet, v.KapazitaetKwh);
            Zahl(tb_SoCMin, v.SoCMinProzent);
            Zahl(tb_SoCMax, v.SoCMaxProzent);
            Zahl(tb_StartSoC, v.StartSoCProzent);
            Zahl(tb_Eta, v.WirkungsgradRt);
            Zahl(tb_Lp, v.LeistungspreisEurProKwA);
            Zahl(tb_Bezugspreis, v.BezugspreisMittelCtKwh);
            Zahl(tb_CCap, v.CCapEurProKwh);
            Zahl(tb_CPow, v.CPowEurProKw);
            Zahl(tb_IFix, v.IFixEur);
            Zahl(tb_Zins, v.KapitalzinsProzent);
            Zahl(tb_Nutzungsdauer, v.NutzungsdauerA);

            chk_Kompatibel.Checked = v.Kompatibilitaetsmodus;
            chk_Adaptiv.Checked = true;
            AdaptivGeaendert(null, EventArgs.Empty);

            lbl_Herkunft.Text = v.AusProjekt
                ? string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.PEAK_HERKUNFT_PROJEKT, v.Bezeichner)
                : MyResource.Resource.PEAK_HERKUNFT_VORGABE;
        }

        private static void Zahl(TextBox feld, double wert)
        {
            feld.Text = wert.ToString("0.###", CultureInfo.CurrentCulture);
        }

        // ==================================================================
        // Quelle
        // ==================================================================

        private void QuelleGeaendert(object sender, EventArgs e)
        {
            if (m_bAufbau) return;

            cbo_Ganglinie.Enabled = rad_Ganglinie.Checked;
            btn_Datei.Enabled = rad_Datei.Checked;

            m_Lastgang = null;
            m_Ergebnis = null;
            ErgebnisLeeren();

            if (rad_Datei.Checked)
            {
                m_Lastgang = m_Import != null ? m_Import.ImportWerte : null;
                ReiheAnzeigen(m_Import != null ? m_Import.Bezeichner : null);
                return;
            }

            int i = cbo_Ganglinie.SelectedIndex;
            if (i < 0 || i >= m_Ganglinien.Count) { ReiheAnzeigen(null); return; }

            GanglinienEintrag eintrag = m_Ganglinien[i];
            Cursor.Current = Cursors.WaitCursor;
            try { m_Lastgang = PeakShavingCtrl.LeseWerte(eintrag); }
            finally { Cursor.Current = Cursors.Default; }

            ReiheAnzeigen(eintrag.Bezeichner);
        }

        private void ReiheAnzeigen(string bezeichner)
        {
            if (m_Lastgang == null || m_Lastgang.Length == 0)
            {
                lbl_Reihe.Text = MyResource.Resource.PEAK_LBL_KEINE_REIHE;
                lbl_Reihe.ForeColor = SystemColors.GrayText;
                return;
            }

            double max = double.NegativeInfinity;
            for (int i = 0; i < m_Lastgang.Length; i++) if (m_Lastgang[i] > max) max = m_Lastgang[i];

            lbl_Reihe.ForeColor = SystemColors.ControlText;
            lbl_Reihe.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.PEAK_LBL_REIHE,
                bezeichner ?? "",
                m_Lastgang.Length.ToString("N0", CultureInfo.CurrentCulture),
                max.ToString("0.#", CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Direktimport ueber die AP5-Kette. Der einzige Unterschied zu
        /// <c>Form_Stromganglinie_Admin.btn_Einlesen_Click</c> ist der Schluss: die
        /// geprueften Werte bleiben im Speicher, nichts wird abgelegt.
        /// </summary>
        private void Datei_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlgDatei = new OpenFileDialog();
            dlgDatei.Filter = MyResource.Resource.IMPORT_DATEIFILTER;
            dlgDatei.FilterIndex = 1;
            dlgDatei.RestoreDirectory = true;
            if (dlgDatei.ShowDialog(this) != DialogResult.OK) return;

            string szPfad = dlgDatei.FileName;
            if (string.IsNullOrEmpty(szPfad) || !File.Exists(szPfad)) return;

            // 1) Format erkennen
            GanglinienVorschau vorschau;
            Cursor.Current = Cursors.WaitCursor;
            try { vorschau = GanglinienDatei.Erkenne(szPfad); }
            finally { Cursor.Current = Cursors.Default; }

            if (vorschau == null || !vorschau.Lesbar)
            {
                Form_GanglinieProtokoll.Zeigen(this, vorschau != null ? vorschau.Meldungen : null, false, true);
                return;
            }

            // 2) Optionen bestaetigen oder uebersteuern
            GanglinienImportOptionen optionen;
            using (Form_GanglinieImportOptionen dlg = new Form_GanglinieImportOptionen(szPfad, vorschau))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                optionen = dlg.Optionen;
            }

            // 3) lesen und 4) pruefen
            GanglinienRohdaten roh;
            GanglinienPruefErgebnis ergebnis = null;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                roh = GanglinienDatei.Lies(szPfad, optionen);
                if (roh.Erfolgreich)
                {
                    ergebnis = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
                    {
                        Rohwerte = roh.Werte,
                        Zeitstempel = roh.Zeitstempel,
                        Einheit = optionen.Einheit,
                        DeklariertesRaster = optionen.Raster,
                        Konvention = optionen.Konvention
                    });
                }
            }
            finally { Cursor.Current = Cursors.Default; }

            // 5) Protokoll zusammenfuehren und anzeigen
            List<PruefMeldung> protokoll = new List<PruefMeldung>(roh.Meldungen);
            if (ergebnis != null) protokoll.AddRange(ergebnis.Protokoll);

            bool moeglich = roh.Erfolgreich && ergebnis != null && ergebnis.Erfolgreich;
            bool bestaetigen = !moeglich || ergebnis == null || ergebnis.BestaetigungNoetig;

            if (!Form_GanglinieProtokoll.Zeigen(this, protokoll, moeglich, bestaetigen)) return;
            if (!moeglich) return;

            // 6) KEINE Ablage - die Reihe bleibt ad hoc im Speicher.
            m_Import = new GanglinienEintrag
            {
                Bezeichner = Path.GetFileNameWithoutExtension(szPfad),
                Zeitinterval = ergebnis.Zeitinterval,
                ImportWerte = ergebnis.Werte
            };

            m_Lastgang = m_Import.ImportWerte;
            m_Ergebnis = null;
            ErgebnisLeeren();
            ReiheAnzeigen(m_Import.Bezeichner);
        }

        // ==================================================================
        // Parameter
        // ==================================================================

        private void AdaptivGeaendert(object sender, EventArgs e)
        {
            bool fest = !chk_Adaptiv.Checked;
            tb_Ziel.Enabled = fest;
            lbl_Ziel.Enabled = fest;
            btn_Minimal.Enabled = true;
        }

        /// <summary>
        /// Liest alle Eingabefelder. Meldet den ersten Fehler nach dem Hausmuster
        /// (<c>Program.ZahlPruefen</c>: Meldung, Fokus, Feld bleibt offen) und liefert
        /// dann <c>false</c>.
        /// </summary>
        /// <param name="zielNoetig">
        /// false fuer die Schwellensuche: sie bestimmt P_ziel gerade erst, ein leeres
        /// Feld darf sie deshalb nicht blockieren.
        /// </param>
        private bool ParameterLesen(out SpeicherParameter p, out PeakShavingParameter ps, bool zielNoetig = true)
        {
            p = null;
            ps = null;

            double pKw, kapazitaet, socMin, socMax, startSoC, eta;
            double ziel, lp, preis, cCap, cPow, iFix, zins, nutzungsdauer;

            if (!Program.ZahlPruefen(tb_P, MyResource.Resource.PEAK_LBL_P, out pKw)) return false;
            if (!Program.ZahlPruefen(tb_Kapazitaet, MyResource.Resource.PEAK_LBL_KAPAZITAET, out kapazitaet)) return false;
            if (!Program.ZahlPruefen(tb_SoCMin, MyResource.Resource.PEAK_LBL_SOCMIN, out socMin)) return false;
            if (!Program.ZahlPruefen(tb_SoCMax, MyResource.Resource.PEAK_LBL_SOCMAX, out socMax)) return false;
            if (!Program.ZahlPruefen(tb_StartSoC, MyResource.Resource.PEAK_LBL_STARTSOC, out startSoC)) return false;
            if (!Program.ZahlPruefen(tb_Eta, MyResource.Resource.PEAK_LBL_ETA, out eta)) return false;
            if (!Program.ZahlPruefen(tb_Lp, MyResource.Resource.PEAK_LBL_LP, out lp, true)) return false;
            if (!Program.ZahlPruefen(tb_Bezugspreis, MyResource.Resource.PEAK_LBL_BEZUGSPREIS, out preis, true)) return false;
            if (!Program.ZahlPruefen(tb_CCap, MyResource.Resource.PEAK_LBL_CCAP, out cCap, true)) return false;
            if (!Program.ZahlPruefen(tb_CPow, MyResource.Resource.PEAK_LBL_CPOW, out cPow, true)) return false;
            if (!Program.ZahlPruefen(tb_IFix, MyResource.Resource.PEAK_LBL_IFIX, out iFix, true)) return false;
            if (!Program.ZahlPruefen(tb_Zins, MyResource.Resource.PEAK_LBL_ZINS, out zins, true)) return false;
            if (!Program.ZahlPruefen(tb_Nutzungsdauer, MyResource.Resource.PEAK_LBL_NUTZUNGSDAUER, out nutzungsdauer)) return false;

            ziel = 0.0;
            if (zielNoetig && !chk_Adaptiv.Checked &&
                !Program.ZahlPruefen(tb_Ziel, MyResource.Resource.PEAK_LBL_ZIEL, out ziel)) return false;

            if (kapazitaet <= 0.0) { Melden(MyResource.Resource.PEAK_MSG_KAPAZITAET, tb_Kapazitaet); return false; }
            if (socMax <= socMin) { Melden(MyResource.Resource.PEAK_MSG_BAND, tb_SoCMax); return false; }
            if (eta <= 0.0 || eta > 1.0) { Melden(MyResource.Resource.PEAK_MSG_ETA, tb_Eta); return false; }
            if (nutzungsdauer <= 0.0) { Melden(MyResource.Resource.PEAK_MSG_NUTZUNGSDAUER, tb_Nutzungsdauer); return false; }

            // Das SoC-Band steht in Prozent an der Maske und in kWh in der Engine.
            p = new SpeicherParameter
            {
                CNomKwh = kapazitaet,
                PKw = pKw,
                SoCMinKwh = kapazitaet * socMin / 100.0,
                SoCMaxKwh = kapazitaet * socMax / 100.0,
                RoundTripWirkungsgrad = eta,
                StartSoCKwh = kapazitaet * startSoC / 100.0,
                DtH = 0.25,
                CCapEurProKwh = cCap,
                CPowEurProKw = cPow,
                IFixEur = iFix,
                Kapitalzins = zins / 100.0,
                NutzungsdauerA = nutzungsdauer,
                // Ohne Degradation: sie ist an dieser Maske bewusst kein Feld. Ihr
                // Einfluss auf eine Leistungspreisersparnis waere nur ueber einen
                // Lauf je Nutzungsjahr sauber abzubilden; ein still mitgefuehrter
                // Geraetewert wuerde das Ergebnis unsichtbar veraendern.
                DegradationProA = 0.0
            };

            ps = new PeakShavingParameter
            {
                PZielKw = ziel,
                Adaptiv = chk_Adaptiv.Checked,
                LeistungspreisEurProKwA = lp,
                BezugspreisMittelCtKwh = preis
            };
            return true;
        }

        private void Melden(string text, Control fokus)
        {
            MessageBox.Show(this, text, MyResource.Resource.PEAK_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
            if (fokus != null) fokus.Focus();
        }

        private SpeicherModus Modus =>
            chk_Kompatibel.Checked ? SpeicherModus.ExcelKompatibilitaet : SpeicherModus.Energetisch;

        // ==================================================================
        // Rechnen
        // ==================================================================

        private void Minimal_Click(object sender, EventArgs e)
        {
            if (!ReiheVorhanden()) return;

            SpeicherParameter p;
            PeakShavingParameter ps;
            if (!ParameterLesen(out p, out ps, false)) return;

            double minimal;
            Cursor.Current = Cursors.WaitCursor;
            try { minimal = PeakShaving.MinimaleSchwelleKw(m_Lastgang, p, Modus); }
            finally { Cursor.Current = Cursors.Default; }

            // Ergebnis in das Schwellenfeld uebernehmen und auf feste Vorgabe
            // umschalten - der adaptive Lauf wuerde es sonst wieder ueberschreiben.
            chk_Adaptiv.Checked = false;
            tb_Ziel.Text = minimal.ToString("0.##", CultureInfo.CurrentCulture);

            MessageBox.Show(this,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PEAK_MSG_MINIMAL,
                              minimal.ToString("0.##", CultureInfo.CurrentCulture)),
                MyResource.Resource.PEAK_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Rechnen_Click(object sender, EventArgs e)
        {
            if (!ReiheVorhanden()) return;

            SpeicherParameter p;
            PeakShavingParameter ps;
            if (!ParameterLesen(out p, out ps)) return;

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                m_Ergebnis = new PeakShaving(ps, Modus).BerechnePeakShaving(m_Lastgang, p);
            }
            catch (ArgumentException ex)
            {
                Cursor.Current = Cursors.Default;
                Melden(ex.Message, null);
                return;
            }
            finally { Cursor.Current = Cursors.Default; }

            KennzahlenAnzeigen();
            MonatsspitzenAnzeigen();
            ChartZeichnen();

            if (m_Ergebnis.SchwelleGerissen)
                MessageBox.Show(this,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PEAK_MSG_GERISSEN,
                                  m_Ergebnis.PNeuMaxKw.ToString("0.#", CultureInfo.CurrentCulture)),
                    MyResource.Resource.PEAK_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool ReiheVorhanden()
        {
            if (m_Lastgang != null && m_Lastgang.Length > 0) return true;
            MessageBox.Show(this, MyResource.Resource.PEAK_MSG_KEINE_REIHE,
                            MyResource.Resource.PEAK_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // ==================================================================
        // Ergebnisanzeige
        // ==================================================================

        private void ErgebnisLeeren()
        {
            if (list_Kennzahlen != null) list_Kennzahlen.Items.Clear();
            if (list_Monate != null) list_Monate.Items.Clear();
            if (chart_Lastgang != null && m_ChartManager != null)
            {
                m_ChartManager.HardReset();
                chart_Lastgang.Invalidate();
            }
        }

        private void KennzahlenAnzeigen()
        {
            PeakShavingErgebnis r = m_Ergebnis;
            list_Kennzahlen.BeginUpdate();
            try
            {
                list_Kennzahlen.Items.Clear();

                Kennzahl(MyResource.Resource.PEAK_KZ_SPITZE_ALT, r.PAltMaxKw, "0.#", "kW");
                Kennzahl(MyResource.Resource.PEAK_KZ_SPITZE_NEU, r.PNeuMaxKw, "0.#", "kW");
                Kennzahl(MyResource.Resource.PEAK_KZ_KAPPUNG, r.KappungKw, "0.#", "kW");
                Kennzahl(MyResource.Resource.PEAK_KZ_SCHWELLE, r.ErreichteSchwelleKw, "0.#", "kW");
                Textzeile(MyResource.Resource.PEAK_KZ_GERISSEN,
                     r.SchwelleGerissen ? MyResource.Resource.PEAK_JA : MyResource.Resource.PEAK_NEIN, "");

                Trenner();
                Kennzahl(MyResource.Resource.PEAK_KZ_LADEENERGIE, r.LadeenergieKwh, "N0", "kWh/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_ENTLADEENERGIE, r.EntladeenergieKwh, "N0", "kWh/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_VERLUSTE, r.SpeicherverlusteKwh, "N0", "kWh/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_VOLLZYKLEN, r.Kennzahlen.AequivalenteVollzyklen, "0.0", "1/a");

                Trenner();
                Kennzahl(MyResource.Resource.PEAK_KZ_ERSPARNIS, r.LeistungspreisersparnisEur, "N2", "EUR/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_VERLUSTKOSTEN, r.VerlustkostenEur, "N2", "EUR/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_ERTRAG, r.ErtragPsEur, "N2", "EUR/a");

                Trenner();
                SpeicherEngine.WirtschaftlichkeitErgebnis w = r.Wirtschaftlichkeit;
                Kennzahl(MyResource.Resource.PEAK_KZ_INVEST, w.InvestitionEur, "N2", "EUR");
                Kennzahl(MyResource.Resource.PEAK_KZ_ANNUITAET, w.AnnuitaetEur, "N2", "EUR/a");
                Kennzahl(MyResource.Resource.PEAK_KZ_UEBERSCHUSS, w.JahresueberschussEur, "N2", "EUR/a");
                AmortisationZeile(MyResource.Resource.PEAK_KZ_AMORT_STAT, w.StatischeAmortisation);
                AmortisationZeile(MyResource.Resource.PEAK_KZ_AMORT_DYN, w.DynamischeAmortisation);
                Kennzahl(MyResource.Resource.PEAK_KZ_NPV, w.KapitalwertEur, "N2", "EUR");
            }
            finally { list_Kennzahlen.EndUpdate(); }
        }

        private void Kennzahl(string name, double wert, string format, string einheit)
        {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(wert.ToString(format, CultureInfo.CurrentCulture));
            item.SubItems.Add(einheit);
            // Hausmuster ZahlFaerben: negative Betraege rot, damit der Blick sie findet.
            if (wert < 0.0) item.ForeColor = Color.FromArgb(176, 0, 0);
            list_Kennzahlen.Items.Add(item);
        }

        private void Textzeile(string name, string wert, string einheit)
        {
            ListViewItem item = new ListViewItem(name);
            item.SubItems.Add(wert);
            item.SubItems.Add(einheit);
            list_Kennzahlen.Items.Add(item);
        }

        private void AmortisationZeile(string name, SpeicherEngine.Amortisation a)
        {
            string wert = a.IstAmortisierbar
                ? a.Jahre.ToString("0.0", CultureInfo.CurrentCulture)
                : (a.Status == SpeicherEngine.AmortisationStatus.UeberNutzungsdauer
                    ? MyResource.Resource.PEAK_AMORT_UEBER
                    : MyResource.Resource.PEAK_AMORT_NIE);
            Textzeile(name, wert, a.IstAmortisierbar ? "a" : "");
        }

        private void Trenner()
        {
            list_Kennzahlen.Items.Add(new ListViewItem(""));
        }

        private void MonatsspitzenAnzeigen()
        {
            list_Monate.BeginUpdate();
            try
            {
                list_Monate.Items.Clear();
                CultureInfo kultur = CultureInfo.CurrentUICulture;

                foreach (Monatsspitze m in m_Ergebnis.Monatsspitzen)
                {
                    string name = m.Monat >= 1 && m.Monat <= 12
                        ? kultur.DateTimeFormat.GetMonthName(m.Monat)
                        : MyResource.Resource.PEAK_MONAT_GESAMT;

                    ListViewItem item = new ListViewItem(name);
                    item.SubItems.Add(m.PAltMaxKw.ToString("0.#", CultureInfo.CurrentCulture));
                    item.SubItems.Add(m.PNeuMaxKw.ToString("0.#", CultureInfo.CurrentCulture));
                    item.SubItems.Add(m.KappungKw.ToString("0.#", CultureInfo.CurrentCulture));
                    list_Monate.Items.Add(item);
                }
            }
            finally { list_Monate.EndUpdate(); }
        }

        private void SoCAnzeigeGeaendert(object sender, EventArgs e)
        {
            if (m_bAufbau || m_Ergebnis == null) return;
            ChartZeichnen();
        }

        /// <summary>
        /// Vorher/Nachher-Chart. <c>MaxXVALUE</c> UND <c>MitViertelStunde</c> sind
        /// beide noetig, sonst kappt <c>AddSeries</c> auf 8.760 Punkte. Der
        /// Ladezustand liegt auf der Sekundaerachse, weil kWh und kW nicht dieselbe
        /// Skala teilen.
        /// </summary>
        private void ChartZeichnen()
        {
            PeakShavingErgebnis r = m_Ergebnis;
            if (r == null) return;

            if (m_ChartManager == null) m_ChartManager = new ChartManager(chart_Lastgang);
            ChartManager cm = m_ChartManager;

            bool viertelstunden = r.Anzahl > RasterAdapter.StundenJahr;

            cm.YMaxValue = r.PAltMaxKw * 1.05;
            cm.YMinValue = 0;
            cm.XAxisAsNumber = false;
            cm.XAxisTitle = MyResource.Resource.PEAK_CHART_X;
            cm.YAxisTitle = MyResource.Resource.PEAK_CHART_Y;
            cm.toolTipUnit = "kW";
            cm.ChartTitle = MyResource.Resource.PEAK_CHART_TITEL;
            cm.MitLegende = true;
            cm.MitChartBorder = true;
            cm.AreaLine = false;
            cm.MaxXVALUE = viertelstunden ? RasterAdapter.ViertelstundenJahr : RasterAdapter.StundenJahr;
            cm.MitViertelStunde = viertelstunden;

            cm.HardReset();
            cm.Init();

            Serie(cm, SerieAlt, MyResource.Resource.PEAK_SERIE_ALT,
                  Color.FromArgb(190, 90, 90), RasterAdapter.ZuFloat(r.PAltKw));
            Serie(cm, SerieNeu, MyResource.Resource.PEAK_SERIE_NEU,
                  Color.FromArgb(40, 110, 180), RasterAdapter.ZuFloat(r.PNeuKw));

            if (chk_SoC.Checked)
            {
                Serie(cm, SerieSoC, MyResource.Resource.PEAK_SERIE_SOC,
                      Color.FromArgb(120, 130, 140), RasterAdapter.ZuFloat(r.SoCKwh));

                ChartArea ca = chart_Lastgang.ChartAreas[0];
                chart_Lastgang.Series[SerieSoC].YAxisType = AxisType.Secondary;
                ca.AxisY2.Title = MyResource.Resource.PEAK_CHART_Y2;
                ca.AxisY2.Minimum = 0;
                ca.AxisY2.MajorGrid.Enabled = false;
                ca.AxisY2.Enabled = AxisEnabled.True;
            }

            chart_Lastgang.Visible = true;
            chart_Lastgang.Invalidate();
        }

        private static void Serie(ChartManager cm, string schluessel, string legende, Color farbe, float[] werte)
        {
            cm.AddSeries(schluessel, farbe, werte);
            cm._chart.Series[schluessel].LegendText = legende;
        }

        // ==================================================================
        // CSV-Export
        // ==================================================================

        private void Csv_Click(object sender, EventArgs e)
        {
            if (m_Ergebnis == null)
            {
                MessageBox.Show(this, MyResource.Resource.PEAK_MSG_KEIN_ERGEBNIS,
                                MyResource.Resource.PEAK_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PeakShavingErgebnis r = m_Ergebnis;
            List<CsvSpalte> spalten = new List<CsvSpalte>();
            spalten.Add(new CsvSpalte(MyResource.Resource.PEAK_CSV_PALT, RasterAdapter.ZuFloat(r.PAltKw)));
            spalten.Add(new CsvSpalte(MyResource.Resource.PEAK_CSV_PNEU, RasterAdapter.ZuFloat(r.PNeuKw)));
            spalten.Add(new CsvSpalte(MyResource.Resource.PEAK_CSV_SOC, RasterAdapter.ZuFloat(r.SoCKwh)));
            spalten.Add(new CsvSpalte(MyResource.Resource.PEAK_CSV_LADUNG, RasterAdapter.ZuFloat(r.LadungAcKwh)));
            spalten.Add(new CsvSpalte(MyResource.Resource.PEAK_CSV_ENTLADUNG, RasterAdapter.ZuFloat(r.EntladungAcKwh)));

            CsvExportClass.Export(MyResource.Resource.PEAK_DATEI, null, spalten,
                                  r.Anzahl > RasterAdapter.StundenJahr);
        }
    }
}
