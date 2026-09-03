using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog für Projektvarianten.
    ///
    /// Konzept (ohne echte Variantentabelle im FK-Pfad zu den Detailtabellen):
    ///  - Eine Variante ist ein vollwertiger Kopie-Datensatz in Tab_Projekt (über ProjektDuplizierenCtrl).
    ///  - Die Seitentabelle Tab_Variante(ID, ID_Projekt, ID_ProjektRef, Variantenname) verknüpft
    ///    die Variante (ID_Projekt) mit ihrem Stammprojekt (ID_ProjektRef). Die Detailtabellen
    ///    bleiben unverändert an ID_Projekt hängen.
    ///
    /// Die Variantenlogik (Anlegen/Löschen/Auflisten) liegt seit Phase 1 des
    /// Berichtsmoduls in VariantenCtrl — dieses Formular ist nur noch Bedienoberfläche.
    /// "Bericht erstellen…" öffnet den neuen Berichtsdialog Form_Bericht.
    ///
    /// Die Form ist komplett im Code aufgebaut (kein .resx nötig). Aufruf z. B.:
    ///     new Form_Variantentest().ShowDialog();
    /// </summary>
    public partial class Form_Variantentest : Form
    {
        // ID des aktuell in Form_Start geöffneten Projekts (-1 = ohne Kontext geöffnet).
        private readonly int _aktuellesProjekt;

        // Variante, die nach dem Laden in der Liste markiert werden soll (-1 = keine).
        private int _markiereVarianteId = -1;

        private readonly VariantenCtrl _ctrl = new VariantenCtrl();

        public Form_Variantentest() : this(-1) { }

        // Aufruf aus Form_Start:  new Form_Variantentest(m_ID_Projekt).ShowDialog();
        public Form_Variantentest(int aktuellesProjekt)
        {
            _aktuellesProjekt = aktuellesProjekt;
            InitializeComponent();
        }

        // ------------------------------------------------------------- Ereignisse

        private void Form_Variantentest_Load(object sender, EventArgs e)
        {
            if (this.DesignMode) return;
            _ctrl.StelleVariantentabelleSicher();
            LadeProjekte();
        }

        private void cbStamm_SelectedIndexChanged(object sender, EventArgs e)
        {
            LadeAuswahl();
            if (AktuellerStamm != null) SpeichereLetztenStamm(AktuellerStamm.Id);
        }

        private void chkNurStaemme_CheckedChanged(object sender, EventArgs e)
        {
            AktualisiereStammCombo(AktuellerStamm != null ? AktuellerStamm.Id : -1);
        }

        private void lvAuswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            AktualisiereButtons();
        }

        // -------------------------------------------------------------- Laden

        // Registry-Ablage der zuletzt gewählten Stamm-Auswahl.
        private const string RegPfad = @"Software\EPOS_PLAN\Variantentest";
        private const string RegWertStamm = "LetzterStammID";

        private void LadeProjekte()
        {
            try
            {
                FuelleStammCombo();
                if (cbStamm.Items.Count == 0) { LadeAuswahl(); return; }

                // Vorrang: aktuelles Projekt -> letzte Auswahl -> erster Eintrag.
                int idx = FindeStammIndex(BestimmeVorauswahl());
                cbStamm.SelectedIndex = idx >= 0 ? idx : 0;
            }
            catch (Exception ex) { Melde("Fehler beim Laden der Projekte: " + ex.Message); }
        }

        // Befüllt das Stamm-Dropdown - je nach Filter alle Projekte oder nur bereits gesetzte Stammprojekte.
        private void FuelleStammCombo()
        {
            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadAll();

            System.Collections.Generic.HashSet<int> nurStaemme = null;
            if (chkNurStaemme != null && chkNurStaemme.Checked)
                nurStaemme = _ctrl.LiesStammProjektIds();

            cbStamm.Items.Clear();
            foreach (ProjektModel p in pc.items)
            {
                if (nurStaemme != null && !nurStaemme.Contains(p.m_ID)) continue;
                cbStamm.Items.Add(new ProjektEintrag(p.m_ID, p.m_szProjektname));
            }
        }

        // Combo neu aufbauen (z. B. nach Anlegen einer Variante oder Filterwechsel) und die
        // gewünschte Stamm-ID beibehalten, sonst ersten Eintrag wählen.
        private void AktualisiereStammCombo(int stammId)
        {
            FuelleStammCombo();
            if (cbStamm.Items.Count == 0) { LadeAuswahl(); return; }
            int idx = FindeStammIndex(stammId);
            cbStamm.SelectedIndex = idx >= 0 ? idx : 0;
            LadeAuswahl();   // sicherstellen, dass die Liste passt (falls der Index unverändert blieb)
        }

        // Bestimmt das vorzuwählende Stammprojekt:
        //  1. aktuell geöffnetes Projekt aus Form_Start (ist es eine Variante -> deren Stammprojekt,
        //     die Variante wird anschließend in der Liste markiert),
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

        // Index des Stammprojekts mit der gegebenen ID in der ComboBox (-1 = nicht gefunden).
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

        // Liest die zuletzt gewählte Stamm-ID (-1, wenn keine/ungültig).
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

        private ProjektEintrag AktuellerStamm => cbStamm.SelectedItem as ProjektEintrag;

        // Füllt die Liste mit dem Stammprojekt (erste Zeile) und seinen Varianten.
        private void LadeAuswahl()
        {
            lvAuswahl.Items.Clear();
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { AktualisiereButtons(); return; }

            foreach (VariantenCtrl.VarianteInfo vi in _ctrl.LadeGruppe(stamm.Id, stamm.Name))
            {
                ListViewItem it = new ListViewItem(new[]
                {
                    vi.IstStamm ? "Stamm" : "Variante",
                    vi.IstStamm ? "(Stammprojekt)" : vi.Variantenname,
                    vi.Projektname
                })
                {
                    Tag = new AuswahlZeile(vi.IdProjekt, vi.Projektname, vi.Variantenname, vi.IstStamm)
                };
                lvAuswahl.Items.Add(it);
            }

            WaehleZeile();
            AktualisiereButtons();
        }

        // Wählt nach dem Laden die passende Listenzeile: die zu markierende Variante,
        // sonst das Stammprojekt (Zeile 0).
        private void WaehleZeile()
        {
            if (lvAuswahl.Items.Count == 0) return;

            if (_markiereVarianteId > 0)
            {
                foreach (ListViewItem it in lvAuswahl.Items)
                {
                    AuswahlZeile z = it.Tag as AuswahlZeile;
                    if (z != null && !z.IstStamm && z.IdProjekt == _markiereVarianteId)
                    {
                        it.Selected = true;
                        it.EnsureVisible();
                        _markiereVarianteId = -1;   // nur einmal (Erstladen) anwenden
                        return;
                    }
                }
                _markiereVarianteId = -1;
            }

            lvAuswahl.Items[0].Selected = true;
        }

        private AuswahlZeile AktuelleZeile =>
            lvAuswahl.SelectedItems.Count > 0 ? lvAuswahl.SelectedItems[0].Tag as AuswahlZeile : null;

        private void AktualisiereButtons()
        {
            AuswahlZeile z = AktuelleZeile;
            btnSimulieren.Enabled = z != null;
            btnLoeschen.Enabled = z != null && !z.IstStamm;
            btnAnlegen.Enabled = AktuellerStamm != null;
            btnVergleich.Enabled = AktuellerStamm != null;
            btnBericht.Enabled = AktuellerStamm != null;
            btnWirtschaft.Enabled = AktuellerStamm != null;
        }

        // ------------------------------------------------------------ Aktionen

        private void btnAnlegen_Click(object sender, EventArgs e)
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { Melde("Kein Stammprojekt gewählt."); return; }

            try
            {
                Cursor = Cursors.WaitCursor;

                string fehler;
                int neueId = _ctrl.AnlegenAusStamm(stamm.Id, stamm.Name, txtBezeichner.Text, out fehler);
                if (neueId <= 0) { Melde(fehler ?? "Variante konnte nicht angelegt werden."); return; }

                string bezeichner = (txtBezeichner.Text ?? "").Trim();
                txtBezeichner.Clear();
                AktualisiereStammCombo(stamm.Id);   // Combo neu (neue Variante -> Stammstatus), Auswahl beibehalten
                // Ä19: Auch die Varianten-Klappliste des Projektkopfs kennt die neue
                // Variante sofort (bisher zog nur der Menüweg über Form_AlsVariante nach).
                Program.startfrm?.VariantenAnzeigeAktualisieren();
                Melde("Variante '" + bezeichner + "' angelegt.");
            }
            catch (Exception ex) { Melde("Fehler beim Anlegen: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            AuswahlZeile z = AktuelleZeile;
            if (z == null || z.IstStamm) { Melde("Bitte eine Variante auswählen (das Stammprojekt wird hier nicht gelöscht)."); return; }

            DialogResult dr = MessageBox.Show(
                "Variante '" + z.Variantenname + "' und alle zugehörigen Projektdaten unwiderruflich löschen?",
                "Variante löschen", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.Yes) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                string fehler;
                if (!_ctrl.LoescheVariante(z.IdProjekt, z.Projektname, out fehler))
                { Melde(fehler ?? "Variante konnte nicht gelöscht werden."); return; }

                LadeAuswahl();
                Melde("Variante '" + z.Variantenname + "' gelöscht.");
            }
            catch (Exception ex) { Melde("Fehler beim Löschen: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
        }

        private void btnSimulieren_Click(object sender, EventArgs e)
        {
            AuswahlZeile z = AktuelleZeile;
            ProjektEintrag stamm = AktuellerStamm;
            if (z == null || stamm == null) { Melde("Bitte Stamm oder Variante auswählen."); return; }

            // Zu simulierende Projekte: der Stamm immer, plus die gewählte Variante.
            // So werden die Ergebnisse von Stamm UND Variante frisch geschrieben (für den Vergleich).
            System.Collections.Generic.List<Tuple<int, string>> laeufe =
                new System.Collections.Generic.List<Tuple<int, string>>();
            laeufe.Add(Tuple.Create(stamm.Id, "Stamm: " + stamm.Name));
            if (!z.IstStamm)
                laeufe.Add(Tuple.Create(z.IdProjekt, "Variante: " + z.Variantenname));

            try
            {
                Cursor = Cursors.WaitCursor;

                System.Collections.Generic.List<string> meldungen = new System.Collections.Generic.List<string>();
                foreach (Tuple<int, string> lauf in laeufe)
                {
                    // Headless-Lauf: neue Instanz je Projekt (frische Simulationsobjekte).
                    string fehler;
                    SimulationRunner runner = new SimulationRunner();
                    int erg = runner.SimuliereUndSpeichere(lauf.Item1, out fehler);
                    meldungen.Add(erg > 0
                        ? lauf.Item2 + ": ok (Ergebnis-ID " + erg + ")"
                        : lauf.Item2 + ": FEHLER – " + fehler);

                    // NACHARBEIT PAKET 8, BEFUND N5: Auch ein ERFOLGREICHER Lauf kann
                    // gemeldet haben, dass er mit einer Ersatzannahme gerechnet hat -
                    // etwa "Tagesverteilungstyp nicht hinterlegt, Bedarfsrechnung
                    // abgebrochen". „out fehler" ist nur im Misserfolgsfall belegt (das
                    // bleibt so), und vor Paket 8 zeigte die Engine hier eine MessageBox.
                    // Ohne diese Zeilen stünde in der Sammelmeldung ein blankes "ok".
                    string hinweise = runner.Protokoll != null
                        ? runner.Protokoll.HinweistextFuerAnzeige() : "";
                    if (!string.IsNullOrEmpty(hinweise))
                        meldungen.Add("    " + hinweise.Replace("\r\n", "\r\n    ")
                                                       .Replace("\n", "\n    "));
                }

                Melde("Simulation abgeschlossen (" + laeufe.Count + " Projekt(e)).");
                MessageBox.Show(string.Join("\r\n", meldungen), "Simulation",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Melde("Fehler bei der Simulation: " + ex.Message); }
            finally { Cursor = Cursors.Default; }
        }

        // Wirtschaftlichkeit (Phase 6): Kapitalwert-Reiter der Vergleichsgruppe.
        private void btnWirtschaft_Click(object sender, EventArgs e)
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { Melde("Kein Stammprojekt gewählt."); return; }

            using (Form_Wirtschaftlichkeit dlg = new Form_Wirtschaftlichkeit(stamm.Id))
                dlg.ShowDialog(this);

            LadeAuswahl();   // Simulationsstände können sich geändert haben
        }

        // Neuer Berichtsweg (Phase 1): öffnet den Berichtsdialog mit Variantencheckliste.
        private void btnBericht_Click(object sender, EventArgs e)
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { Melde("Kein Stammprojekt gewählt."); return; }

            using (Form_Bericht dlg = new Form_Bericht(stamm.Id, stamm.Name))
                dlg.ShowDialog(this);

            LadeAuswahl();   // Simulationsstände können sich geändert haben
        }

        // Bisheriger Direktbericht (Stamm + eine markierte Variante) — wird mit
        // Phase 2 des Berichtsmoduls durch Form_Bericht abgelöst.
        private void btnVergleich_Click(object sender, EventArgs e)
        {
            ProjektEintrag stamm = AktuellerStamm;
            if (stamm == null) { Melde("Kein Stammprojekt gewählt."); return; }

            // Vergleichsgruppe: Stammprojekt + aktuell markierte Variante (falls eine gewählt ist).
            AuswahlZeile z = AktuelleZeile;
            System.Collections.Generic.List<ProjektvergleichBericht.Projekt> gruppe =
                new System.Collections.Generic.List<ProjektvergleichBericht.Projekt>();
            gruppe.Add(new ProjektvergleichBericht.Projekt
            {
                Id = stamm.Id,
                Name = stamm.Name,
                Bezeichner = "",
                IstStamm = true
            });
            if (z != null && !z.IstStamm)
                gruppe.Add(new ProjektvergleichBericht.Projekt
                {
                    Id = z.IdProjekt,
                    Name = z.Projektname,
                    Bezeichner = z.Variantenname,
                    IstStamm = false
                });

            using (System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog())
            {
                sfd.Filter = "Word-Dokument (*.docx)|*.docx";
                sfd.FileName = "Projektvergleich_" + stamm.Name + ".docx";
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    // Der Bericht simuliert die Gruppe selbst neu (Nutzeranforderung
                    // 15.08.2026) und liefert die Meldungen der Läufe zurück.
                    ProjektvergleichBericht bericht = new ProjektvergleichBericht();
                    bericht.Erzeuge(sfd.FileName, gruppe);
                    Melde("Bericht erstellt: " + sfd.FileName);

                    string frage = "Bericht wurde erstellt (alle Projekte neu simuliert).";
                    if (bericht.Laufmeldungen.Count > 0)
                        frage += "\r\n\r\nHinweise:\r\n• " +
                                 string.Join("\r\n• ", bericht.Laufmeldungen);
                    frage += "\r\n\r\nJetzt öffnen?";

                    if (MessageBox.Show(frage, "Projektvergleich",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    // Vollstaendige Fehlermeldung inkl. inner exceptions anzeigen (Statuszeile kuerzt ab).
                    string msg = ex.Message;
                    Exception inner = ex.InnerException;
                    while (inner != null) { msg += "\r\n→ " + inner.Message; inner = inner.InnerException; }
                    Melde("Fehler beim Erstellen des Berichts.");
                    MessageBox.Show(msg, "Fehler beim Erstellen des Berichts",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { Cursor = Cursors.Default; }
            }
        }

        // -------------------------------------------------------------- Helfer

        private void Melde(string text)
        {
            if (lblStatus != null) lblStatus.Text = text;
        }

        // -------------------------------------------------- kleine Hilfsklassen

        private class ProjektEintrag
        {
            public int Id { get; }
            public string Name { get; }
            public ProjektEintrag(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private class AuswahlZeile
        {
            public int IdProjekt { get; }
            public string Projektname { get; }
            public string Variantenname { get; }
            public bool IstStamm { get; }
            public AuswahlZeile(int idProjekt, string projektname, string variantenname, bool istStamm)
            {
                IdProjekt = idProjekt; Projektname = projektname; Variantenname = variantenname; IstStamm = istStamm;
            }
        }
    }
}
