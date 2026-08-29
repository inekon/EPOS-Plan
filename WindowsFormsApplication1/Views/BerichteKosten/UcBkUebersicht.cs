using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Seite „Übersicht" des Reiters „Berichte &amp; Kosten".
    ///
    /// Oben die Verwaltung der Vergleichsgruppe — Stammprojekt-Auswahl, Filter
    /// „nur Stammprojekte", Liste aus Stamm und Varianten mit Simulationsstand,
    /// Variante anlegen/löschen, Simulation starten. Die Logik stammt aus dem
    /// abgelösten Dialog „Projektvarianten" (<see cref="Form_Variantentest"/>) und
    /// arbeitet unverändert über <see cref="VariantenCtrl"/>; die Spalte
    /// „Simulationsstand" kommt wie in Bericht und Wirtschaftlichkeit aus
    /// <see cref="BerichtsDatenSammler.ErmittleStatus"/>.
    ///
    /// Darunter der Komponenten-Bereich: Auf der Stammzeile steht die GEGENÜBERSTELLUNG
    /// aller Versionen der Gruppe — Gewerk · Merkmal · Stamm · je Variante eine Spalte,
    /// in der Reihenfolge der oberen Liste, mit der Anzahl der verbauten Komponenten als
    /// Kopfzeile jedes Gewerks. Damit ist der Bestandsvergleich ohne Klickerei durch die
    /// Varianten lesbar. Auf einer Variantenzeile stehen unverändert nur deren
    /// Unterschiede zum Stamm samt Aktionsspalte. Beides speist sich aus der vorhandenen
    /// Diff-Welt (<see cref="ProjektDetails"/> + <see cref="AbweichungsErmittler"/>), die
    /// auch der Bericht verwendet — es gibt bewusst keine zweite Vergleichslogik: Auch
    /// die Anzahlzeile kommt aus <see cref="AbweichungsErmittler.Anzahl"/> und damit aus
    /// <c>Tab_Energieanlagen</c>, nicht aus dem rohen Zeilenbestand der Gerätetabellen.
    ///
    /// Die Aktionsspalte „Übernehmen" wirkt auf beiden Stufen der Unterschiedsanzeige:
    /// Auf einer Merkmalszeile (Stufe 3) übernimmt sie GENAU DIESES EINE FELD aus einer
    /// anderen Version derselben Gruppe (<see cref="MerkmalUebernahmeCtrl"/>), auf einer
    /// Bestandszeile (Stufe 1) den ganzen Komponentenbestand des Gewerks
    /// (<see cref="KomponentenUebernahmeCtrl"/>). Beides fragt vorher über
    /// <see cref="Form_BkUebernahme"/> nach. Wo die Übernahme nicht trägt — Schlüsselspalte
    /// oder nicht umgesetztes Gewerk —, steht statt des Knopfes ein Strich mit Begründung
    /// im Kurzhinweis; ein sichtbarer, aber wirkungsloser Knopf wäre die schlechtere Auskunft.
    /// </summary>
    public class UcBkUebersicht : UserControl
    {
        // Registry-Ablage der zuletzt gewählten Stamm-Auswahl (Pfad wie im Altdialog,
        // damit die zuletzt bearbeitete Gruppe erhalten bleibt).
        private const string RegPfad = @"Software\EPOS_PLAN\Variantentest";
        private const string RegWertStamm = "LetzterStammID";

        private readonly VariantenCtrl _ctrl = new VariantenCtrl();

        // ID des in Form_Start geöffneten Projekts (-1 = ohne Kontext).
        private int _aktuellesProjekt = -1;

        // Variante, die nach dem Laden markiert werden soll (-1 = keine).
        private int _markiereVarianteId = -1;

        private bool _laedt;

        // Zwischengespeicherte Detaildaten der GANZEN Vergleichsgruppe (Stamm und
        // Varianten). Die Gegenüberstellung der Stammansicht braucht N+1 Ladungen;
        // ohne Puffer läse jeder Klick in der Liste die komplette Gruppe neu. Der
        // Puffer gehört zu genau einer Gruppe (_detailsGruppe) und wird verworfen,
        // sobald sich der Stand ändern konnte: Gruppenwechsel, Simulation, Übernahme,
        // Anlegen/Löschen einer Variante.
        private readonly Dictionary<int, ProjektDetails> _details = new Dictionary<int, ProjektDetails>();
        private int _detailsGruppe = -1;

        // Steuerelemente
        private TableLayoutPanel tl;
        private TableLayoutPanel pnlKopf;
        private Label lblStamm;
        private ComboBox cbStamm;
        private CheckBox chkNurStaemme;
        private ListView lvAuswahl;
        private ColumnHeader colArt, colBezeichner, colProjektname, colSim;
        private TableLayoutPanel pnlVerwaltung;
        private Label lblBez;
        private TextBox txtBezeichner;
        private Button btnAnlegen, btnLoeschen, btnSimulieren;
        private TableLayoutPanel pnlKomponenten;
        private Label lblKomponenten;
        private DataGridView gridKomp;
        private Label lblStatus;

        /// <summary>Das gewählte Stammprojekt hat gewechselt (ID, Projektname).</summary>
        public event Action<int, string> StammGewechselt;

        /// <summary>In der Liste wurde eine Zeile markiert (Projekt-ID, ist Stammzeile).</summary>
        public event Action<int, bool> ProjektMarkiert;

        /// <summary>
        /// Datensatz je Zeile der Unterschiedsanzeige. Trägt alles, was die Übernahme
        /// eines Merkmals vom Stamm in die Variante braucht: Quell- und Zielprojekt,
        /// Tabelle und Spalte des Merkmals sowie die beiden Anzeigewerte.
        /// <c>Tabelle</c>/<c>Spalte</c>/<c>Feld</c> sind null, wenn die Zeile aus Stufe 1
        /// des <see cref="AbweichungsErmittler"/> stammt („Bestand", „Anzahl Komponenten") —
        /// solche Unterschiede lassen sich nicht feldweise, sondern nur als ganzer
        /// Komponentenbestand übernehmen.
        /// </summary>
        public class UebernahmeZeile
        {
            public int IdStamm;
            public int IdVariante;
            public string Gewerk = "";
            public string Merkmal = "";      // Anzeigename (AbweichungsErmittler.Merkmal.Label)
            public string Tabelle;           // z. B. "Tab_WP" (null = nicht feldbezogen)
            public string Spalte;            // z. B. "Nennleistung" (null = nicht feldbezogen)
            public int Dez = AbweichungsErmittler.TEXT;   // Formatvorgabe des Merkmals

            /// <summary>
            /// Das Merkmal der deklarativen Feldliste selbst (null bei Stufe-1-Zeilen).
            /// Es ist der Schlüssel zur Zeilenzuordnung: derselbe Eintrag, aus dem die
            /// Anzeige ihre beiden Werte gelesen hat, bestimmt auch die Zielzeile des
            /// UPDATE (<see cref="MerkmalUebernahmeCtrl"/>).
            /// </summary>
            public AbweichungsErmittler.Merkmal Feld;

            public string WertStamm = "";
            public string WertVariante = "";
        }

        public UcBkUebersicht()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        // ------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            tl = new TableLayoutPanel();
            pnlKopf = new TableLayoutPanel();
            lblStamm = new Label();
            cbStamm = new ComboBox();
            chkNurStaemme = new CheckBox();
            lvAuswahl = new ListView();
            colArt = new ColumnHeader();
            colBezeichner = new ColumnHeader();
            colProjektname = new ColumnHeader();
            colSim = new ColumnHeader();
            pnlVerwaltung = new TableLayoutPanel();
            lblBez = new Label();
            txtBezeichner = new TextBox();
            btnAnlegen = new Button();
            btnLoeschen = new Button();
            btnSimulieren = new Button();
            pnlKomponenten = new TableLayoutPanel();
            lblKomponenten = new Label();
            gridKomp = new DataGridView();
            lblStatus = new Label();
            tl.SuspendLayout();
            pnlKopf.SuspendLayout();
            pnlVerwaltung.SuspendLayout();
            pnlKomponenten.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridKomp).BeginInit();
            SuspendLayout();
            // 
            // tl
            // 
            tl.ColumnCount = 2;
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 222F));
            tl.Controls.Add(pnlKopf, 0, 0);
            tl.Controls.Add(lvAuswahl, 0, 1);
            tl.Controls.Add(pnlVerwaltung, 1, 1);
            tl.Controls.Add(pnlKomponenten, 0, 2);
            tl.Controls.Add(lblStatus, 0, 3);
            tl.Dock = DockStyle.Fill;
            tl.Location = new Point(0, 0);
            tl.Name = "tl";
            tl.Padding = new Padding(10, 8, 10, 6);
            tl.RowCount = 4;
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 46F));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 54F));
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tl.Size = new Size(1040, 520);
            tl.TabIndex = 0;
            // 
            // pnlKopf
            // 
            pnlKopf.ColumnCount = 3;
            tl.SetColumnSpan(pnlKopf, 2);
            pnlKopf.ColumnStyles.Add(new ColumnStyle());
            pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlKopf.ColumnStyles.Add(new ColumnStyle());
            pnlKopf.Controls.Add(lblStamm, 0, 0);
            pnlKopf.Controls.Add(cbStamm, 1, 0);
            pnlKopf.Controls.Add(chkNurStaemme, 2, 0);
            pnlKopf.Dock = DockStyle.Fill;
            pnlKopf.Location = new Point(10, 8);
            pnlKopf.Margin = new Padding(0, 0, 0, 6);
            pnlKopf.Name = "pnlKopf";
            pnlKopf.RowCount = 1;
            pnlKopf.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlKopf.Size = new Size(1020, 26);
            pnlKopf.TabIndex = 0;
            // 
            // lblStamm
            // 
            lblStamm.Anchor = AnchorStyles.Left;
            lblStamm.AutoSize = true;
            lblStamm.Location = new Point(0, 0);
            lblStamm.Margin = new Padding(0, 0, 8, 0);
            lblStamm.Name = "lblStamm";
            lblStamm.Size = new Size(128, 25);
            lblStamm.TabIndex = 0;
            lblStamm.Text = "Stammprojekt:";
            // 
            // cbStamm
            // 
            cbStamm.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbStamm.DropDownStyle = ComboBoxStyle.DropDownList;
            cbStamm.Location = new Point(136, 2);
            cbStamm.Margin = new Padding(0, 2, 12, 2);
            cbStamm.Name = "cbStamm";
            cbStamm.Size = new Size(682, 33);
            cbStamm.TabIndex = 1;
            cbStamm.SelectedIndexChanged += cbStamm_SelectedIndexChanged;
            // 
            // chkNurStaemme
            // 
            chkNurStaemme.Anchor = AnchorStyles.Left;
            chkNurStaemme.AutoSize = true;
            chkNurStaemme.Location = new Point(830, 0);
            chkNurStaemme.Margin = new Padding(0);
            chkNurStaemme.Name = "chkNurStaemme";
            chkNurStaemme.Size = new Size(190, 26);
            chkNurStaemme.TabIndex = 2;
            chkNurStaemme.Text = MyResource.Resource.BK_CHK_NURSTAEMME;
            chkNurStaemme.CheckedChanged += chkNurStaemme_CheckedChanged;
            // 
            // lvAuswahl
            // 
            lvAuswahl.Columns.AddRange(new ColumnHeader[] { colArt, colBezeichner, colProjektname, colSim });
            lvAuswahl.Dock = DockStyle.Fill;
            lvAuswahl.FullRowSelect = true;
            lvAuswahl.Location = new Point(10, 40);
            lvAuswahl.Margin = new Padding(0, 0, 8, 6);
            lvAuswahl.MultiSelect = false;
            lvAuswahl.Name = "lvAuswahl";
            lvAuswahl.Size = new Size(790, 201);
            lvAuswahl.TabIndex = 1;
            lvAuswahl.UseCompatibleStateImageBehavior = false;
            lvAuswahl.View = View.Details;
            lvAuswahl.SelectedIndexChanged += lvAuswahl_SelectedIndexChanged;
            // 
            // colArt
            // 
            colArt.Text = MyResource.Resource.BK_SP_ART;
            colArt.Width = 90;
            // 
            // colBezeichner
            // 
            colBezeichner.Text = MyResource.Resource.BK_SP_BEZEICHNER;
            colBezeichner.Width = 190;
            // 
            // colProjektname
            // 
            colProjektname.Text = MyResource.Resource.BK_SP_PROJEKTNAME;
            colProjektname.Width = 300;
            // 
            // colSim
            // 
            colSim.Text = MyResource.Resource.BK_SP_SIMSTAND;
            colSim.Width = 140;
            // 
            // pnlVerwaltung
            // 
            pnlVerwaltung.ColumnCount = 1;
            pnlVerwaltung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlVerwaltung.Controls.Add(lblBez, 0, 0);
            pnlVerwaltung.Controls.Add(txtBezeichner, 0, 1);
            pnlVerwaltung.Controls.Add(btnAnlegen, 0, 2);
            pnlVerwaltung.Controls.Add(btnLoeschen, 0, 3);
            pnlVerwaltung.Controls.Add(btnSimulieren, 0, 4);
            pnlVerwaltung.Dock = DockStyle.Fill;
            pnlVerwaltung.Location = new Point(808, 40);
            pnlVerwaltung.Margin = new Padding(0, 0, 0, 6);
            pnlVerwaltung.Name = "pnlVerwaltung";
            pnlVerwaltung.RowCount = 6;
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            pnlVerwaltung.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlVerwaltung.Size = new Size(222, 201);
            pnlVerwaltung.TabIndex = 2;
            // 
            // lblBez
            // 
            lblBez.AutoSize = true;
            lblBez.Dock = DockStyle.Fill;
            lblBez.Location = new Point(0, 0);
            lblBez.Margin = new Padding(0);
            lblBez.Name = "lblBez";
            lblBez.Size = new Size(222, 18);
            lblBez.TabIndex = 0;
            lblBez.Text = "Bezeichner:";
            // 
            // txtBezeichner
            // 
            txtBezeichner.Dock = DockStyle.Fill;
            txtBezeichner.Location = new Point(0, 20);
            txtBezeichner.Margin = new Padding(0, 2, 0, 8);
            txtBezeichner.Name = "txtBezeichner";
            txtBezeichner.Size = new Size(222, 31);
            txtBezeichner.TabIndex = 1;
            // 
            // btnAnlegen
            // 
            btnAnlegen.Dock = DockStyle.Fill;
            btnAnlegen.Location = new Point(0, 51);
            btnAnlegen.Margin = new Padding(0, 0, 0, 6);
            btnAnlegen.Name = "btnAnlegen";
            btnAnlegen.Size = new Size(222, 26);
            btnAnlegen.TabIndex = 2;
            btnAnlegen.Text = MyResource.Resource.BK_BTN_ANLEGEN;
            btnAnlegen.Click += btnAnlegen_Click;
            // 
            // btnLoeschen
            // 
            btnLoeschen.Dock = DockStyle.Fill;
            btnLoeschen.Location = new Point(0, 83);
            btnLoeschen.Margin = new Padding(0, 0, 0, 14);
            btnLoeschen.Name = "btnLoeschen";
            btnLoeschen.Size = new Size(222, 26);
            btnLoeschen.TabIndex = 3;
            btnLoeschen.Text = MyResource.Resource.BK_BTN_LOESCHEN;
            btnLoeschen.Click += btnLoeschen_Click;
            // 
            // btnSimulieren
            // 
            btnSimulieren.Dock = DockStyle.Fill;
            btnSimulieren.Location = new Point(0, 123);
            btnSimulieren.Margin = new Padding(0);
            btnSimulieren.Name = "btnSimulieren";
            btnSimulieren.Size = new Size(222, 34);
            btnSimulieren.TabIndex = 4;
            btnSimulieren.Text = MyResource.Resource.BK_BTN_SIMULIEREN;
            btnSimulieren.Click += btnSimulieren_Click;
            // 
            // pnlKomponenten
            // 
            pnlKomponenten.ColumnCount = 1;
            tl.SetColumnSpan(pnlKomponenten, 2);
            pnlKomponenten.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlKomponenten.Controls.Add(lblKomponenten, 0, 0);
            pnlKomponenten.Controls.Add(gridKomp, 0, 1);
            pnlKomponenten.Dock = DockStyle.Fill;
            pnlKomponenten.Location = new Point(10, 247);
            pnlKomponenten.Margin = new Padding(0);
            pnlKomponenten.Name = "pnlKomponenten";
            pnlKomponenten.RowCount = 2;
            pnlKomponenten.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            pnlKomponenten.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlKomponenten.Size = new Size(1020, 243);
            pnlKomponenten.TabIndex = 3;
            // 
            // lblKomponenten
            // 
            lblKomponenten.Dock = DockStyle.Fill;
            lblKomponenten.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblKomponenten.Location = new Point(0, 0);
            lblKomponenten.Margin = new Padding(0);
            lblKomponenten.Name = "lblKomponenten";
            lblKomponenten.Size = new Size(1020, 22);
            lblKomponenten.TabIndex = 0;
            lblKomponenten.Text = "Komponenten im Vergleich — Stammprojekt und Varianten";
            // 
            // gridKomp
            // 
            gridKomp.AllowUserToAddRows = false;
            gridKomp.AllowUserToDeleteRows = false;
            gridKomp.AllowUserToResizeRows = false;
            gridKomp.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            gridKomp.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            gridKomp.ColumnHeadersHeight = 34;
            gridKomp.Dock = DockStyle.Fill;
            gridKomp.Location = new Point(0, 22);
            gridKomp.Margin = new Padding(0);
            gridKomp.Name = "gridKomp";
            gridKomp.ReadOnly = true;
            gridKomp.RowHeadersVisible = false;
            gridKomp.RowHeadersWidth = 62;
            gridKomp.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridKomp.Size = new Size(1020, 221);
            gridKomp.TabIndex = 1;
            gridKomp.CellContentClick += gridKomp_CellContentClick;
            gridKomp.SelectionChanged += gridKomp_KomponentenAuswahl;
            // 
            // lblStatus
            // 
            tl.SetColumnSpan(lblStatus, 2);
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Location = new Point(10, 494);
            lblStatus.Margin = new Padding(0, 4, 0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1020, 20);
            lblStatus.TabIndex = 4;
            // 
            // UcBkUebersicht
            // 
            Controls.Add(tl);
            Font = new Font("Segoe UI", 9F);
            Name = "UcBkUebersicht";
            Size = new Size(1040, 520);
            tl.ResumeLayout(false);
            pnlKopf.ResumeLayout(false);
            pnlKopf.PerformLayout();
            pnlVerwaltung.ResumeLayout(false);
            pnlVerwaltung.PerformLayout();
            pnlKomponenten.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridKomp).EndInit();
            ResumeLayout(false);
        }

        // ------------------------------------------------------------- Laden

        /// <summary>
        /// Setzt den Projektkontext des Reiters (das in Form_Start geöffnete Projekt)
        /// und baut die Auswahl neu auf. Ist das Projekt eine Variante, wird deren
        /// Stammprojekt gewählt und die Variante in der Liste markiert.
        /// </summary>
        public void SetzeAktuellesProjekt(int idProjekt)
        {
            _aktuellesProjekt = idProjekt;
            LadeProjekte();
        }

        private void LadeProjekte()
        {
            if (this.DesignMode) return;
            try
            {
                _ctrl.StelleVariantentabelleSicher();
                FuelleStammCombo();
                if (cbStamm.Items.Count == 0) { LadeAuswahl(); return; }

                // Vorrang: aktuelles Projekt -> letzte Auswahl -> erster Eintrag.
                int idx = FindeStammIndex(BestimmeVorauswahl());
                _laedt = true;
                try { cbStamm.SelectedIndex = idx >= 0 ? idx : 0; }
                finally { _laedt = false; }
                MeldeStammWechsel();
                LadeAuswahl();
            }
            catch (Exception ex)
            { Melde(string.Format(MyResource.Resource.BK_MSG_LADEFEHLER, ex.Message)); }
        }

        // Befüllt das Stamm-Dropdown - je nach Filter alle Projekte oder nur bereits
        // gesetzte Stammprojekte.
        private void FuelleStammCombo()
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadAll();

            HashSet<int> nurStaemme = null;
            if (chkNurStaemme != null && chkNurStaemme.Checked)
                nurStaemme = _ctrl.LiesStammProjektIds();

            cbStamm.Items.Clear();
            foreach (ProjektModel p in pc.items)
            {
                if (nurStaemme != null && !nurStaemme.Contains(p.m_ID)) continue;
                cbStamm.Items.Add(new ProjektEintrag(p.m_ID, p.m_szProjektname));
            }
        }

        // Combo neu aufbauen (z. B. nach Anlegen einer Variante oder Filterwechsel) und
        // die gewünschte Stamm-ID beibehalten, sonst ersten Eintrag wählen.
        private void AktualisiereStammCombo(int stammId)
        {
            FuelleStammCombo();
            if (cbStamm.Items.Count == 0) { LadeAuswahl(); return; }
            int idx = FindeStammIndex(stammId);
            _laedt = true;
            try { cbStamm.SelectedIndex = idx >= 0 ? idx : 0; }
            finally { _laedt = false; }
            MeldeStammWechsel();
            LadeAuswahl();
        }

        // Bestimmt das vorzuwählende Stammprojekt:
        //  1. aktuell geöffnetes Projekt aus Form_Start (ist es eine Variante -> deren
        //     Stammprojekt, die Variante wird anschließend in der Liste markiert),
        //  2. sonst die zuletzt gewählte Auswahl (Registry),
        //  3. sonst -1 (Aufrufer nimmt den ersten Eintrag).
        private int BestimmeVorauswahl()
        {
            _markiereVarianteId = -1;

            if (_aktuellesProjekt > 0)
            {
                int refId = _ctrl.StammRefDerVariante(_aktuellesProjekt);
                if (refId > 0)
                {
                    _markiereVarianteId = _aktuellesProjekt;   // geöffnetes Projekt ist eine Variante
                    return refId;                              // -> deren Stammprojekt wählen
                }
                return _aktuellesProjekt;                      // ist selbst ein (mögliches) Stammprojekt
            }

            return LiesLetztenStamm();
        }

        private int FindeStammIndex(int idProjekt)
        {
            if (idProjekt <= 0) return -1;
            for (int i = 0; i < cbStamm.Items.Count; i++)
            {
                ProjektEintrag pe = cbStamm.Items[i] as ProjektEintrag;
                if (pe != null && pe.Id == idProjekt) return i;
            }
            return -1;
        }

        // Merkt sich die zuletzt gewählte Stamm-Auswahl (Registry, HKCU).
        private void SpeichereLetztenStamm(int idProjekt)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPfad))
                {
                    if (key != null) key.SetValue(RegWertStamm, idProjekt, RegistryValueKind.DWord);
                }
            }
            catch { /* Persistenz ist optional - Fehler hier nicht kritisch. */ }
        }

        private int LiesLetztenStamm()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPfad))
                {
                    object v = key?.GetValue(RegWertStamm);
                    if (v != null) return Convert.ToInt32(v);
                }
            }
            catch { }
            return -1;
        }

        /// <summary>Aktuell gewähltes Stammprojekt (null = keins).</summary>
        public ProjektEintrag AktuellerStamm { get { return cbStamm.SelectedItem as ProjektEintrag; } }

        /// <summary>Aktuell markierte Zeile der Liste (null = keine).</summary>
        public AuswahlZeile AktuelleZeile
        {
            get { return lvAuswahl.SelectedItems.Count > 0 ? lvAuswahl.SelectedItems[0].Tag as AuswahlZeile : null; }
        }

        /// <summary>Das dem Reiter übergebene, tatsächlich geöffnete Projekt (-1 = keines).</summary>
        public int AktuellesProjekt { get { return _aktuellesProjekt; } }

        /// <summary>
        /// Die Listenzeile zu einer Projekt-ID — auch dann, wenn die Liste (noch) an keinem
        /// Fenster hängt: <c>ListView.SelectedItems</c> ist ohne Fensterhandle leer, die
        /// Zeilen selbst stehen aber längst im Steuerelement. null, wenn die Gruppe diese
        /// ID nicht führt.
        /// </summary>
        public AuswahlZeile ZeileFuer(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            foreach (ListViewItem it in lvAuswahl.Items)
            {
                AuswahlZeile z = it.Tag as AuswahlZeile;
                if (z != null && z.IdProjekt == idProjekt) return z;
            }
            return null;
        }

        // Füllt die Liste mit dem Stammprojekt (erste Zeile) und seinen Varianten.
        private void LadeAuswahl()
        {
            lvAuswahl.Items.Clear();
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { AktualisiereButtons(); ZeigeKomponenten(); return; }

            // Simulationsstände über denselben Weg wie Bericht/Wirtschaftlichkeit.
            var stand = new Dictionary<int, string>();
            try
            {
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(stamm.Id, stamm.Name))
                    stand[st.IdProjekt] = st.SimStandText;
            }
            catch { }

            foreach (VariantenCtrl.VarianteInfo vi in _ctrl.LadeGruppe(stamm.Id, stamm.Name))
            {
                ListViewItem it = new ListViewItem(new[]
                {
                    vi.IstStamm ? MyResource.Resource.BK_ART_STAMM : MyResource.Resource.BK_ART_VARIANTE,
                    vi.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT : vi.Variantenname,
                    vi.Projektname,
                    stand.ContainsKey(vi.IdProjekt) ? stand[vi.IdProjekt] : ""
                })
                {
                    Tag = new AuswahlZeile(vi.IdProjekt, vi.Projektname, vi.Variantenname, vi.IstStamm)
                };
                lvAuswahl.Items.Add(it);
            }

            WaehleZeile();
            AktualisiereButtons();
        }

        /// <summary>
        /// Wählt nach dem Laden die passende Listenzeile — in dieser Reihenfolge:
        /// <list type="number">
        ///   <item>die ausdrücklich vorgemerkte Zeile (<c>_markiereVarianteId</c>, gesetzt
        ///         nach einer Übernahme und beim Betreten mit geöffneter Variante) — sie hat
        ///         Vorrang, gilt aber nur EINMAL,</item>
        ///   <item>sonst das TATSÄCHLICH GEÖFFNETE Projekt (<c>_aktuellesProjekt</c>),
        ///         gleich ob es der Stamm oder eine Variante der Gruppe ist,</item>
        ///   <item>sonst das Stammprojekt (Zeile 0).</item>
        /// </list>
        /// Punkt 2 ist der Grund, warum die Kostenseite dem geöffneten Projekt folgt: Sie
        /// zeigt die markierte Zeile, und die Markierung ist beim Betreten nicht mehr blind
        /// Zeile 0. Vorher stand hier nur „vorgemerkte Variante, sonst Zeile 0" — wer den
        /// Reiter mit einer Variante betrat, dessen Vormerkung aber schon verbraucht war
        /// (zweites Laden derselben Gruppe, Rückkehr aus einer anderen Gruppe), landete auf
        /// dem Stamm und bekam dessen Zahlen zu sehen.
        /// </summary>
        private void WaehleZeile()
        {
            if (lvAuswahl.Items.Count == 0) { ZeigeKomponenten(); return; }

            if (_markiereVarianteId > 0)
            {
                int vorgemerkt = _markiereVarianteId;
                _markiereVarianteId = -1;                 // nur einmal (Erstladen) anwenden
                if (MarkiereZeile(vorgemerkt)) return;
            }

            if (_aktuellesProjekt > 0 && MarkiereZeile(_aktuellesProjekt)) return;

            // Das geöffnete Projekt gehört nicht zu dieser Gruppe (der Anwender hat im
            // Auswahlfeld ein anderes Stammprojekt gewählt) — dann ist der Stamm die
            // richtige Ausgangszeile.
            lvAuswahl.Items[0].Selected = true;
        }

        /// <summary>
        /// Markiert die Listenzeile mit dieser Projekt-ID — Stamm wie Variante, denn die
        /// ID ist innerhalb der Gruppe eindeutig. Liefert false, wenn die Gruppe die ID
        /// nicht führt; der Aufrufer entscheidet dann über den Ersatz.
        /// </summary>
        private bool MarkiereZeile(int idProjekt)
        {
            foreach (ListViewItem it in lvAuswahl.Items)
            {
                AuswahlZeile z = it.Tag as AuswahlZeile;
                if (z == null || z.IdProjekt != idProjekt) continue;

                it.Selected = true;
                it.EnsureVisible();
                return true;
            }
            return false;
        }

        private void AktualisiereButtons()
        {
            AuswahlZeile z = AktuelleZeile;
            btnSimulieren.Enabled = z != null;
            btnLoeschen.Enabled = z != null && !z.IstStamm;
            btnAnlegen.Enabled = AktuellerStamm != null;
        }

        // ------------------------------------------------------------- Ereignisse

        private void cbStamm_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_laedt) return;
            VerwirfDetails();
            MeldeStammWechsel();
            LadeAuswahl();
        }

        private void MeldeStammWechsel()
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) return;
            SpeichereLetztenStamm(stamm.Id);
            VerwirfDetails();
            Action<int, string> h = StammGewechselt;
            if (h != null) h(stamm.Id, stamm.Name);
        }

        private void chkNurStaemme_CheckedChanged(object sender, EventArgs e)
        {
            AktualisiereStammCombo(AktuellerStamm != null ? AktuellerStamm.Id : -1);
        }

        private void lvAuswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            AktualisiereButtons();
            ZeigeKomponenten();

            AuswahlZeile z = AktuelleZeile;
            Action<int, bool> h = ProjektMarkiert;
            if (h != null && z != null) h(z.IdProjekt, z.IstStamm);
        }

        // ------------------------------------------------------------- Aktionen

        private void btnAnlegen_Click(object sender, EventArgs e)
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { Melde(MyResource.Resource.BK_MSG_KEIN_STAMM); return; }

            try
            {
                Cursor = Cursors.WaitCursor;

                string fehler;
                int neueId = _ctrl.AnlegenAusStamm(stamm.Id, stamm.Name, txtBezeichner.Text, out fehler);
                if (neueId <= 0) { Melde(fehler ?? MyResource.Resource.BK_MSG_ANLEGEN_FEHLGESCHLAGEN); return; }

                string bezeichner = (txtBezeichner.Text ?? "").Trim();
                txtBezeichner.Clear();
                AktualisiereStammCombo(stamm.Id);   // Combo neu (neue Variante -> Stammstatus), Auswahl beibehalten
                // Ä19: Auch die Varianten-Klappliste des Projektkopfs kennt die neue
                // Variante sofort (bisher zog nur der Menüweg über Form_AlsVariante nach).
                Program.startfrm?.VariantenAnzeigeAktualisieren();
                Melde(string.Format(MyResource.Resource.BK_MSG_VARIANTE_ANGELEGT, bezeichner));
            }
            catch (Exception ex) { Melde(string.Format(MyResource.Resource.BK_MSG_ANLEGEFEHLER, ex.Message)); }
            finally { Cursor = Cursors.Default; }
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            AuswahlZeile z = AktuelleZeile;
            if (z == null || z.IstStamm) { Melde(MyResource.Resource.BK_MSG_NUR_VARIANTE); return; }

            DialogResult dr = MessageBox.Show(
                string.Format(MyResource.Resource.BK_MSG_LOESCHEN_FRAGE, z.Variantenname),
                MyResource.Resource.BK_BTN_LOESCHEN,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.Yes) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                string fehler;
                if (!_ctrl.LoescheVariante(z.IdProjekt, z.Projektname, out fehler))
                { Melde(fehler ?? MyResource.Resource.BK_MSG_LOESCHEN_FEHLGESCHLAGEN); return; }

                VerwirfDetails();   // die Gruppe hat eine Spalte weniger
                LadeAuswahl();
                Melde(string.Format(MyResource.Resource.BK_MSG_VARIANTE_GELOESCHT, z.Variantenname));
            }
            catch (Exception ex) { Melde(string.Format(MyResource.Resource.BK_MSG_LOESCHFEHLER, ex.Message)); }
            finally { Cursor = Cursors.Default; }
        }

        private void btnSimulieren_Click(object sender, EventArgs e)
        {
            AuswahlZeile z = AktuelleZeile;
            ProjektEintrag stamm = AktuellerStamm;
            if (z == null || stamm == null) { Melde(MyResource.Resource.BK_MSG_BITTE_WAEHLEN); return; }

            // Zu simulierende Projekte: der Stamm immer, plus die gewählte Variante.
            // So werden die Ergebnisse von Stamm UND Variante frisch geschrieben.
            var laeufe = new List<Tuple<int, string>>();
            laeufe.Add(Tuple.Create(stamm.Id, string.Format(MyResource.Resource.BK_PRAEFIX_STAMM, stamm.Name)));
            if (!z.IstStamm)
                laeufe.Add(Tuple.Create(z.IdProjekt,
                    string.Format(MyResource.Resource.BK_PRAEFIX_VARIANTE, z.Variantenname)));

            try
            {
                Cursor = Cursors.WaitCursor;

                var meldungen = new List<string>();
                foreach (Tuple<int, string> lauf in laeufe)
                {
                    // Headless-Lauf: neue Instanz je Projekt (frische Simulationsobjekte).
                    string fehler;
                    SimulationRunner runner = new SimulationRunner();
                    int erg = runner.SimuliereUndSpeichere(lauf.Item1, out fehler);
                    meldungen.Add(erg > 0
                        ? string.Format(MyResource.Resource.BK_MSG_SIM_OK, lauf.Item2, erg)
                        : string.Format(MyResource.Resource.BK_MSG_SIM_FEHLER, lauf.Item2, fehler));

                    // Auch ein ERFOLGREICHER Lauf kann gemeldet haben, dass er mit einer
                    // Ersatzannahme gerechnet hat (Paket-8-Fehlerkanal); „out fehler" ist
                    // nur im Misserfolgsfall belegt.
                    string hinweise = runner.Protokoll != null
                        ? runner.Protokoll.HinweistextFuerAnzeige() : "";
                    if (!string.IsNullOrEmpty(hinweise))
                        meldungen.Add("    " + hinweise.Replace("\r\n", "\r\n    ")
                                                       .Replace("\n", "\n    "));
                }

                VerwirfDetails();
                LadeAuswahl();
                Melde(string.Format(MyResource.Resource.BK_MSG_SIM_FERTIG, laeufe.Count));
                MessageBox.Show(string.Join("\r\n", meldungen), MyResource.Resource.BK_TITEL_SIMULATION,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Melde(string.Format(MyResource.Resource.BK_MSG_SIMFEHLER, ex.Message)); }
            finally { Cursor = Cursors.Default; }
        }

        // --------------------------------------------- Komponenten / Unterschiede

        /// <summary>
        /// Zeigt für die Stammzeile die GEGENÜBERSTELLUNG von Stamm und allen Varianten
        /// der Gruppe, für eine Variantenzeile deren Unterschiede zum Stamm. Beide
        /// Ansichten arbeiten mit der deklarativen Feldliste des
        /// <see cref="AbweichungsErmittler"/>.
        /// </summary>
        public void ZeigeKomponenten()
        {
            gridKomp.Rows.Clear();
            gridKomp.Columns.Clear();

            ProjektEintrag stamm = AktuellerStamm;
            AuswahlZeile z = AktuelleZeile;
            if (stamm == null || z == null)
            {
                lblKomponenten.Text = MyResource.Resource.BK_LBL_KOMPONENTEN_VERGLEICH;
                return;
            }

            ProjektDetails ds = Details(stamm.Id, stamm.Id);
            if (z.IstStamm) ZeigeStammVergleich(stamm, ds);
            else ZeigeUnterschiede(stamm, z, ds);
        }

        /// <summary>
        /// Detaildaten eines Projekts der Gruppe — aus dem Puffer, sonst frisch geladen.
        /// Der Puffer gehört zu GENAU EINER Gruppe: wechselt das Stammprojekt, wird er
        /// geleert, statt Zeilen fremder Gruppen mitzuschleppen. Damit kostet das
        /// Umschalten zwischen den Listenzeilen keine Datenbankrunde mehr — die
        /// Gegenüberstellung braucht N+1 Ladungen, die Unterschiedsansicht zwei, und
        /// beide greifen auf denselben Vorrat zu.
        /// </summary>
        private ProjektDetails Details(int idStamm, int idProjekt)
        {
            if (_detailsGruppe != idStamm) { _details.Clear(); _detailsGruppe = idStamm; }

            ProjektDetails d;
            if (_details.TryGetValue(idProjekt, out d)) return d;

            d = ProjektDetails.Lade(idProjekt);
            _details[idProjekt] = d;
            return d;
        }

        /// <summary>
        /// Puffer verwerfen. Aufzurufen, sobald sich der gepufferte Stand ändern konnte:
        /// Gruppenwechsel, Simulationslauf, Übernahme, Anlegen/Löschen einer Variante.
        /// </summary>
        private void VerwirfDetails()
        {
            _details.Clear();
            _detailsGruppe = -1;
        }

        /// <summary>
        /// Höchstzahl der Variantenspalten in der Gegenüberstellung. Mehr Spalten sind
        /// auf einem Bildschirm nicht mehr zu lesen; darüber wird gekappt — aber
        /// SICHTBAR: Überschrift und Statuszeile nennen die Zahl der ausgeblendeten
        /// Varianten. Stilles Abschneiden ließe eine unvollständige Tabelle wie eine
        /// vollständige aussehen.
        /// </summary>
        private const int MAX_VARIANTENSPALTEN = 8;

        /// <summary>
        /// Bis zu so vielen Variantenspalten füllt die Tabelle die Breite aus; darüber
        /// bekommen die Spalten feste Breiten und die Tabelle blättert waagerecht.
        /// </summary>
        private const int FUELLEN_BIS_VARIANTEN = 4;

        /// <summary>
        /// Zelltext für „führt diese Version nicht". Derselbe Strich, den
        /// <see cref="AbweichungsErmittler.Formatiere"/> für einen leeren Wert setzt und
        /// den die Aktionsspalte für „hier nichts zu tun" verwendet — eine Schreibweise
        /// für „hier steht nichts" statt einer zweiten.
        /// </summary>
        private const string OHNE_WERT = "—";

        // Kontext der Gegenüberstellung für Tooltip und Auswahlanzeige
        // (Nutzerauftrag 28.08.2026: eine Zeile je Komponente).
        private List<ProjektDetails> _vergleichsVersionen;
        private List<string> _vergleichsKoepfe;
        private sealed class KomponentenZelle { public string Gewerk; public int Index; }

        // Ressourcen-Helfer mit deutschem Fallback (Drei-Schichten-Regel; die
        // generierten Resource-Eigenschaften entstehen erst im VS-Designer).
        private static string TUeb(string key, string fallback)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch { return fallback; }
        }

        // Gegenüberstellung Stamm ↔ Varianten: Gewerk · Merkmal · Stamm · je Variante eine
        // Spalte, in der Reihenfolge der oberen Liste. Sie ersetzt die frühere
        // Einzelansicht des Stammprojekts: Deren eigentlicher Zweck — der Vergleich der
        // Komponentenzahlen je Gewerk — kostete bis dahin einen Klick je Variante.
        private void ZeigeStammVergleich(ProjektEintrag stamm, ProjektDetails ds)
        {
            List<AuswahlZeile> varianten = VariantenDerListe();
            int ausgelassen = Math.Max(0, varianten.Count - MAX_VARIANTENSPALTEN);
            if (ausgelassen > 0) varianten = varianten.GetRange(0, MAX_VARIANTENSPALTEN);

            string gekappt = ausgelassen > 0
                ? string.Format(MyResource.Resource.BK_LBL_VARIANTEN_GEKAPPT, varianten.Count, ausgelassen)
                : "";
            lblKomponenten.Text = MyResource.Resource.BK_LBL_KOMPONENTEN_VERGLEICH +
                                  (gekappt.Length == 0 ? "" : " — " + gekappt);

            BaueVergleichsSpalten(varianten);

            // Die Versionen in Spaltenreihenfolge: Stamm zuerst, dann die Varianten.
            var versionen = new List<ProjektDetails> { ds };
            foreach (AuswahlZeile v in varianten) versionen.Add(Details(stamm.Id, v.IdProjekt));

            // Kontext für Tooltip/Auswahlanzeige der Komponentenzeilen (28.08.2026).
            _vergleichsVersionen = versionen;
            _vergleichsKoepfe = new List<string> { MyResource.Resource.BK_SP_WERT_STAMM };
            foreach (AuswahlZeile v in varianten) _vergleichsKoepfe.Add(SpaltenKopf(v));

            int zeilen = FuelleVergleich(versionen);

            gridKomp.ClearSelection();
            if (zeilen == 0) { Melde(MyResource.Resource.BK_MSG_KEINE_KOMPONENTEN); return; }

            string status = string.Format(MyResource.Resource.BK_MSG_VERGLEICH_UMFANG,
                                          zeilen, varianten.Count);
            Melde(gekappt.Length == 0 ? status : status + "  " + gekappt);
        }

        // Die Varianten der Gruppe in der Reihenfolge der oberen Liste — bewusst AUS der
        // Liste gelesen und nicht neu abgefragt, damit Spaltenfolge und Listenfolge nicht
        // auseinanderlaufen können.
        private List<AuswahlZeile> VariantenDerListe()
        {
            var liste = new List<AuswahlZeile>();
            foreach (ListViewItem it in lvAuswahl.Items)
            {
                AuswahlZeile z = it.Tag as AuswahlZeile;
                if (z != null && !z.IstStamm) liste.Add(z);
            }
            return liste;
        }

        // Spalten der Gegenüberstellung. Bis FUELLEN_BIS_VARIANTEN füllt die Tabelle die
        // Breite aus (der übliche Fall, eine bis vier Varianten); darüber bekommen die
        // Spalten feste Breiten, damit sie lesbar bleiben und die Tabelle waagerecht
        // blättert, statt alles auf Restbreiten zu quetschen.
        private void BaueVergleichsSpalten(List<AuswahlZeile> varianten)
        {
            gridKomp.AutoSizeColumnsMode = varianten.Count <= FUELLEN_BIS_VARIANTEN
                ? DataGridViewAutoSizeColumnsMode.Fill
                : DataGridViewAutoSizeColumnsMode.None;
            gridKomp.ScrollBars = ScrollBars.Both;

            gridKomp.Columns.Add("gewerk", MyResource.Resource.BK_SP_GEWERK);
            gridKomp.Columns.Add("merkmal", MyResource.Resource.BK_SP_MERKMAL);
            gridKomp.Columns.Add("v0", MyResource.Resource.BK_SP_WERT_STAMM);
            for (int i = 0; i < varianten.Count; i++)
                gridKomp.Columns.Add("v" + (i + 1).ToString(CultureInfo.InvariantCulture),
                                     SpaltenKopf(varianten[i]));

            Spaltenbreite(gridKomp.Columns[0], 90, 130);
            Spaltenbreite(gridKomp.Columns[1], 150, 200);
            for (int c = 2; c < gridKomp.Columns.Count; c++)
            {
                Spaltenbreite(gridKomp.Columns[c], 110, 115);
                gridKomp.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        // FillWeight gilt im Füllmodus, Width im festen Modus - beides zu setzen kostet
        // nichts und macht den Spaltenaufbau von der gewählten Betriebsart unabhängig.
        private static void Spaltenbreite(DataGridViewColumn sp, int gewicht, int breite)
        {
            sp.FillWeight = gewicht;
            sp.MinimumWidth = 60;
            sp.Width = breite;
        }

        // Spaltenkopf einer Variante: ihr Bezeichner (wie in der oberen Liste); fehlt er,
        // hilft der Projektname weiter.
        private static string SpaltenKopf(AuswahlZeile z)
        {
            return string.IsNullOrEmpty(z.Variantenname) ? z.Projektname : z.Variantenname;
        }

        // Eine Zeile je Merkmal, eine Zelle je Version. Ein Gewerk erscheint, sobald es
        // IRGENDEINE der Versionen führt - würde nur die Stamm-Probe entscheiden, verschwiege
        // die Tabelle genau das, was die Gegenüberstellung zeigen soll: dass die Variante
        // ein Gewerk hat, das der Stamm nicht kennt.
        private int FuelleVergleich(List<ProjektDetails> versionen)
        {
            int zeilen = 0;
            foreach (string gewerk in GewerkeInReihenfolge())
            {
                List<AbweichungsErmittler.Merkmal> felder =
                    AbweichungsErmittler.Felder.Where(f => f.Gewerk == gewerk).ToList();
                if (felder.Count == 0) continue;

                // „Anlage" und „Gebäude" sind Konfigurationsblöcke ohne Komponentenbestand;
                // nur die echten Gewerke der GewerkTabellen führen eine Stückzahl.
                bool zaehlbar = ProjektDetails.GewerkTabellen.Any(g => g.Key == gewerk);

                // Artefakt-Guard (Nutzerbefund 28.08.2026): Der Anlage-Block erscheint
                // nur, wenn alle Versionen mit echter Anlagenzeile DASSELBE Gewerk
                // führen — sonst stünden Werte verschiedener Gewerke nebeneinander
                // (WP-Stamm neben BHKW- und Kessel-Variante); Referenzanlagen zählen
                // ohnehin nicht (AbweichungsErmittler.ErsteEchteAnlage).
                if (!zaehlbar && felder[0].Tabelle == "Tab_Energieanlagen" &&
                    !AbweichungsErmittler.AnlagenEinheitlich(versionen)) continue;
                bool irgendwo = versionen.Any(d => AbweichungsErmittler.ZeileFuer(d, felder[0]) != null)
                             || (zaehlbar && versionen.Any(d => AbweichungsErmittler.Anzahl(d, gewerk) > 0));
                if (!irgendwo) continue;

                bool ersteZeile = true;

                // Kopfzeile des Gewerks: die Anzahl der VERBAUTEN Komponenten - dieselbe
                // Kennzahl aus derselben Quelle wie die Stufe-1-Zeile der Unterschiede
                // (AbweichungsErmittler.Anzahl -> ProjektDetails.KomponentenAnzahl ->
                // Tab_Energieanlagen). Sie ist der Grund für diese Ansicht.
                if (zaehlbar)
                {
                    var anzahlen = new List<string>();
                    foreach (ProjektDetails d in versionen)
                        anzahlen.Add(AbweichungsErmittler.AnzahlText(AbweichungsErmittler.Anzahl(d, gewerk)));
                    SchreibeVergleichsZeile(gewerk, AbweichungsErmittler.MERKMAL_ANZAHL, anzahlen);
                    zeilen++;

                    // Nutzerauftrag 28.08.2026: eine Zeile JE KOMPONENTE mit ihrem
                    // Bezeichner je Version — vorher zeigten die Merkmalszeilen nur
                    // die ERSTE Komponente (bei zwei Wärmepumpen fehlte die zweite
                    // komplett). Die Merkmale wandern in den Tooltip der Zelle
                    // (Mouse-over) und in die Statuszeile bei Auswahl.
                    AbweichungsErmittler.Merkmal bez = AbweichungsErmittler.BezeichnerMerkmal(gewerk);
                    int maxKomp = 0;
                    foreach (ProjektDetails d in versionen)
                        maxKomp = Math.Max(maxKomp, AbweichungsErmittler.Anzahl(d, gewerk));
                    for (int k = 0; k < maxKomp; k++)
                    {
                        var namen = new List<string>();
                        foreach (ProjektDetails d in versionen)
                        {
                            DataRow rk = AbweichungsErmittler.KomponenteZeile(d, gewerk, k);
                            namen.Add(rk == null || bez == null ? OHNE_WERT
                                                                : AbweichungsErmittler.Formatiere(rk, bez));
                        }
                        string label = maxKomp == 1
                            ? TUeb("BK_SP_KOMPONENTE", "Komponente")
                            : string.Format(TUeb("BK_SP_KOMPONENTE_N", "Komponente {0}"), k + 1);
                        DataGridViewRow zk = SchreibeVergleichsZeile("", label, namen);
                        zk.Tag = new KomponentenZelle { Gewerk = gewerk, Index = k };
                        for (int c = 0; c < versionen.Count; c++)
                        {
                            DataRow rk = AbweichungsErmittler.KomponenteZeile(versionen[c], gewerk, k);
                            if (rk != null)
                                zk.Cells[2 + c].ToolTipText =
                                    AbweichungsErmittler.MerkmaleText(rk, gewerk, "\r\n");
                        }
                        zeilen++;
                    }
                    continue;   // Merkmalszeilen entfallen für zählbare Gewerke
                }

                foreach (AbweichungsErmittler.Merkmal f in felder)
                {
                    var werte = new List<string>();
                    bool belegt = false;
                    foreach (ProjektDetails d in versionen)
                    {
                        DataRow r = AbweichungsErmittler.ZeileFuer(d, f);
                        werte.Add(r == null ? OHNE_WERT : AbweichungsErmittler.Formatiere(r, f));
                        if (r != null) belegt = true;
                    }
                    if (!belegt) continue;   // Merkmal in keiner Version belegt

                    SchreibeVergleichsZeile(ersteZeile ? gewerk : "", f.Label, werte);
                    ersteZeile = false;
                    zeilen++;
                }
            }
            return zeilen;
        }

        // Trägt eine Zeile ein: das Gewerk nur in der ersten Zeile seines Blocks (fett),
        // Zellen ohne Bestand grau - der Strich allein wäre in einer Zahlenspalte leicht
        // als Zahl zu übersehen.
        private DataGridViewRow SchreibeVergleichsZeile(string gewerk, string merkmal, List<string> werte)
        {
            var zellen = new List<object> { gewerk, merkmal };
            foreach (string w in werte) zellen.Add(w);

            DataGridViewRow zeile = gridKomp.Rows[gridKomp.Rows.Add(zellen.ToArray())];
            if (gewerk.Length > 0)
                zeile.Cells[0].Style.Font = new Font(gridKomp.Font, FontStyle.Bold);

            for (int c = 0; c < werte.Count; c++)
                if (werte[c] == OHNE_WERT || werte[c] == AbweichungsErmittler.BESTAND_FEHLT)
                    zeile.Cells[2 + c].Style.ForeColor = SystemColors.GrayText;
            return zeile;
        }

        /// <summary>
        /// Nutzerauftrag 28.08.2026 — „Anzeige bei Auswahl": Die Merkmale der in der
        /// Gegenüberstellung gewählten Komponente erscheinen in der Statuszeile
        /// (Hersteller, Typ, Leistung, … der Zelle unter dem Cursor); das Mouse-over
        /// der Zelle zeigt dieselben Merkmale mehrzeilig als Tooltip.
        /// </summary>
        private void gridKomp_KomponentenAuswahl(object sender, EventArgs e)
        {
            if (_vergleichsVersionen == null || gridKomp.CurrentCell == null) return;
            var kz = gridKomp.CurrentCell.OwningRow?.Tag as KomponentenZelle;
            if (kz == null) return;

            int v = gridKomp.CurrentCell.ColumnIndex - 2;
            if (v < 0 || v >= _vergleichsVersionen.Count) v = 0;
            DataRow r = AbweichungsErmittler.KomponenteZeile(_vergleichsVersionen[v], kz.Gewerk, kz.Index);
            if (r == null) return;

            AbweichungsErmittler.Merkmal bez = AbweichungsErmittler.BezeichnerMerkmal(kz.Gewerk);
            string name = bez != null ? AbweichungsErmittler.Formatiere(r, bez) : kz.Gewerk;
            string kopf = (_vergleichsKoepfe != null && v < _vergleichsKoepfe.Count) ? _vergleichsKoepfe[v] : "";
            string merkmale = AbweichungsErmittler.MerkmaleText(r, kz.Gewerk, "  ·  ");
            Melde(name + (kopf.Length > 0 ? "  (" + kopf + ")" : "") +
                  (merkmale.Length > 0 ? "  —  " + merkmale : ""));
        }

        // Unterschiede der Variante gegenüber dem Stamm (vorhandene Diff-Logik).
        private void ZeigeUnterschiede(ProjektEintrag stamm, AuswahlZeile z, ProjektDetails ds)
        {
            lblKomponenten.Text = string.Format(MyResource.Resource.BK_LBL_KOMPONENTEN_DIFF,
                string.IsNullOrEmpty(z.Variantenname) ? z.Projektname : z.Variantenname);

            // Feste Spaltenzahl - hier füllt die Tabelle die Breite wie eh und je aus
            // (die Gegenüberstellung kann den Modus auf „None" gestellt haben).
            gridKomp.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridKomp.Columns.Add("gewerk", MyResource.Resource.BK_SP_GEWERK);
            gridKomp.Columns.Add("merkmal", MyResource.Resource.BK_SP_MERKMAL);
            gridKomp.Columns.Add("stamm", MyResource.Resource.BK_SP_WERT_STAMM);
            gridKomp.Columns.Add("variante", MyResource.Resource.BK_SP_WERT_VARIANTE);
            gridKomp.Columns[0].FillWeight = 80;
            gridKomp.Columns[1].FillWeight = 130;
            gridKomp.Columns[2].FillWeight = 100;
            gridKomp.Columns[3].FillWeight = 100;
            gridKomp.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridKomp.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Aktionsspalte: je Zeile ein Knopf (übernehmbar) oder ein Strich (gesperrt).
            var spAktion = new DataGridViewButtonColumn
            {
                Name = SPALTE_AKTION,
                HeaderText = MyResource.Resource.BK_SP_AKTION,
                Text = MyResource.Resource.BK_BTN_UEBERNEHMEN,
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Popup,
                FillWeight = 90
            };
            spAktion.DefaultCellStyle.ForeColor = SystemColors.GrayText;
            gridKomp.Columns.Add(spAktion);

            ProjektDetails dv = Details(stamm.Id, z.IdProjekt);
            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(ds, dv);

            if (liste.Count == 0)
            {
                Melde(MyResource.Resource.BK_MSG_KEINE_ABWEICHUNG);
                return;
            }

            foreach (Abweichung a in liste)
            {
                int idx = gridKomp.Rows.Add(a.Gewerk, a.Merkmal, a.WertStamm, a.WertVariante);
                UebernahmeZeile uz = BaueUebernahmeZeile(stamm.Id, z.IdProjekt, a);
                gridKomp.Rows[idx].Tag = uz;
                SetzeAktionszelle(gridKomp.Rows[idx], uz);
            }
            gridKomp.ClearSelection();
            Melde(string.Format(MyResource.Resource.BK_MSG_ANZAHL_UNTERSCHIEDE, liste.Count));
        }

        /// <summary>Spaltenname der (noch wirkungslosen) Aktionsspalte — Schlüssel, kein Anzeigetext.</summary>
        public const string SPALTE_AKTION = "AKTION_UEBERNEHMEN";

        // Ordnet einer Abweichung ihr Merkmal aus der deklarativen Feldliste zu und
        // baut daraus den Zeilendatensatz für das Folgepaket.
        private static UebernahmeZeile BaueUebernahmeZeile(int idStamm, int idVariante, Abweichung a)
        {
            var uz = new UebernahmeZeile
            {
                IdStamm = idStamm,
                IdVariante = idVariante,
                Gewerk = a.Gewerk,
                Merkmal = a.Merkmal,
                WertStamm = a.WertStamm,
                WertVariante = a.WertVariante
            };
            AbweichungsErmittler.Merkmal f = AbweichungsErmittler.Felder
                .FirstOrDefault(x => x.Gewerk == a.Gewerk && x.Label == a.Merkmal);
            if (f != null) { uz.Feld = f; uz.Tabelle = f.Tabelle; uz.Spalte = f.Spalte; uz.Dez = f.Dez; }
            return uz;
        }

        // ------------------------------------------------------------- Übernahme

        /// <summary>
        /// Stellt die Aktionszelle einer Unterschiedszeile ein: Knopf mit Kurzhinweis,
        /// wenn die Übernahme trägt — sonst ein grauer Strich mit der Begründung. Ein
        /// Knopf, der beim Drücken nur erklärt, warum er nichts tut, wäre die schlechtere
        /// Auskunft.
        /// </summary>
        private static void SetzeAktionszelle(DataGridViewRow zeile, UebernahmeZeile uz)
        {
            string sperre = Sperrgrund(uz);
            if (sperre == null)
            {
                DataGridViewCell knopf = zeile.Cells[SPALTE_AKTION];
                knopf.Style.ForeColor = SystemColors.ControlText;
                knopf.ToolTipText = uz.Feld != null
                    ? MyResource.Resource.BK_TIP_UEBERNEHMEN_FELD
                    : MyResource.Resource.BK_TIP_UEBERNEHMEN_KOMP;
                return;
            }

            var strich = new DataGridViewTextBoxCell { Value = "—" };
            strich.Style.ForeColor = SystemColors.GrayText;
            strich.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            zeile.Cells[SPALTE_AKTION] = strich;
            strich.ToolTipText = sperre;
        }

        /// <summary>Grund, warum diese Zeile nicht übernommen werden kann (null = sie kann).</summary>
        private static string Sperrgrund(UebernahmeZeile uz)
        {
            if (uz == null) return MyResource.Resource.BK_MSG_UEB_KEIN_FELD;

            // Stufe 1: ganzer Komponentenbestand eines Gewerks.
            if (uz.Feld == null || string.IsNullOrEmpty(uz.Tabelle) || string.IsNullOrEmpty(uz.Spalte))
                return KomponentenUebernahmeCtrl.Unterstuetzt(uz.Gewerk)
                    ? null
                    : string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, uz.Gewerk);

            // Stufe 3: der Bezeichner ist der Schlüssel der Zuordnung selbst.
            if (MerkmalUebernahmeCtrl.IstSchluesselspalte(uz.Spalte))
                return MyResource.Resource.BK_TIP_UEBERNEHMEN_GESPERRT_SCHLUESSEL;

            return null;
        }

        // Die wählbaren Quellen: Stammprojekt (Vorgabe, erste Zeile von LadeGruppe) und
        // jede andere Variante derselben Gruppe - das Ziel selbst natürlich nicht.
        private List<Form_BkUebernahme.Quelle> BaueQuellen(int idStamm, int idZiel)
        {
            var liste = new List<Form_BkUebernahme.Quelle>();
            ProjektEintrag stamm = AktuellerStamm;
            foreach (VariantenCtrl.VarianteInfo vi in
                     _ctrl.LadeGruppe(idStamm, stamm != null ? stamm.Name : ""))
            {
                if (vi.IdProjekt == idZiel) continue;
                liste.Add(new Form_BkUebernahme.Quelle
                {
                    Id = vi.IdProjekt,
                    Anzeige = vi.IstStamm
                        ? string.Format(MyResource.Resource.BK_UEB_QUELLE_STAMM, vi.Projektname)
                        : string.Format(MyResource.Resource.BK_UEB_QUELLE_VARIANTE, vi.Variantenname)
                });
            }
            return liste;
        }

        private string ZielName()
        {
            AuswahlZeile z = AktuelleZeile;
            if (z == null) return "";
            return string.IsNullOrEmpty(z.Variantenname) ? z.Projektname : z.Variantenname;
        }

        // --- A) Merkmals-Übernahme (Stufe-3-Zeile) --------------------------------

        private void MerkmalUebernahme(UebernahmeZeile uz)
        {
            AbweichungsErmittler.Merkmal f = uz.Feld;
            if (f == null) { Melde(MyResource.Resource.BK_MSG_UEB_KEIN_FELD); return; }

            List<Form_BkUebernahme.Quelle> quellen = BaueQuellen(uz.IdStamm, uz.IdVariante);
            if (quellen.Count == 0) { Melde(MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE); return; }

            using (var dlg = new Form_BkUebernahme(
                       MyResource.Resource.BK_UEB_TITEL_FELD,
                       uz.Gewerk + " · " + uz.Merkmal,
                       ZielName(), quellen,
                       id => VorschauFeld(id, uz.IdVariante, f), false))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    Cursor = Cursors.WaitCursor;

                    MerkmalUebernahmeCtrl.Befund b =
                        MerkmalUebernahmeCtrl.Pruefe(dlg.GewaehlteQuelleId, uz.IdVariante, f);

                    string fehler;
                    if (!MerkmalUebernahmeCtrl.Schreibe(b, uz.IdVariante, f, out fehler))
                    { Melde(string.Format(MyResource.Resource.BK_MSG_UEB_FEHLER, fehler ?? "")); return; }

                    NachSchreibvorgang(uz.IdVariante, string.Format(
                        MyResource.Resource.BK_MSG_UEB_OK, uz.Gewerk, uz.Merkmal, b.Quelle.Anzeigewert));
                }
                catch (Exception ex)
                { Melde(string.Format(MyResource.Resource.BK_MSG_UEB_FEHLER, ex.Message)); }
                finally { Cursor = Cursors.Default; }
            }
        }

        private static Form_BkUebernahme.Vorschau VorschauFeld(int idQuelle, int idZiel,
                                                               AbweichungsErmittler.Merkmal f)
        {
            MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(idQuelle, idZiel, f);
            var v = new Form_BkUebernahme.Vorschau
            {
                Moeglich = b.Moeglich && !b.Gleichstand,
                Grund = b.Moeglich
                    ? (b.Gleichstand ? MyResource.Resource.BK_MSG_UEB_GLEICH : "")
                    : b.Grund,
                WertQuelle = b.Quelle.Anzeigewert,
                WertZiel = b.Ziel.Anzeigewert
            };

            // Quelle und Ziel können unterschiedliche Komponenten sein - der Dialog nennt
            // sie, statt die Übernahme stillschweigend darüber hinweg zu schreiben.
            if (b.Quelle.Bezeichner.Length > 0 || b.Ziel.Bezeichner.Length > 0)
                v.Komponenten = string.Format(MyResource.Resource.BK_UEB_KOMPONENTEN,
                                              Oder(b.Quelle.Bezeichner), Oder(b.Ziel.Bezeichner));
            return v;
        }

        private static string Oder(string s) { return string.IsNullOrEmpty(s) ? "—" : s; }

        // --- B) Komponenten-Übernahme (Stufe-1-Zeile) ------------------------------

        private void KomponentenUebernahme(UebernahmeZeile uz)
        {
            if (!KomponentenUebernahmeCtrl.Unterstuetzt(uz.Gewerk))
            { Melde(string.Format(MyResource.Resource.BK_MSG_KOMP_GEWERK_UNBEKANNT, uz.Gewerk)); return; }

            List<Form_BkUebernahme.Quelle> quellen = BaueQuellen(uz.IdStamm, uz.IdVariante);
            if (quellen.Count == 0) { Melde(MyResource.Resource.BK_MSG_UEB_KEINE_QUELLE); return; }

            var ctrl = new KomponentenUebernahmeCtrl();

            using (var dlg = new Form_BkUebernahme(
                       MyResource.Resource.BK_UEB_TITEL_KOMP,
                       uz.Gewerk,
                       ZielName(), quellen,
                       id => VorschauKomponenten(ctrl, id, uz.IdVariante, uz.Gewerk), true))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    Cursor = Cursors.WaitCursor;

                    int idQuelle = dlg.GewaehlteQuelleId;
                    KomponentenUebernahmeCtrl.Vorschau v = ctrl.Planen(idQuelle, uz.IdVariante, uz.Gewerk);

                    string fehler, hinweise;
                    if (!ctrl.Uebernehmen(idQuelle, uz.IdVariante, uz.Gewerk, out fehler, out hinweise))
                    { Melde(string.Format(MyResource.Resource.BK_MSG_KOMP_FEHLER, fehler ?? "")); return; }

                    if (!string.IsNullOrEmpty(hinweise))
                        MessageBox.Show(hinweise, MyResource.Resource.BK_UEB_TITEL_KOMP,
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    NachSchreibvorgang(uz.IdVariante, string.Format(
                        MyResource.Resource.BK_MSG_KOMP_OK, uz.Gewerk,
                        v.Anlegen.Count, v.Gleichziehen.Count, v.Entfernen.Count));
                }
                catch (Exception ex)
                { Melde(string.Format(MyResource.Resource.BK_MSG_KOMP_FEHLER, ex.Message)); }
                finally { Cursor = Cursors.Default; }
            }
        }

        private static Form_BkUebernahme.Vorschau VorschauKomponenten(KomponentenUebernahmeCtrl ctrl,
                                                                      int idQuelle, int idZiel, string gewerk)
        {
            KomponentenUebernahmeCtrl.Vorschau p = ctrl.Planen(idQuelle, idZiel, gewerk);
            return new Form_BkUebernahme.Vorschau
            {
                Moeglich = p.Moeglich,
                Grund = p.Grund,
                Klartext = p.Klartext
            };
        }

        // --- gemeinsamer Abschluss ------------------------------------------------

        /// <summary>
        /// Nach jedem Schreibvorgang: zwischengespeicherte Stammdaten verwerfen, Liste und
        /// Unterschiede neu aufbauen (dabei wird auch der Simulationsstand ⚠ neu gelesen —
        /// die Übernahme hat das Änderungsdatum des Zielprojekts gesetzt) und melden.
        /// </summary>
        private void NachSchreibvorgang(int idZiel, string meldung)
        {
            VerwirfDetails();
            _markiereVarianteId = idZiel;      // dieselbe Zeile bleibt markiert
            LadeAuswahl();

            if (MerkmalUebernahmeCtrl.HatErgebnisse(idZiel))
                meldung += "  " + MyResource.Resource.BK_MSG_UEB_ERGEBNIS_VERALTET;
            Melde(meldung);
        }

        // Gewerke in der Reihenfolge der Feldliste (Anlage, Erzeuger …, Gebäude).
        private static IEnumerable<string> GewerkeInReihenfolge()
        {
            var gesehen = new List<string>();
            foreach (AbweichungsErmittler.Merkmal f in AbweichungsErmittler.Felder)
                if (!gesehen.Contains(f.Gewerk)) { gesehen.Add(f.Gewerk); }
            return gesehen;
        }

        private void gridKomp_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (gridKomp.Columns[e.ColumnIndex].Name != SPALTE_AKTION) return;

            UebernahmeZeile uz = gridKomp.Rows[e.RowIndex].Tag as UebernahmeZeile;
            if (uz == null) return;

            string sperre = Sperrgrund(uz);
            if (sperre != null) { Melde(sperre); return; }

            // Stufe 1 („Bestand", „Anzahl Komponenten") kennt kein einzelnes Feld —
            // dort geht es um den ganzen Komponentenbestand des Gewerks.
            if (uz.Feld == null) KomponentenUebernahme(uz);
            else MerkmalUebernahme(uz);
        }

        /// <summary>
        /// Löst die Aktion einer Unterschiedszeile ohne Mausklick aus (Prüfhilfe für den
        /// headless-Test der Oberfläche). Liefert false, wenn die Zeile gesperrt ist.
        /// </summary>
        public bool LoeseAktionAus(int zeilenIndex)
        {
            if (zeilenIndex < 0 || zeilenIndex >= gridKomp.Rows.Count) return false;
            UebernahmeZeile uz = gridKomp.Rows[zeilenIndex].Tag as UebernahmeZeile;
            if (uz == null || Sperrgrund(uz) != null) return false;

            if (uz.Feld == null) KomponentenUebernahme(uz);
            else MerkmalUebernahme(uz);
            return true;
        }

        /// <summary>Zeilendatensatz einer Unterschiedszeile (Prüfhilfe).</summary>
        public UebernahmeZeile Unterschiedszeile(int zeilenIndex)
        {
            return (zeilenIndex >= 0 && zeilenIndex < gridKomp.Rows.Count)
                ? gridKomp.Rows[zeilenIndex].Tag as UebernahmeZeile : null;
        }

        /// <summary>Ist die Aktionszelle dieser Zeile ein Knopf (true) oder gesperrt (false)?</summary>
        public bool AktionFreigegeben(int zeilenIndex)
        {
            if (zeilenIndex < 0 || zeilenIndex >= gridKomp.Rows.Count) return false;
            return gridKomp.Rows[zeilenIndex].Cells[SPALTE_AKTION] is DataGridViewButtonCell;
        }

        /// <summary>Zeilendatensatz der markierten Unterschiedszeile (null = keine).</summary>
        public UebernahmeZeile MarkierteUnterschiedszeile
        {
            get
            {
                return gridKomp.CurrentRow != null ? gridKomp.CurrentRow.Tag as UebernahmeZeile : null;
            }
        }

        /// <summary>Anzahl der aktuell angezeigten Zeilen im Komponentenbereich (Prüfhilfe).</summary>
        public int KomponentenZeilen { get { return gridKomp.Rows.Count; } }

        /// <summary>Überschrift des Komponentenbereichs (Prüfhilfe).</summary>
        public string KomponentenTitel { get { return lblKomponenten.Text; } }

        // -------------------------------------------------------------- Helfer

        private void Melde(string text)
        {
            if (lblStatus != null) lblStatus.Text = text ?? "";
        }

        /// <summary>Statustext der Seite (Prüfhilfe).</summary>
        public string StatusText { get { return lblStatus != null ? lblStatus.Text : ""; } }

        // -------------------------------------------------- kleine Hilfsklassen

        /// <summary>Ein Eintrag der Stammprojekt-Auswahl.</summary>
        public class ProjektEintrag
        {
            public int Id { get; private set; }
            public string Name { get; private set; }
            public ProjektEintrag(int id, string name) { Id = id; Name = name; }
            public override string ToString() { return Name; }
        }

        /// <summary>Eine Zeile der Stamm-/Variantenliste.</summary>
        public class AuswahlZeile
        {
            public int IdProjekt { get; private set; }
            public string Projektname { get; private set; }
            public string Variantenname { get; private set; }
            public bool IstStamm { get; private set; }
            public AuswahlZeile(int idProjekt, string projektname, string variantenname, bool istStamm)
            {
                IdProjekt = idProjekt; Projektname = projektname;
                Variantenname = variantenname; IstStamm = istStamm;
            }
        }
    }
}
