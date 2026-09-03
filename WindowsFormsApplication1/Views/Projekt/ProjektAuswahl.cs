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

        // Varianten-Verknüpfung (Tab_Variante: Projekt -> Stamm), einmal je Laden
        // gelesen. Trägt die Gruppierung „Stamm, darunter seine Varianten" und die
        // Stamm-Haken-Kopplung der Mehrfachauswahl (Nutzerauftrag 02.09.2026).
        private readonly Dictionary<int, int> _stammVon = new Dictionary<int, int>();

        // Mehrfachauswahl (Löschdialog): angehakte Projekt-IDs — unabhängig vom
        // Suchfilter, damit ein Haken beim Filtern nicht verloren geht.
        private readonly HashSet<int> _angehakt = new HashSet<int>();
        private bool _hakenIntern;   // Schutz gegen Rekursion beim Kopplungs-Haken

        // Anzeigepräfix einer Variante in der Namensspalte (unter ihrem Stamm).
        private const string VARIANTE_PRAEFIX = "   ↳ ";

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

        /// <summary>
        /// Die Markierung in der Liste hat gewechselt (einfacher Klick, Vorauswahl,
        /// Tastatur). Anders als <see cref="ProjektGewaehlt"/> ist das noch <b>keine</b>
        /// endgültige Auswahl — der Projektassistent hängt daran das Nachladen seiner
        /// Komponentenkacheln (P4).
        /// </summary>
        [Category("Aktion")]
        [Description("Die Markierung in der Liste hat gewechselt.")]
        public event ProjektGewaehltHandler MarkierungGeaendert;

        public ProjektAuswahl()
        {
            InitializeComponent();
            _anzahlFormat = string.IsNullOrEmpty(label_Anzahl.Text) ? "{0} / {1}" : label_Anzahl.Text;
            label_Anzahl.Text = "";

            // Nutzerauftrag 02.09.2026 (Handhabung): Tooltip mit den vollen Angaben je
            // Zeile, Tastaturweg Suchfeld -> Liste -> Enter, Haken-Kopplung der
            // Mehrfachauswahl. Verdrahtung hier statt im Designer, damit die
            // .resx-Layouts unangetastet bleiben.
            listView_Projekte.ShowItemToolTips = true;
            listView_Projekte.KeyDown += listView_Projekte_KeyDown;
            listView_Projekte.ItemChecked += listView_Projekte_ItemChecked;
            textBox_Suche.KeyDown += textBox_Suche_KeyDown;
        }

        // Ressourcen-Helfer mit deutschem Fallback (Drei-Schichten-Regel; die
        // generierten Resource-Eigenschaften entstehen erst im VS-Designer).
        private static string TPa(string key, string fallback)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch { return fallback; }
        }

        // ------------------------------------------------------------------
        //  Darstellung (im Eigenschaftenfenster des Designers pflegbar)
        // ------------------------------------------------------------------

        private bool _nurNamensspalte;
        private bool _automatischeVorauswahl = true;

        /// <summary>
        /// Schmale Sicht: nur die Spalte „Projektname", auf die volle Breite gezogen.
        /// Für die linke Spalte des Projektassistenten, die dort nur rund 270 Pixel
        /// breit ist — drei Spalten (220 + 150 + 120) passten nicht hinein.
        /// </summary>
        [Category("Darstellung")]
        [Description("Schmale Sicht: nur die Spalte Projektname, auf die volle Breite gezogen.")]
        [DefaultValue(false)]
        public bool NurNamensspalte
        {
            get { return _nurNamensspalte; }
            set
            {
                if (_nurNamensspalte == value) return;
                _nurNamensspalte = value;
                SpaltenAnpassen();
            }
        }

        /// <summary>
        /// true (Vorgabe): Beim Aufbau der Liste wird die erste Zeile markiert, damit
        /// der OK-Knopf eines Dialogs sofort etwas zu tun hat. Der Assistent setzt das
        /// auf false — dort darf „Weiter" erst wirken, wenn der Anwender ein Projekt
        /// ausdrücklich gewählt hat.
        /// </summary>
        [Category("Verhalten")]
        [Description("Markiert nach dem Aufbau der Liste automatisch die erste Zeile.")]
        [DefaultValue(true)]
        public bool AutomatischeVorauswahl
        {
            get { return _automatischeVorauswahl; }
            set { _automatischeVorauswahl = value; }
        }

        private bool _mehrfachAuswahl;

        /// <summary>
        /// Mehrfachauswahl per Häkchen (Löschdialog, Nutzerauftrag 02.09.2026): Die
        /// Liste führt Kontrollkästchen, <see cref="GewaehlteProjekte"/> liefert die
        /// angehakten Projekte. Ein angehaktes Stammprojekt hakt seine Varianten mit
        /// an (und umgekehrt ab) — eine Variante ohne Stamm gibt es nicht.
        /// </summary>
        [Category("Verhalten")]
        [Description("Mehrfachauswahl per Häkchen; angehakte Stämme nehmen ihre Varianten mit.")]
        [DefaultValue(false)]
        public bool MehrfachAuswahl
        {
            get { return _mehrfachAuswahl; }
            set
            {
                _mehrfachAuswahl = value;
                listView_Projekte.CheckBoxes = value;
                if (_geladen) ListeAufbauen();
            }
        }

        /// <summary>Die Auswahl (Häkchen) hat sich geändert — nur im Mehrfachmodus.</summary>
        [Category("Aktion")]
        [Description("Die Häkchen-Auswahl hat sich geändert (Mehrfachmodus).")]
        public event EventHandler AuswahlGeaendert;

        /// <summary>
        /// Die angehakten Projekte des Mehrfachmodus — Varianten VOR ihren Stämmen,
        /// damit ein Löschlauf die Verknüpfungen in der richtigen Reihenfolge räumt.
        /// Der Suchfilter spielt keine Rolle: Ein Haken bleibt gesetzt, auch wenn die
        /// Zeile gerade ausgeblendet ist.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<ProjektModel> GewaehlteProjekte
        {
            get
            {
                var varianten = new List<ProjektModel>();
                var staemme = new List<ProjektModel>();
                foreach (ProjektModel p in _bestand)
                {
                    if (!_angehakt.Contains(p.m_ID)) continue;
                    if (_stammVon.ContainsKey(p.m_ID)) varianten.Add(p); else staemme.Add(p);
                }
                varianten.AddRange(staemme);
                return varianten;
            }
        }

        /// <summary>Hakt alle SICHTBAREN Zeilen an bzw. ab (Mehrfachmodus).</summary>
        public void AlleSichtbaren(bool an)
        {
            if (!_mehrfachAuswahl) return;
            _hakenIntern = true;
            try
            {
                foreach (ListViewItem it in listView_Projekte.Items)
                {
                    ProjektModel p = it.Tag as ProjektModel;
                    if (p == null) continue;
                    it.Checked = an;
                    if (an) _angehakt.Add(p.m_ID); else _angehakt.Remove(p.m_ID);
                }
            }
            finally { _hakenIntern = false; }
            ZaehlzeileSchreiben();
            EventHandler h = AuswahlGeaendert;
            if (h != null) h(this, EventArgs.Empty);
        }

        private void SpaltenAnpassen()
        {
            if (!_nurNamensspalte)
            {
                // Dreispaltige Sicht (Öffnen-/Löschdialog): die Namensspalte nimmt die
                // Restbreite, statt rechts eine leere Spalte stehen zu lassen.
                int rest = listView_Projekte.ClientSize.Width - columnHeader_Kunde.Width
                           - columnHeader_Geaendert.Width - 4;
                if (rest > 220) columnHeader_Name.Width = rest;
                return;
            }
            int breite = listView_Projekte.ClientSize.Width - 4;
            if (breite < 40) return;
            columnHeader_Kunde.Width = 0;
            columnHeader_Geaendert.Width = 0;

            // Nutzerauftrag 02.09.2026: lange Namen nicht mehr kappen — die Spalte
            // wird so breit wie der längste Eintrag; übersteigt das die Sicht,
            // blättert die Liste waagerecht (die Details-Ansicht zeigt den Balken
            // von selbst). Bisher war die Spalte fest auf Sichtbreite gezogen.
            int noetig = 0;
            foreach (ListViewItem it in listView_Projekte.Items)
                noetig = Math.Max(noetig, TextRenderer.MeasureText(it.Text, listView_Projekte.Font).Width + 28);
            columnHeader_Name.Width = Math.Max(breite, noetig);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SpaltenAnpassen();
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

            // Varianten-Verknüpfung einmal je Laden (Tab_Variante: Projekt -> Stamm).
            _stammVon.Clear();
            try
            {
                System.Data.DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Projekt, ID_ProjektRef FROM " + VariantenCtrl.TAB_VARIANTE);
                if (dt != null)
                    foreach (System.Data.DataRow r in dt.Rows)
                        if (r[0] != DBNull.Value && r[1] != DBNull.Value)
                            _stammVon[Convert.ToInt32(r[0])] = Convert.ToInt32(r[1]);
            }
            catch { /* ohne Variantentabelle: flache Liste */ }

            _geladen = true;
            ListeAufbauen();
        }

        /// <summary>true, wenn das Projekt eine Variante (Kopie eines Stamms) ist.</summary>
        public bool IstVariante(int idProjekt) { return _stammVon.ContainsKey(idProjekt); }

        /// <summary>Die Varianten-Ids eines Stammprojekts (leer, wenn keine).</summary>
        public List<int> VariantenVon(int idStamm)
        {
            var liste = new List<int>();
            foreach (KeyValuePair<int, int> kv in _stammVon)
                if (kv.Value == idStamm) liste.Add(kv.Key);
            return liste;
        }

        // Namenssortierung als Gruppierung: jeder Stamm, direkt darunter seine
        // Varianten (eingerückt). Varianten, deren Stamm nicht in der Sicht steht
        // (weggefiltert oder gelöscht), bleiben an ihrer alphabetischen Stelle.
        private List<ProjektModel> Gruppiert(List<ProjektModel> sicht)
        {
            var inSicht = new Dictionary<int, ProjektModel>();
            foreach (ProjektModel p in sicht) inSicht[p.m_ID] = p;

            var ergebnis = new List<ProjektModel>();
            var erledigt = new HashSet<int>();
            foreach (ProjektModel p in sicht)
            {
                if (erledigt.Contains(p.m_ID)) continue;
                int stamm;
                if (_stammVon.TryGetValue(p.m_ID, out stamm) && inSicht.ContainsKey(stamm))
                    continue;   // kommt unter seinem Stamm an die Reihe
                ergebnis.Add(p); erledigt.Add(p.m_ID);
                foreach (ProjektModel v in sicht)
                {
                    int s;
                    if (!erledigt.Contains(v.m_ID) && _stammVon.TryGetValue(v.m_ID, out s) && s == p.m_ID)
                    { ergebnis.Add(v); erledigt.Add(v.m_ID); }
                }
            }
            return ergebnis;
        }

        private void ZaehlzeileSchreiben()
        {
            string text = string.Format(_anzahlFormat, listView_Projekte.Items.Count, _bestand.Count);
            if (_mehrfachAuswahl)
                text += "  ·  " + string.Format(TPa("PA_AUSGEWAEHLT", "{0} ausgewählt"), _angehakt.Count);
            label_Anzahl.Text = text;
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
                // Über den Datensatz statt über den Zeilentext: Varianten tragen in
                // der Namensspalte ein Einrückungspräfix.
                ProjektModel pm = it.Tag as ProjektModel;
                string name = pm != null ? (pm.m_szProjektname ?? "") : it.Text;
                if (!string.Equals(name, projektname, StringComparison.CurrentCultureIgnoreCase)) continue;
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
            bool gruppiert = _sortSpalte == SPALTE_NAME && _stammVon.Count > 0;
            if (gruppiert) sicht = Gruppiert(sicht);

            string vorher = GewaehlterName;
            int vorherID = GewaehlteID;
            _markiert = null;

            _hakenIntern = true;
            listView_Projekte.BeginUpdate();
            listView_Projekte.Items.Clear();
            foreach (ProjektModel p in sicht)
            {
                string name = p.m_szProjektname ?? "";
                bool variante = _stammVon.ContainsKey(p.m_ID);
                ListViewItem it = new ListViewItem((gruppiert && variante ? VARIANTE_PRAEFIX : "") + name);
                it.SubItems.Add(p.m_szKunde ?? "");
                it.SubItems.Add(p.m_Aenderungsdatum.ToShortDateString());
                it.Tag = p;

                // Mouse-over: die vollen Angaben, auch wenn die Spalte schmal ist.
                var tip = new System.Text.StringBuilder(name);
                if (!string.IsNullOrEmpty(p.m_szKunde))
                    tip.Append("\r\n").Append(columnHeader_Kunde.Text).Append(": ").Append(p.m_szKunde);
                tip.Append("\r\n").Append(columnHeader_Geaendert.Text).Append(": ")
                   .Append(p.m_Aenderungsdatum.ToShortDateString());
                if (variante)
                {
                    string stammName = "";
                    foreach (ProjektModel s in _bestand)
                        if (s.m_ID == _stammVon[p.m_ID]) { stammName = s.m_szProjektname ?? ""; break; }
                    if (stammName.Length > 0)
                        tip.Append("\r\n").Append(string.Format(TPa("PA_VARIANTE_VON", "Variante von: {0}"), stammName));
                }
                it.ToolTipText = tip.ToString();

                if (_mehrfachAuswahl) it.Checked = _angehakt.Contains(p.m_ID);
                listView_Projekte.Items.Add(it);
            }
            listView_Projekte.EndUpdate();
            _hakenIntern = false;

            ZaehlzeileSchreiben();

            GewaehlteID = 0;
            GewaehlterName = "";
            SpaltenAnpassen();
            if (!string.IsNullOrEmpty(vorher)) Vorauswaehlen(vorher);
            if (_automatischeVorauswahl && GewaehlteID == 0 && listView_Projekte.Items.Count > 0)
            {
                listView_Projekte.Items[0].Selected = true;
                _markiert = listView_Projekte.Items[0];
                MarkierungUebernehmen();
            }

            // Fiel das bisher markierte Projekt durch den Suchfilter heraus, muss der
            // Abnehmer das erfahren - MarkierungUebernehmen kommt hier nicht mehr vorbei.
            if (GewaehlteID == 0 && vorherID != 0)
            {
                ProjektGewaehltHandler leer = MarkierungGeaendert;
                if (leer != null) leer(0, "");
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
            int vorherID = GewaehlteID;

            GewaehlteID = 0;
            GewaehlterName = "";

            ListViewItem it = null;
            if (listView_Projekte.SelectedItems.Count > 0) it = listView_Projekte.SelectedItems[0];
            else if (_markiert != null && listView_Projekte.Items.Contains(_markiert)) it = _markiert;
            if (it != null)
            {
                _markiert = it;
                ProjektModel p = it.Tag as ProjektModel;
                if (p != null)
                {
                    GewaehlteID = p.m_ID;
                    GewaehlterName = p.m_szProjektname ?? "";
                }
            }

            if (GewaehlteID == vorherID) return;
            ProjektGewaehltHandler h = MarkierungGeaendert;
            if (h != null) h(GewaehlteID, GewaehlterName);
        }

        private void listView_Projekte_DoubleClick(object sender, EventArgs e)
        {
            if (_mehrfachAuswahl)
            {
                // Im Häkchenmodus schaltet der Doppelklick den Haken der Zeile um —
                // eine „Auswahl" im Sinne von Öffnen gibt es hier nicht.
                if (listView_Projekte.SelectedItems.Count > 0)
                    listView_Projekte.SelectedItems[0].Checked = !listView_Projekte.SelectedItems[0].Checked;
                return;
            }
            AuswahlMelden();
        }

        // Tastaturweg (Nutzerauftrag 02.09.2026): Enter im Suchfeld übernimmt die
        // markierte Zeile, Pfeil-ab springt in die Liste; Enter in der Liste wirkt
        // wie der Doppelklick.
        private void textBox_Suche_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !_mehrfachAuswahl)
            {
                e.SuppressKeyPress = true;
                AuswahlMelden();
            }
            else if (e.KeyCode == Keys.Down && listView_Projekte.Items.Count > 0)
            {
                e.SuppressKeyPress = true;
                if (listView_Projekte.SelectedItems.Count == 0) listView_Projekte.Items[0].Selected = true;
                listView_Projekte.Focus();
            }
        }

        private void listView_Projekte_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            listView_Projekte_DoubleClick(sender, EventArgs.Empty);
        }

        // Häkchen-Kopplung: Ein Stamm nimmt seine Varianten mit (an und ab); die
        // angehakten Ids leben unabhängig vom Suchfilter in _angehakt.
        private void listView_Projekte_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_hakenIntern || !_mehrfachAuswahl) return;
            ProjektModel p = e.Item.Tag as ProjektModel;
            if (p == null) return;

            bool an = e.Item.Checked;
            if (an) _angehakt.Add(p.m_ID); else _angehakt.Remove(p.m_ID);

            List<int> varianten = VariantenVon(p.m_ID);
            if (varianten.Count > 0)
            {
                _hakenIntern = true;
                try
                {
                    foreach (int vid in varianten)
                    {
                        if (an) _angehakt.Add(vid); else _angehakt.Remove(vid);
                        foreach (ListViewItem it in listView_Projekte.Items)
                        {
                            ProjektModel q = it.Tag as ProjektModel;
                            if (q != null && q.m_ID == vid) it.Checked = an;
                        }
                    }
                }
                finally { _hakenIntern = false; }
            }

            ZaehlzeileSchreiben();
            EventHandler h = AuswahlGeaendert;
            if (h != null) h(this, EventArgs.Empty);
        }
    }
}
