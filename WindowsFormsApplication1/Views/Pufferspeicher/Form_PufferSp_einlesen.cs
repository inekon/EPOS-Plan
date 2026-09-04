using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp_einlesen : Form
    {
        private PufferSpImport ctrl = new PufferSpImport();

        // Zuordnung: Position in der (gefilterten) ListBox -> Index in ctrl._list
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        // Sperre gegen Rueckkopplung: waehrend FuelleListe() die Markierung
        // wiederherstellt, feuert SelectedIndexChanged und wuerde die Detailfelder
        // auf einen Zwischenstand setzen.
        private bool _listeWirdGefuellt = false;

        public Form_PufferSp_einlesen ()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_PufferSp.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Pufferspeicher");

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = "(*.vdi)|*.vdi";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;

                ctrl.Import(filename);
                FuelleListe();
            }
        }

        private void Liste_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_listeWirdGefuellt) return;   // Neuaufbau der Liste, kein Anwenderklick

            int sel = Liste_PufferSp.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            ZeigeDetails(_anzeigeIndex[sel]);
        }

        // Uebertraegt einen VDI-Eintrag in die Detailfelder. Die Felder sind auch
        // beim Mehrfachladen der Traeger fuer die Uebernahme (FuelleModellwerte
        // liest sie aus) - damit bleibt es bei genau einem Schreibweg.
        private void ZeigeDetails(int i)
        {
            if (i < 0 || i >= ctrl._list.Count) return;

            textBox_Name.Text = ctrl._list[i].m_szName;
            textBox_Firma.Text = ctrl._list[i].m_szFirma;
            textBox_Volumen.Text = ctrl._list[i].m_szVolumen;
            textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
            textBox_Typ.Text = ctrl._list[i].m_szTyp;
        }

        private void Volumenfilter_ValueChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        // Live-Filter ueber Bezeichner und Firma (Anwenderanforderung 17.08.2026).
        private void Suchfilter_TextChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        private void FuelleListe()
        {
            double min = (double)num_VolumenVon.Value;
            double max = (double)num_VolumenBis.Value;
            string suche = txt_Filter.Text;

            // Markierung (echte Indizes) sichern und nach dem Neuaufbau fuer alle
            // weiterhin sichtbaren Eintraege wiederherstellen: so geht eine bereits
            // getroffene Auswahl beim Tippen im Filter nicht stillschweigend
            // verloren, und unsichtbare Eintraege bleiben unmarkiert.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();

            _listeWirdGefuellt = true;
            Liste_PufferSp.BeginUpdate();
            Liste_PufferSp.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                // ZahlParsen statt convertTxt2Double: eine nicht parsbare
                // Volumenangabe darf den Listenaufbau nicht abbrechen - sie
                // zaehlt als 0, den Fehler meldet erst die Uebernahme.
                double volumen;
                if (!Program.ZahlParsen(ctrl._list[i].m_szVolumen, out volumen)) volumen = 0;
                if (volumen < min || volumen > max) continue;
                if (!VdiAuswahlFilter.Passt(suche, ctrl._list[i].m_szName, ctrl._list[i].m_szFirma)) continue;
                Liste_PufferSp.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            for (int zeile = 0; zeile < _anzeigeIndex.Count; zeile++)
            {
                if (markiert.Contains(_anzeigeIndex[zeile])) Liste_PufferSp.SetSelected(zeile, true);
            }
            Liste_PufferSp.EndUpdate();
            _listeWirdGefuellt = false;

            // Detailfelder auf die verbleibende Markierung nachziehen, damit sie
            // nach dem Umfiltern nicht mehr auf einen ausgefilterten Eintrag
            // zeigen (bei der Uebernahme sind sie die Quelle bzw. die Anzeige).
            if (Liste_PufferSp.SelectedIndex >= 0 && Liste_PufferSp.SelectedIndex < _anzeigeIndex.Count)
                ZeigeDetails(_anzeigeIndex[Liste_PufferSp.SelectedIndex]);
        }

        // Markierte Zeilen -> Indizes in ctrl._list.
        private System.Collections.Generic.List<int> MarkierteQuellIndizes()
        {
            return VdiAuswahlFilter.QuellIndizes(Liste_PufferSp.SelectedIndices, _anzeigeIndex);
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            // Mehrfachselektion: je markierter Eintrag laeuft genau der bestehende
            // Einzel-Weg (UebernehmeEintrag) - nur in einer Schleife, damit es
            // keinen zweiten Schreibpfad in die STAMM-Tabelle gibt.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();
            if (markiert.Count == 0)
            {
                MessageBox.Show(MyResource.Resource.PSP_MELDUNG_PUFFER_SELEKTIEREN);
                return;
            }

            // Einzelfall: die Detailfelder werden nicht neu besetzt, damit eine
            // Korrektur von Hand erhalten bleibt. Weil Handkorrektur vorgesehen ist,
            // wird das Zahlfeld vorab nach dem Hausmuster geprueft (sprechende
            // Meldung, Fokus, Dialog bleibt offen); leer gilt wie bisher als 0.
            if (markiert.Count == 1)
            {
                double dVerluste;
                if (!Program.ZahlPruefen(textBox_Versluste, "Betriebsbereitschaftsverluste", out dVerluste, leerErlaubt: true)) return;
            }

            // Vorpruefung der gesamten Auswahl gegen den Katalog und gegen sich
            // selbst (Konzept 4.1) - noch ohne Schreibzugriff. Quelle der Werte
            // sind die Detailfelder (Traeger der Uebernahme): bei Mehrfachauswahl
            // je Eintrag besetzt, im Einzelfall bleiben sie unangetastet, damit
            // die Handkorrektur auch in die Pruefung eingeht.
            KatalogDefinition katalog = KatalogRegistry.Finde("PUFFERSPEICHER");
            System.Collections.Generic.List<ImportKandidat> kandidaten = new System.Collections.Generic.List<ImportKandidat>();
            foreach (int i in markiert)
            {
                if (markiert.Count > 1) ZeigeDetails(i);
                ImportKandidat kand = new ImportKandidat { Name = textBox_Name.Text, Tag = i };
                try
                {
                    PufferSpModel probe = FuelleModellwerte();
                    kand.Werte["Hersteller"] = probe.Firma;
                    kand.Werte["Speichertyp"] = probe.Speichertyp;
                    kand.Werte["Bereitschaftsverluste"] = probe.Betriebsbereitschaftverlust;
                    kand.Werte["Gesamtvolumen"] = probe.Gesamtvolumen;
                }
                catch (FormatException)
                {
                    // Nicht parsbare Zahl: Vorpruefung ohne Inhaltswerte fortsetzen -
                    // den Fehler meldet erst die Uebernahme (Bestandsphilosophie).
                }
                kandidaten.Add(kand);
            }

            System.Collections.Generic.List<ImportPruefung> pruefungen =
                DublettenPruefung.PruefeKandidaten(katalog, kandidaten);

            bool konflikt = false;
            foreach (ImportPruefung p in pruefungen)
                if (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl) { konflikt = true; break; }

            if (!konflikt && markiert.Count == 1)
            {
                // Konfliktfreier Einzelfall: Meldungen und Dialogverhalten bleiben
                // wortgleich beim Bestand.
                string fehlertext;
                VdiUebernahmeErgebnis einzel = UebernehmeEintrag(out fehlertext);
                if (einzel == VdiUebernahmeErgebnis.Duplikat)
                {
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATEN_BEREITS_EINGELESEN);
                    return;
                }
                if (fehlertext != null)
                {
                    MessageBox.Show(string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, fehlertext));
                    this.DialogResult = DialogResult.Cancel;
                    return;
                }
                if (einzel == VdiUebernahmeErgebnis.Gespeichert)
                {
                    this.DialogResult = DialogResult.OK;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT);
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER);
                }
                Close();
                return;
            }

            System.Collections.Generic.List<KonfliktEntscheidung> entscheidungen;
            if (konflikt)
            {
                // EIN Dialog fuer die ganze Auswahl statt einer Meldung je Satz.
                entscheidungen = ImportKonflikteHuelle.Zeigen(this, pruefungen,
                    DublettenPruefung.VergebeneNamen(katalog));
                if (entscheidungen == null) return;
            }
            else
            {
                entscheidungen = new System.Collections.Generic.List<KonfliktEntscheidung>();
                foreach (ImportPruefung p in pruefungen)
                    entscheidungen.Add(new KonfliktEntscheidung { Pruefung = p, Aktion = KonfliktAktion.Importieren });
            }

            FuehreAus(markiert.Count, entscheidungen);
        }

        // Fuehrt die im Konfliktdialog gewaehlten Aktionen aus und zeigt die
        // Sammelmeldung. Auslassen zaehlt als uebersprungen.
        private void FuehreAus(int markiertAnzahl, System.Collections.Generic.List<KonfliktEntscheidung> entscheidungen)
        {
            int nGespeichert = 0;
            int nDuplikat = 0;
            int nFehler = 0;
            int nUeberschrieben = 0;
            int nUmbenannt = 0;
            Cursor alt = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (KonfliktEntscheidung ent in entscheidungen)
                {
                    int i = (int)ent.Pruefung.Kandidat.Tag;

                    // Detailfelder je Eintrag besetzen - sie sind der Traeger fuer
                    // FuelleModellwerte() und damit fuer die Uebernahme. Im
                    // Einzelfall bleiben sie unangetastet, damit eine Handkorrektur
                    // auch den Konfliktweg uebersteht (Bestandsverhalten).
                    if (markiertAnzahl > 1) ZeigeDetails(i);

                    string fehlertext;
                    VdiUebernahmeErgebnis ergebnis;
                    try
                    {
                        switch (ent.Aktion)
                        {
                            case KonfliktAktion.Auslassen:
                                ergebnis = VdiUebernahmeErgebnis.Duplikat;
                                break;
                            case KonfliktAktion.Ueberschreiben:
                                ergebnis = UeberschreibeEintrag(i, ent.Pruefung.Vorhanden.Id);
                                break;
                            case KonfliktAktion.Umbenennen:
                                ergebnis = UebernehmeEintrag(out fehlertext, ent.NeuerName);
                                if (ergebnis == VdiUebernahmeErgebnis.Gespeichert)
                                    ergebnis = VdiUebernahmeErgebnis.Umbenannt;
                                break;
                            default:
                                ergebnis = UebernehmeEintrag(out fehlertext);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ein fehlerhafter Eintrag darf den Gesamtvorgang nicht abbrechen.
                        Console.WriteLine("Fehler beim Einlesen von '" + ctrl._list[i].m_szName + "': " + ex.Message);
                        ergebnis = VdiUebernahmeErgebnis.Fehler;
                    }

                    if (ergebnis == VdiUebernahmeErgebnis.Gespeichert) nGespeichert++;
                    else if (ergebnis == VdiUebernahmeErgebnis.Duplikat) nDuplikat++;
                    else if (ergebnis == VdiUebernahmeErgebnis.Ueberschrieben) nUeberschrieben++;
                    else if (ergebnis == VdiUebernahmeErgebnis.Umbenannt) nUmbenannt++;
                    else nFehler++;
                }
            }
            finally
            {
                Cursor = alt;
            }

            MessageBox.Show(VdiAuswahlFilter.LadeMeldung(nGespeichert, markiertAnzahl,
                nDuplikat, nFehler, nUeberschrieben, nUmbenannt));

            // Wie im Bestand wird der Dialog nach erfolgreicher Uebernahme beendet;
            // ohne einen einzigen Treffer bleibt er offen, damit der Anwender
            // Filter und Auswahl korrigieren kann. Ueberschreiben und Umbenennen
            // zaehlen als Erfolg, weil sie den Katalog veraendert haben.
            if (nGespeichert + nUeberschrieben + nUmbenannt > 0)
            {
                this.DialogResult = DialogResult.OK;
                Close();
            }
        }

        // Uebernahme genau eines Eintrags in Tab_PufferSp_STAMM. Unveraenderter
        // Bestandsweg (Quelle sind die Detailfelder), nur mit Ergebnis als
        // Rueckgabewert statt MessageBox - die Meldung entscheidet der Aufrufer.
        // nameOverride traegt beim Umbenennen (Konfliktdialog) den vom Anwender
        // vergebenen neuen Bezeichner (Konzept 4.3); die Exists-Pruefung bleibt
        // als zweite Verteidigungslinie hinter der Vorpruefung.
        // Der lokale Stamm-Controller heisst pspctrl, weil er sonst das Feld ctrl
        // (PufferSpImport) verdecken wuerde.
        private VdiUebernahmeErgebnis UebernehmeEintrag(out string fehlertext, string nameOverride = null)
        {
            fehlertext = null;

            try
            {
                PufferSpStammCtrl pspctrl = new PufferSpStammCtrl();
                PufferSpModel model = FuelleModellwerte();
                if (nameOverride != null) model.Name = nameOverride;

                if (pspctrl.Exists(model.Name)) return VdiUebernahmeErgebnis.Duplikat;

                if (pspctrl.InsertFrom(model)) return VdiUebernahmeErgebnis.Gespeichert;
                return VdiUebernahmeErgebnis.Fehler;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei der Übernahme des Pufferspeichers: " + ex.Message);
                fehlertext = ex.Message;
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        // Ueberschreiben aus dem Konfliktdialog: aktualisiert genau die Importfelder
        // des Bestandssatzes, adressiert per ID - Bezeichner und Anwenderfelder
        // (Investitionskosten, ReadOnly) bleiben stehen (Konzept 4.2). Die Werte
        // kommen wie bei der Neuanlage aus den Detailfeldern, die FuehreAus fuer
        // den VDI-Eintrag index besetzt hat; der Stamm-Controller erbt vom Modell
        // und wird direkt befuellt.
        private VdiUebernahmeErgebnis UeberschreibeEintrag(int index, int bestandsId)
        {
            PufferSpStammCtrl pspctrl = new PufferSpStammCtrl();
            FuelleModellwerte(pspctrl);

            return pspctrl.UpdateImport(bestandsId)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }

        // Befuellt das Modell (oder den uebergebenen Stamm-Controller - er erbt
        // vom Modell) aus den Detailfeldern - gemeinsame Quelle fuer Vorpruefung,
        // Neuanlage und Ueberschreiben (Muster Form_WP_einlesen). Nicht parsbare
        // Zahlfelder werfen FormatException; das behandelt der Aufrufer.
        PufferSpModel FuelleModellwerte(PufferSpModel model = null)
        {
            if (model == null) model = new PufferSpModel();

            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Speichertyp = textBox_Typ.Text;
            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            model.Gesamtvolumen = Program.convertTxt2Int(textBox_Volumen.Text);

            return model;
        }

    }
}