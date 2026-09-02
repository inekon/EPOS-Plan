using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel_einlesen : Form
    {
        private HeizkesselImport ctrl = new HeizkesselImport();

        string szBrennstoffIndex = string.Empty;
        string szBrennstoffart = string.Empty;
        string szCO2 = string.Empty;
        string szNOx = string.Empty;
        string szCO = string.Empty;

        // Zuordnung: Position in der (gefilterten) ListBox -> Index in ctrl._list
        private System.Collections.Generic.List<int> _anzeigeIndex = new System.Collections.Generic.List<int>();

        // Sperre gegen Rueckkopplung: waehrend FuelleListe() die Markierung
        // wiederherstellt, feuert SelectedIndexChanged und wuerde die Detailfelder
        // auf einen Zwischenstand setzen.
        private bool _listeWirdGefuellt = false;

        public Form_Heizkessel_einlesen()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        // Filter-Felder num_LeistungVon/-Bis sind im Designer angelegt (NumericUpDown).
        private void Leistungsfilter_ValueChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        // Live-Filter ueber Bezeichner und Firma (Anwenderanforderung 17.08.2026).
        private void Suchfilter_TextChanged(object sender, EventArgs e)
        {
            FuelleListe();
        }

        // Fuellt die ListBox aus ctrl._list unter Beruecksichtigung des Leistungs-
        // und des Suchfilters und merkt sich je Zeile den echten Index in ctrl._list.
        private void FuelleListe()
        {
            double min = (double)num_LeistungVon.Value;
            double max = (double)num_LeistungBis.Value;
            string suche = txt_Filter.Text;

            // Markierung (echte Indizes) sichern und nach dem Neuaufbau fuer alle
            // weiterhin sichtbaren Eintraege wiederherstellen: so geht eine bereits
            // getroffene Auswahl beim Tippen im Filter nicht stillschweigend
            // verloren, und unsichtbare Eintraege bleiben unmarkiert.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();

            _listeWirdGefuellt = true;
            Liste_Heizkessel.BeginUpdate();
            Liste_Heizkessel.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                // ZahlParsen statt convertTxt2Double: eine nicht parsbare
                // Leistungsangabe darf den Listenaufbau nicht abbrechen - sie
                // zaehlt als 0, den Fehler meldet erst die Uebernahme.
                double p;
                if (!Program.ZahlParsen(ctrl._list[i].m_szThLeistung, out p)) p = 0;
                if (p < min) continue;
                if (p > max) continue;
                if (!VdiAuswahlFilter.Passt(suche, ctrl._list[i].m_szName, ctrl._list[i].m_szFirma)) continue;
                Liste_Heizkessel.Items.Add(ctrl._list[i].m_szName);
                _anzeigeIndex.Add(i);
            }
            for (int zeile = 0; zeile < _anzeigeIndex.Count; zeile++)
            {
                if (markiert.Contains(_anzeigeIndex[zeile])) Liste_Heizkessel.SetSelected(zeile, true);
            }
            Liste_Heizkessel.EndUpdate();
            _listeWirdGefuellt = false;

            // Detailfelder auf die verbleibende Markierung nachziehen, damit sie
            // nach dem Umfiltern nicht mehr auf einen ausgefilterten Eintrag
            // zeigen (bei der Uebernahme sind sie die Quelle bzw. die Anzeige).
            if (Liste_Heizkessel.SelectedIndex >= 0 && Liste_Heizkessel.SelectedIndex < _anzeigeIndex.Count)
                ZeigeDetails(_anzeigeIndex[Liste_Heizkessel.SelectedIndex]);
        }

        // Markierte Zeilen -> Indizes in ctrl._list.
        private System.Collections.Generic.List<int> MarkierteQuellIndizes()
        {
            return VdiAuswahlFilter.QuellIndizes(Liste_Heizkessel.SelectedIndices, _anzeigeIndex);
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_Heizkessel.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI_Heizkessel");

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

            int sel = Liste_Heizkessel.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            ZeigeDetails(_anzeigeIndex[sel]);
        }

        // Uebertraegt einen VDI-Eintrag in die Detailfelder. Die Felder (und die
        // Zusatzwerte szBrennstoffIndex/szBrennstoffart/szCO2/szNOx/szCO) sind auch beim
        // Mehrfachladen der Traeger fuer die Uebernahme (FuelleModellwerte liest
        // sie aus) - damit bleibt es bei genau einem Schreibweg.
        private void ZeigeDetails(int i)
        {
            if (i < 0 || i >= ctrl._list.Count) return;

            textBox_Name.Text = ctrl._list[i].m_szName;
            textBox_Firma.Text = ctrl._list[i].m_szFirma;
            textBox_Bauart.Text = ctrl._list[i].m_szBauart;
            textBox_ThLeistung.Text = ctrl._list[i].m_szThLeistung;
            textBox_Brennstoff.Text = ctrl._list[i].m_szBrennstoff;
            textBox_Versluste.Text = ctrl._list[i].m_szVerluste;
            textBox__Wirkungsgrad.Text = ctrl._list[i].m_szWirkungsgrad;
            szBrennstoffIndex = ctrl._list[i].m_szBrennstoffIndex;
            szBrennstoffart = ctrl._list[i].szBrennstoffart;
            szCO2 = ctrl._list[i].m_szCO2;
            szNOx = ctrl._list[i].m_szNOX;
            szCO = ctrl._list[i].m_szCO;
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            // Mehrfachselektion: je markierter Eintrag laeuft genau der bestehende
            // Einzel-Weg (UebernehmeEintrag) - nur in einer Schleife, damit es
            // keinen zweiten Schreibpfad in die STAMM-Tabelle gibt.
            System.Collections.Generic.List<int> markiert = MarkierteQuellIndizes();
            if (markiert.Count == 0)
            {
                MessageBox.Show("Bitte einen Heizkessel selektieren!");
                return;
            }

            // Vorpruefung der gesamten Auswahl gegen den Katalog und gegen sich
            // selbst (Konzept 4.1) - noch ohne Schreibzugriff. Quelle der Werte
            // sind die Detailfelder (FuelleModellwerte); beim Einzelfall werden
            // sie NICHT neu besetzt, damit eine Korrektur von Hand erhalten
            // bleibt und die Vorpruefung genau das prueft, was gespeichert
            // wuerde (Bestandsverhalten, siehe Einzelfall-Kommentar unten).
            KatalogDefinition katalog = KatalogRegistry.Finde("HEIZKESSEL");
            System.Collections.Generic.List<ImportKandidat> kandidaten =
                new System.Collections.Generic.List<ImportKandidat>();
            foreach (int i in markiert)
            {
                if (markiert.Count > 1) ZeigeDetails(i);
                HeizkesselModel probe = new HeizkesselModel();
                FuelleModellwerte(probe);
                ImportKandidat kand = new ImportKandidat { Name = probe.Name, Tag = i };
                kand.Werte["Firma"] = probe.Firma;
                kand.Werte["Ptherm"] = probe.Ptherm;
                kand.Werte["Brennstoff"] = probe.Brennstoff;
                kand.Werte["Wirkungsgrad_Gas"] = probe.Wirkungsgrad_Gas;
                kand.Werte["Wirkungsgrad_Öl"] = probe.Wirkungsgrad_Oel;
                kand.Werte["Raumbedarf"] = probe.Raumbedarf;
                kand.Werte["CO2"] = probe.CO2;
                kand.Werte["SO2"] = probe.SO2;
                kand.Werte["NOx"] = probe.NOx;
                kand.Werte["CO"] = probe.CO;
                kand.Werte["Staub"] = probe.Staub;
                kand.Werte["Betriebsbereitschaftverlust"] = probe.Betriebsbereitschaftverlust;
                kandidaten.Add(kand);
            }

            System.Collections.Generic.List<ImportPruefung> pruefungen =
                DublettenPruefung.PruefeKandidaten(katalog, kandidaten);

            bool konflikt = false;
            foreach (ImportPruefung p in pruefungen)
                if (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl) { konflikt = true; break; }

            if (!konflikt && markiert.Count == 1)
            {
                // Einzelfall: Meldungen und Dialogverhalten bleiben wie im Bestand;
                // die Detailfelder werden nicht neu besetzt, damit eine Korrektur
                // von Hand erhalten bleibt. Weil Handkorrektur vorgesehen ist,
                // werden die Zahlfelder vorab nach dem Hausmuster geprueft
                // (sprechende Meldung, Fokus, Dialog bleibt offen); leer gilt
                // wie bisher als 0.
                double dPruef;
                if (!Program.ZahlPruefen(textBox_ThLeistung, "Thermische Leistung", out dPruef, leerErlaubt: true)) return;
                if (!Program.ZahlPruefen(textBox__Wirkungsgrad, "Wirkungsgrad", out dPruef, leerErlaubt: true)) return;
                if (!Program.ZahlPruefen(textBox_Versluste, "Betriebsbereitschaftsverluste", out dPruef, leerErlaubt: true)) return;

                VdiUebernahmeErgebnis einzel = UebernehmeEintrag();
                if (einzel == VdiUebernahmeErgebnis.Duplikat)
                {
                    MessageBox.Show("Daten bereits eingelesen!");
                }
                else if (einzel == VdiUebernahmeErgebnis.Gespeichert)
                {
                    MessageBox.Show("Datensatz erfolgreich neu angelegt.");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Fehler: Name existiert bereits oder Datenbankfehler!");
                }
                return;
            }

            System.Collections.Generic.List<KonfliktEntscheidung> entscheidungen;
            if (konflikt)
            {
                // EIN Dialog fuer die ganze Auswahl statt einer Meldung je Satz.
                entscheidungen = Form_ImportKonflikte.Zeigen(this, pruefungen,
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
                    // FuelleModellwerte() und damit fuer die Uebernahme.
                    ZeigeDetails(i);

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
                                ergebnis = UebernehmeEintrag(ent.NeuerName);
                                if (ergebnis == VdiUebernahmeErgebnis.Gespeichert)
                                    ergebnis = VdiUebernahmeErgebnis.Umbenannt;
                                break;
                            default:
                                ergebnis = UebernehmeEintrag();
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

            // Wie im Bestand wird der Dialog nach erfolgreicher Uebernahme beendet -
            // auch Ueberschreiben und Umbenennen sind erfolgreiche Uebernahmen;
            // ohne einen einzigen Treffer bleibt er offen, damit der Anwender
            // Filter und Auswahl korrigieren kann.
            if (nGespeichert > 0 || nUeberschrieben > 0 || nUmbenannt > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        // Uebernahme genau eines Eintrags in Tab_Heizkessel_STAMM. Unveraenderter
        // Bestandsweg samt Transaktion (Quelle sind die Detailfelder), nur mit
        // Ergebnis als Rueckgabewert statt MessageBox - die Meldung und das
        // Schliessen des Dialogs entscheidet der Aufrufer. nameOverride traegt
        // beim Umbenennen (Konfliktdialog) den vom Anwender vergebenen neuen
        // Bezeichner (Konzept 4.3).
        private VdiUebernahmeErgebnis UebernehmeEintrag(string nameOverride = null)
        {
            try
            {
                // 1. Model aus den Detailfeldern initialisieren; beim Umbenennen
                //    ersetzt nameOverride den Bezeichner
                HeizkesselModel model = new HeizkesselModel();
                FuelleModellwerte(model);
                if (nameOverride != null) model.Name = nameOverride;

                // 2. Saubere Verbindung über das DataRepository öffnen
                // 3. Verbindung UND Transaktion sind ab S4e EIN Datenbankvorgang;
                //    ohne Commit rollt sein Dispose beim Verlassen zurueck - das
                //    ersetzt den frueheren Rollback im catch.
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // 4. Existenzprüfung via COUNT (Ersetzt die alte rs.Open-Logik) -
                    //    nach der Vorpruefung des Konfliktdialogs die zweite
                    //    Verteidigungslinie, sie prueft auch den Umbenennen-Namen
                    string checkSql = "SELECT COUNT(*) FROM [Tab_Heizkessel_STAMM] WHERE Bezeichner = ?";
                    int count = Convert.ToInt32(v.Skalar(checkSql, new OleDbParameter("?", model.Name)));
                    if (count > 0)
                    {
                        v.Rollback();
                        return VdiUebernahmeErgebnis.Duplikat;
                    }

                    // 5. Datensatz in einem Rutsch transaktionssicher speichern
                    if (Insert(model, v))
                    {
                        // Nur wenn das Insert mitsamt allen Feldern erfolgreich war, festschreiben
                        v.Commit();
                        return VdiUebernahmeErgebnis.Gespeichert;
                    }

                    v.Rollback();
                    return VdiUebernahmeErgebnis.Fehler;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Heizkessel Übernehmen: " + ex.Message);

                // Der Rueckrollvorgang ist bereits gelaufen: DbVorgang.Dispose rollt
                // beim Verlassen des using zurueck, wenn kein Commit gesehen wurde.
                return VdiUebernahmeErgebnis.Fehler;
            }
        }

        // Ueberschreiben aus dem Konfliktdialog: aktualisiert genau die Importfelder
        // des Bestandssatzes per HeizkesselStammCtrl.UpdateImport(bestandsId) -
        // ID, Bezeichner und Anwenderfelder bleiben stehen (Konzept 4.2).
        // ZeigeDetails(index) ist in der Ausfuehrungsschleife schon gelaufen, die
        // Detailfelder tragen also den Eintrag; Raumbedarf, SO2 und Staub bleiben
        // wie bei der Neuanlage auf dem Modell-Vorgabewert 0.
        private VdiUebernahmeErgebnis UeberschreibeEintrag(int index, int bestandsId)
        {
            // HeizkesselStammCtrl erbt von HeizkesselModel - FuelleModellwerte
            // besetzt damit direkt die this-Felder, die UpdateImport schreibt.
            HeizkesselStammCtrl stamm = new HeizkesselStammCtrl();
            FuelleModellwerte(stamm);

            return stamm.UpdateImport(bestandsId)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }

        // Überladene Insert-Methode, die voll in der aktiven Transaktion arbeitet
        public bool Insert(HeizkesselModel model, DbVorgang v)
        {
            try
            {
                object mx = v.Skalar("SELECT MAX(ID) FROM [Tab_Heizkessel_STAMM]");
                int newId = (mx == null || mx == DBNull.Value) ? 1 : Convert.ToInt32(mx) + 1;

                string sql = @"INSERT INTO [Tab_Heizkessel_STAMM] 
                       (ID, Bezeichner, Beschreibung, Firma, Ptherm, Brennstoff, Wirkungsgrad_Gas, Wirkungsgrad_Öl, 
                        Investitionskosten, Raumbedarf, Wartungskosten, Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust, ReadOnly) 
                       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                List<OleDbParameter> ps = new List<OleDbParameter>();
                ps.Add(new OleDbParameter("@id", newId));
                ps.Add(new OleDbParameter("@nam", model.Name ?? (object)DBNull.Value));
                ps.Add(new OleDbParameter("@bes", model.Beschreibung ?? (object)DBNull.Value));
                ps.Add(new OleDbParameter("@fir", model.Firma ?? (object)DBNull.Value));
                ps.Add(new OleDbParameter("@pth", model.Ptherm));
                ps.Add(new OleDbParameter("@bre", model.Brennstoff));
                ps.Add(new OleDbParameter("@wgg", model.Wirkungsgrad_Gas));
                ps.Add(new OleDbParameter("@wgo", model.Wirkungsgrad_Oel));
                ps.Add(new OleDbParameter("@inv", model.Investitionskosten));
                ps.Add(new OleDbParameter("@rau", model.Raumbedarf));
                ps.Add(new OleDbParameter("@war", model.Wartungskosten));
                ps.Add(new OleDbParameter("@nut", model.Nutzungsdauer));
                ps.Add(new OleDbParameter("@co2", model.CO2));
                ps.Add(new OleDbParameter("@so2", model.SO2));
                ps.Add(new OleDbParameter("@nox", model.NOx));
                ps.Add(new OleDbParameter("@co", model.CO));
                ps.Add(new OleDbParameter("@sta", model.Staub));
                ps.Add(new OleDbParameter("@bbv", model.Betriebsbereitschaftverlust));
                ps.Add(new OleDbParameter("@ro", false));

                v.Ausfuehren(sql, DbParam.Von(ps.ToArray()));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler bei Insert (Transaktions-Kontext): " + ex.Message);
                return false;
            }
        }

        // Befuellt das Modell mit genau den Werten, die der Import speichert.
        // Quelle sind die Detailfelder samt der Zusatzwerte szBrennstoffIndex/
        // szBrennstoffart/szCO2/szNOx/szCO (je Eintrag gesetzt durch ZeigeDetails) -
        // gemeinsame Quelle fuer Vorpruefung, Neuanlage und Ueberschreiben.
        // Vormals InitDatensatzUpdate().
        void FuelleModellwerte(HeizkesselModel model)
        {
            model.Name = textBox_Name.Text;
            model.Firma = textBox_Firma.Text;
            model.Beschreibung = textBox_Bauart.Text;
            model.Ptherm = Program.convertTxt2Double(textBox_ThLeistung.Text);

            // Brennstoffindex zuerst: er wird als model.Brennstoff gespeichert und
            // entscheidet, aus welchem Feld Simulation und Wirtschaftlichkeit den
            // Wirkungsgrad spaeter lesen (Oel = Index 6-9 und 18-22, wie
            // SimulationSPK.Stunde_Abschluss und der Brennstofffilter der Dialoge).
            int Brennstoffindex = Program.convertTxt2Int(szBrennstoffIndex);
            // Deckel gegen Indizes ausserhalb der Brennstofftabelle. Die Obergrenze
            // kommt aus Tab_Brennstoff_Stamm selbst (MAX(ID)), weil die Tabelle
            // waechst: der alte harte Deckel (> 22 -> 23) machte die spaeter
            // ergaenzten Eintraege Sonstige (24) und Wasserstoff (25) still zu
            // Fernwaerme (23). Ohne Tabellenwert bleibt 25 als heutiger Bestand.
            object oMaxBrennstoff = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM Tab_Brennstoff_Stamm");
            int nMaxBrennstoff = (oMaxBrennstoff != null) ? Convert.ToInt32(oMaxBrennstoff) : 25;
            if (Brennstoffindex > nMaxBrennstoff) Brennstoffindex = nMaxBrennstoff;
            model.Brennstoff = Brennstoffindex;

            double dWirkungsgrad = Program.convertTxt2Double(textBox__Wirkungsgrad.Text) / 100;
            if (Brennstoffindex > 0)
            {
                bool bOel = (Brennstoffindex >= 6 && Brennstoffindex <= 9)
                         || (Brennstoffindex >= 18 && Brennstoffindex <= 22);
                if (bOel) model.Wirkungsgrad_Oel = dWirkungsgrad;
                else model.Wirkungsgrad_Gas = dWirkungsgrad;
            }
            else
            {
                // Ohne Brennstoffindex wie bisher ueber die Brennstoffart des
                // VDI-Satzes (0 = Gas, 1 = Oel, sonst beide Felder). Liefert die
                // Datei gar keine Kennung, bleibt es beim Bestandsverhalten Gas.
                int nBrennstoffart = Program.convertTxt2Int(szBrennstoffart);
                if (nBrennstoffart == 0) model.Wirkungsgrad_Gas = dWirkungsgrad;
                else if (nBrennstoffart == 1) model.Wirkungsgrad_Oel = dWirkungsgrad;
                else model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = dWirkungsgrad;
            }

            if (model.Wirkungsgrad_Gas == 0 && model.Wirkungsgrad_Oel == 0)
                model.Wirkungsgrad_Gas = model.Wirkungsgrad_Oel = 1;

            model.Betriebsbereitschaftverlust = Program.convertTxt2Double(textBox_Versluste.Text);
            model.NOx = Program.convertTxt2Double(szNOx);
            model.CO2 = Program.convertTxt2Double(szCO2);
            model.CO = Program.convertTxt2Double(szCO);
        }
    }
}