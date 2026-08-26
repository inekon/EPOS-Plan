using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Energieträgerverwaltung (Etappe KD4, Konzept Kostendialoge § 7, Folien 25/26) —
    /// der Nachfolger des Energie-Reiters von <c>Form_Kosten</c> (Ä1): links die
    /// Trägerliste, rechts der Trägerbereich (<see cref="ucFuelSettings"/> bleibt Kern);
    /// beim Träger „Elektrische Energie" zusätzlich die beiden K4-Einstiegskarten
    /// „Kostenprofil" und „Spotmarktpreise" — „Kostenprofil kein separater Tab, nur
    /// unter Strom" (Folie 25).
    ///
    /// <para><b>Zwei Kontexte, ein Formular</b> (Muster <c>Form_KostenKomponente</c>):
    /// Projektkontext (<c>projektId &gt; 0</c>) pflegt die Projektübersteuerung
    /// (<c>energy_project_settings</c>, wie der bisherige Reiter); der Katalogkontext
    /// (Menü Administration → Kosten → „Energieträgerverwaltung …", Projekt 0) zeigt
    /// die Katalogwerte NUR LESEND — pflegbar sind dort der Leistungspreis-Modus
    /// (Katalogsache, FK6) und die Stamm-Leistungspreisreihen (FK6a) sowie der
    /// Spotpreis-Import als Stammreihe. Die volle Katalogpreis-Pflege samt
    /// Trägervarianten („Speichern unter …", § 7.1) ist bewusst noch offen.</para>
    ///
    /// <para><b>Emissionsteil offen:</b> Die Quellenwahl je Träger (KL8, § 7.3) setzt
    /// die Etappen E1/E2 des noch zur Abnahme stehenden Emissionsfaktoren-Konzepts
    /// voraus und fehlt hier absichtlich.</para>
    /// </summary>
    public partial class Form_Energietraeger : Form
    {
        private int _projektId;
        private bool _wirdGefuellt;

        private EinstiegsKarte _karteKostenprofil;
        private EinstiegsKarte _karteSpotpreise;

        public Form_Energietraeger()
        {
            InitializeComponent();

            Text = T("KDLG_ET_TITEL", "Energieträgerverwaltung");
            lblKopfTitel.Text = Text;
            lblListeTitel.Text = T("KDLG_ET_LISTE", "Energieträger");
            btnSchliessen.Text = T("KDLG_ET_SCHLIESSEN", "Schließen");
        }

        /// <summary>Kontext setzen und Liste laden — vor <c>ShowDialog</c>.
        /// <paramref name="projektId"/> 0 = Katalogkontext.</summary>
        // ---- Ä9: Katalogpflege (nur Katalogkontext) --------------------------
        private Panel _katalogLeiste;
        private TextBox _txtStammName;
        private ComboBox _cmbStammGruppe;

        public void SetControls(int projektId)
        {
            _projektId = projektId > 0 ? projektId : 0;

            lblKontext.Text = _projektId > 0
                ? string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_ET_KONTEXT_PROJEKT", "Kontext: Projekt {0}"), _projektId)
                : T("KDLG_ET_KONTEXT_KATALOG", "Kontext: Katalog (Stammdaten)");

            _wirdGefuellt = true;
            try
            {
                List<EnergyCarrier> traeger = Form_Kosten.GetAllCarriers(_projektId);
                lstTraeger.DataSource = traeger;
                lstTraeger.DisplayMember = "Name";
                lstTraeger.SelectedIndex = -1;
            }
            finally { _wirdGefuellt = false; }

            pnlInhalt.Controls.Clear();

            if (_projektId <= 0) KatalogLeisteSicherstellen();
            else ProjektLeisteSicherstellen();

            // Erster Träger vorgewählt — ein leerer Detailbereich sah wie ein
            // fehlender Dialog aus (Befund 26.08.2026, Katalogkontext).
            if (lstTraeger.Items.Count > 0) lstTraeger.SelectedIndex = 0;
        }

        private void lstTraeger_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_wirdGefuellt) return;
            ZeigeTraeger(lstTraeger.SelectedItem as EnergyCarrier);
        }

        /// <summary>ETAPPE KD6 (§ 9): Vorwahl des Trägers — „Energiekosten…" aus dem
        /// Anlagendialog springt direkt auf den Träger der Komponente.</summary>
        public void WaehleTraeger(int carrierId)
        {
            for (int i = 0; i < lstTraeger.Items.Count; i++)
                if (lstTraeger.Items[i] is EnergyCarrier c && c.ID == carrierId)
                { lstTraeger.SelectedIndex = i; return; }
        }

        /// <summary>
        /// Bestandsverhalten des Energie-Reiters: Das offene <see cref="ucFuelSettings"/>
        /// wird beim Trägerwechsel und beim Schließen gespeichert (nur Projektkontext;
        /// nur, wenn der Träger dem Projekt noch zugeordnet ist — sonst würde ein
        /// gelöschter Träger wieder angelegt; Logik aus <c>Form_Kosten.OnFormClosing</c>).
        /// </summary>
        private void SpeichereOffenes()
        {
            if (_projektId <= 0) return;
            try
            {
                foreach (Control c in pnlInhalt.Controls)
                {
                    ucFuelSettings uc = c as ucFuelSettings;
                    if (uc == null) continue;

                    int zugeordnet = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_project_settings " +
                        "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                        new System.Data.OleDb.OleDbParameter("@p", _projektId),
                        new System.Data.OleDb.OleDbParameter("@c", uc.CarrierId)));
                    if (zugeordnet > 0) uc.SaveProjectAndHistory();
                }
            }
            catch { /* Wechsel/Schließen nie am Speichern scheitern lassen */ }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SpeichereOffenes();
            base.OnFormClosing(e);
        }

        private void ZeigeTraeger(EnergyCarrier c)
        {
            SpeichereOffenes();
            pnlInhalt.SuspendLayout();
            pnlInhalt.Controls.Clear();
            _karteKostenprofil = null;
            _karteSpotpreise = null;

            if (c == null) { pnlInhalt.ResumeLayout(); return; }

            int y = 12;

            // Ä1: Die beiden K4-Karten erscheinen NUR beim Stromträger — Kostenprofil
            // ist Projektwahrheit (nur Projektkontext), Spotreihen gibt es auch als
            // Stammreihen (beide Kontexte).
            if (string.Equals(c.PricingModel, "ELECTRICITY", StringComparison.OrdinalIgnoreCase))
            {
                int x = 12;
                if (_projektId > 0)
                {
                    _karteKostenprofil = new EinstiegsKarte
                    {
                        Location = new Point(x, y),
                        Size = new Size(360, 150),
                        Titel = T("KPROF_KARTE_PROFIL_TITEL", "Kostenprofil"),
                        Beschreibung = T("KPROF_KARTE_PROFIL_INFO",
                            "Monatliche Preisniveaus des Strombezugs pflegen.")
                    };
                    _karteKostenprofil.Geklickt += (s, e2) =>
                    {
                        KostenprofilCtrl ctrl = new KostenprofilCtrl();
                        var vorhandene = ctrl.ReadAllByProjekt(_projektId);
                        int id = vorhandene.Count > 0 ? vorhandene[0].ID : 0;
                        using (Form_Kostenprofil dlg = new Form_Kostenprofil(_projektId, id))
                            dlg.ShowDialog(this);
                        AktualisiereKarten();
                    };
                    pnlInhalt.Controls.Add(_karteKostenprofil);
                    x += 372;
                }

                _karteSpotpreise = new EinstiegsKarte
                {
                    Location = new Point(x, y),
                    Size = new Size(360, 150),
                    Titel = T("KPROF_KARTE_SPOT_TITEL", "Spotmarktpreise"),
                    Beschreibung = T("KPROF_KARTE_SPOT_INFO",
                        "Stundenpreise importieren und verwalten.")
                };
                _karteSpotpreise.Geklickt += (s, e2) =>
                {
                    using (Form_SpotpreisImport dlg = new Form_SpotpreisImport(_projektId))
                        dlg.ShowDialog(this);
                    AktualisiereKarten();
                };
                pnlInhalt.Controls.Add(_karteSpotpreise);

                AktualisiereKarten();
                y += 162;
            }

            // Ä9: Im Katalogkontext ist der Stammkopf EDITIERBAR — Bezeichnung
            // und Gruppen-Zuordnung schreiben direkt in die Katalogzeile.
            if (_projektId <= 0)
            {
                var lblN = new Label { Text = T("KDLG_ET_STAMM_NAME", "Bezeichnung:"),
                    Location = new Point(12, y + 4), AutoSize = true };
                _txtStammName = new TextBox { Text = c.Name,
                    Location = new Point(110, y), Width = 260 };
                var lblG = new Label { Text = T("KDLG_ET_STAMM_GRUPPE", "Gruppe:"),
                    Location = new Point(390, y + 4), AutoSize = true };
                _cmbStammGruppe = new ComboBox { Location = new Point(450, y), Width = 160,
                    DropDownStyle = ComboBoxStyle.DropDown };
                foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
                    _cmbStammGruppe.Items.Add(g);
                _cmbStammGruppe.Text = c.GroupCode ?? "";
                var btnStamm = new Button { Text = T("KDLG_ET_STAMM_SPEICHERN", "Übernehmen"),
                    Location = new Point(624, y - 1), Size = new Size(110, 26) };
                int stammId = c.ID;
                btnStamm.Click += (s2, e2) =>
                {
                    if (EnergietraegerKatalogCtrl.Umbenennen(stammId,
                            _txtStammName.Text, _cmbStammGruppe.Text))
                        ListeNeuLaden(stammId);
                    else
                        MessageBox.Show(T("KDLG_ET_STAMM_FEHLER",
                                "Bezeichnung darf nicht leer sein."), Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                pnlInhalt.Controls.Add(lblN);
                pnlInhalt.Controls.Add(_txtStammName);
                pnlInhalt.Controls.Add(lblG);
                pnlInhalt.Controls.Add(_cmbStammGruppe);
                pnlInhalt.Controls.Add(btnStamm);
                y += 34;
            }

            ucFuelSettings uc = new ucFuelSettings(_projektId, c)
            {
                Name = "ucFuelSettings",
                Location = new Point(12, y),
                Width = pnlInhalt.ClientSize.Width - 36,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlInhalt.Controls.Add(uc);

            pnlInhalt.ResumeLayout();
        }

        /// <summary>Ä9: Verwaltungsleiste des Katalogkontexts — Neu, Variante
        /// (Kopie mit eigenem Emissions-/Preissatz je Träger), Löschen.</summary>
        private void KatalogLeisteSicherstellen()
        {
            if (_katalogLeiste != null) return;

            _katalogLeiste = new Panel { Height = 38, Dock = DockStyle.Bottom };
            var btnNeu = KatalogKnopf(T("KDLG_ET_BTN_NEU", "Neu…"), 4);
            btnNeu.Click += (s, e) =>
            {
                using (var dlg = new Form_VariantenName())
                {
                    dlg.SetControls(T("KDLG_ET_NEU_TITEL", "Neuer Energieträger"),
                        T("KDLG_ET_NEU_NAME", "Bezeichnung des neuen Trägers:"),
                        T("KDLG_ET_NEU_VORGABE", "Neuer Energieträger"));
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    int id = EnergietraegerKatalogCtrl.Neu(dlg.Ergebnis, null);
                    if (id > 0) ListeNeuLaden(id);
                }
            };
            var btnVariante = KatalogKnopf(T("KDLG_ET_BTN_VARIANTE", "Variante"), 78);
            btnVariante.Click += (s, e) =>
            {
                var c = lstTraeger.SelectedItem as EnergyCarrier;
                if (c == null) return;
                int id = EnergietraegerKatalogCtrl.Variante(c.ID);
                if (id > 0) ListeNeuLaden(id);
            };
            var btnLoeschen = KatalogKnopf(T("KDLG_ET_BTN_LOESCHEN", "Löschen"), 152);
            btnLoeschen.Click += (s, e) =>
            {
                var c = lstTraeger.SelectedItem as EnergyCarrier;
                if (c == null) return;
                if (MessageBox.Show(string.Format(
                            T("KDLG_ET_LOESCHEN_FRAGE", "Energieträger „{0}“ löschen?"), c.Name),
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                string grund;
                if (EnergietraegerKatalogCtrl.Loeschen(c.ID, out grund))
                    ListeNeuLaden(0);
                else
                    MessageBox.Show(string.Format(
                            T("KDLG_ET_LOESCHEN_GESPERRT",
                              "Der Träger wird verwendet und bleibt erhalten: {0}"), grund),
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _katalogLeiste.Controls.Add(btnNeu);
            _katalogLeiste.Controls.Add(btnVariante);
            _katalogLeiste.Controls.Add(btnLoeschen);
            lstTraeger.Parent.Controls.Add(_katalogLeiste);
        }

        private static Button KatalogKnopf(string text, int x)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 6),
                Size = new Size(70, 26),
                UseVisualStyleBackColor = true
            };
        }

        /// <summary>Liste neu aus der Datenbank laden und Auswahl setzen (0 = erste).</summary>
        private void ListeNeuLaden(int auswahlId)
        {
            int projekt = _projektId;
            SetControls(projekt);
            if (auswahlId > 0) WaehleTraeger(auswahlId);
        }

        // ---- Ä10: Projektkontext — Übernahme aus dem Katalog -----------------
        private Panel _projektLeiste;

        /// <summary>Ä10 (KD4-Punkt § 7.2): Katalogträger ins Projekt übernehmen
        /// bzw. eine Zuordnung wieder lösen (Anlagen schützen ihre Träger).</summary>
        private void ProjektLeisteSicherstellen()
        {
            if (_projektLeiste != null) return;

            _projektLeiste = new Panel { Height = 38, Dock = DockStyle.Bottom };
            var btnUebernehmen = new Button
            {
                Text = T("KDLG_ET_BTN_UEBERNEHMEN", "Aus Katalog übernehmen…"),
                Location = new Point(4, 6),
                Size = new Size(170, 26),
                UseVisualStyleBackColor = true
            };
            btnUebernehmen.Click += (s, e) => KatalogUebernahme();
            var btnEntfernen = new Button
            {
                Text = T("KDLG_ET_BTN_ENTFERNEN", "Entfernen"),
                Location = new Point(178, 6),
                Size = new Size(90, 26),
                UseVisualStyleBackColor = true
            };
            btnEntfernen.Click += (s, e) =>
            {
                var c = lstTraeger.SelectedItem as EnergyCarrier;
                if (c == null) return;
                if (MessageBox.Show(string.Format(
                            T("KDLG_ET_ENTFERNEN_FRAGE",
                              "Träger „{0}“ aus dem Projekt entfernen? (Der Katalogeintrag bleibt.)"),
                            c.Name),
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                string grund;
                if (EnergietraegerKatalogCtrl.AusProjektEntfernen(_projektId, c.ID, out grund))
                    ListeNeuLaden(0);
                else
                    MessageBox.Show(string.Format(
                            T("KDLG_ET_ENTFERNEN_GESPERRT", "Nicht möglich: {0}"), grund),
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _projektLeiste.Controls.Add(btnUebernehmen);
            _projektLeiste.Controls.Add(btnEntfernen);
            lstTraeger.Parent.Controls.Add(_projektLeiste);
        }

        /// <summary>Mehrfachauswahl der noch nicht zugeordneten Katalogträger.</summary>
        private void KatalogUebernahme()
        {
            List<EnergyCarrier> frei = EnergietraegerKatalogCtrl.NichtZugeordnete(_projektId);
            if (frei.Count == 0)
            {
                MessageBox.Show(T("KDLG_ET_UEBERNAHME_LEER",
                        "Alle Katalogträger sind dem Projekt bereits zugeordnet."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = T("KDLG_ET_UEBERNAHME_TITEL", "Aus Katalog übernehmen");
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MinimizeBox = false; dlg.MaximizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(360, 380);

                var liste = new CheckedListBox
                {
                    Location = new Point(12, 12),
                    Size = new Size(336, 316),
                    CheckOnClick = true
                };
                foreach (EnergyCarrier c in frei) liste.Items.Add(c.Name);
                var ok = new Button
                {
                    Text = T("KDLG_ET_UEBERNAHME_OK", "Übernehmen"),
                    DialogResult = DialogResult.OK,
                    Location = new Point(152, 340), Size = new Size(95, 27)
                };
                var abbruch = new Button
                {
                    Text = T("PVW_ABBRECHEN", "Abbrechen"),
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(253, 340), Size = new Size(95, 27)
                };
                dlg.Controls.Add(liste); dlg.Controls.Add(ok); dlg.Controls.Add(abbruch);
                dlg.AcceptButton = ok; dlg.CancelButton = abbruch;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                int letzter = 0;
                foreach (int i in liste.CheckedIndices)
                    if (EnergietraegerKatalogCtrl.InsProjekt(_projektId, frei[i].ID))
                        letzter = frei[i].ID;
                if (letzter > 0) ListeNeuLaden(letzter);
            }
        }

        /// <summary>Statuszeilen der Karten (kompakte Fassung der Form_Kosten-Logik).</summary>
        private void AktualisiereKarten()
        {
            if (_karteKostenprofil != null)
            {
                try
                {
                    var vorhandene = new KostenprofilCtrl().ReadAllByProjekt(_projektId);
                    _karteKostenprofil.Status = vorhandene.Count == 0
                        ? T("KPROF_STATUS_KEIN_PROFIL", "Noch kein Profil hinterlegt.")
                        : vorhandene[0].Bezeichner;
                }
                catch { _karteKostenprofil.Status = "—"; }
            }

            if (_karteSpotpreise != null)
            {
                try
                {
                    var reihen = new PreisreiheCtrl().ReadVerfuegbare(_projektId);
                    if (reihen.Count == 0)
                        _karteSpotpreise.Status = T("KDLG_ET_SPOT_KEINE", "Noch keine Preisreihe vorhanden.");
                    else
                    {
                        int min = int.MaxValue, max = int.MinValue;
                        foreach (PreisreiheModel m in reihen)
                        {
                            if (m.Jahr < min) min = m.Jahr;
                            if (m.Jahr > max) max = m.Jahr;
                        }
                        _karteSpotpreise.Status = string.Format(CultureInfo.CurrentCulture,
                            T("KDLG_ET_SPOT_STATUS", "{0} Reihe(n), Jahre {1}–{2}"),
                            reihen.Count, min, max);
                    }
                }
                catch { _karteSpotpreise.Status = "—"; }
            }
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
