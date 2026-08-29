using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wird ausgelöst, wenn ein Projekt gewählt wurde (Doppelklick oder OK der Hüllform).
    /// </summary>
    /// <param name="id">Tab_Projekt.ID des gewählten Projekts.</param>
    /// <param name="name">Projektname des gewählten Projekts.</param>
    public delegate void ProjektGewaehltHandler(int id, string name);

    /// <summary>
    /// Die EINE Projektliste der Anwendung (Konzept „Projektdialoge vereinheitlichen",
    /// Paket P3): Liste aller Projekte mit Projektname, Kunde und Änderungsdatum,
    /// Suchfeld, Sortierung per Spaltenklick, Doppelklick = Auswahl.
    ///
    /// <para>
    /// <b>Datenweg.</b> Ausschließlich über den bestehenden
    /// <see cref="ProjektCtrl"/>.<c>ReadAll()</c> (SELECT * FROM Tab_Projekt) — kein
    /// eigenes SQL. Der Bestand wird EINMAL gelesen und danach nur noch örtlich
    /// gefiltert und sortiert; die Liste ist damit auch bei vielen Projekten flüssig
    /// und die Datenbank wird beim Tippen nicht angefasst.
    /// </para>
    /// <para>
    /// <b>Warum ein UserControl und kein Dialog.</b> Dieselbe Liste wird an mehreren
    /// Stellen gebraucht: Menü „Projekt → Öffnen…", Kachel „Zuletzt geöffnet" und
    /// (Paket P4) die linke Spalte des Assistenten im Bearbeiten-Modus. Die schlanke
    /// Hüllform <see cref="Form_ProjektAuswahl"/> bettet sie nur ein.
    /// </para>
    /// </summary>
    [ToolboxItem(true)]
    [Description("Projektliste mit Suche, Sortierung und Doppelklick-Auswahl.")]
    [DefaultEvent("ProjektGewaehlt")]
    public partial class ProjektAuswahl : UserControl
    {
        /// <summary>Spaltennummer „Projektname".</summary>
        public const int SPALTE_NAME = 0;

        /// <summary>Spaltennummer „Kunde".</summary>
        public const int SPALTE_KUNDE = 1;

        /// <summary>Spaltennummer „Geändert".</summary>
        public const int SPALTE_GEAENDERT = 2;

        private readonly List<ProjektModel> _bestand = new List<ProjektModel>();
        private int _sortSpalte = SPALTE_NAME;
        private bool _sortAbsteigend;
        private bool _geladen;

        // Zuletzt gesetzte Markierung — siehe MarkierungUebernehmen.
        private ListViewItem _markiert;

        // Format der Zählzeile. Er steht als Entwurfstext von label_Anzahl in den
        // drei .resx (z. B. "{0} von {1} Projekten" / "{0} of {1} projects") und
        // wird hier EINMAL abgeholt — so bleibt der Satzbau übersetzbar, ohne dass
        // ein weiterer MyResource-Schlüssel nötig wird.
        private readonly string _anzahlFormat;

        /// <summary>Ein Projekt wurde gewählt (Doppelklick in der Liste).</summary>
        [Category("Aktion")]
        [Description("Ein Projekt wurde gewählt (Doppelklick in der Liste).")]
        public event ProjektGewaehltHandler ProjektGewaehlt;

        /// <summary>Die Auswahl wurde abgebrochen.</summary>
        [Category("Aktion")]
        [Description("Die Auswahl wurde abgebrochen.")]
        public event EventHandler Abgebrochen;

        public ProjektAuswahl()
        {
            InitializeComponent();
            _anzahlFormat = string.IsNullOrEmpty(label_Anzahl.Text) ? "{0} / {1}" : label_Anzahl.Text;
            label_Anzahl.Text = "";
        }

        // ------------------------------------------------------------------
        //  Ergebnis
        // ------------------------------------------------------------------

        /// <summary>Tab_Projekt.ID des markierten Projekts; 0, wenn nichts markiert ist.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int GewaehlteID { get; private set; }

        /// <summary>Projektname des markierten Projekts; leer, wenn nichts markiert ist.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string GewaehlterName { get; private set; } = "";

        /// <summary>Anzahl der Projekte im gelesenen Bestand (ungefiltert).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Anzahl
        {
            get { return _bestand.Count; }
        }

        // ------------------------------------------------------------------
        //  Laden, Filtern, Sortieren
        // ------------------------------------------------------------------

        /// <summary>
        /// Liest den Projektbestand über <see cref="ProjektCtrl"/> und baut die Liste auf.
        /// Mehrfachaufrufe lesen erneut — der Aufrufer entscheidet, wann es sich lohnt.
        /// </summary>
        public void Laden()
        {
            _bestand.Clear();
            try
            {
                ProjektCtrl ctrl = new ProjektCtrl();
                ctrl.ReadAll();
                for (int i = 0; i < ctrl.rows; i++) _bestand.Add(ctrl.items[i]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Projektliste konnte nicht gelesen werden: " + ex.Message);
            }

            _geladen = true;
            ListeAufbauen();
        }

        /// <summary>
        /// Legt die Sortierung fest (Spaltennummer <see cref="SPALTE_NAME"/> …
        /// <see cref="SPALTE_GEAENDERT"/>) und baut die Liste neu auf.
        /// </summary>
        public void SortiereNach(int spalte, bool absteigend)
        {
            _sortSpalte = spalte;
            _sortAbsteigend = absteigend;
            if (_geladen) ListeAufbauen();
        }

        /// <summary>
        /// Markiert das Projekt mit diesem Namen, sofern es in der aktuellen Sicht steht.
        /// </summary>
        public void Vorauswaehlen(string projektname)
        {
            if (string.IsNullOrEmpty(projektname)) return;
            foreach (ListViewItem it in listView_Projekte.Items)
            {
                if (!string.Equals(it.Text, projektname, StringComparison.CurrentCultureIgnoreCase)) continue;
                it.Selected = true;
                it.Focused = true;
                it.EnsureVisible();
                // Markierung ausdruecklich uebernehmen: SelectedIndexChanged kommt aus
                // der nativen Liste und bleibt aus, solange sie kein Fensterhandle hat
                // (Vorauswahl im Load, bevor der Dialog sichtbar ist).
                _markiert = it;
                MarkierungUebernehmen();
                return;
            }
        }

        /// <summary>Setzt den Tastaturfokus auf das Suchfeld.</summary>
        public void SuchfeldFokussieren()
        {
            textBox_Suche.Focus();
        }

        /// <summary>Meldet den Abbruch nach außen (Aufruf aus der Hüllform).</summary>
        public void Abbrechen()
        {
            EventHandler h = Abgebrochen;
            if (h != null) h(this, EventArgs.Empty);
        }

        /// <summary>
        /// Meldet die aktuelle Markierung nach außen (Aufruf aus der Hüllform, „OK").
        /// </summary>
        /// <returns>false, wenn nichts markiert ist.</returns>
        public bool AuswahlMelden()
        {
            MarkierungUebernehmen();
            if (GewaehlteID <= 0) return false;
            ProjektGewaehltHandler h = ProjektGewaehlt;
            if (h != null) h(GewaehlteID, GewaehlterName);
            return true;
        }

        private void ListeAufbauen()
        {
            string suche = (textBox_Suche.Text ?? "").Trim();

            List<ProjektModel> sicht = new List<ProjektModel>();
            foreach (ProjektModel p in _bestand)
                if (Passt(p, suche)) sicht.Add(p);

            sicht.Sort(Vergleiche);

            string vorher = GewaehlterName;
            _markiert = null;

            listView_Projekte.BeginUpdate();
            listView_Projekte.Items.Clear();
            foreach (ProjektModel p in sicht)
            {
                ListViewItem it = new ListViewItem(p.m_szProjektname ?? "");
                it.SubItems.Add(p.m_szKunde ?? "");
                it.SubItems.Add(p.m_Aenderungsdatum.ToShortDateString());
                it.Tag = p;
                listView_Projekte.Items.Add(it);
            }
            listView_Projekte.EndUpdate();

            label_Anzahl.Text = string.Format(_anzahlFormat, sicht.Count, _bestand.Count);

            GewaehlteID = 0;
            GewaehlterName = "";
            if (!string.IsNullOrEmpty(vorher)) Vorauswaehlen(vorher);
            if (GewaehlteID == 0 && listView_Projekte.Items.Count > 0)
            {
                listView_Projekte.Items[0].Selected = true;
                _markiert = listView_Projekte.Items[0];
                MarkierungUebernehmen();
            }
        }

        private static bool Passt(ProjektModel p, string suche)
        {
            if (suche.Length == 0) return true;
            return Enthaelt(p.m_szProjektname, suche)
                || Enthaelt(p.m_szKunde, suche)
                || Enthaelt(p.m_szBeschreibung, suche);
        }

        private static bool Enthaelt(string wert, string suche)
        {
            return !string.IsNullOrEmpty(wert)
                && wert.IndexOf(suche, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private int Vergleiche(ProjektModel a, ProjektModel b)
        {
            int v;
            switch (_sortSpalte)
            {
                case SPALTE_KUNDE:
                    v = string.Compare(a.m_szKunde ?? "", b.m_szKunde ?? "", StringComparison.CurrentCultureIgnoreCase);
                    break;
                case SPALTE_GEAENDERT:
                    v = DateTime.Compare(a.m_Aenderungsdatum, b.m_Aenderungsdatum);
                    break;
                default:
                    v = string.Compare(a.m_szProjektname ?? "", b.m_szProjektname ?? "", StringComparison.CurrentCultureIgnoreCase);
                    break;
            }

            // Gleichstand immer über den Namen auflösen, damit die Reihenfolge
            // bei gleichem Datum (Massenimport!) nicht bei jedem Aufbau springt.
            if (v == 0 && _sortSpalte != SPALTE_NAME)
                v = string.Compare(a.m_szProjektname ?? "", b.m_szProjektname ?? "", StringComparison.CurrentCultureIgnoreCase);

            return _sortAbsteigend ? -v : v;
        }

        // ------------------------------------------------------------------
        //  Ereignisse der Steuerelemente
        // ------------------------------------------------------------------

        private void textBox_Suche_TextChanged(object sender, EventArgs e)
        {
            if (_geladen) ListeAufbauen();
        }

        private void listView_Projekte_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Gleiche Spalte erneut = Richtung umkehren, andere Spalte = aufsteigend
            // beginnen; beim Datum ist absteigend („zuletzt geändert zuerst") die
            // sinnvollere erste Richtung.
            if (e.Column == _sortSpalte) _sortAbsteigend = !_sortAbsteigend;
            else
            {
                _sortSpalte = e.Column;
                _sortAbsteigend = e.Column == SPALTE_GEAENDERT;
            }
            ListeAufbauen();
        }

        private void listView_Projekte_SelectedIndexChanged(object sender, EventArgs e)
        {
            MarkierungUebernehmen();
        }

        /// <summary>
        /// Liest die aktuelle Markierung der Liste in die Ergebnisfelder.
        ///
        /// <para>
        /// Die native Liste führt ihre Auswahlsammlung erst, wenn sie ein
        /// Fensterhandle hat. Wird — wie beim Öffnen des Dialogs — vorher
        /// vorausgewählt, bliebe <see cref="ListView.SelectedItems"/> leer und der
        /// OK-Knopf meldete „Bitte auswählen!" trotz sichtbarer Markierung. Deshalb
        /// merkt sich das Steuerelement die gesetzte Zeile zusätzlich selbst.
        /// </para>
        /// </summary>
        private void MarkierungUebernehmen()
        {
            GewaehlteID = 0;
            GewaehlterName = "";

            ListViewItem it = null;
            if (listView_Projekte.SelectedItems.Count > 0) it = listView_Projekte.SelectedItems[0];
            else if (_markiert != null && listView_Projekte.Items.Contains(_markiert)) it = _markiert;
            if (it == null) return;

            _markiert = it;
            ProjektModel p = it.Tag as ProjektModel;
            if (p == null) return;
            GewaehlteID = p.m_ID;
            GewaehlterName = p.m_szProjektname ?? "";
        }

        private void listView_Projekte_DoubleClick(object sender, EventArgs e)
        {
            AuswahlMelden();
        }
    }
}
